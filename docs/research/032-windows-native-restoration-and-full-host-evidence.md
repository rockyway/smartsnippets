# 032 — Windows full-host and post-publication restoration evidence

Plan 019 execution evidence, not plan completion or production cutover. Plan 018
remains in progress; macOS testing is paused. No hotkey, paste, UI Copy or keyboard
input is established by the two observations below.

## Native full-host observation (default-denied composition)

- Source: integrated policy/lifecycle/UI fixes through `842f3b4` (the artifact was
  built from the corresponding source before commit). Independent confirming
  review: `09436f83-c5db-4929-bc28-cf1baf125f22`, no issues, one repeat authorized.
- Fixture build: `6ddb4ef5e2654bad8f12f099af8e42c8`.
- Executable SHA256: `c2fa6af7238061c97b6a537e8dc1eb2708f74cdb781362f1c9e5183890cac946`.
- Linked-source fingerprint: `96cc86ec3c533dec9ee1c3b785c5008cc6190bc6d6a0d1d5aecabf1bc1564913`.
- Successful private session: `ss-uno018-ui-hyf8p87u`.
- Observed full `StartAsync` completion, actual native management/page surfaces,
  programmatic real-VM/store CRUD/settings invariants, native preview visibility,
  replacement popup visibility after asynchronous warmup, graceful shutdown and
  zero active owned Job processes **before** emergency cleanup.
- All observed visible surfaces were inside physical bottom-left work area
  `(-1920,1506,0,2538)`; foreground and clipboard sequence remained unchanged at
  checks. Owned logs/stdout/stderr passed the shared privacy scanner and its planted
  UTF-8/UTF-16 controls. The default candidate/legacy profiles were not opened.
- This does **not** establish rendered button/key/pointer delivery, native import
  pickers, dialogs, background/tray callbacks or native insertion/Copy.

An earlier session, `ss-uno018-ui-eyn4w6xv`, returned BLOCKED. Its private files
contain every phase/acknowledgement and graceful receipt, one `candidate_started`,
zero ERROR lines and empty stdout/stderr; offline graceful/privacy checks pass.
The initial observer did not retain a failure reason, so the cause is not asserted.
The corrected observer records fixed private failure tokens and allows a bounded
three-second pre-kill Job-accounting drain. The successful repeat does not turn the
first result into PASS. `FULL_HOST_OBSERVER_REVIEWED` was restored to false.

## Scene 6A — actual clipboard-only ordinary restoration

- Source baseline: `736cac2`, with **only** the three reviewed execution gates
  temporarily enabled in `.claude/worktrees/uno019-native-trial`; root remained
  default-denied. Worktree gate edits were restored afterwards and its status was
  checked clean. No prototype `NativeEntryReviewed` edit occurred.
- Adapter confirming review: `528bd07a-9c57-46e3-972e-41ed1c4ad7da`, no issues,
  narrowly cleared clipboard-only controls. Bootstrap review
  `fe28f50e-217f-41a3-8e34-000a03ae542c` required graceful pre-assignment cleanup
  and deadline ordering; `3c90e2b` applies those corrections before this build.
- Strict fixture build token: `79e6a3ae5e4740aeb37d3292f94c0041`.
- Fixture SHA256: `7e89efdacd274ea5f90aeed88f3760c25602b5bc83347b76bdbd6136ebd5eea8`.
- Guardian SHA256: `feed4e7855e2a0650b93f0ac8bfe4e592c1226e92d93abc638a52b740124c6ea`.
- Owned target SHA256: `b2e1332b755ce30ac3e1570ed5eb3309e38d0efc30b53b994413d1a96bfb58cb`.
- Enabled linked-source fingerprint: `c677e86c9ac13db3257c0174528a0b4658301609e4ed3e2d66e561c6fff6be09`.
- Session: `ss-uno018-ui-r_byzpl1/ss-uno019-native-2e97aea5274140a1b63275b550160ecb`.
- Outer diagnostic wrapper: `ss-uno018-ui-ctdr5r_d`; exit 0, stderr empty. Its
  bounded wait never terminates a pending coordinator or guardian.

The coordinator and target/candidate started outside Jobs; the independently
pumping guardian was authenticated and ready before only worker/target handles
entered the worker Job. The actual scoped helper publication returned Swapped
with recovery pending. It verified the complete synthetic publication, then
restored and verified all captured original representation bytes under the
clipboard lock. The worker drained the same session, shut down the real host,
reaped the target before stopping WinUI, and the retained guardian exited settled.
The observer required exact ordered phase/result receipts, one completed host
startup, no ERROR/stderr and a clean planted-canary privacy scan. Foreground stayed
unchanged. No `SendInput`, focus transfer, hotkey registration or Copy commit was
reachable in this scene (`allowInsert=false`, `allowCopy=false`, control-only).

Original supported bytes are preserved **plus** the declared run ownership marker
and three protective zero-valued privacy markers. This is not identical format-set
restoration. The guardian never discards RAM while recovery is unsettled, and the
outer observer never tree-kills it.

### Baseline boundary before the next scene

The restore intentionally retains its ownership marker. A fresh session refuses
that marked baseline. The operator must save anything valuable and copy harmless
plain text from Notepad before the next scene; then read-only capability/marker
checks must pass. No agent clears the marker by overwriting the current clipboard.
The operator was asked for this necessary preparation while implementation
continued. Stopped-worker recovery, changed-owner refusal, native shortcut/input,
Copy and rendered management acceptance are still separate required evidence.

## Read-only prerequisites (not restoration acceptance)

Before Scene 6A, aggregate checks found zero held input, idle >=1500ms, four builtin
clipboard representation handles (408 bytes total), no custom formats, and an
outside-Job launch context. They did not copy or log clipboard contents. A separate
memory-only GlobalAlloc/GlobalSize/GlobalFree check returned exact sizes for
4/16/46/64-byte allocations; it did not access the clipboard. Native writes now
verify actual allocation sizes and exact bytes rather than guessing padding.
