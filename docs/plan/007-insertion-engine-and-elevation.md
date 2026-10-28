# 007 — Insertion engine and elevation handling

**Date:** 2026-10-28
**Status:** Draft — awaiting approval
**Size:** L (subplan of [001-master-smart-snippets-v1.md](001-master-smart-snippets-v1.md))
**Area:** `src/SmartSnippets.Presentation/Services/`, `src/SmartSnippets.Presentation/Text/UiText.cs`,
`src/SmartSnippets.Core/Domain/`, `src/SmartSnippets.App/Services/` (insertion, elevation probe),
`src/SmartSnippets.App/AppHost.cs`, `tests/SmartSnippets.Presentation.Tests/`, `tests/SmartSnippets.Core.Tests/`
**Depends on:** 002 (complete, insertion spike), 003 (complete, `ISnippetStore.RecordUsage`), and the
**T0 shared contract** (§4.3.1, written by the coordinator before either track starts).
**Runs in parallel with:** 006 (popup). 006 consumes `IInsertionService`; 007 implements it.
**Requirement brief confirmed:** 2026-10-28 (G1, "006–008 brief")

> **API check (2026-10-28, at `4861b0e`):** every API this plan cites was opened at that commit
> (§3). The T0 contract (§4.3.1) does not exist yet; it is written by the coordinator before 006 and
> 007 start. Before starting 007, check that the merged T0 file matches §4.3.1 verbatim, and that
> `ISnippetStore.RecordUsage`, `ISnippetStore.Snippets`, `IElevation` and `IClipboardService` still have
> the shapes cited in §3. If anything differs, update this plan first.

---

## 1. Goal

When you press Enter on a snippet in the popup, its exact text lands once in the app you were
using, as if you had pasted it yourself. Your clipboard comes back as it was, and the snippet never
shows up in clipboard history. If the text can't be inserted (the app was closed, it runs as
administrator, focus couldn't be restored, or another app is holding the clipboard), nothing is
pasted anywhere else. The popup then tells you why and what to do: Copy text, or Restart as
administrator. Usage counts only go up when the paste was actually sent.

## 2. Requirement slice (G1, 2026-10-28)

- **Production service.** Move the 002 spike into a production insertion service.
  - The logic (state machine and orchestration) lives in **Presentation**. It is unit-tested on
    Linux against a fake Win32 boundary (`ITargetProbe`, `IPasteDispatcher`).
  - **App** provides the Win32 implementation by refactoring `ClipboardPasteInserter`,
    `ElevationProbe` and the `ForegroundTarget` checks.
- **Checks before dispatch.** The target window still exists, is not a Smart Snippets window or
  process, and is not elevated while we are not. Never pick another window.
- **Steps.** 006 has already hidden the popup. Then:
  1. Restore focus to the target, handling the foreground lock as the spike did.
  2. Release held modifiers.
  3. Snapshot the copyable clipboard formats.
  4. Set Unicode text, excluded from clipboard history.
  5. Send Ctrl+V with `SendInput`.
  6. Restore the clipboard after 250 ms.
  - Retry when the clipboard is busy.
- **Line endings.** The stored text is unchanged. At paste time, lone CR and lone LF become CRLF.
  This is a pure function in Core or Presentation, with tests for mixed endings, CRLF left
  untouched, and empty input.
- **Single-flight.** A second call while one is running returns `Busy` with no side effects: no
  paste and no usage.
- **Usage.** `store.RecordUsage(id)` runs only after a successful dispatch. A failure or
  cancellation never increments it. If `RecordUsage` fails after a successful paste, the failure is
  logged and the result is still a success (decided in §4.2).
- **Messages (in `UiText`).** One actionable message per outcome:
  - `TargetClosed`: "Couldn't insert. <App> was closed. Nothing was pasted."
  - `TargetElevated`: "The target app is running as administrator. Smart Snippets can't insert
    text into it."
  - `FocusFailed`.
  - `ClipboardBusy`: "…another app is using the clipboard…".
  - `PasteFailed`.
  - `SnippetMissing`: the snippet was deleted in the meantime.
  - The popup (006) shows these in its footer with **Copy text**, plus **Restart as administrator**
    for `TargetElevated` (through the existing `IElevation`).
- **Logs.** The outcome plus ids and timings only.
- **Prototype cleanup.** Remove the prototype `MessageBox` path and `TestString` once 006 is wired.
  006 removes the prototype popup, so the two plans coordinate this.
- **Acceptance (master §2):**
  - **AC9:** Unicode and multiline text preserved; selection replaced normally; exactly once;
    never into a Smart Snippets window; clipboard restored; actionable errors; usage counted only
    on success.
  - **AC10:** elevated targets detected with a message; "Restart as administrator" works. The
    restart itself was built in 004; 007 provides the detection and the outcome that triggers the
    button.
- **Out of scope:**
  - Running the compatibility matrix (008).
  - Popup UI: footer, buttons, re-showing the popup on failure (006).
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
    (`:224-238`), then close.
  - The snapshot copies every `HGLOBAL` format, skipping GDI and owner-display formats
    (`IsSkippedFormat`, `:161-172`). It has no size cap. `GetClipboardData` on a delayed-render
    format makes the clipboard owner render it synchronously.
  - `SetText` writes `CF_UNICODETEXT` exactly as given (`:226`), so there is **no line-ending
    normalization**. It adds the `ExcludeClipboardContentFromMonitorProcessing` marker (`:231-235`).
- **Paste.**
  - `:72-75`: if the text could not be set, the spike reports `ClipboardBusy`, which is the wrong
    outcome for an allocation failure.
  - `:77` `ReleaseHeldModifiers` (`:302-318`) sends a key-up for each held Shift, Ctrl, Alt, LWin
    or RWin. It runs **after** the clipboard swap. A lone synthetic Alt-up or Win-up can open the
    target's menu bar or the Start menu (risk R5).
  - `:79-82` `SendPaste` (`:320-330`) sends Ctrl down, V down, V up, Ctrl up. It fails only when
    `SendInput` returns fewer than 4 events. There is **no foreground re-check** between focus
    restore and the keystroke.
- **Restore.**
  - `:84-91` await the restore delay, then restore in a `finally`.
  - `RestoreClipboard` (`:267-300`) always overwrites the clipboard. It does not check whether the
    user or the target changed it in the meantime (no `GetClipboardSequenceNumber`). The restored
    data carries no exclusion marker, so it re-enters clipboard history as a new entry.
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
  - the guarantee that a swapped clipboard is always restored;
  - the usage update;
  - the mapping from outcome to message;
  - logging.
- **Win32 work goes behind two narrow interfaces, implemented in App:**
  - **`ITargetProbe`** answers "can we paste into this target right now?" (closed / own process /
    elevated / ok) and "is it the foreground window?". `Win32TargetProbe` is built from the checks
    in `ClipboardPasteInserter:36-44`, `ElevationProbe` and the own-process rule of
    `ForegroundTarget.Capture`. It adds a check that the HWND still belongs to the captured PID.
  - **`IPasteDispatcher`** does one Win32 action per call: restore focus, release modifiers, one
    attempt to swap the clipboard, send Ctrl+V, one attempt to restore the clipboard.
    `ClipboardPasteDispatcher` is `ClipboardPasteInserter` cut into these methods. Its
    snapshot/set/restore and `SendInput` code is kept. `Thread.Sleep` and all decisions move to
    the service.
- **`LineEndings.ToCrLf`** is a pure function in Core (`SmartSnippets.Core.Domain`), next to the
  other text rules. It is tested in `Core.Tests`.
- **Messages** come from new `UiText` insertion strings. `InsertionResult.Message` is ready to show
  as-is.
- **AppHost** builds the service once and hands it to 006's popup view-model. This replaces the
  interim adapter 006 uses until 007 lands. `IInserter`, `ClipboardPasteInserter`, `TestString` and
  the `MessageBox` path are gone at the end.

**Ordering inside `InsertAsync`** (full branches in §4.4):

1. **Guard.** If another call is in flight, return `Busy` with no side effects.
2. **Resolve.** Find the snippet in `store.Snippets` by id. If it is missing → `SnippetMissing`.
3. **Normalize.** `text = LineEndings.ToCrLf(snippet.Text)`. The store copy is never changed.
4. **Check.** `probe.Check(target)` returns `Closed` → `TargetClosed`, `OwnProcess` → `PasteFailed`
   (a defensive invariant, see §4.2), or `Elevated` → `TargetElevated`.
5. **Focus.** `dispatcher.RestoreFocusAsync`. If it fails → `FocusFailed`.
6. **Modifiers.** `dispatcher.ReleaseHeldModifiers()`.
7. **Swap.** `dispatcher.TrySwapClipboard(text)` is retried up to 10 times, 20 ms apart, with an
   **awaited** delay. `Busy` every time → `ClipboardBusy`. `Failed` → `PasteFailed`. From here on,
   a `finally` guarantees the restore.
8. **Last gate.** `probe.IsForeground(target)` must still be true; otherwise → `FocusFailed`. This
   is the last chance to avoid pasting into whatever took focus.
9. **Paste.** `dispatcher.SendPaste()`. If it fails → `PasteFailed`.
10. **Usage.** Dispatch succeeded: `store.RecordUsage(id)`, on the caller's (UI) context. A failure
    is logged, and the result stays `Inserted`.
11. **Restore.** Wait `RestoreDelay` (250 ms), then call `TryRestoreClipboard` up to 10 times, 20 ms
    apart. If the clipboard changed since the swap (sequence number), skip the restore and log it.
12. **Result.** Log `insert_result` and return `Inserted`.

**Threading.** `InsertAsync` is called on the UI thread, and every `await` resumes there. The
service never uses `ConfigureAwait(false)`, so `store.RecordUsage` runs on the store's owner
thread (`ISnippetStore.cs:7`), and `OpenClipboard`/`CloseClipboard` stay on one thread. No await
happens while the clipboard is open: each dispatcher call opens and closes it synchronously.

**Cancellation.** `ct` is honoured only **before the paste keystroke** (checked before steps 4, 5, 7
and 9). Cancelling there throws `OperationCanceledException` after restoring the clipboard if it was
swapped. It records no usage and logs `insert_result Cancelled`. After the keystroke, `ct` is
ignored: the text is already on its way, so the usage update and the restore always run. 006 has no
cancel trigger today; this keeps the contract honest without adding an enum value.

### 4.2 Alternatives considered

| Topic | Option | Verdict |
|---|---|---|
| Where the logic lives | **Orchestration in Presentation behind `ITargetProbe` + `IPasteDispatcher`; Win32 in App** | **Chosen.** The rules that matter for AC9 (exactly once, never another window, usage only on success, always restore, Busy without side effects) become Linux unit tests. App code only compiles on `main`'s Windows CI, so logic left there would be verified only by hand. |
| | Keep `IInserter` in App, harden it in place | Rejected. It can only be verified manually, and every rule would need a runbook step. It also keeps App-internal outcome types that 006 cannot consume from Presentation. |
| | One fat `IWin32Insertion.InsertAsync(hwnd, text)` boundary | Rejected. The ordering, retries and restore guarantee would stay untestable inside it. |
| Line endings | **Normalize lone CR / lone LF to CRLF at paste time; stored text unchanged** | **Chosen by the user.** PRD §3.2 says not to modify stored text. CRLF is the Windows clipboard convention for `CF_UNICODETEXT`, and the WinUI `TextBox` already turns CRLF into CR when editing (D1.eol), so storing CRLF would not last. |
| | Normalize on save | Rejected by the user. It changes stored text, and the editor would turn it back into CR on the next edit anyway. |
| | Paste as-is | Rejected. Lone CR breaks multi-line paste in several targets (D1.eol). |
| Normalizer location | **Core `Domain/LineEndings`** | **Chosen.** It is a pure text rule like `TagList` and `LabelSuggester`, already tested cross-platform in `Core.Tests`, and reusable later (for example by an export). Presentation would work too; Core keeps all text rules together. |
| Insertion mechanism | **Clipboard + Ctrl+V** (master decision) | **Chosen.** Proven in 002. It keeps Unicode and the target's normal selection replacement. |
| | Type the text with `SendInput` `KEYEVENTF_UNICODE` as a fallback | **Out of scope.** Slow for long text, breaks with IME and auto-indent and auto-complete editors, and has no clipboard to restore. Could be a later fallback for `ClipboardBusy`; not in the G1 brief. |
| | Detect consumption with delayed rendering (`SetClipboardData(NULL)` + `WM_RENDERFORMAT`) instead of a fixed delay | Deferred. It would tell us when the target read the text, but needs the message window's WndProc to render on demand, and some targets render several formats or never ask. Revisit only if 008's matrix shows that 250 ms is too short. |
| Usage update | **Inside the service, right after a successful `SendPaste`** | **Chosen.** One place enforces "only after dispatch". Recording before the restore delay means an app exit during the delay still counts the paste that already happened. |
| | Popup records usage when `Succeeded` | Rejected. Two consumers (the popup and any future caller) would each need to remember the rule, and the "exactly once" test would have to live in 006. |
| `RecordUsage` fails after a paste | **Log `usage_record_failed` (id + failure kind) and keep `Inserted`** | **Chosen.** The text is already in the target and can't be taken back. Reporting a failure would invite the user to press Enter again, which inserts twice and breaks AC9's "exactly once". Usage statistics are secondary, and PRD §3.8 ties them to dispatch, not to bookkeeping. |
| | Turn it into `PasteFailed` | Rejected for the reason above. |
| Own-process target at dispatch | **Map to `PasteFailed`** (message: generic paste failure) and log `target_rejected reason=own_process` | **Chosen.** The contract has no dedicated value. `ForegroundTarget.Capture` already rejects our own process (`:20`), so this is reachable only through a capture bug. The guarantee "never into a Smart Snippets window" is what matters, not the wording. |
| Single-flight | **One `Interlocked` flag on the service instance** | **Chosen.** It covers double Enter, Enter plus a click, and any second caller, independently of the popup's own guard. |
| Restore when the clipboard changed in the meantime | **Skip the restore** (compare `GetClipboardSequenceNumber` with the value after our swap) | **Chosen.** If the user or the target copied something during the 250 ms, overwriting it would lose their data. Our text stays in that case, still excluded from history. |
| Restored data in clipboard history | **Restore with the exclusion marker added** | **Chosen.** The original content is already in history (or deliberately kept out of it), so restoring it should not create a duplicate entry. |
| Foreground unlock | **Spike path: direct `SetForegroundWindow` while we still hold the foreground, then `AttachThreadInput`** | **Chosen.** Verified in 002 (`focus_path direct`). |
| | Synthetic Alt keypress to unlock `SetForegroundWindow` | Rejected. It activates menus in the target and conflicts with releasing modifiers. |

### 4.3 Data / config / API model

No schema, settings or migration changes.

#### 4.3.1 Shared contract with 006 (T0, coordinator, verbatim)

```csharp
// SmartSnippets.Presentation.Services
public sealed record InsertionTarget(nint WindowHandle, uint ProcessId, nint MonitorHandle, string DisplayName);
public enum InsertionOutcome { Inserted, TargetClosed, TargetElevated, FocusFailed, ClipboardBusy, PasteFailed, Busy, SnippetMissing }
public sealed record InsertionResult(InsertionOutcome Outcome, string Message) { public bool Succeeded => Outcome == InsertionOutcome.Inserted; }
public interface IInsertionService
{
    // Resolves the snippet text from the store, normalizes line endings, dispatches the paste, and records usage only on success.
    // Single-flight: a second call while one is running returns Busy without side effects.
    Task<InsertionResult> InsertAsync(InsertionTarget target, long snippetId, CancellationToken ct = default);
}
```

`DisplayName` is used in messages. It can contain a window title, so it is **never logged**.

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
    bool IsForeground(InsertionTarget target);        // GetForegroundWindow() == target.WindowHandle
}

public enum FocusPath { Direct, AttachThreadInput }
public enum SwapStatus { Swapped, Busy, Failed }
public enum RestoreStatus { Restored, Busy, ClipboardChanged, Failed }

/// <summary>Opaque snapshot owned by the dispatcher; counts are for logs only.</summary>
public interface IClipboardSnapshot { int FormatCount { get; } int SkippedCount { get; } long ByteCount { get; } }

public sealed record ClipboardSwap(SwapStatus Status, IClipboardSnapshot? Snapshot);  // Snapshot set whenever the clipboard was emptied

public interface IPasteDispatcher
{
    Task<FocusPath?> RestoreFocusAsync(InsertionTarget target, CancellationToken ct); // null = focus not restored
    void ReleaseHeldModifiers();
    ClipboardSwap TrySwapClipboard(string text);           // one attempt: open, snapshot, empty, set text + markers, close
    bool SendPaste();                                      // Ctrl down, V down, V up, Ctrl up; true when all 4 injected
    RestoreStatus TryRestoreClipboard(IClipboardSnapshot snapshot); // one attempt; ClipboardChanged = sequence number moved
}

public sealed record InsertionOptions(
    TimeSpan RestoreDelay,          // 250 ms (002 measurement)
    int ClipboardAttempts,          // 10
    TimeSpan ClipboardRetryDelay)   // 20 ms
{
    public static InsertionOptions Default { get; } = new(TimeSpan.FromMilliseconds(250), 10, TimeSpan.FromMilliseconds(20));
}

// Presentation/Services/InsertionService.cs
public sealed class InsertionService(
    ISnippetStore store, ITargetProbe probe, IPasteDispatcher dispatcher,
    InsertionOptions? options = null, Func<TimeSpan, CancellationToken, Task>? delay = null) : IInsertionService;
```

The `delay` seam defaults to `Task.Delay`. Tests pass a recording delay that completes at once,
or one that waits on a `TaskCompletionSource` (for the single-flight test). No new package is
needed, and no `TimeProvider` timer fake has to be written. `InsertionOptions` is internal
configuration, not a user setting (master R3).

#### 4.3.3 Messages (`UiText`, insertion section)

`app` is `target.DisplayName`, or `"the target app"` when it is blank.

| Outcome | Text | Popup actions (006) |
|---|---|---|
| `Inserted` | `Inserted into {app}.` (not shown today; the popup is hidden) | — |
| `TargetClosed` | `Couldn't insert. {app} was closed. Nothing was pasted.` | Copy text |
| `TargetElevated` | `The target app is running as administrator. Smart Snippets can't insert text into it.` | Restart as administrator · Copy text |
| `FocusFailed` | `Couldn't switch back to {app}. Nothing was pasted. Click in {app} and try again.` | Copy text |
| `ClipboardBusy` | `Couldn't insert because another app is using the clipboard. Nothing was pasted. Try again in a moment.` | Copy text |
| `PasteFailed` | `Couldn't paste into {app}. Use Copy text and paste it yourself.` | Copy text |
| `SnippetMissing` | `Couldn't insert. This snippet was deleted. Nothing was pasted.` | — (no text to copy) |
| `Busy` | `Already inserting a snippet.` (006 may ignore it silently) | — |

Plus `RestartAsAdministrator = "Restart as administrator"` for the button. The `TargetClosed` and
`TargetElevated` texts are the design's wording (`docs/design/001-smart-snippets-design.html`,
"Dialogs and errors").

#### 4.3.4 Log events (ids, counts and timings only)

| Event | Detail |
|---|---|
| `insert_start` | `id=<snippetId> pid=<target pid>` |
| `target_check` | `result=Ok\|Closed\|OwnProcess\|Elevated` |
| `elevation_probe` | as today (`own=0x… target=0x… blocked=…`) |
| `focus_path` | `direct\|attach_thread_input\|failed` |
| `clipboard_swap` | `attempts=<n> status=… formats=<n> skipped=<n> bytes=<n>` |
| `paste_sent` | `ok=<bool>` |
| `usage_record_failed` | `id=<id> kind=<SnippetNotFound\|DatabaseBusy\|…>` |
| `clipboard_restore` | `status=Restored\|ClipboardChanged\|Busy\|Failed attempts=<n> failed_formats=<n>` |
| `insert_result` | `outcome=<Outcome> id=<id> ms=<elapsed>` |

Never logged: snippet text, label, tags, `DisplayName`, window titles, or `InsertionResult.Message`
(it contains `DisplayName`).

### 4.4 Sequence diagram: `InsertAsync`

```
006 popup VM (UI thread): Enter/click → popup already hidden → insertion.InsertAsync(target, id, ct)
  InsertionService.InsertAsync
  ├─ Interlocked flag already set? ─────────────────────────────► Busy            (nothing touched, not logged as failure)
  ├─ store.Snippets lookup(id) ── not found ────────────────────► SnippetMissing  (no Win32 call)
  ├─ text = LineEndings.ToCrLf(snippet.Text)                       (store copy untouched)
  ├─ ct cancelled? → throw OCE
  ├─ probe.Check(target)
  │    ├─ Closed (no window / HWND now another PID / process gone) ► TargetClosed   (no focus change, clipboard untouched)
  │    ├─ OwnProcess ──────────────────────────────────────────► PasteFailed     (log target_rejected)
  │    └─ Elevated ────────────────────────────────────────────► TargetElevated  (006: Restart as administrator → IElevation)
  ├─ ct cancelled? → throw OCE
  ├─ dispatcher.RestoreFocusAsync(target)   SetForegroundWindow → wait ≤500 ms → AttachThreadInput → wait ≤500 ms
  │    └─ null ────────────────────────────────────────────────► FocusFailed     (clipboard untouched, never retargets)
  ├─ dispatcher.ReleaseHeldModifiers()
  ├─ ct cancelled? → throw OCE
  ├─ loop ≤10: dispatcher.TrySwapClipboard(text)   (await 20 ms between attempts, UI thread free)
  │    ├─ Busy ×10 ────────────────────────────────────────────► ClipboardBusy   (clipboard untouched)
  │    └─ Failed (alloc/SetClipboardData) ─────────────────────► PasteFailed     (restore if Snapshot != null)
  ├─ try
  │    ├─ ct cancelled? → throw OCE                                (finally restores)
  │    ├─ probe.IsForeground(target) false ────────────────────► FocusFailed     (finally restores; nothing pasted)
  │    ├─ dispatcher.SendPaste() false ────────────────────────► PasteFailed     (finally restores; no usage)
  │    ├─ ── dispatch succeeded: ct ignored from here ──
  │    └─ store.RecordUsage(id)  (UI thread, one transaction)
  │         ├─ Fail(SnippetNotFound) / StoreException ──► log usage_record_failed; outcome stays Inserted
  │         └─ Ok → store.Changed(UsageRecorded) → popup/main lists refresh
  │  finally (only when the clipboard was emptied)
  │    ├─ await RestoreDelay (250 ms; only after a sent paste — failures restore immediately)
  │    └─ loop ≤10: dispatcher.TryRestoreClipboard(snapshot)
  │         ├─ ClipboardChanged → skip, log (user/target data wins)
  │         ├─ Busy ×10 / Failed → log clipboard_restore status; the result is unchanged
  │         └─ Restored
  ├─ unexpected exception from the boundary → log type only → PasteFailed (finally already restored)
  └─ clear flag; log insert_result; return InsertionResult(outcome, UiText message)
```

**Parallel paths checked:**
- `IInserter`/`ClipboardPasteInserter` is the only insertion path today: grep for
  `InsertAsync(`, `SendInput` and `SetClipboardData` in `src/` finds only `ClipboardPasteInserter.cs`
  and `PopupWindow.xaml.cs:140`.
- `ClipboardService.SetText` (Copy text) is a separate, intended path. It writes the clipboard on
  purpose, without restoring it, and stays unchanged.
- After T6, the grep must find `SendInput` and `SetClipboardData` only in
  `ClipboardPasteDispatcher.cs`, and `IInsertionService` implemented only by `InsertionService`.

### 4.5 Surface list

| Surface | Change | Task |
|---|---|---|
| `Presentation/Services/IInsertionService.cs` (contract) | new, **T0 coordinator** | — |
| `Presentation/Services/InsertionBoundary.cs` (`ITargetProbe`, `IPasteDispatcher`, enums, `IClipboardSnapshot`, `InsertionOptions`) | new | T1 |
| `Core/Domain/LineEndings.cs` + `Core.Tests/Domain/LineEndingsTests.cs` | new | T2 |
| `Presentation/Services/InsertionService.cs` + tests + fakes | new | T3 |
| `Presentation/Text/UiText.cs`: insertion section only | change | T4 |
| `App/Services/Win32TargetProbe.cs`, `App/Services/ClipboardPasteDispatcher.cs` | new (refactor) | T5 |
| `App/Services/ElevationProbe.cs`: `IsBlocked(uint pid)` instead of `ForegroundTargetInfo` | change | T5 |
| `App/Services/ClipboardPasteInserter.cs`, `App/Services/IInserter.cs` | **deleted** | T5/T6 |
| `App/AppHost.cs`: compose `InsertionService`, remove the interim adapter, flush the clipboard restore on exit | change | T6 |
| `App/Popup/PopupWindow.xaml.cs` prototype (`TestString`, `MessageBox`) | removed by 006; 007 verifies | T6, T7 |
| Popup footer, Copy text, Restart as administrator button | **006** consumes `InsertionResult` | — |

## 5. Impact and risk

| Question | Answer |
|---|---|
| State management: what survives a crash mid-operation? | A crash between swap and restore leaves the snippet text on the clipboard. It is excluded from clipboard history, and the user's previous clipboard is lost. That is accepted and logged (`clipboard_swap` without a following `clipboard_restore`). A normal Exit during the 250 ms delay is covered: T6 flushes a pending restore in `ReleaseResources`. The database is touched only by `RecordUsage`, in one transaction. |
| Data consistency: orphans, races, double-apply? | The single-flight flag prevents double paste and double usage. `RecordUsage` runs once, after dispatch. A snippet deleted after the lookup gives `SnippetNotFound` from `RecordUsage`, which is logged while the result stays `Inserted`. Store calls run on the UI thread (store contract). |
| User experience | The root problem (D1.eol lone CR) is fixed at paste time. Every failure says what happened, that nothing was pasted, and what to do next, plus Copy text so the text is never stuck. The elevated case offers Restart as administrator. No surprise retargeting: a closed or changed target always fails. |
| Backward compatibility | No data or setting changes. Stored lone-CR text stays as it is and is pasted as CRLF. The App-internal `IInserter` types are removed; their only consumer is the prototype popup, which 006 removes. |
| Error recovery | Every failure before the paste leaves the target untouched and the clipboard restored or never touched, so the user can retry. Busy is transient. `ClipboardBusy` says to try again. A restore failure leaves our text on the clipboard; it is logged and not surfaced, because the paste itself worked. |
| Logging & debugging | The §4.3.4 events cover each step with attempt counts, the focus path and timings, enough to tell foreground-lock, clipboard-contention and UIPI problems apart in the field. Privacy is enforced by a test (T3). |
| Edge cases | Covered: empty snippet text (cannot be stored, per the validator; the service still handles `""` without error); huge text (R8); text with only `\r`; `\r\n` already; mixed endings; emoji and surrogate pairs; target closed during focus restore; HWND reused by another process; target on another desktop or a locked screen (focus fails → `FocusFailed`); double Enter; Enter during the restore delay; Exit during the restore delay. |

**Risks:**

- **R1 Foreground lock.** `SetForegroundWindow` can be refused once we no longer own the
  foreground, for example when 006 hides the popup and Windows activates another window first.
  - **Mitigations:**
    - 006 hides the popup and calls `InsertAsync` in the same UI turn, so we still hold the
      last-input right (verified `direct` in 002).
    - The `AttachThreadInput` fallback stays.
    - The final `IsForeground` gate before `SendPaste` means a lost race gives `FocusFailed`,
      never a paste into the wrong window.
- **R2 UIPI and elevation.** `SendInput` into a higher-integrity window fails silently. The
  documentation says neither the return value nor `GetLastError` reports it.
  - **Mitigations:**
    - Detect elevation up front. Access denied counts as elevated (conservative). A target
      running as another user is reported as elevated: an acceptable false positive.
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
    braces (open question 5).
  - Third-party managers and the Office Clipboard may still capture it; this is recorded in 008.
  - The restore also carries the marker, so it creates no duplicate history entry.
- **R5 Releasing modifiers.** A synthetic Alt-up or Win-up with no other key in between opens the
  target's menu bar or the Start menu.
  - **Mitigation:** when Alt or Win is held, T5 first injects a no-op key (unassigned VK `0xE8`)
    down and up, then the key-ups. Order as in the brief: focus, then modifiers, then clipboard.
  - Runbook step 10 holds Alt or Win while pressing Enter.
- **R6 Delayed-render formats.** Snapshotting calls `GetClipboardData` on every format. For
  delayed-render owners (Excel, Word, browsers), that makes the owner render each format
  synchronously: slow for big copies, and a hung owner blocks us.
  - **Mitigations:**
    - Skip GDI-handle formats (as today).
    - Cap the snapshot at **64 MB total**. Formats past the cap are skipped and counted (open
      question 4).
    - Log `bytes` and the swap time.
    - Formats that can't be read can't be restored. This is documented as a limitation; the
      plain-text formats always survive.
- **R7 Apps that read the clipboard late.** The restore delay can beat the target's read, so the
  user's old clipboard gets pasted instead of the snippet. Examples: RDP and Citrix sessions, VMs,
  and Electron apps under load.
  - The 250 ms delay comes from the 002 measurement (Notepad, Chrome, Word, Windows Terminal).
  - It is internal configuration (`InsertionOptions`). 008 runs the matrix and records the apps
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

**Rollback:**
- Revert the T5 and T6 commits, and AppHost goes back to 006's interim adapter.
- Presentation and Core additions (T1–T4) are inert without wiring.
- No data migration to undo.

## 6. Tasks

**File ownership:**
- **007 owns:** the files in §4.5 rows T1–T6, and only the insertion section of `UiText.cs`.
- **006 owns:** `ForegroundTarget.cs`, `Popup/**`, the popup view-models, and the rest of
  `UiText.cs`.
- **Shared:** `AppHost.cs` is edited by 007 only in T6, after 006 has merged.

**T0 (coordinator, before either track):**
- Writes `Presentation/Services/IInsertionService.cs` with §4.3.1 verbatim.
- Adds empty `// Insertion (007)` and `// Popup (006)` section markers to `UiText.cs`, so the two
  tracks append without conflicts.

### T1 — Win32 boundary interfaces (AC9, AC10 enabler)
- **Files:** `src/SmartSnippets.Presentation/Services/InsertionBoundary.cs`.
- **Change:** the types in §4.3.2 (`ITargetProbe`, `TargetCheck`, `IPasteDispatcher`, `FocusPath`,
  `SwapStatus`, `RestoreStatus`, `IClipboardSnapshot`, `ClipboardSwap`, `InsertionOptions`), with
  XML docs stating the Win32 semantics the App must honour.
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
    fake that records every call in order (with the text it received) and returns scripted
    results per attempt.
- **Change:**
  - The §4.1 ordering, single-flight, cancellation rule, the restore guarantee and the usage
    rule.
  - Messages through T4's `UiText`. Until T4 lands, `UiText` placeholders keep T3 compiling.
  - The §4.3.4 log events.
- **Verify:** `dotnet test tests/SmartSnippets.Presentation.Tests --filter Insertion` on Linux.
- **Tests that die if this breaks.** Each hop is named; each test also asserts no usage and no
  paste where relevant.

  *Store lookup and text:*
  - `Inserted_CallsStepsInOrder`: Check → focus → modifiers → swap → IsForeground → SendPaste →
    delay(250) → restore.
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

  *Focus:*
  - `FocusFailed_ClipboardUntouched_NoUsage`.
  - `ForegroundLostAfterSwap_FocusFailed_ClipboardRestoredImmediately_NoPaste`.

  *Clipboard swap:*
  - `ClipboardBusy_TenAttempts_TwentyMsApart_Then_ClipboardBusy`.
  - `ClipboardBusy_SucceedsOnThirdAttempt_Inserted`.
  - `SwapFailed_AfterEmpty_RestoresSnapshot_PasteFailed`.

  *Paste and restore:*
  - `SendPasteFails_RestoresWithoutDelay_PasteFailed_NoUsage`.
  - `BoundaryThrows_RestoresClipboard_PasteFailed_FlagCleared`.
  - `Restore_ClipboardChanged_Skipped_ResultStillInserted`.
  - `Restore_BusyRetries_ThenGivesUp_ResultStillInserted`.

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
  - `Integration_Failure_DoesNotPersistUsage` (reopen the store and compare).

### T4 — Insertion messages (AC9 actionable errors, AC10 message)
- **Files:** `src/SmartSnippets.Presentation/Text/UiText.cs` (insertion section only),
  `tests/SmartSnippets.Presentation.Tests/InsertionMessagesTests.cs`.
- **Change:** the §4.3.3 strings, a `RestartAsAdministrator` constant, and a helper
  `InsertionMessage(InsertionOutcome, string displayName)` used by T3.
- **Verify:** `dotnet test … --filter InsertionMessages`.
- **Tests that die if this breaks:**
  - `TargetClosed_DesignWording` (`"Couldn't insert. Notepad was closed. Nothing was pasted."`)
  - `TargetElevated_DesignWording`
  - `ClipboardBusy_MentionsAnotherApp`
  - `EveryFailureOutcome_HasNonEmptyMessage` (iterates the enum, so a new value without a message
    fails)
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
    `ElevationProbe.IsBlocked(pid)` (`Elevated`). `IsForeground` compares with
    `GetForegroundWindow`.
  - **`ElevationProbe.IsBlocked`:** takes `uint processId`. Its logic and log line are unchanged.
    `IsOwnProcessElevated` stays for `ElevationService.cs:19`.
  - **`ClipboardPasteDispatcher`:** moves `RestoreFocusAsync`, `TakeSnapshot`, `SetText`,
    `SetData`, `RestoreClipboard`, `ReleaseHeldModifiers` and `SendPaste` out of
    `ClipboardPasteInserter` into the one-attempt methods of §4.3.2. Additions:
    - `GetClipboardSequenceNumber` captured after the swap and compared before the restore.
    - The exclusion marker added on restore.
    - The `CanIncludeInClipboardHistory` and `CanUploadToCloudClipboard` = 0 markers.
    - The 64 MB snapshot cap.
    - The R5 no-op key before the Alt/Win key-up.
    - `RestorePendingNow()`, for exit (T6).
  - **Removed:** `Thread.Sleep`, and every outcome or message decision.
  - `ForegroundTarget.cs` is **not** edited (006 owns it).
- **Verify:**
  - A CI Windows job (merge to `main`, or `workflow_dispatch` on the branch) compiles and
    AOT-publishes with **0** IL2xxx/IL3xxx warnings.
  - Runbook §7 steps 1–12 on the CI artifact.
- **Test that dies if this breaks:** not unit-testable (Win32). It is guarded by the CI compile,
  the runbook, and the log lines `target_check`, `focus_path`, `clipboard_swap`, `paste_sent` and
  `clipboard_restore` appearing in `app.log` during runbook step 1 (checked in T7).

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
  - If 006 has not yet removed `TestString` and the `MessageBox` path from
    `Popup/PopupWindow.xaml.cs`, coordinate: 006 removes them (it owns `Popup/**`). 007 does not
    merge T6 until they are gone.
- **Verify:**
  - CI Windows compile and AOT publish with 0 warnings.
  - Runbook steps 1, 9, 12 and 13.
  - These greps return nothing in `src/`: `TestString`, `IInserter`, `ClipboardPasteInserter`,
    and `MessageBox` under `Popup/`.
- **Test that dies if this breaks:** runbook step 1 (`insertion_ready` and `insert_result
  outcome=Inserted` in the log). The adapter removal is checked by the T7 grep. In Presentation,
  006's popup view-model tests run against `IInsertionService`, so a contract change breaks their
  compile.

### T7 — Wiring audit (L)
- **New methods → callers:**
  - `IInsertionService.InsertAsync` is called by 006's popup view-model only.
  - `InsertionService` is constructed only in `AppHost.Compose`.
  - `Win32TargetProbe` and `ClipboardPasteDispatcher` are constructed only there.
  - `RestorePendingNow` is called by `ReleaseResources`.
  - `LineEndings.ToCrLf` is called by `InsertionService`.
  - `UiText` insertion strings are used by `InsertionService` and by 006's footer (`RestartAsAdministrator`, `CopyText`).
- **Parallel paths:**
  - `SendInput` and `SetClipboardData` appear only in `ClipboardPasteDispatcher.cs`.
  - `ClipboardService.SetText` (Copy text) is the only other clipboard writer.
  - No `RecordUsage` call outside `InsertionService` (grep `src/`).
- **Privacy:**
  - Every `AppLog` call in `InsertionService`, `Win32TargetProbe` and `ClipboardPasteDispatcher`
    passes only ids, counts, enums and timings.
  - No `DisplayName`, `Message` or text reaches the log (grep, plus the T3 log test).
- **Prototype removed:** none of `TestString`, `IInserter`, `InsertOutcome`, `InsertResult` or a
  popup `MessageBox` remains.
- **Verify:** during the runbook, `app.log` shows, in order, `insertion_ready` (start-up), then
  per insertion: `insert_start`, `target_check`, `focus_path`, `clipboard_swap`, `paste_sent`,
  `clipboard_restore` and `insert_result`.
- **Update:** master §5 status and §9 log, and this plan's §11 approval log.

### AC traceability

| AC | Requirement part | Tasks | Evidence |
|---|---|---|---|
| AC9 | Unicode + multiline preserved | T2, T3, T5 | `LineEndingsTests`, `Dispatch_ReceivesNormalizedText…`, runbook 1–2 |
| AC9 | Selection replaced normally | T5 (Ctrl+V) | runbook 3 |
| AC9 | Exactly once / duplicate activation | T3 | `SecondCallWhileRunning_ReturnsBusy…`, `…RecordsUsageExactlyOnce…`, runbook 9 |
| AC9 | Never into a Smart Snippets window, never another window | T3, T5 | `OwnProcess_…`, `ForegroundLostAfterSwap_…`, `TargetClosed_…`, runbook 4, 6 |
| AC9 | Clipboard restored, not in history | T3, T5 | `Inserted_CallsStepsInOrder`, `Restore_…` tests, runbook 7–8 |
| AC9 | Actionable errors | T4, T3 | `InsertionMessagesTests`, runbook 4–6, 11 |
| AC9 | Usage only on success | T3 | `Integration_…` tests, all failure tests assert no usage, runbook 12 |
| AC10 | Elevated target detected with a message | T5, T3, T4 | `TargetElevated_…`, runbook 5 |
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
  - About 30 `InsertionServiceTests` and 5 `InsertionMessagesTests` (Presentation), against
    `FakeTargetProbe`, `FakePasteDispatcher`, a recording `delay` and `FakeSnippetStore`.
  - **Not covered by unit tests:** the Win32 calls themselves (T5). They are covered by the CI
    compile and AOT gate, plus the runbook.
- **Integration:** `InsertionServiceIntegrationTests` runs the real `SnippetStore` on a temporary
  SQLite database with the fake boundary. Usage is persisted after a success, not after a failure,
  and `updated_at` is unchanged.
- **Compatibility matrix:** run in **008**, not here: VS Code, Word, Outlook, Chrome/Edge, Windows
  Terminal with PowerShell and cmd, Teams, an elevated variant, and a 100 KB snippet.
- **Manual runbook** (user PC, CI AOT artifact, after 006 and 007 are both on `main`). An unchecked
  item blocks completion.

| # | Step | Expected |
|---|---|---|
| 1 | Create a snippet `Héllo – naïve 👋 世界`, Enter, `\tline 2  (trailing spaces)  `, Enter, `line 3` in the editor (stored with lone CR). In Notepad: hotkey → select it → Enter | Inserted once, with accents, emoji, CJK, the tab, the trailing spaces and **two real line breaks**. `app.log` shows `insert_result outcome=Inserted id=…` and the step events, with no text or window title |
| 2 | Same snippet into a Chrome/Edge `<textarea>` and Windows Terminal (PowerShell) | Line breaks are correct. In Windows Terminal, its multi-line paste warning may appear (expected) |
| 3 | In Notepad, select a word → insert a snippet | The selection is replaced by the snippet (normal paste behaviour) |
| 4 | Open the popup over Notepad, close Notepad with the mouse, then Enter | The footer reads "Couldn't insert. Notepad was closed. Nothing was pasted." with Copy text. Nothing was pasted into any other window. Copy text puts the snippet on the clipboard |
| 5 | Run Notepad as administrator → hotkey → Enter | The footer reads "The target app is running as administrator. Smart Snippets can't insert text into it." with **Restart as administrator** and Copy text. Usage is unchanged. Restart as administrator → UAC → Smart Snippets restarts elevated → hotkey over the elevated Notepad → Enter inserts |
| 6 | With the popup open over Notepad, use the tray menu to open the main window, then insert from the popup | Never inserted into a Smart Snippets window. Either it inserts into Notepad or the footer shows a failure |
| 7 | Copy an image (Paint) and some HTML (a browser selection) → insert into Notepad → wait 1 s → paste in Paint / Word | The original image and HTML are back on the clipboard. The log shows `clipboard_restore status=Restored failed_formats=0` |
| 8 | Win+V (clipboard history on) after step 7 | The snippet text does **not** appear. No duplicate of the restored item appears |
| 9 | Popup → press Enter twice quickly (and Enter + click) | Inserted exactly once. Usage +1 |
| 10 | Hold Alt (then Win) while pressing Enter in the popup | Inserted. No menu bar is activated in Notepad, and no Start menu opens |
| 11 | Hold the clipboard open for about 2 s from another process (for example a PowerShell one-liner calling `OpenClipboard` via `Add-Type`, prepared at implementation time) → insert during that window | The footer shows the clipboard-busy message with Copy text. Nothing pasted. The clipboard is unchanged |
| 12 | Check usage in the main window after steps 1–11 | The count rose only for successful inserts (1, 2, 3, 5-elevated, 7, 9 once, 10). Failures (4, 5-unelevated, 11) and Esc-cancels did not count. `Updated` is unchanged |
| 13 | Insert, then use tray Exit within 250 ms | The app exits. The original clipboard content is restored (`clipboard_restore` logged before `exit_unregistered`) |

## 8. Rollout / phasing

- **Order:**
  1. T0, by the coordinator.
  2. **Track 007-P** (Presentation and Core: T1 → T2 → T3 → T4). This runs fully on Linux, in
     parallel with 006.
  3. **Track 007-A**: T5. It can be written in parallel, but it compiles only on Windows.
  4. T6, after 006 has merged its popup and interim adapter.
  5. T7.
- **Fix-forward.** The Windows compile happens on merge to `main` (or a manual
  `workflow_dispatch` on the branch). A WinUI or CsWin32 compile error found there is fixed on a
  follow-up branch, not by reverting, as with 005 (`6e75872`). T5 and T6 are kept small and
  mechanical so the fix-forward surface is small. Recommended: run `workflow_dispatch` on the
  branch before merging T5 and T6.
- **Shipping together.** T1–T4 can merge before T5 and T6 (they are inert without wiring). T5 and
  T6 ship together with or after 006. 008 runs the compatibility matrix afterwards.

## 9. Open questions

None blocking. These are design calls for you to confirm at approval:
1. **`DisplayName` content** (owned by 006). Recommendation: the app's friendly name, for example
   "Notepad", from the process `FileDescription`, falling back to the window title. The design
   shows "Notepad was closed" in the footer and "Notepad, Untitled" as the kept target. 007 uses
   it as given, with a fallback of "the target app".
2. **Hotkey during an in-flight insertion.** The contract has no `IsBusy`. Proposal: 006 does not
   show the popup while its own `InsertAsync` task is pending. It ignores the hotkey and logs
   `hotkey_ignored_inserting`.
3. **Skip the restore when the clipboard changed during the 250 ms** (§4.2). Confirm that the
   user's newer clipboard wins over restoring the older one.
4. **Snapshot cap of 64 MB** (R6). Formats past the cap are not restored.
5. **Extra history markers** (`CanIncludeInClipboardHistory=0`, `CanUploadToCloudClipboard=0`) in
   addition to `ExcludeClipboardContentFromMonitorProcessing` (R4).
6. **`RecordUsage` failure after a paste stays `Inserted`** (§4.2), and **an own-process target
   maps to `PasteFailed`** (the contract has no dedicated outcome).
7. **Cancellation throws `OperationCanceledException` before the paste and is ignored after it**
   (§4.1). The contract enum has no `Cancelled` value.

## 10. Implementation notes

*(Filled during implementation.)*

## 11. Approval log

| Date | Event | Notes |
|---|---|---|
| 2026-10-28 | Requirement brief confirmed (G1) | 006–008 brief |
| | Plan approved (G2) | |
