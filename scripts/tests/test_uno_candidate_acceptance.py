"""Pure negative controls; no native launches, desktop, input or clipboard access."""
import importlib.util
import io
import json
from pathlib import Path
import tempfile
import unittest
from unittest.mock import Mock, patch

SPEC = importlib.util.spec_from_file_location('acceptance', Path(__file__).resolve().parents[1] / 'run_uno_candidate_acceptance.py')
s = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(s)


class AcceptanceTests(unittest.TestCase):
    def test_source_review_gate_closed(self):
        self.assertFalse(s.FULL_HOST_OBSERVER_REVIEWED)

    def test_closed_gate_denies_before_artifact_or_profile_access(self):
        args = ['observer', '--executable', 'untrusted.exe', '--sha256', 'a' * 64,
                '--authorized-screen-slot', '--work-area', '0', '0', '20', '20']
        with patch.object(s.sys, 'argv', args), patch.object(s, 'validate_fixture') as validate:
            with self.assertRaisesRegex(ValueError, 'authorization'):
                s.main()
            validate.assert_not_called()

    def test_no_default_profile_boundary_in_full_host_observer(self):
        source = Path(s.__file__).read_text()
        self.assertNotIn('profile_absent(', source)
        self.assertNotIn('local_app_data(', source)
        self.assertNotIn('OpenDefault', (s.REPO / 'scripts/uno_candidate_fixture/Program.cs').read_text())

    def test_wrong_artifact_path_denied_before_read(self):
        with self.assertRaisesRegex(ValueError, 'provenance'):
            s.validate_fixture(Path('untrusted.exe'), 'a' * 64)

    def test_real_build_gate_errors_propagate(self):
        with tempfile.TemporaryDirectory() as tmp, patch.object(s, 'REPO', Path(tmp)):
            entry = Path(tmp) / ('scripts/uno_candidate_fixture/bin/publish-acceptance/' + 'a' * 32) / s.ENTRY
            entry.parent.mkdir(parents=True)
            entry.write_bytes(b'x' * 64)
            (entry.parent / 'fixture-properties.json').write_text('{}')
            (entry.parent / 'fixture-build.json').write_text('{"startedUtcMs": 1}')
            (entry.parent / 'fixture-references.json').write_text('{}')
            (entry.parent / 'fixture-build.log').write_text('compiler')
            (entry.parent / 'fixture-source.sha256').write_text('source')
            inspector, gate = Mock(), Mock()
            inspector.inspect_executable.return_value = []
            gate.EXACT_PROPERTIES = {}
            gate.validate.return_value = ['missing-native']
            gate.validate_references.return_value = []
            digest = s.hashlib.sha256(entry.read_bytes()).hexdigest()
            with patch.object(s.startup, 'repo_module', side_effect=[inspector, gate]), \
                 patch.object(s, 'source_fingerprint', return_value='source'):
                with self.assertRaisesRegex(ValueError, 'fixture_build_gate'):
                    s.validate_fixture(entry, digest)
            gate.validate.assert_called_once()

    def test_wrong_digest_rejected_before_build_evidence(self):
        with tempfile.TemporaryDirectory() as tmp, patch.object(s, 'REPO', Path(tmp)):
            entry = Path(tmp) / ('scripts/uno_candidate_fixture/bin/publish-acceptance/' + 'b' * 32) / s.ENTRY
            entry.parent.mkdir(parents=True); entry.write_bytes(b'x' * 64)
            with self.assertRaisesRegex(ValueError, 'fixture_artifact'):
                s.validate_fixture(entry, '0' * 64)

    def test_missing_and_foreign_receipts(self):
        with tempfile.TemporaryDirectory() as tmp:
            private = Path(tmp)
            self.assertFalse(s.receipt(private, 'ready', 23))
            (private / 'ready.receipt').write_text(f'24\n{private.name}\nready\n')
            with self.assertRaisesRegex(ValueError, 'receipt_provenance'):
                s.receipt(private, 'ready', 23)

    def test_visible_windows_without_readiness_time_out(self):
        with tempfile.TemporaryDirectory() as tmp:
            process = Mock(pid=23); process.poll.return_value = None
            desktop = Mock(); desktop.snapshot.return_value = (1, 2)
            desktop.windows.return_value = [(0, 0, 10, 10)]
            with self.assertRaisesRegex(ValueError, 'missing_ready'):
                s.observe_phase(process, Path(tmp), 'ready', desktop, (1, 2), (0, 0, 20, 20),
                                clock=Mock(side_effect=[0, 1, 13]), sleep=lambda _: None)
            self.assertFalse((Path(tmp) / 'ready.continue').exists())

    def test_ready_requires_live_owned_window_stable_and_contained(self):
        with tempfile.TemporaryDirectory() as tmp:
            private = Path(tmp)
            (private / 'ready.receipt').write_text(f'23\n{private.name}\nready\n')
            process = Mock(pid=23); process.poll.return_value = None
            desktop = Mock(); desktop.snapshot.return_value = (1, 2)
            desktop.windows.return_value = [(0, 0, 10, 10)]
            s.observe_phase(process, private, 'ready', desktop, (1, 2), (0, 0, 20, 20),
                            clock=Mock(side_effect=[0, 1, 1, 1, 2, 2]), sleep=lambda _: None)
            self.assertTrue((private / 'ready.continue').is_file())
            desktop.windows.assert_called_with(23)

    def test_foreground_or_clipboard_epoch_change_fails(self):
        for snapshot in ((2, 2), (1, 3)):
            desktop = Mock(); desktop.snapshot.return_value = snapshot
            with self.assertRaisesRegex(ValueError, 'desktop_changed'):
                s.observe_phase(Mock(), Path('unused'), 'ready', desktop, (1, 2), (0, 0, 20, 20))
            desktop.windows.assert_not_called()

    def test_missing_cleanup_receipt_fails_even_zero_exit(self):
        with tempfile.TemporaryDirectory() as tmp:
            process = Mock(pid=23); process.wait.return_value = 0
            with self.assertRaisesRegex(ValueError, 'missing_graceful_cleanup'):
                s.require_graceful(process, Path(tmp))

    def test_cleanup_errors_and_missing_host_ready_fail(self):
        for text in ('', ' INFO candidate_started\n ERROR candidate_cleanup_failed\n'):
            with tempfile.TemporaryDirectory() as tmp:
                private = Path(tmp)
                (private / 'graceful.receipt').write_text(f'23\n{private.name}\ngraceful\n')
                log = private / 'fixture-profile/logs/app.log'; log.parent.mkdir(parents=True); log.write_text(text)
                process = Mock(pid=23); process.wait.return_value = 0
                with self.assertRaisesRegex(ValueError, 'host_readiness_cleanup'):
                    s.require_graceful(process, private)

    def test_planted_private_payload_leak_fails(self):
        with tempfile.TemporaryDirectory() as tmp:
            private = Path(tmp)
            (private / 'graceful.receipt').write_text(f'23\n{private.name}\ngraceful\n')
            log = private / 'fixture-profile/logs/app.log'; log.parent.mkdir(parents=True)
            log.write_text(' INFO candidate_started\nacceptance synthetic')
            (private / 'stdout.log').write_bytes(b''); (private / 'stderr.log').write_bytes(b'')
            process = Mock(pid=23); process.wait.return_value = 0
            with self.assertRaisesRegex(ValueError, 'privacy'):
                s.require_graceful(process, private)

    def test_bootstrap_missing_permit_or_job_never_launches(self):
        with patch.object(s.subprocess, 'Popen') as launch:
            with self.assertRaisesRegex(ValueError, 'permit'):
                s.worker(None, None, None, None, io.BytesIO(b''))
            with patch.object(s.startup, 'require_job_membership', side_effect=ValueError('job')):
                with self.assertRaisesRegex(ValueError, 'job'):
                    s.worker(None, None, None, None, io.BytesIO(s.startup.PERMIT))
            launch.assert_not_called()

    def test_job_assignment_precedes_permit_and_cleanup_is_required(self):
        events = []
        process = Mock(returncode=0)
        process.communicate.side_effect = lambda *a, **k: (events.append('permit') or (b'full_host_observed\n', b''))
        def job_factory(p):
            events.append('job'); return Mock()
        with patch.object(s.subprocess, 'CREATE_NO_WINDOW', 0, create=True), \
             patch.object(s.startup, 'finish_job', side_effect=ValueError('job_active')):
            with self.assertRaisesRegex(ValueError, 'job_active'):
                s.supervise(['observer'], job_factory, popen=Mock(return_value=process))
        self.assertEqual(['job', 'permit'], events)

    def test_job_assignment_failure_never_grants_permit(self):
        process = Mock()
        with patch.object(s.subprocess, 'CREATE_NO_WINDOW', 0, create=True), patch.object(s.startup, 'stop_app') as stop:
            with self.assertRaisesRegex(ValueError, 'assignment'):
                s.supervise(['observer'], Mock(side_effect=ValueError('assignment')), popen=Mock(return_value=process))
            process.communicate.assert_not_called(); stop.assert_called_once_with(process)

    def test_live_descendant_before_emergency_kill_cannot_be_called_graceful(self):
        process = Mock(returncode=0); process.communicate.return_value = (b'full_host_observed\n', b'')
        with patch.object(s.subprocess, 'CREATE_NO_WINDOW', 0, create=True), \
             patch.object(s, 'require_empty_job', side_effect=ValueError('graceful_job_not_empty')), \
             patch.object(s.startup, 'finish_job') as emergency_cleanup:
            with self.assertRaisesRegex(ValueError, 'graceful_job_not_empty'):
                s.supervise(['observer'], Mock(), popen=Mock(return_value=process))
            emergency_cleanup.assert_called_once()

    def test_job_query_missing_or_nonzero_active_fails(self):
        for active, success in ((1, True), (0, False)):
            job = Mock()
            def query(handle, kind, target, size, returned):
                target._obj.active = active
                return success
            job.kernel.QueryInformationJobObject = Mock(side_effect=query)
            with self.assertRaisesRegex(ValueError, 'graceful_job_not_empty'):
                s.require_empty_job(job)

    def test_owned_diagnostic_hardlink_refused_before_content_read(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp); original = root / 'original'; original.write_bytes(b'fixed')
            alias = root / 'alias'; s.os.link(original, alias)
            with self.assertRaisesRegex(ValueError, 'evidence_file'):
                s.owned_bytes(alias, root)

    def test_source_provenance_changes_with_linked_driver(self):
        with tempfile.TemporaryDirectory() as tmp, patch.object(s, 'REPO', Path(tmp)):
            root = Path(tmp)
            driver = root / 'tests/SmartSnippets.Uno.Tests/Acceptance/CandidateAcceptanceFlow.cs'
            driver.parent.mkdir(parents=True); driver.write_text('first')
            for name in ('Directory.Build.props', 'Directory.Packages.props'):
                (root / name).write_text('fixed')
            before = s.source_fingerprint(); driver.write_text('changed')
            self.assertNotEqual(before, s.source_fingerprint())

    def test_bad_worker_protocol_cannot_report_success(self):
        for output in ((b'startup_observed\n', b''), (b'full_host_observed\n', b'warning')):
            process = Mock(returncode=0); process.communicate.return_value = output
            with patch.object(s.subprocess, 'CREATE_NO_WINDOW', 0, create=True), patch.object(s.startup, 'finish_job') as finish:
                with self.assertRaisesRegex(ValueError, 'worker_protocol'):
                    s.supervise(['observer'], Mock(), popen=Mock(return_value=process))
                finish.assert_called_once()


if __name__ == '__main__':
    unittest.main()
