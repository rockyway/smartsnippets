# UI QA progress — feature/ui-qa

- [x] Core tests: 426 passed; Presentation tests: 420 passed, sequentially; 0 failed or skipped.
- [x] Native AOT publish succeeded without observed warnings after changes.
- [x] Launched copied published app with supervisor/watchdog, exclusive profile lease, registry journal, owned process and profile hash comparison on restoration. These runs predated the updated top-left parking/posted-input rules; navigation used UIA selection. No further desktop runs after the new rules.
- [x] Captured empty-library dark/system-theme Snippets, Folders and Settings at requested physical window dimensions 1280×800, 900×600 and forced 500×400. The latter was forced programmatically and is not verified to be the actual minimum. Only a subset was visually examined.
- [x] Confirmed Folders Create button clipping at 900×600: `docs/qa/ui/fa61080307504c77be600de2d04ca451/folders-900x600.png`. Responsive grid fix and screenshot-based verification: `docs/qa/ui/741306280fad4ad98f2c6dba84802be9/folders-900x600.png`; commit `c36a6d1`. The UIA button-bounds assertion passed but does not prove clipping against every ancestor.
- [x] Added Settings shared confirmation dialog preview with synthetic long text and two-answer Presentation test; commit `51c16b4`. Not yet verified in real UI; required screenshot matrix not implemented.
- [ ] Update `scripts/verify-ui-qa.ps1` to park on top-left monitor twice, use posted input and foreground probes; it is deliberately disabled until then. Also cover dialog preview across themes and sizes.
- [ ] Visually inspect all captures and verify real minimum-size behavior; screenshots at 500×400 show severe clipping, but forced dimensions bypass the normal interactive sizing constraint, so do not yet classify as a supported-size defect.
- [ ] Exercise light theme, alternate text sizes, editor, tooltip, popup, tray, synthetic import/export, errors, CRUD, search, ordering and owned-target insertion.
- [ ] Fix remaining confirmed defects and run complete acceptance matrix.

One intermediate run `ed8ec46e19ef4ae28b51e82226b016d0` produced DPI-cropped screenshots due to the external screenshot driver; it is not acceptance evidence. Subsequent screenshots use DPI-aware PrintWindow. Previous claim that UI could not be run because activation was forbidden was incorrect for the user's initial authorization; updated rules now require posted input where possible.
