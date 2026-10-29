# Stability checklist

- [x] Read repository guidance, build protocol, popup/insertion/editor plans and UI QA notes; static investigation documented.
- [ ] Folder corruption: static candidate fix and regression tests added; tests are unrun and WinUI reproduction remains pending. The test saves and verifies the folder; it does not establish a no-op save.
- [ ] Audit other editor and Folders page bindings for swap feedback.
- [ ] Establish and fix popup flash; verify first frame on an authorized screen slot.
- [ ] Complete popup prewarm, cache invalidation, lifecycle and timing requirements.
- [ ] Update plan 006 with decisions and evidence.
- [ ] Request single build batch stab-01 only after code and tests are ready; no builds without grant.
- [ ] Screen pass and before/after measurements arranged separately.
