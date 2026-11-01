# 043 — Bottom-left clipboard controls PASS; file-picker BLOCKED

Authorized brief `nr-01`, 2026-11-01. Plan 019 remains in progress; this is not
production authorization, insertion acceptance, picker parity or Plan 018/019
completion. No registered-* scene, keyboard/mouse synthesis, downloads, clock
changes or real-profile access was performed.

## Scope, geometry and source

Trial worktree: `.claude/worktrees/uno019-native-trial2`, branch
`feature/uno019-primary-native-trial2`, starting HEAD
`91a9a0abf79a133f0c332c140bec26c5cba75532`. The brief explicitly superseded the
older primary-landscape instructions. Default bottom-of-leftmost-column placement
was retained; no placement/source guard was weakened.

Live per-monitor-v2 enumeration before controls and picker confirmed the selected
**nonprimary** monitor bounds `(-1920,1506,0,2586)` and work rectangle
`(-1920,1506,0,2538)`. The enumerated primary was actually `(0,0,3840,2160)`,
not the brief's stale primary dimensions; it was not selected. No DISPLAY name
was used as authority. Read-only preflight found no held virtual keys; the
trusted control parent additionally enforced unchanged desktop/foreground,
all-input-up and 1500ms quiet admission. Relevant process count was zero before
the single authorized synthetic baseline seed.

Read approved Plan 019, safety guide, progress handoff, research 034–042 and both
fixture READMEs. Existing operator changes to `AGENTS.md` and the safety guide,
and existing Python cache directories, were preserved and excluded from commit.

## Temporary flags and helper changes

Phase 1 changed exactly these false constants to true:

- `scripts/run_uno_candidate_native.py`: `NATIVE_SCENES_SOURCE_REVIEWED`.
- `scripts/uno_candidate_native_fixture/Program.cs`: `NativeFixtureSourceReviewed`.
- `src/SmartSnippets.Uno/Platforms/Windows/AcceptanceNative/NativeAcceptanceFactory.cs`:
  `SourceReviewed`.

All three were restored false before the management build. Phase 2 changed only
`scripts/run_uno_candidate_ui.py: UI_SOURCE_REVIEWED` false→true, then restored
false on each exit. Positive scene/input/preparation, startup/full-host observer,
prototype entry and ordinary product gates stayed closed. No source fix was
needed or committed.

Original temporary helpers were not edited. A scratchpad copy of
`uno019-reviewed-preparation-trial.py` became `nr01-controls.py`: new trial root,
fresh executable paths/hashes, exact nonprimary bottom-left requirement, no old
witness reconstruction, and only restore→abort→owner-refusal. Each returned
`RestorativeSessionObservation` was passed directly to the next call in trusted
parent memory. No JSON/CLI/evidence discovery supplied baseline authority.
`nr01-picker.py` retained the exact current invocation's session from its stack
for failure reporting only. A copy of `uno019-clipboard-format-check.py` became
`nr01-clipboard-check.py`; its final fixed-baseline comparison is RAM-only and
prints booleans/counts, never payloads. Additional build/preflight/evidence
helpers remained outside the repository.

Private scratchpad evidence is under the brief's scratchpad directory:
`nr01-enabled-flags.diff`, `nr01-ui-enabled-flags.diff`, `nr01-controls.py`,
`nr01-picker.py`, `nr01-build-support.ps1`, `nr01-preflight.py`,
`nr01-clipboard-check.py`, and `nr01-evidence-check.py`.

## Phase 0 — fresh strict builds PASS

Builds ran sequentially, without warning suppression or weakened AOT gates:

```text
pwsh -NoProfile -File scripts/uno_candidate_native_fixture/build.ps1 -BuildOnly
pwsh -NoProfile -File <scratchpad>/nr01-build-support.ps1
pwsh -NoProfile -File scripts/verify-uno-candidate.ps1 -BuildOnly
```

The support helper creates fresh owner-private output directories and runs
`dotnet clean` then `dotnet publish <project> -c Release -r win-x64 -o <output>`
with `NoWarn=`; AOT/trim/compatibility/analyzers/warnings-as-errors true and
trim-warning suppression/single-warning aggregation false. Fixture wrappers use
their pinned SDK and strict property/reference/native x64/SQLite/freshness gates.
All four build logs additionally passed a warning/error scan. Final hashes were
independently recomputed after execution.

| Artifact | Fresh build token | SHA256 |
|---|---|---|
| Native fixture | `ee0cfa19210d4051b627af213aa62dce` | `15c4a4b7d3dce1639f09a04714e0d19e3570830a24ae5ce54a10d9900a529725` |
| Default-placement Target018 | `f3bf97c97d0e4cabb4cde59641bec547` | `494eee33335f2c2d82536dca8ae15fbef3bbe03d6a63356272498058487247e6` |
| Guardian | `6d857fad930a46c4a7d6bc9a9784524a` | `613b45256087b5d226f5f93958bc28e78e5776876f7e61703e9a8d68f3665911` |
| Management fixture | `36e30db8ff144c368ed6aaf841bd8c66` | `3b56535d7e87a6546bae282c07eea158209c3bd7b53bfa3818112e882c888803` |

Native enabled-source fingerprint:
`9bb937b0d47881ff172f3ffb7b1db0ff9aaa392b8827b47f382f147b34a338bd`.
Management fixture base source receipt:
`9f930035ab0d16ff01abb2fd9b13aeb984ce9d0e419a3392aab1419857e46ef6`.

Target/guardian output children are respectively `bin/nr01-<token>` under their
prototype project directories. Native and management fixture outputs use their
standard `bin/publish-native/<token>` and `bin/publish-acceptance/<token>` roots.
Guardian-related tracked source has no diff from reviewed `919eb48` through
starting HEAD (guardian project, AcceptanceNative and GuardianWindow paths).
Nevertheless a fresh guardian was built and pinned; old `d5e58...` bytes were not
reused or asserted equal. Enabled binaries are retained evidence, not permission
to execute against the now-closed source fingerprint.

## Phase 1 — same-artifact controls PASS

After zero-process/no-pending-recovery checks, the brief-authorized system
`clip.exe` command seeded the fixed synthetic baseline exactly once. It was never
reseeded between scenes or afterward. Command:

```text
python -I -B <scratchpad>/nr01-controls.py
```

Trusted parent `ss-uno018-ui-ad10467a0e16022a2db9b0cb701c5f2a` exited 0 with
empty stderr and `nr01_phase1=PASS`.

| Scene | Private session token | Result |
|---|---|---|
| restore | `ss-uno018-ui-67f8bd85e95bcc8d1926bfc2b622d6a4/ss-uno019-native-d39a485459554edcb1b3e4d822bff3f9` | PASS: `clipboard_control_settled`, `regular_restored` |
| abort | `ss-uno018-ui-b98365da652b2e33998336cb5058c63d/ss-uno019-native-34cd0ce049674216b6f297aacc825de1` | PASS: `clipboard_control_settled`, `abort_restored` |
| owner-refusal | `ss-uno018-ui-6dd841541ba4ce2e77a10f3d3c64e525/ss-uno019-native-8d9d6e1bb00444f6bacc643f4b02f211` | PASS: `clipboard_control_settled`, `owner_refusal_restored` |

All three returned the exact same enabled-source fingerprint above. The runner
accepted ordered guardian/publication/result receipts, empty native stderr,
error-free single host startup and privacy checks with planted negative controls.
A separate read-only revalidation of each exact session also passed. No pending
receipt/manual recovery/retry occurred. Abort intentionally terminates only the
worker/target Job after publication; it is not graceful worker shutdown proof.
Owner-refusal is an owned HWND A→B change within one guardian PID, not an arbitrary
foreign producer. Restoration preserves supported original representation bytes
plus declared ownership/protective metadata, not the original exact format set.

## Phase 2 — file-picker BLOCKED; no mutation retry

Command (the wrapper invokes `run_uno_candidate_ui.py --scene file-picker`):

```text
python -I -B <scratchpad>/nr01-picker.py
# --executable .../36e30db8ff144c368ed6aaf841bd8c66/SmartSnippets.UnoAcceptanceFixture.exe
# --sha256 3b56535d7e87a6546bae282c07eea158209c3bd7b53bfa3818112e882c888803
# --authorized-screen-slot --work-area -1920 1506 0 2538
```

An earlier wrapper invocation used the native fixture basename for the management
artifact. The existing provenance guard rejected it before creating a runner
session or launching any app; outer token
`ss-uno018-ui-89c58acaf23c6f70e9246a55cd9a1590`, exit 1, empty stderr, unchanged
clipboard epoch. Only that temporary invocation path was corrected. No guard was
changed and no UI mutation had happened to retry.

The single actual UI execution used trusted parent
`ss-uno018-ui-5199acd0f3c8fba05f1904d2200abc4e` and exact session
`ss-uno018-ui-76de109c016f1c23b0dba6ef2f1ae4e7`. Parent exit 1; empty stderr.
It reached `ui-ready` and `picker-import-cancel.active`, then the Python observer
refused `foreground_changed`. `foreground-failure.json` records
`foregroundRelation=owned_other`; parent reason is `worker_protocol`. No
Import-Cancel completion receipt, phase action receipt, or closed marker exists.
The helper output files are empty. This establishes a foreground-boundary refusal,
not a diagnosis of the precise picker class/thread/broker architecture. Do not
infer a same-thread authenticated provider or permission to adopt the other HWND.

| Required step | Outcome |
|---|---|
| Import Cancel | BLOCKED: actual foreground guard failure before completion receipt |
| Import Commit | BLOCKED / NOT RUN after first failure |
| Export Cancel | BLOCKED / NOT RUN after first failure |
| Export Commit | BLOCKED / NOT RUN after first failure |

No runtime retry, title/desktop discovery, provider fallback, foreign activation,
keyboard/mouse synthesis or widened PID/thread/owner allowance followed. Existing
owned-process/Job failure cleanup stopped the UI fixture/helper; no graceful
shutdown or successful pre-kill JobZero is claimed for this failed scene. Fresh
read-only SQLite aggregate was 0 rows / 0 usage. Post-failure diagnostic privacy
scan plus planted negative controls passed CLEAN. Clipboard sequence stayed
unchanged across the actual attempt (also across the prelaunch refusal).

## Release proof and remaining work

All eight native flags and UI/startup/full-host/prototype gates were verified
false by `rg`; `git diff --exit-code` on all four temporarily edited files passed.
Only pre-existing operator instruction edits remained tracked modifications.
There is no committed source fix, enabled flag or helper change.

Final read-only process counts: native candidate 0, management fixture 0,
guardian 0, Target018 0, and nr01 Python/UI PowerShell helpers 0. No coordinator,
guardian or uncertain keyboard lifetime was killed while pending. No recovery
remains pending. The first post-run clipboard proof found 4 supported builtin
text formats, 1 ownership marker, 3 protective formats, 0 unsupported formats and
no live owner. Locked RAM-only comparison verified the authorized synthetic
baseline exactly; sequence was stable within that check. A later release check
at 18:25:45 UTC still verified the same fixed baseline and a stable locked
sequence, but found 4 builtin text formats, **0 marker, 0 protective formats and
3 unsupported formats**, with no live owner. This is an observed intervening
metadata/format-set change of unknown cause, not permission to capture/drop those
representations or seed again. No further native scene or clipboard write was
attempted. Successful guardian receipts establish restoration at settlement;
they do not guarantee immunity to later clipboard changes. No current recovery
is pending. The restored/verified text is the authorized synthetic baseline, not
clipboard state from before the explicitly authorized seed.

The slot is released safely. Further picker diagnosis requires a separate reviewed
source/observation decision; this refusal must not be converted to PASS or worked
around. Phase 3 remains entirely with the leader. No new test-suite, production
cutover, arbitrary-target/native-input, release-readiness or plan-completion claim
is made.
