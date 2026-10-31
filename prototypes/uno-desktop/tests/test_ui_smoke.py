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

    def invoke(self, *, authorize=True, helper=None, interrupt_wait=False, lifecycle=None,
               coordinator=False, preview=False, companion=False, preview_row=False, extra_args=None, stderr=b""):
        process = Mock(pid=12345)
        process.poll.return_value = None
        process.wait.return_value = 0
        if interrupt_wait:
            process.wait.side_effect = [KeyboardInterrupt(), 0]
        output = io.StringIO()
        err_output = io.StringIO()
        args = self.args + (["--authorized-screen-slot"] if authorize else [])
        if lifecycle:
            args.append("--preview-row" if preview_row else "--companion" if companion else "--preview" if preview else "--coordinator" if coordinator else "--lifecycle")
            if lifecycle == "success":
                process.poll.side_effect = [None, 0, 0]
            elif lifecycle == "early_exit":
                process.poll.return_value = 0
            elif lifecycle == "stubborn":
                process.wait.side_effect = [subprocess.TimeoutExpired("owned-app", 30),
                                            subprocess.TimeoutExpired("owned-app", 3), 0]
            else:
                process.wait.side_effect = [subprocess.TimeoutExpired("owned-app", 30), 0]
        if extra_args:
            args.extend(extra_args)

        def start(arguments, **kwargs):
            if lifecycle:
                receipts = (smoke.PREVIEW_ROW_RECEIPTS if preview_row else smoke.COMPANION_RECEIPTS if companion else smoke.PREVIEW_RECEIPTS if preview
                            else smoke.COORDINATOR_RECEIPTS if coordinator else smoke.LIFECYCLE_RECEIPTS)
                kwargs["stdout"].write(("\n".join(receipts) + "\n").encode())
                kwargs["stdout"].flush()
                if stderr:
                    kwargs["stderr"].write(stderr)
                    kwargs["stderr"].flush()
            return process
        results = helper or [subprocess.CompletedProcess([], 0, "1\n", "")]
        if (coordinator or preview) and helper is None:
            results.append(subprocess.CompletedProcess([], 0, "owned_focus_prepared\n", ""))
        with patch.object(sys, "argv", args), patch.object(smoke, "BASE", self.base), \
             patch.object(smoke, "inspect_executable", return_value=[]), \
             patch.object(smoke, "private_directory", return_value=self.private), \
             patch.object(subprocess, "Popen", side_effect=start) as launch, \
             patch.object(subprocess, "run", side_effect=results), \
             contextlib.redirect_stdout(output), contextlib.redirect_stderr(err_output):
            try:
                result = smoke.main()
            except BaseException as error:
                return error, process, launch, output.getvalue()
        return result, process, launch, output.getvalue()

    def test_preview_row_no_focus_preparation_or_stdin(self):
        result, process, launch, output = self.invoke(lifecycle="success", preview_row=True)
        self.assertEqual(0, result)
        self.assertEqual([str(self.entry), "--authorized-probe-ui", "--preview-row"], launch.call_args.args[0])
        self.assertEqual(subprocess.DEVNULL, launch.call_args.kwargs["stdin"])
        self.assertIn("input=NONE", output)
        self.assertIn("ui_preview_row_result=PASS", output)
        process.terminate.assert_not_called()

    def test_preview_row_mutually_exclusive(self):
        result, _, launch, _ = self.invoke(extra_args=["--preview-row", "--preview"])
        self.assertIsInstance(result, SystemExit)
        launch.assert_not_called()

    def test_preview_row_receipts_reject_native_enter_and_missing_proof(self):
        self.assertFalse(smoke.preview_row_completed("\n".join(smoke.PREVIEW_RECEIPTS), "", 0))
        self.assertFalse(smoke.preview_row_completed("\n".join(smoke.PREVIEW_ROW_RECEIPTS[:-1]), "", 0))
        self.assertFalse(smoke.preview_row_completed("\n".join(smoke.PREVIEW_ROW_RECEIPTS), "error", 0))

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

    def test_coordinator_uses_only_reviewed_scene_flag_and_requires_natural_exit(self):
        result, process, launch, output = self.invoke(lifecycle="success", coordinator=True)
        self.assertEqual(0, result)
        self.assertEqual([str(self.entry), "--authorized-probe-ui", "--probe-ui-coordinator"], launch.call_args.args[0])
        process.terminate.assert_not_called()
        process.stdin.write.assert_called_once_with(b"probe_foreground_prepared\n")
        process.stdin.close.assert_called_once_with()
        self.assertIn("ui_coordinator_result=PASS graceful_exit=true", output)

    def test_preview_requires_preparation_routed_receipts_and_natural_exit(self):
        result, process, launch, output = self.invoke(lifecycle="success", preview=True)
        self.assertEqual(0, result)
        self.assertEqual([str(self.entry), "--authorized-probe-ui", "--probe-ui-preview"], launch.call_args.args[0])
        process.stdin.write.assert_called_once_with(b"probe_foreground_prepared\n")
        process.terminate.assert_not_called()
        self.assertIn("ui_preview_result=PASS graceful_exit=true", output)

    def test_preview_receipts_cannot_pass_watchdog_cleanup(self):
        result, process, launch, output = self.invoke(lifecycle="hung", preview=True)
        self.assertEqual(1, result)
        process.terminate.assert_called_once()
        self.assertIn("ui_preview_result=FAIL", output)

    def test_preview_refuses_preparation_failure_without_signalling(self):
        responses = [subprocess.CompletedProcess([], 0, "1\n", ""),
                     subprocess.CompletedProcess([], 1, "", "refused")]
        result, process, launch, output = self.invoke(lifecycle="success", preview=True, helper=responses)
        self.assertEqual(1, result)
        process.stdin.write.assert_not_called()
        self.assertIn("ui_preview_result=FAIL", output)

    @unittest.skipUnless(sys.platform == "win32", "Windows mouse-journal backstop")
    def test_preview_recovery_failure_cannot_pass_native_receipts(self):
        (self.private / "focus.json").write_text("{}")
        with patch.object(smoke, "replay_mouse_journal", return_value=False) as replay:
            result, process, launch, output = self.invoke(lifecycle="success", preview=True)
        self.assertEqual(1, result)
        replay.assert_called_once()
        self.assertIn("mouse_recovery=INCOMPLETE", output)
        self.assertIn("ui_preview_result=FAIL", output)

    def test_coordinator_refuses_preparation_failure_before_signal(self):
        responses = [subprocess.CompletedProcess([], 0, "1\n", ""),
                     subprocess.CompletedProcess([], 1, "", "owned focus refused")]
        result, process, launch, output = self.invoke(lifecycle="success", coordinator=True, helper=responses)
        self.assertEqual(1, result)
        process.stdin.write.assert_not_called()
        self.assertIn("ui_coordinator_result=FAIL", output)
        self.assertNotIn("owned focus refused", output)

    @unittest.skipUnless(sys.platform == "win32", "Windows mouse-journal backstop")
    def test_failed_recovery_cannot_pass_complete_native_receipts(self):
        (self.private / "focus.json").write_text("{}")
        with patch.object(smoke, "replay_mouse_journal", return_value=False) as replay:
            result, process, launch, output = self.invoke(lifecycle="success", coordinator=True)
        self.assertEqual(1, result)
        replay.assert_called_once()
        self.assertIn("mouse_recovery=INCOMPLETE", output)
        self.assertIn("ui_coordinator_result=FAIL", output)

    @unittest.skipUnless(sys.platform == "win32", "Windows mouse-journal backstop")
    def test_interrupted_recovery_still_stops_owned_child(self):
        (self.private / "focus.json").write_text("{}")
        with patch.object(smoke, "replay_mouse_journal", side_effect=KeyboardInterrupt):
            result, process, launch, output = self.invoke(lifecycle="hung", coordinator=True)
        self.assertIsInstance(result, KeyboardInterrupt)
        process.terminate.assert_called_once_with()
        self.assertEqual(2, process.wait.call_count)

    def test_coordinator_receipts_do_not_pass_a_hung_process(self):
        result, process, launch, output = self.invoke(lifecycle="hung", coordinator=True)
        self.assertEqual(1, result)
        process.terminate.assert_called_once_with()
        self.assertIn("ui_coordinator_result=FAIL graceful_exit=false", output)

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
            self.assertEqual([], list(root.iterdir()))
            if sys.platform == "darwin":
                self.assertEqual(0o700, root.stat().st_mode & 0o777)
        finally:
            root.rmdir()

    def test_private_directory_removed_after_permission_setup_failure(self):
        if sys.platform == "win32":
            with patch.object(smoke, "create_windows_private_directory", side_effect=OSError("synthetic")):
                with self.assertRaises(OSError):
                    smoke.private_directory()
            return
        doomed = self.base / "doomed"
        doomed.mkdir()
        with patch.object(smoke.tempfile, "mkdtemp", return_value=str(doomed)):
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

    def test_companion_selects_native_flag_without_input_preparation_or_recovery(self):
        (self.private / "focus.json").write_text("{}")
        observer = Mock(return_value=subprocess.CompletedProcess([], 0, "1\n", ""))
        with patch.object(smoke, "replay_mouse_journal") as replay, \
             patch.object(subprocess, "run", side_effect=observer):
            args = self.args + ["--authorized-screen-slot", "--companion"]
            process = Mock(pid=12345)
            process.poll.side_effect = [None, 0, 0]
            process.wait.return_value = 0
            output = io.StringIO()

            def start(arguments, **kwargs):
                kwargs["stdout"].write(("\n".join(smoke.COMPANION_RECEIPTS) + "\n").encode())
                kwargs["stdout"].flush()
                return process

            with patch.object(sys, "argv", args), patch.object(smoke, "BASE", self.base), \
                 patch.object(smoke, "inspect_executable", return_value=[]), \
                 patch.object(smoke, "private_directory", return_value=self.private), \
                 patch.object(subprocess, "Popen", side_effect=start) as launch, \
                 contextlib.redirect_stdout(output):
                result = smoke.main()
        self.assertEqual(0, result)
        self.assertEqual([str(self.entry), "--authorized-probe-ui", "--probe-ui-companion"], launch.call_args.args[0])
        self.assertEqual(self.entry.parent, launch.call_args.kwargs["cwd"])
        self.assertEqual(subprocess.DEVNULL, launch.call_args.kwargs["stdin"])
        if sys.platform == "win32":
            self.assertEqual(subprocess.CREATE_NO_WINDOW, launch.call_args.kwargs["creationflags"])
        else:
            self.assertEqual(0, launch.call_args.kwargs["creationflags"])
        observer.assert_called_once()
        self.assertEqual("--window-count", observer.call_args.args[0][2])
        process.stdin.write.assert_not_called()
        process.stdin.close.assert_not_called()
        replay.assert_not_called()
        process.terminate.assert_not_called()
        process.kill.assert_not_called()
        text = output.getvalue()
        self.assertIn("ui_smoke_window=OBSERVED owned_process_stopped=true clipboard=UNTOUCHED input=NONE", text)
        self.assertNotIn("mouse_recovery=", text)
        self.assertIn("ui_companion_result=PASS graceful_exit=true", text)

    def test_companion_cross_platform_mac_bundle_and_creationflags(self):
        bundle_macos = self.base / "bin" / "probe" / "mac-fixture" / "SmartSnippets.UnoProbe.app" / "Contents" / "MacOS"
        bundle_macos.mkdir(parents=True, exist_ok=True)
        mac_entry = bundle_macos / "SmartSnippets.UnoProbe"
        mac_entry.write_bytes(b"synthetic arm64 fixture")
        digest = hashlib.sha256(mac_entry.read_bytes()).hexdigest()
        with (bundle_macos.parent / "Info.plist").open("wb") as stream:
            plistlib.dump({"CFBundleIdentifier": "org.smartsnippets.feasibility018",
                           "CFBundleExecutable": "SmartSnippets.UnoProbe"}, stream)
        args = ["run_ui_smoke.py", "--executable", str(mac_entry), "--sha256", digest,
                "--authorized-screen-slot", "--companion"]
        process = Mock(pid=23456)
        process.poll.side_effect = [None, 0, 0]
        process.wait.return_value = 0
        output = io.StringIO()

        def start(arguments, **kwargs):
            kwargs["stdout"].write(("\n".join(smoke.COMPANION_RECEIPTS) + "\n").encode())
            kwargs["stdout"].flush()
            return process

        with patch.object(smoke.sys, "platform", "darwin"), patch.object(sys, "argv", args), \
             patch.object(smoke, "BASE", self.base), patch.object(smoke, "inspect_executable", return_value=[]), \
             patch.object(smoke, "private_directory", return_value=self.private), \
             patch.object(subprocess, "Popen", side_effect=start) as launch, \
             patch.object(subprocess, "run", return_value=subprocess.CompletedProcess([], 0, "1\n", "")), \
             contextlib.redirect_stdout(output):
            result = smoke.main()
        self.assertEqual(0, result)
        self.assertEqual([str(mac_entry), "--authorized-probe-ui", "--probe-ui-companion"], launch.call_args.args[0])
        self.assertEqual(subprocess.DEVNULL, launch.call_args.kwargs["stdin"])
        self.assertEqual(0, launch.call_args.kwargs["creationflags"])
        self.assertIn("input=NONE", output.getvalue())
        self.assertIn("ui_companion_result=PASS graceful_exit=true", output.getvalue())

    def test_companion_cli_rejects_missing_slot_and_conflicting_scene_flags(self):
        result, _, launch, _ = self.invoke(authorize=False, lifecycle="success", companion=True)
        self.assertIsInstance(result, ValueError)
        launch.assert_not_called()
        for conflict in ("--lifecycle", "--coordinator", "--preview"):
            with self.subTest(conflict=conflict):
                result, _, launch, _ = self.invoke(lifecycle="success", companion=True, extra_args=[conflict])
                self.assertIsInstance(result, SystemExit)
                self.assertEqual(2, result.code)
                launch.assert_not_called()

    def test_companion_timeout_and_stubborn_hang_escalate_owned_cleanup(self):
        result, process, _, output = self.invoke(lifecycle="hung", companion=True)
        self.assertEqual(1, result)
        process.terminate.assert_called_once_with()
        process.kill.assert_not_called()
        self.assertIn("input=NONE", output)
        self.assertIn("ui_companion_result=FAIL graceful_exit=false", output)

        result, process, _, output = self.invoke(lifecycle="stubborn", companion=True)
        self.assertEqual(1, result)
        process.terminate.assert_called_once_with()
        process.kill.assert_called_once_with()
        self.assertEqual(3, process.wait.call_count)
        self.assertIn("ui_companion_result=FAIL graceful_exit=false", output)

    def test_companion_early_exit_or_stderr_fails_without_echoing_diagnostics(self):
        result, process, _, output = self.invoke(lifecycle="early_exit", companion=True)
        self.assertEqual(1, result)
        process.terminate.assert_not_called()
        self.assertIn("ui_smoke_window=NOT_OBSERVED", output)
        self.assertIn("ui_companion_result=FAIL graceful_exit=false", output)

        result, _, _, output = self.invoke(lifecycle="success", companion=True,
                                           stderr=b"ui_companion_failed stage=3 kind=InvalidOperationException secret")
        self.assertEqual(1, result)
        self.assertIn("ui_companion_result=FAIL graceful_exit=false", output)
        self.assertNotIn("secret", output)


class CompanionReceiptTests(unittest.TestCase):
    def setUp(self):
        self.lines = [
            "ui_companion_started",
            "ui_companion_external_geometry_ok binding=COMPILED foreground=UNCHANGED",
            "ui_companion_scroll_ok",
            "ui_companion_hidden",
            "ui_companion_hover_policy_opened",
            "ui_companion_handoff_ok",
            "ui_companion_native_closed",
            "ui_companion_complete managed_closed=2 native_absence=1 input=NONE clipboard=UNTOUCHED",
        ]

    def test_every_companion_hop_order_and_clean_exit_required(self):
        self.assertEqual(self.lines, smoke.COMPANION_RECEIPTS)
        self.assertTrue(smoke.companion_completed("\n".join(self.lines) + "\n", "", 0))
        for index in range(len(self.lines)):
            with self.subTest(index=index):
                self.assertFalse(smoke.companion_completed("\n".join(self.lines[:index] + self.lines[index + 1:]), "", 0))
        swapped = list(self.lines)
        swapped[4], swapped[5] = swapped[5], swapped[4]
        self.assertFalse(smoke.companion_completed("\n".join(swapped), "", 0))
        self.assertFalse(smoke.companion_completed("\n".join(reversed(self.lines)), "", 0))
        self.assertFalse(smoke.companion_completed("\n".join(self.lines + [self.lines[-1]]), "", 0))
        self.assertFalse(smoke.companion_completed("\n".join(self.lines), "", None))
        self.assertFalse(smoke.companion_completed("\n".join(self.lines), "", 1))
        self.assertFalse(smoke.companion_completed("\n".join(self.lines), "", -15))
        self.assertFalse(smoke.companion_completed("\n".join(self.lines), "ui_companion_failed", 0))

    def test_tampered_safety_or_binding_tokens_rejected(self):
        for index, replacement in [
            (1, "ui_companion_external_geometry_ok binding=COMPILED foreground=CHANGED"),
            (1, "ui_companion_external_geometry_ok binding=REFLECTED foreground=UNCHANGED"),
            (7, "ui_companion_complete managed_closed=1 native_absence=1 input=NONE clipboard=UNTOUCHED"),
            (7, "ui_companion_complete managed_closed=2 native_absence=0 input=NONE clipboard=UNTOUCHED"),
            (7, "ui_companion_complete managed_closed=2 native_absence=1 input=MOUSE clipboard=UNTOUCHED"),
            (7, "ui_companion_complete managed_closed=2 native_absence=1 input=NONE clipboard=MODIFIED"),
        ]:
            with self.subTest(replacement=replacement):
                candidate = list(self.lines)
                candidate[index] = replacement
                self.assertFalse(smoke.companion_completed("\n".join(candidate), "", 0))


class PreviewReceiptTests(unittest.TestCase):
    def test_every_native_preview_hop_and_clean_exit_required(self):
        lines = smoke.PREVIEW_RECEIPTS
        self.assertTrue(smoke.preview_completed("\n".join(lines), "", 0))
        for index in range(len(lines)):
            self.assertFalse(smoke.preview_completed("\n".join(lines[:index] + lines[index + 1:]), "", 0))
        self.assertFalse(smoke.preview_completed("\n".join(lines), "", None))
        self.assertFalse(smoke.preview_completed("\n".join(lines), "failure", 0))
        self.assertFalse(smoke.preview_completed("\n".join(reversed(lines)), "", 0))
        self.assertFalse(smoke.preview_completed("\n".join(lines + [lines[-1]]), "", 0))


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


class CoordinatorReceiptTests(unittest.TestCase):
    def setUp(self):
        self.lines = [
            "ui_coordinator_started", "ui_coordinator_warmup_started", "ui_coordinator_cold_after_cancel",
            "ui_coordinator_warmup_started", "ui_coordinator_native_layout_ready", "ui_coordinator_ready_show",
            "ui_coordinator_focus_applied", "ui_coordinator_native_focus_ok", "ui_coordinator_hidden",
            "ui_coordinator_ready_show", "ui_coordinator_focus_applied", "ui_coordinator_reopen_focus_ok",
            "ui_coordinator_complete native_focus=2 warmup_starts=2 clipboard=UNTOUCHED keyboard=NONE",
        ]

    def test_actual_order_and_natural_exit_required(self):
        self.assertTrue(smoke.coordinator_completed("\n".join(self.lines), "", 0))
        self.assertFalse(smoke.coordinator_completed("\n".join(self.lines), "", None))
        self.assertFalse(smoke.coordinator_completed("\n".join(self.lines), "failure", 0))
        self.assertFalse(smoke.coordinator_completed("\n".join(reversed(self.lines)), "", 0))

    def test_disconnected_warmup_or_focus_callback_cannot_pass(self):
        for index in range(len(self.lines)):
            with self.subTest(index=index):
                self.assertFalse(smoke.coordinator_completed("\n".join(self.lines[:index] + self.lines[index + 1:]), "", 0))


if __name__ == "__main__":
    unittest.main()
