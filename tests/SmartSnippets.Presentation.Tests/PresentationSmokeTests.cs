using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SmartSnippets.Presentation.Tests;

public sealed partial class PresentationSmokeTests
{
    [Fact]
    public void ToolkitObservablePropertyRaisesChanged()
    {
        var model = new Probe();
        var raised = new List<string?>();
        ((INotifyPropertyChanged)model).PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        model.Name = "x";

        Assert.Equal(["Name"], raised);
    }

    private sealed partial class Probe : ObservableObject
    {
        [ObservableProperty]
        private string _name = string.Empty;
    }
}
