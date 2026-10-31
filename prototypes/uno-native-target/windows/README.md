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
