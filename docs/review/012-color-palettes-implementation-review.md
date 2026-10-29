# 012 — Color palettes implementation review

**Date:** 2026-10-29
**Scope:** Implementation of [plan 011](../plan/011-color-palettes.md) on `feature/color-theme-selection`.
**Method:** Three independent read-only Codex CLI reviews plus lead-agent architecture/spec audits. Reviewers inspected source, new files and installed WinUI 1.8 generic.xaml; they did not edit, build, test or launch applications.

## Findings and resolution

| Finding | Resolution |
|---|---|
| P2: replacing shared fill brushes alone misses static template aliases for buttons, inputs, combo boxes and menu hover | PaletteResources now explicitly installs stable brushes for those aliases/states, navigation content and other relevant controls. |
| P2: open ContentDialogs retained their opening RequestedTheme | Both dialog hosts pass ThemeService; active dialogs register while ShowAsync runs and unregister in finally; mode changes propagate. Flyout presenters receive current RequestedTheme too. |
| Risk: cross-dictionary StaticResource baseline lookups may select the wrong variant | Removed those new baseline aliases. PaletteBaseline uses explicit Light/Dark consumer scopes and unmodified primitive Color resources, with source-change notifications. |
| Risk: retained unconsumed accent brushes might not refresh | Default reads current accent shades directly from UISettings on each refresh; other baselines have explicit themed consumers attached to the root. |
| P2: text-input focus borders and selection still used system accents under custom palettes | Added stable focus gradient stops preserving WinUI geometry and controlled text selection brushes. Native white selected text gets a saturated palette-colored background in both modes. |

**Final external verdict:** “No remaining definite defects found in the requested fixes.” This is a static code-review result, not visual acceptance.

## Lead-agent audits

- Layering remains App → Presentation → Core; no WinUI dependency added to lower layers.
- All IThemeService implementations updated; selector → view-model → service → key-value store/reload covered by tests.
- Main/initial popup/replacement popup registration paths retain current mode/palette before prewarm; no changes to insertion, cache/store threading, focus coordinator or reveal policy.
- Window/root events, system-color notifications and native message hooks have shutdown removal. Active dialogs unregister in finally; presenters are weakly tracked; logs retain allowlisted enum/type/color-only receipts.
- Stable per-variant application resources cover existing controls and popup-layer consumers; retained baseline sources are not mutated. Default acrylic properties and focus gradient geometry are preserved; custom acrylic fallback colors are opaque.
- AC1–AC6 have code/test/manual-gate coverage; visual ACs are not declared passed solely from source or pure tone tests.

## Executed verification (lead agent, Windows)

- Core tests: **432 passed**.
- Presentation tests: **534 passed** (including real SQLite integration for every palette, tone/contrast and source wiring guards).
- Solution build: **0 warnings, 0 errors**.
- Native AOT publish: succeeded with no warnings. Output contains `SmartSnippets.App.exe` and `e_sqlite3.dll`, and no `SmartSnippets.App.dll`.
- `git diff --check`: passed.

## Remaining acceptance gate

Subsequent authorized isolated desktop testing exposed startup failures and ineffective app-only resource overrides; those defects were fixed under the user-approved plan revision. Eight Default/Ocean/Forest/Plum × Light/Dark cold-start smoke runs then passed, including custom main/popup pixel-color checks and the explicit presenter policy. A later independent review found a potential high-contrast state/foreground pairing defect; the root HighContrast dictionary now overrides only its base surface and relies on native states. The final high-contrast fix and runtime lookup still need real high-contrast testing. Live changes, System theme transitions, dialogs/flyouts and rendered contrast remain unverified and block full visual acceptance. A separate unowned app process was observed after testing and left untouched.
