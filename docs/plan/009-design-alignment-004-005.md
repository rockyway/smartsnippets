# 009 — Design alignment for the shell, settings and main window (004/005)

**Date:** 2026-10-27
**Status:** Complete — CI green on `main` (run #33, `452e5a3`, incl. AOT publish gate); manual runbook (§7) pending on user PC
**Size:** M (subplan of [001-master-smart-snippets-v1.md](001-master-smart-snippets-v1.md))
**Area:** `src/SmartSnippets.Presentation/`, `src/SmartSnippets.App/` (pages, controls, `TrayIcon`, `DialogService`, `HotkeyManager`), `tests/SmartSnippets.Presentation.Tests/`
**Depends on:** 004 and 005 (both complete, CI green at `6e75872`)
**Design reference:** [docs/design/001-smart-snippets-design.html](../design/001-smart-snippets-design.html) (user, commit `3f11852`)
**Decision that created this plan:** "Finish, then align" (user, 2026-10-27). 005 was finished as
approved; this plan brings the already-built 004/005 surfaces in line with the design. 006–008
follow the design from the start and are out of scope here.

---

## 1. Goal

Make the main window, Folders page, Settings page, dialogs and tray menu match the design
reference in layout, wording and behaviour, without changing the Core data model.

Done when every row of §3 is either implemented or explicitly deferred, the view-model tests
cover the new behaviour, and CI (including the AOT publish gate) is green.

## 2. Out of scope

- **Popup** (add-snippet editor over the popup, insertion-failure footer, "Copy text" there): 006.
- **Elevation message wording** ("The target app is running as administrator…"): 007.
- **App icon, tray glyph and its state dots** (blue = popup open, red = hotkey unavailable): 008
  ships the icon assets. The red-dot *state* is exposed here (T6) so 008 only swaps the image.
- Core schema, store and search behaviour: unchanged.

## 3. Gap list (design vs. built)

| # | Area | Design | Built today | Task |
|---|---|---|---|---|
| G1 | Main window layout | Three panes: **Browse** (All / each folder / Unfiled, with counts), list, editor | Two panes: list and editor | T1, T3 |
| G2 | List rows | Label; one-line text preview; "Support, used 31 times" / "Unfiled, never used" | Label; folder; tags | T1, T3 |
| G3 | Editor label | "Custom label" toggle; auto label hint "Edit the label to stop it following your tags." | "Auto" button, placeholder "Auto: …" | T2, T3 |
| G4 | Editor folder | Folder combo + **New** → inline name box with Create / Cancel | "New folder…" flyout | T3 |
| G5 | Editor stats | Read-only block, visually separate: Created, Last used ("Today, 09:12"), Used ("31 times") | Footer: Created, Updated, Last used, Times used | T2, T3 (keeps Updated, Q3) |
| G6 | Editor footer | **Delete** (danger) on the left; "Unsaved changes" bar with **Discard** and **Save** | Save, Revert, "Unsaved changes" text; Delete only on the list | T2, T3 |
| G7 | Folders page | "N snippets"; **Unfiled** as a built-in row ("2 snippets, built in"), no rename/delete; note "Deleting a folder moves its snippets to Unfiled. The snippets themselves are kept." | Folders only; "N snippet(s)" | T4 |
| G8 | Settings order and controls | Global hotkey first (keycaps, **Change**, **Restore default**; hint "Press a new combination to capture it. Conflicts are explained here."), then Text size ("14 units. Custom accepts 10 to 32."), Theme (Light / Dark / System), Start at login ("Starts in the background without opening the main window.") | Appearance, Startup, Shortcut sections; recorder + **Apply** | T5 |
| G9 | Hotkey change | Applies immediately after capture; on failure "Another app is already using Ctrl+Alt+Space. Your hotkey is still Ctrl+Shift+Space. Try a different combination." | Separate Apply step; "Already used by another application." | T5 |
| G10 | Delete snippet dialog | Title 'Delete "Refund policy"?', body "This snippet and its usage history will be permanently removed.", buttons Cancel / **Delete snippet** | "Delete '…'? This cannot be undone." / Delete | T2 |
| G11 | Discard dialog | "Discard your changes?" / "You've edited this snippet but haven't saved it." / **Keep editing** · Discard · Save | "Unsaved changes" / Save · Discard · Cancel | T2 |
| G12 | Delete folder dialog | 'Delete folder "Support"?' / "Its 2 snippets will move to Unfiled. The snippets aren't deleted." / Cancel · **Delete folder** | "Delete '…'? N snippets will move to Unfiled." / Delete | T4 |
| G13 | Duplicate folder | "That folder name is already taken. Try another." | "A folder named '…' already exists." | T2 |
| G14 | Save failure | "Couldn't save your snippet. Your text is still in the editor, so nothing is lost." with **Try again** / **Copy text** | "Save failed" dialog with the exception message | T2, T3 |
| G15 | Tray menu | Open Smart Snippets · Show popup (Ctrl+Shift+Space) · Settings · Exit | Open · Settings · Restart as administrator · Diagnostics · Exit | T6 (keeps Restart/Diagnostics, Q2) |
| G16 | Tokens | Segoe UI Variable, neutral surfaces, one blue accent, violet folder tint, danger red | WinUI defaults | T7 |

## 4. Design

### 4.1 Approach

- **Strings as constants in Presentation.** Every user-facing message from §3 lives in one
  `Presentation/Text/UiText.cs` static class, so tests assert exact wording and views never
  hard-code dialog text. Existing scattered constants (`FolderMessages`, `NoSnippetsMessage`, …)
  move there.
- **Browse pane (G1).** `SnippetListViewModel` gains `BrowseItems`
  (`BrowseItemViewModel(Kind: All|Folder|Unfiled, FolderId, Name, Count)`) and `SelectedBrowse`.
  The filter is applied after search, so search still runs over the chosen scope. Counts update
  on `Changed`. Narrow layout (< 720 px): the browse pane becomes a ComboBox above the list.
- **Rows (G2).** `SnippetRowViewModel` becomes `(Id, Label, Preview, Meta)`. `Preview` is the
  first non-blank line, whitespace-collapsed, cut at 80 chars with "…". `Meta` is
  "<Folder or Unfiled>, used N times" / "used once" / "never used".
- **Editor (G3–G6, G14).** The view-model gets `DeleteCommand` (delegates to the page's delete
  flow so the confirmation stays in one place), `DiscardCommand` (replaces Revert, same
  behaviour), `SaveFailed` state with `RetryCommand`, and `CopyText` through a new
  `IClipboardService` (App implementation via `Windows.ApplicationModel.DataTransfer.Clipboard`).
  Save failures show inline in the editor (InfoBar, error severity) instead of a modal, so the
  text stays visible. The stats block shows Created, **Updated**, Last used and Used: the user
  chose to keep Updated (Q3), a deliberate deviation from the design.
- **Dialogs (G10–G12).** `IDialogService.ConfirmAsync(title, message, confirmText)` already
  takes the button text; only the wording changes. `AskSaveDiscardCancelAsync` gets the button
  labels Save / Discard / **Keep editing** and the design's title and body. The confirm button
  for destructive actions uses the danger style (`AccentButtonStyle` swapped for a danger
  style in T7).
- **Folders (G7, G12).** `FoldersPageViewModel.Rows` adds a final built-in Unfiled row
  (`IsBuiltIn = true`, rename/delete hidden, meta "N snippets, built in"). Count wording becomes
  "1 snippet" / "N snippets".
- **Settings (G8, G9).** The recorder captures and applies in one step: once a valid gesture is
  captured, `SettingsViewModel` calls `IHotkeyManager.TryChange` immediately. The **Apply**
  button is removed. `HotkeyManager`'s conflict reason becomes the design sentence, built from
  the attempted and the kept gesture. Section order and helper text follow the design.
  "Restart as administrator" stays at the bottom under **Advanced** (the design does not show
  it, the PRD needs it); it also stays on the tray menu (Q2).
- **Tray (G15).** Menu: Open Smart Snippets · Show popup `\t<current hotkey>` · Settings ·
  separator · Restart as administrator (only when not elevated) · Diagnostics · separator ·
  Exit. The design's four items come first in its order; the user chose to keep Restart and
  Diagnostics on the tray (Q2). "Show popup" invokes the existing popup show path (the 002
  prototype until 006 replaces it). The accelerator text refreshes on `IHotkeyManager.StatusChanged`.
  `TrayIcon` exposes a `State` (Ready / PopupOpen / HotkeyUnavailable) for 008's glyph.
- **Tokens (G16).** `App.xaml` gets a small `Theme/Tokens.xaml` resource dictionary with
  light/dark `ThemeDictionaries`: `FolderTintBrush`, `DangerBrush`, `DangerButtonStyle`,
  `AccentTintBrush` (selected row). Font: WinUI already uses Segoe UI Variable on Windows 11;
  nothing to set. Accent stays the system accent (§8 Q1).

### 4.2 Alternatives considered

- **Re-skin only in XAML, keep view-models.** Rejected: the browse filter, row text, inline save
  failure and immediate hotkey apply are behaviour, and must be unit-tested on Linux.
- **Third-party WrapPanel/Settings controls (CommunityToolkit WinUI).** Rejected for now: adds
  AOT surface for little gain; built-in controls cover the design.

### 4.3 Data model

No Core change. `Snippet.UsageCount`, `LastUsedAt`, `CreatedAt` already exist.

## 5. Impact and risk

| Risk | Mitigation |
|---|---|
| Wording changes break existing Presentation tests | Expected: tests asserted the old approved wording. Update them to `UiText` constants **because the requirement changed** (design reference), and say so in the commit |
| Immediate hotkey apply could leave the user with a hotkey they did not mean | The capture only fires on a full valid gesture; Restore default is one click; failure keeps the old hotkey (already true) |
| Browse filter + search interplay confuses selection | Selection is kept if the row is still visible, otherwise cleared through the existing unsaved-changes guard; tested |
| `IClipboardService` from a background thread | Called only from UI-thread commands; App implementation asserts dispatcher access |
| XAML compile surprises (WinUI 3 lacks some WPF/UWP types, e.g. `FlowLayout`) | One Windows CI run per track before merge; no new third-party controls |

## 6. Tasks

Parallel tracks after T0. Each track owns its files; T0 is written first by the coordinator.

- **T0 — Shared contracts (coordinator).** `UiText`, `BrowseItemViewModel`, `IClipboardService`,
  new `SnippetRowViewModel` shape, `TrayState` enum. Verify: Presentation builds, existing tests
  updated to the new constants.
- **T1 — List view-model (Track A).** Browse items, counts, filter, preview/meta rows. Verify:
  tests for each browse kind, counts after create/move/delete, preview truncation, meta wording.
- **T2 — Editor and dialog view-models (Track A).** Custom-label toggle, Delete/Discard/Retry/Copy
  commands, inline save failure, new dialog wording (G10, G11, G13, G14). Verify: tests for each
  command and message, including save failure keeps text and dirty state.
- **T3 — Snippets page and editor control (Track B, after T1/T2 contracts).** Three-pane layout,
  narrow layout, stats block, footer bar, inline folder create. Verify: CI compile + AOT gate;
  runbook steps in §7.
- **T4 — Folders view-model and page (Track C).** Unfiled row, counts, dialog wording, footnote.
  Verify: tests for Unfiled row (not renamable/deletable, count) and delete wording.
- **T5 — Settings (Track C).** Immediate apply, conflict sentence, section order and helper text.
  Verify: tests for apply-on-capture, conflict message with both gestures, Restore default.
- **T6 — Tray menu and state (Track D).** Menu items and order, live accelerator text, `State`.
  Verify: CI compile; runbook step.
- **T7 — Tokens (Track D).** `Theme/Tokens.xaml`, danger button style, folder tint on folder
  chips and browse items. Verify: CI compile; light/dark runbook step.
- **T8 — Wiring audit and docs (coordinator).** Every §3 row checked against code; plan 004/005
  runbooks updated where wording changed; master status.

## 7. Test strategy

- **Automated (Linux + CI):** Presentation tests for T1, T2, T4, T5 (new and updated); Core tests
  unchanged and still passing; CI Windows job builds and AOT-publishes with zero IL warnings.
- **Manual (user PC), replaces the matching steps of 004 §7 and 005 §7:**
  1. Browse pane: All / a folder / Unfiled filter the list; counts change after moving a snippet.
  2. Rows show preview and "Folder, used N times"; a new snippet shows "never used".
  3. Editor: toggle Custom label; clear it and the label follows the first tag again.
  4. New folder inline: duplicate name shows "That folder name is already taken. Try another."
  5. Delete from the editor shows the design dialog; Cancel keeps the snippet.
  6. Edit, then switch snippet: "Discard your changes?" with Keep editing / Discard / Save.
  7. Lock the database file externally and save: inline error, text kept, Try again works after
     unlocking, Copy text puts the text on the clipboard.
  8. Editor stats show Created, Updated, Last used and Used; Delete key on the list and the
     editor's Delete button both ask the same confirmation.
  8a. Folders page: Unfiled row has no rename/delete; delete-folder dialog wording.
  9. Settings: capture a hotkey used by another app → design sentence; old hotkey still works.
  10. Tray menu shows Open / Show popup (current hotkey) / Settings, then Restart as
      administrator and Diagnostics, then Exit; changing the hotkey updates the accelerator text.
  11. Light and dark theme: danger buttons and folder tint readable in both.
  12. Narrow window (< 720 px): browse becomes a dropdown; one pane at a time.

## 8. Decisions (user, 2026-10-27)

| # | Question | Decision |
|---|---|---|
| Q1 | Accent colour | **System accent.** The design's blue is the Windows default accent; no fixed brush |
| Q2 | "Restart as administrator" and "Diagnostics" (not in the design) | **Keep them on the tray too**, after the design's items; Restart also stays in Settings → Advanced. Diagnostics is still removed in 008 |
| Q3 | "Updated" timestamp in the editor | **Keep it** (deviation from the design) |
| Q4 | Where delete is available | **Editor Delete button and the Delete key on the list**, same confirmation |

## 9. Rollout

One branch (`claude/epic-allen-07hez6`), Sonnet subagents per track as for 003–005, coordinator
merges, runs both test projects, pushes, and keeps CI green. 006 planning can start in parallel
because it depends only on T0's `UiText` and the editor view-model contract.

## 10. Implementation notes and wiring audit

Implemented 2026-10-27 by four Sonnet tracks on top of the coordinator's T0 contracts (`9fe0dbf`):
A = T1/T2 (`6deffb6`), B = T3 (`31cf060`), C = T4/T5 (`0caec59`), D = T6/T7 (`c72d357`).

**Verification.** Core 164/164, Presentation 128/128 (was 95), Presentation build 0 warnings.
The App project (XAML, tray, tokens) is **not compiled yet**: CI now runs only on pushes to `main`
(commit `59f79f7`), and Linux cannot build WinUI. Expect possible XAML fixes on the first Windows run.

**Wiring audit (§3 rows).**

| Row | Where | Status |
|---|---|---|
| G1 | `SnippetListViewModel.BrowseItems/SelectedBrowse`; SnippetsPage browse ListView (wide) / ComboBox (narrow) | Done |
| G2 | `SnippetRowViewModel(Id, Label, Preview, Meta)`; row template | Done |
| G3 | `IsLabelCustom` ToggleSwitch + `AutoLabelHint`; turning custom off now resets the label to the suggestion | Done |
| G4 | `IsCreatingFolder`, Start/CancelNewFolder commands; inline row replaces the flyout | Done |
| G5 | Stats card: Created, Updated (Q3), Last used ("Today, HH:mm"), Used | Done |
| G6 | Editor Delete (`DeleteRequested` → page's single confirm path), Unsaved bar with Discard (no dialog) / Save | Done |
| G7 | `FolderRowViewModel.IsBuiltIn/CanEdit`; Unfiled row last; footnote | Done |
| G8 | Settings order, keycaps, Change / Restore default, hints | Done |
| G9 | Apply on capture; `HotkeyManager` returns `UiText.HotkeyConflict`; feedback in `HotkeyFeedback` | Done |
| G10–G13 | `UiText` wording in page/folder view-models, `DialogService` "Keep editing", `FolderMessages` | Done |
| G14 | Inline InfoBar `HasSaveFailure` with Try again / Copy text (`IClipboardService` → `ClipboardService`) | Done |
| G15 | Tray: Open / Show popup\t<hotkey> / Settings / Restart as administrator / Diagnostics / Exit; `TrayState` + tooltip | Done |
| G16 | Tokens inlined in `App.xaml` (no `ms-appx` dictionary): FolderTint(Background)Brush, AccentTintBrush (system accent), Danger(Background/Foreground)Brush, DangerButtonStyle | Done |

**Contract cross-check.** Every `x:Bind` path in SnippetsPage, SnippetEditor, FoldersPage and
SettingsPage resolves to a view-model member or an instance helper in code-behind; no bound helper
is static.

**Deviations / notes.**
- `SnippetEditorViewModel.SaveAsync` now completes synchronously (no modal error dialog).
- Hotkey outcome text moved from `StatusMessage` to `HotkeyFeedback`/`HotkeyFeedbackSeverity`.
- "Show popup" from the tray restores the previous foreground window before capturing the target;
  untested on Windows, 006 refines target capture.
- `TrayState.PopupOpen` is declared but not set until 006; glyph dots are 008.

**Code review (`/code-review` medium, 741b9a1..8232071) — fixed:** tray "Show popup" could target
the taskbar (now looks past shell windows in Z-order); Esc discarded edits silently (now asks via
`ConfirmDiscardAsync`; the Discard button still doesn't); "Copy text" could crash on a busy clipboard
(COMException caught and logged; UI-thread check added); empty-list text ignored the Browse scope
(`UiText.NoSnippetsIn`/`NoMatchesIn`); Browse counts were recomputed per keystroke (now one pass,
only when the store snapshots change); toggling Custom label off/on lost the custom label (restored);
`ThemeToIndex` fallback now matches `IndexToTheme` (System). **Not changed:** CI only on `main` (user
decision); hotkey failure wording partly in `HotkeyManager` rather than `UiText` (style).
Presentation tests: 136/136.

**First Windows compile (after merge to `main`).** Run #31 failed: `SnippetEditor.xaml.cs` lost
`using Microsoft.UI.Xaml.Controls;` (rockyway/smartsnippets#2). Run #32 failed: bool-returning x:Bind
function bindings on `Visibility` generated uncompilable code (CS0103 `obj`); the helpers now return
`Visibility`, and the folder ComboBox uses a typed template instead of `DisplayMemberPath` (WMC1510,
AOT) (rockyway/smartsnippets#3). Run #33 green.

**Known Windows-compile risks** (from the tracks): converter inside a page-level DataTemplate,
`ToggleSwitch.OnContent` via x:Bind, StackPanel inside `InfoBar.Content`, keycap `ItemsControl`
with `x:DataType="x:String"`, HighContrast `SystemColor*Color` keys, `DangerButtonStyle` BasedOn
`DefaultButtonStyle` in App.xaml.

## 11. Approval log

| Date | Event | Notes |
|---|---|---|
| 2026-10-27 | Draft | Written after 005 went green (CI #28), per "Finish, then align" |
| 2026-10-27 | §8 decisions | User answered Q1–Q4 (system accent; keep Restart/Diagnostics on the tray; keep Updated; delete from editor and Delete key). Plan still awaiting G2 approval |
| 2026-10-27 | Plan approved (G2) | User: "approve 009, implement with Sonnet subagents". T0 contracts committed by the coordinator |
| 2026-10-27 | Implemented | Tracks A–D merged; Core 164, Presentation 128 green; Windows compile pending (CI on `main` only) |
| 2026-10-27 | Complete — CI green | Merged to `main` via rockyway/smartsnippets#1–#3; run #33 green incl. AOT publish. Manual runbook §7 pending |
