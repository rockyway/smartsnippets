# Manual recovery only. Close acceptance app/worker processes first; never publish this journal.
param([Parameter(Mandatory=$true)][string]$JournalPath)
$ErrorActionPreference='Stop'
try {
    . (Join-Path $PSScriptRoot 'lib/RegistryRecovery.ps1')
    $journal=Import-Clixml -LiteralPath $JournalPath
    if ([string]::IsNullOrWhiteSpace($journal.RunId)) { throw 'Missing registry recovery run identity' }
    Restore-HarnessRegistryJournal $JournalPath $journal.RunId
    Write-Host 'Allowlisted registry values restored. A taskbar refresh/sign-out may be needed; retain the journal until recovery is confirmed.'
    exit 0
} catch {
    Write-Warning 'Registry recovery failed. Keep the private journal and retry after closing acceptance processes; payloads withheld.'
    exit 1
}
