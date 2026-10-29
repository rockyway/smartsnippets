# Stability checklist

- [x] Read repository guidance, build protocol, popup/insertion/editor plans and UI QA notes; static investigation documented.
- [x] T1: explicit guarded folder selection, OneWay editor inputs, regression tests including unchanged-save store row. Tests await grant; screen reproduction pending.
- [x] T1 audit: editor inputs explicitly guarded across rebind; Settings radios/startup/size guarded. Folders text belongs to stable row VMs (no session swap); popup query already explicit, selection does not persist data. Sort actions are explicit.
- [ ] Establish and fix popup flash; verify first frame on an authorized screen slot.
- [ ] Complete popup prewarm, cache invalidation, lifecycle and timing requirements.
- [ ] Update plan 006 with decisions and evidence.
- [ ] Request single build batch stab-01 only after code and tests are ready; no builds without grant.
- [ ] Screen pass and before/after measurements arranged separately.
