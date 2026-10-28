# Syntax only: never execute the scripts being parsed.
$ErrorActionPreference='Stop'
$files=@(Get-ChildItem -LiteralPath $PSScriptRoot -Recurse -Filter *.ps1)
$total=0
foreach ($file in $files) {
    $tokens=$null; $errors=$null
    $null=[Management.Automation.Language.Parser]::ParseFile($file.FullName,[ref]$tokens,[ref]$errors)
    $total+=@($errors).Count
    foreach ($error in $errors) { Write-Host ($file.Name+':'+$error.Extent.StartLineNumber+': '+$error.Message) }
}
Write-Host "Parsed $($files.Count) scripts; errors=$total; PowerShell=$($PSVersionTable.PSVersion)"
if ($total) { exit 1 }
