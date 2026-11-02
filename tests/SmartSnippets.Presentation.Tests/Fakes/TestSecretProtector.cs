using System.Security.Cryptography;
using SmartSnippets.Core.Security;

namespace SmartSnippets.Presentation.Tests.Fakes;

// Deterministic authenticated test double, never used by production composition.
internal sealed class TestSecretProtector : ISecretProtector
{
    internal bool Fail { get; set; }
    internal bool OddPlaintext { get; set; }
    public byte[] Protect(byte[] plaintext)
    {
        if (Fail) throw new SecretProtectionException();
        var result = new byte[32 + plaintext.Length];
        SHA256.HashData(plaintext).CopyTo(result, 0);
        for (var i = 0; i < plaintext.Length; i++) result[32 + i] = (byte)(plaintext[i] ^ 0xA5);
        return result;
    }
    public byte[] Unprotect(byte[] ciphertext)
    {
        if (Fail || ciphertext.Length < 32) throw new SecretProtectionException();
        var result = new byte[ciphertext.Length - 32];
        for (var i = 0; i < result.Length; i++) result[i] = (byte)(ciphertext[32 + i] ^ 0xA5);
        if (!CryptographicOperations.FixedTimeEquals(SHA256.HashData(result), ciphertext.AsSpan(0, 32)))
        {
            CryptographicOperations.ZeroMemory(result);
            throw new SecretProtectionException();
        }
        return OddPlaintext ? [1] : result;
    }
}
