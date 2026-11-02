using Microsoft.UI.Xaml;
using SmartSnippets.Uno.Pages;

namespace SmartSnippets.Uno;

public sealed partial class MainWindow
{
    private bool? _measurementNarrowEditor;
    private readonly Dictionary<SmartSnippets.Presentation.Navigation.PageKind, List<(double Horizontal, double Vertical)>> _measurementScroll = [];

    internal void ReleaseMeasurementPages()
    {
        if (!IsHidden || _closingForExit || _closed || _guardInProgress || _pages.Count == 0) return;
        Controls.HoverDetailsSurface.CancelActive(ContentXamlRoot);
        foreach (var (kind, page) in _pages)
        {
            _measurementScroll[kind] = Measurement.MeasurementScrollState.Capture(page);
            switch (page)
            {
                case SnippetsPage snippets: _measurementNarrowEditor = snippets.ReleaseMeasurementBindings(); break;
                case FoldersPage folders: folders.ReleaseMeasurementBindings(); break;
                case SettingsPage settings: settings.ReleaseMeasurementBindings(); break;
            }
        }
        Navigation.Content = null;
        _pages.Clear();
        // Keep shell, current page, host-owned VMs/editor/selection/query and warm popup.
        // No GC here: this variant measures UI release alone, separately from the trim variant.
    }

    private void RestoreMeasurementPageState(UIElement page)
    {
        if (_current is { } kind && page is FrameworkElement root && _measurementScroll.Remove(kind, out var offsets))
            Measurement.MeasurementScrollState.Restore(root, offsets);
        if (page is SnippetsPage snippets && _measurementNarrowEditor is { } narrow)
        {
            snippets.RestoreMeasurementState(narrow);
            _measurementNarrowEditor = null;
        }
    }
}
