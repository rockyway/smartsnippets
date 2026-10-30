# 025 — Uno mobile acceptance and final wiring audit

**Date:** 2026-10-30
**Status:** Provisional draft—dependency evidence pending; unapproved, not implementation-ready
**Size:** L
**Area:** `scripts/`, `.github/workflows/`, `tests/`, `docs/guides/`, `docs/research/`, `docs/plan/017-master-uno-cross-platform-migration.md`
**Requirement brief confirmed:** 2026-10-30 (G1, master 017)
**Master:** [017 — Master plan: Uno cross-platform migration](../../plan/017-master-uno-cross-platform-migration.md)
**Canonical placeholder:** [025 placeholder](../../plan/025-uno-mobile-acceptance-and-wiring-audit.md) (unchanged until 018–024 complete)

---

## 1. Goal

Gate Phase 2 mobile release and overall `017` migration completion on signed physical-device iOS and Android acceptance, verified AOT/entitlement/manifest safety, desktop non-regression after shared mobile changes, reproducible mobile CI/release runbooks, and a complete cross-platform (`Windows x64`, `Apple Silicon macOS`, `iOS`, `Android`) wiring and privacy audit across Plans 018–024.

## 2. Requirement (as confirmed at G1)

Excerpted from `docs/plan/017-master-uno-cross-platform-migration.md:19–55` and `docs/plan/025-uno-mobile-acceptance-and-wiring-audit.md:7–20`:

- **Problem:** Even after 023 and 024 implement mobile management and keyboard extensions, simulator or unit-test green runs do not prove signed physical-device keyboard behavior, 10k-snippet Jetsam stability, zero-network/privacy compliance, desktop non-regression after Migration v3, or orphan-free wiring across all four OS targets.
- **In scope:** Physical iOS and Android RC acceptance across the agreed device and target-field matrix; independent verification of signed `.ipa` (app + `.appex`) and `.aab`/`.apk` artifacts (AOT flags, architectures, App Group/entitlements, absence of `INTERNET`/JIT); cross-OS JSON library transfer verification (`Win ↔ Mac ↔ iOS ↔ Android`); 10k-snippet mobile memory/package-size gate verification; synthetic fixture privacy scan across app/extension/`os_log`/`logcat` outputs; full Windows x64 + Apple Silicon macOS regression pass; mobile CI/release workflows and install/distribution guides; final cross-phase (`018–024`) wiring audit (`docs/research/NNN-uno-final-wiring-audit.md`) before marking master `017` Complete.
- **Out of scope / non-goals:** Adding unapproved features, relaxing 022-approved AOT/privacy rules to pass release, counting simulator runs as physical-device acceptance (`025 placeholder`), cloud sync, accounts, password snippets (`016`).
- **Users and surfaces:** End users installing signed iOS/Android releases alongside Windows/macOS desktop releases; maintainers running CI, release, and audit gates.
- **Behaviour:** Every AC1–AC6 criterion has fresh, hash-pinned release-artifact evidence across all four platforms; any unexecuted physical-device check, orphan registration, desktop regression, or privacy leak keeps Phase 2 `BLOCKED` and prevents marking `017` Complete.
- **Constraints and assumptions:** Conditional on Plans 018–024 completing, owner-approved mobile runtime/AOT rules from 022 (explicitly accounting for Android `XA1040` https://learn.microsoft.com/en-us/dotnet/android/messages/xa1040 without silent Mono AOT substitution or warning suppression), physical iOS + Android devices, and Apple/Google signing credentials.
- **Acceptance criteria:** Finalizes master **AC1–AC6** across Phase 1 (re-verified) and Phase 2 (accepted): **AC1** Windows parity/data, **AC2** macOS parity, **AC3** desktop AOT/safety, **AC4** mobile management/keyboards + restrictions, **AC5** import/export, persistence, permissions, failure/usage accounting, **AC6** offline + zero snippet/label/tag/title/query/URI/path log leaks.
- **Decisions made:** Simulator evidence is diagnostic only, never acceptance PASS (`025 placeholder`); master `017` completes only when **both** Phase 1 and Phase 2 gates pass (`025 placeholder`).

## 3. Current state (as-built)

- `.github/workflows/ci.yml:1–61` and `release.yml:1–81` currently validate/package Windows `SmartSnippets.App` only (extended to `SmartSnippets.Uno` Windows + macOS in 019/021, with zero mobile build/signing/artifact-inspection jobs yet).
- `docs/research/010-wiring-audit.md:1–82` establishes the repository's canonical wiring-audit standard: `rg`-verified single-producer/consumer call graphs, single `INSERT`/`UPDATE` SQL ownership, `MIN(usage_count + 1, 2147483647)` saturation, `JsonSerializerContext`-only JSON, zero `ConfigureAwait(false)` thread hops onto `SnippetStore`, planted sentinel leak tests, and explicit `BLOCKED`/`PENDING-USER` reporting for unexecuted manual steps.
- `docs/guides/001-install-and-release.md:1–25` and `AGENTS.md:1–59` have no mobile build, signing, keyboard-enablement troubleshooting, or simulator-vs-device evidence rules.
- Shared `Core`/`Presentation` edits planned in 023–024 (`Migrations.cs` v3 `usage_receipts`, `UiText` provider messages, `ISnippetDocumentService`) touch assemblies consumed by Windows x64 and Apple Silicon macOS desktop builds, requiring post-024 desktop regression proof.

## 4. Design

### 4.1 Approach

1. **Signed mobile release pipeline & independent artifact gate:** Add `scripts/publish-ios-release.sh` and `scripts/publish-android-release.ps1`/`.sh` plus tag/manual CI jobs producing checksummed signed `.ipa` and `.aab`/`.apk` artifacts. Run an independent `verify-mobile-release-artifact.py` gate checking:
   - Exact 022-approved Native AOT / compilation properties, zero `IL2xxx`/`IL3xxx` warnings, zero suppressed `XA1040` (`NoWarn` / `SuppressTrimAnalysisWarnings` clean), and native `arm64` code + `e_sqlite3` presence in app and extension/IME slices.
   - Entitlements, permissions & zero-network audit: iOS `.app` and `.appex` carry matching App Group entitlement, `RequestsOpenAccess` matches 022 approval, zero JIT/debug entitlements (`get-task-allow = false`), **plus** Mach-O imported-symbol & dependency audit confirming zero networking APIs (`URLSession`/`CFNetwork`/`NWConnection`/`System.Net.Http`/`System.Net.Sockets`) because iOS uses no network entitlement (and `RequestsOpenAccess=true` enables extension networking); Android manifest carries `BIND_INPUT_METHOD` on the IME service and **zero** `android.permission.INTERNET` or `READ_EXTERNAL_STORAGE` bloat, paired with the same dependency audit.
2. **Physical-device compatibility, sensitive-field matrix, resilience & empirical 10k budget verification:** Execute a structured physical-device acceptance matrix (`RunOrigin = PhysicalDevice`; simulator runs tagged `Diagnostic.Simulator` and rejected for sign-off, matching `002-acceptance-harness-safety.md:30–37`). Read back actual target-editor buffers across third-party apps (Notes/Keep, Messages, Mail/Gmail, Safari/Chrome inputs, Docs/Word, Slack/Teams) rather than trusting `insertText` (`Void`) or `commitText` (`bool`) return values alone; verify iOS OS system-keyboard swap on `isSecureTextEntry`/opted-out hosts **separately** from Android IME's active `EditorInfo` restriction (`TYPE_*_PASSWORD`, `TYPE_NUMBER_VARIATION_PASSWORD`, `IME_FLAG_NO_PERSONALIZED_LEARNING` suppressing snippets & surrounding-text reads); verify Airplane Mode + Wi-Fi zero-packet runtime behavior, phase-specific `kill -9`/swipe-away outcomes (pre-rename `.tmp` → `+0` no-replay vs post-rename persisted receipt → `+1` once, plus reused-rowid/replaced-library rejection), 022-approved empirical 10k `phys_footprint`/package-size ceilings, and 4-way JSON library transfer (`Win x64 → macOS arm64 → iOS → Android → Win x64`).
3. **Desktop non-regression & v2→v3 upgrade check:** Re-run Windows x64 and Apple Silicon macOS sequential unit/integration suites, clean Native AOT publishes, v2→v3 `.v2.bak` migration checks, and desktop smoke/QA flows (`--preview-popup-sort-menu`, single-flight insertion) on the final unified commit so Phase 2 changes cannot silently regress Phase 1.
4. **Cross-phase (`018–024`) wiring & privacy audit:** Following `010-wiring-audit.md`, verify with static `rg` + runtime receipt traces every boundary across all four platforms: `Core` SQL/JSON/logging → `Presentation` policies/VMs → `UnoAppHost` (Win/Mac) & `MobileAppHost` (iOS/Android) & native keyboard hosts (`.appex` / `InputMethodService`) → XAML `x:Bind` / native callbacks → SQLite/clipboard/projection/receipts/cleanup. Confirm zero dead interfaces, zero reflection JSON, zero cross-thread `SnippetStore` calls, and zero synthetic fixture leakage across all four OS log streams.

### 4.2 Alternatives considered

| Option | Pros | Cons | Verdict |
|---|---|---|---|
| A — Signed physical-device matrix + desktop regression re-check + 4-OS `010`-style wiring audit | Proves real keyboard sandbox/Jetsam behavior and protects shipped Phase 1 desktop targets | Requires physical iOS & Android hardware and signing credentials | **Chosen** — required by master `017 §7–§8` and `025 placeholder` |
| B — Accept iOS Simulator + Android Emulator UI automation for Phase 2 sign-off | Runs headless in cloud CI without devices | Misses real iOS Jetsam limits, real provisioning/App Group enforcement, and OEM keyboard switcher behavior | Rejected — explicitly forbidden by `025 placeholder` |
| C — Skip desktop re-verification since 023/024 are "mobile plans" | Faster Phase 2 sign-off | Shared `Core` Migration v3 and `Presentation` porting changes compile into desktop binaries | Rejected — violates `025` expected completion criteria |

### 4.3 Data / config / API model

- **No new database schema or product APIs** are introduced in 025; 025 validates schema v3 (from 024), Native JSON v1 portability across all 4 OSes, and signed release bundles:
  - iOS: `SmartSnippets-ios-arm64-<version>.ipa` + `.sha256` (App Store / TestFlight / Ad-Hoc signed per owner release channel chosen in 022).
  - Android: `SmartSnippets-android-arm64-<version>.aab` + signed `.apk` + `.sha256`.
- **Evidence schema (`docs/research/NNN-phase2-mobile-acceptance-and-final-audit.md`):** Records `Platform`, `DeviceModel`, `OSVersion`, `ArtifactSha256`, `RunOrigin` (`PhysicalDevice` required for PASS), `10kPeakFootprintMb`, `PackageBytes`, `LeakScanResult`, and `Status` (`PASS`/`FAIL`/`BLOCKED`/`NOT RUN`).

### 4.4 Sequence diagram of the audited cross-platform chains

```text
 audited chain A (Desktop Insert, Win x64 & macOS arm64):
   Native Shortcut / Tray-MenuBar / Main-Row → TargetCapture → PopupShowCoordinator → PopupViewModel
     → InsertionShutdownCoordinator → InsertionService → PlatformProbe + PasteDispatcher → Store.RecordUsage (once)

 audited chain B (Mobile Keyboard Dispatch & Deduplicated Reconcile, iOS & Android):
   MobileAppHost Store/Sort Commit → KeyboardProjectionPublisher (atomic temp→rename w/ libraryId + gen + snippetUid)
     → Native .appex / InputMethodService → ProjectionReader → Proxy.insertText [Void] / InputConnection.commitText [bool]
       → UsageReceiptWriter (atomic UUID file w/ libraryId + snippetUid; pre-rename kill = +0 lost receipt, no replay)
         → MobileAppHost OnResumed → KeyboardReceiptReconciler
           → Store.ReconcileUsageReceipts (single tx: usage_receipts PK dedupe + libraryId/snippetUid match) → unlink → republish

Parallel non-inserting paths audited across all 4 platforms:
   - Explicit Copy (Main, Popup, Mobile Row, Editor Save-Failure) → Clipboard only; 0 usage, 0 receipts.
   - Desktop QA Preview (--preview-popup / --preview-popup-sort-menu) → Non-activating; menu_surface logged; 0 paste/clipboard/usage.
   - Mobile Sensitive / Opted-Out Fields → iOS OS system-keyboard swap; Android IME explicit EditorInfo password/PIN/NO_PERSONALIZED_LEARNING suppression (0 context reads, 0 dispatch, 0 receipts).
```

### 4.5 Surface list

| Surface | Change | Task |
|---|---|---|
| `scripts/publish-ios-release.sh`, `publish-android-release.*`, `verify-mobile-release-artifact.py`, `.github/workflows/` | Reproducible mobile release builds, AOT/manifest/entitlement gates, simulator-vs-device evidence checks | T1 |
| Physical iOS & Android acceptance matrix + 4-OS JSON portability + G10 log scan | Device verification across target apps, secure fields, kills, 10k budgets, and offline mode | T2 |
| Windows x64 & Apple Silicon macOS regression + v2→v3 upgrade check | Verify desktop tests, Native AOT publishes, `.v2.bak` v3 upgrade, and guarded smoke pass on final tree | T3 |
| `AGENTS.md`, `docs/guides/001-install-and-release.md`, `docs/requirements/001-prd.md` | Document mobile build/signing/install/restriction behavior without rewriting historical approval logs | T4 |
| `docs/research/NNN-uno-final-wiring-audit.md` + `docs/plan/017-master-uno-cross-platform-migration.md` | Complete `018–024` static + runtime wiring audit and close master `017` iff every gate is PASS | T5 |

## 5. Impact and risk

| Question | Answer |
|---|---|
| State management — what survives a crash mid-operation? | Verified on real hardware under forced `kill -9`/swipe-away across app, keyboard, document picker, and desktop upgrade backup. |
| Data consistency — orphans, races, double-apply? | Final audit verifies single SQLite writer per OS, zero cross-process DB connections, desktop single-flight paste, and at-most-once `(libraryId, snippetUid, receiptId)` reconciliation (with explicit non-replayed `+0` pre-rename window and reused-rowid rejection). |
| User experience — root problem or symptom; any surprise? | Gated check that documented platform restrictions (iOS secure-field swap & Full Access truth, Android IME password/PIN self-suppression, macOS Accessibility, Windows elevation) match shipped UI wording 1:1. |
| Backward compatibility — existing data / sessions / configs? Migration? | Verifies v1→v2→v3 SQLite upgrade safety on desktop & mobile plus 4-OS Native JSON v1 roundtrip in T2/T3. |
| Error recovery — what happens on failure; can it recover? | Any failed mobile or desktop regression gate blocks Phase 2 tag while leaving shipped Phase 1 desktop release untouched. |
| Logging & debugging — enough to diagnose in the field? | G10 synthetic-fixture scan checks Windows `app.log`, macOS `app.log`/captured unified log, iOS app+`.appex` logs/captured `os_log`, and Android `app.log`/captured `logcat` (fixture evidence per `002-acceptance-harness-safety.md:42–44`, not claiming OS-framework log absence). |
| Edge cases — empty, huge, repeated, concurrent, interrupted? | 10k-snippet memory/perf gates, 50k JSON import ceiling, offline Airplane Mode, revoked permissions, and duplicate receipt replay all gated before sign-off. |

**Risks:**
- **R1 — Simulator/emulator evidence accidentally substituted when physical hardware is busy** → `verify-mobile-release-artifact.py` and evidence schema reject non-`PhysicalDevice` rows for AC4/AC5 PASS.
- **R2 — Shared `Core` v3 change introduces trim warning or perf drift on Windows/macOS** → T3 re-runs full Phase 1 AOT + 10k perf checks on the final commit.
- **R3 — App Store / Play Store distribution signing differences vs local dev profiles** → Verify final release-signed `.ipa`/`.aab` entitlements and App Group IDs match before tagging.

**Rollback:** Keep Phase 2 release tags blocked until T1–T5 are 100% green; Phase 1 desktop releases remain independently buildable and deployable from `main`.

## 6. Tasks

### T1 — Mobile release packaging, signing, zero-network & strict-artifact gate in CI/scripts (AC4, AC6)
- **Files:** `scripts/publish-ios-release.sh`, `scripts/publish-android-release.ps1`, `scripts/verify-mobile-release-artifact.py`, `.github/workflows/ci.yml`, `.github/workflows/release.yml`
- **Change:** Automate reproducible signed Release builds for iOS (`.ipa` containing `.app` + `.appex`) and Android (`.aab`/`.apk`), enforcing 022-approved AOT properties, zero `IL2xxx`/`IL3xxx` warnings, zero suppressed `XA1040`, `arm64` native binaries + `e_sqlite3`, matching App Group entitlements, `get-task-allow=false`, zero Android `INTERNET` permission, and Mach-O/ELF/assembly symbol inspection rejecting networking APIs on both iOS and Android.
- **Verify:** Run build + `verify-mobile-release-artifact.py` against real outputs and planted defective fixtures.
- **Test that dies if this breaks:** `MobileReleaseArtifactValidatorTests.RejectsDebugEntitlement_AndroidInternet_IosNetworkSymbols_MissingAppex_SuppressedXa1040_OrSimulatorSlice`.

### T2 — Physical-device iOS/Android acceptance, sensitive-field/kill matrices, 4-OS JSON & G10 scan (AC4–AC6)
- **Files:** `scripts/verify-mobile-acceptance.py`, `docs/research/NNN-phase2-mobile-acceptance-and-final-audit.md`
- **Change:** Execute physical iOS and Android acceptance on release-signed builds: CRUD, folders, search/sort, SAF/`UIDocumentPicker` import/export (4-OS roundtrip + cancel/failure checks), keyboard enablement, target-app buffer-verified insertion, separate iOS secure/opt-out vs Android `EditorInfo` password/PIN/`NO_PERSONALIZED_LEARNING` checks, Airplane Mode + Wi-Fi zero-packet check, phase-specific `kill -9` + reused-rowid/wrong-library receipt rejection, 022-approved empirical 10k memory/size gate, and G10 synthetic leak scan.
- **Verify:** Every matrix row recorded with `RunOrigin=PhysicalDevice`, SHA256 digest, profiler/network metrics, and 0 leaked fixture tokens.
- **Test that dies if this breaks:** `MobileAcceptanceGateTests.FailsWhenSimulatorOriginUsed_AndroidPasswordFieldUnrestricted_10kMemoryUnmeasured_OrSyntheticTokenFound`.

### T3 — Phase 1 Windows x64 & Apple Silicon macOS non-regression verification (AC1–AC3, AC5–AC6)
- **Files:** `docs/research/NNN-phase2-mobile-acceptance-and-final-audit.md`
- **Change:** Run sequential `Core` + `Presentation` tests on Linux/Windows/macOS, clean `win-x64` and `osx-arm64` Native AOT publishes, v2→v3 `.v2.bak` upgrade roundtrip, and guarded desktop smoke/perf checks to prove 023/024 shared edits introduced zero desktop regressions or IL warnings.
- **Verify:** `publish-aot.ps1`, `publish-macos-aot.sh`, and sequential test suites exit 0 with 0 warnings.
- **Test that dies if this breaks:** `DesktopRegressionGateTests.VerifiesV2ToV3BackupAndZeroDesktopAotWarningsAfterMobileMerge`.

### T4 — Update repository instructions, install/release guides & PRD addendum (AC4)
- **Files:** `AGENTS.md`, `docs/guides/001-install-and-release.md`, `docs/guides/002-acceptance-harness-safety.md`, `docs/requirements/001-prd.md`
- **Change:** Document reproducible iOS/Android build/signing commands, simulator-vs-device testing rules, keyboard setup & platform restriction disclosures, and remove disposable `prototypes/uno-mobile/` once T2 passes—leaving all historical approval tables untouched.
- **Verify:** `git diff` review confirming zero edits to historical completed plan approval logs and zero broken relative doc links.
- **Test that dies if this breaks:** `DocIntegrityTests.AllGuideLinksResolve_AndNoDisposableMobilePrototypeRemains`.

### T5 — Final cross-phase (`018–024`) wiring & safety audit and `017` closure (AC1–AC6)
- **Files:** `tests/SmartSnippets.Presentation.Tests/Wiring/CrossPlatformFinalWiringAuditTests.cs`, `docs/research/NNN-uno-final-wiring-audit.md`, `docs/plan/017-master-uno-cross-platform-migration.md`
- **Change:** Perform `rg`-backed static + runtime receipt audit across Windows, macOS, iOS, and Android: verify every interface/service/command/XAML `x:Bind`/native extension entry has an active producer and consumer, single SQL write paths, source-generated JSON only, UI-thread `SnippetStore` invariants, parallel `Insert` vs `Copy` vs `Preview` separation, and sanitized `AppLog` paths. Mark `017` Complete **only** after all Phase 1 and Phase 2 acceptance rows are `PASS`.
- **Verify:** Full test suite + audit script pass with zero orphan types, zero `NOT RUN` rows marked `PASS`, and verified runtime receipts across all 4 OSes.
- **Test that dies if this breaks:** `CrossPlatformFinalWiringAuditTests.ZeroOrphanServices_ZeroReflectionJson_AndAllFourPlatformReceiptChainsVerified`.

## 7. Test strategy

- **Unit:** Release-artifact/manifest/entitlement validators, simulator-origin rejection logic, cross-OS JSON fixture compatibility, and static source-scanner rules (no `JsonSerializer` without `JsonTypeInfo`, no `AppLog.Error` raw exception message, no `ConfigureAwait(false)` before `SnippetStore` calls).
- **Integration:** Full 4-platform composition-root test suite (`UnoAppHost` Win/Mac + `MobileAppHost` iOS/Android + `KeyboardProjectionPublisher`/`KeyboardReceiptReconciler`) against real SQLite v1→v2→v3 databases.
- **Manual runbook (physical iOS & Android devices + Windows x64 + Apple Silicon Mac):**

| # | Step | Expected |
|---|---|---|
| 1 | Build & validate signed release artifacts for `win-x64`, `osx-arm64`, `ios-arm64` (`.ipa` w/ `.appex`), and `android-arm64` (`.aab`/`.apk`) | All 4 artifact validators pass: 0 IL warnings, approved AOT, valid signatures, zero Android `INTERNET`, and zero iOS/Android networking symbols |
| 2 | Export a synthetic Unicode/multiline/folder/tag library on Windows → import on macOS → export → import on iOS → export → import on Android | Snippet, folder, tag, and usage metadata roundtrip cleanly offline across all 4 OSes |
| 3 | On physical iPhone/iPad & Android phone (Airplane Mode + Wi-Fi packet check), run target-app insertion matrix, iOS secure/opt-out vs Android `TYPE_*_PASSWORD`/`NO_PERSONALIZED_LEARNING` checks, and 10k memory check | Target buffers receive exact text without clipboard/network use; iOS swaps to system keyboard; Android IME suppresses snippets/context & offers IME switch; 10k stays within 022 empirical memory budget |
| 4 | Run pre-rename `.tmp` vs post-rename receipt kill checks, live mid-session kills, and deleted/reused-rowid receipt checks; re-run Windows/macOS upgrade & smoke checks | Pre-rename `.tmp` credits `+0` (no replay), persisted receipts credit `+1` once (`+0` on repeat), live kill yields `∈ {0, 1}` matching receipt file (never `≥ 2`), reused rowid rejected; desktop flows & `.v2.bak` remain green |
| 5 | Run G10 synthetic-fixture scan across `app.log` and captured OS diagnostic streams from all 4 OSes and complete `NNN-uno-final-wiring-audit.md` | Zero planted content/labels/tags/titles/queries/paths/URIs in scanned logs; zero orphan wiring; `017` ready for sign-off |

## 8. Rollout / phasing

Publish mobile RC (`TestFlight` internal / signed Android RC) alongside verified desktop RC builds, execute T2–T5 on the exact candidate commit, obtain explicit user release approval, and only then create the production release tag and mark `017` Complete.

## 9. Open questions / dependency & assumption register

| ID | Dependency / Assumption | Status | Owner / Gate |
|---|---|---|---|
| D1 | Completion of Phase 1 (018–021) and Phase 2 implementation/feasibility plans (022–024) | **Pending** | Implementer / sequential subplan gates |
| D2 | Owner-approved mobile AOT/extension architecture & distribution channels (App Store / TestFlight vs Ad-Hoc; Play Store vs signed APK; Android `XA1040` resolution from 022) | **Deferred to 022 → 025 G2** (`017 §9`) | User + implementer before 025 execution |
| D3 | Physical iOS and Android devices + Apple/Google release signing credentials | **External prerequisites** — absence keeps Phase 2 `BLOCKED` (`017 §8`) | User before T1–T2 |

## 10. Approval log

| Date | Event | Notes |
|---|---|---|
| 2026-10-30 | Requirement brief confirmed (G1) | Confirmed in master plan `017` (Phase 2 physical-device acceptance, offline/privacy gates, final cross-phase audit) |
| 2026-10-30 | Provisional candidate drafted | Written to `docs/drafts/uno-019-025/`; depends on 018–024 completion; **unapproved, not implementation-ready** |
