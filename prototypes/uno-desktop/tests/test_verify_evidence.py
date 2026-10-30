import importlib.util
from pathlib import Path
import struct
import subprocess
import sys
import json
import tempfile
import unittest

MODULE = Path(__file__).resolve().parents[1] / "verify_evidence.py"
spec = importlib.util.spec_from_file_location("verify_evidence", MODULE)
evidence = importlib.util.module_from_spec(spec)
spec.loader.exec_module(evidence)


def properties():
    return {"PublishAot": "true", "PublishTrimmed": "true",
            "SuppressTrimAnalysisWarnings": "false", "TrimmerSingleWarn": "false",
            "NoWarn": "1701", "EnableTrimAnalyzer": "true", "EnableAotAnalyzer": "true",
            "TreatWarningsAsErrors": "true", "PublishSingleFile": "false", "WinAppSdkVersion": "1.8.260921001",
            "RuntimeIdentifier": "win-x64", "WindowsPackageType": "None",
            "NETCoreSdkVersion": "10.0.403", "MSBuildVersion": "18.9.15",
            "UnoVersion": "6.7.135", "TargetFramework": "net10.0-windows10.0.26100"}


def native_pe(managed=False):
    data = bytearray(512)
    data[:2] = b"MZ"
    struct.pack_into("<I", data, 0x3C, 0x80)
    data[0x80:0x84] = b"PE\0\0"
    struct.pack_into("<H", data, 0x84, 0x8664)
    struct.pack_into("<H", data, 0x94, 240)
    struct.pack_into("<H", data, 0x98, 0x20B)
    struct.pack_into("<I", data, 0x98 + 108, 16)
    if managed:
        struct.pack_into("<II", data, 0x98 + 112 + 14 * 8, 0x1000, 72)
    return data


class VerifyEvidenceTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.exe = self.root / "probe.exe"
        self.exe.write_bytes(native_pe())
        (self.root / "e_sqlite3.dll").write_bytes(native_pe())
        self.props = properties()
        self.log = "Generating native code\nSmartSnippets.UnoProbe -> output/\nprobe_publish_exit=0\n"

    def check(self):
        return evidence.validate(self.props, self.log, self.root, self.exe, "win-x64")

    def test_positive_control_build_only_not_runtime_certification(self):
        self.assertEqual([], self.check())

    def test_rejects_non_aot(self):
        self.props["PublishAot"] = "false"
        self.assertIn("property:PublishAot", self.check())

    def test_rejects_managed_executable(self):
        self.exe.write_bytes(native_pe(managed=True))
        self.assertIn("artifact:managed-entry", self.check())

    def test_rejects_suppressed_warnings(self):
        for field, value in [("NoWarn", "1701;IL2121"), ("NoWarn", "IL2*"),
                             ("SuppressTrimAnalysisWarnings", "true"),
                             ("TrimmerSingleWarn", "true"), ("EnableAotAnalyzer", "false")]:
            with self.subTest(field=field, value=value):
                self.props = properties()
                self.props[field] = value
                self.assertTrue(self.check())

    def test_rejects_actual_il2059_failure(self):
        self.log += "Trim analysis error IL2059: generated XAML initialization\n"
        self.assertIn("build:diagnostic", self.check())

    def test_rejects_failed_or_incomplete_publish(self):
        self.log = "Generating native code\nerror MSB3077: failed\n"
        self.assertIn("build:diagnostic", self.check())
        self.log = "Generating native code\n"
        self.assertIn("build:no-publish-receipt", self.check())

    def test_incremental_link_without_fresh_compile_is_rejected(self):
        self.log = "SmartSnippets.UnoProbe -> output/\nprobe_publish_exit=0\n"
        self.assertIn("build:no-publish-receipt", self.check())

    def test_native_apphost_with_managed_layout_is_rejected(self):
        for name in ["probe.dll", "probe.runtimeconfig.json", "probe.deps.json"]:
            with self.subTest(name=name):
                path = self.root / name
                path.write_bytes(b"fixture")
                self.assertIn("artifact:managed-apphost-layout", self.check())
                path.unlink()

    def test_singlefile_and_runtime_host_markers_are_rejected(self):
        for marker in evidence.HOST_MARKERS:
            with self.subTest(marker=marker):
                self.exe.write_bytes(native_pe() + marker)
                self.assertIn("artifact:jit-host", self.check())
        self.props["PublishSingleFile"] = "true"
        self.assertIn("property:PublishSingleFile", self.check())

    def test_windows_skia_silent_backend_switch_is_rejected(self):
        self.props["TargetFramework"] = "net10.0-desktop"
        self.assertIn("metadata:windows-native-head", self.check())
        self.props = properties()
        self.props["WinAppSdkVersion"] = ""
        self.assertIn("metadata:windows-native-head", self.check())

    def test_packaged_windows_and_disabled_warning_errors_rejected(self):
        self.props["WindowsPackageType"] = "MSIX"
        self.assertIn("property:WindowsPackageType", self.check())
        self.props["TreatWarningsAsErrors"] = "false"
        self.assertIn("property:TreatWarningsAsErrors", self.check())

    def test_exit_receipt_must_be_exact_line(self):
        self.log = self.log.replace("probe_publish_exit=0", "prefix_probe_publish_exit=0_invalid")
        self.assertIn("build:no-successful-exit", self.check())

    def test_executable_outside_output_rejected(self):
        output = self.root / "output"
        output.mkdir()
        result = evidence.validate(self.props, self.log, output, self.exe, "win-x64")
        self.assertIn("artifact:outside-output", result)

    def test_macos_dylib_loop_and_sqlite_payload(self):
        def macho(filetype, commands=b"", count=0):
            return struct.pack("<IIIIIIII", 0xFEEDFACF, 0x0100000C, 0, filetype,
                               count, len(commands), 0, 0) + commands
        name = b"@rpath/libcoreclr.dylib\0"
        command = struct.pack("<IIIIII", 0xC, 24 + len(name), 24, 0, 0, 0) + name
        self.assertIn("artifact:jit-runtime", evidence.inspect_executable(macho(2, command, 1), "osx-arm64"))
        self.exe.write_bytes(macho(2))
        (self.root / "libe_sqlite3.dylib").write_bytes(macho(6))
        self.props["RuntimeIdentifier"] = "osx-arm64"
        self.props["TargetFramework"] = "net10.0-desktop"
        self.assertEqual([], evidence.validate(self.props, self.log, self.root, self.exe, "osx-arm64"))
        self.props["TargetFramework"] = "net10.0-windows10.0.26100"
        self.assertIn("metadata:macos-desktop-head", evidence.validate(
            self.props, self.log, self.root, self.exe, "osx-arm64"))

    def test_cli_failure_output_does_not_echo_private_paths(self):
        private_props = self.root / "synthetic-private-token.json"
        private_props.write_text("invalid-json", encoding="utf-8")
        result = subprocess.run([sys.executable, str(MODULE), "--properties", str(private_props),
                                 "--log", str(private_props), "--artifact-dir", str(self.root),
                                 "--executable", str(self.exe), "--rid", "win-x64"],
                                capture_output=True, text=True, timeout=10)
        self.assertEqual(1, result.returncode)
        self.assertEqual(["input:invalid"], json.loads(result.stdout)["errors"])
        self.assertNotIn("synthetic-private-token", result.stdout + result.stderr)
        self.assertEqual("", result.stderr)

    def test_cli_unwraps_property_document_without_runtime_pass(self):
        propfile, logfile = self.root / "properties.json", self.root / "publish.log"
        propfile.write_text(json.dumps({"Properties": self.props}), encoding="utf-8")
        logfile.write_text(self.log, encoding="utf-8")
        result = subprocess.run([sys.executable, str(MODULE), "--properties", str(propfile),
                                 "--log", str(logfile), "--artifact-dir", str(self.root),
                                 "--executable", str(self.exe), "--rid", "win-x64"],
                                capture_output=True, text=True, timeout=10)
        self.assertEqual(0, result.returncode)
        self.assertEqual("BUILD_CHECKS_ONLY", json.loads(result.stdout)["result"])
        self.assertEqual("NOT RUN", json.loads(result.stdout)["runtimeGates"])

    def test_rejects_jit_payload_in_nested_bundle(self):
        nested = self.root / "nested"
        nested.mkdir()
        (nested / "coreclr.dll").write_bytes(b"fixture")
        self.assertIn("artifact:jit-runtime", self.check())

    def test_rejects_missing_metadata(self):
        del self.props["NETCoreSdkVersion"]
        self.assertIn("metadata:NETCoreSdkVersion", self.check())

    def test_rejects_missing_native_sqlite(self):
        (self.root / "e_sqlite3.dll").unlink()
        self.assertIn("artifact:sqlite-missing", self.check())

    def test_rejects_corrupt_sqlite_payload(self):
        (self.root / "e_sqlite3.dll").write_bytes(b"not-a-native-library")
        self.assertIn("artifact:sqlite-invalid", self.check())

    def test_rejects_wrong_architecture(self):
        data = native_pe()
        struct.pack_into("<H", data, 0x84, 0xAA64)
        self.exe.write_bytes(data)
        self.assertIn("artifact:wrong-architecture", self.check())

    def test_rejects_invalid_or_truncated_binary(self):
        for content in [b"MZ", b"not-a-binary", b"\xcf\xfa\xed\xfe"]:
            self.exe.write_bytes(content)
            self.assertTrue(self.check())

    def test_rejects_missing_exit_receipt(self):
        self.log = self.log.replace("probe_publish_exit=0", "probe_publish_exit=1")
        self.assertIn("build:no-successful-exit", self.check())

    def test_macho_header_requires_arm64_executable(self):
        data = struct.pack("<IIIIIIII", 0xFEEDFACF, 0x0100000C, 0, 2, 0, 0, 0, 0)
        self.assertEqual([], evidence.inspect_executable(data, "osx-arm64"))
        self.assertIn("artifact:wrong-architecture", evidence.inspect_executable(
            struct.pack("<IIIIIIII", 0xFEEDFACF, 0x01000007, 0, 2, 0, 0, 0, 0), "osx-arm64"))
        self.assertIn("artifact:invalid-macho", evidence.inspect_executable(
            struct.pack("<IIIIIIII", 0xFEEDFACF, 0x0100000C, 0, 2, 3, 0, 0, 0), "osx-arm64"))

    def test_all_warnings_fail_but_zero_warning_summary_is_allowed(self):
        self.log += "0 Warning(s)\n0 Error(s)\n"
        self.assertEqual([], self.check())
        self.log += "warning NETSDK1198: missing publish profile\n"
        self.assertIn("build:diagnostic", self.check())

    def test_rejects_wrong_rid(self):
        self.props["RuntimeIdentifier"] = "osx-arm64"
        self.assertIn("metadata:RuntimeIdentifier", self.check())


if __name__ == "__main__":
    unittest.main()
