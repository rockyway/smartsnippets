# 053 — nd-06: Target018 other-window classifier and picker Invoke epochs

Scope: static analysis of research 050–052, Microsoft documentation, behavioral
stub tests and strict build-only verification. **No screen, application/native
scene execution, clipboard access or input synthesis. All review gates stay false.**
Plans 018/019 remain incomplete. **Diagnostics only: no defensible exact Target018
activation fix or picker epoch exception is established.**

## fg-01: nr-12 root-identity correction (headless/build-only)

The later nr-12 / 19:14 / nd-07 inventory analysis identifies the defect class:

> The "same_pid_other" window that "steals" foreground right after
> OwnedTargetPreparation's UIA SetFocus is Target018 A's own CHILD EDIT control:
> class Edit (system_0, sha8 fd2fa13c), ws_child=1, same thread as A, visible,
> on the bottom-left work area. Thread inventories at ready and refusal contain
> only A, B, the IME window and a NEW msctfime_ui (zero-area IME window); there
> is no other top-level window. Five of six positives exhibit this pattern;
> posted-row passed once foreground came back as A itself.

This supersedes the earlier inference that the sampled different HWND was a
different top-level. Acceptance foreground identity is now GA_ROOT (2) **only
for WS_CHILD**, otherwise the raw HWND. Raw-child and root PID/thread must agree
and be nonzero; exact retained top-level pins and all existing admission guards
still apply. A different same-PID top-level remains forbidden. Owners are never
followed. The fixture, acceptance adapters, Target018 focus/grant receipts and
independent Python/PowerShell observers use this identity convention.

Raw window facts remain raw: `foreground_was_child` is a fixed boolean in trace
facts, windowFacts and the latched monitor record, not permission or an extra
relation token. No input epoch exception, focus retry, ordinary product focus,
PopupShowCoordinator, capture/paste or review gate change accompanies this fix.
Headless regression/build success is not native nonactivation/input acceptance;
a fresh separately authorized scene and fresh pinned artifacts remain necessary.

## nd-07 diagnostic extension (build-only; no new native evidence)

The fixture best-effort snapshots A's EnumThreadWindows inventory at target
readiness, before host startup or UIA SetFocus; failure does not gate the
`fixture_owned` handoff. For positive runs only, after `positive-surfaces.txt` is
published, the observer snapshots it at the first subsequent latched foreground
refusal, alongside native-monitor-failure.json. Refusals before publication and
non-positive scenes do not write this inventory. See the fixture README for exact
fixed schemas, class_sha8, allowlist provenance and new-at-refusal correlation
limits. Extended class/style/rectangle facts also accompany startup and high-rate
trace samples. These independent observers still do not establish
continuous HWND identity or causality. A new-window marker is a diagnostic lead,
not proof of creation origin, activation acceptance, or an allowed surface.
No review gate or product behavior changed; the unknown window has not yet been
identified by a new native run.

## What nr-10 establishes (and does not)

Research 052's posted-row trace sees target-PID `a_other_window/toplevel_other`
at 0ms, then exact A at 14–240ms. Five later independent scenes see A for 1–3
samples, then target-PID other through the end of the 250ms interval. The independent
monitor's failed reads distinguish both pinned A and B before `same_pid_other`.
Thus its failed HWND was neither A nor B. The old high-rate trace did not distinguish
B, and the two observers sample independently: they do not prove the same third
HWND existed throughout, or that the successful scene's transient was the same
kind of window as the later failures. `toplevel_other` was a fallback, not a
verified top-level/class/visibility assertion.

A was activated transiently in the failures. The unchanged exact-A predicate
refused later; the trace does not mean UIA never focused A, or that a Windows
foreground-lock timer automatically reclaimed it. Original refusal remains latched.

## Static window inventory and foreground plausibility

The owned source is `prototypes/uno-native-target/windows/Program.cs`:
`Run` creates precisely A and B using one per-run registered class; `CreateTarget`
creates one standard EDIT child each. Neither has an owner, TOOLWINDOW or
NOACTIVATE extended style. Geometry changes use SWP_NOACTIVATE, and each is shown
with SW_SHOWNOACTIVATE. The target is WinExe, with no console window requested.
`WM_SETFOCUS` calls SetFocus on the existing EDIT; there is no tracked tooltip,
menu, custom dialog or third top-level CreateWindow path. Commands are posted from
the stdin reader to the existing UI thread; `focus_a` runs only after the failing
preparation check. No source change to the prototype is justified by this inventory.

| Window family | Can exist here? | Foreground interpretation |
|---|---|---|
| A and B (`SS_Target018_<run>`) | Explicitly created/shown, same GUI thread, each with EDIT child. | Both are ordinary activatable top-levels. SW_SHOWNOACTIVATE controls that show call, not lifetime eligibility. B remains forbidden, even if focus somehow reaches it. Monitor already excluded B at its failed read. |
| EDIT children | Explicitly created. | Not an alternative foreground top-level identity. SetFocus activates the receiving window or its parent [1]; focused child and foreground top-level must not be conflated. |
| Default IME (`IME`), TSF/IME UI (`MSCTFIME UI`) | Windows creates a default IME window per thread [2]; text input can involve OS/provider helpers. | Default helper existence is not proof of a shown foreground surface. An interactive IME UI may participate in activation; neither it nor a same-PID helper is A. The old classifier recognized these exact two names and did not see them at sampled times. |
| COM STA hidden window (`OleMainThreadWndClass`) | COM documents one hidden dispatch window per initialized STA [3]. Target Main has STAThread; COM/provider initialization is not exhaustively observable from its explicit CreateWindow calls. Client-side UIA COM windows would instead belong to the worker PID. | Normally hidden dispatch machinery, not a displayed application window. No documented requirement that it take foreground upon editor activation. Same-PID observation alone does not establish this class or identify which component initialized COM. |
| UIA/TSF implementation helpers (`UserAdapterWindowClass`, `CicMarshalWndClass`) | Possible implementation-created helpers during accessibility/text-services work; not explicitly created by Target018. Exact class names/lifecycle are not public contracts found in the Microsoft references. | Class tokens are investigative labels, not proof of creator, visibility or authority. Demand initialization after focus is plausible, not demonstrated. Cannot safely patch, disable, restyle or reorder an unidentified system/provider window. |
| Tooltips, menus (`#32768`), dialogs (`#32770`) | No explicit target creation; system/provider code can create windows beyond the two application calls. | TOOLWINDOW only changes presentation/taskbar/Alt+Tab behavior, not a blanket activation prohibition [4]. A visible dialog/menu can affect activation; hidden helpers ordinarily do not. Old trace excluded the exact tooltip name but not every possible provider class. |
| Message-only windows | Possible subsystem dispatch infrastructure. | Documented invisible, no Z-order, not normal enumerated desktop surfaces [5]; not a legitimate replacement foreground editor. HWND_MESSAGE is a lookup sentinel; classifier uses FindWindowEx(HWND_MESSAGE), not a guessed raw parent/class name. |
| DWM/ghost windows | Windows documents replacing a hung top-level with a ghost after several seconds of nonresponse [6]. | A ghost can support interaction while the real app is hung. No hung-target evidence, several-second delay, replacement relationship or ownership proof appears here. The 10–36ms transition alone is not evidence of ghosting. Do not assume the ghost's PID from its appearance; a DWM-owned surface would not meet target-PID equality. |
| .NET/Native AOT runtime windows | No managed UI framework/timer/native message-window creation in this target beyond the above source. Runtime/provider internals are not an exhaustive stable class inventory. | Native AOT and WinExe are not guarantees against subsystem windows. There is no documented requirement that a CLR/AOT helper become foreground after UIA focus, and no basis to blame or suppress one. |

[ShowWindow][7] also documents a first-call STARTUPINFO override, so the literal
SW_SHOWNOACTIVATE argument is not alone proof of startup nonactivation under every
launcher. nr-10's startup observations saw no such activation. That possibility
does not explain a third HWND appearing after focus or justify a speculative fix.

### Why after activation; why opposite order?

[SetFocus][1] changes focus, activates a window/parent and sends focus messages.
UIA behavior depends on the provider [8]. Text-services/accessibility work can
therefore be triggered by the transition rather than startup. COM dispatch and
posted messages can finish after the synchronous call. These are mechanisms
consistent with the ordering, **not identification of the activating caller**.

[SetForegroundWindow][9] documents foreground eligibility/lock restrictions and
that even eligible requests may be denied; it does not document a 15/45ms
foreground-reclaim rule. Each scene starts a fresh Target018: first-versus-later
scene order is not cumulative state in that one target. Shared subsystem/provider
warmup, scheduler/message interleaving, different prior foreground history or two
different transient windows could yield opposite sample order. None is established.
The first trace itself could miss an earlier A transition. We cannot deduce a
deterministic OS cause or a safe delay/retry from these snapshots.

Consequently no helper is disabled/restyled, no speculative precreation is added,
no focus retry/settling exception exists, and no non-A surface is admitted as A.

## Implemented fixed classifier

`WindowDiagnosticFacts` reduces class names to exactly:
`ime_default`, `msctfime_ui`, `ole_main_thread`, `cicmarshal`, `user_adapter`,
`tooltip`, `dialog_32770`, `menu_32768`, `target018_class`, `other`.
The target token requires the exact class prefix plus 32 hexadecimal GUID digits;
a prefix alone cannot smuggle a private string. Even a wrong adapter token is
reduced to `other` before logging. No raw class, title or PID is emitted. The
inventory includes a truncated SHA-256 of the decimal HWND for within-run correlation
only; it is not anonymization or identity authority.

Both `native_startup_foreground` and every `owned_foreground_trace` sample append
booleans `visible`, `toolwindow` (WS_EX_TOOLWINDOW), `noactivate` (WS_EX_NOACTIVATE),
`has_owner` (GW_OWNER), `owner_is_a`, `owner_is_b`, `is_message_only`,
`same_thread_as_a`, `cloaked` (DWMWA_CLOAKED). `cloaked_known` distinguishes a DWM
query failure from a known zero [10]. Nonzero cloak bitmasks count as true. B now
has its own `owned_b` trace relation. The existing coarse `kind` remains for
continuity; it is not permission.

The independent Python monitor uses the same fixed mapping/flags, in required
`windowFacts` on its failure record. Its `owned_a`/`owned_b` relation labels match
NativeControlDiagnostics; all existing exact identity/order/popup-grant rules are
unchanged. For positive runs, it captures facts at the first foreground refusal
after surfaces are published, not at teardown; non-positive runs do not write the
refusal inventory. `sampled=true` means both A and B handles were known; `sampled=false` does not mean
all fact collection failed. If A or its rect/monitor lookup is unavailable, class
and style facts remain; `equals_a_rect`/`on_bottom_left_work` stay false, while
`zero_area`/`offscreen` use the sampled window's own rect when available. Other
facts retain their independent probes. Unknown facts are not negative runtime
evidence. Validator rejects extra fields, arbitrary tokens, duplicate JSON keys and
non-boolean flags. Read failures do not erase the original monitor refusal or cause
a retry.

These are multiple read-only API calls around one captured foreground HWND, not
an atomic OS snapshot. Destruction/reuse can race collection. Message-only lookup
is bounded to 256 children, with false also possible if the cap is exhausted;
this is diagnostic, never authority. Added sampling can perturb scheduling and
reduce the number of samples inside the unchanged time/count bounds. No continuous
coverage, exact provider identification or causal caller attribution is claimed.

## Picker: epoch 5875 / idle 31 during Import Cancel

[GetLastInputInfo][11] returns session last-input time, not an event counter or
human-only source. Microsoft explicitly warns the timestamp need not increase
monotonically (raw-input/desktop-thread timing gaps and SendInput-supplied times).
`inputEpochDelta=5875` is unsigned timestamp subtraction; `inputIdleMs=31` is
current-tick subtraction. It places a timestamp near the check, not proof that a
physical user acted exactly 31ms earlier.

[UIA Invoke implementation guidance][12] permits actions/events associated with
invocation and discusses asynchronous behavior/input simulation. It does **not**
guarantee that Invoke uses SendInput or updates LASTINPUTINFO. The native dialog
provider may dispatch an action directly or employ implementation-specific
mechanisms. Dialog creation/destruction and focus restoration alter window/focus
state [1,13], but their public contracts do not require an input epoch update.
A provider/system injected input event is possible even with an idle operator;
a delayed unrelated event is also possible. The active Import Cancel marker
starts before opening the picker and remains active through Cancel/closure,
so nr-10 cannot place the epoch change at Cancel rather than open, wait or restore.
No defensible source-specific exception, delta allowance or rebaseline follows.

The helper now brackets both opening and commit/Cancel Invoke with its own
GetLastInputInfo reads. Fixed owner-private files
`picker-invoke-<phase>-<open|commit>.json` contain only uint32 `before`, uint32
`after`, boolean `completed`. Four fixed phases and two fixed actions bound this
to eight exclusive-create records. `finally` samples/persists after a throwing
Invoke too. The original immutable epoch and every guard remain intact; no
additional Invoke, input synthesis, UI discovery or desktop run was introduced.

A difference inside a bracket pins temporal coincidence, not origin; asynchronous
provider/input delivery after return can occur outside it. If the independent
observer refuses/kills the helper before the bracket completes, a missing record
is incomplete evidence, not unchanged input. Compare available open/commit records
with worker failure fields in a future separately authorized trial. Reading the
epoch is itself read-only. Persistence/query failures fail closed, never grant
permission to proceed.

## Verification

Each requested full suite was run once, sequentially:

| Suite | Result |
|---|---|
| Native fixture (`dotnet test scripts/uno_candidate_native_fixture/tests -c Release -p:NoWarn=`) | 221 PASS, zero warnings |
| Target018 identity (`dotnet test prototypes/uno-native-target/identity-tests -c Release -p:NoWarn=`) | 86 PASS, zero warnings |
| Candidate (from `src/SmartSnippets.Uno`, prescribed command) | 348 PASS, zero warnings |
| Python (`python -B -m unittest discover -s scripts/tests -p "test_*.py"`) | 221 total: 219 PASS initially; two stale source-spelling expectations failed after Invoke was routed through the measurement wrapper |

Updated those two source supplements to the actual measured call site and reran
**only those two tests**: both PASS. No redundant full-suite rerun. New behavioral
C#/Python tests passed on the first suite runs: fixed class mapping/privacy,
positive/negative styles/owner/thread/cloak, B distinction in startup/trace,
first-refusal facts persistence/strict schema, actual PowerShell measurement with
stubbed epoch and single/throwing Invoke, no rebaseline. Wiring text checks remain
explicit supplements, not native behavior proof.

Final `pwsh -NoProfile -File scripts/uno_candidate_native_fixture/build.ps1 -BuildOnly`
PASS: fresh native x64 strict AOT/trim/reference/SQLite/provenance gate, zero
warnings; token `6b773f9b32f54248ac1fc14a8b6f228c`, SHA256
`69c427818497c0853d832a5ec1ba447ad0a98b30a2858dd8d0bef8765e09798b`.
No warning suppression, product change, gate opening or runtime acceptance claim.
Operator-local AGENTS/safety-guide edits are preserved and excluded from commit.

## Microsoft references (web-checked)

[1]: https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setfocus
[2]: https://learn.microsoft.com/en-us/windows/win32/api/immdev/nf-immdev-immgetdefaultimewnd
[3]: https://learn.microsoft.com/en-us/windows/win32/com/single-threaded-apartments
[4]: https://learn.microsoft.com/en-us/windows/win32/winmsg/extended-window-styles
[5]: https://learn.microsoft.com/en-us/windows/win32/winmsg/window-features
[6]: https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getmessage
[7]: https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-showwindow
[8]: https://learn.microsoft.com/en-us/dotnet/api/system.windows.automation.automationelement.setfocus
[9]: https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setforegroundwindow
[10]: https://learn.microsoft.com/en-us/windows/win32/api/dwmapi/ne-dwmapi-dwmwindowattribute
[11]: https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getlastinputinfo
[12]: https://learn.microsoft.com/en-us/dotnet/framework/ui-automation/implementing-the-ui-automation-invoke-control-pattern
[13]: https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-destroywindow
