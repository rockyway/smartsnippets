# 011 — Selectable color palettes

**Date:** 2026-10-29
**Status:** In progress — cold-start palette smoke passed; live/high-contrast acceptance pending
**Size:** M
**Area:** Core settings, Presentation settings, WinUI appearance and tests
**Requirement brief confirmed:** 2026-10-29 (G1)

## 1. Goal

Let users select a persistent color palette that updates the application's backgrounds and accents live, independently of its light/dark mode.

## 2. Requirement (confirmed at G1)

- **Problem:** Settings offers Light, Dark and System, but no color palette choice.
- **Goal:** Personalize the app with a selectable color palette.
- **In scope:** Default, Ocean, Forest and Plum; backgrounds and accents across the main window and popup; independent Light/Dark/System selection; immediate application and persistence; Windows high contrast takes precedence while retaining the choice.
- **Out of scope:** Custom color pickers, user-created palettes, import/export, changing Windows system theme or accent.
- **Users and surfaces:** Settings selector; main window, pages and dialogs; popup.
- **Behavior and assumptions:** Existing users get Default and the current appearance. Non-default palettes have light and dark variants. Invalid saved values fall back to Default. System mode still follows Windows live.
- **Acceptance criteria:**
  1. AC1: Settings offers all four named palettes independently of Light/Dark/System.
  2. AC2: Changing palette updates main window and popup without restart.
  3. AC3: All palettes work with Light, Dark and System, including live Windows theme changes.
  4. AC4: Choice survives restart; missing/invalid values use Default.
  5. AC5: Windows high contrast takes precedence and the selected palette returns after it is disabled.
  6. AC6: Text, selection states, menus and dialogs remain legible across variants.
- **Decisions:** User chose backgrounds plus accents, the four proposed names, and high-contrast precedence.

## 3. Current state (pre-implementation discovery snapshot)

Citations in this section refer to the baseline before this feature; implementation details and validation are recorded in §11.

- `src/SmartSnippets.Core/Settings/AppSettings.cs:11,32-58` loads typed settings and saves shell keys via the existing key-value store; `SettingsKeys.cs:5` defines the existing theme key. No schema migration is needed for a new key. `SaveShell` intentionally does not overwrite popup preferences.
- `src/SmartSnippets.Presentation/ViewModels/SettingsViewModel.cs:15,95,123-131,161-166` maps the existing theme radio order, initializes from settings, and applies/saves changes. `src/SmartSnippets.App/Pages/SettingsPage.xaml:120-131` and `SettingsPage.xaml.cs:34-55` implement the corresponding selector and rebinding guard.
- `src/SmartSnippets.App/Services/ThemeService.cs:15-93` registers windows, applies root `RequestedTheme`, and tracks `ActualThemeChanged` for title bars. System mode uses `ElementTheme.Default`. `src/SmartSnippets.App/AppHost.cs:144,211,238,276-285` constructs the service, registers the main window and initial popup before prewarm, and registers replacement popups. These are all register call sites found by grep.
- `src/SmartSnippets.App/App.xaml:12-65` owns light/default/high-contrast custom brushes and selection resources; high contrast uses system colors. Main and popup roots use `SolidBackgroundFillColorBaseBrush` (`MainWindow.xaml:10`, `Popup/PopupWindow.xaml:10-11,36`). Pages and editor also use WinUI layer, card, control, and accent resources; menus use a deliberately opaque presenter (`App.xaml:68-74`). Dialogs are created through `AppHost.cs:164,229` for the main window and popup respectively; `DialogService.cs:99-103` attaches them to a XamlRoot. Template resources and flyouts must be verified, not assumed to inherit a root brush.
- `src/SmartSnippets.App/Services/FlyoutSurface.cs:15-83` sets the opened presenter background imperatively to hard-coded light/dark brushes and logs one `menu_surface` receipt; palette dictionary changes alone cannot override this. Popup and snippets sort and row context menus call `KeepSolid` (`PopupWindow.xaml.cs:53,360`, `SnippetsPage.xaml.cs:30,248`).
- `src/SmartSnippets.App/Services/WindowSurface.cs:30-35` detects high contrast; popup border logic uses it (`PopupWindow.xaml.cs:124-132`). Preserve native frame and focus/prewarm policies. Plan 009 §8 chose system accent for the original appearance; Default preserves that choice, while the newly approved palettes intentionally override it.
- `tests/SmartSnippets.Core.Tests/Settings/AppSettingsTests.cs:17-110` and `tests/SmartSnippets.Presentation.Tests/SettingsViewModelTests.cs:36-81,227-236,310-315` already cover defaults, round-trip, shell saves and theme application. WinUI rendering and AOT require Windows verification.

## 4. Design

### 4.1 Approach

Extend the existing theme service rather than introducing a second independently registered window service. Store a separate `ColorPalette` enum, and expose palette application in `IThemeService`; leave `ThemeMode` semantics unchanged. Settings selects a named value, applies immediately and persists using the existing shell settings path. AppHost passes the loaded palette before registering either window; all later popup replacements inherit current service state.

Use **application-scope stable mutable `SolidColorBrush` instances** for palette-controlled tokens: both windows share one mode and palette, and app-scope resources reach both roots and popup-layer consumers. Construct the brushes once before window creation; bind controlled XAML and template resource keys directly to those instances, not to reinserted dictionary entries. Update their `Color` in place on palette/mode/system-color changes so already-realized controls retain the same brush references. Replace the existing `StaticResource` selection aliases with direct aliases to these stable controlled brushes; overriding only `AccentFillColorDefaultBrush` cannot update those pre-resolved aliases. Audit WinUI template resource lookups and explicitly wire any cached or popup-local brush that does not consume the app resource; the Windows visual matrix is a release gate for this architecture, not evidence assumed from unit tests.

Baseline ownership is explicit: `Controls/PaletteBaseline.xaml(.cs)` consumes unmodified WinUI primitive Color resources under a locally requested Light or Dark theme. These collapsed, non-focusable consumers are attached to the retained main root and notify the theme service when their source colors change; this avoids ambiguous cross-dictionary StaticResource baseline lookups. Original fixed custom folder/menu tokens are retained before replacing their entries. Default reads current accent shades through `UISettings.GetColorValue`, rather than trusting an unconsumed retained framework brush. Copy baseline colors and opacities into stable brush instances on refresh; never freeze a system color at startup. High contrast bypasses all custom palette colors even in forced Light/Dark, using the current system high-contrast brush values. Preserve default WinUI text and semantic error colors where contrast is sufficient; supply paired accent foregrounds. Retain popup DWM frame and focus/prewarm behavior. If Windows validation shows baseline lookup or a template cannot update correctly, stop and revise the plan rather than silently sacrificing live updates or Default behavior.

Template aliases for normal buttons, text inputs, combo boxes, navigation content/selection, and menu hover/pressed states are explicitly controlled too. Stable gradient stops preserve the original WinUI focus-border geometry. Text inputs retain native white selected text, paired with the saturated light accent in both theme modes; dark-mode button/list selections instead use the pale accent with near-black text. Stable acrylic brushes preserve baseline acrylic properties for navigation/dropdowns in Default and use opaque palette fallbacks for custom choices; window backdrops are never changed. Active dialogs register with ThemeService only for the duration of ShowAsync and receive subsequent RequestedTheme changes; open flyout presenters also receive current RequestedTheme.

`ThemeService` owns a `UISettings.ColorValuesChanged` subscription (existing popup uses that signal), marshals callbacks to the UI dispatcher, re-queries `WindowSurface.IsHighContrast()`, refreshes stable brushes and existing popup frame/title-bar treatment, and unsubscribes on shutdown. This signal is independent of `ActualThemeChanged`, which alone cannot detect high-contrast changes in forced Light/Dark. Recompute on registration, explicit mode/palette changes and `ActualThemeChanged` as well. Use no high-contrast inference from `ElementTheme`. Also observe `WM_SETTINGCHANGE`, `WM_SYSCOLORCHANGE` and `WM_THEMECHANGED` on the existing hidden top-level MessageWindow, without reading user-provided payloads. Coalesce refreshes on the UI dispatcher at low priority and unsubscribe both notification paths on shutdown. Verify actual notification delivery and high-contrast on/off and system accent changes in all three theme modes during authorized visual acceptance.

`FlyoutSurface.MakeSolid` must use the shared live menu brush for non-default palettes, retaining existing Default light/dark behavior; in high contrast explicitly use the current system window brush rather than trusting `ActualTheme`. Keep `SystemBackdrop = null` and exactly one content-free `menu_surface` receipt per opening. An open presenter holds the stable brush so palette changes update it without reopening. Audit both sort and row context flyouts on both roots.

Controlled resource audit (verify actual WinUI template keys on Windows): `SolidBackgroundFillColorBaseBrush`, `SolidBackgroundFillColorQuarternaryBrush`, `LayerFillColorDefaultBrush`, `CardBackgroundFillColorSecondaryBrush`, `SubtleFillColorSecondaryBrush`, `SubtleFillColorTertiaryBrush`, `ControlFillColorDefaultBrush`, `MenuSolidBrush`, `MenuFlyoutPresenterBackground`, `ListViewItemBackgroundSelected`, `ListViewItemBackgroundSelectedPointerOver`, `ListViewItemBackgroundSelectedPressed`, `ListViewItemForegroundSelected`, `AccentFillColorDefaultBrush`, `AccentFillColorSecondaryBrush`, `AccentFillColorTertiaryBrush`, `TextOnAccentFillColorPrimaryBrush`, `TextOnAccentFillColorSecondaryBrush`, `AccentTintBrush`, `AccentButtonBackground`, `AccentButtonBackgroundPointerOver`, `AccentButtonBackgroundPressed`, `AccentButtonForeground`, `FolderTintBrush`, and `FolderTintBackgroundBrush`. Do not globally replace `SystemAccentColor` (a Color, not a mutable brush); use the stable accent brush explicitly for controlled usages, while baseline Default keeps following the system accent. Preserve semantic and high-contrast resources. Include any additional template-state keys exposed by the Windows audit, with paired foregrounds and contrast checks.

Initial opaque background / accent anchors (ARGB omitted = FF):

| Palette | Light background | Light accent | Dark background | Dark accent |
|---|---|---|---|---|
| Ocean | `#F2F8FC` | `#005A9E` | `#101F2B` | `#8CCBFA` |
| Forest | `#F3F8F3` | `#28613B` | `#14241B` | `#9BD9AD` |
| Plum | `#F9F4FA` | `#753C83` | `#241925` | `#DFB5E8` |

Derive additional surface/hover/pressed colors explicitly per variant in the implementation, not by arbitrary runtime blending. Pair light accents with white text and dark accents with near-black text; verify contrast for actual rendered colors and states. Normal text should meet WCAG 2.2 AA 4.5:1, and meaningful control boundaries/states 3:1 against adjacent colors; do not claim compliance based solely on anchors. Keep semantic danger colors distinguishable and high-contrast system-driven. Adjust derived values if verification fails.

### 4.2 Alternatives

| Option | Verdict |
|---|---|
| Reuse ThemeService with separate palette setting and stable application-scope brushes | **Chosen:** one lifecycle owner, live changes and recovered windows. |
| Accent-only override | Rejected: does not satisfy confirmed background requirement. |
| Separate full window themes or restart for every change | Rejected: duplicates lifecycle and breaks live application/System behavior. |
| Change Windows system accent | Rejected: affects other applications and is out of scope. |

### 4.3 Data / API

Add `ColorPalette { Default, Ocean, Forest, Plum }` in Core and `SettingsKeys.ColorPalette = "color_palette"`; `AppSettings.ColorPalette` defaults to Default. Use existing `ReadEnum` validation and `SaveShell`; no migration. Extend `IThemeService` with current palette and `ApplyPalette(ColorPalette)`, retaining existing `Apply(ThemeMode)`. Update fake implementations. Normalize out-of-range UI indices to Default and avoid writes during page binding. Never log user text; palette names are allowlisted.

### 4.4 Sequence

```text
Settings radio → SettingsPage guarded selection → SettingsViewModel.SelectedPalette
  → IThemeService.ApplyPalette → mutate shared stable app brushes → live window and popup visuals
  → AppSettings.SaveShell → settings key-value store
Restart → AppSettings.Load → ThemeService(initial mode, palette) → register main + popup before prewarm
ActualThemeChanged / UISettings.ColorValuesChanged → UI dispatcher → query high contrast + effective mode → update stable brushes from palette or live baseline system colors
Popup recovery → AppHost.EnsurePopup → ThemeService.Register → current palette before show
```

Parallel paths: existing theme selector remains separate; initial popup and recovered popup must both be registered. Main and popup dialog services use different XamlRoots; verify both, along with flyout presenter resources. Popup preference writes must not overwrite shell settings, and shell writes must not overwrite popup preferences.

### 4.5 Surfaces

| Surface | Change | Task |
|---|---|---|
| Settings persistence | Enum, default, save/load | T1 |
| Settings page / view-model | Selector, live apply, binding | T2 |
| Main and popup, including recovered popup | Theme-aware resources, initial/current palette | T3 |
| Menus, editor and dialogs on both roots | Audit resource propagation and contrast | T3, T4 |
| Tests and acceptance checks | Persistence, event wiring, Windows appearance | T1–T4 |

## 5. Impact and risk

| Question | Answer |
|---|---|
| State management | Every change is saved immediately; an interruption before save can leave the prior saved palette, restored next start. |
| Data consistency | Separate existing key-value row; shell save must preserve popup layout/sort and vice versa. All UI resource work remains on UI thread. |
| User experience | Default retains existing look; new choices change the app only, not Windows. |
| Backward compatibility | Missing or unknown values fall back to Default; no DB schema change. |
| Error recovery | Existing settings failure behavior remains; invalid values recover to Default. Default remains selectable. |
| Logging | Existing settings-invalid logging includes key only; log palette enum only if needed, never snippet text or window title. |
| Edge cases | Repeated selections, invalid enum, startup, closed/recreated popup, active dialog/flyout, live system and high-contrast toggles. |

**Risks:** WinUI `ThemeResource`/template caching may prevent live updates or preserve old values; test actual rendered surfaces and revise resource technique before completion. High contrast can override palettes differently in controls and popups: test Windows transitions, not just a mocked flag. Inadequate contrast or opaque-menu regressions block completion. XAML and Native AOT need a Windows build and publish.

**Rollback:** revert feature changes; unused key is harmless. Users can select Default before rollback.

## 6. Tasks

### T1 — Persist palette (AC1, AC4)
- **Status:** Implemented; Core tests passed (432/432).
- **Files:** `Core/Settings/ColorPalette.cs`, `AppSettings.cs`, `SettingsKeys.cs`, `tests/SmartSnippets.Core.Tests/Settings/AppSettingsTests.cs`.
- **Change:** Add enum, key, load and shell-save; preserve other settings.
- **Verify:** `dotnet test tests/SmartSnippets.Core.Tests`.
- **Tests:** default/missing, all four round trips, unknown/numeric-invalid fallback, shell save leaves popup keys intact; update `Keys_AreExact` for the new key.

### T2 — Selection contract and Settings (AC1, AC2, AC4)
- **Status:** Implemented, including Settings XAML, UI text and automation properties; view-model tests and Windows compiled bindings pass.
- **Files:** `Presentation/Services/IThemeService.cs`, `Presentation/ViewModels/SettingsViewModel.cs`, App `Pages/SettingsPage.xaml(.cs)`, `Presentation/Text/UiText.cs`, presentation fakes and `SettingsViewModelTests.cs`.
- **Change:** Separate palette property and selection mapping with rebinding guard, immediate apply/save, initialize from saved setting; preserve independent theme selection and other preference values. Add section header `Color palette`, labels Default/Ocean/Forest/Plum, `x:Name="ColorPaletteRadios"`, and `AutomationProperties.Name="Color palette"`; use UI text constants consistent with existing bindings.
- **Verify:** `dotnet test tests/SmartSnippets.Presentation.Tests` after Core tests; Windows build validates x:Bind.
- **Tests:** initial value, each index and invalid index, apply and persistence, independent theme, shell vs popup settings, no accidental selection writes during initialization where testable.

### T3 — Runtime palette rendering (AC2, AC3, AC5, AC6)
- **Status:** Implemented. Windows build and Native AOT publish pass with zero warnings. Isolated cold-start captures for all four palettes in Light and Dark pass; live/system/high-contrast visual acceptance remains pending.
- **Files:** `App/Services/ThemeService.cs`, `App/Services/FlyoutSurface.cs`, `App/Services/PaletteResources.cs`, `App/Services/DialogService.cs`, `App/Services/WindowSurface.cs`, `AppHost.cs`, `App/Controls/PaletteBaseline.xaml(.cs)`, `Presentation/Services/PaletteColors.cs`, and `PaletteTones.cs`. Existing application resource keys are overridden before window creation; their original XAML consumers remain unchanged.
- **Change:** Implement §4 stable app-scope brushes, direct selection aliases, baseline restoration and OS color event lifecycle; apply before prewarm and on recovery. Update `FlyoutSurface` to use the current live menu brush and explicit high-contrast system brush while preserving opaque background and one receipt per opening. Audit every listed token and both dialog roots.
- **Verify:** `dotnet build` and `dotnet publish src/SmartSnippets.App -c Release -r win-x64 -o publish` on Windows; no warnings or AOT gate failures. Run §7 visual matrix.
- **Tests:** pure palette mapping, high-contrast precedence/restore, and contrast calculations where feasible; Windows runbook catches resource-resolution and visual regressions that pure tests cannot. Do not substitute unit tests for actual WinUI verification.

### T4 — End-to-end wiring and acceptance (AC1–AC6)
- **Status:** Repository integration, source wiring/contrast tests, architecture/spec audits and external review complete. Eight isolated cold-start Light/Dark smoke runs passed; live selection, System, high-contrast and full visual acceptance remain pending.
- **Files:** Core/Presentation integration tests, `scripts/verify-ui-qa.ps1` only if safe extension is needed, this plan.
- **Change:** Add integration test using real settings repository and SettingsViewModel with fake OS theme boundary: select, persist, reload, switch theme and retain both preferences. Audit all registration sites, UI bindings and resource consumers; record Windows visual results and contrast checks.
- **Verify:** run Core and Presentation tests sequentially, Windows build/AOT publish, and §7 runbook; report any unexecuted manual steps explicitly. UI screen automation only with the authorized isolated harness in `docs/guides/002-acceptance-harness-safety.md`.
- **Tests:** integration test fails if selector-to-store-to-reload or independent mode path breaks; Windows runbook fails if registration, actual resources, high contrast or recovered popup breaks.

## 7. Test strategy

Unit-test settings and mappings cross-platform; integration-test the real settings repository and view-model with the OS/rendering service at the boundary. Visual WinUI behavior is not proven by a fake. Test projects run sequentially to avoid shared Core build collisions. Windows Native AOT publish must meet CI gates (no IL2xxx/IL3xxx, exe and SQLite native DLL present, no App DLL).

| # | Manual Windows step | Expected |
|---|---|---|
| 1 | Open Settings, select each palette under Light then Dark | Main window, pages, popup, selections and accents update immediately and distinctly; Default restores existing appearance. |
| 2 | For every palette select System, switch Windows light/dark and back | Both windows switch variants live, without resetting selection. |
| 3 | For each variant inspect rows, editor, controls, opaque sort/context menus and dialogs on both roots | Readable text and selection/hover/pressed states; measured contrast meets §4.1 targets. |
| 4 | Select a non-default palette, exit via tray, restart; then open popup | Choice persists and appears before popup is revealed; text size and popup preferences remain intact. |
| 5 | In each of Light, Dark and System, enable Windows high contrast with a non-default palette selected, then disable it; also change Windows accent with Default selected | System colors take precedence including open menus and popup frame; selected palette returns; Default tracks changed system accent without restart. |
| 6 | Exercise popup close/recovery and open it again | Replacement uses current palette without exposing an unfinished surface or changing insertion/focus behavior. |
| 7 | Change theme/palette with popup and a menu/dialog open | Existing visible surfaces update; no stale brushes, translucent menu or title-bar/frame regression. |

Manual UI steps require the safe authorized screen slot and isolated profile; unchecked steps are recorded and block claiming full acceptance.

## 8. Rollout

Ship persistence, UI and rendering together on `feature/color-theme-selection`; no migration or feature flag. Land through PR; do not commit directly to main.

## 9. Open questions

None blocking. Derived surface/state colors are implementation details constrained by the listed anchors and contrast requirements.

## 10. Approval log

| Date | Event | Notes |
|---|---|---|
| 2026-10-29 | Requirement brief confirmed (G1) | User confirmed palette scope, choices and high-contrast precedence. |
| 2026-10-29 | Independent plan review folded | `docs/review/011-plan-review.md`: addressed FlyoutSurface, concrete resource ownership and high-contrast notifications, resource inventory, tests, UIA and citations. Windows runtime behavior remains an explicit acceptance gate. |
| 2026-10-29 | Plan approved (G2) | User approved. |
| 2026-10-29 | Implementation started | T1 implemented; subsequent implementation continued through all code tasks. |
| 2026-10-29 | Implementation and automated verification | Core 432/432, Presentation 534/534; Windows build 0 warnings/errors; Native AOT publish and output gate pass. |
| 2026-10-29 | External implementation review | Three read-only Codex passes; findings addressed, final pass found no remaining definite defects. See `docs/review/012-color-palettes-implementation-review.md`. Visual matrix not executed without authorized screen slot. |

| 2026-10-29 | Authorized desktop smoke | Two startup failures discovered and fixed (duplicate compiled resource replacement; baseline controls added twice). Default Light and Ocean/Forest/Plum Light smoke runs pass harness checks, but image analysis shows all four 1280×800 screenshots have identical dominant color counts: palette backgrounds did not render. Dark Ocean smoke fails existing popup border/topmost assertion. See §12; feature is not accepted. |

## 11. Implementation and validation record

- **Persistence/UI:** four enum values and independent key; guarded selector and allowlisted labels; fallback, mapping, no-init-write and real SQLite round-trip tests cover all four choices. No schema change.
- **Rendering/lifecycle:** application-scoped per-variant stable brushes installed before either window is created; template-facing aliases include control/menu states, selected rows, text input focus/selection, dialogs and navigation. AppHost supplies saved choice, both initial and recovered popup paths register before prewarm, and no focus/show coordination was changed.
- **Baseline refinement after review:** explicit themed baseline consumers and current OS accent shade queries replaced ambiguous static brush snapshots. Acrylic and gradient resources preserve their original geometry/properties in Default while exposing stable palette-updatable instances. These are implementation refinements within the approved scope, not new user-facing requirements.
- **System/overlay updates:** UISettings and native setting/color/theme notifications refresh on UI thread; high contrast bypasses all custom colors. Dialog registration is removed in finally; flyout tracking uses weak references, current mode, solid background and one content-free receipt per opening. Shutdown unsubscribes OS events.
- **Architecture/spec audit:** App → Presentation → Core direction preserved; no reflection/dynamic loading/JSON added; no snippet/window-title logs; all modified service methods have callers and fake implementations; all six ACs map to implemented code and automated tests or the explicit manual gate. Baseline consumers are collapsed and non-focusable.
- **Executed on Windows:** Core 432 passed; Presentation 534 passed; `dotnet build` 0 warnings/errors; `dotnet publish src/SmartSnippets.App -c Release -r win-x64 -o publish` succeeded without warnings. Verified native exe and `e_sqlite3.dll` exist, App DLL does not. `git diff --check` passed.
- **Evidence boundary:** tone tests verify opaque text/selection contrast and state mapping, not actual alpha-composited pixels or whole-app accessibility compliance. Source tests guard wiring only. Authorized isolated desktop smoke has now run and found real defects (see §12); the full §7 matrix, live updates and high-contrast transitions remain unverified. Full acceptance and merge readiness are blocked.
## 12. Blocker discovered during desktop verification

- The approved assumption that adding/replacing brushes in `Application.Resources.ThemeDictionaries` after app resources have initialized reaches WinUI controls is not supported by the observed result. Default and all three named Light palettes produced pixel-identical dominant color counts in the 1280×800 Snippets capture; expected palette-specific background changes were absent. A follow-up run verifies `palette_loaded palette=Ocean` in the isolated app's own log, eliminating missing fixture/load as the cause. WinUI resource caching/lookup is a plausible cause but is not yet proven.
- Dark Ocean and Dark Default both fail the popup style assertion; observed `style=0x14C80000`, `ex=0x00000108` (topmost set). The assertion rejects WS_CAPTION; PopupWindow deliberately calls `SetBorderAndTitleBar(true, false)` for the DWM-owned border. Determine whether caption style bits are compatible with the intended border-without-visible-titlebar contract and replace the stale oracle only with an independent reliable check; do not mask a real caption regression. Dark Forest/Plum, interactive live selection, System and high contrast have not been tested.
- Two startup failures were repaired and the isolated Light smoke now runs. The harness seed takes a validated `-Palette` argument and preserves a synthetic owned startup log on launch failure. Owned processes exited and the original profile backup was restored after each run.
- **Approved revision (2026-10-29):** stable app-scope brushes are also installed in per-window root theme dictionaries before first layout. Popup root/content/editor backgrounds explicitly bind to the stable brush because a Light popup retained its preexisting background. WinUI HighContrast native resources remain responsible for state/foreground pairs; root HighContrast overrides only the base surface. Pixel-color oracles check expected custom backgrounds in both main and popup. Popup style verification now checks actual presenter border/titlebar/topmost policy plus independent WS_EX_TOPMOST rather than rejecting WS_CAPTION bits that coexist with the intentionally retained native border.
- **Executed isolated matrix:** all eight combinations Default/Ocean/Forest/Plum × Light/Dark passed the cold-start smoke; custom palette pixels appeared in main and popup, fixture loaded-palette receipts matched, and synthetic profile/owned-process cleanup succeeded. These runs preceded the last HighContrast-only dictionary fix; ordinary Light/Dark code was unchanged by it. That fix came from independent review of hover/foreground pairing and still needs a real high-contrast transition test.
- **Outstanding:** interactive live palette changes, System theme transitions, high-contrast mode and restoration, dialogs/flyouts, actual rendered contrast, and current final-build UI regression remain unverified. A separate unowned SmartSnippets.App process was detected after the matrix; it was not touched, so no further isolated desktop run was attempted. The user must exit it before the safe harness can run again. Automated tests after the final change: Core 432, Presentation 534; solution build and Native AOT publish passed (0 warnings). A first build attempt hit a transient XAML compiler file lock; sequential `-m:1` retry passed.

- **Research:** consulted Microsoft [theme resources](https://learn.microsoft.com/en-us/windows/apps/develop/platform/xaml/xaml-theme-resources), [ThemeResource](https://learn.microsoft.com/en-us/windows/apps/develop/platform/xaml/themeresource-markup-extension) guidance, WinUI issue [9464](https://github.com/microsoft/microsoft-ui-xaml/issues/9464), and installed WinUI 1.8 `generic.xaml` to audit actual aliases, focus gradient and acrylic properties. Research is not runtime verification.
