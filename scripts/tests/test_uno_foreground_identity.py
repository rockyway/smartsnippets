"""Behavioral regression: real observer paths, inert child/root/PID/thread tables."""
import json
from pathlib import Path
import tempfile
from types import SimpleNamespace
import unittest
from unittest.mock import patch
from test_uno_candidate_native import native
from test_uno_candidate_ui import s, FakeWindows
from test_uno_window_diagnostics import Fn


def desktop(raw, root, pid=100, root_thread=7, root_pid=None):
    def pin(h, out):
        out._obj.value = pid if h == raw else (pid if root_pid is None else root_pid)
        return 7 if h == raw else root_thread
    return SimpleNamespace(user=SimpleNamespace(
        GetWindowLongPtrW=Fn(lambda h, i: 0x40000000 if h == raw and raw != root else 0),
        GetAncestor=Fn(lambda h, flags: root if flags == 2 else 0),
        GetWindowThreadProcessId=Fn(pin), GetForegroundWindow=lambda: raw),
        validate_input_desktop=lambda: None)


class ForegroundIdentityTests(unittest.TestCase):
    def test_posted_sample_child_relations_and_pin_refusals(self):
        for raw, root, pid, relation in ((101, 10, 100, 'owned_a'), (102, 20, 100, 'owned_b'),
                (103, 99, 100, 'same_pid_other'), (104, 98, 999, 'foreign'), (10, 10, 100, 'owned_a')):
            with self.subTest(root=root), tempfile.TemporaryDirectory() as directory:
                private = Path(directory)
                (private / 'positive-surfaces.txt').write_bytes(b'10 20 30 40')
                (private / 'positive-identities.txt').write_bytes(b'100 200 300')
                policy = native.PostedForegroundMonitor(private, 1, lambda: 0)
                predicate, actual = policy.sample(desktop(raw, root, pid), raw)
                self.assertEqual(relation, actual)
                self.assertEqual(None if root == 10 else 'foreground_foreign', predicate)
                self.assertEqual(raw != root, policy.foreground_was_child)
        for tid, pid in ((0, 100), (8, 100), (7, 999)):
            self.assertEqual((0, True), native.foreground_identity.normalize(
                desktop(101, 10, root_thread=tid, root_pid=pid), 101))

    def test_monitor_allowed_set_and_failure_record_keep_raw_fact(self):
        for root in (10, 99):
            with tempfile.TemporaryDirectory() as directory:
                private = Path(directory)
                class Process:
                    returncode = 0
                    calls = 0
                    def poll(self):
                        self.calls += 1
                        return None if self.calls == 1 else 0
                d = desktop(101, root)
                facts = native.empty_window_facts()
                facts['foreground_was_child'] = True
                with patch.object(native, 'window_facts', return_value=facts) as sampled:
                    call = lambda: native.monitor(Process(), d, 10, [], sleep=lambda _: None,
                                                   scene='restore', evidence_private=private)
                    if root == 10:
                        call()
                        sampled.assert_not_called()
                    else:
                        with self.assertRaisesRegex(ValueError, 'native_scene_failed'):
                            call()
                        sampled.assert_called_once_with(d, 101, 0, 0)
                        record = native.validate_monitor_failure(private)
                        self.assertTrue(record['foreground_was_child'])
                        self.assertTrue(record['windowFacts']['foreground_was_child'])

    @unittest.skipUnless(s.sys.platform == 'win32', 'PowerShell inert boundary test')
    def test_powershell_real_normalizer_and_guard_comparisons(self):
        import subprocess
        helper = s.HELPER.read_text()
        method = 'public static IntPtr ForegroundRoot' + helper.split('public static IntPtr ForegroundRoot', 1)[1].split(' [DllImport', 1)[0]
        guard = helper.split('        # Sample after authentication, then recheck once if picker activation raced it.', 1)[1].split('    function Guard {', 1)[0].split('    function SelectOwnedLeave', 1)[0].rsplit('    }', 1)[0]
        script = r'''
$ErrorActionPreference='Stop'
Add-Type @'
using System;
public static class Owned019 {
 public static long Raw=101, Root=10; public static uint RootPid=30, RootThread=7;
 public static IntPtr GetForegroundWindow(){return new IntPtr(Raw);}
 public static IntPtr GetWindowLongPtrW(IntPtr h,int i){return new IntPtr(h.ToInt64()==101?0x40000000:0);}
 public static IntPtr GetAncestor(IntPtr h,uint f){return new IntPtr(Root);}
 public static uint GetWindowThreadProcessId(IntPtr h,out uint p){p=h.ToInt64()==101?30u:RootPid;return h.ToInt64()==101?7u:RootThread;}
 METHOD
}
'@
function ForegroundFailure { $script:refused=$true }
$OwnedHwnd=10; $InitialForeground=1; $Scene='snippets'; $script:pickerPhase=$null
function Check([long]$root,[long]$raw,[long]$dialogH,[bool]$expected) {
 [Owned019]::Root=$root; [Owned019]::Raw=$raw; $dialog=[IntPtr]$dialogH
 $script:focused=$false; $script:pickerHwnd=[IntPtr]::Zero; $script:refused=$false
 $passed=$true
 try { GUARD } catch { if($_.Exception.Message -cne 'foreground'){throw}; $passed=$false }
 if($passed -ne $expected -or $script:refused -eq $expected){throw 'root_guard'}
}
Check 10 101 0 $true
Check 20 101 20 $true
Check 99 101 0 $false
Check 10 10 0 $true
[Owned019]::RootPid=31; Check 10 101 0 $false
[Owned019]::RootPid=30; [Owned019]::RootThread=8; Check 10 101 0 $false
'''.replace('METHOD', method).replace('GUARD', guard)
        result = subprocess.run([str(s.system_powershell()), '-NoProfile', '-NonInteractive', '-Command', script],
                                capture_output=True, timeout=20, creationflags=subprocess.CREATE_NO_WINDOW)
        self.assertEqual((0, b'', b''), (result.returncode, result.stdout, result.stderr))

    def test_foreground_pid_uses_root_and_mismatch_is_unavailable(self):
        self.assertEqual(100, native.foreground_pid(desktop(101, 10), 101))
        self.assertEqual(0, native.foreground_pid(desktop(101, 10, root_pid=999), 101))

    def test_management_observation_shutdown_picker_and_diagnostics_use_roots(self):
        world = FakeWindows(enabled_popup=0, foreground=101)
        world.table[101] = dict(world.table[10], root=10)
        world.desktop.user.GetWindowLongPtrW.side_effect = lambda h, i: 0x40000000 if h == 101 else 0
        world.desktop.windows.return_value = [(0, 0, 10, 10)]
        world.desktop.snapshot.return_value = (101, 2)
        self.assertEqual((10, 2), s.normalized_snapshot(world.desktop))
        record = s.foreground_failure_record(world.desktop, 101, (1, 2), 10, 30)
        self.assertEqual('owned_main', record['foregroundRelation'])
        self.assertTrue(record['fgIsMain'])
        self.assertTrue(s.observe_foreground_frame(world.desktop, 10, 30, (1, 2), None, {}, False, 1))
        self.assertFalse(s.shutdown_frame(world.desktop, 30, 10, (1, 2), (0, 0, 20, 20), False, True, lambda *a, **k: [10]))
        self.assertTrue(s.picker_foreground(world.desktop, 101, 10, 10))
        world.table[101]['root'] = 99
        world.table[99] = dict(world.table[10])
        with self.assertRaises(s.ForegroundChanged):
            s.observe_foreground_frame(world.desktop, 10, 30, (1, 2), None, {}, True, 2)
