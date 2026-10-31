"""Plan019 startup-only observer. Review gate intentionally closed; no input synthesis.

After independent review the operator must change STARTUP_OBSERVER_REVIEWED locally.
No native acceptance beyond an owned visible startup window is asserted.
"""
import argparse
import ctypes as C
from ctypes import wintypes as W
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import re
import subprocess
import sys
import time
import uuid

REPO = Path(__file__).resolve().parents[1]
STARTUP_OBSERVER_REVIEWED = False
PERMIT = b'owned_startup_ready\n'


def repo_module(name, relative):
    spec = importlib.util.spec_from_file_location(name, REPO / relative)
    module = importlib.util.module_from_spec(spec)
    # Existing helpers use sibling imports; only explicit repository directories.
    sys.path.insert(0, str((REPO / relative).parent))
    try:
        spec.loader.exec_module(module)
    finally:
        sys.path.pop(0)
    return module


def validate_candidate(path, digest):
    path = Path(os.path.abspath(path))
    root = REPO / 'src/SmartSnippets.Uno/bin/publish-uno-candidate'
    if (path.parent.parent != root or path.name != 'SmartSnippets.UnoCandidate.exe'
            or not re.fullmatch(r'[a-zA-Z0-9_-]{1,80}', path.parent.name)
            or not re.fullmatch(r'[0-9a-fA-F]{64}', digest)):
        raise ValueError('provenance')
    for item in (path, *path.parents):
        info = item.lstat()
        if item.is_symlink() or getattr(info, 'st_file_attributes', 0) & 0x400:
            raise ValueError('alias')
    if not 32 <= path.stat().st_size <= 134217728:
        raise ValueError('size')
    if path.stat().st_nlink != 1:
        raise ValueError('hardlink')
    data = path.read_bytes()
    inspector = repo_module('startup_pe', 'prototypes/uno-desktop/verify_evidence.py')
    if hashlib.sha256(data).hexdigest() != digest.lower() or inspector.inspect_executable(data, 'win-x64'):
        raise ValueError('artifact')
    return path


def local_app_data():
    shell, ole = C.WinDLL('shell32'), C.WinDLL('ole32')
    shell.SHGetKnownFolderPath.argtypes = [C.c_void_p, W.DWORD, W.HANDLE, C.POINTER(C.c_void_p)]
    shell.SHGetKnownFolderPath.restype = C.c_int32
    ole.CoTaskMemFree.argtypes, ole.CoTaskMemFree.restype = [C.c_void_p], None
    identifier = (C.c_ubyte * 16).from_buffer_copy(uuid.UUID('f1b32785-6fba-4fcf-9d55-7b8e7f157091').bytes_le)
    value = C.c_void_p()
    try:
        if shell.SHGetKnownFolderPath(identifier, 0, None, C.byref(value)) != 0 or not value.value:
            raise ValueError('local_app_data')
        return Path(C.wstring_at(value.value))
    finally:
        if value.value:
            ole.CoTaskMemFree(value)


def system_tool(name):
    kernel = C.WinDLL('kernel32')
    kernel.GetSystemDirectoryW.argtypes, kernel.GetSystemDirectoryW.restype = [W.LPWSTR, W.UINT], W.UINT
    value = C.create_unicode_buffer(32768)
    count = kernel.GetSystemDirectoryW(value, len(value))
    if not count or count >= len(value) or name != 'tasklist.exe':
        raise ValueError('system_tool')
    return str(Path(value.value) / name)


def profile_absent(local):
    root = Path(local) / 'SmartSnippets.UnoCandidate019'
    # lexists refuses dangling aliases too. Never inspect contents or change it.
    if os.path.lexists(root):
        raise ValueError('profile_exists')
    for item in (root.parent, *root.parent.parents):
        if item.is_symlink() or getattr(item.lstat(), 'st_file_attributes', 0) & 0x400:
            raise ValueError('profile_alias')
    return root


def require_job_membership():
    kernel = C.WinDLL('kernel32', use_last_error=True)
    kernel.GetCurrentProcess.argtypes, kernel.GetCurrentProcess.restype = [], W.HANDLE
    kernel.IsProcessInJob.argtypes, kernel.IsProcessInJob.restype = [W.HANDLE, W.HANDLE, C.POINTER(W.BOOL)], W.BOOL
    member = W.BOOL()
    if not kernel.IsProcessInJob(kernel.GetCurrentProcess(), None, C.byref(member)) or not member.value:
        raise ValueError('job_membership')


def validate_private(private):
    if not private.is_absolute() or not re.fullmatch(r'ss-uno018-ui-[a-zA-Z0-9_-]{8,80}', private.name):
        raise ValueError('private_evidence')
    for item in (private, *private.parents):
        if item.is_symlink() or getattr(item.lstat(), 'st_file_attributes', 0) & 0x400:
            raise ValueError('private_alias')
    if not private.is_dir() or any(private.iterdir()):
        raise ValueError('private_evidence')


def inside(rect, work):
    l, t, r, b = rect
    x, y, z, w = work
    return x <= l < r <= z and y <= t < b <= w


def bottom_left_work_area(monitors):
    if not monitors or any(not inside(rect, rect) or not inside(work, rect) for rect, work in monitors):
        raise ValueError('monitors')
    return min(monitors, key=lambda item: (item[0][0], -item[0][1]))[1]


def observe(process, snapshot, windows, initial, work, clock=time.monotonic, sleep=time.sleep):
    deadline = clock() + 10
    while clock() < deadline:
        if snapshot() != initial:
            raise ValueError('desktop_changed')
        if process.poll() is not None:
            raise ValueError('early_exit')
        rects = windows(process.pid)
        if rects:
            if not all(inside(rect, work) for rect in rects):
                raise ValueError('geometry')
            return
        sleep(.05)
    raise ValueError('missing_window')


class Desktop:
    def __init__(self, visible_windows):
        self.visible_windows = visible_windows
        self.user = C.WinDLL('user32', use_last_error=True)
        self.dwm = C.WinDLL('dwmapi', use_last_error=True)
        for name, args, result in (
            ('GetForegroundWindow', [], W.HWND),
            ('GetClipboardSequenceNumber', [], W.DWORD),
            ('GetWindowRect', [W.HWND, C.POINTER(W.RECT)], W.BOOL)):
            fn = getattr(self.user, name)
            fn.argtypes, fn.restype = args, result
        self.dwm.DwmGetWindowAttribute.argtypes = [W.HWND, W.DWORD, C.c_void_p, W.DWORD]
        self.dwm.DwmGetWindowAttribute.restype = C.c_long
        self.user.SetThreadDpiAwarenessContext.argtypes = [C.c_void_p]
        self.user.SetThreadDpiAwarenessContext.restype = C.c_void_p
        self.previous_dpi = None

    def enable_dpi(self):
        self.previous_dpi = self.user.SetThreadDpiAwarenessContext(C.c_void_p(-4))
        if not self.previous_dpi:
            raise ValueError('dpi_context')

    def close(self):
        previous, self.previous_dpi = self.previous_dpi, None
        if previous and not self.user.SetThreadDpiAwarenessContext(previous):
            raise ValueError('dpi_cleanup')

    def validate_input_desktop(self):
        self.user.OpenInputDesktop.argtypes = [W.DWORD, W.BOOL, W.DWORD]
        self.user.OpenInputDesktop.restype = W.HANDLE
        self.user.CloseDesktop.argtypes = [W.HANDLE]
        self.user.CloseDesktop.restype = W.BOOL
        self.user.GetUserObjectInformationW.argtypes = [W.HANDLE, C.c_int, C.c_void_p, W.DWORD, C.POINTER(W.DWORD)]
        self.user.GetUserObjectInformationW.restype = W.BOOL
        handle = self.user.OpenInputDesktop(0, False, 1)  # DESKTOP_READOBJECTS only.
        if not handle:
            raise ValueError('input_desktop')
        try:
            name = C.create_unicode_buffer(256)
            needed = W.DWORD()
            if not self.user.GetUserObjectInformationW(handle, 2, name, C.sizeof(name), C.byref(needed)) or name.value != 'Default':
                raise ValueError('input_desktop')
        finally:
            if not self.user.CloseDesktop(handle):
                raise ValueError('desktop_cleanup')

    def validate_work_area(self, expected):
        class MonitorInfo(C.Structure):
            _fields_ = [('size', W.DWORD), ('monitor', W.RECT), ('work', W.RECT), ('flags', W.DWORD)]
        callback_type = C.WINFUNCTYPE(W.BOOL, W.HANDLE, W.HDC, C.POINTER(W.RECT), W.LPARAM)
        self.user.GetMonitorInfoW.argtypes = [W.HANDLE, C.POINTER(MonitorInfo)]
        self.user.GetMonitorInfoW.restype = W.BOOL
        self.user.EnumDisplayMonitors.argtypes = [W.HDC, C.POINTER(W.RECT), callback_type, W.LPARAM]
        self.user.EnumDisplayMonitors.restype = W.BOOL
        found, errors = [], []
        def visit(handle, dc, rect, param):
            info = MonitorInfo()
            info.size = C.sizeof(info)
            if not self.user.GetMonitorInfoW(handle, C.byref(info)):
                errors.append(True)
            else:
                r, m = info.work, info.monitor
                found.append(((m.left, m.top, m.right, m.bottom), (r.left, r.top, r.right, r.bottom)))
            return True
        callback = callback_type(visit)
        if not self.user.EnumDisplayMonitors(None, None, callback, 0) or errors or not found:
            raise ValueError('monitors')
        # Same policy as the candidate: bottom monitor of the leftmost column.
        if tuple(expected) != bottom_left_work_area(found):
            raise ValueError('bottom_left_work_area')

    def snapshot(self):
        self.validate_input_desktop()
        value = (self.user.GetForegroundWindow(), self.user.GetClipboardSequenceNumber())
        if not all(value):
            raise ValueError('desktop_unavailable')
        return value

    def windows(self, pid):
        rects = []
        for hwnd in self.visible_windows(pid, identities=True):
            cloak = W.DWORD()
            if self.dwm.DwmGetWindowAttribute(hwnd, 14, C.byref(cloak), C.sizeof(cloak)):
                raise ValueError('cloak')
            if cloak.value:
                continue
            rect = W.RECT()
            if not self.user.GetWindowRect(hwnd, C.byref(rect)):
                raise ValueError('rect')
            rects.append((rect.left, rect.top, rect.right, rect.bottom))
        return rects


def stop_app(process):
    if process.poll() is None:
        process.terminate()
    try:
        process.wait(timeout=2)
    except subprocess.TimeoutExpired:
        process.kill()
        process.wait(timeout=2)


def worker(entry, digest, private, work, stream=sys.stdin.buffer):
    # Bootstrap cannot create any candidate before parent assigns its Job.
    if stream.readline(64) != PERMIT:
        raise ValueError('permit')
    require_job_membership()
    validate_private(private)
    entry = validate_candidate(entry, digest)
    profile_absent(local_app_data())
    # Conservative refusal of any candidate image; never terminate discovered PIDs.
    check = subprocess.run([system_tool('tasklist.exe'), '/FI', 'IMAGENAME eq SmartSnippets.UnoCandidate.exe', '/FO', 'CSV', '/NH'],
                           capture_output=True, timeout=3, creationflags=subprocess.CREATE_NO_WINDOW)
    if check.returncode or check.stderr or b'smartsnippets.unocandidate.exe' in check.stdout.lower():
        raise ValueError('prior_ownership')
    ui = repo_module('startup_ui', 'prototypes/uno-desktop/run_ui_smoke.py')
    desktop = Desktop(ui.visible_windows)
    try:
        desktop.enable_dpi()  # Observer thread only; never changes application/system DPI settings.
        desktop.validate_work_area(work)
        initial = desktop.snapshot()
        process = None
        with (private / 'stdout.log').open('xb') as out, (private / 'stderr.log').open('xb') as err:
            try:
                process = subprocess.Popen([str(entry)], cwd=entry.parent, stdin=subprocess.DEVNULL,
                                           stdout=out, stderr=err)
                observe(process, desktop.snapshot, desktop.windows, initial, work)
            finally:
                try:
                    if process is not None:
                        stop_app(process)
                finally:
                    if desktop.snapshot() != initial:
                        raise ValueError('desktop_changed')
    finally:
        desktop.close()
    if (private / 'stderr.log').stat().st_size:
        raise ValueError('process_stderr')
    print('startup_observed', flush=True)


def finish_job(job, process):
    # Query only our retained job handle; no PID enumeration or foreign termination.
    kernel = job.kernel
    kernel.TerminateJobObject.argtypes = [W.HANDLE, W.UINT]
    kernel.TerminateJobObject.restype = W.BOOL
    kernel.QueryInformationJobObject.argtypes = [W.HANDLE, C.c_int, C.c_void_p, W.DWORD, C.c_void_p]
    kernel.QueryInformationJobObject.restype = W.BOOL
    class Accounting(C.Structure):
        _fields_ = [('times', C.c_int64 * 4), ('faults', W.DWORD),
                    ('total', W.DWORD), ('active', W.DWORD), ('terminated', W.DWORD)]
    try:
        if not kernel.TerminateJobObject(job.handle, 1):
            raise ValueError('job_stop')
        process.wait(timeout=5)
        deadline = time.monotonic() + 5
        while True:
            accounting = Accounting()
            if not kernel.QueryInformationJobObject(job.handle, 1, C.byref(accounting), C.sizeof(accounting), None):
                raise ValueError('job_query')
            if accounting.active == 0:
                break
            if time.monotonic() >= deadline:
                raise ValueError('job_active')
            time.sleep(.05)
    finally:
        job.close()


def supervise(command, job_factory, popen=subprocess.Popen):
    process = popen(command, stdin=subprocess.PIPE, stdout=subprocess.PIPE, stderr=subprocess.PIPE,
                    creationflags=subprocess.CREATE_NO_WINDOW)
    job = None
    try:
        job = job_factory(process)  # MUST precede permit.
        out, err = process.communicate(PERMIT, timeout=20)
        if process.returncode or err or out not in (b'startup_observed\n', b'startup_observed\r\n'):
            raise ValueError('worker_protocol')
    finally:
        if job is not None:
            finish_job(job, process)
        else:
            stop_app(process)  # No permit: no app can have been started.


class Parser(argparse.ArgumentParser):
    def error(self, message):
        raise ValueError('arguments')


def main():
    parser = Parser(allow_abbrev=False)
    parser.add_argument('--executable', required=True, type=Path)
    parser.add_argument('--sha256', required=True)
    parser.add_argument('--authorized-screen-slot', action='store_true')
    parser.add_argument('--work-area', required=True, nargs=4, type=int,
                        help='Operator-verified bottom-left monitor work rectangle')
    parser.add_argument('--worker-private', type=Path, help=argparse.SUPPRESS)
    args = parser.parse_args()
    if not STARTUP_OBSERVER_REVIEWED or not args.authorized_screen_slot or sys.platform != 'win32':
        raise ValueError('authorization')
    entry = validate_candidate(args.executable, args.sha256)
    profile_absent(local_app_data())
    if not inside(args.work_area, args.work_area):
        raise ValueError('work_area')
    if args.worker_private:
        worker(entry, args.sha256, args.worker_private, args.work_area)
        return
    ui = repo_module('startup_ui', 'prototypes/uno-desktop/run_ui_smoke.py')
    identity = repo_module('startup_identity', 'prototypes/uno-native-target/run_identity_harness.py')
    private = ui.private_directory()
    command = [sys.executable, '-I', '-B', str(Path(__file__).resolve()), '--executable', str(entry),
               '--sha256', args.sha256, '--authorized-screen-slot', '--work-area',
               *map(str, args.work_area), '--worker-private', str(private)]
    supervise(command, identity.WindowsJob)
    if not (local_app_data() / 'SmartSnippets.UnoCandidate019').is_dir():
        raise ValueError('missing_retained_profile')
    return private.name


def cli():
    is_worker = any(arg == '--worker-private' or arg.startswith('--worker-private=') for arg in sys.argv[1:])
    try:
        evidence_token = main()
        if evidence_token is not None:
            print(json.dumps({'startup': 'OBSERVED', 'nativeAcceptance': 'NOT_RUN',
                              'stop': 'FORCED', 'job': 'ZERO_ACTIVE', 'profile': 'RETAINED',
                              'privateEvidenceToken': evidence_token}))
    except BaseException:
        if not is_worker:
            print(json.dumps({'startup': 'BLOCKED', 'nativeAcceptance': 'NOT_RUN'}))
        return 1
    return 0


if __name__ == '__main__':
    sys.exit(cli())
