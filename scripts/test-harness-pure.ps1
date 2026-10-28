param([string]$DatasetDigestPath = '')
$ErrorActionPreference = 'Stop'
. "$PSScriptRoot/lib/Perf.ps1"
. "$PSScriptRoot/lib/LogAudit.ps1"
. "$PSScriptRoot/lib/Safety.ps1"
. "$PSScriptRoot/lib/Watchdog.ps1"
$script:AssertionCount=0
function Assert($Condition, $Message) { if (-not $Condition) { throw $Message }; $script:AssertionCount++; Write-Host "PASS $Message" }
$dir = Join-Path ([IO.Path]::GetTempPath()) ('ss-pure-' + [guid]::NewGuid())
New-Item -ItemType Directory $dir | Out-Null
try {
    Assert ((Get-ProfileCleanupAction $false $true $false $true) -ceq 'LeaveUntouched') 'failed move cannot delete original active profile'
    Assert ((Get-ProfileCleanupAction $false $true $true $false) -ceq 'MoveBackupBackOnly') 'interruption immediately after move restores without deleting anything'
    Assert ((Get-ProfileCleanupAction $false $true $true $true) -ceq 'LeaveUntouched') 'ambiguous profile pair is preserved, not merged'
    Assert ((Get-ProfileCleanupAction $true $false $false $true) -ceq 'RestoreIsolated') 'initially absent profile cleanup removes only isolated fixture'
    $diagnostic=Get-PerfEvidenceClassification 'P1.cold' 10 $true
    Assert ($diagnostic.Id -ceq 'Diagnostic.P1.cold' -and $diagnostic.Status -ceq 'INFO') 'small dataset timing cannot become AC13 PASS'
    Assert ((Get-PerfEvidenceClassification 'P2.Q1.Flat' 10000 $true).Status -ceq 'PASS') '10k timings retain acceptance classification'
    Assert ((Get-PerfEvidenceClassification 'P0' 10 $false).Status -ceq 'FAIL') 'diagnostic fixture failure is still failure'
    Assert ((Get-PerfEvidenceClassification 'E10' 10 $true).Id -ceq 'E10') 'small P dataset does not reclassify standard-profile checks'
    Assert (-not (Test-LogPrivacy @('event target=SS-acceptance-unique.txt - Notepad') @('SS-acceptance-unique'))) 'unique target title token catches title-only leakage'
    Assert (-not (Test-HarnessLogPattern @() 'paste_sent')) 'empty logs cannot satisfy a positive oracle'
    Assert (Test-HarnessLogPattern @('INFO paste_sent state=Dispatched') 'paste_sent') 'singleton log stays a whole line'
    Assert (-not (Test-HarnessLogPattern @('INFO paste_sent') 'paste_sent' 1)) 'log offset beyond singleton is empty'
    Assert (Test-HarnessLogPattern @('old','INFO paste_sent') 'paste_sent' 1) 'multi-line log offsets preserved'
    $privateLine='2026-10-28T00:00:00.000Z INFO insert_result outcome=Inserted PRIVATE-SNIPPET-TITLE'
    $safeSummary=Get-SafeLogSummary @($privateLine,'PRIVATE-SNIPPET-TITLE')
    Assert ($safeSummary -ceq 'line=1 event=insert_result; line=2 event=unrecognized') 'log summary emits only allowlisted event names and line numbers'
    Assert ($safeSummary -notmatch 'PRIVATE|outcome=') 'raw log payloads cannot escape summaries'
    Assert ((Get-SafeLogSummary @('2026-10-28T00:00:00.000Z INFO private_event PRIVATE')) -ceq 'line=1 event=unrecognized') 'unknown event names withheld too'
    Assert ((Get-SafeLogSummary @() 0) -ceq '(no new log lines)') 'empty log summary explicit'
    Assert ((Get-SafeLogSummary @('a','b','c') 0 1) -ceq 'line=3 event=unrecognized') 'log summary bounded to requested tail'
    $enterMessages=@(Get-PopupDuplicateMessages 'EnterTwice' 10 0 0 0)
    Assert ($enterMessages.Count -eq 4 -and @($enterMessages | Where-Object { $_.Window -ne 10 }).Count -eq 0 -and ($enterMessages.Message -join ',') -eq '256,257,256,257') 'double Enter queues only explicit popup keyboard messages'
    $clickMessages=@(Get-PopupDuplicateMessages 'EnterAndClick' 10 11 20 30)
    Assert ($clickMessages.Count -eq 4 -and ($clickMessages.Window -join ',') -eq '10,10,11,11' -and ($clickMessages.Message -join ',') -eq '256,257,513,514') 'Enter plus click queues to captured popup children without global input'
    $badRoute=$false
    try { $null=@(Get-PopupDuplicateMessages 'EnterTwice' 0 0 0 0) } catch { $badRoute=$true }
    Assert $badRoute 'missing popup keyboard route refused'
    $badRoute=$false
    try { $null=@(Get-PopupDuplicateMessages 'EnterAndClick' 10 0 20 30) } catch { $badRoute=$true }
    Assert $badRoute 'missing popup mouse route refused before dispatch'
    $script:clipboardAttempts = 0
    $retried = Invoke-ClipboardRetry { $script:clipboardAttempts++; if ($script:clipboardAttempts -lt 3) { throw 'private payload' }; 'ok' } -Attempts 3 -DelayMs 0
    Assert ($retried -eq 'ok' -and $script:clipboardAttempts -eq 3) 'transient clipboard contention retried'
    $refused=$false
    try { Invoke-ClipboardRetry { throw 'private payload' } -Attempts 2 -DelayMs 0 } catch { $refused=$_.Exception.Message -notmatch 'private payload' }
    Assert $refused 'clipboard acquisition failure throws a payload-free error'
    Assert (Test-ValidClipboardBackup ([pscustomobject]@{ Valid=$true; HasText=$false; Text=$null })) 'successfully empty clipboard backup accepted'
    Assert (Test-ValidClipboardBackup ([pscustomobject]@{ Valid=$true; HasText=$true; Text='' })) 'empty string text is distinct from missing text'
    Assert (-not (Test-ValidClipboardBackup $null)) 'null clipboard backup refused'
    Assert (-not (Test-ValidClipboardBackup ([pscustomobject]@{ HasText=$false }))) 'failed/unvalidated clipboard backup refused'
    $expectedTarget = [pscustomobject]@{ Id=12; StartTicks=100; Path='C:\Windows\notepad.exe'; Token='SS-owned-123' }
    $actualTarget = [pscustomobject]@{ Id=12; StartTicks=100; Path='C:\Windows\notepad.exe' }
    $targetWindow = [pscustomobject]@{ ProcessId=12; ClassName='Notepad'; HasClassicEdit=$true; Title='SS-owned-123.txt - Notepad' }
    Assert (Test-DisposableTargetIdentity $expectedTarget $actualTarget @($targetWindow)) 'classic single-document target identity accepted'
    Assert (-not (Test-DisposableTargetIdentity $expectedTarget $actualTarget @())) 'missing target window refused'
    Assert (-not (Test-DisposableTargetIdentity $expectedTarget $actualTarget @($targetWindow,$targetWindow))) 'multiple windows refused'
    $actualTarget.StartTicks=101
    Assert (-not (Test-DisposableTargetIdentity $expectedTarget $actualTarget @($targetWindow))) 'reused target PID refused'
    $actualTarget.StartTicks=100; $targetWindow.HasClassicEdit=$false
    Assert (-not (Test-DisposableTargetIdentity $expectedTarget $actualTarget @($targetWindow))) 'tabbed or packaged Notepad refused'
    $targetWindow.HasClassicEdit=$true; $targetWindow.Title='user document - Notepad'
    Assert (-not (Test-DisposableTargetIdentity $expectedTarget $actualTarget @($targetWindow))) 'replaced user document refused'
    $targetWindow.Title='SS-owned-123.txt - Notepad'; $actualTarget.Path='C:\other\notepad.exe'
    Assert (-not (Test-DisposableTargetIdentity $expectedTarget $actualTarget @($targetWindow))) 'launcher executable mismatch refused'
    $sql = @(New-PerfLibrarySql -Count 10000 -Seed 42)
    $sql2 = @(New-PerfLibrarySql -Count 10000 -Seed 42)
    if ($DatasetDigestPath) {
        $hash=[Security.Cryptography.SHA256]::Create()
        try { $digest=[BitConverter]::ToString($hash.ComputeHash([Text.Encoding]::UTF8.GetBytes($sql -join "`n"))).Replace('-','').ToLowerInvariant() }
        finally { $hash.Dispose() }
        [IO.File]::WriteAllText($DatasetDigestPath,$digest)
    }
    Assert (($sql -join "`n") -ceq ($sql2 -join "`n")) 'seed 42 produces identical 10k SQL'
    # Read the authoritative migration, not a parallel hand-maintained schema.
    $migration = [IO.File]::ReadAllText((Join-Path $PSScriptRoot '../src/SmartSnippets.Core/Data/Migrations.cs'))
    $schema = [regex]::Match($migration, '(?s)new Migration\(2, """(.*?)"""').Groups[1].Value
    $file = Join-Path $dir 'fixture.sql'
    [IO.File]::WriteAllText($file, $schema + "`n" + ($sql -join "`n"), (New-Object Text.UTF8Encoding $false))
    $db = Join-Path $dir 'fixture.db'
    $out = & sqlite3 $db ".read '$($file.Replace('\','/'))'" 2>&1
    Assert ($LASTEXITCODE -eq 0) "fixture imports through real migration: $out"
    $counts = & sqlite3 $db (Get-PerfSearchCountsSql)
    Assert ($counts -eq '10000|1000|1000|100|0') "search counts $counts"
    $distribution = & sqlite3 $db 'select count(*),min(n),max(n) from (select count(*) n from snippets where folder_id is not null group by folder_id);'
    Assert ($distribution -eq '50|180|180') "folder distribution $distribution"
    Assert ((& sqlite3 $db 'select count(*) from snippets where folder_id is null;') -eq '1000') '1000 unfiled'
    Assert ((& sqlite3 $db 'select sum(label_is_custom),sum(usage_count=0 and last_used_at is not null),sum(usage_count>0 and last_used_at is null) from snippets;') -eq '7000|0|0') 'custom labels and usage timestamps'
    $lengths = & sqlite3 $db 'select sum(length(text) between 20 and 200),sum(length(text) between 201 and 1000),sum(length(text) between 1001 and 4000) from snippets;'
    Assert ($lengths -eq '6000|3000|1000') "text length distribution $lengths"
    $lines = & sqlite3 $db "select sum(n=1),sum(n between 2 and 8),sum(n between 10 and 40) from (select length(text)-length(replace(text,char(10),''))+1 as n from snippets);"
    Assert ($lines -eq '6000|3000|1000') "line-count distribution $lines"
    Assert ((& sqlite3 $db 'select count(*) from snippets where length(cast(text as blob))>length(text);') -eq '500') 'exactly 5 percent non-ASCII'
    Assert ((& sqlite3 $db "select count(*) from snippets where usage_count<0 or usage_count>500 or updated_at<created_at or updated_at>1793145600000 or created_at<1793145600000-730*86400000 or (last_used_at is not null and last_used_at<1793145600000-365*86400000) or length(tags)-length(replace(tags,',',''))>2 or instr(tags,', ')>0;") -eq '0') 'usage/date/tag bounds and normalized comma storage'
    $autoOk = $true
    foreach ($row in (& sqlite3 $db 'select hex(text),hex(label) from snippets where label_is_custom=0;')) {
        $decoded = @()
        foreach ($hex in ($row -split '\|')) {
            $bytes = New-Object byte[] ($hex.Length/2)
            for ($i=0; $i -lt $bytes.Length; $i++) { $bytes[$i]=[Convert]::ToByte($hex.Substring($i*2,2),16) }
            $decoded += [Text.Encoding]::UTF8.GetString($bytes)
        }
        $first = ($decoded[0] -split "`n")[0].Trim()
        $elements = [Globalization.StringInfo]::ParseCombiningCharacters($first)
        $expected = if ($elements.Count -gt 60) { $first.Substring(0,$elements[60]) } else { $first }
        $autoOk = $autoOk -and $decoded[1] -ceq $expected
    }
    Assert $autoOk 'all 3000 auto labels use first nonblank line / 60 text elements'
    $audit = @(Get-HarnessLogAudit (Split-Path $PSScriptRoot -Parent))
    Assert (@($audit | Where-Object { -not $_.Emitted }).Count -eq 0) "all $($audit.Count) consumed/required log events have source emitters"
    New-Item -ItemType Directory (Join-Path $dir 'src'),(Join-Path $dir 'scripts') | Out-Null
    [IO.File]::WriteAllText((Join-Path $dir 'src/OnlyOne.cs'),'AppLog.Info("app_started");')
    $negative = @(Get-HarnessLogAudit $dir)
    Assert (@($negative | Where-Object { -not $_.Emitted }).Count -gt 0) 'log audit rejects missing source emitters'
    $stats = Get-TimingStatistics (1..20)
    Assert ($stats.Count -eq 20 -and $stats.Median -eq 10.5 -and $stats.P95 -eq 19 -and $stats.Max -eq 20) 'even median and nearest-rank p95/max'
    Assert ((Get-TimingStatistics @(5,1,3)).Median -eq 3) 'odd median'
    $sampleLine = '2026-10-28T00:00:00.000Z INFO popup_search_ms 127 layout=flat results=1000'
    Assert ((ConvertFrom-PopupSearchLog @($sampleLine) 'Flat' 1000) -eq 127) 'search render timing parsed with exact layout/count'
    Assert ((ConvertFrom-PopupReadyLog @('INFO popup_ready_ms 299')) -eq 299) 'ready timing parsed'
    foreach ($case in @(@{ Lines=@($sampleLine,$sampleLine); Layout='Flat'; Count=1000 },@{ Lines=@($sampleLine); Layout='Folder'; Count=1000 },@{ Lines=@($sampleLine); Layout='Flat'; Count=100 },@{ Lines=@('INFO popup_search_ms invalid'); Layout='Flat'; Count=1000 })) {
        $rejected=$false
        try { $null = ConvertFrom-PopupSearchLog $case.Lines $case.Layout $case.Count } catch { $rejected=$true }
        Assert $rejected 'duplicate/malformed/wrong-layout/wrong-count samples rejected'
    }
    Assert (Test-LogPrivacy @('app_started version=1','insert_result outcome=Inserted') @('secret')) 'safe logs accepted'
    Assert (-not (Test-LogPrivacy @('first profile secret','second profile clean') @('secret'))) 'leak in earlier profile detected'
    Assert (-not (Test-LogPrivacy @('body a.b[1]') @('a.b[1]'))) 'privacy uses literal not regex matching'
    # Substitute just the native boundary; exercise the real helper comparisons without touching
    # the desktop clipboard. Clipboard.ps1 skips Add-Type when this test double is present.
    Add-Type 'public static class VerifyClipboard { public static string Value; public static int Reads; public static int Writes; public static int WritesFailFor; public static bool FailRead; public static string Text; public static string ReadText() { Reads++; if (FailRead) throw new System.Exception("PRIVATE"); return Text; } public static void WriteText(string s) { Writes++; if (WritesFailFor>0) { WritesFailFor--; throw new System.Exception("PRIVATE"); } Text=s; } public static void Clear() { Writes++; Text=null; } public static void Install(string s) { Value="13:40:TEXT|49152:67112960:BIG"; } public static string Fingerprint() { return Value; } public static string OrderedFingerprint() { return Value; } }'
    . "$PSScriptRoot/lib/Clipboard.ps1"
    $script:HarnessProgressPath=Join-Path $dir 'heartbeat'
    Update-HarnessProgress
    $firstPulse=[IO.File]::ReadAllText($script:HarnessProgressPath)
    Update-HarnessProgress
    Assert ($firstPulse -cne [IO.File]::ReadAllText($script:HarnessProgressPath)) 'watchdog progress advances independently of log content'
    $script:HarnessProgressPath=$null
    # Supervisor integration uses only a fake CLI worker and a TEMP lock/report. Only=A disables
    # clipboard backstops; no app, Notepad, UIA, real profile or desktop API is involved.
    $fakeWorker=Join-Path $dir 'fake-worker.ps1'
    $testLock=Join-Path $dir 'worker.lock'; $testReport=Join-Path $dir 'fake-report.json'
    [IO.File]::WriteAllText($fakeWorker, 'param([string[]]$Only,[string]$HarnessWorkerConfig) exit 0')
    Assert ((Invoke-HarnessWorker $fakeWorker @{ Only=@('A') } -TimeoutSeconds 10 -LockPath $testLock) -eq 0) 'supervisor runs an isolated CLI worker to completion'
    $held=[IO.File]::Open($testLock,[IO.FileMode]::OpenOrCreate,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None)
    $overlapRefused=$false
    try { $null=Invoke-HarnessWorker $fakeWorker @{ Only=@('A') } -LockPath $testLock } catch { $overlapRefused=$true }
    finally { $held.Dispose() }
    Assert $overlapRefused 'exclusive lease refuses a concurrent supervisor before launch'
    $orphanLease=[IO.File]::Open(($testLock+'.worker'),[IO.FileMode]::OpenOrCreate,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None)
    $orphanRefused=$false
    try { $null=Invoke-HarnessWorker $fakeWorker @{ Only=@('A') } -LockPath $testLock } catch { $orphanRefused=$true }
    finally { $orphanLease.Dispose() }
    Assert $orphanRefused 'surviving worker lease blocks restart after supervisor termination'
    [IO.File]::WriteAllText($fakeWorker, 'param([string[]]$Only,[string]$HarnessWorkerConfig,[string]$ReportPath) ''{"Status":"PASS"}'' | Set-Content -LiteralPath $ReportPath; Start-Sleep -Seconds 30')
    $timedOut=$false; $timer=[Diagnostics.Stopwatch]::StartNew()
    try { $null=Invoke-HarnessWorker $fakeWorker @{ Only=@('A'); ReportPath=$testReport } -TimeoutSeconds 2 -LockPath $testLock } catch { $timedOut=$true }
    $failedReport=Get-Content -LiteralPath $testReport -Raw | ConvertFrom-Json
    Assert ($timedOut -and $timer.Elapsed.TotalSeconds -lt 15 -and $failedReport.Status -eq 'FAIL') 'blocked CLI worker is killed promptly and cannot leave a PASS report'
    $protectedPath=Join-Path $dir 'clipboard-backup.clixml'
    Export-ClipboardTextBackup ([pscustomobject]@{ Valid=$true; HasText=$true; Text='PRIVATE-CLIPBOARD-ROUNDTRIP' }) $protectedPath
    $roundtrip=Import-ClipboardTextBackup $protectedPath
    Assert ($roundtrip.HasText -and $roundtrip.Text -ceq 'PRIVATE-CLIPBOARD-ROUNDTRIP') 'DPAPI clipboard recovery file roundtrips without native clipboard access'
    Assert ([IO.File]::ReadAllText($protectedPath).IndexOf('PRIVATE-CLIPBOARD-ROUNDTRIP',[StringComparison]::Ordinal) -lt 0) 'recovery file does not contain plaintext clipboard text'
    Export-ClipboardTextBackup ([pscustomobject]@{ Valid=$true; HasText=$true; Text='' }) $protectedPath
    $roundtrip=Import-ClipboardTextBackup $protectedPath
    Assert ($roundtrip.HasText -and $roundtrip.Text -ceq '') 'protected empty text remains a text snapshot'
    Export-ClipboardTextBackup ([pscustomobject]@{ Valid=$true; HasText=$false; Text=$null }) $protectedPath
    Assert (-not (Import-ClipboardTextBackup $protectedPath).HasText) 'protected no-text snapshot stays distinct'
    [VerifyClipboard]::FailRead=$true
    $failedSnapshot=$false; $writesBefore=[VerifyClipboard]::Writes
    try { $null=Save-ClipboardText -Attempts 2 -DelayMs 0 } catch { $failedSnapshot=$true }
    Assert ($failedSnapshot -and [VerifyClipboard]::Writes -eq $writesBefore) 'native read failure aborts snapshot instead of manufacturing no-text backup'
    [VerifyClipboard]::FailRead=$false
    [VerifyClipboard]::Text='original'
    $savedText=Save-ClipboardText
    Set-ClipboardText 'fixture'
    Restore-ClipboardText $savedText
    Assert ([VerifyClipboard]::Text -ceq 'original') 'real snapshot/restore wrappers preserve text through a fake native boundary'
    [VerifyClipboard]::Text='fixture'; [VerifyClipboard]::WritesFailFor=2; $writesBefore=[VerifyClipboard]::Writes
    Restore-ClipboardText $savedText -Attempts 3 -DelayMs 0
    Assert ([VerifyClipboard]::Text -ceq 'original' -and [VerifyClipboard]::Writes -eq $writesBefore+3) 'restore wrapper retries contention and restores validated text'
    $writes=[VerifyClipboard]::Writes; $invalidRefused=$false
    try { Restore-ClipboardText ([pscustomobject]@{ HasText=$false }) } catch { $invalidRefused=$true }
    Assert ($invalidRefused -and [VerifyClipboard]::Writes -eq $writes) 'invalid backup cannot clear clipboard'
    Set-UnsaveableClipboard 'test'
    Assert (Test-ClipboardIntact) 'F9 exact format/hash fingerprint accepted'
    [VerifyClipboard]::Value = '13:40:CHANGED|49152:67112960:BIG'
    Assert (-not (Test-ClipboardIntact)) 'F9 changed format hash rejected'
    [VerifyClipboard]::Value = '13:40:TEXT|49153:4:MARKER'
    Assert (Test-UnsaveableClipboardRestored) 'F10 saved Unicode restored; added privacy marker allowed'
    [VerifyClipboard]::Value = '13:40:TEXT|49152:67112960:BIG'
    Assert (-not (Test-UnsaveableClipboardRestored)) 'F10 oversized format must be absent'
    [VerifyClipboard]::Value = '13:40:CHANGED'
    Assert (-not (Test-UnsaveableClipboardRestored)) 'F10 restored Unicode hash must match'
} finally { Remove-Item $dir -Recurse -Force }
Write-Host "Assertions PASS: $script:AssertionCount"
