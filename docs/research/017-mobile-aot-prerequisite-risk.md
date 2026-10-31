# 017 — Early mobile Native AOT prerequisite risk

**Status:** NOT RUN for app/keyboard artifacts and devices; production adoption decision required before 019. This is an 018/T7 evidence checkpoint, not completion of T7 or mobile feasibility.

## Confirmed scope

Phase 2 requires iOS/Android management apps plus native keyboard extensions/IME, offline local data and strict Native AOT. Mono full AOT is not silently interchangeable with the confirmed Native AOT requirement. Phase 1 remains Windows x64 and Apple Silicon macOS only.

## Actual host checks

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

## Evidence matrix / remaining work

| Gate | Android | iOS |
|---|---|---|
| .NET workload available | NO — NETSDK1147 | NO — NETSDK1147 |
| Strict Native AOT management app | NOT RUN | NOT RUN |
| Native IME / keyboard extension paired artifact | NOT RUN | NOT RUN |
| Package/entry point/runtime/architecture inspection | NOT RUN | NOT RUN |
| Device launch and 10k projection memory | NOT RUN | NOT RUN |
| Stable production runtime | Documented experimental warning; owner decision needed | Keyboard-extension compatibility unknown |

Before recommending 019 adoption: obtain an owner decision on unresolved mobile strict-AOT risk. Before further mobile builds: agree on enough Mac disk capacity and installation privileges (or a separate suitable build host), install supported toolchains deliberately, then build/inspect a real paired management app and keyboard host. Keep iOS signing/account/device requirements explicit. If prerequisites remain unavailable, final 018 evidence must say NOT RUN and carry that risk to the owner; it must not substitute documentation, a management-only artifact or Mono AOT for the required proof.

## Offline decision-report guard

`prototypes/uno-desktop/verify_gate_report.py` documents a minimal exact-key schema and validates a complete unresolved platform/FG1–FG6 matrix from stdin. `NOT_RUN` is its machine spelling of NOT RUN. It opens no evidence paths and rejects every desktop/mobile PASS, including claimed verified receipts: the existing desktop artifact validator returns only `BUILD_CHECKS_ONLY`, and no paired mobile artifact/physical-device validator exists. Successful report validation is `UNRESOLVED_REPORT_ONLY`, never binary or runtime acceptance. The negative tests `EvidenceGateRejectsNotRunAsPass` and `MobileRiskEvidenceRejectsUnverifiedArtifacts` cover build-only, documentation/prerequisite/management-only, Mono AOT/JIT and warning/preview bypass claims. This guard does not finish T7's actual paired build/device obligations or T8's wiring audit.

No scope relaxation, production dependency choice, 019 implementation approval or Phase 2 completion follows from this checkpoint.
