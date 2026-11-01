# Internal native fixture — SOURCE REVIEW CLOSED / NOT RUN

This is a separate test assembly linking the **actual UnoAppHost, native boundary,
XAML, Core store and Presentation VMs**. Its required basename is
`SmartSnippets.UnoCandidate.exe` because the independent guardian pins that name.
It is NOT the production candidate: AppId `org.smartsnippets.unonativefixture019`,
instance prefix `SmartSnippets.UnoNativeFixture019`, fixed fresh
`native-fixture-profile` beneath an owner-created private working directory.
There is no default-profile fallback, profile argument or environment unlock.

`NativeFixtureSourceReviewed`, Python `NATIVE_SCENES_SOURCE_REVIEWED` and adapter
`NativeAcceptanceFactory.SourceReviewed` are all false. Positive orchestration and
native input additionally require `PositiveProtocol.SourceReviewed` /
`InputSourceReviewed` and Python `POSITIVE_SCENES_SOURCE_REVIEWED` /
`POSITIVE_INPUT_SOURCE_REVIEWED`, also false. Building/testing does not
clear review or authorize any native launch. Parent-owned adapter dependency is
050a102 (locally cherry-picked as ae3ff48); no adapter/production file is authored
by this fixture batch.

## Build only

From repository root:

```
pwsh -NoProfile -File scripts/uno_candidate_native_fixture/build.ps1 -BuildOnly
python -m unittest discover -s scripts/tests -p test_uno_candidate_native.py
```

From this directory (SDK 10.0.403):

```
dotnet test tests/SmartSnippets.Uno.NativeFixture.Tests.csproj -c Release -p:NoWarn=
```

Run .NET suites/publishes sequentially (shared Core output). Build wrapper creates
an ACL-private unique output before any compiler/property evidence. It uses the
existing strict candidate property/artifact/reference gate, adapting only fixture
project spelling and final RuntimeIdentifiers. The source fingerprint includes
linked Core/Presentation/head/adapter/identity/guardian sources and bootstrap.
Fresh native PE/hash, no managed entry, strict warnings and source drift are checked.
The target and guardian remain separately built, native/hash-pinned operator
artifacts under their own prototype bin directories; this does not attest an
untrusted compiler or replace independent review of their exact source/build hashes.

## Topology and scenes

The Python runner starts this executable in coordinator mode **without any Job or
outer timeout-kill**. Both coordinator and worker verify no pre-existing Job. A
private inherited command/response pipe and retained inherited coordinator process
handle bind the worker to its direct parent, same exact native image and user/token.
The worker directly starts Target018. The target source uses
SW_SHOWNOACTIVATE/SetWindowPos no-activate; the worker and external observer also
check foreground, and refuse any observed change. No focus or input call is used
to make a control admissible. There is no clipboard sequence-number dependency.

1. Worker transfers its retained direct target handle (query/synchronize plus
   assignment/termination rights) to the coordinator through that private pipe.
2. Before host StartAsync it constructs a zero-positive-permission capability over
   the late broker, bound to exact owned A HWND/PID. The actual host initializes its
   real store/VMs and creates/prewarms the actual popup, still denied.
3. Factory uses the actual popup NativeHandle and starts the independent guardian
   **outside all Jobs**. The only factory permissions are identity-only Capture and,
   after explicit lease, the distinct ClipboardControl operation. No Insert, Copy,
   Focus, Input, Shortcut or clipboard-loss permission is granted.
4. Only after authenticated guardian-ready does the coordinator receive the
   read-only retained guardian handle, verify hashes/direct-parent provenance and
   guardian-outside-all-Jobs, and assign **only worker + owned target** to its Job.
   It rechecks guardian placement immediately before granting the explicit control
   lease. No arbitrary PID adoption, process-tree kill or descendant enumeration.
5. `restore`: fixed synthetic publication must return Swapped + RecoveryPending;
   explicit restore calls session DrainOwnedRecoveryAsync (even with no local VM
   snapshots). Only a restored receipt, worker/target exit and retained guardian
   zero exit establish settlement.
6. `abort`: the same post-publication acknowledgement precedes terminating only the
   worker Job. Guardian must independently survive and run its known RAM-backup
   recovery loop after retained worker HANDLE death. Exit zero counts only for the
   exact pinned helper/source whose zero exit follows backup settlement. Nonzero or
   unknown exit is never a pass.

A pending guardian is never killed or Job-assigned. The coordinator retains its
session/handle indefinitely, emits RECOVERY_PENDING once, and waits; the guardian
retains RAM and its existing current-user-only retry endpoint. There is no automatic
retry that overwrites a foreign clipboard owner, disk recovery snapshot or forced
pending cleanup. Do not wrap this runner/coordinator in an external watchdog or Job.
There is deliberately no bounded *recovery lifetime*; protocol/worker IO is bounded.

After independent review and exclusive operator slot authorization only, runner
arguments are `--executable`, `--sha256`, `--target`, `--target-sha256`, `--guardian`,
`--guardian-sha256`, `--authorized-screen-slot`, `--scene restore|abort`. Execute the
regular-restore control before the abort control. The runner creates the private
root via the existing owner-private directory helper, retains content-free
phase/result/stdout/stderr evidence, and never reads/persists original clipboard
representations. Foreground/input-desktop observation runs throughout. A monitor
failure records failure but never kills coordinator/guardian. No native command
has been executed for this batch.

## Positive modes: implemented, still review-gated / NOT RUN

After independent source review, parent acceptance of **both ordinary restore and
worker-abort controls for the exact source/artifacts**, and a new exclusive operator
slot, positive runner modes are `registered-enter`, `registered-row`, `registered-main`, `posted-enter`, `posted-row`, `posted-main`, `copy`,
`popup-copy`, `editor-copy`, `footer-copy`, `context-insert`, `preview`, `expired`. They additionally require `--row6-controls-verified` and
`--operator-recopied-baseline`. These are explicit operator attestations, not proof
of the controls or baseline contents. The private launch permit has a distinct exact
positive token; ordinary controls keep their original pipe/permit/receipt protocol.

**Before EVERY session, manually freshly copy a harmless supported baseline** from
a safe application, including between marked restore/abort/positive sessions. The
marker left by a previous session must not become another baseline. Never clear the
clipboard automatically, reuse a marked baseline, or run a batch unattended. One
transaction/session, no arbitrary clipboard content or profile argument. The fixed
synthetic draft is LF-normalized by the real store; insertion and Copy must match
`AΩ中🙂é\r\nsecond line\r\n` exactly (22 UTF-16 units), including final CRLF.
Original clipboard bytes stay only in the independent guardian's RAM.

The actual host creates a late-broker capability over exact owned A HWND/PID before
startup. The actual boundary's existing message-window instance and exact popup/main
handles are retained, not replaced. Session-ID translation happens on binding; both
session lease and broker admission remain denied until the distinct coordinator
permit **after guardian-ready and worker+fixture Job assignment**. The coordinator
publishes a bounded atomic own-surface metadata file for the external foreground
observer, and the runner announces `awaiting_owned_a`. Operator selects the owned
Target A editor and releases all input within 15 seconds. No initial synthetic focus
is used; the fixed `focus_a` command is allowed only after A is already foreground
and the all-VK-up/quiet gate passes. `empty_a/b` are assertions, not clearing commands.

The independent monitor's posted/captured-Copy/context policy accepts initial
foreground only before observed A, then the pinned A; only the exact popup HWND
of the coordinator-authenticated live worker PID can interrupt A after an atomic
successful Target018 grant marker. Main, B, zero, other candidate windows and
wrong-PID/reused HWNDs are not exceptions. The popup window ends on first observed
return to A, host-exit closure, or 20 seconds after observing the grant. Main-row
`copy` uses the same monitor but never permits popup foreground. Other scenes'
foreground policies are unchanged. Sampling is not continuous native-focus proof;
a missed A/return sample fails closed, not success. Monitoring never kills recovery.
A latched failure is retained after process exit in `native-monitor-failure.json`:
`predicate` (`foreground_foreign`, `foreground_unexpected_candidate`,
`output_oversized`, `observation_exception`, `process_exit`), integer `exitCode`,
positive integer `sampleOrder`, and `relation` using the control diagnostic tokens.
The first predicate wins; final output size is checked even on immediate exit.
Python validates the exact schema before reporting failure. This is diagnostics,
not review approval or permission to rerun.

- `posted-enter/row/main`: register Ctrl+Alt+Shift+F11 on the real host message HWND,
  unregister/reacquire, then request `foreground_grant` over the retained owned
  Target018 stdin/stdout control channel. Before readiness, Target018 pins the
  launching candidate PID plus process creation time from the lease's sanitized
  child environment and retains its process handle (missing/mismatched/dead pins
  refuse). No PID is accepted in the command. While exact owned A is foreground,
  Target018 makes one `AllowSetForegroundWindow(exactCandidatePid)` call and replies
  exactly `target_foreground_grant result=granted`, `target_foreground_grant result=refused`
  (only a false native grant), or `target_foreground_grant result=failed` (pre-check
  failure or exception). The scene also logs `result=failed` for cancellation,
  protocol errors or pre-check failure, never mislabelling these as a refused grant.
  Only a granted receipt plus a still-pinned target permits the immediately following
  **posted WM_HOTKEY**. Refusal fails closed (`foreground_grant_refused`); diagnostics
  use the fixed `target_foreground_grant` last pre-trigger stage, which remains set
  for later failures and is not a precise failure locator. Registered scenes never request
  this grant. No ASFW_ANY, self-grant, input queue attachment or focus retry exists.
  This models foreground authority, not a physical shortcut.
  Cheap source-pin tests check the real scene-to-lease wiring and the target's
  single native grant argument (`grantee.Id`); these are text assertions, not
  runtime proof of process identity, foreground authority or native call delivery.
  Human source review and an authorized native trial remain the real gates. Its actual native callback awaits
  bounded session admission and calls actual `host.ShowCapturedPopup`. The unchanged
  popup coordinator alone requests ready-surface focus through the late broker to
  `session.TryFocusOwnedPopup`. Wait for presentation, exact popup foreground and
  quiet/all-up before continuing. This proves native registration and posted callback
  delivery, **not physical shortcut delivery**.
- `registered-enter/row/main`: same owned-A preparation, real host/session and exact temporary
  Ctrl+Alt+Shift+F11 release/reacquire, but **never posts WM_HOTKEY or synthesizes the
  shortcut**. After preparation and registration, exclusively/atomically publishes
  `registered-shortcut-ready` in the owner-private fixture cwd, containing exactly
  `registered_os_shortcut_operator_ready
` (ASCII, LF). The observer validates it
  with `owned_bytes` and prints `native_phase=operator_ready` once. Only then should
  the operator press the real OS shortcut and release all input. Callback wait is
  bounded to 25 seconds; existing bounded all-up/idle, capability, exact target
  capture and shared popup focus coordination remain unchanged. Only one callback
  is admitted; duplicates fail the scene. Operator keys never acquire agent-owned
  recovery and are never synthetically released. Later guarded native Enter uses
  the same insertion/usage/duplicate checks as `posted-enter`, with
  `keyboard_begin`/`keyboard_permit` before agent synthesis only. Registered row/main
  use the actual existing popup/main VM callbacks, not native Enter; their action
  proof is `vm_callback`, never physical click, UIA or native pointer proof. Success labels
  shortcut proof `registered_os_shortcut_operator_delivered`; posted scenes retain
  `registered_posted_callback_not_physical`. Neither input filtering nor these labels
  prove physical HID authenticity. No foreground permission relaxation or fallback.
  The 55-second capability/session and 65-second worker bounds remain unchanged.
  Before launch the Python parent emits JSON phase `registered_session_prelaunch`
  with the exact constrained `privateEvidenceToken`, for this registered family. This is
  metadata, not readiness/success/authorization. A trusted asynchronous outer parent
  can retain that exact session identity and observe its fixed readiness artifact
  read-only before prompting the operator; never discover a latest directory as
  authority and never add an external timeout-kill watchdog. Coordinator stdout
  remains the existing exact phase/result protocol.
- `posted-enter`: separately source-gated `SendInput` Enter down/up reaches the actual
  search-box key handler and host-tracked insertion. Final checks immediately before
  dispatch cover visible/current desktop, 1500ms idle, all VKs up, bounded live lease,
  exact popup HWND/PID, retained image and unguessable pin. Zero accepted events is a
  refusal; partial/throwing calls remain `KEYBOARD_RECOVERY_PENDING`, never key-up repair.
- `registered-row/main` and `posted-row/main`: invoke the actual popup-row or main-row VM callback composed by
  the actual host; these modes prove callback wiring, **not native pointer/menu event
  delivery or a visible main-window click**. All six insert modes assert concurrent
  gate Busy, duplicate VM no-op, single capture/paste, exact A readback and empty B,
  and reload the real SQLite store to require usage exactly once.
- `copy`: Copy-only session, real host-tracked bounded Copy wait and main-row VM Copy;
  exact synthetic CF_UNICODETEXT comparison only after successful commit; empty A/B,
  zero paste/capture/usage. Explicit Copy commits the synthetic clipboard content,
  not restoration of the baseline. Fresh operator recopy is mandatory afterward.
- `popup-copy`, `editor-copy`, `footer-copy`: use the same reviewed Target018 foreground
  grant, posted shortcut, exact captured A and ready-popup foreground checks as `posted-row`.
  These are VM callback scenes, **not rendered control/menu or physical pointer proof**.
  Popup Copy invokes `Popup.CopyRow`; editor Copy opens the real popup editor, injects
  an owned SQLite UPDATE-abort trigger, attempts Save and requires a dirty, editable
  recovery draft, then invokes its Copy command. The trigger is removed in `finally`;
  the draft remains editable after Copy and is discarded without a successful save.
  Footer Copy deliberately suspends gate admission for one insertion callback, requires
  the truthful pre-dispatch error footer, resumes admission and invokes the footer Copy
  command. No Copy scene arms keyboard mutation, swaps insertion clipboard or pastes.
  Each requires exactly one capture and Copy commit, empty A/B and zero usage/paste.
  Capture/Insert/Focus permit captured-popup and shortcut admission. Footer Copy also
  makes a real insertion attempt while gate admission is suspended, yielding the
  intentional pre-dispatch refusal. Session/guardian insertion wiring is present, but
  these Copy capabilities have no Input/Clipboard/ClipboardLoss flags and native
  mutation remains unarmed throughout. Each consumer returns observed fixed fields:
  popup has no editor/footer; editor records failed Save, an open/editable editor
  after Copy and subsequent cancellation; footer records Error immediately before Copy.
  The native scene and behavioral tests require the distinct record for each scene.
- `context-insert`: same posted preparation and keyboard permit as `posted-row`, but
  invokes the menu's neutral `Popup.PopupContextInsert` invocation-time eligibility /
  host-operation callback seam. The Copy-scene driver lives only in the fixture,
  not the ordinary candidate or its AcceptanceNative namespace.
  The rendered context menu and native pointer delivery are **NOT** exercised. Tests
  require the success run wrapper exactly once; preview, a deleted/stale row, busy
  state and refused native permission must refuse before host operation;
  successful insertion requires exact A, empty B, one paste/usage and no Copy.
- `preview/expired`: active preview refuses main/popup Copy and Insert callbacks;
  selection closes without insertion. `expired` waits for the real monotonic capability
  deadline first, independently measuring beyond its entire lifetime, not artificial revocation.
  After preview hides it validates the still-live 55s adapter lease and exercises denied
  non-preview main/popup Copy, host capture and direct/VM Insert. `expired` alone also
  carries bounded insertion permissions, so it requires input source review even though
  no non-preview operation is attempted until expiry is verified.
  Both require empty A/B and zero native
  mutation/capture/usage counters. Preview selection intentionally releases the main
  Copy lock on hiding, so no Copy is attempted after hiding except under expiry.

Before any Enter or insertion callback, the worker requests `keyboard_begin`; the
coordinator arms retained keyboard lifetime **before** replying `keyboard_permit`.
Only explicit `keyboard_settled` after adapter recovery drain permits Job abort/close.
Lost receipts, worker death, timeout or ambiguous input retain coordinator, Job and
worker/guardian handles indefinitely with a content-free pending receipt. The worker
also stays alive without disposing possibly armed state. Its failure path closes host
admission, awaits the actual gate/VM drain, and attempts independent guardian restoration.
While retained it retries that ordered, bounded ownership-checked restoration every second;
clipboard settlement never repairs uncertain input. Known-safe Enter/paste with no pending
clipboard snapshot can send `keyboard_settled` even after failed readback; the scene remains
failed. The coordinator accepts a late settlement receipt without converting failure to PASS.
This is not a keyboard recovery implementation.
Do not wrap either lifetime in an outer timeout/kill, or terminate retained processes
as a substitute for reviewed operator recovery. Ordinary restore/abort controls still
use their original exact protocol and do not arm input.

For every positive scene the independent Python parent snapshots the clipboard sequence
before launch and after actual exit and opens the owned SQLite database with `mode=ro`
and `query_only=ON`. Before opening, metadata-only `lstat` checks reject symlinks,
reparse points and multiply-linked database/WAL/SHM files; no whole-database byte read
is performed. Its SQL reads only usage counts: exactly one row with usage 1 for Insert,
0 for Copy/preview/expiry. Epoch must change for Copy/Insert and remain identical for
preview/expiry. Missing/extra rows, wrong usage, unavailable epoch or wrong mutation
boundary fail. Fixed JSON fields `usageVerified` and `clipboardEpochChanged` report
these checks without persisting text or queries. Copy never supplies a restorative
baseline witness; a fresh operator recopy remains required.

The global ERROR-log gate remains closed: only `editor-copy` requires and permits
exactly one timestamped fixed line, `ERROR store_write_failed op=update_snippet
error=SqliteException sqlite=19 hresult=0x80004005`. Any added field, content, raw
exception, duplicate, missing line or any other ERROR fails; the same line is forbidden
in every other scene. Privacy scanning still examines the original unfiltered log.

Success requires ordered guardian-ready/awaiting-A/result receipts, zero worker/
fixture/guardian exit codes, actual host shutdown-complete log and privacy scan.
C# tests run the actual host/store/VM/gate with fake OS boundaries and pure protocol/
partial-count guards. They do not establish native UI delivery or Windows safety.

## Explicit remaining scope

All native positive modes remain **NOT RUN**. Parent alone owns row6 and subsequent
native execution; no source review gate is cleared here. Physical shortcut, native
row/main pointer/menu events, Copy across every editor/footer surface, full fault
matrix (changed owner/focus loss/closed target/contention/uncertain input/shutdown),
native pointer/hover, legacy coexistence and oldest OS remain open. Uncertain-keyboard
operator recovery has no automatic endpoint and must not be inferred from clipboard
settlement or guardian exit. No production host/adapters were edited. Plan019 T5/T7,
018 obligations and final acceptance are not complete. Independent review of exact
source/build/protocol and a separately authorized screen slot are still mandatory.

## Initial owned A preparation (closed review gate)

`OwnedTargetPreparation.SourceReviewed` is independently hard-false. With it closed,
positive scenes retain the original 15-second operator-selection/quiet wait. With it
reviewed open, only positive scenes after guardian readiness, Job assignment and
`lease_positive` may prepare A, before `keyboard_begin` or clipboard mutation. A
preparation refusal aborts; it never falls back to manual activation or synthetic input.
The original foreground snapshot from `App.OnLaunched` is the only non-A foreground
allowed. An already-foreground A needs no UIA call.

The in-process Native-AOT bridge uses CUIAutomation/IUIAutomation, exact retained
`EditorA` HWND resolution, and one `SetFocus`. This is intentional owned activation,
**not nonactivation QA evidence**. No tree traversal, external automation process,
foreign focus, input synthesis or clipboard API is involved. Pin, exact A/editor
PID/thread/child/classes, session Focus permission, visible input desktop and all-VK
quiet checks surround provider calls. Only the UIA focus call may account for an
input-epoch change, followed by a fresh bounded 1500ms quiet period. The resulting
epoch is frozen throughout settlement and before/after the owned editor command;
already-foreground A refuses epoch drift without any UIA call. Final foreground/session
checks precede the retained manual check. COM references are released on every return/HRESULT failure. A hung provider
remains before keyboard arming: existing coordinator-owned pre-keyboard timeout and
Job cleanup apply; uncertain keyboard retention is unchanged.

The `expired` scene grants the still-unadmitted broker a temporary **Focus-validation
only** scope during initial preparation. Both capability monotonic expiry/permission
and session checks remain mandatory; capture, Copy, Insert, Input, clipboard and
popup-focus methods remain denied. The scope is removed in `finally` (and by Close)
before the 21-second expiry-only test; it does not grant full broker admission.

ABI verified against installed Windows SDK **10.0.26100.0**
`um/UIAutomationClient.h`: line 669 (`UIA_HWND = void*`), lines 19092–19136
(`IUIAutomationVtbl`, `ElementFromHandle` slot 6), lines 1869–1891
(`IUIAutomationElementVtbl`, `SetFocus` slot 3), IID at 18848 and CLSID at 23181.
IUnknown Release is slot 2; HRESULT is 32-bit, HWND/interface pointers pointer-sized,
and calls use STDMETHODCALLTYPE/Stdcall. Tests exercise those actual bridge slots
with unmanaged fake vtables, never COM activation. Policy tests use fake OS/clock/COM;
they do not prove real provider behavior, desktop activation or live HWND identities.
`owned_initial_focus phase=waiting|ready|refused` and fixed preparation reason codes
are content-free. `native_positive_failed stage=<token>` records only an allowlisted
last-entered stage (`NativeStage.cs`), the exception type and HResult, not
messages/titles/paths. A generic prior failure does not identify its failed predicate.
