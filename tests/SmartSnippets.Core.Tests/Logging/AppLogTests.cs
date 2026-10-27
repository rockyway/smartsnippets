using SmartSnippets.Core.Logging;

namespace SmartSnippets.Core.Tests.Logging;

[Collection("AppLog")]
public class AppLogTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "ss-log-" + Guid.NewGuid().ToString("N"));

    public AppLogTests()
    {
        AppLog.Shutdown();
    }

    public void Dispose()
    {
        AppLog.Shutdown();
        try
        {
            Directory.Delete(_directory, recursive: true);
        }
        catch (IOException)
        {
        }
    }

    [Fact]
    public void BeforeInitialize_IsNoOp()
    {
        AppLog.Info("early_event", "x");

        Assert.False(Directory.Exists(_directory));
    }

    [Fact]
    public void WritesInfoAndErrorLines()
    {
        AppLog.Initialize(_directory);

        AppLog.Info("hotkey_registered", "ok");
        AppLog.Error("insert_result", new InvalidOperationException("boom"), "code=5");

        var lines = File.ReadAllLines(Path.Combine(_directory, "app.log"))
            .Where(l => l.Contains("hotkey_registered", StringComparison.Ordinal) || l.Contains("insert_result", StringComparison.Ordinal))
            .ToArray();
        Assert.Equal(2, lines.Length);
        Assert.Matches(@"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}\.\d{3}Z INFO hotkey_registered ok$", lines[0]);
        Assert.Contains(" ERROR insert_result code=5 | InvalidOperationException: boom (HResult=0x", lines[1], StringComparison.Ordinal);
    }

    [Fact]
    public void Initialize_CanBeRepointed()
    {
        AppLog.Initialize(_directory);
        var second = Path.Combine(_directory, "second");
        AppLog.Initialize(second);

        AppLog.Info("moved");

        Assert.Contains("moved", File.ReadAllText(Path.Combine(second, "app.log")), StringComparison.Ordinal);
    }
}
