# 017 — Master plan: Uno cross-platform migration

**Date:** 2026-10-30
**Status:** Approved (2026-10-30)
**Size:** L — master plan and sequential subplans
**Area:** App/UI, platform services, Core/Presentation boundaries, tests, packaging and CI
**Requirement brief confirmed:** 2026-10-30 (G1, in chat)
**Baseline:** `develop` at `3e8d3cf`; planning branch `docs/uno-cross-platform-migration`
**Research:** [011 — Uno migration research](../research/011-uno-platform-migration-research.md)

## 1. Goal

Migrate Smart Snippets to Uno Platform, preserving the Windows desktop experience, adding a full Apple Silicon macOS desktop experience in Phase 1, and adding iOS/Android management apps with snippet keyboard integrations in Phase 2. Native integrations remain explicit platform code; sharing XAML is not proof of native behavior.

## 2. Requirement (confirmed at G1)

The confirmed Requirement Brief follows verbatim:

> **Goal:** Migrate Smart Snippets to Uno Platform while preserving Windows functionality and adding macOS, then mobile.
>
> **Size:** L — master plan with sequential, separately approved subplans.
>
> ### In scope
> - **Phase 1:** Windows x64 and **Apple Silicon macOS only**.
>   - Full desktop functionality: snippet/folder management, search, popup, global shortcut, insertion, tray/menu bar, startup, settings, import/export, and previews.
>   - Preserve existing Windows data and behavior.
>   - Implement native macOS integrations with clear permission handling.
> - **Phase 2:** iOS and Android management apps plus snippet keyboard extensions.
>   - Platform-appropriate mobile UI and insertion behavior.
>   - Explicitly disclose keyboard and secure-field restrictions.
> - Reuse Core and Presentation where practical; adapt platform-dependent contracts.
> - Retain **Native AOT as a release requirement**, validated early per target.
> - Local SQLite storage, with import/export for manual library transfer.
>
> ### Non-goals
> Intel Macs, Windows ARM64, Linux, web, cloud sync, accounts, and implementation of the draft password-snippet feature.
>
> ### Behavior and constraints
> - Desktop insertion retains target validation, duplicate prevention, clipboard-safety handling, and successful-dispatch usage accounting.
> - Unsupported capabilities or denied permissions produce actionable feedback, never silent fallback to another target.
> - Mobile uses approved keyboard-extension workflows—not desktop-style global input injection.
> - macOS builds and validation require Apple Silicon hardware; distribution requires signing/notarization.
> - Technical prototypes must verify Uno/AOT, SQLite, windowing, and native insertion before broad migration. Blockers require your decision, not silent requirement changes.
>
> ### Acceptance criteria
> 1. Existing Windows functionality and saved data survive migration.
> 2. Apple Silicon macOS passes an agreed desktop feature and target-application compatibility matrix.
> 3. Both desktop platforms pass Native AOT publishing and platform-specific safety tests.
> 4. iOS and Android support management and insertion through their keyboard integrations, with documented restrictions.
> 5. Import/export, persistence, permissions, failure handling, and usage accounting pass platform-specific tests.
> 6. No network service is required; logs remain free of snippet content and window titles.
>
> **Plan approach:** Create a master plan and reserve subplan files, detailing only the first dependency-ready subplan. Implementation starts only after plan approval and a subsequent implementation instruction.

These six criteria are referenced as AC1–AC6 below. The user chose the recommended options except CPU coverage: Apple Silicon only, no Intel Macs. Users are local desktop/mobile users; changed surfaces are listed in §4.5. There are no additional product features implicit in this plan.

## 3. Current state (as-built)

References are to the baseline, not promises of Uno compatibility.

- `src/SmartSnippets.App/SmartSnippets.App.csproj:1–33` is a WinUI executable targeting .NET 10 Windows, unpackaged, self-contained `win-x64`, with Native AOT, WindowsAppSDK and CsWin32. `Directory.Packages.props:1–15` has no Uno package.
- `src/SmartSnippets.Core/SmartSnippets.Core.csproj:1–9` and `src/SmartSnippets.Presentation/SmartSnippets.Presentation.csproj:1–12` are .NET 10 libraries; persistence/query and view-models already sit below the native App.
- `src/SmartSnippets.App/AppHost.cs:98–130,137–253` loads the SQLite store/settings, constructs Win32 services, then wires pages, popup and tray. This is explicit construction, not a Generic Host. Do not add a second DI framework solely for migration.
- Hotkey and tray entry points capture a foreground target before showing (`AppHost.cs:322–335`). Main-window Insert delegates to the popup view-model (`:183–187`). Popup keyboard, row activation and main-row insertion converge at `PopupViewModel.cs:247–254,317–342`; explicit Copy is separate (`:238–242,374` and `SnippetsPageViewModel.cs:142–148`). All these consumers must be audited when the insertion contract changes.
- `Presentation/Services/InsertionContracts.cs:3–4,24–29` embeds native handles and CRLF semantics. `InsertionService.cs:18–40,86–126` performs single-flight, line-ending normalization, final foreground validation, dispatch and success-only usage updates. Preserve the safety policy, not Windows-specific representations.
- `Core/Settings/HotkeyGesture.cs:5–24,33–47` persists Win32 modifiers and virtual keys and displays “Win”. `Core/AppPaths.cs:5–19` defaults to LocalApplicationData but accepts an explicit root. These are migration seams despite the portable target framework.
- `Presentation/Services/ISnippetFileService.cs:3–19` assumes path-based pickers and atomic same-directory writes. JSON processing can be reused; mobile document-provider access cannot be assumed to produce ordinary paths.
- `App/Program.cs:13–47,50–94` implements Windows AppLifecycle single-instance/redirect and administrator-restart handoff. `AppHost.cs:196–235,258–277` connects startup/elevation/settings and initial/redirected launch requests.
- `.github/workflows/ci.yml:1–61` tests Core and Presentation sequentially and publishes Windows AOT; it does not validate macOS or mobile. Existing plans' status tables are historical, not evidence that all manual acceptance has passed.
- `docs/plan/016-password-type-snippets.md:1–25` is Draft and proposes Windows account-bound DPAPI. Password functionality is not implemented by this migration.

## 4. Shared design

### 4.1 Approach and boundaries

1. **Prove first, migrate second.** Plan 018 adds an isolated desktop probe, not a production replacement. It must exercise real Uno/AOT/SQLite, representative compiled XAML, two-window lifecycle, macOS target/paste and Windows adapter compatibility. Unknown support is a gate, not a reason to downgrade AOT or clipboard safety.
2. **Retain layering:** platform/UI → Presentation → Core. Reuse domain, SQL migrations, validation, JSON parsing, grouping/search, view-models and pure popup/hover policies. Keep store/cache access on the owning UI thread. Keyboard processes must not share that in-memory store or its connection.
3. **Target shape:** propose one multi-targeted Uno application project at `src/SmartSnippets.Uno/`, with an **unpackaged** Windows-native WinAppSDK target (`WindowsPackageType=None`, subject to template/AOT validation) and a Skia Desktop/AppKit target published only as `osx-arm64`. The 018 probe must actually test this shared project shape, not only separate single-target hosts. Platform services live under explicit Windows/macOS platform folders. Plan 018 confirms the exact template/TFMs/package versions before 019 specifies files and contracts. Do not choose Skia on Windows by default: retaining native WinUI reduces regression risk.
4. **Controlled coexistence:** retain `SmartSnippets.App` as the Windows reference executable until 021 accepts replacement. The probe uses an isolated identity/profile. The new production head must not concurrently own the legacy Windows profile or shortcut. Existing Windows application identity, install/update path, executable naming, startup migration and profile ownership are cutover acceptance items, not accidental template defaults.
5. **Capability-aware services:** separate shared safety/outcome policy from platform target identity, shortcut encoding/display, line-ending dispatch policy, clipboard representation, lifecycle and document access. Windows behavior and saved values remain compatible; macOS settings are native to that platform. Migrate persisted settings only with roundtrip tests. No schema change is required merely to adopt Uno.
6. **Mobile is a new interaction model:** management UI is shared/adaptive; iOS extension and Android IME are native entry points and may require small native UI surfaces. Do not promise that shared Uno XAML embeds in either keyboard host. Plan 022 must prove AOT, extension/service packaging and local data exchange before 023/024 designs are detailed. Copy alone does not count as successful insertion. Keyboard dispatch accounting must be idempotent and reconciled safely with the management store.
7. **Data/privacy:** retain Windows database location and data. New platforms use appropriate app-private storage. Library transfer uses the existing versioned JSON format, not raw cross-OS SQLite/profile copying. No cloud service, telemetry or account dependency. Content, labels, tags, window titles, query strings and user document paths do not go into logs. Existing `Migrator` and `AppLog.Error` can emit raw exception paths internally, beyond a probe caller's control. Probe scans must catch synthetic content/label/tag/title/query/user-document-path fixtures and redact OS temp/username roots **before publishing evidence**; keep raw diagnostic artifacts private. Plan 019 must audit and sanitize production `Core`/Presentation logging, including `Migrator` and snippet-page failures. Device-local keyboard data sharing is necessary, not device-to-device synchronization.
8. **Release policy:** Native AOT is mandatory per release artifact; JIT/interpreter/ordinary Mono AOT is not an unannounced substitute. Exact mobile toolchain interpretation and dependencies must be demonstrated in 022. Zero build warnings and no suppressed trim/AOT defects. macOS Phase 1 proposes direct signed/notarized distribution, not a Mac App Store dependency; if required permissions/distribution cannot coexist, stop for an owner decision.

### 4.2 Alternatives considered

| Option | Benefit | Cost / reason not chosen | Verdict |
|---|---|---|---|
| Shared Uno UI, native Windows and macOS adapters | Reuses WinUI investment and preserves OS integrations | Requires explicit API compatibility and native lifecycle testing | Chosen, subject to 018 evidence |
| Add a macOS-only companion and leave Windows outside Uno permanently | Smallest immediate Windows change | Permanent separate product UI; does not complete the confirmed project migration | Baseline during transition only |
| Skia Desktop on both Windows and macOS | One desktop renderer | Adds Windows rendering/windowing changes without a confirmed need | Not the default; reconsider only on measured blocker |
| Management/copy-only ports | Simpler permissions and packaging | Fails desktop parity and confirmed mobile keyboard scope | Rejected |
| Rewrite Core/Presentation or change database | Clean slate | Unnecessary data and behavior risk | Rejected |

### 4.3 Contracts and compatibility constraints

Detailed API signatures belong to 019, informed by 018. Required invariants:

- Captured desktop target identities are opaque to shared view-models and revalidated immediately before dispatch; no target substitution, including another window in the same PID. The macOS feasibility fixture must include two editable windows in one target process.
- Windows retains its current clipboard/history policy, CRLF dispatch and elevation handling. macOS must establish safety **by outcome**, not claim that a clipboard string copy is a full snapshot: classify preservable eager multi-item/type payloads; refuse mutation for unpreservable/lazy/oversized representations by default (`ClipboardNotPreserved`), allow the existing explicit consent path only with truthful disclosure, and restore only if clipboard ownership is still ours. Investigate history/Universal Clipboard markers; do not claim unsupported equivalence. macOS newline and `⌘V` behavior require observed fixture expectations, not silent reuse of CRLF.
- Denied/revoked permissions, lost target, contention, cancellation, uncertain dispatch and shutdown have explicit outcomes. Uncertain dispatch does not trigger automatic retry or usage credit.
- Reuse popup readiness, pending-show generations, recovery and hover placement policy; native callbacks must feed those policies rather than bypass focus gates.
- Import parsing/transactions and export serialization stay shared. Document-provider publication semantics need explicit failure/cancellation guarantees before mobile acceptance; desktop atomic replacement is not silently weakened.
- Native keyboard boundaries may access a versioned local projection/receipt mechanism, but exact schema, shared container, locking and recovery are deferred until the 022 prototype. One extension process must never reuse a UI-owned SQLite connection from another process.

### 4.4 New hot paths and parallel paths

```mermaid
sequenceDiagram
    participant User
    participant Native as Platform shortcut/menu
    participant Target as Platform target capture
    participant Popup as Uno popup + shared coordinator/VM
    participant Insert as Shared insertion policy
    participant OS as Platform focus/clipboard/dispatch
    participant Store as UI-owned SQLite store
    User->>Native: Open popup
    Native->>Target: Capture original target before UI focus
    Target->>Popup: Target identity + monitor context
    Popup->>Popup: Flush dirty projection; readiness/focus gate
    User->>Popup: Enter / row activation
    Popup->>Insert: Hide; insert selected ID once
    Insert->>Store: Resolve original text
    Insert->>OS: Validate target + permissions; snapshot clipboard
    alt Valid and safely dispatchable
        Insert->>OS: Restore target; final gate; dispatch paste
        OS-->>Insert: Dispatched / uncertain / failed
        Insert->>Store: Record once only when dispatched
        Insert->>OS: Restore clipboard if still owned
    else Not safe
        Insert-->>Popup: Actionable failure; no redirected paste
    end
```

Parallel paths: main-row Insert and popup keyboard/mouse/context-menu Insert must converge on this policy. Explicit Copy (main, popup, editor failure recovery) remains separate and earns no insertion usage. Target-free previews must never use clipboard/insertion. Mobile keyboard dispatch is a separate path: OS keyboard host → native extension/IME → locally shared snippet projection → native text-input proxy/InputConnection → dispatch receipt → management-store reconciliation. Its lifecycle/receipt design and sequence diagram are required in 024, not assumed to use desktop focus/clipboard policy.

### 4.5 Consumer surface inventory

| Surface | Required adaptation / verification | Owner |
|---|---|---|
| Main Snippets, editor, inline folder, search/sort, Copy/Insert | Compiled XAML, dialogs, unsaved state, capability-driven actions | 019, 020, 023 |
| Folders and settings | Native shortcut encoding, validation, display and default; native startup/background detection, theme/palette/text size, capability-gated Windows-only elevation and restart UI; platform-specific Ctrl/⌘ hints | 019, 020, 023 |
| Popup toolbar/list/editor, context menus, keyboard/mouse, hover and keyboard previews | Readiness/recovery, target retention, work-area placement, external preview lifecycle | 019, 020, 021 |
| Tray/menu-bar icon, notices, close-to-background, Exit | Native lifecycle callbacks and cleanup | 020, 021 |
| Initial/redirected launches, background startup, QA preview options | Single ownership, non-inserting preview behavior and foreground preservation; `--preview-popup-sort-menu` and content-free `menu_surface` receipt | 019–021 |
| Clipboard, focus, native target validation, usage | Native adapters behind shared safety policy, explicit Copy versus Insert | 019–022, 024 |
| Import/export and database/settings | Existing Windows profile; sandbox document descriptors/URIs (not raw path-only checks), truthful non-atomic provider failure text; local transfer | 019, 021–023 |
| Mobile management and native keyboards | Adaptive/touch UI, enablement, restrictions, local data exchange | 022–025 |
| Build/install/update/uninstall/CI/runbooks | Per-target AOT, architecture/signatures/dependencies, rollback | 018, 021, 022, 025 |

## 5. Impact, risks and rollback

| Question | Answer |
|---|---|
| State/crash survival | Keep transactional SQLite, unsaved-edit guards and UI-owned caches. Prototype profiles are disposable; clipboard/permission changes need explicit recovery. Keyboard process termination is normal and must not corrupt shared data. |
| Consistency/races | Never run two desktop owners of the same profile. Preserve single-flight insertion and usage rules; cross-process mobile receipts require deduplication and atomic publication. |
| User experience | Desktop parity includes keyboard-only use and previews, not only CRUD. Mobile has an explicitly different entry point; unsupported desktop settings are explained/absent, not fake-success toggles. |
| Backward compatibility | Preserve Windows profile, keys, identity and startup registration through cutover. Make an explicit pre-cutover backup even if no schema upgrade runs (Migrator's `.bak` is conditional); JSON transfer is not an in-place upgrade test. |
| Recovery | Permission denial and unsafe clipboard refuse insertion. Failed migration retains old data. A failed technical gate blocks its downstream plans. |
| Diagnostics | Allowlisted content-free receipts distinguish startup, popup readiness, native capability, dispatch outcome and cleanup. No snippet content, labels, tags, titles, user document paths or query strings in published logs. Probe-local raw exception logs stay private and are redacted before evidence publication; 019 sanitizes production sources. |
| Edge cases | Empty/10k libraries, Unicode/multiline, rapid activation, modifiers held, closed/moved target, multiple monitors/DPI, OS sleep/resume, clipboard ownership change, permission revocation, app/keyboard termination and cancelled document access require tests. |

**Key risks:** Uno API/AOT coverage; macOS exact-target focus and paste safety; ad-hoc-signed versus Developer-ID-signed app permission identity; clipboard representations that cannot be safely preserved; mobile Native AOT keyboard-host compatibility; provider writes without desktop atomic replacement; external hover/windowing differences. These remain unproven until target-device evidence exists.

**Rollback:** keep the legacy Windows release and an untouched pre-cutover profile backup until 021 passes. Probe and candidate binaries use separate output directories and profiles. Do not roll an old executable onto a newer schema without a validated downgrade path; restore the pre-upgrade backup instead. macOS first release has no existing production profile to migrate. Mobile rollout must preserve the management library if keyboard enablement or an extension update fails. No rollback removes unrelated startup/login items or resets OS privacy settings globally.

## 6. Subplans and dependency order

All files are reserved now. Only 018 is detailed. Master approval approves decomposition, not later placeholder contents.

| Plan | Objective | Dependencies | Status |
|---|---|---|---|
| [018 — Desktop technical feasibility](018-uno-desktop-technical-feasibility.md) | Prove chosen Uno heads, AOT/SQLite/XAML/windowing and native desktop insertion; early mobile strict-AOT/keyboard-host risk check before 019 | 017 approval | In progress — [native builds, headless data and basic GUI startup pass](../research/016-uno-native-aot-repair-and-data-probes.md); full UI, native insertion, remaining data/privacy checks and early mobile risk incomplete |
| [019 — Shared shell and platform contracts](019-uno-shared-shell-and-platform-contracts.md) | Shared unpackaged Uno production shell, Windows adapters, compatible platform-neutral boundaries, early AOT CI and data ownership | 018 complete and owner decision on any mobile risk blocker | Placeholder |
| [020 — macOS desktop integration](020-uno-macos-desktop-integration.md) | Complete Apple Silicon desktop parity and native integrations | 019 complete | Placeholder |
| [021 — Desktop acceptance and cutover](021-uno-desktop-acceptance-and-cutover.md) | Windows/macOS validation, signing, CI, migration/rollback and Phase 1 release gate | 020 complete | Placeholder |
| [022 — Mobile technical feasibility](022-uno-mobile-technical-feasibility.md) | Prove iOS/Android Native AOT, keyboard hosting, local sharing and document access | 021 complete; early mobile AOT/keyboard-host risk check occurs in 018 before 019 approval | Placeholder |
| [023 — Mobile management](023-uno-mobile-management.md) | Adaptive management, persistence, settings and document-provider import/export | 022 complete | Placeholder |
| [024 — Mobile keyboard integration](024-uno-mobile-keyboard-integration.md) | Native iOS keyboard/Android IME, local data exchange and idempotent usage | 023 complete | Placeholder |
| [025 — Mobile acceptance and final wiring audit](025-uno-mobile-acceptance-and-wiring-audit.md) | Mobile release gate and cross-phase wiring/safety audit | 024 complete | Placeholder |

Execution is deliberately sequential: complete and record a subplan's evidence → refresh discovery → detail the next placeholder → obtain approval → wait for a later implementation instruction. A *bounded early mobile feasibility check* in 018 does not implement or approve 022; if iOS/Android strict AOT or keyboard packaging is blocked, seek an owner decision **before** 019 adopts Uno as the production foundation. Do not detail future subplans while the current one is incomplete. Do not claim Phase 2 complete when Phase 1 ships.

## 7. Acceptance coverage and cross-plan gates

| Criterion | Main owners | Cross-plan check |
|---|---|---|
| AC1 Windows parity/data | 018, 019, 021 | Existing database/settings fixtures and UI flows run through the new head; legacy comparison |
| AC2 macOS parity | 018, 020, 021 | Original-target insertion, all desktop surfaces and agreed target-app matrix on Apple Silicon |
| AC3 desktop AOT/safety | 018, 021 | Fresh native artifacts, architecture verification, no trim/AOT warnings, native dependencies shipped |
| AC4 mobile management/keyboards | 022–025 | Real installed extension/IME → target editor → local management reconciliation |
| AC5 portability/errors/usage | 018–025 | Shared contracts + native adapters + both UI entry paths, not mocks alone |
| AC6 offline/privacy | 018–025 | Network-free functional runs and synthetic fixture leakage scan across app/extension logs |

Cross-plan acceptance includes real composition-root integration tests (no mandatory DI container), target-free preview tests, native lifecycle start/stop receipts, and platform-device tests. Test projects run sequentially to avoid shared Core build collisions.

## 8. Test and rollout strategy

- Preserve current Core/Presentation suites and add contract/flow tests at the production composition boundaries as they change. Fakes replace only external OS boundaries; SQLite, view-models, dispatch accounting and cache invalidation remain real where the flow exercises them.
- One flow integration test per management, import/export, popup/editor/insertion, permission failure and keyboard dispatch/reconciliation path. Unit tests alone do not establish native focus, clipboard or keyboard-host safety.
- Phase 1 release requires Windows target apps from the existing baseline plus an owner-approved macOS editor/browser/office/terminal matrix. Phase 2 requires physical iOS/Android devices, keyboard opt-out/secure fields, denied sharing/permissions and process termination coverage.
- Existing performance targets remain: popup ≤300 ms and 10k-snippet search ≤150 ms on recorded, agreed reference hardware. NativeAOT release artifacts, not debug runs, supply acceptance evidence.
- Screen/input synthesis requires explicit authorization for that machine and time slot. Isolated identity/profile, owned synthetic targets, bounded watchdog and recoverable clipboard/startup mutations are prerequisites. Plan approval is not screen-slot authorization. See [harness safety](../guides/002-acceptance-harness-safety.md).
- 019 introduces a provisional Windows and macOS Uno AOT build/test gate (on feature branch/PR or manual invocation); do not wait for 021 cutover to detect compile failures. 021 includes an explicit pre-cutover Windows profile backup independent of schema migration, plus the desktop wiring audit; 025 ends with the final cross-phase wiring audit. Each traces producer → registration/composition → caller → UI binding/native callback → persisted side effect, including startup/shutdown and the parallel paths in §4.4.
- Unexecuted native/manual checks remain **NOT RUN/BLOCKED**, never PASS. Signing and unavailable hardware/toolchains are blockers, not waived requirements.

## 9. Deferred decisions and stop conditions

No unresolved product-scope question blocks drafting 018. Technical choices are deliberately deferred to measured gates:

| Decision | Owner / deadline | Why deferred |
|---|---|---|
| Exact stable Uno SDK/packages, compatible .NET SDK and minimum OS floors | Implementer proposes in 018 T0; user approves any baseline reduction/increase before adoption | Framework docs are not proof against this project |
| macOS shortcut default, permission API set, safe target identity and clipboard handling | 018 evidence → 019/020 approval | Must test ad-hoc-signed probe and release-signed identity separately; no blanket Input Monitoring assumption |
| macOS target-app/reference hardware matrix and distribution credentials | User + implementer before 021 acceptance | Requires owned hardware/apps and signing access; direct notarized distribution is proposed |
| Mobile OS floors, CPU/RID coverage, release channel and keyboard host technology | User + implementer in 022, before 023 approval | Must demonstrate strict Native AOT and native extension/service compatibility |
| Mobile shared storage/usage protocol, Full Access needs, keyboard memory/packaging budget and provider publication guarantees | 018 early risk check, 022 findings → 023/024 approval | Not safe to prescribe from desktop contracts |

If the tree contradicts cited constraints, or a task prescribes an unreachable test or unsupported behavior, stop and report the plan defect. No silent AOT downgrade, scope reduction, permission expansion, unsafe clipboard fallback or out-of-scope synchronization.

## 10. Approval log

| Date | Event | Notes |
|---|---|---|
| 2026-10-30 | Requirement brief confirmed (G1) | Recommended choices accepted except macOS is Apple Silicon only; final brief explicitly confirmed |
| 2026-10-30 | Draft / self-review | Coverage, dependencies, surface inventory and gate boundaries checked; no implementation evidence claimed |
| 2026-10-30 | Independent plan review (gemini-4-argon:medium) | Incorporated blocker corrections; technical assertions still require prototype evidence |
| 2026-10-30 | Plan approved (G2) | Master decomposition and detailed 018 approved; placeholders 019–025 remain unapproved |

G2 approved; user authorized 018 implementation and subsequently requested continued blocker fixes. Selected Windows/macOS configurations now clean-publish Native AOT and pass headless native data/restart tests; see [016 current evidence](../research/016-uno-native-aot-repair-and-data-probes.md). Plan 018 is not complete: GUI/windowing, native insertion, remaining data/privacy gates and early mobile risk still require implementation/evidence. Dependent plans remain placeholders, not implementation-ready. The owner subsequently authorized an exclusive desktop test slot; a bounded basic GUI smoke passed on both hosts without input or clipboard changes. This is not full UI/insertion acceptance or permission for later unattended sessions.
