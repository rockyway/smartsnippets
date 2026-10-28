# 008 — Release hardening and acceptance

**Date:** 2026-10-28
**Status:** Implementation complete 2026-10-28, including E/F/P0–P2/G10 harness and static T0/T15 audits; local build/test/AOT evidence in docs/research/008-harness-audit.md. User-run rc acceptance (T10–T13, T16 sign-off), compatibility matrix, R1–R14, independent T14 and release tags remain pending.
**Size:** L (subplan of [001-master-smart-snippets-v1.md](001-master-smart-snippets-v1.md))
**Area:** `.github/workflows/`, `scripts/`, `assets/icons/` (new), `src/SmartSnippets.App/` (csproj, `Assets/`, `Services/TrayIcon.cs`, `AppHost.cs`, `MainWindow.xaml.cs`, `Diagnostics/` removed), `src/SmartSnippets.Core/Data/ProbeRepository.cs` (removed), `src/SmartSnippets.Presentation/` (tray-state policy), `tests/`, `docs/research/`, `docs/guides/` (new)
**Depends on:** 003, 004, 005, 006, 007 complete, and 009 complete
**Requirement brief confirmed:** 2026-10-28 (G1, 006–008 brief)
**Design reference:** [docs/design/001-smart-snippets-design.html](../design/001-smart-snippets-design.html), section "App icon and notification area"

> **Written before 006 and 007 are detailed.** 006 (popup) and 007 (insertion) are being detailed in
> parallel and will be complete before 008 starts. Every acceptance step in this plan that drives the
> popup or the insertion engine (verify-script sections E and F, the compatibility matrix procedure,
> master §7 checks 1–3, the tray `PopupOpen` hook) names the *behaviour* the PRD requires, not 006/007
> code. **Task T0 re-baselines this plan against the merged 006/007 code** (names, log events, error
> wording, accessible names) and may revise those steps. Revisions are recorded in §12 before work
> starts.
>
> **Revised 2026-10-28 after the external review** ([docs/review/006-008-implementation-readiness.md](../review/006-008-implementation-readiness.md)).
> The user's decisions on findings 4, 6, 7 and 8, and the verification side of findings 1–3, are
> folded into the sections below. §11 maps each finding to its change.

---

## 1. Goal

Ship Smart Snippets v1 as a downloadable, versioned release and prove it meets the PRD:
- **Release:** pushing a `v*` tag builds, tests and Native-AOT-publishes the app, and attaches
  `SmartSnippets-win-x64-<version>.zip` plus its SHA-256 file to a GitHub Release. The exe carries
  the version.
- **Finish:** the design's bracket-and-caret mark becomes the app icon and the tray glyph (light and
  dark taskbars, blue dot while the popup is open, red dot when the hotkey is unavailable).
  Prototype-only surfaces (Diagnostics window, tray item, popup test string) are gone.
- **Acceptance:** the release candidate passes the extended verify script, the performance targets
  and the compatibility matrix on the user's PC. Every PRD §5 criterion and master AC1–AC14 has
  recorded evidence. The master plan is marked Complete.

## 2. Requirement slice (G1, user 2026-10-28)

- **Release.** A pushed tag `v*` (e.g. `v1.0.0`) runs a release workflow:
  - tests;
  - AOT publish for win-x64, with the zero trim/AOT warning gate;
  - zip `SmartSnippets-win-x64-<version>.zip` plus a SHA-256 file;
  - attach both to a GitHub Release with generated notes.

  The tag also sets Version/FileVersion in the exe. Main-branch CI keeps its artifact. There is no
  code signing (the SmartScreen warning is documented), no MSIX and no auto-update.
- **Icons.** The design's bracket+caret mark is used as:
  - the **app icon**: a multi-size `.ico` (16–256), set as `ApplicationIcon` and as the window icon
    through `AppWindow.SetIcon`;
  - the **tray glyph**: single-color light and dark variants, picked by the taskbar theme (HKCU
    `...\Themes\Personalize` `SystemUsesLightTheme`) and switched when the theme changes.

  State dots overlay the tray glyph: blue = `PopupOpen`, red = `HotkeyUnavailable`, none = `Ready`.

  The `.ico` files are produced reproducibly: checked-in generated `.ico` plus the generator script.
  Nothing renders SVG at runtime. Icons load under AOT via `LoadImage` from a file next to the exe or
  from an embedded Win32 resource; the plan justifies the choice.
- **Remove prototype-only surfaces.** These go:
  - the Diagnostics window;
  - the tray "Diagnostics" item;
  - any Settings link;
  - `PopupWindow` `TestString` leftovers, if any remain after 006/007.

  Restart as administrator stays.
- **Verification:**
  - Extend the user's verify script with new sections for the popup (006) and insertion (007) flows,
    keeping the existing sections green.
  - Performance acceptance on the user's PC, recorded, on the release build with a defined
    10,000-snippet dataset: popup ≤300 ms (`popup_ready_ms` log) and search ≤150 ms median
    measured end to end in the popup (`popup_search_ms` log, search text change → results
    rendered, both layouts). The Core search benchmark is kept as a diagnostic only (review
    finding 4, user 2026-10-28).
  - Execute the compatibility matrix and record it in `docs/research`. Targets: Notepad, VS Code,
    Word, Outlook, Chrome/Edge text inputs, Windows Terminal (PowerShell and cmd) and Teams, plus an
    elevated Notepad (the expected message). Columns: Unicode, multiline, selected-text replacement,
    clipboard restored, usage counted once.
  - Spec-vs-implementation audit of PRD §5 and the master ACs, with evidence.
  - Master §7 integration checks.
  - Full wiring audit.
  - Open observations from `docs/research/002`: each one resolved or explicitly accepted. CR line
    endings are handled by 007. Hotkey conflict message reachability is accepted as-is by the user.
- **Housekeeping.** Bump the GitHub Actions versions to clear the Node 20 deprecation warnings
  (small).
- **Out of scope:** new features.

**Covers:** AC13 and AC14 directly. Acceptance evidence for every other AC (AC1–AC12) and every PRD §5
criterion.

## 3. Current state (as-built, `main` at `4861b0e`)

**CI and release**
- `.github/workflows/ci.yml` triggers:
  - `ci.yml:5-9`: `push` to `main` with `paths-ignore` docs/md.
  - `ci.yml:11`: `workflow_dispatch`. Dispatch is refused for the Claude integration (403), so only
    the user can dispatch.
  - No tag trigger: with a `branches` filter alone, tag pushes do not run it.
- Action versions: `actions/checkout@v4`, `actions/setup-dotnet@v4` (`ci.yml:17-18`, `27-28`) and
  `actions/upload-artifact@v4` (`ci.yml:56`). These are the source of the Node 20 deprecation
  annotations.
- The AOT publish and warning gate live inline in `ci.yml:35-55`:
  - publish to `publish/`;
  - fail on `warning IL[23][0-9]{3}`;
  - fail if `SmartSnippets.App.dll` exists;
  - require `SmartSnippets.App.exe` and `e_sqlite3.dll`.

  The same gate is re-implemented in `scripts/verify-plans-003-009.ps1:211-219` (A3). That makes two
  copies already.
- Artifact: `ci.yml:56-59` uploads `publish/` as `SmartSnippets-win-x64-aot`. Size per
  `docs/research/001-tech-validation-report.md:6`: 52.6 MB zip, 287 files.
- No version is stamped:
  - `src/SmartSnippets.App/SmartSnippets.App.csproj:1-28` has no `Version`, `FileVersion` or
    `ApplicationIcon`.
  - `Directory.Build.props` sets only Nullable/ImplicitUsings/LangVersion/IsAotCompatible.
  - `app.manifest:3` hard-codes `assemblyIdentity version="1.0.0.0"`, which is unrelated to file
    version info.
- The exe name is `SmartSnippets.App.exe`. The HKCU `Run` value stores its full path
  (`Services/StartupRegistration.cs:44`).

**Icons**
- No icon assets exist: `git ls-files` has no `.ico/.png/.svg`, and there is no `Assets/` folder.
- The tray uses the stock icon `PInvoke.LoadIcon(HINSTANCE.Null, PInvoke.IDI_APPLICATION)`
  (`Services/TrayIcon.cs:100`).
- `TrayState { Ready, PopupOpen, HotkeyUnavailable }` is at `TrayIcon.cs:11-17`. The `State` setter
  (`TrayIcon.cs:80-93`) only calls `UpdateTooltip()` (`:110-124`), and `TooltipFor` distinguishes
  only `HotkeyUnavailable` (`:107-108`).
- `AppHost.cs:197-199` sets `Ready`/`HotkeyUnavailable` from `HotkeyManager.Status`. `PopupOpen` is
  never set (009 §10, `009-design-alignment-004-005.md:219`). 006 sets it.

  Hazard: `AppHost` assigns the state from two sources (hotkey status now, popup later). Without a
  single policy, a popup close would overwrite `HotkeyUnavailable` with `Ready`.
- The tray messages arrive on `MessageWindow`:
  - It is a **hidden top-level** window (`Services/MessageWindow.cs:50`), so it receives broadcasts
    such as `WM_SETTINGCHANGE` and `TaskbarCreated`.
  - `TrayIcon.OnMessage` (`TrayIcon.cs:167-196`) handles `TaskbarCreated` and the callback message
    only.
- `NativeMethods.txt` has `LoadIcon` and `IDI_APPLICATION`. It does not have `LoadImage`,
  `DestroyIcon`, `GetSystemMetricsForDpi`, `GetDpiForSystem` or `WM_SETTINGCHANGE`.
- The main window (`MainWindow.xaml.cs:25-41`) never calls `AppWindow.SetIcon`. Its custom title bar
  (`MainWindow.xaml:16-22`, `ExtendsContentIntoTitleBar`) is text only.
- No theme code reads the taskbar theme. `ThemeService` (`Services/ThemeService.cs:11-80`) handles
  the app theme per window only.
- Design mark geometry (`docs/design/001-smart-snippets-design.html:212`, symbols `i-app` and
  `i-tray`):
  - **App icon:** viewBox 64. Rounded square `rect x=4 y=4 w=56 h=56 rx=14` filled `#1668d4`.
    Brackets `M21 18h-5v28h5M43 18h5v28h-5`, stroke `#ffffff` 4.5, round caps/joins. Caret
    `M32 21v22M28.5 21h7M28.5 43h7`, stroke `#ffd23f` 4.
  - **Tray glyph:** viewBox 24, `stroke=currentColor` 2.2, path
    `M8 5H5v14h3M16 5h3v14h-3M12 7.5v9M10 7.5h4M10 16.5h4`. Taskbar glyph color `#1b1b1b` on light
    (`#f3f3f3`) and `#ffffff` on dark (`#202020`) (`:373`).
  - **State dot:** `circle cx=19 cy=19 r=4.2`, fill `#2f8bff` (popup open) or `#e5484d` (hotkey
    unavailable), with a 1.6 stroke in the taskbar color (`:373`), which reads as a cut-out ring.
  - App icon sizes shown: 96/48/32/16 (`:360-366`). Rationale: "keeps a distinct silhouette at
    16 px" (`:358`).

**Prototype-only surfaces**
- `Diagnostics/DiagnosticsWindow.xaml` (14 lines) and `.xaml.cs` (61 lines):
  - a SQLite probe through `ProbeRepository` (`DiagnosticsWindow.xaml.cs:30-32`);
  - a start-at-login toggle that duplicates Settings (`:22-23`, `:43-60`);
  - runtime info.
- `AppHost` wiring of that window:
  - `using SmartSnippets.App.Diagnostics` (`AppHost.cs:5`);
  - the `_diagnostics` field (`:44`);
  - the tray callback (`:192`);
  - `ShowDiagnostics()` (`:265-282`);
  - close on exit (`:328`).
- Tray Diagnostics wiring:
  - `CommandDiagnostics` (`TrayIcon.cs:26`) and `_onDiagnostics` (`:37`, `:51`, `:61`);
  - the menu item (`:220`, `:232`) and its dispatch (`:267-268`).

  009 Q2 recorded the removal: "Diagnostics is still removed in 008" (`009-…md:173`).
- `Core/Data/ProbeRepository.cs:8` is used **only** by `DiagnosticsWindow` (grep-verified). The
  `probe` table comes from migration 1 (`Core/Data/Migrations.cs:7`) and is asserted by
  `MigratorTests.cs:22,33,46`. **The table and migration stay**: migrations are immutable, and B
  checks upgrade v1 DBs.
- Settings has **no** Diagnostics link. "Advanced" contains only Restart as administrator
  (`Pages/SettingsPage.xaml:143-149`). It stays.
- Popup prototype leftovers:
  - `PopupWindow.TestString` (`Popup/PopupWindow.xaml.cs:18`), inserted at `:140`.
  - 006/007 are expected to replace both. 008 only verifies that and removes any residue.

**Verification assets**
- `scripts/verify-plans-003-009.ps1` (771 lines):
  - Parameters `-SkipBuild`, `-SkipTests`, `-Only`, `-ReportPath` (`:22-27`).
  - Sections A (build/tests/AOT/static, `:199-235`), B (migrations, `:237`), C (shell, `:286`) and
    D (main window, `:522`).
  - **C11 asserts the tray menu has 6 items including Diagnostics** (`:476`). It must change in this
    plan.
  - The C3 latency check reads `popup_ready_ms` and asserts ≤300 (`:316-323`).
  - A2 runs the benchmark but only checks "Passed" (`:207-208`).
- `scripts/verify-plan002.ps1` (236 lines) is the 002 prototype harness. Its step 6 depends on the
  prototype popup inserting `TestString`, so it becomes historical once 006/007 land.
- `docs/research/002-verification-plans-003-009.md`:
  - Run result: 103 PASS / 0 FAIL / 1 WARN / 5 SKIP.
  - Open observations (`:53-62`): lone-CR line breaks (WARN D1.eol, owner 007), conflict sentence
    unreachable by recording, and the "Snippet text cannot be empty." wording.
  - Manual SKIPs (`:64-73`): UAC restart, real Explorer restart, sign-out start-at-login, visual
    review, locked-DB save failure, C9.conflict.
- The benchmark is `tests/SmartSnippets.Core.Tests/Query/SearchBenchmarkTests.cs`:
  - `[Trait("Category","Benchmark")]` (`:9`), `BudgetMs = 150` (`:14`).
  - Timings are written through `ITestOutputHelper` (`:56-62`). They show only with a detailed
    console logger.
- `docs/research/001-tech-validation-report.md:34-35`: the prototype popup took 18–22 ms. The 006
  popup will be heavier (live list), so the 300 ms target must be re-measured.

**Statuses** (master §5, `001-master-smart-snippets-v1.md:125-132`):
- 004, 005 and 009 are Complete, but their manual runbooks are still pending.
- 008's acceptance run closes the remaining manual items (§7, §6 T12).

## 4. Design

### 4.1 Approach

**(a) One publish gate, three callers.** The inline gate moves into a new
`scripts/publish-aot.ps1`. Its parameters are `-Output <dir>`, `-Version <semver>` (optional) and
`-BuildNumber <int>` (optional). It:
1. runs `dotnet publish src/SmartSnippets.App -c Release -r win-x64`, passing
   `-p:Version=<v> -p:FileVersion=<major.minor.patch.build>` when given;
2. tees the log to `<Output>.log`;
3. fails on `warning IL[23]\d{3}`, a non-zero exit, `SmartSnippets.App.dll` present, or
   `SmartSnippets.App.exe` / `e_sqlite3.dll` missing.

`ci.yml`, the new `release.yml` and verify-script A3 all call it, so the gate cannot drift. The
script uses PowerShell 7 syntax that also runs on Windows PowerShell 5.1, because the verify script
runs under 5.1 (`docs/research/002:16`).

**(b) Release workflow (`.github/workflows/release.yml`).** It is separate from CI, so release
permissions (`contents: write`) never apply to ordinary pushes. Outline:

```yaml
name: Release
on:
  push:
    tags: ['v*']
permissions:
  contents: write          # create the release; nothing else
concurrency:
  group: release-${{ github.ref }}
  cancel-in-progress: false
jobs:
  release:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v5        # Node 24 major current at implementation (T1)
        with: { fetch-depth: 0 }         # needed for the on-main ancestry check
      - name: Validate tag
        shell: pwsh
        run: |
          # ^v(\d+)\.(\d+)\.(\d+)(-[0-9A-Za-z.-]+)?$  else fail
          # git merge-base --is-ancestor $GITHUB_SHA origin/main  else fail ("tag must point at main")
          # gh release view $tag succeeds -> fail ("release exists; delete it to re-run")
          # outputs: version=1.2.3[-rc.1], fileversion=1.2.3.${{ github.run_number }}, prerelease=true|false
      - uses: actions/setup-dotnet@v5
        with: { global-json-file: global.json }
      - run: dotnet test tests/SmartSnippets.Core.Tests -c Release
      - run: dotnet test tests/SmartSnippets.Presentation.Tests -c Release
      - name: AOT publish + warning gate
        shell: pwsh
        run: ./scripts/publish-aot.ps1 -Output publish -Version $version -BuildNumber ${{ github.run_number }}
      - name: Check stamped version
        shell: pwsh
        run: # (Get-Item publish/SmartSnippets.App.exe).VersionInfo: ProductVersion starts with $version,
             # FileVersion == $fileversion; else fail
      - name: Package
        shell: pwsh
        run: |
          # copy publish/* except *.pdb into stage/SmartSnippets/   (fixed name, never versioned)
          # Compress-Archive stage/SmartSnippets SmartSnippets-win-x64-<version>.zip
          # check: every zip entry starts with "SmartSnippets/" and SmartSnippets/SmartSnippets.App.exe exists; else fail
          # "<sha256 lowercase>  SmartSnippets-win-x64-<version>.zip" > SmartSnippets-win-x64-<version>.zip.sha256
      - name: Create GitHub Release
        shell: pwsh
        env: { GH_TOKEN: ${{ github.token }} }
        run: |
          gh release create $tag <zip> <zip>.sha256 --verify-tag --generate-notes `
            --title "Smart Snippets $version" --notes-file .github/release-notes-header.md `
            $(if prerelease) --prerelease
```

- **Generated notes.** `--generate-notes` with `--notes-file` prepends a fixed header to the
  generated notes. The header is `.github/release-notes-header.md`: install steps, the SmartScreen
  explanation, hash verification, and upgrade/uninstall.
- **No third-party release action.** `gh` is preinstalled on the runners.
- **Zip layout (review finding 6, user 2026-10-28).** The zip contains exactly one top folder with a
  **fixed** name, `SmartSnippets\`. The version is only in the zip file name and in the exe's
  version info, never in the folder. Extracting any release into the same parent folder
  (recommended `%LOCALAPPDATA%\Programs\`) therefore always yields
  `%LOCALAPPDATA%\Programs\SmartSnippets\SmartSnippets.App.exe` and overwrites the previous
  version in place. The HKCU `Run` value `SmartSnippets` stores that exe path
  (`"<exe>" --background`, `StartupRegistration.cs:44`), so it keeps pointing at the upgraded exe
  and start at login keeps working without re-toggling. A version-named folder was rejected: it
  nests a new folder on every upgrade and leaves the `Run` value on the old exe.
- **Install and upgrade procedure** (the guide and the release header carry these exact steps):
  1. *Upgrade only:* right-click the tray icon → **Exit**, then confirm the process is gone
     (`Get-Process SmartSnippets.App -ErrorAction SilentlyContinue` prints nothing). Files of a
     running exe cannot be overwritten.
  2. Verify the download:
     `(Get-FileHash .\SmartSnippets-win-x64-<version>.zip -Algorithm SHA256).Hash` equals the hex
     in the `.sha256` asset (case-insensitive).
  3. Extract into the parent folder, overwriting:
     `Expand-Archive .\SmartSnippets-win-x64-<version>.zip -DestinationPath "$env:LOCALAPPDATA\Programs" -Force`.
     With Explorer: *Extract All…* → change the destination to `%LOCALAPPDATA%\Programs` (Explorer
     proposes a folder named after the zip, which would nest the app one level deeper) →
     *Replace the files in the destination*.
  4. Start `%LOCALAPPDATA%\Programs\SmartSnippets\SmartSnippets.App.exe` (first run of a new
     version: SmartScreen steps as documented).

  User data lives in `%LOCALAPPDATA%\SmartSnippets` (database, settings, log) and is never inside
  the zip, so an upgrade cannot overwrite it. Files that an older version shipped and a newer one
  dropped stay behind after an overwrite. The guide notes that deleting
  `%LOCALAPPDATA%\Programs\SmartSnippets` before step 3 is equally safe (same path, data
  elsewhere) and gives a clean folder (R10).
- **Pre-release tags** (`v1.0.0-rc.1`) produce a GitHub *pre-release*. That is the dry run used in
  §8.

**(c) Version stamping.** The tag is the single source of truth.
- `version` = the tag without `v`, passed as `-p:Version`. The SDK derives `ProductVersion` /
  `InformationalVersion` (with `+<commit>` from SourceLink) and `AssemblyVersion` from it.
- `FileVersion` = `major.minor.patch.<run_number>`. It must be four numeric parts, so a pre-release
  suffix cannot go in it. The run number keeps rc and final builds distinguishable.
- `Directory.Build.props` gets a default `<Version>0.0.0-dev</Version>`, so local and unversioned
  builds never claim to be a release.
- Main CI passes `-Version 0.0.0-ci.<run_number> -BuildNumber <run_number>`, so a CI artifact is
  traceable to its run. The artifact name `SmartSnippets-win-x64-aot` is unchanged.
- The app logs `app_started version=<InformationalVersion>` once at start-up, so a log can be tied
  to a build. This is a one-line diagnostic, not a feature (assumption A2).

**(d) Icon pipeline.** Sources, generator and outputs are all checked in. Nothing renders at build
time or at runtime.
- **Sources:** `assets/icons/app.svg` and `assets/icons/tray.svg`, copied from the design's `i-app`
  and `i-tray` symbols with the geometry and colors in §3.
- **Hand-tuned small sizes:** `assets/icons/tray-16.svg` and `tray-20.svg`, plus `app-16.svg`. These
  carry the same mark snapped to the pixel grid (integer stroke widths and coordinates), so 16 and
  20 px stay crisp. The design's stated goal is a distinct silhouette at 16 px.
- **Generator:** `scripts/icons/generate_icons.py`, with dependencies pinned in
  `scripts/icons/requirements.txt` (`cairosvg==<pinned>`). It:
  1. renders each source to PNG at every size, choosing the hand-tuned source for 16/20 where one
     exists;
  2. for the tray, recolors the glyph (`#1b1b1b` for light taskbars, `#ffffff` for dark);
  3. composites the dot (`cx=19 cy=19 r=4.2` scaled; blue `#2f8bff` or red `#e5484d`) after erasing
     a transparent ring 1.6 units wide around it. A transparent cut-out works on any taskbar color
     (accent-colored or translucent taskbars). The design's taskbar-colored ring would show a wrong
     halo there;
  4. packs `.ico` files itself (~60 lines): `ICONDIR` + entries, 32-bpp BGRA DIB with AND mask for
     sizes ≤48, PNG payload for 256. It sets no timestamps, so a re-run gives byte-identical output.
- **Outputs**, checked into `src/SmartSnippets.App/Assets/`:
  - `SmartSnippets.ico`: app icon at 16, 20, 24, 32, 40, 48, 64 and 256.
  - Six tray files `Tray-{Light|Dark}-{Ready|PopupOpen|HotkeyUnavailable}.ico`, each at 16, 20, 24,
    32, 40 and 48 (100–300 % DPI small-icon sizes).
  - "Light" means *for a light taskbar* (dark glyph).
- **Preview:** the generator also writes `assets/icons/preview.png`. It is a contact sheet: every
  size and variant on `#f3f3f3` and `#202020` swatches, for review in the PR. It is not shipped.
- `--check` mode regenerates into a temp dir and fails if any output differs. It is run by whoever
  edits the icons (documented in the script header).

**(e) Loading the icons (AOT-safe).**
- **App icon:**
  - `<ApplicationIcon>Assets\SmartSnippets.ico</ApplicationIcon>`. The SDK embeds it, together with
    the generated VERSIONINFO, as a Win32 resource of the IL assembly, and ILC copies Win32 resources
    into the native exe. This drives Explorer, the pinned taskbar and the `Run`-key/Start display.
  - `MainWindow` calls `AppWindow.SetIcon(Path.Combine(AppContext.BaseDirectory, "Assets",
    "SmartSnippets.ico"))` for the taskbar button and Alt+Tab of an unpackaged WinUI window.
  - The popup is `IsShownInSwitchers = false` (`PopupWindow.xaml.cs:45`) with no title bar, so it
    needs no icon.
- **Tray:**
  - `TrayIcon` loads the variant for (taskbar theme, state) with
    `LoadImage(NULL, <BaseDirectory>\Assets\Tray-….ico, IMAGE_ICON, cx, cy, LR_LOADFROMFILE)`, where
    `cx = cy = GetSystemMetricsForDpi(SM_CXSMICON, GetDpiForSystem())`. LoadImage then picks the
    exact frame from the multi-size file.
  - Handles are cached per (theme, state) and destroyed with `DestroyIcon` on theme change and on
    `Dispose`.
  - If a file is missing or `LoadImage` fails, it logs `tray_icon_load_failed name=<file>` and falls
    back to `IDI_APPLICATION`, so the tray never disappears.
- **Why loose files, not an embedded `.rc`:**
  - The SDK accepts one `Win32Resource` (`.res`), and supplying it **replaces** the SDK-generated
    icon and VERSIONINFO. We would have to hand-author VERSIONINFO and inject the tag version into an
    `.rc` compiled by `rc.exe`: a second toolchain and a second version source.
  - Managed `EmbeddedResource` + `CreateIconFromResourceEx` is AOT-safe but needs our own `.ico`
    directory parser (`LookupIconIdFromDirectoryEx` reads `RT_GROUP_ICON`, not `.ico` files).
  - The app already ships as a folder of 287 files, so seven more `.ico` files add no new
    failure mode.
  - The files are declared as `<Content Include="Assets\*.ico" CopyToOutputDirectory="PreserveNewest"
    CopyToPublishDirectory="PreserveNewest" />`.

**(f) Tray glyph selection.**
- **Taskbar theme:** `TaskbarTheme.IsLight()` reads `HKCU\Software\Microsoft\Windows\CurrentVersion\
  Themes\Personalize\SystemUsesLightTheme` (DWORD). `1` means light. `0` or missing means dark: the
  value is missing on Windows 10 1809, whose taskbar is always dark.
- **Theme changes:** `TrayIcon.OnMessage` handles `WM_SETTINGCHANGE` whose `lParam` string is
  `ImmersiveColorSet`. It re-reads the theme and, if it changed, reloads and `NIM_MODIFY`s with
  `NIF_ICON`. `MessageWindow` is a hidden top-level window, so it gets the broadcast
  (`MessageWindow.cs:50`).
- **Explorer restart:** `TaskbarCreated` also re-reads the theme and the system DPI before `Add()`.
- **State:** the `State` setter calls `UpdateIcon()`. That sends `NIM_MODIFY` with
  `NIF_ICON | NIF_TIP`, replacing `UpdateTooltip`, and logs `tray_icon state=<s> theme=<light|dark>`.
  The log gives the verify script something to assert, because the tray image itself is not readable
  through UI Automation.
- **One state policy (single definition; review finding 7, user 2026-10-28):** a pure function
  `TrayStatePolicy.Compute(bool hotkeyRegistered, bool popupOpen)` lives in Presentation so it can be
  unit-tested on Linux. The result is `HotkeyUnavailable` if the hotkey is not registered, else
  `PopupOpen` if the popup is open, else `Ready`.
  - **Red (`HotkeyUnavailable`) wins over blue (`PopupOpen`).** It is a persistent fault the user
    must act on; the open popup is already visible on screen. This is the only precedence rule in
    the programme. 006's `AppHost.UpdateTrayState()` (the single writer, called from
    `PopupWindow.VisibleChanged` and `IHotkeyManager.StatusChanged`) applies this rule from the
    start, so there is no deferred behaviour change in 008.
  - `AppHost` keeps the two inputs (`hotkeyManager.Status.IsRegistered`, the popup's
    `VisibleChanged` from 006) and assigns `tray.State = TrayStatePolicy.Compute(...)` in
    `UpdateTrayState()`. That removes the overwrite hazard in §3.
  - `TrayState` moves to Presentation next to the policy. `TrayIcon` keeps using it. If 006 has
    already extracted the rule into a helper, T7 moves it to `TrayStatePolicy` without changing its
    result; T0 records which.

**(g) Removing prototype surfaces.** Delete:
- `Diagnostics/` (both files);
- the `AppHost` field, method, callback and exit-close;
- the `TrayIcon` command, ctor parameter, menu item and dispatch;
- `Core/Data/ProbeRepository.cs`, whose only caller goes away. This follows the CLAUDE.md rule to
  remove code made obsolete by our own change.

The `probe` table and migration 1 stay. The tray menu becomes: Open Smart Snippets · Show popup
`\t<hotkey>` · Settings · separator · Restart as administrator (when not elevated) · separator ·
Exit. The empty-group separator logic must not leave two adjacent separators when elevated. If
`TestString` or any prototype insertion path survives 006/007, it is removed too.

**(h) Verification layout.** The single acceptance harness stays `scripts/verify-plans-003-009.ps1`.
It is extended, not forked, and keeps its name so the existing docs and links stay valid.
- **New parameter `-ArtifactZip <path>`:**
  1. verifies the zip against its `.sha256`;
  2. extracts it to `publish\`;
  3. implies `-SkipBuild`;
  4. records the exe's `FileVersion`/`ProductVersion` in the report.

  Acceptance runs against the **release candidate as downloaded**, not a local build.
- **New parameter `-PerfLibrary <n>`** (default `10000`): size of the generated performance
  dataset used by section P (§6 T11). `-Only P` runs the performance section alone.
- **New sections:** E (popup, 006), F (insertion, 007), G (release, icons and tray, 008) and
  P (performance at 10k, AC13). They are listed in §6 T10 and T11.

### 4.2 Alternatives considered

| Option | Pros | Cons | Verdict |
|---|---|---|---|
| R-A — separate `release.yml` on `v*` tags + shared `publish-aot.ps1` | Least privilege (write only on tags); gate defined once; verify script reuses it | One new script | **Chosen** |
| R-B — add a tag trigger and a conditional release job to `ci.yml` | One file | `paths-ignore` and tag semantics mix; `contents: write` on every main push; harder to read | Rejected |
| R-C — reusable `workflow_call` build workflow | No duplicated YAML | YAML indirection; the verify script still needs the gate in PowerShell anyway | Rejected — the script already removes the duplication |
| R-D — `softprops/action-gh-release` | Popular | Third-party action holding a write token; `gh` is preinstalled | Rejected |
| V-A — `-p:Version` from the tag in the workflow | Tag is the only source; no packages | Local builds need a default | **Chosen** (default `0.0.0-dev`) |
| V-B — MinVer / Nerdbank.GitVersioning | Automatic from git | Extra package, full-history clone, more AOT/build surface | Rejected |
| V-C — committed `version.txt` | Simple | Second source of truth that can disagree with the tag | Rejected |
| I-A — checked-in SVG sources + Python generator + checked-in `.ico` | Reviewable, reproducible, no build-time toolchain | Python/cairo needed only to *change* icons | **Chosen** |
| I-B — hand-authored `.ico` only | No tooling | Not reproducible; 7 files × 6–8 frames edited by hand; light/dark drift | Rejected |
| I-C — generate at build time (MSBuild → Python) | Never stale | Adds Python + cairo to every Windows build and CI | Rejected |
| I-D — runtime overlay (one base glyph, dot composited with GDI / `CreateIconIndirect`) | 2 files instead of 6 | More unsafe interop and DIB code on the UI thread for no user gain | Rejected |
| I-E — runtime SVG / XAML-rendered tray bitmap | Exact design | Excluded by the brief; AOT and start-up cost | Rejected |
| L-A — `LoadImage(LR_LOADFROMFILE)` from `Assets\` next to the exe | No `rc.exe`; keeps the SDK's VERSIONINFO + icon | Loose files can go missing → fallback + log | **Chosen** |
| L-B — custom `.rc`/`.res` Win32 resources | Single-file icons | Replaces the SDK-generated VERSIONINFO and icon; needs `rc.exe` and hand-made versioning | Rejected |
| L-C — managed embedded resource + `CreateIconFromResourceEx` | AOT-safe, no loose files | Own `.ico` parser and frame selection | Rejected (kept as the fallback design if loose files prove fragile) |
| Z-A — zip with a fixed top folder `SmartSnippets\`, extracted into a stable parent | Upgrade = overwrite in place; `Run` value path never changes | Old files dropped by a newer release stay unless the folder is deleted first (R10) | **Chosen** (user 2026-10-28) |
| Z-B — version-named top folder `SmartSnippets-<version>\` | Side-by-side versions | Every upgrade nests a new folder; start at login keeps launching the old exe (review finding 6) | Rejected |
| Z-C — no top folder (files at the zip root) | Extract straight into any folder | Explorer's default destination and a wrong target folder scatter 287 files | Rejected |
| P-A — end-to-end `popup_search_ms` in the real popup on a generated 10k dataset | Measures what the user sees (filter, group, row mapping, binding, render), both layouts | Needs a dataset generator and UIA-driven runs | **Chosen** (review finding 4) |
| P-B — Core `SearchBenchmarkTests` as the pass/fail evidence | Already exists, deterministic | Skips grouping, row mapping, binding and rendering | Kept as a diagnostic only |
| T-A — registry `SystemUsesLightTheme` + `WM_SETTINGCHANGE "ImmersiveColorSet"` | Exactly the taskbar's mode; no WinRT | Registry contract is undocumented but stable since 1903 | **Chosen** |
| T-B — `UISettings.ColorValuesChanged` | WinRT API | Reflects the app mode, not the taskbar's; fires on a background thread | Rejected |
| T-C — one glyph with an outline for both taskbars | 3 files | Contradicts the design ("adapts to light and dark taskbars") | Rejected |

### 4.3 Data / config model

- No schema or settings change. The `probe` table stays (migration 1 is immutable).
- New MSBuild default: `Version` = `0.0.0-dev` (`Directory.Build.props`), overridable with `-p:`.
- New files shipped: `Assets\SmartSnippets.ico` and six `Assets\Tray-*.ico` in the publish folder.
- New log events (no snippet text):
  - `app_started version=…`
  - `tray_icon state=… theme=…`
  - `tray_icon_load_failed name=…`
  - `tray_theme_changed theme=…`

  They are added to the A4.logevents list.
- Log events 008 consumes from 006 (owned and defined there):
  - `popup_ready_ms <int>` (popup shown and ready);
  - `popup_search_ms <int> layout=<flat|folder> results=<n>` (search text change → results
    rendered; one line per text change).
- Release asset names:
  - `SmartSnippets-win-x64-<version>.zip`
  - `SmartSnippets-win-x64-<version>.zip.sha256` (`<hex>  <filename>`, `sha256sum -c` compatible)

### 4.4 Sequence diagrams

**Tag → release**
```
Maintainer: git tag v1.0.0 <commit on main> && git push origin v1.0.0   (production action: explicit user OK)
  → GitHub: push event refs/tags/v1.0.0 → release.yml (ci.yml not triggered: branches filter)
    → checkout (fetch-depth 0)
    → Validate tag
        ├─ bad format ─────────────────────────────→ FAIL (no release)
        ├─ commit not ancestor of origin/main ─────→ FAIL "tag must point at main"
        ├─ release already exists ──────────────────→ FAIL "delete release to re-run"
        └─ ok → version=1.0.0, fileversion=1.0.0.<run>, prerelease=false
    → setup-dotnet → dotnet test Core, Presentation (Release) ── any failure → FAIL
    → scripts/publish-aot.ps1 -Version 1.0.0 -BuildNumber <run>
        → dotnet publish -p:Version=1.0.0 -p:FileVersion=1.0.0.<run>
        → gate: exit code, IL2xxx/IL3xxx, no App.dll, exe + e_sqlite3.dll ── any → FAIL
    → VersionInfo check on publish/SmartSnippets.App.exe ── mismatch → FAIL
    → Package: stage/SmartSnippets/ (no *.pdb) → zip (single fixed top folder, checked) → .sha256
    → gh release create v1.0.0 zip sha256 --verify-tag --generate-notes --notes-file header [--prerelease]
      → GitHub Release page with 2 assets + header + generated notes
Parallel paths checked: ci.yml (push main) keeps its own artifact via the same publish-aot.ps1;
verify script A3 calls the same gate. No other workflow publishes.
```

**Tray state → icon**
```
HotkeyManager.StatusChanged ─┐                     (UI-thread post, AppHost.cs:198 today)
Popup shown / hidden (006) ──┼→ AppHost.UpdateTrayState()
                             │     state = TrayStatePolicy.Compute(hotkey.IsRegistered, popup.IsOpen)
                             │     → TrayIcon.State = state
                             │         → (unchanged? return)
                             │         → icon = cache[(theme, state)] ?? LoadImage(file) ?? IDI_APPLICATION
                             │         → Shell_NotifyIcon(NIM_MODIFY, NIF_ICON|NIF_TIP)
                             │         → AppLog "tray_icon state=… theme=…"
WM_SETTINGCHANGE("ImmersiveColorSet") → MessageWindow.WndProc → TrayIcon.OnMessage
                             │     theme' = TaskbarTheme.IsLight(); if theme' != theme:
                             │         DestroyIcon(cache); theme = theme'; UpdateIcon(); log tray_theme_changed
TaskbarCreated ──────────────┘     theme = Read(); dpi = GetDpiForSystem(); reload cache; Add()
Parallel paths checked: TrayIcon.Add (initial icon, TrayIcon.cs:95-105) must use the same selection;
ShowBalloon (NIF_INFO only) unaffected; no other Shell_NotifyIcon caller (grep).
```

### 4.5 Surface list

| Surface | Change | Task |
|---|---|---|
| `.github/workflows/ci.yml` | Action majors bumped; gate → `publish-aot.ps1`; CI version stamp | T1, T2, T3 |
| `.github/workflows/release.yml` (new) | Tag-triggered release | T4 |
| `.github/release-notes-header.md` (new) | Install, SmartScreen, hash, upgrade notes | T4 |
| `scripts/publish-aot.ps1` (new) | Shared publish + gate | T2 |
| `Directory.Build.props` | `Version` default | T3 |
| `assets/icons/*.svg`, `scripts/icons/*` (new) | Icon sources + generator | T5 |
| `src/SmartSnippets.App/Assets/*.ico` (new) | Generated icons | T5 |
| `SmartSnippets.App.csproj` | `ApplicationIcon`, `Content` items | T6 |
| `MainWindow.xaml.cs` | `AppWindow.SetIcon` | T6 |
| `Services/TrayIcon.cs`, new `Services/TaskbarTheme.cs`, `NativeMethods.txt` | Glyph per theme/state, theme listener | T7 |
| `Presentation/.../TrayStatePolicy.cs` (+ `TrayState` moved) | State precedence | T7 |
| `AppHost.cs` | Tray-state wiring; Diagnostics removed; `app_started` log | T3, T7, T8 |
| `Diagnostics/*`, `Core/Data/ProbeRepository.cs` | Deleted | T8 |
| `Popup/PopupWindow.xaml.cs` | `TestString` residue removed if present | T9 |
| `scripts/verify-plans-003-009.ps1`, `scripts/lib/Uia.ps1` | `-ArtifactZip`, `-PerfLibrary`; sections E, F, G, P; C11 updated; A3 via shared gate; locked-DB check; perf dataset generator; unsaveable-clipboard helper | T10, T11 |
| `scripts/verify-plan002.ps1` | Header note: historical, superseded by sections A/E/F/G | T10 |
| `docs/research/003-release-acceptance-v1.md` (new) | Perf, matrix, audits, observations | T11–T16 |
| `docs/guides/001-install-and-release.md` (new) | Install/upgrade (fixed `SmartSnippets\` folder, §4.1(b) steps)/uninstall, SmartScreen, maintainer release steps | T4 |
| `docs/plan/001` master | §5/§9 status, Complete | T17 |

## 5. Impact and risk

| Question | Answer |
|---|---|
| State management — what survives a crash mid-operation? | Release job: nothing is published until the final `gh release create`. A failed run leaves at most a tag with no release, which can be re-run after deleting the tag or by pushing a new tag. App: tray icon handles are process-local; Explorer drops the icon on exit as today |
| Data consistency — orphans, races, double-apply? | `concurrency` serialises runs per tag; "release exists" fails the job instead of clobbering. Tray state has one writer (`AppHost.UpdateTrayState`) on the UI thread |
| User experience — root problem or symptom; any surprise? | Unsigned exe → SmartScreen "Windows protected your PC" on first run. Documented in the release header and the install guide (More info → Run anyway, or Unblock in Properties). Removing Diagnostics removes a duplicate start-at-login toggle; Settings keeps it |
| Backward compatibility — existing data / sessions / configs? Migration? | DB untouched; `probe` table kept. The `Run` value stores the exe path. Every zip has the fixed top folder `SmartSnippets\`, so extracting a new version into the same parent (`%LOCALAPPDATA%\Programs\`) overwrites the exe in place and start at login keeps working; proven by runbook R14 (release A → B). Installing to a different parent needs the toggle re-done (documented). Downgrade to an older build with a newer schema is refused (B checks) |
| Error recovery — what happens on failure; can it recover? | Missing tray icon file → `IDI_APPLICATION` + log; registry read failure → dark; `ApplicationIcon` missing in the AOT exe → caught by the G-section check before acceptance |
| Logging & debugging — enough to diagnose in the field? | `app_started version=` ties logs to a build; `tray_icon` / `tray_theme_changed` / `tray_icon_load_failed` explain glyph issues; the release run log keeps the full publish log |
| Edge cases — empty, huge, repeated, concurrent, interrupted? | Pre-release tags → `--prerelease`. Re-pushed or moved tags → "release exists" fail. Tag on a non-main commit → fail. Rapid theme toggles → each broadcast compares and reloads at most once. Elevated instance: `ChangeWindowMessageFilterEx` is already applied for `TaskbarCreated`; `WM_SETTINGCHANGE` is a broadcast from the same-or-lower integrity shell. **Must be checked** on the elevated instance (runbook R9); if blocked, add a message filter for it |

**Risks**
- **R1 — Unsigned binaries / SmartScreen / AV heuristics.** A new unsigned exe that registers a global
  hotkey, sends input and touches the clipboard can trip SmartScreen and occasionally AV heuristics.
  → Documented in the release header and the guide. SHA-256 published. Signing stays out of scope
  (the user's decision); revisit if a false positive is reported.
- **R2 — Artifact size** (~53 MB zip, 287 files; most of it is the self-contained WinAppSDK).
  → Within GitHub's 2 GB asset limit; `*.pdb` excluded. Trimming WinAppSDK satellite languages
  (`SatelliteResourceLanguages`) is a possible later size cut. It is not in this plan because it
  touches resource loading (Q4).
- **R3 — Tag misuse** (tag on an unmerged commit, typo tag, re-tagging a released version).
  → Format regex, ancestor-of-`main` check, release-exists check, and `--verify-tag`. Recommend a
  GitHub tag ruleset limiting `v*` creation to the owner (Q2, user action). Tag pushes are production
  actions: Claude pushes a tag only after the user's explicit confirmation of that tag and commit.
- **R4 — Icon DPI scaling.** Fixed frames 16–48 cover 100–300 % small-icon sizes. Non-integer scales
  (125 %, 175 %) pick 20/32/40 frames rendered from the vector, which can soften. → Hand-tuned 16/20
  sources, a contact-sheet review in the PR, and a user visual check at 100/150 % (runbook R6). After
  a scale change without sign-out, the shell may rescale the cached icon until Explorer restarts or
  the next state change. Accepted, and noted in the guide.
- **R5 — High-contrast themes.** The taskbar uses HC colors; a black or white glyph may vanish on
  some HC themes. → Runbook R7 checks HC Black and HC White. If one is illegible, fix forward by
  picking the variant from `SPI_GETHIGHCONTRAST` + `COLOR_WINDOW` luminance (small, same files).
- **R6 — AOT drops `ApplicationIcon` / VERSIONINFO.** ILC is expected to copy Win32 resources, but
  this is not yet proven in this repo. → The release job's VersionInfo step and G1/G2 checks fail
  loudly. Fallback L-B (`.res`) is designed above.
- **R7 — Verify script fragility.** Real input, focus rules and Office/Teams/Terminal start-up timing
  all vary. → Sections E and F drive only Notepad (inbox, deterministic); other targets are the
  manual matrix. Every new check uses `Wait-Until` with timeouts, never fixed sleeps. Flaky checks are
  fixed in the harness, never by weakening an assertion (CLAUDE.md). The script already backs up and
  restores the user's profile; the F section also saves and restores the user's clipboard text, and
  clears the unsaveable test payload (F9–F11) in `finally`.
- **R8 — Actions major bump changes behaviour** (e.g. `upload-artifact` v4→v5 artifact semantics).
  → Bump in its own commit, then read the next `main` run's annotations and artifact. Revert that
  commit if it misbehaves.
- **R9 — Generator toolchain drift** (cairosvg version changes anti-aliasing). → Pinned version;
  `--check` mode; outputs checked in, so a drift only shows when someone regenerates.
- **R10 — Upgrade leaves stale files or fails mid-extract.** Overwriting in place keeps files a
  newer release dropped, and extracting while the app runs fails on locked files. → The procedure
  starts with Exit and a process check; the guide offers delete-then-extract as an equally safe
  clean variant (same path, data elsewhere); runbook R14 proves the documented overwrite path from
  one real release to the next, including start at login.
- **R11 — Performance measurement noise.** One run on one PC varies with background load, and the
  first search after opening may include list materialisation. → Fixed, seeded dataset; 20 runs
  per (query, layout) cell; the median is the pass bar and p95/max are reported; the machine (CPU,
  RAM, power plan) and build are recorded. A cell that fails is re-run once after closing other
  apps; both runs are recorded. A failure is never resolved by changing the dataset or the bar
  (CLAUDE.md); it goes to the user with the numbers.
- **R12 — Residual focus race in insertion** (review finding 3). Focus can still move in the few
  milliseconds between 007's last target revalidation and `SendInput`. The user accepted this
  residual risk on 2026-10-28. It is not testable by automation; it is recorded in the acceptance
  notes (T16), and 007's late revalidation is evidenced by its unit tests.

**Rollback:**
- Release workflow: delete `release.yml`, or delete the release/tag. Main CI is unaffected.
- Icons: revert the T6/T7 commits. The tray falls back to `IDI_APPLICATION` on any load failure
  anyway.
- Diagnostics removal: `git revert` of T8.

## 6. Tasks

Ownership: **C** = coordinator (Claude, this branch → PR to `main`). **U** = user-run on the Windows
PC. Linux cannot compile WinUI, so every App change is compiled first by CI on `main` (fix-forward
PRs, §8). Claude reads CI results with `gh run view` and the job logs.

### T0 — Re-baseline against merged 006/007 (C)
- **Files:** this plan only (§12 revision row).
- **Change:** re-read the merged 006/007 code and plans, then update §6 T9–T11 and §7. The
  behaviours are fixed by this plan; T0 only fills in the names:
  - how the popup reports open/closed (`VisibleChanged`) and that `AppHost.UpdateTrayState()` uses
    the §4.1(f) precedence;
  - the shared insertion contract in `src/SmartSnippets.Presentation/Services/InsertionContracts.cs`
    (006/007 coordinator T0): the final `InsertionOutcome` names, including 007's new outcomes for
    the unsaveable-clipboard prompt, the target closed after "Paste anyway", and `MaybePasted`;
  - log events (`insert_result`, usage update, duplicate guard, elevation block, clipboard
    preservation, `popup_search_ms`) and the exact end point of `popup_search_ms`;
  - error and prompt wording, including the "Paste anyway?" prompt and its buttons;
  - accessible names of the popup search box, list, toolbar, add-snippet editor and the
    paste-anyway prompt;
  - which clipboard payload 007 treats as unsaveable (it picks the F9 helper's payload);
  - how CR line endings were resolved;
  - whether `TestString` is gone.
- **Verify:** the user sees the revision diff before T9/T10 start.

### T1 — Bump GitHub Actions majors (C) (housekeeping)
- **Files:** `.github/workflows/ci.yml`. `release.yml` is born with the same majors.
- **Change:** move `actions/checkout`, `actions/setup-dotnet` and `actions/upload-artifact` to the
  current majors that run on Node 24. Expected: `@v5` for all three; confirm on the actions' release
  pages at implementation time. No other change in this commit.
- **Verify (C):** the next `main` run is green, `gh api …/check-runs/<id>/annotations` shows no
  "Node.js 20 actions are deprecated" annotation, and the artifact `SmartSnippets-win-x64-aot` is
  still uploaded with the same file count.

### T2 — Shared publish gate (C) (AC14)
- **Files:** `scripts/publish-aot.ps1` (new); `.github/workflows/ci.yml` (`:35-55` → one call).
- **Change:** §4.1(a). Exit codes: 0 ok; 1 gate failure, with a one-line reason as a GitHub
  `::error::` annotation when `$env:GITHUB_ACTIONS` is set.
- **Verify (C):**
  - Green `main` run.
  - The publish log in the run still lists 0 `warning IL` lines.
  - A deliberately broken local invocation is not possible on Linux, so the negative path is
    exercised by the verify script G-section (U) running the script with
    `-Output` pointing at a folder containing a fake `SmartSnippets.App.dll` → expects failure.
- **Test that dies if this breaks:** CI `windows` job; verify A3.

### T3 — Version stamping (C) (AC14)
- **Files:** `Directory.Build.props`, `scripts/publish-aot.ps1`, `ci.yml`, `AppHost.cs`
  (`app_started` log at the start of `Start`).
- **Change:** §4.1(c).
- **Verify:**
  - (C) CI run: `VersionInfo` of the CI exe shows `0.0.0-ci.<run>` / `0.0.0.<run>`, checked by an
    added CI step that mirrors the release check.
  - (U) G1 in the verify script.
- **Test that dies if this breaks:** release-job "Check stamped version"; verify G1.

### T4 — Release workflow and release docs (C writes; U triggers) (AC14)
- **Files:** `.github/workflows/release.yml`, `.github/release-notes-header.md`,
  `docs/guides/001-install-and-release.md`.
- **Change:** §4.1(b).
  - The guide covers: download; verify the hash (`Get-FileHash -Algorithm SHA256`); install by
    extracting into `%LOCALAPPDATA%\Programs\` so the app lands in
    `%LOCALAPPDATA%\Programs\SmartSnippets\` (the zip's fixed top folder); SmartScreen first-run
    steps; **upgrade** with the exact §4.1(b) steps (Exit from the tray and confirm the process is
    gone → verify the hash → extract into the same parent with overwrite → start), including the
    Explorer destination pitfall and the optional delete-first clean variant; why start at login
    keeps working (same exe path); uninstall (turn off Start at login, Exit, delete
    `%LOCALAPPDATA%\Programs\SmartSnippets`, optionally delete `%LOCALAPPDATA%\SmartSnippets`);
    where logs are.
  - `.github/release-notes-header.md` carries the short form of the same install and upgrade steps
    and links the guide.
  - The maintainer section covers the tag procedure, the rc-first rule and the two-release upgrade
    test (§8).
- **Verify (U + C):**
  1. After T1–T9 are on `main` and CI is green, the **user** approves and pushes
     `v1.0.0-rc.1` (or authorises Claude to push that exact tag).
  2. (C) checks the run: all steps green.
  3. The release is marked pre-release, with 2 assets.
  4. The `.sha256` matches the zip after download (`gh release download` + `sha256sum -c`).
  5. The zip has exactly one top folder named `SmartSnippets` (no version in it), has no `.pdb`,
     and contains `SmartSnippets\SmartSnippets.App.exe` and `SmartSnippets\Assets\*.ico`.
  6. The header and the generated notes both appear.
  7. (U) The upgrade test, runbook R14, passes from the previous release to this one.
- **Negative checks (C, cheap):** the regex and the ancestry logic are a PowerShell function in the
  workflow step; the same logic is unit-checked by the G-section (U) calling it with `v1.0`,
  `v1.0.0.0`, `1.0.0` and `v1.0.0-rc.1`.

### T5 — Icon sources and generator (C) (design)
- **Files:** `assets/icons/{app,app-16,tray,tray-16,tray-20}.svg`,
  `scripts/icons/generate_icons.py`, `scripts/icons/requirements.txt`,
  `src/SmartSnippets.App/Assets/{SmartSnippets,Tray-*}.ico`, `assets/icons/preview.png`.
- **Change:** §4.1(d).
- **Verify (C, Linux):**
  - The generator runs; `--check` passes on a second run (byte-identical).
  - A small parser in the script's self-test prints each `.ico`'s frame list. Expected: app = 8
    frames incl. 256 PNG; tray = 6 frames each.
  - Claude inspects `preview.png`, and the user reviews it in the PR (R4).
- **Test that dies if this breaks:** generator `--check`; G2/G3 frame checks.

### T6 — App icon wiring (C; U visual) (design)
- **Files:** `SmartSnippets.App.csproj` (`ApplicationIcon`, `Content` items), `MainWindow.xaml.cs`
  (`SetIcon` after `InitializeComponent`, guarded by `File.Exists`, logs `window_icon_missing`).
- **Verify:**
  - (C) CI publish contains `Assets\*.ico` and the gate still shows 0 IL warnings.
  - (U) G2: `[System.Drawing.Icon]::ExtractAssociatedIcon(exe)` differs from the default
    application icon, compared by hash against `Assets\SmartSnippets.ico` rendered at 32.
  - (U) Visual: Explorer, taskbar button, Alt+Tab and the pinned taskbar show the mark (runbook R5).

### T7 — Tray glyph per theme and state (C; U visual) (design, AC12 tray)
- **Files:** `Services/TrayIcon.cs`, `Services/TaskbarTheme.cs`, `Services/TrayIconNative.cs`
  (`LibraryImport` for `LoadImageW`, `DestroyIcon`, `GetSystemMetricsForDpi`, `GetDpiForSystem`;
  numeric Win32 constants for theme/icon interop), `Presentation/Services/TrayStatePolicy.cs` (+ `TrayState`),
  `AppHost.cs` (006's `UpdateTrayState()` stays the single writer and now calls
  `TrayStatePolicy.Compute`; the result is unchanged because 006 already applies red-over-blue),
  `tests/SmartSnippets.Presentation.Tests/TrayStatePolicyTests.cs`.
- **Change:** §4.1(e)(f).
- **Verify:**
  - (C) Unit tests:
    - `TrayStatePolicyTests.Compute_PrioritizesHotkeyFault` (four theory cases: unregistered
      open/closed → unavailable, registered/open → popup, registered/closed → ready).
  - (C) CI compile with 0 IL warnings.
  - (U) G3–G6 (§7) and runbook R6–R9.
- **Test that dies if this breaks:** `TrayStatePolicyTests`; G4 `tray_icon state=PopupOpen` after
  the hotkey and `state=Ready` after Esc; G5 `state=HotkeyUnavailable` when started with the hotkey
  held by another process.

### T8 — Remove the Diagnostics surfaces (C) (scope)
- **Files:**
  - delete `Diagnostics/DiagnosticsWindow.xaml(.cs)` and `Core/Data/ProbeRepository.cs`;
  - edit `AppHost.cs` (`:5`, `:44`, `:192`, `:265-282`, `:328`) and `TrayIcon.cs` (`:26`, `:37`,
    `:51`, `:61`, `:220`, `:232`, `:267-268`; separator logic);
  - edit `scripts/verify-plans-003-009.ps1` C11 (`:476-477`): expect 5 items (non-elevated) with no
    Diagnostics.
- **Verify:**
  - (C) `grep -rn "Diagnostics\|ProbeRepository" src` → only `System.Diagnostics` usings.
  - Core tests still green (MigratorTests still assert the `probe` table).
  - CI green.
  - (U) C11 passes with the new expectation.
- **Test that dies if this breaks:** C11.menu, C11.order.

### Merged 006/007 re-baseline (T0, b85e8fd)

The following names replace any illustrative names in T9–T11 and §7:

- `VisibleChanged` supplies `AppHost.UpdateTrayState`; `TrayStatePolicy.Compute` prioritizes unregistered over open. `TestString` is absent. The list ID is `PopupList` (no explicit name); search ID `PopupSearch`, name `Search snippets`; toolbar IDs/names are `PopupLayoutButton` / `Toggle layout`, `PopupSortButton` / `Sort`, `PopupAddButton` / `Add snippet`, `PopupOpenMainButton` / `Open main window`. Layout tooltip is `Layout: flat` or `Layout: folder`; sort tooltips begin `Sort: ` with `last used`, `usage count`, `created date`, `updated date`. Editor overlay ID `PopupEditor`, text field name `Snippet text`, buttons `PopupEditorCancel` / `Cancel`, `PopupEditorSave` / `Save`.
- The clipboard confirmation is an inline footer, not a dialog: `PopupFooterError` says `Some of what's on your clipboard can't be restored after pasting. Paste anyway?`; buttons `PopupPasteAnyway` / `Paste anyway` and `PopupPasteCancel` / `Cancel`. Cancel emits `popup_paste_cancelled`, not a second `insert_result=Cancelled`; the preceding insertion emitted `insert_result outcome=ClipboardNotPreserved`. A 64 MiB-plus registered HGLOBAL format is unsaveable. `CopiedForManualPaste` is the closed-target-after-confirmation outcome; `MaybePasted` is uncertain and is not a retryable failure. Target-closed wording is `Couldn't insert. <target> was closed. Nothing was pasted.` Elevation wording: `The target app is running as administrator. Smart Snippets can't insert text into it.`
- `popup_ready_ms` measures hotkey timestamp to ready. `popup_search_ms <ms> layout=<flat|folder> results=<n>` is emitted on `CompositionTarget.Rendered` after list layout (or the next render when no layout replacement occurs), not merely after filtering. Other events: `popup_opened`, `popup_closed`, `popup_layout`, `popup_sort`, `popup_add_opened`, `popup_add_saved`, `popup_add_cancelled`, `popup_insert`, `popup_open_ignored reason=inserting`, `insert_start`, `target_check`, `target_rejected`, `foreground_gate`, `clipboard_swap`, `clipboard_restore`, `insert_result`, `manual_paste_fallback`, `usage_record_failed`. There is no successful usage-specific log; inspect SQLite. Duplicate activation uses popup insertion gating and `Busy`; review remediation adds `popup_insert_ignored reason=inserting source=keyboard|row` at the existing view-model gates. F5 requires that receipt, while independent F5.hotkey consumes `popup_open_ignored reason=inserting`; Busy still returns without an insert_result event. A target elevation block is `target_check result=Elevated`.
- P0 auto-label generation must follow `LabelSuggester.Suggest`: first tag if present, otherwise first nonblank text line, truncated to 60 text elements. The earlier P0 wording saying all auto labels come from the first line conflicts with the merged implementation; the fixture must use the actual rule, or make auto-labelled snippets tagless.
- Lone CR/LF/CRLF are normalized to CRLF during insertion, without modifying stored text. F1 must compare pasted text, not assume D1 changes persisted storage. Live propagation evidence names are `PopupViewModelListTests.WhileShown_SelectedSnippetEdited_RowUpdates_SelectionKept`, `WhileShown_SnippetAdded_AppearsInSortOrder_SelectionKept`, `WhileShown_FolderRenamed_HeadingAndChipsUpdate_SelectionKept`, and `WhileShown_FolderDeleted_RowsShowUnfiled`. F12 unit evidence includes `InsertionServiceTests.FailedDispatch_OnlyUncertainReleasesAndWaits`, `SendThrows_IsUncertain`, and `PopupViewModelInsertionTests.PasteAnyway_ThenMaybePasted_ShowsCaution`. E11/E12 and manual matrix remain required; these tests do not replace them.

### T9 — Remove popup prototype residue (C) (scope)
- **Files:** `Popup/PopupWindow.xaml.cs` only if `TestString` or a prototype insert path survived 006/007.
- **Verify (C):** `grep -rn "TestString\|Héllo – naïve" src` → no hits. F-section insertion uses
  real snippets.

### T10 — Extend the verify script (C writes; U runs) (AC4–AC10, AC13, AC14, PRD §5)
- **Files:** `scripts/verify-plans-003-009.ps1`, `scripts/lib/Uia.ps1` (helpers for the popup
  window, clipboard save/restore, reading Notepad text via UIA `ValuePattern`/`TextPattern`),
  `scripts/verify-plan002.ps1` (header note only).
- **Change:** §4.1(h). New checks follow. The IDs are final, but the wording of the expectations is
  re-checked at T0.
  - **Shared changes:**
    - `-ArtifactZip` (hash check, extract, version recorded).
    - A3 calls `publish-aot.ps1`.
    - A4.logevents gets the new 008 events and 006/007's events.
    - C11 is updated (T8).
    - **D-lock**: closes the 009 #7 SKIP. A background `sqlite3` holds `BEGIN EXCLUSIVE`, then a
      save attempt → inline error, text kept → release the lock → Try again saves.
  - **E — Popup (006)**, against Notepad as the target:
    - E1 Hotkey → popup visible, centred on the target's monitor work area (window rect vs
      `GetMonitorInfo`), search box focused.
    - E2 Typing filters the list (AND terms); empty state for no match.
    - E3 Up/Down moves the selection; headings are skipped in folder layout.
    - E4 Esc closes, no insertion, `usage_count` unchanged (sqlite).
    - E5 Layout toggle and each of the 4 sorts persist across popup reopen and app restart
      (settings table).
    - E6 Toolbar buttons have accessible names and tooltips.
    - E7 Add snippet from the popup: save → list refreshed, new snippet selected, search cleared if
      it would hide it, Enter inserts it into Notepad (PRD §5 "inserted immediately"). Cancel
      restores the prior query and selection.
    - E8 "Open main window" opens the main window.
    - E9 Tab/Shift+Tab cycle; mouse click on a row = Enter.
    - E10 `popup_ready_ms` sampled 20 times on the standard test profile (first open after a
      `--background` start recorded separately) → every sample ≤300 ms; median/p95/max in the
      report. The 10k measurements are in section P (T11).
    - **Store-change propagation (master §7 #1, #2).** The popup closes on deactivation by design
      (006 §2), so it cannot stay open while the user works in the main window. Propagation is
      proven in two parts (review finding 8):
      - *Live update while shown:* 006's view-model unit tests raise `store.Changed` while the
        popup view-model is shown (`WhileShown_SelectedSnippetEdited_RowUpdates_SelectionKept`,
        `WhileShown_FolderRenamed_HeadingAndChipsUpdate_SelectionKept`). They run in A1.
      - *End to end through the real app:* E11 and E12 below.
    - E11 Master §7 #1: with the popup hidden, create a snippet and edit another snippet's label in
      the main window (UIA) → press the hotkey → the popup list (UIA) contains the new row and shows
      the edited label, and the new snippet is found by a search for its unique term.
    - E12 Master §7 #2: with the popup hidden, rename a folder and delete another folder in the main
      window → reopen the popup in folder layout → the heading shows the new name, the old name is
      absent, and the deleted folder's snippets appear under Unfiled.
  - **F — Insertion (007)**, Notepad target. Before the section it sets a sentinel clipboard text
    and restores the user's clipboard after it.
    - F1 The Unicode + multiline probe snippet is inserted. Notepad's text equals the snippet with
      CRLF line breaks: compared as code points, and the CR-only observation is closed.
    - F2 Selected-text replacement: select a word in Notepad → insert → the word is replaced.
    - F3 The clipboard equals the sentinel after the restore delay.
    - F4 `usage_count` +1 exactly once and `last_used_at` updated (sqlite). Master §7 #3: the main
      window's "Used" stat and the popup's Most-used sort reflect it.
    - F5 Double activation (Enter twice fast / Enter + click) → one insertion, guard logged.
    - F6 Target closed while the popup is open → actionable message, no insertion, usage unchanged.
    - F7 Never inserts into a Smart Snippets window: hotkey while the main window is foreground →
      ignored or the 007 message.
    - F8 Elevated target: SKIP in automation (UAC). It stays in the manual matrix.
    - **Clipboard that cannot be fully preserved (007, review finding 1).** The helper
      `Set-UnsaveableClipboard` in `scripts/lib/Uia.ps1` puts a payload on the clipboard that 007
      classifies as unsaveable (the payload is fixed at T0 from 007's rule, e.g. a private format
      above the snapshot size cap), plus a text sentinel. `Test-ClipboardIntact` compares the format
      list and a hash of each format against the values recorded right after setting it.
    - F9 Unsaveable clipboard → hotkey → Enter on the script's plain probe snippet (`smart-snippets-probe`) → the popup shows the "Paste anyway?"
      prompt (accessible names per T0); nothing has been pasted yet. **Cancel** → Notepad text
      unchanged, `Test-ClipboardIntact` true, `usage_count` unchanged, `insert_result` shows the
      `ClipboardNotPreserved` outcome followed by `popup_paste_cancelled`.
    - F10 Same setup → **Paste anyway** → the probe snippet is pasted once into Notepad, `usage_count` +1, and
      the clipboard ends in the state 007 specifies for this path (the unsaveable payload is not
      restored, as the prompt warned; the preserved formats are restored).
    - F11 Same setup → prompt shown → the script closes Notepad → **Paste anyway** → the popup shows
      007's `CopiedForManualPaste` message (`Couldn't paste into <target>. The snippet is on your clipboard. Paste it yourself with Ctrl+V.`), nothing is pasted anywhere, the snippet text is left on the
      clipboard (`Get-Clipboard` equals the probe text) so the user can paste it manually, `usage_count`
      unchanged.
    - F12 `MaybePasted` (dispatch outcome unknown: partial `SendInput` or an exception after
      dispatch) cannot be forced from outside the process. No script check; the evidence is 007's
      unit tests for those paths (usage rule, message, no automatic retry), cited in §7.2. F12 is
      recorded as **N/A — unit-test evidence**, not SKIP.
  - **G — Release, icons, tray (008):**
    - G1 Exe `FileVersion`/`ProductVersion` match the expected version (the zip name when
      `-ArtifactZip` is used).
    - G2 The exe's associated icon is the mark, not the default; `Assets\SmartSnippets.ico` has 8
      frames.
    - G3 The six tray `.ico` files exist, with 6 frames each.
    - G4 Tray state log: `Ready` at start → `PopupOpen` on hotkey → `Ready` on Esc.
    - G5 Start with the hotkey held by a helper process → `HotkeyUnavailable`. Release it and
      re-apply in Settings → `Ready`.
    - G6 A synthetic `WM_SETTINGCHANGE "ImmersiveColorSet"` sent to `SmartSnippets.MessageWindow`
      → the handler runs (`tray_icon` re-evaluated, no crash).
    - G6b Opt-in `-FlipTaskbarTheme`: toggles `SystemUsesLightTheme`, broadcasts, expects
      `tray_theme_changed`, restores the value in `finally`. Opt-in because it changes the user's
      real taskbar for a few seconds.
    - G7 The tray menu has no Diagnostics item, and Restart as administrator is present when not
      elevated.
    - G8 `publish-aot.ps1` negative path (T2) and the tag-validation function cases (T4).
    - G9 The zip contains no `.pdb`, and the `.sha256` matches.
    - G10 Log privacy re-run over the whole log after E/F (no probe or snippet text in `app.log`).
- **Verify (U):** a full run against the rc zip:
  `powershell -ExecutionPolicy Bypass -File scripts/verify-plans-003-009.ps1 -ArtifactZip <path>
  -ReportPath docs\research\003-run.md` (sections A–G and P). Pass bar: 0 FAIL, 0 WARN. Remaining
  SKIPs = only the §7 manual list; F12 is N/A with unit-test evidence. The report is attached to
  `docs/research/003` (T16).

### T11 — Performance acceptance (U runs; C records) (AC13, PRD §5)
Section P of the verify script, run on the rc build (`-ArtifactZip`), in its own temporary
profile. It measures the user-visible path (review finding 4, user 2026-10-28).

- **P0 — Dataset (generated, deterministic).** `New-PerfLibrary -Count $PerfLibrary -Seed 42`
  starts the app once on an empty temporary profile so the schema exists, exits it, then inserts
  through `sqlite3` in one transaction:
  - **Folders:** 50 (`Perf folder 01` … `Perf folder 50`).
  - **Snippets:** 10,000. 9,000 spread evenly over the 50 folders (180 each), 1,000 Unfiled.
  - **Text** from a fixed 2,000-word English-like vocabulary: 60 % short (20–200 characters,
    one line), 30 % medium (200–1,000 characters, 2–8 lines), 10 % long (1,000–4,000 characters,
    10–40 lines). About 5 % contain non-ASCII characters (accents, CJK, emoji).
  - **Labels:** 70 % custom (`label_is_custom=1`, 2–5 vocabulary words), 30 % auto (the first line
    of the text, truncated as the app does).
  - **Tags:** 0–3 per snippet from a 100-tag vocabulary, in the store's normalised format.
  - **Usage:** `usage_count` 0–500 with a long tail (most 0–5), `last_used_at` within the last
    365 days or NULL for unused, `created_at`/`updated_at` spread over two years, so all four
    sorts have real work.
  - **Planted search terms** (no vocabulary word, label, tag or folder name contains them, so
    substring matching cannot add hits): `alpha` in exactly 1,000
    snippets (spread over text, labels and tags), `bravo` in 1,000, both in exactly 100. Counts are
    asserted by SQL before measuring.
- **P1 — Popup ready at 10k (pass/fail).** First open after a `--background` start, then 20 opens
  (10 in flat layout, 10 in folder layout), each from Notepad with Esc between. Pass: **every**
  `popup_ready_ms` sample ≤300 ms. Median/p95/max reported.
  - **Why pass/fail at 10k, not informational:** AC13 (master `001:83`) sets search at 10k and the
    popup at ≤300 ms without naming a library size. The popup's ready time grows with the library
    (it materialises the list), 006 designs its pipeline and prewarm for 10k (006 R4), and judging
    popup and search on the same dataset makes AC13 one consistent condition. A small-library-only
    pass bar would leave the popup target unproven at the only scale the programme agreed. E10
    keeps the small-profile check as well.
- **P2 — Search end to end (pass/fail).** In the open popup the script sets the search box text
  in one step with UIA `ValuePattern.SetValue`, so each change produces exactly one
  `popup_search_ms <int> layout=<flat|folder> results=<n>` line, and waits for that line.
  Queries:

  | Query | Change | Expected `results` |
  |---|---|---|
  | Q1 one common term | empty → `alpha` | 1,000 |
  | Q2 two-term AND | empty → `alpha bravo` | 100 |
  | Q3 no match | empty → `zzqxv` | 0 (empty state shown) |
  | Q4 clearing the query | `alpha bravo` → empty | 10,000 |

  Each query runs **20 times in each layout** (4 × 2 × 20 = 160 samples), in a fixed interleaved
  order; the precondition text for Q4 is set first and its own line is not counted. `results=<n>`
  must equal the expected count (else FAIL: the measurement did not measure the dataset).
  Pass: **median ≤150 ms in every (query, layout) cell**. p95 and max are reported for every cell;
  they are not pass conditions, but any p95 above 300 ms is raised to the user.
- **P3 — Core benchmark (diagnostic only).**
  `dotnet test tests/SmartSnippets.Core.Tests -c Release --filter Category=Benchmark --logger "console;verbosity=detailed"`
  runs when the SDK is present; its lines (`SearchBenchmarkTests.cs:56-62`) go into the report as
  INFO. It never decides AC13. When P2 fails, P3 helps tell Core search cost apart from popup
  grouping, binding and rendering.
- The report records CPU model, RAM, power plan, display scale, build version and the dataset
  counts.
- **Verify:** P0–P2 PASS in the rc run; numbers recorded in `docs/research/003` §Performance. A
  failing cell follows R11 (one re-run, both recorded, then the user decides on a fix-forward).

### T12 — Compatibility matrix and remaining manual items (U runs; C prepares and records) (AC9, AC10, AC14, PRD §5)
- **Preparation (C):** a matrix template in `docs/research/003`, plus three probe snippets the user
  creates in the app (this also exercises CRUD):
  - *P-Unicode*: `Héllo – naïve 👋 世界 Ωμέγα`
  - *P-Multi*: three lines with a leading tab and trailing spaces
  - *P-Plain*: `smart-snippets-probe`
- **Procedure per target (U):**
  1. copy a sentinel (`SENTINEL-<app>`);
  2. insert P-Unicode, then P-Multi into an empty field;
  3. select a word and insert P-Plain;
  4. run `Get-Clipboard` in a separate PowerShell → expect the sentinel;
  5. check that the main window's "Used" counter went up by exactly the number of insertions;
  6. **unsaveable clipboard** (007, findings 1–3): dot-source `scripts/lib/Uia.ps1` in a separate
     PowerShell and run `Set-UnsaveableClipboard`; insert P-Plain → the popup shows "Paste anyway?"
     → **Cancel** → `Test-ClipboardIntact` prints `True` and nothing was pasted; repeat → **Paste
     anyway** → P-Plain is pasted once and "Used" goes up by one. Run `Clear-UnsaveableClipboard`
     afterwards.

  The "Paste anyway after closing the target" path and `MaybePasted` are not repeated per target:
  the first is F11 (Notepad), the second has unit-test evidence only (F12).
- **Rows:**
  - Notepad
  - VS Code (editor)
  - Word
  - Outlook (new mail body)
  - Chrome text input + textarea
  - Edge text input + textarea
  - Windows Terminal PowerShell
  - Windows Terminal cmd
  - Teams (chat compose)
  - **Elevated Notepad** with the app not elevated → expected: the 007 elevation message, no
    insertion, usage unchanged; then Restart as administrator → insertion works (AC10).
- **Columns:** Unicode · multiline · selected-text replacement · clipboard restored · usage counted
  once · unsaveable clipboard (Cancel keeps it / Paste anyway pastes) · notes.
  - Terminals: multiline paste may trigger the terminal's multi-line paste warning, and selection
    replacement does not apply (N/A by nature).
  - Expected differences are recorded as notes, not failures, and the user accepts them explicitly.
- **Remaining manual items** (previous SKIPs plus visual), each one a runbook row in §7:
  - Restart as administrator (UAC accept/cancel);
  - real Explorer restart (icon comes back with the right glyph);
  - start at login after sign-out (starts in background, tray glyph correct);
  - visual review light/dark/text size 32/narrow window/Narrator;
  - icons at 100 % and 150 %;
  - high contrast.
- **Verify:** every matrix cell filled; every failure either fixed (fix-forward, then the cell is
  re-run on the new rc) or accepted by the user in writing in the doc.

### T13 — Master §7 cross-subplan integration checks (U via script; C records) (AC3, AC4, AC12)
- **Mapping:**

  | §7 check | Evidence |
  |---|---|
  | #1 | E11 (main-window change while the popup is hidden → reopen shows it) + 006 view-model test `WhileShown_SelectedSnippetEdited_RowUpdates_SelectionKept` (live update while shown) |
  | #2 | E12 (folder rename/delete while hidden → reopen shows it) + 006 `WhileShown_FolderRenamed_HeadingAndChipsUpdate_SelectionKept` + D folder-rename propagation |
  | #3 | F4 |
  | #4 | C9 plus a restart check: C9.restart, added — after the change and an app restart, the new hotkey still opens the popup |
  | #5 | C12.free |
  | #6 | B (fresh profile, v1 → v2 upgrade) |

- **Verify:** each row PASS in the T10 report (and, for #1/#2, the named unit tests green in A1),
  cited in `docs/research/003`.

### T14 — Spec-vs-implementation audit (C; independent verifier) (all ACs, PRD §5)
- **Change:** fill §7.2's checklist with evidence links (script IDs, test names, matrix cells,
  runbook rows). Per CLAUDE.md, a verifier subagent that did not write the evidence re-checks each
  row, grouped by area: shell, main window, popup, insertion, release. It returns a verdict per row
  and a `topicsWithNoFindings` list.
- **Verify:** no row without evidence. Each "partial" or "fail" row either has a fix-forward PR or a
  user acceptance note.

### T15 — Wiring audit (C) (L)
- **Trace each new or changed entity from emission to consumption:**
  - `publish-aot.ps1` → called by `ci.yml`, `release.yml` and A3.
  - Tag → `Version`/`FileVersion` → exe → G1 / release check.
  - `assets/icons` → generator → `Assets/*.ico` → csproj `Content` → publish folder →
    `TrayIcon`/`MainWindow` loaders → `tray_icon` log.
  - `TaskbarTheme.IsLight` ← `RefreshThemeAndDpi` at construction and `TaskbarCreated`, and
    directly from `WM_SETTINGCHANGE`; `Add` uses the refreshed theme/DPI.
  - `TrayStatePolicy` ← both `AppHost` handlers. 006's popup events have a subscriber, and the
    subscriber unsubscribes on exit.
  - New log events present in A4.
  - Removed Diagnostics: no dangling callbacks or ctor parameters.
  - 006/007 handoffs: popup → `IInsertionService` (declared once, in
    `src/SmartSnippets.Presentation/Services/InsertionContracts.cs`) → usage update → store
    `Changed` → popup and main list refresh. One refresh path (master §4). Every
    `InsertionOutcome`, including the paste-anyway and `MaybePasted` outcomes, has a popup handling
    branch and a `UiText` message.
  - `UpdateTrayState()` is the only `tray.State =` assignment and calls `TrayStatePolicy.Compute`.
  - `popup_search_ms` and `popup_ready_ms` are emitted by the popup and parsed by sections E and P.
- **Verify:** every entry has a caller; each new log event appears in the T10 run's `app.log`.

### T16 — Open observations and acceptance record (C) (PRD §5)
- **Files:** `docs/research/003-release-acceptance-v1.md` (new).
- **Contents:**
  - build under test (tag, commit, run, zip hash);
  - the verify-script report;
  - performance;
  - the matrix;
  - manual runbook results;
  - the §7.2 audit;
  - wiring audit;
  - the observation dispositions in the table below.

  | Observation (`docs/research/002:53-62`) | Disposition |
  |---|---|
  | Lone-CR line breaks (WARN D1.eol) | Resolved by 007. Evidence: F1 CRLF comparison + matrix multiline column; D1.eol becomes PASS or is reworded by T0 to assert 007's chosen normalisation |
  | Conflict sentence unreachable by recording a `RegisterHotKey`-owned chord | **Accepted as-is** by the user (2026-10-28). The startup-conflict path (C13) and G5 cover the user-visible feedback |
  | "Snippet text cannot be empty." vs plan wording | Accepted. The code wording is canonical (`UiText`); plan 005 runbook text is historical |

  **Insertion acceptance notes** (review findings 1–3, recorded in `docs/research/003`):

  | Item | Disposition |
  |---|---|
  | Clipboard that cannot be fully preserved (finding 1) | Behaviour per 007: prompt before anything is emptied. Evidence: F9–F11, matrix column "unsaveable clipboard", 007 unit tests |
  | Uncertain dispatch, `MaybePasted` (finding 2) | Not forceable from outside the process. Evidence: 007 unit tests only (partial `SendInput`, exception after dispatch), cited by name; F12 = N/A |
  | Residual few-ms focus race between the last target check and `SendInput` (finding 3) | **Accepted** by the user (2026-10-28) as residual risk (R12). Evidence of the late revalidation: 007 unit tests; no automated reproduction |
- **Verify:** the user reads the doc. The acceptance sign-off is recorded in §12 and in the master §9.

### T17 — Close the programme (C)
- **Files:** `docs/plan/001-master-smart-snippets-v1.md` (§5 status for 004/005/009 runbooks → done
  via 008, 008 → Complete; §9 log row; Status → Complete); this plan's Status.
- **Precondition:** the final `v1.0.0` release is published (§8 step 6).
- **Verify:** the master's progress table has no "pending" entries.

## 7. Test strategy

### 7.1 Layers
- **Unit (Linux + Windows CI):**
  - `TrayStatePolicyTests` (new);
  - cited from 006/007 (not written here): the popup view-model store-change tests (E11/E12 live
    part) and 007's clipboard-preservation and uncertain-dispatch tests (F12);
  - existing Core (incl. `MigratorTests` with the `probe` table) and Presentation suites, unchanged
    and green;
  - generator `--check` (Linux, run by C when icons change).

  Deliberately not unit-tested: Win32 icon loading and registry reads (thin interop, covered by G
  checks on the real exe).
- **CI (Windows):** the AOT gate through `publish-aot.ps1`, with the version stamp check. The release
  workflow is exercised by the rc tag (§8).
- **Automated acceptance on the user's PC:** `verify-plans-003-009.ps1 -ArtifactZip <rc zip>`,
  sections A–G and P. Pass bar: 0 FAIL, 0 WARN; SKIPs limited to the runbook rows below.
- **Manual runbook (U, on the rc build).** Unchecked rows block the final tag.

| # | Step | Expected |
|---|---|---|
| R1 | Download the rc zip from the Release page; `Get-FileHash` | Matches the `.sha256` asset |
| R2 | Install per the guide: Explorer *Extract All…* with destination `%LOCALAPPDATA%\Programs`, run `%LOCALAPPDATA%\Programs\SmartSnippets\SmartSnippets.App.exe` | App lands in `…\Programs\SmartSnippets\` (no extra nesting); SmartScreen warning as documented; "Run anyway" starts the app. Properties → Details shows the version |
| R3 | Restart as administrator from tray and from Settings; accept, then repeat and cancel UAC | Accept: elevated instance, tray icon present, hotkey works. Cancel: notice, app keeps running |
| R4 | Restart Explorer (Task Manager → Restart) | Tray icon returns with the correct glyph for the current theme/state |
| R5 | Look at the exe in Explorer, the taskbar button, Alt+Tab, pin to taskbar | Bracket+caret app icon everywhere, crisp at 100 % |
| R6 | Display scale 100 % and 150 % (sign out/in between) | Tray glyph crisp, dot legible, no clipping |
| R7 | Settings → Personalization → Colors: "Choose your mode" Light / Dark; Contrast themes Aquatic/Desert (or HC Black/White on Win10) | Glyph switches within ~1 s on mode change, readable in each; HC legibility recorded |
| R8 | Open popup (blue dot), Esc (no dot); start with Ctrl+Shift+Space taken by another app (red dot), free it and re-apply in Settings (no dot) | Matches the design's three states |
| R9 | Same as R7 while running elevated (after R3) | Theme switch still updates the glyph (`WM_SETTINGCHANGE` reaches the elevated window) |
| R10 | Enable Start at login, sign out and in | App starts in the background, no window, tray glyph correct, hotkey works |
| R11 | Visual: Light/Dark, text size Custom 32, window < 720 px wide, Narrator on popup and main window | No clipped essential controls; Narrator announces names (PRD §5 theme/text size) |
| R12 | Compatibility matrix (T12) | All cells filled, failures fixed or accepted |
| R13 | Exit from tray; uninstall per the guide | Process gone, hotkey free, `Run` value removed when the toggle is off, folder deletable |
| R14 | **Upgrade test** (release A = previous rc, B = rc under test; §8 step 3). Run R13's uninstall first if needed. (1) Install A per the guide; turn on Start at login in Settings; add one snippet `upgrade-probe`; record `Get-ItemPropertyValue HKCU:\Software\Microsoft\Windows\CurrentVersion\Run -Name SmartSnippets`. (2) Upgrade to B with the guide's exact steps: tray → Exit, confirm no `SmartSnippets.App` process, verify B's hash, `Expand-Archive <B zip> -DestinationPath "$env:LOCALAPPDATA\Programs" -Force`, start the exe. (3) Check. (4) Sign out and in | (3) `(Get-Item "$env:LOCALAPPDATA\Programs\SmartSnippets\SmartSnippets.App.exe").VersionInfo.ProductVersion` starts with B's version; no `SmartSnippets-*` or nested `SmartSnippets\SmartSnippets` folder exists; `upgrade-probe` and all other snippets, folders and settings are present; the `Run` value is byte-identical to the one recorded for A; Settings shows Start at login on. (4) B starts in the background, the tray glyph is correct, the hotkey opens the popup, and `app.log` shows `app_started version=<B>` |

### 7.2 Acceptance checklist (spec-vs-implementation, filled by T14)

Evidence keys: script IDs (A–G) from the T10 report; `R#` = runbook row; `M:<app>` = matrix row;
tests by class name.

| Criterion | Evidence (planned) |
|---|---|
| **PRD §5** All snippet and folder CRUD workflows pass functional tests | Core + Presentation suites (A1); section D (create/edit/delete/folder CRUD/rename/delete-moves-to-Unfiled) |
| **PRD §5** Data and settings survive restart without loss | B (migrations, upgrade), C (theme/text size persist), D (persistence), E5 (layout/sort persist) |
| **PRD §5** Popup supports the complete keyboard-only workflow | E1–E4, E9; R11 Narrator |
| **PRD §5** Each toolbar action behaves as specified | E5 (layout, sort), E6 (names/tooltips), E7 (add), E8 (open main) |
| **PRD §5** A snippet created from the popup can be inserted immediately | E7 |
| **PRD §5** Search and popup latency meet the agreed targets | T11 on the 10k dataset: P1 every `popup_ready_ms` ≤300 ms; P2 `popup_search_ms` median ≤150 ms in every (query, layout) cell, p95 reported; E10 small-profile popup samples ≤300 ms. P3 Core benchmark = diagnostic only |
| **PRD §5** Successful dispatch increments usage once and updates last used | F4, F10; matrix "usage counted once" column |
| **PRD §5** Cancellation and failed dispatch do not increment usage | E4 (Esc), F6 (target gone), F9 (paste-anyway Cancel), F11 (target closed after Paste anyway), M:elevated Notepad; uncertain dispatch (`MaybePasted`): 007 unit tests (F12 N/A) |
| **PRD §5** Clipboard contents are restored, or the user is told first when they cannot be | F3; F9 (Cancel keeps the clipboard intact); F10; F11 (snippet left on the clipboard + message); matrix "clipboard restored" and "unsaveable clipboard" columns |
| **PRD §5** Unicode, multiline, selected-text replacement, duplicate-activation protection | F1, F2, F5; matrix columns |
| **PRD §5** Theme and text-size changes do not make essential controls inaccessible | C (theme/text size apply), R11 |
| **PRD §5** Hotkey conflicts, unavailable targets, database failures → understandable feedback | C13 (startup conflict) + accepted observation; F6, M:elevated; D-lock (locked DB) |
| **PRD §5** Compatibility matrix records insertion results for the agreed apps | T12 table in `docs/research/003` |
| **PRD §5** Release artifact passes the Native AOT validation gate | Release job gate + A3 + G1/G9 on the rc zip; PRD §4 items 1–5 re-shown on the release exe: AOT (A3), SQLite CRUD (D), hotkey/popup (C3, E1), focus restore + Unicode insertion (F1), startup registration (C Run value, R10) |
| **AC1** Snippet CRUD details | D (exact whitespace, auto label, tag normalisation, custom label, whitespace-only rejection, inline folder, confirmations) |
| **AC2** Folder CRUD | D (duplicate name, rename propagation, delete → Unfiled), E12 |
| **AC3** Persistence, schema version, transactional migrations | B, E5, Core `MigratorTests` |
| **AC4** Hotkey opens centred popup, focus, reconfigure, conflict, restore default | C3, C9 (+ C9.restart), C13, E1 |
| **AC5** Keyboard-only workflow, mouse = Enter | E3, E4, E9 |
| **AC6** Search semantics, live, empty state, keeps sort/layout | Core search tests; E2, E5 |
| **AC7** Flat/folder layouts, 4 sorts, accessible toolbar | Core sort tests; E3, E5, E6 |
| **AC8** Add snippet from popup | E7 |
| **AC9** Insertion guarantees | F1–F7, F9–F11; F12 via 007 unit tests; matrix; residual focus race accepted by the user (T16 notes, R12) |
| **AC10** Elevated detection + Restart as administrator | M:elevated Notepad; R3 |
| **AC11** Theme/text size live, start at login | C (theme, text size, Run value), R10, R11 |
| **AC12** Close keeps hotkey; Exit unregisters; single instance | C4, C12, second-instance check in C; G4/G5 tray states |
| **AC13** Performance on the user's PC | T11 record: P1, P2 (end to end, 10k dataset, both layouts) |
| **AC14** AOT release artifact passes the gate; matrix recorded | Release run + G1/G9 + T12; install and upgrade from the downloaded zip: R2, R14 |

## 8. Rollout / phasing

1. **Fix-forward PRs to `main`** (CI runs only there). They are grouped so each Windows compile
   failure is easy to trace:
   - PR-1: T1 alone (Actions bump).
   - PR-2: T2 + T3 (publish script, version).
   - PR-3: T5 + T6 + T7 (icons, tray).
   - PR-4: T8 + T9 (removals).
   - PR-5: T10 (verify script).
   - T4 (`release.yml` + docs) lands with PR-2 or separately. It is inert until a tag is pushed.

   Each PR is merged only when its `main` run is green. A red run is fixed by the next PR, never by
   force-push.
2. **Release candidate:** once PR-1…PR-5 are green, the user confirms and `v1.0.0-rc.1` is pushed
   on the green `main` commit → pre-release.
3. **Acceptance on the rc (U):**
   - T10 full script run with `-ArtifactZip` (includes section P = T11 performance);
   - T12 matrix and runbook R1–R14;
   - **R14 needs two published releases.** A = the previous rc, B = the rc under test. When the
     first acceptance round has only `v1.0.0-rc.1`, the user confirms and `v1.0.0-rc.2` is pushed
     (the same commit is allowed; the run number gives it a distinct FileVersion) and R14 runs
     rc.1 → rc.2; the remaining acceptance then runs on rc.2;
   - C records everything (T13–T16).
4. **Failures:** fix-forward PR → new `v1.0.0-rc.N`. Re-run only the affected sections and matrix
   cells, plus a full script run and R14 (previous rc → final rc) on the final rc.
5. **Acceptance sign-off:** the user confirms in writing, recorded in §12.
6. **Final release:** the user confirms, and `v1.0.0` is pushed on **the same commit as the
   accepted rc**. That gives a byte-identical source with a different version stamp; the release job
   re-runs tests and the gate. Pre-releases are kept or deleted per Q3. The user then repeats R14
   steps 2–4 once from the accepted rc to `v1.0.0` (the path real users take), recorded in
   `docs/research/003`.
7. T17 closes the master plan.

The release runs only after acceptance: no non-pre-release tag before step 5.

## 9. Open questions

None blocking approval. The defaults below apply unless the user says otherwise.
1. **Q1 — Symbols.** Native AOT emits a `.pdb`. Default: exclude it from the zip and do not attach
   it. Alternative: attach `SmartSnippets-win-x64-<version>-symbols.zip` for crash analysis.
2. **Q2 — Tag protection.** Recommend a GitHub tag ruleset so only the owner can create `v*` tags.
   This is a user action in repo settings, not code.
3. **Q3 — rc pre-releases after v1.0.0.** Default: keep them, marked pre-release, for history.
4. **Q4 — Size trimming** (`SatelliteResourceLanguages=en`). Default: not in 008, since it touches
   WinAppSDK resource loading. Revisit after v1 if the download size matters.
5. **Q5 — Matrix app availability.** If Word, Outlook or Teams (new vs classic) are not installed or
   licensed on the user's PC, the row is recorded "not available" with the user's acceptance. It is
   not marked as a pass.
6. **Q6 — Exe name.** It stays `SmartSnippets.App.exe`. Renaming would break existing `Run` values
   and every script. Deferred past v1.

## 10. Implementation notes and wiring audit

Harness implementation checklist: [008-harness-checklist.md](../research/008-harness-checklist.md).
Exhaustive static T0/T15 trace and local validation: [008-harness-audit.md](../research/008-harness-audit.md).
T13/T16 acceptance record: [003-release-acceptance-v1.md](../research/003-release-acceptance-v1.md).
No interactive checks, CI dispatches, releases, pushes or merges were performed in this worktree.
T13 end-to-end results and T16 user sign-off remain pending the downloaded-rc run.

## 11. Review response

Review: [docs/review/006-008-implementation-readiness.md](../review/006-008-implementation-readiness.md).
Decisions by the user on 2026-10-28. Finding 5 (Ctrl+Enter in WinUI) is owned by 006 and needs no
008 change; the design side of findings 1–3 is owned by 007.

| Finding | Change in 008 |
|---|---|
| 1 — clipboard restoration can lose data (007) | Verification only: F9 (unsaveable clipboard → "Paste anyway?" → Cancel leaves the clipboard intact), F10 (Paste anyway pastes), `Set-UnsaveableClipboard` / `Test-ClipboardIntact` helpers, matrix step 6 and column, §7.2 clipboard row, T16 note |
| 2 — a failed dispatch can already have pasted (007) | Verification only: F11 (Paste anyway after the target closed → snippet left on the clipboard + message, no usage); F12 records `MaybePasted` as not forceable, evidence = 007 unit tests; §7.2 cancellation/AC9 rows and T16 note |
| 3 — foreground gate is not absolute (007) | Residual few-ms focus race accepted by the user; recorded as R12 and in the T16 acceptance notes; AC9 row cites it |
| 4 — search acceptance measured the wrong path | T11 rewritten as section P: generated 10k dataset (50 folders, defined text/label/tag/usage distribution, planted terms), four queries × both layouts × 20 runs of the end-to-end `popup_search_ms` log, pass = median ≤150 ms per cell, p95 reported; popup `popup_ready_ms` at 10k is pass/fail (≤300 ms every sample) with the reason stated; Core benchmark demoted to diagnostic (P3); §2, §4.1(h), §4.2 P-A/P-B, §4.3, §7.2, R11 updated |
| 6 — archive layout conflicts with upgrade | Zip top folder fixed as `SmartSnippets\`; packaging step checks it; exact install/upgrade steps in §4.1(b) (Exit → verify hash → extract into `%LOCALAPPDATA%\Programs\` with overwrite → start); guide and release header (T4) carry them; R2 updated; new runbook R14 upgrade test (A → B: version, data kept, `Run` value unchanged, start at login works); §8 adds rc.2 for R14 and an rc → v1.0.0 repeat; §4.2 Z-A/Z-B/Z-C; R10 |
| 7 — contract file and tray precedence disagree | §4.1(f) is the single precedence definition: `HotkeyUnavailable` (red) wins over `PopupOpen` (blue); 006's `AppHost.UpdateTrayState()` applies it from the start, so 008 has no deferred behaviour change (T7 only routes it through `TrayStatePolicy`). The shared contract is `src/SmartSnippets.Presentation/Services/InsertionContracts.cs` (006/007 coordinator T0), referenced in T0 and T15 |
| 8 — E11/E12 impossible with popup deactivation | E11/E12 rewritten: live propagation proven by 006 view-model unit tests (store raises `Changed` while the popup VM is shown); end to end by changing data in the main window while the popup is hidden, then reopening and asserting rows/folder headings via UIA; T13 mapping updated |

## 12. Approval log

| Date | Event | Notes |
|---|---|---|
| 2026-10-28 | Independent review remediation | See research/008-harness-audit.md and guides/002-acceptance-harness-safety.md. Owner HWND clipboard publication, verified owned classic targets, typed/retried encrypted text recovery, bounded native sends/out-of-process watchdog, targeted duplicate input with explicit gate receipt, actual E2 typing, safe log summaries, 10k-only acceptance evidence, title privacy and exclusive profile recovery. No desktop acceptance performed; legacy plan002 harness retired. |
| 2026-10-28 | Final T0/T15 harness re-baseline | Every T9–T11/§7 reference audited against merged source; see research/008-harness-audit.md. Fixed nonexistent `hotkey_register_failed` consumer to `hotkey_startup_failed`; Busy returns without an insert_result log (F5 consumes the real popup gate event instead). F11 expects CopiedForManualPaste wording, not TargetClosed wording. Corrected TaskbarTheme.IsLight, Services/TrayStatePolicy and actual theory name. Implementation only, not user acceptance. |
| 2026-10-28 | T0 re-baseline at f809e62 | Popup `VisibleChanged` feeds `UpdateTrayState`; `Presentation/Services/TrayStatePolicy.cs` already implements red-over-blue. `InsertionContracts.cs` defines Inserted, TargetClosed, TargetElevated, FocusFailed, ClipboardBusy, PasteFailed, Busy, SnippetMissing, ClipboardNotPreserved, MaybePasted and CopiedForManualPaste. Prompt is an inline footer: `Some of what's on your clipboard can't be restored after pasting. Paste anyway?`, buttons `Paste anyway` / `Cancel`, automation IDs PopupFooterError, PopupPasteAnyway, PopupPasteCancel. Search ID PopupSearch, name Search snippets; toolbar IDs PopupLayoutButton, PopupSortButton, PopupAddButton, PopupOpenMainButton; editor text name Snippet text. `popup_search_ms` ends at CompositionTarget.Rendered after list layout; `popup_ready_ms` reports popup readiness. `insert_result` logs outcome/id/notice/ms; clipboard events include clipboard_restore and clipboard_restore_formats. A registered private clipboard format exceeding 64 MiB triggers the unsaveable path. Insertion normalizes lone CR, LF and CRLF to CRLF. No popup TestString remains. Actual live folder test is `WhileShown_FolderRenamed_HeadingAndChipsUpdate_SelectionKept`; see PopupViewModelListTests for store-change evidence. |
| 2026-10-28 | Requirement brief confirmed (G1) | 006–008 brief |
| 2026-10-28 | Draft written | Before 006/007 are detailed; T0 re-baselines the popup/insertion acceptance steps once 006/007 are complete |
| 2026-10-28 | Review (docs/review/006-008-implementation-readiness.md) | Findings 4, 6, 7, 8 + verification for 1–3 addressed; user decisions recorded |
