"""Private, read-only AX observer for a live sibling owned target. No titles/input."""
import argparse
import ctypes as C
import hashlib
import os
from pathlib import Path
import subprocess
import sys


def frontmost_pid():
    # Public AppKit foreground metadata, like the existing native surface probe.
    # No titles, activation, UI enumeration or accessibility fallback heuristics.
    C.CDLL('/System/Library/Frameworks/AppKit.framework/AppKit')
    objc = C.CDLL('/usr/lib/libobjc.A.dylib')
    objc.objc_getClass.argtypes, objc.objc_getClass.restype = [C.c_char_p], C.c_void_p
    objc.sel_registerName.argtypes, objc.sel_registerName.restype = [C.c_char_p], C.c_void_p
    address = C.cast(objc.objc_msgSend, C.c_void_p).value
    send = C.CFUNCTYPE(C.c_void_p, C.c_void_p, C.c_void_p)(address)
    send_int = C.CFUNCTYPE(C.c_int, C.c_void_p, C.c_void_p)(address)
    send_void = C.CFUNCTYPE(None, C.c_void_p, C.c_void_p)(address)
    sel = objc.sel_registerName
    pool = send(send(objc.objc_getClass(b'NSAutoreleasePool'), sel(b'alloc')), sel(b'init'))
    if not pool:
        raise ValueError('AppKit pool unavailable')
    try:
        workspace = send(objc.objc_getClass(b'NSWorkspace'), sel(b'sharedWorkspace'))
        app = send(workspace, sel(b'frontmostApplication')) if workspace else 0
        pid = send_int(app, sel(b'processIdentifier')) if app else 0
        if pid <= 0:
            raise ValueError('Foreground unavailable')
        return pid
    finally:
        send_void(pool, sel(b'drain'))


class AxReadFailure(ValueError):
    def __init__(self, name, status):
        super().__init__('AX attribute unavailable')
        self.attribute = name if name in ('AXWindows', 'AXIdentifier', 'AXFocusedApplication', 'AXFocusedWindow') else 'unknown'
        self.status = status


class Identity:
    def __init__(self, pid):
        self.pid = pid
        self.refs = []
        self.ax = C.CDLL('/System/Library/Frameworks/ApplicationServices.framework/ApplicationServices')
        self.cf = C.CDLL('/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation')
        for library, name, args, result in [
            (self.ax, 'AXIsProcessTrusted', [], C.c_bool),
            (self.ax, 'AXUIElementCreateApplication', [C.c_int], C.c_void_p),
            (self.ax, 'AXUIElementGetTypeID', [], C.c_ulong),
            (self.ax, 'AXUIElementGetPid', [C.c_void_p, C.POINTER(C.c_int)], C.c_int),
            (self.ax, 'AXUIElementSetMessagingTimeout', [C.c_void_p, C.c_float], C.c_int),
            (self.ax, 'AXUIElementCopyAttributeValue', [C.c_void_p, C.c_void_p, C.POINTER(C.c_void_p)], C.c_int),
            (self.cf, 'CFRelease', [C.c_void_p], None),
            (self.cf, 'CFRetain', [C.c_void_p], C.c_void_p),
            (self.cf, 'CFEqual', [C.c_void_p, C.c_void_p], C.c_bool),
            (self.cf, 'CFGetTypeID', [C.c_void_p], C.c_ulong),
            (self.cf, 'CFArrayGetTypeID', [], C.c_ulong),
            (self.cf, 'CFArrayGetCount', [C.c_void_p], C.c_long),
            (self.cf, 'CFArrayGetValueAtIndex', [C.c_void_p, C.c_long], C.c_void_p),
            (self.cf, 'CFStringGetTypeID', [], C.c_ulong),
            (self.cf, 'CFStringCreateWithCString', [C.c_void_p, C.c_char_p, C.c_uint32], C.c_void_p),
            (self.cf, 'CFStringGetCString', [C.c_void_p, C.c_void_p, C.c_long, C.c_uint32], C.c_bool),
        ]:
            fn = getattr(library, name)
            fn.argtypes, fn.restype = args, result
        if not self.ax.AXIsProcessTrusted():
            raise ValueError('Accessibility unavailable')
        self.app = self.own(self.ax.AXUIElementCreateApplication(pid))
        for element in (self.app,):
            if self.ax.AXUIElementSetMessagingTimeout(element, 1.0):
                raise ValueError('AX timeout configuration failed')
        self.require_pid(self.app)
        windows = self.windows()
        if len(windows) != 2:
            raise ValueError('Two windows required')
        found = {}
        for window in windows:
            identifier = self.copy(window, 'AXIdentifier')
            try:
                if self.cf.CFGetTypeID(identifier) != self.cf.CFStringGetTypeID():
                    raise ValueError('Identifier type refused')
                data = C.create_string_buffer(128)
                if not self.cf.CFStringGetCString(identifier, data, len(data), 0x08000100):
                    raise ValueError('Identifier refused')
                if data.value not in (b'SSFixtureWindowA', b'SSFixtureWindowB') or data.value in found:
                    raise ValueError('Fixture identity refused')
                found[data.value] = window
            finally:
                self.cf.CFRelease(identifier)
        self.a, self.b = found[b'SSFixtureWindowA'], found[b'SSFixtureWindowB']
        if self.cf.CFEqual(self.a, self.b):
            raise ValueError('Distinct windows required')

    def own(self, ref):
        if not ref:
            raise ValueError('Null AX value')
        self.refs.append(ref)
        return ref

    def require_pid(self, element):
        pid = C.c_int()
        if not element or self.cf.CFGetTypeID(element) != self.ax.AXUIElementGetTypeID() or self.ax.AXUIElementGetPid(element, C.byref(pid)) or pid.value != self.pid:
            raise ValueError('AX owner refused')

    def copy(self, element, name):
        key = self.cf.CFStringCreateWithCString(None, name.encode('ascii'), 0x08000100)
        if not key:
            raise ValueError('AX key allocation failed')
        value = C.c_void_p()
        try:
            status = self.ax.AXUIElementCopyAttributeValue(element, key, C.byref(value))
            if status or not value.value:
                # A failure out-parameter is not a successfully copied owned ref.
                raise AxReadFailure(name, status)
            return value.value
        finally:
            self.cf.CFRelease(key)

    def windows(self):
        array = self.copy(self.app, 'AXWindows')
        try:
            if self.cf.CFGetTypeID(array) != self.cf.CFArrayGetTypeID():
                raise ValueError('AX windows type refused')
            count = self.cf.CFArrayGetCount(array)
            if not 0 <= count <= 2:
                raise ValueError('AX window count refused')
            result = []
            for index in range(count):
                ref = self.cf.CFArrayGetValueAtIndex(array, index)
                self.require_pid(ref)
                result.append(self.own(self.cf.CFRetain(ref)))
            return result
        finally:
            self.cf.CFRelease(array)

    def check(self, command):
        if command == 'closed_a':
            live = self.windows()
            if len(live) != 1 or not self.cf.CFEqual(live[0], self.b) or any(self.cf.CFEqual(w, self.a) for w in live):
                raise ValueError('Closed window still eligible')
            return
        if command not in ('focus_a', 'focus_b'):
            raise ValueError('Command refused')
        if frontmost_pid() != self.pid:
            raise ValueError('AX owner refused')
        focused = self.copy(self.app, 'AXFocusedWindow')
        try:
            self.require_pid(focused)
            expected = self.a if command == 'focus_a' else self.b
            if not self.cf.CFEqual(focused, expected):
                raise ValueError('Exact focused window mismatch')
            other = self.b if command == 'focus_a' else self.a
            if self.cf.CFEqual(focused, other):
                raise ValueError('Same PID other window accepted')
            if frontmost_pid() != self.pid:
                raise ValueError('AX owner refused')
        finally:
            self.cf.CFRelease(focused)

    def close(self):
        for ref in reversed(self.refs):
            self.cf.CFRelease(ref)
        self.refs.clear()


def verify_sibling(pid, entry, digest):
    if sys.platform != 'darwin' or not 0 < pid <= 2147483647:
        raise ValueError('Mac owned target required')
    from run_smoke import validate_entry
    entry = validate_entry(entry, digest, False)
    record = subprocess.run(['/bin/ps', '-o', 'ppid=', '-p', str(pid)], capture_output=True, text=True, timeout=3, check=True)
    if record.stdout.strip() != str(os.getppid()):
        raise ValueError('Target must be sibling owned by live supervisor')
    proc = C.CDLL('/usr/lib/libproc.dylib')
    proc.proc_pidpath.argtypes = [C.c_int, C.c_void_p, C.c_uint32]
    proc.proc_pidpath.restype = C.c_int
    path = C.create_string_buffer(4096)
    if proc.proc_pidpath(pid, path, len(path)) <= 0 or Path(os.fsdecode(path.value)).resolve() != entry:
        raise ValueError('Target image mismatch')
    if hashlib.sha256(entry.read_bytes()).hexdigest() != digest.lower():
        raise ValueError('Target image changed')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--pid', type=int, required=True)
    parser.add_argument('--executable', type=Path, required=True)
    parser.add_argument('--sha256', required=True)
    args = parser.parse_args()
    verify_sibling(args.pid, args.executable, args.sha256)
    identity = Identity.__new__(Identity)
    identity.refs = []
    try:
        identity.__init__(args.pid)
        print('ax_identity_ready', flush=True)
        expected = ['focus_a', 'focus_b', 'focus_a', 'closed_a']
        for index, line in enumerate(sys.stdin):
            command = line.rstrip('\n')
            if index >= len(expected) or command != expected[index]:
                raise ValueError('Identity sequence refused')
            identity.check(command)
            print('ax_identity_verified name=' + command, flush=True)
    finally:
        identity.close()
    return 0


if __name__ == '__main__':
    try:
        sys.exit(main())
    except (OSError, ValueError, subprocess.SubprocessError) as error:
        reasons = {'AX owner refused': 'owner', 'AX attribute unavailable': 'attribute',
                   'Exact focused window mismatch': 'window', 'Same PID other window accepted': 'sibling',
                   'Closed window still eligible': 'closed', 'Accessibility unavailable': 'permission'}
        detail = (' attribute=' + error.attribute + ' status=' + str(error.status)) if isinstance(error, AxReadFailure) else ''
        print('ax_identity_failed kind=' + type(error).__name__ + ' reason=' + reasons.get(str(error), 'other') + detail, file=sys.stderr)
        sys.exit(1)
