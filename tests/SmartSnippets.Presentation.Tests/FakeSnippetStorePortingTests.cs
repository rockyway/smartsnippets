using System.Text;
using SmartSnippets.Core.Data;
using SmartSnippets.Core.Porting;
using SmartSnippets.Core.Store;
using SmartSnippets.Presentation.Tests.Fakes;

namespace SmartSnippets.Presentation.Tests;

public class FakeSnippetStorePortingTests
{
    [Theory]
    [InlineData("{\"version\":1,\"snippets\":[{\"text\":\"Alpha\",\"folder\":\"Work\",\"usageCount\":7},{\"text\":\"Alpha\",\"folder\":\"Novel\"},{\"text\":\"Bravo\"},{\"text\":42}]}")]
    [InlineData("{\"format\":\"smart-snippets\",\"version\":1,\"folders\":[{\"name\":\"Empty\"}],\"snippets\":[{\"text\":\"hello\",\"folder\":\"Work\"},{\"text\":\"hello\",\"folder\":\"Home\"},{\"text\":\"hello\",\"folder\":\"work\"}]}")]
    public void FakeSnippetStore_Import_MatchesRealStoreCounts(string json)
    {
        var directory = Path.Combine(Path.GetTempPath(), "ss-parity-" + Guid.NewGuid().ToString("N"));
        try
        {
            var factory = new SqliteConnectionFactory(Path.Combine(directory, "data.db")); new Migrator(factory).MigrateToLatest();
            var real = new SnippetStore(factory, TimeProvider.System); real.Load(); var fake = new FakeSnippetStore();
            var parsed = ImportParser.Parse(Encoding.UTF8.GetBytes(json), DateTimeOffset.UtcNow).Parsed!;
            Assert.Equal(SnippetImport.Commit(parsed, real), SnippetImport.Commit(parsed, fake));
            Assert.Equal(real.Folders.Select(f => f.Name), fake.Folders.Select(f => f.Name));
            Assert.Equal(real.Snippets.Select(s => (s.Snippet.Text, s.Snippet.Label, s.FolderName)), fake.Snippets.Select(s => (s.Snippet.Text, s.Snippet.Label, s.FolderName)));
        }
        finally { Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools(); Directory.Delete(directory, true); }
    }
}
