# Agent guidance

Smart Snippets: .NET 10, Windows x64, unpackaged Native AOT, SQLite.
`SmartSnippets.Uno` is the official Windows app (`SmartSnippets.exe`, profile
`%LOCALAPPDATA%\SmartSnippets`). The legacy WinUI 3 app was removed on 2026-11-01;
git tag `legacy-winui-final` keeps it for reference (see `docs/progress/008`).

## Workflow

- Read the relevant plan and latest `docs/progress/` or `docs/handoff/`.
  References: requirements `001`, Uno plans `017`/`019`.
- Numbered documents use the next free number; never renumber.
- Use feature branches/PRs, not direct `main` commits. Worktrees go under
  `.claude/worktrees/`; carry local instruction files into new worktrees.
- Delegate only when authorized. Match reviewer effort to risk and batch related fixes.

## Commands

Run projects **sequentially**: parallel builds sharing Core can fail with CS2012.
Windows is required for UI builds. Root SDK follows `global.json`; Uno commands
must run from `src/SmartSnippets.Uno` to select its pinned SDK.

```bash
dotnet test tests/SmartSnippets.Core.Tests -c Release
dotnet test tests/SmartSnippets.Presentation.Tests -c Release
# From src/SmartSnippets.Uno:
dotnet test ../../tests/SmartSnippets.Uno.Tests -c Release -p:ContinuousIntegrationBuild=true
# From repository root; build/publish only:
pwsh -NoProfile -File scripts/publish-uno-aot.ps1
```

Require fresh native x64 artifacts, SQLite and zero warnings. Never suppress IL
warnings or weaken AOT/trim gates.

## Invariants

- **Uno → Presentation → Core**. Keep WinUI/Win32 in the Uno head; reuse real
  Core/store/VM services through explicit host composition.
- Store/cache work stays on the UI thread. Flush dirty popup data before showing.
  Preserve deferred/recoverable hosts and shared `PopupShowCoordinator` readiness,
  generations and focus ordering; never reveal unfinished surfaces.
- Capture before popup show, revalidate before paste, preserve clipboard and credit
  usage only for `Inserted`. Copy never credits usage; preview never copies/pastes.
- The hotkey popup centres on the monitor under the cursor; the main window centres
  on the primary monitor.
- No reflection: use source-generated JSON, `LibraryImport`/CsWin32 and compiled
  `x:Bind`; verify bindings and compile UI changes on Windows.
- Never log content, titles, labels/tags, queries, document paths or raw exceptions.
  `InsertionTarget.DisplayName` can contain a window title.

## Verification and desktop QA

Verify with targeted tests for the changed code, one strict AOT publish and a short
live check of the feature; run the full suites once before merging. Live checks
need a screen slot from pc-manager, run on the bottom-left monitor when it exists,
never touch the user's real `%LOCALAPPDATA%\SmartSnippets` profile, and leave no
processes behind.
