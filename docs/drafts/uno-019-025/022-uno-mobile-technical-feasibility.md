# 022 — Uno mobile technical feasibility

**Date:** 2026-10-30
**Status:** Provisional draft—dependency evidence pending; unapproved, not implementation-ready
**Size:** L
**Area:** `prototypes/uno-mobile/` (isolated disposable probe), `docs/research/`
**Requirement brief confirmed:** 2026-10-30 (G1, master 017)
**Master:** [017 — Master plan: Uno cross-platform migration](../../plan/017-master-uno-cross-platform-migration.md)
**Canonical placeholder:** [022 placeholder](../../plan/022-uno-mobile-technical-feasibility.md) (unchanged until 018–021 complete)

---

## 1. Goal

Determine on physical iOS and Android devices whether Uno management apps **paired with native custom keyboard extensions (`UIInputViewController`) and Android IMEs (`InputMethodService`)** satisfy strict Native AOT, empirical 10,000-snippet memory/package budgets, cross-process local data sharing, deduplicated usage-receipt reconciliation, and document-provider safety **before** detailing or starting Phase 2 production plans (023–025). Any unsupported Native AOT target, `XA1040` production restriction, or device-observed iOS Jetsam failure stops downstream work for an explicit owner decision rather than substituting Mono AOT or suppressing warnings.

## 2. Requirement (as confirmed at G1)

Excerpted from `docs/plan/017-master-uno-cross-platform-migration.md:19–55` and `docs/plan/022-uno-mobile-technical-feasibility.md:7–20`:

- **Problem:** Mobile keyboard hosts execute under tight, undocumented device/OS-dependent memory limits (strict iOS extension Jetsam enforcement), Uno XAML is not proven inside keyboard extensions, `.NET for Android` officially marks Native AOT experimental/non-production (`XA1040`), Android routes password/sensitive fields to active custom IMEs unless explicitly restricted, and iOS app-extension Native AOT + App Group behavior without Full Access are unverified.
- **In scope:** Isolated `prototypes/uno-mobile/` probe validating: (1) strict Native AOT (`PublishAot=true`, unsuppressed) for both management app and keyboard/IME artifacts on `ios-arm64` and `android-arm64`; (2) physical-device installation, `UITextDocumentProxy` (`Void` return) / `InputConnection` (`bool` IPC return) Unicode+multiline dispatch vs owned-fixture target delivery, separate iOS OS-suppressed/opt-out fields vs Android `EditorInfo` sensitive-field detection/restriction, and phase-specific process-kill behavior; (3) UI-thread-isolated SQLite in management app + cross-process versioned local projection and `(libraryId, snippetUid, projectionGeneration)`-bound deduplicated usage receipts; (4) iOS `RequestsOpenAccess` (Full Access off vs on) App Group matrix, Android `INTERNET` permission omission, and cross-platform zero-network source/symbol/runtime audit; (5) empirical 10k-snippet iOS keyboard launch/peak `phys_footprint` + owner-approved safety margin and combined package size; (6) iOS/Android document-provider cancellation/truncation behavior; (7) proposed OS floors, RIDs, extension UI stack, and publication contracts for 023/024 approval.
- **Out of scope / non-goals:** Production mobile UI (023), production keyboard/IME polish (024), App Store / Play Store public submission (025), desktop global-hook/clipboard-paste injection on mobile, cloud sync, `INTERNET` permission, password snippets (`016`).
- **Users and surfaces:** Developer/operator executing physical-device probes and reviewing measured feasibility evidence; zero changes to shipped Phase 1 desktop binaries.
- **Behaviour:** Probe either proves end-to-end physical-device app + keyboard AOT execution within memory/safety bounds or emits an exact `BLOCKED` gate with compiler/device diagnostics and concrete options for owner decision.
- **Constraints and assumptions:** Conditional on Phase 1 (018–021) completion, installed `.NET` iOS/Android workloads (currently absent: `NETSDK1147` in `016:65`), freed Mac disk space (currently ~7.7 GiB in `016`), Apple Developer / Android signing setup, and physical iOS + Android test devices. Official Microsoft doc https://learn.microsoft.com/en-us/dotnet/android/messages/xa1040 states `.NET for Android` `PublishAot=true` triggers `XA1040` ("NativeAOT is experimental and not supported for production use"): **never** suppress `XA1040` via `NoWarn` or swap in Mono AOT (`RunAOTCompilation=true`) silently.
- **Acceptance criteria:** Gates master **AC4** (iOS/Android management + keyboard integrations with documented restrictions), **AC5** (portability, persistence, permissions, usage accounting), and **AC6** (no network service, content-free logs).
- **Decisions made:** Mobile uses native keyboard extensions/IMEs, never desktop clipboard/focus injection (`017 §2`); a management-app-only build fails this gate (`022 placeholder`).

## 3. Current state (as-built)

- `src/SmartSnippets.Core/SmartSnippets.Core.csproj:1–9` and `SmartSnippets.Presentation.csproj:1–12` target `net10.0` with `IsAotCompatible=true`; neither mobile TFM (`net10.0-ios`, `net10.0-android`) nor mobile SQLite native packaging (`e_sqlite3` static `.a` on iOS / `.so` on Android) has been built or tested (`016:65`).
- `src/SmartSnippets.Core/Store/SnippetStore.cs:21,41,316` binds a single-threaded `_ownerThreadId` and opens short-lived SQLite connections via `SqliteConnectionFactory`; sharing a UI-owned `SnippetStore` or concurrent cross-process SQLite writes from a keyboard extension risks `SQLITE_BUSY`/Jetsam kills (`017 §4.3`).
- `src/SmartSnippets.Core/Porting/NativeJsonContext.cs:9–11` provides AOT-safe source-generated JSON serialization; `AtomicFileWriter.cs:5–27` relies on same-directory temp file + `File.Move(..., overwrite: true)`, which works inside local app/App Group directories but **not** across Android SAF `ContentResolver` output streams.
- `docs/research/011-uno-platform-migration-research.md:29–31` and `016:65` confirm zero mobile probe code exists, host workloads previously failed `NETSDK1147`, Mac disk space is constrained (~7.7 GiB), iOS keyboard extension Native AOT support is unknown, and Android `PublishAot=true` raises `XA1040`.

## 4. Design

### 4.1 Approach

1. **Toolchain & strict Native AOT gate first (MG1):** Under `prototypes/uno-mobile/`, construct minimal isolated app + extension/IME targets before writing UI code. Inspect effective MSBuild properties (`PublishAot`, `RuntimeIdentifier`, `_IsPublishing`, `NoWarn`, `SuppressTrimAnalysisWarnings`) and compiled binary headers (`otool -hv` / `nm` on iOS Mach-O binaries for App and Keyboard Extension; ELF `.so` inspection on Android). Explicitly test `.NET for Android` `PublishAot=true`: when `XA1040` fires (or if Android IME `InputMethodService` subclassing fails under NativeAOT JNI marshaling), record the exact diagnostic as `BLOCKED` and escalate an owner decision matrix (e.g., pause Android Phase 2, owner-approved runtime exception for Android/extension binary, or thin native Kotlin IME / Swift `UIInputViewController` hosting no managed runtime while reading the shared local projection). Apply the same rule if `.NET for iOS` rejects `PublishAot=true` on `com.apple.keyboard-service` app extensions or exceeds iOS Jetsam limits.
2. **Native minimal keyboard UI, sensitive-field guards & Uno management host:** Host Uno XAML only in the management app (`ios-arm64`, `android-arm64`). Implement `UIInputViewController` (iOS) and `InputMethodService` (Android) with minimal native UI (`UIKit` / Android `View`) to minimize extension startup latency and `phys_footprint` (`017 §4.1`). Separate platform security semantics: on **iOS**, observe OS system-keyboard substitution on `isSecureTextEntry`/keypad/opted-out hosts plus defensive `textDocumentProxy` trait checks; on **Android** (where the OS routinely invokes the active custom IME on password/PIN fields), explicitly inspect `EditorInfo.inputType` (`TYPE_TEXT_VARIATION_PASSWORD`, `VISIBLE_PASSWORD`, `WEB_PASSWORD`, `TYPE_NUMBER_VARIATION_PASSWORD`) and `IME_FLAG_NO_PERSONALIZED_LEARNING` to suppress snippet UI/insertion/surrounding-text reads and expose one-tap IME switching.
3. **Decoupled projection, identity-bound receipts & non-atomic window semantics:** Management app owns `smartsnippets.db` exclusively on its UI thread and atomically publishes `keyboard-projection.v1.json` carrying `libraryId` (persistent DB UUID), monotonic `projectionGeneration`, and per-snippet `snippetUid` (preventing SQLite rowid reuse `Migrations.cs:16` or replaced-DB receipts from crediting the wrong snippet). Distinguish **positively attempted native dispatch** (`insertText` returning `Void` without throwing on an active iOS proxy; `commitText(..., 1)` returning `true` on an active Android `InputConnection`—neither of which proves host-editor persistence) from **verified target delivery** (measured by reading owned fixture buffers). Because native host dispatch and filesystem receipt `rename` cannot commit atomically, document and test the explicit crash boundary: UUID ledger reconciliation guarantees **at-most-once credit per persisted receipt** (never 2+), whereas a kill *between* `insertText`/`commitText` and receipt `rename` yields an uncredited (`+0`) lost-receipt window with **zero automatic replay**.
4. **Permissions, Full Access, zero-network proof & document providers:** Test iOS App Group readability with `RequestsOpenAccess = false` vs `true` on physical iOS devices. Because iOS requires **no entitlement** for ordinary outbound networking (and `RequestsOpenAccess = true` enables extension networking at the OS level), prove zero-network compliance via **source/dependency/Mach-O imported-symbol audit (`URLSession`/`CFNetwork`/`NWConnection`/`System.Net.*` absence) + runtime network/capture check**, alongside Android's OS-enforced `android.permission.INTERNET` manifest omission. Test `UIDocumentPickerViewController` + Android SAF interrupted writes to define truthful `DocumentWriteOutcome` contracts for 023.

### 4.2 Alternatives considered

| Option | Pros | Cons | Verdict |
|---|---|---|---|
| A — Uno management app + native UIKit/Android-View keyboard reading atomic local projection, gated on strict AOT | Minimizes device-dependent iOS extension `phys_footprint`, isolates SQLite to management UI thread, surfaces `XA1040`/extension AOT honestly | Two small native keyboard list/search views to maintain in 024 | **Chosen** — matches master `017 §4.1, §4.3`, subject to 022 device measurements |
| B — Embed Uno XAML inside `UIInputViewController` and `InputMethodService` | Single UI framework everywhere | High risk of iOS Jetsam termination at 10k snippets, slow cold keyboard pop-up, unproven extension lifecycle | Rejected as baseline; measure only if Option A proves trivial |
| C — Suppress `XA1040` or silently fall back to Mono AOT (`RunAOTCompilation=true`) | Produces green CI builds immediately | Violates G1 strict Native AOT release rule and hides unsupported Microsoft production status | Rejected — strictly forbidden |
| D — Share live `smartsnippets.db` read/write across app and keyboard processes | No projection/receipt files | Cross-process SQLite lock contention, WAL shm hazards on iOS background suspend (`0xdead10cc` termination!) | Rejected — iOS terminates background/extension processes holding file locks (`0xdead10cc`) |

### 4.3 Data / config / API model

- **Probe identities:** `com.smartsnippets.mobprobe` + `com.smartsnippets.mobprobe.keyboard` (App Group `group.com.smartsnippets.mobprobe` on iOS; single package `com.smartsnippets.mobprobe` with `InputMethodService` on Android).
- **Candidate local exchange format (to validate for 023/024):**
  - `keyboard-projection.v1.json`: `{ schemaVersion: 1, libraryId: "<uuid>", generation: <monotonic>, sort: <SortField>, folders: [...], snippets: [{ id, snippetUid: "<uuid>", label, text, folder, tags, usageCount, lastUsedAt, createdAt, updatedAt }] }`.
  - `receipts/<receiptId>.json`: `{ schemaVersion: 1, receiptId: "<uuid>", libraryId: "<uuid>", projectionGeneration: <long>, snippetId: <long>, snippetUid: "<uuid>", timestampUtc: <epochSeconds> }`.
- **Empirical budget gates (on 10,000-snippet synthetic fixture):** Cold keyboard ready latency, observed iOS extension peak `phys_footprint` + `os_proc_available_memory()` headroom across physical test devices (no hardcoded universal MB assumption; establishes an owner-approved target budget + margin for 024/025), Android IME PSS, and total signed `.ipa` / `.apk`/`.aab` byte size.

### 4.4 Sequence diagram of the new hot path

```text
Management App (UI thread) → SnippetStore commit → ProjectionPublisher
  → write temp projection → atomic rename to keyboard-projection.v1.json (libraryId + gen N + snippetUid)
Target Editor focused → OS launches iOS Keyboard Extension / Android IME (separate lifecycle)
  → Keyboard loads latest projection; evaluates field security:
    ├─ iOS secure/opted-out host → OS substitutes system keyboard (or defensive trait block; 0 dispatch, 0 receipt)
    ├─ Android password/PIN/NO_PERSONALIZED_LEARNING EditorInfo → IME suppresses snippets & surrounding-text read; shows IME-switch prompt (0 dispatch, 0 receipt)
    └─ Allowed field → UITextDocumentProxy.insertText (Void) / InputConnection.commitText (bool == true)
         ├─ Positively attempted native call → write temp → atomic rename receipts/<uuid>.json (NO clipboard touch)
         │    [Non-atomic window: kill BEFORE rename leaves 0 receipt / no auto-replay; target text verified via owned fixture]
         └─ commitText == false / exception / invalidated proxy → 0 receipt, 0 automatic retry
Management App foreground/open → ReceiptReconciler reads receipts/*.json
  → matches (libraryId + snippetId + snippetUid) & deduplicates receiptId in SQLite → credits at most once per receipt → unlinks
Parallel paths checked:
  - Explicit Copy in management app → OS clipboard write only, zero receipt/usage.
  - Stale receipt from deleted row (reused numeric id) or replaced libraryId → rejected safely with zero mis-attributed credit.
```

### 4.5 Surface list

| Surface | Change | Task |
|---|---|---|
| `prototypes/uno-mobile/` build & artifact inspector | Workload check, `ios-arm64`/`android-arm64` strict AOT publish, `XA1040` / Mach-O / ELF / manifest validator | T0, T1 |
| Management SQLite + document-provider probe | Real `Core` `Migrator`/`SnippetStore` on device + iOS/Android picker read/interrupted-write probe | T2 |
| Local projection publisher + receipt reconciler | Atomic 10k projection write, `0xdead10cc`-safe lockless read, idempotent UUID receipt import | T3 |
| Minimal iOS `UIInputViewController` & Android `InputMethodService` | Physical-device text insertion, secure-field/opt-out observation, Full Access matrix, 10k memory/size profiling | T4, T5 |
| `docs/research/NNN-uno-mobile-feasibility-results.md` | PASS/BLOCKED/NOT RUN evidence matrix + owner decision package for 023/024 | T6 |

## 5. Impact and risk

| Question | Answer |
|---|---|
| State management — what survives a crash mid-operation? | Disposable probe container only; atomic rename ensures keyboard sees either complete old or complete new projection across `SIGKILL`. |
| Data consistency — orphans, races, double-apply? | Zero shared SQLite locks across processes (preventing iOS `0xdead10cc`); `(libraryId, snippetUid, receiptId)` ledger prevents double-counting or reused-id mis-attribution, with explicit non-replayed `+0` lost-receipt window if killed before receipt `rename`. |
| User experience — root problem or symptom; any surprise? | Non-shipping probe; surfaces real iOS Full Access prompts, iOS system-keyboard substitution, and Android IME password-field restriction UX early so 023/024 never promise impossible behavior. |
| Backward compatibility — existing data / sessions / configs? Migration? | Exercises existing v1/v2 SQLite schema and Native JSON v1 fixtures unchanged; touches zero desktop data. |
| Error recovery — what happens on failure; can it recover? | Corrupt/missing projection shows actionable "Open Smart Snippets to sync library" state in keyboard without crashing host editor. |
| Logging & debugging — enough to diagnose in the field? | Probe logs emit allowlisted stage/memory/latency tokens only; synthetic-fixture scans inspect probe logs and captured `os_log`/`logcat` streams without claiming blanket control over OS framework messages. |
| Edge cases — empty, huge, repeated, concurrent, interrupted? | Explicitly profiles 0 and 10,000 snippets, emoji/RTL/multiline payloads, rapid taps, deleted/reused rowids, phase-specific kills before/after receipt `rename`, and revoked Full Access. |

**Risks:**
- **R1 — Android `XA1040` blocks production Native AOT (`https://learn.microsoft.com/en-us/dotnet/android/messages/xa1040`)** → Treat as hard `BLOCKED` gate in T1; present owner options (defer Android, owner-approved runtime exception, or pure Kotlin IME + owner-approved app runtime) without suppressing `XA1040`.
- **R2 — iOS keyboard extension Native AOT unsupported or exceeds device-dependent Jetsam limit on 10k library** → Empirically profile physical-device `phys_footprint` and termination thresholds (never assuming a universal fixed MB limit); if managed AOT extension is rejected or unstable, evaluate pure Swift `UIInputViewController` reading compact binary/mmap index, subject to owner approval.
- **R3 — iOS App Group blocked when `RequestsOpenAccess=false`** → Test both modes on physical iOS hardware; if `RequestsOpenAccess=true` is required, obtain explicit owner sign-off and prove zero-network behavior via source/symbol/PacketLogger audit (since iOS uses no separate network entitlement and `RequestsOpenAccess=true` unlocks extension networking).
- **R4 — Missing mobile workloads (`NETSDK1147`) and constrained Mac disk (~7.7 GiB)** → Require disk cleanup/provisioning before T0; record `NOT RUN / BLOCKED` rather than faking simulator-only or doc-only PASS.

**Rollback:** Probe lives exclusively in `prototypes/uno-mobile/` and uninstalls cleanly from test devices; zero production code, CI, or user data is modified.

## 6. Tasks

### T0 — Provision mobile toolchains, disk headroom, strict-AOT & zero-network validator (AC4, AC6)
- **Files:** `prototypes/uno-mobile/README.md`, `prototypes/uno-mobile/validate_mobile_artifacts.py`
- **Change:** Verify Mac free disk space, `.NET` iOS/Android workloads, Xcode/Android NDK versions, and build an automated validator that inspects `PublishAot=true`, rejects any `NoWarn` for `XA1040`/`IL2xxx`/`IL3xxx` or Mono AOT fallback, verifies Mach-O/ELF native code + `e_sqlite3`, fails if Android `android.permission.INTERNET` is present, and scans iOS/Android probe source/dependencies/Mach-O symbols for forbidden networking APIs (`URLSession`, `CFNetwork`, `NWConnection`, `System.Net.Http`, `System.Net.Sockets`).
- **Verify:** Unit tests on `validate_mobile_artifacts.py` with planted Mono-AOT, `XA1040`-suppressed, management-only, Android `INTERNET`, and iOS `URLSession`-symbol fixtures.
- **Test that dies if this breaks:** `MobileEvidenceGateTests.RejectsMonoAot_SuppressedXa1040_ManagementOnlyArtifact_AndroidInternet_OrIosNetworkSymbols`.

### T1 — Attempt strict Native AOT app + extension/IME builds & escalate `XA1040` / linker blockers (AC4)
- **Files:** `prototypes/uno-mobile/` project files (`UnoMobileProbe`, iOS Keyboard Extension, Android IME), `docs/research/NNN-uno-mobile-feasibility-results.md`
- **Change:** Attempt clean Release `PublishAot=true` builds for `ios-arm64` (app + keyboard extension) and `android-arm64` (app + IME). Record exact compiler/linker output (including expected Android `XA1040`). If blocked, pause affected target and request explicit owner decision via supervisor/G2 gate before device work proceeds on a revised runtime/extension boundary.
- **Verify:** Fresh CLI publish logs and `validate_mobile_artifacts.py` inspection on host.
- **Test that dies if this breaks:** `StrictAotBuildCheckTests.FailsUnlessBothAppAndKeyboardArtifactsPassUnsuppressedNativeAotOrReportBlocked`.

### T2 — Physical-device SQLite persistence & sandbox document-provider probe (AC5, AC6)
- **Files:** `prototypes/uno-mobile/DataAndDocumentProbe.cs`
- **Change:** Run real `Core` `Migrator`, `SnippetStore`, and `NativeExport`/`SnippetImport` on physical iOS and Android devices against v1/v2 fixtures; exercise `UIDocumentPickerViewController` and Android SAF read, cancel, and simulated mid-write failure to observe truncation vs atomic replacement behavior.
- **Verify:** Private device-harness assertions verifying Unicode/multiline roundtrip, safe failure on corrupt/newer DB, and zero leaked URIs/content in `os_log`/`logcat`.
- **Test that dies if this breaks:** `MobileDataAndProviderProbeTests.VerifiesRealSqliteMigration_AndRecordsActualProviderWriteAtomicity`.

### T3 — Cross-process 10k projection, `(libraryId, snippetUid)` binding & phase-specific crash proof (AC4, AC5)
- **Files:** `prototypes/uno-mobile/Sharing/ProjectionPublisher.cs`, `ReceiptReconciler.cs`
- **Change:** Implement lockless atomic projection publishing (`0xdead10cc`-safe, zero cross-process SQLite handles) and `(libraryId, projectionGeneration, snippetId, snippetUid)`-bound UUID receipt reconciliation; test fault injection (a) before receipt `rename` (`.tmp` left → `+0` credit, no replay), (b) after receipt `rename` before/after DB commit (`+1` credit once, `+0` on replay), and (c) deleted/reused rowid & replaced `libraryId` rejection.
- **Verify:** Automated multi-process/fault-checkpoint tests + 10k-snippet serialization/parse timing check.
- **Test that dies if this breaks:** `MobileReceiptTests.EnforcesPhaseSpecificKillOutcomes_DeduplicatesPersistedReceipts_AndRejectsReusedRowIdOrWrongLibrary`.

### T4 — Physical iOS `UIInputViewController` & Android `InputMethodService` probe + empirical 10k memory/size profiling (AC4, AC5, AC6)
- **Files:** `prototypes/uno-mobile/Platforms/iOS/KeyboardExtension/*`, `Platforms/Android/Ime/*`
- **Change:** Install signed app + keyboard/IME on physical iOS and Android devices (for targets cleared in T1/owner decision); verify owned-fixture target buffer delivery (`insertText` `Void` / `commitText` `bool`), iOS OS-suppressed secure/opted-out fields, Android `EditorInfo` password/PIN/`NO_PERSONALIZED_LEARNING` snippet-UI & surrounding-text suppression, iOS Full Access off vs on App Group access + runtime zero-network capture, empirical 10k-snippet cold launch time, peak `phys_footprint`/PSS vs device termination limits, and `.ipa`/`.apk` sizes.
- **Verify:** Device profiler logs (`xcrun xctrace` / `dumpsys meminfo`), owned target-field buffer readbacks, Android password-field UI assertions, and zero clipboard/network activity.
- **Test that dies if this breaks:** `PhysicalKeyboardProbeTests.VerifiesFixtureBufferDelivery_RestrictsAndroidPasswordEditorInfo_AndRecordsEmpirical10kMemory`.

### T5 — Feasibility wiring audit & 023/024 contract decision report (AC4–AC6)
- **Files:** `docs/research/NNN-uno-mobile-feasibility-results.md`
- **Change:** Audit every probe producer → shared container file → native extension reader → proxy dispatch → receipt reconciler → `SnippetStore.RecordUsage` hop; tabulate PASS/BLOCKED/NOT RUN per device/OS/gate, exact package/memory numbers, Full Access/permission findings, and concrete OS floors/contracts requiring user approval before detailing 023 and 024.
- **Verify:** `validate_mobile_artifacts.py` + planted synthetic leak scan across device `os_log`/`logcat` captures.
- **Test that dies if this breaks:** `MobileFeasibilityReportAuditTests.RejectsPassWithoutBothKeyboardArtifact_10kMemoryMeasurement_AndCleanPrivacyScan`.

## 7. Test strategy

- **Unit:** Artifact/property gate (`validate_mobile_artifacts.py`), 10k projection serializer/reader, atomic temp-rename recovery, and UUID receipt deduplicator.
- **Integration:** Real `SnippetStore` → `ProjectionPublisher` → simulated separate-process keyboard reader → `ReceiptReconciler` → `SnippetStore.RecordUsage` flow (verifying Copy produces 0 receipts and crash leaves DB consistent).
- **Manual runbook (physical iOS & Android devices only — simulators are diagnostic only and cannot mark 022 PASS):**

| # | Step | Expected |
|---|---|---|
| 1 | Run clean `ios-arm64` & `android-arm64` strict AOT publish + `validate_mobile_artifacts.py` | Either verified native app+keyboard binaries with 0 warnings, no Android `INTERNET`, and 0 iOS/Android network symbols, or explicit `BLOCKED` (`XA1040` / linker) escalated to owner |
| 2 | Install cleared build(s) on physical device in Airplane Mode; seed 10k synthetic snippets in management app; kill & relaunch app | All 10k snippets + Unicode/multiline persist offline |
| 3 | Enable custom keyboard/IME (testing iOS Full Access OFF first, then ON if needed with network capture); open owned standard text field, search 10k set, tap snippet | Owned target buffer receives exact text without clipboard change; record empirical peak iOS `phys_footprint` & Android PSS with zero Jetsam kills and zero network packets |
| 4 | Focus iOS secure/opted-out fields and Android `TYPE_*_PASSWORD`/`NO_PERSONALIZED_LEARNING` fields; run instrumented pre-rename vs post-rename kill checkpoints plus live mid-session process kill | iOS swaps to system keyboard; Android IME hides snippets/context & offers IME switch (`0` receipts); pre-rename kill yields `+0` credit (no replay), post-rename kill yields `+1` once (`+0` on second reopen), live kill yields `∈ {0, 1}` matching durable receipt (never `≥ 2`) |
| 5 | Export/import JSON via system document picker (including user cancel) and run synthetic-fixture scan on probe logs + captured `os_log`/`logcat` | Library intact on cancel; zero planted snippet text/labels/tags/queries/URIs found in scanned logs |

## 8. Rollout / phasing

Non-shipping feasibility gate. Output feeds an explicit owner go/no-go and contract sign-off before Plans 023 and 024 can be promoted from provisional drafts to approved plans.

## 9. Open questions / dependency & assumption register

| ID | Dependency / Assumption | Status | Owner / Gate |
|---|---|---|---|
| D1 | Android `XA1040`: Microsoft marks `.NET for Android` `PublishAot=true` experimental & unsupported for production (https://learn.microsoft.com/en-us/dotnet/android/messages/xa1040) | **Known upstream blocker** — requires explicit owner decision (no silent Mono AOT or `NoWarn`) | User decision in 018 T7 / 022 T1 |
| D2 | `.NET for iOS` custom keyboard extension (`com.apple.keyboard-service`) strict Native AOT + empirical 10k `phys_footprint` vs undocumented device/OS Jetsam limits | **Unknown / unproven** (`NETSDK1147` on current hosts; `016:65`) | Measure empirically in 022 T1/T4 → owner-approved budget + margin |
| D3 | iOS App Group readability with `RequestsOpenAccess = false` vs `true` + zero-network source/symbol/runtime proof; Android sensitive-field IME policy | **Unknown on device / owner policy gate** | Measure in 022 T4 → owner decision before 023/024 |
| D4 | Mac disk space (>30 GiB recommended vs ~7.7 GiB currently free), mobile workloads, Apple provisioning profile (App Group + Extension), and physical iOS/Android devices | **External prerequisites** — absence keeps gates `NOT RUN / BLOCKED` | User + operator before 022 execution |

## 10. Approval log

| Date | Event | Notes |
|---|---|---|
| 2026-10-30 | Requirement brief confirmed (G1) | Confirmed in master plan `017` (Phase 2 iOS/Android management + keyboard extensions, strict Native AOT, prove first) |
| 2026-10-30 | Provisional candidate drafted | Written to `docs/drafts/uno-019-025/`; depends on 021 & owner AOT/workload gates; **unapproved, not implementation-ready** |
