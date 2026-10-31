"""Review-gated, synthetic-only private-window-station API prerequisite (Windows x64)."""
from __future__ import annotations

import argparse
import ctypes as C
from ctypes import wintypes as W
import hashlib
import json
import os
from pathlib import Path
import queue
import subprocess
import sys
import threading
import time
import uuid

REVIEWED = True  # Reviewed private-station-only trial; interactive clipboard is never opened.
TIMEOUT = 12
SCRIPT = Path(__file__).resolve()
SAMPLES = (b"private-fixture-a\0", b"private-fixture-b\0", b"private-fixture-c\0")
RECEIPTS = frozenset({"cross_process_guardian_owner", "same_owner_external_write",
                      "general_sequence_unchanged", "blocked_createwindowstation_null_create_only",
                      "probe_failed", "not_run_abort_after_clear", "not_run_foreign_owner_refusal"})


class Closed(Exception):
    pass


class CreationBlocked(Closed):
    pass


def require(value):
    if not value:
        raise Closed()
    return value


def digest(path):
    with open(path, "rb") as stream:
        return hashlib.file_digest(stream, "sha256").hexdigest()


def validate_identity(config, parent_pid, parent_path):
    require(set(config) == {"run", "script", "python", "parent", "sid"})
    require(str(uuid.UUID(config["run"])) == config["run"])
    require(config["script"] == digest(SCRIPT))
    require(config["python"] == digest(sys.executable) == digest(parent_path))
    require(config["parent"] == parent_pid == os.getppid())


def receipt(name):
    require(name in RECEIPTS)
    return {"receipt": name, "count": 1}


def valid_receipts(rows):
    require(isinstance(rows, list) and len(rows) <= len(RECEIPTS))
    require(all(isinstance(row, dict) and set(row) == {"receipt", "count"}
                and row["receipt"] in RECEIPTS and type(row["count"]) is int
                and row["count"] == 1 for row in rows))
    require(len({row["receipt"] for row in rows}) == len(rows))
    return rows


class Channel:
    """One bounded line reader per owned pipe; no arbitrary child output is forwarded."""
    def __init__(self, stream):
        self.messages = queue.Queue(maxsize=2)
        self.thread = threading.Thread(target=self._read, args=(stream,), daemon=True)
        self.thread.start()

    def _read(self, stream):
        try:
            while True:
                line = stream.readline(4097)
                if not line or len(line) > 4096 or not line.endswith("\n"):
                    break
                self.messages.put_nowait(json.loads(line))
        except (ValueError, OSError, queue.Full):
            pass
        finally:
            try:
                self.messages.put_nowait(None)
            except queue.Full:
                pass

    def get(self, pump=None):
        deadline = time.monotonic() + TIMEOUT
        while True:
            if pump is not None:
                pump()
            remaining = deadline - time.monotonic()
            require(remaining > 0)
            try:
                return require(self.messages.get(timeout=min(0.01, remaining)))
            except queue.Empty:
                pass


def send(stream, value):
    stream.write(json.dumps(value, separators=(",", ":")) + "\n")
    stream.flush()


class SA(C.Structure):
    _fields_ = [("length", W.DWORD), ("descriptor", C.c_void_p), ("inherit", W.BOOL)]


class UserFlags(C.Structure):
    _fields_ = [("inherit", W.BOOL), ("reserved", W.BOOL), ("flags", W.DWORD)]


class BasicLimits(C.Structure):
    _fields_ = [("process_time", C.c_int64), ("job_time", C.c_int64),
                ("flags", W.DWORD), ("min_ws", C.c_size_t), ("max_ws", C.c_size_t),
                ("active_limit", W.DWORD), ("affinity", C.c_size_t),
                ("priority", W.DWORD), ("scheduling", W.DWORD)]


class IoCounters(C.Structure):
    _fields_ = [(name, C.c_uint64) for name in
                ("read_ops", "write_ops", "other_ops", "read_bytes", "write_bytes", "other_bytes")]


class ExtendedLimits(C.Structure):
    _fields_ = [("basic", BasicLimits), ("io", IoCounters),
                ("process_memory", C.c_size_t), ("job_memory", C.c_size_t),
                ("peak_process", C.c_size_t), ("peak_job", C.c_size_t)]


class Accounting(C.Structure):
    _fields_ = [("user", C.c_int64), ("kernel", C.c_int64),
                ("period_user", C.c_int64), ("period_kernel", C.c_int64),
                ("faults", W.DWORD), ("total", W.DWORD), ("active", W.DWORD),
                ("terminated", W.DWORD)]


class SidAttributes(C.Structure):
    _fields_ = [("sid", C.c_void_p), ("attributes", W.DWORD)]


class Native:
    """All imported ABI signatures are explicit; only constructed on authorized Windows."""
    def __init__(self):
        require(sys.platform == "win32" and C.sizeof(C.c_void_p) == 8)
        require(C.sizeof(W.DWORD) == 4 and C.sizeof(SA) == 24
                and C.sizeof(UserFlags) == 12 and C.sizeof(SidAttributes) == 16
                and C.sizeof(ExtendedLimits) == 144 and C.sizeof(Accounting) == 48)
        self.u = C.WinDLL("user32", use_last_error=True)
        self.k = C.WinDLL("kernel32", use_last_error=True)
        self.a = C.WinDLL("advapi32", use_last_error=True)
        ptr, handle, dword, boolean = C.c_void_p, W.HANDLE, W.DWORD, W.BOOL
        signatures = [
            (self.u, "GetClipboardSequenceNumber", dword, []),
            (self.u, "GetProcessWindowStation", handle, []),
            (self.u, "GetThreadDesktop", handle, [dword]),
            (self.u, "SetProcessWindowStation", boolean, [handle]),
            (self.u, "SetThreadDesktop", boolean, [handle]),
            (self.u, "CreateWindowStationW", handle, [W.LPCWSTR, dword, dword, C.POINTER(SA)]),
            (self.u, "OpenWindowStationW", handle, [W.LPCWSTR, boolean, dword]),
            (self.u, "CloseWindowStation", boolean, [handle]),
            (self.u, "CreateDesktopW", handle, [W.LPCWSTR, W.LPCWSTR, ptr, dword, dword, C.POINTER(SA)]),
            (self.u, "OpenDesktopW", handle, [W.LPCWSTR, dword, boolean, dword]),
            (self.u, "CloseDesktop", boolean, [handle]),
            (self.u, "GetUserObjectInformationW", boolean, [handle, C.c_int, ptr, dword, C.POINTER(dword)]),
            (self.u, "CreateWindowExW", W.HWND, [dword, W.LPCWSTR, W.LPCWSTR, dword,
                C.c_int, C.c_int, C.c_int, C.c_int, W.HWND, handle, handle, ptr]),
            (self.u, "DestroyWindow", boolean, [W.HWND]),
            (self.u, "PeekMessageW", boolean, [ptr, W.HWND, W.UINT, W.UINT, W.UINT]),
            (self.u, "DispatchMessageW", C.c_ssize_t, [ptr]),
            (self.u, "GetWindowThreadProcessId", dword, [W.HWND, C.POINTER(dword)]),
            (self.u, "OpenClipboard", boolean, [W.HWND]),
            (self.u, "CloseClipboard", boolean, []),
            (self.u, "EmptyClipboard", boolean, []),
            (self.u, "CountClipboardFormats", C.c_int, []),
            (self.u, "GetClipboardOwner", W.HWND, []),
            (self.u, "SetClipboardData", handle, [W.UINT, handle]),
            (self.u, "GetClipboardData", handle, [W.UINT]),
            (self.k, "GetCurrentProcess", handle, []),
            (self.k, "GetCurrentThreadId", dword, []),
            (self.k, "OpenProcess", handle, [dword, boolean, dword]),
            (self.k, "QueryFullProcessImageNameW", boolean, [handle, dword, W.LPWSTR, C.POINTER(dword)]),
            (self.k, "CloseHandle", boolean, [handle]),
            (self.k, "LocalFree", handle, [handle]),
            (self.k, "GlobalAlloc", handle, [W.UINT, C.c_size_t]),
            (self.k, "GlobalLock", ptr, [handle]),
            (self.k, "GlobalUnlock", boolean, [handle]),
            (self.k, "GlobalFree", handle, [handle]),
            (self.k, "GlobalSize", C.c_size_t, [handle]),
            (self.k, "CreateJobObjectW", handle, [ptr, W.LPCWSTR]),
            (self.k, "SetInformationJobObject", boolean, [handle, C.c_int, ptr, dword]),
            (self.k, "AssignProcessToJobObject", boolean, [handle, handle]),
            (self.k, "TerminateJobObject", boolean, [handle, W.UINT]),
            (self.k, "QueryInformationJobObject", boolean, [handle, C.c_int, ptr, dword, C.POINTER(dword)]),
            (self.a, "OpenProcessToken", boolean, [handle, dword, C.POINTER(handle)]),
            (self.a, "GetTokenInformation", boolean, [handle, C.c_int, ptr, dword, C.POINTER(dword)]),
            (self.a, "IsValidSid", boolean, [ptr]),
            (self.a, "ConvertSidToStringSidW", boolean, [ptr, C.POINTER(ptr)]),
            (self.a, "ConvertStringSecurityDescriptorToSecurityDescriptorW", boolean,
                [W.LPCWSTR, dword, C.POINTER(ptr), C.POINTER(dword)]),
        ]
        for dll, name, result, args in signatures:
            fn = getattr(dll, name)
            fn.restype, fn.argtypes = result, args

    def pump(self):
        # x64 MSG is 48 bytes; PeekMessage also services cross-thread sent messages.
        message = C.create_string_buffer(48)
        for _ in range(64):
            if not self.u.PeekMessageW(message, None, 0, 0, 1):
                break
            self.u.DispatchMessageW(message)

    def sid(self):
        token = W.HANDLE()
        require(self.a.OpenProcessToken(self.k.GetCurrentProcess(), 8, C.byref(token)))
        try:
            size = W.DWORD()
            self.a.GetTokenInformation(token, 1, None, 0, C.byref(size))
            require(0 < size.value <= 65536)
            buf = C.create_string_buffer(size.value)
            require(self.a.GetTokenInformation(token, 1, buf, size, C.byref(size)))
            # TokenElevation: this prerequisite never runs elevated or requests elevation.
            elevated = W.DWORD()
            require(self.a.GetTokenInformation(token, 20, C.byref(elevated), C.sizeof(elevated), C.byref(size)))
            require(elevated.value == 0)
            sid = C.cast(buf, C.POINTER(SidAttributes)).contents.sid
            require(self.a.IsValidSid(sid))
            text = C.c_void_p()
            require(self.a.ConvertSidToStringSidW(sid, C.byref(text)))
            try:
                result = C.wstring_at(text)
                require(result.startswith("S-1-") and all(c in "S-0123456789" for c in result))
                return result
            finally:
                self.k.LocalFree(text)
        finally:
            self.k.CloseHandle(token)

    def parent_path(self):
        handle = require(self.k.OpenProcess(0x1000, False, os.getppid()))
        try:
            buf, size = C.create_unicode_buffer(32768), W.DWORD(32768)
            require(self.k.QueryFullProcessImageNameW(handle, 0, buf, C.byref(size)))
            return buf.value
        finally:
            self.k.CloseHandle(handle)

    def name(self, handle):
        buf, size = C.create_unicode_buffer(512), W.DWORD()
        require(self.u.GetUserObjectInformationW(handle, 2, buf, C.sizeof(buf), C.byref(size)))
        return buf.value

    def private(self, station):
        name = self.name(station)
        require(name and name.casefold() != "winsta0")
        flags, size = UserFlags(), W.DWORD()
        require(self.u.GetUserObjectInformationW(station, 1, C.byref(flags), C.sizeof(flags), C.byref(size)))
        require(not flags.flags & 1)  # WSF_VISIBLE
        return name

    def owner_pid(self, hwnd, pid):
        actual = W.DWORD()
        require(self.u.GetWindowThreadProcessId(hwnd, C.byref(actual)))
        require(actual.value == pid)

    def set_sample(self, sample):
        memory = require(self.k.GlobalAlloc(0x42, len(sample)))  # MOVEABLE | ZEROINIT
        transferred = False
        try:
            ptr = require(self.k.GlobalLock(memory))
            try:
                C.memmove(ptr, sample, len(sample))
            finally:
                C.set_last_error(0)
                result = self.k.GlobalUnlock(memory)
                require(result or C.get_last_error() == 0)
            require(self.u.SetClipboardData(1, memory))  # CF_TEXT; fixed ASCII only
            transferred = True  # Clipboard, not this process, now frees the allocation.
        finally:
            if not transferred:
                require(not self.k.GlobalFree(memory))

    def check_sample(self, sample):
        memory = require(self.u.GetClipboardData(1))
        require(self.k.GlobalSize(memory) >= len(sample))
        ptr = require(self.k.GlobalLock(memory))
        try:
            require(C.string_at(ptr, len(sample)) == sample)
        finally:
            C.set_last_error(0)
            result = self.k.GlobalUnlock(memory)
            require(result or C.get_last_error() == 0)

    def transaction(self, hwnd, pid, sample, clear=False, fresh=False):
        self.owner_pid(hwnd, pid)
        require(self.u.OpenClipboard(hwnd))
        try:
            if fresh:
                C.set_last_error(0)
                require(self.u.CountClipboardFormats() == 0 and C.get_last_error() == 0)
                require(not self.u.GetClipboardOwner())
            if clear:
                require(self.u.EmptyClipboard())
                require(self.u.GetClipboardOwner() == hwnd)
            else:
                require(self.u.GetClipboardOwner() == hwnd)
            self.set_sample(sample)
            self.check_sample(sample)
            require(self.u.GetClipboardOwner() == hwnd)
        finally:
            require(self.u.CloseClipboard())


class Surface:
    """One process/thread, before any HWND: private binding and reverse-order cleanup."""
    def __init__(self, n, sid, station_name=None, desktop_name=None):
        self.n, self.sid = n, sid
        self.station_name, self.desktop_name = station_name, desktop_name
        self.station = self.desktop = self.hwnd = self.sd = None
        self.original_station = n.u.GetProcessWindowStation()
        self.original_desktop = n.u.GetThreadDesktop(n.k.GetCurrentThreadId())
        require(self.original_station and self.original_desktop)

    def open(self):
        n = self.n
        try:
            if self.station_name is None:
                sd = C.c_void_p()
                # Protected owner-only DACL applies before either kernel object exists.
                require(n.a.ConvertStringSecurityDescriptorToSecurityDescriptorW(
                    f"O:{self.sid}D:P(A;;GA;;;{self.sid})", 1, C.byref(sd), None))
                self.sd = sd
                sa = SA(C.sizeof(SA), sd, False)
                self.station = n.u.CreateWindowStationW(None, 1, 0xF037F, C.byref(sa))
                if not self.station:
                    raise CreationBlocked()
            else:
                self.station = require(n.u.OpenWindowStationW(self.station_name, False, 0xF037F))
            name = n.private(self.station)
            require(self.station_name is None or name == self.station_name)
            self.station_name = name
            require(n.u.SetProcessWindowStation(self.station))
            if self.desktop_name is None:
                self.desktop_name = "probe_" + uuid.uuid4().hex
                self.desktop = require(n.u.CreateDesktopW(self.desktop_name, None, None, 0, 0xF01FF, C.byref(sa)))
            else:
                self.desktop = require(n.u.OpenDesktopW(self.desktop_name, 0, False, 0xF01FF))
            require(n.name(self.desktop) == self.desktop_name)
            require(n.u.SetThreadDesktop(self.desktop))
            # Built-in class, HWND_MESSAGE only; no visible top-level window, activation or input.
            self.hwnd = require(n.u.CreateWindowExW(0, "STATIC", None, 0, 0, 0, 0, 0,
                                                   W.HWND(-3), None, None, None))
            n.owner_pid(self.hwnd, os.getpid())
            return self
        except BaseException:
            self.close()
            raise

    def close(self):
        n = self.n
        try:
            # On restoration failure leave bound objects for owned-process teardown.
            if self.hwnd:
                require(n.u.DestroyWindow(self.hwnd))
                self.hwnd = None
            require(n.u.SetProcessWindowStation(self.original_station))
            require(n.u.SetThreadDesktop(self.original_desktop))
            if self.desktop:
                require(n.u.CloseDesktop(self.desktop))
                self.desktop = None
            if self.station:
                require(n.u.CloseWindowStation(self.station))
                self.station = None
        finally:
            if self.sd:
                n.k.LocalFree(self.sd)
                self.sd = None

def spawn(role):
    # No shell, caller-controlled flags, executable search or stdout/stderr disk logs.
    return subprocess.Popen([str(Path(sys.executable).resolve()), "-I", "-B", str(SCRIPT),
                             "--authorized-private-station", "--role", role],
                            stdin=subprocess.PIPE, stdout=subprocess.PIPE,
                            stderr=subprocess.DEVNULL, text=True, encoding="ascii",
                            close_fds=True, creationflags=0x08000000)


def config_for(n, run):
    return {"run": run, "script": digest(SCRIPT), "python": digest(sys.executable),
            "parent": os.getpid(), "sid": n.sid()}


def authenticate(n, config):
    validate_identity(config, os.getppid(), n.parent_path())
    require(config["sid"] == n.sid())


def stop_child(child, channel=None):
    if child.poll() is None:
        child.kill()  # Only the Popen-owned direct child; never a process-name/global kill.
    child.wait(timeout=TIMEOUT)
    try:
        child.stdin.close()
    except OSError:
        pass
    if channel is not None:
        channel.thread.join(timeout=TIMEOUT)
        require(not channel.thread.is_alive())
    child.stdout.close()


def worker(n, incoming):
    message = incoming.get()
    require(set(message) == {"identity", "station", "desktop", "hwnd"})
    config = message["identity"]
    authenticate(n, config)
    require(isinstance(message["station"], str) and message["station"].casefold() != "winsta0")
    require(message["desktop"].startswith("probe_") and len(message["desktop"]) == 38)
    require(type(message["hwnd"]) is int and message["hwnd"] > 0)
    surface = Surface(n, config["sid"], message["station"], message["desktop"]).open()
    try:
        n.owner_pid(message["hwnd"], config["parent"])
        send(sys.stdout, {"state": "ready", "hwnd": surface.hwnd, "run": config["run"]})
        require(incoming.get() == {"command": "cross"})
        n.transaction(message["hwnd"], config["parent"], SAMPLES[1], clear=True)
        send(sys.stdout, {"state": "cleared"})
        require(incoming.get() == {"command": "write"})
        n.transaction(message["hwnd"], config["parent"], SAMPLES[2])
        send(sys.stdout, {"state": "written"})
        require(incoming.get() == {"command": "exit"})
    finally:
        surface.close()
    return 0


def guardian(n, incoming):
    config = incoming.get()  # Launcher assigns kill-on-close job BEFORE releasing this gate.
    authenticate(n, config)
    try:
        surface = Surface(n, config["sid"]).open()
    except CreationBlocked:
        send(sys.stdout, {"run": config["run"], "receipts": [
            receipt("blocked_createwindowstation_null_create_only")]})
        return 3
    child = channel = None
    try:
        n.transaction(surface.hwnd, os.getpid(), SAMPLES[0], clear=True, fresh=True)
        child = spawn("worker")
        channel = Channel(child.stdout)
        send(child.stdin, {"identity": config_for(n, config["run"]),
                           "station": surface.station_name, "desktop": surface.desktop_name,
                           "hwnd": surface.hwnd})
        ready = channel.get(n.pump)
        require(set(ready) == {"state", "hwnd", "run"} and ready["state"] == "ready")
        require(ready["run"] == config["run"])
        require(type(ready["hwnd"]) is int)
        n.owner_pid(ready["hwnd"], child.pid)
        send(child.stdin, {"command": "cross"})
        require(channel.get(n.pump) == {"state": "cleared"})
        require(n.u.GetClipboardOwner() == surface.hwnd)
        require(n.u.OpenClipboard(surface.hwnd))
        try:
            n.check_sample(SAMPLES[1])
        finally:
            require(n.u.CloseClipboard())
        sequence = require(n.u.GetClipboardSequenceNumber())
        send(child.stdin, {"command": "write"})
        require(channel.get(n.pump) == {"state": "written"})
        require(n.u.GetClipboardSequenceNumber() != sequence)
        require(n.u.GetClipboardOwner() == surface.hwnd)
        require(n.u.OpenClipboard(surface.hwnd))
        try:
            n.check_sample(SAMPLES[2])
        finally:
            require(n.u.CloseClipboard())
        send(child.stdin, {"command": "exit"})
        require(child.wait(timeout=TIMEOUT) == 0)
    finally:
        try:
            if child is not None:
                stop_child(child, channel)  # Reap and join reader before closing surfaces.
        finally:
            surface.close()
    send(sys.stdout, {"run": config["run"], "receipts": [
        receipt("cross_process_guardian_owner"), receipt("same_owner_external_write"),
        receipt("not_run_abort_after_clear"), receipt("not_run_foreign_owner_refusal")]})
    return 0


def drain_job(n, job):
    require(n.k.TerminateJobObject(job, 1))
    deadline = time.monotonic() + TIMEOUT
    while True:
        info = Accounting()
        require(n.k.QueryInformationJobObject(job, 1, C.byref(info), C.sizeof(info), None))
        if info.active == 0:
            return
        require(time.monotonic() < deadline)
        time.sleep(0.025)


def launcher(n):
    # The normal/General clipboard is NEVER opened here: sequence number only.
    require(n.name(n.u.GetProcessWindowStation()).casefold() == "winsta0")
    before = require(n.u.GetClipboardSequenceNumber())
    job = require(n.k.CreateJobObjectW(None, None))
    child = None
    assigned = False
    rows, code = [], 1
    try:
        limits = ExtendedLimits()
        limits.basic.flags = 0x2000  # JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE
        require(n.k.SetInformationJobObject(job, 9, C.byref(limits), C.sizeof(limits)))
        child = spawn("guardian")
        require(n.k.AssignProcessToJobObject(job, W.HANDLE(int(child._handle))))
        assigned = True
        config = config_for(n, str(uuid.uuid4()))
        send(child.stdin, config)
        output, _ = child.communicate(timeout=TIMEOUT * 4)
        require(len(output) <= 4096)
        result = json.loads(output)
        require(set(result) == {"run", "receipts"} and result["run"] == config["run"])
        rows = valid_receipts(result["receipts"])
        code = child.returncode
        require(code in (0, 3))
        expected = ({"cross_process_guardian_owner", "same_owner_external_write",
                     "not_run_abort_after_clear", "not_run_foreign_owner_refusal"} if code == 0
                    else {"blocked_createwindowstation_null_create_only"})
        require({row["receipt"] for row in rows} == expected)
    finally:
        try:
            # An assignment failure leaves a bootstrap-only child OUTSIDE the job.
            if child is not None and not assigned:
                stop_child(child)
            drain_job(n, job)  # Whole owned tree dead, including worker on guardian failure.
            if child is not None and assigned:
                stop_child(child)
        finally:
            try:
                require(n.k.CloseHandle(job))
            finally:
                # Also check on failure, without ever opening/reading the General clipboard.
                after = n.u.GetClipboardSequenceNumber()
                require(after == before)  # Concurrent user changes are inconclusive failures.
    rows.append(receipt("general_sequence_unchanged"))
    for row in valid_receipts(rows):
        print(json.dumps(row, separators=(",", ":")))
    return code


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--authorized-private-station", action="store_true")
    parser.add_argument("--role", choices=("launcher", "guardian", "worker"), default="launcher")
    args = parser.parse_args(argv)
    if not REVIEWED or not args.authorized_private_station or sys.platform != "win32":
        return 2  # No DLL loads, processes, files or clipboard access before all gates pass.
    try:
        n = Native()
        n.sid()  # Validated current SID and unelevated token before child creation or GUI work.
        if args.role == "launcher":
            return launcher(n)
        incoming = Channel(sys.stdin)
        return guardian(n, incoming) if args.role == "guardian" else worker(n, incoming)
    except BaseException:
        # No native error strings, paths, window titles, clipboard bytes or tracebacks.
        if args.role == "launcher":
            print(json.dumps(receipt("probe_failed"), separators=(",", ":")))
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
