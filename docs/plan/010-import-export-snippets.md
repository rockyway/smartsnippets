# 010 — Import and export snippets

**Date:** 2026-10-28
**Status:** Implemented locally; binding CI blocked (no remote branch / no push); manual acceptance PENDING-USER. Approved 2026-10-28 (G2).
**Size:** L
**Area:** `src/SmartSnippets.Core/Porting/` (new), `src/SmartSnippets.Core/Store/`, `src/SmartSnippets.Core/Data/SnippetRepository.cs`,
`src/SmartSnippets.Presentation/ViewModels/`, `src/SmartSnippets.Presentation/Services/`, `src/SmartSnippets.Presentation/Text/UiText.cs`,
`src/SmartSnippets.App/Pages/SnippetsPage.xaml`, `src/SmartSnippets.App/Services/`, `src/SmartSnippets.App/AppHost.cs`,
`tests/SmartSnippets.Core.Tests/`, `tests/SmartSnippets.Presentation.Tests/`, `docs/requirements/001-prd.md`,
`docs/plan/001-master-smart-snippets-v1.md`, `.github/workflows/ci.yml` (one Release budget step, T10)
**Depends on:** 003 (complete: store, repositories, label/tag/folder rules), 005 (complete: Snippets page, dialogs).
Uses 006's popup and 007's `LineEndings` as they are on `main` at `27f08ba`; changes neither.
**Runs in parallel with:** the 008 acceptance harness (`.claude/worktrees/008b`, branch `feature/008-harness`). It touches
`scripts/` and `docs/` only. The one shared file is `docs/plan/001-master-smart-snippets-v1.md` (T11 here, the progress log
there): low conflict, resolved by rebasing whichever lands second.
**Requirement brief confirmed:** 2026-10-28 (G1)

> **Scope change.** The PRD lists import/export as excluded from the initial release
> (`docs/requirements/001-prd.md:303`), and the master plan repeats it as a non-goal
> (`docs/plan/001-master-smart-snippets-v1.md:29`). The user confirmed this feature on 2026-10-28, so this plan moves it in
> scope. T11 updates both documents; nothing ships until they agree with this plan.

> **API check (2026-10-28, at `27f08ba`).** Every file and line in §3 was opened at that commit. Before starting, check
> that `ISnippetStore`, `SnippetStore.Write`/`Prepare`/`Publish`, `SnippetRepository.Insert`, `IDialogService`,
> `DialogService` and the Snippets page toolbar still look as §3 says. If they differ, update this plan first.

> **Standing instruction for implementers.** If a step contradicts itself or the tree, prescribes a test that cannot
> compile or cannot reach production code, or asks for an assertion that a wrong implementation would pass, **stop and
> report the plan defect**. Do not improvise or weaken the test. If the tree contradicts §3, the tree wins: report it.

> **Review folded (2026-10-28).** An independent review (reviewer sol:medium) returned "changes required" with findings
> 1–13. The user decided findings 1 and 2 on 2026-10-28 (§2 "Amendments"); every other finding is fixed in place. §11
> maps each finding to the sections it changed.

> **Round-2 review folded and plan approved (2026-10-28).** The round-2 review
> (`docs/review/010-plan-review-round2.md`) findings R1–R8 and its improvement list are fixed in place, each as a class
> (§11). The user approved the plan, including D1, D5 and D7. The user resolved Q1: if the commit budget fails at 32 MB,
> a **per-file entry limit** applies. This is defined behaviour (D11, §4.3.9), not a stop. No open question remains.

---

## 1. Goal

From the Snippets page you can bring in snippets you already have in TermFlow, Rephlo or InkSpoke, and back up or move
your whole Smart Snippets library as one file.

- **Import** reads a `.json` file, works out which app it came from, and adds its snippets. Snippets you already have are
  skipped. Folders are matched by name or created. TermFlow and Smart Snippets files keep each snippet's usage count and
  dates, so the popup's "Last used" and "Usage count" sorts carry on where they were.
- **Nothing half-happens.** A file that is too big, isn't JSON, or comes from an unknown app or version is refused with a
  message saying exactly why, before anything changes. A failure while saving imports nothing.
- **When it is done** a dialog tells you how many snippets were imported and how many were skipped as duplicates, skipped
  as unsupported, or rejected as malformed.
- **Export** writes every snippet and folder to one versioned file (refused with a clear message if it would exceed the
  32 MB import limit, so every file it writes can be imported again). Importing that file into an empty library gives the
  same library back, including snippets that share a text but differ in label, folder or tags.

## 2. Requirement (as confirmed at G1, 2026-10-28)

- **Goal:** from the Snippets page, import snippet files from TermFlow, Rephlo or InkSpoke (auto-detected) plus the app's
  own native format, and export the whole library to a lossless, versioned native JSON file.
- **In scope:**
  - **Import formats:**
    - **TermFlow v1** `{version:1, snippets[]}`: `id`, `text`, `label?`, `folder?`, `tags?`, `createdAt`, `updatedAt?`,
      `lastUsedAt?`, `usageCount?` (dates are epoch-millisecond numbers).
    - **Rephlo** `{version:"1.3", commands[]}`: `name` → label, `instruction` → text, `groupName` → folder; skip
      `isArchived`; ignore `description`, `executionMode`, `fixedContentAppend`, `linkedSpaceNames`, `isDefault`,
      `hideFromContextMenu`.
    - **InkSpoke** `{SchemaVersion:2, Groups[], Mappings[]}`: only `ActionType` `SendKeys`. `ActionParamsJson` is a JSON
      string holding `Text` (also seen as lowercase `text`; some entries fail to parse). `SpokenPhrase` → label;
      `GroupId` → `Groups[].Name` → folder; missing `GroupId` → Unfiled. Skip `IsSecret=true`, `IsParameterized=true`,
      non-SendKeys actions and unparsable params, each counted as skipped-unsupported with a reason. Ignore `Favorites`,
      `Vocabulary`, `SendEnterAfter`.
    - **The app's own native format.**
  - **Export:** all snippets and folders to `.json` in a new versioned native envelope (`text`, `label`, `isLabelCustom`,
    `folder`, `tags`, `usageCount`, `createdAt`, `updatedAt`, `lastUsedAt`), with the same version-check-first rule.
  - Imported TermFlow and native snippets keep usage count and dates; Rephlo and InkSpoke snippets import fresh.
  - **Result summary:** imported, skipped duplicates, skipped unsupported, rejected malformed.
- **Out of scope:** updating or merging existing snippets; CSV or other formats; exporting a subset; drag-and-drop or
  command-line import; cloud or network; Rephlo prompt-execution settings; InkSpoke Favorites and Vocabulary.
- **Users and surfaces:** Import and Export buttons in the Snippets page toolbar next to New snippet; open and save
  pickers; a result dialog. Layers: **Core** (parsers, validation, store bulk import), **Presentation** (view-model,
  `UiText`), **App** (pickers, file I/O, wiring). The popup is unchanged but must refresh through the store's `Changed`
  event.
- **Behaviour:**
  - The format is auto-detected from the top-level keys (`snippets` / `commands` / `Mappings`) plus the version field.
  - Exact-text duplicates are skipped, against the library **and** within the file (library snapshot at import start;
    in this plan that means the start of the synchronous commit, after reading and parsing, so saved edits made while a
    file is being read or parsed are honoured).
    (*Native files: full-row key, amended by A1.*)
  - Folders are matched case-insensitively, otherwise created. An invalid or missing folder → Unfiled.
  - A given label is kept as a custom label; a missing label is auto-suggested by the existing `LabelSuggester`.
  - Tags are normalized by the existing `TagList` rules.
  - Ids from files are never trusted: every snippet gets a new database id.
  - All-or-nothing in **one** transaction with **one** `Changed` event; a failure imports nothing.
  - A cancelled picker is a no-op.
  - **Limits:** `.json` only; 5 MB cap (*amended to 32 MB by A2*) enforced by a bounded read (not the file length); UTF-8 with BOM tolerated;
    non-UTF-8 refused.
  - An unknown format or version is refused with specific, distinct messages **before any entry is read**.
  - Snippet text is never logged (log counts, format and outcome only).
  - Rephlo uses `instruction` only (no `fixedContentAppend`).
- **Assumptions:** Native AOT with source-generated JSON; unknown extra fields tolerated; dates clamped and validated
  (`Number.isFinite`-style: non-finite and negative handled); line endings handled consistently with the existing editor
  normalization (check `LineEndings` usage).
- **Acceptance criteria:**
  1. **AC1** — each of the three sample files imports with counts equal to an independent count.
  2. **AC2** — auto-detect works; an unknown format or version is refused before any change.
  3. **AC3** (*amended by A1*) — exact-text duplicates are skipped (library and in-file), and
     `imported + skippedDuplicates + skippedUnsupported + rejected == entries`.
  4. **AC4** — TermFlow and native imports keep usage and dates, and the popup's last-used and most-used sorts reflect
     them.
  5. **AC5** — folders matched or created; invalid → Unfiled.
  6. **AC6** — all-or-nothing: an injected failure leaves the library unchanged and raises no event.
  7. **AC7** (*amended by A1/A2*) — export → import into an empty library reproduces every snippet, folder, tag and stat; re-importing into
     the same library adds nothing.
  8. **AC8** (*amended by A2: 32 MB, plus export refusal*) — files over 5 MB, non-`.json`, invalid JSON and non-UTF-8 are refused with specific messages.
  9. **AC9** — keyboard reachable, accessible names, and the result dialog readable at text size 32.
  10. **AC10** — build with 0 warnings, Native AOT publish with no IL warnings, all Core and Presentation tests pass.
- **Size:** L.

**Amendments by the user after review (2026-10-28).** These supersede the brief text above where they differ:
- **A1 — native dedupe on a full-row key** (review finding 1). Native-format imports treat an entry as a duplicate only
  when its **line-ending-normalized text + label + folder + tags** all equal a library row or an earlier entry in the
  file. TermFlow, Rephlo and InkSpoke keep the text-only rule. A native file therefore keeps every row whose key differs
  (for example two snippets with the text `hello` in different folders), and re-importing the same native file adds
  nothing.
- **A2 — 32 MB cap, compact export** (review finding 2). The import cap is **32 MB** (33,554,432 bytes) for all formats,
  still enforced by a bounded read. Export writes **compact** JSON (no indentation) and refuses, with a clear message and
  without writing a file, when its output would exceed 32 MB.
- **Amended criteria.** AC3: duplicates are skipped by the format's dedupe key (A1). AC7: export → import into an empty
  library reproduces every snippet, folder (by name), tag and stat, except rows whose full-row key repeats an earlier row
  (D1). Re-importing into the same library adds nothing. AC8: files over **32 MB** are refused; an export over 32 MB is
  refused with a specific message.

**Decisions (all approved by the user 2026-10-28 with the plan):**
- **D1 — duplicate keys.** Text is always compared ordinally after `LineEndings.ToCrLf` on both
  sides. Nothing else in the text is normalized (no trim, no case folding).
  - **TermFlow, Rephlo, InkSpoke:** the key is the normalized text only.
  - **Native (A1):** the key is the normalized text, the label (ordinal), the folder key (`FolderNameRules.Key`, empty
    for Unfiled) and the normalized tag list (ordinal, in order). Stats and `isLabelCustom` are not part of the key. The
    key is a typed value with component-wise ordinal equality, never a joined string (§4.3.5, round-2 R2).
  - Why line endings are normalized: the editor stores line breaks as lone CR (WinUI `TextBox`,
    `docs/research/002-verification-plans-003-009.md:53-56`), while the sample files use LF and mixed CRLF/LF. Texts
    that differ only in line-ending style paste identically (insertion converts to CRLF, `InsertionService.cs:40`), so
    they are the same snippet to the user. Stored text is never rewritten: it is saved exactly as it appears in the
    file.
  - **Known consequence for native round trips:** two library rows that differ **only** in line-ending style, or
    **only** in stats, and have the same label, folder and tags collapse into the first on export → import. AC7 is
    worded to match, and `RoundTrip_FullKeyRepeat_CollapsesToFirst` pins this so it is visible.
- **D2 — import JSON is read with `JsonDocument`, export is written with a source-generated `JsonSerializerContext`.** Both
  are reflection-free and AOT-safe. Typed source-generated deserialization was rejected for import because one entry
  with a wrong-typed field would fail the whole file, which breaks the per-entry "rejected malformed" count (§4.2).
- **D3 — native envelope marker.** TermFlow and native files both have `version` and `snippets`. The native file carries
  `"format": "smart-snippets"` and is detected by that marker first (§4.3.2).
- **D4 — dates in the native file** are epoch milliseconds (integers), the same unit as the database (`DbTime.cs:5`), so
  export → import is exact. Limit: the brief's rule clamps a date later than the import time to the import time, so a
  backup restored on a machine whose clock is behind the exporting machine's loses those future dates.
- **D5 — folder timestamps are not exported.** The brief's envelope lists snippet fields only;
  nothing in the UI shows folder dates. Folders, including empty ones, are exported by name and re-created with the
  import time. AC7's "every folder" means name and membership.
- **D6 — export writes the saved library.** Unsaved edits in the open editor are not included and the editor is left
  alone (no Save/Discard prompt).
- **D7 — the two new toolbar buttons are icon buttons** with tooltips and accessible names,
  because the list column is 320 px wide in wide mode (`SnippetsPage.xaml:57`) and already holds the search box and New
  snippet. T10 measures the search box at that fixed width (runbook step 9a). **Defined fallback:** if the search box is
  narrower than 120 px, the New snippet button also becomes icon-only (glyph `&#xE710;`). Its tooltip "New snippet
  (Ctrl+N)" and accessible name "New snippet" are unchanged, which frees about 80 px. The step is re-measured, and the
  result recorded in §10. User-directed icon redesign: Import uses Download U+E896 (arrow into tray), Export uses
  Upload U+E898 (arrow out of tray), from Segoe Fluent Icons / Segoe MDL2 Assets; retain icon-only buttons,
  tooltips, accessible names, and automation IDs. Visual verification across themes and text sizes remains pending.
- **D8 — one usage rule everywhere: the representable range `0..int.MaxValue`, saturating at the top** (review finding
  3; round-2 R1).
  - **Import** (every format) keeps any integral count in `0..int.MaxValue` **exactly**, and saturates larger finite
    values to `int.MaxValue`. So native counts round-trip losslessly through the whole range the database can hold.
  - `RecordUsage` (SQL, snapshot and fake) saturates at `int.MaxValue`, and the reader clamps to `0..int.MaxValue`, so
    no sequence of imports and pastes can overflow.
  - There is no lower import cap.
- **D9 — `Changed` subscribers are isolated** (review finding 4). The store invokes each subscriber separately. It logs a
  subscriber's exception (its position in the invocation list and the exception type; no reflection metadata, no
  message) and carries on. So **a subscriber failure alone can never escape to the caller**, and every other view still
  refreshes. (`Publish` itself could still throw after COMMIT; §4.4's stage table handles that.) This is store-wide
  hardening, approved with the plan.
- **D11 — per-file entry limit** (user decision on Q1, 2026-10-28; review fix 2 amended by user). `SnippetFileRules.MaxImportEntries`
  is 50,000 (local calibration in §10; binding CI remains pending). Native workload entries are **snippets + explicit
  folders**, counting all raw array slots before mapping, including malformed/duplicate folders. Other formats count
  their snippet-entry array only. Over-limit import → `TooManyEntries` with the combined count, before mapping or
  accumulation. Export applies the same combined rule; snippet-only overflow retains `TooManySnippets`, combined
  overflow uses sibling `TooManyEntries`. One transaction and one event are kept.
- **D10 — the store's failure log carries no exception message** (review finding 5). `store_write_failed` logs the
  operation, exception type, SQLite error code and HResult, but not `ex.Message`, which can echo a trigger message or a
  path.

## 3. Current state (as-built at `27f08ba`)

**Store write path (Core):**
- `src/SmartSnippets.Core/Store/ISnippetStore.cs:7` — the store is single-threaded, UI thread only (a Debug build
  asserts it: `SnippetStore.cs:279-281`). `:19-20` `Changed` is "raised once per committed mutation". `:25`
  `CreateSnippet(SnippetDraft)` is the only way to add a snippet.
- `src/SmartSnippets.Core/Domain/SnippetDraft.cs:4` — `SnippetDraft(Text, Label, IsLabelCustom, FolderId, TagsInput)`:
  no timestamps, no usage, no folder by name. **It cannot carry imported stats.**
- `src/SmartSnippets.Core/Store/SnippetStore.cs`:
  - `:54-71` `CreateSnippet`: `Prepare` (`:307-323`) runs `TagList.Parse`, `LabelSuggester.ResolveLabel`,
    `SnippetValidator.Validate` and a folder-exists check; then `_time.GetUtcNow()` (`:64`), one `Write`, one `Publish`,
    one `Raise(SnippetCreated)` (`:69`). Importing N snippets through it would mean N transactions and N events.
  - `:298-305` `Write` opens one connection and one transaction, commits, and any database exception rolls back.
  - `:283-295` `Guarded` catches `SqliteException`/`IOException` and calls `AppLog.Error("store_write_failed", ex,
    $"op={op}")` (`:292`). Despite the comment at `:291`, that **logs the exception's message**: `AppLog.Error`
    appends `"{type}: {message} (HResult=…)"` (`Logging/AppLog.cs:44-50`), and `AppLogTests.cs:35-47` pins this. A
    SQLite message can echo a trigger's `RAISE` text or a constraint name; an `IOException` message can hold a path.
    It then throws `StoreException` (`ToStoreException`, `:262-277`, whose messages are fixed and user-safe). D10
    changes `Guarded`.
  - `:341-365` `Publish` replaces the snapshots (folders sorted by name). `:367-368` `Raise` invokes the multicast
    `Changed` directly, with no subscriber isolation. It runs **after** `Write` has committed (`:301-303`), so a throwing
    subscriber makes the store call throw after the data is on disk, and stops later subscribers from running (D9).
  - `:239-258` `RecordUsage`: `:255` `UsageCount = existing.UsageCount + 1` (unchecked `int`).
  - `:260` `Stored()` rounds a time to database milliseconds.
- `src/SmartSnippets.Core/Data/SnippetRepository.cs`:
  - `:53-77` `Insert` hard-codes `created_at = updated_at = $now`, `last_used_at = NULL`, `usage_count = 0` (SQL at
    `:65-67`). There is no insert that takes stats.
  - `:116-124` `RecordUsage` SQL `usage_count = usage_count + 1` (`:120`): no saturation.
  - `:126-136` `Read`; `usage_count` is read with an `(int)` cast (`:136`). A database value above `int.MaxValue`
    (reachable today by `RecordUsage` at `int.MaxValue`) reads back negative.
- **Every usage consumer** (grep `UsageCount|RecordUsage` in `src/`):
  - writers: `SnippetStore.cs:255`, `SnippetRepository.cs:120`, and the test fake `FakeSnippetStore.cs:201`;
  - reader: `SnippetRepository.cs:136`;
  - display and sort: `SnippetSort.cs:22`, `UiText.cs:21` `RowMeta` and `:32` `UsedTimes` (both treat ≤ 0 as never
    used), `SnippetRowViewModel.cs:19`, `SnippetEditorViewModel.cs:520`;
  - the sole caller of `RecordUsage`: `InsertionService.cs:126`;
  - after this plan, `NativeExport` also reads it.
- `src/SmartSnippets.Core/Data/FolderRepository.cs:39-51` `Insert(connection, transaction, name, nameKey, now)`;
  `:89-97` `NameKeyExists`.
- `src/SmartSnippets.Core/Data/Migrations.cs:9-15` — `folders.name_key` is `UNIQUE` (`:12`); `:16-27` the snippets table
  already has every column import needs. **No schema change.**
- `src/SmartSnippets.Core/Data/DbTime.cs:5,7` — epoch milliseconds both ways. `DateTimeOffset.FromUnixTimeMilliseconds`
  throws outside year 0001–9999, so file dates must be range-checked before conversion.
- `src/SmartSnippets.Core/Store/StoreChangeKind.cs:3-13` — eight kinds, no bulk kind. `StoreChangedEventArgs.cs:9`
  takes `(kind, id, snippetIds)`.

**Rules to reuse (Core/Domain):**
- `TagList.cs:7-26` `Parse` splits on `,` (`:16`), trims, drops empties, de-duplicates case-insensitively (`:14`).
  `:31` storage is comma-joined, so a tag can never contain a comma.
- `LabelSuggester.cs:9-13` `Suggest(text, tags)` (first tag, else first non-blank line, max 60 text elements, `:7`).
  `:18-21` `ResolveLabel` keeps a non-blank custom label as given (no trim).
- `SnippetValidator.cs:5-19` rejects whitespace-only text or label; stored text is never trimmed.
- `FolderNameRules.cs:5` `Normalize` = `Trim`; `:8` `Key` = upper-invariant; `:10-11` `Validate` (empty → error).
- `LineEndings.cs:7-37` `ToCrLf`; grep-verified single caller `src/SmartSnippets.Presentation/Services/InsertionService.cs:40`
  (paste time). Nothing normalizes stored text.

**Consumers of `Changed` (grep `\.Changed +=` in `src/`: 4 subscribers, all kind-agnostic):**
- `SnippetListViewModel.cs:53` → `:159` `Rebuild()` on every event.
- `PopupViewModel.cs:50` → `:175` `RebuildSearch(); Refresh(id)` on every event. Sorts come from `SnippetSort`
  (`Core/Query/SnippetSort.cs:21-22` reads `LastUsedAt` and `UsageCount`).
- `FoldersPageViewModel.cs:41` → `:234` `Refresh()` on every event.
- `SnippetEditorViewModel.cs:140` → `:633-663`: `RefreshChoices` on every event (`:640`, so new folders appear in the
  folder box); reloads only when its own id is affected (`:653`). The only `switch`/comparison on a kind is `:653`
  (`Reloaded`).
- **So a new `StoreChangeKind.Imported` needs no consumer change.** Master plan `001:115-116` requires this single event
  to drive every refresh.

**Other `ISnippetStore` implementers:** `tests/SmartSnippets.Presentation.Tests/Fakes/FakeSnippetStore.cs:11` (raises
through `:245-247`). Adding an interface member breaks its compile until it implements it.

**Presentation:**
- `Services/IDialogService.cs:11-18` — `ConfirmAsync`, `AskSaveDiscardCancelAsync`, `ShowErrorAsync`. No informational
  dialog. Implementers: `App/Services/DialogService.cs:9` and `tests/.../Fakes/ScriptedDialogService.cs:6`
  (grep-verified, 2).
- `ViewModels/SnippetsPageViewModel.cs:32-45` constructor `(store, dialogs, clipboard = null, time = null)`; `:42`
  builds the list; `:190-191` `[RelayCommand] New`. `:153-157` is the house pattern for a `StoreException`: log
  `kind=<Failure>`, show `ex.Message`.
- `Text/UiText.cs:10` holds all user-facing wording; sections end with `// Tray.` at `:136`. No porting strings.
- `Services/ITextSizeService.cs:7-12` exposes `FontSize`; `Core/Settings/AppSettings.cs:9` caps custom size at 32.
- No `System.Text.Json` use anywhere in `src/` or `tests/` (grep). This plan adds the first JSON code.

**App:**
- `Pages/SnippetsPage.xaml:162-185` — the toolbar row: a `*` column with the search box and an `Auto` column with the
  accent **New snippet** button (`:174-184`, accessible name "New snippet"). `:56-58` columns are 200 / 320 / `*`.
  `:47-50` page accelerators Ctrl+N, Ctrl+F.
- `Pages/SnippetsPage.xaml.cs:29-48` — `ViewModel` is set by the creator; `x:Bind` paths go through it.
- `Services/DialogService.cs:15-19` ctor `(getXamlRoot, showMainWindow)`; `:53-62` `ShowErrorAsync` uses `Body()`
  (`:64`, a wrapping `TextBlock` at the default font size); `:66-95` one dialog at a time (semaphore).
- `AppHost.cs:158` builds the main-window `DialogService`; `:168-172` builds `SnippetsPageViewModel(Store, Dialogs, new
  ClipboardService())`; `:135-137` `textSize`; `:212` a second `DialogService` for the popup.
- `MainWindow.xaml.cs:46` `ContentXamlRoot`; `:51` uses `AppWindow` (so `AppWindow.Id` is available for pickers).
- No file pickers exist (grep `Picker` in `src/`: none). `Directory.Packages.props` pins Windows App SDK
  `1.8.260921001`, which ships `Microsoft.Windows.Storage.Pickers` (`FileOpenPicker(WindowId)`,
  `FileSavePicker(WindowId)`). Microsoft documents these as working when the app runs elevated, unlike
  `Windows.Storage.Pickers`. That matters because the app offers "Restart as administrator".
- `NativeMethods.txt` needs no change (no Win32 calls are added).

**Tests:**
- `tests/SmartSnippets.Core.Tests/Data/TempDatabase.cs:6-44` and `Store/SnippetStoreTests.cs:21-48` (`Env` with a fake
  `TimeProvider`, event recording, `NewStore()` to reload). `:370-404` shows the failure pattern: snapshots `Assert.Same`
  before/after, `Assert.Empty(env.Events)`.
- `tests/SmartSnippets.Presentation.Tests/InsertionServiceIntegrationTests.cs:10-16` and
  `PopupViewModelIntegrationTests.cs:11-25` — real `SnippetStore` on a temporary SQLite file.
- `InsertionServiceTests.cs:230-252` `RecordUsage_RunsOnCallerSynchronizationContext`: an owner thread pumping a
  private `QueueContext` (`:254-257`), a **forced suspension** (`Task.Delay(10).ConfigureAwait(false)`, `:242`), recorded-thread capture
  (`:240-241`) and a **recorded-thread assertion** (`:246`). `QueueContext` is a private nested type, so new tests need their own copy.
- `Logging/AppLogTests.cs:35-47` asserts that `AppLog.Error` writes the exception message.
- Neither test project copies data files to output. Core has no `InternalsVisibleTo` (grep), so tests reach Core only
  through its public API.
- `Data/SqliteConnectionFactory.cs:32-37` leaves the production command timeout at the driver default (30 s); only tests
  shorten it. Any store write, including import, can therefore block the UI thread for up to 30 s while another
  process holds the database. This is pre-existing and shared by every write.
- Pre-existing file I/O in `src/` (grep `File\.|new FileStream`): `MainWindow.xaml.cs:30` (`File.Exists` for the icon),
  `Data/Migrator.cs:75` (`File.Exists` for the backup name), `Logging/AppLog.cs:78` (`File.AppendAllText`).

**Sample files (`D:\temp\export-ref`, personal data, never committed).** Shapes measured on 2026-10-28 with a read-only
script that printed key names, types and counts only:

| File | Envelope | Entries | Independent count (into an empty library) |
|---|---|---|---|
| `termflow-snippets.json` (6,304 B, no BOM) | `version: 1` (number), `exportedAt` (number), `snippets[13]` | `id` string ×13, `text` ×13, `label` ×4, `folder` ×11 (1 distinct name), `tags` list ×6, `createdAt` int ×13, `updatedAt` ×5, `lastUsedAt` ×10, `usageCount` ×10 (1–11) | 13 imported, 0 duplicates, 0 unsupported, 0 rejected; 1 folder created, 2 Unfiled |
| `rephlo_commands_export_selected_20260904_164225.json` (11,596 B) | `version: "1.3"` (string), `exportedAt` (ISO string), `commands[5]` | `name`, `instruction` ×5 (two with mixed CRLF and LF), `groupName` ×4 (1 distinct), `isArchived` false ×5, `fixedContentAppend` ×2 (1 non-empty), `defaultSpaceName` ×1 (not in the brief, ignored) | 5 imported, 0 / 0 / 0; 1 folder, 1 Unfiled |
| `inkspoke-commands-2026-09-04.json` (41,068 B) | `SchemaVersion: 2`, `ExportedAt`, `SourcePlatform`, `Groups[6]`, `Mappings[45]`, `Favorites[3]`, `Vocabulary[30]` | `ActionType`: SendKeys 40, OpenUrl 2, OpenApp 1, CloseApp 1, ForceCloseApp 1. `IsParameterized` true ×4. **`IsSecret` lives inside `ActionParamsJson`** (2 true), not on the mapping. One param object uses lowercase `text`. `GroupId` missing ×3 (2 of them SendKeys). `IsEnabled` true ×45 | 34 imported, 0 duplicates, 11 unsupported (5 not SendKeys, 4 parameterized, 2 secret), 0 rejected; 3 folders, 2 Unfiled |

- The "params fail to parse" case (an empty `ActionParamsJson`) is in an earlier InkSpoke export
  (`D:\temp\export-ref\_inkspoke_anomalies.txt`), not in this file. The synthetic fixture covers it (§7).
- Importing the three files in order TermFlow → Rephlo → InkSpoke into one library: InkSpoke then gives 32 imported and
  2 duplicates (its texts repeat 2 earlier ones), and TermFlow's folder "Prompt shortcut" matches InkSpoke's group
  "Prompt Shortcut" case-insensitively, so it is reused, not created (runbook §7 step 4).

**System.Text.Json behaviour measured on .NET 10 (scratch program, 2026-10-28):**
- `JsonDocument.Parse(bytes)` **throws on a UTF-8 BOM** → the BOM must be stripped before parsing.
- `JsonDocument.Parse` **accepts invalid UTF-8 inside a string** and fails only later in `GetString()` → UTF-8 must be
  validated explicitly (`System.Text.Unicode.Utf8.IsValid`) before parsing.
- The number `1e400`: `TryGetInt64` → false, but `TryGetDouble` → **true with +∞**. `1.5` and `1.0`: `TryGetInt64` →
  false. `-1`: `TryGetInt64` → true.
- An escaped lone surrogate (`"\ud800"`) parses, but `GetString()` throws `InvalidOperationException`.
- Serializing a string that holds a lone surrogate writes U+FFFD (both default and relaxed encoders). Valid emoji, CJK,
  tab, CR, LF and CRLF round-trip exactly.
- A duplicate property name: `GetProperty` returns the **last** value.
- A source-generated context over positional records serializes them, including `IReadOnlyList<T>` and `null` for a null
  `long?`.

**TermFlow's own import (recon `D:\temp\export-ref\_recon_import.txt`), lessons reused here:**
- Refuse an unknown version **before** looking at any entry; keep "unknown version" and "no entries list" as different
  messages, with a test that they differ.
- A size cap through a bounded read of cap + 1 bytes, never the file's reported length. TermFlow used 5 MB; this plan
  uses 32 MB (A2).
- A `.json` gate on both import and export.
- Dedup by exact text, seeded from the library and grown as entries are accepted, so in-file repeats are duplicates too.
- File ids are never reused.
- `Number.isFinite`, not "is a number", for dates.
- An explicit test of the invariant `imported + duplicates + rejected == entries`.
- A cancelled picker shows nothing.

## 4. Design

### 4.1 Approach

```
App           SnippetsPage (2 buttons) ── SnippetFileService (pickers, FileStream, atomic write) ── DialogService.ShowInfoAsync
                 │ x:Bind                     │ implements
Presentation  SnippetPortingViewModel ── ISnippetFileService, IDialogService, UiText (porting section)
                 │ calls
Core          CappedReader → ImportParser ─┬─ NativeFormat / TermFlowFormat / RephloFormat / InkSpokeFormat
                (worker thread)            └─→ ParsedImport
              SnippetImport.Commit(parsed, store)  (UI thread) ─→ ISnippetStore.ImportSnippets(ImportBatch)
                                                                 ├─ ImportPlanner (dedupe by mode, folders, labels, tags)
                                                                 ├─ one Write: FolderRepository.Insert + SnippetRepository.InsertImported
                                                                 └─ Publish once → Raise (subscribers isolated, D9)
              NativeExport.Write(store.Folders, store.Snippets) → compact bytes, or ExportTooLarge (> 32 MB)
              AtomicFileWriter.WriteAsync(path, bytes)   (plain .NET; App's SnippetFileService delegates to it)
```

- **Core owns everything that can be tested on Linux:**
  - the bounded read, read-error mapping and encoding checks (`CappedReader`);
  - format detection with version-first refusal (`ImportParser`);
  - one small mapper per format;
  - normalization and dedupe (`ImportPlanner`);
  - the one-transaction write (`SnippetStore.ImportSnippets`);
  - the export writer with its size check (`NativeExport`);
  - temp-file-then-replace (`AtomicFileWriter`);
  - `SnippetImport.Commit`, which turns a parsed file into a store call and a summary.
- **Presentation** holds `SnippetPortingViewModel`: the commands, the busy flag, pick → gate → read → parse off-thread
  → commit on the UI thread → dialog, the messages, and logging. Its file boundary is `ISnippetFileService`.
- **App** implements the boundary: Windows App SDK pickers, `FileStream`, and `AtomicFileWriter` for writes. It also adds
  the two buttons, an informational dialog that uses the text size, and the wiring.
- **The store's bulk path reuses the single-snippet rules.** Same `TagList`, `LabelSuggester`, `SnippetValidator` and
  `FolderNameRules`, so imported snippets look exactly like hand-made ones. One `Write`, one `Publish`, one
  `Raise(Imported)`.
- **Threading (review finding 7):**
  - **Parse runs on a worker** (`Task.Run(() => ImportParser.Parse(...))`). It is pure: bytes in, records out, no store
    access.
  - **Commit runs on the UI thread** (`SnippetImport.Commit`): plan, transaction, publish, `Changed`. The store is
    UI-thread-owned (`ISnippetStore.cs:7`), and taking the dedupe snapshot inside this synchronous call means the
    library cannot change between the dedupe and the write.
  - The view-model's continuations must resume on the UI context (no `ConfigureAwait(false)` in Presentation). T9 tests
    this with a forced suspension and a recorded-thread assertion.
  - **The byte cap does not bound UI time by itself.** The UI-thread portion grows with the number of entries and
    folders, the existing library size and the subscribers' rebuilds. T6 sets an **enforceable** budget at the cap
    (R2). A competing database writer can block any store write for up to the driver's 30 s timeout (§3). That is
    pre-existing and accepted, not new to import, and it fails as `StoreException` (busy) with nothing imported.

### 4.2 Alternatives considered

| Option | Pros | Cons | Verdict |
|---|---|---|---|
| **Bulk record + new `ISnippetStore.ImportSnippets(ImportBatch)`** | One transaction, one event, carries stats and folder names; `SnippetDraft` and its 3 callers untouched | New interface member (2 implementers) | **Chosen** — the brief requires one transaction and one event, and preserved stats |
| Extend `SnippetDraft` with optional stats and call `CreateSnippet` in a loop | Smallest API change | N transactions and N `Changed` events (`SnippetStore.cs:65,69`); not all-or-nothing; the editor's draft type grows fields it must never set | Rejected — violates AC6 and "one Changed event" |
| Parse in **Core** | Linux-testable, beside the rules it reuses; App stays thin | Core gains `System.Text.Json` (in-box, AOT-safe) | **Chosen** |
| Parse in App | Close to the file I/O | WinUI only compiles on Windows; parser tests would not run on Linux CI | Rejected |
| **One static mapper per format, dispatched by one `switch` on the detected `ImportFormat`** | Each mapping table is isolated, small and separately testable; no registry, no reflection | Four small files | **Chosen** |
| One parser per format behind an `IImportFormat` interface plus a registry | Open for extension | Indirection for a closed set of four formats (YAGNI); registry order becomes a hidden detection rule | Rejected |
| A single switch with all mappings inline | Fewest files | One long method mixing four schemas; harder to test one format alone | Rejected |
| **Bounded buffer: read at most 32 MB + 1 byte into memory, then parse** | Exact cap regardless of reported length; encoding checked once on the whole buffer; trivially testable with a `MemoryStream` | Holds up to 32 MB plus the DOM (roughly 2–3× more) in memory during an import; acceptable for a desktop app | **Chosen** (the TermFlow lesson) |
| Streaming `Utf8JsonReader` over the file | Lower peak memory | Version-first check still needs the envelope before the entries, which streaming makes awkward; per-entry recovery is harder; the cap is still needed | Rejected |
| **`JsonDocument` for import** | Reflection-free; per-entry type checks, so one bad entry → "rejected", not a failed file; also parses the nested `ActionParamsJson` | Hand-written field access | **Chosen** (D2) |
| Source-generated typed deserialization for import | Less code | A wrong type in one entry throws for the whole file, so no per-entry rejected count; four DTO sets | Rejected for import; **used for export**, where the shape is ours |
| Dedupe and folder resolution inside the store call (`ImportPlanner` called by `ImportSnippets`) | The snapshot is the store's own, taken synchronously, so no window for the library to change | The store method does more | **Chosen** |
| Dedupe in the view-model before calling the store | Simpler store | Two sources for "the library"; a reader could see a different snapshot from the writer | Rejected |
| **Native: full-row dedupe key; third-party: text-only key** | Native backups keep same-text rows that differ in label, folder or tags; third-party behaviour stays as the brief says | Two key definitions | **Chosen** — user decision A1 |
| One text-only key for every format | Simplest | A native backup silently drops ordinary library rows that share a text (review finding 1) | Rejected |
| **Parse on a worker, commit on the UI thread** | Moves the DOM work off the UI thread; keeps the store's thread rule and a stable dedupe snapshot | One thread hop to test | **Chosen** (finding 7) |
| Whole import on the UI thread | No hop | Freezes the UI for the whole parse | Rejected |
| Whole import off the UI thread (store on a worker) | Responsive | Breaks the UI-thread store contract and every `Changed` subscriber's assumptions | Rejected |
| **Isolate `Changed` subscribers in `Raise`** | A committed mutation always returns to its caller; one bad view cannot stop the others refreshing | Exceptions are logged, not thrown | **Chosen** (D9, finding 4) |
| Keep the throwing `Raise`, and let the view-model guess whether the commit happened | No store change | The caller cannot tell "committed but a view threw" from "nothing written"; the risk is a false "Nothing was imported" | Rejected |
| **Compact export with a 32 MB refusal** | Every exported file is importable; compact JSON maximizes how much library fits | A library over the limit cannot be exported in one file | **Chosen** — user decision A2 |
| Indented export with no limit | Readable | Can write files the app then refuses (review finding 2) | Rejected |

### 4.3 Data / format / API model

#### 4.3.1 Shared contracts (T0, verbatim)

`src/SmartSnippets.Core/Porting/PortingContracts.cs`:

```csharp
namespace SmartSnippets.Core.Porting;

public enum ImportFormat { Native, TermFlow, Rephlo, InkSpoke }

/// <summary>Why a whole file was refused; nothing in the library changed.</summary>
public enum ImportRefusal
{
    NotJsonFile,        // path does not end in .json (case-insensitive)
    Unreadable,         // opening OR reading failed (IOException, UnauthorizedAccessException), at any byte offset
    TooLarge,           // more than SnippetFileRules.MaxImportBytes (32 MB)
    NotUtf8,            // UTF-16/UTF-32 BOM, or invalid UTF-8 anywhere
    InvalidJson,        // JsonException (syntax, depth, trailing data)
    UnknownFormat,      // root is not an object, or no known marker/key
    UnsupportedVersion, // known format, version missing or not the supported one
    MissingEntries,     // known format and version, entries member missing or not an array
    TooManyEntries,     // array entries exceed MaxImportEntries (native: snippets + explicit folders); checked before mapping
}

/// <summary>An entry the file holds but this app deliberately does not import.</summary>
public enum SkipReason { Archived, NotSendKeys, Parameterized, UnparsableParams, Secret }

/// <summary>An entry that should be importable but is malformed.</summary>
public enum RejectReason { NotAnObject, TextMissing, TextBlank, InvalidText, WrongFieldType, FailedValidation }

public sealed record SkippedEntry(int EntryIndex, SkipReason Reason);

public sealed record RejectedEntry(int EntryIndex, RejectReason Reason);

/// <summary>Stats kept from the file (TermFlow, native). Already validated and clamped by the parser.</summary>
public sealed record ImportStats(DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt, DateTimeOffset? LastUsedAt, int UsageCount);

/// <summary>
/// One importable entry. <see cref="Label"/> null or blank → auto-suggested; otherwise kept as a custom label.
/// <see cref="FolderName"/> null → Unfiled. <see cref="Stats"/> null → fresh (import time, never used).
/// <see cref="Text"/> is exactly the file's text (never trimmed, line endings untouched).
/// </summary>
public sealed record ImportCandidate(
    int EntryIndex,
    string Text,
    string? Label,
    string? FolderName,
    IReadOnlyList<string> Tags,
    ImportStats? Stats);

/// <summary>TextOnly: TermFlow, Rephlo, InkSpoke. FullRow: native (user decision A1, §4.3.5).</summary>
public enum DedupeMode { TextOnly, FullRow }

/// <summary><see cref="FolderNames"/> are folders to ensure exist even if no candidate uses them (native files only).</summary>
public sealed record ImportBatch(IReadOnlyList<ImportCandidate> Candidates, IReadOnlyList<string> FolderNames, DedupeMode Dedupe);

public sealed record ParsedImport(
    ImportFormat Format,
    int EntryCount,
    ImportBatch Batch,
    IReadOnlyList<SkippedEntry> Skipped,
    IReadOnlyList<RejectedEntry> Rejected);

/// <summary><see cref="FoundVersion"/>: the raw JSON of the version value (at most 32 chars), or null when absent.
/// <see cref="EntryCount"/>: set for TooManyEntries.</summary>
public sealed record ImportFailure(ImportRefusal Refusal, ImportFormat? Format = null, string? FoundVersion = null, int? EntryCount = null);

/// <summary>Exactly one of <see cref="Parsed"/> and <see cref="Failure"/> is non-null.</summary>
public sealed record ImportParseResult(ParsedImport? Parsed, ImportFailure? Failure)
{
    public static ImportParseResult Ok(ParsedImport parsed) => new(parsed, null);
    public static ImportParseResult Fail(ImportFailure failure) => new(null, failure);
}

/// <summary>What the store wrote. <see cref="Rejected"/> counts candidates that failed SnippetValidator.</summary>
public sealed record ImportCommit(IReadOnlyList<long> SnippetIds, int FoldersCreated, int SkippedDuplicates, int Rejected);

public sealed record ImportSummary(
    ImportFormat Format,
    int Entries,
    int Imported,
    int SkippedDuplicates,
    int SkippedUnsupported,
    int Rejected,
    int FoldersCreated)
{
    public bool IsBalanced => Imported + SkippedDuplicates + SkippedUnsupported + Rejected == Entries;
}

/// <summary>Exactly one of <see cref="Summary"/> and <see cref="Failure"/> is non-null.</summary>
public sealed record ImportRunResult(ImportSummary? Summary, ImportFailure? Failure);

public enum ExportRefusal { TooLarge, TooManySnippets, TooManyEntries }

/// <summary>Exactly one of <see cref="Utf8Json"/> (compact, ≤ MaxImportBytes) and <see cref="Refusal"/> is non-null.
/// For TooLarge, <see cref="Bytes"/> is "more than MaxImportBytes" (the writer stops at the limit, so the exact size of
/// an oversized export is never computed).</summary>
public sealed record NativeExportResult(byte[]? Utf8Json, ExportRefusal? Refusal, long Bytes, int SnippetCount, int FolderCount);
```

`src/SmartSnippets.Core/Store/StoreChangeKind.cs`: append `Imported` after `Reloaded` (the enum is never persisted).

`src/SmartSnippets.Presentation/Services/ISnippetFileService.cs`:

```csharp
namespace SmartSnippets.Presentation.Services;

/// <summary>File boundary for import/export. Implemented in App with Windows App SDK pickers and FileStream.</summary>
public interface ISnippetFileService
{
    /// <summary>Open picker filtered to .json. Null when cancelled.</summary>
    Task<string?> PickImportFileAsync();

    /// <summary>Save picker; <paramref name="suggestedBaseName"/> has no extension (".json" is the default). Null when cancelled.</summary>
    Task<string?> PickExportFileAsync(string suggestedBaseName);

    /// <summary>Opens for async sequential read. Throws IOException or UnauthorizedAccessException.</summary>
    Stream OpenRead(string path);

    /// <summary>Delegates to Core's AtomicFileWriter.WriteAsync: temporary file in the same folder, then replace
    /// <paramref name="path"/>. On failure the original file is untouched (unless the replace itself succeeded) and
    /// deleting the temporary file is attempted (best effort; a failed delete never hides the original exception).
    /// Throws IOException or UnauthorizedAccessException.</summary>
    Task WriteAtomicallyAsync(string path, ReadOnlyMemory<byte> content);
}
```

#### 4.3.2 Core API (exact signatures)

```csharp
namespace SmartSnippets.Core.Porting;

public static class SnippetFileRules
{
    public const int MaxImportBytes = 32 * 1024 * 1024;              // 33,554,432 (A2); also the export limit
    public const int MaxImportEntries = 50_000;                       // D11: local calibration (§10); native snippets + explicit folders share it
    public static bool HasJsonExtension(string path);                 // Path.GetExtension, OrdinalIgnoreCase ".json"
    public static string ExportBaseName(DateTimeOffset localNow);     // "smart-snippets-2026-10-28" (InvariantCulture)
}

public sealed record CappedReadResult(byte[]? Utf8Json, ImportRefusal? Refusal);   // exactly one non-null

public static class CappedReader
{
    /// Reads at most maxBytes + 1 bytes. More than maxBytes → TooLarge (no further reads).
    /// IOException or UnauthorizedAccessException from ANY read (first or later) → Unreadable. Every other exception
    /// (OperationCanceledException, InvalidDataException, a bug) propagates unchanged: the reader maps only the two
    /// expected I/O types (round-2 R4).
    /// Then: EF BB BF prefix stripped; FF FE / FE FF / 00 00 FE FF prefix → NotUtf8; !Utf8.IsValid → NotUtf8.
    public static Task<CappedReadResult> ReadAsync(Stream stream, int maxBytes, CancellationToken ct = default);
}

public static class AtomicFileWriter
{
    /// Writes to "<path>.<guid>.tmp" in the same folder, flushes, DISPOSES the temp stream (required before a move on
    /// Windows), then File.Move(temp, path, overwrite: true). Any failure attempts to delete the temp file (best effort;
    /// a delete failure is swallowed so the original exception is rethrown); the target is untouched unless the move
    /// succeeded.
    public static Task WriteAsync(string path, ReadOnlyMemory<byte> content, CancellationToken ct = default);

    /// Test seam: the same algorithm with injectable open/move steps, so each failure stage is testable on Linux.
    public static Task WriteAsync(string path, ReadOnlyMemory<byte> content,
        Func<string, Stream> openTemp, Action<string, string> moveOverwrite, CancellationToken ct = default);
}

public static class ImportParser
{
    /// Never throws for any input bytes; malformed input → Failure. `now` is the import time (clamp bound and fresh stats).
    public static ImportParseResult Parse(ReadOnlyMemory<byte> utf8Json, DateTimeOffset now);
}

internal static class NativeFormat   { internal static ImportParseResult Read(JsonElement root, DateTimeOffset now); }
internal static class TermFlowFormat { internal static ImportParseResult Read(JsonElement root, DateTimeOffset now); }
internal static class RephloFormat   { internal static ImportParseResult Read(JsonElement root); }
internal static class InkSpokeFormat { internal static ImportParseResult Read(JsonElement root); }

/// Typed, total field access. A JSON null counts as absent everywhere.
internal static class JsonFields
{
    internal enum Field { Absent, Ok, WrongType }
    internal enum Text { Absent, Ok, WrongType, InvalidUtf16 }                        // InvalidUtf16 = GetString threw (lone surrogate)
    internal static Text String(JsonElement obj, string name, out string? value);
    internal static Field StringArray(JsonElement obj, string name, out IReadOnlyList<string> values);
    internal static Field Bool(JsonElement obj, string name, out bool value);
    internal static DateTimeOffset? EpochMs(JsonElement obj, string name, DateTimeOffset now);  // §4.3.5 date rule; null = absent/invalid
    internal static int Count(JsonElement obj, string name);                                    // §4.3.5 usage rule
    internal static string? VersionText(JsonElement obj, string name);                          // GetRawText, ≤ 32 chars, null if absent
}

public sealed record PlannedSnippet(string Text, string Label, bool IsLabelCustom, string? FolderKey, IReadOnlyList<string> Tags, ImportStats? Stats);

public sealed record ImportPlan(
    IReadOnlyList<PlannedSnippet> Snippets,
    IReadOnlyList<string> NewFolderNames,                    // normalized, first spelling seen, in first-seen order
    IReadOnlyDictionary<string, long> ExistingFolderIds,     // FolderNameRules.Key → id, for keys the batch uses
    int SkippedDuplicates,
    int Rejected);

public static class ImportPlanner
{
    public static ImportPlan Plan(ImportBatch batch, IReadOnlyList<Snippet> existing, IReadOnlyList<Folder> folders);
}

public static class SnippetImport
{
    /// UI thread. store.ImportSnippets(parsed.Batch), then the summary:
    /// Entries = parsed.EntryCount; Imported = commit.SnippetIds.Count; SkippedDuplicates = commit.SkippedDuplicates;
    /// SkippedUnsupported = parsed.Skipped.Count; Rejected = parsed.Rejected.Count + commit.Rejected.
    /// StoreException propagates (nothing written). Never parses: parsing is the caller's (worker-thread) step.
    public static ImportSummary Commit(ParsedImport parsed, ISnippetStore store);
}

public static class NativeExport
{
    public const string FormatMarker = "smart-snippets";
    public const int Version = 1;
    /// Pure; safe on a worker thread (the snapshots are immutable lists, never mutated, ISnippetStore.cs:13-17).
    /// 1. snippets.Count > MaxImportEntries → TooManySnippets; otherwise snippets.Count + folders.Count over the
    ///    limit → TooManyEntries. Both refuse before string preflight, DTO creation or serialization.
    /// 2. Allocation-free string preflight before DTO creation/serialization: run JavaScriptEncoder.Default's span
    ///    encoder into fixed stack scratch space and sum the UTF-8 size of every encoded string field. This is an exact
    ///    string-payload count and a lower bound for the document (framing/numbers omitted). Stop as soon as the sum
    ///    exceeds MaxImportBytes → TooLarge, without allowing the serializer to buffer an oversized token.
    /// 3. Compact JSON serialized into a BoundedBufferStream(MaxImportBytes): JsonSerializer.Serialize(stream, doc,
    ///    NativeJsonContext.Default.NativeDocument). The sink catches framing overhead that passes the preflight,
    ///    throwing ExportLimitExceededException → TooLarge. Its backing buffer stays ≤ 32 MB; the serializer may also
    ///    use intermediate buffers for accepted tokens (each string payload was already bounded by the preflight).
    public static NativeExportResult Write(IReadOnlyList<Folder> folders, IReadOnlyList<SnippetView> snippets, DateTimeOffset exportedAt);
}
```

`JsonFields.String` catches the `InvalidOperationException` that `GetString()` throws for a lone surrogate and returns
`InvalidUtf16`; the mapper turns that into `RejectReason.InvalidText` for the text field and `WrongFieldType` for any
other field. `JsonFields` is internal and **Core has no `InternalsVisibleTo`**. So every §4.3.5 rule is tested through
the public `ImportParser.Parse`, using one-entry TermFlow or native documents and asserting the resulting candidate or
rejection. No friend-assembly access is added. The implementer may shape `JsonFields` freely; only the public signatures
in this section are fixed.

**Refusal ownership (review finding 8):** each refusal kind is produced, and tested, at exactly one boundary.

| Refusal | Produced by | Tested in |
|---|---|---|
| `NotJsonFile` | `SnippetPortingViewModel` (path gate, before open) | T9 |
| `Unreadable` (open) | `SnippetPortingViewModel` (catches `OpenRead`) | T9 |
| `Unreadable` (read), `TooLarge`, `NotUtf8` | `CappedReader.ReadAsync` | T1, plus T9 through the view-model |
| `InvalidJson`, `UnknownFormat`, `UnsupportedVersion`, `MissingEntries`, `TooManyEntries` | `ImportParser.Parse` | T2, plus T9 through the view-model |

`SnippetImport.Commit` produces no refusal: it only ever sees a successfully parsed file.

Store and repository additions:

```csharp
// ISnippetStore
/// <summary>
/// Adds the batch in ONE transaction. Duplicates by batch.Dedupe (§4.3.5: TextOnly = ToCrLf text; FullRow = ToCrLf
/// text + label + folder key + tags), against the library as of this call and earlier candidates, are skipped.
/// Folders are matched by FolderNameRules.Key or created, but only for planned snippets and batch.FolderNames. Labels
/// and tags follow LabelSuggester and TagList.
/// Raises Changed(Imported, null, newSnippetIds) once, only when something was written; subscriber exceptions are
/// isolated and logged (D9), so a subscriber failure alone can never make this method throw after COMMIT. On a database failure nothing is
/// written, the snapshots are unchanged, no event is raised, and StoreException is thrown.
/// </summary>
ImportCommit ImportSnippets(ImportBatch batch);

// SnippetRepository — Insert(...) becomes a call to this with (now, now, null, 0), so there is one INSERT statement.
public Snippet InsertImported(
    SqliteConnection connection, SqliteTransaction transaction,
    string text, string label, bool isLabelCustom, long? folderId, IReadOnlyList<string> tags,
    DateTimeOffset createdAt, DateTimeOffset updatedAt, DateTimeOffset? lastUsedAt, int usageCount);
```

Changes to existing store code (whole classes of defect, applied at every site):
- **D8, usage saturation.** Every usage writer and reader saturates or clamps:
  - `SnippetRepository.RecordUsage` SQL becomes `usage_count = MIN(usage_count + 1, 2147483647)`.
  - `SnippetStore.RecordUsage` (`:255`) uses `UsageCount = existing.UsageCount == int.MaxValue ? int.MaxValue :
    existing.UsageCount + 1`.
  - `SnippetRepository.Read` (`:136`) uses `(int)Math.Clamp(reader.GetInt64(9), 0, int.MaxValue)`.
  - `FakeSnippetStore.RecordUsage` (`:201`) gets the same saturation.
  - Display, sort and export consumers (§3) need no change once values are always in `0..int.MaxValue`
    (`SnippetSort.cs:22` uses `CompareTo`, not subtraction; `UiText` only formats).
  - Import uses the same range: `Count` keeps `0..int.MaxValue` exactly and saturates above it (§4.3.5). There is no
    separate import cap.
- **D9, subscriber isolation.** `SnippetStore.Raise` (`:367-368`) iterates `Changed?.GetInvocationList()`. It invokes
  each handler in its own `try`/`catch (Exception ex)` through a typed cast
  (`((EventHandler<StoreChangedEventArgs>)handlers[i])(this, args)`, never `DynamicInvoke`). It logs
  `AppLog.Error("store_subscriber_failed", null, $"kind={kind} index={i} of={handlers.Length} error={ex.GetType().Name}")`:
  no reflection metadata (`Method`/`DeclaringType` are not used), no message. It never rethrows. This covers every mutation kind, because they all call `Raise`. `FakeSnippetStore.Raise` (`:245-247`) gets
  the same isolation, so view-model tests see the production semantics.
- **D10, store failure log.** `SnippetStore.Guarded` (`:292`) becomes `AppLog.Error("store_write_failed", null,
  $"op={op} error={ex.GetType().Name} sqlite={(ex as SqliteException)?.SqliteErrorCode} hresult=0x{ex.HResult:X8}")`.
  The exception object is not passed, so no message is logged. `AppLog` itself is unchanged, and `AppLogTests` still
  holds.

Presentation view-model:

```csharp
namespace SmartSnippets.Presentation.ViewModels;

/// <summary>Worker-thread steps, injectable so tests can control when they complete (round-2 R5).</summary>
public sealed record PortingWorkers(
    Func<ReadOnlyMemory<byte>, DateTimeOffset, Task<ImportParseResult>> ParseAsync,
    Func<IReadOnlyList<Folder>, IReadOnlyList<SnippetView>, DateTimeOffset, Task<NativeExportResult>> ExportAsync)
{
    /// <summary>Production: Task.Run(() => { onWorkerThread?.Invoke(); return ImportParser.Parse(...); }) and the same
    /// shape for NativeExport.Write. The callback exists only so a test can observe the worker thread.</summary>
    public static PortingWorkers Create(Action? onWorkerThread = null);

    public static PortingWorkers Default { get; }   // = Create(null)
}

public sealed partial class SnippetPortingViewModel : ObservableObject
{
    public SnippetPortingViewModel(ISnippetStore store, IDialogService dialogs, ISnippetFileService? files,
        TimeProvider? time = null, PortingWorkers? workers = null);

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ImportCommand))]
    [NotifyCanExecuteChangedFor(nameof(ExportCommand))]
    private bool _isBusy;   // ONE owner (round-2 R6): set by ImportAsync/ExportAsync before their first await, cleared in
                            // their own finally AFTER every dialog they show (summary, refusal, store failure and
                            // unexpected-error dialogs alike) has closed.

    [RelayCommand(CanExecute = nameof(CanRun))] private Task Import() => ImportAsync();   // thin; no handling of its own
    [RelayCommand(CanExecute = nameof(CanRun))] private Task Export() => ExportAsync();
    private bool CanRun() => _files is not null && !IsBusy;

    /// <summary>Null when cancelled or busy (a call while IsBusy returns null at once with no side effect). Otherwise the
    /// summary or the refusal that was shown. Never throws (except OperationCanceledException): StoreException and
    /// unexpected exceptions are classified by stage (§4.4) and shown INSIDE this method, before IsBusy is cleared.</summary>
    public Task<ImportRunResult?> ImportAsync();

    /// <summary>False when busy, cancelled, refused or failed. The same single-owner and never-throws rules, using the
    /// export stage table (§4.4).</summary>
    public Task<bool> ExportAsync();
}

// SnippetsPageViewModel: new trailing optional parameter and property
public SnippetsPageViewModel(ISnippetStore store, IDialogService dialogs, IClipboardService? clipboard = null,
    TimeProvider? time = null, ISnippetFileService? files = null);
public SnippetPortingViewModel Porting { get; }   // never null; commands disabled when files is null

// IDialogService: new member
/// <summary>Informational dialog with one OK button; the body uses the snippet text size.</summary>
Task ShowInfoAsync(string title, string message);
```

#### 4.3.3 Format detection (version first)

`ImportParser.Parse`:
1. `JsonDocument.Parse(bytes, new JsonDocumentOptions { CommentHandling = Disallow, AllowTrailingCommas = false })`.
   A `JsonException` → `InvalidJson`.
2. Root not an object → `UnknownFormat`.
3. Detect, in this order. Only the first matching rule applies:

| # | Rule (property names are case-sensitive) | Format | Version member | Supported | Entries member |
|---|---|---|---|---|---|
| 1 | root has `format` (any type) | Native if `format` is the string `"smart-snippets"`, else **`UnknownFormat`** | `version` | number `1` | `snippets` (+ optional `folders`) |
| 2 | root has `snippets` | TermFlow | `version` | number `1` | `snippets` |
| 3 | root has `commands` | Rephlo | `version` | string `"1.3"` | `commands` |
| 4 | root has `Mappings` | InkSpoke | `SchemaVersion` | number `2` | `Mappings` (+ optional `Groups`) |
| 5 | none of the above | — | — | — | **`UnknownFormat`** |

4. **Version check before any entry is touched:** the version member is absent, the wrong JSON type, or another value →
   `UnsupportedVersion(format, FoundVersion)`. Number versions are compared with `TryGetInt64` (so `1.0` and `"1"` are
   refused for TermFlow). The entries member is not looked at until this passes. A TermFlow file with `version: 2` and
   `snippets: 5` gives `UnsupportedVersion`, not `MissingEntries`.
5. The entries member is absent or not an array → `MissingEntries(format)`. Count its raw slots, plus native
   `folders` raw slots when that member is an array. A combined count greater than `SnippetFileRules.MaxImportEntries`
   → `TooManyEntries(format, EntryCount: combinedCount)`. Use only `GetArrayLength()` before any element is mapped
   or accumulated (D11). Absent/null/non-array native `folders` remains ignored.
6. The per-format mapper reads each element. Successful `ParsedImport.EntryCount` = snippet-entry array length
   (native and TermFlow: `snippets`; Rephlo: `commands`; InkSpoke: `Mappings`). Every snippet element ends in exactly
   one of: candidate, skipped, rejected. This summary count excludes native folders, which are reported separately
   as FoldersCreated, so IsBalanced and the exact four-line summary wording remain unchanged.
7. Unknown extra members anywhere are ignored. A duplicate member name takes the last value (measured `JsonDocument`
   behaviour; not otherwise special-cased).

#### 4.3.4 Field mapping per format

Notation: **→ rejected(R)** / **→ skipped(S)** end the entry. "Absent" includes JSON `null`. The checks run in the
order listed.

**Native v1** (`format: "smart-snippets"`, `version: 1`), written by `NativeExport` (§4.3.6):

| File | Rule | Target |
|---|---|---|
| element not an object | → rejected(`NotAnObject`) | |
| `text` | string, else absent → rejected(`TextMissing`), wrong type → rejected(`WrongFieldType`), lone surrogate → rejected(`InvalidText`), whitespace-only → rejected(`TextBlank`) | `Text`, verbatim |
| `label` | optional string (wrong type → rejected(`WrongFieldType`)) | `Label` when `isLabelCustom` is true or absent; `null` (auto) when `isLabelCustom` is false |
| `isLabelCustom` | optional bool (wrong type → rejected(`WrongFieldType`)) | see `label` |
| `folder` | optional string (wrong type → rejected(`WrongFieldType`)) | `FolderName` (blank → Unfiled, by the planner) |
| `tags` | optional array of strings (wrong type or a non-string element → rejected(`WrongFieldType`)) | `Tags` |
| `createdAt`, `updatedAt`, `lastUsedAt`, `usageCount` | §4.3.5 rules | `Stats` (always non-null for native) |
| root `folders[]` | optional array. Each element is an object with string `name`; anything else is ignored (not an entry, not counted) | `ImportBatch.FolderNames` |
| `exportedAt` | ignored | |
| — | | `ImportBatch.Dedupe = FullRow` (A1). Every other format's mapper sets `TextOnly` |

**TermFlow v1** (`version: 1`, `snippets[]`):

| File | Rule | Target |
|---|---|---|
| element not an object | → rejected(`NotAnObject`) | |
| `id` | ignored, never required or reused | new database id |
| `text` | as native | `Text`, verbatim |
| `label` | optional string (wrong type → rejected(`WrongFieldType`)) | `Label` (non-blank → custom; blank or absent → auto) |
| `folder` | optional string (wrong type → rejected) | `FolderName` |
| `tags` | optional array of strings (wrong type or element → rejected) | `Tags` |
| `createdAt`, `updatedAt`, `lastUsedAt`, `usageCount` | §4.3.5 | `Stats` (always non-null) |
| root `exportedAt` | ignored | |

**Rephlo 1.3** (`version: "1.3"`, `commands[]`):

| File | Rule | Target |
|---|---|---|
| element not an object | → rejected(`NotAnObject`) | |
| `isArchived` | `true` → skipped(`Archived`); absent or false → continue; wrong type → rejected(`WrongFieldType`) | |
| `instruction` | as native `text` | `Text`, verbatim. **`fixedContentAppend` is never appended** |
| `name` | optional string (wrong type → rejected) | `Label` (non-blank → custom; else auto) |
| `groupName` | optional string (wrong type → rejected) | `FolderName` |
| `description`, `executionMode`, `fixedContentAppend`, `linkedSpaceNames`, `isDefault`, `hideFromContextMenu`, `defaultSpaceName`, root `exportedAt` | ignored | |
| — | | `Tags` empty, `Stats` null (fresh) |

**InkSpoke v2** (`SchemaVersion: 2`, `Mappings[]`, `Groups[]`):

| File | Rule | Target |
|---|---|---|
| root `Groups[]` | optional. A map from string `Id` to string `Name`; malformed group elements are ignored. Absent → empty map | folder lookup |
| element not an object | → rejected(`NotAnObject`) | |
| `ActionType` | absent or not a string → rejected(`WrongFieldType`); any value other than `"SendKeys"` (ordinal) → skipped(`NotSendKeys`) | |
| `IsParameterized` | `true` → skipped(`Parameterized`); wrong type → rejected(`WrongFieldType`) | |
| `ActionParamsJson` | must be a string that parses (its own `JsonDocument`) to an object; otherwise (absent, not a string, empty, invalid JSON, array, number) → skipped(`UnparsableParams`) | |
| `IsSecret` | `true` **inside the params object or on the mapping** → skipped(`Secret`) | |
| params `Text`, else params `text` | as native `text` (absent → rejected(`TextMissing`), non-string → rejected(`WrongFieldType`), blank → rejected(`TextBlank`)) | `Text`, verbatim (InkSpoke tokens such as `{last}` stay literal) |
| `SpokenPhrase` | optional string (wrong type → rejected) | `Label` (non-blank → custom; else auto) |
| `GroupId` | absent, not a string, or not in the group map → Unfiled; else `Groups[].Name` | `FolderName` |
| `Id`, `IsEnabled`, params `SendEnterAfter`, params `Nonce`, root `Favorites`, `Vocabulary`, `ExportedAt`, `SourcePlatform`, group `IsEnabled`, `SortOrder` | ignored. A disabled mapping is still imported (the brief lists no rule for it) | |
| — | | `Tags` empty, `Stats` null (fresh) |

#### 4.3.5 Shared value rules

- **Numbers (review finding 11): compare and clamp before any cast.** For a JSON number value `v`:
  1. If `TryGetInt64(out long l)` succeeds, work with `l`.
  2. Otherwise, if `TryGetDouble(out double d)` succeeds, `double.IsFinite(d)` and `d == Math.Floor(d)`, work with `d`
     **as a double**. Never cast `d` to `long`: `(long)d` is undefined for `d ≥ 2^63`, and `2^63` compares equal to
     `(double)long.MaxValue`.
  3. Otherwise (∞, a fraction, a string, a boolean) the value is invalid.

  Every range comparison below is done in the working type, against the bound converted to that type, and the result
  is the bound itself whenever a clamp applies. So the only cast ever made is of a value already known to lie inside the
  target range.
- **Dates** (`EpochMs`): invalid → see per field. A value < 0 → invalid. A value > `nowMs` (`DbTime.ToDb(now)`) →
  `now` (clamped). This covers `long.MaxValue`, `2^63`, `9.223372036854775808e18` and `1e300`. Otherwise the value is
  `DbTime.FromDb((long)value)`, which is safe because `0 ≤ value ≤ nowMs`. Invalid is never a rejection.
  - `createdAt` absent or invalid → `now`.
  - `updatedAt` absent or invalid → `createdAt`; if earlier than `createdAt` → `createdAt`.
  - `lastUsedAt` absent or invalid → `null`.
- **Usage** (`Count`, D8, one rule for every format): invalid or < 0 → 0. A value > `int.MaxValue` → `int.MaxValue`
  (saturated, **not** zero), including `long.MaxValue`, `2^63` and `1e300`. Otherwise `(int)value`, **exactly**. So
  every count the database can hold round-trips unchanged.
  `usageCount > 0` with no `lastUsedAt` is kept as is (the sort puts never-used last, `SnippetSort.cs:36-42`).
- **Labels** (planner): a non-blank `Label` is kept exactly (custom, `IsLabelCustom = true`), like
  `LabelSuggester.ResolveLabel` (`:18-21`). Otherwise `LabelSuggester.Suggest(text, tags)`, not custom. No truncation of
  custom labels: the store has no label length limit.
- **Tags** (planner): `TagList.Parse(string.Join(',', candidate.Tags))`. A file tag containing a comma becomes several
  tags, because storage is comma-separated (`TagList.cs:31`). Case-insensitive duplicates collapse to the first.
- **Folders** (planner): `FolderNameRules.Validate(name)` fails (empty after trim) → Unfiled. Otherwise
  `key = FolderNameRules.Key(name)`, matched against existing folders' keys → existing id. Otherwise a new folder named
  `FolderNameRules.Normalize(name)`, using the first spelling seen. Native `FolderNames` are processed before the
  candidates, so their spelling wins.
  - **No incidental folders:** a folder is created only for a candidate that ends up **planned** (not a duplicate, not
    rejected), or for a name in `batch.FolderNames` (native explicit folders, the documented exception). A duplicate or
    rejected candidate naming an unseen folder creates nothing.
  - For a native file, the folder is resolved **before** dedupe, because the folder key is part of the native dedupe
    key; it is only added to `NewFolderNames` once the candidate is planned.
- **Duplicates** (planner, D1, A1), by `batch.Dedupe`:
  - `TextOnly`: `key = LineEndings.ToCrLf(text)`.
  - `FullRow`: the key is built **after** label resolution and tag normalization (the order in §4.4's diagram).
    Components:
    - `text`: `LineEndings.ToCrLf(text)`;
    - `label`: the **resolved** label (the custom label, or the suggestion when the label is auto);
    - `folderKey`: `FolderNameRules.Key`, or `""` for Unfiled;
    - `tags`: the `TagList`-normalized list.

    A library row's key is built the same way from its stored text, label, folder (looked up by `FolderId` in
    `folders`) and tags.
  - **Key representation (round-2 R2), fixed, not left to the implementer:**
    - `internal readonly record struct FullRowKey(string Text, string Label, string FolderKey, string[] Tags)`, used
      with a dedicated `FullRowKeyComparer : IEqualityComparer<FullRowKey>`. It has the
      `HashSet<FullRowKey>(FullRowKeyComparer.Instance)` constructor shape.
    - `Equals`: `string.Equals(a.Text, b.Text, Ordinal) && string.Equals(a.Label, b.Label, Ordinal) &&
      string.Equals(a.FolderKey, b.FolderKey, Ordinal) && a.Tags.AsSpan().SequenceEqual(b.Tags)` (element-wise ordinal,
      order-sensitive). Lengths compare first, so `[]` ≠ `[""]`; the normalizer never produces an empty tag anyway.
    - `GetHashCode`: `HashCode` combining `StringComparer.Ordinal.GetHashCode` of each component and of each tag in
      order.
    - **Forbidden:** concatenating components into one string with any separator (U+001F, NUL or anything else). No
      character is excluded from text, labels or tags, so any separator can collide. Relying on the record struct's
      default equality is also forbidden, because it compares the `string[]` by reference.
    - `TextOnly` keys are plain `string` in a `HashSet<string>(StringComparer.Ordinal)`.
  - Seeded from every snippet in `existing`, then extended with each **planned** candidate in file order. The first
    occurrence wins. Stats and `isLabelCustom` are never part of a key.
- **Validation** (planner): `SnippetValidator.Validate(text, label)`. Any error → counted in `ImportCommit.Rejected`
  (`FailedValidation`). The parser already rejects blank text, so this is a defensive backstop that keeps the count
  balanced.
- **Text** is never trimmed or re-encoded. A lone surrogate → rejected(`InvalidText`).

#### 4.3.6 Native export envelope (v1)

```json
{
  "format": "smart-snippets",
  "version": 1,
  "exportedAt": 1793214267195,
  "folders": [ { "name": "Support" }, { "name": "Empty folder" } ],
  "snippets": [
    {
      "text": "Hello\r\nWorld",
      "label": "Greeting",
      "isLabelCustom": true,
      "folder": "Support",
      "tags": ["mail", "short"],
      "usageCount": 3,
      "createdAt": 1790000000000,
      "updatedAt": 1790000500000,
      "lastUsedAt": 1793000000000
    }
  ]
}
```

- `folders`: every folder in `store.Folders` order (by name). `snippets`: every snippet in `store.Snippets` order (by
  id). `folder` is `null` for Unfiled. `lastUsedAt` is `null` when never used. All dates are epoch ms (`DbTime.ToDb`).
- The example above is indented for reading; the real file is **compact** (A2: no indentation, no spaces between
  tokens).
- Serialized by `NativeExport.Write` (§4.3.2): compact JSON written through the bounded-buffer stream with
  `JsonSerializer.Serialize(stream, document, NativeJsonContext.Default.NativeDocument)`. If the output would pass
  `SnippetFileRules.MaxImportBytes` → `NativeExportResult(null, ExportRefusal.TooLarge, …)`, and **no file is
  written**. The size is measured on the exact bytes that would be written, after escaping. This guarantees every
  exported file passes the importer's cap. The context:

```csharp
// src/SmartSnippets.Core/Porting/NativeJsonContext.cs
internal sealed record NativeDocument(string Format, int Version, long ExportedAt, IReadOnlyList<NativeFolder> Folders, IReadOnlyList<NativeSnippet> Snippets);
internal sealed record NativeFolder(string Name);
internal sealed record NativeSnippet(string Text, string Label, bool IsLabelCustom, string? Folder, IReadOnlyList<string> Tags,
    int UsageCount, long CreatedAt, long UpdatedAt, long? LastUsedAt);

[JsonSourceGenerationOptions(WriteIndented = false, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(NativeDocument))]
internal sealed partial class NativeJsonContext : JsonSerializerContext;
```

- Output is UTF-8 without BOM. The default encoder escapes non-ASCII as `\uXXXX`: valid JSON and lossless, if less
  readable. Escaping expands text: a non-ASCII BMP character becomes 6 bytes, an emoji 12. A library of about 6 MB of
  accented or CJK text can therefore exceed 32 MB on export. That case is refused (`ExportTooLarge`), never written and
  then refused on import (test `ExportImport_EscapingExpansion_RefusedAtExport`). A lone surrogate in a stored text is
  written as U+FFFD (measured, §3; accepted, R6).
- Export never reads the database: it uses the in-memory snapshots, which equal the database (`SnippetStoreTests`
  `Reload_EqualsCache`).

#### 4.3.7 Messages (`UiText`, new `// Import and export (010).` section, exact wording)

| Key / helper | Text |
|---|---|
| `ImportButtonName` | `Import snippets` (accessible name) |
| `ImportButtonTooltip` | `Import snippets from Smart Snippets, TermFlow, Rephlo or InkSpoke (.json)` |
| `ExportButtonName` | `Export snippets` |
| `ExportButtonTooltip` | `Export all snippets and folders to a .json file` |
| `ImportCompleteTitle` | `Import complete` (when `Imported > 0`) |
| `ImportNothingTitle` | `Nothing imported` (when `Imported == 0`) |
| `ImportSummary(ImportSummary s)` | four lines joined with `\n`: `Imported {n} {snippet/snippets} from {FormatName}.` / `Skipped {n} {duplicate/duplicates}.` / `Skipped {n} unsupported {entry/entries}.` / `Rejected {n} malformed {entry/entries}.` |
| `FormatName(ImportFormat)` | `Smart Snippets`, `TermFlow`, `Rephlo`, `InkSpoke` |
| `ImportFailedTitle` | `Couldn't import` |
| `ImportRefusalMessage(ImportFailure f)` | per refusal, below. Every one ends with ` Nothing was imported.` |
| — `NotJsonFile` | `Choose a .json file. Smart Snippets imports JSON export files only.` |
| — `Unreadable` | `Couldn't read the file. Check that it still exists and isn't open in another app.` |
| — `TooLarge` | `This file is larger than 32 MB, the import limit.` |
| — `NotUtf8` | `This file isn't UTF-8 text. Save it as UTF-8 and try again.` |
| — `InvalidJson` | `This file isn't valid JSON.` |
| — `UnknownFormat` | `This file isn't a snippet export from Smart Snippets, TermFlow, Rephlo or InkSpoke.` |
| — `UnsupportedVersion`, version found | `This {FormatName} file is version {FoundVersion}. Smart Snippets can import version {Supported}.` (supported: `1`, `1`, `1.3`, `2`) |
| — `UnsupportedVersion`, no version | `This {FormatName} file has no version number. Smart Snippets can import version {Supported}.` |
| — `MissingEntries` | `This {FormatName} file has no "{EntriesMember}" list.` (`snippets`, `snippets`, `commands`, `Mappings`) |
| `ImportStoreFailed(string storeMessage)` | `Couldn't save the imported snippets: {storeMessage} Nothing was imported.` |
| `ImportUnexpectedFailure` | `Couldn't import the file. Nothing was imported.` (**only** for an exception thrown before `SnippetImport.Commit` is called: pick, gate, open, read, parse) |
| `ImportOutcomeUnknown` | `Couldn't finish the import. Check your snippet list before importing this file again.` (for a non-`StoreException` thrown **from inside** `SnippetImport.Commit`; never claims nothing was imported) |
| `ExportTooLarge` | `Your library is too large to export as one file: it would be over 32 MB. No file was written.` |
| — `TooManyEntries` (refusal) | `This {FormatName} file has {count:N0} entries. Smart Snippets can import up to {MaxImportEntries:N0} entries from one file.` (+ ` Nothing was imported.`) |
| `ExportTooManySnippets(int count)` | `Your library has {count:N0} snippets. One export file can hold up to {MaxImportEntries:N0}, so that it can be imported again. No file was written.` |
| `ExportTooManyEntries(int snippets, int folders)` | `Your library has {snippets:N0} snippets and {folders:N0} folders. One export file can hold up to {MaxImportEntries:N0} entries (snippets and folders), so that it can be imported again. No file was written.` |
| `ExportUnexpectedFailure` | `Couldn't export. No file was written.` |
| `ExportOutcomeUnknown(string fileName)` | `Couldn't finish the export. Check {fileName} before relying on it.` |
| `ExportCompleteTitle` | `Export complete` |
| `ExportComplete(int snippets, int folders, string fileName)` | `Exported {n} {snippet/snippets} and {m} {folder/folders} to {fileName}.` |
| `ExportFailedTitle` | `Couldn't export` |
| `ExportNotJsonFile` | `Choose a file name ending in .json.` |
| `ExportWriteFailed(string fileName)` | `Couldn't write {fileName}. Choose another folder and try again. Any existing file was left unchanged.` |

`FoundVersion` is the raw JSON (for example `2`, `"1.2"`), at most 32 characters. It is a format number, not user
content, so showing it is safe.

#### 4.3.8 Log events (counts, formats, enums and timings only)

| Event | Detail |
|---|---|
| `import_cancelled` | — |
| `import_refused` | `reason=<ImportRefusal> format=<ImportFormat or none> bytes=<n or ->` |
| `import_done` | `format=<f> entries=<n> imported=<n> duplicates=<n> unsupported=<n> rejected=<n> folders_created=<n> bytes=<n> parse_ms=<n> commit_ms=<n>`. `commit_ms` is the UI-thread portion: plan, transaction, publish and `Changed` |
| `import_unbalanced` (ERROR) | the same fields. Logged when `!summary.IsBalanced`; the dialog still shows the counts |
| `import_failed` | `kind=<StoreFailure>`. The store already logged `store_write_failed op=import_snippets error=<type> sqlite=<code> hresult=…` (D10: no message) |
| `import_error` (ERROR) | `stage=<before_commit or in_commit or after_commit> error=<exception type name> hresult=0x…`. Never the exception message: IO and SQLite messages can contain paths or text |
| `store_subscriber_failed` (ERROR, from the store) | `kind=<StoreChangeKind> index=<i> of=<n> error=<type>` (D9) |
| `export_cancelled`, `export_refused reason=<NotJsonFile or TooLarge or TooManySnippets or TooManyEntries> snippets=<n> folders=<n>` | Folder count included for serialization refusals; path-gate refusal retains snippet count only |
| `export_error` (ERROR) | `stage=<before_write or in_write or after_write> error=<type> hresult=0x…` |
| `export_done` | `snippets=<n> folders=<n> bytes=<n>` |
| `export_failed` | `error=<exception type name> hresult=0x…` |

Never logged: snippet text, labels, tags, folder names, file paths or file names (master `001:117`, `AppLog.cs:8`).
`AppLog.Error` is never given an exception object anywhere on the import or export path, including the store's reused
`Guarded` (D10), because `AppLog.Error` appends `ex.Message`.

#### 4.3.9 Entry limit (D11, user decision on Q1) and its calibration

- **Behaviour (review fix 2).** The parser refuses workload counts above `SnippetFileRules.MaxImportEntries` using
  the unchanged `TooManyEntries` message and the combined refusal count. For native files, workload = raw `snippets`
  slots + raw explicit `folders` slots; every other format counts its snippet-entry array. Version and MissingEntries
  checks still precede the limit; length checks precede mapping/accumulation. Export refuses too many snippets with
  existing `TooManySnippets`, or a combined snippet/folder overflow with sibling `TooManyEntries` and the exact
  `ExportTooManyEntries` message in §4.3.7. Exactly at the combined limit is accepted. Native folders remain separate
  from the successful snippet summary/balance. One transaction and one `Changed` event are unchanged.
- **Provisional value:** 100,000. Every test refers to the constant, never the literal, except the pinning test
  `MaxImportEntries_IsCalibratedValue`, which T10 updates together with the constant.
- **Calibration (T10, binding CI run).** `Budget_Calibrate` measures the commit stage for dense files of 25,000,
  50,000, 100,000, 200,000 and "as many as fit in 32 MB" entries. Each run uses a 20,000-snippet existing library and
  live subscribers, and the folder-dense case uses one new folder per entry. The rule:
  `MaxImportEntries` = the largest measured count, rounded down to a multiple of 10,000, at which **every** case is
  ≤ 2,000 ms. If the "fits in 32 MB" count itself passes, the limit is set to that count, so it never binds before the
  byte cap. T10 records the measured table and the chosen value in §10, and the `Budget_AtLimit_*` tests then pin the
  2,000 ms gate at the chosen limit.
- **No further decision is needed:** whatever the measurement, the outcome is a defined limit.
  - If 25,000 fails, T10 also measures 20,000 and 10,000 and applies the same rule.
  - The limit never goes below a floor of **10,000**, because a lower limit would refuse ordinary libraries. If even
    10,000 exceeds 2,000 ms, the limit is 10,000. The measured time is recorded in §10 as the accepted UI pause at the
    floor, and the `Budget_AtLimit_*` threshold becomes that measured time rounded up to the next 500 ms.

### 4.4 Sequence diagram: import

```
User: Tab to "Import snippets" → Enter (or click)
→ SnippetsPage.xaml Button  Command="{x:Bind ViewModel.Porting.ImportCommand}"
  → SnippetPortingViewModel.ImportAsync                                   [UI thread]
    ├─ IsBusy? → return null (CanExecute is already false; double-invoke guard)
    ├─ IsBusy = true
    ├─ await files.PickImportFileAsync()        App: FileOpenPicker(mainWindow.AppWindow.Id), filter .json
    │    └─ null → log import_cancelled → return null            (no dialog, no store call)
    ├─ !SnippetFileRules.HasJsonExtension(path) → refuse(NotJsonFile)
    ├─ using stream = files.OpenRead(path)     App: FileStream(Asynchronous|SequentialScan)
    │    └─ IOException / UnauthorizedAccessException → refuse(Unreadable)
    ├─ await CappedReader.ReadAsync(stream, 32 MB)   reads ≤ 33,554,433 bytes; resumes on the UI context
    │    ├─ IOException / UnauthorizedAccessException on ANY read → refuse(Unreadable)   (finding 6)
    │    ├─ > cap → refuse(TooLarge)
    │    └─ UTF-16/32 BOM or invalid UTF-8 → refuse(NotUtf8)          (UTF-8 BOM stripped)
    ├─ await workers.ParseAsync(bytes, now)   = Task.Run(() => ImportParser.Parse(bytes, now))   [WORKER; pure]
    │    (the saved library may change while this runs; nothing from the library is read before the commit)
    │    ├─ JsonDocument.Parse → JsonException → Failure(InvalidJson)
    │    ├─ detect (§4.3.3) → none → Failure(UnknownFormat)
    │    ├─ VERSION CHECK (no entry read yet) → Failure(UnsupportedVersion)
    │    ├─ entries array? → Failure(MissingEntries)
    │    └─ NativeFormat | TermFlowFormat | RephloFormat | InkSpokeFormat
    │         → each element: candidate | SkippedEntry | RejectedEntry
    │    Failure → refuse(…)                                           (store never called)
    │    ── continuation resumes on the UI context ──
    ├─ SnippetImport.Commit(parsed, store)                              [UI thread, synchronous]
    │    └─ store.ImportSnippets(parsed.Batch)
    │         ├─ AssertOwnerThread
    │         ├─ ImportPlanner.Plan(batch, _snippets, _folders)   ← library snapshot taken HERE (commit start)
    │         │    per candidate, in file order: normalize tags → resolve label → resolve folder key
    │         │    → validate (rejected → next) → dedupe key (batch.Dedupe: TextOnly | FullRow; library + in-file)
    │         │    → duplicate → next; else planned (its folder joins NewFolderNames only now)
    │         │    + native FolderNames
    │         ├─ nothing to write → ImportCommit(no ids) — NO Write, NO event
    │         └─ Write("import_snippets"): ONE connection, ONE BEGIN … COMMIT
    │              ├─ FolderRepository.Insert × NewFolderNames
    │              └─ SnippetRepository.InsertImported × plan.Snippets  (stats or fresh `now`)
    │              any SqliteException/IOException → ROLLBACK → StoreException (nothing published)
    │              any other exception → `using` disposes the transaction uncommitted → ROLLBACK
    │         ═══ COMMITTED: from here the import has happened (Publish could still throw: stage table) ═══
    │         ├─ Publish(folders + new, snippets + new)     (snapshots replaced once)
    │         └─ Raise(Changed: Imported, id: null, snippetIds: new ids)   ← ONE event
    │                each subscriber in its own try/catch (D9): a throw → store_subscriber_failed, next runs
    │                ├─ SnippetListViewModel.OnStoreChanged → Rebuild          (main window list)
    │                ├─ PopupViewModel.OnStoreChanged → RebuildSearch/Refresh  (popup, sorts)
    │                ├─ FoldersPageViewModel.OnStoreChanged → Refresh          (folder counts)
    │                └─ SnippetEditorViewModel.OnStoreChanged → RefreshChoices (new folders in the box)
    │         → returns ImportCommit (a subscriber failure alone cannot prevent this)
    │    → ImportSummary (parse counts + commit counts)
    ├─ StoreException → log import_failed → ShowErrorAsync(ImportFailedTitle, ImportStoreFailed(msg))
    ├─ refusal (incl. TooManyEntries) → log import_refused → ShowErrorAsync(ImportFailedTitle, ImportRefusalMessage)
    ├─ unexpected exception → stage table below → its dialog is shown HERE, still inside ImportAsync
    ├─ success → log import_done (+ import_unbalanced if !IsBalanced)
    │          → await dialogs.ShowInfoAsync(Complete|Nothing title, ImportSummary text)
    │               App: ContentDialog, body TextBlock at ITextSizeService.FontSize, wrapping
    └─ finally IsBusy = false     ← only after every dialog above has closed (single owner, round-2 R6)
```

**Failure stages (review finding 4; round-2 R6).** `ImportAsync` itself catches any exception that is not
cancellation, inside its `try` and before its `finally`. It classifies the exception by the stage it recorded, then
awaits the error dialog. The stage decides both the log and the message. "Nothing was imported" is used **only** where
it is provably true. The relay command adds no handling.

| Stage when it escaped | Can rows be on disk? | Log | User message |
|---|---|---|---|
| `before_commit` (pick, gate, open, read, parse) | No | `import_error stage=before_commit …` | `ImportUnexpectedFailure` ("… Nothing was imported.") |
| `in_commit`, `StoreException` | No (rolled back) | `import_failed kind=…` | `ImportStoreFailed` |
| `in_commit`, any other exception | Only if it was thrown by `Publish` after COMMIT; a subscriber failure alone cannot reach here (D9) | `import_error stage=in_commit …` | `ImportOutcomeUnknown` (never "nothing imported") |
| `after_commit` (the summary dialog itself) | Yes | `import_error stage=after_commit …` | none (the import succeeded; the log records it) |

**Export stages (round-2 improvement).** `ExportAsync` has its own table and never uses import wording:

| Stage when it escaped | Old target file | Log | User message |
|---|---|---|---|
| `before_write` (pick, gate, serialize on the worker) | Untouched; no temp file was created | `export_error stage=before_write …` | `ExportUnexpectedFailure` ("Couldn't export. No file was written.") |
| `in_write`, `IOException` / `UnauthorizedAccessException` | Untouched (the replace did not happen) | `export_failed …` | `ExportWriteFailed(fileName)` |
| `in_write`, any other exception | Unknown (it may have been thrown after the move) | `export_error stage=in_write …` | `ExportOutcomeUnknown(fileName)` |
| `after_write` (the success dialog itself) | Replaced, as intended | `export_error stage=after_write …` | none |

Export runs `NativeExport.Write` through `workers.ExportAsync` (a worker thread) over the store's immutable snapshot
lists, captured on the UI thread before the await. A huge library therefore does not freeze the UI, and an oversized
one stops at the 32 MB bound (§4.3.2).

Export:

```
"Export snippets" → Porting.ExportCommand → ExportAsync
  → files.PickExportFileAsync(SnippetFileRules.ExportBaseName(local now))  FileSavePicker(.json default)
     null → export_cancelled
  → !HasJsonExtension(path) → ShowErrorAsync(ExportFailedTitle, ExportNotJsonFile)
  → var (f, s) = (store.Folders, store.Snippets)                            (UI thread: capture immutable snapshots)
  → await workers.ExportAsync(f, s, now) = Task.Run(() => NativeExport.Write(f, s, now))   (worker; compact; bounded)
     TooManySnippets → export_refused reason=TooManySnippets → ShowErrorAsync(ExportFailedTitle, ExportTooManySnippets(n))
     TooManyEntries → export_refused reason=TooManyEntries → ShowErrorAsync(ExportFailedTitle, ExportTooManyEntries(n, m))
     TooLarge → export_refused reason=TooLarge → ShowErrorAsync(ExportFailedTitle, ExportTooLarge); no file written
  → await files.WriteAtomicallyAsync(path, bytes)          App → Core AtomicFileWriter: temp file + File.Move(overwrite)
     IOException/UnauthorizedAccessException → export_failed → ShowErrorAsync(ExportFailedTitle, ExportWriteFailed(name))
  → export_done → ShowInfoAsync(ExportCompleteTitle, ExportComplete(n, m, Path.GetFileName(path)))
```

**Parallel paths checked:**
- **Snippet writers:** `CreateSnippet` (editor, popup add) and the new `ImportSnippets` are the only inserters. Both go
  through `SnippetRepository.InsertImported`, since `Insert` delegates to it, so there is one INSERT statement. Both
  apply the same four rules (§4.3.5 versus `Prepare`, `SnippetStore.cs:307-323`). The differences are deliberate and
  documented: import has stats, folders by name instead of id, dedupe, and no `FolderNotFound` (names, not ids).
- **Refresh readers:** the four `Changed` subscribers in §3 are kind-agnostic. No other cache of snippets exists: the
  popup's search cache is rebuilt in `OnStoreChanged`. grep `_store.Snippets` / `store.Snippets` in `src/` finds only
  readers that run on demand or on `Changed`.
- **File readers and writers:** the pre-existing file I/O in `src/` is listed in §3 (icon `File.Exists`, migrator
  backup name, `AppLog`); none of it touches snippet files. The feature's only read path is
  `ISnippetFileService.OpenRead` → `CappedReader.ReadAsync`. Its only write path is
  `ISnippetFileService.WriteAtomicallyAsync` → `AtomicFileWriter.WriteAsync`. T12 audits that the feature added no
  other path (scoped to the files this plan creates or changes, not a whole-tree invariant).
- **Usage writers (finding 3):** `SnippetStore.RecordUsage`, `SnippetRepository.RecordUsage` and
  `FakeSnippetStore.RecordUsage` all saturate (D8). Import is the fourth writer of `usage_count`, through
  `InsertImported`, with values already in `0..int.MaxValue` (D8).
- **Event raising (finding 4):** `Raise` is the store's only path to `Changed`, so subscriber isolation (D9) covers
  every mutation kind at one site. `FakeSnippetStore.Raise` mirrors it.
- **Dialogs:** the result dialog goes through `IDialogService` only, so the existing single-dialog semaphore
  (`DialogService.cs:68`) applies.

### 4.5 Surface list

| Surface | Change | Task |
|---|---|---|
| Snippets page toolbar | Two icon buttons after New snippet: Import (glyph `&#xE8B5;`), Export (`&#xEDE1;`), with tooltips and accessible names from `UiText`; tab order search → New snippet → Import → Export | T10 |
| Open picker | Windows App SDK `FileOpenPicker`, `.json` filter, commit text "Import" | T10 |
| Save picker | `FileSavePicker`, suggested name `smart-snippets-YYYY-MM-DD`, default extension `.json`, type choice "JSON" | T10 |
| Result / error dialogs | `ShowInfoAsync` (new) for summaries; `ShowErrorAsync` (existing) for refusals and failures | T9, T10 |
| Main-window snippet list, Folders page, editor folder box, popup | Refresh through `Changed(Imported)`; **no code change** | T6 (tests), T12 |
| Store-wide behaviour (all mutations) | `Changed` subscribers isolated (D9); `store_write_failed` without message (D10); `RecordUsage` saturates and the reader clamps (D8) | T6 |
| Log | §4.3.8 events | T9 |
| `docs/requirements/001-prd.md` §3, §4 | Scope change recorded | T11 |
| `docs/plan/001-master-smart-snippets-v1.md` §2, §5, §9 | Non-goal removed with a note; 010 row; log row | T11 |
| `docs/design/001-smart-snippets-design.html` | **Not changed.** The two buttons follow the existing toolbar button style; the design file has no import/export screen and this plan does not add one | — |
| `NativeMethods.txt`, schema/migrations, settings, tray, popup code | **Not changed** | — |

## 5. Impact and risk

| Question | Answer |
|---|---|
| State management — what survives a crash mid-operation? | Import is one SQLite transaction (WAL). A crash before `COMMIT` leaves nothing; after it, everything. No in-memory state outlives the call. Export writes a temporary file and replaces the target, so a crash leaves the old file (or none) plus at most one stray `*.tmp`, never a truncated target. |
| Data consistency — orphans, races, double-apply? | Folders are created inside the same transaction, so no orphan folder without its snippets (and native empty folders are intended). The `UNIQUE(name_key)` constraint fails closed: a clash rolls everything back. Dedupe and write use one synchronous snapshot on the UI thread, so nothing can interleave. Double-apply: `IsBusy` plus `CanExecute`, and the dedupe means re-importing the same file adds 0 (AC7). |
| User experience — root problem or symptom; any surprise? | It solves the actual migration from three apps plus backup. Possible surprises: duplicates are matched ignoring line-ending style (D1); a tag containing a comma is split; secrets and parameterized InkSpoke commands are skipped (and counted, so not silent); Rephlo `fixedContentAppend` is dropped (by the brief). |
| Backward compatibility — existing data / sessions / configs? Migration? | No schema or settings change. `ISnippetStore` gains one member (2 implementers updated). `SnippetsPageViewModel`'s constructor gains a trailing optional parameter, so existing calls compile unchanged. `StoreChangeKind.Imported` is appended; the enum is not persisted. |
| Error recovery — what happens on failure; can it recover? | Every refusal and store failure shows a specific message ending "Nothing was imported." and leaves the library unchanged; the user can retry. After COMMIT the import is always acknowledged: a throwing view is isolated and logged (D9), and the summary still shows. An exception the stage table cannot rule out shows `ImportOutcomeUnknown`, never "nothing imported". Export failure or refusal leaves any existing file unchanged and removes the temp file. `IsBusy` is reset in `finally`. |
| Logging & debugging — enough to diagnose in the field? | `import_refused reason=…`, `import_done` with all counts, bytes and ms, `import_unbalanced` if the invariant breaks, the store's `store_write_failed op=import_snippets`, and the exception type and HResult. Per-entry reasons are counted in memory, not logged per entry. |
| Edge cases — empty, huge, repeated, concurrent, interrupted? | Empty entries array → "Nothing imported", 0/0/0/0. Exactly 32 MB accepted, 32 MB + 1 refused; an export that would exceed 32 MB is refused before writing. Usage at the import cap plus later pastes saturates instead of wrapping (D8). All duplicates → no write, no event. Same file twice → second adds 0. Import while the editor is dirty → the editor keeps its edits (no guard needed: nothing existing is modified). Picker cancelled → nothing. Concurrent: one UI thread, `IsBusy`. Interrupted: transaction / atomic replace. Export of an empty library → valid file with empty arrays. |

**Risks:**
- **R1 — Duplicate semantics (D1) differ from the literal word "exact".** → D1 awaits user approval. Tests show that
  only line endings are normalized (case, spaces and tabs still differ). A native full-row key repeat collapses on
  round trip; this is pinned and visible.
- **R2 — UI-thread time at the 32 MB cap (findings 7 and 2).** Parsing runs on a worker. The UI-thread portion
  (`SnippetImport.Commit`: plan, transaction, publish, the four `Changed` subscribers) grows with the entry count, the
  folder count and the existing library size, not with bytes. A 32 MB file of short entries can hold several hundred
  thousand rows. → **Enforceable gate plus a defined limit (D11).** The gate is the `Budget_*` tests in a Release
  build, run by a **new CI step** on the `windows-latest` runner (T10 edits `ci.yml`), with a real `SnippetStore` and
  real list, popup, folders and editor view-models subscribed. The **commit stage only** must take **≤ 2,000 ms** for
  each of:
  - (a) a **dense** TermFlow file of tiny distinct entries (`{"text":"s0000001"}` style);
  - (b) a **folder-dense** file with one new folder per entry;
  - (c) a **typical** file at the byte cap: 1 KB texts, with tags and folders;
  - each at the entry limit (or the byte cap, whichever binds first), imported into an **existing library of 20,000
    snippets**.

  The parse stage is measured and logged, not gated (it is off the UI thread). Each budget case also proves the work
  was completed (round-2 R7): expected imported count, balanced summary, a persisted-row count from a fresh store, a
  sample imported text found through the list view-model's search, and **no** `store_subscriber_failed` in `app.log`.

  **If the dense or folder-dense case exceeds 2,000 ms at the byte cap, the entry limit binds.** T10 calibrates
  `MaxImportEntries` per §4.3.9 (user decision, 2026-10-28). This is defined behaviour: no stop, and no change to "one
  transaction / one event".

  The busy path (a competing writer holding the database) is pre-existing for every store write (driver default 30 s,
  §3). It is tested for correctness only (StoreException, nothing written, no event) and accepted as is.
- **R3 — Pickers in an unpackaged, self-contained, possibly elevated app.** → `Microsoft.Windows.Storage.Pickers`
  (Windows App SDK 1.8, already pinned) is documented to work elevated. Runbook steps 1 and 12 check it unelevated and
  elevated. If it fails, stop and report; no Win32 `IFileDialog` fallback is planned.
- **R4 — AOT warnings from new code.** `JsonDocument` and a source-generated context are AOT-clean; the picker
  projections ship with Windows App SDK. → CI gate (`workflow_dispatch` on the branch before merge) must show 0
  IL2xxx/IL3xxx.
- **R5 — Detection precedence.** A future TermFlow file that adds a `format` member would be refused as
  `UnknownFormat`, not misread as native. → Safe direction (refuse, never misroute); test
  `Detect_FormatMemberOtherThanOurs_Unknown`.
- **R6 — Lone surrogates.** Import rejects them (`InvalidText`); export writes U+FFFD for any already stored. → Accepted.
  The AC7 round-trip covers valid Unicode; a test pins the U+FFFD behaviour so it is visible.
- **R7 — Personal data in test fixtures.** The real sample files hold personal prompts. → Fixtures are small synthetic
  files with the same keys and types and invented content. The real files are used only in the manual runbook from
  `D:\temp\export-ref`. A T12 grep checks that no test or source references `D:\temp` or `export-ref`.
- **R8 — InkSpoke secrets not flagged `IsSecret`.** An encrypted text without the flag would import as ciphertext. →
  Cannot be detected reliably; the brief's rule is followed. Both locations of the flag (params and mapping) are
  checked.
- **R9 — Toolbar width.** Three buttons beside the search box. In **wide mode** the list column is a fixed 320 px
  (`SnippetsPage.xaml:57`), about 300 px after padding. In narrow mode the code-behind makes it star-width. → Icon-only
  buttons (D7). Runbook step 9a measures the search box in wide mode at exactly the 880 px threshold (the tightest wide
  layout); step 9b checks narrow mode. A search box under 120 px wide → apply the D7 fallback (New snippet icon-only)
  and re-measure.
- **R10 — Text size 32 in the result dialog.** → The body uses `ITextSizeService.FontSize` and wraps; `ContentDialog`
  scrolls its content area. Runbook step 10.
- **R11 — Counting bug across four mappers and the planner.** → `ImportSummary.IsBalanced` is checked at runtime
  (`import_unbalanced`) and asserted in every fixture test plus a property-style test over all refusal-free fixtures.
- **R12 — Conflict with 008-harness on the master plan.** → T11 is the last code-free task; rebase onto `main` before
  editing.
- **R13 — Subscriber isolation (D9) can hide a view bug.** A throwing view no longer crashes the caller; it logs. →
  `store_subscriber_failed` is logged at ERROR with the subscriber type. A Core test pins that the other subscribers
  still run and the caller gets its result. Every existing test still passes, which shows no current test relied on a
  subscriber exception propagating.
- **R14 — A library too large to export.** With compact JSON and escaping, roughly 5–30 MB of stored text, depending on
  how much is non-ASCII, reaches the 32 MB limit, as does a library whose combined snippets + folders exceed `MaxImportEntries` (D11). →
  Refused with a specific message and no file (A2, D11). Serialization runs off the UI thread and stops at the bound.
  Subset export is out of scope by the brief, so such a library cannot be backed up in one file. This is accepted by
  the user's decisions A2 and D11.
- **R15 — Usage semantics change at the extreme (D8).** A count already at `int.MaxValue` stops increasing instead of
  wrapping negative. → Strictly better than today; tested.

**Rollback:** revert the merge commit. No schema change, no settings key, nothing to migrate back. Snippets already
imported stay in the library as ordinary snippets (deletable as usual).

## 6. Tasks

Order: T0 → T1 … T9 can all be done and tested on Linux. T10 needs Windows (WinUI compiles only there). T11 and T12
close out. Each task lists the tests that must exist and pass; "dies if" names the test that fails when that hop of the
chain breaks (08 chain map). **Write each new test red first** (against the stub or the missing call) and confirm it
fails for the stated reason.

### T0 — Shared contracts (AC1–AC8 enabler)
- **Files:** new `src/SmartSnippets.Core/Porting/PortingContracts.cs`, new
  `src/SmartSnippets.Presentation/Services/ISnippetFileService.cs`; `src/SmartSnippets.Core/Store/StoreChangeKind.cs`.
- **Change:** §4.3.1 verbatim; append `Imported` to `StoreChangeKind`. No interface members are added to
  `ISnippetStore` or `IDialogService` yet (that happens in T6 and T9, with their implementations).
- **Verify:** `dotnet build src/SmartSnippets.Presentation` → 0 warnings; both test projects still pass.
- **Test that dies if this breaks:** `ImportSummaryTests.IsBalanced_TrueOnlyWhenPartsSumToEntries` (Core, written here:
  balanced, one-short, one-over).

### T1 — Bounded read, encoding and file rules (AC8)
- **Files:** `src/SmartSnippets.Core/Porting/SnippetFileRules.cs`, `CappedReader.cs`, `AtomicFileWriter.cs`;
  `tests/SmartSnippets.Core.Tests/Porting/CappedReaderTests.cs`, `SnippetFileRulesTests.cs`, `AtomicFileWriterTests.cs`.
- **Change:** §4.3.2. `ReadAsync` loops `ReadAsync` into a buffer that grows up to `maxBytes + 1`, stops at
  `maxBytes + 1` bytes or EOF, never calls `Stream.Length`, and maps `IOException`/`UnauthorizedAccessException` from
  any read to `Unreadable` (finding 6). The production cap is `SnippetFileRules.MaxImportBytes` (32 MB). Tests pass
  small `maxBytes` values for speed, plus one test at the real constant.
- **Verify:** `dotnet test tests/SmartSnippets.Core.Tests --filter "FullyQualifiedName~Porting"`.
- **Tests that die if this breaks:**
  - `MaxImportBytes_Is32MiB` (`== 33_554_432`) and `MaxImportEntries_IsCalibratedValue` (provisional `100_000`;
    T10 updates both the constant and this test, §4.3.9)
  - `ExactlyCap_Accepted`, `CapPlusOne_TooLarge` (with the real 32 MB constant)
  - `TooLarge_StopsReadingAtCapPlusOne` (a counting stream with 64 MB of data and `CanSeek = false`: reads ≤ cap + 1
    bytes)
  - `LengthLies_StillCapped` (a stream whose `Length` reports 10 bytes but yields cap + 1)
  - `ReadThrowsIOException_OnFirstRead_Unreadable`
  - `ReadThrowsIOException_AfterSomeBytes_Unreadable` (the stream yields 1,000 bytes, then throws)
  - `ReadThrowsUnauthorized_Unreadable`
  - `ReadCancelled_PropagatesOperationCanceled`
  - `ReadThrowsInvalidData_Propagates` (round-2 R4: only the two I/O types are mapped)
  - `Utf8Bom_Stripped`, `Utf16LeBom_NotUtf8`, `Utf16BeBom_NotUtf8`, `Utf32Bom_NotUtf8`
  - `InvalidUtf8InsideString_NotUtf8` (bytes `C3 28` inside a string)
  - `Empty_ReturnsEmptyBytes` (and the parser then says `InvalidJson`)
  - `HasJsonExtension`: `.json`, `.JSON` true; `.json.txt`, `.txt`, no extension, `json` false
  - `ExportBaseName_IsInvariantDate`
  - **`AtomicFileWriter`** (`AtomicFileWriterTests`, real temporary folder plus the injectable overload):
    - `Write_NewFile_CreatedNoTempLeft`
    - `Write_ExistingFile_Replaced`
    - `OpenTempFails_TargetUntouched_NoTemp`
    - `WriteFailsMidway_TargetUntouched_TempDeleted` (the injected stream throws after N bytes)
    - `MoveFails_TargetUntouched_TempDeleted` (the injected move throws)
    - `TempDeleteFails_OriginalExceptionStillThrown`
    - `TempStreamDisposedBeforeMove` (the injected move asserts the stream it was given is already disposed)

### T2 — Parser: detection, version-first refusal, value rules (AC2, AC8)
- **Files:** `src/SmartSnippets.Core/Porting/ImportParser.cs`, `JsonFields.cs` (internal);
  `tests/SmartSnippets.Core.Tests/Porting/ImportParserDetectionTests.cs`.
- **Change:** §4.3.3 steps 1–5, with the four mappers as stubs that return `Ok` with zero entries (filled in T3/T4).
  §4.3.5 date, usage and string helpers in `JsonFields`. Their tests go through the public parser and therefore live in
  T3 (`ValueRulesTests`), once the TermFlow mapper exists. There is no `InternalsVisibleTo` (finding 8).
- **Verify:** `dotnet test … --filter "FullyQualifiedName~ImportParser"`.
- **Tests that die if this breaks** (inline JSON strings):
  - **Detection:**
    - `Detect_NativeMarker_Native`
    - `Detect_FormatMemberOtherThanOurs_Unknown`
    - `Detect_SnippetsKey_TermFlow`
    - `Detect_CommandsKey_Rephlo`
    - `Detect_MappingsKey_InkSpoke`
    - `Detect_LowercaseMappings_Unknown` (case-sensitive)
    - `Detect_NoKnownKey_Unknown`
    - `Detect_RootArray_Unknown`
    - `Detect_SnippetsAndCommands_TermFlowWins` (precedence pinned)
  - **Version-first:**
    - `Version_TermFlow2_Unsupported_FoundVersion2`
    - `Version_TermFlowString1_Unsupported`
    - `Version_TermFlow1Point0_Unsupported`
    - `Version_TermFlowMissing_UnsupportedNoVersion`
    - `Version_Rephlo12_Unsupported`
    - `Version_RephloNumber13_Unsupported`
    - `Version_InkSpoke3_Unsupported`
    - `Version_Native2_Unsupported`
    - `Version_CheckedBeforeEntries`: `{"version":2,"snippets":5}` → `UnsupportedVersion`, not `MissingEntries`; and
      `{"version":2,"snippets":[42]}` → `UnsupportedVersion` with no rejection counted.
  - **Missing entries:** `Entries_Missing_MissingEntries`, `Entries_NotArray_MissingEntries`
  - **Entry limit (D11):**
    - `Entries_AtLimit_Accepted` (`MaxImportEntries` minimal entries)
    - `Entries_LimitPlusOne_TooManyEntries_WithCount`
    - `Entries_OverLimit_RefusedBeforeAnyEntryRead` (all entries are malformed; the result is `TooManyEntries`, with no
      rejection counted)
    - `Entries_OverLimit_AfterVersionCheck` (wrong version + too many → `UnsupportedVersion`)
  - **JSON errors:** `InvalidJson_Syntax`, `InvalidJson_TrailingComma`, `InvalidJson_Comment`,
    `InvalidJson_TrailingGarbage`, `InvalidJson_DepthOver64`
  - **Never throws:** `Parse_NeverThrows` (a fixed list of 30 nasty inputs: empty, `null`, `[]`, `{}`, `"x"`, deep
    nesting, huge numbers; each returns a result)

### T3 — TermFlow and native mappers, value rules (AC1, AC3, AC4)
- **Files:** `src/SmartSnippets.Core/Porting/TermFlowFormat.cs`, `NativeFormat.cs`;
  `tests/SmartSnippets.Core.Tests/Porting/Fixtures/termflow-v1.json`, `native-v1.json`;
  `tests/SmartSnippets.Core.Tests/Porting/TermFlowFormatTests.cs`, `NativeFormatTests.cs`, `ValueRulesTests.cs`,
  `Fixture.cs` (loads a fixture by name); `tests/SmartSnippets.Core.Tests/SmartSnippets.Core.Tests.csproj` (add
  `<None Include="Porting\Fixtures\*.json" CopyToOutputDirectory="PreserveNewest" />`).
- **`ValueRulesTests`** (each one is a one-entry TermFlow document through `ImportParser.Parse`, asserting the
  candidate's `Stats`, or for text the rejection; "now" = `2026-01-01T00:00:00Z`):
  - **Dates:**
    - `Date_Int_Kept`
    - `Date_1e400_Invalid` (raw literal → `createdAt` = now)
    - `Date_Fraction_Invalid`
    - `Date_IntegralDouble_Kept` (`1.7e12`)
    - `Date_Negative_Invalid`
    - `Date_String_Invalid`
    - `Date_Future_ClampedToNow`
  - **Date Int64 boundary (finding 11),** each → clamped to now, never negative, never throws:
    - `Date_LongMax_ClampedToNow` (`9223372036854775807`)
    - `Date_TwoPow63_ClampedToNow` (`9223372036854775808`)
    - `Date_TwoPow63Scientific_ClampedToNow` (`9.223372036854775808e18`)
    - `Date_1e300_ClampedToNow`
    - `Date_LongMin_Invalid` (`-9223372036854775808`)
    - `Date_Minus1e300_Invalid`
  - **Usage:**
    - `Usage_Negative_Zero`
    - `Usage_Fraction_Zero`
    - `Usage_String_Zero`
    - `Usage_OneBillionAndThree_KeptExactly` (`1000000003`: no lower cap, round-2 R1)
    - `Usage_IntMax_KeptExactly` (`2147483647`)
    - `Usage_IntMaxPlusOne_Saturated` (`2147483648` → `int.MaxValue`)
  - **Usage Int64 boundary,** each → `int.MaxValue` (saturated, not zero):
    - `Usage_LongMax_Saturated`
    - `Usage_TwoPow63_Saturated`
    - `Usage_TwoPow63Scientific_Saturated`
    - `Usage_1e300_Saturated`
    - `Usage_1e400_Zero` (infinity is invalid)
  - **Other:**
    - `UpdatedBeforeCreated_SetToCreated`
    - `Text_LoneSurrogate_RejectedInvalidText`
    - `Label_LoneSurrogate_RejectedWrongFieldType`
    - `Null_IsAbsent`
- **Change:** §4.3.4 TermFlow and native tables; stats per §4.3.5. Fixture contents and expected parse outcome per
  entry are in §7.1. Per-entry outcomes are asserted by index, not just totals.
- **Verify:** `dotnet test … --filter "FullyQualifiedName~TermFlowFormat|FullyQualifiedName~NativeFormat|FullyQualifiedName~ValueRules"`.
- **Tests that die if this breaks:**
  - `TermFlow_Fixture_PerEntryOutcomes` (each index → candidate / rejected reason, as §7.1)
  - `TermFlow_Fixture_StatsKept` (entry 1: all four values exact)
  - `TermFlow_Fixture_StatsDefaulted` (entry 6: `createdAt = now`, `updatedAt = createdAt`, `lastUsedAt = null`, usage 0)
  - `TermFlow_IdIgnored_NotRequired`
  - `TermFlow_TextVerbatim_NoTrim_LineEndingsKept`
  - `TermFlow_LabelWrongType_Rejected`
  - `TermFlow_TagsElementNotString_Rejected`
  - `TermFlow_ExtraField_Tolerated`
  - `Native_Fixture_PerEntryOutcomes`
  - `Native_IsLabelCustomFalse_LabelNull`
  - `Native_BatchDedupeIsFullRow`, `TermFlow_BatchDedupeIsTextOnly` (and in T4: Rephlo and InkSpoke are `TextOnly`)
  - `Native_FoldersList_IncludesEmptyFolder`
  - `Native_FolderElementMalformed_IgnoredNotCounted`

### T4 — Rephlo and InkSpoke mappers (AC1, AC3)
- **Files:** `src/SmartSnippets.Core/Porting/RephloFormat.cs`, `InkSpokeFormat.cs`; fixtures `rephlo-1.3.json`,
  `inkspoke-v2.json`; `RephloFormatTests.cs`, `InkSpokeFormatTests.cs`.
- **Change:** §4.3.4 Rephlo and InkSpoke tables, check order as listed.
- **Verify:** `dotnet test … --filter "FullyQualifiedName~RephloFormat|FullyQualifiedName~InkSpokeFormat"`.
- **Tests that die if this breaks:**
  - `Rephlo_Fixture_PerEntryOutcomes`
  - `Rephlo_FixedContentAppend_NeverAppended` (the text equals `instruction` exactly; the append sentinel is absent)
  - `Rephlo_Archived_SkippedBeforeTextCheck` (archived with no instruction → skipped, not rejected)
  - `Rephlo_StatsNull_Fresh`
  - `InkSpoke_Fixture_PerEntryOutcomes`
  - `InkSpoke_SecretInParams_Skipped`
  - `InkSpoke_SecretOnMapping_Skipped`
  - `InkSpoke_LowercaseTextKey_Imported`
  - `InkSpoke_UpperTextWinsOverLower`
  - `InkSpoke_EmptyParams_Unparsable`
  - `InkSpoke_ParamsArray_Unparsable`
  - `InkSpoke_ParameterizedCheckedBeforeParams` (parameterized with unparsable params → `Parameterized`)
  - `InkSpoke_MissingGroupId_Unfiled`
  - `InkSpoke_UnknownGroupId_Unfiled`
  - `InkSpoke_GroupsAbsent_AllUnfiled`
  - `InkSpoke_FavoritesVocabularyIgnored`
  - `InkSpoke_ActionTypeMissing_Rejected`

### T5 — Import planner (AC3, AC5)
- **Files:** `src/SmartSnippets.Core/Porting/ImportPlanner.cs`; `tests/SmartSnippets.Core.Tests/Porting/ImportPlannerTests.cs`.
- **Change:** §4.3.5 labels, tags, folders, duplicates and validation; returns `ImportPlan`.
- **Verify:** `dotnet test … --filter ImportPlanner`.
- **Tests that die if this breaks:**
  - **Duplicates:**
    - `Duplicate_OfLibrary_Skipped`
    - `Duplicate_WithinBatch_FirstWins`
    - `Duplicate_LineEndingsOnly_Skipped` (library `"a\rb"` versus file `"a\nb"` and `"a\r\nb"`)
    - `NotDuplicate_CaseDiffers`
    - `NotDuplicate_TrailingSpaceDiffers`
    - `NotDuplicate_TabVersusSpaces`
  - **Full-row dedupe (`FullRow`, A1):**
    - `FullRow_SameTextDifferentFolder_BothPlanned`
    - `FullRow_SameTextDifferentLabel_BothPlanned`
    - `FullRow_SameTextDifferentTags_BothPlanned`
    - `FullRow_SameTextDifferentTagOrder_BothPlanned` (order-sensitive)
    - `FullRow_SameEverything_DifferentStats_SecondIsDuplicate`
    - `FullRow_CrVsLf_SameMetadata_Duplicate`
    - `FullRow_CrVsLf_DifferentLabel_BothPlanned`
    - `FullRow_MatchesLibraryRow_BuiltFromStoredFolderAndTags` (the library row's key uses its folder name through
      `FolderId`)
    - `FullRow_AutoLabelKeyUsesSuggestion` (`isLabelCustom:false` with a stale file label matches the library row whose
      auto label is the suggestion)
    - `FullRow_FolderCaseDiffers_SameKey_Duplicate`
    - `TextOnly_SameTextDifferentFolder_SecondIsDuplicate` (third-party rule unchanged)
  - **Full-row key equality (round-2 R2).** Each test runs twice: library row versus file entry, and two entries within
    the file.
    - `FullRow_SeparatorInText_VersusSeparatorInLabel_NotDuplicate`: `text "a\u001Fb", label "c"` versus
      `text "a", label "b\u001Fc"`, both planned. A joined-string key would merge them.
    - `FullRow_SeparatorInsideTag_VersusTwoTags_NotDuplicate`: tags `["x\u001Fy"]` versus `["x","y"]`
    - `FullRow_NulAndControlCharsInComponents_NotDuplicate`
    - `FullRow_EmptyTagsVersusOneTag_NotDuplicate`
    - `FullRow_TagBoundaryShift_NotDuplicate`: `["ab","c"]` versus `["a","bc"]`
    - `FullRow_SeparatelyAllocatedEqualTags_Duplicate`: equal tag lists from different arrays **are** duplicates,
      which kills a default record-struct reference comparison.
    - All of these go through the public `ImportPlanner.Plan`. `FullRowKey` and its comparer are internal, and Core
      has no `InternalsVisibleTo`.
  - **No incidental folders (review, nonblocking):**
    - `DuplicateCandidate_WithUnseenFolder_CreatesNoFolder`
    - `RejectedCandidate_WithUnseenFolder_CreatesNoFolder`
    - `AllDuplicates_WithNovelFolderNames_NoNewFolders`
    - `NativeFolderNames_StillCreatedWhenNoCandidateUsesThem` (the documented exception)
  - **Folders:**
    - `Folder_MatchesExistingCaseInsensitive_UsesExistingId`
    - `Folder_NewTwiceDifferentCase_OneNewFolder_FirstSpelling`
    - `Folder_Blank_Unfiled`
    - `Folder_Whitespace_Unfiled`
    - `Folder_Trimmed`
    - `FolderNames_ProcessedFirst_SpellingWins`
    - `FolderNames_ExistingNotRecreated`
  - **Labels and tags:**
    - `Label_Given_KeptExactlyAsCustom`
    - `Label_Missing_Suggested_FromFirstTag`
    - `Label_Missing_Suggested_FromFirstLine`
    - `Tags_CommaSplit_CaseDeduped`
  - **Validation and counts:**
    - `Validation_Failure_CountedRejected` (driven by a candidate whose text is whitespace, bypassing the parser)
    - `Plan_DoesNotMutateInputs`
    - `Counts_DuplicatesPlusPlannedPlusRejected_EqualCandidates`

### T6 — Store bulk import (AC4, AC5, AC6, AC3)
- **Files:**
  - `src/SmartSnippets.Core/Store/ISnippetStore.cs`, `SnippetStore.cs` (`ImportSnippets`; plus D8 `RecordUsage`
    saturation, D9 `Raise` isolation, D10 `Guarded` logging)
  - `src/SmartSnippets.Core/Data/SnippetRepository.cs` (`InsertImported`; `Insert` delegates; D8 `RecordUsage` SQL and
    `Read` clamp)
  - `src/SmartSnippets.Core/Porting/SnippetImport.cs`
  - `tests/SmartSnippets.Presentation.Tests/Fakes/FakeSnippetStore.cs`:
    - implements `ImportSnippets` with `ImportPlanner` and in-memory ids;
    - honours `Failure`;
    - records `ImportCalls` (count) and the batches;
    - `ThrowOnImport` (throws if called at all);
    - `OnImport` (records the calling thread);
    - D8 saturation in `RecordUsage` and D9 isolation in `Raise`.
  - `tests/SmartSnippets.Core.Tests/Store/SnippetStoreImportTests.cs`, `SnippetStoreObserverTests.cs`
  - `tests/SmartSnippets.Core.Tests/Data/SnippetRepositoryTests.cs` (+3)
  - `tests/SmartSnippets.Core.Tests/Porting/SnippetImportIntegrationTests.cs`
- **Change:**
  - `ImportSnippets` as in the §4.4 store branch: plan → nothing to write → return without `Write`/`Raise`; else one
    `Write("import_snippets", …)` → `Publish` once → `Raise(Imported, null, ids)` once.
  - Folder ids for new folders come from `FolderRepository.Insert` inside the same transaction.
  - `SnippetImport.Commit(parsed, store)` → `ImportSummary`, as in §4.3.2 (finding 8: it takes a `ParsedImport`, so
    it cannot see any refusal; refusals are owned per §4.3.2's table).
  - D8, D9 and D10 exactly as in §4.3.2 "Changes to existing store code".
- **Verify:** `dotnet test tests/SmartSnippets.Core.Tests --filter "FullyQualifiedName~Import"`, then both full test
  projects (sequentially).
- **Tests that die if this breaks:**
  - **Repository:**
    - `InsertImported_PersistsAllStats_Reload` (created, updated, lastUsed, usage exact after reopen)
    - `Insert_StillFreshStats` (existing behaviour kept)
    - `RecordUsage_AtIntMax_Saturates` (row set to `2147483647` by SQL; after the call it is still `2147483647`)
    - `Read_UsageAboveIntMax_ClampedToIntMax` (row set to `3000000000` by raw SQL → reads `int.MaxValue`, never
      negative)
  - **Usage chain, store half (finding 3; round-2 R1/R3; no export here, because `NativeExport` arrives in T7):**
    - `UsageImported_Paste_Sort_Reopen`: import a native batch (built directly as an `ImportBatch`, no parser) with
      usage `1000000000`, and call `RecordUsage` 3 times. The snapshot shows exactly 1,000,000,003,
      `SnippetSort.Apply(…, UsageCount)` puts it first, and `NewStore()` reloads exactly 1,000,000,003.
    - `UsageAtIntMax_Paste_Saturates_Reopen`: import usage `int.MaxValue - 1`, then call `RecordUsage` twice. The
      snapshot and the database both hold `int.MaxValue` (never negative), and the sort still puts it first.
    - The export half of this chain is in T7 (`UsageChain_Export_Reimport_Exact`).
  - **Observer isolation (finding 4, `SnippetStoreObserverTests`, real store):**
    - `Import_SubscriberThrows_CommitReturned_RowsPersisted_LaterSubscribersRan`. Three subscribers in order: a
      recorder, one that throws `InvalidOperationException`, and a second recorder. `ImportSnippets` returns its
      `ImportCommit` (no exception); both recorders saw exactly one `Imported` event; `NewStore()` has the rows;
      `app.log` has `store_subscriber_failed kind=Imported index=<i> of=<n> error=InvalidOperationException` (format pinned by §4.3.2), and no exception message.
    - `Create_SubscriberThrows_ResultReturned` (the same isolation for an existing kind, since `Raise` is shared).
  - **Store:**
    - `Import_OneTransaction_OneEvent_KindImported_IdsAreNew` (event count 1, `Id` null, `SnippetIds` = the new ids,
      none equal to any file id)
    - `Import_SnapshotsReplacedOnce_ContainNewRows`
    - `Import_PersistsStats_ReloadEqualsCache` (`NewStore()` equals the snapshot)
    - `Import_FoldersCreatedInSameTransaction`
    - `Import_AllDuplicates_NoWrite_NoEvent`: install `BEFORE INSERT` triggers on `snippets` and `folders` that
      `RAISE(ABORT)`. The call still succeeds, which proves no insert was attempted. No event; `Assert.Same` on both
      snapshots.
    - `Import_OnlyEmptyNativeFolder_WritesFolder_OneEvent`
    - `Import_EditorRules_SameAsCreate` (the same text, tags and label via `CreateSnippet` and via import give the same
      stored label, custom flag and tags)
    - `Import_FullRowBatch_KeepsSameTextRowsWithDifferentMetadata` (store-level counterpart of the T5 planner tests)
  - **AC6 injected failure:** create `TRIGGER fail_import BEFORE INSERT ON snippets WHEN NEW.label = 'boom' BEGIN
    SELECT RAISE(ABORT, 'injected'); END;` and import a batch whose **third** candidate is labelled `boom`, after two
    good ones and a new folder.
    - `Import_InjectedFailure_RollsBack_NoRows_NoFolders_NoEvent_SnapshotsSame`: `StoreException`; `Assert.Same` on
      both snapshots; `Assert.Empty(Events)`; `NewStore()` has 0 snippets and 0 folders.
    - The same test with a trigger on `folders`.
    - The busy-database variant using `BEGIN EXCLUSIVE` (pattern at `SnippetStoreTests.cs:370-404`).
  - **Failure-path privacy (finding 5,** non-parallel collection with `AppLog` on a temporary directory**):**
    - `Import_InjectedFailure_LogHasEventButNoMessage`. The trigger is `RAISE(ABORT, 'SENTINEL-TEXT-7f3a')` and the
      batch text holds `SENTINEL-BODY-91c2`. **First** assert that `app.log` contains `store_write_failed
      op=import_snippets error=SqliteException sqlite=19`. **Then** assert that neither sentinel appears.
    - `CreateSnippet_InjectedFailure_LogHasNoMessage` (the same, for an existing operation; `Guarded` is shared)
  - **Pipeline (`SnippetImportIntegrationTests`, real fixture files on disk → real `FileStream` →
    `CappedReader.ReadAsync(SnippetFileRules.MaxImportBytes)` → `ImportParser.Parse` → `SnippetImport.Commit` → real
    SQLite):**
    - `Pipeline_EachFixture_CountsMatchIndependentTable_AndBalanced` (§7.1 table, four fixtures)
    - `Pipeline_SameFileTwice_SecondImportsZero_NoEvent`
    - (The success-path log-privacy test lives in T9, where `import_done` is written.)
  - **"No write" proofs:** the raising-trigger technique is kept only for no-write and atomicity proofs:
    `Import_AllDuplicates_NoWrite_NoEvent` and the AC6 tests. "Store never called" is proved by call counting at the
    orchestration boundary (finding 9), in T9: `FakeSnippetStore.ImportCalls == 0`, and `ThrowOnImport` makes any
    call fail the test.
  - **Fake parity:** `FakeSnippetStore_Import_MatchesRealStoreCounts` (Presentation tests, all four shared synthetic
    fixtures). Compare summary/folder names, then full row projections against a freshly reopened real store:
    `(Text, Label, IsLabelCustom, FolderName, ordered ordinal Tags, UsageCount, CreatedAt, UpdatedAt, LastUsedAt)`.
    Exclude reassigned ids. Use a fixed parse time and a real-store clock matching the fake's first atomic import tick,
    so both preserved and fresh timestamps are checked. This oracle must fail if the fake discards imported stats or
    reorders tags.

### T7 — Native export (AC7)
- **Files:** `src/SmartSnippets.Core/Porting/NativeExport.cs`, `NativeJsonContext.cs`;
  `tests/SmartSnippets.Core.Tests/Porting/NativeExportTests.cs`, `RoundTripTests.cs`.
- **Change:** §4.3.6: compact output, and the `TooLarge` result above `MaxImportBytes`.
- **Verify:** `dotnet test … --filter "FullyQualifiedName~NativeExport|FullyQualifiedName~RoundTrip|FullyQualifiedName~ExportImport"`;
  on Windows, `dotnet publish src/SmartSnippets.App -c Release -r win-x64 -o publish` shows no IL warnings from
  `NativeJsonContext`.
- **Tests that die if this breaks:**
  - `Export_EnvelopeShape_MarkerVersionArrays` (parse the output with `JsonDocument` and assert member names and types)
  - `Export_IsCompact` (the output contains no `\n` and no `": "`)
  - `Export_EmptyLibrary_ValidEmptyArrays`
  - `Export_UnfiledFolderNull_NeverUsedLastUsedNull`
  - `Export_DatesAreEpochMs`
  - `Export_NoBom`
  - `Export_LoneSurrogate_WrittenAsReplacementChar` (R6, visible)
  - **`RoundTrip_ExportThenImportIntoEmpty_ReproducesEverything` (A1).** The store is seeded **through the real
    `SnippetStore`** (`CreateFolder`, `CreateSnippet`, `RecordUsage`, plus raw SQL only to set distinct past dates).
    It has:
    - 3 folders, including one empty;
    - snippets covering custom and auto labels, 0/1/many tags, Unfiled, usage 0 and >0, null and set `lastUsedAt`;
    - texts with emoji, CJK, tab, trailing spaces, CRLF, lone LF and lone CR;
    - **identical texts with different metadata:** `hello` in Work, `hello` in Home, `hello` Unfiled with tag `x`,
      and `hello` with a different custom label;
    - **CR-vs-LF variants with different metadata:** `a\rb` labelled `One` and `a\nb` labelled `Two`, both kept.

    Export, then write the bytes to a real file, then `CappedReader` → `Parse` → `Commit` into an empty store.
    **Comparison projection (round-2 R8).** Map each store's `Snippets` (`SnippetView`) to
    `(Text, Label, IsLabelCustom, FolderName, Tags, UsageCount, CreatedAt, UpdatedAt, LastUsedAt)`:
    - `FolderName` comes from `SnippetView.FolderName` (null for Unfiled). **`Id` and `FolderId` are excluded**,
      because new ids are required and folder ids are reassigned in name order.
    - `Tags` are compared as ordered sequences with ordinal element equality.
    - Compare the two projections as **multisets** (so multiplicity counts), using a comparer that applies the tag
      rule.

    Separately, compare the two stores' folder **name** sets, which covers the empty folder.
  - `RoundTrip_FullKeyRepeat_CollapsesToFirst` (D1 known consequence, pinned). The library holds `a\rb` and `a\nb` with
    the same label, folder and tags, plus two rows with identical text, label, folder and tags but different usage.
    Each pair imports as **one** row, the first in id order, and the summary counts the other as a duplicate.
  - `RoundTrip_FolderIdsDiffer_ProjectionStillEqual`: folders are created in the order Work, Home, Empty, so the
    target's ids differ. The projection comparison passes. A deliberate check confirms the source and target `FolderId`
    values do differ, so the test really covers the remapping.
  - `UsageChain_Export_Reimport_Exact` (the export half of T6's chain, round-2 R1/R3). Usage `1000000003` and usage
    `int.MaxValue` are exported, then re-imported into an empty store **exactly**.
  - `RoundTrip_ReimportIntoSameLibrary_ZeroNew_NoEvent`
  - `RoundTrip_ReimportIntoTarget_ZeroNew`
  - `RoundTrip_ParserDetectsOwnOutputAsNative`
  - **Size boundary, end to end (A2 and finding 2):** export → real file → `CappedReader` with the **real** 32 MB
    constant → parse → commit. It must not call `ImportParser` directly on the exported bytes.
    - `ExportImport_ExactlyAtCap_ExportsAndImports`: tune one ASCII padding snippet so that the compact export is
      exactly `MaxImportBytes` bytes (measure, then adjust; each ASCII character adds exactly one byte). Assert export
      succeeds, file length == 33,554,432, and the import succeeds with every row.
    - `ExportImport_OneByteOver_RefusedAtExport_NoFile`: the same library + 1 character → `TooLarge`; no file at the
      path; no temp file left.
    - `ExportImport_EscapingExpansion_RefusedAtExport`: about 6 MB of stored text made of `é` (escaped as `\u00E9`, 6
      bytes) exports to over 32 MB. Assert that `Refusal == TooLarge`, and that this library's **raw** text size is
      below the cap, which shows the check runs on the escaped output.
    - `Export_Oversized_StopsAtBound` (round-2 improvement): a library of about 200 MB of escaped output. `Write`
      returns `TooLarge`, and the bounded stream's peak length is ≤ `MaxImportBytes + 1` buffer, so it stops early.
    - `Export_TooManySnippets_RefusedWithoutSerializing` (`MaxImportEntries + 1` snippets; the bounded stream is never
      written to) and `Export_AtEntryLimit_Exports`.
    - `ExportImport_EscapingNearCap_ImportsBack`: a mixed accented/ASCII library whose escaped export lands just under
      the cap round-trips completely.

### T8 — Porting messages (AC2, AC8, AC9)
- **Files:** `src/SmartSnippets.Presentation/Text/UiText.cs` (new section before `// Tray.`);
  `tests/SmartSnippets.Presentation.Tests/PortingMessagesTests.cs`.
- **Change:** §4.3.7, verbatim.
- **Verify:** `dotnet test tests/SmartSnippets.Presentation.Tests --filter PortingMessages`.
- **Tests that die if this breaks:**
  - `EveryRefusal_HasMessage_EndingNothingWasImported` (iterates `Enum.GetValues<ImportRefusal>()`)
  - `RefusalMessages_PairwiseDistinct` (for each format, including the two `UnsupportedVersion` variants and
    `MissingEntries`)
  - `UnsupportedVersion_NamesFormatFoundAndSupported`
  - `MissingEntries_NamesListPerFormat`
  - `Summary_FourLines_Pluralization` (0, 1, 2 for each line)
  - `Summary_FormatNames`
  - `ExportComplete_Pluralization`
  - `ButtonNames_AreDistinctFromNewSnippet`

### T9 — View-model, dialog contract, page wiring (AC2, AC3, AC8, AC9 enabler)
- **Files:**
  - `src/SmartSnippets.Presentation/ViewModels/SnippetPortingViewModel.cs` (new)
  - `SnippetsPageViewModel.cs` (constructor parameter, `Porting` property)
  - `Services/IDialogService.cs` (`ShowInfoAsync`)
  - `tests/SmartSnippets.Presentation.Tests/Fakes/ScriptedDialogService.cs` (`Infos` list, optional `InfoGate`)
  - `tests/SmartSnippets.Presentation.Tests/Fakes/FakeSnippetFileService.cs`. It provides:
    - scripted paths;
    - a configurable read stream: a `MemoryStream`, a stream that throws on the first `ReadAsync`, one that throws
      after N bytes, or one whose reads **suspend** (`await Task.Delay(10).ConfigureAwait(false)`, then complete on a
      thread-pool thread);
    - picker tasks that can likewise suspend;
    - a throwing open;
    - recorded writes, and a configurable throw on write.
  - `tests/SmartSnippets.Presentation.Tests/Fakes/QueueSynchronizationContext.cs`. This is a copy of the private
    `QueueContext` pattern (`InsertionServiceTests.cs:254-257`) plus a `RunOnOwnerThread(Func<Task>)` helper that pumps
    it, modelled on `:230-252`. `InsertionServiceTests` is not edited.
  - `tests/SmartSnippets.Presentation.Tests/SnippetPortingViewModelTests.cs`
  - `SnippetPortingIntegrationTests.cs`, `SnippetPortingBudgetTests.cs`
- **Change:**
  - The §4.4 flows, with messages from T8 and log events from §4.3.8.
  - New `src/SmartSnippets.Presentation/ViewModels/PortingWorkers.cs` (§4.3.2). Parse via
    `await _workers.ParseAsync(bytes, now)` and export serialization via `await _workers.ExportAsync(f, s, now)`. There
    is no `ConfigureAwait(false)` anywhere in the view-model, so `SnippetImport.Commit` runs on the UI context.
  - **Single busy owner (round-2 R6).**
    - `ImportAsync` and `ExportAsync` each return immediately (`null` / `false`) if `IsBusy`.
    - Otherwise they set `IsBusy = true` before their first `await`, and clear it in their own `finally`.
    - They catch every non-cancellation exception **inside** their `try`, classify it with the §4.4 stage tables, and
      **await** the error dialog there.
    - So `IsBusy` stays true until every dialog has closed, on every path. The relay commands are one-line calls
      with no handling.
  - The view-model records a `stage` field (`before_commit`, `in_commit`, `after_commit`; export: `before_write`,
    `in_write`, `after_write`). Messages follow the tables: `ImportUnexpectedFailure` only for `before_commit`,
    `ImportOutcomeUnknown` for a non-store exception `in_commit`, and no dialog for `after_commit`. Export uses
    `ExportUnexpectedFailure`, `ExportWriteFailed`, `ExportOutcomeUnknown` or none.
  - Export handles `NativeExportResult.Refusal`: `TooManySnippets` → `ExportTooManySnippets(count)`, `TooManyEntries` →
    `ExportTooManyEntries(snippets, folders)`, and `TooLarge` → `ExportTooLarge`. All log `export_refused` and none calls
    `WriteAtomicallyAsync`.
  - Import handles `TooManyEntries` like every other parser refusal.
  - `ShowInfoAsync` is added to `IDialogService`. In the same commit, `src/SmartSnippets.App/Services/DialogService.cs`
    gets the plain version (same shape as `ShowErrorAsync`, `:53-62`), so the App still compiles. T10 adds the text
    size. The App file cannot be compiled on Linux: check it by eye against `ShowErrorAsync`, and T10's Windows build
    confirms it.
- **Verify:** `dotnet test tests/SmartSnippets.Presentation.Tests --filter "FullyQualifiedName~Porting"`.
- **Tests that die if this breaks:**
  - **Cancel and gates.** Every refusal test runs with `FakeSnippetStore.ThrowOnImport = true` and asserts
    `ImportCalls == 0` (finding 9: call counting, not "no rows written"). Each also asserts the error dialog text,
    that no info dialog appeared, and that `IsBusy` is false afterwards.
    - `Import_Cancelled_NoRead_NoStoreCall_NoDialog_LogsCancelled`
    - `Import_NotJsonPath_Refused_NoOpen`
    - `Import_OpenThrowsIOException_Unreadable`
    - `Import_OpenThrowsUnauthorized_Unreadable`
    - `Import_ReadThrowsOnFirstRead_Unreadable` and `Import_ReadThrowsAfterSomeBytes_Unreadable` (finding 6)
    - `Import_TooLarge_Refused` (a stream of `MaxImportBytes + 1` bytes)
    - `Import_NotUtf8_Refused`
    - `Import_InvalidJson_Refused`
    - `Import_UnknownFormat_Refused`
    - `Import_UnsupportedVersion_Refused_MessageNamesVersion`
    - `Import_MissingEntries_Refused`
    - `Import_TooManyEntries_Refused_MessageNamesCountAndLimit`
  - **Success and failure:**
    - `Import_Success_ShowsInfoWithSummary_TitleComplete`
    - `Import_ZeroImported_TitleNothingImported`
    - `Import_StoreException_ShowsStoreFailed_NoInfo`
    - **Failure stages (finding 4):**
      - `Import_UnexpectedReadException_BeforeCommit_NothingImportedMessage`. The fake read stream throws
        `InvalidDataException`, which `CappedReader` propagates by contract (§4.3.2, T1
        `ReadThrowsInvalidData_Propagates`). The test asserts `ImportUnexpectedFailure`, `ImportCalls == 0`, and
        `ImportAsync` returning without throwing.
      - `Import_ParseWorkerThrows_BeforeCommit_NothingImportedMessage` (the injected `PortingWorkers.ParseAsync`
        faults)
      - `Import_StoreThrowsNonStoreException_InCommit_OutcomeUnknownMessage_NeverNothingImported` (the fake store
        throws `InvalidOperationException` from `ImportSnippets`)
      - `Import_SummaryDialogThrows_AfterCommit_NoErrorDialog_Logged`
      - `Import_SubscriberThrows_SummaryStillShown` (fake store with D9 isolation, plus a throwing extra subscriber)
  - **Busy, single owner (round-2 R6).** For each test: while the gated dialog is open, both commands have
    `CanExecute == false`, and a second `ImportAsync()` and `ExportAsync()` each return at once with no picker call
    and no stage change. After the dialog is dismissed, `IsBusy` is false and both commands are enabled.
    - `Import_Busy_DuringSummaryDialog` (`InfoGate`)
    - `Import_Busy_DuringRefusalDialog` (`ErrorGate`)
    - `Import_Busy_DuringUnexpectedErrorDialog` (`ErrorGate`, with the `InvalidDataException` read)
    - `Import_Busy_DuringStoreFailureDialog` (`ErrorGate`)
    - `Export_Busy_DuringUnexpectedErrorDialog` and `Export_Busy_DuringSuccessDialog`
  - **Library changes while parsing (round-2 improvement: the snapshot is taken at commit start).** Each test uses a
    `PortingWorkers.ParseAsync` seam that returns a `TaskCompletionSource` task. While that task is pending, the test
    changes the saved library on the owner thread, then completes the parse:
    - `Import_MatchingSnippetCreatedDuringParse_IsSkippedAsDuplicate`
    - `Import_MatchingSnippetDeletedDuringParse_IsImported`
    - `Import_FolderRenamedDuringParse_ResolvesToCurrentFolder` (the file's folder name now matches the renamed folder
      → no new folder)
    - `Import_FolderCreatedDuringParse_Reused`

    Each asserts balanced counts and exactly one event (or none when nothing is written).
  - **Page:**
    - `Import_WhileEditorDirty_EditorKeepsEdits_NewFolderInChoices`
    - `Page_FilesNull_CommandsDisabled`
    - `Page_PortingSharesStoreAndDialogs` (constructor wiring)
  - **Export:**
    - `Export_Cancelled_NoWrite`
    - `Export_NotJson_Refused_NoWrite`
    - `Export_Success_WritesNativeBytes_ShowsCountsAndFileName`
    - `Export_WriteThrows_ShowsWriteFailed`
    - `Export_TooLarge_Refused_NoWriteCall`
    - `Export_TooManySnippets_Refused_NoWriteCall`
    - `Export_UnexpectedSerializeException_BeforeWrite_NoFileWrittenMessage` (the injected `PortingWorkers.ExportAsync`
      faults)
    - `Export_NonIoWriteException_InWrite_OutcomeUnknownMessage`
    - `Export_SuccessDialogThrows_AfterWrite_NoErrorDialog_Logged`
    - `Export_SuggestedBaseName_HasDateNoExtension`
  - **Logs:** `Logs_SuccessAndFailure_NoPathNoFileNameNoText`. Sentinels go in the path, file name, text, label, folder
    and tag. For a success and for a store failure, **first** assert that `import_done` / `import_failed` is present,
    **then** that no sentinel appears.
  - **UI-thread hop (finding 10; round-2 R5).** Each test runs under `QueueSynchronizationContext.RunOnOwnerThread`.
    - The read and the picker **suspend** (`Task.Delay(10).ConfigureAwait(false)`).
    - **The parse is held deterministically.** The injected `PortingWorkers.ParseAsync` returns a `TaskCompletionSource`
      task that is **not complete** when the view-model awaits it. The test then completes it **from a thread-pool
      thread** (`Task.Run(() => tcs.SetResult(realParseResult))`), only after the owner has yielded (the test waits on
      a "parse requested" signal from the seam).
    - Record `Environment.CurrentManagedThreadId` into captured fields, in `FakeSnippetStore.OnImport` and in a
      plain recording `Changed` subscriber. Assert them **after** `ImportAsync` completes, outside every handler, and
      check both equal the owner thread.
    - `Import_StoreCallAndChanged_OnOwnerThread_AfterSuspendedReadAndHeldParse`
    - `Import_ParseCompletesOnWorker_StillCommitsOnOwner` (the same, but the `TaskCompletionSource` is created with
      `RunContinuationsAsynchronously` off, so an inline continuation would run on the worker unless the view-model
      captured the context)
    - `Export_SerializeHeld_WriteAndDialogOnOwner` (the same technique on `PortingWorkers.ExportAsync`)

    `ConfigureAwait(false)` on the read, the parse or the export await fails these tests deterministically.
    - `PortingWorkers_Create_RunsOffCallerThread`: proves production uses a worker.
      `PortingWorkers.Create(onWorkerThread)` (§4.3.2; `Default` is `Create(null)`) invokes the callback inside its
      `Task.Run` lambdas. Called from the owner thread, the recorded thread id differs from the owner's for both
      `ParseAsync` and `ExportAsync`.
  - **Integration** (real `SnippetStore` on temporary SQLite, `FakeSnippetFileService` serving fixture bytes through
    suspending reads, under `RunOnOwnerThread`). Refresh tests **query imported content**, not just counts or order,
    which kills a stale search cache:
    - `Integration_ImportTermFlow_ListFindsImportedTextFolderAndTag`: `SnippetListViewModel.Query` set to a fixture
      text sentinel → the row is found. Selecting the imported folder in Browse → it lists the expected rows. Querying
      a fixture tag → found.
    - `Integration_ImportTermFlow_PopupSearchAndSortsReflectImport`: a `PopupViewModel` built as in
      `PopupViewModelIntegrationTests.cs`. Its query for an imported tag and an imported text finds them.
      `SetSort(LastUsed)` → §7.1 order; `SetSort(UsageCount)` → §7.1 order (AC4).
    - `Integration_ImportNative_FoldersPageShowsNewFolderWithCount`: the new folder's name row exists with the right
      count, and the empty native folder is shown with 0.
    - `Integration_InjectedFailure_NoInfoDialog_LibraryUnchanged`
    - `Integration_ThrowingSubscriber_RowsAcknowledged_OtherViewsRefreshed` (real store with D9; a throwing subscriber
      is attached **before** the list view-model; the list still finds the imported text; the summary dialog shows)
  - **Budget (R2, D11; `SnippetPortingBudgetTests`, `[Trait("Category","Budget")]`).** A real store with
    `SnippetListViewModel`, `PopupViewModel`, `FoldersPageViewModel` and a `SnippetEditorViewModel` subscribed, a
    pre-seeded library of 20,000 snippets, and `AppLog` on a temporary directory:
    - `Budget_AtLimit_Dense_CommitUnderThreshold`: `min(MaxImportEntries, what fits in 32 MB)` tiny distinct entries.
    - `Budget_AtLimit_FolderDense_CommitUnderThreshold`: the same count, with one new folder per entry.
    - `Budget_TypicalAtCap_CommitUnderThreshold`: about 32 MB of 1 KB texts with tags and folders, within the limit.
    - `Budget_Calibrate` (`[Trait("Category","Calibration")]`, run only by T10): the §4.3.9 measurement table, printed
      as a parsable line per size.

    Each budget test times `SnippetImport.Commit` with a `Stopwatch`, then **proves the work was done** (round-2 R7):
    - `summary.Imported` equals the expected count and `IsBalanced`;
    - a fresh `NewStore()` row count equals 20,000 + the expected count;
    - `SnippetListViewModel.Query` set to the last imported text finds exactly one row;
    - for folder-dense, the folders page row count equals the expected folder count + 1 (Unfiled);
    - `app.log` contains **no** `store_subscriber_failed`.

    Only then does it assert the time against the threshold. The threshold is 2,000 ms in Release, or the
    floor-measured value if §4.3.9's floor rule applied. Under `#if DEBUG` it is 10,000 ms so ordinary test runs cannot
    flake. The **binding** run is the Release CI step added in T10.

### T10 — App: pickers, file I/O, dialog, buttons, wiring (AC9, AC10)
- **Files:**
  - **new:** `src/SmartSnippets.App/Services/SnippetFileService.cs`
  - **change:** `Services/DialogService.cs`, `Pages/SnippetsPage.xaml`, `AppHost.cs`
  - **change:** `.github/workflows/ci.yml` (round-2 R7)
  - **change (calibration):** `src/SmartSnippets.Core/Porting/SnippetFileRules.cs` (`MaxImportEntries`) and
    `tests/SmartSnippets.Core.Tests/Porting/SnippetFileRulesTests.cs` (`MaxImportEntries_IsCalibratedValue`)
- **Change:**
  - **CI budget step (`ci.yml`, `windows` job), after "Presentation tests" and before the AOT publish (sequential, as
    AGENTS.md requires):**

    ```yaml
          - name: Import budget (Release)
            run: dotnet test tests/SmartSnippets.Presentation.Tests -c Release --filter "Category=Budget"
    ```

    This is the binding 2,000 ms gate on the `windows-latest` runner. The existing Debug steps are unchanged.
  - **Calibration (D11, §4.3.9).** On the branch, run `workflow_dispatch` once with this temporary extra step.
    The opt-in environment variable is mandatory, and detailed output is required to expose the measurement table:

    ```yaml
          - name: Import budget calibration (temporary)
            shell: pwsh
            env:
              SMARTSNIPPETS_CALIBRATE: '1'
            run: |
              dotnet test tests/SmartSnippets.Presentation.Tests -c Release --filter "Category=Calibration" --logger "console;verbosity=detailed" 2>&1 | Tee-Object -FilePath calibration.log
              if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
              $measurements = @(Select-String -Path calibration.log -Pattern '^\s*BUDGET kind=')
              if ($measurements.Count -ne 18) { throw 'Expected 18 calibration measurement lines; a passing no-op is not evidence' }
              foreach ($kind in @('dense', 'folders', 'typical')) {
                if (-not ($measurements | Where-Object { $_.Line -match "kind=$kind entries=" })) { throw "Missing calibration case $kind" }
              }
    ```

    Read all 18 measured rows (including byte-cap cases), then set `MaxImportEntries` and its pinning test by §4.3.9,
    and **remove the temporary step**. Run `workflow_dispatch` again: the budget step must now pass. Record the table,
    chosen limit and run links in §10. The persistent workflow currently contains only the Release Budget step, not
    a Calibration step; do not add the expensive temporary measurement to ordinary CI runs.
  - **`SnippetFileService(Func<WindowId?> owner)`:**
    - `Microsoft.Windows.Storage.Pickers.FileOpenPicker(id)` with `FileTypeFilter = { ".json" }`,
      `CommitButtonText = "Import"`.
    - `FileSavePicker(id)` with `SuggestedFileName = baseName`, `DefaultFileExtension = ".json"`,
      `FileTypeChoices = { ["JSON"] = [".json"] }`.
    - Return `result?.Path`; a null owner id → return null and log `picker_no_owner`.
    - `OpenRead` → `new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920,
      FileOptions.Asynchronous | FileOptions.SequentialScan)`.
    - `WriteAtomicallyAsync` → `AtomicFileWriter.WriteAsync(path, content)` (Core, T1; tested there for every failure
      stage). App adds no file-writing logic of its own.
  - **`DialogService`:**
    - New optional constructor parameter `ITextSizeService? textSize = null`.
    - `ShowInfoAsync` uses the existing gate (`ShowAsync`) with `CloseButtonText = "OK"`. Its body is a `TextBlock`
      with `TextWrapping.Wrap`, `IsTextSelectionEnabled = true` and `FontSize = textSize?.FontSize` when set.
    - Existing dialogs are unchanged.
  - **`SnippetsPage.xaml` toolbar grid (`:162-185`):**
    - Two more `Auto` columns.
    - Each button: `Command="{x:Bind ViewModel.Porting.ImportCommand, Mode=OneWay}"` (and Export),
      `ToolTipService.ToolTip` and `automation:AutomationProperties.Name` from the `UiText` values (literal strings
      equal to `UiText`, as the page already does), and a `FontIcon` glyph per §4.5.
    - Tab order follows column order.
  - **`AppHost`:**
    - `Dialogs = new DialogService(..., ..., textSize)` (`:158`; the popup's `DialogService` at `:212` stays without
      it).
    - `new SnippetsPageViewModel(Store, Dialogs, new ClipboardService(), files: new SnippetFileService(() =>
      _mainWindow?.AppWindow.Id))` (`:171`).
    - Log `porting_ready` once in `Compose`.
- **Verify:**
  - `dotnet build` on Windows → 0 warnings.
  - CI `workflow_dispatch` on the branch: AOT publish, 0 IL2xxx/IL3xxx, `SmartSnippets.App.exe` present, no
    `SmartSnippets.App.dll`.
  - The CI "Import budget (Release)" step passes on `workflow_dispatch` after calibration. The three commit times and
    the calibrated `MaxImportEntries` are recorded in §10.
  - Runbook §7.3 steps 1–13, including 9a (wide mode at 880 px: search box ≥ 120 px, else the D7 fallback).
  - Check every `x:Bind` path against the view-model (`ViewModel.Porting.ImportCommand`, `ExportCommand` exist and are
    public).
- **Test that dies if this breaks:** not unit-testable (WinUI). Guarded by:
  - the CI Windows compile (a wrong `x:Bind` path fails XAML compile);
  - runbook step 1, with `porting_ready` and `import_done` in `app.log`;
  - the T9 `Page_PortingSharesStoreAndDialogs` test for the view-model side.

### T11 — Requirement and master-plan update (scope change)
- **Files:** `docs/requirements/001-prd.md`, `docs/plan/001-master-smart-snippets-v1.md`. Rebase onto `main` first
  (R12).
- **Change:**
  - **PRD §4:**
    - Delete `- Import/export.` from "Excluded from the initial release" (`:303`).
    - Add to "Included" (`:284-293`): `- Import of snippet files from TermFlow, Rephlo and InkSpoke, and export/import of
      the whole library in Smart Snippets' own JSON format (added 2026-10-28, plan 010).`
  - **PRD §3:** add `### 3.11 Import and export` (after §3.10): six bullets summarizing §2 Behaviour, pointing to plan
    010 §4.3 for formats and messages.
  - **Master §2** (`:29`): remove `import/export` from the non-goals and add `(import/export moved in scope 2026-10-28,
    plan 010)`.
  - **Master §2 JSON constraint** (`:47-48`, D2 and review): replace "any JSON uses source-generated contexts only" with
    "typed JSON serialization uses source-generated contexts only; read-only DOM parsing with `JsonDocument` is allowed
    (no reflection); no reflection-based `JsonSerializer` overloads".
  - **Master §4 Change notification** (`:115-116`): append "Subscribers are isolated: a throwing subscriber is logged and
    does not stop the others or the caller (plan 010 D9)."
  - **Master §5 table:** add the row
    `| 010 | [010-import-export-snippets.md](010-import-export-snippets.md) | Import TermFlow/Rephlo/InkSpoke/native JSON; export the library | 003, 005 | <status> |`.
  - **Master §9 log:** one row with the date, 010, the event and the evidence.
- **Verify (finding 12):**
  - `grep -n "^- Import/export\.$" docs/requirements/001-prd.md` → **no output** (the exclusion is gone).
  - `grep -n "Import of snippet files from TermFlow, Rephlo and InkSpoke" docs/requirements/001-prd.md` → exactly one
    line, inside §4 "Included".
  - `grep -n "^### 3.11 Import and export" docs/requirements/001-prd.md` → one line.
  - `grep -n "import/export moved in scope" docs/plan/001-master-smart-snippets-v1.md` → one line in §2.
  - `grep -n "read-only DOM parsing" docs/plan/001-master-smart-snippets-v1.md` → one line in §2.
  - `grep -n "010-import-export-snippets.md" docs/plan/001-master-smart-snippets-v1.md` → the §5 row (and the §9 row
    if it links the plan).
- **Test that dies if this breaks:** n/a (documents). The T12 audit re-checks both greps.

### T12 — Wiring audit (L)
- **New methods → callers (grep `src/`):**
  - `ISnippetStore.ImportSnippets` ← `SnippetImport.Commit` only.
  - `SnippetImport.Commit` ← `SnippetPortingViewModel.ImportAsync` only.
  - `ImportParser.Parse` ← `PortingWorkers.Create` only (inside its `Task.Run`), reached by
    `SnippetPortingViewModel.ImportAsync` through `PortingWorkers.ParseAsync`. `NativeExport.Write` is likewise
    reached only through `PortingWorkers.ExportAsync`.
  - `PortingWorkers.Default` is used by `SnippetPortingViewModel` when no workers are injected. Production (`AppHost`)
    injects none.
  - `SnippetFileRules.MaxImportEntries` ← `ImportParser` (step 5) and `NativeExport.Write` (and `UiText` messages).
  - `CappedReader.ReadAsync` ← `ImportAsync` only.
  - `AtomicFileWriter.WriteAsync` ← `SnippetFileService.WriteAtomicallyAsync` only.
  - `ImportPlanner.Plan` ← `SnippetStore.ImportSnippets` and `FakeSnippetStore` only.
  - `SnippetRepository.InsertImported` ← `SnippetStore.ImportSnippets` and `SnippetRepository.Insert`.
  - `IDialogService.ShowInfoAsync` ← `SnippetPortingViewModel` only.
  - `ISnippetFileService` members ← `SnippetPortingViewModel` only.
  - `SnippetFileService` constructed only in `AppHost.Compose`.
  - `SnippetsPageViewModel.Porting` ← `SnippetsPage.xaml` `x:Bind` (two commands).
- **New event kind → consumers:** `StoreChangeKind.Imported` is raised only by `ImportSnippets`; all four subscribers
  in §3 handle it (T9 integration tests prove list, popup and folders).
- **Parallel paths:**
  - `INSERT INTO snippets` appears once in `src/` (`InsertImported`).
  - `usage_count` writers: the `RecordUsage` SQL contains `MIN(`. `InsertImported` takes a pre-capped value. No other
    `usage_count =` in `src/`.
  - `Changed?.Invoke` no longer appears in `SnippetStore.cs`: `Raise` iterates the invocation list.
  - **File I/O, scoped to this feature (nonblocking review item):** in the files this plan creates or changes, the only
    file APIs are in `SnippetFileService.cs` (`new FileStream` for read) and `AtomicFileWriter.cs`
    (`File.Move`, `File.Delete`, `new FileStream` for the temp file). The pre-existing callers listed in §3
    (`MainWindow.xaml.cs:30`, `Migrator.cs:75`, `AppLog.cs:78`) are expected and are not part of this audit. This is not
    a whole-tree invariant.
  - No `JsonSerializer` call without a `JsonTypeInfo`/context argument.
  - No `System.Text.Json` in Presentation or App.
- **Privacy:**
  - Every new or changed `AppLog` call passes only enums, counts, bytes, ms, and exception type or HResult.
  - grep the new and changed files **and `SnippetStore.cs`** (the reused `Guarded`, D10) for `AppLog.Error(` with a
    non-null exception argument: none on the import or export path. (`AppLog.Error`'s own behaviour is unchanged.)
  - The T6 failure-path and T9 success/failure log-sentinel tests pass. Each asserts presence before absence.
- **Fixtures:** `grep -rn "D:\\\\temp\|export-ref" src tests` → nothing (R7).
- **Docs:** the T11 greps.
- **Verify (runtime):** during runbook step 1, `app.log` shows `porting_ready` at start-up, then `import_done
  format=TermFlow entries=13 imported=13 duplicates=0 unsupported=0 rejected=0 …`.
- **Update:** this plan's status and §10, and master §5 status.

### AC traceability

| AC | Tasks | Evidence |
|---|---|---|
| AC1 | T3, T4, T6 | `*_Fixture_PerEntryOutcomes`, `Pipeline_EachFixture_CountsMatchIndependentTable_AndBalanced`; runbook steps 1–3 against the real files and the §3 table |
| AC2 | T2, T8, T9 | `Detect_*`, `Version_*` incl. `Version_CheckedBeforeEntries`, `Import_UnknownFormat_Refused` / `Import_UnsupportedVersion_…` with `ImportCalls == 0`, `RefusalMessages_PairwiseDistinct`; runbook 6 |
| AC3 (amended A1) | T5, T6, T0 | `Duplicate_*`, `NotDuplicate_*`, `FullRow_*`, `TextOnly_…`, `Counts_…`, `IsBalanced_…`, `Pipeline_EachFixture_…AndBalanced`, `Pipeline_SameFileTwice_…`; runbook 4–5 |
| AC4 | T3, T6, T9 | `TermFlow_Fixture_StatsKept`, `ValueRules` date and usage tests, `Import_PersistsStats_ReloadEqualsCache`, `UsageImported_Paste_Sort_Reopen`, `UsageAtIntMax_Paste_Saturates_Reopen`, `UsageChain_Export_Reimport_Exact`, `Integration_ImportTermFlow_PopupSearchAndSortsReflectImport`; runbook 7 |
| AC5 | T5, T6, T4 | `Folder_*`, `*_CreatesNoFolder`, `Import_FoldersCreatedInSameTransaction`, `InkSpoke_*GroupId*_Unfiled`; runbook 4 |
| AC6 | T6, T9 | `Import_InjectedFailure_RollsBack_…` (snippets trigger, folders trigger, busy DB), `Integration_InjectedFailure_…`; post-commit: `Import_SubscriberThrows_CommitReturned_…`, `Import_StoreThrowsNonStoreException_InCommit_OutcomeUnknownMessage_…` |
| AC7 (amended A1, A2) | T7 | `RoundTrip_ExportThenImportIntoEmpty_ReproducesEverything` (same-text and CR/LF rows with different metadata), `RoundTrip_FullKeyRepeat_CollapsesToFirst`, `RoundTrip_ReimportIntoSameLibrary_ZeroNew_NoEvent`, `ExportImport_ExactlyAtCap_…`, `ExportImport_OneByteOver_…`, `ExportImport_Escaping*`; runbook 8 |
| AC8 (amended A2) | T1, T2, T7, T8, T9 | `MaxImportBytes_Is32MiB`, `CapPlusOne_TooLarge`, `TooLarge_StopsReadingAtCapPlusOne`, `LengthLies_StillCapped`, `ReadThrows*_Unreadable`, `*Bom*`, `InvalidUtf8InsideString_NotUtf8`, `InvalidJson_*`, `HasJsonExtension`, `Import_*_Refused`, `Export_TooLarge_…`, D11: `Entries_*Limit*`, `Import_TooManyEntries_…`, `Export_TooManySnippets_…`, `Budget_AtLimit_*`; runbook 6 |
| AC9 | T10, T8 | `ButtonNames_…`; runbook 9a/9b–11 (wide 320 px toolbar, keyboard only, Narrator names, text size 32) |
| AC10 | all; T10 | `dotnet build` 0 warnings; CI AOT gate 0 IL warnings; both test projects green on Linux and Windows |

## 7. Test strategy

- **Unit (Core, Linux and Windows):** about 120 tests across `Porting/*` (§6 T0–T7), all on synthetic fixtures or
  inline JSON. Covered: every refusal kind, every mapping row, every value rule, the planner, the store transaction, and
  the export envelope. **Deliberately not covered by unit tests:** the pickers and `FileStream` options in App (runbook
  only).
- **Unit (Presentation):** about 35 tests (T8, T9) against `FakeSnippetStore` (now implementing `ImportSnippets` through
  the real `ImportPlanner`), `ScriptedDialogService` and `FakeSnippetFileService`.
- **Integration:** one per user-facing flow, real `SnippetStore` on temporary SQLite, faking only the file and dialog
  boundary:
  - Import each format: `SnippetImportIntegrationTests.Pipeline_EachFixture_…` (Core, real files through `FileStream`
    and `CappedReader`).
  - Import → list/popup/folders refresh, querying imported content: `SnippetPortingIntegrationTests.Integration_*`
    (Presentation, owner-thread context with suspending awaits).
  - Export → import round trip and the size boundary: `RoundTripTests`, `ExportImport_*` (Core, through a real file).
  - Failure → nothing changes: `Import_InjectedFailure_…` and `Integration_InjectedFailure_…`.
  - Commit → a view throws → still acknowledged: `SnippetStoreObserverTests`,
    `Integration_ThrowingSubscriber_RowsAcknowledged_OtherViewsRefreshed`.
- **Performance gate:** `SnippetPortingBudgetTests`, with the binding run in Release on Windows (R2).

### 7.1 Synthetic fixtures (committed; same keys and types as the real files, invented content)

All under `tests/SmartSnippets.Core.Tests/Porting/Fixtures/`. Dates are around `1700000000000` (2023), and test "now"
is `2026-01-01T00:00:00Z`, so no kept date is clamped unless the entry says so.

**`termflow-v1.json`** — `{"version":1,"exportedAt":1700009999999,"snippets":[…10…]}`:

| # | Entry | Outcome |
|---|---|---|
| 1 | `id:"snip-a"`, `text:"Alpha line"`, `label:"Alpha"`, `folder:"Work"`, `tags:["one","two"]`, `createdAt:1700000000000`, `updatedAt:1700000500000`, `lastUsedAt:1700000900000`, `usageCount:7` | imported; custom label; stats exact |
| 2 | `text:"Bravo\nsecond line"`, `createdAt:1700000100000` | imported; label auto "Bravo"; Unfiled; updated = created; lastUsed null; usage 0 |
| 3 | `text:"Charlie"`, `folder:"work"`, `createdAt:1700000200000`, `lastUsedAt:1700000800000`, `usageCount:2` | imported into the same new folder as #1 |
| 4 | `text:"Alpha line"`, `folder:"Zulu"`, `createdAt:1700000300000` | **duplicate** (in-file, of #1: TermFlow is text-only, so the different folder does not matter); **no "Zulu" folder is created** |
| 5 | `text:"Delta"`, `tags:["Beta, Gamma","beta"]`, `createdAt:1700000400000` | imported; tags `[Beta, Gamma]`; label auto "Beta" |
| 6 | `text:"Echo"`, `createdAt:1e400`, `updatedAt:1.5`, `lastUsedAt:"yesterday"`, `usageCount:-3` | imported; created = now; updated = created; lastUsed null; usage 0 |
| 7 | `text:"Foxtrot"`, `tags:"single string"` | **rejected** `WrongFieldType` |
| 8 | `text:"   "` | **rejected** `TextBlank` |
| 9 | `42` | **rejected** `NotAnObject` |
| 10 | `text:"Golf"`, `folder:"  "`, `pinned:true`, `createdAt:1700000600000`, `lastUsedAt:4102444800000` | imported; Unfiled; lastUsed clamped to now; extra field ignored |

Expected into an empty library: **entries 10, imported 6, duplicates 1, unsupported 0, rejected 3, folders created 1**.
Labels: Alpha (custom), Bravo, Charlie, Beta (Delta's text; first tag), Echo, Golf. Popup order (`SnippetSort`, ties by
label then id): **LastUsed** → Golf (clamped to now), Alpha, Charlie, then never-used by label: Beta, Bravo, Echo.
**UsageCount** → Alpha (7), Charlie (2), then the zeros by label: Beta, Bravo, Echo, Golf.

**`rephlo-1.3.json`** — `{"version":"1.3","exportedAt":"2023-11-14T22:13:20Z","commands":[…6…]}`:

| # | Entry | Outcome |
|---|---|---|
| 1 | `name:"Refactor"`, `instruction:"Refactor the code.\r\nKeep tests.\nDone."`, `groupName:"Developing"`, `fixedContentAppend:"APPEND-SENTINEL"`, `executionMode:"Template"`, `isDefault:false`, `hideFromContextMenu:false`, `linkedSpaceNames:[]`, `isArchived:false` | imported; text exactly the instruction |
| 2 | `name:"Review"`, `instruction:"Review this."`, `isArchived:false` (no group) | imported; Unfiled |
| 3 | `name:"Old"`, `isArchived:true` (no instruction) | **unsupported** `Archived` |
| 4 | `name:"Broken"`, `isArchived:false` (no instruction) | **rejected** `TextMissing` |
| 5 | `name:""`, `instruction:"Explain."`, `groupName:"developing"` | imported; label auto "Explain."; folder matches #1's |
| 6 | `name:"Refactor copy"`, `instruction:"Refactor the code.\nKeep tests.\r\nDone."` | **duplicate** of #1 (line endings only) |

Expected: **entries 6, imported 3, duplicates 1, unsupported 1, rejected 1, folders created 1**.

**`inkspoke-v2.json`** — `SchemaVersion:2`, `ExportedAt`, `SourcePlatform:"Windows"`, `Groups:[{Id:"g1",Name:"Prompts"},
{Id:"g2",Name:"Secure"},{Id:"g3",Name:""}]` (each with `IsEnabled`, `SortOrder`), `Favorites:[…1…]`,
`Vocabulary:[…2…]`, `Mappings:[…9…]`:

| # | Mapping | Outcome |
|---|---|---|
| 1 | SendKeys, `GroupId:"g1"`, `SpokenPhrase:"say hello"`, params `{"Text":"Hello there","SendEnterAfter":true,"IsSecret":false,"Nonce":null}` | imported; label "say hello"; folder Prompts |
| 2 | SendKeys, no `GroupId`, `SpokenPhrase:"insert last"`, params `{"text":"{last}"}` | imported; Unfiled; text `{last}` literal |
| 3 | SendKeys, `IsParameterized:true`, params `{"Text":"{text}","SendEnterAfter":true}` | **unsupported** `Parameterized` |
| 4 | SendKeys, `GroupId:"g2"`, params `{"Text":"c2VjcmV0","IsSecret":true,"Nonce":"bm9uY2U="}` | **unsupported** `Secret` |
| 5 | OpenUrl, params `{"Url":"https://example.invalid"}` | **unsupported** `NotSendKeys` |
| 6 | SendKeys, no `GroupId`, params `""` | **unsupported** `UnparsableParams` |
| 7 | SendKeys, `GroupId:"g1"`, params `{"Text":42}` | **rejected** `WrongFieldType` |
| 8 | SendKeys, `GroupId:"missing"`, `SpokenPhrase:"sign off"`, params `{"Text":"Thanks,\nTam"}` | imported; Unfiled |
| 9 | SendKeys, `GroupId:"g3"`, `SpokenPhrase:""`, params `{"Text":"Line one\nLine two"}` | imported; Unfiled; label auto "Line one" |

Expected: **entries 9, imported 4, duplicates 0, unsupported 4, rejected 1, folders created 1** ("Secure" is not created:
only a skipped entry uses it).

**`native-v1.json`** — `format:"smart-snippets"`, `version:1`, `folders:[{name:"Work"},{name:"Home"},{name:"Empty
folder"},{nope:1}]`, `snippets:[…7…]`:

| # | Entry | Outcome (FullRow dedupe, A1) |
|---|---|---|
| 1 | `text:"hello"`, `label:"Greeting"`, `isLabelCustom:true`, `folder:"Work"`, `tags:["mail"]`, full stats, `usageCount:4` | imported |
| 2 | `text:"hello"`, `label:"Greeting"`, `isLabelCustom:true`, `folder:"Home"`, `tags:["mail"]`, usage 1 | imported (same text, different folder) |
| 3 | `text:"hello"`, `label:"Hi"`, `isLabelCustom:true`, `folder:"Work"`, `tags:["mail"]` | imported (different label) |
| 4 | `text:"hello"`, `label:"Greeting"`, `isLabelCustom:true`, `folder:"work"`, `tags:["mail"]`, `usageCount:9` | **duplicate** of #1 (folder key equal case-insensitively; stats are not in the key) |
| 5 | `text:"a\rb"`, `label:"One"`, Unfiled, `tags:[]` | imported |
| 6 | `text:"a\nb"`, `label:"Two"`, Unfiled, `tags:[]` | imported (CR vs LF, different label) |
| 7 | `text:"Note"`, `label:"stale"`, `isLabelCustom:false`, Unfiled, `lastUsedAt:null` | imported; label = suggestion "Note", not "stale" |

Expected: **entries 7, imported 6, duplicates 1, unsupported 0, rejected 0, folders created 3** (Work, Home, Empty
folder; the malformed `{nope:1}` is ignored).

Refusal inputs (inline in T2 and T9 tests, not files): one per `ImportRefusal`, plus a UTF-8 BOM TermFlow copy
(accepted), a UTF-16 LE copy (refused), an exact-cap and a cap + 1 padded file (generated at test time in a temporary
folder).

The third-party fixture counts are unchanged by A1 (text-only keys) and A2 (all well under 32 MB). The real-sample
counts in §3 are likewise unchanged: all three are third-party files, and the largest is 41 KB.

### 7.2 Real sample files (manual only, never committed)

The independent counts in §3 were produced on 2026-10-28 by a read-only Python script that applied the brief's rules
directly to the three real files. It shares no code with the C# implementation. The runbook compares the app's dialog
to those numbers.

### 7.3 Manual runbook

Run on the user PC with the CI AOT artifact, with a fresh profile (move `%LOCALAPPDATA%\SmartSnippets` aside, as the
008 scripts do). Unchecked items block completion.

| # | Step | Expected |
|---|---|---|
| 1 | Snippets page → Tab to **Import snippets** → Enter → pick `D:\temp\export-ref\termflow-snippets.json` | Dialog "Import complete": Imported 13 snippets from TermFlow. / Skipped 0 duplicates. / Skipped 0 unsupported entries. / Rejected 0 malformed entries. List shows 13; Browse shows folder "Prompt shortcut" plus Unfiled (2). `app.log`: `porting_ready` at start-up, `import_done format=TermFlow entries=13 imported=13 …`, no text, no path |
| 2 | Import `rephlo_commands_export_selected_20260904_164225.json` | Imported 5 from Rephlo, 0 / 0 / 0. Folder "Developing" created; 1 more Unfiled. Open a Rephlo snippet: its text has no appended `fixedContentAppend` content |
| 3 | Fresh profile again → import `inkspoke-commands-2026-09-04.json` alone | Imported 34 from InkSpoke, 0 duplicates, **11 unsupported**, 0 rejected. Folders Prompt Shortcut, Terminal, Text Autofill; no "Secure" folder unless a kept entry uses it; no password-looking text anywhere |
| 4 | Fresh profile → import TermFlow, then Rephlo, then InkSpoke | The InkSpoke dialog shows **32 imported, 2 duplicates, 11 unsupported**, 0 rejected. Only one "Prompt shortcut" folder exists (InkSpoke's "Prompt Shortcut" matched it); its count includes the InkSpoke prompts |
| 5 | Import the TermFlow file a second time | Title "Nothing imported": 0 imported, 13 duplicates. List unchanged |
| 6 | Refusals: (a) rename a copy to `.txt` and type its name in the picker; (b) a 33 MB `.json` (for example a padded copy); (c) a `.json` with `{ "x": ` (invalid); (d) the TermFlow file saved as UTF-16 in Notepad; (e) a copy with `"version": 2`; (f) `{"hello":1}` | Each shows "Couldn't import" with the distinct §4.3.7 message ending "Nothing was imported."; the snippet count is unchanged; `import_refused reason=…` each time |
| 7 | Open the popup over Notepad → sort Last used, then Usage count | TermFlow snippets appear in the order of their imported last-used dates and counts (the most-used TermFlow snippet first under Usage count), not in import order |
| 8 | Before exporting, create by hand two snippets with the text `hello` in different folders, and an empty folder. **Export snippets** → save `backup.json` → fresh profile → import `backup.json` | Export dialog names the count and file. `backup.json` opens in Notepad as a single compact line. The import reproduces every snippet (**both** `hello` rows), folder (including the empty one), tag, label, usage count and the dates shown in the editor's metadata. Importing `backup.json` again → "Nothing imported" |
| 9a | **Wide mode:** set the main window so the Snippets page is exactly at the wide threshold (880 px page width, list column fixed at 320 px) | The search box is at least 120 px wide, its placeholder "Search" fully visible, and about 12 typed characters visible; New snippet, Import and Export visible and clickable. Under 120 px → stop and report (D7) |
| 9b | **Narrow mode:** narrow the main window below 880 px | Search box, New snippet, Import and Export all visible and clickable; tooltips show on hover |
| 10 | Settings → text size Custom 32 → import any file | The result dialog's text is at the large size, wraps, and every line can be read (scroll if needed); OK closes it |
| 11 | Keyboard only, Narrator on: Tab from search → New snippet → Import snippets → Export snippets; Enter on each; Esc in the picker | Narrator reads "Import snippets, button" and "Export snippets, button". Esc in a picker → nothing happens, no dialog, `import_cancelled` / `export_cancelled` in the log |
| 12 | Tray → Restart as administrator → repeat steps 1 and 8 elevated | The pickers open and work elevated (R3) |
| 13 | Export over an existing `backup.json` while it is open read-only in another app that locks it (for example Excel) | "Couldn't export … Any existing file was left unchanged."; the old file is intact; no stray `.tmp` remains. (The other failure stages are covered by `AtomicFileWriterTests`.) |
| 14 | **Not manually practical (recorded, not run):** an export over 32 MB, a read error mid-file, a throwing view after commit, and usage at the saturation limit | Covered by `ExportImport_OneByteOver_…`, `ExportImport_EscapingExpansion_…`, `ReadThrows*`, `SnippetStoreObserverTests`, `UsageAtIntMax_Paste_Saturates_Reopen` |

## 8. Rollout / phasing

- **Order:** T0 → T1 → T2 → T3 → T4 → T5 → T6 → T7 → T8 → T9 (all Linux-testable; one branch
  `feature/010-import-export`), then T10 (Windows), then T11 and T12.
- **Gate before T10:** both test projects green and the Core/Presentation build at 0 warnings.
- **Gate before merge:**
  - CI `workflow_dispatch` on the branch (WinUI compile + AOT gate);
  - its new "Import budget (Release)" step (R2, T10) passing, after the D11 calibration has set `MaxImportEntries`;
  - then runbook §7.3.

  As in 007, a WinUI compile error found there is fixed forward on the branch, not by weakening the change. A budget
  failure before calibration is resolved by the defined entry-limit rule (§4.3.9), not by asking.
- **Store-wide changes (D8–D10) ship in the same merge.** They are small and each has its own test. They change
  behaviour for every mutation (subscriber isolation, log content, usage saturation), so the PR description lists them
  separately from the feature.
- **Shipping together:** everything ships in one merge. **Not inert:** the import and export path is unreachable
  without T10's buttons, but D8–D10 (T6) change existing store behaviour as soon as they land. That is why the whole
  plan ships as one reviewed merge, not in pieces.
- No feature flag (the buttons are the switch). No migration.

## 9. Open questions

None. Everything is decided:
- D1, D5 and D7 were approved with the plan on 2026-10-28. D7 has a defined fallback (§2).
- Q1 was resolved by the user on 2026-10-28 as D11, the entry limit calibrated by §4.3.9.
- D8 (lossless `0..int.MaxValue` usage with saturation), D9, D10 and D11 were approved with the plan.

The only remaining "stop" is the standing instruction for plan defects, and an R3 platform failure (the pickers not
working elevated), which would be a defect report, not a pending decision.

## 10. Approval log

**Review fix 4:** T10's temporary CI calibration step now explicitly sets `SMARTSNIPPETS_CALIBRATE: '1'`, requests
detailed console output and rejects runs missing the expected 18 `BUDGET` measurement rows / any case kind. The
persistent workflow has no Calibration step to amend (only the intended Release Budget gate), so it is unchanged.

**Review fix 3:** full persisted-row fake parity now covers all four fixtures and every stored field except ids.
The review's stats-discard mutation passed the old oracle (2/2), but fails the strengthened oracle (2 failures / 2
fresh-format passes). A reversed-tag-order mutation also fails (1 failure / 3 passes). Both mutations reverted;
**4/4 parity cases pass** against reopened SQLite rows with controlled clocks.

**Review fix 2 (user-directed):** native explicit folders now share the same 50,000 workload-entry limit with
snippets on import and export. Raw folder slots count before mapping, including malformed/duplicate entries.
Successful snippet-summary counts are unchanged. Existing messages remain exact; the sibling export refusal has
its own exact wording in §4.3.7. No full recalibration: keep **50,000** as directed. Cheap local Release verification:
50,000 empty native folders with zero snippets committed in **339.15 ms** with real subscribers and 20,000 existing
snippets; dense **611.00 ms**, folder-dense **895.43 ms**, typical **872.94 ms**; **4/4 budgets pass**. Binding CI is
still pending. Core targeted suite **79 passed**, Presentation view-model/messages **64 passed**.

**Review fix 1 (user-directed):** allocation-free escaped-string preflight now runs before source-generated
serialization, with the bounded sink retained. The 34 MiB `é` regression was red at **1,610,616,384 allocated bytes**;
after the fix it allocates **48 bytes** during export (input allocation excluded, metadata warmed). Text, label,
explicit-folder name, folder reference and tag payloads are all covered, as is their running total. Export/round-trip
filter: **34 passed**. This supersedes the earlier T7 source-only memory oracle interpretation below.

| Date | Event | Notes |
|---|---|---|
| 2026-10-28 | Requirement brief confirmed (G1) | User confirmed the brief in §2; PRD exclusion recorded as a scope change (T11) |
| 2026-10-28 | Review (docs/review, independent plan review, reviewer sol:medium) findings folded | Verdict "changes required"; findings 1–13 plus the nonblocking items folded (§11). User decisions A1 (native full-row dedupe) and A2 (32 MB cap, compact export with a 32 MB refusal) recorded in §2. D1, D5 and D7 remain awaiting user approval |
| 2026-10-28 | Round-2 review (`docs/review/010-plan-review-round2.md`, reviewer sol:medium) findings folded | Verdict "changes required". R1–R8 and the improvement list fixed as classes (§11, round-2 table) |
| 2026-10-28 | Plan approved (G2) | User approved plan 010 including D1, D5 and D7. Q1 resolved: per-file entry limit (D11) if the 32 MB commit budget fails; calibrated in T10. Status set to Approved |
| 2026-10-28 | T12 audit and final local checks | Source call graph, privacy/file I/O/JSON/fixture/doc checks recorded in docs/research/010-wiring-audit.md. Audit caught null version displayed as raw null instead of absent; added red test then fixed JsonFields. Core 409/409 and Presentation 413/413 pass sequentially; full solution 0 warnings; publish-aot.ps1 succeeds with no warnings, native exe + SQLite DLL and no managed App DLL. Fake parity now reads the full shared synthetic fixtures rather than duplicating embedded strings. Binding CI remains blocked and interactive runtime evidence remains PENDING-USER. |
| 2026-10-28 | T10 local evidence and D7 interpretation | No remote feature/010-import-export branch exists (`git ls-remote --heads origin feature/010-import-export` empty); no push allowed, so workflow_dispatch cannot test this tree. Local Windows calibration/build/Release gates used, with binding CI still BLOCKED. D7 fallback applied conservatively before manual measurement, to avoid shipping a toolbar likely to squeeze Search; real 880 px measurement remains PENDING-USER. |
| 2026-10-28 | Implementation interpretation: calibration and verification | Calibration constructs ParsedImport directly so counts above the provisional parser guard can reach the commit being measured; the guarded Budget tests still use real JSON and the public parser. Calibration is opt-in with SMARTSNIPPETS_CALIBRATE=1. T9 command tests were exercised against stubs (red run timed out waiting for an uncalled parse seam; the wait now has a timeout); thread/integration/budget tests were added after orchestration. Local Windows calibration is recorded separately from binding CI evidence, which cannot be obtained for unpushed changes. |
| 2026-10-28 | Implementation interpretation: T7 private buffer oracle | T7 asks tests to inspect an internal stream peak, but fixes a public API exposing no peak and forbids friend-assembly access. Narrow interpretation: public oversized test pins refusal and capped Bytes sentinel; source audit verifies check-before-write and backing-array capacity ≤ cap. No reflection, test-only public API or friend assembly added. Core round-trip tests synchronously wait only at the file boundary to retain store thread ownership; Presentation tests own async UI-hop coverage. |

**Implementation deviations:** T1's file-rules test was confirmed red before implementation; the remaining T1 tests were added after their implementations and have not each been confirmed red. T2 inherited working detection code: the expanded 34-case suite was run against it (33 green, lone-surrogate marker case red), then the marker handling was fixed and all 34 passed; historical red-first evidence cannot be recreated for already-implemented cases. T6 store/observer tests were confirmed red (17 failures against the bulk stub); repository, pipeline and fake-parity tests were added after the shared implementation. T6 log tests use a non-parallel AppLog collection: the first full-suite run exposed a concurrent append/read sharing violation, resolved by serializing the process-wide logger tests.

**Implementation evidence (filled in during implementation):**

| Item | Value |
|---|---|
| D11 calibration table (entries → commit ms per case) and CI run link | Local Windows Release (.NET 10.0.403 SDK / 10.0.14 runtime): dense 10k=254.50,20k=235.18,25k=258.63,50k=804.42,100k=1239.36,200k=2919.21,1,677,720 (32 MB)=19776.88; folder-dense 10k=223.61,20k=755.50,25k=533.43,50k=952.40,100k=2464.78,200k=4131.83,838,860 (32 MB)=18802.93; typical 10k=376.61,20k=585.27,25k=828.28,31,040 (32 MB)=1753.38. Calibration passed (1 test). CI link BLOCKED: remote feature branch absent and user prohibits pushing. |
| Calibrated `MaxImportEntries` | 50,000 (local Windows calibration; binding windows-latest calibration pending). Largest measured count passing every applicable case; typical reaches byte cap first at 31,040. |
| Budget step times (dense, folder-dense, typical) and CI run link | Local Release first gate: dense 50,000=784.29 ms; folder-dense 50,000=1718.42 ms; typical 31,040=1111.59 ms. Final rerun: dense=541.09 ms; folder-dense=912.17 ms; typical=713.08 ms. Both 3/3 passed 2,000 ms gate. CI link blocked as above. |
| D7 wide-mode measurement (search box px) and whether the fallback was applied | PENDING-USER measurement. New snippet icon-only fallback applied conservatively to preserve search space in the 320 px list; original tooltip/name retained. |
| Runbook §7.3 results | Steps 1–13 PENDING-USER (no interactive profile changes or UI automation run). Step 14 covered by automated Core tests. |
| Final local checks | Windows full solution 0 warnings/0 errors; Core 409 passed; Presentation 413 passed (sequential); publish-aot.ps1 exit 0, no IL or other warnings; native exe/e_sqlite3.dll present, managed App DLL absent. T12 static audit: docs/research/010-wiring-audit.md. |

## 11. Review response

Review findings (independent review of plan 010 at `27f08ba`, reviewer sol:medium) → changes in this plan:

| Finding | Change |
|---|---|
| 1 (D, High): lossless native restore versus dedupe | User decision A1: native full-row key (normalized text + label + folder + tags); third-party formats stay text-only. `DedupeMode` on `ImportBatch` (§4.3.1); planner rule (§4.3.5); native fixture with same-text and CR/LF rows (§7.1); round trip seeded through the real store with those rows, plus `RoundTrip_FullKeyRepeat_CollapsesToFirst` pinning the remaining collapse (T7); D1 and AC3/AC7 amended |
| 2 (D, High): exports the importer refuses | User decision A2: 32 MB cap for all formats; compact export; `NativeExportResult.TooLarge` with `ExportTooLarge` message and no file (§4.3.6–4.3.7); end-to-end export → real file → `CappedReader` → import tests at exactly the cap, one byte over, and with escaping expansion (T7); AC8 amended; R14 |
| 3 (A, High): usage overflow after import | D8 (as revised by round-2 R1: no import cap; `0..int.MaxValue` kept exactly); `RecordUsage` SQL and snapshot saturate at `int.MaxValue`; reader clamps; fake updated; all consumers listed in §3; chain test import → paste → sort → reopen → export (T6) |
| 4 (D, High): "Nothing was imported" after COMMIT | D9: `Raise` isolates subscribers and logs (store-wide, fake mirrors it); "Failure stages" table decides messages (§4.4); new `ImportOutcomeUnknown`; real-store throwing-subscriber tests (T6, T9) |
| 5 (C, Medium): store logs exception messages | §3 claim corrected (`SnippetStore.cs:292`, `AppLog.cs:44-50`, `AppLogTests.cs:35-47`); D10 removes the message from `store_write_failed`; failure-path sentinel tests assert presence before absence (T6, T9); T12 privacy audit includes `SnippetStore.cs` |
| 6 (A, Medium): read errors not mapped | `CappedReader` maps read-time IO errors to `Unreadable` (§4.3.2); T1 and T9 tests for first-read and mid-read failures, with `IsBusy` reset |
| 7 (C, Medium): byte cap ≠ UI-time bound | Claim removed; parse moved to a worker, commit on the UI thread (§4.1); enforceable 2,000 ms Release gate on dense, folder-dense and typical files into a 20,000-snippet library with live subscribers (R2, T9, §8); stop-and-report rule; §9 Q1; busy path documented as pre-existing |
| 8 (D, Medium): `Run` cannot produce I/O refusals; internal helpers | `SnippetImport.Run` replaced by `Commit(ParsedImport, store)`; refusal-ownership table (§4.3.2); value-rule tests go through the public parser, so no `InternalsVisibleTo` (T3 `ValueRulesTests`) |
| 9 (B, Medium): triggers don't prove "store never called" | Call counting: `FakeSnippetStore.ImportCalls == 0` with `ThrowOnImport` in every refusal test (T9); triggers kept only for no-write and atomicity proofs (T6) |
| 10 (B, Medium): QueueContext alone does not test the hop | Suspending fake reads and pickers plus a recorded-thread assertion for the store call and `Changed`, in a shared `QueueSynchronizationContext` helper (T9) |
| 11 (B, Medium): Int64 boundary | Compare-and-clamp-before-cast rule (§4.3.5); usage above the cap saturates; tests for `long.MaxValue`, `2^63`, `9.223372036854775808e18`, `1e300`, `long.MinValue`, `-1e300` for dates and usage (T3) |
| 12 (D, Low): T11 grep cannot pass | T11 verification rewritten: absence of the old line, presence of the new wording, plus master-plan checks |
| 13 (C, Low): wrong line | `ScriptedDialogService.cs:6` |
| Nonblocking: incidental folders | Planner creates folders only for planned rows and native `FolderNames`; four planner tests; TermFlow fixture entry 4 names an unseen folder |
| Nonblocking: refresh oracles | Integration tests query imported text, folder and tag in list, popup and folders page (T9) |
| Nonblocking: export I/O cleanup | `AtomicFileWriter` in Core with an injectable-step overload; tests per failure stage (T1); App delegates (T10) |
| Nonblocking: T12 file-I/O exception list | Audit scoped to this feature's files; pre-existing callers listed from §3 |
| Master-plan JSON wording (D2) | T11 rewrites master §2's "source-generated contexts only" to allow read-only `JsonDocument` DOM parsing |
| D7 wide mode | Runbook 9a measures the search box at the fixed 320 px list; under 120 px → the defined fallback (New snippet icon-only) |

Round-2 review (`docs/review/010-plan-review-round2.md`) → changes. Each was fixed as a class across all sites:

| Finding | Change |
|---|---|
| R1 (D, High): D8 truncates native usage | The one-billion import cap is removed. One rule for all formats: keep `0..int.MaxValue` exactly, saturate above (D8, §4.3.5). Writers saturate and the reader clamps (unchanged). Tests: `Usage_OneBillionAndThree_KeptExactly`, `Usage_IntMax_KeptExactly` (T3); store chain with exact values (T6); `UsageChain_Export_Reimport_Exact` (T7). AC4 and AC7 now hold without a usage exception |
| R2 (D, High): unsafe full-row key shortcuts | Fixed representation: `FullRowKey` plus `FullRowKeyComparer` with component-wise ordinal and element-wise tag equality. Joined strings and default record equality are forbidden (§4.3.5). §4.4 diagram reordered: normalize tags → resolve label → folder key → validate → dedupe. Adversarial tests: separators across text/label, inside tags, tag-boundary shift, empty vs one tag, separately allocated equal arrays; each library-vs-file and in-file (T5) |
| R3 (D, Medium): T6 depends on T7 | The T6 chain stops at reopen; the export and re-import half moved to T7 (`UsageChain_Export_Reimport_Exact`) |
| R4 (D, Medium): reader contract versus test | `CappedReader` maps only `IOException`/`UnauthorizedAccessException`; everything else propagates (§4.3.2). T1 `ReadThrowsInvalidData_Propagates`; T9 asserts the wrapper outcome separately |
| R5 (B, Medium): parse hop not deterministic | `PortingWorkers` seam (§4.3.2). T9 holds the parse on a pending `TaskCompletionSource` and completes it from a worker after the owner yields. Thread ids are asserted outside handlers. The same is done for export. `PortingWorkers_Create_RunsOffCallerThread` proves production uses a worker |
| R6 (D, Medium): busy lifetime misses error dialogs | `ImportAsync`/`ExportAsync` own `IsBusy` and handle every exception and dialog before their `finally`; the relay commands are thin. Busy tests are held at the summary, refusal, unexpected-error and store-failure dialogs for import and export (T9) |
| R7 (A, Medium): CI Release gate not wired; budget oracle weak | T10 adds the "Import budget (Release)" step to `ci.yml` (Area updated). Each budget test proves completion (counts, balance, persisted rows, list search, no `store_subscriber_failed`) before timing (T9) |
| R8 (D, Medium): round trip compared FolderIds | An explicit projection: folder name via `SnippetView.FolderName`; `Id` and `FolderId` excluded; tags as ordered sequences; multiset comparison. Plus `RoundTrip_FolderIdsDiffer_ProjectionStillEqual` (T7) |
| Improvement: snapshot timing | "Import start" is defined as commit start (§2). Interleaving tests create, delete and rename during a held parse (T9) |
| Improvement: D9 diagnostics and wording | Logs the subscriber index; no `Method`/`DeclaringType` metadata; typed invocation; "a subscriber failure alone cannot escape" (D9, §4.3.2, §4.4) |
| Improvement: export stages and responsiveness | Export stage table (§4.4). Serialization moved to a worker over immutable snapshots. `BoundedBufferStream` stops at 32 MB. `TooManySnippets` checked before serializing. Tests in T7/T9 |
| Improvement: temp cleanup wording | Best-effort delete stated consistently in `ISnippetFileService` and `AtomicFileWriter`; dispose before move, with a test (T1) |
| Improvement: stale claims | §8 "inert" corrected; `InsertionServiceTests.cs:242` citation fixed |
| User decision Q1 | D11 entry limit: refusal message, export guard, calibration rule and floor (§4.3.9), parser/export/view-model tests, CI calibration step (T10) |
