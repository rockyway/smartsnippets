# 002 — Technical validation prototype (PRD §4 gate) and repository skeleton

**Date:** 2026-10-27
**Status:** In progress — code complete, CI green (AOT gate G-1); manual runbook pending (G-2…G-5)
**Size:** L (subplan of [001-master-smart-snippets-v1.md](001-master-smart-snippets-v1.md))
**Area:** repository root, `src/`, `tests/`, `.github/workflows/`, `docs/research/`
**Requirement brief confirmed:** 2026-10-27 (G1, see master §2)

---

## 1. Goal

Prove that the required stack works before any feature work begins. The published **Native
AOT, x64, unpackaged WinUI 3** build must do five things on the user's PC:

1. Start.
2. Read and write SQLite.
3. Open a popup from a global hotkey.
4. Restore focus to the target and insert Unicode multiline text by clipboard paste.
5. Register itself to start at login.

This work also leaves behind the solution skeleton and CI that every later subplan builds on.
If any gate condition fails, implementation stops and the blocker is reported (PRD §4). The stack
does not change without the user's approval.

## 2. Requirement slice (from master §2)

- **Gate conditions** (PRD §4):
  - **G-1** A .NET 10 WinUI app published with Native AOT.
  - **G-2** SQLite CRUD in that published build.
  - **G-3** Global hotkey registration and popup activation.
  - **G-4** Focus restoration and Unicode text insertion.
  - **G-5** Startup registration with the chosen packaging. That packaging is unpackaged, so
    registration uses the HKCU `Run` key.
- **Master ACs advanced here** (by spike only, not production quality):
  - AC4: the popup opens on the target's monitor with the search box focused.
  - AC9: Unicode and multiline text survive the clipboard paste, and the clipboard is restored.
  - AC10: elevated targets are detected.
  - AC12: the app keeps running after its window closes, and Exit unregisters the hotkey.
  - AC14: the AOT gate.
- **Out of this subplan:**
  - The real data model, search, editor, and settings UI. Those belong to 003–006.
  - Tray icon polish and the single-instance hand-off UX. Those belong to 004.
  - The production insertion engine. That belongs to 007.

## 3. Current state (as-built)

- Greenfield: `git ls-files` returns only `docs/requirements/001-prd.md` and this plan set.
- No `.gitignore`, solution, `global.json`, or CI exists.
- The agent environment has no .NET SDK and no Windows. Every Windows build and AOT step runs in
  GitHub Actions `windows-latest`, and the user runs the manual runbook (§7).
- **Platform facts that bite** (each one is checked by a task below, not assumed):
  - WinUI 3 supports Native AOT starting with Windows App SDK 1.6. AOT requires CsWinRT AOT
    mode and compiled bindings (`x:Bind`). Reflection-based `{Binding}` and reflection-based
    JSON break under trimming.
  - By default, a WinUI 3 app exits when its last window closes. Keeping it alive needs
    `Application.DispatcherShutdownMode = OnExplicitShutdown` (Windows App SDK 1.5+). This is
    verified in T5.
  - `AppInstance.FindOrRegisterForKey` redirect needs a custom `Main`
    (`DISABLE_XAML_GENERATED_MAIN`), so the redirect can run before the XAML app starts.
  - `SetForegroundWindow` is restricted by the foreground lock. The process that just received
    `WM_HOTKEY` has the last input event, which lets it take the foreground. Giving the
    foreground back to the target happens while we still own it.
  - UIPI (User Interface Privilege Isolation) blocks `SendInput` from a medium-integrity process
    into a high-integrity window, and nothing reports the failure. Elevation therefore has to be
    detected up front. If `OpenProcessToken` on an elevated process is denied, we treat it as
    elevated.
  - `SQLitePCLRaw.bundle_e_sqlite3` ships a native `e_sqlite3.dll`, which has to end up next to
    the AOT executable.

## 4. Design

### 4.1 Approach

Build the **real skeleton**, not a throwaway spike. Each gate capability is implemented minimally
in the namespace and class where its production version will live, behind an interface. Later
subplans extend these classes instead of rewriting them. Prototype-only UI lives in
`SmartSnippets.App/Diagnostics/` and is deleted in 008.

```
SmartSnippets.slnx
global.json                      .NET 10 SDK pin (rollForward: latestFeature)
Directory.Build.props            Nullable, ImplicitUsings, LangVersion latest, IsAotCompatible, analyzers
Directory.Packages.props         central package versions
.gitignore / .editorconfig
src/SmartSnippets.Core/          net10.0 — Data/SqliteConnectionFactory, Data/Migrator, AppPaths
src/SmartSnippets.App/           net10.0-windows10.0.19041.0, WinUI 3, unpackaged, x64, PublishAot
  Program.cs                     custom Main: single-instance check → Application.Start
  App.xaml(.cs)                  DispatcherShutdownMode=OnExplicitShutdown, service wiring
  Interop/NativeMethods.txt      CsWin32 API list (allowMarshaling=false for AOT)
  Services/HotkeyService.cs      message-only HWND + RegisterHotKey(MOD_NOREPEAT)
  Services/ForegroundTarget.cs   capture HWND/PID/monitor at WM_HOTKEY
  Services/WindowPlacement.cs    MonitorFromWindow + GetMonitorInfo → centred rect in work area
  Services/ClipboardPasteInserter.cs   save → set text → Ctrl+V → delayed restore
  Services/ElevationProbe.cs     target integrity level vs own
  Services/StartupRegistration.cs      HKCU\…\Run value "SmartSnippets" = "\"<exe>\" --background"
  Services/TrayIcon.cs           minimal Shell_NotifyIcon: Open, Exit
  Popup/PopupWindow.xaml         pre-created, hidden; TextBox + 1-item list (spike)
  Diagnostics/DiagnosticsWindow.xaml   prototype-only: SQLite probe, AOT probe, startup toggle, timings
tests/SmartSnippets.Core.Tests/  xUnit — connection factory, migrator bootstrap, CRUD probe
.github/workflows/ci.yml         Linux core tests + Windows build/test/AOT publish + warning gate
docs/research/001-tech-validation-report.md   gate evidence (G-1..G-5), blockers, measurements
```

The popup window is **created once at startup and then shown and hidden**, never re-created.
Re-creating it each time would put the 300 ms target (AC13) at risk.

### 4.2 Alternatives considered

| Topic | Option | Verdict |
|---|---|---|
| Data access | **`Microsoft.Data.Sqlite` + hand-written SQL** | **Chosen.** It is AOT-clean, small, and gives full control over the schema. |
| | EF Core + SQLite | Rejected. AOT and compiled models are still experimental, and it adds startup cost and warnings. |
| | `sqlite-net` | Rejected. Its reflection-based ORM conflicts with trimming. |
| Win32 interop | **CsWin32 source generator (`allowMarshaling: false`)** | **Chosen.** Generated, AOT-safe, typed signatures that are less error-prone than hand-written ones. |
| | Hand-written `LibraryImport` | Fallback, only if CsWin32 output raises AOT warnings. |
| Prototype shape | **Real skeleton plus a removable Diagnostics window** | **Chosen.** Nothing gets written twice. |
| | Separate throwaway spike repo/project | Rejected. Everything would be rebuilt in 003+, which doubles the AOT validation. |
| Hotkey sink | **Message-only window (`HWND_MESSAGE`) on the UI thread** | **Chosen.** It survives the main window closing. |
| | Subclass the main window's HWND | Rejected. The hotkey would die when the main window closes (AC12). |

### 4.3 Data model (prototype only)

```sql
CREATE TABLE IF NOT EXISTS schema_version (version INTEGER NOT NULL);
CREATE TABLE IF NOT EXISTS probe (id INTEGER PRIMARY KEY, text TEXT NOT NULL);  -- dropped by 003's migration 1
```

- **Pragmas:** `journal_mode=WAL`, `foreign_keys=ON`.
- **Path:** `%LOCALAPPDATA%\SmartSnippets\smartsnippets.db`, resolved through `AppPaths` and
  overridable in tests.

### 4.4 Sequence diagram — hotkey → popup → insert (spike hot path)

```
User presses Ctrl+Shift+Space in Notepad
  → OS posts WM_HOTKEY to message-only HWND (UI thread)
    → HotkeyService.HotkeyPressed
      → ForegroundTarget.Capture(): GetForegroundWindow → reject if own process → PID, monitor
      → Stopwatch start
      → PopupWindow.ShowFor(target): WindowPlacement.CenterIn(target.Monitor.WorkArea)
          → AppWindow.MoveAndResize → Show → SetForegroundWindow(popup) → SearchBox.Focus
          → Activated + first LayoutUpdated → log "popup_ready_ms" (no snippet text)
User presses Enter
  → PopupWindow.OnInvoke (re-entrancy flag set; second Enter ignored)
    → PopupWindow.Hide
    → BRANCH ElevationProbe.IsBlocked(target)?
        ├─ yes → InfoBar/MessageBox "target runs as administrator" → stop (no usage)
        └─ no  → IsWindow(target)? no → error "target window closed" → stop
                 → SetForegroundWindow(target) → wait until GetForegroundWindow == target (≤500 ms)
                 → ClipboardPasteInserter: OpenClipboard (retry) → snapshot formats → SetText(CF_UNICODETEXT,
                    + ExcludeClipboardContentFromMonitorProcessing) → release held modifiers
                    → SendInput Ctrl down, V down/up, Ctrl up → delay (spike: 250 ms, measured) → restore snapshot
                 → result Success/Failed → log result code
Escape → PopupWindow.Hide, no insertion.
Parallel paths checked: none exist yet (greenfield). The only insertion entry point is
ClipboardPasteInserter. 007 hardens this class rather than adding a second one.
```

### 4.5 Surface list

| Surface | Change | Task |
|---|---|---|
| Repository root config | new | T1 |
| `SmartSnippets.Core` + tests | new | T1, T3 |
| `SmartSnippets.App` shell, Program, App | new | T2, T5 |
| Diagnostics window (prototype-only) | new | T2, T3, T7 |
| Hotkey service, popup window, placement | new | T4 |
| Inserter, elevation probe | new | T6 |
| Startup registration | new | T7 |
| Tray icon (minimal) | new | T5 |
| CI workflow | new | T8 |
| `docs/research/001-tech-validation-report.md` | new | T9 |

## 5. Impact and risk

| Question | Answer |
|---|---|
| State management: what survives a crash mid-operation? | Only the probe table and the `Run` value. SQLite in WAL mode survives a crash. If the app crashes during insertion, the clipboard may still hold the test string. This is acceptable in the prototype, logged, and handled properly in 007. |
| Data consistency: orphans, races, double-apply? | A re-entrancy flag blocks double Enter. The probe CRUD runs in a transaction. |
| User experience: any surprise? | A second instance would fight over the hotkey. T5 adds a minimal single-instance redirect. |
| Backward compatibility | n/a. Greenfield. The probe table is dropped by 003's first migration. |
| Error recovery | A hotkey registration failure is logged and shown in Diagnostics. A failed insertion shows a message. A failed `Run` key write shows the Win32 error. |
| Logging & debugging | A plain-text log at `%LOCALAPPDATA%\SmartSnippets\logs\app.log` holds timings, result codes, and HRESULTs. It never contains snippet text. |
| Edge cases | Target closed before Enter. Target elevated. Target on a second monitor with different DPI. Clipboard locked by another app (retry 10×20 ms). Hotkey already taken by another app. |

**Risks:**
- **R1** A WinUI or Windows App SDK component emits AOT warnings → the gate fails in CI.
  Isolate the warning, try the fallback (hand-written `LibraryImport`, drop the offending API),
  and report it if it can't be resolved. Do not ship a non-AOT build.
- **R2** Focus can't be restored reliably → try `AllowSetForegroundWindow` and the
  `AttachThreadInput` fallback, and record the results in the report. If neither works, it is a
  blocker that goes to the user.
- **R3** The target pastes after the clipboard is restored (slow apps) → measure in Notepad,
  Chrome and Word during T6, and record the delay that was needed. 007 sets the final value.
- **R4** Per-monitor DPI mis-centres the popup → work in physical pixels from
  `GetMonitorInfo`, and declare PerMonitorV2 awareness in `app.manifest`.

**Rollback:** It is all new code on a feature branch, so reverting means dropping the branch.
Nothing touches the user's system except the HKCU `Run` value, which T7's toggle and the
runbook's cleanup step remove.

## 6. Tasks

### T1 — Repository skeleton and Core project (G-1 prerequisite)
- **Files:** `.gitignore` (VS/.NET; excludes `bin/ obj/ *.user appsettings.Local.json .env*` but
  keeps `.env.example`), `.editorconfig`, `global.json`, `Directory.Build.props`,
  `Directory.Packages.props`, `SmartSnippets.slnx`, `src/SmartSnippets.Core/SmartSnippets.Core.csproj`,
  `src/SmartSnippets.Core/AppPaths.cs`, `tests/SmartSnippets.Core.Tests/*.csproj`, `tests/.../AppPathsTests.cs`.
- **Change:**
  - Core targets `net10.0` with `IsAotCompatible=true` and has no Windows references.
  - The test project uses xUnit.
  - Package versions are managed centrally.
- **Verify:** `dotnet build` and `dotnet test` pass on the CI Linux and Windows jobs (T8 makes
  this automatic). If an SDK is available locally, the same commands also pass there.
- **Test that dies if this breaks:** `AppPathsTests.DatabasePath_IsUnderOverriddenRoot`.

### T2 — WinUI 3 unpackaged AOT app shell (G-1)
- **Files:** `src/SmartSnippets.App/SmartSnippets.App.csproj`, `app.manifest`, `Program.cs`,
  `App.xaml(.cs)`, `Diagnostics/DiagnosticsWindow.xaml(.cs)`.
- **Change:**
  - **csproj:** `OutputType=WinExe`, `TargetFramework=net10.0-windows10.0.19041.0`,
    `TargetPlatformMinVersion=10.0.17763.0`, `UseWinUI=true`, `WindowsPackageType=None`,
    `WindowsAppSDKSelfContained=true`, `Platforms=x64`, `RuntimeIdentifier=win-x64`,
    `PublishAot=true`, and `DISABLE_XAML_GENERATED_MAIN`.
  - **Packages:** Microsoft.WindowsAppSDK (latest stable 1.x), Microsoft.Windows.SDK.BuildTools,
    CommunityToolkit.Mvvm, Microsoft.Windows.CsWin32.
  - **Manifest:** PerMonitorV2 DPI awareness, `asInvoker`, and the supported-OS GUIDs.
  - **Diagnostics window:** shows `RuntimeFeature.IsDynamicCodeSupported`. A value of false
    proves AOT at runtime.
- **Verify:**
  - The CI Windows job runs `dotnet publish src/SmartSnippets.App -c Release -r win-x64`. It
    succeeds with **zero `warning IL2xxx/IL3xxx`** (enforced by T8's log gate).
  - The publish folder contains `SmartSnippets.App.exe` and `e_sqlite3.dll`, and does **not**
    contain `SmartSnippets.App.dll`.
  - Manual: runbook step 1.
- **Test that dies if this breaks:** the CI step "AOT publish + warning gate".

### T3 — SQLite in the AOT build (G-2)
- **Files:** `src/SmartSnippets.Core/Data/SqliteConnectionFactory.cs`, `Data/Migrator.cs`
  (bootstrap only: `schema_version` and a probe migration), `Data/ProbeRepository.cs`,
  `tests/.../SqliteProbeTests.cs`, `Diagnostics/DiagnosticsWindow` "Run SQLite probe" button.
- **Change:**
  - The connection factory turns on WAL and foreign keys.
  - The migrator applies migrations in order, each in its own transaction.
  - The probe does create, read, update and delete, and returns a result record.
- **Verify:**
  - Unit tests run against a temporary file database.
  - Manual: runbook step 2. The probe reports OK from the **published AOT exe**, and the database
    file exists under `%LOCALAPPDATA%\SmartSnippets\`.
- **Test that dies if this breaks:** `SqliteProbeTests.Crud_RoundTripsUnicodeMultiline`,
  `MigratorTests.Bootstrap_IsIdempotent`, `MigratorTests.FailedMigration_RollsBack`.

### T4 — Global hotkey and popup activation (G-3)
- **Files:** `Interop/NativeMethods.txt`, `NativeMethods.json`, `Services/HotkeyService.cs`,
  `Services/ForegroundTarget.cs`, `Services/WindowPlacement.cs`, `Popup/PopupWindow.xaml(.cs)`.
- **Change:**
  - **Hotkey:** a message-only window registers Ctrl+Shift+Space with `MOD_NOREPEAT`. A failure
    returns the Win32 error code, which Diagnostics shows.
  - **Target capture:** on `WM_HOTKEY`, capture the target window and ignore it if it belongs to
    our own process.
  - **Placement:** centre the popup inside the work area of the target's monitor, clamped to the
    monitor's bounds.
  - **Popup:** borderless and topmost. It is pre-created at startup and hidden after use, the
    search box is focused when it shows, and Esc hides it.
  - **Timing:** log the hotkey-to-ready time.
- **Verify:**
  - Unit test: `WindowPlacementTests` covers centring and clamping for a work area with an
    offset or negative origin. The placement math is a pure static class
    `src/SmartSnippets.Core/Layout/PopupPlacement.cs`, so it runs on Linux. `WindowPlacement` in
    App only supplies the monitor rectangles.
  - Manual: runbook steps 3–5, including the second monitor and the conflict case.
- **Test that dies if this breaks:** `WindowPlacementTests.CentresInWorkArea_NegativeOrigin`,
  `WindowPlacementTests.ClampsWhenPopupLargerThanWorkArea`.

### T5 — Background lifetime, minimal tray, single instance (AC12 spike)
- **Files:** `App.xaml.cs`, `Program.cs`, `Services/TrayIcon.cs`.
- **Change:**
  - **Lifetime:** `DispatcherShutdownMode=OnExplicitShutdown`, so closing the Diagnostics window
    keeps the app running.
  - **Tray:** Open and Exit. Exit unregisters the hotkey, removes the tray icon, and exits.
  - **Single instance:** a second launch redirects to the first instance through
    `AppInstance.FindOrRegisterForKey`, and the first instance shows its window.
  - **Background start:** `--background` starts without a window.
- **Verify:** manual runbook steps 6–8.
- **Test that dies if this breaks:** none automated. A UI/process lifetime check can't run in
  CI, so this is a manual-only gate and is recorded in the report.

### T6 — Focus restoration, Unicode clipboard insertion, elevation probe (G-4)
- **Files:** `Services/ClipboardPasteInserter.cs`, `Services/ElevationProbe.cs`,
  `Services/IInserter.cs`, `Popup/PopupWindow.xaml.cs`.
- **Change:** implements the §4.4 flow with the fixed test string
  `"Héllo – naïve 👋 世界\r\n\tline 2  (trailing spaces)  \r\nline 3"`:
  - **Snapshot and restore:** the clipboard snapshot copies every format that can be read as an
    HGLOBAL. Formats that can't be copied are skipped, and the count of skipped formats is
    logged.
  - **Clipboard-history exclusion:** the paste text carries
    `ExcludeClipboardContentFromMonitorProcessing`.
  - **Modifiers:** held modifiers are released before Ctrl+V.
  - **Re-entrancy:** a flag blocks double activation.
  - **Elevation probe:** compares the target's integrity level with our own. Access denied is
    treated as elevated.
- **Verify:**
  - Manual: runbook steps 9–14 (Notepad, selected-text replacement, Chrome input, Windows
    Terminal, an elevated Notepad, target closed while the popup is open, clipboard restored
    including an image).
  - Measure the restore delay needed in each app and record it in the report.
- **Test that dies if this breaks:** none automated in 002, because the Win32 boundary is
  interactive. 007 adds state-machine unit tests behind a fake boundary. The runbook results
  are the gate.

### T7 — Startup registration (G-5)
- **Files:** `Services/StartupRegistration.cs`, Diagnostics toggle.
- **Change:**
  - **Enable/disable:** writes or removes the HKCU
    `Software\Microsoft\Windows\CurrentVersion\Run` value `SmartSnippets` =
    `"<full exe path>" --background`.
  - **IsEnabled:** checks that the stored path matches the current exe path.
  - **Failure:** returns the Win32 error message.
- **Verify:** manual runbook steps 15–16 (sign out/in → tray icon present, no window; disable →
  value removed).
- **Test that dies if this breaks:** none automated (registry, Windows-only). It is the manual
  gate G-5.

### T8 — CI workflow
- **Files:** `.github/workflows/ci.yml`.
- **Change:**
  - **Triggers:** push and PR on any branch.
  - **`core-linux` job:** `ubuntu-latest`, `actions/setup-dotnet` from `global.json`, then build
    and test Core and Core.Tests.
  - **`windows` job:** `windows-latest`, build the solution, test, then
    `dotnet publish src/SmartSnippets.App -c Release -r win-x64` with the log written to a file.
    The job **fails if the log matches `warning IL[23][0-9]{3}`** or if `SmartSnippets.App.dll`
    is in the output. It uploads the publish folder as the artifact `SmartSnippets-win-x64-aot`.
- **Verify:** both jobs are green on the pushed branch. The artifact downloads and runs
  (runbook step 1 uses it).
- **Test that dies if this breaks:** the workflow itself. A deliberately added reflection call
  (tried locally on a throwaway branch, never pushed) must turn the warning gate red.

### T9 — Validation report and gate decision
- **Files:** `docs/research/001-tech-validation-report.md`.
- **Change:**
  - Record each of G-1..G-5 as pass or fail, with evidence: CI run link, runbook results,
    timings, restore delays, and any AOT warnings encountered.
  - Record the Windows App SDK and .NET versions used.
  - List the blockers.
- **Verify:** the user reviews the report. Any failed gate goes back to the user before 003
  starts. Changing the stack needs their approval.
- **Test that dies if this breaks:** n/a (document).

### T10 — Wiring audit (lite)
- Trace every new service from construction (App startup) to its consumer:
  - HotkeyService → PopupWindow.
  - PopupWindow → IInserter.
  - TrayIcon Exit → HotkeyService.Unregister.
  - StartupRegistration → Diagnostics.
  - Migrator → App startup.
- Flag anything that has no caller.
- **Verify:** the log lines `hotkey_registered`, `migrations_applied`, `popup_ready_ms`,
  `insert_result`, and `exit_unregistered` all appear in `app.log` during the runbook.

## 7. Test strategy

- **Unit (CI, Linux + Windows):** `AppPaths`, connection factory pragmas, migrator idempotency and
  rollback, Unicode/multiline probe round-trip, window placement math.
- **Deliberately not automated in 002:** hotkey, focus, clipboard, registry, and tray. They are
  interactive Win32 behaviour, so the manual runbook below covers them. 007 adds tests for the
  insertion state machine behind a fake Win32 boundary.
- **Manual runbook** (user's PC, the CI artifact `SmartSnippets-win-x64-aot`):

| # | Step | Expected |
|---|---|---|
| 1 | Unzip the artifact and run `SmartSnippets.App.exe` | Diagnostics window opens, showing "Dynamic code supported: False" |
| 2 | Click "Run SQLite probe" | "CRUD OK" appears, and `%LOCALAPPDATA%\SmartSnippets\smartsnippets.db` exists |
| 3 | Focus Notepad, press Ctrl+Shift+Space | Popup appears centred on Notepad's monitor and the search box has the caret. `popup_ready_ms` in the log is ≤300 |
| 4 | Move Notepad to a second monitor (if any) and repeat | Popup centres on that monitor and stays within its work area |
| 5 | With the app closed, register Ctrl+Shift+Space in another tool, then start the app | Diagnostics shows "hotkey in use" with the Win32 error, and the app does not crash |
| 6 | Close the Diagnostics window, then press the hotkey | Popup still opens. The tray icon is present |
| 7 | Launch the exe a second time | The existing instance shows its window, and no second process remains |
| 8 | Tray → Exit, then register Ctrl+Shift+Space in another tool | The process is gone and the other tool registers the hotkey successfully |
| 9 | Copy an image to the clipboard. In Notepad, press the hotkey, then Enter | The test string is inserted once with accents, emoji, CJK, tab and line breaks intact. After about 1 s the clipboard holds the image again |
| 10 | Select a word in Notepad, then hotkey → Enter | The selection is replaced by the test string |
| 11 | Repeat in a Chrome/Edge text area and in Windows Terminal (PowerShell) | Text is inserted. Record any line-break or paste-warning behaviour |
| 12 | Open the popup and press Enter twice quickly | Text is inserted exactly once |
| 13 | Run Notepad as administrator, then hotkey → Enter | A message says the target is elevated. Nothing is inserted |
| 14 | Open the popup over Notepad, close Notepad with the mouse, press Enter | An error says the target window was closed. Nothing is inserted anywhere else |
| 15 | Diagnostics → enable start at login, sign out, sign in | The tray icon appears and no window opens |
| 16 | Disable start at login | The `Run` value `SmartSnippets` is gone (check in `regedit`) |

## 8. Rollout / phasing

T1 → T2 → T8 go first, so CI exists from the second push onward. T3, T4 → T5, T6 and T7 can
proceed in any order after T2. T9 and T10 come last. Everything ships on
`claude/epic-allen-07hez6` (or a branch the user designates). Nothing is released.

## 9. Open questions

None. The latest stable versions of the Windows App SDK and packages are pinned when T1/T2 are
implemented and recorded in the T9 report.

## 10. Approval log

| Date | Event | Notes |
|---|---|---|
| 2026-10-27 | Requirement brief confirmed (G1) | 2 rounds of questions |
| 2026-10-27 | Plan approved (G2) | implementation ordered same day (Sonnet subagents) |
| 2026-10-27 | T1–T8, T10 implemented; T9 report drafted | CI run #4 green on `09b42ee`; wiring audit: every new service has a caller, all 5 required log events emitted, no snippet text logged; see `docs/research/001-tech-validation-report.md` |
