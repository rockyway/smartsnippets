using System;
using System.Linq;
using Microsoft.UI.Xaml.Controls;

namespace SmartSnippets.UnoProbe;

public sealed partial class CompanionProbePage : UserControl
{
    public string Details { get; } = string.Join("\n", Enumerable.Repeat("Synthetic companion λ 日本語 😀 — multiline details", 80));
    public CompanionProbePage() => InitializeComponent();
    internal Microsoft.UI.Xaml.FrameworkElement PointerRoot => PointerSurface;
    internal bool IsReady => IsLoaded && Scroller.IsLoaded && Body.IsLoaded && ActualWidth > 0 && ActualHeight > 0 &&
        Body.Text == Details && Body.ActualHeight > 0 && Scroller.ViewportHeight > 0 && Scroller.ExtentHeight > Scroller.ViewportHeight;
    internal void Scroll() => Scroller.ChangeView(null, 80, null, disableAnimation: true);
    internal bool IsScrolled => Scroller.VerticalOffset > 0;
}
