# Dot-sourced by the acceptance harness; depends on its Sql/Check/Wait-Log helpers.
function Open-TestPopup($Proc, $Target) {
    Assert-OwnedTarget $Target
    if (-not (Set-Foreground $Target)) { throw 'Could not foreground insertion target' }
    $from = @(Get-LogLines).Count
    Press-Hotkey @([byte]$VK.Ctrl, [byte]$VK.Shift, [byte]$VK.Space)
    $popup = Wait-Until { Get-PopupWindow $Proc.Id } 8000 50
    if (-not $popup -or -not (Wait-Log 'popup_ready_ms \d+' $from)) { throw 'Popup did not become ready' }
    return $popup
}
function Close-TestPopup($Proc) {
    if (Get-PopupWindow $Proc.Id) {
        Send-Chord @([byte]$VK.Esc)
        if (-not (Wait-Until { -not (Get-PopupWindow $Proc.Id) } 5000 50)) { throw 'Popup did not close' }
    }
}
function Set-PopupQuery($Proc, [string]$Query) {
    $search = Find-ById (Get-PopupWindow $Proc.Id) 'PopupSearch'
    if ((Get-Text $search) -ceq $Query) { return }
    $from = @(Get-LogLines).Count
    Set-Text $search $Query
    if (-not (Wait-Log 'popup_search_ms \d+ layout=' $from)) { throw 'Search render timing missing' }
}
function Type-PopupQuery($Proc, [string]$Query) {
    $popup=Get-PopupWindow $Proc.Id
    $search=Find-ById $popup 'PopupSearch'
    if ((Get-Text $search) -cne '') { throw 'Typing test requires an empty search box' }
    $search.SetFocus()
    $from=@(Get-LogLines).Count
    foreach ($character in $Query.ToCharArray()) {
        if (-not $search.Current.HasKeyboardFocus) { throw 'Popup search lost keyboard focus; typing stopped' }
        $null=[Win32Verify]::PopupKeyboardWindow([IntPtr]$popup.Current.NativeWindowHandle, $Proc.Id)
        [Win32Verify]::TypeCharacter($character)
    }
    if (-not (Wait-Until { (Get-Text $search) -ceq $Query })) { throw 'Typed popup query did not arrive intact' }
    if (-not (Wait-Log 'popup_search_ms \d+ layout=' $from)) { throw 'Typed search did not render' }
}
function Set-PopupLayout($Proc, [string]$Layout) {
    $button = Find-ById (Get-PopupWindow $Proc.Id) 'PopupLayoutButton'
    $wanted = $Layout -eq 'Folder'
    if (((Get-ToggleState $button) -eq 'On') -ne $wanted) {
        Toggle-El $button
        if (-not (Wait-Until { ((Get-ToggleState (Find-ById (Get-PopupWindow $Proc.Id) 'PopupLayoutButton')) -eq 'On') -eq $wanted })) { throw 'Layout toggle failed' }
    }
}
function Set-PopupSort($Proc, [string]$Sort) {
    # The default LastUsed is not stored until changed; selecting it while already active is
    # intentionally a no-op in SetSort. Move away first so E5 actually exercises persistence.
    if ($Sort -eq 'LastUsed' -and -not (Setting 'popup.sort')) { Set-PopupSort $Proc 'Created' }
    $names = @{ LastUsed='Last used'; UsageCount='Usage count'; Created='Created date'; Updated='Updated date' }
    Invoke-El (Find-ById (Get-PopupWindow $Proc.Id) 'PopupSortButton')
    $item = Wait-Until { Find-ByName (Get-PopupWindow $Proc.Id) $names[$Sort] } 3000 50
    if (-not $item) { throw "Sort menu item missing: $Sort" }
    Invoke-El $item
    if (-not (Wait-Until { (Setting 'popup.sort') -eq $Sort })) { throw 'Sort not persisted' }
}
function Get-PopupRows($Proc) {
    $list = Find-ById (Get-PopupWindow $Proc.Id) 'PopupList'
    @(Find-All $list ([Windows.Automation.ControlType]::ListItem))
}
function Get-SelectedPopupRow($Proc) {
    $list = Find-ById (Get-PopupWindow $Proc.Id) 'PopupList'
    $pattern = $list.GetCurrentPattern([Windows.Automation.SelectionPattern]::Pattern)
    @($pattern.GetCurrentSelection()) | Select-Object -First 1
}
function Get-RowText($Row) {
    if (-not $Row) { return '' }
    (@($Row.Current.Name) + @(Find-All $Row ([Windows.Automation.ControlType]::Text) | ForEach-Object { $_.Current.Name })) -join '|'
}
function Get-PopupText($Proc) {
    (Find-All (Find-ById (Get-PopupWindow $Proc.Id) 'PopupList') ([Windows.Automation.ControlType]::Text) | ForEach-Object { $_.Current.Name }) -join '|'
}
function Click-Element($Element) {
    $r = $Element.Current.BoundingRectangle
    if ($r.IsEmpty -or $Element.Current.IsOffscreen) { throw 'Cannot click offscreen element' }
    [Win32Verify]::Click([int]($r.X + $r.Width / 2), [int]($r.Y + $r.Height / 2))
}
function Send-PopupDuplicate($Proc, [string]$Mode) {
    $popup = Get-PopupWindow $Proc.Id
    $search = Find-ById $popup 'PopupSearch'
    $search.SetFocus()
    if (-not $search.Current.HasKeyboardFocus) { throw 'Duplicate test requires popup search focus' }
    $window = [IntPtr]$popup.Current.NativeWindowHandle
    $keyboard = [Win32Verify]::PopupKeyboardWindow($window, $Proc.Id)
    $mouse = [IntPtr]::Zero; $point = New-Object Win32Verify+Point
    if ($Mode -eq 'EnterAndClick') {
        $row = Get-SelectedPopupRow $Proc
        $rect = $row.Current.BoundingRectangle
        if ($rect.IsEmpty -or $row.Current.IsOffscreen) { throw 'Duplicate test row is not visible' }
        $point.X=[int]($rect.X+$rect.Width/2); $point.Y=[int]($rect.Y+$rect.Height/2)
        $mouse=[Win32Verify]::PopupMouseWindow($window, $Proc.Id, [ref]$point)
    }
    $messages = @(Get-PopupDuplicateMessages $Mode $keyboard.ToInt64() $mouse.ToInt64() $point.X $point.Y)
    # Targeted, queued messages, NOT global SendInput/cached-coordinate clicks. Even after hide,
    # the second activation cannot go to an underlying app. Receipt at the VM gate is mandatory.
    foreach ($m in $messages) {
        if (-not [Win32Verify]::PostMessage([IntPtr]$m.Window, $m.Message, [IntPtr]$m.WParam, [IntPtr]$m.LParam)) { throw 'Could not queue popup activation' }
    }
}
function Get-ReadySample([int]$From) {
    ConvertFrom-PopupReadyLog @(Get-LogLines | Select-Object -Skip $From)
}
function Measure-PopupOpens($Proc, $Target, [int]$Count, [switch]$BothLayouts) {
    $samples = @()
    for ($i = 0; $i -lt $Count; $i++) {
        $from = @(Get-LogLines).Count
        $null = Open-TestPopup $Proc $Target
        $samples += Get-ReadySample $from
        if ($BothLayouts -and $i -eq ($Count / 2 - 1)) { Set-PopupLayout $Proc 'Folder' }
        Close-TestPopup $Proc
    }
    return $samples
}
function Test-TimingBudget([string]$Id, [double[]]$Samples, [double]$Budget, [switch]$MedianOnly) {
    $stats = Get-TimingStatistics $Samples
    $ok = if ($MedianOnly) { $stats.Median -le $Budget } else { $stats.Max -le $Budget }
    Check $Id 'timing budget (ms)' $ok ($stats | ConvertTo-Json -Compress)
    if ($MedianOnly -and $stats.P95 -gt 300) { Add-Result 'INFO' "$Id.p95" 'USER ATTENTION: p95 exceeds 300 ms' $stats.P95 }
    return $ok
}
