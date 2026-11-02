# Code Review Round 2: Plan 016 Fix Batch (`rv2-016`)

- **Repository:** `D:\sources\demo\smartsnippets`
- **Branch / Commit reviewed:** `feature/016-password-snippets` (`54a41c8` / `d40d335..a08fcf1`)
- **Reference:** `docs/review/015-plan-016-code-review-round1.md` (F1–F8), `docs/plan/016-password-type-snippets.md` (§2 AC1–AC7, §4, §4.8 A1–A9)

---

## 1. Round-1 Finding Verification (`F1..F8`)

- **F1: Verified fixed (with new side-effect defect R2-F1 below).** `SnippetEditorViewModel.Load` and `ApplySaved` clear `Text` before switching `IsPassword` and bracket updates with `IsLoading`; `EditorActionConsumers` (`ViewModel.DiscardCommand`) verifies via `SnippetTextBox.Writes` and `SnippetPasswordBox.Writes` that neither `"PRIVATE-DRAFT"` nor `"PRIVATE-SAVED"` is written to `SnippetTextBox` and neither `"original"` nor `"ordinary draft"` is written to `SnippetPasswordBox`. However, the way `IsLoading` and `Text = string.Empty` were wired introduces **R2-F1** on ordinary saves and multiline password saves/discards.
- **F2: Verified fixed.** `Load` (`SnippetEditorViewModel.cs:537,541`) and `ApplySaved` (`SnippetEditorViewModel.cs:566,570`) establish `IsLabelCustom = true` before setting `IsPassword = true` and normalize `IsLabelCustom = IsPassword || ...`. Verified by `PasswordEditorTests.LoadCannotEmitPasswordSuggestion` and `PasswordEditorTests.StoredPasswordWithAutomaticFlagKeepsManualLabel`.
- **F3: Verified fixed.** `SnippetEditor.OnPasswordPaste` (`SnippetEditor.xaml.cs:113,132`) increments `_inputGeneration` on entry (`var generation = ++_inputGeneration;`) and calls `SyncInputs()` after `editor.Text = text;`. `SyncInputs()` also runs on `IsLoading` transitions (`Save`/`Discard`/`Load`). Verified across `"text"`, `"mode"`, `"unload"`, `"discard"`, `"save"`, and `"overlap"` in `EditorActionConsumers.cs:108–138`.
- **F4: Verified fixed (with minor side-effect R2-F2 in `CanUseRowActions`).** `EnsureCurrent()` barriers are present in `PopupViewModel.MoveSelection`, `InsertSelectedAsync`, `ActivateAsync`, and `PopupWindow.OnPreviewKeyDown`/`OnKeyDown`, and tested in `PopupCacheStabilityTests.SelectionEntryPointsFlushDirtySearch`. Placing `EnsureCurrent()` inside the predicate `CanUseRowActions` introduces a minor re-entrancy side effect when a row context menu is open (**R2-F2**).
- **F5: Verified fixed.** `PasswordProtectionTests.FailedPasswordLoadStopsStartupWithoutRewritingRows` (`PasswordProtectionTests.cs:76–84`) restarts `UnoAppHost` with `boundary.Protector.Fail = false` after the failed startup and asserts that the single password snippet row is intact (`IsPassword == true`, `IsLabelCustom == true`, `Label == "manual"`, `Text == "PRIVATE-BODY"`).
- **F6: Verified fixed.** `AppLogPrivacyTests.PasswordFailuresNeverLogSecretOrCiphertext` (`AppLogPrivacyTests.cs:27–28`) and `PasswordSnippetFlowTests.FailureLogsContainOnlySafeTokens` (`PasswordSnippetFlowTests.cs:178–181`) assert `op=update_snippet`, `op=load`, and `error=SecretProtectionException`.
- **F7: Verified fixed.** `PasswordEditorSourceTests.NoRevealButtonOrPlaintextPasswordBinding` (`PasswordEditorSourceTests.cs:22–28`) asserts `Visibility` bindings on all three input controls and checks `ViewModel.Text` without requiring a trailing comma; `EditorActionConsumers` (`EditorActionConsumers.cs:53–55,80–83,96–138`) exercises `FocusText()`'s fallback to `PasswordCheckBox`, `OnPasswordTextInput`'s reset of `SnippetPasswordBox.Password`, and all `OnPasswordPaste` invalidation paths.
- **F8: Verified fixed.** `PasswordProjectionTests` (`PasswordProjectionTests.cs:18–51`) exercises `SnippetListViewModel` for both projection masking and secret-body search exclusion, and asserts both `Assert.NotSame(before, after)` and `Assert.NotEqual(before.GetHashCode(), after.GetHashCode())`.

---

## 2. Round-2 Findings

### R2-F1 — `D` (Major): `Load` and `ApplySaved` unconditionally clear `Text = string.Empty` and `SyncInputs` treats `IsLoading` as `null`, wiping/rewriting `SnippetTextBox.Text` on every ordinary save and transiently flipping `CanEditPassword`/`IsMultilinePassword` on multiline passwords

- **Classification:** `D` (genuine new code defect introduced by the F1 fix in `54a41c8`)
- **Severity:** **Major** (violates AC6 *"Ordinary snippet workflows remain unchanged"* and causes focus/visibility churn on multiline password metadata saves in AC1/AC3)
- **Evidence:**
  - `src/SmartSnippets.Presentation/ViewModels/SnippetEditorViewModel.cs:532–549`:
    ```csharp
        _loading = true;
        OnPropertyChanged(nameof(IsLoading));
        try
        {
            Text = string.Empty;
            if (snippet?.IsPassword == true) IsLabelCustom = true;
            IsPassword = snippet?.IsPassword ?? false;
            Text = snippet?.Text ?? string.Empty;
            ...
        }
        finally
        {
            _loading = false;
            OnPropertyChanged(nameof(IsLoading));
        }
    ```
  - `src/SmartSnippets.Presentation/ViewModels/SnippetEditorViewModel.cs:561–577`:
    ```csharp
    private void ApplySaved(Snippet saved)
    {
        _current = saved;
        _loading = true;
        OnPropertyChanged(nameof(IsLoading));
        try
        {
            Text = string.Empty;
            if (saved.IsPassword) IsLabelCustom = true;
            IsPassword = saved.IsPassword;
            Text = saved.Text;
    ```
  - `src/SmartSnippets.Uno/Controls/SnippetEditor.xaml.cs:158–176`:
    ```csharp
            var editor = _inputUnloaded || _viewModel?.IsLoading == true ? null : _viewModel;
            // Clear the inactive input before assigning the active one.
            if (editor?.IsPassword == true)
            {
                SnippetTextBox.Text = string.Empty;
                var password = editor.CanEditPassword ? editor.Text : string.Empty;
                if (SnippetPasswordBox.Password != password) SnippetPasswordBox.Password = password;
            }
            else
            {
                SnippetPasswordBox.Password = string.Empty;
                var text = editor?.Text ?? string.Empty;
                if (SnippetTextBox.Text != text) SnippetTextBox.Text = text;
            }
    ```
- **Concrete failure scenario:**
  1. **Ordinary snippet save (`Ctrl+S` in `SnippetsPage`) wipes and rewrites `SnippetTextBox.Text`:**
     When a user edits an ordinary snippet in `SnippetTextBox` and presses `Ctrl+S`, `SaveCore()` persists the draft and calls `ApplySaved(saved)`, where `saved.Text == Text` and `saved.IsPassword == IsPassword`. Because `ApplySaved` fires `OnPropertyChanged(nameof(IsLoading))` with `_loading = true` (and sets `Text = string.Empty`), `SyncInputs()` treats `editor` as `null` and sets `SnippetTextBox.Text = ""`. When `_loading` becomes `false` in `finally`, `SyncInputs()` sets `SnippetTextBox.Text = saved.Text`. In WinUI, clearing `SnippetTextBox.Text` to `""` and reassigning it while the user is editing in `SnippetTextBox` resets `SelectionStart`/`SelectionLength` to `0` (jumping the caret to the start of the snippet) and disrupts the `TextBox` undo stack on every save.
  2. **Multiline password metadata save or discard transiently flips `CanEditPassword` and `IsMultilinePassword`:**
     When a user edits metadata (label, folder, or tags) on a multiline password (`IsPassword == true`, `HasMultilineText == true`, `CanEditPassword == false`, `IsMultilinePassword == true`) and saves (`ApplySaved`) or discards (`ResetToSaved` -> `Load`), setting `Text = string.Empty` while `IsPassword == true` changes `HasMultilineText` to `false`, `CanEditPassword` to `true`, and `IsMultilinePassword` to `false`. The `[ObservableProperty]` setter on `_text` synchronously fires `PropertyChanged` for `CanEditPassword` and `IsMultilinePassword`. Because the XAML `x:Bind` expressions on `SnippetPasswordBox.Visibility` and `MultilinePasswordMask.Visibility` (`SnippetEditor.xaml:49,55`) do not check `IsLoading`, `MultilinePasswordMask` collapses (dropping keyboard focus if focused) and `SnippetPasswordBox` becomes visible before `Text = saved.Text` flips them back two lines later.
- **Suggested fix:**
  1. In `SnippetEditor.OnViewModelPropertyChanged` (`SnippetEditor.xaml.cs:274`), ignore intermediate notifications while `_viewModel?.IsLoading == true`:
     ```csharp
     if (_viewModel?.IsLoading == true) return;
     if (e.PropertyName is nameof(SnippetEditorViewModel.Text) or nameof(SnippetEditorViewModel.IsPassword)
         or nameof(SnippetEditorViewModel.IsLoading))
         SyncInputs();
     ```
     and in `SyncInputs()` (`SnippetEditor.xaml.cs:161`), revert `editor` to `var editor = _inputUnloaded ? null : _viewModel;`. Then `SyncInputs()` is not called during `_loading == true` at all, and runs once when `_loading` becomes `false` in `finally`—where `if (SnippetTextBox.Text != text)` leaves an unchanged `SnippetTextBox.Text` untouched while still bumping `_inputGeneration` and clearing `PasswordInputHint.Text`.
  2. In `SnippetEditorViewModel.Load`, clear `Text = string.Empty;` only when leaving password mode (`if (IsPassword && snippet?.IsPassword != true) Text = string.Empty;`), and in `ApplySaved`, remove `Text = string.Empty;` and `Text = saved.Text;` (or only assign `if (IsPassword && !saved.IsPassword) Text = string.Empty;`), and remove the `OnPropertyChanged(nameof(IsLoading))` call on entry (keeping only the `OnPropertyChanged(nameof(IsLoading))` in `finally` when `_loading` becomes `false`).

---

### R2-F2 — `D` (Minor): `PopupViewModel.CanUseRowActions` calls state-mutating `EnsureCurrent()`, causing synchronous cache rebuild and recursive `PropertyChanged` re-entry during `OnStoreChanged` when a row menu is open

- **Classification:** `D` (genuine new code defect introduced by the F4 fix in `54a41c8`)
- **Severity:** **Minor**
- **Evidence:**
  - `src/SmartSnippets.Presentation/ViewModels/PopupViewModel.cs:240–245`:
    ```csharp
    public bool CanUseRowActions(PopupItemViewModel? item)
    {
        EnsureCurrent();
        return !IsInserting && !IsEditorOpen && !IsConfirmingClipboardLoss && item is { Kind: PopupItemKind.Snippet }
            && Items.Contains(item) && _store.Snippets.Any(s => s.Snippet.Id == item.SnippetId);
    }
    ```
  - `src/SmartSnippets.Presentation/ViewModels/PopupViewModel.cs:218–235`:
    ```csharp
    private void OnStoreChanged(object? sender, StoreChangedEventArgs e)
    {
        _cacheDirty = true;
        // Synchronous invalidation closes even pending previews before deferred rebuilding.
        OnPropertyChanged(nameof(IsCacheDirty));
        if (_rebuildQueued) return;
        _rebuildQueued = true;
        _schedule(() => ...);
    }
    ```
  - `src/SmartSnippets.Uno/Popup/PopupWindow.xaml.cs:596–601`:
    ```csharp
        void RefreshActions(object? sender, System.ComponentModel.PropertyChangedEventArgs args)
        {
            copy.IsEnabled = ViewModel.CanCopyRow(item);
            insert.IsEnabled = CanInsert();
        }
        ViewModel.PropertyChanged += RefreshActions;
    ```
- **Concrete failure scenario:**
  When the popup's right-click context menu (`ShowRowMenu`) is open, `RefreshActions` is subscribed to `ViewModel.PropertyChanged`. If the store raises `Changed`, `OnStoreChanged` sets `_cacheDirty = true` and fires `OnPropertyChanged(nameof(IsCacheDirty))`. `RefreshActions` runs synchronously inside `OnPropertyChanged(nameof(IsCacheDirty))` and calls `ViewModel.CanCopyRow(item)` -> `CanUseRowActions(item)` -> `EnsureCurrent()`, which synchronously executes `RebuildSearch(); Refresh(id);` on the store event stack, mutating `Items` and `SelectedIndex`, recursively re-entering `RefreshActions` across five nested `PropertyChanged` events (`Items`, `SelectedIndex`, `CountText`, `IsEmpty`, `CanCopyFailedText`), and then returning to `OnStoreChanged` which still queues a redundant `_schedule` callback.
- **Suggested fix:**
  Keep `CanUseRowActions` side-effect-free (do not call `EnsureCurrent()` inside `CanUseRowActions`) and instead call `EnsureCurrent()` at the action/menu entry points (`PopupWindow.OnRowRightTapped` / `ShowRowMenu`, `PopupViewModel.CopyRow`, `EditRow`, `DeleteRowAsync`, `InsertSelectedAsync`, `ActivateAsync`).

---

## 3. Readiness Verdict

**IS THIS BRANCH READY TO MERGE? No — 1 Major defect (R2-F1) and 1 Minor defect (R2-F2) from the Round-1 fix batch should be resolved first.**

### Blockers / Must-Fix Before Merge
- **R2-F1 (`D`, Major):** Stop wiping and rewriting `SnippetTextBox.Text` on ordinary `SaveAsync()` (`ApplySaved`) and stop transiently flipping `CanEditPassword`/`IsMultilinePassword` via `Text = string.Empty` during `Load`/`ApplySaved` on multiline passwords.

### Improvements
- **R2-F2 (`D`, Minor):** Move `EnsureCurrent()` out of the `CanUseRowActions` predicate into the row-action/menu entry points so `PropertyChanged(nameof(IsCacheDirty))` cannot trigger synchronous rebuild and recursive `PropertyChanged` re-entry via `ShowRowMenu`'s `RefreshActions`.

