"""One guarded click in an owned probe edit field, for test preparation only.

No keyboard/clipboard APIs. Requires idle input, no held buttons/modifiers,
matching PID/root HWND hit-test, and a private recovery journal before mutation.
"""
import ctypes
import json
import os
from pathlib import Path
import tempfile
import time


def require_input_desktop(user, kernel):
    from ctypes import wintypes
    user.OpenInputDesktop.argtypes = [wintypes.DWORD, wintypes.BOOL, wintypes.DWORD]
    user.OpenInputDesktop.restype = wintypes.HANDLE
    user.GetThreadDesktop.argtypes = [wintypes.DWORD]
    user.GetThreadDesktop.restype = wintypes.HANDLE
    user.GetUserObjectInformationW.argtypes = [wintypes.HANDLE, ctypes.c_int, ctypes.c_void_p, wintypes.DWORD, ctypes.POINTER(wintypes.DWORD)]
    user.GetUserObjectInformationW.restype = wintypes.BOOL
    user.CloseDesktop.argtypes = [wintypes.HANDLE]
    user.CloseDesktop.restype = wintypes.BOOL
    kernel.GetCurrentThreadId.restype = wintypes.DWORD
    active = user.OpenInputDesktop(0, False, 1) # Read-only; never switch desktops.
    if not active:
        raise ValueError("Interactive input desktop unavailable")
    try:
        current = user.GetThreadDesktop(kernel.GetCurrentThreadId())
        names = []
        for desktop in (active, current):
            name = ctypes.create_unicode_buffer(256)
            needed = wintypes.DWORD()
            if not desktop or not user.GetUserObjectInformationW(desktop, 2, name, ctypes.sizeof(name), ctypes.byref(needed)):
                raise ValueError("Input desktop identity unavailable")
            names.append(name.value)
        if names[0] != names[1]:
            raise ValueError("Refusing another input desktop")
    finally:
        user.CloseDesktop(active)


def click_owned_window(pid, hwnd, x, y, journal, *, relative=False):
    from ctypes import wintypes
    class Point(ctypes.Structure):
        _fields_ = [("x", wintypes.LONG), ("y", wintypes.LONG)]
    class Rect(ctypes.Structure):
        _fields_ = [("left", wintypes.LONG), ("top", wintypes.LONG), ("right", wintypes.LONG), ("bottom", wintypes.LONG)]
    class LastInput(ctypes.Structure):
        _fields_ = [("size", wintypes.UINT), ("tick", wintypes.DWORD)]
    class Mouse(ctypes.Structure):
        _fields_ = [("dx", wintypes.LONG), ("dy", wintypes.LONG), ("data", wintypes.DWORD),
                    ("flags", wintypes.DWORD), ("time", wintypes.DWORD), ("extra", ctypes.c_size_t)]
    class Data(ctypes.Union):
        _fields_ = [("mouse", Mouse)]
    class Input(ctypes.Structure):
        _fields_ = [("kind", wintypes.DWORD), ("data", Data)]
    if ctypes.sizeof(Input) != 40:
        raise ValueError("Expected x64 native input ABI")
    journal = Path(journal)
    parent = journal.parent.resolve(strict=True)
    if journal.name != "focus.json" or parent.parent != Path(tempfile.gettempdir()).resolve() or not parent.name.startswith("ss-uno018-ui-"):
        raise ValueError("Private recovery journal required")
    journal = parent / "focus.json"
    user = ctypes.WinDLL("user32", use_last_error=True)
    kernel = ctypes.WinDLL("kernel32", use_last_error=True)
    user.GetWindowThreadProcessId.argtypes = [wintypes.HWND, ctypes.POINTER(wintypes.DWORD)]
    user.GetWindowThreadProcessId.restype = wintypes.DWORD
    user.GetWindowRect.argtypes = [wintypes.HWND, ctypes.POINTER(Rect)]
    user.GetWindowRect.restype = wintypes.BOOL
    user.GetClientRect.argtypes = [wintypes.HWND, ctypes.POINTER(Rect)]
    user.GetClientRect.restype = wintypes.BOOL
    user.ClientToScreen.argtypes = [wintypes.HWND, ctypes.POINTER(Point)]
    user.ClientToScreen.restype = wintypes.BOOL
    user.GetSystemMetrics.argtypes = [ctypes.c_int]
    user.GetSystemMetrics.restype = ctypes.c_int
    user.GetCursorPos.argtypes = [ctypes.POINTER(Point)]
    user.GetCursorPos.restype = wintypes.BOOL
    user.SetCursorPos.argtypes = [ctypes.c_int, ctypes.c_int]
    user.SetCursorPos.restype = wintypes.BOOL
    user.WindowFromPoint.argtypes = [Point]
    user.WindowFromPoint.restype = wintypes.HWND
    user.GetAncestor.argtypes = [wintypes.HWND, wintypes.UINT]
    user.GetAncestor.restype = wintypes.HWND
    user.GetForegroundWindow.restype = wintypes.HWND
    user.GetAsyncKeyState.argtypes = [ctypes.c_int]
    user.GetAsyncKeyState.restype = ctypes.c_short
    user.GetLastInputInfo.argtypes = [ctypes.POINTER(LastInput)]
    user.GetLastInputInfo.restype = wintypes.BOOL
    kernel.GetTickCount64.restype = ctypes.c_ulonglong
    user.GetWindowLongPtrW.argtypes = [wintypes.HWND, ctypes.c_int]
    user.GetWindowLongPtrW.restype = ctypes.c_ssize_t
    user.SetWindowPos.argtypes = [wintypes.HWND, wintypes.HWND, ctypes.c_int, ctypes.c_int, ctypes.c_int, ctypes.c_int, wintypes.UINT]
    user.SetWindowPos.restype = wintypes.BOOL
    user.SetThreadDpiAwarenessContext.argtypes = [ctypes.c_void_p]
    user.SetThreadDpiAwarenessContext.restype = ctypes.c_void_p
    user.SendInput.argtypes = [wintypes.UINT, ctypes.POINTER(Input), ctypes.c_int]
    user.SendInput.restype = wintypes.UINT
    held = lambda: any(user.GetAsyncKeyState(key) & 0x8000 for key in (1, 2, 4, 5, 6, 0x10, 0x11, 0x12, 0x5B, 0x5C))
    if user.GetSystemMetrics(23) != 0:
        raise ValueError("Refusing swapped mouse-button mapping")
    owner = wintypes.DWORD()
    if not user.GetWindowThreadProcessId(hwnd, ctypes.byref(owner)) or owner.value != pid:
        raise ValueError("Owned window unavailable")
    deadline = time.monotonic() + 5
    while True:
        last = LastInput(ctypes.sizeof(LastInput), 0)
        if not user.GetLastInputInfo(ctypes.byref(last)):
            raise ValueError("Input-idle query refused")
        idle = ((kernel.GetTickCount64() & 0xFFFFFFFF) - last.tick) & 0xFFFFFFFF
        if idle >= 1500 and not held():
            break
        if time.monotonic() >= deadline:
            raise ValueError("Operator input active; refusing click")
        time.sleep(0.1)
    old_dpi = user.SetThreadDpiAwarenessContext(ctypes.c_void_p(-4))
    if not old_dpi:
        raise ValueError("Physical-coordinate context unavailable")
    original = Point()
    raised = False
    moved = False
    try:
        bounds = Rect()
        if not user.GetWindowRect(hwnd, ctypes.byref(bounds)):
            raise ValueError("Owned bounds unavailable")
        if relative:
            if not (0 < x < 1 and 0 < y < 1):
                raise ValueError("Invalid owned-field fraction")
            x = bounds.left + int(x * (bounds.right - bounds.left))
            y = bounds.top + int(y * (bounds.bottom - bounds.top))
        if not (bounds.left <= x < bounds.right and bounds.top <= y < bounds.bottom):
            import sys
            print(f"owned_focus_bounds x={x} y={y} left={bounds.left} top={bounds.top} right={bounds.right} bottom={bounds.bottom}", file=sys.stderr)
            raise ValueError("Click outside owned bounds")
        client = Rect()
        if not user.GetClientRect(hwnd, ctypes.byref(client)):
            raise ValueError("Owned client area unavailable")
        top_left, bottom_right = Point(client.left, client.top), Point(client.right, client.bottom)
        if not user.ClientToScreen(hwnd, ctypes.byref(top_left)) or not user.ClientToScreen(hwnd, ctypes.byref(bottom_right)) or not (
                top_left.x + 4 <= x < bottom_right.x - 4 and top_left.y + 4 <= y < bottom_right.y - 4):
            raise ValueError("Refusing non-client or border click")
        if not user.GetCursorPos(ctypes.byref(original)):
            raise ValueError("Cannot preserve cursor")
        ctypes.set_last_error(0)
        style = user.GetWindowLongPtrW(hwnd, -20)
        if style == 0 and ctypes.get_last_error():
            raise ValueError("Cannot preserve window state")
        was_topmost = bool(style & 8)
        with journal.open("x", encoding="utf-8") as stream:
            json.dump({"pid": pid, "window": hwnd, "cursor": [original.x, original.y],
                       "point": [x, y], "wasTopmost": was_topmost}, stream)
            stream.flush()
            os.fsync(stream.fileno())
        require_input_desktop(user, kernel)
        if not was_topmost:
            if not user.SetWindowPos(hwnd, ctypes.c_void_p(-1), 0, 0, 0, 0, 0x13):
                raise ValueError("Cannot expose owned click surface")
            raised = True
        if held() or not user.SetCursorPos(x, y):
            raise ValueError("Click state changed")
        moved = True
        require_input_desktop(user, kernel)
        events = (Input * 2)()
        events[0].data.mouse.flags = 2
        events[1].data.mouse.flags = 4
        current = Point()
        if not user.GetCursorPos(ctypes.byref(current)) or (current.x, current.y) != (x, y) or held():
            raise ValueError("Live cursor moved or was clamped; no click")
        leaf = user.WindowFromPoint(current)
        leaf_owner = wintypes.DWORD()
        if not leaf or not user.GetWindowThreadProcessId(leaf, ctypes.byref(leaf_owner)) or leaf_owner.value != pid or user.GetAncestor(leaf, 2) != hwnd or held():
            raise ValueError("Owned hit-test changed; no click")
        sent = user.SendInput(2, events, ctypes.sizeof(Input))
        if sent != 2:
            if sent == 1:
                release = Input()
                release.data.mouse.flags = 4
                if user.SendInput(1, ctypes.byref(release), ctypes.sizeof(Input)) != 1:
                    raise ValueError("Owned mouse release incomplete")
            raise ValueError("Owned click incomplete")
        until = time.monotonic() + 1
        while user.GetForegroundWindow() != hwnd:
            if time.monotonic() >= until:
                raise ValueError("Owned click did not foreground probe")
            time.sleep(0.05)
    finally:
        try:
            if moved:
                current = Point()
                if not user.GetCursorPos(ctypes.byref(current)):
                    raise ValueError("Cursor recovery state unavailable")
                if (current.x, current.y) == (x, y) and not held():
                    if not user.SetCursorPos(original.x, original.y) or not user.GetCursorPos(ctypes.byref(current)) or (current.x, current.y) != (original.x, original.y):
                        raise ValueError("Cursor restoration incomplete")
        finally:
            try:
                if raised:
                    current_owner = wintypes.DWORD()
                    if user.GetWindowThreadProcessId(hwnd, ctypes.byref(current_owner)) and current_owner.value == pid:
                        if not user.SetWindowPos(hwnd, ctypes.c_void_p(-2), 0, 0, 0, 0, 0x13):
                            raise ValueError("Owned window restoration incomplete")
            finally:
                if not user.SetThreadDpiAwarenessContext(old_dpi):
                    raise ValueError("Thread coordinate context restoration incomplete")
