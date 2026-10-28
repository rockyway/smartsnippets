# UI QA progress — feature/ui-qa

- [x] Read repository instructions, safety guide, desktop automation skill and plan 010 manual runbook.
- [x] Core tests: 426 passed, 0 failed, 0 skipped.
- [x] Presentation tests: 418 passed, 0 failed, 0 skipped; run after Core.
- [x] Native AOT publish via `scripts/publish-aot.ps1 -Output publish`; no warnings observed.
- [ ] Implement and verify a non-focus-stealing UI QA driver with the existing supervisor, profile restoration, registry journal, ownership checks, watchdog and restored profile hash verification.
- [ ] Execute and visually inspect screenshots for all requested pages, themes, sizes and text sizes.
- [ ] Exercise synthetic import/export, errors, CRUD, search, ordering and owned-target insertion.
- [ ] Fix any confirmed defects, add regression coverage, re-run and commit per fix.

Blocked: the existing acceptance worker uses foreground activation, global keyboard input and activating UIA patterns, which conflict with the user's active-machine safety requirement. It must not be run unchanged. The posted-input skill warns that launching an app itself can change foreground; popup/insertion also require special care. No app was launched and no profile or registry was modified in this session. No UI acceptance is claimed.
