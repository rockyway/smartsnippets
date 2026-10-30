# 015 — Overlay keyboard content preview

**Status:** In progress — code complete; authorized desktop acceptance pending
**Size:** M
**Area:** Popup keyboard routing, shared preview lifecycle, Presentation interaction policy/tests
**Requirement confirmed:** In chat, including first-Backspace focus-only behavior.

## 1. Goal

Let keyboard users open the existing companion content panel for the selected hotkey-overlay snippet without moving the mouse or stealing focus.

## 2. Confirmed requirement

- After Up/Down navigates snippets, Right opens the selected snippet's content preview immediately, without moving focus.
- Further Up/Down navigation updates the open preview.
- Left closes the preview. Escape closes it before the existing search-clear/overlay-close behavior.
- Right retains ordinary search-caret behavior until Up/Down has been used to navigate results.
- When navigating results or viewing the preview, the **first Backspace only returns to the search caret without deleting**; subsequent Backspace presses edit normally.
- Mouse hover, folder/tag tooltips and the main list retain existing behavior. The preview uses plan 014's muted, top-aligned, matching-height companion panel, current visible-frame geometry and 4-logical-pixel gap.
- No new keyboard scrolling controls, main-list shortcuts, data/schema/settings changes or insertion behavior changes.

### Acceptance criteria

1. **AC1:** Before result navigation, Right/Left/Backspace retain native search editing. After Up/Down navigation, unmodified Right shows the selected snippet's current full content immediately; opening does not paste, change selection, or focus the panel.
2. **AC2:** While keyboard preview is open, Up/Down updates it to the newly selected snippet, including grouped results and transitions requiring ScrollIntoView. Headings/empty results never show stale content. Repeated Right does not toggle or restart a delay.
3. **AC3:** Left closes the keyboard preview. Escape closes the currently visible content preview first, without clearing the query or dismissing the overlay on that key gesture; subsequent Escape retains existing precedence (including context-menu dismissal).
4. **AC4:** From logical result navigation or keyboard preview, first Backspace closes the preview, returns to search at its preserved caret/selection, leaves query text unchanged, and exits navigation mode. A later Backspace performs ordinary editing. Holding the consumed first Backspace must not delete via autorepeat before release.
5. **AC5:** Search edits, owner hide/close/deactivation/recovery, editor/menu/dialog transitions, result replacement and invalid selection cannot resurrect a pinned/stale preview. Mouse-driven content/metadata still has its existing delay, handoff, scrolling, size and appearance; main-list behavior is unchanged.
6. **AC6:** Keyboard preview shares the current companion rendering/appearance and work-area fallback. Automated and desktop evidence are reported separately; no content/window-title logging or clipboard/usage/insertion changes are introduced.

## 3. Current state

- `src/SmartSnippets.App/Popup/PopupWindow.xaml:16,25` binds SearchBox one-way to query and ListView selection two-way to SelectedIndex. Keyboard navigation currently keeps actual focus in search.
- `PopupWindow.xaml.cs:80–83` flushes debounced query in PreviewKeyDown before Up/Down/Enter, but does not route Right/Left/Backspace there. A bubbling handler is insufficient for keys consumed by TextBox native editing.
- `PopupWindow.xaml.cs:371–389` implements Escape/menu precedence and Up/Down selection from search. There is no logical search-versus-result navigation state.
- `PopupWindow.xaml.cs:512–521` scrolls selection into view; `:523–559` debounces search for 120 ms. Keyboard preview must use the selected projection after flushing pending query, not stale SearchBox results.
- `src/SmartSnippets.Presentation/ViewModels/PopupViewModel.cs:107–125` validates selected indices, skips headings and bounds Up/Down navigation. Reuse this selection policy.
- `src/SmartSnippets.App/Controls/HoverDetailsSurface.xaml.cs:15–29,67–118` owns active-surface arbitration, external owner contexts and collection/list-scroll cancellation. Row recycling also cancels surfaces through PopupWindow. A selected row can be unrealized; binding a persistent keyboard preview to that row would be fragile.
- `HoverDetailsSurface.xaml.cs:126–160,164–242` only exposes delayed pointer requests; the reusable Open path already renders the external companion without focus APIs.
- `src/SmartSnippets.Presentation/Services/HoverDetailsCoordinator.cs:16–28,48–60` requires pointer entry and dismisses on pointer exit. A keyboard preview cannot simply fake pointer entry and use its 500 ms timer.
- Parallel consumers: `Controls/SnippetRowDetails.xaml.cs` provides hover targets to both `Controls/PopupRow.xaml` and `Pages/SnippetsPage.xaml`. Only the overlay gains keyboard preview; preserve shared constrained metadata/main paths and plan 014's external placement.

Paths without a project prefix above are relative to `src/SmartSnippets.App`.

## 4. Design

### 4.1 Keyboard interaction policy

Add a framework-free `PopupKeyboardPreviewPolicy` in Presentation. Model logical Search, Navigating and Preview states independently of actual XAML focus. Keep snippet text out of policy state; use selected IDs and explicit effects (show/update/close/return-search/handled). Up/Down arms navigation even at a list boundary if a valid snippet is selected. With no valid result, do not open a preview.

Route plain Right/Left/Backspace and preview Escape in the root PreviewKeyDown path **before TextBox default editing**. Restrict interception to search/result navigation, not editor, toolbar, menu or footer interactions. Preserve modified caret/selection shortcuts (Ctrl/Shift/Alt), text composition, and Enter insertion. Existing focus calls must continue through the popup's coordinator; no native activation or panel focus workaround.

Right in Navigating/Preview flushes pending query and revalidates the selected projection, then requests immediate content display. Left closes preview but leaves result navigation armed. Escape first dismisses an active context menu; otherwise it closes a visible content panel before existing query-clear/overlay-close handling. Backspace from Navigating/Preview consumes that physical key gesture, closes preview, exits navigation mode and restores search caret/selection; ignore its autorepeats until key release. Do not synthesize a replacement Backspace. In Search, native editing remains untouched.

Any actual search edit or intentional click/caret edit in search exits logical navigation and closes keyboard preview. Preserve the existing caret/selection when Up/Down starts navigation. Filter changes invalidate pending preview generations before result refresh.

### 4.2 Immediate preview and ownership

Reuse HoverDetailsSurface's appearance, layout and popup-open implementation with an explicit keyboard request mode. The keyboard-owned surface belongs to a stable overlay root, not a realized row; pass the current text-size service/font explicitly. Show immediately and keep it open independently of pointer exit. At most one hover/keyboard detail surface is visible. Cancel pending pointer timers before keyboard open so a stationary pointer cannot later overwrite it.

Do not add a second, divergent panel implementation. Expose narrow open/update/close operations and an actual-close notification so the logical policy cannot believe an unavailable/failed panel remains visible. Geometry/open failure closes safely, without activation or stale state.

On a genuine new mouse hover request, return to the existing mouse interaction path and release the keyboard pin; a wheel/scrollbar operation inside the panel continues to scroll without changing focus. Pointer-free keyboard navigation must not be canceled by synthetic pointer-exit assumptions.

### 4.3 Selection scrolling and cancellation

Distinguish keyboard-preview ownership from row-hover ownership in list scroll/recycle cancellation. Programmatic selection ScrollIntoView and unrelated row realization must not destroy a root-owned preview. Selected projection refresh updates the root-owned surface directly; no UIA invocation, pointer synthesis, background store access or dependency on ContainerFromItem.

Manual underlying-list scrolling may close the keyboard panel; navigation stays armed so Right can reopen. Root hide/close/deactivate, result-set replacement, invalid selection, menus/dialogs/editor entry and geometry/theme cancellation reset preview state and invalidate queued updates. All pending work is generation/owner checked. Extend existing cancellation entry points explicitly rather than weakening CancelActive globally. Owner disposal removes subscriptions and content references.

### 4.4 Alternatives

| Option | Decision |
|---|---|
| Explicit keyboard mode reusing the existing surface | Chosen: immediate display, stable owner and shared rendering. |
| Synthesize hover on the selected row | Rejected: delay, pointer-exit dismissal and virtualization hazards. |
| Move focus into a new panel/window | Rejected: changes insertion/focus semantics and duplicates appearance/lifecycle. |

### 4.5 Flow and surfaces

```text
Up/Down → existing query flush + MoveSelection → policy Navigating
Right in Navigating → flush/revalidate selection → immediate root-owned companion preview
Up/Down in Preview → MoveSelection → current projection → update same companion
Left/Escape → close preview → existing Escape behavior on a later gesture
Backspace in Navigating/Preview → consume gesture → close + Search focus/caret, no deletion
Next Backspace in Search → native TextBox edit → existing query debounce
Mouse hover → existing delayed row surface (keyboard pin released)
Hide/result replacement/menu/editor → invalidate generation + close/reset
```

Surfaces changed: overlay root keyboard/search/selection routing; shared surface's explicit keyboard mode and lifecycle hooks; framework-free policy and tests. Main list, folder/tag hover behavior, external placement, insertion contracts and store/cache formats remain unchanged.

## 5. Impact and risks

| Concern | Treatment |
|---|---|
| State/crash | Transient window-owned state only; reopening starts in Search. |
| Consistency/races | Selected ID/current projection and generations reject stale callbacks; flush debounced query before actions. |
| UX | Preserve actual focus; Backspace consumes only its first navigation gesture. Do not hijack ordinary caret or modified shortcuts. |
| Compatibility | No data/settings migrations; Enter and existing context/menu/editor behavior retained. |
| Recovery | Open failure or owner recovery closes/reset state; no orphan root/row references. |
| Logging | Only existing content-free events; no snippet text, titles or search text. |
| Edge cases | Empty/grouped results, first/last items, held keys, pending query, IME, unrealized rows, scroll/rebind and panel geometry failure. |

Main risks are native TextBox handling before bubbling, caret preservation despite logical navigation, and list scroll cancellation fighting preview updates. Unit policy tests do not prove WinUI routing or native activation. If shared hover behavior must change beyond explicit keyboard ownership, stop and re-confirm scope.

**Rollback:** Revert keyboard policy/routing and immediate-mode integration, retaining plan 014's companion panel and all unrelated work.

## 6. Tasks

### T1 — Deterministic keyboard policy (AC1–AC5)
- **Files:** new `Presentation/Services/PopupKeyboardPreviewPolicy.cs`, `tests/SmartSnippets.Presentation.Tests/PopupKeyboardPreviewPolicyTests.cs`.
- **Change:** Search/navigation/preview transitions, valid-selection effects, Escape/Left precedence, Backspace focus-only gesture and reset semantics.
- **Verify:** fake-effect tests, no sleeps or desktop.
- **Oracles:** RightBeforeNavigationEditsSearch; NavigateThenRightShowsImmediately; NavigateWhilePreviewUpdates; BackspaceReturnsWithoutDeletionThenEdits; HeldFirstBackspaceNeverDeletes; InvalidSelectionAndResetRejectStaleUpdates.

### T2 — Reusable immediate root-owned preview (AC1, AC2, AC5, AC6)
- **Files:** `App/Controls/HoverDetailsSurface.xaml.cs`, `Presentation/Services/HoverDetailsCoordinator.cs` only if an explicit reusable ownership seam is needed; associated hover tests.
- **Change:** keyboard request mode, stable root/font/content input, pin independent of pointer exit, active-surface arbitration and close notification. Preserve pointer timing/capture and constrained surfaces. Scope row-recycle/manual-scroll versus programmatic-navigation cancellation without weakening host cleanup.
- **Verify:** existing hover/placement suites plus deterministic mode tests and supplementary source contracts; Windows compile.
- **Oracles:** KeyboardOpenCancelsOldHoverTicks; KeyboardPinIgnoresPointerExit; NewMouseHoverReturnsToDelayedMode; HostCloseCancelsBothModes; SelectionScrollDoesNotLoseRootOwnedPreview.

### T3 — Overlay keyboard/search/selection wiring (AC1–AC6)
- **Files:** `App/Popup/PopupWindow.xaml.cs`, `.xaml` for key-up/search-pointer hooks if necessary; `Presentation/Text/UiText.cs` only if updating the existing popup shortcut hint.
- **Change:** preview key routing and release/repeat tracking, caret snapshot/restore through existing coordinator, selection-update integration, and reset at show/hide/menu/editor/search/result transitions. Handle current ContentText without store reads or realized-row dependence.
- **Verify:** integration tests using the real PopupViewModel and store with fake host/input effects; source guards verify WinUI subscription/routing but are not runtime proof.
- **Oracles:** DebouncedQueryThenRightUsesFreshSelection; GroupedNavigationUpdatesCurrentBody; BackspaceRoundTripPreservesQueryAndCaret; CloseReopenDoesNotReopenOldPreview. Preserve existing Enter/preview-mode non-insertion tests.

### T4 — Verification and audit (AC1–AC6)
- **Files:** tests and implementation evidence in this plan/review document; test project source-copy list only when needed.
- **Change:** run Core then Presentation tests sequentially, Windows zero-warning build and Native AOT artifact gate to a unique output. Architecture/spec audit of all keyboard entry/reset paths and shared hover consumers; bounded read-only independent review if available, recording any incomplete result honestly.
- **Verify:** authorized desktop runbook below; no unowned process termination or unsolicited screen/input use.
- **Oracles:** actual caret/text/focus/selection/clipboard checks, not solely unit/source assertions.

## 7. Verification runbook

Follow `docs/guides/002-acceptance-harness-safety.md`, isolated synthetic profile and explicitly authorized screen/input slot.

1. Type search text and position caret mid-string. Before Up/Down, Right/Left/Backspace edit normally.
2. Navigate Up/Down, press Right: current full content opens immediately in the existing companion panel; focused element and foreground HWND do not change.
3. Navigate across grouped rows and beyond the visible list: preview updates correctly without a late stale panel or focus change. Repeated Right is idempotent.
4. Press Left: panel closes. Right reopens. Escape closes panel first; subsequent Escape clears search then closes as before. Context menu Escape still dismisses the menu first.
5. From navigation and from preview separately, press Backspace: search caret returns, query and selection range unchanged. Hold that first key: no deletion. Release and press again: ordinary deletion occurs.
6. Type, click search, filter to no results, open editor/menu, hide/reopen and replace edited content: no stale preview/pinned mode survives. Editor/toolbar/footer shortcuts and Enter insertion behavior remain unchanged.
7. Hover content/folder/tags in overlay and main list; verify 500 ms delay, handoff, scrolling and appearance remain correct. Confirm no clipboard/usage/insertion effects from preview actions.

Unexecuted desktop checks remain pending and block full acceptance claims. No new screen authorization is implied by plan approval.

## 8. Rollout and open questions

Ship policy, root-owned rendering and routing together; no migration or feature flag. Use the existing feature branch/PR workflow and preserve unrelated working-tree changes. No blocking requirement questions remain; implementation details above are subject to plan approval.

## 9. Approval log

- User confirmed keyboard Right preview, Up/Down updates, Left/Escape dismissal and normal Right caret behavior before navigation.
- User specified first Backspace only returns to search without deletion; subsequent presses edit normally.
- User approved plan 015 in chat, then instructed `implement plan 015`.
- User subsequently requested footer hints for the new shortcuts, then requested a brief single row. Final hint: `↑↓ move · → preview · ← hide · ⌫ search · Enter insert · Esc close`, with no wrapping and ellipsis only if space is insufficient. Presentation 599 tests pass and Debug build passes with zero warnings/errors after retrying a transient XAML compiler file lock without terminating any process.

## 10. Implementation evidence

- T1: framework-free logical navigation policy, immediate/idempotent preview effects, caret snapshot, held Backspace/Escape consumption, actual-close and reset behavior. Deterministic tests pass.
- T2: shared surface supports immediate pinned keyboard requests on a stable overlay root, explicit current font, pending-hover cancellation, native-close notification and pointer takeover. Programmatic list scroll/recycle cancels row hovers only; host/collection cancellation still closes all modes. Manual underlying-list wheel closes keyboard preview; panel scrolling remains isolated.
- T3: PreviewKeyDown intercepts plain navigation keys before TextBox edits, tracks releases even when handled, excludes menus/editor/IME/modified shortcuts, restores caret through existing focus coordinator, and updates selected projection content without realized-row dependence. Host/menu/search/result/geometry resets and disposal are wired. Real SQLite/view-model/policy integration tests cover grouped/current-body selection and query/Backspace contracts; these do not exercise WinUI input itself.
- T4: Core **432** and Presentation **599** tests pass (including footer hint coverage). Final Debug App build has **zero warnings/errors**. Native AOT publish/artifact gate passed before the subsequent footer-only amendment, to a unique temporary output directory. No desktop app was launched or input synthesized.
- Architecture/spec self-review: no insertion/store/schema changes; no focus API in the shared panel; keyboard navigation uses the existing popup coordinator for return-to-search; main/metadata pointer paths remain delayed. Logical state is reset independently of actual TextBox focus, and keyboard surface ownership does not depend on row virtualization.
- Bounded read-only independent review was attempted via plan/sandbox CLI. It read implementation files but timed out after 100 seconds before returning a verdict; this is not independent approval.
- **Pending:** authorized desktop checks for actual native TextBox caret/IME/repeat routing, cross-window activation/focus, scrolling/virtualization and unchanged hover behavior. Full acceptance and merge readiness are not claimed.
