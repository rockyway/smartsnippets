# 005 — Main window: snippet and folder management

**Date:** 2026-10-27
**Status:** Complete — CI green (run #28, `6e75872`); manual runbook (§7) pending on user PC
**Size:** L (subplan of [001-master-smart-snippets-v1.md](001-master-smart-snippets-v1.md))
**Area:** `src/SmartSnippets.Presentation/`, `src/SmartSnippets.App/Pages/`, `src/SmartSnippets.App/Controls/`, `tests/SmartSnippets.Presentation.Tests/`
**Depends on:** 003 (planned store/rules/query API), 004 (planned shell: `MainWindow`, `IGuardedPage`, `IDialogService`, `ITextSizeService`, Presentation project)
**Requirement brief confirmed:** 2026-10-27 (G1, master §2)

> Detailed in the same pass as 003 and 004 at the user's request (2026-10-27). It relies on
> **planned** APIs: 003 §4.5 (`ISnippetStore`, `StoreResult`, `LabelSuggester`, `TagList`,
> `SnippetSearch`) and 004 §4.1 (shell services). Before starting 005, check those names against
> the merged 003 and 004 code and update this plan if anything changed.

> **API check (2026-10-27, after 003 merged at `3435c2a`):** the merged API matches this plan
> except for two differences. Error codes are `ValidationError` (TextRequired, LabelRequired,
> FolderNameRequired, FolderNameDuplicate, FolderNotFound, SnippetNotFound), with messages from
> `ToMessage()`. `StoreChangedEventArgs` has `Kind`, `Id` and `SnippetIds`. `Folders` is sorted
> by name. `Snippets` is ordered by ID with `FolderName` joined. See plan 003 §10.

---

## 1. Goal

This fills 004's Snippets and Folders pages so a person can manage the whole library from the
main window:
- **Snippets:** list, search, create, edit, delete. The label suggests itself as you type. Tags
  are comma-separated. A folder can be created inline from the editor. Deletion and unsaved
  changes are confirmed.
- **Folders:** a screen to create, rename and delete folders.

It also delivers the **reusable snippet editor**, a view-model plus a control, which 006's popup
"Add snippet" hosts unchanged.

## 2. Requirement slice (from master §2)

- **AC1, UI part (PRD §3.2):**
  - List and search snippets.
  - Create, view, edit and delete them, covering text, label, folder and tags.
  - Create a folder inline, and select it automatically.
  - Confirm before deleting.
  - Warn before discarding unsaved changes.
  - Validation rejects empty text or label without trimming the stored text.
  - Duplicate labels and texts are allowed.
- **AC2, UI part (PRD §3.3):**
  - The Folders screen supports create, list, rename and delete.
  - Folder names must be unique, ignoring case, and the error says so.
  - Deleting a folder moves its snippets to Unfiled, after a confirmation that says how many.
  - A rename shows up everywhere immediately, through 003's `Changed` event.
- **PRD §3.10:** if a save fails, the editor keeps its content and shows a clear error.
- **AC11, binding part:** lists and editor text follow `ITextSizeService.FontSize` without
  clipping, and all pages follow the theme.
- **AC8, enabler:** the editor can be hosted outside the main window and can report "saved
  snippet ID" to its host (006 uses this).
- **Out of scope:**
  - Popup UI and toolbar (006).
  - Insertion (007).
  - Import/export, bulk operations, multi-select, undo (none are in the PRD).

## 3. Current state

- **At `7d32a8e`:** no main window, pages or view-models exist. The only window is
  `App/Diagnostics/DiagnosticsWindow`.
- **Planned inputs this plan consumes** (not yet built; confirm at the start):
  - **003 §4.5 `ISnippetStore`:** `Snippets` (`IReadOnlyList<SnippetView>`), `Folders`, the
    `Changed` event, and `Create/Update/DeleteSnippet`, `Create/Rename/DeleteFolder` returning
    `StoreResult<T>` with an `Error` (`ValidationError?`, from `SmartSnippets.Core.Domain`) and a `Message`. Database failures throw
    `StoreException`.
  - **003 §4.4 rules:**
    - `LabelSuggester.Suggest(text, tags)`, `TagList.Parse/Format`, `SnippetSearch.Filter`.
    - The store, not the view-model, finalizes the label. The view-model only *previews* the
      suggestion with the same function, so preview and saved result can't differ.
  - **004 §4.1:** the Presentation project; `IDialogService` (`ConfirmAsync`,
    `AskSaveDiscardCancelAsync`, `ShowErrorAsync`); `IGuardedPage.CanLeaveAsync()` (asked by
    `MainWindow` before navigating away or hiding); `ITextSizeService.FontSize`; `IUiDispatcher`;
    the placeholder `SnippetsPage` and `FoldersPage`.
- **Platform facts:**
  - **List performance:** a `ListView` with `ItemsSource` set to a fresh list re-creates its
    containers. With 10,000 items that is noticeable unless the list is virtualized, which
    `ListView` does by default with an `ItemsStackPanel`. The list is replaced only when the
    filter result changes, not on every keystroke if nothing changed.
  - **Multiline input:** a `TextBox` keeps `\r\n` when `AcceptsReturn=true`. Pasting a bare
    `\n` is normalized to `\r` by the RichEdit-based TextBox. 005 must test that stored text is
    exactly what the user typed and pasted, and document any normalization the control itself
    imposes (risk R1).
  - **Dialogs:** only one `ContentDialog` can be open per XamlRoot. Opening a second throws, so
    `DialogService` (004) must serialize them, and every confirmation in 005 awaits it.

## 4. Design

### 4.1 Approach

**Layout:** the Snippets page is master-detail.
- **Left pane:** a search box, a "New snippet" button, and a virtualized list showing Label,
  Folder ("Unfiled" when empty) and the tags as one comma-joined line.
- **Right pane:** the snippet editor.
- **No snippet selected:** the right pane shows an empty-state message.
- **Narrow window (< 720 effective pixels):** a `VisualStateManager` stacks the panes, showing
  the list or the editor with a Back button.

**View-models (Presentation, unit-tested):**

| VM | Responsibility |
|---|---|
| `SnippetListViewModel` | query text → `SnippetSearch` over `store.Snippets` → `Items` (ordered by Label, then Id); selection; rebuilds on `store.Changed`; keeps the selection by ID when it still exists |
| `SnippetEditorViewModel` | edit session for one snippet or a new one: fields, `IsDirty`, label preview, folder choices, inline new folder, `SaveAsync`, `Errors`, `SavedSnippetId`; host-agnostic (main window and popup) |
| `SnippetsPageViewModel` | coordinates list + editor; implements `IGuardedPage`; guards selection change, New, Delete, navigation and hide with Save/Discard/Cancel; deletes after confirmation |
| `FoldersPageViewModel` | folder rows (name + snippet count); create / rename / delete with validation and confirmation; implements `IGuardedPage` when a rename is in progress |

**Views (App):**
- `SnippetsPage.xaml` and `FoldersPage.xaml` replace 004's placeholders.
- `Controls/SnippetEditor.xaml` is a `UserControl` bound to `SnippetEditorViewModel`. 006 hosts
  it unchanged.
- All bindings use `x:Bind`. List items and editor text boxes bind `FontSize` to
  `ITextSizeService.FontSize`.

**Editor field behaviour:**
- **Text:**
  - Multiline: `AcceptsReturn`, `TextWrapping=Wrap`, `IsSpellCheckEnabled=false`.
  - Stored exactly as typed.
  - Required, where "has content" means non-whitespace after trimming, for the check only.
- **Label:**
  - Shows the live suggestion while `IsLabelCustom` is false, with the placeholder "Auto: …".
  - Typing in it sets `IsLabelCustom=true`.
  - Clearing it returns it to automatic (matching 003's rule).
  - A small "Auto" indicator next to it shows the current state.
- **Folder:**
  - A `ComboBox` listing "Unfiled" first, then folders by name.
  - A "New folder…" button next to it opens a flyout with a name box.
  - On Create: `store.CreateFolder` runs at once and the new folder is **selected for this
    snippet automatically** (PRD §3.2). A duplicate or empty name shows an inline error in the
    flyout.
  - The folder is created even if the snippet edit is later discarded, because a folder is its
    own entity.
- **Tags:**
  - A single-line text box for comma-separated input.
  - Below it, normalized "chips" preview the result using `TagList.Parse`, so you see the
    trimmed, de-duplicated tags before saving.
  - On a successful save, the box is reformatted with `TagList.Format`.
- **Dates:** created, updated, last used and usage count are shown read-only in the editor's
  footer.

**Keyboard:**
- **Ctrl+N:** new snippet.
- **Ctrl+S:** save.
- **Delete** (while the list has focus): delete with confirmation.
- **Ctrl+F:** focus search.
- **Esc** in the editor: revert to the last saved values, after confirming if there are changes.

All buttons have `AutomationProperties.Name` and tooltips.

### 4.2 Alternatives considered

| Topic | Option | Verdict |
|---|---|---|
| Snippets layout | **Master-detail (list + editor side by side; stacked when narrow)** | **Chosen.** One click from list to edit, and the unsaved-change guard has one place to live. |
| | List page plus a separate editor page or dialog | Rejected. More navigation, and a modal dialog can't host the folder flyout comfortably. |
| Main list order | **Label A→Z, then ID; no sort selector** | **Chosen.** The PRD defines sorting only for the popup (§3.7). An alphabetical list is predictable for managing. |
| | Reuse the popup's 4 sorts | Rejected. Scope beyond the PRD; it can be added later. |
| Editor reuse | **One `SnippetEditorViewModel` and `SnippetEditor` control, host-agnostic** | **Chosen.** One implementation of the label, tag, folder and validation behaviour for both hosts (PRD §3.7 "Add snippet"). |
| | Separate popup editor in 006 | Rejected. Two copies of the rules would drift apart. |
| Unsaved-change prompt | **Save / Discard / Cancel dialog on any action that would lose edits** | **Chosen.** This is the standard Windows pattern. |
| | Auto-save | Rejected. The PRD asks for a warning, and auto-saving invalid drafts conflicts with validation. |
| Folder delete | **Confirmation names the folder and states "N snippets will move to Unfiled"** | **Chosen.** PRD §3.3, and you see the exact consequence. |
| Tag entry | **Comma text box with a live chip preview** | **Chosen.** The PRD says comma-separated input. The preview makes normalization visible. |
| | Token box | Rejected. WinUI has no built-in token box, and a custom control adds AOT and accessibility risk. |

### 4.3 Data model

No schema or settings changes. `SnippetEditorViewModel` holds a `SnippetDraft`:
`Text`, `Label`, `IsLabelCustom`, `FolderId`, and `TagsInput` as raw text. It submits the draft
unchanged to `store.CreateSnippet` or `UpdateSnippet`.

`IsDirty` compares the current draft with the draft captured at load or after the last save:
- `Text` and `Label` are compared ordinally.
- `FolderId` is compared directly.
- `TagsInput` is compared after `TagList.Parse`, so retyping the same tags with different
  spacing isn't counted as a change.

### 4.4 Sequence diagrams

**Save from the editor:**
```
Ctrl+S / Save button → SnippetEditorViewModel.SaveAsync
  → store.Create/UpdateSnippet(draft)
     ├─ StoreResult.Fail(TextRequired|LabelRequired|FolderNotFound) → Errors[field] (inline), IsDirty stays true
     ├─ StoreException → dialog.ShowErrorAsync("Could not save: <safe message>") ; content kept, IsDirty stays true
     └─ Ok(snippet) → capture saved draft (IsDirty=false), SavedSnippetId=snippet.Id
           store.Changed → SnippetListViewModel rebuild → reselect by Id
           (popup list in 006 rebuilds from the same event)
```

**Switching selection with unsaved edits:**
```
List selection changes to B while editor(A).IsDirty
  → SnippetsPageViewModel.RequestSelect(B) → dialog.AskSaveDiscardCancelAsync
      Save    → SaveAsync → ok? load B : stay on A (selection restored to A)
      Discard → load B
      Cancel  → selection restored to A
Same guard runs for: New, Delete-of-other, navigation to Folders/Settings, window hide (via IGuardedPage)
```

**Folder deleted on the Folders page while the editor has it selected:**
```
FoldersPageViewModel.DeleteAsync(f) → dialog.ConfirmAsync("Delete 'Work'? 12 snippets will move to Unfiled.")
  → store.DeleteFolder(f) → Changed(Kind=FolderDeleted, Id=f, SnippetIds=moved)
     → SnippetListViewModel rebuild (rows show Unfiled)
     → SnippetEditorViewModel.OnStoreChanged: folder choice list refreshed; if draft.FolderId == f
          → FolderId = null; if the editor was clean, reload from the store (so it stays clean)
```

**Parallel paths checked:**
- Every mutation in 005 goes through `ISnippetStore`, and no view-model touches a repository
  directly. The only other mutators are 006 (popup add, through the same editor view-model) and
  007 (`RecordUsage`). All of them raise `Changed`, which every list view-model subscribes to.
- Grep at implementation time: in Presentation and App, `Repository` must appear only in
  `AppHost`. The wiring audit checks this.

### 4.5 Surface list

| Surface | Change | Task |
|---|---|---|
| `Presentation/ViewModels/SnippetEditorViewModel.cs` + tests | new | T1 |
| `Presentation/ViewModels/SnippetListViewModel.cs` + tests | new | T2 |
| `Presentation/ViewModels/SnippetsPageViewModel.cs` + tests | new | T3 |
| `App/Controls/SnippetEditor.xaml(.cs)` | new | T4 |
| `App/Pages/SnippetsPage.xaml(.cs)` (replaces placeholder) | change | T5 |
| `Presentation/ViewModels/FoldersPageViewModel.cs` + tests | new | T6 |
| `App/Pages/FoldersPage.xaml(.cs)` (replaces placeholder) | change | T7 |
| `AppHost` (construct VMs, pass store/services) | change | T5/T7 |

## 5. Impact and risk

| Question | Answer |
|---|---|
| State management: what survives a crash mid-operation? | Unsaved edits are lost on a crash. The PRD doesn't ask for drafts, so that's accepted. A failed save keeps the content in memory (PRD §3.10). |
| Data consistency | All writes go through the store and its transactions. The editor reconciles with `Changed` events (folder deleted or renamed elsewhere). Saving the same draft twice is idempotent for updates. For creates, a second click while `SaveAsync` is running is blocked by an `IsSaving` guard. |
| User experience | Every destructive or lossy action asks first. Validation errors appear next to the field. The label "Auto" state is always visible. |
| Backward compatibility | n/a. The surfaces are new. |
| Error recovery | `StoreException` shows a dialog, the content is kept, and you can retry. |
| Logging & debugging | Log events: `snippet_saved id=…`, `snippet_deleted id=…`, `folder_created/renamed/deleted id=…`, `save_failed kind=…`. Only IDs, never text, labels or tags. |
| Edge cases | Covered: empty library (empty states on both pages), 10k snippets (virtualized list, search latency within AC13), whitespace-only text, label edited then cleared, tags `" a, A ,, b "`, a selected snippet removed by a search filter (the editor keeps it and the list shows no selection), a duplicate folder name differing only in case, renaming a folder to its own name in different case, deleting an empty folder, deleting a folder that the open editor has selected, a second dialog requested while one is open (serialized). |

**Risks:**
- **R1** The WinUI `TextBox` may normalize line endings it receives, for example a pasted lone
  `\n`.
  - **Mitigation:** test what the control returns. If it normalizes, document that the *stored*
    text equals the control's value, since the PRD says not to trim or modify, and the control
    is the input surface.
  - Text entered through the API or the popup is unaffected.
  - Any normalization goes in the 008 compatibility notes.
- **R2** Rebuilding the list on every `Changed` event at 10k items.
  - **Mitigation:** rebuild only the filtered result list (in memory, under 150 ms per 003's
    benchmark), and leave virtualization to `ListView`.
  - If the rebuild stalls the UI, switch to a diffing `ObservableCollection` update. That would
    be a plan update, not a silent change.
- **R3** Focus and keyboard routing between the list, the editor and the flyout. Covered by the
  runbook's keyboard-only steps.

**Rollback:** revert the commits. The pages fall back to 004's placeholders. No data changes.

## 6. Tasks

### T1 — SnippetEditorViewModel (AC1, AC8 enabler)
- **Files:** `Presentation/ViewModels/SnippetEditorViewModel.cs`, `FolderChoice.cs`,
  `tests/.../SnippetEditorViewModelTests.cs`.
- **Change:** §4.1 editor behaviour, §4.3 dirty tracking, §4.4 save flow, plus inline folder
  creation and auto-select. Constructor: `(ISnippetStore, IDialogService, long? snippetId)`.
- **Tests that fail if this breaks:**
  - `LabelPreview_FollowsFirstTagThenFirstLine`
  - `TypingLabel_SetsCustom_StopsPreview`
  - `ClearingLabel_ReturnsToAuto`
  - `TagChips_ShowNormalized`
  - `IsDirty_IgnoresTagSpacingOnly`
  - `Save_WhitespaceText_ShowsTextError_KeepsDirty`
  - `Save_StoreException_KeepsContent_ShowsError`
  - `Save_Ok_ClearsDirty_SetsSavedId`
  - `InlineFolder_CreatesAndSelects`
  - `InlineFolder_Duplicate_ShowsFlyoutError`
  - `StoreFolderDeleted_ClearsFolderChoice`
  - `DoubleSave_Blocked`
  - `TextNeverTrimmedOnSave` (the draft text passed to the fake store is byte-identical)

### T2 — SnippetListViewModel (AC1 list/search)
- **Files:** `Presentation/ViewModels/SnippetListViewModel.cs`, `SnippetRowViewModel.cs`, tests.
- **Change:**
  - The query runs `SnippetSearch.Filter`, then sorts by Label (OrdinalIgnoreCase), then Id.
  - Each row shows "Unfiled" for a null folder and its tags joined with ", ".
  - The empty state distinguishes "No snippets yet" from "No matches for '…'".
  - On `Changed`, the list is rebuilt and the selection kept by ID.
- **Tests that fail if this breaks:**
  - `Search_FiltersAcrossFields`
  - `OrderedByLabelThenId`
  - `Unfiled_DisplayedForNullFolder`
  - `Changed_RebuildsAndKeepsSelection`
  - `EmptyStates_Distinguished`
  - `FolderRename_UpdatesRowsImmediately`

### T3 — SnippetsPageViewModel (AC1 guards, delete)
- **Files:** `Presentation/ViewModels/SnippetsPageViewModel.cs`, tests.
- **Change:** the §4.4 guard on select, New, navigate and hide (`IGuardedPage`). Delete
  confirms "Delete 'label'? This cannot be undone.", then selects the next row.
- **Tests that fail if this breaks** (fake `IDialogService` returning scripted answers):
  - `SwitchWhileDirty_Save_SavesThenSwitches`
  - `SwitchWhileDirty_SaveFails_StaysOnCurrent`
  - `SwitchWhileDirty_Discard_Switches`
  - `SwitchWhileDirty_Cancel_RestoresSelection`
  - `CanLeave_Dirty_AsksOnce`
  - `Delete_Confirmed_RemovesAndSelectsNext`
  - `Delete_Cancelled_NoChange`
  - `New_WhileDirty_Guarded`

### T4 — SnippetEditor control (AC1, AC11)
- **Files:** `App/Controls/SnippetEditor.xaml(.cs)`.
- **Change:**
  - The §4.1 field layout, with `x:Bind` to the view-model and `FontSize` bound on Text, Label
    and Tags.
  - The folder flyout.
  - Errors appear as red text under each field and are announced through
    `AutomationProperties.LiveSetting`.
  - Ctrl+S is a `KeyboardAccelerator`.
- **Verify:** CI AOT publish green (0 IL warnings). Runbook steps 2–7.

### T5 — Snippets page (AC1)
- **Files:** `App/Pages/SnippetsPage.xaml(.cs)` and `AppHost` (construct the view-models).
- **Change:**
  - The master-detail layout with its narrow visual state.
  - The `ListView`, with item `FontSize` bound and virtualization left on.
  - Keyboard accelerators Ctrl+N, Ctrl+F and Delete.
  - The page passes `IGuardedPage` through to `MainWindow`.
- **Verify:** runbook steps 1–10.

### T6 — FoldersPageViewModel (AC2)
- **Files:** `Presentation/ViewModels/FoldersPageViewModel.cs`, `FolderRowViewModel.cs`, tests.
- **Change:**
  - Rows show the name and snippet count, ordered by name.
  - Creating a folder runs `FolderNameRules` through the store, and errors appear inline.
  - Rename works in place: Enter commits, Esc cancels.
  - Delete confirms with the count (§4.2).
  - The page refreshes on `Changed`.
- **Tests that fail if this breaks:**
  - `Create_Duplicate_CaseInsensitive_ShowsError`
  - `Create_Empty_ShowsError`
  - `Rename_ToSameNameDifferentCase_Allowed`
  - `Rename_ToOtherExisting_ShowsError`
  - `Delete_ConfirmTextStatesCount`
  - `Delete_Confirmed_SnippetsUnfiled`
  - `Counts_UpdateOnSnippetChanges`

### T7 — Folders page (AC2)
- **Files:** `App/Pages/FoldersPage.xaml(.cs)`, `AppHost`.
- **Change:** the list with inline rename, a "New folder" box and button, a Delete button per
  row, and an empty state.
- **Verify:** runbook steps 11–15.

### T8 — Wiring audit (L)
- Every view-model is constructed in `AppHost` and bound by its page.
- `SnippetEditor` is used by `SnippetsPage`, and 006 will be its second host.
- `store.Changed` is subscribed by `SnippetListViewModel`, `SnippetEditorViewModel` and
  `FoldersPageViewModel`, and each unsubscribes when disposed.
- The grep check passes: no `Repository` usage outside `AppHost`/Core.
- No `AppLog` call includes text, label or tags.
- **Verify:** these log lines appear in the runbook: `snippet_saved`, `snippet_deleted`,
  `folder_created`, `folder_renamed`, `folder_deleted`.

## 7. Test strategy

- **Unit tests (Linux and Windows CI):** about 45 view-model tests, with a fake store (an
  in-memory `ISnippetStore` that applies 003's real rules, so behaviour matches) and a scripted
  fake `IDialogService`.
- **Integration:** `SnippetEditorViewModelIntegrationTests` run the view-model against the
  **real** `SnippetStore` on a temp SQLite database, covering: save, then reload the store, then
  the snippet is identical, including Unicode and CRLF text.
- **Manual runbook** (your PC, CI artifact):

| # | Step | Expected |
|---|---|---|
| 1 | Open the main window with an empty library | Snippets page shows "No snippets yet" and a New button |
| 2 | Ctrl+N, type text `Hello\r\n  world  ` (with trailing spaces), no tags | The label preview reads "Auto: Hello". Save. The list shows "Hello". Reopening shows the text exactly as typed |
| 3 | Type tags `" work, Work ,, mail "` | Chips preview "work" and "mail". After saving, the box reads "work, mail" and the label becomes "work" |
| 4 | Edit the label to "Greeting", then change the text | The label stays "Greeting". Clear the label: it goes back to "Auto: work" |
| 5 | Folder → New folder… "Clients" → Create | The folder is created and selected in the editor. Save. The list row shows "Clients" |
| 6 | Make an edit, then click another snippet | Save/Discard/Cancel appears. Each option behaves as described in §4.4 |
| 7 | Text containing only spaces → Save | Inline "Text is required". Nothing is saved, and the content stays |
| 8 | Delete key on a list row → confirm | The snippet is gone and the next row is selected. Repeat and cancel: nothing changes |
| 9 | Ctrl+F, type "clie mail" | Only snippets matching both terms in any field (including the folder name) are shown |
| 10 | Make an edit, then close the window with X | The guard asks first. Cancel keeps the window open |
| 11 | Folders page: create "clients" | "A folder named 'Clients' already exists" |
| 12 | Rename "Clients" → "Customers" | The Snippets list and the editor's folder dropdown show "Customers" immediately |
| 13 | Delete "Customers", which holds 2 snippets | The confirmation says 2 snippets will move to Unfiled. After it, those rows show "Unfiled" |
| 14 | Settings → text size Large, then Custom 32 | Snippets list and editor text grow live, with nothing clipped |
| 15 | Keyboard only: Tab through both pages | Every control is reachable, has a visible focus indicator, and is announced by Narrator |

## 8. Rollout / phasing

- **Track A** (Presentation view-models: T1 → T2 → T3 → T6) and **Track B** (App views: T4 → T5
  → T7) can run in parallel once 004 has landed. B depends on A's view-model *signatures*, so A
  publishes those first as stubs.
- **T8** comes last.
- 005 depends on 004's shell being merged first. 003 and 004 can run in parallel with each other
  only if 004's Track A builds against 003's interfaces. Otherwise the order is 003 → 004 → 005.

## 9. Open questions

None blocking. These are design calls for you to confirm at approval:
1. **Master-detail layout** for the Snippets page, stacking when the window is narrow.
2. **The main list is sorted by label A→Z**, with no sort selector. The four sorts stay
   popup-only, as in the PRD.
3. **A folder created inline is kept** even if the snippet edit is then discarded.
4. **Tags are a comma text box with a live chip preview**, not a token box.
5. **No auto-save or draft recovery.** Unsaved edits are lost on a crash, but every in-app action
   that would lose them asks first.

## 10. Implementation notes and wiring audit

**Views (T4, T5, T7).** All bindings are `x:Bind`; function bindings (`Show`, `Hide`, `HasText`) are instance
methods. `SnippetEditor` exposes plain `ViewModel` and `TextSize` properties (each setter calls `Bindings.Update()`),
so hosts set them in code; `SnippetsPage` pushes `ViewModel.Editor` into the control from its `PropertyChanged`
handler. The editor binds Text, Label, Tags and the new-folder name two-way with
`UpdateSourceTrigger=PropertyChanged`, and shows tag chips in an `ItemsRepeater` with `FlowLayout`. Ctrl+S and Esc
are control-level `KeyboardAccelerator`s; the folder flyout closes when `CreateFolderAsync()` returns true. The
Snippets page is a two-column grid (list 360 px, editor `*`) with an `AdaptiveTrigger` at 720 px; the narrow state is
applied in code-behind (one pane at a time, Back button, `ItemClick` re-opens the editor). Ctrl+N, Ctrl+F and
Delete (on the list) are accelerators/key handlers calling the view-model commands. Folder rows are a DataTemplate,
so their buttons are `Click` handlers that read the row from `DataContext`; the template uses two small converters
(`App/Converters/VisibilityConverters.cs`) because it cannot call page functions. No view-model changed.

**Wiring audit (T8).**
- `AppHost.CreatePage` constructs `SnippetsPageViewModel(Store, Dialogs)` and `FoldersPageViewModel(Store, Dialogs)`
  and passes `TextSize` to both pages; the editor view-models are created by `SnippetsPageViewModel`. The two page
  view-models are kept in fields and disposed in `ReleaseResources`.
- `SnippetEditor` is used by `SnippetsPage` only; 006 is its second host (it needs just `ViewModel` and `TextSize`).
- `store.Changed` subscribers: `SnippetListViewModel`, `SnippetEditorViewModel`, `FoldersPageViewModel`; each
  unsubscribes in `Dispose`, and `SnippetsPageViewModel` disposes the list and the current editor.
- `Repository` appears in `src/SmartSnippets.App` and `src/SmartSnippets.Presentation` only in `AppHost` and the
  Diagnostics prototype (`ProbeRepository`).
- `AppLog` calls in Presentation log only ids and failure kinds: `snippet_saved`, `snippet_deleted`,
  `folder_created`, `folder_renamed`, `folder_deleted`, `save_failed`, `delete_failed`, `select_failed`.
- Not compiled on Linux: the XAML and code-behind are verified only by CI (AOT publish) and the runbook. Open
  points to check there: list-item text size inherited from `ListView.FontSize`, the Esc accelerator versus an open
  ComboBox, and the converters under AOT.

## 11. Approval log

| Date | Event | Notes |
|---|---|---|
| 2026-10-27 | Requirement brief confirmed (G1) | master §2 |
| 2026-10-27 | Plan approved (G2) | approved together with 003–005 incl. §9 design calls |
| 2026-10-27 | Implementation complete (views) | T4, T5, T7, T8 done; awaiting CI (AOT publish) and manual runbook §7 |
