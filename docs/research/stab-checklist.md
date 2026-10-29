# Stability checklist

- [x] Read repository guidance, build protocol, popup/insertion/editor plans and UI QA notes; static investigation documented.
- [x] T1: explicit guarded folder selection, OneWay editor inputs, regression tests including unchanged-save store row. Tests await grant; screen reproduction pending.
- [x] T1 audit: editor inputs explicitly guarded across rebind; Settings radios/startup/size guarded. Folders text belongs to stable row VMs (no session swap); popup query already explicit, selection does not persist data. Sort actions are explicit.
- [x] T2: corrected confirmed theme-after-prewarm ordering and immediate-hide-before-layout; cloak offscreen prewarm through first layout, DWM dark attributes before show. Solid root already existed; no backdrop/per-show theme setup found. Visual causality/first-frame verification PENDING-USER.
- [x] T3: UI-queued coalesced projection rebuilds + show freshness barrier, numeric timing logs, lazy window replacement behind unchanged DeferredPopupHost, subscription disposal. Invalidation/coalescing/50k import/memory/isolation tests added; await granted execution.
- [ ] Update plan 006 with decisions and evidence.
- [ ] Request single build batch stab-01 only after code and tests are ready; no builds without grant.
- [ ] Screen pass and before/after measurements arranged separately.
