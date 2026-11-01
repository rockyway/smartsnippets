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
    def test_picker_close_race_only_accepts_authenticated_exact_handle(self):
        desktop = Mock()
        user = desktop.user
        user.GetWindow.side_effect = lambda h, command: 20 if command == 6 else 10
        def thread(h, pid):
            pid._obj.value = 30
            return 40
        user.GetWindowThreadProcessId.side_effect = thread
        def cls(h, buffer, length):
            if h == 10:
                buffer.value = 'WinUIDesktopWin32WindowClass'
                return 1
            return 0  # dialog disappeared between the GetWindow and class queries
        user.GetClassNameW.side_effect = cls
        user.IsWindowVisible.side_effect = lambda h: h == 10
        user.IsWindow.return_value = False
        self.assertIsNone(s.owned_picker(desktop, 10, 30, 20))
        for authenticated in (None, 21):
            with self.assertRaisesRegex(ValueError, 'ui_picker_identity'):
                s.owned_picker(desktop, 10, 30, authenticated)
        # A distinct live-looking replacement cannot be treated as a vanished handle.
        user.IsWindow.return_value = True
        with self.assertRaisesRegex(ValueError, 'ui_picker_identity'):
            s.owned_picker(desktop, 10, 30, 21)

        # The authenticated live dialog must still pass every identity check.
        user.IsWindowVisible.return_value = True
        user.IsWindowVisible.side_effect = None
        def valid_class(h, buffer, length):
            buffer.value = 'WinUIDesktopWin32WindowClass' if h == 10 else '#32770'
            return 1
        user.GetClassNameW.side_effect = valid_class
        self.assertEqual(20, s.owned_picker(desktop, 10, 30, 20))
        for defect in ('owner', 'pid', 'thread', 'class'):
            with self.subTest(defect=defect):
                user.GetWindow.side_effect = lambda h, command: 20 if command == 6 else (11 if defect == 'owner' else 10)
                def altered_thread(h, pid):
                    pid._obj.value = 31 if defect == 'pid' and h == 20 else 30
                    return 41 if defect == 'thread' and h == 20 else 40
                user.GetWindowThreadProcessId.side_effect = altered_thread
                def altered_class(h, buffer, length):
                    buffer.value = 'other' if defect == 'class' and h == 20 else ('WinUIDesktopWin32WindowClass' if h == 10 else '#32770')
                    return 1
                user.GetClassNameW.side_effect = altered_class
                with self.assertRaisesRegex(ValueError, 'ui_picker_identity'):
                    s.owned_picker(desktop, 10, 30, 20)

    def test_picker_foreground_close_transition(self):
        desktop = Mock()
        desktop.user.IsWindow.return_value = False
        self.assertTrue(s.picker_foreground(desktop, 20, None, 20))
        self.assertFalse(s.picker_foreground(desktop, 21, None, 20))
        self.assertFalse(s.picker_foreground(desktop, 20, None, None))
        desktop.user.IsWindow.return_value = True
        desktop.user.IsWindowVisible.return_value = True
        self.assertFalse(s.picker_foreground(desktop, 20, None, 20))
        desktop.user.IsWindowVisible.return_value = False
        self.assertTrue(s.picker_foreground(desktop, 20, None, 20))
        self.assertFalse(s.picker_foreground(desktop, 20, 21, 20))
        self.assertTrue(s.picker_foreground(desktop, 20, 20, 20))
        self.assertTrue(s.picker_foreground(desktop, 20, 20, None))

    def test_picker_refusals_are_recorded_without_collapse(self):
        import ast
        source = (s.REPO / 'scripts/run_uno_candidate_ui.py').read_text()
        tree = ast.parse(source)
        tokens = {node.exc.args[0].value for node in ast.walk(tree)
                  if isinstance(node, ast.Raise) and isinstance(node.exc, ast.Call)
                  and node.exc.args and isinstance(node.exc.args[0], ast.Constant)
                  and isinstance(node.exc.args[0].value, str)
                  and node.exc.args[0].value.startswith('ui_picker_')}
        self.assertTrue(tokens)
        import json
        for token in tokens:
            with self.subTest(token=token), tempfile.TemporaryDirectory() as directory:
                root = Path(directory)
                s.base.record_failure(root, ValueError(token))
                self.assertEqual(token, json.loads(next(root.glob('observer-*-failure.json')).read_text())['reason'])

    def test_dangling_marker_is_never_treated_as_absent(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            dangling = root / 'picker-import-cancel.active'
            real = s.os.path.lexists
            with patch.object(s.os.path, 'lexists', side_effect=lambda p: True if p == dangling else real(p)):
                with self.assertRaises((FileNotFoundError, ValueError)):
                    s.picker_phase(root)
            (root / 'picker-import-cancel.active').write_bytes(b'owned_picker_019\n')
            dangling = root / 'picker-import-cancel.closed'
            with patch.object(s.os.path, 'lexists', side_effect=lambda p: True if p == dangling else real(p)):
                with self.assertRaises((FileNotFoundError, ValueError)):
                    s.picker_phase(root)

    def test_picker_helper_latches_and_publishes_before_info(self):
        helper = s.HELPER.read_text()
        guard = helper.split('    function Guard', 1)[1].split('    function SelectOwnedLeave', 1)[0]
        self.assertLess(guard.index('$script:pickerHwnd = $dialog'), guard.index('$pickerForeground ='))
        self.assertIn("throw 'picker_replaced'", guard)
        self.assertIn('if ($pickerForeground -or $fg -eq $OwnedHwnd) { $script:focused = $true }', guard)
        self.assertLess(guard.index("throw 'foreground'"), guard.index('$script:focused = $true'))
        marker = helper.split('    function PickerMarker', 1)[1].split('    function PickerElement', 1)[0]
        self.assertIn('[IO.FileMode]::CreateNew', marker)
        self.assertLess(marker.index('$file.Dispose()'), marker.index('[IO.File]::Move($tmp, $final)'))
        picker = helper.split('    function RunPicker', 1)[1].split("    $stage = 'ready'", 1)[0]
        self.assertLess(picker.index("PickerMarker 'closed'"), picker.index("Invoke 'picker-info'"))
        self.assertIn('[IO.Directory]::EnumerateFileSystemEntries($files)', picker)
        self.assertIn('$fileInfo.Links -ne 1', picker)
        self.assertIn("$id='ImportSnippetsButton'", helper)
        self.assertIn("$id='ExportSnippetsButton'", helper)
        import re
        tokens = set(re.findall(r"throw '(picker_[a-z_]+)'", helper))
        allow = helper.split('    $allowed = @(', 1)[1].split(')', 1)[0]
        for token in tokens:
            self.assertIn("'" + token + "'", allow)

    def test_picker_owned_bytes_refuse_aliases_and_both_size_caps(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            for filename, maximum in (('import.json', 4096), ('export.json', 16384)):
                path = root / filename
                path.write_bytes(b'x' * (maximum + 1))
                with self.assertRaisesRegex(ValueError, 'evidence_file'):
                    s.base.owned_bytes(path, root, maximum)
                path.write_bytes(b'x')
                alias = root / (filename + '.alias')
                alias.hardlink_to(path)
                with self.assertRaisesRegex(ValueError, 'evidence_file'):
                    s.base.owned_bytes(path, root, maximum)
                alias.unlink()
                self.assertEqual(b'x', s.base.owned_bytes(path, root, maximum))

    def test_picker_fixture_budget_permit_and_privacy_canaries(self):
        program = (s.REPO / 'scripts/uno_candidate_fixture/Program.cs').read_text()
        picker_permit = program.split('var pickerPermit', 1)[1].split('FilePickerScene = true', 1)[0]
        self.assertIn('info.Length != 25', picker_permit)
        self.assertIn('FilePickerAssertDriver.RequirePickerPermit(pickerPermit)', picker_permit)
        driver = (s.REPO / 'tests/SmartSnippets.Uno.Tests/Acceptance/UiAssertDriver.cs').read_text()
        self.assertIn('ReadPickerFile(path, 25, 25)', driver)
        self.assertIn('AddSeconds(FilePickerScene ? 25 : 12)', program)
        source = (s.REPO / 'scripts/run_uno_candidate_ui.py').read_text()
        for token in ('picker-tag', 'picker-files', 'import.json', 'export.json'):
            self.assertIn("'" + token + "'", source.split('canaries =', 1)[1].split('privacy.verify_negative_controls', 1)[0])
        self.assertIn("base.owned_bytes(files / 'import.json', private, 4096)", source)
        self.assertIn("base.owned_bytes(files / 'export.json', private, 16384)", source)

    def test_picker_identity_refuses_foreign_owner_pid_thread_class_and_main(self):
        args = [10, 20, 10, 30, 30, 40, 40, '#32770', True]
        self.assertEqual(20, s.validate_picker_identity(*args))
        for index, value in ((1, 0), (1, 10), (2, 11), (3, 31), (5, 0), (5, 41),
                             (7, 'other'), (8, False)):
            altered = args.copy()
            altered[index] = value
            with self.assertRaisesRegex(ValueError, 'ui_picker_identity'):
                s.validate_picker_identity(*altered)

    def test_picker_markers_refuse_overlap_reordering_and_wrong_content(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            self.assertIsNone(s.picker_phase(root))
            first = root / 'picker-import-cancel.active'
            first.write_bytes(b'owned_picker_019\n')
            self.assertEqual('import-cancel', s.picker_phase(root))
            second = root / 'picker-import.active'
            second.write_bytes(b'owned_picker_019\n')
            with self.assertRaisesRegex(ValueError, 'ui_picker_phase'):
                s.picker_phase(root)
            (root / 'picker-import-cancel.closed').write_bytes(b'owned_picker_019\n')
            self.assertEqual('import', s.picker_phase(root))
            first.unlink()
            with self.assertRaisesRegex(ValueError, 'ui_picker_phase'):
                s.picker_phase(root)
            first.write_bytes(b'arbitrary dialog\n')
            with self.assertRaisesRegex(ValueError, 'ui_picker_phase'):
                s.picker_phase(root)

    def test_picker_phase_rejects_hardlinked_private_evidence(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            source = root / 'owned-synthetic-marker'
            source.write_bytes(b'owned_picker_019\n')
            (root / 'picker-import-cancel.active').hardlink_to(source)
            with self.assertRaisesRegex(ValueError, 'evidence_file'):
                s.picker_phase(root)

    def test_picker_per_phase_action_receipts_are_exact(self):
        for phase in s.PICKER_PHASES:
            value = b'invoke=1\npicker=1\nvalue=0\n' if phase.endswith('-cancel') else b'invoke=2\npicker=1\nvalue=1\n'
            s.validate_picker_phase_actions(phase, value)
            for bad in (b'', value + b'extra', value.replace(b'picker=1', b'picker=2')):
                with self.assertRaisesRegex(ValueError, 'ui_picker_phase_actions'):
                    s.validate_picker_phase_actions(phase, bad)

    def test_picker_action_receipt_rejects_missing_duplicate_or_extra_mutation(self):
        s.validate_picker_actions(b'main=4\npicker=4\nvalue=2\ninfo=2\n')
        for value in (b'', b'main=5\npicker=4\nvalue=2\ninfo=2\n',
                      b'main=4\npicker=5\nvalue=2\ninfo=2\n',
                      b'main=4\npicker=4\nvalue=3\ninfo=2\n',
                      b'main=4\npicker=4\nvalue=2\ninfo=2\nextra'):
            with self.assertRaisesRegex(ValueError, 'ui_picker_actions'):
                s.validate_picker_actions(value)
        self.assertEqual(('ui-ready', 'ui-import-cancelled', 'ui-imported',
                          'ui-export-cancelled', 'ui-exported'), s.scene_phases('file-picker'))

    def test_picker_ps_is_fixed_owned_schema_without_generic_discovery(self):
        helper = s.HELPER.read_text()
        picker = helper.split('    function RunPicker', 1)[1].split("    $stage = 'ready'", 1)[0]
        ownership = helper.split('    function OwnedPicker', 1)[1].split('    function Guard', 1)[0]
        for fragment in ('GetWindow($h, 6)', 'GetWindow($dialog, 4)', '$dialogOwner -ne $h',
                         '$dialogPid -ne $OwnedPid', "$cls.ToString() -cne '#32770'", '$thread -ne $mainThread'):
            self.assertIn(fragment, ownership)
        self.assertLess(ownership.index('GetWindow($dialog, 4)'), ownership.index('IsWindowVisible($dialog)'))
        self.assertLess(ownership.index('IsWindowVisible($dialog)'), ownership.index('IsWindow($dialog)'))
        self.assertIn("PickerElement $dialog '1148' $r", picker)
        self.assertIn("PickerElement $dialog '1001' $e", picker)
        self.assertIn("$items.Count -ne 1", helper.split('    function PickerElement', 1)[1])
        for fragment in ('ComboBox', 'Edit', 'Button', 'picker_existing_export', 'picker_alias'):
            self.assertIn(fragment, picker)
        self.assertEqual(1, picker.count('$value.SetValue($path)'))
        self.assertEqual(1, picker.count('$invoke.Invoke()'))
        for forbidden in ('RootElement', 'SendKeys', 'GetWindowText', 'SetFocus', 'SetForegroundWindow'):
            self.assertNotIn(forbidden, picker)
        self.assertIn('IsChild($dialog, $native)', helper)
        self.assertFalse(s.UI_SOURCE_REVIEWED)

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
        self.assertEqual(('ui-ready', 'ui-new', 'ui-saved', 'ui-cancel', 'ui-dirty-saved',
                          'ui-discard-dirty', 'ui-discarded'), s.scene_phases('snippets'))
        self.assertEqual(('ui-ready', 'ui-settings-theme', 'ui-settings-size', 'ui-settings-reloaded'),
                         s.scene_phases('settings'))
        for value in ('', '../folders', 'Folders', 'Settings'):
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
        self.assertIn("[ValidateSet('snippets','folders','settings','file-picker')]", helper)
        for key in ('folder-new-name', 'folder-create', 'folder-rename', 'folder-name',
                    'folder-save', 'folder-delete', 'folder-confirm'):
            self.assertIn("'" + key + "' {", helper)
        self.assertIn("$id='PrimaryButton'", helper)
        self.assertIn('$script:invokes++', helper)
        self.assertIn('$script:values++', helper)
        self.assertNotIn('SendKeys', helper)

    def test_settings_receipt_counts_scroll_separately_and_requires_exact_actions(self):
        s.validate_settings_actions(b'scroll=2\nselect=2\n')
        for value in (b'', b'scroll=1\nselect=2\n', b'scroll=2\nselect=3\n',
                      b'scroll=2\nselect=2\nextra', b'invoke=2\n'):
            with self.assertRaisesRegex(ValueError, 'ui_settings_actions'):
                s.validate_settings_actions(value)

    def test_settings_scene_has_fixed_group_radio_scroll_select_and_no_fallback(self):
        helper = s.HELPER.read_text()
        program = (s.REPO / 'scripts/uno_candidate_fixture/Program.cs').read_text()
        driver = (s.REPO / 'tests/SmartSnippets.Uno.Tests/Acceptance/UiAssertDriver.cs').read_text()
        self.assertIn('fixture-settings-scene-permit', program)
        self.assertIn('!RenderedUiMode || FolderScene', program)
        self.assertIn('info.Length != 22', program)
        for identity in ("$groupId='ThemeRadios'", "$groupId='TextSizeRadios'",
                         "$radioName='Dark'", "$radioName='Large'"):
            self.assertIn(identity, helper)
        select = helper.split('    function SelectSettingsRadio', 1)[1].split('    function Invoke', 1)[0]
        self.assertEqual(1, select.count('$scroll.ScrollIntoView()'))
        self.assertEqual(1, select.count('$selection.Select()'))
        self.assertNotIn('SetFocus', select)
        self.assertNotIn('while', select)
        self.assertIn('FindSettingsRadio $key $true', select)
        self.assertIn('GetClipboardSequenceNumber() -ne $clipboardSequence', helper)
        settings = driver.split('    internal const string FolderName', 1)[0]
        self.assertNotIn('.Set(', settings)
        self.assertNotIn('.Save(', settings)
        self.assertNotIn('SelectedTheme =', settings)
        self.assertIn('new SettingsRepository(new SqliteConnectionFactory(database))', settings)

    def test_snippet_action_receipts_count_fixed_dialog_actions_and_responsive_back_only(self):
        for backs in range(3):
            s.validate_snippet_actions(f'invoke={8 + backs}\nvalue=3\nfocus=4\nback={backs}\n'.encode())
        for value in (b'', b'invoke=8\nvalue=2\nfocus=4\nback=0\n',
                      b'invoke=8\nvalue=3\nfocus=4\nback=1\n',
                      b'invoke=11\nvalue=3\nfocus=4\nback=3\n',
                      b'invoke=8\nvalue=3\nfocus=4\nback=0\nextra'):
            with self.assertRaisesRegex(ValueError, 'ui_snippet_actions'):
                s.validate_snippet_actions(value)

    def test_dirty_dialog_buttons_have_exact_names_template_ids_and_types(self):
        source = s.HELPER.read_text()
        for name, identity in (('Keep editing', 'CloseButton'), ('Save', 'PrimaryButton'), ('Discard', 'SecondaryButton')):
            self.assertIn(f"$name='{name}'; $id='{identity}'; $type=[Windows.Automation.ControlType]::Button", source)
        self.assertEqual(3, source.count('        RequestSnippetNew') + source.count("; RequestSnippetNew"))
        driver = (s.REPO / 'tests/SmartSnippets.Uno.Tests/Acceptance/UiAssertDriver.cs').read_text()
        assertions = driver.split('internal static void RequireCancelled', 1)[1].split('/// <summary>Explicit fixture setup', 1)[0]
        for forbidden in ('.SaveAsync(', '.CreateSnippet(', '.UpdateSnippet(', '.ResetToSaved(', '.NewAsync('):
            self.assertNotIn(forbidden, assertions)
        self.assertIn('ReferenceEquals(host.Snippets.Editor, discardedEditor)', assertions)
        self.assertIn('host.Store.Load()', assertions)

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

    def test_foreground_failure_preserves_captured_sample_and_logs_only_relation(self):
        import json
        desktop = Mock()
        def owner(hwnd, target):
            target._obj.value = 42 if hwnd == 20 else 99
            return 1
        desktop.user.GetWindowThreadProcessId.side_effect = owner
        for fg, relation in ((0, 'none'), (1, 'initial'), (10, 'owned_main'), (20, 'owned_other'), (30, 'external')):
            with tempfile.TemporaryDirectory() as tmp:
                error = s.ForegroundChanged(fg)
                self.assertEqual('foreground_changed', str(error))
                s.save_foreground_failure(Path(tmp), desktop, error.foreground, (1, 2), 10, 42)
                self.assertEqual({'foregroundRelation': relation}, json.loads((Path(tmp) / 'foreground-failure.json').read_text()))
        desktop.user.GetForegroundWindow.assert_not_called()
        desktop.user.GetWindowThreadProcessId.side_effect = None
        desktop.user.GetWindowThreadProcessId.return_value = 0
        with tempfile.TemporaryDirectory() as tmp:
            s.save_foreground_failure(Path(tmp), desktop, 30, (1, 2), 10, 42)
            self.assertEqual({'foregroundRelation': 'unknown'}, json.loads((Path(tmp) / 'foreground-failure.json').read_text()))

    def test_fixture_exit_diagnostic_is_numeric_and_never_overwrites(self):
        import json
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            s.save_fixture_exit(root, -1073741819)
            self.assertEqual({'exitCode': -1073741819}, json.loads((root / 'fixture-exit.json').read_text()))
            with self.assertRaises(FileExistsError):
                s.save_fixture_exit(root, 0)
        for code in (None, True, 'payload'):
            with tempfile.TemporaryDirectory() as tmp:
                with self.assertRaisesRegex(ValueError, 'ui_exit_code'):
                    s.save_fixture_exit(Path(tmp), code)
                self.assertFalse(list(Path(tmp).iterdir()))

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
        self.assertNotIn("Ack 'ui-discarded'", source)
        self.assertIn("WaitPhase 'ui-discarded'; Guard", source)
        self.assertIn("Ack 'ui-cancel'", source)
        self.assertIn("Ack 'ui-discard-dirty'", source)

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
