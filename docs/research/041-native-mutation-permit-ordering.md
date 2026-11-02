# 041 — Native mutation permission follows keyboard permit

Test-harness safety correction and registered row/main route implementation.
No new native execution is claimed by this checkpoint. Product authorization and
all eight source-review gates remain closed at rest.

## P1 found after the accepted milestone

Follow-up audit `593e0557-4a6d-42c1-b3aa-ff6f9bfa7435` confirmed research 039/040
scoped PASS evidence, then identified two concrete fixture races:

1. The positive-scene lease admitted native Input/Clipboard before coordinator
   and worker keyboard lifetimes were armed. Unexpected popup Enter/click could
   start a VM insertion during the pre-permit window, then dispatch after idle.
2. Unexpected UI insertion during the keyboard pipe awaits could already be in
   flight when the worker received its permit. A zero-event agent Enter could
   then send `keyboard_settled` prematurely across an await despite active VM,
   gate or recovery work.

The accepted operator-only-chord/idle traces did not exercise these races; their
scoped results remain valid. Further interactive insertion was paused pending
this correction.

## Correction

`PositiveNativeMutation` is a separate one-shot grant:

- App permission retains original Capture/Focus/Insert/Shortcut/Copy and
  clipboard-only control admission. Input and Clipboard require the native grant
  as well as the existing positive lease.
- The worker first consumes coordinator `keyboard_permit` and arms its own
  keyboard lifetime. It then grants mutation only if popup is shown, VM is not
  inserting and gate is idle. Unexpected work during the pipe await closes the
  grant permanently and fails into the keyboard-draining path.
- Undispatched Enter can fast-settle only for proven zero accepted events with
  both lifetimes armed, no active VM/gate work and no pending recovery. Native
  mutation permission is revoked synchronously before awaiting its settlement
  receipt; receipt failure retains the keyboard lifetime.
- The native grant closes irreversibly on completion, failure, and before drain.
  Guardian restoration and cleanup remain permission-independent. No generic
  key-up, permission relaxation, focus workaround or unsafe Job termination is
  introduced.

## Registered row/main paths

Worker `ca0bbef7`, integrated as `62213a7`, adds `registered-row` and
`registered-main`. Both share the reviewed operator-delivered F11 registration
and readiness route without posting WM_HOTKEY. They invoke actual existing
popup/main-row VM callbacks after the keyboard permit, retaining exact-target
readback, duplicate Busy, usage-once and recovery checks. Their action proof is
`vm_callback`, not native pointer, UIA click or native Enter. Registered-family
allowlists, proof labels and readiness-file ACL ledger checks are shared.

## Validation and review

- 138 fixture tests, 276 candidate tests, 436 Core, 616 Presentation and 131
  candidate Python tests pass sequentially after integration with the separate
  rendered file-picker fixture work.
- Strict false-gate Native AOT fixture build:
  `600f55aa36d341b2bd34a2b7e08390fe`, SHA256
  `628cfe6187e3a246e6bd205697bb9990aca721ea91d7f7f0bf83ffd3a570a1f1`.
- Confirming review `3422f8e5-b95f-4cb4-bb82-f31acb6c6774` found no issues,
  cleared both P1 races and registered-family source integration, and approved
  fresh-artifact prerequisites followed by authorized operator runs.

Fresh enabled artifact controls and actual registered row/main runs remain to
be performed. None of this closes remaining picker/Copy/hover/coexistence/fault
matrix requirements or Plan 018/019 completion.
