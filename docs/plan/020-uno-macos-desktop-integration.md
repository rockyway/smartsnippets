# 020 — Uno macOS desktop integration

**Status:** Placeholder
**Master:** [017 — Uno cross-platform migration](017-master-uno-cross-platform-migration.md)
**Dependencies:** 019 complete

## Objective and scope
Deliver Apple Silicon macOS desktop parity through the shared Uno UI and native adapters: configurable shortcut, original-target popup/paste, clipboard protection, menu-bar icon, background lifecycle/single ownership (including login-item launch without `--background` arguments), native startup-error presentation, login item, notifications, document access, theme/text size, hover/keyboard previews and permission guidance. No Intel builds or Windows elevation impersonation.

## Expected completion criteria
- Every Phase 1 surface has a functional macOS path with no success-returning unsupported stubs.
- Target validation, duplicate prevention, clipboard outcomes, usage and permission denial/revocation pass real-machine and shared-policy checks.
- Login-item and menu-bar lifecycle, multi-monitor placement, external beside-window hover and keyboard preview, non-activating QA sort-menu/receipt, preview/focus safety, `⌘` shortcuts, import/export and persistence pass.
- Native AOT Apple Silicon app bundle runs with required native dependencies.

Not implementation-ready or approved. Expand only after 019 completes using 018 evidence, with exact APIs/files, target-app fixtures and an explicitly authorized screen runbook.
