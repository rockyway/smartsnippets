# Process-object ownership only; no discovery/termination by process name. Factory injection is
# for isolated fake-boundary tests, not a second app-launch path in desktop acceptance.
function Start-OwnedProcess([string]$Path, [string[]]$ProcessArgs, [Collections.IList]$Owned, [scriptblock]$Factory = $null) {
    if ($Factory) { $process=& $Factory $Path $ProcessArgs }
    elseif ($ProcessArgs.Count) { $process=Start-Process -FilePath $Path -ArgumentList $ProcessArgs -PassThru }
    else { $process=Start-Process -FilePath $Path -PassThru }
    try {
        $null=$process.Handle # Pin immediately, before any wait/UIA/probe.
        $null=$Owned.Add($process)
        return $process
    } catch {
        Stop-OwnedProcess $process
        throw 'Could not register owned process; exact launched process stopped'
    }
}
function Stop-OwnedProcess($Process) {
    if (-not $Process.HasExited) {
        $Process.Kill()
        if (-not $Process.WaitForExit(5000)) { throw 'Owned process did not exit; recovery must not continue' }
    }
}
function Wait-OwnedSecondLaunch($Process, [int]$TimeoutMs = 10000) {
    try { return $Process.WaitForExit($TimeoutMs) }
    finally { Stop-OwnedProcess $Process } # Even the failure being tested cannot escape ownership.
}
