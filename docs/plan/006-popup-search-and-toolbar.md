# 006 — Popup: search, layouts, sorts, toolbar and add snippet

**Date:** 2026-10-28
**Status:** Draft — awaiting approval
**Size:** L (subplan of [001-master-smart-snippets-v1.md](001-master-smart-snippets-v1.md))
**Area:** `src/SmartSnippets.Presentation/` (popup view-model, contracts, `UiText`), `src/SmartSnippets.App/Popup/`, `src/SmartSnippets.App/Controls/SnippetEditor.xaml(.cs)`, `src/SmartSnippets.App/Services/` (target capture, tray, interim insertion adapter), `src/SmartSnippets.App/AppHost.cs`, `tests/SmartSnippets.Presentation.Tests/`, one small Core settings change
**Depends on:** 003, 004, 005, 009 (all complete, CI green on `main`), plus the **007 insertion contract** (§4.3.1), which the coordinator writes in T0 before any track starts
**Design reference:** [docs/design/001-smart-snippets-design.html](../design/001-smart-snippets-design.html), section "Global popup" and its `<script>` (`row()`, `render()`, key handling, add-snippet dialog)
**Requirement brief confirmed:** 2026-10-28 (G1, 006–008 brief)

> **API check (2026-10-28, against `main` at `4861b0e`).** The placeholder assumed several APIs.
> The merged code matches them, with five differences that this plan handles:
> - `SnippetSort.Apply` already sorts descending, puts never-used snippets last for Last used, and
>   breaks ties by label (OrdinalIgnoreCase) and then ID. `SnippetGrouping.ByFolder` already orders
>   folders by name with Unfiled last. **Core query needs no change.**
> - `AppSettings.PopupLayout` and `PopupSort` and their keys exist. However, `SettingsViewModel`
>   writes its whole start-up snapshot back on every change, so it would **overwrite the popup's
>   saved layout and sort** with stale values (T5).
> - `SnippetEditorViewModel` is host-agnostic. The `SnippetEditor` *control* is not: it shows
>   Delete, the stats card and the unsaved-changes bar, and its Esc asks to discard and stays open.
>   `DialogService` always targets the main window's XamlRoot. (T8, §4.1)
> - The 002 inserter (`IInserter`) is App-internal, takes **text** rather than a snippet ID, and
>   records no usage. 006 consumes the 007 contract (`IInsertionService`, by ID) through an
>   interim adapter (T10).
> - `TrayState.PopupOpen` is declared but never set. Tray state has two writers, and the tray's
>   "Show popup" relies on a `SetForegroundWindow` → `GetForegroundWindow` round trip that can race
>   (T11).

---

## 1. Goal

When you press Ctrl+Shift+Space (or choose **Show popup** from the tray), the real popup opens.
- It appears centred on the monitor of the window you were working in, with the search box
  focused and the first result selected.
- You type to filter and move with the arrow keys. Enter inserts the snippet into that window.
- You can switch between a flat list and folders, pick one of four sorts, add a snippet without
  losing your target, or jump to the main window.
- If an insertion fails, the popup comes back with the reason, a **Copy text** button and, for
  elevated targets, **Restart as administrator**.

This replaces the 002 prototype popup, which inserts a fixed test string.

## 2. Requirement slice (as confirmed at G1, 2026-10-28)

Pasted from the confirmed 006 slice of the 006–008 brief. Nothing below goes beyond it, except
where §9 lists a design call for approval.

- **Replace the 002 prototype popup.**
  - Capture the target before showing.
  - Open centred on the target monitor's work area, clamped.
  - Focus the search box and select the first result.
- **Search box:**
  - Placeholder "Search label, text, folder, or tag".
  - Filters live, using the existing `SnippetSearch` semantics: AND of terms, substring,
    case-insensitive.
  - Keeps the current sort and layout.
- **Rows:**
  - Label, a one-line text preview, and chips.
  - Chips: a folder chip (flat layout only; "Unfiled" when there is no folder) and up to 2 tag
    chips.
  - The selected row is visually marked.
- **Folder layout:**
  - Non-selectable headings "Folder (n)".
  - Folders by name, Unfiled last.
  - Up/Down skip headings.
- **Toolbar:** 4 icon buttons, each with a tooltip and `AutomationProperties.Name`:
  - **Layout toggle:** tooltip "Layout: flat" / "Layout: folder".
  - **Sort menu:** Last used / Usage count / Created date / Updated date, all descending.
    Never-used snippets come last for Last used. Ties break by label, then ID; `SnippetSort`
    already does this (API check).
  - **Add snippet.**
  - **Open main window.**
  - Layout and sort persist via `AppSettings`.
- **Footer:**
  - Hint "↑↓ move · Enter insert · Esc close" and "N of M snippets".
  - An error message area, used by 007 failures.
- **Empty state:** "No snippets match" + "Try fewer words, or add a new snippet with the plus
  button."
- **Keyboard and mouse:**
  - Up/Down move, Enter inserts.
  - Esc closes, but closes the sort menu first if it is open.
  - Tab/Shift+Tab move through the controls.
  - A mouse click on a row is the same as Enter.
- **Clicking outside (deactivation)** closes the popup, except while the add-snippet editor is
  open or an insertion is in progress.
- **Add snippet:**
  - An editor overlay over the popup, titled "Add snippet", with the subline "Insertion target
    kept: <DisplayName>".
  - Reuses `SnippetEditorViewModel`: Text, Label + auto hint, Folder + inline New, Tags.
  - Ctrl+Enter saves (design). Cancel/Esc returns to the popup's previous state (query and
    selection).
  - On save: refresh, clear the search if it would hide the new snippet, and select it. The new
    snippet is immediately insertable and the target is unchanged.
- **Open main window:** opens or activates the main window on the Snippets page, and closes the
  popup.
- **Tray "Show popup"** uses the same capture path. `TrayState.PopupOpen` is set while the popup
  is visible; the glyph comes in 008.
- **Insertion failure** (007 decides the message):
  - The popup re-opens with the same target and the same selection.
  - The message shows in the footer, with a **Copy text** button (`IClipboardService`).
  - **Restart as administrator** (`IElevation`) also shows when the outcome is `TargetElevated`.
  - On success the popup stays hidden.
- **Performance:** popup ready ≤ 300 ms after the hotkey on the user's PC. Keep the
  `popup_ready_ms` log and the prewarm.
- **Contract with 007:** 006 consumes `IInsertionService` (§4.3.1, verbatim) and does not
  implement it. Until 007 lands, a thin adapter over `ClipboardPasteInserter` keeps 006 runnable.
- **Out of scope:**
  - Production insertion internals: focus restore, clipboard restore, line-ending normalization,
    elevation probe and message wording are all 007.
  - Tray glyph images, the verify-script popup sections, and AOT release acceptance are 008.

**Master ACs served:**
- **AC4:** open, position and focus.
- **AC5:** keyboard and mouse.
- **AC6:** search UI.
- **AC7:** layouts, sorts and toolbar.
- **AC8:** add snippet.
- Also contributes to:
  - **AC3:** layout and sort persistence.
  - **AC13:** popup latency.
  - The **AC9/AC10** hand-off to 007.

## 3. Current state (as-built, `main` at `4861b0e`)

**Popup prototype (002), to be replaced:**
- `src/SmartSnippets.App/Popup/PopupWindow.xaml:5`: the window title is `"SmartSnippets"`. Lines
  11–23 hold one `TextBox` and a `ListView` with a single hard-coded item, "Insert test string".
- `src/SmartSnippets.App/Popup/PopupWindow.xaml.cs`:
  - `:43`: `TestString` is the only text ever inserted.
  - `:45-46`: fixed logical size of 640 × 480.
  - `:61-70`: the presenter has no border or title bar, is always on top, can't be resized, and
    is hidden from Alt+Tab.
  - `:77-82`: `Prewarm()` shows the window off-screen once and hides it.
  - `:84-98`: `ShowFor(target, ts)` centres the window, clears the search, shows and activates
    it, calls `SetForegroundWindow`, and focuses the search box.
  - `:102-112`: **any** deactivation hides the window.
  - `:120-130`: `ReportReady` logs `popup_ready_ms <n>` once per show, from `Activated` or
    `LayoutUpdated`.
  - `:148-179`: `InvokeAsync` has an `_inserting` guard, hides, and then calls
    `IInserter.InsertAsync(target, TestString)`.
  - `:181-188`: failures show a Win32 `MessageBox`.

**Composition (AppHost):**
- `src/SmartSnippets.App/AppHost.cs`:
  - `:138`: `hotkeyService.HotkeyPressed += OnHotkeyPressed`.
  - `:145`: `Dialogs = new DialogService(() => _mainWindow?.ContentXamlRoot, ...)`, so every
    dialog targets the main window.
  - `:148-151`: the popup is built as `new PopupWindow(new ClipboardPasteInserter(messageWindow.Handle))`,
    then `popup.Prewarm()` and `theme.Register(popup)`.
  - `:186-199`: the tray is built. **Two places write `tray.State`**: the initial assignment at
    `:197` and the `StatusChanged` handler at `:198-199`. Neither knows about the popup.
  - `:217-227`: `OnHotkeyPressed` calls `ForegroundTarget.Capture()`. If it returns null, it logs
    `hotkey_ignored_own_or_no_window`. Otherwise it calls `_popup.ShowFor`.
  - `:231`: `ShowPopupFromTray` re-runs the hotkey path, which re-reads the foreground window.
  - `:327`: `_popup?.Close()` on exit.

**Target capture and placement:**
- `src/SmartSnippets.App/Services/ForegroundTarget.cs`:
  - `:7`: `ForegroundTargetInfo(HWND, uint ProcessId, HMONITOR)`. There is no display name.
  - `:11-27`: `Capture()` returns null for no window or for our own process (`:20`). This is the
    "never target a Smart Snippets window" rule.
- `src/SmartSnippets.App/Services/WindowPlacement.cs`:
  - `:12-32`: `CenterOn` scales by the target monitor's effective DPI (`:22`).
  - It calls `Core.Layout.PopupPlacement.CenterIn` to clamp to the work area, then
    `MoveAndResize` (`:32`).

**Tray (`src/SmartSnippets.App/Services/TrayIcon.cs`):**
- `:12-17`: `TrayState { Ready, PopupOpen, HotkeyUnavailable }`.
- `:80-92`: the `State` setter updates only the tooltip.
- `:250-259`: **Show popup** calls `SetForegroundWindow(PreviousAppWindow(...))`, then
  `_onShowPopup()`. `AppHost` then calls `GetForegroundWindow()` again, which races with the
  asynchronous foreground switch.
- `:280-284`: the comment says "Plan 006 replaces this with full target tracking".
- `:285-322`: `PreviousAppWindow` and `IsAppWindow` skip shell, taskbar and own-process windows.

**Insertion (002 spike):**
- `src/SmartSnippets.App/Services/IInserter.cs:3-17`: `InsertOutcome` (`Success`, `TargetClosed`,
  `TargetElevated`, `FocusFailed`, `ClipboardBusy`, `SendInputFailed`) and
  `InsertAsync(ForegroundTargetInfo, string text)`.
- `src/SmartSnippets.App/Services/ClipboardPasteInserter.cs`:
  - `:27-32`: logs `insert_result <outcome>`.
  - `:36`: the elevation probe.
  - `:95-125`: focus restore, direct first and then `AttachThreadInput`.
  - It records no usage.

**Core query and settings (reused unchanged):**
- `src/SmartSnippets.Core/Query/SnippetSearch.cs:3,27-45`: AND of whitespace-separated terms,
  `OrdinalIgnoreCase` substring over label, text, folder and tags. An empty query returns all
  items in input order.
- `src/SmartSnippets.Core/Query/SnippetSort.cs:7-34`:
  - The four sorts, all descending.
  - `CompareNullableDesc` (`:36-42`) puts never-used snippets last.
  - Ties break by label (OrdinalIgnoreCase) and then ID.
- `src/SmartSnippets.Core/Query/SnippetGrouping.cs:5-37`: `ByFolder(sorted)` keeps the item order
  inside each group, orders folders by name (OrdinalIgnoreCase, then ID), omits empty groups, and
  puts Unfiled last. `SnippetGroup(FolderId, FolderName, Items)` is in `SnippetGroup.cs:4`.
- **Settings:**
  - `src/SmartSnippets.Core/Settings/PopupLayout.cs:3-7` (`Flat`, `Folder`) and
    `SortField.cs:3-9`.
  - `SettingsKeys.cs:10-11` (`popup.layout`, `popup.sort`).
  - `AppSettings.cs:25-27`: defaults Flat / LastUsed. `:42-43` loads them and `:54-55` saves
    them.
- **Stale-snapshot defect:**
  - `src/SmartSnippets.Presentation/ViewModels/SettingsViewModel.cs:26,84` keeps the start-up
    `AppSettings`, and `:248` `Persist() => _settings.Save(_settingsStore)` writes **every** key,
    including `popup.layout` and `popup.sort`.
  - Once the popup persists its own choices, any theme, text-size or hotkey change made later in
    the same session reverts them.
  - `grep '\.Save('` finds only this caller.

**Editor reuse:**
- `src/SmartSnippets.Presentation/ViewModels/SnippetEditorViewModel.cs`:
  - `:118-124`: ctor `(ISnippetStore, IDialogService, long? snippetId, IClipboardService?, TimeProvider?)`.
  - `:163`: `event EventHandler<long>? Saved`.
  - `:183`: `SaveAsync` completes synchronously and has an `IsSaving` guard.
  - `:249-260`: `CreateFolderAsync` uses `_dialogs.ShowErrorAsync` on `StoreException`.
  - `:289`: `ConfirmDiscardAsync`.
- `src/SmartSnippets.App/Controls/SnippetEditor.xaml`:
  - `:7-10`: control-level accelerators, Ctrl+S save and Esc discard.
  - `:192`: the stats card.
  - `:265-270`: Delete.
  - `:276`: the unsaved-changes bar, with Discard and Save.
- `SnippetEditor.xaml.cs`:
  - `:27-56`: plain `ViewModel` and `TextSize` properties that call `Bindings.Update()`.
  - `:62-64`: instance `Show`/`Hide` helpers that return `Visibility`.
  - `:90-108`: Esc cancels the inline new-folder row, otherwise calls `ConfirmDiscardAsync`.
- `src/SmartSnippets.App/Services/DialogService.cs:15-18,71-75`: the dialog root comes from a
  delegate. When it is null, the dialog shows the main window first.

**Presentation services and text (reused):**
- `Presentation/Services/IClipboardService.cs:4-7` (UI thread only).
- `IElevation.cs:3-18`: `IsElevated`, `RestartAsAdministrator()` → `Started`, `Cancelled` or
  `Failed`.
- `SettingsViewModel.cs:23-24,81-91,208-213` shows the existing `IElevation` + `requestExit`
  pattern for restarting.
- `Presentation/Text/UiText.cs:7-79` holds all design wording. `Unfiled` is at `:11` and
  `CopyText` at `:49`; there are no popup strings yet.
- `Presentation/ViewModels/SnippetRowViewModel.cs:22-42`: `PreviewOf(text)` takes the first
  non-blank line, collapses whitespace and cuts at 80 characters, surrogate-safe. **Reused for
  popup rows.**

**Tests, tokens and platform:**
- `tests/SmartSnippets.Presentation.Tests/Fakes/FakeSnippetStore.cs` applies the real rules and
  records `Raised` events and `ChangedSubscriberCount`.
- `Fakes.cs:6-13` has `MemorySettingsStore`; `FakeClipboardService.cs` has `FixedTimeProvider`.
- `ScriptedDialogService.cs` exists.
- **Tokens and converters already in the App:** `App.xaml:14-32` (`AccentTintBrush`,
  `FolderTint(Background)Brush`, danger brushes, light/dark/high contrast).
  `App/Converters/VisibilityConverters.cs` (`NonEmptyToVisibilityConverter`,
  `InvertedBoolToVisibilityConverter`) compiles under AOT inside DataTemplates (005 §10).
- **CI:** `.github/workflows/ci.yml:5-8` runs only on pushes to `main`. The App project
  (WinUI/XAML) is **compiled for the first time when the change merges to `main`**.
- **Verify script:** `scripts/verify-plans-003-009.ps1`.
  - `:89`: `Get-Main` finds the main window by its **exact** title "Smart Snippets".
  - `:314-327` (C3), `:341` (C4) and `:430-451` (C9) wait for the log text `popup_ready_ms <int>`
    and close the popup with **Esc**.
  - The verification report is in `docs/research/002-verification-plans-003-009.md`.
- **Known WinUI 3 / AOT traps** (002 research, 009 §10):
  - `IReadOnlyList<string>` as `ItemsSource` throws E_INVALIDARG.
  - A function binding from bool to `Visibility` doesn't compile; helpers must be instance
    methods that return the target type.
  - There is no `FlowLayout`/`WrapPanel`.
  - `DisplayMemberPath` breaks AOT (WMC1510).
  - No reflection.

## 4. Design

### 4.1 Approach

**Layering.**
- **`PopupViewModel` (Presentation, unit-tested on Linux)** owns all popup behaviour: query,
  rows, selection, layout, sort, the editor session, insertion and its failure state.
- **`PopupWindow` (App)** is a thin view. It binds with `x:Bind`, routes keys and clicks to the
  view-model, and implements **`IPopupHost`**: hide, show again, open main window, and move
  focus. The view-model calls the host and never touches Win32.
- **`AppHost`** captures the `InsertionTarget` and drives `PopupWindow.ShowFor`.
- **Insertion** goes only through `IInsertionService` (the 007 contract).

**Rows: one flat list of items.** `Items` is a `List<PopupItemViewModel>` that the view-model
replaces as a whole. It is a `List<T>`, not an `IReadOnlyList` (E_INVALIDARG trap), and it is
replaced only when the computed list differs (`SequenceEqual`).
- **Flat layout:** one snippet item per result.
- **Folder layout:** a `Heading` item ("Support (2)") before each group, using
  `SnippetGrouping.ByFolder`.
- **Item fields:** `PopupItemViewModel(Kind, SnippetId, Label, Preview, FolderChip, Tag1, Tag2, HeadingText)`.
  - The three chip slots are plain strings. An empty string hides its chip through
    `NonEmptyToVisibilityConverter`.
  - This avoids a nested `ItemsSource` per row, both for speed at 10k rows and to dodge
    E_INVALIDARG.
- **Accessible name:** `ToString()` provides the UIA name.
  - Snippet: "Refund policy, Support, support, billing".
  - Heading: "Support, 2 snippets".

**Selection.**
- The view-model owns `SelectedIndex`, which always points at a snippet item, or is −1 when there
  are none.
- `MoveSelection(±1)` skips headings and clamps at both ends with no wrap, matching the design.
- **The view:**
  - `ListView` (`SelectionMode=Single`, virtualized `ItemsStackPanel`) binds `SelectedIndex` two-way.
  - Heading containers are disabled in `ContainerContentChanging` (`IsEnabled=false`,
    `IsTabStop=false`), so a click or arrow key can't select them.
  - The selected row uses `AccentTintBrush` plus the 3 px accent bar from the design.
- **Focus** stays in the search box, as in the design: the box's `KeyDown` handles Up/Down/Enter.
  The window scrolls the selected row into view. It also raises a UIA notification with the
  row's name, so Narrator announces the selection while focus is in the search box.
- **Selection resets to the first snippet** on a fresh open, a query change, a layout change and
  a sort change (design: `st.sel=0`).
- On `store.Changed` the selection is kept **by ID** if that snippet is still visible; otherwise
  it moves to the first snippet.

**Pipeline and caching** (AC13: popup ≤ 300 ms, search ≤ 150 ms at 10k).
1. On a store change or a sort change: `sorted = SnippetSort.Apply(store.Snippets, Sort)`, then
   `search = new SnippetSearch(sorted)`. This runs once.
2. On a query change: `search.Filter(Query)`. Filtering keeps the sorted order.
3. If the layout is Folder, `SnippetGrouping.ByFolder`.
4. Map each result to a cached `PopupItemViewModel`. The cache is keyed by snippet ID and is
   rebuilt when the snapshot or the layout changes, so a keystroke allocates only the list.

The view-model is built at start-up after `store.Load()`, so the first popup open does no work.

**Toolbar.**
- **Layout:** a `ToggleButton`. `IsChecked` = folder layout, which exposes the state to UIA.
  - Tooltip "Layout: flat" / "Layout: folder".
  - Name "Toggle layout" (design `aria-label`).
- **Sort:** a `Button` with a `MenuFlyout` of 4 `RadioMenuFlyoutItem`s.
  - The check mark follows the current sort.
  - Tooltip "Sort: last used" and so on; Name "Sort".
  - The flyout is set to `ShouldConstrainToRootBounds="True"` so it renders inside the popup window
    and can't deactivate it (R2).
- **Add snippet:** Name and tooltip "Add snippet".
- **Open main window:** Name and tooltip "Open main window".
- **Icons:** Segoe Fluent Icons glyphs in `FontIcon`. No image assets.
- Layout and sort changes are written straight away with
  `ISettingsStore.Set(SettingsKeys.PopupLayout/PopupSort, value.ToString())`, the same format as
  `AppSettings.Save`.

**Footer.**
- Left: the hint "↑↓ move · Enter insert · Esc close", or a transient status ("Snippet added and
  ready to insert", from the design). The status is cleared by the next query change or move.
- Right: "N of M snippets".
- When `HasFooterError` is set, the footer switches to the danger style. It shows the message,
  **Copy text**, and, when the outcome is `TargetElevated` and the app isn't elevated, **Restart
  as administrator**. The message is announced with `LiveSetting=Assertive`.
- The design draws an empty-state icon; we use the `SearchOff`-style glyph `&#xE721;` with the
  two design strings.

**Add snippet: an overlay inside the popup.**
- The overlay is a scrim `Grid` spanning the popup.
  - It holds a card with the title "Add snippet" and the subline "Insertion target kept:
    <DisplayName>".
  - The **`SnippetEditor` control in a new compact mode** comes next.
  - The footer reads "Ctrl+Enter saves", with Cancel and **Save**.
- While the editor is open, the popup's base content (search, toolbar, list, footer) has
  `IsEnabled=false`. This keeps Tab inside the overlay, and the view-model ignores deactivation.
- **Compact mode** adds `IsCompact` to `SnippetEditor`, a plain property like `ViewModel`.
  - It collapses the stats card, Delete and the unsaved-changes bar. Each is wrapped in an outer
    element with `Visibility="{x:Bind Hide(IsCompact)}"`, so the existing inner conditions stay
    as they are and no combined function bindings are needed.
  - It routes Esc to a new `EscapePressed` event instead of `ConfirmDiscardAsync`. Esc in the
    inline new-folder row still only cancels the row.
  - The Snippets page doesn't set `IsCompact`, so it is unchanged.
- **Ctrl+Enter** is handled in the overlay's `PreviewKeyDown`, which tunnels, so the multi-line
  Text box can't turn it into a newline (R7). Ctrl+S keeps working through the control's
  accelerator.
- **The editor's dialogs** (inline-folder `StoreException`, the Esc discard question) use a
  **popup-scoped `DialogService`** whose root is `popup.Content.XamlRoot`. It is a no-op if
  that is null and never shows the main window. A `ContentDialog` lives inside the popup window,
  so it doesn't deactivate it.

**Target capture (App).**
- `TargetCapture.CaptureForeground()` builds the target for the hotkey.
  `TargetCapture.CaptureFrom(HWND)` does the same for the tray, using the window that
  `TrayIcon.PreviousAppWindow` found. That removes the `SetForegroundWindow` round trip.
- Both reuse `ForegroundTarget`'s rules (no window, or our own process, gives null). They build
  `InsertionTarget(hwnd, pid, MonitorFromWindow(hwnd), DisplayName)`.
- **`DisplayName` = "<friendly process name>, <window title>"**, formatted by the pure
  `TargetDisplayName.Format(friendly, exeName, title)` in Presentation (unit-tested):
  - **Friendly name:** the file's `FileDescription`, or else the exe name without its extension.
    It comes from `QueryFullProcessImageName` (`PROCESS_QUERY_LIMITED_INFORMATION`, which works
    for elevated targets) and is **cached per image path**, so capture stays well under 5 ms.
  - **Title:** `GetWindowText`. A trailing " - <friendly>" or " — <friendly>" is stripped, so
    "Untitled - Notepad" becomes "Notepad, Untitled", as in the design.
  - **Empty title, or a title equal to the friendly name:** the friendly name only.
  - **`ApplicationFrameHost`:** the title only.
  - **Length:** cut to 60 characters with "…", surrogate-safe.
  - **Neither name available:** "the target app".
- **The title may contain user content** (document names, mail subjects), so **`DisplayName` and
  titles are never logged.** Logs carry only `pid`.

**Insertion and failure.**
- `InsertSelectedAsync`:
  1. Guard on `IsInserting`.
  2. `host.Hide()`.
  3. `IInsertionService.InsertAsync(Target, id)`.
- **Success:** the popup stays hidden.
- **`Busy`:** logged and otherwise ignored. Another insertion owns the outcome, and the
  view-model's own guard makes this unreachable from the popup.
- **Any other outcome, or an exception** (treated as `PasteFailed` with
  `UiText.InsertFailedGeneric`):
  - Sets the footer error, then calls `host.ShowAgain()`.
  - That shows the window for the **same** target. It does not call `Open`, so the query,
    layout, sort and selection are kept.
- **Copy text** puts the failed snippet's text (looked up from `store.Snippets` by ID) on the
  clipboard. It is hidden for `SnippetMissing`.
- **Restart as administrator** follows the `SettingsViewModel` pattern. `Started` →
  `requestExit()`. `Cancelled` or `Failed` → its message replaces the footer error.

**Re-showing without the hotkey's foreground rights (R1).**
- `PopupWindow.ShowAgain` re-centres the popup on `Target.MonitorHandle`, falling back to the
  primary monitor if the handle is stale.
- It then calls `ForegroundActivation.TryActivate(hwnd)`, a new helper with the same
  direct-then-`AttachThreadInput` sequence as `ClipboardPasteInserter.cs:95-125`, kept separate so
  007 can rewrite the inserter freely.
- **If activation still fails:** the popup is visible and topmost but unfocused, and AppHost's
  `INotifier` shows a tray balloon with the same message. Logged as
  `popup_reopened focus=direct|attach|failed`.

**Open main window.** `host.OpenMainWindow()` runs `_mainWindow.ShowPage(PageKind.Snippets)`
**first**, while the popup still holds foreground, and then hides the popup.

**Hotkey while the popup is in use.**
- `PopupViewModel.Open(target)` returns a `PopupOpenMode`:
  - **`Ignored`** while an insertion is in flight.
  - **`Resumed`** while the add-snippet editor is open. The original target and the editor are
    kept, and the window is just re-activated (PRD §3.7: preserve the original insertion target
    while the editor is open).
  - **`Fresh`** otherwise: new target, empty query, cleared footer, first snippet selected.
- If the popup itself is foreground, capture returns null (own process). The hotkey is then
  ignored, as today.

**Tray state.** `AppHost.UpdateTrayState()` becomes the **only** writer:
`popupVisible ? PopupOpen : hotkey registered ? Ready : HotkeyUnavailable`. It is called from
`PopupWindow.VisibleChanged` and from `IHotkeyManager.StatusChanged`.

**Interim insertion (until 007).** `App/Services/InterimInsertionService : IInsertionService`
does the following:
- Looks up the text from `store.Snippets`; if the snippet is gone → `SnippetMissing`.
- Maps the target to `ForegroundTargetInfo` and calls the existing `ClipboardPasteInserter`.
- Maps outcomes: `Success` → `Inserted`, `SendInputFailed` → `PasteFailed`, the others by name.
- On success, calls `store.RecordUsage(id)`. A `StoreException` there is logged and the result
  is still `Inserted`.
- Is single-flight: a second call returns `Busy`.
- **Passes the 002 messages through unchanged.** 007 replaces this class and owns the wording,
  CRLF normalization and the clipboard-history exclusion. Swapping it is a one-line change in
  `AppHost`.

### 4.2 Alternatives considered

| Topic | Option | Verdict |
|---|---|---|
| Folder layout | **One `ListView` over a flat list of heading + snippet items; the view-model owns the index** | **Chosen.** Up/Down heading-skip, first selection, keep-by-ID and the "N of M" count are all plain list logic, unit-tested on Linux. Switching layout means swapping one list. |
| | `ListView` with a grouped `CollectionViewSource` and `GroupStyle` headers | Rejected. Grouping goes through `ICollectionView`/`ICustomPropertyProvider`, which carries AOT risk. Selection indices span groups in ways the view-model can't test. Switching between grouped and flat changes the source type. |
| | `ItemsRepeater` | Rejected. No built-in selection or UIA list/selection pattern, so accessibility would have to be hand-built. |
| Add snippet host | **Overlay inside the popup window** | **Chosen.** This is the design. No second window means no activation change, so no deactivation close and no foreground juggling. The target stays in the same view-model. |
| | Separate editor window | Rejected. Activating it deactivates the popup. Focus would have to come back to the popup after Save (foreground lock), and the second window would need its own XamlRoot, theme and placement. |
| | Main-window editor | Rejected. It dismisses the popup, against PRD §3.7 ("without dismissing the popup"). |
| Editor UI | **Reuse `SnippetEditor` with a new `IsCompact` mode** | **Chosen.** One implementation of the label/auto hint, folder plus inline New, tags, validation and inline save failure. 005 promised "006 hosts it". The change is additive and leaves the Snippets page untouched. |
| | Reuse it unchanged | Rejected. Delete, the stats card and the unsaved bar (whose Save duplicates the overlay's Save) don't belong in an add dialog. Its Esc asks and then *stays open*, but the popup needs Esc to return to the list. |
| | New popup-only editor control on the same view-model | Rejected. It duplicates about 300 lines of XAML (label toggle, inline folder row, error rows, save-failure InfoBar), which would drift apart. |
| Selection and keyboard | **Focus stays in search; the view-model moves selection; UIA notification on change** | **Chosen.** Matches the design's key handling. Typing never loses focus. |
| | Focus moves into the list on Down | Rejected. Typing then needs a refocus, and Esc/Enter handling would be split across controls. |
| Insertion call | **View-model → `IInsertionService` (007 contract, by ID)** | **Chosen.** Testable with a fake. 007 resolves the text and records usage, so 006 never duplicates those rules. |
| | Window → `IInserter` with text (prototype) | Rejected. It can't be tested on Linux and pushes usage recording into the UI. |
| Layout/sort persistence | **Per-key `ISettingsStore.Set` from the popup; Settings page writes only its own keys** | **Chosen.** Fixes the stale-snapshot overwrite at its source (T5). |
| | Keep `AppSettings.Save` everywhere and share one mutable `AppSettings` | Rejected. `AppSettings` is an immutable record; sharing a mutable holder across two view-models adds coupling for two keys. |
| DisplayName timing | **Computed at capture, friendly name cached per image path** | **Chosen.** The `InsertionTarget` record is immutable and complete when the popup shows. The cache keeps capture cheap. |
| | Computed lazily when the editor opens | Rejected. The target would need to be mutable, or a second lookup could run after the target window has changed. |

### 4.3 Data, contracts and API

**No schema change and no new settings.** The popup uses the existing `popup.layout` and
`popup.sort` keys with the defaults Flat and LastUsed.

The only Core change: `AppSettings.SaveShell(ISettingsStore)` writes theme, text size and hotkey
keys. `Save` becomes `SaveShell` plus the popup keys, so its existing behaviour is unchanged.
`SettingsViewModel.Persist` calls `SaveShell` (T5).

#### 4.3.1 Shared contract with plan 007 (T0, written by the coordinator; 006 consumes, 007 implements)

```csharp
// SmartSnippets.Presentation.Services
public sealed record InsertionTarget(nint WindowHandle, uint ProcessId, nint MonitorHandle, string DisplayName);
public enum InsertionOutcome { Inserted, TargetClosed, TargetElevated, FocusFailed, ClipboardBusy, PasteFailed, Busy, SnippetMissing }
public sealed record InsertionResult(InsertionOutcome Outcome, string Message) { public bool Succeeded => Outcome == InsertionOutcome.Inserted; }
public interface IInsertionService
{
    // Resolves the snippet text from the store, normalizes line endings, dispatches the paste, and records usage only on success.
    // Single-flight: a second call while one is running returns Busy without side effects.
    Task<InsertionResult> InsertAsync(InsertionTarget target, long snippetId, CancellationToken ct = default);
}
```

File: `src/SmartSnippets.Presentation/Services/InsertionContracts.cs`. Neither 006 nor 007 may
change it without updating both plans.

#### 4.3.2 006's own contracts (T0b)

```csharp
// SmartSnippets.Presentation.Services
public enum PopupFocusTarget { Search, Editor }
public interface IPopupHost
{
    void Hide();                                  // hide without inserting; idempotent
    void ShowAgain();                             // re-show for the current target, view-model state untouched
    void OpenMainWindow();                        // show/activate main window (Snippets), then hide the popup
    void Focus(PopupFocusTarget target);
}

// SmartSnippets.Presentation.ViewModels
public enum PopupItemKind { Heading, Snippet }
public sealed record PopupItemViewModel(PopupItemKind Kind, long SnippetId, string Label, string Preview,
    string FolderChip, string Tag1, string Tag2, string HeadingText);   // ToString() = UIA name
public enum PopupOpenMode { Fresh, Resumed, Ignored }

public sealed partial class PopupViewModel : ObservableObject, IDisposable
{
    public PopupViewModel(ISnippetStore store, ISettingsStore settingsStore, AppSettings initial,
        IInsertionService insertion, IClipboardService clipboard, IElevation elevation,
        IPopupHost host, IDialogService editorDialogs, Action? requestExit = null);

    public InsertionTarget? Target { get; }
    public PopupOpenMode Open(InsertionTarget target);
    public string Query { get; set; }
    public List<PopupItemViewModel> Items { get; }
    public int SelectedIndex { get; set; }              // view writes it on click/list keys; always a Snippet item or -1
    public void MoveSelection(int delta);
    public PopupLayout Layout { get; }  public bool IsFolderLayout { get; }  public string LayoutTooltip { get; }
    public SortField Sort { get; }      public string SortTooltip { get; }
    public IRelayCommand ToggleLayoutCommand { get; }
    public void SetSort(SortField sort);
    public string HintText { get; }  public string CountText { get; }  public string StatusText { get; }
    public bool IsEmpty { get; }  public string EmptyTitle { get; }  public string EmptyBody { get; }
    public bool HasFooterError { get; }  public string FooterError { get; }
    public bool CanCopyFailedText { get; }  public bool CanRestartAsAdmin { get; }
    public IRelayCommand CopyFailedTextCommand { get; }  public IRelayCommand RestartAsAdminCommand { get; }
    public bool IsInserting { get; }
    public Task InsertSelectedAsync();
    public Task ActivateAsync(PopupItemViewModel item);  // mouse click == Enter
    public void Close();                                // Esc
    public void OnDeactivated();                        // ignored while IsEditorOpen or IsInserting
    public IRelayCommand OpenMainWindowCommand { get; }
    public SnippetEditorViewModel? Editor { get; }  public bool IsEditorOpen { get; }  public bool IsEditorClosed { get; }
    public string EditorTitle { get; }  public string EditorTargetLine { get; }
    public IRelayCommand AddSnippetCommand { get; }
    public Task SaveEditorAsync();                      // Ctrl+Enter / Save
    public Task CancelEditorAsync(bool confirmIfDirty);  // Cancel button = false, Esc = true
}
```

**App-side API that T0b fixes so Tracks B and C can work in parallel** (signatures only, no
behaviour):
- `PopupWindow(ITextSizeService textSize, Action openMainWindow)`.
- `PopupViewModel ViewModel { set; }`.
- `void ShowFor(InsertionTarget target, long hotkeyTimestamp)`.
- `void Prewarm()`.
- `event EventHandler<bool> VisibleChanged`.
- `PopupWindow` implements `IPopupHost`.
- `internal static class ForegroundActivation { bool TryActivate(HWND hwnd, out string path); }`.
- `TrayIcon(..., Action<HWND> onShowPopup, ...)`.

**New `UiText` strings (T0b):**
- `PopupSearchPlaceholder` = "Search label, text, folder, or tag".
- `PopupSearchName` = "Search snippets".
- `PopupHint` = "↑↓ move · Enter insert · Esc close".
- `CountOf(n, m)` = "N of M snippets" (singular "snippet" when M = 1).
- `FolderHeading(name, n)` = "Support (2)".
- `NoSnippetsMatch` = "No snippets match".
- `NoSnippetsMatchBody` = "Try fewer words, or add a new snippet with the plus button."
- `LayoutTooltip(layout)` = "Layout: flat" / "Layout: folder".
- `SortTooltip(sort)` = "Sort: last used" / "Sort: usage count" / "Sort: created date" /
  "Sort: updated date".
- `SortName(sort)` = "Last used" / "Usage count" / "Created date" / "Updated date".
- `ToggleLayout` = "Toggle layout".
- `SortButton` = "Sort".
- `AddSnippet` = "Add snippet".
- `OpenMainWindow` = "Open main window".
- `TargetKept(name)` = "Insertion target kept: {name}".
- `CtrlEnterSaves` = "Ctrl+Enter saves".
- `Cancel` = "Cancel".
- `SnippetAddedReady` = "Snippet added and ready to insert".
- `RestartAsAdministrator` = "Restart as administrator".
- `InsertFailedGeneric` = "Couldn't insert the snippet. Nothing was pasted." (fallback for
  exceptions only; 007 owns the per-outcome wording).

**Automation IDs** (stable handles for 008's verify sections):
- Window title: **"Smart Snippets popup"**. It must not equal "Smart Snippets"; see R12.
- Control IDs: `PopupSearch`, `PopupLayoutButton`, `PopupSortButton`, `PopupAddButton`,
  `PopupOpenMainButton`, `PopupList`, `PopupCount`, `PopupFooterError`, `PopupCopyText`,
  `PopupRestartAdmin`, `PopupEditor`, `PopupEditorSave`, `PopupEditorCancel`.

**Log events.** These are IDs, enums and numbers only. Never text, labels, tags, queries, titles
or `DisplayName`.
- Kept as is: `popup_ready_ms <n>`.
- `popup_opened mode=Fresh|Resumed pid=…`, `popup_open_ignored reason=inserting`.
- `popup_closed reason=esc|deactivated|insert|main_window`.
- `popup_layout value=…`, `popup_sort value=…`.
- `popup_insert id=… outcome=…`.
- `popup_reopened outcome=… focus=direct|attach|failed`.
- `popup_add_opened`, `popup_add_saved id=…`, `popup_add_cancelled dirty=…`.
- `popup_copy_text id=…`.

### 4.4 Sequence diagrams

**Hotkey or tray → capture → show:**
```
WM_HOTKEY → HotkeyService.HotkeyPressed(ts) → AppHost.OnHotkeyPressed(ts)
  → TargetCapture.CaptureForeground()            GetForegroundWindow, pid, MonitorFromWindow,
                                                 DisplayName (cached friendly name + title; not logged)
     ├─ null (own process / no window) → log hotkey_ignored_own_or_no_window → stop
     └─ target → PopupWindow.ShowFor(target, ts)
          → PopupViewModel.Open(target)
               ├─ Ignored  (IsInserting)   → log popup_open_ignored → stop
               ├─ Resumed  (IsEditorOpen)  → Target and editor kept → activate, Focus(Editor)
               └─ Fresh    → Target = target; Query = ""; footer/status cleared; select first snippet
          → WindowPlacement.CenterOn(AppWindow, target.MonitorHandle, 640, 480)   (work area, clamped, DPI)
          → AppWindow.Show → Activate → SetForegroundWindow (hotkey grants the right) → Focus(Search)
          → VisibleChanged(true) → AppHost.UpdateTrayState() → TrayState.PopupOpen
          → Activated / LayoutUpdated → log popup_ready_ms <n>

Tray "Show popup" → TrayIcon.ShowMenu → PreviousAppWindow(prevForeground) = hwnd (shell/own skipped)
  → onShowPopup(hwnd) → posted → AppHost.ShowPopupFromTray(hwnd)
  → TargetCapture.CaptureFrom(hwnd) → same ShowFor path (no SetForegroundWindow round trip)
```
Parallel paths checked:
- `grep -n "ShowFor\|ForegroundTarget.Capture" src/` returns only `AppHost.cs:219,226` and
  `PopupWindow.xaml.cs:84`.
- After 006, `ForegroundTarget.Capture` is called only from `TargetCapture`, and `ShowFor` only
  from `AppHost`'s two entry points.
- The wiring audit (T12) re-greps this.

**Enter or click → insert → success or failure → re-open:**
```
SearchBox KeyDown(Enter) | ListView ItemClick → PopupViewModel.InsertSelectedAsync / ActivateAsync(item)
  → guard: IsInserting or no snippet selected → return (no second call)
  → IsInserting = true; FooterError cleared
  → host.Hide()            → Deactivated → OnDeactivated() ignored (IsInserting) → VisibleChanged(false) → tray Ready
  → IInsertionService.InsertAsync(Target, id)
       006: InterimInsertionService → ClipboardPasteInserter (focus restore, paste) → RecordUsage on success
       007: production engine (same contract)
  ├─ Inserted → IsInserting=false; log popup_insert outcome=Inserted; popup stays hidden
  ├─ Busy     → IsInserting=false; log only (unreachable from the popup's guard)
  └─ other outcome, or exception → PasteFailed + UiText.InsertFailedGeneric
       → FooterError = result.Message; FailedSnippetId = id
         CanCopyFailedText = outcome != SnippetMissing
         CanRestartAsAdmin = outcome == TargetElevated && !elevation.IsElevated
       → IsInserting = false → host.ShowAgain()        (no Open(): Query, Layout, Sort, SelectedIndex kept)
            → CenterOn(Target.MonitorHandle) → ForegroundActivation.TryActivate(popup)
                ├─ direct | attach → Focus(Search); log popup_reopened focus=…
                └─ failed → visible, topmost, unfocused + INotifier balloon(message); log focus=failed
  Copy text                → clipboard.SetText(store text of FailedSnippetId); log popup_copy_text id=…
  Restart as administrator → elevation.RestartAsAdministrator()
                               ├─ Started → requestExit() (AppHost ShutDown, as in Settings)
                               └─ Cancelled|Failed → FooterError = result.Message
```
Parallel paths checked: the prototype's `InvokeAsync` and `MessageBox` (`PopupWindow.xaml.cs:148-188`)
are deleted. After 006, `IInserter` is referenced only by `InterimInsertionService` and
`ClipboardPasteInserter`, and 007 removes both.

**Add snippet → save → refresh and select:**
```
Add button → AddSnippetCommand → snapshot (Query, SelectedId)
  → Editor = new SnippetEditorViewModel(store, editorDialogs /*popup XamlRoot*/, null, clipboard)
  → Editor.Saved += OnEditorSaved; IsEditorOpen = true (base content disabled; deactivation ignored)
  → host.Focus(Editor) → SnippetEditor.FocusText()
Ctrl+Enter (overlay PreviewKeyDown) | Save | Ctrl+S → SaveEditorAsync → Editor.SaveAsync()
  → store.CreateSnippet(draft)
     ├─ validation fail / StoreException → inline errors / save-failure InfoBar; overlay stays open
     └─ ok → store.Changed → PopupViewModel.OnStoreChanged → re-sort, re-filter (keeps selection by id)
          → Editor.Saved(id) → OnEditorSaved(id)
               → close editor (unsubscribe, Dispose); IsEditorOpen = false
               → if id ∉ search.Filter(Query) → Query = ""   (else query kept)
               → SelectedIndex = index of id; StatusText = "Snippet added and ready to insert"
               → host.Focus(Search)   Target unchanged → Enter inserts id into the original window
Cancel button → CancelEditorAsync(false) → close editor → restore snapshot (Query, selection by id); log popup_add_cancelled
Esc (SnippetEditor.EscapePressed, compact) → CancelEditorAsync(true)
     → Editor.IsDirty ? editorDialogs.ConfirmAsync(DiscardTitle, DiscardBody, Discard) : true
        ├─ true  → as Cancel
        └─ false → stay in editor (Keep editing)
```
Parallel paths checked:
- Snippet creation goes only through `ISnippetStore.CreateSnippet`, through the same
  `SnippetEditorViewModel` as the main window.
- The main window's `SnippetListViewModel` updates from the same `Changed` event (master §7
  check 1).

### 4.5 Surface list

| Surface | Change | Task |
|---|---|---|
| `Presentation/Services/InsertionContracts.cs` | new (shared 006/007 contract, verbatim) | T0a |
| `Presentation/Services/IPopupHost.cs`, `Presentation/ViewModels/PopupItemViewModel.cs`, `PopupViewModel.cs` (surface stub), `Presentation/Text/UiText.cs` (popup strings) | new / change | T0b |
| `tests/.../Fakes/FakeInsertionService.cs`, `FakePopupHost.cs` | new | T0b |
| `Presentation/ViewModels/PopupViewModel.cs` + `PopupViewModel*Tests.cs` | implement | T1–T4 |
| `Core/Settings/AppSettings.cs` (`SaveShell`), `Presentation/ViewModels/SettingsViewModel.cs` (`Persist`) + tests | change (defect fix) | T5 |
| `App/Popup/PopupWindow.xaml(.cs)` | rewrite (replaces 002 prototype) | T6, T7, T8 |
| `App/Controls/SnippetEditor.xaml(.cs)` | add `IsCompact`, `EscapePressed` | T8 |
| `Presentation/Text/TargetDisplayName.cs` + `TargetDisplayNameTests.cs` | new | T9 |
| `App/Services/TargetCapture.cs`, `ForegroundActivation.cs` | new | T9 |
| `App/Services/InterimInsertionService.cs` | new (deleted by 007) | T10 |
| `App/Services/TrayIcon.cs` (`onShowPopup(HWND)`, no `SetForegroundWindow(target)`) | change | T11 |
| `App/AppHost.cs` (capture, popup and view-model composition, popup `DialogService`, `UpdateTrayState`, interim insertion) | change | T11 |
| `App/Services/IInserter.cs`, `ClipboardPasteInserter.cs`, `ForegroundTarget.cs` | unchanged (consumed) | — |
| Plan 001 progress row, plan 006 §10 notes | docs | T12 |

## 5. Impact and risk

| Question | Answer |
|---|---|
| State management: what survives a crash mid-operation? | Layout and sort are written immediately on change. A crash loses the popup's query and an unsaved add-snippet draft. The PRD asks for no drafts; same as 005. An interrupted insertion is 007's concern (clipboard restore). |
| Data consistency: orphans, races, double-apply? | Snippets are created only through the store, so no new write path exists. Double Enter or a double click is blocked twice: by `IsInserting` in the view-model and by the contract's single-flight `Busy`. The Settings page no longer overwrites popup keys (T5). A folder created inline is kept even if the add is cancelled (005 decision). |
| User experience: root problem or symptom; any surprise? | Root: the prototype inserts a test string. Possible surprises: the query resets on every fresh open (design); Esc in a dirty add form asks first (§9 Q1); the popup stays up when you click away while the editor is open (by requirement). |
| Backward compatibility | Existing `popup.layout`/`popup.sort` values are read as before; invalid values fall back to the defaults and log `settings_invalid`. No schema change. The main-window editor behaves the same (`IsCompact` defaults to false). |
| Error recovery | Insert failures re-open the popup with the message, Copy text and, when relevant, Restart as administrator. Editor save failures use the existing inline InfoBar with Try again and Copy text. If re-activation is blocked, a tray balloon carries the message (R1). |
| Logging & debugging | §4.3.2 events cover open, close, insert, re-open and add. The `focus=` field on `popup_reopened` diagnoses the foreground lock. No titles or user text (privacy rule, master §4). |
| Edge cases | Covered: empty library (empty state, Add still works); query with no match; 10k snippets; folder layout with only Unfiled; a folder renamed or deleted, or a snippet deleted, while the popup is open (rebuild, keep by ID); hotkey while the editor is open or while inserting; a target closed before Enter; an elevated target; a target on a second monitor with different DPI; text size 32; high contrast; the selected snippet deleted from the main window while the popup shows a failure (Copy text hidden: `SnippetMissing` lookup gives null). |

**Risks:**
- **R1 — Foreground lock on re-show.** After a failed insertion, our process may no longer be
  allowed to take the foreground, so `ShowAgain` can show an unfocused popup.
  - Mitigations: direct `SetForegroundWindow`, then the `AttachThreadInput` fallback (proven in
    002), then a tray balloon with the message.
  - Logged as `focus=`. Runbook steps 17–18 check it.
- **R2 — Windowed popups deactivating the popup.** In WinUI 3, a flyout or `ComboBox` dropdown can
  render in its own HWND. Opening one could fire `Deactivated` and close the popup.
  - The sort `MenuFlyout` uses `ShouldConstrainToRootBounds="True"`.
  - The folder `ComboBox` is only in the editor, where deactivation is ignored.
  - Tooltips don't activate.
  - Runbook steps 9 and 13 check it.
- **R3 — Mixed-DPI monitors.**
  - Problem: `MoveAndResize` onto a monitor with a different DPI sends `WM_DPICHANGED`, and the
    window may re-size itself to a suggested rectangle, so the popup is off-centre or the wrong
    size.
  - Mitigation: after the move, if `AppWindow.Size` differs from the requested size, call
    `CenterOn` once more (still under 1 ms).
  - Runbook step 3 checks it.
- **R4 — 10k items and ready time.**
  - The pipeline is cached (§4.1), the `ListView` is virtualized, and `Items` is replaced only on
    change.
  - Item templates use `x:Phase` for the preview and chips.
  - Checks: `popup_ready_ms` on runbook step 21; filtering cost is already measured by 003's
    benchmark (< 15 ms).
- **R5 — XAML that compiles only on Windows CI** (first compile on `main`). Known traps are
  avoided by design:
  - `List<T>` sources, enforced by the test `Items_IsConcreteList`.
  - Instance helpers that return `Visibility`.
  - Converters in DataTemplates, as in FoldersPage.
  - A typed template instead of `DisplayMemberPath`.
  - No `FlowLayout`/`WrapPanel`: chips are a horizontal `StackPanel` with 3 fixed slots.

  Expect fix-forward PRs anyway (§8).
- **R6 — Stale settings overwrite.** Confirmed in code (§3). Fixed in T5, with a regression test
  in Presentation and one in Core.
- **R7 — Ctrl+Enter swallowed** by the multi-line Text box. Handled in the overlay's tunnelling
  `PreviewKeyDown`. Runbook step 12.
- **R8 — Text size 32 in a 640 × 480 popup.** Rows get taller, so fewer are visible. The editor
  overlay scrolls (the `SnippetEditor` field area is already a `ScrollViewer`, `:18`), and the
  overlay footer (Cancel/Save) stays pinned. Runbook step 20.
- **R9 — Esc routed twice in the editor** (the control's accelerator and the popup root's
  `KeyDown`). The root handler returns early while `IsEditorOpen`. Compact mode raises one
  `EscapePressed`.
- **R10 — Accessibility.**
  - Narrator would not announce selection changes while focus is in the search box. Mitigated
    with a UIA notification on each selection change.
  - Headings are disabled list items whose UIA name includes the count.
  - Footer errors use `LiveSetting=Assertive`.
  - Runbook step 19.
- **R11 — Privacy.** `DisplayName` holds window titles. It is shown in the UI only. The wiring
  audit greps that no `AppLog` call takes `DisplayName`, `Title`, `Query`, `Label` or `Text`.
- **R12 — Verify-script collisions.**
  - The popup's title must not be exactly "Smart Snippets" (`Get-Main`, script `:89`).
  - The `popup_ready_ms <int>` format and Esc-to-close must stay as they are (C3, C4, C9).
  - The popup reuses the names "Search snippets" and "Copy text". This is safe because the
    script scopes searches to a window root, but **008 must locate the popup by its title or
    AutomationIds**.
  - 006 does **not** edit the script. The popup sections (search, layout, sort, add, failure
    footer) belong to 008's extension of `scripts/verify-plans-003-009.ps1`, or a new
    `verify-plans-006-008.ps1`.
- **R13 — Interim adapter differs from 007.** It uses the 002 message wording and doesn't
  normalize lone-CR line endings (research 002, WARN D1.eol), so multi-line snippets may paste
  with CR-only breaks until 007. This is accepted and noted on the runbook.

**Rollback:** revert the 006 merge commit(s). This restores the 002 prototype popup and its
`AppHost` wiring. No data migrates; popup keys that were already written remain valid.

## 6. Tasks

**Tracks** (as in 009: one coordinator, Sonnet subagents per track, each track owns its files
exclusively):
- **T0a, then T0b,** by the coordinator, committed before any track starts.
- **Track A, Presentation:** T1 → T2 → T3 → T4, and T5.
- **Track B, App popup view:** T6 → T7 → T8.
- **Track C, App capture and wiring:** T9 → T10 → T11.
- **T12, wiring audit,** by the coordinator.

Tracks B and C compile only on Windows CI, so they code against the T0b signatures, and the
coordinator cross-checks every `x:Bind` path against the view-model surface before the merge.

### T0a — Shared insertion contract (coordinator; shared with 007) (AC9/AC10 hand-off)
- **Files:** `src/SmartSnippets.Presentation/Services/InsertionContracts.cs`.
- **Change:** §4.3.1, verbatim.
- **Verify:** `dotnet build src/SmartSnippets.Presentation` has 0 warnings, and the existing
  Presentation tests pass.
- **Test that dies if this breaks:** `FakeInsertionService` (T0b) implements the interface. Any
  signature drift breaks the test build.

### T0b — 006 contracts, stubs and fakes (coordinator) (AC4–AC8)
- **Files:**
  - `Presentation/Services/IPopupHost.cs`.
  - `Presentation/ViewModels/PopupItemViewModel.cs` (complete: record + `ToString`).
  - `Presentation/ViewModels/PopupViewModel.cs`: the public surface from §4.3.2, with bodies that
    throw `NotImplementedException`.
  - `Presentation/Text/UiText.cs`: the popup strings.
  - `tests/.../Fakes/FakeInsertionService.cs`: scripted results, a recorded call log, an optional
    `TaskCompletionSource` to hold a call open, and a `ThrowNext` switch.
  - `tests/.../Fakes/FakePopupHost.cs`: records `Hide`, `ShowAgain`, `OpenMainWindow` and
    `Focus` in order.
- **Change:** the App-side signatures listed in §4.3.2 go into this plan's §10 as the binding
  contract for Tracks B and C.
- **Verify:**
  - Presentation builds and the existing tests pass.
  - `AccessibleNameTests` gets `PopupItem_ToString_SnippetAndHeading`.

### T1 — List, search, layouts, sorts, persistence (Track A) (AC6, AC7, AC3)
- **Files:** `PopupViewModel.cs`, `tests/.../PopupViewModelListTests.cs`.
- **Change:** the §4.1 pipeline and cache.
  - Rows: chips by layout; Unfiled chip; up to 2 tags; preview via `SnippetRowViewModel.PreviewOf`.
  - Headings "Folder (n)".
  - Count and empty state.
  - Tooltips; `ToggleLayoutCommand` and `SetSort` persist per key.
  - Rebuild on `store.Changed`, keeping the selection by ID.
- **Verify:** `dotnet test tests/SmartSnippets.Presentation.Tests --filter PopupViewModelList`.
- **Tests that die if a hop breaks:**
  - `Open_DefaultSort_LastUsed_NeverUsedLast`, `Sort_TiesByLabelThenId`.
  - `Search_AndTerms_SubstringCaseInsensitive_AcrossLabelTextFolderTags`.
  - `Search_KeepsLayoutAndSort`.
  - `Flat_RowChips_FolderOrUnfiled_PlusAtMostTwoTags`, `Preview_FirstNonBlankLine`.
  - `FolderLayout_HeadingsWithCounts_FoldersByName_UnfiledLast`, `FolderLayout_RowsHaveNoFolderChip`.
  - `ToggleLayout_PersistsKey_AndTooltip`, `SetSort_PersistsKey_AndTooltip`.
  - `Ctor_ReadsLayoutAndSortFromSettings`.
  - `CountText_NOfM`.
  - `Empty_ShowsNoSnippetsMatch_AndBody`.
  - `StoreChanged_Rebuilds_KeepsSelectionById`, `FolderRenamed_UpdatesHeadingAndChips`.
  - `SameResult_DoesNotReplaceItems`.
  - `Items_IsConcreteList`: guards the E_INVALIDARG class.
  - `Dispose_Unsubscribes` (`ChangedSubscriberCount` returns to its prior value).

### T2 — Open, selection, keyboard, close, deactivation (Track A) (AC4, AC5)
- **Files:** `PopupViewModel.cs`, `tests/.../PopupViewModelNavigationTests.cs`.
- **Change:**
  - `Open` with its three modes.
  - `MoveSelection` skips headings, clamps, and is a no-op when empty.
  - A query, layout or sort change selects the first snippet.
  - `Close` → `host.Hide()`.
  - `OnDeactivated` is ignored while the editor is open or an insertion is running.
  - `OpenMainWindowCommand` → `host.OpenMainWindow()`.
- **Verify:** `dotnet test ... --filter PopupViewModelNavigation`.
- **Tests that die if a hop breaks:**
  - `Open_Fresh_ClearsQuery_SelectsFirstSnippet`, `Open_FolderLayout_SelectsFirstSnippetNotHeading`.
  - `MoveDown_SkipsHeading`, `MoveUp_AtTop_Stays`, `MoveDown_AtBottom_Stays`, `Move_Empty_NoOp`.
  - `QueryChange_SelectsFirst`.
  - `Close_HidesHost`, `Deactivated_Closes`.
  - `Deactivated_WhileEditorOpen_Ignored`, `Deactivated_WhileInserting_Ignored`.
  - `Open_WhileEditorOpen_Resumed_KeepsTargetAndEditor`, `Open_WhileInserting_Ignored`.
  - `OpenMainWindow_CallsHost`.

### T3 — Add snippet (Track A) (AC8)
- **Files:** `PopupViewModel.cs`, `tests/.../PopupViewModelEditorTests.cs`,
  `tests/.../PopupViewModelIntegrationTests.cs`.
- **Change:** the §4.4 add flow: snapshot and restore, popup-scoped dialogs, the Saved handler,
  clearing the query only if it hides the new snippet, select and status, `CancelEditorAsync`
  with and without confirmation, and `EditorTargetLine`.
- **Verify:** `dotnet test ... --filter PopupViewModelEditor|PopupViewModelIntegration`.
- **Tests that die if a hop breaks:**
  - `AddSnippet_OpensEditor_TargetLineShowsDisplayName`, `AddSnippet_TargetUnchanged`.
  - `Save_SelectsNewSnippet_ClosesEditor_FocusSearch_ShowsStatus`.
  - `Save_ClearsQuery_OnlyWhenItWouldHide`, `Save_KeepsQuery_WhenVisible`.
  - `Save_ThenEnter_InsertsNewIdIntoOriginalTarget`.
  - `Save_Invalid_StaysOpen_InlineError`, `Save_StoreException_StaysOpen_InlineFailure`.
  - `Cancel_RestoresQueryAndSelection`, `Esc_Dirty_KeepEditing_StaysOpen`,
    `Esc_Dirty_Discard_Closes`, `Esc_Clean_ClosesWithoutDialog`.
  - `Close_DisposesEditor_Unsubscribes`.
  - Integration, against a real `SnippetStore` on temp SQLite:
    `AddThenInsert_RealStore_SelectsAndInsertsSavedId` and
    `LayoutAndSort_SurviveAppSettingsReload`.

### T4 — Insertion, failure footer, re-open (Track A) (AC5; AC9/AC10 hand-off)
- **Files:** `PopupViewModel.cs`, `tests/.../PopupViewModelInsertionTests.cs`.
- **Change:** `InsertSelectedAsync`, `ActivateAsync`, the outcome handling from §4.4, Copy text,
  and Restart as administrator.
- **Verify:** `dotnet test ... --filter PopupViewModelInsertion`.
- **Tests that die if a hop breaks:**
  - `Enter_HidesBeforeInsert_PassesTargetAndId`: checks the host call order against the fake
    call log.
  - `Click_EqualsEnter`, `Enter_NoSelection_NoCall`, `DoubleEnter_SingleCall`.
  - `Success_StaysHidden_NoFooterError`.
  - `Failure_ShowsAgain_SameTargetQuerySelection_ShowsMessage`.
  - `Failure_CopyText_CopiesStoreText`, `SnippetMissing_HidesCopyText`.
  - `TargetElevated_ShowsRestart_WhenNotElevated`, `TargetElevated_NoRestart_WhenElevated`.
  - `Restart_Started_RequestsExit`, `Restart_Cancelled_ShowsMessage`.
  - `Busy_NoReopen`, `InsertThrows_TreatedAsFailure_Reopens`.
  - `NextFreshOpen_ClearsFooterError`.
  - `ViewModel_NeverRecordsUsage`: the fake store's `RecordUsage` count stays 0, because usage is
    the service's job.

### T5 — Settings page must not overwrite popup keys (Track A) (AC3)
- **Files:**
  - `src/SmartSnippets.Core/Settings/AppSettings.cs`: add `SaveShell`; `Save` = `SaveShell` + popup keys.
  - `src/SmartSnippets.Presentation/ViewModels/SettingsViewModel.cs:248`: `Persist` calls `SaveShell`.
  - Tests in both test projects.
- **Verify:** `dotnet test tests/SmartSnippets.Core.Tests` and the Presentation tests.
- **Tests that die if this breaks:**
  - `AppSettings_SaveShell_WritesNoPopupKeys` (Core).
  - `ThemeChange_AfterPopupLayoutChange_KeepsPopupLayout` (Presentation, using
    `MemorySettingsStore`).

### T6 — Popup window shell (Track B) (AC4, AC13)
- **Files:** `src/SmartSnippets.App/Popup/PopupWindow.xaml(.cs)`.
- **Change:**
  - Keep the presenter setup, prewarm and the `popup_ready_ms` reporting (`:61-70`, `:77-82`, `:120-130`).
  - `ShowFor(InsertionTarget, ts)` calls `ViewModel.Open`, then acts on the mode (`Ignored` →
    nothing; `Resumed` → activate and focus the editor; `Fresh` → `CenterOn` + R3 re-check, show,
    activate, focus search).
  - `Activated(Deactivated)` → `ViewModel.OnDeactivated()`.
  - `IPopupHost`: `Hide`, `ShowAgain` (with `ForegroundActivation` and the balloon fallback
    callback), `OpenMainWindow` (main window first, then hide), `Focus`.
  - `VisibleChanged` raised on show and hide.
  - Title "Smart Snippets popup".
  - Delete `TestString`, `InvokeAsync` and `ShowWarning`.
- **Verify:** CI compile + AOT publish with 0 IL warnings on `main`. Runbook steps 1–4, 16–18, 21.

### T7 — Popup content: search, toolbar, list, footer, keyboard (Track B) (AC5, AC6, AC7)
- **Files:** `PopupWindow.xaml(.cs)`.
- **Change:**
  - **Search box:** placeholder, name, two-way `Query` with `UpdateSourceTrigger=PropertyChanged`.
  - **Toolbar:** as in §4.1 (`ToggleButton`, `MenuFlyout` with `RadioMenuFlyoutItem`s and
    `ShouldConstrainToRootBounds`).
  - **`ListView`:** a typed item template (`x:DataType=PopupItemViewModel`); heading vs row via
    converters; `FontSize` from `ITextSizeService`; heading containers disabled in
    `ContainerContentChanging`; `ItemClick` → `ActivateAsync`; scroll into view and a UIA
    notification on selection.
  - **Footer:** hint or status, count, error with Copy text and Restart.
  - **Empty state.**
  - **Keys on the root `KeyDown`:**
    - Up/Down → `MoveSelection`.
    - Enter → `InsertSelectedAsync`.
    - Esc → `Close`. The flyout consumes its own Esc, and the handler does nothing while the
      editor is open.
  - **Tab order:** search → 4 buttons → list (`TabNavigation=Once`) → footer buttons.
  - Automation IDs from §4.3.2.
- **Verify:** CI compile. Runbook steps 5–11, 19, 20.

### T8 — Add-snippet overlay and SnippetEditor compact mode (Track B) (AC8)
- **Files:** `PopupWindow.xaml(.cs)`, `src/SmartSnippets.App/Controls/SnippetEditor.xaml(.cs)`.
- **Change:**
  - **Overlay:** scrim, title, target line, `SnippetEditor` (`IsCompact=true`,
    `ViewModel=ViewModel.Editor`, `TextSize`), Ctrl+Enter in `PreviewKeyDown`, Cancel/Save.
  - Base content `IsEnabled="{x:Bind ViewModel.IsEditorClosed, Mode=OneWay}"`.
  - **`SnippetEditor`:**
    - `IsCompact` (plain property, calls `Bindings.Update()`).
    - Wrappers hiding the stats card, Delete and the unsaved bar.
    - `EscapePressed` raised from `OnDiscardAccelerator` when compact (`:90-108`); the new-folder
      Esc behaviour is unchanged.
- **Verify:** CI compile. Runbook steps 12–15. Snippets-page regression (009 runbook 3, 5, 6)
  unchanged.

### T9 — Target capture and DisplayName (Track C) (AC4)
- **Files:**
  - `src/SmartSnippets.App/Services/TargetCapture.cs`: `CaptureForeground()`, `CaptureFrom(HWND)`,
    and the friendly-name cache.
  - `ForegroundActivation.cs`.
  - `src/SmartSnippets.Presentation/Text/TargetDisplayName.cs` and
    `tests/.../TargetDisplayNameTests.cs`.
- **Change:** §4.1 target capture. `ForegroundTarget.Capture` stays as the hwnd/pid/monitor
  source and is reused, not copied.
- **Verify:**
  - `dotnet test ... --filter TargetDisplayName`.
  - CI compile.
  - Runbook steps 1, 12 (the subline shows "Notepad, Untitled").
- **Tests that die if this breaks:**
  - `Format_StripsAppSuffix` ("Untitled - Notepad" / "Notepad" → "Notepad, Untitled").
  - `Format_EmptyTitle_FriendlyOnly`, `Format_NoFriendly_UsesExeName`.
  - `Format_ApplicationFrameHost_TitleOnly`.
  - `Format_Truncates60_SurrogateSafe`, `Format_NothingKnown_Fallback`.

### T10 — Interim insertion adapter (Track C) (keeps 006 runnable; replaced by 007)
- **Files:** `src/SmartSnippets.App/Services/InterimInsertionService.cs`.
- **Change:** §4.1 interim insertion. A header comment states "Deleted by plan 007."
- **Verify:** CI compile. Runbook steps 6, 16, 17. `insert_result` and `popup_insert` appear in
  the log, and usage increments after a success (Last used sort, main-window row meta).

### T11 — AppHost and tray wiring (Track C) (AC4, AC7)
- **Files:** `src/SmartSnippets.App/AppHost.cs`, `src/SmartSnippets.App/Services/TrayIcon.cs`.
- **Change:**
  - **Compose:** `popup = new PopupWindow(textSize, openMain)` → popup-scoped `DialogService` →
    `InterimInsertionService` → `new PopupViewModel(...)` → `popup.ViewModel = vm` →
    `Prewarm` → `theme.Register(popup)`.
  - **Hotkey:** `OnHotkeyPressed` → `TargetCapture.CaptureForeground()` → `ShowFor`.
  - **Tray:** `onShowPopup: hwnd => Post(() => ShowPopupFromTray(hwnd))`. Remove the tray's
    `SetForegroundWindow(target)` (`TrayIcon.cs:253-257`) and update the comment at `:280-284`.
  - **Tray state:** `UpdateTrayState()` replaces the writers at `:197-199`, and
    `popup.VisibleChanged` triggers it.
  - **Restart:** `requestExit` reuses `RestartAsAdministrator`'s `ShutDown` path.
  - **Balloon:** the `ShowAgain` balloon fallback goes through `Notices`.
  - **Exit:** `_popupViewModel.Dispose()` in `ReleaseResources`.
- **Verify:** CI compile. Runbook steps 1, 2, 9, 16–18, 22.
- **Test that dies if the view-model link breaks:** `Ctor_ReadsLayoutAndSortFromSettings`. App
  wiring itself is verified by T12's log lines.

### T12 — Wiring audit and docs (coordinator) (L)
- **Trace each new entity:**
  - `InsertionTarget`: created only in `TargetCapture`; consumed by the view-model and the
    insertion service.
  - `IInsertionService`: one implementation (interim), constructed in `AppHost`.
  - `IPopupHost`: implemented by `PopupWindow`, passed to the view-model.
  - `PopupViewModel`: constructed once, disposed on exit.
  - `store.Changed` subscribers: list, editor, folders, popup.
  - `VisibleChanged` → `UpdateTrayState`.
  - `SnippetEditor.IsCompact`: set only by the popup.
  - `EscapePressed`: subscribed only by the popup.
  - `SaveShell`: called by `SettingsViewModel`.
- **Greps:**
  - `ShowFor` and `ForegroundTarget.Capture` call sites (§4.4).
  - No `tray.State =` outside `UpdateTrayState`.
  - No `AppLog` argument containing `DisplayName|Title|Query|Label|Text`.
  - No `IReadOnlyList` property bound as `ItemsSource` in `PopupWindow.xaml`.
  - Every `x:Bind` path in `PopupWindow.xaml` exists on `PopupViewModel`, `PopupItemViewModel` or
    an instance helper.
- **Docs:** update master 001's progress row and this plan's §10 (implementation notes,
  deviations).
- **Verify:** on the runbook run, the log contains `popup_ready_ms`, `popup_opened`,
  `popup_layout`, `popup_sort`, `popup_insert`, `popup_reopened`, `popup_add_saved`,
  `popup_add_cancelled`, `popup_copy_text` and `popup_closed`.

### AC ↔ task map

| Master AC | Tasks | Evidence |
|---|---|---|
| AC3 (layout/sort persist) | T1, T5 | `ToggleLayout_PersistsKey…`, `ThemeChange_AfterPopupLayoutChange_KeepsPopupLayout`, `LayoutAndSort_SurviveAppSettingsReload`; runbook 10 |
| AC4 (open, centre, clamp, focus) | T2, T6, T9, T11 | `Open_Fresh_…`, `TargetDisplayName*`; runbook 1–4, 22 |
| AC5 (keyboard and mouse) | T2, T4, T7 | `Move*`, `Click_EqualsEnter`, `DoubleEnter_SingleCall`; runbook 5–8, 19 |
| AC6 (search UI) | T1, T7 | `Search_*`, `Empty_*`; runbook 5, 7 |
| AC7 (layouts, sorts, toolbar) | T1, T7, T11 | `FolderLayout_*`, `Sort_*`, tooltips; runbook 8–11 |
| AC8 (add snippet) | T3, T8, T9 | `AddSnippet_*`, `Save_*`, `Cancel_*`, integration; runbook 12–15 |
| AC13 (popup ≤ 300 ms, indicative) | T1, T6 | `popup_ready_ms`; runbook 21 |
| AC9/AC10 hand-off | T0a, T4, T10 | contract, `Failure_*`, `TargetElevated_*`; runbook 16–18 (full acceptance in 007) |

| Task | ACs |
|---|---|
| T0a | AC9/AC10 hand-off |
| T0b | AC4–AC8 (contracts) |
| T1 | AC3, AC6, AC7, AC13 |
| T2 | AC4, AC5 |
| T3 | AC8 |
| T4 | AC5, AC9/AC10 hand-off |
| T5 | AC3 |
| T6 | AC4, AC13 |
| T7 | AC5, AC6, AC7 |
| T8 | AC8 |
| T9 | AC4, AC8 (target line) |
| T10 | AC9 hand-off (interim) |
| T11 | AC4, AC7 |
| T12 | all of the above (audit) |

## 7. Test strategy

**Unit tests (Linux and Windows CI).**
- About 75 new Presentation tests:
  - `PopupViewModelListTests`, `…NavigationTests`, `…EditorTests` and `…InsertionTests`.
  - `TargetDisplayNameTests`.
  - The `SettingsViewModel` regression.
  - The `AccessibleNameTests` addition.
- One new Core test (`SaveShell`).
- **Fakes:** `FakeSnippetStore` (real rules), `MemorySettingsStore`, `ScriptedDialogService` (as
  the popup-scoped dialogs), `FakeClipboardService`, `FakeInsertionService`, `FakePopupHost`,
  and a fake `IElevation` (`IsElevated`, scripted `RestartAsAdministrator`).
- **Deliberately not unit-tested:** XAML layout, Win32 capture, placement and foreground
  activation, and the interim adapter. These are App-only and covered by CI compile plus the
  runbook.

**Integration (`PopupViewModelIntegrationTests`).** The real `SnippetStore` on a temp SQLite
database, with `FakeInsertionService` and `FakePopupHost`:
- Add snippet → saved row reloaded from the DB → selected → Enter → the insertion service gets
  the saved ID and the original target.
- Layout and sort written → `AppSettings.Load` on a fresh `SettingsRepository` returns them.

**Automated Windows verification.** No change in 006: the existing
`scripts/verify-plans-003-009.ps1` sections C3, C4 and C9 must still pass, because the log
format, Esc behaviour and the "Smart Snippets" main title are all kept. **Adding popup sections
to the script belongs to 008** (R12). They will use the window title and the automation IDs from
§4.3.2.

**Manual runbook** (your PC, on the AOT build from CI after the merge to `main`). Unchecked steps
block marking 006 Complete.

| # | Step | Expected |
|---|---|---|
| 1 | Open Notepad ("Untitled - Notepad"), type a few characters, press Ctrl+Shift+Space | The popup is centred on Notepad's monitor, the search box has focus, the first row is selected, the tray tooltip/state is PopupOpen, and the log has `popup_opened mode=Fresh` with no title |
| 2 | Tray icon → Show popup (Notepad was the last app) | Same popup for Notepad; the taskbar is never the target |
| 3 | Move Notepad to a second monitor with a different scale (for example 100% / 150%), press the hotkey | The popup is centred and fully inside that monitor's work area, with the right size and no clipping |
| 4 | Dock the taskbar on the left or top, press the hotkey | The popup stays inside the work area and does not cover the taskbar |
| 5 | Type `ref pol` | Only rows matching both terms; "N of M snippets" updates; the first match is selected |
| 6 | Up/Down, then Enter | The selection moves (stays put at the ends); Enter inserts the selected snippet into Notepad once; the popup stays hidden |
| 7 | Type `zzzz` | "No snippets match" / "Try fewer words, or add a new snippet with the plus button."; Enter does nothing |
| 8 | Click a row with the mouse | Same as Enter |
| 9 | Click the Layout button | Folder headings "Name (n)", folders A→Z, Unfiled last; Up/Down never land on a heading; the tooltip reads "Layout: folder"; flat rows show a folder chip, folder rows don't |
| 10 | Open Sort → Usage count; close the popup; Exit from the tray; restart; press the hotkey | Usage count and the folder layout are still selected; changing the Theme in Settings in between does not reset them |
| 11 | Open Sort, press Esc, press Esc again | The first Esc closes only the menu; the second closes the popup; the popup did not close when the menu opened |
| 12 | Add snippet | The overlay "Add snippet" shows "Insertion target kept: Notepad, Untitled"; focus is in Text; no Delete, stats or unsaved bar |
| 13 | With the query `ref`, add text "Thanks!" with tag "chat", use the folder **New** → "Quick" (open the Folder dropdown too), press Ctrl+Enter | The dropdown did not close the popup; the snippet is saved, the query is cleared (it would have hidden it), the new row is selected, and the footer says "Snippet added and ready to insert"; Enter inserts "Thanks!" into Notepad |
| 14 | Add snippet, type something, click into another app, come back with Alt+Tab or the hotkey | The popup and editor are still there with the same target |
| 15 | Add snippet, type, press Esc → Keep editing; Esc → Discard. Then Add, and Cancel at once | Discard returns to the earlier query and selection; Cancel closes without asking |
| 16 | Select a snippet, close Notepad from the taskbar's jump list without focusing it (or via Task Manager), then press Enter in the popup | The popup re-opens with the same selection and the message in a red footer; Copy text puts the snippet text on the clipboard |
| 17 | Run Notepad as administrator, press the hotkey and Enter | The footer shows the elevated message with **Restart as administrator**; clicking it shows UAC (Cancel → message in the footer) |
| 18 | Check the log after 16–17 | `popup_reopened outcome=… focus=direct` or `attach`; if `failed`, a tray balloon showed the message |
| 19 | Keyboard only, with Narrator on: Tab and Shift+Tab around the popup | Search → Layout → Sort → Add → Open main → list → footer buttons; each is announced with its name and tooltip; moving the selection is announced; headings are announced as "Folder, n snippets" and can't be selected |
| 20 | Settings → text size Custom 32, open the popup and Add snippet | Rows are larger and nothing is clipped; the editor scrolls and Cancel/Save stay visible; check light, dark and high contrast |
| 21 | Seed 10,000 snippets with the app closed: `sqlite3 "%LOCALAPPDATA%\SmartSnippets\smartsnippets.db" "WITH RECURSIVE n(i) AS (SELECT 1 UNION ALL SELECT i+1 FROM n WHERE i<10000) INSERT INTO snippets(text,label,tags,created_at,updated_at) SELECT 'Body '||i,'Snippet '||i,'',1761600000000+i,1761600000000+i FROM n;"`, start the app, press the hotkey 5 times | Every `popup_ready_ms` ≤ 300; typing stays smooth; folder layout toggles without a visible stall. Restore your profile afterwards |
| 22 | Open main window button | The main window opens on Snippets and is in front; the popup is gone; the tray state is back to Ready |

Known interim limitation (R13): until 007, multi-line snippets typed in the editor may paste with
CR-only line breaks, and the failure messages use the 002 wording.

## 8. Rollout / phasing

- **Order:** T0a → T0b (coordinator, one commit), then Tracks A, B and C in parallel (Sonnet
  subagents, as in 009), then T12. All on `claude/epic-allen-07hez6`.
- **Merge rules:**
  - Track A merges once the Linux tests are green.
  - Tracks B and C merge **together**, because `AppHost` constructs the new `PopupWindow`.
  - The coordinator runs both test projects before the PR to `main`.
- **Windows compile happens only when the change reaches `main`** (`ci.yml:5-8`), so the App
  project is first compiled by that PR's merge.
  - Plan for **fix-forward PRs**, small and one compile error class each, as for 009
    (rockyway/smartsnippets#2, #3), until the Windows job and the AOT publish (0 IL warnings) are
    green.
  - Do not stack new features on a red `main`.
- **007 order:** 007 can be detailed and built in parallel against T0a. It replaces
  `InterimInsertionService` with one line in `AppHost` plus deleting the file, so 006 is not
  reworked.
- **008** later adds the popup sections to the verify script and the tray glyphs for
  `TrayState.PopupOpen`.

## 9. Open questions

None block approval. Each design call below has a default this plan implements; confirm or flip
any of them at approval.

1. **Esc in a dirty add-snippet form asks** "Discard your changes?" (consistent with the 009 fix
   to Esc in the main editor). The **Cancel button does not ask** (the design's `closeDlg`).
2. **The query resets on every fresh open.** Re-shows after a failure keep it.
3. **Selection after a layout or sort change goes to the first snippet** (design `st.sel=0`). After
   a store change it is kept by ID.
4. **Hotkey while the editor is open** re-activates the popup with the original target. **Hotkey
   while the popup itself has focus** is ignored (today's behaviour). The alternative, toggling
   it closed, is deferred and not in the PRD.
5. **The popup size stays 640 × 480** logical pixels (prototype, design width 640). The editor
   overlay scrolls at large text sizes instead of growing the window.
6. **An empty library** shows the same empty state as "no match". An extra "No snippets yet"
   variant is deferred because the brief gives one text.
7. **The `Busy` outcome is ignored** (no re-open), because the in-flight insertion reports its
   own result.

## 10. Implementation notes and wiring audit

*(Filled in during implementation: T0b's App-side signatures as committed, deviations, CI
fix-forward history, and the T12 audit table.)*

## 11. Approval log

| Date | Event | Notes |
|---|---|---|
| 2026-10-27 | Placeholder reserved | master plan 001 |
| 2026-10-28 | Requirement brief confirmed (G1) | 006–008 brief |
| 2026-10-28 | Draft written | Awaiting approval (G2), including the §9 design calls |
