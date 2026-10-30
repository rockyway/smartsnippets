# 013 — App identity polish

**Date:** 2026-10-29  
**Status:** In progress (approved 2026-10-29)  
**Size:** M  
**Area:** WinUI title bar and tray, executable identity, startup registration, publish/release and QA scripts  
**Requirement brief confirmed:** 2026-10-29

## 1. Goal

Simplify the normal tray tooltip, put the colored app icon beside the main window title, and deliver the executable as `SmartSnippets.exe` without breaking release checks or silently losing app-owned start-at-login on an upgrade.

## 2. Confirmed requirement

- Normal tray hover displays only “Smart Snippets”; retain the hotkey-unavailable warning.
- Add the existing colored app icon to the left of the main-window title.
- Rename the built and published executable to `SmartSnippets.exe`; update CI, release and QA consumers.
- When upgrading, update the app-owned start-at-login entry where appropriate. Document that users may need to recreate their own shortcuts or taskbar pins; do not modify them automatically.
- Acceptance: (AC1) normal and error tooltips are correct; (AC2) title icon is shown; (AC3) Native AOT publish produces the new executable and release/QA checks use it; (AC4) upgrading does not silently leave an enabled app-owned start-at-login entry pointing to the old executable.
- No changes to popup positioning, icon artwork, databases, snippet content, or user-created shortcuts are intended. The user is the sole user and accepts that old and new executable names must not run together; cross-name protection is out of scope.
- The user confirmed this brief and the warning tooltip on 2026-10-29.

## 3. Current state

- `src/SmartSnippets.App/Services/TrayIcon.cs:22-23,93-104,144-146` chooses a normal or unavailable tooltip on both add and update.
- `src/SmartSnippets.App/MainWindow.xaml:16-22` draws only title text in a custom draggable title bar; `MainWindow.xaml.cs:32-37` separately assigns the `.ico` to `AppWindow` and sets the drag region.
- `scripts/icons/generate_icons.py:80-91` generates the multi-frame ICO from SVG; WinUI should use a supported PNG rather than assume XAML can decode ICO. Preserve the existing ICO and generate a title-bar PNG from the same artwork.
- `src/SmartSnippets.App/SmartSnippets.App.csproj:2-17` has no explicit assembly name, hence the current executable name; the existing root namespace and XAML classes must remain unchanged.
- `src/SmartSnippets.App/Services/StartupRegistration.cs:13-26,33-45` considers startup enabled only if the registry value contains the current process path; enabling writes the current path and `--background`. `AppHost.cs:164` creates this service; `SettingsViewModel.cs:106` calls `IsEnabled` during composition. Without migration the renamed binary will consider the previous value disabled.
- `scripts/publish-aot.ps1:29-31`, `.github/workflows/ci.yml:45`, `.github/workflows/release.yml:57-71`, `scripts/verify-plans-003-009.ps1:106,127,166,319,332,452,501,596,1016`, and `scripts/verify-ui-qa.ps1:111,146,154,181,432` depend on the old executable, DLL or process name. `docs/guides/001-install-and-release.md:5-8` and `.github/release-notes-header.md:3` provide old-name upgrade instructions. `AGENTS.md:20` describes the publish gate.
- `src/SmartSnippets.App/Services/ElevationService.cs:26` and `StartupRegistration.cs:23,35` use `Environment.ProcessPath`; those dynamic paths do not need a fixed-name replacement. Historical plan and review records should not be rewritten.

## 4. Design

### Approach

Set `AssemblyName` to `SmartSnippets` while keeping `RootNamespace` and project path unchanged. Verify WinUI generated code and XAML compilation rather than assuming an assembly rename is harmless. Generate a 32px (or higher-density) PNG from the existing SVG using the existing deterministic icon generator; include it as a WinUI-compatible asset. Place it in the custom title bar without breaking the draggable region, theme contrast, caption buttons, or accessibility. Keep the normal tooltip change isolated from the warning state.

Migrate a legacy run value only when its raw, unexpanded value is `REG_SZ` and exactly matches the canonical quoted `SmartSnippets.App.exe` path in the current executable's directory followed by ` --background`. Compare the Windows path case-insensitively; require the canonical quoting, argument and value kind. Preserve expandable strings, other kinds, foreign paths, extra arguments and other noncanonical values. Run migration on the primary app startup, including background startup, before settings evaluates the registration; redirected activation does not create another `AppHost`. Rewrite to the current `Environment.ProcessPath` as `REG_SZ`. If the registry cannot be read or written, retain a nonfatal result, keep the prior value, and deliver an actionable recovery notice after the tray notifier is ready; if the tray cannot be added, use a fallback notification surface rather than silently discarding it. Log content-free outcome tokens, never exception messages or commands. No entry means do nothing; an already-current value is unchanged. The installation instructions must explicitly require launching the new executable once after extraction; Windows cannot migrate an entry that still launches the old binary before the new binary runs. Document cleanup of the old executable after exit and recreation of user shortcuts/pins. Do not delete the old executable automatically.

Introduce an injected registry adapter used by the actual startup coordinator. Exercise that same coordinator with an in-memory adapter and injected failures; separately verify the Windows adapter against a disposable subkey or isolated Windows user/hive. Do not add a shipping command-line option to redirect registry writes. Source-wiring checks supplement, but do not replace, executable integration tests.

**Cross-name coexistence decision (R2):** Windows App SDK may scope `AppInstance` by executable identity as well as key. The unchanged key therefore does not guarantee cross-name exclusion. The sole user accepts that old and new executables must not run together; no cross-name protection will be added. The upgrade instructions require exiting the old process, removing the old executable after the new version is installed, and updating old shortcuts and pins. Same-name single-instance behavior remains required.

Update current operational scripts, release checks and guidance. Clean or reject stale old executable outputs in publish/package paths to avoid shipping both names. Both old and new process names must be checked by QA isolation safety guards during upgrades; do not weaken ownership safeguards. Before the first app launch in every applicable acceptance section, durably journal the original Run value and registry kind; preserve the first snapshot across launches, fail closed before launch on capture failure, and restore it on normal cleanup and supervisor/watchdog recovery. Update `docs/guides/002-acceptance-harness-safety.md`. Historical evidence remains intact, but the active plan 008 R2/R14 release instructions must be explicitly superseded by a version-aware runbook referenced by the installation guide.

### Alternatives

| Option | Assessment |
|---|---|
| Rename assembly and migrate the specific legacy registration | Chosen: output identity and startup registration remain consistent. |
| Rename only the published file | Rejected: debug/build name, executable identity and runtime dependencies can diverge. |
| Replace every Run value or modify shortcuts | Rejected: could overwrite unrelated user state. |

### Sequence and parallel paths

```text
Fresh/redirected launch → AppHost startup → legacy registration migration
  → exact legacy command? yes → rewrite current exe + --background
  → absent/current/unrecognized? → leave alone
  → registry failure → retain nonfatal result → notifier ready or fallback → actionable notification
Settings initialization → IsEnabled → reads migrated/current Run value
User toggles startup → Enable/Disable → existing registry operations
Build → AssemblyName → SmartSnippets.exe → AOT gate → CI/release archive → QA
Tray add OR state update → tooltip selection → Explorer
Main window construction → XAML title bar → generated app PNG
```
Parallel paths: tray add and state update; first and redirected/background launches; startup enable/disable; debug build and AOT publish; release archive and both QA harnesses. Investigate startup ordering so migration runs before settings without relying on opening Settings.

### Surfaces

| Surface | Change | Task |
|---|---|---|
| Tray, title bar | Tooltip and icon | T1 |
| App output, startup registration | Identity and legacy migration | T2 |
| AOT gate, CI, release, QA safety and user guides | New name and upgrade safety | T3 |

## 5. Impact and risk

| Question | Answer |
|---|---|
| State | Existing Run value is the only migrated state; failed writes must not be reported as success. |
| Consistency | Migrate only an exact legacy command, preserve unrelated entries, and make repeated launches idempotent. |
| UX | Normal tooltip is shorter; retain error warning; title-bar icon must not interfere with dragging. |
| Compatibility | Old launchers and pinned items may need manual repair; first launch of the new version is required for migration. Existing data paths and single-instance key remain unchanged. |
| Recovery | Notify on registry migration failure; user can re-enable startup in Settings. Keep prior value on failure. |
| Logging | Log outcome without recording full executable paths or user data. |
| Edge cases | Disabled or foreign registration, stale previous binary, multiple processes, elevated run, read-only registry, different path, non-ASCII paths, unusual DPI, pre-existing publish files. |

**Risks:** WinUI may treat PNG content versus packaged assets differently in unpackaged/AOT builds; verify in a real publish. Registry migration must not overwrite an unrelated installation. Script process-name changes must not allow destructive QA profile isolation while either version is running. PNG generation must remain reproducible. Renaming changes assembly identity; verify XAML and AOT.

**Rollback:** revert the changes and release the old name; users can toggle start-at-login to rewrite its entry. Never delete user profile data.

## 6. Tasks

### T1 — Tooltip and title icon (AC1, AC2)
- **Files:** `src/SmartSnippets.App/Services/TrayIcon.cs`, `src/SmartSnippets.App/MainWindow.xaml`, `src/SmartSnippets.App/SmartSnippets.App.csproj`, `scripts/icons/generate_icons.py`, generated `src/SmartSnippets.App/Assets/TitleIcon.png`, presentation UI contract tests and their input list.
- **Change:** retain error tooltip, simplify normal tooltip, generate and include a title PNG from the existing app SVG, display it with a stable accessible title and draggable area.
- **Verify:** generator `--check`, UI contract tests, Windows build; assert generated `Assets/TitleIcon.png`, XAML reference, logical display size and output/publish copy metadata agree. Inspect the extracted release artifact, not the source tree, for the PNG; manual light/dark, DPI, tooltip states and title-bar drag from that artifact. Test must detect a missing image or changed error warning.

### T2 — Rename and upgrade migration (AC3, AC4)
- **Files:** app project, `Services/StartupRegistration.cs`, `AppHost.cs`, focused testable migration logic/tests and startup integration as needed.
- **Change:** set assembly name; implement the raw-value/kind migration policy through an injected adapter and invoke the production coordinator at primary startup before lazy settings initialization. Retain failures until notifier readiness, provide a fallback if tray notification is unavailable, and log no exception messages. Preserve namespace, XAML and data locations. Do not implement cross-name exclusion; document the accepted limitation.
- **Verify:** executed coordinator tests with injected adapter cover raw `REG_SZ` legacy/current, missing, foreign, `REG_EXPAND_SZ`, non-string, extra arguments, missing quotes, prefix collisions, case differences, Unicode, missing process path, denied read/write and idempotence; assert no writes for nonlegacy inputs. Windows adapter tests use a disposable subkey or isolated user, never the user's Run key. Test production invocation separately from source-wiring assertions. Fresh/background failures must preserve the old value, continue startup, deliver a recovery message even without tray, and never log an injected synthetic private token. Clean Windows debug build and AOT publish. With owned processes and an isolated profile, verify old→new, new→old, new→new redirection, background launches and elevation handoff, including actual database ownership and hotkey outcomes according to the R2 decision.

### T3 — Release and harness consumers (AC3, AC4)
- **Files:** `scripts/publish-aot.ps1`, `scripts/verify-plans-003-009.ps1`, `scripts/verify-ui-qa.ps1`, `scripts/lib/RegistryRecovery.ps1`, `scripts/lib/Watchdog.ps1`, `.github/workflows/ci.yml`, `.github/workflows/release.yml`, `.github/release-notes-header.md`, `docs/guides/001-install-and-release.md`, `docs/guides/002-acceptance-harness-safety.md`, a version-aware replacement release runbook, `AGENTS.md`, relevant harness tests.
- **Change:** change required output and managed-DLL gate names; prevent stale old binary entering release; update process checks while retaining both-name safety refusal; update archive/version assertions and upgrade instructions. Before any acceptance launch, journal the original Run value and kind durably and fail closed if capture fails, including B/G/P-only runs. Preserve the first snapshot across all launches and use it for normal and watchdog cleanup. Explicitly supersede active plan 008 R2/R14 instructions via a version-aware runbook and update the installation guide's reference; preserve historical evidence. Preserve project and namespace paths.
- **Verify:** gate tests reject managed output and stale executable; inspect clean release archive, title PNG and version; run safe script contract/pure tests; grep all active old-name references and justify intentional legacy checks. With a fake legacy registration and simulated startup writer prove capture precedes first launch, capture failure prevents launch, repeated launches preserve the first snapshot, and normal plus interrupted/watchdog recovery restore original raw value and kind. Runbook oracle distinguishes legacy A→renamed B (Run command changes) from renamed B→renamed C (current command remains unchanged); both verify version, preserved snippets/settings, enabled state and background login. Do not run interactive acceptance or UI screen harness without explicit authorization and isolation.

## 7. Test strategy

- Run Core and Presentation tests sequentially, deterministic icon check, clean Windows build and `scripts/publish-aot.ps1` AOT gate; verify zero warnings.
- Exercise the actual startup coordinator with an injected fake registry adapter, including denied operations, absent tray and privacy assertions; exercise the Windows adapter only on a disposable subkey or isolated Windows user/hive. Do not rely on `finally` alone for production Run-key isolation. Test invocation and migration decisions separately.
- Inspect the publish and packaged zip for the new exe, no old exe and no managed app DLL; validate version metadata and required native SQLite DLL. Test both old/new process guards with safe mocks.
- Manual, with an explicitly authorized UI slot: (1) hover tray in Ready and HotkeyUnavailable; verify texts. (2) Inspect title icon in light/dark themes at common DPIs and drag the window. (3) Upgrade an isolated previous installation with startup enabled, launch the new executable and verify the Run value, restart/login behavior and settings state. (4) Verify disabled and foreign values are untouched; simulate denied registry write and observe warning. (5) Restart as administrator and verify single-instance activation and startup behavior.

## Implementation progress (2026-10-29)

- Implemented tooltip/title asset, assembly rename, guarded startup migration, journal-before-launch, current release/QA consumers and version-aware upgrade guidance.
- Core tests: 432 passed. Presentation tests: 561 passed. Pure harness: 103 assertions passed, including capture-before-launch and fail-closed behavior. Icon generation `--check`, Windows build (zero warnings) and Native AOT publish passed; temporary publish contained the new executable, SQLite DLL and title PNG and no new managed DLL or old executable.
- External implementation review reported issues in failure delivery, migration error handling, production coordinator tests and release archive checks; these were addressed. The external review timed out after returning partial findings, so no full independent approval is claimed.
- **Still pending:** authorized extracted-release UI checks, startup/login and owned-process integration scenarios, proof of runtime title PNG rendering, independent full review, archive/version test and negative stale-output gate test. The coordinator is executed through an injected fake adapter and the Windows adapter is exercised on a disposable subkey; full AppHost UI execution has not been performed. Do not mark this plan complete or claim manual acceptance until these pass.

## 8. Rollout

Ship UI, executable name, migration, scripts and upgrade guidance together. Instruct users to exit the old process, extract the new release, start `SmartSnippets.exe` once, then remove the old executable and recreate shortcuts or pins if needed. Update documentation to check both executable names during transition.

## 9. Open questions

None. The user accepted the cross-name coexistence limitation. GUI/registry acceptance requires user authorization and an isolated profile.

## 10. Approval log

| Date | Event | Notes |
|---|---|---|
| 2026-10-29 | Requirement brief confirmed | Error tooltip retained; shortcut migration excluded. |
| 2026-10-29 | External review | Folded R1–R7. Review: `C:/Users/tamtr/.gemini/antigravity-cli/brain/819e5c7b-be08-44f8-b995-488b879c1912/scratch/review-013.md`. |
| 2026-10-29 | R2 decision | Sole user accepts that old and new executables must not run together; cross-name exclusion is out of scope. |
| 2026-10-29 | Plan approved (G2) | Approved by user. |
