"""Review-closed clipboard-control bootstrap. NEVER wrap coordinator/guardian in a kill Job."""
import argparse
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import re
import subprocess
import sys
import time
import uuid

REPO = Path(__file__).resolve().parents[1]
NATIVE_SCENES_SOURCE_REVIEWED = False
ENTRY = 'SmartSnippets.UnoCandidate.exe'
ROOT = REPO / 'scripts/uno_candidate_native_fixture'
spec = importlib.util.spec_from_file_location('native_nonmutating_helpers', REPO / 'scripts/run_uno_candidate_acceptance.py')
helpers = importlib.util.module_from_spec(spec)
spec.loader.exec_module(helpers)


def source_fingerprint():
    paths = []
    for relative in ('src/SmartSnippets.Core', 'src/SmartSnippets.Presentation', 'src/SmartSnippets.Uno',
                     'scripts/uno_candidate_native_fixture', 'prototypes/uno-native-target/identity',
                     'prototypes/uno-native-target/windows-insertion', 'prototypes/uno-native-target/windows',
                     'prototypes/uno-candidate-native'):
        paths.extend(p for p in (REPO / relative).rglob('*') if p.is_file()
                     and not {'bin', 'obj', '__pycache__'}.intersection(p.relative_to(REPO / relative).parts))
    paths.extend(REPO / name for name in ('Directory.Build.props', 'Directory.Packages.props', 'scripts/run_uno_candidate_native.py'))
    result = hashlib.sha256()
    for path in sorted(paths):
        data = helpers.owned_bytes(path, REPO)
        result.update(path.relative_to(REPO).as_posix().encode() + b'\0')
        result.update(hashlib.sha256(data).digest())
    return result.hexdigest()


def validate_image(path, digest, basename):
    path = Path(os.path.abspath(path))
    if path.name != basename or not re.fullmatch('[0-9a-fA-F]{64}', digest):
        raise ValueError('native_image_identity')
    if basename == ENTRY:
        if path.parent.parent != ROOT / 'bin/publish-native' or not re.fullmatch('[0-9a-f]{32}', path.parent.name):
            raise ValueError('native_build_provenance')
    elif basename in ('SmartSnippets.Target018.exe', 'SmartSnippets.UnoCandidate.Guardian.exe'):
        expected_root = REPO / ('prototypes/uno-native-target/windows/bin' if basename == 'SmartSnippets.Target018.exe'
                                else 'prototypes/uno-candidate-native/bin')
        if not path.is_relative_to(expected_root):
            raise ValueError('native_image_provenance')
    else:
        raise ValueError('native_image_identity')
    data = helpers.owned_bytes(path, path.parent, 134217728)
    pe = helpers.startup.repo_module('native_pe', 'prototypes/uno-desktop/verify_evidence.py')
    if hashlib.sha256(data).hexdigest() != digest.lower() or pe.inspect_executable(data, 'win-x64'):
        raise ValueError('native_image_hash')
    return path


def validate_build(path, digest):
    path = validate_image(path, digest, ENTRY)
    if path.parent.parent != ROOT / 'bin/publish-native' or not re.fullmatch('[0-9a-f]{32}', path.parent.name):
        raise ValueError('native_build_provenance')
    def read(name):
        return helpers.owned_bytes(path.parent / name, path.parent).decode('utf-8-sig')
    if read('fixture-source.sha256').strip() != source_fingerprint():
        raise ValueError('native_source_drift')
    gate = helpers.startup.repo_module('native_build_gate', 'scripts/uno_candidate_gate.py')
    gate.EXACT_PROPERTIES['RuntimeIdentifiers'] = 'win-x64'
    properties = json.loads(read('fixture-properties.json'))
    log = read('fixture-build.log').replace('SmartSnippets.Uno.NativeFixture ->', 'SmartSnippets.UnoCandidate ->')
    errors = gate.validate(properties.get('Properties', properties), log, path.parent,
                           json.loads(read('fixture-build.json'))['startedUtcMs'])
    errors.extend(gate.validate_references(json.loads(read('fixture-references.json'))))
    if errors:
        raise ValueError('native_build_gate')
    return path


def require_authorization(authorized):
    if not NATIVE_SCENES_SOURCE_REVIEWED or not authorized or sys.platform != 'win32':
        raise ValueError('native_source_review_and_slot_required')


def monitor(process, desktop, foreground, output, clock=time.monotonic, sleep=time.sleep):
    """No timeout kill: coordinator retains independent RAM recovery on a pending scene."""
    failed = False
    while process.poll() is None:
        try:
            desktop.validate_input_desktop()
            failed |= desktop.user.GetForegroundWindow() != foreground
            failed |= any(p.stat().st_size > 65536 for p in output)
        except Exception:
            failed = True
        sleep(.05)
    if failed or process.returncode:
        raise ValueError('native_scene_failed')


def verify_result(private, data, expected):
    if data[0].splitlines() != [b'native_phase=guardian_ready', b'native_phase=published', expected] or data[1]:
        raise ValueError('native_result_receipt')
    log = helpers.owned_bytes(private / 'native-fixture-profile/logs/app.log', private)
    text = log.decode('utf-8-sig')
    if sum(line.endswith(' INFO candidate_started') for line in text.splitlines()) != 1 or ' ERROR ' in text:
        raise ValueError('native_host_readiness_cleanup')
    privacy = helpers.startup.repo_module('native_privacy', 'scripts/uno_candidate_privacy.py')
    canaries = ('AΩ中🙂e\u0301\r\nsecond line', 'AΩ中🙂e\u0301\nsecond line', 'second line', str(private), str(private.parent))
    privacy.verify_negative_controls(canaries)
    if privacy.scan_evidence([log, *data], canaries)['privacy'] != 'CLEAN':
        raise ValueError('native_privacy')


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--capture-source', type=Path)
    parser.add_argument('--validate-build', nargs=2, metavar=('ENTRY', 'SHA256'))
    parser.add_argument('--executable', type=Path)
    parser.add_argument('--sha256')
    parser.add_argument('--target', type=Path)
    parser.add_argument('--target-sha256')
    parser.add_argument('--guardian', type=Path)
    parser.add_argument('--guardian-sha256')
    parser.add_argument('--authorized-screen-slot', action='store_true')
    parser.add_argument('--scene', choices=('restore', 'abort'))
    args = parser.parse_args(argv)
    if args.capture_source:
        args.capture_source.write_text(source_fingerprint() + '\n', encoding='ascii')
        return
    if args.validate_build:
        validate_build(*args.validate_build)
        print('BUILD_CHECKS_ONLY')
        return
    require_authorization(args.authorized_screen_slot)  # Before artifact inspection or process/private-root creation.
    if not all((args.executable, args.sha256, args.target, args.target_sha256, args.guardian, args.guardian_sha256, args.scene)):
        raise ValueError('native_arguments')
    entry = validate_build(args.executable, args.sha256)
    target = validate_image(args.target, args.target_sha256, 'SmartSnippets.Target018.exe')
    guardian = validate_image(args.guardian, args.guardian_sha256, 'SmartSnippets.UnoCandidate.Guardian.exe')
    ui = helpers.startup.repo_module('native_private_directory', 'prototypes/uno-desktop/run_ui_smoke.py')
    private = ui.private_directory() / ('ss-uno019-native-' + uuid.uuid4().hex)
    private.mkdir()  # Inherits current-user-only ACL from existing reviewed owner-private helper.
    desktop = helpers.startup.Desktop(ui.visible_windows)
    try:
        desktop.enable_dpi()
        desktop.validate_input_desktop()
        foreground = desktop.user.GetForegroundWindow()
        if not foreground:
            raise ValueError('native_foreground_unavailable')
        (private / 'native-launch-permit').write_bytes(b'owned_clipboard_controls_only\n')
        paths = [private / 'stdout.log', private / 'stderr.log']
        with paths[0].open('xb') as out, paths[1].open('xb') as err:
            # Deliberately no WindowsJob, communicate(timeout), process-tree termination or outer watchdog.
            process = subprocess.Popen([str(entry), '--coordinate', args.scene, str(target), args.target_sha256,
                                        str(guardian), args.guardian_sha256], cwd=private, stdin=subprocess.DEVNULL,
                                       stdout=out, stderr=err)
            monitor(process, desktop, foreground, paths)
        data = [helpers.owned_bytes(p, private, 65536) for p in paths]
        expected = ('native_result=regular_restored' if args.scene == 'restore' else 'native_result=abort_restored').encode()
        verify_result(private, data, expected)
        print(json.dumps({'result': 'clipboard_control_settled', 'scene': args.scene,
                          'privateEvidenceToken': f'{private.parent.name}/{private.name}', 'sourceSha256': source_fingerprint(),
                          'positiveInputImplemented': False}))
    finally:
        desktop.close()  # Never terminates the coordinator or guardian.


if __name__ == '__main__':
    try:
        main()
    except Exception:
        print('native_harness_blocked', file=sys.stderr)
        raise SystemExit(2)
