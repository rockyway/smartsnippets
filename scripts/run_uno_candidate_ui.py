"""Review-closed actual rendered-management callbacks on one retained owned HWND.

SetFocus is explicitly permitted management activation, NOT nonactivation QA proof.
No native launch before independent review and an exclusive authorized screen slot.
"""
import hashlib
import importlib.util
import json
from pathlib import Path
import subprocess
import sys
import time

REPO = Path(__file__).resolve().parents[1]
UI_SOURCE_REVIEWED = False
spec = importlib.util.spec_from_file_location('ui_full_host', REPO / 'scripts/run_uno_candidate_acceptance.py')
base = importlib.util.module_from_spec(spec)
spec.loader.exec_module(base)
startup = base.startup
HELPER = REPO / 'scripts/uno_candidate_ui.ps1'


def require_authorization(authorized):
    if not UI_SOURCE_REVIEWED or not authorized or sys.platform != 'win32':
        raise ValueError('ui_source_review_and_slot_required')


def validate_helper_output(code, out, err):
    if code or err or out not in (b'ui_callbacks_observed\n', b'ui_callbacks_observed\r\n'):
        raise ValueError('ui_helper_protocol')


def source_fingerprint():
    digest = hashlib.sha256(base.source_fingerprint().encode('ascii'))
    for relative in ('scripts/run_uno_candidate_ui.py', 'scripts/uno_candidate_ui.ps1',
                     'tests/SmartSnippets.Uno.Tests/Acceptance/UiAssertDriver.cs'):
        digest.update(base.owned_bytes(REPO / relative, REPO))
    return digest.hexdigest()


def scene_phases(scene):
    if scene == 'snippets':
        return ('ui-ready', 'ui-new', 'ui-saved', 'ui-cancel',
                'ui-dirty-saved', 'ui-discard-dirty', 'ui-discarded')
    if scene == 'folders':
        return ('ui-ready', 'ui-folder-created', 'ui-folder-renamed', 'ui-folder-deleted')
    if scene == 'settings':
        return ('ui-ready', 'ui-settings-theme', 'ui-settings-size', 'ui-settings-reloaded')
    raise ValueError('ui_scene')


def validate_folder_actions(value):
    if value != b'invoke=5\nvalue=2\nfocus=2\n':
        raise ValueError('ui_folder_actions')


def validate_snippet_actions(value):
    allowed = tuple(f'invoke={8 + backs}\nvalue=3\nfocus=4\nback={backs}\n'.encode('ascii')
                    for backs in range(3))
    if value not in allowed:
        raise ValueError('ui_snippet_actions')


def validate_settings_actions(value):
    if value != b'scroll=2\nselect=2\n':
        raise ValueError('ui_settings_actions')


def system_powershell():
    kernel = startup.C.WinDLL('kernel32', use_last_error=True)
    kernel.GetSystemDirectoryW.argtypes = [startup.W.LPWSTR, startup.W.UINT]
    kernel.GetSystemDirectoryW.restype = startup.W.UINT
    value = startup.C.create_unicode_buffer(32768)
    count = kernel.GetSystemDirectoryW(value, len(value))
    if not count or count >= len(value):
        raise ValueError('system_powershell')
    return Path(value.value) / 'WindowsPowerShell/v1.0/powershell.exe'


def require_all_input_up(desktop):
    user = desktop.user
    user.GetAsyncKeyState.argtypes = [startup.C.c_int]
    user.GetAsyncKeyState.restype = startup.C.c_short
    if any(user.GetAsyncKeyState(key) & 0x8000 for key in range(1, 256)):
        raise ValueError('input')


def quiet_input(desktop, expected=None):
    class LastInput(startup.C.Structure):
        _fields_ = [('size', startup.W.UINT), ('time', startup.W.DWORD)]
    user = desktop.user
    user.GetLastInputInfo.argtypes = [startup.C.POINTER(LastInput)]
    user.GetLastInputInfo.restype = startup.W.BOOL
    state = LastInput(startup.C.sizeof(LastInput), 0)
    if not user.GetLastInputInfo(startup.C.byref(state)):
        raise ValueError('input_query_failed')
    kernel = startup.C.WinDLL('kernel32')
    kernel.GetTickCount.restype = startup.W.DWORD
    idle = (kernel.GetTickCount() - state.time) & 0xffffffff
    if (expected is not None and state.time != expected) or idle < 1500:
        raise ValueError('input')
    require_all_input_up(desktop)
    return state.time


def retain_window(process, private, desktop, work, visible_windows, initial, epoch=None):
    deadline = time.monotonic() + 8
    while time.monotonic() < deadline:
        if desktop.snapshot() != initial:
            raise ValueError('desktop_changed')
        require_all_input_up(desktop)
        if epoch is not None:
            quiet_input(desktop, epoch)
        if process.poll() is not None:
            raise ValueError('early_exit')
        if base.receipt(private, 'ui-ready', process.pid):
            handles = visible_windows(process.pid, identities=True)
            rects = desktop.windows(process.pid)
            if len(handles) == len(rects) == 1 and startup.inside(rects[0], work):
                return handles[0]
        time.sleep(.05)
    raise ValueError('ui_ready')


def settle_quiet_epoch(process, desktop, initial, work, clock=time.monotonic, sleep=time.sleep):
    # Recent input may settle before launch (process=None). After nonactivating
    # startup, establish the immutable epoch again before any UIA lookup/focus/action.
    deadline = clock() + 4
    while clock() < deadline:
        if desktop.snapshot() != initial:
            raise ValueError('desktop_changed')
        if process is not None and process.poll() is not None:
            raise ValueError('early_exit')
        require_all_input_up(desktop)
        if process is not None:
            rects = desktop.windows(process.pid)
            if len(rects) != 1 or not startup.inside(rects[0], work):
                raise ValueError('geometry')
        try:
            epoch = quiet_input(desktop)
        except ValueError as error:
            if str(error) != 'input':
                raise
        else:
            if desktop.snapshot() != initial:
                raise ValueError('desktop_changed')
            return epoch
        sleep(.05)
    raise ValueError('ui_quiet_timeout')


class ForegroundChanged(ValueError):
    def __init__(self, foreground):
        super().__init__('foreground_changed')
        self.foreground = foreground


def save_foreground_failure(private, desktop, foreground, initial, hwnd, pid):
    if not foreground:
        relation = 'none'
    elif foreground == initial[0]:
        relation = 'initial'
    elif foreground == hwnd:
        relation = 'owned_main'
    else:
        owner = startup.W.DWORD()
        thread = desktop.user.GetWindowThreadProcessId(foreground, startup.C.byref(owner))
        relation = 'unknown' if not thread else 'owned_other' if owner.value == pid else 'external'
    with (private / 'foreground-failure.json').open('x', encoding='ascii') as report:
        json.dump({'foregroundRelation': relation}, report)


def observe_desktop(desktop, allow_zero_fg=False):
    desktop.validate_input_desktop()
    fg = desktop.user.GetForegroundWindow()
    clip = desktop.user.GetClipboardSequenceNumber()
    if not clip or (not allow_zero_fg and not fg):
        raise ValueError('desktop_unavailable')
    return fg, clip


def shutdown_frame(desktop, pid, hwnd, initial, work, closed_seen, owned_foreground_seen, visible_windows):
    # Sample foreground first: a monotonic close must not leave a stale live-HWND sample
    # paired with the later, legitimate post-close foreground transition.
    fg, clip = observe_desktop(desktop, allow_zero_fg=True)
    if clip != initial[1]:
        raise ValueError('clipboard_changed')
    handles = visible_windows(pid, identities=True)
    rects = desktop.windows(pid)
    if any(not startup.inside(rect, work) for rect in rects):
        raise ValueError('geometry')
    if closed_seen or hwnd not in handles:
        if handles or rects:
            raise ValueError('window_recreated')
        return True
    if fg not in (initial[0], hwnd) or (owned_foreground_seen and fg != hwnd):
        raise ForegroundChanged(fg)
    return False


def save_fixture_exit(private, code):
    if type(code) is not int:
        raise ValueError('ui_exit_code')
    with (private / 'fixture-exit.json').open('x', encoding='ascii') as report:
        json.dump({'exitCode': code}, report)


def save_helper_diagnostics(private, out, err):
    if any(not isinstance(value, bytes) or len(value) > 65536 for value in (out, err)):
        raise ValueError('ui_helper_output_limit')
    for name, value in (('ui-helper-stdout.log', out), ('ui-helper-stderr.log', err)):
        with (private / name).open('xb') as file:
            file.write(value)


def worker(entry, digest, private, work, stream=sys.stdin.buffer, scene='snippets'):
    phases = scene_phases(scene)
    if stream.readline(64) != startup.PERMIT:
        raise ValueError('permit')
    startup.require_job_membership()
    startup.validate_private(private)
    entry = base.validate_fixture(entry, digest)
    ui = startup.repo_module('rendered_ui_windows', 'prototypes/uno-desktop/run_ui_smoke.py')
    desktop = startup.Desktop(ui.visible_windows)
    process = helper = None
    helper_diagnostics_saved = False
    try:
        desktop.enable_dpi()
        desktop.validate_work_area(work)
        initial = desktop.snapshot()
        epoch = settle_quiet_epoch(None, desktop, initial, work)
        (private / 'fixture-launch-permit').write_bytes(b'owned_fixture_only\n')
        (private / 'fixture-rendered-ui-permit').write_bytes(b'owned_rendered_ui_019\n')
        if scene == 'folders':
            with (private / 'fixture-folder-scene-permit').open('xb') as permit:
                permit.write(b'owned_folder_ui_019\n')
        if scene == 'settings':
            with (private / 'fixture-settings-scene-permit').open('xb') as permit:
                permit.write(b'owned_settings_ui_019\n')
        with (private / 'stdout.log').open('xb') as out, (private / 'stderr.log').open('xb') as err:
            process = subprocess.Popen([str(entry)], cwd=private, stdin=subprocess.DEVNULL, stdout=out, stderr=err)
            hwnd = retain_window(process, private, desktop, work, ui.visible_windows, initial)
            epoch = settle_quiet_epoch(process, desktop, initial, work)
            # System Windows PowerShell only, STA, no profiles, no generic UIA selector/operation input.
            powershell = system_powershell()
            command = [str(powershell), '-NoLogo', '-NoProfile', '-NonInteractive', '-STA',
                       '-File', str(HELPER), '-Scene', scene, '-OwnedPid', str(process.pid), '-OwnedHwnd', str(hwnd),
                       '-InitialForeground', str(initial[0]), '-InputEpoch', str(epoch), '-PrivateRoot', str(private), '-Artifact', str(entry)]
            helper = subprocess.Popen(command, cwd=private, stdin=subprocess.DEVNULL,
                                      stdout=subprocess.PIPE, stderr=subprocess.PIPE, creationflags=subprocess.CREATE_NO_WINDOW)
            deadline = time.monotonic() + 35
            owned_foreground_seen = False
            while helper.poll() is None:
                quiet_input(desktop, epoch)
                fg, clip = observe_desktop(desktop)
                if clip != initial[1]:
                    raise ValueError('clipboard_changed')
                if fg not in (initial[0], hwnd) or (owned_foreground_seen and fg != hwnd):
                    raise ForegroundChanged(fg)
                owned_foreground_seen |= fg == hwnd
                if any(not startup.inside(rect, work) for rect in desktop.windows(process.pid)):
                    raise ValueError('geometry')
                if time.monotonic() > deadline:
                    raise ValueError('ui_timeout')
                time.sleep(.05)
            out, err = helper.communicate(timeout=2)
            helper_diagnostics_saved = True
            save_helper_diagnostics(private, out, err)
            validate_helper_output(helper.returncode, out, err)
            for phase in phases:
                if not base.receipt(private, phase, process.pid):
                    raise ValueError('ui_receipt')
            for phase in phases[:-1]:
                if base.owned_bytes(private / (phase + '.continue'), private) != b'continue\n':
                    raise ValueError('ui_receipt')
            if scene == 'snippets':
                validate_snippet_actions(base.owned_bytes(private / 'ui-snippet-actions.receipt', private))
            if scene == 'folders':
                validate_folder_actions(base.owned_bytes(private / 'ui-folder-actions.receipt', private))
            if scene == 'settings':
                validate_settings_actions(base.owned_bytes(private / 'ui-settings-actions.receipt', private))
            # Release shutdown only after helper exit and every actual callback postcondition.
            quiet_input(desktop, epoch)
            fg, clip = observe_desktop(desktop)
            if clip != initial[1]:
                raise ValueError('clipboard_changed')
            if fg not in (initial[0], hwnd) or (owned_foreground_seen and fg != hwnd):
                raise ForegroundChanged(fg)
            with (private / (phases[-1] + '.continue')).open('xb') as ack:
                ack.write(b'continue\n')
            deadline = time.monotonic() + 12
            closed_seen = False
            while process.poll() is None:
                quiet_input(desktop, epoch)
                closed_seen = shutdown_frame(desktop, process.pid, hwnd, initial, work, closed_seen,
                                             owned_foreground_seen, ui.visible_windows)
                if time.monotonic() > deadline:
                    raise ValueError('graceful_timeout')
                time.sleep(.05)
            save_fixture_exit(private, process.returncode)
            base.require_graceful(process, private)
            quiet_input(desktop, epoch)
            shutdown_frame(desktop, process.pid, hwnd, initial, work, True,
                           owned_foreground_seen, ui.visible_windows)
            fg, clip = observe_desktop(desktop, allow_zero_fg=True)
            if clip != initial[1]:
                raise ValueError('clipboard_changed')
            with (private / 'ui-shutdown-observation.json').open('x', encoding='ascii') as observation:
                json.dump({'foregroundAfterClose': 'initial' if fg == initial[0] else 'none' if not fg else 'other'}, observation)
            # Extend the existing privacy scanner and its negative controls with this mode's canaries.
            privacy = startup.repo_module('ui_privacy', 'scripts/uno_candidate_privacy.py')
            canaries = ('ui019 synthetic saved', 'ui019 synthetic dirty', 'ui019 synthetic folder',
                        'ui019 synthetic renamed', 'ui019 synthetic attached', 'ui019 synthetic discarded', str(private))
            privacy.verify_negative_controls(canaries)
            records = [base.owned_bytes(p, private) for p in
                       (private / 'stdout.log', private / 'stderr.log', private / 'fixture-profile/logs/app.log',
                        private / 'ui-helper-stdout.log', private / 'ui-helper-stderr.log')]
            if privacy.scan_evidence(records, canaries)['privacy'] != 'CLEAN':
                raise ValueError('privacy')
    except BaseException as error:
        if isinstance(error, ForegroundChanged) and process is not None:
            save_foreground_failure(private, desktop, error.foreground, initial, hwnd, process.pid)
        base.record_failure(private, error)
        raise
    finally:
        try:
            if helper is not None and helper.poll() is None:
                startup.stop_app(helper)
            if helper is not None and not helper_diagnostics_saved:
                out, err = helper.communicate(timeout=2)
                save_helper_diagnostics(private, out, err)
        finally:
            try:
                if process is not None and process.poll() is None:
                    startup.stop_app(process)
            finally:
                desktop.close()
    print('ui_management_observed', flush=True)


def supervise(command, job_factory, popen=subprocess.Popen):
    process = popen(command, stdin=subprocess.PIPE, stdout=subprocess.PIPE, stderr=subprocess.PIPE,
                    creationflags=subprocess.CREATE_NO_WINDOW)
    job = None
    try:
        job = job_factory(process)
        out, err = process.communicate(startup.PERMIT, timeout=65)
        if process.returncode or err or out not in (b'ui_management_observed\n', b'ui_management_observed\r\n'):
            raise ValueError('worker_protocol')
        base.require_empty_job(job)
    finally:
        if job is not None:
            startup.finish_job(job, process)
        else:
            startup.stop_app(process)


def main():
    parser = startup.Parser(allow_abbrev=False)
    parser.add_argument('--scene', choices=('snippets', 'folders', 'settings'), default='snippets')
    parser.add_argument('--executable', required=True, type=Path)
    parser.add_argument('--sha256', required=True)
    parser.add_argument('--authorized-screen-slot', action='store_true')
    parser.add_argument('--work-area', required=True, nargs=4, type=int)
    parser.add_argument('--worker-private', type=Path, help=__import__('argparse').SUPPRESS)
    args = parser.parse_args()
    require_authorization(args.authorized_screen_slot)
    entry = base.validate_fixture(args.executable, args.sha256)
    if not startup.inside(args.work_area, args.work_area):
        raise ValueError('work_area')
    if args.worker_private:
        worker(entry, args.sha256, args.worker_private, args.work_area, scene=args.scene)
        return None
    ui = startup.repo_module('ui_private', 'prototypes/uno-desktop/run_ui_smoke.py')
    identity = startup.repo_module('ui_identity', 'prototypes/uno-native-target/run_identity_harness.py')
    private = ui.private_directory()
    command = [sys.executable, '-I', '-B', str(Path(__file__).resolve()), '--executable', str(entry),
               '--sha256', args.sha256, '--scene', args.scene, '--authorized-screen-slot', '--work-area', *map(str, args.work_area),
               '--worker-private', str(private)]
    try:
        supervise(command, identity.WindowsJob)
    except BaseException as error:
        base.record_failure(private, error)
        raise
    after_close = json.loads(base.owned_bytes(private / 'ui-shutdown-observation.json', private))
    if after_close not in ({'foregroundAfterClose': 'initial'}, {'foregroundAfterClose': 'none'},
                           {'foregroundAfterClose': 'other'}):
        raise ValueError('ui_receipt')
    return {**after_close, 'renderedNewEditSaveDirtyCancel': 'OBSERVED' if args.scene == 'snippets' else 'NOT_RUN',
            'folders': 'OBSERVED' if args.scene == 'folders' else 'NOT_RUN',
            'settings': 'OBSERVED' if args.scene == 'settings' else 'NOT_RUN',
            'settingsReload': 'REPOSITORY_READ' if args.scene == 'settings' else 'NOT_RUN',
            'settingsRelaunch': 'NOT_RUN',
            'folderSnippetAssignment': 'FIXTURE_SEED_ONLY' if args.scene == 'folders' else 'NOT_RUN',
            'rowSelection': 'NOT_RUN', 'dirtySaveDiscard': 'OBSERVED' if args.scene == 'snippets' else 'NOT_RUN',
            'filePicker': 'NOT_RUN',
            'nativeAdapters': 'DEFAULT_DENIED', 'stop': 'GRACEFUL', 'job': 'ZERO_ACTIVE',
            'privateEvidenceToken': private.name, 'sourceSha256': source_fingerprint()}


def cli():
    try:
        result = main()
        if result is not None:
            print(json.dumps(result))
        return 0
    except BaseException:
        if '--worker-private' not in sys.argv:
            print(json.dumps({'renderedManagement': 'BLOCKED', 'filePicker': 'NOT_RUN'}))
        return 1


if __name__ == '__main__':
    sys.exit(cli())
