using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Input;

namespace SmartSnippets.UnoProbe;

public sealed class ProbeRow(string label, string preview)
{
    public string Label { get; } = label;
    public string Preview { get; } = preview;
    public double TextSize => 18;
}

public sealed partial class PopupProbePage : Page
{
    public string Query { get; set; } = "synthetic";
    public List<ProbeRow> Rows { get; } =
    [
        new("Synthetic row A", "Unicode λ 日本語 and a multiline\npreview"),
        new("Synthetic row B", "Second synthetic result")
    ];

    public PopupProbePage()
    {
        InitializeComponent();
#if WINDOWS
        // The native list creates containers internally. Under Native AOT an
        // unconstructed ListViewItem can otherwise return as a base RCW, so
        // explicitly create genuine containers at the supported factory seam.
        RowsView.ChoosingItemContainer += (_, args) =>
        {
            if (args.ItemContainer is null) args.ItemContainer = new ListViewItem();
            args.IsContainerPrepared = false;
        };
#endif
    }

    internal bool IsSearchFocused => XamlRoot is not null &&
        ReferenceEquals(FocusManager.GetFocusedElement(XamlRoot), SearchEditor);
    internal bool FocusSearch() => SearchEditor.Focus(FocusState.Programmatic) && IsSearchFocused;

    internal int ReadinessFlags => (IsLoaded ? 1 : 0) | (ActualWidth > 0 ? 2 : 0) |
        (ActualHeight > 0 ? 4 : 0) | (RowsView.Items.Count == 2 ? 8 : 0) |
        (RowsView.ContainerFromIndex(0) is ListViewItem ? 16 : 0) | (HasRealizedRow ? 32 : 0) |
        (RowsView.IsLoaded ? 64 : 0) | (RowsView.ActualHeight > 0 ? 128 : 0) |
        (RowsView.ItemsPanelRoot is not null ? 256 : 0) |
        (RowsView.ItemsPanelRoot?.Children.Count > 0 ? 512 : 0);
    internal int PanelChildCount => RowsView.ItemsPanelRoot?.Children.Count ?? 0;
    internal int VisualRowCount => CountVisualRows(RowsView);
    internal int VisualTextCount => CountVisualText(RowsView);
    internal ElementTheme TargetTheme { get; private set; }
    internal bool HasRealizedRow => RowsView.ContainerFromIndex(0) is ListViewItem { ActualHeight: > 0 };

    internal void VerifyCompiledRows()
    {
        if (SearchEditor.Text != Query || RowsView.Items.Count != 2 ||
            RowsView.ContainerFromIndex(0) is not ListViewItem row || row.ActualHeight <= 0 ||
            !ContainsBoundText(row, Rows[0].Label, Rows[0].TextSize) ||
            !ContainsBoundText(row, Rows[0].Preview))
            throw new InvalidOperationException("Compiled popup row realization failed.");
        RowsView.SelectedIndex = 1;
        if (!ReferenceEquals(RowsView.SelectedItem, Rows[1]))
            throw new InvalidOperationException("Popup selection failed.");
        TargetTheme = ActualTheme == ElementTheme.Dark ? ElementTheme.Light : ElementTheme.Dark;
        RequestedTheme = TargetTheme;
    }

    private static int CountVisualRows(DependencyObject parent)
    {
        var count = parent is ListViewItem ? 1 : 0;
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
            count += CountVisualRows(VisualTreeHelper.GetChild(parent, index));
        return count;
    }

    private static int CountVisualText(DependencyObject parent)
    {
        var count = parent is TextBlock ? 1 : 0;
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
            count += CountVisualText(VisualTreeHelper.GetChild(parent, index));
        return count;
    }

    private static bool ContainsBoundText(DependencyObject parent, string expected, double? size = null)
    {
        if (parent is TextBlock text && text.Text == expected && (size is null || text.FontSize == size))
            return true;
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
            if (ContainsBoundText(VisualTreeHelper.GetChild(parent, index), expected, size)) return true;
        return false;
    }
}
