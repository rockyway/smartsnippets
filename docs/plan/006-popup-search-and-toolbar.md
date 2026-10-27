# 006 — Popup: search, layouts, sorts, and toolbar

**Date:** 2026-10-27
**Status:** Placeholder
**Master plan:** [001-master-smart-snippets-v1.md](001-master-smart-snippets-v1.md)
**Depends on:** 003, 004, 005

> Reservation and scope boundary only — not an implementation-ready or approved plan. It will be
> expanded with the full plan template after its dependencies are complete.

## Objective

Build the global popup the hotkey opens.

## Scope

- Capture target before activation; centre on the target monitor's work area, clamp to bounds; focus search box.
- List showing Label, Folder, Tags; selection visuals; flat and folder layouts (headings non-selectable, Unfiled group).
- Live search with empty state; sort menu; layout and sort persisted.
- Keyboard: Up/Down, Enter, Esc, Tab/Shift+Tab; mouse activation = Enter.
- Toolbar (icon buttons + tooltips + automation names): layout, sort, add snippet (editor without dismissing popup, preserves target, refresh + select + clear search if hidden, cancel keeps state), open main window.
- Hands the selected snippet + target to the insertion engine (007 interface; 002 spike until then).
- Out: production insertion internals.

## Expected completion criteria

- AC4 (open/position/focus), AC5, AC6 (UI), AC7, AC8 pass view-model tests and runbook; popup opens ≤300 ms indicatively on the user's PC.
