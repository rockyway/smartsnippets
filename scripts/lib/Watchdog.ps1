# Supervisor owns the profile lock and an independent clipboard-text backstop. It imports no UIA.
function Update-HarnessProgress {
    if ($script:HarnessProgressPath) {
        [IO.File]::WriteAllText($script:HarnessProgressPath, [guid]::NewGuid().ToString('N'))
    }
}
function Invoke-ClipboardWorker([string]$Mode, [string]$BackupPath) {
    $shell=(Get-Process -Id $PID).Path
    $helper=Join-Path $PSScriptRoot '../clipboard-worker.ps1'
    $proc=Start-Process -FilePath $shell -ArgumentList ('-NoProfile -STA -ExecutionPolicy Bypass -File "'+$helper+'" -Mode '+$Mode+' -BackupPath "'+$BackupPath+'"') -NoNewWindow -PassThru
    $null=$proc.Handle
    try {
        if (-not $proc.WaitForExit(15000)) {
            $proc.Kill()
            if (-not $proc.WaitForExit(5000)) { throw 'Clipboard helper could not be stopped' }
            throw 'Clipboard helper timed out; no clipboard preservation success is claimed'
        }
        if ($proc.ExitCode -ne 0) { throw 'Clipboard helper failed; no clipboard preservation success is claimed' }
    } finally {
        try { if (-not $proc.HasExited) { $proc.Kill(); $null=$proc.WaitForExit(5000) } }
        finally { $proc.Dispose() }
    }
}
function Invoke-HarnessWorker([string]$ScriptPath, [hashtable]$Arguments, [int]$TimeoutSeconds = 600) {
    $lockPath = Join-Path $env:LOCALAPPDATA 'SmartSnippets.verify.lock'
    try { $lease = [IO.File]::Open($lockPath, [IO.FileMode]::OpenOrCreate, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None) }
    catch { throw 'Another acceptance supervisor holds the exclusive profile lock; no side effects were started' }
    $worker=$null; $saved=$null; $dir=$null; $aborted=$true; $retainBackup=$false
    try {
        $dir=Join-Path ([IO.Path]::GetTempPath()) ('ss-worker-' + [guid]::NewGuid().ToString('N'))
        $null=New-Item -ItemType Directory -Path $dir
        $configPath=Join-Path $dir 'config.clixml'
        $progress=Join-Path $dir 'progress'
        $parent=Get-Process -Id $PID
        [pscustomobject]@{ ParentId=$PID; ParentStartTicks=$parent.StartTime.ToUniversalTime().Ticks; Arguments=$Arguments; Script=$ScriptPath; Progress=$progress } | Export-Clixml -LiteralPath $configPath
        $bootstrap=Join-Path $dir 'worker.ps1'
        # Use a hashtable splat, not positional hashtable/array expansion.
        [IO.File]::WriteAllText($bootstrap, '$c=Import-Clixml -LiteralPath $args[0]; $a=$c.Arguments; & $c.Script @a -HarnessWorkerConfig $args[0]')
        $needsClipboard = -not $Arguments.ContainsKey('Only') -or @($Arguments.Only).Count -eq 0 -or 'E' -in $Arguments.Only -or 'F' -in $Arguments.Only
        if ($needsClipboard) {
            $backupPath=Join-Path $dir 'clipboard.clixml'
            Invoke-ClipboardWorker 'Save' $backupPath # Acquisition failure refuses launch.
            $saved=$backupPath # Text is DPAPI-encrypted for this Windows user, never plaintext on disk.
        }
        $shell=(Get-Process -Id $PID).Path
        $worker=Start-Process -FilePath $shell -ArgumentList ('-NoProfile -STA -ExecutionPolicy Bypass -File "' + $bootstrap + '" "' + $configPath + '"') -NoNewWindow -PassThru
        $null=$worker.Handle
        $last=''; $idle=[Diagnostics.Stopwatch]::StartNew()
        while (-not $worker.WaitForExit(200)) {
            if (Test-Path -LiteralPath $progress) {
                try { $current=[IO.File]::ReadAllText($progress) } catch { $current=$last }
                if ($current -and $current -ne $last) { $last=$current; $idle.Restart() }
            }
            if ($idle.Elapsed.TotalSeconds -gt $TimeoutSeconds) { throw 'Acceptance worker exceeded its progress timeout (possibly a blocked UIA/native call)' }
        }
        $aborted=$false
        return $worker.ExitCode
    } finally {
        try {
            if ($worker -and -not $worker.HasExited) {
                $worker.Kill()
                if (-not $worker.WaitForExit(5000)) { throw 'Acceptance worker could not be stopped' }
            }
            if ($aborted -and $worker) {
                Write-Warning 'Acceptance aborted. App/target processes may remain. The real profile backup is NOT automatically moved over a live test profile. Follow docs/guides/002-acceptance-harness-safety.md before retrying.'
                if ($Arguments.ReportPath) {
                    @([pscustomobject]@{ Status='FAIL'; Id='HARNESS.watchdog'; Name='worker aborted'; Detail='No acceptance verdict; manual profile/owned-target recovery required' }) | ConvertTo-Json | Set-Content -LiteralPath $Arguments.ReportPath -Encoding UTF8
                }
            }
        } finally {
            try {
                if ($saved) {
                    try { Invoke-ClipboardWorker 'Restore' $saved }
                    catch { $retainBackup=$true; Write-Warning "Clipboard restoration failed; encrypted recovery backup retained at $saved"; throw 'Clipboard restoration failed' }
                }
            }
            finally {
                if ($worker) { $worker.Dispose() }
                if ($dir -and -not $retainBackup) { Remove-Item -LiteralPath $dir -Recurse -Force -ErrorAction SilentlyContinue }
                $lease.Dispose()
            }
        }
    }
}
