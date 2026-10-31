import importlib.util
import json
import os
import stat
import subprocess
import sys
from unittest.mock import Mock, patch
from pathlib import Path
import tempfile
import unittest
import xml.etree.ElementTree as ET

SOURCE = Path(__file__).resolve().parent
spec = importlib.util.spec_from_file_location('private_attempt', SOURCE / 'run_private_attempt.py')
runner = importlib.util.module_from_spec(spec)
spec.loader.exec_module(runner)


class PrivateAttemptTests(unittest.TestCase):
    def test_sdk_fingerprint_detects_change_and_rejects_symlinks(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            (root / 'sdk').write_text('10.0.403')
            before = runner.sdk_fingerprint(root)
            (root / 'sdk').write_text('different version')
            self.assertNotEqual(before, runner.sdk_fingerprint(root))
            try:
                (root / 'link').symlink_to(root / 'sdk')
            except OSError:
                self.skipTest('Symlink creation unavailable on this host')
            with self.assertRaisesRegex(RuntimeError, 'isolation not proven'):
                runner.sdk_fingerprint(root)

    def test_workspace_size_counts_apparent_bytes(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            (root / 'sdk').write_bytes(b'a' * 1024)
            (root / 'cache').mkdir()
            (root / 'cache/package').write_bytes(b'b' * 2048)
            self.assertEqual(3072, runner.workspace_size(root))

    def test_stable_xcode26_workload_set_is_pinned_for_install_and_build(self):
        sdk = json.loads((SOURCE / 'global.json').read_text())['sdk']
        self.assertEqual('10.0.403', sdk['version'])
        self.assertEqual('disable', sdk['rollForward'])
        self.assertEqual('10.0.401', sdk['workloadVersion'])
        command = runner.ios_install_command('private-dotnet', 'config', 'tmp')
        self.assertEqual(['private-dotnet', 'workload', 'install', 'ios'], command[:4])
        self.assertEqual('10.0.401', command[command.index('--version') + 1])
        self.assertNotIn('--skip-manifest-update', command)
        self.assertEqual('26.5.10318', runner.IOS_SDK_VERSION)
        self.assertNotIn('android', command)

    def test_workspace_size_tolerates_disappearing_files(self):
        root, missing, present = Mock(), Mock(), Mock()
        root.rglob.return_value = [missing, present]
        missing.lstat.side_effect = FileNotFoundError
        present.lstat.return_value = Mock(st_mode=stat.S_IFREG, st_size=123)
        self.assertEqual(123, runner.workspace_size(root))

    def test_root_symlink_is_rejected(self):
        root = Mock()
        root.is_symlink.return_value = True
        with self.assertRaisesRegex(RuntimeError, 'root symlink'):
            runner.sdk_fingerprint(root)

    def test_cleanup_missing_and_readonly_paths_is_idempotent(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            nested = root / 'nuget/readonly'
            nested.mkdir(parents=True)
            (nested / 'package').write_text('owned')
            nested.chmod(0o500)
            with patch.object(runner.os, 'getuid', return_value=root.stat().st_uid, create=True):
                first = runner.cleanup_owned_paths(root)
                second = runner.cleanup_owned_paths(root)
            self.assertTrue(all(x['removed'] for x in first.values()))
            self.assertTrue(all(x['removed'] for x in second.values()))

    def test_cleanup_failure_is_truthful_and_does_not_skip_other_paths(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            (root / 'dotnet').mkdir()
            with patch.object(runner.os, 'getuid', return_value=root.stat().st_uid, create=True), \
                    patch.object(runner.shutil, 'rmtree', side_effect=PermissionError):
                result = runner.cleanup_owned_paths(root)
            self.assertFalse(result['dotnet']['removed'])
            self.assertEqual('PermissionError', result['dotnet']['errorClass'])
            self.assertTrue(result['tmp']['removed'])

    def test_failure_changed_sdk_or_incomplete_cleanup_is_nonzero(self):
        success = {'globalSdkUnchanged': True, 'privateSdkAndCachesRemoved': True}
        self.assertEqual(0, runner.report_exit_code(success))
        for failure in ({'stopReason': 'publish failed'}, {'globalSdkUnchanged': False},
                        {'privateSdkAndCachesRemoved': False}):
            self.assertNotEqual(0, runner.report_exit_code(success | failure))
        self.assertEqual(130, runner.report_exit_code(success | {'stopReason': '130'}))

    @unittest.skipUnless(os.name == 'posix', 'Owned process groups require POSIX')
    def test_disconnect_signals_run_owned_group_finally(self):
        for name in ('SIGTERM', 'SIGHUP'):
            script = '''import json, os, pathlib, select, signal, subprocess, sys
sys.path.insert(0, sys.argv[1])
import run_private_attempt as r
sig = getattr(signal, sys.argv[2])
signal.signal(sig, r.interrupted)
child = 'import json,os,time;print(json.dumps(dict(pid=os.getpid(),pgid=os.getpgrp(),uid=os.getuid())),flush=True);time.sleep(8)'
p = subprocess.Popen([str(pathlib.Path(sys.executable).resolve()), '-c', child],
                     stdout=subprocess.PIPE, text=True, start_new_session=True)
try:
    if not select.select([p.stdout], [], [], 3)[0]:
        raise RuntimeError('Owned child readiness timeout')
    receipt = json.loads(p.stdout.readline())
    if receipt != dict(pid=p.pid, pgid=p.pid, uid=os.getuid()) or p.poll() is not None:
        raise RuntimeError('Owned child identity mismatch')
    os.kill(os.getpid(), sig)
finally:
    try:
        r.stop_group(p)
        print('GROUP_STOPPED', p.returncode, flush=True)
    finally:
        if p.poll() is None:
            p.kill()  # Direct-child test handle only, never a workload fallback.
            p.wait(timeout=2)
'''
            result = subprocess.run([sys.executable, '-c', script, str(SOURCE), name],
                                    capture_output=True, text=True, timeout=10)
            self.assertEqual(130, result.returncode)
            self.assertIn('GROUP_STOPPED', result.stdout)

    def test_strict_properties_reject_aot_trim_warning_and_rid_bypasses(self):
        strict = {'PublishAot': 'true', 'PublishTrimmed': 'true', 'IsAotCompatible': 'true',
                  'SuppressTrimAnalysisWarnings': 'false', 'TrimmerSingleWarn': 'false',
                  'TreatWarningsAsErrors': 'true', 'EnableCodeSigning': 'false',
                  'CodesignRequireProvisioningProfile': 'false', 'RuntimeIdentifier': 'ios-arm64',
                  'NoWarn': '1701;1702'}
        runner.assert_strict_properties(strict)
        for key in strict:
            invalid = strict | {key: 'IL3050' if key == 'NoWarn' else 'invalid'}
            with self.assertRaises(RuntimeError, msg=key):
                runner.assert_strict_properties(invalid)
        self.assertIn('-p:RuntimeIdentifier=ios-arm64', runner.PUBLISH_PROPERTIES)
        self.assertIn('-p:NoWarn=', runner.PUBLISH_PROPERTIES)
        self.assertIn('-p:_IsPublishing=true', runner.PUBLISH_PROPERTIES)
        for flag in ('PublishAot=true', 'PublishTrimmed=true', 'IsAotCompatible=true',
                     'SuppressTrimAnalysisWarnings=false', 'TrimmerSingleWarn=false', 'TreatWarningsAsErrors=true'):
            self.assertIn('-p:' + flag, runner.PUBLISH_PROPERTIES)
        with self.assertRaisesRegex(RuntimeError, 'warning suppression'):
            runner.assert_strict_properties(strict | {'NoWarn': '1701;1702;IL2121'})

    def test_skia_gate_requires_effective_runtime_package_and_portable_reference(self):
        props = {'UnoUIRuntimeIdentifier': 'Skia', 'UnoVersion': '6.7.135',
                 'UnoFeatures': ';skiarenderer;', 'DefineConstants': 'HAS_UNO_SKIA_APPLE_UIKIT'}
        assets = {'libraries': {'Uno.WinUI.Runtime.Skia.AppleUIKit/6.7.135': {}}}
        runner.assert_skia_selection(props, assets)
        for key in props:
            with self.assertRaises(RuntimeError, msg=key):
                runner.assert_skia_selection(props | {key: ''}, assets)
        with self.assertRaises(RuntimeError):
            runner.assert_skia_selection(props, {'libraries': {}})

    def test_consumer_gate_rejects_native_facade_wrong_runtime_and_absent_ilc_input(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            portable = root / 'uno-runtime/net10.0/skia/Uno.UI.dll'
            native = root / 'lib/net10.0-ios/Uno.UI.dll'
            linked = root / 'obj/linked/Uno.UI.dll'
            for path, content in ((portable, b'portable'), (native, b'native'), (linked, b'linked-portable')):
                path.parent.mkdir(parents=True)
                path.write_bytes(content)
            result = runner.classify_skia_consumer_inputs([portable], [linked], portable)
            self.assertIn('ilcLinkedImplementationSha256', result)
            for sources, consumers in (([native], [linked]), ([portable], []),
                                       ([portable, native], [linked]), ([], [linked])):
                with self.assertRaises(RuntimeError):
                    runner.classify_skia_consumer_inputs(sources, consumers, portable)

    def test_native_aot_gate_rejects_mono_and_absent_ilc_evidence(self):
        log = 'Generating native code /microsoft.dotnet.ilcompiler/10.0.14/tools/ilc '
        runner.assert_native_aot_evidence(log, 'native', ['app'], True)
        for forbidden in ('Mono.framework', 'libmonosgen-2.0.dylib', 'mono_jit_init', 'libcoreclr.dylib'):
            with self.assertRaises(RuntimeError, msg=forbidden):
                runner.assert_native_aot_evidence(log, forbidden, [], True)
            with self.assertRaises(RuntimeError, msg=forbidden):
                runner.assert_native_aot_evidence(log, '', [forbidden], True)
        for evidence, response in (('', True), (log, False)):
            with self.assertRaises(RuntimeError):
                runner.assert_native_aot_evidence(evidence, '', [], response)

    def test_pair_is_uno_aot_and_embeds_keyboard_without_signing(self):
        project = ET.parse(SOURCE / 'IosPair.csproj').getroot()
        self.assertEqual('Uno.Sdk/6.7.30', project.attrib['Sdk'])
        self.assertEqual('false', project.findtext('.//EnableCodeSigning'))
        self.assertEqual('SkiaRenderer;', project.findtext('.//UnoFeatures'))
        entrypoint = (SOURCE / 'Platforms/iOS/EntryPoint.cs').read_text()
        for call in ('UnoPlatformHostBuilder.Create()', '.App(() => new App())', '.UseAppleUIKit()', '.Build()', '.Run()'):
            self.assertIn(call, entrypoint)
        self.assertNotIn('UIApplication.Main', entrypoint)
        self.assertEqual('PairKeyboard', project.findtext('.//AdditionalAppExtensions/Name'))
        self.assertIsNotNone(project.find('.//Target/Exec'))
        props = ET.parse(SOURCE / 'Directory.Build.props').getroot()
        self.assertEqual('true', props.findtext('.//PublishAot'))
        self.assertEqual('false', props.findtext('.//SuppressTrimAnalysisWarnings'))
        self.assertEqual('true', props.findtext('.//TreatWarningsAsErrors'))
        swift = (SOURCE / 'build_keyboard.sh').read_text()
        self.assertIn('-emit-executable', swift)
        self.assertIn('_NSExtensionMain', swift)
        self.assertNotIn('-emit-library', swift)


if __name__ == '__main__':
    unittest.main()
