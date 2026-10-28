Section 'E. Popup acceptance'
$savedPopupClipboard = Save-ClipboardText
try {
Set-ClipboardText 'SMART-SNIPPETS-POPUP-CLIPBOARD-SENTINEL'
Reset-Profile
$p = Start-App
if (-not (Wait-Main $p)) { throw 'E requires main window' }
$null = Create-Snippet $p 'popup alpha bravo probe' '' 'E alpha bravo'
$null = Create-Snippet $p 'popup alpha charlie probe' '' 'E alpha charlie'
# Folder fixtures are created through the same inline editor UI used by D5.
foreach ($name in @('E Rename','E Delete')) {
    $tb = New-SnippetEditor $p
    Set-Text $tb "$name body"
    $script:PrivacySecrets.Add("$name body")
    Press (Get-Main $p) 'New folder'
    $box = Wait-Until { Find-ByName (Get-Main $p) 'New folder name' ([Windows.Automation.ControlType]::Edit) }
    Set-Text $box $name
    Press (Get-Main $p) 'Create folder'
    if (-not (Wait-Until { ((Sql "select count(*) from folders where name='$name';") -join '') -eq '1' })) { throw 'E folder fixture failed' }
    if (-not (Save-Snippet $p)) { throw 'E folder snippet save failed' }
}
$target = Start-Target
if (-not $target) { throw 'E requires Notepad' }
$initialText = Get-NotepadText $target
$beforeUsage = (Sql 'select coalesce(sum(usage_count),0) from snippets;') -join ''
$popup = Open-TestPopup $p $target
$search = Find-ById $popup 'PopupSearch'
$area = [Win32Verify]::WorkArea([IntPtr]$target.Current.NativeWindowHandle)
$rect = $popup.Current.BoundingRectangle
$centered = [Math]::Abs(($rect.X + $rect.Width / 2) - ($area.Left + $area.Right) / 2) -le 2 -and [Math]::Abs(($rect.Y + $rect.Height / 2) - ($area.Top + $area.Bottom) / 2) -le 2
Check 'E1' 'popup visible, centered in target monitor work area, search focused' ($centered -and [bool](Wait-Until { $search.Current.HasKeyboardFocus })) "popup=$rect work=$($area.Left),$($area.Top),$($area.Right),$($area.Bottom)"
Set-PopupQuery $p 'alpha bravo'
$count = Wait-Until { $c = Find-ById (Get-PopupWindow $p.Id) 'PopupCount'; if ($c.Current.Name -eq '1 of 4 snippets') { $c.Current.Name } }
Check 'E2.and' 'typing filters with AND terms' ([bool]$count) "$count"
Set-PopupQuery $p 'zzqxv'
Check 'E2.empty' 'no match empty state' ([bool](Wait-Until { Find-ByName (Get-PopupWindow $p.Id) 'No snippets match' }))
foreach ($pair in @(@('PopupLayoutButton','Toggle layout'),@('PopupSortButton','Sort'),@('PopupAddButton','Add snippet'),@('PopupOpenMainButton','Open main window'))) {
    $button = Find-ById (Get-PopupWindow $p.Id) $pair[0]
    Check "E6.$($pair[0])" 'accessible toolbar name and tooltip' ($button.Current.Name -eq $pair[1] -and -not [string]::IsNullOrWhiteSpace($button.Current.HelpText))
}
Set-PopupQuery $p ''
Set-PopupLayout $p 'Folder'
$rows = @(Get-PopupRows $p)
$enabled = @($rows | Where-Object { $_.Current.IsEnabled })
$headingCount = @($rows | Where-Object { -not $_.Current.IsEnabled }).Count
$movement = $headingCount -ge 3 -and $enabled.Count -eq 4
Select-El $enabled[0]
$search.SetFocus()
# Start explicitly at the first snippet. Walk every snippet, including across heading boundaries.
for ($i = 0; $i -lt $enabled.Count; $i++) {
    $expected = Get-RowText $enabled[$i]
    $movement = $movement -and [bool](Wait-Until { (Get-RowText (Get-SelectedPopupRow $p)) -ceq $expected })
    if ($i -lt $enabled.Count - 1) { Send-Chord @([byte]$VK.Down) }
}
for ($i = $enabled.Count - 2; $i -ge 0; $i--) {
    Send-Chord @([byte]$VK.Up)
    $expected = Get-RowText $enabled[$i]
    $movement = $movement -and [bool](Wait-Until { (Get-RowText (Get-SelectedPopupRow $p)) -ceq $expected })
}
Check 'E3' 'Up/Down moves selection and skips folder headings' $movement "headings=$headingCount snippets=$($enabled.Count)"
Close-TestPopup $p
Check 'E4' 'Esc closes, no insertion, usage unchanged' ((Get-NotepadText $target) -ceq $initialText -and ((Sql 'select coalesce(sum(usage_count),0) from snippets;') -join '') -eq $beforeUsage)
foreach ($layout in @('Flat','Folder')) {
    foreach ($sort in @('LastUsed','UsageCount','Created','Updated')) {
        $null = Open-TestPopup $p $target
        Set-PopupLayout $p $layout
        Set-PopupSort $p $sort
        Close-TestPopup $p
        $null = Open-TestPopup $p $target
        $persisted = (Setting 'popup.layout') -eq $layout -and (Setting 'popup.sort') -eq $sort
        $persisted = $persisted -and ((Get-ToggleState (Find-ById (Get-PopupWindow $p.Id) 'PopupLayoutButton')) -eq $(if ($layout -eq 'Folder') { 'On' } else { 'Off' }))
        Close-TestPopup $p
        Stop-App
        $p = Start-App @('--background')
        $null = Open-TestPopup $p $target
        $tooltip = (Find-ById (Get-PopupWindow $p.Id) 'PopupSortButton').Current.HelpText
        $expectedTooltip = @{ LastUsed='Sort: last used'; UsageCount='Sort: usage count'; Created='Sort: created date'; Updated='Sort: updated date' }[$sort]
        Check "E5.$layout.$sort" 'layout and sort persist on reopen and restart (settings + UI)' ($persisted -and (Setting 'popup.layout') -eq $layout -and (Setting 'popup.sort') -eq $sort -and $tooltip -eq $expectedTooltip -and ((Get-ToggleState (Find-ById (Get-PopupWindow $p.Id) 'PopupLayoutButton')) -eq $(if ($layout -eq 'Folder') { 'On' } else { 'Off' })))
        Close-TestPopup $p
    }
}
$null = Open-TestPopup $p $target
Set-PopupQuery $p 'alpha bravo'
$priorSelection = Get-RowText (Get-SelectedPopupRow $p)
Invoke-El (Find-ById (Get-PopupWindow $p.Id) 'PopupAddButton')
$cancel = Wait-Until { $b = Find-ById (Get-PopupWindow $p.Id) 'PopupEditorCancel'; if ($b -and -not $b.Current.IsOffscreen) { $b } }
Invoke-El $cancel
$cancelRestored = Wait-Until { (Get-Text (Find-ById (Get-PopupWindow $p.Id) 'PopupSearch')) -eq 'alpha bravo' -and (Get-RowText (Get-SelectedPopupRow $p)) -ceq $priorSelection }
Check 'E7.cancel' 'Cancel restores query and selection' ([bool]$cancelRestored)
Invoke-El (Find-ById (Get-PopupWindow $p.Id) 'PopupAddButton')
$editor = Wait-Until { $e = Find-ById (Get-PopupWindow $p.Id) 'PopupEditor'; if ($e -and -not $e.Current.IsOffscreen) { $e } }
$createdText = 'E popup immediate unique zebra'
$script:PrivacySecrets.Add($createdText)
Set-Text (Find-ByName $editor 'Snippet text' ([Windows.Automation.ControlType]::Edit)) $createdText
Invoke-El (Find-ById (Get-PopupWindow $p.Id) 'PopupEditorSave')
$saved = Wait-Until { (Get-RowText (Get-SelectedPopupRow $p)) -match [regex]::Escape($createdText) -and (Get-Text (Find-ById (Get-PopupWindow $p.Id) 'PopupSearch')) -eq '' }
Check 'E7.saved' 'save refreshes list, selects new snippet, clears hiding query' ([bool]$saved)
$from = @(Get-LogLines).Count
(Find-ById (Get-PopupWindow $p.Id) 'PopupSearch').SetFocus()
Send-Chord @([byte]$VK.Enter)
Check 'E7.insert' 'Enter immediately inserts new snippet into Notepad' ((Wait-Log 'insert_result outcome=Inserted' $from) -and [bool](Wait-Until { (Get-NotepadText $target) -ceq ($initialText + $createdText) }))
$null = Open-TestPopup $p $target
Invoke-El (Find-ById (Get-PopupWindow $p.Id) 'PopupOpenMainButton')
Check 'E8' 'Open main window opens main and hides popup' ([bool](Wait-Until { $w = Get-Main $p; $w -and [Win32Verify]::IsWindowVisible([IntPtr]$w.Current.NativeWindowHandle) -and -not (Get-PopupWindow $p.Id) }))
$null = Open-TestPopup $p $target
function Get-PopupFocusToken {
    $el = $script:AE::FocusedElement
    $walker = [Windows.Automation.TreeWalker]::ControlViewWalker
    while ($el) {
        if ($el.Current.AutomationId -in @('PopupSearch','PopupLayoutButton','PopupSortButton','PopupAddButton','PopupOpenMainButton','PopupList')) { return $el.Current.AutomationId }
        $el = $walker.GetParent($el)
    }
    return 'outside'
}
(Find-ById (Get-PopupWindow $p.Id) 'PopupSearch').SetFocus()
$forward = @('PopupSearch')
for ($i=0; $i -lt 6; $i++) {
    $oldToken = Get-PopupFocusToken
    Send-Chord @([byte]$VK.Tab)
    $null = Wait-Until { (Get-PopupFocusToken) -ne $oldToken }
    $forward += Get-PopupFocusToken
}
$reverse = @('PopupSearch')
for ($i=0; $i -lt 6; $i++) {
    $oldToken = Get-PopupFocusToken
    Send-Chord @([byte]$VK.Shift,[byte]$VK.Tab)
    $null = Wait-Until { (Get-PopupFocusToken) -ne $oldToken }
    $reverse += Get-PopupFocusToken
}
$expectedReverse = @($forward); [array]::Reverse($expectedReverse)
Check 'E9.tab' 'Tab/Shift+Tab cycle through search, toolbar and list in reverse order' ($forward[-1] -eq 'PopupSearch' -and @($forward | Select-Object -Unique).Count -eq 6 -and 'outside' -notin $forward -and ($reverse -join ',') -eq ($expectedReverse -join ',')) "forward=$($forward -join ','); reverse=$($reverse -join ',')"
Set-PopupQuery $p $createdText
$beforeText = Get-NotepadText $target
$from = @(Get-LogLines).Count
Click-Element (Get-SelectedPopupRow $p)
Check 'E9.click' 'mouse row click equals Enter' ((Wait-Log 'insert_result outcome=Inserted' $from) -and [bool](Wait-Until { (Get-NotepadText $target) -ceq ($beforeText + $createdText) }))
Stop-App
$p = Start-App @('--background')
$from = @(Get-LogLines).Count
$null = Open-TestPopup $p $target
$cold = Get-ReadySample $from
$null = Test-TimingBudget 'E10.cold' @($cold) 300
Close-TestPopup $p
$samples = @(Measure-PopupOpens $p $target 20)
$null = Test-TimingBudget 'E10' $samples 300
# Main-window edits while hidden, followed by reopening the real popup (not SQL mutations).
# This instance started --background for E10; explicitly show its main window via the popup.
$null = Open-TestPopup $p $target
Invoke-El (Find-ById (Get-PopupWindow $p.Id) 'PopupOpenMainButton')
if (-not (Wait-Main $p)) { throw 'E11 main window did not open' }
$null = Set-Foreground (Get-Main $p)
$null = Create-Snippet $p 'E11 unique propagation term' '' 'E11 new label'
Nav $p 'SnippetsItem'
Set-Text (Find-ById (Get-Main $p) 'SearchBox') 'alpha charlie'
$row = Wait-Until { @(Find-All (Find-ById (Get-Main $p) 'SnippetList') ([Windows.Automation.ControlType]::ListItem)) | Select-Object -First 1 }
Select-El $row
$label = Wait-Until { Find-ByName (Get-Main $p) 'Snippet label' ([Windows.Automation.ControlType]::Edit) }
Set-Text $label 'E11 edited label'
$null = Save-Snippet $p
$null = Open-TestPopup $p $target
Set-PopupQuery $p ''
$propagated = Wait-Until { $t = Get-PopupText $p; $t -match 'E11 new label' -and $t -match 'E11 edited label' }
Set-PopupQuery $p 'unique propagation'
Check 'E11' 'new row, edited label visible on reopen; unique term finds new snippet' ([bool]$propagated -and (Get-PopupText $p) -match 'E11 new label' -and (Find-ById (Get-PopupWindow $p.Id) 'PopupCount').Current.Name -eq '1 of 6 snippets')
Close-TestPopup $p
Nav $p 'FoldersItem'
$renameRow = Wait-Until { @(Find-All (Find-ById (Get-Main $p) 'FolderList') ([Windows.Automation.ControlType]::ListItem)) | Where-Object { (Get-RowText $_) -match 'E Rename' } | Select-Object -First 1 }
Invoke-El (Btn $renameRow 'Rename folder')
$edit = Wait-Until { @(Find-All (Get-Main $p) ([Windows.Automation.ControlType]::Edit)) | Where-Object { $_.Current.Name -notmatch 'New folder name' -and $_.Current.AutomationId -ne 'InputBox' } | Select-Object -First 1 }
Set-Text $edit 'E Renamed'
Send-Chord @([byte]$VK.Enter)
if (-not (Wait-Until { ((Sql "select count(*) from folders where name='E Renamed';") -join '') -eq '1' })) { throw 'E12 rename did not save' }
$deleteRow = Wait-Until { @(Find-All (Find-ById (Get-Main $p) 'FolderList') ([Windows.Automation.ControlType]::ListItem)) | Where-Object { (Get-RowText $_) -match 'E Delete' } | Select-Object -First 1 }
Invoke-El (Btn $deleteRow 'Delete folder')
$null = Confirm-Dialog $p 'Delete folder'
$null = Open-TestPopup $p $target
Set-PopupLayout $p 'Folder'
Set-PopupQuery $p ''
$text = Get-PopupText $p
$rows = @(Get-PopupRows $p)
$underUnfiled = $false; $moved = $false
foreach ($row in $rows) {
    $t = Get-RowText $row
    if (-not $row.Current.IsEnabled) { $underUnfiled = $t -match 'Unfiled \(' }
    elseif ($underUnfiled -and $t -match 'E Delete body') { $moved = $true }
}
Check 'E12' 'renamed heading replaces old name; deleted-folder snippet under Unfiled' ($text -match 'E Renamed \(1\)' -and $text -notmatch 'E Rename \(' -and $text -notmatch 'E Delete \(' -and $moved)
Close-TestPopup $p
} finally {
    try { Stop-Target } finally { try { Stop-App } finally { Restore-ClipboardText $savedPopupClipboard } }
}
