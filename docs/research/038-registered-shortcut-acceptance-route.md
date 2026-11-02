# 038 — Operator-delivered registered shortcut route

Plan 019 in progress; native execution of this new route is not yet accepted.
The ordinary candidate remains default-denied; no runtime switch unlocks it.

## Source scope

Worker commit `47ab7e2`, integrated as `74f96e8`, adds test-only `registered-enter`
to the private native fixture and observer. It registers/releases/reacquires the
explicit temporary Ctrl+Alt+Shift+F9 shortcut on the actual owned host message
window, publishes a fixed owner-private readiness receipt after exact Target A
preparation, and waits up to 25 seconds for operator delivery. Unlike the posted
scene, it never calls `PostShortcut`, synthesizes the shortcut chord, or releases
operator-held keys. Only subsequent agent-generated Enter/paste use the existing
keyboard-permit, exact target, quiet-input, guardian and recovery controls.

Prelaunch relative-session metadata is not success, readiness or input authority.
Only the exact atomically published readiness literal permits prompting the
operator. The observer retains all desktop/owned-foreground checks and no-kill
pending-recovery policy. Console instructions for this scene explicitly forbid
manual Target A clicking during automatic preparation.

The result label is `registered_os_shortcut_operator_delivered` with
`actionProof=native_enter`. It does not authenticate physical HID origin or turn
posted callback evidence into real shortcut evidence.

## Review and validation

Review `64373ad3-6c53-4226-83b1-3e5e8c6e3ab4` cleared the six-file commit plus the
console-clarity patch. Its non-blocking P2 note (prior-session ACL enumeration
omitted the registered readiness file) is fixed by adding that exact file to the
per-file ACL checks for registered-session handoff, with accepted/denied ACL
tests. No new discovery or arbitrary directory authorization is introduced.

Parent integrated validation before the ACL follow-up: 436 Core, 616 Presentation,
274 candidate, 114 fixture and 124 candidate Python tests pass. Strict false-gate
fixture build `2a0fcf24986842f69aa79ae7aedb913d`, SHA256
`d6c527dd3755bcd09df7a2fec4a596a577e1d2e763af1e405c768ac176c21723`.
Latest build-only ordinary candidate artifact:
`6ae2ef2b0b8a44e2ad3e730945dabd3d`, SHA256
`de4ff0d03e55648587414900aed9c37741545d2e1636e51bb583fd9f9cd3e305`;
no runtime acceptance is implied by either publish.

Required next native sequence: fresh eight-gate fixture plus fresh strict guardian
including reviewed Busy-only retry (`919eb48`, research 037), then exact
same-artifact restore → abort → owner-refusal controls, followed by one registered
shortcut attempt on the explicitly authorized primary landscape. Restore all
eight flags false afterward. Never terminate pending keyboard/clipboard recovery.

## Actual first operator-delivered attempt

Reviewed private build (all eight test flags enabled only for the trial):

- fixture token `1ab8755f46814e2093c57f86b4d816d6`;
- executable SHA256 `ccb2632b2dcc3915091cb6695f25f8b1f1c5963e6b32fd76d055c5a2f88015fa`;
- fresh strict guardian SHA256 `d5e58f5c866bd8fa40651c4899d911ca4111ea5230e88149cfa92482ac6662f9`;
- unchanged primary Target018 SHA256 `6362333761b44c9cd6b18e9e127333bc04193c7f10165e2012c8b4129924476c`;
- enabled source fingerprint `b813a3059ce32fcf29d86acd26f711ca7eca437f4a5030fe184e86cae4278758`.

Parent verified no fixture processes remained, then prepared the fixed synthetic
baseline using the operator-authorized system `clip.exe` command. Trusted wrapper
`ss-uno018-ui-0a49d005192bbe893839d97ae7a47d64` ran all three same-artifact controls
successfully, chaining exact returned observations in memory:

| Scene | Private session |
|---|---|
| restore | `ss-uno018-ui-3a1503a979ca380430bb5f714f0ba683/ss-uno019-native-eb2619f2a4d14fca8ab2bbb937df3023` |
| abort | `ss-uno018-ui-bf9c41369c9f0c3448d0777413004086/ss-uno019-native-6f4a825ca41b403caf3b4ca432277e35` |
| owner-refusal | `ss-uno018-ui-09744e40e38b15f470e0d263820e6031/ss-uno019-native-843e334fa705432ba15d72f21a12f451` |

Each returned `clipboard_control_settled` with the updated guardian; no pending
recovery or manual retry occurred in this sequence. This does not establish that
a Busy condition actually occurred and exercised the new retry at runtime.

Registered session
`ss-uno018-ui-6a35cd80a784e993b6be58d5bee52516/ss-uno019-native-97fc2ac87fa445078438798669fc52a5`
passed exact Target A preparation and published the reviewed readiness literal.
The parent observed that exact session metadata/readiness and prompted the
operator to press the chord. No registered callback was observed before the
25-second bound: the log ends with a sanitized `TimeoutException` at that bound,
without popup opening, Enter/keyboard arm, clipboard publication or insertion
in this positive scene. This does not establish whether the operator pressed a
key, why no callback arrived, or a global-shortcut PASS. The parent explicitly
withdrew the expired keypress instruction.

All eight flags were restored false. Read-only process enumeration confirmed
zero candidate, guardian and Target018 fixture processes. No recovery was killed.
The native registered-shortcut/Enter/usage acceptance remains unproven; a future
operator-ready slot must not reuse the expired prompt. The ACL follow-up adds
one Python test, bringing the current candidate script suite to 125 passing tests.

After a new explicit operator-ready message, a second attempt reused the exact
unchanged source/artifacts and accepted control witness above (no rebuild or
repeat clipboard control mutations). Trusted wrapper
`ss-uno018-ui-b4d1abf55691a9fb30449dc203e680cc` observed registered session
`ss-uno018-ui-5618e66328cbc20948bcc38995903cda/ss-uno019-native-ab8f8b76db104496acde20644f44bd53`.
It again passed owned Target A preparation and exact registered-readiness
observation, then expired at the 25-second callback bound without popup opening
or agent keyboard arm. The parent withdrew the prompt and asked whether the
operator's keypress occurred within the window, rather than inferring user action
or OS delivery from the timeout. All eight flags returned false; read-only
process enumeration again found zero candidate, guardian or target processes.
No insertion or global-shortcut PASS is claimed.

The operator subsequently reported that the F9 combination conflicts with another
application and explicitly requested another combination. The test-only gesture
is changed to Ctrl+Alt+Shift+F11 (VK `0x7A`), with matching observer instructions,
README and assertion. This is a reported conflict, not proof that `RegisterHotKey`
itself returned a conflict (both prior runs reached registered readiness). No
registration is stolen or unregistered from another application; all original
conflict, release/reacquire, source-gate and input guards remain. The 125 Python
and 114 fixture tests pass after this narrow operator-selected gesture change.
The changed source requires a fresh fixture fingerprint/artifact before retry.
