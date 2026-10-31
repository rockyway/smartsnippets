using SmartSnippets.Core.Settings;
using SmartSnippets.Presentation.Navigation;
using SmartSnippets.Presentation.Services;
using SmartSnippets.Presentation.ViewModels;

namespace SmartSnippets.Uno;

internal interface ICandidateMainWindow : IDisposable
{
    event EventHandler? HiddenByClose;
    Task NavigateAsync(PageKind page, bool show);
    Task<bool> CanExitAsync();
    void SetInteractionEnabled(bool enabled);
}
internal interface ICandidatePopupWindow : IPopupHost, IDisposable
{
    event EventHandler? Closed;
    void Prewarm();
    void ShowPreview(string? query, bool openSortMenu);
}
internal interface ICandidateTray : INotifier, IDisposable { }

/// <summary>Only external UI/OS boundaries are substituted; UnoAppHost always composes the real store and VMs.</summary>
internal abstract class CandidateHostBoundary
{
    internal abstract IUiDispatcher Dispatcher { get; }
    internal abstract IThemeService CreateTheme(AppSettings settings);
    internal abstract IDisposable CreateMessageWindow();
    internal abstract IDialogService CreateDialogs(bool popup);
    internal abstract ISnippetFileService CreateFiles();
    internal abstract ICandidateMainWindow CreateMain(UnoAppHost host);
    internal abstract ICandidatePopupWindow CreatePopup(UnoAppHost host);
    internal abstract ICandidateTray CreateTray(Action open, Action preview, Action settings, Action exit);
    internal abstract void ReportStartupFailure();
    internal abstract void Exit();
}
