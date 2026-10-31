# 025 — macOS General clipboard recovery boundary

Status: design risk discovered during Plan 018 T4; not an approved requirement amendment or General mutation implementation.

## Current host observations

After checkpoint e0fe206, a new metadata-only input query still reports `keys_held=1 buttons_held=0`. The current complete held-key guard therefore continues to refuse native key posting. No key identity was logged and no unowned key-up/reset was sent.

A separately authorized read-only General snapshot attempt against native build `2xe7Kl` (SHA256 `891c2119daafd069e1993713c109b6061dbe15017a9fa43ebffb4b721cdfd987`) again reports conservative REFUSED, private token `ss-uno018-ui-aiknpb1r`. It performs no mutation or restore. This is not permission to discard unsupported contents.

## Ownership counterexample

`NSPasteboard.changeCount` detects changes; it is not a writer identity, lock or atomic compare-and-swap. An active protocol lease does not make arithmetic predictions (`E0+1` means our clear, `E0+2` means our write) ownership proof. An external clear/write in the same interval can produce those observations. Restoring solely from inferred epoch arithmetic can overwrite a newer owner.

Exact temporary representation/marker checks plus the actual post-write epoch are stronger conservative conditions, but they do not make the check and subsequent native write atomic. In particular, worker death after clear but before a complete marker write leaves an ambiguous empty/partial board. A separately retained RAM snapshot prevents loss of the backup on worker death; it does not provide a public OS ownership transaction.

## Implementation requirements retained

- Require complete bounded eager capture and unchanged capture epoch before any mutation; unsupported, lazy/unreadable, capped, unstable or permission-denied content refuses before clear.
- Keep independently recoverable full snapshots in owned process memory, never plaintext files/logs/stdout.
- Revalidate both actual epoch and exact temporary state before conditional restore; preserve a changed/ambiguous owner rather than overwrite it.
- Explicitly report restoration failure/ambiguity; do not convert watchdog exit, empty clipboard, or successful event posting into restoration or insertion PASS.
- Keep private-board single-writer recovery evidence separate from General clipboard acceptance.

## Decision required before claiming full recovery

Public AppKit clipboard APIs do not provide an unconditional atomic snapshot/mutate/rollback guarantee or automatic recovery after arbitrary guardian/OS/process failure. A safe implementation can offer bounded, fault-tested best-effort restoration and fail closed on ambiguity; it cannot truthfully promise restoration for every interruption while also guaranteeing never overwriting a concurrent newer owner.

No requirement is relaxed here. Before accepting T4/T5 General recovery, explicitly settle which bounded failures must recover and which must report ambiguity/refuse. If the approved plan requires unconditional recovery even for an ambiguous mid-clear crash, that guarantee is blocked rather than silently weakened. The separate Mac input/unsupported-current-clipboard host blockers also remain.

Read-only specialist scouting confirmed this boundary against MacPrivatePasteboardProbe, MacPrivateWatchdogProbe, MacGeneralSnapshotProbe and the shared InsertionBoundary contracts. No General mutation experiment was performed to create this document.
