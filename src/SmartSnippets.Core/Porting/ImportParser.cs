using System.Text.Json;

namespace SmartSnippets.Core.Porting;

public static class ImportParser
{
    public static ImportParseResult Parse(ReadOnlyMemory<byte> utf8Json, DateTimeOffset now)
    {
        try
        {
            using var doc = JsonDocument.Parse(utf8Json, new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Disallow, AllowTrailingCommas = false });
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object) return ImportParseResult.Fail(new(ImportRefusal.UnknownFormat));
            ImportFormat format;
            string version, entries;
            if (root.TryGetProperty("format", out var marker))
            {
                if (JsonFields.String(root, "format", out var markerText) != JsonFields.Text.Ok || markerText != "smart-snippets") return ImportParseResult.Fail(new(ImportRefusal.UnknownFormat));
                format = ImportFormat.Native; version = "version"; entries = "snippets";
            }
            else if (root.TryGetProperty("snippets", out _)) { format = ImportFormat.TermFlow; version = "version"; entries = "snippets"; }
            else if (root.TryGetProperty("commands", out _)) { format = ImportFormat.Rephlo; version = "version"; entries = "commands"; }
            else if (root.TryGetProperty("Mappings", out _)) { format = ImportFormat.InkSpoke; version = "SchemaVersion"; entries = "Mappings"; }
            else return ImportParseResult.Fail(new(ImportRefusal.UnknownFormat));

            bool supported = root.TryGetProperty(version, out var v) && (format == ImportFormat.Rephlo
                ? JsonFields.String(root, version, out var versionText) == JsonFields.Text.Ok && versionText == "1.3"
                : v.ValueKind == JsonValueKind.Number && v.TryGetInt64(out var n) && n == (format == ImportFormat.InkSpoke ? 2 : 1));
            if (!supported) return ImportParseResult.Fail(new(ImportRefusal.UnsupportedVersion, format, JsonFields.VersionText(root, version)));
            if (!root.TryGetProperty(entries, out var array) || array.ValueKind != JsonValueKind.Array)
                return ImportParseResult.Fail(new(ImportRefusal.MissingEntries, format));
            long count = array.GetArrayLength();
            // Native empty folders also create synchronous store/subscriber work. Count raw array slots
            // before mapping, including malformed / duplicate folder entries; no accumulation can bypass D11.
            if (format == ImportFormat.Native && root.TryGetProperty("folders", out var folders) && folders.ValueKind == JsonValueKind.Array)
                count += folders.GetArrayLength();
            if (count > SnippetFileRules.MaxImportEntries)
                return ImportParseResult.Fail(new(ImportRefusal.TooManyEntries, format, EntryCount: (int)Math.Min(count, int.MaxValue)));
            return format switch
            {
                ImportFormat.Native => NativeFormat.Read(root, now),
                ImportFormat.TermFlow => TermFlowFormat.Read(root, now),
                ImportFormat.Rephlo => RephloFormat.Read(root),
                _ => InkSpokeFormat.Read(root)
            };
        }
        catch (JsonException) { return ImportParseResult.Fail(new(ImportRefusal.InvalidJson)); }
        catch (InvalidOperationException) { return ImportParseResult.Fail(new(ImportRefusal.InvalidJson)); }
    }
}
