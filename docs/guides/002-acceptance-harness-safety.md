# Acceptance harness safety

Do not run desktop acceptance while using the machine. The harness sends input. Save work first.

## Disposable target

Only **classic single-document Notepad** is supported. `-TargetExe <path-to-classic-notepad.exe>` can select a trusted installed classic binary. The default is `%WINDIR%\System32\notepad.exe`; on systems where this redirects to packaged/tabbed Notepad, acceptance refuses the target. Do not disable this check to make a run green. A replacement safe target strategy needs separate review if classic Notepad is unavailable.

The harness refuses to launch while another Notepad is running. It opens a uniquely named empty temporary file, pins the started process handle, and verifies executable path, start time, PID, one visible window, unique title and a native classic Edit child before input or termination. It never kills Notepad by process name. A launcher redirect or an unverified/changed window is left untouched: inspect and close it manually. No acceptance PASS is claimed for a refused target. Do not open other documents in an owned target while a run is active.

Smart Snippets must also be exited by the user before acceptance. Only process handles started by this run are terminated. An unowned instance blocks profile mutation instead of being killed.

The historical `verify-plan002.ps1` is retired and refuses execution; its unsafe profile/clipboard/process operations are not supported.
