# 044 — nr-03 native diagnostics: controls PASS; picker BLOCKED; posted row FAIL

Authorized brief `native-run4-brief.md`, 2026-11-01. Plan 019 remains in progress.
This is bounded fixture evidence, not production authorization, physical-input
acceptance, picker parity, or Plan 018/019 completion.

## Scope, screen gate and preparation

- Worktree: `.claude/worktrees/uno019-native-trial3`.
- Branch: `feature/uno019-native-trial3`; starting HEAD
  `dbfed46536bb1473af0cad76858e8c3f1aa81859`.
- Read AGENTS, safety guide, progress handoff 002, approved Plan 019, research
  034–043, both fixture READMEs and prior nr-01/nr-02 helpers/reports.
- Phase 0 performed only flag edits, helper copies, builds and artifact work:
  **no windows, clipboard access or application launch**.
- Before any desktop/clipboard work, re-read `nr03-grant.txt`. It contained
  `GRANTED nr-03 13:29 bottom-left DISPLAY29 -1920,1506 1920x1080 ~60min`.
  No DENIED/REVOKED line was present; no waiting poll was necessary because the
  grant was already available. The parent rechecked the grant before each scene
  and bounded admission to before 14:29 local; no external timeout-kill was used.
- Live PMv2 enumeration verified the **nonprimary bottom-left** bounds
  `(-1920,1506,0,2586)`, work rectangle `(-1920,1506,0,2538)`.
  Actual primary bounds were `(0,0,3840,2160)`, not the stale dimensions in the
  brief; primary was never selected. DISPLAY names were not authority.
- Preflight reported no held virtual keys. The trusted parent also enforced
  unchanged desktop/foreground, all-input-up and 1500ms quiet admission.
- Before seeding, candidate, management fixture, guardian and Target018 counts
  were all zero; no prior recovery or uncertain keyboard lifetime was pending.
  The authorized baseline was seeded **once**, never between scenes or afterward:

```powershell
"Smart Snippets QA" | & "$env:SystemRoot\System32\clip.exe"
```

Locked RAM-only verification immediately afterward matched the fixed synthetic
baseline, with stable sequence, four builtin text formats, no ownership marker,
no protective metadata, zero unsupported formats and no owner. No arbitrary
clipboard payload or unknown format name was logged or persisted.

No registered-* scene, agent keyboard/mouse synthesis, downloads, clock changes,
real-profile access, foreign activation, guard relaxation or mutation retry occurred.
Posted row uses the fixture's existing reviewed preparation/callback route; the
product's own permit-guarded exact-owned-target paste was authorized, but was not
reached. Posted Enter was not run. No placement helper source change was needed.

## Phase 0 — PASS: flags and fresh strict builds

Exactly nine test-only booleans were temporarily changed false→true:

| File | Constants |
|---|---|
| `scripts/run_uno_candidate_native.py` | `NATIVE_SCENES_SOURCE_REVIEWED`, `POSITIVE_SCENES_SOURCE_REVIEWED`, `POSITIVE_INPUT_SOURCE_REVIEWED` |
| `scripts/uno_candidate_native_fixture/Program.cs` | `NativeFixtureSourceReviewed` |
| `scripts/uno_candidate_native_fixture/PositiveProtocol.cs` | `SourceReviewed`, `InputSourceReviewed` |
| `scripts/uno_candidate_native_fixture/OwnedTargetPreparation.cs` | `SourceReviewed` |
| `src/SmartSnippets.Uno/Platforms/Windows/AcceptanceNative/NativeAcceptanceFactory.cs` | `SourceReviewed` |
| `scripts/run_uno_candidate_ui.py` | `UI_SOURCE_REVIEWED` |

The same enabled native source/artifacts were retained through controls and the
posted attempt; no intermediate rebuild or source-equivalence exception.
Startup/full-host/prototype gates and ordinary product capability remained closed.
Exact temporary diff: scratchpad `nr03-enabled-flags.diff`, SHA256
`e98a8dc73d4a71249888b4a9e0c817d5465524ca8270b7fdf9e55bbbc06895a8`.

Commands, sequentially from repository root (`<scratchpad>` is the brief's private
scratchpad directory):

```text
pwsh -NoProfile -File scripts/uno_candidate_native_fixture/build.ps1 -BuildOnly
pwsh -NoProfile -File <scratchpad>/nr03-build-support.ps1
pwsh -NoProfile -File scripts/verify-uno-candidate.ps1 -BuildOnly
```

The support helper is a copy of nr01's, changing only the trial root and fresh
output prefix to nr03. It creates owner-private unique outputs, then cleans and
publishes Target018 and guardian sequentially with `NoWarn=`, AOT/trim/compatibility/
analyzers/warnings-as-errors true, and trim-warning suppression/single-warning
aggregation false. Fixture wrappers select their pinned SDK and enforce native
x64/SQLite/property/reference/freshness gates. All four build logs independently
passed warning/error scans; all four executable hashes were recomputed after runs.
No tests were rerun and no build-only result is native acceptance evidence.

| Artifact | Fresh build token | SHA256 |
|---|---|---|
| Native fixture | `6c054212ed2e45c6afafdcfc19101987` | `a550ba15a5c535ec5e05a5f8bb29bf09856244d61946da81bd21493fb55ad45c` |
| Target018 | `21676c3ad8574b87bce11262560fabfe` | `b06a6bcb5ede4b8468cb023d3c04c34530b05831cf7a34e8a3b6fd3c5138273b` |
| Guardian | `6eb7bc8ab9a54d19b9f0d031479a7fb4` | `0170dadc84a360a9fc58c9c2f221f22b10dcf5b84dabeada13d0e0b86f341de5` |
| Management fixture | `e18ff6fd69844ea6a6f2083bc8c2dfae` | `81d358212311cd7ad771954ff783821b5e446edd64241b5a053381edc407b2fd` |

Native enabled-source receipt:
`5d35a86ab6f892c0d8f01ec798c0f9c7e4c696f0f802155954289221f1fd39b1`.
Management fixture base source receipt:
`ff12e726c0bc097abc89bd94bc7dde7552cb1aa2140547f0f3c70432f4ae2879`.
Native/management outputs use their standard `bin/publish-native/<token>` and
`bin/publish-acceptance/<token>` directories. Target/guardian outputs are
`bin/nr03-<token>` beneath their respective prototype projects. Old binaries were
not reused; retained enabled artifacts are evidence, not fresh execution authority.

## Phase 1 — PASS: same-artifact restore → abort → owner-refusal

```text
python -I -B <scratchpad>/nr03-preflight.py
python -I -B <scratchpad>/nr03-clipboard-check.py
python -I -B <scratchpad>/nr03-run.py
```

Trusted detached parent token: `ss-uno018-ui-3c0839bae4a3ab07e0e4f252dd1bfd23`.
Its stdout contains `nr03_phase1=PASS`; stderr is empty. The parent calls the
actual `scripts/run_uno_candidate_native.py` main entry with exact executable/
target/guardian paths and hashes, `--authorized-screen-slot` and each scene.
Each returned `RestorativeSessionObservation` was passed directly to the next
call in this same trusted process. No JSON import, witness reconstruction,
latest-directory discovery, false recopy assertion or clipboard reseed supplied
baseline authority. The witness stayed in RAM while phase 2 ran, then was supplied
to phase 3 with `--row6-controls-verified`.

| Scene | Exact private session token | Result |
|---|---|---|
| restore | `ss-uno018-ui-abf2cdcd5b1ff93b954d3582995034ef/ss-uno019-native-0a8a2cf3bd954e129b60c24f0e360f54` | PASS: `clipboard_control_settled`, `regular_restored` |
| abort | `ss-uno018-ui-be68b3b778f93ca9a8d3649c72d678af/ss-uno019-native-7f8b1187a2434aa6b2ae2472a8b4ead3` | PASS: `clipboard_control_settled`, `abort_restored` |
| owner-refusal | `ss-uno018-ui-71572a27c1565f90938faf965e47aa54/ss-uno019-native-3b0b9134d893447f8687265f8e414c68` | PASS: `clipboard_control_settled`, `owner_refusal_restored` |

All returned the native source receipt above. Exact ordered receipts, empty native
stderr, host/error checks and privacy scans with planted negative controls passed;
separate read-only revalidation passed. No pending receipt or recovery retry.
Abort intentionally terminates only the worker/target Job after publication; this
is not graceful worker-shutdown evidence. Owner-refusal changes owned HWND A→B
within one guardian PID, not an arbitrary foreign producer. Restoration preserves
supported original representation bytes plus declared marker/protective metadata,
not identical original format-set restoration. Original backups remain RAM-only.

## Phase 2 — BLOCKED: Import Cancel foreground failure

The parent invoked scratchpad `nr03-picker.py`, copied from nr01 and updated only
for this worktree, module names and new artifact pins. It runs:

```text
scripts/run_uno_candidate_ui.py --scene file-picker
  --executable <management-output>/SmartSnippets.UnoAcceptanceFixture.exe
  --sha256 81d358212311cd7ad771954ff783821b5e446edd64241b5a053381edc407b2fd
  --authorized-screen-slot --work-area -1920 1506 0 2538
```

Picker parent: `ss-uno018-ui-b7cb2b5af2f6ea74399b7d974f09f339`.
Exact UI session: `ss-uno018-ui-92ab8686f6448a8f139c7f568cce011b`.
Picker child exited 1, stderr empty; its outer logging wrapper exited 0, which is
**not** a picker PASS. Parent reason: `worker_protocol`; observer-worker reason:
`foreground_changed`. Clipboard sequence stayed unchanged across the attempt.

The observer's `foreground-failure.json` passed the exact 13-field schema:

```json
{
  "foregroundRelation": "owned_other",
  "fgIsMain": false,
  "fgIsPopup": false,
  "popupPresent": false,
  "fgSamePid": true,
  "fgThreadIsMainThread": false,
  "fgOwnerIsMain": true,
  "fgRootOwnerIsMain": true,
  "fgClassIs32770": true,
  "fgVisible": false,
  "fgEnabled": true,
  "phaseIndex": 0,
  "sampleOrder": 31
}
```

No helper foreground-failure file exists; helper stdout/stderr are empty. The
record describes the failure-path samples, not an atomic window identity snapshot.
It does not authenticate an acceptable same-thread picker or justify adopting
another thread/provider. In particular, same-PID/main-owner/class observations
must not be promoted to the missing main-thread/visible-popup acceptance.
No title, raw HWND, class string or desktop discovery was added.

| Required step | Outcome |
|---|---|
| Import Cancel | BLOCKED: foreground guard failed before completion |
| Import Commit | BLOCKED / NOT RUN after first phase failure |
| Export Cancel | BLOCKED / NOT RUN after first phase failure |
| Export Commit | BLOCKED / NOT RUN after first phase failure |

`ui-ready.receipt` and `picker-import-cancel.active` exist. No Import Cancel
closed marker or completion receipt exists. Read-only owned SQLite aggregate:
0 rows / 0 usage. Post-failure diagnostic privacy scan and planted negative
controls passed CLEAN. Existing owned UI/helper/Job failure cleanup completed;
no graceful shutdown or successful pre-kill JobZero is claimed. No mutation retry,
provider fallback, replacement HWND adoption or broadened ownership guard followed.

## D26 / pt-01 — narrowed picker observation rule (headless change only)

Plan 019 line 165 requires the existing Windows pickers; line 216 requires the
candidate window as owner. The product remains on
`Microsoft.Windows.Storage.Pickers`; no picker service is replaced. D26 supersedes
the harness's same-main-thread prerequisite, not the historical blocked result
above. The observed same-PID, direct-main-owner `#32770` on another thread may be
authenticated from foreground even when `GW_ENABLEDPOPUP(main)` is empty and the
dialog is not yet visible. This does not establish why that API returned empty or
which internal provider mechanism created the dialog.

Both observers now require exact class, direct owner and candidate PID, allowing
any nonzero thread. They retain the exact first HWND per phase and refuse a
replacement. Initial invisibility permits latching only: phase completion still
requires a visible observation, and picker controls are not used before visibility.
Other foreground classes, owners and PIDs remain refused with the existing fixed
13-field diagnostic schema; `fgThreadIsMainThread` is informational. No title
reads, desktop enumeration, PID adoption or input synthesis is added. Source gates
stay closed. Headless tests are not native acceptance; no UI/native run was made
for pt-01, and Plan 019 remains incomplete.

## Phase 3 — FAIL: posted-row; later posted scenes NOT RUN

Phase 3 ran independently of the picker result because phase 1 passed. The trusted
parent called the actual native runner with the same artifacts/source, retained
owner-refusal witness, `--scene posted-row --row6-controls-verified` and no fabricated
recopy flag. No registered-* scene or operator action was requested. Generic
legacy runner instructions in private stdout did not trigger operator interaction;
reviewed in-process owned-A preparation performed the initial activation.

Exact session:
`ss-uno018-ui-6d2d0ff97c46bfba8906d3c8670b11a2/ss-uno019-native-a289a71054c643a9961f978f8c4fa3d0`.

Runner result: `native_scene_failed`; **`failureStage=guardian_ready`**.
Native stdout is exactly `guardian_ready` → `awaiting_owned_a` → `failed` using
the normal `native_phase=` / `native_result=` prefixes; native stderr is empty.
The fixed sanitized failure line is:

```text
2026-11-01T19:33:34.393Z ERROR native_positive_failed stage=guardian_ready InvalidOperationException (HResult=0x80131509)
```

Additional fixed diagnostics, in order:

```text
owned_initial_focus phase=waiting
owned_preparation_admission reason=ready
owned_target_preparation reason=Prepared
owned_initial_focus phase=ready
native_popup_focus reason=foreground_refused
candidate_popup_focus reason=refused
native_popup_readiness shown=True target=True popup_foreground=False target_foreground=True quiet=True
```

This attempt passed the earlier pre-ready boundary and exact owned-A preparation.
The existing single popup foreground request returned false; popup readiness then
failed with the popup shown/correct target/quiet input but foreground on Target A.
The stage token is the **last pre-guardian-ready stage**, not a claim that the
throw occurred before readiness or inside guardian startup. The fixed focus
receipts identify the refused foreground request, not Windows' internal reason.
No focus retry/workaround, keyboard arm, clipboard publication or paste followed.

Read-only owned SQLite aggregate: 1 synthetic row / 0 usage; no `insert_start`.
Diagnostic scan with planted negative controls passed CLEAN. Native stdout has no
recovery-pending receipt and no keyboard-pending diagnostic was found. The log
has **no `candidate_shutdown_complete`**; all native processes nevertheless exited.
Thus full graceful host shutdown is not claimed for this failed scene.

| Scene | Outcome |
|---|---|
| posted-row | FAIL: popup foreground/readiness refusal, `failureStage=guardian_ready` |
| posted-main | BLOCKED / NOT RUN after first phase failure |
| posted-enter | BLOCKED / NOT RUN after first phase failure |

Proof limit: **`registered_posted_callback_not_physical`**. Posted WM_HOTKEY/VM
callbacks are not HID, native pointer/menu or physical shortcut acceptance. No
positive insertion PASS is claimed and no failed mutation/scene was retried.

## Release — flags closed; zero processes; no pending recovery

All nine temporary booleans were restored false immediately after the stopped
sequence. `git diff --exit-code` passed for every temporarily edited source file.
`nr03-flags-closed.txt` retains grep proof of all nine, plus:

```text
STARTUP_OBSERVER_REVIEWED = False
FULL_HOST_OBSERVER_REVIEWED = False
NativeEntryReviewed = false
```

No enabled flag or source/helper change is committed; ordinary candidate native
capability remains default-denied. Operator edits to `AGENTS.md` and
`docs/guides/002-acceptance-harness-safety.md` were never changed or staged.

Final read-only process proof at `2026-11-01T19:34:35.4844896Z`:

| Process group | Count |
|---|---:|
| SmartSnippets.UnoCandidate (coordinator/worker) | 0 |
| SmartSnippets.UnoAcceptanceFixture | 0 |
| SmartSnippets.UnoCandidate.Guardian | 0 |
| SmartSnippets.Target018 | 0 |
| nr03 Python run/picker helpers | 0 |
| UI PowerShell helpers | 0 |

No pending recovery, retained guardian/coordinator or uncertain keyboard lifetime
remains. No pending process was killed; no synthetic key-up repair or manual
recovery endpoint was used. Screen work ended well inside the 60-minute grant.

Final locked clipboard check: fixed authorized synthetic baseline matches exactly
in RAM; sequence stable during the check; 4 builtin text formats, 1 ownership
marker, 3 protective formats, **0 unsupported formats**, owner absent. No further
seed/clipboard write occurred. This is restoration of the authorized post-seed
baseline, not pre-seed user clipboard data or immunity to subsequent changes.

Evidence helpers and content-free summaries are retained in the brief's scratchpad:
`nr03-build-support.ps1`, `nr03-artifacts.json`, `nr03-run.py`, `nr03-picker.py`,
`nr03-preflight.py`, `nr03-clipboard-check.py`, `nr03-evidence-check.py`,
`nr03-evidence.txt`, `nr03-process-release.json`, `nr03-clipboard-release.json`,
`nr03-flags-closed.txt` and `nr03-enabled-flags.diff`. Original nr01/nr02 helpers
were not edited. Separate evidence checking was read-only, never witness authority.

Pending acceptance: picker provider/foreground compatibility and popup focus
under the posted route remain unresolved; posted-main/posted-enter remain unrun.
These need a separate reviewed disposition and screen authorization before new
runtime attempts. No active recovery work, operator prompt or decision is pending.
Commit scope for this run is **only this research document**.

## pg-01 — posted-scene foreground grant design (no acceptance claim)

D27 supplies the missing foreground authority in the fixture, not in candidate
product focus code. The owned lease bootstraps Target018 with the launching
candidate's exact PID and creation time; Target018 validates them and retains the
process handle before readiness. One fixed `foreground_grant` control-channel
message carries no caller-selected PID. Only while owned A remains foreground
and the retained candidate is live does Target018 call
`AllowSetForegroundWindow(exactCandidatePid)` once. The fixed receipt is
`target_foreground_grant result=granted`, `target_foreground_grant result=refused`
(only a false native grant), or `target_foreground_grant result=failed`
(pre-check/exception failure). A refused/malformed receipt or changed target identity fails closed before posting.

Only posted-enter/row/main await this receipt immediately before posting the
existing callback. Registered scenes request no grant. The candidate's single
ready-coordinator foreground request, exact popup foreground/quiet readiness and
keyboard-permit-before-mutation gates are unchanged. No ASFW_ANY, candidate
self-grant, AttachThreadInput, retry or weakened foreground check is introduced.
The fixed `target_foreground_grant` stage is the last pre-trigger stage, not a
precise failure locator: it remains set for later posted-scene readiness,
keyboard-permit and insertion failures.
Windows can still revoke or refuse foreground rights; such failures remain FAIL.
Unit tests and builds are not native acceptance: no UI/native scene is run for
pg-01, all source-review gates remain false, and 018/019 closure stays open.
