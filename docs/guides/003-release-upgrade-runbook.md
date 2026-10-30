# Version-aware release upgrade acceptance

This procedure supersedes the executable-name and byte-identical startup-value assertions in plan 008 R2 and R14; historical evidence remains unchanged. Use an authorized isolated test profile and the acceptance harness safety guide. Do not run old and new executables together.

1. Install release A, seed synthetic snippets and settings, enable Start at login, record its raw Run value and type, and exit. Confirm both `SmartSnippets.App` and `SmartSnippets` processes have exited.
2. Extract the verified release B archive into the same directory. Launch its `SmartSnippets.exe` once, then verify the version, snippets, settings and enabled startup state.
3. For legacy-name A to renamed B, verify the exact app-owned Run command changes from the old executable to the new quoted path plus `--background`, retaining `REG_SZ`. For renamed B to renamed C, verify an already-current command remains unchanged.
4. Exit, verify both process names have stopped, remove the old executable, and recreate any old shortcuts or taskbar pins. Log in again and verify background startup; verify snippets, settings and version again. Record evidence for both upgrade paths before release sign-off.
