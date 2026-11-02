using SmartSnippets.Core.Data;
using SmartSnippets.Core.Logging;

namespace SmartSnippets.Core.Tests.Data;

[Collection("AppLog")]
public sealed class MigratorPrivacyTests : IDisposable
{
    private readonly string _logs = Path.Combine(Path.GetTempPath(), "ss-migration-log-" + Guid.NewGuid().ToString("N"));
    public MigratorPrivacyTests() { AppLog.Initialize(_logs); }
    public void Dispose() { AppLog.Shutdown(); Directory.Delete(_logs, true); }
    private string Log => File.ReadAllText(Path.Combine(_logs, "app.log"));

    [Fact]
    public void Backup_LogsFromVersionWithoutPath()
    {
        using var db = new TempDatabase();
        new Migrator(db.Factory, Migrations.All.Take(1).ToList()).MigrateToLatest();
        new Migrator(db.Factory).MigrateToLatest();
        Assert.Contains("migration_backup from=1", Log);
        Assert.DoesNotContain(db.Factory.DatabasePath, Log);
        Assert.DoesNotContain(".bak", Log);
    }

    [Fact]
    public void FailedBackup_LogsSafeReasonAndRetainsOriginal()
    {
        using var db = new TempDatabase();
        new Migrator(db.Factory, Migrations.All.Take(1).ToList()).MigrateToLatest();
        // A directory at the backup destination deterministically refuses VACUUM INTO.
        Directory.CreateDirectory(db.Factory.DatabasePath + ".v1.bak");
        var ex = Assert.Throws<MigrationException>(() => new Migrator(db.Factory).MigrateToLatest());
        Assert.NotNull(ex.InnerException);
        Assert.Contains("Check available disk space and folder permissions", ex.Message);
        Assert.DoesNotContain(db.Factory.DatabasePath, ex.Message);
        Assert.Contains("migration_failed reason=backup", Log);
        Assert.DoesNotContain(db.Factory.DatabasePath, Log);
        Assert.DoesNotContain(ex.InnerException!.Message, Log);
        Assert.Equal(1L, db.Scalar("SELECT version FROM schema_version"));
    }
}
