# Handoff 003 — Plan 019 leader (ss-leader), 2026-11-01 ~14:55

## Goal
Lead Plan 019 (`docs/plan/019-uno-shared-shell-and-platform-contracts.md`) to real completion. Claude is the leader:
delegates by tier, gates merges, books screen via pc-manager. Supersedes handoff 002.

## Standing user rules (also in memory)
- Tiers: XS/S `pi --model openai-codex/gpt-6-luna:xhigh`; M `gpt-6.1-sol:low`; L `sol:medium`; XL `sol:high`;
  scout `gpt-6-luna:medium`; reviewer `pi --model antigravity/claude-sonnet-5-5:<low|medium>`. Leader fixes tiny things.
  Brief: "authorized to edit and commit, do not stop short". No review for test-only/doc-only.
- Never ask the user for key presses; use posted-* scenes. Screen from pc-manager (SendMessage), bottom-left
  DISPLAY29 (-1920,1506 1920x1080) only, never primary. Send SCREEN-DONE release report after each run.
  Ask for EXCLUSIVE slots: concurrent sessions caused input/foreground failures (research 045 nr-04).
- **Push/CI only once at the final pass:** no pushes/PRs now; at the end push once, open one PR to `develop`
  (triggers uno-windows-candidate.yml), dispatch ci.yml once, merge when green. Never main without confirmation.
- Root has uncommitted operator edits to AGENTS.md and docs/guides/002: never commit/modify; copy into new worktrees;
  they block `git rebase` in worktrees, so bring commits to root with ff-merge or cherry-pick.

## State
- Root `D:/sources/demo/smartsnippets`, branch `feature/uno-desktop-feasibility` @ `3fef64e`. Contains: diag batch,
  picker any-thread observer (pt-01), Target018 foreground grant for posted scenes (pg-01), worker() behavioural
  tests, research 044/045, plan checkpoint refresh, hl-01 headless hardening.
- Full local regression at 1860faf: Core 436, Presentation 616, candidate 278 (289 after hl-01), fixture 153,
  Target018 86, Python 164; candidate AOT publish + legacy publish PASS, zero warnings.
- Native: controls PASS 4 runs; nr-05 (exclusive) restore control FAIL `native_control_admission ForegroundChanged`;
  picker reaches UIA control check then `picker_schema`; posted-* never reached the new grant path.
- Audit au-01 (scratchpad `audit-ac-report.md`): all AC PARTIAL; headless gaps H1–H11; native runs N1 (native chain
  incl. new Copy scenes), N2 (UI chain incl. picker), N3 (coexistence/hover/tray runner — not started).

## In flight (agents in termflow terminals; each reports to scratchpad `<id>-report.md`, last line `REPORT-COMPLETE <id>`)
| id | worktree (.claude/worktrees/) | tier | scope |
|---|---|---|---|
| el-01 | uno019-evidence-ledger | sol:low | H2 evidence ledger + MissingNativeEvidence test, H3 README matrix, stale claims |
| aw-01 | uno019-action-wiring | sol:medium | H1 EveryCandidateAction behavioural wiring test |
| nd-02 | uno019-native-diag2 | sol:low | picker control wait + fixed sub-reasons; control-admission foreground relation record |
| cs-01 | uno019-copy-scenes | sol:medium | native scenes popup-row/editor/footer Copy + context insert |
Scratchpad: `C:/Users/tamtr/AppData/Local/Temp/claude/D--sources-demo-smartsnippets/830f4403-f23f-4c8a-9d2e-977e63ae5f7c/scratchpad/`
(briefs `<id>-brief.md`, `common-header.md`, leader ledger `ledger.md` D1–D43).

## Next steps
1. Re-arm a Monitor on the four report files (they may already be complete). Read each report.
2. el-01: doc+test → merge without review if no production change. aw-01, nd-02, cs-01: review with
   `antigravity/claude-sonnet-5-5:medium` (brief pattern: scratchpad `review-pg-brief.md`, A/B/C/D, VERDICT), fold
   findings into the same agent, append ledger row to `docs/agentic/review-rounds-ledger.md`, merge into root
   (ff or cherry-pick; HostFixture.cs and native fixture overlap between branches is expected).
3. Rerun full regression (scratchpad `regress.sh`), then request an EXCLUSIVE bottom-left slot and dispatch one XL
   `sol:high` native run (template scratchpad `native-run5-brief.md` + gate file): controls → picker → posted-row/main/enter
   → new Copy scenes. Record in research 046.
4. Then N3 runner (coexistence/hover/tray) as an L dispatch, its run, final T8 audit, plan status, final push/PR.

## Gotchas
- Write long briefs with the Write tool (bash heredocs with quotes broke once). Check TUI is Ready before sending;
  if the prompt sits unsubmitted, send one empty submit, never the prompt twice.
- Positive scenes need a fresh same-parent control witness: a failed control blocks all posted scenes.
