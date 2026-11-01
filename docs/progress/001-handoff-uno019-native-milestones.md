# Handoff — Plan 019 Windows candidate

## Goal and operator direction

Continue approved Plan019 to actual completion; do not present a partial checkpoint
as completed. Mac tests paused. Use primary **landscape**, never right portrait;
query actual primary work area dynamically (last `(0,0,3840,2088)`). Operator wants
speed, direct minor fixes, isolated parallel workers, and reviewer effort matched
to risk (low routine, medium bounded review, high only genuinely hard security).
Operator explicitly permits agent preparation of fixed synthetic QA baseline:
`"Smart Snippets QA" | & "$env:SystemRoot\System32\clip.exe"` in PowerShell.
Never reseed while recovery is pending; clipboard history is not a backup guarantee.
Latest request: finish subagents, find clean boundary, perform context handoff.

## Environment / clean boundary

Root **D:/sources/demo/smartsnippets**, branch **feature/uno-desktop-feasibility**.
Last implementation commit **0079f47**; final handoff/docs commit follows it.
All known subagents completed; reports consumed. No active native test, uncertain
keyboard lifetime or guardian recovery. Last enumeration: candidate/guardian/
Target018 process counts all zero. All eight native flags, all UI observer gates,
product null capability and prototype `NativeEntryReviewed=false` remain closed.
No pushes/PRs, no Plan018/019 completion, no020 start. Preserve all worktrees/artifacts.

Final write-only checkpoint records picker review BLOCK; no new implementation
was started after operator requested this handoff. Check `git status` on resume.

## Done and evidence

Read `docs/plan/019-uno-shared-shell-and-platform-contracts.md`, updated tracker.

- Research033: rendered snippets/Save/Cancel/new-draft Discard, folders and settings
  PASS on primary landscape; settings repository reload is not process restart.
- Research035–038: actual ordinary restore, stopped-worker abort, owned-HWND
  owner-change refusal; strict witness chaining; source fixes and failures retained.
- **Research039:** actual operator Ctrl+Alt+Shift+F11 → registered OS callback →
  popup focus → native Enter → fixed Unicode/CRLF paste/readback → SQLite usage
  exactly once, duplicate Busy refusals, clipboard restore and graceful shutdown
  PASS. Scoped independent audit `c1635f83` cleared. Not HID-authenticity proof.
- **Research040:** actual native preview and real monotonic expiry preserve
  clipboard epoch/usage zero; explicit main-row VM Copy commits only synthetic
  text, no paste/usage. Independent audit `593e0557` cleared these scoped results.
  Copy is not a restorative ledger witness; current clipboard holds synthetic
  Copy content, not an unsettled original backup.
- `62213a7` (worker `ca0bbef7`) implements registered-row/main operator routes,
  truthful VM-callback labels, no posted message or pointer proof. Native NOT RUN.
- **0079f47 / research041:** fixes P1 unexpected-popup-input-before-keyboard-permit
  and premature undispatched settlement. Separate one-shot `PositiveNativeMutation`
  grants Input/Clipboard only after coordinator permit + local Keyboard.Arm +
  shown/idle/not-inserting recheck. Revokes before settlement await/failure/drain;
  clipboard cleanup stays permission-independent. Confirming review `3422f8e5`
  cleared the batch plus registered-family extension. Fresh native build required.

Latest sequential validation: **436 Core, 616 Presentation, 276 candidate,
138 native-fixture, 131 candidate Python** tests pass. Strict false-gate native
fixture build `600f55aa36d341b2bd34a2b7e08390fe`, SHA
`628cfe6187e3a246e6bd205697bb9990aca721ea91d7f7f0bf83ffd3a570a1f1`.

## Immediate next task — picker single-batch fixes, then confirming review

File-picker worker `f864bb8`, integrated **a808e84**, source-closed, no native run.
Review **3ca40b49-2c27-4fb9-a743-e33cb406949c** is **BLOCK**, not GO.
Read **docs/research/042-rendered-file-picker-review-block.md** and its linked full
report. Required fixes in one batch:

1. PS monotonic owned foreground/picker HWND latch matching Python; never allow
   initial foreign foreground again after main/picker gains foreground.
2. Atomic phase markers (temp→Move), `lexists` alias detection; only previously
   authenticated exact picker may disappear during close; end picker phase before
   info-dialog dismissal. FilePickerScene phase timeout25s, otherwise12s.
3. Hardlink checks on import/export/permit, exact permit length25; bounded owned
   file validation; reject dangling export; tag/basename privacy canaries.
4. Fold P2 fixed picker refusal tokens and exact Import/Export AutomationIds.

Conservative provider boundary remains same PID/main thread, direct main owner,
`GW_ENABLEDPOPUP`, exact #32770, fixed unique 1148/1001 filename and1/2 buttons.
Broker/other-thread/unknown schema BLOCKS; no title/desktop search, PID adoption,
focus workaround or keyboard/mouse synthesis. README now prominently says BLOCK.

After fixes: Python suite, candidate/fixture tests sequentially, PS parse, strict
management fixture BuildOnly, confirming medium review before enabling UI gate.
Then safe primary-landscape actual picker acceptance. No production edits needed.

## Native continuation after picker source checkpoint

Private native trial worktree is **behind root**:
`D:/sources/demo/smartsnippets/.claude/worktrees/uno019-native-trial`, branch
`feature/uno019-primary-native-trial`, HEAD **513d381**, clean/gates false.
It has primary-monitor-only candidate + target patches and old accepted binaries,
but lacks root62213a7/a808e84/0079f47. Do not run old pre-permit-gap fixture again.
Integrate current root carefully while preserving primary-only patches; branches
are divergent, so do not retry `merge --ff-only`, reset or clean. Fresh fixture
build and same-artifact restore→abort→owner-refusal controls before row/main runs.
Guardian d5e58... may remain byte-identical if its source is unchanged; verify.

Human shortcut lessons: F9 reportedly conflicted. Use **Ctrl+Alt+Shift+F11**.
Observe exact registered readiness, then **end the assistant turn** with “Press
now, release, reply pressed”; blocking a tool after a commentary prompt produced
25s timeouts. Source now supports registered-enter/row/main; temporary observer
scripts are still hardcoded to registered-enter and require deliberate updates.
Never kill pending coordinator/guardian/keyboard or fabricate settled authority.

Accepted old artifact/source/hashes/session paths are fully in research039/040.
Old temp helpers under `C:/Users/tamtr/AppData/Local/Temp/`:
`uno019-reviewed-preparation-trial.py`, `uno019-observe-registered-trial.py`,
`uno019-noninteractive-boundaries.py`. They are evidence/runbook references, not
new-source artifact authority. Update hashes, scene, witness and readonly checks.

## Other preserved worktrees and remaining gates

- `.../.claude/worktrees/uno019-ui-trial`, branch feature/uno019-primary-ui-trial,
  HEADd4987ec: old accepted primary UI variant, behind root.
- `uno019-native-route-matrix` HEADca0bbef; `uno019-file-picker-acceptance`
  HEADf864bb8; `uno019-registered-shortcut` HEAD47ab7e2: completed source lanes.
- Older018/Mac/isolation worktrees are preserved, not current implementation.

Remaining019: actual row/main/context consumers, popup/editor/footer Copy, picker,
selection/assignment/existing-item failure/relaunch settings, preview/menu/hover/
tray/coexistence, wrong-target/permission/contention/in-flight/sleep matrix,
final integrated AOT/legacy/hosted CI/wiring/privacy audit. Check plan, not this
summary, before marking criteria complete. Mac/mobile and oldest-runtime are
explicitly deferred, not PASS; ordinary candidate native warning is intentional.

## Handoff mechanism

Full tool-catalog search found **no CronCreate, open-terminal or compact/session
control tools** in this API session. No wake-up or self-compaction was invented,
no terminal input synthesis or alternative CLI used. This committed handoff is
ready for harness/context reset. Resume by reading this absolute-path document
and continue from “Immediate next task”; do not rerun accepted tests blindly.
