# Install and release Smart Snippets

## Install or upgrade

1. For upgrades, right-click the tray icon and select **Exit**. Confirm `Get-Process SmartSnippets.App,SmartSnippets,SmartSnippets.UnoCandidate -ErrorAction SilentlyContinue` prints nothing. The old and new executables must not run together.
2. Back up `%LOCALAPPDATA%\SmartSnippets` with all versions closed. Download the zip and its `.sha256` asset. Compare `(Get-FileHash .\SmartSnippets-win-x64-<version>.zip -Algorithm SHA256).Hash` with the hash in the asset (case-insensitive).
3. Run `Expand-Archive .\SmartSnippets-win-x64-<version>.zip -DestinationPath "$env:LOCALAPPDATA\Programs" -Force`. In Explorer, select **Extract All…** and change the proposed destination to `%LOCALAPPDATA%\Programs`, then replace the files. The default zip-named destination would add an unwanted level.
4. Run `%LOCALAPPDATA%\Programs\SmartSnippets\SmartSnippets.exe` once to migrate an existing app-owned Start at login entry. Remove the old `SmartSnippets.App.exe` after exiting it, and recreate shortcuts or taskbar pins that reference it. The executable is unsigned; SmartScreen may show Windows protected your PC. If you trust the downloaded and verified release, select **More info**, then **Run anyway**, or unblock it in Properties.

The official app now uses `src/SmartSnippets.Uno` and takes over the legacy identity/profile. Ordinary launch normalizes a recognized existing `SmartSnippets.exe` startup command to its current path, retains the legacy same-directory `SmartSnippets.App.exe` migration, and removes the old `SmartSnippets.UnoCandidate019` Run value. Unknown/malformed registrations are not replaced; check Settings after moving the app. An absent official registration remains disabled. Data, settings and logs are in `%LOCALAPPDATA%\SmartSnippets`, outside the installation. Overwriting can leave files removed by a later release; after exiting, deleting `%LOCALAPPDATA%\Programs\SmartSnippets` before extraction gives a clean installation without deleting data. After display scale changes, the tray may be rescaled until Explorer restarts or the state changes.

## Uninstall

Turn off Start at login in Settings, Exit from the tray, and delete `%LOCALAPPDATA%\Programs\SmartSnippets`. Optionally delete `%LOCALAPPDATA%\SmartSnippets` to remove all data and logs.

## Build from source

Run `pwsh -NoProfile -File scripts/publish-uno-aot.ps1` from the repository root on Windows x64. The script selects the Uno-pinned SDK, performs fresh strict Native AOT/SQLite/zero-warning checks and writes the complete app to `publish/`. It never launches the app. See [the root README](../../README.md) for sequential test commands and prerequisites.

## Maintainers

Only tag a green main commit after explicit approval. Publish an rc first (`v1.0.0-rc.1`), complete acceptance using the downloaded artifact, then publish a second rc if needed for the [version-aware upgrade test](003-release-upgrade-runbook.md). After user sign-off and explicit approval, tag the same accepted commit `v1.0.0`; repeat the version-aware upgrade test from accepted rc to final. Tags start the release workflow and create GitHub Releases. Keep prereleases for history unless the user decides otherwise. Restrict `v*` tag creation with a repository ruleset.
