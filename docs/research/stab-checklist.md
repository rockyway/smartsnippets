# Stability checklist

- [x] Read repository guidance, build protocol, popup/insertion/editor plans and UI QA notes; static investigation documented.
- [x] T1: explicit guarded folder selection, OneWay editor inputs, regression tests including unchanged-save store row. Tests await grant; screen reproduction pending.
- [x] T1 audit: editor inputs explicitly guarded across rebind; Settings radios/startup/size guarded. Folders text belongs to stable row VMs (no session swap); popup query already explicit, selection does not persist data. Sort actions are explicit.
- [x] T2: corrected confirmed theme-after-prewarm ordering and immediate-hide-before-layout; cloak offscreen prewarm through first layout, DWM dark attributes before show. Solid root already existed; no backdrop/per-show theme setup found. Visual causality/first-frame verification PENDING-USER.
- [x] T3: UI-queued coalesced projection rebuilds + show freshness barrier, numeric timing logs, lazy window replacement behind unchanged DeferredPopupHost, subscription disposal. Invalidation/coalescing/50k import/memory/isolation tests added; await granted execution.
- [x] T4: plan 006 records A/B/C approval, confirmed vs candidate causes, full invalidation matrix and screen limits; AGENTS documents queued cache/recoverable host architecture.
- [x] T5 preparation: code/tests committed and changed compiled bindings statically reviewed. Requesting stab-01; no build/test/publish/app launch has run. Await explicit BUILD GO stab-01.
- [ ] T5 execution: sequential Core/Presentation tests (-m:4), final AOT gate (-MaxCpuCount 4), record exact counts/warnings and BUILD_FINISHED.
- [ ] Screen pass and before/after measurements arranged separately.
