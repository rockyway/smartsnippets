# 014 — Overlay content hover beside the window

**Status:** In progress — code complete; authorized desktop acceptance pending
**Size:** M
**Requirement:** Confirmed in chat. Content hover in the hotkey overlay appears outside its right edge, aligned with the hovered snippet, falling back left at the screen edge. Existing folder/tag tooltips and main-list behavior remain unchanged.

## Goal and acceptance

1. Hovering a snippet label in the hotkey overlay shows its content beside the overlay's right edge, aligned with the hovered row, after the existing 500 ms delay.
2. If the right side cannot accommodate it, use the left side. Constrain size and vertical placement to the monitor work area without covering the overlay when either side has usable space.
3. Folder/tag tooltips and all main-list tooltips retain current behavior.
4. Content remains bounded and scrollable, with pointer handoff, no activation/focus change, no selection/insertion/clipboard mutation, and existing cancellation on hide, rebind, scroll and close.
5. Desktop verification not executed is explicitly reported as pending; unit/source tests are not evidence of native focus behavior.

## Current state

- `src/SmartSnippets.App/Controls/HoverDetailsSurface.xaml.cs:31` creates a root-constrained WinUI Popup. Its Open path (`:146–182`) measures and positions everything within the owner root. This cannot satisfy outside-window placement.
- `src/SmartSnippets.App/Controls/SnippetRowDetails.xaml.cs:53–62` routes content and metadata through the shared surface, identifying metadata explicitly. Both the main list and overlay consume this control.
- `src/SmartSnippets.App/Popup/PopupWindow.xaml.cs:54–55,79–80` wires shared hover lifecycle and native drag-region integration. Deactivation also cancels hover (`:305`).
- `src/SmartSnippets.App/Services/WindowPlacement.cs` already uses native monitor work areas and DPI scaling. Reuse these coordinate conventions.
- Existing `HoverDetailsPlacement` is shared with the main list; preserve its current Place behavior and add a separate beside-window policy.

## Design

Use WinUI's unconstrained Popup (`ShouldConstrainToRootBounds = false`) only for overlay content. Microsoft documents that this allows a desktop Popup to extend outside its XamlRoot. This property cannot be switched back to true after opening: create a fresh Popup when switching between constrained metadata and unconstrained content rather than mutating a previously opened unconstrained instance back.

Give the overlay an explicit owner-scoped placement context/provider, registered and removed with its lifecycle. Do not infer overlay ownership from labels, visual names or a process-global mode flag. Main-list roots never receive this context.

The context supplies the overlay screen rectangle, client origin, monitor work area and current rasterization scale. Convert screen pixels to owner-root logical coordinates once. Prefer a small gap after the overlay's right edge; flip left when necessary. When neither side fits the normal width, use the larger available side and reduce width. If neither side has usable space, suppress the external surface rather than overlap the overlay. Clamp vertical position to the work area, starting at the hovered row's top. Bound content to 480 logical pixels wide and 320 high, further limited by work area and available side space.

Keep existing policy/timers, appearance resolution, content privacy, scrolling and close hooks. External surface bounds must not be submitted as out-of-window native caption/passthrough rectangles. Preserve normal constrained metadata drag-region behavior. Cancel external hover on owner move/resize or DPI changes rather than leave stale screen coordinates.

Pointer handoff must remain possible from the hovered row through the overlay to the adjacent surface. Inspect the existing row/neighbor handoff policy and adapt only the external-content path if its short transit timeout prevents this. Entering another row must still eventually select that row's hover target; do not make the external surface permanently sticky.

No new focus-taking Window, Flyout or dialog. Do not work around native activation failures by forcing focus back to the overlay. If the unconstrained Popup changes activation or cannot support reliable handoff, stop and revise the mechanism.

### Flow

```text
Overlay label hover → shared row → shared hover surface → 500 ms policy
  → overlay context present and not metadata?
      yes → screen/work-area geometry → beside-window policy → unconstrained Popup
      no  → existing root-local placement → constrained Popup
Pointer enters detail → existing scrolling/capture policy
Owner hides/moves/rebinds or pointer leaves → cancel and release text
```

### Alternatives

- **Unconstrained WinUI Popup (chosen):** reuses current themed content and lifecycle without a new application window.
- Separate native/WinUI tool window: more ownership, activation, DPI and recovery complexity; not needed unless Popup fails verified runtime constraints.
- Expanding the overlay: changes its layout and target behavior; outside confirmed scope.

## Tasks

### T1 — Placement policy
- Add a separate beside-window placement method/result in Presentation; retain current root-local Place method.
- Test right placement, left fallback, reduced width, negative monitor origins, top/bottom clamping and no available side space.
- Verify deterministic tests, including unchanged main/metadata placement tests.

### T2 — Owner context and unconstrained surface
- Update `Controls/HoverDetailsSurface.xaml.cs` and `Popup/PopupWindow.xaml.cs` with explicit scoped context and cleanup.
- Resolve current client/screen geometry using existing native conventions; add a narrow helper in `Services/WindowPlacement.cs` if needed.
- Create/recreate constrained versus unconstrained Popup safely; maintain root/theme/owner references and cancellation.
- Exclude external bounds from overlay native drag regions; cancel on geometry changes. Inspect pointer handoff and cover any policy adjustment with tests.
- Verify metadata and main-list branches are unchanged, Windows build has zero warnings, and Native AOT publish remains clean.

### T3 — Verification
- Add focused branch/lifecycle source contracts alongside behavioral placement/policy tests. Run Core then Presentation tests sequentially.
- Build the Windows App and publish Native AOT; do not terminate unowned processes to unlock files.
- In an explicitly authorized screen/input slot, use the isolated QA harness with synthetic text: right/left monitor-edge placement, long-content scroll, row-to-surface handoff, folder/tag regression, main-list regression, hide/reopen, resize/move/DPI and focus/foreground preservation.
- Follow `docs/guides/002-acceptance-harness-safety.md`; do not launch interactive QA or synthesize input without authorization. Record pending checks rather than claiming runtime acceptance.
- Self-review architecture, scope and privacy; no snippet text or window titles in logs.

## Risk, compatibility and rollback

No schema, settings, insertion, cache or data changes. Primary risks are native Popup activation, cross-window pointer transitions, mixed-DPI coordinate conversion and stale placement after movement. Existing constrained surfaces remain the parallel fallback path for metadata/main, not a silent fallback for failed external content placement. Roll back only this external-content context/policy, preserving earlier tooltip changes and unrelated work.

## References

- [Plan 012](012-shared-snippet-hover-details.md): shared hover lifecycle, scrolling and focus contracts.
- [Popup.ShouldConstrainToRootBounds](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.primitives.popup.shouldconstraintorootbounds): unconstrained desktop Popup and mutation restriction.

## Approval

- Requirement confirmed in chat: external right-side content tooltip, left-side screen-edge fallback; folder/tag behavior explicitly retained. Main list unchanged.
- User approved the plan in chat and subsequently instructed `implement plan 014`.

## Confirmed companion-panel amendment

After reviewing the supplied screenshot, the user confirmed that external content should be a companion panel aligned with the overlay's **top edge and matching its height**, rather than following the row or shrinking to the content. Use an opaque muted neutral background with a subtle border, retaining system high-contrast colors. Folder/tag tooltips and the main list remain unchanged. This supersedes row alignment and the external surface's 320-pixel height cap above; monitor work-area clamping and left-side fallback still apply. Ordinary constrained tooltips retain their existing limits. Implementation uses fixed external Height, clears that fixed height for constrained surfaces, and scopes the neutral appearance to external content only.

Amendment verification: Presentation **577 tests pass**; Debug App build and Native AOT artifact gate pass with zero warnings/errors. An initial Debug attempt hit a transient XAML compiler file lock; retry succeeded without terminating any process. Published to a unique temporary directory. Desktop visual/focus acceptance remains pending.

### Visible-frame alignment correction

The next screenshot showed an oversized companion and excessive separation. Geometry now prefers `DWMWA_EXTENDED_FRAME_BOUNDS` over `GetWindowRect`, excluding invisible resize borders when sizing/alignment are calculated. Native window bounds remain the fallback if DWM cannot supply valid visible bounds. The side gap is reduced from 8 to **4 logical pixels**. Presentation **578 tests pass**; Debug build and Native AOT publish/artifact gate pass with zero warnings/errors. Visual verification of the correction remains pending.

## Implementation and verification

- T1 complete: separate beside-window policy, right/left selection, bounded side width, negative origins, work-area clamp and no-space suppression. Existing root-local placement is unchanged by plan 014.
- T2 code complete: explicit overlay-root provider, fresh constrained/unconstrained Popup when required, native client-to-screen/work-area conversion, actual constraint check, external bounds excluded from caption regions, owner movement/DPI/unload/close cancellation.
- Pointer handoff: independent per-target surfaces let metadata run its normal 500 ms clock while existing external content stays visible during horizontal transit through the original row. Exiting incidental metadata cancels only its pending request. Settling opens metadata and cancels content. Neighboring rows are not included in the transit route.
- T3 automated checks: Core 432 and Presentation 575 tests pass. Windows Debug build passes with zero warnings/errors; Native AOT publish/artifact gate passes using a unique temporary output directory. No app was launched and no input synthesized.
- Architecture/spec self-review: UI-thread-only scoped owner registration, immutable geometry policy in Presentation, native coordinate conversion in App, no data/schema/cache/insertion changes, no content logging or focus APIs. Main roots do not register external placement. Startup geometry events without an overlay root cannot cancel another root's hover.
- Independent read-only CLI review was attempted with a 100-second bounded plan/sandbox session; it timed out without a review result. This is not independent approval.
- **Pending:** authorized desktop evidence for actual right/left placement, native activation/foreground preservation, cross-window pointer/scrollbar handoff, metadata/main regressions and mixed-DPI/monitor-edge behavior. Unit and source checks do not establish those outcomes. Full acceptance is not claimed.
