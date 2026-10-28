# Pure safety predicates; no desktop or process operations on import.
function Invoke-ClipboardRetry([scriptblock]$Operation, [int]$Attempts = 50, [int]$DelayMs = 100) {
    for ($attempt=1; $attempt -le $Attempts; $attempt++) {
        try { return (& $Operation) }
        catch {
            # Clipboard/provider exceptions may contain payloads. Never relay their text.
            if ($attempt -eq $Attempts) { throw 'Clipboard operation failed after bounded retries; no valid snapshot/result was obtained' }
            if ($DelayMs -gt 0) { Start-Sleep -Milliseconds $DelayMs }
        }
    }
    throw 'Clipboard retry budget must be positive'
}
function Test-ValidClipboardBackup($Saved) {
    return $null -ne $Saved -and $Saved.Valid -is [bool] -and $Saved.Valid -ceq $true -and $Saved.HasText -is [bool] -and (-not $Saved.HasText -or $Saved.Text -is [string])
}
function Get-ProfileCleanupAction([bool]$Isolated, [bool]$HadOriginal, [bool]$BackupExists, [bool]$ActiveExists) {
    if ($Isolated) { return 'RestoreIsolated' }
    if ($HadOriginal -and $BackupExists -and -not $ActiveExists) { return 'MoveBackupBackOnly' }
    return 'LeaveUntouched'
}
function Get-PopupDuplicateMessages([string]$Mode, [long]$KeyboardWindow, [long]$MouseWindow, [int]$X, [int]$Y) {
    if ($KeyboardWindow -eq 0 -or $Mode -notin @('EnterTwice','EnterAndClick')) { throw 'Invalid popup input route' }
    # WM_KEYDOWN/UP with Enter scan code; every message retains an explicit captured HWND.
    [pscustomobject]@{ Window=$KeyboardWindow; Message=0x100; WParam=13; LParam=0x001C0001 }
    [pscustomobject]@{ Window=$KeyboardWindow; Message=0x101; WParam=13; LParam=3223060481L }
    if ($Mode -eq 'EnterTwice') {
        [pscustomobject]@{ Window=$KeyboardWindow; Message=0x100; WParam=13; LParam=0x001C0001 }
        [pscustomobject]@{ Window=$KeyboardWindow; Message=0x101; WParam=13; LParam=3223060481L }
    } else {
        if ($MouseWindow -eq 0 -or $X -lt 0 -or $Y -lt 0 -or $X -gt 32767 -or $Y -gt 32767) { throw 'Invalid popup row input route' }
        $position=($Y -shl 16) -bor $X
        [pscustomobject]@{ Window=$MouseWindow; Message=0x201; WParam=1; LParam=$position }
        [pscustomobject]@{ Window=$MouseWindow; Message=0x202; WParam=0; LParam=$position }
    }
}
function Test-DisposableTargetIdentity($Expected, $Actual, [object[]]$Windows) {
    if (-not $Expected -or -not $Actual -or -not $Expected.Token -or -not $Windows.Count) { return $false }
    if ($Expected.Id -ne $Actual.Id -or $Expected.StartTicks -ne $Actual.StartTicks -or $Expected.Path -ine $Actual.Path) { return $false }
    # Only classic, single-document Notepad is supported. Packaged/tabbed Notepad may restore
    # user tabs even in a new process; never infer ownership just from its launcher PID.
    if ($Windows.Count -ne 1) { return $false }
    foreach ($w in $Windows) {
        if ($w.ProcessId -ne $Expected.Id -or $w.ClassName -cne 'Notepad' -or -not $w.HasClassicEdit -or $w.Title.IndexOf($Expected.Token, [StringComparison]::Ordinal) -lt 0) { return $false }
    }
    return $true
}
