# InkSpoke hotkey reference — Plan 018

Read-only source inspection at InkSpoke commit `21059ae424ccadab570d33fa34b3f6f12a00aa47`. Relevant inspected files are clean at that commit. No source was copied, no package was added, and no build/test/hook/application was launched in that repository. This is implementation evidence, not independent runtime verification of its deployments.

## Actual implementation, not just dependency names

Paths below are relative to the reference repository, not this repository.

| Platform/component | Verified source | Approach |
|---|---|---|
| macOS | `src/InkSpoke.Platform.macOS/Services/MacOSGlobalHotkeyService.cs:1–398` | SharpHook `SimpleGlobalHook`, key-down/up tracking, exact modifier matching, matched-event suppression, hardware-flags resampling, supervised lifecycle |
| Windows | `src/InkSpoke.Platform.Windows/Services/WindowsGlobalHotkeyService.cs:90–129`; `Win32KeyboardHook.cs:48–112`; `Win32KeyboardHookNative.cs:30–68` | Current default is a custom native `WH_KEYBOARD_LL` hook, not SharpHook's native Windows hook; SharpHook event/key types remain in use |
| Linux | `src/InkSpoke.Platform.Linux/Services/LinuxGlobalHotkeyService.cs:1–105,266–299` | SharpHook X11 hook plus the common supervisor; source does not establish Wayland support |
| Common lifecycle | `src/InkSpoke.Abstractions/SystemIntegration/GlobalHookSupervisor.cs:1–336` | Observe run-task completion/failure; serialize teardown/start; bounded restart budget; explicit unlock/resume rebuild; report failed teardown |
| Contract/tests | `src/InkSpoke.Abstractions/SystemIntegration/IGlobalHotkeyService.cs`; `tests/InkSpoke.Tests/Platform/CrossPlatformHotkeySupervisionTests.cs` | Platform-neutral registration interface and injectable native hook factory; unit tests never install a real global hook |
| Package | Platform project files | SharpHook 5.3.7 |

The Windows replacement is particularly relevant: its implementation documents native module-table installation failures, avoids blocking the hook callback, gates startup, and ignores deliveries from retired hook instances. Do not assume one backend is used identically across all platforms merely because all three projects reference SharpHook.

## Reliability patterns worth adapting

- Track the native run-loop task instead of a never-reset `hook != null` latch or unobserved `Task.Run`.
- Rebuild on resume/unlock, with serialized teardown/start and bounded retries. Keep errors actionable instead of claiming registration succeeded because nothing threw.
- Reject late callbacks from an old hook generation; clear only application-owned modifier tracking after retiring the old hook.
- Keep native callbacks short and non-blocking. Marshal application work to the UI dispatcher; never touch the single-threaded snippet store from a hook thread.
- Test lifecycle and modifier recovery through injected native boundaries, then separately prove actual native registration/callback delivery during the guarded desktop slot.

These are reference patterns, not automatic adoption. In particular, the reference supervisor can start a replacement after teardown timeout/failure; Smart Snippets must consider duplicate delivery/suppression risk before adopting that policy. Its registration methods also return true after requesting supervisor startup; our registration success must reflect verified native readiness/failure, not merely an accepted request.

## Native AOT is still a separate gate

`src/InkSpoke.UI/InkSpoke.UI.csproj:21–33` explicitly describes release builds as ReadyToRun, with strict Native AOT opt-in via `PublishAot=true`. Working InkSpoke releases therefore do not independently prove that SharpHook 5.3.7, native asset resolution, callbacks and packaging meet Smart Snippets' unsuppressed Native AOT requirements. Do not weaken our release gate or switch frameworks based on this reference.

Before adding a dependency: check license/native distribution obligations, restore/publish the exact package in an isolated probe, verify arm64 native assets and zero AOT/trim warnings, and exercise registration/failure/shutdown on the actual host.

## Suggested direction for Smart Snippets

1. Preserve the existing Windows `RegisterHotKey`/`WM_HOTKEY` path (`src/SmartSnippets.App/Services/HotkeyService.cs`), including truthful conflict/error results and register-new-before-release-old replacement. Our single activation chord does not require all of InkSpoke's push-to-talk/key-up/overlay-arrow interception machinery.
2. Compare a macOS registered-shortcut implementation with SharpHook/event-tap fallback, using the same portable registration policy and explicit native capability results. Choose only after the native AOT and permission/dispatch probe passes; this inspection does not select a library yet.
3. Carry supervision, sleep/resume recovery, short callbacks, generation guards and injected tests into whichever backend is selected. Route popup focus through `PopupShowCoordinator` and keep store/cache work on the UI thread.
4. Linux remains outside the approved release targets; use its source as a lifecycle reference, not an expansion of scope.

InkSpoke's stale **application modifier-tracker** repair is not a repair for the current Mac host guard: our preview queries the OS key-state table directly and reports a held non-modifier. Resetting an application tracker must not be used to bypass that external-input safety condition.
