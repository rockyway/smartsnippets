using SmartSnippets.Core.Logging;

namespace SmartSnippets.Core.Tests.Logging;

[Collection("AppLog")]
public sealed class AppLogPrivacyTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "ss-private-" + Guid.NewGuid().ToString("N"));
    public AppLogPrivacyTests() { AppLog.Shutdown(); AppLog.Initialize(_directory); }
    public void Dispose() { AppLog.Shutdown(); Directory.Delete(_directory, true); }

    [Fact]
    public void PasswordFailuresNeverLogSecretOrCiphertext()
    {
        using var db = new SmartSnippets.Core.Tests.Data.TempDatabase();
        new SmartSnippets.Core.Data.Migrator(db.Factory).MigrateToLatest();
        var protector = new TestSecretProtector();
        var store = new SmartSnippets.Core.Store.SnippetStore(db.Factory, TimeProvider.System, protector);
        store.Load();
        var row = store.CreateSnippet(new("PRIVATE-PASSWORD", "manual", true, null, "", true)).Value!;
        var cipher = Assert.IsType<string>(db.Scalar("SELECT text FROM snippets"));
        protector.Fail = true;
        Assert.Throws<SmartSnippets.Core.Store.StoreException>(() => store.UpdateSnippet(row.Id, new("PRIVATE-NEW-PASSWORD", "manual", true, null, "", true)));
        Assert.Throws<SmartSnippets.Core.Store.StoreException>(store.Load);
        var log = File.ReadAllText(Path.Combine(_directory, "app.log"));
        Assert.Contains("store_write_failed", log);
        Assert.DoesNotContain("PRIVATE-PASSWORD", log);
        Assert.DoesNotContain("PRIVATE-NEW-PASSWORD", log);
        Assert.DoesNotContain(cipher, log);
        Assert.DoesNotContain("dpapi:v1:", log);
    }

    [Fact]
    public void Error_PreservesExistingLoadAndPortingDiagnosticFields()
    {
        AppLog.Error("store_write_failed", null, "op=load error=InvalidDataException sqlite=5 hresult=0x80004005");
        AppLog.Error("import_error", null, "stage=in_commit error=InvalidDataException");
        AppLog.Error("export_error", null, "stage=in_write error=TaskCanceledException");
        AppLog.Error("export_error", null, "error=PRIVATE-PAYLOADException path=C:\\PRIVATE-DOCUMENT");
        var log = File.ReadAllText(Path.Combine(_directory, "app.log"));
        Assert.Contains("op=load error=InvalidDataException sqlite=5 hresult=0x80004005", log);
        Assert.Contains("stage=in_commit error=InvalidDataException", log);
        Assert.Contains("stage=in_write error=TaskCanceledException", log);
        Assert.DoesNotContain("PRIVATE-PAYLOAD", log);
        Assert.DoesNotContain("PRIVATE-DOCUMENT", log);
    }

    [Fact]
    public void Error_OmitsMessageInnerExceptionAndUserPath()
    {
        string[] planted = ["PRIVATE-BODY", "PRIVATE-LABEL", "PRIVATE-TAG", "PRIVATE-TITLE", "PRIVATE-QUERY", @"C:\PRIVATE-DOCUMENT\secret.txt"];
        var payload = string.Join(" ", planted);
        AppLog.Error("startup_failed", new InvalidOperationException(payload, new IOException(payload)),
            $"code=5 reason={payload} path={payload}\n at Namespace.Method({payload}):line 42");
        var log = File.ReadAllText(Path.Combine(_directory, "app.log"));
        Assert.Contains("ERROR startup_failed code=5", log);
        Assert.Contains("InvalidOperationException (HResult=0x", log);
        Assert.DoesNotContain("IOException", log);
        foreach (var token in planted)
        {
            Assert.DoesNotContain(token, log);
            // Negative control: the scanner must detect each deliberately planted token.
            Assert.Contains(token, log + payload);
        }
        Assert.DoesNotContain("Namespace.Method", log);
    }
}
