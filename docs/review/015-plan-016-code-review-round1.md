# Code Review Round 1: Plan 016 Password-Type Snippets (`rv1-016`)

- **Repository:** `D:\sources\demo\smartsnippets`
- **Branch / Diff reviewed:** `feature/016-password-snippets` (`b9ccc33..d40d335`, T1–T6 merged; T7 in progress)
- **Reference:** `AGENTS.md`, `docs/plan/016-password-type-snippets.md` (§2 AC1–AC7, §§4.1–4.7, §4.8 A1–A9 authoritative)

---

## 1. Enumeration of Readers in `src/`

### 1.1 `Snippet.Text` (and `SnippetDraft.Text` / `SnippetEditorViewModel.Text` / `ImportedSnippetRow.Text`)

| Location | Reader | Display surface? | Assessment |
|---|---|---|---|
| `src/SmartSnippets.Core/Data/SnippetRepository.cs:85` | `InsertRow`: `isPassword ? SecretEnvelope.Encode(text, _protector) : text` | No (SQLite persistence) | Safe: encrypts before parameter binding when `isPassword` is true. |
| `src/SmartSnippets.Core/Data/SnippetRepository.cs:96` | `InsertRow`: constructs in-memory `Snippet` with original `text` | No (domain model) | Safe: callers retain decrypted domain text in memory per §4.1. |
| `src/SmartSnippets.Core/Data/SnippetRepository.cs:120,129` | `InsertImportedMany`: binds `row.Text` and constructs `Snippet` (`is_password` defaults to `0`) | No (SQLite import) | Safe: imported rows are ordinary snippets (`IsPassword = false`). |
| `src/SmartSnippets.Core/Data/SnippetRepository.cs:155` | `Update`: `isPassword ? SecretEnvelope.Encode(text, _protector) : text` | No (SQLite persistence) | Safe: encrypts before parameter binding in the same transaction as `is_password`. |
| `src/SmartSnippets.Core/Data/SnippetRepository.cs:186` | `Read`: `reader.GetInt64(10) != 0 ? SecretEnvelope.Decode(...) : reader.GetString(1)` | No (SQLite read boundary) | Safe: decodes only when `is_password != 0`; fails closed on invalid envelopes. |
| `src/SmartSnippets.Core/Domain/LabelSuggester.cs:23` | `ResolveLabel`: `Suggest(draft.Text, tags)` | No (label resolution) | Safe: guarded by `draft.IsPassword ? (draft.Label, true) : ...` on line 20. |
| `src/SmartSnippets.Core/Store/SnippetStore.cs:75` | `ImportSnippets`: copies `PlannedSnippet.Text` into `ImportedSnippetRow` | No (import pipeline) | Safe: imports are ordinary snippets only. |
| `src/SmartSnippets.Core/Store/SnippetStore.cs:99,124,132` | `CreateSnippet` / `UpdateSnippet`: passes `draft.Text` to repository and in-memory `Snippet` | No (store write) | Safe: repository encodes when `draft.IsPassword` is true; commits before publishing. |
| `src/SmartSnippets.Core/Store/SnippetStore.cs:353` | `Prepare`: `SnippetValidator.Validate(draft.Text, label)` | No (validation) | Safe: checks only `string.IsNullOrWhiteSpace(text)`. |
| `src/SmartSnippets.Core/Query/SnippetSearch.cs:17` | `SnippetSearch` constructor: `if (!s.IsPassword) fields.Add(s.Text);` | No (search index) | Safe: password body is excluded from search indexing. |
| `src/SmartSnippets.Core/Porting/ImportPlanner.cs:41` | `Plan`: `var text = LineEndings.ToCrLf(snippet.Text);` | No (in-memory import dedupe) | Safe: intentional per §4.4 (dedupes against in-memory library without reclassifying or exporting). |
| `src/SmartSnippets.Core/Porting/NativeExport.cs:36,65` | `Write` / `StringPayloadExceedsLimit`: reads `v.Snippet.Text` / `snippet.Text` | No (JSON export) | Safe: password rows are filtered out at lines 18–25 before caps, sizing, DTOs, and serialization. |
| `src/SmartSnippets.Presentation/Text/SnippetDisplayPolicy.cs:9` | `Content(Snippet snippet)`: `snippet.IsPassword ? PasswordMask : snippet.Text` | **Yes** (central display policy) | Safe: returns fixed 8-bullet mask `"••••••••"` when `IsPassword` is true. |
| `src/SmartSnippets.Presentation/Services/InsertionService.cs:40` | `InsertAsync`: `var text = LineEndings.ToCrLf(snippet.Text);` | No (paste into target) | Safe: intentional per AC6 (inserts original text, restores clipboard). |
| `src/SmartSnippets.Presentation/ViewModels/PopupViewModel.cs:246,386` | `CopyRow` / `CopyFailedText`: `_clipboard.TrySetText(snippet.Text)` | No (explicit clipboard copy) | Safe: intentional per §4.4 / AC6. |
| `src/SmartSnippets.Presentation/ViewModels/SnippetsPageViewModel.cs:155` | `CopyRow`: `_clipboard.TrySetText(snippet.Text)` | No (explicit clipboard copy) | Safe: intentional per §4.4 / AC6. |
| `src/SmartSnippets.Presentation/ViewModels/SnippetEditorViewModel.cs:50` | `HasMultilineText`: `Text.IndexOfAny(['\r', '\n']) >= 0` | No (editor mode state) | Safe: boolean check only. |
| `src/SmartSnippets.Presentation/ViewModels/SnippetEditorViewModel.cs:197` | `Suggestion`: `IsPassword ? string.Empty : LabelSuggester.Suggest(Text, ...)` | **Yes** (feeds `LabelPlaceholder` / auto `Label`) | Safe: returns `string.Empty` whenever `IsPassword` is true. |
| `src/SmartSnippets.Presentation/ViewModels/SnippetEditorViewModel.cs:233,518,533,575` | `SaveCore`, `RecomputeDirty`, `Load`, `CaptureBaseline`: reads/sets `Text` | No (VM draft/baseline state) | **Defect in `Load` ordering — see F1.** |
| `src/SmartSnippets.Presentation/ViewModels/SnippetEditorViewModel.cs:348` | `CopyText`: `_clipboard!.TrySetText(Text)` | No (explicit save-failure copy) | Safe: intentional per §3 / §4.4 / AC6. |
| `src/SmartSnippets.Uno/Controls/SnippetEditor.xaml.cs:88,105,116,123,131` | `OnTextInput`, `OnPasswordTextInput`, `OnPasswordPaste`: syncs user input to/from `_viewModel.Text` | **Yes** (editor input controls) | **Has paste generation/hint defects — see F3.** |
| `src/SmartSnippets.Uno/Controls/SnippetEditor.xaml.cs:165,171` | `SyncInputs`: assigns `editor.Text` to `SnippetPasswordBox.Password` (when `IsPassword && CanEditPassword`) or `SnippetTextBox.Text` (when `!IsPassword`) | **Yes** (editor input controls) | **Receives transient password plaintext on `Load` `true -> false` transition — see F1.** |

### 1.2 `ContentText`

| Location | Reader | Display surface? | Assessment |
|---|---|---|---|
| `src/SmartSnippets.Presentation/ViewModels/SnippetRowViewModel.cs:28,30,42` | `Equals`, `GetHashCode`, `From`: populated via `SnippetDisplayPolicy.Content(view.Snippet)` | **Yes** (main list row projection) | Safe: masked before projection construction. |
| `src/SmartSnippets.Presentation/ViewModels/PopupViewModel.cs:190` | `BuildCache`: `PopupItemViewModel.ContentText = SnippetDisplayPolicy.Content(v.Snippet)` | **Yes** (popup row projection) | Safe: masked before projection construction. |
| `src/SmartSnippets.Uno/Pages/SnippetsPage.xaml:235` | `SnippetRowDetails ContentText="{x:Bind ContentText}"` | **Yes** (main list row details) | Safe: receives masked `SnippetRowViewModel.ContentText`. |
| `src/SmartSnippets.Uno/Controls/PopupRow.xaml:9` | `SnippetRowDetails ContentText="{x:Bind Item.ContentText}"` | **Yes** (popup list row details) | Safe: receives masked `PopupItemViewModel.ContentText`. |
| `src/SmartSnippets.Uno/Controls/SnippetRowDetails.xaml.cs:60` | `OnTargetEntered`: passes `ContentText` to `HoverDetailsSurface.Request` when hovering `LabelText` | **Yes** (pointer hover preview) | Safe: `ContentText` is already masked (`"••••••••"`). |
| `src/SmartSnippets.Uno/Popup/PopupWindow.xaml.cs:490` | `ShowKeyboardPreview`: `_keyboardSurface.ShowKeyboard(selected.ContentText, TextSize.FontSize)` | **Yes** (popup keyboard preview) | Safe: calls `ViewModel.EnsureCurrent()` first and passes masked `selected.ContentText`. |

### 1.3 `Preview`

| Location | Reader | Display surface? | Assessment |
|---|---|---|---|
| `src/SmartSnippets.Presentation/ViewModels/SnippetRowViewModel.cs:9,38` | `SnippetRowViewModel.Preview`: populated via `PreviewOf(SnippetDisplayPolicy.Content(view.Snippet))` | **Yes** (main row projection property) | Safe: masked before calling `PreviewOf`. (Not currently bound in XAML; `SnippetRowViewModel.ToString()` omits `Preview`.) |
| `src/SmartSnippets.Presentation/ViewModels/PopupItemViewModel.cs:7` | `PopupItemViewModel.Preview`: populated with `string.Empty` in `PopupViewModel.BuildCache` (`PopupViewModel.cs:187`) | **Yes** (popup row projection property) | Safe: always `string.Empty`; `PopupItemViewModel.ToString()` omits `Preview`. |

### 1.4 `Suggestion` (and `LabelSuggester.Suggest`)

| Location | Reader | Display surface? | Assessment |
|---|---|---|---|
| `src/SmartSnippets.Core/Domain/LabelSuggester.cs:9,23` | `Suggest(text, tags)` called from `ResolveLabel` | No (store label resolution) | Safe: bypassed when `draft.IsPassword` is true. |
| `src/SmartSnippets.Core/Porting/ImportPlanner.cs:63` | `LabelSuggester.Suggest(candidate.Text, tags)` | No (ordinary import label) | Safe: only ordinary snippets are imported. |
| `src/SmartSnippets.Presentation/ViewModels/SnippetEditorViewModel.cs:191` | `LabelPlaceholder`: `IsPassword || IsLabelCustom ? "Label" : $"Auto: {Suggestion}"` | **Yes** (`PlaceholderText` on label `TextBox` in `SnippetEditor.xaml:80`) | Safe: guarded by `IsPassword` in both `LabelPlaceholder` and `Suggestion`. |
| `src/SmartSnippets.Presentation/ViewModels/SnippetEditorViewModel.cs:197` | `Suggestion`: `IsPassword ? string.Empty : LabelSuggester.Suggest(Text, TagList.Parse(TagsInput))` | **Yes** (editor label suggestion) | Safe: returns `string.Empty` when `IsPassword` is true. |
| `src/SmartSnippets.Presentation/ViewModels/SnippetEditorViewModel.cs:397,437,466,495,536` | `OnIsPasswordChanged`, `OnLabelChanged`, `OnIsLabelCustomChanged`, `OnInputChanged`, `Load`: `SetLabel(Suggestion)` | **Yes** (editor `Label` text box) | Safe when `IsPassword` is true (`Suggestion` is `""`); see **F2** for `Load` when `snippet.IsLabelCustom == false`. |

---

## 2. Findings

### F1 — `D` (Blocker): `SnippetEditorViewModel.Load` sets `IsPassword = false` before updating `Text`, pushing discarded password plaintext into `SnippetTextBox.Text`

- **Classification:** `D` (genuine new code defect)
- **Severity:** **Blocker** (violates AC3, §4.3, and §4.8 A5: collapsed/inactive plaintext `TextBox` must never receive password text)
- **Evidence:**
  - `src/SmartSnippets.Presentation/ViewModels/SnippetEditorViewModel.cs:525–536`:
    ```csharp
    private void Load(Snippet? snippet)
    {
        _current = snippet;
        _labelBeforeAuto = null;
        _loading = true;
        try
        {
            IsPassword = snippet?.IsPassword ?? false;
            Text = snippet?.Text ?? string.Empty;
    ```
  - `src/SmartSnippets.Uno/Controls/SnippetEditor.xaml.cs:272–275`:
    ```csharp
    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(SnippetEditorViewModel.Text) or nameof(SnippetEditorViewModel.IsPassword))
            SyncInputs();
    ```
  - `src/SmartSnippets.Uno/Controls/SnippetEditor.xaml.cs:162–173`:
    ```csharp
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
  1. A user opens a new snippet (or an existing ordinary snippet), checks **Password** (`IsPassword = true`), and types `"MySecretPassword"` into `SnippetPasswordBox`. Because `IsPassword` is `true`, `"MySecretPassword"` has never been placed into `SnippetTextBox.Text`.
  2. The user clicks **Discard** (`ResetToSaved()` -> `Load(null)` or `Load(ordinarySnippet)`).
  3. At `SnippetEditorViewModel.cs:532`, `IsPassword = false;` executes while `vm.Text` is still `"MySecretPassword"`.
  4. Even though `_loading` is `true`, the `[ObservableProperty]` setter for `IsPassword` synchronously fires `PropertyChanged(nameof(IsPassword))` before line 533 (`Text = ...`) executes.
  5. `SnippetEditor.OnViewModelPropertyChanged` synchronously calls `SyncInputs()`, which sees `editor.IsPassword == false` and `editor.Text == "MySecretPassword"`, and executes `SnippetTextBox.Text = "MySecretPassword"`—writing the discarded password plaintext into the ordinary `TextBox` (and its internal undo/text buffer) before line 533 overwrites `Text`.
  6. Conversely, when `Load` transitions `IsPassword` from `false` to `true`, line 532 fires `PropertyChanged(nameof(IsPassword))` while `vm.Text` still holds the old ordinary text, transiently assigning the old ordinary text into `SnippetPasswordBox.Password` before line 533 sets the new text.
- **Suggested fix:**
  In `SnippetEditorViewModel.Load(Snippet? snippet)` (inside `try` while `_loading = true`), clear `Text = string.Empty;` before setting `IsPassword = snippet?.IsPassword ?? false;`, then set `Text = snippet?.Text ?? string.Empty;`. That guarantees `vm.Text` is empty whenever `IsPassword` transitions during `Load`.

---

### F2 — `D` (Major): `SnippetEditorViewModel.Load` violates Amendment A6 (`IsLabelCustom` cannot be `false` while `IsPassword` is `true`) during `Load` notifications and when loading a password `Snippet` with `IsLabelCustom == false`

- **Classification:** `D` (genuine new code defect)
- **Severity:** **Major** (violates §4.8 A6 invariant)
- **Evidence:**
  - `src/SmartSnippets.Presentation/ViewModels/SnippetEditorViewModel.cs:385–393`:
    ```csharp
    partial void OnIsPasswordChanged(bool value)
    {
        if (_loading) return;
        _labelBeforeAuto = null;
        if (value)
        {
            if (!IsLabelCustom) SetLabel(string.Empty);
            IsLabelCustom = true;
        }
    ```
  - `src/SmartSnippets.Presentation/ViewModels/SnippetEditorViewModel.cs:449–455`:
    ```csharp
    partial void OnIsLabelCustomChanged(bool value)
    {
        if (IsPassword && !value)
        {
            IsLabelCustom = true;
            return;
        }
    ```
  - `src/SmartSnippets.Presentation/ViewModels/SnippetEditorViewModel.cs:532–536`:
    ```csharp
            IsPassword = snippet?.IsPassword ?? false;
            Text = snippet?.Text ?? string.Empty;
            TagsInput = snippet is null ? string.Empty : TagList.Format(snippet.Tags);
            IsLabelCustom = snippet?.IsLabelCustom ?? false;
            SetLabel(IsLabelCustom ? snippet!.Label : Suggestion);
    ```
- **Concrete failure scenario:**
  1. When `Load(snippet)` loads a password snippet (`snippet.IsPassword == true`) while `_isLabelCustom` is `false` (which is the initial field value in `new SnippetEditorViewModel(...)` and also the state when an auto-labeled ordinary snippet is updated to a password snippet in the store), `OnIsPasswordChanged(true)` returns early at `if (_loading) return;`. Consequently, during all `PropertyChanged` notifications fired by lines 532–534 (`IsPassword`, `Suggestion`, `LabelPlaceholder`, `AutoLabelHint`, `CanChangeLabelMode`, `CanEditPassword`, `IsMultilinePassword`, `Text`, `TagsInput`, `TagChips`), `vm.IsPassword` is `true` while `vm.IsLabelCustom` is `false` and `vm.IsLabelAuto` is `true`.
  2. Furthermore, if `Load(snippet)` is given a `Snippet` with `IsPassword == true` and `IsLabelCustom == false` (e.g., from a database row with `is_password = 1, label_is_custom = 0`), line 535 executes `IsLabelCustom = false;` while `_isLabelCustom` is already `false`. Because `ObservableProperty` skips `OnIsLabelCustomChanged` when the value does not change, `IsLabelCustom` remains `false` after `Load` completes and line 536 (`SetLabel(IsLabelCustom ? snippet!.Label : Suggestion)`) overwrites `snippet.Label` with `Suggestion` (`""`).
- **Suggested fix:**
  In `Load(Snippet? snippet)` (and `ApplySaved(Snippet saved)`), compute `var isPassword = snippet?.IsPassword ?? false; var isCustom = isPassword || (snippet?.IsLabelCustom ?? false);` and set `IsLabelCustom = isCustom;` before (or immediately when) setting `IsPassword = isPassword;`.

---

### F3 — `D` (Major): `SnippetEditor.OnPasswordPaste` does not increment `_inputGeneration` on entry or re-sync when `text == editor.Text`, leaving stale `PasswordInputHint` and allowing overlapping async paste races

- **Classification:** `D` (genuine new code defect)
- **Severity:** **Major**
- **Evidence:**
  - `src/SmartSnippets.Uno/Controls/SnippetEditor.xaml.cs:109–140`:
    ```csharp
    private async void OnPasswordPaste(object sender, TextControlPasteEventArgs e)
    {
        // Cancel synchronously: an async clipboard read must never allow native single-line truncation.
        e.Handled = true;
        if (_inputUnloaded || _viewModel?.CanEditPassword != true) return;
        var editor = _viewModel;
        var generation = _inputGeneration;
        var original = editor.Text;
        try
        {
            var clipboard = Clipboard.GetContent();
            if (!clipboard.Contains(StandardDataFormats.Text)) return;
            var text = await clipboard.GetTextAsync();
            if (generation != _inputGeneration || !ReferenceEquals(editor, _viewModel) || _inputUnloaded
                || !editor.CanEditPassword || editor.Text != original) return;
            if (text.IndexOfAny(['\r', '\n']) >= 0)
            {
                PasswordInputHint.Text = UiText.PasswordPasteHint;
                return;
            }
            // PasswordBox exposes no selection range. Explicit whole-value replacement avoids a
            // second native clipboard read (which could race and paste unchecked multiline data).
            editor.Text = text;
            SnippetPasswordBox.SelectAll();
        }
    ```
  - `src/SmartSnippets.Uno/Controls/SnippetEditor.xaml.cs:272–275`:
    ```csharp
    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(SnippetEditorViewModel.Text) or nameof(SnippetEditorViewModel.IsPassword))
            SyncInputs();
    ```
- **Concrete failure scenario:**
  1. **Stale `PasswordInputHint` after valid same-value paste, Save, or Discard:** Suppose `editor.IsPassword == true` and `editor.Text == "secret"`. The user attempts to paste a multiline string, setting `PasswordInputHint.Text = UiText.PasswordPasteHint`. Next, the user either (a) copies `"secret"` and pastes it into `SnippetPasswordBox`, or (b) edits `Label` and clicks **Discard** (`ResetToSaved()`) or **Save**. Because `editor.Text` is already `"secret"` and `editor.IsPassword` is already `true`, `PropertyChanged` does not fire for `Text` or `IsPassword`. `SyncInputs()` never runs, and `PasswordInputHint.Text` stays stuck showing `"Multiline paste is not supported..."`.
  2. **Overlapping async pastes / paste across Save or Discard:** `OnPasswordPaste` reads `var generation = _inputGeneration;` without incrementing `_inputGeneration`. If Paste #1 awaits a slow `clipboard.GetTextAsync()` while the user triggers Paste #2 (which finishes first with a multiline refusal or with `text == original`, neither of which increments `_inputGeneration`) or while the user clicks **Save** or **Discard** (when `Text` and `IsPassword` equal the baseline), `_inputGeneration` and `editor.Text` are unchanged (`generation == _inputGeneration && editor.Text == original`). When Paste #1 later resolves, it overwrites `editor.Text` after Paste #2 or after `Save`/`Discard`.
- **Suggested fix:**
  1. Increment `_inputGeneration` at the start of `OnPasswordPaste` (`var generation = ++_inputGeneration;`).
  2. Call `SyncInputs()` after `editor.Text = text;` in `OnPasswordPaste` so `PasswordInputHint.Text` is cleared and `_inputGeneration` is bumped even when `text == original`.
  3. Trigger `SyncInputs()` on `Load`/`ApplySaved` as well (for example, by including `nameof(SnippetEditorViewModel.LabelPlaceholder)` in `OnViewModelPropertyChanged`, since `Load` and `ApplySaved` both unconditionally raise `OnPropertyChanged(nameof(LabelPlaceholder))`).

---

### F4 — `A` (Minor): `PopupViewModel.EnsureCurrent()` is called in `ShowKeyboardPreview()` (A8) but missed in `MoveSelection` and `OnPreviewKeyDown`

- **Classification:** `A` (a class of fix was applied at some sites but missed others)
- **Severity:** **Minor**
- **Evidence:**
  - Applied at: `src/SmartSnippets.Uno/Popup/PopupWindow.xaml.cs:482` (`ShowKeyboardPreview` calls `ViewModel.EnsureCurrent()`), `src/SmartSnippets.Presentation/ViewModels/PopupViewModel.cs:70,80,104,136,310` (`OpenPreview`, `Open`, `Query.set`, `ToggleLayout`, `OnEditorSaved`)
  - Missed at:
    - `src/SmartSnippets.Presentation/ViewModels/PopupViewModel.cs:116–126` (`MoveSelection(int delta)`)
    - `src/SmartSnippets.Uno/Popup/PopupWindow.xaml.cs:515–531` (`OnPreviewKeyDown`)
- **Concrete failure scenario:**
  While `PopupWindow` is visible with `Query == "secretOnly"` and a matching ordinary snippet selected, the snippet is updated to `IsPassword = true` in the store. `OnStoreChanged` marks `_cacheDirty = true` and synchronously closes any open preview, deferring the cache rebuild via `_schedule`. If keyboard input (`Up`, `Down`, or `Right`) arrives before the scheduled rebuild runs:
  - `ApplyQuery()` (`PopupWindow.xaml.cs:739`) returns immediately because `SearchBox.Text == ViewModel.Query`.
  - `MoveSelection` navigates the stale `ViewModel.Items` collection before the password row is filtered out.
  - Pressing `Right` calls `_keyboardPreview.Handle(PopupPreviewKey.Right, SelectedSnippet?.SnippetId, ...)` (`PopupWindow.xaml.cs:528`) using the stale non-null `SelectedSnippet?.SnippetId` before `ShowKeyboardPreview()` flushes `ViewModel.EnsureCurrent()`, causing `OnPreviewKeyDown` to set `e.Handled = true` and swallow the `Right` arrow key even though flushing the cache empties `Items` and resets keyboard navigation.
- **Suggested fix:**
  Call `EnsureCurrent()` at the start of `PopupViewModel.MoveSelection(int delta)` and call `ViewModel.EnsureCurrent()` in `PopupWindow.OnPreviewKeyDown` before reading `SelectedSnippet`.

---

### F5 — `B` (Major): `PasswordProtectionTests.FailedPasswordLoadStopsStartupWithoutRewritingRows` never asserts that database rows were not rewritten or deleted

- **Classification:** `B` (a test oracle identifies its subject by too few fields)
- **Severity:** **Major** (AC2 / §4.1 require proving that load failure does not rewrite, downgrade, or delete unreadable password rows)
- **Evidence:**
  - `tests/SmartSnippets.Uno.Tests/Composition/PasswordProtectionTests.cs:59–76`:
    ```csharp
    [Fact]
    public async Task FailedPasswordLoadStopsStartupWithoutRewritingRows()
    {
        ...
        await host.StartAsync(false, new(null, false, null));
        Assert.Equal(1, boundary.Reports);
        Assert.Contains("original Windows profile/key", boundary.StartupFailure);
        Assert.DoesNotContain("PRIVATE-BODY", boundary.StartupFailure);
        Assert.Equal(1, boundary.Exits);
        Assert.Empty(boundary.Popups);
    }
    ```
- **Concrete failure scenario (wrong implementation that passes):**
  A wrong implementation in `UnoAppHost.StartAsync` or `SnippetStore.Load` that catches `StoreException`, deletes the unreadable row (or resets `snippets` / downgrades `is_password = 0`), and then reports startup failure and exits still passes every assertion in `FailedPasswordLoadStopsStartupWithoutRewritingRows`.
- **Suggested fix:**
  After `await host.StartAsync(...)`, start a new `UnoAppHost` (or open `SnippetStore`) against `fixture.Root` with `Protector.Fail = false` and assert that the single snippet is still present with `IsPassword == true` and `Text == "PRIVATE-BODY"`.

---

### F6 — `B` (Minor): `AppLogPrivacyTests.PasswordFailuresNeverLogSecretOrCiphertext` omits assertions for operation and failure diagnostic tokens

- **Classification:** `B` (a test oracle identifies its subject by too few fields)
- **Severity:** **Minor**
- **Evidence:**
  - `tests/SmartSnippets.Core.Tests/Logging/AppLogPrivacyTests.cs:20–30`:
    ```csharp
        protector.Fail = true;
        Assert.Throws<SmartSnippets.Core.Store.StoreException>(() => store.UpdateSnippet(row.Id, new("PRIVATE-NEW-PASSWORD", "manual", true, null, "", true)));
        Assert.Throws<SmartSnippets.Core.Store.StoreException>(store.Load);
        var log = File.ReadAllText(Path.Combine(_directory, "app.log"));
        Assert.Contains("store_write_failed", log);
        Assert.DoesNotContain("PRIVATE-PASSWORD", log);
        Assert.DoesNotContain("PRIVATE-NEW-PASSWORD", log);
        Assert.DoesNotContain(cipher, log);
        Assert.DoesNotContain("dpapi:v1:", log);
    ```
- **Concrete failure scenario (wrong implementation that passes):**
  Plan §6 T7 specifies: *"Capture logs with a synthetic secret and assert it and ciphertext are absent while operation/failure tokens are present."* Because the test only asserts `Assert.Contains("store_write_failed", log)`, a wrong implementation where `store.Load()` does not log at all on `SecretProtectionException`, or where `AppLog.SafeErrorDetail` rejects `SecretProtectionException` / `op=update_snippet` / `op=load`, still passes.
- **Suggested fix:**
  Add `Assert.Contains("op=update_snippet error=SecretProtectionException", log);` and `Assert.Contains("op=load error=SecretProtectionException", log);`.

---

### F7 — `B` (Minor): `PasswordEditorSourceTests` and `EditorActionConsumers` leave XAML visibility bindings, `FocusText()` fallback, `OnPasswordTextInput` control reset, and `OnPasswordPaste` guards unverified

- **Classification:** `B` (a test oracle identifies its subject by too few fields)
- **Severity:** **Minor**
- **Evidence:**
  - `tests/SmartSnippets.Uno.Tests/Wiring/PasswordEditorSourceTests.cs:11–30`
  - `tests/SmartSnippets.Uno.Tests/Wiring/EditorActionConsumers.cs:37–105`
  - `src/SmartSnippets.Uno/Controls/SnippetEditor.xaml.cs:99–103,122–123,204–211`
- **Concrete failure scenario (wrong implementations that pass):**
  1. **XAML `Visibility` and `Text` bindings (`PasswordEditorSourceTests.cs:17–25`):**
     - Line 22 checks `Assert.DoesNotContain("x:Bind ViewModel.Text,", a.Value)` with a trailing comma; a default one-time binding `{x:Bind ViewModel.Text}` without a comma passes.
     - Neither `PasswordEditorSourceTests` nor `HeadlessXaml.cs` checks the `Visibility` bindings on `SnippetTextBox` (`{x:Bind Hide(ViewModel.IsPassword), Mode=OneWay}`), `SnippetPasswordBox` (`{x:Bind Show(ViewModel.CanEditPassword), Mode=OneWay}`), or `MultilinePasswordMask` (`{x:Bind Show(ViewModel.IsMultilinePassword), Mode=OneWay}`). Removing `Visibility` from `SnippetTextBox` in `SnippetEditor.xaml` still passes every test.
  2. **`FocusText()` visibility check and checkbox fallback (`SnippetEditor.xaml.cs:206–210`, `EditorActionConsumers.cs:38–50`):**
     - In `HeadlessXaml.cs`, `UIElement.Visibility` is always `Visibility.Visible`, so `input.Visibility == Visibility.Visible` is never false and `|| PasswordCheckBox.Focus(FocusState.Programmatic)` is never reached. Deleting `input.Visibility == Visibility.Visible &&` or replacing `|| PasswordCheckBox.Focus(FocusState.Programmatic)` with `|| false` still passes.
  3. **`OnPasswordTextInput` newline rejection control reset (`SnippetEditor.xaml.cs:99–103`, `EditorActionConsumers.cs:77`):**
     - Line 77 calls `OnPasswordTextInput(new PasswordBox { Password = "bad\ninput", FocusState = FocusState.Keyboard }, new())` with a throwaway `PasswordBox` instead of `SnippetPasswordBox`, and never asserts `SnippetPasswordBox.Password`. Deleting `SyncInputs();` at `SnippetEditor.xaml.cs:101` (leaving `"bad\ninput"` in `SnippetPasswordBox.Password`) still passes.
  4. **`OnPasswordPaste` hint clearing and stale guards (`SnippetEditor.xaml.cs:122–132`, `EditorActionConsumers.cs:87–99`):**
     - After pasting `"pasted"`, the test never asserts `Assert.Equal("pasted", SnippetPasswordBox.Password)` or `Assert.Empty(PasswordInputHint.Text)`.
     - The stale-paste check only sets `ViewModel = null`, which simultaneously trips `generation != _inputGeneration` and `!ReferenceEquals(editor, _viewModel)`. Deleting `_inputUnloaded || !editor.CanEditPassword || editor.Text != original` at `SnippetEditor.xaml.cs:122–123` still passes.
- **Suggested fix:**
  - In `PasswordEditorSourceTests.NoRevealButtonOrPlaintextPasswordBinding`, assert `Visibility` attributes on `SnippetTextBox`, `SnippetPasswordBox`, and `MultilinePasswordMask`, and check `a.Value` for `"ViewModel.Text"` (excluding `"ViewModel.TextError"`).
  - In `EditorActionConsumers`, test `FocusText()` when the active input's `Visibility` is `Visibility.Collapsed` (asserting `FocusManager.Focused` is `PasswordCheckBox`); invoke `OnPasswordTextInput` on `SnippetPasswordBox` when `SnippetPasswordBox.Password` contains a newline and assert `Assert.Equal("changed", SnippetPasswordBox.Password)`; assert `Assert.Equal("pasted", SnippetPasswordBox.Password)` and `Assert.Empty(PasswordInputHint.Text)` after single-line paste; and exercise stale paste cancellation when `vm.Text` or `vm.IsPassword` changes while `PendingText` is awaiting.

---

### F8 — `B` (Minor): `PasswordProjectionTests` does not test `SnippetListViewModel` or `SnippetRowViewModel.GetHashCode()`

- **Classification:** `B` (a test oracle identifies its subject by too few fields)
- **Severity:** **Minor**
- **Evidence:**
  - `tests/SmartSnippets.Presentation.Tests/PasswordProjectionTests.cs:10–47`
  - `src/SmartSnippets.Presentation/ViewModels/SnippetRowViewModel.cs:30`
- **Concrete failure scenario (wrong implementation that passes):**
  `PasswordProjectionTests` calls `SnippetRowViewModel.From(...)` directly instead of constructing `SnippetListViewModel` (the main-window list VM), and `ModeTransitionReplacesRows` only asserts `Assert.NotEqual(before, after)`. Removing `IsPassword` from `SnippetRowViewModel.GetHashCode()` (`SnippetRowViewModel.cs:30`) still passes all tests.
- **Suggested fix:**
  Assert `Assert.NotEqual(before.GetHashCode(), after.GetHashCode())` in `ModeTransitionReplacesRows`, and verify via `SnippetListViewModel` that main-list rows are replaced on mode transition and that secret body substrings are not matched by `SnippetListViewModel.Query`.

---

## 3. Readiness Verdict

**IS THIS BRANCH READY TO MERGE? No.**

### Blockers (must fix before merge)
- **F1 (`D`, Blocker):** `SnippetEditorViewModel.Load` sets `IsPassword = false` before updating `Text`, causing `SnippetEditor.SyncInputs()` to write discarded password plaintext into `SnippetTextBox.Text` on `true -> false` transitions (such as `ResetToSaved()` / Discard).
- *(Plus pending T7 end-to-end tests and T8 wiring audit / desktop QA evidence per plan 016.)*

### Improvements (Major / Minor findings to address in T3–T7)
- **F2 (`D`, Major):** Keep `IsLabelCustom == true` throughout `SnippetEditorViewModel.Load` whenever `snippet.IsPassword` is `true` (including during `_loading` property notifications and when `snippet.IsLabelCustom` is `false`).
- **F3 (`D`, Major):** Increment `_inputGeneration` at the start of `OnPasswordPaste`, call `SyncInputs()` after setting `editor.Text = text` in `OnPasswordPaste`, and re-sync inputs on `Load`/`ApplySaved` even when `Text` and `IsPassword` are unchanged.
- **F5 (`B`, Major):** Assert in `PasswordProtectionTests.FailedPasswordLoadStopsStartupWithoutRewritingRows` that the encrypted password row remains intact in SQLite after the failed startup.
- **F4 (`A`, Minor):** Call `EnsureCurrent()` in `PopupViewModel.MoveSelection` and `PopupWindow.OnPreviewKeyDown`.
- **F6 (`B`, Minor):** Assert `op=update_snippet`, `op=load`, and `error=SecretProtectionException` tokens in `AppLogPrivacyTests.PasswordFailuresNeverLogSecretOrCiphertext`.
- **F7 (`B`, Minor):** Strengthen `PasswordEditorSourceTests` and `EditorActionConsumers` oracles for `Visibility` bindings, `FocusText()` fallback, `OnPasswordTextInput` reset on `SnippetPasswordBox`, and `OnPasswordPaste` guards.
- **F8 (`B`, Minor):** Assert `GetHashCode()` inequality and `SnippetListViewModel` behavior in `PasswordProjectionTests`.

<<RV1-DONE>>
