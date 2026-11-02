# 021 — Uno desktop acceptance and cutover

**Status:** Placeholder
**Master:** [017 — Uno cross-platform migration](017-master-uno-cross-platform-migration.md)
**Dependencies:** 020 complete

## Objective and scope
Gate Phase 1 release on Windows x64/Apple Silicon macOS parity, strict Native AOT, performance, permissions, clipboard/insertion safety, signing/notarization, CI and reproducible installation. Create an explicit pre-cutover backup independent of the migration system's conditional `.bak`, and validate Windows upgrade/profile/settings/startup compatibility and rollback before replacing the legacy App head. Update requirements, repository instructions and release guides to the accepted platform matrix without rewriting historical approval records.

## Expected completion criteria
- Approved desktop feature/target-app matrix and synthetic privacy checks pass on fresh release artifacts.
- Windows update and rollback preserve original data/identity; new macOS install and relaunch pass.
- Search ≤150 ms on 10k fixtures and popup ≤300 ms on agreed recorded reference hardware.
- Native dependencies, executable identities, supported OS floors, x64/arm64 architecture and signatures are verified; macOS notarization passes.
- CI, sequential test execution and safety runbooks match the new production targets.
- Final task audits all desktop producer/composition/caller/UI/native lifecycle edges and parallel Insert/Copy/preview paths; the Windows check is a working daily flow plus a live check, not native-acceptance receipts (Plan 019 §0, 2026-11-01).

Not implementation-ready or approved. Expand only after 020 completes. Mobile remains pending after this release gate.
