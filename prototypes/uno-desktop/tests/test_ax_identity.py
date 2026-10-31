"""Pure protocol/negative-oracle tests; never load AX or launch a native target."""
import ctypes
import importlib.util
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest
from unittest.mock import Mock, patch

BASE = Path(__file__).resolve().parents[2] / 'uno-native-target/macos'
sys.path.insert(0, str(BASE))
import ax_identity_observer as observer
import run_identity as runner


class AxIdentityTests(unittest.TestCase):
    def setUp(self):
        foreground = patch.object(observer, 'frontmost_pid', return_value=42)
        self.foreground = foreground.start()
        self.addCleanup(foreground.stop)

    def fixture(self, focused=11, pid=42):
        identity = observer.Identity.__new__(observer.Identity)
        identity.pid, identity.app, identity.system = 42, 1, 2
        identity.a, identity.b = 11, 12
        identity.refs = [11, 12]
        identity.cf = Mock()
        identity.ax = Mock()
        identity.cf.CFEqual.side_effect = lambda a, b: a == b
        identity.cf.CFGetTypeID.return_value = 77
        identity.ax.AXUIElementGetTypeID.return_value = 77
        def get_pid(element, result):
            ctypes.cast(result, ctypes.POINTER(ctypes.c_int))[0] = pid
            return 0
        identity.ax.AXUIElementGetPid.side_effect = get_pid
        identity.copy = Mock(side_effect=lambda element, name: 3 if name == 'AXFocusedApplication' else focused)
        return identity

    def test_original_window_then_same_pid_other_window(self):
        identity = self.fixture()
        identity.check('focus_a')
        identity.copy.side_effect = lambda element, name: 3 if name == 'AXFocusedApplication' else 12
        identity.check('focus_b')
        with self.assertRaisesRegex(ValueError, 'Exact focused window mismatch'):
            identity.check('focus_a')
        self.assertEqual(3, identity.cf.CFRelease.call_count)

    def test_pid_only_false_positive_is_rejected(self):
        identity = self.fixture(focused=11)
        with self.assertRaisesRegex(ValueError, 'Exact focused window mismatch'):
            identity.check('focus_b')

    def test_wildcard_comparator_cannot_accept_either_sibling(self):
        for command in ('focus_a', 'focus_b'):
            identity = self.fixture()
            identity.cf.CFEqual.side_effect = None
            identity.cf.CFEqual.return_value = True
            with self.subTest(command=command), self.assertRaisesRegex(ValueError, 'Same PID other window accepted'):
                identity.check(command)

    def test_other_app_foreground_refuses_even_with_cached_window(self):
        identity = self.fixture()
        self.foreground.return_value = 99
        with self.assertRaisesRegex(ValueError, 'AX owner refused'):
            identity.check('focus_a')
        identity.copy.assert_not_called()
        identity.cf.CFRelease.assert_not_called()

    def test_foreground_changes_during_window_read_are_rejected(self):
        identity = self.fixture()
        self.foreground.side_effect = [42, 99]
        with self.assertRaisesRegex(ValueError, 'AX owner refused'):
            identity.check('focus_a')
        identity.cf.CFRelease.assert_called_once_with(11)

    def test_closed_a_requires_exact_b_survival_not_count(self):
        identity = self.fixture()
        identity.windows = Mock(return_value=[12])
        identity.check('closed_a')
        for wrong in ([11], [13], [11, 12], []):
            identity.windows.return_value = wrong
            with self.subTest(wrong=wrong), self.assertRaisesRegex(ValueError, 'Closed window still eligible'):
                identity.check('closed_a')

    def test_unknown_commands_refuse_without_ax_read(self):
        identity = self.fixture()
        with self.assertRaisesRegex(ValueError, 'Command refused'):
            identity.check('paste')
        identity.copy.assert_not_called()

    def test_all_owned_refs_are_released_once(self):
        identity = self.fixture()
        identity.close()
        self.assertEqual([unittest.mock.call(12), unittest.mock.call(11)], identity.cf.CFRelease.call_args_list)
        identity.close()
        self.assertEqual(2, identity.cf.CFRelease.call_count)

    def test_protocol_mismatch_exit_and_deadline_cannot_pass(self):
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / 'stdout'
            path.write_text('wrong\n')
            child = Mock()
            child.poll.return_value = None
            with self.assertRaisesRegex(ValueError, 'protocol refused'):
                runner.wait_lines(path, ['expected'], [child], runner.time.monotonic() + 1)
            path.write_text('expected\n')
            child.poll.return_value = 0
            with self.assertRaisesRegex(ValueError, 'protocol refused'):
                runner.wait_lines(path, ['expected'], [child], runner.time.monotonic() + 1)
            with self.assertRaisesRegex(ValueError, 'deadline exceeded'):
                runner.wait_lines(path, ['expected'], [child], 0)

    def test_observer_failure_still_stops_target(self):
        with tempfile.TemporaryDirectory() as directory:
            private = Path(directory)
            target, ax = Mock(pid=42), Mock()
            target.poll.return_value = ax.poll.return_value = None
            def spawn(*args, **kwargs):
                child = target if Path(args[0][0]) == Path('/fixture') else ax
                if child is target:
                    kwargs['stdout'].write(b'target_ready pid=42 a=11 b=12\n')
                    kwargs['stdout'].flush()
                return child
            with patch.object(runner.subprocess, 'Popen', side_effect=spawn), patch.object(runner, 'wait_lines', side_effect=ValueError('observer failed')):
                with self.assertRaisesRegex(ValueError, 'observer failed'):
                    runner.run(Path('/fixture'), 'a' * 64, private)
            target.terminate.assert_called_once()
            ax.terminate.assert_called_once()

    def test_full_parent_protocol_requires_both_native_streams(self):
        for corrupted in (False, True):
            with self.subTest(corrupted=corrupted), tempfile.TemporaryDirectory() as directory:
                target, ax = Mock(pid=42), Mock()
                target.poll.return_value = ax.poll.return_value = None
                ax.returncode = 0
                def spawn(*args, **kwargs):
                    is_target = Path(args[0][0]) == Path('/fixture')
                    child = target if is_target else ax
                    stream = kwargs['stdout']
                    stream.write(b'target_ready pid=42 a=11 b=12\n' if is_target else b'ax_identity_ready\n')
                    stream.flush()
                    def command(data):
                        name = data.decode('ascii').strip()
                        if not is_target and corrupted and name == 'focus_b':
                            name = 'focus_a'
                        receipt = ('target_command name=' + name + ' ok=1') if is_target else ('ax_identity_verified name=' + name)
                        stream.write((receipt + '\n').encode('ascii'))
                        stream.flush()
                    child.stdin.write.side_effect = command
                    if not is_target:
                        child.stdin.close.side_effect = lambda: setattr(ax.poll, 'return_value', 0)
                    return child
                with patch.object(runner.subprocess, 'Popen', side_effect=spawn), patch.object(runner, 'require_quiet_focus'):
                    if corrupted:
                        with self.assertRaisesRegex(ValueError, 'protocol refused'):
                            runner.run(Path('/fixture'), 'a' * 64, Path(directory))
                    else:
                        self.assertEqual(0, runner.run(Path('/fixture'), 'a' * 64, Path(directory)))
                        self.assertEqual([unittest.mock.call(b'focus_a\n'), unittest.mock.call(b'focus_b\n'), unittest.mock.call(b'focus_a\n'), unittest.mock.call(b'close_a\n')], target.stdin.write.call_args_list)
                target.terminate.assert_called_once()

    def test_interrupt_escalates_and_broken_stdin_does_not_skip_stop(self):
        child = Mock()
        child.poll.return_value = None
        child.stdin.close.side_effect = BrokenPipeError()
        child.wait.side_effect = [KeyboardInterrupt(), 0]
        with self.assertRaises(KeyboardInterrupt):
            runner.stop(child)
        child.terminate.assert_called_once()
        child.kill.assert_called_once()
        self.assertEqual(2, child.wait.call_count)

    def test_focus_waits_for_idle_but_never_waives_held_input(self):
        cg = Mock()
        cg.CGEventSourceSecondsSinceLastEventType.side_effect = [0.2, 2.0]
        cg.CGEventSourceKeyState.return_value = False
        cg.CGEventSourceButtonState.return_value = False
        with patch.object(runner.C, 'CDLL', return_value=cg), patch.object(runner.time, 'sleep') as pause:
            runner.require_quiet_focus(runner.time.monotonic() + 1)
            pause.assert_called_once_with(0.05)
        cg.CGEventSourceSecondsSinceLastEventType.side_effect = None
        cg.CGEventSourceSecondsSinceLastEventType.return_value = 2.0
        cg.CGEventSourceKeyState.return_value = True
        with patch.object(runner.C, 'CDLL', return_value=cg), patch.object(runner.time, 'monotonic', side_effect=[0, 0, 6]), patch.object(runner.time, 'sleep'):
            with self.assertRaisesRegex(ValueError, 'idle preflight refused'):
                runner.require_quiet_focus(10)

    def test_watchdog_only_kills_owned_handle(self):
        child = Mock()
        child.poll.return_value = None
        child.wait.side_effect = [subprocess.TimeoutExpired('owned', 3), 0]
        runner.stop(child)
        child.terminate.assert_called_once()
        child.kill.assert_called_once()


if __name__ == '__main__':
    unittest.main()
