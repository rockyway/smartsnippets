"""Create fresh Windows evidence directories with privacy set at creation, never repaired."""
import ctypes
from ctypes import wintypes as w
from pathlib import Path
import secrets
import tempfile


class SecurityAttributes(ctypes.Structure):
    _fields_ = [("nLength", w.DWORD), ("lpSecurityDescriptor", ctypes.c_void_p),
                ("bInheritHandle", w.BOOL)]


class SidAndAttributes(ctypes.Structure):
    _fields_ = [("Sid", ctypes.c_void_p), ("Attributes", w.DWORD)]


class WindowsSecurity:
    def __init__(self):
        self.kernel = ctypes.WinDLL("kernel32", use_last_error=True)
        self.advapi = ctypes.WinDLL("advapi32", use_last_error=True)
        p = ctypes.c_void_p
        self._bind(self.kernel, "GetCurrentProcess", [], w.HANDLE)
        self._bind(self.kernel, "CloseHandle", [w.HANDLE], w.BOOL)
        self._bind(self.kernel, "LocalFree", [p], p)
        self._bind(self.kernel, "CreateDirectoryW", [w.LPCWSTR, ctypes.POINTER(SecurityAttributes)], w.BOOL)
        self._bind(self.advapi, "OpenProcessToken", [w.HANDLE, w.DWORD, ctypes.POINTER(w.HANDLE)], w.BOOL)
        self._bind(self.advapi, "GetTokenInformation", [w.HANDLE, ctypes.c_int, p, w.DWORD, ctypes.POINTER(w.DWORD)], w.BOOL)
        self._bind(self.advapi, "ConvertSidToStringSidW", [p, ctypes.POINTER(p)], w.BOOL)
        self._bind(self.advapi, "ConvertStringSecurityDescriptorToSecurityDescriptorW", [w.LPCWSTR, w.DWORD, ctypes.POINTER(p), ctypes.POINTER(w.DWORD)], w.BOOL)
        self._bind(self.advapi, "GetNamedSecurityInfoW", [w.LPWSTR, ctypes.c_int, w.DWORD, p, p, p, p, ctypes.POINTER(p)], w.DWORD)
        self._bind(self.advapi, "ConvertSecurityDescriptorToStringSecurityDescriptorW", [p, w.DWORD, w.DWORD, ctypes.POINTER(p), ctypes.POINTER(w.DWORD)], w.BOOL)

    @staticmethod
    def _bind(dll, name, args, result):
        function = getattr(dll, name)
        function.argtypes, function.restype = args, result

    @staticmethod
    def check(ok):
        if not ok:
            raise ctypes.WinError(ctypes.get_last_error())

    def current_sid(self):
        token = w.HANDLE()
        self.check(self.advapi.OpenProcessToken(self.kernel.GetCurrentProcess(), 8, ctypes.byref(token)))
        try:
            size = w.DWORD()
            ok = self.advapi.GetTokenInformation(token, 1, None, 0, ctypes.byref(size))
            if ok or ctypes.get_last_error() != 122 or size.value < ctypes.sizeof(SidAndAttributes):
                raise OSError("Cannot size token identity")
            data = ctypes.create_string_buffer(size.value)
            self.check(self.advapi.GetTokenInformation(token, 1, data, size, ctypes.byref(size)))
            text = ctypes.c_void_p()
            self.check(self.advapi.ConvertSidToStringSidW(SidAndAttributes.from_buffer(data).Sid, ctypes.byref(text)))
            try:
                return ctypes.wstring_at(text)
            finally:
                self.kernel.LocalFree(text)
        finally:
            self.kernel.CloseHandle(token)

    @staticmethod
    def policy(sid):
        return f"O:{sid}D:P(A;OICI;FA;;;{sid})"

    def create(self, root, sid):
        descriptor = ctypes.c_void_p()
        self.check(self.advapi.ConvertStringSecurityDescriptorToSecurityDescriptorW(self.policy(sid), 1, ctypes.byref(descriptor), None))
        try:
            attributes = SecurityAttributes(ctypes.sizeof(SecurityAttributes), descriptor, False)
            self.check(self.kernel.CreateDirectoryW(str(root), ctypes.byref(attributes)))
        finally:
            self.kernel.LocalFree(descriptor)

    def verify(self, root, sid):
        descriptor = ctypes.c_void_p()
        error = self.advapi.GetNamedSecurityInfoW(str(root), 1, 5, None, None, None, None, ctypes.byref(descriptor))
        if error:
            raise ctypes.WinError(error)
        try:
            text = ctypes.c_void_p()
            self.check(self.advapi.ConvertSecurityDescriptorToStringSecurityDescriptorW(descriptor, 1, 5, ctypes.byref(text), None))
            try:
                if ctypes.wstring_at(text) != self.policy(sid):
                    raise OSError("Evidence directory security mismatch")
            finally:
                self.kernel.LocalFree(text)
        finally:
            self.kernel.LocalFree(descriptor)


def create_windows_private_directory():
    security = WindowsSecurity()
    sid = security.current_sid()
    root = Path(tempfile.gettempdir()) / ("ss-uno018-ui-" + secrets.token_hex(16))
    # One create only: a collision is failure, never an existing-directory open.
    security.create(root, sid)
    try:
        security.verify(root, sid)
    except BaseException:
        root.rmdir()  # Only the newly created, still empty directory; never recursive.
        raise
    return root
