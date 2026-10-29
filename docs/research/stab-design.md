# Stability investigation and design

## Findings (static; real UI reproduction pending)

- The narrow Back button only hides the editor; switching rows replaces the editor VM on the same control. `SnippetEditor.ViewModel` assigns the new VM before `Bindings.Update()`. The folder ComboBox has a one-way ItemsSource and two-way SelectedItem, so changing the source can send a transient null to the new VM. The generated setter accepts null before `OnSelectedFolderChanged`; outside refresh that handler converts null to Unfiled, and during refresh returns leaving null. Both are unsafe. This is a plausible direct cause, not yet verified on WinUI. FolderChoice is a value record, so a rebuilt equal choice may not replace the old reference.
- Text, label and tags also have two-way bindings across the VM swap; their order and actual WinUI behavior require control-level verification. Folders page needs a similar audit.
- Popup and VM already exist at startup, with a precomputed search and row dictionary. `Prewarm` briefly shows off-screen and immediately hides; theme registration happens afterward. A white/black flash has not been localized or reproduced; first-frame initialization and theme-before-prewarm are suspects, not proven causes.

## Implementation and validation follow-up

Completed in stability commits through `125a05a`; detailed decisions and invalidation matrix are in plan 006 §10's stability amendment. Folder selection is now OneWay with explicit user selection and control/VM guards; text inputs and Settings initialization are also guarded. Cache invalidation is UI-queued/coalesced with synchronous show freshness barriers. Closed popup windows are replaced through a stable forwarding endpoint under the unchanged recovery gate. Prewarm applies theme/DWM attributes before showing a cloaked offscreen surface and waits for first content layout before hiding/uncloaking.

Granted validation ran exclusively from `C:\temp\ss-stab-build`: build 0 warnings/errors, Core 426 passed, Presentation 467 passed, one Native AOT publish gate passed with 0 warnings. The 50k-row dirty-open VM test measured 67.48 ms. No app launch or screen pass occurred. The reported WinUI folder event sequence and visual flash causality remain candidates pending user screen confirmation; the unsafe static write path and initialization ordering were confirmed and corrected.

## Original proposed approach

Guard null at the folder property's entry point; explicit Unfiled remains a real choice. Ensure rebuilt selection is the actual collection member. Prevent control selection feedback during a source swap, and review other two-way fields. Unit tests simulate null during collection reset and editor switching; WinUI verification is still required.

Popup invalidation matrix: snippet create/edit/delete, import/reload, usage, folder create/rename/delete all raise store Changed: refresh projection and sorted search; visible refresh retains query and selection by ID; hidden dirty data must be refreshed before show. Sort changes rebuild sorted search; layout rebuilds rows; text-size service is shared by row projections; theme applies to the window. Coalesce deferred work only on the store's UI thread; no background access to the single-threaded store. Disposal unsubscribes and drops cached projections. Preserve insertion/recovery and tray policies. Confirm first composed frame, current theme, hidden startup and no activation with a separate authorized screen pass.
