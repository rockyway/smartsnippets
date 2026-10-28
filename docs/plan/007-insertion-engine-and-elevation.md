# 007 — Insertion engine and elevation handling

**Date:** 2026-10-28 (revised 2026-10-28 after review [006-008-implementation-readiness](../review/006-008-implementation-readiness.md))
**Status:** Approved 2026-10-28 (G2)
**Size:** L (subplan of [001-master-smart-snippets-v1.md](001-master-smart-snippets-v1.md))
**Area:** `src/SmartSnippets.Presentation/Services/`, `src/SmartSnippets.Presentation/Text/UiText.cs`,
`src/SmartSnippets.Core/Domain/`, `src/SmartSnippets.App/Services/` (insertion, elevation probe),
`src/SmartSnippets.App/AppHost.cs`, `tests/SmartSnippets.Presentation.Tests/`, `tests/SmartSnippets.Core.Tests/`
**Depends on:** 002 (complete, insertion spike), 003 (complete, `ISnippetStore.RecordUsage`), and the
**T0 shared contract** (§4.3.1, `src/SmartSnippets.Presentation/Services/InsertionContracts.cs`,
written by the coordinator before either track starts).
**Runs in parallel with:** 006 (popup). 006 consumes `IInsertionService`; 007 implements it.
**Requirement brief confirmed:** 2026-10-28 (G1, "006–008 brief")

> **API check (2026-10-28, at `4861b0e`):** every API this plan cites was opened at that commit
> (§3). The T0 contract (§4.3.1) does not exist yet; the coordinator writes it in T0 as
> `src/SmartSnippets.Presentation/Services/InsertionContracts.cs` before 006 and 007 start. Before
> starting 007, check that the merged T0 file matches §4.3.1 verbatim, and that
> `ISnippetStore.RecordUsage`, `ISnippetStore.Snippets`, `IElevation` and `IClipboardService` still have
> the shapes cited in §3. If anything differs, update this plan first.

---

## 1. Goal

When you press Enter on a snippet in the popup, its exact text lands once in the app you were
using, as if you had pasted it yourself. Your clipboard comes back as it was, and the snippet never
shows up in clipboard history.

- **Nothing is changed without telling you.** If your clipboard holds something Smart Snippets
  can't save and put back, it stops before touching the clipboard and asks "Paste anyway?". If it
  pasted but couldn't fully put your clipboard back, it says so.
- **Nothing is pasted anywhere else.** If the text can't be inserted (the app was closed, it runs
  as administrator, focus couldn't be restored, or another app is holding the clipboard), the popup
  tells you why and what to do: Copy text, or Restart as administrator.
- **No silent double paste.** If Windows only partly sent the paste keystroke, Smart Snippets says
  it *may* have pasted and asks you to check, instead of offering a retry.
- **Usage counts only go up when the paste was actually sent.**

## 2. Requirement slice (G1, 2026-10-28; review decisions 2026-10-28)

- **Production service.** Move the 002 spike into a production insertion service.
  - The logic (state machine and orchestration) lives in **Presentation**. It is unit-tested on
    Linux against a fake Win32 boundary (`ITargetProbe`, `IPasteDispatcher`).
  - **App** provides the Win32 implementation by refactoring `ClipboardPasteInserter`,
    `ElevationProbe` and the `ForegroundTarget` checks.
- **Checks before dispatch.** The target window still exists, still belongs to the captured PID, is
  not a Smart Snippets window or process, and is not elevated while we are not. Never pick another
  window.
- **Steps.** 006 has already hidden the popup. Then:
  1. Restore focus to the target, handling the foreground lock as the spike did.
  2. Release held modifiers.
  3. Snapshot the copyable clipboard formats. **If the snapshot is incomplete and the caller did
     not allow clipboard loss, stop before `EmptyClipboard`** → `ClipboardNotPreserved`, nothing
     changed (user decision, finding 1).
  4. Set Unicode text, excluded from clipboard history.
  5. Re-check, as late as possible, that the target HWND is foreground **and** belongs to the
     captured PID (finding 3).
  6. Send Ctrl+V with `SendInput`. Classify the result as `NotDispatched`, `Dispatched` or
     `Uncertain` (finding 2).
  7. Restore the clipboard after 250 ms.
  - Retry when the clipboard is busy.
- **"Paste anyway" (finding 1).** The popup (006) asks the user after `ClipboardNotPreserved`. On
  **Paste anyway** it calls `InsertAsync` again with `AllowClipboardLoss = true`. The engine
  re-validates the target and restores focus:
  - If that works, it pastes, restoring whatever formats it could save → `Inserted`, usage
    counted.
  - If it cannot re-target (closed, focus restore failed, foreground lost at the last gate), it
    puts the snippet on the clipboard and **leaves it there** → `CopiedForManualPaste` with a
    message to paste it manually with Ctrl+V. No usage (we did not dispatch).
- **Uncertain dispatch (finding 2).** `SendInput` injecting fewer events than requested, or an
  exception escaping the paste call, is `Uncertain`: send key-ups for Ctrl and V, record **no**
  usage, restore the clipboard after the delay, return `MaybePasted` ("Smart Snippets may have
  pasted into <App>. Check it before trying again."). 006 shows it but never as a retryable error.
- **Restore failure after a paste (finding 1).** The result stays `Inserted` with a non-empty
  `Message` ("Pasted, but your previous clipboard couldn't be fully restored."); the App shows it
  as a tray notice (006 / AppHost).
- **Line endings.** The stored text is unchanged. At paste time, lone CR and lone LF become CRLF.
  This is a pure function in Core, with tests for mixed endings, CRLF left untouched, and empty
  input.
- **Single-flight.** A second call while one is running returns `Busy` with no side effects: no
  paste and no usage.
- **Usage.** `store.RecordUsage(id)` runs only when the outcome is `Inserted` (dispatch
  `Dispatched`). A failure, an uncertain dispatch, a manual-paste fallback or a cancellation never
  increments it. If `RecordUsage` fails after a successful paste, the failure is logged and the
  result is still `Inserted` (decided in §4.2).
- **Messages (in `UiText`).** One actionable message per outcome (§4.3.3). The popup (006) shows
  them in its footer with **Copy text**, **Restart as administrator** for `TargetElevated` (through
  the existing `IElevation`), and **Paste anyway / Cancel** for `ClipboardNotPreserved`.
- **Logs.** The outcome plus ids, counts and timings only.
- **Prototype cleanup.** Remove the prototype `MessageBox` path and `TestString` once 006 is wired.
  006 removes the prototype popup, so the two plans coordinate this.
- **Acceptance (master §2):**
  - **AC9:** Unicode and multiline text preserved; selection replaced normally; exactly once;
    never into a Smart Snippets window; clipboard restored (or the user is asked / told first);
    actionable errors; usage counted only on success.
  - **AC10:** elevated targets detected with a message; "Restart as administrator" works. The
    restart itself was built in 004; 007 provides the detection and the outcome that triggers the
    button.
- **Out of scope:**
  - Running the compatibility matrix (008).
  - Popup UI: footer, buttons, the "Paste anyway?" prompt, re-showing the popup, the tray notice
    (006 / AppHost).
  - Tray icon state and its precedence (008 owns icons; `HotkeyUnavailable` (red) wins over
    `PopupOpen` (blue)). 007 never sets tray state.
  - Typing text with `SendInput` Unicode keystrokes as a fallback (see §4.2).

## 3. Current state (as-built at `4861b0e`)

**Spike insertion path (App, Win32):**
- **Entry point and constants.** `src/SmartSnippets.App/Services/ClipboardPasteInserter.cs:12` is
  the only implementation of `IInserter`.
  - `:14` sets `RestoreDelay` to 250 ms.
  - `:16-18` set the focus timeout to 500 ms, and clipboard retries to 10 attempts 20 ms apart.
- **Logging and cancellation.**
  - `:27-32` `InsertAsync` logs `insert_result <Outcome>`.
  - The `CancellationToken` is accepted but never observed: `:34` does not use `ct`, and `:85`
    waits on `CancellationToken.None`.
- **Check order.**
  - `:36-39` run the elevation probe **before** the window-exists check (`:41-44`). For a closed
    target whose process is still alive, the probe runs needlessly. For a closed elevated target,
    the user gets the elevation message instead of "closed".
  - `:41` checks only `IsWindow(hwnd)`. There is no re-check that the HWND still belongs to
    `target.ProcessId` (HWND reuse), and no re-check that it is not our own process.
- **Focus.**
  - `:46-49` → `RestoreFocusAsync` (`:94-128`): first `SetForegroundWindow` directly, then the
    `AttachThreadInput` fallback, each followed by `WaitForForegroundAsync` (`:130-144`, ≤ 500 ms
    polling every 10 ms). The path is logged as `focus_path direct|attach_thread_input|failed`.
- **Clipboard swap.**
  - `:51-54`, `:146-159` `TryOpenClipboard` retries with **`Thread.Sleep` on the UI thread**. The
    002 report already lists this as a limitation handed to 007.
  - `:56-68` open the clipboard, then `TakeSnapshot` (`:174-222`), `EmptyClipboard` and `SetText`
    (`:224-238`), then close. **`EmptyClipboard` runs whatever the snapshot missed** (finding 1).
  - The snapshot copies every `HGLOBAL` format, skipping GDI and owner-display formats
    (`IsSkippedFormat`, `:161-172`, which also skips `CF_ENHMETAFILE`). A format whose data can't be
    read is silently counted as skipped. It has no size cap. `GetClipboardData` on a delayed-render
    format makes the clipboard owner render it synchronously.
  - `SetText` writes `CF_UNICODETEXT` exactly as given (`:226`), so there is **no line-ending
    normalization**. It adds the `ExcludeClipboardContentFromMonitorProcessing` marker (`:231-235`).
- **Paste.**
  - `:72-75`: if the text could not be set, the spike reports `ClipboardBusy`, which is the wrong
    outcome for an allocation failure.
  - `:77` `ReleaseHeldModifiers` (`:302-318`) sends a key-up for each held Shift, Ctrl, Alt, LWin
    or RWin. It runs **after** the clipboard swap. A lone synthetic Alt-up or Win-up can open the
    target's menu bar or the Start menu (risk R5).
  - `:79-82` `SendPaste` (`:320-330`) sends Ctrl down, V down, V up, Ctrl up. It reports failure
    whenever `SendInput` returns fewer than 4 events, even though Ctrl+V may already have been
    delivered (finding 2), and it leaves an injected Ctrl or V down. There is **no foreground
    re-check** between focus restore and the keystroke (finding 3).
- **Restore.**
  - `:84-91` await the restore delay, then restore in a `finally`.
  - `RestoreClipboard` (`:267-300`) always overwrites the clipboard. It does not check whether the
    user or the target changed it in the meantime (no `GetClipboardSequenceNumber`). The restored
    data carries no exclusion marker, so it re-enters clipboard history as a new entry. A restore
    failure is only logged.
- **Outcomes and messages.**
  - `src/SmartSnippets.App/Services/IInserter.cs:3-17` defines the App-internal
    `InsertOutcome { Success, TargetClosed, TargetElevated, FocusFailed, ClipboardBusy,
    SendInputFailed }`, `InsertResult`, and `IInserter`.
  - Messages are hard-coded in English at `ClipboardPasteInserter.cs:38,43,48,53,74,81` and do not
    match the design wording.
- **Elevation probe.**
  - `src/SmartSnippets.App/Services/ElevationProbe.cs:14-56` `IsBlocked(ForegroundTargetInfo)`:
    - Never blocked when we are elevated (`:16-19`).
    - `OpenProcess` denied → blocked (`:36-37`).
    - `ERROR_INVALID_PARAMETER` (process gone) → not blocked (`:30-34`).
    - Otherwise it compares integrity RIDs (`:48`).
  - `IsOwnProcessElevated` (`:58-81`) is also used by `ElevationService.cs:19`.
- **Target capture.**
  - `src/SmartSnippets.App/Services/ForegroundTarget.cs:7` defines
    `ForegroundTargetInfo(HWND, ProcessId, HMONITOR)`.
  - `Capture()` (`:11-27`) rejects our own process (`:20`). It is called only from
    `AppHost.cs:219`.
  - **006 owns this file** (it adds `DisplayName` capture). 007 does not edit it.
- **Prototype popup.** `src/SmartSnippets.App/Popup/PopupWindow.xaml.cs`:
  - `:18` defines `TestString`.
  - `:28` and `:123-154` are a per-window `_inserting` re-entrancy guard.
  - `:139` hides the popup, then `:140` inserts `TestString`.
  - Failures go to a Win32 `MessageBox` (`:156-163`).
  - The constructor takes an `IInserter` (`:30`).
- **Composition.**
  - `src/SmartSnippets.App/AppHost.cs:148` builds
    `new PopupWindow(new ClipboardPasteInserter(messageWindow.Handle))`.
  - `:143` builds `ElevationService`.
  - `:252-263` `RestartAsAdministrator` shuts down on `Started`.
  - `ReleaseResources` (`:320-339`) has no step to flush a pending clipboard restore.
- **Call sites (grep-verified).** `IInserter`, `ClipboardPasteInserter`, `TestString` and
  `ElevationProbe.IsBlocked` are referenced only from the files above.

**Store (Core), verified for the usage path:**
- **Threading.** `src/SmartSnippets.Core/Store/ISnippetStore.cs:7`: the store is single-threaded
  and must be called on the UI thread (a Debug build asserts it).
- **API.** `:17` `Snippets` is the in-memory snapshot (lookup by id, no I/O). `:38-39`
  `RecordUsage(long id)` is documented as "leaves updated_at untouched".
- **`RecordUsage` behaviour.** `src/SmartSnippets.Core/Store/SnippetStore.cs:239-258`:
  - An unknown id → `StoreResult.Fail(SnippetNotFound)`, with no event.
  - Otherwise `Write("record_usage", …)`. `Write` (`:298-305`) opens one connection, calls
    `BeginTransaction` (`:301`) and commits. A database failure is logged with the operation name
    only and thrown as `StoreException` (`Guarded`, `:283-296`; `ToStoreException`, `:262`).
  - On success it publishes a new snapshot and raises `UsageRecorded`.
- **SQL.** `src/SmartSnippets.Core/Data/SnippetRepository.cs:115-124` runs
  `UPDATE snippets SET usage_count = usage_count + 1, last_used_at = $now WHERE id = $id`, which
  **does not touch `updated_at`**.
- **Existing tests.**
  - `tests/SmartSnippets.Core.Tests/Store/SnippetStoreTests.cs:299`
    `RecordUsage_CountsUse_LeavesUpdatedAt`, `:319` `RecordUsage_UnknownId_FailsWithoutEvent`, and
    `:384` (busy database → `StoreException`, no event).
  - `tests/SmartSnippets.Core.Tests/Data/SnippetRepositoryTests.cs:88`
    `RecordUsage_IncrementsAndSetsLastUsed_KeepsUpdatedAt`.
  - Nothing in 007 changes Core persistence.
- **Fake store.** `tests/SmartSnippets.Presentation.Tests/Fakes/FakeSnippetStore.cs:189`
  implements `RecordUsage` with the same not-found rule and supports a `Failure` (`:28`) that
  throws.

**Presentation services and text:**
- `src/SmartSnippets.Presentation/Services/IElevation.cs:12-18` defines `IsElevated` and
  `RestartAsAdministrator()`. On `Started`, the caller exits (`:16`).
- `IClipboardService.cs:4-7` defines `SetText`, already used for "Copy text" on save failures. The
  App implementation, `ClipboardService.cs:12-32`, swallows `COMException`.
- `src/SmartSnippets.Presentation/Text/UiText.cs:7` is the design-wording class. `:49` already
  defines `CopyText = "Copy text"`. It has no insertion or elevation strings yet.
- `src/SmartSnippets.Core/Logging/AppLog.cs:9-54` is a static file log. `Error` appends the
  exception type and message. `tests/SmartSnippets.Core.Tests/Logging/AppLogTests.cs:38` shows the
  pattern for asserting on log content from a temporary directory.
- Tests already use a hand-written `TimeProvider` fake
  (`tests/SmartSnippets.Presentation.Tests/Fakes/FakeClipboardService.cs:13`). No timer-capable
  fake exists.

**Evidence from earlier work:**
- `docs/research/002-verification-plans-003-009.md:53-56`, **WARN D1.eol:** WinUI `TextBox`
  stores multi-line text with **lone CR**. Pasted as-is, many targets (terminals, some web inputs)
  would get no line breaks or the wrong ones. That report asks 007 to decide; the user chose to
  normalize at paste time.
- `docs/research/001-tech-validation-report.md` (gate G-4) measured:
  - focus restore taking the `direct` path;
  - 6 of 6 formats restored;
  - a 250 ms restore delay enough for Notepad, Chrome, Word and Windows Terminal.

**CI:**
- `.github/workflows/ci.yml:3-11`: the Windows job runs on pushes to `main` (except
  documentation-only changes) and on manual `workflow_dispatch`. App code (WinUI and CsWin32) is
  compiled nowhere else.

## 4. Design

### 4.1 Approach

- **The orchestration moves to Presentation as `InsertionService : IInsertionService`.** It owns:
  - the single-flight guard;
  - the snippet lookup;
  - line-ending normalization;
  - the ordered steps, the clipboard retry loop and the restore delay;
  - the "stop before `EmptyClipboard`" rule and the `AllowClipboardLoss` path;
  - the dispatch classification (`NotDispatched` / `Dispatched` / `Uncertain`) and key cleanup;
  - the guarantee that a swapped clipboard is restored (except on the manual-paste fallback);
  - the usage update;
  - the mapping from outcome to message;
  - logging.
- **Win32 work goes behind two narrow interfaces, implemented in App:**
  - **`ITargetProbe`** answers "can we paste into this target right now?" (closed / own process /
    elevated / ok) and "is this exact target (HWND **and** PID) the foreground window?".
    `Win32TargetProbe` is built from the checks in `ClipboardPasteInserter:36-44`, `ElevationProbe`
    and the own-process rule of `ForegroundTarget.Capture`. It adds the HWND-to-PID check.
  - **`IPasteDispatcher`** does one Win32 action per call: restore focus, release modifiers, one
    attempt to snapshot-and-swap the clipboard (which refuses to empty an incompletely saved
    clipboard unless allowed), one attempt to place text without a snapshot, send Ctrl+V, release
    the paste keys, one attempt to restore the clipboard. `ClipboardPasteDispatcher` is
    `ClipboardPasteInserter` cut into these methods. Its snapshot/set/restore and `SendInput` code
    is kept. `Thread.Sleep` and all decisions move to the service.
- **`LineEndings.ToCrLf`** is a pure function in Core (`SmartSnippets.Core.Domain`), next to the
  other text rules. It is tested in `Core.Tests`.
- **Messages** come from new `UiText` insertion strings. `InsertionResult.Message` is ready to show
  as-is. It is empty only for a clean `Inserted`.
- **AppHost** builds the service once and hands it to 006's popup view-model. This replaces the
  interim adapter 006 uses until 007 lands. `IInserter`, `ClipboardPasteInserter`, `TestString` and
  the `MessageBox` path are gone at the end.

**Ordering inside `InsertAsync`** (full branches in §4.4). `loss` is `options.AllowClipboardLoss`.

1. **Guard.** If another call is in flight, return `Busy` with no side effects.
2. **Resolve.** Find the snippet in `store.Snippets` by id. If it is missing → `SnippetMissing`.
3. **Normalize.** `text = LineEndings.ToCrLf(snippet.Text)`. The store copy is never changed.
4. **Check.** `probe.Check(target)`:
   - `Closed` → `TargetClosed`; with `loss`, the **manual-paste fallback** (below).
   - `OwnProcess` → `PasteFailed` (a defensive invariant, see §4.2), in both modes.
   - `Elevated` → `TargetElevated`, in both modes (the popup offers Restart as administrator).
5. **Focus.** `dispatcher.RestoreFocusAsync`. If it fails → `FocusFailed`; with `loss`, the
   manual-paste fallback.
6. **Modifiers.** `dispatcher.ReleaseHeldModifiers()`.
7. **Swap.** `dispatcher.TrySwapClipboard(text, allowIncomplete: loss)` is retried up to 10 times,
   20 ms apart, with an **awaited** delay.
   - `Busy` every time → `ClipboardBusy` (clipboard untouched).
   - `NotPreserved` (only when `!loss`) → `ClipboardNotPreserved` at once, no retry. The dispatcher
     closed the clipboard **without calling `EmptyClipboard`**, so nothing changed.
   - `Failed` → `PasteFailed` (restore if the clipboard was emptied).
   - `Swapped` → from here on, the restore is guaranteed by a `finally`, except on the manual-paste
     fallback at step 8.
8. **Last gate (synchronous, no `await` and no other call before step 9).** Check `ct` first, then
   `probe.IsTargetForeground(target)`: `GetForegroundWindow() == target.WindowHandle` **and**
   `GetWindowThreadProcessId(that HWND) == target.ProcessId`. If false → `FocusFailed` and an
   immediate restore; with `loss`, `CopiedForManualPaste`: the snippet is already on the clipboard,
   the snapshot is discarded and **not** restored. A residual race remains between this check and
   the moment Windows delivers the injected keys (§5 R1, accepted by the user 2026-10-28).
9. **Paste.** `dispatcher.SendPaste()` injects Ctrl↓ V↓ V↑ Ctrl↑ in one `SendInput` call and
   returns a `DispatchState`:
   - `Dispatched` (all 4 injected) → step 10.
   - `NotDispatched` (0 injected) → `PasteFailed`, immediate restore, no usage.
   - `Uncertain` (1–3 injected), **or any exception escaping `SendPaste`** → call
     `dispatcher.ReleasePasteKeys()` (V↑ then Ctrl↑, so no key stays down), record **no** usage,
     restore after the delay (the target may still read the clipboard), outcome `MaybePasted`.
   - An exception from any earlier step is `NotDispatched` → `PasteFailed`.
10. **Usage.** Only for `Dispatched`: `store.RecordUsage(id)`, on the caller's (UI) context. A
    failure is logged, and the result stays `Inserted`.
11. **Restore.** After `Dispatched` or `Uncertain`, wait `RestoreDelay` (250 ms); after any other
    failure, restore at once. Then call `TryRestoreClipboard` up to 10 times, 20 ms apart (retry
    only on `Busy`).
    - `Restored` or `ClipboardChanged` (the user or target copied something meanwhile; their data
      wins) → no notice.
    - `Partial` (some saved formats could not be set), `Failed`, or `Busy` after 10 attempts → the
      result carries the **restore notice**: `Inserted` gets the message "Pasted, but your previous
      clipboard couldn't be fully restored."; a failure outcome gets its own message plus "Your
      previous clipboard couldn't be fully restored."; `MaybePasted` keeps its message (the notice
      is logged only, to keep that message short and unambiguous).
    - On the `loss` path, formats that were never saved are the loss the user accepted; they do not
      trigger the notice. Only a failure to restore what *was* saved does.
12. **Result.** Log `insert_result` and return.

**Manual-paste fallback (`loss` only, target closed or focus not restored before the swap).**
`dispatcher.TryPlaceText(text)` (open, empty, set text + exclusion markers, close; no snapshot) is
retried like the swap. `Swapped` → `CopiedForManualPaste` (the text stays on the clipboard; no
restore, no usage). `Busy` ×10 → `ClipboardBusy`. `Failed` → `PasteFailed`. The user's previous
clipboard is lost on this path; they accepted that with Paste anyway.

**Threading.** `InsertAsync` is called on the UI thread, and every `await` resumes there. The
service never uses `ConfigureAwait(false)`, so `store.RecordUsage` runs on the store's owner
thread (`ISnippetStore.cs:7`), and `OpenClipboard`/`CloseClipboard` stay on one thread. No await
happens while the clipboard is open: each dispatcher call opens and closes it synchronously.

**Cancellation.** `ct` is honoured only **before the paste keystroke** (checked before steps 4, 5, 7
and at the start of step 8). Cancelling there throws `OperationCanceledException` after restoring
the clipboard if it was swapped. It records no usage and logs `insert_result Cancelled`. From
`SendPaste` on, `ct` is ignored: the keys may already be on their way, so key cleanup, the usage
update and the restore always run. 006 has no cancel trigger today; this keeps the contract honest
without adding an enum value.

#### 4.1.1 What the snapshot saves, skips and refuses (finding 1)

The snapshot walks `EnumClipboardFormats` and sorts every format into one of three groups. Only the
third group makes the snapshot **incomplete**.

| Group | Formats | Why |
|---|---|---|
| **Saved** (bytes copied, restored with `SetClipboardData`) | Every `HGLOBAL` format: `CF_UNICODETEXT`, `CF_TEXT`, `CF_OEMTEXT`, `CF_LOCALE`, `CF_DIB`, `CF_DIBV5`, `CF_HDROP`, `CF_RIFF`, `CF_WAVE`, `CF_TIFF`, `CF_SYLK`, `CF_DIF`, `CF_PENDATA`, `CF_DSPTEXT`, and all registered formats (`0xC000`–`0xFFFF`: HTML Format, Rich Text Format, PNG, Office and browser formats, our own exclusion markers). **`CF_ENHMETAFILE`** is saved too, through `GetEnhMetaFileBits` / `SetEnhMetaFileBits` (new: the spike skipped it). | This is the user's data. Office, browsers and drawing apps put vector pictures in `CF_ENHMETAFILE`, sometimes as the only picture format, so skipping it would be silent loss. |
| **Skipped by design** (counted as `skipped_by_design`, never prompt) | `CF_BITMAP`, `CF_PALETTE`, `CF_METAFILEPICT`; `CF_OWNERDISPLAY`; `CF_DSPBITMAP`, `CF_DSPMETAFILEPICT`, `CF_DSPENHMETAFILE`; the private ranges `CF_PRIVATEFIRST`–`CF_PRIVATELAST` (`0x0200`–`0x02FF`) and `CF_GDIOBJFIRST`–`CF_GDIOBJLAST` (`0x0300`–`0x03FF`). | **Synthesized:** Windows derives `CF_BITMAP` (and its palette) from the saved `CF_DIB`/`CF_DIBV5`, and `CF_METAFILEPICT` from the saved `CF_ENHMETAFILE`, so they come back on their own after the restore. **Owner display:** `CF_OWNERDISPLAY` has no data; the owner paints it on request, and stops being the owner once we write the clipboard. **Display variants and private handles:** the `CF_DSP*` picture formats and the private/GDI ranges are handles whose meaning only the owning process knows (GDI handles are deleted by Windows when the clipboard is emptied); their content is not portable bytes, and the owner's real data is in its registered formats, which are saved. |
| **Cannot preserve** (counted as `unsaved`; makes the snapshot incomplete) | Any format from the first group whose data can't be read: `GetClipboardData` returns null (including a delayed-render owner that fails or times out), `GlobalSize` is 0, `GlobalLock` fails, `GetEnhMetaFileBits` fails; **and every format past the 64 MB total cap**. | We would empty data we cannot put back. Without `AllowClipboardLoss`, the dispatcher closes the clipboard **without `EmptyClipboard`** and returns `NotPreserved`. With it, the saved formats are restored later and the unsaved ones are lost, as the user accepted. |

The snapshot is read entirely before deciding, so the decision never depends on enumeration order.
An empty clipboard gives a complete snapshot with 0 formats.

### 4.2 Alternatives considered

| Topic | Option | Verdict |
|---|---|---|
| Where the logic lives | **Orchestration in Presentation behind `ITargetProbe` + `IPasteDispatcher`; Win32 in App** | **Chosen.** The rules that matter for AC9 (exactly once, never another window, usage only on success, restore or ask, Busy without side effects) become Linux unit tests. App code only compiles on `main`'s Windows CI, so logic left there would be verified only by hand. |
| | Keep `IInserter` in App, harden it in place | Rejected. It can only be verified manually, and every rule would need a runbook step. It also keeps App-internal outcome types that 006 cannot consume from Presentation. |
| | One fat `IWin32Insertion.InsertAsync(hwnd, text)` boundary | Rejected. The ordering, retries and restore guarantee would stay untestable inside it. |
| Incomplete clipboard snapshot | **Stop before `EmptyClipboard` → `ClipboardNotPreserved`; the popup asks "Paste anyway?" and retries with `AllowClipboardLoss`** | **Chosen by the user (2026-10-28, finding 1).** Nothing is lost without consent, and the user can still paste. |
| | Empty anyway, restore what was saved, log the rest (spike behaviour) | Rejected (finding 1): silent data loss. |
| | Refuse with no override | Rejected: the user could never insert while holding, for example, a huge image. |
| "Paste anyway" cannot re-target | **Put the snippet on the clipboard and leave it there → `CopiedForManualPaste`, no usage** | **Chosen by the user.** The user already accepted losing the clipboard, and the text is one Ctrl+V away. Restoring the old clipboard would leave them with nothing. |
| Restore fails after a paste | **`Inserted` with a notice message, shown as a tray notice** | **Chosen by the user.** The paste worked and must not look retryable; the user still learns their clipboard changed. |
| Partial `SendInput` / exception during it | **`Uncertain` → key-ups for Ctrl and V, no usage, `MaybePasted`, restore after the delay, no retry offered** | **Chosen by the user (finding 2).** Treating it as a failure invites a duplicate paste; treating it as success could count a paste that never happened. Restoring after the delay (not at once) avoids pasting the user's old clipboard if the keys did arrive. |
| Final foreground gate | **HWND foreground and PID match, checked synchronously right before `SendInput`; residual race accepted** | **Chosen by the user (finding 3).** Windows has no atomic "send input only to this window" API for Ctrl+V; the gate shrinks the window to the time between two calls. |
| | Gate inside `ClipboardPasteDispatcher.SendPaste` | Rejected: it would gain microseconds but move the rule into untestable App code. The service calls the gate and `SendPaste` back to back with nothing in between, and a test asserts that order. |
| Line endings | **Normalize lone CR / lone LF to CRLF at paste time; stored text unchanged** | **Chosen by the user.** PRD §3.2 says not to modify stored text. CRLF is the Windows clipboard convention for `CF_UNICODETEXT`, and the WinUI `TextBox` already turns CRLF into CR when editing (D1.eol), so storing CRLF would not last. |
| | Normalize on save | Rejected by the user. It changes stored text, and the editor would turn it back into CR on the next edit anyway. |
| | Paste as-is | Rejected. Lone CR breaks multi-line paste in several targets (D1.eol). |
| Normalizer location | **Core `Domain/LineEndings`** | **Chosen.** It is a pure text rule like `TagList` and `LabelSuggester`, already tested cross-platform in `Core.Tests`, and reusable later (for example by an export). |
| Insertion mechanism | **Clipboard + Ctrl+V** (master decision) | **Chosen.** Proven in 002. It keeps Unicode and the target's normal selection replacement. |
| | Type the text with `SendInput` `KEYEVENTF_UNICODE` as a fallback | **Out of scope.** Slow for long text, breaks with IME and auto-indent and auto-complete editors, and has no clipboard to restore. Could be a later fallback for `ClipboardBusy`; not in the G1 brief. |
| | Detect consumption with delayed rendering (`SetClipboardData(NULL)` + `WM_RENDERFORMAT`) instead of a fixed delay | Deferred. It would tell us when the target read the text, but needs the message window's WndProc to render on demand, and some targets render several formats or never ask. Revisit only if 008's matrix shows that 250 ms is too short. |
| Usage update | **Inside the service, right after a `Dispatched` `SendPaste`** | **Chosen.** One place enforces "only after dispatch". Recording before the restore delay means an app exit during the delay still counts the paste that already happened. |
| | Popup records usage when `Succeeded` | Rejected. Two consumers (the popup and any future caller) would each need to remember the rule, and the "exactly once" test would have to live in 006. |
| `RecordUsage` fails after a paste | **Log `usage_record_failed` (id + failure kind) and keep `Inserted`** | **Chosen.** The text is already in the target and can't be taken back. Reporting a failure would invite the user to press Enter again, which inserts twice and breaks AC9's "exactly once". |
| Own-process target at dispatch | **Map to `PasteFailed`** (message: generic paste failure) and log `target_rejected reason=own_process` | **Chosen.** The contract has no dedicated value. `ForegroundTarget.Capture` already rejects our own process (`:20`), so this is reachable only through a capture bug. |
| Elevated target on the "Paste anyway" call | **`TargetElevated`, as on the first call** | **Chosen (assumption, §9 Q8).** Elevation does not change between the two calls in practice, and Restart as administrator is the useful action, not a manual paste. |
| Single-flight | **One `Interlocked` flag on the service instance** | **Chosen.** It covers double Enter, Enter plus a click, and any second caller, independently of the popup's own guard. |
| Restore when the clipboard changed in the meantime | **Skip the restore** (compare `GetClipboardSequenceNumber` with the value after our swap) | **Chosen.** If the user or the target copied something during the 250 ms, overwriting it would lose their data. No notice: the clipboard holds what they just copied. |
| Restored data in clipboard history | **Restore with the exclusion marker added** | **Chosen.** The original content is already in history (or deliberately kept out of it), so restoring it should not create a duplicate entry. |
| Foreground unlock | **Spike path: direct `SetForegroundWindow` while we still hold the foreground, then `AttachThreadInput`** | **Chosen.** Verified in 002 (`focus_path direct`). |
| | Synthetic Alt keypress to unlock `SetForegroundWindow` | Rejected. It activates menus in the target and conflicts with releasing modifiers. |

### 4.3 Data / config / API model

No schema, settings or migration changes.

#### 4.3.1 Shared contract with 006 (T0, coordinator, verbatim)

One file, written by the coordinator in T0 (not by a track):
`src/SmartSnippets.Presentation/Services/InsertionContracts.cs`. Neither 006 nor 007 changes it
without updating both plans.

```csharp
// src/SmartSnippets.Presentation/Services/InsertionContracts.cs (coordinator, T0)
public sealed record InsertionTarget(nint WindowHandle, uint ProcessId, nint MonitorHandle, string DisplayName);
public enum InsertionOutcome
{
    Inserted,              // dispatched; usage counted; Message empty, or a notice (e.g. clipboard not fully restored)
    TargetClosed, TargetElevated, FocusFailed, ClipboardBusy, PasteFailed, Busy, SnippetMissing, // not dispatched; nothing changed
    ClipboardNotPreserved, // not dispatched; clipboard untouched; caller may retry with AllowClipboardLoss
    MaybePasted,           // uncertain dispatch; no usage; do not offer retry
    CopiedForManualPaste,  // AllowClipboardLoss path could not re-target; snippet left on clipboard; no usage
}
public sealed record InsertionResult(InsertionOutcome Outcome, string Message) { public bool Succeeded => Outcome == InsertionOutcome.Inserted; }
public readonly record struct InsertionOptions(bool AllowClipboardLoss);
public interface IInsertionService
{
    // Resolves the snippet text from the store, normalizes line endings to CRLF, dispatches the paste,
    // and records usage only when Outcome == Inserted. Single-flight: a concurrent call returns Busy with no side effects.
    Task<InsertionResult> InsertAsync(InsertionTarget target, long snippetId, InsertionOptions options = default, CancellationToken ct = default);
}
```

`DisplayName` is used in messages. It can contain a window title, so it is **never logged**.

"Nothing changed" for the not-dispatched outcomes means the target received no keystroke and the
clipboard is as it was. The one exception is a restore that fails after a swap; that is reported in
`Message` (§4.1 step 11).

#### 4.3.2 Win32 boundary (007, Presentation, implemented in App)

```csharp
namespace SmartSnippets.Presentation.Services;

public enum TargetCheck { Ok, Closed, OwnProcess, Elevated }

public interface ITargetProbe
{
    // Closed: !IsWindow, or the HWND's PID != target.ProcessId (HWND reused), or the process is gone.
    // OwnProcess: the HWND belongs to this process. Elevated: target integrity > ours while we are not elevated
    // (OpenProcess/OpenProcessToken denied counts as elevated, as in the spike). Order: Closed, OwnProcess, Elevated.
    TargetCheck Check(InsertionTarget target);
    // Last gate: GetForegroundWindow() == target.WindowHandle && GetWindowThreadProcessId(that HWND) == target.ProcessId.
    bool IsTargetForeground(InsertionTarget target);
}

public enum FocusPath { Direct, AttachThreadInput }
public enum SwapStatus { Swapped, Busy, Failed, NotPreserved }
public enum RestoreStatus { Restored, Partial, Busy, ClipboardChanged, Failed }
public enum DispatchState { NotDispatched, Dispatched, Uncertain }

/// <summary>Opaque snapshot owned by the dispatcher; counts are for logs and the completeness rule (§4.1.1).</summary>
public interface IClipboardSnapshot
{
    int SavedCount { get; }
    int SkippedByDesignCount { get; }
    int UnsavedCount { get; }      // unreadable formats + formats past the 64 MB cap
    bool CapExceeded { get; }
    long ByteCount { get; }
    bool IsComplete => UnsavedCount == 0;
}

// Snapshot is non-null exactly when EmptyClipboard was called (Swapped, or Failed after the empty).
// NotPreserved: the clipboard was closed without EmptyClipboard; Snapshot is null.
public sealed record ClipboardSwap(SwapStatus Status, IClipboardSnapshot? Snapshot, int UnsavedCount = 0, bool CapExceeded = false);

public interface IPasteDispatcher
{
    Task<FocusPath?> RestoreFocusAsync(InsertionTarget target, CancellationToken ct); // null = focus not restored
    void ReleaseHeldModifiers();
    // One attempt: open, snapshot (§4.1.1). If incomplete && !allowIncomplete: close WITHOUT EmptyClipboard → NotPreserved.
    // Otherwise empty, set text + markers, close → Swapped.
    ClipboardSwap TrySwapClipboard(string text, bool allowIncomplete);
    SwapStatus TryPlaceText(string text);                  // one attempt: open, empty, set text + markers, close; no snapshot
    DispatchState SendPaste();                             // Ctrl↓ V↓ V↑ Ctrl↑ in one SendInput: 4 → Dispatched, 0 → NotDispatched, 1–3 → Uncertain
    void ReleasePasteKeys();                               // V↑ then Ctrl↑ (key-ups only; harmless if not down)
    RestoreStatus TryRestoreClipboard(IClipboardSnapshot snapshot); // one attempt; ClipboardChanged = sequence number moved
}

public sealed record InsertionTimings(
    TimeSpan RestoreDelay,          // 250 ms (002 measurement)
    int ClipboardAttempts,          // 10
    TimeSpan ClipboardRetryDelay)   // 20 ms
{
    public static InsertionTimings Default { get; } = new(TimeSpan.FromMilliseconds(250), 10, TimeSpan.FromMilliseconds(20));
}

// Presentation/Services/InsertionService.cs
public sealed class InsertionService(
    ISnippetStore store, ITargetProbe probe, IPasteDispatcher dispatcher,
    InsertionTimings? timings = null, Func<TimeSpan, CancellationToken, Task>? delay = null) : IInsertionService;
```

The internal configuration record is named `InsertionTimings` so it does not clash with the
contract's `InsertionOptions` (the per-call `AllowClipboardLoss` flag). The `delay` seam defaults to
`Task.Delay`. Tests pass a recording delay that completes at once, or one that waits on a
`TaskCompletionSource` (for the single-flight test). No new package is needed, and no
`TimeProvider` timer fake has to be written. `InsertionTimings` is not a user setting (master R3).

#### 4.3.3 Messages (`UiText`, insertion section)

`app` is `target.DisplayName`, or `"the target app"` when it is blank.

| Outcome | Text | Popup / App actions (006) |
|---|---|---|
| `Inserted` | *(empty)*. With the restore notice: `Pasted, but your previous clipboard couldn't be fully restored.` | Popup stays hidden. A non-empty message is shown as a tray notice (006 / AppHost). |
| `TargetClosed` | `Couldn't insert. {app} was closed. Nothing was pasted.` | Copy text |
| `TargetElevated` | `The target app is running as administrator. Smart Snippets can't insert text into it.` | Restart as administrator · Copy text |
| `FocusFailed` | `Couldn't switch back to {app}. Nothing was pasted. Click in {app} and try again.` | Copy text |
| `ClipboardBusy` | `Couldn't insert because another app is using the clipboard. Nothing was pasted. Try again in a moment.` | Copy text |
| `PasteFailed` | `Couldn't paste into {app}. Use Copy text and paste it yourself.` | Copy text |
| `SnippetMissing` | `Couldn't insert. This snippet was deleted. Nothing was pasted.` | — (no text to copy) |
| `Busy` | `Already inserting a snippet.` (006 may ignore it silently) | — |
| `ClipboardNotPreserved` | `Your clipboard holds something Smart Snippets can't save and put back. Nothing was pasted. Paste anyway? What's on your clipboard now may be lost.` | Paste anyway (→ `InsertAsync` with `AllowClipboardLoss: true`) · Cancel |
| `MaybePasted` | `Smart Snippets may have pasted into {app}. Check it before trying again.` | Informational only; **no retry** action |
| `CopiedForManualPaste` | `Couldn't paste into {app}. The snippet is on your clipboard. Paste it yourself with Ctrl+V.` | Informational (the text is already on the clipboard) |

- **Restore notice on failure outcomes:** the outcome text plus ` Your previous clipboard couldn't
  be fully restored.` (§4.1 step 11).
- **Button labels in the insertion section:** `RestartAsAdministrator = "Restart as administrator"`
  and `PasteAnyway = "Paste anyway"`. 006 uses them; `Cancel` and `CopyText` already exist or are
  006's.
- The `TargetClosed` and `TargetElevated` texts are the design's wording
  (`docs/design/001-smart-snippets-design.html`, "Dialogs and errors").

#### 4.3.4 Log events (ids, counts and timings only)

| Event | Detail |
|---|---|
| `insert_start` | `id=<snippetId> pid=<target pid> allow_loss=<bool>` |
| `target_check` | `result=Ok\|Closed\|OwnProcess\|Elevated` |
| `elevation_probe` | as today (`own=0x… target=0x… blocked=…`) |
| `focus_path` | `direct\|attach_thread_input\|failed` |
| `clipboard_swap` | `attempts=<n> status=Swapped\|Busy\|Failed\|NotPreserved saved=<n> skipped_by_design=<n> unsaved=<n> cap_exceeded=<bool> bytes=<n> ms=<n>` |
| `foreground_gate` | `ok=<bool>` |
| `paste_sent` | `state=Dispatched\|NotDispatched\|Uncertain injected=<n>` (the count is logged by the dispatcher) |
| `paste_keys_released` | after `Uncertain` |
| `manual_paste_fallback` | `reason=Closed\|FocusFailed\|ForegroundLost status=…` |
| `usage_record_failed` | `id=<id> kind=<SnippetNotFound\|DatabaseBusy\|…>` |
| `clipboard_restore` | `status=Restored\|Partial\|ClipboardChanged\|Busy\|Failed\|Skipped attempts=<n> failed_formats=<n>` (`Skipped` = manual-paste fallback) |
| `insert_result` | `outcome=<Outcome> id=<id> notice=<bool> ms=<elapsed>` |

Never logged: snippet text, label, tags, `DisplayName`, window titles, or `InsertionResult.Message`
(it contains `DisplayName`).

### 4.4 Sequence diagram: `InsertAsync`

```
006 popup VM (UI thread): Enter/click → popup hidden → insertion.InsertAsync(target, id, options, ct)
  InsertionService.InsertAsync                      (loss = options.AllowClipboardLoss)
  ├─ Interlocked flag already set? ─────────────────────────────► Busy            (nothing touched, not logged as failure)
  ├─ store.Snippets lookup(id) ── not found ────────────────────► SnippetMissing  (no Win32 call)
  ├─ text = LineEndings.ToCrLf(snippet.Text)                       (store copy untouched)
  ├─ ct cancelled? → throw OCE
  ├─ probe.Check(target)
  │    ├─ Closed (no window / HWND now another PID / gone) ── !loss ► TargetClosed   (no focus change, clipboard untouched)
  │    │                                                   ── loss ─► FALLBACK
  │    ├─ OwnProcess ──────────────────────────────────────────► PasteFailed     (log target_rejected)
  │    └─ Elevated ────────────────────────────────────────────► TargetElevated  (006: Restart as administrator → IElevation)
  ├─ ct cancelled? → throw OCE
  ├─ dispatcher.RestoreFocusAsync(target)   SetForegroundWindow → wait ≤500 ms → AttachThreadInput → wait ≤500 ms
  │    └─ null ── !loss ───────────────────────────────────────► FocusFailed     (clipboard untouched, never retargets)
  │            ── loss ────────────────────────────────────────► FALLBACK
  ├─ dispatcher.ReleaseHeldModifiers()
  ├─ ct cancelled? → throw OCE
  ├─ loop ≤10: dispatcher.TrySwapClipboard(text, allowIncomplete: loss)   (await 20 ms between attempts)
  │    ├─ Busy ×10 ────────────────────────────────────────────► ClipboardBusy   (clipboard untouched)
  │    ├─ NotPreserved (snapshot incomplete, !loss) ───────────► ClipboardNotPreserved (closed WITHOUT EmptyClipboard;
  │    │                                                           006 asks "Paste anyway?" → InsertAsync(…, AllowClipboardLoss: true))
  │    └─ Failed (alloc/SetClipboardData) ─────────────────────► PasteFailed     (restore if Snapshot != null)
  ├─ try                                                     (Swapped: snapshot held)
  │    ├─ ct cancelled? → throw OCE                                (finally restores)
  │    ├─ ── synchronous from here: no await, no other call ──
  │    ├─ probe.IsTargetForeground(target)  [HWND foreground AND PID match]
  │    │    └─ false ── !loss ─────────────────────────────────► FocusFailed     (finally restores at once; nothing pasted)
  │    │             ── loss ──────────────────────────────────► CopiedForManualPaste (snippet stays; snapshot discarded; no restore)
  │    ├─ dispatcher.SendPaste()          ← residual race (§5 R1): focus can still move in the ms before delivery
  │    │    ├─ NotDispatched (0 injected) ─────────────────────► PasteFailed     (finally restores at once; no usage)
  │    │    ├─ Uncertain (1–3 injected) or exception ──────────► MaybePasted     (ReleasePasteKeys: V↑ Ctrl↑; no usage;
  │    │    │                                                                     finally restores AFTER the delay)
  │    │    └─ Dispatched (4 injected): ct ignored from here
  │    └─ store.RecordUsage(id)  (UI thread, one transaction)
  │         ├─ Fail(SnippetNotFound) / StoreException ──► log usage_record_failed; outcome stays Inserted
  │         └─ Ok → store.Changed(UsageRecorded) → popup/main lists refresh
  │  finally (only when the clipboard was emptied, and not on the manual-paste branch)
  │    ├─ await RestoreDelay (250 ms) after Dispatched or Uncertain; failures restore at once
  │    └─ loop ≤10: dispatcher.TryRestoreClipboard(snapshot)
  │         ├─ Restored / ClipboardChanged (user data wins) → no notice
  │         └─ Partial / Failed / Busy ×10 → restore notice:
  │              Inserted → Message "Pasted, but your previous clipboard couldn't be fully restored." (tray notice)
  │              failure outcome → its message + notice sentence;  MaybePasted → logged only
  ├─ unexpected exception before SendPaste → log type only → PasteFailed (NotDispatched; finally already restored)
  └─ clear flag; log insert_result; return InsertionResult(outcome, UiText message)

FALLBACK (loss only; target closed or focus not restored before the swap):
  loop ≤10: dispatcher.TryPlaceText(text)   (open, empty, set text + markers, close; no snapshot)
    ├─ Swapped ──────────────────────────────────────────────► CopiedForManualPaste ("paste it yourself with Ctrl+V"; no usage)
    ├─ Busy ×10 ─────────────────────────────────────────────► ClipboardBusy
    └─ Failed ───────────────────────────────────────────────► PasteFailed
```

**Parallel paths checked:**
- `IInserter`/`ClipboardPasteInserter` is the only insertion path today: grep for
  `InsertAsync(`, `SendInput` and `SetClipboardData` in `src/` finds only `ClipboardPasteInserter.cs`
  and `PopupWindow.xaml.cs:140`.
- `ClipboardService.SetText` (Copy text) is a separate, intended path. It writes the clipboard on
  purpose, without restoring it, and stays unchanged. The manual-paste fallback uses the
  dispatcher's `TryPlaceText` instead, so the snippet keeps its history-exclusion markers.
- After T6, the grep must find `SendInput` and `SetClipboardData` only in
  `ClipboardPasteDispatcher.cs`, and `IInsertionService` implemented only by `InsertionService`.

### 4.5 Surface list

| Surface | Change | Task |
|---|---|---|
| `Presentation/Services/InsertionContracts.cs` (shared contract, §4.3.1) | new, **T0 coordinator** (not a track) | — |
| `Presentation/Services/InsertionBoundary.cs` (`ITargetProbe`, `IPasteDispatcher`, enums, `IClipboardSnapshot`, `ClipboardSwap`, `InsertionTimings`) | new | T1 |
| `Core/Domain/LineEndings.cs` + `Core.Tests/Domain/LineEndingsTests.cs` | new | T2 |
| `Presentation/Services/InsertionService.cs` + tests + fakes | new | T3 |
| `Presentation/Text/UiText.cs`: insertion section only | change | T4 |
| `App/Services/Win32TargetProbe.cs`, `App/Services/ClipboardPasteDispatcher.cs` | new (refactor) | T5 |
| `App/Services/ElevationProbe.cs`: `IsBlocked(uint pid)` instead of `ForegroundTargetInfo` | change | T5 |
| `App/Services/ClipboardPasteInserter.cs`, `App/Services/IInserter.cs` | **deleted** | T5/T6 |
| `App/AppHost.cs`: compose `InsertionService`, remove the interim adapter, flush the clipboard restore on exit | change | T6 |
| `App/Popup/PopupWindow.xaml.cs` prototype (`TestString`, `MessageBox`) | removed by 006; 007 verifies | T6, T7 |
| Popup footer, Copy text, Restart as administrator, "Paste anyway?" prompt, no-retry `MaybePasted` display, tray notice for `Inserted` with a message | **006 / AppHost** consume `InsertionResult` | — |
| Tray icon state and precedence (`HotkeyUnavailable` over `PopupOpen`) | **008**; untouched by 007 | — |

## 5. Impact and risk

| Question | Answer |
|---|---|
| State management: what survives a crash mid-operation? | A crash between swap and restore leaves the snippet text on the clipboard. It is excluded from clipboard history, and the user's previous clipboard is lost. That is accepted and logged (`clipboard_swap` without a following `clipboard_restore`). A normal Exit during the 250 ms delay is covered: T6 flushes a pending restore in `ReleaseResources`. The database is touched only by `RecordUsage`, in one transaction. |
| Data consistency: orphans, races, double-apply? | The single-flight flag prevents double paste and double usage. `RecordUsage` runs once, after a `Dispatched` paste only; `Uncertain` never counts. A snippet deleted after the lookup gives `SnippetNotFound` from `RecordUsage`, which is logged while the result stays `Inserted`. Store calls run on the UI thread (store contract). |
| User experience | The root problem (D1.eol lone CR) is fixed at paste time. Every failure says what happened, whether anything was pasted, and what to do next, plus Copy text so the text is never stuck. An unsaveable clipboard is never emptied without asking. An uncertain paste says "check it" instead of inviting a retry. The elevated case offers Restart as administrator. No surprise retargeting: a closed or changed target never receives the paste; on "Paste anyway" it gets the text on the clipboard instead. |
| Backward compatibility | No data or setting changes. Stored lone-CR text stays as it is and is pasted as CRLF. The App-internal `IInserter` types are removed; their only consumer is the prototype popup, which 006 removes. |
| Error recovery | Every not-dispatched failure leaves the target untouched and the clipboard restored or never touched, so the user can retry; a failed restore is reported in the message. `ClipboardNotPreserved` offers Paste anyway. `MaybePasted` is deliberately not retryable. `CopiedForManualPaste` leaves the text one Ctrl+V away. Busy is transient. |
| Logging & debugging | The §4.3.4 events cover each step with attempt counts, snapshot completeness, the dispatch state and injected count, the focus path and timings, enough to tell foreground-lock, clipboard-contention, delayed-render and UIPI problems apart in the field. Privacy is enforced by a test (T3). |
| Edge cases | Covered: empty snippet text (cannot be stored, per the validator; the service still handles `""` without error); huge text (R8); text with only `\r`; `\r\n` already; mixed endings; emoji and surrogate pairs; target closed during focus restore; HWND reused by another process; foreground taken by another window between swap and paste; target on another desktop or a locked screen (focus fails → `FocusFailed`); empty clipboard (complete snapshot); clipboard over 64 MB or with an unreadable format (`ClipboardNotPreserved`); Paste anyway after the target closed; double Enter; Enter during the restore delay; Exit during the restore delay. |

**Risks:**

- **R1 Foreground lock and the focus race (finding 3).** `SetForegroundWindow` can be refused once
  we no longer own the foreground, and focus can move to another window at any time.
  - **Mitigations:**
    - 006 hides the popup and calls `InsertAsync` in the same UI turn, so we still hold the
      last-input right (verified `direct` in 002).
    - The `AttachThreadInput` fallback stays.
    - The last gate checks the HWND **and** its PID, synchronously and immediately before
      `SendInput`, with no await or other call in between. A focus change detected there gives
      `FocusFailed` (or `CopiedForManualPaste` on the Paste-anyway call).
  - **Residual risk, accepted by the user (2026-10-28):** focus can still change in the few
    milliseconds between the gate and the moment Windows delivers the injected keys, and the paste
    then goes to the window that took focus. Windows offers no atomic "inject into this window"
    for Ctrl+V. This plan does **not** claim wrong-window pastes are impossible; it makes them
    require a focus change inside that short window.
- **R2 UIPI and elevation.** `SendInput` into a higher-integrity window fails silently. The
  documentation says neither the return value nor `GetLastError` reports it.
  - **Mitigations:**
    - Detect elevation up front, on every call including Paste anyway. Access denied counts as
      elevated (conservative). A target running as another user is reported as elevated: an
      acceptable false positive.
    - When we are elevated, nothing is blocked.
    - The message offers Restart as administrator through the existing `IElevation`, which keeps
      start-at-login un-elevated (master decision).
- **R3 Clipboard owners.**
  - Clipboard managers, RDP and Office hold the clipboard open briefly, so swap and restore retry
    10 × 20 ms with an awaited delay. The UI no longer freezes, unlike the spike's `Thread.Sleep`.
  - A clipboard that stays busy for 200 ms or more gives `ClipboardBusy` with "try again".
- **R4 Clipboard history and cloud sync.**
  - `ExcludeClipboardContentFromMonitorProcessing` keeps the snippet out of Win+V history, cloud
    clipboard and most well-behaved monitors.
  - T5 also sets `CanIncludeInClipboardHistory` and `CanUploadToCloudClipboard` to 0 as belt and
    braces (open question 5). The manual-paste fallback carries the same markers.
  - Third-party managers and the Office Clipboard may still capture it; this is recorded in 008.
  - The restore also carries the marker, so it creates no duplicate history entry.
- **R5 Releasing modifiers.** A synthetic Alt-up or Win-up with no other key in between opens the
  target's menu bar or the Start menu.
  - **Mitigation:** when Alt or Win is held, T5 first injects a no-op key (unassigned VK `0xE8`)
    down and up, then the key-ups. Order as in the brief: focus, then modifiers, then clipboard.
  - Runbook step 10 holds Alt or Win while pressing Enter.
- **R6 Clipboard preservation (finding 1).** Snapshotting calls `GetClipboardData` on every
  format. Delayed-render owners (Excel, Word, browsers) render each format synchronously: slow for
  big copies, and a hung owner blocks us until Windows times out the render.
  - **Mitigations:**
    - The §4.1.1 rules: only synthesized, owner-display and owner-private handle formats are skipped
      by design; `CF_ENHMETAFILE` is now saved.
    - A **64 MB total cap**. Exceeding it, or any unreadable format, makes the snapshot incomplete,
      and the engine **stops before `EmptyClipboard`** → `ClipboardNotPreserved` → "Paste anyway?".
    - A restore that fails after a paste is reported (tray notice), not only logged.
    - Log `saved`/`skipped_by_design`/`unsaved`/`bytes` and the swap time.
  - **Residual risk:** an app that announces formats it never renders would trigger the prompt on
    every insertion. 008's matrix records how often the prompt appears with common apps; if a
    common app triggers it falsely, the specific format is reviewed then (not pre-emptively
    whitelisted).
- **R7 Apps that read the clipboard late.** The restore delay can beat the target's read, so the
  user's old clipboard gets pasted instead of the snippet. Examples: RDP and Citrix sessions, VMs,
  and Electron apps under load.
  - The 250 ms delay comes from the 002 measurement (Notepad, Chrome, Word, Windows Terminal).
  - It is internal configuration (`InsertionTimings`). 008 runs the matrix and records the apps
    that need more.
  - The consumption-detection alternative (§4.2) is the escape hatch.
- **R8 Large text.**
  - A 1 MB+ snippet means a 2 MB+ `HGLOBAL`, which is fine.
  - Some targets take longer than 250 ms to read big text, the same issue as R7. 008 records a
    100 KB case.
  - Normalization is O(n). It returns the same string instance when nothing changes.
- **R9 Terminals.**
  - Windows Terminal binds Ctrl+V to paste by default.
  - conhost (cmd and PowerShell) supports Ctrl+V on Windows 10 and later with "Enable Ctrl key
    shortcuts", which is on by default.
  - Multi-line CRLF text pasted into a shell runs line by line. Windows Terminal shows its
    multi-line paste warning; that is the terminal's normal behaviour, not ours.
  - Terminals bound to Ctrl+Shift+V only (custom key bindings, WSL tmux) are out of scope and
    recorded in 008.
- **R10 Office.** Word and Outlook paste `CF_UNICODETEXT` as plain text in the destination
  formatting, which is expected. Outlook's "paste options" pop-up may appear. Recorded in 008.
- **R11 Concurrency.**
  - The service guard covers re-entrancy.
  - A hotkey press during an in-flight insertion would show the popup and steal focus in the
    middle of the paste. 006 must not show the popup while its own insertion task is pending
    (open question 2).
  - `RecordUsage` raising `Changed` re-sorts lists only; it does not interfere with the paste.
- **R12 Windows compile only on `main`.**
  - The App changes (T5, T6) are first compiled by CI after merge, or by a manual
    `workflow_dispatch` on the branch.
  - App edits are kept mechanical (move code, change signatures), and every rule sits in tested
    Presentation code.
- **R13 Uncertain dispatch (finding 2).** `SendInput` can inject part of Ctrl↓ V↓ V↑ Ctrl↑ (for
  example when another thread blocks input mid-call), and the target may or may not have pasted.
  - **Mitigations:** classify it as `Uncertain`; send V↑ and Ctrl↑ so no key stays down; count no
    usage; restore after the normal delay so a delivered paste still reads the snippet; return
    `MaybePasted` ("check it before trying again"), which 006 shows without a retry action.
  - **Residual risk:** if it did paste, usage is under-counted by one; accepted, because the
    alternative risks a duplicate paste. A forced partial `SendInput` cannot be produced by hand,
    so this path is verified by unit tests only (§7).

**Rollback:**
- Revert the T5 and T6 commits, and AppHost goes back to 006's interim adapter.
- Presentation and Core additions (T1–T4) are inert without wiring.
- No data migration to undo.

## 6. Tasks

**File ownership:**
- **007 owns:** the files in §4.5 rows T1–T6, and only the insertion section of `UiText.cs`.
- **006 owns:** `ForegroundTarget.cs`, `Popup/**`, the popup view-models, the "Paste anyway?" prompt,
  the tray notice for `Inserted` messages, and the rest of `UiText.cs`.
- **008 owns:** tray icons and the tray-state precedence.
- **Coordinator owns:** `Presentation/Services/InsertionContracts.cs` (T0).
- **Shared:** `AppHost.cs` is edited by 007 only in T6, after 006 has merged.

**T0 (coordinator, before either track):**
- Writes `src/SmartSnippets.Presentation/Services/InsertionContracts.cs` with §4.3.1 verbatim.
  Neither track writes or edits it.
- Adds empty `// Insertion (007)` and `// Popup (006)` section markers to `UiText.cs`, so the two
  tracks append without conflicts.

### T1 — Win32 boundary interfaces (AC9, AC10 enabler)
- **Files:** `src/SmartSnippets.Presentation/Services/InsertionBoundary.cs`.
- **Change:** the types in §4.3.2 (`ITargetProbe`, `TargetCheck`, `IPasteDispatcher`, `FocusPath`,
  `SwapStatus`, `RestoreStatus`, `DispatchState`, `IClipboardSnapshot`, `ClipboardSwap`,
  `InsertionTimings`), with XML docs stating the Win32 semantics the App must honour: the §4.1.1
  format groups, "no `EmptyClipboard` on `NotPreserved`", the `SendPaste` count mapping, and the
  PID check in `IsTargetForeground`.
- **Verify:** `dotnet build src/SmartSnippets.Presentation` builds with no warnings.
- **Test that dies if this breaks:** the T3 fakes implement these interfaces, so any signature
  drift fails to compile `SmartSnippets.Presentation.Tests`.

### T2 — Line-ending normalizer (AC9 multiline)
- **Files:** `src/SmartSnippets.Core/Domain/LineEndings.cs`,
  `tests/SmartSnippets.Core.Tests/Domain/LineEndingsTests.cs`.
- **Change:** `public static string ToCrLf(string text)`:
  - Each lone `\r` and each lone `\n` becomes `\r\n`.
  - Existing `\r\n` is kept.
  - It returns the **same instance** when the text has no lone CR or LF.
  - Every other character is untouched: tabs, trailing spaces, U+2028, surrogate pairs.
  - `null` throws `ArgumentNullException`.
- **Verify:** `dotnet test tests/SmartSnippets.Core.Tests --filter LineEndings`.
- **Tests that die if this breaks:**
  - `Empty_ReturnsEmpty`
  - `CrLf_Untouched_SameInstance`
  - `LoneCr_BecomesCrLf`
  - `LoneLf_BecomesCrLf`
  - `Mixed_AllBecomeCrLf` (`"a\r\nb\rc\nd"` → `"a\r\nb\r\nc\r\nd"`)
  - `CrCr_BecomesTwoCrLf`
  - `LfCr_BecomesTwoCrLf`
  - `CrLfLf_BecomesTwoCrLf`
  - `LeadingAndTrailingBreaks_Normalized`
  - `UnicodeTabsAndTrailingSpaces_Preserved` (the 002 `TestString` content)
  - `NoBreaks_SameInstance`
  - `Null_Throws`

### T3 — `InsertionService` state machine (AC9, AC10)
- **Files:**
  - `src/SmartSnippets.Presentation/Services/InsertionService.cs`
  - `tests/SmartSnippets.Presentation.Tests/InsertionServiceTests.cs`
  - `tests/SmartSnippets.Presentation.Tests/InsertionServiceIntegrationTests.cs`
  - `tests/SmartSnippets.Presentation.Tests/Fakes/FakeTargetProbe.cs`
  - `tests/SmartSnippets.Presentation.Tests/Fakes/FakePasteDispatcher.cs`. This is a scripted
    fake that records every call in order (with the text and `allowIncomplete` it received),
    returns scripted results per attempt, and can throw from any method. It models the clipboard
    as a list of formats with an `Emptied` flag, so tests can assert that `EmptyClipboard` was
    never reached.
- **Change:**
  - The §4.1 ordering, single-flight, cancellation rule, the stop-before-empty rule, the
    `AllowClipboardLoss` path and manual-paste fallback, the dispatch classification and key
    cleanup, the restore guarantee and notice, and the usage rule.
  - Messages through T4's `UiText`. Until T4 lands, `UiText` placeholders keep T3 compiling.
  - The §4.3.4 log events.
- **Verify:** `dotnet test tests/SmartSnippets.Presentation.Tests --filter Insertion` on Linux.
- **Tests that die if this breaks.** Each hop is named; each test also asserts no usage and no
  paste where relevant.

  *Store lookup and text:*
  - `Inserted_CallsStepsInOrder`: Check → focus → modifiers → swap → IsTargetForeground →
    SendPaste → delay(250) → restore; `Message` empty.
  - `Dispatch_ReceivesNormalizedText_StoreTextUnchanged`: stored `"a\rb"` → the dispatcher
    receives `"a\r\nb"`, and `store.Snippets` still holds `"a\rb"`. This is the store → normalizer
    → dispatcher hop.
  - `SnippetMissing_NoWin32Calls_NoUsage`.

  *Usage:*
  - `Inserted_RecordsUsageExactlyOnce_AfterSendPaste`. This is the dispatch → store hop.
  - `RecordUsage_StoreException_StillInserted_Logged`.
  - `RecordUsage_NotFound_StillInserted_Logged`.
  - `RecordUsage_RunsOnCallerSynchronizationContext`, using a single-thread test context. This
    is the UI-thread hop (`ISnippetStore.cs:7`).

  *Target checks:*
  - `TargetClosed_NoFocus_NoClipboard_NoUsage`.
  - `TargetElevated_NoFocus_NoClipboard_NoUsage_MessageMatchesDesign`.
  - `OwnProcess_PasteFailed_NeverDispatches`.

  *Focus and the last gate (finding 3):*
  - `FocusFailed_ClipboardUntouched_NoUsage`.
  - `ForegroundLostAfterSwap_FocusFailed_ClipboardRestoredImmediately_NoPaste`.
  - `ForegroundGate_SamePidRequired`: the fake probe reports the HWND foreground but a different
    PID → `FocusFailed`, no `SendPaste`.
  - `ForegroundGate_IsLastCallBeforeSendPaste`: in the recorded call log, `IsTargetForeground` is
    immediately followed by `SendPaste`, with no delay call in between.

  *Clipboard preservation (finding 1):*
  - `IncompleteSnapshot_NotPreserved_NoEmptyClipboard_NoPaste_NoUsage`: the fake reports one
    unreadable format → `ClipboardNotPreserved`; the fake clipboard was never emptied, still holds
    the original formats; no `SendPaste`, no restore call, no retry of the swap.
  - `OverCap_NotPreserved_NoEmptyClipboard`: `CapExceeded` → same as above; log has
    `cap_exceeded=true`.
  - `NotPreserved_PassesAllowIncompleteFalse_ByDefault`: `default(InsertionOptions)` →
    `TrySwapClipboard(text, false)`.
  - `AllowLoss_IncompleteSnapshot_Pastes_RestoresSaved_Inserted_UsageCounted`: second call with
    `AllowClipboardLoss: true` re-runs `Check` and focus, swaps with `allowIncomplete: true`,
    pastes, restores the saved formats; `Message` empty (the unsaved loss was accepted).
  - `AllowLoss_TargetClosed_PlacesText_CopiedForManualPaste_NoRestore_NoUsage`.
  - `AllowLoss_FocusFailed_PlacesText_CopiedForManualPaste_NoUsage`.
  - `AllowLoss_ForegroundLostAtGate_CopiedForManualPaste_SnapshotNotRestored_NoUsage`.
  - `AllowLoss_TargetElevated_StillTargetElevated_ClipboardUntouched`.
  - `AllowLoss_PlaceTextBusy_ClipboardBusy`.
  - `CopiedForManualPaste_MessageSaysCtrlV`.

  *Clipboard swap:*
  - `ClipboardBusy_TenAttempts_TwentyMsApart_Then_ClipboardBusy`.
  - `ClipboardBusy_SucceedsOnThirdAttempt_Inserted`.
  - `SwapFailed_AfterEmpty_RestoresSnapshot_PasteFailed`.

  *Dispatch states (finding 2):*
  - `SendPasteNotDispatched_RestoresWithoutDelay_PasteFailed_NoUsage_NoKeyCleanup`.
  - `SendPastePartial_Uncertain_MaybePasted_NoUsage_ReleasesPasteKeys_RestoresAfterDelay`.
  - `SendPasteThrows_Uncertain_MaybePasted_NoUsage_ReleasesPasteKeys_RestoresAfterDelay`.
  - `ExceptionBeforeDispatch_PasteFailed_NoKeyCleanup_ClipboardRestored_FlagCleared`
    (the fake throws from `TrySwapClipboard` after emptying, and separately from
    `IsTargetForeground`).
  - `Dispatched_NoPasteKeyCleanup` (`ReleasePasteKeys` is called only after `Uncertain`).
  - `MaybePasted_MessageNamesApp_SaysCheckBeforeTrying`.

  *Restore:*
  - `Restore_ClipboardChanged_Skipped_ResultStillInserted_NoNotice`.
  - `RestoreFailed_AfterPaste_InsertedWithNotice` (`Partial`, `Failed`, and `Busy` ×10 each →
    `Inserted`, `Message` = "Pasted, but your previous clipboard couldn't be fully restored.",
    usage counted once).
  - `RestoreFailed_AfterNotDispatchedFailure_MessageHasNoticeSentence`.
  - `RestoreFailed_AfterMaybePasted_MessageUnchanged_Logged`.

  *Single-flight:*
  - `SecondCallWhileRunning_ReturnsBusy_NoProbeNoDispatchNoUsage`. The first call is held at the
    restore delay with a `TaskCompletionSource`.
  - `AfterCompletion_NextCallRuns`, and `AfterFailure_NextCallRuns` (the flag is cleared).

  *Cancellation:*
  - `CancelledBeforeCheck_Throws_NoSideEffects`.
  - `CancelledAfterSwap_Throws_ClipboardRestored_NoUsage`.
  - `CancelledAfterPaste_Ignored_UsageRecorded_Restored`.

  *Messages and logs:*
  - `Message_PerOutcome_UsesDisplayName`, and `Message_BlankDisplayName_UsesFallback`.
  - `Logs_HaveOutcomeAndId_NeverTextOrDisplayName`. This uses `AppLog.Initialize` on a temporary
    directory, in a non-parallel xunit collection, and asserts that neither the snippet text nor
    a `DisplayName` sentinel appears in `app.log`.

  *Integration (real `SnippetStore` on a temporary SQLite database, fake boundary):*
  - `Integration_Inserted_PersistsUsage_LeavesUpdatedAt`.
  - `Integration_Failure_DoesNotPersistUsage` (reopen the store and compare), run for
    `PasteFailed`, `ClipboardNotPreserved`, `MaybePasted` and `CopiedForManualPaste`.

### T4 — Insertion messages (AC9 actionable errors, AC10 message)
- **Files:** `src/SmartSnippets.Presentation/Text/UiText.cs` (insertion section only),
  `tests/SmartSnippets.Presentation.Tests/InsertionMessagesTests.cs`.
- **Change:** the §4.3.3 strings, the `RestartAsAdministrator` and `PasteAnyway` constants, the
  restore-notice strings, and a helper `InsertionMessage(InsertionOutcome, string displayName,
  bool restoreNotice)` used by T3.
- **Verify:** `dotnet test … --filter InsertionMessages`.
- **Tests that die if this breaks:**
  - `TargetClosed_DesignWording` (`"Couldn't insert. Notepad was closed. Nothing was pasted."`)
  - `TargetElevated_DesignWording`
  - `ClipboardBusy_MentionsAnotherApp`
  - `ClipboardNotPreserved_AsksPasteAnyway`
  - `MaybePasted_DoesNotSayNothingWasPasted` (guards against a retry-inviting wording)
  - `CopiedForManualPaste_MentionsCtrlV`
  - `Inserted_Empty_WithoutNotice_NoticeTextWithIt`
  - `EveryNonInsertedOutcome_HasNonEmptyMessage` (iterates the enum, so a new value without a
    message fails)
  - `BlankDisplayName_Fallback`

### T5 — App Win32 boundary implementation (AC9, AC10)
- **Files:**
  - **new:** `src/SmartSnippets.App/Services/Win32TargetProbe.cs`,
    `src/SmartSnippets.App/Services/ClipboardPasteDispatcher.cs`
  - **change:** `src/SmartSnippets.App/Services/ElevationProbe.cs`
  - **delete:** `src/SmartSnippets.App/Services/ClipboardPasteInserter.cs`,
    `src/SmartSnippets.App/Services/IInserter.cs`. They are deleted in T6 if the interim adapter
    still references them at T5 time.
- **Change:**
  - **`Win32TargetProbe.Check`:** `IsWindow`, then `GetWindowThreadProcessId(hwnd) == target.ProcessId`
    (otherwise `Closed`), then `pid == GetCurrentProcessId()` (`OwnProcess`), then
    `ElevationProbe.IsBlocked(pid)` (`Elevated`).
  - **`Win32TargetProbe.IsTargetForeground`:** `GetForegroundWindow() == target.WindowHandle` and
    `GetWindowThreadProcessId(GetForegroundWindow()) == target.ProcessId`.
  - **`ElevationProbe.IsBlocked`:** takes `uint processId`. Its logic and log line are unchanged.
    `IsOwnProcessElevated` stays for `ElevationService.cs:19`.
  - **`ClipboardPasteDispatcher`:** moves `RestoreFocusAsync`, `TakeSnapshot`, `SetText`,
    `SetData`, `RestoreClipboard`, `ReleaseHeldModifiers` and `SendPaste` out of
    `ClipboardPasteInserter` into the one-attempt methods of §4.3.2. Changes and additions:
    - `TakeSnapshot` sorts formats into the §4.1.1 groups (replacing `IsSkippedFormat`): unreadable
      formats now count as `unsaved`, not skipped; `CF_ENHMETAFILE` is saved via
      `GetEnhMetaFileBits` and restored via `SetEnhMetaFileBits`; the 64 MB cap marks the rest
      `unsaved`.
    - `TrySwapClipboard(text, allowIncomplete)`: on an incomplete snapshot without
      `allowIncomplete`, `CloseClipboard` **without** `EmptyClipboard` → `NotPreserved`.
    - `TryPlaceText(text)`: open, empty, set text + markers, close.
    - `SendPaste` returns `DispatchState` from the `SendInput` count (4 / 0 / 1–3) and logs
      `paste_sent injected=<n>`. `ReleasePasteKeys` sends V↑ then Ctrl↑ in one `SendInput`.
    - `TryRestoreClipboard` returns `Partial` when at least one saved format failed to set.
    - `GetClipboardSequenceNumber` captured after the swap and compared before the restore.
    - The exclusion marker added on restore.
    - The `CanIncludeInClipboardHistory` and `CanUploadToCloudClipboard` = 0 markers.
    - The R5 no-op key before the Alt/Win key-up.
    - `RestorePendingNow()`, for exit (T6).
  - **Removed:** `Thread.Sleep`, and every outcome or message decision.
  - `ForegroundTarget.cs` is **not** edited (006 owns it).
- **Verify:**
  - A CI Windows job (merge to `main`, or `workflow_dispatch` on the branch) compiles and
    AOT-publishes with **0** IL2xxx/IL3xxx warnings.
  - Runbook §7 steps 1–16 on the CI artifact.
- **Test that dies if this breaks:** not unit-testable (Win32). It is guarded by the CI compile,
  the runbook, and the log lines `target_check`, `focus_path`, `clipboard_swap`, `foreground_gate`,
  `paste_sent` and `clipboard_restore` appearing in `app.log` during runbook step 1 (checked in T7),
  plus `clipboard_swap status=NotPreserved` in step 14.

### T6 — AppHost wiring, replacing 006's interim adapter (AC9, AC10)
- **Files:** `src/SmartSnippets.App/AppHost.cs`. Also `src/SmartSnippets.App/Services/IInserter.cs`
  and `ClipboardPasteInserter.cs`, deleted here if T5 could not delete them.
- **Change:**
  - In `Compose`, build `var dispatcher = new ClipboardPasteDispatcher(messageWindow.Handle);`
    and `var insertion = new InsertionService(Store, new Win32TargetProbe(), dispatcher);`.
  - Pass `insertion` (and the existing `elevation`) to the popup view-model wherever 006
    constructs it, replacing the interim adapter.
  - In `ReleaseResources`, call `dispatcher.RestorePendingNow()` before the windows close.
  - Log `insertion_ready` once.
  - Check that 006's wiring shows an `Inserted` result with a non-empty `Message` as a tray notice
    (006 / AppHost own it; 007 only verifies it in runbook step 16 if reproducible).
  - If 006 has not yet removed `TestString` and the `MessageBox` path from
    `Popup/PopupWindow.xaml.cs`, coordinate: 006 removes them (it owns `Popup/**`). 007 does not
    merge T6 until they are gone.
- **Verify:**
  - CI Windows compile and AOT publish with 0 warnings.
  - Runbook steps 1, 9, 12, 13, 14 and 15.
  - These greps return nothing in `src/`: `TestString`, `IInserter`, `ClipboardPasteInserter`,
    and `MessageBox` under `Popup/`.
- **Test that dies if this breaks:** runbook step 1 (`insertion_ready` and `insert_result
  outcome=Inserted` in the log). The adapter removal is checked by the T7 grep. In Presentation,
  006's popup view-model tests run against `IInsertionService`, so a contract change breaks their
  compile.

### T7 — Wiring audit (L)
- **New methods → callers:**
  - `IInsertionService.InsertAsync` is called by 006's popup view-model only (first call with
    default options; Paste anyway with `AllowClipboardLoss: true`).
  - `InsertionService` is constructed only in `AppHost.Compose`.
  - `Win32TargetProbe` and `ClipboardPasteDispatcher` are constructed only there.
  - `RestorePendingNow` is called by `ReleaseResources`.
  - `ReleasePasteKeys` and `TryPlaceText` are called only by `InsertionService`.
  - `LineEndings.ToCrLf` is called by `InsertionService`.
  - `UiText` insertion strings are used by `InsertionService` and by 006's footer
    (`RestartAsAdministrator`, `PasteAnyway`, `CopyText`).
- **Parallel paths:**
  - `SendInput` and `SetClipboardData` appear only in `ClipboardPasteDispatcher.cs`.
  - `EmptyClipboard` appears only in `TrySwapClipboard` (after the completeness check) and
    `TryPlaceText`.
  - `ClipboardService.SetText` (Copy text) is the only other clipboard writer.
  - No `RecordUsage` call outside `InsertionService` (grep `src/`).
- **Privacy:**
  - Every `AppLog` call in `InsertionService`, `Win32TargetProbe` and `ClipboardPasteDispatcher`
    passes only ids, counts, enums and timings.
  - No `DisplayName`, `Message` or text reaches the log (grep, plus the T3 log test).
- **Prototype removed:** none of `TestString`, `IInserter`, `InsertOutcome`, `InsertResult` or a
  popup `MessageBox` remains.
- **Verify:** during the runbook, `app.log` shows, in order, `insertion_ready` (start-up), then
  per insertion: `insert_start`, `target_check`, `focus_path`, `clipboard_swap`,
  `foreground_gate`, `paste_sent`, `clipboard_restore` and `insert_result`.
- **Update:** master §5 status and §9 log, and this plan's §12 approval log.

### AC traceability

| AC | Requirement part | Tasks | Evidence |
|---|---|---|---|
| AC9 | Unicode + multiline preserved | T2, T3, T5 | `LineEndingsTests`, `Dispatch_ReceivesNormalizedText…`, runbook 1–2 |
| AC9 | Selection replaced normally | T5 (Ctrl+V) | runbook 3 |
| AC9 | Exactly once / duplicate activation | T3 | `SecondCallWhileRunning_ReturnsBusy…`, `…RecordsUsageExactlyOnce…`, `SendPastePartial_Uncertain_MaybePasted…`, `SendPasteThrows_Uncertain…` (no retry offered, no usage), `MaybePasted_DoesNotSayNothingWasPasted`, runbook 9 |
| AC9 | Never into a Smart Snippets window; never another window (residual few-ms focus race accepted 2026-10-28, §5 R1) | T3, T5 | `OwnProcess_…`, `ForegroundLostAfterSwap_…`, `ForegroundGate_SamePidRequired`, `ForegroundGate_IsLastCallBeforeSendPaste`, `TargetClosed_…`, runbook 4, 6, 15 |
| AC9 | Clipboard restored, not in history; never emptied without consent; failed restore reported | T3, T5 | `Inserted_CallsStepsInOrder`, `IncompleteSnapshot_NotPreserved_NoEmptyClipboard…`, `OverCap_NotPreserved…`, `AllowLoss_…` tests, `RestoreFailed_AfterPaste_InsertedWithNotice`, `Restore_…` tests, runbook 7–8, 14 |
| AC9 | Actionable errors | T4, T3 | `InsertionMessagesTests`, runbook 4–6, 11, 14, 15 |
| AC9 | Usage only on success | T3 | `Integration_…` tests (incl. `MaybePasted`, `CopiedForManualPaste`, `ClipboardNotPreserved`), all failure tests assert no usage, runbook 12 |
| AC10 | Elevated target detected with a message (also on the Paste-anyway call) | T5, T3, T4 | `TargetElevated_…`, `AllowLoss_TargetElevated_…`, runbook 5 |
| AC10 | Restart as administrator works from the message | T4 (string), 006 (button), 004 (`IElevation`) | runbook 5 |

| Task | ACs |
|---|---|
| T1 | AC9, AC10 (enabler) |
| T2 | AC9 |
| T3 | AC9, AC10 |
| T4 | AC9, AC10 |
| T5 | AC9, AC10 |
| T6 | AC9, AC10 |
| T7 | AC9, AC10 (audit) |

## 7. Test strategy

- **Unit (Linux and Windows CI):**
  - About 12 `LineEndingsTests` (Core).
  - About 50 `InsertionServiceTests` and 9 `InsertionMessagesTests` (Presentation), against
    `FakeTargetProbe`, `FakePasteDispatcher`, a recording `delay` and `FakeSnippetStore`.
  - **Covered by unit tests only:** the partial-`SendInput` and exception-during-`SendInput` paths
    (`Uncertain` → `MaybePasted`) and the restore-failure notice. They cannot be forced by hand on
    a real desktop.
  - **Not covered by unit tests:** the Win32 calls themselves (T5). They are covered by the CI
    compile and AOT gate, plus the runbook.
- **Integration:** `InsertionServiceIntegrationTests` runs the real `SnippetStore` on a temporary
  SQLite database with the fake boundary. Usage is persisted after `Inserted`, not after any other
  outcome, and `updated_at` is unchanged.
- **Compatibility matrix:** run in **008**, not here: VS Code, Word, Outlook, Chrome/Edge, Windows
  Terminal with PowerShell and cmd, Teams, an elevated variant, and a 100 KB snippet. 008 also
  records whether any common app triggers `ClipboardNotPreserved` spuriously (R6).
- **Manual runbook** (user PC, CI AOT artifact, after 006 and 007 are both on `main`). An unchecked
  item blocks completion.

| # | Step | Expected |
|---|---|---|
| 1 | Create a snippet `Héllo – naïve 👋 世界`, Enter, `\tline 2  (trailing spaces)  `, Enter, `line 3` in the editor (stored with lone CR). In Notepad: hotkey → select it → Enter | Inserted once, with accents, emoji, CJK, the tab, the trailing spaces and **two real line breaks**. No tray notice. `app.log` shows `insert_result outcome=Inserted id=…` and the step events, with no text or window title |
| 2 | Same snippet into a Chrome/Edge `<textarea>` and Windows Terminal (PowerShell) | Line breaks are correct. In Windows Terminal, its multi-line paste warning may appear (expected) |
| 3 | In Notepad, select a word → insert a snippet | The selection is replaced by the snippet (normal paste behaviour) |
| 4 | Open the popup over Notepad, close Notepad with the mouse, then Enter | The footer reads "Couldn't insert. Notepad was closed. Nothing was pasted." with Copy text. Nothing was pasted into any other window. Copy text puts the snippet on the clipboard |
| 5 | Run Notepad as administrator → hotkey → Enter | The footer reads "The target app is running as administrator. Smart Snippets can't insert text into it." with **Restart as administrator** and Copy text. Usage is unchanged. Restart as administrator → UAC → Smart Snippets restarts elevated → hotkey over the elevated Notepad → Enter inserts |
| 6 | With the popup open over Notepad, use the tray menu to open the main window, then insert from the popup | Never inserted into a Smart Snippets window. Either it inserts into Notepad or the footer shows a failure |
| 7 | Copy an image (Paint) and some HTML (a browser selection), and separately a Word selection containing a chart (EMF) → insert into Notepad → wait 1 s → paste in Paint / Word | The original image, HTML and chart are back on the clipboard. No "Paste anyway?" prompt. The log shows `clipboard_swap … unsaved=0` and `clipboard_restore status=Restored failed_formats=0` |
| 8 | Win+V (clipboard history on) after step 7 | The snippet text does **not** appear. No duplicate of the restored item appears |
| 9 | Popup → press Enter twice quickly (and Enter + click) | Inserted exactly once. Usage +1 |
| 10 | Hold Alt (then Win) while pressing Enter in the popup | Inserted. No menu bar is activated in Notepad, and no Start menu opens |
| 11 | Hold the clipboard open for about 2 s from another process (for example a PowerShell one-liner calling `OpenClipboard` via `Add-Type`, prepared at implementation time) → insert during that window | The footer shows the clipboard-busy message with Copy text. Nothing pasted. The clipboard is unchanged |
| 12 | Check usage in the main window after steps 1–15 | The count rose only for successful inserts (1, 2, 3, 5-elevated, 7, 9 once, 10, 14 Paste anyway). Failures (4, 5-unelevated, 11, 14 Cancel, 15) and Esc-cancels did not count. `Updated` is unchanged |
| 13 | Insert, then use tray Exit within 250 ms | The app exits. The original clipboard content is restored (`clipboard_restore` logged before `exit_unregistered`) |
| 14 | **Clipboard holding unsaveable content.** In Paint, create and copy an image of at least 5000 × 4000 px (about 80 MB as a DIB, over the 64 MB cap) → popup over Notepad → Enter. **(a)** Choose **Cancel**. **(b)** Repeat and choose **Paste anyway** | The popup comes back asking "Paste anyway?" (the `ClipboardNotPreserved` message); log `clipboard_swap status=NotPreserved cap_exceeded=true`. (a) Cancel: nothing pasted, Paste in Paint still gives the big image, usage unchanged. (b) Paste anyway: the snippet is inserted in Notepad once, usage +1; the big image is gone from the clipboard (accepted loss); no tray notice |
| 15 | **Paste anyway after closing the target.** Repeat step 14 up to the prompt, close Notepad, then choose **Paste anyway** | No paste into any other window. The popup shows "Couldn't paste into Notepad. The snippet is on your clipboard. Paste it yourself with Ctrl+V." Ctrl+V in another app pastes the snippet. Usage unchanged. Log `manual_paste_fallback reason=Closed` and `clipboard_restore status=Skipped` (or no restore line) |
| 16 | **Not manually testable (recorded, not run):** a forced partial `SendInput` (`MaybePasted`), an exception during `SendInput`, and a restore failure after a paste (`Inserted` + tray notice) cannot be produced reliably on a real desktop. They are covered by the T3 unit tests listed in §6. If a restore failure happens naturally during the runbook, note that the tray notice appeared | — |

## 8. Rollout / phasing

- **Order:**
  1. T0, by the coordinator (`InsertionContracts.cs` and the `UiText` markers).
  2. **Track 007-P** (Presentation and Core: T1 → T2 → T3 → T4). This runs fully on Linux, in
     parallel with 006.
  3. **Track 007-A**: T5. It can be written in parallel, but it compiles only on Windows.
  4. T6, after 006 has merged its popup, its "Paste anyway?" prompt and its interim adapter.
  5. T7.
- **Fix-forward.** The Windows compile happens on merge to `main` (or a manual
  `workflow_dispatch` on the branch). A WinUI or CsWin32 compile error found there is fixed on a
  follow-up branch, not by reverting, as with 005 (`6e75872`). T5 and T6 are kept small and
  mechanical so the fix-forward surface is small. Recommended: run `workflow_dispatch` on the
  branch before merging T5 and T6.
- **Shipping together.** T1–T4 can merge before T5 and T6 (they are inert without wiring). T5 and
  T6 ship together with or after 006. 008 runs the compatibility matrix afterwards.

## 9. Open questions

None blocking. Decided by the user on 2026-10-28 (review): stop before `EmptyClipboard` on an
incomplete snapshot with a "Paste anyway?" retry; the manual-paste fallback; `Inserted` with a
restore notice; the `Uncertain` / `MaybePasted` handling; the HWND+PID last gate with the residual
race accepted; one contract file written by the coordinator. These remain design calls for you to
confirm at approval:
1. **`DisplayName` content** (owned by 006). Recommendation: the app's friendly name, for example
   "Notepad", from the process `FileDescription`, falling back to the window title. 007 uses it as
   given, with a fallback of "the target app".
2. **Hotkey during an in-flight insertion.** The contract has no `IsBusy`. Proposal: 006 does not
   show the popup while its own `InsertAsync` task is pending. It ignores the hotkey and logs
   `hotkey_ignored_inserting`.
3. **Skip the restore when the clipboard changed during the 250 ms** (§4.2), with no notice.
   Confirm that the user's newer clipboard wins over restoring the older one.
4. **Snapshot cap of 64 MB** (R6). Exceeding it now triggers the "Paste anyway?" prompt instead of
   a silent skip.
5. **Extra history markers** (`CanIncludeInClipboardHistory=0`, `CanUploadToCloudClipboard=0`) in
   addition to `ExcludeClipboardContentFromMonitorProcessing` (R4).
6. **`RecordUsage` failure after a paste stays `Inserted`** (§4.2), and **an own-process target
   maps to `PasteFailed`** (the contract has no dedicated outcome).
7. **Cancellation throws `OperationCanceledException` before the paste and is ignored after it**
   (§4.1). The contract enum has no `Cancelled` value.
8. **Assumption: an elevated target on the Paste-anyway call returns `TargetElevated`** (not the
   manual-paste fallback), and **the unsaved formats on the Paste-anyway path do not trigger the
   restore notice** (the user already accepted that loss). The §4.1.1 format groups
   (`CF_ENHMETAFILE` now saved; synthesized, owner-display and private-handle formats skipped by
   design) are part of this approval.

## 10. Implementation notes

Worktree implementation: the Presentation insertion state machine, messages, Windows boundary and
AppHost composition are present. The 006 popup is not present in this worktree: the prototype
`PopupWindow` still uses `ClipboardPasteInserter`, and the newly composed service is not yet called.
At merge, replace 006's interim adapter with `AppHost.Insertion`, keep the composed dispatcher and
shutdown flush, and remove the prototype insertion path. The manual runbook requires that merged
popup and remains unrun. The T7 parallel-path/prototype audit therefore does not yet pass.

The dispatcher boundary adds `DiscardClipboardSnapshot`: the foreground-lost manual-paste branch
must discard its pending exit restore so shutdown does not replace the intentionally retained text.
The runbook, remaining exhaustive state-machine test cases, and exit-while-clipboard-busy behaviour
remain to be verified before calling this plan complete.

## 11. Review response

Review: [docs/review/006-008-implementation-readiness.md](../review/006-008-implementation-readiness.md)
(at `69b5240`). User decisions recorded 2026-10-28.

| Finding | Change in this plan |
|---|---|
| **1. Clipboard restoration can silently lose data** (High) | The snapshot is classified first (§4.1.1: saved / skipped by design / cannot preserve, with justification). An incomplete snapshot (unreadable format or over the 64 MB cap) without `AllowClipboardLoss` closes the clipboard **without `EmptyClipboard`** → `ClipboardNotPreserved`, nothing changed (§4.1 step 7). 006 asks "Paste anyway?"; the retry with `AllowClipboardLoss` re-validates and re-focuses the target, then pastes (`Inserted`, usage counted) or, if it cannot re-target, leaves the snippet on the clipboard → `CopiedForManualPaste` (no usage). A restore failure after a paste gives `Inserted` with a notice message, shown as a tray notice. The old claim that plain text always survives is removed; `CF_ENHMETAFILE` is now saved. New contract values, boundary members (`NotPreserved`, `TryPlaceText`, `Partial`), messages, log fields, T3/T4 tests, R6 and runbook 14–15. |
| **2. A failed dispatch can already have pasted** (High) | `SendPaste` returns `DispatchState { NotDispatched, Dispatched, Uncertain }`. A partial injection or an exception escaping `SendPaste` is `Uncertain`: `ReleasePasteKeys` (V↑, Ctrl↑), no usage, restore after the delay, `MaybePasted` with a "check it before trying again" message that 006 shows without a retry. An exception before `SendPaste` is `NotDispatched` → `PasteFailed`. Tests for partial send, exception after dispatch, exception before dispatch and key-cleanup calls; new R13; runbook 16 says the forced partial send is not manually testable. |
| **3. The foreground gate does not guarantee the target receives the paste** (High) | The last gate checks HWND foreground **and** PID (`IsTargetForeground`), synchronously and immediately before `SendPaste`, with a test asserting the call order. The claim that wrong-window pastes are eliminated is removed; R1 states the residual few-millisecond race as accepted by the user (2026-10-28). |
| **7. Cross-plan contract and tray precedence disagree** (Medium) | One contract file, `src/SmartSnippets.Presentation/Services/InsertionContracts.cs`, written by the coordinator in T0 (not by a track), replaced verbatim with the updated contract (§4.3.1). The internal timings record is renamed `InsertionTimings` to avoid clashing with the contract's `InsertionOptions`. 007 never sets tray state; 008 owns icons and the precedence `HotkeyUnavailable` (red) over `PopupOpen` (blue). |
| 4, 5, 6, 8 | Not in 007's scope (006 / 008). No change here. |

## 12. Approval log

| Date | Event | Notes |
|---|---|---|
| 2026-10-28 | Requirement brief confirmed (G1) | 006–008 brief |
| 2026-10-28 | Review (docs/review/006-008-implementation-readiness.md) | Findings 1–3, 7 addressed; user decisions recorded |
| 2026-10-28 | Plan approved (G2) | User: "approve" |
| 2026-10-28 | Implementation in progress | 006 integration and manual runbook pending; see §10 |
