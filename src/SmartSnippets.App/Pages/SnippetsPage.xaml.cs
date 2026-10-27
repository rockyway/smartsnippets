using Microsoft.UI.Xaml.Controls;
using SmartSnippets.Presentation.Navigation;

namespace SmartSnippets.App.Pages;

/// <summary>Placeholder until plan 005.</summary>
public sealed partial class SnippetsPage : Page, IGuardedPage
{
    public SnippetsPage()
    {
        InitializeComponent();
    }

    public Task<bool> CanLeaveAsync() => Task.FromResult(true);
}
