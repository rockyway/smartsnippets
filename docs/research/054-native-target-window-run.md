# 054 — nr-11: Target018 diagnostic run refused at preflight

Authorized brief `native-run12-brief.md`, 2026-11-01. **Phase 0 PASS;
phase 1 FAIL at held-input preflight; all native scenes NOT RUN. Release PASS.**
No clipboard access, application launch, UIA action or input synthesis occurred.
Research 053's Target018 classification and picker-epoch questions remain unresolved;
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
