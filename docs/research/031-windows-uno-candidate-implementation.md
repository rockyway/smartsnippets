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
with confirming review pending.

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
binary reads, and CLI privacy/wrapped-property negative controls are exercised. The candidate artifact build and independent review of this new gate
are pending. Successful output is labelled BUILD_CHECKS_ONLY, never runtime PASS.
CI compiler logs are not uploaded because they can contain host paths.

## Remaining gates

Candidate build/artifact, actual composition tests, integration/compatibility review
and candidate UI/manual runs are pending. Legacy fresh AOT regression passed as
noted above; runtime legacy compatibility remains separate. Windows
post-publication clipboard restoration, stopped-worker recovery, physical shortcut,
positive paste/usage, native pointer/foreground and fault/privacy evidence remain
BLOCKED/NOT RUN until independently reviewed prerequisites genuinely pass. The
existing prototype native-entry hard gate stays closed. No clipboard write, key
posting, pointer movement, registry change or Mac connection was performed here.

019 cannot be marked complete from a closed-gate shell, policy tests or native build
alone. Oldest-runtime support and production cutover remain separate 021 gates.
