## Install or upgrade

For an upgrade, Exit from the tray and confirm `Get-Process SmartSnippets.App -ErrorAction SilentlyContinue` prints nothing. Verify `(Get-FileHash .\SmartSnippets-win-x64-<version>.zip -Algorithm SHA256).Hash` matches the `.sha256` asset. Extract using `Expand-Archive .\SmartSnippets-win-x64-<version>.zip -DestinationPath "$env:LOCALAPPDATA\Programs" -Force`, then run `%LOCALAPPDATA%\Programs\SmartSnippets\SmartSnippets.App.exe`. In Explorer **Extract All…**, explicitly choose `%LOCALAPPDATA%\Programs` as the destination and replace files; the default destination creates an extra folder.

The executable is unsigned, so SmartScreen may warn on first run: if you trust the verified download, choose **More info** → **Run anyway**, or unblock it in Properties. Start at login keeps working because the executable path is unchanged. Data and logs live separately in `%LOCALAPPDATA%\SmartSnippets`. For a clean upgrade, after Exit you can delete the installation folder before extracting. To uninstall, disable Start at login, Exit, delete `%LOCALAPPDATA%\Programs\SmartSnippets`, and optionally delete the data folder.

See the [install and release guide](https://github.com/rockyway/smartsnippets/blob/main/docs/guides/001-install-and-release.md) for details.
