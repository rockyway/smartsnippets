# 020 — Desktop toolchain and OS-floor evidence

Status: verified probe build/data/lifecycle evidence; not full Plan 018 completion or old-OS runtime acceptance. Production projects and release policies are unchanged.

## Source-backed OS policy versus executable metadata

Primary source: [.NET 10 supported operating systems at commit 44927bc821d37f596e317c4316335632bd38c79b](https://github.com/dotnet/core/blob/44927bc821d37f596e317c4316335632bd38c79b/release-notes/10.0/supported-os.md), updated 2026-09-28. The raw pinned source was fetched and checked, rather than trusting an inferred search summary. Its macOS row lists `27, 26, 15`; macOS 14 appears in the out-of-support table. OS support and minimum deployment metadata are different facts.

- **Windows:** Uno's default probe minimum was 18362, above the existing Windows product's 17763 baseline. The probe now explicitly sets both `SupportedOSPlatformVersion` and `TargetPlatformMinVersion` to `10.0.17763.0`, preserving that build baseline. This is not a promise of support for unserviced Windows editions; the upstream OS support/lifecycle policy still applies. No production minimum was changed.
- **macOS:** The probe bundle now declares `LSMinimumSystemVersion=15.0`, aligned with the current .NET 10 supported macOS range. This is a conservative probe launch floor and recommendation for later release planning, not evidence of testing on macOS 15 or approval of a final production support matrix. Only Apple Silicon is targeted.
- **Mach-O link minima:** `xcrun vtool -arch arm64 -show-build` reports entry `minos 12.0`, and native Uno/Skia/SQLite dependencies `minos 11.0`. These lower link targets do not make those OS versions vendor-supported or tested. Do not interpret the unrelated `LC_SOURCE_VERSION` number as an OS version.
- **Generic desktop MSBuild properties:** `net10.0-desktop` evaluates `SupportedOSPlatformVersion=1.0`, `TargetPlatformVersion=1.0`, and an empty `TargetPlatformMinVersion`. Those are generic desktop-TFM values, not a macOS 1.0 support claim. The bundle and native deployment metadata must be inspected separately.

Historical 12.0 bundle metadata/18362 Windows defaults remain recorded in earlier evidence; they are superseded for the artifacts below, not retroactively changed.

## Effective build configuration

Collectors now retain the OS-platform properties alongside their existing private effective-property evidence. Fresh source verification, clean native compilation, unsuppressed warning inspection and binary/SQLite validation all pass for both artifacts below.

| Field | Windows | Apple Silicon macOS |
|---|---|---|
| Tested host | Windows `10.0.26300`, x64 | macOS 26.5.2, arm64; Xcode 26.6 |
| .NET SDK / MSBuild | 10.0.403 / 18.9.15 | 10.0.403 / 18.9.15 |
| Uno SDK / framework | 6.7.30 / 6.7.135 | 6.7.30 / 6.7.135 |
| TFM / RID | `net10.0-windows10.0.26100` / `win-x64` | `net10.0-desktop` / `osx-arm64` |
| Native backend | Manual native WinAppSDK 1.8.260921001 graph | Pinned source-repaired Uno Mac runtime |
| OS-floor evidence | Both effective minimum properties `10.0.17763.0`; SDK target `10.0.26100.0` | Actual bundle launch minimum `15.0`; lower Mach-O minima as above |
| PublishAot / PublishTrimmed | true / true | true / true |
| PublishSingleFile | false | false |
| AOT / trim analyzers | true / true | true / true |
| TreatWarningsAsErrors | true | true |
| SuppressTrimAnalysisWarnings / TrimmerSingleWarn | false / false | false / false |
| NoWarn | 1701 only; no IL suppression | 1701 only; no IL suppression |

Resolved portable data dependencies are `Microsoft.Data.Sqlite/10.0.14` and `SQLitePCLRaw` bundle/core/native/provider `2.1.13`. The desktop assets also resolve `SkiaSharp/3.119.2` and `Uno.WinUI.Runtime.Skia.MacOS/6.7.135`; the Windows project's multi-target assets file contains desktop package entries too, which must not be mistaken for the selected Windows runtime closure. The Mac managed-runtime replacement retains the original native host assets and source-lock/license verification. Neither graph is silently adopted for production.

## Fresh artifacts and native verification

| Host | Build token | Native entry SHA256 | Lifecycle token |
|---|---|---|---|
| Windows | `5bf6803df42b416b9c4a8f9cb6787375` | `ee8190b850bd50e15755498c790037244279b97a825139161de90e9171a01b7f` | `ss-uno018-ui-o2nii6lc` |
| macOS | `uzs4Zg` | `26a0902559e558c82c6a9434bfba853cfa8592ccf2f87e7a800db1fa8e7fc561` | `ss-uno018-ui-k17fdzn5` |

Both freshly built native entries pass real-store seed/restart, native lifecycle compiled editor/row bindings, opposite-theme transition, UI-thread data access, hide/re-show/close/recreate and graceful exit. Windows windows remain on the bottom-left monitor. There is no keyboard, mouse or clipboard operation in these lifecycle runs. Oldest supported OS runtime execution remains NOT RUN, not inferred from clean compilation.

Both hosts pass Core 432, Presentation 599 and Python 143 tests (Windows one platform skip; macOS 28 platform skips). New pure bundle metadata tests verify the actual factory used by packaging; existing Windows MSBuild tests assert both explicit minimum properties. These tests do not replace runtime compatibility testing.

## Remaining adoption gates

The Windows implicit graph defect and bounded Mac runtime correction still require an upstream/maintenance decision before production adoption. Full hover/preview/native insertion/privacy/General recovery and mobile risk disposition remain governed by 018. Final oldest-OS release compatibility, signing and distribution acceptance belong to 021; no Windows 17763 or macOS 15 runtime PASS is claimed here.
