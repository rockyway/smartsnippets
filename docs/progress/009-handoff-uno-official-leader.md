# Handoff 009 — ss-leader after the Uno official cutover (2026-11-01 ~23:59)
Supersedes 007. Outcome (2026-11-02): PR #7 merged to develop after CI fix 076aa42 (general test steps back to develop's Debug form); merged and archived branches cleaned up (unmerged work bundled in local `.claude/archive/`); cl-01 cleanup landed via `feature/uno-cleanup`.

## Goal
The Uno app (`src/SmartSnippets.Uno`) is now THE official Smart Snippets Windows app. The user live-tested it on their real data and everything works.
Remaining:
1. merge PR #7 to `develop` and delete the feature branch;
2. run the legacy/candidate cleanup (cl-01).

## User decisions
D80–D86: see the memory file `whole-app-functional-first`.
- Uno only, no legacy.
- Native-proof regime dropped. Verification is targeted tests plus one AOT publish plus the user's own live check. Full suites run once before a merge.
- Uno took over the legacy identity and profile: `SmartSnippets.exe`, `%LOCALAPPDATA%\SmartSnippets`, instance key `SmartSnippets.Main`, Run value `SmartSnippets`.
- Legacy and the old harness were deleted. Tag `legacy-winui-final` = 793c90b. The tag is local only; push it if the user wants it.
- Hotkey popup centres on the cursor's monitor; main window centres on the primary monitor.
- Hint shows on the Snippets page only; there is no header row on the other pages.
- Hotkey keys are accent chips.
- The user said: "complete pr, merge to develop and delete feature branch. then clean up the legacy app code".

## State
- Root `D:\sources\demo\smartsnippets`, branch `feature/uno-desktop-feasibility`, pushed. **PR #7 → develop**: https://github.com/rockyway/smartsnippets/pull/7.
- The first CI run failed on Windows: 2 timing tests (`Category=Budget`) failed inside the general Presentation step. Fix e770590 adds `--filter "Category!=Budget"` to the general Presentation steps; the budget tests keep their own isolated Release step, as on develop. CI is re-running, watched by monitor bp7clsxc1.
- Root commits after the cutover (10fa6ef/ad8f095):

  | Commit | Change |
  |---|---|
  | b5bb18e | hint shown on Snippets only |
  | 64d6e23 | no header gap (AlwaysShowHeader; page top padding 8) |
  | bfe7200 | accent chips |
  | e770590 | CI budget filter |

- **Operator files, uncommitted:**
  - `AGENTS.md`: I rewrote it to describe the single official Uno app.
  - `docs/guides/002-acceptance-harness-safety.md`: obsolete; it describes the deleted harness.

  Tell the user they can commit or delete them. Never commit them yourself.
- The user's real profile backup is in scratchpad `legacy-profile-backup/`.
- Ledger: scratchpad `ledger.md`.
- Old worktrees `uno-daily-native` and `uno-official` are merged; they can be removed. Their branches are local only.

## Next steps
1. When CI is green:
   - `gh pr view 7 --json mergeable,mergeStateStatus`
   - `gh pr merge 7 --merge --delete-branch`
   - delete the local branches `feature/uno-desktop-feasibility`, `feature/uno-daily-native` and `feature/uno-official`, and remove their worktrees. Check first that each is merged into develop.

   If CI is red: read the failing step, fix the cause (never weaken tests), push, and watch again.
2. **Cleanup cl-01:**
   - Worktree `.claude/worktrees/uno-cleanup`, branch `feature/uno-cleanup` from `origin/develop`. Copy `AGENTS.md` into it.
   - Brief: scratchpad `cl-01-brief.md`. Report: `cl-01-report.md`.
   - Dispatch `pi --model openai-codex/gpt-6.1-sol:medium` (M tier, mechanical).
   - When done: spot-check, add this handoff file, then PR to develop, CI once, and merge when green.
3. Update memory as needed. Give the user a short summary.

Scratchpad: `C:\Users\tamtr\AppData\Local\Temp\claude\D--sources-demo-smartsnippets\830f4403-f23f-4c8a-9d2e-977e63ae5f7c\scratchpad`
