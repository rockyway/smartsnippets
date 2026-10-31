"""Pure multi-process protocol fixtures; never launch native code or use clipboard."""
import contextlib
import io
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest
from unittest.mock import Mock, patch

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
import run_private_watchdog_probe as runner


class PrivateWatchdogTests(unittest.TestCase):
    def exercise(self, case, corrupt=None):
        guardian, worker = Mock(), Mock()
        for child in (guardian, worker):
            child.poll.return_value = None
            child.returncode = None
        state = {'captured': False, 'restored': False}
        def start(args, **kwargs):
            owner = args[1] == '--probe-private-watchdog-owner'
            child = guardian if owner else worker
            stream = kwargs['stdout']
            def emit(line):
                stream.write((line + '\n').encode('ascii'))
                stream.flush()
            def exited(code):
                child.poll.return_value = code
                child.returncode = code
                child.wait.return_value = code
            emit(runner.GUARD_READY if owner else runner.WORK_READY)
            def write(data):
                command = data.decode('ascii').strip()
                if not owner:
                    emit('private_watchdog_worker_swapped' if command == 'swap' else 'private_watchdog_worker_new_owner')
                elif command == 'capture_swapped':
                    state['captured'] = True
                    emit(runner.SWAPPED)
                elif command in ('restore', 'refuse_changed'):
                    state['restored'] = True
                    if corrupt == 'restore':
                        emit('private_watchdog_restored representations=PARTIAL')
                    else:
                        emit('private_watchdog_restored representations=EXACT' if command == 'restore' else
                             'private_watchdog_changed_owner_preserved mutation=NONE')
                elif command == 'finish':
                    emit(runner.GUARD_COMPLETE)
                    exited(0)
                else:
                    raise AssertionError('Unexpected fixture command')
            def close():
                if child.poll.return_value is not None:
                    return
                if owner:
                    self.assertTrue(state['captured'])
                    emit('private_watchdog_eof_restored representations=EXACT')
                    emit(runner.GUARD_COMPLETE)
                else:
                    emit(runner.WORK_COMPLETE)
                exited(0)
            child.stdin.write.side_effect = write
            child.stdin.close.side_effect = close
            child.kill.side_effect = lambda: exited(0 if corrupt == 'abort' else -9)
            return child
        with tempfile.TemporaryDirectory() as directory, patch.object(runner.subprocess, 'Popen', side_effect=start), contextlib.redirect_stdout(io.StringIO()) as output:
            runner.run_case(Path('/non-executable-fixture'), case, Path(directory))
        return guardian, worker, state, output.getvalue()

    def test_all_four_complete_ordered_protocols(self):
        for case in runner.CASES:
            with self.subTest(case=case):
                guardian, worker, state, output = self.exercise(case)
                self.assertTrue(state['captured'])
                self.assertIn('case=' + case + ' result=PASS', output)
                self.assertIn('scope=PRIVATE_ONLY', output)
                self.assertEqual(0 if case == 'normal' else 1, worker.kill.call_count)
                guardian.kill.assert_not_called()
                self.assertEqual(case != 'supervisor_eof', state['restored'])

    def test_partial_restoration_or_fake_abort_never_passes(self):
        for corrupt in ('restore', 'abort'):
            with self.subTest(corrupt=corrupt), self.assertRaises(ValueError):
                self.exercise('worker_killed', corrupt)

    def test_unknown_case_refuses_before_launch(self):
        with patch.object(runner.subprocess, 'Popen') as start:
            with self.assertRaisesRegex(ValueError, 'Unknown private watchdog case'):
                runner.run_case(Path('/fixture'), 'unknown', Path('/unused'))
            start.assert_not_called()

    def test_interrupt_and_broken_pipe_cannot_skip_owned_kill(self):
        child = Mock()
        child.stdin.close.side_effect = BrokenPipeError()
        child.poll.return_value = None
        child.wait.side_effect = [KeyboardInterrupt(), 0]
        with self.assertRaises(KeyboardInterrupt):
            runner.cleanup(child)
        child.kill.assert_called_once()
        self.assertEqual(2, child.wait.call_count)

    def test_timeout_kills_only_owned_child(self):
        child = Mock()
        child.wait.side_effect = [subprocess.TimeoutExpired('owned', 3), -9]
        runner.cleanup(child)
        child.kill.assert_called_once()

    def test_early_exit_or_incomplete_native_output_cannot_pass(self):
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / 'stdout'
            path.write_text(runner.GUARD_READY + '\n')
            child = Mock()
            child.poll.return_value = 0
            with self.assertRaisesRegex(ValueError, 'protocol failed'):
                runner.wait_lines(path, [runner.GUARD_READY], [child], runner.time.monotonic() + 1)
            child.poll.return_value = None
            with self.assertRaisesRegex(ValueError, 'deadline exceeded'):
                runner.wait_lines(path, [runner.GUARD_READY, runner.SWAPPED], [child], 0)

    def test_authorization_and_platform_checked_before_read_or_launch(self):
        args = ['runner', '--executable', '/never-read', '--sha256', '0' * 64]
        for platform, authorized in [('darwin', False), ('win32', True)]:
            with self.subTest(platform=platform), patch.object(sys, 'argv', args + (['--authorized-private-watchdog'] if authorized else [])), patch.object(sys, 'platform', platform), patch.object(runner, 'validate_entry') as validate:
                with self.assertRaisesRegex(ValueError, 'Authorized Mac private watchdog required'):
                    runner.main()
                validate.assert_not_called()


if __name__ == '__main__':
    unittest.main()
