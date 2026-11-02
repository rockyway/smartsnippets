# 012 — Uno desktop feasibility results (blocked checkpoint)

**Date:** 2026-10-30
**Status:** Initial blocked checkpoint — default graph still fails; follow-up experiment in [013](013-uno-windows-aot-defect-investigation.md) publishes in opt-in build-only mode, with no runtime gates passed
**Plan:** [018](../plan/018-uno-desktop-technical-feasibility.md), master [017](../plan/017-master-uno-cross-platform-migration.md)
**Branch:** `feature/uno-desktop-feasibility` from approved plans at `b5991aa`

## 1. Decision / stop condition

**NO-GO for 019 at this checkpoint.** T0/toolchain investigation and T1 Windows skeleton exposed a reproducible Native AOT error in the chosen stable Uno release. Preserve strict Native AOT and zero-warning policy; no production migration or native input testing follows this failed gate without an owner decision. Neither the artifact layout nor a later incremental zero-diagnostic build can waive a failed clean native compile.

No application window was launched. No insertion, clipboard access, profile move, startup registration or OS permission mutation occurred. No Apple Silicon Mac, iOS/Android workload or mobile device was available. Current data/domain/presentation and release CI remain unchanged.

## 2. Observed environment / pins

| Field | Observation |
|---|---|
| Host | Windows x64, OS `10.0.26300`; no configured Mac execution host |
| .NET SDK / runtime | SDK `10.0.403`, runtime/native compiler `10.0.14` |
| MSBuild | `18.9.15+5a2162bea` |
| NuGet source | `https://api.nuget.org/v3-flatcontainer/uno.sdk/index.json` and `uno.templates/index.json` queried; last non-prerelease in returned lists was `6.7.30` |
| Uno.Templates / Uno.Sdk | `6.7.30` / `6.7.30`, generated using a temp custom template hive |
| Resolved Uno framework | `6.7.135` (SDK and framework versions are not identical) |
| WinAppSDK | Explicit `1.8.260921001`; SDK default `1.7.250909003` was also attempted before aligning to production |
| Windows project | One multi-targeted Uno project, `WindowsPackageType=None`, `win-x64`, `net10.0-windows10.0.26100` |
| Windows min OS (evaluated) | `10.0.18362.0` — probe only; production 17763 compatibility is not established or changed |
| macOS target (evaluation only) | `net10.0-desktop`, `osx-arm64`; macOS OS floor not validated (generic SDK property `1.0` is not an OS support claim) |
| Installed workloads | Only `wasm-tools-net6`; Android/iOS workloads absent |

Root SDK/package settings, production App, Core and Presentation sources, solution and existing CI are untouched. The scoped probe imported root build defaults and attempted to disable central package management locally. Follow-up 013 exposed root NuGet CPM overriding that build property; a scoped `Directory.Packages.props` now establishes actual probe-only CPM isolation. Production libraries remain centrally managed. No global template or workload installation was performed.

## 3. Reproduction and actual failure

Run on Windows from the repository root (no GUI):

```powershell
pwsh -NoProfile -File prototypes/uno-desktop/collect-build.ps1 -RuntimeIdentifier win-x64
```

Collector behavior: owner-only OS-temp evidence → effective MSBuild property JSON → clean → fresh native publish into unique ignored `bin/probe/<run-id>` → build-only validator. Exact native publish properties include `PublishAot=true`, `PublishTrimmed=true`, `SuppressTrimAnalysisWarnings=false`, `TrimmerSingleWarn=false`, `EnableTrimAnalyzer=true`, `EnableAotAnalyzer=true`, and effective `NoWarn=1701` with **no suppressed IL diagnostics**. HotDesign/MCP development integrations are disabled. The `1701` command property removes SDK-injected `IL2121` suppression; it does not suppress the failure.

Fresh publish reaches:

```text
Generating native code
Trim analysis error IL2059:
Uno.UI.Toolkit.Uno_UI_Toolkit_Windows_XamlTypeInfo.XamlUserType.RunInitializer():
Unrecognized value passed to the parameter 'type' of method
System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(RuntimeTypeHandle).
It's not possible to guarantee the availability of the target static constructor.
error MSB3077: native compiler diagnostics fail the build
```

The upstream generated source suffix is `Uno.UI.Toolkit/obj/Uno.UI.Toolkit.Windows/Release/net9.0-windows10.0.19041.0/XamlTypeInfo.g.cs(537)`. This names the observed failing symbol/source, not a claim that this repository contains it or that every Uno version fails. No linker descriptor, warning suppression, vendor patch, SDK downgrade or JIT fallback was introduced.

**Important negative-control observation:** the failed ILC invocation left a native object. A subsequent incremental publish returned exit 0 without running ILC again. The validator rejected it for missing fresh compile receipt. A following `dotnet clean` plus publish reproduced IL2059 and was rejected for actual diagnostics/missing successful exit. Thus “native exe exists” or “latest publish log contains no warnings” alone would have produced false feasibility evidence.

Private run IDs (raw logs/properties retained under owner-only OS temp; not committed):

- `4c70c555936d48f2a962732519d7986f`: incremental native-link attempt, exit 0, validator **REJECTED** (`build:no-publish-receipt`).
- `5738f952b8d640ff9eed8a523f0283ae`: clean native publish, exit nonzero, validator **REJECTED** (`build:diagnostic`, `build:no-successful-exit`, missing published entry).
- `b011c6301c384c43a7881ebff938a9cb`: confirming clean publish after review fixes (Windows-only project properties scoped; explicit single-file/backend evidence checks), same IL2059 and **REJECTED** result.

No artifact hash/architecture/signed runtime PASS is claimed: fresh publish failed. Private raw logs include disposable filesystem roots; do not publish them without redaction. Synthetic user data was not loaded at this checkpoint.

## 4. Gate matrix

| Gate | Windows x64 | Apple Silicon macOS | Meaning |
|---|---|---|---|
| FG1 stable toolchain / restore | Restore and XAML compile observed; **BLOCKED** Native AOT IL2059 | **NOT RUN** — no matching host | No accepted desktop native artifact pair |
| FG2 SQLite fixture migrate/reopen/export | **NOT RUN** in Uno AOT host | **NOT RUN** | Existing Core tests are not Uno runtime evidence |
| FG3 two-window/warmup/external hover/preview | **NOT RUN** — no GUI slot or accepted artifact | **NOT RUN** | Single-page compiled-binding fixture only; no lifecycle proof |
| FG4 shortcut/target/clipboard/insertion | **NOT RUN / not implemented** | **NOT RUN / not implemented** | No adapter/target demo or fake PASS |
| FG5 native runtime privacy/recovery | **NOT RUN** | **NOT RUN** | Build collector/validator has no clipboard/profile side effects; runtime safety not tested |
| FG6 decision artifact / early mobile risk | This blocked report records available evidence | Same host limitations | Not overall 018 completion; 019 cannot start |

## 5. Task checkpoint and wiring audit

| Task | Status / actual producer → consumer |
|---|---|
| T0 | Partial: pinned package/template, effective properties and build-only validator. Mac toolchain not established. |
| T1 | Partial/BLOCKED: single Uno project → Windows generated XAML → native compiler → IL2059; no accepted fresh publish or runtime receipt. |
| T2 | NOT STARTED: no DataProbe or fixture data loaded; production SQLite suites run independently. |
| T3 | NOT STARTED: minimal x:Bind main-page fixture compiles on Windows, but no two-window coordinator, theme/hover/native lifecycle integration. |
| T4/T5 | NOT STARTED: no shortcut, target/clipboard adapters or input-sending code added. |
| T6 | Partial: collector → property/log files → validator → content-free REJECTED summary; positive header fixtures and red rejection controls. Full privacy/target evidence pending. |
| T7 | Prerequisite-only check: commands below fail before app/keyboard build; no mobile artifact or memory evidence. |
| T8 | Blocked handoff audit only: no production registration/binding orphan introduced; all unimplemented native paths remain explicit. |

`App.OnLaunched` refuses activation without explicit probe flag; desktop `Main` returns before native host construction without that flag. Neither guard is claimed to be a tested non-activating self-test. The collector never launches an artifact. GUI flags require separate slot authorization.

## 6. Early mobile prerequisite results (not extension feasibility)

Actual restore attempts:

```text
dotnet restore prototypes/uno-desktop/mobile-risk/ToolchainCheck.csproj
# NETSDK1147: android workload must be installed

dotnet restore prototypes/uno-desktop/mobile-risk/ToolchainCheck.csproj -p:TargetFramework=net10.0-ios
# NETSDK1147: ios workload must be installed
```

This is a restore-only project with `PublishAot=true`, **not** an Android IME or iOS keyboard extension. No workload is installed automatically and no managed assembly is presented as native evidence. App-plus-keyboard Native AOT, device launch, input delivery, shared-container access, extension memory/launch with 10k snippets, package size and signing are **NOT RUN**. This is an unknown, not proof of impossibility.

Primary research leads (not substituted for build evidence): [Microsoft iOS-like Native AOT](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/ios-like-platforms/), [Uno Native AOT](https://aka.platform.uno/native-aot), [Android input-method service](https://developer.android.com/develop/ui/views/touch-and-input/creating-input-method). These do not establish a working Native AOT keyboard host for this application.

## 7. Verification and audits

- Validator tests: **26 passed**. Initial stub run: 12 cases executed with 15 assertion/subtest failures (correct red: unsafe evidence returned no errors); after implementation/review fixes all 26 cases pass. Covers IL2059, suppressed warnings, failed/missing exit/native compile, managed/wrong-arch/truncated binary, JIT payload, metadata and missing SQLite. Header fixture success is a parser control, not real Native AOT evidence. The validator also inspected (without executing) an installed Windows .NET apphost and rejected it as `artifact:jit-host`; its embedded bundle and hostfxr markers were present.
- Actual fresh Windows publish: **FAILED**, expected stop; collector/validator exit nonzero. No runtime PASS row.
- Core: **432 passed, 0 failed, 0 skipped**; Presentation: **599 passed, 0 failed, 0 skipped**, run sequentially.
- Technical architecture self-audit: no production dependency inversion or source changes; scoped single-project skeleton, shared library references, private evidence storage, no native mutation code. Actual target/clipboard boundaries not implemented and not certified.
- Spec-versus-implementation self-audit: 018 remains **BLOCKED**, tasks/gates above explicitly incomplete; no 019 detailing or implementation. Mac, screen/input, mobile artifact and signing requirements have not been waived.
- Independent implementation review: `gemini-4-argon:medium` confirmed safe isolation and truthful BLOCKED reporting, and found validator apphost/backend test gaps plus Windows property leakage into macOS. Fixed with explicit TFM/RID and single-file/host-marker/layout rejection, added negative controls/CLI/Mach-O coverage, and conditional Windows properties. Collector hardened for PowerShell/native failure capture; unverified macOS signing claim removed. Confirming re-review cleared B1/B2 and optional O1/O2: checkpoint safe to commit, no remaining checkpoint blockers. Future Universal2 SQLite dylibs are a fail-closed validator limitation to revisit only if observed on an Apple Silicon host. The feasibility blocker is not resolved.

## 8. Owner decision required

Recommended next step: authorize bounded investigation of the generated Uno IL2059 defect (or test a different pinned stable Uno release) while preserving strict Native AOT and zero warnings. A version change or platform/backend/safety relaxation must be recorded and approved; do not silently suppress IL2059. Separately arrange Apple Silicon tooling and a screen slot when runtime tests become possible. Do not start 019 from this failed gate.
