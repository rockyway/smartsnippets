#Requires -Version 7.2
# Build/inspect only. Never executes the probe, synthesizes input or accesses a profile.
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateSet('win-x64', 'osx-arm64')]
    [string] $RuntimeIdentifier,
    [switch] $ManualWindowsPackages
)
$ErrorActionPreference = 'Stop'
# Compiler failure is evidence to collect, not a PowerShell terminating exception.
$PSNativeCommandUseErrorActionPreference = $false
$onWindows = [System.Runtime.InteropServices.RuntimeInformation]::IsOSPlatform([System.Runtime.InteropServices.OSPlatform]::Windows)
$onMac = [System.Runtime.InteropServices.RuntimeInformation]::IsOSPlatform([System.Runtime.InteropServices.OSPlatform]::OSX)
if (($RuntimeIdentifier -eq 'win-x64' -and !$onWindows) -or ($RuntimeIdentifier -eq 'osx-arm64' -and !$onMac)) {
    throw 'Run the collector locally on the matching host. Cross-host artifact evidence is not accepted.'
}
if ($onWindows -and [System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture -ne [System.Runtime.InteropServices.Architecture]::X64) {
    throw 'The Windows probe requires an x64 host.'
}
if ($onMac -and [System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture -ne [System.Runtime.InteropServices.Architecture]::Arm64) {
    throw 'The macOS probe requires Apple Silicon.'
}
if ($ManualWindowsPackages -and !$onWindows) {
    throw 'The manual-package experiment is scoped to the Windows target.'
}
if ($onMac) {
    # Use the tested Native AOT bundle path, not SDK Extras' CoreCLR launcher.
    & bash (Join-Path $PSScriptRoot 'collect-build.sh')
    exit $LASTEXITCODE
}
$runId = [Guid]::NewGuid().ToString('N')
$private = Join-Path ([System.IO.Path]::GetTempPath()) "SmartSnippets.Uno018-$runId"
New-Item -ItemType Directory -Path $private | Out-Null
if ($onWindows) {
    $acl = New-Object System.Security.AccessControl.DirectorySecurity
    $acl.SetAccessRuleProtection($true, $false)
    $identity = [System.Security.Principal.WindowsIdentity]::GetCurrent().User
    $rule = New-Object System.Security.AccessControl.FileSystemAccessRule($identity, 'FullControl', 'ContainerInherit,ObjectInherit', 'None', 'Allow')
    $acl.AddAccessRule($rule)
    Set-Acl -LiteralPath $private -AclObject $acl
} else {
    & chmod 700 $private
    if ($LASTEXITCODE -ne 0) { throw 'Could not restrict evidence directory permissions.' }
}
$project = Join-Path $PSScriptRoot 'SmartSnippets.UnoProbe.csproj'
$output = Join-Path $PSScriptRoot "bin/probe/$runId"
$tfm = if ($onWindows) { 'net10.0-windows10.0.26100' } else { 'net10.0-desktop' }
$properties = Join-Path $private 'properties.json'
$log = Join-Path $private 'publish.log'
# Override vendor IL2121 suppression with only a non-IL compiler warning ID.
$common = @('-p:Configuration=Release', "-p:RuntimeIdentifier=$RuntimeIdentifier", '-p:NoWarn=1701',
    '-p:SuppressTrimAnalysisWarnings=false', '-p:TrimmerSingleWarn=false')
if ($ManualWindowsPackages) { $common += '-p:ProbeManualWindowsPackages=true' }
& dotnet restore $project -r $RuntimeIdentifier -p:Configuration=Release @common 2>&1 | Set-Content -LiteralPath (Join-Path $private 'restore.log') -Encoding utf8
if ($LASTEXITCODE -ne 0) { throw 'Restore failed; property evidence cannot be collected.' }
$names = 'TargetFramework,RuntimeIdentifier,PublishAot,PublishTrimmed,PublishSingleFile,SuppressTrimAnalysisWarnings,TrimmerSingleWarn,NoWarn,EnableTrimAnalyzer,EnableAotAnalyzer,TreatWarningsAsErrors,WindowsPackageType,NETCoreSdkVersion,MSBuildVersion,UnoVersion,WinAppSdkVersion,TargetPlatformMinVersion,ProbeManualWindowsPackages,DisableImplicitUnoPackages'
& dotnet msbuild $project "-p:TargetFramework=$tfm" -p:_IsPublishing=true @common "-getProperty:$names" 2>&1 | Set-Content -LiteralPath $properties -Encoding utf8
if ($LASTEXITCODE -ne 0) { throw 'MSBuild property collection failed; evidence is private and not a PASS.' }
# An ILC invocation may leave an object even when trim diagnostics fail the build.
# Never accept a later incremental link as fresh zero-warning evidence.
& dotnet clean $project -f $tfm -r $RuntimeIdentifier -c Release @common 2>&1 | Set-Content -LiteralPath (Join-Path $private 'clean.log') -Encoding utf8
if ($LASTEXITCODE -ne 0) { throw 'Clean failed; a fresh compiler run cannot be established.' }
& dotnet publish $project -f $tfm -r $RuntimeIdentifier -c Release --no-restore -o $output @common 2>&1 | Set-Content -LiteralPath $log -Encoding utf8
$publishExit = $LASTEXITCODE
Add-Content -LiteralPath $log -Value "probe_publish_exit=$publishExit" -Encoding utf8
$executable = if ($onWindows) {
    Join-Path $output 'SmartSnippets.UnoProbe.exe'
} else {
    # Actual Uno bundle layout must be checked on the Mac. Missing layout fails closed.
    Join-Path $output 'SmartSnippets.UnoProbe.app/Contents/MacOS/SmartSnippets.UnoProbe'
}
& python (Join-Path $PSScriptRoot 'verify_evidence.py') --properties $properties --log $log --artifact-dir $output --executable $executable --rid $RuntimeIdentifier
$checkExit = $LASTEXITCODE
Write-Host "Private evidence retained under temp run ID $runId. No native artifact was launched."
if ($publishExit -ne 0 -or $checkExit -ne 0) { exit 1 }
exit 0
