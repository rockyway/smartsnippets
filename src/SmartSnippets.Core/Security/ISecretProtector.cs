namespace SmartSnippets.Core.Security;

/// <summary>Protects raw bytes for the current account. Implementations must fail closed.</summary>
public interface ISecretProtector
{
    byte[] Protect(byte[] plaintext);
    byte[] Unprotect(byte[] ciphertext);
}

/// <summary>Deliberately content-free, with no native message or inner exception.</summary>
public sealed class SecretProtectionException : Exception
{
    public SecretProtectionException() : base("Password protection is unavailable or the protected data is invalid.") { }
}

internal sealed class UnavailableSecretProtector : ISecretProtector
{
    internal static readonly UnavailableSecretProtector Instance = new();
    public byte[] Protect(byte[] plaintext) => throw new SecretProtectionException();
    public byte[] Unprotect(byte[] ciphertext) => throw new SecretProtectionException();
}
