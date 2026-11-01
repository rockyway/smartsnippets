| Date | Branch/ticket | Round | A | B | C | D | Note |
|---|---|---|---|---|---|---|---|
| 2026-11-01 | feature/uno019-picker-fixes | 1 | 2 | 1 | 0 | 1 | close-time foreground race; PS owner ordering + token allowlist missed |
| 2026-11-01 | feature/uno019-picker-fixes | 2 | 0 | 2 | 0 | 0 | GO; oracle gaps closed in test-only commit |
| 2026-11-01 | feature/uno019-diag-fixes | 1 | 0 | 3 | 0 | 2 | open-time recheck + helper-file validation ordering; weak oracles |
| 2026-11-01 | feature/uno019-diag-fixes | 2 | 0 | 2 | 0 | 2 | round-1 fix reordered helper-exit/diagnostics error path; leader folded in directly |
| 2026-11-01 | feature/uno019-diag-fixes | 3 | 1 | 1 | 1 | 0 | rv-05 GO; A (unguarded save_foreground_failure) + C folded by leader; B behavioural worker() test deferred |
| 2026-11-01 | feature/uno019-picker-thread | 1 | 1 | 1 | 1 | 1 | rv-06 GO; visibility-gate oracles + 3rd replacement site + wording folded (pt-01b) |
| 2026-11-01 | feature/uno019-popup-grant | 1 | 0 | 1 | 1 | 1 | rv-07 GO; wiring pins, refused/failed receipt split, stage wording folded (pg-01b) |
| 2026-11-01 | uno019-native-diag2 (nd-02) | 1 | 1 | 2 | 0 | 3 | rv-08 BLOCK: PS poll test never read the record; validator rejections untested; refusal callback unguarded; all folded (nd-02b), leader verified |
| 2026-11-01 | uno019-copy-scenes (cs-01) | 1 | 0 | 3 | 3 | 1 | rv-09 BLOCK: editor-copy deliberate store_write_failed ERROR fails parent log gate; Copy-scene/context-insert oracles too weak; all folded (cs-01b), leader verified |
| 2026-11-01 | uno019-native-diag3 (nd-03) | 1 | 1 | 2 | 1 | 2 | rv-10 BLOCK: new monitor rejects teardown foreground (A destroyed) after settle; transient zero; thin PID/record oracles; all folded (nd-03b), leader verified |
| 2026-11-01 | uno019-n3-shell (n3-01) | 1 | 0 | 5 | 1 | 0 | rv-11 BLOCK: real shell_frame guard and PostOwned HWND guard never executed by tests; vacuous tray-open and message-cycle stub; liveness claim unsupported; all folded (n3-01b), leader verified |
