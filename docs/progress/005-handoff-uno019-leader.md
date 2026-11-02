# Handoff 005 — Plan 019 leader (ss-leader), 2026-11-01 ~18:20

## Goal
Lead Plan 019 (`docs/plan/019-uno-shared-shell-and-platform-contracts.md`) to real completion. Supersedes handoff 004.
**User: time matters — avoid avoidable waiting; parallelize review with runs; pre-stage the next run.**

## Standing user rules (also in memory files)
- Tiers: XS/S `pi --model openai-codex/gpt-6-luna:xhigh`; M `gpt-6.1-sol:low`; L `sol:medium`; XL `sol:high`;
  scout `gpt-6-luna:medium`; reviewer `pi --model antigravity/claude-sonnet-5-5:<low|medium>`. Leader fixes tiny
  things and verifies round-2 fold-ins itself (D46). No review for test-only/doc-only.
- Posted-* scenes only; never ask user for key presses. Screen from pc-manager (SendMessage `pc-manager`),
  bottom-left DISPLAY29 (-1920,1506 1920x1080) only. Send PHASE0-READY, then SCREEN-DONE release report after.
  **User pre-approved bottom-left runs of this scope** (per pc-manager), so no per-run confirmation needed now.
- Push/CI only once at the final pass (one PR to `develop`, ci.yml once, merge when green). Never main.
- Root has uncommitted operator edits to AGENTS.md and docs/guides/002: never commit; copy into new worktrees;
  bring branch commits to root by cherry-pick.

## State
- Root `D:/sources/demo/smartsnippets`, branch `feature/uno-desktop-feasibility` @ `453a11b`. Since 004 merged:
  N3 shell scenes + rv-11 fixes, pk-01/01b picker operationClass/pickerStep, nd-05/05b foreground trace + picker
  epoch fields, nd-06 Target018 other-window class tokens + picker Invoke epochs, research 049–053, ledger rows.
- Decisions: D49 coexistence NOT native-closable in 019 (user); D50 posted-enter excluded (SendInput);
  D53/D55 run while reviewing diagnostics-only changes. Leader ledger: scratchpad `ledger.md` (D1–D55).
- Native evidence: controls PASS and posted-row PASS end to end in nr-07, nr-09, nr-10. posted-main + 4 Copy
  scenes always FAIL ForegroundRefused at guardian_ready: right after UIA SetFocus, A is foreground 1–3 samples,
  then another top-level window of the Target018 PID (not A/B/IME/tooltip/owned-by-A) holds it (research 052).
  Picker Import Cancel FAILs input epoch_changed (delta 5875, idle 31 ms) with operator idle.

## In flight
| id | where | state |
|---|---|---|
| nr-11 native run | worktree `.claude/worktrees/uno019-native-trial10` @cad80bf, terminal tm-5091a2f00 (sol:high), brief scratchpad `native-run12-brief.md`, report `native-run12-report.md` | GRANTED 18:16–18:41, running. Will commit research 054. |
| nd-06b rv-14 fold-in | worktree `.claude/worktrees/uno019-native-diag6b` @cad80bf, terminal tm-b80df5c59 (luna:xhigh), brief `nd-06b-brief.md`, report `nd-06b-report.md` | running |
Scratchpad: `C:/Users/tamtr/AppData/Local/Temp/claude/D--sources-demo-smartsnippets/830f4403-f23f-4c8a-9d2e-977e63ae5f7c/scratchpad/`

## Next steps
1. Arm Monitors on `native-run12-report.md` (`REPORT-COMPLETE nr-11`) and `nd-06b-report.md` (`REPORT-COMPLETE nd-06b`).
2. nr-11 done: SCREEN-DONE to pc-manager (release proof + per-scene results), cherry-pick its research 054 commit
   to root, read the windowFacts/class tokens for the stealing window and the picker Invoke epochs.
3. With that identity, dispatch the fix (L sol:medium) at once; pre-stage the next run brief (derive from
   `native-run12-brief.md`) and pre-queue the slot with pc-manager in the same step.
4. nd-06b done: leader-verify, cherry-pick to root (resolve conflicts keeping both sides).
5. Then: N3 native run (scenes in `scripts/uno_candidate_fixture/N3-README.md`, gate closed until trial), final
   T8 audit, plan status update (backlog item for coexistence D49 and posted-enter D50), full regression, final
   push/PR to develop + CI per user rule.

## Gotchas
- Derive run briefs with python replace (long, specific replacements first); check guard/text references after.
- Check TUI Ready before sending; pi TUI submits with cliType "pi".
- cherry-pick conflicts recur in the test csproj Compile line and native fixture `App.xaml.cs` diagnostics ctor: keep both.
- Positive scenes need a fresh same-parent control witness each run (phase 1 controls).
