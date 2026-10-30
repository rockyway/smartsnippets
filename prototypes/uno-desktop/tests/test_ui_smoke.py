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

    def invoke(self, *, authorize=True, helper=None, interrupt_wait=False):
        process = Mock(pid=12345)
        process.poll.return_value = None
        process.wait.return_value = 0
        if interrupt_wait:
            process.wait.side_effect = [KeyboardInterrupt(), 0]
        output = io.StringIO()
        args = self.args + (["--authorized-screen-slot"] if authorize else [])
        with patch.object(sys, "argv", args), patch.object(smoke, "BASE", self.base), \
             patch.object(smoke, "inspect_executable", return_value=[]), \
             patch.object(smoke, "private_directory", return_value=self.private), \
             patch.object(subprocess, "Popen", return_value=process) as launch, \
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
        self.assertIsInstance(result, subprocess.TimeoutExpired)
        launch.assert_called_once()
        process.terminate.assert_called_once_with()
        process.wait.assert_called_once_with(timeout=3)
        self.assertNotIn("OBSERVED", output)

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


if __name__ == "__main__":
    unittest.main()
