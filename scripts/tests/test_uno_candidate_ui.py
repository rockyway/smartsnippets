"""Pure guard/protocol controls: never loads UIA or launches native applications."""
import importlib.util
import io
from pathlib import Path
import unittest
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
