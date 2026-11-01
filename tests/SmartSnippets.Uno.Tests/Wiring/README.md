# Candidate action-consumer audit

`EveryCandidateAction_ReachesRegisteredConsumerOrTruthfulRefusal` compares all
candidate XAML actions with the checked-in `CandidateActions.txt` inventory, then
executes all 90 entries on fresh `Composition/HostFixture` instances, on the store's
owning synchronization context. Views come from the linked production
`WindowsCandidateBoundary.CreateMain/CreatePopup` factories, not reimplemented
factory wiring. Runtime VM identity and registered admission delegates are checked
before driving the actual handlers. The manifest is **not** regenerated during builds.
An added, changed or deleted action requires explicit inventory/consumer review.
The index identifies the XML element (including the root); repeated row targets
are separate entries. Known property `x:Bind` expressions are excluded; an unknown
binding attribute is conservatively treated as an action pending classification.

## Consumer map

| XAML | Executed consumer/assertions |
| --- | --- |
| `MainWindow` | Actual guarded navigation; dirty Cancel/Discard before page change |
| `Pages/FoldersPage` | Actual key/click handlers and VM command; create, rename, commit, cancel, delete in SQLite |
| `Pages/SettingsPage` | Actual handlers/command; persisted theme/palette/size, edit-session focus, shortcut/startup truthful refusals, recorder state, host preview |
| `Pages/SnippetsPage` | Actual handlers/commands; browse folder create/cancel, search, sort, editor reveal/back, accelerators, layout, delete, menu callback registration, real import parser/export publication |
| `Controls/SnippetEditor` | Actual handlers/commands; field/folder input, save/discard/delete, real SQLite-trigger failure and Retry, admitted Copy plus failed-Copy feedback, folder keyboard handling |
| `Popup/PopupWindow` | Actual handlers/commands; layout/search/sort, host open-main/add, item/Enter insertion, registered context-menu Copy/Insert/Edit, live revocation, footer Copy failure/cancel/dismiss, editor save/cancel/Escape/Ctrl+Enter |
| `Controls/SnippetRowDetails` | Actual handlers into actual hover coordinator; each label/folder/tag target, delayed open/cancel, pointer movement and handoff |
| `Controls/HoverDetailsSurface` | Actual surface handlers into actual coordinator; enter preserves handoff, exit closes |

The partial `*ActionConsumers.cs` test drivers can access the **linked production
private handlers** without reflection. Commands are the real generated VM/control
commands, not duplicated implementations. Every case asserts an observable state,
persisted value, registered callback effect, or nonempty refusal. Removing a
handler breaks compilation; making it a no-op breaks its consumer assertion.

## Boundaries and limits

- `HeadlessFields.cs` substitutes XAML-created named rendering fields and binding
  refresh calls; it does **not** replace any action handler. The manifest pins the
  XAML declaration to the typed handler/command driver.
- `HeadlessXaml.cs` substitutes WinUI rendering primitives, explicit timer ticks,
  focus/keyboard/pointer state and OS window calls. It never creates a desktop
  window or injects input. `ActionAuditNative` substitutes only owned OS operations;
  the candidate capability/gate and real `InsertionService` remain in the path.
- `HeadlessBoundaryServices.cs` supplies external rendering/platform service types
  needed to compile the real Windows factory. Its unused picker/dialog methods
  throw; the real host continues to use `HostFixture`'s picker/dialog boundaries.
- Views receive the actual host VMs, operation admission and Copy admission from
  the real factory, including the page-to-editor Copy forwarding setter.
  `HostFixture.cs` itself is unchanged to avoid overlap with other work.
- This is not generated-XAML binding delivery, rendering, native focus/pointer,
  shortcut/HID or clipboard acceptance. Native boundary forwarding, placement,
  disabled controls and other unique source/binding audits remain. Only two
  popup-menu availability source-spelling assertions were retired after positive,
  revoked and attempted-disabled-invocation behavior was covered.
- The exclusive xUnit collection prevents the substituted rendering statics from
  racing. Tests clean timers, hover state and focus between entries.

Validation also temporarily planted five mutations (all restored): an unmapped
XAML Click, a no-op popup handler, a no-op VM command, an empty startup-refusal
message, and a disconnected factory Copy-admission consumer. The named audit
failed for all five. No native review flag was changed.
