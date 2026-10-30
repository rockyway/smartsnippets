# Probe-scoped Uno macOS managed runtime correction

Owner authorization: continue fixing plan 018, including a pinned source-built correction. This is **not an upstream release** and is not a production dependency decision.

## Provenance / correction

`source-lock.json` pins official Uno commit `da217df2312e9e39676880f9cb4b8d519cc297e0` / framework `6.7.135`: 37 managed source files, `License.md` and `THIRD-PARTY-NOTICES.md`. `prepare_source.py` downloads them to an explicitly owned ignored directory, verifies each Git blob digest, and rewrites exactly two serializer expressions. The generated context source is checked in alongside the build project.

```sh
python3 vendor/prepare_source.py --output vendor/obj/upstream
python3 vendor/prepare_source.py --verify --output vendor/obj/upstream
```

Windows uses `python` instead of `python3`. Work from `prototypes/uno-desktop`.

Verification reverses only the two allowed replacements and rechecks every original blob; modifying another byte or an unpatched input fails. Build invokes verification again. No native host source, callback or original NuGet-cache DLL is modified. The native host continues to come from the matching published package; its bundled copy is ad-hoc signed with the other probe binaries.

The two input types remain distinct:

- Resource callback: `Dictionary<string,string>` → generated dictionary contract.
- Navigation: `HttpRequestHeaders` → generated header-collection contract (array of Key/Value pairs, not a dictionary). Native serializer proof links this exact context and checks the navigation output against an independently measured legacy serializer digest.

## Rebuild boundary

`UnoMacOSRuntime/UnoMacOSRuntime.csproj` compiles only the pinned managed backend plus the new JSON context against matching published Skia implementation assemblies. This is a narrow source build, not a full Uno repository rebuild. The public portable facades lack the backend's internal APIs, so implementation references match upstream's friend/project-reference build boundary. Assembly identity remains `Uno.UI.Runtime.Skia.MacOS`, version `255.255.255.255`; file/package metadata marks the probe correction.

The overlay removes **Uno.Analyzers**, the application-consumer “not implemented API” analyzer introduced by using packages rather than upstream implementation project references. It otherwise incorrectly reports backend setters/helpers as unsupported app calls. SDK trim/AOT analyzers, warnings-as-errors and generated System.Text.Json checks remain enabled. The Uno application still runs its consumer analyzer. No IL diagnostic is suppressed. The library builds as managed input (`PublishAot=false`), like Core/Presentation; the final application compiles that input with Native AOT and ships no managed entry DLL.

`ProbePatchedMacOSRuntime=true` selects the project only for the desktop target. The original package retains native/build assets but excludes only its managed compile/runtime asset; other Uno packages retain theirs. A regression test covers that package-selection boundary.

## Build / licensing / retirement

Use `bash collect-build.sh` on Apple Silicon after preparing sources. The standard SDK Extras 6.3.1 `PackageFormat=app` bundler links a CoreCLR launcher, so the probe's tested Native AOT bundle builder uses the already-compiled native executable instead. It includes upstream license/notices, preserves runtime-relative resources with sealed bundle-internal links, and ad-hoc signs/verifies owned copies without launching them or changing OS trust settings.

Do not publish this as an upstream package, remove callbacks, patch global package caches, or enable it in production without a separate decision. Retire the overlay once an upstream stable correction passes the same clean AOT, protocol, native data and GUI/insertion acceptance checks. Native UI, permissions and clipboard behavior still require their own evidence.
