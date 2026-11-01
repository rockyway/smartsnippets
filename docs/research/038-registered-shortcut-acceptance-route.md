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
