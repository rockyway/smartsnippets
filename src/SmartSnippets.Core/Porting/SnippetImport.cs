using SmartSnippets.Core.Store;

namespace SmartSnippets.Core.Porting;

public static class SnippetImport
{
    public static ImportSummary Commit(ParsedImport parsed, ISnippetStore store)
    {
        var commit = store.ImportSnippets(parsed.Batch);
        return new(parsed.Format, parsed.EntryCount, commit.SnippetIds.Count, commit.SkippedDuplicates,
            parsed.Skipped.Count, parsed.Rejected.Count + commit.Rejected, commit.FoldersCreated);
    }
}
