# UI Automation + Win32 helpers shared by the verification scripts (Windows PowerShell 5.1).
Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes
Add-Type -AssemblyName System.Windows.Forms
. (Join-Path $PSScriptRoot 'Clipboard.ps1')

if (-not ('Win32Verify' -as [type])) {
Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;
using System.Text;

public static class Win32Verify {
    [DllImport("user32.dll", SetLastError = true)] public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
    [DllImport("user32.dll")] public static extern bool UnregisterHotKey(IntPtr hWnd, int id);
    [DllImport("user32.dll")] public static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);
    [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
    [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr h);
    [StructLayout(LayoutKind.Sequential)] public struct Rect { public int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential)] public struct MonitorInfo { public int Size; public Rect Monitor, Work; public uint Flags; }
    [DllImport("user32.dll")] public static extern IntPtr MonitorFromWindow(IntPtr h, uint flags);
    [DllImport("user32.dll", CharSet=CharSet.Auto)] public static extern bool GetMonitorInfo(IntPtr h, ref MonitorInfo info);
    public static Rect WorkArea(IntPtr window) {
        var info = new MonitorInfo(); info.Size = Marshal.SizeOf(typeof(MonitorInfo));
        if (!GetMonitorInfo(MonitorFromWindow(window, 2), ref info)) throw new InvalidOperationException("GetMonitorInfo failed");
        return info.Work;
    }
    [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern IntPtr FindWindowEx(IntPtr parent, IntPtr after, string cls, string title);
    [DllImport("user32.dll")] public static extern bool PostMessage(IntPtr h, uint msg, IntPtr w, IntPtr l);
    [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] public static extern void mouse_event(uint flags, uint dx, uint dy, uint data, UIntPtr extra);

    [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
    [DllImport("user32.dll")] public static extern uint GetDpiForSystem();
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern IntPtr FindWindowW(string cls, string title);
    [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h, int cmd);
    [DllImport("user32.dll")] public static extern IntPtr SendMessage(IntPtr h, uint msg, IntPtr w, IntPtr l);
    [DllImport("user32.dll", CharSet=CharSet.Unicode, SetLastError=true)] public static extern IntPtr SendMessageTimeout(IntPtr h, uint msg, IntPtr w, string l, uint flags, uint timeout, out IntPtr result);
    [DllImport("user32.dll")] public static extern int GetMenuItemCount(IntPtr hMenu);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern int GetMenuString(IntPtr hMenu, uint id, StringBuilder sb, int max, uint flags);
    [DllImport("user32.dll")] public static extern uint GetMenuItemID(IntPtr hMenu, int pos);
    public static string[] MenuItems(IntPtr menuWnd) {
        IntPtr hm = SendMessage(menuWnd, 0x1E1, IntPtr.Zero, IntPtr.Zero);
        if (hm == IntPtr.Zero) return new string[0];
        int n = GetMenuItemCount(hm);
        var r = new System.Collections.Generic.List<string>();
        for (int i = 0; i < n; i++) { var sb = new StringBuilder(256); GetMenuString(hm, (uint)i, sb, 256, 0x400); r.Add(sb.ToString()); }
        return r.ToArray();
    }
    public const uint KEYUP = 2;
    public static bool ForceForeground(IntPtr h) {
        ShowWindow(h, 9);
        Key(0x12, false); Key(0x12, true);
        SetForegroundWindow(h);
        System.Threading.Thread.Sleep(150);
        return GetForegroundWindow() == h;
    }
    public static void Key(byte vk, bool up) { keybd_event(vk, 0, up ? KEYUP : 0u, UIntPtr.Zero); }
    public static void Chord(params byte[] vks) {
        foreach (var k in vks) Key(k, false);
        System.Threading.Thread.Sleep(40);
        for (int i = vks.Length - 1; i >= 0; i--) Key(vks[i], true);
    }
    public static void Click(int x, int y) {
        SetCursorPos(x, y); System.Threading.Thread.Sleep(150);
        mouse_event(2, 0, 0, 0, UIntPtr.Zero); System.Threading.Thread.Sleep(60);
        mouse_event(4, 0, 0, 0, UIntPtr.Zero);
    }
    public static uint ForegroundPid() { uint pid; GetWindowThreadProcessId(GetForegroundWindow(), out pid); return pid; }
}
'@
}

[void][Win32Verify]::SetProcessDPIAware()
$script:AE = [Windows.Automation.AutomationElement]
$script:TS = [Windows.Automation.TreeScope]

function Get-AppWindows([int]$ProcId) {
    $c = New-Object Windows.Automation.PropertyCondition($script:AE::ProcessIdProperty, $ProcId)
    @($script:AE::RootElement.FindAll($script:TS::Children, $c))
}
function Get-AppWindow([int]$ProcId, [string]$Name) {
    Get-AppWindows $ProcId | Where-Object { $_.Current.Name -eq $Name } | Select-Object -First 1
}
function Find-ById($Root, [string]$Id) {
    if (-not $Root) { return $null }
    $c = New-Object Windows.Automation.PropertyCondition($script:AE::AutomationIdProperty, $Id)
    $Root.FindFirst($script:TS::Descendants, $c)
}
function Find-ByName($Root, [string]$Name, $Type = $null) {
    if (-not $Root) { return $null }
    $c = New-Object Windows.Automation.PropertyCondition($script:AE::NameProperty, $Name)
    if ($Type) {
        $t = New-Object Windows.Automation.PropertyCondition($script:AE::ControlTypeProperty, $Type)
        $c = New-Object Windows.Automation.AndCondition($c, $t)
    }
    $Root.FindFirst($script:TS::Descendants, $c)
}
function Find-All($Root, $Type) {
    $t = New-Object Windows.Automation.PropertyCondition($script:AE::ControlTypeProperty, $Type)
    @($Root.FindAll($script:TS::Descendants, $t))
}
function Wait-Until([scriptblock]$Cond, [int]$TimeoutMs = 5000, [int]$PollMs = 100) {
    $sw = [Diagnostics.Stopwatch]::StartNew()
    while ($sw.ElapsedMilliseconds -lt $TimeoutMs) {
        $r = & $Cond
        if ($r) { return $r }
        Start-Sleep -Milliseconds $PollMs
    }
    return $null
}
function Invoke-El($El) {
    $p = $El.GetCurrentPattern([Windows.Automation.InvokePattern]::Pattern)
    $p.Invoke()
}
function Select-El($El) {
    $p = $El.GetCurrentPattern([Windows.Automation.SelectionItemPattern]::Pattern)
    $p.Select()
}
function Set-Text($El, [string]$Text) {
    # ValuePattern replaces the whole value and keeps CR/LF/spaces exactly.
    $p = $El.GetCurrentPattern([Windows.Automation.ValuePattern]::Pattern)
    $p.SetValue($Text)
}
function Get-Text($El) {
    $p = $El.GetCurrentPattern([Windows.Automation.ValuePattern]::Pattern)
    $p.Current.Value
}
function Toggle-El($El) {
    $p = $El.GetCurrentPattern([Windows.Automation.TogglePattern]::Pattern)
    $p.Toggle()
}
function Get-ToggleState($El) {
    $p = $El.GetCurrentPattern([Windows.Automation.TogglePattern]::Pattern)
    $p.Current.ToggleState.ToString()
}
function Focus-El($El) { $El.SetFocus() }
function Dump-Tree($Root, [int]$Max = 400) {
    $all = $Root.FindAll($script:TS::Descendants, [Windows.Automation.Condition]::TrueCondition)
    $n = 0
    foreach ($e in $all) {
        $n++; if ($n -gt $Max) { break }
        "  [$($e.Current.ControlType.ProgrammaticName -replace 'ControlType.','')] id='$($e.Current.AutomationId)' name='$($e.Current.Name)' enabled=$($e.Current.IsEnabled) off=$($e.Current.IsOffscreen)"
    }
}
function Send-Chord([byte[]]$Keys) { [Win32Verify]::Chord($Keys) }
$VK = @{ Ctrl=0x11; Shift=0x10; Alt=0x12; Space=0x20; Enter=0x0D; Esc=0x1B; Tab=0x09; Delete=0x2E; Down=0x28; Up=0x26; N=0x4E; F=0x46; S=0x53; K=0x4B; J=0x4A; A=0x41; Win=0x5B }

function Get-PopupWindow([int]$ProcId) {
    foreach ($window in (Get-AppWindows $ProcId)) {
        if ([Win32Verify]::IsWindowVisible([IntPtr]$window.Current.NativeWindowHandle) -and (Find-ById $window 'PopupSearch')) { return $window }
    }
    return $null
}

function Get-NotepadText($Window) {
    foreach ($edit in @(@(Find-All $Window ([Windows.Automation.ControlType]::Edit)) + @(Find-All $Window ([Windows.Automation.ControlType]::Document)))) {
        $pattern = $null
        if ($edit.TryGetCurrentPattern([Windows.Automation.ValuePattern]::Pattern, [ref]$pattern)) { return $pattern.Current.Value }
        if ($edit.TryGetCurrentPattern([Windows.Automation.TextPattern]::Pattern, [ref]$pattern)) { return $pattern.DocumentRange.GetText(-1) }
    }
    throw 'Notepad text control does not expose ValuePattern or TextPattern'
}

function Set-Foreground($El) {
    # Brings the window that owns the UIA element to the foreground (Alt tap lifts the foreground lock).
    $h = [IntPtr]$El.Current.NativeWindowHandle
    if ($h -eq [IntPtr]::Zero) { try { $El.SetFocus() } catch { }; return $false }
    return [Win32Verify]::ForceForeground($h)
}
