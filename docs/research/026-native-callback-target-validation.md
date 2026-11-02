# 026 — Native callback and exact-target validation

Status: in-process native event delivery observed on both desktops; partial Plan 018 T4, not physical/global shortcut invocation or shortcut-to-popup/insertion completion.

## Native observations

| Host | Harness SHA256 | Target SHA256 | Private evidence token |
|---|---|---|---|
| Windows x64 | `54b35a08ada99a7c0d9f76c7d384c07935ef863d3d6831608faf58532976c569` | `b2e1332b755ce30ac3e1570ed5eb3309e38d0efc30b53b994413d1a96bfb58cb` | `d4683a0a747a446a80c2da37b524ac3b` |
| Apple Silicon macOS | `cebe9f9733f6d4b8eb4268182a4b451805d3827255daf21ca6cb93ab5edb5af3` | `3274c2e279d9e2529760a14cbb8697491cd8f842fb4cef601e7300f9f8d86b82` | `0d25aaf8953b42b3b321d9e3f97c7f25` |

Strict Native AOT publishes pass with empty NoWarn and zero warnings on both hosts. Separately authorized runs report `owned_callback_harness=OBSERVED delivery=INPROCESS_NATIVE_EVENT physical_shortcut=NOT_RUN watchdog=REAPED input=NONE clipboard=UNTOUCHED`.

The trusted direct-child/window provenance and outer owned-job/session watchdog from research 023 remain mandatory. Callback mode additionally requires `--inprocess-native-callback --authorized-native-hotkey`. It uses a synchronous original-entry-thread head, including a Darwin main-thread assertion; async target IO does not move callback lease operations off that thread.

## Actual native producer/consumer sequence

Windows registers the rare Win+Ctrl+Alt+Shift+F24 chord on an owned message-only HWND, receives explicitly posted WM_HOTKEY through its real WndProc/event loop, and validates native ID plus generation. macOS installs a real Carbon event handler and registers Ctrl+Option+Shift+F20, then uses CreateEvent/SetEventParameter/SendEventToEventTarget through that native handler table. Neither path synthesizes HID/key/mouse events.

For each of two generations the fixture verifies native registration, refusal of a conflicting second lease without overriding the owner, wrong-ID and stale-generation rejection, valid callback delivery, exact A foreground validation and recording of its previously captured identity, same-PID sibling B refusal, release, post-release rejection, disposal and reacquisition. All target focus changes are owned fixture commands with quiet-session preflight. Registration/release/disposal must not change exact target foreground. Callback exceptions are contained before crossing native ABI boundaries.

This is **not generic capture of an arbitrary application's foreground window**: the callback validates and records an already captured, provenance-checked fixture target through the actual ITargetProbe. It does not open Uno, run PopupShowCoordinator, dispatch paste or record usage. Physical shortcut input, OS-generated repeat/delivery, sleep/resume and full shortcut-to-popup-to-insertion wiring remain unverified.

## Pre-live ABI correction

Independent high-effort review blocked Mac execution because Carbon ItemCount/ByteCount were declared uint. Installed Xcode MacTypes.h defines both as unsigned long, eight bytes on Apple Silicon; GetEventParameter's out byte count could overwrite a four-byte stack slot. The four declarations now use nuint, including out nuint. Five managed tests inspect the actual P/Invoke declaration types; reflection is confined to tests, never the native harness. Confirming critical ABI re-review found no remaining issues before native execution.

Both hosts pass 67 identity/callback C# tests and 14 runner Python tests. These policy/ABI/runner tests complement, rather than substitute for, the native observations above. No production source/package/CI changes, General clipboard mutations, Plan 018 completion or Plan 019 approval are implied.
