# 056 — nr-13: foreground-root native run

Authorized brief `native-run14-brief.md`, 2026-11-01; root source HEAD `35c9ed408fb893f954914ac358386d21f835ea57`, branch `feature/uno019-native-trial12`. **Phase-0 redo PASS; controls PASS; three positives PASS and three FAIL; picker Import Cancel FAIL; desktop released.** Plans 018/019 remain incomplete. No production/native parity, physical shortcut/pointer proof or release readiness is claimed.

## Authorization and phase-0 redo

Read AGENTS, safety guide 002, progress handoff 005, approved Plan 019, research 053–055 and both fixture READMEs. Preserved operator-local AGENTS/safety-guide edits; never staged them. No delegation or push. Only bottom-left DISPLAY29 was authorized; no primary/top-left window was controlled. Ordinary candidate was built, never launched.

Initial phase 0 failed before any build or application launch: scratch copy used the Windows default cp1252 codec and raised UnicodeEncodeError while copying the evidence helper. The shell erroneously continued; dry admission attempt 1 exited 64 because the seed helper did not exist, with fixed reason guard_error. It never ran the guard body. The report immediately recorded PHASE0-FAILED helper_copy_encoding; all flags were closed and processes zero. This is an agent orchestration failure, not a product safety refusal. Nothing touched the clipboard or opened a window.

The leader explicitly authorized **one phase-0 redo**, not a scene retry: delete partial nr13 Python/PowerShell copies, use explicit UTF-8 (or bytes), fail-fast chaining, then perform full phase 0 and wait for the grant. The report replaced the failure marker with `phase-0 redo 1: helper_copy_encoding fixed`. All partial helper copies were deleted; all copy reads used utf-8-sig and writes utf-8; shell steps used set -e and build wrappers checked exit codes. The redo completed at **19:46:38 local**, when `PHASE0-READY nr-13` was written immediately. No scene was retried.

Filesystem-only polling:

```jsonl

{"status": "WAITING", "polls": 1, "waitSeconds": 0.001, "observedUtc": "2026-11-02T01:46:43.355135+00:00"}
{"status": "GRANT_AVAILABLE", "polls": 2, "waitSeconds": 30.012, "observedUtc": "2026-11-02T01:47:13.366192+00:00"}

```



Exact grant:

```text

GRANTED nr-13
window: EXCLUSIVE full desktop; harness windows on bottom-left DISPLAY29 (-1920,1506 1920x1080) only; never primary or top-left
start: 2026-11-01 19:46 local
hard end: 2026-11-01 20:11 local
scope: controls (restore, abort, owner-refusal); posted-row, posted-main; popup-copy, editor-copy, footer-copy, context-insert (synthetic text, clipboard restore); optional picker Import Cancel only if >=5 min remain before hard end
excluded: posted-enter (SendInput, D50); registered-* scenes
rules: owned throwaway profiles; posted callbacks only; no keyboard/mouse synthesis; stop and clean up by hard end
source: pc-manager, under the user's pre-approval for bottom-left runs of this kind

```



Grant/time admission:

```json

{
  "status": "GRANTED",
  "slotStartEpoch": 1793583960.0,
  "hardEndEpoch": 1793585460.0,
  "deadlineMonotonic": 388137.6017617912,
  "observedUtc": "2026-11-02T01:47:13.441427+00:00",
  "slotStartLocal": "2026-11-01 19:46",
  "hardEndLocal": "2026-11-01 20:11"
}

```



Live geometry and bounded held-input preflight (the existing downstream 1500 ms quiet guard remained unchanged):

```text

{"monitors": [{"display": "\\\\.\\DISPLAY5", "bounds": [0, 2160, 2560, 3760], "work": [0, 2160, 2560, 3688], "primary": false}, {"display": "\\\\.\\DISPLAY2", "bounds": [3840, 47, 5040, 1967], "work": [3840, 47, 5040, 1919], "primary": false}, {"display": "\\\\.\\DISPLAY3", "bounds": [0, 0, 3840, 2160], "work": [0, 0, 3840, 2088], "primary": true}, {"display": "\\\\.\\DISPLAY29", "bounds": [-1920, 1506, 0, 2586], "work": [-1920, 1506, 0, 2538], "primary": false}, {"display": "\\\\.\\DISPLAY30", "bounds": [-1920, 306, 0, 1506], "work": [-1920, 306, 0, 1458], "primary": false}], "authorized": {"display": "\\\\.\\DISPLAY29", "bounds": [-1920, 1506, 0, 2586], "work": [-1920, 1506, 0, 2538], "primary": false}}
{"sampleOrder": 1, "held_virtual_keys": [], "inputIdleMs": 156, "elapsedSeconds": 0.002}
nr13_preflight_quiet=1

```



The live primary DISPLAY3 was 3840×2160; the authorized monitor remained nonprimary DISPLAY29, bounds (-1920,1506,0,2586), work (-1920,1506,0,2538). No stale DISPLAY assumptions were used. No keyboard/mouse synthesis by the agent, generic key-up, clock change, download, registered scene or posted-enter occurred. Product-guarded paste into retained owned A was the only insertion input. UIA SetFocus was the reviewed owned activation, never nonactivation proof.

## Phase 0 — PASS (authorized redo 1)

Exactly nine tracked booleans temporarily enabled: native runner NATIVE_SCENES_SOURCE_REVIEWED/POSITIVE_SCENES_SOURCE_REVIEWED/POSITIVE_INPUT_SOURCE_REVIEWED; UI runner UI_SOURCE_REVIEWED; fixture Program.NativeFixtureSourceReviewed; PositiveProtocol.SourceReviewed/InputSourceReviewed; OwnedTargetPreparation.SourceReviewed; NativeAcceptanceFactory.SourceReviewed. Startup/full-host/prototype gates remained false. Exact diff SHA256: `56819da18843af722247381b17733767e0273f110d953c6d51f34b82a7f9d0a8`. Exact trial diff retained privately as nr13-enabled-flags.diff; no enabled flags committed.

Five fresh native x64 artifacts were built sequentially in new unique output directories. Strict AOT/trim/analyzer/warnings-as-errors, effective-property/reference/native PE/SQLite/provenance gates passed without warning suppression; all warning/error scans passed. Target/guardian output directories were owner-ACL-private and clean→publish used empty NoWarn. No test suites or legacy publish were rerun.

| Artifact | Token | SHA256 |
|---|---|---|
| native | `0e1ff0822dfd48229c8c9b1c113f81e8` | `4b062dfc36a5f1541f6d8d8693a83c1eb211ff08b92508894cc53d286438cba7` |
| target | `681a3d53be974b86989875aaa50f9546` | `497f25c29356a088edee963b530aa5233b284968d05a815d99c775adb7d6bd59` |
| guardian | `17591d4702134586b5cb5d914fab8fe4` | `2146e6578a59a00d5bcba2b4246eadc73d2cd119e812a46702fe58fa585dc8e4` |
| management | `dd2319bd31564f8f91e0b0a8e57d0295` | `c007dfa90374c71e3df27b11796a4bed73e7215fd73c19daf6920bca3e058109` |
| candidate | `ca2e3ebb3173421ea3d757664c7c2771` | `588328f683c3ef488020ae12da96283a37d2bc3b89fc66041a4dc595a6ec1adb` |



Enabled native source SHA256: `90ee75d1dd314702b95c8674a64373160445291a210c32d202680cc89e06293a`; management source SHA256: `f4646af4dd140af33ac4d8416b5f108109c8f57808077379849a7545305464d9`. All five executable SHA256 values independently rechecked after release: PASS. Trial-enabled binaries remain retained evidence, not permission for a later launch.

Two consecutive dry admissions used the same bash → Python → pwsh -File chain. The copied nr-12 guard preserved self/live-ancestor/trusted-parent exclusions and fixed admission_match fields; DryRun returned before clip.exe:

```jsonl

{"attempt": 1, "exitCode": 0, "stdout": ["admission_dry_pass=1"], "stderrEmpty": true, "reason": "none"}
{"attempt": 2, "exitCode": 0, "stdout": ["admission_dry_pass=1"], "stderrEmpty": true, "reason": "none"}

```



Scratch Python ast.parse and all PowerShell parsing passed. Picker Python/PowerShell copies came from **current tracked sources**, retaining pk-02 telemetry and fg-01 normalization; changed only repository/helper path, local UI gate and explicit `throw 'actions'` immediately after `WaitPhase 'ui-import-cancelled'`. It precedes Import commit and is not an undefined Fail. The parent/evidence helpers were copied from nr-12 with nr13/current-worktree substitutions. The parent labels excluded Enter NOT RUN and excludes it from the six-scene aggregate. The read-only collector additionally retains every picker timeline JSONL row; no guard, scene or epoch is relaxed.

Executed-helper hashes (before closing the scratch UI flag):

```text

scratch_ps_syntax=PASS
{"name":"nr13-admit-window.py","sha256":"a5da4af8be62b518c8de40407afb490e6f76bc7bcf81345b7f31cd3e452eb3b1"}
{"name":"nr13-artifacts.py","sha256":"a4dfcca974aa92503e096d971452739ac37ebe4f1ce548ab665c88c075532f54"}
{"name":"nr13-build-phase0.ps1","sha256":"b399878cfe4c2f04d3c4b1ebca8d1c132f70064f63ccb0b312bfe7e789b71af3"}
{"name":"nr13-build-support.ps1","sha256":"b6b46fc196bf3ff64cc560d90e96338bbb4c19a96f0d996cb8937202386b250a"}
{"name":"nr13-clipboard-check.py","sha256":"a4b0cd7fde4a90aae59cc5a7382a5e48b009618e38a01aebd1ef7ed2c789a40a"}
{"name":"nr13-clipboard-release.py","sha256":"26e521f7190199f84d892b0389093d2c61de7d37781ce5ee5ff1d8fdb627df14"}
{"name":"nr13-dry-admission.py","sha256":"1acfee9a35e30442c142c8219a452a52c8da094ad6daaee21ec52bb936abfc02"}
{"name":"nr13-evidence-check.py","sha256":"4c0c95f4edf61c0bc70e5ccc6ddd76a34ac3508715c1252cfd03e7d44c632199"}
{"name":"nr13-parse-helpers.ps1","sha256":"ca0cd56cf01154f302d878ee09e7f7940e1b59e8fc5c5340ee3fd528710474c8"}
{"name":"nr13-picker-cancel-only.ps1","sha256":"cfa6817dbbb0a2b819d56bca9cd7eccdae8cc38365d4a26e51518b3ebb7a7cf2"}
{"name":"nr13-picker-cancel-only.py","sha256":"bf517ef1d57b60924e3a4a76678d801e237d95bd3b1d4b1eb0cf1eb237038e1b"}
{"name":"nr13-picker.py","sha256":"ffca341e98d1f0ae8059fdba51b32ff72f041faa0ab2dba26beac87678eb1b59"}
{"name":"nr13-preflight.py","sha256":"7c56c7514d3f499f18037bc89f0dc23cdc4dd969404bc010c2040a32e4349bc8"}
{"name":"nr13-process-check.ps1","sha256":"6daeaa3cd13fbef836d5c60c361f3104d958969faf00a7c9f0eb0d447e0ceb50"}
{"name":"nr13-run.py","sha256":"03392c011753409b91e8465021bf6993692de7a7ecbb752ef75b51cc86dd079c"}
{"name":"nr13-seed.ps1","sha256":"7326222af2819d2283bcc76dd4701b85d0d6a7d7f604ae5fc04bea9f1e8e4ce2"}
{"name":"nr13-wait-grant.py","sha256":"f59985b892c90d22439fa8adbff0f79523cccd90f5c66f9527eb166f66b7e545"}

```



## Commands actually executed

`<scratchpad>` is the private brief/report directory. Build wrapper commands run from repository root and internally select their pinned SDK working directories; project builds were never parallel.

```text

# Authorized redo: delete partial nr13-*.py / nr13-*.ps1; copy with explicit UTF-8.
# Enable nine flags and record exact diff; shell fail-fast (set -e).
python -I -B <scratchpad>/nr13-dry-admission.py
pwsh -NoProfile -File <scratchpad>/nr13-process-check.ps1
pwsh -NoProfile -File <scratchpad>/nr13-parse-helpers.ps1
# ast.parse every scratch Python helper.
pwsh -NoProfile -File <scratchpad>/nr13-build-phase0.ps1
# Sequential inside that build-only wrapper:
pwsh -NoProfile -File scripts/uno_candidate_native_fixture/build.ps1 -BuildOnly
pwsh -NoProfile -File <scratchpad>/nr13-build-support.ps1
pwsh -NoProfile -File scripts/verify-uno-candidate.ps1 -BuildOnly
pwsh -NoProfile -File scripts/publish-uno-candidate-aot.ps1
python -I -B <scratchpad>/nr13-artifacts.py
# Repeat syntax/hash collection, zero-process readiness check; PHASE0-READY.
python -I -B <scratchpad>/nr13-wait-grant.py
python -I -B <scratchpad>/nr13-admit-window.py
# set -e prevents parent launch if preflight fails:
python -I -B <scratchpad>/nr13-preflight.py
python -I -B <scratchpad>/nr13-run.py
# Trusted parent retains witnesses in RAM, calls guarded seed/native runner/picker.
python -I -B <scratchpad>/nr13-evidence-check.py ss-uno018-ui-ce6b99390918f47c7e6d2a678c37b67c
# Restore nine flags and scratch UI gate; grep false; tracked trial-source diff empty.
pwsh -NoProfile -File <scratchpad>/nr13-process-check.ps1
python -I -B <scratchpad>/nr13-clipboard-check.py
# SCREEN-DONE; filesystem-only artifact recheck/documentation after this.
python -I -B <scratchpad>/nr13-document.py

```



## Per-phase ledger

| Step | Result | Independent boundary |
|---|---|---|
| Initial phase 0 | FAIL | helper-copy encoding/orchestration; no build/scene/clipboard access |
| Phase-0 redo 1 | PASS | explicit leader authorization; five fresh strict native builds, hashes, syntax, two dry admissions |
| Phase 1 restore → abort → owner-refusal | PASS each | same artifacts, fresh witness, zero rows/usage; recovery settled |
| Phase 2 posted-row | PASS | usage PASS (1); epoch changed |
| Phase 2 posted-main | FAIL | usage FAIL (0); epoch unchanged |
| Phase 2 popup-copy | FAIL | usage PASS (0); epoch changed |
| Phase 2 editor-copy | FAIL | usage PASS (0); epoch unchanged |
| Phase 2 footer-copy | PASS | usage PASS (0); epoch changed |
| Phase 2 context-insert | PASS | usage PASS (1); epoch changed |
| posted-enter | NOT RUN | SendInput excluded (D50) |
| registered-* | NOT RUN | excluded by grant |
| Phase 3 Import Cancel | FAIL | input epoch_changed; clipboard epoch unchanged, rows/usage 0/0 |
| Other picker phases | NOT RUN | no other phase action attempted |
| Release | PASS with clipboard metadata reported | flags false, zero owned processes, no pending receipts; baseline matches, metadata retained |

Trusted native parent token: `ss-uno018-ui-ce6b99390918f47c7e6d2a678c37b67c`. Fresh owner-refusal witness was retained in that same parent and validated before independent positives. Baseline seeds used exactly the authorized fixed QA string via clip.exe only after zero-application-process audit/no pending recovery; never reseeded at release. No external timeout-kill wrapped the recovery-bearing native coordinator/guardian. Abort deliberately terminated only its reviewed owned worker/target Job after publication; independent guardian survived and settled. Failed scenes remain failures even when a consumer, usage or epoch subcheck passed.

## Foreground-root finding and remaining failures

**All six positives passed owned preparation and obtained `target_foreground_grant result=granted`.** All 94 high-rate trace rows classify the normalized foreground as owned_a: 87 raw samples have foreground_was_child=True and seven False. No trace row remains a_other_window/same_pid_other. Each startup after_focus sample is owned_a with foreground_was_child=False; earlier startup samples are foreign, with launcher_foreground=False. These are sampled, non-atomic observations, not continuous foreground proof.

| Scene | Trace rows | Raw child True / False | Final result |
|---|---|---|---|
| posted-row | 16 | 14 / 2 | PASS |
| posted-main | 16 | 15 / 1 | FAIL |
| popup-copy | 16 | 15 / 1 | FAIL |
| editor-copy | 16 | 15 / 1 | FAIL |
| footer-copy | 16 | 15 / 1 | PASS |
| context-insert | 14 | 13 / 1 | PASS |

The former guardian_ready/child-EDIT refusal is not reproduced. Posted-row and context-insert passed exact A/CRLF readback, empty B, one capture/paste, duplicate Busy/no-op and persisted usage once; footer-copy passed its intentional denied-insertion footer then one Copy commit with no paste/usage. These remain posted/VM callback proofs: **registered_posted_callback_not_physical**, never real shortcut/pointer/menu delivery.

Posted-main failed later: last-entered stage target_foreground_grant, grant already granted; native InvalidOperationException and independent foreground_foreign at sample 130, relation foreign, closed=false, foreground_was_child=false. Facts are class_token other / class_sha8 827664e8, visible/caption, not same A thread, not on bottom-left work. Do not identify the foreign application or infer a causal stealer from its fingerprint. Stage is last entered, not precise failure site.

Editor-copy likewise failed after granted foreground authority: sample 81, foreground_foreign, relation none, closed=false. No clipboard epoch change; usage zero alone is not Copy acceptance. Missing editor failure trigger/consumer completion must not be upgraded to PASS.

Popup-copy completed its native Copy receipt and graceful host shutdown, independent usage/epoch passed, but the independent monitor latched foreground_foreign at sample 154 with relation none and closed=true (exitCode 0). Overall **FAIL** remains; a zero-exit/settled consumer does not override the monitor. The recorded zero/closure sample is not proof of a foreign nonzero window, nor a reason to relax admission.

All three failed-scene ready inventories are complete. Their refusal inventories exist but report complete=false, windows=[]; no usable ready/refusal difference or newly created provider identity can be inferred. Raw class facts remain raw even where root identity is accepted. Exact root-only pins, no same-PID/owner exception, no focus/input retry or epoch rebaseline were retained.

## Exact per-scene records

Full fixed diagnostic lines and raw monitor/inventory JSON are copied below from this run, not from historical runs. Each native stream privacy CLEAN; planted negative controls PASS; stderr empty and pendingReceipt=false. Trace emission is buffered: elapsed_ms, not emission timestamp, gives local ordering; monitor and worker counters are independent. FailureStage none means no native failure-stage line, not no monitor failure. Abort lacks graceful worker shutdown by design.

### restore — PASS

Session: `ss-uno018-ui-eae0366be2d1411d2b8bf98639f343c6/ss-uno019-native-96d33895896a40ceb0ea2eb156fdc6ef`.

FailureStage: `none`; receipt/host check PASS; host shutdown-complete=true. Rows/usage=0/0; independent usage PASS.

Outer clipboard epoch: not separately recorded for control; combined independent positive boundary:

```json

null

```



Monitor: absent (no independent monitor failure recorded).

All startup/readiness/trace/native-stage and selected fixed boundary lines, verbatim:

```text

2026-11-02T01:47:32.811Z INFO native_startup_foreground phase=target_launch relation=foreign launcher_foreground=False sample_order=1 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=False cloaked=False cloaked_known=True class_sha8=827664e8 ws_popup=False ws_caption=True ws_child=False foreground_was_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=False title_nonempty=True
2026-11-02T01:47:32.811Z INFO native_startup_foreground phase=host_ready relation=foreign launcher_foreground=False sample_order=2 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=False cloaked=False cloaked_known=True class_sha8=827664e8 ws_popup=False ws_caption=True ws_child=False foreground_was_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=True
2026-11-02T01:47:32.812Z INFO native_host_readiness reason=ready
2026-11-02T01:47:33.037Z INFO native_control_admission reason=Ready
2026-11-02T01:47:33.144Z INFO native_control_publish status=swapped

```



Coordinator stdout:

```text

native_phase=guardian_ready
native_phase=published
native_result=regular_restored

```



target-thread-windows-ready.json, verbatim:

```json

{"phase":"ready","complete":true,"windows":[{"window_id":"243cc0be","is_a":false,"is_b":true,"new_at_refusal":false,"facts":{"class_token":"target018_class","visible":true,"toolwindow":false,"noactivate":false,"has_owner":false,"owner_is_a":false,"owner_is_b":false,"is_message_only":false,"same_thread_as_a":true,"cloaked":false,"cloaked_known":true,"class_sha8":"c139c83e","ws_popup":false,"ws_caption":true,"ws_child":false,"foreground_was_child":false,"ws_disabled":false,"ws_ex_topmost":false,"ws_ex_layered":false,"ws_ex_transparent":false,"ws_ex_appwindow":false,"zero_area":false,"offscreen":false,"equals_a_rect":false,"on_bottom_left_work":true,"title_nonempty":true,"sampled":true}},{"window_id":"705fdb3a","is_a":true,"is_b":false,"new_at_refusal":false,"facts":{"class_token":"target018_class","visible":true,"toolwindow":false,"noactivate":false,"has_owner":false,"owner_is_a":false,"owner_is_b":false,"is_message_only":false,"same_thread_as_a":true,"cloaked":false,"cloaked_known":true,"class_sha8":"c139c83e","ws_popup":false,"ws_caption":true,"ws_child":false,"foreground_was_child":false,"ws_disabled":false,"ws_ex_topmost":false,"ws_ex_layered":false,"ws_ex_transparent":false,"ws_ex_appwindow":false,"zero_area":false,"offscreen":false,"equals_a_rect":true,"on_bottom_left_work":true,"title_nonempty":true,"sampled":true}},{"window_id":"2453d017","is_a":false,"is_b":false,"new_at_refusal":false,"facts":{"class_token":"ime_default","visible":false,"toolwindow":false,"noactivate":false,"has_owner":true,"owner_is_a":true,"owner_is_b":false,"is_message_only":false,"same_thread_as_a":true,"cloaked":false,"cloaked_known":true,"class_sha8":"29ee788e","ws_popup":true,"ws_caption":false,"ws_child":false,"foreground_was_child":false,"ws_disabled":true,"ws_ex_topmost":false,"ws_ex_layered":false,"ws_ex_transparent":false,"ws_ex_appwindow":false,"zero_area":true,"offscreen":true,"equals_a_rect":false,"on_bottom_left_work":false,"title_nonempty":true,"sampled":true}}]}

```



### abort — PASS

Session: `ss-uno018-ui-efaaad848fb01f1f47ef27e01ed06f36/ss-uno019-native-c0fd83dca0584c88a7af941bbb0cae35`.

FailureStage: `none`; receipt/host check PASS; host shutdown-complete=false. Rows/usage=0/0; independent usage PASS.

Outer clipboard epoch: not separately recorded for control; combined independent positive boundary:

```json

null

```



Monitor: absent (no independent monitor failure recorded).

All startup/readiness/trace/native-stage and selected fixed boundary lines, verbatim:

```text

2026-11-02T01:47:39.019Z INFO native_startup_foreground phase=target_launch relation=foreign launcher_foreground=False sample_order=1 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=False cloaked=False cloaked_known=True class_sha8=827664e8 ws_popup=False ws_caption=True ws_child=False foreground_was_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=False title_nonempty=True
2026-11-02T01:47:39.019Z INFO native_startup_foreground phase=host_ready relation=foreign launcher_foreground=False sample_order=2 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=False cloaked=False cloaked_known=True class_sha8=827664e8 ws_popup=False ws_caption=True ws_child=False foreground_was_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=True
2026-11-02T01:47:39.019Z INFO native_host_readiness reason=ready
2026-11-02T01:47:39.326Z INFO native_control_admission reason=Ready
2026-11-02T01:47:39.443Z INFO native_control_publish status=swapped

```



Coordinator stdout:

```text

native_phase=guardian_ready
native_phase=published
native_result=abort_restored

```



target-thread-windows-ready.json, verbatim:

```json

{"phase":"ready","complete":true,"windows":[{"window_id":"38fc0247","is_a":false,"is_b":true,"new_at_refusal":false,"facts":{"class_token":"target018_class","visible":true,"toolwindow":false,"noactivate":false,"has_owner":false,"owner_is_a":false,"owner_is_b":false,"is_message_only":false,"same_thread_as_a":true,"cloaked":false,"cloaked_known":true,"class_sha8":"67396b9e","ws_popup":false,"ws_caption":true,"ws_child":false,"foreground_was_child":false,"ws_disabled":false,"ws_ex_topmost":false,"ws_ex_layered":false,"ws_ex_transparent":false,"ws_ex_appwindow":false,"zero_area":false,"offscreen":false,"equals_a_rect":false,"on_bottom_left_work":true,"title_nonempty":true,"sampled":true}},{"window_id":"22eae641","is_a":true,"is_b":false,"new_at_refusal":false,"facts":{"class_token":"target018_class","visible":true,"toolwindow":false,"noactivate":false,"has_owner":false,"owner_is_a":false,"owner_is_b":false,"is_message_only":false,"same_thread_as_a":true,"cloaked":false,"cloaked_known":true,"class_sha8":"67396b9e","ws_popup":false,"ws_caption":true,"ws_child":false,"foreground_was_child":false,"ws_disabled":false,"ws_ex_topmost":false,"ws_ex_layered":false,"ws_ex_transparent":false,"ws_ex_appwindow":false,"zero_area":false,"offscreen":false,"equals_a_rect":true,"on_bottom_left_work":true,"title_nonempty":true,"sampled":true}},{"window_id":"30663fff","is_a":false,"is_b":false,"new_at_refusal":false,"facts":{"class_token":"ime_default","visible":false,"toolwindow":false,"noactivate":false,"has_owner":true,"owner_is_a":true,"owner_is_b":false,"is_message_only":false,"same_thread_as_a":true,"cloaked":false,"cloaked_known":true,"class_sha8":"29ee788e","ws_popup":true,"ws_caption":false,"ws_child":false,"foreground_was_child":false,"ws_disabled":true,"ws_ex_topmost":false,"ws_ex_layered":false,"ws_ex_transparent":false,"ws_ex_appwindow":false,"zero_area":true,"offscreen":true,"equals_a_rect":false,"on_bottom_left_work":false,"title_nonempty":true,"sampled":true}}]}

```



### owner-refusal — PASS

Session: `ss-uno018-ui-aec1ee19bd5ba0f5c10e3d971059e0d2/ss-uno019-native-0c032bef94954b96b0d14c6976cdefbd`.

FailureStage: `none`; receipt/host check PASS; host shutdown-complete=true. Rows/usage=0/0; independent usage PASS.

Outer clipboard epoch: not separately recorded for control; combined independent positive boundary:

```json

null

```



Monitor: absent (no independent monitor failure recorded).

All startup/readiness/trace/native-stage and selected fixed boundary lines, verbatim:

```text

2026-11-02T01:47:45.174Z INFO native_startup_foreground phase=target_launch relation=foreign launcher_foreground=False sample_order=1 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=False cloaked=False cloaked_known=True class_sha8=827664e8 ws_popup=False ws_caption=True ws_child=False foreground_was_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=False title_nonempty=True
2026-11-02T01:47:45.176Z INFO native_startup_foreground phase=host_ready relation=foreign launcher_foreground=False sample_order=2 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=False cloaked=False cloaked_known=True class_sha8=827664e8 ws_popup=False ws_caption=True ws_child=False foreground_was_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=True
2026-11-02T01:47:45.176Z INFO native_host_readiness reason=ready
2026-11-02T01:47:45.350Z INFO native_control_admission reason=Ready
2026-11-02T01:47:45.457Z INFO native_control_publish status=swapped

```



Coordinator stdout:

```text

native_phase=guardian_ready
native_phase=published
native_phase=owner_refused
native_result=owner_refusal_restored

```



target-thread-windows-ready.json, verbatim:

```json

{"phase":"ready","complete":true,"windows":[{"window_id":"5cb80c59","is_a":false,"is_b":true,"new_at_refusal":false,"facts":{"class_token":"target018_class","visible":true,"toolwindow":false,"noactivate":false,"has_owner":false,"owner_is_a":false,"owner_is_b":false,"is_message_only":false,"same_thread_as_a":true,"cloaked":false,"cloaked_known":true,"class_sha8":"da46301c","ws_popup":false,"ws_caption":true,"ws_child":false,"foreground_was_child":false,"ws_disabled":false,"ws_ex_topmost":false,"ws_ex_layered":false,"ws_ex_transparent":false,"ws_ex_appwindow":false,"zero_area":false,"offscreen":false,"equals_a_rect":false,"on_bottom_left_work":true,"title_nonempty":true,"sampled":true}},{"window_id":"b4f7d2cf","is_a":true,"is_b":false,"new_at_refusal":false,"facts":{"class_token":"target018_class","visible":true,"toolwindow":false,"noactivate":false,"has_owner":false,"owner_is_a":false,"owner_is_b":false,"is_message_only":false,"same_thread_as_a":true,"cloaked":false,"cloaked_known":true,"class_sha8":"da46301c","ws_popup":false,"ws_caption":true,"ws_child":false,"foreground_was_child":false,"ws_disabled":false,"ws_ex_topmost":false,"ws_ex_layered":false,"ws_ex_transparent":false,"ws_ex_appwindow":false,"zero_area":false,"offscreen":false,"equals_a_rect":true,"on_bottom_left_work":true,"title_nonempty":true,"sampled":true}},{"window_id":"f721c8d6","is_a":false,"is_b":false,"new_at_refusal":false,"facts":{"class_token":"ime_default","visible":false,"toolwindow":false,"noactivate":false,"has_owner":true,"owner_is_a":true,"owner_is_b":false,"is_message_only":false,"same_thread_as_a":true,"cloaked":false,"cloaked_known":true,"class_sha8":"29ee788e","ws_popup":true,"ws_caption":false,"ws_child":false,"foreground_was_child":false,"ws_disabled":true,"ws_ex_topmost":false,"ws_ex_layered":false,"ws_ex_transparent":false,"ws_ex_appwindow":false,"zero_area":true,"offscreen":true,"equals_a_rect":false,"on_bottom_left_work":false,"title_nonempty":true,"sampled":true}}]}

```



### posted-row — PASS

Session: `ss-uno018-ui-a9a2d9d23a0ee3621ad920a88abd5534/ss-uno019-native-5097c258132a45d28f01f0d9598032eb`.

FailureStage: `none`; receipt/host check PASS; host shutdown-complete=true. Rows/usage=1/1; independent usage PASS.

Outer clipboard epoch: changed; combined independent positive boundary:

```json

{
  "usageVerified": true,
  "clipboardEpochChanged": true
}

```



Monitor: absent (no independent monitor failure recorded).

All startup/readiness/trace/native-stage and selected fixed boundary lines, verbatim:

```text

2026-11-02T01:47:55.643Z INFO native_startup_foreground phase=target_launch relation=foreign launcher_foreground=False sample_order=1 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=False cloaked=False cloaked_known=True class_sha8=827664e8 ws_popup=False ws_caption=True ws_child=False foreground_was_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=False title_nonempty=True
2026-11-02T01:47:55.643Z INFO native_startup_foreground phase=host_ready relation=foreign launcher_foreground=False sample_order=2 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=False cloaked=False cloaked_known=True class_sha8=827664e8 ws_popup=False ws_caption=True ws_child=False foreground_was_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=True
2026-11-02T01:47:55.644Z INFO native_host_readiness reason=ready
2026-11-02T01:47:55.868Z INFO owned_initial_focus phase=waiting
2026-11-02T01:47:55.869Z INFO native_startup_foreground phase=preparation_start relation=foreign launcher_foreground=False sample_order=3 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=False cloaked=False cloaked_known=True class_sha8=827664e8 ws_popup=False ws_caption=True ws_child=False foreground_was_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=True
2026-11-02T01:47:56.739Z INFO owned_preparation_admission reason=ready
2026-11-02T01:47:56.875Z INFO native_startup_foreground phase=before_focus relation=foreign launcher_foreground=False sample_order=4 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=False cloaked=False cloaked_known=True class_sha8=827664e8 ws_popup=False ws_caption=True ws_child=False foreground_was_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=True
2026-11-02T01:47:56.924Z INFO native_startup_foreground phase=after_focus relation=owned_a launcher_foreground=False sample_order=5 class_token=target018_class visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=a2d303f4 ws_popup=False ws_caption=True ws_child=False foreground_was_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=True on_bottom_left_work=True title_nonempty=True
2026-11-02T01:47:57.188Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=1 elapsed_ms=0 class_token=target018_class visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=a2d303f4 ws_popup=False ws_caption=True ws_child=False foreground_was_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=True on_bottom_left_work=True title_nonempty=True
2026-11-02T01:47:57.188Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=2 elapsed_ms=19 class_token=target018_class visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=a2d303f4 ws_popup=False ws_caption=True ws_child=False foreground_was_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=True on_bottom_left_work=True title_nonempty=True
2026-11-02T01:47:57.188Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=3 elapsed_ms=34 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:47:57.188Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=4 elapsed_ms=65 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:47:57.189Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=5 elapsed_ms=80 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:47:57.189Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=6 elapsed_ms=95 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:47:57.189Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=7 elapsed_ms=110 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:47:57.189Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=8 elapsed_ms=126 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:47:57.189Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=9 elapsed_ms=141 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:47:57.189Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=10 elapsed_ms=156 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:47:57.189Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=11 elapsed_ms=171 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:47:57.190Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=12 elapsed_ms=187 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:47:57.190Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=13 elapsed_ms=203 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:47:57.190Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=14 elapsed_ms=218 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:47:57.190Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=15 elapsed_ms=233 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:47:57.190Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=16 elapsed_ms=248 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:47:58.884Z INFO owned_target_preparation reason=Prepared
2026-11-02T01:47:58.910Z INFO owned_initial_focus phase=ready
2026-11-02T01:47:58.959Z INFO target_foreground_grant result=granted
2026-11-02T01:47:59.939Z INFO native_popup_focus reason=foreground_accepted
2026-11-02T01:47:59.940Z INFO candidate_popup_focus reason=accepted
2026-11-02T01:48:00.811Z INFO clipboard_swap attempts=1 status=Swapped saved=4 skipped_by_design=0 unsaved=0 cap_exceeded=False bytes=104 ms=234
2026-11-02T01:48:01.482Z INFO clipboard_restore status=Restored attempts=1
2026-11-02T01:48:01.482Z INFO insert_result outcome=Inserted id=1 notice=False ms=1480

```



Coordinator stdout:

```text

native_phase=guardian_ready
native_phase=awaiting_owned_a
native_result=posted-row_settled

```



target-thread-windows-ready.json, verbatim:

```json

{"phase":"ready","complete":true,"windows":[{"window_id":"77f7c258","is_a":false,"is_b":true,"new_at_refusal":false,"facts":{"class_token":"target018_class","visible":true,"toolwindow":false,"noactivate":false,"has_owner":false,"owner_is_a":false,"owner_is_b":false,"is_message_only":false,"same_thread_as_a":true,"cloaked":false,"cloaked_known":true,"class_sha8":"a2d303f4","ws_popup":false,"ws_caption":true,"ws_child":false,"foreground_was_child":false,"ws_disabled":false,"ws_ex_topmost":false,"ws_ex_layered":false,"ws_ex_transparent":false,"ws_ex_appwindow":false,"zero_area":false,"offscreen":false,"equals_a_rect":false,"on_bottom_left_work":true,"title_nonempty":true,"sampled":true}},{"window_id":"1331e1da","is_a":true,"is_b":false,"new_at_refusal":false,"facts":{"class_token":"target018_class","visible":true,"toolwindow":false,"noactivate":false,"has_owner":false,"owner_is_a":false,"owner_is_b":false,"is_message_only":false,"same_thread_as_a":true,"cloaked":false,"cloaked_known":true,"class_sha8":"a2d303f4","ws_popup":false,"ws_caption":true,"ws_child":false,"foreground_was_child":false,"ws_disabled":false,"ws_ex_topmost":false,"ws_ex_layered":false,"ws_ex_transparent":false,"ws_ex_appwindow":false,"zero_area":false,"offscreen":false,"equals_a_rect":true,"on_bottom_left_work":true,"title_nonempty":true,"sampled":true}},{"window_id":"dcfd350b","is_a":false,"is_b":false,"new_at_refusal":false,"facts":{"class_token":"ime_default","visible":false,"toolwindow":false,"noactivate":false,"has_owner":true,"owner_is_a":true,"owner_is_b":false,"is_message_only":false,"same_thread_as_a":true,"cloaked":false,"cloaked_known":true,"class_sha8":"29ee788e","ws_popup":true,"ws_caption":false,"ws_child":false,"foreground_was_child":false,"ws_disabled":true,"ws_ex_topmost":false,"ws_ex_layered":false,"ws_ex_transparent":false,"ws_ex_appwindow":false,"zero_area":true,"offscreen":true,"equals_a_rect":false,"on_bottom_left_work":false,"title_nonempty":true,"sampled":true}}]}

```



### posted-main — FAIL

Session: `ss-uno018-ui-3799510bb59f1727bf999d1053e51b0a/ss-uno019-native-1f62104eff06446395e41a972f03540e`.

FailureStage: `target_foreground_grant`; receipt/host check FAIL_ValueError; host shutdown-complete=false. Rows/usage=1/0; independent usage FAIL.

Outer clipboard epoch: unchanged; combined independent positive boundary:

```json

{
  "result": "FAIL",
  "errorType": "ValueError"
}

```



Monitor: full native-monitor-failure.json below.

```json

{"predicate": "foreground_foreign", "exitCode": 2, "sampleOrder": 130, "relation": "foreign", "closed": false, "foreground_was_child": false, "windowFacts": {"class_token": "other", "sampled": true, "visible": true, "toolwindow": false, "noactivate": false, "has_owner": false, "owner_is_a": false, "owner_is_b": false, "is_message_only": false, "same_thread_as_a": false, "cloaked": false, "cloaked_known": true, "class_sha8": "827664e8", "ws_popup": false, "ws_caption": true, "ws_child": false, "foreground_was_child": false, "ws_disabled": false, "ws_ex_topmost": false, "ws_ex_layered": false, "ws_ex_transparent": false, "ws_ex_appwindow": false, "zero_area": false, "offscreen": false, "equals_a_rect": false, "on_bottom_left_work": false, "title_nonempty": true}}

```



All startup/readiness/trace/native-stage and selected fixed boundary lines, verbatim:

```text

2026-11-02T01:48:08.643Z INFO native_startup_foreground phase=target_launch relation=foreign launcher_foreground=False sample_order=1 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=False cloaked=False cloaked_known=True class_sha8=827664e8 ws_popup=False ws_caption=True ws_child=False foreground_was_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=False title_nonempty=True
2026-11-02T01:48:08.644Z INFO native_startup_foreground phase=host_ready relation=foreign launcher_foreground=False sample_order=2 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=False cloaked=False cloaked_known=True class_sha8=827664e8 ws_popup=False ws_caption=True ws_child=False foreground_was_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=True
2026-11-02T01:48:08.644Z INFO native_host_readiness reason=ready
2026-11-02T01:48:08.839Z INFO owned_initial_focus phase=waiting
2026-11-02T01:48:08.839Z INFO native_startup_foreground phase=preparation_start relation=foreign launcher_foreground=False sample_order=3 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=False cloaked=False cloaked_known=True class_sha8=827664e8 ws_popup=False ws_caption=True ws_child=False foreground_was_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=True
2026-11-02T01:48:08.879Z INFO owned_preparation_admission reason=ready
2026-11-02T01:48:09.016Z INFO native_startup_foreground phase=before_focus relation=foreign launcher_foreground=False sample_order=4 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=False cloaked=False cloaked_known=True class_sha8=827664e8 ws_popup=False ws_caption=True ws_child=False foreground_was_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=True
2026-11-02T01:48:09.053Z INFO native_startup_foreground phase=after_focus relation=owned_a launcher_foreground=False sample_order=5 class_token=target018_class visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=7a5f82ff ws_popup=False ws_caption=True ws_child=False foreground_was_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=True on_bottom_left_work=True title_nonempty=True
2026-11-02T01:48:09.317Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=1 elapsed_ms=0 class_token=target018_class visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=7a5f82ff ws_popup=False ws_caption=True ws_child=False foreground_was_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=True on_bottom_left_work=True title_nonempty=True
2026-11-02T01:48:09.318Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=2 elapsed_ms=19 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:48:09.318Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=3 elapsed_ms=34 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:48:09.318Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=4 elapsed_ms=49 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:48:09.318Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=5 elapsed_ms=64 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:48:09.318Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=6 elapsed_ms=79 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:48:09.318Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=7 elapsed_ms=95 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:48:09.319Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=8 elapsed_ms=111 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:48:09.319Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=9 elapsed_ms=141 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:48:09.319Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=10 elapsed_ms=156 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:48:09.319Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=11 elapsed_ms=172 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:48:09.319Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=12 elapsed_ms=187 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:48:09.319Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=13 elapsed_ms=203 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:48:09.319Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=14 elapsed_ms=218 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:48:09.320Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=15 elapsed_ms=234 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:48:09.320Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=16 elapsed_ms=249 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:48:11.056Z INFO owned_target_preparation reason=Prepared
2026-11-02T01:48:11.075Z INFO owned_initial_focus phase=ready
2026-11-02T01:48:11.122Z INFO target_foreground_grant result=granted
2026-11-02T01:48:14.338Z ERROR native_positive_failed stage=target_foreground_grant InvalidOperationException (HResult=0x80131509)

```



Coordinator stdout:

```text

native_phase=guardian_ready
native_phase=awaiting_owned_a
native_result=failed

```



target-thread-windows-ready.json, verbatim:

```json

{"phase":"ready","complete":true,"windows":[{"window_id":"3b57e07e","is_a":false,"is_b":true,"new_at_refusal":false,"facts":{"class_token":"target018_class","visible":true,"toolwindow":false,"noactivate":false,"has_owner":false,"owner_is_a":false,"owner_is_b":false,"is_message_only":false,"same_thread_as_a":true,"cloaked":false,"cloaked_known":true,"class_sha8":"7a5f82ff","ws_popup":false,"ws_caption":true,"ws_child":false,"foreground_was_child":false,"ws_disabled":false,"ws_ex_topmost":false,"ws_ex_layered":false,"ws_ex_transparent":false,"ws_ex_appwindow":false,"zero_area":false,"offscreen":false,"equals_a_rect":false,"on_bottom_left_work":true,"title_nonempty":true,"sampled":true}},{"window_id":"bf98a86f","is_a":true,"is_b":false,"new_at_refusal":false,"facts":{"class_token":"target018_class","visible":true,"toolwindow":false,"noactivate":false,"has_owner":false,"owner_is_a":false,"owner_is_b":false,"is_message_only":false,"same_thread_as_a":true,"cloaked":false,"cloaked_known":true,"class_sha8":"7a5f82ff","ws_popup":false,"ws_caption":true,"ws_child":false,"foreground_was_child":false,"ws_disabled":false,"ws_ex_topmost":false,"ws_ex_layered":false,"ws_ex_transparent":false,"ws_ex_appwindow":false,"zero_area":false,"offscreen":false,"equals_a_rect":true,"on_bottom_left_work":true,"title_nonempty":true,"sampled":true}},{"window_id":"ccda92b3","is_a":false,"is_b":false,"new_at_refusal":false,"facts":{"class_token":"ime_default","visible":false,"toolwindow":false,"noactivate":false,"has_owner":true,"owner_is_a":true,"owner_is_b":false,"is_message_only":false,"same_thread_as_a":true,"cloaked":false,"cloaked_known":true,"class_sha8":"29ee788e","ws_popup":true,"ws_caption":false,"ws_child":false,"foreground_was_child":false,"ws_disabled":true,"ws_ex_topmost":false,"ws_ex_layered":false,"ws_ex_transparent":false,"ws_ex_appwindow":false,"zero_area":true,"offscreen":true,"equals_a_rect":false,"on_bottom_left_work":false,"title_nonempty":true,"sampled":true}}]}

```



target-thread-windows-refusal.json, verbatim:

```json

{"phase": "refusal", "complete": false, "windows": []}

```



### posted-enter — NOT RUN

NOT RUN: input synthesis excluded. No session, monitor, usage or clipboard observation.

### popup-copy — FAIL

Session: `ss-uno018-ui-a9ab4a8056de100535a4f2b176c71b95/ss-uno019-native-c6e2f68f66094b5a9c3afd9b2f99daf1`.

FailureStage: `none`; receipt/host check PASS; host shutdown-complete=true. Rows/usage=1/0; independent usage PASS.

Outer clipboard epoch: changed; combined independent positive boundary:

```json

{
  "usageVerified": true,
  "clipboardEpochChanged": true
}

```



Monitor: full native-monitor-failure.json below.

```json

{"predicate": "foreground_foreign", "exitCode": 0, "sampleOrder": 154, "relation": "none", "closed": true, "foreground_was_child": false, "windowFacts": {"class_token": "other", "class_sha8": "e3b0c442", "sampled": false, "visible": false, "toolwindow": false, "noactivate": false, "has_owner": false, "owner_is_a": false, "owner_is_b": false, "is_message_only": false, "same_thread_as_a": false, "cloaked": false, "cloaked_known": false, "foreground_was_child": false, "ws_popup": false, "ws_caption": false, "ws_child": false, "ws_disabled": false, "ws_ex_topmost": false, "ws_ex_layered": false, "ws_ex_transparent": false, "ws_ex_appwindow": false, "zero_area": false, "offscreen": false, "equals_a_rect": false, "on_bottom_left_work": false, "title_nonempty": false}}

```



All startup/readiness/trace/native-stage and selected fixed boundary lines, verbatim:

```text

2026-11-02T01:48:20.555Z INFO native_startup_foreground phase=target_launch relation=foreign launcher_foreground=False sample_order=1 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=False cloaked=False cloaked_known=True class_sha8=827664e8 ws_popup=False ws_caption=True ws_child=False foreground_was_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=False title_nonempty=True
2026-11-02T01:48:20.555Z INFO native_startup_foreground phase=host_ready relation=foreign launcher_foreground=False sample_order=2 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=False cloaked=False cloaked_known=True class_sha8=827664e8 ws_popup=False ws_caption=True ws_child=False foreground_was_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=True
2026-11-02T01:48:20.555Z INFO native_host_readiness reason=ready
2026-11-02T01:48:20.954Z INFO owned_initial_focus phase=waiting
2026-11-02T01:48:20.954Z INFO native_startup_foreground phase=preparation_start relation=foreign launcher_foreground=False sample_order=3 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=False cloaked=False cloaked_known=True class_sha8=827664e8 ws_popup=False ws_caption=True ws_child=False foreground_was_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=True
2026-11-02T01:48:21.079Z INFO owned_preparation_admission reason=ready
2026-11-02T01:48:21.217Z INFO native_startup_foreground phase=before_focus relation=foreign launcher_foreground=False sample_order=4 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=False cloaked=False cloaked_known=True class_sha8=827664e8 ws_popup=False ws_caption=True ws_child=False foreground_was_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=True
2026-11-02T01:48:23.260Z INFO native_startup_foreground phase=after_focus relation=owned_a launcher_foreground=False sample_order=5 class_token=target018_class visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=266b821d ws_popup=False ws_caption=True ws_child=False foreground_was_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=True on_bottom_left_work=True title_nonempty=True
2026-11-02T01:48:23.516Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=1 elapsed_ms=0 class_token=target018_class visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=266b821d ws_popup=False ws_caption=True ws_child=False foreground_was_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=True on_bottom_left_work=True title_nonempty=True
2026-11-02T01:48:23.516Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=2 elapsed_ms=9 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:48:23.516Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=3 elapsed_ms=24 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:48:23.516Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=4 elapsed_ms=55 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:48:23.517Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=5 elapsed_ms=70 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:48:23.517Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=6 elapsed_ms=86 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:48:23.517Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=7 elapsed_ms=101 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:48:23.517Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=8 elapsed_ms=117 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:48:23.518Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=9 elapsed_ms=131 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:48:23.518Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=10 elapsed_ms=146 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:48:23.518Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=11 elapsed_ms=162 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:48:23.518Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=12 elapsed_ms=177 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:48:23.518Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=13 elapsed_ms=193 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:48:23.518Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=14 elapsed_ms=209 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:48:23.519Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=15 elapsed_ms=224 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:48:23.519Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=16 elapsed_ms=239 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:48:25.269Z INFO owned_target_preparation reason=Prepared
2026-11-02T01:48:25.305Z INFO owned_initial_focus phase=ready
2026-11-02T01:48:25.377Z INFO target_foreground_grant result=granted
2026-11-02T01:48:26.558Z INFO native_popup_focus reason=foreground_accepted
2026-11-02T01:48:26.558Z INFO candidate_popup_focus reason=accepted

```



Coordinator stdout:

```text

native_phase=guardian_ready
native_phase=awaiting_owned_a
native_result=popup-copy_settled

```



target-thread-windows-ready.json, verbatim:

```json

{"phase":"ready","complete":true,"windows":[{"window_id":"7b642cde","is_a":false,"is_b":true,"new_at_refusal":false,"facts":{"class_token":"target018_class","visible":true,"toolwindow":false,"noactivate":false,"has_owner":false,"owner_is_a":false,"owner_is_b":false,"is_message_only":false,"same_thread_as_a":true,"cloaked":false,"cloaked_known":true,"class_sha8":"266b821d","ws_popup":false,"ws_caption":true,"ws_child":false,"foreground_was_child":false,"ws_disabled":false,"ws_ex_topmost":false,"ws_ex_layered":false,"ws_ex_transparent":false,"ws_ex_appwindow":false,"zero_area":false,"offscreen":false,"equals_a_rect":false,"on_bottom_left_work":true,"title_nonempty":true,"sampled":true}},{"window_id":"9b05c0e2","is_a":true,"is_b":false,"new_at_refusal":false,"facts":{"class_token":"target018_class","visible":true,"toolwindow":false,"noactivate":false,"has_owner":false,"owner_is_a":false,"owner_is_b":false,"is_message_only":false,"same_thread_as_a":true,"cloaked":false,"cloaked_known":true,"class_sha8":"266b821d","ws_popup":false,"ws_caption":true,"ws_child":false,"foreground_was_child":false,"ws_disabled":false,"ws_ex_topmost":false,"ws_ex_layered":false,"ws_ex_transparent":false,"ws_ex_appwindow":false,"zero_area":false,"offscreen":false,"equals_a_rect":true,"on_bottom_left_work":true,"title_nonempty":true,"sampled":true}},{"window_id":"0f77894c","is_a":false,"is_b":false,"new_at_refusal":false,"facts":{"class_token":"ime_default","visible":false,"toolwindow":false,"noactivate":false,"has_owner":true,"owner_is_a":true,"owner_is_b":false,"is_message_only":false,"same_thread_as_a":true,"cloaked":false,"cloaked_known":true,"class_sha8":"29ee788e","ws_popup":true,"ws_caption":false,"ws_child":false,"foreground_was_child":false,"ws_disabled":true,"ws_ex_topmost":false,"ws_ex_layered":false,"ws_ex_transparent":false,"ws_ex_appwindow":false,"zero_area":true,"offscreen":true,"equals_a_rect":false,"on_bottom_left_work":false,"title_nonempty":true,"sampled":true}}]}

```



target-thread-windows-refusal.json, verbatim:

```json

{"phase": "refusal", "complete": false, "windows": []}

```



### editor-copy — FAIL

Session: `ss-uno018-ui-0380ca1901434dd5557bf27dcfe23605/ss-uno019-native-556cacb25660413081c1bfb3d54e35ae`.

FailureStage: `target_foreground_grant`; receipt/host check FAIL_ValueError; host shutdown-complete=false. Rows/usage=1/0; independent usage PASS.

Outer clipboard epoch: unchanged; combined independent positive boundary:

```json

{
  "result": "FAIL",
  "errorType": "ValueError"
}

```



Monitor: full native-monitor-failure.json below.

```json

{"predicate": "foreground_foreign", "exitCode": 2, "sampleOrder": 81, "relation": "none", "closed": false, "foreground_was_child": false, "windowFacts": {"class_token": "other", "class_sha8": "e3b0c442", "sampled": false, "visible": false, "toolwindow": false, "noactivate": false, "has_owner": false, "owner_is_a": false, "owner_is_b": false, "is_message_only": false, "same_thread_as_a": false, "cloaked": false, "cloaked_known": false, "foreground_was_child": false, "ws_popup": false, "ws_caption": false, "ws_child": false, "ws_disabled": false, "ws_ex_topmost": false, "ws_ex_layered": false, "ws_ex_transparent": false, "ws_ex_appwindow": false, "zero_area": false, "offscreen": false, "equals_a_rect": false, "on_bottom_left_work": false, "title_nonempty": false}}

```



All startup/readiness/trace/native-stage and selected fixed boundary lines, verbatim:

```text

2026-11-02T01:48:34.229Z INFO native_startup_foreground phase=target_launch relation=foreign launcher_foreground=False sample_order=1 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=False cloaked=False cloaked_known=True class_sha8=827664e8 ws_popup=False ws_caption=True ws_child=False foreground_was_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=False title_nonempty=True
2026-11-02T01:48:34.229Z INFO native_startup_foreground phase=host_ready relation=foreign launcher_foreground=False sample_order=2 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=False cloaked=False cloaked_known=True class_sha8=827664e8 ws_popup=False ws_caption=True ws_child=False foreground_was_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=True
2026-11-02T01:48:34.230Z INFO native_host_readiness reason=ready
2026-11-02T01:48:34.676Z INFO owned_initial_focus phase=waiting
2026-11-02T01:48:34.677Z INFO native_startup_foreground phase=preparation_start relation=foreign launcher_foreground=False sample_order=3 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=False cloaked=False cloaked_known=True class_sha8=827664e8 ws_popup=False ws_caption=True ws_child=False foreground_was_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=True
2026-11-02T01:48:34.716Z INFO owned_preparation_admission reason=ready
2026-11-02T01:48:34.859Z INFO native_startup_foreground phase=before_focus relation=foreign launcher_foreground=False sample_order=4 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=False cloaked=False cloaked_known=True class_sha8=827664e8 ws_popup=False ws_caption=True ws_child=False foreground_was_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=True
2026-11-02T01:48:34.900Z INFO native_startup_foreground phase=after_focus relation=owned_a launcher_foreground=False sample_order=5 class_token=target018_class visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=a742631d ws_popup=False ws_caption=True ws_child=False foreground_was_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=True on_bottom_left_work=True title_nonempty=True
2026-11-02T01:48:35.164Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=1 elapsed_ms=0 class_token=target018_class visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=a742631d ws_popup=False ws_caption=True ws_child=False foreground_was_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=True on_bottom_left_work=True title_nonempty=True
2026-11-02T01:48:35.165Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=2 elapsed_ms=18 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:48:35.165Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=3 elapsed_ms=34 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:48:35.165Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=4 elapsed_ms=49 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:48:35.165Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=5 elapsed_ms=79 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:48:35.165Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=6 elapsed_ms=94 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:48:35.165Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=7 elapsed_ms=110 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:48:35.166Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=8 elapsed_ms=125 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:48:35.166Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=9 elapsed_ms=141 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:48:35.166Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=10 elapsed_ms=156 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:48:35.166Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=11 elapsed_ms=171 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:48:35.166Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=12 elapsed_ms=187 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:48:35.166Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=13 elapsed_ms=202 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:48:35.166Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=14 elapsed_ms=218 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:48:35.166Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=15 elapsed_ms=234 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:48:35.167Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=16 elapsed_ms=248 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:48:36.897Z INFO owned_target_preparation reason=Prepared
2026-11-02T01:48:36.918Z INFO owned_initial_focus phase=ready
2026-11-02T01:48:36.954Z INFO target_foreground_grant result=granted
2026-11-02T01:48:37.337Z ERROR native_positive_failed stage=target_foreground_grant InvalidOperationException (HResult=0x80131509)

```



Coordinator stdout:

```text

native_phase=guardian_ready
native_phase=awaiting_owned_a
native_result=failed

```



target-thread-windows-ready.json, verbatim:

```json

{"phase":"ready","complete":true,"windows":[{"window_id":"b15ffa66","is_a":false,"is_b":true,"new_at_refusal":false,"facts":{"class_token":"target018_class","visible":true,"toolwindow":false,"noactivate":false,"has_owner":false,"owner_is_a":false,"owner_is_b":false,"is_message_only":false,"same_thread_as_a":true,"cloaked":false,"cloaked_known":true,"class_sha8":"a742631d","ws_popup":false,"ws_caption":true,"ws_child":false,"foreground_was_child":false,"ws_disabled":false,"ws_ex_topmost":false,"ws_ex_layered":false,"ws_ex_transparent":false,"ws_ex_appwindow":false,"zero_area":false,"offscreen":false,"equals_a_rect":false,"on_bottom_left_work":true,"title_nonempty":true,"sampled":true}},{"window_id":"35248a64","is_a":true,"is_b":false,"new_at_refusal":false,"facts":{"class_token":"target018_class","visible":true,"toolwindow":false,"noactivate":false,"has_owner":false,"owner_is_a":false,"owner_is_b":false,"is_message_only":false,"same_thread_as_a":true,"cloaked":false,"cloaked_known":true,"class_sha8":"a742631d","ws_popup":false,"ws_caption":true,"ws_child":false,"foreground_was_child":false,"ws_disabled":false,"ws_ex_topmost":false,"ws_ex_layered":false,"ws_ex_transparent":false,"ws_ex_appwindow":false,"zero_area":false,"offscreen":false,"equals_a_rect":true,"on_bottom_left_work":true,"title_nonempty":true,"sampled":true}},{"window_id":"944fde65","is_a":false,"is_b":false,"new_at_refusal":false,"facts":{"class_token":"ime_default","visible":false,"toolwindow":false,"noactivate":false,"has_owner":true,"owner_is_a":true,"owner_is_b":false,"is_message_only":false,"same_thread_as_a":true,"cloaked":false,"cloaked_known":true,"class_sha8":"29ee788e","ws_popup":true,"ws_caption":false,"ws_child":false,"foreground_was_child":false,"ws_disabled":true,"ws_ex_topmost":false,"ws_ex_layered":false,"ws_ex_transparent":false,"ws_ex_appwindow":false,"zero_area":true,"offscreen":true,"equals_a_rect":false,"on_bottom_left_work":false,"title_nonempty":true,"sampled":true}}]}

```



target-thread-windows-refusal.json, verbatim:

```json

{"phase": "refusal", "complete": false, "windows": []}

```



### footer-copy — PASS

Session: `ss-uno018-ui-e2aeb1ab69952e35ea71494245bc72e4/ss-uno019-native-6de77c89b2ca4f909bf1301db7a16acc`.

FailureStage: `none`; receipt/host check PASS; host shutdown-complete=true. Rows/usage=1/0; independent usage PASS.

Outer clipboard epoch: changed; combined independent positive boundary:

```json

{
  "usageVerified": true,
  "clipboardEpochChanged": true
}

```



Monitor: absent (no independent monitor failure recorded).

All startup/readiness/trace/native-stage and selected fixed boundary lines, verbatim:

```text

2026-11-02T01:48:44.084Z INFO native_startup_foreground phase=target_launch relation=foreign launcher_foreground=False sample_order=1 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=False cloaked=False cloaked_known=True class_sha8=827664e8 ws_popup=False ws_caption=True ws_child=False foreground_was_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=False title_nonempty=True
2026-11-02T01:48:44.085Z INFO native_startup_foreground phase=host_ready relation=foreign launcher_foreground=False sample_order=2 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=False cloaked=False cloaked_known=True class_sha8=827664e8 ws_popup=False ws_caption=True ws_child=False foreground_was_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=True
2026-11-02T01:48:44.085Z INFO native_host_readiness reason=ready
2026-11-02T01:48:44.487Z INFO owned_initial_focus phase=waiting
2026-11-02T01:48:44.499Z INFO native_startup_foreground phase=preparation_start relation=foreign launcher_foreground=False sample_order=3 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=False cloaked=False cloaked_known=True class_sha8=827664e8 ws_popup=False ws_caption=True ws_child=False foreground_was_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=True
2026-11-02T01:48:47.048Z INFO owned_preparation_admission reason=ready
2026-11-02T01:48:47.245Z INFO native_startup_foreground phase=before_focus relation=foreign launcher_foreground=False sample_order=4 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=False cloaked=False cloaked_known=True class_sha8=827664e8 ws_popup=False ws_caption=True ws_child=False foreground_was_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=True
2026-11-02T01:48:49.299Z INFO native_startup_foreground phase=after_focus relation=owned_a launcher_foreground=False sample_order=5 class_token=target018_class visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=2dc2c691 ws_popup=False ws_caption=True ws_child=False foreground_was_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=True on_bottom_left_work=True title_nonempty=True
2026-11-02T01:48:49.556Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=1 elapsed_ms=0 class_token=target018_class visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=2dc2c691 ws_popup=False ws_caption=True ws_child=False foreground_was_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=True on_bottom_left_work=True title_nonempty=True
2026-11-02T01:48:49.556Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=2 elapsed_ms=5 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:48:49.557Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=3 elapsed_ms=21 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:48:49.557Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=4 elapsed_ms=52 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:48:49.557Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=5 elapsed_ms=67 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:48:49.557Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=6 elapsed_ms=82 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:48:49.558Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=7 elapsed_ms=99 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:48:49.558Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=8 elapsed_ms=115 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:48:49.558Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=9 elapsed_ms=130 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:48:49.558Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=10 elapsed_ms=145 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:48:49.559Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=11 elapsed_ms=160 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:48:49.559Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=12 elapsed_ms=175 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:48:49.559Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=13 elapsed_ms=206 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:48:49.559Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=14 elapsed_ms=221 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:48:49.560Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=15 elapsed_ms=235 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:48:49.560Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=16 elapsed_ms=250 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:48:51.361Z INFO owned_target_preparation reason=Prepared
2026-11-02T01:48:51.395Z INFO owned_initial_focus phase=ready
2026-11-02T01:48:51.457Z INFO target_foreground_grant result=granted
2026-11-02T01:48:52.638Z INFO native_popup_focus reason=foreground_accepted
2026-11-02T01:48:52.638Z INFO candidate_popup_focus reason=accepted

```



Coordinator stdout:

```text

native_phase=guardian_ready
native_phase=awaiting_owned_a
native_result=footer-copy_settled

```



target-thread-windows-ready.json, verbatim:

```json

{"phase":"ready","complete":true,"windows":[{"window_id":"57eb0d33","is_a":false,"is_b":true,"new_at_refusal":false,"facts":{"class_token":"target018_class","visible":true,"toolwindow":false,"noactivate":false,"has_owner":false,"owner_is_a":false,"owner_is_b":false,"is_message_only":false,"same_thread_as_a":true,"cloaked":false,"cloaked_known":true,"class_sha8":"2dc2c691","ws_popup":false,"ws_caption":true,"ws_child":false,"foreground_was_child":false,"ws_disabled":false,"ws_ex_topmost":false,"ws_ex_layered":false,"ws_ex_transparent":false,"ws_ex_appwindow":false,"zero_area":false,"offscreen":false,"equals_a_rect":false,"on_bottom_left_work":true,"title_nonempty":true,"sampled":true}},{"window_id":"d605a313","is_a":true,"is_b":false,"new_at_refusal":false,"facts":{"class_token":"target018_class","visible":true,"toolwindow":false,"noactivate":false,"has_owner":false,"owner_is_a":false,"owner_is_b":false,"is_message_only":false,"same_thread_as_a":true,"cloaked":false,"cloaked_known":true,"class_sha8":"2dc2c691","ws_popup":false,"ws_caption":true,"ws_child":false,"foreground_was_child":false,"ws_disabled":false,"ws_ex_topmost":false,"ws_ex_layered":false,"ws_ex_transparent":false,"ws_ex_appwindow":false,"zero_area":false,"offscreen":false,"equals_a_rect":true,"on_bottom_left_work":true,"title_nonempty":true,"sampled":true}},{"window_id":"dcdbce06","is_a":false,"is_b":false,"new_at_refusal":false,"facts":{"class_token":"ime_default","visible":false,"toolwindow":false,"noactivate":false,"has_owner":true,"owner_is_a":true,"owner_is_b":false,"is_message_only":false,"same_thread_as_a":true,"cloaked":false,"cloaked_known":true,"class_sha8":"29ee788e","ws_popup":true,"ws_caption":false,"ws_child":false,"foreground_was_child":false,"ws_disabled":true,"ws_ex_topmost":false,"ws_ex_layered":false,"ws_ex_transparent":false,"ws_ex_appwindow":false,"zero_area":true,"offscreen":true,"equals_a_rect":false,"on_bottom_left_work":false,"title_nonempty":true,"sampled":true}}]}

```



### context-insert — PASS

Session: `ss-uno018-ui-5e3ebdeac7404a7e08413b64a62d5c9a/ss-uno019-native-cdcd93c9b2b048c198532e87ec9fce90`.

FailureStage: `none`; receipt/host check PASS; host shutdown-complete=true. Rows/usage=1/1; independent usage PASS.

Outer clipboard epoch: changed; combined independent positive boundary:

```json

{
  "usageVerified": true,
  "clipboardEpochChanged": true
}

```



Monitor: absent (no independent monitor failure recorded).

All startup/readiness/trace/native-stage and selected fixed boundary lines, verbatim:

```text

2026-11-02T01:49:00.923Z INFO native_startup_foreground phase=target_launch relation=foreign launcher_foreground=False sample_order=1 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=False cloaked=False cloaked_known=True class_sha8=827664e8 ws_popup=False ws_caption=True ws_child=False foreground_was_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=False title_nonempty=True
2026-11-02T01:49:00.923Z INFO native_startup_foreground phase=host_ready relation=foreign launcher_foreground=False sample_order=2 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=False cloaked=False cloaked_known=True class_sha8=827664e8 ws_popup=False ws_caption=True ws_child=False foreground_was_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=True
2026-11-02T01:49:00.923Z INFO native_host_readiness reason=ready
2026-11-02T01:49:01.117Z INFO owned_initial_focus phase=waiting
2026-11-02T01:49:01.119Z INFO native_startup_foreground phase=preparation_start relation=foreign launcher_foreground=False sample_order=3 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=False cloaked=False cloaked_known=True class_sha8=827664e8 ws_popup=False ws_caption=True ws_child=False foreground_was_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=True
2026-11-02T01:49:01.168Z INFO owned_preparation_admission reason=ready
2026-11-02T01:49:01.303Z INFO native_startup_foreground phase=before_focus relation=foreign launcher_foreground=False sample_order=4 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=False cloaked=False cloaked_known=True class_sha8=827664e8 ws_popup=False ws_caption=True ws_child=False foreground_was_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=True
2026-11-02T01:49:01.339Z INFO native_startup_foreground phase=after_focus relation=owned_a launcher_foreground=False sample_order=5 class_token=target018_class visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=1c78a1f2 ws_popup=False ws_caption=True ws_child=False foreground_was_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=True on_bottom_left_work=True title_nonempty=True
2026-11-02T01:49:01.596Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=1 elapsed_ms=0 class_token=target018_class visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=1c78a1f2 ws_popup=False ws_caption=True ws_child=False foreground_was_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=True on_bottom_left_work=True title_nonempty=True
2026-11-02T01:49:01.597Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=2 elapsed_ms=29 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:49:01.597Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=3 elapsed_ms=44 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:49:01.597Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=4 elapsed_ms=74 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:49:01.597Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=5 elapsed_ms=89 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:49:01.597Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=6 elapsed_ms=104 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:49:01.598Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=7 elapsed_ms=119 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:49:01.598Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=8 elapsed_ms=150 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:49:01.598Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=9 elapsed_ms=165 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:49:01.598Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=10 elapsed_ms=180 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:49:01.599Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=11 elapsed_ms=196 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:49:01.599Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=12 elapsed_ms=211 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:49:01.599Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=13 elapsed_ms=226 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:49:01.599Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=14 elapsed_ms=242 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:49:03.327Z INFO owned_target_preparation reason=Prepared
2026-11-02T01:49:03.345Z INFO owned_initial_focus phase=ready
2026-11-02T01:49:03.384Z INFO target_foreground_grant result=granted
2026-11-02T01:49:04.540Z INFO native_popup_focus reason=foreground_accepted
2026-11-02T01:49:04.540Z INFO candidate_popup_focus reason=accepted
2026-11-02T01:49:05.442Z INFO clipboard_swap attempts=1 status=Swapped saved=4 skipped_by_design=0 unsaved=0 cap_exceeded=False bytes=104 ms=214
2026-11-02T01:49:06.095Z INFO clipboard_restore status=Restored attempts=1
2026-11-02T01:49:06.095Z INFO insert_result outcome=Inserted id=1 notice=False ms=1461

```



Coordinator stdout:

```text

native_phase=guardian_ready
native_phase=awaiting_owned_a
native_result=context-insert_settled

```



target-thread-windows-ready.json, verbatim:

```json

{"phase":"ready","complete":true,"windows":[{"window_id":"3d2e4cd8","is_a":false,"is_b":true,"new_at_refusal":false,"facts":{"class_token":"target018_class","visible":true,"toolwindow":false,"noactivate":false,"has_owner":false,"owner_is_a":false,"owner_is_b":false,"is_message_only":false,"same_thread_as_a":true,"cloaked":false,"cloaked_known":true,"class_sha8":"1c78a1f2","ws_popup":false,"ws_caption":true,"ws_child":false,"foreground_was_child":false,"ws_disabled":false,"ws_ex_topmost":false,"ws_ex_layered":false,"ws_ex_transparent":false,"ws_ex_appwindow":false,"zero_area":false,"offscreen":false,"equals_a_rect":false,"on_bottom_left_work":true,"title_nonempty":true,"sampled":true}},{"window_id":"7fff60e9","is_a":true,"is_b":false,"new_at_refusal":false,"facts":{"class_token":"target018_class","visible":true,"toolwindow":false,"noactivate":false,"has_owner":false,"owner_is_a":false,"owner_is_b":false,"is_message_only":false,"same_thread_as_a":true,"cloaked":false,"cloaked_known":true,"class_sha8":"1c78a1f2","ws_popup":false,"ws_caption":true,"ws_child":false,"foreground_was_child":false,"ws_disabled":false,"ws_ex_topmost":false,"ws_ex_layered":false,"ws_ex_transparent":false,"ws_ex_appwindow":false,"zero_area":false,"offscreen":false,"equals_a_rect":true,"on_bottom_left_work":true,"title_nonempty":true,"sampled":true}},{"window_id":"2368a766","is_a":false,"is_b":false,"new_at_refusal":false,"facts":{"class_token":"ime_default","visible":false,"toolwindow":false,"noactivate":false,"has_owner":true,"owner_is_a":true,"owner_is_b":false,"is_message_only":false,"same_thread_as_a":true,"cloaked":false,"cloaked_known":true,"class_sha8":"29ee788e","ws_popup":true,"ws_caption":false,"ws_child":false,"foreground_was_child":false,"ws_disabled":true,"ws_ex_topmost":false,"ws_ex_layered":false,"ws_ex_transparent":false,"ws_ex_appwindow":false,"zero_area":true,"offscreen":true,"equals_a_rect":false,"on_bottom_left_work":false,"title_nonempty":true,"sampled":true}}]}

```



## Picker Import Cancel telemetry — FAIL

Last scene only, while more than five minutes remained before hard end 20:11. Picker parent `ss-uno018-ui-f5685b7ee642bc2d52daf27e53d9e2c9`; session `ss-uno018-ui-5af765ee29d4c2c04dcfb719aa13fde1`. Wrapper/child exit 1. No other picker phase action ran; the fixed telemetry helper initializes helper_entry rows for all four named phase buckets, which does **not** mean those scenes were invoked.

Observer parent/worker failures, all Invoke records if present, and every timeline row:

```json

{
  "observer-parent-failure.json": {
    "reason": "worker_protocol"
  },
  "observer-worker-failure.json": {
    "reason": "input",
    "inputSubreason": "epoch_changed",
    "inputEpochDelta": 8828,
    "inputIdleMs": 31,
    "inputPhase": "unspecified",
    "inputRefusalElapsedMs": 1756,
    "inputSampleCount": 33,
    "pickerEpochTimeline": {
      "import-cancel": [
        {
          "step": "helper_entry",
          "epoch": 386823375,
          "elapsed_ms": 171,
          "changed_from_baseline": false
        },
        {
          "step": "before_main_invoke",
          "epoch": 386823375,
          "elapsed_ms": 321,
          "changed_from_baseline": false
        }
      ],
      "import": [
        {
          "step": "helper_entry",
          "epoch": 386823375,
          "elapsed_ms": 213,
          "changed_from_baseline": false
        }
      ],
      "export-cancel": [
        {
          "step": "helper_entry",
          "epoch": 386823375,
          "elapsed_ms": 214,
          "changed_from_baseline": false
        }
      ],
      "export": [
        {
          "step": "helper_entry",
          "epoch": 386823375,
          "elapsed_ms": 214,
          "changed_from_baseline": false
        }
      ]
    }
  },
  "pickerEpochTimeline": {
    "picker-epoch-timeline-import-cancel.jsonl": [
      {
        "step": "helper_entry",
        "epoch": 386823375,
        "elapsed_ms": 171,
        "changed_from_baseline": false
      },
      {
        "step": "before_main_invoke",
        "epoch": 386823375,
        "elapsed_ms": 321,
        "changed_from_baseline": false
      }
    ],
    "picker-epoch-timeline-import.jsonl": [
      {
        "step": "helper_entry",
        "epoch": 386823375,
        "elapsed_ms": 213,
        "changed_from_baseline": false
      }
    ],
    "picker-epoch-timeline-export-cancel.jsonl": [
      {
        "step": "helper_entry",
        "epoch": 386823375,
        "elapsed_ms": 214,
        "changed_from_baseline": false
      }
    ],
    "picker-epoch-timeline-export.jsonl": [
      {
        "step": "helper_entry",
        "epoch": 386823375,
        "elapsed_ms": 214,
        "changed_from_baseline": false
      }
    ]
  }
}

```



reason=input; inputSubreason=epoch_changed; inputEpochDelta=8828; inputIdleMs=31; inputPhase=unspecified; **inputRefusalElapsedMs=1756; inputSampleCount=33**. OperationClass, pickerStep and HRESULT absent; helper stdout/stderr empty. No picker_schema_multiple or per-match schema record exists.

Import Cancel timeline has helper_entry then before_main_invoke, both unchanged_from_baseline. No after_main_invoke or open/commit bracket file completed; all other phase timelines contain only initialization helper_entry. The immutable epoch refusal happened 1756 ms after observer tracking began; these independently timed records establish neither a human action nor a specific provider as source. inputEpochDelta is unsigned timestamp subtraction, not an input-event count. No epoch exception/rebaseline or retry follows.

Import-cancel active=true, closed/actions=false, cancellation receipt absent. Import/export active/closed/actions all absent. Therefore neither successful picker cancellation nor the explicit actions stop was reached. The bounded failure cleanup is not graceful acceptance. Owned rows/usage 0/0; independent clipboard epoch unchanged; privacy CLEAN and planted controls PASS. Full fixed summary:

```json

{
  "pickerPrivateEvidenceToken": "ss-uno018-ui-5af765ee29d4c2c04dcfb719aa13fde1",
  "pickerParentToken": "ss-uno018-ui-f5685b7ee642bc2d52daf27e53d9e2c9",
  "helperStderr": [],
  "helperStdout": [],
  "privacy": "CLEAN",
  "negativeControls": "PASS",
  "ownedRowsAndUsage": [
    0,
    0
  ],
  "markers": {
    "import-cancel": {
      ".active": true,
      ".closed": false,
      ".actions": false
    },
    "import": {
      ".active": false,
      ".closed": false,
      ".actions": false
    },
    "export-cancel": {
      ".active": false,
      ".closed": false,
      ".actions": false
    },
    "export": {
      ".active": false,
      ".closed": false,
      ".actions": false
    }
  },
  "importCancelledReceipt": false,
  "pickerParentSummary": [
    "{\"phase2\": \"BLOCKED\", \"errorType\": \"ValueError\", \"reason\": \"worker_protocol\", \"privateEvidenceToken\": \"ss-uno018-ui-5af765ee29d4c2c04dcfb719aa13fde1\"}",
    "{\"phase2ClipboardEpoch\": \"unchanged\"}"
  ]
}

```



## Release and pending obligations

All nine trial booleans, scratch UI gate and unchanged wider startup/full-host/prototype gates false:

```text

scripts/run_uno_candidate_startup.py:21:STARTUP_OBSERVER_REVIEWED = False
scripts/run_uno_candidate_acceptance.py:24:FULL_HOST_OBSERVER_REVIEWED = False
prototypes/uno-native-target/windows-insertion/Program.cs:11:    private static readonly bool NativeEntryReviewed = false;
src/SmartSnippets.Uno/Platforms/Windows/AcceptanceNative/NativeAcceptanceFactory.cs:14:    internal static readonly bool SourceReviewed = false;
scripts/uno_candidate_native_fixture/PositiveProtocol.cs:8:    internal static readonly bool SourceReviewed = false;
scripts/uno_candidate_native_fixture/PositiveProtocol.cs:9:    internal static readonly bool InputSourceReviewed = false;
scripts/uno_candidate_native_fixture/OwnedTargetPreparation.cs:26:    internal static readonly bool SourceReviewed = false;
scripts/run_uno_candidate_ui.py:20:UI_SOURCE_REVIEWED = False
scripts/uno_candidate_native_fixture/Program.cs:14:    internal static readonly bool NativeFixtureSourceReviewed = false;
nr13-picker-cancel-only.py:20:UI_SOURCE_REVIEWED = False
scripts/run_uno_candidate_native.py:19:NATIVE_SCENES_SOURCE_REVIEWED = False
scripts/run_uno_candidate_native.py:20:POSITIVE_SCENES_SOURCE_REVIEWED = False
scripts/run_uno_candidate_native.py:21:POSITIVE_INPUT_SOURCE_REVIEWED = False

```



All six tracked trial-source files have empty git diff; operator-local AGENTS/safety-guide edits remain excluded. Final process audit:

```json

{
  "observedUtc": "2026-11-02T01:50:12.6189276+00:00",
  "counts": {
    "SmartSnippets.UnoCandidate.exe": 0,
    "SmartSnippets.UnoAcceptanceFixture.exe": 0,
    "SmartSnippets.UnoCandidate.Guardian.exe": 0,
    "SmartSnippets.Target018.exe": 0,
    "nr13PythonHelpers": 0,
    "UIHelper": 0
  }
}

```



No pending clipboard/keyboard receipt across any native session; all native applications and parent/UI helpers exited. No guardian/coordinator/keyboard-recovery process was killed or adopted. Recovery settled; abort was the one intentional reviewed post-publication worker-Job termination. Clipboard final locked, RAM-only synthetic-baseline comparison, **without reseeding**:

```json

{
  "builtin_text": 4,
  "ownership_marker": 1,
  "protective_metadata": 3,
  "unsupported": 0,
  "owner_present": false,
  "authorized_synthetic_baseline_matches": true,
  "sequence_stable": true
}

```



The authorized QA baseline matches and sequence is stable; owner absent and unsupported formats zero. **One ownership marker and three protective metadata formats remain.** These are reported, not erased or misrepresented as absent. This check does not attest arbitrary clipboard data from before the first authorized seed. No live profile was queried, moved or deleted; private evidence/artifacts retained.

**SCREEN-DONE nr-13 2026-11-01 19:50:13 local (-0600)**. Written immediately after closure checks; no subsequent desktop/clipboard work. Only filesystem evidence assembly/hash verification and this doc-only commit followed.

Pending: posted-main/editor-copy post-grant focus failures; popup-copy closed-phase zero-foreground refusal; picker input attribution/cancel acceptance; independent review of new root-normalized native evidence; remaining native pointer/hover/tray/fault/lifecycle/oldest-runtime and final wiring/CI acceptance obligations. No active recovery/operator key press is pending. Plans 018/019 remain open. This released slot authorizes no retry or later launch.

Commit scope: **only docs/research/056-native-foreground-root-run.md**. No trial flag, helper, operator instruction edit or production code included; no push.
