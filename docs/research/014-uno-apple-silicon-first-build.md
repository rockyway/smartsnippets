# 014 — Apple Silicon checkout and first native build

**Date:** 2026-10-30
**Status:** BLOCKED — macOS Native AOT IL2026 / IL3050 in Uno native WebView
**Plan:** [018](../plan/018-uno-desktop-technical-feasibility.md)
**Authorization:** User approved continuing with the opt-in Windows probe workaround and SSH checkout on their Apple Silicon Mac; GUI/input testing was not authorized.

## 1. Checkout and environment

- Created a real Git checkout at `~/sources/demo/smartsnippets`, branch `feature/uno-desktop-feasibility`, using a local Git bundle at baseline `3aa0aa5`. Origin is the existing GitHub repository. No GitHub push or credentials were required.
- The first clone attempt was refused because the parent `~/sources/demo` had the `uchg` Locked flag. The user removed that flag and created the empty destination directory. The agent did not alter directory flags, TCC, startup settings or existing checkouts.
- Mac is **Darwin arm64**, macOS **26.5.2**, Xcode **26.6** (build `17F113`).
- Selected existing `/usr/local/share/dotnet` explicitly: .NET SDK **10.0.403**, MSBuild **18.9.15**, runtime/compiler **10.0.14**. The user's separate `~/.dotnet` SDK 10.0.301 remains untouched.
- No SDK, PowerShell, workload, device tool or global PATH change was installed. Commands set PATH/DOTNET_ROOT only for their SSH session. Python 3.14.5 is available via Homebrew.
- Available disk space fell from about **13 GiB to 10 GiB** after package restores/builds. No unrelated data was removed; avoid broad workload installations or accumulating repeated native outputs without an explicit space plan.

## 2. Actual commands / outcome

Run from `~/sources/demo/smartsnippets/prototypes/uno-desktop` on the Mac, with the selected SDK in PATH. Raw output is held in a private directory created with `umask 077`; it is not committed.

```sh
export PATH="/usr/local/share/dotnet:/opt/homebrew/bin:/usr/bin:/bin:/usr/sbin:/sbin"
export DOTNET_ROOT=/usr/local/share/dotnet

dotnet restore SmartSnippets.UnoProbe.csproj -r osx-arm64

dotnet clean SmartSnippets.UnoProbe.csproj \
  -f net10.0-desktop -r osx-arm64 -c Release

dotnet publish SmartSnippets.UnoProbe.csproj \
  -f net10.0-desktop -r osx-arm64 -c Release \
  -o bin/probe/mac-6GWpg0 -p:PackageFormat=app \
  -p:NoWarn=1701 -p:SuppressTrimAnalysisWarnings=false -p:TrimmerSingleWarn=false
```

- Restore: **exit 0**. Actual single-project macOS target and source selection work through restore.
- C#/XAML compilation: succeeds; generated bindable/attached-property descriptors are emitted. Core/Presentation compile for .NET 10. The desktop host's `.UseMacOS()` call compiles.
- Fresh native compilation: **exit 1** after IL2026 and IL3050; no successful native app/bundle publish. SDK/Uno versions remain **6.7.30 / 6.7.135**.
- Effective properties: `TargetFramework=net10.0-desktop`, `RuntimeIdentifier=osx-arm64`, `PublishAot=true`, `PublishTrimmed=true`, `PublishSingleFile=false`, `TreatWarningsAsErrors=true`, both analyzers enabled, `SuppressTrimAnalysisWarnings=false`, `TrimmerSingleWarn=false`, `NoWarn=1701` with **no IL suppression**. WindowsPackageType and WinAppSdkVersion are empty on this target.

Observed vendor failure (payload/host paths omitted):

```text
Uno.UI.Runtime.Skia.MacOS.MacOSNativeWebView.OnWebResourceRequested(String,String)
  -> JsonSerializer.Serialize<Dictionary<String,String>>(..., JsonSerializerOptions)
IL2026: RequiresUnreferencedCode; reflection-based JSON may break trimming
IL3050: RequiresDynamicCode; JSON may require runtime code generation
MSB3077: native compiler diagnostics fail publish
```

The probe contains no WebView. The error is in the referenced Uno macOS runtime. This identifies the failing method/path, **not** a verified fix or proof of why that path is rooted. No assembly patch, linker descriptor, JSON workaround, feature exclusion, warning suppression, JIT fallback or package version change was attempted.

## 3. Host-local evidence validation

Private run directory token: `ss-uno018-mac.6GWpg0` (OS temp, owner-only). Contains effective properties, restore/clean/publish logs and separate test logs. Do not share raw logs without redacting host filesystem roots.

The existing Python validator was executed **on the Mac**, against these properties and the actual failed publish log. Result:

```json
{"result":"REJECTED","errors":["artifact:missing-or-unreadable","build:diagnostic","build:no-successful-exit"],"runtimeGates":"NOT RUN"}
```

There is no artifact hash, signature, native SQLite load, bundle-layout or notarization PASS to report. The collector's intended app executable path was not asserted as a valid macOS layout: publish failed before that could be checked.

## 4. Existing tests on actual Apple Silicon

Run sequentially from the repository root:

```sh
dotnet test tests/SmartSnippets.Core.Tests --verbosity minimal
dotnet test tests/SmartSnippets.Presentation.Tests --verbosity minimal
```

| Suite | Passed | Failed | Skipped |
|---|---:|---:|---:|
| Core | 432 | 0 | 0 |
| Presentation | 599 | 0 | 0 |

These establish the existing managed regression suites on Apple Silicon (including their SQLite fixtures). They are **not** tests inside a published Uno Native AOT app, and do not satisfy FG2 app data/restart acceptance or any GUI/focus/clipboard gate.

## 5. Status and next decision

- Windows manual-package mode is approved **for probe work only**; default Windows IL2059 remains unchanged, and the workaround does not repair the vendor helper.
- Mac SSH checkout/toolchain discovery, restore, source compilation and managed regression tests are now observed, not assumed.
- Mac native release gate remains **BLOCKED** by the two unsuppressed vendor JSON diagnostics.
- Native executable startup, T2 application data roundtrip, two-window/popup/previews, target identity/clipboard/insertion, signing and mobile extension/device work remain unverified.
- No application window, native artifact, input synthesis, hotkey registration, clipboard access, startup mutation or OS privacy change occurred. Production source/solution/SDK/package settings/CI remain untouched.

**Recommendation:** authorize a bounded investigation of the Uno macOS WebView Native AOT path (documented feature configuration or upstream correction) without weakening AOT or silently removing a dependency. Keep 019 blocked. Separately, an authorized screen slot is still required before the native window/focus/insertion acceptance steps.
