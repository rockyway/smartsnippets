# Pure coordinate/ownership rejection checks. Does not launch or drive windows.
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'lib/PostedInput.ps1')
if ([SmartSnippetsPostedInput]::PackClientPoint(20,30) -ne ((30 -shl 16) -bor 20)) { throw 'Incorrect LPARAM packing' }
if ([SmartSnippetsPostedInput]::PackClientPoint(32767,32767) -ne 0x7fff7fff) { throw 'Incorrect maximum LPARAM packing' }
foreach ($point in @(@(-1,0),@(0,-1),@(32768,0),@(0,32768))) {
    $failed = $false
    try { $null = [SmartSnippetsPostedInput]::PackClientPoint($point[0], $point[1]) } catch { $failed = $true }
    if (-not $failed) { throw 'Out-of-range point was accepted' }
}
$failed = $false
try { $null = [SmartSnippetsPostedInput]::FindInputWindow([IntPtr]::Zero, $PID, 0, 0) } catch { $failed = $true }
if (-not $failed) { throw 'Unowned HWND was accepted' }
Write-Host 'Pure posted-input checks passed; actual WinUI delivery is not tested.'
