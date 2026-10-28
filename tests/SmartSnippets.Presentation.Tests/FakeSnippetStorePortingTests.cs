using SmartSnippets.Core.Data;
using SmartSnippets.Core.Porting;
using SmartSnippets.Core.Store;
using SmartSnippets.Presentation.Tests.Fakes;

namespace SmartSnippets.Presentation.Tests;

public class FakeSnippetStorePortingTests
{
    [Theory]
    [InlineData("termflow-v1.json")]
    [InlineData("native-v1.json")]
    public void FakeSnippetStore_Import_MatchesRealStoreCounts(string fixture)
    {
        var directory = Path.Combine(Path.GetTempPath(), "ss-parity-" + Guid.NewGuid().ToString("N"));
        try
        {
            var factory = new SqliteConnectionFactory(Path.Combine(directory, "data.db")); new Migrator(factory).MigrateToLatest();
            var real = new SnippetStore(factory, TimeProvider.System); real.Load(); var fake = new FakeSnippetStore();
            var bytes = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Porting", "Fixtures", fixture));
            var parsed = ImportParser.Parse(bytes, DateTimeOffset.UtcNow).Parsed!;
            Assert.Equal(SnippetImport.Commit(parsed, real), SnippetImport.Commit(parsed, fake));
            Assert.Equal(real.Folders.Select(f => f.Name), fake.Folders.Select(f => f.Name));
            Assert.Equal(real.Snippets.Select(s => (s.Snippet.Text, s.Snippet.Label, s.FolderName)), fake.Snippets.Select(s => (s.Snippet.Text, s.Snippet.Label, s.FolderName)));
        }
        finally { Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools(); Directory.Delete(directory, true); }
    }
}
