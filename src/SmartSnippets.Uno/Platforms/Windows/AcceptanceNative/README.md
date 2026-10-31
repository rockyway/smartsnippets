# Plan019 acceptance-native adapters (internal, source-review closed)

This is **not** native acceptance evidence or a production capability. `NativeAcceptanceFactory.SourceReviewed` remains false. No CLI, environment, stored setting, tray command or prototype-gate change enables it. The separate guardian shares this same gate. Independent review and operator authorization are still required before any native launch. Prototype `NativeEntryReviewed` is unchanged.

## Implemented scope

- Actual `ICandidateNativeOperations` bridge: `NativeAcceptanceSession`, using the contract introduced by upstream 2359d9d. Default host composition is not edited here.
- Internal factory creates only its own retained, native/hash-pinned `SmartSnippets.Target018.exe` fixture. Unchanged linked identity code validates exact A/B HWND, PID, per-run class and editor HWND; no PID adoption, title lookup or arbitrary window enumeration. Select exactly `"a"` or `"b"` when creating a session.
- Explicitly selected nondefault hotkey, own message HWND/thread only, conflict refusal without unregister, fresh callback permission and exact message ID/gesture, retryable/idempotent owned cleanup. Saved/default production gestures are never registered.
- Separate Copy vs insertion permissions and helper commands. No clipboard-loss/manual-paste/discard path. Only the fixed synthetic `OwnClipboardPublication.Multiline` text is accepted, after existing insertion CRLF normalization. Store resolution/usage remain exclusively in the existing `InsertionService`.
- Independent guardian performs the entire clipboard transaction under one clipboard lock. Original representations never cross IPC or reach disk/logs. Worker receives only count/byte metadata and a recovery-obligation token. No clipboard sequence API/arithmetic is used. Actual owner, complete actual representations and private per-run marker bind recovery.
- Supported original representations are deliberately restricted to HGLOBAL ANSI/OEM/Unicode text, locale and compatible privacy markers (4 MiB total). **Any** custom, skipped, handle, unreadable, unsupported or over-cap representation refuses before clear. This is narrower than the shipping clipboard dispatcher, not a replacement for it.
- Publication/restoration install ownership marker first and privacy protection before text. Verified restoration preserves all captured representation bytes and intentionally retains the run marker plus zero-valued privacy markers. Original nonzero/incompatible privacy flags refuse before clear. Partial/foreign-owner recovery keeps the RAM backup; no “changed owner means restored” inference.
- Four-event SendInput only. Zero means not dispatched; one-to-three or a throwing call means keyboard recovery pending. No generic modifier releases, key-up repair, retry of uncertain input or AttachThreadInput exists. Clipboard settlement does not clear keyboard pending.
- Exact owned popup focus only: random HWND property + exact class/thread/PID + retained native candidate image prevent destroyed/reused HWND adoption. Only exact fixture ↔ exact pinned candidate popup may transfer foreground; foreign foreground refuses. The helper independently checks the popup pin for Copy from the popup. Main-window-focused Copy is intentionally unavailable unless the authorized exact popup/fixture foreground rule holds.

## Admission versus native boundaries

Per supervisor decision, nonmutating Insert/Focus `Validate` checks session, monotonic bounded lifetime, identity and visible/current desktop, **not** an idle/foreground guarantee. `RestoreFocusAsync` boundedly waits up to three seconds for unchanged quiet/all-input-up conditions, rechecks permission and exact owned foreground before SetForegroundWindow, then verifies the exact fixture. Cancellation/expiry/preview/shutdown refuses, without mutation retry.

`ReadyForAdmissionAsync(ct)` provides the same bounded quiet wait for a shortcut callback before host capture/show. After waiting, the host must recheck permission and capture again. Capture, Copy, clipboard publication and SendPaste retain strict idle/all-up checks; input/publication require the exact retained fixture foreground. Popup Copy permits the exact pinned popup foreground instead. No physical key release or guard exemption is synthesized.

## Required process topology

1. A recovery-aware external coordinator runs **outside every kill Job**. Launch the candidate without an observer/outer Job.
2. From the candidate's internal harness-only seam, start `AcceptanceFixture`, create the exact candidate popup, and call `NativeAcceptanceFactory.Create(...)`. It starts the guardian using private inherited anonymous pipes. Before any clipboard open, the guardian validates duplicated retained worker/fixture handles, native image hashes, same unelevated authentication/user identity, direct-parent provenance and exact fixture/popup identity. Both sides reject a guardian in any Job. Factory refuses an already-jobbed candidate **before helper launch**.
3. Only after the `ready` handshake may the coordinator assign the already-running candidate and fixture retained handles to a worker kill-on-close Job. Do **not** include the guardian, and do not use process-tree termination on candidate/ancestors. Existing children are not retroactively moved when their parent is assigned; nevertheless verify the guardian is outside every Job immediately before mutation (the helper also rechecks).
4. Graceful shutdown first closes admission and drains VM/service continuations, then calls `DrainOwnedRecoveryAsync`. False means **RECOVERY_PENDING**, not permission to terminate recovery or claim cleanup. The coordinator retains its recovery session until settlement. On worker abort/EOF, the guardian waits for the actual retained worker HANDLE to signal before restoring. EOF, timeout and PID disappearance alone are not death proof.
5. An unsettled guardian remains running, holding its RAM snapshot and current-user-only `ss-uno019-recovery-<run>` pipe. `status` returns content-free pending status; `retry` retries ownership-checked restoration. A foreign owner is never overwritten. No unconditional crash/power-loss/OS-termination recovery guarantee is claimed. No plaintext recovery persistence exists.

The nonmutating observer's unconditional outer `TerminateJob`/watchdog **cannot** be reused around this tree. Killing the guardian, its encompassing Job, or all descendants destroys the sole backup and is forbidden until verified settlement. Keyboard uncertainty is a separate unresolved state; this clipboard guardian cannot repair it.

## Exact integration patches (other lane owns these files)

Candidate project, before its closing Project element:

```xml
<Import Project="Platforms/Windows/AcceptanceNative/AcceptanceNative.links.projitems" />
```

Candidate test project:

```xml
<ItemGroup>
  <Compile Include="../../src/SmartSnippets.Uno/Platforms/Windows/AcceptanceNative/*.cs" LinkBase="AcceptanceNative" />
</ItemGroup>
<Import Project="../../src/SmartSnippets.Uno/Platforms/Windows/AcceptanceNative/AcceptanceNative.links.projitems" />
```

The candidate already default-compiles this new subtree. Links reuse unchanged retained-child identity, fixed synthetic publication/conversion, bounded IPC and input-desktop preflight sources. Mac identity is linked solely because the existing lease's factory references both concrete types; this factory only permits Windows native artifacts. No shipping/prototype source or existing project was modified by this commit.

Internal harness construction sequence (not an ordinary launch option):

1. `AcceptanceFixture.StartAsync(nativeRoot, exactTargetPath, targetSha256, ct)`.
2. `NativeAcceptanceFactory.Create(fixture, "a", exactPopupHwnd, exactGuardianPath, guardianSha256, expires, allowInsert, allowCopy, permitted)`; maximum two minutes, also bounded monotonically in candidate and guardian. `permitted` is the reviewed harness's operation/preview/revocation predicate, **not** `CandidateNativeCapability.Allows` (which would recurse into Validate).
3. Construct `CandidateNativeCapability(session.SessionId, session.Target.WindowHandle, session.Target.ProcessId, explicitPermissions, lifetime, TimeProvider.System, session)` and inject through the existing host seam. Never grant ClipboardLoss. Keep ordinary composition null/default denied. Choose Copy separately from Insert/Focus/Input/Clipboard; Capture and Shortcut are independently checked by the adapter predicate.
4. Build hotkey with `session.CreateHotkey(explicitGesture, candidateMessageHwnd, callback)`, call `TryChange(explicitGesture)`, and route the real message callback through `hotkey.HandleMessage(message, wParam, lParam)`. Never register from saved settings. The callback must await `session.ReadyForAdmissionAsync(ct)`, recheck session/host permission and use the guarded host capture/show route. Async callback cancellation/failure must be owned/observed by the host, not async-void native callbacks.
5. Route the popup coordinator's **already-ready surface focus callback** to `session.TryFocusOwnedPopup()`. Do not call it before the shared coordinator reveals readiness, replace coordinator policy, or use a direct Activate/foreign SetForegroundWindow fallback. This is the exact additional hook needed for shortcut → actual popup keyboard selection to be reachable. The outer gate then uses the same session for popup/main-row insertion.
6. Dispose hotkey before its message window, close gate admission, drain active insertion and **always** query `session.DrainOwnedRecoveryAsync()` (even without local insertion snapshots), then dispose session/window/fixture only under the recovery-aware topology above. Session disposal detaches control; it never terminates an unsettled guardian. Refresh Copy availability after a session settles: each session permits one successful transaction; create a newly reviewed session for the next acceptance scene.

**Root-gate integration blocker:** upstream 2359d9d removes a snapshot on `ClipboardChanged` and skips adapter drain when local snapshots are empty. This adapter intentionally retains independent RAM on foreign ownership. Retain that recovery obligation and always query adapter drain, including failed Copy (which has no insertion snapshot). Otherwise graceful shutdown can falsely report settled. Also suppress/deny UI loss-consent/manual fallback for this fixed-synthetic scope; the adapter always rejects it, but a NotPreserved result alone must not advertise that retry as available.

## Validation and limits

From candidate cwd, run the isolated fake-OS tests sequentially:

```text
dotnet test ../../tests/SmartSnippets.Uno.Tests/AcceptanceNative/SmartSnippets.Uno.AcceptanceNative.Tests.csproj -c Release -p:NoWarn=
```

From `prototypes/uno-candidate-native`:

```text
dotnet publish -c Release -r win-x64 -p:NoWarn= -o bin/acceptance-aot
```

Strict AOT, analyzers, warnings-as-errors and no suppressions are enforced; publish does not launch anything. The isolated tests exercise actual transaction, adapter, bridge, focus, target and hotkey code through low-level fakes. Native bootstrap/inherited-handle behavior, actual HWND class, postpublication restore, worker-abort guardian survival, native clipboard synthesis, repeated lifecycle, callback→popup→Enter/main-row/Copy and final usage receipts remain **NOT RUN / review-gated**. Fixed text, one transaction/session, no arbitrary rich clipboard preservation and no automatic uncertain-keyboard recovery are explicit limits. Plan019/T7/AC4 remain open.
