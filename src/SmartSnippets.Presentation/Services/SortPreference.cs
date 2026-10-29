using SmartSnippets.Core.Settings;

namespace SmartSnippets.Presentation.Services;

/// <summary>The snippet sort shared by the hotkey popup and the main snippet list; persisted under the popup sort key.</summary>
public sealed class SortPreference
{
    private readonly ISettingsStore _settings;

    public SortPreference(ISettingsStore settings, SortField initial) { _settings = settings; Current = initial; }

    public SortField Current { get; private set; }

    public event EventHandler? Changed;

    public void Set(SortField sort)
    {
        if (Current == sort) return;
        Current = sort;
        _settings.Set(SettingsKeys.PopupSort, sort.ToString());
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
