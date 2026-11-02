using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace SmartSnippets.Uno.Measurement;

internal static class MeasurementScrollState
{
    internal static List<(double Horizontal, double Vertical)> Capture(DependencyObject root) =>
        Viewers(root).Select(view => (view.HorizontalOffset, view.VerticalOffset)).ToList();

    internal static void Restore(FrameworkElement root, List<(double Horizontal, double Vertical)> offsets)
    {
        // Wait for the rebuilt templates and layout before restoring offsets. Only numeric view
        // state is retained, never old UI elements or subscriptions into the retained VMs.
        void Loaded(object sender, RoutedEventArgs args)
        {
            root.Loaded -= Loaded;
            root.DispatcherQueue.TryEnqueue(() =>
            {
                root.UpdateLayout();
                var viewers = Viewers(root).ToArray();
                for (var i = 0; i < Math.Min(viewers.Length, offsets.Count); i++)
                    viewers[i].ChangeView(offsets[i].Horizontal, offsets[i].Vertical, null, disableAnimation: true);
            });
        }
        root.Loaded += Loaded;
    }

    private static IEnumerable<ScrollViewer> Viewers(DependencyObject root)
    {
        if (root is ScrollViewer viewer) yield return viewer;
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
            foreach (var child in Viewers(VisualTreeHelper.GetChild(root, i))) yield return child;
    }
}
