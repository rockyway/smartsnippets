# Pure safety predicates; no desktop or process operations on import.
function Test-DisposableTargetIdentity($Expected, $Actual, [object[]]$Windows) {
    if (-not $Expected -or -not $Actual -or -not $Expected.Token -or -not $Windows.Count) { return $false }
    if ($Expected.Id -ne $Actual.Id -or $Expected.StartTicks -ne $Actual.StartTicks -or $Expected.Path -ine $Actual.Path) { return $false }
    # Only classic, single-document Notepad is supported. Packaged/tabbed Notepad may restore
    # user tabs even in a new process; never infer ownership just from its launcher PID.
    if ($Windows.Count -ne 1) { return $false }
    foreach ($w in $Windows) {
        if ($w.ProcessId -ne $Expected.Id -or $w.ClassName -cne 'Notepad' -or -not $w.HasClassicEdit -or $w.Title.IndexOf($Expected.Token, [StringComparison]::Ordinal) -lt 0) { return $false }
    }
    return $true
}
