# 046 — nr-06: controls PASS; picker and posted-row FAIL; Copy not granted

Authorized `native-run6-brief.md`, with the leader's narrower screen update,
2026-11-01. Plan 019 remains In progress. This is bounded fixture evidence, not
production authorization, physical-input acceptance or Plan 018/019 completion.

## Authorization and screen boundary

- Worktree `.claude/worktrees/uno019-native-trial5`, branch
  `feature/uno019-native-trial5`, starting HEAD
  `eacd4ef8e46d20096ca069ef74d5ae8368d42985`.
- Read AGENTS, safety guide 002, progress handoff 003, approved Plan 019,
  research 041–045, both fixture READMEs and nr-05 helpers/report.
- Phase 0 was build/filesystem-only: no windows, clipboard access or application
  launch before the slot. Fresh builds completed before 15:22 local.
- The later user update narrowed the slot to **15:22–15:47 local**, hard end,
  controls → picker → posted-row/main/enter only. The four additional scenes
  required an exact standalone `GRANTED nr-06-copy` line. None appeared.
- The grant contained `GRANTED nr-06`, the above window/scope, and later
  `DENIED nr-06-copy (pc-manager 15:14)`. Copy/context scenes were **NOT RUN
  (no grant)**. A scoped Copy denial was not treated as revoking the separately
  granted controls/picker/posted scope.
- An initial filesystem-only checker matched that scoped denial too broadly and
  exited before the slot, without any desktop/clipboard operation. Scratchpad
  copies were corrected to match the exact `nr-06` grant/denial token, excluding
  `nr-06-copy`; no source guard or mutation was retried. That initial observation
  is retained as `nr06-copy-denial-prewindow-gate.json`.
- Corrected filesystem/time-only polling checked every 30 seconds. Poll 14
  admitted execution at `2026-11-01T21:22:18.246112Z` (15:22:18 local), after
  390.011 seconds. Deadline remained the absolute 15:47 hard end, not 35 minutes
  from that observation. Scene admission reserved three minutes for release.
- Live PMv2 enumeration verified the **nonprimary bottom-left** bounds
  `(-1920,1506,0,2586)` and work `(-1920,1506,0,2538)`. Actual primary bounds
  were `(0,0,3840,2160)`, not the brief's stale dimensions. No primary/top-left
  window was selected, controlled or moved. No placement change was needed.

At `21:22:30.7185897Z`, candidate, management fixture, guardian, Target018,
nr06 run/picker and UI-helper counts were all zero; no recovery was pending.
Held-input preflight was clear. Only then was the authorized baseline seeded
**once**, with a second process/grant/time check inside the seed helper:

```powershell
"Smart Snippets QA" | & "$env:SystemRoot\System32\clip.exe"
```

Initial locked RAM-only comparison matched that fixed baseline with stable
sequence: 4 builtin text formats, 0 ownership markers, 0 protective formats,
0 unsupported formats, owner absent. No reseed or later clipboard write by the
agent. Original arbitrary clipboard payloads/names were never logged/persisted.

No registered-* scene, operator action/request, external keyboard/mouse synthesis,
foreign-focus workaround, generic key-up, download, clock change, production
profile access or failed mutation retry occurred. The reviewed fixture performed
owned-A preparation and the application's own guarded paste into Target018.
Posted-enter was not reached. Generic legacy operator instructions in runner
stdout were not acted upon.

## Phase 0 — PASS: exact trial flags and fresh native artifacts

Exactly nine documented booleans were temporarily enabled; no new Copy gate was
present in the fixture README/source:

| File | Constants |
|---|---|
| `scripts/run_uno_candidate_native.py` | `NATIVE_SCENES_SOURCE_REVIEWED`, `POSITIVE_SCENES_SOURCE_REVIEWED`, `POSITIVE_INPUT_SOURCE_REVIEWED` |
| `scripts/run_uno_candidate_ui.py` | `UI_SOURCE_REVIEWED` |
| `scripts/uno_candidate_native_fixture/Program.cs` | `NativeFixtureSourceReviewed` |
| `scripts/uno_candidate_native_fixture/PositiveProtocol.cs` | `SourceReviewed`, `InputSourceReviewed` |
| `scripts/uno_candidate_native_fixture/OwnedTargetPreparation.cs` | `SourceReviewed` |
| `src/SmartSnippets.Uno/Platforms/Windows/AcceptanceNative/NativeAcceptanceFactory.cs` | `SourceReviewed` |

All startup/full-host/prototype and ordinary candidate gates remained closed.
Exact boolean-only diff: private `nr06-enabled-flags.diff`, SHA256
`1de6354c90de24259c58e82ae9c77f2aa4f6d77e9bc567f21243b80aefc87d4e`.

Commands ran sequentially from the worktree root; `<scratchpad>` denotes the
brief's private directory:

```text
pwsh -NoProfile -File scripts/uno_candidate_native_fixture/build.ps1 -BuildOnly
pwsh -NoProfile -File <scratchpad>/nr06-build-support.ps1
pwsh -NoProfile -File scripts/verify-uno-candidate.ps1 -BuildOnly
python -I -B scripts/run_uno_candidate_native.py --validate-build <native-entry> <native-sha256>
python -I -B <scratchpad>/nr06-artifacts.py
```

Both candidate-linked fixtures passed their strict native x64/SQLite/effective
property/reference/freshness gates. The copied support helper created fresh
owner-private outputs, cleaned and published Target018 and guardian with
`NoWarn=`, AOT/trim/compatibility/analyzers/warnings-as-errors true, and trim
warning suppression/single-warning aggregation false. All four warning/error
scans passed. All executable hashes were independently recomputed after the
run and matched. No warning suppression, gate weakening, reused executable or
additional test-suite execution is claimed. These are fixture, not distributable
production-candidate, artifacts.

| Artifact | Fresh token | SHA256 |
|---|---|---|
| Native fixture | `9c178e15208c4302a4dad0f484ba9583` | `5de7041736226b215d5122e43aa5e1e53eb3bbc3d3ac9a0e42d0cde813059cc3` |
| Target018 | `8e1e57808da14098b8d1e67f91d0daa5` | `fb3dca86ce327690abe5273dd808b7ea2493ae0d4aa0200ffcc5581a779d5cf4` |
| Guardian | `8be242bdc8d9467d872e4029dfb427af` | `91ba2823f9b60e454cc26d5fc86826ed64c20346b27ebb8bc47ea8354187faf6` |
| Management fixture | `663fb39db9c848fe9e7dc85d7cb13003` | `b9e23b611866bad3dd4aeaeed5918a15b1d72f245ea10211a1a190dd23ea61bf` |

Native enabled-source receipt:
`b42e8421e77bac16f1abb5fe13c95e05289f9c0d9edbef3b2b68a924f7a290bd`.
Management base source receipt:
`43d5a7b6854964e02dcccb3d6771abd4a798eb3c938024be04db913f9f1cb460`.
Fixture outputs use `bin/publish-native/<token>` and
`bin/publish-acceptance/<token>`; target/guardian use fresh `bin/nr06-<token>`
under their prototype projects. Enabled binaries remain private evidence, not
permission to run against the now-closed source.

Original nr05 helpers were never edited. Scratchpad nr06 copies changed the
worktree/artifact pins, scope-aware grant/time checks, first-failure phase
handling and process audits. No tracked placement/behavior/helper change remains.

## Execution commands and per-step results

```text
python -I -B <scratchpad>/nr06-wait-grant.py
python -I -B <scratchpad>/nr06-preflight.py
pwsh -NoProfile -File <scratchpad>/nr06-process-check.ps1
pwsh -NoProfile -File <scratchpad>/nr06-seed.ps1
python -I -B <scratchpad>/nr06-clipboard-check.py
python -I -B <scratchpad>/nr06-run.py
# After completion and closing all flags, read-only checks:
python -I -B <scratchpad>/nr06-evidence-check.py
python -I -B <scratchpad>/nr06-artifacts.py
```

The trusted detached parent invokes the actual native runner main with exact
native/target/guardian paths/hashes, `--authorized-screen-slot --scene <scene>`;
posted-row additionally receives `--row6-controls-verified`. Controls and positive
witnesses are direct returned objects retained only in that same parent's RAM,
never reconstructed from JSON, old receipts or discovered directories.

| Phase/step | Result |
|---|---|
| 0: flags/builds/hashes | PASS |
| 1: restore | PASS: `regular_restored` |
| 1: abort | PASS: `abort_restored` |
| 1: owner-refusal | PASS: `owner_refusal_restored` |
| 2: Import Cancel | FAIL: `picker_schema_multiple` |
| 2: Import Commit | BLOCKED / NOT RUN after first picker failure |
| 2: Export Cancel | BLOCKED / NOT RUN after first picker failure |
| 2: Export Commit | BLOCKED / NOT RUN after first picker failure |
| 3: posted-row | FAIL: independent runner `native_scene_failed`, despite internal settled result |
| 3: posted-main | BLOCKED / NOT RUN after first positive failure |
| 3: posted-enter | BLOCKED / NOT RUN after first positive failure |
| popup-copy | NOT RUN (no grant) |
| editor-copy | NOT RUN (no grant) |
| footer-copy | NOT RUN (no grant) |
| context-insert | NOT RUN (no grant) |
| Release | PASS: flags closed, zero processes, no pending recovery, baseline verified |

## Phase 1 — same-artifact controls PASS

Trusted parent: `ss-uno018-ui-c72114dacd54fc5526bca913c32f27e9`, empty stderr,
final `nr06_observer_complete=1`. Same enabled source/artifacts throughout.

| Scene | Exact private session |
|---|---|
| restore | `ss-uno018-ui-d3ced9aa1efe64d033be4e4f5a42820a/ss-uno019-native-80c1dbf6655e429bb0966f586ede1b21` |
| abort | `ss-uno018-ui-1be6ae414b3ef623739fdd07c18ab98e/ss-uno019-native-e21f42742af042fd9e45b9b71c02c648` |
| owner-refusal | `ss-uno018-ui-102f6cfaca4e1b81f62033caf55c44dc/ss-uno019-native-e02c7917f7d04188ac69dba60f984bfb` |

Each logged `native_control_admission reason=Ready` then
`native_control_publish status=swapped`, with the ordered guardian/publication/
settlement receipts, empty stderr, accepted process settlement and privacy scans
with planted negative controls. Separate read-only receipt/host/privacy checks
also passed. Each owned SQLite aggregate was 0 rows / 0 usage.

No `ForegroundChanged` admission occurred; therefore no refusal-only
`native_control_foreground` relation/fgVisible/fgEnabled/fgSameThreadAsOwned/check/
sampleOrder record exists to copy. None is fabricated. No pending receipt or
recovery retry occurred. Abort intentionally terminates only its worker/target
Job after publication; it is not graceful worker shutdown evidence. Restore and
owner-refusal have `candidate_shutdown_complete`; abort does not. Owner-refusal
is an owned HWND A→B change within the guardian PID, not a foreign producer.
Restoration preserves supported original representations plus declared marker/
protective metadata, not identical format sets or pre-seed user clipboard data.

At `21:23:00.0182785Z`, all native/UI process counts were zero except the trusted
nr06 parent itself. Its owner-refusal witness stayed in RAM during the picker.

## Phase 2 — picker FAIL: two Cancel-schema matches

Picker parent: `ss-uno018-ui-d346fee2c37e4fec9d01c7f2e5d97b10`, empty stderr,
child/wrapper exit 1. Exact UI session:
`ss-uno018-ui-ee8f5a195c396237cfe53ab1ad37d7c7`.

The copied wrapper calls `scripts/run_uno_candidate_ui.py --scene file-picker`
with the new management executable/hash, `--authorized-screen-slot --work-area
-1920 1506 0 2538`. It is independent of the positive native phase.

Fixed failure records:

```json
{"observer-parent-failure.json":{"reason":"worker_protocol"},"observer-worker-failure.json":{"reason":"ui_helper_protocol"}}
```

Exact helper stderr and fixed-schema `ui-helper-picker-schema.json`:

```text
ui_helper_failed stage=picker_import_cancel reason=picker_schema_multiple
```

```json
{"reason":"picker_schema_multiple","matchCount":2,"controlTypeId":0}
```

The authenticated visible picker/UIA root reached the Cancel-control schema
check, which found two matches rather than exactly one. `controlTypeId=0` is
the nonunique/unknown sentinel, not an observed type of either control. The record
does not identify why the provider exposed duplicates or authorize selecting one.
Both foreground-failure JSON files are absent; no 13-field foreground record
exists. No fallback selector, wider discovery, ownership relaxation or retry.

`ui-ready.receipt` and `picker-import-cancel.active` exist; no closed/actions/
completion receipt or later phase marker exists. SQLite: 0 rows / 0 usage.
Privacy scan and planted negative controls CLEAN. Clipboard epoch unchanged
across the attempt. Existing owned helper/Job cleanup completed; no graceful
failed-host shutdown or successful pre-kill JobZero claim. At
`21:23:11.4613826Z`, only the trusted parent remained; no recovery was pending.

## Phase 3 — posted-row FAIL at independent monitor; internal insertion settled

Exact session:
`ss-uno018-ui-fa7f19b0a1e90294b9e0e1f5c698bf53/ss-uno019-native-8bb106664b0f405882f17625790c8432`.

The runner reported `result=native_scene_failed`, **`failureStage=none`**.
There is **no `native_positive_failed stage=<token> <Type> (HResult)` line**:
the native worker did not log that failure. Native stdout was exactly:

```text
native_phase=guardian_ready
native_phase=awaiting_owned_a
native_result=posted-row_settled
```

Native stderr was empty. Ordered content-free native diagnostics include:

```text
owned_initial_focus phase=waiting
owned_preparation_admission reason=ready
owned_target_preparation reason=Prepared
owned_initial_focus phase=ready
target_foreground_grant result=granted
native_popup_focus reason=foreground_accepted
candidate_popup_focus reason=accepted
clipboard_swap attempts=1 status=Swapped saved=7 skipped_by_design=0 unsaved=0 cap_exceeded=False bytes=116 ms=162
clipboard_restore status=Restored attempts=1
insert_result outcome=Inserted id=1 notice=False ms=1158
```

The target grant occurred at `21:23:15.170Z`, popup focus was accepted at
`21:23:15.985Z`, and restoration/Inserted at `21:23:17.215Z`/`21:23:17.216Z`.
The native scene's settled receipt follows its exact-target/empty-B, single-paste,
duplicate Busy and usage assertions. The log contains `candidate_shutdown_complete`
at `21:23:17.428Z`, no ERROR and no recovery/keyboard-pending receipt. Separate
read-only ordered-receipt/host/privacy checks pass, including negative controls.
These are scoped internal results, **not an accepted overall scene**.

The independent Python monitor nevertheless raised `native_scene_failed` before
`verify_result`/`verify_positive_boundary` and before returning a trusted positive
witness. Its fixed error combines a latched observation failure (foreground,
oversized output or observation exception) and nonzero coordinator exit; this
run did not retain the individual predicate or process exit code. Do not infer a
specific foreground window, startup/teardown race or product cause from the
settled receipt. No new diagnostic, monitor relaxation or rerun was attempted.

Independent read-only SQLite check: **1 row / usage 1, PASS**. The runner's
independent before/after clipboard-epoch check was **UNAVAILABLE / NOT COMPLETED**
because monitor failure short-circuited it; no `usageVerified`/
`clipboardEpochChanged` success JSON was emitted. The native restore log and final
baseline comparison cannot replace that missing epoch result. Overall posted-row
remains **FAIL**, and posted-main/enter remain **BLOCKED / NOT RUN**. Their
failureStage, grant receipts, failure lines and independent checks are absent,
not borrowed from posted-row. Copy/context scenes have no such evidence because
they were not granted or launched.

Proof limit: **`registered_posted_callback_not_physical`**. This VM callback and
posted WM_HOTKEY route does not prove native pointer/menu, HID or physical shortcut
delivery. A successful internal paste does not override the independent failure.

## Release — PASS, completed before 15:24 local

All nine flags restored false immediately after the stopped sequence. Grep proof
is retained in `nr06-flags-closed.txt`, also showing startup/full-host observer and
prototype `NativeEntryReviewed` false. `git diff --exit-code` passed for every
temporarily edited source file. Ordinary candidate capability is default-denied.
No enabled flag, tracked helper, placement fix or behavior change is committed.
Operator edits to AGENTS and safety guide were preserved, never edited/staged.

At `2026-11-01T21:23:59.9002408Z` (15:23:59 local), release counts were:

| Process group | Count |
|---|---:|
| Candidate coordinator/worker | 0 |
| Management fixture | 0 |
| Guardian | 0 |
| Target018 | 0 |
| nr06 run/picker Python helpers | 0 |
| UI PowerShell helpers | 0 |

No pending clipboard recovery, retained coordinator/guardian or uncertain keyboard
lifetime remains. No pending process was killed, recovery endpoint called or
synthetic release sent. Bottom-left screen work finished before 15:24, well before
the 15:47 hard end; the slot is safely released. No operator prompt is pending.

Final locked clipboard comparison: authorized post-seed baseline matches exactly
in RAM; sequence stable within the check; 4 builtin text formats, 1 ownership
marker, 3 protective formats, 0 unsupported formats, owner absent. No further
clipboard write. This is not restoration of pre-seed user data, an independent
positive epoch result, or a guarantee against later clipboard changes.

Private retained evidence: `nr06-artifacts.json`, enabled diff, build outputs,
artifact recheck, grant polling/result and initial prewindow Copy-denial record,
helper copies (build/preflight/seed/run/picker/process/clipboard/evidence),
`nr06-evidence.txt`, `nr06-flags-closed.txt`, `nr06-process-release.json`,
`nr06-clipboard-release.json`, and exact sessions above. No original helper was
edited; no private profile/evidence deleted. Commit scope: **only this document**.

Pending acceptance: picker duplicate Cancel schema; independent posted monitor
failure and missing epoch evidence; posted-main/enter unrun; four additional
scenes not granted. No active recovery or operator decision is pending. Further
runtime attempts require a separate authorization, not continuation/retry under
this released slot. Plans 018/019 remain incomplete.
