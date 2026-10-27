# 003 — Core domain and persistence

**Date:** 2026-10-27
**Status:** Approved (2026-10-27)
**Size:** L (subplan of [001-master-smart-snippets-v1.md](001-master-smart-snippets-v1.md))
**Area:** `src/SmartSnippets.Core/`, `tests/SmartSnippets.Core.Tests/`
**Depends on:** 002 (Complete; see [validation report](../research/001-tech-validation-report.md))
**Requirement brief confirmed:** 2026-10-27 (G1, master §2)

---

## 1. Goal

`SmartSnippets.Core` will provide everything below the UI. This covers the snippet and folder
model, the label and tag rules, validation, the SQLite schema and its migrations, repositories,
one store that keeps an in-memory cache and raises change events, search, sorting, folder
grouping, and typed settings. With this in place, 004–007 only build views and Windows services
on top of a tested, cross-platform API. There is no UI in this subplan.

## 2. Requirement slice (from master §2)

- **AC1, rules only:**
  - Label auto-suggestion: the first non-empty tag, otherwise the first non-empty line, at most
    60 characters. It stops updating once the user edits the label.
  - Tag normalization: trim, drop empty entries, deduplicate case-insensitively, keep the
    display form.
  - Validation rejects whitespace-only text or label, and never modifies stored text.
  - Duplicate labels and duplicate texts are allowed.
- **AC2, rules only:**
  - Folder names are non-empty and unique case-insensitively.
  - Deleting a folder moves its snippets to Unfiled; the snippets themselves are kept.
  - A rename takes effect everywhere at once through the change event.
- **AC3:**
  - Data, settings and usage survive a restart.
  - The schema is versioned, and each migration runs in a transaction.
  - Foreign keys are enforced.
  - Recording a use does not touch `updated_at`.
- **AC6, engine:** case-insensitive substring search with AND across terms, matching label,
  text, folder name and tags. An empty query returns everything.
- **AC7, engine:** sort by last used, usage count, created or updated, all descending. Never-used
  snippets go last, and ties break on label, then ID. Folder grouping includes Unfiled.
- **AC13, indicative:** searching 10,000 snippets takes ≤ 150 ms in a CI benchmark.
- **PRD §3.10:**
  - Data lives in a per-user location.
  - Related changes share a transaction.
  - Saving fails cleanly, so the editor can keep the user's content.
  - Errors say what went wrong.
  - Snippet text is never logged.
- **Out of scope:**
  - All UI and view-models (004–006).
  - Hotkey and settings persistence wiring into the UI (004).
  - Recording usage after insertion (007 calls the API added here).
  - Removing the probe table and the Diagnostics window (008).

## 3. Current state (as-built, verified 2026-10-27 at `7d32a8e`)

- **`Core/Data/Migrator.cs`** ([Migrator.cs:11-14](../../src/SmartSnippets.Core/Data/Migrator.cs))
  - It has one migration, `1` = `probe` table, and a public overload that accepts a migration
    list.
  - Each migration runs in its own transaction ([Migrator.cs:76-104](../../src/SmartSnippets.Core/Data/Migrator.cs)).
  - **There is no guard against a database newer than the app**: versions above the latest
    known one are silently treated as already applied.
- **Live databases are already at version 1:** your verification run logged `migrations_applied
  from=1 to=1` (report §Trace). So migration 1 **cannot be repurposed**. New schema goes in
  migration 2, and the v1→v2 path is a real upgrade that must be tested.
- **`Core/Data/SqliteConnectionFactory.cs:14-41`:** opens a connection, then sets WAL and
  `foreign_keys=ON` on every connection. Reuse as-is.
- **`Core/Data/ProbeRepository.cs`:** prototype only, used by
  `App/Diagnostics/DiagnosticsWindow.xaml.cs:30-32`. It stays until 008, so 003 must keep table
  `probe`.
- **`App/App.xaml.cs:40`:** runs `new Migrator(factory).MigrateToLatest()` at startup.
  - Adding migration 2 to the default list means the AOT app creates the new schema on the next
    launch.
  - This is the only App touchpoint 003 has. Grep confirms the only two `Migrator`
    constructions are `App.xaml.cs:40` and `DiagnosticsWindow.xaml.cs:31`.
- **`Core/Logging/AppLog.cs`:** a static logger whose XML doc says never to pass snippet text.
- **`Core/AppPaths.cs`:** provides `Root`, `DatabasePath` and `LogDirectory`.
- **`tests/.../Data/TempDatabase.cs`:** a per-test temp DB that calls `ClearAllPools` on
  dispose. Reuse it for every data test.
- **Platform fact:** SQLite's `COLLATE NOCASE` folds **ASCII only**, so `Ä` ≠ `ä`. A Unicode
  case-insensitive unique folder name therefore cannot rely on `NOCASE`.
- **Platform fact:** `Microsoft.Data.Sqlite` runs several `;`-separated statements in one
  `ExecuteNonQuery`, so a migration can be one SQL batch, as migration 1 already is.

## 4. Design

### 4.1 Approach

```
SmartSnippets.Core/
  Domain/       Snippet, Folder, SnippetDraft, FolderId-less "Unfiled" convention,
                TagList (parse/format), LabelSuggester, SnippetValidator, FolderNameRules
  Data/         (existing) SqliteConnectionFactory, Migrator (+ migration 2, newer-db guard, pre-upgrade backup)
                Migrations.cs (ordered SQL list), FolderRepository, SnippetRepository, SettingsRepository,
                DbTime (unix-ms <-> DateTimeOffset)
  Store/        ISnippetStore, SnippetStore (cache + Changed event), StoreResult<T>, StoreError, StoreException
  Query/        SnippetSearch (index + filter), SnippetSort, SnippetGrouping, SortField, SnippetView
  Settings/     AppSettings (typed, defaults), SettingsKeys, enums (ThemeMode, TextSizePreset, PopupLayout)
```

- **One write path:** every mutation goes through `SnippetStore`. It validates the input, writes
  in one transaction through the repositories, and only after the commit updates the cache and
  raises a single `Changed` event. A failed write therefore never changes the cache. The popup,
  the main window and the search index all react to this one event, so there are no parallel
  refresh paths (master §4).
- **Search is in memory over the store's snapshot.** The DB is never queried per keystroke.
  `SnippetSearch` precomputes the searchable fields once per snapshot.
- **Rules are pure static classes** with no I/O, so they can be tested exhaustively. The store
  calls them, and 005's editor view-model calls the same functions for live label suggestion.
- **Time comes from an injected `TimeProvider`**, `TimeProvider.System` in production. Tests
  control every timestamp.

### 4.2 Alternatives considered

| Topic | Option | Verdict |
|---|---|---|
| Tag storage | **One `tags TEXT` column holding the normalized tags joined with `,`** | **Chosen.** Tags can't contain commas (PRD §3.1), so the join is lossless. They're always loaded with the snippet, and search runs in memory. It's the smallest schema. |
| | `snippet_tags` join table | Rejected. It only pays off for SQL tag queries, tag rename or tag listing, and none of those are in scope (YAGNI). |
| Folder-name uniqueness | **A `name_key` column holding `ToUpperInvariant(trimmed name)`, with a UNIQUE index** | **Chosen.** Gives full Unicode case-insensitivity, and the database enforces it even if two writers race. |
| | `name TEXT COLLATE NOCASE UNIQUE` | Rejected. ASCII-only folding would allow both `Ärger` and `ärger`. |
| | Uniqueness checked only in code | Rejected. It has no database guarantee. |
| Timestamps | **INTEGER Unix milliseconds, UTC** | **Chosen.** Compact, sorts correctly, no parsing, and no time-zone ambiguity. |
| | ISO-8601 TEXT | Rejected. Slower comparisons and needless string parsing in the reader. |
| IDs | **`INTEGER PRIMARY KEY` (rowid)** | **Chosen.** Stable, unique and cheap, and it's the PRD's final tie-breaker. Nothing needs to sync between machines (sync is a non-goal). |
| | GUID | Rejected. No sync requirement, and it's bigger and slower. |
| Search matching | **`string.Contains(term, StringComparison.OrdinalIgnoreCase)`** | **Chosen.** Fast, handles Unicode case, and is deterministic. It does **not** fold accents (`naive` ≠ `naïve`), which the PRD doesn't ask for. |
| | Culture-aware `CompareInfo.IndexOf(IgnoreCase)` | Rejected. Several times slower, and results would change with the OS culture. |
| | SQLite FTS5 | Rejected. FTS5 matches tokens and prefixes, not arbitrary substrings, and it would add a query on every keystroke. |
| Store API | **Synchronous methods called from the UI thread, returning `StoreResult<T>` for expected failures and throwing `StoreException` for database failures** | **Chosen.** One small row write takes about a millisecond. Expected failures (validation, duplicate name, not found) are data, so the UI can show them inline. |
| | Async API everywhere | Rejected for v1. It adds re-entrancy cases with no measured need. Revisit only if the benchmark shows UI stalls. |
| Store layout | **Repositories (SQL) plus `SnippetStore` (cache, rules, events)** | **Chosen.** The SQL can be tested against a real temp DB, and the store tests cover the event and rollback behaviour. |
| | One combined class | Rejected. A class mixing SQL, caching and events is hard to test in isolation. |

### 4.3 Data model

**Migration 2** is added to the default list. Migration 1 (`probe`) stays unchanged until 008.

```sql
CREATE TABLE folders (
  id          INTEGER PRIMARY KEY,
  name        TEXT    NOT NULL,           -- trimmed display form
  name_key    TEXT    NOT NULL UNIQUE,    -- upper-invariant of name, uniqueness only
  created_at  INTEGER NOT NULL,
  updated_at  INTEGER NOT NULL
);
CREATE TABLE snippets (
  id              INTEGER PRIMARY KEY,
  text            TEXT    NOT NULL,                 -- stored byte-for-byte as entered
  label           TEXT    NOT NULL,
  label_is_custom INTEGER NOT NULL DEFAULT 0,       -- 1 once the user edits the label
  folder_id       INTEGER NULL REFERENCES folders(id) ON DELETE RESTRICT,
  tags            TEXT    NOT NULL DEFAULT '',      -- normalized, ',' joined, display form
  created_at      INTEGER NOT NULL,
  updated_at      INTEGER NOT NULL,
  last_used_at    INTEGER NULL,
  usage_count     INTEGER NOT NULL DEFAULT 0
);
CREATE INDEX ix_snippets_folder ON snippets(folder_id);
CREATE TABLE settings (key TEXT PRIMARY KEY, value TEXT NOT NULL);
```

- **Folder deletion uses `ON DELETE RESTRICT`, not `SET NULL`.** `FolderRepository.Delete` runs
  in one transaction: `UPDATE snippets SET folder_id = NULL, updated_at = $now WHERE folder_id
  = $id; DELETE FROM folders WHERE id = $id;`
  - The update is explicit because moving a snippet to Unfiled changes an organisational field,
    so its updated date must change (PRD §3.1).
  - RESTRICT turns any code path that forgets this step into an error instead of silently
    orphaning snippets.
- **Folder rename** changes only the `folders` row. Snippets' `updated_at` stays the same,
  because their own fields didn't change.
- **`RecordUsage(id)`** sets `usage_count = usage_count + 1, last_used_at = $now` and **never**
  changes `updated_at`.

**Migrator changes:**
1. **Newer-version guard:** if the stored version is higher than the highest known migration,
   throw `MigrationException("database was created by a newer Smart Snippets version")` and
   leave the file untouched. The App already catches this and shows the message.
2. **Pre-upgrade backup:** when the stored version is above 0 and migrations are pending, first
   run `VACUUM INTO '<db>.v{from}.bak'`, a consistent copy even under WAL. If the backup fails,
   abort the upgrade.
3. Log `migrations_applied from=X to=Y` (unchanged) and `migration_backup path=…`. The path
   contains no user content.

**Domain types** (records, all immutable):

| Type | Fields |
|---|---|
| `Folder` | `long Id, string Name, DateTimeOffset CreatedAt, UpdatedAt` |
| `Snippet` | `long Id, string Text, string Label, bool IsLabelCustom, long? FolderId, IReadOnlyList<string> Tags, DateTimeOffset CreatedAt, UpdatedAt, DateTimeOffset? LastUsedAt, int UsageCount` |
| `SnippetDraft` | `string Text, string Label, bool IsLabelCustom, long? FolderId, string TagsInput`: what an editor submits |
| `SnippetView` | `Snippet Snippet, string? FolderName`: one row of the snapshot that search, sort and grouping consume |

**Settings:** `AppSettings` is loaded and saved through `SettingsRepository` as string keys.
- An unknown or invalid value falls back to its default and logs `settings_invalid key=…`.
- `Hotkey` is stored as modifiers plus key code, so 004 can register it without parsing display
  text.
- Start-at-login is **not** stored. The `Run` key is the truth (plan 002, T7).

| Key | Type | Default |
|---|---|---|
| `theme` | `ThemeMode` System/Light/Dark | System |
| `text_size.preset` | Small/Medium/Large/Custom | Medium |
| `text_size.custom` | double 10–32, clamped | 14 |
| `hotkey.modifiers` / `hotkey.key` | uint / uint | Ctrl+Shift (0x0006) / Space (0x20) |
| `popup.layout` | Flat/Folder | Flat |
| `popup.sort` | LastUsed/UsageCount/Created/Updated | LastUsed |

### 4.4 Rules (exact)

- **`TagList.Parse(string input)`:**
  1. Split on `,`.
  2. Trim each entry and drop the empty ones.
  3. Remove duplicates with `OrdinalIgnoreCase`, keeping the **first** occurrence's spelling,
     in input order.

  `TagList.Format(tags)` joins with `", "`. Parsing the formatted output gives the same list
  back.
- **`LabelSuggester.Suggest(text, tags)`:**
  1. Take the first tag if there is one. Otherwise take the first line of `text` that is not
     whitespace, trimmed.
  2. Cut it to at most **60 text elements** (`StringInfo`), so an emoji or a combining mark is
     never split.
  3. If nothing qualifies, return `""`.
- **Label behaviour in the store:**
  - On create or update, if `IsLabelCustom` is false, the store sets `Label =
    Suggest(Text, Tags)`.
  - If `IsLabelCustom` is true but the label is whitespace, the store treats it as not custom
    and suggests one. Clearing the label therefore returns it to automatic, so the label is
    never empty when the text isn't.
- **`SnippetValidator`:**
  - `Text` must contain a non-whitespace character, else `TextRequired`.
  - The final `Label` must too, else `LabelRequired`. This can only fail when the text is also
    empty.
  - `FolderId` must exist, else `FolderNotFound` (checked by the store against its cache, and
    backed up by the FK).
  - Text is **never** trimmed or rewritten, and line endings are kept exactly.
- **`FolderNameRules`:**
  - The name is trimmed. An empty result gives `FolderNameRequired`.
  - The key is `trimmed.ToUpperInvariant()`. An existing key gives `FolderNameDuplicate`, and
    the same applies to a rename onto another folder's name.
  - Renaming a folder to its own name in different casing is allowed and updates the display
    form.
- **`SnippetSearch`:**
  - The query is split on whitespace and empty terms are ignored. An empty query returns every
    item in input order, so the current sort is kept.
  - An item matches when **every** term is found, ignoring case, in **at least one** of: label,
    text, folder name, or any single tag.
  - Unfiled snippets have no folder name to match. The word "Unfiled" is a display heading, not
    data.
  - The filter is stable: it keeps the input order and never re-sorts.
- **`SnippetSort.Apply(items, field)`:** descending on the field.
  - For `LastUsed`, `null` (never used) comes after every used snippet.
  - Ties break on `Label` (`OrdinalIgnoreCase`, ascending), then `Id` ascending.
  - It's a total order, so the result is deterministic.
- **`SnippetGrouping.ByFolder(sortedItems, folders)`:**
  - One group per folder that has at least one visible item, ordered by folder name
    (`OrdinalIgnoreCase`).
  - **Unfiled** comes last.
  - Items keep their incoming sorted order within each group.

### 4.5 Store contract

```csharp
public interface ISnippetStore
{
    IReadOnlyList<Folder> Folders { get; }              // snapshot, replaced on change
    IReadOnlyList<SnippetView> Snippets { get; }        // snapshot, replaced on change
    event EventHandler<StoreChangedEventArgs>? Changed;  // raised once per committed mutation
    void Load();                                          // full reload from DB (startup)
    StoreResult<Snippet> CreateSnippet(SnippetDraft draft);
    StoreResult<Snippet> UpdateSnippet(long id, SnippetDraft draft);
    StoreResult DeleteSnippet(long id);
    StoreResult<Folder> CreateFolder(string name);
    StoreResult<Folder> RenameFolder(long id, string name);
    StoreResult DeleteFolder(long id);                   // snippets -> Unfiled
    StoreResult RecordUsage(long id);                    // 007 calls after successful dispatch
}
```

- **Change kinds:** `StoreChangedEventArgs` carries `Kind` (SnippetCreated, SnippetUpdated,
  SnippetDeleted, FolderCreated, FolderRenamed, FolderDeleted, UsageRecorded, Reloaded) and the
  affected IDs.
- **Snapshots:** they are immutable lists, so a reader holding the old snapshot during an event
  is never affected.
- **Expected failures** come back as `StoreResult` with a `StoreError` (TextRequired,
  LabelRequired, FolderNameRequired, FolderNameDuplicate, FolderNotFound, SnippetNotFound) and a
  user-readable message.
- **Database failures** (`SqliteException`, `IOException`) are wrapped in
  `StoreException(StoreFailure.DatabaseUnavailable|DatabaseBusy|Corrupt)`. The message is safe
  to show, the inner exception is logged, and snippet text is never included.
- **Threading:** the store is single-threaded and must be called on the UI thread, which is
  documented on the interface. A debug-only assertion catches cross-thread calls.

### 4.6 Sequence diagram: save from an editor (005/006 use this path)

```
EditorViewModel.Save
  → SnippetStore.UpdateSnippet(id, draft)
    → TagList.Parse(draft.TagsInput) → LabelSuggester (if !custom or label blank) → SnippetValidator
      ├─ invalid → StoreResult.Fail(error)            (cache unchanged, no event, editor keeps content)
      └─ valid   → FolderId in cache? no → Fail(FolderNotFound)
                 → SnippetRepository.Update(conn, tx, …, updated_at=now)  → tx.Commit
                     ├─ SqliteException → rollback → StoreException (cache unchanged, no event)
                     └─ ok → replace snapshot (new list) → Changed(SnippetUpdated, id)
                           → subscribers: popup list, main list, search index rebuild
Parallel paths checked: the only other writers are Migrator (startup, before Load) and
ProbeRepository (probe table only, Diagnostics, removed in 008). Neither touches the snippets,
folders or settings tables, so this path stays the only writer to them.
```

### 4.7 Surface list

| Surface | Change | Task |
|---|---|---|
| `Core/Domain/*` | new | T1 |
| `Core/Data/Migrator.cs`, `Data/Migrations.cs` | migration 2, newer-db guard, backup | T2 |
| `Core/Data/*Repository.cs`, `DbTime.cs` | new | T3 |
| `Core/Settings/*` | new | T4 |
| `Core/Store/*` | new | T5 |
| `Core/Query/*` | new | T6 |
| Benchmark test | new | T7 |
| `App/App.xaml.cs` | **no code change.** The existing `MigrateToLatest()` call applies migration 2, and the existing catch shows the newer-db message | T2 (verify only) |
| Docs: this plan, master progress | update | T8 |

## 5. Impact and risk

| Question | Answer |
|---|---|
| State management: what survives a crash mid-operation? | Every mutation is one transaction, and WAL keeps the database consistent through a crash. The cache is updated only after the commit, so it never runs ahead of the database. |
| Data consistency: orphans, races, double-apply? | Foreign keys are on and folder deletion is RESTRICT, so the move to Unfiled has to happen explicitly. A UNIQUE `name_key` stops duplicate folders at the database level. Usage updates use `+1` in SQL, which is atomic. |
| User experience | No UI changes yet. Error messages are written now, ready for later UI. |
| Backward compatibility | Real user databases are at v1. Moving to v2 only adds tables, and a backup is made first. Databases newer than the app are refused without being modified. |
| Error recovery | A failed migration rolls back, and the backup file remains. A failed write leaves the cache and editor content unchanged. Expected errors are data, not exceptions. |
| Logging & debugging | Log events: `migrations_applied`, `migration_backup`, `migration_failed`, `store_write_failed kind=… op=…`, `settings_invalid key=…`. Only IDs and keys are logged, never text, labels or tags. |
| Edge cases | Covered by tests: 0 snippets, 10k snippets, emoji or combining character exactly at the 60-character limit, CRLF/LF/CR text, whitespace-only text or label, duplicate tags differing only in case, an all-comma tag input, a renamed folder in a different case, deleting a folder with 0 or many snippets, `RecordUsage` on a deleted ID, a query of only whitespace, Turkish `İ`/`i` case, a newer-version DB. |

**Risks:**
- **R1** `ToUpperInvariant` and `OrdinalIgnoreCase` don't do full Unicode case folding (for
  example `ß` vs `SS`). This is an accepted limitation, documented in tests as expected
  behaviour. Culture-aware comparison would make results depend on the OS culture.
- **R2** Rebuilding the search index on every change costs O(n). At 10k snippets this is
  measured in T7. If it's over 50 ms, it switches to incremental patching for single-item
  changes.
- **R3** `VACUUM INTO` needs free disk space equal to the database size. On failure the upgrade
  aborts and the message says so, and the user's data stays untouched at the old version.

**Rollback:** the changes are additive. Reverting the commits leaves a database at v2, which an
older build refuses with the guard message once the guard ships. Before the guard ships, an
older build ignores the unknown tables. The `.v1.bak` file allows a manual restore.

## 6. Tasks

The tasks fall into two independent tracks that can run in parallel: **A** is pure logic
(T1, T4, T6) and **B** is data (T2, T3). T5 joins them, and T7–T8 come last.

### T1 — Domain types and rules (AC1, AC2)
- **Files:** `Core/Domain/{Folder,Snippet,SnippetDraft,TagList,LabelSuggester,SnippetValidator,FolderNameRules,ValidationError}.cs`, and `tests/.../Domain/*Tests.cs`.
- **Change:** the records and pure rules exactly as in §4.3–4.4.
- **Verify:** `dotnet test --filter FullyQualifiedName~Domain` passes.
- **Tests that fail if this breaks:**
  - `TagListTests.Parse_TrimsDropsEmptyDedupesKeepingFirstSpelling`
  - `TagListTests.FormatThenParse_RoundTrips`
  - `LabelSuggesterTests.PrefersFirstTag`
  - `LabelSuggesterTests.FallsBackToFirstNonEmptyLine`
  - `LabelSuggesterTests.TruncatesAt60TextElements_DoesNotSplitEmoji`
  - `SnippetValidatorTests.WhitespaceOnlyText_Rejected`
  - `SnippetValidatorTests.TextIsNeverModified`
  - `FolderNameRulesTests.CaseInsensitiveUnicodeDuplicate` (`Ärger`/`ärger`)

### T2 — Migration 2 and migrator hardening (AC3)
- **Files:** `Core/Data/Migrations.cs` (moves the default list out of `Migrator.cs`),
  `Core/Data/Migrator.cs`, `Core/Data/MigrationException.cs`, and
  `tests/.../Data/MigratorTests.cs` (extend the existing file).
- **Change:** the §4.3 schema as migration 2, the newer-version guard, and the backup with
  `VACUUM INTO` before upgrading an existing database.
- **Verify:** tests, then `dotnet build src/SmartSnippets.Core` with 0 warnings. After merge,
  CI's AOT publish stays green, and on your PC `app.log` shows `migrations_applied from=1 to=2`
  and a `.v1.bak` file.
- **Tests that fail if this breaks:**
  - `MigratorTests.EmptyDb_MigratesTo2`
  - `MigratorTests.V1Db_UpgradesTo2_PreservesProbeRows_WritesBackup`
  - `MigratorTests.NewerDb_ThrowsAndLeavesFileUntouched`
  - the existing `Bootstrap_IsIdempotent` and `FailedMigration_RollsBack`
  - `SchemaTests.ForeignKeys_RejectUnknownFolder`
  - `SchemaTests.FolderNameKey_IsUnique`

### T3 — Repositories (AC1–AC3)
- **Files:** `Core/Data/{FolderRepository,SnippetRepository,SettingsRepository,DbTime}.cs` and
  `tests/.../Data/*RepositoryTests.cs`.
- **Change:** parameterized SQL only. Every write method takes the caller's
  connection/transaction so the store can group them. `SnippetRepository.LoadAll` uses one query
  joined with folders.
- **Verify:** tests against `TempDatabase`.
- **Tests that fail if this breaks:**
  - `SnippetRepositoryTests.Insert_RoundTripsUnicodeMultilineExactly`, including `\r\n`, a lone
    `\r`, a tab, trailing spaces and emoji.
  - `FolderRepositoryTests.Delete_MovesSnippetsToUnfiled_BumpsTheirUpdatedAt`
  - `FolderRepositoryTests.Rename_DoesNotTouchSnippets`
  - `SnippetRepositoryTests.RecordUsage_IncrementsAndSetsLastUsed_KeepsUpdatedAt`
  - `SettingsRepositoryTests.Upsert_Overwrites`

### T4 — Typed settings (AC3)
- **Files:** `Core/Settings/{AppSettings,SettingsKeys,ThemeMode,TextSizePreset,PopupLayout,SortField}.cs`
  and `tests/.../Settings/AppSettingsTests.cs`. `SortField` is defined here and shared with
  Query.
- **Change:** `AppSettings.Load(SettingsRepository)` and `Save`, with the defaults and fallback
  behaviour in §4.3. Custom text size is clamped to 10–32.
- **Verify:** tests.
- **Tests that fail if this breaks:**
  - `AppSettingsTests.EmptyDb_ReturnsDefaults`
  - `AppSettingsTests.SaveLoad_RoundTrips`
  - `AppSettingsTests.InvalidValue_FallsBackToDefault`
  - `AppSettingsTests.CustomTextSize_Clamped`

### T5 — SnippetStore (AC1–AC3, change event)
- **Files:** `Core/Store/{ISnippetStore,SnippetStore,StoreResult,StoreError,StoreException,StoreChangedEventArgs}.cs`
  and `tests/.../Store/SnippetStoreTests.cs`.
- **Change:** the §4.5 contract and the §4.6 flow. The store takes a
  `SqliteConnectionFactory` and a `TimeProvider`.
- **Verify:** tests, including fault injection: open a second connection that holds
  `BEGIN EXCLUSIVE` with `busy_timeout = 0` to force `SQLITE_BUSY`.
- **Tests that fail if this breaks:**
  - `SnippetStoreTests.Create_RaisesChangedOnce_SnapshotContainsNew`
  - `SnippetStoreTests.Create_AutoLabel_FromTagThenText`
  - `SnippetStoreTests.CustomLabel_NotOverwrittenByTextOrTagEdits`
  - `SnippetStoreTests.ClearedCustomLabel_ReturnsToAuto`
  - `SnippetStoreTests.ValidationFailure_NoEvent_CacheUnchanged`
  - `SnippetStoreTests.DbBusy_ThrowsStoreException_CacheUnchanged`
  - `SnippetStoreTests.DeleteFolder_SnippetsBecomeUnfiled_EventCarriesIds`
  - `SnippetStoreTests.RenameFolder_ReflectedInSnippetViews`
  - `SnippetStoreTests.Reload_EqualsCache` (after a mix of operations, a fresh `Load()` gives a
    snapshot identical to the cached one)

### T6 — Search, sort, grouping (AC6, AC7)
- **Files:** `Core/Query/{SnippetSearch,SnippetSort,SnippetGrouping,SnippetView}.cs` and
  `tests/.../Query/*Tests.cs`.
- **Change:** §4.4. `SnippetSearch` is built from a snapshot. It precomputes one
  `SearchEntry(fields[])` per item, and `Filter(query)` returns a stable list.
- **Verify:** tests.
- **Tests that fail if this breaks:**
  - `SnippetSearchTests.AndAcrossTerms_EachTermAnyField`
  - `SnippetSearchTests.MatchesFolderNameAndTags`
  - `SnippetSearchTests.CaseInsensitive_Unicode`
  - `SnippetSearchTests.EmptyOrWhitespaceQuery_ReturnsAllInOrder`
  - `SnippetSearchTests.PreservesInputOrder`
  - `SnippetSortTests.LastUsed_NeverUsedLast`
  - `SnippetSortTests.Ties_LabelThenId`
  - `SnippetSortTests.EachField_Descending`
  - `SnippetGroupingTests.FoldersByName_UnfiledLast_EmptyGroupsOmitted_OrderKept`

### T7 — Benchmark (AC13, indicative)
- **Files:** `tests/.../Query/SearchBenchmarkTests.cs` (trait `Category=Benchmark`).
- **Change:**
  - **Data:** generate 10,000 snippets with a fixed random seed: 200–2,000 character texts with
    mixed scripts, 0–5 tags, and 50 folders.
  - **Measure:** (a) building the index, (b) the median and p95 of 50 runs of `Filter` for a
    one-term query and a three-term query, and (c) `SnippetSort.Apply` on all 10k items.
  - **Assert:** median `Filter` ≤ 150 ms, and index build + sort ≤ 150 ms. Print the numbers to
    the test output.
  - The test runs in the normal CI test job, so no new workflow is needed.
- **Verify:** passes locally and in both CI jobs. The numbers are copied into §9 of this plan.
- **Test that fails if this breaks:** `SearchBenchmarkTests.TenThousandSnippets_WithinBudget`.

### T8 — Wiring audit (lite) and docs
- **Audit:**
  - Every new public type has a caller in tests.
  - `ISnippetStore` will get its production callers in 004–006, and this is noted as expected.
  - Migration 2 is reached through the existing `App.xaml.cs:40` call, confirmed in the CI AOT
    publish and on your next run.
  - Grep confirms no `AppLog` call passes `Text`, `Label` or `Tags`.
- **Docs:** set this plan to Complete with the benchmark numbers, and update master §5/§9.
- **Verify:** CI green on both jobs. The AOT publish still has 0 IL warnings, which proves the
  new Core code is AOT-clean.

## 7. Test strategy

- **Unit tests (Linux and Windows CI):** all of T1–T7, about 50 new tests. Data tests use a real
  SQLite temp file, not mocks.
- **Integration:** `SnippetStoreTests` run end to end: store → repositories → real SQLite →
  reload. This is the integration test for the only write path.
- **Deliberately not tested here:** UI behaviour (005/006), and Windows-only paths, of which 003
  has none.
- **Manual runbook** (your PC, after the CI artifact is built):

| # | Step | Expected |
|---|---|---|
| 1 | Run the new AOT build over your existing v1 database | `app.log` shows `migration_backup` and `migrations_applied from=1 to=2`. `smartsnippets.db.v1.bak` exists |
| 2 | Start the app again | `migrations_applied from=2 to=2`, and no new backup is made |
| 3 | `sqlite3 %LOCALAPPDATA%\SmartSnippets\smartsnippets.db ".schema"` | `folders`, `snippets`, `settings`, `probe` and `schema_version` are present |
| 4 | Diagnostics → Run SQLite probe | Still "CRUD OK", so the probe table is unaffected |

## 8. Rollout / phasing

- **Two parallel tracks:** A (T1 → T4 → T6) and B (T2 → T3). Each can go to a Sonnet agent in a
  separate worktree, and the tracks share no files.
- **Then T5**, which depends on both tracks.
- **Then T7 and T8.**
- One push after each merged track keeps CI feedback quick.

## 9. Open questions

None blocking. These are design calls made in this plan for you to confirm at approval:
1. **A cleared label returns to automatic**, instead of being a validation error.
2. **Deleting a folder bumps the moved snippets' `updated_at`**, because their organisational
   field changed. Renaming a folder doesn't.
3. **Search does not fold accents** (`naive` ≠ `naïve`), and "Unfiled" isn't a searchable
   folder name.
4. **In folder layout, Unfiled is listed last**, and folders with no visible snippets are hidden.
5. **The probe table stays until 008.** The 002 plan text said 003 would drop it, but migration 1
   is already in live databases.

## 10. Benchmark results

*(filled in by T7)*

## 11. Approval log

| Date | Event | Notes |
|---|---|---|
| 2026-10-27 | Requirement brief confirmed (G1) | master §2; no new requirement questions |
| 2026-10-27 | Plan approved (G2) | approved together with 003–005 incl. §9 design calls |
