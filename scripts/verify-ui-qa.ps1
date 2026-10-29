# Isolated visual smoke test. Does not yet implement the complete UI QA matrix.
param([string]$HarnessWorkerConfig = '', [int]$WatchdogTimeoutSeconds = 180, [string[]]$Only = @('UI'), [ValidateSet('System','Light','Dark')][string]$Theme = 'System', [ValidateRange(0,32)][int]$CustomSize = 0, [switch]$CaptureOnly = $true, [switch]$MeasureOnly, [switch]$ProbePopupStyle)
$ErrorActionPreference = 'Stop'
if (-not $CaptureOnly) { throw 'Only the no-input capture mode is supported' }
trap { Write-Warning ("UI QA worker failed: " + $_.Exception.Message + ' at ' + $_.ScriptStackTrace); break }
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
function Update-HarnessProgress {
    for ($i = 0; $i -lt 20; $i++) {
        try { [IO.File]::WriteAllText($script:HarnessProgressPath, [guid]::NewGuid().ToString('N')); return }
        catch [IO.IOException] { Start-Sleep -Milliseconds 25 }
    }
    throw 'Could not update watchdog progress'
}
. (Join-Path $PSScriptRoot 'lib/PostedInput.ps1')
. (Join-Path $PSScriptRoot 'lib/Safety.ps1')
. (Join-Path $PSScriptRoot 'lib/OwnedProcesses.ps1')
. (Join-Path $PSScriptRoot 'lib/Uia.ps1')
Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;
public static class QaResize {
    [StructLayout(LayoutKind.Sequential)] public struct Rect { public int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential)] public struct Point { public int X, Y; }
    [StructLayout(LayoutKind.Sequential)] private struct MinMax { public Point Reserved, MaxSize, MaxPosition, MinTrack, MaxTrack; }
    [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr h, IntPtr after, int x, int y, int cx, int cy, uint flags);
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out Rect r);
    [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr h, IntPtr dc, uint flags);
    [DllImport("user32.dll")] public static extern bool ShowWindowAsync(IntPtr h, int cmd);
    [DllImport("user32.dll", EntryPoint="GetWindowLongPtrW")] public static extern IntPtr GetWindowStyle(IntPtr h, int index);
    [DllImport("user32.dll")] public static extern uint GetDpiForWindow(IntPtr h);
    [DllImport("user32.dll")] private static extern int GetSystemMetricsForDpi(int index, uint dpi);
    [DllImport("user32.dll", SetLastError=true)] private static extern IntPtr SendMessageTimeout(IntPtr h, uint msg, IntPtr w, IntPtr l, uint flags, uint timeout, out IntPtr result);
    public static Point MinimumTrackSize(IntPtr hwnd) {
        uint dpi = GetDpiForWindow(hwnd);
        var m = new MinMax { MinTrack = new Point { X = GetSystemMetricsForDpi(34, dpi), Y = GetSystemMetricsForDpi(35, dpi) } };
        var ptr = Marshal.AllocHGlobal(Marshal.SizeOf<MinMax>());
        try {
            Marshal.StructureToPtr(m, ptr, false);
            IntPtr result;
            if (SendMessageTimeout(hwnd, 0x24, IntPtr.Zero, ptr, 0x22, 2000, out result) == IntPtr.Zero)
                throw new InvalidOperationException("Could not query minimum track size");
            return Marshal.PtrToStructure<MinMax>(ptr).MinTrack;
        } finally { Marshal.FreeHGlobal(ptr); }
    }
}
'@
$script:QaFailures = New-Object 'System.Collections.Generic.List[string]'
$script:PopupCaptionWarned = $false
function Assert-OnDisplay9([IntPtr]$window) {
    $rect = New-Object QaResize+Rect
    if (-not [QaResize]::GetWindowRect($window, [ref]$rect)) { throw 'Window rectangle unavailable' }
    if ($rect.Left -lt -1920 -or $rect.Top -lt 1506 -or $rect.Right -gt 0 -or $rect.Bottom -gt 2538 -or $rect.Right -le $rect.Left -or $rect.Bottom -le $rect.Top) {
        throw 'Owned window is outside DISPLAY9 work area; capture refused'
    }
    return $rect
}
function Save-QAScreenshot([IntPtr]$window, [string]$path) {
    $rect = Assert-OnDisplay9 $window
    $bitmap = New-Object System.Drawing.Bitmap ($rect.Right-$rect.Left),($rect.Bottom-$rect.Top)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    try {
        $dc = $graphics.GetHdc()
        try { if (-not [QaResize]::PrintWindow($window,$dc,2)) { throw 'PrintWindow failed' } }
        finally { $graphics.ReleaseHdc($dc) }
        $bitmap.Save($path,[System.Drawing.Imaging.ImageFormat]::Png)
        if ([IO.Path]::GetFileName($path) -like 'popup-*-Light-*' -and $bitmap.Width -gt 120 -and $bitmap.Height -gt 180) {
            $pixel = $bitmap.GetPixel([int]($bitmap.Width / 2), $bitmap.Height - 100)
            if ($pixel.R -lt 100 -and $pixel.G -lt 100 -and $pixel.B -lt 100) { $script:QaFailures.Add('Light popup background is dark or PrintWindow could not capture it'); Write-Warning $script:QaFailures[-1] }
        }
        Write-Host "shot $path $($bitmap.Width)x$($bitmap.Height)"
    } finally { $graphics.Dispose(); $bitmap.Dispose() }
}
function Request-QAActivation([string[]]$arguments) {
    $before = [Win32Verify]::GetForegroundWindow()
    $redirect = Start-OwnedProcess (Join-Path $copy 'SmartSnippets.App.exe') $arguments $procs
    if (-not (Wait-OwnedSecondLaunch $redirect 10000) -or $redirect.ExitCode -ne 0) { throw 'Owned activation redirect failed' }
    Start-Sleep -Milliseconds 600
    $after = [Win32Verify]::GetForegroundWindow()
    if ($before -cne $after) { throw 'Foreground changed during activation redirect' }
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
    # Start hidden so the main window cannot appear on an unauthorized monitor.
    $p = Start-OwnedProcess (Join-Path $copy 'SmartSnippets.App.exe') @('--background') $procs
    $mainHwnd = Wait-Until { try { [SmartSnippetsPostedInput]::FindOwnedWindow($p.Id, 'Smart Snippets', $null) } catch { $null } } 20000 200
    if (-not $mainHwnd) { [SmartSnippetsPostedInput]::DescribeOwnedWindows($p.Id) | ForEach-Object { Write-Host $_ }; throw 'Owned hidden main window not found' }
    if ($CaptureOnly) {
        # Seed only the isolated profile while the owned app is stopped.
        Stop-OwnedProcess $p
        $db = Join-Path $profile 'smartsnippets.db'
        & sqlite3 -batch $db "INSERT INTO settings(key,value) VALUES('theme','$Theme') ON CONFLICT(key) DO UPDATE SET value=excluded.value; INSERT INTO folders(name,name_key,created_at,updated_at) VALUES('QA folder','QA FOLDER',1700000000000,1700000000000); INSERT INTO snippets(text,label,label_is_custom,folder_id,tags,created_at,updated_at,usage_count) VALUES('Synthetic one','First synthetic',1,1,'work,qa',1700000000000,1700000000000,3),('Synthetic two','Second synthetic',1,NULL,'qa',1700000000000,1700000000000,0),('Synthetic three','Third synthetic with a deliberately very long label that must truncate cleanly on one row',1,1,'sample,extra-long-tag',1700000000000,1700000000000,1);"
        if ($LASTEXITCODE -ne 0) { throw 'Synthetic fixture seeding failed' }
        if ($CustomSize -gt 0) {
            if ($CustomSize -lt 10) { throw 'Custom size must be 10 through 32' }
            & sqlite3 -batch $db "INSERT INTO settings(key,value) VALUES('text_size.preset','Custom'),('text_size.custom','$CustomSize') ON CONFLICT(key) DO UPDATE SET value=excluded.value;"
            if ($LASTEXITCODE -ne 0) { throw 'Text size fixture seeding failed' }
        }
        $p = Start-OwnedProcess (Join-Path $copy 'SmartSnippets.App.exe') @('--background') $procs
        $mainHwnd = Wait-Until { try { [SmartSnippetsPostedInput]::FindOwnedWindow($p.Id, 'Smart Snippets', $null) } catch { $null } } 20000 200
        if (-not $mainHwnd) { throw 'Fixture app main window not found' }
    }
    $messageHwnd = Wait-Until { try { [SmartSnippetsPostedInput]::FindOwnedWindow($p.Id, $null, 'SmartSnippets.MessageWindow') } catch { $null } } 10000 200
    if (-not $messageHwnd) { throw 'Owned tray callback window not found' }
    # Probe only the foreground HWND; do not inspect another app's process or title.
    # Only DISPLAY9's work area is authorized; never park on the top-left monitor.
    $screen = [System.Windows.Forms.Screen]::AllScreens | Where-Object {
        $_.Bounds.X -eq -1920 -and $_.Bounds.Y -eq 1506 -and $_.Bounds.Width -eq 1920 -and $_.Bounds.Height -eq 1080 -and
        $_.WorkingArea.X -eq -1920 -and $_.WorkingArea.Y -eq 1506 -and $_.WorkingArea.Width -eq 1920 -and $_.WorkingArea.Height -eq 1032
    } | Select-Object -First 1
    if (-not $screen) { throw 'DISPLAY9 bounds/work area do not match authorization; refusing UI operations' }
    $parkBefore = [Win32Verify]::GetForegroundWindow()
    for ($attempt = 0; $attempt -lt 2; $attempt++) {
        if (-not [QaResize]::SetWindowPos([IntPtr]$mainHwnd,[IntPtr]::Zero,-1880,1546,900,600,0x14)) { throw 'Could not position owned app on DISPLAY9' }
        Start-Sleep -Milliseconds 300
    }
    $parkAfter = [Win32Verify]::GetForegroundWindow()
    if ($parkBefore -cne $parkAfter) { throw 'Foreground changed during positioning' }
    $null = Assert-OnDisplay9 ([IntPtr]$mainHwnd)
    $showBefore = [Win32Verify]::GetForegroundWindow()
    # SW_SHOWNOACTIVATE is window placement, not input; unlike the tray callback it
    # does not ask the app to activate and must preserve the existing foreground.
    $null = [QaResize]::ShowWindowAsync([IntPtr]$mainHwnd, 4)
    $w = Wait-Until { Get-AppWindow $p.Id 'Smart Snippets' } 10000 200
    if (-not $w) { throw 'Non-activating show did not expose main window' }
    $showAfter = [Win32Verify]::GetForegroundWindow()
    if ($showBefore -cne $showAfter) { throw 'Foreground changed during non-activating show' }
    $null = Assert-OnDisplay9 ([IntPtr]$mainHwnd)
    $minimum = [QaResize]::MinimumTrackSize([IntPtr]$mainHwnd)
    if ($minimum.X -le 0 -or $minimum.Y -le 0 -or $minimum.X -gt 1840 -or $minimum.Y -gt 952) { throw 'Invalid or unsupported minimum track size' }
    Write-Host "Window minimum track size: $($minimum.X)x$($minimum.Y)"
    if ($minimum.X -lt 640 -or $minimum.Y -lt 480) { $script:QaFailures.Add('Main window minimum track size is below 640x480 physical pixels'); Write-Warning $script:QaFailures[-1] }
    foreach ($size in @(@(1280,800),@(900,600),@($minimum.X,$minimum.Y))) {
        $isMinimum = $size[0] -eq $minimum.X -and $size[1] -eq $minimum.Y
        Update-HarnessProgress
        $resizeBefore = [Win32Verify]::GetForegroundWindow()
        if (-not [QaResize]::SetWindowPos([IntPtr]$w.Current.NativeWindowHandle,[IntPtr]::Zero,-1880,1546,$size[0],$size[1],0x14)) { throw 'Resize failed' }
        $resizeAfter = [Win32Verify]::GetForegroundWindow()
        if ($resizeBefore -cne $resizeAfter) { throw 'Foreground changed during resize' }
        Start-Sleep -Milliseconds 800
        $actualRect = Assert-OnDisplay9 ([IntPtr]$w.Current.NativeWindowHandle)
        if (($actualRect.Right - $actualRect.Left) -ne $size[0] -or ($actualRect.Bottom - $actualRect.Top) -ne $size[1]) {
            if (-not $isMinimum) { throw "Requested $($size[0])x$($size[1]); actual $(($actualRect.Right-$actualRect.Left))x$(($actualRect.Bottom-$actualRect.Top))" }
            Write-Warning "Minimum track query and actual resize differ; actual $(($actualRect.Right-$actualRect.Left))x$(($actualRect.Bottom-$actualRect.Top)) is a candidate, not proven interactive minimum"
            $size = @(($actualRect.Right-$actualRect.Left), ($actualRect.Bottom-$actualRect.Top))
        }
        if ($CaptureOnly) {
            $fixtureRows = (& sqlite3 -batch (Join-Path $profile 'smartsnippets.db') "SELECT count(*) FROM snippets; SELECT value FROM settings WHERE key='theme'; SELECT value FROM settings WHERE key='text_size.preset'; SELECT value FROM settings WHERE key='text_size.custom';")
            if ($LASTEXITCODE -ne 0 -or $fixtureRows[0] -ne '3' -or $fixtureRows[1] -ne $Theme) { throw 'Fixture database read-back failed' }
            if ($CustomSize -gt 0 -and ($fixtureRows[2] -ne 'Custom' -or $fixtureRows[3] -ne "$CustomSize")) { throw 'Text size fixture did not persist' }
            Write-Host "Verified synthetic rows=$($fixtureRows[0]), theme=$Theme, customSize=$CustomSize"
            if ($MeasureOnly) {
                if ($isMinimum) { continue }
                if ($ProbePopupStyle) {
                    Request-QAActivation @('--preview-popup')
                    $popup = Wait-Until { Get-PopupWindow $p.Id } 8000 100
                    if (-not $popup) { throw 'Preview popup not found for style probe' }
                    $ph = [IntPtr]$popup.Current.NativeWindowHandle
                    $null = Assert-OnDisplay9 $ph
                    $style = [QaResize]::GetWindowStyle($ph, -16).ToInt64()
                    $extendedStyle = [QaResize]::GetWindowStyle($ph, -20).ToInt64()
                    Write-Host ("Owned popup WS_CAPTION=$([bool]($style -band 0x00C00000)); WS_EX_TOPMOST=$([bool]($extendedStyle -band 8)); style=0x{0:X}" -f $style)
                    continue
                }
                $label = Find-ByName (Get-AppWindow $p.Id 'Smart Snippets') 'First synthetic' ([Windows.Automation.ControlType]::Text)
                if (-not $label) { throw 'Synthetic label not found for text size measurement' }
                Write-Host "Synthetic label bounding height=$($label.Current.BoundingRectangle.Height)"
                continue
            }
            foreach ($targetPage in @(@('snippets','SnippetList'), @('folders','FolderList'), @('settings','TextSizeRadios'))) {
                Request-QAActivation @('--page', $targetPage[0])
                $shown = Wait-Until { Find-ById (Get-AppWindow $p.Id 'Smart Snippets') $targetPage[1] } 8000 100
                if (-not $shown) { throw "Redirect did not navigate to $($targetPage[0])" }
                if ($targetPage[0] -eq 'snippets' -and -not $isMinimum) {
                    $visibleText = @(Find-All $shown ([Windows.Automation.ControlType]::Text) | ForEach-Object { $_.Current.Name })
                    if ('Synthetic one' -in $visibleText -or 'Synthetic two' -in $visibleText -or 'Synthetic three' -in $visibleText) { throw 'Snippet bodies appeared as separate list rows' }
                    if ('First synthetic' -notin $visibleText) { throw 'Synthetic row label missing' }
                    if ($CustomSize -gt 0) {
                        $label = Find-ByName $shown 'First synthetic' ([Windows.Automation.ControlType]::Text)
                        $dpiScale = [QaResize]::GetDpiForWindow([IntPtr]$mainHwnd) / 96.0
                        if (-not $label -or $label.Current.BoundingRectangle.Height -lt ($CustomSize * $dpiScale * 0.8)) { throw 'Snippet label does not reflect custom text size' }
                    }
                }
                Save-QAScreenshot ([IntPtr]$mainHwnd) (Join-Path $out ("$($targetPage[0])-$Theme-size$CustomSize-$($size[0])x$($size[1]).png"))
            }
            foreach ($query in @('', 'First', 'NoSuchQaResult')) {
                $activation = @('--preview-popup')
                if ($query) { $activation += @('--preview-popup-query', $query) }
                Request-QAActivation $activation
                $popup = Wait-Until { Get-PopupWindow $p.Id } 8000 100
                if (-not $popup) { throw 'Preview popup did not appear' }
                $popupStyle = [QaResize]::GetWindowStyle([IntPtr]$popup.Current.NativeWindowHandle, -16).ToInt64()
                $popupExtendedStyle = [QaResize]::GetWindowStyle([IntPtr]$popup.Current.NativeWindowHandle, -20).ToInt64()
                if ((($popupStyle -band 0x00C00000) -ne 0 -or ($popupExtendedStyle -band 8) -eq 0) -and -not $script:PopupCaptionWarned) {
                    $script:PopupCaptionWarned = $true
                    $script:QaFailures.Add('Popup is not borderless and topmost')
                    Write-Warning $script:QaFailures[-1]
                }
                $search = Find-ById $popup 'PopupSearch'
                if (-not $search -or (Get-Text $search) -cne $query) { throw 'Preview query did not match' }
                $list = Find-ById $popup 'PopupList'
                if (-not $list) { throw 'Popup list missing' }
                $rows = @(Find-All $list ([Windows.Automation.ControlType]::ListItem))
                if ($query -eq 'NoSuchQaResult' -and $rows.Count -ne 0) { throw 'Empty preview search returned rows' }
                if (-not $query -and $rows.Count -lt 3) { throw 'Populated preview returned fewer than three rows' }
                if ($query -eq 'First' -and $rows.Count -ne 1) { throw 'Preview search did not return exactly one row' }
                $popupText = @(Find-All $list ([Windows.Automation.ControlType]::Text) | ForEach-Object { $_.Current.Name })
                if ('Synthetic one' -in $popupText -or 'Synthetic two' -in $popupText -or 'Synthetic three' -in $popupText) { throw 'Snippet bodies appeared in popup rows' }
                $state = if (-not $query) { 'filled' } elseif ($query -eq 'First') { 'search' } else { 'empty' }
                Save-QAScreenshot ([IntPtr]$popup.Current.NativeWindowHandle) (Join-Path $out ("popup-$state-$Theme-size$CustomSize-$($size[0])x$($size[1]).png"))
            }
            continue
        }
        foreach ($page in ,@('FoldersItem','folders')) {
            Update-HarnessProgress
            $w = Get-AppWindow $p.Id 'Smart Snippets'
            $beforeForeground = [Win32Verify]::GetForegroundWindow()
            $nav = Find-ById $w $page[0]
            if (-not $nav -or $nav.Current.IsOffscreen) { throw "Missing or offscreen navigation $($page[0])" }
            $rect = $nav.Current.BoundingRectangle
            $x = [int]($rect.X + $rect.Width / 2); $y = [int]($rect.Y + $rect.Height / 2)
            $inputWindow = [SmartSnippetsPostedInput]::Click([IntPtr]$w.Current.NativeWindowHandle, $p.Id, $x, $y)
            Write-Host "Posted navigation to owned child HWND $inputWindow"
            $afterForeground = [Win32Verify]::GetForegroundWindow()
            if ($beforeForeground -cne $afterForeground) { throw 'Foreground changed during posted navigation' }
            Start-Sleep -Milliseconds 500
            if (-not (Find-ById (Get-AppWindow $p.Id 'Smart Snippets') 'FolderList')) {
                $retryBefore = [Win32Verify]::GetForegroundWindow()
                $null = [SmartSnippetsPostedInput]::Click([IntPtr]$w.Current.NativeWindowHandle, $p.Id, $x, $y)
                $retryAfter = [Win32Verify]::GetForegroundWindow()
                if ($retryBefore -cne $retryAfter) { throw 'Foreground changed during posted retry' }
                Start-Sleep -Milliseconds 500
            }
            if (-not (Find-ById (Get-AppWindow $p.Id 'Smart Snippets') 'FolderList')) {
                $keyBefore = [Win32Verify]::GetForegroundWindow()
                # Keyboard fallback remains targeted to the same owned bridge; delivery
                # must be proven by the Folders UIA element, not merely by posting.
                [SmartSnippetsPostedInput]::Key([IntPtr]$w.Current.NativeWindowHandle, $p.Id, $x, $y, 0x09)
                [SmartSnippetsPostedInput]::Key([IntPtr]$w.Current.NativeWindowHandle, $p.Id, $x, $y, 0x28)
                [SmartSnippetsPostedInput]::Key([IntPtr]$w.Current.NativeWindowHandle, $p.Id, $x, $y, 0x0D)
                Start-Sleep -Milliseconds 500
                $keyAfter = [Win32Verify]::GetForegroundWindow()
                if ($keyBefore -cne $keyAfter) { throw 'Foreground changed during posted keyboard navigation' }
            }
            if (-not (Find-ById (Get-AppWindow $p.Id 'Smart Snippets') 'FolderList')) { throw 'Posted mouse and keyboard navigation did not open Folders' }
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
    if ($script:QaFailures.Count) { throw 'One or more visual QA assertions failed' }
} finally {
    foreach ($p in $procs) { Stop-OwnedProcess $p }
    if ($isolated -and (Get-Process -Name 'SmartSnippets.App' -ErrorAction SilentlyContinue)) { throw 'App remains running; profile recovery requires manual inspection' }
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
