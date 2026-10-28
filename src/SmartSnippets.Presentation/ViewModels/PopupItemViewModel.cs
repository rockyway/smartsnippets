using SmartSnippets.Presentation.Services;

namespace SmartSnippets.Presentation.ViewModels;

public enum PopupItemKind { Heading, Snippet }

public sealed record PopupItemViewModel(PopupItemKind Kind, long SnippetId, string Label, string Preview,
    string FolderChip, string Tag1, string Tag2, string HeadingText)
{
    public ITextSizeService? TextSize { get; init; }

    public override string ToString()
    {
        if (Kind == PopupItemKind.Heading)
        {
            var index = HeadingText.LastIndexOf(" (", StringComparison.Ordinal);
            return index >= 0 && HeadingText.EndsWith(')')
                ? $"{HeadingText[..index]}, {HeadingText[(index + 2)..^1]} {(HeadingText[(index + 2)..^1] == "1" ? "snippet" : "snippets")}"
                : HeadingText;
        }

        return string.Join(", ", new[] { Label, FolderChip, Tag1, Tag2 }.Where(s => !string.IsNullOrEmpty(s)));
    }
}

public enum PopupOpenMode { Fresh, Resumed, Ignored }
public enum PopupFooterState { None, Error, ConfirmClipboardLoss, ManualPaste, MaybePasted }
