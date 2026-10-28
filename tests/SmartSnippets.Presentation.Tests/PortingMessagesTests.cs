using SmartSnippets.Core.Porting;
using SmartSnippets.Presentation.Text;

namespace SmartSnippets.Presentation.Tests;

public class PortingMessagesTests
{
    [Fact] public void EveryRefusal_HasMessage_EndingNothingWasImported() { foreach (var r in Enum.GetValues<ImportRefusal>()) Assert.EndsWith(" Nothing was imported.", UiText.ImportRefusalMessage(new(r, ImportFormat.TermFlow, "2", 123456))); }
    [Fact] public void RefusalMessages_PairwiseDistinct() { foreach (var f in Enum.GetValues<ImportFormat>()) { var messages = Enum.GetValues<ImportRefusal>().Select(r => UiText.ImportRefusalMessage(new(r, f, "99", 123456))).Append(UiText.ImportRefusalMessage(new(ImportRefusal.UnsupportedVersion, f))).ToArray(); Assert.Equal(messages.Length, messages.Distinct(StringComparer.Ordinal).Count()); } }
    [Theory] [InlineData(ImportFormat.Native, "Smart Snippets", "1")] [InlineData(ImportFormat.TermFlow, "TermFlow", "1")] [InlineData(ImportFormat.Rephlo, "Rephlo", "1.3")] [InlineData(ImportFormat.InkSpoke, "InkSpoke", "2")]
    public void UnsupportedVersion_NamesFormatFoundAndSupported(ImportFormat f, string name, string supported) { Assert.Equal($"This {name} file is version 99. Smart Snippets can import version {supported}. Nothing was imported.", UiText.ImportRefusalMessage(new(ImportRefusal.UnsupportedVersion, f, "99"))); Assert.Equal($"This {name} file has no version number. Smart Snippets can import version {supported}. Nothing was imported.", UiText.ImportRefusalMessage(new(ImportRefusal.UnsupportedVersion, f))); }
    [Theory] [InlineData(ImportFormat.Native, "snippets")] [InlineData(ImportFormat.TermFlow, "snippets")] [InlineData(ImportFormat.Rephlo, "commands")] [InlineData(ImportFormat.InkSpoke, "Mappings")]
    public void MissingEntries_NamesListPerFormat(ImportFormat f, string list) => Assert.Equal($"This {UiText.FormatName(f)} file has no \"{list}\" list. Nothing was imported.", UiText.ImportRefusalMessage(new(ImportRefusal.MissingEntries, f)));
    [Theory] [InlineData(0)] [InlineData(1)] [InlineData(2)] public void Summary_FourLines_Pluralization(int n) { string snippet = n == 1 ? "snippet" : "snippets", duplicate = n == 1 ? "duplicate" : "duplicates", entry = n == 1 ? "entry" : "entries"; Assert.Equal($"Imported {n} {snippet} from TermFlow.\nSkipped {n} {duplicate}.\nSkipped {n} unsupported {entry}.\nRejected {n} malformed {entry}.", UiText.ImportSummary(new(ImportFormat.TermFlow, n * 4, n, n, n, n, 0))); }
    [Fact] public void Summary_FormatNames() { Assert.Equal(new[] { "Smart Snippets", "TermFlow", "Rephlo", "InkSpoke" }, Enum.GetValues<ImportFormat>().Select(UiText.FormatName)); foreach (var f in Enum.GetValues<ImportFormat>()) Assert.Contains($"from {UiText.FormatName(f)}.", UiText.ImportSummary(new(f, 1, 1, 0, 0, 0, 0))); }
    [Theory] [InlineData(0)] [InlineData(1)] [InlineData(2)] public void ExportComplete_Pluralization(int n) => Assert.Equal($"Exported {n} {(n == 1 ? "snippet" : "snippets")} and {n} {(n == 1 ? "folder" : "folders")} to backup.json.", UiText.ExportComplete(n, n, "backup.json"));
    [Fact] public void ButtonNames_AreDistinctFromNewSnippet() { Assert.Equal("Import snippets", UiText.ImportButtonName); Assert.Equal("Export snippets", UiText.ExportButtonName); Assert.Equal(3, new[] { "New snippet", UiText.ImportButtonName, UiText.ExportButtonName }.Distinct().Count()); }
}
