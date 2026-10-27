# 005 — Main window: snippet and folder management

**Date:** 2026-10-27
**Status:** Placeholder
**Master plan:** [001-master-smart-snippets-v1.md](001-master-smart-snippets-v1.md)
**Depends on:** 003, 004

> Reservation and scope boundary only — not an implementation-ready or approved plan. It will be
> expanded with the full plan template after its dependencies are complete.

## Objective

Build the management interface: snippet list/search/editor and the dedicated folder screen.

## Scope

- Snippet list with search; create/view/edit/delete; editor for text (multiline), label (auto-suggest/freeze), folder picker, tags (comma input).
- Inline “new folder” from the editor, auto-selected; delete confirmation; unsaved-changes warning; save-failure keeps editor content.
- Folders screen: create/list/rename/delete with Unfiled behaviour and confirmation.
- Reusable snippet editor component (also hosted by the popup in 006).
- Out: popup, insertion.

## Expected completion criteria

- AC1, AC2 pass view-model tests and runbook steps; rename/delete reflect immediately via `ISnippetStore` change event; text size and theme respected without clipping.
