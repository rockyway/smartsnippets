param([Parameter(Mandatory=$true)][int]$OwnedPid,
      [Parameter(Mandatory=$true)][long]$OwnedHwnd,
      [Parameter(Mandatory=$true)][long]$InitialForeground,
      [Parameter(Mandatory=$true)][uint32]$InputEpoch,
      [Parameter(Mandatory=$true)][string]$PrivateRoot,
      [Parameter(Mandatory=$true)][string]$Artifact)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
try {
    if ([Threading.Thread]::CurrentThread.ApartmentState -ne 'STA') { throw 'sta' }
    [void][Reflection.Assembly]::Load('UIAutomationClient, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35')
    [void][Reflection.Assembly]::Load('UIAutomationTypes, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35')
    Add-Type -TypeDefinition @'
using System;
using System.Text;
using System.Runtime.InteropServices;
public static class Owned019 {
 [DllImport("user32.dll")] public static extern bool IsWindow(IntPtr h);
 [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr h);
 [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h, out uint p);
 [DllImport("user32.dll", CharSet=CharSet.Unicode)] public static extern int GetClassName(IntPtr h, StringBuilder b, int n);
 [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
 [DllImport("user32.dll")] public static extern short GetAsyncKeyState(int k);
 [DllImport("user32.dll")] public static extern IntPtr OpenInputDesktop(uint f, bool i, uint a);
 [DllImport("user32.dll", CharSet=CharSet.Unicode)] public static extern bool GetUserObjectInformation(IntPtr h, int i, StringBuilder b, uint n, out uint needed);
 [DllImport("user32.dll")] public static extern bool CloseDesktop(IntPtr h);
 [StructLayout(LayoutKind.Sequential)] public struct Input { public uint size; public uint time; }
 [DllImport("user32.dll")] public static extern bool GetLastInputInfo(ref Input input);
 public static uint Epoch() { var i=new Input {size=8}; if(!GetLastInputInfo(ref i)) throw new Exception(); return i.time; }
}
'@
    $root = [IO.Path]::GetFullPath($PrivateRoot)
    if ($root -cne [Environment]::CurrentDirectory -or [IO.Path]::GetFileName($root) -cnotmatch '^ss-uno018-ui-[a-zA-Z0-9_-]{8,80}$') { throw 'root' }
    $h = [IntPtr]$OwnedHwnd
    $epoch = $InputEpoch
    if ([Owned019]::Epoch() -ne $epoch) { throw 'input' }
    $focused = $false
    $ownedProcess = [Diagnostics.Process]::GetProcessById($OwnedPid)
    try { $started = $ownedProcess.StartTime.ToUniversalTime().Ticks } finally { $ownedProcess.Dispose() }
    function Guard {
        if (-not [Owned019]::IsWindow($h) -or -not [Owned019]::IsWindowVisible($h)) { throw 'window' }
        [uint32]$owner = 0
        [void][Owned019]::GetWindowThreadProcessId($h, [ref]$owner)
        $b = New-Object Text.StringBuilder 256
        [void][Owned019]::GetClassName($h, $b, 256)
        if ($owner -ne $OwnedPid -or $b.ToString() -cne 'WinUIDesktopWin32WindowClass') { throw 'identity' }
        $p = [Diagnostics.Process]::GetProcessById($OwnedPid)
        try {
            if ($p.MainModule.FileName -cne $Artifact -or $p.StartTime.ToUniversalTime().Ticks -ne $started) { throw 'artifact' }
        } finally { $p.Dispose() }
        $d = [Owned019]::OpenInputDesktop(0, $false, 1)
        if ($d -eq [IntPtr]::Zero) { throw 'desktop' }
        try {
            $name = New-Object Text.StringBuilder 256
            [uint32]$needed = 0
            if (-not [Owned019]::GetUserObjectInformation($d, 2, $name, 512, [ref]$needed) -or $name.ToString() -cne 'Default') { throw 'desktop' }
        } finally { if (-not [Owned019]::CloseDesktop($d)) { throw 'desktop_close' } }
        if ([Owned019]::Epoch() -ne $epoch -or ([Environment]::TickCount -band 0xffffffffL) - $epoch -lt 500) { throw 'input' }
        for ($k=1; $k -lt 256; $k++) { if (([int][Owned019]::GetAsyncKeyState($k) -band 0x8000) -ne 0) { throw 'input' } }
        $fg = [Owned019]::GetForegroundWindow().ToInt64()
        if (($script:focused -and $fg -ne $OwnedHwnd) -or (-not $script:focused -and $fg -ne $InitialForeground -and $fg -ne $OwnedHwnd)) { throw 'foreground' }
    }
    function Find([string]$key) {
        Guard
        $r = [Windows.Automation.AutomationElement]::FromHandle($h)
        if ($r.Current.ProcessId -ne $OwnedPid) { throw 'uia_owner' }
        switch -CaseSensitive ($key) {
            'new' { $name='New snippet'; $id=$null; $type=[Windows.Automation.ControlType]::Button }
            'text' { $name='Snippet text'; $id='SnippetTextBox'; $type=[Windows.Automation.ControlType]::Edit }
            'save' { $name='Save snippet'; $id=$null; $type=[Windows.Automation.ControlType]::Button }
            'cancel' { $name='Keep editing'; $id=$null; $type=[Windows.Automation.ControlType]::Button }
            default { throw 'schema' }
        }
        $conditions = @([Windows.Automation.PropertyCondition]::new([Windows.Automation.AutomationElement]::NameProperty,$name),
            [Windows.Automation.PropertyCondition]::new([Windows.Automation.AutomationElement]::ControlTypeProperty,$type))
        if ($id) { $conditions += [Windows.Automation.PropertyCondition]::new([Windows.Automation.AutomationElement]::AutomationIdProperty,$id) }
        $condition = [Windows.Automation.AndCondition]::new([Windows.Automation.Condition[]]$conditions)
        $items = $r.FindAll([Windows.Automation.TreeScope]::Descendants,$condition)
        if ($items.Count -ne 1 -or $items[0].Current.ProcessId -ne $OwnedPid -or $items[0].Current.IsOffscreen -or -not $items[0].Current.IsEnabled) { throw 'schema' }
        return $items[0]
    }
    function Invoke([string]$key) {
        $e = Find $key
        Guard
        ([Windows.Automation.InvokePattern]$e.GetCurrentPattern([Windows.Automation.InvokePattern]::Pattern)).Invoke()
    }
    function Value([string]$value) {
        $e = Find 'text'
        Guard
        $e.SetFocus() # Explicitly authorized management activation; not nonactivation QA.
        $script:focused = $true
        Guard
        if (-not $e.Current.HasKeyboardFocus) { throw 'focus' }
        ([Windows.Automation.ValuePattern]$e.GetCurrentPattern([Windows.Automation.ValuePattern]::Pattern)).SetValue($value)
        Guard
        if (([Windows.Automation.ValuePattern]$e.GetCurrentPattern([Windows.Automation.ValuePattern]::Pattern)).Current.Value -cne $value) { throw 'value' }
    }
    function WaitPhase([string]$phase) {
        $path = Join-Path $root ($phase+'.receipt')
        $until = [DateTime]::UtcNow.AddSeconds(8)
        while (-not [IO.File]::Exists($path)) { Guard; if ([DateTime]::UtcNow -gt $until) { throw 'timeout' }; Start-Sleep -Milliseconds 50 }
        Guard
        $info = Get-Item -LiteralPath $path
        if (($info.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0 -or $info.Length -gt 256) { throw 'receipt' }
        $lines = [IO.File]::ReadAllLines($path)
        if ($lines.Count -ne 3 -or $lines[0] -cne [string]$OwnedPid -or $lines[1] -cne [IO.Path]::GetFileName($root) -or $lines[2] -cne $phase) { throw 'receipt' }
    }
    function Ack([string]$phase) {
        Guard
        $file = [IO.File]::Open((Join-Path $root ($phase+'.continue')), [IO.FileMode]::CreateNew)
        try { $bytes=[Text.Encoding]::ASCII.GetBytes("continue`n"); $file.Write($bytes,0,$bytes.Length) } finally { $file.Dispose() }
    }
    WaitPhase 'ui-ready'
    # Focus only this retained owned window before invoking callbacks.
    $e=Find 'new'; Guard; $e.SetFocus(); $script:focused=$true; Guard
    Invoke 'new'; Start-Sleep -Milliseconds 400; Ack 'ui-ready'
    WaitPhase 'ui-new'
    Value 'ui019 synthetic saved'; Invoke 'save'; Start-Sleep -Milliseconds 700; Ack 'ui-new'
    WaitPhase 'ui-saved'
    Value 'ui019 synthetic dirty'; Invoke 'new'; Start-Sleep -Milliseconds 400
    Invoke 'cancel'; Start-Sleep -Milliseconds 400; Ack 'ui-saved'
    WaitPhase 'ui-cancel'
    Ack 'ui-cancel'
    [Console]::Out.WriteLine('ui_callbacks_observed')
} catch { [Console]::Error.WriteLine('ui_helper_failed'); exit 1 }
