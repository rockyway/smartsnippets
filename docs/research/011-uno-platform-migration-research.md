# 011 — Uno Platform migration research

**Date:** 2026-10-30
**Status:** Initial research — documentation and source inspection, not runtime validation
**Baseline:** `develop` / `3e8d3cf`
**Decision owner:** [017 master plan](../plan/017-master-uno-cross-platform-migration.md)

## 1. Conclusion and evidence limits

Uno is a plausible migration path because the application already separates domain/persistence and view-models from WinUI. Official documentation describes a Windows-native target and Skia Desktop/AppKit on macOS, plus mobile targets. This establishes a framework direction, **not** proof that this application's XAML, native adapters, SQLite packaging, Native AOT, popup overflow or keyboard extensions work.

No Uno packages have been installed, no probe has run, and no macOS/mobile device, signing identity or acceptance hardware was inspected during planning. An independent plan reviewer could not live-fetch the external links; the listed platform claims are research leads, not independently corroborated runtime evidence. No exact “latest stable” Uno version is asserted. Plan 018 must pin a mutually supported stable toolchain and reproduce results from fresh artifacts. Plan 018 includes a bounded early strict-AOT/mobile-keyboard-host risk check before production Uno adoption; plan 022 repeats and deepens the mobile gate on real devices before broad mobile implementation.

## 2. Official source register

Sources were located/reviewed through web grounding on 2026-10-30. The claim column is deliberately narrower than a project-level support guarantee.

| Source | Documentation finding | Consequence / unproven part |
|---|---|---|
| [Uno solution structure](https://platform.uno/docs/articles/uno-app-solution-structure.html) | Uno supports shared application code with platform-specific target structure, including Windows-native and desktop targets | Propose retaining native WinAppSDK rendering on Windows; verify exact template/TFMs and unpackaged AOT compatibility |
| [Skia Desktop](https://platform.uno/docs/articles/features/using-skia-desktop.html) | Desktop host uses Uno.Sdk and platform host builders; .NET 10 desktop documentation uses `net10.0-desktop` | macOS is not a recompiled Win32 executable; build native adapters and test lifecycle |
| [Skia on macOS](https://platform.uno/docs/articles/features/using-skia-macos.html) and [macOS internals](https://platform.uno/docs/articles/uno-development/uno-internals-macos.html) | Current macOS approach is Skia rendering with an AppKit shell, not the retired native-control macOS backend | Shared WinUI API surface is useful; do not infer native NSView embedding or AppKit type bindings from it |
| [Native AOT](https://platform.uno/docs/articles/features/native-aot.html) | Uno documents Native AOT configuration/support with restrictions | Each actual app/extension and dependency graph must publish and run without trim/AOT defects; ordinary mobile AOT is not automatically equivalent |
| [Embedding native elements](https://platform.uno/docs/articles/features/using-skia-hosting-native-controls.html) | Native control hosting support differs by backend; macOS limitations are documented | Menu-bar/accessibility interop is a separate seam; do not assume arbitrary native controls can be hosted in shared XAML |
| [Publishing on macOS](https://platform.uno/docs/articles/uno-publishing-desktop-macos.html) | Publishing app bundles requires macOS; documented flow supports explicit runtime identifiers, app packaging and code-signing identity | Use `osx-arm64` only. Need Apple Silicon machine, bundle identity, native dependency inspection, signing and notarization evidence; a locally ad-hoc-signed build is not a distributed release |
| [Apple AX trust check](https://developer.apple.com/documentation/applicationservices/axisprocesstrusted()) and [CGEvent posting](https://developer.apple.com/documentation/coregraphics/cgevent/post(tap:)) | Native APIs exist for accessibility trust and synthetic event posting | Exact permission, target identity, focus restoration and dispatch observability require a signed-host prototype. Event posting is not proof the target consumed text |
| [Apple custom keyboard guide](https://developer.apple.com/library/archive/documentation/General/Conceptual/ExtensibilityPG/CustomKeyboard.html) | Keyboard extensions use a text proxy; secure and phone-pad fields use the system keyboard; host apps can opt out; open access affects shared-container/network capabilities | Mobile cannot promise desktop-style ubiquitous insertion. Determine the minimal sharing/Full Access setup with current toolchains; use the archived guide alongside current API docs |
| [Apple keyboard text interactions](https://developer.apple.com/documentation/uikit/handling-text-interactions-in-custom-keyboards) and [open access](https://developer.apple.com/documentation/uikit/configuring-open-access-for-a-custom-keyboard) | Current keyboard APIs and access configuration are separate from ordinary application UI | A native extension entry point and explicit privacy/enablement UX are necessary; Uno XAML keyboard hosting remains unproven |
| [Android input method](https://developer.android.com/develop/ui/views/touch-and-input/creating-input-method) | IMEs use `InputMethodService`, manifest service metadata and the editor's `InputConnection` | Implement a native service and user enablement; do not port desktop global hooks or rely on AccessibilityService injection |

## 3. Reuse and coupling found in this repository

| Area | Reuse | Required change / gate |
|---|---|---|
| Core | .NET 10, SQLite SQL/migrations, domain validation/query, source-generated JSON | SQLite native assets per RID; app-private roots; Win32 shortcut model is not portable |
| Presentation | .NET 10 view-models and pure popup/hover/safety policies | Native target handles, monitor handles, CRLF promise, focus-path enum and clipboard format semantics need adaptation |
| WinUI App | Existing compiled XAML, palette resources and controls are migration candidates | WindowsAppSDK/WinRT/native calls cannot run on macOS/mobile; compile and render representative controls early |
| Desktop insertion | Single-flight, final target gate, uncertain dispatch and usage accounting | Preserve Windows safety; prove macOS target-specific activation and pasteboard snapshot/ownership policy, including failure without mutation |
| Lifecycle | Explicit composition and UI dispatcher; popup readiness/recovery gates | Replace native startup/tray/instance/activation APIs. No assumption that host lifecycle hooks auto-start services |
| File portability | Existing versioned JSON parser/planner/transactional import | Path-based picker/write contracts need sandbox/provider adaptation; do not claim atomic provider replacement without proof |
| Mobile integration | Domain and management policy can be shared | Native extension/IME host, local sharing and receipts, process death, 10k-library keyboard memory/package budgets and strict Native AOT are unproven |

Source anchors: `src/SmartSnippets.Core/SmartSnippets.Core.csproj:1–9`; `src/SmartSnippets.Presentation/SmartSnippets.Presentation.csproj:1–12`; `src/SmartSnippets.App/SmartSnippets.App.csproj:1–33`; `src/SmartSnippets.Core/Settings/HotkeyGesture.cs:5–47`; `src/SmartSnippets.Presentation/Services/InsertionContracts.cs:3–29`; `InsertionBoundary.cs:5–58`; `InsertionService.cs:18–40,86–126`; `ISnippetFileService.cs:3–19`; `src/SmartSnippets.App/AppHost.cs:98–253,322–336`; `src/SmartSnippets.App/Popup/PopupWindow.xaml.cs:259–344`; `src/SmartSnippets.App/Controls/HoverDetailsSurface.xaml.cs:190–265`.

## 4. Architecture recommendation

- Keep Core and Presentation rather than rewrite persistence/view-models.
- Start with an isolated Uno desktop probe; retain the existing Windows executable unchanged until a separately accepted cutover.
- Prefer Windows-native WinAppSDK plus Apple Silicon Skia/AppKit. Unified Skia-on-Windows is an alternative only if measured constraints justify its regression cost.
- Treat shortcuts, target identity, permissions, clipboard, startup, menu-bar/tray, file providers and activation as native adapters with explicit capabilities.
- Keep desktop insertion and mobile keyboard dispatch as different mechanisms sharing only valid policy (content fidelity, deduplication, privacy and usage semantics).
- Validate iOS/Android management Native AOT **and native keyboard artifact feasibility** before committing to a detailed mobile design. A platform-supported keyboard API does not establish that .NET/Uno can package its host with the strict release requirements.

## 5. Blocking unknowns and planned evidence

| Unknown | Gate |
|---|---|
| Exact stable Uno/.NET/WinAppSDK combination, minimum OS support and compiled control coverage | 018 T0/T1/T3 |
| Actual native executable plus SQLite/framework dependencies under AOT | 018 T2/T6; repeat for mobile in 022 |
| macOS original-window identity/focus, permission denial/revocation and paste dispatch uncertainty | 018 T4/T5 native synthetic-target run |
| macOS all-format clipboard snapshot/ownership, lazy providers and history behavior | 018 T4/T5; any inability to meet the confirmed safety requirement is a blocker, not a hidden limitation |
| External hover, multiple windows, recovery generations and non-activating previews | 018 T3/T5, full parity in 020/021 |
| Direct-distribution signing/notarization and privacy identity stability | 018 documents local evidence limits; 021 final signed release gate |
| iOS extension Native AOT/package support, Android IME hosting and local sharing | 018 early risk check before 019; 022 physical-device probe before 023/024 |
| Mobile receipt durability, provider publication guarantees and app/keyboard lifecycle | 022 measurement, then explicit design in 023/024 |

## 6. Confirmed boundaries

Phase 1: Windows x64 + Apple Silicon macOS, full desktop parity, Native AOT. Phase 2: iOS + Android management and snippet keyboards. Local-only storage; file-based transfer; no Intel, Windows ARM64, Linux/web, cloud sync/accounts or draft password implementation. Plan 016's DPAPI proposal is not shipped functionality and is not silently translated to a keychain/keystore feature.
