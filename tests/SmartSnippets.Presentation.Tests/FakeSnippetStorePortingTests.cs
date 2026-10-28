using SmartSnippets.Core.Data;
using SmartSnippets.Core.Query;
using SmartSnippets.Core.Porting;
using SmartSnippets.Core.Store;
using SmartSnippets.Presentation.Tests.Fakes;

namespace SmartSnippets.Presentation.Tests;

public class FakeSnippetStorePortingTests
{
    [Theory]
    [InlineData("termflow-v1.json")]
    [InlineData("native-v1.json")]
    [InlineData("rephlo-1.3.json")]
    [InlineData("inkspoke-v2.json")]
    public void FakeSnippetStore_Import_MatchesRealStoreCounts(string fixture)
    {
        var directory = Path.Combine(Path.GetTempPath(), "ss-parity-" + Guid.NewGuid().ToString("N"));
        try
        {
            var factory = new SqliteConnectionFactory(Path.Combine(directory, "data.db")); new Migrator(factory).MigrateToLatest();
            var real = new SnippetStore(factory, new FirstFakeMutationTime()); real.Load(); var fake = new FakeSnippetStore();
            var bytes = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Porting", "Fixtures", fixture));
            var parsed = ImportParser.Parse(bytes, new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)).Parsed!;
            Assert.Equal(SnippetImport.Commit(parsed, real), SnippetImport.Commit(parsed, fake));
            // Reopen SQLite so the oracle covers persisted rows, not just the real store's own cache.
            var persisted = new SnippetStore(factory, new FirstFakeMutationTime()); persisted.Load();
            Assert.Equal(persisted.Folders.Select(f => f.Name), fake.Folders.Select(f => f.Name));
            Assert.Equal(persisted.Snippets.Select(Project), fake.Snippets.Select(Project), new RowComparer());
        }
        finally { Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools(); Directory.Delete(directory, true); }
    }

    private sealed record Row(string Text, string Label, bool IsLabelCustom, string? FolderName,
        IReadOnlyList<string> Tags, int UsageCount, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt, DateTimeOffset? LastUsedAt);
    private static Row Project(SnippetView view)
    {
        var s = view.Snippet;
        return new(s.Text, s.Label, s.IsLabelCustom, view.FolderName, s.Tags, s.UsageCount, s.CreatedAt, s.UpdatedAt, s.LastUsedAt);
    }
    private sealed class RowComparer : IEqualityComparer<Row>
    {
        public bool Equals(Row? a, Row? b) => a is not null && b is not null &&
            a with { Tags = b.Tags } == b && a.Tags.SequenceEqual(b.Tags, StringComparer.Ordinal);
        public int GetHashCode(Row row) => HashCode.Combine(row.Text, row.Label, row.FolderName);
    }
    private sealed class FirstFakeMutationTime : TimeProvider
    {
        // FakeSnippetStore starts at midnight and advances once for the atomic import, not once per row.
        public override DateTimeOffset GetUtcNow() => new(2026, 1, 1, 0, 1, 0, TimeSpan.Zero);
    }
}
