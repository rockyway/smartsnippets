# Handoff 006 — Plan 019 leader (ss-leader), 2026-11-01 ~19:46

## Goal
Lead Plan 019 (`docs/plan/019-uno-shared-shell-and-platform-contracts.md`) to real completion. Supersedes handoff 005.
**User: time matters — avoid avoidable waiting; parallelize review with runs; pre-stage the next run; verify every
agent prompt actually submitted (re-read the screen after sending).**

## Standing user rules (also in memory files)
- Tiers: XS/S `pi --model openai-codex/gpt-6-luna:xhigh`; M `gpt-6.1-sol:low`; L `sol:medium`; XL `sol:high`;
  scout `gpt-6-luna:medium`; reviewer `pi --model antigravity/claude-sonnet-5-5:<low|medium>`. Leader fixes tiny
  things, verifies round-2 fold-ins itself (D46). No review for test-only/doc-only.
- Posted-* scenes only; never ask the user for key presses. Screen from pc-manager (SendMessage `pc-manager`);
  harness windows bottom-left DISPLAY29 only; runs need EXCLUSIVE full desktop (global foreground + input checks).
  User pre-approved runs of this scope. Send PHASE0-READY, then SCREEN-DONE release report.
- Grant file format MUST be the block (first line exactly `GRANTED nr-NN`, then `start: YYYY-MM-DD HH:MM local`,
  `hard end: ...`); an inline line does not open the helper gate (cost 16 min once).
- Push/CI only once at the final pass (one PR to `develop`, ci.yml once, merge when green). Never main.
- Root has uncommitted operator edits to AGENTS.md and docs/guides/002: never commit; copy into new worktrees;
  bring branch commits to root by cherry-pick.

## State (root `D:/sources/demo/smartsnippets`, branch `feature/uno-desktop-feasibility` @ 35c9ed4)
Since 005: research 054 (nr-11/11b) and 055 (nr-12); nd-06b; nd-07 + leader fold (A's-monitor work area) + nd-07b
(rv-15 blockers); review ledger row rv-15; pk-02 picker epoch timeline (2ff3369); fg-01 foreground root identity
(76bd000); test slice fix (35c9ed4). Leader ledger: scratchpad `ledger.md` (D1–D67).
- ROOT CAUSE (D63): the "same_pid_other" foreground "stealer" was A's own child EDIT. GetForegroundWindow returned
  the focused child and every observer compared it raw with top-level A. fg-01 normalizes WS_CHILD to its GA_ROOT
  only when the root equals the exact pinned HWND with the same pid and thread, and adds a `foreground_was_child` fact.
- Picker Import Cancel keeps failing `input epoch_changed` (delta 5–7 s, idle 0–31 ms) even with an exclusive
  desktop. Scout sc-pk places it in RunPicker's ≤6 s dialog wait after the main Import Invoke; no injection API.
  pk-02 adds a per-step epoch timeline, so nr-13 should localize it.
- nr-12: controls PASS, posted-row PASS, the other 5 positives FAIL (child-EDIT cause), picker FAIL.

## In flight
| id | where | state |
|---|---|---|
| nr-13 native run | worktree `.claude/worktrees/uno019-native-trial12` @35c9ed4, terminal tm-bbf017ab4 (sol:high), brief scratchpad `native-run14-brief.md`, report `native-run14-report.md`, grant file `nr13-grant.txt` (empty) | phase-0 REDO after its own helper_copy_encoding failure (no desktop used). pc-manager GRANTED 25 min starting at PHASE0-READY. Will commit research 056. |
| rv-16 review of fg-01+pk-02 (`git diff a5ebccf..35c9ed4`) | root, terminal tm-0683aa3b7 (sonnet medium), brief `review-fg-brief.md`, report `review-fg-report.md` | running |
Monitor bz93fxc7v watches both (may need re-arming after compaction). Scratchpad:
`C:/Users/tamtr/AppData/Local/Temp/claude/D--sources-demo-smartsnippets/830f4403-f23f-4c8a-9d2e-977e63ae5f7c/scratchpad/`

## Next steps
1. Re-arm a Monitor on `native-run14-report.md` (a new `PHASE0-READY nr-13`, a 2nd `SCREEN-DONE nr-13`,
   `REPORT-COMPLETE nr-13`) and `review-fg-report.md` (`REPORT-COMPLETE rv-16`).
2. On PHASE0-READY: write the grant block to `nr13-grant.txt` with start = now (HH:MM) and hard end = +25 min, same
   scope lines as `nr12-grant.txt`, and tell pc-manager the times.
3. On SCREEN-DONE: send pc-manager the release report (flags false, zero processes, clipboard state, per-scene
   results). Read native-monitor-failure.json / target-thread-windows-*.json / picker timeline straight from
   `%TEMP%/ss-uno018-ui-*/ss-uno019-native-*` (newer than the run start), without waiting for the write-up.
   Cherry-pick its research 056 commit.
4. rv-16: add its review-ledger row (docs/agentic/review-rounds-ledger.md); fold blockers (S or M), leader-verify.
5. If positives now pass: remaining Plan 019 work is the N3 native run (scenes in
   `scripts/uno_candidate_fixture/N3-README.md`), the picker epoch fix, the final T8 audit, the plan status update
   (backlog items for D49 coexistence and D50 posted-enter), full regression, then the final push and PR to develop + CI.

## Gotchas
- Derive run briefs with python replace (long, specific replacements first). A blanket `nr-N` -> `nr-N+1` replace
  corrupts historical references; check them after.
- pi TUI: send with cliType "pi", then re-read the screen to confirm `Working`. If the prompt is still sitting in
  the composer, submit it with an empty command.
- A git cherry-pick can fail on a transient index.lock from another worktree: check staged state, then `git commit -C <sha>`.
- After combining branches, run the Python suite on root. Slice-based PS tests break when functions are wrapped.
- Positive scenes need a fresh same-parent control witness each run (phase 1 controls).
