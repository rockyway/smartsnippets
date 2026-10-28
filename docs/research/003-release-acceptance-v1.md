# 003 — Release acceptance v1

**Status: harness implementation complete; rc acceptance PENDING-USER. This is not an acceptance sign-off.**

## Build under test

PENDING-USER: record accepted rc tag, commit, GitHub run, archive filename, SHA-256 and exe ProductVersion/FileVersion. No release has been triggered. Run acceptance only after the 008 implementation and harness are complete and the rc is published.

## Verify script and master integration

PENDING-USER: after implementation, download the rc zip and `.sha256`; run `powershell -ExecutionPolicy Bypass -File scripts/verify-plans-003-009.ps1 -ArtifactZip <path> -ReportPath docs\research\003-run.md`. The harness validates/extracts `-ArtifactZip` and implements the complete E/F/G/P and D-lock assertions; see [implementation checklist](008-harness-checklist.md) and [static T0/T15 audit](008-harness-audit.md). These are implementation records, not desktop passes. Attach the final report. Master §7 #1: E11 plus `PopupViewModelListTests.WhileShown_SelectedSnippetEdited_RowUpdates_SelectionKept` and `WhileShown_SnippetAdded_AppearsInSortOrder_SelectionKept`; #2: E12 plus `WhileShown_FolderRenamed_HeadingAndChipsUpdate_SelectionKept` and D12; #3: F4; #4: C9 and C9.restart; #5: C12.free; #6: B. All integration results are PENDING-USER.

## Performance

PENDING-USER: run implemented section P against the downloaded rc and deterministic 10,000-snippet seed-42 dataset as specified in plan 008 T11. Record CPU, RAM, power plan, scale and build; P1 first open and 20 samples (each ≤300 ms); P2 four queries × two layouts × 20 samples, expected result counts and median ≤150 ms per cell, p95 and max; P3 Core benchmark is diagnostic only. Re-run a failed cell once after closing other apps, preserving both runs. No measurements have been made.

## Compatibility matrix

PENDING-USER: create P-Unicode `Héllo – naïve 👋 世界 Ωμέγα`, P-Multi (three lines, leading tab and trailing spaces), P-Plain `smart-snippets-probe`. For each target copy a sentinel, insert Unicode and multiline, replace a selected word with plain, check `Get-Clipboard` and usage counts, then perform the unsaveable clipboard Cancel/Paste anyway procedure from plan 008 T12 using the implemented `scripts/lib/Clipboard.ps1` helpers (also sourced by Uia.ps1). Record actual outcomes; terminal selection replacement is N/A and multiline warnings are noted. Elevated Notepad requires the app unelevated first, then retry after Restart as administrator.

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

## Wiring audit (T15, static pass; runtime evidence pending)

The [complete T0/T15 trace](008-harness-audit.md) covers every T9–T11 and §7 reference, every changed wiring entity, and every consumed log event. `scripts/lib/LogAudit.ps1` locates source AppLog emitters for all 51 required/consumed events; A4 invokes it and the pure test rejects missing emitters. Fixed the nonexistent `hotkey_register_failed` consumer to `hotkey_startup_failed`. Busy is returned without an insert_result log; F5 consumes the actual popup insertion gate instead.

E/F/P use the actual PopupSearch/PopupList/editor/footer names and UiText wording. E10/P1/P2 consume the real ready/render timing events. G10 aggregates logs before every profile deletion and after P, registering saved snippet text/labels, including earlier E/F profiles. F9–F11 consume the clipboard helpers with finally cleanup; F10 checks every saved pre-cap format hash plus absence of the oversized payload. User clipboard preservation is text-only, as specified.

Published-artifact checks G1–G9 are implemented, not locally claimed to have passed. Runtime appearance of events, theme/icon visuals, UAC and downloaded-rc measurements remain PENDING-USER. No release has been created.

## T13 integration record

| Master §7 | Implemented end-to-end evidence | Live/unit evidence | Runtime result |
|---|---|---|---|
| #1 | E11 | WhileShown_SelectedSnippetEdited_RowUpdates_SelectionKept; WhileShown_SnippetAdded_AppearsInSortOrder_SelectionKept | PENDING-USER |
| #2 | E12 and D12.rename/propagate/moved | WhileShown_FolderRenamed_HeadingAndChipsUpdate_SelectionKept; WhileShown_FolderDeleted_RowsShowUnfiled | PENDING-USER |
| #3 | F4.database, F4.main, F4.sort | RecordUsage and popup/main store subscriptions, full Presentation suite | PENDING-USER |
| #4 | C9.changed/newkey/restart/restore | Hotkey/settings tests | PENDING-USER |
| #5 | C12.exit/log/free | Hotkey lifetime code | PENDING-USER |
| #6 | B1–B3 | Core MigratorTests | PENDING-USER |

Local suite counts and pure-logic validation are recorded in the audit document; these do not substitute for the T13 rc run.

## Observations and insertion notes

- Lone CR: insertion contract specifies CRLF normalization. F1 and matrix confirmation PENDING-USER.
- Shortcut conflict sentence unreachable by recorder: accepted as-is by user 2026-10-28; startup conflict evidence C13/G5 PENDING-USER.
- `Snippet text cannot be empty.`: canonical wording accepted in plan 008 T16.
- Unsaveable clipboard: prompt before emptying; unit evidence above, F9–F11 and matrix PENDING-USER.
- MaybePasted: F12 is N/A externally; `InsertionServiceTests.SendThrows_IsUncertain` and `PopupViewModelInsertionTests.PasteAnyway_ThenMaybePasted_ShowsCaution` provide unit evidence, not manual acceptance.
- Residual focus race between final revalidation and SendInput accepted by user 2026-10-28 (plan 008 R12).

## Sign-off

PENDING-USER: review completed evidence and explicitly sign off after all blockers are resolved. Final tag and programme closure remain pending; do not mark the master plan Complete.
