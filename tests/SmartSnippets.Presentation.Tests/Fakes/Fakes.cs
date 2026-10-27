using SmartSnippets.Core.Settings;
using SmartSnippets.Presentation.Services;

namespace SmartSnippets.Presentation.Tests.Fakes;

internal sealed class MemorySettingsStore : ISettingsStore
{
    public Dictionary<string, string> Values { get; } = [];

    public string? Get(string key) => Values.TryGetValue(key, out var v) ? v : null;

    public void Set(string key, string value) => Values[key] = value;
}

internal sealed class FakeThemeService : IThemeService
{
    public ThemeMode Current { get; private set; } = ThemeMode.System;

    public List<ThemeMode> Applied { get; } = [];

    public void Apply(ThemeMode mode)
    {
        Current = mode;
        Applied.Add(mode);
    }
}

internal sealed class FakeStartupRegistration : IStartupRegistration
{
    public bool Enabled { get; set; }

    public StartupResult? NextResult { get; set; }

    public int Calls { get; private set; }

    public bool IsEnabled() => Enabled;

    public StartupResult Enable() => Run(true);

    public StartupResult Disable() => Run(false);

    private StartupResult Run(bool enable)
    {
        Calls++;
        var result = NextResult ?? new StartupResult(true, string.Empty);
        if (result.Success)
        {
            Enabled = enable;
        }

        return result;
    }
}

internal sealed class FakeHotkeyManager : IHotkeyManager
{
    private HotkeyStatus _status = new(true, HotkeyGesture.Default, string.Empty);

    public HotkeyStatus Status
    {
        get => _status;
        set
        {
            _status = value;
            StatusChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public event EventHandler? StatusChanged;

    public HotkeyChangeResult NextResult { get; set; } = new(true, string.Empty);

    public List<HotkeyGesture> TryChangeCalls { get; } = [];

    public int RestoreDefaultCalls { get; private set; }

    public HotkeyChangeResult TryChange(HotkeyGesture gesture)
    {
        TryChangeCalls.Add(gesture);
        if (NextResult.Success)
        {
            _status = new HotkeyStatus(true, gesture, string.Empty);
        }

        return NextResult;
    }

    public HotkeyChangeResult RestoreDefault()
    {
        RestoreDefaultCalls++;
        if (NextResult.Success)
        {
            _status = new HotkeyStatus(true, HotkeyGesture.Default, string.Empty);
        }

        return NextResult;
    }
}

internal sealed class FakeElevation : IElevation
{
    public bool IsElevated { get; set; }

    public ElevationResult NextResult { get; set; } = new(ElevationOutcome.Started, string.Empty);

    public int Calls { get; private set; }

    public ElevationResult RestartAsAdministrator()
    {
        Calls++;
        return NextResult;
    }
}
