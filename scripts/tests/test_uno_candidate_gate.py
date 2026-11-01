"""Planted negative controls for build-only checks; no executable is launched."""
import importlib.util
import json
import subprocess
import sys
from pathlib import Path
import os
import struct
import tempfile
import unittest
from unittest.mock import patch

SCRIPT = Path(__file__).resolve().parents[1] / 'uno_candidate_gate.py'
SPEC = importlib.util.spec_from_file_location('uno_candidate_gate', SCRIPT)
gate = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(gate)


def pe(machine=0x8664, managed=False):
    data = bytearray(512)
    data[:2] = b'MZ'
    struct.pack_into('<I', data, 0x3C, 64)
    data[64:68] = b'PE\0\0'
    struct.pack_into('<H', data, 68, machine)
    struct.pack_into('<H', data, 84, 240)
    struct.pack_into('<H', data, 88, 0x20B)
    struct.pack_into('<I', data, 196, 16)
    if managed:
        struct.pack_into('<II', data, 312, 4096, 72)
    return data


class CandidateGateTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.properties = {name: 'true' for name in gate.TRUE_PROPERTIES}
        self.properties.update({name: 'false' for name in gate.FALSE_PROPERTIES})
        self.properties.update(gate.EXACT_PROPERTIES)
        self.properties.update(NoWarn='', MSBuildVersion='18.0.0')
        self.log = ('Generating native code\nSmartSnippets.Uno -> owned-output\n'
                    '0 Warning(s)\n0 Error(s)\ncandidate_publish_exit=0\n')
        self.started = 10_000
        for name in (gate.ENTRY, 'e_sqlite3.dll'):
            path = self.root / name
            path.write_bytes(pe())
            os.utime(path, ns=(20_000_000_000, 20_000_000_000))

    def errors(self):
        return gate.validate(self.properties, self.log, self.root, self.started)

    def test_valid_build_only_fixture(self):
        self.assertEqual([], self.errors())

    def test_each_required_property_is_enforced(self):
        for name in (*gate.TRUE_PROPERTIES, *gate.FALSE_PROPERTIES, *gate.EXACT_PROPERTIES, 'MSBuildVersion', 'NoWarn'):
            with self.subTest(property=name):
                value = self.properties.pop(name)
                self.assertIn('property:' + name, self.errors())
                self.properties[name] = value

    def reference_fixture(self):
        props = {name: 'true' for name in ('IsAotCompatible', 'EnableTrimAnalyzer', 'EnableAotAnalyzer', 'TreatWarningsAsErrors')}
        props.update(NoWarn='', RestorePackagesWithLockFile='', RestoreLockedMode='',
                     SuppressTrimAnalysisWarnings='false', TrimmerSingleWarn='false')
        return {name: {phase: dict(props) for phase in ('Authored', 'BuildContext')}
                for name in ('Core', 'Presentation')}

    def test_reference_authored_and_actual_context_checked(self):
        self.assertEqual([], gate.validate_references(self.reference_fixture()))
        for phase, key, value in (('Authored', 'NoWarn', 'IL3050'),
                                  ('Authored', 'IsAotCompatible', 'false'),
                                  ('Authored', 'RestoreLockedMode', 'true'),
                                  ('BuildContext', 'RestorePackagesWithLockFile', 'true'),
                                  ('BuildContext', 'TreatWarningsAsErrors', 'false')):
            document = self.reference_fixture()
            document['Core'][phase][key] = value
            self.assertIn('references:Core:' + phase + ':' + key, gate.validate_references(document))
        self.assertTrue(gate.validate_references({}))
        self.assertTrue(gate.validate_references({'Core': None, 'Presentation': {}}))

    def test_cli_wrapped_json_and_private_invalid_input(self):
        properties = self.root / 'properties.json'
        references = self.root / 'references.json'
        log = self.root / 'build.log'
        properties.write_text(json.dumps({'Properties': self.properties}))
        references.write_text(json.dumps(self.reference_fixture()))
        log.write_text(self.log)
        command = [sys.executable, '-B', str(SCRIPT), '--properties', str(properties),
                   '--reference-properties', str(references), '--log', str(log),
                   '--artifact-dir', str(self.root), '--started-utc-ms', str(self.started)]
        result = subprocess.run(command, capture_output=True, text=True, timeout=15)
        self.assertEqual(0, result.returncode, result.stderr)
        self.assertEqual('BUILD_CHECKS_ONLY', json.loads(result.stdout)['result'])
        self.assertEqual('NOT RUN', json.loads(result.stdout)['nativeAcceptance'])
        self.assertEqual('', result.stderr)
        properties.write_text('PRIVATE-PAYLOAD ' + str(self.root))
        result = subprocess.run(command, capture_output=True, text=True, timeout=15)
        self.assertEqual(1, result.returncode)
        self.assertEqual('REJECTED', json.loads(result.stdout)['result'])
        self.assertNotIn('PRIVATE-PAYLOAD', result.stdout + result.stderr)
        self.assertNotIn(str(self.root), result.stdout + result.stderr)
        self.assertEqual('', result.stderr)

    def test_link_rejection_precedes_binary_reads(self):
        with patch.object(gate, 'is_link', side_effect=lambda path: path.name == gate.ENTRY):
            with patch.object(Path, 'read_bytes', side_effect=AssertionError('must not read link')):
                self.assertIn('artifact:link', self.errors())

    def test_multi_architecture_restore_is_rejected(self):
        for value in ('win-x86;win-x64;win-arm64', 'win-arm64', ''):
            with self.subTest(value=value):
                self.properties['RuntimeIdentifiers'] = value
                self.assertIn('property:RuntimeIdentifiers', self.errors())

    def test_il_suppression_patterns_rejected(self):
        for value in ('IL2121', '1701;IL3050', '*', 'all', 'il2*', '1701,IL2026'):
            with self.subTest(value=value):
                self.properties['NoWarn'] = value
                self.assertIn('property:NoWarn', self.errors())

    def test_all_warning_and_error_families_rejected(self):
        for line in ('warning IL3050: planted', 'warning CS1234: planted', 'error CS0001: planted', 'IL2111: planted'):
            with self.subTest(line=line):
                old = self.log
                self.log += line
                self.assertIn('build:diagnostic', self.errors())
                self.log = old

    def test_no_native_compilation_is_not_accepted(self):
        self.log = self.log.replace('Generating native code', 'incremental link')
        self.assertIn('build:native-compiler-receipt-missing', self.errors())

    def test_failed_missing_or_duplicate_exit_not_accepted(self):
        for suffix in ('', 'candidate_publish_exit=1\n', 'candidate_publish_exit=0\ncandidate_publish_exit=0\n'):
            self.log = 'Generating native code\nSmartSnippets.Uno -> owned\n' + suffix
            self.assertIn('build:successful-exit-missing', self.errors())

    def test_missing_entry_or_sqlite_rejected(self):
        for name in (gate.ENTRY, 'e_sqlite3.dll'):
            path = self.root / name
            data = path.read_bytes()
            path.unlink()
            self.assertIn('artifact:missing-' + name, self.errors())
            path.write_bytes(data)

    def test_stale_entry_rejected(self):
        os.utime(self.root / gate.ENTRY, ns=(1_000, 1_000))
        self.assertIn('artifact:stale-entry', self.errors())

    def test_wrong_arch_entry_and_sqlite_rejected(self):
        for name in (gate.ENTRY, 'e_sqlite3.dll'):
            path = self.root / name
            original = path.read_bytes()
            path.write_bytes(pe(machine=0xAA64))
            self.assertIn('artifact:wrong-architecture', self.errors())
            path.write_bytes(original)

    def test_managed_entry_rejected(self):
        (self.root / gate.ENTRY).write_bytes(pe(managed=True))
        self.assertIn('artifact:managed-entry', self.errors())

    def test_native_jit_apphost_markers_rejected(self):
        for marker in gate.HOST_MARKERS:
            with self.subTest(marker=marker):
                (self.root / gate.ENTRY).write_bytes(pe() + marker)
                self.assertIn('artifact:jit-host', self.errors())

    def test_malformed_headers_fail_closed(self):
        for data in (b'', b'MZ', pe()[:200], b'\x7fELF' + bytes(512)):
            with self.subTest(size=len(data)):
                self.assertTrue(gate.inspect_pe(data))

    def test_managed_or_legacy_layout_rejected(self):
        for name in ('SmartSnippets.UnoCandidate.dll', 'SmartSnippets.exe', 'SmartSnippets.App.exe', 'SmartSnippets.App.dll', 'SmartSnippets.Uno.dll', 'SmartSnippets.Uno.exe', 'other.runtimeconfig.json', 'other.deps.json'):
            with self.subTest(name=name):
                path = self.root / name
                path.write_bytes(b'planted')
                self.assertIn('artifact:managed-or-legacy-layout', self.errors())
                path.unlink()

    def test_jit_runtime_payload_rejected(self):
        for name in gate.RUNTIME_NAMES:
            with self.subTest(name=name):
                path = self.root / name
                path.write_bytes(b'planted')
                self.assertIn('artifact:jit-runtime', self.errors())
                path.unlink()

    def test_bad_start_time_rejected(self):
        for value in (0, -1, None, '10000'):
            self.started = value
            self.assertIn('build:invalid-start-time', self.errors())

    def test_workflow_is_additive_and_never_launches_candidate(self):
        # Supplemental configuration audit, not a native-acceptance test.
        workflow = (SCRIPT.parents[1] / '.github/workflows/uno-windows-candidate.yml').read_text()
        self.assertIn('global-json-file: src/SmartSnippets.Uno/global.json', workflow)
        self.assertIn('working-directory: src/SmartSnippets.Uno', workflow)
        self.assertIn('./scripts/publish-aot.ps1', workflow)
        self.assertIn("'prototypes/uno-native-target/identity/**'", workflow)
        self.assertIn("'prototypes/uno-native-target/windows-insertion/**'", workflow)
        self.assertIn('dotnet test ../../scripts/uno_candidate_native_fixture/tests -c Release', workflow)
        for forbidden in ('run_uno_candidate_', '-authorizedscreenslot',
                          '--authorized-screen-slot', 'restore-harness-registry'):
            self.assertNotIn(forbidden, workflow.lower())
        self.assertNotIn('--authorized', workflow)
        self.assertNotIn('Start-Process', workflow)
        self.assertTrue((SCRIPT.parents[1] / '.github/workflows/ci.yml').is_file())


if __name__ == '__main__':
    unittest.main()
