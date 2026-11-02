# 023 — Uno mobile management

**Status:** Placeholder
**Master:** [017 — Uno cross-platform migration](017-master-uno-cross-platform-migration.md)
**Dependencies:** 022 complete

## Objective and scope
Deliver adaptive Uno iOS/Android snippet/folder management, search, editor validation/unsaved-state handling, touch/accessibility, theme/text size and local persistence. Adapt path/extension/filename-based import-export checks to document-provider descriptors/URIs with explicit cancellation/failure semantics and accurate, capability-specific replacement messages. Provide keyboard enablement/privacy guidance and only meaningful platform settings. Prepare the validated local sharing boundary for 024; no desktop hotkey/tray/startup impersonation and no cloud sync.

## Expected completion criteria
- CRUD, folders, search/sort, Copy, restart persistence and JSON transfer pass through real composition and native device UI.
- Small displays, software keyboard, navigation interruptions and app process loss do not corrupt committed data.
- Document-provider cancellation/failure preserves the library and reports publication guarantees accurately.
- Native AOT mobile release artifacts pass; clipboard/content/labels/tags/window titles, queries and user document paths do not enter published logs.
- Local projection/receipt boundaries follow 022 decisions without prematurely claiming keyboard insertion acceptance.

Not implementation-ready or approved. Expand only after 022 completes, specifying exact schemas/contracts, permission handling, files and tests.
