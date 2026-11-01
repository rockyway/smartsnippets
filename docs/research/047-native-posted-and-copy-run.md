# 047 — nr-07: controls and posted-row PASS; remaining scenes FAIL

Authorized `native-run8-brief.md`, 2026-11-01. Plan 019 remains In progress.
This is bounded native-fixture evidence, not production authorization, physical
shortcut/pointer acceptance, complete Copy coverage or Plan 018/019 completion.

## Authorization, timing and screen boundary

- Worktree `.claude/worktrees/uno019-native-trial6`, branch
  `feature/uno019-native-trial6`, starting HEAD
  `735cac49f291cf3eb7de1d2bab6a9ffa38ebe33e`.
- Read AGENTS, safety guide 002, progress handoff 003, approved Plan 019,
  research 041–046, both fixture READMEs and nr-06 helpers/report. Existing
  operator edits to AGENTS and safety guide were preserved, never staged.
- Fresh builds completed before 15:57 local; pre-slot helper preparation remained
  filesystem/build-only: **no app launch, window opening or clipboard access
  before 16:20**.
- `nr07-grant.txt` contained the exact line `GRANTED nr-07`, with an exclusive
  **16:20–16:40 local** slot. No DENIED/REVOKED appeared. Filesystem/time-only
  polling every 30 seconds admitted on poll 52 at
  `2026-11-01T22:20:17.465774Z` (16:20:17 local), after 1530.072 seconds.
  The absolute hard end stayed 16:40; normal scene admission reserved three
  minutes for release, and picker admission required at least five minutes.
- Live PMv2 enumeration selected only the nonprimary bottom-left monitor:
  bounds `(-1920,1506,0,2586)`, work `(-1920,1506,0,2538)`. Actual primary bounds
  were `(0,0,3840,2160)`, not the brief's stale primary dimensions. No primary
  or top-left window was selected, moved or controlled; DISPLAY names were not
  identity authority. No placement change was needed.
- At `22:20:36.9716899Z`, candidate, management fixture, guardian, Target018,
  run/picker Python and UI-helper counts were all zero. Held-input preflight
  was clear; no recovery was pending. Only then was the authorized fixed
  synthetic clipboard baseline seeded using the system executable:

```powershell
"Smart Snippets QA" | & "$env:SystemRoot\System32\clip.exe"
```

Initial locked RAM-only verification matched the authorized baseline with stable
sequence: builtin text 4, ownership marker 0, protective metadata 0, unsupported
0, owner absent. Arbitrary clipboard contents/format names were never logged.

No registered-* scene, operator action/request, external keyboard/mouse synthesis,
foreign-focus workaround, generic key-up, download, clock change, production
profile access or failed-scene/mutation retry occurred. Existing generic operator
instructions in runner stdout were not acted upon; the reviewed in-process owned-A
preparation ran. The application's own guarded paste was reached in posted-row.
The posted-enter attempt failed before its posted trigger/input path.

## Phase 0 — PASS: exact trial flags and five fresh native artifacts

Exactly the same nine reviewed trial booleans as nr-06 were temporarily enabled:

| File | Constants |
|---|---|
| `scripts/run_uno_candidate_native.py` | `NATIVE_SCENES_SOURCE_REVIEWED`, `POSITIVE_SCENES_SOURCE_REVIEWED`, `POSITIVE_INPUT_SOURCE_REVIEWED` |
| `scripts/run_uno_candidate_ui.py` | `UI_SOURCE_REVIEWED` |
| `scripts/uno_candidate_native_fixture/Program.cs` | `NativeFixtureSourceReviewed` |
| `scripts/uno_candidate_native_fixture/PositiveProtocol.cs` | `SourceReviewed`, `InputSourceReviewed` |
| `scripts/uno_candidate_native_fixture/OwnedTargetPreparation.cs` | `SourceReviewed` |
| `src/SmartSnippets.Uno/Platforms/Windows/AcceptanceNative/NativeAcceptanceFactory.cs` | `SourceReviewed` |

Startup/full-host/prototype gates and ordinary candidate capability stayed closed.
Exact boolean-only diff: private `nr07-enabled-flags.diff`, SHA256
`42d16e91bd49954d2bc2b2e9fb4cac753ee9e9d094471080759c25a6acbec476`.

Commands ran sequentially from the worktree root. `<scratchpad>` means the private
brief directory; original nr-06 helpers were never edited.

```text
pwsh -NoProfile -File scripts/uno_candidate_native_fixture/build.ps1 -BuildOnly
pwsh -NoProfile -File <scratchpad>/nr07-build-support.ps1
pwsh -NoProfile -File scripts/verify-uno-candidate.ps1 -BuildOnly
pwsh -NoProfile -File scripts/publish-uno-candidate-aot.ps1
python -I -B scripts/run_uno_candidate_native.py --validate-build <native-entry> <native-sha256>
python -I -B <scratchpad>/nr07-artifacts.py
```

Both linked fixtures and the separate ordinary candidate passed their strict
native x64/SQLite/effective-property/reference/freshness gates. The ordinary
candidate was **build-only, never launched**. Support builds cleaned and published
Target018 and guardian into fresh owner-private outputs with `NoWarn=`, AOT/trim/
compatibility/analyzers/warnings-as-errors true and trim-warning suppression/
single-warning aggregation false. All five warning/error scans passed. All five
executable hashes were independently recomputed after execution and matched.
No warning suppression, weakened AOT gate or additional test-suite run is claimed.
Scratch Python helpers compiled, and the narrowed picker PowerShell copy parsed.

| Artifact | Fresh token | SHA256 |
|---|---|---|
| Native fixture | `5a81b24a7d834ff38f3e3e6ac8d4b59d` | `6ae1eae2e4370d96d55ba550ad82823fdae9ca560486e94b393f68fff37573fb` |
| Target018 | `4e0cc8813f2d4e8d8aca15b994a45bad` | `646a362d36c59c7f116986ad05b76fdabee435311eacef76ea513cafbb7c2b61` |
| Guardian | `edc8ef46b73a47f78b19edc85e19b0f5` | `c30932cb16f6d5c7f3b50fdaadbbaf694d383d614b4f7ee7503f39f32a9ee5db` |
| Management fixture | `8c16650e830c43f8be132e284a7f94a5` | `b0b499cc9df238cd9e37afb472a1740a6c6beedf14d4de94b751783d372077c9` |
| Ordinary candidate, not launched | `dbec6edc989344bdaa565b956ab58f9f` | `ecb48ab4ee7e00f032fe6d9a1cc1ae099520eb2a0e5ccc6768d74e5c69775347` |

Native enabled-source receipt:
`eef6afc603fe71f3b0ba6a86277caf57cd9bdacccd091b5810465284e402d017`.
Management base source receipt:
`36f2485708fa350b8377000e798905f8ebf5815d9fe94e15ccddf18795c7a800`.

Fixture outputs use `bin/publish-native/<token>` and
`bin/publish-acceptance/<token>`. Target/guardian outputs use `bin/nr07-<token>`
under their prototype projects. Candidate output is
`src/SmartSnippets.Uno/bin/publish-uno-candidate/<token>`. Enabled binaries remain
private evidence, not authority to launch against now-closed source.

## Execution protocol and results

```text
python -I -B <scratchpad>/nr07-wait-grant.py
python -I -B <scratchpad>/nr07-preflight.py
pwsh -NoProfile -File <scratchpad>/nr07-process-check.ps1
pwsh -NoProfile -File <scratchpad>/nr07-seed.ps1
python -I -B <scratchpad>/nr07-clipboard-check.py
python -I -B <scratchpad>/nr07-run.py
# After all desktop work, flag closure and zero-process check:
python -I -B <scratchpad>/nr07-evidence-check.py ss-uno018-ui-cfa6032510b22533cb020d13288541d6
python -I -B <scratchpad>/nr07-artifacts.py
```

Trusted detached parent: `ss-uno018-ui-cfa6032510b22533cb020d13288541d6`.
It completed with `nr07_observer_complete=1`, empty stderr. It invoked the actual
native runner main with exact native/target/guardian paths and SHA256 pins,
`--authorized-screen-slot --scene <scene>`.

Phase-1 observations were actual returned `RestorativeSessionObservation` objects,
passed directly restore → abort → owner-refusal and retained in that same parent's
RAM. The final control witness was revalidated there before phase 2; it was never
reconstructed from old evidence, JSON, directory discovery or CLI data.

Each positive scene was independent as explicitly required by nr-07: the parent
checked zero native/UI survivors and no retained recovery, then freshly seeded
the authorized harmless baseline before **each** scene. Eight seed operations in
total: one before controls plus seven before positives. The seed helper rechecked
the grant/time/process boundary, excluding only its exact retained trusted parent
PID. Each positive invocation truthfully used `--row6-controls-verified
--operator-recopied-baseline`. No failed scene was retried and no Copy result was
used as a restorative witness. The parent separately measured clipboard epochs
around each call and ran the read-only usage/epoch verifier even after monitor
failure; this diagnostic measurement did not override the scene result.

| Phase/step | Result |
|---|---|
| 0: flags, fresh builds and hashes | PASS |
| 1: restore | PASS: `regular_restored` |
| 1: abort | PASS: `abort_restored` |
| 1: owner-refusal | PASS: `owner_refusal_restored` |
| 2: posted-row | PASS: independent runner and boundary checks |
| 2: posted-main | FAIL: owned preparation refused foreground |
| 2: posted-enter | FAIL: `host_ready`, before guardian/posted trigger |
| 2: popup-copy | FAIL: owned preparation refused foreground |
| 2: editor-copy | FAIL: owned preparation refused foreground |
| 2: footer-copy | FAIL: owned preparation refused foreground |
| 2: context-insert | FAIL: owned preparation refused foreground |
| 3: Import Cancel, once, last | FAIL: `input` guard; clipboard epoch unchanged |
| Import Commit / Export Cancel / Export Commit | NOT RUN: outside granted picker scope |
| Release | PASS: desktop work ended before 16:23, well before 16:40 |

## Phase 1 — same-artifact controls PASS

| Scene | Exact private session |
|---|---|
| restore | `ss-uno018-ui-1bddce5443779e658c1f10a33c550c1a/ss-uno019-native-30298a637de04bc58a6d7be785a4c592` |
| abort | `ss-uno018-ui-f82eb89f59122cefae57e42242510ca1/ss-uno019-native-c4bc9adfaa454434af2a5d756047cbac` |
| owner-refusal | `ss-uno018-ui-fcac0e8b6e641acd1ac03d88bab52573/ss-uno019-native-e7bad9657caf4d3383d3ec5eb7f404e0` |

All three logged `native_control_admission reason=Ready` then
`native_control_publish status=swapped`, with ordered guardian/publication/
restoration receipts, accepted settlement, empty stderr, privacy CLEAN and planted
negative controls PASS. Separate read-only receipt/host/privacy checks passed;
each owned SQLite aggregate was 0 rows / 0 usage. No refusal-only
`native_control_foreground` record exists because admission did not refuse.

No recovery-pending receipt or retry occurred. Abort intentionally terminates only
its worker/target Job after publication; not graceful worker-exit proof. Restore
and owner-refusal logged shutdown-complete. Owner-refusal is an owned HWND A→B
change within one guardian PID, not an arbitrary foreign clipboard producer.
Restoration preserves supported representations plus declared ownership/protective
metadata, not pre-seed user data or identical original format sets.

## Phase 2 — exact session and independent boundary ledger

`rows/usage` below is an independent read-only SQLite aggregate after actual exit.
The usage result compares against the scene requirement, not merely zero safety
side effects. Epoch is the outer independent before/after measurement; the inner
runner emitted successful boundary fields only for posted-row.

| Scene | Exact private session | failureStage | Foreground grant | rows/usage; usage result | Epoch / boundary |
|---|---|---|---|---|---|
| posted-row | `ss-uno018-ui-63a9dcb6b3b8f09df80ecf2c2fb7ff79/ss-uno019-native-d9ae9407f7064509a2ba4a694b91b633` | `none` | `granted` | 1/1; PASS | changed; PASS |
| posted-main | `ss-uno018-ui-d3361bf650b0475a8974d8f41b55e9e5/ss-uno019-native-af841acb0d67427f84bdf0a78b3aed23` | `guardian_ready` | absent | 1/0; FAIL (required 1) | unchanged; FAIL |
| posted-enter | `ss-uno018-ui-1cc34246e80584753c367b759f27f4e3/ss-uno019-native-8ff3bdd8ca014bfdb77368ceb935a96f` | `host_ready` | absent | 0/0; FAIL (required one row/usage 1) | unchanged; FAIL |
| popup-copy | `ss-uno018-ui-84f12d0d63b4a1d2a78a26ecdf93df83/ss-uno019-native-1eb2f7a508844d3eb68a80de0a1b4d8b` | `guardian_ready` | absent | 1/0; zero-usage PASS only | unchanged; Copy mutation boundary FAIL |
| editor-copy | `ss-uno018-ui-557e2516d25174b7cbe0ca538ebb8fca/ss-uno019-native-13e6125ec19141abb1732c6e0b56048a` | `guardian_ready` | absent | 1/0; zero-usage PASS only | unchanged; Copy mutation boundary FAIL |
| footer-copy | `ss-uno018-ui-f5df06346cab981257c1bd362522a727/ss-uno019-native-64fa31bdb2cd41d4ac1b006c1c6dd617` | `guardian_ready` | absent | 1/0; zero-usage PASS only | unchanged; Copy mutation boundary FAIL |
| context-insert | `ss-uno018-ui-409c3249a0876f2297b518c97eb0e7ca/ss-uno019-native-aeb652a0d42448f7b2e05a3d6e8d265f` | `guardian_ready` | absent | 1/0; FAIL (required 1) | unchanged; FAIL |

### posted-row PASS

The runner returned `positive_scene_settled`, `usageVerified=true`,
`clipboardEpochChanged=true`, action proof `vm_callback`. The outer independent
boundary verifier returned the same two true fields. Native stdout was exactly
`guardian_ready` → `awaiting_owned_a` → `posted-row_settled` with normal prefixes;
native stderr empty. No `native_positive_failed` line or monitor-failure file exists.

Fixed diagnostics, in order:

```text
2026-11-01T22:20:59.137Z INFO owned_preparation_admission reason=ready
2026-11-01T22:20:59.506Z INFO owned_target_preparation reason=Prepared
2026-11-01T22:20:59.631Z INFO target_foreground_grant result=granted
2026-11-01T22:21:00.315Z INFO native_popup_focus reason=foreground_accepted
2026-11-01T22:21:00.316Z INFO candidate_popup_focus reason=accepted
2026-11-01T22:21:00.876Z INFO clipboard_swap attempts=1 status=Swapped saved=4 skipped_by_design=0 unsaved=0 cap_exceeded=False bytes=104 ms=173
2026-11-01T22:21:01.456Z INFO clipboard_restore status=Restored attempts=1
2026-11-01T22:21:01.456Z INFO insert_result outcome=Inserted id=1 notice=False ms=1104
```

Accepted native settlement includes exact A readback, empty B, one paste/capture,
duplicate Busy/no-op and usage once. Independent monitor, ordered receipts,
host shutdown, read-only usage, clipboard epoch, privacy scan and planted negative
controls all passed. This establishes only this current-source posted-row route;
it does not establish why historical nr-06 monitoring failed.

### Six failures — full fixed monitor records and sanitized failure lines

Each JSON below is the complete validated `native-monitor-failure.json` for that
scene; no field is omitted. All failures were sampled **before validated closure**
(`closed=false`), not evidence of the hypothesized nr-06 post-closure race.
`foreground_foreign` is the predicate token; the separate relation field is retained
verbatim even when it is `same_pid_other`. No window identity, title, actor or
underlying Windows cause is inferred.

**posted-main**

```json
{"predicate":"foreground_foreign","exitCode":2,"sampleOrder":21,"relation":"same_pid_other","closed":false}
```

```text
2026-11-01T22:21:07.658Z ERROR native_positive_failed stage=guardian_ready InvalidOperationException (HResult=0x80131509)
```

**posted-enter**

```json
{"predicate":"foreground_foreign","exitCode":2,"sampleOrder":14,"relation":"foreign","closed":false}
```

```text
2026-11-01T22:21:12.971Z ERROR native_positive_failed stage=host_ready InvalidOperationException (HResult=0x80131509)
```

**popup-copy**

```json
{"predicate":"foreground_foreign","exitCode":2,"sampleOrder":23,"relation":"foreign","closed":false}
```

```text
2026-11-01T22:21:18.959Z ERROR native_positive_failed stage=guardian_ready InvalidOperationException (HResult=0x80131509)
```

**editor-copy**

```json
{"predicate":"foreground_foreign","exitCode":2,"sampleOrder":24,"relation":"foreign","closed":false}
```

```text
2026-11-01T22:21:24.763Z ERROR native_positive_failed stage=guardian_ready InvalidOperationException (HResult=0x80131509)
```

**footer-copy**

```json
{"predicate":"foreground_foreign","exitCode":2,"sampleOrder":22,"relation":"foreign","closed":false}
```

```text
2026-11-01T22:21:30.233Z ERROR native_positive_failed stage=guardian_ready InvalidOperationException (HResult=0x80131509)
```

**context-insert**

```json
{"predicate":"foreground_foreign","exitCode":2,"sampleOrder":19,"relation":"same_pid_other","closed":false}
```

```text
2026-11-01T22:21:35.798Z ERROR native_positive_failed stage=guardian_ready InvalidOperationException (HResult=0x80131509)
```

The five `guardian_ready` failures each logged owned initial focus waiting,
preparation admission `ready`, preparation result `ForegroundRefused`, then initial
focus `refused`. None logged a `target_foreground_grant` result. Their stdout was
exactly guardian-ready → awaiting-owned-A → failed with normal protocol prefixes.
The stage is the last entered stage, not an exact exception location. No later
Copy/editor/footer/context consumer acceptance can be inferred from these refusals.
The intentional editor store-failure path was not reached.

Posted-enter logged only `native_result=failed`, with no guardian-ready or
awaiting-owned-A receipt and no foreground grant. Its database had no seeded row.
It did log host shutdown-complete; the other five failed positives did not.
All six native stderr streams were empty; privacy CLEAN and planted negative
controls PASS. Read-only receipt/host checks correctly failed. None had a
clipboard/keyboard-recovery-pending receipt. Zero survivors were verified before
each independent next scene. No focus retry, guard exemption or mutation retry.

Proof limit for all posted/captured-Copy/context routes:
**`registered_posted_callback_not_physical`**. VM callback/posted WM_HOTKEY evidence
is not physical shortcut/HID or native pointer/menu delivery. The failed
posted-enter attempt does not prove native Enter delivery.

## Phase 3 — Import Cancel FAIL at input guard; no schema telemetry

The optional phase began at about 16:21:39 local, more than 18 minutes before the
hard end, after all seven positive attempts and a zero-native/UI-survivor check.

Picker parent: `ss-uno018-ui-025a08209fdb44848892a6ddddfff0c0`.
Exact UI session: `ss-uno018-ui-d8329e06b3df599d869719ddffca6893`.
Child and wrapper exited 1; picker-parent stderr empty. Fixed failure files:

```json
{"observer-parent-failure.json":{"reason":"worker_protocol"},"observer-worker-failure.json":{"reason":"input"}}
```

The clipboard epoch was independently unchanged across the attempt. SQLite
aggregate: 0 rows / 0 usage. Privacy CLEAN, planted negative controls PASS.
`ui-ready.receipt` and `picker-import-cancel.active` exist, but no closed marker,
action receipt or `ui-import-cancelled.receipt`; no later picker phase marker exists.
Helper stdout/stderr were empty. Both foreground-failure files and
`ui-helper-picker-schema.json` are absent: **no `picker_schema_multiple` or
per-match isDirectChild/classToken/enabled/offscreen/controlTypeId record was
produced**. Do not borrow nr-06's duplicate-schema result. `input` does not identify
held input versus idle/epoch drift, its source or a picker-provider defect.

To enforce Import Cancel only, scratchpad copies of the existing UI Python and
PowerShell helpers were used. The Python copy only pinned REPO and the copied
helper path. The PowerShell copy added an unconditional bounded stop immediately
after the first `WaitPhase 'ui-import-cancelled'`, before any Import Commit call.
It would print `nr07_import_cancel_only_complete` and raise the existing fixed
`actions` refusal, letting existing owned Job cleanup stop the partial fixture;
that planned stop was **not reached** because input failed earlier. Existing
ownership, foreground, input, schema, privacy and cleanup guards were unchanged.
No tracked helper/placement change was made, no full-picker PASS was possible or
claimed, and no Import/Export commit was admitted. Executed/closed helper SHA256
ledgers are retained privately. The copied UI review flag was also closed afterward.
No retry followed the failure. Failed UI cleanup is not graceful/pre-kill JobZero
acceptance; no recovery-bearing process was terminated.

## Release — PASS, SCREEN-DONE before 16:23 local

All nine tracked flags were restored false, plus the scratch copied UI gate.
`nr07-flags-closed.txt` contains grep proof, including the unchanged closed gates:

```text
NATIVE_SCENES_SOURCE_REVIEWED = False
POSITIVE_SCENES_SOURCE_REVIEWED = False
POSITIVE_INPUT_SOURCE_REVIEWED = False
UI_SOURCE_REVIEWED = False
NativeFixtureSourceReviewed = false
PositiveProtocol.SourceReviewed / InputSourceReviewed = false
OwnedTargetPreparation.SourceReviewed = false
NativeAcceptanceFactory.SourceReviewed = false
STARTUP_OBSERVER_REVIEWED = False
FULL_HOST_OBSERVER_REVIEWED = False
NativeEntryReviewed = false
```

`git diff --exit-code` passed for every temporarily edited tracked source file.
No enabled flag, behavior fix, placement change or helper is committed. Ordinary
candidate capabilities remain default-denied.

At `2026-11-01T22:22:09.2931527Z` (16:22:09 local), release counts were:

| Process group | Count |
|---|---:|
| Candidate coordinator/worker | 0 |
| Management fixture | 0 |
| Guardian | 0 |
| Target018 | 0 |
| nr07 run/picker/copied-UI Python helpers | 0 |
| UI PowerShell helpers | 0 |

No pending recovery, retained coordinator/guardian or uncertain keyboard lifetime
remains. No pending process was killed, recovery endpoint called or synthetic
release sent. The copied UI flag closure was confirmed by 16:22:39. No further
window/clipboard/native operation followed release; bottom-left work finished
well before the 16:40 hard end, with no operator prompt or decision pending.

Final locked RAM-only clipboard check: authorized synthetic baseline matches,
sequence stable during the check; builtin text 4, ownership marker 0, protective
metadata 0, unsupported 0, owner absent. No post-run reseed. This is not restoration
of pre-seed user data or a guarantee against subsequent clipboard changes.

Private retained evidence: `nr07-artifacts.json`, enabled diff, all five build
logs/receipts, artifact recheck, grant polling/result, helper copies and executed/
closed hash ledgers, preflight and initial process/clipboard checks, trusted parent
and exact sessions above, `nr07-evidence.txt`, `nr07-flags-closed.txt`,
`nr07-process-release.json` and `nr07-clipboard-release.json`. No profile/evidence
was deleted. Commit scope is **only this research document**; no push.

Pending acceptance: posted-main/Enter and all three captured-Copy/context consumers
failed before their intended operations; picker input admission/schema telemetry
remains unresolved. Posted-row's scoped PASS does not close these failures, the
remaining native/fault/hover/tray/coexistence/oldest-runtime obligations or Plans
018/019. No further attempt is authorized by this completed, released run.
