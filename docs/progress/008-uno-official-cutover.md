# 008 — Uno official Windows cutover

**Date:** 2026-11-01
**Authority:** user D86 / implementer brief dn-03. This explicit Windows cutover supersedes the former separate-candidate identity/coexistence scope; it does not retroactively complete historical plans or establish macOS/mobile parity.
**Implementation:** `10fa6ef6dcc75cd87e3514fe681e770d0ef31802` (`feature/uno-official`).

## Changes

- `src/SmartSnippets.Uno` is now the sole official head in `SmartSnippets.slnx`; assembly/output is `SmartSnippets.exe`, application ID `SmartSnippets`, display title Smart Snippets.
- Takes over the legacy `%LOCALAPPDATA%\SmartSnippets` profile, `SmartSnippets.Main` instance key, `SmartSnippets.App.app` manifest identity, `SmartSnippets.MessageWindow` class, tray HWND/icon ID 1 and HKCU Run value `SmartSnippets`.
- Candidate production-overlap refusal, canonical/hard-link inspection and `.candidate-owner` lease are removed. As with legacy, process admission is the AppLifecycle single-instance key; profile access follows admission. There is no profile override. Core database/schema-v2/migration/backup code is unchanged; synthetic v1/v2 copy tests remain.
- Ported legacy ShellExecuteEx `runas` elevation, Win32 error/cancel outcomes and the five-second `--after-pid` child wait before instance/profile admission. Hidden main-window state preserves `--background`.
- Settings, tray and elevated-target popup footer now provide administrator restart. Recovery, tracked operations and dirty main/popup Save–Discard–Cancel checks precede UAC. Cancel/recovery failure/UAC failure retain the current host; successful launch alone commits teardown. The shared elevation interface has an additive asynchronous entry point with a compatibility default for existing implementations.
- Startup migration preserves the canonical same-directory `SmartSnippets.App.exe` rule, additionally normalizes recognized existing `SmartSnippets.exe` commands to the current executable, and idempotently removes `SmartSnippets.UnoCandidate019`. Unknown/malformed/non-string values remain untouched; an absent official value remains disabled. Failure feedback uses a tray balloon with an information-dialog fallback. Settings reads actual registration.
- Removed acceptance capability/session indirection; the daily `DesktopInsertionCoordinator` keeps single-flight, exact-target checks, clipboard ownership/recovery, nonmutating previews and success-only usage. Copy never credits usage. Popup Paste anyway is wired to the existing explicit-consent command, not an automatic loss fallback.
- Replaced two-head source/harness pins with official identity, real handler/VM/store, lifecycle, recovery, restart and startup behavior coverage. Remaining test names with `Candidate` are internal historical names, not a separate runtime identity.

## Removed paths and historical preservation

Removed `src/SmartSnippets.App`, all `prototypes/` (018 probes/targets, Windows insertion fixtures and Uno guardian), `Platforms/Windows/AcceptanceNative`, candidate capability/file-identity/evidence ledger, old acceptance tests/fixtures, legacy QA PowerShell scripts/libraries, `run_uno_candidate_*.py`, other `uno_candidate_*` harness tooling and its Python tests. Removed redundant `.github/workflows/uno-windows-candidate.yml`.

`legacy-winui-final` remains at **`793c90bd886d64abe44c05d4046d716bd3985404`**, preserving all removed reference code. Historical `docs/plan/` and `docs/research/` files were not edited. Operator-owned `AGENTS.md` and `docs/guides/002-acceptance-harness-safety.md` were neither edited nor staged by dn-03.

Retained icon tooling, repointed to Uno. Renamed the strict build script to `scripts/publish-uno-aot.ps1`, its non-launching validator to `scripts/aot_gate.py`, and retained 20 validator tests. Official output is `publish/SmartSnippets.exe` plus `e_sqlite3.dll`, other native dependencies and assets. CI/release now test/publish Uno using its pinned SDK/cwd; Core/Presentation remain Linux + Windows. Release version stamping and zip layout checks remain, including SQLite. No push or hosted CI run occurred.

## Final sequential pre-merge verification

All final checks used implementation commit `10fa6ef`; no source changes followed.

| Check | Result |
|---|---|
| Root Core tests, Release | 436 passed; 0 failed/skipped |
| Root Presentation tests, Release | 617 passed; 0 failed/skipped |
| Uno tests from `src/SmartSnippets.Uno`, Release + `ContinuousIntegrationBuild=true` | 152 passed; 0 failed/skipped |
| Remaining Python tests | 20 passed |
| `pwsh -NoProfile -File scripts/publish-uno-aot.ps1 -Version 0.0.0-cutover -BuildNumber 1` | Fresh native x64; strict authored/reference properties; zero warnings; native entry/SQLite checks passed |
| Version-resource inspection | Product `0.0.0-cutover+10fa6ef6dcc75cd87e3514fe681e770d0ef31802`; File `0.0.0.1` |

Artifact: `publish/SmartSnippets.exe`
SHA256: **`bb7bd7905123de77db2af8876c4c291a75d4ae6e01f59043eb3c5c73fa11d91c`**
Build token: `048da534654e4ea5a262a87c5220529a`. This version stamp is verification-only, not a release/tag.

**Execution boundaries:** no app launch, screen use, hotkey/input/clipboard operation or real SmartSnippets profile/Run-key access. The final suites use substituted registry boundaries. Caveat: the initial exploratory Presentation run included an existing integration test that created/read/deleted a disposable `HKCU\Software\SmartSnippets.Tests\Migration-<guid>` subkey. That pre-existing test was replaced with fake-boundary coverage before the final run; it never touched the Run key or real profile. This initial disposable-registry use is a deviation from the brief's no-registry instruction, not evidence of live startup validation.

## User first-run checks / risks

1. Exit **all** legacy/candidate/official processes. Retain the leader's backup; optionally make another closed-app profile backup. Copy/run the whole published directory, not just the exe.
2. Confirm real legacy snippets/folders/settings/usage open unchanged. The separate candidate profile is neither imported nor deleted; that is intentional under the user's legacy-data decision.
3. Check Start at login: a previously enabled recognized official entry should point to this installation; the candidate Run value should be gone. Previously disabled official startup stays disabled. Unknown registrations require repair via Settings.
4. Check hotkey → popup → exact-target paste, clipboard restoration, explicit Copy (usage unchanged), tray/background and previews. Recreate obsolete shortcuts/pins pointing to removed candidate/legacy filenames.
5. Exercise administrator restart from Settings/tray and an elevated-target footer: dirty Save/Discard/Cancel, UAC Cancel and successful restart; verify exactly one surviving process and correct foreground/background behavior.

Real-data startup, registry migration, actual UAC handoff and post-cutover daily flow were intentionally **not live-tested by dn-03**. The child retains legacy's bounded five-second wait. Artifacts remain unsigned. Oldest supported Windows runtime, macOS/mobile and hosted CI/release acceptance are not established here.
