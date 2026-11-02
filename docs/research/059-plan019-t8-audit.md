# 059 — Plan 019 T8 final wiring / evidence audit

**Scope:** t8-01, audit baseline `8d69d9d`, `feature/uno019-t8-audit`.
No screen, UI/native run, clipboard access, input synthesis, publish or delegation.
All native/review flags remain false. Plan 019's Status line is unchanged.
This is a source/consumer/evidence audit, **not AC1–AC7 closure, native parity,
Plan 018 completion or release readiness**. N3 is deliberately pending nr-15
(research 058); the integration leader owns updating those placeholders.

## Inputs and evidence policy

Read approved Plan 019 (AC1–AC7, R6, T7/T8, full surface inventory and Windows
outstanding table), latest progress handoff 006, review-rounds ledger through
rv-17, Wiring/EvidenceLedgerTests.cs and the existing action-consumer audit.
The brief's newer D49/D50/D75 decisions supersede stale progress recommendations,
not the approved acceptance criteria. No temporary execution authority is inferred.

Native chain reviewed:

| Research | Source binding / result retained by this audit |
|---|---|
| [039](039-registered-shortcut-native-insertion-pass.md), [040](040-native-preview-expiry-and-copy-boundaries.md) | Same enabled source fingerprint `611c71a3c829e4fb6fb94f5adeee8aca930bfb2bf686d61f2959e08a7b1e73f3`; registered F11/native Enter, preview, expiry and main-row VM Copy scoped PASS. **Pre-041, not re-proven**. No exact commit head is supplied by these receipts; the fingerprint/artifact, not an invented head, binds them. |
| [041](041-native-mutation-permit-ordering.md) | Post-publication/keyboard-permit ordering correction; source/build/review evidence, not a fresh native success. |
| [042](042-rendered-file-picker-review-block.md) | Picker harness review BLOCK and subsequent fixes/review; not native picker PASS. |
| [043](043-bottom-left-controls-and-picker-acceptance.md), [044](044-native-diagnostics-run.md), [045](045-native-picker-and-posted-run.md) | Repeated scoped controls PASS, picker/foreground/input refusals and registered/posted failures retained. Successful controls cannot rescue failed positives; nr-05 restore itself failed. |
| [046](046-native-exclusive-run-copy-scenes.md) | Controls PASS; picker and independent posted-row verdict FAIL, despite internal settled insertion. Copy was not granted. |
| [047](047-native-posted-and-copy-run.md) | Controls/posted-row PASS; remaining scenes FAIL. |
| [048](048-native-startup-foreground-and-input-diagnostics.md), [049](049-native-startup-foreground-diagnostic-run.md) | Diagnostics design, then baseline admission refusal/native diagnostics BLOCKED; picker input failure. Build evidence is not a scene receipt. |
| [050](050-native-admission-and-foreground-run.md), [051](051-owned-foreground-loss-analysis.md), [052](052-native-foreground-trace-run.md) | Controls/posted-row PASS, five other positives and picker FAIL. Transient samples did not prove a foreground thief or causality. 051 is source/build analysis, not native acceptance. |
| [053](053-target-other-window-analysis.md) | Fixed classifier, Invoke epochs, later thread-window diagnostics and WS_CHILD root identity analysis. The later child-EDIT finding supersedes the earlier top-level inference; diagnostic metadata never grants admission. |
| [054](054-native-target-window-run.md) | nr-11 held-input preflight FAIL/no scenes; authorized nr-11b controls PASS, six positives and picker FAIL. Original failure stays failed. |
| [055](055-native-target-thread-window-run.md) | nr-12 controls/posted-row PASS, five positives/picker FAIL; inventory identifies the relevant target child EDIT rather than a proven foreign top-level. |
| [056](056-native-foreground-root-run.md) | nr-13 controls, posted-row, footer-copy, context-insert PASS; posted-main/popup-copy/editor-copy and picker FAIL; posted-enter NOT RUN. |
| [057](057-native-post-grant-run.md) | nr-14 source `86f9cb6e57575d30bfdbc7cdb90fab23f2ae65e8`: fresh phase-0 redo, controls and **all six positives PASS**; picker FAIL. All flags closed and release settled. |

057 was read from
`.claude/worktrees/uno019-native-trial13/docs/research/057-native-post-grant-run.md`
because it is not yet present in this audit baseline. Its relative link resolves
after leader integration. No files in that worktree were edited. The source head
in 057 is newer than this baseline: this audit does not re-run or independently
authenticate private native binaries/SQLite/clipboard. It checks the documented
artifact/session/result chain and current consumer/source contracts. Private raw
records are not copied into this document. Subsequent fg-04 fixture/monitor source
changes require leader equivalence review or new receipts, not automatic re-binding.

## 1. Wiring / composition result

**PASS headless at this audit baseline**, with the native limits below. The
existing `CandidateActionConsumerTests.EveryCandidateAction_ReachesRegisteredConsumerOrTruthfulRefusal`
executes all **90** checked-in XAML action entries, not a string-presence proxy.
It uses linked unmodified code-behind, real `WindowsCandidateBoundary` view
factories and the actual `UnoAppHost` SQLite/settings/shared VMs. Only external
rendering/native/dialog/file/dispatcher/exit boundaries are substituted. Store
operations remain on the fixture's owning synchronization context. The action
manifest is not regenerated; a new/deleted/changed action requires review.

Consumer inventory (details in `tests/SmartSnippets.Uno.Tests/Wiring/README.md`):

| Surface | Executed real consumer / boundary |
|---|---|
| MainWindow | Dirty guarded navigation and page change through host VMs |
| SnippetsPage | Search/sort, browse/folder create/cancel, row/editor navigation, delete, accelerators, registered context callbacks, real import parser/export publication |
| FoldersPage | Create/rename/commit/cancel/delete and persisted SQLite effects |
| SettingsPage / ShortcutRecorder | Theme/palette/size persistence, recorder state, truthful shortcut/startup refusal, host preview |
| SnippetEditor | Actual generated commands/input handlers, folder assignment, save/discard/delete, SQLite-trigger failure/Retry and admitted/refused Copy feedback |
| Popup toolbar/rows/editor/footer/context/sort | Host open/add, selection/Enter, real InsertionService behind gate, Copy admission/revocation, registered context Insert/Copy/Edit, footer failure/cancel/dismiss, editor save/cancel/keyboard |
| SnippetRowDetails / HoverDetailsSurface | Actual handlers into shared hover coordinator; target-specific delayed open/cancel/move/handoff, zero native mutation |
| Non-XAML host/lifecycle/platform entry | Existing actual-host composition/activation/lifecycle/ownership/gate tests plus source forwarding supplements; no duplicate fake host graph |

Removing a handler breaks linked compilation; making it a no-op breaks the
observable state/store/refusal assertion. Disconnected factory Copy admission is
caught by the real factory/VM identity and delegate tests. The prior action-audit
lane documented five restored negative mutations (unmapped Click, no-op popup
handler, no-op VM command, empty refusal, disconnected factory Copy admission).
Those are prior test-quality checks, **not additional native runs by t8-01**.

A t8-01 negative mutation removed `_openMainWindow()` from the actual
`PopupWindow.OpenMainWindow` bridge. The new paired-source pin failed, but the
existing 90-action test passed: its VM uses HostFixture's separate popup boundary,
so it did not exercise that real window-to-main callback. The new
`PopupSurface_OpenMainForwardsThroughRealFactoryToGuardedManagement` test closes
this specific gap by creating both actual factory surfaces, starting on Folders,
calling the real popup forwarding surface and requiring visible Snippets navigation.
The same restored-and-replanted mutation then failed this behavioral test
(FoldersPage remained instead of SnippetsPage). Product source was restored exactly;
the complete sequential suites passed afterward. No native operations occurred.
This is a bounded bridge oracle, not a claim that all platform callbacks are
behaviorally covered; the source pins remain deliberate supplements.

`CandidateWiringAuditTests` already existed despite the brief's '(new)' label. It
is extended in place, preserving its native-forwarding/teardown/default-denial
checks and the independent behavioral suite. New R6 tests enumerate every common
head-relative `.cs`/`.xaml` file (excluding bin/obj), plus the explicitly renamed
`UnoAppHost.cs` ↔ legacy `AppHost.cs`. There are **40 pairs**. Both heads' paths and
normalized-LF source SHA256s are pinned; adding/removing a port or changing either
head fails and forces review. This deliberately over-detects harmless textual
drift; updating a pin is not a substitute for consumer tests or source review.

- Candidate paired-source digest:
  `f2f433bde7e9ac1d0ee4713b3c48abbb0c9f78e6b38323c70603bd6c66db132a`.
- Legacy paired-source digest:
  `236480e57647f200648b36bc9bb14b59ef46154ab03e9ea86b1b36aa306c7966`.
- Digest recipe: SHA256 of LF-joined `relative-path:sha256(normalized UTF-8 source)`
  in the test's explicit path order; renamed composition root last. This paired
  audit digest is distinct from the native artifact/source fingerprints.

The full enumerated list is `PortedSources` in the test (39 common paths + root
pair). Namespace-normalized comparison finds 22 identical ports and 18 divergent
pairs. The divergent set is: App.xaml.cs; SnippetEditor.xaml and .xaml.cs;
MainWindow.xaml and .xaml.cs; FoldersPage.xaml.cs; SettingsPage.xaml;
SnippetsPage.xaml.cs; PopupWindow.xaml and .xaml.cs; RecoverablePopupHost.cs;
Program.cs; DialogService.cs; MessageWindow.cs; TaskbarTheme.cs; TrayIcon.cs;
WindowPlacement.cs; UnoAppHost.cs ↔ AppHost.cs. These are intentional candidate
identity/composition, gated action, readiness/recovery, theme/dialog/placement and
lifetime differences, not shared business-logic replacements. The other 22 retain
legacy implementation after namespace substitution. Core/store/VM/coordinator
logic is shared; in particular no candidate-local DeferredPopupHost copy exists.
Candidate-only policy/profile/capability/acceptance adapters are not falsely
classified as unchanged legacy ports.

The new source/view guard rejects reflection entry points and runtime `{Binding}`
or DataContext attributes across candidate sources. Existing `x:Class`, compiled
command/action manifest and actual-handler compilation checks remain. No runtime
reflection was added. This is a static supplement, not a compiler-grade proof
against every possible reflection spelling. Generated XAML delivery, real native
callbacks, rendering/focus/pointer and arbitrary target acceptance still need
source-bound native evidence; headless stubs prove none of those.

## 2. AC / T7 evidence disposition

The candidate [README](../../src/SmartSnippets.Uno/README.md) now has an audited
matrix for **every AC1–AC7 and all eleven T7 outstanding-table rows**, including
research/source/result, explicit OPEN/LIMITATION, owner and backlog ID. It also
indexes each nr-14 positive/control scene and all N3 placeholders. Its generated
JSON-ledger block remains unchanged and conservative: no full required row is
promoted to PASS. `EvidenceLedgerTests` verifies the rendered block and blocks
018/019 Complete status when required rows/receipts/source bindings are missing.
A supplemental scoped PASS must not be substituted for that all-obligations gate.

Latest positive native receipt is 057 at `86f9cb6`, not 039/040 or the failed
044/045 attempts. Restore → abort → owner-refusal is a fresh same-artifact RAM
witness chain; posted-row, posted-main and context-insert each credit usage once;
popup/editor/footer Copy each leave usage zero. Exact readback and changed
clipboard epoch passed. This is posted/VM-callback delivery, not physical pointer,
menu selection, HID, registered-shortcut reacquisition or the full fault matrix.
Recovery retains its declared ownership/protective markers, not an identical
clipboard format set. 057 reports no pending recovery and zero owned processes;
this audit performs no new live-process or clipboard query.

N3 shell, initial/redirected QA, sort-menu/theme, hover/tray/message/lifecycle rows:
**pending nr-15 (research 058)**. Head/results must be filled by the leader. No
N3 PASS is invented from its built fixture or a UIA invocation. Unreceipted DPI,
physical pointer/nonactivation, shutdown-in-flight and sleep/re-show subcases
stay open even if another shell scene passes.

### Fixed limitations (not acceptance waivers)

| Decision | Disposition / owner / backlog |
|---|---|
| D49 coexistence | Fixed refusal: **not native-closable in 019**. Headless profile/identity isolation is useful but no two-head/registration preservation receipt exists. 021 cutover + 018 Windows owner / [002](../backlog/002-candidate-legacy-native-coexistence.md). |
| D50 posted-enter | Excluded because it uses SendInput. 057 NOT RUN. Posted row/main/context do not prove this route; 039 registered Enter is pre-041, not re-proven. 018 input/security + 021 acceptance / [003](../backlog/003-posted-enter-input-boundary.md). |
| D75 picker epoch | User closes Import Cancel as a **limitation**, not PASS. In 057 before_main_invoke is clean and after_main_invoke changed, with no dialog and no input down. Operationally OS-generated last-input change during IFileOpenDialog open / UIA Invoke; component-level causality is not proven by timestamps. Uno has no injection APIs in this path. No hook, epoch exception, rebaseline or retry. 021 picker + 018 input/security / [004](../backlog/004-picker-input-epoch-attribution.md). |

Other import/export phases are NOT RUN, not implicitly covered by this Cancel
limitation. Rendered assignment/search/sort/existing-item Discard/save-failure and
process-relaunch persistence remain explicit T3/T7 work. Headless parser/atomic
write and real-store tests do not replace picker/window acceptance.

## 3. Privacy / content-free diagnostics audit (053–057)

**PASS source audit within this diagnostic scope; no titles/content/document
paths/raw exceptions emitted by the inspected additions.** Runtime privacy CLEAN
and planted-control PASS claims are those in the cited run documents, principally
057; no new native runtime scan is claimed here.

| Producer / consumer | Retained information and privacy boundary |
|---|---|
| `scripts/uno_candidate_native_fixture/WindowDiagnosticFacts.cs` | Fixed class-token allowlist, `other` fallback, validated eight-hex `class_sha8`, booleans for styles/owner/thread/cloak/geometry/title presence. `Fields()` and `Json()` reduce arbitrary tokens before serialization. Hash input is the class name, **never a title**. UTF-16 code-unit hashing agrees with Python including unpaired surrogates. |
| `WindowDiagnosticNative.cs` | Read-only class/style/owner/thread/rectangle sampling; `GetWindowTextLengthW` produces only `title_nonempty`, never GetWindowText/title text. Geometry is reduced to facts, not document/UI content. Ready inventory caps at 32, emits hashed numeric `window_id`, A/B/new-window booleans and fixed facts. Failure is best effort and does not replace fixture ownership handoff. |
| `NativeStartupDiagnostics.cs` | Fixed phase/relation/kind tokens, counters/timings, launcher/initial relations and facts. No raw PID/HWND/class/title leaves the sample. Bounded foreground trace is at most 51 records / 250ms, not continuous proof. |
| `scripts/run_uno_candidate_native.py` | Independent fixed class map, UTF-16LE sha8, style/title-presence facts, exact-schema validators and 32-entry thread inventories. Duplicate IDs/unknown fields/types are refused. Read failures collapse to unavailable fixed facts. First monitor refusal stays latched; later snapshots are diagnostic, not permission. |
| `scripts/uno_foreground_identity.py` and C#/PowerShell root consumers | Normalize WS_CHILD only with exact nonzero root/raw PID/thread agreement; do not walk owners or allow same-PID other top-levels. `foreground_was_child` records a boolean; the same-named raw window-fact field aliases ws_child and is not evidence that an inventory window was foreground. No content is inspected. |
| `scripts/uno_candidate_ui.ps1` | Picker timeline allows only fixed phase/step tokens plus epoch, elapsed_ms and changed_from_baseline. Fixed input/foreground schemas contain booleans, numeric epochs/idle/counts and allowlisted phase/predicate/relation. Helper failure output reduces operation exceptions to fixed class/step/reason tokens and optional eight-hex HRESULT, not exception Message/ToString/stack. |
| `scripts/uno_candidate_helper_schema.py`, `run_uno_candidate_ui.py` | Strict allowlists/regex and bounded owned-file parsers validate helper telemetry before retained failure output. Schema/type failures do not admit free-form diagnostics. Import/export document paths are used for the operation but are not echoed in these diagnostic records. |

`class_sha8` and `window_id` are **not anonymization or security identities**:
32-bit truncation permits collisions, class fingerprints are dictionary-matchable,
and hashed numeric handles can correlate samples. They contain no raw titles or
document paths, but remain restricted private diagnostics. A hash match, newly
seen window or title_nonempty=true proves neither component origin nor authority.
Do not generalize these hashes to content, labels, queries or file paths. The
research records contain artifact/session metadata, not permission to publish
raw build logs, user paths or synthetic fixture payloads. Private output filenames
and root paths are destinations, not log detail values.

Review ledger caveats are preserved: rv-15 inventory/I/O and rv-16 invalid-root
findings were folded before the baseline; rv-17 GO still records sampled null-gap
timing/coverage limitations. This audit does not relabel diagnostic improvements
as native nonactivation, input provenance or physical-delivery acceptance. D75's
OS-path disposition is deliberately narrower than a proven injection source.

Existing tests exercised by the Python suite include exact class-table/schema
agreement, invalid token/type rejection, C#-shaped JSON, UTF-16 vectors, independent
style/geometry bits, raw-child root pin rejection, first-refusal latching, bounded
picker input samples, real PowerShell Invoke epoch measurements (including throw
and write-failure paths), wrapper classification/HRESULT and unknown-record
collapse. `test_uno_candidate_privacy.py` retains planted UTF-8/UTF-16 leak controls.
Presentation tests exercise safe legacy logging and actual linked legacy clipboard
source with substituted OS boundaries. Passing these does not exercise legacy
AppHost startup or its native clipboard on the desktop.

## 4. Carried owners / no completion inference

[Backlog 005](../backlog/005-candidate-carried-acceptance-gates.md) retains named
sub-obligations, not acceptance inventions:

- **019 T6/integration leader:** final integrated candidate and unchanged legacy
  fresh strict native x64/SQLite/AOT/trim/zero-warning builds and hosted additive
  plus legacy CI (005-B). 057's five fresh artifacts do not include a legacy
  publish or hosted run and do not bind later source automatically.
- **019 T3/T7:** remaining rendered management/porting/relaunch (005-M); N3 is
  pending nr-15 (research 058), integration leader fills exact scope (005-N3).
- **018 Windows T3/T4/T5/T6/T8 owner + reviewer:** unresolved pointer/focus,
  shortcut/negative/fault/recovery/in-flight/sleep and final privacy/source-adapter
  equivalence (005-F/005-P). Candidate receipts are reusable only after equivalence
  review; no retroactive 018 completion.
- **020/021 owners:** macOS dependency/fork/native/CI/parity remains deferred;
  oldest supported Windows runtime NOT RUN, distribution/signing/install,
  backup/cutover/startup and release acceptance stay in 021 (005-X/005-R).
  No Windows build-minimum, shell or source audit is runtime-support proof.
- Mobile paired/device/10k/offline feasibility remains with 022–025 (005-X).

Default capture/clipboard/hotkey/input/startup/elevation remain denied. No NoWarn,
IL suppression, acceptance criterion, production profile or legacy CI was changed.

## 5. Verification

Executed **sequentially**, with no desktop/native scene:

1. From `src/SmartSnippets.Uno`:
   `dotnet test ../../tests/SmartSnippets.Uno.Tests -c Release -p:ContinuousIntegrationBuild=true`
   — **368 passed, 0 failed, 0 skipped**.
2. From repository root:
   `dotnet test tests/SmartSnippets.Presentation.Tests -c Release`
   — **616 passed, 0 failed, 0 skipped**.
3. `python -B -m unittest discover -s scripts/tests -p "test_*.py"`
   — **251 passed**, no skips/failures.

No compiler/analyzer warnings were emitted. No native publish or full UI binding
compile was needed: product UI source is unchanged. Core standalone tests, hosted
CI and final product native builds were not rerun here; they remain integration
work, not inferred success. Operator-local AGENTS.md and safety-guide edits are
excluded from the commit. Parallel fg-04/nr-15 files are untouched.
