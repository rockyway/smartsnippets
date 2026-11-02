# 039 — Registered OS shortcut → native Enter → insertion PASS

Scoped Plan 019 T7 acceptance on the private Windows fixture. This is not Plan
019 completion, a production native unlock, arbitrary-target support or physical
HID authenticity proof.

## Artifact and source binding

- Private primary-landscape fixture build: `c2d51fd344314dd3ab917034219397df`.
- Executable SHA256: `58856e3931ccef03cc921640cb0d277d2f747cffdfb1c45967ba9e7e02353e6e`.
- Enabled linked-source fingerprint: `611c71a3c829e4fb6fb94f5adeee8aca930bfb2bf686d61f2959e08a7b1e73f3`.
- Updated strict guardian SHA256: `d5e58f5c866bd8fa40651c4899d911ca4111ea5230e88149cfa92482ac6662f9`.
- Primary Target018 SHA256: `6362333761b44c9cd6b18e9e127333bc04193c7f10165e2012c8b4129924476c`.
- OS primary landscape work area independently queried: `(0,0,3840,2088)`.
- Temporary operator-selected shortcut: Ctrl+Alt+Shift+F11 (VK `0x7A`), not the production shortcut.

The operator explicitly selected another combination after reporting an F9
application conflict. Only the fixed test gesture and matching observer text
changed; no conflict stealing, PID adoption, permission expansion or focus
fallback was added. Strict fixture Native AOT/artifact checks passed. Native
operations ran only in the reviewed eight-gate private trial variant.

## Same-artifact prerequisite controls PASS

Trusted wrapper `ss-uno018-ui-56cb4e7a4c2b6c6aca1f613fe93387ea` chained returned
observations in memory and accepted exact receipts/privacy/logs:

| Scene | Session |
|---|---|
| restore | `ss-uno018-ui-e6ff90fd405340c2807ae0e7b85bef10/ss-uno019-native-1e9510f435ef48aba8ba97a9f89df538` |
| abort | `ss-uno018-ui-1bbd3b22833ab3826d770151569f501d/ss-uno019-native-6043b5c0abac4b1b8b35ca0a7985d664` |
| owner-refusal | `ss-uno018-ui-586968eeb440d35afa41f30f7dea7863/ss-uno019-native-f40d3f7718e04a59a7dd283cb58c0aa1` |

The first following F11 attempt refused initial UIA focus (`ForegroundRefused`)
before readiness; the next reached readiness but expired without a callback.
Neither is retroactively PASS. A separate preflight mouse/foreground change was
also correctly refused before native launch.

## Registered callback and native Enter PASS

Trusted wrapper `ss-uno018-ui-a4f915fb9deaf4099362619a786503e2` reused the exact
accepted owner-refusal witness above, not arbitrary discovered evidence. Session:

`ss-uno018-ui-27e1d0bd5fb002666d5b0a229048cf97/ss-uno019-native-805778bc12644676accd7a63f8dbced9`

The parent independently observed the exact owner-private readiness receipt,
ended its turn with the operator prompt, and the operator replied `pressed`.
The runner returned:

- `result=positive_scene_settled`, `scene=registered-enter`;
- `shortcutProof=registered_os_shortcut_operator_delivered`;
- `actionProof=native_enter`, `positiveInputImplemented=true`;
- exact source fingerprint above.

Native stdout was exactly `guardian_ready` → `awaiting_owned_a` →
`registered-enter_settled`; native stderr was empty. The real linked host logged:

1. exact Target A preparation `Prepared`;
2. registered shortcut callback → captured popup `mode=Fresh`;
3. `native_popup_focus reason=foreground_accepted` and outer gate accepted;
4. native Enter through SearchBox → actual popup handler/host operation;
5. single `insert_start`, exact target check `Ok`, direct owned focus path;
6. one guarded clipboard swap, foreground gate true and restore `Restored`;
7. one `insert_result outcome=Inserted`, followed by real store reload;
8. full graceful host shutdown with `candidate_shutdown_complete`.

The scene itself verifies exact fixed Unicode/CRLF target readback (including
final CRLF), one callback/capture/paste, zero unsettled recovery, and persisted
usage exactly once. It also injects duplicate gate/main/keyboard callback requests
while busy and requires refusal; these are not claimed as repeated physical-input
proof. Parent independently opened only the owned synthetic SQLite database in
read-only mode and queried aggregate `(COUNT(*), SUM(usage_count)) = (1,1)`.
The runner's privacy scanner and planted negative controls passed; original
clipboard bytes remained guardian-RAM-only and were restored with declared
ownership/protective metadata.

All eight flags were restored false immediately after result verification.
Read-only enumeration confirmed zero candidate, guardian and Target018 fixture
processes. The ordinary candidate capability remains null, and prototype
`NativeEntryReviewed=false` is unchanged.

## Remaining scope

Independent milestone audit `c1635f83-e01a-49d8-b61e-4d92008de375` cleared the
scoped PASS with explicit read-only tool limitations: it verified artifact/layout,
strict properties/build log, source-hash receipt, exact controls/permit token
chain, native result/logs and source readback/usage/recovery gates, but did not
independently recompute binary digests or query SQLite/live processes. Parent
separately recomputed all three binary SHA256 values and confirmed exact matches,
and performed the read-only SQLite/process checks above. No broader completion
is inferred from this division of evidence.

This PASS does not cover row/context/main
native pointer insertion, every Copy/footer/preview/expired path, wrong-window
and fault matrices, file pickers, coexistence, hover, in-flight/sleep acceptance,
final CI/audit or Plan 018 closure. F9/post-only timeouts and the previous Windows
foreground refusal remain failed/negative evidence, not promoted by this result.
