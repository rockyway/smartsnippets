using SmartSnippets.Presentation.Services;
using SmartSnippets.Presentation.ViewModels;

namespace SmartSnippets.Uno;

// Shared by compiled window handlers and portable lifecycle drivers; never bypass readiness.
internal sealed class CandidatePopupLifecycle
{
    internal event EventHandler<bool>? PresentationChanged;
    internal PopupShowCoordinator Shows { get; }
    internal CandidatePopupLifecycle(Action<Action> schedule, Action<PopupFocusTarget> focus,
        Action startWarmup, Action stopWarmup, Action? warmupFailed = null)
    {
        Shows = new(schedule, focus, startWarmup, stopWarmup, () => OnWarmupFailed(warmupFailed));
    }
    private void OnWarmupFailed(Action? callback)
    {
        try { callback?.Invoke(); }
        finally { Shows.Hide(); Hidden(); }
    }

    private PopupShowResult Present(Func<PopupShowResult> show)
    {
        var result = default(PopupShowResult);
        try { return result = show(); }
        finally { PresentationChanged?.Invoke(this, result.Shown); }
    }
    internal void Hidden() => PresentationChanged?.Invoke(this, false);
    internal void Preview(PopupViewModel model, string? query, Func<PopupShowResult> reveal, Func<bool>? canShow = null)
        => Shows.RequestShow(() => Present(() =>
        {
            if (canShow?.Invoke() == false) return default;
            var mode = model.OpenPreview();
            if (mode == PopupOpenMode.Ignored)
            {
                if (!model.IsPreview || !model.IsEditorOpen || model.IsInserting) return default;
                model.EnsureCurrent(); // Resume without resetting the editor's query or selection.
            }
            else if (query is not null) model.Query = query;
            return canShow?.Invoke() == false ? default : reveal();
        }));

    internal void Captured(PopupViewModel model, InsertionTarget target, Func<bool> canShow, Func<PopupShowResult> reveal)
        => Shows.RequestShow(() => Present(() =>
        {
            if (!canShow() || model.Open(target) == PopupOpenMode.Ignored) return default;
            return canShow() ? reveal() : default;
        }));

    internal void Reopen(PopupViewModel model, Func<PopupShowResult> reveal, Func<bool>? canShow = null)
        => Shows.RequestShow(() => Present(() =>
        {
            if (canShow?.Invoke() == false) return default;
            model.EnsureCurrent(); return reveal();
        }));
}

internal static class CandidateUiPolicy
{
    internal static bool PlaceManagement(bool qa, Func<bool> parkBottomLeft, Func<bool> centerPrimary)
        => (qa && parkBottomLeft()) || centerPrimary();

    internal static int BottomLeftMonitorIndex(IReadOnlyList<SmartSnippets.Core.Layout.PixelRect> monitors)
    {
        if (monitors.Count == 0) throw new InvalidOperationException("No candidate display is available.");
        var selected = 0;
        for (var i = 0; i < monitors.Count; i++)
        {
            if (monitors[i].Width <= 0 || monitors[i].Height <= 0)
                throw new InvalidOperationException("Invalid candidate display geometry.");
            if (IsBetterBottomLeft(monitors[i], monitors[selected])) selected = i;
        }
        return selected;
    }

    internal static bool IsBetterBottomLeft(SmartSnippets.Core.Layout.PixelRect candidate, SmartSnippets.Core.Layout.PixelRect selected)
        => candidate.Left < selected.Left || (candidate.Left == selected.Left && candidate.Top > selected.Top);

    internal static void ReportStartupFailure(bool background, bool qaRequest, Action showInteractiveDialog, Action reportQuietFailure)
    {
        if (!background && !qaRequest) showInteractiveDialog();
        else reportQuietFailure();
    }

    internal const string PreviewHint = "Preview only — selection closes; clipboard unavailable";
    internal static string HintLine(string hint, string status, bool isPreview)
        => isPreview && string.IsNullOrEmpty(status) ? PreviewHint : hint;

    internal static T? DialogRoot<T>(Func<T?> getRoot, Func<bool> isVisible, Action show) where T : class
    {
        if (!isVisible() || getRoot() is null) show();
        return getRoot();
    }

    internal static async Task<bool> NavigateAsync(Func<Task<bool>> guard, Action navigate, bool show, Action reveal)
    {
        if (!await guard()) return false;
        navigate();
        if (show) reveal();
        return true;
    }
}
