# Official Smart Snippets Windows head

This is the Smart Snippets Windows application. Uno SDK 6.7.30 selects native WinAppSDK rendering; implicit Uno packages are disabled. The local `global.json` pins .NET SDK 10.0.403. No Skia, managed-runtime fallback, macOS or mobile release is included.

## Identity and data

| Surface | Official value |
|---|---|
| Executable/assembly | `SmartSnippets.exe` / `SmartSnippets` |
| Application ID/title | `SmartSnippets` / `Smart Snippets` |
| Win32 manifest identity | `SmartSnippets.App.app` (compatibility identity) |
| AppLifecycle instance key | `SmartSnippets.Main` |
| Profile | `%LOCALAPPDATA%\SmartSnippets` |
| Database | `smartsnippets.db`, existing schema v2 |
| Message-window class | `SmartSnippets.MessageWindow` |
| Tray identity | message HWND + icon ID `1` |
| HKCU Run value | `SmartSnippets`, quoted current executable + ` --background` |

The default profile is opened only after the process claims the single-instance key. There is no profile override option. Core handles existing migrations/backup unchanged. Tests inject throwaway roots; they never open the default profile.

Ordinary startup normalizes an existing recognized startup command (including a moved `SmartSnippets.exe` or a same-directory `SmartSnippets.App.exe`). Unknown/malformed/non-string entries are left alone. Obsolete Run registration is removed idempotently. Failures are nonfatal and get a tray notification with an information-dialog fallback.

## Lifecycle and safety

`UnoAppHost` explicitly composes the real SQLite store and shared VMs on the UI thread. `DesktopInsertionCoordinator` retains single-flight admission, capture before show, exact target revalidation, clipboard ownership/recovery, preview refusal and success-only usage. All Copy consumers share admission and do not credit usage. Explicit clipboard-loss consent remains available through the popup's Paste anyway action.

Settings, tray and the elevated-target footer use coordinated administrator restart. Recovery, tracked operations and dirty main/popup Save–Discard–Cancel checks precede ShellExecuteEx(`runas`). Cancel/failure resumes the host. Only a successful elevated launch commits shutdown; its child waits up to five seconds for `--after-pid` before instance registration/profile access. Hidden main-window state adds `--background`.

Popup warmup, deferred recovery, generation/focus ordering and flush-before-show are unchanged. Close hides to tray; Exit waits for recovery and protects drafts. Diagnostic page/preview launches compose no daily native services; redirected preview remains non-inserting.

## Build, publish and run

From the repository root:

```powershell
pwsh -NoProfile -File scripts/publish-uno-aot.ps1
./publish/SmartSnippets.exe
# Tray-only:
./publish/SmartSnippets.exe --background
```

The build script does not launch the app. It selects this directory's pinned SDK internally and checks fresh Windows x64 Native AOT output, SQLite, strict authored properties and zero warnings. `publish/` is the official output, including all native dependencies and assets.

For headless tests, run from **this directory**:

```powershell
dotnet test ../../tests/SmartSnippets.Uno.Tests -c Release -p:ContinuousIntegrationBuild=true
```

Run Core/Presentation tests sequentially from root; see the [root README](../../README.md). The headless suite links real handlers/composition with substituted rendering and OS boundaries. It is not a live UAC/clipboard/hotkey validation. Use a disposable profile for desktop QA, never the user's real data.

**History:** Previous sources are preserved at tag `legacy-winui-final`; see [progress 008](../../docs/progress/008-uno-official-cutover.md).
