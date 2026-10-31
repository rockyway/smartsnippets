"""Authorized Windows pointer-only companion scene. Never sends buttons or keys.

Runs only as a bounded child of run_ui_smoke. The recovery journal records both
cursor candidates durably. The owner stops its
exact child before replaying pointer recovery on every still-alive path.
"""
import argparse
import base64
import re
import ctypes
from ctypes import wintypes
import json
import os
from pathlib import Path
import subprocess
import time

from owned_mouse_focus import require_input_desktop
from prepare_owned_focus import query_windows_process, validate_image


def wait_line(log, prefix, deadline):
    window_receipt = prefix in ("ui_companion_pointer_ready", "ui_companion_pointer_opened")
    pattern = re.escape(prefix) + (r" hwnd=[1-9][0-9]*" if window_receipt else "")
    while time.monotonic() < deadline:
        if log.with_name("stderr.log").read_text(errors="replace").strip():
            raise ValueError("Native pointer stderr failure")
        lines = log.read_text(errors="replace").splitlines()
        candidates = [line for line in lines if line.startswith(prefix)]
        if len(candidates) > 1 or (candidates and re.fullmatch(pattern, candidates[0]) is None):
            raise ValueError("Native pointer receipt failed")
        if candidates:
            return candidates[0]
        time.sleep(.002)
    raise ValueError("Native pointer receipt timeout")


def control_point(pid, hwnd, automation_id):
    # Fixed allowlist only; never query another process or use UIA invocation.
    if automation_id not in ("OwnedCompanionRow", "OwnedCompanionSurface", "OwnedCompanionLeave"):
        raise ValueError("Unapproved pointer control")
    script = f"""
$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'
[void][System.Reflection.Assembly]::Load('UIAutomationClient, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35')
[void][System.Reflection.Assembly]::Load('UIAutomationTypes, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35')
$root = [System.Windows.Automation.AutomationElement]::FromHandle([IntPtr]::new({hwnd}))
if ($root.Current.ProcessId -ne {pid}) {{ throw 'owner' }}
$c = [System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::AutomationIdProperty, '{automation_id}')
$f = $root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $c)
if ($null -eq $f -or $f.Current.ProcessId -ne {pid} -or $f.Current.IsOffscreen -or -not $f.Current.IsEnabled) {{ throw 'control' }}
$b = $f.Current.BoundingRectangle
$r = $root.Current.BoundingRectangle
if ($b.IsEmpty -or $b.Width -lt 8 -or $b.Height -lt 8 -or $b.Left -lt $r.Left -or $b.Right -gt $r.Right -or $b.Top -lt $r.Top -or $b.Bottom -gt $r.Bottom) {{ throw 'bounds' }}
@(($b.Left + $b.Width / 2 - $r.Left) / $r.Width, ($b.Top + $b.Height / 2 - $r.Top) / $r.Height) | ConvertTo-Json -Compress
"""
    powershell = Path(os.environ.get("SystemRoot", r"C:\Windows")) / "System32/WindowsPowerShell/v1.0/powershell.exe"
    result = subprocess.run([str(powershell), "-NoProfile", "-NonInteractive", "-STA", "-OutputFormat", "Text", "-EncodedCommand",
                             base64.b64encode(script.encode("utf-16le")).decode("ascii")],
                            stdin=subprocess.DEVNULL, capture_output=True, text=True, timeout=7,
                            creationflags=subprocess.CREATE_NO_WINDOW)
    if result.returncode or result.stderr.strip():
        raise ValueError("Owned UIA bounds unavailable:" + automation_id)
    point = json.loads(result.stdout)
    if type(point) is not list or len(point) != 2 or not all(type(v) in (int, float) and 0 < v < 1 for v in point):
        raise ValueError("Invalid UIA point")
    return point


def run(pid, entry, digest, journal, log, main_hwnd):
    entry = validate_image(entry, digest)
    parent, image = query_windows_process(pid)
    if parent != os.getppid() or image.resolve() != entry:
        raise ValueError("Owned pointer process mismatch")
    from recover_owned_mouse import private_journal_path, read_journal
    journal = private_journal_path(journal)
    if log.absolute() != journal.parent / "stdout.log":
        raise ValueError("Private pointer log required")
    for path in (log, log.with_name("stderr.log"), journal.with_name("focus-next.json")):
        if path.is_symlink() or getattr(path, "is_junction", lambda: False)():
            raise ValueError("Linked pointer file refused")
    user = ctypes.WinDLL("user32", use_last_error=True)
    kernel = ctypes.WinDLL("kernel32", use_last_error=True)
    class Point(ctypes.Structure):
        _fields_ = [("x", wintypes.LONG), ("y", wintypes.LONG)]
    class Rect(ctypes.Structure):
        _fields_ = [(n, wintypes.LONG) for n in ("left", "top", "right", "bottom")]
    class Last(ctypes.Structure):
        _fields_ = [("size", wintypes.UINT), ("tick", wintypes.DWORD)]
    for name, args, result in [
        ("GetWindowThreadProcessId", [wintypes.HWND, ctypes.POINTER(wintypes.DWORD)], wintypes.DWORD),
        ("GetWindowRect", [wintypes.HWND, ctypes.POINTER(Rect)], wintypes.BOOL),
        ("GetClientRect", [wintypes.HWND, ctypes.POINTER(Rect)], wintypes.BOOL),
        ("ClientToScreen", [wintypes.HWND, ctypes.POINTER(Point)], wintypes.BOOL),
        ("GetCursorPos", [ctypes.POINTER(Point)], wintypes.BOOL),
        ("SetCursorPos", [ctypes.c_int, ctypes.c_int], wintypes.BOOL),
        ("GetAsyncKeyState", [ctypes.c_int], ctypes.c_short),
        ("WindowFromPoint", [Point], wintypes.HWND),
        ("GetAncestor", [wintypes.HWND, wintypes.UINT], wintypes.HWND),
        ("GetLastInputInfo", [ctypes.POINTER(Last)], wintypes.BOOL),
        ("GetWindowLongPtrW", [wintypes.HWND, ctypes.c_int], ctypes.c_ssize_t),
        ("SetThreadDpiAwarenessContext", [ctypes.c_void_p], ctypes.c_void_p),
    ]:
        function = getattr(user, name)
        function.argtypes, function.restype = args, result
    kernel.GetTickCount64.restype = ctypes.c_ulonglong
    held = lambda: any(user.GetAsyncKeyState(k) & 0x8000 for k in range(1, 255))
    def owner(hwnd):
        value = wintypes.DWORD()
        return bool(user.GetWindowThreadProcessId(hwnd, ctypes.byref(value))) and value.value == pid
    def cursor():
        p = Point()
        if not user.GetCursorPos(ctypes.byref(p)):
            raise ValueError("Cursor unavailable")
        return [p.x, p.y]
    require_input_desktop(user, kernel)
    last = Last(ctypes.sizeof(Last), 0)
    if held() or not user.GetLastInputInfo(ctypes.byref(last)) or (((kernel.GetTickCount64() & 0xffffffff) - last.tick) & 0xffffffff) < 1500:
        raise ValueError("Exclusive idle input required")
    baseline = last.tick
    old_dpi = user.SetThreadDpiAwarenessContext(ctypes.c_void_p(-4))
    if not old_dpi:
        raise ValueError("PMv2 unavailable")
    try:
        original = expected = cursor()
        if type(main_hwnd) is not int or not 0 < main_hwnd < 2**64 or not owner(main_hwnd):
            raise ValueError("Main ownership changed")
        if owner(user.WindowFromPoint(Point(*original))):
            raise ValueError("Original cursor already over owned probe")
        ctypes.set_last_error(0)
        style = user.GetWindowLongPtrW(main_hwnd, -20)
        if not style and ctypes.get_last_error():
            raise ValueError("Owned style unavailable")
        saved = {"pid": pid, "window": main_hwnd, "cursor": original, "point": original,
                 "previous": original, "wasTopmost": bool(style & 8), "version": 2,
                 "kind": "pointer-only", "zorderChanged": False, "inputTick": baseline}
        def persist(previous, planned, tick):
            saved.update(previous=previous, point=planned, inputTick=tick)
            pending = journal.with_name("focus-next.json")
            with pending.open("x", encoding="utf-8") as stream:
                json.dump(saved, stream)
                stream.flush()
                os.fsync(stream.fileno())
            os.replace(pending, journal)
            read_journal(journal, pid)
        def move(hwnd, fraction):
            nonlocal expected
            require_input_desktop(user, kernel)
            if held() or cursor() != expected or not owner(hwnd):
                raise ValueError("Pointer ownership changed")
            current = Last(ctypes.sizeof(Last), 0)
            if not user.GetLastInputInfo(ctypes.byref(current)) or current.tick != baseline:
                raise ValueError("Operator input changed")
            rect, client = Rect(), Rect()
            if not user.GetWindowRect(hwnd, ctypes.byref(rect)) or not user.GetClientRect(hwnd, ctypes.byref(client)):
                raise ValueError("Owned frame unavailable")
            x = rect.left + int(fraction[0] * (rect.right - rect.left))
            y = rect.top + int(fraction[1] * (rect.bottom - rect.top))
            a, b = Point(client.left, client.top), Point(client.right, client.bottom)
            if not user.ClientToScreen(hwnd, ctypes.byref(a)) or not user.ClientToScreen(hwnd, ctypes.byref(b)) or not (a.x + 4 <= x < b.x - 4 and a.y + 4 <= y < b.y - 4):
                raise ValueError("Nonclient pointer refused")
            leaf = user.WindowFromPoint(Point(x, y))
            if not leaf or not owner(leaf) or user.GetAncestor(leaf, 2) != hwnd:
                raise ValueError("Final owned leaf hit-test failed")
            # Both sides of SetCursorPos survive worker loss, including before
            # the final guard. Recovery accepts neither an unrelated point nor a
            # changed physical-input epoch, and never changes Z order.
            persist(expected, [x, y], baseline)
            require_input_desktop(user, kernel)
            if (held() or cursor() != expected or not owner(hwnd) or not owner(leaf)
                    or user.GetAncestor(leaf, 2) != hwnd or user.WindowFromPoint(Point(x, y)) != leaf
                    or not user.GetLastInputInfo(ctypes.byref(current)) or current.tick != baseline):
                raise ValueError("Final pointer guard changed")
            if not user.SetCursorPos(x, y) or cursor() != [x, y]:
                raise ValueError("Pointer move failed")
            expected = [x, y]
            if not user.GetLastInputInfo(ctypes.byref(current)) or current.tick != baseline:
                raise ValueError("Physical input epoch changed; recovery pending")
        deadline = time.monotonic() + 24
        row = control_point(pid, main_hwnd, "OwnedCompanionRow")
        leave = control_point(pid, main_hwnd, "OwnedCompanionLeave")
        move(main_hwnd, row)
        wait_line(log, "ui_companion_pointer_row_entered", deadline)
        opened = wait_line(log, "ui_companion_pointer_opened", deadline)
        match = re.fullmatch(r"ui_companion_pointer_opened hwnd=([1-9][0-9]*)", opened)
        if match is None:
            raise ValueError("Malformed companion receipt")
        companion = int(match[1])
        if not 0 < companion < 2**64 or companion == main_hwnd or not owner(companion):
            raise ValueError("Companion ownership mismatch")
        # Expensive UIA work finishes BEFORE starting the actual 120ms handoff.
        surface = control_point(pid, companion, "OwnedCompanionSurface")
        move(main_hwnd, leave)
        wait_line(log, "ui_companion_pointer_row_exited", deadline)
        move(companion, surface)
        wait_line(log, "ui_companion_pointer_surface_entered", deadline)
        wait_line(log, "ui_companion_pointer_held geometry=EXTERNAL foreground=UNCHANGED", deadline)
        move(main_hwnd, leave)
        wait_line(log, "ui_companion_pointer_surface_exited", deadline)
        wait_line(log, "ui_companion_pointer_hidden", deadline)
        # The native scene detached callbacks before exposing this receipt.
        require_input_desktop(user, kernel)
        current = Last(ctypes.sizeof(Last), 0)
        persist(expected, original, baseline)
        if (held() or cursor() != expected or not user.GetLastInputInfo(ctypes.byref(current))
                or current.tick != baseline):
            raise ValueError("Pointer recovery refused")
        if not user.SetCursorPos(*original) or cursor() != original:
            raise ValueError("Pointer recovery incomplete")
    finally:
        if not user.SetThreadDpiAwarenessContext(old_dpi):
            raise ValueError("PMv2 recovery incomplete")


def failure_code(error):
    # Only fixed allowlisted tokens; never echo arbitrary exception/provider text.
    return {
        "Exclusive idle input required": "INPUT_NOT_IDLE",
        "Original cursor already over owned probe": "CURSOR_OVER_PROBE",
        "Owned UIA bounds unavailable:OwnedCompanionRow": "UIA_ROW",
        "Owned UIA bounds unavailable:OwnedCompanionLeave": "UIA_LEAVE",
        "Owned UIA bounds unavailable:OwnedCompanionSurface": "UIA_SURFACE",
        "Final owned leaf hit-test failed": "OWNED_LEAF",
        "Nonclient pointer refused": "CLIENT_INSET",
        "PMv2 unavailable": "DPI_CONTEXT",
        "Operator input changed": "INPUT_CHANGED",
    }.get(str(error), "GUARD_REFUSED")


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--pid", type=int, required=True)
    parser.add_argument("--hwnd", type=int, required=True)
    parser.add_argument("--executable", type=Path, required=True)
    parser.add_argument("--sha256", required=True)
    parser.add_argument("--journal", type=Path, required=True)
    args = parser.parse_args()
    run(args.pid, args.executable, args.sha256, args.journal, args.journal.absolute().parent / "stdout.log", args.hwnd)
    print("owned_pointer_prepared")


if __name__ == "__main__":
    try:
        main()
    except (OSError, ValueError, subprocess.SubprocessError, IndexError) as error:
        raise SystemExit("Owned pointer refused; retain private journal reason=" + failure_code(error))
