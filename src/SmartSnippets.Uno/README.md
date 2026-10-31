# Plan 019 Windows Uno candidate — closed-gate checkpoint

This is an isolated implementation checkpoint, **not completed Plan 019, native parity, release readiness, or completion of Plan 018**. The legacy app remains unchanged by this candidate commit. Shared privacy/clipboard changes are separate prerequisites.

## Boundaries

- Assembly/executable: `SmartSnippets.UnoCandidate`; application ID: `org.smartsnippets.unocandidate019`; instance/profile: `SmartSnippets.UnoCandidate019`.
- The default profile is explicitly rooted under LocalApplicationData with that candidate name. Ownership is locked before logging/migration/store access. Production paths, canonical production aliases, reparse paths and hard-linked profile files are refused. There is no general profile CLI option.
- Clipboard, insertion, global shortcuts, startup registration and administrator restart are unavailable. No native clipboard, paste, external target capture, hotkey registration, registry registration or elevation adapter is composed. `CandidateNativeGate` always refuses; even clipboard-loss consent cannot reach an inner insertion service. There is **no runtime authorization/unlock mechanism**.
- Tray preview goes directly to target-free preview without external enumeration or title/process capture. Both tooltip states are candidate-branded; the tray owns a distinct icon GUID. Shell/preview showing does not explicitly transfer foreground.
- Windows UI rendering remains native WinAppSDK. Uno SDK tooling is pinned to 6.7.30 with implicit Uno packages disabled; no Skia/Mac graph or runtime fallback is adopted.

## Implemented and checked

The actual `UnoAppHost` composes the real SQLite store, settings repository and shared management/popup VMs. Tests substitute only external window/tray/message/dialog/file/exit/dispatcher boundaries; they compile the same host source, not a parallel composition graph.

Ported compiled views include management, folders, settings, editor, dialogs, JSON pickers, popup toolbar/rows/editor/footer/sort menu, hover details, palette/theme and text sizing. Native entry operations are visibly unavailable. Preview selection closes without Copy, paste or usage. The popup uses the shared `DeferredPopupHost` and `PopupShowCoordinator`, with recoverable window forwarding and cancellation of stale reveal/warmup callbacks. Exit guards main and popup drafts, suspends recovery, closes interaction admission and waits for tracked management/porting continuations before releasing profile ownership.

Headless flow coverage includes real-store CRUD/folders/delete-to-Unfiled, dirty Save–Discard–Cancel/validation failure, settings persistence/refusals, JSON cancel/malformed/oversized input and actual atomic-publication refusal, offline synthetic v1/v2 fixture compatibility, corrupt/newer startup refusal, profile ownership/hard-link rejection, redirect serialization, popup recreation, preview/Copy refusal, porting drain and popup draft preservation. Message-class ownership cleanup is tested through substituted native calls. Static wiring/binding checks are supplements, not native event evidence.

## Reproduce without launching the app

Run sequentially from **this directory**, not the repository root, so CLI SDK selection uses `global.json` (10.0.403, roll-forward disabled):

```text
dotnet --version
dotnet test ../../tests/SmartSnippets.Core.Tests -c Release
dotnet test ../../tests/SmartSnippets.Presentation.Tests -c Release
dotnet test ../../tests/SmartSnippets.Uno.Tests -c Release -p:ContinuousIntegrationBuild=true
dotnet clean SmartSnippets.Uno.csproj -c Release -r win-x64 -p:ContinuousIntegrationBuild=true
dotnet restore SmartSnippets.Uno.csproj -r win-x64 -p:Configuration=Release -p:ContinuousIntegrationBuild=true
dotnet publish SmartSnippets.Uno.csproj -c Release -r win-x64 --no-restore -p:ContinuousIntegrationBuild=true -o bin/publish-uno-candidate
```

Use a freshly absent publish destination. The local verification artifact is under ignored `bin/publish-uno-candidate`, never legacy `publish/`. Parent integration owns the additive publish/CI gate and its output policy. Lock settings are candidate-csproj-scoped, not global CLI properties. Central package pins and root SDK policy are unchanged. Explicit final SDK imports clear the inherited ILLink IL2121 suppression: authored effective `NoWarn` is empty, including publishing context; no CLI override is needed to mask source settings.

Final checkpoint evidence: **436 Core, 615 Presentation, 37 candidate tests passed**. A fresh clean Release/win-x64 Native AOT publish completed with zero warnings, without AOT/trim/NoWarn CLI overrides. `Evidence/` contains sanitized effective properties, source digests, native PE/import/hash checks and resolved-package lock hash. Raw compiler logs are private outside the repository. No candidate executable or GUI was launched.

## Still pending / not claimed

- T0/T2–T5 are a checked closed-gate checkpoint, not full task/AC closure. The candidate has no authorized native insertion/Copy/shortcut capability, inner native adapter, or live authorization harness. Positive/expired authorization, exact-target dispatch, single-flight native insertion and clipboard restore/guardian settlement remain unimplemented/unaccepted candidate paths.
- T3/T4 compiled bindings and host tests do not prove actual Windows command delivery, dialog focus, picker behavior, menus, rendering, hover/pointer handoff, DPI/work-area placement, close/recreate callbacks or every native asynchronous shutdown race. Reparse/canonical-alias defenses require independent review and broader filesystem fixtures; current executable file-alias test exercises hard links.
- Process-level single-instance redirect, actual repeated native message-class cleanup, coexistence with legacy, nonactivation/foreground preservation, bottom-left visible-window placement, palette/text-size rendering and native lifecycle remain **NOT RUN**. There has been no authorized screen session.
- T6 integration CI/artifact gate, T7 runbook/native acceptance, T8 independent final wiring/privacy/evidence audit and unchanged legacy AOT gate are parent/reviewer work. This worktree's artifact inspection is not a substitute for those gates.
- Oldest supported Windows runtime is NOT RUN. macOS/mobile feasibility and all existing Plan 018 obligations remain open with their original owners.

Do not enable native mutations because compilation or these tests pass. Any future native capability requires separate concrete boundary/recovery review and explicit operator authorization under the acceptance-harness safety guide.
