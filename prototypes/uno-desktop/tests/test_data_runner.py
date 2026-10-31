import contextlib
import hashlib
import importlib.util
import io
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest
from unittest.mock import patch

BASE = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(BASE))
spec = importlib.util.spec_from_file_location("run_data_probe", BASE / "run_data_probe.py")
runner = importlib.util.module_from_spec(spec)
spec.loader.exec_module(runner)


@unittest.skipUnless(sys.platform in ("win32", "darwin"), "Supported native test hosts only")
class DataRunnerTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.base = Path(self.temp.name).resolve()
        self.output = self.base / "bin" / "probe"
        self.output.mkdir(parents=True)
        self.entry = self.output / ("SmartSnippets.UnoProbe.exe" if sys.platform == "win32" else "SmartSnippets.UnoProbe")
        self.entry.write_bytes(b"synthetic parser control; no executable code")
        self.digest = hashlib.sha256(self.entry.read_bytes()).hexdigest()

    def invoke(self, results, digest=None, entry=None):
        args = ["run_data_probe.py", "--executable", str(entry or self.entry), "--sha256", digest or self.digest]
        output = io.StringIO()
        with patch.object(runner, "BASE", self.base), patch.object(sys, "argv", args), \
             patch.object(runner, "inspect_executable", return_value=[]), \
             patch.object(subprocess, "run", side_effect=results) as execute, contextlib.redirect_stdout(output):
            result = runner.main()
        return result, execute, output.getvalue()

    def test_two_processes_use_only_data_mode_and_same_owned_run(self):
        results = [subprocess.CompletedProcess([], 0, "probe_data_seed_ok migration=ok crud=ok import_export=ok failure_paths=ok\n", ""),
                   subprocess.CompletedProcess([], 0, "probe_data_restart_ok persistence=ok privacy_fixture=ok cleanup=ok\n", "")]
        result, execute, output = self.invoke(results)
        self.assertEqual(0, result)
        self.assertEqual(2, execute.call_count)
        first, second = [call.args[0] for call in execute.call_args_list]
        self.assertEqual(["--probe-data", "seed"], first[1:3])
        self.assertEqual(["--probe-data", "verify"], second[1:3])
        self.assertEqual(first[3], second[3])
        self.assertNotIn("--authorized-probe-ui", first + second)
        if sys.platform == "win32":
            self.assertEqual(subprocess.CREATE_NO_WINDOW, execute.call_args_list[0].kwargs["creationflags"])
        self.assertIn("gui=NOT_RUN", output)

    def test_exact_stage_receipts_reject_wrong_implementations(self):
        seed = "probe_data_seed_ok migration=ok crud=ok import_export=ok failure_paths=ok\n"
        verify = "probe_data_restart_ok persistence=ok privacy_fixture=ok cleanup=ok\n"
        leak = "fabricated_private_payload"
        cases = [
            (0, seed + leak, ""),
            (0, verify, ""),
            (0, seed + seed, ""),
            (0, "probe_data_seed_ok\n", ""),
            (0, seed.rstrip("\n"), ""),
            (0, seed + "\n", ""),
            (0, seed, " "),
            (0, seed, leak),
            (1, seed, ""),
        ]
        for code, stdout, stderr in cases:
            with self.subTest(code=code, case=cases.index((code, stdout, stderr))):
                result, execute, output = self.invoke([subprocess.CompletedProcess([], code, stdout, stderr)])
                self.assertEqual(1, result)
                self.assertEqual(1, execute.call_count)
                self.assertNotIn(leak, output)
                self.assertNotIn("data_probe_native_restart_passed", output)
        for stdout in (seed, verify + verify, verify + leak):
            with self.subTest(verify_case=True):
                result, execute, output = self.invoke([
                    subprocess.CompletedProcess([], 0, seed, ""),
                    subprocess.CompletedProcess([], 0, stdout, ""),
                ])
                self.assertEqual(1, result)
                self.assertEqual(2, execute.call_count)
                self.assertNotIn(leak, output)
                self.assertNotIn("data_probe_native_restart_passed", output)

    def test_wrong_hash_refuses_before_process_launch(self):
        with patch.object(subprocess, "run") as execute:
            with self.assertRaisesRegex(ValueError, "Artifact identity check failed"):
                self.invoke([], digest="0" * 64)
            execute.assert_not_called()

    def test_outside_owned_output_refuses_before_process_launch(self):
        outside = self.base / self.entry.name
        outside.write_bytes(self.entry.read_bytes())
        with patch.object(subprocess, "run") as execute:
            with self.assertRaisesRegex(ValueError, "Use an owned probe output"):
                self.invoke([], entry=outside)
            execute.assert_not_called()

    def test_failed_native_output_never_echoes_untrusted_payload(self):
        result, execute, output = self.invoke([subprocess.CompletedProcess([], 1, "private_payload", "private_payload")])
        self.assertEqual(1, result)
        self.assertEqual(1, execute.call_count)
        self.assertNotIn("private_payload", output)


if __name__ == "__main__":
    unittest.main()
