using Microsoft.UI.Dispatching;
using SmartSnippets.Core.Domain;
using SmartSnippets.Presentation.Navigation;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace SmartSnippets.Uno;

internal sealed partial class UnoAppHost
{
    private DispatcherQueueTimer? _measurementTimer;
    private bool _measurementBusy;
    private void SeedMeasurementProfile()
    {
        if (Store.Snippets.Count != 0) throw new InvalidOperationException("Measurement profile must be empty.");
        for (var i = 1; i <= 200; i++)
            if (!Store.CreateSnippet(new SnippetDraft($"Synthetic measurement text {i:D3}. " + new string('x', 512),
                $"Synthetic {i:D3}", true, null, "synthetic,measurement")).Success)
                throw new InvalidOperationException("Measurement seed failed.");
        SettingsStore.Set(SmartSnippets.Core.Settings.SettingsKeys.ShellCloseHintShown, "1");
    }

    internal void StartMeasurementCommands()
    {
        if (_disposed || !_started) return;
        var root = Measurement.MeasurementProfile.Root;
        _measurementTimer = DispatcherQueue.GetForCurrentThread().CreateTimer();
        _measurementTimer.Interval = TimeSpan.FromMilliseconds(100);
        _measurementTimer.Tick += async (_, _) =>
        {
            ObserveMeasurementIdle();
            var request = Path.Combine(root, "command");
            if (_measurementBusy || !File.Exists(request)) return;
            _measurementBusy = true;
            try
            {
                var command = File.ReadAllText(request).Trim();
                File.Delete(request);
                switch (command)
                {
                    case "main": await _main!.NavigateAsync(PageKind.Snippets, true); break;
                    case "hide-main":
                        PInvoke.ShowWindow(new HWND(_main!.NativeHandle), SHOW_WINDOW_CMD.SW_HIDE);
                        ObserveMeasurementIdle();
                        break;
                    case "popup": ShowPreview(); break; // Existing readiness-gated, target-free, nonactivating preview.
                    case "hide-popup": _popup?.Hide(); break;
                    case "exit": _measurementTimer.Stop(); await ExitAsync(); break;
                    default: throw new InvalidOperationException("Unknown measurement command.");
                }
                File.WriteAllText(Path.Combine(root, "ack"), command);
            }
            catch
            {
                File.WriteAllText(Path.Combine(root, "ack"), "failed");
            }
            finally { _measurementBusy = false; }
        };
        _measurementTimer.Start();
        File.WriteAllText(Path.Combine(root, "ready"), Environment.ProcessId.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }
}
