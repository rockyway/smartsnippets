# Plan019 nonmutating full-host acceptance fixture

**UNREVIEWED / NOT RUN.** `FULL_HOST_OBSERVER_REVIEWED=False` remains closed.
Build success and mocked/headless tests are not native acceptance. No production
candidate sources, default profile, CLI/environment profile overrides, or existing
startup observer gates are changed. The retained default candidate profile is never
queried/opened/moved/deleted by this harness.

This separate Windows/AOT executable links the actual candidate sources, App.xaml,
compiled views, WindowsCandidateBoundary, Core and Presentation. Only App/Program
are replaced by the test bootstrap. Package graph is pinned by its local SDK/lock;
the final authored RuntimeIdentifiers is win-x64. It is not a production candidate
binary and must not be distributed as one.

## Build (does not launch)

From repository root, PowerShell 7.2:

```
./scripts/verify-uno-candidate.ps1 -BuildOnly
```

Output is `BUILD_CHECKS_ONLY`, a unique build token and entry SHA256. The fixture
publish directory is owner-private before logs/properties are written. The gate
reuses the production binary/property/reference checks with only the fixture's
assembly/project spelling adapted. It additionally binds linked sources to
`fixture-source.sha256`; source drift, wrong digest, aliases, managed entry or stale
native compilation evidence fail closed. Build evidence is local-host provenance,
not a defense against a malicious compiler/operator.

## Observer protocol (requires independent source review and screen authorization)

Do not execute merely because this documentation exists. After review, the operator
must explicitly open the source gate locally, authorize an exclusive slot and
supply the exact build executable/hash and physical bottom-left work rectangle:

```
./scripts/verify-uno-candidate.ps1 -Executable <fixture-entry> -Sha256 <hash> -AuthorizedScreenSlot -WorkArea <left>,<top>,<right>,<bottom>
```

The executable is accepted only under this fixture's unique publish token. The
supervisor creates a fresh owner-private directory using the existing startup
observer helper, assigns the waiting observer to its kill-on-close Job **before**
granting its permit, and bounds the entire worker to 75 seconds. The app derives
its fixed `fixture-profile` child and unique instance key from that newly owned
working directory, never from a profile argument/environment value. An existing
fixture profile refuses startup; there is no fallback to OpenDefault.

The fixture emits PID/directory/phase-bound receipts and pauses for filesystem-only
acknowledgements. The observer reuses the reviewed input-desktop, physical DPI,
owned HWND/PID/visibility/cloak, bottom-left geometry and foreground/clipboard-epoch
snapshot protections. It never sends window messages/input, invokes UIA, activates,
reads clipboard contents, registers a shortcut or contacts another machine.

| Phase | Implemented check | What it does NOT prove |
|---|---|---|
| ready | StartAsync returned; real store/VMs exist; exactly one candidate_started and no ERROR; owned visible window stable 500ms | Rendered control event delivery |
| management | Actual native page navigation; VM folder create/rename; editor create/update/save/assignment; store reload/delete-to-Unfiled; compatible setting persistence | Mouse/keyboard callbacks, dialog Save/Discard/Cancel, GUI deletion confirmation, import/export, restart parity |
| preview | Real preview route/query/cache result; IsPreview; zero usage; gate unavailable; at least two owned visible windows | Positive Copy/Insert, physical pointer/hover or keyboard selection |
| recreated | Dispose actual native popup, then real host creates replacement; same preview invariants and windows | Main close-to-background/tray callback, sleep/DPI stress |
| graceful | Real ExitAsync; no cleanup ERROR; profile lease already released before Application.Exit; process exit 0 | Positive insertion/restore draining (gate remains closed) |
| job/privacy | Zero active descendants **before** emergency termination; zero after cleanup; safely-read owned app/stdout/stderr scanned using shared UTF8/UTF16 canary scanner + planted controls | Legacy coexistence/registry proof; privacy CLEAN cannot replace missing receipts |

Any missing/foreign receipt, desktop change, out-of-work-area window, watchdog,
cleanup error, nonzero exit or nonempty successful job is failure. Emergency forced
cleanup is never reported as graceful success. All app logs/stdout/stderr stay in
the private session; profile and evidence are retained, never automatically deleted.
Synthetic content/label/tag/query/path canaries exercise the diagnostic scanner;
synthetic title is a planted negative control only (no external title is read).
The database is intentionally **not** scanned for fixture text.

## Integration and outstanding gates

No production or shared-test project patch is required: SDK default inclusion picks
up Acceptance/*.cs; the fixture links the same driver. Parent should integrate only
this worker's commit (the privacy helper was cherry-picked from parent bb23f32 and
must not be applied twice). Parent owns status-only candidate README/research031/
Plan019 updates and independent reviewer fanout. Rebuild the fixture after any
candidate/shared/driver changes; rerun candidate suites sequentially, not alongside
publish (Core output is shared).

Native execution remains NOT RUN. Full rendered GUI delivery, close-to-background,
tray callbacks, dialogs/import/export, legacy coexistence/registrations, hover/DPI,
oldest Windows runtime, positive clipboard/shortcut/input/recovery and Plan019
completion remain open. Native positive clipboard work is parent-owned. Do not
promote programmatic fixture assertions or this separate fixture's eventual run to
production candidate parity without source-equivalence review.


## Fixed rendered folder checkpoint (review closed)

Test-only executable linking the actual candidate XAML/handlers/host and shared
Core/Presentation. No executable CLI arguments or production profile override.
The owned private directory, launch permit and exact rendered permit are required;
production native capability remains null. This fixture is not production parity.

## Fixed rendered scenes

`scripts/run_uno_candidate_ui.py --scene snippets|folders|settings` selects only a fixed
scene in the observer, never arbitrary selectors or mutation text. All source
execution gates remain false. **All visible tests remain PAUSED pending operator
remaining-monitor authorization and independent review.** No scene was executed
for this checkpoint.

The default snippets scene retains New/edit/save/dirty-leave/Keep-editing checks.
The folders scene additionally requires the test-only
`fixture-folder-scene-permit` containing exactly `owned_folder_ui_019\n`; it cannot
be selected by the production executable. Initial fixture navigation is Folders.

| Phase | Required evidence |
|---|---|
| ui-ready | Fresh empty store, actual retained owned main HWND |
| ui-folder-created | UIA New folder name ValuePattern + Create folder InvokePattern; real VM/store create; SQLite reload verifies exact name/id |
| explicit setup | Fixture **seeds** one synthetic snippet attached to that actual newly created folder; this is NOT rendered snippet creation/assignment evidence |
| ui-folder-renamed | UIA Rename folder, Folder name, Save folder name; actual Click handlers/shared VM/store; reload verifies unchanged id and attached snippet |
| ui-folder-deleted | UIA Delete folder then actual confirmation PrimaryButton; reload verifies zero folders, same snippet text, null FolderId, zero usage and Unfiled count 1 |
| actions | Exclusive content-free receipt: exactly 5 completed InvokePattern calls, 2 SetValue calls, 2 SetFocus calls; observer verifies before shutdown acknowledgment |
| graceful | Actual host ExitAsync, released profile lease, zero process exit, zero job descendants |

Only exact allowlisted name/type/id controls under the retained HWND, owned PID,
uniquely enabled and onscreen are admitted. Discovery may wait; mutations never
retry. The snippets scene retains its stricter duplicate-selector rejection.
The observer's idle/input epoch, clipboard sequence, identity, work-area geometry,
foreground monotonicity, bounded watchdog and monotonic shutdown checks are
unchanged. SetFocus is authorized management activation, not nonactivation proof.
Signed system Windows PowerShell, STA and strongly typed UIA remain required.

Fixture binaries require fresh strict Native AOT publication and existing fixture
provenance validation; source hashing includes the linked assertions, fixture and
observer/helper. App/helper stdout/stderr and app log are privacy-scanned including
folder, renamed-folder and seeded-snippet canaries with planted negative controls.
Private profiles/evidence are retained, not automatically deleted. Forced cleanup
is never graceful success. The database intentionally contains synthetic text and
is not diagnostic evidence to scan.

## Remaining unproven paths

Rendered execution of all scenes is NOT RUN. The bounded settings scene below
adds theme/text-size/repository reload checks, not executed evidence. Folder
unsaved rename Save/Discard/Cancel, validation/failure paths, rendered snippet
assignment, file pickers/import/export, row selection, tray/close/background,
legacy coexistence, hover/DPI/oldest runtime and all positive native clipboard,
hotkey/input/recovery acceptance remain open. The existing non-rendered full-host
management/preview/recreation fixture remains available, but its VM/store calls
are not rendered-event proof. Plan019 remains incomplete.

## Fixed rendered settings checkpoint (review closed)

The test-only `fixture-settings-scene-permit` must contain exactly
`owned_settings_ui_019\n`, accompany the rendered permit, and cannot coexist with
the folder permit. No production CLI, environment or profile override is added.
The fixture initially navigates to Settings; it seeds no settings values.

| Phase | Required evidence |
|---|---|
| ui-ready | Fresh System/Medium defaults, unset theme/size keys, zero folders/snippets, native gate denied |
| ui-settings-theme | Exact ThemeRadios/Theme group and Dark radio; SelectionItemPattern.Select once, actual SelectionChanged handler/shared VM; repository reads Dark/Medium and fixture checks actual theme service Current |
| ui-settings-size | Exact TextSizeRadios/Text size group and Large radio; Select once, actual handler/shared VM; repository reads Dark/Large, shared text-size service is 18 |
| ui-settings-reloaded | Fresh SettingsRepository/SQLite connection and AppSettings.Load verify persisted Dark/Large, default palette/custom size; no setting writes in assertion driver |
| actions | Exclusive receipt with exactly 2 ScrollItemPattern.ScrollIntoView calls and 2 SelectionItemPattern.Select calls; no Invoke/SetValue/SetFocus calls |

The separately approved narrow scroll exception permits one ScrollIntoView on each
uniquely identified owned radio, even if initially offscreen. Immediately afterward,
all guards and exact group/radio identity are revalidated, and the target must be
onscreen/enabled before the single Select. Discovery/layout waiting is read-only;
scrolls and selections never retry. No generic tree scrolling, coordinate fallback,
foreign focus or input synthesis exists. Missing ScrollItemPattern, unsupported
native UIA group/radio identity, refusal or timeout is BLOCKED, never bypassed.
Scroll counts describe UI scrolling, not additional settings mutations.

The existing immutable quiet/input epoch, foreground, ownership, clipboard sequence,
geometry observer and monotonic shutdown remain enforced. The helper additionally
checks its captured clipboard sequence at every guard. All source execution flags
remain false and all visible execution remains PAUSED pending operator authorization
and consolidated independent review. Headless VM setup in assertion unit tests is
not native/rendered evidence. Repository reload is **not process relaunch**, visual
pixel/theme comparison or proof of the custom-size/palette/System/Light paths.
File pickers and dirty-folder dialogs are not part of this settings batch.
