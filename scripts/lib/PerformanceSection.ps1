Section 'P. End-to-end performance acceptance'
function New-PerfLibrary([int]$Count, [int]$Seed) {
    Reset-Profile
    $null = Start-App @('--background')
    Stop-App
    $fixture = Join-Path $appRoot 'perf-library.sql'
    try {
        [IO.File]::WriteAllLines($fixture, [string[]]@(New-PerfLibrarySql -Count $Count -Seed $Seed), (New-Object Text.UTF8Encoding $false))
        $import = & sqlite3 -batch $dbPath ".read '$($fixture.Replace('\','/'))'" 2>&1
        if ($LASTEXITCODE -ne 0) { throw 'Perf import failed (SQLite output withheld)' }
    } finally { Remove-Item $fixture -Force -ErrorAction SilentlyContinue }
}
New-PerfLibrary -Count $PerfLibrary -Seed 42
$common = [int][Math]::Floor($PerfLibrary * .1)
$both = [int][Math]::Floor($PerfLibrary * .01)
$counts = (Sql (Get-PerfSearchCountsSql)) -join ''
Check 'P0' 'seed-42 library and planted term counts' ($counts -eq "$PerfLibrary|$common|$common|$both|0") "count/alpha/bravo/both/no-match=$counts; seed=42; folders=50"
if ($counts -ne "$PerfLibrary|$common|$common|$both|0") { throw 'Refusing performance measurements of invalid fixture' }
$cpu = (Get-CimInstance Win32_Processor | ForEach-Object { $_.Name }) -join '; '
$ram = [Math]::Round((Get-CimInstance Win32_ComputerSystem).TotalPhysicalMemory / 1GB, 2)
$power = (powercfg /getactivescheme) -join ' '
Add-Result 'INFO' 'P.machine' 'performance environment' "CPU=$cpu; RAM=${ram}GB; power=$power; scale=$([Win32Verify]::GetDpiForSystem() / 96 * 100)%; build=$((Get-Item $exe).VersionInfo.ProductVersion); fixture=$PerfLibrary; seed=42"
$p = Start-App @('--background')
$target = Start-Target
if (-not $target) { throw 'P requires Notepad' }
$from = @(Get-LogLines).Count
$null = Open-TestPopup $p $target
$cold = Get-ReadySample $from
$null = Test-TimingBudget 'P1.cold' @($cold) 300
Set-PopupLayout $p 'Flat'
Close-TestPopup $p
$opens = @(Measure-PopupOpens $p $target 20 -BothLayouts)
$null = Test-TimingBudget 'P1' $opens 300
$null = Open-TestPopup $p $target
$queries = @(
    @{ Id='Q1'; Before=''; After='alpha'; Expected=$common },
    @{ Id='Q2'; Before=''; After='alpha bravo'; Expected=$both },
    @{ Id='Q3'; Before=''; After='zzqxv'; Expected=0 },
    @{ Id='Q4'; Before='alpha bravo'; After=''; Expected=$PerfLibrary }
)
# Fixed interleaving: round -> query -> flat/folder. Every precondition has completed rendering
# before the sample boundary; ValuePattern.SetValue is one whole-text change, not keystrokes.
for ($attempt = 1; $attempt -le 2; $attempt++) {
    $cells = @{}
    $valid = @{}
    for ($round = 0; $round -lt 20; $round++) {
        foreach ($query in $queries) {
            foreach ($layout in @('Flat','Folder')) {
                Set-PopupLayout $p $layout
                Set-PopupQuery $p $query.Before
                $from = @(Get-LogLines).Count
                Set-PopupQuery $p $query.After
                $lines = @(Get-LogLines | Select-Object -Skip $from | Where-Object { $_ -match 'popup_search_ms ' })
                $key = "$($query.Id).$layout"
                if (-not $cells.ContainsKey($key)) { $cells[$key] = @(); $valid[$key] = $true }
                $ok = $true
                try {
                    $ms = ConvertFrom-PopupSearchLog $lines $layout $query.Expected
                    $cells[$key] += $ms
                } catch { $ok = $false }
                if ($query.Id -eq 'Q3') { $ok = $ok -and [bool](Find-ByName (Get-PopupWindow $p.Id) 'No snippets match') }
                $valid[$key] = $valid[$key] -and $ok
                if (-not $ok) { Check "P2.$key.sample$round.attempt$attempt" 'one rendered sample with expected layout/count/empty state' $false (Get-SafeLogSummary @(Get-LogLines) $from) }
            }
        }
    }
    $allOk = $true
    foreach ($query in $queries) {
        foreach ($layout in @('Flat','Folder')) {
            $key = "$($query.Id).$layout"
            Check "P2.$key.counts.attempt$attempt" '20 samples; every result count/layout correct' ($valid[$key] -and $cells[$key].Count -eq 20)
            if ($cells[$key].Count) {
                $ok = Test-TimingBudget "P2.$key.attempt$attempt" $cells[$key] 150 -MedianOnly
                $allOk = $allOk -and $ok -and $valid[$key]
            } else { $allOk = $false }
        }
    }
    if ($allOk -or $attempt -eq 2) { break }
    Add-Result 'INFO' 'P2.retry' 'R11: first failure retained; close other apps before the single retry' 'Both attempts remain in this report; a failed first attempt is not erased.'
    $null = Read-Host 'Close other apps now, then press Enter for the one permitted retry'
}
Close-TestPopup $p
Stop-Target
Stop-App
if (Get-Command dotnet -ErrorAction SilentlyContinue) {
    $benchmark = & dotnet test tests/SmartSnippets.Core.Tests -c Release --filter 'Category=Benchmark' --logger 'console;verbosity=detailed' 2>&1 | Out-String
    Add-Result 'INFO' 'P3' 'Core benchmark (diagnostic only; does not decide AC13)' $benchmark
} else { Add-Result 'INFO' 'P3' 'Core benchmark' 'SDK unavailable; diagnostic not run' }
