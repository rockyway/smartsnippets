# 021 — Uno desktop acceptance and cutover

**Date:** 2026-10-30
**Status:** Provisional draft—dependency evidence pending; unapproved, not implementation-ready
**Size:** L
**Area:** `src/SmartSnippets.Uno/`, `src/SmartSnippets.App/` (retire on pass), `scripts/`, `.github/workflows/`, `docs/guides/`, `docs/requirements/`
**Requirement brief confirmed:** 2026-10-30 (G1, master 017)
**Master:** [017 — Master plan: Uno cross-platform migration](../../plan/017-master-uno-cross-platform-migration.md)
**Canonical placeholder:** [021 placeholder](../../plan/021-uno-desktop-acceptance-and-cutover.md) (unchanged until 018–020 complete)

---

## 1. Goal

Prove Phase 1 release readiness across Windows x64 and Apple Silicon macOS (`osx-arm64`) on signed/notarized Native AOT release candidates—verifying parity, 10k-snippet performance, target-application compatibility, privacy, unconditional pre-cutover Windows profile backup, in-place Windows upgrade, and tested rollback—before cutting production CI/releases over from `SmartSnippets.App` to `SmartSnippets.Uno`.

## 2. Requirement (as confirmed at G1)

Excerpted from `docs/plan/017-master-uno-cross-platform-migration.md:19–55` and `docs/plan/021-uno-desktop-acceptance-and-cutover.md:7–21`:

- **Problem:** Even after 019 and 020 implement Windows and macOS Uno adapters, `SmartSnippets.Uno` runs under candidate isolation, macOS builds are ad-hoc signed rather than Developer ID notarized, `Migrator`'s `.bak` will not fire when schema stays at v2, and production CI/release pipelines still ship `SmartSnippets.App`.
- **In scope:** Explicit pre-cutover Windows profile backup independent of schema migration; Windows identity/profile/startup cutover (`SmartSnippets.exe`, `SmartSnippets.Main`, `%LOCALAPPDATA%\SmartSnippets`, HKCU `Run`); Apple Silicon Developer ID signing, hardened runtime (no JIT entitlements), notarization, and stapling; Windows + macOS 10k performance and target-app compatibility matrices; synthetic log-privacy verification; production CI & `release.yml` cutover; retiring `SmartSnippets.App` and disposable `prototypes/uno-desktop/` after acceptance; updating guides/PRD/AGENTS without rewriting historical logs; Phase 1 desktop wiring audit.
- **Out of scope / non-goals:** Mobile feasibility/implementation (Phase 2 plans 022–025 remain pending after Phase 1 ships), Intel macOS, Windows ARM64, Windows Authenticode/MSIX changes not already in baseline (`008 §2`), cloud sync, password snippets (`016`).
- **Users and surfaces:** Upgrading Windows users, new Windows x64 and Apple Silicon macOS users, maintainers running release/acceptance workflows, and GitHub Actions CI/Release pipelines.
- **Behaviour:** Upgrading Windows users keep every snippet, folder, usage counter, setting, and `Start at login` registration with a verified pre-cutover backup for rollback; macOS users install a notarized `arm64` app bundle that passes Gatekeeper and TCC checks; any failed acceptance check blocks cutover and retains `SmartSnippets.App`.
- **Constraints and assumptions:** Conditional on Plans 018, 019, and 020 completing with zero unresolved blockers, available Apple Developer ID credentials/notarization access, recorded Windows x64 and Apple Silicon reference hardware, and explicit operator authorization for live screen-harness runs.
- **Acceptance criteria:** Satisfies Phase 1 gate for master **AC1** (Windows parity & saved data), **AC2** (Apple Silicon macOS matrix), **AC3** (desktop Native AOT & safety tests), **AC5** (import/export, persistence, permissions, failure/usage accounting), and **AC6** (offline, leak-free logs).
- **Decisions made:** Direct signed/notarized macOS distribution (`017 §4.1`); unconditional pre-cutover backup because `Migrator` `.bak` is schema-conditional (`017 §5`); Phase 1 completion never claims Phase 2 completion (`017 §6`).

## 3. Current state (as-built)

- `src/SmartSnippets.Core/Data/Migrator.cs:46–49` calls `Backup()` **only** when `start > 0 && pending.Count > 0`. Moving an existing schema-v2 Windows profile onto `SmartSnippets.Uno` triggers zero schema migrations and therefore writes **no** `.bak` unless an explicit cutover backup is added.
- `src/SmartSnippets.App/Program.cs:13`, `AppPaths.cs:7`, `StartupRegistration.cs:11–12`, and `StartupMigration.cs:16–39` bind Windows production ownership to `InstanceKey = "SmartSnippets.Main"`, `%LOCALAPPDATA%\SmartSnippets`, and `HKCU\Software\Microsoft\Windows\CurrentVersion\Run\SmartSnippets` (`"<exe>" --background`).
- `.github/workflows/ci.yml:22–61`, `.github/workflows/release.yml:45–81`, and `scripts/publish-aot.ps1:14–38` hardcode `src/SmartSnippets.App`, verify `SmartSnippets/SmartSnippets.exe`, `e_sqlite3.dll`, and `Assets/TitleIcon.png`, reject `SmartSnippets.dll` / `SmartSnippets.App.exe`, and have no `osx-arm64` signing/notarization/release job.
- `docs/guides/002-acceptance-harness-safety.md:1–95` defines Windows classic-Notepad verification, `.verify.lock` leases, `registry.clixml` journaling, DPAPI clipboard backstop, `-PerfLibrary 10000` gating, and G10 synthetic leak scanning; no equivalent macOS acceptance safety guide or runner exists yet.
- `docs/guides/001-install-and-release.md:1–25` and `003-release-upgrade-runbook.md:1–10` document Windows-only zip upgrades; `AGENTS.md:5–25` describes `SmartSnippets.App` as the sole UI head.

## 4. Design

### 4.1 Approach

1. **Unconditional pre-cutover Windows backup & cutover marker:** Add `CutoverBackupCoordinator` invoked in `UnoAppHost` before `Migrator`/`SnippetStore` open `%LOCALAPPDATA%\SmartSnippets\smartsnippets.db` when `settings` lacks `uno_desktop_cutover_v1 = 1` and the DB file already exists. Using a read-only SQLite connection (`VACUUM INTO`) or exclusive file copy before write open, emit `smartsnippets.db.pre-uno-v1.bak` (preserving existing backups via UTC timestamp suffix like `Migrator.cs:72–75`), verify non-zero SQLite header + `PRAGMA quick_check`, record `cutover_backup` in `app.log` (filename only, no user path), and set `uno_desktop_cutover_v1 = 1` after successful load. If backup creation fails, abort startup via `IStartupErrorReporter` **without** mutating the live DB.
2. **Production identity switch & legacy retirement:** Switch `SmartSnippets.Uno` Windows build properties to `AssemblyName=SmartSnippets`, `InstanceKey="SmartSnippets.Main"`, production `AppPaths`, and `StartupMigration` (`StartupMigration.cs:16–39`). Only after Windows parity, version-aware upgrade (`003-release-upgrade-runbook.md`), and backup-restore rollback tests pass on RC artifacts, remove `src/SmartSnippets.App` and `prototypes/uno-desktop/` from active build/tree and point `publish-aot.ps1`, `ci.yml`, and `release.yml` at `src/SmartSnippets.Uno`.
3. **macOS Developer ID signing, hardened runtime & notarization:** Build `SmartSnippets-osx-arm64-<version>.zip` (and/or `.dmg`) + `.sha256` from fresh Native AOT output. Enforce hardened runtime (`codesign --options runtime --timestamp`) with **zero** `com.apple.security.cs.allow-jit` or `allow-unsigned-executable-memory` entitlements (proving pure Native AOT), sign `libe_sqlite3.dylib` and inner Mach-O binaries inside-out, notarize via `xcrun notarytool submit --wait`, staple, and verify `spctl -a -vvv -t exec`. Re-run TCC Accessibility, `SMAppService` login item, and target-app insertion on the **notarized** bundle so ad-hoc TCC assumptions cannot mask release failures.
4. **Guarded cross-platform acceptance & perf gates:** Extend Windows `verify-plans-003-009.ps1` / `verify-ui-qa.ps1` to target the cutover `SmartSnippets.exe` and add `scripts/verify-macos-desktop.py` (inheriting `002-acceptance-harness-safety.md` rules: exclusive lockfile, isolated temp profile, owned two-window synthetic target, no clipboard plaintext on disk, bounded watchdog, G10 synthetic log leak scan). Measure `-PerfLibrary 10000` `popup_ready_ms` ≤ 300 ms and `popup_search_ms` median ≤ 150 ms on recorded Windows x64 and Apple Silicon hardware.

### 4.2 Alternatives considered

| Option | Pros | Cons | Verdict |
|---|---|---|---|
| A — Unconditional pre-cutover DB backup + RC upgrade/notarization gate before retiring `SmartSnippets.App` | Protects v2 Windows users despite unchanged schema; tests real Gatekeeper/TCC identity before deleting fallback | Requires two-step RC verification before final tag | **Chosen** — enforces master `017 §5, §8` |
| B — Rely on `Migrator.cs:46` `.bak` and JSON export for rollback | Zero new backup code | `Migrator` writes no `.bak` on schema v2→v2; JSON loses exact internal IDs/settings keys | Rejected — explicitly forbidden by `017 §5` |
| C — Delete `SmartSnippets.App` first, then run acceptance | Fewer files during RC | Destroys reference comparison baseline and complicates emergency Windows hotfix | Rejected |
| D — Ship ad-hoc signed macOS zip with `xattr -cr` instructions | Avoids Apple Developer ID / notarization setup | Fails confirmed G1 requirement (`distribution requires signing/notarization`, `017 §2`) and breaks TCC stability | Rejected |

### 4.3 Data / config / API model

- **Settings key (`SettingsKeys`):** `UnoDesktopCutoverCompleted = "uno_desktop_cutover_v1"` (`"1"` once pre-cutover backup + load succeed; ignored on fresh empty profiles where no prior `smartsnippets.db` existed).
- **Backup artifact:** `%LOCALAPPDATA%\SmartSnippets\smartsnippets.db.pre-uno-v1.bak` (verified SQLite copy created prior to first Uno write).
- **Release assets (`release.yml` on `v*` tag):**
  - `SmartSnippets-win-x64-<version>.zip` + `.sha256` (`SmartSnippets/SmartSnippets.exe`, `e_sqlite3.dll`, `Assets/*`, zero `.dll` managed apphost or `.pdb`).
  - `SmartSnippets-osx-arm64-<version>.zip` (or `.dmg`) + `.sha256` (`Smart Snippets.app`, `Mach-O 64-bit arm64`, signed + notarized + stapled, `libe_sqlite3.dylib`, zero `.pdb`/`.dSYM` inside bundle).

### 4.4 Sequence diagram of the new hot path

```text
Windows Upgrade Launch (SmartSnippets.exe replacing legacy SmartSnippets.exe / SmartSnippets.App.exe)
  → Program.Main (WaitForPreviousInstance --after-pid; AppInstance "SmartSnippets.Main")
    → UnoAppHost.TryLoadData()
      → CutoverBackupCoordinator.EnsureBackedUp(paths.DatabasePath)
        ├─ Existing DB & uno_desktop_cutover_v1 != "1" → write & quick_check smartsnippets.db.pre-uno-v1.bak
        │     ├─ Failure → IStartupErrorReporter; exit BEFORE touching live DB
        │     └─ Success → continue
        └─ Fresh profile or already marked → no-op
      → Migrator.MigrateToLatest() → SnippetStore.Load() → AppSettings.Load()
      → Mark uno_desktop_cutover_v1 = "1"
      → StartupMigrationCoordinator (preserves/updates HKCU Run "--background")
Parallel paths checked:
  - Background login launch (--background on Windows; SMAppService on macOS) → runs same CutoverBackupCoordinator before store open.
  - Rollback path → Exit Uno app → restore smartsnippets.db.pre-uno-v1.bak over smartsnippets.db → launch previous Windows release binary → verify snippets/settings/startup intact.
```

### 4.5 Surface list

| Surface | Change | Task |
|---|---|---|
| `CutoverBackupCoordinator` + `UnoAppHost` startup | Unconditional pre-cutover `.pre-uno-v1.bak` + integrity check + fail-closed abort | T1 |
| `SmartSnippets.Uno` Windows identity + `StartupMigration` | Switch candidate keys to `SmartSnippets.exe` / `SmartSnippets.Main` / `%LOCALAPPDATA%\SmartSnippets` | T2 |
| `scripts/publish-macos-release.sh` + entitlements/signing | Hardened-runtime Developer ID signing (no JIT), `notarytool`, `stapler`, `spctl`/arch/AOT gate | T3 |
| Windows & macOS acceptance/perf harnesses + compatibility matrix | 10k perf, target-app matrix, upgrade/rollback run, G10 privacy scan | T4 |
| `.github/workflows/ci.yml`, `release.yml`, `SmartSnippets.App`, `prototypes/`, docs | Cut CI/release over to `SmartSnippets.Uno`, retire legacy head & probe, update guides/AGENTS/PRD | T5, T6 |

## 5. Impact and risk

| Question | Answer |
|---|---|
| State management — what survives a crash mid-operation? | Pre-cutover `.bak` is completed and verified before any Uno write transaction opens `smartsnippets.db`; failed backup leaves original DB byte-identical. |
| Data consistency — orphans, races, double-apply? | Upgrade runbook & `AppInstance` mutex forbid concurrent old/new owners; `uno_desktop_cutover_v1` prevents overwriting the original pre-cutover backup on subsequent launches. |
| User experience — root problem or symptom; any surprise? | Target UX (gated by T4): preserve Windows folder, shortcut, tray, and data across upgrade, and launch notarized macOS `.app` cleanly through Gatekeeper. |
| Backward compatibility — existing data / sessions / configs? Migration? | Verified in T4 against real v1 and v2 Windows fixture profiles and version-aware upgrade A → RC → Final (`003-release-upgrade-runbook.md`). |
| Error recovery — what happens on failure; can it recover? | Documented offline rollback restores `.pre-uno-v1.bak` and prior Windows release zip without touching unrelated HKCU entries or OS-wide TCC. |
| Logging & debugging — enough to diagnose in the field? | `AppLog` emits UTC timestamps plus `cutover_backup`, `startup_migration`, `popup_ready_ms`, `popup_search_ms`, and `menu_surface`; G10 scans `app.log` and captured diagnostic streams for synthetic fixture/title/path tokens (per `002-acceptance-harness-safety.md:42–44`, fixture evidence rather than blanket OS-framework log guarantees). |
| Edge cases — empty, huge, repeated, concurrent, interrupted? | Read-only/locked DB on cutover backup fails closed; 10k-snippet perf budget verified in both Flat and Folder layouts; notarization stapler offline launch verified. |

**Risks:**
- **R1 — Hardened runtime breaks Uno/Skia if any hidden dynamic code path remains** → Test signed `--options runtime` bundle without JIT entitlements before final tag; any crash is a release blocker.
- **R2 — Performance regression under Uno XAML/Skia at 10k snippets** → Measure `popup_ready_ms` (≤300 ms) and `popup_search_ms` (≤150 ms) on Release AOT artifacts before retiring `SmartSnippets.App`.
- **R3 — Missing Apple Developer ID / notarization secrets in CI** → Treat missing signing credentials as `BLOCKED` (never ship unsigned/ad-hoc macOS release as Phase 1 complete; `017 §8`).

**Rollback:** Keep the last pre-Uno Windows release artifact and `smartsnippets.db.pre-uno-v1.bak`; revert `SmartSnippets.slnx`/`ci.yml`/`release.yml` to `SmartSnippets.App` if any RC gate fails prior to final tag.

## 6. Tasks

### T1 — Implement unconditional pre-cutover Windows backup coordinator (AC1, AC5)
- **Files:** `src/SmartSnippets.Core/Data/CutoverBackup.cs`, `src/SmartSnippets.Uno/UnoAppHost.cs`, `src/SmartSnippets.Core/Settings/SettingsKeys.cs`
- **Change:** Before opening an existing `smartsnippets.db` lacking `uno_desktop_cutover_v1=1`, create and `quick_check`-verify `smartsnippets.db.pre-uno-v1.bak` without overwriting prior backups; fail closed via `IStartupErrorReporter` if backup fails; persist `uno_desktop_cutover_v1=1` only after store + settings load succeed.
- **Verify:** `dotnet test tests/SmartSnippets.Core.Tests` sequentially.
- **Test that dies if this breaks:** `CutoverBackupTests.ExistingSchemaV2Database_CreatesVerifiedBackupOnce_AndFailsClosedWithoutMutatingOriginalWhenDiskDenied`.

### T2 — Switch `SmartSnippets.Uno` Windows target to production identity & startup migration (AC1, AC3)
- **Files:** `src/SmartSnippets.Uno/SmartSnippets.Uno.csproj`, `src/SmartSnippets.Uno/Program.cs`, `src/SmartSnippets.Uno/Platforms/Windows/StartupMigration.cs`, `scripts/publish-aot.ps1`
- **Change:** Configure `SmartSnippets.Uno` Windows output as `SmartSnippets.exe` with version stamping, `InstanceKey="SmartSnippets.Main"`, default `AppPaths`, and HKCU `Run` migration compatibility; verify exact zip layout required by `release.yml:68`.
- **Verify:** `./scripts/publish-aot.ps1 -Output publish -Version 2.0.0-rc.1 -BuildNumber 1` produces native `SmartSnippets.exe`, `e_sqlite3.dll`, zero `IL2xxx`/`IL3xxx` warnings, and zero managed `.dll`.
- **Test that dies if this breaks:** `WindowsCutoverIdentityTests.MatchesLegacySettingsKeys_PreservesRunRegistryCommand_AndRejectsNonAotLayout`.

### T3 — Apple Silicon Developer ID signing, hardened runtime & notarization pipeline (AC2, AC3)
- **Files:** `scripts/publish-macos-aot.sh`, `src/SmartSnippets.Uno/Platforms/MacOS/Entitlements.plist`, `.github/workflows/release.yml`
- **Change:** Assemble `Smart Snippets.app` from `osx-arm64` Native AOT publish, verify zero JIT/interpreter entitlements and `Mach-O 64-bit arm64` slices (`SmartSnippets` + `libe_sqlite3.dylib`), sign inside-out with Developer ID + `--options runtime --timestamp`, notarize via `notarytool`, staple, and package checksummed `.zip`/`.dmg`.
- **Verify:** `codesign --verify --deep --strict`, `spctl -a -vvv -t exec`, `xcrun stapler validate`, and entitlement audit rejecting `allow-jit`.
- **Test that dies if this breaks:** `MacReleaseArtifactGateTests.RejectsAdHocSignature_JitEntitlement_X64Slice_OrUnstapledBundle`.

### T4 — Execute guarded Windows/macOS acceptance, 10k perf, upgrade/rollback & target matrices (AC1–AC3, AC5–AC6)
- **Files:** `scripts/verify-plans-003-009.ps1`, `scripts/verify-ui-qa.ps1`, `scripts/verify-macos-desktop.py`, `docs/guides/002-acceptance-harness-safety.md`, `docs/research/017-phase1-desktop-acceptance-evidence.md` (next free `NNN` at execution time)
- **Change:** Run authorized isolated acceptance on downloaded/fresh RC artifacts across both OSes: `-PerfLibrary 10000` (`popup_ready_ms` ≤300 ms, `popup_search_ms` ≤150 ms), Windows & macOS target-app matrices (reading back actual target buffers to verify delivery), Windows A→RC upgrade + `.pre-uno-v1.bak` rollback drill, runtime zero-network source/dependency audit + offline execution (distinct from build-time `codesign --timestamp` / `notarytool`), and G10 synthetic fixture/title/path leak scan.
- **Verify:** All acceptance rows PASS with recorded hardware/OS/SHA256 digests; zero `NOT RUN` rows treated as PASS.
- **Test that dies if this breaks:** `AcceptanceReportValidatorTests.FailsWhenPerfBelow10k_TargetMatrixIncomplete_OrLeakDetected`.

### T5 — Production CI/release cutover, legacy `SmartSnippets.App` retirement & doc updates (AC1, AC3)
- **Files:** `SmartSnippets.slnx`, `.github/workflows/ci.yml`, `.github/workflows/release.yml`, `src/SmartSnippets.App/` (remove after T4 pass), `prototypes/uno-desktop/` (remove after T4 pass), `AGENTS.md`, `docs/guides/001-install-and-release.md`, `003-release-upgrade-runbook.md`, `docs/requirements/001-prd.md`
- **Change:** Promote Windows + Apple Silicon macOS Native AOT jobs to main `ci.yml` and tag `release.yml`; remove retired `SmartSnippets.App` and disposable `prototypes/uno-desktop/`; update operational docs without altering historical plan approval logs.
- **Verify:** Clean `dotnet test` (sequential) + `publish-aot.ps1` + `publish-macos-aot.sh` after legacy folder removal.
- **Test that dies if this breaks:** `RepoHygieneTests.SolutionAndWorkflowsReferenceOnlyProductionUnoHead_AndNoPrototypeRemains`.

### T6 — Phase 1 desktop wiring audit (AC1–AC3, AC5–AC6)
- **Files:** `tests/SmartSnippets.Presentation.Tests/Wiring/Phase1DesktopWiringAuditTests.cs`, `docs/plan/017-master-uno-cross-platform-migration.md` (Phase 1 status update only)
- **Change:** Trace every Windows and macOS producer → `UnoAppHost` composition → VM/XAML binding → native OS adapter → SQLite/clipboard/startup side effect, including parallel `Insert` (hotkey, tray/menu bar, popup Enter/row, main-row) vs `Copy` vs `--preview-popup*`, startup backup, and shutdown clipboard restore. Confirm Phase 2 (`022–025`) stays marked pending.
- **Verify:** Runtime receipt verification across fresh Windows x64 and notarized macOS `arm64` runs + zero orphan registrations.
- **Test that dies if this breaks:** `Phase1DesktopWiringAuditTests.EveryDesktopEntryAndShutdownEdgeIsWiredAndEmitsExpectedReceipt`.

## 7. Test strategy

- **Unit:** `CutoverBackupCoordinator` idempotency/failure modes, release-tag/zip/bundle manifest validators, entitlement/AOT property guards, and startup-migration command preservation.
- **Integration:** Windows v1/v2 fixture copy → `UnoAppHost` cutover backup + load + rollback restore roundtrip; Windows + macOS full composition flow tests across management, JSON porting, single-flight insertion, and shutdown restore.
- **Manual runbook (authorized exclusive screen slots on Windows x64 & Apple Silicon Mac; isolated profiles per `002-acceptance-harness-safety.md`):**

| # | Step | Expected |
|---|---|---|
| 1 | Seed prior Windows release in isolated profile with snippets, custom palette/hotkey, and `Start at login`; exit and confirm process gone | Baseline DB hash and HKCU `Run` `REG_SZ` recorded |
| 2 | Extract Windows Uno RC zip, launch `SmartSnippets.exe`, inspect profile dir, snippets, settings, HKCU `Run`, and run Windows target matrix + 10k perf | `.pre-uno-v1.bak` created & valid; all data/startup intact; `popup_ready_ms` ≤300 ms, `popup_search_ms` ≤150 ms; matrix green |
| 3 | Exit Uno RC, restore `.pre-uno-v1.bak` + prior Windows release zip, relaunch prior release | Clean rollback; all pre-cutover snippets, settings, and hotkey work with zero corruption |
| 4 | On offline Apple Silicon Mac, mount/extract notarized `Smart Snippets.app`, launch via Finder (Gatekeeper check), grant Accessibility, run macOS target matrix + 10k perf + login-item reboot | Zero Gatekeeper warnings; exact-window paste + clipboard restore pass across agreed apps; perf within budget |
| 5 | Run G10 privacy scanner across Windows & macOS app logs and captured diagnostics | Zero synthetic snippet content, labels, tags, window titles, queries, or user document paths |

## 8. Rollout / phasing

Ship `v2.0.0-rc.1` (or next agreed version) from release workflow, complete T4 acceptance and upgrade/rollback runbook on downloaded RC assets, retire `SmartSnippets.App` in T5, verify `rc.2` → final tag upgrade, and mark **Phase 1 only** complete in `017`.

## 9. Open questions / dependency & assumption register

| ID | Dependency / Assumption | Status | Owner / Gate |
|---|---|---|---|
| D1 | Completion of Plans 018, 019, and 020 with verified Windows & macOS AOT dependencies | **Pending** | Implementer / prerequisite plans |
| D2 | Apple Developer ID Application certificate, App Store Connect API key (`notarytool`), and GitHub Actions macOS secrets | **External prerequisite** — absence blocks T3/Phase 1 release (`017 §8, §9`) | User before 021 execution |
| D3 | Agreed macOS target-application list & recorded Windows/Mac reference hardware specs for 10k perf sign-off | **Deferred to pre-021 G2** (`017 §9`) | User + implementer at 021 G2 |

## 10. Approval log

| Date | Event | Notes |
|---|---|---|
| 2026-10-30 | Requirement brief confirmed (G1) | Confirmed in master plan `017` (Phase 1 desktop parity, Native AOT, signing/notarization, safe cutover) |
| 2026-10-30 | Provisional candidate drafted | Written to `docs/drafts/uno-019-025/`; depends on 018–020 completion; **unapproved, not implementation-ready** |
