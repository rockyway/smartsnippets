# 045 — nr-04: controls PASS; picker input failure; posted-row foreground failure

Authorized `native-run5-brief.md`, 2026-11-01. Plan 019 remains in progress.
This is bounded fixture evidence, not picker parity, positive insertion acceptance,
production authorization, physical-input proof or Plan 018/019 completion.

## Authorization, screen gate and preparation

- Worktree `.claude/worktrees/uno019-native-trial4`, branch
  `feature/uno019-native-trial4`, starting HEAD `25dfd4b`.
- Read AGENTS, safety guide 002, progress handoff 002, approved Plan 019,
  research 034–044, both fixture READMEs and previous helpers/reports.
- Phase 0 performed only flag edits, helper copies, builds and hashes. **No window,
  clipboard access or application launch occurred before the grant.**
- Filesystem-only polling began at `2026-11-01T19:57:25.140817Z`. It checked every
  30 seconds, with a 90-minute deadline, and observed the grant on check 34 at
  `20:13:55.160210Z`, after 990.020 seconds. No DENIED/REVOKED was observed.
  The grant file was then 14.212 seconds old; the 60-minute admission deadline was
  bounded from that file update, not reset by later launches.
- Grant began `GRANTED nr-04 14:13 bottom-left DISPLAY29 -1920,1506 1920x1080 ~60min`.
  It additionally warned that another session was using the top-left monitor and
  could change foreground/clipboard: record interference as environment failure,
  never retry. No top-left or primary window was controlled or moved.
- Live PMv2 enumeration confirmed the selected **nonprimary bottom-left** bounds
  `(-1920,1506,0,2586)` and work `(-1920,1506,0,2538)`. Actual primary bounds were
  `(0,0,3840,2160)`, not the stale dimensions in the brief; primary was not selected.
  DISPLAY names were not authority. Default placement needed no source change.
- Held-input preflight was clear. At `20:14:02.6851407Z`, candidate, management
  fixture, guardian, Target018, nr04 Python and UI PowerShell helper counts were
  all zero; there was no pending recovery or uncertain keyboard lifetime.
- Only then, the explicitly authorized fixed harmless clipboard baseline was
  seeded **once**, using the system executable:

```powershell
"Smart Snippets QA" | & "$env:SystemRoot\System32\clip.exe"
```

Locked RAM-only comparison matched the fixed baseline with stable sequence:
4 builtin text formats, 0 ownership markers, 0 protective formats, 0 unsupported
formats, owner absent. No reseed between scenes or afterward. No arbitrary
clipboard payload or unknown format name was logged/persisted.

No registered-* scene, operator prompt/action request, agent keyboard/mouse
synthesis, download, clock change, production-profile access, focus workaround,
input repair or mutation retry occurred. The product's own guarded paste was
within scope but never reached; posted-enter was not run.

## Phase 0 — PASS: fresh strict native artifacts

Exactly nine test-only booleans were enabled temporarily:

| File | Constants |
|---|---|
| `scripts/run_uno_candidate_native.py` | `NATIVE_SCENES_SOURCE_REVIEWED`, `POSITIVE_SCENES_SOURCE_REVIEWED`, `POSITIVE_INPUT_SOURCE_REVIEWED` |
| `scripts/run_uno_candidate_ui.py` | `UI_SOURCE_REVIEWED` |
| `scripts/uno_candidate_native_fixture/Program.cs` | `NativeFixtureSourceReviewed` |
| `scripts/uno_candidate_native_fixture/PositiveProtocol.cs` | `SourceReviewed`, `InputSourceReviewed` |
| `scripts/uno_candidate_native_fixture/OwnedTargetPreparation.cs` | `SourceReviewed` |
| `src/SmartSnippets.Uno/Platforms/Windows/AcceptanceNative/NativeAcceptanceFactory.cs` | `SourceReviewed` |

Startup/full-host/prototype and ordinary candidate gates stayed closed. Exact
boolean-only diff is retained privately as `nr04-enabled-flags.diff`, SHA256
`9ba25a3902ba69ea4bac874b7cad43ab2e23f028a57af200d4d94faf13d3548d`.

Commands ran sequentially from the worktree root (`<scratchpad>` denotes the
brief's private scratchpad):

```text
pwsh -NoProfile -File scripts/uno_candidate_native_fixture/build.ps1 -BuildOnly
pwsh -NoProfile -File <scratchpad>/nr04-build-support.ps1
pwsh -NoProfile -File scripts/verify-uno-candidate.ps1 -BuildOnly
```

The copied support helper creates new owner-private output directories, then
cleans and publishes Target018 and guardian sequentially with `NoWarn=`,
AOT/trim/compatibility/analyzers/warnings-as-errors true, and trim-warning
suppression/single-warning aggregation false. Fixture wrappers select their
pinned SDK and enforce native x64/SQLite/property/reference/freshness gates.
All four warning/error scans passed; all four executable hashes were independently
recomputed after execution and matched. No warning suppression, AOT gate weakening
or additional test-suite execution is claimed. Target018 was rebuilt for the new
protocol; no nr-03 executable was reused.

| Artifact | Fresh build token | SHA256 |
|---|---|---|
| Native fixture | `8334ab2c646e4e7c861be6ebe3b4aecc` | `e8080752c63048d69c397a53250ee4d948fd5cad881b0912f123f695af47a816` |
| Target018 | `9f3791ceaffc4adab354f135ccd9ad7d` | `fa482754ebcda3ba264be852b34b98ec8093c8bf4d73930dcfbfbbe58ffd2555` |
| Guardian | `70c124416e4a4a5f87af7b3a4da64879` | `fa41223e79dd26760723f4f464a57ccd528000b0680898ee4b9e0e7e98cbd964` |
| Management fixture | `fa0fb5fe7bd64c20b0ccc9442665b157` | `a6e3a66d63ffc2d6293ba124c804515b68f312ba6879f74501a0ec18af64dd5e` |

Native enabled-source receipt:
`7fb69a67f791042e3c86a6cf3e7af1ca8a188600d95e408fab9ba1f18664c8d9`.
Management base source receipt:
`51f8567ee0ac1b10e74d9cef0437059b23da4b9d03e2a3e9773c01e131534450`.
Fixture outputs use `bin/publish-native/<token>` and
`bin/publish-acceptance/<token>`; target/guardian outputs use `bin/nr04-<token>`
under their respective prototype projects.

Original nr01/nr02/nr03 helpers were not edited. New scratchpad copies changed the
worktree, names and artifact pins, replaced the expired hardcoded deadline with
the grant-derived monotonic deadline, propagated picker child failure, and required
zero native/UI survivors between phases (only the trusted parent itself remained).
No placement or tracked behavior/helper change was necessary.

## Phase 1 — PASS: same-artifact restore → abort → owner-refusal

```text
python -I -B <scratchpad>/nr04-wait-grant.py
python -I -B <scratchpad>/nr04-preflight.py
pwsh -NoProfile -File <scratchpad>/nr04-process-check.ps1
# authorized seed above, followed by:
python -I -B <scratchpad>/nr04-clipboard-check.py
python -I -B <scratchpad>/nr04-run.py
```

Trusted parent: `ss-uno018-ui-692e6af8ef41cff8b044f832dde45114`.
It verified bottom-left geometry and unchanged-desktop/all-up/quiet preflight,
rechecked the grant before each scene, and retained returned
`RestorativeSessionObservation` objects only in its own RAM. No JSON import,
witness reconstruction, latest-directory discovery or false recopy assertion
supplied authority. The same source/artifacts stayed enabled through all scenes.

| Scene | Exact private session | Result |
|---|---|---|
| restore | `ss-uno018-ui-e3a5f552b5643cc7d58c431fae650965/ss-uno019-native-904a72a5c95748719a300e6f8249d914` | PASS: `clipboard_control_settled` / `regular_restored` |
| abort | `ss-uno018-ui-d46584d5092781d4069e56eddd283115/ss-uno019-native-4ee317bedd2446e8b80954c66438fdad` | PASS: `clipboard_control_settled` / `abort_restored` |
| owner-refusal | `ss-uno018-ui-e6b83ecc6d20d2b53cf53d1e848a53b4/ss-uno019-native-b6f333981d0347bab87afdb7b0b8e151` | PASS: `clipboard_control_settled` / `owner_refusal_restored` |

All three returned the enabled fingerprint above. Exact ordered receipts, empty
native stderr, host/error checks, privacy scans and planted negative controls
passed, including separate read-only receipt/privacy revalidation. No pending
receipt or recovery retry occurred. Abort intentionally terminates only its
worker/target Job; this is not graceful worker shutdown evidence. Owner-refusal
changes owned HWND A→B within the same guardian PID, not an arbitrary foreign
producer. Restoration preserves supported original representation bytes plus
declared ownership/protective metadata, not identical original format sets.
Original backups stayed in guardian RAM.

## Phase 2 — FAIL at Import Cancel; picker acceptance BLOCKED

The parent ran copied `nr04-picker.py`, invoking the actual UI runner:

```text
scripts/run_uno_candidate_ui.py --scene file-picker
  --executable <management-output>/SmartSnippets.UnoAcceptanceFixture.exe
  --sha256 a6e3a66d63ffc2d6293ba124c804515b68f312ba6879f74501a0ec18af64dd5e
  --authorized-screen-slot --work-area -1920 1506 0 2538
```

Picker parent `ss-uno018-ui-cfa929179b12d6094cec2b24b81f204d`; exact UI session
`ss-uno018-ui-f1c7bdd25b5b7bae3e2ba4055d6c8322`. Child and wrapper exit 1;
parent stderr empty. Fixed observer failure records:

```json
{"observer-parent-failure.json":{"reason":"worker_protocol"},"observer-worker-failure.json":{"reason":"input"}}
```

Both `foreground-failure.json` and `ui-helper-foreground-failure.json` are absent.
There is **no 13-field foreground record to copy**: this was an input guard
failure, not a recorded foreground refusal. Helper stdout/stderr are empty.
`input` can mean held input, insufficient idle or immutable input-epoch change;
the retained record does not identify which or who/what caused it. Under the grant's
concurrency warning this is recorded as environment/input admission failure, not
as proof of a picker-provider defect or attribution to the other session.

| Step | Result |
|---|---|
| Import Cancel | FAIL: `input`; no completion receipt, picker acceptance BLOCKED |
| Import Commit | BLOCKED / NOT RUN after first phase failure |
| Export Cancel | BLOCKED / NOT RUN after first phase failure |
| Export Commit | BLOCKED / NOT RUN after first phase failure |

`ui-ready.receipt` and `picker-import-cancel.active` exist; no closed marker or
completion receipt exists. No later phase's active/closed/receipt marker exists.
Read-only owned SQLite aggregate: 0 rows / 0 usage. Diagnostic privacy scan and
planted negative controls: CLEAN. Clipboard epoch unchanged across the attempt.
Existing owned helper/fixture Job cleanup completed; no graceful host shutdown
or successful pre-kill JobZero claim is made. No mutation retry or guard relaxation.

At `20:14:24.1102678Z`, the between-phase audit found zero candidate, management
fixture, guardian, Target018 and UI helper processes; only the trusted nr04 parent
remained. Its control witness stayed in RAM throughout the picker attempt.

## Phase 3 — FAIL at posted-row before foreground-grant stage

Phase 3 ran independently of phase 2 because phase 1 passed. The same parent
called `scripts/run_uno_candidate_native.py` with exact native/target/guardian
paths and hashes, `--authorized-screen-slot --scene posted-row
--row6-controls-verified`, and the returned owner-refusal witness. Legacy generic
runner instructions in private stdout were not an operator request and were not
acted upon; no registered scene or external UI/input driving occurred.

Exact session:
`ss-uno018-ui-2b679d1775d1f34f4f7867af436c42fc/ss-uno019-native-ff4e2a13103c47c8a69aa1815ddb9b12`.

Runner: `result=native_scene_failed`, **`failureStage=guardian_ready`**.
Native stdout was exactly `native_phase=guardian_ready` →
`native_phase=awaiting_owned_a` → `native_result=failed`; stderr empty.
The exact sanitized failure line:

```text
2026-11-01T20:14:28.825Z ERROR native_positive_failed stage=guardian_ready InvalidOperationException (HResult=0x80131509)
```

Ordered fixed diagnostics:

```text
owned_initial_focus phase=waiting
owned_preparation_admission reason=foreground
owned_target_preparation reason=Refused
owned_initial_focus phase=refused
```

The owned-A preparation guard refused foreground admission before accepted
preparation, `target_foreground_grant`, popup focus/readiness, keyboard arm,
insertion or paste. `guardian_ready` is the last entered stage, not a claim that
the exception occurred inside guardian startup. The `foreground` token rejects
an unallowed/changed initial foreground; it does not identify a foreign window,
thread, application or cause. Record environment/foreground failure under the
concurrency warning, without attributing it to the other session. Neither the
new foreground grant nor the picker observation fix reached an acceptance result.

Read-only owned SQLite: 1 seeded row / 0 usage. No `insert_start`, foreground-grant
receipt, recovery-pending stdout or keyboard-pending diagnostic. Privacy scan
with planted negative controls: CLEAN. No `candidate_shutdown_complete` exists;
subsequent zero process counts do **not** establish graceful failed-host shutdown.
Parent stderr was empty and its final `nr04_observer_complete=1` was observed.

| Scene | Result |
|---|---|
| posted-row | FAIL: foreground admission; `failureStage=guardian_ready` |
| posted-main | BLOCKED / NOT RUN after first phase failure |
| posted-enter | BLOCKED / NOT RUN after first phase failure |

Proof limit: **`registered_posted_callback_not_physical`**. Posted callbacks are
not HID, native pointer/menu or physical shortcut proof. This run did not reach
a successful posted callback/insertion. No failed scene was retried.

## Release — PASS: flags closed, zero processes, baseline verified

All nine flags were restored false immediately after the sequence. Exact grep
proof is retained in `nr04-flags-closed.txt`; it additionally confirms
`STARTUP_OBSERVER_REVIEWED=False`, `FULL_HOST_OBSERVER_REVIEWED=False`, and
prototype `NativeEntryReviewed=false`. `git diff --exit-code` passed for every
temporarily edited source file. Ordinary candidate capability remains denied.
No enabled flag, behavior fix, placement helper or scratchpad helper is committed.
Operator edits to AGENTS and safety guide were preserved, never edited or staged.

Read-only release process counts at `2026-11-01T20:15:06.5422570Z`:

| Process group | Count |
|---|---:|
| Candidate coordinator/worker | 0 |
| Management fixture | 0 |
| Guardian | 0 |
| Target018 | 0 |
| nr04 Python run/picker helpers | 0 |
| UI PowerShell helpers | 0 |

No pending coordinator/guardian, clipboard recovery or uncertain keyboard lifetime
remains. No pending process was killed, manual recovery endpoint called, generic
key-up synthesized or further native run attempted. Screen work ended within
two minutes of the grant; the bottom-left slot is safely released.

Final locked clipboard check: fixed authorized post-seed baseline matches exactly
in RAM; stable sequence, 4 builtin text formats, 1 ownership marker, 3 protective
formats, 0 unsupported formats, owner absent. No further clipboard write. This
is not restoration of pre-seed user clipboard data or immunity to later changes.

Private evidence: `nr04-artifacts.json`, `nr04-enabled-flags.diff`,
`nr04-gate-result.json`, `nr04-wait-grant.py`, `nr04-build-support.ps1`,
`nr04-run.py`, `nr04-picker.py`, `nr04-preflight.py`, `nr04-process-check.ps1`,
`nr04-clipboard-check.py`, `nr04-evidence-check.py`, `nr04-evidence.txt`,
`nr04-process-release.json`, `nr04-clipboard-release.json`, `nr04-flags-closed.txt`,
and the exact retained private sessions above. Enabled binaries are evidence,
not permission to rerun against now-closed source.

No active recovery/operator decision is pending. Remaining picker actions and
posted-main/posted-enter are unrun; both changed paths still need separately
reviewed/authorized native acceptance under a stable environment. No further
runtime retry is authorized by this completed run. Commit scope: **only this doc**.
