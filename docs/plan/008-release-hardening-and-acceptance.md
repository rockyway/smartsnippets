# 008 — Release hardening and acceptance

**Date:** 2026-10-27
**Status:** Placeholder
**Master plan:** [001-master-smart-snippets-v1.md](001-master-smart-snippets-v1.md)
**Depends on:** 003, 004, 005, 006, 007

> Reservation and scope boundary only — not an implementation-ready or approved plan. It will be
> expanded with the full plan template after its dependencies are complete.

## Objective

Produce and accept the v1 release artifact.

## Scope

- CI release job: Native AOT win-x64 publish, zero trim/AOT warnings, zipped artifact.
- Performance acceptance on the user's PC (popup ≤300 ms, search ≤150 ms @10k) with results recorded.
- Compatibility matrix executed and recorded (Notepad, VS Code, Word, Outlook, Chrome/Edge, Windows Terminal PowerShell/cmd, Teams; elevated variant).
- Master §7 cross-subplan integration checks; full wiring audit; spec-vs-implementation audit of PRD §5.
- Remove prototype-only diagnostics surfaces.
- Out: new features.

## Expected completion criteria

- AC13, AC14 met; every PRD §5 success criterion marked pass with evidence; master plan marked Complete.
