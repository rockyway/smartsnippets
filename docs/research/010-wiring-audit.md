# 010 wiring audit

Static audit performed after T11 rebase onto local `main`. Runtime runbook checks are explicitly PENDING-USER; no interactive app launch, real profile manipulation or personal fixture access was performed.

## Production call graph (verified with rg in src)

- `SnippetsPage.xaml:185,191` binds `ViewModel.Porting.ImportCommand` / `ExportCommand`; both paths compiled in the full Windows solution build.
- `AppHost.Compose` constructs the sole `SnippetFileService`, supplies the main-window owner id and passes it to the page view-model; no worker override is injected. `porting_ready` is emitted there.
- `SnippetsPageViewModel` always constructs `Porting` with its store, dialogs and optional file service. Null file service disables both commands (passing page tests).
- `SnippetPortingViewModel` is the only production caller of the file-service members, `CappedReader.ReadAsync`, `SnippetImport.Commit` and informational dialogs.
- It uses `PortingWorkers.Default`; the only production `ImportParser.Parse` and `NativeExport.Write` calls are inside the two `Task.Run` lambdas in `PortingWorkers.Create`.
- `SnippetImport.Commit` is the only production caller of `ISnippetStore.ImportSnippets`. The real store calls `ImportPlanner.Plan` against its current snapshots and calls `InsertImported` inside one `Write`.
- `ImportPlanner.Plan` has no other production caller; the test fake mirrors it.
- `SnippetRepository.Insert` delegates to `InsertImported`. There is exactly one `INSERT INTO snippets` in production source.
- `SnippetFileService.WriteAtomicallyAsync` delegates directly to `AtomicFileWriter.WriteAsync`. That is the only production caller of the writer; its overload delegates to the injectable algorithm.
- Parser and export both enforce `MaxImportEntries` (50,000); UiText uses the same constant.

## Parallel paths / ownership

- `usage_count` has one SQL UPDATE, `MIN(usage_count + 1, 2147483647)`. Repository reads clamp Int64 to 0..int.MaxValue. Store and fake RecordUsage saturate; imported usage is validated by JsonFields. Sort uses CompareTo; no remaining overflowing increment.
- `SnippetStore.Raise` iterates a captured invocation list with typed casts and per-subscriber catch. No `Changed?.Invoke` remains in the store. The fake has the same isolation. Imported is raised once by the bulk path, and not on no-op or rollback.
- All four production Changed subscribers remain kind-agnostic: list, popup, folders, editor. Real-store integration tests query imported text/tag/folder and popup sorts; the dirty editor retains its unsaved text and gains folder choices.
- Busy ownership: only ImportAsync and ExportAsync set IsBusy=true, and only their finally blocks clear it, after awaited dialogs. Relay commands have no secondary handler.
- No ConfigureAwait(false) in the view-model. A temporary mutation adding it to parse/export awaits made all three owner-thread hop tests fail (3 failed, 2 other thread-class tests passed); mutation reverted. Final full Presentation suite passes.
- Scoped file API audit: only SnippetFileService opens the import FileStream, and AtomicFileWriter opens/deletes the same-folder temp file and moves it with overwrite. Other changed files contain no new file API. Pre-existing icon/migration/log I/O is unchanged.
- Atomic writer disposal-before-move and cleanup/failure stages are covered by passing T1 tests.
- Source-generated `NativeJsonContext.Default.NativeDocument` is passed to the sole production JsonSerializer call. No System.Text.Json in Presentation or App. No friend assembly or reflection-based JSON added.
- Export's internal buffer checks the incoming length before every write; Array.Resize capacity is clamped to the limit. The public oversized test pins refusal and the cap+1 Bytes sentinel; the internal-capacity oracle interpretation is recorded in plan §10.

## Privacy and fixtures

- Every added/changed log statement was inspected. New import/export and store failure logs use counts/enums/bytes/ms or exception type/HResult only; their AppLog.Error exception argument is null.
- AppHost and SnippetsPageViewModel have pre-existing exception-bearing log calls on unrelated startup/selection/deletion/exit paths; none was added or changed and none is on the new import/export path.
- Real trigger-failure tests first assert store_write_failed with SQLite code 19, then absence of trigger/body sentinels. View-model tests first assert import_done/import_failed, then absence of path/name/text/label/folder/tag/exception sentinels.
- `rg 'D:\\temp|export-ref' src tests` found no matches. All four fixtures are small invented files; no personal source was opened or copied.
- T11's six documentation greps were rerun: exclusion absent; one Included bullet; one 3.11 heading; scope note, DOM constraint and master row present.

## Final local verification

- Full Windows `dotnet build`: 0 warnings, 0 errors, App included.
- Core: 409 passed, 0 failed, 0 skipped.
- Presentation, run after Core: 413 passed, 0 failed, 0 skipped. The ordinary calibration test is opt-in/no-op; the separate opt-in Release calibration actually ran and passed (1 test).
- Release budgets: 3 passed. Final rerun: dense 50,000=541.09 ms, folder-dense 50,000=912.17 ms, typical 31,040=713.08 ms. All completion oracles run before timing assertions.
- `pwsh -NoProfile -File scripts/publish-aot.ps1`: exit 0; publish.log contains no warnings, including no IL2xxx/IL3xxx; exe and e_sqlite3.dll exist; managed App DLL absent.
- `git diff --check`: clean.

## Remaining gates

- Binding windows-latest CI calibration/budget/AOT run: BLOCKED. `git ls-remote --heads origin feature/010-import-export` returns no branch; user explicitly prohibits pushing. Local Windows evidence is not labelled as CI evidence.
- Runtime `porting_ready` / real-sample import_done, picker elevation, toolbar width at 880 px, accessible keyboard flow and text-size-32 dialog: PENDING-USER, runbook steps 1–13. D7 fallback is already applied; actual pixel measurement is still required.
