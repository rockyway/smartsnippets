# UI QA progress — feature/ui-qa

Build slot batch: Core 426/426, Presentation 422/422, one AOT publish succeeded with `DOTNET_PROCESSOR_COUNT=4` and MSBuild `-m:4`; 0 warnings observed. No further builds without a new slot.

BUILD_BATCH_DONE

Screen slot started 2026-10-28 17:23 -0500; DISPLAY9 work area only, posted input only, no Rephlo access, no builds. Deadline approximately 18:03 -0500. Screen runs stopped at approximately 17:30; profile/hash cleanup completed and no owned app remains.

SCREEN_BATCH_DONE

- [x] Core tests: 426 passed; Presentation tests: 422 passed, sequentially; 0 failed or skipped.
- [x] Native AOT publish succeeded without observed warnings after changes.
- [x] Launched copied published app with supervisor/watchdog, exclusive profile lease, registry journal, owned process and profile hash comparison on restoration. These runs predated the updated top-left parking/posted-input rules; navigation used UIA selection. No further desktop runs after the new rules.
- [x] Captured empty-library dark/system-theme Snippets, Folders and Settings at requested physical window dimensions 1280×800, 900×600 and forced 500×400. The latter was forced programmatically and is not verified to be the actual minimum. Only a subset was visually examined.
- [x] Confirmed Folders Create button clipping at 900×600: `docs/qa/ui/fa61080307504c77be600de2d04ca451/folders-900x600.png`. Responsive grid fix and screenshot-based verification: `docs/qa/ui/741306280fad4ad98f2c6dba84802be9/folders-900x600.png`; commit `c36a6d1`. The UIA button-bounds assertion passed but does not prove clipping against every ancestor.
- [x] Reverted the mistaken modal preview (`5768461`). Added target-free real popup preview (`a54ad29`): selecting a row/Enter closes without calling insertion or clipboard; tests cover both routes, usage, normal mode and editor target preservation. Presentation tests: 422 passed. Real popup preview is not yet UI-verified.
- [ ] Complete `scripts/verify-ui-qa.ps1`: with DISPLAY9 positioning and non-activating show, posted clicks targeted the owned DesktopChildSiteBridge, and posted Tab/Down/Enter were also tried; Folders did not open. Read-back checks failed rather than claiming success. Foreground probes remained unchanged for these posted attempts. The pure LPARAM/ownership checks passed, but do not prove actual input delivery. Popup preview, other page and flow automation remain blocked by navigation delivery.
- [x] Inspected synthetic populated Snippets at 1280×800 and 900×600 in light (`docs/qa/ui/e7565b84a44a4d39b1d11576aafd9cbc/`) and dark (`docs/qa/ui/570d68c55a6c4bd0b621ee6e99e56aee/`): layout and toolbar are visible, no overlap observed at these settings.
- [ ] **Confirmed text-size defect:** seeded and independently read back Custom 10 and Custom 32, but the synthetic label's UIA bounding height remains 19 physical pixels at both settings and both sizes. Visually inspected `docs/qa/ui/fadad64f0f93472495065c71e9a10b55/` (10) and `docs/qa/ui/7cb872af901b4514bb10746eb978e94b/` (32). This violates consistent snippet-list sizing; root cause and fix not verified, no fix claimed.
- [x] Inspected other captures: `docs/qa/ui/821f67864bb245079cf3eebedae01dac/` (empty dark), `docs/qa/ui/3adcd6ac03314e96b522fcbfcc9586e4/` (light 32), `docs/qa/ui/bdfe4327464c41bbae2002c1eed2b608/` (dark 32). All captures were made on DISPLAY9 after bounds checks.
- [ ] Visually inspect all captures and verify real minimum-size behavior; screenshots at 500×400 show severe clipping, but forced dimensions bypass the normal interactive sizing constraint, so do not yet classify as a supported-size defect.
- [ ] Exercise other pages and controls across themes/text sizes, editor, tooltip, popup, tray, synthetic import/export, errors, CRUD, search, ordering and owned-target insertion.
- [ ] Fix remaining confirmed defects and run complete acceptance matrix.

One intermediate run `ed8ec46e19ef4ae28b51e82226b016d0` produced DPI-cropped screenshots due to the external screenshot driver; it is not acceptance evidence. Subsequent screenshots use DPI-aware PrintWindow. Previous claim that UI could not be run because activation was forbidden was incorrect for the user's initial authorization; updated rules now require posted input where possible.
