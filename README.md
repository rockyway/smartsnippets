# Smart Snippets

A local-first Windows snippet manager: global shortcut, searchable popup, exact-target paste with clipboard restoration, folders, Copy, JSON import/export, tray and start with Windows.

The official application is **`src/SmartSnippets.Uno`**, using Uno SDK tooling with native Windows App SDK rendering. It is .NET 10, unpackaged, self-contained Windows x64 and Native AOT. Core and Presentation are shared libraries; macOS/mobile are not shipping targets.

## Build and test

Install .NET SDKs selected by root `global.json` and `src/SmartSnippets.Uno/global.json`, Python 3, PowerShell 7.2+, and Visual Studio C++ desktop/Native AOT build tools with the Windows SDK. UI builds require Windows x64.

Run projects **sequentially**:

```powershell
# Repository root; Core/Presentation also run on Linux:
dotnet test tests/SmartSnippets.Core.Tests -c Release
dotnet test tests/SmartSnippets.Presentation.Tests -c Release

# Select the Uno head's pinned SDK by changing directory:
Push-Location src/SmartSnippets.Uno
dotnet test ../../tests/SmartSnippets.Uno.Tests -c Release -p:ContinuousIntegrationBuild=true
Pop-Location

python -B -m unittest discover -s scripts/tests -q
pwsh -NoProfile -File scripts/publish-uno-aot.ps1
```

The publish script performs locked restore, a fresh native compilation, effective-property and zero-warning checks, and x64 native PE/SQLite checks. It replaces `publish/` only after successful validation. Output: `publish/SmartSnippets.exe` with `e_sqlite3.dll`, Windows native dependencies and assets. Keep the **whole directory** together; this is not a single-file distribution. No binaries are launched by this script.

For release version stamping, pass `-Version 1.2.3 -BuildNumber 1`. CI and release workflows use the same gate.

## Run and upgrade

1. Exit any old Smart Snippets process from its tray before replacing files. Do not run the legacy/candidate and official versions concurrently.
2. Back up `%LOCALAPPDATA%\SmartSnippets` while the app is closed.
3. Run `publish/SmartSnippets.exe` (or `SmartSnippets.exe --background` for tray-only startup).
4. The official app opens the existing `%LOCALAPPDATA%\SmartSnippets\smartsnippets.db` and settings in place. No candidate-profile import, reset or database schema change is performed. Existing Core migrations and `.bak` behavior are retained.
5. On ordinary launch, an existing recognized `SmartSnippets` HKCU Run command is normalized to the current executable, and the old `SmartSnippets.UnoCandidate019` Run value is removed. An absent official registration is not enabled automatically. Check **Settings → Start at login** after moving the executable.
6. **Restart as administrator** is available in Settings, the tray and the elevated-target popup footer. Clipboard recovery and Save–Discard–Cancel finish before UAC; cancelling keeps this instance running. The elevated child uses the legacy `--after-pid` handoff and preserves background mode.

Copy does not paste or credit usage. Only a confirmed `Inserted` result credits usage. Preview is target-free and never copies/pastes. Existing `--page` and popup preview options are non-inserting diagnostic launches, not normal daily launch.

The removed WinUI app, prototypes and old native-acceptance harness are preserved by Git tag **`legacy-winui-final`** at `793c90b`. Historical plans/research describe their original checkpoints, not the current shipping layout.

See [the head README](src/SmartSnippets.Uno/README.md) and [cutover progress](docs/progress/008-uno-official-cutover.md).
