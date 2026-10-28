# Disposable classic Notepad only. Never discover targets by a global class-name search.
$script:OwnedTarget = $null
function Get-OwnedTargetWindow {
    if (-not $script:OwnedTarget) { throw 'No disposable target is owned by this harness' }
    $owned = $script:OwnedTarget
    $proc = $owned.Process
    $proc.Refresh()
    if ($proc.HasExited) { throw 'Disposable target exited' }
    $windows = @(Get-AppWindows $proc.Id | Where-Object { [Win32Verify]::IsWindowVisible([IntPtr]$_.Current.NativeWindowHandle) })
    $records = @($windows | ForEach-Object {
        [pscustomobject]@{ ProcessId=$_.Current.ProcessId; ClassName=$_.Current.ClassName; Title=$_.Current.Name
            HasClassicEdit=([Win32Verify]::FindWindowEx([IntPtr]$_.Current.NativeWindowHandle, [IntPtr]::Zero, 'Edit', $null) -ne [IntPtr]::Zero) }
    })
    $actual = [pscustomobject]@{ Id=$proc.Id; StartTicks=$proc.StartTime.ToUniversalTime().Ticks; Path=$proc.MainModule.FileName }
    if (-not (Test-DisposableTargetIdentity $owned.Identity $actual $records)) {
        throw 'Target isolation cannot be proved. Only classic single-document Notepad is supported; no input or termination is permitted.'
    }
    # Register both the unique title token and observed title: modified-title prefixes/localized
    # suffixes must not evade privacy detection. Neither value is printed in diagnostics.
    Register-PrivacyText $owned.Identity.Token
    foreach ($record in $records) { Register-PrivacyText $record.Title }
    return $windows[0]
}
function Assert-OwnedTarget($Window) {
    $verified = Get-OwnedTargetWindow
    if (-not $Window -or $verified.Current.NativeWindowHandle -ne $Window.Current.NativeWindowHandle) { throw 'Target window identity changed' }
}
function Start-Target {
    Stop-Target
    # Fail before launch if Notepad is already in use. Do not close user windows, even with consent
    # inferred from a run command. A modern launcher/session-reuse result is refused below too.
    if (Get-Process -Name notepad -ErrorAction SilentlyContinue) { throw 'Notepad is already running. Save and close it yourself before acceptance; the harness will not touch it.' }
    $token = 'SS-acceptance-' + [guid]::NewGuid().ToString('N')
    $file = Join-Path ([IO.Path]::GetTempPath()) ($token + '.txt')
    [IO.File]::WriteAllText($file, '', (New-Object Text.UTF8Encoding $false))
    $path = (Resolve-Path -LiteralPath $TargetExe).Path
    $proc = Start-Process -FilePath $path -ArgumentList ('"' + $file + '"') -PassThru
    # Cache a process handle now: never kill a subsequently reused PID.
    $null = $proc.Handle
    $identity = [pscustomobject]@{ Id=$proc.Id; StartTicks=$proc.StartTime.ToUniversalTime().Ticks; Path=$path; Token=$token }
    $script:OwnedTarget = [pscustomobject]@{ Process=$proc; Identity=$identity; File=$file; Verified=$false }
    $window = Wait-Until {
        $proc.Refresh()
        if ($proc.HasExited) { throw 'Notepad launcher exited/reused another instance; target refused. No spawned or existing window will be killed.' }
        $visible = @(Get-AppWindows $proc.Id | Where-Object { [Win32Verify]::IsWindowVisible([IntPtr]$_.Current.NativeWindowHandle) })
        if ($visible.Count) { Get-OwnedTargetWindow }
    } 8000 100
    if (-not $window) { throw 'Disposable classic Notepad did not become available; no unverified process will be killed' }
    $script:OwnedTarget.Verified = $true
    if (-not (Set-Foreground $window)) { throw 'Cannot foreground owned target' }
    return $window
}
function Stop-Target {
    if (-not $script:OwnedTarget) { return }
    $owned = $script:OwnedTarget
    $owned.Process.Refresh()
    if (-not $owned.Process.HasExited) {
        if (-not $owned.Verified) { throw 'Unverified Notepad was left untouched. Inspect and close it manually before retrying.' }
        $null = Get-OwnedTargetWindow # Recheck topology/title before destructive termination.
        $owned.Process.Kill()
        if (-not $owned.Process.WaitForExit(5000)) { throw 'Owned Notepad did not exit' }
    }
    $owned.Process.Dispose()
    Remove-Item -LiteralPath $owned.File -Force -ErrorAction SilentlyContinue
    $script:OwnedTarget = $null
}
