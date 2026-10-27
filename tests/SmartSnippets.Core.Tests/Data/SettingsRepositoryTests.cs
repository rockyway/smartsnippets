using SmartSnippets.Core.Data;

namespace SmartSnippets.Core.Tests.Data;

public class SettingsRepositoryTests
{
    [Fact]
    public void Upsert_Overwrites()
    {
        using var db = new TempDatabase();
        new Migrator(db.Factory).MigrateToLatest();
        var repo = new SettingsRepository(db.Factory);

        Assert.Null(repo.Get("theme"));
        repo.Set("theme", "Dark");
        repo.Set("theme", "Light");
        repo.Set("other", "x");

        Assert.Equal("Light", repo.Get("theme"));
        Assert.Equal(1L, db.Scalar("SELECT COUNT(*) FROM settings WHERE key = 'theme'"));
        var all = repo.LoadAll();
        Assert.Equal(2, all.Count);
        Assert.Equal("x", all["other"]);
    }
}
