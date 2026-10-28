# Isolated visual smoke test. Does not yet implement the complete UI QA matrix.
param([string]$HarnessWorkerConfig = '', [int]$WatchdogTimeoutSeconds = 180, [string[]]$Only = @('UI'))
$ErrorActionPreference = 'Stop'
# Disabled until navigation is converted to posted input, with foreground probes and
# top-left-monitor parking. The previous worker used activating UIA selection patterns.
throw 'UI QA worker requires posted-input and foreground-probe updates before it can run safely.'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
. (Join-Path $PSScriptRoot 'lib/Watchdog.ps1')
if (-not $HarnessWorkerConfig) {
    $arguments = @{} + $PSBoundParameters
    $arguments.Only = $Only
    exit (Invoke-HarnessWorker -ScriptPath $PSCommandPath -Arguments $arguments -TimeoutSeconds $WatchdogTimeoutSeconds)
}
$config = Import-Clixml $HarnessWorkerConfig
$parent = Get-Process -Id $config.ParentId -ErrorAction Stop
if ($parent.StartTime.ToUniversalTime().Ticks -ne $config.ParentStartTicks) { throw 'Invalid supervisor' }
$lease = [IO.File]::Open($config.WorkerLock,'OpenOrCreate','ReadWrite','None')
$script:HarnessProgressPath = $config.Progress
. (Join-Path $PSScriptRoot 'lib/Safety.ps1')
. (Join-Path $PSScriptRoot 'lib/OwnedProcesses.ps1')
. (Join-Path $PSScriptRoot 'lib/Uia.ps1')
Add-Type -TypeDefinition 'using System; using System.Runtime.InteropServices; public static class QaResize { [StructLayout(LayoutKind.Sequential)] public struct Rect { public int Left, Top, Right, Bottom; } [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr h, IntPtr after, int x, int y, int cx, int cy, uint flags); [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out Rect r); [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr h, IntPtr dc, uint flags); }'
function Save-QAScreenshot([IntPtr]$window, [string]$path) {
    $rect = New-Object QaResize+Rect
    if (-not [QaResize]::GetWindowRect($window, [ref]$rect)) { throw 'Window rectangle unavailable' }
    $bitmap = New-Object System.Drawing.Bitmap ($rect.Right-$rect.Left),($rect.Bottom-$rect.Top)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    try {
        $dc = $graphics.GetHdc()
        try { if (-not [QaResize]::PrintWindow($window,$dc,2)) { throw 'PrintWindow failed' } }
        finally { $graphics.ReleaseHdc($dc) }
        $bitmap.Save($path,[System.Drawing.Imaging.ImageFormat]::Png)
        Write-Host "shot $path $($bitmap.Width)x$($bitmap.Height)"
    } finally { $graphics.Dispose(); $bitmap.Dispose() }
}
function ProfileHash($path) {
    if (-not (Test-Path -LiteralPath $path)) { return 'ABSENT' }
    $items = @(Get-ChildItem -LiteralPath $path -File -Recurse -Force | Sort-Object FullName | ForEach-Object { $_.FullName.Substring($path.Length) + ':' + (Get-HarnessFileHash $_.FullName) })
    return ($items -join "`n")
}
$profile = Join-Path $env:LOCALAPPDATA 'SmartSnippets'
$backup = "$profile.verify-backup"
$copy = Join-Path ([IO.Path]::GetTempPath()) ('ss-uiqa-' + $config.RunId)
$out = Join-Path $root ('docs/qa/ui/' + $config.RunId)
$procs = New-Object 'System.Collections.Generic.List[System.Diagnostics.Process]'
$isolated = $false; $had = Test-Path -LiteralPath $profile; $before = $null
try {
    if (Test-Path $backup) { throw 'Existing profile backup: manual recovery required' }
    if (Get-Process -Name 'SmartSnippets.App' -ErrorAction SilentlyContinue) { throw 'Unowned Smart Snippets process: refusing profile isolation' }
    $before = ProfileHash $profile
    $null = Save-HarnessRegistryOriginal $config.RegistryJournal $config.RunId 'Software\Microsoft\Windows\CurrentVersion\Run' 'SmartSnippets'
    New-Item -ItemType Directory -Path $copy,$out | Out-Null
    Copy-Item (Join-Path $root 'publish/*') $copy -Recurse
    if ($had) { Move-Item -LiteralPath $profile -Destination $backup }
    $isolated = $true
    $p = Start-OwnedProcess (Join-Path $copy 'SmartSnippets.App.exe') @() $procs
    $w = Wait-Until { Get-AppWindow $p.Id 'Smart Snippets' } 20000 200
    if (-not $w) { throw 'Main window not found' }
    foreach ($size in @(@(1280,800),@(900,600),@(500,400))) {
        Update-HarnessProgress
        if (-not [QaResize]::SetWindowPos([IntPtr]$w.Current.NativeWindowHandle,[IntPtr]::Zero,30,30,$size[0],$size[1],0x14)) { throw 'Resize failed' }
        Start-Sleep -Milliseconds 800
        foreach ($page in @(@('SnippetsItem','snippets'),@('FoldersItem','folders'),@('SettingsItem','settings'))) {
            Update-HarnessProgress
            $w = Get-AppWindow $p.Id 'Smart Snippets'
            $nav = Find-ById $w $page[0]
            if (-not $nav) { throw "Missing navigation $($page[0])" }
            Select-El $nav
            Start-Sleep -Milliseconds 500
            if ($page[1] -eq 'folders' -and $size[0] -ge 900) {
                $button = Find-ByName (Get-AppWindow $p.Id 'Smart Snippets') 'Create folder' ([Windows.Automation.ControlType]::Button)
                $windowRect = (Get-AppWindow $p.Id 'Smart Snippets').Current.BoundingRectangle
                if (-not $button -or $button.Current.IsOffscreen -or $button.Current.BoundingRectangle.Right -gt $windowRect.Right - 10) { throw 'Folder Create button is clipped' }
            }
            $file = Join-Path $out ("$($page[1])-$($size[0])x$($size[1]).png")
            Save-QAScreenshot ([IntPtr](Get-AppWindow $p.Id 'Smart Snippets').Current.NativeWindowHandle) $file
            if (-not (Test-Path $file)) { throw 'Screenshot failed' }
        }
    }
    Write-Host "Screenshots: $out"
} finally {
    foreach ($p in $procs) { Stop-OwnedProcess $p }
    if (Get-Process -Name 'SmartSnippets.App' -ErrorAction SilentlyContinue) { throw 'App remains running; profile recovery requires manual inspection' }
    $action = Get-ProfileCleanupAction $isolated $had (Test-Path $backup) (Test-Path $profile)
    if ($action -eq 'RestoreIsolated') {
        if (Test-Path $profile) { Remove-Item -LiteralPath $profile -Recurse -Force }
        if ($had) { Move-Item -LiteralPath $backup -Destination $profile }
    } elseif ($action -eq 'MoveBackupBackOnly') { Move-Item -LiteralPath $backup -Destination $profile }
    if ($null -ne $before -and (ProfileHash $profile) -cne $before) { throw 'Profile hash mismatch after recovery' }
    if (Test-Path $copy) { Remove-Item $copy -Recurse -Force }
    $lease.Dispose()
}
Complete-HarnessWorker $config 0
