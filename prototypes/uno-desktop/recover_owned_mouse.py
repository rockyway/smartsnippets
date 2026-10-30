"""Replay numeric cursor/owned-topmost state after the preparation worker exits.

Never clicks, releases buttons, types, touches clipboard, or changes other apps.
Held input or changed cursor ownership refuses/skips restoration, not overwrites.
"""
import argparse
import ctypes
import json
import os
from pathlib import Path
import sys
import tempfile
from owned_mouse_focus import require_input_desktop
from prepare_owned_focus import validate_image, query_windows_process


def read_journal(path, pid):
    path = Path(path)
    parent = path.parent.resolve(strict=True)
    if path.name != "focus.json" or parent.parent != Path(tempfile.gettempdir()).resolve() or not parent.name.startswith("ss-uno018-ui-") or path.is_symlink():
        raise ValueError("Private recovery journal required")
    path = parent / "focus.json"
    if path.stat().st_size > 4096:
        raise ValueError("Invalid recovery journal")
    data = json.loads(path.read_text(encoding="utf-8"))
    if not isinstance(data, dict) or set(data) != {"pid", "window", "cursor", "point", "wasTopmost"}:
        raise ValueError("Invalid recovery journal")
    if type(data["pid"]) is not int or data["pid"] != pid or type(data["window"]) is not int or not 0 < data["window"] < 2**64 or type(data["wasTopmost"]) is not bool:
        raise ValueError("Recovery identity mismatch")
    for key in ("cursor", "point"):
        if type(data[key]) is not list or len(data[key]) != 2 or any(type(value) is not int or not -(2**31) <= value < 2**31 for value in data[key]):
            raise ValueError("Invalid recovery coordinate")
    return data


def recover(path, pid, entry, digest):
    from ctypes import wintypes
    entry = validate_image(Path(entry), digest)
    data = read_journal(path, pid)
    class Point(ctypes.Structure):
        _fields_ = [("x", wintypes.LONG), ("y", wintypes.LONG)]
    user = ctypes.WinDLL("user32", use_last_error=True)
    kernel = ctypes.WinDLL("kernel32", use_last_error=True)
    user.GetWindowThreadProcessId.argtypes = [wintypes.HWND, ctypes.POINTER(wintypes.DWORD)]
    user.GetWindowThreadProcessId.restype = wintypes.DWORD
    user.GetCursorPos.argtypes = [ctypes.POINTER(Point)]
    user.GetCursorPos.restype = wintypes.BOOL
    user.SetCursorPos.argtypes = [ctypes.c_int, ctypes.c_int]
    user.SetCursorPos.restype = wintypes.BOOL
    user.GetAsyncKeyState.argtypes = [ctypes.c_int]
    user.GetAsyncKeyState.restype = ctypes.c_short
    user.SetWindowPos.argtypes = [wintypes.HWND, wintypes.HWND, ctypes.c_int, ctypes.c_int, ctypes.c_int, ctypes.c_int, wintypes.UINT]
    user.SetWindowPos.restype = wintypes.BOOL
    user.SetThreadDpiAwarenessContext.argtypes = [ctypes.c_void_p]
    user.SetThreadDpiAwarenessContext.restype = ctypes.c_void_p
    owner = wintypes.DWORD()
    owned = bool(user.GetWindowThreadProcessId(data["window"], ctypes.byref(owner)) and owner.value == pid)
    if owned:
        actual_parent, actual_image = query_windows_process(pid)
        if actual_parent != os.getppid() or actual_image != entry:
            raise ValueError("Recovery process not spawned by caller")
    old_dpi = user.SetThreadDpiAwarenessContext(ctypes.c_void_p(-4))
    if not old_dpi:
        raise ValueError("Recovery coordinate context unavailable")
    try:
        try:
            require_input_desktop(user, kernel)
            current = Point()
            if not user.GetCursorPos(ctypes.byref(current)):
                raise ValueError("Cursor recovery state unavailable")
            if [current.x, current.y] == data["point"]:
                if any(user.GetAsyncKeyState(key) & 0x8000 for key in (1, 2, 4, 5, 6, 0x10, 0x11, 0x12, 0x5B, 0x5C)):
                    raise ValueError("Held input; recovery journal retained")
                if not user.SetCursorPos(*data["cursor"]) or not user.GetCursorPos(ctypes.byref(current)) or [current.x, current.y] != data["cursor"]:
                    raise ValueError("Cursor restoration incomplete")
        finally:
            if owned and not data["wasTopmost"]:
                latest = wintypes.DWORD()
                if user.GetWindowThreadProcessId(data["window"], ctypes.byref(latest)) and latest.value == pid:
                    if not user.SetWindowPos(data["window"], ctypes.c_void_p(-2), 0, 0, 0, 0, 0x13):
                        raise ValueError("Owned topmost restoration incomplete")
    finally:
        if not user.SetThreadDpiAwarenessContext(old_dpi):
            raise ValueError("Recovery thread context restoration incomplete")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--journal", type=Path, required=True)
    parser.add_argument("--pid", type=int, required=True)
    parser.add_argument("--executable", type=Path, required=True)
    parser.add_argument("--sha256", required=True)
    args = parser.parse_args()
    if sys.platform != "win32" or not 0 < args.pid <= 2147483647:
        raise ValueError("Windows owned-process recovery required")
    recover(args.journal, args.pid, args.executable, args.sha256)
    print("owned_mouse_recovery_complete")
    return 0


if __name__ == "__main__":
    try:
        sys.exit(main())
    except Exception as error:
        print("owned_mouse_recovery_failed kind=" + type(error).__name__, file=sys.stderr)
        sys.exit(1)
