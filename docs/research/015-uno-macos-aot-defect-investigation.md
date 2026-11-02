# 015 — Uno macOS WebView Native AOT investigation

**Date:** 2026-10-30
**Status:** Cause traced; standalone source-generated JSON proof passes native arm64; Uno app still BLOCKED
**Authorization:** User approved investigating the macOS AOT blocker; no GUI/input or vendor-fork adoption authorized
**Baseline:** `c93119f`, branch `feature/uno-desktop-feasibility`
**Related:** [018](../plan/018-uno-desktop-technical-feasibility.md), [first Mac build 014](014-uno-apple-silicon-first-build.md)

## 1. Why unused WebView code is reachable

Package `Uno.WinUI.Runtime.Skia.MacOS/6.7.135` identifies repository commit `da217df2312e9e39676880f9cb4b8d519cc297e0`. Inspection of that published DLL and the corresponding official source establishes this chain:

```text
Program.Main → UnoPlatformHostBuilder.UseMacOS → MacOSHostBuilder.Create
  → MacSkiaHost static constructor
    → MacOSNativeWebViewProvider.Register (unconditional)
      → native resource-request callback function pointer
        → MacOSNativeWebView.WebResourceRequestedCallback
          → OnWebResourceRequested
            → JsonSerializer.Serialize(Dictionary<string,string>)
```

`MacSkiaHost` registers the provider regardless of whether a view uses WebView. `Register()` passes the static callback's address to the native host. That callback retrieves the WebView instance and calls `OnWebResourceRequested`; the reflection-based serializer in that method remains reachable to Native AOT. Removing a XAML control, disabling developer tools, registering JSON metadata in the application, or waiting for an app-level runtime condition does not rewrite this precompiled vendor call.

Exact upstream sources:

- [MacSkiaHost.cs](https://github.com/unoplatform/uno/blob/da217df2312e9e39676880f9cb4b8d519cc297e0/src/Uno.UI.Runtime.Skia.MacOS/Hosting/MacSkiaHost.cs) — static constructor at line 41; provider registration at line 60.
- [MacOSNativeWebViewProvider.cs](https://github.com/unoplatform/uno/blob/da217df2312e9e39676880f9cb4b8d519cc297e0/src/Uno.UI.Runtime.Skia.MacOS/UI/Xaml/Controls/WebView/MacOSNativeWebViewProvider.cs) — callback registration.
- [MacOSNativeWebView.WebResourceRequested.cs](https://github.com/unoplatform/uno/blob/da217df2312e9e39676880f9cb4b8d519cc297e0/src/Uno.UI.Runtime.Skia.MacOS/UI/Xaml/Controls/WebView/MacOSNativeWebView.WebResourceRequested.cs) — `OnWebResourceRequested`, reflection-based dictionary serialization at line 123.
- [MacOSNativeWebView.cs](https://github.com/unoplatform/uno/blob/da217df2312e9e39676880f9cb4b8d519cc297e0/src/Uno.UI.Runtime.Skia.MacOS/UI/Xaml/Controls/WebView/MacOSNativeWebView.cs) — another reflection-based serializer at line 220 for `System.Net.Http.HttpRequestMessage.Headers`.

The second call has a different input type and wire shape. It was **not** among the two diagnostics from the current probe build. A library repair must audit it too, without blindly converting it to a string dictionary or changing the native protocol.

## 2. Configuration and release investigation

- The published `MacOSHostBuilder` exposes no WebView-off option through the inspected `.UseMacOS()` API. The provider registration in the inspected static constructor has no feature guard. Searches of relevant official feature/WebView docs did not establish a supported build-time disable switch. This is a bounded finding, not proof that no alternative API can ever exist.
- Official [Native AOT guidance](https://platform.uno/docs/articles/features/native-aot.html) recommends source-generated JSON contracts. `EnableDevTools` controls inspection tools, not callback rooting or Native AOT safety.
- NuGet's stable Uno.Sdk list still ends at **6.7.30**. The preceding two stable SDKs were inspected without installing/changing the application SDK:

| Uno.Sdk | Framework version from SDK | Published macOS runtime dictionary call |
|---|---|---|
| 6.7.30 (current) | 6.7.135 | Reflection `Serialize(headers)`; actual app build fails IL2026/IL3050 |
| 6.7.22 | 6.7.103 | Same unsafe call present; full probe publish NOT RUN |
| 6.6.42 | 6.6.184 | Same unsafe call present; full probe publish NOT RUN |

- Upstream `master` snapshot `0d9eee97d802cf59eb487f21cbff0a3e2d3b1b72` moves the implementation to `src/Uno.UI.Runtime.MacOS/...`; its inspected resource-request partial still contains the same reflection call. No verified fixed release/PR was located. This is not a claim that every prerelease or branch is broken.
- No SDK version was changed, package cache patched, binary rewritten, callback removed, warning suppressed or vendor fork checked into this repository.

## 3. Headless red/green correction proof

Added standalone `prototypes/uno-desktop/macos-json-risk/JsonRepairProbe.csproj`. It references **only .NET**, not Uno/Core/Presentation, and is explicitly excluded from the parent Uno project's Compile items. It cannot validate the host or repair the vendor DLL.

On the authorized Apple Silicon Mac, with .NET SDK 10.0.403/compiler 10.0.14 and strict inherited warning/AOT settings:

1. **Red:** a tiny console calling the same reflection overload `JsonSerializer.Serialize(headers)` failed publish with **IL2026 and IL3050**. It was not run. Private log: `ss-uno018-mac-json.GrSpNe/red.log` under OS temp.
2. **Green:** replace that call with a generated contract:

   ```csharp
   [JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Serialization)]
   [JsonSerializable(typeof(Dictionary<string, string>))]
   internal partial class HeaderJsonContext : JsonSerializerContext;

   // The vendor assembly would need an equivalent call compiled inside it:
   JsonSerializer.Serialize(headers, HeaderJsonContext.Default.DictionaryStringString);
   ```

3. Fresh clean/publish succeeded with zero observed warnings, `PublishAot=true`, `JsonSerializerIsReflectionEnabledByDefault=false`, `NoWarn=1701` and no IL suppression. `file` identifies the result as a **Mach-O 64-bit arm64 executable**.
4. Executed **only this headless CLI**, with synthetic in-memory fixtures: empty dictionary, simple header, Unicode keys/values, escaped control characters and empty header values. JSON shape/key/value comparisons all passed. It checks reflection serialization is disabled and prints no payload:

   ```text
   json_repair_checks=4 reflection_enabled=false
   headless_run_exit=0
   ```

Artifact: `JsonRepairProbe`, SHA-256 `4c88b86f0ed4418a83ba13ca752a73faf65e7674711ee589abd962170f8f263a`. Private logs/proof build remain on the Mac; no binaries or vendor/decompiler sources are committed. After checkout synchronization, the original proof worktree may be retained under the same private evidence directory rather than its initial checkout location.

Reproduce the **green standalone proof only** from its directory, not the parent Uno app:

```sh
export PATH="/usr/local/share/dotnet:/opt/homebrew/bin:/usr/bin:/bin:/usr/sbin:/sbin"
export DOTNET_ROOT=/usr/local/share/dotnet
dotnet clean JsonRepairProbe.csproj -c Release -r osx-arm64
dotnet publish JsonRepairProbe.csproj -c Release -r osx-arm64 -p:NoWarn=1701 -o bin/green-proof
./bin/green-proof/JsonRepairProbe
```

No window, app host, snippet profile, database, network, clipboard, hotkey or OS privacy setting is used by this program. This is a real native CLI run, **not** the Uno application startup/runtime PASS.

## 4. Validation and audits

- **31 Python checks pass**, including a positive App source assertion and exclusion of `macos-json-risk` from real evaluated Uno Compile items.
- Fresh Windows manual-package native publish still returns **BUILD_CHECKS_ONLY**, exit 0, private run `fa780bf73a33475e8c515bbf138875a5`; the new helper has not leaked into the Windows application.
- Existing Mac managed suites from checkpoint 014: **432 Core + 599 Presentation pass**, not rerun for this standalone serialization change and not substituted for Uno AOT/runtime evidence.
- Architecture audit: no production code, central package/SDK settings, solution or CI change; new helper is a separate package-free CLI and excluded from app compilation. Vendor repair/adoption is a separate decision.
- Spec alignment: plan 018 remains BLOCKED; only standalone serializer runtime correctness is proven. Main Uno macOS publish, application data/restart, UI/window/focus/clipboard and mobile gates remain incomplete.
- Independent bounded review (`gemini-4-argon:medium`): **no P0/P1 blockers; safe investigation checkpoint, OK with notes**, not plan completion. Optional follow-ups before expanding the probe: exclude nested risk directories from all default items (not only Compile), assert desktop/helper effective properties, and keep status wording current. No vendor correction or GUI gate was approved by the review.

## 5. Recommendation / decision required

The durable correction belongs **inside the Uno macOS runtime**: use generated JSON contracts for the callback and audit the separate navigation/header overload while preserving wire format. Application-side serializer settings or DynamicDependency annotations cannot remove the precompiled method's analyzer contract.

Preferred: prepare an upstream reproducer/fix for review and consume an upstream corrected stable package when available. If waiting is unacceptable, explicitly approve a **pinned source-built vendor correction for the probe**, with a recorded patch, provenance, regression tests and maintenance cost. A source-built package is not automatically an upstream stable release; it must not silently become the production dependency.

Do not delete the entire macOS host/WebView callbacks, broadly root assemblies, suppress IL2026/IL3050, enable reflection fallback, or switch to JIT to obtain a green gate. Do not start 019 or declare plan 018 complete. GUI/input and permission tests still require separate authorization.
