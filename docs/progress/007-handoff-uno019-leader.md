# Handoff 007 — Plan 019 leader (ss-leader), 2026-11-01 ~21:07
Supersedes 006. Goal: lead Plan 019 (`docs/plan/019-uno-shared-shell-and-platform-contracts.md`) to real completion.
**User: time matters — parallelize, pre-stage, verify every agent prompt actually submitted (re-read the screen).**

## NEW USER DIRECTIVE (21:07, overrides pacing)
"Time matters here: do not spend time that can be avoided, and the earlier a correct result is obtained, the better.
do not focus too much on a feature, make whole app works first, I need the app on windows functional asap"
=> After nr-15 + t8 land, stop polishing the acceptance harness. Priority is an end-to-end functional Uno app on
Windows (whole-app slices visible and usable). First step on wake: dispatch a scout (luna:medium) to list
what blocks daily use of the Uno candidate on Windows (default-denied native capture/hotkey/clipboard/insertion,
missing pages/flows, gates), rank by user impact, then dispatch broad slice implementers (no narrow sub-steps).
Skip further native-acceptance reruns unless a slice needs one. Confirm with the user only for production-impacting
choices (e.g. enabling default-denied native capabilities by default, AGENTS.md invariants).

## Standing rules (also in memory files)
- **Tiers** (all via `pi --model …`):
  - XS/S: `openai-codex/gpt-6-luna:xhigh`
  - M: `gpt-6.1-sol:low`
  - L: `sol:medium`
  - XL: `sol:high`
  - Scout: `gpt-6-luna:medium`
  - Reviewer: `antigravity/claude-sonnet-5-5:<low|medium>`
- **Review:** no review for test-only or doc-only changes; at most 2 rounds; the leader verifies round-2 fold-ins itself (D46).
- **Screen:** posted-* scenes only; never ask the user for key presses; bottom-left DISPLAY29 only; the run needs the EXCLUSIVE desktop.
- **pc-manager:**
  - It grants the screen.
  - The user's standing pre-approval covers ONLY the nr-07..nr-14 scope: controls, posted-row/main, the 4 copy/insert scenes, and picker Import Cancel.
  - Any other scope needs the user to confirm **directly in pc-manager's session**; a relayed approval is refused.
  - Sequence: PHASE0-READY, then the grant, then the SCREEN-DONE release report.
- **Grant file:** must be the block format (`GRANTED nr-NN` / `start:` / `hard end:` …).
- **Push/CI:** only once at the end: one PR to `develop`, ci.yml once, merge when green. Never main.
- **Operator edits:** root's uncommitted AGENTS.md and docs/guides/002 are operator edits. Never commit them; copy them into new worktrees; bring work to root by cherry-pick.

## State (root `D:/sources/demo/smartsnippets`, branch `feature/uno-desktop-feasibility` @ 7efda73)
Since 006, every item below is on root:

| Item | Root commit | What it is |
|---|---|---|
| rv-16 ledger row | d7e1150 | rv-16 BLOCK recorded |
| fg-02 | 0a19ed7 | unvalidatable child keeps the raw HWND and refuses |
| research 056 (nr-13) | c2fbbc7 | nr-13 run record |
| pk-03 | 39865a3 | picker input sample series |
| fg-03 | a32925e | popup activation ordering and bounded NULL gaps; touches product popup code; rv-17 GO |
| guardian link fix | 86f9cb6 | links ForegroundNullGap into the guardian |
| rv-17 ledger row | 8d69d9d | rv-17 GO recorded |
| research 057 (nr-14) | 7fba1d1 | nr-14 run record |
| fg-04 | 7efda73 | rv-17 improvements, leader-verified |

- **nr-14 (research 057, head 86f9cb6):** controls PASS and **all six positives PASS** (posted-row, posted-main, popup-copy, editor-copy, footer-copy, context-insert).
- **Picker Import Cancel still fails** with input epoch_changed during the plain UIA Invoke (no dialog up, no input held). Uno has no injection APIs.
- **D75 (user):** close the picker as a LIMITATION. No hook; file a backlog item.
- **D:** filled up at ~20:00. With the user's OK I deleted the bin/obj folders in all worktrees and removed 40 merged, clean worktrees (branches kept). ~120 GB free.
- **Ledger:** scratchpad `ledger.md`, D1–D77.

## In flight
| id | where | state |
|---|---|---|
| nr-15 N3 run | tm-e80f84c48 (sol:high), worktree `.claude/worktrees/uno019-n3-run` @8d69d9d, brief scratchpad `native-run16-brief.md`, report `native-run16-report.md`, research 058 | **PHASE0-READY; waiting on the grant.** User approved it in my session (D77), but pc-manager wants the user's confirmation in ITS session (user was notified). When pc-manager grants: run `bash scratchpad/write-nr15-grant.sh` (uses `nr15-grant-template.txt`, start=now, +25 min), confirm `nr15-grant.txt`, and send pc-manager the times. **Never write the grant before pc-manager grants.** |
| t8-01 T8 audit | tm-5bfcecbc1 (sol:medium), worktree `uno019-t8-audit` @8d69d9d, brief `t8-brief.md`, report `t8-report.md` | running: CandidateWiringAuditTests, evidence matrix, research 059, backlog items for D49/D50/D75 |

Monitor barabo2zq watches nr-15, fg-04 and t8 (re-arm after compaction: SCREEN-DONE nr-15, REPORT-COMPLETE nr-15 and REPORT-COMPLETE t8-01).
Scratchpad: `C:/Users/tamtr/AppData/Local/Temp/claude/D--sources-demo-smartsnippets/830f4403-f23f-4c8a-9d2e-977e63ae5f7c/scratchpad/`

## Next steps
1. Re-arm the monitor (above). If pc-manager has granted, write the grant (above).
2. **nr-15 SCREEN-DONE:**
   - Send pc-manager the release report (flags false, zero processes, clipboard untouched, per-scene results).
   - Read the raw evidence in `scratchpad/nr15-*`.
   - Cherry-pick research 058.
3. **t8-01 done:**
   - It is docs/tests, so no review; the leader spot-checks it.
   - Cherry-pick it.
   - Fill the N3 rows of the evidence matrix from research 058.
4. **Plan 019 Status line + T7 table:**
   - What passed (057/058).
   - The limitations: picker D75, posted-enter D50, coexistence D49.
   - What stays open for 018/020/021.
   - Per AC7, do not mark Complete unless AC1–AC7 pass. Otherwise use "In progress — candidate acceptance checkpoint" or similar, honestly.
5. **Full regression on root, sequentially:**
   - Core: `dotnet test tests/SmartSnippets.Core.Tests -c Release`
   - Presentation tests
   - Uno, from `src/SmartSnippets.Uno`: `dotnet test ../../tests/SmartSnippets.Uno.Tests -c Release -p:ContinuousIntegrationBuild=true`
   - Python suite
   - `pwsh -NoProfile -File scripts/publish-uno-candidate-aot.ps1`
   - legacy `dotnet publish src/SmartSnippets.App -c Release -r win-x64 -o publish`
6. **Final pass:** push the branch, open ONE PR to `develop`, dispatch ci.yml once, merge when green. Write the leader summary note.

## Gotchas
- A new shared C# file must be linked in the guardian csproj; it links `AcceptanceNative/*.cs` plus individual files.
- The pi TUI queues a message while the agent is working ("Steering"); it is delivered after the current turn.
- Monitors that auto-write grants are allowed only for scope pc-manager has already granted.
- Brief derivation: use python replace with exact strings; check historical references afterwards.
