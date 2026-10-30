"""Explicitly authorized bounded GUI startup only; no clipboard, hotkeys or input.

Observes a visible top-level window owned by the exact started PID. Never reads
window titles or screen pixels. Stops only the subprocess handle it created.
"""
import argparse
import csv
import ctypes
import hashlib
import io
import plistlib
import shutil
from pathlib import Path
import subprocess
import sys
import tempfile
import time
from verify_evidence import inspect_executable

BASE = Path(__file__).resolve().parent
LIFECYCLE_RECEIPTS = [
    "ui_lifecycle_started",
    "ui_main_compiled_bindings_ok",
    "probe_data_seed_ok migration=ok crud=ok import_export=ok failure_paths=ok",
    "probe_data_restart_ok persistence=ok privacy_fixture=ok cleanup=ok",
    "ui_data_serialized_ok",
    "ui_popup_compiled_rows_ok",
    "ui_popup_hidden",
    "ui_popup_reopened",
    "ui_popup_recreated",
    "ui_lifecycle_complete managed_closed=3 native_popup_absence=2 store_thread=UI input=NONE",
]


def lifecycle_completed(stdout, stderr, code):
    return code == 0 and not stderr.strip() and stdout.splitlines() == LIFECYCLE_RECEIPTS


def visible_windows(pid):
    if sys.platform == "win32":
        from ctypes import wintypes
        user = ctypes.WinDLL("user32", use_last_error=True)
        callback_type = ctypes.WINFUNCTYPE(wintypes.BOOL, wintypes.HWND, wintypes.LPARAM)
        user.EnumWindows.argtypes = [callback_type, wintypes.LPARAM]
        user.EnumWindows.restype = wintypes.BOOL
        user.IsWindowVisible.argtypes = [wintypes.HWND]
        user.IsWindowVisible.restype = wintypes.BOOL
        user.GetWindowThreadProcessId.argtypes = [wintypes.HWND, ctypes.POINTER(wintypes.DWORD)]
        user.GetWindowThreadProcessId.restype = wintypes.DWORD
        count = 0
        def visit(hwnd, parameter):
            nonlocal count
            owner = wintypes.DWORD()
            valid = user.GetWindowThreadProcessId(hwnd, ctypes.byref(owner))
            if valid and owner.value == pid and user.IsWindowVisible(hwnd):
                count += 1
            return True
        callback = callback_type(visit)
        if not user.EnumWindows(callback, 0):
            raise ValueError("Window enumeration failed")
        return count
    if sys.platform != "darwin":
        raise ValueError("Unsupported UI host")
    cf = ctypes.CDLL("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")
    cg = ctypes.CDLL("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")
    pointer = ctypes.c_void_p
    cf.CFStringCreateWithCString.argtypes = [pointer, ctypes.c_char_p, ctypes.c_uint32]
    cf.CFStringCreateWithCString.restype = pointer
    cf.CFArrayGetCount.argtypes = [pointer]
    cf.CFArrayGetCount.restype = ctypes.c_long
    cf.CFArrayGetValueAtIndex.argtypes = [pointer, ctypes.c_long]
    cf.CFArrayGetValueAtIndex.restype = pointer
    cf.CFDictionaryGetValue.argtypes = [pointer, pointer]
    cf.CFDictionaryGetValue.restype = pointer
    cf.CFNumberGetValue.argtypes = [pointer, ctypes.c_long, pointer]
    cf.CFNumberGetValue.restype = ctypes.c_bool
    cf.CFRelease.argtypes = [pointer]
    cf.CFRelease.restype = None
    cg.CGWindowListCopyWindowInfo.argtypes = [ctypes.c_uint32, ctypes.c_uint32]
    cg.CGWindowListCopyWindowInfo.restype = pointer
    keys = [cf.CFStringCreateWithCString(None, name, 0x08000100)
            for name in (b"kCGWindowOwnerPID", b"kCGWindowLayer")]
    windows = None
    try:
        windows = cg.CGWindowListCopyWindowInfo(1 | 16, 0)
        if not windows or not all(keys):
            raise ValueError("Window enumeration unavailable")
        count = 0
        for index in range(cf.CFArrayGetCount(windows)):
            item = cf.CFArrayGetValueAtIndex(windows, index)
            values = []
            for key in keys:
                number = cf.CFDictionaryGetValue(item, key)
                value = ctypes.c_int64()
                valid = number and cf.CFNumberGetValue(number, 4, ctypes.byref(value))
                values.append(value.value if valid else None)
            if values == [pid, 0]:
                count += 1
        return count
    finally:
        if windows:
            cf.CFRelease(windows)
        for key in keys:
            if key:
                cf.CFRelease(key)


def private_directory():
    root = Path(tempfile.mkdtemp(prefix="ss-uno018-ui-"))
    try:
        if sys.platform == "win32":
            identity = subprocess.run(["whoami", "/user", "/fo", "csv", "/nh"],
                                      capture_output=True, text=True, check=True, timeout=10,
                                      creationflags=subprocess.CREATE_NO_WINDOW)
            sid = next(csv.reader(io.StringIO(identity.stdout)))[1]
            if not sid.startswith("S-1-"):
                raise ValueError("Cannot isolate UI evidence")
            subprocess.run(["icacls", str(root), "/inheritance:r", "/grant:r", "*" + sid + ":(OI)(CI)F"],
                           capture_output=True, check=True, timeout=10,
                           creationflags=subprocess.CREATE_NO_WINDOW)
        else:
            root.chmod(0o700)
        return root
    except BaseException:
        shutil.rmtree(root, ignore_errors=True)
        raise


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--executable", type=Path, required=True)
    parser.add_argument("--sha256", required=True)
    parser.add_argument("--authorized-screen-slot", action="store_true")
    parser.add_argument("--lifecycle", action="store_true", help="Require real-host lifecycle receipts and graceful exit")
    args = parser.parse_args()
    if not args.authorized_screen_slot or sys.platform not in ("win32", "darwin"):
        raise ValueError("An authorized Windows or macOS screen slot is required")
    entry = args.executable.resolve(strict=True)
    expected = "SmartSnippets.UnoProbe.exe" if sys.platform == "win32" else "SmartSnippets.UnoProbe"
    rid = "win-x64" if sys.platform == "win32" else "osx-arm64"
    if not entry.is_file() or not entry.is_relative_to((BASE / "bin" / "probe").resolve()) or entry.name != expected:
        raise ValueError("Use an owned probe output")
    if sys.platform == "darwin":
        if (entry.parent.name != "MacOS" or entry.parents[1].name != "Contents" or
                entry.parents[2].name != "SmartSnippets.UnoProbe.app"):
            raise ValueError("Use the isolated macOS app bundle")
        with (entry.parents[1] / "Info.plist").open("rb") as stream:
            info = plistlib.load(stream)
        if info.get("CFBundleIdentifier") != "org.smartsnippets.feasibility018" or info.get("CFBundleExecutable") != expected:
            raise ValueError("Wrong macOS bundle identity")
    data = entry.read_bytes()
    if hashlib.sha256(data).hexdigest() != args.sha256.lower() or inspect_executable(data, rid):
        raise ValueError("Artifact identity check failed")
    private = private_directory()
    process = None
    observed = False
    code = None
    with (private / "stdout.log").open("wb") as stdout, (private / "stderr.log").open("wb") as stderr:
        try:
            arguments = [str(entry), "--authorized-probe-ui"]
            if args.lifecycle:
                arguments.append("--probe-ui-lifecycle")
            process = subprocess.Popen(arguments, stdout=stdout, stderr=stderr,
                                       cwd=entry.parent, stdin=subprocess.DEVNULL)
            deadline = time.monotonic() + (30 if args.lifecycle else 15)
            while time.monotonic() < deadline:
                code = process.poll()
                if code is not None:
                    break
                # Native enumeration occurs in a disposable helper so a hung OS
                # call cannot bypass the owner's bounded process cleanup.
                try:
                    helper = subprocess.run([sys.executable, str(Path(__file__).resolve()),
                                             "--window-count", str(process.pid)],
                                            capture_output=True, text=True, timeout=3, check=True,
                                            creationflags=subprocess.CREATE_NO_WINDOW if sys.platform == "win32" else 0)
                except subprocess.TimeoutExpired:
                    break
                if not helper.stdout.strip().isdigit() or helper.stderr.strip():
                    raise ValueError("Window observer failed")
                if int(helper.stdout.strip()) > 0:
                    observed = True
                    if args.lifecycle:
                        try:
                            code = process.wait(timeout=max(0.0, deadline - time.monotonic()))
                        except subprocess.TimeoutExpired:
                            code = process.poll()
                    break
                time.sleep(0.1)
            if code is None:
                code = process.poll()
        finally:
            if process is not None:
                if process.poll() is None:
                    process.terminate()
                try:
                    process.wait(timeout=3)
                except subprocess.TimeoutExpired:
                    process.kill()
                    process.wait(timeout=3)
                except BaseException:
                    if process.poll() is None:
                        process.kill()
                        process.wait(timeout=3)
                    raise
    print("ui_smoke_window=" + ("OBSERVED" if observed else "NOT_OBSERVED") +
          " owned_process_stopped=true clipboard=UNTOUCHED input=NONE")
    print("private_evidence_token=" + private.name)
    # Keep exception payloads and OS diagnostics private, including native crash logs.
    if not observed:
        print("ui_smoke_early_exit=" + (str(code) if code is not None else "timeout"))
    if args.lifecycle:
        completed = observed and lifecycle_completed(
            (private / "stdout.log").read_text(errors="replace"),
            (private / "stderr.log").read_text(errors="replace"), code)
        print("ui_lifecycle_result=" + ("PASS" if completed else "FAIL") +
              " graceful_exit=" + str(bool(completed)).lower())
        return 0 if completed else 1
    return 0 if observed else 1


if __name__ == "__main__":
    try:
        if len(sys.argv) == 3 and sys.argv[1] == "--window-count" and sys.argv[2].isdigit():
            print(visible_windows(int(sys.argv[2])))
            sys.exit(0)
        sys.exit(main())
    except (OSError, ValueError, subprocess.SubprocessError):
        print("UI smoke failed; inspect private evidence. No input was requested.", file=sys.stderr)
        sys.exit(1)
