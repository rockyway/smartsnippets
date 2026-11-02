# 058 — nr-15 N3 shell-scene run

Authorized brief `native-run16-brief.md`; source `8d69d9dfc0dd3bfb822d6a4dd7ef536aa43acc3a`, branch `feature/uno019-n3-run`, 2026-11-01. **Phase 0 PASS; six required scene attempts FAIL; coexistence BLOCKED with its expected pre-launch refusal.** No retry. No N3 closure, production parity, physical input/tray proof or Plan 019 completion is claimed.

## Authority, preparation and screen boundary

Read local AGENTS, safety guide 002, progress handoff 006, approved Plan 019/T7, both management-fixture READMEs, research 032/033/056 and nr-14 helpers/report. Preserved the operator's uncommitted AGENTS/safety-guide edits. No delegation, production code change, legacy launch, download, clock change, input synthesis, clipboard content access or clipboard mutation occurred. Retained default candidate profile was not opened. All application profiles were newly owned throwaway fixture profiles; private evidence remains retained.

Phase 0 completed **20:59:27 local**, without desktop access or application launch. `PHASE0-READY nr-15` was written to `native-run16-report.md`. Filesystem-only grant polling ran every 30 seconds. One foreground polling tool call reached its 180-second tool timeout while waiting; no acceptance process existed. A filesystem-only background waiter continued polling; no scene was retried or started by either waiter.

The grant file admitted only bottom-left DISPLAY29, exclusive full desktop:

```text
GRANTED nr-15
start: 2026-11-01 21:15:31 local
hard end: 2026-11-01 21:40:31 local
```

Scope: the six fixed N3 selectors, then coexistence refusal only. Exclusions: legacy launch, keyboard/mouse synthesis, clipboard changes, registered scenes, primary DISPLAY3 and top-left. Admission occurred **21:16:38 local**, after the stated start. The copied time parser was adjusted **before its first invocation** to accept optional seconds and parse the exact timestamp, without rounding or changing the deadline. Process admission protections were unchanged.

Live PMv2 enumeration selected nonprimary DISPLAY29: bounds `(-1920,1506,0,2586)`, work `(-1920,1506,0,2538)` — 1920×1080 / 1920×1032. The primary remained DISPLAY3, bounds `(0,0,3840,2160)`, work `(0,0,3840,2088)`; top-left DISPLAY30 was not selected. No primary fallback was introduced. The reused bounded 60-second held-input preflight returned on sample 1: **no held virtual keys**, idle **687 ms**. This preflight establishes all-up, **not** the downstream 1500-ms quiet requirement, which remained unchanged.

## Phase 0 artifacts and exact trial change

Only one tracked boolean was temporarily enabled; N3 needs no UI/native/input/startup flag:

```diff
--- a/scripts/run_uno_candidate_acceptance.py
+++ b/scripts/run_uno_candidate_acceptance.py
-FULL_HOST_OBSERVER_REVIEWED = False
+FULL_HOST_OBSERVER_REVIEWED = True
```

Exact retained Git diff SHA256: `c6d9d718ffb82ca32b55a5ea14d4128a02c0edaacffd7aabe164b1153ffee811` (`nr15-enabled-flags.diff`). Enabled observer SHA256: `71f90d6e586f7cdd3fbed0284b0260a2337e962ec7503724f2b599bb074ead88`. No enabled flag was committed.

Fresh sequential strict native x64 builds used new unique output directories, pinned SDK 10.0.403 and the existing artifact gates. Native PE/SQLite, effective-property/reference checks, empty NoWarn, AOT/trim analyzers, warnings-as-errors and unsuppressed trim diagnostics passed. Both independent warning/error scans passed; both executable hashes were rechecked after release. No legacy/native-insertion fixture/support artifact was needed or launched.

| Artifact | Build token | SHA256 |
|---|---|---|
| Linked-source management fixture | `1849f5d2bf9d46fb99fc8281bb6c88ff` | `1ac736d406e4ccbdfbb38ac8e22566dae0ed2e2cb6534c448692fefe55851cec` |
| Ordinary candidate, build only; never launched | `83640c1679cc4e7e8f503c9f654a72f4` | `0c4ae7152ddadee947801613f74efe9cbaa5710623d9d4a1f30dd3298b2cffef` |

Management linked-source fingerprint: `b16a349d2cf1496dd2f43ca51721d0a4499f73c81c73b9107beef8226892ae6b`.

Copied nr-14 helpers with explicit UTF-8, never edited originals. The admission helper preserves exact process classification and self/live-ancestor/trusted-parent exclusions; its former clipboard-seeding tail was removed entirely. Two consecutive dry admissions ran in **one bash → Python → pwsh -File chain**, each returning exit 0, `admission_dry_pass=1`, empty stderr. Python AST/PowerShell parser checks and zero-process readiness passed. The nr-14 bounded preflight was reused with run-ID substitutions only. Helper hashes and syntax evidence are retained privately as `nr15-helper-hashes-final.json` and the phase-0 records.

## Commands

`<scratchpad>` denotes the brief/report directory; `<fixture>` and `<hash>` denote the pinned management entry/hash above. Project builds were sequential; build wrappers internally selected their pinned SDK directories.

```text
# set -e; explicit UTF-8 preparation, exact flag diff retained
python -I -B <scratchpad>/nr15-prepare.py
python -I -B <scratchpad>/nr15-dry-admission.py
pwsh -NoProfile -File <scratchpad>/nr15-process-check.ps1
pwsh -NoProfile -File <scratchpad>/nr15-build-phase0.ps1
# Sequential inside the build-only wrapper:
pwsh -NoProfile -File scripts/verify-uno-candidate.ps1 -BuildOnly
pwsh -NoProfile -File scripts/publish-uno-candidate-aot.ps1
python -I -B <scratchpad>/nr15-artifacts.py
pwsh -NoProfile -File <scratchpad>/nr15-parse-helpers.ps1
# PHASE0-READY; filesystem-only polling, then exact start/deadline admission
python -I -B <scratchpad>/nr15-wait-grant.py
python -I -B <scratchpad>/nr15-admit-window.py
# set -e / && prevents execution after a failed admission or preflight
python -I -B <scratchpad>/nr15-preflight.py
python -I -B <scratchpad>/nr15-run.py
```

The retained parent invoked the real entrypoint once per runnable selector, exactly in this order:

```text
python -I -B scripts/run_uno_candidate_acceptance.py --scene preview-options --executable <fixture> --sha256 <hash> --authorized-screen-slot --work-area -1920 1506 0 2538
# Same arguments, once each: hover, tray, close-background, message-window, dpi
```

Before each invocation the unchanged process-admission body ran without clipboard seeding. A read-only parent collected existing receipts/diagnostics and, if readiness receipts existed, owned-window geometry/DPI. It never acknowledged fixture phases, sent messages, activated windows or altered observer guards. No readiness receipt existed, so this additional collector recorded no window samples. The actual observer alone controlled permits, Jobs and acknowledgements.

Coexistence ran the same real CLI once through `nr15-coexistence-observe.py`/`runpy.run_path`, using a deliberately nonexistent executable and all-zero digest. A read-only Python trace retained only the exact allowlisted exception and boundary call counts; it did not replace any guard or entrypoint behavior. No worker was invoked directly.

## Per-scene results, actual receipts and diagnostics

Every invocation exited **1** and its CLI returned `fullHost=BLOCKED`, `nativeMutation=NOT_RUN`, with empty CLI stderr. The six required acceptance attempts below are recorded **FAIL**, including truthful pre-launch guard refusals; the expected coexistence refusal is separately **BLOCKED**, never PASS.

**Actual receipt sequence and acknowledgements: empty for every scene.** No `shell-ready`, scene receipt, `shell-verified` or `graceful` receipt was accepted. Missing receipts never become success. Every runnable scene retained parent failure `worker_protocol`; its more specific worker token follows.

| Order / scene | Result | Private evidence token | Worker diagnostic | DPI/geometry and cleanup facts |
|---|---|---|---|---|
| 1 `preview-options` | FAIL | `ss-uno018-ui-61433d98a67411c04edec357aad0e169` | `input`, `inputSubreason=idle` | Refused before launch/shell permits or app logs; no candidate window/DPI observation. Zero processes after cleanup. |
| 2 `hover` | FAIL | `ss-uno018-ui-9bded21a18211535e4fee1d01d3858a6` | `input`, `inputSubreason=idle` | Same pre-launch refusal; no pointer delivery, window/DPI observation or app logs. Zero processes after cleanup. |
| 3 `tray` | FAIL | `ss-uno018-ui-b6b531d063ee8fc2121cad9281ecb939` | `input`, `inputSubreason=idle` | Same pre-launch refusal; no tray callback or window/DPI observation. Zero processes after cleanup. |
| 4 `close-background` | FAIL | `ss-uno018-ui-052501518d753c6457051b3f07c3347b` | `shell_window_dpi` | Launch/shell permits present; observer rejected an owned visible window's DPI before readiness. No numerical expected/actual DPI or window rectangle retained. Failure cleanup left zero processes; no graceful receipt. |
| 5 `message-window` | FAIL | `ss-uno018-ui-e36fe1874aeb503e66f100f470ccafc9` | `early_exit`; fixture stderr exactly `fixture_failed` | Launch/shell permits present, no app log/readiness/window sample. No 20-cycle/resource-count proof. Zero processes after failure cleanup. |
| 6 `dpi` | FAIL | `ss-uno018-ui-9762d9b4d5ec93a16633a61f1eeb30a4` | `shell_window_dpi` | Same pre-readiness DPI rejection and missing numerical/window telemetry as close-background. Zero processes after failure cleanup; no graceful receipt. |
| 7 `coexistence` | BLOCKED, expected refusal verified | No session created | `shell_legacy_isolation_unavailable` | No artifact/profile/desktop/launch access by the scene; no legacy process or coexistence acceptance. |

The live preflight geometry above is the common monitor fact, **not** successful placement evidence for any failed scene. `shell_window_dpi` identifies the unchanged observer predicate, not the particular HWND or numerical mismatch. No numerical DPI, transient-window cause, input source or message-cycle leak cause is inferred. The first three idle refusals contain no serialized idle measurement beyond their fixed subreason; the preflight's earlier 687-ms sample is not substituted for them.

Close-background and dpi each retained app/stdout/stderr, zero `candidate_started` lines and zero ERROR lines; neither completed startup/readiness. Their private SQLite aggregate read-only checks returned **0 snippets / 0 usage**, not completed shell-action evidence. Message-window emitted the fixed failure marker without a specific cycle exception; strict resource-count failure is only a possible source path, not an observed diagnosis.

Coexistence trace, exactly:

```json
{"reason":"shell_legacy_isolation_unavailable","artifactValidationCalls":0,"privateDirectoryCalls":0,"desktopConstructionCalls":0,"subprocessLaunchCalls":0}
```

No new session directory appeared. The real CLI refused before artifact validation despite the nonexistent executable/invalid artifact pin. This confirms D49's fixed boundary, not native coexistence.

### Privacy and release per scene

All six runnable attempts passed the shared planted UTF-8/UTF-16LE/UTF-16BE negative controls (21 detections each) and their available diagnostic evidence scanned **CLEAN**. Record counts: preview-options **4**, hover **4**, tray **4**, close-background **7**, message-window **6**, dpi **7**. Scans included available app/stdout/stderr, observer failure JSON and CLI stdout/stderr. Absent application logs in pre-launch refusals are explicitly absent, not application privacy proof. SQLite contains synthetic fixture data and was not diagnostic-scanned. Coexistence created no profile/app diagnostics; no native privacy PASS is claimed for it.

Every scene preserved both clipboard sequence and foreground HWND equality at the parent's before/after boundary. All six owned sessions had zero pending-recovery filenames. All seven post-invocation process audits found zero candidate/fixture/guardian/Target018/UI-helper processes; the sole runner process during those audits was the retained parent, excluded by exact identity. Failed scene cleanup and final zero-process counts are **not** graceful success or pre-emergency JobZero acceptance.

## Required receipt sequences and proof limits

For each runnable scene the required sequence is `shell-ready` → its middle phases below → `shell-verified` → posted Exit seam → `graceful`. **None was observed in this run.** The following README proof limits are retained verbatim:

| Selector / required middle phases | Proof limit / missing work (verbatim) |
|---|---|
| `preview-options`: `shell-preview` | Common launch consumer only, **not** initial/second-process redirect; foreground epoch preservation is **not an owned-target proof** |
| `hover`: `shell-hover-open`, `shell-hover-closed` | Posted pointer messages, **not physical pointer/input acceptance**; top-level HWND delivery may not reach the XAML input-site HWND, which refuses by timeout; no input fallback |
| `tray`: `shell-tray-open`, `shell-tray-settings` | **Not** physical tray clicks, Explorer event delivery, TrackPopupMenu command selection or tray identity proof |
| `close-background`: `shell-background`, `shell-reopened` | No dirty-dialog, in-flight insertion, sleep or shutdown fault-matrix proof |
| `message-window`: `shell-message-cycles` | Strict exact counts may truthfully refuse platform/framework noise; no tolerance or leaked-window allowance |
| `dpi`: `shell-dpi` | No primary fallback, mixed-monitor dragging or sleep/re-show stress |
| `coexistence`: no receipt sequence; pre-launch refusal | **D49 — NOT NATIVE-CLOSABLE IN PLAN 019:** production legacy hardcodes AppPaths/instance and has no owned-profile bootstrap; starting it with an ignored `--profile` would access production state. No legacy launch, profile/registry diff, registration or coexistence PASS is claimed |

The background requirement remains exactly zero visible owned windows; no background receipt was reached. Owned/pinned foreground-target lifecycle and actual process redirect remain unimplemented. No physical pointer movement, Explorer click, keyboard route, initial/second-process redirect, dirty/in-flight/sleep matrix or native insertion/Copy acceptance was attempted or established.

## Final release and retained evidence

Desktop work ended **21:17:06 local**. Restored the single trial flag false immediately, verified all eleven tracked startup/full-host/UI/native/C# execution booleans false, and verified the tracked trial-source diff empty. Final process audit at **21:17:18 local**: candidate **0**, management fixture **0**, guardian **0**, Target018 **0**, runner **0**, UI helper **0**. No pending clipboard/keyboard recovery; N3 introduces neither. Whole-run clipboard sequence and foreground remained unchanged; no clipboard contents were read, reseeded or overwritten. `SCREEN-DONE nr-15 2026-11-01 21:17:44 local` was written to the report, well before the hard end. No desktop access occurred afterward.

Filesystem-only post-release audit `nr15-evidence-check.py` verified the seven ordered invocations, all empty receipt/ack sequences, precise refusal boundary, post-cleanup zero processes, unchanged epochs and both artifact hashes. Evidence: `nr15-scenes.jsonl`, `nr15-final-verification.json`, `nr15-artifacts.json`, `nr15-flags-closed.txt`, process/preflight/grant records and the private session tokens above. No scene or phase was retried. Only this research document is committed; local instruction files remain unstaged.
