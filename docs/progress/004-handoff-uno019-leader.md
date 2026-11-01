# Handoff 004 — Plan 019 leader (ss-leader), 2026-11-01 ~16:48

## Goal
Lead Plan 019 (`docs/plan/019-uno-shared-shell-and-platform-contracts.md`) to real completion as leader: delegate
by tier, gate merges, book screen via pc-manager. Supersedes handoff 003.

## Standing user rules (also in memory files)
- Tiers: XS/S `pi --model openai-codex/gpt-6-luna:xhigh`; M `gpt-6.1-sol:low`; L `sol:medium`; XL `sol:high`;
  scout `gpt-6-luna:medium`; reviewer `pi --model antigravity/claude-sonnet-5-5:<low|medium>`. Leader fixes tiny
  things and may verify round-2 fold-ins itself (D46). No review for test-only/doc-only.
- Never ask the user for key presses; posted-* scenes only. Screen from pc-manager (SendMessage `pc-manager`),
  bottom-left DISPLAY29 (-1920,1506 1920x1080) only. Ask EXCLUSIVE slots; send SCREEN-DONE release report after.
  pc-manager requires the USER's direct confirmation (not relayed) for any run with Copy-scene clipboard writes.
- Push/CI only once at the final pass: push once, one PR to `develop` (triggers uno-windows-candidate.yml),
  dispatch ci.yml once, merge when green. Never main without confirmation.
- Root has uncommitted operator edits to AGENTS.md and docs/guides/002: never commit; copy into new worktrees;
  bring branch commits to root by cherry-pick.

## State
- Root `D:/sources/demo/smartsnippets`, branch `feature/uno-desktop-feasibility` @ `8bcf79c`. Merged since 003:
  el-01 evidence ledger (fingerprint binds only on PASS rows, D45), nd-02 diag + rv-08 fixes, aw-01 action audit
  (90 XAML actions), cs-01 Copy scenes + rv-09 fixes, nd-03 monitor policy + rv-10 fixes, nd-04 startup foreground
  diagnostics, research 046/047/048. Review rows in `docs/agentic/review-rounds-ledger.md`.
- Full regression at 735cac4 PASS: Core 436, Pres 616, candidate 310, fixture 169 (185 after nd-04), T018 86,
  Python 191 (193), both publishes 0 warnings.
- Native: nr-06 (046) controls PASS; nr-07 (047) controls PASS and **posted-row PASS end to end** (first native
  positive PASS). posted-main + 4 Copy scenes FAIL preparation ForegroundRefused (owned Target018 A cannot take
  foreground on later scenes; Windows foreground rules, research 048); posted-enter host_ready; picker input guard.

## In flight
| id | where | state |
|---|---|---|
| nr-08 diagnostic native run | worktree `.claude/worktrees/uno019-native-trial7` @8bcf79c, terminal tm-c42897cce (sol:high), brief scratchpad `native-run9-brief.md`, report `native-run9-report.md`, grant file `nr08-grant.txt` (empty) | phase 0 builds, then waits for `GRANTED nr-08` line. pc-manager QUEUED it pending the user's direct confirmation; user also asked to keep input idle. |
| n3-01 N3 shell scenes | `.claude/worktrees/uno019-n3-shell`, commit 2f7acd9 | done PARTIAL; review rv-11 running in tm-fa34a042b, report scratchpad `review-n3-report.md` (`REPORT-COMPLETE rv-11`). Implementer terminal tm-e59d9e64b still open for fold-ins. |
Scratchpad: `C:/Users/tamtr/AppData/Local/Temp/claude/D--sources-demo-smartsnippets/830f4403-f23f-4c8a-9d2e-977e63ae5f7c/scratchpad/`
(leader ledger `ledger.md` D1–D48, `regress.sh`, briefs, reports).

## Next steps
1. Arm Monitors on `review-n3-report.md` (rv-11) and `native-run9-report.md` (nr-08), and watch for pc-manager's
   GRANTED nr-08 message. On grant: write `nr08-grant.txt` with `GRANTED nr-08`, window start/hard end, scope
   (controls; posted-row/main/enter; 4 Copy scenes; optional picker Import Cancel if >=5 min remain).
2. rv-11: fold findings into n3-01 (tm-e59d9e64b), append ledger row, verify, cherry-pick 2f7acd9+fix to root.
3. After nr-08: SCREEN-DONE to pc-manager, cherry-pick doc 049 to root, read native_startup_foreground samples
   and decide the foreground fix (dispatch L agent). Then another native slot.
4. USER DECISION pending: N3 coexistence is BLOCKED because the legacy app hardcodes production `AppPaths` and has
   no owned-profile bootstrap. Options: add a reviewed legacy owned-profile launch option (legacy product change),
   or accept coexistence as not-native-closable in 019. Ask with AskUserQuestion.
5. Then N3 native run, final T8 audit, plan status update, final push/PR to develop + CI per user rule.

## Gotchas
- Write long briefs with the Write tool; derive new run briefs from the previous one (python replace, order matters).
- Check TUI Ready before sending; one empty submit if a prompt sits unsubmitted.
- Positive scenes need a fresh same-parent control witness each run.
- cherry-pick conflicts recur in `tests/SmartSnippets.Uno.Tests/SmartSnippets.Uno.Tests.csproj` Compile line: keep both sides.
