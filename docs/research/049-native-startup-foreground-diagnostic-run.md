# 049 — nr-08: baseline admission FAIL; native diagnostics BLOCKED

Authorized brief `native-run9-brief.md`, 2026-11-01. Phase 0 passed. The baseline
seed's process guard refused **before clipboard mutation or a native session**;
therefore no same-artifact control witness existed and all positive scenes were
blocked. The independent final Import Cancel attempt failed once. Release passed.
Plans 018/019 remain incomplete; no production, startup-foreground, physical-input,
Copy, insertion or picker acceptance is claimed.

## Authorization, preparation and screen gate

- Worktree `.claude/worktrees/uno019-native-trial7`, branch
  `feature/uno019-native-trial7`, starting HEAD
  `8bcf79ca626c0be914cb8753a389f1086331aafc`.
- Read AGENTS, safety guide 002, progress handoff 003, approved Plan 019,
  research 041–045/047–048, both fixture READMEs and nr-07 helpers/report.
  Operator edits to AGENTS and guide 002 were preserved, never edited/staged.
- Phase 0 was filesystem/build-only: **no app launch, window opening or clipboard
  access before the stated start**. Five fresh native builds/hashes passed before
  the grant. No test-suite rerun or legacy publish is claimed.
- Filesystem-only `nr08-wait-grant.py` checked every 30 seconds. On poll 25,
  after 720.017 seconds, at `2026-11-01T22:53:37.360511Z`, it observed the exact
  line `GRANTED nr-08`. Grant notification was not desktop admission.
- The grant specified **exclusive 16:55–17:15 local**, bottom-left only; no
  DENIED/REVOKED was observed. A second filesystem/time-only helper continued
  30-second waiting and admitted at `22:55:21.192423Z` (16:55:21 local).
  Start/end were parsed from the grant, not inherited from nr-07 or restarted
  as a new duration. Normal admission reserved three minutes; picker required
  at least five minutes remaining.
- Live PMv2 geometry confirmed nonprimary bottom-left bounds
  `(-1920,1506,0,2586)`, work `(-1920,1506,0,2538)`. Actual primary was
  `(0,0,3840,2160)`, not the brief's stale primary dimensions. DISPLAY names
  were not identity authority. No primary/top-left window was controlled or
  moved; no placement helper change was required. Held-input preflight was clear.

Grant scope included controls, posted/captured-Copy/context scenes, and optional
Import Cancel only. It expressly prohibited keyboard/mouse synthesis and
registered-* scenes. `posted-enter` additionally has an independent scope blocker:
`PositiveHostScene.cs:225–234` calls `PositiveNativeInput.Enter`, whose
`PositiveNativeInput.cs:26` uses `SendInput(2, ...)`. That is harness Enter
synthesis, not the separately permitted product guarded paste. No input guard was
weakened and no operator clarification/action was requested. In this run all
positives were already blocked by the missing controls, so no Enter scene or
synthetic input was attempted.

## Phase 0 — PASS: nine flags, five fresh strict artifacts

Exactly these nine tracked trial booleans were temporarily enabled, then restored:

| File | Constants |
|---|---|
| `scripts/run_uno_candidate_native.py` | `NATIVE_SCENES_SOURCE_REVIEWED`, `POSITIVE_SCENES_SOURCE_REVIEWED`, `POSITIVE_INPUT_SOURCE_REVIEWED` |
| `scripts/run_uno_candidate_ui.py` | `UI_SOURCE_REVIEWED` |
| `scripts/uno_candidate_native_fixture/Program.cs` | `NativeFixtureSourceReviewed` |
| `scripts/uno_candidate_native_fixture/PositiveProtocol.cs` | `SourceReviewed`, `InputSourceReviewed` |
| `scripts/uno_candidate_native_fixture/OwnedTargetPreparation.cs` | `SourceReviewed` |
| `src/SmartSnippets.Uno/Platforms/Windows/AcceptanceNative/NativeAcceptanceFactory.cs` | `SourceReviewed` |

Exact enabled diff: private `nr08-enabled-flags.diff`, SHA256
`711b3e9ee707c5dae3d61f4612646948f5d8b83c625974764084c6beb9eb468d`.
Startup/full-host/prototype execution and ordinary candidate capabilities remained
closed. Original nr-07 helpers were never edited; nr-08 copies are scratch-only.

Commands ran sequentially from the worktree root; `<scratchpad>` denotes the
private brief directory:

```text
pwsh -NoProfile -File scripts/uno_candidate_native_fixture/build.ps1 -BuildOnly
pwsh -NoProfile -File <scratchpad>/nr08-build-support.ps1
pwsh -NoProfile -File scripts/verify-uno-candidate.ps1 -BuildOnly
pwsh -NoProfile -File scripts/publish-uno-candidate-aot.ps1
python -I -B scripts/run_uno_candidate_native.py --validate-build <native-entry> <native-sha256>
python -I -B <scratchpad>/nr08-artifacts.py
```

Both linked fixtures and ordinary candidate passed existing strict native x64,
SQLite, effective-property/reference/freshness gates. Support outputs were new
owner-private directories, cleaned and published with `NoWarn=`, AOT/trim/
compatibility/analyzers/warnings-as-errors true and trim-warning suppression/
single-warning aggregation false. All five warning/error scans passed; all five
SHA256 values were independently recomputed after execution and matched. No
warning suppression, weakened AOT/trim gate or reused executable. Ordinary
candidate was **build-only, never launched**. Scratch Python syntax and narrowed
picker PowerShell parsing passed.

| Artifact | Fresh token | SHA256 |
|---|---|---|
| Native fixture | `1611c2f81d2d4cf68cac1ef4ae2c0fa3` | `389ac3dbe9caefe4925dc5b618e5d901db3d51b9cf2eba0494f2a7d5c976b18b` |
| Target018 | `ac333c635c2e45d7bfb83ee211435c4f` | `548d1e37a4fddf0b2c0e2a67655c82ee894b32789aef952509bfa23b9cf3ac0c` |
| Guardian | `d3baf413071d4828a64b5e802c5c9a7f` | `ae43a6456e179ccb23896347c6ccf180ecb4ea6838461c2fc8bfd2af4779d473` |
| Management fixture | `46a05156254f4749839e057528b4b211` | `108e0c8c9d94ab30e2e676473d698870da60c56128d56ffcd4239399b9208b3e` |
| Ordinary candidate, not launched | `2eb6dfad7ecd4d15a3e6028bc3a055a5` | `7dd7ab2a4d12657a019cd27f57eea5bcbdf80947827f9541fb08b778eda7f20c` |

Native enabled-source receipt:
`ae679be46f84073adc78869df2aebd4b10fd8a663f092ece80a377c370c4d5ce`.
Management base source receipt:
`0a39df409fd6d8bd2dd842493d349c88b574390419fb02ccd9b0c6ebda56c21a`.
Fixture outputs use `bin/publish-native/<token>` and
`bin/publish-acceptance/<token>`; support outputs use `bin/nr08-<token>` beneath
the respective prototype projects. Ordinary candidate output is
`src/SmartSnippets.Uno/bin/publish-uno-candidate/<token>`. Retained enabled
binaries are private evidence, not permission to launch against closed source.

## Execution and per-step ledger

```text
python -I -B <scratchpad>/nr08-wait-grant.py
python -I -B <scratchpad>/nr08-admit-window.py
python -I -B <scratchpad>/nr08-preflight.py
pwsh -NoProfile -File <scratchpad>/nr08-process-check.ps1
pwsh -NoProfile -File <scratchpad>/nr08-seed.ps1
# Seed refused before clip.exe; && chain did not run clipboard-check.py or run.py.
# After phase-1 stop and another zero-process check, independent final phase:
python -I -B <scratchpad>/nr08-picker.py
# Flags closed and process/clipboard release checks; filesystem-only evidence:
python -I -B <scratchpad>/nr08-picker-evidence.py
python -I -B <scratchpad>/nr08-artifacts.py
```

| Phase/step | Result |
|---|---|
| 0: flags, builds, artifact/hash checks | PASS |
| 1: baseline admission | FAIL: `owned_process_retained`, before seed/write/session |
| 1: restore | BLOCKED / NOT RUN after prerequisite failure |
| 1: abort | BLOCKED / NOT RUN |
| 1: owner-refusal | BLOCKED / NOT RUN; no fresh control witness |
| 2: posted-row | BLOCKED / NOT RUN: no phase-1 witness |
| 2: posted-main | BLOCKED / NOT RUN: same prerequisite |
| 2: posted-enter | BLOCKED / NOT RUN: same prerequisite; synthesis also prohibited |
| 2: popup-copy | BLOCKED / NOT RUN: no phase-1 witness |
| 2: editor-copy | BLOCKED / NOT RUN: same prerequisite |
| 2: footer-copy | BLOCKED / NOT RUN: same prerequisite |
| 2: context-insert | BLOCKED / NOT RUN: same prerequisite |
| 3: Import Cancel once, last | FAIL: helper `reason=operation`; epoch unchanged |
| Import Commit / Export Cancel / Export Commit | NOT RUN: outside grant |
| Release | PASS: flags closed, zero processes, no recovery, clipboard metadata reported |

### Phase 1 — process guard refused before baseline mutation

At `2026-11-01T22:55:31.8397944Z`, the initial read-only process audit reported
candidate, management fixture, guardian, Target018, nr08 Python and UI-helper
counts all zero. The subsequent seed helper independently repeated its own
process admission and threw exactly **`owned_process_retained`** at its guard,
before the authorized `clip.exe` line. Seed stdout was empty; its success marker
was absent. No baseline seed, clipboard content read, native runner parent,
coordinator, target, guardian or control session followed. The chained
`nr08-clipboard-check.py` / `nr08-run.py` commands were not reached.

A subsequent metadata-only matching-process query returned no entries. Another
audit at `22:56:13.4695548Z` again reported all six counts zero before the picker.
The seed did not retain a matching-process identity record: the initial and later
zero samples do **not** prove that its intervening sample was zero, identify a
survivor, establish a false positive, or justify bypassing/retrying its guard.
The cause of that admission refusal remains unknown. No mutation/scene retry,
process kill, baseline assertion or old-witness reconstruction was performed.

### Phase 2 — required native startup diagnostics unavailable, not PASS

No native scene launched and no returned `RestorativeSessionObservation` existed.
For **each** of posted-row, posted-main, posted-enter, popup-copy, editor-copy,
footer-copy and context-insert:

- Session token, `failureStage`, `target_foreground_grant result=` and
  `native_positive_failed`: **absent / NOT RUN**.
- `native-monitor-failure.json` (predicate, exitCode, sampleOrder, relation,
  closed): **absent**, not an invented all-clear record.
- Every `native_startup_foreground` phase/relation/launcher_foreground/sample_order
  and `native_host_readiness reason`: **absent**, since no worker started.
- Independent native usage and clipboard-epoch boundary: **NOT RUN**. No
  native database exists for these scenes; the picker aggregate is not their
  evidence. No guarded paste, Copy commit, keyboard arm or shortcut occurred.

Proof limit remains **`registered_posted_callback_not_physical`**; this run does
not even establish posted callback delivery. Research 048's new diagnostics
remain unexercised, and nr-07's historical records must not substitute for them.

## Phase 3 — Import Cancel FAIL; fixed telemetry retained

The independent non-inserting picker has no restorative-witness prerequisite.
It ran once, last, after the zero-process audit, with over eighteen minutes left
before 17:15. Exact fresh management executable/hash above, arguments
`--scene file-picker --authorized-screen-slot --work-area -1920 1506 0 2538`.

- Picker parent: `ss-uno018-ui-46be9576623540cc9087ae9824d7c0db`.
- Exact UI session: `ss-uno018-ui-02033fd81ac5b014a56707700c2b4e22`.
- Child/wrapper exit 1; parent and fixture stderr empty. Helper stdout empty.
- Clipboard epoch independently **unchanged** across this attempt.
- Read-only owned SQLite aggregate: **0 rows / 0 usage**.
- Diagnostic privacy **CLEAN**; planted negative controls **PASS**.

Complete fixed failure records/helper line:

```json
{"observer-parent-failure.json":{"reason":"worker_protocol"},"observer-worker-failure.json":{"reason":"ui_helper_protocol"}}
```

```text
ui_helper_failed stage=picker_import_cancel reason=operation
```

`operation` is a sanitized catch-all, not a precise failing predicate/provider
cause. No raw exception was logged or used to speculate. Neither foreground
failure file nor `ui-helper-picker-schema.json` exists. Thus there is **no
`picker_schema_multiple` per-match isDirectChild/classToken/enabled/offscreen/
controlTypeId telemetry to copy**, and no `reason=input` / `inputSubreason`
record. No telemetry from an older run is borrowed.

`ui-ready.receipt` and `picker-import-cancel.active` exist. No Cancel closed
marker, phase action receipt or `ui-import-cancelled.receipt` exists. Every
Import/Export later-phase active/closed/action marker is absent. Existing owned
helper/fixture Job failure cleanup completed; no graceful failed-host shutdown or
successful pre-kill JobZero acceptance is claimed.

Scratch copies of the **current** UI runner/helper preserved nd-04 input-subreason
and all existing ownership/foreground/input/schema/privacy guards. Python pinned
only this worktree and copied helper path. PowerShell added an unconditional
fixed `actions` stop immediately after the first `WaitPhase 'ui-import-cancelled'`,
before any Import Commit call; this planned one-phase stop was not reached.
The copied UI flag was also restored false on release. No UI mutation retry,
provider fallback or tracked helper/placement/source behavior change.

## Release — SCREEN-DONE by 16:57:08 local

All nine tracked trial flags and the copied UI gate were restored false.
`nr08-flags-closed.txt` contains grep proof, including unchanged closed gates:

```text
NATIVE_SCENES_SOURCE_REVIEWED = False
POSITIVE_SCENES_SOURCE_REVIEWED = False
POSITIVE_INPUT_SOURCE_REVIEWED = False
UI_SOURCE_REVIEWED = False
NativeFixtureSourceReviewed = false
PositiveProtocol.SourceReviewed / InputSourceReviewed = false
OwnedTargetPreparation.SourceReviewed = false
NativeAcceptanceFactory.SourceReviewed = false
STARTUP_OBSERVER_REVIEWED = False
FULL_HOST_OBSERVER_REVIEWED = False
NativeEntryReviewed = false
```

`git diff --exit-code` passed for all six temporarily edited tracked source files.
At `2026-11-01T22:56:53.4567756Z`, counts were candidate coordinator/worker 0,
management fixture 0, guardian 0, Target018 0, nr08 run/picker/copied-UI Python 0,
UI PowerShell helper 0. No pending clipboard recovery, retained coordinator/
guardian or uncertain keyboard lifetime exists. No pending process was killed,
recovery endpoint invoked or generic release synthesized.

Final locked metadata-only clipboard check, before 16:57:08: builtin text 4,
ownership marker 0, protective metadata 0, unsupported 0, owner absent, sequence
stable. `seedPerformed=false`, `payloadRead=false`. Clipboard contents were
neither read nor written; no claim is made about their value or restoration of
preexisting user data. The picker epoch observation proves only its sampled
boundary, not immunity to unrelated later changes. No further desktop/clipboard
operation followed release, well before the 17:15 hard end.

Private retained evidence: nr08 build receipts/logs, enabled diff, artifacts and
post-run hash recheck, filesystem grant polls/admission, copied helpers/hash ledger,
preflight and initial/prepicker/release process audits, empty seed stdout, exact
picker parent/session, `nr08-picker-evidence.json`, `nr08-flags-closed.txt` and
`nr08-clipboard-release.json`. No profile/evidence was deleted. Commit scope is
**only this research document**, no enabled flag/helper/source fix and no push.

Pending: explain the seed process-admission refusal under a separately authorized
follow-up; obtain fresh controls before any positive run; exercise nd-04 startup
records and the intended posted/main/Copy/context consumers; diagnose picker
`operation` without relaxing guards. Existing physical/pointer/fault/hover/tray/
coexistence/oldest-runtime obligations remain open. No active recovery or operator
action is pending. This released run authorizes no further runtime retry.
