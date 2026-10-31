import contextlib
import hashlib
import importlib.util
import io
from pathlib import Path
import plistlib
import subprocess
import sys
import tempfile
import unittest
from unittest.mock import patch

BASE = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(BASE))
spec = importlib.util.spec_from_file_location("private_pasteboard_runner", BASE / "run_private_pasteboard_probe.py")
runner = importlib.util.module_from_spec(spec)
spec.loader.exec_module(runner)


class PrivatePasteboardReceiptTests(unittest.TestCase):
    def test_all_native_hops_and_natural_exit_are_required(self):
        lines = runner.RECEIPTS
        self.assertTrue(runner.completed("\n".join(lines), "", 0))
        for i in range(len(lines)):
            self.assertFalse(runner.completed("\n".join(lines[:i] + lines[i + 1:]), "", 0))
        self.assertFalse(runner.completed("\n".join(lines + [lines[-1]]), "", 0))
        self.assertFalse(runner.completed("\n".join(lines), "", None))
        self.assertFalse(runner.completed("\n".join(lines), "", 1))
        self.assertFalse(runner.completed("\n".join(lines), "diagnostic", 0))


class PrivatePasteboardRunnerTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.base = Path(self.temp.name).resolve()
        self.entry = self.base / "bin/probe/run/SmartSnippets.UnoProbe.app/Contents/MacOS/SmartSnippets.UnoProbe"
        self.entry.parent.mkdir(parents=True)
        self.entry.write_bytes(b"non-executable fixture")
        self.info = self.entry.parents[1] / "Info.plist"
        self.info.write_bytes(plistlib.dumps({"CFBundleIdentifier": "org.smartsnippets.feasibility018", "CFBundleExecutable": self.entry.name}))
        self.private = self.base / "private"
        self.private.mkdir()
        self.args = ["runner", "--executable", str(self.entry), "--sha256", hashlib.sha256(self.entry.read_bytes()).hexdigest()]

    def invoke(self, *, platform="darwin", timeout=False, stderr=b"", header=None, launch=True):
        def execute(args, **kwargs):
            kwargs["stdout"].write(("\n".join(runner.RECEIPTS) + "\n").encode())
            kwargs["stdout"].flush()
            kwargs["stderr"].write(stderr)
            kwargs["stderr"].flush()
            if timeout:
                raise subprocess.TimeoutExpired("owned", 20)
            return subprocess.CompletedProcess(args, 0)
        output = io.StringIO()
        with patch.object(runner.sys, "platform", platform), patch.object(sys, "argv", self.args), \
             patch.object(runner, "BASE", self.base), patch.object(runner, "inspect_executable", return_value=header or []), \
             patch.object(runner, "private_directory", return_value=self.private), \
             patch.object(runner.subprocess, "run", side_effect=execute) as execute_mock, contextlib.redirect_stdout(output):
            try:
                result = runner.main()
            finally:
                self.assertEqual(int(launch), execute_mock.call_count)
        return result, execute_mock, output.getvalue()

    def test_only_private_non_gui_flag_and_bounded_child(self):
        result, execute, output = self.invoke()
        self.assertEqual(0, result)
        self.assertEqual([str(self.entry), "--probe-private-pasteboard"], execute.call_args.args[0])
        self.assertEqual(20, execute.call_args.kwargs["timeout"])
        self.assertEqual(subprocess.DEVNULL, execute.call_args.kwargs["stdin"])
        self.assertIn("private_pasteboard_result=PASS scope=PRIVATE_ONLY", output)

    def test_timeout_cannot_pass_even_with_all_receipts(self):
        result, _, output = self.invoke(timeout=True)
        self.assertEqual(1, result)
        self.assertIn("result=FAIL", output)

    def test_private_diagnostics_are_not_echoed(self):
        result, _, output = self.invoke(stderr=b"private synthetic diagnostic must not be echoed")
        self.assertEqual(1, result)
        self.assertNotIn("must not be echoed", output)

    def test_wrong_platform_hash_bundle_or_header_never_launches(self):
        original = self.info.read_bytes()
        digest = self.args[-1]
        for failure in ("platform", "hash", "bundle", "header"):
            with self.subTest(failure=failure):
                self.info.write_bytes(original)
                self.args[-1] = "0" * 64 if failure == "hash" else digest
                if failure == "bundle":
                    self.info.write_bytes(plistlib.dumps({"CFBundleIdentifier": "wrong"}))
                with self.assertRaises(ValueError):
                    self.invoke(platform="linux" if failure == "platform" else "darwin", header=["invalid"] if failure == "header" else None, launch=False)


if __name__ == "__main__":
    unittest.main()
