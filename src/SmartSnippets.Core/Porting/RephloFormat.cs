using System.Text.Json;

namespace SmartSnippets.Core.Porting;

internal static class RephloFormat
{
    internal static ImportParseResult Read(JsonElement root)
    {
        var candidates = new List<ImportCandidate>();
        var skipped = new List<SkippedEntry>();
        var rejected = new List<RejectedEntry>();
        int index = 0;
        foreach (var entry in root.GetProperty("commands").EnumerateArray())
        {
            var reason = ReadEntry(entry, index, out var candidate, out var skip);
            if (reason is not null) rejected.Add(new(index, reason.Value));
            else if (skip is not null) skipped.Add(new(index, skip.Value));
            else candidates.Add(candidate!);
            index++;
        }
        return ImportParseResult.Ok(new(ImportFormat.Rephlo, index, new(candidates, [], DedupeMode.TextOnly), skipped, rejected));
    }

    private static RejectReason? ReadEntry(JsonElement entry, int index, out ImportCandidate? candidate, out SkipReason? skip)
    {
        candidate = null; skip = null;
        if (entry.ValueKind != JsonValueKind.Object) return RejectReason.NotAnObject;
        if (JsonFields.Bool(entry, "isArchived", out var archived) == JsonFields.Field.WrongType) return RejectReason.WrongFieldType;
        if (archived) { skip = SkipReason.Archived; return null; }
        var reason = TermFlowFormat.ReadText(entry, "instruction", out var text);
        if (reason is not null) return reason;
        if (!TermFlowFormat.OptionalString(entry, "name", out var label) || !TermFlowFormat.OptionalString(entry, "groupName", out var folder)) return RejectReason.WrongFieldType;
        candidate = new(index, text!, label, folder, [], null);
        return null;
    }
}
