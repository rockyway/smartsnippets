# 001 — Context handoff (2026-10-27)

**Branch:** `claude/epic-allen-07hez6` (pushed, clean at `7836ccb` when this was written)
**Repo:** `rockyway/smartsnippets`
**Requirement:** [`docs/requirements/001-prd.md`](../requirements/001-prd.md)
**Master plan:** [`docs/plan/001-master-smart-snippets-v1.md`](../plan/001-master-smart-snippets-v1.md)

## 1. Goal

Build Smart Snippets v1: a .NET 10 + WinUI 3 + Native AOT + SQLite Windows utility. A global
hotkey opens a popup, you search for a snippet, and it is inserted into the app that had focus.
A main window manages snippets, folders and settings.

The work runs as a master plan (001) plus sequential subplans (002–008), using the planning
skill's gates:
- **G1:** requirement confirmed.
- **G2:** plan approved.
- **Implementation:** starts only on an explicit user instruction.

**Current user instruction:** "implement 003–005 with Sonnet subagents."

## 2. Status by subplan

| # | Plan | Status | Notes |
|---|---|---|---|
| 001 | master | Approved | progress table in §5/§9 |
| 002 | tech validation prototype | **Complete** (user-marked) | CI AOT publish green, 0 IL warnings; user's harness `scripts/verify-plan002.ps1` passed G-1…G-5 on their PC |
| 003 | core domain & persistence | **In progress** | Tracks A and B running (see §4) |
| 004 | app shell, lifecycle, settings | Approved, not started | written against 003's *planned* API: re-check before starting |
| 005 | main window management | Approved, not started | written against 003/004 planned API: re-check before starting |
| 006–008 | popup / insertion / release | Placeholder | not detailed |

## 3. Key decisions (all recorded in the plans)

**Stack**
- Unpackaged, self-contained Windows App SDK **1.8.260921001**, **x64 only**, Win10 1809+.
- Native AOT is required. The CI warning gate fails on any `warning IL2xxx/IL3xxx`.
- Packages: `Microsoft.Data.Sqlite` 10.0.14 with hand-written SQL (no EF), CsWin32 0.3.367
  (`allowMarshaling:false`, so `AllowUnsafeBlocks` is on), CommunityToolkit.Mvvm 8.4.2, xUnit v2.

**Behaviour choices**
- **Insertion:** clipboard paste with save and restore (250 ms restore delay, constant). An
  elevated target is detected and gets a message. "Restart as administrator" is manual.
- **Start at login:** HKCU `Run` key, always un-elevated.
- **PRD proposed rules:** all accepted.

**Verification**
- Core and Presentation tests run on Linux and Windows CI.
- Windows-only behaviour is covered by the user's runbooks or harness.

**Plan 003 design calls (approved)**
- Tags are stored as one comma-joined column.
- `folders.name_key` = `ToUpperInvariant(trimmed)` with a UNIQUE index, because SQLite `NOCASE`
  is ASCII-only.
- Times are Unix-ms INTEGERs and IDs are rowid.
- `folder_id ... ON DELETE RESTRICT`. Deleting a folder explicitly moves its snippets to Unfiled
  and bumps their `updated_at`.
- `RecordUsage` never touches `updated_at`.
- **Migration 1 (`probe`) stays until 008.** Live user databases are at v1, so the new schema
  is migration 2. A newer-version guard and a `VACUUM INTO` backup run before any upgrade.
- A cleared label returns to automatic.
- Search uses `OrdinalIgnoreCase` substring with AND across terms, no accent folding, and
  "Unfiled" is not searchable.
- Folder grouping puts Unfiled last.

**Plan 004 design calls (approved)**
- A new **`SmartSnippets.Presentation`** project (net10.0) holds the view-models and service
  interfaces, so they can be tested on Linux.
- An `AppHost` composition root.
- The hotkey is changed register-new-then-unregister-old using a spare ID.
- If the saved hotkey fails at startup, the app keeps running unregistered, shows a balloon,
  and does not fall back to the default on its own.
- A shortcut must include Ctrl, Alt or Win.
- Settings save immediately.
- Text size is bound through `ITextSizeService.FontSize` via `x:Bind`.
- Diagnostics stays in the tray menu until 008.

**Plan 005 design calls (approved)**
- Master-detail Snippets page.
- The main list sorts by label A→Z.
- One reusable `SnippetEditorViewModel` + `SnippetEditor` control, which 006 also uses.
- A folder created inline is kept even if the snippet edit is discarded.
- Tags are entered in a comma text box with a chip preview.
- No auto-save.

## 4. In-flight work at handoff (plan 003)

Shared base commit **`7836ccb`** (pushed) adds the domain records that both tracks use:
`Domain/{Folder,Snippet,SnippetDraft}.cs`, `Query/SnippetView.cs`, `Settings/SortField.cs`.

Two Sonnet agents are running in git worktrees under `.claude/worktrees/` (git-ignored). Each
commits locally only and does **not** push.

| Track | Worktree branch | Scope (plan 003 tasks) | Owns |
|---|---|---|---|
| A | `worktree-agent-a53de56c6c25bd8bf` | T1 rules, T4 typed settings, T6 search/sort/grouping | `Core/Domain/*` rules, `Core/Settings/*`, `Core/Query/*`, matching tests |
| B | `worktree-agent-a463de859bebfe508` | T2 migration 2 + guard/backup, T3 repositories | `Core/Data/*`, `tests/.../Data/*` |

**Contracts the two tracks share:**
- **`ISettingsStore`:** `SmartSnippets.Core.Settings.ISettingsStore { string? Get(string key);
  void Set(string key, string value); }`. **Both** tracks create
  `Core/Settings/ISettingsStore.cs` with identical content, so expect a trivial add/add merge
  conflict and keep one copy.
- **Tag storage format:** `,`-joined. Track A provides `TagList.ToStorage/FromStorage`. Track B
  uses a private helper with the same format; dedupe it after merge if desired.
- **Folder keys:** `FolderRepository` takes a precomputed `nameKey`, which the store computes
  with `FolderNameRules.Key`.

**If this session is lost:** look at the two worktree branches. If they have commits, review
and merge them; if not, re-dispatch from plan 003 §6 using the ownership table above.

## 5. Next actions (in order)

1. Merge Track A and Track B into `claude/epic-allen-07hez6` (resolve the `ISettingsStore`
   conflict). Run
   `export PATH=$HOME/.dotnet:$PATH; dotnet test tests/SmartSnippets.Core.Tests`, push, and
   check that CI is green, especially the Windows AOT publish warning gate.
2. Dispatch one Sonnet agent for plan 003:
   - **T5** `SnippetStore`: `ISnippetStore`, `StoreResult`, `StoreException`, the `Changed`
     event, and fault injection with `BEGIN EXCLUSIVE`.
   - **T7** the 10k benchmark, recording numbers in plan 003 §10.
   - **T8** wiring audit and docs.

   Then mark 003 Complete and update master §5/§9.
3. Re-check 004 and 005 against the merged 003 API (names in 004 §3 and 005 §3), and update
   the plans if anything changed.
4. Plan 004: T1 (Presentation projects + CI) first. Then two parallel tracks: Presentation
   (T2, T7, T8) and App (T3, T5, T6, T9). Then T4 integration, T10, T11. Merge on green CI.
5. Plan 005: two parallel tracks, view-models (T1, T2, T3, T6) and views (T4, T5, T7). Then T8.
   Merge on green CI.
6. Tell the user which runbook steps (004 §7, 005 §7) need their Windows PC.

## 6. Verification so far

- **Local** (Linux, .NET SDK 10.0.403 at `$HOME/.dotnet`, installed this session through the
  proxy): Core tests 16/16 at `09b42ee`. `dotnet build src/SmartSnippets.Core` gives 0 warnings
  at `7836ccb`.
- **CI** ([run #4](https://github.com/rockyway/smartsnippets/actions/runs/40571806659),
  `09b42ee`): both jobs green. AOT publish "Generating native code", 0 warnings. Artifact
  `SmartSnippets-win-x64-aot`.
- **User's harness** (`7d32a8e` report): popup ready in 18–22 ms, insertion `Success` with direct
  focus, clipboard restored (6/6 formats), hotkey conflict 1409 handled.

## 7. Unresolved issues / risks

- **Review findings on the user's 002 report and harness, raised and left as-is by the user:**
  1. G-5 was verified by the script writing the `Run` key itself, not through
     `StartupRegistration`, and no sign-out/in test was run.
  2. The inserted Unicode text was never read back. Only `insert_result Success` was checked.
  3. Runbook steps 4, 6, 8 and 10–14 were not exercised.
  4. `scripts/verify-plan002.ps1` runs `Stop-Process -Name notepad -Force`, which kills
     **every** Notepad window. It should kill only the Notepad process it started. It also
     needs the `sqlite3` CLI and uses the real profile, clipboard and log.

  Consider folding fixes into 008.
- **Known prototype limitations carried forward:**
  - The tray icon isn't re-added after Explorer restarts (fixed in 004 T5).
  - The clipboard retry uses `Thread.Sleep` on the UI thread (007).
  - Popup prewarm may flash (006).
- **003 → 004/005 API drift risk:** 004 and 005 are written against 003's planned API.
- **CI:** `actions/*@v4` produce a Node 20 deprecation warning that is currently harmless.
  Bump the actions in 008.

## 8. Environment notes

- There is no `gh` CLI. Use the GitHub MCP tools (`mcp__github__actions_*`,
  `get_job_logs`) for CI.
- Agent worktrees go under `.claude/worktrees/`, which is in `.gitignore`. A worktree can be
  created from a **stale** base. Every agent prompt must start by checking `git log -1` and
  fast-forwarding to the intended base commit.
- WinUI/App code cannot compile on Linux. Windows CI is the only compiler for `src/SmartSnippets.App`.
- Commit convention: `type(scope): summary`. No AI attribution lines. Never push to
  `main`/`master`.
