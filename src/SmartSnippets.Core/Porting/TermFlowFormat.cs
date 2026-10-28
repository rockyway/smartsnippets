using System.Text.Json;

namespace SmartSnippets.Core.Porting;

internal static class TermFlowFormat
{
    internal static ImportParseResult Read(JsonElement root, DateTimeOffset now) => ReadEntries(root, now, false);

    // Native and TermFlow share the text/metadata/stat rules; native alone honours isLabelCustom.
    internal static ImportParseResult ReadEntries(JsonElement root, DateTimeOffset now, bool native)
    {
        var candidates = new List<ImportCandidate>();
        var rejected = new List<RejectedEntry>();
        int index = 0;
        foreach (var entry in root.GetProperty("snippets").EnumerateArray())
        {
            var reason = ReadEntry(entry, index, now, native, out var candidate);
            if (reason is not null) rejected.Add(new(index, reason.Value));
            else candidates.Add(candidate!);
            index++;
        }
        var folders = new List<string>();
        if (native && root.TryGetProperty("folders", out var array) && array.ValueKind == JsonValueKind.Array)
            foreach (var folder in array.EnumerateArray())
                if (folder.ValueKind == JsonValueKind.Object && JsonFields.String(folder, "name", out var name) == JsonFields.Text.Ok)
                    folders.Add(name!);
        return ImportParseResult.Ok(new(native ? ImportFormat.Native : ImportFormat.TermFlow, index,
            new(candidates, folders, native ? DedupeMode.FullRow : DedupeMode.TextOnly), [], rejected));
    }

    private static RejectReason? ReadEntry(JsonElement entry, int index, DateTimeOffset now, bool native, out ImportCandidate? candidate)
    {
        candidate = null;
        if (entry.ValueKind != JsonValueKind.Object) return RejectReason.NotAnObject;
        var textError = ReadText(entry, "text", out var text);
        if (textError is not null) return textError;
        if (!OptionalString(entry, "label", out var label)) return RejectReason.WrongFieldType;
        if (native)
        {
            var field = JsonFields.Bool(entry, "isLabelCustom", out var custom);
            if (field == JsonFields.Field.WrongType) return RejectReason.WrongFieldType;
            if (field == JsonFields.Field.Ok && !custom) label = null;
        }
        if (!OptionalString(entry, "folder", out var folder)) return RejectReason.WrongFieldType;
        if (JsonFields.StringArray(entry, "tags", out var tags) == JsonFields.Field.WrongType) return RejectReason.WrongFieldType;
        var created = JsonFields.EpochMs(entry, "createdAt", now) ?? now;
        var updated = JsonFields.EpochMs(entry, "updatedAt", now) ?? created;
        if (updated < created) updated = created;
        candidate = new(index, text!, label, folder, tags,
            new(created, updated, JsonFields.EpochMs(entry, "lastUsedAt", now), JsonFields.Count(entry, "usageCount")));
        return null;
    }

    internal static bool OptionalString(JsonElement obj, string name, out string? value) =>
        JsonFields.String(obj, name, out value) is JsonFields.Text.Ok or JsonFields.Text.Absent;

    internal static RejectReason? ReadText(JsonElement obj, string name, out string? text)
    {
        var field = JsonFields.String(obj, name, out text);
        return field switch
        {
            JsonFields.Text.Absent => RejectReason.TextMissing,
            JsonFields.Text.WrongType => RejectReason.WrongFieldType,
            JsonFields.Text.InvalidUtf16 => RejectReason.InvalidText,
            _ => string.IsNullOrWhiteSpace(text) ? RejectReason.TextBlank : null
        };
    }
}
