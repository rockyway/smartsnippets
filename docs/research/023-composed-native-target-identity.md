# 023 — Composed native target identity prerequisite

Status: native identity observation on both desktops; partial Plan 018 T4. No insertion, hotkey callback or General clipboard acceptance.

## Composition and native observations

`run_identity_harness.py` → verified native C# harness → hash/native-header/bundle/signature-validated direct child → retained process/provenance lease → actual `ITargetProbe` implementation → platform exact-window adapter. The external runner establishes a kill-on-close Job Object (Windows) or private session/process group (macOS) before releasing the launch permit. The harness owns target readiness, commands, native references and cleanup; no arbitrary-PID attachment is exposed.

| Host | Native harness SHA256 | Native target SHA256 | Successful private evidence token |
|---|---|---|---|
| Windows x64 | `e2903b238b3a891321e45e6f8dbe21f9d1f9c26aa3fae22a832f136c80d13c0e` | `b2e1332b755ce30ac3e1570ed5eb3309e38d0efc30b53b994413d1a96bfb58cb` | `78b2fe6328724e4998ca13fa8c7ce1ff` |
| Apple Silicon macOS | `258ba5128d0b0df53e439891963703ab5e1810b265c328c8a47311aaaa806f42` | `3274c2e279d9e2529760a14cbb8697491cd8f842fb4cef601e7300f9f8d86b82` | `54223c6714a04579b7b630e2d21b245d` |

Both native C# heads publish with strict Native AOT, empty NoWarn and zero warnings. The Swift target is arm64 native with warnings-as-errors and verified ad-hoc signature. Each authorized native run reports `owned_identity_harness=OBSERVED watchdog=REAPED input=NONE clipboard=UNTOUCHED`. The target's focus commands activate only its owned windows after quiet-input/session checks; no keyboard/mouse events or clipboard operations occur. Windows targets use bottom-left monitor placement.

Observed sequence: two empty editors in one owned process; capture A and B; focus A → B → A with exact same-PID sibling rejection; close A; require captured A closed/foreground false and the exact B still eligible; focus B; stop owned target and harness. Windows identity includes HWND/PID/run-specific class/editor ownership. macOS identity retains AX elements matched by fixed synthetic identifiers, checks current live AX window membership and exact CFEqual focused-window identity, and brackets with frontmost application PID. A bare PID, window count, title or geometric match cannot pass.

## Native failures found and corrected

1. **Session key macro:** The initial Mac focus preflight used the public C macro's name as a dictionary string. Installed Xcode `CoreGraphics/CGSession.h:46` defines `kCGSessionOnConsoleKey` as `CFSTR("kCGSSessionOnConsoleKey")`. Using the actual expansion fixes the false refusal; login-done and optional lock-state checks remain. A fixed-value regression protects this typo.
2. **Exit versus pipe EOF:** Initial Mac identity stages all completed, but outer teardown did not produce a successful result. Pipe EOF is not proof that a process has exited. The runner now observes natural exit before teardown: retained Windows handle wait, or Darwin kqueue NOTE_EXIT without polling/reaping the session leader. The kqueue is explicitly closed (Python's Darwin kqueue has no context-manager protocol).
3. **Already-zombie groups:** Darwin returns kqueue EV_ERROR/ESRCH when registering an already-zombie child, and killpg can return EPERM for a zombie-only group. These behaviors were reproduced with a harmless directly owned `/bin/sleep` child. The fallback accepts only a bounded, numeric-only `ps` query proving the unreaped leader exists in its own group and every returned member is a zombie. A live, foreign-group, missing-leader, malformed or denied query refuses the fallback; a genuine denied live-group kill is not silently accepted. The leader remains unreaped until group cleanup, preventing PID/group identity reuse.

Earlier failing Mac observations are not retroactively labeled successful. The final Mac token above is from the corrected path. Independent high-effort review and confirming critical-cleanup re-review found no issues. The final Windows row above was republished and rerun after the Mac session-key correction; the earlier Windows observation also succeeded.

## Coverage and limits

Both hosts pass 55 C# identity/protocol tests. Both hosts also pass 12 runner tests covering ordering, spoof/refusal, timeout, interruption, zombie-only classification, and non-reaping exit observation. Actual positive Unicode/multiline editor verification after native paste is NOT RUN: fixed allowlisted verification commands exist and their in-memory exact UTF-16 policies are unit-tested, but no synthetic text was pasted in these native runs.

Permission revocation, shortcut-trigger capture before popup, popup handoff/revalidation, positive native paste, usage recording, duplicate dispatch, General clipboard guardian/recovery and full T4/T5 acceptance remain open. Identity tokens are retained live-window references only, not durable window identities across recreation. This does not approve Plan 019 or a completion merge.
