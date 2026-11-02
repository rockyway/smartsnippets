# 017 — Early mobile Native AOT prerequisite risk

**Status:** Historical compatible-toolchain **default/UIKit-entry** fixture is BLOCKED by IL2111/IL3050 (plus MSB3077 wrapper); it omitted `UnoFeatures=SkiaRenderer` and is not evidence of a general Uno iOS blocker. The latest corrected-Skia fixture compiled its public host API but failed ILLink ManagedRegistrar with duplicate native registration (MT4118 plus wrappers); actual linker input remained native iOS Uno.UI and no ILC response exists, so complete portable-Skia/NativeAOT consumption remains UNVERIFIED/BLOCKED. Unsigned Swift keyboard executable built/inspected; no Native AOT management app or paired artifact. Earlier workload/Xcode mismatch retained below as history. Android app/IME artifacts and all devices remain NOT RUN; Android XA1040 production blocker remains. Production adoption decision required before 019. This is an 018/T7 evidence checkpoint, not mobile feasibility acceptance.

## Confirmed scope

Phase 2 requires iOS/Android management apps plus native keyboard extensions/IME, offline local data and strict Native AOT. Mono full AOT is not silently interchangeable with the confirmed Native AOT requirement. Phase 1 remains Windows x64 and Apple Silicon macOS only.

## Earlier prerequisite-only host checks

.NET SDK 10.0.403 was used on both hosts. Windows workload inventory has only `wasm-tools-net6`; Mac inventory has no .NET workloads. Xcode's iPhoneOS SDK reports 26.5, and simulator inventory has eleven available simulators. That does not provide the missing .NET iOS workload, extension signing, physical devices or keyboard-host acceptance.

The following real publish commands were attempted sequentially on **both** hosts, using the existing explicitly prerequisite-only project:

```text
dotnet publish prototypes/uno-desktop/mobile-risk/ToolchainCheck.csproj -c Release -r android-arm64 -p:TargetFramework=net10.0-android
dotnet publish prototypes/uno-desktop/mobile-risk/ToolchainCheck.csproj -c Release -r ios-arm64 -p:TargetFramework=net10.0-ios
```

All four exited 1 with **NETSDK1147**, before native compilation or packaging. Captured output is private in owner-only OS-temp storage; evidence tokens: Windows `ss-uno018-ui-k8uo_yx4`, Mac `ss-uno018-ui-0mwdlea1`. The reused private-directory helper's `ui` prefix does not mean a GUI was launched: only workload inventory, SDK inventory and CLI publish were used. No simulators, keyboard settings, permissions or apps were activated.

`ToolchainCheck.csproj` deliberately has no app/extension implementation. These are prerequisite failures, **not app-plus-keyboard build attempts**, not proof that those artifacts are impossible, and not app/keyboard PASS evidence. No binary architecture, runtime mode, extension memory or launch assertion can be made from them. Mac free space was approximately 7 GiB after desktop probe builds, so no large workload/Android SDK installation was attempted silently.

## Current primary-source risk

Official Microsoft [XA1040 documentation](https://learn.microsoft.com/en-us/dotnet/android/messages/xa1040), also maintained in [dotnet/docs-mobile](https://github.com/dotnet/docs-mobile/blob/main/docs/android/messages/xa1040.md), states:

> warning XA1040: The NativeAOT runtime on Android is an experimental feature and not yet suitable for production use. File issues at: https://github.com/dotnet/android/issues

Live source research retrieved that statement during this checkpoint. It is a documented Android Native AOT readiness warning, **not a measured diagnostic from our missing-workload builds**. A successful generic Uno/mobile app build would not establish IME service lifecycle/registration, package composition or production suitability. Do not suppress XA1040 to claim a warning-free stable production artifact. The same official diagnostic documentation describes `EnablePreviewFeatures=true` as acknowledging the experimental runtime, not making it production-supported; removing `PublishAot=true` changes the runtime instead. Thus Android .NET 10 strict Native AOT is **BLOCKED for production adoption** under the current requirement, separately from our **NOT RUN** app/IME builds. This is a sourced policy blocker, not an observed XA1040 in our NETSDK1147 output.

Microsoft's [Native AOT iOS-like platform overview](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/ios-like-platforms/) does not establish keyboard-extension support in the retrieved material. App support is not proof of app-extension entry point, embedding, signing, App Group access or memory viability. Those remain unknown until a matching app-plus-keyboard fixture is packaged and inspected.

An alternative worth testing, not an adopted implementation, is a pure Swift/Objective-C `UIInputViewController` keyboard `.appex` embedded in the Native AOT management app via the officially documented [`AdditionalAppExtensions` build item](https://learn.microsoft.com/en-us/dotnet/ios/building-apps/build-items#additionalappextensions). This avoids assuming Uno XAML or a managed runtime must run inside the keyboard process. It still needs macOS/Xcode, the .NET iOS workload for the containing app, matching embedding/signing/App Group provisioning, and paired physical-device launch and 10k-projection memory evidence. None has been verified here.

The [.NET iOS SDK targets](https://github.com/dotnet/macios/blob/release/10.0.1xx/dotnet/targets/Xamarin.Shared.Sdk.targets) include `-framework CFNetwork` for `xamarin_start_wwan`. A CFNetwork load command in a .NET-produced executable is therefore not by itself proof of networking behavior or a failed offline policy. Future privacy inspection must distinguish SDK dependencies from actual network API use (for example URLSession, NWConnection, System.Net.Http and System.Net.Sockets), with runtime evidence; absence of those names alone is not an offline guarantee. No dependency or symbol scan of our mobile binaries occurred, because there are no such binaries.

## Earlier prerequisite evidence matrix / remaining work

| Gate | Android | iOS |
|---|---|---|
| .NET workload available | NO — NETSDK1147 | NO — NETSDK1147 |
| Strict Native AOT management app | NOT RUN | NOT RUN |
| Native IME / keyboard extension paired artifact | NOT RUN | NOT RUN |
| Package/entry point/runtime/architecture inspection | NOT RUN | NOT RUN |
| Device launch and 10k projection memory | NOT RUN | NOT RUN |
| Stable production runtime | Documented experimental warning; owner decision needed | Keyboard-extension compatibility unknown |

Before recommending 019 adoption: obtain an owner decision on unresolved mobile strict-AOT risk. The subsequently authorized private-only iOS attempt below improves prerequisite evidence without granting global installation privileges. A compatible toolchain and real paired artifact are still required. Keep iOS signing/account/device requirements explicit; do not substitute documentation, a management-only artifact or Mono AOT for the required proof.

## Offline decision-report guard

`prototypes/uno-desktop/verify_gate_report.py` documents a minimal exact-key schema and validates a complete unresolved platform/FG1–FG6 matrix from stdin. `NOT_RUN` is its machine spelling of NOT RUN. It opens no evidence paths and rejects every desktop/mobile PASS, including claimed verified receipts: the existing desktop artifact validator returns only `BUILD_CHECKS_ONLY`, and no paired mobile artifact/physical-device validator exists. Successful report validation is `UNRESOLVED_REPORT_ONLY`, never binary or runtime acceptance. The negative tests `EvidenceGateRejectsNotRunAsPass` and `MobileRiskEvidenceRejectsUnverifiedArtifacts` cover build-only, documentation/prerequisite/management-only, Mono AOT/JIT and warning/preview bypass claims. This guard does not finish T7's actual paired build/device obligations or T8's wiring audit.

No scope relaxation, production dependency choice, 019 implementation approval or Phase 2 completion follows from this checkpoint.

## Subsequent bounded actual iOS paired-build attempt (018/T7)

User authorized only an entirely private mobile SDK/workspace, with no global SDK, profile, security, signing or device changes. The actual Uno/Swift fixture is [mobile-risk/ios-pair](../../prototypes/uno-desktop/mobile-risk/ios-pair/README.md); it is independent of the production solution and shared Mac checkout. That historical fixture used Uno.Sdk6.7.30 with a real Application/XAML/UIKit entry point, strict `net10.0-ios`/`ios-arm64` Native AOT settings and a Swift UIKit keyboard `.appex` build target wired through `AdditionalAppExtensions`. It does not implement 019/022, data exchange or production UI.

Read-only host preflight: .NET SDK **10.0.403**, root-owned SDK directory approximately **1.94 GiB**, no SDK symlinks; **~21.7 GiB free**; Xcode **26.6 (17F113)**, actual iPhoneOS SDK **26.5**. The earlier prerequisite report's Xcode/SDK wording is superseded by these distinct current values.

The runner used an owner-only OS-temp workspace and APFS `cp -cR` SDK clone, checking owner, distinct file inodes and symlink absence. It isolated HOME, DOTNET_ROOT/CLI_HOME, NuGet config/cache and temp paths, with official nuget.org as the sole source. **Private** `dotnet workload install ios --skip-manifest-update` succeeded: iOS **26.0.11017/10.0.100**, Native AOT runtime pack **10.0.14**. It did not install Android, mutate the system SDK or update workload manifests.

The source-matched real paired publish command (private paths abbreviated) was:

```text
private-dotnet publish IosPair.csproj -c Release -r ios-arm64 --configfile private-NuGet.Config -p:PublishAot=true -p:EnableCodeSigning=false -p:CodesignRequireProvisioningProfile=false -p:UseSharedCompilation=false -nodeReuse:false
```

Restore succeeded; publish exited **1**, with **one unnumbered SDK/toolchain compatibility error**, at `Xamarin.Shared.Sdk.targets(2382,3)`:

> This version of .NET for iOS (26.0.11017) requires Xcode 26.0. The current version of Xcode is 26.6. Either install Xcode 26.0, or use a different version of .NET for iOS.

No management app compile/Native AOT artifact, embedding or paired-package inspection was reached. Effective `PublishAot/PublishTrimmed/IsAotCompatible=true`; `SuppressTrimAnalysisWarnings/TrimmerSingleWarn/EnableCodeSigning=false`; `NoWarn=1701;1702` only (default assembly-reference warnings, no IL suppression). No IL-analysis success is claimed. No preview flags, Mono fallback, compatibility-check bypass, Xcode installation or vendor repair was attempted.

The native Swift compile succeeded with existing `xcrun --sdk iphoneos swiftc`, warnings-as-errors, `-application-extension`, `-emit-executable`, `_NSExtensionMain` linker entry and signing disabled. `file`/`otool` verified **Mach-O ARM64 EXECUTE**, `nm` showed `_NSExtensionMain`, plist lint passed and `LC_CODE_SIGNATURE` count was zero. Executable SHA-256 **`05bc5ebae5d66ba9ea9c1c89446ff44ebee8c900f21b2c6fee94886365e3fa4c`**. This unsigned candidate executable is **not** a verified deployable keyboard or paired app.

Private final evidence token **`ss-mobile-vbbmjoob`** retains raw logs, attempted-source hashes, native artifact and report. The fixture README records selected public source hashes; all attempted inputs are hashed in the private report. Exact inspection commands for the corrected runner are documented there separately. The earlier **`ss-mobile-550iy1sj`** token records the initial attempt and executable-link correction; its initial dylib is not used as executable evidence. No private paths, usernames or log contents are published here.

Final run **36.5s**, minimum free **18.23 GiB**, maximum apparent workspace **5.35 GiB**, within ≥18 GiB preflight, ≥10 GiB running free-space floor, 8 GiB growth cap, 10-minute install and 20-minute overall bounds. The polling supervisor owns each subprocess group and stops only that group. Global SDK recursive metadata fingerprint matched before/after. Private SDKs/workload records/caches/homes and staging directories were removed; owner-only private evidence remains. Final cleanup free **21.70 GiB**. No GUI/input/clipboard/TCC/device/signing credential access or profile changes occurred. Three fixture/isolation helper tests passed on Windows and Mac; these are not mobile acceptance tests.

| Gate after first paired attempt (historical) | iOS | Android |
|---|---|---|
| Toolchain prerequisite | Private iOS workload install succeeded, then Xcode version compatibility BLOCKED | Earlier NETSDK1147; no downloads or new attempt |
| Actual management app + keyboard build | Attempted, BLOCKED before app compilation/embedding | NOT RUN |
| Native keyboard/IME artifact | Unsigned Swift ARM64 executable inspected; paired package NOT RUN | NOT RUN |
| Strict Native AOT app artifact/runtime inspection | NOT RUN after SDK validation failure | NOT RUN; sourced XA1040 production blocker unchanged |
| Deployed app/permission UI, device launch, 10k memory | NOT RUN | NOT RUN |

At that checkpoint the next prerequisite was an explicitly approved compatible iOS workload/Xcode pairing, not silent manifest/global SDK changes or version-check suppression. The single subsequently authorized compatible attempt is recorded below. Neither failure is PASS, production adoption approval, clearance of 019 or completion of T8's desktop wiring gates.

## Historical pinned-toolchain default/UIKit fixture, scoped strict Native AOT failure

The parent authorized **one** further private-only compatible-toolchain attempt within 018/T7. Official [release notes](https://github.com/dotnet/macios/releases/tag/dotnet-10.0.1xx-xcode26.5-10318), independently retrieved using the [GitHub release API](https://api.github.com/repos/dotnet/macios/releases/tags/dotnet-10.0.1xx-xcode26.5-10318), explicitly require **Xcode26.6**, list stable **iOS26.5.10318**, and prescribe workload set **10.0.401**, published as [Microsoft.NET.Workloads.10.0.400 / 10.401.0](https://www.nuget.org/packages/Microsoft.NET.Workloads.10.0.400/10.401.0). No Xcode27 preview settings were used.

The release recommends SDK10.0.401. [Official workload-set documentation](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-workload-sets) supports explicit set selection on SDK8.0.400+ and recommends matching SDK/set versions. The authorized existing SDK **10.0.403**, same **10.0.400 feature band**, was cloned privately; actual SDK/Xcode validation **passed**, without bypasses or an additional SDK download. The fixture's independent `global.json` now pins `workloadVersion=10.0.401`. Installation runs outside that file's scope, using explicit `ios --version 10.0.401`; publication runs under the pin. A new regression test verifies the install/build set pin; runtime checks verify selected set401 and installed iOS26.5.10318.

The private install succeeded. The unchanged strict paired publish command restored successfully, compiled the real Uno C#/generated XAML to an intermediate assembly, and reached **Generating native code** with ILCompiler **10.0.14**. Publish exit **1**, with **two primary analysis errors and one wrapper error**:

- **IL2111, trim analysis:** ILC reports `<Module>..cctor()` accessing `Microsoft.UI.Xaml.Controls.Frame.CreatePageInstance(Type)` via reflection despite `DynamicallyAccessedMembersAttribute` requirements.
- **IL3050, AOT analysis:** `Microsoft.UI.Xaml.Controls.UnoWKWebView.SyncCustomHeadersToJavaScript()` calls dynamic-code-requiring `JsonSerializer.Serialize<Dictionary<string,string>>(..., JsonSerializerOptions)`; ILC-reported PDB sequence point `UnoWKWebView.WebResourceRequested.iOS.cs:182` (not independently pinned source evidence).
- **MSB3077, build wrapper:** ILC task detects these errors and fails even though its process returned0.

No fixture C# compilation errors occurred. Effective `PublishAot/PublishTrimmed/IsAotCompatible=true`, `SuppressTrimAnalysisWarnings/TrimmerSingleWarn/EnableCodeSigning=false`, `NoWarn=1701;1702` only. No IL suppression, Mono fallback, preview flag, compatibility bypass or vendor repair was attempted. The `<Module>..cctor()` root chain was not traced. This ends the one authorized compatible build attempt.

Inspection found only a partial `IosPair.app` directory: **no app executable and no embedded keyboard**. An intermediate managed assembly is not an accepted artifact. The native Swift keyboard again compiled as unsigned ARM64 Mach-O EXECUTE, with `_NSExtensionMain`, valid plists, no `LC_CODE_SIGNATURE`, SHA-256 **`05bc5ebae5d66ba9ea9c1c89446ff44ebee8c900f21b2c6fee94886365e3fa4c`**. Actual app-plus-keyboard native artifact acceptance remains **BLOCKED**; app/OS permission UI, deployment, launch and10k memory remain **NOT RUN**. Android's documented XA1040 production blocker and earlier missing-workload evidence are unchanged; no Android download occurred.

Private evidence token **`ss-mobile-b8rplmb9`** retains logs, source hashes, Swift artifact and partial app outputs outside git. Source hashes: `global.json` **`4215106e81fe41fcbabf11d91f7fd65aac6f8456098a65e79f029af5caa3ac06`**; runner **`89ca15c98830c11e64e3b73056cc046b583e724bab63071d0eaeb74fbb545904`**; tests **`115d68fc145f0fbb989a0d8a5acd6f9ce4897ff9bdb87e694b4372668eed4886`**. All application/Swift source hashes are unchanged from the historical attempt; complete attempted-source inventory is private in `report.json`. README/research updates following execution are documentation, not asserted build inputs.

Attempt elapsed **148.0s**, preflight ~**21.47GiB** free, sampled minimum **17.618GiB** free, maximum apparent workspace **5.852GiB**, within all prior watchdog bounds. Original/global SDK metadata fingerprint remained exactly **`1eee1cbb41ebef804600b0c46217bfd215c1f736c87b4ebb9c5cefbf82148154`**. Private SDK, workload records, caches, homes and input staging were removed. Private evidence remains owner-only (`0700`); final cleanup free **21.142GiB** (22,700,756,992 bytes). No global toolchain/profile/security/signing/device/GUI/input/clipboard changes occurred. Four fixture tests passed locally and on Mac; no acceptance claim follows from those helper tests.

**Disposition of this historical configuration:** workload/Xcode compatibility passed, but strict Native AOT failed for the tested default/UIKit-entry fixture, which omitted `UnoFeatures=SkiaRenderer` and used `UIApplication.Main(..., typeof(App))`. These diagnostics are not a general Uno iOS no-go, do not establish the Skia AppleUIKit result, and do not prove that a fork/vendor-rooting repair is needed. The parent subsequently authorized one corrected-renderer attempt. No 019/022 implementation or production adoption was performed.

## Latest corrected-renderer checkpoint and reviewer fixes

The parent identified the default-renderer fixture defect and authorized a corrected Skia attempt, with independent review fixes **before** any new SDK launch. Exact local **Uno.Sdk6.7.30** evidence confirms `Sdk/Sdk.props` pins **UnoVersion6.7.135**, empty features normalize to `;`, and the iOS project-system target selects `Uno.WinUI.Runtime.Skia.AppleUIKit` only for `;skiarenderer;`. The version-pinned AppleUIKit6.7.135 package nuspec identifies repository commit **`da217df2312e9e39676880f9cb4b8d519cc297e0`**. Public [Skia builder extension](https://github.com/unoplatform/uno/blob/da217df2312e9e39676880f9cb4b8d519cc297e0/src/Uno.UI.Runtime.Skia.AppleUIKit/Builder/HostBuilder.cs), [host builder](https://github.com/unoplatform/uno/blob/da217df2312e9e39676880f9cb4b8d519cc297e0/src/Uno.UI.Runtime.Skia.AppleUIKit/Builder/AppleUIKitHostBuilder.cs), [host](https://github.com/unoplatform/uno/blob/da217df2312e9e39676880f9cb4b8d519cc297e0/src/Uno.UI.Runtime.Skia.AppleUIKit/Hosting/AppleUIKitHost.cs) and [delegate](https://github.com/unoplatform/uno/blob/da217df2312e9e39676880f9cb4b8d519cc297e0/src/Uno.UI.Runtime.Skia.AppleUIKit/UnoUIApplicationDelegate.cs) verify the changed entry point `UnoPlatformHostBuilder.Create().App(() => new App()).UseAppleUIKit().Build().Run()`. The project explicitly sets `UnoFeatures=SkiaRenderer;`. No SDK/package/backend version change or production code change was made.

Runner review corrections: SIGTERM/SIGHUP execute owned-group finally cleanup; each owned path is cleaned idempotently with read-only-directory handling and truthful per-path status; failures, fingerprint changes and incomplete cleanup return nonzero. Root symlinks and transient file-deletion sampling are handled. Strict effective properties use the same publish RID/global properties. Checked native inspections include file/otool/nm/plutil, and artifact claims require ILC evidence and rejection of Mono/CoreCLR payloads/symbols. Selected public hashes are explicitly distinguished from the full attempted-input manifest retained privately. New negative tests cover these guards.

A Mac signal test initially exposed EPERM when signaling an unreaped zombie process-group leader after TERM. Before SDK launch, an authorized resolved-real-interpreter readiness probe proved exact owned PID=PGID/current UID/live state; numeric `ps` then proved that leader `Z`. Reaping via the retained `Popen` handle removed the group, yielding ESRCH. The stopper now polls/reaps before each signal and **does not accept EPERM**. Both signal/finally tests then passed. No elevation, permission bypass or broad process kill was used.

Private setup stops are retained separately: **`ss-mobile-icu_0x7l`** rejected inherited `NoWarn=1701;1702;IL2121` before publishing; **`ss-mobile-vo294376`** stopped on a premature standalone ResolveReferences facade check (validation-phase defect, not AOT failure); **`ss-mobile-w5bvzj0d`** hit NETSDK1112 before compile because a separate restore lacked the publish-only runtime pack; **`ss-mobile-c_xxek0g`** rejected SDK-default `SuppressTrimAnalysisWarnings=true` in the corrected publishing context. The parent authorized these small fixture/evidence corrections. They did not change backend versions or waive strictness. Final restore/evaluation/publish use identical global strict properties, `_IsPublishing=true`, normal publish restore, and **`NoWarn=`** so neither inherited IL2121 nor later SDK defaults can suppress analysis. Exact final commands are in the fixture README; every raw setup report remains private and unchanged.

### Actual final publish result

Evidence token **`ss-mobile-3tz850n2`**, elapsed **56.2s**, publish/runner exit **1**, build summary **0 warnings / 5 errors**. The verified public Skia host API compiled, and effective properties/restored assets confirmed:

- SDK10.0.403; stable workloadset10.0.401/iOS26.5.10318; unchanged Xcode26.6.
- `UnoFeatures=;skiarenderer;`, `UnoVersion=6.7.135`, `UnoUIRuntimeIdentifier=Skia`, `HAS_UNO_SKIA_APPLE_UIKIT` and restored AppleUIKit6.7.135.
- `PublishAot/PublishTrimmed/IsAotCompatible/TreatWarningsAsErrors=true`; `SuppressTrimAnalysisWarnings/TrimmerSingleWarn/EnableCodeSigning/CodesignRequireProvisioningProfile=false`; `NoWarn` empty.

ILLink ManagedRegistrar then failed with **MT4118**: duplicate native name `Microsoft_UI_Xaml_Controls_LocalWKUIDelegate` for managed types `Microsoft.UI.Xaml.Controls.LocalWKUIDelegate` in both `Uno.UI.Runtime.Skia.AppleUIKit` and `Uno.UI`. Wrappers were **IL7000**, **MT4116**, **MT2431**, and **NETSDK1144**. The five-error build summary includes wrapper/repeated diagnostics, not five independent root causes.

The authoritative `ManagedAssemblyToLink` capture immediately before `_RunILLink` points to **native iOS** `Uno.WinUI/6.7.135/lib/net10.0-ios26.0/Uno.UI.dll`; its copied implementation hash **`3e5d6701eea9091345f2cd03ba6ba737e336ec38491e545e422aa2ac051ab3a0`** matches the pinned native assembly, not portable Skia implementation hash **`0703d9c8671ef7eeb3e1943ed7ce217f261933178ec87e90f2fff3c652db63d5`**. No `.ilc.rsp` was produced. Consequently complete portable-Skia/ILC consumption is **UNVERIFIED/BLOCKED**, despite Skia flags and runtime package being present. No ILC analysis ran, so no conclusion that historical Frame/JSON errors are gone is justified. No vendor repair or further build followed this actual registrar failure.

Native Swift ARM64 executable inspection again passed with unchanged hash **`05bc5ebae5d66ba9ea9c1c89446ff44ebee8c900f21b2c6fee94886365e3fa4c`**; no native management executable or embedded pair was produced. The final source hash inventory includes project **`3f3452c6f6c04c1f0c64128eabbaccda1941ea47a6be37f739ff853a06b4e680`**, entry point **`85ec5f7c51b7f36366932c8852d0f2d008e1325af4013862001c766ac7f0ce27`**, runner **`e63abf6a7aa3434d848b4f2b5d439950d86e6cdbecc89916246c443abf311037`**, tests **`74f808b7a5c722766ef9c1e60873cf8d05f639c2d5f4abdbd90d985d0a2911a7`**. All attempted inputs are hashed in the private report; subsequent documentation edits are not claimed build inputs.

Final sampled free-space minimum **16.853GiB**, maximum apparent workspace **6.039GiB**, within all original disk/time/growth guards. Global SDK fingerprint remained **`1eee1cbb41ebef804600b0c46217bfd215c1f736c87b4ebb9c5cefbf82148154`** before/after every invocation. Per-path private SDK/cache/home/temp cleanup was true for every setup/final token; staging was removed. Owner-only (`0700`) evidence/source/partial outputs remain; final cleanup free **20.744GiB** (22,274,076,672 bytes). Existing Xcode tools may use OS-managed per-UID Darwin caches; no global SDK/profile configuration, security/signing settings, device, GUI/input or clipboard changes occurred.

Tests: **14 passed on Mac**, **13 passed/1 POSIX-only skipped on Windows**, with five existing gate-report tests also passing. These are fixture/guard checks only. Current mobile pair/device/10k/offline runtime acceptance remains BLOCKED/NOT RUN; Android XA1040 remains unchanged. This checkpoint is not a general Uno iOS Native AOT no-go, evidence that a vendor fork is required, 019 implementation or production adoption approval.
