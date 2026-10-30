# 020 — Uno macOS desktop integration

**Date:** 2026-10-30
**Status:** Provisional draft—dependency evidence pending; unapproved, not implementation-ready
**Size:** L
**Area:** `src/SmartSnippets.Uno/Platforms/MacOS/`, `src/SmartSnippets.Uno/Popup/`, `src/SmartSnippets.Uno/Controls/`, `src/SmartSnippets.Presentation/`, `tests/`
**Requirement brief confirmed:** 2026-10-30 (G1, master 017)
**Master:** [017 — Master plan: Uno cross-platform migration](../../plan/017-master-uno-cross-platform-migration.md)
**Canonical placeholder:** [020 placeholder](../../plan/020-uno-macos-desktop-integration.md) (unchanged until 018 and 019 complete)

---

## 1. Goal

Target Phase 1 desktop feature parity on Apple Silicon macOS (`osx-arm64`) inside `SmartSnippets.Uno` by replacing 019's fail-closed macOS stubs with native AppKit, Accessibility (`AXUIElement`), CoreGraphics, `NSPasteboard`, `NSStatusItem`, `SMAppService`, and `NSOpenPanel`/`NSSavePanel` adapters—conditioned on 018 FG3–FG5 evidence for exact-window target validation, pasteboard preservation, non-activating QA/preview surfaces, and Native AOT bundle execution.

## 2. Requirement (as confirmed at G1)

Excerpted from `docs/plan/017-master-uno-cross-platform-migration.md:19–55` and `docs/plan/020-uno-macos-desktop-integration.md:7–17`:

- **Problem:** After 019, `SmartSnippets.Uno` compiles for `osx-arm64` with shared XAML and portable contracts, but macOS native shortcut capture, exact-window focus/paste, pasteboard safety, menu-bar/login-item lifecycle, startup alerts, and external hover surfaces remain unimplemented stubs.
- **In scope:** Apple Silicon (`osx-arm64`) native implementations for global shortcut registration/recording, pre-focus target capture and same-PID exact-window validation, `NSPasteboard` snapshot/privacy-marker/changeCount restoration, `⌘V` dispatch with Accessibility permission guidance, `NSStatusItem` menu bar, single-instance & `SMAppService` login-item background launch (without `--background` CLI arguments), `NSAlert` startup failures, notifications, `NSOpenPanel`/`NSSavePanel` atomic JSON I/O, multi-monitor `NSScreen.visibleFrame` placement, beside-window hover/keyboard previews, and non-activating `--preview-popup-sort-menu` + `menu_surface` receipt.
- **Out of scope / non-goals:** Intel (`osx-x64`) builds, Windows elevation/UAC impersonation on macOS, Mac App Store sandboxing (Phase 1 targets direct signed/notarized distribution; `017 §4.1`), notarization/final release cutover (021), mobile (022–025), cloud sync, password snippets (`016`).
- **Users and surfaces:** Apple Silicon macOS users across Main Window (`Snippets`, `Folders`, `Settings`), Global Popup, beside-window hover/keyboard preview, menu-bar icon, login item, and permission guidance banners/dialogs.
- **Behaviour:** Shortcut or menu-bar action captures the frontmost editable window before activating Smart Snippets; Enter/row/main-row Insert validates permissions and exact target window, snapshots preservable `NSPasteboard` items, posts `⌘V` once, credits usage only when native event posting completes without error after the adjacent foreground gate (`CGEventPost` does not prove target persistence; actual buffer delivery is verified in owned fixtures and 021 target matrices), and restores `NSPasteboard` iff `changeCount` is still owned. Denied/revoked permissions or unpreservable pasteboard payloads fail closed without pasting into another window or mutating the pasteboard.
- **Constraints and assumptions:** Conditional on Plan 018 FG3–FG5 measured evidence (exact shortcut API without blanket Input Monitoring, `AXUIElement`/`CGWindowID` window identity, external companion `NSPanel` vs Uno popup overflow, observed `LF` vs `CRLF` paste behavior, and production-approved macOS Uno AOT dependency replacing `016`'s probe-only overlay) plus Plan 019 completion.
- **Acceptance criteria:** Maps to master **AC2** (Apple Silicon macOS desktop parity), **AC3** (`osx-arm64` Native AOT + safety tests), **AC5** (import/export, persistence, permissions, failure handling, usage), and **AC6** (offline, content/title/path-free logs).
- **Decisions made:** Apple Silicon only (`017 §2`); no success-returning unsupported stubs (`020 placeholder`); safety by outcome on `NSPasteboard` (`017 §4.3`).

## 3. Current state (as-built)

- `src/SmartSnippets.App/Services/TargetCapture.cs:14–59` and `Win32TargetProbe.cs:7–21` capture `(HWND, PID, HMONITOR, DisplayName)` via Win32 and validate exact `HWND + PID` through `TargetIdentityProbe.cs:4–21`. A PID-only macOS check (`NSRunningApplication`) would mis-paste into Window B of the same app.
- `src/SmartSnippets.App/Services/ClipboardPasteDispatcher.cs:81–195,200–254` and `ClipboardSafety.cs:6–56` snapshot Win32 formats up to 64 MB, return `SwapStatus.NotPreserved` before `EmptyClipboard()` when any non-skipped format is unreadable/oversized, publish history/cloud exclusion markers before text, inject 4 `SendInput` events, and check `GetClipboardSequenceNumber()` before restoring.
- `src/SmartSnippets.App/Popup/PopupWindow.xaml.cs:259–340,378–485` relies on Win32 DWM cloaking, `HMONITOR` work-area centering (`PopupPlacement.cs:12–24`), `FlyoutShowMode.Transient` for `--preview-popup-sort-menu`, and `FlyoutSurface.cs:93–116` for content-free `menu_surface` receipts.
- `src/SmartSnippets.App/Controls/HoverDetailsSurface.xaml.cs:190–272` uses WinUI `ShouldConstrainToRootBounds = false` to place external hover/keyboard previews beside the popup (`HoverDetailsPlacement.PlaceBesideWindow`); on Uno Skia/AppKit, unconstrained XAML popups may clip to the host `NSWindow` unless backed by an 018-validated companion borderless `NSPanel`.
- `src/SmartSnippets.App/Services/StartupRegistration.cs:9–70` writes `"<exe>" --background` to `HKCU\...\Run`, and `Program.cs:25` checks `--background`. On macOS, `SMAppService.mainApp` launches the `.app` bundle without custom CLI arguments.
- `src/SmartSnippets.App/Services/TrayIcon.cs:13–64` and `AppHost.cs:85,484–493` use Win32 `Shell_NotifyIcon` and `PInvoke.MessageBox`.
- `docs/research/016-uno-native-aot-repair-and-data-probes.md:21–24,45–56` proves an ad-hoc-signed `osx-arm64` `.app` bundle structure (`Contents/MacOS/`, `Contents/Resources/` with `libe_sqlite3.dylib`) and basic `NSWindow` smoke, while leaving native shortcut/AX/pasteboard/multi-window flows unimplemented in 018.

## 4. Design

### 4.1 Approach

1. **Native capability & permission layer (`Platforms/MacOS/Interop/`):** Bind AppKit, ApplicationServices (`AXIsProcessTrustedWithOptions`, `AXUIElement*`), Carbon `RegisterEventHotKey` (or the 018-validated non-Input-Monitoring API), CoreGraphics (`CGEventCreateKeyboardEvent`, `CGEventPost`, `CGWindowListCopyWindowInfo`), ServiceManagement (`SMAppService`), and UserNotifications via Native AOT-safe `[LibraryImport]` / Objective-C selector P/Invokes—zero reflection. Expose a live `IMacPermissionMonitor` that checks Accessibility trust before capture/dispatch and surfaces actionable Settings/Popup guidance (deep-linking to System Settings → Privacy & Security → Accessibility without auto-retrying uncertain pastes).
2. **Exact-window target identity (`MacTargetCapture` + `MacTargetProbe`):** Before activating Smart Snippets' `NSPanel`/`NSWindow`, query the frontmost non-self `NSRunningApplication` and its focused window (`kAXFocusedWindowAttribute` paired with exact window number / structural `CFHash` + bounds/role check as proven in 018 FG4) plus the owning `NSScreen` (`visibleFrame` in backing/logical coordinates). Revalidate via `TargetIdentityProbe` both before pasteboard mutation and immediately adjacent to `CGEventPost`: if Accessibility was revoked, the app changed, or another window of the **same PID** is now focused, abort (`TargetClosed` / `FocusFailed`) with zero paste.
3. **Outcome-safe `MacPasteboardDispatcher`:** Inspect `NSPasteboard.generalPasteboard.pasteboardItems` eagerly:
   - Classify eager data UTIs (`public.utf8-plain-text`, `public.rtf`, `public.html`, `public.png`, `public.tiff`, `public.file-url`, custom eager PLists/data) within the 64 MB cap.
   - Detect lazy/promised providers, file promises (`NSFilesPromisePboardType` / `com.apple.pasteboard.promised-file-*`), empty/unreadable data for a declared UTI, or >64 MB totals → increment `UnsavedCount` and return `SwapStatus.NotPreserved` **before** calling `clearContents()` unless `AllowClipboardLoss` is true.
   - When swapping, write standard community privacy markers (`org.nspasteboard.TransientType`, `org.nspasteboard.ConcealedType`, `org.nspasteboard.AutoGeneratedType`) alongside `public.utf8-plain-text` (normalized per 018-verified macOS `ILineEndingPolicy`), record `changeCount`, post `⌘V` (key-down/up with modifier cleanup), and after `RestoreDelay` restore saved `NSPasteboardItem`s **only if** `generalPasteboard.changeCount == recordedChangeCount` (returning `RestoreStatus.ClipboardChanged` otherwise). Disclose truthfully in UI text that Universal Clipboard / third-party managers without `nspasteboard.org` support cannot be guaranteed.
4. **Desktop shell, lifecycle & companion surfaces:**
   - **Single ownership & login item:** Acquire an exclusive `flock` + owner-only Unix domain IPC socket under `~/Library/Application Support/SmartSnippets/` (`AppPaths`) to redirect second-launch `--page`/`--preview-popup*` requests without stealing focus. Register `SMAppService.mainApp`; detect login-item / background launch via launch AppleEvent / `NSApplicationActivationPolicyAccessory` transition so login startup stays hidden in the menu bar without needing `--background`, while Finder/Dock reopen reveals `MainWindow`.
   - **Menu bar, alerts & file panels:** Implement `NSStatusItem` (`Open Smart Snippets`, `Show popup`, `Settings`, `Quit`—no admin item), `NSAlert` for `IStartupErrorReporter`, `NSOpenPanel`/`NSSavePanel` wired to `ISnippetDocumentService` + `AtomicFileWriter`, and either Uno's native popup or an 018-validated child non-activating `NSPanel` (`NSWindowStyleMaskNonActivatingPanel`) for beside-window `HoverDetailsSurface` and `--preview-popup-sort-menu`.

### 4.2 Alternatives considered

| Option | Pros | Cons | Verdict |
|---|---|---|---|
| A — AppKit/AX exact-window probe + eager `NSPasteboard` classification + `CGEvent` `⌘V` + `SMAppService`/IPC | Designed to enforce same-PID window safety, preserve supported eager clipboards, and run under strict AOT | Requires Accessibility trust, careful CF/ObjC lifetime handling, and 018 empirical proof | **Chosen** — aligns with AC2/AC3/AC5/AC6, subject to 018 FG3–FG5 validation |
| B — PID-only `NSRunningApplication.activate` + plain-string pasteboard backup | Much simpler P/Invoke surface | Silently pastes into wrong window of multi-window apps and destroys rich/image clipboards | Rejected — violates master `017 §4.3` |
| C — AppleScript `System Events` `keystroke "v" using command down` | Avoids `CGEvent` P/Invokes | Requires Automation TCC prompts per host/System Events, slow process spawn, fragile | Rejected |
| D — Direct `AXValueAttribute` text mutation instead of `⌘V` | Avoids pasteboard swap | Fails in Electron/browsers/terminals/custom editors; inconsistent undo history | Rejected as primary; `⌘V` matches desktop contract |

### 4.3 Data / config / API model

- **macOS paths (`AppPaths` on macOS):** Root `~/Library/Application Support/SmartSnippets` (candidate `SmartSnippets.UnoDev` until 021 release packaging); logs at `~/Library/Logs/SmartSnippets/app.log` (or `<Root>/logs` when `--profile-root` is set for isolated QA).
- **macOS settings keys (`SettingsKeys`):** `macos_hotkey_modifiers`, `macos_hotkey_key` (default `⌘⇧Space` or 018-validated conflict-free gesture; displays `⌃⌥⇧⌘` / `Cmd+Shift+Space` per `IShortcutPlatform`). Windows keys remain untouched.
- **Bundle metadata (`Info.plist`):** `LSUIElement` / dynamic `NSApplicationActivationPolicy` transition for background menu-bar operation, `NSAppleEventsUsageDescription` omitted unless needed, minimum `LSMinimumSystemVersion` set to the 018-tested Apple Silicon macOS floor.

### 4.4 Sequence diagram of the new hot path

```text
Global Shortcut (Carbon/AppKit) or NSStatusItem "Show popup"
  → MacTargetCapture.CaptureFrontmost() [before SmartSnippets activates]
    ├─ Self or no window → log hotkey_ignored_own_or_no_window; stop
    ├─ AX untrusted → open popup in permission-guidance state (Target capability blocked); zero paste
    └─ Valid → InsertionTarget(PID + AXWindowId + NSScreenId + unlogged DisplayName)
         → PopupShowCoordinator → MacPopupWindow (centered in NSScreen.visibleFrame)
           → User selects snippet (Enter / click / MainWindow InsertRow)
             → Hide popup → InsertionService.InsertAsync
               → MacTargetProbe.Check(target) [AX trusted + window exists + same PID + not self]
               → MacPasteboardDispatcher.RestoreFocusAsync [activate app + AXRaise exact window]
               → ReleaseHeldModifiers → TrySwapClipboard [eager UTI snapshot + Transient/Concealed markers + LF/018 text + changeCount]
               → MacTargetProbe.IsTargetForeground(target) [exact PID + focused AXWindowId == captured]
               → CGEventPost(⌘V) → [Positively posted ONLY: Store.RecordUsage; target receipt verified via owned fixture] → wait RestoreDelay → restore if changeCount unchanged
Parallel paths checked:
  - Main-row Insert → PopupViewModel.InsertFromMainAsync → same MacPasteboardDispatcher path.
  - Explicit Copy → NSPasteboard plain UTF-8 write without transient marker or usage credit.
  - --preview-popup / --preview-popup-sort-menu → non-activating preview + Transient sort menu + menu_surface receipt; zero pasteboard/CGEvent calls.
  - SMAppService login launch → detected without --background; starts in menu bar without opening MainWindow.
```

### 4.5 Surface list

| Surface | Change | Task |
|---|---|---|
| `Platforms/MacOS/Interop/*` & `MacPermissionService` | Native AOT Objective-C/CoreFoundation/Carbon/AX/CGEvent bindings & Accessibility status/guidance | T1 |
| `MacHotkeyService`, `ShortcutRecorder` (macOS), `SettingsPage` | `⌃⌥⇧⌘` capture/validation, conflict feedback, `macos_hotkey_*` persistence, hidden admin controls | T2 |
| `MacTargetCapture`, `MacTargetProbe`, `MacPasteboardDispatcher` | Exact-window AX identity, eager UTI snapshot/refusal, `nspasteboard.org` markers, `⌘V`, `changeCount` restore | T3 |
| `MacPopupWindow`, `HoverDetailsSurface` companion `NSPanel`, `FlyoutSurface` | Multi-screen `visibleFrame` centering, warmup/cloak equivalent (`alphaValue`/orderOut), beside-window hover, QA sort-menu | T4 |
| `MacStatusItem`, `MacStartupRegistration`, `MacSingleInstanceIpc`, `MacDocumentService`, `MacStartupErrorReporter` | Menu bar, `SMAppService` background login launch, CLI redirect socket, `NSOpenPanel`/`NSSavePanel`, `NSAlert` | T5 |

## 5. Impact and risk

| Question | Answer |
|---|---|
| State management — what survives a crash mid-operation? | SQLite transactions and `AtomicFileWriter` survive crashes; transient pasteboard swaps carry `org.nspasteboard.TransientType` and never write plaintext recovery files to disk (`018 FG5`). |
| Data consistency — orphans, races, double-apply? | Single-instance `flock` prevents dual-process SQLite access; UI-thread `SnippetStore` + single-flight `InsertionService` prevent duplicate pastes or usage double-counts. |
| User experience — root problem or symptom; any surprise? | Target UX: native `⌘` shortcuts (`⌘Enter` save, `⌘F` search, `⌘V` manual-paste hint), native menu bar, and clear Accessibility guidance instead of silent paste failure (subject to 020/021 verification). |
| Backward compatibility — existing data / sessions / configs? Migration? | Reuses shared JSON parser to import Windows exports; separate `macos_hotkey_*` settings keys avoid clobbering Windows keys. |
| Error recovery — what happens on failure; can it recover? | Revoked Accessibility, closed window, switched window in same PID, or promised pasteboard data aborts cleanly and reopens popup footer with `Copy text` / `Paste anyway` options. |
| Logging & debugging — enough to diagnose in the field? | `AppLog` records UTC timestamps and allowlisted tokens (`ax_trusted`, `target_check`, `clipboard_swap`, `foreground_gate`, `paste_sent`, `menu_surface`, `startup_mode`) without snippet text, titles, or paths; fixture scans check captured app/OS logs without claiming blanket control over Apple framework logs. |
| Edge cases — empty, huge, repeated, concurrent, interrupted? | Handles Retina/non-Retina multi-monitor moves, Spaces/full-screen rejection when unfocused, sleep/wake shortcut retention, >64 MB/lazy pasteboards, and third-party clipboard manager edits mid-paste. |

**Risks:**
- **R1 — Ad-hoc vs Developer ID TCC Accessibility identity drift** → Ad-hoc rebuilds invalidate macOS TCC entries; isolate dev bundle ID (`com.smartsnippets.unodev`) and re-verify under Developer ID signing in 021.
- **R2 — Apps without standard `kAXFocusedWindowAttribute` / Electron quirks** → Validate fallback window-number matching against 018's two-window fixture and 021's target-app matrix; fail closed (`FocusFailed`) rather than guessing PID-wide.
- **R3 — Uno Skia/AppKit external popup clipping** → Use 018-proven borderless non-activating companion `NSPanel` for beside-window `HoverDetailsSurface` if XAML `ShouldConstrainToRootBounds=false` clips.

**Rollback:** Keep macOS release gated behind 021; Windows `SmartSnippets.App` remains unaffected, and candidate macOS login items can be unregistered via `SMAppService.mainApp.unregisterAndReturnError` during cleanup.

## 6. Tasks

### T0 — Fold 018 macOS native findings into adapter constants (AC2, AC3)
- **Files:** `src/SmartSnippets.Uno/Platforms/MacOS/MacPlatformConfig.cs`
- **Change:** Pin the 018-verified shortcut registration API, macOS `ILineEndingPolicy` (`LF` vs `CRLF`), companion `NSPanel` mode for external hover, and tested `LSMinimumSystemVersion`. Stop and escalate if any 018 FG3–FG5 item failed or remained `BLOCKED`.
- **Verify:** `osx-arm64` Release AOT build succeeds with 0 warnings.
- **Test that dies if this breaks:** `MacPlatformConfigTests.RequiresVerifiedFloorAndLineEndingPolicy`.

### T1 — Native AOT interop & Accessibility permission service (AC2, AC3, AC5)
- **Files:** `src/SmartSnippets.Uno/Platforms/MacOS/Interop/*`, `MacPermissionService.cs`
- **Change:** Implement `[LibraryImport]` bindings with deterministic `CFRelease`/ARC-safe wrappers and `IMacPermissionMonitor` (`IsAccessibilityTrusted`, prompt/open System Settings helper, live revocation check).
- **Verify:** `dotnet test tests/SmartSnippets.Presentation.Tests` + `osx-arm64` AOT publish with zero `IL2xxx`/`IL3xxx` warnings.
- **Test that dies if this breaks:** `MacPermissionFlowTests.DeniedOrRevokedAccessibility_SurfacesActionableGuidanceAndBlocksDispatch`.

### T2 — macOS global shortcut manager, recorder & `⌘` UI bindings (AC2, AC5)
- **Files:** `src/SmartSnippets.Uno/Platforms/MacOS/MacHotkeyService.cs`, `Controls/ShortcutRecorder.xaml.cs`, `Presentation/ViewModels/SettingsViewModel.cs`
- **Change:** Register/unregister global shortcuts without Input Monitoring, persist `macos_hotkey_*`, format `⌃⌥⇧⌘` keycaps, wire `⌘Enter`/`⌘C`/`⌘F` accelerators, and hide Windows admin-restart controls via `IPlatformCapabilities`.
- **Verify:** Unit + host integration tests for valid/invalid/conflicting gestures and settings roundtrip.
- **Test that dies if this breaks:** `MacShortcutTests.RegistersReplacesAndPersistsCommandGesture_RejectingModifierOnlyKeys`.

### T3 — Exact-window target probe & outcome-safe `MacPasteboardDispatcher` (AC2, AC5, AC6)
- **Files:** `src/SmartSnippets.Uno/Platforms/MacOS/MacTargetCapture.cs`, `MacTargetProbe.cs`, `MacPasteboardDispatcher.cs`
- **Change:** Capture frontmost PID + exact AX window token + `NSScreen`; enforce same-PID wrong-window rejection; snapshot eager multi-item/multi-UTI payloads up to 64 MB; refuse lazy/promised/oversized items (`ClipboardNotPreserved`) without `clearContents()` in default mode; write `nspasteboard.org` privacy markers; post `⌘V`; restore iff `changeCount` matches.
- **Verify:** Owned two-window macOS fixture tests (in authorized screen slot) + deterministic pasteboard classification unit tests.
- **Test that dies if this breaks:** `MacTargetIdentityTests.SamePidSecondWindow_IsRejectedBeforePaste`, `MacPasteboardSafetyTests.PromisedOrOversizedItem_ReturnsNotPreservedWithoutMutatingPasteboard`, and `MacPasteboardSafetyTests.ExternalOwnerWriteDuringDelay_SkipsRestoreAndPreservesNewClipboard`.

### T4 — Multi-screen popup warmup, beside-window hover `NSPanel`, & QA preview seam (AC2, AC6)
- **Files:** `src/SmartSnippets.Uno/Platforms/MacOS/MacPopupWindowAdapter.cs`, `MacCompanionHoverPanel.cs`, `Popup/PopupWindow.xaml.cs`
- **Change:** Integrate `PopupShowCoordinator` with AppKit offscreen/alpha-zero warmup (preventing unpainted flash), `NSScreen.visibleFrame` placement across Retina/secondary displays, beside-window `HoverDetailsSurface` + arrow-key preview, and non-activating `--preview-popup` / `--preview-popup-sort-menu` emitting `menu_surface`.
- **Verify:** Authorized macOS QA harness run checking frontmost PID/window preservation during `--preview-popup-sort-menu` and `menu_surface` log receipt.
- **Test that dies if this breaks:** `MacPopupSurfaceTests.PreviewAndSortMenu_PreserveExternalForegroundWindowAndNeverDispatchPaste`.

### T5 — Menu bar, `SMAppService` login item, single-instance IPC, `NSAlert` & file panels (AC2, AC5, AC6)
- **Files:** `src/SmartSnippets.Uno/Platforms/MacOS/MacStatusItem.cs`, `MacStartupRegistration.cs`, `MacSingleInstanceIpc.cs`, `MacStartupErrorReporter.cs`, `MacSnippetDocumentService.cs`
- **Change:** Wire `NSStatusItem`, `flock` + Unix socket CLI redirect, `SMAppService.mainApp` background startup detection without `--background`, `NSAlert` fatal DB dialog, `UNUserNotificationCenter`/banner notices, and `NSOpenPanel`/`NSSavePanel` + `AtomicFileWriter`.
- **Verify:** Multi-launch redirect test, login-item state toggle test, startup corrupt-DB `NSAlert` test, and JSON import/export roundtrip test.
- **Test that dies if this breaks:** `MacLifecycleTests.LoginLaunchStartsHiddenInMenuBar_AndSecondLaunchRedirectsOptionsWithoutDuplicateOwner`.

### T6 — Wiring audit (AC2, AC3, AC5, AC6)
- **Files:** `tests/SmartSnippets.Presentation.Tests/Wiring/MacDesktopWiringAuditTests.cs`
- **Change:** Audit every macOS producer → `UnoAppHost` registration → VM/XAML consumer → native callback & shutdown disposal (`MacHotkeyService`, `MacStatusItem`, `MacSingleInstanceIpc`, `MacCompanionHoverPanel`, `MacPasteboardDispatcher.RestorePendingAsync`), verifying zero stubbed capabilities remain and parallel `Insert` / `Copy` / `Preview` paths obey shared policies.
- **Verify:** Startup-to-Quit receipt trace on `osx-arm64` native bundle (`app_started` → `insertion_ready` → `menu_surface` → `exit_unregistered`) + leak scan.
- **Test that dies if this breaks:** `MacDesktopWiringAuditTests.ZeroUnsupportedStubsRemainAndAllLifecycleHandlesDisposeOnQuit`.

## 7. Test strategy

- **Unit:** Pasteboard UTI classifier (eager vs promised vs >64 MB), `changeCount` ownership state machine, `macos_hotkey_*` gesture parser/formatter, `NSScreen` coordinate-flip math (bottom-left Cocoa pt → top-left `PixelRect`), and login-launch policy.
- **Integration:** Real `UnoAppHost` + SQLite macOS composition tests with fake native interop seam covering: (1) shortcut → popup → paste → usage +1, (2) same-PID window switch → `FocusFailed` + usage +0, (3) promised clipboard → `ConfirmClipboardLoss` → `PasteAnyway`, (4) second-instance socket `--preview-popup-sort-menu` redirect.
- **Manual runbook (Apple Silicon macOS, authorized exclusive screen slot + isolated temp profile + owned two-window synthetic target):**

| # | Step | Expected |
|---|---|---|
| 1 | Publish `osx-arm64` Native AOT `.app`, verify `file` (`Mach-O 64-bit executable arm64`), `libe_sqlite3.dylib`, and `codesign --verify --deep --strict` | Native arm64 bundle, 0 IL warnings, valid signature |
| 2 | Launch with Accessibility **denied**; press shortcut over owned target Window A and attempt Insert | Actionable Accessibility guidance shown; Window A untouched; pasteboard untouched; usage = 0 |
| 3 | Grant Accessibility; place rich RTF+PNG on pasteboard; invoke shortcut in Window A → Enter; repeat switching focus to same-PID Window B mid-flight | Window A gets exact snippet once (usage = 1) and RTF+PNG restores; Window B attempt aborts (`FocusFailed`, usage stays 1) |
| 4 | Place lazy/promised Finder file promise on pasteboard; attempt Insert → Cancel, then Insert → `Paste anyway` | Default refuses (`ClipboardNotPreserved`, promise intact); `Paste anyway` inserts once and warns of clipboard replacement |
| 5 | Enable Start at login (`SMAppService`), simulate login launch (no `--background`), invoke `--preview-popup --preview-popup-sort-menu`, then Quit | Starts hidden in menu bar; preview sort menu opens without stealing frontmost app; `menu_surface` logged; zero fixture strings in `app.log` |

## 8. Rollout / phasing

Delivered on feature branches gated by 019's `osx-arm64` AOT CI job. Remains pre-release (`SmartSnippets.UnoDev` bundle ID) until Plan 021 completes Developer ID signing, notarization, target-app matrix testing, and Windows cutover.

## 9. Open questions / dependency & assumption register

| ID | Dependency / Assumption | Status | Owner / Gate |
|---|---|---|---|
| D1 | 018 FG3/FG4 proof of macOS global shortcut API, `AXUIElement` exact-window ID, companion `NSPanel` hover, and newline expectation | **Pending** (not yet implemented/run in 018; `016`) | Implementer / 018 completion |
| D2 | Production macOS Uno AOT package/build fix replacing `016` probe-only 37-file vendor source overlay | **Pending owner decision** | User + implementer before 019/020 |
| D3 | Apple Silicon disk headroom (~7.7 GiB currently free on test Mac) & Developer ID certificate for stable TCC testing | **Constrained / Pending** | User + operator before T3–T5 live TCC runs |

## 10. Approval log

| Date | Event | Notes |
|---|---|---|
| 2026-10-30 | Requirement brief confirmed (G1) | Confirmed in master plan `017` (Apple Silicon macOS only, native integrations, strict Native AOT) |
| 2026-10-30 | Provisional candidate drafted | Written to `docs/drafts/uno-019-025/`; depends on 018 & 019 completion; **unapproved, not implementation-ready** |
