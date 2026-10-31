# Internal native clipboard-control fixture — SOURCE REVIEW CLOSED / NOT RUN

This is a separate test assembly linking the **actual UnoAppHost, native boundary,
XAML, Core store and Presentation VMs**. Its required basename is
`SmartSnippets.UnoCandidate.exe` because the independent guardian pins that name.
It is NOT the production candidate: AppId `org.smartsnippets.unonativefixture019`,
instance prefix `SmartSnippets.UnoNativeFixture019`, fixed fresh
`native-fixture-profile` beneath an owner-created private working directory.
There is no default-profile fallback, profile argument or environment unlock.

`NativeFixtureSourceReviewed`, Python `NATIVE_SCENES_SOURCE_REVIEWED` and adapter
`NativeAcceptanceFactory.SourceReviewed` are all false. Building/testing does not
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

## Explicit remaining scope

These controls are not positive VM insertion or Copy evidence. Shortcut callback
→ bounded ReadyForAdmission → actual host capture/show → coordinator-ready focus,
Enter/row/main-row InsertionService → exact once usage, distinct Copy and preview
no-op native scenes are **not implemented**. They require subsequent review-gated
work after both clipboard controls are proven. Keyboard uncertainty remains pending,
never generic key-up repair. Native pointer, legacy coexistence, oldest OS, UI event
parity and Plan019 T5/T7 completion remain open. No production host/gate patch is
required by this batch beyond the parent's already supplied metadata getters and
clipboard-control APIs. Native inherited-handle/bootstrap behavior still requires
independent review and separately authorized Windows execution.
