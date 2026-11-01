"""Read-only observer of the linked-source, default-denied full-host fixture.

No launch until independent review AND an exclusive authorized screen slot.
Programmatic fixture checks are not rendered input/event delivery acceptance.
"""
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

REPO = Path(__file__).resolve().parents[1]
FULL_HOST_OBSERVER_REVIEWED = False
spec = importlib.util.spec_from_file_location('startup_observer', REPO / 'scripts/run_uno_candidate_startup.py')
startup = importlib.util.module_from_spec(spec)
spec.loader.exec_module(startup)
ENTRY = 'SmartSnippets.UnoAcceptanceFixture.exe'


def source_fingerprint():
    """Bind fixture evidence to the actual linked head, shared libraries and test driver."""
    paths = []
    for relative in ('src/SmartSnippets.Core', 'src/SmartSnippets.Presentation',
                     'src/SmartSnippets.Uno', 'scripts/uno_candidate_fixture',
                     'prototypes/uno-native-target/identity',
                     'prototypes/uno-native-target/windows-insertion'):
        paths.extend(path for path in (REPO / relative).rglob('*') if path.is_file()
                     and not {'bin', 'obj'}.intersection(path.relative_to(REPO / relative).parts))
    paths.extend(REPO / relative for relative in (
        'tests/SmartSnippets.Uno.Tests/Acceptance/CandidateAcceptanceFlow.cs',
        'tests/SmartSnippets.Uno.Tests/Acceptance/UiAssertDriver.cs',
        'scripts/run_uno_candidate_ui.py', 'scripts/uno_candidate_ui.ps1'))
    paths.extend(REPO / name for name in ('Directory.Build.props', 'Directory.Packages.props'))
    result = hashlib.sha256()
    for path in sorted(paths):
        if path.is_symlink() or getattr(path.lstat(), 'st_file_attributes', 0) & 0x400:
            raise ValueError('source_alias')
        result.update(path.relative_to(REPO).as_posix().encode('utf-8') + b'\0')
        result.update(hashlib.sha256(path.read_bytes()).digest())
    return result.hexdigest()


def validate_fixture(path, digest):
    path = Path(os.path.abspath(path))
    root = REPO / 'scripts/uno_candidate_fixture/bin/publish-acceptance'
    if (path.parent.parent != root or path.name != ENTRY
            or not re.fullmatch(r'[0-9a-f]{32}', path.parent.name)
            or not re.fullmatch(r'[0-9a-fA-F]{64}', digest)):
        raise ValueError('fixture_provenance')
    for item in (path, *path.parents):
        info = item.lstat()
        if item.is_symlink() or getattr(info, 'st_file_attributes', 0) & 0x400:
            raise ValueError('fixture_alias')
    if path.stat().st_nlink != 1 or not 32 <= path.stat().st_size <= 134217728:
        raise ValueError('fixture_file')
    data = path.read_bytes()
    inspector = startup.repo_module('acceptance_pe', 'prototypes/uno-desktop/verify_evidence.py')
    if hashlib.sha256(data).hexdigest() != digest.lower() or inspector.inspect_executable(data, 'win-x64'):
        raise ValueError('fixture_artifact')
    # Same strict native dependency/layout gate; only the test-only assembly identity differs.
    gate = startup.repo_module('acceptance_gate', 'scripts/uno_candidate_gate.py')
    gate.ENTRY = ENTRY
    gate.EXACT_PROPERTIES['AssemblyName'] = 'SmartSnippets.UnoAcceptanceFixture'
    gate.EXACT_PROPERTIES['RuntimeIdentifiers'] = 'win-x64'
    props = json.loads(owned_bytes(path.parent / 'fixture-properties.json', path.parent).decode('utf-8-sig'))
    receipt = json.loads(owned_bytes(path.parent / 'fixture-build.json', path.parent).decode('utf-8-sig'))
    if owned_bytes(path.parent / 'fixture-source.sha256', path.parent).decode('ascii').strip() != source_fingerprint():
        raise ValueError('fixture_source_provenance')
    log = owned_bytes(path.parent / 'fixture-build.log', path.parent).decode('utf-8-sig')
    # Gate's compiler receipt names the production project; adapt ONLY the linked fixture project spelling.
    adapted_log = log.replace('SmartSnippets.Uno.AcceptanceFixture ->', 'SmartSnippets.UnoCandidate ->')
    errors = gate.validate(props.get('Properties', props), adapted_log, path.parent, receipt['startedUtcMs'])
    references = json.loads(owned_bytes(path.parent / 'fixture-references.json', path.parent).decode('utf-8-sig'))
    errors.extend(gate.validate_references(references))
    if (path.parent / 'SmartSnippets.UnoAcceptanceFixture.dll').exists():
        errors.append('managed-fixture')
    if errors:
        raise ValueError('fixture_build_gate')
    return path


def owned_bytes(path, private, maximum=16 * 1024 * 1024):
    path.relative_to(private)  # Only newly owned fixture diagnostics, never a database.
    for item in (path, *path.parents):
        info = item.lstat()
        if item.is_symlink() or getattr(info, 'st_file_attributes', 0) & 0x400:
            raise ValueError('evidence_alias')
    info = path.stat()
    if not path.is_file() or info.st_nlink != 1 or info.st_size > maximum:
        raise ValueError('evidence_file')
    return path.read_bytes()


def receipt(private, phase, pid):
    path = private / (phase + '.receipt')
    if not os.path.lexists(path):
        return False
    info = path.lstat()
    if (path.is_symlink() or getattr(info, 'st_file_attributes', 0) & 0x400
            or info.st_nlink != 1 or info.st_size > 256):
        raise ValueError('receipt_alias')
    if owned_bytes(path, private, 256).decode('utf-8').splitlines() != [str(pid), private.name, phase]:
        raise ValueError('receipt_provenance')
    return True


def observe_phase(process, private, phase, desktop, initial, work, minimum_windows=1,
                  clock=time.monotonic, sleep=time.sleep):
    deadline = clock() + 12
    stable = None
    while clock() < deadline:
        if desktop.snapshot() != initial:
            raise ValueError('desktop_changed')
        if process.poll() is not None:
            raise ValueError('early_exit')
        rects = desktop.windows(process.pid)
        if any(not startup.inside(rect, work) for rect in rects):
            raise ValueError('geometry')
        if receipt(private, phase, process.pid) and len(rects) >= minimum_windows:
            stable = clock() if stable is None else stable
            if clock() - stable >= .5:
                if desktop.snapshot() != initial:
                    raise ValueError('desktop_changed')
                (private / (phase + '.continue')).write_bytes(b'continue\n')
                return
        else:
            stable = None
        sleep(.05)
    raise ValueError('missing_' + phase)


def require_graceful(process, private):
    if process.wait(timeout=12) != 0 or not receipt(private, 'graceful', process.pid):
        raise ValueError('missing_graceful_cleanup')
    log = private / 'fixture-profile/logs/app.log'
    data = owned_bytes(log, private).decode('utf-8-sig')
    if sum(line.endswith(' INFO candidate_started') for line in data.splitlines()) != 1 or ' ERROR ' in data:
        raise ValueError('host_readiness_cleanup')
    privacy = startup.repo_module('acceptance_privacy', 'scripts/uno_candidate_privacy.py')
    canaries = ('acceptance synthetic', 'acceptance-label-canary', 'acceptance-tag-canary',
                'acceptance-title-canary', 'Acceptance renamed', 'second 😀', str(private))
    privacy.verify_negative_controls(canaries)
    records = [owned_bytes(path, private) for path in (log, private / 'stdout.log', private / 'stderr.log')]
    if privacy.scan_evidence(records, canaries)['privacy'] != 'CLEAN':
        raise ValueError('privacy')
    if (private / 'stderr.log').stat().st_size:
        raise ValueError('process_stderr')


def worker(entry, digest, private, work, stream=sys.stdin.buffer):
    if stream.readline(64) != startup.PERMIT:
        raise ValueError('permit')
    startup.require_job_membership()
    startup.validate_private(private)
    entry = validate_fixture(entry, digest)
    # No default candidate profile is queried, opened, moved or deleted.
    ui = startup.repo_module('acceptance_ui', 'prototypes/uno-desktop/run_ui_smoke.py')
    desktop = startup.Desktop(ui.visible_windows)
    process = None
    try:
        desktop.enable_dpi()
        desktop.validate_work_area(work)
        initial = desktop.snapshot()
        (private / 'fixture-launch-permit').write_bytes(b'owned_fixture_only\n')
        with (private / 'stdout.log').open('xb') as out, (private / 'stderr.log').open('xb') as err:
            try:
                process = subprocess.Popen([str(entry)], cwd=private, stdin=subprocess.DEVNULL, stdout=out, stderr=err)
                for phase, count in (('ready', 1), ('management', 1), ('preview', 2), ('recreated', 2)):
                    observe_phase(process, private, phase, desktop, initial, work, count)
                # Keep observing foreground/epoch until actual process exit, not only before it.
                deadline = time.monotonic() + 12
                while process.poll() is None:
                    if desktop.snapshot() != initial:
                        raise ValueError('desktop_changed')
                    if time.monotonic() >= deadline:
                        raise ValueError('graceful_timeout')
                    time.sleep(.05)
                require_graceful(process, private)
            finally:
                if process is not None and process.poll() is None:
                    startup.stop_app(process)  # Failure cleanup never becomes graceful PASS.
                if desktop.snapshot() != initial:
                    raise ValueError('desktop_changed')
    finally:
        desktop.close()
    print('full_host_observed', flush=True)


def require_empty_job(job, clock=time.monotonic, sleep=time.sleep):
    # Successful worker exit must leave zero descendants BEFORE emergency termination.
    # Otherwise a leaked child could be killed by finish_job and falsely called graceful.
    class Accounting(startup.C.Structure):
        _fields_ = [('times', startup.C.c_int64 * 4), ('faults', startup.W.DWORD),
                    ('total', startup.W.DWORD), ('active', startup.W.DWORD), ('terminated', startup.W.DWORD)]
    query = job.kernel.QueryInformationJobObject
    query.argtypes = [startup.W.HANDLE, startup.C.c_int, startup.C.c_void_p, startup.W.DWORD, startup.C.c_void_p]
    query.restype = startup.W.BOOL
    deadline = clock() + 3
    while True:
        accounting = Accounting()
        if not query(job.handle, 1, startup.C.byref(accounting), startup.C.sizeof(accounting), None):
            raise ValueError('graceful_job_query_failed')
        if not accounting.active:
            return
        if clock() >= deadline:
            raise ValueError('graceful_job_not_empty')
        sleep(.05)


def supervise(command, job_factory, popen=subprocess.Popen):
    process = popen(command, stdin=subprocess.PIPE, stdout=subprocess.PIPE, stderr=subprocess.PIPE,
                    creationflags=subprocess.CREATE_NO_WINDOW)
    job = None
    try:
        job = job_factory(process)  # Assignment before permit; worker cannot launch before this.
        out, err = process.communicate(startup.PERMIT, timeout=75)
        if process.returncode or err or out not in (b'full_host_observed\n', b'full_host_observed\r\n'):
            raise ValueError('worker_protocol')
        require_empty_job(job)
    finally:
        if job is not None:
            startup.finish_job(job, process)  # Requires zero active, propagates cleanup failure.
        else:
            startup.stop_app(process)


def record_failure(private, error):
    # Fixed tokens only. Never echo an exception message, path, title or payload.
    allowed = {'desktop_changed', 'early_exit', 'geometry', 'missing_ready', 'missing_management',
               'missing_preview', 'missing_recreated', 'missing_graceful_cleanup', 'graceful_timeout',
               'host_readiness_cleanup', 'privacy', 'process_stderr', 'worker_protocol',
               'graceful_job_not_empty', 'graceful_job_query_failed', 'receipt_provenance',
               'receipt_alias', 'evidence_alias', 'evidence_file', 'input', 'input_query_failed',
               'ui_ready', 'ui_timeout', 'ui_quiet_timeout', 'ui_helper_protocol', 'ui_receipt',
               'ui_helper_output_limit', 'clipboard_changed', 'foreground_changed',
               'desktop_unavailable', 'window_recreated', 'ui_exit_code'}
    reason = str(error) if isinstance(error, ValueError) and str(error) in allowed else 'observer_failure'
    try:
        with (private / ('observer-' + ('worker' if '--worker-private' in sys.argv else 'parent') + '-failure.json')).open('x') as file:
            json.dump({'reason': reason}, file)
    except OSError:
        pass


def main():
    parser = startup.Parser(allow_abbrev=False)
    parser.add_argument('--executable', required=True, type=Path)
    parser.add_argument('--sha256', required=True)
    parser.add_argument('--authorized-screen-slot', action='store_true')
    parser.add_argument('--work-area', required=True, nargs=4, type=int)
    parser.add_argument('--worker-private', type=Path, help=argparse.SUPPRESS)
    args = parser.parse_args()
    if not FULL_HOST_OBSERVER_REVIEWED or not args.authorized_screen_slot or sys.platform != 'win32':
        raise ValueError('authorization')
    entry = validate_fixture(args.executable, args.sha256)
    if not startup.inside(args.work_area, args.work_area):
        raise ValueError('work_area')
    if args.worker_private:
        try:
            worker(entry, args.sha256, args.worker_private, args.work_area)
        except BaseException as error:
            record_failure(args.worker_private, error)
            raise
        return None
    ui = startup.repo_module('acceptance_ui', 'prototypes/uno-desktop/run_ui_smoke.py')
    identity = startup.repo_module('acceptance_identity', 'prototypes/uno-native-target/run_identity_harness.py')
    private = ui.private_directory()
    command = [sys.executable, '-I', '-B', str(Path(__file__).resolve()), '--executable', str(entry),
               '--sha256', args.sha256, '--authorized-screen-slot', '--work-area', *map(str, args.work_area),
               '--worker-private', str(private)]
    try:
        supervise(command, identity.WindowsJob)
    except BaseException as error:
        record_failure(private, error)
        raise
    return {'privateEvidenceToken': private.name, 'artifactSha256': args.sha256.lower(),
            'fixtureSourceSha256': source_fingerprint(),
            'observerSha256': hashlib.sha256(Path(__file__).read_bytes()).hexdigest()}


def cli():
    is_worker = any(arg == '--worker-private' or arg.startswith('--worker-private=') for arg in sys.argv[1:])
    try:
        token = main()
        if token is not None:
            print(json.dumps({'fullHost': 'OBSERVED', 'management': 'PROGRAMMATIC_INVARIANTS',
                              'previewLifecycle': 'PROGRAMMATIC_WITH_WINDOW_OBSERVATIONS',
                              'renderedEventDelivery': 'NOT_RUN', 'nativeMutation': 'NOT_RUN',
                              'stop': 'GRACEFUL', 'job': 'ZERO_ACTIVE', 'profile': 'PRIVATE_RETAINED',
                              **token}))
    except BaseException:
        if not is_worker:
            print(json.dumps({'fullHost': 'BLOCKED', 'nativeMutation': 'NOT_RUN'}))
        return 1
    return 0


if __name__ == '__main__':
    sys.exit(cli())
