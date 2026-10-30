# 016 — Native AOT repairs and native data probes

**Status:** In progress; build/data evidence only. Plan 018 is not complete. No production migration or live screen/input test.

## Authorization and scope

The owner requested “fix plan 018 until there is no block” after approving continued Windows probe work and SSH access to the Apple Silicon Mac. Implemented a probe-only pinned managed macOS runtime correction, not an upstream release or production adoption. Production source, central packages, SDK pin, solution and CI remain unchanged. Plans 019–025 remain placeholders.

## Corrections

1. Windows retains the approved opt-in `ProbeManualWindowsPackages=true` native WinAppSDK package graph. The default Uno helper graph still has the previously reproduced IL2059 defect; this is not an upstream fix.
2. macOS source-builds the managed backend at Uno commit `da217df2312e9e39676880f9cb4b8d519cc297e0` (framework 6.7.135). A Git-blob lock covers 37 C# files and both upstream notice/license files. Preparation changes exactly two reflection serializers to generated contracts: resource dictionaries and navigation `HttpRequestHeaders`. Verification reverses those replacements and verifies every original blob before compiling. No callbacks or native host code are removed; no global package-cache binary is modified. See [vendor boundary](../../prototypes/uno-desktop/vendor/README.md).
3. Only the original macOS package's managed compile/runtime assets are replaced. Its native/build assets and the other Uno dependencies remain. SDK AOT/trim analyzers and warnings-as-errors remain enabled. The backend library excludes the app-consumer Uno analyzer that incorrectly diagnoses implementation APIs when rebuilding against implementation assemblies; the application retains that analyzer. No IL warning suppression is added.
4. SDK Extras 6.3.1 `PackageFormat=app` attempts to link a CoreCLR launcher even for this native publish, failing `ld: library 'coreclr' not found`. The probe instead assembles a conventional bundle from the compiled native entry, signs owned binary copies ad-hoc, and verifies the sealed bundle. Runtime resources live under Resources with internal relative links for base-directory lookups; debug symbols remain outside the bundle. This is not notarization or a production installer.
5. An explicit `--probe-data` path runs before either GUI host is initialized. It uses actual Core SQLite, migration, store, settings and source-generated JSON code in an owner-only disposable OS-temp root. The launcher verifies the expected native artifact's SHA256 and owned output location, runs seed and verification in separate bounded processes, and captures content-free receipts. It does not access the production profile or clipboard.

## Reproduction

Windows, repository root (sequential with other .NET builds):

```powershell
pwsh -NoProfile -File prototypes/uno-desktop/collect-build.ps1 -RuntimeIdentifier win-x64 -ManualWindowsPackages
python -m unittest discover -s prototypes/uno-desktop/tests -v
```

Apple Silicon, from `prototypes/uno-desktop`, with the .NET 10 SDK on PATH:

```sh
python3 vendor/prepare_source.py --output vendor/obj/upstream
bash collect-build.sh
```

Collectors clean, restore, capture effective properties, publish with unsuppressed IL diagnostics and validate native output. They do not launch the probe. Native data execution is a separate explicit command from `prototypes/uno-desktop` on either host (use `python3` on macOS):

```text
python run_data_probe.py --executable <owned-native-entry> --sha256 <independently-recorded-digest>
```

## Measured evidence

Toolchain: Uno.Sdk 6.7.30 / framework 6.7.135, .NET SDK 10.0.403 / native compiler 10.0.14. Windows x64 uses WinAppSDK 1.8.260921001. Mac is macOS 26.5.2 arm64 / Xcode 26.6. Exact raw logs and effective properties remain private in owner-only OS temp; only content-free tokens/digests are recorded here.

| Check | Windows x64 | Apple Silicon macOS |
|---|---|---|
| Fresh native publish with no IL suppression | PASS, opt-in Windows graph | PASS, pinned source correction |
| Native architecture / no managed apphost / native SQLite validator | BUILD_CHECKS_ONLY | BUILD_CHECKS_ONLY |
| Evidence run token | `1be56dba32474159926b8ee8a56d3214` | `sCz5la` |
| Native entry SHA256 | `50cb2fa4fc9a0036a0dd6e0dbbd85d47a69c40eb1419107b66b7eb5df02984e8` | `4148db87bd858d978d1a16cd45853d5cd5becd5e150ddcf5904bb1f4e0be2948` |
| Separate native data seed/restart/cleanup | PASS | PASS, bundle entry |
| Ad-hoc bundle signature verification | N/A | PASS (`--strict --deep`), not notarized |
| GUI startup/windowing/focus | NOT RUN | NOT RUN |
| Hotkey/target validation/clipboard/insertion | NOT IMPLEMENTED / NOT RUN | NOT IMPLEMENTED / NOT RUN |

Both native data runs emitted only `probe_data_seed_ok`, `probe_data_restart_ok`, `data_probe_native_restart_passed gui=NOT_RUN`. Coverage: missing DB bootstrap; v1→v2 migration and backup; Unicode and mixed-newline persistence; folder/snippet CRUD; exact creation/update/usage timestamps; settings; source-generated export/import and duplicates; corrupt and newer schema refusal; log fixture leakage scan; restart and cleanup. The inaccessible-path case is a directory occupying the DB path, **not** an ACL permission-denial test. No user-supplied copied database, UI-thread dispatcher test, full OS-log scan or clipboard recovery was exercised.

The standalone native JSON proof links the exact overlay context and passes five checks with reflection disabled: four dictionary fixtures plus navigation-header serialization. Navigation uses the original Key/Value-array wire shape, independently checked against legacy serializer digest `ecc9a586e6757150414f01bdf84b470534e083ef42673499ded50e1f0daf7acc`.

Regression results: Windows 46 Python tests passed; actual Apple Silicon run executed the same 46 tests with 39 passed and 7 Windows-only property checks skipped. Earlier sequential Core and Presentation runs passed 432 and 599 tests respectively. Following independent review, source verification now rejects unlisted files (including injected wildcard C# inputs) and symlinks before build or re-preparation; data-runner temp-path tests resolve aliases and assert the exact refusal reason. The prepared Mac source tree passed the hardened verifier. Header/parser fixtures and these unit tests are not GUI feasibility evidence. The native Mac entry reports deployment minimum 12.0; this does not establish a tested product OS floor or compatibility of every dependency on that release.

## Remaining Plan 018 gates

- FG1/T1: native GUI host startup and bounded exit on both machines, plus tested OS/toolchain floor and release-signing considerations.
- FG2/T2: true permission-denial behavior, UI-thread store integration, remaining fixture/query/load checks.
- FG3/T3: representative main/editor/popup compiled bindings, multiple windows, warmup, external hover surfaces, focus, theme/geometry and preview non-insertion. Not yet implemented/validated sufficiently.
- FG4/T4–T5: owned target adapters, shortcut capture, permission denial/revocation, same-PID wrong-window rejection, safe pasteboard preservation/contention, exact-once dispatch and usage. Not implemented.
- FG5: complete app/process/owned-OS diagnostic privacy and recovery tests after native flows exist.
- T7: mobile app/keyboard strict-AOT packaging and bounded memory risk. Workloads absent; previous prerequisite diagnostic is NETSDK1147, not a framework impossibility proof. Mac disk is constrained; no large install or signing change was performed.
- T6/T8: final evidence matrix and independent audits after these flows pass. Independent review found source-set verification and macOS test-path defects, now corrected with regression coverage; README discrepancies were corrected during that review. Confirming independent re-review (gemini-4-argon:medium) cleared all checkpoint blockers with no further findings. This approves the investigation checkpoint only, not Plan 018 completion.

**Recommendation:** Continue isolated probe implementation; do not adopt the patched dependency in production or start 019. Build blockers are addressed for the selected probe configurations, but runtime feasibility is unresolved. Obtain an explicitly authorized screen/input slot on each machine before any GUI, hotkey, clipboard or target-app experiment. Never weaken Native AOT or mark unexecuted checks PASS to remove a blocker.
