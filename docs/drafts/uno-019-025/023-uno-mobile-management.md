# 023 — Uno mobile management

**Date:** 2026-10-30
**Status:** Provisional draft—dependency evidence pending; unapproved, not implementation-ready
**Size:** L
**Area:** `src/SmartSnippets.Uno/` (iOS/Android heads & adaptive views), `src/SmartSnippets.Presentation/`, `src/SmartSnippets.Core/`, `tests/`
**Requirement brief confirmed:** 2026-10-30 (G1, master 017)
**Master:** [017 — Master plan: Uno cross-platform migration](../../plan/017-master-uno-cross-platform-migration.md)
**Canonical placeholder:** [023 placeholder](../../plan/023-uno-mobile-management.md) (unchanged until 018–022 complete)

---

## 1. Goal

Deliver an adaptive, touch-first Uno snippet and folder management app on iOS and Android—supporting full offline CRUD, search/sort, unsaved-edit guards, explicit Copy, theme/palette/text-size settings, truthful sandbox document-provider JSON import/export, keyboard enablement/restriction guidance, and atomic local projection publication for Plan 024—under the mobile AOT/toolchain architecture approved in Plan 022.

## 2. Requirement (as confirmed at G1)

Excerpted from `docs/plan/017-master-uno-cross-platform-migration.md:19–55` and `docs/plan/023-uno-mobile-management.md:7–19`:

- **Problem:** Desktop XAML assumes a ≥`640x480` multi-column window (`MainWindow.xaml.cs:18–19`), filesystem-path file pickers with atomic sibling-temp replacement (`SnippetFileService.cs:9–28`, `UiText.cs:215`), and desktop-only Settings (global hotkey, tray, Start at login, admin restart).
- **In scope:** `net10.0-ios` (`ios-arm64`) and `net10.0-android` (`android-arm64`) targets in `SmartSnippets.Uno` (per 022-approved runtime/AOT rules); adaptive phone/tablet layouts for Snippets, Editor, Folders, and Settings; hardware/gesture Back & lifecycle unsaved-edit handling; UI-thread SQLite persistence safe across iOS/Android backgrounding (`0xdead10cc`-free); `UIDocumentPickerViewController` and Android SAF `ISnippetDocumentService` adapters with truthful non-atomic failure wording; platform-accurate Keyboard Setup & Restrictions guidance; coalesced atomic publication of the 022-validated local keyboard projection.
- **Out of scope / non-goals:** Shipping or claiming acceptance of the iOS keyboard extension or Android IME (024), desktop global hotkeys/tray/startup/elevation on mobile, cloud sync, `INTERNET` permission, password snippets (`016`).
- **Users and surfaces:** iOS and Android users managing snippets/folders, copying text, transferring `.json` libraries via Files/SAF, customizing appearance, and reading keyboard setup/restriction instructions.
- **Behaviour:** Touch CRUD, folder filtering, search/sort, and explicit Copy (`usage +0`) work offline across portrait/landscape, software-keyboard resize, and process termination; cancelled or failed document-provider operations leave the SQLite library untouched and report accurate overwrite guarantees; every committed store/sort change updates the local keyboard projection atomically.
- **Constraints and assumptions:** Strictly conditional on Plan 022 completing on physical devices **and** explicit owner sign-off on: (a) Android `XA1040` (`PublishAot=true` experimental/non-production: https://learn.microsoft.com/en-us/dotnet/android/messages/xa1040) disposition, (b) iOS AOT/App Group/Full Access configuration, and (c) exact local projection schema and OS floors.
- **Acceptance criteria:** Contributes to master **AC4** (mobile management + documented restrictions), **AC5** (import/export, persistence, failure handling, usage rules), and **AC6** (offline, zero snippet/label/tag/query/URI log leaks).
- **Decisions made:** Reuse `Core` + `Presentation` view-models (`017 §4.1`); no fake desktop toggles on mobile (`017 §5`); Copy never increments insertion usage (`017 §4.4`).

## 3. Current state (as-built)

- `src/SmartSnippets.App/MainWindow.xaml.cs:16–19,43–69` enforces `640x480` minimum desktop window bounds and side-by-side master-detail panes that overflow narrow mobile viewports (~320–430 pt) and lack software-keyboard inset handling.
- `src/SmartSnippets.Presentation/ViewModels/SnippetsPageViewModel.cs:96–128,133–145,208–232` implements `IGuardedPage` (`Save`/`Discard`/`Cancel`), `CanInsertRow` (`false` when `insertRow` is null), and `CopyRow` (`_clipboard.SetText` + `snippet_row_copied`, zero usage).
- `src/SmartSnippets.Presentation/ViewModels/SnippetEditorViewModel.cs:17–128`, `SnippetListViewModel.cs:15–98`, and `FoldersPageViewModel.cs:14–108` are WinUI-free `ObservableObject` view-models ready for adaptive mobile XAML binding.
- `src/SmartSnippets.App/Services/SnippetFileService.cs:9–28`, `SnippetPortingViewModel.cs:45–138`, and `UiText.cs:215` assume local paths, `Path.GetExtension`, and atomic replacement (`ExportWriteFailed` promises `"Any existing file was left unchanged"`), which is false when an Android SAF `ContentResolver` or security-scoped provider stream fails mid-write.
- `src/SmartSnippets.Presentation/ViewModels/SettingsViewModel.cs:67–98,215–259` requires `IStartupRegistration`, `IHotkeyManager`, and `IElevation`; mobile needs capability-gated hiding of desktop settings plus a dedicated Keyboard Setup & Restrictions view-model section.
- `src/SmartSnippets.Core/Store/SnippetStore.cs:21,41–51,333` opens and disposes SQLite connections per operation (`using var connection = _factory.Open()`), naturally avoiding persistent open file locks during mobile background suspension when kept strictly on the UI thread.

## 4. Design

### 4.1 Approach

1. **Mobile composition root (`MobileAppHost`) & background-safe SQLite:** Construct `AppPaths` in app-private storage (`Library/Application Support/SmartSnippets` on iOS with `NSFileProtectionCompleteUntilFirstUserAuthentication` so background/extension transitions after first unlock never fail; `Context.FilesDir/SmartSnippets` on Android). Run `Migrator` and `SnippetStore` exclusively on the UI dispatcher. Ensure zero open SQLite handles span `OnSuspending`/`DidEnterBackground` (preventing iOS `0xdead10cc`), and flush pending `MobileProjectionPublisher` writes inside a bounded `beginBackgroundTask` / lifecycle hook before suspension.
2. **Adaptive mobile shell & touch/accessibility UX:** Use `VisualStateManager` / adaptive navigation in `SmartSnippets.Uno`:
   - Compact width (<720 pt, phones): bottom `TabBar`/`NavigationView` (`Snippets`, `Folders`, `Settings`), folder filter chip/sheet above search, single-column touch-sized (`≥44x44` pt) snippet list with swipe/context menu (`Copy`, `Edit`, `Delete`—`Insert` hidden via `CanInsertRow == false`), pushing a full-screen `SnippetEditor` sheet/page. Wire Android `OnBackPressedCallback` and iOS navigation back/dismissal through `SnippetsPageViewModel.CanLeaveAsync()` (`IGuardedPage`).
   - Expanded width (≥720 pt, iPad/tablets): two-pane master-detail reusing desktop proportions with safe-area and `UIKeyboardLayoutGuide` / `WindowInsetsCompat.Type.ime()` padding so the software keyboard never covers editor fields or validation errors.
3. **Sandbox document-provider `ISnippetDocumentService`:**
   - **iOS (`IosDocumentService`):** `UIDocumentPickerViewController` (`forOpeningContentTypes: [UTType.json]` / `forExporting: [tempJsonUrl], asCopy: true`). Staging export to an app-private temp `.json` first makes `forExporting(..., asCopy: true)` let the OS copy a complete validated payload; security-scoped URLs pair `startAccessingSecurityScopedResource()` / `defer stopAccessing...` and never log URLs/filenames.
   - **Android (`AndroidSafDocumentService`):** `ActivityResultContracts.OpenDocument` (`application/json` / `*/*` validated via `OpenableColumns.DISPLAY_NAME` `.json` check + UTF-8/JSON parser) and `CreateDocument("application/json")`. Because `ContentResolver.openOutputStream(uri, "wt")` truncates target provider URIs non-atomically, return `DocumentWriteOutcome.FailedOutcomeUnknown` if a stream write fails after opening, mapping `SnippetPortingViewModel` to truthful `UiText.ExportProviderWriteFailed` (omitting desktop's `"Any existing file was left unchanged"` claim).
4. **Mobile Settings, platform-specific disclosures & identity-bound projection publication:** Gate out hotkey, Start-at-login, and admin-restart sections via `IPlatformCapabilities`. Add `MobileKeyboardGuideViewModel` displaying: (a) live OS enablement check where observable (`InputMethodManager.enabledInputMethodList` on Android; container handshake + deep-link to App Settings on iOS), (b) step-by-step enablement instructions, and (c) platform-accurate G1 disclosures: on **iOS**, the OS forces the system keyboard on `isSecureTextEntry`/certain keypad/opted-out host fields, and Full Access (`RequestsOpenAccess`) is disclosed truthfully alongside Smart Snippets' audited absence of networking code/dependencies (never claiming an iOS entitlement blocks networking); on **Android**, where the OS still presents the active custom IME on password/PIN fields, Smart Snippets' IME deliberately suppresses snippet UI/insertion/surrounding-text access on password, PIN, and `NO_PERSONALIZED_LEARNING` fields with a one-tap IME switch, backed by omitting `android.permission.INTERNET`. Wire `MobileProjectionPublisher` to `SnippetStore.Changed` and `SortPreference.Changed` to publish the 022-validated `keyboard-projection.v1.json`—carrying persistent `libraryId`, monotonic `generation`, and stable per-snippet `snippetUid`—atomically on every committed change.

### 4.2 Alternatives considered

| Option | Pros | Cons | Verdict |
|---|---|---|---|
| A — Adaptive Uno shell + `ISnippetDocumentService` URI descriptors + UI-thread atomic projection publisher | Reuses 100% of `Core`/`Presentation` CRUD & validation; truthful SAF error UX; zero SQLite cross-process locking | Requires responsive compact/expanded XAML states and native picker bridges | **Chosen** — satisfies AC4/AC5/AC6 cleanly |
| B — Force desktop 3-column `MainWindow` onto mobile | Zero new layout XAML | Unusable on phones (<430 pt), obscured by software keyboard, lacks mobile Back guard | Rejected |
| C — Expose raw app Documents folder instead of system pickers | Avoids `UIDocumentPicker` / SAF | Poor UX on Android scoped storage; fails cloud/Files-app manual transfer workflows | Rejected |
| D — Claim desktop atomic overwrite on Android SAF | Reuses `UiText.ExportWriteFailed` unchanged | Lies to user if provider truncated existing file before mid-stream I/O error | Rejected — violates `017 §4.5` |

### 4.3 Data / config / API model

- **SQLite & identity metadata:** Retains core tables with persistent `library_id` UUID in `settings` (`SettingsKeys.LibraryId`, rotated on whole-DB replacement) and stable non-reused per-snippet `snippet_uid` (per 022-validated identity schema, preventing SQLite rowid reuse `Migrations.cs:16` from mis-attributing keyboard receipts in 024). Desktop-only `Hotkey*` keys ignored on mobile.
- **Document write contract (`ISnippetDocumentService`, refined from 019 & 022):**
  - `DocumentReadHandle { string SafeExtension; Func<CancellationToken, Task<Stream>> OpenReadAsync; IAsyncDisposable Scope; }` (never exposes raw `content://` or user path to logs).
  - `ExportWriteFailed` wording split: `UiText.ExportWriteFailedAtomic` (desktop) vs `UiText.ExportWriteFailedProvider` (`"Couldn't finish writing the selected file. Check the destination file before relying on it, or export to a new file."`).
- **Projection publication:** Writes `<SharedContainer>/keyboard-projection.v1.json` (`libraryId`, monotonic `generation`, `sort`, folders, and snippets with `(id, snippetUid, ...)`) via `AtomicFileWriter` on UI-dispatcher coalesced callback after every `Store.Changed` / `SortPreference.Changed`.

### 4.4 Sequence diagram of the new hot path

```text
User edits snippet / imports JSON / changes Sort in Mobile Management App
  → ViewModel (SnippetEditorViewModel / SnippetPortingViewModel) on UI thread
    → SnippetStore.Write(...) [short-lived SQLite tx, closes connection immediately]
      → Store.Changed raised on UI thread
        ├─ Adaptive List / Folders VMs refresh observable rows
        └─ MobileProjectionPublisher (coalesced on UI dispatcher)
             → builds versioned projection DTO (source-generated JSON)
             → AtomicFileWriter writes temp → renames keyboard-projection.v1.json in App Group / files/keyboard/
Parallel paths checked:
  - Explicit Copy (row swipe / editor failure Copy text) → IClipboardService.SetText; zero usage, zero receipt.
  - Document Import cancel / non-JSON / >32 MB / corrupt JSON → refused BEFORE SnippetImport.Commit; projection untouched.
  - Hardware Back / tab switch with dirty editor → IGuardedPage Save/Discard/Cancel prompt before navigation.
```

### 4.5 Surface list

| Surface | Change | Task |
|---|---|---|
| `SmartSnippets.Uno.csproj` (`net10.0-ios`, `net10.0-android`) + `MobileAppHost` | Mobile target configuration (per 022 AOT rules), app-private `AppPaths`, `0xdead10cc`-safe lifecycle | T1 |
| Adaptive `MobileShell`, `SnippetsPage`, `SnippetEditor`, `FoldersPage` | Compact/tablet responsive XAML, safe-area + IME insets, `≥44pt` touch targets, Back guard, `Copy`-only row actions | T2 |
| `IosDocumentService`, `AndroidSafDocumentService`, `SnippetPortingViewModel`, `UiText` | Security-scoped URL & SAF URI import/export, 32 MB/50k caps, truthful provider write-failure messaging | T3 |
| `SettingsPage` (mobile), `MobileKeyboardGuideViewModel` | Hide hotkey/startup/admin; add Keyboard Setup steps, Android IME status check, and secure-field/privacy disclosures | T4 |
| `MobileProjectionPublisher` | Coalesced atomic publication of 022-validated keyboard projection on store/sort changes | T5 |

## 5. Impact and risk

| Question | Answer |
|---|---|
| State management — what survives a crash mid-operation? | Committed SQLite transactions and atomic projection files survive `SIGKILL`/process eviction; uncommitted dirty editor text stays in-memory only (disclosed via inline save-failure `Copy text` recovery). |
| Data consistency — orphans, races, double-apply? | Single UI-thread `SnippetStore` + `SnippetPortingViewModel.IsBusy` gate + monotonic projection `generation` prevent out-of-order overwrites. |
| User experience — root problem or symptom; any surprise? | Target UX: native touch navigation, software-keyboard scroll-into-view, VoiceOver/TalkBack labels, and distinct iOS (OS keyboard swap) vs Android (IME self-restriction on password/PIN fields) disclosures. |
| Backward compatibility — existing data / sessions / configs? Migration? | Reuses shared `ImportParser`/`NativeExport` to preserve Native v1, TermFlow, Rephlo, and InkSpoke `.json` compatibility with desktop Phase 1 (verified in T3). |
| Error recovery — what happens on failure; can it recover? | Save failure retains editor text and offers `Try again` / `Copy text` (`UiText.cs:64–66`); cancelled/failed SAF import leaves DB untouched; failed SAF overwrite warns user accurately. |
| Logging & debugging — enough to diagnose in the field? | `AppLog` writes UTC timestamps and allowlisted tokens (`app_started`, `store_loaded`, `import_done`, `export_done`, `projection_published gen=<g> count=<n> ms=<t>`); synthetic-fixture scans verify `app.log` and captured `os_log`/`logcat` without claiming blanket control over OS framework logs. |
| Edge cases — empty, huge, repeated, concurrent, interrupted? | 10k-snippet virtualized list scrolling, rotation mid-edit, OS process death while picker is open (Android activity recreation), and revoked security-scoped URL access. |

**Risks:**
- **R1 — Android activity death while SAF picker is foregrounded** → Persist navigation/porting state cleanly or fail safe without crashing on recreated `MainActivity` when picker result arrives.
- **R2 — 10k projection write jank on mobile UI thread** → Coalesce `Store.Changed` bursts (e.g., 50k-entry import produces 1 `Reloaded`/`Imported` event) and stream UTF-8 bytes; verify within 022 latency budget.
- **R3 — Upstream 022 gate unresolved (`XA1040` or iOS extension sharing)** → Do not start 023 until 022 owner decisions are recorded.

**Rollback:** Mobile targets build independently of Phase 1 desktop executables; revert mobile TFM/adapter commits without impacting shipped Windows or macOS releases.

## 6. Tasks

### T0 — Lock 022-approved mobile runtime, OS floors, zero-network checks & projection contract (AC4–AC6)
- **Files:** `src/SmartSnippets.Uno/SmartSnippets.Uno.csproj`, `src/SmartSnippets.Core/Porting/KeyboardProjection.cs`
- **Change:** Configure `net10.0-ios` and `net10.0-android` TFMs, minimum OS versions, signing/App Group entitlements, and source-generated `KeyboardProjectionJsonContext` (`libraryId`, `generation`, `snippetUid`) exactly as approved at the end of Plan 022 (zero `NoWarn` for `IL2xxx`/`IL3xxx` or unapproved `XA1040` workarounds; zero Android `INTERNET` permission; static dependency/symbol check rejecting networking APIs on both iOS and Android).
- **Verify:** Clean Release publish of mobile management heads + manifest/symbol/AOT property check.
- **Test that dies if this breaks:** `MobileTargetConfigTests.EnforcesApprovedAotProperties_ZeroAndroidInternet_ZeroNetworkApis_AndIdentityBoundProjectionRoundtrip`.

### T1 — Implement `MobileAppHost`, private `AppPaths`, and background-safe SQLite lifecycle (AC5, AC6)
- **Files:** `src/SmartSnippets.Uno/Platforms/iOS/IosAppHost.cs`, `Platforms/Android/AndroidAppHost.cs`, `MobileAppHost.cs`
- **Change:** Wire `Migrator`, `SnippetStore`, `SettingsRepository`, `ThemeService`, and `TextSizeService` on the mobile UI dispatcher; set iOS `NSFileProtectionCompleteUntilFirstUserAuthentication` on DB/log directory; verify zero SQLite connections remain open idle across background suspension.
- **Verify:** Sequential `Core`/`Presentation` unit tests + composition lifecycle test simulating suspend/resume and corrupt-DB startup alert.
- **Test that dies if this breaks:** `MobileLifecycleStoreTests.NeverHoldsOpenSqliteConnectionIdle_AndPreservesUiThreadOwnership`.

### T2 — Build adaptive phone/tablet management XAML, Back guard & touch accessibility (AC4, AC5)
- **Files:** `src/SmartSnippets.Uno/Mobile/*` (or adaptive states in `Pages/SnippetsPage.xaml*`, `FoldersPage.xaml*`, `Controls/SnippetEditor.xaml*`)
- **Change:** Add compact single-pane navigation + tablet two-pane states, software-keyboard safe-area insets, `≥44pt` touch targets, VoiceOver/TalkBack `AutomationProperties.Name`, hardware/gesture Back wired to `IGuardedPage.CanLeaveAsync()`, and row `Copy`/`Edit`/`Delete` actions (no desktop `Insert`).
- **Verify:** Build both mobile heads with 0 XAML/`x:Bind` warnings + view-model navigation guard tests.
- **Test that dies if this breaks:** `MobileNavigationGuardTests.BackGestureWithDirtyEditor_PromptsSaveDiscardCancel_AndCopyEarnsZeroUsage`.

### T3 — Implement iOS & Android document-provider `ISnippetDocumentService` adapters (AC5, AC6)
- **Files:** `src/SmartSnippets.Uno/Platforms/iOS/IosSnippetDocumentService.cs`, `Platforms/Android/AndroidSafDocumentService.cs`, `src/SmartSnippets.Presentation/ViewModels/SnippetPortingViewModel.cs`, `src/SmartSnippets.Presentation/Text/UiText.cs`
- **Change:** Wire `UIDocumentPickerViewController` (with `startAccessingSecurityScopedResource` disposal and staged export copy) and Android SAF (`OpenDocument`/`CreateDocument` via `ActivityResultLauncher`), enforcing `.json`/32 MB/50k rules without raw filesystem path assumptions, never logging URIs/filenames, and surfacing truthful `ExportWriteFailedProvider` messaging on non-atomic SAF stream errors.
- **Verify:** Unit + integration tests covering cancel, non-JSON URI, >32 MB stream, mid-stream `IOException`, and desktop-exported JSON roundtrip.
- **Test that dies if this breaks:** `MobilePortingFlowTests.SafMidWriteFailure_ReportsNonAtomicWarning_AndCancelledImportLeavesStoreUntouched` and `MobilePortingPrivacyTests.FailedProviderOpen_NeverLogsUriOrDisplayName`.

### T4 — Mobile Settings & Keyboard Setup / Restriction disclosures (AC4, AC6)
- **Files:** `src/SmartSnippets.Presentation/ViewModels/SettingsViewModel.cs`, `MobileKeyboardGuideViewModel.cs`, `src/SmartSnippets.Uno/Pages/SettingsPage.xaml*`
- **Change:** Hide desktop hotkey, Start-at-login, and admin-restart cards via `IPlatformCapabilities`; render Theme, Color Palette, Text Size, and the Keyboard Setup & Restrictions card (OS Settings deep-link, Android IME enabled check, explicit secure-field/opt-out/Full Access/offline disclosures).
- **Verify:** `SettingsViewModel` capability tests on iOS and Android configurations.
- **Test that dies if this breaks:** `MobileSettingsTests.ExposesThemePaletteTextSizeAndKeyboardDisclosures_WithoutDesktopHotkeyOrStartupStubs`.

### T5 — Coalesced atomic local projection publisher & wiring audit (AC4–AC6)
- **Files:** `src/SmartSnippets.Presentation/Services/KeyboardProjectionPublisher.cs`, `tests/SmartSnippets.Presentation.Tests/Wiring/MobileManagementWiringAuditTests.cs`
- **Change:** Subscribe `KeyboardProjectionPublisher` to `SnippetStore.Changed` and `SortPreference.Changed` on UI dispatcher; write `keyboard-projection.v1.json` atomically to the 022-validated App Group / private `files/keyboard/` path; flush on backgrounding; audit every mobile producer → `MobileAppHost` → VM/XAML → picker/clipboard/projection consumer and confirm Phase 1 desktop tests remain green.
- **Verify:** End-to-end composition test (create/edit/delete/import/sort → verified projection JSON; explicit Copy → clipboard updated, usage = 0) + runtime receipt check.
- **Test that dies if this breaks:** `MobileManagementWiringAuditTests.EveryStoreAndSortMutationPublishesAtomicProjection_AndAllMobileBindingsResolve`.

## 7. Test strategy

- **Unit:** `KeyboardProjectionPublisher` coalescing/monotonic generation, `SnippetPortingViewModel` non-atomic provider failure states, mobile `SettingsViewModel` capability gating, and URI extension/MIME validation without `System.IO.Path` crashes on opaque `content://` strings.
- **Integration:** Real `MobileAppHost` + SQLite + temp container directory flows covering CRUD, dirty-editor Back cancellation, Native/TermFlow/Rephlo/InkSpoke JSON import/export, and atomic projection updates.
- **Manual runbook (physical iOS & Android devices, Airplane Mode):**

| # | Step | Expected |
|---|---|---|
| 1 | Launch app on phone (portrait & landscape); create folders and Unicode/multiline snippets; force-close and relaunch | Layout adapts cleanly above software keyboard; all folders/snippets persist |
| 2 | Edit snippet without saving, swipe/press Back or switch tabs → test `Cancel`, `Discard`, and `Save` | Unsaved-changes dialog guards all exits accurately |
| 3 | Export library via system document picker, delete a snippet, re-import `.json`, and cancel a second import | Export/import roundtrips cleanly; cancelled picker leaves library unchanged |
| 4 | Inspect Settings and tap Copy on a snippet | Theme/Palette/Text Size apply live; desktop hotkey/startup/admin absent; Keyboard restrictions disclosed; Copy sets clipboard with usage = 0 |
| 5 | Inspect shared container projection file and run synthetic-fixture scan on `app.log` + captured `os_log`/`logcat` | `keyboard-projection.v1.json` carries `libraryId`/`generation`/`snippetUid` matching store; zero planted fixture content, labels, tags, queries, or URIs in scanned logs |

## 8. Rollout / phasing

Merged behind mobile target conditions in `SmartSnippets.Uno` while keeping Windows/macOS desktop CI green. Does not ship publicly to users until Plan 024 adds the native keyboard extensions/IME and Plan 025 passes full device acceptance.

## 9. Open questions / dependency & assumption register

| ID | Dependency / Assumption | Status | Owner / Gate |
|---|---|---|---|
| D1 | Plan 022 physical-device resolution of Android `XA1040` and iOS app/extension Native AOT | **Pending** (`XA1040` experimental per Microsoft docs; workloads currently unrun) | User + implementer in 022 before 023 G2 |
| D2 | Exact iOS App Group container & `RequestsOpenAccess` disclosure text | **Conditional on 022 T4** | Lock at 023 G2 from 022 evidence |
| D3 | Minimum iOS and Android OS versions and CPU RIDs (`ios-arm64`, `android-arm64`) | **Conditional on 022 T0/T5** | User + implementer at 022 → 023 G2 |

## 10. Approval log

| Date | Event | Notes |
|---|---|---|
| 2026-10-30 | Requirement brief confirmed (G1) | Confirmed in master plan `017` (Phase 2 adaptive mobile management, local SQLite/JSON, explicit restrictions) |
| 2026-10-30 | Provisional candidate drafted | Written to `docs/drafts/uno-019-025/`; depends on 022 physical-device gates; **unapproved, not implementation-ready** |
