# Plan 018 — isolated Uno desktop feasibility probe

**In progress: Native AOT builds, headless native data tests and basic GUI startup smoke pass on Windows x64 and Apple Silicon. Full UI/insertion/mobile feasibility is NOT established. Not a production port.**

Current evidence, artifact digests, limitations and remaining gates: [research 016](../../docs/research/016-uno-native-aot-repair-and-data-probes.md). Earlier failures remain documented in research 012–015; they describe historical checkpoints, not the current selected configurations.

## Tooling and isolation

- Uno.Sdk 6.7.30, resolved framework 6.7.135; .NET SDK 10.0.403.
- Windows native WinAppSDK 1.8.260921001, unpackaged x64. Probe minimum Windows 18362 is not approval to raise production's 17763 floor.
- macOS `net10.0-desktop` / `osx-arm64` only, tested building on macOS 26.5.2 / Xcode 26.6. Native entry minimum 12.0 is not a tested product-support floor.
- Independent application identity, scoped NuGet package mode, no HotDesign/MCP servers. Production packages/source/solution/SDK/CI remain untouched.
- No default GUI launch. Both native entry points handle only explicit headless `--probe-data` before host initialization, or require `--authorized-probe-ui` for GUI mode. **The flag itself is not authorization.**

## Build and inspect only

From the repository root on Windows, PowerShell 7.2+ and Python 3:

```powershell
pwsh -NoProfile -File prototypes/uno-desktop/collect-build.ps1 -RuntimeIdentifier win-x64 -ManualWindowsPackages
```

The opt-in manual Windows graph avoids the precompiled Uno helper IL2059 by retaining native WinAppSDK dependencies without Uno helper packages. Default mode still reproduces the defect. This is approved only for continued probe work, not production adoption or full UI dependency parity; see [013](../../docs/research/013-uno-windows-aot-defect-investigation.md).

On Apple Silicon, with .NET on PATH, from `prototypes/uno-desktop`:

```sh
python3 vendor/prepare_source.py --output vendor/obj/upstream
bash collect-build.sh
```

The collector selects the pinned source-built macOS managed runtime correction described in [vendor/README.md](vendor/README.md). Two generated JSON contracts replace reflection serializer calls; native callbacks and host code remain. Source verification is mandatory before compilation. SDK Extras' CoreCLR app launcher is not used: `build_native_bundle.py` packages the compiled native entry, preserves resources/notices, ad-hoc signs owned copies and verifies the bundle. No notarization or release-signing claim is made.

Collectors clean before publish and retain private owner-only OS-temp logs/properties. Output is under ignored `bin/probe`. They never execute the app, access clipboard/profile, register hotkeys or alter TCC/HKCU. Run .NET builds/tests sequentially to avoid shared Core output collisions.

`NoWarn=1701` removes vendor IL suppression rather than adding one. Warnings-as-errors, `SuppressTrimAnalysisWarnings=false` and `TrimmerSingleWarn=false` remain. A native compile receipt is required: incremental output left by a failed ILC run cannot silently become fresh successful evidence.

## Explicit headless native data test

After inspecting a collector's native artifact and recording its SHA256:

```text
python prototypes/uno-desktop/run_data_probe.py --executable <owned-native-entry> --sha256 <recorded-SHA256>
```

Use `python3` on macOS. The runner accepts only the expected native entry inside this probe's owned output, checks its hash/header, and runs seed then verification as separate bounded processes with content-free receipts. Actual Core SQLite/store/migrations/settings/JSON run in an owner-only synthetic OS-temp profile. Successful verification removes that profile. A failed run retains synthetic data for diagnosis; do not broadly delete temp directories.

Coverage includes migration/backup, missing DB, Unicode/mixed newlines, CRUD/usage/settings, export/import, restart, corrupt/newer-schema refusal and log scan. Failure coverage includes a directory occupying the DB path and actual file-permission denial during a required v1→latest migration. Recovery asserts original permissions, independent OS read/write access and successful migration after restoration; SQLite pools are closed before permission or exclusive-file checks. No GUI host starts, and no production profile or clipboard is used. These checks do not establish UI-thread access, windowing or insertion.

## Tests / evidence validator

```text
python -m unittest discover -s prototypes/uno-desktop/tests -v
```

Validator checks effective AOT/unsuppressed properties, fresh successful native compilation, PE/Mach-O architecture, SQLite native assets, backend/RID correspondence and absence of managed apphost/JIT layouts. Result is REJECTED or BUILD_CHECKS_ONLY, never runtime feasibility PASS. Parser fixtures do not independently prove native provenance.

The `macos-json-risk` CLI links the overlay's exact generated context and natively verifies four dictionary fixtures plus the distinct navigation-header Key/Value-array wire shape with reflection disabled. It is not a GUI test.

## Lifecycle fixture (Windows and macOS PASS; partial UI coverage)

During a separately confirmed exclusive desktop slot, add `--lifecycle` to `run_ui_smoke.py` to select `--probe-ui-lifecycle`. The real-host fixture checks live source-to-control compiled editor bindings, exact Unicode/line boundaries with platform UI newline normalization, bound popup row text/font, opposite-theme change, dispatcher-thread real-store checks, native hide/re-show, close/recreate and bounded graceful exit. It does not validate editor-to-model write-back, popup warmup, external hover, keyboard focus, native preview Enter/click or insertion.

The runner requires an owned visible window, exact ordered callback receipts, empty stderr and natural exit code zero. A watchdog kill cannot pass. The native surface adapter avoids macOS's unimplemented `AppWindow.Hide()` without suppressing Uno diagnostics; its macOS enumeration reads only this probe's own synthetic window titles. Both heads compile with Native AOT, and independent pre-live safety review cleared the fixture. Native lifecycle runs now pass on both hosts, including graceful exit. Windows needed an explicit `ChoosingItemContainer` factory to construct native `ListViewItem` containers under Native AOT; the real bound-text/font/selection assertions remain unchanged. Windows owned windows are parked inside the bottom monitor of the leftmost display column. No fake PASS or weakened assertion is accepted. See research 016 for hashes, failed-before/fixed-after evidence and high-effort review.

The Mac bundle also accepts read-only `--probe-native-capabilities` without GUI startup. It queries Accessibility/event-posting preflight flags and the readonly macOS 15.4+ pasteboard access-behavior enum; no content read, prompt request or mutation occurs. The bounded native enum query passed and reported ALWAYS_ALLOW on this host; see [safety prerequisites](../../docs/research/018-native-desktop-safety-prerequisites.md). GRANTED flags are not proof that shortcut registration, exact-window validation or paste dispatch work.

## Coordinator fixture (both desktops PASS; partial T3)

Select `run_ui_smoke.py --coordinator` with the same owned-entry/hash/authorized-slot arguments. The real `PopupShowCoordinator` drives cloaked, non-activating native layout warmup, cancellation to cold, layout completion, queued search focus, hide and ready reopen without a new warmup. Success requires native foreground plus logical search focus, ordered receipts and natural process exit. On Windows only, guarded mouse preparation focuses the owned main editor before the test; it does not bypass coordinator popup focus. UIA reads owned rectangles; relative coordinates cancel DPI scaling before PMv2 physical conversion. Input-idle/client-area/live-hit-test guards and an exclusively created private recovery journal precede mutation; parent cleanup replays recovery before stopping its child. macOS preparation uses owned-process AppKit activation without synthesized input. Neither path touches clipboard. Independent high-effort review and confirming re-review cleared this harness; actual runs passed on both hosts. Exact-window external target identity, external hover and native keyboard preview/insertion are not covered.

## Native Enter preview (Windows PASS; Mac held-input blocker)

`run_ui_smoke.py --preview` requires the actual native Enter → routed TextBox handler → production `PopupViewModel.OpenPreview()` selection → owned hide path, with real SQLite reload proving unchanged usage/content/metadata. Compiled label/details bindings must realize before coordinator focus; forbidden insertion/clipboard boundaries count and throw. Read-only clipboard epochs must remain unchanged. Successful cleanup removes the private synthetic profile and requires natural exit zero; receipt-only/watchdog/duplicate output cannot pass. Windows native runs pass, including mouse recovery. macOS reaches native and logical focus but refuses Enter because its OS reports held input, despite long event inactivity. No unowned key-up/reset or safety-guard exemption is implemented. Native row activation, external hover and insertion remain unverified. See research 016 for exact artifacts, reviews and receipts.

## Remaining / safety

External hover, target-free keyboard preview, native shortcut/target/clipboard adapters, permission/error recovery, full privacy scans and mobile app/keyboard strict-AOT risk checks remain incomplete. Mobile prerequisite restores currently report missing workloads (NETSDK1147); no workload was silently installed. See research 016 and plan 018 for the exact acceptance gates.

The owner authorized the exclusive slot used for the basic GUI smoke recorded in research 016. `run_ui_smoke.py --executable <owned-entry> --sha256 <digest> --authorized-screen-slot` observes PID-owned visible-window metadata and stops only its started process; it sends no input and never reads titles or screen pixels. On macOS only the isolated bundle entry is accepted. This is not a visual-rendering, focus, graceful-exit or insertion test. The owner subsequently granted standing preapproval for automatic guarded tests needed to finish 018 and advance reviewed plans; OS security weakening, destructive changes, unsafe clipboard loss and unowned input remain prohibited. Keep Windows probe windows on the bottom-left monitor (bottom of the leftmost column) within its work area, moving only owned windows. Follow [harness safety](../../docs/guides/002-acceptance-harness-safety.md): owned targets, isolated profiles, watchdog, recoverable state, no plaintext operator-clipboard files, no production hotkey/instance key/startup changes. Do not begin plan 019 or claim desktop parity from this checkpoint.
