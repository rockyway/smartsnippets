# 018 — Uno desktop technical feasibility

**Date:** 2026-10-30
**Status:** In progress — selected Windows/macOS Native AOT builds and headless data checks pass; GUI/insertion/mobile gates incomplete
**Size:** L (first detailed subplan of the cross-platform migration)
**Area:** isolated desktop prototype, tooling, platform boundaries, tests and evidence
**Requirement brief confirmed:** 2026-10-30 (G1)
**Master:** [017 — Uno cross-platform migration](017-master-uno-cross-platform-migration.md)
**Research:** [011 — source register and unknowns](../research/011-uno-platform-migration-research.md)

## 1. Goal

Determine whether the confirmed Phase 1 architecture can satisfy strict Native AOT, existing SQLite/data compatibility, representative Uno UI and safe original-target insertion on Windows x64 and Apple Silicon macOS **before** altering production code. A failed gate produces a specific blocker and requests a user decision; it does not silently switch backend, weaken clipboard safety or mark feasibility complete.

## 2. Requirement (confirmed at G1)

This plan implements only the **technical prototype** obligation of the confirmed brief in [master §2](017-master-uno-cross-platform-migration.md#2-requirement-confirmed-at-g1). Relevant exact brief statements:

> - **Phase 1:** Windows x64 and **Apple Silicon macOS only**.
> - Retain **Native AOT as a release requirement**, validated early per target.
> - Technical prototypes must verify Uno/AOT, SQLite, windowing, and native insertion before broad migration. Blockers require your decision, not silent requirement changes.
> - Desktop insertion retains target validation, duplicate prevention, clipboard-safety handling, and successful-dispatch usage accounting.
> - Unsupported capabilities or denied permissions produce actionable feedback, never silent fallback to another target.
> - No network service is required; logs remain free of snippet content and window titles.

**Users/surfaces for this subplan:** developer/operator runs an isolated Windows probe and an Apple Silicon Mac probe, then reviews an evidence report. A synthetic target app demonstrates capture → popup → insertion; ordinary users still use the unchanged Windows executable. No production UI or database is migrated in 018.

**Subplan acceptance (tied to master AC1, AC2, AC3, AC5, AC6):**

1. **FG1 tooling/AOT (AC3):** Pin a mutually compatible stable Uno/.NET/WinAppSDK setup, identify minimum OS/toolchain versions from actual build output/docs, and freshly publish/run **both** Windows x64 and `osx-arm64` Native AOT desktop probes; record exact commands, versions, artifact architecture, **unsuppressed** absence of IL2xxx/IL3xxx warnings and native dependencies. Failure of either is BLOCKED, not success with JIT.
2. **FG2 SQLite/data (AC1, AC3, AC5):** Both probes open and migrate **copies** of real-schema version fixtures, read/write exact Unicode/multiline data and reopen after restart. A missing DB in the disposable root must bootstrap a new schema; corrupt, newer-schema and inaccessible DB cases must fail safely without modifying the Windows production profile. macOS uses an app-private disposable path, not the user's Windows profile.
3. **FG3 windowing/UI (AC1, AC2):** Representative main/editor/popup XAML with compiled binding builds and renders on both heads; two-window creation/hide/reopen, dispatcher/UI-thread store access, popup warmup readiness, keyboard focus, target-free non-inserting preview, theme and geometry are observed. Unsupported API or external native-popup overflow without a demonstrated safe companion-surface alternative **blocks FG3**; document the exact failing path.
4. **FG4 native insertion (AC2, AC5):** On Apple Silicon with an owned synthetic target, shortcut activation captures the original target before focus, the popup hides, final target validation prevents wrong-target paste, Unicode/multiline insertion occurs at most once, and usage is credited only for positively dispatched insertion; lost target, permission denial/revocation, clipboard contention/representation loss and uncertain dispatch are tested or explicitly marked BLOCKED. Windows probe exercises the same shared policy with the existing native adapter or a faithful isolated integration; it does not mutate the production shortcut/profile.
5. **FG5 privacy/recovery (AC5, AC6):** Probe is offline; synthetic snippet content/labels/tags, target titles and user document paths/queries do not appear in app logs, captured stdout/stderr or probe-owned OS diagnostic streams. `Migrator.cs:59,88` can log raw exception paths internally; a probe caller cannot sanitize these static log writes. Keep OS temp/username roots private and redact before publishing evidence; scan synthetic content/label/tag/title/query/user-document-path fixtures in private app/process/OS logs. Record `Migrator.cs:59,88` and `SnippetsPageViewModel.cs:292,334` as confirmed production sanitization targets in 019; do not claim probe-boundary sanitization covers them. Probe cleanup restores its *owned* clipboard/startup state or fails closed and reports recovery. Never write an operator's pre-run clipboard to disk in plaintext. No input is sent without an authorized screen slot and positively identified owned synthetic target.
6. **FG6 decision artifact (AC1–AC3, AC5–AC6):** Report PASS/BLOCKED/NOT RUN by platform and gate, actual evidence paths, API mismatches, OS/permission prompts, remaining risk and an actionable go/no-go recommendation for detailing 019. No unsupported claim of signed-release notarization, Windows regression parity or macOS full feature parity. Include an early bounded iOS/Android strict-AOT app/keyboard-host risk check before adopting Uno for production in 019; this is not Phase 2 implementation.

**Non-goals:** Production Uno app head, Windows cutover, macOS complete parity, installer/auto-update, **production** mobile apps/keyboard hosts (beyond T7's isolated risk check), Intel Mac builds, cloud sync or plan 016 password mode.

## 3. Current state (as-built)

- `src/SmartSnippets.App/SmartSnippets.App.csproj:1–33` targets Windows WinUI with `PublishAot`, `win-x64`, compiled XAML and Windows App SDK; there is no Uno project. `Directory.Packages.props:1–15` centrally pins current packages. `global.json:1–7` pins the .NET SDK family; pin the probe's Uno toolchain without silently changing the production SDK baseline.
- `src/SmartSnippets.Core/SmartSnippets.Core.csproj:1–9` and `src/SmartSnippets.Presentation/SmartSnippets.Presentation.csproj:1–12` are AOT-compatible .NET 10 libraries; SQLite native assets under another RID are unverified. `src/SmartSnippets.Core/AppPaths.cs:5–19` permits an explicit isolated root.
- `src/SmartSnippets.App/AppHost.cs:98–130,137–253` is the explicit composition root, connects UI-thread store to VM/page/popup, and creates Win32 native services. `src/SmartSnippets.App/Program.cs:13–47` owns Windows instance registration; probes must not claim its instance key or profile.
- `src/SmartSnippets.Presentation/Services/PopupShowCoordinator.cs:16–79` sequences warmup, pending show and focus; `src/SmartSnippets.App/Popup/PopupWindow.xaml.cs:259–340` uses Win32 cloaking, `AppWindow` and focus APIs around it. XAML compatibility alone does not establish equivalent native behavior.
- `src/SmartSnippets.App/AppHost.cs:322–336` has **hotkey and tray** target-capture entry points. `src/SmartSnippets.Presentation/ViewModels/PopupViewModel.cs:247–254,317–370` merges main-row, keyboard and mouse insertion; `src/SmartSnippets.Presentation/ViewModels/SnippetsPageViewModel.cs:142–153` calls it. Copy from popup/main/editor remains separate (`PopupViewModel.cs:238–242,374`; `SnippetEditorViewModel.cs:328–329`). Target-free preview entry points exist in `AppHost.cs:205–208,307–310`; do not accidentally send a paste from preview.
- `src/SmartSnippets.Presentation/Services/InsertionContracts.cs:3–29` and `InsertionBoundary.cs:5–58` explicitly encode HWND/PID, CRLF, Windows clipboard formats/SendInput and AttachThreadInput. `InsertionService.cs:18–40,86–126` records usage on dispatched paste only; a macOS adapter cannot be declared contract-compatible until these semantics are reconciled.
- `src/SmartSnippets.App/Controls/HoverDetailsSurface.xaml.cs:190–265` handles root/external popups. `src/SmartSnippets.App/Popup/PopupWindow.xaml.cs:288–339` handles normal versus target-free preview, activation and monitor placement. Both represent UI surfaces a simple Hello World probe would miss.
- `.github/workflows/ci.yml:1–61` gates current Windows AOT but has no macOS job. `docs/guides/002-acceptance-harness-safety.md:1–28,79–122` defines the Windows screen/clipboard/profile safety constraints; approval of this plan is **not** authorization to drive the user's desktop.

## 4. Design

### 4.1 Approach

Add a disposable experiment under `prototypes/uno-desktop/` with independent application/bundle ID, output and profile. Keep it out of the production solution and release CI until its own documented commands pass. Reference the actual Core/Presentation assemblies for data/policy validation; representative copied XAML is a compatibility fixture, never silently treated as a replacement of production views. Use separate native Windows/macOS host adapter prototypes and one probe orchestration path that calls real code for the store, popup coordinator and dispatch accounting where compatible. `InsertionService.cs:40` already converts LF to CRLF; record this macOS contract mismatch and per-platform expected newline transformation, rather than claiming unchanged shared code inserts exact LF. Test shared single-flight/usage policy where compatible and label adapter-only evidence; design the production fix in 019. If other Windows-specific contracts prevent honest reuse, record each mismatch and test a minimal adapter without changing production contracts in 018.

On macOS use native APIs only within a known owned target process. Research candidate global shortcut registration without presuming Input Monitoring permission; test actual behavior of registration, capture and posting under authorized permissions. Preserve clipboard before mutation and restore only if the probe still owns its change; classify eager multi-item/multi-type payloads and ownership; for lazy/promised/oversized or unpreservable items, return `ClipboardNotPreserved` **without mutation** in default mode. Exercise explicit loss-consent separately; it does not waive default refusal or demand perfect preservation of unsupported formats. Native AppKit/AX/CGEvent behavior must be observed, not inferred from Uno documentation.

On Windows use an isolated application identity and fixture profile. Compare relevant existing insertion and popup behavior through current production interfaces but never register the production hotkey/instance key or change HKCU startup. AOT artifacts are inspected for native executable architecture and the required SQLite library. No real UI acceptance or synthetic input occurs without user-authorized screen time and safe harness setup. Hardware or signing unavailability stays NOT RUN, not PASS.

### 4.2 Alternatives

| Option | Benefit | Reason | Verdict |
|---|---|---|---|
| Isolated Uno probe with actual Core/Presentation and OS adapters | Tests real package/native edges without changing users' install | Requires two host environments and guarded input testing | Chosen |
| Create production Uno head immediately | Early visible UI | Entangles architecture changes with unproven native/AOT gates | Rejected |
| Compile an Uno Hello World only | Cheapest | Cannot validate SQLite, actual controls, original-target behavior or safety | Rejected as sufficient evidence; useful T1 smoke step |
| Skia for both desktop OSes | Uniform rendering | Adds unnecessary Windows regression risk | Consider only after measured failure and user decision |

### 4.3 Probe data and configuration

Probe uses a unique app/bundle identity, per-run disposable profile and versioned synthetic fixtures (Unicode, CRLF/LF, empty/10k library). **All profiles, copied databases, private logs and recovery journals live outside the repository** in OS user-temp locations with owner-only permissions; never write plaintext clipboard recovery files. Never point at `%LOCALAPPDATA%/SmartSnippets` or an existing macOS user profile. Windows legacy data compatibility is tested with generated schema-version fixtures and optionally a **user-supplied copied database** in that private location. No schema mutation or real startup registration occurs in 018. Publish an evidence matrix with commands, machine/OS/runtime/Uno versions, artifact hashes/architectures, PASS/FAIL/BLOCKED/NOT RUN, cleanup and data/clipboard recovery status; redact fixture payloads and window titles. Do not commit signing credentials or private reports.

### 4.4 Hot-path sequence

```text
Synthetic target receives cursor → native shortcut callback (or menu command)
  → native target capture before activating Uno surface
    → PopupShowCoordinator.RequestShow → native two-window warmup/ready
      → popup VM selected ID → hide → shared insertion policy (if compatible)
        → Core store resolves text → OS checks captured target and permission
          → clipboard capability/snapshot; refuse if incomplete in default mode
            → focus original target → final identity/foreground check
              → native paste dispatch → explicit dispatched/uncertain/failure outcome
                → Core.RecordUsage ONLY after positive dispatch
              → restore clipboard only if still owned → content-free result
Parallel paths: main-row Insert, popup Enter/click and tray/menu shortcut require coverage.
Separate: explicit Copy changes clipboard deliberately, earns no usage; target-free preview cannot reach insertion.
```

### 4.5 Surface list

| Surface | Probe action | Task |
|---|---|---|
| Windows native / macOS AppKit Uno host | AOT publish/run, native dependencies, windowing | T0–T3, T6 |
| Main/editor/popup representative XAML | Compiled binding, theme, keyboard focus, show/hide/recreate | T3 |
| Hotkey/menu, popup warmup and preview | Target capture, ready focus, noninserting preview | T3–T5 |
| Native target, clipboard and usage | Exact target validation, denial/error paths, result/usage | T4–T5 |
| SQLite/local profile/logs | Copy fixture, migrate, reopen, failure, privacy | T2, T5–T6 |
| Early mobile risk check | Strict AOT management/keyboard packaging and optional 10k memory observation; not Phase 2 acceptance | T7 |
| Reports and release gate | Repro commands, constraints, go/no-go | T0, T6, T8 |

## 5. Impact and risk

| Question | Response |
|---|---|
| State/crash | Disposable profile and own synthetic target only. On interruption retain run-specific recovery journal; never assume target/profile can be deleted while a process lives. |
| Consistency | Single UI-thread connection; no concurrent production owner. Count usage only for confirmed dispatch, never for copy, preview or uncertain native result. |
| UX | Probe is non-production; no user-visible migration or settings changes. Simulate failure messages in the probe and record whether they are actionable. |
| Backward compatibility | Read/migrate copies of existing schema fixtures; leave current Windows application/profile untouched. |
| Error recovery | Reject unsafe target/clipboard/permission state before mutation. Preserve original clipboard if possible; on incomplete restore report failure and recovery instructions, no false PASS. |
| Logging | Record result enums, time, architecture and stage, not text/labels/tags/title/query/user document path; scan private logs, captured stdout/stderr and probe-owned OS diagnostics with synthetic leakage fixtures. |
| Edge cases | Lost/changed target, fast repeat, first-show warmup, double window, multi-display scaling, sleep, revoked permissions, modified clipboard, missing SQLite native library and AOT warning are negative tests. |

**Specific risks:** Mac not available, toolchain mismatch, ad-hoc-signed/Developer-ID-signed permission behavior difference, unsafe pasteboard format conversions, wrong-window insertion, Uno external popup limitations, Native AOT linkage or native SQLite packaging. Each unmet required **desktop** gate (FG1–FG5) is a blocker, not an acceptable partial pass. FG6 may record mobile risk as BLOCKED/NOT RUN and complete its decision artifact; any unresolved risk requires an owner decision before starting 019. Do not use an uncertain dispatch to prove exact-once insertion.

**Rollback:** No production files/settings/data changed; remove the probe only after stopping positively identified owned processes and confirming clipboard/profile cleanup. Uncommitted private test data is retained for recovery until operator signs off; no wildcard process kills or OS permission resets.

## 6. Tasks (ordered)

### T0 — Pin source/toolchain and executable gate (FG1, FG6)
- **Files:** `prototypes/uno-desktop/README.md`, `docs/research/012-uno-desktop-feasibility-results.md` (new on execution), scoped version/package props within the probe.
- **Change:** Resolve the exact stable Uno SDK/template and supported .NET SDK, WinAppSDK/Skia heads, AOT configuration and minimum toolchains from official docs and a fresh restore; record Windows and macOS build prerequisites and provenance. Verify conditional TFM restoration on macOS and actual platform source inclusion (whether `Platforms/Desktop` or explicit glob). Define PASS/FAIL/BLOCKED/NOT RUN and fresh artifact inspection. Define a single evidence validator: verify effective `PublishAot=true`, unsuppressed trim/AOT warnings (inspect `NoWarn`, `SuppressTrimAnalysisWarnings`, `TrimmerSingleWarn`), native executable architecture and absence of CoreCLR/Mono JIT payloads (including single-file JIT impostors). Avoid speculative package versions in the plan.
- **Verify:** `dotnet --info`, template/package resolution, `dotnet restore` in probe scope, documented resolved versions on both hosts; malformed or absent SDK blocks the correct host.
- **Test that dies if hop breaks:** a deterministic evidence validator rejects missing version/OS/RID/artifact fields or a non-AOT release artifact (`prototypes/uno-desktop/tests/VerifyEvidence*`, exact runner established here).

### T1 — Create isolated native desktop Uno hosts (FG1, FG3)
- **Files:** new `prototypes/uno-desktop/` single multi-targeted Uno SDK probe project with Windows/macOS conditional entry points, `prototypes/uno-desktop/README.md`; `SmartSnippets.slnx` stays unchanged.
- **Change:** Prove the proposed **single multi-targeted project** with unpackaged (`WindowsPackageType=None`) Windows-native WinAppSDK and Skia/AppKit macOS entry points sharing a minimal Uno view; independent app ID/profile and probe-only outputs. No production replacement. Verify host builders against template output. GUI launches require a verified non-activating auto-exit self-test or a separately authorized screen slot.
- **Verify:** Fresh Release Native AOT publish for `win-x64` and, on Apple Silicon macOS, `osx-arm64`; run binaries and confirm separate profile/identity; inspect executable format/arch and dependency manifests. Report Mac unavailability NOT RUN.
- **Test that dies if hop breaks:** smoke integration must launch each native artifact and observe a bounded content-free startup/exit receipt; an artifact-only file check is insufficient.

### T2 — Real SQLite and portable policy exercise (FG2)
- **Files:** probe host DataProbe and test fixtures under `prototypes/uno-desktop/`; new results doc.
- **Change:** Reference Core/Presentation without recompiling copied sources; open disposable schema-version fixtures with actual Migrator/SnippetStore, create Unicode/multiline snippet, reload/restart, query and export; test missing DB bootstraps a schema, and corrupt/newer-schema/denied DB fail safely. Verify UI-thread store access remains serialized; never log fixture content or write real profile.
- **Verify:** Both AOT binaries open fixture, mutate and reload with exact-content assertions in private test code; compare DB hashes/records before/after failure and confirm real Windows profile remains unchanged; confirm native `e_sqlite3` packaged.
- **Test that dies if hop breaks:** probe `DataRoundtrip`, `MissingDbBootstraps` and `DataFailureIsolated` must fail if migration/load is skipped, Unicode/newline changes or corrupt/newer schema is accepted; no mock store.

### T3 — Representative XAML/windowing and warmup (FG3)
- **Files:** probe views/control fixture, host window adapters, probe UI tests; results doc.
- **Change:** Exercise representative compiled XAML from main editor/popup, palette/text-size bindings, `PopupShowCoordinator` and recoverable two-window lifecycle. Test first show/hide/reopen, focus, external beside-window hover (**native overflow or a tested companion surface/NSPanel**), work-area placement and target-free preview with Enter/row selection. If neither external-hover route works, FG3 is BLOCKED; do not emulate success.
- **Verify:** Build both AOT heads and manually inspect both **under authorized, isolated screen slots**; assert probe receipts for warmup generation → ready → focus and preview with zero insertion/clipboard/usage. Existing `PopupShowCoordinator` unit tests still pass sequentially.
- **Test that dies if hop breaks:** `WindowLifecycleFlow` and `PreviewNeverDispatches` require native callbacks and a live two-window surface; preview test activates a row and presses Enter, asserting zero dispatch, usage and clipboard mutation.

### T4 — Prototype native desktop capabilities without production writes (FG4)
- **Files:** probe `Platforms/Windows/*`, `Platforms/MacOS/*` for shortcut/target/focus/pasteboard/event dispatch, test target fixture, results doc.
- **Change:** Use an isolated probe identity and owned synthetic target with **two editable windows in one PID**. Map macOS shortcut registration, target identity, AX/permissions, focus and event posting; compare Windows adapter. Test eager multi-item/multi-UTI payloads (text, RTF, HTML, image, URL), lazy/promised/oversized formats, history markers, change-count ownership and restoration. Return `ClipboardNotPreserved` without mutation when unsafe by default; test explicit loss consent separately. Identify permitted distribution/permission assumptions separately from local ad-hoc runs.
- **Verify:** Permission denied/revoked; lost/changed target **including window A → B in the same PID**; incomplete/busy clipboard; new owner before restore; canceled work and positive paste path are exercised with owned fixture. No OS-wide input or clipboard mutation outside authorized slot. Record native error classes without titles/payload.
- **Test that dies if hop breaks:** `NativeCapabilitiesDenyWithoutMutation` plus `OriginalTargetIdentity` fail on shortcut/target bypass and any clipboard mutation after a denied permission/unsafe snapshot.

### T5 — End-to-end synthetic insertion and parallel paths (FG4, FG5)
- **Files:** probe flow harness and synthetic target; results doc. Production App, Core and Presentation remain unchanged.
- **Change:** Connect real host callbacks to popup policy, real Core store and native dispatcher (adapter if needed). Exercise shortcut/menu, Enter/row/main-row activation (**capture target → open popup → open main → main-row Insert**), rapid duplicate, failure/caution and Copy. Assert Unicode/multiline output against documented per-platform newline transformation (unchanged shared service emits CRLF), exact **window** identity, supported clipboard restoration on normal success, no usage on failure/uncertain/copy/preview and once-only usage on confirmed dispatch. If safe default/consented clipboard behavior is unachievable, stop that path and report BLOCKED; do not force paste for a green demo.
- **Verify:** Controlled screen run with preflight, exclusive slot, isolated profile, owned target and watchdog; fixtures and counters are read from private artifacts, logs screened for leakage. Windows existing tests are run sequentially; wrong target and closed target never receive text.
- **Test that dies if hop breaks:** `ShortcutToTargetDispatch` (host wiring and separately authorized live shortcut), `MainRowToTargetDispatch`, `SamePidOtherWindowRejected`, `CopyVsInsertUsage` and `NoPasteOnUnsafeClipboard` fail if entry point disconnects, wrong-window gate is omitted or success is fabricated.

### T6 — Reproducible artifact/privacy/negative gate (FG1–FG6)
- **Files:** probe README, evidence validator and `docs/research/012-uno-desktop-feasibility-results.md`.
- **Change:** Document exact host-local publish/run commands, SDK versions, effective MSBuild properties, AOT warning inspection, binary/arch/dependency inspection, ad-hoc versus Developer ID signing limitations, DB isolated paths, failure matrix, cleanup receipts and synthetic content/label/tag/title/path leak scan of app and captured process/OS diagnostics. Include no private fixture payloads or usernames. Differentiate debug build, **ad-hoc-signed macOS AOT probe** and distributed Developer-ID-signed/notarized release; do not globally reset TCC permissions (scoped probe-bundle-ID cleanup requires operator consent).
- **Verify:** Run the validator **locally on each build host** against fresh Windows or Mac artifacts, then aggregate per-host signed-off manifests; check missing/non-native artifacts turn red and run tests sequentially. Confirm warning/property evidence is unsuppressed, not merely zero visible warnings. No `PASS` row for a host without hardware, screen authorization or verified artifact.
- **Test that dies if hop breaks:** `EvidenceGateRejectsNonAot`, `EvidenceGateRejectsSuppressedWarnings`, `EvidenceGateRejectsNotRunAsPass` and `PrivacyFixtureScan` with a planted synthetic leak control.

### T7 — Early mobile AOT/keyboard-host risk check (FG6)
- **Files:** `docs/research/012-uno-desktop-feasibility-results.md`; minimal isolated toolchain/extension fixture under `prototypes/uno-desktop/mobile-risk/` only if needed (no production app).
- **Change:** Before recommending 019, check official current .NET/Uno tooling and attempt minimal Native AOT app-plus-iOS-keyboard and app-plus-Android-IME builds/packaging on available hosts. If a required host/toolchain is absent, record the attempted command and precise limitation as NOT RUN rather than inventing support. Record exact runtime mode and executable architecture; distinguish CoreCLR Native AOT from Mono AOT and JIT, app artifact from extension/service artifact. Inspect extension memory/launch behavior against a 10k synthetic projection if a device is available; otherwise explicitly mark device/memory NOT RUN and seek a user decision before production Uno adoption. This does **not** complete 022.
- **Verify:** Actual build output and artifact inspection or precise toolchain error, not marketing documentation alone; no claim of mobile acceptance. Present any strict-AOT or keyboard-host blocker for owner decision before starting 019.
- **Test that dies if hop breaks:** `MobileRiskEvidenceRejectsUnverifiedArtifacts` rejects a `PASS` row based only on docs, a plain mobile AOT build, or a management-app artifact without its keyboard host.

### T8 — Wiring audit and go/no-go handoff (FG6)
- **Files:** `docs/research/012-uno-desktop-feasibility-results.md`, master 017 status table; optionally 019 only after 018 completion and fresh discovery.
- **Change:** Trace shortcut/menu → target capture → popup readiness → selection → dispatcher → usage/clipboard cleanup and separately preview/Copy; map each native host registration and lifecycle start/stop. Distinguish in-process callback wiring from separately authorized global shortcut input. Record observed versus unverified links, blockers, alternatives and recommendation for 019. Do not detail 019 until this plan is complete.
- **Verify:** Search for each new producer and its consumer, validate content-free runtime receipts and same synthetic target outcome; return BLOCKED if a desktop registration has no caller or a required desktop FG1–FG5 gate lacks a verified run. FG6 is complete when early mobile build/packaging attempts and NOT RUN device/memory limits are accurately recorded; a mobile blocker/unknown gates **019 approval**, not completion of this evidence report. Explicitly list residual release-only signing/compatibility risks.
- **Test that dies if hop breaks:** host integration `ShortcutToTargetDispatch` must be red when the shortcut callback is disconnected; `PreviewNeverDispatches` must be red when preview binds insertion.

**Standing instruction:** if a step cites an unsupported API, prescribes an unreachable/compile-failing assertion, or an assertion a wrong implementation can pass, stop and report the plan defect; do not weaken the test. For every new flow test, identify a wrong implementation that would fail and test through the actual host composition, not a hand-built mock graph.

## 7. Test strategy and manual runbook

- **Unit:** existing Core/Presentation tests; pure policy, evidence validator and adapter decision logic with permission/clipboard failure fakes. Run sequentially.
- **Integration:** both native hosts load real SQLite and run startup/restart; each desktop entry path reaches real popup/selection/store with only the OS boundary substituted in CI. Native input/focus/pasteboard acceptance requires real, authorized desktop testing; fake-only green cannot satisfy FG4.
- **Manual, authorized screen slot only:** operator confirms no active production Smart Snippets instance will be changed, owns synthetic target and confirms recovery. An unavailable Mac or screen slot blocks that run, not all non-screen research.

| # | Step | Expected |
|---|---|---|
| 1 | Build/publish probe from clean output on Windows and Apple Silicon macOS; inspect binary and SQLite dependency | Separate `win-x64` / `osx-arm64` native executables, zero IL warnings, probe identity not production |
| 2 | Launch both with disposable library, add Unicode/multiline fixture, close/reopen | Text and metadata exact; no production profile touched |
| 3 | Open/hide/reopen two windows, switch theme, open target-free preview | First layout ready before reveal, focus correct, preview has no clipboard/usage/insertion |
| 4 | On Mac with owned target, authorize required permission; invoke shortcut then insert | Original target receives exactly one payload; usage once, clipboard safely restored or explicitly blocked |
| 5 | Deny/revoke permission, close/change target, introduce clipboard contention/change and rapidly repeat | No redirected insertion or false usage; actionable result; no unsafe clipboard mutation |
| 6 | Inspect logs/evidence and clean up owned processes/profile/clipboard | Only content-free receipts; restoration verified or recovery reported and plan blocked |

**Run authorization:** do not run scripts that synthesize input, move a user profile, mutate startup/OS permissions or change the clipboard while the machine is in use. Screen approval is distinct from G2. Adapt the safety protocol from `docs/guides/002-acceptance-harness-safety.md` for macOS **before** running native tests; do not reuse Windows UIA/Notepad assumptions as a Mac harness.

## 8. Rollout

018 is a non-shipping experiment. Upon completion, update 017 with evidence, then detail 019 with fresh codebase discovery. Any AOT, safety or host compatibility blocker pauses all dependent subplans for an explicit user decision. Do not tag a successful prototype as Phase 1 release or overwrite existing CI gates.

## 9. Open questions / decision timing

No additional product-scope question blocks G2. Exact Uno package/toolchain pin, minimum OS floors and shortcut/permission mechanism are deliverables of T0/T4; signed release identity and compatibility matrix are finalized for 021. Mac hardware, screen slot and Apple signing credentials are external prerequisites; lack of them means NOT RUN/BLOCKED, not tacit approval of reduced scope. T7 surfaces early mobile strict-AOT/keyboard feasibility risks before 019; detailed iOS/Android design and memory budget acceptance belong to 022.

## 10. Approval log

| Date | Event | Notes |
|---|---|---|
| 2026-10-30 | Requirement brief confirmed (G1) | Apple Silicon only; recommended desktop/mobile/AOT scope |
| 2026-10-30 | Draft produced | No prototype run, app code edits or native test authorization |
| 2026-10-30 | Independent plan review (gemini-4-argon:medium) | Corrected contradictory DB test, privacy handling, same-PID window oracle, clipboard outcomes, AOT project/evidence gates and early mobile risk |
| 2026-10-30 | Plan approved (G2) | Prototype plan approved; live screen/input testing still requires a separate authorized slot |
| 2026-10-30 | Implementation started → blocked | T0/T1 single Uno skeleton compiles Windows XAML but clean AOT publish fails IL2059; no unsafe fallback or downstream migration |
| 2026-10-30 | Workaround and Mac access authorized | User approved continuation with opt-in Windows mode and SSH checkout; user unlocked Mac parent directory. Mac restore/C#/XAML compile and 432+599 managed tests pass; clean Native AOT fails IL2026/IL3050 in vendor WebView. No GUI/input authorization |
| 2026-10-30 | Mac AOT investigation reviewed | Unconditional vendor callback rooting traced; standalone source-generated JSON CLI publishes/runs natively with four synthetic checks. Review found no checkpoint blockers. Vendor correction/adoption remains a decision; Uno app gates still incomplete |

The owner subsequently requested “fix plan 018 until there is no block”, authorizing continued probe corrections. Selected configurations now clean-publish with Native AOT on both hosts: approved manual Windows packages and a pinned, source-verified macOS managed backend with two generated JSON contracts. A native bundle builder avoids the SDK Extras CoreCLR launcher. Separate headless native processes on both hosts pass actual Core migration/store/settings/JSON persistence and restart checks. See [016 — current evidence and remaining gates](../research/016-uno-native-aot-repair-and-data-probes.md) for exact hashes, coverage and limitations; [012](../research/012-uno-desktop-feasibility-results.md)–[015](../research/015-uno-macos-aot-defect-investigation.md) remain historical investigation records. This does not complete T1/FG1 GUI startup or all T2/FG2 checks. T3–T5 UI/native insertion, full privacy/recovery, T7 mobile risk and final audits remain incomplete. No production dependency decision, 019 start or screen/input test is authorized by these fixes.
