# 004 — App shell, lifecycle, and settings

**Date:** 2026-10-27
**Status:** Placeholder
**Master plan:** [001-master-smart-snippets-v1.md](001-master-smart-snippets-v1.md)
**Depends on:** 002, 003

> Reservation and scope boundary only — not an implementation-ready or approved plan. It will be
> expanded with the full plan template after its dependencies are complete.

## Objective

Turn the prototype shell into the production background app: lifecycle, global hotkey, tray, and all settings except popup layout/sort.

## Scope

- Single instance with activation redirect (second launch opens main window).
- Tray icon: Open, Settings, Restart as administrator, Exit; closing main window keeps running; Exit unregisters hotkey and quits.
- Hotkey service: register/unregister, capture UI, conflict explanation, keep previous on failure, restore default (Ctrl+Shift+Space), persisted.
- Settings page: theme Light/Dark/System (live, follows system), text size Small/Medium/Large/Custom (12/14/18, custom 10–32) applied app-wide, start at login via HKCU `Run` (`--background`), failure messages.
- Restart as administrator (UAC `runas`, single-instance hand-off).
- Out: snippet/folder UI, popup content, insertion engine.

## Expected completion criteria

- AC4 (hotkey config), AC10 (restart as admin), AC11, AC12 pass their runbook steps on the AOT build; settings persist across restart (AC3).
