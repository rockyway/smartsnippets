# Plan 018 — isolated Uno desktop feasibility probe

**State: BLOCKED — default Windows IL2059 and macOS native WebView IL2026/IL3050. Not a production port.**

The default multi-target Uno project restores and its Windows XAML compiles, but a **clean default-mode** native publish fails in precompiled `Uno.UI.Toolkit` XAML initialization. An **opt-in manual Windows package experiment** now publishes a native artifact; it is not enabled by default or runtime-verified. See [investigation 013](../../docs/research/013-uno-windows-aot-defect-investigation.md). No GUI artifact has been launched, no store fixture has been run in the Uno host, and no target/clipboard/shortcut adapter is implemented. Do not read the scaffold or build-validator tests as desktop parity evidence. See [results](../../docs/research/012-uno-desktop-feasibility-results.md).

## Pinned, observed tooling

- Uno.Templates **6.7.30**, Uno.Sdk **6.7.30**, resolved Uno framework **6.7.135**.
- .NET SDK **10.0.403**, MSBuild **18.9.15**, native compiler **10.0.14** on Windows x64.
- `WinAppSdkVersion=1.8.260921001` (matches the current app), unpackaged `WindowsPackageType=None`.
- Windows TFM `net10.0-windows10.0.26100`; evaluated minimum OS `10.0.18362.0`. This is **probe-only** and is not approval to raise production's 17763 minimum.
- macOS TFM `net10.0-desktop`, RID **osx-arm64** only. Actual Mac checkout is `~/sources/demo/smartsnippets`: macOS 26.5.2 / Xcode 26.6 / .NET SDK 10.0.403. Restore and C#/XAML compile pass, but clean native publish fails IL2026/IL3050 in vendor WebView JSON. OS floor, published bundle and launch remain **unverified**; see [014](../../docs/research/014-uno-apple-silicon-first-build.md).
- No production `global.json`, central packages, source files, solution or CI changes.

Template source was generated in OS temp using a **private template hive** (not the global template install):

```text
dotnet new install Uno.Templates@6.7.30 --debug:custom-hive <private-temp-hive>
dotnet new unoapp -n SmartSnippets.UnoProbe -o <private-temp-output> -preset blank -platforms windows desktop -tfm net10.0 -presentation none -markup xaml -tests none -log none -vscode false -rider false -skip --debug:custom-hive <private-temp-hive>
```

The checked-in project is deliberately smaller than the generated solution. A scoped `Directory.Packages.props` explicitly stops NuGet inheriting production CPM; setting the local build property alone was insufficient. `UnoDisableHotDesign=true` and `UnoDisableMCPSupport=true` exclude design/MCP tools from this offline probe. Local package-management settings do not affect the production projects' central package versions. Only the Windows host selects the Windows TFM; macOS target/source selection has now compiled on Apple Silicon, with native AOT/runtime acceptance still blocked. `Platforms/Desktop/Program.cs` uses the actual generated `UnoPlatformHostBuilder`/`UseMacOS` APIs and compiles on the Mac; the host has not been launched.

## Build-only collector (no GUI, Python 3 + PowerShell 7.2+ required)

From the repository root on Windows:

```powershell
pwsh -NoProfile -File prototypes/uno-desktop/collect-build.ps1 -RuntimeIdentifier win-x64
```

The collector creates owner-only evidence storage under the current user's OS temp directory and a unique output under ignored `bin/probe/`. It cleans before publishing and records effective properties, raw compiler output and the exit code privately. **An ILC failure can leave an object file that a subsequent incremental publish links without reprinting diagnostics.** The validator refuses this as fresh evidence: a real native compile receipt is mandatory. No artifact, profile or user's clipboard is executed/accessed by this collector. Do not run other builds concurrently against the shared Core/Presentation output.

The effective `NoWarn=1701` command property deliberately **removes** vendor-injected `IL2121` suppression, rather than adding any IL suppression. `SuppressTrimAnalysisWarnings=false`, `TrimmerSingleWarn=false` and warnings-as-errors stay enabled. The project has a matching Windows publish profile and default language to avoid missing-profile/resource-language warnings without suppressions.

A future Mac operator may try the matching `-RuntimeIdentifier osx-arm64` collector after prerequisites are established; its macOS bundle layout is unverified and a mismatch must be corrected from actual host evidence, not treated as PASS. `PackageFormat=app` is only an unverified bundle request until tested on Uno's desktop backend; no signing property or resulting ad-hoc signature is assumed. A Mac operator must inspect actual packaging/signature output before accepting any signing evidence. It never launches that bundle.

## Opt-in AOT investigation (Windows build only)

```powershell
pwsh -NoProfile -File prototypes/uno-desktop/collect-build.ps1 -RuntimeIdentifier win-x64 -ManualWindowsPackages
```

This selects documented manual package management only on the Windows TFM, omits the precompiled Uno helper causing IL2059, and explicitly retains native WinAppSDK/BuildTools using the same version pins. Uno.Sdk injects a `Uno.UI` global using even in manual package mode, so that mode disables implicit usings and the App imports its namespaces explicitly. Desktop still uses implicit Uno packages. No IL suppression, package patch, Skia-on-Windows or JIT fallback is involved.

**Scope limit:** the experiment also omits Uno Windows helper/asset/logging packages. It proves this minimal XAML can compile/publish, not that the full UI can omit these dependencies. The library-specific `DisableImplicitUnoWinAppSdkPackages` switch is not used. The generic `DisableImplicitUnoPackages` path is documented, but this app-head dependency set still requires runtime validation. The owner approved this opt-in mode for remaining probe work; it is not a production dependency decision. Both commands clean before compilation; run them sequentially.

## Validator and property-only integration tests

```text
python -m unittest discover -s prototypes/uno-desktop/tests -v
```

The standard-library-only validator checks effective properties, all visible warnings/errors, a native compiler receipt/successful publish exit, PE/Mach-O architecture/managed-entry characteristics, known JIT payloads and a native SQLite asset. It also binds the expected TFM/backend to each RID and rejects managed apphost layouts, single-file publish settings and known runtime-host bundle markers. It returns **REJECTED** or **BUILD_CHECKS_ONLY**, never runtime PASS. Header fixtures test the parser, not real Native AOT provenance; trusted local build records and target-device validation remain necessary. It does not certify macOS signatures, exact dependencies, runtime execution or clipboard/focus safety. All raw paths/exception output stay out of its JSON summary.

To inspect existing **private** build evidence manually:

```text
python prototypes/uno-desktop/verify_evidence.py --properties <private-properties.json> --log <private-publish.log> --artifact-dir <output> --executable <output-executable> --rid win-x64
```

## macOS JSON risk proof (not the Uno application)

[Investigation 015](../../docs/research/015-uno-macos-aot-defect-investigation.md) traces unconditional host callback registration to the vendor reflection serializer. A standalone .NET CLI under `macos-json-risk/` first reproduced IL2026/IL3050 and then successfully published and ran natively on Apple Silicon using a generated JSON contract (four synthetic fixtures, reflection serialization disabled). Its source is excluded from the Uno app's Compile items.

This proves the proposed dictionary serializer correction, **not** a patched Uno package or native GUI startup. The separate navigation-header serializer still needs a vendor-level audit. Only the headless CLI was run; no screen/input permission is implied. Reproduction commands and exact upstream source links are in 015.

## Mobile prerequisite diagnostic (not keyboard implementation)

```text
dotnet restore prototypes/uno-desktop/mobile-risk/ToolchainCheck.csproj
dotnet restore prototypes/uno-desktop/mobile-risk/ToolchainCheck.csproj -p:TargetFramework=net10.0-ios
```

Both currently fail **NETSDK1147** because Android/iOS workloads are absent. No workload is silently installed. This checks only the first prerequisite: it proves neither mobile Native AOT support nor impossibility. Actual app-plus-keyboard/IME builds, signing, device launch and 10k projection memory remain NOT RUN.

## Safety / next action

There is **no default GUI mode**: desktop startup returns before host creation without `--authorized-probe-ui`; the Windows App also refuses activation without that flag. **The flag is not authorization.** Only use it after a separate approved screen slot and safe isolation procedure. Windowing, external hover, runtime receipts and native insertion adapters are not implemented/verified at this blocked checkpoint. Never call this a non-activating self-test.

All future private databases, WAL/backups, profiles, raw logs and recovery journals must remain outside the repository with owner-only permissions. No plaintext operator clipboard recovery file is permitted. Do not register the production instance key/hotkey or mutate HKCU startup/TCC. No live screen/input tests are authorized by implementation approval.

**Stop here:** the Windows mode is approved for probe work, but first Apple Silicon native publish hit a new vendor JSON AOT blocker. Decide how to correct that macOS vendor path; do not suppress errors. No default Windows switch, 019 start, GUI launch or lower Native AOT requirement is authorized.
