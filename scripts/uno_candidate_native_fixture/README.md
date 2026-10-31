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
The worker directly starts Target018. The unchanged target source uses
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
slot, positive runner modes are `posted-enter`, `posted-row`, `posted-main`, `copy`,
`preview`, `expired`. They additionally require `--row6-controls-verified` and
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

- `posted-enter/row/main`: register Ctrl+Alt+Shift+F9 on the real host message HWND,
  unregister/reacquire, then **post WM_HOTKEY**. Its actual native callback awaits
  bounded session admission and calls actual `host.ShowCapturedPopup`. The unchanged
  popup coordinator alone requests ready-surface focus through the late broker to
  `session.TryFocusOwnedPopup`. Wait for presentation, exact popup foreground and
  quiet/all-up before continuing. This proves native registration and posted callback
  delivery, **not physical shortcut delivery**.
- `posted-enter`: separately source-gated `SendInput` Enter down/up reaches the actual
  search-box key handler and host-tracked insertion. Final checks immediately before
  dispatch cover visible/current desktop, 1500ms idle, all VKs up, bounded live lease,
  exact popup HWND/PID, retained image and unguessable pin. Zero accepted events is a
  refusal; partial/throwing calls remain `KEYBOARD_RECOVERY_PENDING`, never key-up repair.
- `posted-row/main`: invoke the actual popup-row or main-row VM callback composed by
  the actual host; these modes prove callback wiring, **not native pointer/menu event
  delivery or a visible main-window click**. All three insert modes assert concurrent
  gate Busy, duplicate VM no-op, single capture/paste, exact A readback and empty B,
  and reload the real SQLite store to require usage exactly once.
- `copy`: Copy-only session, real host-tracked bounded Copy wait and main-row VM Copy;
  exact synthetic CF_UNICODETEXT comparison only after successful commit; empty A/B,
  zero paste/capture/usage. Explicit Copy commits the synthetic clipboard content,
  not restoration of the baseline. Fresh operator recopy is mandatory afterward.
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
