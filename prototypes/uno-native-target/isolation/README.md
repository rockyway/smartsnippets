# Review-gated private station clipboard prerequisite

**Reviewed private-station trial; BLOCKED on the current host.** `REVIEWED = True` permits only the explicitly authorized, fail-closed noninteractive station experiment. The initial worker performed pure mocked tests only; the parent applied the review's message-pump/cleanup corrections, ran 27 pure tests and one authorized native trial. That trial returned code 3 (`blocked_createwindowstation_null_create_only`) with `general_sequence_unchanged`. No station clipboard was seeded; no normal clipboard was opened. A later diagnostic captured ERROR_ALREADY_EXISTS (183) for a fresh NULL/create-only attempt. A separate security-reviewed unique-name/create-only experiment also returned BLOCKED 3 with unchanged General sequence. Thirty pure tests now pass. `NAMED_CREATE_ONLY_REVIEWED=False` remains the committed default; only an isolated reviewed trial copy enabled it. There is never an automatic fallback, elevation or existing/interactive-station reuse.

Scope: standalone Python/ctypes Windows x64 API experiment, not product code, a production guardian, Uno integration, or Native AOT artifact evidence. No dependencies beyond Python 3.11+ standard library. Only the first two proofs are implemented:

1. A worker process opens the private clipboard using the guardian's message-only HWND, calls `EmptyClipboard`, observes that exact HWND as owner, and sets/reads fixed synthetic bytes. Guardian independently checks owner PID and readback.
2. That worker opens using the same guardian HWND and calls `SetClipboardData` **without** `EmptyClipboard`. Guardian checks changed sequence, unchanged owner HWND, and different fixed synthetic bytes. This is the same-owner external-write counterexample, not a security guarantee based on owner identity.

Worker-abort-after-clear and foreign-child-owner-change refusal are **NOT RUN / not implemented** and have explicit `not_run_*` receipts. Pure owner-mismatch tests are not native fault-matrix evidence.

## Isolation contract

- No elevation; validated current TokenUser SID and TokenElevation=0 are required. Station and desktop are created with protected owner-only DACLs (`O:<SID>D:P(A;;GA;;;<SID>)`) at creation. No diagnostic files are created, so there is no pre-ACL log-write interval. Same-SID hostile processes are not excluded by an owner-only ACL.
- The launcher never changes its station/desktop, opens the General clipboard, or reads General clipboard contents. It requires its station name to be `WinSta0`, reads only `GetClipboardSequenceNumber` before and after, and fails on any change (including unrelated user activity). Zero initial sequence fails closed. A matching sequence is an observation, not proof against wraparound or hostile interference.
- In the owned guardian process, before **any HWND**, the default `CreateWindowStationW(NULL, CWF_CREATE_ONLY, ...)` must create a fresh station. Its returned name must not be `WinSta0`, and `UOI_FLAGS` must lack `WSF_VISIBLE`. Failure is terminal: no existing-station open, automatic retry or interactive fallback exists.
- The separately reviewed, source-gated named experiment instead makes a single create-only request for `ss_probe018_<uuid>`, with the same DACL and nonvisibility checks plus exact returned-name verification before binding. It does not run after a failed NULL attempt. Its native trial was also blocked; no broader API proof resulted.
- [CreateWindowStationW](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-createwindowstationw) restricts caller-specified names to Administrators, documents NULL's system-formed logon-session name, and failure when the station exists with `CWF_CREATE_ONLY`. Both experiments preserve the unelevated-token requirement and stop on refusal without requesting elevation or bypassing policy. NULL and named failures emit their distinct allowlisted blocker receipts, then exit 3 after cleanup and the outer sequence check.
- Guardian switches only its own process station, creates a private desktop, and switches only its own thread desktop. Only then may it create a built-in `STATIC` message-only HWND (`HWND_MESSAGE`), never a visible window. No `SwitchDesktop`, `SendInput`, activation, display changes, or user-input synthesis is present.
- Original station and thread-desktop handles are retained (borrowed, never closed). Cleanup destroys HWNDs, restores original process station before original thread desktop (the desktop must belong to the current station), then closes owned desktop and station in reverse creation order. Restoration failure stops explicit handle closure and leaves teardown to the owned process/job.
- Fresh private clipboard must have zero formats and no owner **before** the initial clear/seed. Every data read is fixed-length readback of one of three fixed ASCII fixtures in that private station; no snapshotting, titles, or payload output. Clipboard locks close in `finally`; movable global allocations transfer ownership only on successful `SetClipboardData` and otherwise are freed.
- Guardian and worker use pinned absolute interpreter/script paths, fixed `-I -B` / role flags, SHA-256 script and interpreter digests, current SID, direct-parent PID and queried parent executable identity, a run GUID, private stdin/stdout pipes, and bounded readiness messages. The spawned command is pinned by the spawning parent; child-side interpreter/digest checks are not an adversarial same-SID attestation of its parent's command line. Child opens only the station/desktop identifiers supplied by its authenticated spawning guardian and checks nonvisibility before any HWND. HWNDs are checked against the expected process PID.
- Outer launcher places the guardian in a kill-on-close Job Object **before** releasing the bootstrap pipe. Worker inherits that job. Failed assignment kills/reaps the still-bootstrap-only child. The guardian normally kills/reaps its direct worker before surface teardown. The launcher imposes a 48-second completion timeout, terminates only its owned job, polls active process count to zero (12-second bound), and reaps the guardian. No process-name/global kills. On forced termination object references are released by process exit; surviving worker references cannot be abandoned outside the job. Job assignment/nesting restrictions fail closed.
- Only allowlisted receipt names and numeric counts reach launcher stdout; no exception strings, clipboard bytes, titles, paths, or native diagnostic text. Exit 0 means both limited proofs passed plus unchanged General sequence; 1 means failed/inconclusive; 2 means closed authorization/review/platform gate; 3 means the selected create-only API prerequisite was blocked. A failed or blocked run is not a successful isolation proof.

## Commands

From repository root, safe validation (does not load native DLLs, spawn children, or access a clipboard):

```bash
python -B -m unittest discover -s prototypes/uno-native-target/isolation -p 'test_*.py' -v
python -B -c "from pathlib import Path; files=list(Path('prototypes/uno-native-target/isolation').glob('*.py')); [compile(p.read_text(), str(p), 'exec') for p in files]; print('syntax checked:',len(files))"
```

**Parent only, after security review and explicit private-probe authorization, on an unelevated Windows x64 host:**

```powershell
python -I -B prototypes/uno-native-target/isolation/probe.py --authorized-private-station
```

Do not invoke internal `--role` modes manually; their bootstrap protocol is supplied only by the owning parent. Do not capture arbitrary console output in shared files. The initial implementation did not run native code. The parent subsequently ran the reviewed bounded trial described above; the source SHA256 was `a7c1f51f8789b654ec0227a34d6d62d030bfacc6d6051428c851b6b5fbb4e636`. The current-host trial confirmed the fail-closed creation blocker and unchanged General sequence only. Station creation, API ownership semantics and successful private clipboard cleanup remain NOT RUN. This experiment deliberately provides no insertion capability and no broader fault-matrix acceptance.
