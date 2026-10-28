# Clipboard acceptance helpers. These intentionally replace the desktop clipboard; run only in an
# interactive acceptance session. The caller must restore the user's text in a finally block.
if (-not ('VerifyClipboard' -as [type])) {
    Add-Type -TypeDefinition @'
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

public static class VerifyClipboard {
    [DllImport("user32.dll", SetLastError=true)] static extern bool OpenClipboard(IntPtr owner);
    [DllImport("user32.dll", SetLastError=true)] static extern bool CloseClipboard();
    [DllImport("user32.dll", SetLastError=true)] static extern bool EmptyClipboard();
    [DllImport("user32.dll", SetLastError=true)] static extern uint EnumClipboardFormats(uint format);
    [DllImport("user32.dll", SetLastError=true)] static extern IntPtr GetClipboardData(uint format);
    [DllImport("user32.dll", SetLastError=true)] static extern IntPtr SetClipboardData(uint format, IntPtr memory);
    [DllImport("user32.dll", CharSet=CharSet.Unicode, SetLastError=true)] static extern uint RegisterClipboardFormat(string name);
    [DllImport("kernel32.dll", SetLastError=true)] static extern IntPtr GlobalAlloc(uint flags, UIntPtr bytes);
    [DllImport("kernel32.dll", SetLastError=true)] static extern IntPtr GlobalLock(IntPtr h);
    [DllImport("kernel32.dll", SetLastError=true)] static extern bool GlobalUnlock(IntPtr h);
    [DllImport("kernel32.dll", SetLastError=true)] static extern UIntPtr GlobalSize(IntPtr h);
    [DllImport("kernel32.dll", SetLastError=true)] static extern IntPtr GlobalFree(IntPtr h);
    static void Require(bool ok, string message) { if (!ok) throw new InvalidOperationException(message + ": " + Marshal.GetLastWin32Error()); }
    static void Put(uint format, byte[] bytes, int length) {
        IntPtr h = GlobalAlloc(0x42, (UIntPtr)(uint)length); // moveable, zero-initialized
        Require(h != IntPtr.Zero, "GlobalAlloc");
        try {
            IntPtr p = GlobalLock(h); Require(p != IntPtr.Zero, "GlobalLock");
            try { Marshal.Copy(bytes, 0, p, bytes.Length); } finally { GlobalUnlock(h); }
            Require(SetClipboardData(format, h) != IntPtr.Zero, "SetClipboardData");
            h = IntPtr.Zero; // clipboard owns the allocation
        } finally { if (h != IntPtr.Zero) GlobalFree(h); }
    }
    public static void Install(string sentinel) {
        uint format = RegisterClipboardFormat("SmartSnippets.Acceptance.Unsaveable");
        Require(format != 0, "RegisterClipboardFormat");
        Require(OpenClipboard(IntPtr.Zero), "OpenClipboard");
        try {
            Require(EmptyClipboard(), "EmptyClipboard");
            Put(13, Encoding.Unicode.GetBytes(sentinel + "\0"), Encoding.Unicode.GetByteCount(sentinel + "\0"));
            Put(format, Encoding.ASCII.GetBytes("SmartSnippets oversized test payload"), 64 * 1024 * 1024 + 4096);
        } finally { CloseClipboard(); }
    }
    public static string Fingerprint() {
        var entries = new List<string>();
        Require(OpenClipboard(IntPtr.Zero), "OpenClipboard");
        try {
            uint format = 0;
            while ((format = EnumClipboardFormats(format)) != 0) {
                IntPtr h = GetClipboardData(format); Require(h != IntPtr.Zero, "GetClipboardData");
                ulong n = GlobalSize(h).ToUInt64();
                Require(n > 0 && n <= Int32.MaxValue, "GlobalSize");
                IntPtr p = GlobalLock(h); Require(p != IntPtr.Zero, "GlobalLock");
                try {
                    byte[] bytes = new byte[(int)n]; Marshal.Copy(p, bytes, 0, bytes.Length);
                    using (var hash = SHA256.Create()) entries.Add(format + ":" + n + ":" + BitConverter.ToString(hash.ComputeHash(bytes)));
                } finally { GlobalUnlock(h); }
            }
        } finally { CloseClipboard(); }
        entries.Sort(StringComparer.Ordinal);
        return String.Join("|", entries.ToArray());
    }
    public static void Clear() {
        Require(OpenClipboard(IntPtr.Zero), "OpenClipboard");
        try { Require(EmptyClipboard(), "EmptyClipboard"); } finally { CloseClipboard(); }
    }
}
'@
}
function Save-ClipboardText {
    $value = Get-Clipboard -Raw -ErrorAction SilentlyContinue
    return [pscustomobject]@{ HasText = $null -ne $value; Text = $value }
}
function Restore-ClipboardText($Saved) {
    if ($Saved.HasText) { Set-Clipboard -Value $Saved.Text } else { [VerifyClipboard]::Clear() }
}
function Set-UnsaveableClipboard([string]$Sentinel = 'SMART-SNIPPETS-CLIPBOARD-SENTINEL') {
    [VerifyClipboard]::Install($Sentinel)
    $script:UnsaveableClipboardFingerprint = [VerifyClipboard]::Fingerprint()
    $entries = $script:UnsaveableClipboardFingerprint -split '\|'
    $script:SavedUnicodeFingerprint = @($entries | Where-Object { $_ -match '^13:' })[0]
    $script:OversizedFormatId = @($entries | Where-Object { [long](($_ -split ':')[1]) -gt 64MB } | ForEach-Object { ($_ -split ':')[0] })[0]
    if (-not $script:SavedUnicodeFingerprint -or -not $script:OversizedFormatId) { throw 'Unsaveable clipboard fixture did not contain expected formats' }
}
function Test-UnsaveableClipboardRestored {
    # Install orders Unicode before the oversized private format. TakeSnapshot saves Unicode,
    # then reaches the cap; synthesized legacy text formats after that are not preserved entries.
    # Restoration may legitimately add clipboard-history privacy markers and synthesize text.
    $entries = [VerifyClipboard]::Fingerprint() -split '\|'
    return $script:SavedUnicodeFingerprint -cin $entries -and -not [bool]@($entries | Where-Object { ($_ -split ':')[0] -eq $script:OversizedFormatId }).Count
}
function Test-ClipboardIntact {
    if (-not $script:UnsaveableClipboardFingerprint) { throw 'Call Set-UnsaveableClipboard first' }
    return [VerifyClipboard]::Fingerprint() -ceq $script:UnsaveableClipboardFingerprint
}
function Clear-UnsaveableClipboard {
    [VerifyClipboard]::Clear()
    $script:UnsaveableClipboardFingerprint = $null
}
