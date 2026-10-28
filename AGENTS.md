# AGENTS.md

This file provides guidance to Claude Code (claude.ai/code) and other coding agents when working with code in this repository.

Smart Snippets: a Windows utility (.NET 10, WinUI 3, unpackaged, x64 only, Native AOT, SQLite). A global hotkey opens a popup to find a snippet and paste it into the app that had focus; a main window manages snippets, folders and settings. Requirement: `docs/requirements/001-prd.md`; master plan: `docs/plan/001-master-smart-snippets-v1.md`.

## Commands

SDK is pinned by `global.json` (10.0.100, roll forward to latest feature).

```bash
dotnet build                                   # whole solution (SmartSnippets.slnx); needs Windows for the App project
dotnet test tests/SmartSnippets.Core.Tests
dotnet test tests/SmartSnippets.Presentation.Tests
dotnet test tests/SmartSnippets.Core.Tests --filter "FullyQualifiedName~LineEndings"   # single test/class
dotnet publish src/SmartSnippets.App -c Release -r win-x64 -o publish                  # Native AOT publish
```

- Run test projects sequentially; parallel builds collide on the shared Core output (CS2012).
- CI (`.github/workflows/ci.yml`, runs on `main` pushes and manually): Core + Presentation tests on Linux and Windows, then the AOT publish gate on Windows. The gate fails on any `warning IL2xxx/IL3xxx`, on `publish/SmartSnippets.App.dll` existing (means non-AOT), or on a missing `SmartSnippets.App.exe` / `e_sqlite3.dll`.
- `scripts/verify-plans-003-009.ps1` (and `verify-plan002.ps1`) are interactive acceptance scripts: they drive the published exe with real key presses and UI Automation, move the real `%LOCALAPPDATA%\SmartSnippets` profile aside, and need `sqlite3.exe` on PATH. Do not run them while using the machine; steps needing a human are reported as SKIP.

## Architecture

Three projects with a strict dependency direction, `App → Presentation → Core`:

- **`SmartSnippets.Core`** (`net10.0`, cross-platform): domain, SQLite store (`Microsoft.Data.Sqlite` with hand-written SQL, no EF Core), schema migrations, in-memory search/query, popup layout, settings (stored in the same database), logging, `AppPaths`. Unit-tested on Linux and Windows.
- **`SmartSnippets.Presentation`**: view-models (CommunityToolkit.Mvvm), UI text (`Text/UiText.cs`), navigation, and the service *interfaces* the UI depends on (`IClipboardService`, `IHotkeyManager`, `IElevation`, `INotifier`, `IInsertionService`, ...). No WinUI types, so it is testable on Linux; tests use hand-written fakes.
- **`SmartSnippets.App`** (WinUI 3): windows, pages, XAML, and the Win32 implementations of the Presentation interfaces. `AppHost.cs` is the composition root: it builds the store, services, windows and tray, and owns startup/shutdown. `Program.cs` handles single-instance (`AppInstance.FindOrRegisterForKey` with activation redirect).

Insertion flow (plans 006/007): the foreground window is captured as an `InsertionTarget` before the popup shows; on Enter the popup hides, `IInsertionService.InsertAsync` revalidates the target, swaps the clipboard, sends Ctrl+V, restores the clipboard, and records usage only when the outcome is `Inserted`. The shared contract is `Presentation/Services/InsertionContracts.cs`; plans 006 and 007 both depend on it, so change it only by updating both plans.

## Constraints that shape the code

- Native AOT: no reflection. JSON only via source-generated contexts; Win32 via `LibraryImport`/CsWin32 (`NativeMethods.txt`); XAML uses compiled `x:Bind`. `IsAotCompatible` is on for every non-test project, so trim/AOT warnings are real defects.
- Warnings are treated seriously: builds are expected to be 0 warnings.
- Logs must never contain snippet text or window titles (`InsertionTarget.DisplayName` can hold a title).
- WinUI/XAML only compiles on Windows, so Linux CI cannot catch App compile errors; verify `x:Bind` paths against the view-model surface.
- Unelevated app cannot paste into elevated windows; the tray offers "Restart as administrator". Start-at-login (HKCU `Run`) is always unelevated.

## Planning workflow

Work is driven by numbered documents: requirements in `docs/requirements/`, plans in `docs/plan/NNN-*.md` (status, tasks T0..Tn, approval log), reviews in `docs/review/`, handoffs in `docs/handoff/`. Read the relevant plan before changing an area; plans cite files and line numbers and record user decisions. Numbered docs use the next free `NNN`; never renumber existing ones.

`main` is the production branch: land work through feature branches and PRs, not direct commits. Agent worktrees go under `.claude/worktrees/` (git-ignored).
