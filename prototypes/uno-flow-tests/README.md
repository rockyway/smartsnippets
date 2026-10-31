# 018 T5 portable actual-host flow checkpoint

**CI substitutes OS boundaries. This is not native FG3/FG4/FG5 acceptance.**
No native UI, input, clipboard, global shortcut delivery, or permission tests were
run. The existing `windows-insertion/Program.cs` `NativeEntryReviewed` gate is
unchanged and false. No shipping Windows adapter is referenced or called.

## Commands (sequential, no GUI launch)

```sh
dotnet test prototypes/uno-flow-tests/SmartSnippets.UnoFlow.Tests.csproj
dotnet test tests/SmartSnippets.Core.Tests
dotnet test tests/SmartSnippets.Presentation.Tests
pwsh -NoProfile -File prototypes/uno-desktop/collect-build.ps1 -RuntimeIdentifier win-x64 -ManualWindowsPackages
```

No root solution/CI/package configuration is changed. This project disables CPM
locally and links the actual portable `uno-desktop/Flow/*.cs` and actual thin
`FlowProbePage.xaml.cs`. Core/Presentation are project references, not copies.

## What is executed in CI

- `ProbeFlowComposition.Create` is the same factory consumed by `UiFlowProbe`.
  Actual SQLite migration/store, Popup/Snippets VMs, `PopupShowCoordinator`,
  `InsertionService`, and `InsertionShutdownCoordinator` are never mocked.
- `ProbePopupDriver` is compiled and executed in both host and tests. Only native
  surface primitives, timer callbacks, target/clipboard/input services and UI
  dispatcher scheduling are substituted. Tests cover cloak-before-warmup,
  hide-before-uncloak, generation guards, retry remeasurement, bounded timeout,
  stale callback delivery, close while warming/ready, lazy cold recreation,
  queued-focus cancellation and deferred dirty-store projection flushing.
- The actual page code-behind is compiled against **test-only UI primitives**.
  Tests invoke those handler bodies through a test-only partial wrapper. This
  verifies forwarding, both Windows container-creation callbacks, copy roles,
  Footer/Search routing and dismissal; it does **not** execute compiled XAML,
  native event delivery, focus, layout, or WinRT RCW construction.
- All null-adapter refusals and injected-adapter forwarding execute compiled
  `ProbeFlowBoundary` code through the actual driver. Consented clipboard loss
  still refuses without an adapter.
- Real insertion paths cover target capture before warmup, Enter/row/main-row
  insertion, Unicode/CRLF, hide-before-focus, exact original target, same-PID
  wrong-window rejection, Busy/duplicate suppression, once-only confirmed usage,
  zero uncertain/failure/copy/preview usage, re-show, footer focus and caution
  dismissal. Preview exercises actual `Popup.CopyRow`, not just a wrapper guard.
- Shutdown synchronously closes admission and hides, then asynchronously drains
  insertion restoration and tracked VM continuations before deleting the profile.
  It is idempotent. `ShutdownAsync`/`DisposeAsync` must be awaited for active work;
  `Dispose` starts cleanup without blocking the UI thread. Post-shutdown host
  callbacks and queued projection/focus actions cannot start work.
- Profile tests verify collision refusal, owner-only Windows DACL (or Unix 0700),
  ownership marker, constructor-failure cleanup, safe deletion refusal on marker
  mismatch, repeated cleanup and post-drain removal. Only this SQLite pool is
  cleared. An actual AppLog canary checks synthetic title/snippet tokens stay out
  of portable-flow logs; this is not a process/OS-stream FG5 privacy audit.

`HostCallbackRoutingTests` additionally perform **STATIC-only** audits of actual
App/XAML/native-wrapper source. C# comments are stripped; XML hierarchy checks
require enabled popup ItemClick and the Insert button inside the typed main-row
DataTemplate. Native wrapper handle invalidation order is statically inspected.
These checks do not prove that native `Closed`, layout or routed events fire.

Five temporary mutation controls were rejected by assertion failures: disconnected
re-show, disconnected footer focus, missing dirty flush, stale callback cleanup
before the generation guard, and showing a still-cloaked surface. Sources were
restored, then the entire suite rerun. Removing only the driver's redundant
identity guard did not bypass the composition's independent generation guard;
that benign mutation is not claimed as a detected defect.

## Actual host and remaining gaps

Both `--authorized-probe-ui` and `--probe-ui-flow` are required. Scene exclusivity
is owned by the entry points. **Do not launch without separate screen approval.**
This is an interactive probe, not an unattended acceptance runner, and emits no
fake PASS receipts. Shortcut/menu buttons are in-process callbacks, not global
shortcut/menu-bar/tray delivery from an externally focused target.

The default `IGuardedProbeNativeAdapter` remains null and has no implementation.
Shortcut/menu capture refuses; main Copy explicitly refuses even after a preview;
popup preview Copy cannot reach the clipboard boundary. Main and popup copy
controls have separate roles. Preview is target-free and non-activating. Footer
focus goes to an actual cancel/dismiss control; Editor focus is explicitly
**NOT IMPLEMENTED**, never silently mapped to Search. This probe does not expose
an inline editor or a native consented-insertion UI acceptance path.

The thin native wrapper invalidates its NSWindow/HWND references before notifying
the shared driver of `Closed`. The driver detaches callbacks/stops the timer,
resets policy cold and lazily creates a new surface. Main close invalidates its
own reference before asynchronously draining flow shutdown, then closes any
still-live owned popup. These are compiled/source-audited, **not native-runtime
verified** guarantees. Native preview/event delivery, live close/recreate, two
simultaneous compiled binding surfaces, real footer focus, native shortcut,
clipboard recovery, positive insertion and process/OS privacy remain NOT RUN.
Existing PreviewProbe and companion native scenes are not evidence for this mode.

50 portable tests pass (17 composition cases, 12 driver/profile cases, 8 actual
page-handler cases, 13 static audits). Core 432 and Presentation 599 tests pass;
the combined snapshot's 5 companion-pointer policy tests also pass. Windows strict
Native AOT/XAML build evidence is recorded below. No Mac build/run was attempted.
This checkpoint does not complete 018/T8, FG3/FG4/FG5, or permit 019. Independent
consolidated review is required before any authorized native preview trial.

Build-only collector run `b1d5ad493484489ab47d19ede29043c8` returned
`BUILD_CHECKS_ONLY`, `errors: []`, `runtimeGates: NOT RUN`. Its private OS-temp
root retains effective-property, restore, clean and publish evidence. Artifact:
`prototypes/uno-desktop/bin/probe/b1d5ad493484489ab47d19ede29043c8/SmartSnippets.UnoProbe.exe`.
SHA-256 `f5b662620ec91f0a70a130c671052a5f5ed1261fc1565b940dbd67a3e6f1a1ad`.
The artifact was not executed. Windows DACL checks executed on this host; the Unix
0700 branch still requires its own host/CI run.
