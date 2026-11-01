# 051 — nd-05: owned foreground loss and picker epoch analysis

Scope: static investigation of research 048–050, behavioral stub tests and strict
build-only verification. **No desktop/native execution, screen, clipboard access,
input synthesis or review-gate opening.** Plans 018/019 remain incomplete.

## Finding: transient activation is proven; the thief is not

Research 050's first positive (`posted-row`) sampled `after_focus=none`, then
Prepared/granted/Inserted. All five later positives sampled `after_focus=owned_a`,
then ForegroundRefused about 43–49ms later, before the grant. The causal predicate
is exact foreground inequality, not necessarily Windows refusing the UIA call.
The records do not identify a single static root cause for the first/later split.
Treating scene order as a deterministic foreground-lock defect would invent evidence.
The safe implementation choice is the brief's diagnostic alternative, not a waiver.

### Static chain and exclusions (current-source line references)

1. `scripts/uno_candidate_native_fixture/App.xaml.cs:43-58` snapshots foreground,
   launches the nonactivating target and refuses a launch change. Target creation
   makes A and B once (`prototypes/uno-native-target/windows/Program.cs:133-134`);
   each has one standard EDIT child (`:233-250`), no tooltip/menu/custom popup.
   `WindowProcedure` handles size, focus and destruction (`:267-280`). Its only
   focus action is SetFocus on the **existing child** (`:272-276`), not creation or
   activation of another top-level window. No custom editor focus handler exists.
   System EDIT/IME/default-provider behavior is outside this source: it is not
   statically excluded by the absence of an explicit CreateWindow in our handler.
2. Preparation runs only after guardian-ready, authenticated surface publication
   and coordinator lease (`Coordinator.cs:64-83`, `PositiveHostScene.cs:32-55`, both
   under `scripts/uno_candidate_native_fixture/`). Exactly one native UIA editor
   resolution/focus occurs (`OwnedTargetPreparation.cs:70-80`); COM vtable slots
   are ElementFromHandle 6 and SetFocus 3 (`OwnedPreparationAutomation.cs:21-30`).
   ElementFromHandle may execute provider code; the pre-focus checks are repeated.
   The synchronous client makes no explicit PostMessage/activation call, but COM/
   provider work can marshal calls and process messages. Synchronous HRESULT
   success is not an atomic, lasting foreground guarantee.
3. `after_focus` is a distinct foreground read, followed by Guard and another read
   (`OwnedTargetPreparation.cs:80-89`). Guard (`:35`) checks permission, identity and
   desktop; native implementations (`OwnedTargetPreparationNative.cs:33-54`) use
   session validation and read-only window/process/class/desktop checks. Focus-only
   validation (`src/SmartSnippets.Uno/Platforms/Windows/AcceptanceNative/NativeAcceptanceSession.cs:56-74`)
   does **not** call RestoreFocus/TryFocusPopup. Probe checks are read-only
   (`prototypes/uno-native-target/identity/OwnedTargetProbe.cs:41-52`;
   `WindowsWindowIdentity.cs:16-35`). These checks can consume time; they contain
   no activation request that explains the loss. COM Release is later, on leaving
   the using block (`OwnedPreparationAutomation.cs:31-42`), so cannot cause the
   already-determined ForegroundRefused. The owned `focus_a` command is later still
   (`OwnedTargetPreparation.cs:114`); it cannot cause this failure either.
4. `same_pid_other` is relative to **Target018**, NOT worker or guardian. The
   authenticated tuple is target/worker/guardian (`Coordinator.cs:75-77`), consumed
   in that order by `scripts/run_uno_candidate_native.py:283-289`. Exact A and B
   comparisons precede the PID-only fallback. Thus it means a third HWND in the
   current target process, not B, and not an old scene's guardian owner window.
   Two nr-09 failures had this relation, two foreign, one none; neither relation
   identifies window class, causality or the exact worker predicate's timestamp.
5. Coordinator starts a no-console worker (`Coordinator.cs:29-36`) and waits on
   pipes/exit; it has no window-activation path. Guardian creates a message-only
   clipboard owner (`prototypes/uno-candidate-native/Program.cs:78`,
   `prototypes/uno-native-target/windows-insertion/GuardianWindow.cs:37`),
   not a shown foreground surface. Its second owner exists only on the controls'
   explicit refusal command (`Program.cs:112-122`). Worker popup activation needs
   the **later** target grant and ready popup (`PositiveHostScene.cs:176-205`);
   failed preparation never reaches it. The Python observer reads foreground
   (`scripts/run_uno_candidate_native.py:322-369`), not restores it. No tracked
   parent/coordinator/guardian instruction explains a competing activation here.
6. Previous positive teardown closes the host, publishes closure, disposes session
   and target (`App.xaml.cs:93-103`); coordinator waits for worker/target/guardian
   completion (`Coordinator.cs:94-100`). Target lease disposal kills only its own
   retained child (`prototypes/uno-native-target/identity/NativeChild.cs:106-121`).
   Research 050 additionally reports zero survivors between scenes: a lingering
   previous-scene owned process is not supported. Teardown can change which foreign
   window is active/eligible before a later scene, but all five monitor failures
   have `closed=false`, not evidence of current-scene teardown. The untracked shell
   or another foreign process may reactivate; neither its identity nor the request
   appears in existing logs. The original snapshot alone does not identify a shell.

### Microsoft semantics (web-checked)

- [Native UIA SetFocus](https://learn.microsoft.com/en-us/windows/win32/api/uiautomationclient/nf-uiautomationclient-iuiautomationelement-setfocus)
  sets keyboard focus. The [managed UIA remarks](https://learn.microsoft.com/en-us/dotnet/api/system.windows.automation.automationelement.setfocus)
  explicitly make behavior provider-dependent; setting focus need not bring an
  element to foreground/visibility. Do not turn either API into a durable grant.
- [Win32 SetFocus](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setfocus)
  activates the receiving window or parent, sends WM_KILLFOCUS/WM_SETFOCUS, and can
  therefore cause provider/window-procedure side effects.
  [WM_KILLFOCUS](https://learn.microsoft.com/en-us/windows/win32/inputdev/wm-killfocus)
  warns against displaying/activating another window while processing it. This
  makes a competing handler possible in general, not proven in our target.
- [SetForegroundWindow](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setforegroundwindow)
  restricts activation via foreground locks, active menus and eligibility such as
  last input, foreground ancestry, no foreground, debugging or elapsed foreground
  lock timeout. Even satisfying conditions need not guarantee success. Another
  eligible process, user input, or activation/destruction side effects can change
  foreground. Timeout is an eligibility condition, **not documentation of a 45ms
  timer that automatically gives foreground back to the shell**.
- [GetForegroundWindow](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getforegroundwindow)
  can return NULL while losing activation. [DestroyWindow](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-destroywindow)
  deactivates/removes focus; it does not promise restoration to our snapshot.

The first positive passed because its later exact-A checks, quiet/session checks
and grant succeeded, despite its earlier transient zero sample. Later positives
failed because A was no longer foreground at the post-Guard check. **Why** that
transition differed is unresolved; no static source or Microsoft timing contract
justifies attributing it to a foreground-lock timeout, UIA failure or user activity.

## Implemented diagnostic alternative; no new admission rule

`OwnedForegroundTrace` (`NativeStartupDiagnostics.cs:22-54`) records one foreground
read/sample, up to 51 samples, requesting 5ms delays for 250ms after SetFocus.
First sample is synchronous; later continuations run off the UI context. Buffering
avoids high-rate disk logging, and finally-await completes logging before COM
release/scene teardown, including immediate refusal. Actual elapsed milliseconds
and order are retained; Windows scheduling can skip intervals and sub-sample
transitions remain invisible. Count cap also bounds a stalled fake clock.

Relations: owned_a, a_other_window, worker, guardian, coordinator, launcher, foreign,
none. For A's other HWND, fixed kind: ime (IME/MSCTFIME UI), tooltip
(tooltips_class32), owned_by_a (direct GW_OWNER=A), otherwise toplevel_other.
Classification adapters are `OwnedTargetPreparationNative.cs:12-32`. No raw class,
PID/HWND, title, content or path is emitted. `initial_foreground` identifies return
to the original snapshot without claiming that it belongs to the shell. Launcher
means coordinator's direct parent; coordinator/worker are separately classified.
Historical nd-04 startup `launcher_foreground` actually tested worker PID; nd-05
corrects that wiring (`App.xaml.cs:26-28`). Historical false values must not be
reinterpreted as observations of the trusted Python parent.

The original refusal is latched before awaiting the trace (`OwnedTargetPreparation.cs:84-96`).
A later return to A cannot rescue it. A successful immediate check still goes
through every original subsequent guard. The post-focus epoch is frozen before
awaiting diagnostics and must remain equal afterward (`:90-104`), so the extra
wait cannot expand the existing UIA epoch-change allowance. No input synthesis,
ASFW_ANY, AttachThreadInput, foreign manipulation or foreign admission was added.

An A-PID IME/tooltip/owned window is **not** the exact captured editor/A identity.
Ownership alone does not prove input goes to A or that the surface is benign.
There is no defensible static rule to accept it or wait/rebaseline and then admit.
The 250ms interval is observation only, not settlement authority. On the next
separately authorized run, correlate kind/relation/initial/order with the independent
monitor and unchanged failure. Even this cannot identify the caller of an external
activation or prove completeness between samples; further instrumentation may be
needed if only foreign/none is observed.

## Picker epoch_changed

The Python immutable epoch is LASTINPUTINFO.dwTime, not clipboard sequence and
not a count of physical events (`scripts/run_uno_candidate_ui.py:209-234`). After
startup-only settling, it is fixed before helper launch (`:540-545`). Held keys,
any epoch difference, or insufficient idle still refuse. Worker checks continue
through helper observation, verification and shutdown (`:558`, `:604-623`).
PowerShell separately checks the same timestamp (`scripts/uno_candidate_ui.ps1:71-78,174`).
Import Cancel marks active **before** opening invocation (`:440-442`); the dialog
Cancel Invoke occurs later (`:507-510`), followed by closed marker (`:520`). The
nr-09 active-only record cannot place the change before/after creation or Cancel.

[GetLastInputInfo](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getlastinputinfo)
reports session input, not exclusively human input, and explicitly need not be
monotonically increasing: raw-input/desktop timing and SendInput-supplied timestamps
can produce backward values. UIA Invoke is a provider method, not documented here
as a mandatory SendInput/last-input update; neither dialog creation nor UIA alone
proves the timestamp must change. System/provider-injected events and asynchronous
input delivery cannot be distinguished from real input using this API. Operator
idle is compatible with an epoch change, but does not establish its source.

No tolerated delta, backward-time exception or system-event rebaseline is safe
statically. The worker now adds unsigned inputEpochDelta, inputIdleMs and inputPhase
(current fixed picker marker or management/verification/shutdown/unspecified).
Persistence strictly allowlists phases and uint32 integers, rejecting bools/free
text (`scripts/run_uno_candidate_acceptance.py:350-360`). Delta is timestamp
subtraction modulo 2^32, not an event count; wrap/backward cases remain refusals.
Phase identifies the observed marker, not proof that Invoke completed. This is the
brief's sub-diagnostic alternative, not a claimed picker fix or acceptance.

## Verification and limits

Final sequential verification (all PASS, zero warnings):

1. Native fixture tests: **206**.
2. Target018 identity tests: **86**.
3. Candidate tests from pinned Uno cwd: **348**.
4. Python tests: **216**.
5. `pwsh -NoProfile -File scripts/uno_candidate_native_fixture/build.ps1 -BuildOnly`:
   fresh native x64 strict gate PASS, token `a926966a19a347afaff9f818b652dea4`,
   SHA256 `669c22b43d5922d954e3639fb2fbd5fa1c9ff6e2a080650fa6ad3498723e1123`.

Behavioral stubs cover all trace relations, wrong-PID A, allowlisted kinds and
invalid class privacy, time/count bounds, foreground changes across samples,
trace-before-disposal ordering, latched foreground/identity refusal despite recovery,
post-trace foreground/epoch drift refusal, unsigned epoch forward/backward/wrap,
actual picker phase tokens, persisted phase/delta and hostile metadata rejection.
No gate changed, warning suppressed, app launched or clipboard touched. These
results do not resolve the runtime thief or establish native/picker acceptance.
