using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace SmartSnippets.Core.Security;

internal static class SecretEnvelope
{
    private const string Prefix = "dpapi:v1:";

    internal static string Encode(string text, ISecretProtector protector)
    {
        // Explicit little-endian code units, including lone surrogates; no encoding fallback.
        var bytes = MemoryMarshal.AsBytes(text.AsSpan()).ToArray();
        if (!BitConverter.IsLittleEndian) SwapByteOrder(bytes);
        byte[]? protectedBytes = null;
        try
        {
            protectedBytes = protector.Protect(bytes);
            if (protectedBytes.Length == 0) throw new SecretProtectionException();
            return Prefix + Convert.ToBase64String(protectedBytes);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(bytes);
            if (protectedBytes is not null) CryptographicOperations.ZeroMemory(protectedBytes);
        }
    }

    internal static string Decode(string envelope, ISecretProtector protector)
    {
        if (!envelope.StartsWith(Prefix, StringComparison.Ordinal) || envelope.Length == Prefix.Length)
            throw new SecretProtectionException();
        byte[] ciphertext;
        try { ciphertext = Convert.FromBase64String(envelope[Prefix.Length..]); }
        catch (FormatException) { throw new SecretProtectionException(); }
        byte[]? plaintext = null;
        try
        {
            if (ciphertext.Length == 0) throw new SecretProtectionException();
            plaintext = protector.Unprotect(ciphertext);
            if (plaintext.Length % 2 != 0) throw new SecretProtectionException();
            if (!BitConverter.IsLittleEndian) SwapByteOrder(plaintext);
            return new string(MemoryMarshal.Cast<byte, char>(plaintext.AsSpan()));
        }
        finally
        {
            CryptographicOperations.ZeroMemory(ciphertext);
            if (plaintext is not null) CryptographicOperations.ZeroMemory(plaintext);
        }
    }

    private static void SwapByteOrder(byte[] bytes)
    {
        for (var i = 0; i < bytes.Length; i += 2)
            (bytes[i], bytes[i + 1]) = (bytes[i + 1], bytes[i]);
    }
}
