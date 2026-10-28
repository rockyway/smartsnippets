# Shared Native AOT publish gate. Compatible with Windows PowerShell 5.1 and PowerShell 7.
[CmdletBinding()]
param(
    [string]$Output = 'publish',
    [string]$Version,
    [Nullable[int]]$BuildNumber
)
$ErrorActionPreference = 'Stop'
function Fail([string]$Reason) {
    if ($env:GITHUB_ACTIONS) { Write-Host "::error::$Reason" } else { Write-Host "ERROR: $Reason" }
    exit 1
}
try {
    $arguments = @('publish', 'src/SmartSnippets.App', '-c', 'Release', '-r', 'win-x64', '-o', $Output)
    if ($Version) {
        if ($Version -notmatch '^(\d+)\.(\d+)\.(\d+)(?:-[0-9A-Za-z.-]+)?$') { Fail 'Invalid version' }
        if ($null -eq $BuildNumber -or $BuildNumber -lt 0 -or $BuildNumber -gt 65535) { Fail 'BuildNumber must be between 0 and 65535' }
        $arguments += "-p:Version=$Version", "-p:FileVersion=$($Matches[1]).$($Matches[2]).$($Matches[3]).$BuildNumber"
    } elseif ($null -ne $BuildNumber) { Fail 'BuildNumber requires Version' }
    $log = "$Output.log"
    $parent = Split-Path -Parent $log
    if ($parent) { New-Item -ItemType Directory -Force -Path $parent | Out-Null }
    # Redirect both streams before Tee-Object so native stderr is not treated as a terminating error in PS 5.1.
    & dotnet @arguments 2>&1 | Tee-Object -FilePath $log
    $code = $LASTEXITCODE
    if ($code -ne 0) { Fail "dotnet publish failed ($code)" }
    if (Select-String -Path $log -Pattern 'warning IL[23][0-9]{3}' -Quiet) { Fail 'Trim/AOT warnings found' }
    if (Test-Path (Join-Path $Output 'SmartSnippets.App.dll')) { Fail 'Managed app DLL found; output is not Native AOT' }
    foreach ($file in @('SmartSnippets.App.exe', 'e_sqlite3.dll')) {
        if (-not (Test-Path (Join-Path $Output $file) -PathType Leaf)) { Fail "Missing $file" }
    }
} catch { Fail $_.Exception.Message }
exit 0
