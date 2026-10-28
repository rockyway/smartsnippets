using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SmartSnippets.Presentation.ViewModels;

namespace SmartSnippets.App.Controls;

public sealed partial class PopupRow : UserControl
{
    private PopupItemViewModel? _item;
    public PopupRow() => InitializeComponent();
    public PopupItemViewModel? Item
    {
        get => _item;
        set { _item = value; Bindings.Update(); }
    }
    private Visibility HasText(string? value) => string.IsNullOrEmpty(value) ? Visibility.Collapsed : Visibility.Visible;
}
