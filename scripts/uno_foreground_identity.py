"""Read-only acceptance foreground identity; never follows window owners."""
import ctypes as C
from ctypes import wintypes as W


def normalize(desktop, raw):
    user = desktop.user
    if not raw:
        return raw, False
    style = user.GetWindowLongPtrW
    try:
        style.argtypes, style.restype = [W.HWND, C.c_int], C.c_ssize_t
    except AttributeError:  # bound-method test double
        pass
    child = bool(style(raw, -16) & 0x40000000)
    if not child:
        return raw, False
    for name, args, result in (
            ('GetAncestor', [W.HWND, W.UINT], W.HWND),
            ('GetWindowThreadProcessId', [W.HWND, C.POINTER(W.DWORD)], W.DWORD)):
        fn = getattr(user, name)
        try:
            fn.argtypes, fn.restype = args, result
        except AttributeError:
            pass
    root = user.GetAncestor(raw, 2)
    def pin(h):
        pid = W.DWORD()
        tid = user.GetWindowThreadProcessId(h, C.byref(pid)) if h else 0
        return tid, pid.value
    tid, pid = pin(raw)
    return (root if root and tid and pid and (tid, pid) == pin(root) else 0), True
