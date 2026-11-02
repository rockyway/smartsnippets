"""Review-closed actual rendered-management callbacks on one retained owned HWND.

SetFocus is explicitly permitted management activation, NOT nonactivation QA proof.
No native launch before independent review and an exclusive authorized screen slot.
"""
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import subprocess
import sys
import time

REPO = Path(__file__).resolve().parents[1]
helper_schema_spec = importlib.util.spec_from_file_location(
    'uno_candidate_helper_schema', REPO / 'scripts/uno_candidate_helper_schema.py')
helper_schema = importlib.util.module_from_spec(helper_schema_spec)
helper_schema_spec.loader.exec_module(helper_schema)
UI_SOURCE_REVIEWED = False
spec = importlib.util.spec_from_file_location('ui_full_host', REPO / 'scripts/run_uno_candidate_acceptance.py')
base = importlib.util.module_from_spec(spec)
spec.loader.exec_module(base)
startup = base.startup
foreground_identity = startup.repo_module('ui_foreground_identity', 'scripts/uno_foreground_identity.py')
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
    if scene == 'file-picker':
        return ('ui-ready', 'ui-import-cancelled', 'ui-imported', 'ui-export-cancelled', 'ui-exported')
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


PICKER_PHASES = helper_schema.PICKER_PHASES
INPUT_PHASES = helper_schema.INPUT_PHASES
PICKER_TIMELINE_STEPS = ('helper_entry', 'before_main_invoke', 'after_main_invoke', 'dialog_visible',
                         'after_from_handle', 'before_cancel_invoke', 'after_cancel_invoke',
                         'dialog_closed', 'guard_refused')
PICKER_TIMELINE_FIELDS = frozenset(('step', 'epoch', 'elapsed_ms', 'changed_from_baseline'))
HELPER_FAILURE_ALLOWLISTS = helper_schema.HELPER_FAILURE_ALLOWLISTS
HELPER_FAILURE_PATTERN = helper_schema.HELPER_FAILURE_PATTERN


def _unique_json_object(pairs):
    value = {}
    for key, item in pairs:
        if key in value:
            raise ValueError('ui_picker_epoch_timeline')
        value[key] = item
    return value


def _parse_picker_timeline_file(phase, raw):
    if phase not in PICKER_PHASES or not raw or not raw.endswith(b'\n') or len(raw) > 65536:
        raise ValueError('ui_picker_epoch_timeline')
    lines = raw.splitlines()
    if not lines or len(lines) > len(PICKER_TIMELINE_STEPS):
        raise ValueError('ui_picker_epoch_timeline')
    rows = []
    previous_elapsed = -1
    for line in lines:
        if not line or b'\r' in line:
            raise ValueError('ui_picker_epoch_timeline')
        row = json.loads(line.decode('ascii'), object_pairs_hook=_unique_json_object)
        if (type(row) is not dict or set(row) != PICKER_TIMELINE_FIELDS
                or row['step'] not in PICKER_TIMELINE_STEPS
                or type(row['epoch']) is not int or not 0 <= row['epoch'] <= 0xffffffff
                or type(row['elapsed_ms']) is not int or not previous_elapsed <= row['elapsed_ms'] <= 0x7fffffff
                or type(row['changed_from_baseline']) is not bool):
            raise ValueError('ui_picker_epoch_timeline')
        previous_elapsed = row['elapsed_ms']
        rows.append(row)
    ordered = ['helper_entry', 'before_main_invoke', 'after_main_invoke', 'dialog_visible', 'after_from_handle']
    if phase.endswith('-cancel'):
        ordered.extend(('before_cancel_invoke', 'after_cancel_invoke'))
    ordered.append('dialog_closed')
    steps = [row['step'] for row in rows]
    guard_refused = steps[-1:] == ['guard_refused']
    ordinary_steps = steps[:-1] if guard_refused else steps
    if ((guard_refused and not ordinary_steps)
            or ordinary_steps != ordered[:len(ordinary_steps)]):
        raise ValueError('ui_picker_epoch_timeline')
    return rows


def parse_picker_epoch_timeline(private):
    """Parse only fixed, content-free picker timeline files; collapse any unknown schema."""
    timelines = {}
    try:
        for phase in PICKER_PHASES:
            path = private / ('picker-epoch-timeline-' + phase + '.jsonl')
            if not os.path.lexists(path):
                continue
            timelines[phase] = _parse_picker_timeline_file(phase, base.owned_bytes(path, private, 65536))
    except Exception:
        return None
    return timelines or None


def validate_picker_actions(value):
    if value != b'main=4\npicker=4\nvalue=2\ninfo=2\n':
        raise ValueError('ui_picker_actions')


def validate_picker_phase_actions(phase, value):
    if phase not in PICKER_PHASES:
        raise ValueError('ui_picker_phase')
    expected = b'invoke=1\npicker=1\nvalue=0\n' if phase.endswith('-cancel') else b'invoke=2\npicker=1\nvalue=1\n'
    if value != expected:
        raise ValueError('ui_picker_phase_actions')


def validate_picker_identity(main, popup, owner, pid, expected_pid, thread, main_thread, cls):
    if (not popup or popup == main or owner != main or pid != expected_pid
            or not thread or not main_thread or cls != '#32770'):
        raise ValueError('ui_picker_identity')
    return popup


def picker_phase(private):
    # Exactly one active fixed phase, in order. Marker content is not a selector.
    active = None
    pending = False
    for phase in PICKER_PHASES:
        opened = private / ('picker-' + phase + '.active')
        closed = private / ('picker-' + phase + '.closed')
        if os.path.lexists(opened):
            if pending or base.owned_bytes(opened, private) != b'owned_picker_019\n':
                raise ValueError('ui_picker_phase')
            if os.path.lexists(closed):
                if base.owned_bytes(closed, private) != b'owned_picker_019\n':
                    raise ValueError('ui_picker_phase')
            else:
                active = phase
                pending = True
        else:
            if os.path.lexists(closed):
                raise ValueError('ui_picker_phase')
            pending = True
    return active


def owned_picker(desktop, hwnd, pid, authenticated=None, foreground=None, *, sample_foreground=True):
    user = desktop.user
    user.GetWindow.argtypes = [startup.W.HWND, startup.W.UINT]
    user.GetWindow.restype = startup.W.HWND
    user.GetClassNameW.argtypes = [startup.W.HWND, startup.W.LPWSTR, startup.C.c_int]
    user.GetClassNameW.restype = startup.C.c_int
    user.IsWindow.argtypes = [startup.W.HWND]
    user.IsWindow.restype = startup.W.BOOL
    user.IsWindowVisible.argtypes = [startup.W.HWND]
    user.IsWindowVisible.restype = startup.W.BOOL
    popup = user.GetWindow(hwnd, 6)  # GW_ENABLEDPOPUP, never desktop enumeration.
    from_foreground = not popup or popup == hwnd
    if from_foreground:
        # Pre-sample authentication checks only GW_ENABLEDPOPUP; the frame later
        # supplies its sampled foreground to avoid taking a second focus sample.
        if foreground is None and not sample_foreground:
            return None
        popup, _ = foreground_identity.normalize(desktop, foreground if foreground is not None else user.GetForegroundWindow())
        if not popup or popup == hwnd:
            return None
    owner = user.GetWindow(popup, 4)  # GW_OWNER must be the retained main, not an ancestor.
    actual = startup.W.DWORD()
    thread = user.GetWindowThreadProcessId(popup, startup.C.byref(actual))
    main_pid = startup.W.DWORD()
    main_thread = user.GetWindowThreadProcessId(hwnd, startup.C.byref(main_pid))
    main_class = startup.C.create_unicode_buffer(256)
    if (not user.GetClassNameW(hwnd, main_class, len(main_class))
            or main_class.value != 'WinUIDesktopWin32WindowClass' or not user.IsWindowVisible(hwnd)):
        raise ValueError('ui_picker_main_identity')
    cls = startup.C.create_unicode_buffer(256)
    class_ok = user.GetClassNameW(popup, cls, len(cls))
    visible = bool(user.IsWindowVisible(popup))
    if main_pid.value != pid:
        raise ValueError('ui_picker_identity')
    # Only a previously authenticated exact HWND may disappear during close.
    if popup == authenticated and (not user.IsWindow(popup) or not visible):
        return None
    if from_foreground and (owner != hwnd or actual.value != pid or not thread
                            or not class_ok or cls.value != '#32770'):
        return None
    if not class_ok:
        raise ValueError('ui_picker_identity')
    return validate_picker_identity(hwnd, popup, owner, actual.value, pid, thread, main_thread,
                                    cls.value)


def picker_foreground(desktop, fg, popup, authenticated):
    fg, _ = foreground_identity.normalize(desktop, fg)
    if popup is not None:
        return fg == popup
    return bool(authenticated and fg == authenticated and
                (not desktop.user.IsWindow(authenticated) or
                 not desktop.user.IsWindowVisible(authenticated)))


def system_powershell():
    kernel = startup.C.WinDLL('kernel32', use_last_error=True)
    kernel.GetSystemDirectoryW.argtypes = [startup.W.LPWSTR, startup.W.UINT]
    kernel.GetSystemDirectoryW.restype = startup.W.UINT
    value = startup.C.create_unicode_buffer(32768)
    count = kernel.GetSystemDirectoryW(value, len(value))
    if not count or count >= len(value):
        raise ValueError('system_powershell')
    return Path(value.value) / 'WindowsPowerShell/v1.0/powershell.exe'


class InputRefused(ValueError):
    def __init__(self, subreason):
        if subreason not in ('held', 'epoch_changed', 'idle'):
            raise ValueError('input_query_failed')
        super().__init__('input')
        self.input_subreason = subreason


def require_all_input_up(desktop):
    user = desktop.user
    user.GetAsyncKeyState.argtypes = [startup.C.c_int]
    user.GetAsyncKeyState.restype = startup.C.c_short
    if any(user.GetAsyncKeyState(key) & 0x8000 for key in range(1, 256)):
        raise InputRefused('held')


def quiet_input(desktop, expected=None, phase='unspecified'):
    if phase not in INPUT_PHASES:
        raise ValueError('input_query_failed')
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
    if expected is not None and state.time != expected:
        error = InputRefused('epoch_changed')
        # Unsigned timestamp difference, NOT an event count or source attribution.
        error.input_epoch_delta = (state.time - expected) & 0xffffffff
        error.input_idle_ms = idle
        error.input_phase = phase
        raise error
    if idle < 1500:
        raise InputRefused('idle')
    require_all_input_up(desktop)
    return state.time


def retain_window(process, private, desktop, work, visible_windows, initial, epoch=None):
    deadline = time.monotonic() + 8
    while time.monotonic() < deadline:
        if normalized_snapshot(desktop) != initial:
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
        if normalized_snapshot(desktop) != initial:
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
            if normalized_snapshot(desktop) != initial:
                raise ValueError('desktop_changed')
            return epoch
        sleep(.05)
    raise ValueError('ui_quiet_timeout')


class ForegroundChanged(ValueError):
    def __init__(self, foreground, popup=None, phase_index=-1, sample_order=0, token='foreground_changed'):
        super().__init__(token)
        self.foreground = foreground
        self.popup = popup
        self.phase_index = phase_index
        self.sample_order = sample_order


def privacy_schema():
    return startup.repo_module('ui_privacy_schema', 'scripts/uno_candidate_privacy.py')


def foreground_failure_record(desktop, foreground, initial, hwnd, pid, popup=None,
                              phase_index=-1, sample_order=0):
    # Fixed schema only: a relation enum, booleans and two small counters, all derived from the
    # already captured foreground HWND. No title, class string, path, HWND value or enumeration.
    user = desktop.user
    user.GetWindow.argtypes = [startup.W.HWND, startup.W.UINT]
    user.GetWindow.restype = startup.W.HWND
    user.GetAncestor.argtypes = [startup.W.HWND, startup.W.UINT]
    user.GetAncestor.restype = startup.W.HWND
    user.GetClassNameW.argtypes = [startup.W.HWND, startup.W.LPWSTR, startup.C.c_int]
    user.GetClassNameW.restype = startup.C.c_int
    user.IsWindowVisible.argtypes = [startup.W.HWND]
    user.IsWindowVisible.restype = startup.W.BOOL
    user.IsWindowEnabled.argtypes = [startup.W.HWND]
    user.IsWindowEnabled.restype = startup.W.BOOL
    fg_pid = startup.W.DWORD()
    main_pid = startup.W.DWORD()
    raw_foreground = foreground
    foreground, _ = foreground_identity.normalize(desktop, raw_foreground)
    thread = user.GetWindowThreadProcessId(raw_foreground, startup.C.byref(fg_pid)) if raw_foreground else 0
    main_thread = user.GetWindowThreadProcessId(hwnd, startup.C.byref(main_pid)) if hwnd else 0
    if not foreground:
        relation = 'none'
    elif foreground == initial[0]:
        relation = 'initial'
    elif foreground == hwnd:
        relation = 'owned_main'
    else:
        relation = 'unknown' if not thread else 'owned_other' if fg_pid.value == pid else 'external'
    live = bool(foreground and thread)
    cls = startup.C.create_unicode_buffer(256)
    class_ok = live and user.GetClassNameW(raw_foreground, cls, len(cls))
    return privacy_schema().validate_foreground_failure({
        'foregroundRelation': relation,
        'fgIsMain': bool(foreground and foreground == hwnd),
        'fgIsPopup': bool(foreground and popup is not None and foreground == popup),
        'popupPresent': popup is not None,
        'fgSamePid': bool(live and fg_pid.value == pid),
        'fgThreadIsMainThread': bool(live and main_thread and thread == main_thread),
        'fgOwnerIsMain': bool(live and user.GetWindow(raw_foreground, 4) == hwnd),  # GW_OWNER
        'fgRootOwnerIsMain': bool(live and user.GetAncestor(raw_foreground, 3) == hwnd),  # GA_ROOTOWNER
        'fgClassIs32770': bool(class_ok and cls.value == '#32770'),
        'fgVisible': bool(live and user.IsWindowVisible(raw_foreground)),
        'fgEnabled': bool(live and user.IsWindowEnabled(raw_foreground)),
        'phaseIndex': phase_index,
        'sampleOrder': sample_order})


def save_foreground_failure(private, desktop, foreground, initial, hwnd, pid, popup=None,
                            phase_index=-1, sample_order=0):
    record = foreground_failure_record(desktop, foreground, initial, hwnd, pid, popup,
                                       phase_index, sample_order)
    with (private / 'foreground-failure.json').open('x', encoding='ascii') as report:
        json.dump(record, report)


def parse_helper_failure(stderr):
    """Parse one content-free PowerShell failure line; unknown classification tokens collapse safely."""
    if stderr in (None, b''):
        return None
    if not isinstance(stderr, bytes):
        raise ValueError('ui_helper_protocol')
    match = HELPER_FAILURE_PATTERN.fullmatch(stderr)
    if match is None:
        raise ValueError('ui_helper_protocol')
    stage, reason, raw_class, raw_step, raw_hresult = (part.decode('ascii') if part is not None else None
                                                        for part in match.groups())
    allowlists = HELPER_FAILURE_ALLOWLISTS
    if stage not in allowlists['stages'] or reason not in allowlists['reasons']:
        raise ValueError('ui_helper_protocol')
    operation_class = raw_class if raw_class in allowlists['operationClasses'] else 'other'
    picker_step = raw_step if raw_step in allowlists['pickerSteps'] else 'other'
    if operation_class == 'com':
        if raw_hresult is None:
            raise ValueError('ui_helper_protocol')
    elif raw_hresult is not None:
        raise ValueError('ui_helper_protocol')
    result = {'stage': stage, 'reason': reason, 'operationClass': operation_class, 'pickerStep': picker_step}
    if raw_hresult is not None:
        result['operationHresult'] = int(raw_hresult, 16)
    return result


def validate_helper_picker_schema(private, helper_code=None, helper_stderr=None):
    path = private / 'ui-helper-picker-schema.json'
    if not os.path.lexists(path):
        return False
    def unique_fields(pairs):
        record = dict(pairs)
        if len(record) != len(pairs):
            raise ValueError('ui_picker_schema_record')
        return record
    try:
        record = json.loads(base.owned_bytes(path, private, 4096).decode('ascii'), object_pairs_hook=unique_fields)
    except (OSError, UnicodeDecodeError, json.JSONDecodeError):
        raise ValueError('ui_picker_schema_record') from None
    if (type(record) is not dict or set(record) not in ({'reason', 'matchCount', 'controlTypeId'},
                                                       {'reason', 'matchCount', 'controlTypeId', 'matches'})
            or type(record['matchCount']) is not int or type(record['controlTypeId']) is not int):
        raise ValueError('ui_picker_schema_record')
    count, kind = record['matchCount'], record['controlTypeId']
    expected = ('picker_schema_missing' if count == 0 and kind == 0 else
                'picker_schema_multiple' if count > 1 and kind == 0 else
                'picker_schema_type' if count == 1 and 50000 <= kind <= 50040 and kind != 50000 else None)
    if expected is None or record['reason'] != expected:
        raise ValueError('ui_picker_schema_record')
    matches = record.get('matches')
    if count > 1:
        if type(matches) is not list or len(matches) != count:
            raise ValueError('ui_picker_schema_record')
        for match in matches:
            if (type(match) is not dict or set(match) != {'isDirectChild', 'classToken', 'enabled', 'offscreen', 'controlTypeId'}
                    or any(type(match[key]) is not bool for key in ('isDirectChild', 'enabled', 'offscreen'))
                    or type(match['classToken']) is not str or match['classToken'] not in ('button', 'other')
                    or type(match['controlTypeId']) is not int or not 50000 <= match['controlTypeId'] <= 50040):
                raise ValueError('ui_picker_schema_record')
    elif 'matches' in record:
        raise ValueError('ui_picker_schema_record')
    if helper_code is not None:
        if helper_code == 0:
            raise ValueError('ui_picker_schema_record')  # A refusal record after success is unexpected.
        try:
            failure = parse_helper_failure(helper_stderr)
        except ValueError:
            raise ValueError('ui_picker_schema_record') from None
        stages = ('picker_import_cancel', 'picker_import', 'picker_export_cancel', 'picker_export')
        if failure is None or failure['stage'] not in stages or failure['reason'] != expected:
            raise ValueError('ui_picker_schema_record')
    return True


def validate_helper_foreground_failure(private, helper_code=None, helper_stderr=None):
    validate_helper_picker_schema(private, helper_code, helper_stderr)
    # The PowerShell observer writes the same fixed schema; refuse any other shape.
    path = private / 'ui-helper-foreground-failure.json'
    if not os.path.lexists(path):
        return False
    try:
        record = json.loads(base.owned_bytes(path, private, 4096).decode('ascii'))
    except OSError:
        raise ValueError('ui_foreground_file') from None
    except (UnicodeDecodeError, json.JSONDecodeError):
        raise ValueError('ui_foreground_schema') from None
    privacy_schema().validate_foreground_failure(record)
    return True


def normalized_snapshot(desktop):
    raw, clip = desktop.snapshot()
    foreground, child = foreground_identity.normalize(desktop, raw)
    if not foreground or (child and foreground == raw):
        raise ValueError('desktop_unavailable')
    return foreground, clip


def observe_desktop(desktop, allow_zero_fg=False):
    desktop.validate_input_desktop()
    fg, _ = foreground_identity.normalize(desktop, desktop.user.GetForegroundWindow())
    clip = desktop.user.GetClipboardSequenceNumber()
    if not clip or (not allow_zero_fg and not fg):
        raise ValueError('desktop_unavailable')
    return fg, clip


def observe_foreground_frame(desktop, hwnd, pid, initial, phase, picker_handles, owned_foreground_seen, sample_order, visible_phases=None):
    # Authenticate before sampling; if activation raced that query, authenticate once more
    # before judging the sampled HWND. Both queries preserve the same ownership/latch checks.
    popup = owned_picker(desktop, hwnd, pid, picker_handles.get(phase), sample_foreground=False) if phase else None
    if popup is not None:
        if picker_handles.setdefault(phase, popup) != popup:
            raise ForegroundChanged(desktop.user.GetForegroundWindow(), popup,
                                    PICKER_PHASES.index(phase), sample_order, 'ui_picker_replaced')
    fg, clip = observe_desktop(desktop)
    if clip != initial[1]:
        raise ValueError('clipboard_changed')
    picker_fg = picker_foreground(desktop, fg, popup, picker_handles.get(phase) if phase else None)
    allowed = (initial[0], hwnd) if not owned_foreground_seen else (hwnd,)
    if phase and popup is None and fg not in allowed and not picker_fg:
        popup = owned_picker(desktop, hwnd, pid, picker_handles.get(phase), fg)
        if popup is not None and picker_handles.setdefault(phase, popup) != popup:
            raise ForegroundChanged(desktop.user.GetForegroundWindow(), popup,
                                    PICKER_PHASES.index(phase), sample_order, 'ui_picker_replaced')
        picker_fg = picker_foreground(desktop, fg, popup, picker_handles.get(phase))
    if popup is not None and visible_phases is not None and desktop.user.IsWindowVisible(popup):
        visible_phases.add(phase)
    if fg not in allowed and not picker_fg:
        raise ForegroundChanged(fg, popup, PICKER_PHASES.index(phase) if phase else -1, sample_order)
    return owned_foreground_seen or fg == hwnd or picker_fg


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
    helper_started_at = None
    samples = 0
    helper_diagnostics_saved = False
    helper_stderr = None
    try:
        desktop.enable_dpi()
        desktop.validate_work_area(work)
        initial = normalized_snapshot(desktop)
        epoch = settle_quiet_epoch(None, desktop, initial, work)
        (private / 'fixture-launch-permit').write_bytes(b'owned_fixture_only\n')
        (private / 'fixture-rendered-ui-permit').write_bytes(b'owned_rendered_ui_019\n')
        if scene == 'folders':
            with (private / 'fixture-folder-scene-permit').open('xb') as permit:
                permit.write(b'owned_folder_ui_019\n')
        if scene == 'file-picker':
            with (private / 'fixture-file-picker-scene-permit').open('xb') as permit:
                permit.write(b'owned_file_picker_ui_019\n')
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
            helper_started_at = time.monotonic()
            helper = subprocess.Popen(command, cwd=private, stdin=subprocess.DEVNULL,
                                      stdout=subprocess.PIPE, stderr=subprocess.PIPE, creationflags=subprocess.CREATE_NO_WINDOW)
            deadline = time.monotonic() + (60 if scene == 'file-picker' else 35)
            owned_foreground_seen = False
            picker_handles = {}
            visible_phases = set()
            previous_phase = None
            while helper.poll() is None:
                samples += 1
                if process.poll() is not None:
                    raise ValueError('early_exit')
                if scene == 'file-picker':
                    # Refuse known input changes before trusting the poll-time phase marker.
                    quiet_input(desktop, epoch)
                    phase = picker_phase(private)
                else:
                    phase = None
                quiet_input(desktop, epoch, phase or 'management')
                if previous_phase is not None and phase != previous_phase and previous_phase not in visible_phases:
                    raise ValueError('ui_picker_phase')
                previous_phase = phase
                owned_foreground_seen = observe_foreground_frame(
                    desktop, hwnd, process.pid, initial, phase, picker_handles, owned_foreground_seen, samples, visible_phases)
                if any(not startup.inside(rect, work) for rect in desktop.windows(process.pid)):
                    raise ValueError('geometry')
                if time.monotonic() > deadline:
                    raise ValueError('ui_timeout')
                time.sleep(.05)
            out, err = helper.communicate(timeout=2)
            helper_stderr = err
            helper_diagnostics_saved = True
            save_helper_diagnostics(private, out, err)
            # A failed helper exit is a protocol failure; a failure file is unexpected only after success.
            validate_helper_output(helper.returncode, out, err)
            if validate_helper_foreground_failure(private, helper.returncode, helper_stderr):
                raise ValueError('ui_foreground_unexpected')
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
            if scene == 'file-picker':
                if visible_phases != set(PICKER_PHASES):
                    raise ValueError('ui_picker_phase')
                validate_picker_actions(base.owned_bytes(private / 'ui-picker-actions.receipt', private))
                if picker_phase(private) is not None:
                    raise ValueError('ui_picker_phase')
                for phase in PICKER_PHASES:
                    if not os.path.lexists(private / ('picker-' + phase + '.closed')):
                        raise ValueError('ui_picker_phase')
                    validate_picker_phase_actions(phase, base.owned_bytes(private / ('picker-' + phase + '.actions'), private))
                files = private / 'fixture-profile' / 'picker-files'
                base.owned_bytes(files / 'import.json', private, 4096)
                base.owned_bytes(files / 'export.json', private, 16384)
            # Release shutdown only after helper exit and every actual callback postcondition.
            quiet_input(desktop, epoch, 'verification')
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
                quiet_input(desktop, epoch, 'shutdown')
                closed_seen = shutdown_frame(desktop, process.pid, hwnd, initial, work, closed_seen,
                                             owned_foreground_seen, ui.visible_windows)
                if time.monotonic() > deadline:
                    raise ValueError('graceful_timeout')
                time.sleep(.05)
            save_fixture_exit(private, process.returncode)
            base.require_graceful(process, private)
            quiet_input(desktop, epoch, 'shutdown')
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
                        'ui019 synthetic renamed', 'ui019 synthetic attached', 'ui019 synthetic discarded',
                        'ui019 synthetic picker', 'ui019 picker label', 'ui019 picker folder',
                        'picker-tag', 'picker-files', 'import.json', 'export.json', str(private))
            privacy.verify_negative_controls(canaries)
            records = [base.owned_bytes(p, private) for p in
                       (private / 'stdout.log', private / 'stderr.log', private / 'fixture-profile/logs/app.log',
                        private / 'ui-helper-stdout.log', private / 'ui-helper-stderr.log')]
            if privacy.scan_evidence(records, canaries)['privacy'] != 'CLEAN':
                raise ValueError('privacy')
    except BaseException as error:
        # Preserve the observer's own monotonic timing and poll count, independent of helper records.
        if isinstance(error, InputRefused) and helper_started_at is not None:
            try:
                error.input_refusal_elapsed_ms = max(0, int((time.monotonic() - helper_started_at) * 1000))
                error.input_sample_count = samples
            except Exception:
                pass
        # Diagnostics errors (observer record or helper output) never skip failure-file
        # validation or failure recording.
        diagnostics_error = None
        if isinstance(error, ForegroundChanged) and process is not None:
            try:
                save_foreground_failure(private, desktop, error.foreground, initial, hwnd, process.pid,
                                        error.popup, error.phase_index, error.sample_order)
            except (OSError, ValueError) as caught:
                diagnostics_error = caught
        # Reap the writer before inspecting its evidence, including failures mid-frame.
        if helper is not None:
            try:
                if helper.poll() is None:
                    startup.stop_app(helper)
                if not helper_diagnostics_saved:
                    helper_diagnostics_saved = True
                    out, err = helper.communicate(timeout=2)
                    helper_stderr = err
                    save_helper_diagnostics(private, out, err)
            except (OSError, ValueError, subprocess.TimeoutExpired) as caught:
                diagnostics_error = caught
        helper_failure = None
        try:
            helper_failure = parse_helper_failure(helper_stderr)
        except ValueError:
            # Preserve the original observer/helper error if stderr is not our fixed schema.
            helper_failure = None
        try:
            validate_helper_foreground_failure(private, helper.returncode if helper is not None else None, helper_stderr)
        except ValueError as schema_error:
            error = schema_error
        timeline = parse_picker_epoch_timeline(private)
        if timeline is not None:
            error.picker_epoch_timeline = timeline
        base.record_failure(private, error, helper_failure=helper_failure)
        if diagnostics_error is not None:
            raise error from diagnostics_error
        raise error
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
        out, err = process.communicate(startup.PERMIT, timeout=95)
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
    parser.add_argument('--scene', choices=('snippets', 'folders', 'settings', 'file-picker'), default='snippets')
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
            'filePicker': 'OBSERVED' if args.scene == 'file-picker' else 'NOT_RUN',
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
