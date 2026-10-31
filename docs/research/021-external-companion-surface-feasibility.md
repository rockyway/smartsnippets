# 021 — External companion surface feasibility

Status: partial Plan 018 T3 evidence, not completion of FG3 or authorization to start 019.

## Actual native runs

| Host | Build token | Native entry SHA256 | Runtime token |
|---|---|---|---|
| Windows x64 | `0f98cfe836874afd90cb084f43c56172` | `96883ee4a5dda963696564bf9ffd485d75fe4865782fbc636b08987bf8b1c57c` | `ss-uno018-ui-rwskocyd` |
| Apple Silicon macOS | `JU368h` | `261dbaa4c3983664812781ddae1ef878f6d07bdfc31e6725c2c85792aed5742a` | `ss-uno018-ui-cio_hf2o` |

Both collectors report BUILD_CHECKS_ONLY with no warning/error. Separately, hash-pinned `run_ui_smoke.py --companion --authorized-screen-slot` reports an observed owned window and `ui_companion_result=PASS graceful_exit=true` on each host. This is the companion scene's result only, not an overall plan PASS. Windows probe windows were parked on the bottom-left monitor. Neither run synthesized input or accessed clipboard contents.

## Verified composition

`App` / platform entry → `CompanionSurfaceProbe` → actual Presentation `HoverDetailsCoordinator` and `HoverDetailsPlacement.PlaceBesideWindow` → owned native companion surface. A realized XAML `ScrollViewer` contains compiled-bound synthetic multiline Unicode text. Assertions require native frame outside the owner and inside the current screen work area, cloaked non-activating warmup before reveal, unchanged foreground token, scroll offset advancement, hide, delayed policy reopen, enter/exit handoff cancellation, native companion absence after close, both managed close callbacks, and natural successful process exit.

Windows frame operations verify owned PID and use non-activating placement with a second placement for DPI transitions. Darwin arm64 uses native NSWindow frame/screen visibleFrame, converted to a consistent downward-Y coordinate space. Native surfaces are cleared before window closure. Failures emit only fixed stages and exception type, never text or titles.

## Regression and review

Windows and macOS pass Core 432 and Presentation 599 tests; the Python suite runs 156 tests on each host (Windows one skip; macOS 29 skips). Independent argon review found no P0/P1 blockers for this checkpoint. Two documentation consistency notes were corrected; this is not a completion merge.

## Boundaries

This validates an **alternative native companion-window surface**, not the production external Popup implementation. Hover policy enter/exit calls and scrolling are programmatic: physical pointer event routing, actual row-to-surface traversal, pointer-wheel delivery, all monitor/work-area geometries and production integration remain unverified. Windows foreground identity is HWND-based; macOS's unchanged foreground token is a process identity check, not an independent AX exact-window handoff proof. The separate AX fixture evidence does not automatically fill that gap.

The support-floor corrections and fresh prior data/lifecycle runs are recorded in [020](020-desktop-toolchain-and-support-floors.md). Mac held-input preview refusal, General clipboard preservation/recovery and actual insertion gates remain open. No clipboard guardian or native paste success is inferred from this scene.
