using System.Text;
using SmartSnippets.Core.Porting;

namespace SmartSnippets.Core.Tests.Porting;

internal static class Fixture
{
    internal static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
    internal static string PathFor(string name) => Path.Combine(AppContext.BaseDirectory, "Porting", "Fixtures", name);
    internal static ParsedImport Read(string name) => Parse(File.ReadAllText(PathFor(name)));
    internal static ParsedImport Parse(string json) => ImportParser.Parse(Encoding.UTF8.GetBytes(json), Now).Parsed!;
    internal static ParsedImport TermFlow(string entry) => Parse("{\"version\":1,\"snippets\":[" + entry + "]}");
    internal static ParsedImport Native(string entry, string folders = "[]") => Parse("{\"format\":\"smart-snippets\",\"version\":1,\"folders\":" + folders + ",\"snippets\":[" + entry + "]}");
}
