# 012 — Shared snippet content and metadata hover

**Date:** 2026-10-29
**Status:** In progress (approved 2026-10-29)
**Size:** M
**Area:** Presentation row projections/cache, App shared row and hover UI, main/popup lifecycle, tests
**Requirement brief confirmed:** 2026-10-29 (G1)
**Working branch:** `feature/popup-metadata-tooltips` (includes existing uncommitted metadata-hover work)

## 1. Goal

Make both snippet lists reveal actual content and individual metadata after a 500 ms hover, using a readable bounded surface that can be scrolled without changing focus or activating a snippet.

## 2. Requirement Brief (confirmed at G1)

**Problem:** The hotkey popup has folder/tag hover details but no snippet-content tooltip. The main list lacks the same metadata styling and hover behavior, and a Ctrl+N hint can appear when hovering a list item.

**Goal:** Provide consistent, delayed access to snippet content and metadata in both lists.

**In scope**
- In the hotkey popup and main snippet list, hovering the **label** shows the snippet’s actual content after **500 ms**.
- Show long or multiline content in a **bounded, scrollable** surface.
- Hovering the folder or tags shows that item’s full text after the same delay.
- In the main list, match the popup’s smaller, subdued folder/tag styling.
- Investigate and fix the misplaced Ctrl+N hint while retaining the New button’s shortcut hint.
- Create a separate backlog item for password-type snippets and masked display.

**Behavior and constraints**
- The hover surface must remain open while the user moves the pointer into it to scroll, and close when the pointer leaves the row/hover surface or its host closes.
- It must not steal keyboard focus, change selection, paste, or use the clipboard.
- Current snippets have no password type. **Their actual content will appear unmasked on hover** until the separate password feature is designed and implemented.
- Snippet content and metadata must not be written to logs.
- Keep content current after edits and cache refreshes; avoid background access to the store.

**Out of scope**
- Implementing password storage, encryption, masking, or a reveal workflow now. Those need a separate requirements and security design.
- Changing search, insertion, or snippet data.

**Acceptance criteria**
1. AC1: Both lists show the correct actual content after a 500 ms label hover; short hovers show nothing.
2. AC2: Long/multiline content remains readable in a bounded surface that can be scrolled without the surface closing.
3. AC3: Folder and tags each show their own full text after a 500 ms hover in both lists.
4. AC4: Main-list metadata matches the popup’s smaller, subdued styling and remains readable in selected and high-contrast states.
5. AC5: Hovering a list item no longer displays the New button’s Ctrl+N hint; the button retains its hint.
6. AC6: Rebinding, editing, closing, and reopening cannot display stale content; hover does not change focus, selection, insertion, or clipboard state.
7. AC7: A backlog item records the future password-type/masked-display requirement without claiming that current content is protected.

**Decisions made:** Actual content, scrollable display, 500 ms delay, and label-versus-metadata hover targets.

**Still open:** Password-feature security and storage design, deferred to its own requirements work.

**Size:** M — this crosses both list surfaces, content projection/cache behavior, and hover lifecycle.

## 3. Current state (discovery against working tree)

- `App/Controls/PopupRow.xaml:7-30` has a heading, label and separate folder/tag fields; metadata has 85% sizing and `PopupMetadataOpacity`. `PopupRow.xaml.cs:32-106` owns the existing 500 ms timer, native ToolTip, owner registration, generation check and cancellation. A detached native ToolTip previously crashed; current code registers its owner before opening. Its hit testing is disabled, so it cannot support pointer scrolling.
- `App/Pages/SnippetsPage.xaml:232-251` instead contains an inline main-row template, one combined Details field, and metadata at full label size. These two row render paths must converge. `Presentation/ViewModels/SnippetRowViewModel.cs:14-18,32-43` stores folder/tags and an 80-character collapsed preview, not actual body text.
- `Presentation/ViewModels/SnippetRowViewModel.cs:24-28` deliberately ignores preview/body and usage in equality. `SnippetListViewModel.cs:179-219` rebuilds projections but retains existing Items when SequenceEqual succeeds. Merely adding a tooltip property without updating equality can leave content stale after body-only edits.
- `Presentation/ViewModels/PopupViewModel.cs:183-207` builds a compact row cache with an empty Preview and uses record equality to avoid unnecessary Item replacement. `:215-233` dirties/coalesces changes and flushes before show. Grouped rows omit the folder chip intentionally. Both flat and grouped snippet rows use PopupRow through `App/Popup/PopupWindow.xaml:25-26`.
- `App/Pages/SnippetsPage.xaml:46-49` attaches Ctrl+N/Ctrl+F accelerators to the Page. `:195-201` separately provides the New button hint. Automatic accelerator tooltip placement is a plausible cause of Ctrl+N over the whole page, **not yet a reproduced root cause**. Suppression must retain accelerator behavior and explicit button hint.
- `App/Popup/PopupWindow.xaml.cs:256-266` explicitly cancels existing row hover on Hide, including before the early return. `:74-82` has a separate Closed path; `:380-391,437-443` restores focus around menus/blank surfaces, and `:403-435` configures native drag regions. A new interactive hover must not be mistaken for a blank drag/focus surface or deactivate the popup.
- `App/MainWindow.xaml.cs:156-168` caches/navigates pages, while `:249-263` hides the native window after guard approval without unloading its content. `App/Pages/SnippetsPage.xaml.cs:205-220` collapses the list when the narrow editor opens. Unloaded alone is insufficient for main-list hover cleanup.
- Existing test seams include `tests/SmartSnippets.Presentation.Tests/SnippetListViewModelTests.cs:305-324` (usage changes do not rebuild stable rows), `PopupCacheStabilityTests.cs:96-133` (updated search fields), and `PopupRowFontSourceTests.cs:17-47` (source guards, not runtime hover tests).
- `Core/Domain/Snippet.cs:3-13` has ordinary text only and no password/sensitive type. [Backlog 001](../backlog/001-password-type-snippets.md) captures the separate requested feature.

Paths under App, Presentation and Core above are relative to their corresponding `src/SmartSnippets.*` project directories. Line numbers describe discovery before plan implementation.

## 4. Design

### 4.1 Shared row rendering and current content

Introduce `App/Controls/SnippetRowDetails.xaml(.cs)` for the label/folder/tags portion used by both lists. Its typed properties are `Label`, `FolderName`, `TagsDisplay`, `ContentText` and `TextSize`; compiled bindings and conventional dependency properties remain Native-AOT compatible. PopupRow keeps only its existing heading-versus-snippet wrapper and delegates snippet rendering to the shared control. The main list replaces its inline row layout with that same control. Preserve compact row layout, trimming and existing grouped heading/folder-chip policy. Metadata remains 85% of the selected text size, using the existing subdued opacity token, with full opacity under high contrast and inherited foreground on selected rows.

Add an explicit `ContentText` immutable string reference to both row projections, populated from `Snippet.Text` on the existing UI-thread projection path. Do not use the truncated/normalized Preview or overwrite its meaning. This retains a reference to the immutable stored string, not a second copy of its characters, a whole domain object, or a hover-time background store call. Main equality and hash include ContentText with ordinal equality; keep usage/timestamps excluded. Popup record equality includes the new property. Body-only changes, including changes beyond the first 80 characters, therefore replace stale projections; separately allocated equal content and usage-only changes with unchanged ordering retain stable rows. Existing sort-induced reorder behavior remains valid.

Normal row accessibility names/ToString must remain label/metadata only; actual body is exposed only by the deliberately opened content surface. Do not let record-generated diagnostics or logs print ContentText. Content update/rebind closes any pending/open hover before old text can persist.

### 4.2 Shared delayed hover coordinator and surface

Replace the metadata-only per-PopupRow timer/ToolTip code with one reusable hover implementation shared by both list roots. Implement framework-free policy in `Presentation/Services/HoverDetailsCoordinator.cs`, with an injected UI-thread delayed scheduler and show/close callbacks. The WinUI host in `App/Controls/HoverDetailsSurface.xaml(.cs)` owns a `Microsoft.UI.Xaml.Controls.Primitives.Popup` containing themed opaque Border + ScrollViewer + wrapping TextBlock. This is a read-only hover detail surface, **not a focus-taking Flyout or ContentDialog**.

- Label target passes ContentText; folder target passes FolderName; tag target passes complete TagsDisplay, not legacy Tag1/Tag2. Empty values and headings have no hover surface.
- Wait 500 ms of continuous hover over the same target. Switching targets restarts the full delay. Generation-tag callbacks so canceled ticks cannot open old text after rebind, recycle, hide or disposal. Keep at most one active hover per application interaction; opening on another root cancels the previous one.
- The show callback revalidates owner, target binding generation, XamlRoot and visibility. A pending target never reads the store; it holds only current projection data. Release content/owner references on cancel/close.
- Bound surface to 480 logical pixels wide and 320 high, clamped further to the owner XamlRoot's usable size. Wrap multiline/long-line text and allow vertical wheel/scrollbar interaction. Place adjacent to the target within the owner's bounds so the pointer can reach it without traversing unrelated rows.
- Treat owner row and hover surface as a single interaction region. Before opening, pointer exit cancels immediately. Once open, allow a short, generation-guarded pointer handoff grace (implementation target 120 ms), canceled by entry into the row/surface; leaving both closes the surface. Switching to another hover target cancels old content and starts a fresh 500 ms delay. Scrolling the content must not close it; scrolling/replacing the underlying list does close it.
- User amendment during implementation (2026-10-29): prefer placement 12 logical pixels to the **right of the current cursor**, with vertical room above/below for movement; flip left when the right side cannot fit and clamp within the owner root. Capture/update pointer coordinates without restarting the 500 ms delay. The 120 ms handoff covers the small pointer gap. During handoff, do not let an incidental neighboring-row enter destroy the current surface before the pointer has settled outside it. While a scrollbar/thumb owns pointer capture or a pointer press begun inside the surface is active, suppress exit-triggered dismissal until release/capture loss and then re-evaluate both regions. Cover capture-loss/cancel and release-outside cases in policy and UI tests.
- Use no activation/focus APIs and disable focus-on-interaction/tab stops on hover descendants. Wheel/scrollbar input is limited to hover scrolling and must not bubble into row activation, popup background focus restoration or drag handling. Verify actual focus/HWND preservation; flags alone are not proof.
- Use explicit owner XamlRoot and RequestedTheme. Do not assume a detached Popup.Child inherits RootGrid.Resources: expose a narrow appearance resolver through the existing ThemeService/PaletteResources owner, apply the current stable palette surface/border/foreground brushes explicitly, and register active surfaces for theme changes with cleanup on close. In high contrast resolve system Window/WindowText colors (`WindowSurface.SystemColor(5)` / `(8)`) together, not palette colors. Update or safely close on OS/theme/palette transitions; actual popup-layer lookup remains a runtime verification requirement. The surface must never retain a detached owner after recovery.
- A tooltip/Popup opening failure is contained and cancels the hover. Only allowlisted error/event tokens may be logged, never body/label/folder/tag text or window titles. Do not add a broad global exception handler or suppress unrelated errors.

### 4.3 Lifecycle and accelerator scope

Expose explicit cancellation by owner/root. Wire both main and popup hide/close/deactivation, page navigation/unload, narrow editor transition, row recycle/rebind, underlying list scroll, item-set replacement, and opening context menus, sort menus or dialogs. Preserve the existing unsaved-change guards: cancellation is a UI-only side effect and does not bypass guard decisions. Main cached pages need native hide cancellation even when their content stays loaded. Popup closed/recovery must cancel before an old window can survive under the new host.

Concrete lifecycle hooks: add MainWindow.Activated cancellation on Deactivated and cancel explicitly in CloseForExit, before the hide operation, and before guard awaits that may show dialogs. DialogService.ShowAsync cancels its owner's hover before opening a ContentDialog. Opening the hotkey popup cancels main-root hover even if native deactivation delivery is delayed. These are additional subscriptions/hooks, not events already provided by MainWindow.

Hook each ListView's realized internal ScrollViewer.ViewChanging/ViewChanged once its template is available, detach on unload/dispose, and cancel only for scrolling of that underlying list (not the detail surface's own ScrollViewer). Cover wheel and programmatic ScrollIntoView; do not rely on pointer wheel alone. SnippetsPage.ApplyLayout explicitly cancels when layout changes or the list pane collapses, even when the page remains loaded.

Popup native input integration: include the open hover surface's owner-coordinate rectangle in UpdateDragRegions' NonClientRegionKind.Passthrough regions and recompute on hover open/close/resize. RootGrid visual-child traversal alone cannot discover Popup-layer content. OnSurfacePointerReleased/IsEmptySurface must recognize and exclude hover-owned interaction; handled pointer/wheel routing must not refocus Search or activate a row. Verify actual native hit testing separately from XAML event flags; do not blanket-suspend unrelated popup behavior.

Inspect the page accelerator placement with the actual main list, then suppress generated Page-level accelerator tooltip placement (`KeyboardAcceleratorPlacementMode="Hidden"` if confirmed effective). Keep Ctrl+N/Ctrl+F handlers working, retain explicit button/search hints, and do not remove shortcuts or blanket-disable unrelated tooltips to hide the symptom.

### 4.4 Alternatives

| Option | Decision |
|---|---|
| Shared row + shared delayed non-focus-taking scrollable Popup | **Chosen:** consistent styling and behavior with one lifecycle policy to test. |
| Ordinary native ToolTip with longer text | Rejected for body content: current hit-test-disabled tooltip cannot be entered/scrolled and auto-dismissal competes with handoff. |
| Focus-taking Flyout/ContentDialog | Rejected: changes keyboard focus and risks closing the hotkey popup or affecting insertion target state. |
| Independent implementations for each list | Rejected: repeats the already divergent font, metadata and cancellation paths. |
| Copy body or query the store on a background hover task | Rejected: unnecessary memory copies or violation of single-threaded store access. |

### 4.5 New hot path and parallel paths

```text
Store change (UI thread) → main projection / popup dirty-cache rebuild
  → ContentText references current Snippet.Text → equality detects body change → row rebind cancels old hover
Pointer enters label/folder/tags → shared row → HoverDetailsCoordinator.Request(target, generation, text)
  → scheduler waits 500 ms → generation/owner still valid?
      no → ignore/cancel
      yes → owner-scoped HoverDetailsSurface opens without activation/focus
Pointer enters surface → handoff close canceled → wheel/scrollbar scrolls bounded content
Pointer leaves both / owner hides / row rebinds → cancel pending callbacks + close surface + release text
Page Ctrl+N → existing New command (unchanged); automatic Page hint suppressed, New button hint retained
```

Parallel paths explicitly covered: main list and hotkey popup; popup flat/grouped layouts; hidden/visible cache refresh and recovered popup; cached main-page navigation and narrow editor mode. Editors themselves, folder browse entries and unrelated toolbar tooltips are not new content-hover targets.

### 4.6 Surfaces

| Surface | Change | Task |
|---|---|---|
| Main and popup row projections/cache | Current full body reference and equality semantics | T1 |
| Shared snippet row and popup heading wrapper | Label/body target, separate smaller/subdued metadata | T3 |
| Shared hover policy and WinUI surface | Delay, handoff, scroll, safe owner placement | T2, T3 |
| Main/popup host, menus and dialog lifecycle | Cancel safely, preserve focus/recovery/guards | T4 |
| Main New shortcut/hint | Remove misplaced generated hint only | T4 |
| Tests and authorized UI harness | Behavioral policy, real-store refresh, safe UI verification | T1–T5 |
| Password backlog | Masked-display requirement, no implementation/security promise | T6 |

## 5. Impact and risk

| Question | Answer |
|---|---|
| State/crash | Hover is transient; no persistence or clipboard writes. Existing snippets are unchanged after restart. |
| Consistency/races | UI-thread projections plus generation/owner guards prevent delayed stale content. Body equality changes intentionally cause refresh only when body differs. |
| UX | Actual ordinary snippet content is visible on hover by explicit user request. Bounded scrollable surface must not interfere with selection/insertion. |
| Compatibility/migration | No domain/schema/settings migration; current snippets are not automatically made sensitive. Existing font, grouping, sort and shortcuts remain. |
| Recovery | Closing/recycling/host failure cancels and releases all text. A recovered popup receives fresh owner state; surface-open errors do not crash the application. |
| Logging/debugging | No content in logs or default row names. Use synthetic text in QA; avoid logging raw exception payloads that could contain content. |
| Edge cases | Empty metadata, unfiled/grouped snippets, whitespace-only/large/multiline bodies, same-label body edits, 10–32 font sizes, rapid target switching, resize/DPI, menu/editor transitions and system themes. |

**Risks:** Real WinUI popup hit testing/focus and root resource lookup are not proven by source or unit tests (plan 011 already exposed that gap). Hover handoff and scrollbar clicks can conflict with native popup drag/focus regions. Larger body-aware projections change cache equality; preserve usage-only stability and retain the existing synthetic 50k unit-level cache stability/performance test; desktop performance acceptance, if run, uses the safety harness's required 10k dataset. Do not conflate these evidence classes. Very large bodies may incur layout work when opened; render only the active hover and avoid up-front formatting/copies. If actual scrolling/focus behavior contradicts the approach, stop and revise that mechanism instead of substituting a non-scrollable tooltip or stealing focus.

**Rollback:** Revert this feature's projection/row/hover changes; no migrated data exists. Preserve the independent password backlog request. Do not remove unrelated plan 011 or earlier user work.

## 6. Tasks

### T1 — Current body projection and cache correctness (AC1, AC6)
- **Files:** `Presentation/ViewModels/{PopupItemViewModel,PopupViewModel,SnippetRowViewModel}.cs`; row/list/cache tests.
- **Change:** Add explicit ContentText reference; populate both projections; main equality/hash include it but not usage/timestamps. Keep Preview, search fields, grouping and ToString/privacy contracts intact.
- **Verify:** sequential Core then Presentation tests, especially existing cache/usage stability cases.
- **Tests:** Extend `PopupCacheStabilityTests.EveryStoreChangeInvalidatesAndOpenIsFreshnessBarrier` to assert ContentText equals current Snippet.Text while Preview stays empty; retain the 50k stability case. `BodyOnlyEdit_ReplacesHoverContentBeyondPreviewBoundary` for both lists; `EqualBodyAndUsageOnlyChange_RetainsRowsWhenOrderUnchanged`; real SQLite save/reload integration for both main and popup content projections. Check visible and hidden refresh paths.

### T2 — Framework-free hover state policy (AC1–AC3, AC6)
- **Files:** new `Presentation/Services/HoverDetailsCoordinator.cs`, fake scheduler and `HoverDetailsCoordinatorTests.cs`.
- **Change:** Cancellable 500 ms request, generation checks, per-target identity, bounded handoff/close policy and lifecycle invalidation, with injected UI scheduling/show/close boundaries. No threads/store or body logging.
- **Verify:** deterministic fake-clock tests; no sleeps or desktop access.
- **Tests:** Scrollbar capture suppresses exit dismissal, release outside both regions closes, pointer capture loss cancels safely, and adjacent-row transit does not destroy the open handoff. 499 ms does not open, 500 ms opens; rapid A→B restarts; canceled old ticks ignored; pointer handoff enters surface without closing; leaving both closes; content/recycle/host close cancels pending/open requests; empty text suppressed; dispose releases owner/content.

### T3 — Shared row and scrollable WinUI hover (AC1–AC4, AC6)
- **Files:** new `App/Controls/{SnippetRowDetails,HoverDetailsSurface}.xaml(.cs)`; refactor `PopupRow.xaml(.cs)` and `Pages/SnippetsPage.xaml`; `tests/SmartSnippets.Presentation.Tests/{SmartSnippets.Presentation.Tests.csproj,PopupRowFontSourceTests.cs}`; existing app theme/hover tokens and narrow ThemeService/PaletteResources appearance resolver as needed.
- **Change:** Link new `SnippetRowDetails.xaml.cs` and `HoverDetailsSurface.xaml.cs` into the test project's explicit UiContracts file list (XAML is already wildcard-copied), and migrate existing PopupRow-specific source guards to the shared files; retain behavioral tests as the primary policy oracle. Common 85% subdued folder/tag rendering; label body target; one owner-scoped bounded scrollable surface with correct palette/high-contrast styling. Remove duplicate native-tooltip timer code and its static retained-row ownership. Safe native Popup integration without input synthesis/focus calls.
- **Verify:** Windows build and Native AOT publish; source guards are supplementary only. Authorized UI test of body/metadata targets, actual scrolling and foreground/focus preservation.
- **Tests:** row binding/projection integration checks, reused-template source guard, and T5 real UI oracles for nontruncated body, independent metadata and scroll behavior.

### T4 — Host lifecycle and misplaced accelerator hint (AC5, AC6)
- **Files:** `App/MainWindow.xaml.cs`, `Pages/SnippetsPage.xaml(.cs)`, `Popup/PopupWindow.xaml.cs`, `Services/DialogService.cs` and shared hover host integration.
- **Change:** Close by root on native hide/close/deactivate, page/editor/list/menu/dialog transitions, recycle/scroll and recovery; isolate hover from row activation and caption/blank-surface focus code using §4.3's explicit native passthrough bounds and pointer-release exclusion. Add the missing main Activated/CloseForExit/pre-guard hooks, internal list ScrollViewer lifecycle handlers and ApplyLayout cancellation; do not assume unload covers native hide or pane collapse. Investigate generated Ctrl+N hint; set precise placement suppression while preserving New/Search shortcuts and button hints.
- **Verify:** lifecycle tests/source guards and authorized main/popup UI flow, including no selection/insertion/clipboard mutation.
- **Tests:** `OwnerHide_DoesNotReopenFromOldTick`, `RebindAndNavigation_ClearOldContent`; real UI asserts no page-level Ctrl+N hint over rows, correct hint on New button, Ctrl+N/Ctrl+F still invoke their original commands.

### T5 — Integrated verification and review (AC1–AC6)
- **Files:** Presentation real-store integration tests; `scripts/verify-ui-qa.ps1` and dedicated scoped helper/tests if needed; implementation review/QA evidence.
- **Change:** Add safe hover tests using only synthetic content and owned processes. Measure timing with tolerances around real UI scheduling, verify full-body scrolling and cancellation, label/metadata font sizes and contrast; preserve foreground HWND and focused element identity. Use the approved safety harness and an explicitly authorized screen slot; do not reuse an unowned app or modify global input/OS settings silently.
- **Verify:** all tests sequentially; Windows solution build and Native AOT gate; §7 matrix; architecture/spec audit and independent code review. Record unexecuted UI checks explicitly rather than declaring complete.
- **Tests:** real SQLite edit→store notification→both projections integration; per-target delay/cancel tests; actual scroll-offset/content-bottom change without focus/selection change; metadata/tag UIA font assertions; log/clipboard/insertion non-effects and owned-profile cleanup.

### T6 — Record future sensitive-content work (AC7)
- **Files:** `docs/backlog/001-password-type-snippets.md`, this plan.
- **Change:** Backlog item has been created after G1, with explicit no-current-protection warning and separate security/storage requirements discovery; no domain/masking implementation here. Record task completion/validation accurately.
- **Verify:** document links resolve and scope does not claim current masking/encryption.
- **Test/oracle:** documentation review of the recorded requirement and explicit current-content disclosure warning.

### Implementation progress (2026-10-29)

| Task | State | Evidence / remaining gate |
|---|---|---|
| T1 | Complete | Full body references/equality; real SQLite edit beyond preview/reload and equal-body/usage stability; cache freshness assertions. |
| T2 | Complete | Deterministic 500 ms/120 ms, target switching, stale ticks, capture/release, concurrent neighbor clock and placement tests. |
| T3 | Code complete; UI acceptance pending | Shared rows/surface built and Native-AOT published; actual main/popup metadata font ratios verified at 10 and 32; physical hover/scroll/HC evidence pending. |
| T4 | Code complete; UI acceptance pending | Explicit lifecycle/collection/scroll/native passthrough and accelerator placement hooks; live hint/shortcut/focus flows pending. |
| T5 | In progress | Core 432 and Presentation 554 pass; Release 0 warnings/errors; AOT artifact gate passes; three isolated no-input desktop smoke runs pass. Posted hover attempt failed to expose the surface; real-pointer authorization requested, not bypassed. Architecture/spec and three external code review rounds recorded in review 013. |
| T6 | Complete | Password backlog captures separate security requirements and explicitly disclaims current protection. |

See [implementation review and evidence](../review/013-shared-hover-implementation-review.md). No full acceptance/merge-readiness claim while desktop hover checks remain open.

## 7. Verification and manual runbook

Unit tests cover scheduling/cancel and body-aware equality. Integration tests use the real SQLite repository/store plus both view-models, mocking OS boundaries only; fake-policy and source tests cannot establish rendered behavior. Run Core and Presentation tests sequentially to avoid shared build collisions. Build/publish Windows with zero warnings and the existing native artifact gate; do not kill an unowned app to unlock output.

| # | Authorized Windows test | Required result |
|---|---|---|
| 1 | Seed different label and multiline body; hover label in main/popup, including grouped popup | No display before 500 ms; actual body, not label or 80-character preview, appears after delay. Headings have no body tooltip. |
| 2 | Hover label→folder→tags rapidly and leave before the delay; repeat after a surface is open | Fresh full delay per new target, no stale flash; individual full metadata values appear. |
| 3 | Enter long-body surface; wheel-scroll and operate scrollbar to its end | Bounded wrapping surface stays open, final synthetic line is reachable; focus/foreground and selected ID unchanged, no row click/insertion/clipboard event. |
| 4 | Edit only text after character 80, save, rehover; delete/recycle/filter rows during a pending hover | New actual body appears; deleted/old content never opens from a delayed callback. |
| 5 | Hide/close/reopen main and popup; navigate to Settings/editor, open menus/dialogs, recover popup | No orphan/late hover; normal guards/focus/menus and insertion availability remain unchanged. |
| 6 | Inspect both lists under Light/Dark palettes, selected/unselected rows, high contrast and sizes 10/14/32 | Matching 85% subdued metadata, readable selected/high-contrast pairs; actual full tags and content remain accessible. |
| 7 | Hover a main row, then New; invoke Ctrl+N/Ctrl+F | No misplaced shortcut hint on rows; New hint and both shortcut behaviors remain correct. |
| 8 | Review logs and restore owned processes/profile after QA | No snippet content/metadata/window-title payload; all harness cleanup/recovery checks succeed. |

Missing UI evidence blocks claiming full acceptance/merge readiness. OS high-contrast/theme changes, global input or a new screen region require explicit authorization and recovery consistent with `docs/guides/002-acceptance-harness-safety.md`.

## 8. Rollout

Implement the six tasks in order on the existing feature branch (T6 backlog capture is already drafted). No feature flag, migration or changes to ordinary snippet data. PR through the chosen integration branch; do not commit directly to main/develop. Existing uncommitted metadata-hover changes are the starting point and will be consolidated, not discarded. Record benchmarks against the current compact-cache behavior without silently changing its acceptance target.

## 9. Open questions

None blocking the plan. Password-type storage, masking/reveal, clipboard and export security belong to the separate backlog and require future requirements confirmation; they are not promised by this feature.

## 10. Approval log

| Date | Event | Notes |
|---|---|---|
| 2026-10-29 | Requirement brief confirmed (G1) | User confirmed actual content, bounded scrolling, independent targets, 500 ms and separate password backlog. |
| 2026-10-29 | Discovery/self-review | Read both rendering paths, equality/cache and lifecycle sites; independent read-only discovery confirmed stale-body and hidden-host hazards. Scope and AC/task coverage checked. |
| 2026-10-29 | External plan review | Requested `agy --dangerously-skip-permissions --model gemini-4-argon-low -p ...`, constrained to read-only review. Verdict revise before approval. Folded explicit caption/passthrough and focus exclusion, test-project source links, main pre-guard/deactivation/exit hooks, internal-scroll/layout cancellation, owner-resolved popup appearance, pointer capture/handoff cases and separate 50k-unit/10k-desktop evidence. No user-facing requirement changes; G2 remained pending at review. |
| 2026-10-29 | Plan approved (G2) | User approved plan 012, including the folded external review findings. |
| 2026-10-29 | Implementation started | User explicitly instructed `implement 012`. |
| 2026-10-29 | Placement amendment | User requested the surface to the right of the cursor to leave room for vertical pointer movement; use edge-aware flip/clamp and retain delay/handoff/non-focus constraints. |
| 2026-10-29 | Implementation verification/review | Core 432/Presentation 554 pass; zero-warning Release build and Native AOT gate pass. Isolated Light/Ocean, Dark/Plum/32 and Light/Forest/10 smoke passes. Three read-only agy reviews; final no blocking code defects. Physical-pointer acceptance awaits explicit input authorization; posted-message hover failure is not a PASS. See review 013. |
