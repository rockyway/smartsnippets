# 022 — Uno mobile technical feasibility

**Status:** Placeholder
**Master:** [017 — Uno cross-platform migration](017-master-uno-cross-platform-migration.md)
**Dependencies:** 021 complete; 018 contains an early strict-AOT/keyboard-host risk check before 019 production adoption

## Objective and scope
Prove iOS/Android management-app and native keyboard-extension/IME feasibility before broad mobile work. Validate strict Native AOT per artifact (not silently substituting ordinary mobile AOT), SQLite/native dependencies, extension/service packaging, physical-device input, local app/keyboard data exchange, permission restrictions and document-provider import/export. Do not assume Uno XAML embeds directly in keyboard hosts.

## Expected completion criteria
- Reproducibly installed AOT management app and minimal keyboard integration on physical iOS/Android devices, or explicit blockers requiring user decisions.
- Native text proxy/InputConnection insertion, unsupported fields/app opt-out and process termination behavior recorded.
- Shared container/projection and deduplicated usage-receipt feasibility demonstrated without concurrent use of a UI-owned store connection.
- Full Access/App Group sharing/enablement implications on iOS and Android network-permission needs documented independently; no network access introduced for a nonexistent sync feature.
- OS floors, CPU/RIDs, signing/release channel, native extension UI technology and provider publication guarantees proposed for approval before 023/024. Measure iOS keyboard extension launch/peak memory and app-plus-extension package size on a 10k-snippet synthetic library; a management-app-only build does not pass.

Not implementation-ready or approved. Expand only after 021 completes; failure blocks mobile work rather than reducing confirmed scope silently.
