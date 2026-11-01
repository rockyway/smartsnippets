"""Headless picker-thread regression controls; no native window APIs."""
import json
import subprocess
import tempfile
import unittest
from pathlib import Path
from test_uno_candidate_ui import s, FakeWindows


class PickerThreadTests(unittest.TestCase):
    def test_other_thread_invisible_then_visible_and_never_visible(self):
        world = FakeWindows(foreground=20)
        world.table[20]['thread'] = 41
        visible = {10}
        world.desktop.user.IsWindowVisible.side_effect = lambda h: h in visible
        handles, seen = {}, set()
        def frame():
            return s.observe_foreground_frame(world.desktop, 10, 30, (1, 2),
                                             'import-cancel', handles, True, 7, seen)
        self.assertTrue(frame())
        self.assertEqual({'import-cancel': 20}, handles)
        self.assertEqual(set(), seen)  # cannot complete without a visible observation
        self.assertTrue(frame())
        self.assertEqual(set(), seen)
        visible.add(20)
        self.assertTrue(frame())
        self.assertEqual({'import-cancel'}, seen)
        record = s.foreground_failure_record(world.desktop, 20, (1, 2), 10, 30, 20, 0, 7)
        self.assertEqual(dict(foregroundRelation='owned_other', fgIsMain=False, fgIsPopup=True,
                              popupPresent=True, fgSamePid=True, fgThreadIsMainThread=False,
                              fgOwnerIsMain=True, fgRootOwnerIsMain=True, fgClassIs32770=True,
                              fgVisible=True, fgEnabled=True, phaseIndex=0, sampleOrder=7), record)

    def test_foreground_identity_pairs_and_full_records(self):
        for field, value in [('cls', 'Other'), ('owner', 11), ('pid', 31)]:
            with self.subTest(field=field):
                world = FakeWindows(foreground=20)
                world.table[20].update(thread=41)
                world.table[20][field] = value
                with self.assertRaises(s.ForegroundChanged) as caught:
                    s.observe_foreground_frame(world.desktop, 10, 30, (1, 2), 'import-cancel', {}, True, 7)
                e = caught.exception
                record = s.foreground_failure_record(world.desktop, e.foreground, (1, 2), 10, 30,
                                                     e.popup, e.phase_index, e.sample_order)
                self.assertEqual(dict(foregroundRelation='external' if field == 'pid' else 'owned_other',
                    fgIsMain=False, fgIsPopup=False, popupPresent=False, fgSamePid=field != 'pid',
                    fgThreadIsMainThread=False, fgOwnerIsMain=field != 'owner', fgRootOwnerIsMain=True,
                    fgClassIs32770=field != 'cls', fgVisible=True, fgEnabled=True, phaseIndex=0,
                    sampleOrder=7), record)
        world = FakeWindows(foreground=20)
        with self.assertRaisesRegex(s.ForegroundChanged, 'ui_picker_replaced') as caught:
            s.observe_foreground_frame(world.desktop, 10, 30, (1, 2), 'import-cancel',
                                      {'import-cancel': 19}, True, 7)
        e = caught.exception
        self.assertEqual(dict(foregroundRelation='owned_other', fgIsMain=False, fgIsPopup=True,
            popupPresent=True, fgSamePid=True, fgThreadIsMainThread=True, fgOwnerIsMain=True,
            fgRootOwnerIsMain=True, fgClassIs32770=True, fgVisible=True, fgEnabled=True,
            phaseIndex=0, sampleOrder=7), s.foreground_failure_record(world.desktop,
                e.foreground, (1, 2), 10, 30, e.popup, e.phase_index, e.sample_order))

    @unittest.skipUnless(s.sys.platform == 'win32', 'system PowerShell stub execution')
    def test_powershell_owned_picker_accept_refuse_pairs(self):
        script = r'''
$ErrorActionPreference='Stop'
Add-Type @'
using System;
using System.Text;
public static class Owned019 {
 public static uint Pid=30; public static string Class="#32770";
 public static int Owner=10; public static bool Visible=false;
 public static IntPtr GetWindow(IntPtr h,int n) { return new IntPtr(n==6?0:Owner); }
 public static IntPtr GetForegroundWindow() { return new IntPtr(20); }
 public static uint GetWindowThreadProcessId(IntPtr h,out uint pid) { pid=h.ToInt64()==10?30:Pid; return h.ToInt64()==10?40u:41u; }
 public static int GetClassName(IntPtr h,StringBuilder b,int n) { b.Append(Class); return Class.Length; }
 public static bool IsWindowVisible(IntPtr h) { return Visible; }
 public static bool IsWindow(IntPtr h) { return true; }
}
'@
$t=$null; $e=$null
$a=[Management.Automation.Language.Parser]::ParseFile('HELPER',[ref]$t,[ref]$e)
if($e.Count){throw 'parse'}
$f=$a.FindAll({param($n) $n -is [Management.Automation.Language.FunctionDefinitionAst] -and $n.Name -ceq 'OwnedPicker'},$true)
Invoke-Expression $f[0].Extent.Text
$h=[IntPtr]10; $OwnedPid=30; $script:pickerHwnd=[IntPtr]::Zero
if((OwnedPicker) -ne [IntPtr]20){throw 'invisible'}
[Owned019]::Visible=$true
if((OwnedPicker) -ne [IntPtr]20){throw 'visible'}
[Owned019]::Class='Other'
if((OwnedPicker) -ne [IntPtr]::Zero){throw 'class'}
[Owned019]::Class='#32770'; [Owned019]::Owner=11
if((OwnedPicker) -ne [IntPtr]::Zero){throw 'owner'}
[Owned019]::Owner=10; [Owned019]::Pid=31
if((OwnedPicker) -ne [IntPtr]::Zero){throw 'pid'}
'''.replace('HELPER', str(s.HELPER).replace("'", "''"))
        # Execute the real Guard latch/refusal block after OwnedPicker, with only
        # the diagnostic sink stubbed (no process/desktop APIs are loaded).
        helper = s.HELPER.read_text()
        latch = helper.split('            $dialog = OwnedPicker', 1)[1].split(
            '        # Sample after authentication', 1)[0]
        script += "\n[Owned019]::Pid=30; $script:pickerHwnd=[IntPtr]19\n"
        script += "function ForegroundFailure { $script:recorded=$true }\n"
        script += "$script:recorded=$false; $refused=$false; try { $dialog=OwnedPicker\n"
        script += latch.rsplit('        }', 1)[0]
        script += "\n} catch { if($_.Exception.Message -cne 'picker_replaced'){throw}; $refused=$true }\n"
        script += "if(-not $refused -or -not $script:recorded){throw 'replacement'}\n"
        result = subprocess.run([str(s.system_powershell()), '-NoProfile', '-NonInteractive', '-Command', script],
                                capture_output=True, timeout=20, creationflags=subprocess.CREATE_NO_WINDOW)
        self.assertEqual((0, b'', b''), (result.returncode, result.stdout, result.stderr))
