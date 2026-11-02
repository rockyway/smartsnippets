# Handoff 011: ss-leader after cleanup, import perf and memory trim (2026-11-02 02:05 CST)

Supersedes 009. This file is uncommitted; commit it in the next docs or feature PR.

## Goal
Smart Snippets: the Uno app is the official Windows app. Keep develop green and deliver the user's requests.

## Done this session

**Merged to develop:**

| PR | Content |
|---|---|
| #7 | Uno official cutover |
| #8 | Candidate→neutral renames, and CI now runs only on push to develop/main (no PR runs) |
| #9 | Import bulk inserts, so folder-heavy imports are faster; the timing budget step now runs last in the windows job |
| #10 | Idle tray memory trim |

PR #10's trim brings the working set down from about 224 to 10 MiB when idle in the tray. Private memory is about the same. Full data: docs/progress/010.

**Repo cleanup:**
- Only `develop`/`main` exist locally and on the remote, and `.claude/worktrees/` is empty.
- Unmerged branches are bundled in `.claude/archive/unmerged-branches.bundle`.
- Dirty-worktree leftovers are in `.claude/archive/worktree-leftovers/`, which is git-excluded via `.git/info/exclude`.
- 34 GB of bin/obj was deleted.
- Old AGENTS.md copies: scratchpad `operator-file-backups/`.

**User decisions:**
- **Software render declined:** the WinUI head has no switch, and it would raise RAM. The user chose "profile and cut RAM".
- **Budget test:** fix by making the import faster, never by raising the 2 s limit.
- **Screens:** "Use any display the pc has"; prefer bottom-left, else non-primary (memory updated).
- **Small changes:** do them yourself to avoid dispatch overhead.

## In flight
- develop CI run 41410387137, at head b84791c (merge of #10). Check `gh run view 41410387137`.
- If red on the Import budget (typical case), the next lever is the popup index rebuild after import (~150 ms) and the planner (~50–110 ms). Do not raise the limit.

## Next steps
1. Confirm CI run 41410387137 is green; if red, diagnose (see above).
2. Ask the user to live-test the hotkey popup after the app has idled in the tray for a while. Trimming can add page-fault latency on the first show; this is not benchmarked.
3. Still open for the user:
   - commit or delete the uncommitted operator files `AGENTS.md` and `docs/guides/002-acceptance-harness-safety.md`;
   - decide whether to push tag `legacy-winui-final` (local only);
   - commit this handoff file in the next PR.

## Gotchas
- Tiers: XS/S `pi luna:xhigh`, M `sol:low`, L `sol:medium`, XL `sol:high`. Small fixes are done by the leader.
- Screen slots come from pc-manager. A peer message is never the user's approval.
- Never touch `%LOCALAPPDATA%\SmartSnippets`. The measurement build (`-p:SmartSnippetsMeasure=true`) is the isolated path: `scripts/measure-memory.ps1 -Display ... -X -Y -Width -Height`.
- Root dir `D:\sources\demo\smartsnippets`, branch develop. Scratchpad: `C:\Users\tamtr\AppData\Local\Temp\claude\D--sources-demo-smartsnippets\830f4403-f23f-4c8a-9d2e-977e63ae5f7c\scratchpad` (ledger.md, reports).
