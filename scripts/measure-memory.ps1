#Requires -Version 7.2
# Launch ONLY after the operator grants the bottom-left screen slot (mem-01 Phase B).
[CmdletBinding()]
param(
    [string]$Executable = (Join-Path (Split-Path -Parent $PSScriptRoot) 'publish-measure-baseline/SmartSnippets.exe'),
    [Parameter(Mandatory)][switch]$ScreenGranted,
    [string]$CsvPath,
    [string]$Display = '\\.\DISPLAY29',
    [int]$X = -1920, [int]$Y = 1506,
    [ValidateRange(1, 16384)][int]$Width = 1920,
    [ValidateRange(1, 16384)][int]$Height = 1080
)
$ErrorActionPreference = 'Stop'
if (!$ScreenGranted -or !$IsWindows) { throw 'Windows screen grant required' }
$Executable = (Resolve-Path -LiteralPath $Executable).Path
if (!(Test-Path -LiteralPath (Join-Path (Split-Path $Executable) 'measurement-build.txt'))) {
    throw 'Only a measurement publish is allowed'
}
$expectedHash = (Get-Content -LiteralPath (Join-Path (Split-Path $Executable) 'measurement-build.txt') -Raw).Trim()
if ($expectedHash -notmatch '^[0-9A-Fa-f]{64}$' -or
    (Get-FileHash -LiteralPath $Executable -Algorithm SHA256).Hash -ne $expectedHash) {
    throw 'Measurement executable receipt mismatch'
}
$variantFile = Join-Path (Split-Path $Executable) 'measurement-variant.json'
$variant = if (Test-Path -LiteralPath $variantFile) { (Get-Content -LiteralPath $variantFile -Raw | ConvertFrom-Json).Variant } else { 'baseline' }
Add-Type -AssemblyName System.Windows.Forms
function Assert-Monitor {
    $screen = @([Windows.Forms.Screen]::AllScreens | Where-Object {
        $_.DeviceName -eq $Display -and $_.Bounds.X -eq $X -and $_.Bounds.Y -eq $Y -and
        $_.Bounds.Width -eq $Width -and $_.Bounds.Height -eq $Height -and !$_.Primary
    })
    if ($screen.Count -ne 1) {
        $observed = ([Windows.Forms.Screen]::AllScreens | ForEach-Object { "$($_.DeviceName):$($_.Bounds.X),$($_.Bounds.Y),$($_.Bounds.Width),$($_.Bounds.Height),primary=$($_.Primary)" }) -join ';'
        throw "Granted non-primary display absent or physical bounds changed; expected=$Display,$X,$Y,$Width,$Height observed=$observed"
    }
}
Add-Type -TypeDefinition @'
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
public static class MemoryWindows {
    [DllImport("user32.dll")] public static extern IntPtr SetThreadDpiAwarenessContext(IntPtr context);
    public delegate bool EnumProc(IntPtr hwnd, IntPtr data);
    [StructLayout(LayoutKind.Sequential)] public struct Rect { public int L,T,R,B; }
    [DllImport("user32.dll")] static extern bool EnumWindows(EnumProc proc, IntPtr data);
    [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint pid);
    [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr hwnd);
    [DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr hwnd, out Rect rect);
    public static int Check(uint owner, int x, int y, int width, int height) {
        int count = 0;
        bool invalid = false;
        EnumWindows((h,d) => {
            GetWindowThreadProcessId(h, out uint pid);
            if (pid != owner || !IsWindowVisible(h)) return true;
            if (!GetWindowRect(h, out Rect r)) { invalid = true; return false; }
            if (r.R > r.L && r.B > r.T) {
                if (r.L < x || r.T < y || r.R > x + width || r.B > y + height)
                    { invalid = true; return false; }
                count++;
            }
            return true;
        }, IntPtr.Zero);
        if (invalid) throw new InvalidOperationException("owned visible window outside screen grant or bounds unavailable");
        return count;
    }
}
'@
$root = Join-Path ([IO.Path]::GetTempPath()) ('ss-mem-01-' + [guid]::NewGuid().ToString('N'))
$owned = $null
$previousDpi = [MemoryWindows]::SetThreadDpiAwarenessContext([IntPtr]::new(-4))
if ($previousDpi -eq [IntPtr]::Zero) { throw 'Physical-pixel DPI context required' }
$rows = [Collections.Generic.List[object]]::new()
function Assert-Owned {
    if ($owned.HasExited) { throw 'Owned measurement process exited early' }
    Assert-Monitor
    [MemoryWindows]::Check([uint32]$owned.Id, $X, $Y, $Width, $Height)
}
function Wait-Receipt([string]$Name, [string]$Expected, [int]$Seconds = 45) {
    $deadline = [DateTime]::UtcNow.AddSeconds($Seconds)
    $file = Join-Path $root $Name
    do {
        $null = Assert-Owned
        if ((Test-Path -LiteralPath $file) -and (Get-Content -LiteralPath $file -Raw).Trim() -eq $Expected) { return }
        if ((Test-Path -LiteralPath $file) -and (Get-Content -LiteralPath $file -Raw).Trim() -eq 'failed') { throw 'Measurement command failed' }
        Start-Sleep -Milliseconds 100
    } while ([DateTime]::UtcNow -lt $deadline)
    throw 'Measurement receipt timed out'
}
function Command([string]$Value) {
    Remove-Item -LiteralPath (Join-Path $root 'ack') -Force -ErrorAction SilentlyContinue
    $pending = Join-Path $root 'pending'
    [IO.File]::WriteAllText($pending, $Value)
    Move-Item -LiteralPath $pending -Destination (Join-Path $root 'command')
    if ($Value -ne 'exit') { Wait-Receipt 'ack' $Value }
}
function Idle([int]$Seconds) {
    for ($i = 0; $i -lt $Seconds; $i++) { $null = Assert-Owned; Start-Sleep -Seconds 1 }
}
function Sample([string]$State, [bool]$Visible) {
    $windows = Assert-Owned
    if (($Visible -and $windows -eq 0) -or (!$Visible -and $windows -ne 0)) { throw 'Unexpected window visibility' }
    # Query all instances, then match the exact PID token. No matching instance means zero GPU allocation;
    # a missing/unavailable counter is an error, not a fabricated zero.
    $gpu = (Get-Counter -Counter '\GPU Process Memory(*)\Dedicated Usage','\GPU Process Memory(*)\Shared Usage').CounterSamples |
        Where-Object { $_.InstanceName -match "^pid_$($owned.Id)(_|$)" }
    if (@($gpu | Where-Object Status -ne 0).Count) { throw 'Invalid GPU counter sample' }
    $dedicated = ($gpu | Where-Object Path -like '*\dedicated usage' | Measure-Object CookedValue -Sum).Sum
    $shared = ($gpu | Where-Object Path -like '*\shared usage' | Measure-Object CookedValue -Sum).Sum
    $owned.Refresh()
    if (!$Visible -and $variant -in @('trim','gc-trim','trim-release','combined')) {
        if (!(Test-Path -LiteralPath (Join-Path $root 'trim-result')) -or
            (Get-Content -LiteralPath (Join-Path $root 'trim-result') -Raw).Trim() -ne 'ok') { throw 'Idle working-set trim did not succeed' }
    }
    $rows.Add([pscustomobject]@{ Variant=$variant; State=$State; WorkingSetMiB=[math]::Round($owned.WorkingSet64/1MB,2)
        PrivateMiB=[math]::Round($owned.PrivateMemorySize64/1MB,2)
        DedicatedGpuMiB=[math]::Round($dedicated/1MB,2); SharedGpuMiB=[math]::Round($shared/1MB,2) })
}
try {
    Assert-Monitor
    # Verify counter availability before launch. No app/profile/clipboard/registry has been accessed.
    $null = Get-Counter -Counter '\GPU Process Memory(*)\Dedicated Usage','\GPU Process Memory(*)\Shared Usage'
    New-Item -ItemType Directory -Path $root | Out-Null
    [IO.File]::WriteAllText((Join-Path $root 'measurement-owned'), 'mem-01')
    $start = [Diagnostics.ProcessStartInfo]::new($Executable)
    $start.UseShellExecute = $false
    $start.ArgumentList.Add('--background')
    $start.Environment['SMARTSNIPPETS_MEASURE_ROOT'] = $root
    $start.Environment['SMARTSNIPPETS_MEASURE_DISPLAY'] = $Display
    $start.Environment['SMARTSNIPPETS_MEASURE_BOUNDS'] = "$X,$Y,$Width,$Height"
    # NativeAOT GC environment overrides take precedence over compiled runtime knobs. Isolate child only.
    foreach ($key in @('DOTNET_gcServer','COMPlus_gcServer','DOTNET_gcConcurrent','COMPlus_gcConcurrent',
        'DOTNET_GCConserveMemory','COMPlus_GCConserveMemory')) { $null = $start.Environment.Remove($key) }
    $owned = [Diagnostics.Process]::Start($start)
    Wait-Receipt 'ready' ([string]$owned.Id)
    # The measurement-only UI-thread startup seeds exactly 200 synthetic snippets before readiness.
    Idle 20; Sample 'S1 tray 20s' $false
    Command 'main'; Idle 10; Sample 'S2 main 10s' $true
    Command 'hide-main'; Idle 20; Sample 'S3 tray 20s' $false
    Command 'popup'; Idle 5; Sample 'S4 preview 5s' $true
    Command 'hide-popup'; Idle 20; Sample 'S5 tray 20s' $false
    # Same hidden main/popup state as S3; 60 additional idle seconds after S5.
    Idle 60; Sample 'S6 tray +60s' $false
    # Exercise page reconstruction after the final sample, without contaminating S1-S6.
    Command 'main'; Idle 1
    if ((Assert-Owned) -eq 0) { throw 'Main reconstruction failed' }
    Command 'hide-main'
    Command 'exit'
    if (!$owned.WaitForExit(15000)) { throw 'Measurement shutdown timed out' }
    if ($CsvPath) { $rows | Export-Csv -LiteralPath $CsvPath -NoTypeInformation }
    $rows | Format-Table -AutoSize | Out-String | Write-Output
} finally {
    if ($null -ne $owned) {
        if (!$owned.HasExited) { $owned.Kill(); $owned.WaitForExit() }
        $owned.Dispose()
    }
    if (Test-Path -LiteralPath $root) { Remove-Item -LiteralPath $root -Recurse -Force }
    $null = [MemoryWindows]::SetThreadDpiAwarenessContext($previousDpi)
    Write-Output 'cleanup: owned process stopped; throwaway profile deleted; real profile and clipboard never accessed'
}
