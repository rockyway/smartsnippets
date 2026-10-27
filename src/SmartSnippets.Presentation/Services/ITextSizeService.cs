using System.ComponentModel;
using SmartSnippets.Core.Settings;

namespace SmartSnippets.Presentation.Services;

/// <summary>Font size for snippet lists and editable content; views bind <see cref="FontSize"/> with x:Bind.</summary>
public interface ITextSizeService : INotifyPropertyChanged
{
    double FontSize { get; }

    void Apply(TextSizePreset preset, double customSize);
}
