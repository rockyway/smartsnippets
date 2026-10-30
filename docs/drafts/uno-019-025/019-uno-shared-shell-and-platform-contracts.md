# 019 — Uno shared shell and platform contracts

**Date:** 2026-10-30
**Status:** Provisional draft—dependency evidence pending; unapproved, not implementation-ready
**Size:** L
**Area:** `src/SmartSnippets.Core`, `src/SmartSnippets.Presentation`, `src/SmartSnippets.Uno` (new), `tests/`, `.github/workflows/`
**Requirement brief confirmed:** 2026-10-30 (G1, master 017)
**Master:** [017 — Master plan: Uno cross-platform migration](../../plan/017-master-uno-cross-platform-migration.md)
**Canonical placeholder:** [019 placeholder](../../plan/019-uno-shared-shell-and-platform-contracts.md) (unchanged until 018 completes)

---

## 1. Goal

Introduce the production multi-targeted `SmartSnippets.Uno` shell, refactor platform-coupled contracts in `Core` and `Presentation` into capability-aware portable boundaries, sanitize production exception logging, and stand up a fully wired unpackaged Windows x64 head plus provisional Windows/macOS Native AOT CI gates—while keeping `SmartSnippets.App` and the live Windows user profile untouched until Plan 021 cutover.

## 2. Requirement (as confirmed at G1)

Excerpted from the confirmed G1 brief in `docs/plan/017-master-uno-cross-platform-migration.md:19–55`:

- **Problem:** Production `Core`/`Presentation` contracts embed Win32 handles, virtual keys, CRLF normalization, elevation assumptions, path-only file pickers, and raw exception-message logging, while no production Uno shell or cross-platform AOT CI gate exists.
- **In scope:** Single multi-targeted `src/SmartSnippets.Uno/` project (unpackaged Windows WinAppSDK `win-x64` + macOS `osx-arm64` compilation/shell skeleton); portable contracts for target identity, newline policy, shortcuts, capabilities, startup failure UI, and document descriptors; Windows native adapters under Uno; production log sanitization; UI-thread store/cache preservation; isolated candidate identity/profile during coexistence; provisional Windows + macOS Native AOT CI gates.
- **Out of scope / non-goals:** macOS native hotkey/AX/pasteboard/login-item parity (020), replacing `SmartSnippets.App` or migrating the live `%LOCALAPPDATA%\SmartSnippets` profile (021), mobile heads (022–025), Intel macOS, Windows ARM64, cloud sync, password snippets (`016`).
- **Users and surfaces:** Developers/CI exercising the candidate Windows Uno head and macOS shell skeleton against isolated profiles; end users remain on `SmartSnippets.App`. All main pages, popup, previews, tray, settings, and import/export are wired on Windows Uno.
- **Behaviour:** Happy path preserves Windows CRUD, search, popup warmup/recovery, original-target paste, explicit Copy, previews, theme/palette/text-size, and JSON transfer; unsupported capabilities or denied permissions fail closed with actionable platform-accurate messages and content-free logs.
- **Constraints and assumptions:** Conditional on Plan 018 completion (FG1–FG6) **and** explicit owner decisions on: (a) macOS Uno AOT dependency strategy (research `016` source-built overlay at `da217df23...` is probe-only, not production-approved), (b) Windows Uno package graph (`ProbeManualWindowsPackages` vs default Uno helper `IL2059`), and (c) early mobile Native AOT risk (`NETSDK1147` unrun on hosts; official Microsoft `.NET for Android` `XA1040` marks `PublishAot=true` experimental/non-production: https://learn.microsoft.com/en-us/dotnet/android/messages/xa1040 — no silent Mono AOT substitution or warning suppression).
- **Acceptance criteria:** Maps to master **AC1** (Windows functionality/data compatibility), **AC3** (desktop Native AOT zero-warning publish), **AC5** (portable contracts, failure handling, usage accounting), and **AC6** (offline, zero snippet/label/tag/title/query/path log leakage).
- **Decisions made:** Retain `Core`/`Presentation` without Generic Host DI (`017 §3`); native WinAppSDK on Windows rather than Skia (`017 §4.1`); Apple Silicon only on macOS (`017 §2`).

## 3. Current state (as-built)

- `src/SmartSnippets.App/SmartSnippets.App.csproj:1–33` is a single-target WinUI 3 `win-x64` Native AOT executable; `SmartSnippets.slnx:1–12` and `Directory.Packages.props:1–15` contain no Uno packages.
- `src/SmartSnippets.App/Program.cs:13–47` registers Windows `AppInstance` key `"SmartSnippets.Main"` and initializes `AppLog` under default `AppPaths` (`%LOCALAPPDATA%\SmartSnippets`, `src/SmartSnippets.Core/AppPaths.cs:5–19`). Running a candidate Uno head with the same key/path would collide with a live Windows install.
- `src/SmartSnippets.App/AppHost.cs:69–135,137–277` explicitly composes `Migrator`, `SnippetStore`, `SettingsRepository`, Win32 `MessageWindow`, `ClipboardPasteDispatcher`, `HotkeyManager`, `PopupWindow`, `MainWindow`, and `TrayIcon` on the UI `DispatcherQueue` thread, and reports startup failure via Win32 `PInvoke.MessageBox` (`AppHost.cs:484–493`).
- `src/SmartSnippets.Core/Store/SnippetStore.cs:21,41,56,316` binds `_ownerThreadId = Environment.CurrentManagedThreadId` at construction and asserts UI-thread ownership. `src/SmartSnippets.Presentation/ViewModels/PopupViewModel.cs:46–58,175–232` coalesces `Store.Changed` rebuilds onto `_schedule` (`_ui.Post`) and enforces `EnsureCurrent()` before reveal.
- `src/SmartSnippets.Presentation/Services/InsertionContracts.cs:4,24–29`, `InsertionBoundary.cs:3–63`, `TargetIdentityProbe.cs:3–21`, and `InsertionService.cs:40` hardcode `nint WindowHandle`/`MonitorHandle`, `LineEndings.ToCrLf(snippet.Text)`, `TargetCheck.Elevated`, and `FocusPath.AttachThreadInput`.
- `src/SmartSnippets.App/AppHost.cs:183,322–336` and `PopupViewModel.cs:240,247–263,317–370` converge hotkey, tray, popup Enter/row, and main-row `Insert` onto `PopupViewModel` → `IInsertionService`, keeping `CopyRow` (`PopupViewModel.cs:240`, `SnippetsPageViewModel.cs:135`) and `IsPreview` (`PopupViewModel.cs:324`) strictly non-inserting.
- `src/SmartSnippets.Core/Settings/HotkeyGesture.cs:6–64`, `AppSettings.cs:21–28`, `SettingsViewModel.cs:11,18,93,242–259`, and `UiText.cs:88,125,141,165` hardcode Win32 `MOD_*`/`VK_*` validation, `"Ctrl+"/"Win+"` splitting, unconditional `!_elevation.IsElevated` admin-restart UI, and `"Ctrl+Enter"`/`"Ctrl+V"` strings.
- `src/SmartSnippets.Presentation/Services/ISnippetFileService.cs:4–20`, `SnippetPortingViewModel.cs:47,91–94,112`, `SnippetFileRules.cs:10–11`, and `UiText.cs:215` assume filesystem `string` paths, `Path.GetExtension`, and atomic same-folder replacement wording.
- `src/SmartSnippets.Core/Logging/AppLog.cs:40–48` appends raw `exception.Message` whenever `exception != null`. Confirmed production leak vectors include `Migrator.cs:59,88` (plus `:89` embedding `ex.Message` into `MigrationException` logged at `AppHost.cs:118`), `SnippetsPageViewModel.cs:292,334`, `AppHost.cs:85,118,123,128,368,402,455,479`, `MainWindow.xaml.cs:222,287`, `Program.cs:72,89`, `ClipboardService.cs:30`, and `StartupRegistration.cs:28,50,66`.
- `.github/workflows/ci.yml:1–61` and `scripts/publish-aot.ps1:1–40` test `Core`/`Presentation` and publish `SmartSnippets.App` (`win-x64` AOT) only.

## 4. Design

### 4.1 Approach

1. **Capability & contract extraction in `Core`/`Presentation`:** Replace raw Win32 handle tuples in `InsertionTarget` with an opaque `PlatformTargetToken` (`ICapturedTarget` carrying process ID, opaque window/display tokens, and never-logged `DisplayName`). Parameterize `InsertionService` with `ILineEndingPolicy` (`CrLf` on Windows, `Lf` or observed 018 fixture policy on macOS) and generalize `FocusPath`/`TargetCheck` (`PrivilegeOrPermissionBlocked` alongside `Elevated` mapping so Windows UI wording stays exact while macOS permission refusal maps cleanly). Introduce `IPlatformCapabilities` (supports elevation restart, shortcut modifier symbols/defaults, primary command key `Ctrl` vs `⌘`, atomic export guarantee) and `IShortcutPlatform` so existing Windows `HotkeyModifiers`/`HotkeyKey` SQLite keys (`AppSettings.cs:45–46`) load unchanged on Windows while macOS uses platform-specific keys/defaults in 020. Refactor `ISnippetFileService` to return `DocumentSelection` descriptors (`DisplayName`, `HasJsonType`, opaque token, `SupportsAtomicReplace`) rather than raw paths, keeping `AtomicFileWriter` on desktop.
2. **Production log sanitization:** Change `AppLog.Error` (`AppLog.cs:40–48`) so `exception.Message` and user paths are **never** formatted into `app.log`; emit only allowlisted event names, `exception.GetType().Name`, `HResult`, `SqliteException.SqliteErrorCode`/`SqliteExtendedErrorCode`, and sanitized call-site tokens. Audit and clean `Migrator.cs:59,88–89` (keep user-facing `MigrationException` safe without raw filesystem paths in logs), `SnippetsPageViewModel.cs:292,334`, and all host `AppLog.Error` call sites.
3. **Production Uno shell & coexistence:** Create `src/SmartSnippets.Uno/` targeting the 018-validated unpackaged Windows WinAppSDK TFM and macOS AppKit/Skia `osx-arm64` TFM (using only owner-approved upstream or explicitly approved dependency pins—never silently copying `prototypes/uno-desktop/vendor/`). Port XAML views/controls and Windows Win32 services (`Platforms/Windows/`) beneath a shared `UnoAppHost` composition root that preserves single-threaded UI `DispatcherQueue` ownership (`SnippetStore` + `PopupViewModel._schedule` + `PopupShowCoordinator` + `RecoverablePopupHost` + `DeferredPopupHost`). Until 021 cutover, `SmartSnippets.Uno` uses an isolated candidate instance key (`SmartSnippets.UnoCandidate`), candidate root (`%LOCALAPPDATA%\SmartSnippets.UnoDev` or `--profile-root` in QA), and non-conflicting default dev shortcut unless explicitly invoked in isolated harness tests. macOS compiles cleanly with explicit `PlatformNotSupported`/unconfigured capability stubs that fail closed (never fake `Success`) until 020.

### 4.2 Alternatives considered

| Option | Pros | Cons | Verdict |
|---|---|---|---|
| A — Capability-aware contracts + shared `SmartSnippets.Uno` coexisting beside `SmartSnippets.App` | Designed to preserve Windows SQLite compatibility, native WinUI rendering, UI-thread invariants, and instant rollback until 021 | Temporarily maintains legacy `App` and Uno Windows adapters | **Chosen** — matches master `017 §4.1` with zero live-profile risk, subject to 018 evidence |
| B — Replace `SmartSnippets.App` in place during 019 | Avoids temporary coexistence | Violates staged cutover; strands Windows releases before 020/021 parity and backup gates | Rejected |
| C — Keep Win32 `InsertionTarget`/`HotkeyGesture`/`ISnippetFileService` and `#if` inside view-models | Fewer interface edits | Breaks Linux-testable platform-agnostic `Presentation` rule (`AGENTS.md`) and leaks OS details across VMs | Rejected |
| D — Adopt probe macOS source overlay (`016`) silently in production | Unblocks `osx-arm64` AOT immediately | Carries a 37-file patched vendor fork without owner approval | Rejected — requires explicit owner decision post-018 |

### 4.3 Data / config / API model

- **SQLite schema:** Unchanged (schema v2). Windows `HotkeyModifiers`/`HotkeyKey` (`SettingsKeys.cs`) remain backward-compatible integers; `IShortcutPlatform` validates/formats gestures per OS, reserving separate macOS settings keys (`macos_hotkey_modifiers`, `macos_hotkey_key`) for 020 so importing/opening fixtures never corrupts Windows gestures.
- **Portable contracts (`Presentation/Services/`):**
  - `InsertionTarget(PlatformWindowId Window, uint ProcessId, PlatformDisplayId Display, string DisplayName)`
  - `IPlatformCapabilities { bool SupportsAdminRestart; PrimaryModifierKind PrimaryModifier; bool GuaranteesAtomicFileReplace; }`
  - `ISnippetDocumentService` (replaces path-coupled `ISnippetFileService`): `PickImportAsync() -> DocumentReadHandle?`, `PickExportAsync(string baseName) -> DocumentWriteHandle?`, `OpenReadAsync`, `WriteAsync` returning `DocumentWriteOutcome` (`WrittenAtomic`, `WrittenNonAtomic`, `FailedUnchanged`, `FailedOutcomeUnknown`).
  - `IStartupErrorReporter`: native modal startup failure surface (`MessageBox` on Windows; `NSAlert` wired in 020).
- **Logging contract (`AppLog`):** `AppLog.Error(string eventName, Exception? ex = null, string? safeDetail = null)` records `type=<Name> hresult=0x<X8> [sqlite=<code>]` only—never `ex.Message` or paths.

### 4.4 Sequence diagram of the new hot path

```text
Entry (Hotkey / Tray / MainWindow InsertRow) → UnoAppHost (UI DispatcherQueue)
  → ITargetCapture.CaptureForeground() → InsertionTarget(opaque Window+PID+Display)
    → EnsurePopup() → PopupViewModel.EnsureCurrent() [flushes coalesced UI-thread Store.Changed cache]
      → PopupShowCoordinator → RecoverablePopupHost / PopupWindow
        → User activates row / Enter (or MainWindow InsertRowAsync)
          ├─ IsPreview == true → Hide(); log popup_closed reason=preview_selection (NO clipboard/dispatch/usage)
          └─ IsPreview == false → InsertionShutdownCoordinator → InsertionService.InsertAsync
               → ILineEndingPolicy.Normalize(snippet.Text) → ITargetProbe.Check(target)
                 → IPasteDispatcher.RestoreFocusAsync → TrySwapClipboard → IsTargetForeground (adjacent)
                   → SendPaste() → [Positively dispatched (DispatchState.Dispatched) ONLY: Store.RecordUsage(id); target-buffer receipt verified separately by owned fixture tests] → TryRestoreClipboard
Parallel paths checked:
  - Main-row Insert (SnippetsPageViewModel.cs:142), Popup Enter (:317), Popup row click (:328) → all hit InsertAsync.
  - Explicit Copy (SnippetsPageViewModel.cs:135, PopupViewModel.cs:240, SnippetEditorViewModel.cs:328) → IClipboardService.SetText only, zero usage.
  - QA launch (--page / --preview-popup / --preview-popup-sort-menu) → ApplyLaunchOptionsAsync without foreground steal or paste.
```

### 4.5 Surface list

| Surface | Change | Task |
|---|---|---|
| `AppLog`, `Migrator`, `SnippetsPageViewModel`, host error handlers | Strip raw `exception.Message` and file paths from logs; preserve safe user-facing dialogs | T1 |
| `InsertionContracts`, `InsertionBoundary`, `InsertionService`, `HotkeyGesture`, `ISnippetFileService`, `UiText`, `SettingsViewModel` | Capability-aware target, newline, shortcut, elevation, Ctrl/⌘ hints, and document-handle contracts | T2 |
| `src/SmartSnippets.Uno/` shared XAML + `UnoAppHost` + `Platforms/Windows/*` | Production Uno project, unpackaged Windows head, candidate isolation, UI-thread store/popup wiring | T3, T4 |
| `Platforms/MacOS/*` compile skeleton in `SmartSnippets.Uno` | Fail-closed capability stubs so `osx-arm64` Native AOT compiles cleanly ahead of 020 | T4 |
| `.github/workflows/` + `scripts/publish-uno-aot.*` | Provisional Windows `win-x64` + macOS `osx-arm64` Native AOT & unsuppressed IL-warning CI gates | T5 |

## 5. Impact and risk

| Question | Answer |
|---|---|
| State management — what survives a crash mid-operation? | SQLite transactions (`SnippetStore.cs:333`) and atomic desktop `.json` temp-replace survive crashes; candidate Uno uses an isolated profile root so live Windows data cannot be touched. |
| Data consistency — orphans, races, double-apply? | Single UI-thread `SnippetStore` (`AssertOwnerThread`) + `PopupViewModel._schedule` coalescing + `Interlocked` single-flight in `InsertionService` prevent races and double-paste. |
| User experience — root problem or symptom; any surprise? | Target invariant: preserve Windows UX, keycaps, elevation restart, and messages (verified in T3/T6), while capability-driven `UiText` prepares `⌘` hints and hides Windows-only admin UI on non-Windows targets. |
| Backward compatibility — existing data / sessions / configs? Migration? | Schema v2 and Windows settings keys are designed to load unmodified from fixture copies of real v1/v2 databases; no destructive migration runs. |
| Error recovery — what happens on failure; can it recover? | Startup DB/migration failure shows `IStartupErrorReporter` and exits cleanly; unsafe clipboard returns `ClipboardNotPreserved` untouched; shutdown waits for in-flight paste restore (`AppHost.cs:434`). |
| Logging & debugging — enough to diagnose in the field? | `AppLog` emits UTC timestamp, level, allowlisted event name, `HResult`/SQLite codes, and `menu_surface` tokens while omitting `exception.Message`; synthetic-fixture scans verify app/captured process logs (without claiming OS-level streams are under `AppLog` control). |
| Edge cases — empty, huge, repeated, concurrent, interrupted? | Covered for empty/10k libraries, rapid hotkey spam during warmup, dirty editor guards on navigation/exit, cancelled pickers, and same-PID multi-window targets. |

**Risks:**
- **R1 — Upstream Uno AOT defects (`IL2059` on Windows default graph; `IL2026`/`IL3050` on macOS WebView in `016`)** → Gate T0 on 018 completion and explicit owner sign-off on package/source strategy; enforce unsuppressed `IL2xxx`/`IL3xxx` CI failure.
- **R2 — Mobile strict-AOT blocker (`XA1040` / iOS extension AOT) invalidating Uno foundation** → Require owner decision on 018 T7 findings before starting 019 implementation (`017 §6`).
- **R3 — Coexistence collision with installed `SmartSnippets.App`** → Enforce distinct candidate `InstanceKey`, profile root, and output folder until 021 cutover.

**Rollback:** `SmartSnippets.App` and `.github/workflows/ci.yml`'s legacy Windows gate remain intact; revert `src/SmartSnippets.Uno` and contract commits with zero user-profile or schema rollback needed.

## 6. Tasks

### T0 — Reconcile 018 evidence and owner dependency decisions (AC3, AC5)
- **Files:** `Directory.Packages.props`, `global.json`, `docs/plan/019-uno-shared-shell-and-platform-contracts.md` (when promoted)
- **Change:** Confirm 018 FG1–FG6 completion and record explicit owner approvals for: exact Uno.Sdk/WinAppSDK/.NET versions, Windows manual vs helper package graph, macOS upstream fix vs approved source build, and mobile `XA1040`/iOS AOT disposition. Stop if any gate is unresolved.
- **Verify:** `dotnet restore` across solution with zero `NoWarn` for `IL2xxx`/`IL3xxx`.
- **Test that dies if this breaks:** `BuildGateTests.RejectsSuppressedTrimOrAotWarnings` (inspects project/effective MSBuild properties for `SuppressTrimAnalysisWarnings`, `NoWarn` containing `IL2`/`IL3`).

### T1 — Sanitize production `AppLog`, `Migrator`, and view-model error paths (AC5, AC6)
- **Files:** `src/SmartSnippets.Core/Logging/AppLog.cs`, `src/SmartSnippets.Core/Data/Migrator.cs`, `src/SmartSnippets.Presentation/ViewModels/SnippetsPageViewModel.cs`, `src/SmartSnippets.App/AppHost.cs`
- **Change:** Remove `exception.Message` concatenation from `AppLog.Error` (`AppLog.cs:43`), replacing with structured type/`HResult`/SQLite error code metadata; sanitize `Migrator.cs:59,88–89` and `SnippetsPageViewModel.cs:292,334` so thrown/logged failures never leak DB paths, usernames, snippet content, labels, or tags.
- **Verify:** `dotnet test tests/SmartSnippets.Core.Tests` and `dotnet test tests/SmartSnippets.Presentation.Tests` sequentially.
- **Test that dies if this breaks:** `AppLogPrivacyTests.Error_NeverWritesExceptionMessageOrUserPath` and `MigratorPrivacyTests.FailedBackupAndMigration_NeverLogDatabasePathOrFixtureTokens` (negative oracle planting synthetic secret/path inside thrown `SqliteException`/`IOException`).

### T2 — Extract capability-aware portable contracts in `Core` and `Presentation` (AC1, AC5)
- **Files:** `src/SmartSnippets.Presentation/Services/InsertionContracts.cs`, `InsertionBoundary.cs`, `InsertionService.cs`, `TargetIdentityProbe.cs`, `ISnippetFileService.cs` (→ `ISnippetDocumentService.cs`), `IPlatformCapabilities.cs`, `IShortcutPlatform.cs`, `IStartupErrorReporter.cs`, `src/SmartSnippets.Core/Settings/HotkeyGesture.cs`, `src/SmartSnippets.Presentation/ViewModels/SettingsViewModel.cs`, `PopupViewModel.cs`, `SnippetPortingViewModel.cs`, `src/SmartSnippets.Presentation/Text/UiText.cs`, `src/SmartSnippets.App/*` (thin adapter updates to keep legacy build green)
- **Change:** Replace Win32-coupled handles, CRLF hardcoding (`InsertionService.cs:40`), `"Win+"` modifier splitting (`SettingsViewModel.cs:18`), unconditional `CanRestartAsAdmin` (`SettingsViewModel.cs:93`, `PopupViewModel.cs:165`), `"Ctrl+"` hints, and raw string file pickers with portable contracts; update `SmartSnippets.App` callers so existing behavior and tests remain 100% green.
- **Verify:** Sequential `dotnet test tests/SmartSnippets.Core.Tests`, `dotnet test tests/SmartSnippets.Presentation.Tests`, and `dotnet build SmartSnippets.slnx`.
- **Test that dies if this breaks:** `InsertionServiceContractTests.NormalizesNewlinesPerPlatformPolicy_AndNeverCreditsUncertainDispatch`, `SettingsViewModelCapabilityTests.HidesAdminRestartAndUsesCommandSymbolWhenCapabilityUnsupported`, `SnippetPortingViewModelTests.DocumentHandle_RejectsNonJsonAndReportsNonAtomicFailureTruthfully`.

### T3 — Scaffold `SmartSnippets.Uno` shared shell, XAML views, and Windows native adapters (AC1, AC3, AC6)
- **Files:** `src/SmartSnippets.Uno/SmartSnippets.Uno.csproj`, `SmartSnippets.slnx`, `src/SmartSnippets.Uno/App.xaml*`, `UnoAppHost.cs`, `Program.cs`, `Pages/*`, `Controls/*`, `Popup/*`, `Platforms/Windows/*`
- **Change:** Add multi-targeted `SmartSnippets.Uno` (`WindowsPackageType=None` on `win-x64`, AppKit/Skia on `osx-arm64`), port compiled `x:Bind` XAML surfaces (`MainWindow`, `SnippetsPage`, `FoldersPage`, `SettingsPage`, `PopupWindow`, `HoverDetailsSurface`, `ShortcutRecorder`, `SnippetEditor`), wire `UnoAppHost` on the UI `DispatcherQueue` (`PopupShowCoordinator`, `RecoverablePopupHost`, `DeferredPopupHost`, `--page`, `--preview-popup`, `--preview-popup-sort-menu`, `menu_surface` receipt), and isolate candidate instance key/profile root.
- **Verify:** `dotnet build src/SmartSnippets.Uno -c Release -r win-x64` with 0 warnings.
- **Test that dies if this breaks:** `UnoCompositionFlowTests.WindowsComposition_PreservesUiThreadStoreOwnership_CoalescesPopupCache_AndIsolatesPreviewFromInsertion` (asserts cross-thread store call throws/fails owner check, preview selection emits zero paste/clipboard/usage, and main-row + popup Enter share single-flight insertion).

### T4 — Wire fail-closed macOS shell skeleton for 020 handoff (AC3, AC5)
- **Files:** `src/SmartSnippets.Uno/Platforms/MacOS/*`
- **Change:** Provide compile-clean `osx-arm64` startup entry, isolated macOS app-support root (`~/Library/Application Support/SmartSnippets.UnoDev`), and explicit fail-closed capability adapters (returning actionable `Unsupported`/`NotRegistered` outcomes, never fake success stubs) ready for 020 native replacement.
- **Verify:** On Apple Silicon host/runner, fresh Release Native AOT publish for `osx-arm64` with 0 warnings.
- **Test that dies if this breaks:** `MacSkeletonContractTests.UnimplementedNativeAdapters_FailClosedWithoutClipboardMutationOrUsageCredit`.

### T5 — Provisional Windows x64 and macOS `osx-arm64` Native AOT CI gates (AC3)
- **Files:** `scripts/publish-uno-aot.ps1`, `scripts/publish-uno-aot.sh`, `.github/workflows/ci.yml`
- **Change:** Keep existing `SmartSnippets.App` main gate intact; add provisional `SmartSnippets.Uno` jobs on Windows (`win-x64`) and macOS Apple Silicon (`macos-14` / `macos-15` `arm64`) triggered on PRs/feature branches and `workflow_dispatch`. Fail on any `warning IL2xxx/IL3xxx`, suppressed trim properties, managed apphost DLLs, wrong binary architecture (`PE32+ x86-64` vs `Mach-O 64-bit arm64`), or missing `e_sqlite3`.
- **Verify:** Run `publish-uno-aot.ps1` locally and validate script against planted non-AOT/warning fixtures.
- **Test that dies if this breaks:** `AotArtifactGateTests.FailsOnIlWarning_ManagedDll_MissingSqlite_OrWrongArch`.

### T6 — Wiring audit (AC1, AC3, AC5, AC6)
- **Files:** `tests/SmartSnippets.Presentation.Tests/Wiring/UnoShellWiringAuditTests.cs`, audit notes in PR/review doc
- **Change:** Trace every new interface (`IPlatformCapabilities`, `ILineEndingPolicy`, `IShortcutPlatform`, `ISnippetDocumentService`, `IStartupErrorReporter`) and every `UnoAppHost` creation step from producer → constructor wiring → XAML `x:Bind` / command → runtime receipt (`app_started`, `settings_loaded`, `insertion_ready`, `porting_ready`, `menu_surface`, `exit_unregistered`), verifying parallel `Insert` vs `Copy` vs `Preview` paths and confirming zero dead registrations or unreferenced legacy services in `SmartSnippets.Uno`.
- **Verify:** Runtime log inspection on isolated candidate startup/shutdown confirms every expected receipt and zero planted fixture tokens.
- **Test that dies if this breaks:** `UnoShellWiringAuditTests.AllCompositionBoundariesHaveActiveConsumersAndEmitExpectedReceipts`.

## 7. Test strategy

- **Unit:** Sequential `SmartSnippets.Core.Tests` and `SmartSnippets.Presentation.Tests` covering log sanitization (`AppLog`, `Migrator`), newline policy, capability-gated `SettingsViewModel`/`PopupViewModel`, shortcut encoding roundtrips, and `ISnippetDocumentService` failure states. Deliberately excludes mocking `SnippetStore` in flow tests.
- **Integration:** Real composition-root tests against SQLite in disposable temp directories with fake OS boundaries only:
  1. `ManagementAndPortingFlow_CommitsAndExportsThroughDocumentService`
  2. `PopupAndMainRowInsertionFlow_SharesSingleFlightAndRecordsUsageOnlyOnDispatched`
  3. `PreviewAndCopyFlow_NeverInvokesPasteDispatcherOrRecordsUsage`
  4. `StartupFailureFlow_ReportsSanitizedErrorWithoutLogLeakage`
- **Manual runbook (guarded — requires isolated candidate profile; screen-driving steps require explicit operator slot per `docs/guides/002-acceptance-harness-safety.md`):**

| # | Step | Expected |
|---|---|---|
| 1 | Publish `SmartSnippets.Uno` (`win-x64` Release AOT) to `publish-uno/` while live `%LOCALAPPDATA%\SmartSnippets` checksum is recorded | 0 warnings, native x64 `.exe` + `e_sqlite3.dll`, live profile checksum unchanged |
| 2 | Launch candidate with `--profile-root <temp>` + copied v1/v2 fixture DB; edit snippet, switch palette/theme/text size, export & re-import JSON | Migration succeeds, UI updates live, restart preserves changes, `app.log` has zero snippet/path strings |
| 3 | Launch candidate with `--preview-popup --preview-popup-query synthetic --preview-popup-sort-menu` | Preview & sort menu render without stealing foreground; `menu_surface` logged; Enter closes with 0 paste/usage |
| 4 | In an authorized screen slot with owned synthetic Notepad target, invoke candidate hotkey → Enter, then main-row Insert, then Copy | Target receives CRLF text once per Insert (usage +1 each); Copy sets clipboard with usage +0; Exit restores clipboard & unregisters hotkey |

## 8. Rollout / phasing

`SmartSnippets.Uno` lands behind isolated candidate identity alongside unchanged `SmartSnippets.App`. T1–T2 land first (keeping legacy `App` green), followed by T3–T6. Production cutover and legacy `App` retirement are gated on Plans 020 and 021.

## 9. Open questions / dependency & assumption register

| ID | Dependency / Assumption | Status | Owner / Gate |
|---|---|---|---|
| D1 | Plan 018 FG3 (representative multi-window/external hover) & FG4–FG5 (native insertion/clipboard/privacy) evidence | **Pending** (only build, headless data incl. DB permission-denial follow-up, and basic GUI smoke pass so far; `016`) | Implementer / 018 completion before 019 G2 |
| D2 | macOS Uno AOT dependency: `016` 37-file source-built backend (`da217df23...`) is probe-only | **Unresolved** — requires upstream release or explicit owner approval of vendor build | User + implementer before T0 |
| D3 | Windows Uno package graph (`ProbeManualWindowsPackages=true` vs default helper `IL2059`) | **Unresolved** — requires owner approval for production `csproj` | User + implementer before T0 |
| D4 | Mobile strict Native AOT & extension memory gate: Android `XA1040` marks NativeAOT experimental/non-production; iOS keyboard AOT & device-dependent Jetsam headroom unmeasured (`NETSDK1147`, Mac disk ~7.7 GiB) | **Blocker decision required** before committing production shell to Uno (`017 §6`) | User decision at 018 T7 → 019 G2 |

## 10. Approval log

| Date | Event | Notes |
|---|---|---|
| 2026-10-30 | Requirement brief confirmed (G1) | Confirmed in master plan `017` (Phase 1 Win x64 + Apple Silicon macOS; Phase 2 iOS/Android; strict Native AOT) |
| 2026-10-30 | Provisional candidate drafted | Written to `docs/drafts/uno-019-025/`; dependency evidence from 018 pending; **unapproved, not implementation-ready** |
