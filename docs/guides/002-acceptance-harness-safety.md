# Desktop acceptance safety

**Optional since 2026-11-01 (Plan 019 §0):** these rules apply only when someone
chooses to run the native-acceptance harness. Native-acceptance proof is not
required for any task. Everyday live checks only need a screen slot, a throwaway
profile, the granted monitor and clean-up.

## Before running

- Obtain an exclusive, explicit screen slot; save work and leave input idle.
- Use the operator-selected monitor and query its current work area. Powered-off
  monitors can remain enumerated; do not reuse stale coordinates.
- Read the plan, harness README and latest review. Closed source gates and blocked
  reviews stay closed until the exact scope is reviewed and authorized.
- Use synthetic data, owner-private profiles/evidence, fresh artifact hashes and
  retained owned process/window identities. Never take over production state.
- Refuse held/recent input, foreign foreground, changed identity or unsafe paths.
  Never relax guards, synthesize generic key-ups or kill by process name.

## Harnesses

| Harness | Boundary |
|---|---|
| `verify-plans-003-009.ps1`, `verify-ui-qa.ps1` | Legacy. May move the real profile aside. Insertion requires verified classic single-document Notepad, not packaged/tabbed Notepad. Exit unowned instances yourself. |
| `verify-uno-candidate.ps1`, `run_uno_candidate_ui.py` | Uno full-host/rendered UI; defaults/gates do not grant native permission. See [fixture README](../../scripts/uno_candidate_fixture/README.md). Picker execution is currently [review BLOCKED](../research/042-rendered-file-picker-review-block.md). |
| `run_uno_candidate_native.py` | Owned Target018 plus independent guardian. Follow the [native runbook](../../scripts/uno_candidate_native_fixture/README.md); pass same-artifact restore/abort/owner-refusal controls before insertion. Ordinary candidate actions remain denied. |

`verify-plan002.ps1` is retired. Do not call internal worker entrypoints directly.
A worker timeout does not prove all descendants or queued operations stopped.

## Clipboard and interrupted runs

- Legacy E/F preserves **text only**, with same-user DPAPI recovery files; rich
  formats are not backed up. Uno retains supported original representations in
  guardian RAM, with declared ownership/privacy markers. Unsupported formats refuse
  before clearing. Clipboard history is not a recovery guarantee.
- Prepare a harmless baseline only with operator authorization, never during
  pending recovery. Explicit Copy commits new content; it is not restoration.
- Never terminate pending guardians/coordinators or uncertain keyboard Jobs.
  Resolve the exact owned recovery protocol without overwriting foreign data.
- Stop new runs after interruption. Identify owned survivors/locks; retain private
  journals. Never delete or merge ambiguous profiles. For legacy recovery,
  quarantine the test profile before restoring `SmartSnippets.verify-backup`.
- Replay only allowlisted registry values with
  `restore-harness-registry.ps1 -JournalPath ...`; restore retained clipboard text
  with `clipboard-worker.ps1 -Mode Restore -BackupPath ...`, as the same Windows
  user. Do not publish recovery files. Failed recovery remains FAIL.

## Evidence

Require fresh run-specific receipts, exact text/usage assertions, cleanup and
privacy scans with planted negative controls. Never log content, titles, tags,
queries, document paths or raw exceptions. Keep evidence private. Posted messages
and VM callbacks are not physical shortcut/pointer proof; missing receipts,
refusals and unrun checks must never become PASS.
