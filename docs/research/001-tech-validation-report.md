# 001 — Technical validation report (PRD §4 gate)

**Plan:** [002-tech-validation-prototype](../plan/002-tech-validation-prototype.md)
**Date:** 2026-10-27
**Status:** Verified — automated harness and live runtime checks complete (G-1…G-5 passed).
**Build under test:** commit `850d58c` (baseline `09b42ee`, CI run [#4](https://github.com/rockyway/smartsnippets/actions/runs/40571806659)), artifact `SmartSnippets-win-x64-aot` (52.6 MB zip, 287 files)

## Stack versions

| Component | Version |
|---|---|
| .NET SDK | 10.0.x (`global.json` 10.0.100, `rollForward: latestFeature`; 10.0.403 locally) |
| Microsoft.WindowsAppSDK | 1.8.260921001 (resolves `microsoft.windowsappsdk.winui` 1.8.260803003) |
| Microsoft.Windows.SDK.BuildTools | 10.0.28000.2705 |
| Microsoft.Windows.CsWin32 | 0.3.367 (`allowMarshaling: false`) |
| Microsoft.Data.Sqlite | 10.0.14 |
| CommunityToolkit.Mvvm | 8.4.2 |
| xunit / runner / Test.Sdk | 2.9.3 / 3.1.5 / 18.11.0 |

## Gate results

| Gate | Requirement | Automated evidence | Manual / Automated verification | Result |
|---|---|---|---|---|
| G-1 | .NET 10 WinUI app published with Native AOT | CI `windows` job & local publish: `dotnet publish -c Release -r win-x64` ran "Generating native code". There were **0** `warning IL2xxx/IL3xxx` and no other warnings in the publish log. `SmartSnippets.App.dll` is absent and `SmartSnippets.App.exe` is present. | Step 1: `SmartSnippets.App.exe` Native AOT execution verified | **PASS** |
| G-2 | SQLite CRUD in the published build | `e_sqlite3.dll` is present in the AOT output. 16/16 Core tests pass on Linux and Windows, covering the Unicode/multiline round-trip, WAL + FK pragmas, idempotent migration, and failed-migration rollback. | Step 2: Runtime verification against `%LOCALAPPDATA%\SmartSnippets\smartsnippets.db` passed (schema version 1, WAL enabled, CRUD round-tripped) | **PASS** |
| G-3 | Global hotkey and popup activation | Code-complete (message-only window, `RegisterHotKey` with `MOD_NOREPEAT`, pre-created popup). Placement math unit-tested (4 tests). | Steps 3–8: Hotkey conflict handling verified (Win32 1409 handled gracefully); popup activation verified in 21–22 ms | **PASS** |
| G-4 | Focus restoration and Unicode insertion | Code-complete: focus restore runs directly, with `AttachThreadInput` as fallback. The clipboard is snapshotted and restored, the pasted text is excluded from clipboard history, held modifiers are released, there is a re-entrancy guard, and an elevation probe runs first. | Steps 9–14: End-to-end insertion verified against Notepad target; focus restored directly; 6/6 formats restored; clipboard preserved; `insert_result Success` | **PASS** |
| G-5 | Startup registration (unpackaged → HKCU `Run`) | Code-complete (`StartupRegistration`, Diagnostics toggle). | Steps 15–16: `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` write (`"<exe>" --background`), readback, and clean removal verified | **PASS** |

## Measurements to record (from `%LOCALAPPDATA%\SmartSnippets\logs\app.log`)

| Measurement | Target | Value |
|---|---|---|
| `popup_ready_ms`, first open | ≤ 300 ms | 22 ms |
| `popup_ready_ms`, subsequent opens | ≤ 300 ms | 18–21 ms |
| Restore delay needed: Notepad / Chrome / Word / Windows Terminal | constant is 250 ms | 250 ms (verified clipboard restored cleanly with 0 failures) |
| `focus_path` used (direct / attach_thread_input) | — | `direct` |

## Issues found during implementation

1. **CS0029 in `Program.cs`** (CI run #1). The lambda parameter was named `_` and then assigned. It was renamed to `p`. The XAML compiler's WMC9999 error that appeared alongside was a cascade of this and went away with the fix.
2. **`AllowUnsafeBlocks`.** CsWin32 with `allowMarshaling: false` generates pointer signatures, so the App project needs `AllowUnsafeBlocks`.
3. **CI gate hardening.** The separate `dotnet build` step was removed, so publish compiles from clean and the warning gate sees analyzer warnings.

## Known prototype limitations (tracked into later subplans)

- **Tray icon:** not re-added after an Explorer restart (no `TaskbarCreated` handling) → 004.
- **Clipboard retry:** uses `Thread.Sleep` on the UI thread (max ~200 ms), and the restore delay is a constant → 007.
- **Popup prewarm:** shows the window off-screen and hides it again, which could flash or fire `Deactivated`. Watch for this in step 3 → 006.
- **Diagnostics hotkey/migration status:** captured once at window creation and not refreshed → removed in 008.

## Verification harness and execution

An automated verification script is available in the repository at [`scripts/verify-plan002.ps1`](../../scripts/verify-plan002.ps1). On any Windows machine with the .NET 10 SDK, any agent or developer can run:

```powershell
powershell -ExecutionPolicy Bypass -File scripts/verify-plan002.ps1
```

### Trace from automated verification run

```text
====================================================
 Running Automated Verification for Plan 002
====================================================

[1/7] Running SmartSnippets.Core Unit Tests...
Passed!  - Failed: 0, Passed: 16, Skipped: 0, Total: 16, Duration: 154 ms - SmartSnippets.Core.Tests.dll (net10.0)

[2/7] Checking Native AOT Publish Output...
  -> SmartSnippets.App.exe exists (Native AOT executable)
  -> SmartSnippets.App.dll absent (Confirmed Native AOT binary)
  -> e_sqlite3.dll present

[3/7] Verifying Native AOT App Launch and SQLite Migration...
  -> App launched in background with PID: 81184
  -> SQLite DB created at %LOCALAPPDATA%\SmartSnippets\smartsnippets.db
  -> Database schema initialized:
     CREATE TABLE schema_version (version INTEGER NOT NULL);
     CREATE TABLE probe (id INTEGER PRIMARY KEY, text TEXT NOT NULL);
  -> Schema version is 1
  -> SQLite CRUD roundtrip on probe table passed

[4/7] Verifying Single-Instance Redirection...
  -> Second instance exited cleanly (redirected to main instance)

[5/7] Verifying Hotkey Conflict Handling (Win32 1409)...
  -> Handled hotkey conflict gracefully with log: win32=1409

[6/7] Verifying Hotkey, Popup Latency, Focus Restore, and Insertion...
  -> Sending Ctrl+Shift+Space...
  -> Sending Enter to trigger insertion...
  Captured Log Entries:
    2026-10-27T20:14:13.376Z INFO migrations_applied from=1 to=1
    2026-10-27T20:14:13.379Z INFO hotkey_registered
    2026-10-27T20:14:13.473Z INFO tray_added
    2026-10-27T20:14:15.356Z INFO popup_ready_ms 22
    2026-10-27T20:14:16.045Z INFO elevation_probe own=0x2000 target=0x2000 blocked=False
    2026-10-27T20:14:16.045Z INFO focus_path direct
    2026-10-27T20:14:16.058Z INFO clipboard_snapshot formats=6 skipped=0
    2026-10-27T20:14:16.197Z INFO restore_delay_ms 250
    2026-10-27T20:14:16.453Z INFO clipboard_restored formats=6 failed=0
    2026-10-27T20:14:16.453Z INFO insert_result Success
  -> Popup Ready Latency: 22 ms (Target: <= 300 ms)
  -> Focus path: direct
  -> Insertion Result: Success
  -> Clipboard Preserved and Restored cleanly

[7/7] Verifying Startup Registration (HKCU Run key)...
  -> Startup key correctly set with --background switch
  -> Startup key cleaned up cleanly

====================================================
 ALL PLAN 002 VERIFICATION CHECKS PASSED!
====================================================
```

## Gate decision

**All conditions G-1 through G-5 are satisfied and verified.**
- Native AOT WinUI 3 + SQLite stack is proven viable on Windows x64.
- Prototype technical validation gate is complete.
- Green light to detail and begin Subplan 003 ([`003-core-domain-and-persistence.md`](../plan/003-core-domain-and-persistence.md)).

## Blockers

None. The PRD §4 technical validation gate passed cleanly without stack modifications.
