# 016 — Password-type snippets

**Date:** 2026-10-30
**Status:** Draft — awaiting approval
**Size:** L
**Area:** Core domain/persistence/query/export; Presentation editor/projections; App encryption/editor/composition
**Requirement brief confirmed:** 2026-10-30 (G1, in chat)

## 1. Goal

Let a user explicitly mark a snippet as a password, require a safe manual label, mask its content in editors and previews, encrypt its persisted content using the current Windows account, and omit it from JSON exports without changing normal insertion.

## 2. Requirement (confirmed at G1)

The confirmed chat brief:

- Add a **Password** checkbox in both snippet editors.
- When checked, mask editing and every content preview, including hover and keyboard previews. No separate reveal button; unchecking returns to ordinary text mode.
- Require an explicitly entered label. Never derive it from password text. When converting an ordinary snippet, clear its auto-generated label.
- Encrypt password content in the database.
- Exclude password snippets from JSON exports and report how many were excluded.
- Preserve normal pasting of the original content.

**Confirmed security defaults:**
- Use Windows user-account protection (DPAPI), with no extra master password. A copied database cannot normally be decrypted under another Windows account; account/key loss can make passwords unrecoverable.
- Search password snippets by label, folder and tags—not password content.
- Labels and metadata remain unencrypted. Pasting necessarily exposes the password to the target app and temporarily to the clipboard.
- Encryption cannot retract plaintext already present in earlier exports, backups or historical disk data.

**Confirmed acceptance:** Password mode survives restart; stored content is encrypted; all preview/edit surfaces are masked; saving without a manual label fails; exports omit password snippets; pasting remains correct; ordinary snippets remain unchanged.

### Independently verifiable criteria

1. **AC1 — Mode and validation:** Both editor hosts support Password. Checked snippets require a nonblank custom label, never falling back to tags/text. Converting an auto-labeled snippet clears that label; existing explicitly custom labels remain. Mode participates in dirty/save/discard/reload behavior.
2. **AC2 — Storage:** Newly saved password content is encrypted before SQL binding, with no plaintext fallback. Reopening under the same Windows account recovers exact original text. Existing snippets migrate as ordinary text. Encryption/decryption failures are content-free, fail closed and do not overwrite committed data or publish an unsuccessful write.
3. **AC3 — Display:** Main/popup row projections, hover, keyboard preview and editors cannot reveal password content while the checkbox is checked. Toggling/recycling/cache refresh/reopening cannot leave stale plaintext. No separate reveal action is provided.
4. **AC4 — Search:** Both lists find password snippets by label/folder/tags but not by secret-only substrings. Ordinary snippet content search remains unchanged.
5. **AC5 — Export:** Native export excludes password rows before sizing/DTO creation/serialization, reports exported and excluded counts, and exports a valid document even if every snippet is excluded. Existing external secret-import exclusions stay in effect.
6. **AC6 — Insertion and compatibility:** Existing paste normalization, target validation, clipboard restoration and usage rules remain intact for password snippets. Explicit Copy actions remain deliberate clipboard exposure, not preview/reveal actions. Ordinary snippet workflows remain unchanged.
7. **AC7 — Security evidence:** No content, ciphertext or native error messages containing user data are logged. Native AOT build/publish passes with zero warnings. Automated evidence and authorized desktop/DPAPI evidence are recorded separately; no claim of protecting historical backups, process memory, clipboard history or a compromised Windows account is made.

**Non-goals:** Whole-database encryption; encrypting metadata; master-password/unlock sessions; portable password backups; importing/decrypting InkSpoke secrets; secure erasure of old disk data; changing insertion contracts or clipboard policy; a custom rich/multiline secure-input control.

## 3. Current state (as-built)

Paths below abbreviated as Core, Presentation and App refer to `src/SmartSnippets.*`.

- `Core/Domain/Snippet.cs:3–14` and `SnippetDraft.cs:4` contain text and label state but no password flag. `Core/Domain/LabelSuggester.cs:18–21` falls back to tags/text even for blank custom labels. `Core/Store/SnippetStore.cs:343–350` resolves the label before validation, so UI-only validation cannot enforce the new invariant.
- `Core/Data/Migrations.cs:5–33` currently ends at migration 2. `Migrator.cs:45–48,78–99` creates a pre-upgrade `VACUUM INTO` backup; `:118–152` applies each migration transactionally and refuses newer schemas. That backup may contain formerly ordinary plaintext.
- `Core/Data/SnippetRepository.cs:8–9,53–113,135–146` selects, binds and reads plaintext directly. Create/update/import all use this repository. `Core/Store/SnippetStore.cs:89–146` commits before publishing; `:300–330` maps/logs SQLite and I/O failures only. Cryptographic failures need an explicit sanitized mapping.
- `Core/Data/SqliteConnectionFactory.cs:51–53` enables WAL and foreign keys. Encryption of new row values does not remove earlier WAL/free-page/backups containing ordinary text.
- `Presentation/ViewModels/SnippetEditorViewModel.cs:175–183,216,443–453,467–526` derives suggestions, creates drafts, updates labels, captures dirty state and reloads state. `:328–329` has an explicit save-failure Copy command.
- `App/Controls/SnippetEditor.xaml:22–32` is a multiline TextBox directly bound to plaintext. `SnippetEditor.xaml.cs:38–61,86` guards user input against rebinding and focuses only that TextBox. Both main and compact popup editors reuse this control; collapsed plaintext controls must not remain bound to password text.
- `Presentation/ViewModels/SnippetRowViewModel.cs:24–41` includes hover content in equality and builds Preview/ContentText from raw text. `PopupViewModel.cs:173–189` separately caches raw ContentText; `App/Popup/PopupWindow.xaml.cs:426` uses that projection for root-owned keyboard preview. `App/Controls/SnippetRowDetails.xaml.cs:60` supplies ContentText to label hover for both lists.
- `Core/Query/SnippetSearch.cs:16–23` indexes body alongside label/folder/tags for both main and popup paths.
- `Core/Porting/NativeExport.cs:14–29` sizes and serializes every supplied snippet. `Presentation/ViewModels/SnippetPortingViewModel.cs:102–125` passes a store snapshot to a worker, writes it, then reports counts; `Core/Porting/PortingContracts.cs:92` currently has no exclusion count. `Core/Porting/InkSpokeFormat.cs:45–47` already excludes secret entries.
- `Presentation/Services/InsertionService.cs:33–40,119–123` resolves original store text by ID and records usage only for Inserted. Explicit copy paths independently resolve original text in `SnippetsPageViewModel.cs:142–148`, `PopupViewModel.cs:238–242,374`, and the editor. These paths must not accidentally copy the display mask.
- `App/AppHost.cs:103–107` constructs the store directly; there is no DI container to extend. `:115–132` fails startup if store load fails. Use this existing failure behavior rather than adding partially readable password records.
- InkSpoke reference: `D:/sources/work/inkspoke/inkspoke-main/src/InkSpoke.UI/Views/Settings/CommandMappingsDialog.axaml:988–1021` swaps normal/masked multiline Avalonia TextBoxes using EditIsSecret. `ViewModels/Settings/CommandMappingsViewModel.cs:1270–1290` encrypts before saving and `:1754–1757` uses a fixed preview mask. Its Avalonia password TextBox is not a WinUI API, and its no-copy policy is not adopted here.

**Prior decisions:** plans 003 (transactional storage), 005 (exact text/shared editor), 007 (explicit clipboard actions), 010 (export budgets/import secret exclusions), 012/014/015 (shared hover/keyboard preview) remain authoritative except for the password-specific behavior defined here. [Backlog 001](../backlog/001-password-type-snippets.md) identifies the same security boundaries.

## 4. Design

### 4.1 Storage and account protection

- Add `IsPassword`, default false, to Snippet and SnippetDraft. Preserve existing ordinary call sites with a trailing defaulted parameter; update fakes and equality-sensitive fixtures explicitly where needed.
- Migration 3 adds `is_password INTEGER NOT NULL DEFAULT 0 CHECK(is_password IN (0,1))`. Existing text is not automatically classified or encrypted. Retain existing migration backup and newer-version guard.
- Keep `snippets.text` as TEXT: ordinary rows hold original text; password rows hold a versioned envelope `dpapi:v1:<base64 protected bytes>`. The flag, not a prefix heuristic, selects decoding. An ordinary literal beginning with `dpapi:` remains ordinary text. Unknown/invalid password envelopes fail closed.
- Define `Core/Security/ISecretProtector.cs` and a content-free `SecretProtectionException`. Repository encrypts/decrypts at the persistence boundary; callers and insertion retain plaintext domain objects in memory. Use lossless UTF-16 code-unit serialization with explicit byte order so Unicode, CR/LF, whitespace and unusual code units do not silently change. Clear temporary byte buffers in finally blocks; managed string lifetime is not a secure-memory guarantee.
- Add `App/Services/WindowsSecretProtector.cs`: CsWin32-generated `CryptProtectData`/`CryptUnprotectData`, current-user scope (never LOCAL_MACHINE), UI forbidden, fixed application/version purpose entropy. Free native allocations with the correct allocator and zero sensitive temporary/native plaintext buffers before release. Do not use snippet labels/text as DPAPI descriptions or entropy. Direct DPAPI avoids a new user-managed encryption key or package.
- Wire an explicit protector from AppHost to SnippetStore to SnippetRepository. Legacy constructors may default only to an unavailable protector that throws for passwords, never an identity/plaintext protector. Test doubles are confined to tests.
- Flag and encoded text change in the same transaction. Failed encryption/decryption becomes a dedicated sanitized StoreFailure/StoreException, not raw crypto/SQL exception text. Keep unsaved drafts after save failures. A load failure prevents startup without rewriting/skipping rows; users must restore the original Windows profile/key or a compatible backup. No automatic downgrade, destructive reset or empty-password substitution.
- Enable SQLite secure_delete on application connections as defense in depth for future overwritten/deleted cell bytes. It is not WAL/history/SSD secure erasure. Do not delete backups or checkpoint/vacuum user files as an unannounced security promise.
- Unchecking reveals only the draft immediately. The committed database remains encrypted until save; saving with Password off intentionally stores ordinary plaintext. Discard restores saved mode. Explain this beside the checkbox, along with Windows-account binding and export omission.

### 4.2 Required manual label

- In password mode, label resolution returns the user label as custom and never calls Suggest. Empty/whitespace labels fail with LabelRequired at the store boundary, including direct API calls. A password draft declaring automatic label state is rejected rather than accepted with a generated label.
- Entering password mode clears a previously automatic label, resets `_labelBeforeAuto`, forces custom mode and removes automatic label hints/placeholders. Keep an already custom label, since the app cannot determine what a user deliberately entered.
- Clearing a password label leaves it blank/custom/error; it must not reactivate automatic labeling. Disable the Custom label toggle while Password is checked. Returning to ordinary mode keeps custom label mode until the user explicitly requests automatic labeling.
- Include IsPassword in baseline, dirty detection, save, load, reset and ApplySaved. Load mode before body/label updates under the existing loading/rebind guard so transient events cannot generate a plaintext suggestion.

### 4.3 Masking and secure input

- Use one centralized display policy (Presentation helper with a fixed eight-bullet mask). Main Preview/ContentText and popup ContentText are masked *before* creating row projections. Row projections carry IsPassword; equality/hash include it, so equal mask text cannot hide a mode transition.
- Keep the existing shared hover surface, keyboard companion and cancellation paths. They receive only safe projected text. Preserve UI-thread cache rebuilds and pre-show dirty flush. Closing/rebinding must invalidate a previously opened ordinary preview before a saved mode transition is rendered.
- Use a native WinUI PasswordBox for single-line password editing, `PasswordRevealMode=Hidden`, no separate eye button, predictable accessibility name and disabled text prediction/spellcheck. Verify that no last-character reveal appears on the supported Windows runtime. Never simulate security by placing a painted mask over a plaintext TextBox.
- Only the active input control receives body text. Clear/disconnect the ordinary TextBox while Password is checked and the PasswordBox while off. Use guarded event handlers, not uncontrolled two-way binding; update FocusText to select the active input and prevent rebind events from modifying drafts. Clear detached control values during editor disposal/rebinding.
- **WinUI constraint / proposed approval detail:** PasswordBox is single-line, unlike InkSpoke's multiline masked TextBox. Preserve an existing multiline body exactly, show a fixed read-only mask while checked, and explain “Uncheck Password to edit multiline text.” Do not feed multiline values through PasswordBox or truncate/normalize them on metadata-only saves. Users can uncheck, edit multiline text, then recheck before saving. New single-line passwords remain editable while masked. This avoids building a custom secure multiline control; approving this plan approves this explicit UI limitation.
- Search indexing excludes password body, but preserves label/folder/tags. No password length or line count is encoded in preview masks. Metadata and deliberately entered labels stay visible.

### 4.4 Export, import and clipboard boundaries

- NativeExport filters password rows at entry, before caps, preflight, DTOs and JSON. Export result adds an excluded-password count; SnippetCount counts included rows. Reuse normal folders/export schema v1 (folders/metadata are not secret). All-password libraries export folders and an empty snippet array with a clear excluded count.
- Presentation also creates the filtered export snapshot on the UI thread before invoking the worker, so the worker does not receive password domain objects. Capture the excluded count from the same immutable snapshot, carry it to the result/report, and do not double-count when NativeExport performs its independent safety filter. Standalone Core callers remain safe.
- Keep external secret-import exclusions. Imported ordinary snippets retain existing dedupe behavior against the in-memory library; do not reclassify a password or convert an existing record during import. No new secret import/export format.
- Existing explicit Copy and insertion-failure Copy actions intentionally place original text on the clipboard. They remain available and are documented as exposure; they must not copy a mask. Preview and editor rebind/toggle never call clipboard services. No changes to insertion contracts, clipboard history policy or target checks.

### 4.5 Alternatives considered

| Option | Benefits | Costs / decision |
|---|---|---|
| Checkbox and UI mask only | Smallest change | Rejected: user requires database protection. |
| Per-content current-user DPAPI at repository boundary | No master key management; leaves ordinary data/queries compatible | **Chosen:** Windows-account binding and decrypted process memory are explicit limits. |
| SQLCipher / whole-database encryption | Also protects metadata | Rejected: adds SQLite native/key distribution changes; beyond confirmed content-only protection. |
| AES-GCM with separate DPAPI-wrapped key | Flexible portable envelope hierarchy | Rejected for this scope: extra key lifecycle/recovery without a portability requirement. |
| Custom multiline masked TextBox | Closer to Avalonia input | Rejected: selection, IME, undo and accessibility introduce a new secure-input subsystem. Native PasswordBox plus explicit multiline fallback is safer. |
| Skip unreadable password rows | Allows ordinary snippets to open | Rejected: partial-cache/deletion/recovery semantics expand scope; preserve current fail-startup-on-load-failure policy. |

### 4.6 Sequence diagram and parallel paths

```mermaid
sequenceDiagram
    actor User
    participant Editor as Shared editor / VM
    participant Store as SnippetStore
    participant Repo as SnippetRepository
    participant DPAPI as WindowsSecretProtector
    participant DB as SQLite
    participant Lists as Main/popup projections
    User->>Editor: Check Password, enter manual label, Save
    Editor->>Store: Create/Update(IsPassword=true, original text)
    Store->>Store: Validate custom nonblank label (no suggestion)
    Store->>Repo: Write in transaction
    Repo->>DPAPI: Protect original text
    DPAPI-->>Repo: Versioned ciphertext envelope
    Repo->>DB: Bind ciphertext + flag, commit
    Store->>Lists: Publish original in-memory domain snapshot
    Lists->>Lists: Mask content; omit body from search
    Note over Repo,DB: Protect/write failure rolls back; no cache publication
    User->>Editor: Restart / reopen
    Repo->>DB: Read flag + text
    Repo->>DPAPI: Unprotect password envelope
    DPAPI-->>Repo: Exact original text or sanitized failure
    Note over Editor,Lists: Active PasswordBox or read-only multiline mask; no hidden plaintext TextBox binding
```

Parallel flows checked: main and popup share editor but independently build rows/caches; keyboard preview is root-owned rather than row hover; both search constructors share SnippetSearch; create/update/import share repository; usage/folder mutations must not rewrite encryption; Core export can be called without Presentation; all explicit clipboard paths resolve store/draft original text independently. Insertion uses store text, never masked projections.

### 4.7 Surface list

| Surface | Change | Task |
|---|---|---|
| SQLite/store/domain | Flag, migration, encryption boundary, sanitized failures | T1–T2 |
| App startup/composition | Current-user protector; safe load failure | T2 |
| Shared main/popup editor | Checkbox, label rule, secure input, multiline fallback, dirty/reset | T3–T4 |
| Main/popup rows, hover and keyboard preview | Safe projections and mode invalidation | T5 |
| Main/popup search | No password-body index | T5 |
| JSON export and completion dialog | Exclusion before worker/serialization, counts | T6 |
| Paste and explicit copy | Regression coverage, no mask substitution | T7 |
| Backlog/user documentation/QA | Security boundaries and evidence | T8 |

## 5. Impact and risk

| Question | Answer |
|---|---|
| State/crash | Transactions keep mode/content coherent; publish only after commit. Draft checkbox changes do not change storage until save. |
| Consistency/races | Single UI-thread store retained. Export uses one immutable snapshot; delayed preview generations cannot re-open old plaintext. |
| UX | Manual label required; checkbox itself reveals by turning mode off. Multiline masked editing has the explicit native-control limitation above. Exports warn that passwords are omitted. |
| Compatibility | Migration defaults existing rows to ordinary; old binaries refuse schema 3. Native JSON version and ordinary imports/paste behavior stay unchanged. |
| Error recovery | Crypto failure never falls back to plaintext. Failed saves preserve draft and committed state; unreadable encrypted rows block startup with a recovery explanation. |
| Logging/debugging | Allowlisted operation/failure tokens and numeric IDs/counts only. No raw crypto exception/record formatting, text, ciphertext or native descriptions. |
| Edge cases | Test blank labels, tag-only labels, repeated toggles, multiline/Unicode/long inputs, corrupt envelopes, missing protector, all-password export, stale menus/previews and save/load failures. |

**Risks:** DPAPI cannot protect secrets from software running as the same user, process dumps or a compromised account. Existing custom labels can themselves contain sensitive text and remain the user's responsibility. Backups made before conversion can contain ordinary plaintext. Windows-account loss is not recoverable by this app. Decryption currently occurs at load and retains domain strings in memory, matching the existing editor/insertion architecture; this is not a password-manager vault.

**Rollback:** Do not downgrade or strip encryption flags in place. Prefer a fixed forward release. An older binary can use the pre-migration backup only with explicit user acceptance of lost later changes and historical plaintext. Never automatically restore that backup or delete unreadable rows.

## 6. Tasks

Implementation must stop and report contradictions or platform-test failures rather than weakening a security assertion to finish a task. The feature is one bounded implementation plan; partial task completion is not shippable.

### T1 — Domain, label invariant and schema (AC1, AC2)
- **Files:** Core `Domain/{Snippet,SnippetDraft,LabelSuggester}.cs`, `Data/Migrations.cs`, `Store/SnippetStore.cs`; Core Domain/Data/Store tests; Presentation fake store.
- **Change:** Add default-false flag, migration 3, mandatory manual-label branch, propagation on create/update; retain import default false and transaction/cache semantics. Add mode to fake-store behavior.
- **Verify:** Existing migration-2 fixture upgrades without content changes; schema version/flag checks; direct store calls reject missing/automatic password labels.
- **Tests:** New `PasswordSnippetDomainTests.ManualLabelCannotFallBackToBodyOrTags`, `PasswordSnippetPersistenceTests.ExistingRowsRemainOrdinary`; update existing migration version assertions rather than deleting coverage.

### T2 — DPAPI adapter and encrypted repository (AC2, AC7)
- **Files:** new Core `Security/{ISecretProtector,SecretProtectionException}.cs`; new App `Services/WindowsSecretProtector.cs`; Core `Data/{SnippetRepository,SqliteConnectionFactory}.cs`, `Store/{SnippetStore,StoreFailure}.cs`; App `AppHost.cs`, `NativeMethods.txt`; new security/persistence tests and headless Windows verification script.
- **Change:** Encrypt before SQL binding, decode only flagged/versioned envelopes, lossless encoding/cleanup, fail-closed default dependency, safe StoreException mapping, current-user native implementation and composition. Enable secure_delete without promising historical erasure. Keep low-level repository entry points protected, not just VM calls.
- **Verify:** Real temporary SQLite DB via store and repository, inject a distinguishing test protector; directly inspect persisted values/flag, then reload original; force protect/unprotect/SQL failures and assert unchanged rows/cache/events. Headless Windows probe compiles the production adapter/generated imports, performs real DPAPI round-trip/tamper rejection and uses isolated temporary data only. Include elevated same-account round-trip as authorized manual verification.
- **Tests:** `PasswordSnippetPersistenceTests.CiphertextIsBoundInsteadOfPlaintext`, `.ProtectorFailureDoesNotCommitOrPublish`, `.BadEnvelopeNeverFallsBackToText`, `.MissingProtectorFailsClosed`, `.FailedReloadDoesNotPublishPartialState`; Windows probe `DpapiPasswordRoundTrip` / `TamperedBlobFailsClosed`. Cross-platform fake tests are not evidence of DPAPI working.

### T3 — Editor state and manual-label lifecycle (AC1, AC3)
- **Files:** Presentation `ViewModels/SnippetEditorViewModel.cs`, `Text/UiText.cs`; `SnippetEditorViewModelTests.cs`, `SnippetEditorViewModelIntegrationTests.cs`.
- **Change:** Mode property/notifications; clear automatic label on check; forbid auto fallback/hints while checked; reset stale auto-label history; propagate draft/baseline/load/save/reset; preserve exact text and custom label when toggling. Add secure-input eligibility/multiline hint state as needed without putting WinUI types in Presentation.
- **Verify:** Exercise new/edit/save/discard/failure and row-switch lifecycles through real store and fake dialogs/protector; assert no intermediate label/placeholder notification exposes password-derived content while checked.
- **Tests:** `PasswordEditorTests.ToggleClearsOnlyAutomaticLabel`, `.ClearingLabelStaysManualAndInvalid`, `.ModeOnlyEditIsDirtyAndDiscardRestoresIt`, `.LoadCannotEmitPasswordSuggestion`, `.MultilineMetadataSavePreservesExactBody`.

### T4 — Shared WinUI masked input (AC1, AC3, AC7)
- **Files:** App `Controls/SnippetEditor.xaml(.cs)`; UiText as needed; new `PasswordEditorSourceTests.cs` for structural guards; authorized desktop evidence.
- **Change:** Checkbox, PasswordBox Hidden, active-control-only text assignment, guarded handlers and focus routing, custom-label switch disablement, account/export hints, explicit read-only multiline mask/fallback. Do not leave plaintext in an inactive TextBox, tooltip, AutomationProperties or error message.
- **Verify:** Windows build and Native AOT publish; inspect both main and compact editor runtime behavior, typing/selection/IME/paste, UIA password semantics, no eye/last-character reveal, hidden-control value clearing, rebinding and exact multiline preservation. Source guards support but do not substitute runtime checks. If native runtime reveals characters despite Hidden or cannot meet the safety contract, stop and report before adding a custom workaround.
- **Tests:** `PasswordEditorSourceTests.NoRevealButtonOrPlaintextPasswordBinding`; runtime runbook rows 1–4. Verify FocusText targets the active input and does not mutate state on rebind.

### T5 — Safe projections, search and live preview invalidation (AC3, AC4)
- **Files:** new Presentation display-policy helper; `ViewModels/{SnippetRowViewModel,PopupItemViewModel,PopupViewModel}.cs`; Core `Query/SnippetSearch.cs`; App preview lifecycle only if existing cancellation does not cover the transition; Presentation/Core query/cache tests.
- **Change:** Mask before projection construction; include mode in equality/hash/cache; exclude password body from both searches; keep shared hover and root-owned keyboard preview on safe projections. Ensure a formerly ordinary open preview closes/updates before masked state is visible.
- **Verify:** Cover flat/grouped lists, usage-only cache stability, equal-looking masks, normal-to-password transition with an already open/pending preview and keyboard-selected unrealized row. Search using synthetic secret-only and metadata terms.
- **Tests:** `PasswordProjectionTests.NoBodyInEitherRowProjection`, `.ModeTransitionReplacesRows`, `PasswordSearchTests.SecretBodyNotIndexedButMetadataIs`, `PopupCacheStabilityTests` password transition cases; authorized pointer/keyboard lifecycle checks.

### T6 — Export exclusion and count reporting (AC5, AC7)
- **Files:** Core `Porting/{NativeExport,PortingContracts}.cs`; Presentation `ViewModels/{SnippetPortingViewModel,PortingWorkers}.cs`, `Text/UiText.cs`; Core Porting tests and Presentation porting integration/thread/budget/message tests.
- **Change:** Filter on UI thread before worker and independently inside NativeExport; transport a single correct excluded count from the captured snapshot; preflight budgets apply only to included rows. Completion dialog names exclusion, including zero included rows. Keep existing InkSpoke secret exclusions and import behavior.
- **Verify:** Mixed/all-password/empty libraries, huge excluded body cannot cause export refusal, direct Core calls, worker capture contains no secret domain records, valid schema-v1 round-trip for ordinary rows, balanced counts and cancellation/write failures.
- **Tests:** `PasswordExportTests.ExcludesBeforeSizingAndSerialization`, `.AllPasswordsProducesValidEmptySnippets`, `SnippetPortingIntegrationTests.PasswordExclusionsReportedOnce`, `SnippetPortingThreadTests.WorkerReceivesOnlyOrdinaryRows`; existing secret-import tests remain green.

### T7 — End-to-end insertion/copy and security regressions (AC2–AC7)
- **Files:** Presentation `InsertionServiceIntegrationTests.cs`, `SnippetEditorViewModelIntegrationTests.cs`, popup/page copy/insertion tests; Core persistence/logging tests.
- **Change:** Real migrated SQLite store + real editor/query/projection/export/insertion services, with only OS protector/clipboard/target/dialog boundaries faked on cross-platform tests. Follow existing manual composition (no new DI framework). Verify reloaded password text—not mask/ciphertext—reaches insertion/explicit copy; no clipboard activity during toggles/previews.
- **Verify:** Check success/uncertain/failure outcomes, usage-only-on-Inserted and original clipboard restoration. Capture logs with a synthetic secret and assert it and ciphertext are absent while operation/failure tokens are present. Test protect failure and tamper through production entry points, not helper-only tests.
- **Tests:** `PasswordSnippetFlowTests.SaveReloadPreviewSearchExportPaste`, `.ExplicitCopiesUseOriginalText`, `.PreviewAndToggleNeverUseClipboard`, `.FailureLogsContainOnlySafeTokens`; extend existing insertion integration matrix.

### T8 — Wiring audit, documentation and acceptance (AC1–AC7)
- **Files:** AppHost/bindings/callers as audit subjects; this plan; `docs/backlog/001-password-type-snippets.md`; relevant numbered QA/user guide; plan 010/backlog links documenting password-specific export exception without rewriting historical decisions.
- **Change:** Trace every new flag/protector dependency/label branch/projection/export count/failure mapping from producer to every consumer. Review all `Snippet.Text`, `ContentText`, `Preview`, `Suggestion`, SQL write and clipboard usages; record paths intentionally retaining plaintext. Perform Technical Architect and spec-vs-implementation audits; offer independent review and run only with operator authorization.
- **Verify:** Sequential Core then Presentation tests, Windows solution build, Release Native AOT publish and existing CI AOT artifact gate. Headless DPAPI evidence and authorized UI acceptance must be separate. Follow `docs/guides/002-acceptance-harness-safety.md`; no desktop launch/input without an authorized screen slot. Content-free runtime receipts only; do not instrument password values to prove wiring.
- **Test that detects a missed hop:** T1–T7 behavioral chain map plus runbook. Record each AC with test/evidence path; pending desktop evidence blocks marking Complete. If a check cannot run, mark it pending, not passed.

## 7. Test strategy

Run test projects sequentially to avoid shared Core output collisions:

```bash
dotnet test tests/SmartSnippets.Core.Tests
dotnet test tests/SmartSnippets.Presentation.Tests
dotnet build
dotnet publish src/SmartSnippets.App -c Release -r win-x64 -o publish
```

Use new tests first against the missing behavior where feasible, then implementation; record actual tests run, no unmatched/skipped test as evidence. Run native verification only on Windows. Use fake encryption solely to pin repository/composition behavior; the headless Windows probe must exercise the production adapter. A different-user test is optional evidence requiring an operator-provided test account, not a reason to create or modify accounts automatically.

### Authorized manual runbook (synthetic secrets and isolated profile only)

| # | Step | Expected |
|---|---|---|
| 1 | Main editor: check Password; type synthetic single-line secret; leave label blank; save | Masked input, no eye/character reveal, blank manual-label error, no text/tag-derived label. |
| 2 | Enter label and save; restart; open main and compact popup editor | Same mode/manual label, masked content, no stale plaintext in UIA/hidden TextBox. |
| 3 | Convert ordinary auto-labeled text; test check/uncheck/discard/save and clearing label | Automatic label clears on check; custom labels retained; correct dirty/reset; unchecked save intentionally removes encryption. |
| 4 | Convert multiline content; edit metadata while checked; uncheck/edit/recheck/save | Fixed masked read-only field with explanatory hint while checked; exact multiline body survives; no truncation through PasswordBox. |
| 5 | Hover label and use popup Up/Down/Right; repeat after mode changes and recycling | Fixed mask in both preview paths; no stale ordinary text, insertion or focus theft. |
| 6 | Search synthetic secret-only substring and then label/folder/tag | Password body does not match; metadata still matches. Ordinary content search unchanged. |
| 7 | Inspect synthetic password DB row; reopen; run isolated DPAPI/tamper probe | Ciphertext envelope in flagged row; same-account decryption succeeds; tamper fails safely; no plaintext/ciphertext in logs. Historical backup is explicitly not protected. |
| 8 | Export mixed library and all-password library | JSON omits password snippets; correct exported/excluded counts; valid empty snippet array for all-password case. |
| 9 | Insert into owned synthetic target, exercise failure and explicit Copy | Original text (normal CRLF conversion) arrives, correct usage/restoration; explicit copy intentionally exposes original, never mask. |
| 10 | Run same-account elevated app under separately authorized harness; test corrupted password DB copy | Same-account elevated round-trip; corrupted copy fails startup with safe recovery message, does not rewrite/delete rows. |

Unexecuted required rows remain merge blockers. Do not use the live profile or insert into an unowned foreground target.

## 8. Rollout / phasing

Ship T1–T8 together through a feature branch/PR, not direct main commits. Keep one implementation plan because the feature is bounded and its layers share one inseparable safety boundary; do not release encryption without masking/export protection or masking without encryption. Migration creates a normal pre-upgrade backup, which is not a protected password export. No feature flag or automatic classification of old snippets.

## 9. Open questions / approval details

No unresolved requirement questions. Plan approval must explicitly accept the native WinUI multiline fallback described in §4.3. Native DPAPI and PasswordBox runtime evidence remain implementation verification gates, not assumed results. Independent plan review is offered; it has not been authorized or run.

## 10. Approval log

| Date | Event | Notes |
|---|---|---|
| 2026-10-30 | Requirement brief confirmed (G1) | User confirmed account-protected database content, checkbox-as-reveal, export omission and mandatory manual label. |
| 2026-10-30 | Draft/self-review | ACs mapped to tasks; parallel editor/preview/export/copy paths enumerated; native-control limitation and historical-plaintext limits explicit. No implementation changes or runtime verification performed. |
