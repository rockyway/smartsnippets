using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

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
    public ProbeRow[] Rows { get; } =
    [
        new("Synthetic row A", "Unicode λ 日本語 and a multiline\npreview"),
        new("Synthetic row B", "Second synthetic result")
    ];

    public PopupProbePage() => InitializeComponent();

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

    private static bool ContainsBoundText(DependencyObject parent, string expected, double? size = null)
    {
        if (parent is TextBlock text && text.Text == expected && (size is null || text.FontSize == size))
            return true;
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
            if (ContainsBoundText(VisualTreeHelper.GetChild(parent, index), expected, size)) return true;
        return false;
    }
}
