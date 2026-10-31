"""Mandatory owned-process watchdog for the native 018 identity harness. No input/clipboard."""
import argparse
import ctypes as C
from contextlib import closing
from ctypes import wintypes as W
import hashlib
import errno
import os
from pathlib import Path
import platform
import queue
import re
import signal
import stat
import subprocess
import sys
import threading
import time

BASE = Path(__file__).resolve().parent
sys.path.insert(0, str(BASE / "macos"))
from run_smoke import validate_entry
from verify_evidence import inspect_executable


def no_links(path):
    for item in (path, *path.parents):
        if item.is_symlink() or (hasattr(item, 'is_junction') and item.is_junction()):
            raise ValueError('Artifact link refused')


def validate_harness(entry, digest, windows):
    entry = Path(os.path.abspath(entry))
    root = BASE / 'identity' / 'bin'
    no_links(entry)
    expected = 'SmartSnippets.TargetIdentity018' + ('.exe' if windows else '')
    if not entry.is_relative_to(root) or entry.name != expected or not re.fullmatch(r'[0-9a-fA-F]{64}', digest):
        raise ValueError('Harness provenance refused')
    if not 32 <= entry.stat().st_size <= 134217728:
        raise ValueError('Harness size refused')
    data = entry.read_bytes()
    if hashlib.sha256(data).hexdigest() != digest.lower() or inspect_executable(data, 'win-x64' if windows else 'osx-arm64'):
        raise ValueError('Harness artifact refused')
    if not windows:
        if entry.stat().st_mode & (stat.S_ISUID | stat.S_ISGID | stat.S_IWGRP | stat.S_IWOTH):
            raise ValueError('Harness mode refused')
        signature = subprocess.run(['/usr/bin/codesign', '--verify', '--strict', str(entry)],
                                   stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL, timeout=10)
        if signature.returncode:
            raise ValueError('Harness signature refused')
    return entry


class WindowsJob:
    """Kill-on-close job: assigned BEFORE the harness receives its launch permit."""
    def __init__(self, process):
        class Basic(C.Structure):
            _fields_ = [('process_time', C.c_int64), ('job_time', C.c_int64), ('flags', W.DWORD),
                        ('min_ws', C.c_size_t), ('max_ws', C.c_size_t), ('processes', W.DWORD),
                        ('affinity', C.c_size_t), ('priority', W.DWORD), ('scheduling', W.DWORD)]
        class Io(C.Structure):
            _fields_ = [(name, C.c_uint64) for name in ('read_ops', 'write_ops', 'other_ops', 'read_bytes', 'write_bytes', 'other_bytes')]
        class Extended(C.Structure):
            _fields_ = [('basic', Basic), ('io', Io), ('process_mem', C.c_size_t), ('job_mem', C.c_size_t),
                        ('peak_process', C.c_size_t), ('peak_job', C.c_size_t)]
        self.kernel = C.WinDLL('kernel32', use_last_error=True)
        self.kernel.CreateJobObjectW.argtypes, self.kernel.CreateJobObjectW.restype = [C.c_void_p, W.LPCWSTR], W.HANDLE
        self.kernel.SetInformationJobObject.argtypes, self.kernel.SetInformationJobObject.restype = [W.HANDLE, C.c_int, C.c_void_p, W.DWORD], W.BOOL
        self.kernel.AssignProcessToJobObject.argtypes, self.kernel.AssignProcessToJobObject.restype = [W.HANDLE, W.HANDLE], W.BOOL
        self.kernel.CloseHandle.argtypes, self.kernel.CloseHandle.restype = [W.HANDLE], W.BOOL
        self.handle = self.kernel.CreateJobObjectW(None, None)
        if not self.handle:
            raise ValueError('Owned job refused')
        settings = Extended()
        settings.basic.flags = 0x2000  # JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE; no breakaway.
        try:
            if not self.kernel.SetInformationJobObject(self.handle, 9, C.byref(settings), C.sizeof(settings)) or not self.kernel.AssignProcessToJobObject(self.handle, int(process._handle)):
                raise ValueError('Owned job refused')
        except BaseException:
            self.close()
            raise

    def close(self):
        if self.handle:
            if not self.kernel.CloseHandle(self.handle):
                raise ValueError('Owned job cleanup refused')
            self.handle = None


def pump(stream, messages, refused, error_stream=False):
    """Bound output even for an unterminated line; untrusted text is never logged."""
    try:
        line = bytearray()
        count = 0
        while True:
            char = stream.read(1)
            if not char:
                if line:
                    refused.set()
                return
            if error_stream:
                refused.set()
                return
            if char == b'\n':
                count += 1
                if count > 2:
                    refused.set()
                    return
                if line.endswith(b'\r'):
                    del line[-1:]
                text = line.decode('ascii', errors='strict')
                if '\r' in text:
                    raise ValueError('Protocol refused')
                messages.put_nowait(text)
                line.clear()
            else:
                if len(line) >= 256:
                    refused.set()
                    return
                line.extend(char)
    except (OSError, ValueError, queue.Full):
        refused.set()


def stop_owned(process, job, windows):
    # Do not let a second console interrupt abandon the bounded cleanup halfway.
    handlers = {}
    if threading.current_thread() is threading.main_thread():
        for name in ('SIGINT', 'SIGTERM', 'SIGHUP', 'SIGBREAK'):
            if hasattr(signal, name):
                value = getattr(signal, name)
                handlers[value] = signal.signal(value, signal.SIG_IGN)
    try:
        _stop_owned(process, job, windows)
    finally:
        for value, handler in handlers.items():
            signal.signal(value, handler)


def _stop_owned(process, job, windows):
    # Every native identity here derives from this Popen, never a supplied PID.
    try:
        if windows:
            if job is not None:
                job.close()  # Includes a target stuck in a synchronous AX/native call.
            elif process.poll() is None:
                process.kill()  # No permit was sent, so it could not create a target.
        else:
            # start_new_session=True created this private group before exec. All group
            # members are our harness and its children; never join a preexisting group.
            try:
                os.killpg(process.pid, signal.SIGKILL)
            except ProcessLookupError:
                pass
            except PermissionError:
                # Darwin returns EPERM for a group containing only zombies.
                # Do not swallow a real denied kill of a live group member.
                if not zombie_only_group(process.pid):
                    raise
        process.wait(timeout=5)
    finally:
        if process.stdin is not None:
            try:
                process.stdin.close()
            except OSError:
                pass


def zombie_only_group(pid):
    result = subprocess.run(['/bin/ps', '-o', 'pid=,pgid=,stat=', '-g', str(pid)],
                            stdin=subprocess.DEVNULL, capture_output=True, text=True, timeout=3)
    if result.returncode != 0 or result.stderr or len(result.stdout) > 4096:
        return False
    rows = [line.split() for line in result.stdout.splitlines()]
    return (bool(rows) and any(row[0] == str(pid) for row in rows if len(row) == 3) and
            all(len(row) == 3 and row[0].isdigit() and row[1] == str(pid) and
                row[2].startswith('Z') for row in rows))


def await_natural_exit(process, windows):
    if windows:
        # The live Windows process handle and Job Object preserve ownership even
        # after wait(); do not close the job before the harness has actually exited.
        process.wait(timeout=5)
        return
    # Pipe EOF can precede process exit. Observe NOTE_EXIT without waitpid/poll,
    # keeping the unreaped session-leader PID pinned until killpg cleanup.
    import select
    with closing(select.kqueue()) as events:
        request = select.kevent(process.pid, filter=select.KQ_FILTER_PROC,
                                flags=select.KQ_EV_ADD | select.KQ_EV_ONESHOT,
                                fflags=select.KQ_NOTE_EXIT)
        result = events.control([request], 1, 5)
        if (len(result) == 1 and result[0].ident == process.pid and
                result[0].flags & select.KQ_EV_ERROR and result[0].data == errno.ESRCH and
                zombie_only_group(process.pid)):
            return  # Already-zombie children cannot be newly registered with kqueue.
        if (len(result) != 1 or result[0].ident != process.pid or
                result[0].flags & select.KQ_EV_ERROR or
                not result[0].fflags & select.KQ_NOTE_EXIT):
            raise ValueError('Native exit was not observed')


CALLBACK_RESULT = 'owned_callback=OBSERVED delivery=INPROCESS_NATIVE_EVENT physical_shortcut=NOT_RUN cleanup=STOPPED input=NONE clipboard=UNTOUCHED'


def run(harness, target, digest, windows, native_callback=False):
    process = job = None
    messages, refused = queue.Queue(maxsize=2), threading.Event()
    threads = []
    receipts = []
    try:
        arguments = [str(harness), '--authorized-screen-slot', '--watchdog-stdin', '--executable', str(target), '--sha256', digest]
        if native_callback:
            arguments += ['--inprocess-native-callback', '--authorized-native-hotkey']
        process = subprocess.Popen(arguments,
                                   stdin=subprocess.PIPE, stdout=subprocess.PIPE, stderr=subprocess.PIPE,
                                   cwd=harness.parent, creationflags=subprocess.CREATE_NO_WINDOW if windows else 0,
                                   start_new_session=not windows)
        if windows:
            job = WindowsJob(process)
        for stream, is_error in ((process.stdout, False), (process.stderr, True)):
            thread = threading.Thread(target=pump, args=(stream, messages, refused, is_error), daemon=True)
            thread.start()
            threads.append(thread)
        process.stdin.write(b'owned_watchdog_ready\n')
        process.stdin.flush()
        deadline = time.monotonic() + 65
        # Do not poll/reap the Mac session leader before killpg cleanup: its
        # unreaped PID pins our private process-group identity against PID reuse.
        while threads[0].is_alive() and time.monotonic() < deadline and not refused.is_set():
            time.sleep(0.05)
        if threads[0].is_alive() or refused.is_set():
            raise ValueError('Owned watchdog deadline or protocol refusal')
        for thread in threads:
            thread.join(timeout=1)
        while not messages.empty():
            receipts.append(messages.get_nowait())
        if refused.is_set() or any(t.is_alive() for t in threads) or len(receipts) != 2 or not re.fullmatch(r'private_evidence_token=[0-9a-f]{32}', receipts[0]) or receipts[1] != (CALLBACK_RESULT if native_callback else 'owned_identity=OBSERVED cleanup=STOPPED input=NONE clipboard=UNTOUCHED'):
            raise ValueError('Native owned identity refused')
        await_natural_exit(process, windows)
    finally:
        if process is not None:
            try:
                stop_owned(process, job, windows)
            finally:
                for stream in (process.stdout, process.stderr):
                    if stream is not None:
                        stream.close()
                for thread in threads:
                    thread.join(timeout=1)
    if process.returncode != 0:
        raise ValueError('Native owned identity refused')
    print(receipts[0])
    print('owned_callback_harness=OBSERVED delivery=INPROCESS_NATIVE_EVENT physical_shortcut=NOT_RUN watchdog=REAPED input=NONE clipboard=UNTOUCHED' if native_callback else 'owned_identity_harness=OBSERVED watchdog=REAPED input=NONE clipboard=UNTOUCHED')
    return 0


class FixedDiagnosticParser(argparse.ArgumentParser):
    def error(self, message):
        raise ValueError('Arguments refused')


def main():
    parser = FixedDiagnosticParser(description=__doc__)
    parser.add_argument('--harness', required=True, type=Path)
    parser.add_argument('--harness-sha256', required=True)
    parser.add_argument('--executable', required=True, type=Path)
    parser.add_argument('--sha256', required=True)
    parser.add_argument('--authorized-screen-slot', action='store_true')
    mode = parser.add_mutually_exclusive_group()
    mode.add_argument('--inprocess-native-callback', action='store_true')
    mode.add_argument('--identity-only', action='store_true')
    parser.add_argument('--authorized-native-hotkey', action='store_true')
    args = parser.parse_args()
    if args.inprocess_native_callback != args.authorized_native_hotkey:
        raise ValueError('Explicit native hotkey authorization required')
    windows = sys.platform == 'win32'
    if not args.authorized_screen_slot or (not windows and sys.platform != 'darwin') or platform.machine().lower() not in (('amd64', 'x86_64') if windows else ('arm64',)):
        raise ValueError('Authorized native host required')
    harness = validate_harness(args.harness, args.harness_sha256, windows)
    no_links(Path(os.path.abspath(args.executable)))
    if not 32 <= args.executable.stat().st_size <= 134217728:
        raise ValueError('Target size refused')
    target = validate_entry(args.executable, args.sha256, windows)
    if args.inprocess_native_callback:
        return run(harness, target, args.sha256, windows, native_callback=True)
    return run(harness, target, args.sha256, windows)


def interrupted(signum, frame):
    raise InterruptedError('Owned watchdog interrupted')


if __name__ == '__main__':
    for name in ('SIGTERM', 'SIGHUP', 'SIGBREAK'):
        if hasattr(signal, name):
            signal.signal(getattr(signal, name), interrupted)
    try:
        sys.exit(main())
    except (OSError, ValueError, subprocess.SubprocessError, KeyboardInterrupt):
        print('owned_identity_harness_failed', file=sys.stderr)
        sys.exit(1)
