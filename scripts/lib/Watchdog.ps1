# Supervisor owns the profile lock and user-state recovery backstops. It imports no UIA.
. (Join-Path $PSScriptRoot 'RegistryRecovery.ps1')
function Update-HarnessProgress {
    if ($script:HarnessProgressPath) {
        [IO.File]::WriteAllText($script:HarnessProgressPath, [guid]::NewGuid().ToString('N'))
    }
}
function Get-HarnessFileHash([string]$Path) {
    $stream=[IO.File]::OpenRead($Path); $hash=[Security.Cryptography.SHA256]::Create()
    try { return [BitConverter]::ToString($hash.ComputeHash($stream)).Replace('-','') }
    finally { $hash.Dispose(); $stream.Dispose() }
}
function Complete-HarnessWorker($Config, [int]$ExitCode) {
    # Called only after worker cleanup and report production, not by the bootstrap.
    $hash=$null
    if ($Config.Arguments.ReportPath) { $hash=Get-HarnessFileHash $Config.Arguments.ReportPath }
    $ack=[pscustomobject]@{ RunId=$Config.RunId; ExitCode=$ExitCode; ReportHash=$hash }
    $temp=$Config.Completion+'.tmp'
    $ack | Export-Clixml -LiteralPath $temp
    Move-Item -LiteralPath $temp -Destination $Config.Completion -Force
}
function Test-CurrentHarnessReport($Config, [switch]$RequireFailure) {
    try {
        $rows=ConvertFrom-Json -InputObject ([IO.File]::ReadAllText($Config.Arguments.ReportPath))
        if (-not @($rows).Count) { return $false }
        foreach ($row in @($rows)) { if ($row.RunId -cne $Config.RunId) { return $false } }
        if ($RequireFailure) { return @($rows | Where-Object { $_.Status -eq 'FAIL' }).Count -gt 0 }
        return @($rows | Where-Object { $_.Status -eq 'FAIL' }).Count -eq 0
    } catch { return $false }
}
function Test-HarnessCompletion($Config, [int]$ExitCode) {
    try {
        $ack=Import-Clixml -LiteralPath $Config.Completion -ErrorAction Stop
        if ($ack.RunId -cne $Config.RunId -or $ack.ExitCode -ne $ExitCode) { return $false }
        if ($Config.Arguments.ReportPath) {
            if ($ack.ReportHash -cne (Get-HarnessFileHash $Config.Arguments.ReportPath)) { return $false }
            if (-not (Test-CurrentHarnessReport $Config)) { return $false }
        }
        return $true
    } catch { return $false }
}
function Write-HarnessSupervisorFailure($Config, [string]$Id) {
    if ($Config.Arguments.ReportPath) {
        @([pscustomobject]@{ RunId=$Config.RunId; Status='FAIL'; Id=$Id; Name='worker did not complete acceptance'; Detail='No acceptance verdict; missing/invalid completion or worker failure' }) | ConvertTo-Json | Set-Content -LiteralPath $Config.Arguments.ReportPath -Encoding UTF8
    }
}
function Invoke-ClipboardWorker([string]$Mode, [string]$BackupPath, [string]$LeasePath) {
    $shell=(Get-Process -Id $PID).Path
    $helper=Join-Path $PSScriptRoot '../clipboard-worker.ps1'
    $proc=Start-Process -FilePath $shell -ArgumentList ('-NoProfile -STA -ExecutionPolicy Bypass -File "'+$helper+'" -Mode '+$Mode+' -BackupPath "'+$BackupPath+'" -LeasePath "'+$LeasePath+'"') -NoNewWindow -PassThru
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
function Invoke-HarnessWorker([string]$ScriptPath, [hashtable]$Arguments, [int]$TimeoutSeconds = 600, [string]$LockPath = '', [scriptblock]$RegistryWriter = $null) {
    if (-not $LockPath) { $LockPath = Join-Path $env:LOCALAPPDATA 'SmartSnippets.verify.lock' }
    try { $lease = [IO.File]::Open($LockPath, [IO.FileMode]::OpenOrCreate, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None) }
    catch { throw 'Another acceptance supervisor holds the exclusive profile lock; no side effects were started' }
    $worker=$null; $saved=$null; $dir=$null; $config=$null; $aborted=$true; $retainBackup=$false
    try {
        # PowerShell workers/helpers keep their own lease if the supervisor is killed.
        # Native command descendants do NOT inherit it or get tree-killed; see safety guide.
        # Probe before the clipboard backstop. The primary lease serializes supervisors.
        $workerLock=$LockPath+'.worker'
        try { $probe=[IO.File]::Open($workerLock,[IO.FileMode]::OpenOrCreate,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None); $probe.Dispose() }
        catch { throw 'An earlier acceptance worker is still alive; no new side effects were started' }
        $dir=Join-Path ([IO.Path]::GetTempPath()) ('ss-worker-' + [guid]::NewGuid().ToString('N'))
        $null=New-Item -ItemType Directory -Path $dir
        $configPath=Join-Path $dir 'config.clixml'
        $progress=Join-Path $dir 'progress'
        $parent=Get-Process -Id $PID
        $config=[pscustomobject]@{ RunId=[guid]::NewGuid().ToString('N'); RegistryJournal=(Join-Path $dir 'registry.clixml'); Completion=(Join-Path $dir 'complete.clixml'); ParentId=$PID; ParentStartTicks=$parent.StartTime.ToUniversalTime().Ticks; Arguments=$Arguments; Script=$ScriptPath; Progress=$progress; WorkerLock=$workerLock }
        $config | Export-Clixml -LiteralPath $configPath
        $bootstrap=Join-Path $dir 'worker.ps1'
        # Use a hashtable splat, not positional hashtable/array expansion.
        [IO.File]::WriteAllText($bootstrap, '$ErrorActionPreference="Stop"; try { $c=Import-Clixml -LiteralPath $args[0]; $a=$c.Arguments; $global:LASTEXITCODE=0; & $c.Script @a -HarnessWorkerConfig $args[0]; exit $LASTEXITCODE } catch { exit 1 }')
        $needsClipboard = -not $Arguments.ContainsKey('Only') -or @($Arguments.Only).Count -eq 0 -or 'E' -in $Arguments.Only -or 'F' -in $Arguments.Only
        if ($needsClipboard) {
            $backupPath=Join-Path $dir 'clipboard.clixml'
            Invoke-ClipboardWorker 'Save' $backupPath $workerLock # Acquisition failure refuses launch.
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
        $exitCode=$worker.ExitCode
        if ($exitCode -eq 0 -and -not (Test-HarnessCompletion $config $exitCode)) { $exitCode=1 }
        if ($exitCode -ne 0 -and -not (Test-CurrentHarnessReport $config -RequireFailure)) { Write-HarnessSupervisorFailure $config 'HARNESS.workerexit' }
        $aborted=$false
        return $exitCode
    } finally {
        try {
            if ($worker -and -not $worker.HasExited) {
                $worker.Kill()
                if (-not $worker.WaitForExit(5000)) { throw 'Acceptance worker could not be stopped' }
            }
            if ($aborted -and $worker) {
                Write-Warning 'Acceptance aborted. App/target and CLI descendant processes/output locks may remain. The real profile backup is NOT automatically moved over a live test profile. Follow docs/guides/002-acceptance-harness-safety.md before retrying.'
                Write-HarnessSupervisorFailure $config 'HARNESS.watchdog'
            }
        } finally {
            try {
                if ($config -and (Test-Path -LiteralPath $config.RegistryJournal)) {
                    try {
                        Restore-HarnessRegistryJournal $config.RegistryJournal $config.RunId -Writer $RegistryWriter
                        if ($aborted) { $retainBackup=$true; Write-Warning "Registry values restored; journal retained at $($config.RegistryJournal) for replay after closing surviving acceptance processes" }
                    } catch {
                        $retainBackup=$true
                        Write-Warning "Registry recovery failed; private journal retained at $($config.RegistryJournal)"
                        throw 'Registry recovery failed'
                    }
                }
            } finally {
            try {
                if ($saved) {
                    try { Invoke-ClipboardWorker 'Restore' $saved $workerLock }
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
}
