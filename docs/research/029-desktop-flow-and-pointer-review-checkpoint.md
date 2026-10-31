# 029 — Actual-host flow and external-pointer checkpoint

Status: reviewed desktop source/portable-test checkpoint with fresh Native AOT builds and bounded **no-input** native regressions. Plan 018 is incomplete. No production migration, 019 start or required-gate merge follows.

## Shared host composition and corrected lifecycle

The opt-in `--probe-ui-flow` consumes the same portable factory as its tests: actual Core Migrator/SnippetStore/settings, actual Popup/Snippets VMs, PopupShowCoordinator, InsertionService and InsertionShutdownCoordinator. Only native/window services are substituted in portable tests. The default GUI has no native adapter, cannot capture an external target, and refuses clipboard/input operations.

Independent review identified real defects, fixed together: stale closed-window references (including macOS NSWindow lifetime), native Windows AOT ListView container construction, warmup generation/retry/timeout handling, main versus popup Copy routing, re-show/footer/focus assertions, shutdown admission/draining, post-dispose callbacks and owner-private profile cleanup. A compiled portable popup driver now owns policy/timer/cloak transitions; the actual page handlers are compiled into tests with UI-boundary stubs. Separate static XML audits do not establish native event delivery. The confirming review cleared P0/P1 findings. The optional missing footer-event audit was added directly.

Native adapters remain unfilled; the Windows insertion executable remains hard-disabled. Inline Editor focus is explicitly NOT IMPLEMENTED in this flow fixture rather than incorrectly routed to Search. A GUI button labelled shortcut/menu callback is not physical global shortcut or native tray/menu-bar evidence.

## External-pointer path and recovery

`--companion-native-pointer` routes actual row/surface PointerEntered/PointerExited to the real HoverDetailsCoordinator. It does not fall back to a programmatic hover. The Windows helper moves only the cursor over exact owned leaf HWNDs after active-desktop, idle/all-input-up, PMv2, client-inset, image/parent/PID and geometry checks.

The first review found callback-ordering, durable-journal and UIA/bootstrap defects. The fix batch awaits intra-main row exit before cross-window handoff, flushes and validates callbacks, uses system PowerShell STA with pinned framework UIA assemblies, validates companion ownership before UIA, detaches callbacks before restoration, and stops the owned child before recovery on all live paths. Version-2 pointer journals retain both previous/planned cursor positions, physical input epoch and truthful original topmost status, plus an explicit pointer-only/no-Z-order-change kind. Existing row-click journals remain compatible. Unknown cursor position/input takeover fails recovery closed rather than fabricating original restoration. Confirming security review found no remaining issues.

The automated physical pointer helper is Windows-only. Cross-platform C#/XAML compilation does not prove native macOS pointer delivery.

## Fresh build and unit evidence

| Host | Native build token | Entry SHA256 |
|---|---|---|
| Windows x64 | `7ba71f2139784c24b06c0880d0c22a5a` | `39efb765da86f4595d15ea285489d8ed519f92307a0f02d681ad4a3d1af6e3f5` |
| Apple Silicon macOS | `xFKtxt` | `c84926143cffa349c51f61618b3009ae91a77f1c381040d02344ffaf2e12c0ee` |

Both strict collectors pass native binary/dependency/property/warning checks. An intermediate Mac build `JLSmxF` failed CS0104 because Uno's implicit Windows.Storage import made FileAttributes ambiguous. Parent explicitly qualified System.IO.FileAttributes; the fresh build above passes. No warning suppression or runtime fallback was introduced.

Core 432 and Presentation 599 tests pass on each host. Identity 70 and Windows-insertion 29 tests pass on Windows (the unchanged identity suite had already passed on Mac). The portable flow suite passes 51 tests on both hosts, including the added footer static-audit case; portable hover policy passes 5. Desktop Python runs 210 tests (Windows one skip; Mac 55 skips), including three pure refusal-diagnostic tests. Private-station policy tests pass 30. Identity runner Python passes 14. These suites are not native insertion/clipboard proof.

## Observed native runs — limited, content-free

All test processes below were owned, hash-pinned and stopped. Windows visible windows stayed on the bottom-left monitor. No keyboard/button synthesis or clipboard writes occurred.

| Scene | Windows token/result | Mac token/result |
|---|---|---|
| Programmatic companion geometry/scroll/real policy handoff | `ss-uno018-ui-dw52m72n` PASS, natural exit | `ss-uno018-ui-1no4ii39` PASS, natural exit |
| Programmatic shared preview-row handler / actual preview VM | `ss-uno018-ui-y9hfaxga` PASS, natural exit | `ss-uno018-ui-o96uk8yv` PASS, natural exit |
| Default actual flow GUI startup and owned native close | `ss-uno018-ui-265p5c8s` observed | `ss-uno018-ui-9mn3bmlc` observed |

Flow startup/close on each host observed one new private profile and its removal after native window closure, empty stderr and natural process exit. Windows close used WM_CLOSE on the exact retained child's HWND; Mac used AXPress on the exact owned window's close button after AX PID/type checks, not an arbitrary control. These are lifecycle/cleanup observations, **not Preview/Enter/row-delivery or insertion proof**. No window titles or fixture payloads were read/logged. Windows additionally observed unchanged clipboard sequence. Mac default native adapter was absent; no clipboard contents were accessed.

## Guarded pointer trial — BLOCKED before movement

The first Windows run (`ss-uno018-ui-e6pkxy36`) observed the owned main window/ready receipt, then refused before any movement or journal creation. A small content-free diagnostic fix maps only allowlisted error text to fixed reason tokens; arbitrary provider/exception content is never echoed. Its three pure tests pass.

The one diagnostic retry (`ss-uno018-ui-ucgvniwg`) again refused before movement/journal creation, this time with `reason=INPUT_NOT_IDLE`. A separate read-only aggregate check observed `held_input_count=1`, idle at least 1.5 seconds, and valid input-info API result. No individual key/button identity was logged. Thus this is an external held-input prerequisite, not permission to remove the guard. Both runs stopped the owned process and left clipboard untouched; neither proves pointer delivery/handoff or requires cursor recovery, because no move was reached.

Mac's earlier aggregate check also reported a held documented key (research 025). No blanket key-up/reset was sent on either host. Native interaction awaits operator release of input and disposable clipboard readiness. Private station attempts remain blocked (research 028); General clipboard recovery and positive insertion remain NOT RUN. Physical shortcut-to-popup-to-paste/usage, real main-row target dispatch, the remaining permission/fault matrix, privacy manifests and final go/no-go are still outstanding.
