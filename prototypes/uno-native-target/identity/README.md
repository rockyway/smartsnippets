# Owned native target identity harness — 018 prerequisite

**Composed and natively observed on both desktops; not T4/FG4 PASS.**
See [research 023](../../../docs/research/023-composed-native-target-identity.md) for exact artifacts, failures corrected, runtime receipts and confirming safety review.
The Native AOT console harness calls the actual Presentation `ITargetProbe` adapter
against only its own direct child. There is no arbitrary-PID attach, paste, input
posting, clipboard, shortcut registration, permission prompt or production wiring.

## Ownership and bounded execution

- The CLI accepts only explicit screen authorization, the mandatory watchdog stdin
  gate, contained executable path and pinned SHA-256. Root is derived from the
  harness location beneath `uno-native-target/identity/bin`, never a CLI root.
- Artifact validation mirrors `macos/run_smoke.py` and
  `uno-desktop/verify_evidence.py`: exact target name, platform-specific `bin`
  containment, no symlink/reparse ancestors, native x64 GUI PE without CLR entry or
  JIT-host markers / arm64 executable Mach-O without JIT dependencies. Mac also
  requires fixed bundle identity/executable/package type and bounded strict/deep
  codesign verification. Set-ID/group-or-other-writable Mac images are refused.
- The low-level CLI API guard accepts Windows >= 10.0.17763 x64 and macOS >= 12 arm64; this is not an OS-support promise. The .NET 10 supported macOS launch floor for the Uno app probe is 15 (research 020). Native runs here use current Windows and macOS 26.5.2 only; oldest-OS runtime acceptance remains NOT RUN.
- The factory uses `Process.Start`, no shell, clean loader environment, fixed
  `--authorized-probe-target <GUID>` arguments and redirected pipes. It keeps the
  live `Process` and OS handle, pins image bytes, checks executable path and process
  liveness at each gate. Mac additionally checks direct parent, UID and BSD process
  start timestamp; Windows queries the retained process handle and verifies both
  tokens are unelevated with equal user SID and logon authentication ID. An
  elevated/untokenverified Windows launcher or target is refused, never rerouted.
- Startup requires the exact started PID and distinct positive native identities;
  Windows also requires four distinct HWND/editor handles. Native capture checks
  owned run-specific window class, editor HWND/PID/parentage, or allowlisted AX IDs.
  Capture factory and probe use the **same lease**.
- The image file stays open. Each gate also hashes the current path and checks the
  bundle metadata, detecting ordinary replacement including POSIX rename-over.
  This is a trusted, operator-owned output experiment, not protection against a
  malicious same-user actor racing filesystem swaps around exec.
- Stdin writes and bounded ASCII stdout lines have timeouts; stderr or unsolicited,
  oversized, malformed or duplicate receipts fail closed. Untrusted output is never
  copied to diagnostics. Cancellation/broken pipe/partial initialization close pipes
  and kill/reap only the retained direct child, never a named/global process.
- Diagnostics use an owner-only Windows ACL or atomic Mac `mkdtemp` (0700), with
  0600 Mac files. Only fixed stage/failure tokens are written. The printed private
  token identifies `ss-identity018-<token>*` under OS user-temp; no user path/title
  or editor contents/hash is logged.

**Run through `../run_identity_harness.py`, not by piping a permit manually.** The
wrapper validates the harness hash/native header and target artifact before
launch. It assigns a Windows kill-on-close job *before* sending the launch permit,
or creates a private Mac session/process group. Mac does not poll/reap its session
leader before group cleanup, retaining the group's owned identity against PID
reuse. Normal, timeout, broken-pipe and interrupt paths kill the owned job/group
and reap the directly owned harness. Descendant targets are killed by that owned
job/group if the harness cannot clean them up itself; orphan reaping is by the OS.

The C# deadline is 45 seconds, startup 8 seconds, command write/read 2/4 seconds,
cleanup 3 seconds and individual AX messaging 1 second. **These cannot cancel a
hung synchronous native call.** The separate watchdog enforces a 65-second outer
execution bound plus a five-second non-reaping natural-exit observation and bounded cleanup. Forced watchdog cleanup is failure, never
an observed identity result. SIGKILL of the watchdog itself is not a recoverable
Python finally path; do not externally hard-kill the watchdog during a native run.
On Mac, simultaneous watchdog death and a hung native call requires operator-owned
process recovery; no claim of that failure mode's automatic recovery is made.

## Real composed assertions

Initial empty A/B → capture A/B → owned focus A → B → A → close A → empty/focus B.
Each focus requires exact original-window foreground eligibility and rejection of
the other window in the same PID. Closed A must return `Closed`, never foreground;
B must survive and remain exactly eligible. Usage/insertion is not exercised.

Before each focus command the harness checks the active Windows input desktop (or
Mac logged-in on-console/unlocked session), 1.5 seconds idle, released modifiers
and mouse buttons, with a five-second refusal bound. This is **focus-only** and
not the full held-key guard required for any future input dispatch. Only the
target activates its own windows; Windows target placement remains its existing
bottom-left owned-monitor implementation. Foreground restrictions produce failure,
not synthetic input or attachment to foreign input queues.

Mac window identities are retained AX elements compared with `CFEqual` against
live windows and the focused window, plus frontmost AppKit PID before/after. Tokens
are monotonically allocated opaque registry keys, never `NSWindow.windowNumber`.
CF references and autorelease pools are deterministically released, including
partial initialization. **The Objective-C class-bearing AppKit framework is loaded
once and intentionally retained until process exit**, never unloaded on disposal.
Calls are serialized; no store or production UI-thread state is accessed.

## Build and headless tests (no GUI)

Publish commands explicitly pass global `-p:NoWarn=` because ILLink otherwise
implicitly adds `IL2121` to `NoWarn`. This propagates to referenced projects without
editing production configuration. Verify effective `PublishAot=true`,
`IsAotCompatible=true`, `SuppressTrimAnalysisWarnings=false`,
`TrimmerSingleWarn=false`, `TreatWarningsAsErrors=true`, and empty `NoWarn` with
`dotnet msbuild <project> -p:NoWarn= -getProperty:PublishAot,IsAotCompatible,SuppressTrimAnalysisWarnings,TrimmerSingleWarn,TreatWarningsAsErrors,NoWarn`.
Keep all IL2xxx/IL3xxx diagnostics unsuppressed.

From repository root, sequentially:

```text
dotnet test prototypes/uno-native-target/identity-tests
python -m unittest discover -s prototypes/uno-native-target/identity-tests -p "test_*.py"
python -m unittest discover -s prototypes/uno-desktop/tests -p test_ax_identity.py
python -m unittest discover -s prototypes/uno-desktop/tests -p test_native_target_protocol.py
```

### Windows publish, then separately authorized native run

PowerShell, from repository root (publishing does not run either native binary):

```powershell
$base = Join-Path $PWD 'prototypes/uno-native-target'
dotnet publish "$base/windows/SmartSnippets.Target018.csproj" -c Release -r win-x64 -p:NoWarn= -o "$base/windows/bin/identity-target-win-x64"
dotnet publish "$base/identity/SmartSnippets.TargetIdentity018.csproj" -c Release -r win-x64 -p:NoWarn= -o "$base/identity/bin/publish-win-x64"
$target = "$base/windows/bin/identity-target-win-x64/SmartSnippets.Target018.exe"
$harness = "$base/identity/bin/publish-win-x64/SmartSnippets.TargetIdentity018.exe"
$targetHash = (Get-FileHash $target -Algorithm SHA256).Hash
$harnessHash = (Get-FileHash $harness -Algorithm SHA256).Hash
# ONLY in the separately authorized, exclusive, quiet screen slot:
python "$base/run_identity_harness.py" --authorized-screen-slot --harness "$harness" --harness-sha256 "$harnessHash" --executable "$target" --sha256 "$targetHash"
```

### Apple Silicon publish, then separately authorized native run

Bash, from repository root:

```bash
base="$PWD/prototypes/uno-native-target"
bash "$base/macos/build.sh" # Record emitted target build token and SHA-256.
dotnet publish "$base/identity/SmartSnippets.TargetIdentity018.csproj" -c Release -r osx-arm64 -p:NoWarn= -o "$base/identity/bin/publish-osx-arm64"
harness="$base/identity/bin/publish-osx-arm64/SmartSnippets.TargetIdentity018"
codesign --force --sign - --timestamp=none "$harness"
codesign --verify --strict "$harness"
# Set TARGET_BUILD_TOKEN to the exact build.sh token just observed, not a glob/latest file.
target="$base/macos/bin/target-${TARGET_BUILD_TOKEN}/SmartSnippets.Target018.app/Contents/MacOS/SmartSnippets.Target018"
target_hash=$(shasum -a 256 "$target" | cut -d' ' -f1)
harness_hash=$(shasum -a 256 "$harness" | cut -d' ' -f1)
# ONLY in the separately authorized, exclusive, quiet screen slot:
python3 "$base/run_identity_harness.py" --authorized-screen-slot --harness "$harness" --harness-sha256 "$harness_hash" --executable "$target" --sha256 "$target_hash"
```

No OS permission changes are performed. Existing AX denial produces a refusal,
not a request to change TCC. A successful result is `owned_identity=OBSERVED` /
`owned_identity_harness=OBSERVED`, not T4 PASS. Native identity runtime and strict Native AOT publishing now pass on both hosts; permission-denied/revoked runtime acceptance, popup handoff and insertion remain open. All C# flow/factory tests replace only native process,
window and focus-preflight boundaries; they do not substitute runtime evidence.
