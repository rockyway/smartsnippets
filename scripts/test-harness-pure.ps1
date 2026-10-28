$ErrorActionPreference = 'Stop'
. "$PSScriptRoot/lib/Perf.ps1"
function Assert($Condition, $Message) { if (-not $Condition) { throw $Message }; Write-Host "PASS $Message" }
$dir = Join-Path ([IO.Path]::GetTempPath()) ('ss-pure-' + [guid]::NewGuid())
New-Item -ItemType Directory $dir | Out-Null
try {
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
    $stats = Get-TimingStatistics (1..20)
    Assert ($stats.Count -eq 20 -and $stats.Median -eq 10.5 -and $stats.P95 -eq 19 -and $stats.Max -eq 20) 'even median and nearest-rank p95/max'
    Assert ((Get-TimingStatistics @(5,1,3)).Median -eq 3) 'odd median'
    Assert (Test-LogPrivacy @('app_started version=1','insert_result outcome=Inserted') @('secret')) 'safe logs accepted'
    Assert (-not (Test-LogPrivacy @('first profile secret','second profile clean') @('secret'))) 'leak in earlier profile detected'
    Assert (-not (Test-LogPrivacy @('body a.b[1]') @('a.b[1]'))) 'privacy uses literal not regex matching'
} finally { Remove-Item $dir -Recurse -Force }
