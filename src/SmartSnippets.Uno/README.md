# Plan 019 Windows Uno candidate — closed-gate checkpoint

This is an isolated implementation checkpoint, **not completed Plan 019, native parity, release readiness, or completion of Plan 018**. The legacy app remains unchanged by this candidate commit. Shared privacy/clipboard changes are separate prerequisites.

## Boundaries

- Assembly/executable: `SmartSnippets.UnoCandidate`; application ID: `org.smartsnippets.unocandidate019`; instance/profile: `SmartSnippets.UnoCandidate019`.
- The default profile is explicitly rooted under LocalApplicationData with that candidate name. Ownership is locked before profile-file enumeration or logging/migration/store access; production ancestors and the canonical production directory (including a production junction target) are refused. Production paths, canonical production aliases, reparse paths and hard-linked profile files are refused. There is no general profile CLI option.
- Clipboard, insertion, global shortcuts, startup registration and administrator restart are unavailable. No native clipboard, paste, external target capture, hotkey registration, registry registration or elevation adapter is composed. `CandidateNativeGate` always refuses; even clipboard-loss consent cannot reach an inner insertion service. There is **no runtime authorization/unlock mechanism**.
- Tray preview goes directly to target-free preview without external enumeration or title/process capture. Both tooltip states are candidate-branded; the tray is identified by its owned message HWND and candidate icon ID 2019, avoiding Explorer GUID binding to one build-token path. Shell/preview showing does not explicitly transfer foreground.
- Windows UI rendering remains native WinAppSDK. Uno SDK tooling is pinned to 6.7.30 with implicit Uno packages disabled; no Skia/Mac graph or runtime fallback is adopted.

## Implemented and checked

The actual `UnoAppHost` composes the real SQLite store, settings repository and shared management/popup VMs. Tests substitute only external window/tray/message/dialog/file/exit/dispatcher boundaries; they compile the same host source, not a parallel composition graph.

Ported compiled views include management, folders, settings, editor, dialogs, JSON pickers, popup toolbar/rows/editor/footer/sort menu, hover details, palette/theme and text sizing. Native entry operations are visibly unavailable. Preview selection closes without Copy, paste or usage. The popup uses the shared `DeferredPopupHost` and `PopupShowCoordinator`, with recoverable window forwarding and cancellation of stale reveal/warmup callbacks. Exit guards main and popup drafts, suspends recovery, closes interaction admission and waits for tracked management/porting continuations before releasing profile ownership.

Headless flow coverage includes real-store CRUD/folders/delete-to-Unfiled, dirty Save–Discard–Cancel/validation failure, settings persistence/refusals, JSON cancel/malformed/oversized input and actual atomic-publication refusal, offline synthetic v1/v2 fixture compatibility, corrupt/newer startup refusal, profile ownership/hard-link rejection, redirect serialization, popup recreation, preview/Copy refusal, porting drain and popup draft preservation. Message-class ownership cleanup is tested through substituted native calls. Static wiring/binding checks are supplements, not native event evidence.

## Reproduce without launching the app

Run sequentially from **this directory**, not the repository root, so CLI SDK selection uses `global.json` (10.0.403, roll-forward disabled):

```text
dotnet --version
dotnet test ../../tests/SmartSnippets.Core.Tests -c Release
dotnet test ../../tests/SmartSnippets.Presentation.Tests -c Release
dotnet test ../../tests/SmartSnippets.Uno.Tests -c Release -p:ContinuousIntegrationBuild=true
dotnet clean SmartSnippets.Uno.csproj -c Release -r win-x64 -p:ContinuousIntegrationBuild=true
dotnet restore SmartSnippets.Uno.csproj -r win-x64 -p:Configuration=Release -p:ContinuousIntegrationBuild=true
dotnet publish SmartSnippets.Uno.csproj -c Release -r win-x64 --no-restore -p:ContinuousIntegrationBuild=true -o bin/publish-uno-candidate
```

Use a freshly absent publish destination. The local verification artifact is under ignored `bin/publish-uno-candidate`, never legacy `publish/`. Parent integration owns the additive publish/CI gate and its output policy. Lock settings are candidate-csproj-scoped, not global CLI properties. Central package pins and root SDK policy are unchanged. Explicit final SDK imports clear the inherited ILLink IL2121 suppression: authored effective `NoWarn` is empty, including publishing context; no CLI override is needed to mask source settings.

Baseline suite counts before this evidence-ledger change: **436 Core, 616 Presentation, 278 candidate, 153 native fixture, 86 Target018 and 164 Python**. These are test counts, not native acceptance receipts. `Evidence/` property/artifact JSON files other than `ac-matrix.json` are historical worker snapshots, not current-source publication evidence. Research 031 records the historical local strict candidate and legacy AOT builds; a fresh final-source artifact and hosted CI receipt remain required.

Research 032 observed full-host preview/recreation and graceful shutdown; research 033 observed rendered snippets/folders/settings with graceful exit. The earlier startup-only forced-stop observation in research 031 is not full initialization evidence and is not the latest lifecycle evidence.

## Evidence matrix

`Evidence/ac-matrix.json` is the source-generated-JSON ledger. All rows are required: AC summaries and surface rows retain missing sub-obligations, so partial historical success cannot close them. Empty receipt fields mean MISSING receipt, not PASS. The source fingerprint is SHA256 of ordinal-sorted candidate-relative `.cs`/`.xaml`/`.csproj` paths plus their SHA256 digests (excluding bin, obj and Evidence); changing candidate source invalidates the checkpoint. Historical R039/R040 fingerprints remain superseded. Research documents describe private sessions; their presence is not independent native revalidation. The tests block Complete status for either 018 or 019 while any required row is not PASS.

<!-- evidence-matrix:start -->
| ID | AC | Surface / scene | Status | Receipt / session |
|---|---|---|---|---|
| AC1 | AC1 | Coexistence registry profile redirect / required acceptance | PARTIAL | MISSING |
| AC2 | AC2 | Management failure discard assignment relaunch pickers / required acceptance | PARTIAL | MISSING |
| AC3 | AC3 | Popup lifecycle nonactivation hover tray DPI / required acceptance | PARTIAL | MISSING |
| AC4 | AC4 | Safe boundaries all native consumers and faults / required acceptance | PARTIAL | MISSING |
| AC5 | AC5 | Privacy legacy compatibility hosted CI / required acceptance | PARTIAL | MISSING |
| AC6 | AC6 | Fresh final-source AOT and hosted CI / required acceptance | PARTIAL | MISSING |
| AC7 | AC7 | Honest evidence final audit / required acceptance | PARTIAL | MISSING |
| S01 | AC1 | Startup and startup failure / surface inventory | PARTIAL | MISSING |
| S02 | AC2 | Main snippets and editor / surface inventory | PARTIAL | MISSING |
| S03 | AC2 | Folders / surface inventory | PARTIAL | MISSING |
| S04 | AC2 | Settings and shortcut recorder / surface inventory | PARTIAL | MISSING |
| S05 | AC2 | Import export and dialogs / surface inventory | BLOCKED-NATIVE | MISSING |
| S06 | AC3 | Popup toolbar rows editor footer context sort / surface inventory | PARTIAL | MISSING |
| S07 | AC3 | Hover details and keyboard preview / surface inventory | BLOCKED-NATIVE | MISSING |
| S08 | AC3 | Theme palette text size brushes / surface inventory | PARTIAL | MISSING |
| S09 | AC3 | Message window tray notifier dispatcher / surface inventory | PARTIAL | MISSING |
| S10 | AC3 | LaunchOptions QA / surface inventory | PARTIAL | MISSING |
| S11 | AC4 | Windows service adapters / surface inventory | PARTIAL | MISSING |
| S12 | AC5 | Shared logging and migration / surface inventory | PARTIAL | MISSING |
| S13 | AC4 | Clipboard and three Copy VMs / surface inventory | PARTIAL | MISSING |
| S14 | AC6 | Publish CI tests evidence / surface inventory | PARTIAL | MISSING |
| R039-registered-enter | AC4 | Native boundaries / registered-enter | PARTIAL | docs/research/039-registered-shortcut-native-insertion-pass.md / ss-uno019-native-805778bc12644676accd7a63f8dbced9 |
| R040-preview | AC4 | Native boundaries / preview | PARTIAL | docs/research/040-native-preview-expiry-and-copy-boundaries.md / ss-uno019-native-edf07654100047a6af066a3420358598 |
| R040-expired | AC4 | Native boundaries / expired | PARTIAL | docs/research/040-native-preview-expiry-and-copy-boundaries.md / ss-uno019-native-22601da1f9d648fb95900f697f842912 |
| R040-copy | AC4 | Native boundaries / copy | PARTIAL | docs/research/040-native-preview-expiry-and-copy-boundaries.md / ss-uno019-native-c6cd5e49b15b4752baeacff6b133fc2a |
| R033-snippets | AC2 | Rendered management / snippets | PARTIAL | docs/research/033-primary-landscape-rendered-management.md / ss-uno018-ui-8a9a56e7ab39172a5eb3b2964d725721 |
| R033-folders | AC2 | Rendered management / folders | PARTIAL | docs/research/033-primary-landscape-rendered-management.md / ss-uno018-ui-d2e8457d8a0d821c530d88b96f1d4316 |
| R033-settings | AC2 | Rendered management / settings | PARTIAL | docs/research/033-primary-landscape-rendered-management.md / ss-uno018-ui-a879cce72998f18ba06b7b2ccb5f0818 |
<!-- evidence-matrix:end -->

## Still pending / not claimed

- T0/T2–T5 are a checked closed-gate checkpoint, not full task/AC closure. A bounded internal capability, guarded native adapters, authorization fixture and independent recovery guardian are implemented. R039/R040: **PASS on pre-041 source; not re-proven**. Registered F11/native Enter and preview/expiry/main-row VM Copy were scoped successes, not current-source acceptance or ordinary runtime authorization. Newer posted-row attempts failed (R044/R045).
- T3/T4 compiled bindings and host tests do not prove actual Windows command delivery, dialog focus, picker behavior, menus, rendering, hover/pointer handoff, DPI/work-area placement, close/recreate callbacks or every native asynchronous shutdown race. Reparse/canonical-alias defenses require independent review and broader filesystem fixtures; current executable file-alias test exercises hard links.
- Full-host readiness/preview/recreation and graceful lifecycle ran in R032; rendered management and theme/text-size changes ran in R033. Process-level redirect, repeated native message-class cleanup, legacy coexistence, palette rendering and relaunch persistence remain unproven. File pickers were BLOCKED in R043–R045, not merely unattempted.
- The parent integrated additive Windows-only CI and passed a fresh local strict artifact/property/freshness gate and the unchanged legacy AOT gate. Hosted CI, T7 native acceptance and T8 final wiring/privacy/evidence audit remain pending. Earlier `Evidence/` files describe the worker checkpoint, not acceptance of subsequent source revisions; research 031 records parent builds.
- Oldest supported Windows runtime is NOT RUN. macOS/mobile feasibility and all existing Plan 018 obligations remain open with their original owners.

Do not enable native mutations because compilation or these tests pass. Any future native capability requires separate concrete boundary/recovery review and explicit operator authorization under the acceptance-harness safety guide.
