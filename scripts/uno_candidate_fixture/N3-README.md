# N3 shell scenes — CLOSED, NOT RUN, incomplete acceptance

`run_uno_candidate_acceptance.py --scene` now accepts the fixed selectors below.
`FULL_HOST_OBSERVER_REVIEWED` remains **false**. Do not invoke a worker directly.
No scene was executed for this checkpoint; builds and stub tests are not native
acceptance. Production native gates/default profile/legacy composition are unchanged.

The existing observer still requires a reviewed pinned linked-source fixture,
exclusive screen authorization, fresh owner-private session, Job assignment before
permit, physical work-area verification, unchanged foreground/clipboard epoch,
PID/session/phase receipts, explicit acknowledgements, graceful exit and zero live
Job descendants before forced cleanup. No keyboard/clipboard mutation is added.
N3 additionally rejects the primary monitor, re-enumerates the bottom-left geometry,
and compares visible owned-window DPI with that monitor's actual effective DPI.
An operator screen selection that disagrees with bottom-left policy must refuse,
not fall back to the primary. No screen is authorized by this README.

| Selector | Fixed fixture/observer checks | Proof limit / missing work |
|---|---|---|
| `preview-options` | Real LaunchOptions parser → host ApplyLaunchOptionsAsync; synthetic query; actual sort-flyout-open state; preview, matching row, native gate denied, zero usage | Common launch consumer only, **not** initial/second-process redirect; foreground epoch preservation is **not an owned-target proof** |
| `hover` | Same preview; find loaded row's named label without reading/logging text; post WM_MOUSEMOVE to owned candidate HWND; require actual HoverDetailsSurface visible; post leave and require closed | Posted pointer messages, **not physical pointer/input acceptance**; top-level HWND delivery may not reach the XAML input-site HWND, which refuses by timeout; no input fallback |
| `tray` | First hide main and verify background state so Open cannot pass vacuously; post fixed fixture callback-seam message on retained owned message HWND; real tray-supplied Open/Settings/Exit callbacks; actual page/visibility assertions | **Not** physical tray clicks, Explorer event delivery, TrackPopupMenu command selection or tray identity proof |
| `close-background` | Post WM_CLOSE only to owned main; require hidden main with live process/tray and nonzero valid message HWND; require that message HWND retired and invalid at Exit; same tray Open callback reopens; Exit uses host shutdown | No dirty-dialog, in-flight insertion, sleep or shutdown fault-matrix proof |
| `message-window` | Before host owns singleton: warm native class once, then exactly 20 actual MessageWindow create/dispose cycles; each retired HWND must be invalid; process handle and USER-object counts must equal fixed baseline each cycle | Strict exact counts may truthfully refuse platform/framework noise; no tolerance or leaked-window allowance |
| `dpi` | Real candidate placement; observer requires current bottom-left, nonprimary monitor and real window DPI, with all visible windows inside granted physical work rectangle | No primary fallback, mixed-monitor dragging or sleep/re-show stress |
| `coexistence` | **Refuses before artifact/profile/desktop/launch access** with fixed internal reason | **D49 — NOT NATIVE-CLOSABLE IN PLAN 019:** production legacy hardcodes AppPaths/instance and has no owned-profile bootstrap; starting it with an ignored `--profile` would access production state. No legacy launch, profile/registry diff, registration or coexistence PASS is claimed |

## Receipt order

Each runnable N3 scene starts `shell-ready`, then its fixed scene phases, then
`shell-verified`, followed by the posted Exit seam and existing `graceful` receipt.
Preview: `shell-preview`; hover: `shell-hover-open`, `shell-hover-closed`;
tray: `shell-tray-open`, `shell-tray-settings`; close: `shell-background`,
`shell-reopened`; cycles: `shell-message-cycles`; DPI: `shell-dpi`.
The background phase requires **exactly zero visible owned windows**, not merely
"at least zero". Missing receipt/callback/DPI/geometry/privacy/cleanup fails before
acknowledgement or success. Observer failures serialize fixed tokens only, never
raw exceptions, text, labels/tags, titles, queries or paths. Owned diagnostics use
the existing scanner and planted canaries; SQLite intentionally contains synthetic
fixture data and is not diagnostic evidence. Evidence/profiles remain private and
retained. Emergency termination is never successful graceful cleanup.

The shell permit is fixture-only, incompatible with rendered-UI permits, exact
allowlisted text, and supplied only by the gated observer. No new production CLI,
profile override, focus capability or insertion/Copy capability exists.

## Headless coverage / remaining blockers

`ShellSceneTests` drives the shared fixture scene state machine with stub surfaces
and real host navigation/preview/tray/store consumers. `ShellMessageCycleTests`
drives the exact native-cycle algorithm with resource/window stubs, including
leak/failure disposal. `test_uno_candidate_shell.py` drives observer scene dispatch,
permit ordering, refusal, DPI/primary policy, zero-window background acknowledgements,
failure cleanup and planted privacy failures without native execution.

This is a **partial N3 implementation**, not N3 closure. **User decision D49:
coexistence is NOT native-closable in Plan 019.** Legacy hardcodes production
AppPaths and has no owned-profile bootstrap; no legacy product change is authorized.
The fail-closed refusal remains, not a bootstrap pending delivery in 019. Never
move the real profile, change KnownFolders or modify legacy startup as a workaround.
Full preview acceptance also requires an
owned, pinned foreground target lifecycle (and actual process redirect). These are
not implemented, and the observer reports them explicitly as missing/blocked.
Independent review and a fresh fixture native AOT artifact are required before any
future screen session. Candidate AOT publication alone is not fixture provenance.
