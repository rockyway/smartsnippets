# Plan 018 — owned Windows target

Synthetic Win32 fixture only: plain Win32, x64 Native AOT, two empty multiline EDIT controls in one PID. No production identity/profile, clipboard API, input synthesis, startup registration or file association. Startup requires `--authorized-probe-target <UUID>`; that switch is not operator consent. Both windows are parked non-activating inside the bottom-left monitor work area. The GUI subsystem avoids an unexpected console activation; stdout is redirected by the harness.

From the repository root:

```text
dotnet publish prototypes/uno-native-target/windows/SmartSnippets.Target018.csproj -c Release -r win-x64 -o prototypes/uno-native-target/windows/bin/<owned-run> --verbosity quiet
```

Strict local AOT/trim warnings are enabled. Inspect the native entry and pin its SHA256 before a separately authorized bounded smoke:

```text
python prototypes/uno-native-target/macos/run_smoke.py --executable <owned-entry> --sha256 <digest> --authorized-screen-slot
```

The shared runner currently resides under `macos/` and selects this sibling output on Windows. It rejects console-subsystem heads, observes only owned PID window metadata, and stops only its child process. Startup observation proves neither exact-window target validation nor paste delivery. No keyboard/clipboard operations are performed by this smoke. Reviewed startup-only Windows execution passed. The high-effort-reviewed `--empty-close` revision also passed native verification: it sends only allowlisted empty-editor/close-A commands, requires native UI-thread receipts and independently observes exact HWND transition `{A,B}` → `{B}`. A wrong same-PID window closing cannot pass. No focus/input/clipboard command is present.

### Content-free verification and owned focus (018 prerequisite only)

The fixed stdin allowlist now also accepts `focus_a`, `focus_b`,
`verify_unicode_a/b` and `verify_multiline_a/b` (expand `/b` to the separate
`_b` command). No arguments, arbitrary expected text or hashes are accepted.
Verification compares editor contents in memory, exactly and without Unicode
normalization. Receipts contain only fixed command name, `ok=0|1`, and for verify
commands `utf16_count` (`-1` if unavailable). It never sets editor text.

Synthetic Unicode fixture: `A\u03A9\u4E2D\U0001F642e\u0301`.
Multiline fixture: that sequence + `\r\nsecond line\r\n`. These escapes specify
fixtures for the harness; payloads are never printed by the target. Extra copies,
LF-only text and NFC normalization fail exact verification.

Focus makes one bounded UI-thread `SetForegroundWindow`/`SetFocus` attempt after
owned parent/editor PID and child checks. It reports success only when both exact
foreground top-level identity (GA_ROOT only for a WS_CHILD foreground, with
matching PID/thread) and editor focus match. A different same-PID top-level is
never accepted. The foreground grant uses the same exact-root rule. Foreground restrictions produce truthful
failure; no input synthesis, AttachThreadInput or foreign activation is used.
The composed, hash-pinned direct-child identity harness and mandatory outer
watchdog are documented in `../identity/README.md`. Native exact-focus/empty/closed-window identity verification now succeeds on both desktops; see [research 023](../../../docs/research/023-composed-native-target-identity.md). Positive pasted-text verification remains NOT RUN; the fixture comparison policies are unit-tested, not a paste-delivery PASS.
