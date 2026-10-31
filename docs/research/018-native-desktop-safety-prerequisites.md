# Native desktop safety prerequisites — Plan 018

Status: research / partial implementation; not desktop feasibility PASS or approval to start 019.

## macOS held-input condition

The native preview reaches real layout and native/logical search focus, but the full held-input guard refuses posting. Read-only aggregate checks report one held key in both combined-session and HID state despite over three hours of event inactivity. Return, modifier/function groups and mouse buttons are not reported held. No key identity/text is recorded. The physical cause is unknown; inactivity alone is not proof that a key is physically released.

Do not synthesize an unowned key-up, log out, reset WindowServer, change keyboard settings or weaken the external-paste guard to obtain PASS. A reviewer suggested a separate private-source own-PID preview experiment, but it is not implemented or accepted as a replacement for a clear external-input guard. Continue non-input work while the host condition remains. Native input gates must be retried only after a safe preflight succeeds.

## Pasteboard access is distinct from Accessibility/event-posting permission

Official sources:
- [NSPasteboard.accessBehavior](https://developer.apple.com/documentation/AppKit/NSPasteboard/accessBehavior-86972)
- [NSPasteboard.AccessBehavior.ask](https://developer.apple.com/documentation/appkit/nspasteboard/accessbehavior-swift.enum/ask)
- Installed Xcode 26.6 macOS SDK `AppKit.framework/Headers/NSPasteboard.h`, inspected read-only (not copied into this repository).

The SDK declares `accessBehavior` as a readonly `NSInteger` enum available on macOS 15.4+:

| Value | SDK meaning | Conservative harness disposition |
|---|---|---|
| 0 | Default: General pasteboard asks on programmatic access; app has not triggered an alert yet | Do not read contents or trigger an alert implicitly |
| 1 | Ask before programmatic access; user-originated paste is allowed | Require an explicit permission/user-action disposition before automated snapshotting |
| 2 | Always allow | Permission only; still require complete bounded snapshot/recovery |
| 3 | Always deny; user-originated paste is allowed | Refuse automated content access without mutation |
| unavailable/unknown | Unsupported selector or unrecognized enum | Do not infer permission |

The read-only capability-query candidate inspects this enum only, using `respondsToSelector:` for runtime availability. It never reads `pasteboardItems`, `types`, strings, data or URLs; never calls clear/write/set APIs; never requests permission or edits settings. The metadata-only query passed independent review and actual bounded native execution on bundle `lsxRSE`, SHA256 `2b96f87b0a9f42a068c25c690b329fb4ed9c11a4b528ec14f4bec940ba54e07e`: Accessibility GRANTED, event posting GRANTED, pasteboard access ALWAYS_ALLOW. No prompt, content read, input or clipboard mutation occurred. This is permission metadata, not snapshot/fidelity or dispatch evidence. AX/event-posting GRANTED alone must not be reported as clipboard access GRANTED.

## Fidelity and recovery constraints

- The SDK explicitly distinguishes per-item pasteboard data from board-level reads. `dataForType:` on the board combines several text/RTF/RTFD items with newlines; it is not a lossless multi-item snapshot. A string-only portable clipboard helper is insufficient.
- Promised types may ask a provider to materialize data. Neither eager-data availability nor bounded completion can be assumed from advertised types. Unknown/private/promised/oversized/unreadable data must conservatively refuse mutation unless a supported complete snapshot and explicit consent policy exist.
- Use only synthetic data in a uniquely named private pasteboard to develop native format/multi-item/change-count checks without touching General. Private-board results do not establish General pasteboard authorization, global Command+V delivery or operator-data recovery.
- Before any General mutation: verify permission, complete bounded in-memory snapshot, current change-count ownership, owned exact target/window, watchdog and recoverability. Do not persist plaintext operator clipboard to disk. New ownership before restore must prevent overwriting the new owner's data.
- AX references must retain/release correctly and compare the exact focused window, not merely frontmost PID. An owned-window close oracle is useful negative evidence but is not foreground revalidation.

Current run hashes and receipts remain in [016](016-uno-native-aot-repair-and-data-probes.md). Windows/macOS Native AOT repairs and passing partial probes do not remove these T3–T5 obligations.
