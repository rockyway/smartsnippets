using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml;
using SmartSnippets.App.Services;
using SmartSnippets.Core;
using SmartSnippets.Core.Data;

namespace SmartSnippets.App.Diagnostics;

public sealed partial class DiagnosticsWindow : Window
{
    private const string ProbeText = "Héllo – naïve 👋 世界\r\n\tline 2  (trailing spaces)  \r\nline 3";

    private readonly StartupRegistration _startup = new();
    private bool _suppressToggle;

    public DiagnosticsWindow()
    {
        InitializeComponent();
        DynamicCodeText.Text = $"Dynamic code supported: {RuntimeFeature.IsDynamicCodeSupported}";
        RuntimeText.Text = $".NET: {Environment.Version}";

        StartupToggle.IsOn = _startup.IsEnabled();
        StartupToggle.Toggled += OnStartupToggled;
    }

    private void OnRunSqliteProbe(object sender, RoutedEventArgs e)
    {
        try
        {
            var factory = new SqliteConnectionFactory(new AppPaths().DatabasePath);
            new Migrator(factory).MigrateToLatest();
            var result = new ProbeRepository(factory).RunCrud(ProbeText);
            SqliteResultText.Text = result.Success
                ? $"CRUD OK — {factory.DatabasePath}"
                : $"CRUD FAILED — {result.Message}";
        }
        catch (Exception ex)
        {
            SqliteResultText.Text = $"CRUD FAILED — {ex.GetType().Name}: {ex.Message}";
        }
    }

    private void OnStartupToggled(object sender, RoutedEventArgs e)
    {
        if (_suppressToggle)
        {
            return;
        }

        var wantOn = StartupToggle.IsOn;
        var result = wantOn ? _startup.Enable() : _startup.Disable();
        StartupResultText.Text = result.Message;

        if (!result.Success)
        {
            _suppressToggle = true;
            StartupToggle.IsOn = !wantOn;
            _suppressToggle = false;
        }
    }
}
