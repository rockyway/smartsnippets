using System.Text.Json;

namespace SmartSnippets.Core.Porting;

internal static class InkSpokeFormat
{
    internal static ImportParseResult Read(JsonElement root)
    {
        var groups = new Dictionary<string, string>(StringComparer.Ordinal);
        if (root.TryGetProperty("Groups", out var array) && array.ValueKind == JsonValueKind.Array)
            foreach (var group in array.EnumerateArray())
                if (group.ValueKind == JsonValueKind.Object && JsonFields.String(group, "Id", out var id) == JsonFields.Text.Ok && JsonFields.String(group, "Name", out var name) == JsonFields.Text.Ok)
                    groups[id!] = name!;
        var candidates = new List<ImportCandidate>();
        var skipped = new List<SkippedEntry>();
        var rejected = new List<RejectedEntry>();
        int index = 0;
        foreach (var entry in root.GetProperty("Mappings").EnumerateArray())
        {
            var reason = ReadEntry(entry, index, groups, out var candidate, out var skip);
            if (reason is not null) rejected.Add(new(index, reason.Value));
            else if (skip is not null) skipped.Add(new(index, skip.Value));
            else candidates.Add(candidate!);
            index++;
        }
        return ImportParseResult.Ok(new(ImportFormat.InkSpoke, index, new(candidates, [], DedupeMode.TextOnly), skipped, rejected));
    }

    private static RejectReason? ReadEntry(JsonElement entry, int index, Dictionary<string, string> groups, out ImportCandidate? candidate, out SkipReason? skip)
    {
        candidate = null; skip = null;
        if (entry.ValueKind != JsonValueKind.Object) return RejectReason.NotAnObject;
        if (JsonFields.String(entry, "ActionType", out var action) != JsonFields.Text.Ok) return RejectReason.WrongFieldType;
        if (action != "SendKeys") { skip = SkipReason.NotSendKeys; return null; }
        if (JsonFields.Bool(entry, "IsParameterized", out var parameterized) == JsonFields.Field.WrongType) return RejectReason.WrongFieldType;
        if (parameterized) { skip = SkipReason.Parameterized; return null; }
        if (JsonFields.String(entry, "ActionParamsJson", out var json) != JsonFields.Text.Ok) { skip = SkipReason.UnparsableParams; return null; }
        JsonDocument doc;
        try { doc = JsonDocument.Parse(json!); }
        catch (JsonException) { skip = SkipReason.UnparsableParams; return null; }
        using (doc)
        {
            var parameters = doc.RootElement;
            if (parameters.ValueKind != JsonValueKind.Object) { skip = SkipReason.UnparsableParams; return null; }
            // The contract skips true flags only; other values do not turn a command into a secret.
            JsonFields.Bool(parameters, "IsSecret", out var secretParams);
            JsonFields.Bool(entry, "IsSecret", out var secretMapping);
            if (secretParams || secretMapping) { skip = SkipReason.Secret; return null; }
            // A JSON null counts as absent, including when deciding whether to use the lowercase fallback.
            var upper = JsonFields.String(parameters, "Text", out _);
            var reason = TermFlowFormat.ReadText(parameters, upper == JsonFields.Text.Absent ? "text" : "Text", out var text);
            if (reason is not null) return reason;
            if (!TermFlowFormat.OptionalString(entry, "SpokenPhrase", out var label)) return RejectReason.WrongFieldType;
            string? folder = null;
            if (JsonFields.String(entry, "GroupId", out var groupId) == JsonFields.Text.Ok) groups.TryGetValue(groupId!, out folder);
            candidate = new(index, text!, label, folder, [], null);
            return null;
        }
    }
}
