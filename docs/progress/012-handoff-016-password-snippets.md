# 012 — Handoff: plan 016 password-type snippets (leader)

**Date:** 2026-11-02 · **Session:** ss-leader

## Goal
Land plan `docs/plan/016-password-type-snippets.md` (approved 2026-11-02; §4.8 A1–A9 authoritative) on `feature/016-password-snippets`, then PR to `develop` (never direct `main`).

## Environment
- Main checkout `D:\sources\demo\smartsnippets`, branch `feature/016-password-snippets` (HEAD `a08fcf1` + this doc). Integration branch; all lanes merged here.
- Worktree `D:\sources\demo\smartsnippets\.claude\worktrees\016-fx1` (branch `feature/016-fx1`, same code as integration minus docs) holds the **measurement build** `publish-measure\SmartSnippets.exe` (strict AOT gate passed, build token `10ff2df935374eef8df3224512070df9`).
- Leftover lane worktrees `016-t2`, `016-t34`, `016-t5`, `016-t6`, `016-t7` are merged; remove them and their branches at the end (`git worktree remove`, `git branch -d`).
- Agent briefs/reports: `C:\Users\tamtr\AppData\Local\Temp\claude\D--sources-demo-smartsnippets\c7b02540-33da-4703-b649-ad521277db58\scratchpad\reports\` (`common.md`, `*-brief.md`, `*-report.md`).

## Done
- T1 flag/migration 3/manual label (`98d8788`); T2 DPAPI envelope + HostBoundary protector + startup message (`0b92cf2`); T3+T4 editor VM + PasswordBox control (`b5c0c54`); T5 masked projections/search/preview invalidation (`d342523`); T6 export exclusion (`974df9b`); T7 end-to-end tests (`abf1146`); v2 fixture fix by leader (`eb5e1d5`).
- Plan review `docs/review/014`, folded as §4.8. Code review round 1 (argon:high) `docs/review/015`: 1 blocker + 2 major D + 1 A + 4 B; all fixed in `54a41c8` with red/restore evidence (merged `a08fcf1`). Ledger row added in `docs/agentic/review-rounds-ledger.md`.
- T8 docs: backlog 001 status, user guide section in `docs/guides/001-install-and-release.md` (`51d9bfc`).
- Last full verification (fx1 lane, same code): Core 471, Presentation 648, Uno 178, all pass, zero warnings; strict AOT publish passed.

## In progress
- **Review round 2** (`pi --model antigravity/gemini-4-argon:high`) running in termflow terminal `tm-fa58d714b`; brief `reports\review2-brief.md`; output `reports\review2.md`, done tag `<<RV2-DONE>>`. Wait on it with a background monitor (don't poll the terminal); then close the terminal.

## Next steps
1. Read `review2.md`. Save as `docs/review/016-plan-016-code-review-round2.md`, add a ledger row, fix findings (small ones yourself; otherwise one fix lane, `pi` sol:low, worktree under `.claude/worktrees/`). Stop at round 3 per doctrine 08.
2. **Live check:** request a fresh screen slot from `pc-manager` (SendMessage). The previous grant was `\\.\DISPLAY33` at -1920,1506, 1920x1080, but device names change, so ask again. Scope: posted input only, no clipboard, no global hotkeys, no other apps. Launch the measurement exe as `scripts/measure-memory.ps1` does: root `%TEMP%\ss-mem-01-<guid>` containing a file `measurement-owned`, env `SMARTSNIPPETS_MEASURE_ROOT`/`_DISPLAY`/`_BOUNDS`, arg `--background`; commands via the `command` file (`main`, `hide-main`, `exit`), ack in `ack`. The app seeds 200 snippets and needs an empty root, so it can't be relaunched on the same root. Drive with the desktop-ui-automation skill (don't use `park`; the build places itself). Rows: (1) new snippet, check Password, PasswordBox visible, Custom-label toggle disabled, blank-label save fails; (2) label + save, row shows the 8-bullet mask, hover preview masked; (3) search on a secret substring misses, label hits; (4) DB row read-only (`smartsnippets.db` under root): `is_password=1`, text `dpapi:v1:…`, decrypts via PowerShell `ProtectedData.Unprotect(CurrentUser, entropy "SmartSnippets/password/dpapi/v1")` to the UTF-16LE secret; (5) UIA tree dump contains no secret; (6) `exit`, then 0 owned processes and root deleted. Send RELEASE to pc-manager with exit codes, process count and pass/fail per row.
3. Run all three suites sequentially, plus one AOT publish from a worktree (see gotchas), then update plan 016 status/approval log with the evidence (live rows vs automated), commit.
4. Push the branch and open a PR to `develop` (`gh pr create --base develop`); merge when green per the process. Pushing needs no extra confirmation, but `main` does.

## Gotchas / settled decisions
- `D:\sources\demo\smartsnippets\publish\SmartSnippets.exe` (PID 60224, started 1:14 PM, before this session) is the user's running copy. Don't kill it. It locks `publish\`, so `scripts/publish-uno-aot.ps1` fails at "artifact_check" (really the `Remove-Item` of `publish\`) **in the main checkout only**. Publish from a worktree.
- The paste UX is settled: a single-line paste replaces the whole password (PasswordBox has no selection API), and multiline paste is refused with a hint. Tell the user; don't relitigate.
- Agent tiers (user, 2026-11-02): XS/S terra:medium, M sol:low, L sol:medium, XL sol:high; scouting luna:medium; review `antigravity/gemini-4-argon`. Agents' common rules forbid launching the desktop app; the leader runs live checks.
- Run test projects sequentially only (CS2012). Uno tests run from `src/SmartSnippets.Uno`.
