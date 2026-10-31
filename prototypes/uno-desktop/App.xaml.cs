using System;
using System.Linq;
using Microsoft.UI.Xaml;

namespace SmartSnippets.UnoProbe;

public partial class App : Application
{
    private Window? _window;
    private LifecycleProbe? _lifecycle;
    private CoordinatedPopupProbe? _coordinator;
    private PreviewProbe? _preview;
    private CompanionSurfaceProbe? _companionProbe;
    private UiFlowProbe? _uiFlow;

    public App() => InitializeComponent();

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        // Merely building/publishing this probe must not authorize a GUI launch.
        if (!Environment.GetCommandLineArgs().Contains("--authorized-probe-ui", StringComparer.Ordinal))
        {
            Exit();
            return;
        }

        var page = new MainPage();
        _window = new Window { Content = page };
#if WINDOWS
        ProbeWindowPlacement.Park(_window, 640, 520);
#endif
        if (Environment.GetCommandLineArgs().Contains("--probe-ui-flow", StringComparer.Ordinal))
            _uiFlow = new UiFlowProbe(_window);
        else if (Environment.GetCommandLineArgs().Contains("--companion-native-pointer", StringComparer.Ordinal))
            _companionProbe = new CompanionSurfaceProbe(this, _window, page, nativePointer: true);
        else if (Environment.GetCommandLineArgs().Contains("--probe-ui-companion", StringComparer.Ordinal))
            _companionProbe = new CompanionSurfaceProbe(this, _window, page);
        else if (Environment.GetCommandLineArgs().Contains("--preview-row-native", StringComparer.Ordinal))
            _preview = new PreviewProbe(this, _window, page, rowMode: true, nativeRow: true);
        else if (Environment.GetCommandLineArgs().Contains("--preview-row", StringComparer.Ordinal))
            _preview = new PreviewProbe(this, _window, page, rowMode: true);
        else if (Environment.GetCommandLineArgs().Contains("--probe-ui-preview", StringComparer.Ordinal))
            _preview = new PreviewProbe(this, _window, page);
        else if (Environment.GetCommandLineArgs().Contains("--probe-ui-coordinator", StringComparer.Ordinal))
            _coordinator = new CoordinatedPopupProbe(this, _window, page);
        else if (Environment.GetCommandLineArgs().Contains("--probe-ui-lifecycle", StringComparer.Ordinal))
            _lifecycle = new LifecycleProbe(this, _window, page);
        _window.Activate();
    }
}
