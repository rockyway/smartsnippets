# 052 — nr-10: bounded foreground trace run

Authorized brief `native-run11-brief.md`, 2026-11-01. Phase 0 and the fresh
same-artifact restore → abort → owner-refusal chain **PASS**. Posted-row **PASS**;
posted-main, popup-copy, editor-copy, footer-copy and context-insert **FAIL** during
owned-A preparation. Import Cancel **FAIL** at the unchanged input guard.
Release **PASS**. Plans 018/019 remain incomplete; no production or physical-input
acceptance is claimed.

## Authorization and screen gate

- Worktree `.claude/worktrees/uno019-native-trial9`, branch
  `feature/uno019-native-trial9`, starting HEAD
  `4112b9c70637b499d2b5d3bf9d963c77ccd578a1` (nd-05 plus pk-01b).
- Read AGENTS, safety guide 002, progress handoff 004, approved Plan 019,
  research 049–051 and both fixture READMEs. Uncommitted operator edits to
  AGENTS/guide 002 were preserved and never staged. No delegation or push.
- Phase 0 was build/filesystem/process-query only: no windows, app launch,
  clipboard access or input synthesis. Wrote `PHASE0-READY nr-10` to
  `native-run11-report.md` before waiting.
- Filesystem-only grant poller checked every 30 seconds, observing exact
  `GRANTED nr-10` on poll 2 at `2026-11-01T23:53:21.458854Z`, after
  30.002 seconds. No DENIED/REVOKED observed.
- The pc-manager grant stated **EXCLUSIVE 17:53–18:18 local**, bottom-left only,
  under the user's pre-approval for these runs. Start/hard end were parsed from
  that grant, not inherited from a previous run. Time admission occurred
  `17:53:29.540096` local (`23:53:29.540096Z`).
- Live PMv2 enumeration confirmed `DISPLAY29`, nonprimary, bounds
  `(-1920,1506,0,2586)`, work `(-1920,1506,0,2538)`. The live primary was
  `DISPLAY3`, bounds `(0,0,3840,2160)`, work `(0,0,3840,2088)`;
  the top-left was `DISPLAY30`, bounds `(-1920,306,0,1506)`.
  Names were diagnostic, not authority. No primary/top-left window was controlled
  or moved. Held-VK preflight was empty.
- **posted-enter NOT RUN**: `PositiveNativeInput.Enter` uses SendInput, forbidden
  by D50/grant. All registered-* scenes and other picker phases NOT RUN.
  No operator key/action request, external keyboard/mouse synthesis, input/focus
  workaround, generic key-up, download, clock change or mutation retry.
  Only the product's guarded paste in posted-row reached native input.
- Grant was rechecked before independent scenes; ordinary admission reserved
  three minutes for release. Last Import Cancel began about 17:54:31, over
  23 minutes before hard end. No desktop work followed SCREEN-DONE.

## Phase 0 — PASS: nine trial booleans, five fresh strict artifacts

Exactly these tracked trial constants were temporarily true, then restored false:

| File | Constants |
|---|---|
| scripts/run_uno_candidate_native.py | NATIVE_SCENES_SOURCE_REVIEWED, POSITIVE_SCENES_SOURCE_REVIEWED, POSITIVE_INPUT_SOURCE_REVIEWED |
| scripts/run_uno_candidate_ui.py | UI_SOURCE_REVIEWED |
| scripts/uno_candidate_native_fixture/Program.cs | NativeFixtureSourceReviewed |
| scripts/uno_candidate_native_fixture/PositiveProtocol.cs | SourceReviewed, InputSourceReviewed |
| scripts/uno_candidate_native_fixture/OwnedTargetPreparation.cs | SourceReviewed |
| src/SmartSnippets.Uno/Platforms/Windows/AcceptanceNative/NativeAcceptanceFactory.cs | SourceReviewed |

Exact enabled diff retained privately: `nr10-enabled-flags.diff`, SHA256
`c72abab7dacabc33c16c33d2f48ceae8e5ee09360d168fdd0b4c682290f1dbe6`.
Startup/full-host/prototype gates and ordinary candidate capability stayed closed.
The copied picker UI gate was true only for the authorized trial and also closed
at release.

All five builds were sequential. Existing strict effective-property/reference/
native-x64/SQLite/freshness/provenance gates passed for linked fixtures and ordinary
candidate. Target/guardian support outputs were new owner-ACL-private directories,
cleaned and published with NoWarn empty, AOT/trim/compatibility/analyzers/
warnings-as-errors true, trim-warning suppression and single-warning aggregation
false. Every warning/error scan passed; native --validate-build passed.
No warning suppression, weakened gate, old executable, test-suite rerun or legacy
publish is claimed. Ordinary candidate was build-only, **never launched**.
All five executable hashes were recomputed after release and matched.

| Artifact | Fresh token | SHA256 |
|---|---|---|
| native | `955a96a587ec4edda50d976a003fa991` | `b8186741e4f446c9ef82cd641e31ca9c7d33d779a8e9b383f06bf8d2fb24ffc5` |
| target | `795c94959177451caf7e04bd4a9c9feb` | `e0e634750651216d638119f216b37edbd042cd60010edf39d819502679170a4f` |
| guardian | `bf500c2a10bb4e77a91774f44b2202fe` | `cf2968ddc3c2096c29518bbc31c56900891c41166ea684a2876b97c347a331c4` |
| management | `4ea7be578dbd4f6e83d989566190057a` | `4b3869d011a71a80e4016916a9c6d50a19de2cb8d4ed705b70a03bdb670f3899` |
| candidate (not launched) | `050d989b7a34410da088bfc4c6c6af99` | `9d5e611dad91dd01ac655fc3d4b4e94c0db504d5842c842ecec132156007c25e` |

Native enabled-source receipt:
`4312db6a6d8715da49c6b262ef461b6a663f8579ebca4ce1bb26245c981c54d1`.
Management base-source receipt:
`fbfe04a7091e202fed1476880f068821c6e404563a009235f420ccb8058de5f2`.
Fixture paths use unique publish-native/publish-acceptance tokens; support outputs
use `bin/nr10-<token>`; candidate uses its separate publish-uno-candidate token.
Retained enabled binaries are private evidence, not permission to relaunch.

### Helpers and two dry admissions — PASS

Scratch nr10 helpers were based on nr09 copies, except the current-source picker
copies described below; originals untouched. The fixed guard retained candidate,
fixture, guardian, Target018, run_python and ui_helper refusal classes, excluding
self/live ancestors and the exact trusted parent, and retaining fixed
`admission_match class=... self_or_ancestor=... trusted_parent=...` refusal lines.
The same unchanged dry guard ran twice consecutively from
**bash → Python → pwsh -NoProfile -File** (the later runner-to-seed topology).
Each returned exit 0, empty stderr, `admission_dry_pass=1`; DryRun returned
before clip.exe. No refused class was removed and no phase-0 failure occurred.
Eleven scratch Python syntax checks and all scratch PowerShell parsing passed;
helper SHA256 ledgers were retained before execution and after closure.

Picker copies were made from the **current** tracked UI runner/helper, preserving
nd-05 epoch fields and pk-01b operationClass/pickerStep diagnostics. Only REPO/helper
paths and an unconditional explicit `throw 'actions'` immediately after
`WaitPhase 'ui-import-cancelled'` were changed. The stop precedes Import Commit;
there is no undefined Fail call. That planned stop was not reached in this run.
Monitor preflight additionally records live DISPLAY names; process audits also
recognize the copied picker PowerShell helper.

## Commands and fresh witness

`<scratchpad>` is the private brief/report directory. No original helper edited.

```text
pwsh -NoProfile -File scripts/uno_candidate_native_fixture/build.ps1 -BuildOnly
pwsh -NoProfile -File <scratchpad>/nr10-build-support.ps1
pwsh -NoProfile -File scripts/verify-uno-candidate.ps1 -BuildOnly
pwsh -NoProfile -File scripts/publish-uno-candidate-aot.ps1
python -I -B <scratchpad>/nr10-artifacts.py
python -I -B scripts/run_uno_candidate_native.py --validate-build <native-entry> <native-sha256>
python -I -B <scratchpad>/nr10-dry-admission.py
pwsh -NoProfile -File <scratchpad>/nr10-parse-helpers.ps1
pwsh -NoProfile -File <scratchpad>/nr10-process-check.ps1
# PHASE0-READY written, then filesystem/time gate:
python -I -B <scratchpad>/nr10-wait-grant.py
python -I -B <scratchpad>/nr10-admit-window.py
python -I -B <scratchpad>/nr10-preflight.py
pwsh -NoProfile -File <scratchpad>/nr10-process-check.ps1
python -I -B <scratchpad>/nr10-run.py
# Parent invokes guarded seed, actual native main, then last picker.
# Close all nine tracked flags plus copied UI flag, then:
git diff --exit-code -- <six temporarily changed tracked source files>
pwsh -NoProfile -File <scratchpad>/nr10-process-check.ps1
python -I -B <scratchpad>/nr10-clipboard-check.py
# SCREEN-DONE, then filesystem-only evidence:
python -I -B <scratchpad>/nr10-evidence-check.py ss-uno018-ui-f43da7783db451b8ebbb03a40b6b714c
python -I -B <scratchpad>/nr10-artifacts.py
pwsh -NoProfile -File <scratchpad>/nr10-parse-helpers.ps1
```

Trusted detached parent `ss-uno018-ui-f43da7783db451b8ebbb03a40b6b714c`
completed with `nr10_observer_complete=1` and empty stderr.
The actual native runner received exact native/target/guardian executable paths
and SHA256 pins, --authorized-screen-slot and --scene.
Restore → abort → owner-refusal passed their actual returned
RestorativeSessionObservation objects directly within this parent's RAM.
The last same-artifact witness was revalidated there before positives; no
discovered directory, old receipt or JSON reconstruction supplied authority.

All application/recovery/helper counts were zero before initial seed and between
independent scenes, apart from the exact trusted runner itself. With no pending
recovery, the authorized fixed seed ran once before controls and once before each
of six positive scenes, seven times total, never during recovery:
`"Smart Snippets QA" | & "$env:SystemRoot\System32\clip.exe"`.
Fresh-baseline flags truthfully accompanied positives. Copy never supplied a
restorative witness. Each failed scene stopped once; later positive scenes were
independent authorized attempts, not retries.

## Per-step ledger

| Phase / step | Result | Independent usage / clipboard epoch |
|---|---|---|
| 0 | PASS | no clipboard access |
| 1: restore, abort, owner-refusal | PASS each | 0 rows / 0 usage each; positive epoch predicate not applicable |
| 2: posted-row | PASS | 1 row / usage 1 PASS; changed epoch PASS |
| 2: posted-main | FAIL | 1 row / usage 0 FAIL; unchanged epoch FAIL |
| 2: popup-copy, editor-copy, footer-copy | FAIL each | zero usage PASS only; unchanged Copy epoch FAIL |
| 2: context-insert | FAIL | 1 row / usage 0 FAIL; unchanged epoch FAIL |
| posted-enter | NOT RUN / BLOCKED | input synthesis excluded; no session |
| 3: Import Cancel | FAIL | 0 rows / 0 usage; clipboard epoch unchanged |
| registered-* / later picker phases | NOT RUN | excluded |
| Release | PASS | all flags false; zero owned processes; synthetic baseline verified |

Every completed native stream below was privacy **CLEAN**, planted negative
controls **PASS**, stderr empty, no pending recovery receipt.
FailureStage is the last-entered stage, not an exact predicate location.
All available fixed startup/readiness/preparation/grant/failure records and
**every full trace line verbatim** follow. Trace order and monitor order are
separate counters; trace timestamps are buffered emission times, not sample times
(use elapsed_ms for sampled intervals).

### restore — PASS

Session: `ss-uno018-ui-cdebe75e2bbc28dac4c98ed9a7330f0b/ss-uno019-native-f00adca665544a81a89cc65a52699439`.
FailureStage: `none`.
Target foreground grant: **absent**.
Rows/usage: 0/0; independent usage **PASS**.
Positive clipboard-epoch predicate: not applicable.
Receipt/host check: **PASS**; host shutdown-complete true.
Monitor failure: absent.


```text
2026-11-01T23:53:36.864Z INFO native_startup_foreground phase=target_launch relation=foreign launcher_foreground=False sample_order=1
2026-11-01T23:53:36.864Z INFO native_startup_foreground phase=host_ready relation=foreign launcher_foreground=False sample_order=2
2026-11-01T23:53:36.864Z INFO native_host_readiness reason=ready
2026-11-01T23:53:37.135Z INFO native_control_admission reason=Ready
2026-11-01T23:53:37.241Z INFO native_control_publish status=swapped
```

### abort — PASS

Session: `ss-uno018-ui-a6762e2d71aa40daf4f27eb4301fe424/ss-uno019-native-527840df93db49b48ccc007edb648120`.
FailureStage: `none`.
Target foreground grant: **absent**.
Rows/usage: 0/0; independent usage **PASS**.
Positive clipboard-epoch predicate: not applicable.
Receipt/host check: **PASS**; host shutdown-complete false.
Monitor failure: absent.


```text
2026-11-01T23:53:41.842Z INFO native_startup_foreground phase=target_launch relation=foreign launcher_foreground=False sample_order=1
2026-11-01T23:53:41.842Z INFO native_startup_foreground phase=host_ready relation=foreign launcher_foreground=False sample_order=2
2026-11-01T23:53:41.842Z INFO native_host_readiness reason=ready
2026-11-01T23:53:42.006Z INFO native_control_admission reason=Ready
2026-11-01T23:53:42.108Z INFO native_control_publish status=swapped
```

### owner-refusal — PASS

Session: `ss-uno018-ui-ef01f240778c7466036e4905be0fb60b/ss-uno019-native-be9238aa6df94c95a1e5fe83afb21a01`.
FailureStage: `none`.
Target foreground grant: **absent**.
Rows/usage: 0/0; independent usage **PASS**.
Positive clipboard-epoch predicate: not applicable.
Receipt/host check: **PASS**; host shutdown-complete true.
Monitor failure: absent.


```text
2026-11-01T23:53:46.567Z INFO native_startup_foreground phase=target_launch relation=foreign launcher_foreground=False sample_order=1
2026-11-01T23:53:46.567Z INFO native_startup_foreground phase=host_ready relation=foreign launcher_foreground=False sample_order=2
2026-11-01T23:53:46.567Z INFO native_host_readiness reason=ready
2026-11-01T23:53:46.734Z INFO native_control_admission reason=Ready
2026-11-01T23:53:46.835Z INFO native_control_publish status=swapped
```

### posted-row — PASS

Session: `ss-uno018-ui-5ceb0e05a87dfa08a6292ecad3c0c3bc/ss-uno019-native-fb1dcce1ecf74f95a7b981f7059ca8c4`.
FailureStage: `none`.
Target foreground grant: **granted**.
Rows/usage: 1/1; independent usage **PASS**.
Outer clipboard epoch **changed**; independent combined boundary **PASS**.
Receipt/host check: **PASS**; host shutdown-complete true.
Monitor failure: absent.


```text
2026-11-01T23:53:54.327Z INFO native_startup_foreground phase=target_launch relation=foreign launcher_foreground=False sample_order=1
2026-11-01T23:53:54.327Z INFO native_startup_foreground phase=host_ready relation=foreign launcher_foreground=False sample_order=2
2026-11-01T23:53:54.328Z INFO native_host_readiness reason=ready
2026-11-01T23:53:54.506Z INFO owned_initial_focus phase=waiting
2026-11-01T23:53:54.506Z INFO native_startup_foreground phase=preparation_start relation=foreign launcher_foreground=False sample_order=3
2026-11-01T23:53:54.547Z INFO owned_preparation_admission reason=ready
2026-11-01T23:53:54.678Z INFO native_startup_foreground phase=before_focus relation=foreign launcher_foreground=False sample_order=4
2026-11-01T23:53:54.679Z INFO native_startup_foreground phase=after_focus relation=none launcher_foreground=False sample_order=5
2026-11-01T23:53:54.935Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=1 elapsed_ms=0
2026-11-01T23:53:54.935Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=2 elapsed_ms=14
2026-11-01T23:53:54.935Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=3 elapsed_ms=29
2026-11-01T23:53:54.935Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=4 elapsed_ms=44
2026-11-01T23:53:54.935Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=5 elapsed_ms=59
2026-11-01T23:53:54.936Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=6 elapsed_ms=74
2026-11-01T23:53:54.936Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=7 elapsed_ms=89
2026-11-01T23:53:54.936Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=8 elapsed_ms=104
2026-11-01T23:53:54.936Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=9 elapsed_ms=119
2026-11-01T23:53:54.936Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=10 elapsed_ms=135
2026-11-01T23:53:54.936Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=11 elapsed_ms=149
2026-11-01T23:53:54.936Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=12 elapsed_ms=164
2026-11-01T23:53:54.937Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=13 elapsed_ms=179
2026-11-01T23:53:54.937Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=14 elapsed_ms=195
2026-11-01T23:53:54.937Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=15 elapsed_ms=210
2026-11-01T23:53:54.937Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=16 elapsed_ms=225
2026-11-01T23:53:54.937Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=17 elapsed_ms=240
2026-11-01T23:53:55.105Z INFO owned_target_preparation reason=Prepared
2026-11-01T23:53:55.124Z INFO owned_initial_focus phase=ready
2026-11-01T23:53:55.160Z INFO target_foreground_grant result=granted
2026-11-01T23:53:55.861Z INFO native_popup_focus reason=foreground_accepted
2026-11-01T23:53:55.861Z INFO candidate_popup_focus reason=accepted
2026-11-01T23:53:56.423Z INFO clipboard_swap attempts=1 status=Swapped saved=4 skipped_by_design=0 unsaved=0 cap_exceeded=False bytes=104 ms=159
2026-11-01T23:53:57.005Z INFO clipboard_restore status=Restored attempts=1
2026-11-01T23:53:57.005Z INFO insert_result outcome=Inserted id=1 notice=False ms=1099
```

### posted-main — FAIL

Session: `ss-uno018-ui-67bc809443952c541f7107aba6616895/ss-uno019-native-0699e45f09fd47318b491dfc0c744d73`.
FailureStage: `guardian_ready`.
Target foreground grant: **absent**.
Rows/usage: 1/0; independent usage **FAIL**.
Outer clipboard epoch **unchanged**; independent combined boundary **FAIL**.
Receipt/host check: **FAIL_ValueError**; host shutdown-complete false.
Monitor failure:
```json
{"predicate":"foreground_foreign","exitCode":2,"sampleOrder":21,"relation":"same_pid_other","closed":false}
```


```text
2026-11-01T23:54:03.188Z INFO native_startup_foreground phase=target_launch relation=foreign launcher_foreground=False sample_order=1
2026-11-01T23:54:03.188Z INFO native_startup_foreground phase=host_ready relation=foreign launcher_foreground=False sample_order=2
2026-11-01T23:54:03.188Z INFO native_host_readiness reason=ready
2026-11-01T23:54:03.368Z INFO owned_initial_focus phase=waiting
2026-11-01T23:54:03.368Z INFO native_startup_foreground phase=preparation_start relation=foreign launcher_foreground=False sample_order=3
2026-11-01T23:54:03.409Z INFO owned_preparation_admission reason=ready
2026-11-01T23:54:03.548Z INFO native_startup_foreground phase=before_focus relation=foreign launcher_foreground=False sample_order=4
2026-11-01T23:54:03.567Z INFO native_startup_foreground phase=after_focus relation=owned_a launcher_foreground=False sample_order=5
2026-11-01T23:54:03.831Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=1 elapsed_ms=0
2026-11-01T23:54:03.831Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=2 elapsed_ms=19
2026-11-01T23:54:03.831Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=3 elapsed_ms=34
2026-11-01T23:54:03.831Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=4 elapsed_ms=49
2026-11-01T23:54:03.831Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=5 elapsed_ms=64
2026-11-01T23:54:03.831Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=6 elapsed_ms=79
2026-11-01T23:54:03.831Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=7 elapsed_ms=95
2026-11-01T23:54:03.832Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=8 elapsed_ms=110
2026-11-01T23:54:03.832Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=9 elapsed_ms=125
2026-11-01T23:54:03.832Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=10 elapsed_ms=140
2026-11-01T23:54:03.832Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=11 elapsed_ms=155
2026-11-01T23:54:03.832Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=12 elapsed_ms=170
2026-11-01T23:54:03.832Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=13 elapsed_ms=185
2026-11-01T23:54:03.832Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=14 elapsed_ms=201
2026-11-01T23:54:03.833Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=15 elapsed_ms=217
2026-11-01T23:54:03.833Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=16 elapsed_ms=232
2026-11-01T23:54:03.833Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=17 elapsed_ms=248
2026-11-01T23:54:03.834Z INFO owned_target_preparation reason=ForegroundRefused
2026-11-01T23:54:03.834Z INFO owned_initial_focus phase=refused
2026-11-01T23:54:03.834Z ERROR native_positive_failed stage=guardian_ready InvalidOperationException (HResult=0x80131509)
```

### popup-copy — FAIL

Session: `ss-uno018-ui-3a1b9521875d6246900e551fe49717d7/ss-uno019-native-5c4254df64c54a7d978a8d3218af09b4`.
FailureStage: `guardian_ready`.
Target foreground grant: **absent**.
Rows/usage: 1/0; independent usage **PASS**.
Outer clipboard epoch **unchanged**; independent combined boundary **FAIL**.
Receipt/host check: **FAIL_ValueError**; host shutdown-complete false.
Monitor failure:
```json
{"predicate":"foreground_foreign","exitCode":2,"sampleOrder":22,"relation":"same_pid_other","closed":false}
```


```text
2026-11-01T23:54:09.297Z INFO native_startup_foreground phase=target_launch relation=foreign launcher_foreground=False sample_order=1
2026-11-01T23:54:09.297Z INFO native_startup_foreground phase=host_ready relation=foreign launcher_foreground=False sample_order=2
2026-11-01T23:54:09.297Z INFO native_host_readiness reason=ready
2026-11-01T23:54:09.577Z INFO owned_initial_focus phase=waiting
2026-11-01T23:54:09.577Z INFO native_startup_foreground phase=preparation_start relation=foreign launcher_foreground=False sample_order=3
2026-11-01T23:54:09.615Z INFO owned_preparation_admission reason=ready
2026-11-01T23:54:09.747Z INFO native_startup_foreground phase=before_focus relation=foreign launcher_foreground=False sample_order=4
2026-11-01T23:54:09.783Z INFO native_startup_foreground phase=after_focus relation=owned_a launcher_foreground=False sample_order=5
2026-11-01T23:54:10.045Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=1 elapsed_ms=0
2026-11-01T23:54:10.046Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=2 elapsed_ms=16
2026-11-01T23:54:10.046Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=3 elapsed_ms=32
2026-11-01T23:54:10.046Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=4 elapsed_ms=47
2026-11-01T23:54:10.046Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=5 elapsed_ms=62
2026-11-01T23:54:10.046Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=6 elapsed_ms=78
2026-11-01T23:54:10.046Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=7 elapsed_ms=93
2026-11-01T23:54:10.047Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=8 elapsed_ms=108
2026-11-01T23:54:10.047Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=9 elapsed_ms=124
2026-11-01T23:54:10.047Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=10 elapsed_ms=139
2026-11-01T23:54:10.047Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=11 elapsed_ms=154
2026-11-01T23:54:10.047Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=12 elapsed_ms=169
2026-11-01T23:54:10.047Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=13 elapsed_ms=184
2026-11-01T23:54:10.048Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=14 elapsed_ms=200
2026-11-01T23:54:10.048Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=15 elapsed_ms=215
2026-11-01T23:54:10.048Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=16 elapsed_ms=231
2026-11-01T23:54:10.048Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=17 elapsed_ms=246
2026-11-01T23:54:10.049Z INFO owned_target_preparation reason=ForegroundRefused
2026-11-01T23:54:10.049Z INFO owned_initial_focus phase=refused
2026-11-01T23:54:10.049Z ERROR native_positive_failed stage=guardian_ready InvalidOperationException (HResult=0x80131509)
```

### editor-copy — FAIL

Session: `ss-uno018-ui-adb1c5c596c00452fb0c8f1735c850d6/ss-uno019-native-45fafc27a3ef428d9d7b73908a3eb531`.
FailureStage: `guardian_ready`.
Target foreground grant: **absent**.
Rows/usage: 1/0; independent usage **PASS**.
Outer clipboard epoch **unchanged**; independent combined boundary **FAIL**.
Receipt/host check: **FAIL_ValueError**; host shutdown-complete false.
Monitor failure:
```json
{"predicate":"foreground_foreign","exitCode":2,"sampleOrder":25,"relation":"same_pid_other","closed":false}
```


```text
2026-11-01T23:54:15.530Z INFO native_startup_foreground phase=target_launch relation=foreign launcher_foreground=False sample_order=1
2026-11-01T23:54:15.530Z INFO native_startup_foreground phase=host_ready relation=foreign launcher_foreground=False sample_order=2
2026-11-01T23:54:15.531Z INFO native_host_readiness reason=ready
2026-11-01T23:54:15.903Z INFO owned_initial_focus phase=waiting
2026-11-01T23:54:15.903Z INFO native_startup_foreground phase=preparation_start relation=foreign launcher_foreground=False sample_order=3
2026-11-01T23:54:15.943Z INFO owned_preparation_admission reason=ready
2026-11-01T23:54:16.071Z INFO native_startup_foreground phase=before_focus relation=foreign launcher_foreground=False sample_order=4
2026-11-01T23:54:16.093Z INFO native_startup_foreground phase=after_focus relation=owned_a launcher_foreground=False sample_order=5
2026-11-01T23:54:16.356Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=1 elapsed_ms=0
2026-11-01T23:54:16.356Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=2 elapsed_ms=18
2026-11-01T23:54:16.357Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=3 elapsed_ms=34
2026-11-01T23:54:16.357Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=4 elapsed_ms=49
2026-11-01T23:54:16.357Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=5 elapsed_ms=65
2026-11-01T23:54:16.357Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=6 elapsed_ms=80
2026-11-01T23:54:16.357Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=7 elapsed_ms=95
2026-11-01T23:54:16.357Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=8 elapsed_ms=110
2026-11-01T23:54:16.357Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=9 elapsed_ms=125
2026-11-01T23:54:16.357Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=10 elapsed_ms=140
2026-11-01T23:54:16.357Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=11 elapsed_ms=156
2026-11-01T23:54:16.358Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=12 elapsed_ms=171
2026-11-01T23:54:16.358Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=13 elapsed_ms=186
2026-11-01T23:54:16.358Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=14 elapsed_ms=202
2026-11-01T23:54:16.358Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=15 elapsed_ms=217
2026-11-01T23:54:16.358Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=16 elapsed_ms=233
2026-11-01T23:54:16.358Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=17 elapsed_ms=247
2026-11-01T23:54:16.359Z INFO owned_target_preparation reason=ForegroundRefused
2026-11-01T23:54:16.359Z INFO owned_initial_focus phase=refused
2026-11-01T23:54:16.359Z ERROR native_positive_failed stage=guardian_ready InvalidOperationException (HResult=0x80131509)
```

### footer-copy — FAIL

Session: `ss-uno018-ui-00381069bbbbc122720bccb7612459eb/ss-uno019-native-3ff20d12ee3649ba87fc97bd5f4c5325`.
FailureStage: `guardian_ready`.
Target foreground grant: **absent**.
Rows/usage: 1/0; independent usage **PASS**.
Outer clipboard epoch **unchanged**; independent combined boundary **FAIL**.
Receipt/host check: **FAIL_ValueError**; host shutdown-complete false.
Monitor failure:
```json
{"predicate":"foreground_foreign","exitCode":2,"sampleOrder":26,"relation":"same_pid_other","closed":false}
```


```text
2026-11-01T23:54:21.587Z INFO native_startup_foreground phase=target_launch relation=foreign launcher_foreground=False sample_order=1
2026-11-01T23:54:21.587Z INFO native_startup_foreground phase=host_ready relation=foreign launcher_foreground=False sample_order=2
2026-11-01T23:54:21.588Z INFO native_host_readiness reason=ready
2026-11-01T23:54:22.067Z INFO owned_initial_focus phase=waiting
2026-11-01T23:54:22.067Z INFO native_startup_foreground phase=preparation_start relation=foreign launcher_foreground=False sample_order=3
2026-11-01T23:54:22.106Z INFO owned_preparation_admission reason=ready
2026-11-01T23:54:22.236Z INFO native_startup_foreground phase=before_focus relation=foreign launcher_foreground=False sample_order=4
2026-11-01T23:54:22.265Z INFO native_startup_foreground phase=after_focus relation=owned_a launcher_foreground=False sample_order=5
2026-11-01T23:54:22.518Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=1 elapsed_ms=0
2026-11-01T23:54:22.518Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=2 elapsed_ms=10
2026-11-01T23:54:22.518Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=3 elapsed_ms=26
2026-11-01T23:54:22.519Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=4 elapsed_ms=41
2026-11-01T23:54:22.519Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=5 elapsed_ms=56
2026-11-01T23:54:22.519Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=6 elapsed_ms=71
2026-11-01T23:54:22.519Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=7 elapsed_ms=86
2026-11-01T23:54:22.519Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=8 elapsed_ms=102
2026-11-01T23:54:22.519Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=9 elapsed_ms=117
2026-11-01T23:54:22.519Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=10 elapsed_ms=133
2026-11-01T23:54:22.520Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=11 elapsed_ms=147
2026-11-01T23:54:22.520Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=12 elapsed_ms=162
2026-11-01T23:54:22.520Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=13 elapsed_ms=178
2026-11-01T23:54:22.520Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=14 elapsed_ms=193
2026-11-01T23:54:22.520Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=15 elapsed_ms=208
2026-11-01T23:54:22.520Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=16 elapsed_ms=223
2026-11-01T23:54:22.520Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=17 elapsed_ms=238
2026-11-01T23:54:22.521Z INFO owned_target_preparation reason=ForegroundRefused
2026-11-01T23:54:22.521Z INFO owned_initial_focus phase=refused
2026-11-01T23:54:22.521Z ERROR native_positive_failed stage=guardian_ready InvalidOperationException (HResult=0x80131509)
```

### context-insert — FAIL

Session: `ss-uno018-ui-f068ba028764647e60419b8a4b5fb675/ss-uno019-native-37af363083e048619e94b2f01ef4ecc2`.
FailureStage: `guardian_ready`.
Target foreground grant: **absent**.
Rows/usage: 1/0; independent usage **FAIL**.
Outer clipboard epoch **unchanged**; independent combined boundary **FAIL**.
Receipt/host check: **FAIL_ValueError**; host shutdown-complete false.
Monitor failure:
```json
{"predicate":"foreground_foreign","exitCode":2,"sampleOrder":20,"relation":"same_pid_other","closed":false}
```


```text
2026-11-01T23:54:27.590Z INFO native_startup_foreground phase=target_launch relation=foreign launcher_foreground=False sample_order=1
2026-11-01T23:54:27.590Z INFO native_startup_foreground phase=host_ready relation=foreign launcher_foreground=False sample_order=2
2026-11-01T23:54:27.591Z INFO native_host_readiness reason=ready
2026-11-01T23:54:27.760Z INFO owned_initial_focus phase=waiting
2026-11-01T23:54:27.761Z INFO native_startup_foreground phase=preparation_start relation=foreign launcher_foreground=False sample_order=3
2026-11-01T23:54:27.801Z INFO owned_preparation_admission reason=ready
2026-11-01T23:54:27.931Z INFO native_startup_foreground phase=before_focus relation=foreign launcher_foreground=False sample_order=4
2026-11-01T23:54:27.946Z INFO native_startup_foreground phase=after_focus relation=owned_a launcher_foreground=False sample_order=5
2026-11-01T23:54:28.211Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=1 elapsed_ms=0
2026-11-01T23:54:28.211Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=2 elapsed_ms=6
2026-11-01T23:54:28.211Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=3 elapsed_ms=21
2026-11-01T23:54:28.211Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=4 elapsed_ms=36
2026-11-01T23:54:28.211Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=5 elapsed_ms=52
2026-11-01T23:54:28.212Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=6 elapsed_ms=67
2026-11-01T23:54:28.212Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=7 elapsed_ms=83
2026-11-01T23:54:28.212Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=8 elapsed_ms=98
2026-11-01T23:54:28.212Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=9 elapsed_ms=114
2026-11-01T23:54:28.212Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=10 elapsed_ms=129
2026-11-01T23:54:28.212Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=11 elapsed_ms=144
2026-11-01T23:54:28.212Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=12 elapsed_ms=159
2026-11-01T23:54:28.213Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=13 elapsed_ms=174
2026-11-01T23:54:28.213Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=14 elapsed_ms=190
2026-11-01T23:54:28.213Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=15 elapsed_ms=204
2026-11-01T23:54:28.213Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=16 elapsed_ms=219
2026-11-01T23:54:28.213Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=17 elapsed_ms=234
2026-11-01T23:54:28.213Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=18 elapsed_ms=249
2026-11-01T23:54:28.214Z INFO owned_target_preparation reason=ForegroundRefused
2026-11-01T23:54:28.214Z INFO owned_initial_focus phase=refused
2026-11-01T23:54:28.214Z ERROR native_positive_failed stage=guardian_ready InvalidOperationException (HResult=0x80131509)
```

Controls all admitted Ready and published Swapped. Abort deliberately stops only
the worker/target Job after publication; independent guardian restoration passed.
Abort is not graceful worker shutdown. Owner-refusal exercises owned guardian
HWND A→B within the same PID, not a foreign clipboard producer.
Posted-row passed exact A/CRLF readback, empty B, single capture/paste, duplicate
Busy/no-op and one usage, actual host shutdown and independent boundaries.
Proof limit remains **registered_posted_callback_not_physical** with
**vm_callback** action proof, not physical shortcut/pointer/menu delivery.
No failed Copy/main/context scene reached its intended consumer or the editor
Save-failure trigger; zero Copy usage alone is not a successful Copy.

## Trace interpretation — target-PID other top-level, not a proven caller

All six positives have startup target_launch/host_ready/preparation_start/
before_focus relation=foreign, launcher_foreground=False and host readiness=ready.
Successful posted-row sampled after_focus=none, then its first trace sample was
a_other_window/toplevel_other at 0ms, followed by exact owned_a from 14–240ms.
Preparation/grant/insertion then passed. The other-window sample is not itself
authority or evidence of a failed scene.

Each of the five failures sampled after_focus=owned_a. Its trace then moved from
owned_a to **a_other_window kind=toplevel_other** and stayed in that category:

| Scene | First other-window trace sample | Last trace sample |
|---|---|---|
| posted-main | order 3, 34ms | order 17, 248ms |
| popup-copy | order 2, 16ms | order 17, 246ms |
| editor-copy | order 2, 18ms | order 17, 247ms |
| footer-copy | order 2, 10ms | order 17, 238ms |
| context-insert | order 4, 36ms | order 18, 249ms |

Every trace sample has initial_foreground=False. No trace sample classified worker,
guardian, coordinator, launcher, foreign, none, ime, tooltip or owned_by_a.
These are sampled observations, not exclusions between samples. All five independent
monitor records say foreground_foreign / same_pid_other / closed=false.

This narrows the observed replacement foreground to Target018's PID at those reads,
rather than identifying an external foreground thief. `toplevel_other` means only
the current classifier did not recognize IME/MSCTFIME UI, tooltip or direct GW_OWNER=A;
it does not disclose an exact class/window/caller or prove benignness.
The high-rate trace does not separately classify B; the independent monitor checks
exact A and B before same_pid_other, so at **its** failed read the window was neither
pinned A nor B. These independent reads cannot prove an identical HWND persisted
through all trace samples. No raw HWND/PID/class/title was emitted in these
diagnostic records.

ForegroundRefused was latched by the unchanged post-Guard check before awaiting
trace completion; delayed trace emission is not a late new grant check.
There were 17 samples per scene except context-insert's 18, with observed intervals
often about 15ms despite requested 5ms delays. No continuous/5ms-resolution guarantee.
No result proves a never-activated A, foreground-lock timeout, physical input source,
specific provider/WM handler or causal caller. Further bounded, reviewed diagnostics
would be needed to distinguish the exact target-PID other surface; no same-PID
admission, settling/rebaseline, focus retry or guard relaxation is justified.

## Phase 3 — Import Cancel FAIL; epoch diagnostics captured

Picker parent: `ss-uno018-ui-06ccbce70fd78a542f9082e9040770d1`.
Exact session: `ss-uno018-ui-917d90f745d0e0abcdf4b687c29e2ed2`.
Wrapper/child exit 1. Complete fixed failure JSON:


```json
{
  "observer-parent-failure.json": {
    "reason": "worker_protocol"
  },
  "observer-worker-failure.json": {
    "reason": "input",
    "inputSubreason": "epoch_changed",
    "inputEpochDelta": 5875,
    "inputIdleMs": 31,
    "inputPhase": "import-cancel"
  }
}
```

The discriminating refusal is **reason=input, inputSubreason=epoch_changed**,
with **inputEpochDelta=5875**, **inputIdleMs=31**,
**inputPhase=import-cancel**. Delta is unsigned timestamp subtraction, not an event
count or proof of human/system origin. Phase is the observed marker, not proof that
Cancel Invoke completed. No tolerated delta or rebaseline was introduced.

Helper stdout/stderr were empty: no reason/operationClass/pickerStep/HRESULT helper
line; no picker_schema_multiple/per-match record or foreground failure file exists.
These are absent, not replaced with older diagnostics. Privacy CLEAN, planted
negative controls PASS; owned read-only aggregate 0 rows / 0 usage; independent
clipboard epoch unchanged.

Import Cancel active marker exists; closed marker, action receipt and
ui-import-cancelled receipt are absent. All later Import/Export markers absent.
The current-source scratch helper's explicit throw was never reached.
Existing owned helper/fixture Job failure cleanup completed; no graceful failed-host
shutdown or pre-kill JobZero success is claimed. No recovery-bearing process killed.
The generic wrapper's historic field name `phase2` is not this brief's phase number.

## Release — PASS; SCREEN-DONE 17:54:52 local

All nine tracked booleans and copied UI gate restored false. Exact grep proof:

```text
scripts/run_uno_candidate_startup.py:21:STARTUP_OBSERVER_REVIEWED = False
scripts/run_uno_candidate_acceptance.py:23:FULL_HOST_OBSERVER_REVIEWED = False
prototypes/uno-native-target/windows-insertion/Program.cs:11:    private static readonly bool NativeEntryReviewed = false;
src/SmartSnippets.Uno/Platforms/Windows/AcceptanceNative/NativeAcceptanceFactory.cs:14:    internal static readonly bool SourceReviewed = false;
scripts/uno_candidate_native_fixture/Program.cs:14:    internal static readonly bool NativeFixtureSourceReviewed = false;
scripts/uno_candidate_native_fixture/PositiveProtocol.cs:8:    internal static readonly bool SourceReviewed = false;
scripts/uno_candidate_native_fixture/PositiveProtocol.cs:9:    internal static readonly bool InputSourceReviewed = false;
scripts/run_uno_candidate_ui.py:20:UI_SOURCE_REVIEWED = False
scripts/uno_candidate_native_fixture/OwnedTargetPreparation.cs:26:    internal static readonly bool SourceReviewed = false;
scripts/run_uno_candidate_native.py:19:NATIVE_SCENES_SOURCE_REVIEWED = False
scripts/run_uno_candidate_native.py:20:POSITIVE_SCENES_SOURCE_REVIEWED = False
scripts/run_uno_candidate_native.py:21:POSITIVE_INPUT_SOURCE_REVIEWED = False
20:UI_SOURCE_REVIEWED = False
```

`git diff --exit-code` passed for all six temporarily changed tracked sources.
Final process audit:

```json
{"observedUtc":"2026-11-01T23:54:49.5630138+00:00","counts":{"SmartSnippets.UnoCandidate.exe":0,"SmartSnippets.UnoAcceptanceFixture.exe":0,"SmartSnippets.UnoCandidate.Guardian.exe":0,"SmartSnippets.Target018.exe":0,"nr10PythonHelpers":0,"UIHelper":0}}
```

Final locked RAM-only clipboard check, with no post-run reseed:

```json
{"builtin_text":4,"ownership_marker":0,"protective_metadata":0,"unsupported":0,"owner_present":false,"authorized_synthetic_baseline_matches":true,"sequence_stable":true}
```

This verifies the authorized synthetic baseline, not restoration of pre-seed user
data or immunity to later unrelated changes. No ownership/protective marker,
unsupported format, owner, retained coordinator/guardian or uncertain keyboard
lifetime remained. No pending process was killed, recovery endpoint invoked or
synthetic release sent.

`SCREEN-DONE nr-10 2026-11-01 17:54:52 local (-06:00)` was written immediately
to the report after release checks, well before hard end 18:18.
All subsequent work was filesystem-only diagnostic extraction, hash rechecking
and documentation. No profile/evidence deleted.

Private retained evidence: nr10 artifact/build/source receipts, exact enabled
diff, helper hashes and syntax checks, two dry admission PASS records,
grant polls/time admission, live monitor/preflight/process audits, exact parent/
session logs, nr10-evidence.txt, final flag/process/clipboard proofs and post-run
artifact recheck. Commit scope: **only this research document**; no enabled flags,
helper, operator edit, source fix or push.

Pending: explain the target-PID other top-level foreground transition and picker
epoch change without weakening guards, then separately authorized fresh-source
acceptance of main/Copy/context and remaining fault/lifecycle/pointer/hover/tray/
oldest-runtime obligations. No active recovery or operator action pending.
This released run grants no retry or further runtime authority and closes neither
Plan 018 nor Plan 019.
