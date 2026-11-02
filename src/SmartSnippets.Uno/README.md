# Plan 019 Windows Uno candidate — daily Windows features

This is an isolated implementation checkpoint, **not completed Plan 019, native parity, release readiness, or completion of Plan 018**. The legacy app remains unchanged by this candidate commit. Shared privacy/clipboard changes are separate prerequisites.

## Boundaries

- Assembly/executable: `SmartSnippets.UnoCandidate`; application ID: `org.smartsnippets.unocandidate019`; instance/profile: `SmartSnippets.UnoCandidate019`.
- The default profile is explicitly rooted under LocalApplicationData with that candidate name. Ownership is locked before profile-file enumeration or logging/migration/store access; production ancestors and the canonical production directory (including a production junction target) are refused. Production paths, canonical production aliases, reparse paths and hard-linked profile files are refused. There is no general profile CLI option.
- Ordinary launch composes real Windows services: saved global shortcut → captured target → popup → paste with clipboard restoration and success-only usage. Explicit Copy works from popup/context rows, editor, failure footer and main rows, without paste or usage credit. The admission/recovery gate prevents overlapping Copy/insertion and refuses mutation in previews or during shutdown. There is no runtime acceptance-harness unlock.
- Ordinary tray **Show popup** captures the pre-menu application window using the legacy selection policy, before popup show/focus, and inserts into that exact target after revalidation. Tooltip/menu show the actual shortcut status. The tray retains its candidate HWND/icon ID 2019 identity. QA launches (`--page`/preview/query/sort-menu requests) compose no daily native services; their previews remain target-free and non-inserting.
- Windows UI rendering remains native WinAppSDK. Uno SDK tooling is pinned to 6.7.30 with implicit Uno packages disabled; no Skia/Mac graph or runtime fallback is adopted.

## What works in ordinary launch

- The main window opens centered on the primary monitor with DPI-scaled, work-area-clamped bounds. Like legacy, no saved main-window placement is restored. Only explicit QA launch options / acceptance use bottom-left parking; without a dedicated bottom-left monitor they fall back to primary-center. Ordinary settings preview is also centered, not QA-parked.
- User-visible title, Settings and tray say **Smart Snippets**. Internal executable, profile, startup value and instance identity remain separate from legacy as listed above.
- Saved hotkey, exact-target insertion, sealed clipboard restoration, cursor-monitor popup centering, Copy and candidate-only start-at-login are composed in ordinary launch. Administrator restart remains unavailable for the handoff reasons below.

## Implemented and checked

The actual `UnoAppHost` composes the real SQLite store, settings repository and shared management/popup VMs. Tests substitute only external window/tray/message/dialog/file/exit/dispatcher boundaries; they compile the same host source, not a parallel composition graph.

Ported compiled views include management, folders, settings, editor, dialogs, JSON pickers, popup toolbar/rows/editor/footer/sort menu, hover details, palette/theme and text sizing. Start at login uses the candidate-only HKCU Run value `SmartSnippets.UnoCandidate019`, containing the quoted current executable path plus `--background`; Settings reads the actual registration. No legacy Run value or startup migration is touched. Administrator restart remains truthfully unavailable: the candidate lacks the legacy `--after-pid` relaunch/wait handoff, and a safe port must coordinate profile ownership, recovery drain and cancelled dirty-editor exit. Preview selection closes without Copy, paste or usage. The popup uses the shared `DeferredPopupHost` and `PopupShowCoordinator`, with recoverable window forwarding and cancellation of stale reveal/warmup callbacks. Exit guards main and popup drafts, suspends recovery, closes interaction admission and waits for tracked management/porting continuations before releasing profile ownership.

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

## T8 audited receipt matrix (research 059)

Audit baseline: `8d69d9d`; no native run in this audit. Sequential headless audit
verification: **368 Uno, 616 Presentation, 251 Python passed**, zero compiler
warnings. The real popup-to-main bridge has an additional behavioral regression
check; removing its callback fails it (mutation restored; see research 059).
The generated matrix above
is a **conservative promotion blocker**, not the latest scene history. It is left
non-PASS deliberately: a scoped receipt does not close an entire AC or surface.
The following matrix records the latest bounded receipts and remaining obligations.
`Rnnn` means `docs/research/nnn-*.md` (full document titles linked from research 059).
Every PASS below is limited to the cited run/head, not current HEAD or ordinary
candidate authorization. Research 057 was read from the nr-14 worktree pending
leader integration. N3 rows are **pending nr-15 (research 058)**; only the leader
may fill their source head, receipts and results.

Backlog references: [002](../../docs/backlog/002-candidate-legacy-native-coexistence.md)
(D49), [003](../../docs/backlog/003-posted-enter-input-boundary.md) (D50),
[004](../../docs/backlog/004-picker-input-epoch-attribution.md) (D75),
[005](../../docs/backlog/005-candidate-carried-acceptance-gates.md) (carried gates).
OPEN/LIMITATION is not PASS and does not amend the approved ACs.

| AC | Cited evidence / source head / scoped result | Remaining status, owner / backlog |
|---|---|---|
| AC1 coexistence | R032 / `842f3b4`: owned full-host start/exit observed; real-host isolation/redirect and copied-fixture tests (research 059), not a two-head native run | LIMITATION D49, fixed refusal: coexistence not native-closable in 019; 021 + 018 Windows owner / 002. Native redirect/tray identity pending nr-15 (research 058) / 005-N3 |
| AC2 management | R033 / primary trial `32cf03b` plus teardown `f149fd4`, diagnostics `c91d886`/`3061426`, exact enabled fingerprint `33d11ff5…`: snippets/folders/settings OBSERVED with graceful exit; R057 / `86f9cb6`: editor-copy PASS (failure Copy, not all management) | OPEN rendered residuals, relaunch and remaining porting phases: T3/T7 / 005-M. Import Cancel historical FAIL, LIMITATION D75: 021 picker owner / 004 |
| AC3 popup/lifecycle | R032 / `842f3b4`: preview/recreate/graceful exit observed. R040 preview / pre-041 enabled fingerprint `611c71a3…`: PASS, **not re-proven** | pending nr-15 (research 058): T4/T7 / 005-N3. DPI/work-area, pointer/native nonactivation and sleep/in-flight subcases remain OPEN unless explicitly receipted / 005-F |
| AC4 safe boundaries | R057 / `86f9cb6`: restore/abort/owner-refusal and all six posted/VM positives PASS (detail below); default-denied host/gate and real service consumers tested (research 059) | OPEN fault/negative/shortcut/current preview/expiry matrix: T5/T7 + 018 Windows / 005-F. Posted Enter excluded D50 / 003. No physical HID/menu delivery claim |
| AC5 privacy/legacy | R057 / `86f9cb6`: per-scene privacy CLEAN, planted controls PASS; research 059: diagnostics source audit, Presentation legacy-source regressions. R031 local legacy AOT is historical build-only evidence | OPEN final integrated legacy AOT/hosted CI and privacy review: T6/T8 leader / 005-B, 005-P; legacy product runtime not exercised here |
| AC6 reproducible AOT | R057 / `86f9cb6`: phase-0 redo PASS, five fresh strict native x64 artifacts including ordinary candidate (not launched); R031 historical local candidate/legacy gates | OPEN final integrated candidate + legacy gates and hosted CI: T6/leader / 005-B; oldest runtime NOT RUN: 021 / 005-R |
| AC7 honest evidence | R059 / audit baseline `8d69d9d`: 90-action real consumer audit, paired-head source pins and promotion guard; no native or completion claim | OPEN integration of N3/058, final builds/CI and evidence review: leader T8 / 005-N3, 005-B, 005-P. 018/020/021 retain their own gates / 005-X |

### Every T7 outstanding-table row

| Plan 019 §7 row | Receipt / head / result | Outstanding disposition / owner / backlog |
|---|---|---|
| 1 fresh candidate + legacy / hosted | R057 / `86f9cb6` phase-0 redo PASS for candidate/support artifacts, no legacy publish; R031 historical local builds only | OPEN final integrated/hosted gates, T6/leader / 005-B |
| 2 alongside legacy / redirect / background | R032 / `842f3b4` owned startup/exit only, not coexistence | LIMITATION D49 / 021 + 018 / 002; N3 redirect/background pending nr-15 (research 058), nr-15 / 005-N3 |
| 3 management / transfer / dirty & failure | R033 / source variant and fingerprint above: snippets/folders/settings OBSERVED; R057 / `86f9cb6` picker FAIL | OPEN residual rendered flows, T3/T7 / 005-M; picker LIMITATION D75 / 004 |
| 4 QA / popup / preview / hover / DPI | R032 / `842f3b4` preview/recreate observed; R040 / pre-041 fingerprint `611c71a3…` preview PASS, **not re-proven** | pending nr-15 (research 058), nr-15 / 005-N3; unreceipted DPI/native nonactivation subcases OPEN T4/T7 / 005-F |
| 5 quiet desktop / baseline / capability | R057 / `86f9cb6` admitted same-artifact native sessions; release verified stable synthetic baseline; picker correctly refused changed epoch | Scoped prerequisites PASS, not general format/input acceptance; OPEN unsupported/held/recent-input matrix, T7 + 018 / 005-F |
| 6 restore / worker abort / changed owner | R057 / `86f9cb6` restore → abort → owner-refusal PASS, RAM witnesses settled, zero usage | Scoped PASS; protective markers retained, not identical format-set restoration. Broader recovery faults OPEN T7 + 018 / 005-F |
| 7 insertion paths / shortcut / faults | R057 / `86f9cb6` posted-row, posted-main, context-insert PASS, usage once; R039 registered F11/native Enter pre-041 fingerprint `611c71a3…` PASS, **not re-proven** | Posted Enter LIMITATION D50 / 003; shortcut conflict/reacquire, physical delivery and fault matrix OPEN T5/T7 + 018 / 005-F |
| 8 all Copy / preview / expiry | R057 / `86f9cb6` popup/editor/footer Copy PASS, usage zero; R040 main-row VM Copy, preview/expiry pre-041 fingerprint `611c71a3…` PASS, **not re-proven** | OPEN current main-row Copy/preview/expiry re-proof T5/T7 + 018 / 005-F; N3 non-inserting preview pending nr-15 (research 058) / 005-N3 |
| 9 pointer / tray / exit / sleep / cleanup | R032 / `842f3b4`, R033 / variant above: scoped graceful exit observed; R057 / `86f9cb6` release PASS | pending nr-15 (research 058), nr-15 / 005-N3; unreceipted physical pointer/in-flight/sleep faults OPEN T4/T7 + 018 / 005-F |
| 10 privacy / final matrix | R057 / `86f9cb6` CLEAN and planted controls PASS; R059 source audit + matrix | OPEN integrated regressions/review, leader T8 + 018 reviewer / 005-P |
| 11 oldest Windows runtime | No run / no source receipt / NOT RUN | OPEN 021 release owner / 005-R |

### Latest native scene receipt index

All rows here cite [R057](../../docs/research/057-native-post-grant-run.md),
source `86f9cb6e57575d30bfdbc7cdb90fab23f2ae65e8`; exact private session identifiers,
artifact hashes and independent receipts are in that document. The native artifact
SHA256 is `427768f74c0cc170c341314a79deb21c48d8a50c85fbbfb570e4ce9ea1d82d1f`.

| Scene | nr-14 result / scope |
|---|---|
| restore, abort, owner-refusal | PASS each; same-artifact witness chain, zero rows/usage, recovery settled |
| posted-row, posted-main, context-insert | PASS each; exact owned target/readback, clipboard epoch changed, persisted usage 1 |
| popup-copy, editor-copy, footer-copy | PASS each; clipboard epoch changed, no paste/usage (usage 0) |
| posted-enter | NOT RUN / excluded D50 / 003 |
| picker Import Cancel | FAIL, guard refused; D75 LIMITATION / 004; zero rows/usage, unchanged clipboard, no cancel receipt |
| N3 shell/QA/tray/hover/message/lifecycle | pending nr-15 (research 058), head/result pending, owner nr-15 / 005-N3 |

Earlier R043–R056 refusals/failures remain historical failures, never retroactive
PASS. R057 successes do not re-prove R039's registered callback or R040's separate
main-row Copy/preview/expiry scenes. Source changes, including fg-04's parallel
fixture/monitor work, require equivalence review or fresh receipts before reuse.

## Still pending / not claimed

- T0/T2–T5 are a checked closed-gate checkpoint, not full task/AC closure. A bounded internal capability, guarded native adapters, authorization fixture and independent recovery guardian are implemented. R039/R040: **PASS on pre-041 source; not re-proven**. R057 establishes the scoped six-positive chain above, not ordinary runtime authorization, physical shortcut/menu delivery or full fault acceptance.
- T3/T4 compiled bindings and host tests do not prove actual Windows command delivery, dialog focus, picker behavior, menus, rendering, hover/pointer handoff, DPI/work-area placement, close/recreate callbacks or every native asynchronous shutdown race. Reparse/canonical-alias defenses require independent review and broader filesystem fixtures; current executable file-alias test exercises hard links.
- Full-host readiness/preview/recreation and graceful lifecycle ran in R032; rendered management and theme/text-size changes ran in R033. Process-level redirect, repeated native message-class cleanup and palette rendering are pending nr-15 (research 058). Relaunch persistence remains open. Coexistence is D49's fixed limitation; picker Import Cancel is D75's limitation after R057's FAIL, not merely unattempted or successful Cancel.
- The parent integrated additive Windows-only CI and passed historical local candidate/legacy gates (R031). Hosted CI and final integrated builds remain pending. T8 source/consumer/privacy/evidence audit is recorded in research 059; final integration and T7 residual acceptance remain open. Earlier `Evidence/` files describe the worker checkpoint, not acceptance of subsequent source revisions.
- Oldest supported Windows runtime is NOT RUN. macOS/mobile feasibility and all existing Plan 018 obligations remain open with their original owners.

Ordinary daily Windows services are enabled by the user’s daily-use directive, not by these historical acceptance receipts. The separate acceptance harness remains closed (`SourceReviewed=false`); its capabilities still require explicit operator authorization. Current build/headless tests do not establish native acceptance, parity or release readiness.
