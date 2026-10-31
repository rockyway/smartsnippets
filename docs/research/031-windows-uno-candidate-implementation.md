# 031 — Windows Uno candidate implementation (Plan 019)

**Status:** In progress — no candidate native acceptance claimed.
**Plan:** [019](../plan/019-uno-shared-shell-and-platform-contracts.md)
**Approved planning baseline:** `8804627`.

The user approved the Windows-first plan and separately instructed implementation.
Mac testing and mobile feasibility decisions are deferred for the isolated candidate;
018 remains incomplete. Neither approval relaxes clipboard/input guards, adopts a
Mac vendor fork, or authorizes production cutover.

## Work boundaries

- Shared lane: reviewed privacy corrections and additive clipboard availability/result
  semantics, real VM consumers and legacy source compatibility.
- Candidate lane: separate Windows project/profile/identity, real host composition,
  management/popup/lifecycle, default-denied native boundaries and headless tests.
- Parent: additive build-only CI/publish gate, integration, reviews and verification.
- Legacy profile, production startup/hotkey registrations and current release workflow
  must remain unchanged. Shared source changes are limited to the approved plan.

## Baseline before integration

Sequential local Windows runs on the baseline shared source:

| Suite | Result |
|---|---|
| Core Release | 432 passed |
| Presentation Release | 599 passed |
| Desktop probe Python | 210 run, 1 skipped, no failures |
| Private station Python policy tests | 30 passed |

These are baseline regressions, not evidence for new candidate behavior.

## Shared implementation checkpoint

Shared implementation commit `a5c899e` (integrated from isolated lane `c9aa19a`):
additive clipboard availability/result defaults, three real VM Copy consumers,
legacy flush-result reporting, error-message/detail sanitization and safe migration
backup diagnostics. Initial independent review found diagnostic field loss, build
property masking and misplaced compatibility tests; corrections are implemented,
with confirming review `74a2e921-9c3d-4583-8a07-9e3c65436270`
(Argon high) finding no remaining issues in the shared/build-only checkpoint.

Sequential root verification: Core **435**, Presentation **608**, actual legacy
clipboard source with substituted OS boundary **6**, Release budget filter **4**
passed. Unchanged legacy publish script passed a fresh Native AOT compilation with
no reported warnings and native x64 entry/SQLite headers checked. Artifact SHA256:
`0a145ac1cfdacee0c49caafce268460620646e6a02b8b0066eb67f7b6c519b4b`.
Review corrections additionally passed **436 Core** and **615 Presentation** tests;
the six actual legacy clipboard-source tests now run inside the existing
Presentation test project/legacy CI, with failure-log privacy assertions. Copy
retry clears only its own stale error, retaining dirty text. This executable was
**not launched**; build evidence does not prove clipboard or UI
runtime compatibility. The live legacy profile was not opened.

## Build-only gate under development

`scripts/publish-uno-candidate-aot.ps1` selects the candidate working directory and
SDK, performs matching locked Release/win-x64 restore/publish, captures effective
properties into private owner-restricted temp evidence, cleans before compiling and
uses a fresh unique output. It never runs an executable or accesses an app profile.
`scripts/uno_candidate_gate.py` rejects suppressed diagnostics, missing native
compiler/exit receipts, wrong-architecture/managed/JIT entry images, stale entry,
missing or malformed SQLite and managed/legacy/JIT payload layouts.

After review corrections, deterministic negative controls: **19** Python tests
passed. PowerShell parser check passed. Authored candidate properties are inspected
without CLI AOT/trim/NoWarn/RID overrides. Core/Presentation authored and actual
strict-build contexts are captured separately; no global lock properties may leak
to references. Root libraries keep their existing build policy while candidate
publication enforces warnings-as-errors. Link/junction payloads are refused before
binary reads, and CLI privacy/wrapped-property negative controls are exercised. Independent review of the gate source cleared after the corrections above;
runtime acceptance was not reviewed as passed. Fresh integrated artifact checks
now pass as recorded below. Successful output is labelled BUILD_CHECKS_ONLY, never runtime PASS.
CI compiler logs are not uploaded because they can contain host paths.

## Integrated Windows candidate checkpoint

Candidate-only commit `1fcb339` (from isolated lane `c7bd271`) adds the Windows
head, real host composition, copied/ported management and popup views, isolated
identity/profile ownership, closed native gate, and 37 headless candidate tests.
The first worker hit its 30-minute limit; its partial state was inspected and
preserved. One bounded same-approach retry completed and committed the checkpoint.

Parent verification on the integrated source, from the pinned candidate cwd:

- SDK 10.0.403; candidate tests **37 passed**.
- Core **436**, Presentation **615**, Release budget filter **4** passed sequentially.
- Build-gate Python negative controls **19 passed**.
- Fresh clean locked restore + native AOT publish + authored/reference properties
  and binary/layout/freshness gate: **BUILD_CHECKS_ONLY**, no errors.
- Build token `57cb900f8d9f45f883123a2d3d4ae1b5`.
- Entry SHA256 `b2f4ee14dc20bbc3a92adb37d6e748db77f113bb1b0c541ca0d57f83f3a9c719`.
- Artifact under `src/SmartSnippets.Uno/bin/publish-uno-candidate/<build-token>/`;
  private compiler/property evidence retained in host temp, not uploaded.

This parent artifact supersedes the worker's earlier hashes for integrated-build
verification. It was not launched. The candidate has **no authorized native adapter
or unlock mechanism**: native insertion, Copy and shortcuts always refuse. Tests
exercise actual host/store/VM code with external surfaces substituted; static XAML
checks supplement rather than replace native callback delivery. Hosted CI has not
run. Initial candidate source reviews identified hidden dialog hosts, preview-editor
recovery/placement, disconnected hints/availability bindings, disabled buttons being
re-enabled by Commands, fixed tray GUID/path binding, profile overlap/lock ordering
and message cleanup defects. Fixes were folded into `c6e0245` (parent ownership/tray/
message batch) and `428e76a` (isolated UI batch), plus the relocated-coordinator audit.
No native gate was opened.

Profile fixtures exercise owned production ancestors/junction aliases, locking
before data enumeration, failed validation lease release and hard-linked lease
refusal. Popup tests compile the same lifecycle policy consumed by native handlers,
including stale readiness and retained preview editor/query across hide/recreate/
exit-cancel. GUI static checks remain supplemental, not native acceptance.

Integrated post-fix verification: **51 candidate tests passed**, and another fresh
strict AOT/property/header/dependency/freshness gate passed BUILD_CHECKS_ONLY:

- Build token `c9f7030491d04f5297df3056cf2ee3a2`.
- Entry SHA256 `8dd05016bcb4ee938bc3e032a00c257315fba63275def5b8cf0b7c2c8a3bbd60`.

This is the latest parent candidate build; the earlier token/hash above is historical.
Confirming independent candidate review remains pending.

## Remaining gates

Candidate integration/compatibility reviews and UI/manual runs are pending. Legacy
fresh AOT regression passed again on integrated source after review corrections:
entry SHA256 `60f69179289441a1dbb31e890bf27bff0067f32d7239667c677fa5b699f6366e`
(the unchanged legacy publish gate, native compilation with no reported warnings).
Runtime legacy compatibility remains separate. Windows
post-publication clipboard restoration, stopped-worker recovery, physical shortcut,
positive paste/usage, native pointer/foreground and fault/privacy evidence remain
BLOCKED/NOT RUN until independently reviewed prerequisites genuinely pass. The
existing prototype native-entry hard gate stays closed. No clipboard write, key
posting, pointer movement, registry change or Mac connection was performed here.

019 cannot be marked complete from a closed-gate shell, policy tests or native build
alone. Oldest-runtime support and production cutover remain separate 021 gates.
