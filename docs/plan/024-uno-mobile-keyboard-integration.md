# 024 — Uno mobile keyboard integration

**Status:** Placeholder
**Master:** [017 — Uno cross-platform migration](017-master-uno-cross-platform-migration.md)
**Dependencies:** 023 complete

## Objective and scope
Ship an iOS custom keyboard extension and Android IME for snippet selection/search and native text insertion. Use the 022-proven host technology and local data-sharing mechanism, not desktop focus/clipboard injection. Implement enablement, denied access, secure-field/app restrictions, snapshot freshness, offline operation, process termination recovery and idempotent usage reconciliation. Keep native UI minimal where Uno cannot be safely hosted.

## Expected completion criteria
- Installed keyboards insert exact Unicode/multiline fixture text through the platform input APIs; no success claim where apps/fields reject the keyboard.
- App edits reach the keyboard safely; a killed/restarted app or extension cannot corrupt the library or double-count an insertion receipt.
- Copy/manual paste does not earn automatic insertion usage; uncertain dispatch is not automatically retried.
- No cross-process sharing of UI-owned store objects or unsafe concurrent SQLite connection use.
- Native AOT keyboard artifacts and app/extension composition flow tests pass on physical devices, including 10k-snippet iOS keyboard launch/peak-memory and app-plus-extension package-size gates established in 022.

Not implementation-ready or approved. Expand only after 023 completes with a concrete native-dispatch/data-exchange sequence diagram, receipt ownership/schema, lifecycle tests and privacy/runbook coverage.
