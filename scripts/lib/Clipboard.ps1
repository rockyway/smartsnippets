# Clipboard acceptance helpers. These intentionally replace the desktop clipboard; run only in an
# interactive acceptance session. The caller must restore the user's text in a finally block.
. (Join-Path $PSScriptRoot 'Safety.ps1')
if (-not ('VerifyClipboard' -as [type])) {
    Add-Type -TypeDefinition @'
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

public static class VerifyClipboard {
    [DllImport("user32.dll", CharSet=CharSet.Unicode, SetLastError=true)] static extern IntPtr CreateWindowEx(uint ex, string cls, string name, uint style, int x, int y, int w, int h, IntPtr parent, IntPtr menu, IntPtr instance, IntPtr param);
    [DllImport("user32.dll", SetLastError=true)] static extern bool DestroyWindow(IntPtr window);
    [DllImport("user32.dll", SetLastError=true)] static extern bool OpenClipboard(IntPtr owner);
    // A message-only STATIC window is owned by this thread. All payloads are eager HGLOBALs,
    // so the owner can be destroyed after CloseClipboard (no delayed-rendering lifetime).
    static IntPtr NewOwner() {
        var owner = CreateWindowEx(0, "STATIC", "SmartSnippets acceptance clipboard", 0, 0, 0, 0, 0, new IntPtr(-3), IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
        Require(owner != IntPtr.Zero, "Create clipboard owner");
        return owner;
    }
    [DllImport("user32.dll", SetLastError=true)] static extern bool CloseClipboard();
    [DllImport("user32.dll", SetLastError=true)] static extern bool EmptyClipboard();
    [DllImport("user32.dll", SetLastError=true)] static extern uint EnumClipboardFormats(uint format);
    [DllImport("user32.dll", SetLastError=true)] static extern IntPtr GetClipboardData(uint format);
    [DllImport("user32.dll")] static extern bool IsClipboardFormatAvailable(uint format);
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
        var owner = NewOwner();
        try {
            Require(OpenClipboard(owner), "OpenClipboard");
            try {
                Require(EmptyClipboard(), "EmptyClipboard");
                Put(13, Encoding.Unicode.GetBytes(sentinel + "\0"), Encoding.Unicode.GetByteCount(sentinel + "\0"));
                Put(format, Encoding.ASCII.GetBytes("SmartSnippets oversized test payload"), 64 * 1024 * 1024 + 4096);
            } finally { CloseClipboard(); }
        } finally { DestroyWindow(owner); }
    }
    public static string ReadText() {
        Require(OpenClipboard(IntPtr.Zero), "OpenClipboard");
        try {
            if (!IsClipboardFormatAvailable(13)) return null; // successful read, genuinely no Unicode text
            var h = GetClipboardData(13); Require(h != IntPtr.Zero, "GetClipboardData");
            ulong size = GlobalSize(h).ToUInt64(); Require(size >= 2 && size <= Int32.MaxValue && size % 2 == 0, "Text size");
            var p = GlobalLock(h); Require(p != IntPtr.Zero, "GlobalLock");
            try {
                byte[] bytes = new byte[(int)size]; Marshal.Copy(p, bytes, 0, bytes.Length);
                var text = Encoding.Unicode.GetString(bytes);
                int end = text.IndexOf('\0'); Require(end >= 0, "Text terminator");
                return text.Substring(0, end);
            } finally { GlobalUnlock(h); }
        } finally { CloseClipboard(); }
    }
    public static void WriteText(string text) {
        var owner = NewOwner();
        try {
            Require(OpenClipboard(owner), "OpenClipboard");
            try {
                Require(EmptyClipboard(), "EmptyClipboard");
                Put(13, Encoding.Unicode.GetBytes(text + "\0"), Encoding.Unicode.GetByteCount(text + "\0"));
            } finally { CloseClipboard(); }
        } finally { DestroyWindow(owner); }
    }
    static List<string> ReadEntries() {
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
        return entries;
    }
    public static string Fingerprint() {
        var entries = ReadEntries(); entries.Sort(StringComparer.Ordinal);
        return String.Join("|", entries.ToArray());
    }
    public static string OrderedFingerprint() { return String.Join("|", ReadEntries().ToArray()); }
    public static void Clear() {
        var owner = NewOwner();
        try {
            Require(OpenClipboard(owner), "OpenClipboard");
            try { Require(EmptyClipboard(), "EmptyClipboard"); } finally { CloseClipboard(); }
        } finally { DestroyWindow(owner); }
    }
}
'@
}
function Read-ClipboardText { Invoke-ClipboardRetry { [VerifyClipboard]::ReadText() } }
function Set-ClipboardText([AllowEmptyString()][string]$Text) { Invoke-ClipboardRetry { [VerifyClipboard]::WriteText($Text) } }
function Save-ClipboardText {
    # Exceptions are not an empty snapshot. Abort before callers mutate anything on read failure.
    $value = Read-ClipboardText
    return [pscustomobject]@{ Valid = $true; HasText = $null -ne $value; Text = $value }
}
function Restore-ClipboardText($Saved) {
    if (-not (Test-ValidClipboardBackup $Saved)) { throw 'Invalid clipboard backup; refusing to clear or overwrite the clipboard' }
    if ($Saved.HasText) { Set-ClipboardText $Saved.Text } else { Invoke-ClipboardRetry { [VerifyClipboard]::Clear() } }
}
function Set-UnsaveableClipboard([string]$Sentinel = 'SMART-SNIPPETS-CLIPBOARD-SENTINEL') {
    Invoke-ClipboardRetry { [VerifyClipboard]::Install($Sentinel) }
    $script:UnsaveableClipboardFingerprint = Invoke-ClipboardRetry { [VerifyClipboard]::Fingerprint() }
    $entries = $script:UnsaveableClipboardFingerprint -split '\|'
    $script:SavedUnicodeFingerprint = @($entries | Where-Object { $_ -match '^13:' })[0]
    $script:OversizedFormatId = @($entries | Where-Object { [long](($_ -split ':')[1]) -gt 64MB } | ForEach-Object { ($_ -split ':')[0] })[0]
    if (-not $script:SavedUnicodeFingerprint -or -not $script:OversizedFormatId) { throw 'Unsaveable clipboard fixture did not contain expected formats' }
    # Follow TakeSnapshot's enumeration order and cap. This payload contains HGLOBAL text and
    # private formats only (no skipped GDI formats). Save every entry before the cap is reached,
    # not just Unicode, in case Windows enumerates synthesized text ahead of the private format.
    $script:ExpectedPreservedFormats = @()
    $bytes = 0L
    foreach ($entry in ((Invoke-ClipboardRetry { [VerifyClipboard]::OrderedFingerprint() }) -split '\|')) {
        $size = [long](($entry -split ':')[1])
        if ($bytes + $size -gt 64MB) { break }
        $script:ExpectedPreservedFormats += $entry
        $bytes += $size
    }
}
function Test-UnsaveableClipboardRestored {
    # Restoration may legitimately add clipboard-history privacy markers and synthesize text.
    $entries = (Invoke-ClipboardRetry { [VerifyClipboard]::Fingerprint() }) -split '\|'
    if (@($entries | Where-Object { ($_ -split ':')[0] -eq $script:OversizedFormatId }).Count) { return $false }
    foreach ($entry in $script:ExpectedPreservedFormats) { if ($entry -cnotin $entries) { return $false } }
    return $script:ExpectedPreservedFormats.Count -gt 0
}
function Test-ClipboardIntact {
    if (-not $script:UnsaveableClipboardFingerprint) { throw 'Call Set-UnsaveableClipboard first' }
    return (Invoke-ClipboardRetry { [VerifyClipboard]::Fingerprint() }) -ceq $script:UnsaveableClipboardFingerprint
}
function Clear-UnsaveableClipboard {
    Invoke-ClipboardRetry { [VerifyClipboard]::Clear() }
    $script:UnsaveableClipboardFingerprint = $null
}
