import ctypes
import hashlib
import os
import importlib.util
import json
from pathlib import Path
import sys
import tempfile
import unittest
from unittest.mock import Mock, patch

BASE = Path(__file__).resolve().parents[1]
spec = importlib.util.spec_from_file_location("owned_mouse_focus", BASE / "owned_mouse_focus.py")
mouse = importlib.util.module_from_spec(spec)
spec.loader.exec_module(mouse)
sys.path.insert(0, str(BASE))
recovery_spec = importlib.util.spec_from_file_location("recover_owned_mouse", BASE / "recover_owned_mouse.py")
recovery = importlib.util.module_from_spec(recovery_spec)
recovery_spec.loader.exec_module(recovery)


@unittest.skipUnless(sys.platform == "win32", "Win32 input ABI; all API calls below are fakes")
class OwnedMouseFocusTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(prefix="ss-uno018-ui-")
        self.addCleanup(self.temp.cleanup)
        self.journal = Path(self.temp.name).resolve() / "focus.json"
        self.user = Mock()
        self.kernel = Mock()
        self.owner = 123
        self.cursor = [10, 20]
        self.user.GetWindowThreadProcessId.side_effect = self.pid
        self.user.GetWindowRect.side_effect = self.rect
        self.user.GetClientRect.side_effect = self.client_rect
        self.user.ClientToScreen.side_effect = self.client_to_screen
        self.user.GetSystemMetrics.return_value = 0
        self.user.GetCursorPos.side_effect = self.get_cursor
        self.user.SetCursorPos.side_effect = self.set_cursor
        self.user.GetAsyncKeyState.return_value = 0
        self.user.GetLastInputInfo.side_effect = self.last_input
        self.kernel.GetTickCount64.return_value = 10000
        self.user.SetThreadDpiAwarenessContext.return_value = 17
        self.user.GetWindowLongPtrW.return_value = 0
        self.user.SetWindowPos.return_value = 1
        self.user.OpenInputDesktop.return_value = 5
        self.user.GetThreadDesktop.return_value = 6
        self.user.GetUserObjectInformationW.side_effect = self.desktop
        self.user.CloseDesktop.return_value = 1
        self.user.WindowFromPoint.return_value = 88
        self.user.GetAncestor.return_value = 77
        self.user.SendInput.return_value = 2
        self.user.GetForegroundWindow.return_value = 77

    def pid(self, hwnd, output):
        output._obj.value = self.owner
        return 1

    def rect(self, hwnd, output):
        output._obj.left, output._obj.top = -2000, 1500
        output._obj.right, output._obj.bottom = -1000, 2300
        return 1

    def client_rect(self, hwnd, output):
        output._obj.left, output._obj.top = 0, 0
        output._obj.right, output._obj.bottom = 1000, 800
        return 1

    def client_to_screen(self, hwnd, output):
        output._obj.x += -2000
        output._obj.y += 1500
        return 1

    def get_cursor(self, output):
        output._obj.x, output._obj.y = self.cursor
        return 1

    def set_cursor(self, x, y):
        self.cursor[:] = [x, y]
        return 1

    def last_input(self, output):
        output._obj.tick = 0
        return 1

    def desktop(self, handle, kind, name, size, needed):
        name.value = "Default"
        return 1

    def relative(self, u, v):
        with patch.object(mouse.ctypes, "WinDLL", side_effect=[self.user, self.kernel]):
            mouse.click_owned_window(123, 77, u, v, self.journal, relative=True)

    def test_relative_negative_monitor_mapping_and_integer_journal(self):
        def actual_rect(hwnd, output):
            output._obj.left, output._obj.top = -1904, 2002
            output._obj.right, output._obj.bottom = -1264, 2522
            return 1
        self.user.GetWindowRect.side_effect = actual_rect
        self.relative(0.5, 118 / 520)
        self.assertEqual([-1584, 2120], json.loads(self.journal.read_text())["point"])
        self.user.SetCursorPos.assert_any_call(-1584, 2120)
        self.assertEqual([10, 20], self.cursor)

    def test_invalid_relative_fractions_refuse_before_mutation(self):
        for u, v in [(0, .5), (-.1, .5), (1, .5), (.5, 1), (.5, float("nan"))]:
            with self.subTest(u=u, v=v), self.assertRaisesRegex(ValueError, "Invalid owned-field fraction"):
                self.relative(u, v)
        self.assertFalse(self.journal.exists())
        self.user.SetWindowPos.assert_not_called()
        self.user.SetCursorPos.assert_not_called()
        self.user.SendInput.assert_not_called()

    def test_relative_border_refuses_before_mutation(self):
        with self.assertRaisesRegex(ValueError, "non-client or border"):
            self.relative(.5, .001)
        self.assertFalse(self.journal.exists())
        self.user.SetWindowPos.assert_not_called()
        self.user.SetCursorPos.assert_not_called()
        self.user.SendInput.assert_not_called()

    def invoke(self, *, require_foreground=True):
        with patch.object(mouse.ctypes, "WinDLL", side_effect=[self.user, self.kernel]):
            mouse.click_owned_window(123, 77, -1500, 2000, self.journal, require_foreground=require_foreground)

    def test_native_row_waits_for_hidden_before_cursor_or_zorder_restore(self):
        order = []
        def visible(hwnd):
            self.assertEqual([-1500, 2000], self.cursor)
            self.assertEqual(1, self.user.SetWindowPos.call_count)
            self.assertEqual(1, self.user.SendInput.call_count)
            order.append("visible")
            return len(order) < 3
        self.user.IsWindowVisible.side_effect = visible
        with patch.object(mouse.time, "sleep") as sleep:
            self.invoke(require_foreground=False)
        self.assertEqual(3, len(order))
        self.assertEqual(2, sleep.call_count)
        self.assertEqual([10, 20], self.cursor)
        self.assertEqual(2, self.user.SetWindowPos.call_count)
        self.user.GetForegroundWindow.assert_not_called()

    def test_native_row_close_while_waiting_is_success(self):
        def visible(hwnd):
            self.user.GetWindowThreadProcessId.return_value = 0
            self.user.GetWindowThreadProcessId.side_effect = None
            return 1
        self.user.IsWindowVisible.side_effect = visible
        with patch.object(mouse.time, "sleep"):
            self.invoke(require_foreground=False)
        self.assertEqual([10, 20], self.cursor)
        self.assertEqual(1, self.user.SetWindowPos.call_count)

    def test_native_row_visible_timeout_cannot_succeed(self):
        self.user.IsWindowVisible.return_value = 1
        with patch.object(mouse.time, "monotonic", side_effect=[0, 0, 2]),              self.assertRaisesRegex(ValueError, "did not hide"):
            self.invoke(require_foreground=False)
        self.assertTrue(self.journal.exists())

    def test_demotion_failure_rechecks_owner_and_accepts_destroy_or_reuse_only(self):
        for owner in (0, 999, 123):
            with self.subTest(owner=owner):
                self.setUp()
                def demote(hwnd, after, *args):
                    if after.value == ctypes.c_void_p(-2).value:
                        self.owner = owner
                        return 0
                    return 1
                self.user.SetWindowPos.side_effect = demote
                if owner == 123:
                    with self.assertRaisesRegex(ValueError, "window restoration incomplete"):
                        self.invoke()
                else:
                    self.invoke()
                self.assertEqual([10, 20], self.cursor)

    def recover_fixture(self):
        self.journal.write_text(json.dumps({"pid": 123, "window": 77, "cursor": [10, 20],
                                           "point": [-1500, 2000], "wasTopmost": False}))
        self.cursor[:] = [-1500, 2000]

    def recover_invoke(self):
        with patch.object(recovery, "validate_image", return_value=Path("fixture.exe")),              patch.object(recovery.ctypes, "WinDLL", side_effect=[self.user, self.kernel]):
            recovery.recover(self.journal, 123, Path("fixture.exe"), "unused")

    def test_recovery_all_held_virtual_keys_refuse_cursor_and_retain_journal(self):
        for key in range(1, 255):
            with self.subTest(key=key):
                self.recover_fixture()
                self.user.GetAsyncKeyState.side_effect = lambda vk: 0x8000 if vk == key else 0
                with patch.object(recovery, "query_windows_process", return_value=(os.getppid(), Path("fixture.exe"))),                      self.assertRaisesRegex(ValueError, "Held input"):
                    self.recover_invoke()
                self.assertEqual([-1500, 2000], self.cursor)
                self.assertTrue(self.journal.exists())
        self.user.SetCursorPos.assert_not_called()
        self.user.SendInput.assert_not_called()

    def test_recovery_process_query_failure_allows_cursor_only_after_hwnd_gone(self):
        for state in (0, 999, 123):
            with self.subTest(state=state):
                self.setUp()
                self.recover_fixture()
                def exited(pid):
                    self.owner = state
                    if state == 0:
                        self.user.GetWindowThreadProcessId.side_effect = None
                        self.user.GetWindowThreadProcessId.return_value = 0
                    raise ValueError("Process ownership unavailable")
                with patch.object(recovery, "query_windows_process", side_effect=exited):
                    if state:
                        with self.assertRaisesRegex(ValueError, "Process ownership unavailable"):
                            self.recover_invoke()
                        self.user.SetCursorPos.assert_not_called()
                    else:
                        self.recover_invoke()
                        self.assertEqual([10, 20], self.cursor)
                    self.user.SetWindowPos.assert_not_called()

    def test_recovery_demotion_failure_rechecks_fresh_owner(self):
        for state in (0, 999, 123):
            with self.subTest(state=state):
                self.setUp()
                self.recover_fixture()
                def demote(*args):
                    self.owner = state
                    return 0
                self.user.SetWindowPos.side_effect = demote
                with patch.object(recovery, "query_windows_process", return_value=(os.getppid(), Path("fixture.exe"))):
                    if state == 123:
                        with self.assertRaisesRegex(ValueError, "topmost restoration incomplete"):
                            self.recover_invoke()
                    else:
                        self.recover_invoke()
                self.assertEqual([10, 20], self.cursor)

    def test_wrong_pid_never_moves_cursor_raises_window_or_sends_input(self):
        self.owner = 999
        with self.assertRaisesRegex(ValueError, "Owned window unavailable"):
            self.invoke()
        self.user.SendInput.assert_not_called()
        self.user.SetWindowPos.assert_not_called()
        self.user.SetCursorPos.assert_not_called()
        self.assertFalse(self.journal.exists())

    def test_operator_input_activity_refuses_before_mutation(self):
        self.kernel.GetTickCount64.return_value = 100
        with patch.object(mouse.time, "monotonic", side_effect=[0, 6]):
            with self.assertRaisesRegex(ValueError, "Operator input active"):
                self.invoke()
        self.user.SendInput.assert_not_called()
        self.user.SetWindowPos.assert_not_called()
        self.user.SetCursorPos.assert_not_called()

    def test_locked_or_unavailable_input_desktop_never_clicks(self):
        self.user.OpenInputDesktop.return_value = 0
        with self.assertRaisesRegex(ValueError, "Interactive input desktop unavailable"):
            self.invoke()
        self.user.SendInput.assert_not_called()
        self.user.SetWindowPos.assert_not_called()
        self.user.SetCursorPos.assert_not_called()
        self.assertEqual(2, self.user.SetThreadDpiAwarenessContext.call_count)

    def test_same_pid_other_window_hit_test_never_clicks_and_restores_state(self):
        self.user.GetAncestor.return_value = 99
        with self.assertRaisesRegex(ValueError, "Owned hit-test changed"):
            self.invoke()
        self.user.SendInput.assert_not_called()
        self.assertEqual([10, 20], self.cursor)
        self.assertEqual(2, self.user.SetWindowPos.call_count)
        self.assertEqual(2, self.user.SetThreadDpiAwarenessContext.call_count)

    def test_clamped_live_cursor_never_clicks(self):
        self.user.SetCursorPos.side_effect = lambda x, y: 1
        with self.assertRaisesRegex(ValueError, "Live cursor moved or was clamped"):
            self.invoke()
        self.user.SendInput.assert_not_called()
        self.assertEqual(2, self.user.SetWindowPos.call_count)

    def test_swapped_buttons_refused_before_mutation(self):
        self.user.GetSystemMetrics.return_value = 1
        with self.assertRaisesRegex(ValueError, "swapped mouse-button"):
            self.invoke()
        self.user.SendInput.assert_not_called()
        self.user.SetWindowPos.assert_not_called()
        self.user.SetCursorPos.assert_not_called()

    def test_partial_mouse_down_is_compensated_with_one_mouse_up(self):
        self.user.SendInput.side_effect = [1, 1]
        with self.assertRaisesRegex(ValueError, "Owned click incomplete"):
            self.invoke()
        self.assertEqual(2, self.user.SendInput.call_count)
        second = self.user.SendInput.call_args_list[1].args
        self.assertEqual((1, 40), (second[0], second[2]))
        self.assertEqual(4, second[1]._obj.data.mouse.flags)
        self.assertEqual([10, 20], self.cursor)
        self.assertEqual(2, self.user.SetWindowPos.call_count)

    def test_replay_after_worker_loss_restores_only_journaled_owned_state(self):
        import prepare_owned_focus
        root = Path(self.temp.name).resolve()
        image = root / "bin" / "probe" / "SmartSnippets.UnoProbe.exe"
        image.parent.mkdir(parents=True)
        image.write_bytes(b"synthetic image validation fixture")
        digest = hashlib.sha256(image.read_bytes()).hexdigest()
        self.journal.write_text(json.dumps({"pid": 123, "window": 77, "cursor": [10, 20],
                                            "point": [-1500, 2000], "wasTopmost": False}))
        self.cursor[:] = [-1500, 2000]
        with patch.object(prepare_owned_focus, "BASE", root), \
             patch.object(recovery, "query_windows_process", return_value=(os.getppid(), image)), \
             patch.object(recovery.ctypes, "WinDLL", side_effect=[self.user, self.kernel]):
            recovery.recover(self.journal, 123, image, digest)
        self.assertEqual([10, 20], self.cursor)
        self.user.SendInput.assert_not_called()
        self.assertEqual(1, self.user.SetWindowPos.call_count)
        self.assertEqual(ctypes.c_void_p(-2).value, self.user.SetWindowPos.call_args.args[1].value)
        self.assertEqual(2, self.user.SetThreadDpiAwarenessContext.call_count)

    def test_recovery_rejects_wrong_pid_and_non_numeric_journal(self):
        self.journal.write_text(json.dumps({"pid": 999, "window": 77, "cursor": [10, 20],
                                            "point": [-1500, 2000], "wasTopmost": False}))
        with self.assertRaisesRegex(ValueError, "Recovery identity mismatch"):
            recovery.read_journal(self.journal, 123)
        self.journal.write_text(json.dumps({"pid": 123, "window": 77, "cursor": ["untrusted", 20],
                                            "point": [-1500, 2000], "wasTopmost": False}))
        with self.assertRaisesRegex(ValueError, "Invalid recovery coordinate"):
            recovery.read_journal(self.journal, 123)

    def test_only_mouse_down_up_then_cursor_topmost_and_dpi_restoration(self):
        self.invoke()
        count, events, size = self.user.SendInput.call_args.args
        self.assertEqual((2, 40), (count, size))
        self.assertEqual([0, 0], [event.kind for event in events])
        self.assertEqual([2, 4], [event.data.mouse.flags for event in events])
        self.assertEqual([10, 20], self.cursor)
        self.assertEqual(2, self.user.SetWindowPos.call_count)
        self.assertEqual(ctypes.c_void_p(-2).value, self.user.SetWindowPos.call_args.args[1].value)
        self.assertEqual(2, self.user.SetThreadDpiAwarenessContext.call_count)
        saved = json.loads(self.journal.read_text())
        self.assertEqual([10, 20], saved["cursor"])
        self.assertEqual((123, 77), (saved["pid"], saved["window"]))


if __name__ == "__main__":
    unittest.main()
