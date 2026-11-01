"""Review-closed native fixture. NEVER wrap coordinator/guardian in a kill Job or timeout-kill."""
import argparse
from dataclasses import dataclass
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
POSITIVE_SCENES_SOURCE_REVIEWED = False
POSITIVE_INPUT_SOURCE_REVIEWED = False
REGISTERED_SCENES = ('registered-enter', 'registered-row', 'registered-main')
POSITIVE_SCENES = ('registered-enter', 'registered-row', 'registered-main', 'posted-enter', 'posted-row', 'posted-main', 'copy', 'preview', 'expired')
INSERT_SCENES = ('registered-enter', 'registered-row', 'registered-main', 'posted-enter', 'posted-row', 'posted-main')
INPUT_CAPABILITY_SCENES = (*INSERT_SCENES, 'expired')
POSITIVE_PERMIT = b'owned_positive_row6_verified_operator_recopied_baseline\n'
PRIOR_POSITIVE_PERMIT = b'owned_positive_row6_verified_settled_baseline_v2\n'
PRIOR_CONTROL_PERMIT = b'owned_clipboard_controls_settled_baseline_v2\n'
REGISTERED_READY_FILE = 'registered-shortcut-ready'
REGISTERED_READY_RECEIPT = b'registered_os_shortcut_operator_ready\n'
ENTRY = 'SmartSnippets.UnoCandidate.exe'
# Mirrors NativeStage.Tokens in scripts/uno_candidate_native_fixture/NativeStage.cs (a test keeps them equal).
NATIVE_STAGES = ('start', 'review_gate', 'fixture_start', 'fixture_owned', 'host_start', 'host_ready',
                 'session_create', 'session_probe', 'session_capture', 'guardian_image', 'guardian_spawn',
                 'guardian_bootstrap', 'guardian_wait', 'session_focus', 'broker_bind', 'outside_job',
                 'guardian_dup', 'guardian_ready')
STAGE_LINE = re.compile(r'^\S+ ERROR native_positive_failed stage=([a-z_]+) [A-Za-z0-9_`]+ \(HResult=0x[0-9A-F]{8}\)$')
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


def failure_stage(private):
    """Last fixed stage from the sanitized failure line; any other shape is 'unknown'. Never echoes log text."""
    try:
        text = helpers.owned_bytes(private / 'native-fixture-profile/logs/app.log', private).decode('utf-8-sig')
    except (OSError, ValueError, UnicodeDecodeError):
        return 'unavailable'
    stage = 'none'
    for line in text.splitlines():
        if ' native_positive_failed' in line:
            match = STAGE_LINE.match(line)
            stage = match.group(1) if match and match.group(1) in NATIVE_STAGES else 'unknown'
    return stage


def failure_report(scene, private):
    return {'result': 'native_scene_failed', 'scene': scene, 'failureStage': failure_stage(private),
            'privateEvidenceToken': f'{private.parent.name}/{private.name}'}


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


def require_positive_authorization(scene, row6, recopied):
    if scene in POSITIVE_SCENES and (not POSITIVE_SCENES_SOURCE_REVIEWED or not row6 or not recopied
                                    or (scene in INPUT_CAPABILITY_SCENES and not POSITIVE_INPUT_SOURCE_REVIEWED)):
        raise ValueError('native_positive_review_row6_and_operator_recopy_required')


def positive_foregrounds(private, foreground):
    path = private / 'positive-surfaces.txt'
    if not path.exists():
        return {foreground}
    text = helpers.owned_bytes(path, private, 256).decode('ascii')
    if not re.fullmatch(r'[1-9][0-9]*( [1-9][0-9]*){3}', text):
        raise ValueError('native_surfaces')
    windows = [int(field) for field in text.split(' ')]
    if len(set(windows)) != 4 or any(window > 2**63 - 1 for window in windows):
        raise ValueError('native_surfaces')
    return {foreground, 0, *windows}  # Includes owned B for observation only, never admission/input.


def announce_registered_session(scene, private):
    if scene not in REGISTERED_SCENES:
        return
    token = f'{private.parent.name}/{private.name}'
    if not re.fullmatch(r'ss-uno018-ui-[a-zA-Z0-9_-]+/ss-uno019-native-[0-9a-f]{32}', token):
        raise ValueError('native_session_token')
    # Metadata only: no success, readiness, input authorization or filesystem authority.
    print(json.dumps({'phase': 'registered_session_prelaunch', 'scene': scene,
                      'privateEvidenceToken': token}), flush=True)


def registered_operator_ready(private):
    path = private / REGISTERED_READY_FILE
    if not path.exists():
        return False
    if helpers.owned_bytes(path, private, 128) != REGISTERED_READY_RECEIPT:
        raise ValueError('native_registered_readiness')
    return True


def shortcut_proof(scene):
    if scene in REGISTERED_SCENES:
        return 'registered_os_shortcut_operator_delivered'
    if scene in ('posted-enter', 'posted-row', 'posted-main'):
        return 'registered_posted_callback_not_physical'
    if scene in ('restore', 'abort', 'owner-refusal', 'copy', 'preview', 'expired'):
        return 'none'
    raise ValueError('native_scene')


def action_proof(scene):
    if scene in ('registered-enter', 'posted-enter'):
        return 'native_enter'
    if scene in ('registered-row', 'registered-main', 'posted-row', 'posted-main', 'copy', 'preview', 'expired'):
        return 'vm_callback'
    if scene == 'owner-refusal':
        return 'owned_hwnd_change_refusal'
    if scene in ('restore', 'abort'):
        return 'clipboard_control'
    raise ValueError('native_scene')


def monitor(process, desktop, foreground, output, clock=time.monotonic, sleep=time.sleep, positive_private=None, registered=False):
    """No timeout kill: coordinator retains independent RAM recovery on a pending scene."""
    failed = False
    announced = False
    operator_announced = False
    while process.poll() is None:
        try:
            desktop.validate_input_desktop()
            allowed = {foreground} if positive_private is None else positive_foregrounds(positive_private, foreground)
            if positive_private is not None and len(allowed) > 1 and not announced:
                print('native_phase=awaiting_registered_shortcut_preparation' if registered else
                      'native_phase=awaiting_owned_a operator_select_A_then_release_all_input', flush=True)
                announced = True
            if registered and positive_private is not None and not operator_announced and registered_operator_ready(positive_private):
                print('native_phase=operator_ready press_Ctrl_Alt_Shift_F11_then_release_all_input', flush=True)
                operator_announced = True
            failed |= desktop.user.GetForegroundWindow() not in allowed
            failed |= any(p.stat().st_size > 65536 for p in output)
        except Exception:
            failed = True
        sleep(.05)
    if failed or process.returncode:
        raise ValueError('native_scene_failed')


def verify_result(private, data, expected, positive=False):
    phase = b'native_phase=awaiting_owned_a' if positive else b'native_phase=published'
    phases = [b'native_phase=guardian_ready', phase]
    if expected == b'native_result=owner_refusal_restored':
        phases.append(b'native_phase=owner_refused')
    if data[0].splitlines() != [*phases, expected] or data[1]:
        raise ValueError('native_result_receipt')
    if expected in {('native_result=' + scene + '_settled').encode() for scene in REGISTERED_SCENES} and not registered_operator_ready(private):
        raise ValueError('native_registered_readiness')
    log = helpers.owned_bytes(private / 'native-fixture-profile/logs/app.log', private)
    text = log.decode('utf-8-sig')
    if (sum(line.endswith(' INFO candidate_started') for line in text.splitlines()) != 1 or ' ERROR ' in text
            or (positive and sum(line.endswith(' INFO candidate_shutdown_complete') for line in text.splitlines()) != 1)):
        raise ValueError('native_host_readiness_cleanup')
    privacy = helpers.startup.repo_module('native_privacy', 'scripts/uno_candidate_privacy.py')
    canaries = ('AΩ中🙂e\u0301\r\nsecond line', 'AΩ中🙂e\u0301\nsecond line', 'second line', str(private), str(private.parent))
    privacy.verify_negative_controls(canaries)
    if privacy.scan_evidence([log, *data], canaries)['privacy'] != 'CLEAN':
        raise ValueError('native_privacy')


@dataclass(frozen=True)
class RestorativeSessionObservation:
    """Trusted parent memory witness, never deserialized from evidence/CLI/environment."""
    session: Path
    scene: str
    source_sha256: str
    entry_sha256: str
    target_sha256: str
    guardian_sha256: str
    parent_exit_code: int
    parent_stderr: bytes
    result: str
    private_evidence_token: str


def system_powershell():
    kernel = helpers.startup.C.WinDLL('kernel32')
    kernel.GetSystemDirectoryW.argtypes = [helpers.startup.W.LPWSTR, helpers.startup.W.UINT]
    kernel.GetSystemDirectoryW.restype = helpers.startup.W.UINT
    value = helpers.startup.C.create_unicode_buffer(32768)
    count = kernel.GetSystemDirectoryW(value, len(value))
    if not count or count >= len(value):
        raise ValueError('native_ledger_system_tool')
    return Path(value.value) / 'WindowsPowerShell/v1.0/powershell.exe'


def require_owner_private(path, protected=False):
    """Fail closed unless the Windows owner and every DACL grant are this user only."""
    if sys.platform != 'win32':
        raise ValueError('native_ledger_owner_acl')
    # Fixed script, literal path via stdin, no token and no diagnostic persistence.
    script = (
        "[Console]::InputEncoding=[Text.Encoding]::UTF8;"
        "$p=[Console]::In.ReadToEnd();$s=[System.Security.Principal.WindowsIdentity]::GetCurrent().User.Value;"
        "$a=Get-Acl -LiteralPath $p -ErrorAction Stop;"
        "$o=$a.GetOwner([System.Security.Principal.SecurityIdentifier]).Value;"
        "$r=@($a.GetAccessRules($true,$true,[System.Security.Principal.SecurityIdentifier]));"
        "if($o -ne $s -or $r.Count -eq 0){exit 2};"
        "foreach($e in $r){if($e.IdentityReference.Value -ne $s -or $e.AccessControlType -ne 'Allow'){exit 2}};"
        "if(" + ('$true' if protected else '$false') + " -and -not $a.AreAccessRulesProtected){exit 2}"
    )
    powershell = system_powershell()
    environment = dict(os.environ)
    # A PowerShell 7 parent may otherwise autoload an incompatible Core security module
    # in Windows PowerShell 5.1. Restrict this child to its own trusted system modules.
    environment['PSModulePath'] = str(powershell.parent / 'Modules')
    result = subprocess.run([str(powershell), '-NoProfile', '-NonInteractive', '-Command', script],
                            input=str(path).encode('utf-8'), capture_output=True, timeout=10,
                            creationflags=subprocess.CREATE_NO_WINDOW, env=environment)
    if result.returncode or result.stdout or result.stderr:
        raise ValueError('native_ledger_owner_acl')


def verified_prior_token(observation):
    if type(observation) is not RestorativeSessionObservation:
        raise ValueError('native_ledger_witness')
    allowed = {'restore': ('clipboard_control_settled', b'native_result=regular_restored'),
               'abort': ('clipboard_control_settled', b'native_result=abort_restored'),
               'owner-refusal': ('clipboard_control_settled', b'native_result=owner_refusal_restored'),
               **{scene: ('positive_scene_settled', ('native_result=' + scene + '_settled').encode())
                  for scene in INSERT_SCENES}}
    if (observation.scene not in allowed or observation.parent_exit_code != 0 or observation.parent_stderr or
            observation.result != allowed[observation.scene][0] or
            any(not re.fullmatch('[0-9a-f]{64}', digest) for digest in
                (observation.source_sha256, observation.entry_sha256, observation.target_sha256, observation.guardian_sha256))):
        raise ValueError('native_ledger_completion')
    private = observation.session
    if (not isinstance(private, Path) or not private.is_absolute() or '..' in private.parts or
            not re.fullmatch('ss-uno019-native-[0-9a-f]{32}', private.name) or
            not re.fullmatch('ss-uno018-ui-[a-zA-Z0-9_-]+', private.parent.name) or
            observation.private_evidence_token != private.parent.name + '/' + private.name):
        raise ValueError('native_ledger_path')
    # No discovery or resolve(): reject aliases instead of turning them into authority.
    paths = [private.parent, private, private / 'stdout.log', private / 'stderr.log',
             private / 'recovery-control.txt', private / 'native-fixture-profile',
             private / 'native-fixture-profile/logs', private / 'native-fixture-profile/logs/app.log']
    if observation.scene in REGISTERED_SCENES:
        paths.append(private / REGISTERED_READY_FILE)
    for path in paths:
        for item in (path, *path.parents):
            info = item.lstat()
            if item.is_symlink() or getattr(info, 'st_file_attributes', 0) & 0x400:
                raise ValueError('native_ledger_alias')
        require_owner_private(path, protected=path == private.parent)
    data = [helpers.owned_bytes(private / name, private, 65536) for name in ('stdout.log', 'stderr.log')]
    verify_result(private, data, allowed[observation.scene][1], observation.scene in INSERT_SCENES)
    control = helpers.owned_bytes(private / 'recovery-control.txt', private, 128)
    match = re.fullmatch(rb'ss-uno019-recovery-([0-9a-f]{32})\r?\n', control)
    if not match:
        raise ValueError('native_ledger_recovery')
    token = uuid.UUID(hex=match[1].decode('ascii'))
    if not token.int:
        raise ValueError('native_ledger_recovery')
    return token.bytes_le


def launch_permit(positive, prior_token=None):
    if prior_token is None:
        return POSITIVE_PERMIT if positive else b'owned_clipboard_controls_only\n'
    if len(prior_token) != 16 or not any(prior_token):
        raise ValueError('native_baseline_token')
    return (PRIOR_POSITIVE_PERMIT if positive else PRIOR_CONTROL_PERMIT) + prior_token


def main(argv=None, *, prior_restorative_session=None):
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
    parser.add_argument('--scene', choices=('restore', 'abort', 'owner-refusal', *POSITIVE_SCENES))
    parser.add_argument('--row6-controls-verified', action='store_true', help='Operator attests independently accepted ordinary+abort controls for this exact source/artifact.')
    parser.add_argument('--operator-recopied-baseline', action='store_true', help='Operator has freshly copied a harmless supported baseline since the previous marked session; never auto-cleared.')
    args = parser.parse_args(argv)
    if args.capture_source:
        args.capture_source.write_text(source_fingerprint() + '\n', encoding='ascii')
        return
    if args.validate_build:
        validate_build(*args.validate_build)
        print('BUILD_CHECKS_ONLY')
        return
    require_authorization(args.authorized_screen_slot)  # Before artifact inspection or process/private-root creation.
    prior_token = verified_prior_token(prior_restorative_session) if prior_restorative_session is not None else None
    require_positive_authorization(args.scene, args.row6_controls_verified, args.operator_recopied_baseline or prior_token is not None)
    if not all((args.executable, args.sha256, args.target, args.target_sha256, args.guardian, args.guardian_sha256, args.scene)):
        raise ValueError('native_arguments')
    entry = validate_build(args.executable, args.sha256)
    target = validate_image(args.target, args.target_sha256, 'SmartSnippets.Target018.exe')
    guardian = validate_image(args.guardian, args.guardian_sha256, 'SmartSnippets.UnoCandidate.Guardian.exe')
    ui = helpers.startup.repo_module('native_private_directory', 'prototypes/uno-desktop/run_ui_smoke.py')
    private = ui.private_directory() / ('ss-uno019-native-' + uuid.uuid4().hex)
    private.mkdir()  # Inherits current-user-only ACL from existing reviewed owner-private helper.
    announce_registered_session(args.scene, private)
    desktop = helpers.startup.Desktop(ui.visible_windows)
    try:
        desktop.enable_dpi()
        desktop.validate_input_desktop()
        foreground = desktop.user.GetForegroundWindow()
        if not foreground:
            raise ValueError('native_foreground_unavailable')
        positive = args.scene in POSITIVE_SCENES
        (private / 'native-launch-permit').write_bytes(launch_permit(positive, prior_token))
        if args.scene in REGISTERED_SCENES:
            print('Operator: wait for operator_ready, then press Ctrl+Alt+Shift+F11 once and release all input. Do not click or type before that receipt.', flush=True)
        elif positive:
            print('Operator: select owned Target A after guardian-ready, then release all input. One transaction only; freshly recopy baseline before another session.', flush=True)
        paths = [private / 'stdout.log', private / 'stderr.log']
        with paths[0].open('xb') as out, paths[1].open('xb') as err:
            # Deliberately no WindowsJob, communicate(timeout), process-tree termination or outer watchdog.
            process = subprocess.Popen([str(entry), '--coordinate', args.scene, str(target), args.target_sha256,
                                        str(guardian), args.guardian_sha256], cwd=private, stdin=subprocess.DEVNULL,
                                       stdout=out, stderr=err)
            monitor(process, desktop, foreground, paths, positive_private=private if positive else None, registered=args.scene in REGISTERED_SCENES)
        data = [helpers.owned_bytes(p, private, 65536) for p in paths]
        expected = (f'native_result={args.scene}_settled' if positive else
                    'native_result=owner_refusal_restored' if args.scene == 'owner-refusal' else
                    'native_result=regular_restored' if args.scene == 'restore' else 'native_result=abort_restored').encode()
        verify_result(private, data, expected, positive)
        print(json.dumps({'result': 'positive_scene_settled' if positive else 'clipboard_control_settled', 'scene': args.scene,
                          'privateEvidenceToken': f'{private.parent.name}/{private.name}', 'sourceSha256': source_fingerprint(),
                          'positiveInputImplemented': positive and args.scene in INSERT_SCENES,
                          'shortcutProof': shortcut_proof(args.scene),
                          'actionProof': action_proof(args.scene),
                          'operatorReCopyRequiredBeforeNextSession': True}))
        if args.scene in ('restore', 'abort', 'owner-refusal', *INSERT_SCENES):
            return RestorativeSessionObservation(private, args.scene, source_fingerprint(), args.sha256.lower(),
                args.target_sha256.lower(), args.guardian_sha256.lower(), process.returncode, data[1],
                'positive_scene_settled' if positive else 'clipboard_control_settled',
                private.parent.name + '/' + private.name)

    except Exception:
        print(json.dumps(failure_report(args.scene, private)), flush=True)
        raise
    finally:
        desktop.close()  # Never terminates the coordinator or guardian.


if __name__ == '__main__':
    try:
        main()
    except Exception:
        print('native_harness_blocked', file=sys.stderr)
        raise SystemExit(2)
