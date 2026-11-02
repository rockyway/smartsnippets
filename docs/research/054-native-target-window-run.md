# 054 — nr-11/nr-11b: Target018 window diagnostic acceptance

Authorized brief `native-run12-brief.md`, 2026-11-01. **Phase 0 PASS;
phase 1 FAIL at held-input preflight; all native scenes NOT RUN. Release PASS.**
The original nr-11 made no clipboard access, application launch, UIA action or input synthesis.
The separately authorized nr-11b continuation below reused its artifacts, passed
controls and recorded six positive failures plus one picker failure.
Research 053's causal Target018 and picker-epoch questions remain unresolved;
Plans 018/019 remain incomplete. This is a safety refusal, not native acceptance.

## Authorization, timing and first failure

- Worktree `.claude/worktrees/uno019-native-trial10`, branch
  `feature/uno019-native-trial10`, starting HEAD `cad80bf` (nd-06).
- Read the brief, AGENTS, safety guide 002, progress handoff 004, approved Plan 019,
  research 051–053 and both fixture READMEs. Operator-local AGENTS/safety-guide
  edits were preserved and excluded from commit. No delegation or push.
- Phase 0 used filesystem/process queries and sequential build-only wrappers.
  `PHASE0-READY nr-11` was written to `native-run12-report.md` before polling.
- Filesystem-only grant polling ran every 30 seconds: WAITING at
  `2026-11-02T00:16:12.778580Z` and `00:16:42.779166Z`, GRANT_AVAILABLE at
  `00:17:12.779731Z` (poll 3, 60.002 seconds). No DENIED/REVOKED observed.
- The exact `GRANTED nr-11` file authorized EXCLUSIVE **18:16–18:41 local**,
  bottom-left only, controls and six posted/VM positive scenes, then optional
  Import Cancel with at least five minutes remaining. Time admission occurred
  `18:17:19.746184` local (`2026-11-02T00:17:19.746184Z`).
- Live PMv2 enumeration confirmed nonprimary `DISPLAY29`, bounds
  `(-1920,1506,0,2586)`, work `(-1920,1506,0,2538)`. Live primary `DISPLAY3`
  was `3840x2160`, work `(0,0,3840,2088)`; top-left `DISPLAY30` was
  `(-1920,306,0,1506)`, work `(-1920,306,0,1458)`. No window was controlled
  or moved on any monitor.
- **First preflight failed**, exit 1, stderr `held_input_refused`. Its complete
  fixed input record was:

```json
{"held_virtual_keys": [32]}
```

The command chain used `&&`; failure prevented `nr11-run.py` from being launched.
No controls, native coordinator, fixture, Target018, guardian or picker started.
No input-release workaround, retry, baseline seed, generic key-up or request for
operator input followed. VK 32 was observed held; this does not identify its source
or establish that it remained held later. No later input sampling was used to
rescue the refusal. The safety stop, not the remaining time budget, ended the run.

## Phase 0 — PASS

Exactly the same nine tracked trial booleans as nr-10 were temporarily enabled:

| File | Constants |
|---|---|
| scripts/run_uno_candidate_native.py | NATIVE_SCENES_SOURCE_REVIEWED, POSITIVE_SCENES_SOURCE_REVIEWED, POSITIVE_INPUT_SOURCE_REVIEWED |
| scripts/run_uno_candidate_ui.py | UI_SOURCE_REVIEWED |
| scripts/uno_candidate_native_fixture/Program.cs | NativeFixtureSourceReviewed |
| scripts/uno_candidate_native_fixture/PositiveProtocol.cs | SourceReviewed, InputSourceReviewed |
| scripts/uno_candidate_native_fixture/OwnedTargetPreparation.cs | SourceReviewed |
| src/SmartSnippets.Uno/Platforms/Windows/AcceptanceNative/NativeAcceptanceFactory.cs | SourceReviewed |

Exact enabled diff retained privately as `nr11-enabled-flags.diff`, SHA256
`0109717d483af87f573bc705dbc25b29c1496b3649a74a93c64bdfd9a405eea1`.
Ordinary candidate capability, startup/full-host/prototype gates stayed denied.
No enabled source was committed. All nine constants and the scratch UI gate were
restored false at release.

Five fresh strict Native AOT x64 builds ran sequentially, with new output directories.
Linked fixture/candidate property, reference, native artifact, SQLite and provenance
gates passed; all warning/error scans passed. The native build wrapper also passed
`--validate-build`. Target/guardian builds used owner-ACL-private new directories,
clean followed by publish, empty NoWarn, AOT/trim/compatibility/analyzers and
warnings-as-errors enabled, trim-warning suppression and aggregation disabled.
No warning gate was weakened. No test-suite rerun or legacy publish was performed.

| Artifact | Fresh token | SHA256 |
|---|---|---|
| native fixture | `c5443f0bca994b96b3b8a72a6364ca3a` | `b1e816f2217058a9947b58fe2da459bca6dfc6ae8fd0195190cd9312ca15ca3e` |
| Target018 | `0dd6e6c9b4f54b1490cc8a81096fe9f8` | `c287df14018067a45f496890d2750d848f5660802a5918636787939aaf22e4a9` |
| guardian | `60e7b57cc5bd45ac959f5b61933eafbe` | `b07e3c419326dff75ce1adbd228e11eead6bb5dad38653bbaa7a2d74e511a9a8` |
| management fixture | `a8fa82969a404f22aebe053c207d3d2c` | `e250cc182b8700edbb05771e41e72270a1833530c33ffc8db8b1d86a4bc53fee` |
| ordinary candidate, build only | `f065101631e04419831a53aa7dd50cbe` | `a6fd42f7565f9cb839dae3e903f0bfc2ed12710ae5168aa5795960ba5e23a4b0` |

Native enabled-source receipt:
`42f9c7885748c2caceb0b527a2ea05e9cd7b326ae95b55897077378c4c93dca4`.
Management base-source receipt:
`1372a2191a3c38403d9d89b3aa25b79fda2059a11d4088dce7e43a63e6e3008c`.
Artifact manifest is `nr11-artifacts.json`. No artifact was launched; retained
trial-enabled binaries are evidence, not permission to launch later.

### Helpers and admission — PASS

Copied nr-10 helpers to nr11 names and current worktree paths; originals untouched.
The fixed admission guard retained all candidate/fixture/guardian/Target018/
run_python/ui_helper classes, self/live-ancestor/trusted-parent exclusions and fixed
`admission_match class=... self_or_ancestor=... trusted_parent=...` refusal lines.
It ran dry twice consecutively from the same bash → Python → pwsh -File chain:

```json
{"attempt": 1, "exitCode": 0, "stdout": ["admission_dry_pass=1"], "stderrEmpty": true, "reason": "none"}
{"attempt": 2, "exitCode": 0, "stdout": ["admission_dry_pass=1"], "stderrEmpty": true, "reason": "none"}
```

DryRun returned before clip.exe. Eleven scratch Python syntax checks and all
scratch PowerShell parsing passed; helper hashes retained in
`nr11-helper-hashes.jsonl`. Process counts were all zero before and after builds.

Picker helper copies came from current tracked sources, preserving nd-06 measured
open/commit Invoke epochs and pk-01b failure fields. Only repository/helper paths,
local UI gate and an explicit `throw 'actions'` immediately after
`WaitPhase 'ui-import-cancelled'` were changed. The stop precedes Import Commit;
there is no undefined Fail call. The wrapper resolves fresh management pins from
this run's artifact manifest. None of these picker helpers was executed.

## Commands actually executed

`<scratchpad>` denotes the private brief/report directory. Builds were supervised
by `nr11-build-phase0.ps1`; the following build-only commands ran sequentially:

```text
python -I -B <scratchpad>/nr11-dry-admission.py
pwsh -NoProfile -File <scratchpad>/nr11-process-check.ps1
pwsh -NoProfile -File scripts/uno_candidate_native_fixture/build.ps1 -BuildOnly
pwsh -NoProfile -File <scratchpad>/nr11-build-support.ps1
pwsh -NoProfile -File scripts/verify-uno-candidate.ps1 -BuildOnly
pwsh -NoProfile -File scripts/publish-uno-candidate-aot.ps1
python -I -B <scratchpad>/nr11-artifacts.py
pwsh -NoProfile -File <scratchpad>/nr11-parse-helpers.ps1
# Python ast.parse of all 11 scratch .py files; final phase-0 process audit.
# PHASE0-READY written, then:
python -I -B <scratchpad>/nr11-wait-grant.py
python -I -B <scratchpad>/nr11-admit-window.py > <scratchpad>/nr11-time-admission.txt && python -I -B <scratchpad>/nr11-preflight.py > <scratchpad>/nr11-preflight-result.json && python -I -B <scratchpad>/nr11-run.py > <scratchpad>/nr11-launch.txt
# preflight exited 1: the final chained native parent command DID NOT EXECUTE.
# Restore all nine flags and scratch UI gate; grep proof; then:
git diff --exit-code -- <six temporarily changed tracked source files>
pwsh -NoProfile -File <scratchpad>/nr11-process-check.ps1
```

## Per-phase ledger and diagnostic availability

| Step | Result | Evidence / boundary |
|---|---|---|
| Phase 0 | PASS | five fresh strict builds, hashes, two dry admissions, syntax checks |
| Phase 1 preflight | FAIL | held_input_refused; held_virtual_keys=[32] |
| restore → abort → owner-refusal | NOT RUN / BLOCKED | no fresh restorative witness |
| Phase 2 posted-row, posted-main | NOT RUN / BLOCKED | phase 1 did not pass |
| Phase 2 popup-copy, editor-copy, footer-copy, context-insert | NOT RUN / BLOCKED | phase 1 did not pass |
| posted-enter | NOT RUN | grant excludes SendInput Enter (D50) |
| registered-* | NOT RUN | excluded by grant |
| Phase 3 Import Cancel | NOT RUN / BLOCKED | held-input safety stop; no picker launched |
| Other picker phases | NOT RUN | outside scope; explicit helper stop prepared |
| Release | PASS | flags false; zero owned processes; no clipboard access or recovery |

**No native session tokens exist.** There are no `failureStage`,
`target_foreground_grant result=`, `native_positive_failed`,
`native_startup_foreground`, `native_host_readiness`, `owned_foreground_trace` or
`native-monitor-failure.json` records for nr-11. Thus there is no monitor predicate,
exitCode/sampleOrder/relation/closed/windowFacts observation to copy. No independent
usage or clipboard-epoch checks ran; none may be reported PASS. The preflight exit
code 1 is not a native monitor exit code.

Picker reason/operationClass/pickerStep/HRESULT/schema matches/inputSubreason,
inputEpochDelta/inputIdleMs/inputPhase and helper-side before/after/completed Invoke
epochs are all **unavailable because NOT RUN**, not unchanged/negative observations.
No scene logs were produced, so no runtime privacy CLEAN assertion is claimed.

The intended posted/VM proof limit remains
`registered_posted_callback_not_physical`, never physical shortcut/pointer/menu
acceptance. This run proves neither that Target018's other foreground surface is
B nor its fixed class/style/owner/message-only/thread/cloak facts. No diagnostic
from research 052 is substituted for a fresh nr-11 observation.

## Release — PASS; SCREEN-DONE 18:17:40 local

Exact grep proof for tracked gates:

```text
scripts/run_uno_candidate_startup.py:21:STARTUP_OBSERVER_REVIEWED = False
prototypes/uno-native-target/windows-insertion/Program.cs:11:    private static readonly bool NativeEntryReviewed = false;
scripts/run_uno_candidate_acceptance.py:24:FULL_HOST_OBSERVER_REVIEWED = False
scripts/uno_candidate_native_fixture/PositiveProtocol.cs:8:    internal static readonly bool SourceReviewed = false;
scripts/uno_candidate_native_fixture/PositiveProtocol.cs:9:    internal static readonly bool InputSourceReviewed = false;
scripts/run_uno_candidate_ui.py:20:UI_SOURCE_REVIEWED = False
src/SmartSnippets.Uno/Platforms/Windows/AcceptanceNative/NativeAcceptanceFactory.cs:14:    internal static readonly bool SourceReviewed = false;
scripts/uno_candidate_native_fixture/OwnedTargetPreparation.cs:26:    internal static readonly bool SourceReviewed = false;
scripts/uno_candidate_native_fixture/Program.cs:14:    internal static readonly bool NativeFixtureSourceReviewed = false;
scripts/run_uno_candidate_native.py:19:NATIVE_SCENES_SOURCE_REVIEWED = False
scripts/run_uno_candidate_native.py:20:POSITIVE_SCENES_SOURCE_REVIEWED = False
scripts/run_uno_candidate_native.py:21:POSITIVE_INPUT_SOURCE_REVIEWED = False
```

Scratch `nr11-picker-cancel-only.py:20:UI_SOURCE_REVIEWED = False` also verified.
`git diff --exit-code` passed for all six temporarily changed tracked sources.
Final process record, verbatim:

```json
{"observedUtc":"2026-11-02T00:17:40.4570043+00:00","counts":{"SmartSnippets.UnoCandidate.exe":0,"SmartSnippets.UnoAcceptanceFixture.exe":0,"SmartSnippets.UnoCandidate.Guardian.exe":0,"SmartSnippets.Target018.exe":0,"nr11PythonHelpers":0,"UIHelper":0}}
```

Clipboard state: **untouched and uninspected throughout nr-11**. No baseline was
seeded, content read, sequence queried, Copy committed or paste performed. No claim
of prior content, restoration or later unrelated clipboard state is made. No native
session, coordinator, guardian or uncertain keyboard lifetime was initiated; no
owned recovery remains pending. No process was killed or recovery endpoint used.

`SCREEN-DONE nr-11 2026-11-01 18:17:40 local (-06:00)` was written to the report
immediately after closure checks, before hard end 18:41. Subsequent work was
filesystem-only documentation. Commit scope: **only this research document**.

Pending: a separately authorized fresh acceptance run must pass quiet-input
preflight and the same-artifact control chain before measuring Target018's fixed
window facts and the six positives. Picker Invoke epoch attribution also remains
unmeasured. No retry or remaining-slot authority is inferred from this release;
no operator input/action was requested. Plans 018/019 remain open.

## nr-11b — authorized continuation, 2026-11-01

**Reused phase 0 PASS; controls PASS; all six positives FAIL; Import Cancel FAIL;
release PASS.** The original nr-11 refusal remains historical FAIL, not rescued.

### Reuse and screen admission

No rebuild or test-suite rerun: all five tokens/entry paths and recomputed SHA256
matched the phase-0 artifact table above. Re-enabled the same nine trial booleans;
enabled diff SHA256 matched
`0109717d483af87f573bc705dbc25b29c1496b3649a74a93c64bdfd9a405eea1`.
Original nr10 helpers untouched. Reused nr11 helper basenames preserve the fixed
admission guard's classification. Only exact grant identity changed to nr-11b,
plus the leader-authorized preflight sampling window; syntax/parsing passed.
Ordinary capability and unrelated gates remained denied.

Filesystem-only polling continued every 30 seconds. The initial inline grant did
not open the exact-line gate. The leader superseded it with this exact block:

```text
GRANTED nr-11b
start: 2026-11-01 18:37 local
hard end: 2026-11-01 19:05 local
```

The file was reread and reparsed; no cached 18:41 end was used. After one 30-second
wait for start, admission was `2026-11-02T00:37:01.139203Z` (18:37:01 local).
Live DISPLAY29 bounds/work matched (-1920,1506,0,2586)/(-1920,1506,0,2538).
Primary DISPLAY3 remained 3840x2160; primary/top-left were not controlled.

The single prelaunch sampling window was capped at 60 seconds and passed its
first sample:

```json
{"sampleOrder": 1, "held_virtual_keys": [], "inputIdleMs": 13969, "elapsedSeconds": 0.001}
```

No-held-keys was the leader's quiet-sample condition; downstream 1500ms idle,
immutable epoch, identity and foreground guards stayed unchanged. No operator input
was requested by the agent. Legacy fixed operator prompts appeared in private runner
stdout; preparation was automatic and refusal never fell back to manual activation
or synthesized input.

Trusted detached parent: `ss-uno018-ui-1761f079048a1cec303242e18352c445`,
nr11_observer_complete=1, empty stderr. Reused helper log prefixes remain nr11;
the exact grant and this section identify nr-11b.

Fresh restore → abort → owner-refusal controls passed. Actual returned same-source/
artifact RestorativeSessionObservation objects stayed in this parent's RAM; no
directory/JSON reconstruction supplied authority. Abort deliberately stopped only
its owned worker/target Job after publication; guardian recovery passed. Owner-refusal
tested same-PID owned guardian HWND change, not foreign clipboard publication.

Seven fixed authorized clip.exe baseline seeds occurred only with zero owned app/
recovery processes and no pending recovery: once before controls and before each
of six independent positives. Every positive ran exactly once despite earlier
positive failures. Copy failure never supplied restorative witness authority.

### Commands and ledger

```text
# Re-enable nine flags; verify exact diff and all five artifact SHA256. No rebuild.
# Reused nr11 helper basenames, with exact nr-11b grant identity.
python -I -B <scratchpad>/nr11-wait-grant.py
python -I -B <scratchpad>/nr11-admit-window.py > <scratchpad>/nr11b-time-admission.txt && python -I -B <scratchpad>/nr11-preflight.py > <scratchpad>/nr11b-preflight-result.json && python -I -B <scratchpad>/nr11-run.py > <scratchpad>/nr11b-launch.txt
# Trusted parent: fresh controls, six scenes via actual native runner, last picker once.
# Close nine flags and copied UI gate; grep proof; then:
git diff --exit-code -- <six temporarily changed tracked source files>
pwsh -NoProfile -File <scratchpad>/nr11-process-check.ps1
python -I -B <scratchpad>/nr11-clipboard-check.py
# SCREEN-DONE, then filesystem-only:
python -I -B <scratchpad>/nr11-evidence-check.py ss-uno018-ui-1761f079048a1cec303242e18352c445
```

| Phase / step | Result | Independent usage / clipboard epoch |
|---|---|---|
| 0 reuse | PASS | all five hashes and exact diff match; no rebuild |
| preflight | PASS | first sample quiet; no input synthesis |
| 1 restore, abort, owner-refusal | PASS each | 0 rows / 0 usage; positive epoch predicate not applicable |
| 2 posted-row, posted-main | FAIL each | 1 row / usage 0 FAIL; unchanged insertion epoch FAIL |
| 2 popup-copy, editor-copy, footer-copy | FAIL each | 1 row / usage 0 PASS only; unchanged Copy epoch FAIL |
| 2 context-insert | FAIL | 1 row / usage 0 FAIL; unchanged insertion epoch FAIL |
| posted-enter / registered-* | NOT RUN | excluded; no synthetic Enter/physical shortcut |
| 3 Import Cancel | FAIL | 0 rows / 0 usage; clipboard epoch unchanged |
| Later picker phases | NOT RUN | excluded by explicit stop |
| Release | PASS | flags closed; zero processes; synthetic baseline verified |

Every positive failed at guardian_ready / ForegroundRefused during preparation,
before target foreground grant or intended consumer. All combined boundary checks
FAIL. Zero Copy usage is not Copy acceptance. No editor Save-failure trigger or
native paste was reached. Proof limit remains
**registered_posted_callback_not_physical**, not physical shortcut/pointer/menu proof.

### Window finding — same-PID other, not B or an identified provider

All six sampled after_focus=owned_a / target018_class, then A changed to
a_other_window / toplevel_other / class_token=other. Every other-window trace
sample and all six latched monitor facts agreed:
visible=true, toolwindow=false, noactivate=false, has_owner=false,
owner_is_a=false, owner_is_b=false, is_message_only=false,
same_thread_as_a=true, cloaked=false, cloaked_known=true.
Monitors: sampled=true, relation=same_pid_other, predicate=foreground_foreign,
closed=false. No owned_b observed; the monitor checks exact A and B before
same_pid_other, so its refused sample was neither pinned A nor B.

| Scene | First other trace order / elapsed | Final trace order / elapsed | Monitor sampleOrder |
|---|---|---|---|
| posted-row | 2 / 21ms | 17 / 248ms | 28 |
| posted-main | 2 / 26ms | 16 / 240ms | 27 |
| popup-copy | 3 / 37ms | 17 / 250ms | 32 |
| editor-copy | 3 / 41ms | 16 / 240ms | 29 |
| footer-copy | 2 / 27ms | 15 / 243ms | 26 |
| context-insert | 3 / 30ms | 17 / 245ms | 24 |

There were 98 full trace lines. All initial_foreground values were false; no trace
sampled B, known helper/IME/tooltip/dialog/menu token, worker, guardian, coordinator
or launcher. The fixed other token does not identify the raw class or caller.
Multi-API facts/independent observers are not atomic; identical facts do not prove
one unchanged HWND persisted, and between-sample transitions remain unknown.
Refusal was latched by the original exact-A check, not rescued by observation.
No same-PID allowlist, extra activation, focus retry, epoch rebaseline or speculative
Target018 fix is justified by this run.


### Exact per-scene diagnostics — nr-11b

All nine native streams: privacy CLEAN, planted negative controls PASS, empty
stderr, no pending recovery receipt. Trace emission timestamps are buffered;
use elapsed_ms for sample timing. Monitor and trace counters are independent.
FailureStage is the last-entered stage, not an exact failed-predicate locator.

#### restore — PASS

Session: `ss-uno018-ui-0bed142362a1538adc6442e7045d5ec5/ss-uno019-native-3d0c1796f44842f1916de4c56b904fb9`.
FailureStage: `none`.
Target foreground grant: **absent** (no target_foreground_grant line).
Rows/usage: 0/0; independent usage **PASS**.
Positive clipboard-epoch predicate: not applicable to this control.
Receipt/host check: **PASS**; host shutdown-complete true.
Monitor failure: absent.

```text
2026-11-02T00:37:07.805Z INFO native_startup_foreground phase=target_launch relation=foreign launcher_foreground=False sample_order=1 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=False cloaked=False cloaked_known=True
2026-11-02T00:37:07.805Z INFO native_startup_foreground phase=host_ready relation=foreign launcher_foreground=False sample_order=2 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=False cloaked=False cloaked_known=True
2026-11-02T00:37:07.806Z INFO native_host_readiness reason=ready
2026-11-02T00:37:08.155Z INFO native_control_admission reason=Ready
2026-11-02T00:37:08.255Z INFO native_control_publish status=swapped
```

#### abort — PASS

Session: `ss-uno018-ui-e85d55760b787ebeee2a59167cfcd1ce/ss-uno019-native-d96ee4029c724412b8745f733ddeefa0`.
FailureStage: `none`.
Target foreground grant: **absent** (no target_foreground_grant line).
Rows/usage: 0/0; independent usage **PASS**.
Positive clipboard-epoch predicate: not applicable to this control.
Receipt/host check: **PASS**; host shutdown-complete false.
Monitor failure: absent.

```text
2026-11-02T00:37:14.249Z INFO native_startup_foreground phase=target_launch relation=foreign launcher_foreground=False sample_order=1 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=False cloaked=False cloaked_known=True
2026-11-02T00:37:14.250Z INFO native_startup_foreground phase=host_ready relation=foreign launcher_foreground=False sample_order=2 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=False cloaked=False cloaked_known=True
2026-11-02T00:37:14.250Z INFO native_host_readiness reason=ready
2026-11-02T00:37:15.680Z INFO native_control_admission reason=Ready
2026-11-02T00:37:15.754Z INFO native_control_publish status=swapped
```

#### owner-refusal — PASS

Session: `ss-uno018-ui-0199271d949120aaec369b77d7f3e740/ss-uno019-native-37f523e9eff54372acff9fb7be195645`.
FailureStage: `none`.
Target foreground grant: **absent** (no target_foreground_grant line).
Rows/usage: 0/0; independent usage **PASS**.
Positive clipboard-epoch predicate: not applicable to this control.
Receipt/host check: **PASS**; host shutdown-complete true.
Monitor failure: absent.

```text
2026-11-02T00:37:21.413Z INFO native_startup_foreground phase=target_launch relation=foreign launcher_foreground=False sample_order=1 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=False cloaked=False cloaked_known=True
2026-11-02T00:37:21.413Z INFO native_startup_foreground phase=host_ready relation=foreign launcher_foreground=False sample_order=2 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=False cloaked=False cloaked_known=True
2026-11-02T00:37:21.413Z INFO native_host_readiness reason=ready
2026-11-02T00:37:21.725Z INFO native_control_admission reason=Ready
2026-11-02T00:37:21.835Z INFO native_control_publish status=swapped
```

#### posted-row — FAIL

Session: `ss-uno018-ui-b705d875579807988c0a09ae66e1c43e/ss-uno019-native-5d44fac9920147d5affeee2327a4a934`.
FailureStage: `guardian_ready`.
Target foreground grant: **absent** (no target_foreground_grant line).
Rows/usage: 1/0; independent usage **FAIL**.
Independent outer clipboard epoch **unchanged**;
combined boundary **FAIL**.
Receipt/host check: **FAIL_ValueError**; host shutdown-complete false.
Full native-monitor-failure.json, verbatim:

```json
{"predicate": "foreground_foreign", "exitCode": 2, "sampleOrder": 28, "relation": "same_pid_other", "closed": false, "windowFacts": {"class_token": "other", "sampled": true, "visible": true, "toolwindow": false, "noactivate": false, "has_owner": false, "owner_is_a": false, "owner_is_b": false, "is_message_only": false, "same_thread_as_a": true, "cloaked": false, "cloaked_known": true}}
```

```text
2026-11-02T00:37:31.177Z INFO native_startup_foreground phase=target_launch relation=foreign launcher_foreground=False sample_order=1 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=False cloaked=False cloaked_known=True
2026-11-02T00:37:31.178Z INFO native_startup_foreground phase=host_ready relation=foreign launcher_foreground=False sample_order=2 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=False cloaked=False cloaked_known=True
2026-11-02T00:37:31.178Z INFO native_host_readiness reason=ready
2026-11-02T00:37:31.367Z INFO owned_initial_focus phase=waiting
2026-11-02T00:37:31.368Z INFO native_startup_foreground phase=preparation_start relation=foreign launcher_foreground=False sample_order=3 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=False cloaked=False cloaked_known=True
2026-11-02T00:37:31.409Z INFO owned_preparation_admission reason=ready
2026-11-02T00:37:31.544Z INFO native_startup_foreground phase=before_focus relation=foreign launcher_foreground=False sample_order=4 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=False cloaked=False cloaked_known=True
2026-11-02T00:37:31.600Z INFO native_startup_foreground phase=after_focus relation=owned_a launcher_foreground=False sample_order=5 class_token=target018_class visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True
2026-11-02T00:37:31.863Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=1 elapsed_ms=0 class_token=target018_class visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True
2026-11-02T00:37:31.864Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=2 elapsed_ms=21 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True
2026-11-02T00:37:31.864Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=3 elapsed_ms=36 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True
2026-11-02T00:37:31.864Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=4 elapsed_ms=52 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True
2026-11-02T00:37:31.864Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=5 elapsed_ms=67 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True
2026-11-02T00:37:31.865Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=6 elapsed_ms=82 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True
2026-11-02T00:37:31.865Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=7 elapsed_ms=97 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True
2026-11-02T00:37:31.865Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=8 elapsed_ms=112 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True
2026-11-02T00:37:31.865Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=9 elapsed_ms=127 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True
2026-11-02T00:37:31.865Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=10 elapsed_ms=142 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True
2026-11-02T00:37:31.865Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=11 elapsed_ms=157 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True
2026-11-02T00:37:31.866Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=12 elapsed_ms=172 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True
2026-11-02T00:37:31.866Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=13 elapsed_ms=187 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True
2026-11-02T00:37:31.866Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=14 elapsed_ms=202 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True
2026-11-02T00:37:31.866Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=15 elapsed_ms=217 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True
2026-11-02T00:37:31.866Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=16 elapsed_ms=232 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True
2026-11-02T00:37:31.866Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=17 elapsed_ms=248 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True
2026-11-02T00:37:31.867Z INFO owned_target_preparation reason=ForegroundRefused
2026-11-02T00:37:31.867Z INFO owned_initial_focus phase=refused
2026-11-02T00:37:31.868Z ERROR native_positive_failed stage=guardian_ready InvalidOperationException (HResult=0x80131509)
```

#### posted-main — FAIL

Session: `ss-uno018-ui-81e1eaf20dc12112e9b85601ece16c9a/ss-uno019-native-fa9f7d993d7d4af489a0d7837eb987a5`.
FailureStage: `guardian_ready`.
Target foreground grant: **absent** (no target_foreground_grant line).
Rows/usage: 1/0; independent usage **FAIL**.
Independent outer clipboard epoch **unchanged**;
combined boundary **FAIL**.
Receipt/host check: **FAIL_ValueError**; host shutdown-complete false.
Full native-monitor-failure.json, verbatim:

```json
{"predicate": "foreground_foreign", "exitCode": 2, "sampleOrder": 27, "relation": "same_pid_other", "closed": false, "windowFacts": {"class_token": "other", "sampled": true, "visible": true, "toolwindow": false, "noactivate": false, "has_owner": false, "owner_is_a": false, "owner_is_b": false, "is_message_only": false, "same_thread_as_a": true, "cloaked": false, "cloaked_known": true}}
```

```text
2026-11-02T00:37:38.035Z INFO native_startup_foreground phase=target_launch relation=foreign launcher_foreground=False sample_order=1 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=False cloaked=False cloaked_known=True
2026-11-02T00:37:38.035Z INFO native_startup_foreground phase=host_ready relation=foreign launcher_foreground=False sample_order=2 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=False cloaked=False cloaked_known=True
2026-11-02T00:37:38.036Z INFO native_host_readiness reason=ready
2026-11-02T00:37:38.305Z INFO owned_initial_focus phase=waiting
2026-11-02T00:37:38.305Z INFO native_startup_foreground phase=preparation_start relation=foreign launcher_foreground=False sample_order=3 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=False cloaked=False cloaked_known=True
2026-11-02T00:37:38.345Z INFO owned_preparation_admission reason=ready
2026-11-02T00:37:38.483Z INFO native_startup_foreground phase=before_focus relation=foreign launcher_foreground=False sample_order=4 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=False cloaked=False cloaked_known=True
2026-11-02T00:37:38.523Z INFO native_startup_foreground phase=after_focus relation=owned_a launcher_foreground=False sample_order=5 class_token=target018_class visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True
2026-11-02T00:37:38.779Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=1 elapsed_ms=0 class_token=target018_class visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True
2026-11-02T00:37:38.779Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=2 elapsed_ms=26 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True
2026-11-02T00:37:38.779Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=3 elapsed_ms=42 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True
2026-11-02T00:37:38.779Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=4 elapsed_ms=57 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True
2026-11-02T00:37:38.779Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=5 elapsed_ms=72 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True
2026-11-02T00:37:38.780Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=6 elapsed_ms=86 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True
2026-11-02T00:37:38.780Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=7 elapsed_ms=102 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True
2026-11-02T00:37:38.780Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=8 elapsed_ms=118 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True
2026-11-02T00:37:38.780Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=9 elapsed_ms=132 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True
2026-11-02T00:37:38.780Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=10 elapsed_ms=148 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True
2026-11-02T00:37:38.780Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=11 elapsed_ms=164 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True
2026-11-02T00:37:38.780Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=12 elapsed_ms=180 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True
2026-11-02T00:37:38.781Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=13 elapsed_ms=195 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True
2026-11-02T00:37:38.781Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=14 elapsed_ms=210 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True
2026-11-02T00:37:38.781Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=15 elapsed_ms=225 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True
2026-11-02T00:37:38.781Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=16 elapsed_ms=240 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True
2026-11-02T00:37:38.782Z INFO owned_target_preparation reason=ForegroundRefused
2026-11-02T00:37:38.782Z INFO owned_initial_focus phase=refused
2026-11-02T00:37:38.782Z ERROR native_positive_failed stage=guardian_ready InvalidOperationException (HResult=0x80131509)
```

#### popup-copy — FAIL

Session: `ss-uno018-ui-f30e2ae301605c704dd0585b56514bfd/ss-uno019-native-4b0c19c17c144d18ab99b3fd827961a0`.
FailureStage: `guardian_ready`.
Target foreground grant: **absent** (no target_foreground_grant line).
Rows/usage: 1/0; independent usage **PASS**.
Independent outer clipboard epoch **unchanged**;
combined boundary **FAIL**.
Receipt/host check: **FAIL_ValueError**; host shutdown-complete false.
Full native-monitor-failure.json, verbatim:

```json
{"predicate": "foreground_foreign", "exitCode": 2, "sampleOrder": 32, "relation": "same_pid_other", "closed": false, "windowFacts": {"class_token": "other", "sampled": true, "visible": true, "toolwindow": false, "noactivate": false, "has_owner": false, "owner_is_a": false, "owner_is_b": false, "is_message_only": false, "same_thread_as_a": true, "cloaked": false, "cloaked_known": true}}
```

```text
2026-11-02T00:37:45.233Z INFO native_startup_foreground phase=target_launch relation=foreign launcher_foreground=False sample_order=1 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=False cloaked=False cloaked_known=True
2026-11-02T00:37:45.233Z INFO native_startup_foreground phase=host_ready relation=foreign launcher_foreground=False sample_order=2 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=False cloaked=False cloaked_known=True
2026-11-02T00:37:45.234Z INFO native_host_readiness reason=ready
2026-11-02T00:37:45.641Z INFO owned_initial_focus phase=waiting
2026-11-02T00:37:45.642Z INFO native_startup_foreground phase=preparation_start relation=foreign launcher_foreground=False sample_order=3 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=False cloaked=False cloaked_known=True
2026-11-02T00:37:45.681Z INFO owned_preparation_admission reason=ready
2026-11-02T00:37:45.819Z INFO native_startup_foreground phase=before_focus relation=foreign launcher_foreground=False sample_order=4 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=False cloaked=False cloaked_known=True
2026-11-02T00:37:45.853Z INFO native_startup_foreground phase=after_focus relation=owned_a launcher_foreground=False sample_order=5 class_token=target018_class visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True
2026-11-02T00:37:46.104Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=1 elapsed_ms=0 class_token=target018_class visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True
2026-11-02T00:37:46.105Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=2 elapsed_ms=22 class_token=target018_class visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True
2026-11-02T00:37:46.105Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=3 elapsed_ms=37 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True
2026-11-02T00:37:46.105Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=4 elapsed_ms=52 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True
2026-11-02T00:37:46.105Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=5 elapsed_ms=68 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True
2026-11-02T00:37:46.105Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=6 elapsed_ms=83 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True
2026-11-02T00:37:46.105Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=7 elapsed_ms=98 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True
2026-11-02T00:37:46.106Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=8 elapsed_ms=114 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True
2026-11-02T00:37:46.106Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=9 elapsed_ms=129 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True
2026-11-02T00:37:46.106Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=10 elapsed_ms=144 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True
2026-11-02T00:37:46.106Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=11 elapsed_ms=158 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True
2026-11-02T00:37:46.106Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=12 elapsed_ms=173 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True
2026-11-02T00:37:46.107Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=13 elapsed_ms=189 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True
2026-11-02T00:37:46.107Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=14 elapsed_ms=205 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True
2026-11-02T00:37:46.107Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=15 elapsed_ms=220 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True
2026-11-02T00:37:46.107Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=16 elapsed_ms=235 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True
2026-11-02T00:37:46.107Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=17 elapsed_ms=250 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True
2026-11-02T00:37:46.108Z INFO owned_target_preparation reason=ForegroundRefused
2026-11-02T00:37:46.108Z INFO owned_initial_focus phase=refused
2026-11-02T00:37:46.109Z ERROR native_positive_failed stage=guardian_ready InvalidOperationException (HResult=0x80131509)
```

#### editor-copy — FAIL

Session: `ss-uno018-ui-a53240fa85b17d1dadf928b53df14a6b/ss-uno019-native-4674b6722d1c482b9abbf69697ffb28b`.
FailureStage: `guardian_ready`.
Target foreground grant: **absent** (no target_foreground_grant line).
Rows/usage: 1/0; independent usage **PASS**.
Independent outer clipboard epoch **unchanged**;
combined boundary **FAIL**.
Receipt/host check: **FAIL_ValueError**; host shutdown-complete false.
Full native-monitor-failure.json, verbatim:

```json
{"predicate": "foreground_foreign", "exitCode": 2, "sampleOrder": 29, "relation": "same_pid_other", "closed": false, "windowFacts": {"class_token": "other", "sampled": true, "visible": true, "toolwindow": false, "noactivate": false, "has_owner": false, "owner_is_a": false, "owner_is_b": false, "is_message_only": false, "same_thread_as_a": true, "cloaked": false, "cloaked_known": true}}
```

```text
2026-11-02T00:37:52.070Z INFO native_startup_foreground phase=target_launch relation=foreign launcher_foreground=False sample_order=1 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=False cloaked=False cloaked_known=True
2026-11-02T00:37:52.070Z INFO native_startup_foreground phase=host_ready relation=foreign launcher_foreground=False sample_order=2 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=False cloaked=False cloaked_known=True
2026-11-02T00:37:52.070Z INFO native_host_readiness reason=ready
2026-11-02T00:37:52.394Z INFO owned_initial_focus phase=waiting
2026-11-02T00:37:52.394Z INFO native_startup_foreground phase=preparation_start relation=foreign launcher_foreground=False sample_order=3 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=False cloaked=False cloaked_known=True
2026-11-02T00:37:52.439Z INFO owned_preparation_admission reason=ready
2026-11-02T00:37:52.584Z INFO native_startup_foreground phase=before_focus relation=foreign launcher_foreground=False sample_order=4 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=False cloaked=False cloaked_known=True
2026-11-02T00:37:52.620Z INFO native_startup_foreground phase=after_focus relation=owned_a launcher_foreground=False sample_order=5 class_token=target018_class visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True
2026-11-02T00:37:52.876Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=1 elapsed_ms=0 class_token=target018_class visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True
2026-11-02T00:37:52.876Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=2 elapsed_ms=26 class_token=target018_class visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True
2026-11-02T00:37:52.877Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=3 elapsed_ms=41 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True
2026-11-02T00:37:52.877Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=4 elapsed_ms=56 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True
2026-11-02T00:37:52.877Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=5 elapsed_ms=71 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True
2026-11-02T00:37:52.877Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=6 elapsed_ms=87 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True
2026-11-02T00:37:52.877Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=7 elapsed_ms=103 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True
2026-11-02T00:37:52.877Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=8 elapsed_ms=118 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True
2026-11-02T00:37:52.877Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=9 elapsed_ms=134 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True
2026-11-02T00:37:52.877Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=10 elapsed_ms=149 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True
2026-11-02T00:37:52.878Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=11 elapsed_ms=164 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True
2026-11-02T00:37:52.878Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=12 elapsed_ms=179 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True
2026-11-02T00:37:52.878Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=13 elapsed_ms=195 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True
2026-11-02T00:37:52.878Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=14 elapsed_ms=210 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True
2026-11-02T00:37:52.878Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=15 elapsed_ms=225 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True
2026-11-02T00:37:52.878Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=16 elapsed_ms=240 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True
2026-11-02T00:37:52.880Z INFO owned_target_preparation reason=ForegroundRefused
2026-11-02T00:37:52.880Z INFO owned_initial_focus phase=refused
2026-11-02T00:37:52.880Z ERROR native_positive_failed stage=guardian_ready InvalidOperationException (HResult=0x80131509)
```

#### footer-copy — FAIL

Session: `ss-uno018-ui-61a87b95675f5ae12801d6d1ab7615bc/ss-uno019-native-f56eddd75bc84161817f41620582c4a1`.
FailureStage: `guardian_ready`.
Target foreground grant: **absent** (no target_foreground_grant line).
Rows/usage: 1/0; independent usage **PASS**.
Independent outer clipboard epoch **unchanged**;
combined boundary **FAIL**.
Receipt/host check: **FAIL_ValueError**; host shutdown-complete false.
Full native-monitor-failure.json, verbatim:

```json
{"predicate": "foreground_foreign", "exitCode": 2, "sampleOrder": 26, "relation": "same_pid_other", "closed": false, "windowFacts": {"class_token": "other", "sampled": true, "visible": true, "toolwindow": false, "noactivate": false, "has_owner": false, "owner_is_a": false, "owner_is_b": false, "is_message_only": false, "same_thread_as_a": true, "cloaked": false, "cloaked_known": true}}
```

```text
2026-11-02T00:37:58.937Z INFO native_startup_foreground phase=target_launch relation=foreign launcher_foreground=False sample_order=1 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=False cloaked=False cloaked_known=True
2026-11-02T00:37:58.938Z INFO native_startup_foreground phase=host_ready relation=foreign launcher_foreground=False sample_order=2 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=False cloaked=False cloaked_known=True
2026-11-02T00:37:58.938Z INFO native_host_readiness reason=ready
2026-11-02T00:37:59.227Z INFO owned_initial_focus phase=waiting
2026-11-02T00:37:59.228Z INFO native_startup_foreground phase=preparation_start relation=foreign launcher_foreground=False sample_order=3 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=False cloaked=False cloaked_known=True
2026-11-02T00:37:59.267Z INFO owned_preparation_admission reason=ready
2026-11-02T00:37:59.399Z INFO native_startup_foreground phase=before_focus relation=foreign launcher_foreground=False sample_order=4 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=False cloaked=False cloaked_known=True
2026-11-02T00:37:59.435Z INFO native_startup_foreground phase=after_focus relation=owned_a launcher_foreground=False sample_order=5 class_token=target018_class visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True
2026-11-02T00:37:59.694Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=1 elapsed_ms=0 class_token=target018_class visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True
2026-11-02T00:37:59.695Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=2 elapsed_ms=27 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True
2026-11-02T00:37:59.695Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=3 elapsed_ms=43 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True
2026-11-02T00:37:59.695Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=4 elapsed_ms=59 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True
2026-11-02T00:37:59.695Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=5 elapsed_ms=74 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True
2026-11-02T00:37:59.695Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=6 elapsed_ms=89 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True
2026-11-02T00:37:59.695Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=7 elapsed_ms=104 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True
2026-11-02T00:37:59.695Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=8 elapsed_ms=120 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True
2026-11-02T00:37:59.695Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=9 elapsed_ms=135 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True
2026-11-02T00:37:59.696Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=10 elapsed_ms=165 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True
2026-11-02T00:37:59.696Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=11 elapsed_ms=181 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True
2026-11-02T00:37:59.696Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=12 elapsed_ms=197 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True
2026-11-02T00:37:59.696Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=13 elapsed_ms=212 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True
2026-11-02T00:37:59.696Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=14 elapsed_ms=227 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True
2026-11-02T00:37:59.696Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=15 elapsed_ms=243 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True
2026-11-02T00:37:59.697Z INFO owned_target_preparation reason=ForegroundRefused
2026-11-02T00:37:59.697Z INFO owned_initial_focus phase=refused
2026-11-02T00:37:59.697Z ERROR native_positive_failed stage=guardian_ready InvalidOperationException (HResult=0x80131509)
```

#### context-insert — FAIL

Session: `ss-uno018-ui-f3af1fd01624ef473920299a8629907f/ss-uno019-native-0d5d3e5749454034ac18b4079f5e98b5`.
FailureStage: `guardian_ready`.
Target foreground grant: **absent** (no target_foreground_grant line).
Rows/usage: 1/0; independent usage **FAIL**.
Independent outer clipboard epoch **unchanged**;
combined boundary **FAIL**.
Receipt/host check: **FAIL_ValueError**; host shutdown-complete false.
Full native-monitor-failure.json, verbatim:

```json
{"predicate": "foreground_foreign", "exitCode": 2, "sampleOrder": 24, "relation": "same_pid_other", "closed": false, "windowFacts": {"class_token": "other", "sampled": true, "visible": true, "toolwindow": false, "noactivate": false, "has_owner": false, "owner_is_a": false, "owner_is_b": false, "is_message_only": false, "same_thread_as_a": true, "cloaked": false, "cloaked_known": true}}
```

```text
2026-11-02T00:38:05.442Z INFO native_startup_foreground phase=target_launch relation=foreign launcher_foreground=False sample_order=1 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=False cloaked=False cloaked_known=True
2026-11-02T00:38:05.442Z INFO native_startup_foreground phase=host_ready relation=foreign launcher_foreground=False sample_order=2 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=False cloaked=False cloaked_known=True
2026-11-02T00:38:05.443Z INFO native_host_readiness reason=ready
2026-11-02T00:38:05.619Z INFO owned_initial_focus phase=waiting
2026-11-02T00:38:05.620Z INFO native_startup_foreground phase=preparation_start relation=foreign launcher_foreground=False sample_order=3 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=False cloaked=False cloaked_known=True
2026-11-02T00:38:05.660Z INFO owned_preparation_admission reason=ready
2026-11-02T00:38:05.793Z INFO native_startup_foreground phase=before_focus relation=foreign launcher_foreground=False sample_order=4 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=False cloaked=False cloaked_known=True
2026-11-02T00:38:05.831Z INFO native_startup_foreground phase=after_focus relation=owned_a launcher_foreground=False sample_order=5 class_token=target018_class visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True
2026-11-02T00:38:06.091Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=1 elapsed_ms=0 class_token=target018_class visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True
2026-11-02T00:38:06.092Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=2 elapsed_ms=15 class_token=target018_class visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True
2026-11-02T00:38:06.092Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=3 elapsed_ms=30 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True
2026-11-02T00:38:06.092Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=4 elapsed_ms=46 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True
2026-11-02T00:38:06.092Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=5 elapsed_ms=62 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True
2026-11-02T00:38:06.092Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=6 elapsed_ms=77 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True
2026-11-02T00:38:06.093Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=7 elapsed_ms=92 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True
2026-11-02T00:38:06.093Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=8 elapsed_ms=107 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True
2026-11-02T00:38:06.093Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=9 elapsed_ms=122 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True
2026-11-02T00:38:06.093Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=10 elapsed_ms=138 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True
2026-11-02T00:38:06.093Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=11 elapsed_ms=153 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True
2026-11-02T00:38:06.093Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=12 elapsed_ms=168 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True
2026-11-02T00:38:06.093Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=13 elapsed_ms=183 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True
2026-11-02T00:38:06.093Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=14 elapsed_ms=198 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True
2026-11-02T00:38:06.094Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=15 elapsed_ms=213 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True
2026-11-02T00:38:06.094Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=16 elapsed_ms=229 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True
2026-11-02T00:38:06.094Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=17 elapsed_ms=245 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True
2026-11-02T00:38:06.094Z INFO owned_target_preparation reason=ForegroundRefused
2026-11-02T00:38:06.095Z INFO owned_initial_focus phase=refused
2026-11-02T00:38:06.095Z ERROR native_positive_failed stage=guardian_ready InvalidOperationException (HResult=0x80131509)
```

### Picker Import Cancel — FAIL, nr-11b

Parent: `ss-uno018-ui-e96d16a55f53206410b419fb47b49619`.
Session: `ss-uno018-ui-72264100c667e14abfeff0bfa6b5f2d4`.
Wrapper/child exit 1. Complete fixed failure records:

observer-parent-failure.json:

```json
{"reason": "worker_protocol"}
```

observer-worker-failure.json:

```json
{"reason": "input", "inputSubreason": "epoch_changed", "inputEpochDelta": 7078, "inputIdleMs": 15, "inputPhase": "unspecified"}
```

Refusal: reason=input, inputSubreason=epoch_changed, inputEpochDelta=7078,
inputIdleMs=15, inputPhase=unspecified. The delta is unsigned timestamp subtraction,
not an event count or proof of human/system origin. The unspecified phase must not
be rewritten as import-cancel merely because that active marker exists.

Helper stdout/stderr were empty: no operationClass/pickerStep/HRESULT line,
no picker_schema_multiple/per-match or foreground-failure record. Every
picker-invoke-<phase>-<open|commit>.json file was absent, including Import Cancel
open and commit: **no helper-side before/after/completed epochs available**.
This cannot prove an unchanged epoch or localize the change to open versus Cancel.
The observer may have stopped the helper before a bracket could persist; that is
incomplete evidence, not attribution.

Import Cancel active marker exists; closed/actions/ui-import-cancelled receipt
absent. All later phase markers absent; explicit throw 'actions' was not reached.
One picker attempt only, no later Import/Export actions or mutation retry.
Picker privacy CLEAN, planted controls PASS, aggregate rows/usage 0/0;
independent clipboard epoch unchanged. Existing owned helper/fixture failure
cleanup completed; no graceful failed-host or pre-kill JobZero success claimed.
The wrapper's historical phase2 field is not this brief's phase number.


### nr-11b release — PASS

All nine tracked flags and copied UI gate false; tracked-source diff empty.
The tracked grep proof repeats the original false lines above; full second proof
retained in nr11b-flags-closed.txt. Final process record:

```json
{"observedUtc":"2026-11-02T00:38:37.1810683+00:00","counts":{"SmartSnippets.UnoCandidate.exe":0,"SmartSnippets.UnoAcceptanceFixture.exe":0,"SmartSnippets.UnoCandidate.Guardian.exe":0,"SmartSnippets.Target018.exe":0,"nr11PythonHelpers":0,"UIHelper":0}}
```

Final locked RAM-only clipboard check, without post-run reseed:

```json
{"builtin_text": 4, "ownership_marker": 0, "protective_metadata": 0, "unsupported": 0, "owner_present": false, "authorized_synthetic_baseline_matches": true, "sequence_stable": true}
```

This verifies the authorized synthetic baseline, not user data preceding the first
seed. No ownership/protective marker, unsupported format, clipboard owner or
unsettled coordinator/guardian/keyboard lifetime remained. No pending recovery
process was killed; no synthetic release/recovery endpoint used. Picker failure
cleanup is not graceful success.

**SCREEN-DONE nr-11b 2026-11-01 18:38:37 local (-06:00)** was recorded immediately
after closure checks, before hard end 19:05. No desktop/clipboard work followed.
Remaining work was filesystem evidence/documentation. Only this research doc is
committed; operator-local AGENTS/safety-guide edits untouched. No push, warning
suppression, ordinary unlock or parity claim.

Pending: identify the unrecognized visible same-thread target-PID surface and
picker epoch source with separately reviewed diagnostics, then separately authorized
native acceptance for main/Copy/context and remaining fault/lifecycle/pointer/hover/
tray/oldest-runtime obligations. No active recovery or operator action remains.
These failures do not close Plans 018/019 or authorize another attempt.
