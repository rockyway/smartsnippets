using SmartSnippets.Uno.Platforms.Windows;

namespace SmartSnippets.Uno;

internal enum PositiveSceneReason
{
    CallbackDuplicate, ShortcutForegroundNotReady, PopupShowRefused, PopupReadinessTimeout,
    PopupForegroundRefused, CopyRestoreRefused, PopupWarmupTimeout, ShortcutReadinessFailed, PopupShowFailed
}

/// <summary>Reasons are fixed at the throw site; never infer a reason from exception text.</summary>
internal sealed class PositiveSceneFailure(PositiveSceneReason reason) : InvalidOperationException
{
    internal string Token => reason switch
    {
        PositiveSceneReason.CallbackDuplicate => "callback_duplicate",
        PositiveSceneReason.ShortcutForegroundNotReady => "shortcut_foreground_not_ready",
        PositiveSceneReason.PopupShowRefused => "popup_show_refused",
        PositiveSceneReason.PopupReadinessTimeout => "popup_readiness_timeout",
        PositiveSceneReason.PopupForegroundRefused => "popup_foreground_refused",
        PositiveSceneReason.CopyRestoreRefused => "copy_restore_refused",
        PositiveSceneReason.PopupWarmupTimeout => "popup_warmup_timeout",
        PositiveSceneReason.ShortcutReadinessFailed => "shortcut_readiness_failed",
        PositiveSceneReason.PopupShowFailed => "popup_show_failed",
        _ => "unknown"
    };
}

internal static class PositiveSceneChecks
{
    internal static async Task WaitForWarmupAsync(Func<bool> ready, Func<long> elapsed, Func<Task> delay)
    {
        while (!ready())
        {
            if (elapsed() >= 5000) throw new PositiveSceneFailure(PositiveSceneReason.PopupWarmupTimeout);
            await delay();
        }
    }

    internal static async Task ShortcutAsync(Func<bool> accept, Func<Task<bool>> ready, Func<bool> show)
    {
        if (!accept()) throw new PositiveSceneFailure(PositiveSceneReason.CallbackDuplicate);
        bool admitted;
        try { admitted = await ready(); }
        catch (Exception) { throw new PositiveSceneFailure(PositiveSceneReason.ShortcutReadinessFailed); }
        if (!admitted) throw new PositiveSceneFailure(PositiveSceneReason.ShortcutForegroundNotReady);
        bool shown;
        try { shown = show(); }
        catch (Exception) { throw new PositiveSceneFailure(PositiveSceneReason.PopupShowFailed); }
        if (!shown) throw new PositiveSceneFailure(PositiveSceneReason.PopupShowRefused);
    }

    internal static async Task RestoreBeforeHideAsync(Func<Task<bool>> restore, Action hide)
    {
        if (!await restore()) throw new PositiveSceneFailure(PositiveSceneReason.CopyRestoreRefused);
        hide();
    }

    internal static async Task PopupReadyAsync(Func<nint> foreground, nint a, nint popup, Func<bool> ready,
        Func<long> elapsed, Func<Task> delay)
    {
        var gap = new ForegroundNullGap();
        while (true)
        {
            var window = foreground();
            if (!gap.Observe(window == 0, window == a || window == popup, elapsed()) ||
                (window != 0 && window != a && window != popup))
                throw new PositiveSceneFailure(PositiveSceneReason.PopupForegroundRefused);
            if (window == popup && ready()) return;
            if (elapsed() >= 5000) throw new PositiveSceneFailure(PositiveSceneReason.PopupReadinessTimeout);
            await delay();
        }
    }
}
