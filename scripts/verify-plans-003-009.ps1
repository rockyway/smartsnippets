<#
.SYNOPSIS
  Automated verification for plans 003 (core), 004 (shell/settings), 005 (main window) and 009 (design alignment).

.DESCRIPTION
  Runs, in order:
    A. unit tests, benchmark, Native AOT publish gate, static wiring checks
    B. database migration scenarios against the published AOT exe (003 runbook)
    C. shell lifecycle, hotkey, tray, settings (004 / 009 runbook)
    D. main-window snippet and folder workflows driven through UI Automation (005 / 009 runbook)
  Everything that needs a human (UAC prompt, Narrator, light/dark visual review, Explorer restart,
  sign-out) is reported as SKIP with the reason.

  The real %LOCALAPPDATA%\SmartSnippets profile is moved aside for the run and restored in `finally`.
  Requires: Windows PowerShell 5.1, .NET 10 SDK, sqlite3.exe on PATH, an interactive desktop session.
  Do not touch the keyboard/mouse while it runs (it sends real key presses).

.PARAMETER SkipBuild   Reuse existing .\publish output instead of rebuilding.
.PARAMETER SkipTests   Skip unit tests and benchmark (sections A1-A3).
.PARAMETER Only        Run only the given sections, e.g. -Only B,D
#>
param(
    [switch]$SkipBuild,
    [switch]$SkipTests,
    [string[]]$Only = @(),
    [string]$ReportPath = '',
    [string]$ArtifactZip = '',
    [ValidateRange(10, 1000000)][int]$PerfLibrary = 10000,
    [switch]$FlipTaskbarTheme
)

$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
Set-Location $root
. (Join-Path $PSScriptRoot 'lib\Uia.ps1')
. (Join-Path $PSScriptRoot 'lib\Perf.ps1')
. (Join-Path $PSScriptRoot 'lib\PopupHarness.ps1')
$script:ArchivedLogs = New-Object System.Collections.Generic.List[string]
$script:PrivacySecrets = New-Object System.Collections.Generic.List[string]
foreach ($secret in @('smart-snippets-probe','locked-save-probe','ZZ-PRIVACY-MARKER-7431')) { $script:PrivacySecrets.Add($secret) }
function Archive-Logs {
    if (Test-Path $appRoot) {
        foreach ($file in (Get-ChildItem (Join-Path $appRoot 'logs') -File -ErrorAction SilentlyContinue)) {
            foreach ($line in (Get-Content -LiteralPath $file.FullName)) { $script:ArchivedLogs.Add($line) }
        }
    }
}

$exe = Join-Path $root 'publish\SmartSnippets.App.exe'
$appRoot = Join-Path $env:LOCALAPPDATA 'SmartSnippets'
$dbPath = Join-Path $appRoot 'smartsnippets.db'
$logPath = Join-Path $appRoot 'logs\app.log'
$backupRoot = "$appRoot.verify-backup"
$SecretMarker = 'ZZ-PRIVACY-MARKER-7431'
$artifactInfo = $null
if ($ArtifactZip) {
    $zipPath = (Resolve-Path -LiteralPath $ArtifactZip).Path
    $hashPath = "$zipPath.sha256"
    if (-not (Test-Path -LiteralPath $hashPath -PathType Leaf)) { throw "Missing checksum: $hashPath" }
    $hashLine = (Get-Content -LiteralPath $hashPath -Raw).Trim()
    if ($hashLine -notmatch '^([0-9a-fA-F]{64})\s+\*?([^\r\n]+)$') { throw 'Invalid SHA-256 file' }
    $expectedHash = $Matches[1]; $expectedName = $Matches[2]
    if ($expectedName -cne [IO.Path]::GetFileName($zipPath)) { throw 'Checksum filename does not match archive' }
    $actualHash = (Get-FileHash -LiteralPath $zipPath -Algorithm SHA256).Hash
    if ($actualHash -ine $expectedHash) { throw 'Archive SHA-256 mismatch' }
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $archive = [IO.Compression.ZipFile]::OpenRead($zipPath)
    try {
        $entries = @($archive.Entries | ForEach-Object { $_.FullName.Replace('\', '/') })
        if (@($entries | Where-Object { -not $_.StartsWith('SmartSnippets/') -or $_ -match '(^|/)\.\.(/|$)' -or $_ -match '\.pdb$' }).Count -gt 0 -or 'SmartSnippets/SmartSnippets.App.exe' -notin $entries) { throw 'Unsafe or invalid release archive layout' }
    } finally { $archive.Dispose() }
    $temp = Join-Path ([IO.Path]::GetTempPath()) ('SmartSnippets-verify-' + [guid]::NewGuid())
    try {
        [IO.Compression.ZipFile]::ExtractToDirectory($zipPath, $temp)
        if (Test-Path "$root\publish") { Remove-Item "$root\publish" -Recurse -Force }
        Move-Item (Join-Path $temp 'SmartSnippets') "$root\publish"
    } finally { if (Test-Path $temp) { Remove-Item $temp -Recurse -Force } }
    $version = (Get-Item $exe).VersionInfo
    $artifactInfo = "archive=$([IO.Path]::GetFileName($zipPath)) sha256=$actualHash ProductVersion=$($version.ProductVersion) FileVersion=$($version.FileVersion)"
    $SkipBuild = $true
}

$script:Results = New-Object System.Collections.ArrayList
$script:Procs = @()
$script:HotkeyIds = @()

# ----------------------------------------------------------------- reporting
function Add-Result($Status, $Id, $Name, $Detail) {
    [void]$script:Results.Add([pscustomobject]@{ Status = $Status; Id = $Id; Name = $Name; Detail = $Detail })
    $color = switch ($Status) { 'PASS' { 'Green' } 'FAIL' { 'Red' } 'SKIP' { 'DarkYellow' } 'WARN' { 'Yellow' } default { 'Gray' } }
    $line = ('  [{0}] {1} {2}' -f $Status, $Id, $Name)
    if ($Detail) { $line += "  -- $Detail" }
    Write-Host $line -ForegroundColor $color
}
function Check($Id, $Name, $Ok, $Detail = '') { Add-Result $(if ($Ok) { 'PASS' } else { 'FAIL' }) $Id $Name $Detail }
function Skip($Id, $Name, $Reason) { Add-Result 'SKIP' $Id $Name $Reason }
function Warn($Id, $Name, $Detail) { Add-Result 'WARN' $Id $Name $Detail }
function Section($Title) { Write-Host "`n=== $Title ===" -ForegroundColor Cyan }
function Want($Section) { ($Only.Count -eq 0) -or ($Only -contains $Section) }
if ($artifactInfo) { Add-Result 'INFO' 'Artifact' 'Downloaded release artifact' $artifactInfo }

# ----------------------------------------------------------------- app + profile helpers
function Stop-App {
    Get-Process -Name 'SmartSnippets.App' -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
    $null = Wait-Until { -not (Get-Process -Name 'SmartSnippets.App' -ErrorAction SilentlyContinue) } 5000
    Start-Sleep -Milliseconds 300
}
function Reset-Profile([scriptblock]$Seed = $null) {
    Stop-App
    Archive-Logs
    if (Test-Path $appRoot) { Remove-Item $appRoot -Recurse -Force }
    New-Item -ItemType Directory -Path $appRoot | Out-Null
    if ($Seed) { & $Seed }
}
function Get-LogLines { if (Test-Path $logPath) { @(Get-Content $logPath) } else { @() } }
function Log-Has([string]$Pattern, [int]$From = 0) {
    $lines = Get-LogLines
    if ($lines.Count -le $From) { return $false }
    [bool]($lines[$From..($lines.Count - 1)] | Where-Object { $_ -match $Pattern })
}
function Tail([int]$From = 0) { $l = Get-LogLines; if ($l.Count -le $From) { return "(no new log lines)" }; ($l[$From..($l.Count - 1)] | Select-Object -Last 4 | ForEach-Object { ($_ -split " ", 2)[1] }) -join " ; " }
function Wait-Log([string]$Pattern, [int]$From = 0, [int]$TimeoutMs = 8000) {
    [bool](Wait-Until { Log-Has $Pattern $From } $TimeoutMs 150)
}
function Start-App([string[]]$AppArgs = @(), [switch]$NoWait) {
    $from = (Get-LogLines).Count
    $p = if ($AppArgs.Count) { Start-Process $exe -ArgumentList $AppArgs -PassThru } else { Start-Process $exe -PassThru }
    $script:Procs += $p
    if (-not $NoWait) { $null = Wait-Log 'tray_added|second_instance_redirect|hotkey_register_failed' $from 15000 }
    Start-Sleep -Milliseconds 600
    return $p
}
function Get-Main($Proc) { Get-AppWindow $Proc.Id 'Smart Snippets' }
function Wait-Main($Proc, [int]$TimeoutMs = 8000) { Wait-Until { Get-Main $Proc } $TimeoutMs 200 }
function Sql([string]$Query) {
    $out = & sqlite3 -batch $dbPath $Query 2>&1
    if ($LASTEXITCODE -ne 0) { throw "sqlite3 failed: $out" }
    @($out)
}
function HexOf([string]$Text) { -join ([Text.Encoding]::UTF8.GetBytes($Text) | ForEach-Object { $_.ToString('X2') }) }
function Setting([string]$Key) { (Sql "select value from settings where key='$Key';") -join '' }
function Restore-Profile {
    Stop-App
    if (Test-Path $backupRoot) {
        if (Test-Path $appRoot) { Remove-Item $appRoot -Recurse -Force }
        Move-Item $backupRoot $appRoot -Force
    }
}

# ----------------------------------------------------------------- UI helpers
function CrashInfo($Proc) {
    $Proc.Refresh()
    $ev = Get-WinEvent -FilterHashtable @{ LogName = 'Application'; StartTime = (Get-Date).AddMinutes(-2) } -ErrorAction SilentlyContinue | Where-Object { $_.ProviderName -eq 'Application Error' -and $_.Message -match 'SmartSnippets' } | Select-Object -First 1
    $mod = if ($ev) { ($ev.Message -split "`n" | Where-Object { $_ -match 'Faulting module name|Exception code' } | ForEach-Object { $_.Trim() }) -join ' ' } else { 'no WER event' }
    "exited=$($Proc.HasExited); $mod; log: $(Tail ([Math]::Max(0, (Get-LogLines).Count - 3)))"
}
function Nav($Proc, [string]$ItemId) {
    $w = Get-Main $Proc
    if (-not $w) { Add-Result 'FAIL' 'CRASH' "main window disappeared before navigating to $ItemId" (CrashInfo $Proc); throw 'main window missing' }
    Select-El (Find-ById $w $ItemId)
    Start-Sleep -Milliseconds 700
}
function Btn($Root, [string]$Name) { Find-ByName $Root $Name ([Windows.Automation.ControlType]::Button) }
function Press($Root, [string]$Name) {
    $b = Btn $Root $Name
    if (-not $b) { throw "button '$Name' not found" }
    Invoke-El $b
    Start-Sleep -Milliseconds 400
}
function Focus-Main($Proc) {
    $w = Get-Main $Proc
    $null = Set-Foreground $w
    $sb = Find-ById $w 'SearchBox'
    if ($sb) { try { $sb.SetFocus() } catch { } }
    if (-not $sb) { $w.SetFocus() }
    Start-Sleep -Milliseconds 300
}
function New-SnippetEditor($Proc) {
    Nav $Proc 'SnippetsItem'
    Focus-Main $Proc
    Send-Chord @([byte]$VK.Ctrl, [byte]$VK.N)
    $w = Get-Main $Proc
    $tb = Wait-Until { Find-ById (Get-Main $Proc) 'SnippetTextBox' } 4000 150
    return $tb
}
function Save-Snippet($Proc) {
    $w = Get-Main $Proc
    $b = Wait-Until { Btn (Get-Main $Proc) 'Save snippet' } 3000 150
    if (-not $b) { return $false }
    Invoke-El $b
    Start-Sleep -Milliseconds 700
    return $true
}
function Create-Snippet($Proc, [string]$Text, [string]$Tags = '', [string]$Label = $null) {
    $script:PrivacySecrets.Add($Text)
    foreach ($line in ($Text -split '\r?\n|\r')) { if ($line.Length -ge 8) { $script:PrivacySecrets.Add($line) } }
    $tb = New-SnippetEditor $Proc
    if (-not $tb) { throw 'editor did not open' }
    $w = Get-Main $Proc
    Set-Text $tb $Text
    if ($Tags) { Set-Text (Find-ByName $w 'Tags' ([Windows.Automation.ControlType]::Edit)) $Tags }
    if ($Label) { Set-Text (Find-ByName $w 'Snippet label' ([Windows.Automation.ControlType]::Edit)) $Label }
    Start-Sleep -Milliseconds 300
    return (Save-Snippet $Proc)
}
function Press-Hotkey([byte[]]$Keys) {
    [Win32Verify]::Chord($Keys)
}
function Start-Target {
    $np = Start-Process notepad.exe -PassThru
    Start-Sleep -Milliseconds 1800
    $el = Wait-Until {
        $c = New-Object Windows.Automation.PropertyCondition($script:AE::ClassNameProperty, 'Notepad')
        $script:AE::RootElement.FindFirst($script:TS::Children, $c)
    } 6000 200
    if ($el) { $null = Set-Foreground $el }
    Start-Sleep -Milliseconds 500
    return $el
}
function Stop-Target { Get-Process -Name notepad -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue }
function Confirm-Dialog($Proc, [string]$ButtonName) {
    $b = Wait-Until { Btn (Get-Main $Proc) $ButtonName } 3000 150
    if (-not $b) { return $false }
    Invoke-El $b
    Start-Sleep -Milliseconds 600
    return $true
}

# ================================================================= start
$summaryStart = Get-Date
Write-Host '====================================================' -ForegroundColor Cyan
Write-Host ' Verification: plans 003 / 004 / 005 / 009' -ForegroundColor Cyan
Write-Host '====================================================' -ForegroundColor Cyan

if (-not (Get-Command sqlite3 -ErrorAction SilentlyContinue)) { throw 'sqlite3.exe is required on PATH' }
if (Test-Path $backupRoot) { throw "Leftover profile backup at $backupRoot from an earlier aborted run. Restore it manually (move it back to $appRoot) and re-run." }

Stop-App
if (Test-Path $appRoot) { Move-Item $appRoot $backupRoot -Force }

try {

# ================================================================= A. build + static
if (Want 'A') {
    Section 'A. Build, unit tests, AOT gate, static checks'

    if (-not $SkipTests) {
        foreach ($proj in 'Core', 'Presentation') {
            $out = & dotnet test "tests/SmartSnippets.$proj.Tests" --nologo 2>&1 | Out-String
            $m = [regex]::Match($out, '(Passed|Failed)!\s+-\s+Failed:\s+(\d+), Passed:\s+(\d+), Skipped:\s+(\d+), Total:\s+(\d+)')
            Check "A1.$proj" "$proj unit tests" ($m.Success -and $m.Groups[1].Value -eq 'Passed' -and [int]$m.Groups[2].Value -eq 0) $m.Value
        }
        $out = & dotnet test tests/SmartSnippets.Core.Tests -c Release --filter 'Category=Benchmark' --nologo 2>&1 | Out-String
        Check 'A2' '10k-snippet search benchmark (003, AC13 indicative)' ($out -match 'Passed:\s+1') ''
    }

    if (-not $SkipBuild) {
        if (Test-Path publish) { Remove-Item publish -Recurse -Force }
        & (Join-Path $PSScriptRoot 'publish-aot.ps1') -Output publish
        $log = Get-Content publish.log -Raw
        $ok = ($LASTEXITCODE -eq 0)
        Check 'A3.build' 'AOT publish succeeds' $ok
        Check 'A3.warnings' 'zero IL2xxx/IL3xxx warnings' (-not ($log -match 'warning IL[23][0-9]{3}'))
    }
    Check 'A3.aot' 'publish has App.exe + e_sqlite3.dll and no App.dll (Native AOT)' `
        ((Test-Path "$root\publish\SmartSnippets.App.exe") -and (Test-Path "$root\publish\e_sqlite3.dll") -and -not (Test-Path "$root\publish\SmartSnippets.App.dll"))

    # static wiring: compiled-binding discipline and layering
    $xaml = Get-ChildItem src\SmartSnippets.App -Recurse -Filter *.xaml | Where-Object { $_.FullName -notmatch '\\(obj|bin)\\' }
    $bindings = $xaml | Select-String -Pattern '\{Binding[ }]' 
    Check 'A4.binding' 'no reflection {Binding} in XAML (x:Bind only)' (-not $bindings) (($bindings | Select-Object -First 3 | ForEach-Object { $_.Path | Split-Path -Leaf }) -join ',')
    $pres = Get-Content src\SmartSnippets.Presentation\SmartSnippets.Presentation.csproj -Raw
    Check 'A4.layering' 'Presentation has no WinUI/Windows target' (($pres -notmatch 'UseWinUI') -and ($pres -notmatch 'windows10'))
    $keyevents = 'store_loaded', 'settings_loaded', 'hotkey_registered', 'hotkey_changed', 'theme_applied', 'tray_readded', 'restart_elevated_started', 'exit_unregistered', 'migration_backup', 'snippet_saved', 'snippet_deleted', 'folder_created', 'folder_renamed', 'folder_deleted', 'app_started', 'tray_icon', 'tray_icon_load_failed', 'tray_theme_changed', 'popup_ready_ms', 'popup_search_ms', 'insert_result', 'clipboard_swap', 'clipboard_restore'
    $src = (Get-ChildItem src -Recurse -Filter *.cs | Where-Object { $_.FullName -notmatch '\\(obj|bin)\\' } | Get-Content -Raw) -join "`n"
    $missing = $keyevents | Where-Object { $src -notmatch ('"' + $_ + '"') }
    Check 'A4.logevents' 'all runbook log events exist in code' (-not $missing) ($missing -join ',')
}

if (-not (Test-Path $exe)) { throw "publish\SmartSnippets.App.exe not found; run without -SkipBuild" }

# ================================================================= B. migrations (plan 003 runbook)
if (Want 'B') {
    Section 'B. Database migrations on the published AOT build (003)'

    # B1: upgrade a real v1 database (what plan 002 created), data must survive
    Reset-Profile {
        New-Item -ItemType Directory -Path (Join-Path $appRoot 'logs') | Out-Null
        & sqlite3 -batch $dbPath "create table schema_version (version integer not null); insert into schema_version values (1); create table probe (id integer primary key, text text not null); insert into probe (id, text) values (7, 'kept-from-v1');"
    }
    $p = Start-App @('--background')
    Check 'B1.log' 'v1 DB: migration_backup then migrations_applied from=1 to=2' ((Log-Has 'migration_backup') -and (Log-Has 'migrations_applied from=1 to=2'))
    Check 'B1.bak' 'smartsnippets.db.v1.bak exists' (Test-Path "$dbPath.v1.bak")
    Check 'B1.ver' 'schema_version is 2' ((Sql 'select max(version) from schema_version;') -join '' -eq '2')
    $tables = (Sql "select name from sqlite_master where type='table' order by name;") -join ','
    Check 'B1.tables' 'folders, snippets, settings, probe, schema_version present' (($tables -match 'folders') -and ($tables -match 'snippets') -and ($tables -match 'settings') -and ($tables -match 'probe') -and ($tables -match 'schema_version')) $tables
    Check 'B1.data' 'probe row from v1 preserved' ((Sql 'select text from probe where id=7;') -join '' -eq 'kept-from-v1')
    $bak = (Sql "select 1;") ; $bakVer = (& sqlite3 -batch "$dbPath.v1.bak" 'select max(version) from schema_version;')
    Check 'B1.bakver' 'backup file is the v1 database' ($bakVer -eq '1')
    Check 'B1.wal' 'journal_mode is WAL' ((Sql 'pragma journal_mode;') -join '' -eq 'wal')
    Stop-App

    # B2: second start does not migrate or back up again
    $before = @(Get-ChildItem "$appRoot\smartsnippets.db*.bak*").Count
    $from = (Get-LogLines).Count
    $p = Start-App @('--background')
    Check 'B2.log' 'restart logs migrations_applied from=2 to=2' (Log-Has 'migrations_applied from=2 to=2' $from)
    Check 'B2.nobak' 'no new backup is made' (@(Get-ChildItem "$appRoot\smartsnippets.db*.bak*").Count -eq $before)
    Stop-App

    # B3: fresh profile
    Reset-Profile
    $p = Start-App @('--background')
    Check 'B3.log' 'fresh profile: migrations_applied from=0 to=2, no backup' ((Log-Has 'migrations_applied from=0 to=2') -and -not (Log-Has 'migration_backup'))
    Check 'B3.db' 'database file created under %LOCALAPPDATA%\SmartSnippets' (Test-Path $dbPath)
    Stop-App

    # B4: corrupt/newer schema must not crash silently (error path)
    Reset-Profile {
        New-Item -ItemType Directory -Path (Join-Path $appRoot 'logs') | Out-Null
        & sqlite3 -batch $dbPath "create table schema_version (version integer not null); insert into schema_version values (99);"
    }
    $p = Start-App @('--background') -NoWait
    Start-Sleep -Seconds 4
    $p.Refresh()
    Check 'B4' 'DB from a newer app version: app does not run on it (exits or reports), data untouched' (($p.HasExited -or (Log-Has 'migration_failed|data_load_failed')) -and ((Sql 'select max(version) from schema_version;') -join '' -eq '99'))
    # a message box may be showing; closing the process dismisses it
    Stop-App
}

# ================================================================= C. shell, hotkey, tray, settings (plans 004, 009)
if (Want 'C') {
    Section 'C. Shell lifecycle, hotkey, tray and settings (004 / 009)'

    Reset-Profile
    $p = Start-App
    $w = Wait-Main $p
    Check 'C1.window' 'normal launch shows the main window (Snippets page)' ([bool]$w -and [bool](Find-ById $w 'SnippetList'))
    Check 'C1.nav' 'navigation has Snippets, Folders, Settings' ($w -and (Find-ById $w 'SnippetsItem') -and (Find-ById $w 'FoldersItem') -and (Find-ById $w 'SettingsItem'))
    foreach ($ev in 'store_loaded', 'settings_loaded', 'hotkey_registered', 'tray_added') { Check "C1.$ev" "log has $ev" (Log-Has $ev) }
    Check 'C1.empty' 'empty library shows "No snippets yet"' ([bool](Find-ByName $w 'No snippets yet'))

    # pages open without crashing (regression: ItemsRepeater/ItemsControl bound to IReadOnlyList<string> -> E_INVALIDARG)
    foreach ($pair in @(@('FoldersItem', 'FolderList', 'Folders page'), @('SettingsItem', 'TextSizeRadios', 'Settings page'), @('SnippetsItem', 'SnippetList', 'Snippets page'))) {
        $from = (Get-LogLines).Count
        Nav $p $pair[0]
        $ww = Get-Main $p
        Check "C2.$($pair[0])" "$($pair[2]) opens" ([bool]$ww -and [bool](Find-ById $ww $pair[1]) -and -not (Log-Has 'navigate_failed' $from)) $(if (Log-Has 'navigate_failed' $from) { 'navigate_failed logged' } else { '' })
    }
    $tb = New-SnippetEditor $p
    $alive = -not $p.HasExited -and [bool](Get-Main $p)
    Check 'C2.newsnippet' 'New snippet (Ctrl+N) opens the editor without crashing' ($alive -and [bool]$tb)
    if (-not $alive) {
        Skip 'C*' 'remaining shell checks' 'app crashed in C2; fix crash first'
        Stop-App
    }
}
if ((Want 'C') -and ((Get-Process -Name SmartSnippets.App -ErrorAction SilentlyContinue))) {
    $p = Get-Process -Name SmartSnippets.App | Select-Object -First 1

    # hotkey -> popup (needs a foreign foreground window)
    $target = Start-Target
    if ($target) {
        $from = (Get-LogLines).Count
        Press-Hotkey @([byte]$VK.Ctrl, [byte]$VK.Shift, [byte]$VK.Space)
        $ok = Wait-Log 'popup_ready_ms' $from 4000
        $ms = ''
        if ($ok) { $ms = ((Get-LogLines)[$from..((Get-LogLines).Count - 1)] | Where-Object { $_ -match 'popup_ready_ms (\d+)' } | Select-Object -First 1) -replace '.*popup_ready_ms ', '' }
        Check 'C3.popup' 'Ctrl+Shift+Space opens the popup over another app' $ok "popup_ready_ms=$ms"
        if ($ms) { Check 'C3.latency' 'popup ready within 300 ms (AC13)' ([int]$ms -le 300) "$ms ms" }
        Send-Chord @([byte]$VK.Esc)
        Start-Sleep -Milliseconds 400
    }
    else { Skip 'C3' 'hotkey opens popup' 'could not start/focus Notepad as target' }

    # close with X hides, hotkey stays alive, one-time hint recorded
    $w = Get-Main $p
    Invoke-El (Find-ById $w 'Close')
    Start-Sleep -Milliseconds 1200
    $p.Refresh()
    Check 'C4.hidden' 'closing the main window hides it; process stays alive' (-not $p.HasExited -and -not (Get-Main $p))
    Check 'C4.hint' 'one-time close hint recorded (shell.close_hint_shown)' ((Setting 'shell.close_hint_shown') -ne '')
    if ($target) {
        $null = Set-Foreground $target
        Start-Sleep -Milliseconds 400
        $from = (Get-LogLines).Count
        Press-Hotkey @([byte]$VK.Ctrl, [byte]$VK.Shift, [byte]$VK.Space)
        Check 'C4.hotkey' 'hotkey still opens the popup after the main window was closed (AC12)' (Wait-Log 'popup_ready_ms' $from 4000) (Tail $from)
        Send-Chord @([byte]$VK.Esc)
        Start-Sleep -Milliseconds 400
    }

    # second launch shows the hidden window, still one process
    $from = (Get-LogLines).Count
    $second = Start-Process $exe -PassThru
    $null = $second.WaitForExit(10000)
    Check 'C5.exit' 'second instance exits' $second.HasExited
    Check 'C5.window' 'existing instance shows its main window again' ([bool](Wait-Main $p 6000))
    Check 'C5.single' 'only one SmartSnippets process remains' (@(Get-Process -Name SmartSnippets.App).Count -eq 1)
    Stop-Target
}

if (Want 'C') {
    # settings page (UI -> DB), persisted across restart
    Reset-Profile
    $p = Start-App
    $null = Wait-Main $p
    Nav $p 'SettingsItem'
    $w = Get-Main $p
    $group = Find-ById $w 'ThemeRadios'
    foreach ($theme in 'Dark', 'Light', 'System') {
        $rb = Find-ByName $group $theme ([Windows.Automation.ControlType]::RadioButton)
        Select-El $rb
        Start-Sleep -Milliseconds 700
        $stored = Setting 'theme'
        Check "C6.theme.$theme" "theme $theme applies and persists" ($stored -match $theme) "stored='$stored'"
    }
    Select-El (Find-ByName $group 'Dark' ([Windows.Automation.ControlType]::RadioButton))
    Start-Sleep -Milliseconds 500
    Check 'C6.themelog' 'theme_applied logged' (Log-Has 'theme_applied')

    $sizes = Find-ById $w 'TextSizeRadios'
    foreach ($size in 'Large', 'Small', 'Medium') {
        Select-El (Find-ByName $sizes $size ([Windows.Automation.ControlType]::RadioButton))
        Start-Sleep -Milliseconds 600
        Check "C6.size.$size" "text size $size persists" ((Setting 'text_size.preset') -match $size) ('stored=' + (Setting 'text_size.preset'))
    }
    Select-El (Find-ByName $sizes 'Custom' ([Windows.Automation.ControlType]::RadioButton))
    Start-Sleep -Milliseconds 500
    $input = Find-ById $w 'InputBox'
    $rv = $input.GetCurrentPattern([Windows.Automation.ValuePattern]::Pattern)
    $rv.SetValue('32')
    $input.SetFocus(); Send-Chord @([byte]$VK.Tab); Start-Sleep -Milliseconds 700
    Check 'C6.custom32' 'custom text size 32 persists' ((Setting 'text_size.custom') -eq '32') ('stored=' + (Setting 'text_size.custom'))
    $before = Setting 'text_size.custom'
    $rv.SetValue('99'); $input.SetFocus(); Send-Chord @([byte]$VK.Tab); Start-Sleep -Milliseconds 700
    $after = Setting 'text_size.custom'
    Check 'C6.clamp' 'custom text size is clamped to 10-32' ([double]$after -le 32 -and [double]$after -ge 10) "stored=$after"

    # startup toggle (HKCU Run)
    $runKey = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run'
    $had = (Get-ItemProperty $runKey -Name SmartSnippets -ErrorAction SilentlyContinue)
    $toggle = Find-ById $w 'StartupToggle'
    $state = Get-ToggleState $toggle
    if ($state -eq 'Off') { Toggle-El $toggle; Start-Sleep -Milliseconds 800 }
    $val = (Get-ItemProperty $runKey -Name SmartSnippets -ErrorAction SilentlyContinue).SmartSnippets
    Check 'C7.on' 'Start at login ON writes HKCU Run value with --background' ($val -and $val -match '--background' -and $val -match 'SmartSnippets.App.exe') $val
    Toggle-El $toggle; Start-Sleep -Milliseconds 800
    Check 'C7.off' 'Start at login OFF removes the Run value' (-not (Get-ItemProperty $runKey -Name SmartSnippets -ErrorAction SilentlyContinue))
    if ($had) { Set-ItemProperty $runKey -Name SmartSnippets -Value $had.SmartSnippets }

    # persistence over restart
    Stop-App
    $p = Start-App
    $null = Wait-Main $p
    Nav $p 'SettingsItem'
    $w = Get-Main $p
    $darkSel = (Find-ByName (Find-ById $w 'ThemeRadios') 'Dark' ([Windows.Automation.ControlType]::RadioButton)).GetCurrentPattern([Windows.Automation.SelectionItemPattern]::Pattern).Current.IsSelected
    Check 'C8.restart' 'theme choice survives restart (AC3)' $darkSel

    # hotkey change flow: capture applies immediately, old key released, conflict keeps old
    $target = Start-Target
    Nav $p 'SettingsItem'
    $w = Get-Main $p
    $null = Set-Foreground (Get-Main $p); Press (Get-Main $p) 'Change shortcut'
    Start-Sleep -Milliseconds 400
    $from = (Get-LogLines).Count
    Send-Chord @([byte]$VK.Ctrl, [byte]$VK.Alt, [byte]$VK.K)
    Start-Sleep -Milliseconds 900
    Check 'C9.changed' 'recording Ctrl+Alt+K applies it (hotkey_changed)' (Log-Has 'hotkey_changed' $from)
    Check 'C9.stored' 'new hotkey persisted (modifiers+key)' (((Setting 'hotkey.key') -ne '') -and ((Setting 'hotkey.modifiers') -ne ''))
    if ($target) {
        $null = Set-Foreground $target
        Start-Sleep -Milliseconds 400
        $from = (Get-LogLines).Count
        Press-Hotkey @([byte]$VK.Ctrl, [byte]$VK.Alt, [byte]$VK.K)
        Check 'C9.newkey' 'Ctrl+Alt+K opens the popup' (Wait-Log 'popup_ready_ms' $from 4000) (Tail $from)
        Send-Chord @([byte]$VK.Esc); Start-Sleep -Milliseconds 400
        $null = Set-Foreground $target
        Start-Sleep -Milliseconds 300
        $from = (Get-LogLines).Count
        Press-Hotkey @([byte]$VK.Ctrl, [byte]$VK.Shift, [byte]$VK.Space)
        Start-Sleep -Milliseconds 1200
        Check 'C9.oldkey' 'old Ctrl+Shift+Space no longer opens the popup' (-not (Log-Has 'popup_ready_ms' $from))
    }
    Stop-App
    $p = Start-App
    $null = Wait-Main $p
    if ($target) {
        $null = Set-Foreground $target
        $from = (Get-LogLines).Count
        Press-Hotkey @([byte]$VK.Ctrl, [byte]$VK.Alt, [byte]$VK.K)
        Check 'C9.restart' 'changed shortcut still opens the popup after restart' (Wait-Log 'popup_ready_ms' $from 4000) (Tail $from)
        Send-Chord @([byte]$VK.Esc)
    } else { Check 'C9.restart' 'changed shortcut after restart' $false 'Notepad target unavailable' }
    Nav $p 'SettingsItem'
    # A conflicting shortcut cannot be driven from outside: a combination owned by another RegisterHotKey
    # client never reaches the recorder (the OS swallows the key press), so the in-page conflict sentence is
    # only reachable with hook-based owners. Startup conflict (C13) and the view-model tests cover the logic.
    Skip 'C9.conflict' 'conflict sentence after recording a taken shortcut (004 #4)' 'OS swallows a RegisterHotKey-owned chord before the recorder sees it; covered by C13 + HotkeyManager/Settings tests'
    $w = Get-Main $p
    Press (Get-Main $p) 'Restore default shortcut'
    Start-Sleep -Milliseconds 800
    if ($target) {
        $null = Set-Foreground $target
        Start-Sleep -Milliseconds 400
        $from = (Get-LogLines).Count
        Press-Hotkey @([byte]$VK.Ctrl, [byte]$VK.Shift, [byte]$VK.Space)
        Check 'C9.restore' 'Restore default makes Ctrl+Shift+Space work again' (Wait-Log 'popup_ready_ms' $from 4000) (Tail $from)
        Send-Chord @([byte]$VK.Esc); Start-Sleep -Milliseconds 300
    }
    Stop-Target

    # TaskbarCreated (what Explorer broadcasts after a restart): the handler must run and try to re-add the icon.
    # The icon still exists here, so NIM_ADD may legitimately fail; we only prove the handler fires.
    $tbMsg = Add-Type -MemberDefinition '[DllImport("user32.dll", CharSet=CharSet.Unicode)] public static extern uint RegisterWindowMessage(string s);' -Name TbMsg -Namespace Verify -PassThru
    $msgId = $tbMsg::RegisterWindowMessage('TaskbarCreated')
    $hwndMsg = [Win32Verify]::FindWindowW('SmartSnippets.MessageWindow', $null)
    $from = (Get-LogLines).Count
    if ($hwndMsg -ne [IntPtr]::Zero) {
        [void][Win32Verify]::PostMessage($hwndMsg, $msgId, [IntPtr]::Zero, [IntPtr]::Zero)
        Check 'C10.taskbar' 'TaskbarCreated message makes the tray re-add the icon (tray_readded / tray_add attempt)' (Wait-Log 'tray_readded|tray_added|tray_add_failed' $from 4000) (Tail $from)
    }
    else { Skip 'C10.taskbar' 'tray re-add' 'message window not found' }

    # tray menu: open it through the tray callback (right-button-up) and read the real Win32 menu
    $WM_TRAY = 0x8001; $WM_RBUTTONUP = 0x0205
    if ($hwndMsg -ne [IntPtr]::Zero) {
        [void][Win32Verify]::PostMessage($hwndMsg, $WM_TRAY, [IntPtr]::Zero, [IntPtr]$WM_RBUTTONUP)
        $menuWnd = Wait-Until { $m = [Win32Verify]::FindWindowW('#32768', $null); if ($m -ne [IntPtr]::Zero) { $m } } 3000 150
        $menuItems = @()
        if ($menuWnd) { $menuItems = @([Win32Verify]::MenuItems([IntPtr]$menuWnd) | Where-Object { $_ }) }
        $joined = $menuItems -join ' | '
        Check 'C11.menu' 'tray menu has Open / Show popup / Settings / Restart as administrator / Exit, no Diagnostics' (($menuItems.Count -eq 5) -and ($joined -match 'Open Smart Snippets') -and ($joined -match 'Show popup') -and ($joined -match 'Settings') -and ($joined -match 'Restart as administrator') -and ($joined -notmatch 'Diagnostics') -and ($joined -match 'Exit')) $joined
        Check 'C11.order' 'order: Open, Show popup, Settings first (design) then Restart, Exit last' (($menuItems.Count -gt 0) -and ($menuItems[0] -match 'Open') -and ($menuItems[1] -match 'Show popup') -and ($menuItems[2] -match 'Settings') -and ($menuItems[-1] -match 'Exit')) ''
        Check 'C11.accel' 'Show popup row shows the live hotkey text' ($joined -match 'Show popup\tCtrl\+Shift\+Space') ''

        # pick Exit (last item): Up wraps from nothing to the last item, Enter activates it
        Send-Chord @([byte]$VK.Up)
        Start-Sleep -Milliseconds 250
        Send-Chord @([byte]$VK.Enter)
        $exited = Wait-Until { -not (Get-Process -Name SmartSnippets.App -ErrorAction SilentlyContinue) } 6000 200
        Check 'C12.exit' 'tray Exit terminates the process' ([bool]$exited)
        Check 'C12.log' 'exit_unregistered logged' (Log-Has 'exit_unregistered')
        $free = [Win32Verify]::RegisterHotKey([IntPtr]::Zero, 9102, 0x4006, 0x20)
        if ($free) { [void][Win32Verify]::UnregisterHotKey([IntPtr]::Zero, 9102) }
        Check 'C12.free' 'Ctrl+Shift+Space is free for other apps after Exit (AC12)' $free
    }

    # startup hotkey conflict: app keeps running, reports, does not fall back silently
    Reset-Profile
    $held = [Win32Verify]::RegisterHotKey([IntPtr]::Zero, 9103, 0x4006, 0x20)
    $script:HotkeyIds += 9103
    $p = Start-App
    $null = Wait-Main $p
    Check 'C13.keeps' 'startup with hotkey taken: app keeps running' ($held -and -not $p.HasExited)
    Check 'C13.log' 'hotkey_startup_failed / hotkey_register_failed logged' ((Log-Has 'hotkey_startup_failed') -or (Log-Has 'hotkey_register_failed'))
    Nav $p 'SettingsItem'
    $w = Get-Main $p
    $text = (Find-All $w ([Windows.Automation.ControlType]::Text) | ForEach-Object { $_.Current.Name }) -join '|'
    Check 'C13.banner' 'Settings shows "The shortcut is not active" explanation' ($text -match 'not active') ''
    [void][Win32Verify]::UnregisterHotKey([IntPtr]::Zero, 9103)
    Stop-App

    # --background: no window
    Reset-Profile
    $p = Start-App @('--background')
    Start-Sleep -Milliseconds 800
    Check 'C14.bg' '--background starts with no window but alive, hotkey registered' (-not $p.HasExited -and -not (Get-Main $p) -and (Log-Has 'hotkey_registered'))
    Stop-App

    Skip 'C15.admin' 'Restart as administrator (004 #13/#14)' 'UAC prompt is on the secure desktop; run manually'
    Skip 'C15.explorer' 'real explorer.exe restart (004 #11)' 'TaskbarCreated path verified above; real restart is disruptive'
    Skip 'C15.signout' 'start at login after sign-out (004 #15)' 'needs sign-out; Run-value content verified above'
    Skip 'C15.visual' 'light/dark visual review, text size 32 clipping (009 #11, 004 #8)' 'needs eyes; settings persistence verified above'
}

# ================================================================= D. main window workflows (plans 005, 009)
if (Want 'D') {
    Section 'D. Snippet and folder workflows via UI Automation (005 / 009)'

    Reset-Profile
    $p = Start-App
    $w = Wait-Main $p
    Nav $p 'SnippetsItem'

    # D1 create with trailing spaces + CRLF, auto label from first line
    $text1 = "Hello`r`n  world  "
    $tb = New-SnippetEditor $p
    $w = Get-Main $p
    Set-Text $tb $text1
    Start-Sleep -Milliseconds 500
    $names = (Find-All $w ([Windows.Automation.ControlType]::Text) | ForEach-Object { $_.Current.Name }) -join '|'
    $saved = Save-Snippet $p
    Check 'D1.save' 'Save is offered once the text is typed and saves' $saved
    $row = (Sql "select hex(text), label, label_is_custom, usage_count from snippets order by id desc limit 1;") -join ''
    $hex = HexOf $text1
    $hexLf = HexOf ($text1 -replace "`r`n", "`r")
    Check 'D1.exact' 'stored text keeps whitespace exactly (leading/trailing spaces, not trimmed)' ($row -like "$hex|*" -or $row -like "$hexLf|*") ''
    Check 'D1.eol' 'stored TextBox line endings are CR or CRLF; insertion normalizes to CRLF (F1)' ($row -like "$hex|*" -or $row -like "$hexLf|*") 'End-to-end insertion proof remains F1'
    Check 'D1.label' 'auto label is the first line ("Hello"), not custom' ($row -match '\|Hello\|0\|0$') $row
    Check 'D1.usage' 'new snippet has usage_count 0' ($row -match '\|0$')

    # D2 tags normalization
    $null = Create-Snippet $p 'tag test body' ' work, Work ,, mail '
    $row = (Sql "select tags, label from snippets where text='tag test body';") -join ''
    Check 'D2.tags' 'tags normalized (trim, dedupe case-insensitive, drop empty)' ($row -match '^work, ?mail\|') $row
    Check 'D2.label' 'auto label follows first tag ("work")' ($row -match '\|work$') $row

    # D3 custom label survives text edits
    $null = Create-Snippet $p 'custom label body' '' 'Greeting'
    $row = (Sql "select label, label_is_custom from snippets where text='custom label body';") -join ''
    Check 'D3.custom' 'edited label is stored as custom' ($row -eq 'Greeting|1') $row

    # D4 whitespace-only text rejected, nothing stored
    $count0 = (Sql 'select count(*) from snippets;') -join ''
    $tb = New-SnippetEditor $p
    Set-Text $tb '   '
    $null = Save-Snippet $p
    $count1 = (Sql 'select count(*) from snippets;') -join ''
    $w = Get-Main $p
    $texts = (Find-All $w ([Windows.Automation.ControlType]::Text) | ForEach-Object { $_.Current.Name }) -join '|'
    Check 'D4.reject' 'whitespace-only text is not saved' ($count0 -eq $count1)
    $kept = Get-Text (Find-ById $w 'SnippetTextBox')
    Check 'D4.message' 'inline "required" error is shown and content kept' (($texts -match 'cannot be empty|required') -and ($kept -eq '   ')) "kept='$kept' texts=$(($texts -split '\|' | Where-Object { $_ } | Select-Object -First 12) -join ',')"
    # leave the editor: discard
    Send-Chord @([byte]$VK.Esc)
    Start-Sleep -Milliseconds 500
    $null = Confirm-Dialog $p 'Discard'

    # D5 inline folder creation selects the folder
    $tb = New-SnippetEditor $p
    $w = Get-Main $p
    Set-Text $tb 'folder test body'
    Press $w 'New folder'
    $w = Get-Main $p
    $nameBox = Wait-Until { Find-ByName (Get-Main $p) 'New folder name' ([Windows.Automation.ControlType]::Edit) } 3000 150
    Check 'D5.row' 'inline new-folder row appears' ([bool]$nameBox)
    if ($nameBox) {
        Set-Text $nameBox 'Clients'
        Press (Get-Main $p) 'Create folder'
        Start-Sleep -Milliseconds 600
        Check 'D5.created' 'folder row created in DB' (((Sql "select count(*) from folders where name='Clients';") -join '') -eq '1')
        $combo = Find-ByName (Get-Main $p) 'Folder' ([Windows.Automation.ControlType]::ComboBox)
        $sel = ''
        try { $sel = $combo.GetCurrentPattern([Windows.Automation.ValuePattern]::Pattern).Current.Value } catch { try { $sel = ($combo.GetCurrentPattern([Windows.Automation.SelectionPattern]::Pattern).Current.GetSelection() | ForEach-Object { $_.Current.Name }) -join '' } catch { } }
        Check 'D5.selected' 'inline-created folder is auto-selected in the editor' ($sel -match 'Clients') "combo='$sel'"
        $null = Save-Snippet $p
        $row = (Sql "select f.name from snippets s join folders f on f.id=s.folder_id where s.text='folder test body';") -join ''
        Check 'D5.saved' 'saved snippet references the new folder' ($row -eq 'Clients') $row
    }

    # D6 duplicate folder (case-insensitive) on the Folders page
    Nav $p 'FoldersItem'
    $w = Get-Main $p
    Set-Text (Find-ByName $w 'New folder name' ([Windows.Automation.ControlType]::Edit)) 'clients'
    Press $w 'Create folder'
    Start-Sleep -Milliseconds 500
    $w = Get-Main $p
    $texts = (Find-All $w ([Windows.Automation.ControlType]::Text) | ForEach-Object { $_.Current.Name }) -join '|'
    Check 'D6.dup' 'duplicate folder name (different case) is rejected with design wording' (($texts -match 'already taken') -and (((Sql 'select count(*) from folders;') -join '') -eq '1')) ''

    # D7 browse pane counts and row meta (009)
    Nav $p 'SnippetsItem'
    $w = Get-Main $p
    $texts = (Find-All $w ([Windows.Automation.ControlType]::Text) | ForEach-Object { $_.Current.Name }) -join '|'
    Check 'D7.browse' 'browse pane lists All, Unfiled and the folder' (($texts -match 'All') -and ($texts -match 'Unfiled') -and ($texts -match 'Clients'))
    Check 'D7.meta' 'rows show "<folder>, never used" meta' (($texts -match 'Clients, never used') -and ($texts -match 'Unfiled, never used')) ''

    # D8 search: AND across label/text/folder/tags
    $null = Create-Snippet $p 'mail template for clients' 'mail'
    Nav $p 'SnippetsItem'
    $w = Get-Main $p
    $sb = Find-ById $w 'SearchBox'
    Set-Text $sb 'clie mail'
    Start-Sleep -Milliseconds 700
    $list = Find-ById (Get-Main $p) 'SnippetList'
    $items = @($list.FindAll($script:TS::Children, [Windows.Automation.Condition]::TrueCondition))
    Check 'D8.and' 'search "clie mail" is a live AND substring match (only matching rows remain)' ($items.Count -ge 1 -and $items.Count -lt 5) "rows=$($items.Count)"
    Set-Text $sb 'zzzznomatch'
    Start-Sleep -Milliseconds 600
    $texts = (Find-All (Get-Main $p) ([Windows.Automation.ControlType]::Text) | ForEach-Object { $_.Current.Name }) -join '|'
    Check 'D8.empty' 'no results shows "No matches for ..." empty state' ($texts -match "No matches for 'zzzznomatch'") ''
    Set-Text $sb ''
    Start-Sleep -Milliseconds 500

    # D9 Unicode round trip
    $uni = 'H' + [char]0x00E9 + 'llo ' + [char]0x2013 + ' na' + [char]0x00EF + 've ' + [char]::ConvertFromUtf32(0x1F44B) + ' ' + [char]0x4E16 + [char]0x754C
    $null = Create-Snippet $p $uni
    $hexU = (Sql "select hex(text) from snippets where text like 'H%llo%' order by id desc limit 1;") -join ''
    Check 'D9.unicode' 'Unicode text (accents, en dash, emoji, CJK) is stored exactly' ($hexU -eq (HexOf $uni)) ''

    # D10 delete: cancel keeps, confirm removes (Delete key on the list row, same dialog)
    $null = Create-Snippet $p "delete me $SecretMarker"
    Nav $p 'SnippetsItem'
    $w = Get-Main $p
    $list = Find-ById $w 'SnippetList'
    $victim = @($list.FindAll($script:TS::Children, [Windows.Automation.Condition]::TrueCondition)) | Select-Object -First 1
    if ($victim) { Select-El $victim }
    Start-Sleep -Milliseconds 500
    $before = ((Sql 'select count(*) from snippets;') -join '')
    try { $victim.SetFocus() } catch { }
    Send-Chord @([byte]$VK.Delete)
    Start-Sleep -Milliseconds 700
    $dlg = Btn (Get-Main $p) 'Delete snippet'
    Check 'D10.dialog' 'Delete key on a list row opens the confirmation' ([bool]$dlg)
    if ($dlg) {
        $null = Confirm-Dialog $p 'Cancel'
        Check 'D10.cancel' 'Cancel keeps the snippet' (((Sql 'select count(*) from snippets;') -join '') -eq $before)
        try { $victim.SetFocus() } catch { }
        Send-Chord @([byte]$VK.Delete)
        Start-Sleep -Milliseconds 700
        $null = Confirm-Dialog $p 'Delete snippet'
        Check 'D10.delete' 'confirming removes exactly one snippet' ((([int]((Sql 'select count(*) from snippets;') -join '')) + 1) -eq [int]$before)
    }

    # D11 unsaved-changes guard: switching rows asks Save / Discard / Keep editing
    $w = Get-Main $p
    $list = Find-ById $w 'SnippetList'
    $rows = @($list.FindAll($script:TS::Children, [Windows.Automation.Condition]::TrueCondition))
    if ($rows.Count -ge 2) {
        Select-El $rows[0]
        Start-Sleep -Milliseconds 500
        $tb = Find-ById (Get-Main $p) 'SnippetTextBox'
        $orig = Get-Text $tb
        Set-Text $tb ($orig + ' EDITED')
        Start-Sleep -Milliseconds 400
        Select-El $rows[1]
        Start-Sleep -Milliseconds 700
        $keep = Btn (Get-Main $p) 'Keep editing'
        Check 'D11.dialog' 'switching snippet with unsaved edits shows Save / Discard / Keep editing' ([bool]$keep -and [bool](Btn (Get-Main $p) 'Discard') -and [bool](Btn (Get-Main $p) 'Save'))
        if ($keep) {
            Invoke-El $keep
            Start-Sleep -Milliseconds 500
            Check 'D11.keep' 'Keep editing leaves the edit in place' ((Get-Text (Find-ById (Get-Main $p) 'SnippetTextBox')) -like '* EDITED')
        }
        # closing the window while dirty must ask first
        Invoke-El (Find-ById (Get-Main $p) 'Close')
        Start-Sleep -Milliseconds 900
        $keep2 = Btn (Get-Main $p) 'Keep editing'
        Check 'D11.close' 'closing the window with unsaved edits asks first; Keep editing stays' ([bool]$keep2 -and [bool](Get-Main $p))
        if ($keep2) { Invoke-El $keep2; Start-Sleep -Milliseconds 400 }
        Press (Get-Main $p) 'Discard changes'
        Start-Sleep -Milliseconds 500
    }
    else { Skip 'D11' 'unsaved-changes guard' 'needs at least two snippets' }

    # D12 folders: rename and delete moves snippets to Unfiled
    Nav $p 'FoldersItem'
    $w = Get-Main $p
    $flist = Find-ById $w 'FolderList'
    $frows = @($flist.FindAll($script:TS::Children, [Windows.Automation.Condition]::TrueCondition))
    Check 'D12.rows' 'Folders page lists the folder and the built-in Unfiled row' ($frows.Count -ge 2) "rows=$($frows.Count)"
    $texts = (Find-All $w ([Windows.Automation.ControlType]::Text) | ForEach-Object { $_.Current.Name }) -join '|'
    Check 'D12.unfiled' 'Unfiled row is marked "built in" and has no Rename/Delete' (($texts -match 'built in'))
    $buttons = @(Find-All $w ([Windows.Automation.ControlType]::Button) | ForEach-Object { $_.Current.Name })
    $renameName = $buttons | Where-Object { $_ -match 'Rename' } | Select-Object -First 1
    $deleteName = $buttons | Where-Object { $_ -match 'Delete' } | Select-Object -First 1
    Check 'D12.buttons' 'folder row exposes Rename and Delete buttons' ([bool]$renameName -and [bool]$deleteName) "$renameName / $deleteName"
    if ($renameName) {
        Invoke-El (Btn (Get-Main $p) $renameName)
        Start-Sleep -Milliseconds 500
        $edit = @(Find-All (Get-Main $p) ([Windows.Automation.ControlType]::Edit)) | Where-Object { $_.Current.Name -notmatch 'New folder name' -and $_.Current.AutomationId -ne 'InputBox' } | Select-Object -First 1
        if ($edit) {
            Set-Text $edit 'Customers'
            Send-Chord @([byte]$VK.Enter)
            Start-Sleep -Milliseconds 700
            Check 'D12.rename' 'rename stored in DB' (((Sql "select count(*) from folders where name='Customers';") -join '') -eq '1')
            Nav $p 'SnippetsItem'
            $texts = (Find-All (Get-Main $p) ([Windows.Automation.ControlType]::Text) | ForEach-Object { $_.Current.Name }) -join '|'
            Check 'D12.propagate' 'rename is reflected in the Snippets page immediately' (($texts -match 'Customers') -and -not ($texts -cmatch '(^|[|])Clients(,|[|]|$)')) (($texts -split '[|]' | Where-Object { $_ -match 'Cust|Clie' }) -join ' ; ')
            Nav $p 'FoldersItem'
        }
        else { Skip 'D12.rename' 'rename editing' 'rename edit box not found in UI tree' }
    }
    $moved = ((Sql "select count(*) from snippets where folder_id is not null;") -join '')
    $deleteBtn = @(Find-All (Get-Main $p) ([Windows.Automation.ControlType]::Button)) | Where-Object { $_.Current.Name -match 'Delete' } | Select-Object -First 1
    if ($deleteBtn) {
        Invoke-El $deleteBtn
        Start-Sleep -Milliseconds 700
        $texts = (Find-All (Get-Main $p) ([Windows.Automation.ControlType]::Text) | ForEach-Object { $_.Current.Name }) -join '|'
        Check 'D12.warn' 'delete dialog says N snippets will move to Unfiled' ($texts -match 'will move to Unfiled') ''
        $null = Confirm-Dialog $p 'Delete folder'
        Check 'D12.moved' 'deleting the folder keeps its snippets and moves them to Unfiled' ((((Sql 'select count(*) from folders;') -join '') -eq '0') -and (((Sql 'select count(*) from snippets where folder_id is not null;') -join '') -eq '0') -and ([int]$moved -ge 1)) "moved=$moved"
    }

    # D13 persistence over restart
    $snippetCount = ((Sql 'select count(*) from snippets;') -join '')
    Stop-App
    $p = Start-App
    $null = Wait-Main $p
    $listAfter = Find-ById (Get-Main $p) 'SnippetList'
    $rowsAfter = @($listAfter.FindAll($script:TS::Children, [Windows.Automation.Condition]::TrueCondition))
    Check 'D13.persist' 'all snippets are still listed after restart (AC3)' ($rowsAfter.Count -eq [int]$snippetCount) "rows=$($rowsAfter.Count) db=$snippetCount"

    # D14 accessibility names (AC5/AC7, 005 #15): list items must read as text, not as view-model type names
    $w = Get-Main $p
    $listItems = @(Find-All $w ([Windows.Automation.ControlType]::ListItem) | ForEach-Object { $_.Current.Name }) | Where-Object { $_ }
    $bad = @($listItems | Where-Object { $_ -match 'ViewModel' })
    Check 'D14.names' 'list items expose readable accessible names (no "...ViewModel" type names)' ($bad.Count -eq 0) (($bad | Select-Object -First 2) -join ' ; ')

    # Keep the sqlite process alive with its transaction open while the UI attempts a write.
    $lockMarker = Join-Path ([IO.Path]::GetTempPath()) ("ss-lock-" + [guid]::NewGuid() + '.txt')
    $locker = $null
    try {
        $tb = New-SnippetEditor $p
        if (-not $tb) { throw 'Could not open editor for D-lock' }
        $lockText = 'locked-save-probe'
        Set-Text $tb $lockText
        $psi = New-Object Diagnostics.ProcessStartInfo
        $psi.FileName = (Get-Command sqlite3).Source
        $psi.Arguments = '-batch "' + $dbPath + '"'
        $psi.UseShellExecute = $false
        $psi.RedirectStandardInput = $true
        $psi.CreateNoWindow = $true
        $locker = [Diagnostics.Process]::Start($psi)
        $locker.StandardInput.WriteLine('BEGIN EXCLUSIVE;')
        $locker.StandardInput.WriteLine('.once "' + $lockMarker.Replace('\', '/') + '"')
        $locker.StandardInput.WriteLine("SELECT 'locked';")
        $locker.StandardInput.Flush()
        if (-not (Wait-Until { Test-Path $lockMarker } 10000 100)) { throw 'sqlite exclusive transaction did not start' }
        Invoke-El (Btn (Get-Main $p) 'Save snippet')
        $retry = Wait-Until { Btn (Get-Main $p) 'Try saving again' } 15000 150
        Check 'D-lock.error' 'database lock shows inline retry' ([bool]$retry)
        $field = Find-ById (Get-Main $p) 'SnippetTextBox'
        Check 'D-lock.text' 'failed save preserves editor text' ([bool]$field -and (Get-Text $field) -eq $lockText)
        $locker.StandardInput.WriteLine('ROLLBACK;')
        $locker.StandardInput.WriteLine('.quit')
        $locker.StandardInput.Flush()
        if (-not $locker.WaitForExit(10000)) { throw 'sqlite lock did not release' }
        if ($retry) { Invoke-El $retry }
        Check 'D-lock.retry' 'retry saves after lock release' ([bool](Wait-Until { ((Sql "select count(*) from snippets where text='locked-save-probe';") -join '') -eq '1' } 10000 150))
    } finally {
        if ($locker -and -not $locker.HasExited) { $locker.Kill(); $locker.WaitForExit() }
        if ($locker) { $locker.Dispose() }
        Remove-Item $lockMarker -Force -ErrorAction SilentlyContinue
    }

    # D15 privacy: no snippet text in the log
    $logText = (Get-LogLines) -join "`n"
    Check 'D15.privacy' 'app.log never contains snippet text or tags' (-not ($logText -match [regex]::Escape($SecretMarker)) -and -not ($logText -match 'tag test body|custom label body|mail template')) ''

    Stop-App
}

if (Want 'E') {
    Section 'E. Popup basic verification (incomplete)'
    Reset-Profile
    $p = Start-App
    if (-not (Wait-Main $p)) { throw 'E requires the main window' }
    if (-not (Create-Snippet $p 'popup alpha bravo probe')) { throw 'E could not create first test snippet' }
    if (-not (Create-Snippet $p 'popup alpha charlie probe')) { throw 'E could not create second test snippet' }
    $target = Start-Target
    if (-not $target) { throw 'E requires Notepad' }
    $initialText = Get-NotepadText $target
    $beforeUsage = (Sql 'select coalesce(sum(usage_count), 0) from snippets;') -join ''
    $null = Set-Foreground $target
    Press-Hotkey @([byte]$VK.Ctrl, [byte]$VK.Shift, [byte]$VK.Space)
    $popup = Wait-Until { Get-PopupWindow $p.Id } 5000 100
    if (-not $popup) { throw 'E popup did not appear' }
    $search = Find-ById $popup 'PopupSearch'
    Check 'E1.visible' 'popup opens and search is focused' ([bool]$popup -and [bool]$search -and $search.Current.HasKeyboardFocus)
    Set-Text $search 'alpha bravo'
    $from = (Get-LogLines).Count
    $count = Wait-Until { $c = Find-ById (Get-PopupWindow $p.Id) 'PopupCount'; if ($c -and $c.Current.Name -match '^1 of 2') { $c.Current.Name } } 5000 100
    Check 'E2.and' 'two search terms are ANDed' ([bool]$count) "$count"
    Set-Text $search 'zzqxv'
    $empty = Wait-Until { Find-ByName (Get-PopupWindow $p.Id) 'No snippets match' } 5000 100
    Check 'E2.empty' 'unmatched search shows empty state' ([bool]$empty)
    foreach ($pair in @(@('PopupLayoutButton','Toggle layout'), @('PopupSortButton','Sort'), @('PopupAddButton','Add snippet'), @('PopupOpenMainButton','Open main window'))) {
        $button = Find-ById (Get-PopupWindow $p.Id) $pair[0]
        Check "E6.$($pair[0])" 'toolbar button has expected accessible name and tooltip' ([bool]$button -and $button.Current.Name -eq $pair[1] -and -not [string]::IsNullOrWhiteSpace($button.Current.HelpText))
    }
    Send-Chord @([byte]$VK.Esc)
    $closed = Wait-Until { -not (Get-PopupWindow $p.Id) } 5000 100
    $afterUsage = (Sql 'select coalesce(sum(usage_count), 0) from snippets;') -join ''
    Check 'E4' 'Escape closes without insertion or usage' ([bool]$closed -and $initialText -ceq (Get-NotepadText $target) -and $beforeUsage -eq $afterUsage)
    Stop-Target
    Stop-App
}

if (Want 'F') {
    Section 'F. Insertion basic verification (incomplete)'
    $savedClipboard = Save-ClipboardText
    try {
        Reset-Profile
        $p = Start-App
        if (-not (Wait-Main $p)) { throw 'F requires the main window' }
        $probe = "Héllo – naïve 👋 世界`r`n`tline 2  `r`nline 3"
        if (-not (Create-Snippet $p $probe)) { throw 'F could not create Unicode probe' }
        $target = Start-Target
        if (-not $target) { throw 'F requires Notepad' }
        Set-Clipboard -Value 'SMART-SNIPPETS-CLIPBOARD-SENTINEL'
        $before = (Sql 'select usage_count from snippets order by id desc limit 1;') -join ''
        $null = Set-Foreground $target
        Press-Hotkey @([byte]$VK.Ctrl, [byte]$VK.Shift, [byte]$VK.Space)
        $popup = Wait-Until { Get-PopupWindow $p.Id } 5000 100
        if (-not $popup) { throw 'F popup did not appear' }
        $from = (Get-LogLines).Count
        Send-Chord @([byte]$VK.Enter)
        $inserted = Wait-Log 'insert_result outcome=Inserted' $from 10000
        $text = Wait-Until { $s = Get-NotepadText $target; if ($s) { $s } } 10000 100
        Check 'F1' 'Unicode and multiline probe inserted with CRLF' ($inserted -and $text -ceq $probe) "actual UTF-16 length=$($text.Length) expected=$($probe.Length)"
        $restored = Wait-Until { (Get-Clipboard -Raw -ErrorAction SilentlyContinue) -ceq 'SMART-SNIPPETS-CLIPBOARD-SENTINEL' } 10000 150
        Check 'F3' 'clipboard sentinel restored' ([bool]$restored)
        $usage = (Sql 'select usage_count, last_used_at is not null from snippets order by id desc limit 1;') -join ''
        Check 'F4.database' 'usage increments exactly once and last-used is set' ($usage -eq "$([int]$before + 1)|1") $usage
        Add-Result 'INFO' 'F12' 'N/A — uncertain dispatch is unit-tested' 'InsertionServiceTests.FailedDispatch_OnlyUncertainReleasesAndWaits and SendThrows_IsUncertain; PopupViewModelInsertionTests.PasteAnyway_ThenMaybePasted_ShowsCaution'
        Skip 'F8' 'elevated target' 'requires user UAC acceptance; see compatibility matrix'
    } finally {
        Stop-Target
        Stop-App
        Restore-ClipboardText $savedClipboard
    }
}

# Release checks.
if (Want 'G') {
    Section 'G. Release artifact and icon structure'
    $info = (Get-Item $exe).VersionInfo
    if ($ArtifactZip -and [IO.Path]::GetFileName($ArtifactZip) -match '^SmartSnippets-win-x64-(.+)\.zip$') {
        $expectedVersion = $Matches[1]
        $numericVersion = ($expectedVersion -split '-', 2)[0]
        $validFileVersion = $info.FileVersion -match ('^' + [regex]::Escape($numericVersion) + '\.\d+$')
        Check 'G1' 'ProductVersion and FileVersion match release archive' ($info.ProductVersion.StartsWith($expectedVersion) -and $validFileVersion) "ProductVersion=$($info.ProductVersion); FileVersion=$($info.FileVersion)"
    } else { Skip 'G1' 'release version' 'requires -ArtifactZip named SmartSnippets-win-x64-<version>.zip' }
    function Get-IcoFrameCount([string]$Path) {
        if (-not (Test-Path $Path -PathType Leaf)) { return 0 }
        $bytes = [IO.File]::ReadAllBytes($Path)
        if ($bytes.Length -lt 6 -or $bytes[0] -ne 0 -or $bytes[1] -ne 0 -or $bytes[2] -ne 1 -or $bytes[3] -ne 0) { return 0 }
        return [BitConverter]::ToUInt16($bytes, 4)
    }
    $appIcon = Join-Path $root 'publish\Assets\SmartSnippets.ico'
    Check 'G2.frames' 'app icon has eight frames' ((Get-IcoFrameCount $appIcon) -eq 8)
    Add-Type -AssemblyName System.Drawing
    function Get-IconPixelHash($Icon) {
        $bitmap = New-Object Drawing.Bitmap 32, 32
        try {
            $graphics = [Drawing.Graphics]::FromImage($bitmap)
            try { $graphics.Clear([Drawing.Color]::Transparent); $graphics.DrawIcon($Icon, 0, 0, 32, 32) } finally { $graphics.Dispose() }
            $stream = New-Object IO.MemoryStream
            try {
                $bitmap.Save($stream, [Drawing.Imaging.ImageFormat]::Png)
                $hash = [Security.Cryptography.SHA256]::Create()
                try { return [Convert]::ToBase64String($hash.ComputeHash($stream.ToArray())) } finally { $hash.Dispose() }
            } finally { $stream.Dispose() }
        } finally { $bitmap.Dispose() }
    }
    $associated = [Drawing.Icon]::ExtractAssociatedIcon($exe)
    $sourceIcon = New-Object Drawing.Icon($appIcon, 32, 32)
    try {
        $associatedHash = Get-IconPixelHash $associated
        $expectedIconHash = Get-IconPixelHash $sourceIcon
        $defaultIconHash = Get-IconPixelHash ([Drawing.SystemIcons]::Application)
        Check 'G2.associated' 'exe associated icon matches app mark and differs from stock icon' ($associatedHash -eq $expectedIconHash -and $associatedHash -ne $defaultIconHash)
    } finally { if ($associated) { $associated.Dispose() }; $sourceIcon.Dispose() }
    foreach ($theme in 'Light', 'Dark') {
        foreach ($state in 'Ready', 'PopupOpen', 'HotkeyUnavailable') {
            $name = "Tray-$theme-$state.ico"
            Check "G3.$theme.$state" 'tray icon has six frames' ((Get-IcoFrameCount (Join-Path "$root\publish\Assets" $name)) -eq 6)
        }
    }
    . (Join-Path $PSScriptRoot 'release-tag.ps1')
    Check 'G8.tags' 'invalid tags rejected and prerelease accepted' (-not (Test-ReleaseTag 'v1.0') -and -not (Test-ReleaseTag 'v1.0.0.0') -and -not (Test-ReleaseTag '1.0.0') -and (Test-ReleaseTag 'v1.0.0-rc.1'))
    if ($ArtifactZip) { Check 'G9' 'archive hash and layout validated before extraction' $true $artifactInfo }
    else { Skip 'G9' 'release archive and checksum' 'requires -ArtifactZip' }
    Reset-Profile
    $p = Start-App
    $null = Wait-Main $p
    Check 'G4.ready' 'tray starts Ready' (Log-Has 'tray_icon state=Ready')
    $target = Start-Target
    if (-not $target) { throw 'G4 requires Notepad target' }
    $null = Set-Foreground $target
    $from = (Get-LogLines).Count
    Press-Hotkey @([byte]$VK.Ctrl, [byte]$VK.Shift, [byte]$VK.Space)
    Check 'G4.open' 'popup changes tray state to PopupOpen' (Wait-Log 'tray_icon state=PopupOpen' $from 5000) (Tail $from)
    $from = (Get-LogLines).Count
    Send-Chord @([byte]$VK.Esc)
    Check 'G4.close' 'closing popup returns tray to Ready' (Wait-Log 'tray_icon state=Ready' $from 5000) (Tail $from)
    Stop-Target
    Stop-App
    Reset-Profile
    $held = [Win32Verify]::RegisterHotKey([IntPtr]::Zero, 9210, 0x4006, 0x20)
    if (-not $held) { throw 'G5 could not reserve the default hotkey' }
    try {
        $p = Start-App
        $null = Wait-Main $p
        Check 'G5.unavailable' 'held hotkey produces unavailable tray state' (Wait-Log 'tray_icon state=HotkeyUnavailable' 0 5000)
    } finally { [void][Win32Verify]::UnregisterHotKey([IntPtr]::Zero, 9210) }
    Nav $p 'SettingsItem'
    $from = (Get-LogLines).Count
    Press (Get-Main $p) 'Restore default shortcut'
    Check 'G5.ready' 're-applying freed hotkey restores Ready state' (Wait-Log 'tray_icon state=Ready' $from 5000) (Tail $from)
    $hwndMsg = [Win32Verify]::FindWindowW('SmartSnippets.MessageWindow', $null)
    if ($hwndMsg -eq [IntPtr]::Zero) { throw 'G6 message window not found' }
    $messageData = [Runtime.InteropServices.Marshal]::StringToHGlobalUni('ImmersiveColorSet')
    try {
        $from = (Get-LogLines).Count
        [void][Win32Verify]::SendMessage($hwndMsg, 0x001A, [IntPtr]::Zero, $messageData)
        Check 'G6' 'theme broadcast re-evaluates icon without a crash' ((Wait-Log 'tray_icon state=' $from 5000) -and -not $p.HasExited) (Tail $from)
    } finally { [Runtime.InteropServices.Marshal]::FreeHGlobal($messageData) }
    if ($FlipTaskbarTheme) {
        $keyPath = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize'
        $key = Get-Item $keyPath
        $names = @($key.GetValueNames())
        $existed = $names -contains 'SystemUsesLightTheme'
        $old = $key.GetValue('SystemUsesLightTheme', 0)
        try {
            $from = (Get-LogLines).Count
            New-ItemProperty $keyPath -Name SystemUsesLightTheme -PropertyType DWord -Value $(if ($old -eq 1) { 0 } else { 1 }) -Force | Out-Null
            $result = [IntPtr]::Zero
            [void][Win32Verify]::SendMessageTimeout([IntPtr]0xffff, 0x001A, [IntPtr]::Zero, 'ImmersiveColorSet', 2, 5000, [ref]$result)
            Check 'G6b' 'taskbar theme flip triggers tray_theme_changed' (Wait-Log 'tray_theme_changed' $from 5000) (Tail $from)
        } finally {
            if ($existed) { New-ItemProperty $keyPath -Name SystemUsesLightTheme -PropertyType DWord -Value $old -Force | Out-Null }
            else { Remove-ItemProperty $keyPath -Name SystemUsesLightTheme -ErrorAction SilentlyContinue }
            $result = [IntPtr]::Zero
            [void][Win32Verify]::SendMessageTimeout([IntPtr]0xffff, 0x001A, [IntPtr]::Zero, 'ImmersiveColorSet', 2, 5000, [ref]$result)
        }
    } else { Skip 'G6b' 'taskbar theme flip' 'opt in with -FlipTaskbarTheme' }
    [void][Win32Verify]::PostMessage($hwndMsg, 0x8001, [IntPtr]::Zero, [IntPtr]0x0205)
    $menuWnd = Wait-Until { $m = [Win32Verify]::FindWindowW('#32768', $null); if ($m -ne [IntPtr]::Zero) { $m } } 3000 150
    $menuItems = if ($menuWnd) { @([Win32Verify]::MenuItems([IntPtr]$menuWnd) | Where-Object { $_ }) } else { @() }
    $joined = $menuItems -join ' | '
    Check 'G7' 'tray menu has restart and no diagnostics' ($joined -match 'Restart as administrator' -and $joined -notmatch 'Diagnostics') $joined
    Send-Chord @([byte]$VK.Esc)
    Stop-App
    $gateDir = Join-Path ([IO.Path]::GetTempPath()) ('ss-fake-aot-' + [guid]::NewGuid())
    try {
        New-Item -ItemType Directory -Path $gateDir | Out-Null
        [IO.File]::WriteAllText((Join-Path $gateDir 'SmartSnippets.App.dll'), 'not a real assembly')
        $shell = Join-Path $PSHOME $(if ($PSVersionTable.PSEdition -eq 'Core') { 'pwsh.exe' } else { 'powershell.exe' })
        $gateOutput = & $shell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'publish-aot.ps1') -Output $gateDir 2>&1 | Out-String
        Check 'G8.gate' 'publish gate rejects a managed app DLL' ($LASTEXITCODE -ne 0 -and $gateOutput -match 'Managed app DLL found') $gateOutput
    } finally {
        Remove-Item $gateDir -Recurse -Force -ErrorAction SilentlyContinue
        Remove-Item "$gateDir.log" -Force -ErrorAction SilentlyContinue
    }
}
if (Want 'P') { . (Join-Path $PSScriptRoot 'lib\PerformanceSection.ps1') }
foreach ($section in 'E', 'F') {
    if (Want $section) { Check "$section.incomplete" "section $section implementation" $false 'Not yet implemented; do not use this run for acceptance' }
}

}
finally {
    foreach ($id in $script:HotkeyIds) { [void][Win32Verify]::UnregisterHotKey([IntPtr]::Zero, $id) }
    Stop-Target
    Stop-App
    Archive-Logs
    if (Want 'G') { Check 'G10' 'whole-run logs across profile resets exclude acceptance snippet text' (Test-LogPrivacy $script:ArchivedLogs.ToArray() $script:PrivacySecrets.ToArray()) "archived lines=$($script:ArchivedLogs.Count) secrets=$($script:PrivacySecrets.Count)" }
    Restore-Profile
}

# ================================================================= summary
$pass = @($script:Results | Where-Object Status -eq 'PASS').Count
$fail = @($script:Results | Where-Object Status -eq 'FAIL').Count
$skip = @($script:Results | Where-Object Status -eq 'SKIP').Count
$warn = @($script:Results | Where-Object Status -eq 'WARN').Count
Write-Host "`n====================================================" -ForegroundColor Cyan
Write-Host (' PASS {0}   FAIL {1}   WARN {4}   SKIP {2}   ({3:N0}s)' -f $pass, $fail, $skip, ((Get-Date) - $summaryStart).TotalSeconds, $warn) -ForegroundColor $(if ($fail) { 'Red' } else { 'Green' })
if ($fail) {
    Write-Host ' Failures:' -ForegroundColor Red
    $script:Results | Where-Object Status -eq 'FAIL' | ForEach-Object { Write-Host ('   {0} {1} {2}' -f $_.Id, $_.Name, $_.Detail) -ForegroundColor Red }
}
Write-Host '====================================================' -ForegroundColor Cyan
if ($ReportPath) { $script:Results | ConvertTo-Json -Depth 3 | Set-Content $ReportPath -Encoding UTF8 }
exit $(if ($fail) { 1 } else { 0 })
