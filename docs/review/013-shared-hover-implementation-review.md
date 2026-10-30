# 013 — Shared hover implementation review

**Date:** 2026-10-29
**Plan:** [012](../plan/012-shared-snippet-hover-details.md)
**Branch:** `feature/popup-metadata-tooltips`
**Verdict:** No outstanding blocking code findings from final independent audit; **desktop hover acceptance is not complete and merge readiness is not claimed**.

## Independent review

Ran three read-only implementation reviews using the user-requested command form:

```text
agy --dangerously-skip-permissions --model gemini-4-argon-low -p <read-only review instructions>
```

Reviewers were prohibited from editing, launching apps, accessing user profiles/logs or driving desktop input. They reviewed the modified and new files, not only tracked diffs. Final response: “Zero remaining blocking code defects found.” This is code-review evidence, not runtime proof.

Findings fixed across rounds:
- Separate target dwell from row/surface handoff; same-row switching restarts 500 ms rather than dropping requests.
- New neighboring target's 500 ms clock runs concurrently with the old surface's 120 ms handoff, not after it; removed a test-only transit parameter.
- Body-aware equality and real SQLite edit/reload/usage/equal-string stability checks for both projections.
- Owner-root coordinates for both pointer placement and popup native passthrough rectangles.
- Resolve high-contrast Window/WindowText together, cancel on appearance transitions, subscribe/detach live font changes.
- Cancel on main native hide/deactivation/exit/pre-guard, dialogs, page/layout, menus, popup recovery, container changes, list scrolling and ObservableCollection mutations.
- Stop wheel chaining and re-evaluate actual pointer regions on scrollbar release/capture loss; reset scroll position on reopening.
- Release text/target references and stop pending timers when canceled; revalidate host visibility before opening.

## Architecture audit

Dependency direction remains App → Presentation → Core. Scheduling/state/placement policy is framework-free; XAML/Win32 are App-only. Projection creation and hover run on the UI thread, without hover-time store reads or background store access. Actual content is an immutable string reference, not a copied body or domain object. No schema/settings migration, insertion-contract, clipboard or search behavior change. Compiled bindings and dependency properties publish under Native AOT. Narrator row names and ToString exclude body text. The only added app log event is the content-free `hover_surface_failed` token.

## Spec audit

- AC1/AC3: Independent label→body, folder→full folder, tags→all tags targets, 500 ms dwell, cancellation and switching implemented; deterministic policy and projection tests pass. Physical pointer delivery still needs validation.
- AC2: Bounded scrollable surface, handoff/capture policy and cursor-right/left-fallback/clamped placement implemented. Actual wheel/thumb/focus behavior is not yet proven.
- AC4: Both lists use the same row control. Actual UIA TextPattern confirms 85% metadata at sizes 10 and 32 in both lists. High-contrast behavior is implemented but not desktop-tested; no OS setting was changed.
- AC5: Page accelerator placement hidden; explicit New tooltip and Ctrl+N/F handlers retained. Actual hover hint/shortcut interaction remains in desktop acceptance.
- AC6: Real-store freshness, stale-tick, usage/equal-body stability, lifecycle hooks and capture cancellation tests pass. End-to-end focus/selection/clipboard/non-insertion proof is pending.
- AC7: [Password backlog](../backlog/001-password-type-snippets.md) explicitly records no current protection; no masking/security implementation added.

## Executed verification

- Core: **432 passed**.
- Presentation: **554 passed**.
- Release solution build: **0 warnings, 0 errors**.
- Native AOT publish: succeeds; exe and e_sqlite3.dll present, App.dll absent.
- `git diff --check`: clean (Git emits line-ending normalization notices only).
- Isolated no-input desktop smoke, all with owned processes, profile hash restoration and watchdog:
  - Light/Ocean/default size, screenshots `docs/qa/ui/34920e7ffd3040d0880247adf7dc1b2f/`.
  - Dark/Plum/32, screenshots `docs/qa/ui/87acc9fd3de848879f477623430005f4/`; main and popup label 32, metadata approximately 27.2.
  - Light/Forest/10, screenshots `docs/qa/ui/75903c9b98a64e3caea57a3b50d1c5ae/`; main and popup label 10, metadata 8.5.
  - Each covers 1280×800, 900×600 and 640×480, main pages and populated/search/empty popup, with palette surface and presenter checks.

Screenshots remain untracked and are not automatically staged with unrelated existing captures.

## Desktop hover gate

Added `-HoverDetails` with scoped posted mouse move/wheel tests and synthetic 80-line content. The owned app starts, but the posted-move attempt did not expose a hover surface. **This failed attempt is not counted as a passing hover test or proof of its root cause.** Posted legacy messages are not a reliable substitute for real WinUI pointer routing. No global cursor/input workaround was silently used.

Requested explicit authorization to temporarily move the real cursor and scroll over the owned test app in the previously authorized screen slot, restoring the cursor afterward. Awaiting that authorization. Physical hover timing/handoff/scrollbar/focus, rapid switching/grouped headings, live lifecycle flows, New hint/shortcuts and high-contrast acceptance remain open. No high-contrast or OS settings were changed. The full plan must not be marked complete until the applicable desktop acceptance evidence is collected or the user explicitly revises its gate.
