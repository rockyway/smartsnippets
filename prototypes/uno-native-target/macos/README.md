# Owned macOS two-window target — Plan 018

Synthetic AppKit fixture only; not the product, mobile app or keyboard extension. Swift compiles directly to arm64 native code. This does not establish .NET/Uno mobile AOT feasibility.

```sh
bash build.sh
```

Builds/signs/verifies a unique ignored `bin/target-<token>/SmartSnippets.Target018.app` with identity `org.smartsnippets.feasibility018.target`. Private compiler/signing logs stay owner-only in OS temp. It never runs the app. No Developer ID/notarization claim.

The executable refuses normal launch (exit 2) unless passed `--authorized-probe-target <canonical-UUID>`. That flag is not consent: use only inside the standing authorized guarded test workflow. A reviewed startup-only artifact passed bounded native observation/owned cleanup; see research 016. The current stdin-control revision also passed high-effort review and native empty-editor/owned-close verification.

The fixture creates two empty editable `NSTextView` windows in **one PID**, exposes fixed accessibility editor identifiers and prints only `target_ready pid=<number> a=<window-number> b=<window-number>`. Window titles contain synthetic per-run identity tokens but are never logged. The target has a standard responder-chain Paste menu so a later genuine Command+V can be tested; it neither synthesizes input nor reads/writes the clipboard itself. It has no database, file-open, startup or permission settings.

Windows are placed inside the selected leftmost/bottom screen's work area (AppKit coordinates increase upward). Ordinary launch does not explicitly activate the application. It must remain owned and blank/synthetic throughout testing; never paste operator data into it or reuse it for documents.

Before input: validate the exact native artifact/hash/bundle identity, pin the started process and both native windows, verify current target PID and exact AX window identity immediately before dispatch, hold a bounded watchdog, and acquire a complete recoverable clipboard snapshot. A missing or unsafe snapshot refuses mutation; never save plaintext operator clipboard to disk. Post/readback verifies actual owned target content independently of event-post API return. Stop only processes started by this run, not process names.

Current status: earlier native startup/owned cleanup PASS; Integrated capture/handoff foreground revalidation, paste dispatch and General clipboard recovery remain NOT RUN; the bounded owned AX identity probe below passes. The reviewed stdin protocol (`empty_a`, `empty_b`, `close_a`, `close_b`) dispatches owned editor-length checks and owned close actions on the main actor. The reviewed macOS-only `focus_a`/`focus_b` fixture commands activate only the fixture's own windows and await native focus readiness. EOF is harmless; it does not implicitly close a window. The shared `run_smoke.py --empty-close` candidate requires empty-editor callbacks and independent OS window-ID transition `{A,B}` → `{B}`; count-only matching cannot pass. The `--empty-close` flow sends no focus/input/clipboard commands. High-effort review and actual bounded native control verification both passed for this revision. Integrated original-target capture/handoff revalidation and insertion remain unverified.

## Public AX identity probe — macOS PASS (partial T4)

`run_identity.py --executable <owned-target-entry> --sha256 <digest> --authorized-screen-slot` runs the reviewed, bounded identity experiment. It validates the native bundle/hash/signature and owns both the target and a disposable read-only AX observer. The observer independently verifies sibling parentage and the live target image before loading AX; `AXIsProcessTrusted` is queried without requesting permission. Missing trust fails without prompting or changing TCC.

The target exposes fixed synthetic AX window identifiers (not titles). Only its own main-actor `focus_a`/`focus_b` commands activate windows; no mouse, keyboard or clipboard operation is performed. Idle/modifier/pointer preflight precedes focus commands; this is not a relaxation of full held-key posting guards. The observer retains the original live AX references, checks AppKit `NSWorkspace.frontmostApplication` PID before and after exact `CFEqual` AX focused-window identity through A → B → A, rejects A while B has focus, then requires A absent and the exact B reference present after A closes. All AX attributes are limited to windows, identifiers and focus, never titles or editor text. Public APIs only; no private window-ID bridge or geometry/title heuristic.

This bounded live-reference experiment is not a durable cross-process/window-recreation identity scheme or a production adapter. The parent bounds the run to 30 seconds plus owned-child cleanup; AX messaging timeouts are also configured to one second. No target input, shortcut capture, popup handoff, paste delivery or permission-revocation acceptance is claimed. Pre-live review found and corrected asynchronous activation-readiness and interrupted-cleanup gaps. Focus commands now await active-app/key-window/editor-responder readiness across main-actor yields for at most two seconds; a background semaphore serializes stdin commands without blocking AppKit. Parent cleanup suppresses broken-pipe close errors and escalates interrupted waits to owned-handle kill/reap. Symmetric sibling rejection and bounded idle polling have regression coverage. The corrected Swift 6 warnings-as-errors build/signature check passed (`fwMRB7`, SHA256 `74bc52488f34e8a7d5800c043ae6aaed09ecefb8d3ae834c034b99d20298aae5`). Confirming pre-live review cleared the focus/cleanup corrections. Initial native runs (`ss-uno018-ui-l9q5hbwn`, `ss-uno018-ui-cp6ljy0q`, `ss-uno018-ui-wxdk_trx`) failed at system-wide `AXFocusedApplication` with `kAXErrorCannotComplete` (-25204), despite readable owned AX windows. The observer now uses the existing public AppKit foreground-PID pattern instead of that failing system-wide query, while retaining exact AX window equality and race checks. Native verification then passed (`ss-uno018-ui-swb_yxqi`): A → B → A exact identities, same-PID sibling rejection, closed-A absence/exact-B survival, clean observer exit and owned target cleanup; no keyboard/mouse/clipboard use. Focused post-run review cleared the AppKit query correction; its non-blocking stale-status wording finding is corrected. This does not complete T4 or Plan 018.

### Content-free exact editor verification (018 prerequisite only)

The fixed stdin commands additionally accept `verify_unicode_a`,
`verify_unicode_b`, `verify_multiline_a`, `verify_multiline_b`,
`verify_shared_crlf_a`, and `verify_shared_crlf_b`. No arguments, arbitrary
expected text or hash receipt is supported. The native editor is read in memory,
not mutated, and compared by exact UTF-16 sequence (not Swift's canonical Unicode
equality). Receipts add only `utf16_count` (`-1` if the window is unavailable) to
fixed command name and `ok=0|1`.

Unicode fixture: `A\u03A9\u4E2D\U0001F642e\u0301`.
Native multiline: that sequence + `\nsecond line\n`.
`verify_shared_crlf_*` instead expects `\r\nsecond line\r\n`, explicitly
separating the unchanged Presentation insertion service's CRLF transform from
native Mac LF expectations. Neither payload nor hash is emitted. Duplicate,
normalized, wrong-newline and closed-editor contents cannot pass verification.

Existing `focus_a/b` commands remain owned-window-only bounded AppKit focus
operations, with no synthetic input. The C# identity adapter is composed in the separately authorized, owned-child watchdog harness; see `../identity/README.md`. Mac native compilation and exact-focus/empty/closed-window identity verification now succeed; [research 023](../../../docs/research/023-composed-native-target-identity.md) records artifacts and cleanup corrections. Positive pasted-text verification remains NOT RUN, not T4 PASS.
