using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using SmartSnippets.Presentation.Services;

namespace SmartSnippets.App.Controls;

public sealed partial class SnippetRowDetails : UserControl
{
    private readonly Dictionary<TextBlock, HoverDetailsSurface> _hovers = [];
    public SnippetRowDetails()
    {
        InitializeComponent();
        Loaded += (_, _) => { AttachSize(); Bindings.Update(); };
        Unloaded += (_, _) => { DetachSize(); Cancel(); };
    }
    private ITextSizeService? _observedSize;
    private void DetachSize()
    {
        if (_observedSize is not null) _observedSize.PropertyChanged -= OnSizeChanged;
        _observedSize = null;
    }
    private void AttachSize()
    {
        DetachSize();
        if (!IsLoaded) return;
        _observedSize = TextSize;
        if (_observedSize is not null) _observedSize.PropertyChanged += OnSizeChanged;
    }
    private void OnSizeChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        Cancel(); Bindings.Update();
    }
    private static void Changed(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        var row = (SnippetRowDetails)sender;
        row.Cancel();
        if (args.Property == TextSizeProperty) row.AttachSize();
    }
    public static readonly DependencyProperty LabelProperty = DependencyProperty.Register(nameof(Label), typeof(string), typeof(SnippetRowDetails), new PropertyMetadata(string.Empty, Changed));
    public static readonly DependencyProperty FolderNameProperty = DependencyProperty.Register(nameof(FolderName), typeof(string), typeof(SnippetRowDetails), new PropertyMetadata(string.Empty, Changed));
    public static readonly DependencyProperty TagsDisplayProperty = DependencyProperty.Register(nameof(TagsDisplay), typeof(string), typeof(SnippetRowDetails), new PropertyMetadata(string.Empty, Changed));
    public static readonly DependencyProperty ContentTextProperty = DependencyProperty.Register(nameof(ContentText), typeof(string), typeof(SnippetRowDetails), new PropertyMetadata(string.Empty, Changed));
    public static readonly DependencyProperty TextSizeProperty = DependencyProperty.Register(nameof(TextSize), typeof(ITextSizeService), typeof(SnippetRowDetails), new PropertyMetadata(null, Changed));
    public string Label { get => (string)GetValue(LabelProperty); set => SetValue(LabelProperty, value); }
    public string FolderName { get => (string)GetValue(FolderNameProperty); set => SetValue(FolderNameProperty, value); }
    public string TagsDisplay { get => (string)GetValue(TagsDisplayProperty); set => SetValue(TagsDisplayProperty, value); }
    public string ContentText { get => (string)GetValue(ContentTextProperty); set => SetValue(ContentTextProperty, value); }
    public ITextSizeService? TextSize { get => (ITextSizeService?)GetValue(TextSizeProperty); set => SetValue(TextSizeProperty, value); }
    private double MetadataFontSize(double size) => size * 0.85;
    private Visibility HasText(string? value) => string.IsNullOrEmpty(value) ? Visibility.Collapsed : Visibility.Visible;
    private void Cancel()
    {
        foreach (var hover in _hovers.Values) hover.Dispose();
        _hovers.Clear();
    }
    private void OnTargetEntered(object sender, PointerRoutedEventArgs e)
    {
        if (sender is not TextBlock target) return;
        var text = ReferenceEquals(target, LabelText) ? ContentText : ReferenceEquals(target, FolderText) ? FolderName : TagsDisplay;
        if (string.IsNullOrEmpty(text)) return;
        if (!_hovers.TryGetValue(target, out var hover))
        {
            hover = new HoverDetailsSurface(this);
            _hovers.Add(target, hover);
        }
        hover.Request(target, text, e.GetCurrentPoint(XamlRoot.Content).Position,
            isMetadata: !ReferenceEquals(target, LabelText));
    }
    private void OnTargetExited(object sender, PointerRoutedEventArgs e)
    {
        if (sender is TextBlock target && _hovers.TryGetValue(target, out var hover)) hover.TargetExited();
    }
    private void OnRowPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        var point = e.GetCurrentPoint(XamlRoot.Content).Position;
        foreach (var hover in _hovers.Values) hover.UpdatePointer(point);
    }
    private void OnRowEntered(object sender, PointerRoutedEventArgs e)
    {
        foreach (var hover in _hovers.Values) hover.OwnerEntered();
    }
    private void OnRowExited(object sender, PointerRoutedEventArgs e)
    {
        foreach (var hover in _hovers.Values) hover.OwnerExited();
    }
}
