"""Pure bootstrap/build/protocol tests: never start a native process."""
from dataclasses import fields, replace
import importlib.util
import io
from pathlib import Path
import subprocess
import sys
import tempfile
import uuid
import os
import unittest
from unittest.mock import patch

SCRIPT = Path(__file__).resolve().parents[1] / 'run_uno_candidate_native.py'
spec = importlib.util.spec_from_file_location('native_bootstrap', SCRIPT)
native = importlib.util.module_from_spec(spec)
spec.loader.exec_module(native)


class NativeBootstrapTests(unittest.TestCase):
    def test_prelaunch_session_metadata_is_registered_only_and_not_readiness(self):
        private = Path('ss-uno018-ui-owned') / ('ss-uno019-native-' + 'a' * 32)
        with patch('builtins.print') as output:
            for scene in ('restore', 'abort', 'owner-refusal', 'posted-enter', 'posted-row', 'posted-main', 'copy', 'preview', 'expired'):
                native.announce_registered_session(scene, private)
            output.assert_not_called()
            native.announce_registered_session('registered-enter', private)
            output.assert_called_once()
            self.assertEqual({'phase': 'registered_session_prelaunch', 'scene': 'registered-enter',
                              'privateEvidenceToken': private.as_posix()}, native.json.loads(output.call_args.args[0]))
        with self.assertRaisesRegex(ValueError, 'session_token'):
            native.announce_registered_session('registered-enter', Path('arbitrary/session'))

    def test_registered_readiness_requires_owned_exact_literal(self):
        with tempfile.TemporaryDirectory() as directory:
            private = Path(directory)
            self.assertFalse(native.registered_operator_ready(private))
            path = private / native.REGISTERED_READY_FILE
            path.write_bytes(native.REGISTERED_READY_RECEIPT)
            self.assertTrue(native.registered_operator_ready(private))
            with patch.object(native.helpers, 'owned_bytes', side_effect=ValueError('alias')):
                with self.assertRaises(ValueError): native.registered_operator_ready(private)
            for data in (b'', native.REGISTERED_READY_RECEIPT + b'extra', b'operator ready'):
                path.write_bytes(data)
                with self.assertRaisesRegex(ValueError, 'readiness'): native.registered_operator_ready(private)

    def test_registered_exact_result_also_requires_readiness_artifact(self):
        with tempfile.TemporaryDirectory() as directory:
            private = Path(directory)
            logs = private / 'native-fixture-profile/logs'
            logs.mkdir(parents=True)
            (logs / 'app.log').write_bytes(b'fixed INFO candidate_started\nfixed INFO candidate_shutdown_complete\n')
            expected = b'native_result=registered-enter_settled'
            good = b'native_phase=guardian_ready\nnative_phase=awaiting_owned_a\n' + expected + b'\n'
            with self.assertRaisesRegex(ValueError, 'readiness'):
                native.verify_result(private, [good, b''], expected, True)
            (private / native.REGISTERED_READY_FILE).write_bytes(native.REGISTERED_READY_RECEIPT)
            native.verify_result(private, [good, b''], expected, True)
            for data in (good + b'extra\n', good.replace(b'registered-enter', b'posted-enter')):
                with self.assertRaisesRegex(ValueError, 'receipt'):
                    native.verify_result(private, [data, b''], expected, True)

    def test_registered_proof_does_not_change_posted_labels(self):
        self.assertEqual('registered_os_shortcut_operator_delivered', native.shortcut_proof('registered-enter'))
        for scene in ('posted-enter', 'posted-row', 'posted-main'):
            self.assertEqual('registered_posted_callback_not_physical', native.shortcut_proof(scene))
        self.assertIn('registered-enter', native.INSERT_SCENES)
        self.assertIn('registered-enter', native.INPUT_CAPABILITY_SCENES)

    def test_registered_monitor_announces_once_after_exact_owned_readiness(self):
        class Process:
            returncode = 0
            polls = 0
            def poll(self):
                self.polls += 1
                return None if self.polls < 4 else 0
        class User:
            def GetForegroundWindow(self): return 1
        class Desktop:
            user = User()
            def validate_input_desktop(self): pass
        with patch.object(native, 'positive_foregrounds', return_value={1}), patch.object(native, 'registered_operator_ready', side_effect=[False, True]) as ready, patch('builtins.print') as output:
            native.monitor(Process(), Desktop(), 1, [], sleep=lambda _: None, positive_private=Path('private'), registered=True)
            self.assertEqual(2, ready.call_count)
            output.assert_called_once_with('native_phase=operator_ready press_Ctrl_Alt_Shift_F11_then_release_all_input', flush=True)

    def test_source_gate_precedes_any_launch_or_profile_access(self):
        with patch.object(native.subprocess, 'Popen') as start, patch.object(native, 'validate_build') as validate:
            with self.assertRaisesRegex(ValueError, 'review'):
                native.main(['--scene', 'restore', '--authorized-screen-slot'])
            start.assert_not_called()
            validate.assert_not_called()

    def test_cli_fails_closed_without_native_launch(self):
        result = subprocess.run([sys.executable, '-I', '-B', str(SCRIPT), '--scene', 'abort'],
                                capture_output=True, timeout=10)
        self.assertEqual(2, result.returncode)
        self.assertEqual(b'native_harness_blocked\n', result.stderr.replace(b'\r\n', b'\n'))

    def test_fixed_scene_allowlist_rejects_unapproved_input(self):
        with patch.object(native.sys, 'stderr', io.StringIO()), self.assertRaises(SystemExit):
            native.main(['--scene', 'paste'])

    def test_source_fingerprint_binds_linked_sources_and_changes(self):
        first = native.source_fingerprint()
        self.assertRegex(first, '^[0-9a-f]{64}$')
        self.assertEqual(first, native.source_fingerprint())

    def test_artifact_basename_and_digest_refuse_before_read(self):
        with self.assertRaisesRegex(ValueError, 'identity'):
            native.validate_image('other.exe', '0' * 64, native.ENTRY)
        with self.assertRaisesRegex(ValueError, 'identity'):
            native.validate_image(native.ENTRY, 'bad', native.ENTRY)

    def test_wrong_directory_is_rejected_before_any_artifact_bytes(self):
        for basename in (native.ENTRY, 'SmartSnippets.Target018.exe', 'SmartSnippets.UnoCandidate.Guardian.exe'):
            with patch.object(native.helpers, 'owned_bytes') as read:
                with self.assertRaisesRegex(ValueError, 'provenance'):
                    native.validate_image(Path('untrusted') / basename, 'a' * 64, basename)
                read.assert_not_called()

    def test_exact_ordered_receipts_and_private_log_privacy_are_required(self):
        private = Path(tempfile.gettempdir()) / 'owned-parent-directory' / 'owned-child-directory'
        expected = b'native_result=regular_restored'
        good = b'native_phase=guardian_ready\nnative_phase=published\n' + expected + b'\n'
        log = b'fixed INFO candidate_started\nfixed INFO candidate_shutdown_complete\n'
        with patch.object(native.helpers, 'owned_bytes', return_value=log):
            native.verify_result(private, [good, b''], expected)
            owner_expected = b'native_result=owner_refusal_restored'
            owner_good = b'native_phase=guardian_ready\nnative_phase=published\nnative_phase=owner_refused\n' + owner_expected + b'\n'
            native.verify_result(private, [owner_good, b''], owner_expected)
            with self.assertRaisesRegex(ValueError, 'native_result_receipt'):
                native.verify_result(private, [owner_good.replace(b'native_phase=owner_refused\n', b''), b''], owner_expected)
            for output in (expected + b'\n', b'foreign\n' + good, good + b'extra\n', good.replace(b'published', b'wrong')):
                with self.assertRaisesRegex(ValueError, 'native_result_receipt'):
                    native.verify_result(private, [output, b''], expected)
            with self.assertRaisesRegex(ValueError, 'native_result_receipt'):
                native.verify_result(private, [good, b'warning'], expected)
        for invalid in (b'not ready', log + b'fixed ERROR candidate_cleanup_failed\n'):
            with patch.object(native.helpers, 'owned_bytes', return_value=invalid):
                with self.assertRaisesRegex(ValueError, 'native_host_readiness_cleanup'):
                    native.verify_result(private, [good, b''], expected)
        with patch.object(native.helpers, 'owned_bytes', return_value=log + b'second line'):
            with self.assertRaisesRegex(ValueError, 'native_privacy'):
                native.verify_result(private, [good, b''], expected)

    def test_monitor_never_force_cleans_pending_coordinator(self):
        class Process:
            returncode = 0
            polls = 0
            def poll(self):
                self.polls += 1
                return None if self.polls < 4 else 0
            def kill(self):
                raise AssertionError('must not kill guardian/coordinator')
        class User:
            def GetForegroundWindow(self): return 1
        class Desktop:
            user = User()
            def validate_input_desktop(self): pass
        process = Process()
        native.monitor(process, Desktop(), 1, [], sleep=lambda _: None)
        self.assertEqual(4, process.polls)

    def test_oracle_failure_is_failure_without_process_tree_cleanup(self):
        class Process:
            returncode = 0
            polls = 0
            def poll(self):
                self.polls += 1
                return None if self.polls < 3 else 0
        class User:
            def GetForegroundWindow(self): return 2
        class Desktop:
            user = User()
            def validate_input_desktop(self): pass
        with self.assertRaisesRegex(ValueError, 'failed'):
            native.monitor(Process(), Desktop(), 1, [], sleep=lambda _: None)

    def test_positive_review_and_operator_attestations_precede_image_or_root_work(self):
        with patch.object(native, 'require_authorization'), patch.object(native, 'validate_build') as validate, patch.object(native.subprocess, 'Popen') as start:
            for scene in native.POSITIVE_SCENES:
                with self.assertRaisesRegex(ValueError, 'positive_review'):
                    native.main(['--scene', scene, '--row6-controls-verified', '--operator-recopied-baseline'])
            validate.assert_not_called()
            start.assert_not_called()
        with patch.object(native, 'POSITIVE_SCENES_SOURCE_REVIEWED', True), patch.object(native, 'POSITIVE_INPUT_SOURCE_REVIEWED', True):
            for row6, recopied in ((False, True), (True, False), (False, False)):
                with self.assertRaisesRegex(ValueError, 'operator_recopy'):
                    native.require_positive_authorization('copy', row6, recopied)
            native.require_positive_authorization('copy', True, True)
        self.assertFalse(native.POSITIVE_SCENES_SOURCE_REVIEWED)
        self.assertFalse(native.POSITIVE_INPUT_SOURCE_REVIEWED)

    def test_input_has_separate_source_gate_even_after_positive_review(self):
        with patch.object(native, 'POSITIVE_SCENES_SOURCE_REVIEWED', True):
            native.require_positive_authorization('copy', True, True)
            for scene in native.INPUT_CAPABILITY_SCENES:
                with self.assertRaisesRegex(ValueError, 'positive_review'):
                    native.require_positive_authorization(scene, True, True)

    def test_positive_observer_accepts_only_distinct_exact_metadata(self):
        with tempfile.TemporaryDirectory() as directory:
            private = Path(directory)
            self.assertEqual({11}, native.positive_foregrounds(private, 11))
            path = private / 'positive-surfaces.txt'
            path.write_text('1 2 3 4', encoding='ascii')
            self.assertEqual({0, 1, 2, 3, 4, 11}, native.positive_foregrounds(private, 11))
            for text in ('1 1 3 4', '0 2 3 4', '1 2 3 4 extra', '-1 2 3 4', '1 2 3 9223372036854775808'):
                path.write_text(text, encoding='ascii')
                with self.assertRaisesRegex(ValueError, 'surfaces'):
                    native.positive_foregrounds(private, 11)

    def test_positive_receipt_requires_phase_order_and_complete_actual_host_shutdown(self):
        private = Path(tempfile.gettempdir()) / 'owned-parent-directory' / 'owned-child-directory'
        expected = b'native_result=posted-enter_settled'
        good = b'native_phase=guardian_ready\nnative_phase=awaiting_owned_a\n' + expected + b'\n'
        log = b'fixed INFO candidate_started\nfixed INFO candidate_shutdown_complete\n'
        with patch.object(native.helpers, 'owned_bytes', return_value=log):
            native.verify_result(private, [good, b''], expected, True)
            with self.assertRaisesRegex(ValueError, 'receipt'):
                native.verify_result(private, [good.replace(b'awaiting_owned_a', b'published'), b''], expected, True)
            with self.assertRaisesRegex(ValueError, 'receipt'):
                native.verify_result(private, [good + b'native_result=KEYBOARD_RECOVERY_PENDING\n', b''], expected, True)
        with patch.object(native.helpers, 'owned_bytes', return_value=b'fixed INFO candidate_started\n'):
            with self.assertRaisesRegex(ValueError, 'readiness_cleanup'):
                native.verify_result(private, [good, b''], expected, True)

    def test_build_capture_does_not_require_or_grant_native_authorization(self):
        with tempfile.TemporaryDirectory() as directory:
            output = Path(directory) / 'source.sha256'
            native.main(['--capture-source', str(output)])
            self.assertEqual(native.source_fingerprint() + '\n', output.read_text())
        self.assertFalse(native.NATIVE_SCENES_SOURCE_REVIEWED)


class PriorRestorativeLedgerTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory(prefix='ss-uno018-ui-')
        self.addCleanup(self.temporary.cleanup)
        self.private = Path(self.temporary.name) / ('ss-uno019-native-' + 'a' * 32)
        self.private.mkdir()
        self.stdout = self.private / 'stdout.log'
        self.stdout.write_bytes(b'native_phase=guardian_ready\nnative_phase=published\nnative_result=regular_restored\n')
        (self.private / 'stderr.log').write_bytes(b'')
        self.control = self.private / 'recovery-control.txt'
        self.control.write_bytes(b'ss-uno019-recovery-00112233445566778899aabbccddeeff\n')
        self.log = self.private / 'native-fixture-profile/logs/app.log'
        self.log.parent.mkdir(parents=True)
        self.log.write_bytes(b'fixed INFO candidate_started\nfixed INFO candidate_shutdown_complete\n')
        self.witness = native.RestorativeSessionObservation(self.private, 'restore', *(['a' * 64] * 4), 0, b'',
                       'clipboard_control_settled', self.private.parent.name + '/' + self.private.name)
        self.acl = patch.object(native, 'require_owner_private')
        self.acl.start()
        self.addCleanup(self.acl.stop)

    def test_known_token_has_exact_dotnet_guid_endianness_and_immutable_witness(self):
        self.assertEqual(bytes.fromhex('33221100554477668899aabbccddeeff'), native.verified_prior_token(self.witness))
        for field in fields(self.witness):
            with self.subTest(field=field.name), self.assertRaises(AttributeError):
                setattr(self.witness, field.name, None)

    def test_registered_handoff_checks_readiness_file_acl_before_trusting_receipt(self):
        self.stdout.write_bytes(b'native_phase=guardian_ready\nnative_phase=awaiting_owned_a\nnative_result=registered-enter_settled\n')
        ready = self.private / native.REGISTERED_READY_FILE
        ready.write_bytes(native.REGISTERED_READY_RECEIPT)
        witness = replace(self.witness, scene='registered-enter', result='positive_scene_settled')
        with patch.object(native, 'require_owner_private') as checked:
            native.verified_prior_token(witness)
            checked.assert_any_call(ready, protected=False)
        def deny_ready(path, **kwargs):
            if path == ready:
                raise ValueError('native_ledger_owner_acl')
        with patch.object(native, 'require_owner_private', side_effect=deny_ready):
            with self.assertRaisesRegex(ValueError, 'owner_acl'):
                native.verified_prior_token(witness)

    def test_handoff_is_keyword_only_and_cannot_be_supplied_by_cli(self):
        with self.assertRaises(TypeError):
            native.main([], self.witness)
        with patch.object(native.sys, 'stderr', io.StringIO()), self.assertRaises(SystemExit):
            native.main(['--prior-restorative-session', str(self.private)])

    def test_untrusted_or_incomplete_parent_observations_never_grant(self):
        for changes in ({'scene': 'copy'}, {'scene': 'preview'}, {'scene': 'expired'}, {'parent_exit_code': 2},
                        {'parent_stderr': b'failure'}, {'result': 'failed'}, {'source_sha256': ''},
                        {'entry_sha256': 'bad'}, {'private_evidence_token': 'other/session'}):
            with self.subTest(changes=changes), self.assertRaises(ValueError):
                native.verified_prior_token(replace(self.witness, **changes))
        with self.assertRaises(ValueError):
            native.verified_prior_token(self.private)

    def test_ordered_receipts_pending_failure_stderr_and_privacy_rejected(self):
        original = self.stdout.read_bytes()
        for receipt in (original + b'RECOVERY_PENDING\n', original.replace(b'regular_restored', b'failed'),
                        original.replace(b'published', b'awaiting_owned_a'), original[::-1]):
            self.stdout.write_bytes(receipt)
            with self.assertRaises(ValueError): native.verified_prior_token(self.witness)
        self.stdout.write_bytes(original)
        (self.private / 'stderr.log').write_bytes(b'error')
        with self.assertRaises(ValueError): native.verified_prior_token(self.witness)
        (self.private / 'stderr.log').write_bytes(b'')
        self.log.write_bytes(self.log.read_bytes() + b'second line')
        with self.assertRaisesRegex(ValueError, 'privacy'): native.verified_prior_token(self.witness)

    def test_recovery_token_exact_frame_zero_and_malformed_rejected(self):
        for control in (b'ss-uno019-recovery-' + b'0' * 32 + b'\n',
                        b'ss-uno019-recovery-' + b'a' * 32, self.control.read_bytes() + b'extra',
                        b'ss-uno019-recovery-' + b'A' * 32 + b'\n'):
            self.control.write_bytes(control)
            with self.assertRaises(ValueError): native.verified_prior_token(self.witness)

    def test_hardlinks_and_alias_spelling_rejected(self):
        alias = self.private / 'hardlink'
        os.link(self.control, alias)
        with self.assertRaises(ValueError): native.verified_prior_token(self.witness)
        alias.unlink()
        with self.assertRaises(ValueError):
            native.verified_prior_token(replace(self.witness, session=self.private / '..' / self.private.name))

    def test_owner_acl_failure_does_not_fall_back_to_directory_trust(self):
        with patch.object(native, 'require_owner_private', side_effect=ValueError('native_ledger_owner_acl')):
            with self.assertRaisesRegex(ValueError, 'owner_acl'): native.verified_prior_token(self.witness)

    def test_legacy_and_prior_permit_frames_have_exact_sizes(self):
        token = uuid.UUID('00112233-4455-6677-8899-aabbccddeeff').bytes_le
        self.assertEqual(b'owned_clipboard_controls_only\n', native.launch_permit(False))
        self.assertEqual(native.POSITIVE_PERMIT, native.launch_permit(True))
        for positive, header in ((True, native.PRIOR_POSITIVE_PERMIT), (False, native.PRIOR_CONTROL_PERMIT)):
            frame = native.launch_permit(positive, token)
            self.assertEqual(header + token, frame)
            self.assertEqual(len(header) + 16, len(frame))
        for invalid in (b'', b'1' * 15, b'1' * 17, bytes(16)):
            with self.assertRaises(ValueError): native.launch_permit(True, invalid)


if __name__ == '__main__':
    unittest.main()
