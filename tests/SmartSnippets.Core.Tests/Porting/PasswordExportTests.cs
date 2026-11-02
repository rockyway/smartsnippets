using System.Text;
using System.Text.Json;
using SmartSnippets.Core.Porting;

namespace SmartSnippets.Core.Tests.Porting;

public class PasswordExportTests
{
    private static SmartSnippets.Core.Query.SnippetView Password(string text)
    {
        var view = NativeExportTests.View(text);
        return view with { Snippet = view.Snippet with { IsPassword = true } };
    }

    [Fact]
    public void ExcludesBeforeSizingAndSerialization()
    {
        var huge = Password(new string('x', SnippetFileRules.MaxImportBytes + 1));
        var result = NativeExport.Write([], [NativeExportTests.View("ordinary"), huge], Fixture.Now);
        Assert.Null(result.Refusal);
        Assert.Equal(1, result.SnippetCount);
        Assert.Equal(1, result.ExcludedPasswordCount);
        Assert.DoesNotContain("xxxx", Encoding.UTF8.GetString(result.Utf8Json!));
        using var json = JsonDocument.Parse(result.Utf8Json!);
        Assert.Equal("ordinary", Assert.Single(json.RootElement.GetProperty("snippets").EnumerateArray()).GetProperty("text").GetString());
    }

    [Fact]
    public void AllPasswordsProducesValidEmptySnippets()
    {
        var result = NativeExport.Write([new(1, "Folder", Fixture.Now, Fixture.Now)], [Password("secret")], Fixture.Now);
        Assert.Null(result.Refusal);
        Assert.Equal(0, result.SnippetCount);
        Assert.Equal(1, result.ExcludedPasswordCount);
        using var json = JsonDocument.Parse(result.Utf8Json!);
        Assert.Equal(1, json.RootElement.GetProperty("version").GetInt32());
        Assert.Single(json.RootElement.GetProperty("folders").EnumerateArray());
        Assert.Empty(json.RootElement.GetProperty("snippets").EnumerateArray());
    }

    [Fact]
    public void FolderCapPrecedesSnippetEnumeration()
    {
        var folders = Enumerable.Repeat(new SmartSnippets.Core.Domain.Folder(1, "f", Fixture.Now, Fixture.Now), SnippetFileRules.MaxImportEntries + 1).ToArray();
        var result = NativeExport.Write(folders, new UnreadableSnippets(), Fixture.Now);
        Assert.Equal(ExportRefusal.TooManyEntries, result.Refusal);
    }

    private sealed class UnreadableSnippets : IReadOnlyList<SmartSnippets.Core.Query.SnippetView>
    {
        public int Count => throw new InvalidOperationException();
        public SmartSnippets.Core.Query.SnippetView this[int index] => throw new InvalidOperationException();
        public IEnumerator<SmartSnippets.Core.Query.SnippetView> GetEnumerator() => throw new InvalidOperationException();
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }

    [Fact]
    public void EntryCapCountsOnlyOrdinaryRows()
    {
        var ordinary = NativeExportTests.View("x");
        var rows = Enumerable.Repeat(ordinary, SnippetFileRules.MaxImportEntries).Append(Password("secret")).ToArray();
        var result = NativeExport.Write([], rows, Fixture.Now);
        Assert.Null(result.Refusal);
        Assert.Equal(SnippetFileRules.MaxImportEntries, result.SnippetCount);
        Assert.Equal(1, result.ExcludedPasswordCount);
        result = NativeExport.Write([], rows.Append(ordinary).ToArray(), Fixture.Now);
        Assert.Equal(ExportRefusal.TooManySnippets, result.Refusal);
        Assert.Equal(1, result.ExcludedPasswordCount);
    }
}
