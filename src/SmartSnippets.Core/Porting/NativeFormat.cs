using System.Text.Json;

namespace SmartSnippets.Core.Porting;

internal static class NativeFormat
{
    internal static ImportParseResult Read(JsonElement root, DateTimeOffset now) => TermFlowFormat.ReadEntries(root, now, true);
}
