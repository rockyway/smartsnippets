# 033 — Primary-landscape rendered management acceptance

Plan 019 partial execution evidence, not plan completion or production cutover.
Plan 018 remains in progress. macOS is paused.

## Authorized display and source variant

The operator explicitly selected the **primary landscape**, not the right-hand
portrait display. Windows initially reported two displays, then all five again
when powered-off displays remained in its active topology. The stale bottom-left
work area could therefore not satisfy this screen-slot instruction.

Only the isolated `feature/uno019-primary-ui-trial` worktree changes placement:
commit `32cf03b` filters C# window placement and Python work-area validation to
`MONITORINFOF_PRIMARY`. Root/production placement is unchanged. Review
`b1d407f6-b9a3-4466-9acb-5b2cde56381c` cleared this geometry-only exception.
No candidate artifact was launched before that review. Its temporary source gate
was restored and the closed-gate Python suite passed before authorized execution.
Physical PMv2 work area for the accepted runs was `(0,0,3840,2088)`.

Windows reported a held Right Arrow state even though the operator was not
physically holding it. No synthetic key-up was sent. After the operator pressed
and released the key, the read-only check returned no held inputs and sufficient
idle. Subsequent acceptance retained all input/foreground/clipboard guards.

## Runtime defect found and fixed

Earlier primary-display snippet runs completed rendered callbacks but failed
cleanup. Session `ss-uno018-ui-a43402bc47dbaf29ddcc1281fb2a8238` recorded native exit
`3221226107` (`0xC000027B`) and sanitized `fixture_unhandled COMException
(HResult=0x800710DD)` at `candidate_cleanup_stage stage=main_window`.

`f149fd4` removes the closed-window Content read, makes close idempotent and guards
activation/queued DPI callbacks during teardown. `c91d886` adds content-free
release-stage, unhandled-exception and numeric exit diagnostics without handling
or suppressing the failure. Review `74685810-33fb-4385-9a01-72686e697e9a` cleared
these changes. A later run stopped safely on `foreground_changed`; it remains
BLOCKED, despite complete cleanup stages. `3061426` records only the relationship
of the originally captured foreground to known owned handles, without relaxing
the guard. No external activation or clipboard mutation was added.

## Accepted fresh fixture

- Build: `a2ebfda289cb40b89aa5740bf9830688`.
- Executable SHA256: `29830e360cb44af0f208aa3cd5b6111b7a11022bc8d009c4116489bc87d0b03d`.
- Enabled linked-source fingerprint:
  `33d11ff5929e13a2d5aaa18aff8a28ba74467138a1beb40121cb4fff300d9b17`.
- Additional observer helper SHA256 (primary-monitor filter):
  `715e19787c2008e412ef9fc2d2fabf7e88e5f8b88ed4fabc1c386a751b889ccf`.
- Management source review: `bb881ff9-527c-4d78-8bdd-5e6afddb1784`.

| Scene | Private session | Observed scope |
|---|---|---|
| snippets | `ss-uno018-ui-8a9a56e7ab39172a5eb3b2964d725721` | Actual New/edit/Save, SQLite reload, dirty Cancel, dirty Save, new-draft Discard and editor replacement; exact callback counts |
| folders | `ss-uno018-ui-d2e8457d8a0d821c530d88b96f1d4316` | Actual folder create/rename/delete; attached synthetic snippet survives as Unfiled |
| settings | `ss-uno018-ui-a879cce72998f18ba06b7b2ccb5f0818` | Actual fixed theme/text-size radio scroll/selection; shared VM/theme/text-size effects and repository reload |

All three observers returned OBSERVED with graceful exit, pre-kill `ZERO_ACTIVE`,
unchanged clipboard sequence, quiet/all-up immutable input epoch, Default input
desktop, owned identity/geometry checks and clean privacy scans with planted
negative controls. `foregroundAfterClose=initial` in all three. UIA intentionally
focused only the owned management window; this is **not** non-activating QA proof.
The same source execution gate was restored false immediately after the scenes.
Native clipboard/insertion capability remained default-denied throughout.

## Explicit remaining limits

Folder snippet assignment was explicit fixture seeding, not a rendered assignment
callback. Settings persistence was a fresh repository read, not process relaunch.
Existing-snippet Discard, save-failure recovery, native file pickers, row-selection,
preview/pointer/hover/tray/coexistence and native insertion/Copy/fault matrices are
not established by these three runs. Clipboard abort/changed-owner controls remain
separate prerequisites before any positive native input execution.
