using SmartSnippets.Presentation.Services;

namespace SmartSnippets.Presentation.Tests.Fakes;

/// <summary>Queued answers plus a record of every call. An unscripted question fails the test loudly.</summary>
internal sealed class ScriptedDialogService : IDialogService
{
    public Queue<bool> ConfirmAnswers { get; } = new();

    public Queue<SaveChoice> ChoiceAnswers { get; } = new();

    public List<(string Title, string Message, string ConfirmText)> Confirms { get; } = [];

    public List<(string Title, string Message)> SaveDiscardPrompts { get; } = [];

    public List<(string Title, string Message)> Errors { get; } = [];

    /// <summary>When set, <see cref="ShowErrorAsync"/> stays open until the source completes.</summary>
    public TaskCompletionSource? ErrorGate { get; set; }
    public TaskCompletionSource? InfoGate { get; set; }
    public List<(string Title, string Message)> Infos { get; } = [];
    public Exception? InfoException { get; set; }
    public Action? OnInfo { get; set; }

    public Task ShowInfoAsync(string title, string message)
    {
        OnInfo?.Invoke();
        Infos.Add((title, message));
        if (InfoException is { } ex) throw ex;
        return InfoGate?.Task ?? Task.CompletedTask;
    }

    public Task<bool> ConfirmAsync(string title, string message, string confirmText)
    {
        Confirms.Add((title, message, confirmText));
        return Task.FromResult(ConfirmAnswers.Count > 0
            ? ConfirmAnswers.Dequeue()
            : throw new InvalidOperationException($"Unscripted confirm: {title}"));
    }

    public Task<SaveChoice> AskSaveDiscardCancelAsync(string title, string message)
    {
        SaveDiscardPrompts.Add((title, message));
        return Task.FromResult(ChoiceAnswers.Count > 0
            ? ChoiceAnswers.Dequeue()
            : throw new InvalidOperationException($"Unscripted save/discard/cancel: {title}"));
    }

    public Task ShowErrorAsync(string title, string message)
    {
        Errors.Add((title, message));
        return ErrorGate?.Task ?? Task.CompletedTask;
    }
}
