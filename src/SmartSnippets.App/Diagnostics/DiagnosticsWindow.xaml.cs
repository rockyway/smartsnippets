using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml;

namespace SmartSnippets.App.Diagnostics;

public sealed partial class DiagnosticsWindow : Window
{
    public DiagnosticsWindow()
    {
        InitializeComponent();
        DynamicCodeText.Text = $"Dynamic code supported: {RuntimeFeature.IsDynamicCodeSupported}";
        RuntimeText.Text = $".NET: {Environment.Version}";
    }
}
