$ErrorActionPreference = 'Stop'
. "$PSScriptRoot/lib/Perf.ps1"
. "$PSScriptRoot/lib/LogAudit.ps1"
. "$PSScriptRoot/lib/Safety.ps1"
function Assert($Condition, $Message) { if (-not $Condition) { throw $Message }; Write-Host "PASS $Message" }
$dir = Join-Path ([IO.Path]::GetTempPath()) ('ss-pure-' + [guid]::NewGuid())
New-Item -ItemType Directory $dir | Out-Null
try {
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
    Add-Type 'public static class VerifyClipboard { public static string Value; public static void Install(string s) { Value="13:40:TEXT|49152:67112960:BIG"; } public static string Fingerprint() { return Value; } public static string OrderedFingerprint() { return Value; } }'
    . "$PSScriptRoot/lib/Clipboard.ps1"
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
