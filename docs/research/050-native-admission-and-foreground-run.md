# 050 — nr-09: admission and controls PASS; transient foreground diagnostics

Authorized brief `native-run10-brief.md`, 2026-11-01. Phase 0 and the fresh
same-artifact restore/abort/owner-refusal chain **PASS**. Posted-row **PASS**;
posted-main and all four remaining captured-Copy/context scenes **FAIL** before
their consumers. Import Cancel **FAIL** at input epoch guard. Release **PASS**.
Plans 018/019 remain incomplete; no production or physical-input acceptance.

## Authorization and screen boundary

- Worktree `.claude/worktrees/uno019-native-trial8`, branch
  `feature/uno019-native-trial8`, starting HEAD
  `8a06f65d3d36c6762792d6cfd2d98d1954af4a6e`.
- Read AGENTS, guide 002, progress 004, approved Plan 019, research 047–049 and
  both fixture READMEs. Operator edits to AGENTS and guide 002 were untouched,
  never staged. No delegation, push or legacy-profile operation.
- Phase 0 completed by 17:15:28 local, filesystem/build/process-query only: no
  app/target/guardian launch, windows, clipboard access or input synthesis.
  `PHASE0-READY nr-09` was written before grant waiting.
- Filesystem/time-only 30-second polling observed exact `GRANTED nr-09` at
  `2026-11-01T23:33:22.229427Z`, poll 36, after 1050.040 seconds. The grant
  stated **exclusive 17:33–17:58 local**, bottom-left only, direct user confirmation
  in pc-manager. Start/time admission occurred at 17:33:29 local; no DENIED/REVOKED.
  No window or clipboard work preceded that admission.
- Live PMv2 enumeration confirmed only nonprimary bottom-left bounds
  `(-1920,1506,0,2586)`, work `(-1920,1506,0,2538)`. Actual primary was
  `(0,0,3840,2160)`; names were not identity authority. No primary/top-left
  window controlled or moved. Held-VK preflight was empty.
- Grant reserved controls, posted-row/main, popup/editor/footer Copy,
  context-insert and last Import Cancel with >=5 minutes remaining. Native
  admission reserved three minutes for release. Picker started about 17:34:37,
  over 23 minutes before hard end.
- **posted-enter NOT RUN:** its Enter uses harness SendInput, excluded by D50.
  No registered-* scene, external keyboard/mouse synthesis, operator key/action
  request, focus workaround, generic key-up, downloads, clock change or mutation
  retry. Product's guarded paste was reached only in posted-row. Generic runner
  operator instructions were not acted upon; reviewed owned-A preparation ran.

## Phase 0 — PASS

Exactly nine booleans were temporarily enabled, same scope as nr-08:

| File | Constants |
|---|---|
| scripts/run_uno_candidate_native.py | NATIVE_SCENES_SOURCE_REVIEWED, POSITIVE_SCENES_SOURCE_REVIEWED, POSITIVE_INPUT_SOURCE_REVIEWED |
| scripts/run_uno_candidate_ui.py | UI_SOURCE_REVIEWED |
| scripts/uno_candidate_native_fixture/Program.cs | NativeFixtureSourceReviewed |
| scripts/uno_candidate_native_fixture/PositiveProtocol.cs | SourceReviewed, InputSourceReviewed |
| scripts/uno_candidate_native_fixture/OwnedTargetPreparation.cs | SourceReviewed |
| src/SmartSnippets.Uno/Platforms/Windows/AcceptanceNative/NativeAcceptanceFactory.cs | SourceReviewed |

Exact enabled diff retained privately: `nr09-enabled-flags.diff`, SHA256
`18465ff1d34afbe91a1254f59129414b4c1470a9be9916fb25d6b12b21872de1`.
Startup/full-host/prototype gates and ordinary capability remained closed.

Five fresh strict native x64 builds ran sequentially; linked fixtures and ordinary
candidate passed their existing provenance/effective-property/reference/SQLite/
freshness gates. Support projects published to new ACL-private outputs with
NoWarn empty, AOT/trim/compatibility/analyzers/warnings-as-errors true and trim
suppression/single-warning aggregation false. All five warning/error scans passed.
Native --validate-build passed. No gate weakened, IL warning suppressed or test
suite rerun claimed. Ordinary candidate was build-only, **never launched**.
All five executable hashes were recomputed after release and matched.

| Artifact | Fresh token | SHA256 |
|---|---|---|
| native | `64d84faed6ba4a4eb17cb2eb49a635ce` | `cf8df35d30cca41cf0d8b6781f74f14d98ddf9a2b39ce6cb333f8142a1a7c4cd` |
| target | `0880f2e4621d4017ae3a001688ceb926` | `0af470d72e1c52244ee3d92bc8843051f70da47796d91ccf699dae6411a42ae9` |
| guardian | `0dd1f849fb3f451483024f8ac1a1f945` | `b9802456fa99f07a52dd84445291f0a0b3a2f10a862434aa669d7d5e141f51cb` |
| management | `944798beb05648adac8867e2bae456c5` | `319f903df1af05fdd4244b4782b997fcc34f820027b383459e42ab6119dd75d7` |
| candidate (not launched) | `96aeae333edf48d2b92e0e21442baa39` | `6b32f19ffe5a4c65e9c28bac9edc1c0e5db425a7fc9b11cbcbb0bdb040e1d2d6` |

Native enabled-source receipt: `d6f3bc338f4a7a04783f5d3f002c7e062c1062c9b5dfad83a558ef928ba2f7ff`.
Management base-source receipt: `43db2825b74d6032c18bde3734f010ab20bc0c1a90ab4cb87ccef3fcd9acc340`.
Native/management outputs use their unique publish-native/publish-acceptance tokens;
support outputs use `bin/nr09-<token>`; candidate uses its separate publish token.
Enabled binaries remain private research evidence, not further launch authority.

### Admission guard correction and dry rehearsal

Scratch copies only; nr08 originals unchanged. The new seed guard walks
ParentProcessId from self, excluding every live ancestor and exact trusted parent.
It retains all six refusal classes: candidate, fixture, guardian, target018,
run_python, ui_helper. Refusal emits one fixed record per classified match:

```text
admission_match class=<candidate|fixture|guardian|target018|run_python|ui_helper> self_or_ancestor=<0|1> trusted_parent=<0|1>
```

No command lines, paths or PIDs are emitted. Matching run-Python/UI helpers include
tracked runner names and numbered scratch runners, not just the current run.
A missing/exited ancestor ends the live chain without excluding that nonexistent
PID; missing self refuses. Retained application/recovery classes still refuse.

During guard-only helper preparation, initial rehearsal refused an exited ancestor
(`admission_ancestry_unavailable`). After correcting the traversal, scratch code
hit a PowerShell automatic `$Matches` collision, producing one empty class record,
not an authenticated process match. Renaming the collection `classifiedMatches`
removed that scratch bug without removing any process class. These are preparation
failures, not failed mutation retries. The historical nr-08 cause remains unknown.
The final unchanged guard ran **twice consecutively**, both exit 0, empty stderr,
`admission_dry_pass=1`, from **bash → Python → pwsh -NoProfile -File**, matching
later runner-to-seed launch topology. DryRun returns before clip.exe and bypasses
only the not-yet-granted screen-time check, not process admission.
Scratch Python syntax (11 files) and all copied PowerShell parsing passed.
Helper hashes retained before execution and after gate closure.

## Commands and witness lifetime

`<scratchpad>` is the private brief directory. No original helper was edited.

```text
pwsh -NoProfile -File scripts/uno_candidate_native_fixture/build.ps1 -BuildOnly
pwsh -NoProfile -File <scratchpad>/nr09-build-support.ps1
pwsh -NoProfile -File scripts/verify-uno-candidate.ps1 -BuildOnly
pwsh -NoProfile -File scripts/publish-uno-candidate-aot.ps1
python -I -B scripts/run_uno_candidate_native.py --validate-build <native-entry> <native-sha256>
python -I -B <scratchpad>/nr09-artifacts.py
python -I -B <scratchpad>/nr09-dry-admission.py
pwsh -NoProfile -File <scratchpad>/nr09-parse-helpers.ps1
python -I -B <scratchpad>/nr09-wait-grant.py
python -I -B <scratchpad>/nr09-admit-window.py
python -I -B <scratchpad>/nr09-preflight.py
pwsh -NoProfile -File <scratchpad>/nr09-process-check.ps1
python -I -B <scratchpad>/nr09-run.py
# Runner invokes seed, native main and last picker; no external timeout-kill.
# After completion: close all nine flags plus copied UI flag, then:
pwsh -NoProfile -File <scratchpad>/nr09-process-check.ps1
python -I -B <scratchpad>/nr09-clipboard-check.py
# Filesystem-only after SCREEN-DONE:
python -I -B <scratchpad>/nr09-evidence-check.py ss-uno018-ui-891e52ea8327937fd8a88431eaf6a424
python -I -B <scratchpad>/nr09-artifacts.py
```

Trusted detached parent: `ss-uno018-ui-891e52ea8327937fd8a88431eaf6a424`;
`nr09_observer_complete=1`, empty stderr. Actual native runner main was invoked
with exact artifact paths/SHA256 pins and --authorized-screen-slot --scene.
Restore → abort → owner-refusal passed actual returned RestorativeSessionObservation
objects directly in that parent's RAM; the last witness was revalidated there before
positives. No old receipts/JSON or discovered directory supplied authority.

Read-only process audits were zero before initial seed and before every independent
scene (excluding only the exact trusted runner). With no pending recovery, seed
ran the authorized fixed command once before controls and once before each of six
positive scenes, **seven times total**, never during recovery or after failed seed:

```powershell
"Smart Snippets QA" | & "$env:SystemRoot\System32\clip.exe"
```

Fresh baseline flags were truthfully supplied for positives; no Copy result was
used as a restorative witness. Every failed positive stopped that scene; all six
authorized independent scenes were attempted once as required. Audit/refusal would
have stopped admission, not triggered a mutation retry.

## Per-step results and exact native evidence

All records below are current-run only. Every completed native diagnostic stream
was privacy CLEAN with planted negative controls PASS and empty stderr; no
clipboard/keyboard recovery-pending receipt. Failure stages are last-entered stages,
not exact predicate locations. Full validated monitor records are retained verbatim.

| Step | Result | Usage/epoch |
|---|---|---|
| Phase 0 | PASS | no clipboard access |
| restore, abort, owner-refusal | PASS each | 0 rows / 0 usage each |
| posted-row | PASS | 1 row / usage 1; changed epoch; both independent checks PASS |
| posted-main | FAIL | 1 row / usage 0: FAIL; unchanged epoch: FAIL |
| popup-copy, editor-copy, footer-copy | FAIL each | zero usage PASS only; unchanged Copy epoch boundary FAIL |
| context-insert | FAIL | 1 row / usage 0: FAIL; unchanged epoch: FAIL |
| posted-enter | NOT RUN / BLOCKED | input synthesis excluded; no session or diagnostics |
| Import Cancel | FAIL | 0 rows / 0 usage; epoch unchanged |
| other picker phases, registered-* | NOT RUN | excluded |
| Release | PASS | flags closed, zero processes, baseline verified |

### restore — PASS

Session: `ss-uno018-ui-7e91f694201899b83d8fb7cb3c699829/ss-uno019-native-2c5a54128c3f432d8d6a8cc5e7d0ab40`.
FailureStage: `none`; target foreground grant: **absent**.
Rows/usage: 0/0; independent usage PASS.
Positive epoch check not applicable to clipboard-only controls.
Receipt/host verification: PASS; host shutdown-complete true.
Monitor failure: absent.

All fixed startup/readiness/preparation/grant/failure records, plus available mutation diagnostics:

```text
2026-11-01T23:33:37.747Z INFO native_startup_foreground phase=target_launch relation=foreign launcher_foreground=False sample_order=1
2026-11-01T23:33:37.747Z INFO native_startup_foreground phase=host_ready relation=foreign launcher_foreground=False sample_order=2
2026-11-01T23:33:37.747Z INFO native_host_readiness reason=ready
2026-11-01T23:33:37.976Z INFO native_control_admission reason=Ready
2026-11-01T23:33:38.068Z INFO native_control_publish status=swapped
```

### abort — PASS

Session: `ss-uno018-ui-35bd73320444ad2a66a4d3c994bb9de9/ss-uno019-native-963acce8aa0e45a887cd6eacdb9031cf`.
FailureStage: `none`; target foreground grant: **absent**.
Rows/usage: 0/0; independent usage PASS.
Positive epoch check not applicable to clipboard-only controls.
Receipt/host verification: PASS; host shutdown-complete false.
Monitor failure: absent.

All fixed startup/readiness/preparation/grant/failure records, plus available mutation diagnostics:

```text
2026-11-01T23:33:43.569Z INFO native_startup_foreground phase=target_launch relation=foreign launcher_foreground=False sample_order=1
2026-11-01T23:33:43.569Z INFO native_startup_foreground phase=host_ready relation=foreign launcher_foreground=False sample_order=2
2026-11-01T23:33:43.570Z INFO native_host_readiness reason=ready
2026-11-01T23:33:43.749Z INFO native_control_admission reason=Ready
2026-11-01T23:33:43.859Z INFO native_control_publish status=swapped
```

### owner-refusal — PASS

Session: `ss-uno018-ui-e8f2067d0a3f8b5aa6d23abe1b5be8ab/ss-uno019-native-914535db55f542189d5c6b30c9e82608`.
FailureStage: `none`; target foreground grant: **absent**.
Rows/usage: 0/0; independent usage PASS.
Positive epoch check not applicable to clipboard-only controls.
Receipt/host verification: PASS; host shutdown-complete true.
Monitor failure: absent.

All fixed startup/readiness/preparation/grant/failure records, plus available mutation diagnostics:

```text
2026-11-01T23:33:49.386Z INFO native_startup_foreground phase=target_launch relation=foreign launcher_foreground=False sample_order=1
2026-11-01T23:33:49.386Z INFO native_startup_foreground phase=host_ready relation=foreign launcher_foreground=False sample_order=2
2026-11-01T23:33:49.387Z INFO native_host_readiness reason=ready
2026-11-01T23:33:49.565Z INFO native_control_admission reason=Ready
2026-11-01T23:33:49.662Z INFO native_control_publish status=swapped
```

### posted-row — PASS

Session: `ss-uno018-ui-0c30c4e839a87a6a18dd0b54b762ee2e/ss-uno019-native-1cf30df3b52e4bf3b189a609ea17f432`.
FailureStage: `none`; target foreground grant: **granted**.
Rows/usage: 1/1; independent usage PASS.
Outer epoch changed; independent combined boundary PASS.
Receipt/host verification: PASS; host shutdown-complete true.
Monitor failure: absent.

All fixed startup/readiness/preparation/grant/failure records, plus available mutation diagnostics:

```text
2026-11-01T23:33:58.078Z INFO native_startup_foreground phase=target_launch relation=foreign launcher_foreground=False sample_order=1
2026-11-01T23:33:58.078Z INFO native_startup_foreground phase=host_ready relation=foreign launcher_foreground=False sample_order=2
2026-11-01T23:33:58.079Z INFO native_host_readiness reason=ready
2026-11-01T23:33:58.267Z INFO owned_initial_focus phase=waiting
2026-11-01T23:33:58.268Z INFO native_startup_foreground phase=preparation_start relation=foreign launcher_foreground=False sample_order=3
2026-11-01T23:33:58.308Z INFO owned_preparation_admission reason=ready
2026-11-01T23:33:58.464Z INFO native_startup_foreground phase=before_focus relation=foreign launcher_foreground=False sample_order=4
2026-11-01T23:33:58.465Z INFO native_startup_foreground phase=after_focus relation=none launcher_foreground=False sample_order=5
2026-11-01T23:33:58.692Z INFO owned_target_preparation reason=Prepared
2026-11-01T23:33:58.711Z INFO owned_initial_focus phase=ready
2026-11-01T23:33:58.756Z INFO target_foreground_grant result=granted
2026-11-01T23:33:59.607Z INFO native_popup_focus reason=foreground_accepted
2026-11-01T23:33:59.608Z INFO candidate_popup_focus reason=accepted
2026-11-01T23:34:00.173Z INFO clipboard_swap attempts=1 status=Swapped saved=4 skipped_by_design=0 unsaved=0 cap_exceeded=False bytes=104 ms=138
2026-11-01T23:34:00.751Z INFO clipboard_restore status=Restored attempts=1
2026-11-01T23:34:00.751Z INFO insert_result outcome=Inserted id=1 notice=False ms=1093
```

### posted-main — FAIL

Session: `ss-uno018-ui-d83bcb6c61ad25f0be4c22eeb0785b29/ss-uno019-native-580d0ad1d7a44f9b99e9e20ac747d46b`.
FailureStage: `guardian_ready`; target foreground grant: **absent**.
Rows/usage: 1/0; independent usage FAIL.
Outer epoch unchanged; independent combined boundary FAIL.
Receipt/host verification: FAIL_ValueError; host shutdown-complete false.
Monitor failure:

```json
{"predicate":"foreground_foreign","exitCode":2,"sampleOrder":26,"relation":"same_pid_other","closed":false}
```.

All fixed startup/readiness/preparation/grant/failure records, plus available mutation diagnostics:

```text
2026-11-01T23:34:07.535Z INFO native_startup_foreground phase=target_launch relation=foreign launcher_foreground=False sample_order=1
2026-11-01T23:34:07.535Z INFO native_startup_foreground phase=host_ready relation=foreign launcher_foreground=False sample_order=2
2026-11-01T23:34:07.535Z INFO native_host_readiness reason=ready
2026-11-01T23:34:07.826Z INFO owned_initial_focus phase=waiting
2026-11-01T23:34:07.827Z INFO native_startup_foreground phase=preparation_start relation=foreign launcher_foreground=False sample_order=3
2026-11-01T23:34:07.871Z INFO owned_preparation_admission reason=ready
2026-11-01T23:34:08.011Z INFO native_startup_foreground phase=before_focus relation=foreign launcher_foreground=False sample_order=4
2026-11-01T23:34:08.028Z INFO native_startup_foreground phase=after_focus relation=owned_a launcher_foreground=False sample_order=5
2026-11-01T23:34:08.073Z INFO owned_target_preparation reason=ForegroundRefused
2026-11-01T23:34:08.074Z INFO owned_initial_focus phase=refused
2026-11-01T23:34:08.074Z ERROR native_positive_failed stage=guardian_ready InvalidOperationException (HResult=0x80131509)
```

### popup-copy — FAIL

Session: `ss-uno018-ui-485abe0266f4d6dc277574724be5ae6a/ss-uno019-native-6485c36409314091b9517c82b7e2d2d1`.
FailureStage: `guardian_ready`; target foreground grant: **absent**.
Rows/usage: 1/0; independent usage PASS.
Outer epoch unchanged; independent combined boundary FAIL.
Receipt/host verification: FAIL_ValueError; host shutdown-complete false.
Monitor failure:

```json
{"predicate":"foreground_foreign","exitCode":2,"sampleOrder":33,"relation":"foreign","closed":false}
```.

All fixed startup/readiness/preparation/grant/failure records, plus available mutation diagnostics:

```text
2026-11-01T23:34:14.094Z INFO native_startup_foreground phase=target_launch relation=foreign launcher_foreground=False sample_order=1
2026-11-01T23:34:14.095Z INFO native_startup_foreground phase=host_ready relation=foreign launcher_foreground=False sample_order=2
2026-11-01T23:34:14.095Z INFO native_host_readiness reason=ready
2026-11-01T23:34:14.536Z INFO owned_initial_focus phase=waiting
2026-11-01T23:34:14.536Z INFO native_startup_foreground phase=preparation_start relation=foreign launcher_foreground=False sample_order=3
2026-11-01T23:34:14.583Z INFO owned_preparation_admission reason=ready
2026-11-01T23:34:14.731Z INFO native_startup_foreground phase=before_focus relation=foreign launcher_foreground=False sample_order=4
2026-11-01T23:34:14.749Z INFO native_startup_foreground phase=after_focus relation=owned_a launcher_foreground=False sample_order=5
2026-11-01T23:34:14.798Z INFO owned_target_preparation reason=ForegroundRefused
2026-11-01T23:34:14.799Z INFO owned_initial_focus phase=refused
2026-11-01T23:34:14.799Z ERROR native_positive_failed stage=guardian_ready InvalidOperationException (HResult=0x80131509)
```

### editor-copy — FAIL

Session: `ss-uno018-ui-de20d347d57702492cd2bdb846d6e695/ss-uno019-native-cddf02e9450a420ebdb036d34d8063cb`.
FailureStage: `guardian_ready`; target foreground grant: **absent**.
Rows/usage: 1/0; independent usage PASS.
Outer epoch unchanged; independent combined boundary FAIL.
Receipt/host verification: FAIL_ValueError; host shutdown-complete false.
Monitor failure:

```json
{"predicate":"foreground_foreign","exitCode":2,"sampleOrder":30,"relation":"foreign","closed":false}
```.

All fixed startup/readiness/preparation/grant/failure records, plus available mutation diagnostics:

```text
2026-11-01T23:34:21.229Z INFO native_startup_foreground phase=target_launch relation=foreign launcher_foreground=False sample_order=1
2026-11-01T23:34:21.230Z INFO native_startup_foreground phase=host_ready relation=foreign launcher_foreground=False sample_order=2
2026-11-01T23:34:21.231Z INFO native_host_readiness reason=ready
2026-11-01T23:34:21.573Z INFO owned_initial_focus phase=waiting
2026-11-01T23:34:21.573Z INFO native_startup_foreground phase=preparation_start relation=foreign launcher_foreground=False sample_order=3
2026-11-01T23:34:21.615Z INFO owned_preparation_admission reason=ready
2026-11-01T23:34:21.767Z INFO native_startup_foreground phase=before_focus relation=foreign launcher_foreground=False sample_order=4
2026-11-01T23:34:21.779Z INFO native_startup_foreground phase=after_focus relation=owned_a launcher_foreground=False sample_order=5
2026-11-01T23:34:21.831Z INFO owned_target_preparation reason=ForegroundRefused
2026-11-01T23:34:21.831Z INFO owned_initial_focus phase=refused
2026-11-01T23:34:21.831Z ERROR native_positive_failed stage=guardian_ready InvalidOperationException (HResult=0x80131509)
```

### footer-copy — FAIL

Session: `ss-uno018-ui-8357a67464e277e756c556a3834dd93c/ss-uno019-native-3ce2d33261d0430baa9016b534a48d34`.
FailureStage: `guardian_ready`; target foreground grant: **absent**.
Rows/usage: 1/0; independent usage PASS.
Outer epoch unchanged; independent combined boundary FAIL.
Receipt/host verification: FAIL_ValueError; host shutdown-complete false.
Monitor failure:

```json
{"predicate":"foreground_foreign","exitCode":2,"sampleOrder":25,"relation":"none","closed":false}
```.

All fixed startup/readiness/preparation/grant/failure records, plus available mutation diagnostics:

```text
2026-11-01T23:34:27.236Z INFO native_startup_foreground phase=target_launch relation=foreign launcher_foreground=False sample_order=1
2026-11-01T23:34:27.237Z INFO native_startup_foreground phase=host_ready relation=foreign launcher_foreground=False sample_order=2
2026-11-01T23:34:27.237Z INFO native_host_readiness reason=ready
2026-11-01T23:34:27.548Z INFO owned_initial_focus phase=waiting
2026-11-01T23:34:27.549Z INFO native_startup_foreground phase=preparation_start relation=foreign launcher_foreground=False sample_order=3
2026-11-01T23:34:27.590Z INFO owned_preparation_admission reason=ready
2026-11-01T23:34:27.749Z INFO native_startup_foreground phase=before_focus relation=foreign launcher_foreground=False sample_order=4
2026-11-01T23:34:27.761Z INFO native_startup_foreground phase=after_focus relation=owned_a launcher_foreground=False sample_order=5
2026-11-01T23:34:27.804Z INFO owned_target_preparation reason=ForegroundRefused
2026-11-01T23:34:27.805Z INFO owned_initial_focus phase=refused
2026-11-01T23:34:27.805Z ERROR native_positive_failed stage=guardian_ready InvalidOperationException (HResult=0x80131509)
```

### context-insert — FAIL

Session: `ss-uno018-ui-a4088410bc95d43e6f078908abbc0f3d/ss-uno019-native-c71f9682c6674c9f9a5c5883bf4f325d`.
FailureStage: `guardian_ready`; target foreground grant: **absent**.
Rows/usage: 1/0; independent usage FAIL.
Outer epoch unchanged; independent combined boundary FAIL.
Receipt/host verification: FAIL_ValueError; host shutdown-complete false.
Monitor failure:

```json
{"predicate":"foreground_foreign","exitCode":2,"sampleOrder":23,"relation":"same_pid_other","closed":false}
```.

All fixed startup/readiness/preparation/grant/failure records, plus available mutation diagnostics:

```text
2026-11-01T23:34:33.337Z INFO native_startup_foreground phase=target_launch relation=foreign launcher_foreground=False sample_order=1
2026-11-01T23:34:33.337Z INFO native_startup_foreground phase=host_ready relation=foreign launcher_foreground=False sample_order=2
2026-11-01T23:34:33.338Z INFO native_host_readiness reason=ready
2026-11-01T23:34:33.530Z INFO owned_initial_focus phase=waiting
2026-11-01T23:34:33.530Z INFO native_startup_foreground phase=preparation_start relation=foreign launcher_foreground=False sample_order=3
2026-11-01T23:34:33.573Z INFO owned_preparation_admission reason=ready
2026-11-01T23:34:33.712Z INFO native_startup_foreground phase=before_focus relation=foreign launcher_foreground=False sample_order=4
2026-11-01T23:34:33.737Z INFO native_startup_foreground phase=after_focus relation=owned_a launcher_foreground=False sample_order=5
2026-11-01T23:34:33.782Z INFO owned_target_preparation reason=ForegroundRefused
2026-11-01T23:34:33.783Z INFO owned_initial_focus phase=refused
2026-11-01T23:34:33.783Z ERROR native_positive_failed stage=guardian_ready InvalidOperationException (HResult=0x80131509)
```

The controls all admitted Ready and swapped; abort deliberately stopped only the
worker/target Job after publication and the independent guardian restored. It is
not graceful worker shutdown. Owner-refusal changed owned HWND A→B within the
same guardian PID, not an arbitrary foreign producer. All three independent
receipt checks passed; restore and owner-refusal completed host shutdown.

Posted-row passed exact A readback/CRLF, empty B, one paste/capture, duplicate
Busy/no-op and usage once, actual host shutdown, monitor and both independent
usage/epoch verifiers. Action proof remains **vm_callback** and shortcut proof
**registered_posted_callback_not_physical**; no physical shortcut/pointer/menu claim.

### Diagnostic interpretation — transient sample, not never-activated proof

Every positive starts with target_launch/host_ready/preparation_start/before_focus
relation=foreign and launcher_foreground=False; host readiness is ready. Successful
posted-row sampled after_focus=none and subsequently proved A/preparation/foreground
grant and insertion. Each of the **five failures sampled after_focus=owned_a**, yet
the later foreground predicate returned ForegroundRefused, before target grant.
Thus these records do **not** support a claim that UIA never activated A. They are
consistent with transient A foreground followed by loss between distinct samples.
The after_focus diagnostic and immediate os.Foreground check are separate reads;
neither is atomic or identifies the foreign actor/cause. launcher_foreground=False
does not prove Windows foreground ineligibility. No bootstrap/ASFW/input workaround,
retry or guard relaxation follows from these samples.

All five monitor failures are closed=false: none proves a post-closure teardown
failure. Relation values (including none/same_pid_other under foreground_foreign)
are copied verbatim, not normalized or used to infer another application. None of
the five reached its intended Copy/main/context consumer, nor the editor's planned
store-failure test. No successful failed-host graceful shutdown is claimed.

## Phase 3 — Import Cancel FAIL; input epoch changed

Picker parent: `ss-uno018-ui-fdd391b2ec61c9e115724b527cb81988`.
Exact session: `ss-uno018-ui-46834c89219d631c8b728b983439f0ad`.
Wrapper/child exit 1. Full available fixed failure records:

```json
{
  "observer-parent-failure.json": {
    "reason": "worker_protocol"
  },
  "observer-worker-failure.json": {
    "reason": "input",
    "inputSubreason": "epoch_changed"
  }
}
```

The discriminating worker record is **reason=input, inputSubreason=epoch_changed**,
not the parent's worker_protocol. It does not identify the source of input.
Helper stdout/stderr empty: **no reason/operationClass/pickerStep/HRESULT helper
record**, no picker_schema_multiple or per-match schema fields, and no foreground
failure JSON. These are absent, not substituted from older runs. Privacy CLEAN,
negative controls PASS, read-only aggregate 0 rows/usage 0 and epoch unchanged.
Import Cancel active marker exists; closed/action/import-cancelled receipt absent.
No later picker-phase marker exists. This proves refusal, not successful Cancel.
Existing owned helper/fixture Job failure cleanup completed; not graceful/pre-kill
JobZero acceptance. No recovery-bearing process was killed.

Scratch UI Python/PowerShell copies came from current phase-0 pk-01 sources and
preserved all ownership/input/foreground/schema/privacy guards. Python pinned REPO
and helper path; PowerShell unconditionally stops after first WaitPhase
ui-import-cancelled and before Import Commit. That planned stop was not reached.
Post-run inspection found the inserted stop used `Fail 'actions'`, not the prior
helper's `throw 'actions'`; Fail is undefined. With ErrorActionPreference=Stop it
would also terminate before later phases, but would not emit the intended fixed
actions reason. This unused scratch defect is retained in the hashed executed
copy, not silently corrected or rerun; use the explicit throw in future preparation.
It cannot explain this run's earlier Python input-epoch refusal.
The grant optionally allowed rv-12 script cherry-pick 031c39d before phase 3 only;
it was **not taken**. Evidence is explicitly for starting HEAD's pk-01 scripts,
not rv-12; no script-source drift, cherry-pick or rebuild occurred. No retry.

## Release — PASS; SCREEN-DONE 17:35:02 local

All nine tracked booleans and copied UI flag restored false. Exact grep proof:

```text
scripts/run_uno_candidate_startup.py:21:STARTUP_OBSERVER_REVIEWED = False
scripts/run_uno_candidate_acceptance.py:18:FULL_HOST_OBSERVER_REVIEWED = False
scripts/run_uno_candidate_ui.py:17:UI_SOURCE_REVIEWED = False
scripts/run_uno_candidate_native.py:19:NATIVE_SCENES_SOURCE_REVIEWED = False
scripts/run_uno_candidate_native.py:20:POSITIVE_SCENES_SOURCE_REVIEWED = False
scripts/run_uno_candidate_native.py:21:POSITIVE_INPUT_SOURCE_REVIEWED = False
prototypes/uno-native-target/windows-insertion/Program.cs:11:    private static readonly bool NativeEntryReviewed = false;
src/SmartSnippets.Uno/Platforms/Windows/AcceptanceNative/NativeAcceptanceFactory.cs:14:    internal static readonly bool SourceReviewed = false;
scripts/uno_candidate_native_fixture/Program.cs:14:    internal static readonly bool NativeFixtureSourceReviewed = false;
scripts/uno_candidate_native_fixture/OwnedTargetPreparation.cs:26:    internal static readonly bool SourceReviewed = false;
scripts/uno_candidate_native_fixture/PositiveProtocol.cs:8:    internal static readonly bool SourceReviewed = false;
scripts/uno_candidate_native_fixture/PositiveProtocol.cs:9:    internal static readonly bool InputSourceReviewed = false;
17:UI_SOURCE_REVIEWED = False
```

`git diff --exit-code` passed for every temporarily changed tracked source.
Release process audit (2026-11-01T23:35:02.1249282Z):

```json
{"observedUtc":"2026-11-01T23:35:02.1249282+00:00","counts":{"SmartSnippets.UnoCandidate.exe":0,"SmartSnippets.UnoAcceptanceFixture.exe":0,"SmartSnippets.UnoCandidate.Guardian.exe":0,"SmartSnippets.Target018.exe":0,"nr09PythonHelpers":0,"UIHelper":0}}
```

Final locked RAM-only clipboard check, without post-run reseed:

```json
{"builtin_text": 4, "ownership_marker": 0, "protective_metadata": 0, "unsupported": 0, "owner_present": false, "authorized_synthetic_baseline_matches": true, "sequence_stable": true}
```

Baseline matches the authorized synthetic seed; sequence stable, no ownership or
protective marker, unsupported formats or owner. This is **not restoration of
pre-seed user data** or a guarantee against later unrelated clipboard changes.
No retained coordinator/guardian, pending clipboard recovery or uncertain keyboard
lifetime remains. No pending process killed, recovery endpoint invoked or synthetic
release sent. No desktop/clipboard operation followed SCREEN-DONE; all later work
was filesystem-only, well before hard end 17:58.

Private retained evidence: nr09 artifacts/build logs and source receipts, exact
enabled diff, two dry-admission PASS records, helper hashes and syntax checks,
grant polls/admission, geometry/preflight/process audits, exact parent/session
logs, nr09-evidence.txt, final flag/process/clipboard proof and post-run artifact
recheck. No profile/evidence deleted. Commit scope is **only this document**;
no enabled flags, helper, operator edit, source fix or push.

Pending: diagnose transient initial-A foreground loss and picker input epoch
without weakening guards, then separately authorized fresh-source acceptance of
posted-main/Copy/context and remaining fault/lifecycle/hover/tray/oldest-runtime
obligations. No active recovery or operator action pending. This run grants no
further runtime retry and closes neither Plan 018 nor Plan 019.
