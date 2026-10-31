# 030 — Clipboard-only prerequisite: bootstrap fixed, execution still blocked

Status: Plan 018 remains incomplete. No paste or pointer input, target launch or positive insertion was performed. Root `Program.NativeEntryReviewed` remains false. The temporarily enabled private worktree source was returned to false after the bounded trials; its ignored trial artifacts are not normal application builds.

## Authorization and read-only readiness

The operator replied **ready** after being asked to save valuable clipboard contents, prepare harmless text and release input. Aggregate read-only checks still reported one key state on Windows and Mac, with no mouse/momentary-modifier/latching-category state held. Individual key identities were never logged and no key-up/reset was sent. Input scenes therefore remain blocked; these observations do not establish whether the state is physical, stale remote input or another source.

Mac native read-only General snapshot against bundle `xFKtxt` (entry SHA256 `c84926143cffa349c51f61618b3009ae91a77f1c381040d02344ffaf2e12c0ee`) returned `REFUSED reason=unsupported_type`, zeroed secret buffers, mutation/input NONE, restore NOT RUN. Evidence token: `ss-uno018-ui-gof95vi4`. Unknown clipboard data was not cleared or silently treated as preservable.

## No-input scene separation

The old abort scene reached the input/service focus preflight despite promising no input. It now branches before native target launch, focus, SQLite/service creation and any input capability. Its only capabilities are the existing atomic guarded swap and readiness callback; it sleeps for the watchdog's worker-job termination only after `Swapped` with a non-null snapshot. All post-clear failure handling still uses the independent original RAM guardian.

Visible station, current input desktop and >=1.5-second idle checks remain. A bounded three-second poll requires the actual guard to become true; timeout never grants permission. The original positive-input path still checks all held keys/buttons and exact target identity without exemptions. A separately gated `--authorized-readonly-desktop` reports only fixed guard reason tokens and cannot access clipboard contents or post input.

The six swap/refusal tests, two child-temp environment tests and three bounded-wait tests raise the insertion suite from 29 to **40**. Independent review cleared the scene separation, bootstrap fix and final bounded-wait/diagnostic batch. Native execution permission was limited to clipboard-only abort, not positive input. No production files or OS settings changed.

## Bootstrap defect discovered and fixed

Initial trial entry SHA256 `6f758c91dd1dfa45f735d99332e86833e181e4bf3a8ea51272d06ca8fe176ad9` failed `role_evidence_token` before capture (token `ss-uno018-ui-t0j06oml`). A no-permit/stdin-EOF bootstrap diagnostic confirmed child `private_directory` failure. Clearing the child environment had dropped TEMP/TMP/USERPROFILE, causing GetTempPath fallback to the Windows directory, which the unelevated child could not write.

The coordinator now passes only its already-created owner-only diagnostics directory as TEMP/TMP, alongside SystemRoot/WINDIR. Absolute/existing/no-reparse checks apply; no PATH or user-profile environment is inherited. Child diagnostics retain atomic owner-only DACL creation, now nested under the coordinator's private directory. Child directory tokens are therefore resolved beneath the first coordinator token, not directly beneath user temp. Neither coordinator disposal nor failure deletes a live guardian's diagnostics directory.

## Native observations and limits

Strict fresh win-x64 Native AOT publishes passed with empty NoWarn, no managed entry DLL and the required SQLite dependency. The referenced owned-target artifact was hash-pinned but **never launched** in clipboard-only scenes.

| Trial | Entry SHA256 | Token | Observed outcome |
|---|---|---|---|
| Bootstrap corrected | `c2c6275b719b51a6dd69cf36a4948aba60b981fc9c1e4db3996dfa3217058d4c` | `ss-uno018-ui-x48w1t8f` | Complete independent original capture armed; worker did not reach swap-ready; guardian settled |
| Fixed private progress diagnostics | `61c650ddf80bb142d6e75c7776b07d222da01db3ef499e9d5d38f3fb1c1f17e4` | `ss-uno018-ui-b94lasaz` | Permit and exact guardian pin valid; desktop guard refused before swap; guardian settled |
| Final bounded-quiet attempt | `0f6a9b20aa9d16cc27b74a0b820a12c12dd4b9553f2323e62250c4a4a2f4f757` | `ss-uno018-ui-tui9vvgp` | Same valid permit/pin; `desktop_recent_input` / `clipboard_only_desktop_refused` after bounded wait; guardian settled |

The final private worker receipt contains no swap-status receipt or `worker_swap_ready`; the guarded swap was never called. Guardian receipt `guardian_settled_verified_original_representations` in these pre-swap failures follows its unchanged original-owner/sequence settlement branch, **not** a byte-for-byte post-swap restoration test. No after-publication hard abort, cross-process EmptyClipboard ownership, clipboard synthesis fidelity or recovery PASS is claimed. No RECOVERY_PENDING receipt occurred.

A read-only native check with the same minimal TEMP/TMP role environment initially observed recent input; with the bounded wait it reported READY (token `ss-uno018-ui-oixi1gdf`). That does not override the later worker's actual refusal. The cause of its repeated recent-input observation remains unresolved; no specific background program or physical key is blamed. No further native trials were attempted after the final refusal.

## Read-only launch-context comparison

A subsequent bounded experiment started three owned Python children, sampled only GetLastInputInfo/GetTickCount64 metadata, and compared full environment, minimal role environment, and minimal environment plus the same kill-on-close worker job. All three observed identical aggregates over 35 samples: idle minimum 0 ms, maximum 2188 ms, seven samples at/above 1500 ms, and eight last-input timestamp changes. No timestamps, key identities, event contents or titles were logged. No GUI, input posting or clipboard access occurred; all owned children were reaped and the job closed.

This contemporaneous match supports actual session-wide input-timing variation rather than an environment/job-dependent API defect. It does not identify the input source or blame the operator. The earlier refusal cannot be safely converted into PASS, and no further clipboard attempt followed this comparison.

## Next steps

Retain the idle guard and establish an exclusive quiet desktop before retrying; operator activity, remote-control input or keep-awake automation may need to be paused, but no particular source has been identified and none was stopped automatically. On Mac, the operator can deliberately prepare local plain text (for example `printf 'SmartSnippets test\n' | pbcopy` in their own Terminal after saving valuable contents); then repeat read-only capability capture before any mutation. Full General recovery, native insertion/usage, physical shortcut wiring, pointer acceptance and the remaining 018 gates are still NOT RUN/BLOCKED. No 019 start or develop merge is justified by this checkpoint.
