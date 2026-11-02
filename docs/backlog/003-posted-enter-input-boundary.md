# 003 — Posted Enter input boundary (D50)

**Status: Obsolete / closed** — the candidate/harness was removed at the Uno cutover; see [progress 008](../progress/008-uno-official-cutover.md).
**Status:** Backlog — excluded from Plan 019 native runs
**Owner:** Plan 018 Windows input/security owner; Plan 021 acceptance owner
**Source:** Plan 019 AC4, T7 row 7; leader decision D50; research 039–041/057/059

Posted Enter uses SendInput; posting the activation callback does not make the
selection route input-free. D50 excludes this scene. It was NOT RUN in research
057. Research 039's registered F11 / native Enter success predates the 041 permit
ordering fix and is **not re-proven**; posted row/main/context success is not an
Enter or physical HID receipt.

Any later work needs a separately reviewed exact-target, keyboard-lifetime,
permission, single-flight and recovery protocol, fresh pinned artifacts, negative
controls and explicit operator authorization. Do not silently substitute a VM
callback, UIA invocation or posted message for native keyboard acceptance. All
review flags and the ordinary candidate remain default-denied.
