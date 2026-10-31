"""Pure guard/protocol controls: never loads UIA or launches native applications."""
import importlib.util
import io
from pathlib import Path
import unittest
import tempfile
from unittest.mock import Mock, patch

SPEC = importlib.util.spec_from_file_location('rendered_management', Path(__file__).resolve().parents[1] / 'run_uno_candidate_ui.py')
s = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(s)


class UiManagementTests(unittest.TestCase):
    def test_review_closed_before_artifact_access(self):
        self.assertFalse(s.UI_SOURCE_REVIEWED)
        args = ['observer', '--executable', 'untrusted.exe', '--sha256', 'a'*64,
                '--authorized-screen-slot', '--work-area', '0', '0', '20', '20']
        with patch.object(s.sys, 'argv', args), patch.object(s.base, 'validate_fixture') as validate:
            with self.assertRaisesRegex(ValueError, 'ui_source_review_and_slot_required'):
                s.main()
            validate.assert_not_called()

    def test_fixed_scenes_and_exact_completed_action_receipt(self):
        self.assertEqual(('ui-ready', 'ui-folder-created', 'ui-folder-renamed', 'ui-folder-deleted'),
                         s.scene_phases('folders'))
        self.assertEqual('ui-cancel', s.scene_phases('snippets')[-1])
        for value in ('settings', '', '../folders', 'Folders'):
            with self.assertRaisesRegex(ValueError, 'ui_scene'):
                s.scene_phases(value)
        s.validate_folder_actions(b'invoke=5\nvalue=2\nfocus=2\n')
        for value in (b'', b'invoke=4\nvalue=2\nfocus=2\n', b'invoke=6\nvalue=2\nfocus=2\n',
                      b'invoke=5\nvalue=2\nfocus=2\nextra'):
            with self.assertRaisesRegex(ValueError, 'ui_folder_actions'):
                s.validate_folder_actions(value)

    def test_folder_scene_is_fixture_only_and_uses_fixed_rendered_controls(self):
        program = (s.REPO / 'scripts/uno_candidate_fixture/Program.cs').read_text()
        helper = s.HELPER.read_text()
        self.assertIn('fixture-folder-scene-permit', program)
        self.assertIn('!RenderedUiMode', program)
        self.assertIn('FileAttributes.ReparsePoint', program)
        self.assertIn("[ValidateSet('snippets','folders')]", helper)
        for key in ('folder-new-name', 'folder-create', 'folder-rename', 'folder-name',
                    'folder-save', 'folder-delete', 'folder-confirm'):
            self.assertIn("'" + key + "' {", helper)
        self.assertIn("$id='PrimaryButton'", helper)
        self.assertIn('$script:invokes++', helper)
        self.assertIn('$script:values++', helper)
        self.assertNotIn('SendKeys', helper)

    def test_slot_required_even_after_source_review(self):
        with patch.object(s, 'UI_SOURCE_REVIEWED', True), patch.object(s.sys, 'platform', 'win32'):
            with self.assertRaises(ValueError):
                s.require_authorization(False)
            s.require_authorization(True)

    def test_no_job_permit_no_profile_or_artifact_access(self):
        with patch.object(s.startup, 'require_job_membership') as job, patch.object(s.base, 'validate_fixture') as validate:
            with self.assertRaisesRegex(ValueError, 'permit'):
                s.worker(None, None, None, None, stream=io.BytesIO(b'wrong\n'))
            job.assert_not_called()
            validate.assert_not_called()

    def test_exact_content_free_helper_protocol(self):
        s.validate_helper_output(0, b'ui_callbacks_observed\r\n', b'')
        for code, out, err in ((1,b'ui_callbacks_observed\n',b''), (0,b'payload',b''),
                               (0,b'ui_callbacks_observed\n',b'warning'), (0,b'',b'')):
            with self.assertRaisesRegex(ValueError, 'ui_helper_protocol'):
                s.validate_helper_output(code, out, err)

    def test_helper_diagnostics_are_bounded_and_never_overwrite_existing_files(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            s.save_helper_diagnostics(root, b'', b'ui_helper_failed stage=cancel reason=schema\n')
            self.assertEqual(b'', (root / 'ui-helper-stdout.log').read_bytes())
            with self.assertRaises(FileExistsError):
                s.save_helper_diagnostics(root, b'replacement', b'')
            self.assertEqual(b'', (root / 'ui-helper-stdout.log').read_bytes())
        with tempfile.TemporaryDirectory() as tmp:
            with self.assertRaisesRegex(ValueError, 'ui_helper_output_limit'):
                s.save_helper_diagnostics(Path(tmp), b'x' * 65537, b'')
            self.assertFalse(list(Path(tmp).iterdir()))

    def test_job_assignment_before_permit_and_zero_before_cleanup(self):
        events=[]
        process=Mock(returncode=0)
        process.communicate.side_effect=lambda *a, **kw: (events.append('permit') or (b'ui_management_observed\n',b''))
        job=Mock()
        def factory(p):
            events.append('assigned'); return job
        with patch.object(s.subprocess, 'CREATE_NO_WINDOW', 0, create=True), \
             patch.object(s.base, 'require_empty_job', side_effect=lambda j: events.append('zero')), \
             patch.object(s.startup, 'finish_job', side_effect=lambda j,p: events.append('cleanup')):
            s.supervise(['owned'],factory,popen=Mock(return_value=process))
        self.assertEqual(['assigned','permit','zero','cleanup'],events)

    def test_failed_protocol_still_cleans_owned_job(self):
        process=Mock(returncode=1); process.communicate.return_value=(b'',b'')
        job=Mock()
        with patch.object(s.subprocess, 'CREATE_NO_WINDOW', 0, create=True), \
             patch.object(s.startup, 'finish_job') as cleanup, patch.object(s.base, 'require_empty_job') as zero:
            with self.assertRaisesRegex(ValueError,'worker_protocol'):
                s.supervise(['owned'],Mock(return_value=job),popen=Mock(return_value=process))
            cleanup.assert_called_once_with(job,process); zero.assert_not_called()

    def test_foreign_foreground_aborts_before_window_attach(self):
        desktop=Mock(); desktop.snapshot.return_value=(99,2)
        with self.assertRaisesRegex(ValueError,'desktop_changed'):
            s.retain_window(Mock(),None,desktop,None,Mock(),(1,2))
        desktop.windows.assert_not_called()

    def test_post_ready_epoch_is_settled_before_any_uia_action(self):
        desktop = Mock(); desktop.snapshot.return_value = (1, 2)
        desktop.windows.return_value = [(0, 0, 10, 10)]
        process = Mock(); process.poll.return_value = None
        with patch.object(s, 'require_all_input_up') as up, \
             patch.object(s, 'quiet_input', side_effect=[ValueError('input'), 77]):
            self.assertEqual(77, s.settle_quiet_epoch(process, desktop, (1, 2), (0, 0, 20, 20),
                                                   clock=lambda: 0, sleep=lambda _: None))
            self.assertEqual(2, up.call_count)
        with patch.object(s, 'require_all_input_up', side_effect=ValueError('input')), \
             patch.object(s, 'quiet_input') as quiet:
            with self.assertRaisesRegex(ValueError, 'input'):
                s.settle_quiet_epoch(process, desktop, (1, 2), (0, 0, 20, 20))
            quiet.assert_not_called()
        with patch.object(s, 'require_all_input_up'), \
             patch.object(s, 'quiet_input', side_effect=ValueError('input_query_failed')):
            with self.assertRaisesRegex(ValueError, 'input_query_failed'):
                s.settle_quiet_epoch(process, desktop, (1, 2), (0, 0, 20, 20))

    def test_shutdown_keeps_foreground_strict_until_monotonic_owned_close(self):
        desktop = Mock()
        desktop.user.GetClipboardSequenceNumber.return_value = 2
        desktop.user.GetForegroundWindow.return_value = 10
        desktop.windows.return_value = [(0, 0, 10, 10)]
        handles = Mock(return_value=[10])
        args = (desktop, 42, 10, (1, 2), (0, 0, 20, 20))
        self.assertFalse(s.shutdown_frame(*args, False, True, handles))
        desktop.user.GetForegroundWindow.return_value = 99
        with self.assertRaisesRegex(ValueError, 'foreground_changed'):
            s.shutdown_frame(*args, False, True, handles)
        handles.return_value = []
        desktop.windows.return_value = []
        for fg in (0, 1, 99):
            desktop.user.GetForegroundWindow.return_value = fg
            self.assertTrue(s.shutdown_frame(*args, False, True, handles))
        desktop.user.GetClipboardSequenceNumber.return_value = 3
        with self.assertRaisesRegex(ValueError, 'clipboard_changed'):
            s.shutdown_frame(*args, True, True, handles)
        desktop.user.GetClipboardSequenceNumber.return_value = 2
        handles.return_value = [10]
        with self.assertRaisesRegex(ValueError, 'window_recreated'):
            s.shutdown_frame(*args, True, True, handles)
        desktop.windows.return_value = [(30, 30, 40, 40)]
        with self.assertRaisesRegex(ValueError, 'geometry'):
            s.shutdown_frame(*args, True, True, handles)

    def test_close_tick_samples_foreground_before_owned_liveness(self):
        events = []
        desktop = Mock()
        desktop.user.GetForegroundWindow.side_effect = lambda: events.append('foreground') or 0
        desktop.user.GetClipboardSequenceNumber.return_value = 2
        desktop.windows.side_effect = lambda _: events.append('rects') or []
        def closed_handles(*args, **kwargs):
            events.append('handles')
            return []
        self.assertTrue(s.shutdown_frame(desktop, 42, 10, (1, 2), (0, 0, 20, 20),
                                         False, True, closed_handles))
        self.assertEqual(['foreground', 'handles', 'rects'], events)

    def test_zero_foreground_allowed_only_during_verified_shutdown(self):
        desktop = Mock()
        desktop.user.GetForegroundWindow.return_value = 0
        desktop.user.GetClipboardSequenceNumber.return_value = 2
        with self.assertRaisesRegex(ValueError, 'desktop_unavailable'):
            s.observe_desktop(desktop)
        self.assertEqual((0, 2), s.observe_desktop(desktop, allow_zero_fg=True))
        desktop.user.GetClipboardSequenceNumber.return_value = 0
        with self.assertRaisesRegex(ValueError, 'desktop_unavailable'):
            s.observe_desktop(desktop, allow_zero_fg=True)
        desktop.validate_input_desktop.side_effect = ValueError('desktop_changed')
        with self.assertRaisesRegex(ValueError, 'desktop_changed'):
            s.observe_desktop(desktop, allow_zero_fg=True)

    def test_final_ack_is_not_written_by_uia_helper(self):
        source = s.HELPER.read_text()
        self.assertNotIn("Ack 'ui-cancel'", source)
        self.assertIn("WaitPhase 'ui-cancel'; Guard", source)

    def test_helper_exact_allowlist_and_no_foreign_discovery_or_input_synthesis(self):
        source=s.HELPER.read_text()
        for required in ('WinUIDesktopWin32WindowClass','FromHandle($h)', 'GetWindowThreadProcessId',
                         'GetLastInputInfo', 'GetAsyncKeyState', 'OpenInputDesktop', 'Default',
                         'GetForegroundWindow', 'HasKeyboardFocus', 'SnippetTextBox',
                         'UIAutomationClient, Version=4.0.0.0', 'UIAutomationTypes, Version=4.0.0.0',
                         'PublicKeyToken=31bf3856ad364e35'):
            self.assertIn(required,source)
        for forbidden in ('RootElement','FindWindow','GetWindowText','SendKeys','SendInput','SetCursorPos',
                          'GetClipboardData','SetClipboardData','SetForegroundWindow'):
            self.assertNotIn(forbidden,source)
        self.assertIn("default { throw 'schema' }",source)

    @unittest.skipUnless(s.sys.platform == 'win32', 'Windows PowerShell pure selector test')
    def test_responsive_leave_selector_executes_actual_function_without_uia(self):
        import subprocess
        helper = str(s.HELPER).replace("'", "''")
        script = r"""
$ErrorActionPreference='Stop'
$tokens=$null; $errors=$null
$ast=[Management.Automation.Language.Parser]::ParseFile('HELPER',[ref]$tokens,[ref]$errors)
if($errors.Count){throw 'parse'}
$function=$ast.FindAll({param($node) $node -is [Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -ceq 'SelectOwnedLeave'},$true)
if($function.Count -ne 1){throw 'function'}
Invoke-Expression $function[0].Extent.Text
$OwnedPid=42
function Item($name,$off=$false,$enabled=$true,$owner=42){
    [pscustomobject]@{Current=[pscustomobject]@{Name=$name; IsOffscreen=$off; IsEnabled=$enabled; ProcessId=$owner}}
}
function Reject($items,$reason){
    try { SelectOwnedLeave $items; throw 'accepted' } catch { if($_.Exception.Message -cne $reason){throw} }
}
if((SelectOwnedLeave @((Item 'New snippet'),(Item 'Back to list' $true))).Current.Name -cne 'New snippet'){throw 'wide'}
if((SelectOwnedLeave @((Item 'New snippet' $true),(Item 'Back to list'))).Current.Name -cne 'Back to list'){throw 'narrow'}
if($null -ne (SelectOwnedLeave @((Item 'Back to list' $false $false)))){throw 'disabled'}
if($null -ne (SelectOwnedLeave @((Item 'New snippet' $true)))){throw 'offscreen'}
Reject @((Item 'New snippet'),(Item 'Back to list')) 'schema'
Reject @((Item 'New snippet'),(Item 'New snippet' $true)) 'schema'
Reject @((Item 'untrusted')) 'schema'
Reject @((Item 'Back to list' $false $true 43)) 'uia_owner'
""".replace('HELPER', helper)
        result = subprocess.run([str(s.system_powershell()), '-NoProfile', '-NonInteractive', '-Command', script],
                                capture_output=True, timeout=15, creationflags=subprocess.CREATE_NO_WINDOW)
        self.assertEqual((0, b'', b''), (result.returncode, result.stdout, result.stderr))

    def test_mode_is_fixed_owned_permit_not_cli_profile_override(self):
        source=(s.REPO/'scripts/uno_candidate_fixture/Program.cs').read_text()
        self.assertIn('fixture-rendered-ui-permit',source)
        self.assertIn('if (args.Length != 0)',source)
        self.assertNotIn('OpenDefault',source)
        driver=(s.REPO/'tests/SmartSnippets.Uno.Tests/Acceptance/UiAssertDriver.cs').read_text()
        for mutation in ('NewAsync(', 'SaveAsync(', 'DeleteFolder(', 'CreateAsync(', '.Text ='):
            self.assertNotIn(mutation,driver)
        self.assertIn('host.Store.Load()',driver)


if __name__ == '__main__':
    unittest.main()
