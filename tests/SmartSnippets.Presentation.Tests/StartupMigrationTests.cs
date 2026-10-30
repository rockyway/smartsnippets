using Microsoft.Win32;
using SmartSnippets.App.Services;

namespace SmartSnippets.Presentation.Tests;

// These tests use RegistryValueKind constants only, not the Windows registry.
#pragma warning disable CA1416

public class StartupMigrationTests
{
    private sealed class FakeRegistry : IStartupMigrationRegistry
    {
        public object? Value;
        public RegistryValueKind? Kind;
        public bool FailRead;
        public bool FailWrite;
        public int Writes;
        public (object? Value, RegistryValueKind? Kind) Read() => FailRead ? throw new IOException("private token") : (Value, Kind);
        public void Write(string command)
        {
            if (FailWrite) throw new IOException("private token");
            Writes++;
            Value = command;
            Kind = RegistryValueKind.String;
        }
    }

    [Fact]
    public void MigratesOnlyCanonicalLegacyCommandAndIsIdempotent()
    {
        var exe = Path.Combine(Path.GetTempPath(), "space ü", "SmartSnippets.exe");
        var old = Path.Combine(Path.GetDirectoryName(exe)!, "SmartSnippets.App.exe");
        var registry = new FakeRegistry { Value = $"\"{old.ToUpperInvariant()}\" --background", Kind = RegistryValueKind.String };
        Assert.Equal(StartupMigrationResult.Migrated, StartupMigration.Run(registry, exe));
        Assert.Equal($"\"{exe}\" --background", registry.Value);
        Assert.Equal(StartupMigrationResult.Unchanged, StartupMigration.Run(registry, exe));
        Assert.Equal(1, registry.Writes);
    }

    [Fact]
    public void LeavesForeignAndNoncanonicalValuesUntouched()
    {
        var exe = Path.Combine(Path.GetTempPath(), "SmartSnippets.exe");
        var old = Path.Combine(Path.GetDirectoryName(exe)!, "SmartSnippets.App.exe");
        foreach (var (value, kind) in new (object?, RegistryValueKind?)[] {
            (null, null), ($"\"{old}\" --background", RegistryValueKind.ExpandString),
            (1, RegistryValueKind.DWord), ($"{old} --background", RegistryValueKind.String),
            ($"\"{old}\" --background extra", RegistryValueKind.String),
            ($"\"{old}.other\" --background", RegistryValueKind.String),
            ($"\"{Path.Combine(Path.GetDirectoryName(exe)!, "foreign", "SmartSnippets.App.exe")}\" --background", RegistryValueKind.String),
            ($"\"{exe}\" --background", RegistryValueKind.String) })
        {
            var registry = new FakeRegistry { Value = value, Kind = kind };
            Assert.Equal(StartupMigrationResult.Unchanged, StartupMigration.Run(registry, exe));
            Assert.Equal(0, registry.Writes);
            Assert.Equal(value, registry.Value);
        }
    }

    [Fact]
    public void CoordinatorRetainsFailureAndDeliversFallbackWithoutLeakingException()
    {
        var registry = new FakeRegistry { FailRead = true };
        var logs = new List<string>();
        var result = StartupMigrationCoordinator.Start(registry, Path.Combine(Path.GetTempPath(), "SmartSnippets.exe"), logs.Add);
        Assert.Equal(StartupMigrationResult.Failed, result);
        Assert.DoesNotContain(logs, item => item.Contains("private token", StringComparison.Ordinal));
        var notifications = 0;
        var fallbacks = 0;
        StartupMigrationCoordinator.Deliver(result, () => { notifications++; return false; }, () => fallbacks++);
        Assert.Equal(1, notifications);
        Assert.Equal(1, fallbacks);
        StartupMigrationCoordinator.Deliver(result, () => true, () => fallbacks++);
        StartupMigrationCoordinator.Deliver(StartupMigrationResult.Unchanged, () => { notifications++; return true; }, () => fallbacks++);
        Assert.Equal(1, notifications);
        Assert.Equal(1, fallbacks);
    }

    [Fact]
    public void WindowsAdapterUsesRawValueAndKindInDisposableSubkey()
    {
        if (!OperatingSystem.IsWindows()) return;
        var keyPath = @"Software\SmartSnippets.Tests\Migration-" + Guid.NewGuid().ToString("N");
        try
        {
            using (var key = Registry.CurrentUser.CreateSubKey(keyPath))
                key.SetValue("SmartSnippets", "%TEMP%", RegistryValueKind.ExpandString);
            var adapter = new WindowsStartupMigrationRegistry(keyPath);
            var (value, kind) = adapter.Read();
            Assert.Equal("%TEMP%", value);
            Assert.Equal(RegistryValueKind.ExpandString, kind);
            adapter.Write("test command");
            (value, kind) = adapter.Read();
            Assert.Equal("test command", value);
            Assert.Equal(RegistryValueKind.String, kind);
        }
        finally { Registry.CurrentUser.DeleteSubKeyTree(keyPath, throwOnMissingSubKey: false); }
    }

    [Fact]
    public void FailuresAreNonfatalAndDoNotChangeRegistration()
    {
        var exe = Path.Combine(Path.GetTempPath(), "SmartSnippets.exe");
        var old = $"\"{Path.Combine(Path.GetDirectoryName(exe)!, "SmartSnippets.App.exe")}\" --background";
        var registry = new FakeRegistry { Value = old, Kind = RegistryValueKind.String, FailRead = true };
        Assert.Equal(StartupMigrationResult.Failed, StartupMigration.Run(registry, exe));
        registry.FailRead = false;
        registry.FailWrite = true;
        Assert.Equal(StartupMigrationResult.Failed, StartupMigration.Run(registry, exe));
        Assert.Equal(old, registry.Value);
        Assert.Equal(StartupMigrationResult.Failed, StartupMigration.Run(registry, null));
        Assert.Equal(0, registry.Writes);
    }
}
