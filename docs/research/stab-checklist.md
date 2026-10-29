# Stability checklist

- [x] Read repository guidance, build protocol, popup/insertion/editor plans and UI QA notes; static investigation documented.
- [x] T1: explicit guarded folder selection, OneWay editor inputs, regression tests including unchanged-save store row. Presentation suite passed; screen reproduction remains PENDING-USER.
- [x] T1 audit: editor inputs explicitly guarded across rebind; Settings radios/startup/size guarded. Folders text belongs to stable row VMs (no session swap); popup query already explicit, selection does not persist data. Sort actions are explicit.
- [x] T2: corrected confirmed theme-after-prewarm ordering and immediate-hide-before-layout; cloak offscreen prewarm through first layout, DWM dark attributes before show. Solid root already existed; no backdrop/per-show theme setup found. Visual causality/first-frame verification PENDING-USER.
- [x] T3: UI-queued coalesced projection rebuilds + show freshness barrier, numeric timing logs, lazy window replacement behind unchanged DeferredPopupHost, subscription disposal. Invalidation/coalescing/50k import/cache-size/disposal/subscriber-isolation tests passed in stab-01. These assert row/dictionary/subscription counts and queued-work safety, not collectibility, retained snapshots/windows or memory growth; no retention oracle was run. 50k dirty open: 67.48 ms (VM rebuild/filter only, not rendered frame).
- [x] T4: plan 006 records A/B/C approval, confirmed vs candidate causes, full invalidation matrix and screen limits; AGENTS documents queued cache/recoverable host architecture.
- [x] T5 preparation: code/tests committed; compiled bindings statically reviewed. Explicit BUILD GO stab-01 received; all build outputs and TEMP/TMP confined to C: snapshot, nothing built on full D:.
- [x] T5 execution: dotnet build -m:4 succeeded (0 warnings, 0 errors); Core 426 passed / 0 failed / 0 skipped; Presentation 467 passed / 0 failed / 0 skipped (sequential, -m:4). One scripts/publish-aot.ps1 -MaxCpuCount 4 -Output C:\temp\ss-stab-build\publish passed with 0 warnings, including 0 IL2xxx/IL3xxx. EXE + e_sqlite3.dll present, managed app DLL absent. BUILD_FINISHED stab-01 written. Initial build found one NumberBox event signature error; fixed/committed/re-mirrored before successful build and tests.
- [ ] PENDING-USER: authorized screen pass, first-frame colour (light/dark/system/high contrast), first/repeated hotkey timings, A→Back→B folder behaviour, hidden theme/data refresh and lazy window recovery. No app launch, insertion, clipboard operation or user-profile mutation performed. No baseline or rendered-frame measurements claimed.

## Static review fix round (validated under stab-02)

- [x] Fix 1: NumberBox focus-within walks the focused descendant tree; an edit-session latch and deferred final-value read preserve focus-loss commits. Finite/rebind/session-identity guards retained. Typing, stepping, spin buttons and focus-loss commit still require control-level verification.
- [x] Fix 2: shared testable show/focus coordinator carries explicit focus through async prewarm; late Search cannot overwrite Footer, queued focus is invalidated by hide/close/new requests. Actual VM ClipboardNotPreserved replacement and suspended/resume regressions passed under stab-02.
- [x] Fix 3: show coordinator explicitly cancels warmup on hide, resets to cold, restarts on next show and rejects stale generation-tagged timeouts. WinUI layout readiness is current-state-validated (warming, visible, XamlRoot and width), with its handler detached on cancel; it is not tagged by originating attempt. Two-second UI timer retries once; repeated failure logs and remains hidden/restartable. Tests covering cancelled exit→next show, timeout/retry/focus retention, explicitly supplied stale coordinator generations, close and supersession passed under stab-02; these do not verify the native layout adapter's event delivery. Class sweep: hotkey, preview, recovery, main/edit cancellation, late focus callbacks; nonactivating preview now invalidates queued focus from older shows. Standalone hidden Focus intentionally does not show/activate.
- [x] Fix 4: added hidden-show-barrier and visible-queue regressions using unchanged IDs: old/new body/tag/folder searches, old-match disappearance, current tag projection, AND query, and exact two-row usage-count crossover order plus selected-ID retention passed under stab-02.
- [x] Fix 5: narrowed T3 evidence to cache-size/disposal/subscriber counts and explicitly excluded retention/collectibility/memory-growth proof.
- [x] Static readiness: reviewed new event signatures, all show/focus entry points, timer cleanup and supersession; `git diff --check` clean. Code committed before BUILD GO stab-02; no ungranted builds or app launches.
- [x] Granted stab-02 C: snapshot validation of source `ef70486`: final build 0 warnings / 0 errors; Core 426 passed / 0 failed / 0 skipped; Presentation 480 passed / 0 failed / 0 skipped (906 tests total). One AOT publish passed, 0 warnings including 0 IL2xxx/IL3xxx; EXE and e_sqlite3.dll present, managed app DLL absent. BUILD_FINISHED stab-02 written. Initial build had CS0104 timer ambiguity plus cascading WMC9999/WMC1509 (2 errors / 1 warning); alias fixed, committed and re-mirrored before clean build/tests/publish. No app launch.
- [x] Fix-round evidence: `C:\temp\ss-stab-02-build.log` (initial), `C:\temp\ss-stab-02-build-final.log`, `C:\temp\ss-stab-02-core.log`, `C:\temp\ss-stab-02-presentation.log`, `C:\temp\ss-stab-results\core-stab02.trx`, `C:\temp\ss-stab-results\presentation-stab02.trx`, `C:\temp\ss-stab-build\publish.log`. AOT output remains only `C:\temp\ss-stab-build\publish`. Current 50k dirty-open VM diagnostic: 69.16 ms, not rendered-frame latency.
- [ ] PENDING-USER: NumberBox typing/keyboard steps/spin buttons/commit on focus loss, replacement clipboard-loss Footer focus, cancelled-exit/retry behaviour, plus all previously pending folder/first-frame/desktop-timing checks. Pure shared-policy tests are not WinUI event evidence.

## Static review fix round 2 (no runtime validation authorized)

- [x] Fix 1: ignored show requests preserve existing coordinator visibility, while RequestShow still invalidates obsolete queued focus. Added visible→ignored-preview→explicit-Search-focus regression; kept cold/rejected and nonactivating-preview cases. Tests are unrun.
- [x] Visibility class sweep: PopupWindow only changes its visibility flag after native Show/Hide; all rejected Ready methods return before either transition. Coordinator success marks visible; BeginWarmup/Hide/Close explicitly clear it. Rejection no longer invents a Hide.
- [x] Fix 2: chose the smaller documentation correction: generation-tagged timeouts versus current-state-validated WinUI layout readiness. No per-attempt layout delegate or native stale-layout rejection is claimed; adapter verification remains separate from shared-policy tests.
- [ ] Runtime validation of fix-round-2 code remains pending a future explicit grant; no build/test/app launch or BUILD_REQUEST this round.

## Screen grant stab-screen (2026-10-28)

- [x] Safety preflight: authorized DISPLAY9 geometry exists; refused launch because unowned SmartSnippets.App PID 55600 was running from `D:\sources\demo\smartsnippets\publish\SmartSnippets.App.exe` and remained after a bounded 60-second wait.
- [ ] Screen checks remain PENDING (blocked before launch): first-frame colour/capture, no activation/taskbar entry, synthetic 10k timing logs, Snippets/Settings rendering. Folder behaviour remains PENDING-USER.
- No windows touched/captured, no candidate launch, no builds/input/profile/registry changes. `docs/qa/ui/stab/README.md` records the refusal; no screenshot evidence exists.

## Historical stab-01 evidence (C: only; does not validate fix-round code)

- Validated source commit: `125a05a`; `0346d81` recorded that evidence. Review fixes after `2915e18` were subsequently validated under stab-02 above.
- Build log: `C:\temp\ss-stab-build-build.log` (final successful build).
- Core log: `C:\temp\ss-stab-core.log`; results `C:\temp\ss-stab-results\core.trx`.
- Presentation log: `C:\temp\ss-stab-presentation.log`; results `C:\temp\ss-stab-results\presentation.trx` (includes `dirty_open_50000_ms=67.48 rows=50000`).
- AOT log: `C:\temp\ss-stab-build\publish.log`; artifacts remain `C:\temp\ss-stab-build\publish`. No app was launched. User chooses deployment/run location.
