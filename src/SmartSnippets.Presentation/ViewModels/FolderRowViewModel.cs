using CommunityToolkit.Mvvm.ComponentModel;

namespace SmartSnippets.Presentation.ViewModels;

/// <summary>One folder on the Folders page, including its in-place rename state.</summary>
public sealed partial class FolderRowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _name;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SnippetCountDisplay))]
    private int _snippetCount;

    [ObservableProperty]
    private bool _isRenaming;

    /// <summary>The text in the rename box; meaningful while <see cref="IsRenaming"/>.</summary>
    [ObservableProperty]
    private string _editName;

    [ObservableProperty]
    private string _renameError = string.Empty;

    public FolderRowViewModel(long id, string name, int snippetCount)
    {
        Id = id;
        _name = name;
        _editName = name;
        _snippetCount = snippetCount;
    }

    public long Id { get; }

    public string SnippetCountDisplay => SnippetCount == 1 ? "1 snippet" : $"{SnippetCount} snippets";

    partial void OnEditNameChanged(string value) => RenameError = string.Empty;
}
