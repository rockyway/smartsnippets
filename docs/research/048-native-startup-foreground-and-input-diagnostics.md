# 048 — nd-04: repeated-scene foreground and first-refusal diagnostics

Static investigation of research 047 / nr-07. **No screen, UI/native scene or
clipboard access. All review gates remain false. Plans 018/019 remain incomplete.**
The brief explicitly permits diagnostics when a defensible static fix is unavailable.

## Microsoft rules (official documentation)

- [SetForegroundWindow](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setforegroundwindow):
  desktop application, foreground process has not locked foreground calls, and no
  active menus; additionally at least one listed condition: lock timeout expired,
  caller is foreground, caller was started by foreground process, no foreground,
  caller received last input, or caller/foreground process is being debugged.
  Windows may deny even when these conditions hold. Shared ancestry or identical
  launch arguments are **not** a documented grant to every subsequent child.
- [AllowSetForegroundWindow](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-allowsetforegroundwindow):
  grantor must already be eligible; exact-PID grant is lost on subsequent user input
  not directed to the grantee, or another ASFW call naming a different process.
  A foreground change alone is not the documented complete revocation rule.
- [LockSetForegroundWindow](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-locksetforegroundwindow):
  foreground process can disable foreground calls; ALT or a system-caused foreground
  change automatically enables them. No lock/unlock workaround is appropriate here.
- The foreground lock timeout is a distinct system eligibility alternative, **not**
  the fixture's 1500ms quiet-input interval. No measured system timeout, active-menu,
  lock-state or Windows eligibility receipt exists in 047. A short quiet wait does
  not prove timeout expiry; do not change system settings or add activation retries.

## Static chain and limits of the root-cause claim

1. Trusted Python runner launches a new coordinator with `subprocess.Popen`, not
   Target018 (`scripts/run_uno_candidate_native.py:564-570`). Coordinator snapshots
   foreground and launches the worker with `Process.Start`
   (`scripts/uno_candidate_native_fixture/Coordinator.cs:34-36`). Neither retained
   Python parent identity nor repeated process creation proves foreground authority.
2. The **candidate worker** launches Target018: `App.xaml.cs:44-47` calls
   `AcceptanceFixture.StartAsync`; the lease uses that worker's PID/start time for
   the later grantee (`prototypes/uno-native-target/identity/OwnedFixtureLease.cs:56-75`),
   and the factory's actual launcher is `Process.Start(info)`
   (`src/SmartSnippets.Uno/Platforms/Windows/AcceptanceNative/AcceptanceFixture.cs:24,46-50`).
   Target018 windows are deliberately nonactivating
   (`prototypes/uno-native-target/windows/Program.cs:249`). The worker checks unchanged
   foreground across target and host startup (`App.xaml.cs:51,64-69`). The old logs
   do **not** establish whether this launcher had any Windows eligibility alternative
   at launch. It need not itself own the foreground to have permission, either.
3. Preparation admission `ready` proves the original-foreground/pinned/desktop/
   permission/quiet predicates, **not** the Windows foreground right
   (`OwnedTargetPreparation.cs:35-64`). Exactly one UIA `SetFocus` is attempted after
   repeated guards; `ForegroundRefused` specifically means UIA returned success but
   A was not foreground (`OwnedTargetPreparation.cs:73-83`). The native wrapper is
   UIA vtable SetFocus, not an ASFW grant (`OwnedPreparationAutomation.cs:29-30`).
   Hence the five nr-07 failures are missing initial owned-A activation, not the
   later popup grant failing. UIA success is not native foreground acceptance.
4. Only **after A is proven foreground** does the posted route request the
   Target018 → exact-candidate grant (`PositiveHostScene.cs:170-184`;
   `prototypes/uno-native-target/windows/Program.cs:52-60`). It cannot bootstrap A.
   The existing candidate popup request remains single-attempt
   (`src/SmartSnippets.Uno/Platforms/Windows/AcceptanceNative/OwnedFocus.cs:70-79`).
5. Controls do not supply a durable foreground anchor. They require their sampled
   original foreground unchanged and dispose the host/fixture afterward
   (`App.xaml.cs:117-128,150-154`; `Coordinator.cs:104,131`). The owner-refusal A→B
   transition is **clipboard ownership**, not foreground activation
   (`src/SmartSnippets.Uno/Platforms/Windows/AcceptanceNative/OwnedOwnerControl.cs:16-40`).
   Posted-row closes the host and disposes its Target018 (`App.xaml.cs:93-99`;
   `AcceptanceFixture.cs:64-73`). With nr-07's zero survivors between scenes, neither
   that A nor that popup survives to grant anything to the next Target018 PID.
   A different foreground after teardown can remove the foreground-process / started-
   by-foreground route for a new launch; it does not authenticate the actor, prove
   all eligibility alternatives false, or prove that moving outside a process tree
   itself revoked a specific grant. The monitor's `same_pid_other` also does not
   identify a foreground application or a parent with permission.

**Conclusion:** the protocol has no guaranteed cross-scene bootstrap foreground
right. First-scene success followed by later preparation denial is consistent with
Windows' changing eligibility and destruction of owned foreground surfaces, but
047 cannot establish which eligibility condition enabled the first activation or
which later condition caused refusal. Identical launch topology is insufficient.
A claim that timeout or process-tree escape alone definitively caused nr-07 would
exceed the available static evidence.

## Disposition: diagnostic fallback, not a speculative authority fix

An exact-PID ASFW handoff is valid only from an authenticated eligible grantor,
while that grantor remains live, before new Target018 preparation. The retained
trusted Python parent is not proven eligible. The preceding owned A/popup is dead
under the explicit zero-survivor rule. Keeping it alive would change that reviewed
lifetime boundary and require a new authenticated cross-scene handoff protocol.
A worker self-grant, ASFW_ANY, foreign activation, input synthesis, AttachThreadInput
or blind grant/retry would not solve the missing authority safely. None is added.

Instead `NativeStartupDiagnostics` records a single foreground sample per call,
fixed relation `none|owned_a|owned_b|launcher|foreign`, boolean
`launcher_foreground`, and increasing worker-local `sample_order`:

- `target_launch`: before fixture bootstrap; buffered until host log opens. This is
  **not** an atomic observation at the nested Process.Start call.
- `host_ready`: after host StartAsync, before readiness checks.
- `preparation_start`, `before_focus`, `after_focus`: the actual preparation path,
  surrounding its one UIA call. Already-A/refused admission never invents focus samples.

`launcher` means the worker that actually creates Target018. No PID, HWND, title,
class string, path, content or raw exception is logged. Sampling grants nothing;
`launcher_foreground=False` does not prove ineligibility. These records distinguish
owned/launcher/foreign startup and loss around preparation in a future authorized
run; they cannot expose Windows' complete internal denial cause. No new run is
implicitly authorized by this change.

## posted-enter host_ready: first refusal narrowed without guessing

047 establishes last stage `host_ready`, no guardian receipt, no synthetic row,
and host shutdown. In this code the next stage is `session_create`
(`App.xaml.cs:61-70`); before it, the only explicit refusal predicates are missing
popup/handle/main/handle/store/popup model, insertion unexpectedly admitted, then
changed foreground. Row creation is later in `PositiveHostScene.RunAsync:28-31`.
Thus a keyboard/Enter-delivery or guardian grant defect cannot explain that first
refusal. The original compound resource check and foreground check shared the same
stage and sanitized exception type: no available log distinguishes them.

`NativeHostReadiness.Check` preserves their exact short-circuit order and logs
`native_host_readiness reason=popup_missing|popup_handle_missing|main_missing|
main_handle_missing|store_missing|popup_model_missing|insert_admitted|
foreground_changed|ready`. First false predicate wins; no admission is broadened.
The stage and wire protocol stay unchanged. This pins the first explicit refusal
on the next authorized run instead of guessing from the foreign monitor sample.

## Picker reason=input

There are exactly three guard causes in the Python observer:

- `require_all_input_up`: any held VK 1..255 → `held`.
- `quiet_input`: expected last-input timestamp differs → `epoch_changed`.
- Same check: unsigned tick idle interval <1500ms → `idle`.

See `scripts/run_uno_candidate_ui.py:183-216`. Query failure remains the separate
`input_query_failed`. Epoch mismatch retains precedence over insufficient idle;
held-input preflights still run in their original positions. The existing bounded
prelaunch quiet-settle loop is unchanged. Failure JSON keeps `reason=input` and
adds only allowlisted `inputSubreason`; arbitrary attributes/text are discarded
(`scripts/run_uno_candidate_acceptance.py:232-252`). The worker record, not the
parent's `worker_protocol`, is the discriminating evidence. Nr-07's old `input`
record cannot retrospectively identify the guard or the source of input.

## Headless verification

Behavioural stubs exercise all foreground relations/ordered single samples,
invalid-phase refusal, successful/refused preparation with exactly one focus,
all eight first host-readiness refusals and short-circuit order, real Python held/
epoch/idle branches, the inclusive 1500ms boundary, query failure, JSON persistence
and rejection of nonallowlisted subreasons. Existing identity/guard/lifetime suites
remain intact. No source-review flag changes or acceptance claims.

Sequential commands: native fixture tests **185 PASS** → Target018 identity tests
**86 PASS** → candidate tests **310 PASS** → Python tests **193 PASS** → fixture
`build.ps1 -BuildOnly` **PASS**, fresh native x64 artifact token
`ea0b720e6bad4a0a8d20e98fcf0e692c`, SHA256
`3666c62e93c54a251482193ff55faa864495339b5f7e89481068f40d74804f90`.
Final build passed strict existing gates with zero warnings. Two earlier build-only
attempts exposed the explicit source-list omission and nullable flow lost across
the extracted check; both were corrected, no warnings suppressed. No executable
was launched. Build/test success is not foreground, picker or native-input acceptance.
