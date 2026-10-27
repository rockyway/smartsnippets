# 001 — Master plan: Smart Snippets v1

**Date:** 2026-10-27
**Status:** Approved (2026-10-27)
**Size:** L (master plan + sequential subplans)
**Area:** whole repository (greenfield)
**Requirement source:** [`docs/requirements/001-prd.md`](../requirements/001-prd.md)
**Requirement brief confirmed:** 2026-10-27 (G1)

---

## 1. Goal

Deliver Smart Snippets v1: a Windows utility (.NET 10, WinUI 3, Native AOT, SQLite) where a
global hotkey opens a centred popup to find a snippet and insert it into the application that
had focus, plus a main window to manage snippets, folders, and settings.

## 2. Requirement (as confirmed at G1)

- **Problem:** reusable text is retyped or copy-pasted manually; no keyboard-first way to find a
  snippet and drop it into whatever app has focus.
- **In scope:** everything in PRD §3 and §4 "Included" — snippet CRUD, single-level folder CRUD,
  comma tags, global hotkey + conflict validation, tray icon, single instance, popup (search,
  flat/folder layout, 4 sorts, add-snippet, open-main), insertion + usage tracking, settings
  (text size, theme, start at login, hotkey), persistence with schema migrations, the §4
  tech-validation prototype gate, and a manual **"Restart as administrator"** command for
  elevated targets.
- **Out of scope / non-goals:** PRD §4 "Excluded" (cloud/accounts, shared libraries, rich
  text/images, templates/macros, clipboard history, nested folders, import/export); ARM64;
  MSIX/Store; elevated start-at-login.
- **Users and surfaces:** one local Windows user. Surfaces: global hotkey; popup window; main
  window (Snippets, Folders, Settings); notification-area icon (Open, Settings, Restart as
  administrator, Exit); HKCU `Run` key; SQLite file under `%LOCALAPPDATA%\SmartSnippets\`.
- **Behaviour:** as the PRD, with every "Proposed" rule accepted unchanged. Insertion: capture
  the foreground window before the popup shows → on Enter/click dismiss popup, restore focus to
  the target (fail with a message if it is gone; never pick another window) → save clipboard,
  set snippet text, send Ctrl+V → restore clipboard after a short delay → increment usage only
  on successful dispatch; a guard blocks double activation. Elevated target while the app is not
  elevated → message pointing to "Restart as administrator"; no usage counted. Start at login is
  always un-elevated via the `Run` key.
- **Constraints and assumptions:**
  - Unpackaged, self-contained Windows App SDK (latest stable 1.x), **x64 only**,
    Windows 10 1809 (10.0.17763)+ and Windows 11.
  - Release build is Native AOT; a non-AOT build never satisfies the requirement.
  - `Microsoft.Data.Sqlite` with hand-written SQL (no EF Core — AOT support experimental);
    settings stored in the same database.
  - CommunityToolkit.Mvvm source generators + `x:Bind` compiled bindings; any JSON uses
    source-generated contexts only; Win32 interop via `LibraryImport`/CsWin32 — no reflection.
  - Search runs in memory over a cached snippet list refreshed on data change.
  - Single instance via `AppInstance.FindOrRegisterForKey` with activation redirect.
  - Local log file, never containing snippet text.
  - Solution split: `SmartSnippets.Core` (`net10.0`, cross-platform, unit-tested in CI on Linux
    and Windows), `SmartSnippets.App` (WinUI), `SmartSnippets.Core.Tests`.
  - Verification: GitHub Actions `windows-latest` builds, tests, and AOT-publishes; the user runs
    the manual runbooks and the compatibility matrix on their PC.
- **Acceptance criteria:**
  1. **AC1** Snippet CRUD incl. label auto-suggest/freeze, tag normalization, validation that
     rejects whitespace-only text/label without trimming stored text, inline folder creation
     auto-selected, delete and unsaved-change confirmations.
  2. **AC2** Folder CRUD: case-insensitive unique names, delete moves snippets to Unfiled after
     confirmation, rename reflected everywhere immediately.
  3. **AC3** Data, settings, layout/sort choices, and usage survive restart; schema version
     tracked; migrations transactional.
  4. **AC4** Ctrl+Shift+Space opens the popup centred on the target monitor's work area with the
     search box focused; hotkey reconfigurable with conflict explanation, previous hotkey kept on
     failure, restore default.
  5. **AC5** Full keyboard-only workflow (Up/Down, Enter, Esc, Tab/Shift+Tab); mouse activation
     equals Enter.
  6. **AC6** Search: case-insensitive substring AND across label/text/folder/tags, live, empty
     state, keeps sort/layout.
  7. **AC7** Flat and folder layouts (non-selectable headings); 4 sorts with agreed tie-breaks;
     toolbar icons with tooltips, accessible.
  8. **AC8** Add-snippet from popup keeps the target; on save refreshes, selects the new snippet,
     clears search if it would hide it; immediately insertable; cancel restores prior state.
  9. **AC9** Insertion: Unicode + multiline preserved, selection replaced normally, exactly once,
     never into a Smart Snippets window, clipboard restored, actionable errors, usage counted
     only on success.
  10. **AC10** Elevated targets detected with a message; "Restart as administrator" works.
  11. **AC11** Theme (Light/Dark/System, live system follow) and text size applied without
      restart and without clipping; start-at-login toggle with failure explanation, starts in
      background.
  12. **AC12** Closing the main window keeps the hotkey alive; Exit unregisters; single instance.
  13. **AC13** Performance: search on 10k snippets ≤150 ms and popup ≤300 ms on the user's PC;
      CI benchmark indicative only.
  14. **AC14** Native AOT x64 release artifact passes the PRD §4 validation gate; compatibility
      matrix recorded (Notepad, VS Code, Word, Outlook, Chrome/Edge, Windows Terminal with
      PowerShell/cmd, Teams).
- **Decisions made:**
  | Question | Answer |
  |---|---|
  | Packaging / arch / OS | Unpackaged, self-contained, x64 only, Win10 1809+ / Win11 |
  | Insertion mechanism | Clipboard paste (Ctrl+V) with save + restore |
  | Elevated targets | Detect + message; manual "Restart as administrator" |
  | Elevation vs start at login | `Run` key always un-elevated; elevation is a manual restart (UAC) |
  | PRD "Proposed" rules (§6.1–6.3) | All accepted as written |
  | Verification environment | Cross-platform Core tests + GitHub Actions Windows AOT publish; user runs runbooks |
  | Performance reference | User's dev PC (recorded in runbook); CI benchmark indicative |

## 3. Current state (as-built)

Greenfield. `git ls-files` (2026-10-27) returns only `docs/requirements/001-prd.md`. No solution,
projects, CI, or `.gitignore` exist. No `docs/plan/` existed before this plan.

## 4. Shared design constraints (apply to every subplan)

- **Layering:** `SmartSnippets.Core` has no Windows/WinUI dependency: domain model, label/tag
  rules, validation, search, sort, repositories, migrations, settings store. `SmartSnippets.App`
  holds views, view-models, and Win32 services (hotkey, tray, popup positioning, insertion,
  clipboard, elevation, startup registration) behind interfaces so view-models stay testable.
- **AOT discipline:** every project sets `IsAotCompatible=true`; the publish job fails on any
  trim/AOT warning (IL2xxx/IL3xxx). No reflection-based serialization, no `Binding` (only
  `x:Bind`), no dynamic code.
- **Data:** one SQLite file `%LOCALAPPDATA%\SmartSnippets\smartsnippets.db`; WAL mode; foreign
  keys on; `schema_version` table; migrations are ordered, idempotent, each in a transaction.
- **Change notification:** a single in-process `ISnippetStore` change event drives refresh of
  the popup, main window, and search cache — no parallel refresh paths.
- **Privacy:** logs never include snippet text or tags.
- **Every subplan** ends with its own verification, updates this master's progress table, and
  records handoff notes.

## 5. Subplans and dependency order

| # | File | Objective | Depends on | Status |
|---|---|---|---|---|
| 002 | [002-tech-validation-prototype.md](002-tech-validation-prototype.md) | PRD §4 gate: AOT WinUI + SQLite + hotkey/popup + focus/Unicode insertion + startup registration; repo skeleton + CI | — | Complete |
| 003 | [003-core-domain-and-persistence.md](003-core-domain-and-persistence.md) | Domain model, label/tag rules, validation, schema + migrations, repositories, search/sort engine, benchmark | 002 | Complete (2026-10-27) |
| 004 | [004-app-shell-lifecycle-and-settings.md](004-app-shell-lifecycle-and-settings.md) | Single instance, tray, hotkey service + configuration, theme, text size, start at login, restart as admin | 002, 003 | Complete — CI green; manual runbook §7 pending |
| 005 | [005-main-window-management.md](005-main-window-management.md) | Main window: snippet list/search/editor, folder management screen, confirmations | 003, 004 | Complete — CI green (run #28, `6e75872`); manual runbook (§7) pending on user PC |
| 006 | [006-popup-search-and-toolbar.md](006-popup-search-and-toolbar.md) | Popup: positioning, search, layouts, sorts, keyboard/mouse, add-snippet, open main | 003, 004, 005 (shared editor) | Placeholder |
| 007 | [007-insertion-engine-and-elevation.md](007-insertion-engine-and-elevation.md) | Production insertion engine, clipboard restore, double-activation guard, elevation detection, usage stats | 002, 003, 006 | Placeholder |
| 008 | [008-release-hardening-and-acceptance.md](008-release-hardening-and-acceptance.md) | AOT release artifact, perf acceptance, compatibility matrix, full runbook, wiring audit | 003–007 | Placeholder |
| 009 | [009-design-alignment-004-005.md](009-design-alignment-004-005.md) | Align built 004/005 surfaces (main window, folders, settings, dialogs, tray menu, tokens) with `docs/design/001-smart-snippets-design.html` | 004, 005 | Draft — awaiting approval |

Only one subplan is detailed and in progress at a time. Approving this master approves the
decomposition, not the contents of undetailed subplans.

## 6. Acceptance-criterion coverage

| AC | Subplan(s) |
|---|---|
| AC1 | 003 (rules), 005 (UI) |
| AC2 | 003, 005 |
| AC3 | 003, 004 (settings), 006 (layout/sort persistence) |
| AC4 | 002 (spike), 004, 006 |
| AC5 | 006 |
| AC6 | 003 (engine), 006 (UI) |
| AC7 | 003 (sort), 006 |
| AC8 | 005 (editor), 006 |
| AC9 | 002 (spike), 007 |
| AC10 | 002 (detection spike), 004 (restart as admin), 007 |
| AC11 | 004 |
| AC12 | 002 (spike), 004 |
| AC13 | 003 (CI benchmark), 008 (acceptance) |
| AC14 | 002 (gate), 008 (release + matrix) |

## 7. Cross-subplan integration checks (run in 008)

1. Save in main-window editor → popup list (already open) shows it without reopening.
2. Rename folder in Folders screen → popup folder headings and editor dropdowns update.
3. Insert from popup → usage count/last used update in main window and popup sort.
4. Change hotkey in Settings → old hotkey unregistered, new one opens popup; restart keeps it.
5. Exit from tray → hotkey released (another app can register it); process gone.
6. Fresh profile (no DB) → migrations create schema; existing DB at older version → migrated.

## 8. Risks (programme level)

- **R1 WinUI 3 + Native AOT gaps** (third-party controls, reflection in bindings) → 002 proves
  the stack first; only built-in controls; `x:Bind` only. If the gate fails, stop and ask before
  changing the stack (PRD §4).
- **R2 Focus-stealing rules** prevent popup activation or target restore → spike in 002 using
  the foreground rights a `WM_HOTKEY` grants; documented fallbacks evaluated there.
- **R3 Clipboard restore races** (target pastes after restore, delayed-render formats) → 002
  measures; 007 makes the delay configurable internally and restores only formats it can copy.
- **R4 No local Windows/.NET** in the agent environment → all Windows verification goes through
  CI plus user-run runbooks; unchecked runbook items block completion.

## 9. Progress and handoff log

| Date | Subplan | Status change | Notes |
|---|---|---|---|
| 2026-10-27 | 001–008 | Created | Master + placeholders reserved; 002 detailed |
| 2026-10-27 | 002 | In progress | Code complete; CI AOT publish green with 0 IL warnings; awaiting user runbook (G-2…G-5) before Complete and before detailing 003 |
| 2026-10-27 | 002 | Complete | Verified via automated harness `scripts/verify-plan002.ps1`; PRD §4 gate passed (G-1…G-5 verified) |
| 2026-10-27 | 003, 004, 005 | Draft — awaiting approval | Detailed together in one pass at the user's request (deviation from one-at-a-time); 004/005 reference 003's planned API and must be re-checked against merged code before each starts. 004 adds a `SmartSnippets.Presentation` project for testable view-models |
| 2026-10-27 | 003 | Complete | Domain, repositories, migration 2, settings, `SnippetStore`, search/sort/grouping done; 138 tests pass locally; benchmark: 10k snippets filter median < 15 ms, build + sort < 40 ms (plan 003 §10). Store API differs slightly from the plan text (`ValidationError` as error code; see 003 §10). CI/AOT run pending on push |
| 2026-10-27 | 004 | Implementation complete | AppHost composition root, tray/lifecycle wiring, startup notices and wiring audit done (T4, T10, T11); Core and Presentation tests pass locally; awaiting CI (AOT publish) and the manual runbook (004 §7) |
| 2026-10-27 | 005 | Implementation complete | View-models (T1–T3, T6) and views (snippet editor control, Snippets and Folders pages, AppHost wiring, T4/T5/T7/T8) done; Core and Presentation tests pass locally; App not compiled on Linux, awaiting CI (AOT publish) and the manual runbook (005 §7) |
| 2026-10-27 | 005 | Complete — CI green | First Windows compile failed on `FlowLayout` (not in WinUI 3); replaced with `UniformGridLayout` in `6e75872`; CI #28 green incl. AOT publish with 0 trim/AOT warnings. Manual runbook (005 §7) pending |

## 10. Open questions

None.

## 11. Approval log

| Date | Event | Notes |
|---|---|---|
| 2026-10-27 | Requirement brief confirmed (G1) | 2 rounds of questions |
| 2026-10-27 | Master plan approved (G2) | approved by user; implementation of 002 ordered same day |
