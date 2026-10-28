# Review: implementation readiness of plans 006–008

**Reviewed branch:** `claude/epic-allen-07hez6` at `69b5240`  
**Plans:** [006](../plan/006-popup-search-and-toolbar.md), [007](../plan/007-insertion-engine-and-elevation.md), [008](../plan/008-release-hardening-and-acceptance.md)

## Verdict

Not yet ready for implementation. The plans have detailed tasks, contracts and test inventories, but the issues below need decisions or corrections before approval. Plan 008's re-baseline after 006/007 is sensible; it should not defer decisions that affect their architecture or acceptance criteria.

## Findings

### 1. High — Clipboard restoration can silently lose data

**Plan 007, §4.1, §5 R6:** The engine empties the clipboard even when formats cannot be snapshotted or exceed the 64 MB cap, and restore failures are only logged. This conflicts with the stated clipboard-restoration guarantee; the claim that plain text always survives is not guaranteed by the cap.

**Recommendation:** Decide whether to abort before `EmptyClipboard` when preservation is incomplete, or explicitly obtain approval for potential loss and surface it to the user. Test incomplete snapshots and failed restores.

### 2. High — A failed dispatch can already have pasted

**Plan 007, §4.1 steps 9–10 and §4.4:** `SendInput` returning fewer than four events can still have sent Ctrl+V. An exception after dispatch can also become `PasteFailed`. In either case, reopening the popup as a retryable failure risks a duplicate paste and contradicts the proposed failure/usage rules.

**Recommendation:** Model not-dispatched, dispatched and uncertain outcomes separately. Specify cleanup for partially injected modifiers, safe messaging, and usage behavior for uncertain outcomes. Add tests for partial sends and exceptions after dispatch.

### 3. High — The foreground gate does not guarantee the target receives the paste

**Plan 007, §4.1 step 8 and §5 R1:** Focus can change between `IsForeground` and `SendInput`; the plan claims this eliminates wrong-window pastes. The final gate checks the HWND but not its captured PID.

**Recommendation:** Revalidate target identity as late as possible, acknowledge the remaining race, and explicitly decide whether that residual risk is acceptable rather than promising an absolute guarantee.

### 4. High — Search performance acceptance measures the wrong path

**Plan 006, §4.1 pipeline; plan 008, T11:** The release test benchmarks Core search, but the popup also filters, groups, maps rows, updates bindings and renders. The benchmark cannot establish the user-visible ≤150 ms target at 10,000 snippets.

**Recommendation:** Add an end-to-end measurement from query input to updated rendered results, in both layouts, with a defined dataset and pass threshold. Keep the Core benchmark as a diagnostic.

### 5. Medium — The planned Ctrl+Enter handler needs a WinUI-specific solution

**Plan 006, §4.1 and T8:** The plan relies on a tunneling `PreviewKeyDown` event, a WPF pattern that is not the standard WinUI 3 keyboard event model.

**Recommendation:** Specify and validate an actual WinUI-compatible accelerator or key-routing approach, including behavior in the multiline TextBox.

### 6. Medium — Release upgrade instructions conflict with the archive layout

**Plan 008, §4.1(b) and T4:** Each zip contains a version-named top folder, but the guide says to extract over a stable installation folder. Normal extraction creates a new nested version folder instead of replacing the existing executable, leaving startup registration pointing at the old version.

**Recommendation:** Specify exact extraction and copy steps and test an upgrade from one downloaded release to another, including start-at-login.

### 7. Medium — Cross-plan contract and tray precedence disagree

**Plan 006, §4.3.1 and §4.1; plan 007, §4.5 and T0; plan 008, §4.1(f):** Plans 006 and 007 name different T0 contract files (`InsertionContracts.cs` versus `IInsertionService.cs`). Plan 006 makes popup-open win over hotkey-unavailable, whereas 008 makes the fault win.

**Recommendation:** Align the shared file name and ownership, and choose one tray policy now rather than deferring a behavioral change to release hardening.

### 8. Medium — Integration scenarios conflict with popup deactivation

**Plan 006, §2; plan 008, T10 E11–E12:** Plan 008 asks to update a snippet or folder in the main window while the popup remains open, but plan 006 closes the popup on deactivation except during editing or insertion.

**Recommendation:** Define achievable integration tests that exercise store-change propagation, or change the intended deactivation behavior and acceptance criteria consistently.

## Recommended next step

Revise plan 007's dispatch and clipboard guarantees first, then align the shared contract, UI interaction, performance evidence and release procedure across all three plans. Re-review the amended plans before starting implementation.
