# 002 — Context handoff (2026-10-27, after plan 004)

**Supersedes:** [001-context-handoff-2026-10-27.md](001-context-handoff-2026-10-27.md). Its §3
decisions and §8 environment notes still apply and are not repeated here.
**Branch:** `claude/epic-allen-07hez6` (pushed at `3a6fbea` when this was written)
**Repo:** `rockyway/smartsnippets`
**User instruction in force:** "implement 003–005 with Sonnet subagents".

## 1. Status

| # | Plan | Status | Evidence |
|---|---|---|---|
| 002 | tech validation | Complete | user harness, CI #4 |
| 003 | core domain & persistence | **Complete** | 138 → now 164 Core tests; benchmark in plan 003 §10 (10k search median 3–14 ms); CI #15 green |
| 004 | app shell, lifecycle, settings | **Complete — CI green; manual runbook §7 pending** | CI #21 green on `45157a8` (AOT publish 0 warnings); wiring audit in plan 004 §10 |
| 005 | main window management | **In progress** | view-models merged at `c042eb2` (Presentation 95 tests); views agent running (see §2) |
| 006–008 | popup / insertion / release | Placeholder | not detailed |

Last CI checked: #21 (`45157a8`) green. #22 (`c042eb2`) was in progress at handoff, and nothing
after it had been checked.

## 2. In-flight work

**005 views agent** (Sonnet), working **directly in the main checkout** with no worktree.

- **Scope:** plan 005 T4, T5, T7 and T8:
  - `App/Controls/SnippetEditor.xaml(.cs)`
  - `App/Pages/SnippetsPage.xaml(.cs)` and `FoldersPage.xaml(.cs)`, replacing the placeholders
  - the `AppHost.CreatePage` wiring
  - the wiring audit
  - plan 005 and master status rows
- **Commit message it will use:** `feat(app): snippet editor control, Snippets and Folders pages,
  AppHost wiring (plan 005 T4, T5, T7, T8)`. It commits explicit paths and does **not** push.
- **If the session is lost:**
  1. Check `git log` for that commit.
  2. If it's absent, check `git status` for partial work.
  3. Either finish it, or re-dispatch from plan 005 §6 T4/T5/T7/T8 using the view-model API
     summarized in §4 below.

## 3. Next actions

1. When the views agent reports:
   1. Review its commit and run `dotnet test` for both test projects.
   2. Push, then check CI. This is the first Windows compile of the 005 XAML, so expect
      possible XAML/x:Bind fixes. Known lesson: x:Bind function-binding helpers must be
      **instance** methods.
   3. Fix any failure and push again until CI is green.
2. Mark plan 005 "Complete — CI green; manual runbook §7 pending" in both the plan and the master
   plan.
3. Tell the user which runbook steps need their Windows PC:
   - Plan 004 §7: 15 steps covering hotkey change/conflict, theme/text size, tray, Explorer
     restart, restart-as-admin, and start at login.
   - Plan 005 §7: 15 steps covering the editor, label/tags, inline folder, the unsaved guard,
     delete, search, the Folders page, text size and keyboard-only use.
4. The user's instruction (003–005) is then fully done. 006–008 need a new planning pass
   (detail → G2 approval → explicit implement instruction).

## 4. APIs the remaining work depends on (all merged)

**Core** (`SmartSnippets.Core`):
- **Store:** `ISnippetStore` / `SnippetStore(SqliteConnectionFactory, TimeProvider)`.
  - Members: `Load`, `Folders`, `Snippets` (`SnippetView`), `Changed`
    (`StoreChangedEventArgs{Kind, Id, SnippetIds}`).
  - Writes: `Create/Update/DeleteSnippet`, `Create/Rename/DeleteFolder`, `RecordUsage`. They
    return `StoreResult[<T>]{Success, Value, Error: ValidationError?, Message}`.
  - Database failures throw `StoreException(StoreFailure)`.
- **Rules:** `TagList`, `LabelSuggester(.ResolveLabel)`, `SnippetValidator`, `FolderNameRules`.
- **Query:** `SnippetSearch`, `SnippetSort`, `SnippetGrouping` (006 needs these).
- **Settings:** `AppSettings` (sealed record with `init` properties, `Load(ISettingsStore)`,
  `Save`, `Hotkey`, `WithHotkey`). Keys live in `SettingsKeys`, including
  `ShellCloseHintShown`. `HotkeyGesture` has `Default`, `IsValid`, `TryCreate` and
  `ToDisplayString`.

**Presentation** (`SmartSnippets.Presentation`, net10.0, tested on Linux):
- **Services:** `IHotkeyManager`, `IStartupRegistration`, `IElevation`, `IThemeService`,
  `ITextSizeService` / `TextSizeService`, `IDialogService` (`ConfirmAsync`,
  `AskSaveDiscardCancelAsync` → `SaveChoice`, `ShowErrorAsync`), `IUiDispatcher`, `INotifier`.
- **Navigation:** `IGuardedPage`, `PageKind`.
- **View-models:** `SettingsViewModel`, `SnippetEditorViewModel(store, dialogs, long? id)`,
  `SnippetListViewModel`, `SnippetsPageViewModel(store, dialogs)`,
  `FoldersPageViewModel(store, dialogs)`. Their row and choice records:
  `SnippetRowViewModel`, `FolderRowViewModel`, `FolderChoice`.
- **For 006:** `SnippetEditorViewModel.Saved` (event `<long>`) is the hook for the popup's
  "Add snippet".

**App:**
- **`AppHost`** is the single composition root. Its internal properties `Store`, `Settings`,
  `TextSize`, `Dialogs`, `Notices` and `Dispatcher` are available to 005/006.
  `CreatePage(PageKind)` is the page factory.
- **`MainWindow(Func<PageKind, UIElement>)`:** `ShowPage`, `CanExitAsync`, `CloseForExit`, and
  the `HiddenByClose` event.
- **Services:** `HotkeyManager`, `TrayIcon` (with balloons and `TaskbarCreated`),
  `ThemeService.Register(Window)`, `ElevationService` (`--after-pid` hand-off),
  `DialogService` (serialized).
- **Message window:** `MessageWindow` is now a hidden **top-level** window, not message-only, so
  it receives `TaskbarCreated`. Verify this in the runbook.

## 5. Verification so far

- **Local** (Linux, .NET 10.0.403 at `$HOME/.dotnet`): Core 164/164 and Presentation 95/95 at
  `c042eb2`. Core and Presentation builds give 0 warnings.
- **CI:** green on every push from `7836ccb` through `45157a8`, except two runs that were fixed:
  - **`48eff1a` and `2e2b4d4`:** CS0176. x:Bind function helpers were `static`; fixed in
    `2510ab1`.
  - **Earlier, `21d093e`:** a `Program.cs` lambda discard bug, fixed then.
- **Not verified:** no WinUI runtime behaviour from 004 or 005 has run on Windows yet, and
  every manual runbook step for 004 and 005 is still unchecked.

## 6. Open risks and notes

- **Text size on 005 pages:** plan 004's runbook step 8 (placeholder pages resizing) only
  becomes meaningful once 005's pages bind `TextSize.FontSize`.
- **Unbuilt 004 constructs:** the 004 agent flagged some constructs that only CI compiled, none
  of them exercised at runtime yet:
  - `MessageBox` on startup failure, followed by `Application.Current.Exit()` inside
    `OnLaunched`.
  - `ThemeService.Register(popup)` setting title-bar colours on a window that has no title bar.
- **Shortcut recorder:** it lets a plain Tab move focus rather than recording it. This was
  reported to the user and accepted silently.
- **Windows-key shortcuts:** Win-based gestures may not be capturable in the recorder, because
  the shell can swallow the Win key.
- **Still open from handoff 001 §7:** the user's `scripts/verify-plan002.ps1` kills every
  Notepad window, and several 002 gates (G-5, Unicode read-back) were verified only partially.
  Suggested home: plan 008.
- **CI:** the `actions/*@v4` Node 20 deprecation warning is harmless for now. Bump the actions in
  008.
