# Posted input only, to positively owned WinUI bridges. Never moves the global cursor.
function Get-OwnedHoverElement([int]$ProcessId, [string]$Id) {
    $condition = New-Object Windows.Automation.PropertyCondition ([Windows.Automation.AutomationElement]::ProcessIdProperty),$ProcessId
    $windows = [Windows.Automation.AutomationElement]::RootElement.FindAll([Windows.Automation.TreeScope]::Children,$condition)
    foreach ($window in $windows) {
        $element = Find-ById $window $Id
        if ($element) { return $element }
    }
    return $null
}
function Invoke-HoverDetailsQa($Window, [int]$ProcessId, [string]$BodyText, [string]$SurfaceName) {
    $hwnd = [IntPtr]$Window.Current.NativeWindowHandle
    $null = Assert-OnDisplay9 $hwnd
    $foreground = [Win32Verify]::GetForegroundWindow()
    $clipboardSequence = [QaResize]::GetClipboardSequenceNumber()
    $focus = [Windows.Automation.AutomationElement]::FocusedElement
    $focusId = if ($focus) { $focus.GetRuntimeId() -join ',' } else { '' }
    $usage = & sqlite3 -batch (Join-Path $profile 'smartsnippets.db') 'SELECT sum(usage_count) FROM snippets;'
    $list = Find-ById $Window $(if ($SurfaceName -eq 'main') { 'SnippetList' } else { 'PopupList' })
    if (-not $list) { throw 'Hover QA list unavailable' }
    $selection = $null
    $selectionIds = ''
    if ($list.TryGetCurrentPattern([Windows.Automation.SelectionPattern]::Pattern, [ref]$selection)) {
        $selectionIds = @($selection.Current.GetSelection() | ForEach-Object { $_.GetRuntimeId() -join ',' }) -join ';'
    }
    foreach ($entry in @(@('First synthetic',$BodyText),@('QA folder','QA folder'),@('#work #qa','#work #qa'))) {
        Update-HarnessProgress
        $target = Find-ByName $list $entry[0] ([Windows.Automation.ControlType]::Text)
        if (-not $target -or $target.Current.IsOffscreen) { throw 'Hover QA target not visible' }
        $r = $target.Current.BoundingRectangle
        $x = [int]($r.Left + [Math]::Min(20,$r.Width/2)); $y = [int]($r.Top+$r.Height/2)
        $windowRect = $Window.Current.BoundingRectangle
        $null = [SmartSnippetsPostedInput]::Move($hwnd,$ProcessId,[int]($windowRect.Left+15),[int]($windowRect.Top+80))
        Start-Sleep -Milliseconds 200
        $null = [SmartSnippetsPostedInput]::Move($hwnd,$ProcessId,$x,$y)
        Start-Sleep -Milliseconds 200
        if (Get-OwnedHoverElement $ProcessId 'HoverDetailsBody') { throw 'Hover QA opened before 500ms' }
        $shown = Wait-Until { Get-OwnedHoverElement $ProcessId 'HoverDetailsBody' } 2500 50
        if (-not $shown) { throw "Hover QA did not open: $SurfaceName" }
        if ($shown.Current.Name -cne $entry[1]) { throw 'Hover QA content mismatch' }
        $scroll = Get-OwnedHoverElement $ProcessId 'HoverDetailsScroll'
        if (-not $scroll) { throw 'Hover QA scroll surface unavailable' }
        $sr = $scroll.Current.BoundingRectangle
        if ($sr.Width -gt 510 -or $sr.Height -gt 350) { throw 'Hover QA surface unbounded' }
        $sx = [int]($sr.Left+$sr.Width/2); $sy = [int]($sr.Top+[Math]::Min(40,$sr.Height/2))
        $null = [SmartSnippetsPostedInput]::Move($hwnd,$ProcessId,$sx,$sy)
        Start-Sleep -Milliseconds 200
        if (-not (Get-OwnedHoverElement $ProcessId 'HoverDetailsBody')) { throw 'Hover QA handoff closed surface' }
        if ($entry[0] -eq 'First synthetic') {
            $pattern = $null
            if (-not $scroll.TryGetCurrentPattern([Windows.Automation.ScrollPattern]::Pattern,[ref]$pattern) -or -not $pattern.Current.VerticallyScrollable) { throw 'Hover QA body not scrollable' }
            for ($i=0; $i -lt 30; $i++) { [SmartSnippetsPostedInput]::Wheel($hwnd,$ProcessId,$sx,$sy,-120) }
            Start-Sleep -Milliseconds 400
            if ($pattern.Current.VerticalScrollPercent -lt 99) { throw 'Hover QA wheel did not reach content bottom' }
            if (-not (Get-OwnedHoverElement $ProcessId 'HoverDetailsBody')) { throw 'Hover QA scroll closed surface' }
        }
        if ($foreground -ne [Win32Verify]::GetForegroundWindow()) { throw 'Hover QA changed foreground' }
        $afterFocus = [Windows.Automation.AutomationElement]::FocusedElement
        $afterId = if ($afterFocus) { $afterFocus.GetRuntimeId() -join ',' } else { '' }
        if ($focusId -cne $afterId) { throw 'Hover QA changed focused element' }
        if ($selection) {
            $afterSelection = @($selection.Current.GetSelection() | ForEach-Object { $_.GetRuntimeId() -join ',' }) -join ';'
            if ($selectionIds -cne $afterSelection) { throw 'Hover QA changed selection' }
        }
        $null = [SmartSnippetsPostedInput]::Move($hwnd,$ProcessId,[int]($windowRect.Left+15),[int]($windowRect.Top+80))
        Start-Sleep -Milliseconds 250
        if (Get-OwnedHoverElement $ProcessId 'HoverDetailsBody') { throw 'Hover QA did not close on exit' }
    }
    $afterUsage = & sqlite3 -batch (Join-Path $profile 'smartsnippets.db') 'SELECT sum(usage_count) FROM snippets;'
    if ($usage -cne $afterUsage) { throw 'Hover QA recorded usage' }
    if ($clipboardSequence -ne [QaResize]::GetClipboardSequenceNumber()) { throw 'Clipboard changed during hover QA' }
    Write-Host "Hover QA passed: surface=$SurfaceName delay/body/metadata/handoff/wheel/focus/selection/usage"
}
