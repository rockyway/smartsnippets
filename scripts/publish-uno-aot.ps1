#Requires -Version 7.2
# Build/inspect only: no app launch, UI, clipboard, input, registry or profile access.
[CmdletBinding()]
param([string]$Version, [Nullable[int]]$BuildNumber, [ValidateRange(1, 64)][int]$MaxCpuCount = 4, [switch]$Measure)
$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $false
$repository = Split-Path -Parent $PSScriptRoot
$app = Join-Path $repository 'src/SmartSnippets.Uno'
$token = [Guid]::NewGuid().ToString('N')
$private = Join-Path ([IO.Path]::GetTempPath()) "ss-uno019-build-$token"
$output = Join-Path $app "bin/publish-native/$token"
$destination = Join-Path $repository $(if ($Measure) { "publish-measure" } else { "publish" })
$previous = Get-Location
$stage = 'host'
try {
    if (!$IsWindows -or [Runtime.InteropServices.RuntimeInformation]::OSArchitecture -ne 'X64') {
        throw 'matching_windows_x64_host_required'
    }
    # No logs are written until the newly created, uniquely named directory is protected.
    New-Item -ItemType Directory -Path $private -ErrorAction Stop | Out-Null
    $acl = [Security.AccessControl.DirectorySecurity]::new()
    $acl.SetAccessRuleProtection($true, $false)
    $sid = [Security.Principal.WindowsIdentity]::GetCurrent().User
    $acl.SetOwner($sid)
    $acl.AddAccessRule([Security.AccessControl.FileSystemAccessRule]::new(
        $sid, 'FullControl', 'ContainerInherit,ObjectInherit', 'None', 'Allow'))
    Set-Acl -LiteralPath $private -AclObject $acl
    if (Test-Path -LiteralPath $output) { throw 'output_must_be_fresh' }
    Set-Location -LiteralPath $app
    $sdk = (& dotnet --version 2>&1 | Out-String).Trim()
    if ($LASTEXITCODE -ne 0 -or $sdk -ne '10.0.403') { throw 'pinned_sdk_required' }
    $log = Join-Path $private 'build.log'
    $properties = Join-Path $private 'properties.json'
    $referenceProperties = Join-Path $private 'reference-properties.json'
    $common = @('-p:Configuration=Release', '-p:RuntimeIdentifier=win-x64',
        '-p:ContinuousIntegrationBuild=true', '-p:NoWarn=', '-p:PublishAot=true',
        '-p:PublishTrimmed=true', '-p:PublishSingleFile=false',
        '-p:SuppressTrimAnalysisWarnings=false', '-p:TrimmerSingleWarn=false',
        '-p:EnableTrimAnalyzer=true', '-p:EnableAotAnalyzer=true', '-p:TreatWarningsAsErrors=true')
    if ($Measure) { $common += '-p:SmartSnippetsMeasure=true' }
    if ($Version) {
        if ($Version -notmatch '^(\d+)\.(\d+)\.(\d+)(?:-[0-9A-Za-z.-]+)?$') { throw 'invalid_version' }
        if ($null -eq $BuildNumber -or $BuildNumber -lt 0 -or $BuildNumber -gt 65535) { throw 'invalid_build_number' }
        $common += "-p:Version=$Version", "-p:FileVersion=$($Matches[1]).$($Matches[2]).$($Matches[3]).$BuildNumber"
    } elseif ($null -ne $BuildNumber) { throw 'build_number_requires_version' }
    # Lock settings belong to the head csproj, never global CLI properties propagated to references.
    $stage = 'locked_restore'
    & dotnet restore SmartSnippets.Uno.csproj -r win-x64 @common 2>&1 | Set-Content -LiteralPath $log -Encoding utf8
    if ($LASTEXITCODE -ne 0) { throw 'locked_restore_failed' }
    $names = 'PublishAot,PublishTrimmed,PublishSingleFile,IsAotCompatible,EnableTrimAnalyzer,EnableAotAnalyzer,TreatWarningsAsErrors,SuppressTrimAnalysisWarnings,TrimmerSingleWarn,NoWarn,DisableImplicitUnoPackages,ImplicitUsings,RuntimeIdentifier,RuntimeIdentifiers,TargetFramework,NETCoreSdkVersion,MSBuildVersion,WindowsPackageType,WindowsAppSDKSelfContained,AssemblyName,SupportedOSPlatformVersion,TargetPlatformMinVersion,RestorePackagesWithLockFile,RestoreLockedMode'
    $stage = 'effective_properties'
    # Inspect authored settings without global AOT/trim/NoWarn/RID overrides masking defects.
    $evaluation = @('-p:Configuration=Release', '-p:ContinuousIntegrationBuild=true', '-p:_IsPublishing=true')
    if ($Measure) { $evaluation += '-p:SmartSnippetsMeasure=true' }
    & dotnet msbuild SmartSnippets.Uno.csproj @evaluation "-getProperty:$names" 2>&1 | Set-Content -LiteralPath $properties -Encoding utf8
    if ($LASTEXITCODE -ne 0) { throw 'effective_properties_failed' }
    $references = @{}
    $referenceNames = 'IsAotCompatible,EnableTrimAnalyzer,EnableAotAnalyzer,TreatWarningsAsErrors,SuppressTrimAnalysisWarnings,TrimmerSingleWarn,NoWarn,RestorePackagesWithLockFile,RestoreLockedMode'
    foreach ($name in @('Core', 'Presentation')) {
        $project = "../SmartSnippets.$name/SmartSnippets.$name.csproj"
        $authoredFile = Join-Path $private "$name-authored.json"
        $contextFile = Join-Path $private "$name-context.json"
        & dotnet msbuild $project @evaluation "-getProperty:$referenceNames" 2>&1 | Set-Content -LiteralPath $authoredFile -Encoding utf8
        if ($LASTEXITCODE -ne 0) { throw 'reference_authored_properties_failed' }
        & dotnet msbuild $project @common -p:_IsPublishing=true "-getProperty:$referenceNames" 2>&1 | Set-Content -LiteralPath $contextFile -Encoding utf8
        if ($LASTEXITCODE -ne 0) { throw 'reference_build_context_failed' }
        $references[$name] = @{ Authored = (Get-Content -LiteralPath $authoredFile -Raw | ConvertFrom-Json).Properties
            BuildContext = (Get-Content -LiteralPath $contextFile -Raw | ConvertFrom-Json).Properties }
    }
    $references | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $referenceProperties -Encoding utf8
    # Clean forces a fresh native compiler invocation; no incremental link is accepted as evidence.
    $stage = 'fresh_clean'
    & dotnet clean SmartSnippets.Uno.csproj -c Release -r win-x64 @common 2>&1 | Add-Content -LiteralPath $log -Encoding utf8
    if ($LASTEXITCODE -ne 0) { throw 'clean_failed' }
    $stage = 'native_publish'
    $started = [DateTimeOffset]::UtcNow.ToUnixTimeMilliseconds()
    & dotnet publish SmartSnippets.Uno.csproj -c Release -r win-x64 --no-restore -o $output "-m:$MaxCpuCount" @common 2>&1 | Add-Content -LiteralPath $log -Encoding utf8
    $publishExit = $LASTEXITCODE
    Add-Content -LiteralPath $log -Value "publish_exit=$publishExit" -Encoding utf8
    if ($publishExit -ne 0) { throw 'native_publish_failed' }
    $stage = 'artifact_check'
    $receipt = & python (Join-Path $PSScriptRoot 'aot_gate.py') --properties $properties --log $log --artifact-dir $output --started-utc-ms $started --reference-properties $referenceProperties
    if ($LASTEXITCODE -ne 0) { throw 'artifact_gate_rejected' }
    $receipt | Set-Content -LiteralPath (Join-Path $output 'build-checks.json') -Encoding utf8
    if (!(Test-Path -LiteralPath (Join-Path $output "Assets/TitleIcon.png") -PathType Leaf)) { throw 'title_icon_missing' }
    if ($Measure) {
        (Get-FileHash -LiteralPath (Join-Path $output 'SmartSnippets.exe') -Algorithm SHA256).Hash |
            Set-Content -LiteralPath (Join-Path $output 'measurement-build.txt')
    }
    if (Test-Path -LiteralPath $destination) { Remove-Item -LiteralPath $destination -Recurse -Force }
    Copy-Item -LiteralPath $output -Destination $destination -Recurse
    Write-Output $receipt
    Write-Output "build_token=$token"
    Write-Output "Published $(Split-Path -Leaf $destination)/SmartSnippets.exe"
    # Log paths and compiler diagnostics can contain user roots; do not print/upload them.
} catch {
    Write-Error "Official build rejected stage=$stage; private evidence token=$token. No native artifact launched." -ErrorAction Continue
    exit 1
} finally {
    Set-Location -LiteralPath $previous.Path
}
exit 0
