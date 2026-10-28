# Internal clipboard-only subprocess. The supervisor bounds both snapshot and restore, including
# delayed-rendering/owner callbacks inside native clipboard APIs. Never print clipboard/errors.
param(
    [Parameter(Mandatory=$true)][ValidateSet('Save','Restore')][string]$Mode,
    [Parameter(Mandatory=$true)][string]$BackupPath
)
$ErrorActionPreference='Stop'
try {
    . (Join-Path $PSScriptRoot 'lib/Clipboard.ps1')
    if ($Mode -eq 'Save') { Export-ClipboardTextBackup (Save-ClipboardText) $BackupPath }
    else { Restore-ClipboardText (Import-ClipboardTextBackup $BackupPath) }
    exit 0
} catch { exit 1 }
