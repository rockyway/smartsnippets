# 004 — App shell, lifecycle, and settings

**Date:** 2026-10-27
**Status:** Draft — awaiting approval
**Size:** L (subplan of [001-master-smart-snippets-v1.md](001-master-smart-snippets-v1.md))
**Area:** `src/SmartSnippets.App/`, new `src/SmartSnippets.Presentation/`, new `tests/SmartSnippets.Presentation.Tests/`, `src/SmartSnippets.Core/Settings/`, `.github/workflows/ci.yml`, `SmartSnippets.slnx`
**Depends on:** 002 (Complete), 003 (planned API: `ISnippetStore`, `AppSettings`, `SettingsRepository`)
**Requirement brief confirmed:** 2026-10-27 (G1, master §2)

> Detailed in the same pass as 003 and 005 at the user's request (2026-10-27). It references
> 003's **planned** API (003 §4.3–4.5). If 003's implementation changes that API, update this
> plan before starting 004.

---

## 1. Goal

This turns the prototype into the real background app:
- **Main window:** a Snippets/Folders/Settings window replaces the Diagnostics window. Closing it
  hides it rather than quitting.
- **Tray icon:** a complete tray icon menu.
- **Hotkey:** you can change the global hotkey without ever being left with none registered.
- **Settings:** theme and text size apply live, and start-at-login is a setting.
- **Elevation:** "Restart as administrator" for inserting into elevated windows.

005 then fills the Snippets and Folders pages. 006 reuses the shell services.

## 2. Requirement slice (from master §2)

- **AC4, configuration part:**
  - The hotkey can be configured with a shortcut recorder.
  - Conflicts are explained.
  - A failed change keeps the previous hotkey.
  - "Restore default" sets it back to Ctrl+Shift+Space.
- **AC10:** a manual "Restart as administrator" command. UAC prompts for it. Start at login stays
  un-elevated.
- **AC11:**
  - Theme is Light, Dark or System, and System follows Windows changes live.
  - Text size is Small 12, Medium 14, Large 18, or Custom 10–32. It applies to snippet lists and
    editable content without a restart and without clipping.
  - Start at login is a toggle, and a failure is explained.
  - Login start runs in the background.
- **AC12:**
  - Closing the main window keeps the hotkey working.
  - Tray Exit unregisters the hotkey and quits.
  - Only one instance runs.
- **AC3, settings part:** all of the above survive a restart through 003's `AppSettings`.
- **PRD §3.4:** the tray menu has Open main window, Settings, and Exit.
- **Out of scope:**
  - Snippet and folder page content (005).
  - Popup features (006).
  - The insertion engine (007).
  - Removing the Diagnostics window (008; until then it moves to a tray item).

## 3. Current state (as-built at `7d32a8e`)

- **`App/App.xaml.cs:33-86`, `OnLaunched`:** runs migrations, creates `MessageWindow`,
  `HotkeyService`, `PopupWindow` and `TrayIcon`, and shows the Diagnostics window unless
  `Program.StartInBackground`. This is the composition root, and it's all inline.
- **`App/App.xaml.cs:55-57`:** the hotkey is hard-coded to `MOD_CONTROL|MOD_SHIFT|MOD_NOREPEAT +
  VK_SPACE`.
- **`App/Services/HotkeyService.cs:21`:** `private const int HotkeyId = 1`. There is a single ID,
  so a "register new, then drop old" swap is impossible as written.
  - `Register` returns `HotkeyRegistration(Success, Win32Error)` (`:8-17`).
  - `HotkeyPressed` is `event Action<long>` (`:33`).
- **`App/Services/TrayIcon.cs`:**
  - The menu has only `CommandOpen=1` and `CommandExit=2` (`:14-15`, `:97-98`).
  - The flags are `NIF_MESSAGE|NIF_ICON|NIF_TIP`, with no balloon support (`:33`).
  - There is no `TaskbarCreated` handling, which is a known limitation in the 002 report.
- **`App/Services/StartupRegistration.cs`:** `IsEnabled/Enable/Disable` return
  `StartupResult(Success, Message)`. It's public and works (002 G-5). Reuse as-is.
- **`App/Program.cs`:**
  - `--background` sets `StartInBackground`.
  - The single-instance key is `"SmartSnippets.Main"`, and a redirect runs on an MTA worker
    thread.
  - Nothing coordinates with a process that is about to exit, which a restart-as-admin needs.
- **`App/App.xaml`:** only `XamlControlsResources`. No theme or size resources yet.
- **`App/Diagnostics/DiagnosticsWindow`:** the only window the tray's Open shows today.
- **`.github/workflows/ci.yml`:** both jobs run `dotnet test tests/SmartSnippets.Core.Tests`
  only. A new test project needs adding there.
- **Platform facts:**
  - **Theme:** `Application.RequestedTheme` can only be set before the first window exists. At
    runtime the theme is set per window through the root `FrameworkElement.RequestedTheme`.
    `ElementTheme.Default` follows the Windows setting live. The title bar's caption buttons need
    `AppWindow.TitleBar` colours set separately.
  - **Text size:** most WinUI control templates take their font size from
    `{ThemeResource ControlContentThemeFontSize}` and ignore an inherited `FontSize`. So text size
    has to be bound explicitly on the list and editor elements PRD §3.9 names, not set once at
    the root.
  - **Closing a window:** cancelling `AppWindow.Closing` (`args.Cancel = true`) and calling
    `AppWindow.Hide()` keeps the window and its state alive.
  - **Taskbar restart:** Explorer broadcasts the registered message `"TaskbarCreated"` when it
    restarts, and tray icons have to be re-added at that point.
  - **UAC cancel:** `ShellExecuteEx` with the `runas` verb returns `ERROR_CANCELLED` (1223)
    when the user declines.

## 4. Design

### 4.1 Approach

- **New `SmartSnippets.Presentation` project** (`net10.0`, cross-platform). It references Core
  and CommunityToolkit.Mvvm.
  - **Contents:** all view-models (004 Settings, 005 Snippets/Folders/Editor, 006 Popup) and the
    service *interfaces* they depend on.
  - **App side:** App implements those interfaces with Win32/WinUI and keeps only views.
  - **Why:** view-models can then be unit-tested on Linux CI with fakes, where WinUI can't run.
    It also stops view-model logic leaking into code-behind.
- **`AppHost`** (in App) replaces the inline composition in `App.OnLaunched`. In order, it:
  1. runs migrations
  2. builds `SnippetStore` (003) and calls `Load()`
  3. loads `AppSettings`
  4. builds the Win32 services
  5. registers the saved hotkey
  6. builds the windows
  7. adds the tray icon
  8. shows the main window unless started in the background

  `App` keeps only lifetime events. There is exactly one composition root.
- **Window model:**
  - **`MainWindow`** has a `NavigationView` with Snippets, Folders and Settings. 004 provides the
    Snippets and Folders pages as placeholders, and 005 fills them.
  - **Closing:** closing the main window cancels the close and hides the window instead. Before
    hiding, it asks the active page `IGuardedPage.CanLeaveAsync()`. 005 uses this for the
    unsaved-changes prompt.
  - **Diagnostics** moves to a tray item until 008.
- **Shell services** live in Presentation as interfaces, implemented in App:

| Interface (Presentation) | Implementation (App) | Purpose |
|---|---|---|
| `IHotkeyManager` | `HotkeyManager` over `HotkeyService` | `Current`, `TryChange(HotkeyGesture) → HotkeyChangeResult`, `RestoreDefault()`, `Status` |
| `IStartupRegistration` | existing `StartupRegistration` (add interface) | toggle start at login |
| `IElevation` | `ElevationService` | `IsElevated`, `RestartAsAdministrator() → ElevationResult` |
| `IThemeService` | `ThemeService` | `Apply(ThemeMode)`, `Register(Window)`; pushes the theme to all windows and their title bars |
| `ITextSizeService` | `TextSizeService` (in Presentation, pure) | observable `FontSize`, set from the preset or custom value; views bind with `x:Bind` |
| `IDialogService` | `DialogService` (ContentDialog) | `ConfirmAsync`, `AskSaveDiscardCancelAsync`, `ShowErrorAsync`; 005 uses it heavily |
| `IUiDispatcher` | `DispatcherQueue` wrapper | marshal back to the UI thread |
| `INotifier` | tray balloon (`NIF_INFO`) | background notices, e.g. "hotkey could not be registered at startup" |

### 4.2 Alternatives considered

| Topic | Option | Verdict |
|---|---|---|
| Where view-models live | **New `SmartSnippets.Presentation` project (net10.0)** | **Chosen.** They can be unit-tested in Linux CI, and the view-model/view split is enforced by project references. |
| | In `SmartSnippets.App` | Rejected. They'd only be testable on Windows, and only with WinUI loaded. |
| | In Core | Rejected. That mixes MVVM toolkit code with the domain. |
| Hotkey change | **Register the new gesture under a spare ID; only on success unregister the old one, then persist** | **Chosen.** There's never a moment with no working hotkey, and a failure keeps the old one (PRD §3.4). |
| | Unregister the old one first, then register the new one | Rejected. If the new one fails, re-registering the old one can itself fail if another app grabbed it in between. |
| Saved hotkey fails at startup | **Keep running unregistered; show a tray balloon and a Settings banner with the reason; don't silently swap in the default** | **Chosen.** The default may conflict too, and changing your setting silently would be surprising. |
| | Silently fall back to the default | Rejected. The behaviour would change without you knowing. |
| Text size mechanism | **`TextSizeService.FontSize` bound with `x:Bind` on lists and editor text** | **Chosen.** Works despite templates using `ControlContentThemeFontSize`, and covers exactly the PRD §3.9 surfaces. |
| | Override `ControlContentThemeFontSize` in app resources | Rejected. ThemeResources don't re-evaluate live, so it would need a restart (AC11 forbids that). It would also enlarge every control. |
| Theme mechanism | **Per-window root `RequestedTheme` + title bar colours, via `ThemeService`** | **Chosen.** Live switching works, and System (`ElementTheme.Default`) follows Windows on its own. |
| Restart as admin | **`ShellExecuteEx(runas, exe, "--after-pid <pid> [--background]")`; on success the current instance exits cleanly; the new one waits ≤ 5 s for that PID before claiming the single-instance key** | **Chosen.** No race over the instance key or the hotkey, and a cancelled UAC prompt leaves the current instance running. |
| | Launch elevated and exit at once, with no hand-off | Rejected. The new process could find the old key still registered and redirect to a dying instance. |

### 4.3 Data / config model

- **New in Core, `Core/Settings/HotkeyGesture.cs`:**
  - `readonly record struct HotkeyGesture(uint Modifiers, uint Key)` with `Default` (Ctrl+Shift,
    Space), `IsValid` and `ToDisplayString()`.
  - **Validity rule:** at least one of Ctrl, Alt or Win must be held. Shift on its own is not
    enough, because Shift+letter would block normal typing. The key must not be a modifier.
  - **Display names:** a pure lookup table from virtual-key code to name covers A–Z, 0–9, F1–F24,
    Space, Enter, Tab, arrows, Insert/Delete/Home/End/PgUp/PgDn, the punctuation keys, and the
    numeric keypad. Any other key shows as `VK_0x..`.
  - `AppSettings` (003) stores it as `hotkey.modifiers` and `hotkey.key`, which 003 §4.3 already
    defines.
- **`MOD_NOREPEAT`** is always added when registering and is never stored.
- **Text sizes:** `TextSizePreset` maps Small to 12, Medium to 14 and Large to 18. Custom uses
  `text_size.custom`, clamped to 10–32 (003 T4).
- **New command-line arguments:**
  - `--background`, which already exists.
  - `--after-pid <int>`, used only by the restart-as-admin hand-off.
- **No schema change.**

### 4.4 Sequence diagrams

**Change hotkey (Settings page):**
```
ShortcutRecorder (KeyDown, modifiers) → SettingsViewModel.CandidateGesture
  → IsValid? no → inline "Add Ctrl, Alt or Win" (no registration attempt)
  → Apply → IHotkeyManager.TryChange(g)
      → HotkeyService.Register(spareId, g) ── fail(1409) → result Conflict("Ctrl+Alt+K is used by another app") → old stays
                                          └─ ok → Unregister(activeId) → swap ids → AppSettings.Save(hotkey) → result Ok
  → ViewModel shows result; Current updates; popup trigger unchanged (same HotkeyPressed event)
```

**Start-up:**
```
Program.Main → (--after-pid? wait ≤5 s for exit) → single-instance key → Application.Start → App.OnLaunched
  → AppHost.Start: Migrator → SnippetStore.Load → AppSettings.Load → ThemeService/TextSizeService.Apply
    → HotkeyManager.Register(saved) ── fail → INotifier balloon + Status for Settings banner
    → PopupWindow (prewarm) → MainWindow (created hidden) → TrayIcon.Add
    → StartInBackground? no → MainWindow.Show(Snippets)
```

**Close and exit:**
```
MainWindow X → AppWindow.Closing → active IGuardedPage.CanLeaveAsync() → false: cancel, stay
                                                                      → true : cancel close, Hide()
Tray Exit → (MainWindow visible & guard says no? → bring to front, abort exit)
          → HotkeyManager.UnregisterAll → log exit_unregistered → Tray.Dispose → windows Close → Exit()
```

**Parallel paths checked:**
- **Today:** the hotkey is registered only in `App.xaml.cs:55` and the window is shown only in
  `ShowDiagnostics`. 004 moves both into `AppHost`, after which they are the only paths.
- **Second launch:** the single-instance redirect (`App.xaml.cs:79`) will show `MainWindow`, not
  Diagnostics.

### 4.5 Surface list

| Surface | Change | Task |
|---|---|---|
| `SmartSnippets.Presentation` + `.Tests` projects, slnx, CI | new | T1 |
| `Core/Settings/HotkeyGesture.cs` + tests | new | T2 |
| `HotkeyService` (multi-id), `HotkeyManager` | change/new | T3 |
| `AppHost`, `App.xaml.cs`, `Program.cs` (`--after-pid`) | change/new | T4 |
| `TrayIcon` (menu, balloon, TaskbarCreated) | change | T5 |
| `MainWindow` + placeholder pages, `IGuardedPage` | new | T6 |
| `ThemeService`, `TextSizeService`, App.xaml resources | new | T7 |
| `SettingsPage` + `SettingsViewModel`, `ShortcutRecorder` control | new | T8 |
| `ElevationService` | new | T9 |
| `DialogService`, `UiDispatcher`, `Notifier` | new | T4/T6 |
| `NativeMethods.txt` (ShellExecuteEx, RegisterWindowMessage, NIF_INFO) | change | T3/T5/T9 |

## 5. Impact and risk

| Question | Answer |
|---|---|
| State management: what survives a crash mid-operation? | Settings are saved only after a successful apply. A crash during a hotkey change leaves the old saved gesture, which is registered on the next start. |
| Data consistency | A hotkey swap is register-then-unregister, so there's never a gap. Settings writes are single upserts. |
| User experience | Closing the window no longer quits, and the tray tooltip says "Smart Snippets — running in background". On first launch, a one-time balloon says closing keeps it in the tray. This is stored as setting key `shell.close_hint_shown` (new key; 003's settings table is a key-value store). |
| Backward compatibility | Settings are empty on existing installs, so defaults apply and match today's hard-coded hotkey. The Diagnostics window stays reachable from the tray. |
| Error recovery | Hotkey conflict: explained, and the old one stays. Start-at-login failure: message shown, toggle reverts (existing pattern). UAC cancel: info message, keeps running. Theme or size errors can't happen (pure property sets). |
| Logging & debugging | Log events: `hotkey_changed`, `hotkey_change_conflict win32=…`, `hotkey_startup_failed`, `theme_applied`, `restart_elevated_started`, `restart_elevated_cancelled`, `tray_readded`. No user content. |
| Edge cases | Covered: registering the gesture that's already active (no-op OK), recording only a modifier (ignored until a key is pressed), Esc during recording (cancels), Explorer restart, a second launch while hidden, a second launch while elevated, exiting while the editor has unsaved changes (the guard asks), custom size 10 and 32 (no clipping, checked in the runbook), switching to Dark while the popup is visible. |

**Risks:**
- **R1** `ContentDialog` needs a `XamlRoot`, which is missing when the main window is hidden.
  `DialogService` targets the main window and shows it first if it's hidden. The popup's own
  dialogs are 006's concern.
- **R2** `NavigationView` and `ContentDialog` under Native AOT. These are built-in controls, so
  they should be fine, but the CI warning gate catches it either way. A failure is a blocker that
  goes to you.
- **R3** Inherited `FontSize` doesn't reach templated controls. That's why it's bound explicitly
  (§4.2). 005 and 006 must bind `FontSize` on their lists and editors, and their plans include
  that.

**Rollback:** revert the commits. Settings rows that have no reader are harmless.

## 6. Tasks

### T1 — Presentation projects and CI (enables testing for AC4, AC10, AC11)
- **Files:**
  - `src/SmartSnippets.Presentation/SmartSnippets.Presentation.csproj` (net10.0,
    IsAotCompatible, refs Core and CommunityToolkit.Mvvm)
  - `tests/SmartSnippets.Presentation.Tests/*.csproj`
  - `SmartSnippets.slnx`
  - `App.csproj`, which gets a ProjectReference to Presentation
  - `.github/workflows/ci.yml`, where both jobs also test Presentation.Tests
- **Verify:**
  - `dotnet test` runs both test projects locally.
  - The CI windows job still passes the AOT publish with 0 IL warnings, which proves the
    toolkit's source generators are AOT-clean.
- **Test that fails if this breaks:** the CI step itself, plus a smoke test
  `PresentationSmokeTests.ToolkitObservablePropertyRaisesChanged`.

### T2 — HotkeyGesture (AC4)
- **Files:** `Core/Settings/HotkeyGesture.cs` and `tests/.../Settings/HotkeyGestureTests.cs`.
  `AppSettings.Hotkey` becomes `HotkeyGesture`, a small change to 003's T4 type.
- **Tests that fail if this breaks:**
  - `HotkeyGestureTests.Default_IsCtrlShiftSpace`
  - `HotkeyGestureTests.ShiftOnly_Invalid`
  - `HotkeyGestureTests.ModifierAsKey_Invalid`
  - `HotkeyGestureTests.Display_CtrlAltK`
  - `HotkeyGestureTests.UnknownVk_DisplaysHex`

### T3 — Hotkey manager (AC4, AC12)
- **Files:**
  - `App/Services/HotkeyService.cs`: register and unregister by an `int id` parameter; one
    `HotkeyPressed` event for both IDs.
  - `App/Services/HotkeyManager.cs`
  - `Presentation/Services/IHotkeyManager.cs`, which also defines `HotkeyChangeResult` and
    `HotkeyStatus`
- **Change:** the §4.4 swap. Error 1409 becomes the message "Already used by another
  application"; other error codes become "Windows refused this shortcut (error N)".
- **Verify:**
  - Unit tests on `SettingsViewModel` with a fake `IHotkeyManager` (see T8).
  - Manual: runbook steps 3–6.
- **Test that fails if this breaks:** `SettingsViewModelTests.ApplyConflict_KeepsCurrent_ShowsReason`
  (T8). The Win32 swap itself is manual-only (runbook step 4).

### T4 — AppHost composition and startup (AC12, AC3)
- **Files:**
  - `App/AppHost.cs`
  - `App/App.xaml.cs`, which shrinks to delegating to AppHost
  - `App/Program.cs`, adding `--after-pid`
  - `App/Services/{DialogService,UiDispatcher,Notifier}.cs`
  - `Presentation/Services/{IDialogService,IUiDispatcher,INotifier}.cs`
- **Change:** §4.4 start-up, close and exit sequences.
  - `ISnippetStore` and `AppSettings` are created once and passed to the view-models through
    constructors. No DI container is needed.
- **Verify:**
  - CI AOT build is green.
  - Runbook steps 1, 2, 9 and 10.
  - `app.log` shows `store_loaded count=N` and `settings_loaded`.
- **Test that fails if this breaks:** the runbook, plus the T11 wiring audit's log check.

### T5 — Tray menu, balloon, Explorer restart (PRD §3.4, AC12)
- **Files:** `App/Services/TrayIcon.cs` and `NativeMethods.txt` (`RegisterWindowMessage`).
- **Change:**
  - **Menu items:** Open Smart Snippets, Settings, a separator, Restart as administrator (hidden
    when already elevated), Diagnostics (prototype, removed in 008), a separator, Exit.
  - **Clicks:** left-click opens the main window.
  - **Notifications:** `ShowBalloon(title, text)` using `NIF_INFO`.
  - **Explorer restart:** when the `TaskbarCreated` message arrives, re-add the icon and log
    `tray_readded`.
- **Verify:** runbook steps 9, 11 and 12.

### T6 — MainWindow shell (AC12)
- **Files:**
  - `App/MainWindow.xaml(.cs)` (NavigationView with Snippets, Folders, Settings)
  - `App/Pages/{SnippetsPage,FoldersPage}.xaml` placeholders saying "Coming in 005"
  - `Presentation/Navigation/IGuardedPage.cs`
- **Change:**
  - **Close:** cancel the close, then hide after the guard.
  - **Navigation:** `ShowPage(PageKind)` opens a page, used by tray Settings and the second-launch
    redirect.
  - **Window:** the size and position are remembered for the session only.
  - **Theme:** the window registers with `ThemeService`.
- **Verify:** runbook steps 9 and 10.

### T7 — Theme and text size (AC11)
- **Files:**
  - `App/Services/ThemeService.cs`
  - `Presentation/Services/{IThemeService,ITextSizeService,TextSizeService}.cs`
  - `tests/.../TextSizeServiceTests.cs`
- **Change:**
  - **Theme:** `ThemeService` sets each registered window's root `RequestedTheme` and its
    title-bar button colours. System maps to `Default`.
  - **Text size:** `TextSizeService` raises `FontSize` whenever the preset or custom value
    changes.
  - **Bindings:** the placeholder pages and the Settings preview bind `FontSize` now; 005 and
    006 bind theirs.
- **Tests that fail if this breaks:**
  - `TextSizeServiceTests.PresetsMapTo12_14_18`
  - `TextSizeServiceTests.CustomClampedTo10_32`
  - `TextSizeServiceTests.RaisesPropertyChangedOnce`
- **Manual:** runbook steps 7 and 8, including a live change of the Windows theme.

### T8 — Settings page (AC4, AC11, AC10 entry)
- **Files:**
  - `Presentation/ViewModels/SettingsViewModel.cs`
  - `App/Pages/SettingsPage.xaml(.cs)`
  - `App/Controls/ShortcutRecorder.xaml(.cs)`, which captures `KeyDown` with modifiers. Esc
    cancels, Backspace clears, and screen readers announce it as "Shortcut, press keys".
  - `tests/.../SettingsViewModelTests.cs`
- **Change:**
  - **Sections:** Appearance (Theme radio buttons, Text size radio buttons plus a NumberBox for
    Custom, and a preview line), Startup (Start at login toggle), Shortcut (recorder, Apply,
    Restore default, status line), Advanced (Restart as administrator, disabled when elevated).
  - **Saving:** every change saves immediately; there is no Save button.
  - **Errors:** shown in an InfoBar on the page.
- **Tests that fail if this breaks:**
  - `SettingsViewModelTests.ThemeChange_AppliesAndPersists`
  - `SettingsViewModelTests.CustomSize_InvalidInput_Clamped`
  - `SettingsViewModelTests.StartupToggleFailure_RevertsAndShowsMessage`
  - `SettingsViewModelTests.ApplyConflict_KeepsCurrent_ShowsReason`
  - `SettingsViewModelTests.ApplyInvalidGesture_NoRegistrationAttempt`
  - `SettingsViewModelTests.RestoreDefault_RegistersDefault`
  - `SettingsViewModelTests.LoadsPersistedValues`

### T9 — Restart as administrator (AC10)
- **Files:**
  - `App/Services/ElevationService.cs`
  - `Presentation/Services/IElevation.cs`
  - `Program.cs` (`--after-pid` wait)
  - `NativeMethods.txt` (`ShellExecuteEx`, `SHELLEXECUTEINFOW`, `WaitForSingleObject`)
- **Change:** §4.2.
  - `IsElevated` uses the token-elevation check `ElevationProbe` already has.
  - Error 1223 returns `Cancelled` with no message box beyond an InfoBar.
  - The new process is started with `--background` when the main window was hidden.
- **Verify:** runbook steps 13 and 14.
- **Test that fails if this breaks:** `SettingsViewModelTests.RestartCancelled_ShowsInfo_StaysRunning`
  (fake `IElevation`).

### T10 — Startup hotkey failure and first-close hint
- **Files:** `AppHost`, `SettingsViewModel` (status banner).
- **Change:** §4.2 behaviour when the saved hotkey fails at startup, plus the one-time close hint.
- **Verify:** runbook step 5.

### T11 — Wiring audit (L)
- Trace every new interface to its implementation and the place that constructs it in
  `AppHost`.
- Every tray command reaches a handler.
- `TextSizeService.FontSize` has at least one `x:Bind` consumer.
- `IGuardedPage` is implemented by the placeholders, which always allow leaving; 005 replaces
  them.
- **Verify:** these log lines appear during the runbook: `store_loaded`, `settings_loaded`,
  `hotkey_registered`, `hotkey_changed`, `theme_applied`, `tray_readded`,
  `restart_elevated_started`, `exit_unregistered`.

## 7. Test strategy

- **Unit tests (Linux + Windows CI):** `HotkeyGesture`, `TextSizeService`, `SettingsViewModel`
  with fakes for every service interface. That's about 20 tests.
- **Integration:** the CI AOT publish of the composed app proves the AOT side. The run-time
  composition is covered by the manual runbook, because WinUI windows can't run in CI.
- **Manual runbook** (your PC, CI artifact):

| # | Step | Expected |
|---|---|---|
| 1 | Launch the exe normally | Main window opens on Snippets (placeholder). The tray icon is present |
| 2 | Close the main window with X, then press Ctrl+Shift+Space | The window hides and the one-time balloon appears. The popup still opens |
| 3 | Settings → record Ctrl+Alt+K → Apply | Status shows "Ctrl+Alt+K". Ctrl+Alt+K opens the popup and Ctrl+Shift+Space no longer does |
| 4 | Register Ctrl+Alt+J in another tool, then record Ctrl+Alt+J → Apply | Message says it's used by another application. Ctrl+Alt+K still works |
| 5 | Exit. With Ctrl+Alt+K held by another tool, start the app | Balloon says the shortcut couldn't be registered. The Settings banner explains. The app keeps running |
| 6 | Restore default | Ctrl+Shift+Space works again and the setting persists after a restart |
| 7 | Theme: Light → Dark → System, then switch the Windows theme | All windows and title bars switch at once. In System mode they follow Windows live |
| 8 | Text size Small/Medium/Large/Custom 10 and 32 | The preview and the placeholder lists resize live. Nothing is clipped or overlapping at 32 |
| 9 | Tray menu: Open, Settings, Diagnostics | Each opens the right window or page |
| 10 | Launch the exe again while the window is hidden | The existing window appears, and there's still only one process |
| 11 | Restart `explorer.exe` from Task Manager | The tray icon comes back |
| 12 | Tray → Exit, then register Ctrl+Shift+Space in another tool | The process is gone and the hotkey is free |
| 13 | Settings → Restart as administrator → accept UAC | One elevated process remains, and the hotkey still works. Inserting into an elevated Notepad works (via the 002 prototype popup) |
| 14 | Repeat and cancel UAC | An info message appears and the original instance keeps running |
| 15 | Start-at-login toggle on, sign out and in | The app starts in the background, unelevated, with no window |

## 8. Rollout / phasing

- **Track A** (Presentation side: T1 → T2 → T7 → T8) and **Track B** (App side: T3 → T5 → T6 →
  T9) can run in parallel once T1 has landed.
- **T4** integrates them.
- **T10 and T11** come last.
- Each merged track is pushed so CI checks the AOT publish.

## 9. Open questions

None blocking. These are design calls for you to confirm at approval:
1. **Saved hotkey fails at startup:** the app keeps running with no hotkey, shows a balloon and a
   Settings banner, and does **not** fall back to the default on its own.
2. **Shortcut rule:** it must include Ctrl, Alt or Win. Shift alone is rejected.
3. **Settings save immediately**, with no Save button.
4. **Diagnostics stays in the tray menu until 008.**
5. **The main window remembers its size and position for the session only**, not across
   restarts.

## 10. Approval log

| Date | Event | Notes |
|---|---|---|
| 2026-10-27 | Requirement brief confirmed (G1) | master §2 |
| | Plan approved (G2) | |
