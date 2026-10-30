# 013 — Uno Windows Native AOT defect investigation

**Date:** 2026-10-30
**Status:** Root cause isolated; opt-in build-only workaround verified; runtime feasibility NOT RUN
**Authorization:** User requested “investigate the Uno AOT defect”; no GUI/input permission granted
**Baseline:** `734ec20` on `feature/uno-desktop-feasibility`
**Related:** [018](../plan/018-uno-desktop-technical-feasibility.md), [initial results 012](012-uno-desktop-feasibility-results.md)

## 1. Finding and limits

The default Windows package graph still fails clean Native AOT publish with IL2059. The immediate cause is a **precompiled generated metadata method inside `Uno.UI.Toolkit.dll` shipped in `Uno.WinUI/6.7.135`**, not the probe's newly generated initializer. Updating the app's WinAppSDK version cannot regenerate metadata inside that dependency.

A documented manual-package **experiment**, conditional on `ProbeManualWindowsPackages=true` and the Windows TFM, omits the offending Uno runtime helper dependency while retaining Uno.Sdk, native WinAppSDK rendering, the same shared XAML, Core/Presentation references, strict Native AOT and unsuppressed warning checks. This yields a fresh Windows x64 native artifact with zero observed build warnings. It is a **workaround for this small probe, not a repair of Uno.UI.Toolkit** or proof that the full production UI can omit Uno packages.

Default mode is unchanged. No binary was executed. No store/SQLite data operation, GUI, windowing, insertion, clipboard, hotkey, startup or permissions experiment occurred. No Mac/device was available. Plan 018 remains blocked/incomplete and 019 must not start.

## 2. Root-cause trace

1. `Uno.Sdk/6.7.30` normally injects `Uno.WinUI/6.7.135` on Windows. Package source `buildTransitive/uno.winui.winappsdk.targets`, target `_UnoRemoveReferences`, adds Windows helper DLLs from `lib/net9.0-windows10.0.19041.0/` to both reference and copy-local lists. `_UnoIncludeUIToolkit` also adds helper resources.
2. Default probe-generated `obj/x64/Release/net10.0-windows10.0.26100/win-x64/XamlTypeInfo.g.cs` references `Uno.UI.Toolkit.Uno_UI_Toolkit_Windows_XamlTypeInfo.XamlMetaDataProvider` and `Uno.Diagnostics.UI.DiagnosticsOverlay`.
3. Inspecting the shipped `Uno.UI.Toolkit.dll` with **ilspycmd 9.1.0.7988** (installed only to OS temp, not globally) shows:

   ```csharp
   public override void RunInitializer()
   {
       RuntimeHelpers.RunClassConstructor(base.UnderlyingType.TypeHandle);
   }
   ```

   The static constructor type flows through instance metadata. The native compiler cannot prove it is available; the clean publish reports IL2059 against this method.
4. In contrast, the probe's current WinAppSDK-generated metadata uses per-type `typeof(...).TypeHandle` initializer delegates, with `RunInitializer()` calling `StaticInitializer()`. Changing that generated app file would not fix the precompiled dependency and was not attempted.
5. Removing the unused helper dependency in the opt-in graph removes the corresponding generated helper-provider/diagnostics references. An actual clean compile then succeeds. No warnings are suppressed, types artificially preserved, package binaries patched, or backend switched to Skia/JIT.

Decompiler output and package source were inspected locally; no vendor DLL/source copy is committed. The experiment demonstrates this dependency path, not a universal statement about every Uno release or all helper APIs.

## 3. Supported configuration versus unverified adoption

Official docs fetched through the Uno documentation MCP:

- [Windows using WinAppSDK](https://platform.uno/docs/articles/how-uno-works.html#windows-using-winappsdk): native Windows target uses Microsoft's WinUI tooling.
- [Disabling implicit Uno packages](https://platform.uno/docs/articles/features/using-the-uno-sdk.html#disabling-implicit-uno-packages): documents `DisableImplicitUnoPackages=true` and manual package references.
- [WinAppSDK-only libraries](https://platform.uno/docs/articles/features/using-the-uno-sdk.html#winappsdk-only-libraries): `DisableImplicitUnoWinAppSdkPackages` is aimed at libraries, **not app heads**. It is not the switch used here; package source guards it to non-executables.

The generic manual-package mechanism is documented. **This particular minimal application dependency set is only build-tested.** It excludes implicit Uno helpers, resize/asset tooling and logging integrations on Windows; applications using those features may need a different graph or an upstream corrected package. Existing probe code has plain WinUI controls, no resizetizer assets and no calls to Uno Windows helper APIs. Retaining representative production controls/assets and full runtime behavior is still required by 018/019.

## 4. Changes, scoped to the diagnostic

- Windows conditional property group adds `DisableImplicitUnoPackages=true` and `ImplicitUsings=disable` **only** when `ProbeManualWindowsPackages=true`. The latter avoids Uno.Sdk's otherwise injected `global using Uno.UI`; `App.xaml.cs` now imports `System` and `System.Linq` explicitly.
- Manual Windows references `Microsoft.WindowsAppSDK` and `Microsoft.Windows.SDK.BuildTools` using shared pinned properties (`1.8.260921001`, `10.0.28000.2705`). Both modes use the same Microsoft version pins to reduce A/B differences.
- `collect-build.ps1 -ManualWindowsPackages` forwards the switch, rejects using it on macOS and captures it plus `DisableImplicitUnoPackages` in private property evidence. No GUI is launched by either mode.
- New scoped `Directory.Packages.props` stops NuGet walking upward to the production CPM file. Investigation first hit NU1008: the existing local `Directory.Build.props` value alone was overridden by the root `Directory.Packages.props`. With the local packages file, effective probe CPM is false; Core/Presentation CPM remains true. No production configuration changed.
- `tests/test_package_mode.py` evaluates actual MSBuild properties/items for default Windows, manual Windows, desktop with the flag, and the unchanged production libraries. These are property-only checks, not native execution.

## 5. Clean A/B measurements

Reproduce sequentially from the repo root (build/inspect only):

```powershell
# Control: expected IL2059 and nonzero exit
pwsh -NoProfile -File prototypes/uno-desktop/collect-build.ps1 -RuntimeIdentifier win-x64

# Experiment: expected BUILD_CHECKS_ONLY, never a runtime PASS
pwsh -NoProfile -File prototypes/uno-desktop/collect-build.ps1 -RuntimeIdentifier win-x64 -ManualWindowsPackages
```

Both run clean before publish and use fresh run-specific output, .NET SDK 10.0.403/compiler 10.0.14, Uno.Sdk 6.7.30, WinAppSDK 1.8.260921001, BuildTools 10.0.28000.2705, `PublishAot=true`, `PublishTrimmed=true`, `PublishSingleFile=false`, `TreatWarningsAsErrors=true`, `SuppressTrimAnalysisWarnings=false`, `TrimmerSingleWarn=false`, analyzers enabled and effective `NoWarn=1701` (no IL suppression).

| Mode | Private temp run ID | Outcome |
|---|---|---|
| Default control | `7be4e473cdf841739ec8d3ac3ebcb3fd` | Fresh native compile fails IL2059; validator REJECTED, collector exit 1 |
| Manual Windows packages | `0e74b2f00cc542bdb472bbc3a8f81fe6` | Fresh native compile and publish exit 0; no diagnostics, validator BUILD_CHECKS_ONLY |

Earlier investigation runs: `4346ebb4e1394394b1b0e0e31a4f39a2` exposed NU1008; `d0ed5a38fc084458b505d88cadad0c59` exposed the unused `Uno.UI` implicit namespace; `3669738c373c4b96818eeadf6b1e1d07` first passed the manual build; `0c454a5049af4a349b81bd5478f536e6` reconfirmed default failure. Raw evidence stays private outside the repository; none of these are runtime receipts.

Final experiment artifact:

- Entry: `SmartSnippets.UnoProbe.exe`, **4,807,168 bytes**.
- SHA-256: `5e8eb49b22409d86a79fc3b9260f85b268105a26ae8486d459a28642c4b4d3c2`.
- Validator inspected PE native x64 entry, absence of known JIT-host/payload layouts and native SQLite header; did **not** load the executable or SQLite library.
- Actual restore graph for the experiment has no `Uno.WinUI` on `net10.0-windows10.0.26100/win-x64`, while `net10.0-desktop` still resolves `Uno.WinUI/6.7.135`. Desktop graph restoration/evaluation on Windows is **not** a Mac compile or runtime test.

No application startup, native dependencies-at-runtime, data persistence, two-window, hover, clipboard or target-safety PASS is inferred from this result.

## 6. Verification, audits and decision

- **30 Python checks passed** (26 evidence checks + 4 real MSBuild property/item integration checks). No property test compiles/executes the app. The existing blocked control and clean positive experiment supply red/green native build evidence.
- Production regression suites rerun sequentially: **432 Core + 599 Presentation passed**, zero failed/skipped. Production source, SDK/package settings, solution and CI diff is empty.
- Technical architecture audit: same single Uno project and native Windows backend; opt-in Windows-only dependency change, desktop Uno graph retained, production source/SDK/CPM/CI unchanged.
- Spec-vs-implementation audit: investigation authorization only; default IL2059 preserved, no broad migration, no runtime gate waived. `DisableImplicitUnoPackages` is **not** `DisableImplicitUnoWinAppSdkPackages`; library-only guidance is not asserted as app-head support.
- Independent bounded review (`gemini-4-argon:medium`): **no P0/P1 blockers; safe investigation checkpoint, OK with notes**. Optional follow-ups before expanding the collector: restore before collecting package-import-dependent properties; label manual-package mode in summary JSON; assert positive Uno package items in default/desktop property tests. Reviewed A/B property records were accurate and the conclusions remain valid. These notes do not clear runtime/Mac gates or approve adoption.

**Recommendation:** approve using the opt-in manual Windows package mode for the next probe steps, with a clear obligation to validate every required control/asset/helper at runtime and add explicit packages as needed. Alternatively, pursue/rebuild an upstream AOT-corrected Uno helper package. Do not enable the workaround by default, claim the framework is repaired, start 019 or change release/AOT safety requirements without the owner's decision. Screen/input and Apple Silicon validation still require separate access/authorization.
