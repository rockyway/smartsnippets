# 019 — Uno shared shell and platform contracts

**Status:** Placeholder
**Master:** [017 — Uno cross-platform migration](017-master-uno-cross-platform-migration.md)
**Dependencies:** 018 complete

## Objective and scope
Build the production single multi-targeted Uno shell and port the **unpackaged** Windows head (`WindowsPackageType=None`) after refreshing discovery against 018 and receiving an explicit user decision on any early mobile AOT/keyboard-host blocker. Reuse Core/Presentation, adapt target identity, shortcuts, clipboard/focus policy, paths, document access and capability-driven UI without weakening Windows behavior. Preserve the existing profile, settings, activation, popup recovery and UI-thread store ownership. Keep the legacy executable as a cutover reference.

## Expected completion criteria
- New Windows-native Uno head exercises management, popup, both insertion entry paths, previews, theme/palette, settings and import/export.
- Existing data/settings and native Windows contracts pass compatibility tests; Core/Presentation remain platform-UI independent.
- Native AOT publishes and real composition-root flow tests pass; provisional Windows/macOS AOT feature-branch/PR CI gates begin here rather than waiting for cutover.
- macOS interfaces/shell are ready for 020 without declaring native parity; shortcut defaults/validation, Ctrl/⌘ hints, elevation capability, document descriptors, sanitized error logging (including internal `Migrator.cs:59,88` and `SnippetsPageViewModel.cs:292,334` exception-message paths) and current startup-failure UI are explicitly addressed.

Not implementation-ready or approved. Expand only after 018 completes; record exact APIs/files, integration tests, rollback and wiring checks then.
