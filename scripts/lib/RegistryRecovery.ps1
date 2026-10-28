# Registry recovery is write-ahead and value-specific. Importing this file does not access HKCU.
function Assert-HarnessRegistryAddress([string]$Key, [string]$Name) {
    $run='Software\Microsoft\Windows\CurrentVersion\Run'
    $theme='Software\Microsoft\Windows\CurrentVersion\Themes\Personalize'
    if (-not (($Key -ieq $run -and $Name -ieq 'SmartSnippets') -or ($Key -ieq $theme -and $Name -ieq 'SystemUsesLightTheme'))) { throw 'Registry recovery address is not allowlisted' }
}
function Read-HarnessRegistryValue([string]$Key, [string]$Name) {
    Assert-HarnessRegistryAddress $Key $Name
    $handle=[Microsoft.Win32.Registry]::CurrentUser.OpenSubKey($Key)
    try {
        $exists=$null -ne $handle -and $Name -in @($handle.GetValueNames())
        $kind=$null; $value=$null
        if ($exists) {
            $kind=$handle.GetValueKind($Name).ToString()
            $value=$handle.GetValue($Name,$null,[Microsoft.Win32.RegistryValueOptions]::DoNotExpandEnvironmentNames)
        }
        [pscustomobject]@{ Key=$Key; Name=$Name; Exists=$exists; Kind=$kind; Value=$value }
    } finally { if ($handle) { $handle.Dispose() } }
}
function Restore-HarnessRegistryValue($Original) {
    Assert-HarnessRegistryAddress $Original.Key $Original.Name
    if ($Original.Exists -isnot [bool]) { throw 'Invalid registry existence marker' }
    $handle=$null
    try {
        if ($Original.Exists) {
            $kind=[Microsoft.Win32.RegistryValueKind]$Original.Kind
            $value=switch ($Original.Kind) {
                'DWord' { [int]$Original.Value }
                'QWord' { [long]$Original.Value }
                'Binary' { ,([byte[]]$Original.Value) }
                'None' { ,([byte[]]$Original.Value) }
                'MultiString' { ,([string[]]$Original.Value) }
                'String' { [string]$Original.Value }
                'ExpandString' { [string]$Original.Value }
                default { throw 'Unsupported registry value type; recovery retained' }
            }
            $handle=[Microsoft.Win32.Registry]::CurrentUser.CreateSubKey($Original.Key)
            $handle.SetValue($Original.Name,$value,$kind)
        } else {
            $handle=[Microsoft.Win32.Registry]::CurrentUser.OpenSubKey($Original.Key,$true)
            if ($handle) { $handle.DeleteValue($Original.Name,$false) }
        }
    } finally { if ($handle) { $handle.Dispose() } }
}
function Save-HarnessRegistryOriginal([string]$Path, [string]$RunId, [string]$Key, [string]$Name, [scriptblock]$Reader=$null) {
    Assert-HarnessRegistryAddress $Key $Name
    $entries=@()
    if (Test-Path -LiteralPath $Path) {
        $existing=Import-Clixml -LiteralPath $Path
        if ($existing.RunId -cne $RunId) { throw 'Registry journal belongs to another run' }
        $entries=@($existing.Entries)
        foreach ($entry in $entries) { if ($entry.Key -ieq $Key -and $entry.Name -ieq $Name) { return $entry } }
    }
    $original=if ($Reader) { & $Reader $Key $Name } else { Read-HarnessRegistryValue $Key $Name }
    if ($original.Key -ine $Key -or $original.Name -ine $Name -or $original.Exists -isnot [bool]) { throw 'Invalid registry snapshot; mutation refused' }
    if ($original.Exists -and $original.Kind -notin @('String','ExpandString','DWord','QWord','Binary','MultiString','None')) { throw 'Registry type cannot be recovered; mutation refused' }
    # Commit the original before any UIA toggle/SetValue. Repeated saves never replace it.
    $temp=$Path+'.tmp'
    [pscustomobject]@{ RunId=$RunId; Entries=@($entries)+@($original) } | Export-Clixml -LiteralPath $temp
    Move-Item -LiteralPath $temp -Destination $Path -Force
    return $original
}
function Restore-HarnessRegistryJournal([string]$Path, [string]$RunId, [scriptblock]$Writer=$null) {
    $journal=Import-Clixml -LiteralPath $Path
    if ($journal.RunId -cne $RunId) { throw 'Registry journal belongs to another run' }
    # Attempt every value even if one restore fails. Never print private registry contents.
    $failed=$false
    foreach ($entry in @($journal.Entries)) {
        try {
            Assert-HarnessRegistryAddress $entry.Key $entry.Name
            if ($Writer) { $null=& $Writer $entry } else { Restore-HarnessRegistryValue $entry }
        } catch { $failed=$true }
    }
    if ($failed) { throw 'Registry recovery incomplete; retain journal and retry after closing acceptance processes' }
}
