# 008 harness — implementation and static audit

2026-10-28, branch `feature/008-harness`. This is **implementation evidence**, not a desktop acceptance result or a release sign-off. See [checklist](008-harness-checklist.md) and [acceptance record](003-release-acceptance-v1.md).

## T0: exhaustive T9–T11 and §7 reconciliation

| Reference | Merged source / harness evidence |
|---|---|
| T9 prototype removal | No `TestString` or `Héllo – naïve` prototype string in `src`; F uses stored snippets. Diagnostics/ProbeRepository callers are absent (System.Diagnostics is unrelated). |
| T10 shared | ArtifactZip verifies filename/hash/layout/no PDB before extraction and records exe versions; A3 calls the shared gate; A4 runs `Get-HarnessLogAudit`; C11 expects the production menu; D-lock holds a real SQLite writer transaction, checks inline retry and retained text, releases and retries. |
| E1 | `PopupWindow.ShowFor`/`WindowPlacement.CenterOn`; `PopupSearch` / `Search snippets`; monitor work rectangle comes from GetMonitorInfo, physical UIA rectangle centers compared within rounding tolerance (2 px). |
| E2 | Types both query fixtures using per-character native keyboard text input with foreground/search-focus checks, then verifies exact entered text, `PopupCount` (`1 of 4 snippets`) and `UiText.NoSnippetsMatch`. P2 deliberately retains whole-value UIA SetValue for timing. |
| E3 | `PopupViewModel.MoveSelection`, `PopupItemKind.Snippet`; disabled heading containers in PopupWindow. Both arrow directions traverse the fixture, skipping headings. |
| E4 | Esc → `PopupViewModel.Close`; assert hidden, exact unchanged target and SQLite usage sum. |
| E5 | `popup.layout` = Flat/Folder; `popup.sort` = LastUsed/UsageCount/Created/Updated. Eight layout/sort combinations checked in UI and SQLite on reopen and process restart. Selecting an unstored default LastUsed is a no-op, so the helper changes away first to exercise a real persistence write. |
| E6 | PopupLayoutButton/Toggle layout; PopupSortButton/Sort; PopupAddButton/Add snippet; PopupOpenMainButton/Open main window. Tooltips follow UiText; layout is TogglePattern, sort menu uses the actual four names. |
| E7 | Overlay PopupEditor; editor Snippet text; PopupEditorCancel/Cancel and PopupEditorSave/Save. Cancel restores query/selection; save clears a hiding query and selects the new row; Enter inserts into the original Notepad. |
| E8 | PopupOpenMainButton invokes `PopupWindow.OpenMainWindow` → MainWindow.ShowPage(Snippets), hides popup. |
| E9 | Tab/Shift+Tab cycles search, four toolbar controls and list (reverse sequence checked); a physical row click is checked against Notepad text and Inserted. |
| E10 | Standard small fixture, first open after --background separate, then 20 opens; every sample ≤300 ms, median/nearest-rank p95/max reported. |
| E11 | Main-window UI creates and relabels while popup hidden; reopen inspects both labels, then unique-term search. Live-while-shown evidence is `PopupViewModelListTests.WhileShown_SelectedSnippetEdited_RowUpdates_SelectionKept` and `WhileShown_SnippetAdded_AppearsInSortOrder_SelectionKept`. |
| E12 | Folder row buttons have generic names `Rename folder` / `Delete folder`, so scope them to the correct ListItem; rename edit is `Folder name`. Reopened folder list must have renamed heading, no former/deleted headings, moved snippet specifically after Unfiled heading. Live tests: `WhileShown_FolderRenamed_HeadingAndChipsUpdate_SelectionKept` and `WhileShown_FolderDeleted_RowsShowUnfiled`. |
| F1/F2/F3 | Real mixed CR/LF/CRLF Unicode fixture → Core LineEndings.ToCrLf → dispatcher. Ordinal text equality includes surrogate pairs, tabs and trailing spaces. TextPattern selects a word for replacement; clipboard sentinel restored. Notepad Edit and Document controls supported. |
| F4 / master §7 #3 | No successful-usage log exists: read SQLite + main editor Used/Once stat + popup Usage count (the actual Most-used sort name). `RecordUsage` updates last_used_at without updated_at. |
| F5 | Queues double-Enter / Enter-plus-row mouse messages to captured native input HWNDs belonging to the popup (never global cached-coordinate input). Requires actual `popup_insert_ignored reason=inserting source=keyboard|row` receipt at the view-model gate, exact target text, one Inserted and one usage increment. An independent F5.hotkey checks `popup_open_ignored reason=inserting`; it is not evidence for duplicate keyboard/row delivery. No delivery/receipt means FAIL, not a fallback physical click. Busy still returns without an insert_result log. |
| F6/F7 | TargetClosed footer: `Couldn't insert. <target> was closed. Nothing was pasted.`; own foreground is rejected by TargetCapture/AppHost (`hotkey_ignored_own_or_no_window`), with Win32TargetProbe/TargetIdentityProbe as late defense. No paste_sent and no usage permitted. |
| F8/F12 | F8 SKIP: UAC requires user acceptance, matrix remains required. F12 INFO/N/A: externally unforceable uncertain dispatch; `InsertionServiceTests.FailedDispatch_OnlyUncertainReleasesAndWaits`, `SendThrows_IsUncertain`, `PopupViewModelInsertionTests.PasteAnyway_ThenMaybePasted_ShowsCaution`. Not a desktop pass. |
| F9 | 64 MiB +4096 registered HGLOBAL plus Unicode sentinel. Inline PopupFooterError and PopupPasteAnyway/Paste anyway, PopupPasteCancel/Cancel. Complete format/size/hash fingerprint retained; ClipboardNotPreserved must precede popup_paste_cancelled; no dispatch/usage/text change. |
| F10 | AllowClipboardLoss revalidates and dispatches. Compare every format saved before the snapshot cap, not just text; oversized registered format must be absent. Added privacy markers are allowed. Exact one-probe Notepad text, usage +1 and successful restore required. |
| F11 | Closed target after confirmation is **CopiedForManualPaste**, not TargetClosed: `Couldn't paste into <target>. The snippet is on your clipboard. Paste it yourself with Ctrl+V.` No paste_sent; unchanged usage; clipboard probe; `manual_paste_fallback reason=Closed status=Swapped`. Corrected the plan's ambiguous “target-closed message”. |
| G1–G10 | Exe versions/icons and frame counts, real tray logs and broadcasts, opt-in reversible theme change, production menu, tag tests, subprocess negative publish gate, validated archive, and whole-run privacy. G10 runs after P in finalization; every reset archives logs before deletion, including older E/F profiles, and gathers all saved library text/labels. App-log summaries emit only bounded allowlisted event names/line numbers, never raw log payloads; the corresponding exception and failed SQLite payloads are withheld too. This does not sanitize every UI/DB/machine detail in the acceptance report; review before sharing. |
| P0 | New-PerfLibrary starts the real app to create schema, exits, then imports one transaction generated by New-PerfLibrarySql(-Count PerfLibrary, -Seed 42). Default exactly 10k, 50 folders/180 each +1000 Unfiled, 60/30/10 length and line distributions, 5% Unicode, 70% custom labels. Auto snippets deliberately tagless to use LabelSuggester first-line/60-text-element rule. Tags use comma storage, not whitespace. Fixed epoch 2026-10-28 keeps fixture deterministic; 2-year creation/update and 365-day usage windows are relative to it. Planted counts 1000/1000/100/0 asserted via SQL. Nondefault library sizes scale planted/foldered counts proportionally; 10k remains the acceptance default. |
| P1/P2/P3 | popup_ready_ms is hotkey timestamp → ReportReady at activation/layout readiness, **not** search render completion. popup_search_ms ends at CompositionTarget.Rendered following list layout, or next frame for unchanged rows. Cold +20 ready samples; 160 interleaved searches via whole-value SetValue, precondition renders excluded, exactly one sample with matching layout/result count. Every cell median ≤150; p95/max reported; p95>300 explicitly raised as INFO attention (not a changed pass bar). R11 single retry asks user to close other apps and retains both attempts/failures. P3 and A2 Core benchmark diagnostic only. |
| §7.1 | Unit classes and named methods exist in tests. Automated acceptance remains user-run on rc; visual/UAC/sign-out/Explorer/contrast/scale/Narrator and matrix R1–R14 remain manual. No new manual exemption was invented for an automatable assertion; missing required UI fixtures now FAIL rather than SKIP. |
| §7.2 | All PRD/AC evidence IDs remain mapped to actual implemented sections; manual matrix and runbook entries remain planned, not passed. Theme/text-size/startup use C/R10/R11, persistence B/D/E5, CRUD D/E7/E11/E12, keyboard E, dispatch/clipboard/usage F, performance E10/P1/P2, release A3/G1/G9/R2/R14. |

## T15: complete static wiring trace

- Publish gate: `scripts/publish-aot.ps1` is called by `.github/workflows/ci.yml`, `release.yml`, A3 and the G8 negative subprocess. Release tag validation feeds Version/FileVersion into publish; SDK VERSIONINFO is consumed by release/CI version checks and G1. No release was triggered.
- Icons: SVGs → `scripts/icons/generate_icons.py` → seven checked-in `Assets/*.ico` → csproj ApplicationIcon and Content/CopyToPublishDirectory → native executable/loose assets → MainWindow.SetIcon and TrayIcon.GetIcon. TrayIconNative uses source-generated LibraryImport, not the illustrative extra CsWin32 enum entries. Cache handles are destroyed on theme change/disposal; fallback logs filename only. G2/G3 check structure, G4/G5/G6 consume state/theme events; visual R5–R9 still needed.
- TaskbarTheme's real method is **IsLight**, not Read: constructor/RefreshThemeAndDpi and TaskbarCreated refresh before Add, WM_SETTINGCHANGE reads it and always re-evaluates the icon. Elevated filter for 0x001A exists; R9 still needs execution.
- AppHost is the sole `_tray.State =` writer and calls `TrayStatePolicy.Compute`. StatusChanged and VisibleChanged use named handlers, unsubscribed in ReleaseResources. Four `Compute_PrioritizesHotkeyFault` theory cases cover red-over-blue; corrected illustrative test names/path in plan T7.
- AppHost composes one Presentation InsertionService and Win32TargetProbe/ClipboardPasteDispatcher, passes the one IInsertionService contract to PopupViewModel. Popup hides before insertion; Inserted alone calls RecordUsage. SnippetStore publishes a new snapshot and raises Changed; PopupViewModel, SnippetListViewModel and SnippetEditorViewModel subscribe and unsubscribe, so main Used metadata and popup list update through the shared store path, without a second successful-usage event.
- Every InsertionOutcome has UiText wording. Popup explicitly handles Inserted notice/hidden, Busy no-op, ClipboardNotPreserved confirmation, MaybePasted caution, CopiedForManualPaste manual-paste footer, and default retryable errors. Clipboard confirmation retries same id/target with AllowClipboardLoss. Enter during MaybePasted dismisses, not retries. Residual last-check-to-SendInput race remains explicitly accepted by the user, not claimed tested.
- Removed Diagnostics has no callbacks, constructor parameters, window or ProbeRepository caller left. Immutable probe migration/table remain.
- Uia.ps1 sources Clipboard.ps1. F sources InsertionSection.ps1 and calls Install/Intact/Restored/Clear with cleanup in finally. User clipboard preservation is **text only**, as specified; non-text user data is not claimed preserved. E also saves/restores user text around a deterministic clipboard sentinel so its insertion checks cannot unexpectedly hit a user-owned unsaveable payload. Main profile restoration is in finally even if final privacy collection fails; an initially absent user profile remains absent.

### Every consumed log event has an emitter

`Get-HarnessLogAudit` parses all script Log-Has/Wait-Log literal consumers (including alternatives), direct timing/outcome regex consumers and the A4 manifest. It locates **AppLog.Info/Error calls**, not merely matching source strings. The original 51 required/consumed events had emitters; the remediation adds `popup_insert_ignored` (52 manifest events). The pure test also proves missing emitters are rejected. Payloads used by checks were traced against the calls, including ordered ClipboardNotPreserved/cancel, Closed fallback and restore states. Removed nonexistent `hotkey_register_failed`; the actual startup event is `hotkey_startup_failed`.

| Source | Events (all audited) |
|---|---|
| AppHost / Program | app_started, data_load_failed, exit_unregistered, hotkey_ignored_own_or_no_window, settings_loaded, second_instance_redirect |
| Migrator / SnippetStore | migration_backup, migration_failed, migrations_applied, store_loaded |
| HotkeyManager / ThemeService / ElevationService / MainWindow | hotkey_registered, hotkey_changed, hotkey_startup_failed, theme_applied, restart_elevated_started, navigate_failed |
| TrayIcon | tray_added, tray_add_failed, tray_readded, tray_icon, tray_icon_load_failed, tray_theme_changed |
| Main-window view-models | snippet_saved, snippet_deleted, folder_created, folder_renamed, folder_deleted |
| PopupWindow | popup_ready_ms, popup_search_ms, popup_open_ignored |
| PopupViewModel | popup_opened, popup_closed, popup_layout, popup_sort, popup_add_opened, popup_add_saved, popup_add_cancelled, popup_insert, popup_insert_ignored, popup_paste_anyway, popup_paste_cancelled |
| InsertionService / ClipboardPasteDispatcher | insert_start, target_check, target_rejected, foreground_gate, clipboard_swap, clipboard_restore, clipboard_restore_formats, insert_result, manual_paste_fallback, usage_record_failed, paste_sent |

Runtime appearance of success/failure/optional events remains part of the user-run report; absence of naturally untriggered failure events is not a static wiring failure.

## Original local validation (before independent review)

| Command / check | Result |
|---|---|
| Windows PowerShell 5.1.26100.9711 parser | All 14 `.ps1` files; 0 errors |
| PowerShell 7.6.9 parser | All 14 `.ps1` files; 0 errors |
| `scripts/test-harness-pure.ps1`, in both shells | 29 assertions PASS per shell; real migration/SQLite import, deterministic 10k generation, exact distributions/search counts, all auto labels, statistics, strict timing parsers (negative cases), whole-run privacy, 51 source emitters, missing-emitter rejection, clipboard-format comparison test double |
| `scripts/test-harness-native-definitions.ps1`, in both shells | Both C# native helper definitions compile; no native APIs invoked |
| `dotnet build` | PASS; 0 warnings, 0 errors |
| `dotnet test tests/SmartSnippets.Core.Tests` | 176 passed, 0 failed/skipped |
| `dotnet test tests/SmartSnippets.Presentation.Tests` | 333 passed, 0 failed/skipped |
| `pwsh -NoProfile -File scripts/publish-aot.ps1 -Output publish` | PASS; 0 IL2xxx/IL3xxx warnings, native exe + e_sqlite3.dll present, no SmartSnippets.App.dll |

Local native output is unversioned `0.0.0-dev` (FileVersion 0.0.0.0), not a downloaded release candidate. No desktop automation was run. RC measurements, matrix, R1–R14 and acceptance sign-off remain PENDING-USER. CLI stdout was captured during the session; no test/profile/clipboard side effects from the pure checks persist.

## Round-1 independent review remediation — non-interactive validation

Safety and abort/concurrency procedures: [002-acceptance-harness-safety.md](../guides/002-acceptance-harness-safety.md). Seven finding-specific commits replace unowned clipboard publication, global process cleanup, failed-read-as-empty snapshots, unbounded native menu reads, misrouted duplicate input, programmatic-only E2 query input and raw-log diagnostics. Follow-ups add encrypted recovery subprocesses, exclusive profile leases, a protected move boundary, title tokens and explicitly diagnostic non-10k results. Native input delivery/clipboard ownership remain user-run checks, not inferred from compilation.

- Only verified classic single-document Notepad is accepted. Modern tabbed/packaged Notepad (including launcher reuse/session restoration) is **refused**, never killed or used as a target. Use the safety guide; do not weaken the identity predicate to obtain PASS.
- The plan002 prototype script now refuses execution instead of retaining unsafe dormant cleanup/profile code.
- Supervisor tests launch **fake CLI workers only**, with temporary lock/report paths and `Only=A` to disable clipboard backstops. They exercise worker completion, manually held supervisor/orphan-worker lease refusal and worker timeout/FAIL-report replacement without app/Notepad/UIA/profile/clipboard access. They do **not** cover native CLI descendants, actual surviving-child lease ownership, or clipboard-helper exit/handoff/restore/backup lifecycle. Those lifecycle gaps are documented in the safety guide; clipboard orchestration remains manual-run evidence, not inferred from pure tests.
- `test-harness-pure.ps1`: **77 assertions PASS in each shell** (Windows PowerShell 5.1.26100.9711 and PowerShell 7.6.9). The 48 added assertions cover target identity (7), clipboard retry/validation/recovery (14), watchdog/lease logic (5), duplicate message plans (4), log collection/sanitization (9), and profile/evidence/title improvements (9).
- Full seed-42 generated SQL SHA-256 matches across both shells: `c6dd44821b918cfe64312aa298271b37e17e1fe7231566244d3a766193f4aead`. Exact 10k fixture/search distributions still pass.
- All **19** `.ps1` files parse in each shell: **0 errors** (`test-harness-parsing.ps1`).
- Native-definition test: **2 C# definitions compile + 1 managed INPUT ABI layout check PASS per shell**. No native API is invoked by that test.
- `dotnet build`: **0 warnings, 0 errors**. Core: **176 passed**. Presentation: **335 passed**, including 2 new pending-duplicate theory cases. Test projects ran sequentially.
- Initial new DPAPI roundtrip verification exposed a machine-local Windows PowerShell Security module type-data autoload failure. Recovery serialization was changed to construct SecureString directly (no Security cmdlet autoload); final runs in both shells pass. No failure was waived.
- No interactive acceptance, clipboard mutation, Notepad/app launch, AOT re-publish, release or push was performed during remediation verification. The encrypted roundtrip tests use synthetic text in temporary files, not the desktop clipboard. The new targeted F5 route must produce its own keyboard/row gate receipt in desktop acceptance; failure to deliver is FAIL, never a fallback click on the underlying app.

## Round-2 scoped remediation

Scope: implement review findings 1/2/3/6; document, rather than implement, descendant-process containment and clipboard/supervisor orchestration coverage (findings 4/5). See the safety guide's Known limitations. No job objects or new handoff framework were added.

- Bootstrap explicitly propagates nested exit codes and fails terminating errors. A fresh RunId/completion acknowledgement is required for exit 0; requested reports must match the run and acknowledged SHA-256. Early exit/stale reports cannot supply acceptance evidence.
- C5 launches through the pinned owned-process helper; a failed second-launch wait stops that exact process in finally before profile recovery.
- C7 and G6b durably journal original value existence/type/value before mutation. C7 now restores in finally. Supervisor recovery independently replays the allowlisted run-specific registry journal after worker exit/kill, retaining it on failure/abort; manual recovery is documented. Tests inject file-backed readers/writers and never change real HKCU.
- D14 and C11/G7 withhold raw names/menu text. The privacy claim now explicitly covers app-log summaries, not every UI/DB/machine report detail; reports require review before sharing.
- Final checks: all **22** scripts parse with **0 errors** in Windows PowerShell 5.1.26100.9711 and PowerShell 7.6.9; **101 pure assertions PASS per shell** (24 added this round: worker verdict/completion 8, owned second-launch boundary 3, registry recovery 13); both native definitions compile plus managed INPUT ABI check (3 checks per shell). Cross-shell seed-42 SQL hashes still match.
- `dotnet build`: **0 warnings/errors**. Core **176 passed**; Presentation **335 passed**, run sequentially. No interactive acceptance, real registry/clipboard mutation, Notepad/app launch or AOT re-publish was performed. Fake supervisor tests still use Only=A; clipboard-process lease/backup handoff and actual native/UI delivery remain manual-run evidence, not conclusions from the test count.
