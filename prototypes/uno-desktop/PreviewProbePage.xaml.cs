using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using SmartSnippets.Presentation.ViewModels;
using Windows.System;

namespace SmartSnippets.UnoProbe;

public sealed partial class PreviewProbePage : Page
{
    public PopupViewModel Model { get; }
    private readonly Action<Exception> _fail;
    private readonly Func<bool> _hidden;
    public int EnterEvents { get; private set; }
    public bool EnterCompleted { get; private set; }
    internal bool IsSearchFocused => XamlRoot is not null && ReferenceEquals(FocusManager.GetFocusedElement(XamlRoot), SearchEditor);
    internal bool FocusSearch() => SearchEditor.Focus(FocusState.Programmatic) && IsSearchFocused;
    internal bool IsReady => IsLoaded && ActualWidth > 0 && ActualHeight > 0 && RowsView.ContainerFromIndex(Model.SelectedIndex) is ListViewItem { ActualHeight: > 0 } row &&
        ContainsText(row, "Synthetic preview") && ContainsText(row, "#synthetic");

    public PreviewProbePage(PopupViewModel model, Action<Exception> fail, Func<bool> hidden)
    {
        Model = model;
        _fail = fail;
        _hidden = hidden;
        InitializeComponent();
#if WINDOWS
        RowsView.ChoosingItemContainer += (_, args) =>
        {
            if (args.ItemContainer is null) args.ItemContainer = new ListViewItem();
            args.IsContainerPrepared = false;
        };
#endif
    }

    private async void SearchKeyDown(object sender, KeyRoutedEventArgs args)
    {
        if (args.Key != VirtualKey.Enter) return;
        args.Handled = true;
        try
        {
            EnterEvents++;
            if (EnterEvents != 1 || !IsSearchFocused || !Model.IsPreview || Model.Target is not null || Model.SelectedIndex != 0)
                throw new InvalidOperationException("Preview key precondition failed.");
            await Model.InsertSelectedAsync();
            if (!_hidden()) throw new InvalidOperationException("Preview did not hide.");
            EnterCompleted = true;
            Console.WriteLine("ui_preview_enter_routed");
        }
        catch (Exception error) { _fail(error); }
    }

    private static bool ContainsText(DependencyObject parent, string expected)
    {
        if (parent is TextBlock text && text.Text == expected) return true;
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            if (ContainsText(VisualTreeHelper.GetChild(parent, i), expected)) return true;
        return false;
    }
}
