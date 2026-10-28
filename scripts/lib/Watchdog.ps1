# Supervisor owns the profile lock and an independent clipboard-text backstop. It imports no UIA.
function Update-HarnessProgress {
    if ($script:HarnessProgressPath) {
        [IO.File]::WriteAllText($script:HarnessProgressPath, [guid]::NewGuid().ToString('N'))
    }
}
function Invoke-HarnessWorker([string]$ScriptPath, [hashtable]$Arguments, [int]$TimeoutSeconds = 600) {
    $lockPath = Join-Path $env:LOCALAPPDATA 'SmartSnippets.verify.lock'
    try { $lease = [IO.File]::Open($lockPath, [IO.FileMode]::OpenOrCreate, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None) }
    catch { throw 'Another acceptance supervisor holds the exclusive profile lock; no side effects were started' }
    $worker=$null; $saved=$null; $dir=$null; $aborted=$true
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
            . (Join-Path $PSScriptRoot 'Clipboard.ps1')
            $saved=Save-ClipboardText # Acquisition failure refuses launch; never serialize user's text.
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
            try { if ($saved) { Restore-ClipboardText $saved } }
            finally {
                if ($worker) { $worker.Dispose() }
                if ($dir) { Remove-Item -LiteralPath $dir -Recurse -Force -ErrorAction SilentlyContinue }
                $lease.Dispose()
            }
        }
    }
}
