# Independent Plan Review — Plan 016 (Password-type snippets, request `pr-016a`)

**Branch reviewed:** `feature/016-password-snippets`
**Scope:** Tasks `T2`–`T8` and §§2–9 of `docs/plan/016-password-type-snippets.md` verified against `SmartSnippets.Core`, `SmartSnippets.Presentation`, `SmartSnippets.Uno`, and their test/build gates.

## Summary of Scope & API Verification

- **Scope alignment vs §2:** Overall scope matches the confirmed G1 brief well. Neither whole-DB encryption nor a custom multiline masked control is over-scoped, `PRAGMA secure_delete=ON` is an appropriate single-line defense-in-depth measure with honest caveats, and `InsertionService` / `ClipboardSafety.PublishProtectedText` already preserve exact paste and clipboard-marker behavior without contract changes.
- **CsWin32 availability in Uno head (`src/SmartSnippets.Uno`):** Verified against `packages.lock.json:23–32` (`Microsoft.Windows.CsWin32` `0.3.367` / `Microsoft.Windows.SDK.Win32Metadata` `71.0.37-preview`). `CryptProtectData`, `CryptUnprotectData`, `LocalFree`, `CRYPT_INTEGER_BLOB`, `HLOCAL`, and `CRYPTPROTECT_UI_FORBIDDEN` are all present in `Windows.Win32.winmd` (`AllowUnsafeBlocks=true` in `SmartSnippets.Uno.csproj:30`, `"allowMarshaling": false` in `NativeMethods.json:3`). None of the DPAPI symbols are in `src/SmartSnippets.Uno/NativeMethods.txt` yet (`GlobalFree` is present, `LocalFree` is not).
- **WinUI 3 `PasswordBox` on Uno Windows head:** Because `SmartSnippets.Uno.csproj:4,14,57` targets `net10.0-windows10.0.26100` with `DisableImplicitUnoPackages=true` and `Microsoft.WindowsAppSDK` (`1.8.260921001` / `Microsoft.WindowsAppSDK.WinUI` `1.8.260803003`), XAML compiles directly against real WinAppSDK `Microsoft.UI.Xaml.Controls.PasswordBox` (not Uno's managed control shim). `PasswordBox` supports `Password`, `PasswordChanged`, `PasswordRevealMode="Hidden"` (which keeps `RevealButton` collapsed in `DefaultPasswordBoxStyle`), `Header`, `FontSize`, `Paste`, and `AutomationProperties.Name` — **but lacks `IsSpellCheckEnabled` and `IsTextPredictionEnabled` (see Finding 3)**. Absence of momentary last-character reveal on the target Windows desktop runtime cannot be proved from metadata alone and rightly remains a live desktop gate in T4.

---

## Findings (T2–T8)

### F1 — Direct `UnoAppHost` wiring of `WindowsSecretProtector` breaks `SmartSnippets.Uno.Tests` compilation and `HostFixture` composition
- **Severity:** blocker
- **Task affected:** T2, T7, T8
- **Evidence:**
  - `docs/plan/016-password-type-snippets.md:62,76,126` (assumes legacy `AppHost.cs` directly constructs `WindowsSecretProtector` and wires `SnippetStore`)
  - `src/SmartSnippets.Uno/UnoAppHost.cs:13,67–68` (`internal sealed partial class UnoAppHost(HostBoundary boundary, ...)` constructs `new SnippetStore(factory, TimeProvider.System)`)
  - `src/SmartSnippets.Uno/HostBoundary.cs:35–51` & `src/SmartSnippets.Uno/WindowsHostBoundary.cs:14` (all OS/WinUI services enter `UnoAppHost` exclusively via `HostBoundary`)
  - `tests/SmartSnippets.Uno.Tests/SmartSnippets.Uno.Tests.csproj:2,9` (`<TargetFramework>net10.0</TargetFramework>`, compiles `UnoAppHost.cs` and `HostBoundary.cs` directly **without** `Microsoft.Windows.CsWin32` and **without** `Services/WindowsSecretProtector.cs`)
  - `tests/SmartSnippets.Uno.Tests/Composition/HostFixture.cs:11–20` (`HostFixture` instantiates `UnoAppHost` with `TestBoundary`)
  - `tests/SmartSnippets.Uno.Tests/Wiring/WiringAuditTests.cs:65–66` (explicitly forbids `GetEnvironmentVariable(` and `--profile` in `src/SmartSnippets.Uno` sources)
- **Problem:** Unlike the removed `SmartSnippets.App/AppHost.cs`, `UnoAppHost.cs` is linked directly into the portable `net10.0` `SmartSnippets.Uno.Tests` project, which has neither a `-windows` TFM nor `Microsoft.Windows.CsWin32`. Constructing `new WindowsSecretProtector()` inside `UnoAppHost.cs` fails to compile `SmartSnippets.Uno.Tests`. Furthermore, because `SnippetStore`'s default protector throws on passwords (§4.1), `HostFixture` / `TestBoundary` would reject all password operations in Uno flow and wiring tests unless `HostBoundary` supplies the protector.
- **Suggested plan change:** Update §4.1, T2, and T8 to route `ISecretProtector` through `HostBoundary` (`internal abstract ISecretProtector CreateSecretProtector();`): `WindowsHostBoundary.cs` returns `new WindowsSecretProtector()`, `UnoAppHost.cs:67` passes `boundary.CreateSecretProtector()` into `SnippetStore`, and `TestBoundary` (`tests/SmartSnippets.Uno.Tests/Composition/HostFixture.cs`) provides a deterministic test-only protector (with a failure toggle for negative tests). Add `HostBoundary.cs`, `WindowsHostBoundary.cs`, and `HostFixture.cs` to T2's file list.

---

### F2 — Adding `CheckBox` and `PasswordBox` to `SnippetEditor.xaml(.cs)` breaks Uno headless XAML compilation and `Actions.txt` action-manifest gate
- **Severity:** blocker
- **Task affected:** T4, T8
- **Evidence:**
  - `tests/SmartSnippets.Uno.Tests/SmartSnippets.Uno.Tests.csproj:13` (links `../../src/SmartSnippets.Uno/Controls/*.xaml.cs` directly into `SmartSnippets.Uno.Tests`)
  - `tests/SmartSnippets.Uno.Tests/Wiring/HeadlessXaml.cs:175–280` (defines headless stubs for `TextBox`, `ToggleSwitch`, `ComboBox`, `Button`, etc., **but has no `CheckBox`, `PasswordBox`, or `PasswordRevealMode`**)
  - `tests/SmartSnippets.Uno.Tests/Wiring/HeadlessFields.cs:37–41` (manually declares `SnippetEditor`'s XAML-named fields `SnippetTextBox` and `NewFolderBox`)
  - `tests/SmartSnippets.Uno.Tests/Wiring/ActionConsumerTests.cs:31–44,91–94` (`BoundProperties` allowlist omits `Password`, `PasswordRevealMode`, `IsReadOnly`, `Header`; `EveryAppAction_ReachesRegisteredConsumerOrTruthfulRefusal` asserts exact equality between every XAML action/unlisted binding — including element ordinal `Controls/SnippetEditor.xaml|<index>|...` — and `Actions.txt`)
  - `tests/SmartSnippets.Uno.Tests/Wiring/Actions.txt:3–19` & `tests/SmartSnippets.Uno.Tests/Wiring/EditorActionConsumers.cs:13–73`
  - `tests/SmartSnippets.Presentation.Tests/SmartSnippets.Presentation.Tests.csproj:15–16` (`UiContracts` copies `*.xaml` but omits `Controls/SnippetEditor.xaml.cs`)
- **Problem:** T4 modifies `Controls/SnippetEditor.xaml(.cs)` without accounting for the Uno headless wiring suite added in Plan 019:
  1. `SmartSnippets.Uno.Tests` will not compile until `CheckBox`, `PasswordBox`, `PasswordRevealMode` (in `HeadlessXaml.cs`) and the new `x:Name` fields on `SnippetEditor` (in `HeadlessFields.cs`) exist.
  2. Inserting elements into `Controls/SnippetEditor.xaml` shifts XML element indices for all existing `Controls/SnippetEditor.xaml|...` lines in `Actions.txt`, and any new checkbox/password event or unlisted `{x:Bind}` property fails `ActionConsumerTests` unless registered in `Actions.txt`, `BoundProperties`, and `EditorActionConsumers.cs`.
  3. T4 names `PasswordEditorSourceTests.cs` without specifying its test project; placing it in `SmartSnippets.Presentation.Tests` cannot read `SnippetEditor.xaml.cs` unless `.csproj` is updated, whereas `SmartSnippets.Uno.Tests` reads `src/SmartSnippets.Uno` directly.
- **Suggested plan change:** Add `tests/SmartSnippets.Uno.Tests/Wiring/{HeadlessXaml.cs,HeadlessFields.cs,Actions.txt,ActionConsumerTests.cs,EditorActionConsumers.cs}` to T4's file list, add headless action-audit cases for Password checkbox toggling and `PasswordBox.PasswordChanged`, and place `PasswordEditorSourceTests.cs` in `tests/SmartSnippets.Uno.Tests/Wiring/` (or link `SnippetEditor.xaml.cs` into `Presentation.Tests`).

---

### F3 — WinUI 3 `PasswordBox` has neither `IsSpellCheckEnabled` nor `IsTextPredictionEnabled` (§4.3 XAML compile failure)
- **Severity:** blocker
- **Task affected:** T4
- **Evidence:**
  - `docs/plan/016-password-type-snippets.md:90` (§4.3: *"Use a native WinUI PasswordBox for single-line password editing, `PasswordRevealMode=Hidden`, no separate eye button, predictable accessibility name and disabled text prediction/spellcheck."*)
  - `src/SmartSnippets.Uno/packages.lock.json:54` (`Microsoft.WindowsAppSDK.WinUI` `1.8.260803003` — `Microsoft.WinUI.dll` `Microsoft.UI.Xaml.Controls.PasswordBox` derives from `Control`, not `TextBox`; its properties are `CanPasteClipboardContent`, `Description`, `Header`, `HeaderTemplate`, `InputScope`, `IsPasswordRevealButtonEnabled`, `MaxLength`, `Password`, `PasswordChar`, `PasswordRevealMode`, `PlaceholderText`, `PreventKeyboardDisplayOnProgrammaticFocus`, `SelectionFlyout`, `SelectionHighlightColor`, `TextReadingOrder`)
- **Problem:** Setting `IsSpellCheckEnabled="False"` or `IsTextPredictionEnabled="False"` on `<PasswordBox>` in `SnippetEditor.xaml` will fail Windows XAML compilation because neither property exists on `Microsoft.UI.Xaml.Controls.PasswordBox` (native `PasswordBox` inherently disables prediction and spellcheck). Also note that in WinUI 3, `PasswordRevealMode="Hidden"` supersedes `IsPasswordRevealButtonEnabled`; setting both is redundant/quirky.
- **Suggested plan change:** Amend §4.3 and T4 to specify `PasswordRevealMode="Hidden"` on `PasswordBox` without `IsSpellCheckEnabled`/`IsTextPredictionEnabled` (apply `IsSpellCheckEnabled="False" ` only to `SnippetTextBox` and any read-only `TextBox` used for the multiline mask fallback). Retain T4's runtime verification gate on Windows for eye-button absence, UIA `IsPassword`, and no momentary last-character reveal.

---

### F4 — `UnoAppHost` / `WindowsHostBoundary` discards startup `StoreException.Message`, breaking recovery-message requirement (§5 & Runbook #10)
- **Severity:** major
- **Task affected:** T2, T8
- **Evidence:**
  - `docs/plan/016-password-type-snippets.md:62,77,111,203` (§3: `AppHost.cs:115–132`; §5: *"unreadable encrypted rows block startup with a recovery explanation"*; Runbook #10: *"corrupted copy fails startup with safe recovery message"*)
  - `src/SmartSnippets.Uno/UnoAppHost.cs:67–68,122–129` (`store.Load()` inside `try`; `catch (Exception ex)` calls parameterless `boundary.ReportStartupFailure();`)
  - `src/SmartSnippets.Uno/HostBoundary.cs:49` (`internal abstract void ReportStartupFailure();`)
  - `src/SmartSnippets.Uno/WindowsHostBoundary.cs:135–140` (`MessageBox(default, "Smart Snippets could not start.", "Smart Snippets", MB_OK)`)
  - `tests/SmartSnippets.Uno.Tests/Wiring/UiBindingAuditTests.cs:48–49`
- **Problem:** After the Uno cutover, startup failures no longer display `StoreException.Message` or `MigrationException.Message`; `WindowsHostBoundary.ReportStartupFailure()` always shows `"Smart Snippets could not start."`. Even if T2 maps unreadable/tampered DPAPI envelopes to a dedicated `StoreFailure` with a safe recovery message, the user never sees it and Runbook #10 fails.
- **Suggested plan change:** Update T2 to pass a sanitized startup error message (`ex is StoreException or MigrationException ? ex.Message : "Smart Snippets could not start."`, never raw exception text) through `HostBoundary.ReportStartupFailure(...)` / `WindowsHostBoundary.ReportStartupFailure(...)`, and update `HostFixture.cs` and `UiBindingAuditTests.cs`.

---

### F5 — Shared `SnippetEditor` control-swap hazards: `Mode=OneWay` plaintext leak, focus-guarded `_rebinding` wiping `ViewModel.Text`, non-disposable lifecycle, `FocusText()` on multiline passwords, and silent multiline paste truncation in `PasswordBox`
- **Severity:** major
- **Task affected:** T4
- **Evidence:**
  - `src/SmartSnippets.Uno/Controls/SnippetEditor.xaml:22–32` (`Text="{x:Bind ViewModel.Text, Mode=OneWay}" TextChanged="OnTextInput"`)
  - `src/SmartSnippets.Uno/Controls/SnippetEditor.xaml.cs:16,51–83,103` (`SnippetEditor : UserControl` is not `IDisposable`; `_rebinding` is set only in `ViewModel { set; }`; `IsUserInput` returns `true` whenever `!_rebinding && _viewModel is not null && control.FocusState != FocusState.Unfocused`; `FocusText()` calls `SnippetTextBox.Focus(...)`)
  - `src/SmartSnippets.Uno/Popup/PopupWindow.xaml:40` & `src/SmartSnippets.Uno/Popup/PopupWindow.xaml.cs:169,428` (`EditorControl.FocusText()`, `EditorControl.ViewModel = null`)
  - `src/SmartSnippets.Uno/Measurement/MeasurementEditorBindings.cs:5–9` (`ViewModel = null; Bindings.StopTracking();`)
- **Problem:** Five concrete defects will hit T4 unless specified in the plan:
  1. **`Mode=OneWay` `x:Bind` populates collapsed `SnippetTextBox`:** `SnippetEditor.xaml:30` uses `Text="{x:Bind ViewModel.Text, Mode=OneWay}"` (not two-way), which still pushes password plaintext into `SnippetTextBox.Text` while `Collapsed`. Neither `SnippetTextBox.Text` nor `PasswordBox.Password` may be XAML-bound to `ViewModel.Text`.
  2. **Programmatic clear wipes `ViewModel.Text`:** `_rebinding` is `false` during `ViewModel.PropertyChanged` callbacks. If `SnippetTextBox` or `PasswordBox` still has focus (`FocusState != Unfocused`) when `IsPassword` changes and code-behind clears the outgoing control (`SnippetTextBox.Text = ""` or `PasswordBox.Password = ""`), `OnTextInput`/`OnPasswordInput` sees `IsUserInput == true` and overwrites `_viewModel.Text = ""`.
  3. **`SnippetEditor` is never disposed:** §4.3 says *"Clear detached control values during editor disposal/rebinding"*, but `SnippetEditor` is a persistent `UserControl` in `SnippetsPage` and `PopupWindow`. Clearing must trigger on `ViewModel` setter (including `null` from `PopupWindow.Closed` and `MeasurementEditorBindings`) and `Unloaded`.
  4. **`FocusText()` fails on multiline password snippets:** When opening a multiline password snippet in the popup editor (`PopupWindow.xaml.cs:428`), both `SnippetTextBox` and `PasswordBox` are `Collapsed`; `Focus(Programmatic)` returns `false`, stranding keyboard focus outside `EditorControl` and breaking `Escape` / `Tab`.
  5. **Pasting multiline text into `PasswordBox` silently truncates secrets:** If a user checks **Password** *first* on a new/single-line snippet and pastes a multiline secret (or a password with a trailing `\r\n`) into single-line `PasswordBox`, the native password control silently truncates at the first newline while masked so the user cannot see data was lost.
- **Suggested plan change:** Specify in §4.3 / T4: (1) remove `Text="{x:Bind ViewModel.Text, ...}"` from XAML and drive both inputs imperatively in code-behind; (2) wrap all programmatic `Text`/`Password` assignments in a `_syncingInput` guard; (3) clear both controls on `ViewModel` change/`null` and `Unloaded`; (4) route `FocusText()` to a visible focusable control in all three states (ordinary `SnippetTextBox`, single-line `PasswordBox`, or multiline read-only mask/checkbox); and (5) handle `PasswordBox.Paste` (or verify/guard multiline clipboard input) so multiline pastes are either preserved via the multiline masked state or explicitly refused rather than silently truncated.

---

### F6 — `NativeExport` pre-cap filtering breaks `UnreadableList` tests, dual UI/Core filtering makes excluded count ambiguous, and Export UI copy still claims "all snippets"
- **Severity:** major
- **Task affected:** T6
- **Evidence:**
  - `src/SmartSnippets.Core/Porting/NativeExport.cs:14–21` (checks `snippets.Count > MaxImportEntries` and `snippets.Count + folders.Count > MaxImportEntries` before enumerating)
  - `tests/SmartSnippets.Core.Tests/Porting/NativeExportTests.cs:22,85,95,128` (`UnreadableList` throws `InvalidOperationException("Must refuse before enumeration")` on `this[index]` and `GetEnumerator()`; `UnreadableFolders` at `:85` throws on `folders` enumeration)
  - `src/SmartSnippets.Presentation/ViewModels/SnippetPortingViewModel.cs:100,104–123` & `PortingWorkers.cs:7–14`
  - `tests/SmartSnippets.Presentation.Tests/PortingMessagesTests.cs:16` & `SnippetPortingViewModelTests.cs:82` (assert exact `UiText.ExportComplete(1, 1, "output.json")` wording)
  - `src/SmartSnippets.Uno/Pages/SnippetsPage.xaml:255` & `src/SmartSnippets.Presentation/Text/UiText.cs:155` (`"Export all snippets and folders to a .json file"`)
- **Problem:**
  1. Filtering `snippets` before `MaxImportEntries` caps in `NativeExport.Write` (§4.4, T6) requires enumerating `snippets`, which immediately breaks `NativeExportTests.Export_TooManySnippets_RefusedWithoutSerializing` (`:22`) and `Export_CombinedLimitPlusOne_Refused` (`:95`). Also, `folders.Count > MaxImportEntries` must be checked *before* filtering `snippets` so `Export_TooManyEmptyFolders_RefusedBeforeSerialization` (`:85`, `UnreadableFolders`) continues to refuse without enumerating `folders`.
  2. When `SnippetPortingViewModel` filters password rows on the UI thread before `_workers.ExportAsync`, `NativeExport.Write` receives 0 password rows and returns `ExcludedPasswordCount = 0`. T6 does not specify how `SnippetPortingViewModel` combines `uiExcluded` with `result.ExcludedPasswordCount` when tests stub `PortingWorkers.ExportAsync` (`SnippetPortingViewModelTests.cs:84,91`).
  3. T6 does not state whether `excluded == 0` preserves existing `UiText.ExportComplete` wording (`PortingMessagesTests.cs:16`), and overlooks `SnippetsPage.xaml:255` (hardcoded tooltip, not bound to `UiText`) and `UiText.ExportButtonTooltip` (`:155`), both claiming *"Export all snippets..."*.
- **Suggested plan change:** In T6: (a) check `folders.Count > MaxImportEntries` first in `NativeExport.Write`, then single-pass filter `snippets`, and explicitly update `NativeExportTests.cs:22,95` from `UnreadableList` to concrete ordinary collections plus a `MaxImportEntries` ordinary + password test; (b) specify that `SnippetPortingViewModel` partitions `_store.Snippets` once on the UI thread into `(ordinary, uiExcluded)`, passes `ordinary` to `_workers.ExportAsync`, and reports `uiExcluded + result.ExcludedPasswordCount` (requiring stubbed worker tests to match); (c) keep `UiText.ExportComplete` unchanged when `excluded == 0` and append/format the excluded-password count when `excluded > 0`; (d) update `UiText.ExportButtonTooltip` and `SnippetsPage.xaml:255`.

---

### F7 — DPAPI implementation gaps: `Encoding.Unicode` mangles lone surrogates, 0-byte `fixed` pointer fails Win32 `CryptProtectData`, `CRYPTPROTECT_UI_FORBIDDEN` missing in `NativeMethods.txt`, `AppLog.SafeErrorDetail` strips unlisted tokens, and Windows DPAPI probe is outside CI
- **Severity:** major
- **Task affected:** T2, T7
- **Evidence:**
  - `docs/plan/016-password-type-snippets.md:74–77,125–130`
  - `src/SmartSnippets.Uno/NativeMethods.txt:1–83` & `Windows.Win32.winmd` (`CryptProtectData` `dwFlags` is `uint`, so `CRYPTPROTECT_UI_FORBIDDEN` is a constant on `Apis`, not auto-emitted as an enum)
  - `src/SmartSnippets.Core/Logging/AppLog.cs:53–79` (`SafeErrorDetail` strict `key=value` allowlist)
  - `src/SmartSnippets.Core/Store/SnippetStore.cs:316–327` (`Guarded<T>` exception filter and `AppLog.Error("store_write_failed", null, ...)`)
  - `.github/workflows/ci.yml:39–46` (runs `Core.Tests`, `Presentation.Tests`, `Uno.Tests`, `scripts/tests`, `publish-uno-aot.ps1`)
- **Problem:**
  1. **UTF-16 lone surrogates:** §4.1 requires *"lossless UTF-16 code-unit serialization with explicit byte order so Unicode, CR/LF, whitespace and unusual code units do not silently change."* `Encoding.Unicode` (`UnicodeEncoding`) replaces lone surrogates (`\uD800`–`\uDFFF`, already tested in `NativeExportTests.cs:21`) with `U+FFFD` or throws! Raw UTF-16LE `ushort` code-unit serialization (`BinaryPrimitives` / `MemoryMarshal.AsBytes` on LE `win-x64`) is required, and `Unprotect` must reject odd decrypted byte counts (`(cbData & 1) != 0`) and empty envelopes after `dpapi:v1:`.
  2. **0-byte `fixed` pointer:** T2 requires low-level repository/protector entry points to stay protected. In C#, `fixed (byte* p = emptySpan)` produces `p == null`, causing Win32 `CryptProtectData` to fail with `ERROR_INVALID_PARAMETER` even when `cbData == 0`; a non-null dummy byte pointer (`byte dummy = 0;`) must be used when `cbData == 0`.
  3. **Buffer clearing & `NativeMethods.txt`:** Temporary managed `byte[]`/`char[]` buffers and native `blobOut.pbData` (before `PInvoke.LocalFree((HLOCAL)blobOut.pbData)`) should be wiped via `CryptographicOperations.ZeroMemory` so Native AOT ILC cannot dead-store-eliminate clears. `NativeMethods.txt` must list all four names: `CryptProtectData`, `CryptUnprotectData`, `LocalFree`, and `CRYPTPROTECT_UI_FORBIDDEN`.
  4. **`AppLog.SafeErrorDetail` allowlist:** `Core/Logging/AppLog.cs` is omitted from T2's file list. Any new diagnostic token (e.g., `reason=...` or `win32=...`) passed to `AppLog.Error` in `SnippetStore.Guarded` is silently dropped by `SafeErrorDetail` unless allowlisted in `AppLog.cs` (`SecretProtectionException` itself passes the `EndsWith("Exception")` check at `AppLog.cs:64`).
  5. **DPAPI probe not executed in CI:** T2 specifies a standalone "headless Windows verification script", which `.github/workflows/ci.yml` never runs. Because `SmartSnippets.Uno.Tests` already runs on `windows-latest` in CI (`ci.yml:39`), running the real `WindowsSecretProtector` round-trip/tamper tests inside `SmartSnippets.Uno.Tests` (by adding `Microsoft.Windows.CsWin32` to `SmartSnippets.Uno.Tests.csproj` and linking `WindowsSecretProtector.cs` + `NativeMethods.txt`) or adding the probe script to `ci.yml` ensures automated Windows enforcement.
- **Suggested plan change:** Add these five concrete requirements and `Core/Logging/AppLog.cs` (plus `SmartSnippets.Uno.Tests.csproj` or `.github/workflows/ci.yml`) to §4.1 and T2.

---

### F8 — `SnippetEditorViewModel.Suggestion` / `OnIsLabelCustomChanged` leak password-derived suggestions, and unchecking Password with a blank label leaves an invalid custom-label zombie state
- **Severity:** major
- **Task affected:** T3
- **Evidence:**
  - `src/SmartSnippets.Presentation/ViewModels/SnippetEditorViewModel.cs:183` (`public string Suggestion => LabelSuggester.Suggest(Text, TagList.Parse(TagsInput));`)
  - `src/SmartSnippets.Presentation/ViewModels/SnippetEditorViewModel.cs:430–448` (`OnIsLabelCustomChanged(bool value)` calls `SetLabel(Suggestion)` when `!value` without checking `IsPassword`)
  - `src/SmartSnippets.Presentation/ViewModels/SnippetEditorViewModel.cs:483–526` (`Load` and `ApplySaved`)
  - `src/SmartSnippets.Core/Domain/LabelSuggester.cs:19–22` (`ResolveLabel` converts ordinary drafts with `IsLabelCustom == true && string.IsNullOrWhiteSpace(Label)` to `(Suggest(draft.Text, tags), false)`)
- **Problem:**
  1. Disabling the XAML `ToggleSwitch` in T4 does not guard Presentation: if `Suggestion` is read or `IsLabelCustom = false` is set on `SnippetEditorViewModel` while `IsPassword == true`, `LabelSuggester.Suggest(Text, ...)` still derives a label from secret `Text`. Also, both `Load` and `ApplySaved` must assign `IsPassword` *before* `Text`, `TagsInput`, `IsLabelCustom`, and `Label` (`ApplySaved` at `:508` currently omits `Text` and `IsPassword`).
  2. §4.2 states *"Returning to ordinary mode keeps custom label mode until the user explicitly requests automatic labeling."* When a user checks **Password** on an auto-labeled or new snippet (clearing `Label` to `""` and forcing `IsLabelCustom = true`) and immediately unchecks **Password**, keeping `IsLabelCustom = true` with `Label = ""` creates an otherwise-unreachable state in ordinary mode: the UI displays `Custom label = On` with an empty label and placeholder `"Label"`, yet on Save `LabelSuggester.ResolveLabel` (`LabelSuggester.cs:20`) silently saves an auto-generated label with `IsLabelCustom = false` — and toggling Password on→off leaves `IsDirty == true` on an untouched auto-labeled snippet.
- **Suggested plan change:** In §4.2 / T3: (a) make `Suggestion` return `string.Empty` when `IsPassword` is `true` and prevent `IsLabelCustom` from becoming `false` while `IsPassword` is `true`; (b) set `IsPassword` first in both `Load` and `ApplySaved`; and (c) when `IsPassword` changes from `true` to `false`, keep `IsLabelCustom = true` if `Label` is non-whitespace, but restore automatic mode (`IsLabelCustom = false`, `SetLabel(Suggestion)`) if `Label` is blank/whitespace so UI state matches `LabelSuggester.ResolveLabel` and accidental check→uncheck returns cleanly to `IsDirty == false`.

---

### F9 — `PopupViewModel` deferred rebuild window leaves stale plaintext `ContentText` readable by `ShowKeyboardPreview` while popup is visible
- **Severity:** minor
- **Task affected:** T5
- **Evidence:**
  - `src/SmartSnippets.Presentation/ViewModels/PopupViewModel.cs:212–232` (`OnStoreChanged` marks `_cacheDirty = true` and defers `RebuildSearch(); Refresh(id);` via `_schedule` / `Dispatcher.Post`)
  - `src/SmartSnippets.Uno/Popup/PopupWindow.xaml.cs:481–491,518,737` (`ShowKeyboardPreview()` reads `SelectedSnippet.ContentText` without calling `ViewModel.EnsureCurrent()`; `ApplyQuery()` returns early when `SearchBox.Text == ViewModel.Query` without flushing `_cacheDirty`)
- **Problem:** If both `MainWindow` and `PopupWindow` are open (e.g., Settings "Preview global dialog") and an ordinary snippet is saved as a password in `MainWindow`, `SnippetListViewModel` masks synchronously, whereas `PopupViewModel` waits for its posted callback. During that window, an already-open popup hover/keyboard preview remains visible with plaintext and `ShowKeyboardPreview()` can read dirty `SelectedSnippet.ContentText`.
- **Suggested plan change:** In T5, call `ViewModel.EnsureCurrent()` at the entry of `PopupWindow.ShowKeyboardPreview()` and cancel any open popup `HoverDetailsSurface` immediately when `_cacheDirty` becomes `true` (or flush synchronously when popup is visible).

---

### F10 — Stale file/line citations in §3 / T8 and optional harness wording in `docs/guides/002`
- **Severity:** minor
- **Task affected:** T2, T5, T8
- **Evidence:**
  - `docs/plan/016-password-type-snippets.md:56–62,126,169` vs actual Uno codebase:
    - `Core/Data/SqliteConnectionFactory.cs:51–53` → actual `PRAGMA` lines are `:43–44`.
    - `SnippetEditor.xaml.cs:38–61,86` → actual `ViewModel` setter is `:51–75`, `FocusText()` is `:103`.
    - `App/Popup/PopupWindow.xaml.cs:426` → actual `ShowKeyboard(selected.ContentText, ...)` is `:489`.
    - `SnippetsPageViewModel.cs:142–148` → actual `CopyRow` is `:151–161`.
    - `AppHost.cs` in §4.1, T2, T8 → `UnoAppHost.cs`, `HostBoundary.cs`, `WindowsHostBoundary.cs`.
  - `docs/guides/002-acceptance-harness-safety.md:3–6` (notes the native-acceptance harness is optional since 2026-11-01, whereas `AGENTS.md` governs everyday live checks via pc-manager screen slot, bottom-left monitor, and isolated profile).
- **Suggested plan change:** Refresh stale file names and line numbers in §3, T2, and T8 when revising the plan, and align T8's live-check instructions with `AGENTS.md` and `docs/guides/002-acceptance-harness-safety.md:3–6`.

---

PLAN READY: no
<<PLAN-REVIEW-DONE>>
