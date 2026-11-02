using System.Security.Cryptography;
using SmartSnippets.Core.Security;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Security.Cryptography;

namespace SmartSnippets.Uno.Services;

/// <summary>Current-user DPAPI only; no UI, user-derived descriptions, or machine scope.</summary>
internal sealed class WindowsSecretProtector : ISecretProtector
{
    private static readonly byte[] Entropy = "SmartSnippets/password/dpapi/v1"u8.ToArray();

    public byte[] Protect(byte[] plaintext) => Transform(plaintext, protect: true);
    public byte[] Unprotect(byte[] ciphertext) => Transform(ciphertext, protect: false);

    private static unsafe byte[] Transform(byte[] input, bool protect)
    {
        if (!OperatingSystem.IsWindowsVersionAtLeast(5, 1, 2600)) throw new SecretProtectionException();
        // DPAPI requires a non-null input pointer even for an empty payload.
        byte dummy = 0;
        fixed (byte* data = input)
        fixed (byte* entropy = Entropy)
        {
            var source = new CRYPT_INTEGER_BLOB { cbData = (uint)input.Length, pbData = input.Length == 0 ? &dummy : data };
            var purpose = new CRYPT_INTEGER_BLOB { cbData = (uint)Entropy.Length, pbData = entropy };
            CRYPT_INTEGER_BLOB output = default;
            try
            {
                var ok = protect
                    ? PInvoke.CryptProtectData(&source, default, &purpose, null, null, PInvoke.CRYPTPROTECT_UI_FORBIDDEN, &output)
                    : PInvoke.CryptUnprotectData(&source, null, &purpose, null, null, PInvoke.CRYPTPROTECT_UI_FORBIDDEN, &output);
                if (!ok) throw new SecretProtectionException();
                return new ReadOnlySpan<byte>(output.pbData, checked((int)output.cbData)).ToArray();
            }
            finally
            {
                if (output.pbData is not null)
                {
                    CryptographicOperations.ZeroMemory(new Span<byte>(output.pbData, checked((int)output.cbData)));
                    PInvoke.LocalFree(new HLOCAL(output.pbData));
                }
            }
        }
    }
}
