#Requires -Version 7.2
# Build-only by default. Launch path also requires the independently reviewed Python source gate.
[CmdletBinding(DefaultParameterSetName = 'Build')]
param(
    [Parameter(ParameterSetName = 'Build')][switch]$BuildOnly,
    [Parameter(Mandatory, ParameterSetName = 'Observe')][string]$Executable,
    [Parameter(Mandatory, ParameterSetName = 'Observe')][ValidatePattern('^[0-9a-fA-F]{64}$')][string]$Sha256,
    [Parameter(Mandatory, ParameterSetName = 'Observe')][switch]$AuthorizedScreenSlot,
    [Parameter(Mandatory, ParameterSetName = 'Observe')][int[]]$WorkArea
)
$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $false
if (!$IsWindows -or [Runtime.InteropServices.RuntimeInformation]::OSArchitecture -ne 'X64') { throw 'windows_x64_required' }
if ($PSCmdlet.ParameterSetName -eq 'Observe') {
    if (!$AuthorizedScreenSlot -or $WorkArea.Count -ne 4) { throw 'screen_slot_and_work_rectangle_required' }
    & python -I -B (Join-Path $PSScriptRoot 'run_uno_candidate_acceptance.py') --executable $Executable --sha256 $Sha256 --authorized-screen-slot --work-area @WorkArea
    exit $LASTEXITCODE
}
$fixture = Join-Path $PSScriptRoot 'uno_candidate_fixture'
$project = 'SmartSnippets.Uno.AcceptanceFixture.csproj'
$token = [Guid]::NewGuid().ToString('N')
$output = Join-Path $fixture "bin/publish-acceptance/$token"
$previous = Get-Location
try {
    # Reject aliases before creating any output, not merely at the post-build gate.
    $ancestor = [IO.DirectoryInfo]::new($fixture)
    while ($null -ne $ancestor) {
        if ($ancestor.Exists -and ($ancestor.Attributes -band [IO.FileAttributes]::ReparsePoint)) { throw 'build_alias' }
        $ancestor = $ancestor.Parent
    }
    foreach ($prefix in @('bin', 'bin/publish-acceptance')) {
        $path = Join-Path $fixture $prefix
        if ((Test-Path -LiteralPath $path) -and ((Get-Item -LiteralPath $path -Force).Attributes -band [IO.FileAttributes]::ReparsePoint)) { throw 'build_alias' }
    }
    # Output (including path-bearing compiler/property evidence) is owner-private before any writes.
    New-Item -ItemType Directory -Path $output -ErrorAction Stop | Out-Null
    $acl = [Security.AccessControl.DirectorySecurity]::new()
    $acl.SetAccessRuleProtection($true, $false)
    $sid = [Security.Principal.WindowsIdentity]::GetCurrent().User
    $acl.SetOwner($sid)
    $acl.AddAccessRule([Security.AccessControl.FileSystemAccessRule]::new($sid, 'FullControl', 'ContainerInherit,ObjectInherit', 'None', 'Allow'))
    Set-Acl -LiteralPath $output -AclObject $acl
    Set-Location -LiteralPath $fixture
    if ((& dotnet --version | Out-String).Trim() -ne '10.0.403' -or $LASTEXITCODE) { throw 'pinned_sdk_required' }
    $common = @('-p:Configuration=Release', '-p:RuntimeIdentifier=win-x64', '-p:ContinuousIntegrationBuild=true',
        '-p:NoWarn=', '-p:PublishAot=true', '-p:PublishTrimmed=true', '-p:PublishSingleFile=false',
        '-p:SuppressTrimAnalysisWarnings=false', '-p:TrimmerSingleWarn=false', '-p:EnableTrimAnalyzer=true',
        '-p:EnableAotAnalyzer=true', '-p:TreatWarningsAsErrors=true')
    $log = Join-Path $output 'fixture-build.log'
    & dotnet restore $project -r win-x64 @common 2>&1 | Set-Content $log -Encoding utf8
    if ($LASTEXITCODE) { throw 'restore_failed' }
    $names = 'PublishAot,PublishTrimmed,PublishSingleFile,IsAotCompatible,EnableTrimAnalyzer,EnableAotAnalyzer,TreatWarningsAsErrors,SuppressTrimAnalysisWarnings,TrimmerSingleWarn,NoWarn,DisableImplicitUnoPackages,ImplicitUsings,RuntimeIdentifier,RuntimeIdentifiers,TargetFramework,NETCoreSdkVersion,MSBuildVersion,WindowsPackageType,WindowsAppSDKSelfContained,AssemblyName,SupportedOSPlatformVersion,TargetPlatformMinVersion,RestorePackagesWithLockFile,RestoreLockedMode'
    $evaluation = @('-p:Configuration=Release', '-p:ContinuousIntegrationBuild=true', '-p:_IsPublishing=true')
    & dotnet msbuild $project @evaluation "-getProperty:$names" 2>&1 | Set-Content (Join-Path $output 'fixture-properties.json') -Encoding utf8
    if ($LASTEXITCODE) { throw 'properties_failed' }
    $references = @{}
    $referenceNames = 'IsAotCompatible,EnableTrimAnalyzer,EnableAotAnalyzer,TreatWarningsAsErrors,SuppressTrimAnalysisWarnings,TrimmerSingleWarn,NoWarn,RestorePackagesWithLockFile,RestoreLockedMode'
    foreach ($name in @('Core', 'Presentation')) {
        $reference = "../../src/SmartSnippets.$name/SmartSnippets.$name.csproj"
        $authored = & dotnet msbuild $reference @evaluation "-getProperty:$referenceNames" 2>&1 | Out-String
        if ($LASTEXITCODE) { throw 'reference_properties_failed' }
        $context = & dotnet msbuild $reference @common -p:_IsPublishing=true "-getProperty:$referenceNames" 2>&1 | Out-String
        if ($LASTEXITCODE) { throw 'reference_context_failed' }
        $references[$name] = @{ Authored = ($authored | ConvertFrom-Json).Properties; BuildContext = ($context | ConvertFrom-Json).Properties }
    }
    $references | ConvertTo-Json -Depth 6 | Set-Content (Join-Path $output 'fixture-references.json') -Encoding utf8
    & dotnet clean $project -c Release -r win-x64 @common 2>&1 | Add-Content $log -Encoding utf8
    if ($LASTEXITCODE) { throw 'clean_failed' }
    $started = [DateTimeOffset]::UtcNow.ToUnixTimeMilliseconds()
    & dotnet publish $project -c Release -r win-x64 --no-restore -o $output -m:4 @common 2>&1 | Add-Content $log -Encoding utf8
    $code = $LASTEXITCODE
    Add-Content $log "candidate_publish_exit=$code" -Encoding utf8
    if ($code) { throw 'publish_failed' }
    @{ startedUtcMs = $started } | ConvertTo-Json | Set-Content (Join-Path $output 'fixture-build.json') -Encoding utf8
    & python -I -B (Join-Path $PSScriptRoot 'uno_candidate_fixture_gate.py') --capture-source (Join-Path $output 'fixture-source.sha256')
    if ($LASTEXITCODE) { throw 'source_receipt_failed' }
    $entry = Join-Path $output 'SmartSnippets.UnoAcceptanceFixture.exe'
    $hash = (Get-FileHash -LiteralPath $entry -Algorithm SHA256).Hash.ToLowerInvariant()
    & python -I -B (Join-Path $PSScriptRoot 'uno_candidate_fixture_gate.py') $entry $hash
    if ($LASTEXITCODE) { throw 'artifact_gate_failed' }
    Write-Output "fixture_build_token=$token"
    Write-Output "fixture_sha256=$hash"
} catch {
    Write-Error "Fixture build blocked; private build token=$token. No native executable launched." -ErrorAction Continue
    exit 1
} finally { Set-Location -LiteralPath $previous.Path }
exit 0
