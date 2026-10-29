# 006 — Popup: search, layouts, sorts, toolbar and add snippet

**Date:** 2026-10-28
**Status:** Approved 2026-10-28 (G2); compact rows/context menu amendment in progress
**Size:** L (subplan of [001-master-smart-snippets-v1.md](001-master-smart-snippets-v1.md))
**Area:** `src/SmartSnippets.Presentation/` (popup view-model, contracts, `UiText`), `src/SmartSnippets.App/Popup/`, `src/SmartSnippets.App/Controls/SnippetEditor.xaml(.cs)`, `src/SmartSnippets.App/Services/` (target capture, tray, interim insertion adapter), `src/SmartSnippets.App/AppHost.cs`, `tests/SmartSnippets.Presentation.Tests/`, one small Core settings change
**Depends on:** 003, 004, 005, 009 (all complete, CI green on `main`), plus the **007 insertion contract** (§4.3.1), which the coordinator writes in T0 before any track starts
**Design reference:** [docs/design/001-smart-snippets-design.html](../design/001-smart-snippets-design.html), section "Global popup" and its `<script>` (`row()`, `render()`, key handling, add-snippet dialog)
**Requirement brief confirmed:** 2026-10-28 (G1, 006–008 brief)

> **User amendment — compact rows and context actions (2026-10-28).** Supersedes older row-preview/chip statements: each snippet is a single ellipsized label with muted, right-aligned folder and tags on the same row, respecting text size and accessible naming. Search still matches text, label, folder and tags. A row context menu must offer exactly Copy, Insert, Edit and Delete; preview-mode Insert closes without insertion. Menu interaction must not accidentally trigger popup deactivation, and Esc dismisses the menu before the popup. Implementation and real-UI acceptance are pending.
**Revised:** 2026-10-28 after the external review
[docs/review/006-008-implementation-readiness.md](../review/006-008-implementation-readiness.md)
and the user's decisions on it (§11)

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
- If pasting would lose part of your clipboard, the popup comes back and asks **Paste anyway?**
  first. If the paste may already have happened, it says so as a caution and never invites a
  blind retry.

This replaces the 002 prototype popup, which inserts a fixed test string.

## 2. Requirement slice (as confirmed at G1, 2026-10-28)

Pasted from the confirmed 006 slice of the 006–008 brief. Nothing below goes beyond it, except
where §9 lists a design call for approval or a bullet is marked as a user decision of 2026-10-28
(review response, §11).

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
  open or an insertion is in progress. *(Kept after review finding 8; store-change propagation is
  tested without keeping the popup open across a main-window edit, §7.)*
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
  - *(User decision, 2026-10-28, review finding 7.)* **`HotkeyUnavailable` wins over
    `PopupOpen`**: the tray shows red whenever the hotkey is not registered, even while the popup
    is open.
- **Insertion failure** (007 decides the message):
  - The popup re-opens with the same target and the same selection.
  - The message shows in the footer, with a **Copy text** button (`IClipboardService`).
  - **Restart as administrator** (`IElevation`) also shows when the outcome is `TargetElevated`.
  - On success the popup stays hidden.
- *(User decisions, 2026-10-28, review findings 1–2; 007 implements the engine side.)* **Further
  insertion outcomes the popup handles:**
  - **`ClipboardNotPreserved`** (nothing changed): the popup re-opens (same target, same
    selection) with the footer prompt "Some of what's on your clipboard can't be restored after
    pasting. Paste anyway?" and the buttons **Paste anyway** / **Cancel** (Esc = Cancel).
    **Paste anyway** hides the popup and calls `InsertAsync` again for the same snippet and target
    with `new InsertionOptions(AllowClipboardLoss: true)`.
  - **`CopiedForManualPaste`**: the popup re-opens showing the message (the snippet is on the
    clipboard; paste it yourself). No Copy text (already copied) and no retry button.
  - **`MaybePasted`**: the popup re-opens showing the message as a **caution**, not a retryable
    error (no "try again" wording; Copy text is still offered). Enter must not instantly
    re-insert.
  - **`Inserted` with a non-empty `Message`** (for example, the clipboard was not fully
    restored): the popup stays hidden and the message is shown as a tray notice through
    `INotifier`.
- **Performance:** popup ready ≤ 300 ms after the hotkey on the user's PC. Keep the
  `popup_ready_ms` log and the prewarm.
  - *(Review finding 4.)* The popup also logs **end-to-end search time**, from the search text
    change to the rendered results, in both layouts: `popup_search_ms <int> layout=<flat|folder>
    results=<n>`. 008 uses it for the ≤ 150 ms at 10k acceptance; the Core benchmark stays a
    diagnostic.
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
  - `:79-82`: `SendInputFailed` is returned when `SendInput` sends fewer events than asked, so
    part of Ctrl+V may already have reached the target (review finding 2).
  - `:86`: success returns the message "Inserted." (non-empty).
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
- `Presentation/Services/INotifier.cs:4-7`: `Notify(title, message)` (tray balloon). `AppHost`
  builds it as `Notices = new Notifier(tray)` (`AppHost.cs:57,201`).
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
- **Store-change propagation (review finding 8).** The view-model subscribes to `store.Changed`
  for its whole life, visible or hidden, so the next open always shows current data. Because
  deactivation closes the popup, a main-window edit can't happen while the popup is on screen;
  the realistic paths are (a) a change while the popup is hidden, then re-open, and (b) a change
  raised while it is shown (the add-snippet save, usage recorded after an insert). Both are
  covered by view-model tests that raise `Changed` from the fake store while the popup is shown
  and while it is hidden (T1), plus runbook step 23.

**Pipeline and caching** (AC13: popup ≤ 300 ms, search ≤ 150 ms at 10k).
1. On a store change or a sort change: `sorted = SnippetSort.Apply(store.Snippets, Sort)`, then
   `search = new SnippetSearch(sorted)`. This runs once.
2. On a query change: `search.Filter(Query)`. Filtering keeps the sorted order.
3. If the layout is Folder, `SnippetGrouping.ByFolder`.
4. Map each result to a cached `PopupItemViewModel`. The cache is keyed by snippet ID and is
   rebuilt when the snapshot or the layout changes, so a keystroke allocates only the list.

The view-model is built at start-up after `store.Load()`, so the first popup open does no work.

**End-to-end search timing (review finding 4).** The Core benchmark times only `SnippetSearch`.
The user waits for filter + grouping + row mapping + `x:Bind` update + `ListView` layout +
the composed frame, so `PopupWindow` (T7) measures that whole path in both layouts:
1. **Start:** the search `TextBox.TextChanged` handler in code-behind stores
   `Stopwatch.GetTimestamp()` as the pending start. The two-way `x:Bind` (with
   `UpdateSourceTrigger=PropertyChanged`) pushes `Query` to the view-model synchronously during
   the same input turn, and the pipeline (§4.1) runs synchronously in the setter, so `Items` is
   already replaced when control returns to the dispatcher.
2. **Results applied:** the window listens to `ViewModel.PropertyChanged` for `Items`. When it
   fires with a pending start, the window sets `awaitingLayout`. If the result was identical
   (`SequenceEqual`, no `Items` change), the window goes straight to step 4.
3. **Laid out:** the first `PopupList.LayoutUpdated` after `awaitingLayout` (the `ListView` has
   measured and arranged the realized containers for the new `ItemsSource`) sets
   `awaitingFrame`.
4. **Rendered (stop):** the first `Microsoft.UI.Xaml.Media.CompositionTarget.Rendered` after
   `awaitingFrame` stops the clock, logs `popup_search_ms <int> layout=<flat|folder>
   results=<n>` (`n` = snippet rows, not headings) and unsubscribes `Rendered`, which is attached
   only while a measurement is pending so it costs nothing at rest.
- A new `TextChanged` before the stop **replaces** the pending start, so each log line measures
  the latest edit to its rendered result. Superseded edits are not logged. 008's acceptance
  therefore sets the query in **one edit** (UIA `ValuePattern.SetValue` or a paste) and waits
  for the log line, rather than typing at machine speed.
- The query text is never logged. The line is built by the pure
  `PopupLogFormat.SearchMs(int ms, PopupLayout layout, int results)` in Presentation so its
  format is unit-tested (T1) and stays stable for 008's verify script.
- `popup_ready_ms` is unchanged.

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
- **Footer message states** (`PopupFooterState`, one at a time; the message is announced with
  `LiveSetting=Assertive` in every state except `None`):

  | State | Set by | Style | Message | Buttons | Enter / click on a row | Esc |
  |---|---|---|---|---|---|---|
  | `None` | fresh open, Cancel, Dismiss | hint or status | — | — | inserts | closes |
  | `Error` | `TargetClosed`, `TargetElevated`, `FocusFailed`, `ClipboardBusy`, `PasteFailed`, `SnippetMissing`, exception | danger (red) | `result.Message` (exception: `UiText.InsertFailedGeneric`) | **Copy text** (not for `SnippetMissing`); **Restart as administrator** for `TargetElevated` when not elevated | inserts again (nothing was pasted) | closes |
  | `ConfirmClipboardLoss` | `ClipboardNotPreserved` | caution | `UiText.ClipboardLossPrompt` (popup-owned) | **Paste anyway** (focused), **Cancel** | ignored (the prompt owns the answer) | = **Cancel** |
  | `ManualPaste` | `CopiedForManualPaste` | info (neutral) | `result.Message` (blank → `UiText.CopiedForManualPasteFallback`) | none (no Copy text: already copied; no retry) | inserts (nothing was pasted) | closes |
  | `MaybePasted` | `MaybePasted` | caution (amber, not danger) | `result.Message` (blank → `UiText.MaybePastedFallback`); no "try again" wording | **Copy text**, **Dismiss** | the **first** one only dismisses the caution; inserting needs a second, deliberate Enter or click | closes |

  - In `ConfirmClipboardLoss` and `MaybePasted` the hint line reads `UiText.ConfirmHint`
    ("Enter choose · Esc cancel") or `UiText.DismissHint` ("Enter dismiss · Esc close").
  - **Cancel** (or Esc) in `ConfirmClipboardLoss` clears the prompt and keeps the popup open with
    the same query and selection, so you can pick another snippet; a second Esc closes the popup.
  - A query change or `MoveSelection` clears `ConfirmClipboardLoss` (treated as Cancel) and
    `MaybePasted` (treated as Dismiss), so the prompt never applies to a different snippet.
  - `Error` and `ManualPaste` are cleared by the next insertion attempt or the next fresh open.
  - Footer buttons are `IsTabStop` and follow the list in Tab order. `ConfirmClipboardLoss` moves
    focus to **Paste anyway** (`host.Focus(PopupFocusTarget.Footer)`), so Enter/Space answers it
    explicitly; the other states keep focus in the search box.
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
- **Ctrl+Enter (review finding 5)** must save even when the caret is in the multi-line Text box
  (`AcceptsReturn="True"`), whose own `KeyDown` handling turns Enter into a newline and marks the
  event handled, so a bubbling `KeyDown` on the overlay never sees it. The WinUI 3 approach, in
  order (R7):
  1. **Primary: `UIElement.PreviewKeyDown` on the overlay root.** This is the WinUI 3 /
     Windows App SDK event `Microsoft.UI.Xaml.UIElement.PreviewKeyDown` (tunnelling, raised from
     the root down to the focused element *before* `KeyDown`), not a WPF borrowing. The handler
     acts only when `e.Key == VirtualKey.Enter`, Ctrl is down and Shift/Alt are up (read with
     `InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control)` and the same for
     Shift/Menu). It sets `e.Handled = true` **before** the Text box sees the key, so no newline
     is inserted, and calls `ViewModel.SaveEditorAsync()`. Plain Enter and Shift+Enter are not
     touched and still make newlines.
  2. **Fallback A, if runbook step 12a shows a newline or no save:** a
     `KeyboardAccelerator Key="Enter" Modifiers="Control"` on the overlay root
     (`ScopeOwner` = overlay, `KeyboardAcceleratorPlacementMode="Hidden"`), whose `Invoked` sets
     `args.Handled = true` and saves. Runbook step 12a is repeated to check that the
     `AcceptsReturn` Text box neither swallows the chord nor adds a newline before the
     accelerator runs.
  3. **Fallback B, if neither works on the user's PC:** ship with **Ctrl+S** (already a working
     accelerator on `SnippetEditor`, `:7-10`) and the Save button, change the overlay footer hint
     to "Ctrl+S saves", and record the deviation in §10 for the user to accept. This changes the
     design's shortcut, so it needs the user's sign-off before 006 is marked Complete.
  - A `UIElement.AddHandler(UIElement.KeyDownEvent, handler, handledEventsToo: true)` handler is
    **not** used for this: it would see Ctrl+Enter only after the Text box has already inserted
    the newline.
  - Ctrl+S keeps working through the control's accelerator in every case. `SaveEditorAsync` is
    guarded by `Editor.IsSaving` and by the editor closing, so a double route can't save twice.
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

**Insertion and outcomes.**
- `InsertSelectedAsync` (Enter; `ActivateAsync(item)` for a click selects the item first):
  1. If `FooterState == MaybePasted`: dismiss the caution (`FooterState = None`) and **return
     without inserting**. If `FooterState == ConfirmClipboardLoss`: return (the prompt owns the
     answer).
  2. Guard on `IsInserting` and on a selected snippet.
  3. Clear the footer, `host.Hide()`.
  4. `IInsertionService.InsertAsync(Target, id, default)`.
- `PasteAnywayAsync` (the **Paste anyway** button): takes the snippet ID stored when the prompt
  was raised (`PendingId`, never the current selection), clears the footer, `host.Hide()`, then
  `InsertAsync(Target, PendingId, new InsertionOptions(AllowClipboardLoss: true))`. The result is
  handled by the same table below, so it can end as `Inserted`, `CopiedForManualPaste`,
  `MaybePasted` or an `Error`.
- **Outcome handling** (every branch sets `IsInserting = false` first):

  | Outcome | Popup | Footer state | Other |
  |---|---|---|---|
  | `Inserted`, `Message` empty | stays hidden | — | — |
  | `Inserted`, `Message` non-empty | stays hidden | — | `notices.Notify(UiText.InsertNoticeTitle, result.Message)` (tray balloon via AppHost's `INotifier`) |
  | `Busy` | stays hidden | — | logged only; unreachable from the popup's own guard (§9 Q7) |
  | `ClipboardNotPreserved` | `host.ShowAgain()` | `ConfirmClipboardLoss`, `PendingId = id` | `host.Focus(Footer)` after the re-show |
  | `CopiedForManualPaste` | `host.ShowAgain()` | `ManualPaste` | — |
  | `MaybePasted` | `host.ShowAgain()` | `MaybePasted` | Copy text allowed |
  | `TargetClosed`, `TargetElevated`, `FocusFailed`, `ClipboardBusy`, `PasteFailed`, `SnippetMissing` | `host.ShowAgain()` | `Error` | Copy text unless `SnippetMissing`; Restart for `TargetElevated` when not elevated |
  | exception from `InsertAsync` | `host.ShowAgain()` | `Error`, message `UiText.InsertFailedGeneric` | treated as `PasteFailed` |

  - `ShowAgain` shows the window for the **same** target. It does not call `Open`, so the query,
    layout, sort and selection are kept.
  - **Usage is never recorded by the popup.** The service records it only for `Inserted`.
- **Copy text** puts the snippet's text (looked up from `store.Snippets` by the stored ID) on
  the clipboard. It is shown for `Error` (except `SnippetMissing`) and `MaybePasted`, and hidden
  for `ManualPaste` (the text is already there) and `ConfirmClipboardLoss`.
- **Restart as administrator** follows the `SettingsViewModel` pattern. `Started` →
  `requestExit()`. `Cancelled` or `Failed` → its message replaces the footer message (state
  stays `Error`).

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

**Tray state (review finding 7; user decision: red wins).**
- `AppHost.UpdateTrayState()` becomes the **only** writer of `tray.State`. It assigns
  `TrayStatePolicy.Compute(hotkeyRegistered, popupVisible)` and is called from
  `PopupWindow.VisibleChanged` and from `IHotkeyManager.StatusChanged`.
- `TrayStatePolicy.Compute` is a pure function in Presentation (unit-tested on Linux):
  **`HotkeyUnavailable` if the hotkey is not registered**, else `PopupOpen` if the popup is
  visible, else `Ready`. Red wins because it is a fault the user must act on; the open popup is
  already visible on screen.
- `TrayState` moves from `App/Services/TrayIcon.cs:12-17` to
  `Presentation/Services/TrayStatePolicy.cs` next to the policy (same members); `TrayIcon` uses
  it from there.
- This is the same rule and shape as 008 §4.1(f). 006 lands first, so **008 T0 adopts 006's
  `TrayStatePolicy`** and adds only the glyphs and the `tray_icon` log.

**Interim insertion (until 007).** `App/Services/InterimInsertionService : IInsertionService`
does the following:
- Implements the §4.3.1 signature exactly, including `InsertionOptions options = default`. It
  ignores `options` (the 002 inserter has no clipboard-preservation check), so it **never returns
  `ClipboardNotPreserved` or `CopiedForManualPaste`**. That is accepted until 007.
- Looks up the text from `store.Snippets`; if the snippet is gone → `SnippetMissing`.
- Maps the target to `ForegroundTargetInfo` and calls the existing `ClipboardPasteInserter`.
- Maps outcomes:
  - `Success` → `Inserted` with an **empty** `Message` (the 002 "Inserted." text is dropped, so
    a plain success raises no tray notice).
  - `SendInputFailed` → **`MaybePasted`** with `UiText.MaybePastedFallback`, and no usage: a
    partial `SendInput` may already have pasted (review finding 2), so the popup must not offer
    it as a retryable error.
  - The others by name (`TargetClosed`, `TargetElevated`, `FocusFailed`, `ClipboardBusy`).
- On `Inserted` only, calls `store.RecordUsage(id)`. A `StoreException` there is logged and the
  result is still `Inserted`.
- Is single-flight: a second call returns `Busy`.
- **Otherwise passes the 002 messages through unchanged.** 007 replaces this class and owns the
  wording, CRLF normalization, clipboard preservation and the clipboard-history exclusion.
  Swapping it is a one-line change in `AppHost`.

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
| Ctrl+Enter in the multi-line Text box | **`UIElement.PreviewKeyDown` (WinUI 3, tunnelling) on the overlay root, with an accelerator fallback** | **Chosen.** It runs before the Text box's own `KeyDown`, so the chord never becomes a newline. Fallbacks and the runbook check are in §4.1. |
| | `AddHandler(KeyDownEvent, …, handledEventsToo: true)` | Rejected as the primary route. It runs after the Text box has inserted the newline. |
| | `KeyboardAccelerator` only | Kept as fallback A. Whether an `AcceptsReturn` Text box lets the chord reach a scoped accelerator first is not documented, so it is verified on the runbook rather than assumed. |
| `MaybePasted` and Enter | **First Enter (or row click) only dismisses the caution; a second one inserts** | **Chosen.** No instant duplicate paste, yet no extra dialog. Testable in the view-model. |
| | Disable insertion until a Dismiss button is clicked | Rejected. Keyboard users would need Tab to reach Dismiss; the first-Enter rule gives the same protection. |
| | Treat it like any failure (Enter re-inserts) | Rejected. Risks a duplicate paste (review finding 2). |
| `ClipboardNotPreserved` Cancel | **Clears the prompt; the popup stays open** | **Chosen.** You can pick another snippet without the hotkey again; a second Esc closes. |
| | Cancel closes the popup | Rejected. Loses the query and selection for no benefit. |
| Search timing hook | **`TextChanged` → `Items` changed → `PopupList.LayoutUpdated` → first `CompositionTarget.Rendered`** | **Chosen.** Ends on a composed frame, so it includes binding, layout and render, in both layouts. |
| | Time only the view-model's `Query` setter | Rejected. Same blind spot as the Core benchmark (review finding 4). |
| | Stop at `LayoutUpdated` | Rejected as the end point. Layout can finish before the frame is composed; it is used only as the step before `Rendered`. |
| Tray precedence | **`HotkeyUnavailable` > `PopupOpen` > `Ready`, in `TrayStatePolicy`** | **Chosen** by the user (2026-10-28). Same rule as 008. |
| | `PopupOpen` > `HotkeyUnavailable` (earlier draft) | Rejected. It hides a persistent fault while the popup is open. |
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
// src/SmartSnippets.Presentation/Services/InsertionContracts.cs (coordinator, T0)
public sealed record InsertionTarget(nint WindowHandle, uint ProcessId, nint MonitorHandle, string DisplayName);
public enum InsertionOutcome
{
    Inserted,              // dispatched; usage counted; Message empty, or a notice (e.g. clipboard not fully restored)
    TargetClosed, TargetElevated, FocusFailed, ClipboardBusy, PasteFailed, Busy, SnippetMissing, // not dispatched; nothing changed
    ClipboardNotPreserved, // not dispatched; clipboard untouched; caller may retry with AllowClipboardLoss
    MaybePasted,           // uncertain dispatch; no usage; do not offer retry
    CopiedForManualPaste,  // AllowClipboardLoss path could not re-target; snippet left on clipboard; no usage
}
public sealed record InsertionResult(InsertionOutcome Outcome, string Message) { public bool Succeeded => Outcome == InsertionOutcome.Inserted; }
public readonly record struct InsertionOptions(bool AllowClipboardLoss);
public interface IInsertionService
{
    // Resolves the snippet text from the store, normalizes line endings to CRLF, dispatches the paste,
    // and records usage only when Outcome == Inserted. Single-flight: a concurrent call returns Busy with no side effects.
    Task<InsertionResult> InsertAsync(InsertionTarget target, long snippetId, InsertionOptions options = default, CancellationToken ct = default);
}
```

- **One file, one owner:** `src/SmartSnippets.Presentation/Services/InsertionContracts.cs`,
  written by the coordinator in T0 (this plan's T0a) before any 006 or 007 track starts. 007 uses
  the same file name (review finding 7). Neither plan may change it without updating both plans.
- **How 006 uses it:** §4.1 "Insertion and outcomes". 006 passes `default` options on a normal
  insert and `new InsertionOptions(AllowClipboardLoss: true)` only from **Paste anyway**.

#### 4.3.2 006's own contracts (T0b)

```csharp
// SmartSnippets.Presentation.Services
public enum PopupFocusTarget { Search, Editor, Footer }   // Footer = the footer's primary button (Paste anyway)
public enum TrayState { Ready, PopupOpen, HotkeyUnavailable }   // moved from App/Services/TrayIcon.cs
public static class TrayStatePolicy
{
    public static TrayState Compute(bool hotkeyRegistered, bool popupOpen);   // HotkeyUnavailable > PopupOpen > Ready
}
public static class PopupLogFormat
{
    public static string SearchMs(int ms, PopupLayout layout, int results);   // "<ms> layout=flat|folder results=<n>" for event popup_search_ms
}
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
public enum PopupFooterState { None, Error, ConfirmClipboardLoss, ManualPaste, MaybePasted }

public sealed partial class PopupViewModel : ObservableObject, IDisposable
{
    public PopupViewModel(ISnippetStore store, ISettingsStore settingsStore, AppSettings initial,
        IInsertionService insertion, IClipboardService clipboard, IElevation elevation,
        IPopupHost host, IDialogService editorDialogs, INotifier notices, Action? requestExit = null);

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
    public PopupFooterState FooterState { get; }  public string FooterMessage { get; }
    public bool HasFooterMessage { get; }  public bool IsFooterError { get; }  public bool IsFooterCaution { get; }   // Caution = ConfirmClipboardLoss | MaybePasted
    public bool IsConfirmingClipboardLoss { get; }  public bool CanDismissFooter { get; }   // Dismiss shown for MaybePasted
    public bool CanCopyFailedText { get; }  public bool CanRestartAsAdmin { get; }
    public IRelayCommand CopyFailedTextCommand { get; }  public IRelayCommand RestartAsAdminCommand { get; }
    public IAsyncRelayCommand PasteAnywayCommand { get; }  public IRelayCommand CancelPasteAnywayCommand { get; }
    public IRelayCommand DismissFooterCommand { get; }
    public bool IsInserting { get; }
    public Task InsertSelectedAsync();                  // MaybePasted: first call only dismisses; ConfirmClipboardLoss: no-op
    public Task ActivateAsync(PopupItemViewModel item);  // mouse click == Enter
    public void Close();                                // Esc: cancels ConfirmClipboardLoss if shown, otherwise hides
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
- Added by the review decisions (the popup owns these; 007 owns `result.Message` for each
  outcome):
  - `ClipboardLossPrompt` = "Some of what's on your clipboard can't be restored after pasting.
    Paste anyway?"
  - `PasteAnyway` = "Paste anyway" (the prompt's **Cancel** reuses `Cancel`).
  - `ConfirmHint` = "Enter choose · Esc cancel".
  - `Dismiss` = "Dismiss".
  - `DismissHint` = "Enter dismiss · Esc close".
  - `MaybePastedFallback` = "The snippet may already have been pasted. Check the target app before
    inserting it again." (used when `Message` is blank, and by the interim adapter).
  - `CopiedForManualPasteFallback` = "The snippet is on your clipboard. Click in the target app
    and press Ctrl+V." (used when `Message` is blank).
  - `InsertNoticeTitle` = "Snippet inserted" (tray notice title for `Inserted` with a message).
- If 007 adds strings with the same meaning to its `// Insertion (007)` section, the coordinator
  keeps one copy at merge; the names above are the ones the popup binds to.

**Automation IDs** (stable handles for 008's verify sections):
- Window title: **"Smart Snippets popup"**. It must not equal "Smart Snippets"; see R12.
- Control IDs: `PopupSearch`, `PopupLayoutButton`, `PopupSortButton`, `PopupAddButton`,
  `PopupOpenMainButton`, `PopupList`, `PopupCount`, `PopupFooterError`, `PopupCopyText`,
  `PopupRestartAdmin`, `PopupPasteAnyway`, `PopupPasteCancel`, `PopupDismiss`, `PopupEditor`,
  `PopupEditorSave`, `PopupEditorCancel`.
- `PopupFooterError` keeps its name for 008 but holds the footer message in **every**
  `PopupFooterState` (error, caution or info); the state is exposed through its
  `AutomationProperties.ItemStatus` (`Error`, `ConfirmClipboardLoss`, `ManualPaste`,
  `MaybePasted`).

**Log events.** These are IDs, enums and numbers only. Never text, labels, tags, queries, titles
or `DisplayName`.
- Kept as is: `popup_ready_ms <n>`.
- `popup_search_ms <int> layout=<flat|folder> results=<n>` (§4.1; never the query).
- `popup_paste_anyway id=…`, `popup_paste_cancelled id=…`, `popup_caution_dismissed id=…`.
- `popup_notice outcome=Inserted` (a tray notice was raised; the message is not logged).
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
          → VisibleChanged(true) → AppHost.UpdateTrayState()
               → tray.State = TrayStatePolicy.Compute(hotkeyRegistered, popupOpen: true)
                  = PopupOpen, or HotkeyUnavailable if the hotkey is not registered (red wins)
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

**Search text → rendered results (timing, §4.1):**
```
SearchBox TextChanged → start = Stopwatch.GetTimestamp()   (a newer TextChanged replaces a pending start)
  → x:Bind TwoWay → PopupViewModel.Query = text → Filter → (Folder: ByFolder) → map rows
       ├─ Items replaced → PropertyChanged(Items) → awaitingLayout
       │     → PopupList.LayoutUpdated (first after awaitingLayout) → awaitingFrame
       └─ same result (SequenceEqual) → awaitingFrame
  → CompositionTarget.Rendered (first after awaitingFrame; subscribed only while pending)
  → log popup_search_ms <ms> layout=<flat|folder> results=<n>  (PopupLogFormat.SearchMs; no query) → unsubscribe
```

**Enter or click → insert → outcome → re-open, prompt or notice:**
```
SearchBox KeyDown(Enter) | ListView ItemClick → PopupViewModel.InsertSelectedAsync / ActivateAsync(item)
  → FooterState == MaybePasted        → FooterState = None (dismiss only); log popup_caution_dismissed → return
  → FooterState == ConfirmClipboardLoss → return (focus is on Paste anyway / Cancel)
  → guard: IsInserting or no snippet selected → return (no second call)
  → IsInserting = true; footer cleared
  → host.Hide()            → Deactivated → OnDeactivated() ignored (IsInserting) → VisibleChanged(false) → UpdateTrayState()
  → IInsertionService.InsertAsync(Target, id, options: default)
       006: InterimInsertionService → ClipboardPasteInserter (focus restore, paste) → RecordUsage on Inserted
       007: production engine (same contract)
  (IsInserting = false before every branch below; log popup_insert id=… outcome=…)
  ├─ Inserted, Message ""        → popup stays hidden
  ├─ Inserted, Message non-empty → popup stays hidden → notices.Notify(InsertNoticeTitle, Message)
  │                                 (AppHost's INotifier → tray balloon); log popup_notice
  ├─ Busy                        → log only (unreachable from the popup's guard)
  ├─ ClipboardNotPreserved       → FooterState = ConfirmClipboardLoss, FooterMessage = ClipboardLossPrompt,
  │                                 PendingId = id → ReShow → host.Focus(Footer)  (Paste anyway focused)
  ├─ CopiedForManualPaste        → FooterState = ManualPaste, FooterMessage = Message (or fallback);
  │                                 no Copy text, no retry → ReShow
  ├─ MaybePasted                 → FooterState = MaybePasted, FooterMessage = Message (or fallback);
  │                                 Copy text + Dismiss → ReShow   (next Enter/click only dismisses)
  └─ TargetClosed | TargetElevated | FocusFailed | ClipboardBusy | PasteFailed | SnippetMissing,
     or exception (→ PasteFailed + UiText.InsertFailedGeneric)
                                 → FooterState = Error, FooterMessage = Message; FailedSnippetId = id
                                   CanCopyFailedText = outcome != SnippetMissing
                                   CanRestartAsAdmin = outcome == TargetElevated && !elevation.IsElevated
                                 → ReShow
  ReShow = host.ShowAgain()  (no Open(): Query, Layout, Sort, SelectedIndex kept)
            → CenterOn(Target.MonitorHandle) → ForegroundActivation.TryActivate(popup)
                ├─ direct | attach → Focus(Search) (or Footer for the prompt); log popup_reopened outcome=… focus=…
                └─ failed → visible, topmost, unfocused + INotifier balloon(FooterMessage); log focus=failed

  Paste anyway (button, Enter/Space on it) → PasteAnywayCommand
       → footer cleared; IsInserting = true; host.Hide(); log popup_paste_anyway id=PendingId
       → InsertAsync(Target, PendingId, new InsertionOptions(AllowClipboardLoss: true))
       → same outcome branches as above
  Cancel (button) | Esc | query change | MoveSelection while ConfirmClipboardLoss
       → FooterState = None; popup stays open, same query and selection; Focus(Search); log popup_paste_cancelled
  Dismiss (button) while MaybePasted → FooterState = None; Focus(Search); log popup_caution_dismissed
  Copy text (Error except SnippetMissing, MaybePasted) → clipboard.SetText(store text of FailedSnippetId); log popup_copy_text id=…
  Restart as administrator → elevation.RestartAsAdministrator()
                               ├─ Started → requestExit() (AppHost ShutDown, as in Settings)
                               └─ Cancelled|Failed → FooterMessage = result.Message (state stays Error)
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
Ctrl+Enter (overlay UIElement.PreviewKeyDown, Handled before the Text box; fallback: scoped
KeyboardAccelerator) | Save | Ctrl+S → SaveEditorAsync → Editor.SaveAsync()
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

**Store change from elsewhere (main window, usage update) → popup:**
```
Main window edit (popup hidden, because deactivation closed it) | RecordUsage after Inserted
  → ISnippetStore raises Changed → PopupViewModel.OnStoreChanged (subscribed while hidden too)
  → re-sort, rebuild row cache, re-filter current Query; selection kept by id, else first snippet
  → next ShowFor/Open(Fresh) shows current data (labels, folder headings and chips, order)
```

### 4.5 Surface list

| Surface | Change | Task |
|---|---|---|
| `Presentation/Services/InsertionContracts.cs` | new (shared 006/007 contract, verbatim) | T0a |
| `Presentation/Services/IPopupHost.cs`, `Presentation/ViewModels/PopupItemViewModel.cs`, `PopupViewModel.cs` (surface stub), `Presentation/Text/UiText.cs` (popup strings) | new / change | T0b |
| `Presentation/Services/TrayStatePolicy.cs` (`TrayState` moved here + `Compute`), `Presentation/Services/PopupLogFormat.cs` | new | T0b (surface), T1 (`PopupLogFormat`), T11 (`TrayStatePolicy`) |
| `tests/.../Fakes/FakeInsertionService.cs`, `FakePopupHost.cs`, `RecordingNotifier.cs` | new | T0b |
| `tests/.../TrayStatePolicyTests.cs` | new | T11 |
| `Presentation/ViewModels/PopupViewModel.cs` + `PopupViewModel*Tests.cs` | implement | T1–T4 |
| `Core/Settings/AppSettings.cs` (`SaveShell`), `Presentation/ViewModels/SettingsViewModel.cs` (`Persist`) + tests | change (defect fix) | T5 |
| `App/Popup/PopupWindow.xaml(.cs)` | rewrite (replaces 002 prototype) | T6, T7, T8 |
| `App/Controls/SnippetEditor.xaml(.cs)` | add `IsCompact`, `EscapePressed` | T8 |
| `Presentation/Text/TargetDisplayName.cs` + `TargetDisplayNameTests.cs` | new | T9 |
| `App/Services/TargetCapture.cs`, `ForegroundActivation.cs` | new | T9 |
| `App/Services/InterimInsertionService.cs` | new (deleted by 007) | T10 |
| `App/Services/TrayIcon.cs` (`onShowPopup(HWND)`, no `SetForegroundWindow(target)`, `TrayState` enum removed and imported from Presentation) | change | T11 |
| `App/AppHost.cs` (capture, popup and view-model composition, popup `DialogService`, `UpdateTrayState` via `TrayStatePolicy`, `Notices` passed to the view-model, interim insertion) | change | T11 |
| `App/Services/IInserter.cs`, `ClipboardPasteInserter.cs`, `ForegroundTarget.cs` | unchanged (consumed) | — |
| Plan 001 progress row, plan 006 §10 notes | docs | T12 |

## 5. Impact and risk

| Question | Answer |
|---|---|
| State management: what survives a crash mid-operation? | Layout and sort are written immediately on change. A crash loses the popup's query and an unsaved add-snippet draft. The PRD asks for no drafts; same as 005. An interrupted insertion is 007's concern (clipboard restore). |
| Data consistency: orphans, races, double-apply? | Snippets are created only through the store, so no new write path exists. Double Enter or a double click is blocked twice: by `IsInserting` in the view-model and by the contract's single-flight `Busy`. After `MaybePasted` the first Enter/click only dismisses the caution, so an uncertain paste is never repeated by reflex. **Paste anyway** re-uses the prompted snippet ID, never the current selection. The popup never records usage; the service does, only for `Inserted`. The Settings page no longer overwrites popup keys (T5). A folder created inline is kept even if the add is cancelled (005 decision). |
| User experience: root problem or symptom; any surprise? | Root: the prototype inserts a test string. Possible surprises: the query resets on every fresh open (design); Esc in a dirty add form asks first (§9 Q1); the popup stays up when you click away while the editor is open (by requirement). |
| Backward compatibility | Existing `popup.layout`/`popup.sort` values are read as before; invalid values fall back to the defaults and log `settings_invalid`. No schema change. The main-window editor behaves the same (`IsCompact` defaults to false). |
| Error recovery | Insert failures re-open the popup with the message, Copy text and, when relevant, Restart as administrator. `ClipboardNotPreserved` asks before any clipboard loss; `MaybePasted` and `CopiedForManualPaste` explain what happened without inviting a blind retry; a successful insert with a notice goes to the tray. Editor save failures use the existing inline InfoBar with Try again and Copy text. If re-activation is blocked, a tray balloon carries the message (R1). |
| Logging & debugging | §4.3.2 events cover open, close, insert, re-open and add. The `focus=` field on `popup_reopened` diagnoses the foreground lock. No titles or user text (privacy rule, master §4). |
| Edge cases | Covered: empty library (empty state, Add still works); query with no match; 10k snippets; folder layout with only Unfiled; a folder renamed or deleted, or a snippet deleted, while the popup is open (rebuild, keep by ID); hotkey while the editor is open or while inserting; a target closed before Enter; an elevated target; a target on a second monitor with different DPI; text size 32; high contrast; the selected snippet deleted from the main window while the popup shows a failure (Copy text hidden: `SnippetMissing` lookup gives null); the prompted snippet deleted before **Paste anyway** (the service returns `SnippetMissing` → `Error`); deactivation while the clipboard prompt is shown (popup closes, prompt dropped, nothing pasted); the hotkey unregistered while the popup is open (tray red). |

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
  - Checks: `popup_ready_ms` and the end-to-end `popup_search_ms` (both layouts) on runbook step
    21. 003's Core benchmark (< 15 ms filtering) stays a diagnostic only; it does not cover
    binding, layout or render (review finding 4).
  - Measurement accuracy: the `Rendered` stop is the first composed frame after layout, so the
    figure can over-read by up to one frame (~16 ms at 60 Hz). That errs on the safe side of the
    150 ms target.
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
- **R7 — Ctrl+Enter swallowed** by the multi-line Text box (review finding 5). Primary route:
  WinUI 3 `UIElement.PreviewKeyDown` on the overlay root, handled before the Text box. Fallback
  A: a scoped `KeyboardAccelerator` (Ctrl+Enter). Fallback B: Ctrl+S only, with the user's
  sign-off (§4.1). Runbook step 12a decides which one ships; the result goes in §10.
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
  with CR-only breaks until 007. It also never returns `ClipboardNotPreserved` or
  `CopiedForManualPaste`, so those footer states are verified by view-model tests in 006 and on
  screen only by 007's runbook. This is accepted and noted on the runbook.
- **R14 — Duplicate paste after an uncertain dispatch.** Mitigated by the `MaybePasted` footer
  state (caution wording, first Enter/click only dismisses, no retry button) and by the interim
  adapter mapping `SendInputFailed` to `MaybePasted`. Tests in T4.
- **R15 — Clipboard prompt answered for the wrong snippet.** **Paste anyway** uses `PendingId`;
  any query change or selection move cancels the prompt first. Tests in T4.

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
- **Files:** `src/SmartSnippets.Presentation/Services/InsertionContracts.cs` (the one shared
  file; 007's T0 is the same step, done once).
- **Change:** §4.3.1, verbatim (outcomes incl. `ClipboardNotPreserved`, `MaybePasted`,
  `CopiedForManualPaste`; `InsertionOptions(AllowClipboardLoss)`).
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
  - `Presentation/Services/TrayStatePolicy.cs` (`TrayState` + `Compute` stub) and
    `Presentation/Services/PopupLogFormat.cs` (stub).
  - `tests/.../Fakes/FakeInsertionService.cs`: scripted results, a recorded call log **including
    the `InsertionOptions` of each call**, an optional `TaskCompletionSource` to hold a call open,
    and a `ThrowNext` switch.
  - `tests/.../Fakes/FakePopupHost.cs`: records `Hide`, `ShowAgain`, `OpenMainWindow` and
    `Focus` in order.
  - `tests/.../Fakes/RecordingNotifier.cs`: records `Notify(title, message)` calls.
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
  - Rebuild on `store.Changed`, keeping the selection by ID, whether the popup is shown or
    hidden (§4.1 store-change propagation).
  - `PopupLogFormat.SearchMs`.
- **Verify:** `dotnet test tests/SmartSnippets.Presentation.Tests --filter "PopupViewModelList|PopupLogFormat"`.
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
  - **Store-change propagation (review finding 8).** The fake store raises `Changed` while the
    popup is shown (after `Open`, before `Close`):
    - `WhileShown_FolderRenamed_HeadingAndChipsUpdate_SelectionKept`.
    - `WhileShown_SelectedSnippetEdited_RowUpdates_SelectionKept`.
    - `WhileShown_SelectedSnippetDeleted_SelectsFirstSnippet`.
    - `WhileShown_SnippetAdded_AppearsInSortOrder_SelectionKept`.
    - `WhileShown_FolderDeleted_RowsShowUnfiled`.
    - `WhileShown_QueryKept_ResultRefiltered` (an edit that makes the selected row stop matching
      moves the selection to the first match).
    - And while hidden: `WhileHidden_StoreChanged_NextOpenShowsCurrentData` (change after
      `Close`, then `Open` again: label, heading and order are the new ones).
  - `SearchMs_Format_LayoutAndResults_NoQueryText` (`PopupLogFormat`): `"42 layout=folder
    results=7"`; the query is not a parameter, so it can't leak.
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

### T4 — Insertion, footer states, re-open (Track A) (AC5; AC9/AC10 hand-off)
- **Files:** `PopupViewModel.cs`, `tests/.../PopupViewModelInsertionTests.cs`.
- **Change:** `InsertSelectedAsync`, `ActivateAsync`, the outcome table and footer states from
  §4.1 and §4.4, Copy text, Restart as administrator, Paste anyway / Cancel, Dismiss, and the
  `INotifier` notice for `Inserted` with a message.
- **Verify:** `dotnet test ... --filter PopupViewModelInsertion`.
- **Tests that die if a hop breaks:**
  - `Enter_HidesBeforeInsert_PassesTargetAndId`: checks the host call order against the fake
    call log.
  - `Click_EqualsEnter`, `Enter_NoSelection_NoCall`, `DoubleEnter_SingleCall`.
  - `Enter_PassesDefaultOptions` (`AllowClipboardLoss == false`).
  - `Success_StaysHidden_NoFooterMessage`.
  - `Inserted_EmptyMessage_NoNotice`, `Inserted_WithMessage_StaysHidden_NotifiesOnce`
    (`RecordingNotifier` gets `InsertNoticeTitle` + the message; no `ShowAgain`).
  - `Failure_ShowsAgain_SameTargetQuerySelection_ShowsMessage` (state `Error`).
  - Clipboard prompt (`ClipboardNotPreserved`):
    - `ClipboardNotPreserved_ShowsAgain_PromptState_FocusFooter_SameSelection`.
    - `PasteAnyway_HidesThenInserts_SameIdAndTarget_AllowClipboardLossTrue`.
    - `PasteAnyway_UsesPendingId_NotCurrentSelection`.
    - `PasteAnyway_ThenInserted_StaysHidden`, `PasteAnyway_ThenMaybePasted_ShowsCaution`.
    - `PromptCancel_ClearsPrompt_StaysOpen_NoCall`, `PromptEsc_EqualsCancel_DoesNotHide`.
    - `Prompt_QueryChangeOrMove_CancelsPrompt`, `Prompt_Enter_DoesNotInsert`.
    - `Prompt_HidesCopyText`.
  - Manual paste (`CopiedForManualPaste`):
    - `CopiedForManualPaste_ShowsAgain_InfoState_NoCopyText_NoRestart`.
    - `ManualPaste_NextEnter_InsertsNormally`.
  - Uncertain dispatch (`MaybePasted`):
    - `MaybePasted_ShowsAgain_CautionState_CopyTextShown_NoRestart`.
    - `MaybePasted_FirstEnter_OnlyDismisses_NoCall`, `MaybePasted_SecondEnter_Inserts`.
    - `MaybePasted_FirstClick_OnlyDismisses_NoCall`.
    - `MaybePasted_DismissButton_Clears_NextEnterInserts`.
    - `MaybePasted_Esc_Closes`.
  - `Failure_CopyText_CopiesStoreText`, `SnippetMissing_HidesCopyText`.
  - `TargetElevated_ShowsRestart_WhenNotElevated`, `TargetElevated_NoRestart_WhenElevated`.
  - `Restart_Started_RequestsExit`, `Restart_Cancelled_ShowsMessage`.
  - `Busy_NoReopen`, `InsertThrows_TreatedAsFailure_Reopens`.
  - `NextFreshOpen_ClearsFooterState` (each non-`None` state).
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
  - **Footer:** hint or status, count, and the message area for every `PopupFooterState`
    (danger, caution or info style from the existing tokens; high-contrast safe) with Copy text,
    Restart, Paste anyway / Cancel and Dismiss, each shown by its view-model flag through an
    instance `Visibility` helper.
  - **Search timing:** the §4.1 `TextChanged` → `Items` → `LayoutUpdated` →
    `CompositionTarget.Rendered` measurement, logging `popup_search_ms` via
    `PopupLogFormat.SearchMs`. `Rendered` is subscribed only while a measurement is pending.
  - **Empty state.**
  - **Keys on the root `KeyDown`:**
    - Up/Down → `MoveSelection`.
    - Enter → `InsertSelectedAsync`.
    - Esc → `Close` (which cancels the clipboard prompt first, if shown). The flyout consumes its
      own Esc, and the handler does nothing while the editor is open.
  - **Tab order:** search → 4 buttons → list (`TabNavigation=Once`) → footer buttons.
  - `IPopupHost.Focus(Footer)` focuses **Paste anyway**.
  - Automation IDs from §4.3.2.
- **Verify:** CI compile. Runbook steps 5–11, 19, 20, 21 (`popup_search_ms` lines in both
  layouts).

### T8 — Add-snippet overlay and SnippetEditor compact mode (Track B) (AC8)
- **Files:** `PopupWindow.xaml(.cs)`, `src/SmartSnippets.App/Controls/SnippetEditor.xaml(.cs)`.
- **Change:**
  - **Overlay:** scrim, title, target line, `SnippetEditor` (`IsCompact=true`,
    `ViewModel=ViewModel.Editor`, `TextSize`), Cancel/Save.
  - **Ctrl+Enter:** the WinUI 3 `UIElement.PreviewKeyDown` handler on the overlay root (§4.1:
    Enter + Ctrl only, `e.Handled = true`, `SaveEditorAsync`). If runbook step 12a fails, switch
    to fallback A (scoped `KeyboardAccelerator`), then fallback B (Ctrl+S with the user's
    sign-off), and record which one shipped in §10.
  - Base content `IsEnabled="{x:Bind ViewModel.IsEditorClosed, Mode=OneWay}"`.
  - **`SnippetEditor`:**
    - `IsCompact` (plain property, calls `Bindings.Update()`).
    - Wrappers hiding the stats card, Delete and the unsaved bar.
    - `EscapePressed` raised from `OnDiscardAccelerator` when compact (`:90-108`); the new-folder
      Esc behaviour is unchanged.
- **Verify:** CI compile. Runbook steps 12, 12a, 13–15. Snippets-page regression (009 runbook 3,
  5, 6) unchanged.

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
- **Change:** §4.1 interim insertion, against the §4.3.1 signature (with `InsertionOptions`).
  Plain success returns an empty `Message`; `SendInputFailed` maps to `MaybePasted` without
  usage. A header comment states "Deleted by plan 007."
- **Verify:** CI compile. Runbook steps 6, 16, 17. `insert_result` and `popup_insert` appear in
  the log, usage increments after a success (Last used sort, main-window row meta), and no tray
  notice appears after a plain success.

### T11 — AppHost and tray wiring (Track C) (AC4, AC7; tray state from §2)
- **Files:** `src/SmartSnippets.App/AppHost.cs`, `src/SmartSnippets.App/Services/TrayIcon.cs`,
  `src/SmartSnippets.Presentation/Services/TrayStatePolicy.cs` (body) and
  `tests/.../TrayStatePolicyTests.cs`. The policy and its tests are pure Presentation code; Track
  C owns them so that the one writer and its rule change together.
- **Change:**
  - **Compose:** `popup = new PopupWindow(textSize, openMain)` → popup-scoped `DialogService` →
    `InterimInsertionService` → `new PopupViewModel(..., notices: Notices, ...)` →
    `popup.ViewModel = vm` → `Prewarm` → `theme.Register(popup)`. `Notices` must be built before
    the view-model (today it is built after the tray at `:201`, so the popup composition moves
    after it).
  - **Hotkey:** `OnHotkeyPressed` → `TargetCapture.CaptureForeground()` → `ShowFor`.
  - **Tray:** `onShowPopup: hwnd => Post(() => ShowPopupFromTray(hwnd))`. Remove the tray's
    `SetForegroundWindow(target)` (`TrayIcon.cs:253-257`) and update the comment at `:280-284`.
  - **Tray state:** `UpdateTrayState()` = `tray.State = TrayStatePolicy.Compute(hotkeyManager
    .Status.IsRegistered, popupVisible)`. It replaces the writers at `:197-199`, and both
    `popup.VisibleChanged` and `StatusChanged` trigger it. `TrayIcon.cs` drops its own `TrayState`
    enum and imports the Presentation one.
  - **Restart:** `requestExit` reuses `RestartAsAdministrator`'s `ShutDown` path.
  - **Balloon:** the `ShowAgain` balloon fallback goes through `Notices`.
  - **Exit:** `_popupViewModel.Dispose()` in `ReleaseResources`.
- **Verify:** `dotnet test ... --filter TrayStatePolicy`; CI compile. Runbook steps 1, 2, 9,
  16–18, 22, 24.
- **Tests that die if this breaks:**
  - `TrayStatePolicyTests`: `Unregistered_Wins_OverPopupOpen`, `Unregistered_PopupHidden_IsUnavailable`,
    `PopupOpen_WhenRegistered`, `Ready_WhenRegisteredAndHidden` (including the two names 008 lists, so 008
    T0 adopts them unchanged).
  - `Ctor_ReadsLayoutAndSortFromSettings` (the view-model link). App wiring itself is verified by
    T12's greps and log lines.

### T12 — Wiring audit and docs (coordinator) (L)
- **Trace each new entity:**
  - `InsertionTarget`: created only in `TargetCapture`; consumed by the view-model and the
    insertion service.
  - `IInsertionService`: one implementation (interim), constructed in `AppHost`.
  - `IPopupHost`: implemented by `PopupWindow`, passed to the view-model.
  - `PopupViewModel`: constructed once, disposed on exit.
  - `store.Changed` subscribers: list, editor, folders, popup.
  - `VisibleChanged` → `UpdateTrayState` → `TrayStatePolicy.Compute`.
  - `INotifier` (`Notices`) → `PopupViewModel` (notice for `Inserted` with a message) and the
    `ShowAgain` balloon fallback.
  - `InsertionOptions(AllowClipboardLoss: true)`: constructed only in `PasteAnywayAsync`.
  - `SnippetEditor.IsCompact`: set only by the popup.
  - `EscapePressed`: subscribed only by the popup.
  - `SaveShell`: called by `SettingsViewModel`.
- **Greps:**
  - `ShowFor` and `ForegroundTarget.Capture` call sites (§4.4).
  - No `tray.State =` outside `UpdateTrayState`, and no `enum TrayState` left in App.
  - `AllowClipboardLoss: true` appears only in `PopupViewModel.PasteAnywayAsync`.
  - No `PreviewKeyDown` handler besides the overlay's Ctrl+Enter one (or the shipped fallback).
  - No `AppLog` argument containing `DisplayName|Title|Query|Label|Text`.
  - No `IReadOnlyList` property bound as `ItemsSource` in `PopupWindow.xaml`.
  - Every `x:Bind` path in `PopupWindow.xaml` exists on `PopupViewModel`, `PopupItemViewModel` or
    an instance helper.
- **Docs:** update master 001's progress row and this plan's §10 (implementation notes,
  deviations).
- **Verify:** on the runbook run, the log contains `popup_ready_ms`, `popup_search_ms` (both
  `layout=` values), `popup_opened`,
  `popup_layout`, `popup_sort`, `popup_insert`, `popup_reopened`, `popup_add_saved`,
  `popup_add_cancelled`, `popup_copy_text` and `popup_closed`.

### AC ↔ task map

| Master AC | Tasks | Evidence |
|---|---|---|
| AC3 (layout/sort persist) | T1, T5 | `ToggleLayout_PersistsKey…`, `ThemeChange_AfterPopupLayoutChange_KeepsPopupLayout`, `LayoutAndSort_SurviveAppSettingsReload`; runbook 10 |
| AC4 (open, centre, clamp, focus) | T2, T6, T9, T11 | `Open_Fresh_…`, `TargetDisplayName*`; runbook 1–4, 22 |
| AC5 (keyboard and mouse) | T2, T4, T7 | `Move*`, `Click_EqualsEnter`, `DoubleEnter_SingleCall`, `MaybePasted_FirstEnter_OnlyDismisses_NoCall`, `PromptEsc_EqualsCancel_DoesNotHide`; runbook 5–8, 19 |
| AC6 (search UI) | T1, T7 | `Search_*`, `Empty_*`, `WhileShown_*`, `WhileHidden_StoreChanged_NextOpenShowsCurrentData`; runbook 5, 7, 23 |
| AC7 (layouts, sorts, toolbar) | T1, T7, T11 | `FolderLayout_*`, `Sort_*`, tooltips, `WhileShown_FolderRenamed_*`; runbook 8–11, 23 |
| AC8 (add snippet) | T3, T8, T9 | `AddSnippet_*`, `Save_*`, `Cancel_*`, integration; runbook 12, 12a (Ctrl+Enter), 13–15 |
| AC13 (popup ≤ 300 ms; search ≤ 150 ms at 10k, indicative here, accepted in 008) | T1, T6, T7 | `popup_ready_ms`; end-to-end `popup_search_ms` in both layouts, `SearchMs_Format_*`; runbook 21 |
| AC9/AC10 hand-off | T0a, T4, T10 | contract, `Failure_*`, `TargetElevated_*`, `ClipboardNotPreserved_*`, `PasteAnyway_*`, `CopiedForManualPaste_*`, `MaybePasted_*`, `Inserted_WithMessage_*`; runbook 16–18 (full acceptance in 007) |
| Tray state (§2; red wins) | T11 | `TrayStatePolicyTests`; runbook 1, 22, 24 |

| Task | ACs |
|---|---|
| T0a | AC9/AC10 hand-off |
| T0b | AC4–AC8 (contracts) |
| T1 | AC3, AC6, AC7, AC13 (log format) |
| T2 | AC4, AC5 |
| T3 | AC8 |
| T4 | AC5, AC9/AC10 hand-off |
| T5 | AC3 |
| T6 | AC4, AC13 |
| T7 | AC5, AC6, AC7, AC13 (search timing) |
| T8 | AC8 |
| T9 | AC4, AC8 (target line) |
| T10 | AC9 hand-off (interim) |
| T11 | AC4, AC7, tray state |
| T12 | all of the above (audit) |

## 7. Test strategy

**Unit tests (Linux and Windows CI).**
- About 110 new Presentation tests:
  - `PopupViewModelListTests` (including the `WhileShown_*` / `WhileHidden_*` store-change
    propagation tests), `…NavigationTests`, `…EditorTests` and `…InsertionTests` (including
    every footer state: `Error`, `ConfirmClipboardLoss`, `ManualPaste`, `MaybePasted`, and the
    `Inserted` notice).
  - `TargetDisplayNameTests`, `TrayStatePolicyTests`, `PopupLogFormatTests`.
  - The `SettingsViewModel` regression.
  - The `AccessibleNameTests` addition.
- One new Core test (`SaveShell`).
- **Fakes:** `FakeSnippetStore` (real rules), `MemorySettingsStore`, `ScriptedDialogService` (as
  the popup-scoped dialogs), `FakeClipboardService`, `FakeInsertionService` (records options),
  `FakePopupHost`, `RecordingNotifier`, and a fake `IElevation` (`IsElevated`, scripted
  `RestartAsAdministrator`).
- **Store-change propagation (review finding 8)** is tested at the view-model level: the fake
  store raises `Changed` while the popup view-model is shown (folder renamed, selected snippet
  edited or deleted, snippet added, folder deleted) and while it is hidden (then re-opened).
  Keeping the popup open while editing in the main window is not possible by design
  (deactivation closes it), so **008's E11–E12 must use this approach**: these view-model tests
  plus runbook step 23 (change in the main window while the popup is hidden, then re-open), not
  "edit in the main window with the popup open".
- **Deliberately not unit-tested:** XAML layout, Win32 capture, placement and foreground
  activation, the Ctrl+Enter key routing, the search-timing hooks, and the interim adapter. These
  are App-only and covered by CI compile plus the runbook (12a, 21).

**Integration (`PopupViewModelIntegrationTests`).** The real `SnippetStore` on a temp SQLite
database, with `FakeInsertionService` and `FakePopupHost`:
- Add snippet → saved row reloaded from the DB → selected → Enter → the insertion service gets
  the saved ID and the original target.
- Layout and sort written → `AppSettings.Load` on a fresh `SettingsRepository` returns them.

**Automated Windows verification.** No change in 006: the existing
`scripts/verify-plans-003-009.ps1` sections C3, C4 and C9 must still pass, because the log
format, Esc behaviour and the "Smart Snippets" main title are all kept. **Adding popup sections
to the script belongs to 008** (R12). They will use the window title and the automation IDs from
§4.3.2, and read `popup_search_ms <int> layout=<flat|folder> results=<n>` for the ≤ 150 ms at
10k acceptance (the Core benchmark stays a diagnostic).

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
| 12a | **Ctrl+Enter routing (R7).** In the overlay's Text box type `line one`, Enter, `line two`, Shift+Enter, `line three`; put the caret in the middle of `line two`; press Ctrl+Enter | Enter and Shift+Enter made newlines; Ctrl+Enter saved at once and added **no** newline (open the snippet in the main window: exactly three lines). **If it fails:** switch to fallback A (scoped `KeyboardAccelerator`, §4.1), rebuild and repeat; if that also fails, fallback B (Ctrl+S, user sign-off). Record the shipped route in §10 |
| 13 | With the query `ref`, add text "Thanks!" with tag "chat", use the folder **New** → "Quick" (open the Folder dropdown too), press Ctrl+Enter | The dropdown did not close the popup; the snippet is saved, the query is cleared (it would have hidden it), the new row is selected, and the footer says "Snippet added and ready to insert"; Enter inserts "Thanks!" into Notepad |
| 14 | Add snippet, type something, click into another app, come back with Alt+Tab or the hotkey | The popup and editor are still there with the same target |
| 15 | Add snippet, type, press Esc → Keep editing; Esc → Discard. Then Add, and Cancel at once | Discard returns to the earlier query and selection; Cancel closes without asking |
| 16 | Select a snippet, close Notepad from the taskbar's jump list without focusing it (or via Task Manager), then press Enter in the popup | The popup re-opens with the same selection and the message in a red footer; Copy text puts the snippet text on the clipboard |
| 17 | Run Notepad as administrator, press the hotkey and Enter | The footer shows the elevated message with **Restart as administrator**; clicking it shows UAC (Cancel → message in the footer) |
| 18 | Check the log after 16–17 | `popup_reopened outcome=… focus=direct` or `attach`; if `failed`, a tray balloon showed the message |
| 19 | Keyboard only, with Narrator on: Tab and Shift+Tab around the popup | Search → Layout → Sort → Add → Open main → list → footer buttons; each is announced with its name and tooltip; moving the selection is announced; headings are announced as "Folder, n snippets" and can't be selected |
| 20 | Settings → text size Custom 32, open the popup and Add snippet | Rows are larger and nothing is clipped; the editor scrolls and Cancel/Save stay visible; check light, dark and high contrast |
| 21 | Seed 10,000 snippets with the app closed: `sqlite3 "%LOCALAPPDATA%\SmartSnippets\smartsnippets.db" "WITH RECURSIVE n(i) AS (SELECT 1 UNION ALL SELECT i+1 FROM n WHERE i<10000) INSERT INTO snippets(text,label,tags,created_at,updated_at) SELECT 'Body '||i,'Snippet '||i,'',1761600000000+i,1761600000000+i FROM n;"`, start the app, press the hotkey 5 times. Then, in the flat layout, paste `Snippet 99` into the search box, wait, clear it; toggle to the folder layout and repeat | Every `popup_ready_ms` ≤ 300; typing stays smooth; folder layout toggles without a visible stall. The log has one `popup_search_ms <int> layout=flat results=<n>` and one `layout=folder` line per paste or clear, with no query text; record the values (indicative here, ≤ 150 ms accepted in 008). Restore your profile afterwards |
| 22 | Open main window button | The main window opens on Snippets and is in front; the popup is gone; the tray state is back to Ready |
| 23 | **Store-change propagation (review finding 8).** With the popup closed, in the main window: rename a folder, edit the label of a snippet, delete another snippet, add a new one. Press the hotkey; check flat and folder layouts | The popup shows the renamed heading and chips, the new label, no deleted row, and the new snippet in its sort position. (The popup cannot stay open while you edit in the main window, because clicking away closes it; this is by design.) |
| 24 | **Tray precedence.** Start a helper that holds Ctrl+Shift+Space (or start a second hotkey owner), start Smart Snippets so the hotkey is unavailable, then open the popup from the tray's **Show popup** | The tray tooltip/state stays **HotkeyUnavailable** while the popup is open (red wins) and is still HotkeyUnavailable after the popup closes |

Known interim limitation (R13): until 007, multi-line snippets typed in the editor may paste with
CR-only line breaks, and the failure messages use the 002 wording. The interim adapter never
returns `ClipboardNotPreserved` or `CopiedForManualPaste`, and `MaybePasted` only on a partial
`SendInput`, which can't be provoked by hand. Those footer states are covered by T4's view-model
tests here and checked on screen by 007's runbook once the production engine is wired.

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
  `TrayState.PopupOpen`. It adopts 006's `TrayStatePolicy` (same rule: red wins), reads
  `popup_search_ms` for the search acceptance, and runs E11–E12 as view-model tests plus a
  hidden-then-reopen check (§7), not with the popup open during a main-window edit.

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
8. **`ClipboardNotPreserved` → Cancel keeps the popup open** (prompt cleared, same query and
   selection); a second Esc closes it. **Paste anyway** has initial focus, so Enter answers the
   prompt explicitly; Enter in the search box does not.
9. **`MaybePasted`: the first Enter or row click only dismisses the caution**; a second one
   inserts. A **Dismiss** button does the same for the mouse. Esc closes the popup as usual.
10. **`CopiedForManualPaste` shows no Copy text** (the snippet is already on the clipboard) and no
    retry button; Enter afterwards inserts normally (nothing was pasted).
11. **The interim adapter maps `SendInputFailed` to `MaybePasted`** rather than `PasteFailed`,
    because a partial `SendInput` may already have pasted.

## 10. Implementation notes and wiring audit

**Implementation status:** implemented, automated checks pass; manual acceptance in §7 remains
pending. The shared insertion contract was not changed and the interim adapter remains until 007.
The shipped Ctrl+Enter route is `PreviewKeyDown`; runbook 12a must still validate it.

**T12 wiring audit:**

| Hop / invariant | Evidence |
|---|---|
| Capture → target → popup | Both AppHost entry points call `TargetCapture`; only `TargetCapture` creates `InsertionTarget`; foreground capture alone calls `ForegroundTarget.Capture`. Tray passes the discovered HWND directly. |
| Popup → insertion | AppHost constructs one `InterimInsertionService` and one `PopupViewModel`; the window implements `IPopupHost` and the view-model is disposed on exit. `AllowClipboardLoss: true` appears only in Paste anyway. |
| Changes and persistence | The popup subscribes to `store.Changed`, rebuilds its snapshot, and unsubscribes on disposal. Layout and sort write their keys; the Settings page calls `SaveShell`. |
| Tray and notices | `VisibleChanged` and hotkey status call `UpdateTrayState`, the only `_tray.State` writer; it uses `TrayStatePolicy`. `Notices` reaches the view-model and reopen-failure callback. |
| Editor | `IsCompact=True` and `EscapePressed` are used only by the popup; its sole `PreviewKeyDown` handler routes Ctrl+Enter. Its dialog service uses the popup XamlRoot. |
| Bindings | Every popup and row `x:Bind` resolves against the view-model, row item, text-size service or window/control helper; WinUI compilation succeeds. `ItemsSource` is a concrete `List<PopupItemViewModel>`. |
| Privacy | Popup logs carry IDs, outcomes, modes, layout and counts; no query, title, display name, label or snippet text is passed to a popup log. |
| Timing | Search changes start timing before setting the query; changed items wait for list layout and `CompositionTarget.Rendered`, while identical results wait for the next rendered frame. New edits supersede earlier ones. |

Implementation details differ from the draft in two places: the text-change handler explicitly
updates the view-model instead of a two-way binding, so timing starts before filtering; and the
row is a small typed `PopupRow` control with instance visibility helpers instead of template
converters. Footer styling uses WinUI `InfoBar` severity. Several planned individual test cases
are combined in theories or multi-assertion tests. Manual accessibility, mixed-DPI, Ctrl+Enter,
performance and activation checks are deliberately left to the user's runbook.

### Stability amendment — folder rebind, first frame and cached popup (2026-10-28)

User decisions: (A) prevent silent folder loss when switching editor sessions, including all
transient binding feedback, (B) stabilize white/black overlay flashing, (C) prewarm/cache the
popup at startup and refresh its cache after changes. This supersedes the synchronous
per-Changed cache pipeline and row-preview allocation described above (compact-row amendment
`f85d4a1` remains authoritative for the UI).

**Confirmed in static code:** editor assigned the new VM before updating a TwoWay
SelectedItem with a changing ItemsSource; null was historically translated to Unfiled. The
unsafe write path is removed: OneWay selection plus explicit user-selection method, rebind
and VM loading/choice-refresh guards, real non-null Unfiled, selection restored through the
current choice collection by ID. Other editor inputs use OneWay plus focused-input handlers;
Settings rebinding is guarded. Folders row text edits have stable individual row VMs and only
explicit commands persist them. Popup query is already explicit; selection is ephemeral and
sort/layout actions are explicit. **Candidate UI causality:** actual WinUI event order producing
the reported folder loss still requires screen confirmation; view-model tests simulate it and
assert the saved store row, including save without edits and deliberate Unfiled.

**Confirmed initialization defects:** original theme registration followed popup prewarm;
prewarm showed then immediately hid without waiting for layout. Solid themed root background
already existed. No popup Mica/acrylic backdrop, per-show presenter setup, or per-show theme
switch was found. Main window constructor navigates but does not show; registration precedes
show. DialogService copies the owner's requested theme before ShowAsync. Preview uses the
same themed window. **Candidate flash cause:** unthemed/unfinished first surface; not visually
proven. Prewarm now cloaks the native surface, shows offscreen without activation/switcher
entry, waits for first valid content layout, hides then uncloaks. Show requests arriving during
prewarm are deferred. ThemeService applies DWM immersive-dark state before show and updates
hidden registered windows as well as visible ones. The solid ThemeResource root remains the
background source (including high contrast); there is no material transition. Presenter and
theme are not reapplied on every show. First-frame colour must still be checked on screen.

**Cache:** compact projection only (label/folder/tags; no text preview copy), plus a search
index referencing the store's immutable snapshots so full-text search is preserved. Startup
builds the cache. A store event only marks dirty and queues one UI-dispatcher rebuild; events
before that callback coalesce. No store access on a background thread. Hidden and visible
result lists refresh while preserving query and selected ID. Open/OpenPreview/ShowAgain flush
dirty work before showing, and editor-save/query/layout paths also enforce freshness. A
queued callback following a show-barrier rebuild is a no-op. Rebuild replaces the dictionary,
not appends. Dispose unsubscribes, clears rows/search/cache, and queued work checks disposal.
Store D9 subscriber isolation was already implemented; existing Core tests and the new popup
fake-store test cover failure of an earlier subscriber.

| Invalidation source | Projection/search | Result/window action |
|---|---|---|
| Snippet create/edit/delete | Dirty; one queued rebuild | Refilter current query; preserve selected ID or first row |
| Folder create/rename/delete | Same (folder labels/search/grouping may change) | Same; deleted folder becomes Unfiled |
| Import (one Changed per batch), reload | Same; usage events in same turn coalesce | Same; 50k-import test exercises show barrier |
| Usage recorded | Same (sort keys change) | Resort, preserve selected ID |
| Popup sort preference | Immediate sorted search rebuild (also clears dirty) | Persist own key, first selection |
| Popup layout preference | Flush dirty then rebuild row projection | Persist own key, regroup, first selection |
| Query/filter | Flush dirty, filter current index | No persisted content writes; first selection |
| Text size preference | No row-data copy to invalidate | Shared observable ITextSizeService updates existing rows/headings |
| Theme preference / system actual theme | No row-data copy to invalidate | Apply registered window/root and DWM state even while hidden |

Closed/lost popup windows are recreated lazily, themed and prewarmed before showing, using a
stable forwarding endpoint beneath unchanged DeferredPopupHost. TrayStatePolicy and
InsertionShutdownCoordinator are unchanged. Popup/editor view subscriptions detach on close.
New logs are numeric-only `popup_cache_rebuild_ms=NN rows=N` and `popup_show_ms=NN`; the latter
measures hotkey-handler timestamp through successful AppWindow.Show (not compositor latency).
Existing `popup_ready_ms` and search-frame logs remain. The 50k dirty-open test records measured
VM rebuild/filter time; real first-frame timing and <=300 ms acceptance remain screen work.

### Static-review fix round (stab-02 validated)

NumberBox edits use focus-within (including template descendants) plus a short edit-session
latch/final-value read across focus-loss commit; finite/rebinding/session guards remain.
`PopupShowCoordinator` now owns readiness/show/focus ordering, shared by the real WinUI window
and pure tests. Pending recovery Footer focus survives asynchronous replacement warmup and
DeferredPopupHost suspend/resume; default Search never overwrites it. Queued focus is versioned
and cancelled by hide, close or any newer show (including nonactivating preview).
Hide during warmup explicitly detaches layout, stops the timer, hides/uncloaks and returns to
cold; the next show restarts initialization. A generation-tagged two-second timeout retries
once, then logs `popup_prewarm_failed retry=next_show` and stays hidden/restartable. Timeout
callbacks capture their originating generation, so stale timeouts cannot affect another
attempt. WinUI layout readiness instead validates current warming/visibility/XamlRoot/width
and detaches the handler on cancellation; it does not capture a per-attempt layout generation.
Shared-policy tests reject explicitly supplied old completion generations, not prove native
layout-event attribution. Real focus, layout and timer behaviour remain pending an authorized
control-level pass; tests exercise the shared policy,
not WinUI events. DeferredPopupHost, tray policy and shutdown coordinator remain unchanged.
Granted C: snapshot validation of `ef70486`: final build 0 warnings/errors, Core 426 passed,
Presentation 480 passed (including 13 additional fix-round cases), single Native AOT gate
passed with 0 warnings. The initial ambiguous dispatcher-timer type and cascading XAML
warning/error were corrected before the successful build. No app was launched. Current 50k
VM dirty-open diagnostic: 69.16 ms; not desktop acceptance or memory-retention evidence.

## 11. Review response

Review: [docs/review/006-008-implementation-readiness.md](../review/006-008-implementation-readiness.md).
User decisions recorded 2026-10-28. Findings 3 and 6 are owned by 007 and 008 and need no 006
change; the engine side of findings 1–2 is 007's.

| Finding | Decision | Change in this plan |
|---|---|---|
| 1 — Clipboard restoration can silently lose data (UI side) | The engine aborts before touching the clipboard when it can't preserve it (`ClipboardNotPreserved`); the user may then accept the loss. An `Inserted` that could not fully restore the clipboard carries a notice. | Contract §4.3.1 replaced (new outcomes, `InsertionOptions(AllowClipboardLoss)`). Footer state `ConfirmClipboardLoss` with "Paste anyway?" / Cancel, Esc = Cancel; **Paste anyway** re-inserts the pending snippet with `AllowClipboardLoss: true` (§4.1, §4.4). `CopiedForManualPaste` → info footer, no Copy text, no retry. `Inserted` with a message → popup stays hidden, tray notice via `INotifier`. New `UiText` strings, automation IDs, logs; tests in T4; R15. |
| 2 — A failed dispatch can already have pasted (UI side) | Uncertain dispatch is its own outcome, `MaybePasted`, with no usage and no retry offer. | Footer state `MaybePasted` (caution, Copy text, Dismiss); the first Enter/click only dismisses (§4.1, §4.2, §9 Q9). Interim adapter maps `SendInputFailed` → `MaybePasted`. Tests in T4; R14. |
| 4 — Search performance measures the wrong path | End-to-end timing in the popup; the Core benchmark stays a diagnostic. | §4.1 "End-to-end search timing": `TextChanged` → `Items` changed → `PopupList.LayoutUpdated` → first `CompositionTarget.Rendered`; logs `popup_search_ms <int> layout=<flat|folder> results=<n>` (no query). `PopupLogFormat` + test; T7; runbook 21; 008 uses it for ≤ 150 ms at 10k. `popup_ready_ms` kept. |
| 5 — Ctrl+Enter needs a WinUI-specific solution | Use a specified WinUI 3 route with a tested fallback. | §4.1 / R7 / T8: `Microsoft.UI.Xaml.UIElement.PreviewKeyDown` (exists in Windows App SDK) on the overlay root, handled before the `AcceptsReturn` Text box; fallback A scoped `KeyboardAccelerator` (Ctrl+Enter); fallback B Ctrl+S with sign-off. `handledEventsToo` rejected (runs after the newline). Runbook 12a with "if it fails" steps. |
| 7 — Contract file and tray precedence disagree | One file `src/SmartSnippets.Presentation/Services/InsertionContracts.cs`, written by the coordinator in T0. **`HotkeyUnavailable` wins over `PopupOpen`.** | §4.3.1 names the one file and owner. `TrayStatePolicy.Compute` (Presentation, unit-tested) used by the single writer `AppHost.UpdateTrayState`; `TrayState` moves to Presentation; 008 T0 adopts it. T11 tests, runbook 24. |
| 8 — Integration scenarios conflict with deactivation | Keep "deactivation closes the popup". | Store-change propagation tested by view-model tests that raise `Changed` while shown and while hidden (T1), plus runbook 23 (edit in the main window while the popup is hidden, re-open). §7 states that 008's E11–E12 must use this approach. |

## 12. Approval log

| Date | Event | Notes |
|---|---|---|
| 2026-10-27 | Placeholder reserved | master plan 001 |
| 2026-10-28 | Requirement brief confirmed (G1) | 006–008 brief |
| 2026-10-28 | Draft written | Awaiting approval (G2), including the §9 design calls |
| 2026-10-28 | Review (docs/review/006-008-implementation-readiness.md) | Findings 4, 5, 7, 8 + UI for 1–2 addressed; user decisions recorded |
| 2026-10-28 | User approved compact-row/context-menu amendment | One label row, right-aligned folder/tags, Copy / Insert / Edit / Delete; preview has no insertion, clipboard write or usage recording, and Copy is disabled. Pending UI verification. |
| 2026-10-28 | User requested stability decisions A/B/C | Eliminate editor rebind write hazards; stabilize overlay first frame; startup-prewarm cached popup with coalesced fresh data. Static confirmed defects vs candidate visual causes and invalidation matrix recorded in §10 stability amendment. Granted C: snapshot build: 0 warnings/errors; Core 426/426, Presentation 467/467; single AOT gate passed, 0 warnings. Dirty-open projection/filter at 50k rows: 67.48 ms (not render time). Screen acceptance remains PENDING-USER. |
