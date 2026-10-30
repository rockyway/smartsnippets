# 025 — Uno mobile acceptance and final wiring audit

**Status:** Placeholder
**Master:** [017 — Uno cross-platform migration](017-master-uno-cross-platform-migration.md)
**Dependencies:** 024 complete

## Objective and scope
Gate Phase 2 release on physical-device management/keyboard flows, platform restrictions, Native AOT/signing/packaging, data safety, offline operation and documented distribution. Audit the entire Windows/macOS/iOS/Android implementation chain and update the master only after overall acceptance passes.

## Expected completion criteria
- iOS/Android management, keyboard enablement/insertion, provider import/export, permission failures, process termination and usage reconciliation pass the agreed device/target-field matrix.
- Signed AOT app/extension artifacts are independently verified; CI/release documentation reproduces builds and distinguishes device/simulator evidence.
- No content/label/tag/title/query/user-document-path leakage in synthetic app/extension fixture logs; no required network service or unexpected permission dependency.
- Desktop regression suites and accepted compatibility evidence remain valid after shared mobile changes.
- Last task traces producer → shared contract → native registration/composition → user-visible caller/binding → persistence/usage/recovery for every new boundary across 018–024, including Copy versus Insert and target-free previews; orphan wiring or unexecuted acceptance blocks completion.

Not implementation-ready or approved. Expand only after 024 completes; master completion requires both phase gates, not mobile build success alone.
