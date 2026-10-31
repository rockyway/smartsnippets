"""Foreground preparation for a freshly spawned probe, never an arbitrary app.

Caller must be the target's actual parent. Revalidates image path/hash and PID
before a guarded owned-window mouse click (Windows) or AppKit activation (Mac).
UIA reads only the owned field's rectangle. No keyboard or clipboard APIs.
"""
import argparse
import base64
import ctypes
import hashlib
import os
from pathlib import Path
import re
import subprocess
import sys

BASE = Path(__file__).resolve().parent


def validate_image(entry, digest):
    entry = entry.resolve(strict=True)
    expected = "SmartSnippets.UnoProbe.exe" if sys.platform == "win32" else "SmartSnippets.UnoProbe"
    if not entry.is_relative_to((BASE / "bin" / "probe").resolve()) or entry.name != expected:
        raise ValueError("Refusing an unowned image")
    if not re.fullmatch(r"[0-9a-fA-F]{64}", digest) or hashlib.sha256(entry.read_bytes()).hexdigest() != digest.lower():
        raise ValueError("Refusing an unverified image")
    return entry


def query_windows_process(pid):
    from ctypes import wintypes
    class Entry(ctypes.Structure):
        _fields_ = [("size", wintypes.DWORD), ("usage", wintypes.DWORD), ("pid", wintypes.DWORD),
                    ("heap", ctypes.c_size_t), ("module", wintypes.DWORD), ("threads", wintypes.DWORD),
                    ("parent", wintypes.DWORD), ("priority", wintypes.LONG), ("flags", wintypes.DWORD),
                    ("image_name", wintypes.WCHAR * 260)]
    kernel = ctypes.WinDLL("kernel32", use_last_error=True)
    kernel.CreateToolhelp32Snapshot.argtypes = [wintypes.DWORD, wintypes.DWORD]
    kernel.CreateToolhelp32Snapshot.restype = wintypes.HANDLE
    kernel.Process32FirstW.argtypes = [wintypes.HANDLE, ctypes.POINTER(Entry)]
    kernel.Process32FirstW.restype = wintypes.BOOL
    kernel.Process32NextW.argtypes = [wintypes.HANDLE, ctypes.POINTER(Entry)]
    kernel.Process32NextW.restype = wintypes.BOOL
    kernel.CloseHandle.argtypes = [wintypes.HANDLE]
    kernel.CloseHandle.restype = wintypes.BOOL
    snapshot = kernel.CreateToolhelp32Snapshot(2, 0)
    if snapshot is None or snapshot == ctypes.c_void_p(-1).value:
        raise ValueError("Process ownership unavailable")
    actual_parent = None
    try:
        item = Entry()
        item.size = ctypes.sizeof(item)
        valid = kernel.Process32FirstW(snapshot, ctypes.byref(item))
        while valid:
            if item.pid == pid:
                actual_parent = item.parent
                break
            valid = kernel.Process32NextW(snapshot, ctypes.byref(item))
    finally:
        kernel.CloseHandle(snapshot)
    if actual_parent is None:
        raise ValueError("Process ownership unavailable")
    kernel.OpenProcess.argtypes = [wintypes.DWORD, wintypes.BOOL, wintypes.DWORD]
    kernel.OpenProcess.restype = wintypes.HANDLE
    kernel.QueryFullProcessImageNameW.argtypes = [wintypes.HANDLE, wintypes.DWORD, wintypes.LPWSTR, ctypes.POINTER(wintypes.DWORD)]
    kernel.QueryFullProcessImageNameW.restype = wintypes.BOOL
    handle = kernel.OpenProcess(0x1000, False, pid) # Query-only, never VM/write rights.
    if not handle:
        raise ValueError("Process image unavailable")
    try:
        size = wintypes.DWORD(4096)
        path = ctypes.create_unicode_buffer(size.value)
        if not kernel.QueryFullProcessImageNameW(handle, 0, path, ctypes.byref(size)):
            raise ValueError("Process image unavailable")
        return actual_parent, Path(path.value).resolve()
    finally:
        kernel.CloseHandle(handle)


def prepare_windows(pid, parent, entry, digest, journal, *, row_hwnd=None):
    if journal is None:
        raise ValueError("Private cursor journal required")
    try:
        actual_parent, actual_image = query_windows_process(pid)
    except (OSError, ValueError) as error:
        print("owned_focus_identity_query_failed kind=" + type(error).__name__, file=sys.stderr)
        raise
    if actual_parent != parent or actual_image != entry:
        print(f"owned_focus_identity parent_match={int(actual_parent == parent)} image_match={int(actual_image == entry)}", file=sys.stderr)
    if actual_parent != parent:
        raise ValueError("Refusing a process not spawned by caller")
    if actual_image != entry:
        raise ValueError("Process image mismatch")
    if row_hwnd is not None and (type(row_hwnd) is not int or row_hwnd <= 0):
        raise ValueError("Invalid popup handle")
    # All variable values are validated numeric/hex or single-quote escaped.
    image = str(entry).replace("'", "''")
    script = f"""
$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'
$stage = 1
try {{
  $process = [System.Diagnostics.Process]::GetProcessById({pid})
  if ($process.HasExited -or $process.MainModule.FileName -ne '{image}') {{ throw [InvalidOperationException]::new() }}
  $algorithm = [System.Security.Cryptography.SHA256]::Create()
  try {{
    $actualHash = [BitConverter]::ToString($algorithm.ComputeHash([System.IO.File]::ReadAllBytes($process.MainModule.FileName))).Replace('-', '')
  }} finally {{ $algorithm.Dispose() }}
  if ($actualHash -ne '{digest}') {{ throw [InvalidOperationException]::new() }}
  $stage = 2
  [void][System.Reflection.Assembly]::Load('UIAutomationClient, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35')
  [void][System.Reflection.Assembly]::Load('UIAutomationTypes, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35')
  $stage = 3
  $process.Refresh()
  $hwnd = {"[IntPtr]::new(" + str(row_hwnd) + ")" if row_hwnd is not None else "$process.MainWindowHandle"}
  if ($hwnd -eq [IntPtr]::Zero) {{ throw [InvalidOperationException]::new() }}
  $root = [System.Windows.Automation.AutomationElement]::FromHandle($hwnd)
  if ($root.Current.ProcessId -ne {pid}) {{ throw [InvalidOperationException]::new() }}
  $condition = [System.Windows.Automation.PropertyCondition]::new(
    [System.Windows.Automation.AutomationElement]::{"AutomationIdProperty" if row_hwnd is not None else "ControlTypeProperty"},
    {"'OwnedPreviewRow'" if row_hwnd is not None else "[System.Windows.Automation.ControlType]::Edit"})
  $field = $root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $condition)
  if ($null -eq $field -or $field.Current.ProcessId -ne {pid}) {{ throw [InvalidOperationException]::new() }}
  $stage = 4
  $bounds = $field.Current.BoundingRectangle
  if ($bounds.IsEmpty -or $bounds.Width -lt 4 -or $bounds.Height -lt 4 -or $field.Current.IsOffscreen -or -not $field.Current.IsEnabled) {{ throw [InvalidOperationException]::new() }}
  $rootBounds = $root.Current.BoundingRectangle
  if ($rootBounds.IsEmpty -or $rootBounds.Width -le 0 -or $rootBounds.Height -le 0) {{ throw [InvalidOperationException]::new() }}
  if ($bounds.Left -lt $rootBounds.Left -or $bounds.Top -lt $rootBounds.Top -or $bounds.Right -gt $rootBounds.Right -or $bounds.Bottom -gt $rootBounds.Bottom) {{ throw [InvalidOperationException]::new() }}
  $culture = [Globalization.CultureInfo]::InvariantCulture
  $u = (($bounds.Left + $bounds.Width / 2 - $rootBounds.Left) / $rootBounds.Width).ToString('R', $culture)
  $v = (($bounds.Top + $bounds.Height / 2 - $rootBounds.Top) / $rootBounds.Height).ToString('R', $culture)
  [Console]::Out.WriteLine("owned_focus_fraction hwnd=$($hwnd.ToInt64()) u=$u v=$v")
}} catch {{
  [Console]::Error.WriteLine("owned_focus_prepare_failed stage=$stage kind=" + $_.Exception.GetType().Name)
  exit 1
}}
"""
    powershell = Path(os.environ.get("SystemRoot", r"C:\Windows")) / "System32/WindowsPowerShell/v1.0/powershell.exe"
    result = subprocess.run([str(powershell), "-NoProfile", "-NonInteractive", "-STA", "-OutputFormat", "Text", "-EncodedCommand",
                             base64.b64encode(script.encode("utf-16le")).decode("ascii")],
                            stdin=subprocess.DEVNULL, capture_output=True, text=True, timeout=7,
                            creationflags=subprocess.CREATE_NO_WINDOW)
    point = re.fullmatch(r"owned_focus_fraction hwnd=([0-9]+) u=(0\.[0-9]+) v=(0\.[0-9]+)\s*", result.stdout)
    if result.returncode or not point or result.stderr.strip() or not (0 < float(point[2]) < 1 and 0 < float(point[3]) < 1):
        print(f"owned_focus_helper_exit={result.returncode} stdout_bytes={len(result.stdout)} stderr_bytes={len(result.stderr)}", file=sys.stderr)
        diagnostic = re.search(r"owned_focus_prepare_failed stage=[0-9]+ kind=[A-Za-z0-9]+", result.stderr)
        if diagnostic:
            print(diagnostic[0].strip(), file=sys.stderr)
        raise ValueError("Owned focus preparation refused")
    if row_hwnd is not None and int(point[1]) != row_hwnd:
        raise ValueError("Popup HWND changed")
    from owned_mouse_focus import click_owned_window
    click_owned_window(pid, int(point[1]), float(point[2]), float(point[3]), journal, relative=True, require_foreground=row_hwnd is None)


def prepare_macos(pid, parent, entry):
    record = subprocess.run(["/bin/ps", "-o", "ppid=", "-p", str(pid)], capture_output=True,
                            text=True, timeout=3, check=True)
    if record.stdout.strip() != str(parent):
        raise ValueError("Refusing a process not spawned by caller")
    proc = ctypes.CDLL("/usr/lib/libproc.dylib")
    proc.proc_pidpath.argtypes = [ctypes.c_int, ctypes.c_void_p, ctypes.c_uint32]
    proc.proc_pidpath.restype = ctypes.c_int
    path = ctypes.create_string_buffer(4096)
    if proc.proc_pidpath(pid, path, len(path)) <= 0 or Path(os.fsdecode(path.value)).resolve() != entry:
        raise ValueError("Process image mismatch")
    ctypes.CDLL("/System/Library/Frameworks/AppKit.framework/AppKit")
    objc = ctypes.CDLL("/usr/lib/libobjc.A.dylib")
    objc.objc_getClass.argtypes = [ctypes.c_char_p]
    objc.objc_getClass.restype = ctypes.c_void_p
    objc.sel_registerName.argtypes = [ctypes.c_char_p]
    objc.sel_registerName.restype = ctypes.c_void_p
    address = ctypes.cast(objc.objc_msgSend, ctypes.c_void_p).value
    send = ctypes.CFUNCTYPE(ctypes.c_void_p, ctypes.c_void_p, ctypes.c_void_p)(address)
    send_void = ctypes.CFUNCTYPE(None, ctypes.c_void_p, ctypes.c_void_p)(address)
    send_pid = ctypes.CFUNCTYPE(ctypes.c_void_p, ctypes.c_void_p, ctypes.c_void_p, ctypes.c_int)(address)
    activate = ctypes.CFUNCTYPE(ctypes.c_bool, ctypes.c_void_p, ctypes.c_void_p, ctypes.c_ulong)(address)
    sel = objc.sel_registerName
    pool = send(send(objc.objc_getClass(b"NSAutoreleasePool"), sel(b"alloc")), sel(b"init"))
    try:
        app = send_pid(objc.objc_getClass(b"NSRunningApplication"), sel(b"runningApplicationWithProcessIdentifier:"), pid)
        if not app or not activate(app, sel(b"activateWithOptions:"), 2):
            raise ValueError("Owned application activation refused")
    finally:
        if pool:
            send_void(pool, sel(b"drain"))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--pid", type=int, required=True)
    parser.add_argument("--executable", type=Path, required=True)
    parser.add_argument("--sha256", required=True)
    parser.add_argument("--journal", type=Path)
    parser.add_argument("--row-hwnd", type=int)
    args = parser.parse_args()
    if not 0 < args.pid <= 2147483647 or sys.platform not in ("win32", "darwin"):
        raise ValueError("Unsupported focus target")
    try:
        entry = validate_image(args.executable, args.sha256)
    except (OSError, ValueError) as error:
        print("owned_focus_image_validation_failed kind=" + type(error).__name__, file=sys.stderr)
        raise
    parent = os.getppid()
    if sys.platform == "win32":
        prepare_windows(args.pid, parent, entry, args.sha256, args.journal, row_hwnd=args.row_hwnd)
    else:
        if args.row_hwnd is not None:
            raise ValueError("Native row is Windows-only")
        prepare_macos(args.pid, parent, entry)
    print("owned_focus_prepared")
    return 0


if __name__ == "__main__":
    try:
        sys.exit(main())
    except (OSError, ValueError, subprocess.SubprocessError) as error:
        import traceback
        frames = traceback.extract_tb(error.__traceback__)
        print(f"owned_focus_refused kind={type(error).__name__} line={frames[-1].lineno}", file=sys.stderr)
        print("Owned focus preparation refused.", file=sys.stderr)
        sys.exit(1)
