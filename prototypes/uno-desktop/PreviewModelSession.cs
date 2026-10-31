using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using SmartSnippets.Core.Data;
using SmartSnippets.Core.Domain;
using SmartSnippets.Core.Settings;
using SmartSnippets.Core.Store;
using SmartSnippets.Presentation.Services;
using SmartSnippets.Presentation.ViewModels;

namespace SmartSnippets.UnoProbe;

/// <summary>Actual store/VM; every forbidden preview boundary counts then fails.</summary>
internal sealed class PreviewModelSession : IPopupHost, IInsertionService, IClipboardService,
    IElevation, IDialogService, INotifier, IDisposable
{
    private readonly string _root;
    private readonly SqliteConnectionFactory _factory;
    private readonly SnippetStore _store;
    private readonly Action _hide;
    private readonly Snippet _original;
    public PopupViewModel Model { get; }
    public int Hides { get; private set; }
    public int ForbiddenCalls { get; private set; }

    public PreviewModelSession(Action hide, Action<Action> schedule)
    {
        _hide = hide;
        _root = Path.Combine(Path.GetTempPath(), "SmartSnippets.Uno018-preview-" + Guid.NewGuid().ToString("N"));
        if (Directory.Exists(_root)) throw new InvalidOperationException();
        DataProbe.CreatePrivateRoot(_root);
        File.WriteAllText(Path.Combine(_root, ".probe-owned"), "preview018");
        _factory = new SqliteConnectionFactory(Path.Combine(_root, "preview.db"));
        new Migrator(_factory).MigrateToLatest();
        _store = new SnippetStore(_factory, TimeProvider.System);
        _store.Load();
        var created = _store.CreateSnippet(new SnippetDraft("Synthetic preview λ 日本語 😀\nsecond line", "Synthetic preview", true, null, "synthetic"));
        _original = created.Value ?? throw new InvalidOperationException();
        Model = new PopupViewModel(_store, new SettingsRepository(_factory), new AppSettings(),
            this, this, this, this, this, this, schedule: schedule);
        Require(Model.OpenPreview() == PopupOpenMode.Fresh && Model.IsPreview && Model.Target is null);
        Require(Model.Items.Count(i => i.Kind == PopupItemKind.Snippet) == 1 && Model.SelectedIndex >= 0);
    }

    public void VerifyUnchanged()
    {
        Require(Hides == 1 && ForbiddenCalls == 0 && Model.IsPreview && Model.Target is null && !Model.IsInserting);
        var fresh = new SnippetStore(_factory, TimeProvider.System);
        fresh.Load();
        var saved = fresh.Snippets.Single().Snippet;
        Require(saved.Id == _original.Id && saved.Text == _original.Text && saved.Label == _original.Label &&
            saved.UsageCount == 0 && saved.LastUsedAt is null && saved.CreatedAt == _original.CreatedAt &&
            saved.UpdatedAt == _original.UpdatedAt && saved.Tags.SequenceEqual(_original.Tags));
    }

    public void Hide() { Hides++; _hide(); }
    private Exception Forbidden() { ForbiddenCalls++; return new InvalidOperationException("Forbidden preview boundary."); }
    public void ShowAgain() => throw Forbidden();
    public void OpenMainWindow() => throw Forbidden();
    public void EditSnippet(long id) => throw Forbidden();
    public void Focus(PopupFocusTarget target) => throw Forbidden();
    public Task<InsertionResult> InsertAsync(InsertionTarget target, long id, InsertionOptions options = default, CancellationToken ct = default) => throw Forbidden();
    public void SetText(string text) => throw Forbidden();
    public bool IsElevated => false;
    public ElevationResult RestartAsAdministrator() => throw Forbidden();
    public void Notify(string title, string message) => throw Forbidden();
    public Task<bool> ConfirmAsync(string title, string message, string confirmText) => throw Forbidden();
    public Task<SaveChoice> AskSaveDiscardCancelAsync(string title, string message) => throw Forbidden();
    public Task ShowErrorAsync(string title, string message) => throw Forbidden();
    public Task ShowInfoAsync(string title, string message) => throw Forbidden();

    public void Dispose()
    {
        Model.Dispose();
        SqliteConnection.ClearAllPools();
        Require(File.ReadAllText(Path.Combine(_root, ".probe-owned")) == "preview018");
        Directory.Delete(_root, recursive: true);
        Require(!Directory.Exists(_root));
    }

    private static void Require(bool value) { if (!value) throw new InvalidOperationException("Preview model assertion failed."); }
}
