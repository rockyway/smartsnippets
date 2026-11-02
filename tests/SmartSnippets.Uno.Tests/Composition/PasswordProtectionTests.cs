using System.Runtime.InteropServices;
using System.Security.Cryptography;
using SmartSnippets.Core.Security;
using SmartSnippets.Core.Store;
using SmartSnippets.Uno.Services;

namespace SmartSnippets.Uno.Tests.Composition;

public sealed class PasswordProtectionTests
{
    [Fact]
    public void DpapiPasswordRoundTrip()
    {
        if (!OperatingSystem.IsWindows()) return;
        var protector = new WindowsSecretProtector();
        // Raw UTF-16 code units: CRLF, Unicode, NUL, and unpaired surrogates.
        var text = " \r\n\u03a9 \ud83d\ude03 \ud800 \udfff\0 ";
        var bytes = MemoryMarshal.AsBytes(text.AsSpan()).ToArray();
        var ciphertext = protector.Protect(bytes);
        var decoded = protector.Unprotect(ciphertext);
        try
        {
            Assert.False(bytes.SequenceEqual(ciphertext));
            Assert.Equal(text, new string(MemoryMarshal.Cast<byte, char>(decoded.AsSpan())));
            Assert.Empty(protector.Unprotect(protector.Protect([])));
        }
        finally
        {
            CryptographicOperations.ZeroMemory(bytes);
            CryptographicOperations.ZeroMemory(decoded);
            CryptographicOperations.ZeroMemory(ciphertext);
        }
    }

    [Fact]
    public void TamperedBlobFailsClosed()
    {
        if (!OperatingSystem.IsWindows()) return;
        var protector = new WindowsSecretProtector();
        var cipher = protector.Protect([1, 2, 3, 4]);
        cipher[^1] ^= 0x80;
        var error = Assert.Throws<SecretProtectionException>(() => protector.Unprotect(cipher));
        Assert.Null(error.InnerException);
        Assert.Equal(new SecretProtectionException().Message, error.Message);
    }

    [Fact]
    public async Task HostComposesProtectorAndShowsSanitizedRecoveryMessage()
    {
        using var fixture = new HostFixture();
        await fixture.Start();
        fixture.Host.Store.CreateSnippet(new("PRIVATE-BODY", "manual", true, null, "", true));
        // Profile path is owned by the test; reload proves the same boundary protector is used.
        fixture.Host.Store.Load();
        fixture.Boundary.Protector.Fail = true;
        Assert.Equal(StoreFailure.SecretProtection, Assert.Throws<StoreException>(fixture.Host.Store.Load).Failure);
    }

    [Fact]
    public async Task FailedPasswordLoadStopsStartupWithoutRewritingRows()
    {
        using var fixture = new HostFixture();
        // Seed via the real composed host, then restart with the same owned profile and failing protector.
        await fixture.Start();
        fixture.Host.Store.CreateSnippet(new("PRIVATE-BODY", "manual", true, null, "", true));
        fixture.Host.Dispose();
        var boundary = new TestBoundary();
        boundary.Protector.Fail = true;
        using var host = new SmartSnippets.Uno.UnoAppHost(boundary, () => SmartSnippets.Uno.AppProfile.OpenOwnedRoot(fixture.Root));
        await host.StartAsync(false, new(null, false, null));
        Assert.Equal(1, boundary.Reports);
        Assert.Contains("original Windows profile/key", boundary.StartupFailure);
        Assert.DoesNotContain("PRIVATE-BODY", boundary.StartupFailure);
        Assert.Equal(1, boundary.Exits);
        Assert.Empty(boundary.Popups);
    }
}
