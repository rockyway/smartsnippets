# 003 — Release acceptance v1

**Status: PENDING-USER / implementation incomplete. This is not an acceptance sign-off.**

## Build under test

PENDING-USER: record accepted rc tag, commit, GitHub run, archive filename, SHA-256 and exe ProductVersion/FileVersion. No release has been triggered. Run acceptance only after the 008 implementation and harness are complete and the rc is published.

## Verify script and master integration

PENDING-USER: after implementation, download the rc zip and `.sha256`; run `powershell -ExecutionPolicy Bypass -File scripts/verify-plans-003-009.ps1 -ArtifactZip <path> -ReportPath docs\research\003-run.md`. The harness validates and extracts `-ArtifactZip` and has preliminary E/F/G and D-lock checks, but E/F/P are incomplete and report explicit failures; do not use an incomplete run for acceptance. Attach the final report. Master §7 #1: E11 plus `PopupViewModelListTests.WhileShown_SelectedSnippetEdited_RowUpdates_SelectionKept` and `WhileShown_SnippetAdded_AppearsInSortOrder_SelectionKept`; #2: E12 plus `WhileShown_FolderRenamed_HeadingAndChipsUpdate_SelectionKept` and D12; #3: F4; #4: C9 and C9.restart; #5: C12.free; #6: B. All integration results are PENDING-USER.

## Performance

PENDING-USER: implement section P first, then run it against the downloaded rc and deterministic 10,000-snippet seed-42 dataset as specified in plan 008 T11. Record CPU, RAM, power plan, scale and build; P1 first open and 20 samples (each ≤300 ms); P2 four queries × two layouts × 20 samples, expected result counts and median ≤150 ms per cell, p95 and max; P3 Core benchmark is diagnostic only. Re-run a failed cell once after closing other apps, preserving both runs. No measurements have been made.

## Compatibility matrix

PENDING-USER: create P-Unicode `Héllo – naïve 👋 世界 Ωμέγα`, P-Multi (three lines, leading tab and trailing spaces), P-Plain `smart-snippets-probe`. For each target copy a sentinel, insert Unicode and multiline, replace a selected word with plain, check `Get-Clipboard` and usage counts, then perform the unsaveable clipboard Cancel/Paste anyway procedure from plan 008 T12 once its helper is implemented. Record actual outcomes; terminal selection replacement is N/A and multiline warnings are noted. Elevated Notepad requires the app unelevated first, then retry after Restart as administrator.

| Target | Unicode | Multiline | Replacement | Clipboard restored | Usage once | Unsaveable Cancel / Paste | Notes |
|---|---|---|---|---|---|---|---|
| Notepad | PENDING-USER | PENDING-USER | PENDING-USER | PENDING-USER | PENDING-USER | PENDING-USER | |
| VS Code | PENDING-USER | PENDING-USER | PENDING-USER | PENDING-USER | PENDING-USER | PENDING-USER | |
| Word | PENDING-USER | PENDING-USER | PENDING-USER | PENDING-USER | PENDING-USER | PENDING-USER | |
| Outlook | PENDING-USER | PENDING-USER | PENDING-USER | PENDING-USER | PENDING-USER | PENDING-USER | |
| Chrome input and textarea | PENDING-USER | PENDING-USER | PENDING-USER | PENDING-USER | PENDING-USER | PENDING-USER | |
| Edge input and textarea | PENDING-USER | PENDING-USER | PENDING-USER | PENDING-USER | PENDING-USER | PENDING-USER | |
| Terminal PowerShell | PENDING-USER | PENDING-USER | N/A | PENDING-USER | PENDING-USER | PENDING-USER | |
| Terminal cmd | PENDING-USER | PENDING-USER | N/A | PENDING-USER | PENDING-USER | PENDING-USER | |
| Teams | PENDING-USER | PENDING-USER | PENDING-USER | PENDING-USER | PENDING-USER | PENDING-USER | |
| Elevated Notepad | PENDING-USER | PENDING-USER | PENDING-USER | PENDING-USER | PENDING-USER | PENDING-USER | Expected elevation refusal then success after elevation |

## Manual runbook

PENDING-USER: perform R1–R14 exactly as specified in plan 008 §7.1, recording each result, failures and explicit user acceptance here. R14 requires two published rc releases and a repeat from accepted rc to final. Visual checks, UAC, real Explorer restart, sign-out, high-contrast, scale, Narrator and matrix have not been run.

## Independent spec audit

PENDING-USER: T14 is reserved for the user. Plan 008 §7.2 lists planned evidence, not a completed independent audit.

## Wiring audit (T15, static inspection; incomplete)

- Shared `publish-aot.ps1` is called by CI, release and A3. Version flows from tag through workflow outputs to publish properties and release version check; G1 is not implemented.
- `assets/icons/*.svg` → `scripts/icons/generate_icons.py` → seven checked-in `Assets/*.ico` → csproj content/publish. `ApplicationIcon` embeds the mark; MainWindow uses `AppWindow.SetIcon`. TrayIcon loads and caches theme/state variants at system DPI and falls back to the shared stock icon on load failure. `TaskbarTheme.IsLight` reads the registry at construction, theme broadcast and Explorer restart. Visual, high-contrast, elevated broadcast, published-exe icon and DPI checks remain PENDING-USER.
- `AppHost.UpdateTrayState` is the only tray state assignment and calls Presentation `TrayStatePolicy`; hotkey status and popup visibility events call it. Named handlers are unsubscribed during resource cleanup.
- Popup calls the one `IInsertionService` contract; `InsertionService` records usage only for `Inserted`; `SnippetStore` raises `Changed`. `UiText.InsertionMessage` covers the enum including clipboard-loss and uncertain-dispatch outcomes. Relevant tests include `InsertionServiceTests.IncompleteSnapshot_DoesNotEmpty`, `OverCap_NotPreserved_NoEmptyClipboard`, `SendThrows_IsUncertain`, and `PopupViewModelInsertionTests.PasteAnyway_ThenMaybePasted_ShowsCaution`.
- `popup_ready_ms` and `popup_search_ms` are emitted by PopupWindow; E/P measurement consumers are not implemented. A4 includes 008 and selected insertion/popup events. G4/G5/G6/G6b now consume tray state/theme logs; synthetic G6 relies on `TrayIcon` reevaluating on every `ImmersiveColorSet` broadcast. G10 currently observes only the surviving pre-G profile log plus G's last profile, not every profile from the entire run; privacy coverage is incomplete. Runtime evidence is PENDING-USER.
- Diagnostics callback, menu item, window and ProbeRepository were removed; prototype-only repository tests were removed while migration tests remain.
- Clipboard helpers are dot-sourced by `Uia.ps1`, but F9–F11 do not yet consume them. `Save-ClipboardText` preserves text only, as the plan's F section specifies; non-text clipboard data cannot be restored by this helper. P0–P2 and the E/F full checks remain absent; an explicit incomplete failure prevents acceptance of a partial run.

## Observations and insertion notes

- Lone CR: insertion contract specifies CRLF normalization. F1 and matrix confirmation PENDING-USER.
- Shortcut conflict sentence unreachable by recorder: accepted as-is by user 2026-10-28; startup conflict evidence C13/G5 PENDING-USER.
- `Snippet text cannot be empty.`: canonical wording accepted in plan 008 T16.
- Unsaveable clipboard: prompt before emptying; unit evidence above, F9–F11 and matrix PENDING-USER.
- MaybePasted: F12 is N/A externally; `InsertionServiceTests.SendThrows_IsUncertain` and `PopupViewModelInsertionTests.PasteAnyway_ThenMaybePasted_ShowsCaution` provide unit evidence, not manual acceptance.
- Residual focus race between final revalidation and SendInput accepted by user 2026-10-28 (plan 008 R12).

## Sign-off

PENDING-USER: review completed evidence and explicitly sign off after all blockers are resolved. Final tag and programme closure remain pending; do not mark the master plan Complete.
