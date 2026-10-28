# 008 — Release hardening and acceptance

**Date:** 2026-10-28
**Status:** Draft — awaiting approval
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
> wording, accessible names) and may revise those steps. Revisions are recorded in §11 before work
> starts.

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
  - Performance acceptance on the user's PC, recorded: popup ≤300 ms (`popup_ready_ms` log) and
    search ≤150 ms at 10k snippets (benchmark).
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
          # copy publish/* except *.pdb into stage/SmartSnippets-<version>/
          # Compress-Archive stage/* SmartSnippets-win-x64-<version>.zip
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
- **Zip layout.** The zip contains one top folder, `SmartSnippets-<version>/`, so users extract to a
  named folder.
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
- **Taskbar theme:** `TaskbarTheme.Read()` reads `HKCU\Software\Microsoft\Windows\CurrentVersion\
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
- **One state policy:** a pure function `TrayStatePolicy.Compute(bool hotkeyRegistered,
  bool popupOpen)` lives in Presentation so it can be unit-tested on Linux. The result is
  `HotkeyUnavailable` if the hotkey is not registered, else `PopupOpen` if the popup is open, else
  `Ready`.
  - Red wins because it is a persistent fault the user must act on. The open popup is already
    visible on screen.
  - `AppHost` keeps the two inputs, (`hotkeyManager.Status.IsRegistered`, the popup's
    visible/hidden events from 006), and assigns `tray.State = Compute(...)` from both handlers. That
    removes the overwrite hazard in §3.
  - `TrayState` moves to Presentation next to the policy. `TrayIcon` keeps using it.
  - If 006 already centralises this, T0 adopts 006's shape instead.

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
- **New sections:** E (popup, 006), F (insertion, 007) and G (release, icons and tray, 008). They
  are listed in §6 T10.

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
    → Package: stage (no *.pdb) → zip → .sha256
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
                             │     theme' = TaskbarTheme.Read(); if theme' != theme:
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
| `scripts/verify-plans-003-009.ps1`, `scripts/lib/Uia.ps1` | `-ArtifactZip`; sections E, F, G; C11 updated; A3 via shared gate; locked-DB check | T10 |
| `scripts/verify-plan002.ps1` | Header note: historical, superseded by sections A/E/F/G | T10 |
| `docs/research/003-release-acceptance-v1.md` (new) | Perf, matrix, audits, observations | T11–T16 |
| `docs/guides/001-install-and-release.md` (new) | Install/upgrade/uninstall, SmartScreen, maintainer release steps | T4 |
| `docs/plan/001` master | §5/§9 status, Complete | T17 |

## 5. Impact and risk

| Question | Answer |
|---|---|
| State management — what survives a crash mid-operation? | Release job: nothing is published until the final `gh release create`. A failed run leaves at most a tag with no release, which can be re-run after deleting the tag or by pushing a new tag. App: tray icon handles are process-local; Explorer drops the icon on exit as today |
| Data consistency — orphans, races, double-apply? | `concurrency` serialises runs per tag; "release exists" fails the job instead of clobbering. Tray state has one writer (`AppHost.UpdateTrayState`) on the UI thread |
| User experience — root problem or symptom; any surprise? | Unsigned exe → SmartScreen "Windows protected your PC" on first run. Documented in the release header and the install guide (More info → Run anyway, or Unblock in Properties). Removing Diagnostics removes a duplicate start-at-login toggle; Settings keeps it |
| Backward compatibility — existing data / sessions / configs? Migration? | DB untouched; `probe` table kept. The `Run` value stores the exe path: extracting a new version **over the same folder** keeps start-at-login working. A new folder needs the toggle re-done (documented). Downgrade to an older build with a newer schema is refused (B checks) |
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
  restores the user's profile; the F section also saves and restores the user's clipboard text.
- **R8 — Actions major bump changes behaviour** (e.g. `upload-artifact` v4→v5 artifact semantics).
  → Bump in its own commit, then read the next `main` run's annotations and artifact. Revert that
  commit if it misbehaves.
- **R9 — Generator toolchain drift** (cairosvg version changes anti-aliasing). → Pinned version;
  `--check` mode; outputs checked in, so a drift only shows when someone regenerates.

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
- **Files:** this plan only (§11 revision row).
- **Change:** re-read the merged 006/007 code and plans, then update §6 T9/T10 and §7:
  - how the popup reports open/closed;
  - log events (`insert_result`, usage update, duplicate guard, elevation block);
  - error wording;
  - accessible names of the popup search box, list, toolbar and add-snippet editor;
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
  - The guide covers: download; verify the hash (`Get-FileHash -Algorithm SHA256`); extract to a
    stable folder (e.g. `%LOCALAPPDATA%\Programs\SmartSnippets`); SmartScreen first-run steps;
    upgrade (Exit from the tray, extract over the folder); uninstall (turn off Start at login, Exit,
    delete the folder, optionally delete `%LOCALAPPDATA%\SmartSnippets`); where logs are.
  - The maintainer section covers the tag procedure and the rc-first rule (§8).
- **Verify (U + C):**
  1. After T1–T9 are on `main` and CI is green, the **user** approves and pushes
     `v1.0.0-rc.1` (or authorises Claude to push that exact tag).
  2. (C) checks the run: all steps green.
  3. The release is marked pre-release, with 2 assets.
  4. The `.sha256` matches the zip after download (`gh release download` + `sha256sum -c`).
  5. The zip has one top folder, has no `.pdb`, and contains `Assets\*.ico`.
  6. The header and the generated notes both appear.
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
- **Files:** `Services/TrayIcon.cs`, `Services/TaskbarTheme.cs` (new), `NativeMethods.txt` (`LoadImage`,
  `DestroyIcon`, `GetSystemMetricsForDpi`, `GetDpiForSystem`, `WM_SETTINGCHANGE`, `IMAGE_TYPE`,
  `IMAGE_FLAGS`, `SYSTEM_METRICS_INDEX`), `Presentation/Shell/TrayStatePolicy.cs` (+ `TrayState`),
  `AppHost.cs` (`UpdateTrayState`, wiring to the hotkey status and 006's popup events),
  `tests/SmartSnippets.Presentation.Tests/TrayStatePolicyTests.cs`.
- **Change:** §4.1(e)(f).
- **Verify:**
  - (C) Unit tests:
    - `Unregistered_Wins_OverPopupOpen`
    - `PopupOpen_WhenRegistered`
    - `Ready_WhenRegisteredAndClosed`
    - `PopupClose_ReturnsToUnavailable_WhenHotkeyLost`
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
    - E10 `popup_ready_ms` sampled 20 times (first open after a `--background` start recorded
      separately) → median/p95/max in the report.
    - E11 Master §7 #1: with the popup open, save a snippet in the main window → the popup list
      shows it without reopening.
    - E12 Master §7 #2: rename a folder → popup headings update.
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
  -ReportPath docs\research\003-run.md`. Pass bar: 0 FAIL, 0 WARN. Remaining SKIPs = only the §7
  manual list. The report is attached to `docs/research/003` (T16).

### T11 — Performance acceptance (U runs; C records) (AC13, PRD §5)
- **Popup:** E10 samples on the rc build.
  - Pass: **every** sample ≤300 ms, including the first open after a background start.
  - Also recorded, informational: the same with a seeded 10k-snippet profile (`-PerfLibrary 10000`
    seeds via sqlite into the temporary profile). If it exceeds 300 ms, raise it to the user; do not
    silently accept it.
- **Search:**
  `dotnet test tests/SmartSnippets.Core.Tests -c Release --filter Category=Benchmark --logger "console;verbosity=detailed"`
  on the user's PC. Pass: all medians ≤150 ms and build+sort ≤150 ms (`SearchBenchmarkTests.cs:64-68`).
  The script captures the output lines (`:56-62`) into the report, along with CPU model and RAM.
- **Verify:** numbers recorded in `docs/research/003` §Performance, with machine details and build
  version.

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
  5. check that the main window's "Used" counter went up by exactly the number of insertions.
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
  once · notes.
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
  | #1 | E11 |
  | #2 | E12 (+ D folder-rename propagation) |
  | #3 | F4 |
  | #4 | C9 plus a restart check: C9.restart, added — after the change and an app restart, the new hotkey still opens the popup |
  | #5 | C12.free |
  | #6 | B (fresh profile, v1 → v2 upgrade) |

- **Verify:** each row PASS in the T10 report, cited in `docs/research/003`.

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
  - `TaskbarTheme.Read` ← `Add`, `TaskbarCreated`, `WM_SETTINGCHANGE`.
  - `TrayStatePolicy` ← both `AppHost` handlers. 006's popup events have a subscriber, and the
    subscriber unsubscribes on exit.
  - New log events present in A4.
  - Removed Diagnostics: no dangling callbacks or ctor parameters.
  - 006/007 handoffs: popup → inserter → usage update → store `Changed` → popup and main list
    refresh. One refresh path (master §4).
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
- **Verify:** the user reads the doc. The acceptance sign-off is recorded in §11 and in the master §9.

### T17 — Close the programme (C)
- **Files:** `docs/plan/001-master-smart-snippets-v1.md` (§5 status for 004/005/009 runbooks → done
  via 008, 008 → Complete; §9 log row; Status → Complete); this plan's Status.
- **Precondition:** the final `v1.0.0` release is published (§8 step 6).
- **Verify:** the master's progress table has no "pending" entries.

## 7. Test strategy

### 7.1 Layers
- **Unit (Linux + Windows CI):**
  - `TrayStatePolicyTests` (new);
  - existing Core (incl. `MigratorTests` with the `probe` table) and Presentation suites, unchanged
    and green;
  - generator `--check` (Linux, run by C when icons change).

  Deliberately not unit-tested: Win32 icon loading and registry reads (thin interop, covered by G
  checks on the real exe).
- **CI (Windows):** the AOT gate through `publish-aot.ps1`, with the version stamp check. The release
  workflow is exercised by the rc tag (§8).
- **Automated acceptance on the user's PC:** `verify-plans-003-009.ps1 -ArtifactZip <rc zip>`,
  sections A–G. Pass bar: 0 FAIL, 0 WARN; SKIPs limited to the runbook rows below.
- **Manual runbook (U, on the rc build).** Unchecked rows block the final tag.

| # | Step | Expected |
|---|---|---|
| R1 | Download the rc zip from the Release page; `Get-FileHash` | Matches the `.sha256` asset |
| R2 | Extract with Explorer, run `SmartSnippets.App.exe` | SmartScreen warning as documented; "Run anyway" starts the app. Properties → Details shows the version |
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
| **PRD §5** Search and popup latency meet the agreed targets | T11: E10 samples ≤300 ms; benchmark medians ≤150 ms on the user's PC |
| **PRD §5** Successful dispatch increments usage once and updates last used | F4; matrix "usage counted once" column |
| **PRD §5** Cancellation and failed dispatch do not increment usage | E4 (Esc), F6 (target gone), M:elevated Notepad |
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
| **AC9** Insertion guarantees | F1–F7; matrix |
| **AC10** Elevated detection + Restart as administrator | M:elevated Notepad; R3 |
| **AC11** Theme/text size live, start at login | C (theme, text size, Run value), R10, R11 |
| **AC12** Close keeps hotkey; Exit unregisters; single instance | C4, C12, second-instance check in C; G4/G5 tray states |
| **AC13** Performance on the user's PC | T11 record |
| **AC14** AOT release artifact passes the gate; matrix recorded | Release run + G1/G9 + T12 |

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
   - T10 full script run with `-ArtifactZip`;
   - T11 performance;
   - T12 matrix and runbook R1–R13;
   - C records everything (T13–T16).
4. **Failures:** fix-forward PR → new `v1.0.0-rc.N`. Re-run only the affected sections and matrix
   cells, plus a full script run on the final rc.
5. **Acceptance sign-off:** the user confirms in writing, recorded in §11.
6. **Final release:** the user confirms, and `v1.0.0` is pushed on **the same commit as the
   accepted rc**. That gives a byte-identical source with a different version stamp; the release job
   re-runs tests and the gate. Pre-releases are kept or deleted per Q3.
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

*(Filled during implementation: T0 revisions, deviations, CI runs, release runs, link to
`docs/research/003-release-acceptance-v1.md`.)*

## 11. Approval log

| Date | Event | Notes |
|---|---|---|
| 2026-10-28 | Requirement brief confirmed (G1) | 006–008 brief |
| 2026-10-28 | Draft written | Before 006/007 are detailed; T0 re-baselines the popup/insertion acceptance steps once 006/007 are complete |
