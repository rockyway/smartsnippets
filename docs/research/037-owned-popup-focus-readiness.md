# 037 — Owned popup foreground readiness

Plan 019 remains in progress. This is diagnosis, not positive insertion or
physical shortcut acceptance.

## Accepted source and artifact

Review `3c30dc15-a47f-47e6-9507-58aac2db828f` cleared commit `112e9cf`'s
failure-only fixed boolean diagnostics, with no focus retry or guard waiver.
Private primary-landscape trial build `e061547679b846f883f79ce8fed4cc39`:

- executable SHA256: `0eff6a040e866dd548e2bfee35dfeeb4244842e40e237187f908c6f672d189fd`;
- enabled source fingerprint: `0bf2a8b92bc1ef9da242aef83a7fda1d162d43c6440076ca11d5c958f362a669`;
- guardian and Target018 remained the exact pinned binaries recorded in research 036;
- primary landscape work area verified dynamically as `(0,0,3840,2088)`.

## Actual same-artifact controls

Trusted wrapper `ss-uno018-ui-d51207e87052c4906b16336378966173` chained returned
prior-session observations in memory. All three controls returned
`clipboard_control_settled`, exact ordered receipts, empty native stderr, and
passed the runner's host/log/privacy verification:

| Scene | Private session |
|---|---|
| restore | `ss-uno018-ui-eee395a515bf8e941d251dab424a1d6d/ss-uno019-native-ad53476297f74735816562a561420736` |
| abort | `ss-uno018-ui-24514085bbb50c200d87f5cf564dc9a3/ss-uno019-native-4086aaf6d9bb4538b5b74895ca5ad20c` |
| owner-refusal | `ss-uno018-ui-ab034072e35b4c44b5d584053a69bcfe/ss-uno019-native-4b082e2bf1da432095855969d869c98e` |

Owner-refusal remains same-guardian-process HWND A→B, not arbitrary foreign
producer proof. Restoration preserves supported original bytes plus declared
ownership/protective metadata, not identical format-set restoration.

## Positive attempt: precise failed predicate

Session `ss-uno018-ui-0a8ead22c9ae6c196a0f666991c79fa3/ss-uno019-native-c7afb1133aca44d0b3e6751bcc371a91`
passed exact owned Target A preparation (`Prepared`), reached the registered
posted-shortcut callback and opened the captured popup. Its five-second readiness
wait failed before `keyboard_begin`, permit, Enter synthesis or paste. The
failure-only log was:

```text
native_popup_readiness shown=True target=True popup_foreground=False target_foreground=True quiet=True
```

Thus the popup was reported shown with the correct captured target and quiet
input, while foreground remained exact owned Target A rather than the popup.
This does not distinguish an uninvoked coordinator focus callback, outer gate
refusal, adapter admission refusal, or Windows refusing the foreground request.
Posted WM_HOTKEY must not be described as physical shortcut delivery or assumed
to confer Windows' real-hotkey foreground privileges. Parent returned failure;
no positive PASS is claimed. All eight trial flags were restored false.

## Bounded follow-up

Add fixed-token diagnostics at the existing gate and existing single owned-focus
request, retaining original short-circuit guard order and no retries. Optional
focus receipts cover permission/admission/already-popup/target/popup and accepted
or refused foreground request. No handles, PIDs, titles, paths or clipboard
payloads are logged.

Validation: 436 Core, 616 Presentation, 268 candidate, 106 fixture tests pass.
Strict false-gate fixture build `b81fb712970842a48041242ff399cf1b`, SHA256
`3982f9b00b976b4bbfa6d421106f58766a4149347dfd4d01f3895490f59c22ed`.
Review `bbebc1ce-4031-4d12-885a-6b8cf6fbbba7` found no issues and cleared a
fresh same-artifact control sequence followed by one positive attempt.

## Native diagnostics: Windows refused the foreground request

Enabled build `fe767cd3d03c4a0da3c1deb41a02bddc`, executable SHA256
`2babf575695ef72bddd9babd978f589cc8f1ee1aa48c9b33eecf3790c5598838`, source
`a75a2c859b265b2e1db1a3bab8813b7c4e12040fa9c23c8e56130ecd3ff0f096`.

An initial restore attempt refused unquiet startup admission. Another preflight
refused a mouse/foreground change before launch. Neither was counted as PASS.
Wrapper `ss-uno018-ui-12ace468d4b7576172cfb3422236a8fa` subsequently passed ordinary
restore, then abort session
`ss-uno018-ui-45d81070a6aee5a77f1ed11e5dbdc29a/ss-uno019-native-9478d3b460bb46979cc45d25dbcd5c48`
reached publication and retained its guardian/coordinator rather than claiming
recovery. The outer wrapper's bounded observation expired without killing them.

The exact recorded recovery pipe was queried using its bounded `status` →
response → `ack` protocol. It reported `RECOVERY_PENDING Busy`. An explicit
guarded `retry` → response → `ack` returned `settled`. The coordinator then
emitted `abort_restored`, and read-only process enumeration found zero candidate,
guardian or Target018 fixture processes. No forced termination or clipboard
replacement occurred while backup was pending. The extra pending receipt caused
the strict runner's normal-success receipt validator to refuse this interrupted
run; it is not counted as a clean acceptance PASS or trusted returned ledger
observation. Original bytes remained in guardian RAM until verified settlement.

After verified settlement, the parent used the operator's explicit authorization
to prepare the fixed harmless baseline with system `clip.exe`. No clipboard
history backup guarantee was assumed.

Fresh wrapper `ss-uno018-ui-06b8dd080bf396a2b2b613541c267ba9` passed all three
controls on the same unchanged artifact:

| Scene | Private session |
|---|---|
| restore | `ss-uno018-ui-e1f5709aa0982b88ae0779282b0027d3/ss-uno019-native-d61f3e54114640688c90eae61b2efbd0` |
| abort | `ss-uno018-ui-382400fded161bef43b9b4b35060e6da/ss-uno019-native-7e404f2fae9c415dbf5b87458fabcfc9` |
| owner-refusal | `ss-uno018-ui-f34d6eadf2d904e7e37ec7921d4e8d11/ss-uno019-native-fdc7c5de36e84bea80269896a5a17fb4` |

The subsequent single positive session
`ss-uno018-ui-f5621acca40291c3591128e5ffae07a0/ss-uno019-native-8d58bf0a935f4b619f28a47180518761`
logged `owned_target_preparation reason=Prepared`, followed by
`native_popup_focus reason=foreground_refused` and
`candidate_popup_focus reason=refused`. At failure the popup remained shown,
target matched, foreground was exact Target A, and input was quiet. This proves
the existing single guarded `SetForegroundWindow` attempt was invoked and
returned false. It does not establish the internal Windows reason for refusal.
The test stopped before keyboard arm or paste. All eight flags were restored
false. No focus workaround or OS permission relaxation was applied.

Next bounded implementation: a separately reviewed test-only registered OS
shortcut scene, driven by the operator rather than posting WM_HOTKEY. It must
retain all existing guards and must not manufacture a physical HID proof claim.
The ordinary candidate remains native-default-denied.
