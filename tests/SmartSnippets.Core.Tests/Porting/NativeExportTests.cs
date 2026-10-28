using System.Text;
using System.Text.Json;
using SmartSnippets.Core.Domain;
using SmartSnippets.Core.Porting;
using SmartSnippets.Core.Query;
using Xunit.Abstractions;

namespace SmartSnippets.Core.Tests.Porting;

[Collection("Export allocations")]
public class NativeExportTests(ITestOutputHelper output)
{
    internal static SnippetView View(string text, int usage = 0) => new(new(1, text, "Label", true, null, [], Fixture.Now.AddDays(-3), Fixture.Now.AddDays(-2), usage == 0 ? null : Fixture.Now.AddDays(-1), usage), null);
    internal static NativeExportResult Export(string text = "body") => NativeExport.Write([], [View(text)], Fixture.Now);
    [Fact] public void Export_EnvelopeShape_MarkerVersionArrays() { using var d = JsonDocument.Parse(Export().Utf8Json!); var r = d.RootElement; Assert.Equal("smart-snippets", r.GetProperty("format").GetString()); Assert.Equal(1, r.GetProperty("version").GetInt32()); Assert.Equal(JsonValueKind.Array, r.GetProperty("folders").ValueKind); var s = Assert.Single(r.GetProperty("snippets").EnumerateArray()); Assert.Equal(new[] { "text", "label", "isLabelCustom", "folder", "tags", "usageCount", "createdAt", "updatedAt", "lastUsedAt" }, s.EnumerateObject().Select(p => p.Name)); }
    [Fact] public void Export_IsCompact() { string s = Encoding.UTF8.GetString(Export().Utf8Json!); Assert.DoesNotContain("\n", s); Assert.DoesNotContain(": ", s); }
    [Fact] public void Export_EmptyLibrary_ValidEmptyArrays() { using var d = JsonDocument.Parse(NativeExport.Write([], [], Fixture.Now).Utf8Json!); Assert.Equal(0, d.RootElement.GetProperty("folders").GetArrayLength()); Assert.Equal(0, d.RootElement.GetProperty("snippets").GetArrayLength()); }
    [Fact] public void Export_UnfiledFolderNull_NeverUsedLastUsedNull() { using var d = JsonDocument.Parse(Export().Utf8Json!); var s = d.RootElement.GetProperty("snippets")[0]; Assert.Equal(JsonValueKind.Null, s.GetProperty("folder").ValueKind); Assert.Equal(JsonValueKind.Null, s.GetProperty("lastUsedAt").ValueKind); }
    [Fact] public void Export_DatesAreEpochMs() { using var d = JsonDocument.Parse(Export().Utf8Json!); Assert.Equal(Fixture.Now.ToUnixTimeMilliseconds(), d.RootElement.GetProperty("exportedAt").GetInt64()); Assert.Equal(Fixture.Now.AddDays(-3).ToUnixTimeMilliseconds(), d.RootElement.GetProperty("snippets")[0].GetProperty("createdAt").GetInt64()); }
    [Fact] public void Export_NoBom() => Assert.False(Export().Utf8Json!.AsSpan().StartsWith(new byte[] { 0xef, 0xbb, 0xbf }));
    [Fact] public void Export_LoneSurrogate_WrittenAsReplacementChar() { using var d = JsonDocument.Parse(Export("a\ud800b").Utf8Json!); Assert.Equal("a\ufffdb", d.RootElement.GetProperty("snippets")[0].GetProperty("text").GetString()); }
    [Fact] public void Export_TooManySnippets_RefusedWithoutSerializing() { var r = NativeExport.Write([], new UnreadableList(SnippetFileRules.MaxImportEntries + 1), Fixture.Now); Assert.Equal(ExportRefusal.TooManySnippets, r.Refusal); Assert.Null(r.Utf8Json); }
    [Fact] public void Export_AtEntryLimit_Exports() { var r = NativeExport.Write([], Enumerable.Repeat(View("x"), SnippetFileRules.MaxImportEntries).ToArray(), Fixture.Now); Assert.Null(r.Refusal); Assert.NotEmpty(r.Utf8Json!); }
    [Fact] public void Export_Oversized_StopsAtBound() { var r = Export(new string('é', 34 * 1024 * 1024)); Assert.Equal(ExportRefusal.TooLarge, r.Refusal); Assert.Null(r.Utf8Json); Assert.Equal(SnippetFileRules.MaxImportBytes + 1L, r.Bytes); }
    [Fact]
    public void Export_SingleOversizedToken_PreflightRefusesWithoutLargeAllocations()
    {
        // Allocate the input and warm source-generated metadata before measuring only export work.
        var text = new string('é', 34 * 1024 * 1024);
        var snippets = new[] { View(text) };
        Assert.Null(Export("warmup").Refusal);
        long before = GC.GetTotalAllocatedBytes(precise: true);
        var result = NativeExport.Write([], snippets, Fixture.Now);
        long allocated = GC.GetTotalAllocatedBytes(precise: true) - before;
        output.WriteLine($"Oversized 34 MiB token export allocated {allocated:N0} bytes.");
        Assert.Equal(ExportRefusal.TooLarge, result.Refusal);
        Assert.Null(result.Utf8Json);
        Assert.True(allocated < 100L * 1024 * 1024, $"Oversized token allocated {allocated:N0} bytes during export.");
    }

    [Theory]
    [InlineData("text")]
    [InlineData("label")]
    [InlineData("folder-name")]
    [InlineData("folder-reference")]
    [InlineData("tag")]
    public void Export_PreflightCoversEveryEscapedStringField(string field)
    {
        var text = new string('é', 6 * 1024 * 1024); // 12 MiB raw UTF-8, 36 MiB default-escaped payload.
        var view = View("small");
        Folder[] folders = field == "folder-name" ? [new(1, text, Fixture.Now, Fixture.Now)] : [];
        view = field switch
        {
            "text" => view with { Snippet = view.Snippet with { Text = text } },
            "label" => view with { Snippet = view.Snippet with { Label = text } },
            "folder-reference" => view with { FolderName = text },
            "tag" => view with { Snippet = view.Snippet with { Tags = [text] } },
            _ => view
        };
        var snippets = new[] { view };
        Assert.Null(Export("warmup").Refusal);
        long before = GC.GetTotalAllocatedBytes(precise: true);
        var result = NativeExport.Write(folders, snippets, Fixture.Now);
        long allocated = GC.GetTotalAllocatedBytes(precise: true) - before;
        Assert.Equal(ExportRefusal.TooLarge, result.Refusal);
        Assert.True(allocated < 1024 * 1024, $"Preflight of {field} allocated {allocated:N0} bytes.");
    }

    [Fact]
    public void Export_PreflightSumsStringPayloads()
    {
        var text = new string('é', 3 * 1024 * 1024);
        var snippets = new[] { View(text) with { Snippet = View(text).Snippet with { Label = text } } };
        Assert.Null(Export("warmup").Refusal);
        long before = GC.GetTotalAllocatedBytes(precise: true);
        var result = NativeExport.Write([], snippets, Fixture.Now);
        long allocated = GC.GetTotalAllocatedBytes(precise: true) - before;
        Assert.Equal(ExportRefusal.TooLarge, result.Refusal);
        Assert.True(allocated < 1024 * 1024, $"Combined string preflight allocated {allocated:N0} bytes.");
    }

    private sealed class UnreadableList(int count) : IReadOnlyList<SnippetView> { public int Count => count; public SnippetView this[int index] => throw new InvalidOperationException("Must refuse before enumeration"); public IEnumerator<SnippetView> GetEnumerator() => throw new InvalidOperationException("Must refuse before enumeration"); System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator(); }
}
