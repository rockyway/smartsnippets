namespace SmartSnippets.Uno.Pages;

public sealed partial class SnippetsPage
{
    internal bool ReleaseMeasurementBindings()
    {
        _searchTimer?.Stop();
        if (_viewModel is not null) _viewModel.List.Query = SearchBox.Text;
        var narrow = _narrowShowsEditor;
        ViewModel = null; // Detach explicit VM and editor command/property subscriptions.
        EditorControl.ReleaseMeasurementBindings();
        Bindings.StopTracking(); // Detach generated x:Bind subscriptions before dropping the page.
        return narrow;
    }
    internal void RestoreMeasurementState(bool narrow) => _narrowShowsEditor = narrow;
}

public sealed partial class FoldersPage
{
    internal void ReleaseMeasurementBindings()
    {
        ViewModel = null;
        Bindings.StopTracking();
    }
}

public sealed partial class SettingsPage
{
    internal void ReleaseMeasurementBindings()
    {
        Bindings.StopTracking();
        _viewModel = null;
    }
}
