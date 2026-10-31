# 018/T7 — bounded iOS app + Swift keyboard build fixture

Build-only risk probe, independent of production and the desktop probe. **Paired artifact BLOCKED**, not mobile acceptance. No app/device/simulator launch or install is authorized by these commands. No Core/Presentation references or 019/022 implementation.

- Real Uno `Application`/XAML + UIKit entry point, Uno.Sdk **6.7.30**, `net10.0-ios`, `ios-arm64`, strict Native AOT/trim settings.
- Native Swift `UIInputViewController`, `com.apple.keyboard-service`, `RequestsOpenAccess=false`, no App Group/data exchange/insertion implementation.
- `BuildNativeKeyboard` builds the unsigned `.appex`; `AdditionalAppExtensions` requests embedding. The latest corrected-renderer fixture compiled the public Skia host API, but ILLink's managed registrar failed before ILC or app/keyboard embedding. Its linker input was still native iOS Uno.UI, not the portable Skia implementation; a complete Skia Native AOT path remains unverified.
- Codesigning/provisioning disabled; the artifact is **not deployable acceptance evidence**. No preview flags, Mono fallback or IL-warning suppressions.

## Historical first bounded attempt — loose-manifest iOS26.0

Mac Xcode **26.6 (17F113)**; `xcrun --sdk iphoneos --show-sdk-version` **26.5**. SDK **10.0.403**, runtime/AOT pack **10.0.14**. Private `ios` workload **26.0.11017/10.0.100**, workload set `10.0.400-manifests.a00bebb6`.

`python3 run_private_attempt.py` was run from a private copy, **not** the shared Mac checkout. It clones `/usr/local/share/dotnet` with APFS `cp -cR` (never hardlinks), verifies private ownership/distinct inodes/no symlinks, and uses private `HOME`, `DOTNET_ROOT`, `DOTNET_CLI_HOME`, temp and NuGet caches. The sole explicit NuGet source is official nuget.org. No shell profile/global configuration is edited.

Commands within the owner-only workspace (paths abbreviated):

```sh
private-dotnet workload install ios --skip-manifest-update \
  --configfile private-NuGet.Config --source https://api.nuget.org/v3/index.json \
  --temp-dir private-tmp --disable-parallel
bash build_keyboard.sh
private-dotnet msbuild IosPair.csproj -p:Configuration=Release \
  -getProperty:PublishAot,PublishTrimmed,IsAotCompatible,NoWarn,SuppressTrimAnalysisWarnings,TrimmerSingleWarn,EnableCodeSigning,RuntimeIdentifier
private-dotnet publish IosPair.csproj -c Release -r ios-arm64 \
  --configfile private-NuGet.Config -p:PublishAot=true \
  -p:EnableCodeSigning=false -p:CodesignRequireProvisioningProfile=false \
  -p:UseSharedCompilation=false -nodeReuse:false
```

Final source-matched attempt: **36.5 seconds**, publish exit **1**. One diagnostic, unnumbered .NET iOS SDK/Xcode compatibility error at `Xamarin.Shared.Sdk.targets(2382,3)`:

> This version of .NET for iOS (26.0.11017) requires Xcode 26.0. The current version of Xcode is 26.6. Either install Xcode 26.0, or use a different version of .NET for iOS.

Restore succeeded. No management app artifact, paired package or Native AOT compile was produced/verified. No Xcode-version bypass, workload-manifest update or vendor repair was attempted. Effective properties: `PublishAot/PublishTrimmed/IsAotCompatible=true`, `SuppressTrimAnalysisWarnings/TrimmerSingleWarn/EnableCodeSigning=false`, `NoWarn=1701;1702` (default assembly-reference warnings only). There is **no completed IL-analysis result**.

Swift native compile succeeded using existing `xcrun` only. Inspection: Mach-O **ARM64 EXECUTE**, `_NSExtensionMain` import, valid plist; no `LC_CODE_SIGNATURE`. SHA-256:

```text
05bc5ebae5d66ba9ea9c1c89446ff44ebee8c900f21b2c6fee94886365e3fa4c  PairKeyboard
cf2f0b421ba0132fbb235a4373a624ae554696f40a7029d876e3260df072c5f9  IosPair.csproj
217edbaf96f021664bd28b1e19023a1aeb46adfb413431fa1b085150c9c00dfd  build_keyboard.sh
d1c006c988a03633eb863ebdb1c51a037523c7df2e2ff94621db967c90d08960  Keyboard/KeyboardViewController.swift
8b008897e5c7e2d22890b3149d2c8eb097c5124ab547cf5a7ccdb25b7cfc6400  run_private_attempt.py
```

Private evidence token **`ss-mobile-vbbmjoob`**: `report.json` (all attempted-source hashes), `publish-pair.log`, `effective-properties.log`, `install-ios.log`, `workload-list.log`, `swift-inspect.log`, retained source/native artifact. Earlier token `ss-mobile-550iy1sj` contains the first toolchain attempt and a corrected executable-link follow-up; its initial dylib output is **not** the keyboard executable evidence. Final attempt uses `-emit-executable`/`_NSExtensionMain` throughout.

## Historical first-attempt safety / cleanup

Preflight required ≥18 GiB free (observed ~21.7 GiB). A polling supervisor monitors each owned process group, killing only that group on ≥10-minute install/20-minute overall elapsed limit, <10 GiB free, or >8 GiB apparent workspace growth. Final run: minimum **18.23 GiB** free, maximum workspace **5.35 GiB**, no limits crossed. Source and output stay inside the private workspace; no shared project builds.

Global SDK recursive metadata fingerprint before/after matched exactly:
`1eee1cbb41ebef804600b0c46217bfd215c1f736c87b4ebb9c5cefbf82148154`.
Private SDK, workload metadata, caches, homes and staging directories were removed; only owner-only (`0700`) private evidence/source/native artifact remains. Final cleanup free space **21.70 GiB** (23,294,836,736 bytes); concurrent host activity can change it. No child process remains from these synchronous process groups. No global SDK/packs/manifests, real profile/config, signing/keychain, permissions/TCC, GUI/input, clipboard, devices or simulators were modified/accessed by the build fixture.

## Local checks

```sh
python -m unittest discover -s prototypes/uno-desktop/mobile-risk/ios-pair -p 'test_*.py' -v
bash -n prototypes/uno-desktop/mobile-risk/ios-pair/build_keyboard.sh
```

Three tests passed on Windows and Mac; Mac `plutil -lint` passed all three plists. Tests check isolation fingerprint sensitivity/symlink refusal, apparent-byte accounting and fixture wiring/settings; they do **not** prove a paired app or device behavior.

App UI, OS permission UI, extension loading, embedding/signing, physical device, memory/10k projection and offline runtime verification: **NOT RUN**. Android's documented **XA1040 production blocker remains unchanged**; no Android downloads/builds were made. The single subsequently authorized compatible-toolchain attempt is recorded below; no compatibility-check suppression was used.

## Historical pinned-toolchain default/UIKit fixture — scoped strict AOT failure

Official source: [macios release `dotnet-10.0.1xx-xcode26.5-10318`](https://github.com/dotnet/macios/releases/tag/dotnet-10.0.1xx-xcode26.5-10318), independently retrieved from the [GitHub API](https://api.github.com/repos/dotnet/macios/releases/tags/dotnet-10.0.1xx-xcode26.5-10318). Its body explicitly requires **Xcode26.6**, lists stable **Microsoft.iOS.Sdk.net10.0_26.5 26.5.10318**, and selects workload set **10.0.401** ([Microsoft.NET.Workloads.10.0.400 / 10.401.0](https://www.nuget.org/packages/Microsoft.NET.Workloads.10.0.400/10.401.0)). Its Xcode27 preview instructions were **not** used.

The release recommends SDK10.0.401. [Official workload-set documentation](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-workload-sets) permits selecting a specific set with `--version` on SDK8.0.400+ and recommends matching SDK/set versions. This authorized attempt used the existing **10.0.403**, same **10.0.400 feature band**, cloned privately. Actual SDK/Xcode compatibility checks **passed**; no check was disabled and no separate SDK archive was needed.

The fixture now pins `sdk.workloadVersion=10.0.401` in its own independent `global.json`. The runner installs only `ios --version 10.0.401` from the private workspace root, outside that global.json's scope (CLI `--version` is disallowed inside a workloadVersion-pinned scope). Publish and property evaluation run inside the pinned source. It checks selected set `10.0.401` and installed iOS `26.5.10318` before publishing. The new regression test prevents a wrong/unpinned set or Android install command.

```sh
# All paths/environment are private as above; install cwd is the private workspace root.
private-dotnet workload install ios --version 10.0.401 \
  --configfile private-NuGet.Config --source https://api.nuget.org/v3/index.json \
  --temp-dir private-tmp --disable-parallel
# Publish command is unchanged from the historical attempt.
```

Result: **148.0s**, publish exit **1**. Restore and actual Uno C#/generated-XAML compilation succeeded; output reached `Generating native code` with ILCompiler **10.0.14**. Strict unsuppressed analysis emitted **two primary errors** plus **one wrapper error**:

| Code / class | Exact affected method / reason |
|---|---|
| IL2111 — trim analysis | ILC reports `<Module>..cctor()` accessing `Microsoft.UI.Xaml.Controls.Frame.CreatePageInstance(Type)` through reflection despite dynamically-accessed-member requirements; the root chain was not traced. |
| IL3050 — AOT analysis | `Microsoft.UI.Xaml.Controls.UnoWKWebView.SyncCustomHeadersToJavaScript()` calls `JsonSerializer.Serialize<Dictionary<string,string>>(..., JsonSerializerOptions)`, which requires dynamic code; ILC-reported PDB sequence point `UnoWKWebView.WebResourceRequested.iOS.cs:182`, not independently pinned source/root-chain proof. |
| MSB3077 — build wrapper | ILC task detected the above errors and reports failure, even though its process returned0. |

There were no C# fixture compilation errors. The real Native AOT diagnostics are **BLOCKED**, not warnings to suppress. No vendor repair or second compatible build attempt was made in that checkpoint. Effective strict properties remained identical to the first attempt, including only default `NoWarn=1701;1702`.

Artifact inspection found a partial `IosPair.app` directory with **no app executable and no embedded keyboard**. An intermediate `IosPair.dll` is not a Native AOT app or accepted pair. Native Swift again produced the independently inspected ARM64 executable with the same SHA-256 `05bc5ebae5d66ba9ea9c1c89446ff44ebee8c900f21b2c6fee94886365e3fa4c`; `_NSExtensionMain` import, zero `LC_CODE_SIGNATURE`, and all three plists validated. App/paired native artifact inspection, deployment and device/memory remain **NOT RUN**.

Historical default/UIKit private evidence token **`ss-mobile-b8rplmb9`** retains owner-only raw logs, attempted source hashes, native keyboard and partial outputs. New attempted-source SHA-256 values:

```text
4215106e81fe41fcbabf11d91f7fd65aac6f8456098a65e79f029af5caa3ac06  global.json
89ca15c98830c11e64e3b73056cc046b583e724bab63071d0eaeb74fbb545904  run_private_attempt.py
115d68fc145f0fbb989a0d8a5acd6f9ce4897ff9bdb87e694b4372668eed4886  test_private_attempt.py
```

`report.json` contains hashes for every attempted source, including the pre-update historical README; subsequent documentation edits are not purported to be build inputs. Other application/Swift source hashes above are unchanged.

Safety limits remained enforced: ≥18GiB preflight (observed ~21.47GiB), **17.618GiB minimum free**, **5.852GiB maximum apparent workspace**, no watchdog limits crossed. SDK fingerprint before/after exactly matched the original `1eee1cbb41ebef804600b0c46217bfd215c1f736c87b4ebb9c5cefbf82148154`. Private SDK/workload records/caches/homes and input staging were removed; private evidence/partial outputs remain `0700`. Final cleanup free **21.142GiB** (22,700,756,992 bytes). No global metadata, system SDK/Xcode, user profile, signing credential, device, GUI, input or clipboard changes occurred.

**Four** fixture tests now pass on Windows and Mac; the existing five gate-report tests still reject fabricated mobile PASS. That fixture omitted `UnoFeatures=SkiaRenderer` and used `UIApplication.Main(..., typeof(App))`; its diagnostics apply **only to that default/UIKit-entry configuration**, not a general Uno iOS no-go or evidence a fork is required. The Skia AppleUIKit path was untested at that checkpoint. No production adoption or mobile acceptance follows.

## Corrected renderer and reviewed runner

The previous default/UIKit-entry measurement is retained above with its original private inputs/results. The corrected fixture explicitly sets `UnoFeatures=SkiaRenderer;` and uses the verified public host API:

```csharp
UnoPlatformHostBuilder.Create().App(() => new App()).UseAppleUIKit().Build().Run();
```

Exact version-pinned provenance: local NuGet **Uno.Sdk6.7.30** `Sdk/Sdk.props` pins **UnoVersion6.7.135** (not6.7.38); `targets/Uno.Features.targets` normalizes empty features to `;`; `targets/Uno.Implicit.Packages.ProjectSystem.iOS.targets` adds `Uno.WinUI.Runtime.Skia.AppleUIKit` only with `;skiarenderer;`. Public **6.7.135** package repository commit **`da217df2312e9e39676880f9cb4b8d519cc297e0`** verifies the API and host/delegate separation:

- [Builder/HostBuilder.cs](https://github.com/unoplatform/uno/blob/da217df2312e9e39676880f9cb4b8d519cc297e0/src/Uno.UI.Runtime.Skia.AppleUIKit/Builder/HostBuilder.cs): public `UseAppleUIKit` extension in `Uno.UI.Hosting`.
- [Builder/AppleUIKitHostBuilder.cs](https://github.com/unoplatform/uno/blob/da217df2312e9e39676880f9cb4b8d519cc297e0/src/Uno.UI.Runtime.Skia.AppleUIKit/Builder/AppleUIKitHostBuilder.cs), [Hosting/AppleUIKitHost.cs](https://github.com/unoplatform/uno/blob/da217df2312e9e39676880f9cb4b8d519cc297e0/src/Uno.UI.Runtime.Skia.AppleUIKit/Hosting/AppleUIKitHost.cs), [UnoUIApplicationDelegate.cs](https://github.com/unoplatform/uno/blob/da217df2312e9e39676880f9cb4b8d519cc297e0/src/Uno.UI.Runtime.Skia.AppleUIKit/UnoUIApplicationDelegate.cs): Skia host constructs the Application and uses its own UIApplication delegate.
- [UnoPlatformHostBuilder.cs](https://github.com/unoplatform/uno/blob/da217df2312e9e39676880f9cb4b8d519cc297e0/src/Uno.UI/Hosting/UnoPlatformHostBuilder.cs), [UnoHostBuilderExtensions.cs](https://github.com/unoplatform/uno/blob/da217df2312e9e39676880f9cb4b8d519cc297e0/src/Uno.UI/Hosting/UnoHostBuilderExtensions.cs): public Create/App/Build API. The separate [native UIKit HostBuilder](https://github.com/unoplatform/uno/blob/da217df2312e9e39676880f9cb4b8d519cc297e0/src/Uno.UI/Hosting/UIKit/HostBuilder.UIKit.cs) is not evidence of selecting Skia by itself.

Before publishing, the runner asserts strict effective properties using identical RID/publish properties and verifies restored `Uno.WinUI.Runtime.Skia.AppleUIKit/6.7.135`, `UnoUIRuntimeIdentifier=Skia` and `HAS_UNO_SKIA_APPLE_UIKIT`. Runtime classification uses the actual publish consumer phase, not standalone reference facades: the fixture records `ManagedAssemblyToLink` immediately before `_RunILLink`, and the runner retains/classifies that source against pinned `uno-runtime/net10.0/skia/Uno.UI.dll` and hashes ILC's transformed linked `Uno.UI.dll` from its authoritative `.ilc.rsp`. Both hashes and input lists are private evidence. The [pinned macios target](https://github.com/dotnet/macios/blob/dotnet-10.0.1xx-xcode26.5-10318/dotnet/targets/Xamarin.Shared.Sdk.targets) explicitly hands ILLink output to ILC; transformed and original assembly hashes need not match. Absent/ambiguous/wrong implementation inputs block renderer verification.

Reviewer safety fixes precede the corrected attempt: SIGTERM/SIGHUP raise SystemExit130 to execute owned-group/final cleanup; cleanup is per-owned-path, idempotent, read-only tolerant and truthful. Any failed build, stop reason, changed global fingerprint or incomplete cleanup returns nonzero. The historical runner's process exit0 was **not** build success (historical publish exit1 remains unchanged). Workspace-size sampling tolerates disappearing files; SDK roots as well as children reject symlinks. Tests cover these negative cases.

Exact standalone inspection commands (runner uses `set -euo pipefail`, not unchecked semicolon chaining):

```sh
set -euo pipefail
b=native/PairKeyboard.appex/PairKeyboard
file "$b"
xcrun otool -hv "$b"
xcrun otool -L "$b"
xcrun otool -l "$b"
xcrun nm -u "$b"
plutil -lint native/PairKeyboard.appex/Info.plist Keyboard/Entitlements.plist Platforms/iOS/Info.plist
shasum -a 256 "$b"
```

If publish succeeds, the runner separately executes checked `file`, `otool -hv/-L/-l`, `nm`, and `plutil -lint` for the app and embedded keyboard; verifies byte-identical embedded Swift; requires ILC invocation/native-generation evidence plus `.ilc.rsp`; and rejects Mono.framework, libmonosgen, mono_jit_init and CoreCLR payloads/symbols before any build-artifact claim. ARM64 Mach-O alone is insufficient. Every attempted input, including configuration/plists/entry point, is hashed in the **private report**; public tables intentionally list selected hashes only, not a complete public manifest. These checks do not establish deployed/runtime/mobile acceptance.

## Latest corrected-renderer publish — registrar BLOCKED, ILC consumption unverified

All reviewer P1/P2 corrections and Mac negative tests ran **before** the new SDK attempts. The initial signal test exposed macOS EPERM after TERM left an unreaped zombie leader. A resolved-real-interpreter readiness handshake proved exact owned PID=PGID/UID and live state; numeric `ps` then proved the leader was `Z`, and `Popen.poll()` reaped it before the second signal. The group then correctly returned ESRCH. The runner now polls/reaps before each signal; it **never accepts EPERM** or broadens process termination. Both SIGTERM/SIGHUP finally/group tests subsequently passed. No SDK build was running during that investigation.

Small prerequisite/evidence-phase corrections were retained transparently, not presented as AOT failures:

| Private evidence token | Result before actual compiler/AOT work |
|---|---|
| `ss-mobile-icu_0x7l` |46.6s: inherited package `NoWarn=1701;1702;IL2121` rejected by strict gate. No publish. |
| `ss-mobile-vo294376` |46.8s: global `NoWarn=` removed suppression, but standalone ResolveReferences still exposed the native reference facade. Premature validator stopped; not an AOT diagnostic. |
| `ss-mobile-w5bvzj0d` |49.0s: publish NETSDK1112 before compile; standalone restore lacked publish-only NativeAOT runtime pack. No ILC. |
| `ss-mobile-c_xxek0g` |46.7s: publish-aware restore fixed pack selection, but evaluated SDK default `SuppressTrimAnalysisWarnings=true` was rejected. No publish. |

The parent authorized these small corrections without vendor/package/backend changes. The final runner uses the **same global strict properties** for restore, property evaluation and publish, avoiding later imported defaults; `NoWarn=` removes inherited IL2121 rather than hiding it. `_IsPublishing=true` selects the proper restore graph; publish retains its normal implicit restore. Effective properties exactly confirmed strict AOT/trim/warnings-as-errors and zero NoWarn entries.

Final command (private executable/config paths abbreviated):

```sh
private-dotnet publish IosPair.csproj -v:normal --configfile private-NuGet.Config \
  -p:Configuration=Release -p:RuntimeIdentifier=ios-arm64 -p:_IsPublishing=true \
  -p:PublishAot=true -p:PublishTrimmed=true -p:IsAotCompatible=true \
  -p:SuppressTrimAnalysisWarnings=false -p:TrimmerSingleWarn=false \
  -p:TreatWarningsAsErrors=true -p:EnableCodeSigning=false \
  -p:CodesignRequireProvisioningProfile=false -p:UseSharedCompilation=false \
  -p:NoWarn= -nodeReuse:false
```

Final evidence token **`ss-mobile-3tz850n2`**, **56.2s**, runner/publish exit **1**, build summary **0 warnings / 5 errors**. The real public `Create().App(...).UseAppleUIKit().Build().Run()` API **compiled**. Effective `UnoFeatures=;skiarenderer;`, `UnoVersion=6.7.135`, `UnoUIRuntimeIdentifier=Skia`, `HAS_UNO_SKIA_APPLE_UIKIT`, and restored AppleUIKit6.7.135 were verified. However, ILLink's ManagedRegistrar failed before ILC:

- **MT4118** primary collision: both `Microsoft.UI.Xaml.Controls.LocalWKUIDelegate, Uno.UI.Runtime.Skia.AppleUIKit` and `Microsoft.UI.Xaml.Controls.LocalWKUIDelegate, Uno.UI` register native name `Microsoft_UI_Xaml_Controls_LocalWKUIDelegate`.
- Reported wrappers: **IL7000**, **MT4116**, **MT2431**, and **NETSDK1144**. The build summary counts five error records; MT4118 text is nested/repeated in wrapper messages, not six separate root causes.

Authoritative `ManagedAssemblyToLink` manifest pointed to **`Uno.WinUI/6.7.135/lib/net10.0-ios26.0/Uno.UI.dll`**, and its copied implementation hash was **`3e5d6701eea9091345f2cd03ba6ba737e336ec38491e545e422aa2ac051ab3a0`**. This matches the version-pinned native iOS implementation, **not** portable Skia `uno-runtime/net10.0/skia/Uno.UI.dll` (hash **`0703d9c8671ef7eeb3e1943ed7ce217f261933178ec87e90f2fff3c652db63d5`**, independently hashed from the pinned package). There is **no `.ilc.rsp`**. Thus runtime-package restoration and Skia flags do **not** establish correct portable implementation consumption; complete Skia/ILC verification is **UNVERIFIED/BLOCKED**. No ILC diagnostic ran, so neither historical Frame IL2111 nor JSON IL3050 can be called fixed/absent in a completed AOT analysis. No vendor repair or further build followed this genuine registrar failure.

The standalone Swift executable again passed all checked inspections with unchanged SHA-256 `05bc5ebae5d66ba9ea9c1c89446ff44ebee8c900f21b2c6fee94886365e3fa4c`. No native management executable or embedded paired keyboard was produced. Device/UI/permission/signing/10k memory/offline-runtime checks remain **NOT RUN**. Android XA1040 remains unchanged.

Selected final attempted-source hashes (complete fixture inventory remains in private `report.json`; post-execution documentation edits are not build inputs):

```text
3f3452c6f6c04c1f0c64128eabbaccda1941ea47a6be37f739ff853a06b4e680  IosPair.csproj
85ec5f7c51b7f36366932c8852d0f2d008e1325af4013862001c766ac7f0ce27  Platforms/iOS/EntryPoint.cs
e63abf6a7aa3434d848b4f2b5d439950d86e6cdbecc89916246c443abf311037  run_private_attempt.py
74f808b7a5c722766ef9c1e60873cf8d05f639c2d5f4abdbd90d985d0a2911a7  test_private_attempt.py
```

Final minimum free **16.853GiB**, maximum apparent workspace **6.039GiB**; no disk/growth/time limit crossed. Original global SDK metadata fingerprint matched exactly before/after every invocation: `1eee1cbb41ebef804600b0c46217bfd215c1f736c87b4ebb9c5cefbf82148154`. Every owned SDK/cache/home/temp cleanup result was true; source staging was removed. Only owner-only evidence/source/partial outputs remain (final token about154MiB). Cleanup free **20.744GiB** (22,274,076,672 bytes); concurrent host activity may change it. Existing Xcode tools may use OS-managed per-UID Darwin caches; no global SDK/profile configuration, keychain/TCC/signing settings, GUI/input, clipboard or devices were touched.

Tests: **14 passed on Mac**, including real owned-group/SIGTERM/SIGHUP tests; **13 passed/1 POSIX-only skipped on Windows**. Five existing gate-report tests also remain applicable. These verify fixture/guard behavior, not artifact or mobile acceptance. Current disposition is a source- and phase-scoped registrar/implementation-selection blocker; neither this nor the historical default/UIKit failure establishes a general Uno iOS Native AOT no-go or required vendor fork.
