namespace SmartSnippets.Uno.Controls;

public sealed partial class SnippetEditor
{
    internal void ReleaseMeasurementBindings()
    {
        ViewModel = null;
        Bindings.StopTracking();
    }
}
