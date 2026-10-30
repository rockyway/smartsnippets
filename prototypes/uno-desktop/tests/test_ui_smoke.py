import contextlib
import hashlib
import importlib.util
import io
import plistlib
import shutil
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest
from unittest.mock import Mock, patch

BASE = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(BASE))
spec = importlib.util.spec_from_file_location("ui_smoke", BASE / "run_ui_smoke.py")
smoke = importlib.util.module_from_spec(spec)
spec.loader.exec_module(smoke)


@unittest.skipUnless(sys.platform in ("win32", "darwin"), "Native host guards")
class UiSmokeTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.base = Path(self.temp.name).resolve()
        output = self.base / "bin" / "probe"
        if sys.platform == "darwin":
            output = output / "bundle-fixture" / "SmartSnippets.UnoProbe.app" / "Contents" / "MacOS"
        output.mkdir(parents=True)
        if sys.platform == "darwin":
            with (output.parent / "Info.plist").open("wb") as stream:
                plistlib.dump({"CFBundleIdentifier": "org.smartsnippets.feasibility018",
                              "CFBundleExecutable": "SmartSnippets.UnoProbe"}, stream)
        self.entry = output / ("SmartSnippets.UnoProbe.exe" if sys.platform == "win32" else "SmartSnippets.UnoProbe")
        self.entry.write_bytes(b"non-executable unit fixture")
        self.args = ["run_ui_smoke.py", "--executable", str(self.entry), "--sha256",
                     hashlib.sha256(self.entry.read_bytes()).hexdigest()]
        self.private = self.base / "private"
        self.private.mkdir()

    def invoke(self, *, authorize=True, helper=None, interrupt_wait=False, lifecycle=None):
        process = Mock(pid=12345)
        process.poll.return_value = None
        process.wait.return_value = 0
        if interrupt_wait:
            process.wait.side_effect = [KeyboardInterrupt(), 0]
        output = io.StringIO()
        args = self.args + (["--authorized-screen-slot"] if authorize else [])
        if lifecycle:
            args.append("--lifecycle")
            if lifecycle == "success":
                process.poll.side_effect = [None, 0]
            else:
                process.wait.side_effect = [subprocess.TimeoutExpired("owned-app", 30), 0]

        def start(arguments, **kwargs):
            if lifecycle:
                kwargs["stdout"].write(("\n".join(smoke.LIFECYCLE_RECEIPTS) + "\n").encode())
                kwargs["stdout"].flush()
            return process
        with patch.object(sys, "argv", args), patch.object(smoke, "BASE", self.base), \
             patch.object(smoke, "inspect_executable", return_value=[]), \
             patch.object(smoke, "private_directory", return_value=self.private), \
             patch.object(subprocess, "Popen", side_effect=start) as launch, \
             patch.object(subprocess, "run", side_effect=helper or [subprocess.CompletedProcess([], 0, "1\n", "")]), \
             contextlib.redirect_stdout(output):
            try:
                result = smoke.main()
            except BaseException as error:
                return error, process, launch, output.getvalue()
        return result, process, launch, output.getvalue()

    def test_refuses_without_explicit_screen_slot(self):
        result, process, launch, _ = self.invoke(authorize=False)
        self.assertIsInstance(result, ValueError)
        self.assertIn("authorized", str(result))
        launch.assert_not_called()
        process.terminate.assert_not_called()

    def test_owned_process_stopped_after_window_observation_no_input_args(self):
        result, process, launch, output = self.invoke()
        self.assertEqual(0, result)
        self.assertEqual([str(self.entry), "--authorized-probe-ui"], launch.call_args.args[0])
        process.terminate.assert_called_once_with()
        process.wait.assert_called_once_with(timeout=3)
        self.assertIn("window=OBSERVED", output)
        self.assertNotIn("clipboard=MODIFIED", output)

    def test_hung_native_observer_still_stops_only_owned_process(self):
        result, process, launch, output = self.invoke(helper=[subprocess.TimeoutExpired("observer", 3)])
        self.assertEqual(1, result)
        self.assertIn("private_evidence_token=", output)
        launch.assert_called_once()
        process.terminate.assert_called_once_with()
        process.wait.assert_called_once_with(timeout=3)
        self.assertNotIn("window=OBSERVED", output)

    def test_lifecycle_waits_for_natural_exit_without_terminating(self):
        result, process, launch, output = self.invoke(lifecycle="success")
        self.assertEqual(0, result)
        self.assertEqual([str(self.entry), "--authorized-probe-ui", "--probe-ui-lifecycle"], launch.call_args.args[0])
        process.terminate.assert_not_called()
        process.kill.assert_not_called()
        self.assertEqual(2, process.wait.call_count)
        self.assertIn("ui_lifecycle_result=PASS graceful_exit=true", output)

    def test_all_receipts_without_process_exit_is_failure_and_owned_child_stops(self):
        result, process, launch, output = self.invoke(lifecycle="hung")
        self.assertEqual(1, result)
        process.terminate.assert_called_once_with()
        self.assertIn("ui_lifecycle_result=FAIL graceful_exit=false", output)
        self.assertIn("private_evidence_token=", output)

    def test_interrupted_wait_escalates_owned_child_cleanup(self):
        result, process, launch, _ = self.invoke(interrupt_wait=True)
        self.assertIsInstance(result, KeyboardInterrupt)
        process.terminate.assert_called_once_with()
        process.kill.assert_called_once_with()
        self.assertEqual(2, process.wait.call_count)

    def test_private_directory_setup_on_actual_host(self):
        root = smoke.private_directory()
        try:
            self.assertTrue(root.is_dir())
            (root / "write-check").write_text("synthetic")
            if sys.platform == "darwin":
                self.assertEqual(0o700, root.stat().st_mode & 0o777)
        finally:
            shutil.rmtree(root)

    def test_private_directory_removed_after_permission_setup_failure(self):
        doomed = self.base / "doomed"
        doomed.mkdir()
        with patch.object(smoke.tempfile, "mkdtemp", return_value=str(doomed)):
            if sys.platform == "win32":
                with patch.object(subprocess, "run", side_effect=OSError("synthetic")):
                    with self.assertRaises(OSError):
                        smoke.private_directory()
            else:
                with patch.object(Path, "chmod", side_effect=OSError("synthetic")):
                    with self.assertRaises(OSError):
                        smoke.private_directory()
        self.assertFalse(doomed.exists())

    @unittest.skipUnless(sys.platform == "darwin", "macOS bundle identity")
    def test_wrong_bundle_identity_refuses_before_launch(self):
        with (self.entry.parents[1] / "Info.plist").open("wb") as stream:
            plistlib.dump({"CFBundleIdentifier": "other"}, stream)
        result, process, launch, _ = self.invoke()
        self.assertIsInstance(result, ValueError)
        self.assertEqual("Wrong macOS bundle identity", str(result))
        launch.assert_not_called()

    def test_wrong_hash_refuses_before_launch(self):
        self.args[-1] = "0" * 64
        result, process, launch, _ = self.invoke()
        self.assertIsInstance(result, ValueError)
        self.assertEqual("Artifact identity check failed", str(result))
        launch.assert_not_called()


class LifecycleReceiptTests(unittest.TestCase):
    def setUp(self):
        self.lines = [
            "ui_lifecycle_started",
            "ui_main_compiled_bindings_ok",
            "probe_data_seed_ok migration=ok crud=ok import_export=ok failure_paths=ok",
            "probe_data_restart_ok persistence=ok privacy_fixture=ok cleanup=ok",
            "ui_data_serialized_ok",
            "ui_popup_compiled_rows_ok",
            "ui_popup_hidden",
            "ui_popup_reopened",
            "ui_popup_recreated",
            "ui_lifecycle_complete managed_closed=3 native_popup_absence=2 store_thread=UI input=NONE",
        ]

    def test_complete_ordered_native_receipts_and_successful_exit_required(self):
        self.assertTrue(smoke.lifecycle_completed("\n".join(self.lines) + "\n", "", 0))
        self.assertFalse(smoke.lifecycle_completed("\n".join(self.lines), "", None))
        self.assertFalse(smoke.lifecycle_completed("\n".join(self.lines), "", -15))
        self.assertFalse(smoke.lifecycle_completed("\n".join(self.lines), "failure", 0))

    def test_disconnected_callback_missing_any_stage_cannot_pass(self):
        for index in range(len(self.lines)):
            with self.subTest(index=index):
                self.assertFalse(smoke.lifecycle_completed("\n".join(self.lines[:index] + self.lines[index + 1:]), "", 0))
        self.assertFalse(smoke.lifecycle_completed("\n".join(reversed(self.lines)), "", 0))
        self.assertFalse(smoke.lifecycle_completed("\n".join(self.lines + [self.lines[-1]]), "", 0))


if __name__ == "__main__":
    unittest.main()
