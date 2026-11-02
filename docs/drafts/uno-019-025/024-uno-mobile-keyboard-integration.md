# 024 — Uno mobile keyboard integration

**Date:** 2026-10-30
**Status:** Provisional draft—dependency evidence pending; unapproved, not implementation-ready
**Size:** L
**Area:** `src/SmartSnippets.Keyboard.iOS/` (or platform extension target), `src/SmartSnippets.Uno/Platforms/Android/Ime/`, `src/SmartSnippets.Core/`, `src/SmartSnippets.Presentation/`, `tests/`
**Requirement brief confirmed:** 2026-10-30 (G1, master 017)
**Master:** [017 — Master plan: Uno cross-platform migration](../../plan/017-master-uno-cross-platform-migration.md)
**Canonical placeholder:** [024 placeholder](../../plan/024-uno-mobile-keyboard-integration.md) (unchanged until 018–023 complete)

---

## 1. Goal

Ship the production iOS Custom Keyboard Extension (`UIInputViewController`) and Android Input Method Service (`InputMethodService`) for snippet search, folder filtering, and direct native text dispatch—using the 022-proven runtime/UI boundary, lockless local projection snapshots, and `(libraryId, snippetUid, projectionGeneration)`-bound deduplicated usage receipts reconciled into the management store—while enforcing the 022-approved 10,000-snippet memory/package budgets, audited zero-network operation, and platform-specific sensitive-field restrictions.

## 2. Requirement (as confirmed at G1)

Excerpted from `docs/plan/017-master-uno-cross-platform-migration.md:19–55` and `docs/plan/024-uno-mobile-keyboard-integration.md:7–21`:

- **Problem:** Plan 023 delivers mobile snippet management and writes a local projection snapshot, but users still lack native iOS and Android snippet keyboards capable of searching and inserting snippets directly into target apps without clipboard overwriting, sensitive-field exposure on Android IMEs, SQLite rowid-reuse receipt mis-attribution, or cross-process SQLite corruption.
- **In scope:** Native iOS custom keyboard extension (`com.apple.keyboard-service`) and Android `InputMethodService` (built under 022-approved strict AOT/toolchain rules); minimal native keyboard UI (folder filter, `OrdinalIgnoreCase` multi-term search matching `SnippetSearch.cs`, sort order, globe/IME-switch key, dark/light theme); freshness check on projection `generation`; native `UITextDocumentProxy.insertText` (`Void`) / `InputConnection.commitText` (`bool`) dispatch; denied-sharing/empty-library onboarding states; distinct iOS secure/opt-out handling and explicit Android `EditorInfo` password/PIN/`NO_PERSONALIZED_LEARNING` restriction; identity-bound UUID usage receipts and single-transaction management-store deduplication; 022-approved 10k-snippet memory and package-size gates.
- **Out of scope / non-goals:** Full QWERTY typing replacement (snippet picker keyboard with instant globe/IME switch back to system keyboard), desktop focus/clipboard paste injection, automatic retry or crash replay on uncertain/interrupted dispatch, usage credit for manual Copy, any network code/dependencies (audited statically and behaviorally on iOS regardless of `RequestsOpenAccess`, plus omitting Android `INTERNET`), cloud sync, password snippets (`016`).
- **Users and surfaces:** iOS and Android users invoking the Smart Snippets keyboard inside standard third-party text editors, messaging apps, browsers, and notes apps, then returning to the management app where persisted usage receipts reconcile.
- **Behaviour:** Focusing an allowed field and switching to Smart Snippets displays the latest committed snippets; tapping a row invokes the OS text API once (clipboard untouched) and writes one durable identity-bound receipt; on iOS, the OS swaps to the system keyboard on secure/opted-out fields, while on Android (which routes password fields to the active IME) the IME explicitly detects sensitive `EditorInfo` flags, hides snippet rows/surrounding-text reads, and offers one-tap IME switching; killing the app or keyboard never corrupts SQLite or credits a receipt twice, with an explicit non-replayed `+0` lost-receipt window if killed between native dispatch and receipt `rename`.
- **Constraints and assumptions:** Strictly conditional on Plan 022 physical-device proof (especially Android `XA1040` Native AOT experimental status https://learn.microsoft.com/en-us/dotnet/android/messages/xa1040, iOS extension Native AOT + empirical physical-device `phys_footprint` ceiling & owner-approved margin on 10k snippets, and iOS App Group `RequestsOpenAccess` requirements) and Plan 023 completion.
- **Acceptance criteria:** Satisfies implementation of master **AC4** (mobile keyboards + documented restrictions), **AC5** (permissions, failure handling, idempotent usage accounting), and **AC6** (offline, content-free extension/app logs).
- **Decisions made:** Never share UI-owned `SnippetStore` or SQLite connections across processes (`017 §4.3`); minimal native extension UI where Uno XAML is unsafe (`024 placeholder`); uncertain dispatch earns no usage and no auto-retry (`024 placeholder`).

## 3. Current state (as-built)

- `src/SmartSnippets.Core/Query/SnippetSearch.cs:5–87` and `SnippetSort.cs:5–46` implement allocation-light `OrdinalIgnoreCase` AND-of-terms substring search (over Label, Text, Folder, Tags) and deterministic `LastUsed`/`UsageCount`/`Created`/`Updated` tie-broken sorting—reusable directly in managed AOT keyboard code or mirrored identically if 022 selects a thin Swift/Kotlin extension reader.
- `src/SmartSnippets.Core/Store/SnippetStore.cs:21,275–294,316` enforces single-threaded `_ownerThreadId` ownership and `RecordUsage(long id)` using current `_time.GetUtcNow()`, while `Migrations.cs:16` uses `id INTEGER PRIMARY KEY` (which can reuse deleted SQLite rowids); neither stable `snippet_uid`/`library_id` binding nor receipt-UUID deduplication exists yet.
- `src/SmartSnippets.Core/Data/Migrations.cs:5–33` defines schema v1 and v2 (`folders`, `snippets`, `settings`); `Migrator.cs:46–49` automatically creates `smartsnippets.db.v2.bak` before applying any v3 schema migration.
- No iOS app-extension project, Android `InputMethodService`, `Info.plist` `NSExtension`, or Android `method.xml` exists in the repository today (`SmartSnippets.slnx:1–12`).

## 4. Design

### 4.1 Approach

1. **Minimal native keyboard hosts & sensitive-field guards (022-locked stack):**
   - **iOS (`SmartSnippetsKeyboardExtension`):** `UIInputViewController` with native `UIKit` (internal filter strip, horizontal folder pills, virtualized `UITableView`, and mandatory `advanceToNextInputMode` globe button when `needsInputModeSwitchKey == true`). Follows `traitCollection.userInterfaceStyle` + projected palette accent. Zero Uno Skia/XAML runtime loaded in the extension process so 10k-snippet `phys_footprint` stays within the 022 empirical device budget. While iOS itself replaces custom keyboards on `isSecureTextEntry` and opted-out hosts, the controller also checks `textDocumentProxy.isSecureTextEntry == true` defensively and refuses snippet display/dispatch.
   - **Android (`SmartSnippetsInputMethodService`):** Native `InputMethodService` (`res/xml/method.xml`, `BIND_INPUT_METHOD`) rendering a lightweight Android `RecyclerView` + folder chip strip + inline filter + `switchToNextInputMethod` / `showInputMethodPicker` key. Because Android **commonly presents the active custom IME on password and PIN fields**, `onStartInput` / `onStartInputView` explicitly inspects `EditorInfo.inputType` (`TYPE_TEXT_VARIATION_PASSWORD`, `VISIBLE_PASSWORD`, `WEB_PASSWORD`, `TYPE_NUMBER_VARIATION_PASSWORD`, `TYPE_NULL`) and `imeOptions` (`IME_FLAG_NO_PERSONALIZED_LEARNING`): on password/PIN/non-editable fields it hides snippet UI & insertion and shows a restricted card with one-tap IME switch (`0` receipts); on `NO_PERSONALIZED_LEARNING` or any sensitive field it never queries surrounding editor text (`getExtractedText`/`getTextBeforeCursor`) or retains filter history.
2. **Lockless snapshot freshness & bounded memory:** On `viewWillAppear` (iOS) / `onStartInputView` (Android), inspect `(fileSize, mtimeUtc)` or header `(libraryId, generation)` of `keyboard-projection.v1.json` in the shared container (`group.<bundle-id>` on iOS; `files/keyboard/` on Android). Reload and index only when `(libraryId, generation)` changed; release cached row views on `didReceiveMemoryWarning` / `onTrimMemory`. If the container is unreadable or projection is absent, render an actionable onboarding banner with zero crash.
3. **Native proxy dispatch vs target persistence & non-atomic crash window:**
   - **iOS:** Verify `textDocumentProxy` is non-null and non-secure, then invoke `textDocumentProxy.insertText(text)`. Because `insertText` returns `Void` (never confirming host persistence), define **positively attempted dispatch** as normal return without exception while the controller/proxy remained active, and write one receipt; actual editor buffer delivery is verified via owned test fixtures.
   - **Android:** Acquire `currentInputConnection`; if `null` or restricted by `EditorInfo`, show inline status (`NotDispatched`, zero receipt). Otherwise run `ic.beginBatchEdit()`, `var ok = ic.commitText(text, 1)`, `ic.endBatchEdit()`; `ok == true` indicates IPC acceptance (not guaranteed target persistence) and writes one receipt, whereas `ok == false` or exception writes **no** receipt and **never** auto-retries. Neither platform touches `UIPasteboard` or `ClipboardManager`.
   - **Non-atomic boundary:** Host text dispatch and filesystem receipt `rename` cannot commit atomically. Killing the keyboard *after* `insertText`/`commitText` and *before* atomic receipt `rename` leaves `.tmp` (or nothing), resulting in `+0` usage credit with **no automatic replay** (preventing duplicate text). Once `rename` succeeds, reconciliation credits **at most once**.
4. **Identity-bound receipt ledger & single-transaction store reconciliation:**
   - **Receipt ownership:** Keyboard writes one immutable file `receipts/<receiptId>.json` binding `(receiptId, libraryId, projectionGeneration, snippetId, snippetUid, dispatchedAtUtc)` via temp write + atomic `rename`. Keyboard never deletes existing receipts and never opens `smartsnippets.db`.
   - **Store reconciliation (Migration v3 in `Core`):** Add `snippet_uid TEXT NOT NULL` on `snippets` (`UNIQUE` index), `library_id` in `settings`, and `usage_receipts (receipt_id TEXT PRIMARY KEY, library_id TEXT NOT NULL, snippet_id INTEGER NOT NULL, snippet_uid TEXT NOT NULL, projection_gen INTEGER NOT NULL, dispatched_at INTEGER NOT NULL, applied_at INTEGER NOT NULL)` in `Migrations.cs`. `ISnippetStore.ReconcileUsageReceipts(batch)` runs in **one** UI-thread `SqliteTransaction`: `INSERT OR IGNORE INTO usage_receipts`; iff `changes() == 1` **and** `receipt.libraryId == currentLibraryId` **and** a row matches `(id = receipt.snippetId AND snippet_uid = receipt.snippetUid)`, increment `usage_count` and update `last_used_at = MAX(COALESCE(last_used_at, 0), $dispatched_at)` (stale/deleted/rowid-reused/wrong-library receipts record as seen/ignored with `0` credit, pruning entries >30 days / >10k cap). Only after SQLite `Commit()` succeeds does `KeyboardReceiptReconciler` unlink `receipts/<uuid>.json` and republish the projection.

### 4.2 Alternatives considered

| Option | Pros | Cons | Verdict |
|---|---|---|---|
| A — Native UIKit/Android-View keyboards + `(libraryId, snippetUid)` receipts + Migration v3 single-tx SQLite deduplication | Deduplicates persisted receipts (`Commit` before `unlink`), prevents reused-rowid mis-attribution, zero cross-process SQLite locks (`0xdead10cc`-safe), minimal memory | Explicit `+0` lost-receipt window if killed before `rename`; additive schema v3 migration (`.v2.bak` auto-created) | **Chosen** — preserves at-most-once receipt safety without risking duplicate text replay |
| B — Unlink receipt file before SQLite transaction or key receipts by numeric `snippetId` only | Avoids Migration v3 / `snippet_uid` | Crash before `File.Delete` double-counts; deleted rowid reuse (`Migrations.cs:16`) or DB restore credits wrong snippet | Rejected — fails deduplication and identity safety |
| C — Keyboard opens `smartsnippets.db` directly to call `RecordUsage` | Narrower receipt gap | Violates `017 §4.3`; causes `SQLITE_BUSY` and iOS `0xdead10cc` process kills; still not atomic with host editor | Rejected |
| D — Host Uno XAML inside keyboard extension/IME | Shared XAML templates | High risk of exceeding device-dependent iOS Jetsam limits on 10k snippets | Rejected unless 022 explicitly proved & owner chose it |

### 4.3 Data / config / API model

- **Schema v3 (`Migrations.cs`):** Adds `snippets.snippet_uid TEXT NOT NULL` (backfilled with unique UUIDs on v2→v3 upgrade + `CREATE UNIQUE INDEX ix_snippets_uid ON snippets(snippet_uid)`), initializes `settings['library_id']`, and creates:
  ```sql
  CREATE TABLE usage_receipts (
    receipt_id     TEXT    PRIMARY KEY,
    library_id     TEXT    NOT NULL,
    snippet_id     INTEGER NOT NULL,
    snippet_uid    TEXT    NOT NULL,
    projection_gen INTEGER NOT NULL,
    dispatched_at  INTEGER NOT NULL,
    applied_at     INTEGER NOT NULL
  );
  CREATE INDEX ix_usage_receipts_applied_at ON usage_receipts(applied_at);
  ```
- **Receipt file (`<SharedContainer>/receipts/<receiptId>.json`):** `{ "v": 1, "receiptId": "<uuid>", "libraryId": "<uuid>", "projectionGeneration": 7, "snippetId": 42, "snippetUid": "<uuid>", "dispatchedAtUtc": 1761868800 }` (4 KB cap; malformed/oversized files unlinked with content-free `receipt_corrupt` log).
- **Manifests & zero-network audit:** iOS `Info.plist` `NSExtensionPointIdentifier = com.apple.keyboard-service`, `RequestsOpenAccess` set strictly to 022-approved boolean, plus static source/dependency/Mach-O symbol audit confirming zero networking APIs (`URLSession`/`CFNetwork`/`NWConnection`/`System.Net.*`) because iOS requires no network entitlement; Android `AndroidManifest.xml` `BIND_INPUT_METHOD` + zero `android.permission.INTERNET`.

### 4.4 Sequence diagram of the new hot path

```text
1. Management App commit (023) → writes atomic keyboard-projection.v1.json (libraryId + gen N + snippetUid)
2. User focuses target app editor & selects Smart Snippets keyboard
   → iOS UIInputViewController.viewWillAppear / Android InputMethodService.onStartInputView
     → Android checks EditorInfo (password/PIN/NO_PERSONALIZED_LEARNING); iOS checks isSecureTextEntry
       ├─ Restricted field → suppress snippet rows & context reads; show IME/globe switch notice (0 dispatch, 0 receipt)
       └─ Allowed field → ProjectionReader checks (libraryId, gen N) → SnippetSearch.Filter → render native rows
3. User taps snippet row
   → UITextDocumentProxy.insertText(text) [Void] / InputConnection.commitText(text, 1) [bool]
     ├─ Positively attempted native call → write temp → atomic rename receipts/<uuid>.json
     │    [Kill BEFORE rename: .tmp ignored, +0 usage, NO replay; target buffer verified in fixture tests]
     └─ commitText == false / exception / inactive proxy → inline status; NO receipt, NO clipboard write, NO retry
4. User opens/resumes Management App (`DidBecomeActive` / `OnResume` on UI thread)
   → KeyboardReceiptReconciler reads receipts/*.json
     → SnippetStore.ReconcileUsageReceipts(batch) [single tx: INSERT OR IGNORE usage_receipts + match (libraryId, id, snippetUid)]
       → Commit succeeds → delete processed receipts/<uuid>.json → republish projection (gen N+1)
Parallel paths checked:
  - Management CopyRow (SnippetsPageViewModel.cs:135) → clipboard only, 0 receipts.
  - Deleted row with reused numeric id or replaced libraryId → snippetUid/libraryId mismatch skips credit cleanly.
```

### 4.5 Surface list

| Surface | Change | Task |
|---|---|---|
| `Migrations.cs` (v3), `SnippetRepository`, `ISnippetStore.ReconcileUsageReceipts` | Atomic SQLite receipt deduplication, max-timestamp update, deleted-snippet tolerance, ledger pruning | T1 |
| `KeyboardProjectionReader` + `UsageReceiptWriter` + `KeyboardReceiptReconciler` | Lockless generation-aware snapshot reader, atomic UUID receipt writer, foreground UI-thread reconciler | T2 |
| iOS Custom Keyboard Extension (`UIInputViewController`) | Native UIKit list/folder/search, globe switch, `insertText`, secure/denied states, memory-warning trim | T3 |
| Android `SmartSnippetsInputMethodService` | Native `RecyclerView`/filter/search, IME switch, `EditorInfo` secure check, `commitText` batching | T4 |
| Mobile packaging, 10k Jetsam/size gate & wiring audit | Bundle extension/IME into signed mobile builds, enforce 022 10k budgets, end-to-end wiring audit | T5, T6 |

## 5. Impact and risk

| Question | Answer |
|---|---|
| State management — what survives a crash mid-operation? | Renamed receipts survive `SIGKILL`/Jetsam; pre-rename `.tmp` files are ignored (`+0` lost-receipt window, zero auto-replay); uncommitted SQLite reconciliation retries pending receipt files on next resume without double-counting. |
| Data consistency — orphans, races, double-apply? | `usage_receipts` `PRIMARY KEY` + `(libraryId, id, snippetUid)` match inside one SQLite transaction prevents double-counting and protects against reused numeric rowids or replaced libraries. |
| User experience — root problem or symptom; any surprise? | Instant globe/IME switch back to typing keyboard; immediate visibility of management edits via `(libraryId, generation)` check; explicit Android password-field suppression + iOS secure-field compliance. |
| Backward compatibility — existing data / sessions / configs? Migration? | Additive Migration v3 backfills `snippet_uid` and `library_id` on v1/v2 DBs (creating `.v2.bak` per `Migrator.cs:46`); JSON export format stays v1 compatible. |
| Error recovery — what happens on failure; can it recover? | Corrupt projection, `.tmp` fragment, or malformed receipt is logged with allowlisted tokens (`projection_invalid` / `receipt_corrupt`) and cleaned/rebuilt without crashing the keyboard. |
| Logging & debugging — enough to diagnose in the field? | App/extension logs emit UTC timestamps and allowlisted counters (`kb_open`, `kb_gen`, `kb_dispatched`, `kb_restricted`, `receipts_reconciled`); synthetic-fixture scans inspect captured `os_log`/`logcat` without claiming blanket control over OS framework logs. |
| Edge cases — empty, huge, repeated, concurrent, interrupted? | 10k snippets within 022 empirical memory budget, 1,000 queued offline receipts, deleted+recreated rowid `42`, Android `TYPE_*_PASSWORD`/`NO_PERSONALIZED_LEARNING`, and pre-rename vs post-rename kills. |

**Risks:**
- **R1 — iOS Jetsam termination when filtering 10k snippets in extension** → Avoid LINQ/boxing churn on keystrokes, pre-index compact UTF-8/string arrays, drop caches on `didReceiveMemoryWarning`, and gate T5 on 022's physical-device `phys_footprint` ceiling.
- **R2 — Host apps rejecting inline `UISearchTextField` focus inside iOS custom keyboards** → Because iOS keyboard extensions cannot steal `firstResponder` from the host app's `UITextDocumentProxy` using standard `UITextField` without focus quirks, use an internal tap-to-filter mini keystrip / chip bar (as validated in 022 T4).
- **R3 — Schema v3 desktop cross-impact** → Because `Migrations.cs` is shared in `SmartSnippets.Core`, desktop also receives the harmless empty `usage_receipts` table on next release; verify desktop tests + `.v2.bak` migration roundtrip stay 100% green.

**Rollback:** Disable/remove extension target packaging before Phase 2 release tag if physical-device gates fail; desktop Phase 1 artifacts remain isolated on the Phase 1 release branch/tag until 025 passes.

## 6. Tasks

### T1 — Add Migration v3 (`snippet_uid`, `library_id`, `usage_receipts`) & `ReconcileUsageReceipts` in `Core` (AC4, AC5)
- **Files:** `src/SmartSnippets.Core/Data/Migrations.cs`, `SnippetRepository.cs`, `src/SmartSnippets.Core/Store/ISnippetStore.cs`, `SnippetStore.cs`
- **Change:** Backfill unique `snippet_uid` on `snippets`, persist `library_id` in `settings`, add `usage_receipts` (v3), and implement single-transaction `ReconcileUsageReceipts` matching `(libraryId, snippetId, snippetUid)` so duplicate receipts, deleted+reused rowids (`Migrations.cs:16`), and replaced libraries never mis-credit or double-count usage; prune aged ledger rows.
- **Verify:** `dotnet test tests/SmartSnippets.Core.Tests` sequentially (including v1→v2→v3 `.bak` migration tests).
- **Test that dies if this breaks:** `UsageReceiptStoreTests.DeduplicatesReceipts_AndRejectsDeletedReusedRowIdOrReplacedLibraryId`.

### T2 — Implement `KeyboardProjectionReader`, `UsageReceiptWriter`, and `KeyboardReceiptReconciler` (AC4, AC5, AC6)
- **Files:** `src/SmartSnippets.Core/Porting/KeyboardProjectionReader.cs`, `UsageReceiptWriter.cs`, `src/SmartSnippets.Presentation/Services/KeyboardReceiptReconciler.cs`, `src/SmartSnippets.Uno/MobileAppHost.cs`
- **Change:** Build `(libraryId, generation)`-cached lockless projection reader, atomic temp-rename identity-bound receipt writer (ignoring orphan `.tmp` files without replay), and UI-thread foreground reconciler wired to mobile `OnResumed`/`DidBecomeActive` that unlinks receipts **only after** `ReconcileUsageReceipts` commits.
- **Verify:** `dotnet test tests/SmartSnippets.Presentation.Tests` sequentially.
- **Test that dies if this breaks:** `KeyboardReceiptReconcilerTests.PreRenameTempFileCreditsZeroWithoutReplay_AndPostCommitCrashBeforeDeleteCreditsOnce`.

### T3 — Build native iOS Custom Keyboard Extension (`UIInputViewController`) (AC4–AC6)
- **Files:** `src/SmartSnippets.Keyboard.iOS/*` (csproj, `Info.plist`, `Entitlements.plist`, `KeyboardViewController.cs`)
- **Change:** Implement native UIKit snippet list, folder chips, internal filter strip, globe switcher (`advanceToNextInputMode`), `insertText` (`Void`) positive-attempt receipt write, defensive `isSecureTextEntry` guard, onboarding/denied-container view, `didReceiveMemoryWarning` trim, and static symbol audit rejecting networking APIs.
- **Verify:** Clean `ios-arm64` Release build (022-approved strict AOT flags) packaging `.appex` inside `.app` with 0 warnings and 0 network symbols.
- **Test that dies if this breaks:** `IosKeyboardControllerTests.InvokesProxyInsertText_WritesIdentityBoundReceipt_RefusesSecureProxy_AndContainsZeroNetworkSymbols`.

### T4 — Build native Android `SmartSnippetsInputMethodService` (AC4–AC6)
- **Files:** `src/SmartSnippets.Uno/Platforms/Android/Ime/*`, `Platforms/Android/AndroidManifest.xml`, `Resources/xml/method.xml`
- **Change:** Implement `InputMethodService` with native `RecyclerView`, folder/search filter, IME switcher button, explicit `EditorInfo` sensitive-field guard (`TYPE_TEXT_VARIATION_PASSWORD`, `VISIBLE_PASSWORD`, `WEB_PASSWORD`, `TYPE_NUMBER_VARIATION_PASSWORD`, `IME_FLAG_NO_PERSONALIZED_LEARNING`—hiding snippets/insertion and never reading surrounding editor text), batched `commitText(text, 1)` writing a receipt only on `true` return, and zero `INTERNET` permission.
- **Verify:** Clean `android-arm64` Release build (022-approved AOT configuration) + manifest & `EditorInfo`/`InputConnection` target-field tests.
- **Test that dies if this breaks:** `AndroidImeServiceTests.SuppressesSnippetsAndContextOnPasswordAndNoPersonalizedLearningFields_AndCreditsOnlyWhenCommitReturnsTrue`.

### T5 — Enforce 022-approved 10k-snippet physical-device memory, latency & package-size gates (AC4)
- **Files:** `scripts/verify-mobile-keyboard-budgets.py`
- **Change:** Deploy signed Release app + keyboard/IME with a 10,000-snippet library onto physical iOS and Android devices; verify cold keyboard open latency, peak iOS extension `phys_footprint` within the empirical owner-approved 022 budget + margin (zero Jetsam events), and combined `.ipa`/`.aab` size.
- **Verify:** Recorded profiler output on physical iOS and Android devices.
- **Test that dies if this breaks:** `MobileBudgetValidatorTests.FailsOnJetsamEvent_MissingExtensionBinary_OrExceededApproved022MemoryCeiling`.

### T6 — Wiring audit (AC4–AC6)
- **Files:** `tests/SmartSnippets.Presentation.Tests/Wiring/MobileKeyboardWiringAuditTests.cs`
- **Change:** Trace management edit → `KeyboardProjectionPublisher` → shared container → iOS `.appex` & Android `InputMethodService` → native proxy/`InputConnection` → `UsageReceiptWriter` → mobile `OnResumed` → `KeyboardReceiptReconciler` → `SnippetStore.ReconcileUsageReceipts` → refreshed projection, plus parallel `CopyRow` (`0` receipts) and secure-field refusal (`0` receipts).
- **Verify:** Physical-device trace of content-free receipts (`projection_published` → `kb_insert` → `receipts_reconciled`) + log leak scan.
- **Test that dies if this breaks:** `MobileKeyboardWiringAuditTests.FullAppToKeyboardToReconcilerChainIsConnected_AndCopyNeverWritesReceipt`.

## 7. Test strategy

- **Unit:** Schema v3 idempotent receipt SQL, deleted-snippet receipt handling, 30-day ledger pruning, `EditorInfo`/secure-proxy classification, and `KeyboardProjectionReader` generation caching.
- **Integration:** Real `SnippetStore` + `KeyboardProjectionPublisher` + `KeyboardProjectionReader` + `UsageReceiptWriter` + `KeyboardReceiptReconciler` flow across simulated crashes before/after SQLite commit and concurrent management edits.
- **Manual runbook (physical iOS & Android devices, Airplane Mode):**

| # | Step | Expected |
|---|---|---|
| 1 | Enable Smart Snippets keyboard in OS Settings; open Notes/Messages in Airplane Mode and switch to Smart Snippets | Latest snippets/folders appear offline; globe/IME-switch key returns cleanly to system keyboard |
| 2 | Tap a Unicode + multiline snippet twice in target editor; inspect OS clipboard | Exact multiline text inserted twice; OS clipboard completely unchanged |
| 3 | Edit a snippet in Smart Snippets app, switch straight back to target editor without killing keyboard | Updated text/label appears immediately via projection `generation` check |
| 4 | Kill Smart Snippets app, insert 3 snippets (persisting 3 receipts), kill keyboard, reopen app twice; also test delete-and-recreate row + pre-rename `.tmp` orphan | Persisted receipts credit `+3` on first reopen and `+0` on second; orphan `.tmp` credits `+0` with no replay; deleted/recreated row receives `0` stale credit |
| 5 | Focus iOS secure/opted-out fields and Android password/PIN/`NO_PERSONALIZED_LEARNING` fields; load 10k fixture; scan app/extension logs + captured `os_log`/`logcat` | iOS uses system keyboard; Android IME hides snippets/context & offers IME switch (`0` receipts); 10k stays within 022 memory budget; zero planted fixture strings in scanned logs |

## 8. Rollout / phasing

Ships together with Plan 023's mobile management UI into internal physical-device RC builds (`TestFlight` internal / signed Android RC) gated by Plan 025's release acceptance and cross-phase wiring audit.

## 9. Open questions / dependency & assumption register

| ID | Dependency / Assumption | Status | Owner / Gate |
|---|---|---|---|
| D1 | 022 resolution of Android `XA1040` (`PublishAot=true` unsupported for production) & iOS `.appex` Native AOT | **Pending** — if 022 selects thin Swift/Kotlin extension readers instead of C# AOT extensions, T3/T4 implement that exact native language boundary over the same JSON/receipt contract | User + implementer at 022 → 024 G2 |
| D2 | iOS keyboard inline search input ergonomics (tap-strip vs proxy filter) & `RequestsOpenAccess` setting | **Conditional on 022 T4 physical-device findings** | Lock before 024 G2 |
| D3 | Shared `Core` Migration v3 coordination with Phase 1 desktop | **Design note:** keep v3 strictly additive (`CREATE TABLE usage_receipts`) so desktop upgrades remain zero-risk | Verify in T1 across Linux/Win/Mac suites |

## 10. Approval log

| Date | Event | Notes |
|---|---|---|
| 2026-10-30 | Requirement brief confirmed (G1) | Confirmed in master plan `017` (Phase 2 native keyboard extensions/IMEs, explicit restrictions, idempotent usage) |
| 2026-10-30 | Provisional candidate drafted | Written to `docs/drafts/uno-019-025/`; depends on 022 & 023 completion; **unapproved, not implementation-ready** |
