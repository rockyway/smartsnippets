# 004 — Picker input epoch attribution (D75)

**Status:** Backlog — Import Cancel closed as a limitation for this slice, not PASS
**Owner:** Plan 021 Windows picker/acceptance owner, with Plan 018 input/security owner
**Source:** User decision D75; research 053–057, especially nr-14 at `86f9cb6`

Research 057 brackets the last-input change inside main Import UIA Invoke / the
IFileOpenDialog open path: before_main_invoke clean, after_main_invoke changed;
no dialog visible and no input down. The helper completed Invoke but the unchanged
input guard refused before a cancellation receipt. Owned rows/usage stayed zero,
clipboard epoch stayed unchanged, privacy controls passed. Other import/export
phases were not attempted.

D75 accepts an OS-generated last-input change during that path as the operational
limitation. The timeline alone does not identify the OS component or establish
physical-input provenance. Uno has no injection APIs in this path. Do not add a
hook, input synthesis, rebaseline, epoch exception, retry or relaxed admission rule.

Further attribution or picker acceptance requires separate discovery and review,
an owned artifact-pinned protocol and explicit authorization. Keep Import Cancel
as historical FAIL / current LIMITATION, never successful cancel or atomic export
acceptance. Headless real parser/atomic publication tests remain separate evidence.
