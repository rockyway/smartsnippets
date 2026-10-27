# 003 — Core domain and persistence

**Date:** 2026-10-27
**Status:** Placeholder
**Master plan:** [001-master-smart-snippets-v1.md](001-master-smart-snippets-v1.md)
**Depends on:** 002

> Reservation and scope boundary only — not an implementation-ready or approved plan. It will be
> expanded with the full plan template after its dependencies are complete.

## Objective

Implement the cross-platform `SmartSnippets.Core` domain and data layer that every UI surface uses.

## Scope

- Snippet, Folder, Tag, Settings models; label auto-suggest (first nonempty tag → first nonempty line, ≤60 chars, frozen after manual edit); tag normalization (trim, drop empty, case-insensitive dedupe, keep display form).
- Validation (reject whitespace-only text/label; never modify stored text).
- SQLite schema v1 + migration runner (`schema_version`, transactional, FK on, WAL); repositories for snippets, folders (unique NOCASE names, delete → Unfiled), settings, usage (separate from `updated_at`).
- `ISnippetStore` change event + in-memory search cache; search (case-insensitive substring AND over label/text/folder/tags); sorts (last used / usage / created / updated, desc, never-used last, tie-break label then ID).
- 10k-snippet benchmark test (indicative, CI).
- Out: any UI.

## Expected completion criteria

- AC1/AC2 rules, AC3 persistence, AC6 engine, AC7 sort order covered by unit tests passing on Linux and Windows CI.
- Migration from empty DB and re-run idempotency tested; failure mid-migration rolls back.
- Benchmark recorded in the subplan.
