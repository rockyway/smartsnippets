# 055 — nr-12: target-thread window diagnostic run

Authorized `native-run13-brief.md`, 2026-11-01. **Phase 0 PASS; same-artifact
restore → abort → owner-refusal PASS; posted-row PASS; five other positives FAIL;
Import Cancel attempt FAIL; release PASS.** No mutation retry. Plans 018/019 stay
incomplete; no production or physical-input parity is claimed.

## Authorization, source update and admission

- Existing worktree `.claude/worktrees/uno019-native-trial11`, branch
  `feature/uno019-native-trial11`, began at `44c0169`. Read AGENTS, safety guide
  002, progress handoff 005, approved Plan 019, research 052–054 and both fixture
  READMEs. Operator-local AGENTS/safety edits were preserved and never staged.
  No delegation, source fix, push or CI run.
- Initial phase 0 used only sequential build-only wrappers and filesystem/process
  queries: no window, desktop/clipboard access or app launch. Report marker
  `PHASE0-READY nr-12` preceded filesystem-only grant polling every 30 seconds.
  Polls 1–35 WAITING; poll 36 observed GRANT_AVAILABLE at
  `2026-11-02T01:13:12.555290Z`, after 1050.030 seconds. No DENIED/REVOKED.
- Exact grant block was `GRANTED nr-12`, start **19:13**, hard end **19:38 local**,
  EXCLUSIVE; bottom-left only. Parsed time admission was
  `2026-11-02T01:13:12.617483Z`. No earlier/latest-directory grant inferred.
- Leader authorized optional diagnostics fold-in, with a 19:20 cutoff. Saved
  original exact flag diff, temporarily closed nine flags, fast-forwarded
  `git merge --ff-only d78b368`, reopened the same nine flags and rebuilt **only
  the native fixture** in a fresh output. Both additional dry admissions passed.
  `PHASE0-READY nr-12 (rebased d78b368)` recorded at
  **19:14:50.993931 -06:00**, before cutoff. This was a pre-scene source update,
  not a scene retry. `git diff --stat 44c0169 d78b368` shows only native runner,
  diagnostic fixture sources/tests and docs; candidate, Target018, guardian and
  management source trees unchanged. Their freshly built original artifacts and
  all hashes were retained. Read the updated diagnostic semantics in 053/README.
- Live PMv2 enumeration: selected nonprimary **DISPLAY29**, bounds
  `(-1920,1506,0,2586)`, work `(-1920,1506,0,2538)`. Primary **DISPLAY3** was
  `3840x2160`, work `(0,0,3840,2088)`; top-left **DISPLAY30** bounds
  `(-1920,306,0,1506)`, work `(-1920,306,0,1458)`. Other live devices were
  DISPLAY5 and DISPLAY2 (full fixed geometry record retained in preflight file).
  No primary/top-left window was controlled or moved. Monitor names are
  diagnostic, not authority.
- nr-11b's single bounded **60-second held-input sampling window** was reused;
  first sample passed: sampleOrder=1, held_virtual_keys=[], inputIdleMs=1063,
  elapsedSeconds=0.001. Downstream immutable epoch, 1500ms quiet, exact identity,
  foreground and work-area guards were unchanged. No operator key/action requested.
- Actual native runner and fresh returned restorative observation objects stayed
  in one trusted parent, `ss-uno018-ui-529ec4abcf656fd228ac7ee5987af7d1`.
  No directory/JSON reconstruction supplied control-witness authority. The parent
  was detached without a timeout-kill/Job around coordinator or guardian.
- Seven fixed clip.exe seeds (before controls, before each of six positives),
  always with zero owned app/recovery processes and no pending recovery.
  Seeds used exactly the authorized harmless baseline; no post-run reseed.
- Each positive ran once even after independent positive failures. Grants were
  rechecked before scenes, reserving three minutes for release. Import Cancel
  was last and completed by 19:16:05, with over 21 minutes remaining before
  hard end (the unchanged >=5-minute admission check passed).
- **posted-enter NOT RUN** (SendInput Enter excluded, D50), all registered-* and
  later picker phases NOT RUN. No external keyboard/mouse synthesis, generic
  key-up, focus retry, input exception, download, clock change or recovery kill.
  Only the product's guarded paste in successful posted-row reached input.
  The runner's historical operator prompts were not acted upon; preparation was
  automatic. Proof limit: **registered_posted_callback_not_physical**, **vm_callback**,
  never physical shortcut/pointer/menu delivery.

## Phase 0 — PASS: flags, builds and hashes

Exactly nine tracked trial booleans were temporarily enabled, then restored false:

| File | Constants |
|---|---|
| scripts/run_uno_candidate_native.py | NATIVE_SCENES_SOURCE_REVIEWED, POSITIVE_SCENES_SOURCE_REVIEWED, POSITIVE_INPUT_SOURCE_REVIEWED |
| scripts/run_uno_candidate_ui.py | UI_SOURCE_REVIEWED |
| scripts/uno_candidate_native_fixture/Program.cs | NativeFixtureSourceReviewed |
| scripts/uno_candidate_native_fixture/PositiveProtocol.cs | SourceReviewed, InputSourceReviewed |
| scripts/uno_candidate_native_fixture/OwnedTargetPreparation.cs | SourceReviewed |
| src/SmartSnippets.Uno/Platforms/Windows/AcceptanceNative/NativeAcceptanceFactory.cs | SourceReviewed |

Original exact diff `nr12-enabled-flags.diff`, SHA256 `a083e9aea6184855a2d6aff897ee91d9f876add483d67e42c6cad757ca0be38f`.

Rebased exact diff `nr12-enabled-flags-rebased.diff`, SHA256 `5b3629baa09dd6d8e92c02cf3fdc1599593c6ed32bfab727bbd6b9b05471e502`.

Startup/full-host/prototype gates and ordinary candidate capability stayed denied.
The copied picker UI gate was trial-only and closed at release. Enabled binaries
are private evidence, not future launch authority.

Initial five strict Native AOT x64 builds ran sequentially into unique output
directories. Effective-property/reference/native-artifact/SQLite/freshness/source
provenance gates and warning/error scans passed. Target/guardian clean→publish used
owner-ACL-private outputs, empty NoWarn, AOT/trim/compatibility/analyzers and
warnings-as-errors true, trim-warning suppression/aggregation false. Rebased native
fixture passed the same wrapper gates and --validate-build. No suppression, weakened
gate, old executable, redundant suite rerun or legacy publish. Ordinary candidate
was build-only and never launched. All original and rebased executable hashes were
recomputed after release and matched.

| Artifact | Token | SHA256 |
|---|---|---|
| native | `ac41e61aa05146dca17111f2c32110fa` | `b7192155a0c6dfd89772b045e99633276acecad5bd331fad759f59d8f6e9372e` |
| target | `0ccc9cf0cfe140a8864bfe7aad4630b8` | `bf38ebeacba86cbef76f0de8dbc2852128b2a4b380af70c0f0e6a9b74b322e9f` |
| guardian | `769d93f0569b40168c995a5b89c69ce7` | `baf3582c177b907df01d3be257b2baabfa2004627d31a5ba8f755d9a27168fdd` |
| management | `1e39be3db7fd446d8afd5e1e32e53d2f` | `d24fa762909ec1fbc84e76c082e71a98ca8b113380f4ea5ed6c7db5a021bfaf2` |
| candidate (never launched) | `e8839e807f2d40469e0ece1fe398e94b` | `565df6ac9ed5678369e1a0984ca6f77091c1764d0c9b53d62f4a45ea66775f27` |
| original native (superseded before scenes) | `a1d05fc335444dc59e65686d4c7e91ca` | `32507e2ca7f6e01ec493e43ac37c8a0ef90e3951c85eae8a9d82102af1226e68` |

Original native source receipt: `2dd01e4f67a216ebb79803306afbcd193c4b881d0bbfc176e3da57c25928207e`.

Rebased native source receipt: `bdd890bcac4ccd8706c4c30a8c6fc18ebcbeb859001968b718069644b75c4a26`.

Management source receipt: `2978f67dd873f7cd41eabe272618f8aa154bb9db61f1a43509060bbe67416852`.

### Helpers and dry guard

Copied nr11 helpers to nr12 names/current worktree; originals untouched. Fixed
admission classifications retained candidate/fixture/guardian/Target018/run_python/
ui_helper; excludes self/live ancestors/exact trusted parent, with unchanged fixed
`admission_match class=... self_or_ancestor=... trusted_parent=...` refusal lines.
Same bash → Python → pwsh -NoProfile -File chain ran the guard dry twice before
builds, then twice after rebase. DryRun returns before clip.exe. Both pairs:

nr12-dry-admission.txt:
```json
{"attempt": 1, "exitCode": 0, "stdout": ["admission_dry_pass=1"], "stderrEmpty": true, "reason": "none"}
{"attempt": 2, "exitCode": 0, "stdout": ["admission_dry_pass=1"], "stderrEmpty": true, "reason": "none"}
```

nr12-dry-admission-rebased.txt:
```json
{"attempt": 1, "exitCode": 0, "stdout": ["admission_dry_pass=1"], "stderrEmpty": true, "reason": "none"}
{"attempt": 2, "exitCode": 0, "stdout": ["admission_dry_pass=1"], "stderrEmpty": true, "reason": "none"}
```

Eleven initial Python helpers passed ast.parse; all initial PowerShell copies
parsed and helper hashes retained. Picker copies came from current tracked UI
runner/helper, preserving all epoch/operationClass/pickerStep diagnostics. Only
REPO/helper paths, trial UI gate and an explicit **throw 'actions'** immediately
after `WaitPhase 'ui-import-cancelled'` differ. This stop precedes Import Commit;
there is no undefined Fail call. No helper execution was used to relax a guard.

## Commands actually executed

`<scratchpad>` is the private brief/report directory. Builds were supervised by
nr12-build-phase0.ps1; projects ran sequentially.

```text
python -I -B <scratchpad>/nr12-dry-admission.py
pwsh -NoProfile -File <scratchpad>/nr12-process-check.ps1
pwsh -NoProfile -File <scratchpad>/nr12-parse-helpers.ps1
# ast.parse all 11 initial scratch .py helpers
pwsh -NoProfile -File scripts/uno_candidate_native_fixture/build.ps1 -BuildOnly
pwsh -NoProfile -File <scratchpad>/nr12-build-support.ps1
pwsh -NoProfile -File scripts/verify-uno-candidate.ps1 -BuildOnly
pwsh -NoProfile -File scripts/publish-uno-candidate-aot.ps1
python -I -B <scratchpad>/nr12-artifacts.py
# PHASE0-READY, filesystem-only polling and time admission:
python -I -B <scratchpad>/nr12-wait-grant.py
python -I -B <scratchpad>/nr12-admit-window.py
# Close flags, confirm empty trial-source diff:
git merge --ff-only d78b368
git diff --stat 44c0169 d78b368
# Reopen same flags; save rebased exact diff:
pwsh -NoProfile -File scripts/uno_candidate_native_fixture/build.ps1 -BuildOnly
python -I -B <scratchpad>/nr12-dry-admission.py
python -I -B <scratchpad>/nr12-update-manifest.py
# PHASE0-READY nr-12 (rebased d78b368), before 19:20:
python -I -B <scratchpad>/nr12-preflight.py && python -I -B <scratchpad>/nr12-run.py
# Actual parent invokes guarded seed, native runner, last picker once.
# Close nine flags and copied UI gate; grep false; empty trial-source diff:
pwsh -NoProfile -File <scratchpad>/nr12-process-check.ps1
python -I -B <scratchpad>/nr12-clipboard-check.py
# SCREEN-DONE; filesystem-only thereafter:
python -I -B <scratchpad>/nr12-evidence-check.py ss-uno018-ui-529ec4abcf656fd228ac7ee5987af7d1
python -I -B <scratchpad>/nr12-document.py
```

## Per-phase ledger

| Step | Result | Independent boundary |
|---|---|---|
| Phase 0 original + rebase | PASS | fresh strict artifacts; hashes; both pairs of dry guards |
| Held-input preflight | PASS | first sample no held keys; downstream quiet guard unchanged |
| Phase 1 restore → abort → owner-refusal | PASS each | 0 rows / 0 usage; recovery settled |
| Phase 2 posted-row | PASS | 1 row / usage 1; clipboard epoch changed; combined PASS |
| Phase 2 posted-main | FAIL | usage 0 FAIL; clipboard epoch unchanged FAIL |
| Phase 2 popup-copy, editor-copy, footer-copy | FAIL each | usage 0 PASS only; unchanged Copy epoch FAIL |
| Phase 2 context-insert | FAIL | usage 0 FAIL; unchanged epoch FAIL |
| posted-enter | NOT RUN | input synthesis excluded (D50) |
| registered-* | NOT RUN | excluded by grant |
| Phase 3 Import Cancel attempt | FAIL | input epoch refusal; 0 rows / 0 usage; clipboard epoch unchanged |
| Other picker phases | NOT RUN | outside scope; explicit helper stop |
| Release | PASS | flags closed; zero processes; synthetic baseline verified |

Abort intentionally terminated only owned worker/target Job after publication;
independent guardian survived and settled. It is not graceful worker shutdown.
Owner-refusal tested same-PID owned guardian HWND change, not foreign clipboard
publication. Posted-row passed exact A/CRLF readback, empty B, one capture/paste,
duplicate Busy/no-op and persisted usage once, with actual host shutdown. The five
failed positives all stopped in preparation at guardian_ready / ForegroundRefused,
before target foreground grant or intended consumer; no Copy or editor Save-failure
trigger was reached. Zero Copy usage alone is not Copy acceptance.

## Diagnostic finding and limits

**All five failed-positive independent monitors sampled `system_0`,
`class_sha8=fd2fa13c`, `ws_child=true`**, visible, same_thread_as_a=true,
no owner/toolwindow/noactivate/cloak. The fixed class table maps `system_0` to
Edit (research 053's fingerprint), not an unidentified new top-level class.
The refusal remains `foreground_foreign`, relation=`same_pid_other`, closed=false.
The coarse `kind=toplevel_other` is merely the old fallback classification; the
new child-style facts mean it must **not** be described as proven top-level.
Neither pinned A nor B matched the independent monitor's failed sample.
No exact EditorA/EditorB identity or causal caller is recorded by this schema.

Every failed positive has complete ready/refusal inventories. At readiness: A,
B, hidden ime_default. At refusal: the same three correlation IDs plus **one NEW
hidden msctfime_ui / 4f47bb8d**. EnumThreadWindows does not enumerate child
windows: absence of the Edit-class sample from these top-level inventories is
not evidence of a disappearing/new top-level foreground window, nor proof that
the new hidden msctfime_ui was foreground or caused the refusal.

| Scene | NEW at refusal window_id | class_token / class_sha8 |
|---|---|---|
| posted-main | `1341435d` | `msctfime_ui` / `4f47bb8d` |
| popup-copy | `fe0bf834` | `msctfime_ui` / `4f47bb8d` |
| editor-copy | `965871b3` | `msctfime_ui` / `4f47bb8d` |
| footer-copy | `f7029607` | `msctfime_ui` / `4f47bb8d` |
| context-insert | `01ecccb4` | `msctfime_ui` / `4f47bb8d` |

These are independent, non-atomic sampled reads; inventories hash decimal HWNDs
for within-session correlation, not anonymization or identity authority. Handle
reuse/hash collisions and between-sample transitions limit inference. A new entry
is a lead, not creation-origin proof. `sampled` means A/B known, not that every
probe succeeded; unavailable rect/monitor booleans are not negative facts. Original
refusal is latched; no exact-A relaxation, same-PID exception, focus retry, delay
workaround or epoch rebaseline follows. This run does not retroactively identify
nr-11b's class=other sample as this run's Edit-class observation.

### Trace ordering

There were 98 full trace lines across six positives. Successful posted-row's
first sample was the same fixed Edit-class/WS_CHILD category at 0ms, followed
by owned_a at 29–242ms; preparation and the target grant then passed. The five
failures sampled owned_a first, then the Edit-class category through trace end:

| Scene | First Edit-class trace order / elapsed | Final trace order / elapsed | Monitor sampleOrder |
|---|---|---|---|
| posted-main | 3 / 32ms | 17 / 248ms | 44 |
| popup-copy | 2 / 21ms | 16 / 237ms | 104 |
| editor-copy | 2 / 29ms | 16 / 243ms | 48 |
| footer-copy | 2 / 20ms | 16 / 235ms | 25 |
| context-insert | 3 / 31ms | 17 / 245ms | 24 |

This temporal ordering is not continuous identity or proof of activation origin.
The successful transient is not a failed scene and does not justify a settling retry.

## Exact per-scene records

All nine native streams: privacy CLEAN; planted negative controls PASS; stderr
empty; no pending clipboard/keyboard receipt. Full startup/trace/stage lines below
are copied verbatim, not summarized/reformatted. Trace emission is buffered; use
elapsed_ms, not emission timestamps. Monitor and worker sample counters are
independent. FailureStage is last entered, not an exact predicate locator.

### restore — PASS

Session: `ss-uno018-ui-f46a7fc36b3c012440f25e4e7bcb7b2d/ss-uno019-native-d4d90f53dcd2499989ba34f05b8ce676`.

FailureStage: `none`. Target foreground grant: **absent** (no result line).

Rows/usage: 0/0; independent usage **PASS**.

Independent clipboard epoch: not applicable to control positive-boundary predicate.

Receipt/host check: **PASS**; host shutdown-complete true.

Monitor failure: absent.

```text
2026-11-02T01:14:57.301Z INFO native_startup_foreground phase=target_launch relation=foreign launcher_foreground=False sample_order=1 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=False cloaked=False cloaked_known=True class_sha8=827664e8 ws_popup=False ws_caption=True ws_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=False title_nonempty=True
2026-11-02T01:14:57.302Z INFO native_startup_foreground phase=host_ready relation=foreign launcher_foreground=False sample_order=2 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=False cloaked=False cloaked_known=True class_sha8=827664e8 ws_popup=False ws_caption=True ws_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=True
2026-11-02T01:14:57.302Z INFO native_host_readiness reason=ready
2026-11-02T01:14:58.471Z INFO native_control_admission reason=Ready
2026-11-02T01:14:58.594Z INFO native_control_publish status=swapped
```

target-thread-windows-ready.json, verbatim:
```json
{"phase":"ready","complete":true,"windows":[{"window_id":"4f8c7137","is_a":false,"is_b":true,"new_at_refusal":false,"facts":{"class_token":"target018_class","visible":true,"toolwindow":false,"noactivate":false,"has_owner":false,"owner_is_a":false,"owner_is_b":false,"is_message_only":false,"same_thread_as_a":true,"cloaked":false,"cloaked_known":true,"class_sha8":"532b2b53","ws_popup":false,"ws_caption":true,"ws_child":false,"ws_disabled":false,"ws_ex_topmost":false,"ws_ex_layered":false,"ws_ex_transparent":false,"ws_ex_appwindow":false,"zero_area":false,"offscreen":false,"equals_a_rect":false,"on_bottom_left_work":true,"title_nonempty":true,"sampled":true}},{"window_id":"6f33f14a","is_a":true,"is_b":false,"new_at_refusal":false,"facts":{"class_token":"target018_class","visible":true,"toolwindow":false,"noactivate":false,"has_owner":false,"owner_is_a":false,"owner_is_b":false,"is_message_only":false,"same_thread_as_a":true,"cloaked":false,"cloaked_known":true,"class_sha8":"532b2b53","ws_popup":false,"ws_caption":true,"ws_child":false,"ws_disabled":false,"ws_ex_topmost":false,"ws_ex_layered":false,"ws_ex_transparent":false,"ws_ex_appwindow":false,"zero_area":false,"offscreen":false,"equals_a_rect":true,"on_bottom_left_work":true,"title_nonempty":true,"sampled":true}},{"window_id":"526d33ef","is_a":false,"is_b":false,"new_at_refusal":false,"facts":{"class_token":"ime_default","visible":false,"toolwindow":false,"noactivate":false,"has_owner":true,"owner_is_a":true,"owner_is_b":false,"is_message_only":false,"same_thread_as_a":true,"cloaked":false,"cloaked_known":true,"class_sha8":"29ee788e","ws_popup":true,"ws_caption":false,"ws_child":false,"ws_disabled":true,"ws_ex_topmost":false,"ws_ex_layered":false,"ws_ex_transparent":false,"ws_ex_appwindow":false,"zero_area":true,"offscreen":true,"equals_a_rect":false,"on_bottom_left_work":false,"title_nonempty":true,"sampled":true}}]}
```

### abort — PASS

Session: `ss-uno018-ui-a6c1ab002c5ddd96c6f113801d736794/ss-uno019-native-c408df94da74421fbf296f66a0e64940`.

FailureStage: `none`. Target foreground grant: **absent** (no result line).

Rows/usage: 0/0; independent usage **PASS**.

Independent clipboard epoch: not applicable to control positive-boundary predicate.

Receipt/host check: **PASS**; host shutdown-complete false.

Monitor failure: absent.

```text
2026-11-02T01:15:04.037Z INFO native_startup_foreground phase=target_launch relation=foreign launcher_foreground=False sample_order=1 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=False cloaked=False cloaked_known=True class_sha8=827664e8 ws_popup=False ws_caption=True ws_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=False title_nonempty=True
2026-11-02T01:15:04.037Z INFO native_startup_foreground phase=host_ready relation=foreign launcher_foreground=False sample_order=2 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=False cloaked=False cloaked_known=True class_sha8=827664e8 ws_popup=False ws_caption=True ws_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=True
2026-11-02T01:15:04.037Z INFO native_host_readiness reason=ready
2026-11-02T01:15:04.411Z INFO native_control_admission reason=Ready
2026-11-02T01:15:04.537Z INFO native_control_publish status=swapped
```

target-thread-windows-ready.json, verbatim:
```json
{"phase":"ready","complete":true,"windows":[{"window_id":"cba48610","is_a":false,"is_b":true,"new_at_refusal":false,"facts":{"class_token":"target018_class","visible":true,"toolwindow":false,"noactivate":false,"has_owner":false,"owner_is_a":false,"owner_is_b":false,"is_message_only":false,"same_thread_as_a":true,"cloaked":false,"cloaked_known":true,"class_sha8":"cf8ea3d0","ws_popup":false,"ws_caption":true,"ws_child":false,"ws_disabled":false,"ws_ex_topmost":false,"ws_ex_layered":false,"ws_ex_transparent":false,"ws_ex_appwindow":false,"zero_area":false,"offscreen":false,"equals_a_rect":false,"on_bottom_left_work":true,"title_nonempty":true,"sampled":true}},{"window_id":"fc6df5fb","is_a":true,"is_b":false,"new_at_refusal":false,"facts":{"class_token":"target018_class","visible":true,"toolwindow":false,"noactivate":false,"has_owner":false,"owner_is_a":false,"owner_is_b":false,"is_message_only":false,"same_thread_as_a":true,"cloaked":false,"cloaked_known":true,"class_sha8":"cf8ea3d0","ws_popup":false,"ws_caption":true,"ws_child":false,"ws_disabled":false,"ws_ex_topmost":false,"ws_ex_layered":false,"ws_ex_transparent":false,"ws_ex_appwindow":false,"zero_area":false,"offscreen":false,"equals_a_rect":true,"on_bottom_left_work":true,"title_nonempty":true,"sampled":true}},{"window_id":"524295dc","is_a":false,"is_b":false,"new_at_refusal":false,"facts":{"class_token":"ime_default","visible":false,"toolwindow":false,"noactivate":false,"has_owner":true,"owner_is_a":true,"owner_is_b":false,"is_message_only":false,"same_thread_as_a":true,"cloaked":false,"cloaked_known":true,"class_sha8":"29ee788e","ws_popup":true,"ws_caption":false,"ws_child":false,"ws_disabled":true,"ws_ex_topmost":false,"ws_ex_layered":false,"ws_ex_transparent":false,"ws_ex_appwindow":false,"zero_area":true,"offscreen":true,"equals_a_rect":false,"on_bottom_left_work":false,"title_nonempty":true,"sampled":true}}]}
```

### owner-refusal — PASS

Session: `ss-uno018-ui-27d38830fa06bb23cca6190d69d7310e/ss-uno019-native-d5a1967c83ed467c9ed1ed5e14b99d22`.

FailureStage: `none`. Target foreground grant: **absent** (no result line).

Rows/usage: 0/0; independent usage **PASS**.

Independent clipboard epoch: not applicable to control positive-boundary predicate.

Receipt/host check: **PASS**; host shutdown-complete true.

Monitor failure: absent.

```text
2026-11-02T01:15:09.678Z INFO native_startup_foreground phase=target_launch relation=foreign launcher_foreground=False sample_order=1 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=False cloaked=False cloaked_known=True class_sha8=827664e8 ws_popup=False ws_caption=True ws_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=False title_nonempty=True
2026-11-02T01:15:09.678Z INFO native_startup_foreground phase=host_ready relation=foreign launcher_foreground=False sample_order=2 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=False cloaked=False cloaked_known=True class_sha8=827664e8 ws_popup=False ws_caption=True ws_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=True
2026-11-02T01:15:09.678Z INFO native_host_readiness reason=ready
2026-11-02T01:15:10.651Z INFO native_control_admission reason=Ready
2026-11-02T01:15:10.744Z INFO native_control_publish status=swapped
```

target-thread-windows-ready.json, verbatim:
```json
{"phase":"ready","complete":true,"windows":[{"window_id":"7dfe25ff","is_a":false,"is_b":true,"new_at_refusal":false,"facts":{"class_token":"target018_class","visible":true,"toolwindow":false,"noactivate":false,"has_owner":false,"owner_is_a":false,"owner_is_b":false,"is_message_only":false,"same_thread_as_a":true,"cloaked":false,"cloaked_known":true,"class_sha8":"68cca88f","ws_popup":false,"ws_caption":true,"ws_child":false,"ws_disabled":false,"ws_ex_topmost":false,"ws_ex_layered":false,"ws_ex_transparent":false,"ws_ex_appwindow":false,"zero_area":false,"offscreen":false,"equals_a_rect":false,"on_bottom_left_work":true,"title_nonempty":true,"sampled":true}},{"window_id":"e59707cd","is_a":true,"is_b":false,"new_at_refusal":false,"facts":{"class_token":"target018_class","visible":true,"toolwindow":false,"noactivate":false,"has_owner":false,"owner_is_a":false,"owner_is_b":false,"is_message_only":false,"same_thread_as_a":true,"cloaked":false,"cloaked_known":true,"class_sha8":"68cca88f","ws_popup":false,"ws_caption":true,"ws_child":false,"ws_disabled":false,"ws_ex_topmost":false,"ws_ex_layered":false,"ws_ex_transparent":false,"ws_ex_appwindow":false,"zero_area":false,"offscreen":false,"equals_a_rect":true,"on_bottom_left_work":true,"title_nonempty":true,"sampled":true}},{"window_id":"f6ac9d7d","is_a":false,"is_b":false,"new_at_refusal":false,"facts":{"class_token":"ime_default","visible":false,"toolwindow":false,"noactivate":false,"has_owner":true,"owner_is_a":true,"owner_is_b":false,"is_message_only":false,"same_thread_as_a":true,"cloaked":false,"cloaked_known":true,"class_sha8":"29ee788e","ws_popup":true,"ws_caption":false,"ws_child":false,"ws_disabled":true,"ws_ex_topmost":false,"ws_ex_layered":false,"ws_ex_transparent":false,"ws_ex_appwindow":false,"zero_area":true,"offscreen":true,"equals_a_rect":false,"on_bottom_left_work":false,"title_nonempty":true,"sampled":true}}]}
```

### posted-row — PASS

Session: `ss-uno018-ui-e12941c59c158737405e141d659dd2d1/ss-uno019-native-c85f8d667ad04e46b27b6e38eed5135f`.

FailureStage: `none`. Target foreground grant: **granted**.

Rows/usage: 1/1; independent usage **PASS**.

Independent clipboard epoch: **changed**; combined boundary **PASS**.

Receipt/host check: **PASS**; host shutdown-complete true.

Monitor failure: absent.

```text
2026-11-02T01:15:18.619Z INFO native_startup_foreground phase=target_launch relation=foreign launcher_foreground=False sample_order=1 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=False cloaked=False cloaked_known=True class_sha8=827664e8 ws_popup=False ws_caption=True ws_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=False title_nonempty=True
2026-11-02T01:15:18.619Z INFO native_startup_foreground phase=host_ready relation=foreign launcher_foreground=False sample_order=2 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=False cloaked=False cloaked_known=True class_sha8=827664e8 ws_popup=False ws_caption=True ws_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=True
2026-11-02T01:15:18.619Z INFO native_host_readiness reason=ready
2026-11-02T01:15:18.803Z INFO owned_initial_focus phase=waiting
2026-11-02T01:15:18.804Z INFO native_startup_foreground phase=preparation_start relation=foreign launcher_foreground=False sample_order=3 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=False cloaked=False cloaked_known=True class_sha8=827664e8 ws_popup=False ws_caption=True ws_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=True
2026-11-02T01:15:18.843Z INFO owned_preparation_admission reason=ready
2026-11-02T01:15:18.978Z INFO native_startup_foreground phase=before_focus relation=foreign launcher_foreground=False sample_order=4 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=False cloaked=False cloaked_known=True class_sha8=827664e8 ws_popup=False ws_caption=True ws_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=True
2026-11-02T01:15:18.980Z INFO native_startup_foreground phase=after_focus relation=none launcher_foreground=False sample_order=5 class_token=other visible=False toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=False cloaked=False cloaked_known=False class_sha8=e3b0c442 ws_popup=False ws_caption=False ws_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=False title_nonempty=False
2026-11-02T01:15:19.239Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=1 elapsed_ms=0 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:15:19.239Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=2 elapsed_ms=29 class_token=target018_class visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=d1437a32 ws_popup=False ws_caption=True ws_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=True on_bottom_left_work=True title_nonempty=True
2026-11-02T01:15:19.239Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=3 elapsed_ms=44 class_token=target018_class visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=d1437a32 ws_popup=False ws_caption=True ws_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=True on_bottom_left_work=True title_nonempty=True
2026-11-02T01:15:19.239Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=4 elapsed_ms=60 class_token=target018_class visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=d1437a32 ws_popup=False ws_caption=True ws_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=True on_bottom_left_work=True title_nonempty=True
2026-11-02T01:15:19.239Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=5 elapsed_ms=75 class_token=target018_class visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=d1437a32 ws_popup=False ws_caption=True ws_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=True on_bottom_left_work=True title_nonempty=True
2026-11-02T01:15:19.239Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=6 elapsed_ms=90 class_token=target018_class visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=d1437a32 ws_popup=False ws_caption=True ws_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=True on_bottom_left_work=True title_nonempty=True
2026-11-02T01:15:19.239Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=7 elapsed_ms=104 class_token=target018_class visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=d1437a32 ws_popup=False ws_caption=True ws_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=True on_bottom_left_work=True title_nonempty=True
2026-11-02T01:15:19.240Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=8 elapsed_ms=120 class_token=target018_class visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=d1437a32 ws_popup=False ws_caption=True ws_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=True on_bottom_left_work=True title_nonempty=True
2026-11-02T01:15:19.240Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=9 elapsed_ms=135 class_token=target018_class visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=d1437a32 ws_popup=False ws_caption=True ws_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=True on_bottom_left_work=True title_nonempty=True
2026-11-02T01:15:19.240Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=10 elapsed_ms=150 class_token=target018_class visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=d1437a32 ws_popup=False ws_caption=True ws_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=True on_bottom_left_work=True title_nonempty=True
2026-11-02T01:15:19.240Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=11 elapsed_ms=166 class_token=target018_class visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=d1437a32 ws_popup=False ws_caption=True ws_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=True on_bottom_left_work=True title_nonempty=True
2026-11-02T01:15:19.240Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=12 elapsed_ms=181 class_token=target018_class visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=d1437a32 ws_popup=False ws_caption=True ws_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=True on_bottom_left_work=True title_nonempty=True
2026-11-02T01:15:19.240Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=13 elapsed_ms=197 class_token=target018_class visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=d1437a32 ws_popup=False ws_caption=True ws_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=True on_bottom_left_work=True title_nonempty=True
2026-11-02T01:15:19.241Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=14 elapsed_ms=212 class_token=target018_class visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=d1437a32 ws_popup=False ws_caption=True ws_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=True on_bottom_left_work=True title_nonempty=True
2026-11-02T01:15:19.241Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=15 elapsed_ms=227 class_token=target018_class visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=d1437a32 ws_popup=False ws_caption=True ws_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=True on_bottom_left_work=True title_nonempty=True
2026-11-02T01:15:19.241Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=16 elapsed_ms=242 class_token=target018_class visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=d1437a32 ws_popup=False ws_caption=True ws_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=True on_bottom_left_work=True title_nonempty=True
2026-11-02T01:15:19.416Z INFO owned_target_preparation reason=Prepared
2026-11-02T01:15:19.435Z INFO owned_initial_focus phase=ready
2026-11-02T01:15:19.472Z INFO target_foreground_grant result=granted
2026-11-02T01:15:20.191Z INFO native_popup_focus reason=foreground_accepted
2026-11-02T01:15:20.191Z INFO candidate_popup_focus reason=accepted
2026-11-02T01:15:20.780Z INFO clipboard_swap attempts=1 status=Swapped saved=4 skipped_by_design=0 unsaved=0 cap_exceeded=False bytes=104 ms=171
2026-11-02T01:15:21.364Z INFO clipboard_restore status=Restored attempts=1
2026-11-02T01:15:21.364Z INFO insert_result outcome=Inserted id=1 notice=False ms=1118
```

target-thread-windows-ready.json, verbatim:
```json
{"phase":"ready","complete":true,"windows":[{"window_id":"2fc4b87a","is_a":false,"is_b":true,"new_at_refusal":false,"facts":{"class_token":"target018_class","visible":true,"toolwindow":false,"noactivate":false,"has_owner":false,"owner_is_a":false,"owner_is_b":false,"is_message_only":false,"same_thread_as_a":true,"cloaked":false,"cloaked_known":true,"class_sha8":"d1437a32","ws_popup":false,"ws_caption":true,"ws_child":false,"ws_disabled":false,"ws_ex_topmost":false,"ws_ex_layered":false,"ws_ex_transparent":false,"ws_ex_appwindow":false,"zero_area":false,"offscreen":false,"equals_a_rect":false,"on_bottom_left_work":true,"title_nonempty":true,"sampled":true}},{"window_id":"48c5aec3","is_a":true,"is_b":false,"new_at_refusal":false,"facts":{"class_token":"target018_class","visible":true,"toolwindow":false,"noactivate":false,"has_owner":false,"owner_is_a":false,"owner_is_b":false,"is_message_only":false,"same_thread_as_a":true,"cloaked":false,"cloaked_known":true,"class_sha8":"d1437a32","ws_popup":false,"ws_caption":true,"ws_child":false,"ws_disabled":false,"ws_ex_topmost":false,"ws_ex_layered":false,"ws_ex_transparent":false,"ws_ex_appwindow":false,"zero_area":false,"offscreen":false,"equals_a_rect":true,"on_bottom_left_work":true,"title_nonempty":true,"sampled":true}},{"window_id":"689355a1","is_a":false,"is_b":false,"new_at_refusal":false,"facts":{"class_token":"ime_default","visible":false,"toolwindow":false,"noactivate":false,"has_owner":true,"owner_is_a":true,"owner_is_b":false,"is_message_only":false,"same_thread_as_a":true,"cloaked":false,"cloaked_known":true,"class_sha8":"29ee788e","ws_popup":true,"ws_caption":false,"ws_child":false,"ws_disabled":true,"ws_ex_topmost":false,"ws_ex_layered":false,"ws_ex_transparent":false,"ws_ex_appwindow":false,"zero_area":true,"offscreen":true,"equals_a_rect":false,"on_bottom_left_work":false,"title_nonempty":true,"sampled":true}}]}
```

### posted-main — FAIL

Session: `ss-uno018-ui-762f3640a9319880488423efb3389050/ss-uno019-native-e4466be64df3406faa11b870fc91f4a3`.

FailureStage: `guardian_ready`. Target foreground grant: **absent** (no result line).

Rows/usage: 1/0; independent usage **FAIL**.

Independent clipboard epoch: **unchanged**; combined boundary **FAIL**.

Receipt/host check: **FAIL_ValueError**; host shutdown-complete false.

Full native-monitor-failure.json, verbatim:
```json
{"predicate": "foreground_foreign", "exitCode": 2, "sampleOrder": 44, "relation": "same_pid_other", "closed": false, "windowFacts": {"class_token": "system_0", "sampled": true, "visible": true, "toolwindow": false, "noactivate": false, "has_owner": false, "owner_is_a": false, "owner_is_b": false, "is_message_only": false, "same_thread_as_a": true, "cloaked": false, "cloaked_known": true, "class_sha8": "fd2fa13c", "ws_popup": false, "ws_caption": false, "ws_child": true, "ws_disabled": false, "ws_ex_topmost": false, "ws_ex_layered": false, "ws_ex_transparent": false, "ws_ex_appwindow": false, "zero_area": false, "offscreen": false, "equals_a_rect": false, "on_bottom_left_work": true, "title_nonempty": false}}
```

```text
2026-11-02T01:15:27.509Z INFO native_startup_foreground phase=target_launch relation=foreign launcher_foreground=False sample_order=1 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=False cloaked=False cloaked_known=True class_sha8=b1b23522 ws_popup=False ws_caption=True ws_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=True zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=False title_nonempty=True
2026-11-02T01:15:27.509Z INFO native_startup_foreground phase=host_ready relation=foreign launcher_foreground=False sample_order=2 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=False cloaked=False cloaked_known=True class_sha8=b1b23522 ws_popup=False ws_caption=True ws_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=True zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=True
2026-11-02T01:15:27.509Z INFO native_host_readiness reason=ready
2026-11-02T01:15:27.770Z INFO owned_initial_focus phase=waiting
2026-11-02T01:15:27.771Z INFO native_startup_foreground phase=preparation_start relation=foreign launcher_foreground=False sample_order=3 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=False cloaked=False cloaked_known=True class_sha8=b1b23522 ws_popup=False ws_caption=True ws_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=True zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=True
2026-11-02T01:15:28.790Z INFO owned_preparation_admission reason=ready
2026-11-02T01:15:28.916Z INFO native_startup_foreground phase=before_focus relation=foreign launcher_foreground=False sample_order=4 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=False cloaked=False cloaked_known=True class_sha8=b1b23522 ws_popup=False ws_caption=True ws_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=True zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=True
2026-11-02T01:15:28.947Z INFO native_startup_foreground phase=after_focus relation=owned_a launcher_foreground=False sample_order=5 class_token=target018_class visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=ac28b73d ws_popup=False ws_caption=True ws_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=True on_bottom_left_work=True title_nonempty=True
2026-11-02T01:15:29.212Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=1 elapsed_ms=0 class_token=target018_class visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=ac28b73d ws_popup=False ws_caption=True ws_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=True on_bottom_left_work=True title_nonempty=True
2026-11-02T01:15:29.212Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=2 elapsed_ms=17 class_token=target018_class visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=ac28b73d ws_popup=False ws_caption=True ws_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=True on_bottom_left_work=True title_nonempty=True
2026-11-02T01:15:29.213Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=3 elapsed_ms=32 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:15:29.213Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=4 elapsed_ms=48 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:15:29.213Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=5 elapsed_ms=63 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:15:29.213Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=6 elapsed_ms=78 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:15:29.213Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=7 elapsed_ms=94 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:15:29.214Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=8 elapsed_ms=110 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:15:29.214Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=9 elapsed_ms=125 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:15:29.214Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=10 elapsed_ms=140 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:15:29.214Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=11 elapsed_ms=155 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:15:29.214Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=12 elapsed_ms=171 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:15:29.214Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=13 elapsed_ms=187 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:15:29.214Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=14 elapsed_ms=202 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:15:29.215Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=15 elapsed_ms=218 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:15:29.215Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=16 elapsed_ms=233 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:15:29.215Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=17 elapsed_ms=248 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:15:29.216Z INFO owned_target_preparation reason=ForegroundRefused
2026-11-02T01:15:29.216Z INFO owned_initial_focus phase=refused
2026-11-02T01:15:29.216Z ERROR native_positive_failed stage=guardian_ready InvalidOperationException (HResult=0x80131509)
```

NEW at refusal: `1341435d`. Other entries retain ready IDs.

target-thread-windows-ready.json, verbatim:
```json
{"phase":"ready","complete":true,"windows":[{"window_id":"7a31b0c9","is_a":false,"is_b":true,"new_at_refusal":false,"facts":{"class_token":"target018_class","visible":true,"toolwindow":false,"noactivate":false,"has_owner":false,"owner_is_a":false,"owner_is_b":false,"is_message_only":false,"same_thread_as_a":true,"cloaked":false,"cloaked_known":true,"class_sha8":"ac28b73d","ws_popup":false,"ws_caption":true,"ws_child":false,"ws_disabled":false,"ws_ex_topmost":false,"ws_ex_layered":false,"ws_ex_transparent":false,"ws_ex_appwindow":false,"zero_area":false,"offscreen":false,"equals_a_rect":false,"on_bottom_left_work":true,"title_nonempty":true,"sampled":true}},{"window_id":"b36b1824","is_a":true,"is_b":false,"new_at_refusal":false,"facts":{"class_token":"target018_class","visible":true,"toolwindow":false,"noactivate":false,"has_owner":false,"owner_is_a":false,"owner_is_b":false,"is_message_only":false,"same_thread_as_a":true,"cloaked":false,"cloaked_known":true,"class_sha8":"ac28b73d","ws_popup":false,"ws_caption":true,"ws_child":false,"ws_disabled":false,"ws_ex_topmost":false,"ws_ex_layered":false,"ws_ex_transparent":false,"ws_ex_appwindow":false,"zero_area":false,"offscreen":false,"equals_a_rect":true,"on_bottom_left_work":true,"title_nonempty":true,"sampled":true}},{"window_id":"03890ab8","is_a":false,"is_b":false,"new_at_refusal":false,"facts":{"class_token":"ime_default","visible":false,"toolwindow":false,"noactivate":false,"has_owner":true,"owner_is_a":true,"owner_is_b":false,"is_message_only":false,"same_thread_as_a":true,"cloaked":false,"cloaked_known":true,"class_sha8":"29ee788e","ws_popup":true,"ws_caption":false,"ws_child":false,"ws_disabled":true,"ws_ex_topmost":false,"ws_ex_layered":false,"ws_ex_transparent":false,"ws_ex_appwindow":false,"zero_area":true,"offscreen":true,"equals_a_rect":false,"on_bottom_left_work":false,"title_nonempty":true,"sampled":true}}]}
```

target-thread-windows-refusal.json, verbatim:
```json
{"phase": "refusal", "complete": true, "windows": [{"window_id": "b36b1824", "is_a": true, "is_b": false, "new_at_refusal": false, "facts": {"class_token": "target018_class", "sampled": true, "visible": true, "toolwindow": false, "noactivate": false, "has_owner": false, "owner_is_a": false, "owner_is_b": false, "is_message_only": false, "same_thread_as_a": true, "cloaked": false, "cloaked_known": true, "class_sha8": "ac28b73d", "ws_popup": false, "ws_caption": true, "ws_child": false, "ws_disabled": false, "ws_ex_topmost": false, "ws_ex_layered": false, "ws_ex_transparent": false, "ws_ex_appwindow": false, "zero_area": false, "offscreen": false, "equals_a_rect": true, "on_bottom_left_work": true, "title_nonempty": true}}, {"window_id": "7a31b0c9", "is_a": false, "is_b": true, "new_at_refusal": false, "facts": {"class_token": "target018_class", "sampled": true, "visible": true, "toolwindow": false, "noactivate": false, "has_owner": false, "owner_is_a": false, "owner_is_b": false, "is_message_only": false, "same_thread_as_a": true, "cloaked": false, "cloaked_known": true, "class_sha8": "ac28b73d", "ws_popup": false, "ws_caption": true, "ws_child": false, "ws_disabled": false, "ws_ex_topmost": false, "ws_ex_layered": false, "ws_ex_transparent": false, "ws_ex_appwindow": false, "zero_area": false, "offscreen": false, "equals_a_rect": false, "on_bottom_left_work": true, "title_nonempty": true}}, {"window_id": "1341435d", "is_a": false, "is_b": false, "new_at_refusal": true, "facts": {"class_token": "msctfime_ui", "sampled": true, "visible": false, "toolwindow": false, "noactivate": false, "has_owner": true, "owner_is_a": false, "owner_is_b": false, "is_message_only": false, "same_thread_as_a": true, "cloaked": false, "cloaked_known": true, "class_sha8": "4f47bb8d", "ws_popup": true, "ws_caption": false, "ws_child": false, "ws_disabled": true, "ws_ex_topmost": false, "ws_ex_layered": false, "ws_ex_transparent": false, "ws_ex_appwindow": false, "zero_area": true, "offscreen": true, "equals_a_rect": false, "on_bottom_left_work": false, "title_nonempty": true}}, {"window_id": "03890ab8", "is_a": false, "is_b": false, "new_at_refusal": false, "facts": {"class_token": "ime_default", "sampled": true, "visible": false, "toolwindow": false, "noactivate": false, "has_owner": true, "owner_is_a": true, "owner_is_b": false, "is_message_only": false, "same_thread_as_a": true, "cloaked": false, "cloaked_known": true, "class_sha8": "29ee788e", "ws_popup": true, "ws_caption": false, "ws_child": false, "ws_disabled": true, "ws_ex_topmost": false, "ws_ex_layered": false, "ws_ex_transparent": false, "ws_ex_appwindow": false, "zero_area": true, "offscreen": true, "equals_a_rect": false, "on_bottom_left_work": false, "title_nonempty": true}}]}
```

### popup-copy — FAIL

Session: `ss-uno018-ui-a05df1884ae53d8b39c5f620e10f681b/ss-uno019-native-7eebb2b22bfa48cab137bf9173b92dee`.

FailureStage: `guardian_ready`. Target foreground grant: **absent** (no result line).

Rows/usage: 1/0; independent usage **PASS**.

Independent clipboard epoch: **unchanged**; combined boundary **FAIL**.

Receipt/host check: **FAIL_ValueError**; host shutdown-complete false.

Full native-monitor-failure.json, verbatim:
```json
{"predicate": "foreground_foreign", "exitCode": 2, "sampleOrder": 104, "relation": "same_pid_other", "closed": false, "windowFacts": {"class_token": "system_0", "sampled": true, "visible": true, "toolwindow": false, "noactivate": false, "has_owner": false, "owner_is_a": false, "owner_is_b": false, "is_message_only": false, "same_thread_as_a": true, "cloaked": false, "cloaked_known": true, "class_sha8": "fd2fa13c", "ws_popup": false, "ws_caption": false, "ws_child": true, "ws_disabled": false, "ws_ex_topmost": false, "ws_ex_layered": false, "ws_ex_transparent": false, "ws_ex_appwindow": false, "zero_area": false, "offscreen": false, "equals_a_rect": false, "on_bottom_left_work": true, "title_nonempty": false}}
```

```text
2026-11-02T01:15:35.018Z INFO native_startup_foreground phase=target_launch relation=foreign launcher_foreground=False sample_order=1 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=False cloaked=False cloaked_known=True class_sha8=b1b23522 ws_popup=False ws_caption=True ws_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=True zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=False title_nonempty=True
2026-11-02T01:15:35.018Z INFO native_startup_foreground phase=host_ready relation=foreign launcher_foreground=False sample_order=2 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=False cloaked=False cloaked_known=True class_sha8=b1b23522 ws_popup=False ws_caption=True ws_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=True zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=True
2026-11-02T01:15:35.018Z INFO native_host_readiness reason=ready
2026-11-02T01:15:35.311Z INFO owned_initial_focus phase=waiting
2026-11-02T01:15:35.313Z INFO native_startup_foreground phase=preparation_start relation=foreign launcher_foreground=False sample_order=3 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=False cloaked=False cloaked_known=True class_sha8=b1b23522 ws_popup=False ws_caption=True ws_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=True zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=True
2026-11-02T01:15:37.116Z INFO owned_preparation_admission reason=ready
2026-11-02T01:15:37.239Z INFO native_startup_foreground phase=before_focus relation=foreign launcher_foreground=False sample_order=4 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=False cloaked=False cloaked_known=True class_sha8=b1b23522 ws_popup=False ws_caption=True ws_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=True zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=True
2026-11-02T01:15:39.270Z INFO native_startup_foreground phase=after_focus relation=owned_a launcher_foreground=False sample_order=5 class_token=target018_class visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=f078d874 ws_popup=False ws_caption=True ws_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=True on_bottom_left_work=True title_nonempty=True
2026-11-02T01:15:39.523Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=1 elapsed_ms=0 class_token=target018_class visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=f078d874 ws_popup=False ws_caption=True ws_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=True on_bottom_left_work=True title_nonempty=True
2026-11-02T01:15:39.523Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=2 elapsed_ms=21 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:15:39.523Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=3 elapsed_ms=36 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:15:39.523Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=4 elapsed_ms=51 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:15:39.524Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=5 elapsed_ms=67 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:15:39.524Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=6 elapsed_ms=82 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:15:39.524Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=7 elapsed_ms=98 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:15:39.524Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=8 elapsed_ms=114 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:15:39.524Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=9 elapsed_ms=130 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:15:39.525Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=10 elapsed_ms=146 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:15:39.525Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=11 elapsed_ms=162 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:15:39.525Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=12 elapsed_ms=177 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:15:39.525Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=13 elapsed_ms=192 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:15:39.525Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=14 elapsed_ms=207 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:15:39.525Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=15 elapsed_ms=222 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:15:39.526Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=16 elapsed_ms=237 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:15:39.526Z INFO owned_target_preparation reason=ForegroundRefused
2026-11-02T01:15:39.527Z INFO owned_initial_focus phase=refused
2026-11-02T01:15:39.527Z ERROR native_positive_failed stage=guardian_ready InvalidOperationException (HResult=0x80131509)
```

NEW at refusal: `fe0bf834`. Other entries retain ready IDs.

target-thread-windows-ready.json, verbatim:
```json
{"phase":"ready","complete":true,"windows":[{"window_id":"0c6acca7","is_a":false,"is_b":true,"new_at_refusal":false,"facts":{"class_token":"target018_class","visible":true,"toolwindow":false,"noactivate":false,"has_owner":false,"owner_is_a":false,"owner_is_b":false,"is_message_only":false,"same_thread_as_a":true,"cloaked":false,"cloaked_known":true,"class_sha8":"f078d874","ws_popup":false,"ws_caption":true,"ws_child":false,"ws_disabled":false,"ws_ex_topmost":false,"ws_ex_layered":false,"ws_ex_transparent":false,"ws_ex_appwindow":false,"zero_area":false,"offscreen":false,"equals_a_rect":false,"on_bottom_left_work":true,"title_nonempty":true,"sampled":true}},{"window_id":"50e09d2b","is_a":true,"is_b":false,"new_at_refusal":false,"facts":{"class_token":"target018_class","visible":true,"toolwindow":false,"noactivate":false,"has_owner":false,"owner_is_a":false,"owner_is_b":false,"is_message_only":false,"same_thread_as_a":true,"cloaked":false,"cloaked_known":true,"class_sha8":"f078d874","ws_popup":false,"ws_caption":true,"ws_child":false,"ws_disabled":false,"ws_ex_topmost":false,"ws_ex_layered":false,"ws_ex_transparent":false,"ws_ex_appwindow":false,"zero_area":false,"offscreen":false,"equals_a_rect":true,"on_bottom_left_work":true,"title_nonempty":true,"sampled":true}},{"window_id":"0662f749","is_a":false,"is_b":false,"new_at_refusal":false,"facts":{"class_token":"ime_default","visible":false,"toolwindow":false,"noactivate":false,"has_owner":true,"owner_is_a":true,"owner_is_b":false,"is_message_only":false,"same_thread_as_a":true,"cloaked":false,"cloaked_known":true,"class_sha8":"29ee788e","ws_popup":true,"ws_caption":false,"ws_child":false,"ws_disabled":true,"ws_ex_topmost":false,"ws_ex_layered":false,"ws_ex_transparent":false,"ws_ex_appwindow":false,"zero_area":true,"offscreen":true,"equals_a_rect":false,"on_bottom_left_work":false,"title_nonempty":true,"sampled":true}}]}
```

target-thread-windows-refusal.json, verbatim:
```json
{"phase": "refusal", "complete": true, "windows": [{"window_id": "50e09d2b", "is_a": true, "is_b": false, "new_at_refusal": false, "facts": {"class_token": "target018_class", "sampled": true, "visible": true, "toolwindow": false, "noactivate": false, "has_owner": false, "owner_is_a": false, "owner_is_b": false, "is_message_only": false, "same_thread_as_a": true, "cloaked": false, "cloaked_known": true, "class_sha8": "f078d874", "ws_popup": false, "ws_caption": true, "ws_child": false, "ws_disabled": false, "ws_ex_topmost": false, "ws_ex_layered": false, "ws_ex_transparent": false, "ws_ex_appwindow": false, "zero_area": false, "offscreen": false, "equals_a_rect": true, "on_bottom_left_work": true, "title_nonempty": true}}, {"window_id": "0c6acca7", "is_a": false, "is_b": true, "new_at_refusal": false, "facts": {"class_token": "target018_class", "sampled": true, "visible": true, "toolwindow": false, "noactivate": false, "has_owner": false, "owner_is_a": false, "owner_is_b": false, "is_message_only": false, "same_thread_as_a": true, "cloaked": false, "cloaked_known": true, "class_sha8": "f078d874", "ws_popup": false, "ws_caption": true, "ws_child": false, "ws_disabled": false, "ws_ex_topmost": false, "ws_ex_layered": false, "ws_ex_transparent": false, "ws_ex_appwindow": false, "zero_area": false, "offscreen": false, "equals_a_rect": false, "on_bottom_left_work": true, "title_nonempty": true}}, {"window_id": "0662f749", "is_a": false, "is_b": false, "new_at_refusal": false, "facts": {"class_token": "ime_default", "sampled": true, "visible": false, "toolwindow": false, "noactivate": false, "has_owner": true, "owner_is_a": true, "owner_is_b": false, "is_message_only": false, "same_thread_as_a": true, "cloaked": false, "cloaked_known": true, "class_sha8": "29ee788e", "ws_popup": true, "ws_caption": false, "ws_child": false, "ws_disabled": true, "ws_ex_topmost": false, "ws_ex_layered": false, "ws_ex_transparent": false, "ws_ex_appwindow": false, "zero_area": true, "offscreen": true, "equals_a_rect": false, "on_bottom_left_work": false, "title_nonempty": true}}, {"window_id": "fe0bf834", "is_a": false, "is_b": false, "new_at_refusal": true, "facts": {"class_token": "msctfime_ui", "sampled": true, "visible": false, "toolwindow": false, "noactivate": false, "has_owner": true, "owner_is_a": false, "owner_is_b": false, "is_message_only": false, "same_thread_as_a": true, "cloaked": false, "cloaked_known": true, "class_sha8": "4f47bb8d", "ws_popup": true, "ws_caption": false, "ws_child": false, "ws_disabled": true, "ws_ex_topmost": false, "ws_ex_layered": false, "ws_ex_transparent": false, "ws_ex_appwindow": false, "zero_area": true, "offscreen": true, "equals_a_rect": false, "on_bottom_left_work": false, "title_nonempty": true}}]}
```

### editor-copy — FAIL

Session: `ss-uno018-ui-5c9f93a8e10edfe4ddfc566f70f46def/ss-uno019-native-4dc79231e3fe45508940f9bc4ed2e6de`.

FailureStage: `guardian_ready`. Target foreground grant: **absent** (no result line).

Rows/usage: 1/0; independent usage **PASS**.

Independent clipboard epoch: **unchanged**; combined boundary **FAIL**.

Receipt/host check: **FAIL_ValueError**; host shutdown-complete false.

Full native-monitor-failure.json, verbatim:
```json
{"predicate": "foreground_foreign", "exitCode": 2, "sampleOrder": 48, "relation": "same_pid_other", "closed": false, "windowFacts": {"class_token": "system_0", "sampled": true, "visible": true, "toolwindow": false, "noactivate": false, "has_owner": false, "owner_is_a": false, "owner_is_b": false, "is_message_only": false, "same_thread_as_a": true, "cloaked": false, "cloaked_known": true, "class_sha8": "fd2fa13c", "ws_popup": false, "ws_caption": false, "ws_child": true, "ws_disabled": false, "ws_ex_topmost": false, "ws_ex_layered": false, "ws_ex_transparent": false, "ws_ex_appwindow": false, "zero_area": false, "offscreen": false, "equals_a_rect": false, "on_bottom_left_work": true, "title_nonempty": false}}
```

```text
2026-11-02T01:15:45.104Z INFO native_startup_foreground phase=target_launch relation=foreign launcher_foreground=False sample_order=1 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=False cloaked=False cloaked_known=True class_sha8=b1b23522 ws_popup=False ws_caption=True ws_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=True zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=False title_nonempty=True
2026-11-02T01:15:45.104Z INFO native_startup_foreground phase=host_ready relation=foreign launcher_foreground=False sample_order=2 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=False cloaked=False cloaked_known=True class_sha8=b1b23522 ws_popup=False ws_caption=True ws_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=True zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=True
2026-11-02T01:15:45.104Z INFO native_host_readiness reason=ready
2026-11-02T01:15:45.531Z INFO owned_initial_focus phase=waiting
2026-11-02T01:15:45.532Z INFO native_startup_foreground phase=preparation_start relation=foreign launcher_foreground=False sample_order=3 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=False cloaked=False cloaked_known=True class_sha8=b1b23522 ws_popup=False ws_caption=True ws_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=True zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=True
2026-11-02T01:15:46.356Z INFO owned_preparation_admission reason=ready
2026-11-02T01:15:46.485Z INFO native_startup_foreground phase=before_focus relation=foreign launcher_foreground=False sample_order=4 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=False cloaked=False cloaked_known=True class_sha8=b1b23522 ws_popup=False ws_caption=True ws_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=True zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=True
2026-11-02T01:15:46.520Z INFO native_startup_foreground phase=after_focus relation=owned_a launcher_foreground=False sample_order=5 class_token=target018_class visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=3a76957f ws_popup=False ws_caption=True ws_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=True on_bottom_left_work=True title_nonempty=True
2026-11-02T01:15:46.779Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=1 elapsed_ms=0 class_token=target018_class visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=3a76957f ws_popup=False ws_caption=True ws_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=True on_bottom_left_work=True title_nonempty=True
2026-11-02T01:15:46.779Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=2 elapsed_ms=29 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:15:46.779Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=3 elapsed_ms=45 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:15:46.779Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=4 elapsed_ms=60 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:15:46.779Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=5 elapsed_ms=75 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:15:46.779Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=6 elapsed_ms=91 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:15:46.779Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=7 elapsed_ms=107 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:15:46.780Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=8 elapsed_ms=121 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:15:46.780Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=9 elapsed_ms=137 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:15:46.780Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=10 elapsed_ms=151 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:15:46.780Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=11 elapsed_ms=166 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:15:46.780Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=12 elapsed_ms=181 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:15:46.780Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=13 elapsed_ms=197 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:15:46.781Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=14 elapsed_ms=213 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:15:46.781Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=15 elapsed_ms=228 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:15:46.781Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=16 elapsed_ms=243 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:15:46.782Z INFO owned_target_preparation reason=ForegroundRefused
2026-11-02T01:15:46.782Z INFO owned_initial_focus phase=refused
2026-11-02T01:15:46.782Z ERROR native_positive_failed stage=guardian_ready InvalidOperationException (HResult=0x80131509)
```

NEW at refusal: `965871b3`. Other entries retain ready IDs.

target-thread-windows-ready.json, verbatim:
```json
{"phase":"ready","complete":true,"windows":[{"window_id":"413877b1","is_a":false,"is_b":true,"new_at_refusal":false,"facts":{"class_token":"target018_class","visible":true,"toolwindow":false,"noactivate":false,"has_owner":false,"owner_is_a":false,"owner_is_b":false,"is_message_only":false,"same_thread_as_a":true,"cloaked":false,"cloaked_known":true,"class_sha8":"3a76957f","ws_popup":false,"ws_caption":true,"ws_child":false,"ws_disabled":false,"ws_ex_topmost":false,"ws_ex_layered":false,"ws_ex_transparent":false,"ws_ex_appwindow":false,"zero_area":false,"offscreen":false,"equals_a_rect":false,"on_bottom_left_work":true,"title_nonempty":true,"sampled":true}},{"window_id":"4eed1fa4","is_a":true,"is_b":false,"new_at_refusal":false,"facts":{"class_token":"target018_class","visible":true,"toolwindow":false,"noactivate":false,"has_owner":false,"owner_is_a":false,"owner_is_b":false,"is_message_only":false,"same_thread_as_a":true,"cloaked":false,"cloaked_known":true,"class_sha8":"3a76957f","ws_popup":false,"ws_caption":true,"ws_child":false,"ws_disabled":false,"ws_ex_topmost":false,"ws_ex_layered":false,"ws_ex_transparent":false,"ws_ex_appwindow":false,"zero_area":false,"offscreen":false,"equals_a_rect":true,"on_bottom_left_work":true,"title_nonempty":true,"sampled":true}},{"window_id":"e318aba7","is_a":false,"is_b":false,"new_at_refusal":false,"facts":{"class_token":"ime_default","visible":false,"toolwindow":false,"noactivate":false,"has_owner":true,"owner_is_a":true,"owner_is_b":false,"is_message_only":false,"same_thread_as_a":true,"cloaked":false,"cloaked_known":true,"class_sha8":"29ee788e","ws_popup":true,"ws_caption":false,"ws_child":false,"ws_disabled":true,"ws_ex_topmost":false,"ws_ex_layered":false,"ws_ex_transparent":false,"ws_ex_appwindow":false,"zero_area":true,"offscreen":true,"equals_a_rect":false,"on_bottom_left_work":false,"title_nonempty":true,"sampled":true}}]}
```

target-thread-windows-refusal.json, verbatim:
```json
{"phase": "refusal", "complete": true, "windows": [{"window_id": "4eed1fa4", "is_a": true, "is_b": false, "new_at_refusal": false, "facts": {"class_token": "target018_class", "sampled": true, "visible": true, "toolwindow": false, "noactivate": false, "has_owner": false, "owner_is_a": false, "owner_is_b": false, "is_message_only": false, "same_thread_as_a": true, "cloaked": false, "cloaked_known": true, "class_sha8": "3a76957f", "ws_popup": false, "ws_caption": true, "ws_child": false, "ws_disabled": false, "ws_ex_topmost": false, "ws_ex_layered": false, "ws_ex_transparent": false, "ws_ex_appwindow": false, "zero_area": false, "offscreen": false, "equals_a_rect": true, "on_bottom_left_work": true, "title_nonempty": true}}, {"window_id": "413877b1", "is_a": false, "is_b": true, "new_at_refusal": false, "facts": {"class_token": "target018_class", "sampled": true, "visible": true, "toolwindow": false, "noactivate": false, "has_owner": false, "owner_is_a": false, "owner_is_b": false, "is_message_only": false, "same_thread_as_a": true, "cloaked": false, "cloaked_known": true, "class_sha8": "3a76957f", "ws_popup": false, "ws_caption": true, "ws_child": false, "ws_disabled": false, "ws_ex_topmost": false, "ws_ex_layered": false, "ws_ex_transparent": false, "ws_ex_appwindow": false, "zero_area": false, "offscreen": false, "equals_a_rect": false, "on_bottom_left_work": true, "title_nonempty": true}}, {"window_id": "965871b3", "is_a": false, "is_b": false, "new_at_refusal": true, "facts": {"class_token": "msctfime_ui", "sampled": true, "visible": false, "toolwindow": false, "noactivate": false, "has_owner": true, "owner_is_a": false, "owner_is_b": false, "is_message_only": false, "same_thread_as_a": true, "cloaked": false, "cloaked_known": true, "class_sha8": "4f47bb8d", "ws_popup": true, "ws_caption": false, "ws_child": false, "ws_disabled": true, "ws_ex_topmost": false, "ws_ex_layered": false, "ws_ex_transparent": false, "ws_ex_appwindow": false, "zero_area": true, "offscreen": true, "equals_a_rect": false, "on_bottom_left_work": false, "title_nonempty": true}}, {"window_id": "e318aba7", "is_a": false, "is_b": false, "new_at_refusal": false, "facts": {"class_token": "ime_default", "sampled": true, "visible": false, "toolwindow": false, "noactivate": false, "has_owner": true, "owner_is_a": true, "owner_is_b": false, "is_message_only": false, "same_thread_as_a": true, "cloaked": false, "cloaked_known": true, "class_sha8": "29ee788e", "ws_popup": true, "ws_caption": false, "ws_child": false, "ws_disabled": true, "ws_ex_topmost": false, "ws_ex_layered": false, "ws_ex_transparent": false, "ws_ex_appwindow": false, "zero_area": true, "offscreen": true, "equals_a_rect": false, "on_bottom_left_work": false, "title_nonempty": true}}]}
```

### footer-copy — FAIL

Session: `ss-uno018-ui-9da4150fd48ee566fac5195a6773db6a/ss-uno019-native-02df302bfafb4309a1f3e704c4938e71`.

FailureStage: `guardian_ready`. Target foreground grant: **absent** (no result line).

Rows/usage: 1/0; independent usage **PASS**.

Independent clipboard epoch: **unchanged**; combined boundary **FAIL**.

Receipt/host check: **FAIL_ValueError**; host shutdown-complete false.

Full native-monitor-failure.json, verbatim:
```json
{"predicate": "foreground_foreign", "exitCode": 2, "sampleOrder": 25, "relation": "same_pid_other", "closed": false, "windowFacts": {"class_token": "system_0", "sampled": true, "visible": true, "toolwindow": false, "noactivate": false, "has_owner": false, "owner_is_a": false, "owner_is_b": false, "is_message_only": false, "same_thread_as_a": true, "cloaked": false, "cloaked_known": true, "class_sha8": "fd2fa13c", "ws_popup": false, "ws_caption": false, "ws_child": true, "ws_disabled": false, "ws_ex_topmost": false, "ws_ex_layered": false, "ws_ex_transparent": false, "ws_ex_appwindow": false, "zero_area": false, "offscreen": false, "equals_a_rect": false, "on_bottom_left_work": true, "title_nonempty": false}}
```

```text
2026-11-02T01:15:52.165Z INFO native_startup_foreground phase=target_launch relation=foreign launcher_foreground=False sample_order=1 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=False cloaked=False cloaked_known=True class_sha8=b1b23522 ws_popup=False ws_caption=True ws_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=True zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=False title_nonempty=True
2026-11-02T01:15:52.166Z INFO native_startup_foreground phase=host_ready relation=foreign launcher_foreground=False sample_order=2 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=False cloaked=False cloaked_known=True class_sha8=b1b23522 ws_popup=False ws_caption=True ws_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=True zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=True
2026-11-02T01:15:52.166Z INFO native_host_readiness reason=ready
2026-11-02T01:15:52.447Z INFO owned_initial_focus phase=waiting
2026-11-02T01:15:52.447Z INFO native_startup_foreground phase=preparation_start relation=foreign launcher_foreground=False sample_order=3 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=False cloaked=False cloaked_known=True class_sha8=b1b23522 ws_popup=False ws_caption=True ws_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=True zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=True
2026-11-02T01:15:52.486Z INFO owned_preparation_admission reason=ready
2026-11-02T01:15:52.618Z INFO native_startup_foreground phase=before_focus relation=foreign launcher_foreground=False sample_order=4 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=False cloaked=False cloaked_known=True class_sha8=b1b23522 ws_popup=False ws_caption=True ws_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=True zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=True
2026-11-02T01:15:52.650Z INFO native_startup_foreground phase=after_focus relation=owned_a launcher_foreground=False sample_order=5 class_token=target018_class visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=573bb93c ws_popup=False ws_caption=True ws_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=True on_bottom_left_work=True title_nonempty=True
2026-11-02T01:15:52.901Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=1 elapsed_ms=0 class_token=target018_class visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=573bb93c ws_popup=False ws_caption=True ws_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=True on_bottom_left_work=True title_nonempty=True
2026-11-02T01:15:52.901Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=2 elapsed_ms=20 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:15:52.902Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=3 elapsed_ms=35 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:15:52.902Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=4 elapsed_ms=50 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:15:52.902Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=5 elapsed_ms=65 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:15:52.902Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=6 elapsed_ms=81 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:15:52.902Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=7 elapsed_ms=96 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:15:52.902Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=8 elapsed_ms=112 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:15:52.902Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=9 elapsed_ms=128 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:15:52.902Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=10 elapsed_ms=144 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:15:52.903Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=11 elapsed_ms=159 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:15:52.903Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=12 elapsed_ms=174 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:15:52.903Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=13 elapsed_ms=189 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:15:52.903Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=14 elapsed_ms=204 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:15:52.903Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=15 elapsed_ms=219 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:15:52.903Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=16 elapsed_ms=235 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:15:52.904Z INFO owned_target_preparation reason=ForegroundRefused
2026-11-02T01:15:52.904Z INFO owned_initial_focus phase=refused
2026-11-02T01:15:52.904Z ERROR native_positive_failed stage=guardian_ready InvalidOperationException (HResult=0x80131509)
```

NEW at refusal: `f7029607`. Other entries retain ready IDs.

target-thread-windows-ready.json, verbatim:
```json
{"phase":"ready","complete":true,"windows":[{"window_id":"55bd6b06","is_a":false,"is_b":true,"new_at_refusal":false,"facts":{"class_token":"target018_class","visible":true,"toolwindow":false,"noactivate":false,"has_owner":false,"owner_is_a":false,"owner_is_b":false,"is_message_only":false,"same_thread_as_a":true,"cloaked":false,"cloaked_known":true,"class_sha8":"573bb93c","ws_popup":false,"ws_caption":true,"ws_child":false,"ws_disabled":false,"ws_ex_topmost":false,"ws_ex_layered":false,"ws_ex_transparent":false,"ws_ex_appwindow":false,"zero_area":false,"offscreen":false,"equals_a_rect":false,"on_bottom_left_work":true,"title_nonempty":true,"sampled":true}},{"window_id":"45e1fce0","is_a":true,"is_b":false,"new_at_refusal":false,"facts":{"class_token":"target018_class","visible":true,"toolwindow":false,"noactivate":false,"has_owner":false,"owner_is_a":false,"owner_is_b":false,"is_message_only":false,"same_thread_as_a":true,"cloaked":false,"cloaked_known":true,"class_sha8":"573bb93c","ws_popup":false,"ws_caption":true,"ws_child":false,"ws_disabled":false,"ws_ex_topmost":false,"ws_ex_layered":false,"ws_ex_transparent":false,"ws_ex_appwindow":false,"zero_area":false,"offscreen":false,"equals_a_rect":true,"on_bottom_left_work":true,"title_nonempty":true,"sampled":true}},{"window_id":"f385045f","is_a":false,"is_b":false,"new_at_refusal":false,"facts":{"class_token":"ime_default","visible":false,"toolwindow":false,"noactivate":false,"has_owner":true,"owner_is_a":true,"owner_is_b":false,"is_message_only":false,"same_thread_as_a":true,"cloaked":false,"cloaked_known":true,"class_sha8":"29ee788e","ws_popup":true,"ws_caption":false,"ws_child":false,"ws_disabled":true,"ws_ex_topmost":false,"ws_ex_layered":false,"ws_ex_transparent":false,"ws_ex_appwindow":false,"zero_area":true,"offscreen":true,"equals_a_rect":false,"on_bottom_left_work":false,"title_nonempty":true,"sampled":true}}]}
```

target-thread-windows-refusal.json, verbatim:
```json
{"phase": "refusal", "complete": true, "windows": [{"window_id": "45e1fce0", "is_a": true, "is_b": false, "new_at_refusal": false, "facts": {"class_token": "target018_class", "sampled": true, "visible": true, "toolwindow": false, "noactivate": false, "has_owner": false, "owner_is_a": false, "owner_is_b": false, "is_message_only": false, "same_thread_as_a": true, "cloaked": false, "cloaked_known": true, "class_sha8": "573bb93c", "ws_popup": false, "ws_caption": true, "ws_child": false, "ws_disabled": false, "ws_ex_topmost": false, "ws_ex_layered": false, "ws_ex_transparent": false, "ws_ex_appwindow": false, "zero_area": false, "offscreen": false, "equals_a_rect": true, "on_bottom_left_work": true, "title_nonempty": true}}, {"window_id": "55bd6b06", "is_a": false, "is_b": true, "new_at_refusal": false, "facts": {"class_token": "target018_class", "sampled": true, "visible": true, "toolwindow": false, "noactivate": false, "has_owner": false, "owner_is_a": false, "owner_is_b": false, "is_message_only": false, "same_thread_as_a": true, "cloaked": false, "cloaked_known": true, "class_sha8": "573bb93c", "ws_popup": false, "ws_caption": true, "ws_child": false, "ws_disabled": false, "ws_ex_topmost": false, "ws_ex_layered": false, "ws_ex_transparent": false, "ws_ex_appwindow": false, "zero_area": false, "offscreen": false, "equals_a_rect": false, "on_bottom_left_work": true, "title_nonempty": true}}, {"window_id": "f7029607", "is_a": false, "is_b": false, "new_at_refusal": true, "facts": {"class_token": "msctfime_ui", "sampled": true, "visible": false, "toolwindow": false, "noactivate": false, "has_owner": true, "owner_is_a": false, "owner_is_b": false, "is_message_only": false, "same_thread_as_a": true, "cloaked": false, "cloaked_known": true, "class_sha8": "4f47bb8d", "ws_popup": true, "ws_caption": false, "ws_child": false, "ws_disabled": true, "ws_ex_topmost": false, "ws_ex_layered": false, "ws_ex_transparent": false, "ws_ex_appwindow": false, "zero_area": true, "offscreen": true, "equals_a_rect": false, "on_bottom_left_work": false, "title_nonempty": true}}, {"window_id": "f385045f", "is_a": false, "is_b": false, "new_at_refusal": false, "facts": {"class_token": "ime_default", "sampled": true, "visible": false, "toolwindow": false, "noactivate": false, "has_owner": true, "owner_is_a": true, "owner_is_b": false, "is_message_only": false, "same_thread_as_a": true, "cloaked": false, "cloaked_known": true, "class_sha8": "29ee788e", "ws_popup": true, "ws_caption": false, "ws_child": false, "ws_disabled": true, "ws_ex_topmost": false, "ws_ex_layered": false, "ws_ex_transparent": false, "ws_ex_appwindow": false, "zero_area": true, "offscreen": true, "equals_a_rect": false, "on_bottom_left_work": false, "title_nonempty": true}}]}
```

### context-insert — FAIL

Session: `ss-uno018-ui-591428bb5191381294e48b90f064f443/ss-uno019-native-80938c73bbe44b939894d58fe32b7178`.

FailureStage: `guardian_ready`. Target foreground grant: **absent** (no result line).

Rows/usage: 1/0; independent usage **FAIL**.

Independent clipboard epoch: **unchanged**; combined boundary **FAIL**.

Receipt/host check: **FAIL_ValueError**; host shutdown-complete false.

Full native-monitor-failure.json, verbatim:
```json
{"predicate": "foreground_foreign", "exitCode": 2, "sampleOrder": 24, "relation": "same_pid_other", "closed": false, "windowFacts": {"class_token": "system_0", "sampled": true, "visible": true, "toolwindow": false, "noactivate": false, "has_owner": false, "owner_is_a": false, "owner_is_b": false, "is_message_only": false, "same_thread_as_a": true, "cloaked": false, "cloaked_known": true, "class_sha8": "fd2fa13c", "ws_popup": false, "ws_caption": false, "ws_child": true, "ws_disabled": false, "ws_ex_topmost": false, "ws_ex_layered": false, "ws_ex_transparent": false, "ws_ex_appwindow": false, "zero_area": false, "offscreen": false, "equals_a_rect": false, "on_bottom_left_work": true, "title_nonempty": false}}
```

```text
2026-11-02T01:15:58.377Z INFO native_startup_foreground phase=target_launch relation=foreign launcher_foreground=False sample_order=1 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=False cloaked=False cloaked_known=True class_sha8=b1b23522 ws_popup=False ws_caption=True ws_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=True zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=False title_nonempty=True
2026-11-02T01:15:58.378Z INFO native_startup_foreground phase=host_ready relation=foreign launcher_foreground=False sample_order=2 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=False cloaked=False cloaked_known=True class_sha8=b1b23522 ws_popup=False ws_caption=True ws_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=True zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=True
2026-11-02T01:15:58.378Z INFO native_host_readiness reason=ready
2026-11-02T01:15:58.554Z INFO owned_initial_focus phase=waiting
2026-11-02T01:15:58.555Z INFO native_startup_foreground phase=preparation_start relation=foreign launcher_foreground=False sample_order=3 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=False cloaked=False cloaked_known=True class_sha8=b1b23522 ws_popup=False ws_caption=True ws_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=True zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=True
2026-11-02T01:15:58.595Z INFO owned_preparation_admission reason=ready
2026-11-02T01:15:58.726Z INFO native_startup_foreground phase=before_focus relation=foreign launcher_foreground=False sample_order=4 class_token=other visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=False cloaked=False cloaked_known=True class_sha8=b1b23522 ws_popup=False ws_caption=True ws_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=True zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=True
2026-11-02T01:15:58.761Z INFO native_startup_foreground phase=after_focus relation=owned_a launcher_foreground=False sample_order=5 class_token=target018_class visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=251802fa ws_popup=False ws_caption=True ws_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=True on_bottom_left_work=True title_nonempty=True
2026-11-02T01:15:59.022Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=1 elapsed_ms=0 class_token=target018_class visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=251802fa ws_popup=False ws_caption=True ws_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=True on_bottom_left_work=True title_nonempty=True
2026-11-02T01:15:59.022Z INFO owned_foreground_trace relation=owned_a kind=not_applicable initial_foreground=False sample_order=2 elapsed_ms=16 class_token=target018_class visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=251802fa ws_popup=False ws_caption=True ws_child=False ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=True on_bottom_left_work=True title_nonempty=True
2026-11-02T01:15:59.022Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=3 elapsed_ms=31 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:15:59.022Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=4 elapsed_ms=46 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:15:59.022Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=5 elapsed_ms=61 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:15:59.022Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=6 elapsed_ms=77 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:15:59.023Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=7 elapsed_ms=91 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:15:59.023Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=8 elapsed_ms=107 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:15:59.023Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=9 elapsed_ms=123 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:15:59.023Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=10 elapsed_ms=138 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:15:59.023Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=11 elapsed_ms=153 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:15:59.023Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=12 elapsed_ms=168 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:15:59.023Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=13 elapsed_ms=183 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:15:59.023Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=14 elapsed_ms=198 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:15:59.024Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=15 elapsed_ms=213 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:15:59.024Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=16 elapsed_ms=229 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:15:59.024Z INFO owned_foreground_trace relation=a_other_window kind=toplevel_other initial_foreground=False sample_order=17 elapsed_ms=245 class_token=system_0 visible=True toolwindow=False noactivate=False has_owner=False owner_is_a=False owner_is_b=False is_message_only=False same_thread_as_a=True cloaked=False cloaked_known=True class_sha8=fd2fa13c ws_popup=False ws_caption=False ws_child=True ws_disabled=False ws_ex_topmost=False ws_ex_layered=False ws_ex_transparent=False ws_ex_appwindow=False zero_area=False offscreen=False equals_a_rect=False on_bottom_left_work=True title_nonempty=False
2026-11-02T01:15:59.025Z INFO owned_target_preparation reason=ForegroundRefused
2026-11-02T01:15:59.025Z INFO owned_initial_focus phase=refused
2026-11-02T01:15:59.025Z ERROR native_positive_failed stage=guardian_ready InvalidOperationException (HResult=0x80131509)
```

NEW at refusal: `01ecccb4`. Other entries retain ready IDs.

target-thread-windows-ready.json, verbatim:
```json
{"phase":"ready","complete":true,"windows":[{"window_id":"b5752a59","is_a":false,"is_b":true,"new_at_refusal":false,"facts":{"class_token":"target018_class","visible":true,"toolwindow":false,"noactivate":false,"has_owner":false,"owner_is_a":false,"owner_is_b":false,"is_message_only":false,"same_thread_as_a":true,"cloaked":false,"cloaked_known":true,"class_sha8":"251802fa","ws_popup":false,"ws_caption":true,"ws_child":false,"ws_disabled":false,"ws_ex_topmost":false,"ws_ex_layered":false,"ws_ex_transparent":false,"ws_ex_appwindow":false,"zero_area":false,"offscreen":false,"equals_a_rect":false,"on_bottom_left_work":true,"title_nonempty":true,"sampled":true}},{"window_id":"b3b44714","is_a":true,"is_b":false,"new_at_refusal":false,"facts":{"class_token":"target018_class","visible":true,"toolwindow":false,"noactivate":false,"has_owner":false,"owner_is_a":false,"owner_is_b":false,"is_message_only":false,"same_thread_as_a":true,"cloaked":false,"cloaked_known":true,"class_sha8":"251802fa","ws_popup":false,"ws_caption":true,"ws_child":false,"ws_disabled":false,"ws_ex_topmost":false,"ws_ex_layered":false,"ws_ex_transparent":false,"ws_ex_appwindow":false,"zero_area":false,"offscreen":false,"equals_a_rect":true,"on_bottom_left_work":true,"title_nonempty":true,"sampled":true}},{"window_id":"f15c2b31","is_a":false,"is_b":false,"new_at_refusal":false,"facts":{"class_token":"ime_default","visible":false,"toolwindow":false,"noactivate":false,"has_owner":true,"owner_is_a":true,"owner_is_b":false,"is_message_only":false,"same_thread_as_a":true,"cloaked":false,"cloaked_known":true,"class_sha8":"29ee788e","ws_popup":true,"ws_caption":false,"ws_child":false,"ws_disabled":true,"ws_ex_topmost":false,"ws_ex_layered":false,"ws_ex_transparent":false,"ws_ex_appwindow":false,"zero_area":true,"offscreen":true,"equals_a_rect":false,"on_bottom_left_work":false,"title_nonempty":true,"sampled":true}}]}
```

target-thread-windows-refusal.json, verbatim:
```json
{"phase": "refusal", "complete": true, "windows": [{"window_id": "b3b44714", "is_a": true, "is_b": false, "new_at_refusal": false, "facts": {"class_token": "target018_class", "sampled": true, "visible": true, "toolwindow": false, "noactivate": false, "has_owner": false, "owner_is_a": false, "owner_is_b": false, "is_message_only": false, "same_thread_as_a": true, "cloaked": false, "cloaked_known": true, "class_sha8": "251802fa", "ws_popup": false, "ws_caption": true, "ws_child": false, "ws_disabled": false, "ws_ex_topmost": false, "ws_ex_layered": false, "ws_ex_transparent": false, "ws_ex_appwindow": false, "zero_area": false, "offscreen": false, "equals_a_rect": true, "on_bottom_left_work": true, "title_nonempty": true}}, {"window_id": "b5752a59", "is_a": false, "is_b": true, "new_at_refusal": false, "facts": {"class_token": "target018_class", "sampled": true, "visible": true, "toolwindow": false, "noactivate": false, "has_owner": false, "owner_is_a": false, "owner_is_b": false, "is_message_only": false, "same_thread_as_a": true, "cloaked": false, "cloaked_known": true, "class_sha8": "251802fa", "ws_popup": false, "ws_caption": true, "ws_child": false, "ws_disabled": false, "ws_ex_topmost": false, "ws_ex_layered": false, "ws_ex_transparent": false, "ws_ex_appwindow": false, "zero_area": false, "offscreen": false, "equals_a_rect": false, "on_bottom_left_work": true, "title_nonempty": true}}, {"window_id": "01ecccb4", "is_a": false, "is_b": false, "new_at_refusal": true, "facts": {"class_token": "msctfime_ui", "sampled": true, "visible": false, "toolwindow": false, "noactivate": false, "has_owner": true, "owner_is_a": false, "owner_is_b": false, "is_message_only": false, "same_thread_as_a": true, "cloaked": false, "cloaked_known": true, "class_sha8": "4f47bb8d", "ws_popup": true, "ws_caption": false, "ws_child": false, "ws_disabled": true, "ws_ex_topmost": false, "ws_ex_layered": false, "ws_ex_transparent": false, "ws_ex_appwindow": false, "zero_area": true, "offscreen": true, "equals_a_rect": false, "on_bottom_left_work": false, "title_nonempty": true}}, {"window_id": "f15c2b31", "is_a": false, "is_b": false, "new_at_refusal": false, "facts": {"class_token": "ime_default", "sampled": true, "visible": false, "toolwindow": false, "noactivate": false, "has_owner": true, "owner_is_a": true, "owner_is_b": false, "is_message_only": false, "same_thread_as_a": true, "cloaked": false, "cloaked_known": true, "class_sha8": "29ee788e", "ws_popup": true, "ws_caption": false, "ws_child": false, "ws_disabled": true, "ws_ex_topmost": false, "ws_ex_layered": false, "ws_ex_transparent": false, "ws_ex_appwindow": false, "zero_area": true, "offscreen": true, "equals_a_rect": false, "on_bottom_left_work": false, "title_nonempty": true}}]}
```

## Picker Import Cancel attempt — FAIL

Parent: `ss-uno018-ui-57df0fdf46dd93cb719b1a51ba5d1350`.

Session: `ss-uno018-ui-ba9a883d1c0022a5503fcb5f9208c34a`.

Wrapper/child exit 1. Fixed failure records:
observer-parent-failure.json:
```json
{"reason": "worker_protocol"}
```

observer-worker-failure.json:
```json
{"reason": "input", "inputSubreason": "epoch_changed", "inputEpochDelta": 5234, "inputIdleMs": 0, "inputPhase": "unspecified"}
```

reason=input, inputSubreason=epoch_changed, inputEpochDelta=5234,
inputIdleMs=0, inputPhase=unspecified. Unsigned timestamp subtraction is not an
event counter or evidence of human/system origin; unspecified must not be rewritten
as import-cancel. Helper stdout/stderr were empty: **operationClass, pickerStep and
HRESULT absent**; no picker_schema_multiple/per-match or foreground failure record.

All eight potential helper-side picker-invoke-<phase>-<open|commit>.json files were
absent, including Import Cancel open/commit: **before/after/completed epochs are
unavailable**, not unchanged. All active/closed/actions markers and the cancelled
receipt were absent. No evidence that the Import or Cancel Invoke was reached;
only the one bounded Import Cancel scene attempt is recorded, with no retry or
later phase. The explicit throw 'actions' stop was not reached.

Owned read-only rows/usage 0/0; independent clipboard epoch unchanged. Picker privacy
CLEAN and planted controls PASS. Existing helper/fixture failure cleanup completed;
not graceful success or pre-kill JobZero acceptance. Wrapper's historic `phase2`
field is not this brief's phase number. No recovery-bearing process was killed.

```json
{
  "markers": {
    "import-cancel": {
      ".active": false,
      ".closed": false,
      ".actions": false
    },
    "import": {
      ".active": false,
      ".closed": false,
      ".actions": false
    },
    "export-cancel": {
      ".active": false,
      ".closed": false,
      ".actions": false
    },
    "export": {
      ".active": false,
      ".closed": false,
      ".actions": false
    }
  },
  "importCancelledReceipt": false,
  "pickerParentSummary": [
    "{\"phase2\": \"BLOCKED\", \"errorType\": \"ValueError\", \"reason\": \"worker_protocol\", \"privateEvidenceToken\": \"ss-uno018-ui-ba9a883d1c0022a5503fcb5f9208c34a\"}",
    "{\"phase2ClipboardEpoch\": \"unchanged\"}"
  ]
}
```

## Release — PASS

Exact false-gate grep proof (scratch helper path shortened to basename):
```text
scripts/run_uno_candidate_acceptance.py:24:FULL_HOST_OBSERVER_REVIEWED = False
scripts/run_uno_candidate_startup.py:21:STARTUP_OBSERVER_REVIEWED = False
prototypes/uno-native-target/windows-insertion/Program.cs:11:    private static readonly bool NativeEntryReviewed = false;
scripts/uno_candidate_native_fixture/PositiveProtocol.cs:8:    internal static readonly bool SourceReviewed = false;
scripts/uno_candidate_native_fixture/PositiveProtocol.cs:9:    internal static readonly bool InputSourceReviewed = false;
src/SmartSnippets.Uno/Platforms/Windows/AcceptanceNative/NativeAcceptanceFactory.cs:14:    internal static readonly bool SourceReviewed = false;
scripts/uno_candidate_native_fixture/OwnedTargetPreparation.cs:26:    internal static readonly bool SourceReviewed = false;
scripts/uno_candidate_native_fixture/Program.cs:14:    internal static readonly bool NativeFixtureSourceReviewed = false;
nr12-picker-cancel-only.py:20:UI_SOURCE_REVIEWED = False
scripts/run_uno_candidate_ui.py:20:UI_SOURCE_REVIEWED = False
scripts/run_uno_candidate_native.py:19:NATIVE_SCENES_SOURCE_REVIEWED = False
scripts/run_uno_candidate_native.py:20:POSITIVE_SCENES_SOURCE_REVIEWED = False
scripts/run_uno_candidate_native.py:21:POSITIVE_INPUT_SOURCE_REVIEWED = False
```

`git diff --exit-code` passed for all six temporarily changed tracked source
files. Final process record, verbatim:
```json
{"observedUtc":"2026-11-02T01:16:30.2625090+00:00","counts":{"SmartSnippets.UnoCandidate.exe":0,"SmartSnippets.UnoAcceptanceFixture.exe":0,"SmartSnippets.UnoCandidate.Guardian.exe":0,"SmartSnippets.Target018.exe":0,"nr12PythonHelpers":0,"UIHelper":0}}
```

Final locked RAM-only clipboard check, without post-run reseed:
```json
{"builtin_text": 4, "ownership_marker": 0, "protective_metadata": 0, "unsupported": 0, "owner_present": false, "authorized_synthetic_baseline_matches": true, "sequence_stable": true}
```

This verifies the authorized synthetic baseline, not user data before the first
seed. No owner/ownership/protective marker/unsupported format, retained coordinator,
guardian or uncertain keyboard lifetime remained. No recovery endpoint, generic
release, process-name kill or pending-recovery termination. Original profiles and
private evidence retained. All original/rebased artifact hashes match after release.

**SCREEN-DONE nr-12 2026-11-01 19:16:43 local (-0600)**. Written immediately after closure checks, well before 19:38.
No desktop/clipboard work followed; remaining work was filesystem-only.

Commit scope: **only this research document**. No flags, source helpers, operator
edits or production change committed. No push.

Pending: source analysis of the Edit-class / WS_CHILD foreground observation and
its exact identity without relaxing the exact-A boundary; picker epoch attribution
still unavailable; separately authorized native main/Copy/context, fault/lifecycle,
pointer/hover/tray and oldest-runtime obligations remain. No active recovery or
operator action pending. Plans 018/019 remain open; released slot grants no retry.
