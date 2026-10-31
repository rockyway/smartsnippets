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
spec = importlib.util.spec_from_file_location("general_snapshot_runner", BASE / "run_general_snapshot_probe.py")
runner = importlib.util.module_from_spec(spec)
spec.loader.exec_module(runner)


LINES = ['general_snapshot status=COMPLETE reason=complete secret_buffers=ZEROED mutation=NONE input=NONE', runner.FOOTER]


class GeneralSnapshotReceiptTests(unittest.TestCase):
    def test_exact_status_reason_and_clean_exit_required(self):
        self.assertEqual('COMPLETE', runner.observed("\n".join(LINES), '', 0))
        for status, reasons in runner.REASONS.items():
            for reason in reasons:
                line = f'general_snapshot status={status} reason={reason} secret_buffers=ZEROED mutation=NONE input=NONE'
                self.assertEqual(status, runner.observed(line + "\n" + runner.FOOTER, '', 0))
        for lines, stderr, code in [
            (LINES[:1], '', 0), (LINES[1:], '', 0), (LINES + LINES[-1:], '', 0),
            (LINES, 'diagnostic', 0), (LINES, '', None), (LINES, '', 1),
            ([LINES[0].replace('reason=complete', 'reason=permission'), LINES[1]], '', 0),
            ([LINES[0].replace('ZEROED', 'RETAINED'), LINES[1]], '', 0),
        ]:
            self.assertIsNone(runner.observed("\n".join(lines), stderr, code))


class GeneralSnapshotRunnerTests(unittest.TestCase):
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
        self.args = ["runner", "--authorized-clipboard-read", "--executable", str(self.entry), "--sha256", hashlib.sha256(self.entry.read_bytes()).hexdigest()]

    def invoke(self, *, platform="darwin", timeout=False, stderr=b"", header=None, launch=True):
        def execute(args, **kwargs):
            kwargs["stdout"].write(("\n".join(LINES) + "\n").encode())
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

    def test_only_explicit_read_non_gui_flags_and_bounded_child(self):
        result, execute, output = self.invoke()
        self.assertEqual(0, result)
        self.assertEqual([str(self.entry), "--probe-general-snapshot", "--authorized-probe-clipboard"], execute.call_args.args[0])
        self.assertEqual(20, execute.call_args.kwargs["timeout"])
        self.assertEqual(subprocess.DEVNULL, execute.call_args.kwargs["stdin"])
        self.assertIn("general_snapshot_observation=COMPLETE scope=READ_ONLY", output)

    def test_timeout_cannot_pass_even_with_all_receipts(self):
        result, _, output = self.invoke(timeout=True)
        self.assertEqual(1, result)
        self.assertIn("observation=FAILED", output)

    def test_private_diagnostics_are_not_echoed(self):
        result, _, output = self.invoke(stderr=b"private synthetic diagnostic must not be echoed")
        self.assertEqual(1, result)
        self.assertNotIn("must not be echoed", output)

    def test_wrong_platform_hash_bundle_or_header_never_launches(self):
        original = self.info.read_bytes()
        digest = self.args[-1]
        for failure in ("platform", "hash", "bundle", "header", "authorization"):
            if failure == "authorization":
                self.args.remove("--authorized-clipboard-read")
            with self.subTest(failure=failure):
                self.info.write_bytes(original)
                self.args[-1] = "0" * 64 if failure == "hash" else digest
                if failure == "bundle":
                    self.info.write_bytes(plistlib.dumps({"CFBundleIdentifier": "wrong"}))
                with self.assertRaises(ValueError):
                    self.invoke(platform="linux" if failure == "platform" else "darwin", header=["invalid"] if failure == "header" else None, launch=False)


if __name__ == "__main__":
    unittest.main()
