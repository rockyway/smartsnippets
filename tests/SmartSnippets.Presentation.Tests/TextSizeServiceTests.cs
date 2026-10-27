using SmartSnippets.Core.Settings;
using SmartSnippets.Presentation.Services;

namespace SmartSnippets.Presentation.Tests;

public sealed class TextSizeServiceTests
{
    [Fact]
    public void PresetsMapTo12_14_18()
    {
        var svc = new TextSizeService();

        svc.Apply(TextSizePreset.Small, 20);
        Assert.Equal(12, svc.FontSize);
        svc.Apply(TextSizePreset.Medium, 20);
        Assert.Equal(14, svc.FontSize);
        svc.Apply(TextSizePreset.Large, 20);
        Assert.Equal(18, svc.FontSize);
    }

    [Theory]
    [InlineData(3, 10)]
    [InlineData(10, 10)]
    [InlineData(21.5, 21.5)]
    [InlineData(32, 32)]
    [InlineData(100, 32)]
    [InlineData(double.NaN, 14)]
    [InlineData(double.PositiveInfinity, 14)]
    public void CustomClampedTo10_32(double input, double expected)
    {
        var svc = new TextSizeService();

        svc.Apply(TextSizePreset.Custom, input);

        Assert.Equal(expected, svc.FontSize);
    }

    [Fact]
    public void RaisesPropertyChangedOnce()
    {
        var svc = new TextSizeService();
        var raised = new List<string?>();
        svc.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        svc.Apply(TextSizePreset.Large, 14);
        svc.Apply(TextSizePreset.Large, 30);
        svc.Apply(TextSizePreset.Custom, 18);

        Assert.Equal([nameof(TextSizeService.FontSize)], raised);
    }
}
