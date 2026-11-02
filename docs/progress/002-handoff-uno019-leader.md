# Handoff 002 — Plan 019 leader (ss-leader), 2026-11-01 13:21

## Goal
Lead Plan 019 (`docs/plan/019-uno-shared-shell-and-platform-contracts.md`) to real completion.
Claude is the leader: delegates by tier, gates merges, books screen via pc-manager.

## Standing user rules (also in memory)
- Agent tiers: XS/S `pi --model openai-codex/gpt-6-luna:xhigh`; M `pi ... gpt-6.1-sol:low`;
  L `sol:medium`; XL `sol:high`. Scout `gpt-6-luna:medium`.
  Reviewer: `pi --model antigravity/claude-sonnet-5-5:<low|medium>`.
  Leader fixes small things directly. Tell agents "authorized to edit and commit, do not stop short".
- **Do not ask the user** for key presses or approvals during dev/test; automate. Use
  `posted-row` / `posted-main` / `posted-enter` instead of operator `registered-*`
  (record the limit: `registered_posted_callback_not_physical`; HID delivery proven in research 039).
- Screen: request from peer session `pc-manager` via SendMessage; send a release report afterwards.
  **Bottom-left monitor only** (live bounds -1920,1506 1920x1080, work -1920,1506 1920x1032;
  DISPLAY names change), never primary. Clipboard seed `"Smart Snippets QA" | clip.exe` only with
  no pending recovery; state it in the grant request.
- Review process: <=2 rounds normally; this batch is now at round 3 (see below).

## Branches / worktrees (all under D:/sources/demo/smartsnippets/.claude/worktrees/)
- Root `D:/sources/demo/smartsnippets`, branch `feature/uno-desktop-feasibility` @ `21df97f`
  (picker fixes merged). Root has uncommitted **operator edits to AGENTS.md and
  docs/guides/002** — never commit or modify them; copy into new worktrees.
- `uno019-diag-fixes`, branch `feature/uno019-diag-fixes` @ `204970a` = 21df97f + research 043
  commits + diagnostics batch (eb27913, ddc0f8b, a41004b, 204970a). **Not yet merged to root.**
- `uno019-native-trial2`, branch `feature/uno019-primary-native-trial2` @ `530f806`
  (trial worktree used for native runs; equals root code + research 043). Rebuild from
  diag-fixes before the next run.

## Done this session
- Picker review-block fixes (d24a0fd, 77075a3, 21df97f), review GO, ff-merged to root.
- Native run nr-01/nr-02 on bottom-left: restore/abort/owner-refusal controls PASS (twice);
  file-picker BLOCKED at Import Cancel (`foreground_changed`/`owned_other`); registered-row FAIL
  at startup `InvalidOperationException` before guardian-ready; registered-main NOT RUN.
  Evidence: `docs/research/043-bottom-left-controls-and-picker-acceptance.md`. Flags all closed.
- Scouts: no unique cause for either failure. Diagnostics batch added: picker open-time race fix
  + one-shot recheck, fixed-schema foreground-failure records (Python+PS), pre-ready native stage
  tokens (`NativeStage.cs`, `failureStage` in runner result).
- Review rv-03 BLOCK → a41004b; rv-04 BLOCK (two new D in worker() error path from round-1 fix)
  → leader folded directly in `204970a` (helper exit validated before failure file; diagnostics
  errors never skip failure-file validation/recording; full 13-field PS record test). 156 Python
  tests pass at 204970a; 148 fixture / 278 candidate passed at a41004b (no C# change since).

## Next steps
1. Round-3 review of `204970a` only (reviewer `pi antigravity/claude-sonnet-5-5:medium`; brief
   pattern: scratchpad `diag-review2-brief.md`, A/B/C/D classes, VERDICT line). Round 3 = per
   doctrine look at root cause if it blocks again rather than patching symptoms.
2. On GO: ff-merge `feature/uno019-diag-fixes` into root; rebuild trial worktree from it.
3. Request bottom-left slot from pc-manager (state: posted scenes, picker scene, clipboard seed,
   no operator input). Dispatch XL `pi sol:high` native run: controls → picker → posted-row →
   posted-main (→ posted-enter). Diagnostics now name the failing window/stage.
4. Fix based on diagnostics; then remaining Plan 019 rows (Copy paths, hover/tray/coexistence,
   fault matrix, final audit T8). Update plan tracker only with real evidence.

## Gotchas
- terra agents stopped short twice; agy TUI sometimes needs an extra Enter after the trust prompt;
  don't send the prompt twice. The Agent tool with `isolation: worktree` commits in its own branch.
- `pi` reports: always pre-create report file and watch it with a Monitor (report tag line).
- Leader ledger (decisions D1–D20): `C:/Users/tamtr/AppData/Local/Temp/claude/D--sources-demo-smartsnippets/830f4403-f23f-4c8a-9d2e-977e63ae5f7c/scratchpad/ledger.md`; briefs/reports live there too.
- No pushes/PRs made; nothing pushed to main.
