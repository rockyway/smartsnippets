# 024 — Windows native preview row delivery

Status: Windows native row event verified, partial Plan 018 T3. Not Mac native pointer acceptance, external-hover pointer proof, insertion or overall feasibility completion.

## Authorized native run

- Native AOT build: `51e5f7715c2c466cafbd2bf3e0639e1d`, zero-warning collector BUILD_CHECKS_ONLY.
- Native entry SHA256: `352a6cb8cc7c6b9de3d23ca1f45a23ed8b2315b841efd4053d479de715e3dadc`.
- Runtime private token: `ss-uno018-ui-kgeb18nj`.
- Result: owned window observed; `ui_preview_row_native_result=PASS graceful_exit=true`; `mouse_recovery=COMPLETE`; clipboard untouched.

`run_ui_smoke.py --preview-row-native` launches only its hash-pinned owned Windows probe. After coordinator readiness/uncloaking, the child exports the exact popup HWND and an allowlisted automation ID on the realized selected ListViewItem. A separately bounded helper verifies live parent/image/hash, reads only that owned UIA row's bounds, maps normalized bounds into PMv2 physical coordinates, and posts one guarded mouse down/up pair. Windows main and popup stay on the bottom-left monitor.

The actual ListView ItemClick count must be exactly one, then the shared row method calls real PopupViewModel.InsertRowAsync in target-free preview mode. The popup hides; real SQLite reload verifies unchanged content/all metadata/zero usage, forbidden service boundaries remain zero, and clipboard epoch remains unchanged. Both native windows close and the process exits naturally. No keyboard or clipboard operation is part of this scene.

## Safety review and fixes before input

The first independent pre-live review blocked execution; **no click was run against that revision**. The fixes preserve explicit authorization, Windows-only entry gating, scene mutual exclusion and PreviewProbe exit-code propagation, plus:

- Before input: active input desktop, 1.5-second idle, all virtual keys/buttons released, unchanged live cursor, exact owned root-HWND leaf hit-test, client-area inset, unswapped buttons and journal fsync before any cursor/Z-order change.
- After SendInput: keep the cursor/raised owned surface stable until the actual preview popup hides or is destroyed, rather than treating event enqueue as consumption. Only then restore cursor/Z-order; no requirement that the already-hidden popup remain foreground.
- Concurrent native close: failed non-topmost restoration is ignored only when a fresh ownership check proves the owned HWND no longer exists; no other window is changed.
- Recovery: all-VK held-input refusal, safe destroyed-window/process-exit handling, journal replay before owned-process teardown, and structured timeout/failure/recovery receipts.
- Falsification: shown/not-warming/visible/uncloaked gating; exact ItemClick count (one native, zero programmatic); exact readiness line count and position, matching HWND, ordered terminal receipts, empty stderr and natural zero exit.

The complete Python suite ran 180 tests (179 passed, one platform skip on Windows). Tests exercise the actual runner branch, timeout/journal replay and hide-wait/close behavior, not only source strings. Confirming independent review found no remaining issues and explicitly cleared the bounded live Windows run. This is one actual native mouse event path, not UIA InvokePattern delivery or a Mac path.

## Final regressions

Both hosts pass Core 432, Presentation 599, identity/protocol 55 and identity-runner 12 tests. The desktop Python suite runs 180 tests on each host (Windows one skip; macOS 38 skips). Windows lifecycle/programmatic-row regression tokens on the same native build are `ss-uno018-ui-l7inlxl9` and `ss-uno018-ui-t7_j6iyg`. Fresh Mac build `2xe7Kl`, SHA256 `891c2119daafd069e1993713c109b6061dbe15017a9fa43ebffb4b721cdfd987`, passes lifecycle (`ss-uno018-ui-xvkbm7_g`) and programmatic-row (`ss-uno018-ui-s9kq491g`) without input. This confirms cross-platform compilation and preserved non-input scenes, not Mac native row delivery.

## Open boundaries

Mac native Enter still refuses its reported held-input state; Mac native row-click delivery remains NOT RUN. External companion geometry/scroll/handoff policy passes separately, but actual external hover pointer routing remains unverified. Shortcut callback capture, target handoff, General clipboard guardian/recovery and end-to-end native insertion remain open. Production source/packages/CI are unchanged; Plan 019 is not started.
