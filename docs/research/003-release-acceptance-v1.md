# 003 — Release acceptance v1

**Status: PENDING-USER / implementation incomplete. This is not an acceptance sign-off.**

## Build under test

PENDING-USER: record accepted rc tag, commit, GitHub run, archive filename, SHA-256 and exe ProductVersion/FileVersion. No release has been triggered. Run acceptance only after the 008 implementation and harness are complete and the rc is published.

## Verify script and master integration

PENDING-USER: after implementation, download the rc zip and `.sha256`; run `powershell -ExecutionPolicy Bypass -File scripts/verify-plans-003-009.ps1 -ArtifactZip <path> -ReportPath docs\research\003-run.md`. The current harness does **not** yet support `-ArtifactZip`, E/F/G/P, or D-lock. Do not run this command until those are implemented. Attach the report and cite E11 + popup live store test (#1), E12 + folder live test (#2), F4 (#3), C9.restart (#4), C12.free (#5), B (#6). All six are PENDING-USER.

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
- Icon assets, generator, loaders, taskbar theme handling and related log events are not yet implemented. T5–T7 remain open.
- `AppHost.UpdateTrayState` is the only tray state assignment and calls the existing Presentation `TrayStatePolicy`; hotkey and popup events call it. Both subscriptions use anonymous handlers and are not explicitly unsubscribed on exit; this is an open audit finding.
- Popup calls the one `IInsertionService` contract; `InsertionService` records usage only for `Inserted`; `SnippetStore` raises `Changed`. `UiText.InsertionMessage` covers the enum including clipboard-loss and uncertain-dispatch outcomes. Relevant tests include `InsertionServiceTests.IncompleteSnapshot_DoesNotEmpty`, `OverCap_NotPreserved_NoEmptyClipboard`, `SendThrows_IsUncertain`, and `PopupViewModelInsertionTests.PasteAnyway_ThenMaybePasted_ShowsCaution`.
- `popup_ready_ms` and `popup_search_ms` are emitted by PopupWindow; E/P consumers are not implemented. New 008 tray log events are not implemented or included in A4. Runtime log evidence is PENDING-USER.
- Diagnostics callback, menu item, window and ProbeRepository were removed; prototype-only repository tests were removed while migration tests remain.

## Observations and insertion notes

- Lone CR: insertion contract specifies CRLF normalization. F1 and matrix confirmation PENDING-USER.
- Shortcut conflict sentence unreachable by recorder: accepted as-is by user 2026-10-28; startup conflict evidence C13/G5 PENDING-USER.
- `Snippet text cannot be empty.`: canonical wording accepted in plan 008 T16.
- Unsaveable clipboard: prompt before emptying; unit evidence above, F9–F11 and matrix PENDING-USER.
- MaybePasted: F12 is N/A externally; `InsertionServiceTests.SendThrows_IsUncertain` and `PopupViewModelInsertionTests.PasteAnyway_ThenMaybePasted_ShowsCaution` provide unit evidence, not manual acceptance.
- Residual focus race between final revalidation and SendInput accepted by user 2026-10-28 (plan 008 R12).

## Sign-off

PENDING-USER: review completed evidence and explicitly sign off after all blockers are resolved. Final tag and programme closure remain pending; do not mark the master plan Complete.
