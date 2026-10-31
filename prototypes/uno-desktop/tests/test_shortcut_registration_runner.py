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
spec = importlib.util.spec_from_file_location("shortcut_registration_runner", BASE / "run_shortcut_registration_probe.py")
runner = importlib.util.module_from_spec(spec)
spec.loader.exec_module(runner)


class ShortcutRegistrationRunnerTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.base = Path(self.temp.name).resolve()
        self.private = self.base / "private"
        self.private.mkdir()

    def invoke(self, *, windows=False, authorize=True, timeout=False, stderr=b"", invalid=None):
        root = self.base / "bin/probe/run"
        entry = root / "SmartSnippets.UnoProbe.exe" if windows else root / "SmartSnippets.UnoProbe.app/Contents/MacOS/SmartSnippets.UnoProbe"
        entry.parent.mkdir(parents=True, exist_ok=True)
        entry.write_bytes(b"non-executable fixture")
        if not windows:
            (entry.parents[1] / "Info.plist").write_bytes(plistlib.dumps({
                "CFBundleIdentifier": "wrong" if invalid == "bundle" else "org.smartsnippets.feasibility018",
                "CFBundleExecutable": entry.name}))
        digest = "0" * 64 if invalid == "hash" else hashlib.sha256(entry.read_bytes()).hexdigest()
        args = ["runner", "--executable", str(entry), "--sha256", digest]
        if authorize:
            args.append("--authorized-screen-slot")
        def execute(arguments, **kwargs):
            self.assertEqual([str(entry), "--probe-shortcut-register", "--authorized-probe-shortcut"], arguments)
            self.assertEqual(subprocess.DEVNULL, kwargs["stdin"])
            self.assertEqual(20, kwargs["timeout"])
            kwargs["stdout"].write(("\n".join(runner.RECEIPTS) + "\n").encode())
            kwargs["stdout"].flush()
            kwargs["stderr"].write(stderr)
            kwargs["stderr"].flush()
            if timeout:
                raise subprocess.TimeoutExpired("owned", 20)
            return subprocess.CompletedProcess(arguments, 0)
        output = io.StringIO()
        with patch.object(runner.sys, "platform", "win32" if windows else "darwin"), \
             patch.object(subprocess, "CREATE_NO_WINDOW", 0x08000000, create=True), \
             patch.object(sys, "argv", args), patch.object(runner, "BASE", self.base), \
             patch.object(runner, "inspect_executable", return_value=["invalid"] if invalid == "header" else []), \
             patch.object(runner, "private_directory", return_value=self.private), \
             patch.object(runner.subprocess, "run", side_effect=execute) as run, contextlib.redirect_stdout(output):
            try:
                result = runner.main()
            except ValueError:
                run.assert_not_called()
                raise
        return result, run, output.getvalue()

    def test_windows_owned_non_gui_flags_and_hidden_child(self):
        result, run, output = self.invoke(windows=True)
        self.assertEqual(0, result)
        self.assertEqual(0x08000000, run.call_args.kwargs["creationflags"])
        self.assertIn("delivery=NOT_RUN", output)

    def test_mac_owned_non_gui_flags(self):
        result, run, _ = self.invoke()
        self.assertEqual(0, result)
        self.assertEqual(0, run.call_args.kwargs["creationflags"])

    def test_no_authorization_or_bad_hash_bundle_header_never_launches(self):
        with self.assertRaises(ValueError):
            self.invoke(authorize=False)
        for invalid in ("hash", "bundle", "header"):
            with self.subTest(invalid=invalid), self.assertRaises(ValueError):
                self.invoke(invalid=invalid)

    def test_timeout_and_stderr_cannot_pass_or_echo_private_diagnostics(self):
        self.assertEqual(1, self.invoke(timeout=True)[0])
        result, _, output = self.invoke(stderr=b"private diagnostic must not echo")
        self.assertEqual(1, result)
        self.assertNotIn("must not echo", output)

    def test_every_receipt_clean_stderr_and_natural_exit_required(self):
        lines = runner.RECEIPTS
        self.assertTrue(runner.completed("\n".join(lines), "", 0))
        for i in range(len(lines)):
            self.assertFalse(runner.completed("\n".join(lines[:i] + lines[i + 1:]), "", 0))
        self.assertFalse(runner.completed("\n".join(lines + [lines[-1]]), "", 0))
        self.assertFalse(runner.completed("\n".join(lines), "", None))
        self.assertFalse(runner.completed("\n".join(lines), "", 1))
        self.assertFalse(runner.completed("\n".join(lines), "error", 0))


if __name__ == "__main__":
    unittest.main()
