# 040 — Native preview/expiry and explicit Copy boundaries PASS

Scoped Plan 019 T7 fixture evidence, not full row 8 completion or a production
native unlock. Same source and artifacts as research 039; no source edits between
these runs. The temporary input gate was restored false afterward.

## Binding and parent observations

Trusted wrapper `ss-uno018-ui-c0619b152ac7dd2f10d398b6b2e85028` returned exit 0
with empty stderr, using the exact accepted registered-enter observation from
research 039 as its trusted in-memory baseline witness. Each scene ran in a fresh
owned profile/session on the primary-landscape trial variant:

| Scene | Session | Parent clipboard epoch check | Owned SQLite count / usage |
|---|---|---|---|
| preview | `ss-uno018-ui-01233d7d9b12692005126cec354a6768/ss-uno019-native-edf07654100047a6af066a3420358598` | unchanged | 1 / 0 |
| expired | `ss-uno018-ui-26db3ee500bf0b48f149f5518c990426/ss-uno019-native-22601da1f9d648fb95900f697f842912` | unchanged | 1 / 0 |
| copy | `ss-uno018-ui-7c15f81ddef478ff88b9abee86531b8d/ss-uno019-native-c6cd5e49b15b4752baeacff6b133fc2a` | changed | 1 / 0 |

All receipts report source fingerprint
`611c71a3c829e4fb6fb94f5adeee8aca930bfb2bf686d61f2959e08a7b1e73f3`,
`positive_scene_settled`, `positiveInputImplemented=false`,
`shortcutProof=none`, and `actionProof=vm_callback`. Native stdout is exactly
`guardian_ready` → `awaiting_owned_a` → `<scene>_settled`; stderr is empty. The
runner validates readiness, error-free single host startup/full shutdown, privacy
scanning and planted negative controls before accepting each result.

The outer trusted parent queried only `GetClipboardSequenceNumber` before/after
each complete scene (no clipboard contents read by that observer), required exact
equality for preview/expired and inequality for Copy, and independently opened
only the owned synthetic SQLite file read-only for aggregate count/usage queries.
Those extra assertions all passed.

## Verified native-boundary behavior

- Preview: the actual host/window lifecycle showed preview, exercised main/popup
  Copy and main/popup selection attempts plus reopen, and closed on selection.
  Broker Capture/Copy-commit/paste counters stayed zero; persisted usage was zero.
  This is VM-callback/native-surface evidence, not pointer or physical Enter proof.
- Expired: exact Target A preparation occurred before expiry under narrowly
  scoped Focus validation. The real monotonic capability expired at its 20-second
  lifetime; the scene explicitly waited 21 seconds and independently measured
  that delay. Logs record `candidate_native_expired`. After expiry it exercised
  both preview and non-preview outer-gate admission. Inner session validation of
  Copy/Insert/Capture still succeeded (55-second session lifetime), isolating the
  outer capability expiry rather than an unrelated adapter denial. No native
  capture/paste/Copy commit, clipboard epoch change or usage occurred.
- Explicit Copy: actual `UnoAppHost.RunCopyOperationAsync` → main-row
  `SnippetsPageViewModel.CopyRow` → guarded clipboard service committed exactly
  the fixed synthetic Unicode text and verified readback. Logs contain exactly
  one `snippet_row_copied`. No paste, target capture or usage credit occurred.
  This intentionally commits the synthetic Copy content rather than restoring
  the old baseline; it is not an insertion restoration claim. The original RAM
  backup is released only after the explicit Copy commit succeeds.

All three traces reach `candidate_shutdown_complete`. Parent read-only process
enumeration found zero candidate, guardian and Target018 fixture processes. All
eight source flags were restored false, product capability remains null, and the
prototype insertion entry gate remains false. No pending recovery was killed.

## Limits

Popup-row/editor/footer Copy and native context/pointer controls are not covered
by the successful main-row VM Copy callback. Ordinary user clipboard history is
not treated as guaranteed recovery. After Copy, this run's committed marker is
not a restorative-session ledger witness; future mutating tests must use a freshly
authorized harmless baseline or another valid source of restorative authority.
Independent audit `593e0557-4a6d-42c1-b3aa-ff6f9bfa7435` verified the three
scoped PASS results with the same read-only binary/SQLite/live-process tooling
limits noted in research 039. It separately identified a P1 fixture permission
ordering gap for unexpected operator popup input before keyboard-lifetime arming.
The accepted 039/040 traces remain valid within their operator-input scope; no
further interactive positive trial may run until that gap and the adjacent
undispatched-settlement race are fixed, tested and reviewed. All source gates
remain closed in the meantime. The subsequent correction and confirming security
clearance are recorded in research 041; no unreviewed native retry occurred.
