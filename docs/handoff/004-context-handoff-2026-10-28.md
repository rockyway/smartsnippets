# 004 — Context handoff (2026-10-28 ~20:15, ui-qa merged, stability work building)

**Repo:** `D:\sources\demo\smartsnippets` (branch `main` at `218035d` + this handoff, local only, NOT pushed). Read `AGENTS.md` first. Supersedes 003.
Role: I am the orchestrator ("ss-leader", id `smartsnippets-d5` with the PC manager). All implementation is delegated to `pi` terminal agents via termflow MCP.

## Done (local `main`)
- Plans 006/007/008/010 merged earlier (see 003). Then `feature/ui-qa` merged as `218035d`: real-UI QA fixes, one-row label-only lists (popup + Snippets page), right-click Copy/Insert/Edit/Delete menu, Settings "Preview global dialog" (= hotkey popup preview, no paste/clipboard/usage), launch seam args (`--page`, `--preview-popup`), borderless/topmost popup, min-size DPI fix, redesigned Import/Export icons (U+E896/U+E898), review fixes. Verified on main: build 0 warnings, Core 426, Presentation 446, AOT publish OK. User runs `D:\sources\demo\smartsnippets\publish\SmartSnippets.App.exe` (do not delete `publish\`).
- Worktree/branch ui-qa removed.

## In progress: stability branch
- Worktree `D:\sources\demo\smartsnippets\.claude\worktrees\stability`, branch `feature/stability-popup-cache` (9 commits over main, tip ~`6c097f8`). Agent: terminal `tm-796af71ac` (pi, sol:medium).
- User report fixed by the agent (UNVERIFIED until build + screen/user check): (A) Snippet editor Folder shows "unfilled" after A -> Back -> B (TwoWay ComboBox transient null; replaced by guarded explicit user inputs); (B) overlay flashing white->black (popup themed before prewarm unmask; cause not confirmed on screen); (C) pre-warmed popup + projection cache with coalesced invalidation, rebuild-before-show when dirty, closed-window recreation, `popup_show_ms` logging.
- Build `stab-01` GRANTED by pc-manager (slot 6, 4 cores, ~15 min from ~20:12). Agent builds from a SNAPSHOT on C: (`C:\temp\ss-stab-build`, publish to `C:\temp\ss-stab-build\publish`) because D: is full (~854 MB free). Report: `...\scratchpad\report-stab.md` (ends STAB_COMPLETE or BLOCKED). Watcher armed on `proto\build-request.txt` for `BUILD_FINISHED stab-01`.
- scratchpad = `C:\Users\tamtr\AppData\Local\Temp\claude\D--sources-demo-smartsnippets\4a21892f-b72e-4767-862a-26efacb4fb6b\scratchpad`.

## Next steps
1. On `BUILD_FINISHED stab-01`: send `BUILD-DONE smartsnippets-d5` to `pc-manager [292bc3]` (SendMessage); read report-stab.md (counts, warnings).
2. Request a DISPLAY9 screen slot from pc-manager (short, ~15 min) for first-frame colour/timing checks via launch seam; the folder bug needs pointer clicks, so ask the user to verify it by hand on the new build (give them `C:\temp\ss-stab-build\publish\SmartSnippets.App.exe`).
3. Independent static review (sol:medium, separate terminal, A/B/C/D classes) of `main..feature/stability-popup-cache`; fix; re-review once.
4. Verify locally on merged main using a C: snapshot build (see D: gotcha), merge `--no-ff` into main, remove worktree+branch, close terminals. Ask before pushing.

## Protocols (settled)
- Shared PC: never build/launch/drive UI without a GRANT from pc-manager (`SendMessage` to `pc-manager [292bc3]`): build slot = 4 cores/~15 min; screen slot = DISPLAY9 only (X=-1920 Y=1506), posted/no input, HWND-only probes, never touch/read Rephlo, only own SmartSnippets.App; DISPLAY10 (-1920,306) belongs to rpl-orch1. Send `BUILD-DONE` / `SCREEN-DONE smartsnippets-d5` when finished.
- Agent build protocol: agent writes `BUILD_REQUEST <id> ...` to `scratchpad\proto\build-request.txt` (watched); I request from pc-manager, then TYPE `BUILD GO <id>` into the agent terminal (do not rely on the response file; the agent does not poll). Agent writes `BUILD_FINISHED <id>`.
- Tiers: XS/S terra:low, M terra:medium, L terra:high, XL sol:medium (terra stalls on big tasks), review sol:low|medium. Process: plan->review->fold->implement->review->fix->re-review->verify->merge. Fix minor things myself.
- Posted mouse/keyboard input does NOT reach WinUI content; UI verification uses launch args seam + PrintWindow only. Real clicks (context menu, tooltips, folder bug, pickers, hotkey, tray, insertion) are PENDING-USER.

## Gotchas
- **D: is full** (0-854 MB free); I deleted only my own bin/obj of main (pc-manager notified). Build from a C: snapshot (robocopy sources excluding .git/bin/obj/publish/docs\qa/*.png), never write build output to D:. Others' D:\temp\417* folders are not mine.
- PATH for AOT publish needs `C:\Program Files (x86)\Microsoft Visual Studio\Installer` (vswhere).
- Run Core then Presentation tests sequentially; `-m:4`.
- If an unowned `SmartSnippets.App` runs (single-instance), harness refuses; the user's instance runs from `publish\`: ask before closing (user approved closing `D:\temp\smartsnippets.app-dev` once).
- Pi prompts sometimes sit unsubmitted: check the screen, send an empty `execute_command` (cliType pi).
- PENDING-USER: real-input acceptance (above), plan 010 manual runbook, CI calibration (needs push), T14 audit, release tag, any push of `main`.
