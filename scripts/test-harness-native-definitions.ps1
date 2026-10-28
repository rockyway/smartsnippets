# Compile native helper definitions only. Never dot-source Uia.ps1 or invoke a Win32 API.
$ErrorActionPreference = 'Stop'
foreach ($path in @('lib/Uia.ps1','lib/Clipboard.ps1')) {
    $source = [IO.File]::ReadAllText((Join-Path $PSScriptRoot $path))
    $match = [regex]::Match($source, "(?s)Add-Type -TypeDefinition @'\r?\n(.*?)\r?\n'@")
    if (-not $match.Success) { throw "Missing native definition: $path" }
    Add-Type -TypeDefinition $match.Groups[1].Value -ErrorAction Stop
    Write-Host "PASS native helper compiled (not invoked): $path"
}
