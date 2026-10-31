import base64
import hashlib
import importlib.util
import os
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest
from unittest.mock import patch

BASE = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(BASE))
spec = importlib.util.spec_from_file_location("prepare_owned_focus", BASE / "prepare_owned_focus.py")
prepare = importlib.util.module_from_spec(spec)
spec.loader.exec_module(prepare)


@unittest.skipUnless(sys.platform in ("win32", "darwin"), "Native desktop hosts")
class OwnedFocusPreparationTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.base = Path(self.temp.name).resolve()
        output = self.base / "bin" / "probe"
        output.mkdir(parents=True)
        self.entry = output / ("SmartSnippets.UnoProbe.exe" if sys.platform == "win32" else "SmartSnippets.UnoProbe")
        self.entry.write_bytes(b"not executable; validation unit fixture")
        self.digest = hashlib.sha256(self.entry.read_bytes()).hexdigest()

    def test_owned_image_hash_and_path_both_required(self):
        with patch.object(prepare, "BASE", self.base):
            self.assertEqual(self.entry, prepare.validate_image(self.entry, self.digest))
            with self.assertRaisesRegex(ValueError, "unverified image"):
                prepare.validate_image(self.entry, "0" * 64)
            outside = self.base / self.entry.name
            outside.write_bytes(self.entry.read_bytes())
            with self.assertRaisesRegex(ValueError, "unowned image"):
                prepare.validate_image(outside, self.digest)

    def test_wrong_parent_refused_before_mac_native_frameworks_load(self):
        with patch.object(subprocess, "run", return_value=subprocess.CompletedProcess([], 0, "999\n", "")), \
             patch.object(prepare.ctypes, "CDLL") as native:
            with self.assertRaisesRegex(ValueError, "not spawned by caller"):
                prepare.prepare_macos(123, 456, self.entry)
            native.assert_not_called()

    @unittest.skipUnless(sys.platform == "win32", "Windows UIA script protocol only")
    def test_windows_script_checks_parent_and_image_before_any_focus(self):
        with patch.object(prepare, "query_windows_process", return_value=(456, self.entry)) as query, \
             patch.object(subprocess, "run", return_value=subprocess.CompletedProcess([], 0, "owned_focus_fraction hwnd=987 u=0.5 v=0.25\n", "")) as execute, \
             patch("owned_mouse_focus.click_owned_window") as click:
            prepare.prepare_windows(123, 456, self.entry, self.digest, self.base / "focus.json")
        click.assert_called_once_with(123, 987, 0.5, 0.25, self.base / "focus.json", relative=True, require_foreground=True)
        query.assert_called_once_with(123)
        args = execute.call_args.args[0]
        script = base64.b64decode(args[-1]).decode("utf-16le")
        focus = script.index('[Console]::Out.WriteLine("owned_focus_fraction')
        for predicate in ("MainModule.FileName -ne", "$actualHash -ne", "$root.Current.ProcessId -ne 123", "$field.Current.ProcessId -ne 123"):
            self.assertLess(script.index(predicate), focus)
        self.assertLess(script.index("$bounds.Left -lt $rootBounds.Left"), focus)
        self.assertLess(script.index("$bounds.Bottom -gt $rootBounds.Bottom"), focus)
        self.assertNotIn("SetFocus()", script)
        self.assertNotIn("Get-CimInstance", script)
        self.assertNotIn("Add-Type", script)
        self.assertNotIn("SendInput", script)
        self.assertNotIn("Clipboard", script)
        self.assertEqual(subprocess.CREATE_NO_WINDOW, execute.call_args.kwargs["creationflags"])

    @unittest.skipUnless(sys.platform == "win32", "Windows row UIA protocol only; all workers faked")
    def test_native_row_pins_popup_and_uses_hide_wait_mouse_path(self):
        with patch.object(prepare, "query_windows_process", return_value=(456, self.entry)), \
             patch.object(subprocess, "run", return_value=subprocess.CompletedProcess([], 0, "owned_focus_fraction hwnd=987 u=0.5 v=0.25\n", "")) as execute, \
             patch("owned_mouse_focus.click_owned_window") as click:
            prepare.prepare_windows(123, 456, self.entry, self.digest, self.base / "focus.json", row_hwnd=987)
        click.assert_called_once_with(123, 987, 0.5, 0.25, self.base / "focus.json", relative=True, require_foreground=False)
        script = base64.b64decode(execute.call_args.args[0][-1]).decode("utf-16le")
        self.assertIn("$hwnd = [IntPtr]::new(987)", script)
        self.assertIn("'OwnedPreviewRow'", script)
        self.assertIn("AutomationIdProperty", script)
        self.assertNotIn("$process.MainWindowHandle", script)
        self.assertNotIn("InvokePattern", script)
        self.assertNotIn("SetFocus", script)
        self.assertEqual(7, execute.call_args.kwargs["timeout"])

    @unittest.skipUnless(sys.platform == "win32", "Windows row UIA protocol only; all workers faked")
    def test_native_row_changed_popup_or_timed_out_uia_never_clicks(self):
        for result in (subprocess.CompletedProcess([], 0, "owned_focus_fraction hwnd=988 u=0.5 v=0.25\n", ""),
                       subprocess.TimeoutExpired("fake UIA", 7)):
            with self.subTest(result=result), \
                 patch.object(prepare, "query_windows_process", return_value=(456, self.entry)), \
                 patch.object(subprocess, "run", side_effect=[result]), \
                 patch("owned_mouse_focus.click_owned_window") as click:
                with self.assertRaises((ValueError, subprocess.TimeoutExpired)):
                    prepare.prepare_windows(123, 456, self.entry, self.digest, self.base / "focus.json", row_hwnd=987)
                click.assert_not_called()

    @unittest.skipUnless(sys.platform == "win32", "Windows UIA script protocol only")
    def test_bad_fraction_protocol_never_calls_mouse(self):
        for u, v in [("-0.7375", "0.5"), ("0.5", "2.265"), ("1.0", "0.5"), ("0.5", "1.0")]:
            with self.subTest(u=u, v=v), \
                 patch.object(prepare, "query_windows_process", return_value=(456, self.entry)), \
                 patch.object(subprocess, "run", return_value=subprocess.CompletedProcess([], 0, f"owned_focus_fraction hwnd=987 u={u} v={v}\n", "")), \
                 patch("owned_mouse_focus.click_owned_window") as click:
                with self.assertRaisesRegex(ValueError, "preparation refused"):
                    prepare.prepare_windows(123, 456, self.entry, self.digest, self.base / "focus.json")
                click.assert_not_called()

    @unittest.skipUnless(sys.platform == "win32", "Windows UIA script protocol only")
    def test_windows_refusal_is_not_treated_as_prepared(self):
        with patch.object(prepare, "query_windows_process", return_value=(456, self.entry)), \
             patch.object(subprocess, "run", return_value=subprocess.CompletedProcess([], 1, "private diagnostic", "")):
            with self.assertRaisesRegex(ValueError, "preparation refused"):
                prepare.prepare_windows(123, 456, self.entry, self.digest, self.base / "focus.json")


    @unittest.skipUnless(sys.platform == "win32", "Real read-only Win32 owner query")
    def test_actual_win32_snapshot_layout_and_process_image(self):
        parent, image = prepare.query_windows_process(os.getpid())
        self.assertEqual(os.getppid(), parent)
        self.assertEqual(Path(sys.executable).resolve(), image)

    @unittest.skipUnless(sys.platform == "win32", "Windows owner gating")
    def test_wrong_windows_parent_refused_before_any_uia_worker(self):
        with patch.object(prepare, "query_windows_process", return_value=(999, self.entry)), \
             patch.object(subprocess, "run") as worker:
            with self.assertRaisesRegex(ValueError, "not spawned by caller"):
                prepare.prepare_windows(123, 456, self.entry, self.digest, self.base / "focus.json")
            worker.assert_not_called()


if __name__ == "__main__":
    unittest.main()
