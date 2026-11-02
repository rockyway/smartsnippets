# Export a clean source zip (tracked files only: no bin/obj, publish output, .claude or other untracked files).
# The docs/ folder is left out unless -IncludeDocs is given.
# Binary files (images, archives, executables) are left out, except under src/ and assets/ where the build needs them.
#   pwsh -NoProfile -File scripts/export-source.ps1                 # working-tree content, includes uncommitted edits
#   pwsh -NoProfile -File scripts/export-source.ps1 -FromHead       # committed HEAD only
#   pwsh -NoProfile -File scripts/export-source.ps1 -IncludeDocs    # keep docs/
#   pwsh -NoProfile -File scripts/export-source.ps1 -p <password>   # AES-256 encrypted zip (needs 7-Zip)
#   pwsh -NoProfile -File scripts/export-source.ps1 -p <password> -HideNames  # .7z; file names are encrypted too
#   pwsh -NoProfile -File scripts/export-source.ps1 -OutFile D:\share\ss.zip
param([string]$OutFile, [switch]$FromHead, [switch]$IncludeDocs, [Alias('p')][string]$Password, [switch]$HideNames)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression, System.IO.Compression.FileSystem

$repository = (git -C $PSScriptRoot rev-parse --show-toplevel).Trim()
$sha = (git -C $repository rev-parse --short HEAD).Trim()
if ($PSBoundParameters.ContainsKey('Password') -and [string]::IsNullOrEmpty($Password)) {
    throw '-p was given an empty password (an unset variable?); refusing to write an unprotected zip.'
}
# The zip format cannot encrypt file names; only 7z's header encryption can.
if ($HideNames -and -not $Password) { throw '-HideNames needs -p <password>.' }
if (-not $OutFile) {
    $suffix = if ($Password) { '-protected' } else { '' }
    $OutFile = Join-Path (Split-Path $repository -Parent) ("smartsnippets-src-{0}-{1:yyyyMMdd-HHmmss}{2}.{3}" -f $sha, (Get-Date), $suffix, $(if ($HideNames) { '7z' } else { 'zip' }))
}
$OutFile = [IO.Path]::GetFullPath($OutFile)
if ($OutFile.StartsWith($repository.Replace('/', '\') + '\', [StringComparison]::OrdinalIgnoreCase)) {
    throw "Write the zip outside the repository: $OutFile"
}
if ($Password) {
    # .NET's ZipArchive cannot encrypt; 7-Zip writes standard AES-256 zip entries.
    $sevenZip = @((Get-Command 7z -ErrorAction SilentlyContinue).Source,
        "$env:ProgramFiles\7-Zip\7z.exe", "${env:ProgramFiles(x86)}\7-Zip\7z.exe") |
        Where-Object { $_ -and (Test-Path -LiteralPath $_) } | Select-Object -First 1
    if (-not $sevenZip) { throw '-p needs 7-Zip (7z.exe); install it or omit the password.' }
}

$binary = '\.(png|jpe?g|gif|bmp|ico|webp|zip|7z|bundle|dll|exe|pdb|nupkg|msi|db|sqlite)$'
$keepBinaryUnder = '^(src|assets)/'
$files = git -C $repository ls-files | Where-Object { $_ -notmatch $binary -or $_ -match $keepBinaryUnder }
if (-not $IncludeDocs) { $files = $files | Where-Object { $_ -notmatch '^docs/' } }
# Tracked but deleted locally: nothing to read from the working tree.
if (-not $FromHead) { $files = $files | Where-Object { Test-Path -LiteralPath (Join-Path $repository $_) -PathType Leaf } }

function Read-SourceBytes([string]$file) {
    if (-not $FromHead) { return , [IO.File]::ReadAllBytes((Join-Path $repository $file)) }
    # git show writes raw bytes; read them through a temp file to avoid PowerShell re-encoding.
    $temp = [IO.Path]::GetTempFileName()
    try {
        cmd /c "git -C `"$repository`" show `"HEAD:$file`" > `"$temp`""
        if ($LASTEXITCODE -ne 0) { throw "git show failed for $file" }
        return , [IO.File]::ReadAllBytes($temp)
    }
    finally { Remove-Item -LiteralPath $temp -ErrorAction SilentlyContinue }
}

if (Test-Path -LiteralPath $OutFile) { Remove-Item -LiteralPath $OutFile }
$count = 0
if ($Password) {
    # Stage the selected files, then let 7-Zip build the encrypted archive.
    $stage = Join-Path ([IO.Path]::GetTempPath()) ("ss-export-" + [Guid]::NewGuid().ToString('N'))
    try {
        foreach ($file in $files) {
            $path = Join-Path $stage "smartsnippets/$file"
            New-Item -ItemType Directory -Force -Path (Split-Path $path -Parent) | Out-Null
            [IO.File]::WriteAllBytes($path, (Read-SourceBytes $file))
            $count++
        }
        Push-Location $stage
        try {
            $format = if ($HideNames) { '-t7z', '-mhe=on' } else { '-tzip', '-mem=AES256' }
            & $sevenZip a @format "-p$Password" -bso0 -bsp0 $OutFile smartsnippets | Out-Null
            if ($LASTEXITCODE -ne 0) { throw "7-Zip failed with exit code $LASTEXITCODE" }
        }
        finally { Pop-Location }
        # Verify before reporting success: every file entry is encrypted, and the archive opens only with the password.
        $listing = & $sevenZip l -slt "-p$Password" $OutFile
        $entries = @($listing | Select-String '^Attributes = (?!.*D)').Count  # files only; folders carry D (zip and 7z alike)
        $encrypted = @($listing | Select-String '^Encrypted = \+').Count
        & $sevenZip t "-p$Password" -bso0 -bse0 $OutFile | Out-Null; $withPassword = $LASTEXITCODE
        & $sevenZip t "-p$([Guid]::NewGuid())" -bso0 -bse0 $OutFile 2>&1 | Out-Null; $withoutPassword = $LASTEXITCODE  # a wrong password; a bare -p would prompt
        $namesHidden = $true
        if ($HideNames) {
            # With encrypted headers even listing must fail without the password.
            & $sevenZip l "-p$([Guid]::NewGuid())" -bso0 -bse0 $OutFile 2>&1 | Out-Null; $namesHidden = $LASTEXITCODE -ne 0
        }
        if ($entries -ne $count -or $encrypted -ne $count -or $withPassword -ne 0 -or $withoutPassword -eq 0 -or -not $namesHidden) {
            Remove-Item -LiteralPath $OutFile -ErrorAction SilentlyContinue
            throw "Encryption check failed (files=$entries encrypted=$encrypted test=$withPassword no-password=$withoutPassword names-hidden=$namesHidden); archive deleted."
        }
    }
    finally { Remove-Item -LiteralPath $stage -Recurse -Force -ErrorAction SilentlyContinue }
}
else {
    $zip = [IO.Compression.ZipFile]::Open($OutFile, [IO.Compression.ZipArchiveMode]::Create)
    try {
        foreach ($file in $files) {
            $bytes = Read-SourceBytes $file
            $target = $zip.CreateEntry("smartsnippets/$file", [IO.Compression.CompressionLevel]::Optimal).Open()
            try { $target.Write($bytes, 0, $bytes.Length) } finally { $target.Dispose() }
            $count++
        }
    }
    finally { $zip.Dispose() }
}

$size = (Get-Item -LiteralPath $OutFile).Length / 1MB
"Exported $count files from $sha ($(if ($FromHead) { 'HEAD' } else { 'working tree' })) to $OutFile ({0:N1} MB{1})" -f $size, $(if ($HideNames) { ', 7z AES-256: contents and file names need the password' } elseif ($Password) { ', AES-256: every file needs the password; names stay visible, as in any zip' } else { ', NOT password-protected' })
