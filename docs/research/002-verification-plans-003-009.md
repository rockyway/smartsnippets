# 002 — Automated verification of plans 003, 004, 005 and 009

**Date:** 2026-10-27
**Branch:** `verify/plans-003-009` (based on `main` at `b988ed0`)
**Script:** [`scripts/verify-plans-003-009.ps1`](../../scripts/verify-plans-003-009.ps1) (helpers in `scripts/lib/Uia.ps1`)
**Result on the branch:** PASS 103, FAIL 0, WARN 1, SKIP 5

## How to run

```powershell
powershell -ExecutionPolicy Bypass -File scripts/verify-plans-003-009.ps1            # build + everything
powershell -ExecutionPolicy Bypass -File scripts/verify-plans-003-009.ps1 -SkipBuild # reuse .\publish
powershell -ExecutionPolicy Bypass -File scripts/verify-plans-003-009.ps1 -Only B,D  # sections
```

Needs an interactive Windows desktop, .NET 10 SDK, `sqlite3.exe` on PATH and Windows PowerShell 5.1.
It sends real key presses (hands off the keyboard while it runs). Your real `%LOCALAPPDATA%\SmartSnippets`
profile is moved to `SmartSnippets.verify-backup` for the run and restored in `finally`; if a run is killed,
move the backup back by hand (the script refuses to start while the backup exists).

The script drives the **published Native AOT exe** through UI Automation, real keyboard input, the real tray
callback and Win32 menu, `sqlite3` for DB assertions and the app log for events. A failure means the build is
wrong, not the harness (harness problems were fixed during development; see "Harness notes").

## What is covered

| Section | Plan runbooks | Checks |
|---|---|---|
| A | all | Core + Presentation unit tests, 10k-snippet benchmark, AOT publish (0 IL warnings, no `App.dll`), no `{Binding}`, layering, every runbook log event exists |
| B | 003 | v1 DB upgraded (backup, `from=1 to=2`, data kept), restart does not re-migrate, fresh profile, newer-schema DB refused, WAL |
| C | 004, 009 | main window on launch, pages open, hotkey -> popup (latency), close-to-hide + hotkey alive, second instance, theme and text size persist and survive restart, start-at-login Run value, hotkey change/old key released/restore default, startup conflict, `--background`, TaskbarCreated handler, tray menu items/order/live accelerator, tray Exit frees the hotkey |
| D | 005, 009 | create (exact whitespace), auto label, tag normalization, custom label, whitespace-only rejection, inline folder create + auto-select, duplicate folder name, browse pane and row meta, AND search + empty state, Unicode, Delete key + confirm/cancel, unsaved-changes guard (switch row, close window), folder rename propagation, folder delete moves snippets to Unfiled, persistence, accessible names, log privacy |

## Bugs found by this run on `main` (and fixed on this branch)

1. **Crash: creating a snippet (Ctrl+N / "New snippet") killed the app.**
   `SnippetEditor.xaml` binds `ItemsRepeater.ItemsSource` to `ViewModel.TagChips`, typed `IReadOnlyList<string>`.
   WinUI/CsWinRT passes that as `IVectorView<string>`, which is not a valid ItemsSource, so XAML throws
   `E_INVALIDARG` (`0xc000027b` in `Microsoft.UI.Xaml.dll`; same in a JIT build, so not an AOT issue).
   Bisected to the `ItemsRepeater`; changing the property type to `List<string>` fixes it. CI could not catch it
   (it only compiles and publishes). **Every snippet create/edit was impossible.**
2. **Settings page could not be opened** (`navigate_failed ... 0x80070057`): same cause,
   `SettingsViewModel.CurrentHotkeyParts` (`IReadOnlyList<string>`) bound to the keycap `ItemsControl`. Fixed the same way.
3. **Accessible names were view-model dumps.** Browse, Folders and Snippet list items were announced as
   `BrowseItemViewModel { Kind = All, ... }`, `...FolderRowViewModel`, `SnippetRowViewModel { Id = 4, ... }`
   (plan 005 runbook step 15, Narrator). Fixed with `ToString()` overrides; unit tests added.

Fix files: `SnippetEditorViewModel.cs`, `SettingsViewModel.cs`, `BrowseItemViewModel.cs`, `FolderRowViewModel.cs`,
`SnippetRowViewModel.cs`, `tests/.../AccessibleNameTests.cs`. Presentation tests 136 -> 139, publish still 0 warnings.

## Open observations (not fixed)

- **WARN D1.eol — line breaks are stored as lone CR (`0x0D`).** WinUI `TextBox.Text` normalizes CRLF to CR, so a
  multi-line snippet typed in the editor is stored with `\r` only (spaces are preserved exactly). The 005 integration
  test uses CRLF through the view-model and never hits this. Plan 007 should decide: normalize to `\r\n` when
  setting `CF_UNICODETEXT` (or on save) so multi-line paste is correct in every target (PRD 3.8, AC9).
- **Conflict sentence cannot be reached by recording a taken shortcut.** A chord owned by another
  `RegisterHotKey` client is swallowed by the OS before the recorder sees it, so the "Another app is already using
  ..." message only appears for hook-based owners. The startup-conflict path (C13) works and is verified.
  Consider whether the Settings flow needs a different way to surface this (design call, not a defect in the code).
- Wording differs slightly from the plan text: empty text shows "Snippet text cannot be empty." (plan 005 runbook
  says "Text is required"). The script accepts either.

## Not automatable (SKIP in the report, still manual)

| Item | Why |
|---|---|
| 004 #13/#14 Restart as administrator, UAC accept/cancel | UAC runs on the secure desktop |
| 004 #11 real `explorer.exe` restart | disruptive; the `TaskbarCreated` handler is exercised instead (it logs an add attempt; a real restart removes the icon so the add succeeds) |
| 004 #15 start at login after sign-out | needs sign-out; the Run value content is verified |
| 004 #8, 009 #11, 009 #12 visual review (text size 32 clipping, light/dark, narrow window, Narrator speech) | needs eyes; settings persistence and UIA names are verified |
| 009 #7 locked-database save failure (Try again / Copy text) | possible to script (hold an exclusive lock) but not done here |
| C9.conflict | see observation above |

## Harness notes (so the next person is not surprised)

- UIA `Invoke`/`Select` work on WinUI 3 controls, but keyboard accelerators need the main window foregrounded;
  the helpers use an Alt tap before `SetForegroundWindow` to beat the foreground lock.
- The hidden `SmartSnippets.MessageWindow` is a top-level window (found with `FindWindow`), by design.
- The tray menu is a real Win32 menu; items are read through `MN_GETHMENU`, Exit is picked with Up + Enter.
