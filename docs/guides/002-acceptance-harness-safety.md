# Acceptance harness safety

Do not run desktop acceptance while using the machine. The harness sends input. Save work first.

## Disposable target

Only **classic single-document Notepad** is supported. `-TargetExe <path-to-classic-notepad.exe>` can select a trusted installed classic binary. The default is `%WINDIR%\System32\notepad.exe`; on systems where this redirects to packaged/tabbed Notepad, acceptance refuses the target. Do not disable this check to make a run green. A replacement safe target strategy needs separate review if classic Notepad is unavailable.

The harness refuses to launch while another Notepad is running. It opens a uniquely named empty temporary file, pins the started process handle, and verifies executable path, start time, PID, one visible window, unique title and a native classic Edit child before input or termination. It never kills Notepad by process name. A launcher redirect or an unverified/changed window is left untouched: inspect and close it manually. No acceptance PASS is claimed for a refused target. Do not open other documents in an owned target while a run is active.

Smart Snippets must also be exited by the user before acceptance. Only process handles started by this run are terminated. An unowned instance blocks profile mutation instead of being killed.

## Duplicate input

F5 does not race global Enter/click input against a disappearing popup. It captures and validates the popup's native keyboard and row input HWNDs, queues targeted window messages, and requires the corresponding keyboard/row duplicate-gate log from the view-model. Exact Notepad text is required (an extra newline fails). F5.hotkey independently tests hotkey suppression. If a WinUI/provider version does not deliver the targeted messages to the routed input handlers, F5 fails; never replace it with an underlying-window click or count an unrelated hotkey guard as success. This new desktop driver remains subject to the user-run acceptance, not proven by the pure message-plan tests.

## Clipboard

E/F preserve **text only**. Rich formats/images/files are not backed up. Acquisition must succeed before any fixture overwrites the clipboard; a failed read is never interpreted as an empty clipboard. Reads, writes and restores retry contention for up to 50 attempts with 100 ms between failures, then fail explicitly without echoing payloads. Clipboard/provider calls themselves are native synchronous calls, covered by the worker watchdog described below. Finally blocks restore a validated snapshot on ordinary failure/interrupt. The supervisor also keeps a DPAPI-encrypted text backup for its Windows user (never plaintext on disk). Force-killing the supervisor, closing the terminal, logout or power loss cannot run finally; save valuable clipboard text elsewhere before starting. A final restoration failure is a failed run, not successful preservation.

## Evidence scope

Only `-PerfLibrary 10000` can produce P0/P1/P2 acceptance PASS evidence. Other sizes emit `Diagnostic.P*` IDs and INFO on success, explicitly marked NON-ACCEPTANCE (failures remain FAIL). A zero exit code for a diagnostic run is not AC13 sign-off.

G10 scans all archived profiles for saved snippet text/labels and each disposable target's unique title token/observed titles. This detects title-only leaks and changed-title prefixes without printing titles. Diagnostics show bounded allowlisted event names and line numbers only; raw log/exception payloads are withheld. This is fixture-based evidence, not proof that every possible private string can never leak.

## Supervisor, timeouts and recovery

The public command starts a separate `-STA` worker in the same PowerShell edition. An out-of-process supervisor holds an exclusive `%LOCALAPPDATA%\SmartSnippets.verify.lock` file handle, so two runs cannot race the move-aside/restore. A stale **file** is harmless; only an open exclusive handle blocks another run. Do not invoke the internal worker parameter yourself.

Native menu/theme sends are bounded (menu/direct send: 2 seconds). A polling timeout does not bound a synchronous UIA provider call. The supervisor therefore kills **only its worker process handle** after `-WatchdogTimeoutSeconds` without progress (default 600 seconds; range 30–3600). This also bounds UIA calls/properties outside polling helpers and long native calls. Build/import commands and the explicit R11 retry prompt consume this budget too: answer the retry prompt within the budget. No separate worker thread can continue sending input after the worker is killed. For E/F the supervisor acquires a DPAPI-encrypted clipboard-text snapshot through a separate clipboard-only subprocess **before** launching the UIA worker. Both snapshot acquisition and backstop restoration are bounded to 15 seconds (plus up to 5 seconds to stop the helper). This covers delayed-rendering/owner callbacks inside clipboard APIs, rather than hanging the supervisor itself. Failed acquisition refuses launch; failed restoration makes the supervisor fail and retain the encrypted file at the path printed in its warning. This backstop runs after worker exit or watchdog termination. Killing the supervisor itself defeats automatic restoration.

The profile move is inside the protected try. Cleanup distinguishes a completed isolation, an interruption immediately after a successful rename (backup present and active absent), and a failed/ambiguous move. It never deletes an active original profile after a failed move, and never merges ambiguous directories. On an ordinary failure the worker's finally stops owned processes and restores the profile. On watchdog termination, terminal closure, power loss, or interruption before finally can finish, **do not immediately rerun**:

1. Inspect and close the acceptance Smart Snippets instance and the uniquely named `SS-acceptance-<guid>.txt` target. Save any unrelated work first. No global taskkill command is recommended. A watchdog does not blindly terminate app/target processes or move a profile out from under a running app.
2. Confirm Smart Snippets is no longer running. Locate `%LOCALAPPDATA%\SmartSnippets.verify-backup` (the original profile) and `%LOCALAPPDATA%\SmartSnippets` (the test profile). If a backup exists, move the **test** directory to a distinct quarantine name first. Do not delete or merge the backup. Only when `SmartSnippets` does not exist, rename `SmartSnippets.verify-backup` to `SmartSnippets` in the same parent. Moving the backup into an existing `SmartSnippets` directory would nest it and is wrong.
3. If no backup exists, do not assume the active directory is disposable: the original profile may have been absent or final restoration may have completed. Inspect before deleting anything. Retain quarantined test logs for investigation, without publishing raw payloads.
4. If a clipboard recovery file was retained, run `powershell -NoProfile -STA -ExecutionPolicy Bypass -File scripts/clipboard-worker.ps1 -Mode Restore -BackupPath <the-retained-clipboard.clixml>` as the **same Windows user**, after clipboard contention is resolved. Exit 0 means the restore operation succeeded; do not print/publish the text or file. Retain the file on failure and remove its temporary directory only after successful recovery. DPAPI binds its text to that user; it is not a portable public report. A killed supervisor may leave an `ss-worker-*` directory in the user's temp directory; inspect its encrypted `clipboard.clixml` with the same procedure rather than assuming clipboard preservation.
5. The next run refuses a leftover backup. Restart from the public command only after recovery; no acceptance PASS is inferred from an aborted worker. An aborted supervisor writes a FAIL report when `-ReportPath` is supplied.

The historical `verify-plan002.ps1` is retired and refuses execution; its unsafe profile/clipboard/process operations are not supported.
