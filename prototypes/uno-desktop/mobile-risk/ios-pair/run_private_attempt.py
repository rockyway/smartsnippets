#!/usr/bin/env python3
"""Mac-only CLI attempt: no launch, installation, signing or global SDK writes.
Run a copied script/source in OS temp, never in the shared Mac checkout.
"""
import hashlib
import json
import os
from pathlib import Path
import re
import shutil
import signal
import stat
import subprocess
import sys
import tempfile
import time

GIB = 1024 ** 3
GLOBAL_SDK = Path('/usr/local/share/dotnet')
# Stable Xcode26.6 release: dotnet/macios dotnet-10.0.1xx-xcode26.5-10318.
WORKLOAD_VERSION = '10.0.401'
IOS_SDK_VERSION = '26.5.10318'
UNO_VERSION = '6.7.135'
CLEANUP_NAMES = ('dotnet', 'nuget', 'home', 'cli', 'tmp')
PUBLISH_PROPERTIES = ['-p:Configuration=Release', '-p:RuntimeIdentifier=ios-arm64', '-p:_IsPublishing=true',
                      '-p:PublishAot=true', '-p:PublishTrimmed=true', '-p:IsAotCompatible=true',
                      '-p:SuppressTrimAnalysisWarnings=false', '-p:TrimmerSingleWarn=false',
                      '-p:TreatWarningsAsErrors=true', '-p:EnableCodeSigning=false',
                      '-p:CodesignRequireProvisioningProfile=false',
                      '-p:UseSharedCompilation=false', '-p:NoWarn=', '-nodeReuse:false']


def ios_install_command(dotnet, config, temp):
    return [dotnet, 'workload', 'install', 'ios', '--version', WORKLOAD_VERSION,
            '--configfile', str(config), '--source', 'https://api.nuget.org/v3/index.json',
            '--temp-dir', str(temp), '--disable-parallel']


def sdk_fingerprint(root):
    if root.is_symlink():
        raise RuntimeError('SDK root symlink: isolation not proven')
    digest = hashlib.sha256()
    for directory, dirs, files in os.walk(root):
        dirs.sort()
        for name in sorted(dirs + files):
            path = Path(directory) / name
            if path.is_symlink():
                raise RuntimeError('SDK symlink: isolation not proven')
            st = path.stat()
            digest.update(str((str(path.relative_to(root)), st.st_mode, st.st_uid,
                               st.st_gid, st.st_size, st.st_mtime_ns)).encode())
    return digest.hexdigest()


def workspace_size(root):
    # Apparent bytes deliberately count cloned SDK data against the growth cap.
    total = 0
    for path in root.rglob('*'):
        try:
            entry = path.lstat()
            if stat.S_ISREG(entry.st_mode):
                total += entry.st_size
        except FileNotFoundError:
            # Workload/restore temporary files can disappear during this sample.
            continue
    return total


def interrupted(signum, frame):
    raise SystemExit(130)


def stop_group(proc):
    for sig in (signal.SIGTERM, signal.SIGKILL):
        # Reap our exited direct child before a second signal: macOS can return EPERM
        # for its otherwise-empty zombie group. EPERM itself is never accepted.
        proc.poll()
        try:
            os.killpg(proc.pid, sig)
        except ProcessLookupError:
            break
        time.sleep(0.5)
    proc.wait()


def cleanup_owned_paths(root):
    if root.is_symlink() or root.stat().st_uid != os.getuid():
        raise RuntimeError('Cleanup root ownership/isolation failure')
    results = {}

    def writable_retry(function, path, exc_info):
        os.chmod(path, stat.S_IRUSR | stat.S_IWUSR | stat.S_IXUSR,
                 follow_symlinks=False)
        function(path)

    for name in CLEANUP_NAMES:
        path = root / name
        record = {}
        try:
            if path.is_symlink():
                path.unlink()  # Only the owned link, never its target.
            elif path.exists():
                os.chmod(path, stat.S_IRUSR | stat.S_IWUSR | stat.S_IXUSR)
                # SDK/package subdirectories may be read-only; make only owned directories traversable.
                for directory, dirs, files in os.walk(path, followlinks=False):
                    for child in dirs:
                        child_path = Path(directory) / child
                        if not child_path.is_symlink():
                            os.chmod(child_path, stat.S_IRUSR | stat.S_IWUSR | stat.S_IXUSR)
                shutil.rmtree(path, onerror=writable_retry)
        except OSError as exc:
            record['errorClass'] = type(exc).__name__
        record['removed'] = not os.path.lexists(path)
        results[name] = record
    return results


def report_exit_code(report):
    if report.get('stopReason') == '130':
        return 130
    return 1 if (report.get('stopReason') or not report.get('globalSdkUnchanged')
                 or not report.get('privateSdkAndCachesRemoved')) else 0


def assert_strict_properties(properties):
    expected = {'PublishAot': 'true', 'PublishTrimmed': 'true', 'IsAotCompatible': 'true',
                'SuppressTrimAnalysisWarnings': 'false', 'TrimmerSingleWarn': 'false',
                'TreatWarningsAsErrors': 'true', 'EnableCodeSigning': 'false',
                'CodesignRequireProvisioningProfile': 'false', 'RuntimeIdentifier': 'ios-arm64'}
    for name, value in expected.items():
        if properties.get(name, '').lower() != value:
            raise RuntimeError('Strict property mismatch: ' + name)
    warnings = {p.strip().upper() for p in re.split('[;, ]', properties.get('NoWarn', '')) if p.strip()}
    if warnings - {'1701', '1702'}:
        raise RuntimeError('Unexpected warning suppression')


def assert_skia_selection(properties, assets):
    if (properties.get('UnoUIRuntimeIdentifier') != 'Skia'
            or properties.get('UnoVersion') != UNO_VERSION
            or ';skiarenderer;' not in properties.get('UnoFeatures', '').lower()
            or 'HAS_UNO_SKIA_APPLE_UIKIT' not in properties.get('DefineConstants', '').split(';')):
        raise RuntimeError('Skia effective property mismatch')
    package = 'Uno.WinUI.Runtime.Skia.AppleUIKit/' + UNO_VERSION
    if package not in assets['libraries']:
        raise RuntimeError('Pinned AppleUIKit Skia runtime absent from restored assets')
    return {'runtimePackage': package}


def classify_skia_consumer_inputs(linker_inputs, ilc_inputs, pinned_portable):
    source_inputs = [p for p in linker_inputs if p.name == 'Uno.UI.dll']
    consumer_inputs = [p for p in ilc_inputs if p.name == 'Uno.UI.dll']
    if (len(source_inputs) != 1 or len(consumer_inputs) != 1
            or source_inputs[0].resolve() != pinned_portable.resolve()
            or not pinned_portable.is_file() or not consumer_inputs[0].is_file()):
        raise RuntimeError('Portable Skia implementation lineage to ILC unverified')
    return {'portableImplementationSha256': hashlib.sha256(pinned_portable.read_bytes()).hexdigest(),
            'ilcLinkedImplementationSha256': hashlib.sha256(consumer_inputs[0].read_bytes()).hexdigest(),
            'implementation': 'Uno.WinUI/' + UNO_VERSION + '/uno-runtime/net10.0/skia/Uno.UI.dll'}


def assert_native_aot_evidence(publish_log, inspection, payload_names, has_ilc_response):
    combined = (inspection + '\n' + '\n'.join(payload_names)).lower()
    for forbidden in ('mono.framework', 'libmonosgen', 'mono_jit_init', 'libcoreclr', 'coreclr.dll'):
        if forbidden in combined:
            raise RuntimeError('Non-NativeAOT payload/symbol: ' + forbidden)
    if (not has_ilc_response or 'Generating native code' not in publish_log
            or not re.search(r'ilcompiler|[/\\]ilc(?:"|\s)', publish_log, re.IGNORECASE)):
        raise RuntimeError('NativeAOT ILC execution not verified')


def main():
    if sys.platform != 'darwin':
        raise SystemExit('Mac-only runner; NOT RUN')
    os.umask(0o077)
    for signum in (signal.SIGTERM, signal.SIGHUP):
        signal.signal(signum, interrupted)
    if shutil.disk_usage('/private/tmp').free < 18 * GIB:
        raise SystemExit('Disk preflight below 18GiB; NOT RUN')
    before = sdk_fingerprint(GLOBAL_SDK)
    root = Path(tempfile.mkdtemp(prefix='ss-mobile-', dir='/private/tmp'))
    source = root / 'source'
    report = {'workspaceToken': root.name, 'stages': [], 'minimumFreeBytes': shutil.disk_usage(root).free,
              'maximumWorkspaceBytes': 0, 'globalSdkBefore': before}
    started = time.monotonic()
    env = {
        'HOME': str(root / 'home'), 'TMPDIR': str(root / 'tmp') + '/',
        'PATH': str(root / 'dotnet') + ':/usr/bin:/bin:/usr/sbin:/sbin',
        'DOTNET_ROOT': str(root / 'dotnet'), 'DOTNET_HOST_PATH': str(root / 'dotnet/dotnet'),
        'DOTNET_CLI_HOME': str(root / 'cli'), 'NUGET_PACKAGES': str(root / 'nuget'),
        'NUGET_HTTP_CACHE_PATH': str(root / 'home/http-cache'),
        'NUGET_PLUGINS_CACHE_PATH': str(root / 'home/plugin-cache'),
        'DOTNET_MULTILEVEL_LOOKUP': '0', 'DOTNET_CLI_TELEMETRY_OPTOUT': '1',
        'DOTNET_SKIP_FIRST_TIME_EXPERIENCE': '1', 'DOTNET_CLI_WORKLOAD_UPDATE_NOTIFY_DISABLE': 'true',
        'DOTNET_NOLOGO': '1', 'LANG': 'en_US.UTF-8',
        'DEVELOPER_DIR': '/Applications/Xcode.app/Contents/Developer',
        'MSBUILDDISABLENODEREUSE': '1',
    }

    def run(stage, args, limit, cwd=None, allow_failure=False):
        log = root / (stage + '.log')
        with log.open('wb') as output:
            proc = subprocess.Popen(args, cwd=cwd or source, env=env, stdout=output,
                                    stderr=subprocess.STDOUT, start_new_session=True)
            deadline = min(started + 1200, time.monotonic() + limit)
            reason = None
            try:
                while proc.poll() is None:
                    free = shutil.disk_usage(root).free
                    size = workspace_size(root)
                    report['minimumFreeBytes'] = min(report['minimumFreeBytes'], free)
                    report['maximumWorkspaceBytes'] = max(report['maximumWorkspaceBytes'], size)
                    if free < 10 * GIB:
                        reason = 'disk-floor-10GiB'
                    elif size > 8 * GIB:
                        reason = 'workspace-growth-cap-8GiB'
                    elif time.monotonic() > deadline:
                        reason = 'time-limit'
                    if reason:
                        break
                    time.sleep(2)
            finally:
                stop_group(proc)
                report['stages'].append({'stage': stage, 'command': args,
                                         'exitCode': proc.returncode, 'limit': reason})
        (root / 'report.json').write_text(json.dumps(report, indent=2))
        if reason or proc.returncode:
            failure = stage + ': ' + (reason or 'command-failed')
            if reason or not allow_failure:
                raise RuntimeError(failure)
            report['stopReason'] = failure
        return log.read_text(errors='replace').strip()

    try:
        shutil.copytree(Path(__file__).resolve().parent, source,
                        ignore=shutil.ignore_patterns('bin', 'obj', 'native', '__pycache__'))
        report['sourceHashes'] = {str(p.relative_to(source)): hashlib.sha256(p.read_bytes()).hexdigest()
                                  for p in sorted(source.rglob('*')) if p.is_file()}
        for name in ('home', 'tmp', 'nuget', 'cli'):
            (root / name).mkdir(mode=0o700)
        config = root / 'NuGet.Config'
        config.write_text('''<configuration><packageSources><clear/>
<add key="nuget.org" value="https://api.nuget.org/v3/index.json"/>
</packageSources><fallbackPackageFolders><clear/></fallbackPackageFolders></configuration>''')
        run('clone-sdk', ['/bin/cp', '-cR', str(GLOBAL_SDK), str(root / 'dotnet')], 120)
        private_sdk = root / 'dotnet'
        if private_sdk.is_symlink():
            raise RuntimeError('Private SDK root symlink: isolation failure')
        for path in [private_sdk, *private_sdk.rglob('*')]:
            if path.is_symlink() or path.stat().st_uid != os.getuid():
                raise RuntimeError('Private SDK ownership/isolation failure')
            if path.is_file():
                original = GLOBAL_SDK / path.relative_to(private_sdk)
                if path.stat().st_ino == original.stat().st_ino:
                    raise RuntimeError('Hardlinked SDK: isolation failure')
        dotnet = str(private_sdk / 'dotnet')
        if run('private-version', [dotnet, '--version'], 20) != '10.0.403':
            raise RuntimeError('Private SDK version mismatch')
        run('swift-keyboard', ['/bin/bash', 'build_keyboard.sh'], 180)
        run('swift-inspect', ['/bin/bash', '-c', '''set -euo pipefail
b=native/PairKeyboard.appex/PairKeyboard
file "$b"
xcrun otool -hv "$b"
xcrun otool -L "$b"
xcrun otool -l "$b"
xcrun nm -u "$b"
plutil -lint native/PairKeyboard.appex/Info.plist Keyboard/Entitlements.plist Platforms/iOS/Info.plist
shasum -a 256 "$b"
'''], 30)
        # --version is disallowed under global.json's workloadVersion pin.
        run('install-ios', ios_install_command(dotnet, config, root / 'tmp'), 600, cwd=root)
        installed = run('workload-list', [dotnet, 'workload', 'list'], 30)
        selected_set = run('workload-version', [dotnet, 'workload', '--version'], 30)
        if selected_set != WORKLOAD_VERSION or IOS_SDK_VERSION not in installed:
            raise RuntimeError('Pinned workload set/iOS SDK mismatch')
        run('restore-skia', [dotnet, 'restore', 'IosPair.csproj', '--configfile', str(config),
                             *PUBLISH_PROPERTIES], 180)
        props_text = run('effective-properties', [dotnet, 'msbuild', 'IosPair.csproj', *PUBLISH_PROPERTIES,
            '-getProperty:PublishAot,PublishTrimmed,IsAotCompatible,NoWarn,SuppressTrimAnalysisWarnings,TrimmerSingleWarn,TreatWarningsAsErrors,EnableCodeSigning,CodesignRequireProvisioningProfile,RuntimeIdentifier,UnoFeatures,UnoVersion,UnoUIRuntimeIdentifier,DefineConstants'], 30)
        properties = json.loads(props_text)['Properties']
        assert_strict_properties(properties)
        assets = json.loads((source / 'obj/project.assets.json').read_text())
        report['skiaSelection'] = assert_skia_selection(properties, assets)
        publish_log = run('publish-pair', [dotnet, 'publish', 'IosPair.csproj',
            '-v:normal', '--configfile', str(config), *PUBLISH_PROPERTIES], 1000, allow_failure=True)
        manifests = list((source / 'obj').rglob('skia-linker-inputs.txt'))
        responses = list((source / 'obj').rglob('*.ilc.rsp'))
        if len(manifests) != 1 or len(responses) != 1:
            raise RuntimeError('Authoritative linker/ILC inputs absent or ambiguous')
        linker_inputs = [Path(line.strip()) for line in manifests[0].read_text().splitlines() if line.strip()]
        ilc_inputs = []
        for line in responses[0].read_text().splitlines():
            if line.startswith('-r:'):
                path = Path(line[3:].strip('"'))
                ilc_inputs.append(path if path.is_absolute() else source / path)
        report['skiaConsumerInputs'] = classify_skia_consumer_inputs(linker_inputs, ilc_inputs,
            root / 'nuget/uno.winui' / UNO_VERSION / 'uno-runtime/net10.0/skia/Uno.UI.dll')
        if report.get('stopReason'):
            raise RuntimeError('Publish failed; consumer-input evidence retained without artifact claim')
        import plistlib
        pairs = [p for p in (source / 'bin').rglob('*.app')
                 if (p / 'PlugIns/PairKeyboard.appex/PairKeyboard').is_file()]
        if not pairs:
            raise RuntimeError('No app-plus-keyboard artifact pair found')
        for index, app in enumerate(pairs):
            info = plistlib.loads((app / 'Info.plist').read_bytes())
            executable = app / info['CFBundleExecutable']
            keyboard = app / 'PlugIns/PairKeyboard.appex/PairKeyboard'
            inspection = ''
            for label, binary in (('app', executable), ('keyboard', keyboard)):
                output = run(f'inspect-{index}-{label}', ['/usr/bin/file', str(binary)], 20)
                if 'arm64' not in output or 'executable' not in output:
                    raise RuntimeError('Paired artifact is not an ARM64 executable')
                for kind, args in (('headers', ['otool', '-hv']), ('dependencies', ['otool', '-L']),
                                   ('load-commands', ['otool', '-l']), ('symbols', ['nm'])):
                    inspection += run(f'{kind}-{index}-{label}', ['/usr/bin/xcrun', *args, str(binary)], 30)
                run(f'plist-{index}-{label}', ['/usr/bin/plutil', '-lint', str(binary.parent / 'Info.plist')], 20)
                report.setdefault('artifactHashes', {})[f'{index}-{label}'] = hashlib.sha256(binary.read_bytes()).hexdigest()
            assert_native_aot_evidence(publish_log, inspection,
                [str(p.relative_to(app)) for p in app.rglob('*')],
                any((source / 'obj').rglob('*.ilc.rsp')))
            if keyboard.read_bytes() != (source / 'native/PairKeyboard.appex/PairKeyboard').read_bytes():
                raise RuntimeError('Embedded keyboard differs from native fixture')
        report['artifactInspection'] = 'BUILD ARTIFACTS ONLY; runtime, deployment and memory NOT RUN'
    except (Exception, SystemExit) as exc:
        if report.get('stopReason'):
            report['evidenceNote'] = str(exc) or type(exc).__name__
        else:
            report['stopReason'] = str(exc) or type(exc).__name__
    finally:
        # Do not allow repeated disconnect signals to interrupt owned cleanup.
        for signum in (signal.SIGTERM, signal.SIGHUP):
            signal.signal(signum, signal.SIG_IGN)
        try:
            report['globalSdkAfter'] = sdk_fingerprint(GLOBAL_SDK)
            report['globalSdkUnchanged'] = report['globalSdkAfter'] == before
        except Exception as exc:
            report['globalSdkUnchanged'] = False
            report['fingerprintErrorClass'] = type(exc).__name__
        try:
            report['cleanupPaths'] = cleanup_owned_paths(root)
        except Exception as exc:
            report['cleanupErrorClass'] = type(exc).__name__
        report['privateSdkAndCachesRemoved'] = all(not os.path.lexists(root / n) for n in CLEANUP_NAMES)
        report['elapsedSeconds'] = round(time.monotonic() - started, 1)
        report['remainingFreeBytes'] = shutil.disk_usage(root).free
        (root / 'report.json').write_text(json.dumps(report, indent=2))
        print(json.dumps(report, indent=2))
        print('PRIVATE_EVIDENCE=' + str(root))
    return report_exit_code(report)


if __name__ == '__main__':
    sys.exit(main())
