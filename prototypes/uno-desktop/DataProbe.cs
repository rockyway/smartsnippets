using System;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using Microsoft.Data.Sqlite;
using SmartSnippets.Core.Data;
using SmartSnippets.Core.Domain;
using SmartSnippets.Core.Logging;
using SmartSnippets.Core.Porting;
using SmartSnippets.Core.Store;

namespace SmartSnippets.UnoProbe;

internal static class DataProbe
{
    private const string Text = "BODY_U018_λ😀\nsecond\r\nlast\t updated";
    private const string Label = "LABEL_U018";
    private const string Tags = "TAG_U018,β";
    private static string _stage = "setup";

    public static int Run(string[] args)
    {
        _stage = "setup";
        if (args.Length != 3 || !Guid.TryParseExact(args[2], "N", out var id) ||
            args[1] is not ("seed" or "verify"))
            return 2;
        var root = Path.Combine(Path.GetTempPath(), "SmartSnippets.Uno018-" + id.ToString("N"));
        try
        {
            if (args[1] == "seed")
            {
                if (Directory.Exists(root)) throw new InvalidOperationException();
                CreatePrivateRoot(root);
                File.WriteAllText(Path.Combine(root, ".probe-owned"), id.ToString("N"));
            }
            else
            {
                Require(File.ReadAllText(Path.Combine(root, ".probe-owned")) == id.ToString("N"));
            }
            AppLog.Initialize(Path.Combine(root, "logs"));
            if (args[1] == "seed")
            {
                Seed(root);
                Console.WriteLine("probe_data_seed_ok migration=ok crud=ok import_export=ok failure_paths=ok");
            }
            else
            {
                VerifyRestart(root);
                AppLog.Shutdown();
                SqliteConnection.ClearAllPools();
                Directory.Delete(root, recursive: true);
                Console.WriteLine("probe_data_restart_ok persistence=ok privacy_fixture=ok cleanup=ok");
            }
            return 0;
        }
        catch (Exception ex)
        {
            // Deliberately no exception payload, fixture text, filesystem root or
            // native message. Failed runs retain ONLY this owned synthetic root.
            Console.Error.WriteLine($"probe_data_failed stage={_stage} kind={ex.GetType().Name} synthetic_profile_retained");
            return 1;
        }
        finally
        {
            AppLog.Shutdown();
            SqliteConnection.ClearAllPools();
        }
    }

    private static void Seed(string root)
    {
        _stage = "bootstrap_migration";
        var factory = Factory(root, "fixture.db");
        Require(!File.Exists(factory.DatabasePath));
        Require(new Migrator(factory, Migrations.All.Take(1).ToArray()).MigrateToLatest() == 1);
        using (var connection = factory.Open())
        using (var command = connection.CreateCommand())
        {
            command.CommandText = "INSERT INTO probe(text) VALUES($text)";
            command.Parameters.AddWithValue("$text", "migration_fixture");
            command.ExecuteNonQuery();
        }
        Require(new Migrator(factory).MigrateToLatest() == Migrations.All.Max(m => m.Version));
        Require(File.Exists(factory.DatabasePath + ".v1.bak"));
        using (var connection = factory.Open())
        using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT text FROM probe";
            Require((string?)command.ExecuteScalar() == "migration_fixture");
        }
        _stage = "crud_settings";
        var clock = new ProbeClock();
        var store = new SnippetStore(factory, clock);
        store.Load();
        var folder = store.CreateFolder("FOLDER_U018").Value ?? throw new InvalidOperationException();
        var created = store.CreateSnippet(new SnippetDraft("temporary fixture", Label, true, folder.Id, Tags));
        Require(created.Success && created.Value is not null);
        var snippetId = created.Value!.Id;
        clock.Now = ProbeClock.Start.AddMinutes(1);
        Require(store.UpdateSnippet(snippetId, new SnippetDraft(Text, Label, true, folder.Id, Tags)).Success);
        clock.Now = ProbeClock.Start.AddMinutes(2);
        Require(store.RecordUsage(snippetId).Success);
        Require(store.RenameFolder(folder.Id, "RENAMED_U018").Success);
        new SettingsRepository(factory).Set("probe_key", "probe_value");
        AssertSaved(store);

        _stage = "import_export";
        var exported = NativeExport.Write(store.Folders, store.Snippets, DateTimeOffset.UtcNow);
        Require(exported.Utf8Json is not null && exported.Refusal is null);
        var parsed = ImportParser.Parse(exported.Utf8Json!, DateTimeOffset.UtcNow);
        Require(parsed.Parsed is not null && parsed.Failure is null);
        var copyFactory = Factory(root, "import.db");
        new Migrator(copyFactory).MigrateToLatest();
        var copy = new SnippetStore(copyFactory, TimeProvider.System);
        copy.Load();
        Require(SnippetImport.Commit(parsed.Parsed!, copy).Imported == 1);
        Require(SnippetImport.Commit(parsed.Parsed!, copy).Imported == 0);
        AssertSaved(copy);
        var copyId = copy.Snippets.Single().Snippet.Id;
        Require(copy.DeleteFolder(copy.Folders.Single().Id).Success);
        Require(copy.Snippets.Single().Snippet.FolderId is null);
        Require(copy.DeleteSnippet(copyId).Success && copy.Snippets.Count == 0);

        _stage = "corrupt_database";
        var corrupt = Factory(root, "corrupt.db");
        var bytes = Encoding.UTF8.GetBytes("synthetic non-SQLite fixture");
        File.WriteAllBytes(corrupt.DatabasePath, bytes);
        ExpectFailure(() => new Migrator(corrupt).MigrateToLatest());
        _stage = "corrupt_integrity";
        // A failed SQLite open can still leave a pooled native file handle on
        // Windows. Close owned pools before inspecting bytes, not a data waiver.
        SqliteConnection.ClearAllPools();
        Require(File.ReadAllBytes(corrupt.DatabasePath).SequenceEqual(bytes));
        _stage = "inaccessible_path";
        var inaccessible = Factory(root, "directory.db");
        Directory.CreateDirectory(inaccessible.DatabasePath);
        ExpectFailure(() => new Migrator(inaccessible).MigrateToLatest());
        Require(Directory.Exists(inaccessible.DatabasePath));
        _stage = "permission_denied";
        VerifyDeniedDatabase(root);
        _stage = "newer_schema";
        var newer = Factory(root, "newer.db");
        using (var connection = newer.Open())
        using (var command = connection.CreateCommand())
        {
            command.CommandText = "CREATE TABLE schema_version(version INTEGER NOT NULL); INSERT INTO schema_version VALUES(999);";
            command.ExecuteNonQuery();
        }
        ExpectFailure(() => new Migrator(newer).MigrateToLatest());
        using (var connection = newer.Open())
        using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT version FROM schema_version";
            Require(Convert.ToInt64(command.ExecuteScalar()) == 999);
        }
        _stage = "privacy_fixture";
        VerifyLog(root);
    }

    private static void VerifyRestart(string root)
    {
        _stage = "restart_persistence";
        var factory = Factory(root, "fixture.db");
        Require(File.Exists(factory.DatabasePath));
        Require(new Migrator(factory).MigrateToLatest() == Migrations.All.Max(m => m.Version));
        var store = new SnippetStore(factory, TimeProvider.System);
        store.Load();
        AssertSaved(store);
        Require(new SettingsRepository(factory).Get("probe_key") == "probe_value");
        VerifyLog(root);
    }

    private static void VerifyDeniedDatabase(string root)
    {
        var factory = Factory(root, "denied.db");
        // Force a required write. Opening an already-current SQLite database can
        // legitimately fall back to read-only and is not a migration-denial oracle.
        new Migrator(factory, Migrations.All.Take(1).ToArray()).MigrateToLatest();
        SqliteConnection.ClearAllPools();
        var original = File.ReadAllBytes(factory.DatabasePath);
        if (OperatingSystem.IsWindows()) VerifyWindowsDeniedDatabase(factory);
        else
        {
            var mode = File.GetUnixFileMode(factory.DatabasePath);
            try
            {
                _stage = "permission_apply";
                File.SetUnixFileMode(factory.DatabasePath, UnixFileMode.None);
                _stage = "permission_os_oracle";
                // Independent OS-read oracle: a privileged process bypassing
                // mode bits must fail this test rather than claim denial PASS.
                ExpectFailure(() => File.ReadAllBytes(factory.DatabasePath));
                _stage = "permission_migrator";
                ExpectFailure(() => new Migrator(factory).MigrateToLatest());
            }
            finally
            {
                SqliteConnection.ClearAllPools();
                var failedStage = _stage;
                _stage = "permission_restore";
                File.SetUnixFileMode(factory.DatabasePath, mode);
                Require(File.GetUnixFileMode(factory.DatabasePath) == mode);
                _stage = failedStage;
            }
        }
        _stage = "permission_postcheck";
        Require(File.ReadAllBytes(factory.DatabasePath).SequenceEqual(original));
        using (var connection = factory.Open())
        using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT version FROM schema_version";
            Require(Convert.ToInt64(command.ExecuteScalar()) == 1);
        }
        SqliteConnection.ClearAllPools();
        // A read-only SQLite fallback or retained deny ACE must not pass recovery.
        using (File.Open(factory.DatabasePath, FileMode.Open, FileAccess.ReadWrite)) { }
        Require(new Migrator(factory).MigrateToLatest() == Migrations.All.Max(m => m.Version));
        SqliteConnection.ClearAllPools();
    }

    [SupportedOSPlatform("windows")]
    private static void VerifyWindowsDeniedDatabase(SqliteConnectionFactory factory)
    {
        var file = new FileInfo(factory.DatabasePath);
        var originalSddl = file.GetAccessControl(AccessControlSections.Access)
            .GetSecurityDescriptorSddlForm(AccessControlSections.Access);
        var restricted = file.GetAccessControl();
        var user = WindowsIdentity.GetCurrent().User ?? throw new InvalidOperationException();
        // Deny data access only. The owner retains DACL restoration rights;
        // never deny WRITE_DAC or touch an inherited/user-profile directory.
        restricted.AddAccessRule(new FileSystemAccessRule(user,
            FileSystemRights.WriteData | FileSystemRights.AppendData,
            AccessControlType.Deny));
        try
        {
            _stage = "permission_apply";
            file.SetAccessControl(restricted);
            _stage = "permission_os_oracle";
            ExpectFailure(() => { using var denied = File.Open(factory.DatabasePath, FileMode.Open, FileAccess.ReadWrite); });
            _stage = "permission_migrator";
            ExpectFailure(() => new Migrator(factory).MigrateToLatest());
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            var failedStage = _stage;
            _stage = "permission_restore";
            // GetAccessControl snapshots are unmodified: persisting one is a
            // no-op. Importing SDDL marks Access modified so it really restores.
            var restored = new FileSecurity();
            restored.SetSecurityDescriptorSddlForm(originalSddl, AccessControlSections.Access);
            file.SetAccessControl(restored);
            Require(file.GetAccessControl(AccessControlSections.Access)
                .GetSecurityDescriptorSddlForm(AccessControlSections.Access) == originalSddl);
            _stage = failedStage;
        }
    }

    private static void AssertSaved(SnippetStore store)
    {
        var view = store.Snippets.Single();
        Require(view.Snippet.Text == Text && view.Snippet.Label == Label && view.Snippet.IsLabelCustom);
        Require(view.Snippet.Tags.SequenceEqual(new[] { "TAG_U018", "β" }));
        Require(view.FolderName == "RENAMED_U018" && view.Snippet.UsageCount == 1);
        Require(view.Snippet.CreatedAt == ProbeClock.Start);
        Require(view.Snippet.UpdatedAt == ProbeClock.Start.AddMinutes(1));
        Require(view.Snippet.LastUsedAt == ProbeClock.Start.AddMinutes(2));
    }

    private static void VerifyLog(string root)
    {
        var log = File.ReadAllText(Path.Combine(root, "logs", "app.log"));
        Require(log.Contains("store_loaded", StringComparison.Ordinal));
        foreach (var token in new[] { "BODY_U018", Label, "TAG_U018", "FOLDER_U018", "RENAMED_U018" })
            Require(!log.Contains(token, StringComparison.Ordinal));
    }

    private sealed class ProbeClock : TimeProvider
    {
        public static readonly DateTimeOffset Start = new(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        public DateTimeOffset Now { get; set; } = Start;
        public override DateTimeOffset GetUtcNow() => Now;
    }

    private static SqliteConnectionFactory Factory(string root, string name) => new(Path.Combine(root, name));
    private static void Require(bool condition) { if (!condition) throw new InvalidOperationException("Synthetic data check failed."); }
    private static void ExpectFailure(Action action)
    {
        try { action(); }
        catch (Exception ex) when (ex is SqliteException or MigrationException or IOException or UnauthorizedAccessException) { return; }
        throw new InvalidOperationException("Synthetic failure was not rejected.");
    }

    private static void CreatePrivateRoot(string root)
    {
        if (OperatingSystem.IsWindows()) CreateWindowsRoot(root);
        else Directory.CreateDirectory(root, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
    }

    [SupportedOSPlatform("windows")]
    private static void CreateWindowsRoot(string root)
    {
        Directory.CreateDirectory(root);
        var security = new DirectorySecurity();
        security.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);
        var user = WindowsIdentity.GetCurrent().User ?? throw new InvalidOperationException();
        security.AddAccessRule(new FileSystemAccessRule(user, FileSystemRights.FullControl,
            InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None, AccessControlType.Allow));
        new DirectoryInfo(root).SetAccessControl(security);
    }
}
