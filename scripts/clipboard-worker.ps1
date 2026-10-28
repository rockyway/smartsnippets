# Internal clipboard-only subprocess. The supervisor bounds both snapshot and restore, including
# delayed-rendering/owner callbacks inside native clipboard APIs. Never print clipboard/errors.
param(
    [Parameter(Mandatory=$true)][ValidateSet('Save','Restore')][string]$Mode,
    [Parameter(Mandatory=$true)][string]$BackupPath,
    [string]$LeasePath = ''
)
$ErrorActionPreference='Stop'
try {
    if (-not $LeasePath) { $LeasePath=Join-Path $env:LOCALAPPDATA 'SmartSnippets.verify.lock.worker' }
    # An orphaned clipboard helper must also exclude a new acceptance run until it exits.
    $lease=[IO.File]::Open($LeasePath,[IO.FileMode]::OpenOrCreate,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None)
    . (Join-Path $PSScriptRoot 'lib/Clipboard.ps1')
    if ($Mode -eq 'Save') { Export-ClipboardTextBackup (Save-ClipboardText) $BackupPath }
    else { Restore-ClipboardText (Import-ClipboardTextBackup $BackupPath) }
    exit 0
} catch { exit 1 }
