# Acceptance harness safety

Do not run desktop acceptance while using the machine. The harness sends input. Save work first.

## Disposable target

Only **classic single-document Notepad** is supported. `-TargetExe <path-to-classic-notepad.exe>` can select a trusted installed classic binary. The default is `%WINDIR%\System32\notepad.exe`; on systems where this redirects to packaged/tabbed Notepad, acceptance refuses the target. Do not disable this check to make a run green. A replacement safe target strategy needs separate review if classic Notepad is unavailable.

The harness refuses to launch while another Notepad is running. It opens a uniquely named empty temporary file, pins the started process handle, and verifies executable path, start time, PID, one visible window, unique title and a native classic Edit child before input or termination. It never kills Notepad by process name. A launcher redirect or an unverified/changed window is left untouched: inspect and close it manually. No acceptance PASS is claimed for a refused target. Do not open other documents in an owned target while a run is active.

Smart Snippets must also be exited by the user before acceptance. Only process handles started by this run are terminated. An unowned instance blocks profile mutation instead of being killed.

## Duplicate input

F5 does not race global Enter/click input against a disappearing popup. It captures and validates the popup's native keyboard and row input HWNDs, queues targeted window messages, and requires the corresponding keyboard/row duplicate-gate log from the view-model. Exact Notepad text is required (an extra newline fails). F5.hotkey independently tests hotkey suppression. If a WinUI/provider version does not deliver the targeted messages to the routed input handlers, F5 fails; never replace it with an underlying-window click or count an unrelated hotkey guard as success. This new desktop driver remains subject to the user-run acceptance, not proven by the pure message-plan tests.

## Clipboard

E/F preserve **text only**. Rich formats/images/files are not backed up. Acquisition must succeed before any fixture overwrites the clipboard; a failed read is never interpreted as an empty clipboard. Reads, writes and restores retry contention for up to 50 attempts with 100 ms between failures, then fail explicitly without echoing payloads. Clipboard/provider calls themselves are native synchronous calls, covered by the worker watchdog described below. Finally blocks restore a validated snapshot on ordinary failure/interrupt. Force-killing PowerShell, closing the terminal, logout or power loss cannot run finally or recover an in-memory clipboard snapshot: save valuable clipboard text elsewhere before starting. A final restoration failure is a failed run, not successful preservation.

## Supervisor, timeouts and recovery

The public command starts a separate `-STA` worker in the same PowerShell edition. An out-of-process supervisor holds an exclusive `%LOCALAPPDATA%\SmartSnippets.verify.lock` file handle, so two runs cannot race the move-aside/restore. A stale **file** is harmless; only an open exclusive handle blocks another run. Do not invoke the internal worker parameter yourself.

Native menu/theme sends are bounded (menu/direct send: 2 seconds). A polling timeout does not bound a synchronous UIA provider call. The supervisor therefore kills **only its worker process handle** after `-WatchdogTimeoutSeconds` without progress (default 600 seconds; range 30–3600). This also bounds UIA calls/properties outside polling helpers and long native calls. Build/import commands and the explicit R11 retry prompt consume this budget too: answer the retry prompt within the budget. No separate worker thread can continue sending input after the worker is killed. The supervisor keeps an independent in-memory clipboard-text snapshot for E/F and restores it after worker exit or watchdog termination. Killing the supervisor itself defeats this backstop.

On an ordinary failure the worker's finally stops owned processes and restores the profile. On watchdog termination, terminal closure, power loss, or interruption before finally can finish, **do not immediately rerun**:

1. Inspect and close the acceptance Smart Snippets instance and the uniquely named `SS-acceptance-<guid>.txt` target. Save any unrelated work first. No global taskkill command is recommended. A watchdog does not blindly terminate app/target processes or move a profile out from under a running app.
2. Confirm Smart Snippets is no longer running. Locate `%LOCALAPPDATA%\SmartSnippets.verify-backup` (the original profile) and `%LOCALAPPDATA%\SmartSnippets` (the test profile). If a backup exists, move the **test** directory to a distinct quarantine name first. Do not delete or merge the backup. Only when `SmartSnippets` does not exist, rename `SmartSnippets.verify-backup` to `SmartSnippets` in the same parent. Moving the backup into an existing `SmartSnippets` directory would nest it and is wrong.
3. If no backup exists, do not assume the active directory is disposable: the original profile may have been absent or final restoration may have completed. Inspect before deleting anything. Retain quarantined test logs for investigation, without publishing raw payloads.
4. The next run refuses a leftover backup. Restart from the public command only after recovery; no acceptance PASS is inferred from an aborted worker. An aborted supervisor writes a FAIL report when `-ReportPath` is supplied.

The historical `verify-plan002.ps1` is retired and refuses execution; its unsafe profile/clipboard/process operations are not supported.
