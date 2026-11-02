# 057 — nr-14: native post-grant acceptance run

Authorized brief `native-run15-brief.md`, 2026-11-01. **Source HEAD `86f9cb6e57575d30bfdbc7cdb90fab23f2ae65e8` (`86f9cb6`), branch `feature/uno019-native-trial13`. Phase-0 authorized redo PASS; controls PASS; all six positives PASS; picker Import Cancel FAIL; desktop released.** Plans 018/019 remain incomplete. This is scoped posted/VM-callback evidence, not physical shortcut/pointer/menu proof, production-native parity or release readiness.

## Authorization and initial build failure

Read AGENTS, safety guide 002, progress handoff 006, approved Plan 019, research 053–056 and both fixture READMEs. Preserved the operator-local AGENTS/safety-guide edits and excluded them from commit. No delegation or push. Ordinary candidate built but never launched. Only bottom-left DISPLAY29 was used; never primary/top-left. No download, clock change, keyboard/mouse synthesis by the agent, generic key-up, registered scene or posted-enter occurred. Product-guarded paste into the exact owned A was the only insertion input. Reviewed UIA SetFocus was intentional owned activation, never nonactivation proof.

Initial phase 0 on a32925e failed guardian publication with CS0246: ForegroundNullGap missing from the compile closure used by OwnedFocus.cs (41,23 and 64,23). Native fixture/Target018 had built; supervisor stopped at the first failure. No application/window/clipboard work occurred. Flags closed, zero processes, initial SCREEN-DONE 20:42:15 local. Initial logs/partial manifest were retained as nr14-initial-*; no native scene was retried.

Leader committed/cherry-picked 86f9cb6, linking Platforms/Windows/ForegroundNullGap.cs into the guardian, and explicitly authorized **ONE phase-0 redo**. This was not a scene retry or guard waiver. All five artifacts were rebuilt fresh in new output directories on 86f9cb6. The report was replaced with fresh status plus one initial-failure history line. Redo readiness was written before any desktop/clipboard action, then the runner waited for the grant.

## Phase 0 redo — PASS

Exactly nine tracked trial booleans temporarily enabled: native Python NATIVE_SCENES_SOURCE_REVIEWED/POSITIVE_SCENES_SOURCE_REVIEWED/POSITIVE_INPUT_SOURCE_REVIEWED; UI Python UI_SOURCE_REVIEWED; fixture NativeFixtureSourceReviewed; PositiveProtocol.SourceReviewed/InputSourceReviewed; OwnedTargetPreparation.SourceReviewed; NativeAcceptanceFactory.SourceReviewed. All nine and the scratch UI gate were restored false. Wider gates were unchanged; ordinary capability stayed denied. Exact private diff nr14-enabled-flags.diff SHA256: `491d386f85faeb2d3a517ce025a3daf10f7c92735a77c671affe33d1b4103fd4`. No enabled source committed.

Helpers copied from nr13 using explicit UTF-8, originals untouched. The admission guard preserves self/live-ancestor/trusted-parent exclusions and fixed admission_match fields. It ran dry twice consecutively from the same bash → Python → pwsh -File chain and returned before clip.exe:

```jsonl

{"attempt": 1, "exitCode": 0, "stdout": ["admission_dry_pass=1"], "stderrEmpty": true, "reason": "none"}
{"attempt": 2, "exitCode": 0, "stdout": ["admission_dry_pass=1"], "stderrEmpty": true, "reason": "none"}

```



All scratch PowerShell parsing and Python ast.parse passed. The bounded 60-second held-input preflight was retained without relaxed quiet/input predicates. Picker Python/PowerShell copied from current tracked sources, retaining pk-03 telemetry; only repository/helper paths, the local gate and explicit `throw 'actions'` immediately after `WaitPhase 'ui-import-cancelled'` changed. It precedes Import commit; no undefined Fail call. Read-only collector includes native_scene_refusal and allows the larger fixed picker sample record. No scene, guard or input epoch was relaxed.

Five fresh native x64 artifacts built sequentially in unique output directories. Strict Native AOT/trim/analyzers/warnings-as-errors, linked fixture effective-property/reference/PE/SQLite/provenance gates and warning/error scans passed. Support builds used owner-private ACLs, clean→publish, empty NoWarn and no trim-warning suppression. No test-suite rerun or legacy publish occurred.

| Artifact | Token | SHA256 |
|---|---|---|
| native | `6abc7e67eff04c69baa71a0e5c4a46e7` | `427768f74c0cc170c341314a79deb21c48d8a50c85fbbfb570e4ce9ea1d82d1f` |
| target | `32d6621b903b463e9a99afed326aba51` | `0ba3b4953d6eba4741f2db98fe4a39ae892dd2384a3745cd535dbc6e0945f62d` |
| guardian | `32ea48569f2d411f9bb247874b65140e` | `3fd0028628c789d45145003e554050548320f2a52e1cbad1a2f68dd1f52b76ae` |
| management | `7f2df16efa6f4a92be5df3f63036ebcd` | `2418c65e9476ebd1c3f38ee29a4465c90096f5cad679f58baa50eba2e91c31c2` |
| candidate | `a42da9b9321a43b5a42f4f5832303175` | `cfd744b628ea7126e8009b0abbf7ab80c4d18814650f59f9b8ba6d145351286b` |



Enabled native source SHA256 `81352980b0f862f696d0cd3d956ab89f038e04c40a8b49902d5beb08d6d2d77f`; management source SHA256 `6b0bdceeb4461965a8bc96501f5f1482a5198b383d2927c09b17a86ec29b76bf`. All five executable hashes rechecked after release: PASS. Trial-enabled binaries remain evidence, not later launch authority.

Executed-helper hashes (before flag closure):

```text

scratch_ps_syntax=PASS
{"name":"nr14-admit-window.py","sha256":"555f830ea5779b3bc34eea975522bbc98964e311d798a22d674045ee37b427c1"}
{"name":"nr14-artifacts.py","sha256":"3925cab378191d2de841e8d28ee2bf562056c5c5406314c62d240d35081b6e58"}
{"name":"nr14-build-phase0.ps1","sha256":"e9dbe43e41e6933d94548e7793d56680a7bb55197db8df5c7c2756ec88f79cd4"}
{"name":"nr14-build-support.ps1","sha256":"1372e2a33504b5170bfc566702a46bd512a9e462503f13000d78d704fc122b24"}
{"name":"nr14-clipboard-check.py","sha256":"a4b0cd7fde4a90aae59cc5a7382a5e48b009618e38a01aebd1ef7ed2c789a40a"}
{"name":"nr14-clipboard-release.py","sha256":"c24e8cc8795725913cfb1fc7d459c39cb1c35c29a7c33dcf215674234bbe7c70"}
{"name":"nr14-dry-admission.py","sha256":"daafe0b08dc1c743387dc51081a766a6e832cd22a95e03641e4a97e1bc6c392b"}
{"name":"nr14-evidence-check.py","sha256":"7bced623968ff82b3a6096877111588c5e6726df750f7eacfaabb0ab98372d3f"}
{"name":"nr14-parse-helpers.ps1","sha256":"c9ba369f7d9e14213fdd48ad416013827d880b9a457c86ea508f7ddd14eabaef"}
{"name":"nr14-picker-cancel-only.ps1","sha256":"2f6b40f78eebbdd597d78c9dae758bcc498c01593fd588ca8f8fbb95124de8ab"}
{"name":"nr14-picker-cancel-only.py","sha256":"ff83dac8b3cb783cdecd169c3dd457424ad8f3fe6948c2b9d45a1014c2d5541b"}
{"name":"nr14-picker.py","sha256":"d36ce2b8e87b653d04d91e9bd1dd521e60bb9ee753d2cb25d62af236dadedab6"}
{"name":"nr14-preflight.py","sha256":"c0e839d338edc375d7f1fb7285c46eedd1273a26061499df97f50c178c39d96d"}
{"name":"nr14-process-check.ps1","sha256":"e92264feb9cf9fcaae557eb78e8f3d47e4ea17a687442fa1b194baf950f3a9cc"}
{"name":"nr14-run.py","sha256":"8f85bc77fdd21d9b3ad8519091f9e2e895184758234a03aa956faf5363f64b0e"}
{"name":"nr14-seed.ps1","sha256":"44dd3d2acc999cee9c2b250d47cea6cd4d215f46c40f09158ab722f1d39ff44d"}
{"name":"nr14-wait-grant.py","sha256":"3ba29c55e814221c261f17bd5379d3582cb0ea2edd52533108f8b525acae2221"}

```



## Grant, time and live geometry

```jsonl

{"status": "WAITING", "polls": 1, "waitSeconds": 0.001, "observedUtc": "2026-11-02T02:47:53.010787+00:00"}
{"status": "GRANT_AVAILABLE", "polls": 2, "waitSeconds": 30.011, "observedUtc": "2026-11-02T02:48:23.020587+00:00"}

```



```text

GRANTED nr-14
window: EXCLUSIVE full desktop; harness windows on bottom-left DISPLAY29 (-1920,1506 1920x1080) only; never primary or top-left
start: 2026-11-01 20:48 local
hard end: 2026-11-01 21:13 local
scope: controls (restore, abort, owner-refusal); posted-row, posted-main; popup-copy, editor-copy, footer-copy, context-insert (synthetic text, clipboard restore); optional picker Import Cancel only if >=5 min remain before hard end
excluded: posted-enter (SendInput, D50); registered-* scenes
rules: owned throwaway profiles; posted callbacks only; no keyboard/mouse synthesis; stop and clean up by hard end
source: pc-manager, under the user's pre-approval for bottom-left runs of this kind

```



```json

{
  "status": "GRANTED",
  "slotStartEpoch": 1793587680.0,
  "hardEndEpoch": 1793589180.0,
  "deadlineMonotonic": 391857.6017611554,
  "observedUtc": "2026-11-02T02:48:50.176967+00:00",
  "slotStartLocal": "2026-11-01 20:48",
  "hardEndLocal": "2026-11-01 21:13"
}

```



```text

{"monitors": [{"display": "\\\\.\\DISPLAY5", "bounds": [0, 2160, 2560, 3760], "work": [0, 2160, 2560, 3688], "primary": false}, {"display": "\\\\.\\DISPLAY2", "bounds": [3840, 47, 5040, 1967], "work": [3840, 47, 5040, 1919], "primary": false}, {"display": "\\\\.\\DISPLAY3", "bounds": [0, 0, 3840, 2160], "work": [0, 0, 3840, 2088], "primary": true}, {"display": "\\\\.\\DISPLAY29", "bounds": [-1920, 1506, 0, 2586], "work": [-1920, 1506, 0, 2538], "primary": false}, {"display": "\\\\.\\DISPLAY30", "bounds": [-1920, 306, 0, 1506], "work": [-1920, 306, 0, 1458], "primary": false}], "authorized": {"display": "\\\\.\\DISPLAY29", "bounds": [-1920, 1506, 0, 2586], "work": [-1920, 1506, 0, 2538], "primary": false}}
{"sampleOrder": 1, "held_virtual_keys": [], "inputIdleMs": 292563, "elapsedSeconds": 0.001}
nr14_preflight_quiet=1

```



Filesystem-only polling used 30-second intervals. The stated start had arrived at admission (20:48:50 local); hard end was 21:13. Live primary DISPLAY3 was 3840×2160. Authorized nonprimary DISPLAY29 bounds (-1920,1506,0,2586), work (-1920,1506,0,2538), matched exactly. First held-input sample had no held keys; downstream immutable 1500-ms quiet guard remained unchanged. No stale geometry or primary-screen substitution.

## Commands actually executed

```text

# <scratchpad> is the private brief/report directory. set -e / && used.
# Initial failed build attempt released; leader authorized one redo on 86f9cb6.
# Re-enable nine flags; record exact diff; UTF-8 helpers; fresh output dirs.
python -I -B <scratchpad>/nr14-dry-admission.py
pwsh -NoProfile -File <scratchpad>/nr14-process-check.ps1
pwsh -NoProfile -File <scratchpad>/nr14-parse-helpers.ps1
# ast.parse scratch Python helpers.
pwsh -NoProfile -File <scratchpad>/nr14-build-phase0.ps1
# Sequential inside wrapper:
pwsh -NoProfile -File scripts/uno_candidate_native_fixture/build.ps1 -BuildOnly
pwsh -NoProfile -File <scratchpad>/nr14-build-support.ps1
pwsh -NoProfile -File scripts/verify-uno-candidate.ps1 -BuildOnly
pwsh -NoProfile -File scripts/publish-uno-candidate-aot.ps1
python -I -B <scratchpad>/nr14-artifacts.py
# Recheck artifacts / zero process readiness; PHASE0-READY.
python -I -B <scratchpad>/nr14-wait-grant.py
python -I -B <scratchpad>/nr14-admit-window.py
python -I -B <scratchpad>/nr14-preflight.py
python -I -B <scratchpad>/nr14-run.py
# Exact native parent token below; no external recovery timeout-kill.
python -I -B <scratchpad>/nr14-evidence-check.py ss-uno018-ui-559a777ada54ca49cadf4d7d89e5d4cb
# Close nine flags + scratch UI gate; grep proof; empty tracked source diff.
pwsh -NoProfile -File <scratchpad>/nr14-process-check.ps1
python -I -B <scratchpad>/nr14-clipboard-check.py
# SCREEN-DONE; filesystem-only artifact/document checks and doc-only commit.

```



Build wrappers internally selected the pinned SDK cwd and explicitly checked exit codes. Builds were never parallel. Native parent retained fresh same-artifact controls/witnesses in RAM, performed the reviewed seed admission and invoked scripts/run_uno_candidate_native.py. Seed used only the authorized fixed QA baseline through clip.exe, with zero native/UI processes and no pending recovery. Controls chained authenticated restorative witnesses; positives freshly seeded independently. No reseed at release. Abort intentionally terminated only the reviewed post-publication worker/target Job; independent guardian survived and settled. No pending recovery or uncertain keyboard process was killed.

## Per-phase ledger and finding

| Step | Result | Independent evidence |
|---|---|---|
| Initial phase 0 | FAIL | guardian CS0246; no desktop/clipboard use |
| Authorized phase-0 redo 1 | PASS | five fresh strict artifacts, hashes, syntax, two dry admissions |
| Phase 1 restore → abort → owner-refusal | PASS each | fresh same-artifact witnesses; zero rows/usage; recovery settled |
| Phase 2 posted-row | PASS | usage 1 PASS; epoch changed PASS; exact host/receipt PASS |
| Phase 2 posted-main | PASS | usage 1 PASS; epoch changed PASS; exact host/receipt PASS |
| Phase 2 popup-copy | PASS | usage 0 PASS; epoch changed PASS; exact host/receipt PASS |
| Phase 2 editor-copy | PASS | usage 0 PASS; epoch changed PASS; exact host/receipt PASS |
| Phase 2 footer-copy | PASS | usage 0 PASS; epoch changed PASS; exact host/receipt PASS |
| Phase 2 context-insert | PASS | usage 1 PASS; epoch changed PASS; exact host/receipt PASS |
| posted-enter / registered-* | NOT RUN | input synthesis excluded / outside scope |
| Phase 3 Import Cancel | FAIL | input epoch_changed; 0 rows/usage; clipboard unchanged |
| Other picker phases | NOT RUN | outside scope; no later mutation |
| Release | PASS | flags false, zero processes, no recovery pending; baseline verified |

Native trusted parent: `ss-uno018-ui-559a777ada54ca49cadf4d7d89e5d4cb`. All six positives obtained `target_foreground_grant result=granted`, reached native popup focus `foreground_accepted`, completed their distinct consumer and graceful host shutdown, and passed independent usage/clipboard checks. Insert scenes require exact A readback/empty B, single capture/paste and persisted usage once. Copy scenes require their distinct popup/editor-save-failure/footer-error consumers, one Copy, empty targets, no paste and zero usage. These are **registered_posted_callback_not_physical**, not physical shortcut/pointer/menu proof.

The three nr13 failures (posted-main, editor-copy, popup-copy) are not reproduced on this source/artifact set. This is fresh scoped PASS, not retroactive acceptance of earlier failures or proof that one particular ordering change caused every improvement. There is no failed positive monitor record, fg-03 failure transition/nullGap timeline or reason token to copy: those artifacts were not emitted. FailureStage is none for every native scene. No native_positive_failed/native_scene_refusal line exists. Raw NULL behavior and failure-path tokens are therefore not specifically exercised/proven by this passing run.

High-rate preparation trace totals: 96 rows, 86 raw child and 10 nonchild; all normalized relation=owned_a. These sampled/non-atomic diagnostics are not continuous foreground identity or causal attribution. Full lines follow, including every startup phase/relation/launcher_foreground/sample_order and readiness reason.

## Exact native records

### restore — PASS

Session: `ss-uno018-ui-e0fc6b4603979a188e4af31f5edc5134/ss-uno019-native-f302708856a343bf89867ab763aefe06`.

```json

{
  "failureStage": "none",
  "receiptHostCheck": "PASS",
  "hostShutdownComplete": true,
  "ownedRows": 0,
  "ownedUsage": 0,
  "independentUsageCheck": "PASS",
  "independentOuterBoundary": null,
  "outerClipboardEpochChanged": null,
  "monitorFailure": null,
  "privacy": "CLEAN",
  "negativeControls": "PASS",
  "stderrEmpty": true,
  "pendingReceipt": false
}

```



Full fixed startup/readiness/preparation/trace/grant/focus and selected boundary lines, verbatim:

```text

2026-11-02T02:48:58.792Z INFO native_startup_foreground phase=target_launch relation=foreign launcher_foreground=False sample_order=1 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=False cloaked=False cloaked_known=True class_sha8=827664e8 ws_popup=False ws_caption=True ws_child=False foreground_was_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=False title_nonempty=True
2026-11-02T02:48:58.792Z INFO native_startup_foreground phase=host_ready relation=foreign launcher_foreground=False sample_order=2 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=False cloaked=False cloaked_known=True class_sha8=827664e8 ws_popup=False ws_caption=True ws_child=False foreground_was_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=False title_nonempty=True
2026-11-02T02:48:58.792Z INFO native_host_readiness reason=ready
2026-11-02T02:48:59.030Z INFO native_control_admission reason=Ready
2026-11-02T02:48:59.112Z INFO native_control_publish status=swapped

```



Coordinator stdout:

```text

native_phase=guardian_ready
native_phase=published
native_result=regular_restored

```



target-thread-windows-ready.json, verbatim:

```json

{"phase":"ready","complete":true,"windows":[{"window_id":"f81c614c","is_a":false,"is_b":true,"new_at_refusal":false,"facts":{"class_token":"target018_class","visible":true,"toolwindow":false,"noactivate":false,"has_owner":false,"owner_is_a":false,"owner_is_b":false,"is_message_only":false,"same_thread_as_a":true,"cloaked":false,"cloaked_known":true,"class_sha8":"087e9d48","ws_popup":false,"ws_caption":true,"ws_child":false,"foreground_was_child":false,"ws_disabled":false,"ws_ex_topmost":false,"ws_ex_layered":false,"ws_ex_transparent":false,"ws_ex_appwindow":false,"zero_area":false,"offscreen":false,"equals_a_rect":false,"on_bottom_left_work":true,"title_nonempty":true,"sampled":true}},{"window_id":"8317ca3f","is_a":true,"is_b":false,"new_at_refusal":false,"facts":{"class_token":"target018_class","visible":true,"toolwindow":false,"noactivate":false,"has_owner":false,"owner_is_a":false,"owner_is_b":false,"is_message_only":false,"same_thread_as_a":true,"cloaked":false,"cloaked_known":true,"class_sha8":"087e9d48","ws_popup":false,"ws_caption":true,"ws_child":false,"foreground_was_child":false,"ws_disabled":false,"ws_ex_topmost":false,"ws_ex_layered":false,"ws_ex_transparent":false,"ws_ex_appwindow":false,"zero_area":false,"offscreen":false,"equals_a_rect":true,"on_bottom_left_work":true,"title_nonempty":true,"sampled":true}},{"window_id":"284d33bb","is_a":false,"is_b":false,"new_at_refusal":false,"facts":{"class_token":"ime_default","visible":false,"toolwindow":false,"noactivate":false,"has_owner":true,"owner_is_a":true,"owner_is_b":false,"is_message_only":false,"same_thread_as_a":true,"cloaked":false,"cloaked_known":true,"class_sha8":"29ee788e","ws_popup":true,"ws_caption":false,"ws_child":false,"foreground_was_child":false,"ws_disabled":true,"ws_ex_topmost":false,"ws_ex_layered":false,"ws_ex_transparent":false,"ws_ex_appwindow":false,"zero_area":true,"offscreen":true,"equals_a_rect":false,"on_bottom_left_work":false,"title_nonempty":true,"sampled":true}}]}

```



No native-monitor-failure.json or refusal inventory exists for this passed scene; no failure timeline/reason is fabricated.

### abort — PASS

Session: `ss-uno018-ui-fb0a34c20c1a8b75749ae3025a31f27c/ss-uno019-native-aff7224ef30b44c0b7b2121617923558`.

```json

{
  "failureStage": "none",
  "receiptHostCheck": "PASS",
  "hostShutdownComplete": false,
  "ownedRows": 0,
  "ownedUsage": 0,
  "independentUsageCheck": "PASS",
  "independentOuterBoundary": null,
  "outerClipboardEpochChanged": null,
  "monitorFailure": null,
  "privacy": "CLEAN",
  "negativeControls": "PASS",
  "stderrEmpty": true,
  "pendingReceipt": false
}

```



Full fixed startup/readiness/preparation/trace/grant/focus and selected boundary lines, verbatim:

```text

2026-11-02T02:49:05.844Z INFO native_startup_foreground phase=target_launch relation=foreign launcher_foreground=False sample_order=1 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=False cloaked=False cloaked_known=True class_sha8=827664e8 ws_popup=False ws_caption=True ws_child=False foreground_was_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=False title_nonempty=True
2026-11-02T02:49:05.845Z INFO native_startup_foreground phase=host_ready relation=foreign launcher_foreground=False sample_order=2 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=False cloaked=False cloaked_known=True class_sha8=827664e8 ws_popup=False ws_caption=True ws_child=False foreground_was_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=False title_nonempty=True
2026-11-02T02:49:05.845Z INFO native_host_readiness reason=ready
2026-11-02T02:49:06.033Z INFO native_control_admission reason=Ready
2026-11-02T02:49:06.153Z INFO native_control_publish status=swapped

```



Coordinator stdout:

```text

native_phase=guardian_ready
native_phase=published
native_result=abort_restored

```



target-thread-windows-ready.json, verbatim:

```json

{"phase":"ready","complete":true,"windows":[{"window_id":"51433fbe","is_a":false,"is_b":true,"new_at_refusal":false,"facts":{"class_token":"target018_class","visible":true,"toolwindow":false,"noactivate":false,"has_owner":false,"owner_is_a":false,"owner_is_b":false,"is_message_only":false,"same_thread_as_a":true,"cloaked":false,"cloaked_known":true,"class_sha8":"a0f26596","ws_popup":false,"ws_caption":true,"ws_child":false,"foreground_was_child":false,"ws_disabled":false,"ws_ex_topmost":false,"ws_ex_layered":false,"ws_ex_transparent":false,"ws_ex_appwindow":false,"zero_area":false,"offscreen":false,"equals_a_rect":false,"on_bottom_left_work":true,"title_nonempty":true,"sampled":true}},{"window_id":"f9881519","is_a":true,"is_b":false,"new_at_refusal":false,"facts":{"class_token":"target018_class","visible":true,"toolwindow":false,"noactivate":false,"has_owner":false,"owner_is_a":false,"owner_is_b":false,"is_message_only":false,"same_thread_as_a":true,"cloaked":false,"cloaked_known":true,"class_sha8":"a0f26596","ws_popup":false,"ws_caption":true,"ws_child":false,"foreground_was_child":false,"ws_disabled":false,"ws_ex_topmost":false,"ws_ex_layered":false,"ws_ex_transparent":false,"ws_ex_appwindow":false,"zero_area":false,"offscreen":false,"equals_a_rect":true,"on_bottom_left_work":true,"title_nonempty":true,"sampled":true}},{"window_id":"0839f568","is_a":false,"is_b":false,"new_at_refusal":false,"facts":{"class_token":"ime_default","visible":false,"toolwindow":false,"noactivate":false,"has_owner":true,"owner_is_a":true,"owner_is_b":false,"is_message_only":false,"same_thread_as_a":true,"cloaked":false,"cloaked_known":true,"class_sha8":"29ee788e","ws_popup":true,"ws_caption":false,"ws_child":false,"foreground_was_child":false,"ws_disabled":true,"ws_ex_topmost":false,"ws_ex_layered":false,"ws_ex_transparent":false,"ws_ex_appwindow":false,"zero_area":true,"offscreen":true,"equals_a_rect":false,"on_bottom_left_work":false,"title_nonempty":true,"sampled":true}}]}

```



No native-monitor-failure.json or refusal inventory exists for this passed scene. No failure transition timeline/reason is fabricated. Abort intentionally lacks graceful worker shutdown.

### owner-refusal — PASS

Session: `ss-uno018-ui-4e99999edf6a5450945863f9c75266cb/ss-uno019-native-325e50ac0ab1434d8d6e57588282baba`.

```json

{
  "failureStage": "none",
  "receiptHostCheck": "PASS",
  "hostShutdownComplete": true,
  "ownedRows": 0,
  "ownedUsage": 0,
  "independentUsageCheck": "PASS",
  "independentOuterBoundary": null,
  "outerClipboardEpochChanged": null,
  "monitorFailure": null,
  "privacy": "CLEAN",
  "negativeControls": "PASS",
  "stderrEmpty": true,
  "pendingReceipt": false
}

```



Full fixed startup/readiness/preparation/trace/grant/focus and selected boundary lines, verbatim:

```text

2026-11-02T02:49:13.096Z INFO native_startup_foreground phase=target_launch relation=foreign launcher_foreground=False sample_order=1 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=False cloaked=False cloaked_known=True class_sha8=827664e8 ws_popup=False ws_caption=True ws_child=False foreground_was_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=False title_nonempty=True
2026-11-02T02:49:13.097Z INFO native_startup_foreground phase=host_ready relation=foreign launcher_foreground=False sample_order=2 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=False cloaked=False cloaked_known=True class_sha8=827664e8 ws_popup=False ws_caption=True ws_child=False foreground_was_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=False title_nonempty=True
2026-11-02T02:49:13.097Z INFO native_host_readiness reason=ready
2026-11-02T02:49:13.299Z INFO native_control_admission reason=Ready
2026-11-02T02:49:13.398Z INFO native_control_publish status=swapped

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

{"phase":"ready","complete":true,"windows":[{"window_id":"5eb100c6","is_a":false,"is_b":true,"new_at_refusal":false,"facts":{"class_token":"target018_class","visible":true,"toolwindow":false,"noactivate":false,"has_owner":false,"owner_is_a":false,"owner_is_b":false,"is_message_only":false,"same_thread_as_a":true,"cloaked":false,"cloaked_known":true,"class_sha8":"6b6bbc07","ws_popup":false,"ws_caption":true,"ws_child":false,"foreground_was_child":false,"ws_disabled":false,"ws_ex_topmost":false,"ws_ex_layered":false,"ws_ex_transparent":false,"ws_ex_appwindow":false,"zero_area":false,"offscreen":false,"equals_a_rect":false,"on_bottom_left_work":true,"title_nonempty":true,"sampled":true}},{"window_id":"95c56a18","is_a":true,"is_b":false,"new_at_refusal":false,"facts":{"class_token":"target018_class","visible":true,"toolwindow":false,"noactivate":false,"has_owner":false,"owner_is_a":false,"owner_is_b":false,"is_message_only":false,"same_thread_as_a":true,"cloaked":false,"cloaked_known":true,"class_sha8":"6b6bbc07","ws_popup":false,"ws_caption":true,"ws_child":false,"foreground_was_child":false,"ws_disabled":false,"ws_ex_topmost":false,"ws_ex_layered":false,"ws_ex_transparent":false,"ws_ex_appwindow":false,"zero_area":false,"offscreen":false,"equals_a_rect":true,"on_bottom_left_work":true,"title_nonempty":true,"sampled":true}},{"window_id":"0874d52b","is_a":false,"is_b":false,"new_at_refusal":false,"facts":{"class_token":"ime_default","visible":false,"toolwindow":false,"noactivate":false,"has_owner":true,"owner_is_a":true,"owner_is_b":false,"is_message_only":false,"same_thread_as_a":true,"cloaked":false,"cloaked_known":true,"class_sha8":"29ee788e","ws_popup":true,"ws_caption":false,"ws_child":false,"foreground_was_child":false,"ws_disabled":true,"ws_ex_topmost":false,"ws_ex_layered":false,"ws_ex_transparent":false,"ws_ex_appwindow":false,"zero_area":true,"offscreen":true,"equals_a_rect":false,"on_bottom_left_work":false,"title_nonempty":true,"sampled":true}}]}

```



No native-monitor-failure.json or refusal inventory exists for this passed scene; no failure timeline/reason is fabricated.

### posted-row — PASS

Session: `ss-uno018-ui-05b24b32740505a2700968f27001a3f6/ss-uno019-native-0316a444fe0142088c824e88e4d37403`.

```json

{
  "failureStage": "none",
  "receiptHostCheck": "PASS",
  "hostShutdownComplete": true,
  "ownedRows": 1,
  "ownedUsage": 1,
  "independentUsageCheck": "PASS",
  "independentOuterBoundary": {
    "usageVerified": true,
    "clipboardEpochChanged": true
  },
  "outerClipboardEpochChanged": true,
  "monitorFailure": null,
  "privacy": "CLEAN",
  "negativeControls": "PASS",
  "stderrEmpty": true,
  "pendingReceipt": false
}

```



Full fixed startup/readiness/preparation/trace/grant/focus and selected boundary lines, verbatim:

```text

2026-11-02T02:49:24.310Z INFO native_startup_foreground phase=target_launch relation=foreign launcher_foreground=False sample_order=1 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=False cloaked=False cloaked_known=True class_sha8=827664e8 ws_popup=False ws_caption=True ws_child=False foreground_was_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=False title_nonempty=True
2026-11-02T02:49:24.310Z INFO native_startup_foreground phase=host_ready relation=foreign launcher_foreground=False sample_order=2 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=False cloaked=False cloaked_known=True class_sha8=827664e8 ws_popup=False ws_caption=True ws_child=False foreground_was_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=False title_nonempty=True
2026-11-02T02:49:24.311Z INFO native_host_readiness reason=ready
2026-11-02T02:49:24.654Z INFO owned_initial_focus phase=waiting
2026-11-02T02:49:24.658Z INFO native_startup_foreground phase=preparation_start relation=foreign launcher_foreground=False sample_order=3 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=False cloaked=False cloaked_known=True class_sha8=827664e8 ws_popup=False ws_caption=True ws_child=False foreground_was_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=False title_nonempty=True
2026-11-02T02:49:24.705Z INFO owned_preparation_admission reason=ready
2026-11-02T02:49:24.871Z INFO native_startup_foreground phase=before_focus relation=foreign launcher_foreground=False sample_order=4 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=False cloaked=False cloaked_known=True class_sha8=827664e8 ws_popup=False ws_caption=True ws_child=False foreground_was_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=False title_nonempty=True
2026-11-02T02:49:24.920Z INFO native_startup_foreground phase=after_focus relation=owned_a launcher_foreground=False sample_order=5 class_token=target018_class visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=8f00d95d ws_popup=False ws_caption=True ws_child=False foreground_was_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=True on_bottom_left_work=True title_nonempty=True
2026-11-02T02:49:25.183Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=1 elapsed_ms=0 class_token=target018_class visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=8f00d95d ws_popup=False ws_caption=True ws_child=False foreground_was_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=True on_bottom_left_work=True title_nonempty=True
2026-11-02T02:49:25.183Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=2 elapsed_ms=18 class_token=target018_class visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=8f00d95d ws_popup=False ws_caption=True ws_child=False foreground_was_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=True on_bottom_left_work=True title_nonempty=True
2026-11-02T02:49:25.184Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=3 elapsed_ms=34 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T02:49:25.184Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=4 elapsed_ms=49 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T02:49:25.184Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=5 elapsed_ms=64 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T02:49:25.184Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=6 elapsed_ms=79 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T02:49:25.184Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=7 elapsed_ms=95 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T02:49:25.185Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=8 elapsed_ms=110 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T02:49:25.185Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=9 elapsed_ms=125 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T02:49:25.185Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=10 elapsed_ms=140 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T02:49:25.185Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=11 elapsed_ms=155 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T02:49:25.185Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=12 elapsed_ms=170 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T02:49:25.185Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=13 elapsed_ms=185 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T02:49:25.185Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=14 elapsed_ms=201 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T02:49:25.186Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=15 elapsed_ms=216 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T02:49:25.186Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=16 elapsed_ms=231 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T02:49:25.186Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=17 elapsed_ms=246 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T02:49:26.882Z INFO owned_target_preparation reason=Prepared
2026-11-02T02:49:26.902Z INFO owned_initial_focus phase=ready
2026-11-02T02:49:26.949Z INFO target_foreground_grant result=granted
2026-11-02T02:49:27.902Z INFO native_popup_focus reason=foreground_accepted
2026-11-02T02:49:27.903Z INFO candidate_popup_focus reason=accepted
2026-11-02T02:49:28.839Z INFO clipboard_swap attempts=1 status=Swapped saved=4 skipped_by_design=0 unsaved=0 cap_exceeded=False bytes=104 ms=239
2026-11-02T02:49:29.561Z INFO clipboard_restore status=Restored attempts=1
2026-11-02T02:49:29.562Z INFO insert_result outcome=Inserted id=1 notice=False ms=1584

```



Coordinator stdout:

```text

native_phase=guardian_ready
native_phase=awaiting_owned_a
native_result=posted-row_settled

```



target-thread-windows-ready.json, verbatim:

```json

{"phase":"ready","complete":true,"windows":[{"window_id":"ccb8ed07","is_a":false,"is_b":true,"new_at_refusal":false,"facts":{"class_token":"target018_class","visible":true,"toolwindow":false,"noactivate":false,"has_owner":false,"owner_is_a":false,"owner_is_b":false,"is_message_only":false,"same_thread_as_a":true,"cloaked":false,"cloaked_known":true,"class_sha8":"8f00d95d","ws_popup":false,"ws_caption":true,"ws_child":false,"foreground_was_child":false,"ws_disabled":false,"ws_ex_topmost":false,"ws_ex_layered":false,"ws_ex_transparent":false,"ws_ex_appwindow":false,"zero_area":false,"offscreen":false,"equals_a_rect":false,"on_bottom_left_work":true,"title_nonempty":true,"sampled":true}},{"window_id":"81d1c988","is_a":true,"is_b":false,"new_at_refusal":false,"facts":{"class_token":"target018_class","visible":true,"toolwindow":false,"noactivate":false,"has_owner":false,"owner_is_a":false,"owner_is_b":false,"is_message_only":false,"same_thread_as_a":true,"cloaked":false,"cloaked_known":true,"class_sha8":"8f00d95d","ws_popup":false,"ws_caption":true,"ws_child":false,"foreground_was_child":false,"ws_disabled":false,"ws_ex_topmost":false,"ws_ex_layered":false,"ws_ex_transparent":false,"ws_ex_appwindow":false,"zero_area":false,"offscreen":false,"equals_a_rect":true,"on_bottom_left_work":true,"title_nonempty":true,"sampled":true}},{"window_id":"545c5713","is_a":false,"is_b":false,"new_at_refusal":false,"facts":{"class_token":"ime_default","visible":false,"toolwindow":false,"noactivate":false,"has_owner":true,"owner_is_a":true,"owner_is_b":false,"is_message_only":false,"same_thread_as_a":true,"cloaked":false,"cloaked_known":true,"class_sha8":"29ee788e","ws_popup":true,"ws_caption":false,"ws_child":false,"foreground_was_child":false,"ws_disabled":true,"ws_ex_topmost":false,"ws_ex_layered":false,"ws_ex_transparent":false,"ws_ex_appwindow":false,"zero_area":true,"offscreen":true,"equals_a_rect":false,"on_bottom_left_work":false,"title_nonempty":true,"sampled":true}}]}

```



No native-monitor-failure.json or refusal inventory exists for this passed scene; no failure timeline/reason is fabricated.

### posted-main — PASS

Session: `ss-uno018-ui-b19cfec22eccfa86d6639f5d877b7098/ss-uno019-native-22c5196cd1154dd3a3ce6a37ca21ab2d`.

```json

{
  "failureStage": "none",
  "receiptHostCheck": "PASS",
  "hostShutdownComplete": true,
  "ownedRows": 1,
  "ownedUsage": 1,
  "independentUsageCheck": "PASS",
  "independentOuterBoundary": {
    "usageVerified": true,
    "clipboardEpochChanged": true
  },
  "outerClipboardEpochChanged": true,
  "monitorFailure": null,
  "privacy": "CLEAN",
  "negativeControls": "PASS",
  "stderrEmpty": true,
  "pendingReceipt": false
}

```



Full fixed startup/readiness/preparation/trace/grant/focus and selected boundary lines, verbatim:

```text

2026-11-02T02:49:38.128Z INFO native_startup_foreground phase=target_launch relation=foreign launcher_foreground=False sample_order=1 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=False cloaked=False cloaked_known=True class_sha8=827664e8 ws_popup=False ws_caption=True ws_child=False foreground_was_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=False title_nonempty=True
2026-11-02T02:49:38.128Z INFO native_startup_foreground phase=host_ready relation=foreign launcher_foreground=False sample_order=2 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=False cloaked=False cloaked_known=True class_sha8=827664e8 ws_popup=False ws_caption=True ws_child=False foreground_was_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=False title_nonempty=True
2026-11-02T02:49:38.128Z INFO native_host_readiness reason=ready
2026-11-02T02:49:38.434Z INFO owned_initial_focus phase=waiting
2026-11-02T02:49:38.443Z INFO native_startup_foreground phase=preparation_start relation=foreign launcher_foreground=False sample_order=3 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=False cloaked=False cloaked_known=True class_sha8=827664e8 ws_popup=False ws_caption=True ws_child=False foreground_was_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=False title_nonempty=True
2026-11-02T02:49:38.488Z INFO owned_preparation_admission reason=ready
2026-11-02T02:49:38.653Z INFO native_startup_foreground phase=before_focus relation=foreign launcher_foreground=False sample_order=4 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=False cloaked=False cloaked_known=True class_sha8=827664e8 ws_popup=False ws_caption=True ws_child=False foreground_was_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=False title_nonempty=True
2026-11-02T02:49:38.696Z INFO native_startup_foreground phase=after_focus relation=owned_a launcher_foreground=False sample_order=5 class_token=target018_class visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=e76f272c ws_popup=False ws_caption=True ws_child=False foreground_was_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=True on_bottom_left_work=True title_nonempty=True
2026-11-02T02:49:38.952Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=1 elapsed_ms=0 class_token=target018_class visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=e76f272c ws_popup=False ws_caption=True ws_child=False foreground_was_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=True on_bottom_left_work=True title_nonempty=True
2026-11-02T02:49:38.952Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=2 elapsed_ms=12 class_token=target018_class visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=e76f272c ws_popup=False ws_caption=True ws_child=False foreground_was_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=True on_bottom_left_work=True title_nonempty=True
2026-11-02T02:49:38.952Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=3 elapsed_ms=28 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T02:49:38.953Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=4 elapsed_ms=43 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T02:49:38.953Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=5 elapsed_ms=58 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T02:49:38.953Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=6 elapsed_ms=73 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T02:49:38.953Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=7 elapsed_ms=88 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T02:49:38.953Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=8 elapsed_ms=103 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T02:49:38.953Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=9 elapsed_ms=118 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T02:49:38.954Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=10 elapsed_ms=133 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T02:49:38.954Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=11 elapsed_ms=149 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T02:49:38.954Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=12 elapsed_ms=164 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T02:49:38.954Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=13 elapsed_ms=179 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T02:49:38.954Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=14 elapsed_ms=195 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T02:49:38.954Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=15 elapsed_ms=210 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T02:49:38.954Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=16 elapsed_ms=226 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T02:49:38.955Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=17 elapsed_ms=240 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T02:49:40.700Z INFO owned_target_preparation reason=Prepared
2026-11-02T02:49:40.722Z INFO owned_initial_focus phase=ready
2026-11-02T02:49:40.762Z INFO target_foreground_grant result=granted
2026-11-02T02:49:41.598Z INFO native_popup_focus reason=foreground_accepted
2026-11-02T02:49:41.598Z INFO candidate_popup_focus reason=accepted
2026-11-02T02:49:42.672Z INFO clipboard_swap attempts=1 status=Swapped saved=4 skipped_by_design=0 unsaved=0 cap_exceeded=False bytes=104 ms=264
2026-11-02T02:49:43.481Z INFO clipboard_restore status=Restored attempts=1
2026-11-02T02:49:43.481Z INFO insert_result outcome=Inserted id=1 notice=False ms=1807

```



Coordinator stdout:

```text

native_phase=guardian_ready
native_phase=awaiting_owned_a
native_result=posted-main_settled

```



target-thread-windows-ready.json, verbatim:

```json

{"phase":"ready","complete":true,"windows":[{"window_id":"3251efb6","is_a":false,"is_b":true,"new_at_refusal":false,"facts":{"class_token":"target018_class","visible":true,"toolwindow":false,"noactivate":false,"has_owner":false,"owner_is_a":false,"owner_is_b":false,"is_message_only":false,"same_thread_as_a":true,"cloaked":false,"cloaked_known":true,"class_sha8":"e76f272c","ws_popup":false,"ws_caption":true,"ws_child":false,"foreground_was_child":false,"ws_disabled":false,"ws_ex_topmost":false,"ws_ex_layered":false,"ws_ex_transparent":false,"ws_ex_appwindow":false,"zero_area":false,"offscreen":false,"equals_a_rect":false,"on_bottom_left_work":true,"title_nonempty":true,"sampled":true}},{"window_id":"8137d198","is_a":true,"is_b":false,"new_at_refusal":false,"facts":{"class_token":"target018_class","visible":true,"toolwindow":false,"noactivate":false,"has_owner":false,"owner_is_a":false,"owner_is_b":false,"is_message_only":false,"same_thread_as_a":true,"cloaked":false,"cloaked_known":true,"class_sha8":"e76f272c","ws_popup":false,"ws_caption":true,"ws_child":false,"foreground_was_child":false,"ws_disabled":false,"ws_ex_topmost":false,"ws_ex_layered":false,"ws_ex_transparent":false,"ws_ex_appwindow":false,"zero_area":false,"offscreen":false,"equals_a_rect":true,"on_bottom_left_work":true,"title_nonempty":true,"sampled":true}},{"window_id":"8515da57","is_a":false,"is_b":false,"new_at_refusal":false,"facts":{"class_token":"ime_default","visible":false,"toolwindow":false,"noactivate":false,"has_owner":true,"owner_is_a":true,"owner_is_b":false,"is_message_only":false,"same_thread_as_a":true,"cloaked":false,"cloaked_known":true,"class_sha8":"29ee788e","ws_popup":true,"ws_caption":false,"ws_child":false,"foreground_was_child":false,"ws_disabled":true,"ws_ex_topmost":false,"ws_ex_layered":false,"ws_ex_transparent":false,"ws_ex_appwindow":false,"zero_area":true,"offscreen":true,"equals_a_rect":false,"on_bottom_left_work":false,"title_nonempty":true,"sampled":true}}]}

```



No native-monitor-failure.json or refusal inventory exists for this passed scene; no failure timeline/reason is fabricated.

### posted-enter — NOT RUN

NOT RUN: excluded SendInput Enter. No native session/usage/epoch/monitor observation.

### popup-copy — PASS

Session: `ss-uno018-ui-d01e7167754837fb4dda8ae11716d8fa/ss-uno019-native-72276ddbf61d459daff3d313b0d921a4`.

```json

{
  "failureStage": "none",
  "receiptHostCheck": "PASS",
  "hostShutdownComplete": true,
  "ownedRows": 1,
  "ownedUsage": 0,
  "independentUsageCheck": "PASS",
  "independentOuterBoundary": {
    "usageVerified": true,
    "clipboardEpochChanged": true
  },
  "outerClipboardEpochChanged": true,
  "monitorFailure": null,
  "privacy": "CLEAN",
  "negativeControls": "PASS",
  "stderrEmpty": true,
  "pendingReceipt": false
}

```



Full fixed startup/readiness/preparation/trace/grant/focus and selected boundary lines, verbatim:

```text

2026-11-02T02:49:52.019Z INFO native_startup_foreground phase=target_launch relation=foreign launcher_foreground=False sample_order=1 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=False cloaked=False cloaked_known=True class_sha8=827664e8 ws_popup=False ws_caption=True ws_child=False foreground_was_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=False title_nonempty=True
2026-11-02T02:49:52.022Z INFO native_startup_foreground phase=host_ready relation=foreign launcher_foreground=False sample_order=2 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=False cloaked=False cloaked_known=True class_sha8=827664e8 ws_popup=False ws_caption=True ws_child=False foreground_was_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=False title_nonempty=True
2026-11-02T02:49:52.022Z INFO native_host_readiness reason=ready
2026-11-02T02:49:52.436Z INFO owned_initial_focus phase=waiting
2026-11-02T02:49:52.437Z INFO native_startup_foreground phase=preparation_start relation=foreign launcher_foreground=False sample_order=3 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=False cloaked=False cloaked_known=True class_sha8=827664e8 ws_popup=False ws_caption=True ws_child=False foreground_was_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=False title_nonempty=True
2026-11-02T02:49:52.480Z INFO owned_preparation_admission reason=ready
2026-11-02T02:49:52.623Z INFO native_startup_foreground phase=before_focus relation=foreign launcher_foreground=False sample_order=4 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=False cloaked=False cloaked_known=True class_sha8=827664e8 ws_popup=False ws_caption=True ws_child=False foreground_was_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=False title_nonempty=True
2026-11-02T02:49:52.661Z INFO native_startup_foreground phase=after_focus relation=owned_a launcher_foreground=False sample_order=5 class_token=target018_class visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=26d9fa97 ws_popup=False ws_caption=True ws_child=False foreground_was_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=True on_bottom_left_work=True title_nonempty=True
2026-11-02T02:49:52.915Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=1 elapsed_ms=0 class_token=target018_class visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=26d9fa97 ws_popup=False ws_caption=True ws_child=False foreground_was_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=True on_bottom_left_work=True title_nonempty=True
2026-11-02T02:49:52.915Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=2 elapsed_ms=25 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T02:49:52.915Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=3 elapsed_ms=40 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T02:49:52.916Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=4 elapsed_ms=55 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T02:49:52.916Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=5 elapsed_ms=70 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T02:49:52.916Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=6 elapsed_ms=85 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T02:49:52.916Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=7 elapsed_ms=100 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T02:49:52.916Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=8 elapsed_ms=116 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T02:49:52.916Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=9 elapsed_ms=131 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T02:49:52.916Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=10 elapsed_ms=147 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T02:49:52.916Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=11 elapsed_ms=162 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T02:49:52.916Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=12 elapsed_ms=177 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T02:49:52.916Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=13 elapsed_ms=193 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T02:49:52.916Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=14 elapsed_ms=209 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T02:49:52.916Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=15 elapsed_ms=224 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T02:49:52.916Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=16 elapsed_ms=239 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T02:49:54.658Z INFO owned_target_preparation reason=Prepared
2026-11-02T02:49:54.689Z INFO owned_initial_focus phase=ready
2026-11-02T02:49:54.759Z INFO target_foreground_grant result=granted
2026-11-02T02:49:56.033Z INFO native_popup_focus reason=foreground_accepted
2026-11-02T02:49:56.033Z INFO candidate_popup_focus reason=accepted

```



Coordinator stdout:

```text

native_phase=guardian_ready
native_phase=awaiting_owned_a
native_result=popup-copy_settled

```



target-thread-windows-ready.json, verbatim:

```json

{"phase":"ready","complete":true,"windows":[{"window_id":"5f31bbb0","is_a":false,"is_b":true,"new_at_refusal":false,"facts":{"class_token":"target018_class","visible":true,"toolwindow":false,"noactivate":false,"has_owner":false,"owner_is_a":false,"owner_is_b":false,"is_message_only":false,"same_thread_as_a":true,"cloaked":false,"cloaked_known":true,"class_sha8":"26d9fa97","ws_popup":false,"ws_caption":true,"ws_child":false,"foreground_was_child":false,"ws_disabled":false,"ws_ex_topmost":false,"ws_ex_layered":false,"ws_ex_transparent":false,"ws_ex_appwindow":false,"zero_area":false,"offscreen":false,"equals_a_rect":false,"on_bottom_left_work":true,"title_nonempty":true,"sampled":true}},{"window_id":"62892df9","is_a":true,"is_b":false,"new_at_refusal":false,"facts":{"class_token":"target018_class","visible":true,"toolwindow":false,"noactivate":false,"has_owner":false,"owner_is_a":false,"owner_is_b":false,"is_message_only":false,"same_thread_as_a":true,"cloaked":false,"cloaked_known":true,"class_sha8":"26d9fa97","ws_popup":false,"ws_caption":true,"ws_child":false,"foreground_was_child":false,"ws_disabled":false,"ws_ex_topmost":false,"ws_ex_layered":false,"ws_ex_transparent":false,"ws_ex_appwindow":false,"zero_area":false,"offscreen":false,"equals_a_rect":true,"on_bottom_left_work":true,"title_nonempty":true,"sampled":true}},{"window_id":"2c4dd5c2","is_a":false,"is_b":false,"new_at_refusal":false,"facts":{"class_token":"ime_default","visible":false,"toolwindow":false,"noactivate":false,"has_owner":true,"owner_is_a":true,"owner_is_b":false,"is_message_only":false,"same_thread_as_a":true,"cloaked":false,"cloaked_known":true,"class_sha8":"29ee788e","ws_popup":true,"ws_caption":false,"ws_child":false,"foreground_was_child":false,"ws_disabled":true,"ws_ex_topmost":false,"ws_ex_layered":false,"ws_ex_transparent":false,"ws_ex_appwindow":false,"zero_area":true,"offscreen":true,"equals_a_rect":false,"on_bottom_left_work":false,"title_nonempty":true,"sampled":true}}]}

```



No native-monitor-failure.json or refusal inventory exists for this passed scene; no failure timeline/reason is fabricated.

### editor-copy — PASS

Session: `ss-uno018-ui-4a2fb88a4cef17a32a01c82f2abe8004/ss-uno019-native-7d80810adc8b4540bd780887bf1dfa97`.

```json

{
  "failureStage": "none",
  "receiptHostCheck": "PASS",
  "hostShutdownComplete": true,
  "ownedRows": 1,
  "ownedUsage": 0,
  "independentUsageCheck": "PASS",
  "independentOuterBoundary": {
    "usageVerified": true,
    "clipboardEpochChanged": true
  },
  "outerClipboardEpochChanged": true,
  "monitorFailure": null,
  "privacy": "CLEAN",
  "negativeControls": "PASS",
  "stderrEmpty": true,
  "pendingReceipt": false
}

```



Full fixed startup/readiness/preparation/trace/grant/focus and selected boundary lines, verbatim:

```text

2026-11-02T02:50:05.101Z INFO native_startup_foreground phase=target_launch relation=foreign launcher_foreground=False sample_order=1 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=False cloaked=False cloaked_known=True class_sha8=827664e8 ws_popup=False ws_caption=True ws_child=False foreground_was_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=False title_nonempty=True
2026-11-02T02:50:05.103Z INFO native_startup_foreground phase=host_ready relation=foreign launcher_foreground=False sample_order=2 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=False cloaked=False cloaked_known=True class_sha8=827664e8 ws_popup=False ws_caption=True ws_child=False foreground_was_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=False title_nonempty=True
2026-11-02T02:50:05.104Z INFO native_host_readiness reason=ready
2026-11-02T02:50:05.599Z INFO owned_initial_focus phase=waiting
2026-11-02T02:50:05.599Z INFO native_startup_foreground phase=preparation_start relation=foreign launcher_foreground=False sample_order=3 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=False cloaked=False cloaked_known=True class_sha8=827664e8 ws_popup=False ws_caption=True ws_child=False foreground_was_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=False title_nonempty=True
2026-11-02T02:50:05.642Z INFO owned_preparation_admission reason=ready
2026-11-02T02:50:05.803Z INFO native_startup_foreground phase=before_focus relation=foreign launcher_foreground=False sample_order=4 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=False cloaked=False cloaked_known=True class_sha8=827664e8 ws_popup=False ws_caption=True ws_child=False foreground_was_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=False title_nonempty=True
2026-11-02T02:50:05.849Z INFO native_startup_foreground phase=after_focus relation=owned_a launcher_foreground=False sample_order=5 class_token=target018_class visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=9342d26f ws_popup=False ws_caption=True ws_child=False foreground_was_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=True on_bottom_left_work=True title_nonempty=True
2026-11-02T02:50:06.100Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=1 elapsed_ms=0 class_token=target018_class visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=9342d26f ws_popup=False ws_caption=True ws_child=False foreground_was_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=True on_bottom_left_work=True title_nonempty=True
2026-11-02T02:50:06.101Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=2 elapsed_ms=20 class_token=target018_class visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=9342d26f ws_popup=False ws_caption=True ws_child=False foreground_was_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=True on_bottom_left_work=True title_nonempty=True
2026-11-02T02:50:06.101Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=3 elapsed_ms=35 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T02:50:06.101Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=4 elapsed_ms=49 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T02:50:06.101Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=5 elapsed_ms=65 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T02:50:06.101Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=6 elapsed_ms=80 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T02:50:06.101Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=7 elapsed_ms=96 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T02:50:06.102Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=8 elapsed_ms=111 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T02:50:06.102Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=9 elapsed_ms=126 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T02:50:06.102Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=10 elapsed_ms=141 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T02:50:06.102Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=11 elapsed_ms=156 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T02:50:06.102Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=12 elapsed_ms=171 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T02:50:06.102Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=13 elapsed_ms=188 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T02:50:06.103Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=14 elapsed_ms=203 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T02:50:06.103Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=15 elapsed_ms=219 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T02:50:06.103Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=16 elapsed_ms=235 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T02:50:06.103Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=17 elapsed_ms=250 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T02:50:07.782Z INFO owned_target_preparation reason=Prepared
2026-11-02T02:50:07.802Z INFO owned_initial_focus phase=ready
2026-11-02T02:50:07.841Z INFO target_foreground_grant result=granted
2026-11-02T02:50:09.562Z INFO native_popup_focus reason=foreground_accepted
2026-11-02T02:50:09.562Z INFO candidate_popup_focus reason=accepted
2026-11-02T02:50:09.826Z ERROR store_write_failed op=update_snippet error=SqliteException sqlite=19 hresult=0x80004005
2026-11-02T02:50:10.769Z INFO native_popup_focus reason=already_popup
2026-11-02T02:50:10.769Z INFO candidate_popup_focus reason=accepted

```



Coordinator stdout:

```text

native_phase=guardian_ready
native_phase=awaiting_owned_a
native_result=editor-copy_settled

```



target-thread-windows-ready.json, verbatim:

```json

{"phase":"ready","complete":true,"windows":[{"window_id":"4f8da597","is_a":false,"is_b":true,"new_at_refusal":false,"facts":{"class_token":"target018_class","visible":true,"toolwindow":false,"noactivate":false,"has_owner":false,"owner_is_a":false,"owner_is_b":false,"is_message_only":false,"same_thread_as_a":true,"cloaked":false,"cloaked_known":true,"class_sha8":"9342d26f","ws_popup":false,"ws_caption":true,"ws_child":false,"foreground_was_child":false,"ws_disabled":false,"ws_ex_topmost":false,"ws_ex_layered":false,"ws_ex_transparent":false,"ws_ex_appwindow":false,"zero_area":false,"offscreen":false,"equals_a_rect":false,"on_bottom_left_work":true,"title_nonempty":true,"sampled":true}},{"window_id":"c95e8405","is_a":true,"is_b":false,"new_at_refusal":false,"facts":{"class_token":"target018_class","visible":true,"toolwindow":false,"noactivate":false,"has_owner":false,"owner_is_a":false,"owner_is_b":false,"is_message_only":false,"same_thread_as_a":true,"cloaked":false,"cloaked_known":true,"class_sha8":"9342d26f","ws_popup":false,"ws_caption":true,"ws_child":false,"foreground_was_child":false,"ws_disabled":false,"ws_ex_topmost":false,"ws_ex_layered":false,"ws_ex_transparent":false,"ws_ex_appwindow":false,"zero_area":false,"offscreen":false,"equals_a_rect":true,"on_bottom_left_work":true,"title_nonempty":true,"sampled":true}},{"window_id":"bf773ada","is_a":false,"is_b":false,"new_at_refusal":false,"facts":{"class_token":"ime_default","visible":false,"toolwindow":false,"noactivate":false,"has_owner":true,"owner_is_a":true,"owner_is_b":false,"is_message_only":false,"same_thread_as_a":true,"cloaked":false,"cloaked_known":true,"class_sha8":"29ee788e","ws_popup":true,"ws_caption":false,"ws_child":false,"foreground_was_child":false,"ws_disabled":true,"ws_ex_topmost":false,"ws_ex_layered":false,"ws_ex_transparent":false,"ws_ex_appwindow":false,"zero_area":true,"offscreen":true,"equals_a_rect":false,"on_bottom_left_work":false,"title_nonempty":true,"sampled":true}}]}

```



No native-monitor-failure.json or refusal inventory exists for this passed scene; no failure timeline/reason is fabricated.

### footer-copy — PASS

Session: `ss-uno018-ui-c2a4ba6ae02eac9149df49caa461ba21/ss-uno019-native-f27eacd3c4724fc2a8e779fafb87d225`.

```json

{
  "failureStage": "none",
  "receiptHostCheck": "PASS",
  "hostShutdownComplete": true,
  "ownedRows": 1,
  "ownedUsage": 0,
  "independentUsageCheck": "PASS",
  "independentOuterBoundary": {
    "usageVerified": true,
    "clipboardEpochChanged": true
  },
  "outerClipboardEpochChanged": true,
  "monitorFailure": null,
  "privacy": "CLEAN",
  "negativeControls": "PASS",
  "stderrEmpty": true,
  "pendingReceipt": false
}

```



Full fixed startup/readiness/preparation/trace/grant/focus and selected boundary lines, verbatim:

```text

2026-11-02T02:50:19.056Z INFO native_startup_foreground phase=target_launch relation=foreign launcher_foreground=False sample_order=1 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=False cloaked=False cloaked_known=True class_sha8=827664e8 ws_popup=False ws_caption=True ws_child=False foreground_was_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=False title_nonempty=True
2026-11-02T02:50:19.056Z INFO native_startup_foreground phase=host_ready relation=foreign launcher_foreground=False sample_order=2 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=False cloaked=False cloaked_known=True class_sha8=827664e8 ws_popup=False ws_caption=True ws_child=False foreground_was_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=False title_nonempty=True
2026-11-02T02:50:19.057Z INFO native_host_readiness reason=ready
2026-11-02T02:50:19.489Z INFO owned_initial_focus phase=waiting
2026-11-02T02:50:19.490Z INFO native_startup_foreground phase=preparation_start relation=foreign launcher_foreground=False sample_order=3 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=False cloaked=False cloaked_known=True class_sha8=827664e8 ws_popup=False ws_caption=True ws_child=False foreground_was_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=False title_nonempty=True
2026-11-02T02:50:19.535Z INFO owned_preparation_admission reason=ready
2026-11-02T02:50:19.679Z INFO native_startup_foreground phase=before_focus relation=foreign launcher_foreground=False sample_order=4 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=False cloaked=False cloaked_known=True class_sha8=827664e8 ws_popup=False ws_caption=True ws_child=False foreground_was_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=False title_nonempty=True
2026-11-02T02:50:19.713Z INFO native_startup_foreground phase=after_focus relation=owned_a launcher_foreground=False sample_order=5 class_token=target018_class visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=0a12fbc1 ws_popup=False ws_caption=True ws_child=False foreground_was_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=True on_bottom_left_work=True title_nonempty=True
2026-11-02T02:50:19.968Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=1 elapsed_ms=0 class_token=target018_class visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=0a12fbc1 ws_popup=False ws_caption=True ws_child=False foreground_was_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=True on_bottom_left_work=True title_nonempty=True
2026-11-02T02:50:19.969Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=2 elapsed_ms=23 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T02:50:19.969Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=3 elapsed_ms=38 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T02:50:19.969Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=4 elapsed_ms=69 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T02:50:19.969Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=5 elapsed_ms=85 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T02:50:19.969Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=6 elapsed_ms=101 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T02:50:19.969Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=7 elapsed_ms=116 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T02:50:19.969Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=8 elapsed_ms=131 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T02:50:19.970Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=9 elapsed_ms=146 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T02:50:19.970Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=10 elapsed_ms=162 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T02:50:19.970Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=11 elapsed_ms=177 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T02:50:19.970Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=12 elapsed_ms=193 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T02:50:19.970Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=13 elapsed_ms=208 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T02:50:19.970Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=14 elapsed_ms=223 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T02:50:19.971Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=15 elapsed_ms=239 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T02:50:21.704Z INFO owned_target_preparation reason=Prepared
2026-11-02T02:50:21.725Z INFO owned_initial_focus phase=ready
2026-11-02T02:50:21.762Z INFO target_foreground_grant result=granted
2026-11-02T02:50:23.414Z INFO native_popup_focus reason=foreground_accepted
2026-11-02T02:50:23.415Z INFO candidate_popup_focus reason=accepted

```



Coordinator stdout:

```text

native_phase=guardian_ready
native_phase=awaiting_owned_a
native_result=footer-copy_settled

```



target-thread-windows-ready.json, verbatim:

```json

{"phase":"ready","complete":true,"windows":[{"window_id":"1047f0d3","is_a":false,"is_b":true,"new_at_refusal":false,"facts":{"class_token":"target018_class","visible":true,"toolwindow":false,"noactivate":false,"has_owner":false,"owner_is_a":false,"owner_is_b":false,"is_message_only":false,"same_thread_as_a":true,"cloaked":false,"cloaked_known":true,"class_sha8":"0a12fbc1","ws_popup":false,"ws_caption":true,"ws_child":false,"foreground_was_child":false,"ws_disabled":false,"ws_ex_topmost":false,"ws_ex_layered":false,"ws_ex_transparent":false,"ws_ex_appwindow":false,"zero_area":false,"offscreen":false,"equals_a_rect":false,"on_bottom_left_work":true,"title_nonempty":true,"sampled":true}},{"window_id":"5319cfa4","is_a":true,"is_b":false,"new_at_refusal":false,"facts":{"class_token":"target018_class","visible":true,"toolwindow":false,"noactivate":false,"has_owner":false,"owner_is_a":false,"owner_is_b":false,"is_message_only":false,"same_thread_as_a":true,"cloaked":false,"cloaked_known":true,"class_sha8":"0a12fbc1","ws_popup":false,"ws_caption":true,"ws_child":false,"foreground_was_child":false,"ws_disabled":false,"ws_ex_topmost":false,"ws_ex_layered":false,"ws_ex_transparent":false,"ws_ex_appwindow":false,"zero_area":false,"offscreen":false,"equals_a_rect":true,"on_bottom_left_work":true,"title_nonempty":true,"sampled":true}},{"window_id":"3ede6f29","is_a":false,"is_b":false,"new_at_refusal":false,"facts":{"class_token":"ime_default","visible":false,"toolwindow":false,"noactivate":false,"has_owner":true,"owner_is_a":true,"owner_is_b":false,"is_message_only":false,"same_thread_as_a":true,"cloaked":false,"cloaked_known":true,"class_sha8":"29ee788e","ws_popup":true,"ws_caption":false,"ws_child":false,"foreground_was_child":false,"ws_disabled":true,"ws_ex_topmost":false,"ws_ex_layered":false,"ws_ex_transparent":false,"ws_ex_appwindow":false,"zero_area":true,"offscreen":true,"equals_a_rect":false,"on_bottom_left_work":false,"title_nonempty":true,"sampled":true}}]}

```



No native-monitor-failure.json or refusal inventory exists for this passed scene; no failure timeline/reason is fabricated.

### context-insert — PASS

Session: `ss-uno018-ui-23e18c0b7b962320e0e0129e4a65a9d3/ss-uno019-native-bc6dccf6bd5f44519c987af4f58b24da`.

```json

{
  "failureStage": "none",
  "receiptHostCheck": "PASS",
  "hostShutdownComplete": true,
  "ownedRows": 1,
  "ownedUsage": 1,
  "independentUsageCheck": "PASS",
  "independentOuterBoundary": {
    "usageVerified": true,
    "clipboardEpochChanged": true
  },
  "outerClipboardEpochChanged": true,
  "monitorFailure": null,
  "privacy": "CLEAN",
  "negativeControls": "PASS",
  "stderrEmpty": true,
  "pendingReceipt": false
}

```



Full fixed startup/readiness/preparation/trace/grant/focus and selected boundary lines, verbatim:

```text

2026-11-02T02:50:33.930Z INFO native_startup_foreground phase=target_launch relation=foreign launcher_foreground=False sample_order=1 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=False cloaked=False cloaked_known=True class_sha8=827664e8 ws_popup=False ws_caption=True ws_child=False foreground_was_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=False title_nonempty=True
2026-11-02T02:50:33.934Z INFO native_startup_foreground phase=host_ready relation=foreign launcher_foreground=False sample_order=2 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=False cloaked=False cloaked_known=True class_sha8=827664e8 ws_popup=False ws_caption=True ws_child=False foreground_was_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=False title_nonempty=True
2026-11-02T02:50:33.935Z INFO native_host_readiness reason=ready
2026-11-02T02:50:34.246Z INFO owned_initial_focus phase=waiting
2026-11-02T02:50:34.247Z INFO native_startup_foreground phase=preparation_start relation=foreign launcher_foreground=False sample_order=3 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=False cloaked=False cloaked_known=True class_sha8=827664e8 ws_popup=False ws_caption=True ws_child=False foreground_was_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=False title_nonempty=True
2026-11-02T02:50:34.305Z INFO owned_preparation_admission reason=ready
2026-11-02T02:50:34.484Z INFO native_startup_foreground phase=before_focus relation=foreign launcher_foreground=False sample_order=4 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=False cloaked=False cloaked_known=True class_sha8=827664e8 ws_popup=False ws_caption=True ws_child=False foreground_was_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=False title_nonempty=True
2026-11-02T02:50:34.532Z INFO native_startup_foreground phase=after_focus relation=owned_a launcher_foreground=False sample_order=5 class_token=target018_class visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=3323917a ws_popup=False ws_caption=True ws_child=False foreground_was_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=True on_bottom_left_work=True title_nonempty=True
2026-11-02T02:50:34.786Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=1 elapsed_ms=0 class_token=target018_class visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=3323917a ws_popup=False ws_caption=True ws_child=False foreground_was_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=True on_bottom_left_work=True title_nonempty=True
2026-11-02T02:50:34.786Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=2 elapsed_ms=25 class_token=target018_class visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=3323917a ws_popup=False ws_caption=True ws_child=False foreground_was_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=True on_bottom_left_work=True title_nonempty=True
2026-11-02T02:50:34.786Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=3 elapsed_ms=40 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T02:50:34.786Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=4 elapsed_ms=71 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T02:50:34.786Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=5 elapsed_ms=101 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T02:50:34.787Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=6 elapsed_ms=116 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T02:50:34.787Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=7 elapsed_ms=131 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T02:50:34.787Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=8 elapsed_ms=146 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T02:50:34.787Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=9 elapsed_ms=161 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T02:50:34.787Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=10 elapsed_ms=176 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T02:50:34.787Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=11 elapsed_ms=192 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T02:50:34.787Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=12 elapsed_ms=208 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T02:50:34.787Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=13 elapsed_ms=223 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T02:50:34.787Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=14 elapsed_ms=238 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True foreground_was_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T02:50:36.534Z INFO owned_target_preparation reason=Prepared
2026-11-02T02:50:36.556Z INFO owned_initial_focus phase=ready
2026-11-02T02:50:36.599Z INFO target_foreground_grant result=granted
2026-11-02T02:50:38.004Z INFO native_popup_focus reason=foreground_accepted
2026-11-02T02:50:38.004Z INFO candidate_popup_focus reason=accepted
2026-11-02T02:50:39.095Z INFO clipboard_swap attempts=1 status=Swapped saved=4 skipped_by_design=0 unsaved=0 cap_exceeded=False bytes=104 ms=300
2026-11-02T02:50:39.830Z INFO clipboard_restore status=Restored attempts=1
2026-11-02T02:50:39.831Z INFO insert_result outcome=Inserted id=1 notice=False ms=1750

```



Coordinator stdout:

```text

native_phase=guardian_ready
native_phase=awaiting_owned_a
native_result=context-insert_settled

```



target-thread-windows-ready.json, verbatim:

```json

{"phase":"ready","complete":true,"windows":[{"window_id":"370b6b5e","is_a":false,"is_b":true,"new_at_refusal":false,"facts":{"class_token":"target018_class","visible":true,"toolwindow":false,"noactivate":false,"has_owner":false,"owner_is_a":false,"owner_is_b":false,"is_message_only":false,"same_thread_as_a":true,"cloaked":false,"cloaked_known":true,"class_sha8":"3323917a","ws_popup":false,"ws_caption":true,"ws_child":false,"foreground_was_child":false,"ws_disabled":false,"ws_ex_topmost":false,"ws_ex_layered":false,"ws_ex_transparent":false,"ws_ex_appwindow":false,"zero_area":false,"offscreen":false,"equals_a_rect":false,"on_bottom_left_work":true,"title_nonempty":true,"sampled":true}},{"window_id":"3b32b20a","is_a":true,"is_b":false,"new_at_refusal":false,"facts":{"class_token":"target018_class","visible":true,"toolwindow":false,"noactivate":false,"has_owner":false,"owner_is_a":false,"owner_is_b":false,"is_message_only":false,"same_thread_as_a":true,"cloaked":false,"cloaked_known":true,"class_sha8":"3323917a","ws_popup":false,"ws_caption":true,"ws_child":false,"foreground_was_child":false,"ws_disabled":false,"ws_ex_topmost":false,"ws_ex_layered":false,"ws_ex_transparent":false,"ws_ex_appwindow":false,"zero_area":false,"offscreen":false,"equals_a_rect":true,"on_bottom_left_work":true,"title_nonempty":true,"sampled":true}},{"window_id":"c2d30701","is_a":false,"is_b":false,"new_at_refusal":false,"facts":{"class_token":"ime_default","visible":false,"toolwindow":false,"noactivate":false,"has_owner":true,"owner_is_a":true,"owner_is_b":false,"is_message_only":false,"same_thread_as_a":true,"cloaked":false,"cloaked_known":true,"class_sha8":"29ee788e","ws_popup":true,"ws_caption":false,"ws_child":false,"foreground_was_child":false,"ws_disabled":true,"ws_ex_topmost":false,"ws_ex_layered":false,"ws_ex_transparent":false,"ws_ex_appwindow":false,"zero_area":true,"offscreen":true,"equals_a_rect":false,"on_bottom_left_work":false,"title_nonempty":true,"sampled":true}}]}

```



No native-monitor-failure.json or refusal inventory exists for this passed scene; no failure timeline/reason is fabricated.

## Picker Import Cancel — FAIL, no retry

Picker parent `ss-uno018-ui-7d88603fe76867ee6ee05da4dbac6dad`; session `ss-uno018-ui-87eb807efbc30307c111328b1cf7c76f`. Started last at 20:50:44 local, more than 22 minutes before hard end, satisfying the ≥5-minute rule. Wrapper/child exit 1. Worker reason=input, inputSubreason=epoch_changed; parent reason=worker_protocol. operationClass, pickerStep and HRESULT are absent. No picker_schema_multiple/per-match schema record exists. Helper stdout/stderr empty.

**inputRefusalElapsedMs=1685; inputSampleCount=29; inputEpochDelta=8969; inputIdleMs=63; inputPhase=unspecified.** InputPhase unspecified must not be rewritten as an observed picker dialog phase.

All 29 per-poll samples report any_input_down=false, owned_dialog_present=false and dialog_foreground=false. The first 28 have epoch_changed=false; sample 29 (1685 ms) reports epoch_changed=true, idle_ms=47. The separate refusal read reports inputIdleMs=63; these are separate sampling instants, not contradictory event counts. No owned dialog was observed by these samples, not proof that none existed between polls.

The helper open Invoke completed: before=390517093, after=390526062. Its import-cancel timeline was unchanged at before_main_invoke (326 ms), changed at after_main_invoke (526 ms), then guard_refused (537 ms). Thus the timestamp change is temporally bracketed by the main Import Invoke, unlike nr13's missing after record. This is not proof of who generated input or permission for an epoch exception. No rebaseline or guard relaxation follows. Helper and observer elapsed clocks have different origins; do not merge them into one atomic timeline.

Every pickerEpochTimeline row (including initialization-only buckets), every pickerInputSamples row, the complete fixed worker failure and the open Invoke record:

```json

{
  "observer-parent-failure.json": {
    "reason": "worker_protocol"
  },
  "observer-worker-failure.json": {
    "reason": "input",
    "inputSubreason": "epoch_changed",
    "inputEpochDelta": 8969,
    "inputIdleMs": 63,
    "inputPhase": "unspecified",
    "inputRefusalElapsedMs": 1685,
    "inputSampleCount": 29,
    "pickerInputSamples": [
      {
        "elapsed_ms": 99,
        "epoch_changed": false,
        "idle_ms": 7453,
        "owned_dialog_present": false,
        "dialog_foreground": false,
        "any_input_down": false
      },
      {
        "elapsed_ms": 153,
        "epoch_changed": false,
        "idle_ms": 7500,
        "owned_dialog_present": false,
        "dialog_foreground": false,
        "any_input_down": false
      },
      {
        "elapsed_ms": 210,
        "epoch_changed": false,
        "idle_ms": 7563,
        "owned_dialog_present": false,
        "dialog_foreground": false,
        "any_input_down": false
      },
      {
        "elapsed_ms": 281,
        "epoch_changed": false,
        "idle_ms": 7610,
        "owned_dialog_present": false,
        "dialog_foreground": false,
        "any_input_down": false
      },
      {
        "elapsed_ms": 350,
        "epoch_changed": false,
        "idle_ms": 7703,
        "owned_dialog_present": false,
        "dialog_foreground": false,
        "any_input_down": false
      },
      {
        "elapsed_ms": 404,
        "epoch_changed": false,
        "idle_ms": 7750,
        "owned_dialog_present": false,
        "dialog_foreground": false,
        "any_input_down": false
      },
      {
        "elapsed_ms": 458,
        "epoch_changed": false,
        "idle_ms": 7813,
        "owned_dialog_present": false,
        "dialog_foreground": false,
        "any_input_down": false
      },
      {
        "elapsed_ms": 512,
        "epoch_changed": false,
        "idle_ms": 7860,
        "owned_dialog_present": false,
        "dialog_foreground": false,
        "any_input_down": false
      },
      {
        "elapsed_ms": 566,
        "epoch_changed": false,
        "idle_ms": 7907,
        "owned_dialog_present": false,
        "dialog_foreground": false,
        "any_input_down": false
      },
      {
        "elapsed_ms": 623,
        "epoch_changed": false,
        "idle_ms": 7969,
        "owned_dialog_present": false,
        "dialog_foreground": false,
        "any_input_down": false
      },
      {
        "elapsed_ms": 680,
        "epoch_changed": false,
        "idle_ms": 8032,
        "owned_dialog_present": false,
        "dialog_foreground": false,
        "any_input_down": false
      },
      {
        "elapsed_ms": 733,
        "epoch_changed": false,
        "idle_ms": 8078,
        "owned_dialog_present": false,
        "dialog_foreground": false,
        "any_input_down": false
      },
      {
        "elapsed_ms": 786,
        "epoch_changed": false,
        "idle_ms": 8141,
        "owned_dialog_present": false,
        "dialog_foreground": false,
        "any_input_down": false
      },
      {
        "elapsed_ms": 840,
        "epoch_changed": false,
        "idle_ms": 8188,
        "owned_dialog_present": false,
        "dialog_foreground": false,
        "any_input_down": false
      },
      {
        "elapsed_ms": 893,
        "epoch_changed": false,
        "idle_ms": 8235,
        "owned_dialog_present": false,
        "dialog_foreground": false,
        "any_input_down": false
      },
      {
        "elapsed_ms": 946,
        "epoch_changed": false,
        "idle_ms": 8297,
        "owned_dialog_present": false,
        "dialog_foreground": false,
        "any_input_down": false
      },
      {
        "elapsed_ms": 1000,
        "epoch_changed": false,
        "idle_ms": 8344,
        "owned_dialog_present": false,
        "dialog_foreground": false,
        "any_input_down": false
      },
      {
        "elapsed_ms": 1055,
        "epoch_changed": false,
        "idle_ms": 8407,
        "owned_dialog_present": false,
        "dialog_foreground": false,
        "any_input_down": false
      },
      {
        "elapsed_ms": 1108,
        "epoch_changed": false,
        "idle_ms": 8453,
        "owned_dialog_present": false,
        "dialog_foreground": false,
        "any_input_down": false
      },
      {
        "elapsed_ms": 1161,
        "epoch_changed": false,
        "idle_ms": 8516,
        "owned_dialog_present": false,
        "dialog_foreground": false,
        "any_input_down": false
      },
      {
        "elapsed_ms": 1217,
        "epoch_changed": false,
        "idle_ms": 8563,
        "owned_dialog_present": false,
        "dialog_foreground": false,
        "any_input_down": false
      },
      {
        "elapsed_ms": 1272,
        "epoch_changed": false,
        "idle_ms": 8625,
        "owned_dialog_present": false,
        "dialog_foreground": false,
        "any_input_down": false
      },
      {
        "elapsed_ms": 1326,
        "epoch_changed": false,
        "idle_ms": 8672,
        "owned_dialog_present": false,
        "dialog_foreground": false,
        "any_input_down": false
      },
      {
        "elapsed_ms": 1380,
        "epoch_changed": false,
        "idle_ms": 8735,
        "owned_dialog_present": false,
        "dialog_foreground": false,
        "any_input_down": false
      },
      {
        "elapsed_ms": 1435,
        "epoch_changed": false,
        "idle_ms": 8782,
        "owned_dialog_present": false,
        "dialog_foreground": false,
        "any_input_down": false
      },
      {
        "elapsed_ms": 1489,
        "epoch_changed": false,
        "idle_ms": 8844,
        "owned_dialog_present": false,
        "dialog_foreground": false,
        "any_input_down": false
      },
      {
        "elapsed_ms": 1544,
        "epoch_changed": false,
        "idle_ms": 8891,
        "owned_dialog_present": false,
        "dialog_foreground": false,
        "any_input_down": false
      },
      {
        "elapsed_ms": 1601,
        "epoch_changed": false,
        "idle_ms": 8953,
        "owned_dialog_present": false,
        "dialog_foreground": false,
        "any_input_down": false
      },
      {
        "elapsed_ms": 1685,
        "epoch_changed": true,
        "idle_ms": 47,
        "owned_dialog_present": false,
        "dialog_foreground": false,
        "any_input_down": false
      }
    ],
    "pickerEpochTimeline": {
      "import-cancel": [
        {
          "step": "helper_entry",
          "epoch": 390517093,
          "elapsed_ms": 158,
          "changed_from_baseline": false
        },
        {
          "step": "before_main_invoke",
          "epoch": 390517093,
          "elapsed_ms": 326,
          "changed_from_baseline": false
        },
        {
          "step": "after_main_invoke",
          "epoch": 390526062,
          "elapsed_ms": 526,
          "changed_from_baseline": true
        },
        {
          "step": "guard_refused",
          "epoch": 390526062,
          "elapsed_ms": 537,
          "changed_from_baseline": true
        }
      ],
      "import": [
        {
          "step": "helper_entry",
          "epoch": 390517093,
          "elapsed_ms": 196,
          "changed_from_baseline": false
        }
      ],
      "export-cancel": [
        {
          "step": "helper_entry",
          "epoch": 390517093,
          "elapsed_ms": 197,
          "changed_from_baseline": false
        }
      ],
      "export": [
        {
          "step": "helper_entry",
          "epoch": 390517093,
          "elapsed_ms": 197,
          "changed_from_baseline": false
        }
      ]
    }
  },
  "picker-invoke-import-cancel-open.json": {
    "before": 390517093,
    "after": 390526062,
    "completed": true
  },
  "pickerEpochTimeline": {
    "picker-epoch-timeline-import-cancel.jsonl": [
      {
        "step": "helper_entry",
        "epoch": 390517093,
        "elapsed_ms": 158,
        "changed_from_baseline": false
      },
      {
        "step": "before_main_invoke",
        "epoch": 390517093,
        "elapsed_ms": 326,
        "changed_from_baseline": false
      },
      {
        "step": "after_main_invoke",
        "epoch": 390526062,
        "elapsed_ms": 526,
        "changed_from_baseline": true
      },
      {
        "step": "guard_refused",
        "epoch": 390526062,
        "elapsed_ms": 537,
        "changed_from_baseline": true
      }
    ],
    "picker-epoch-timeline-import.jsonl": [
      {
        "step": "helper_entry",
        "epoch": 390517093,
        "elapsed_ms": 196,
        "changed_from_baseline": false
      }
    ],
    "picker-epoch-timeline-export-cancel.jsonl": [
      {
        "step": "helper_entry",
        "epoch": 390517093,
        "elapsed_ms": 197,
        "changed_from_baseline": false
      }
    ],
    "picker-epoch-timeline-export.jsonl": [
      {
        "step": "helper_entry",
        "epoch": 390517093,
        "elapsed_ms": 197,
        "changed_from_baseline": false
      }
    ]
  }
}

```



Other phase buckets contain helper_entry initialization only: they do not establish Import/Export actions. Import-cancel active=true, closed/actions=false; cancellation receipt absent. Therefore Import Cancel remains FAIL and the explicit actions stop was not reached. No other picker phase was attempted. Owned rows/usage 0/0; clipboard epoch unchanged; privacy CLEAN and negative controls PASS. Bounded failure cleanup is not graceful picker acceptance.

```json

{
  "pickerPrivateEvidenceToken": "ss-uno018-ui-87eb807efbc30307c111328b1cf7c76f",
  "pickerParentToken": "ss-uno018-ui-7d88603fe76867ee6ee05da4dbac6dad",
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
    "{\"phase2\": \"BLOCKED\", \"errorType\": \"ValueError\", \"reason\": \"worker_protocol\", \"privateEvidenceToken\": \"ss-uno018-ui-87eb807efbc30307c111328b1cf7c76f\"}",
    "{\"phase2ClipboardEpoch\": \"unchanged\"}"
  ]
}

```



## Release and remaining gates

All nine tracked trial flags and copied UI gate false; six tracked trial-source diffs empty:

```text

scripts/run_uno_candidate_native.py:19:NATIVE_SCENES_SOURCE_REVIEWED = False
scripts/run_uno_candidate_native.py:20:POSITIVE_SCENES_SOURCE_REVIEWED = False
scripts/run_uno_candidate_native.py:21:POSITIVE_INPUT_SOURCE_REVIEWED = False
scripts/uno_candidate_native_fixture/Program.cs:14:    internal static readonly bool NativeFixtureSourceReviewed = false;
scripts/uno_candidate_native_fixture/OwnedTargetPreparation.cs:26:    internal static readonly bool SourceReviewed = false;
scripts/run_uno_candidate_ui.py:20:UI_SOURCE_REVIEWED = False
src/SmartSnippets.Uno/Platforms/Windows/AcceptanceNative/NativeAcceptanceFactory.cs:14:    internal static readonly bool SourceReviewed = false;
scripts/uno_candidate_native_fixture/PositiveProtocol.cs:8:    internal static readonly bool SourceReviewed = false;
scripts/uno_candidate_native_fixture/PositiveProtocol.cs:9:    internal static readonly bool InputSourceReviewed = false;
nr14-picker-cancel-only.py:20:UI_SOURCE_REVIEWED = False

```



```json

{
  "observedUtc": "2026-11-02T02:51:50.7853015+00:00",
  "counts": {
    "SmartSnippets.UnoCandidate.exe": 0,
    "SmartSnippets.UnoAcceptanceFixture.exe": 0,
    "SmartSnippets.UnoCandidate.Guardian.exe": 0,
    "SmartSnippets.Target018.exe": 0,
    "nr14PythonHelpers": 0,
    "UIHelper": 0
  }
}

```



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



No pending clipboard/keyboard receipt across any native scene; native/UI processes and trusted observers exited. No pending guardian/coordinator/keyboard process killed, adopted or forcibly cleared. Clipboard verified in RAM only against the explicitly authorized fixed QA baseline, without reseeding. Sequence stable; owner absent; unsupported formats zero. **One ownership marker and three protective metadata formats remain**, reported rather than erased. This does not attest arbitrary clipboard state before the first authorized seed. No live profile was opened/moved/deleted. Evidence/private outputs retained.

**SCREEN-DONE nr-14 2026-11-01 20:51:51 local**. Written immediately after closure and clipboard/process verification, well before 21:13. No later desktop/clipboard work; only filesystem assembly/hash verification and the doc-only commit.

Pending: picker Import Cancel/input-source attribution and remaining import/export phases; independent review of current native evidence; remaining physical pointer/shortcut/menu, preview/hover/tray, lifecycle/fault/oldest-runtime and final wiring/CI acceptance obligations. No active recovery or operator key press pending. Plans 018/019 stay open. The released slot authorizes no retry or future launch.

Commit scope: **only docs/research/057-native-post-grant-run.md**. Operator AGENTS/safety-guide edits, all trial flags, scratch helpers and product source excluded; no push.
