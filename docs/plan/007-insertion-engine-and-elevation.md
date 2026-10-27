# 007 — Insertion engine and elevation handling

**Date:** 2026-10-27
**Status:** Placeholder
**Master plan:** [001-master-smart-snippets-v1.md](001-master-smart-snippets-v1.md)
**Depends on:** 002, 003, 006

> Reservation and scope boundary only — not an implementation-ready or approved plan. It will be
> expanded with the full plan template after its dependencies are complete.

## Objective

Harden the 002 insertion spike into the production engine.

## Scope

- Target validity check (window still exists, not a Smart Snippets window, never retarget).
- Integrity-level detection; elevated target + non-elevated app → message pointing to Restart as administrator.
- Focus restore; modifier-key release; clipboard save (copyable formats) → set Unicode text (excluded from clipboard history) → Ctrl+V via SendInput → delayed restore; clipboard-busy retries.
- Double-activation guard (single in-flight insertion).
- Usage stats (count + last used) updated only on successful dispatch, in a transaction, without touching `updated_at`.
- Actionable error messages for each failure mode.
- Out: compatibility matrix execution (008).

## Expected completion criteria

- AC9, AC10 (detection) pass unit tests (engine state machine with fake Win32 boundary) and runbook on the AOT build; cancellation and failure never increment usage.
