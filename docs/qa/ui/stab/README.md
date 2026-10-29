# stab-screen — safety refusal (2026-10-28)

Status: BLOCKED before launch; no screenshots captured.

Authorized display geometry was present: DISPLAY9 at (-1920, 1506), 1920×1080; work area 1920×1032.
Preflight found an unowned SmartSnippets.App (PID 55600) running from `D:\sources\demo\smartsnippets\publish\SmartSnippets.App.exe`. It remained running after a bounded 60-second wait. Per the grant/harness safety policy, the candidate was NOT launched and no profile isolation was attempted. The process was not stopped or interacted with.

- PASS: unowned-instance launch refusal.
- PENDING: popup cold/repeat dark/light first-frame capture and flash detection.
- PENDING: activation/taskbar checks.
- PENDING: synthetic 10k first/repeated popup timing logs.
- PENDING: Snippets/Settings screenshots.
- PENDING-USER: folder field behaviour (real interaction required).

No windows captured or manipulated, no cursor/keyboard/posted input, no clipboard or registry/profile mutation, no builds. No screenshot or desktop acceptance PASS is claimed. Retry requires the user to exit the existing instance and confirm a remaining/new screen grant.
