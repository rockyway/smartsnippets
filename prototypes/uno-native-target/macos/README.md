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

Current status: earlier native startup/owned cleanup PASS; AX identity, paste dispatch and clipboard recovery remain NOT RUN. A new strictly allowlisted stdin protocol (`empty_a`, `empty_b`, `close_a`, `close_b`) dispatches only owned editor-length checks and owned close actions on the main actor. EOF is harmless; it does not implicitly close a window. The shared `run_smoke.py --empty-close` candidate requires empty-editor callbacks and independent OS window-ID transition `{A,B}` → `{B}`; count-only matching cannot pass. It sends no focus/input/clipboard commands. High-effort review and actual bounded native control verification both passed for this revision. Exact-window foreground revalidation and insertion remain unverified.
