# 028 — Windows insertion composition: review-gated checkpoint

Status: compiling and security-reviewed source/test checkpoint, **NOT T4/T5 completion**. No native clipboard mutation or paste was performed. `Program.NativeEntryReviewed` remains false.

## Composition and actual build evidence

The new Windows-only prototype composes a watchdog, an independently retained RAM clipboard guardian, a separate worker/target job, real SQLite SnippetStore, actual Presentation InsertionService, exact owned native target identity and a linked unchanged shipping ClipboardPasteDispatcher. A prototype partial class adds a single-lock owner/state/sequence transaction seam; it does not prove equivalence to the shipping transaction methods. All production source/project/package/CI files are untouched.

The final strict win-x64 Native AOT publish succeeds with empty NoWarn and zero warnings. Entry SHA256 is `785ce9ca8a99541731df45833ed18bcb5cb5c4776e8d6ec0981f730866db22de`; native PE checks pass, `e_sqlite3.dll` is present, and no managed entry DLL exists. The artifact was not launched. Because the entry is hard-locked, artifact checks do not prove any runtime clipboard/input path.

The worker's initial implementation run timed out after 30 minutes. Its source was preserved; one bounded retry produced the first compiling composition. Integrated tests/builds and two parallel security reviews then exposed recovery, IPC and focus defects. Those were fixed in one batch; confirming review cleared all blocking findings. The optional repeated-recovery timeout was also removed directly. This is a checkpoint, not a substitute for native fault tests.

## Safety corrections and declared limits

- Guardian captures a complete original snapshot into its independent RAM before worker permission; only worker/target descendants enter the kill-on-close job.
- A single OS clipboard lock encloses eligibility checks and mutation. Owner HWND alone and inferred epoch arithmetic never authorize restore; expected actual sequence and recognizable per-run state are checked.
- Partial original restoration retains a run marker and retryable original-state policy. Supported original representation bytes are preserved **plus explicitly declared protective/run markers**; identical original format-set equality is not promised. Incompatible original privacy markers, skipped/unreadable/oversized formats refuse before clear.
- Worker termination/reaping precedes independent guardian recovery, including failure/timeout paths. Unsettled recovery retains the backup and current-user-only recovery control rather than killing the sole RAM backup.
- IPC reads/writes cancel underlying operations and are bounded; pre-arm timeout, broken-pipe reconnection and late STA continuation paths are handled.
- The insertion fixture is already focused. The adapter yields before mutation for a genuine duplicate Busy check, then accepts only exact currently owned foreground; it does not call shipping AttachThreadInput or generic modifier-release logic.
- Full input guards remain. Native partial SendInput is explicitly keyboard-recovery-pending; no blind key-ups are sent. Hard-abort scenarios occur before input, never while intentionally leaving real key-downs outstanding.
- Explicit clipboard locale/codepages and a single locked current snapshot replace speculative user-default synthesis expectations. Actual Windows synthesized representation/size behavior still requires native verification.

## Test results

| Suite | Windows | Apple Silicon macOS |
|---|---|---|
| Core | 432 passed | 432 passed |
| Presentation | 599 passed | 599 passed |
| Prototype identity/protocol | 70 passed | 70 passed |
| Windows insertion policies/actual service flow | 29 passed, native boundaries substituted | Not applicable |
| Desktop Python | 181 run, one skip | 181 run, 38 skips |
| Identity runner Python | 14 passed | 14 passed |
| Private-station pure tests | 27 passed | Not run in this checkpoint |

These checks are not positive native insertion, restored clipboard, actual abort recovery, native contention or changed-owner acceptance. The planned positive and abort-after-swap scenes remain NOT RUN, and the source entry stays disabled.

## Private-station prerequisite: BLOCKED

A separate reviewed Python/ctypes helper attempts only a freshly created, owner-ACL, noninteractive station with `CreateWindowStationW(NULL, CWF_CREATE_ONLY)`. It never falls back to an existing station or opens the normal WinSta0 clipboard. Review's sent-message pump and cleanup corrections were applied; 27 pure tests pass.

One authorized Windows trial returned exit **3** with exactly `blocked_createwindowstation_null_create_only` and `general_sequence_unchanged`. Thus the API returned NULL before a private clipboard was seeded; normal clipboard contents were never opened/read/written. No claim is made about whether the cause was access denial or a pre-existing system-generated station name. The source SHA256 was `a7c1f51f8789b654ec0227a34d6d62d030bfacc6d6051428c851b6b5fbb4e636`.

This is a recorded unavailable isolation prerequisite, **not a successful ownership proof**. No elevation, station/display/security changes or interactive fallback were attempted. See [isolation README](../../prototypes/uno-native-target/isolation/README.md).

## Follow-up isolation and allocation prerequisites

A separate read-only creation diagnostic captured `ERROR_ALREADY_EXISTS` (183) immediately after a later NULL/create-only call, before cleanup could overwrite LastError. The normal clipboard was not opened and its sequence was unchanged. This classifies that later attempt only; the first historical trial did not capture LastError.

A separately security-reviewed alternative requests a fresh `ss_probe018_<uuid>` name with the same create-only flag, owner-only protected DACL, unelevated token, non-visible/non-WinSta0 and exact-returned-name checks, job containment and interpreter/script/parent pinning. It is **not a fallback** after a failed call. The committed `NAMED_CREATE_ONLY_REVIEWED` default remains false; only an isolated trial copy enabled it. Thirty pure tests pass, including terminal create failure without opening an existing station, name mismatch before binding and unique requested names.

The single authorized named trial also returned **BLOCKED 3**, with `blocked_createwindowstation_named_create_only` and `general_sequence_unchanged`. No private clipboard seed/read/write was reached. Its source SHA256 was `fda861645671a2eaa7f75c42d7ae6596513f5bc8912b56160b23643a1ef5d0da`. The named trial did not capture a LastError value, so no particular cause is claimed. No elevation, policy bypass or interactive-clipboard fallback was attempted.

An independent allocation-only Win32 API check requested zero-initialized movable HGLOBAL blocks of 4, 16, 23 and 46 bytes: each actual `GlobalSize` equaled the requested size, and all allocations were freed. No clipboard API was accessed. This narrows the allocation-size prerequisite but does **not** prove clipboard synthesis sizes, cross-process ownership or restoration. These Python/ctypes observations are native API prerequisites, not Native AOT execution evidence.

## Next acceptance steps

1. Obtain an isolated Windows session or an explicitly operator-prepared harmless clipboard. Keep committed insertion entry false; any temporary reviewed native-test enablement requires a fresh strict build.
2. Execute clipboard-only/no-input controls first: cross-process owner assignment, native representation sizing, ordinary restoration, stopped-worker recovery and changed-owner refusal. If recovery is uncertain, stop with the independently retained backup; do not proceed to input.
3. Only after those controls pass, verify real target text/usage/duplicate with guarded input, then wire and verify actual Uno popup/menu/main-row entry paths and remaining negative cases. Existing Mac input and unsupported-clipboard prerequisites and all final Plan 018 gates remain open.
