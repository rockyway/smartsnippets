# Independent plan review 011

Verdict: changes required. Reviewed `docs/plan/011-color-palettes.md` against the current codebase (`src/`, `tests/`, `scripts/verify-ui-qa.ps1`). No existing files were modified and no builds or tests were run.

Classification:
- **A**: missed site in the same change class
- **B**: weak test or verification oracle
- **C**: inaccurate citation or unsupported claim
- **D**: design or WinUI runtime defect

---

## 1. Blocking and substantive findings

### R1 — A/D / High: `FlyoutSurface` hardcodes menu backgrounds and overrides dictionary resources

Plan locations: §3 line 35, §4.1 line 45, §6 T3 lines 124–125.

Section 3 cites `App.xaml:68-74` (`SolidMenuFlyoutPresenterStyle`) for opaque menus, and §4.1 says non-default palettes will install per-window menu resources while preserving opaque menus. However, the plan never mentions `src/SmartSnippets.App/Services/FlyoutSurface.cs`.

In the actual codebase, every `MenuFlyout` (popup sort menu at `PopupWindow.xaml.cs:53`, popup row context menu at `PopupWindow.xaml.cs:360`, snippets sort menu at `SnippetsPage.xaml.cs:30`, and snippets row context menu at `SnippetsPage.xaml.cs:248`) registers `FlyoutSurface.KeepSolid(menu)`. When the menu opens, `FlyoutSurface.MakeSolid` (`FlyoutSurface.cs:52-58`) runs:

```csharp
private static readonly SolidColorBrush Dark = new(Color.FromArgb(0xFF, 0x2B, 0x2B, 0x2B));
private static readonly SolidColorBrush Light = new(Color.FromArgb(0xFF, 0xF9, 0xF9, 0xF9));

private static void MakeSolid(MenuFlyoutPresenter presenter)
{
    presenter.SystemBackdrop = null;
    if (presenter.ActualTheme == ElementTheme.Dark) presenter.Background = Dark;
    else if (presenter.ActualTheme == ElementTheme.Light) presenter.Background = Light;
}
```

As documented in `FlyoutSurface.cs:10-14`, style and theme resources did not reliably replace the presenter background in practice, so `MakeSolid` overwrites `presenter.Background` directly on every open. Any palette `MenuSolidBrush` or `MenuFlyoutPresenterBackground` placed in a resource dictionary will be overwritten by `#2B2B2B` or `#F9F9F9` as soon as the menu opens.

Required fix:
1. Cite `src/SmartSnippets.App/Services/FlyoutSurface.cs:15-83` in §3 and add it to the T3 file list.
2. Specify how `FlyoutSurface.MakeSolid` resolves the active palette's solid menu brush (while keeping High Contrast untouched and keeping the `menu_surface` log receipt in `FlyoutSurface.LogSurface` intact).

---

### R2 — D / High: WinUI resource lookup, `StaticResource` aliasing, and live update mechanics are left undecided

Plan locations: §3 line 35, §4.1 line 45, §5 line 105, §6 T3 line 125.

Section 4.1 proposes installing per-window theme-aware palette resources for non-default palettes, then defers the core WinUI mechanism: *"validate the chosen WinUI resource update mechanism on Windows before relying on it. If a template caches resources, explicitly update its bindings/styles or use stable mutable brush instances scoped per root..."*

Three concrete WinUI 3 facts in this codebase need a definite design in §4.1 before T3 implementation:

1. **`StaticResource` aliases in `App.xaml` do not re-resolve against per-window dictionaries.**
   In `src/SmartSnippets.App/App.xaml:16-23` and `33-40`, `ListViewItemBackgroundSelected`, `ListViewItemBackgroundSelectedPointerOver`, `ListViewItemBackgroundSelectedPressed`, `ListViewItemForegroundSelected`, and `MenuFlyoutPresenterBackground` are declared as `<StaticResource x:Key="..." ResourceKey="AccentFillColorDefaultBrush" />`. In WinUI XAML, `<StaticResource>` resolves inside `Application.Resources` at parse/load time. Overriding `AccentFillColorDefaultBrush` on a window's `RootGrid.Resources` will not change `ListViewItemBackgroundSelected` unless `ListViewItemBackgroundSelected*` themselves are overridden on the window root (or replaced with direct brushes).
2. **In-place dictionary mutation does not trigger `{ThemeResource}` re-evaluation when `ActualTheme` has not changed.**
   Changing `SelectedPalette` from `Ocean` to `Forest` while staying in `Light` mode does not fire `ActualThemeChanged`. Replacing entries in `RootGrid.Resources` will not update already-realized controls (`ListViewItem` presenters, `NavigationView`, `Button` templates, open `ContentDialog`s) unless the plan either:
   - mutates `.Color` on stable per-window `SolidColorBrush` instances that stay bound, or
   - forces a `RequestedTheme` walk on the root (and open popups/dialogs) after swapping dictionary entries.
3. **`DialogService` creates `ContentDialog` instances tied to `XamlRoot`, and `MenuFlyout` opens in a popup root.**
   `DialogService.ShowAsync` (`src/SmartSnippets.App/Services/DialogService.cs:99-103`) sets `dialog.XamlRoot = root` and `dialog.RequestedTheme = content.RequestedTheme`. If palette resources are attached to `RootGrid.Resources` (`MainWindow.xaml:10`, `PopupWindow.xaml:10`), verify whether `ContentDialog` (hosted in the popup layer above `RootGrid`) and `NavigationView` internal panes inherit those overrides, or if `Application.Current.Resources` (since palette is app-wide across both windows) is simpler and reaches popups/dialogs/flyouts automatically. Note that both `MainWindow` and `PopupWindow` always share the same `ThemeMode` and `ColorPalette`; neither window ever has an independent palette.

Required fix:
Commit to a specific resource architecture in §4.1 (e.g., whether palette brushes live in `Application.Current.Resources` or `RootGrid.Resources`, how live switching triggers control visual-state updates without changing `ThemeMode`, and how `StaticResource` aliases in `App.xaml` are handled).

---

### R3 — D / High: High Contrast precedence and live transition detection in `ThemeService`

Plan locations: §2 AC5 (line 26), §3 line 36, §4.1 line 45, §4.4 line 77, §6 T3 line 125.

AC5 requires: *"Windows high contrast takes precedence and the selected palette returns after it is disabled."*
Section 4.4 states: `ActualTheme/high-contrast change -> existing theme event/resource resolution -> effective palette variant or system colors`.

In the current codebase:
- `ThemeService` (`src/SmartSnippets.App/Services/ThemeService.cs:29-38`) only subscribes to `root.ActualThemeChanged` and sets `root.RequestedTheme` to `ElementTheme.Light`, `ElementTheme.Dark`, or `ElementTheme.Default`.
- In WinUI 3, `FrameworkElement.ActualTheme` only has three values (`Default`, `Light`, `Dark`); there is no `ElementTheme.HighContrast`.
- Furthermore, when `ThemeMode` is explicitly `Light` or `Dark` (`root.RequestedTheme = ElementTheme.Light` or `Dark`), enabling Windows High Contrast does not necessarily fire `root.ActualThemeChanged`, and if custom brushes are installed in `Light` or `Dark` dictionaries (or applied imperatively by `ThemeService` based on `ActualTheme`), they will override High Contrast unless `ThemeService` checks `WindowSurface.IsHighContrast()` (`src/SmartSnippets.App/Services/WindowSurface.cs:30-34`).
- `PopupWindow` (`src/SmartSnippets.App/Popup/PopupWindow.xaml.cs:43,70,148-149`) listens to `UISettings.ColorValuesChanged` to refresh its DWM frame on system/high-contrast changes, but `ThemeService` has no `UISettings.ColorValuesChanged` or `WM_SETTINGCHANGE`/`WM_SYSCOLORCHANGE` listener.

Required fix:
Specify in §4.1 and §4.4 how `ThemeService` detects High Contrast (`WindowSurface.IsHighContrast()`) and what event hook triggers `ThemeService` to strip/bypass palette overrides when Windows High Contrast is enabled and re-apply the selected palette when High Contrast is disabled (especially when `ThemeMode` is `Light` or `Dark`, not `System`).

---

## 2. Other findings and completeness gaps

### F1 — A / Medium: Explicit list of overridden resource keys is missing from §4.1 / T3

Section 4.1 says: *"install per-window theme-aware palette resources for the explicitly audited background, layer, card, menu, selection, accent/button and folder-tint tokens."* However, the plan does not list the token keys audited from the XAML files. Listing the exact keys in §4.1 prevents missed surfaces during T3:

- **Root & overlay backgrounds:** `SolidBackgroundFillColorBaseBrush` (`MainWindow.xaml:10`, `PopupWindow.xaml:10,11,36`), `SolidBackgroundFillColorQuarternaryBrush` (`App.xaml:16,33`).
- **Pane, card & chip surfaces:** `LayerFillColorDefaultBrush` (`SnippetsPage.xaml:65`), `CardBackgroundFillColorSecondaryBrush` (`SnippetEditor.xaml:192`), `SubtleFillColorSecondaryBrush` (`SnippetEditor.xaml:172`, `App.xaml:18,35`), `SubtleFillColorTertiaryBrush` (`App.xaml:19,36`), `ControlFillColorDefaultBrush` (`SettingsPage.xaml:47` and standard controls).
- **Menus:** `MenuSolidBrush` (`App.xaml:14,32,49,72`) and `FlyoutSurface` (`FlyoutSurface.cs:17-18`).
- **List selection:** `ListViewItemBackgroundSelected`, `ListViewItemBackgroundSelectedPointerOver`, `ListViewItemBackgroundSelectedPressed`, `ListViewItemForegroundSelected` (`App.xaml:20-23,37-40`).
- **Accents & `AccentButtonStyle`:** `SystemAccentColor`, `AccentFillColorDefaultBrush`, `AccentFillColorSecondaryBrush`, `AccentFillColorTertiaryBrush`, `TextOnAccentFillColorPrimaryBrush`, `TextOnAccentFillColorSecondaryBrush`, `AccentTintBrush` (`App.xaml:26,43`), and `AccentButtonBackground` / `AccentButtonBackgroundPointerOver` / `AccentButtonBackgroundPressed` / `AccentButtonForeground` (used by `Style="{StaticResource AccentButtonStyle}"` at `SnippetsPage.xaml:121,198` and `SnippetEditor.xaml:132,293`).
- **Folder indicators:** `FolderTintBrush` and `FolderTintBackgroundBrush` (`App.xaml:24-25,41-42`, `SnippetsPage.xaml:26`).

### F2 — B / Low: T1 should call out `AppSettingsTests.Keys_AreExact`

In `tests/SmartSnippets.Core.Tests/Settings/AppSettingsTests.cs:74-82`, `Keys_AreExact` asserts the exact ordered list of keys written by `AppSettings.Save(store)`:

```csharp
Assert.Equal(
    ["hotkey.key", "hotkey.modifiers", "popup.layout", "popup.sort", "text_size.custom", "text_size.preset", "theme"],
    store.Values.Keys.Order(StringComparer.Ordinal));
```

Adding `SettingsKeys.ColorPalette = "color_palette"` to `SaveShell` will cause `Keys_AreExact` to fail until `"color_palette"` is added to that expected array. T1 should note updating `Keys_AreExact` alongside the new round-trip and fallback tests.

### F3 — A / Low: UI text constants and UIA automation properties in T2

- `src/SmartSnippets.Presentation/Text/UiText.cs` holds user-facing strings. T2 does not mention `UiText.cs` or specify the exact section header and radio labels (`Default`, `Ocean`, `Forest`, `Plum`) or control `x:Name` / `AutomationProperties.Name` in `SettingsPage.xaml`.
- Specifying `x:Name="ColorPaletteRadios"` and `automation:AutomationProperties.Name="Color palette"` in T2 keeps `SettingsPage.xaml` consistent with `TextSizeRadios` (`SettingsPage.xaml:89-90`) and `ThemeRadios` (`SettingsPage.xaml:123-124`) for UIA and `scripts/verify-ui-qa.ps1`.

### F4 — C / Nitpick: Line citations in §3

- §3 line 34 cites `src/SmartSnippets.App/Services/ThemeService.cs:15-72`. The class and `UpdateTitleBar` run through line **93** (`15-92`).
- §3 line 35 cites `AppHost.cs:164,233` for dialog creation. The popup `DialogService` is constructed at `AppHost.cs:229` and passed to `PopupViewModel` at `:233`.
- §3 line 37 cites `AppSettingsTests.cs:19-68` and `SettingsViewModelTests.cs:36-81`. The relevant key-set and invalid-enum fallback tests in `AppSettingsTests.cs` span lines `17-110`, and the theme index mapping tests in `SettingsViewModelTests.cs` are at lines `227-236` and `310-315`.

---

## 3. Checklist summary

| Check | Status | Notes |
|---|---|---|
| AC1–AC6 mapped to tasks T1–T4 | Pass | Every AC has at least one task and verification step |
| Scope matches G1 brief | Pass | 4 named palettes, independent mode, high-contrast precedence |
| Consumer surfaces grep-verified | **Needs fix** | `FlyoutSurface.cs` missed (R1); token keys not listed (F1) |
| WinUI live resource mechanism specified | **Needs fix** | Deferred in §4.1; `StaticResource` aliases in `App.xaml` break naive per-window override (R2) |
| High Contrast precedence & live restore | **Needs fix** | `ActualThemeChanged` alone is insufficient when `ThemeMode` is Light/Dark (R3) |
| `file:line` citations accurate | Minor fixes | See F4 |
