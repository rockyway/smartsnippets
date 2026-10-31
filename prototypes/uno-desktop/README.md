# Plan 018 — isolated Uno desktop feasibility probe

**In progress: Native AOT builds, headless native data tests and basic GUI startup smoke pass on Windows x64 and Apple Silicon. Full UI/insertion/mobile feasibility is NOT established. Not a production port.**

Current evidence, artifact digests, limitations and remaining gates: [research 016](../../docs/research/016-uno-native-aot-repair-and-data-probes.md). Earlier failures remain documented in research 012–015; they describe historical checkpoints, not the current selected configurations.

## Tooling and isolation

- Uno.Sdk 6.7.30, resolved framework 6.7.135; .NET SDK 10.0.403.
- Windows native WinAppSDK 1.8.260921001, unpackaged x64. Explicit probe minimum 17763 preserves the existing product's build floor; runtime on that oldest OS is NOT RUN.
- macOS `net10.0-desktop` / `osx-arm64` only, tested on macOS 26.5.2 / Xcode 26.6. Bundle launch minimum 15.0 follows current .NET 10 OS support; the entry's lower 12.0 link target is not a vendor-support claim. Runtime on macOS 15 is NOT RUN. See [toolchain/OS evidence](../../docs/research/020-desktop-toolchain-and-support-floors.md).
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

## Companion surface fixture (both desktops PASS; partial T3)

Native artifacts, runtime receipts and limits: [research 021](../../docs/research/021-external-companion-surface-feasibility.md).

`run_ui_smoke.py --companion` selects `--probe-ui-companion` without input synthesis, stdin preparation or clipboard access. It exercises actual production `HoverDetailsCoordinator` and `HoverDetailsPlacement.PlaceBesideWindow` against an exterior owned native companion window positioned beside the owned main window within the monitor work area (remaining on the bottom-left monitor on Windows), verifying cloaked warmup before reveal, unchanged foreground token, compiled synthetic multiline Unicode binding, programmatic `ScrollViewer` scrolling, cancel/hide, asynchronous 500 ms `Enter` hover-policy reopen, `SurfaceEntered`/`SurfaceExited` timed handoff, native companion closure/absence and natural process exit. This is partial T3 companion-surface coverage, not real OS pointer-route delivery proof.

## Private pasteboard probe (macOS PASS; NOT General clipboard acceptance)

`run_private_pasteboard_probe.py --executable <owned-entry> --sha256 <digest>` selects the exact non-GUI `--probe-private-pasteboard` mode on Apple Silicon. The probe creates a uniquely named private board, guards its object/name against General before mutation, and checks synthetic multi-item text/RTF/HTML/PNG/URL/history-marker fidelity (the native UTF-16 text alias is validated at seed time, with exact native bytes required across swap/restore), empty-board roundtrip, post-clear exception recovery, unknown/unreadable/cap refusal, capture-time ownership change and changed-owner restore refusal. General is used only for readonly identity/change-count comparison; no General content is read or written. Successful runs release the private board. Failure during construction or timeout-kill does not guarantee server-side release of its tiny synthetic data; no General contents are at risk. The runner requires exact ordered receipts, empty stderr, natural exit zero and a bounded child. The snapshot cap bounds managed copies, not the cost of arbitrary lazy-provider materialization. Real async providers, TIFF fixtures, General clipboard watchdog/recovery and paste delivery are not covered. The corrected four-item native fixture passes all nine ordered cases; initial alias/inconsistent-fixture failures remain documented in research 018. General adapter/watchdog acceptance is still unproven.

## Shortcut registration candidate (both desktops PASS; delivery NOT RUN)

`run_shortcut_registration_probe.py --executable <owned-entry> --sha256 <digest> --authorized-screen-slot` selects an exact two-argument non-GUI mode, requiring separate `--authorized-probe-shortcut`. It briefly registers/releases/re-registers only a synthetic rare chord: Ctrl+Alt+Shift+F24 via Windows thread-owned `RegisterHotKey`, or Ctrl+Option+Shift+F20 via macOS Carbon `RegisterEventHotKey` with exclusive ownership. It never posts a key, installs an all-key event tap, opens a popup, reads clipboard contents or persists settings. Failed registration must not override an existing owner; native release is verified before reporting success. Idle/chord/pointer preflight, a readonly foreground-token comparison, private logs and a bounded owned child guard the test. The foreground token is exact HWND on Windows but only frontmost PID on macOS, not AX window identity. Registration-only native runs passed on both hosts after independent high-effort review, including verified release and unchanged foreground tokens; see research 019 for artifact hashes and private receipt tokens. Actual shortcut callback delivery, UI dispatch and recovery after sleep/resume remain unverified. This is not adoption of InkSpoke's SharpHook dependency or a replacement of production Windows hotkeys.

## General snapshot observation (READ ONLY; unsupported contents refused)

`run_general_snapshot_probe.py --executable <owned-entry> --sha256 <digest> --authorized-clipboard-read` selects an exact, separately gated non-GUI entry. This independently reviewed first General-content access mode is not a clipboard swap/restore implementation. It refuses before content access unless readonly `accessBehavior` is available and ALWAYS_ALLOW; no permission prompt or TCC change is requested. It only attempts allowlisted representations, with 32-item/16-format/64-MiB managed-copy limits and start/end epoch checks. Unknown, unreadable, oversized or unstable contents are classified, never overwritten. Metadata-only access cannot establish eager provider behavior; `dataForType:` may invoke a provider and native allocation is not bounded by the managed cap. The owned child is bounded to 20 seconds with no mutation, so timeout does not require restoration.

Operator bytes are copied only into child memory, never serialized, hashed, emitted or written to files. Current managed buffers are explicitly zeroed on normal/refused/managed-exception paths; force termination relies on OS process-memory reclamation and does not claim heap/pagefile forensic erasure. Diagnostics contain fixed stage/type tokens only. `COMPLETE` means bounded stable snapshot reads succeeded, not exact restoration or General watchdog acceptance; `BUSY`/`REFUSED` are honest read-only observations, not clipboard-preservation PASS. Full guardian/recovery/restore/paste integration remains required. Native artifact `Cvu6X0` (SHA256 `9abc1d80e2c1bebb2e4f69ec97486d938cb62b2733fe9e7f02c3b628f1d43d03`) ran with private token `ss-uno018-ui-fcn9w4v3` and returned `REFUSED reason=unsupported_type`, zeroed managed buffers, empty stderr and exit zero. This is observed conservative refusal, not preservation PASS. No General contents or unknown type identifiers were logged, and no clipboard mutation occurred. A supported synthetic clipboard state is needed for later positive General tests; do not automatically replace the operator's unsupported clipboard.

## Native private-board recovery milestone (four process flows PASS)

`run_private_watchdog_probe.py --executable <owned-entry> --sha256 <digest> --authorized-private-watchdog` starts a native guardian and an independently owned native mutator for each case. Both bind only a fresh, synthetic GUID-named private board and reject General identity. The guardian holds the original ordered native representation bytes in its own memory; neither snapshots nor fixture content cross process streams or files. The mutator cannot release the guardian-owned board.

Four native flows pass: normal worker exit then exact restoration; hard-killed mutator then exact restoration; guardian stdin EOF after mutator stop then exact restoration; and a newer private owner causing restoration refusal without overwriting its bytes. The guardian independently captures the actual native swap and checks exact restoration or exact newer-owner preservation. Parent cleanup stops its worker before guardian EOF, with bounded wait/kill/reap and interrupted-wait escalation. General is used only for readonly identity/change-count checks.

Build `hO5yjg`, SHA256 `79273479026c674b5a253d8de8ac7df22292b0d97473d9dce7c679fcbed691b7`, passes all four native cases (`ss-uno018-ui-z9xthltn`), the earlier nine-case private fidelity probe (`ss-uno018-ui-a6p1bjej`) and real-store seed/restart. Both hosts pass Core 432, Presentation 599 and Python 141 (Windows one skip; Mac 28 platform skips). Consolidated high-effort milestone review found no issues.

**Limits:** this is not a General guardian or permission to generalize the private constructor. Swap ownership is acknowledged only after a completed controlled private swap. Worker death mid-clear/before acknowledgement, hard parent death with a surviving mutator, guardian death and General-provider races remain unproven; killing the guardian can leave only tiny synthetic private-board data unreleased. EOF simulation closes stdin after the worker is known stopped; it is not a hard-parent-kill test. Full General snapshot/independent recovery ownership, input and insertion remain incomplete.

## Remaining / safety

External hover, target-free keyboard preview, native shortcut/target/clipboard adapters, permission/error recovery, full privacy scans and mobile app/keyboard strict-AOT risk checks remain incomplete. Mobile prerequisite restores currently report missing workloads (NETSDK1147); no workload was silently installed. See research 016 and plan 018 for the exact acceptance gates.

The owner authorized the exclusive slot used for the basic GUI smoke recorded in research 016. `run_ui_smoke.py --executable <owned-entry> --sha256 <digest> --authorized-screen-slot` observes PID-owned visible-window metadata and stops only its started process; it sends no input and never reads titles or screen pixels. On macOS only the isolated bundle entry is accepted. This is not a visual-rendering, focus, graceful-exit or insertion test. The owner subsequently granted standing preapproval for automatic guarded tests needed to finish 018 and advance reviewed plans; OS security weakening, destructive changes, unsafe clipboard loss and unowned input remain prohibited. Keep Windows probe windows on the bottom-left monitor (bottom of the leftmost column) within its work area, moving only owned windows. Follow [harness safety](../../docs/guides/002-acceptance-harness-safety.md): owned targets, isolated profiles, watchdog, recoverable state, no plaintext operator-clipboard files, no production hotkey/instance key/startup changes. Do not begin plan 019 or claim desktop parity from this checkpoint.

### T3 programmatic preview-row checkpoint

`run_ui_smoke.py --preview-row` (with the existing hash/executable and explicit
screen-slot arguments) selects an isolated, non-interactive scene. The app still
requires exact `--authorized-probe-ui`; `--preview-row` is exclusive with the other
scenes. Secondary construction waits for the main page to load. No stdin focus
preparation, focus request or key posting is used by this scene. Real ListView
ItemClick and programmatic activation share the same row method, which validates
realized/selected/model identity before calling actual `InsertRowAsync` in preview
mode. Successful completion requires hide, fresh SQLite metadata/usage equality,
zero forbidden boundary calls, unchanged clipboard epoch and graceful cleanup.
This is **not native pointer or UIA delivery evidence**. Native Enter remains a
separate mode. Lifecycle now asserts the probe-owned theme-resource background
brush before and after opposite-theme switching; no production palette parity is
claimed.

Post-fix native lifecycle and programmatic preview-row runs pass on both desktops:
Windows build `e64d53f9fa254f95a6ddb6ac724849e2`, SHA-256
`5b646025d9a8117503a4692d305b7947a6ddf3a2fc2ef1b3d5b9eecb72bf9acf`;
macOS build `O6c1RB`, SHA-256
`2f07a92fee6b5c535f9aa7e62ce4e088572a05bc91ae82cdff388bc2de7db2c8`.
Both native collectors report zero warnings/errors; separately authorized runtime
scenes exit naturally with matching receipts. Both hosts run 159 Python tests
with platform-specific skips. See [research 022](../../docs/research/022-preview-row-and-palette-probe.md)
for the initial palette failure, correction, runtime tokens and boundaries.
Plan 018 remains incomplete.

### Windows owned native preview row (authorized slot only)

`run_ui_smoke.py --preview-row-native --authorized-screen-slot --executable <owned exe> --sha256 <hash>`
adds real guarded mouse delivery to the realized `OwnedPreviewRow` ListViewItem.
This scene is Windows-only. It does not use the main-window foreground-preparation
handshake: the runner waits for `ui_preview_row_native_ready hwnd=<popup>` on its
owned child's stdout, then passes that exact popup HWND to the existing protected
process/image/hash/UIA helper. UIA only reads the allowlisted row bounds; it never
invokes a pattern. The shared mouse helper journals before raising/moving, checks
PMv2/client bounds, desktop, idle/held input and final owned hit-test, then sends
one down/up pair. Unlike foreground preparation, it does not require the popup
to remain foreground after delivery because the real preview VM immediately hides
it. Runner recovery replays any journal even on preparation failure. Natural exit,
one actual ItemClick, shared row activation, unchanged real SQLite usage and
clipboard epoch, and zero forbidden calls are required for PASS. Existing
`--preview-row` remains programmatic and makes no native-pointer claim.
The reviewed native Windows run passes with actual ItemClick and complete mouse recovery: build `51e5f7715c2c466cafbd2bf3e0639e1d`, SHA256 `352a6cb8cc7c6b9de3d23ca1f45a23ed8b2315b841efd4053d479de715e3dadc`, runtime token `ss-uno018-ui-kgeb18nj`. No macOS pointer evidence is provided by this scene. See [research 024](../../docs/research/024-windows-native-preview-row.md) for safety fixes and limits.

### Actual external companion pointer scene (T3; native acceptance NOT RUN)

`--probe-ui-companion` / runner `--companion` remains the **programmatic** policy/scroll scene.
The separate executable flag `--companion-native-pointer` (with `--authorized-probe-ui`)
requires actual root-row PointerEntered/Exited and companion PointerEntered/Exited.
They call `CompanionPointerRoute`, which uses the actual Presentation
`HoverDetailsCoordinator`: 500ms open, 120ms handoff grace, held-open observation
for 250ms after companion entry, then surface exit -> delayed native hide -> close.
No programmatic Enter/OpenPinned fallback exists in this scene. Receipt counts and
ordering are exact; a missed/duplicate callback or timeout fails, not PASS.

Windows runner (only after independent safety review and exclusive screen authorization):
`python prototypes/uno-desktop/run_ui_smoke.py --executable <fresh-owned-exe> --sha256 <hash> --authorized-screen-slot --companion-native-pointer`.
It uses a bounded owned child helper, read-only allowlisted UIA bounds, PMv2 physical
mapping, active-desktop/all-VK-up/idle guards and exact PID/root/leaf hit-tests.
Only cursor moves occur: row -> owned intra-main nonrow point -> external surface
-> owned editor (no button/key events, clipboard, focus or Z-order calls). Every
callback receipt is flushed and awaited. Surface UIA bounds are precomputed while
still over the row; the intra-main row exit receipt is followed promptly by surface
entry within the real 120ms policy grace. System Windows PowerShell runs UIA in STA
with strong-named assemblies, not a PATH-resolved shell.

Version-2 pointer-only recovery journals atomically persist both previous and planned
cursor candidates before **every** move, including original-cursor restoration.
They retain truthful `wasTopmost`, explicit `zorderChanged=false`, and the original
physical-input epoch; legacy row-click journals remain supported. Unknown cursor
positions, changed input epochs, held input or uncertain moves retain the journal
and report pending/incomplete, never false COMPLETE or unowned key/button release.
Pointer-only replay never calls SetWindowPos. Linked journal files/parents are refused
before resolution. Original cursor positions already over any owned probe are refused.
The helper restores only after native hide and callback detachment. On **every**
still-alive pointer path, the runner stops its exact owned child and awaits teardown
before journal replay. Native execution also must confirm that the OS preserves the
physical-input epoch during cursor-only movement; a change fails closed without an
input-guard exemption. No input helper was executed during implementation; fake-API
unit tests do not prove OS callback delivery or UIA bounds availability.

Both platform heads accept the executable flag and share the XAML callbacks.
The automated pointer runner is deliberately Windows-only. Mac mirroring/build and
native acceptance belong to the coordinating operator; existing held-input refusal
is unchanged, with no exemptions or key releases. Mac native pointer acceptance,
Windows native pointer acceptance and full FG3 remain **NOT RUN**. The Windows
native insertion entry remains hard-disabled; this scene has no insertion, store,
clipboard or hotkey path. Existing preview/NoDispatch scenes are unchanged.

Portable route tests:
`dotnet test prototypes/uno-desktop/tests/CompanionPointer.Tests`.
These fail if the actual Presentation delay is bypassed, stale row-leave closes the
companion, surface leave does not hide, missed handoff is called complete, or disposed
pending work opens a surface. Python tests additionally falsify receipt deletion,
wrong HWND, held-input/wrong-leaf movement and missing-hidden recovery claims.

### T5 actual-host portable flow composition (CI boundary substitution only)

`--authorized-probe-ui --probe-ui-flow` selects an explicit interactive page backed
by the same factory and portable popup driver exercised by
[../uno-flow-tests](../uno-flow-tests/README.md). Actual Core SQLite,
Popup/Snippets VMs, popup coordinator, insertion service and shutdown coordinator
are composed once. Null-adapter forwarding, cloak/timer/close/retry policy and the
actual thin page handlers execute in CI with OS/UI-boundary substitutions.
Default native injection stays null: capture and main Copy refuse (including after
preview); popup preview Copy never reaches the clipboard. Native insertion gates
remain closed. Footer focus has a cancel/dismiss control; inline Editor focus is
explicitly NOT IMPLEMENTED, not mapped to Search. Owned profile removal waits
asynchronously for insertion restoration and VM completion. Static XAML/native
wrapper audits and Windows AOT compilation are **not native runtime acceptance**.
This mode has not been launched. Native GUI/input/clipboard/global-shortcut and
FG3/FG4/FG5 acceptance remain NOT RUN; consolidated review and separate screen
approval are required before native trials. See the linked README for exact scope.
