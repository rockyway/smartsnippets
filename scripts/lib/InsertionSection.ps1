Section 'F. Insertion acceptance'
$savedClipboard = Save-ClipboardText
$sentinel = 'SMART-SNIPPETS-CLIPBOARD-SENTINEL'
function Get-TargetEditor($Target) {
    foreach ($el in @(@(Find-All $Target ([Windows.Automation.ControlType]::Edit)) + @(Find-All $Target ([Windows.Automation.ControlType]::Document)))) {
        $pattern = $null
        if ($el.TryGetCurrentPattern([Windows.Automation.TextPattern]::Pattern, [ref]$pattern)) { return $el }
    }
    throw 'No Notepad TextPattern editor'
}
function Set-TargetText($Target, [string]$Text) {
    $saved = Save-ClipboardText
    try {
        $null = Set-Foreground $Target
        (Get-TargetEditor $Target).SetFocus()
        Send-Chord @([byte]$VK.Ctrl,[byte]$VK.A)
        if ($Text) { Set-Clipboard -Value $Text; Send-Chord @([byte]$VK.Ctrl,[byte]0x56) }
        else { Send-Chord @([byte]$VK.Delete) }
        if (-not (Wait-Until { (Get-NotepadText $Target) -ceq $Text })) { throw 'Notepad fixture text mismatch' }
    } finally { Restore-ClipboardText $saved }
}
function Open-ProbePopup($Proc, $Target, [string]$Query = 'smart-snippets-probe') {
    $null = Open-TestPopup $Proc $Target
    Set-PopupQuery $Proc $Query
    (Find-ById (Get-PopupWindow $Proc.Id) 'PopupSearch').SetFocus()
}
function Get-ProbeUsage { (Sql "select usage_count from snippets where label='F plain';") -join '' }
function Wait-ClipboardPrompt($Proc, [int]$From) {
    $prompt = "Some of what's on your clipboard can't be restored after pasting. Paste anyway?"
    $shown = Wait-Until {
        $footer = Find-ById (Get-PopupWindow $Proc.Id) 'PopupFooterError'
        $paste = Find-ById (Get-PopupWindow $Proc.Id) 'PopupPasteAnyway'
        $cancel = Find-ById (Get-PopupWindow $Proc.Id) 'PopupPasteCancel'
        $footer -and $footer.Current.Name -ceq $prompt -and $paste.Current.Name -eq 'Paste anyway' -and $cancel.Current.Name -eq 'Cancel' -and -not $paste.Current.IsOffscreen -and -not $cancel.Current.IsOffscreen
    }
    return [bool]$shown -and (Wait-Log 'insert_result outcome=ClipboardNotPreserved' $From)
}
try {
    Reset-Profile
    $p = Start-App
    if (-not (Wait-Main $p)) { throw 'F requires main window' }
    $source = "Héllo – naïve 👋 世界 Ωμέγα`r`tline 2  `nline 3`r`nline 4"
    $probe = $source -replace '\r\n|\r|\n', "`r`n"
    $null = Create-Snippet $p $source '' 'F unicode'
    $null = Create-Snippet $p 'smart-snippets-probe' '' 'F plain'
    $target = Start-Target
    if (-not $target) { throw 'F requires Notepad' }
    Set-TargetText $target ''
    Set-Clipboard -Value $sentinel
    $before = (Sql "select usage_count,coalesce(last_used_at,0) from snippets where label='F unicode';") -join ''
    Open-ProbePopup $p $target 'F unicode'
    $from = @(Get-LogLines).Count
    Send-Chord @([byte]$VK.Enter)
    $inserted = Wait-Log 'insert_result outcome=Inserted' $from 10000
    $exact = Wait-Until { (Get-NotepadText $target) -ceq $probe } 10000 50
    $actual = Get-NotepadText $target
    # Ordinal equality compares all UTF-16 code units, including surrogate pairs, tabs and spaces.
    Check 'F1' 'Unicode/mixed-CR-LF probe equals CRLF-normalized text (ordinal)' ($inserted -and [bool]$exact -and [string]::Equals($actual,$probe,[StringComparison]::Ordinal)) "actual units=$($actual.Length); expected=$($probe.Length)"
    Check 'F3' 'clipboard equals sentinel after restore delay' ([bool](Wait-Until { (Get-Clipboard -Raw) -ceq $sentinel } 10000 50))
    $usage = (Sql "select usage_count,last_used_at from snippets where label='F unicode';") -join ''
    $parts = $usage -split '\|'; $old = $before -split '\|'
    Check 'F4.database' 'usage +1 exactly once and last_used_at advanced' ([int]$parts[0] -eq ([int]$old[0] + 1) -and [long]$parts[1] -gt [long]$old[1]) "before=$before after=$usage"
    Focus-Main $p
    Nav $p 'SnippetsItem'
    Set-Text (Find-ById (Get-Main $p) 'SearchBox') 'F unicode'
    $row = Wait-Until { @(Find-All (Find-ById (Get-Main $p) 'SnippetList') ([Windows.Automation.ControlType]::ListItem)) | Select-Object -First 1 }
    Select-El $row
    $usedStat = Wait-Until {
        $used = Find-ByName (Get-Main $p) 'Used' ([Windows.Automation.ControlType]::Text)
        if ($used) {
            $parent = [Windows.Automation.TreeWalker]::ControlViewWalker.GetParent($used)
            $values = @(Find-All $parent ([Windows.Automation.ControlType]::Text) | ForEach-Object { $_.Current.Name })
            $values -contains 'Once'
        }
    }
    Check 'F4.main' 'main-window Used stat reflects exactly one insertion' ([bool]$usedStat)
    $null = Open-TestPopup $p $target
    Set-PopupQuery $p ''
    Set-PopupLayout $p 'Flat'
    Set-PopupSort $p 'UsageCount'
    $rows = @(Get-PopupRows $p | Where-Object { $_.Current.IsEnabled })
    Check 'F4.sort' 'popup Most-used (Usage count) sort puts used snippet before unused probe' ($rows.Count -eq 2 -and (Get-RowText $rows[0]) -match 'F unicode' -and (Get-RowText $rows[1]) -match 'F plain')
    Close-TestPopup $p
    Set-TargetText $target 'prefix replaceword suffix'
    $editor = Get-TargetEditor $target
    $pattern = $editor.GetCurrentPattern([Windows.Automation.TextPattern]::Pattern)
    $range = $pattern.DocumentRange.FindText('replaceword',$false,$false)
    if (-not $range) { throw 'F2 word range missing' }
    $editor.SetFocus(); $range.Select()
    Open-ProbePopup $p $target
    $from = @(Get-LogLines).Count
    Send-Chord @([byte]$VK.Enter)
    Check 'F2' 'selected word replaced, surrounding text unchanged' ((Wait-Log 'insert_result outcome=Inserted' $from) -and [bool](Wait-Until { (Get-NotepadText $target) -ceq 'prefix smart-snippets-probe suffix' }))
    Set-TargetText $target ''
    $beforeUsage = Get-ProbeUsage
    Open-ProbePopup $p $target
    $from = @(Get-LogLines).Count
    Send-Chord @([byte]$VK.Enter)
    Send-Chord @([byte]$VK.Enter)
    # Exercise the popup's insertion gate while restoration is pending as well as the fast double Enter.
    Press-Hotkey @([byte]$VK.Ctrl,[byte]$VK.Shift,[byte]$VK.Space)
    $done = Wait-Log 'insert_result outcome=Inserted' $from
    $guard = Wait-Log 'popup_open_ignored reason=inserting|insert_result outcome=Busy' $from 3000
    $text = Get-NotepadText $target
    $occurrences = [regex]::Matches($text,[regex]::Escape('smart-snippets-probe')).Count
    $successful = @(Get-LogLines | Select-Object -Skip $from | Where-Object { $_ -match 'insert_result outcome=Inserted' }).Count
    Check 'F5' 'double activation inserts once, increments once, guard logged' ($done -and $guard -and $occurrences -eq 1 -and $successful -eq 1 -and [int](Get-ProbeUsage) -eq ([int]$beforeUsage + 1)) (Tail $from)
    Close-TestPopup $p
    Set-TargetText $target ''
    Open-ProbePopup $p $target
    $beforeUsage = Get-ProbeUsage
    Stop-Target
    $from = @(Get-LogLines).Count
    Send-Chord @([byte]$VK.Enter)
    $closedMessage = Wait-Until { $f = Find-ById (Get-PopupWindow $p.Id) 'PopupFooterError'; $f -and $f.Current.Name -match "^Couldn't insert\. .+ was closed\. Nothing was pasted\.$" }
    Check 'F6' 'closed target: actionable message, no dispatch, usage unchanged' ((Wait-Log 'insert_result outcome=TargetClosed' $from) -and [bool]$closedMessage -and -not (Log-Has 'paste_sent' $from) -and (Get-ProbeUsage) -eq $beforeUsage)
    Close-TestPopup $p
    Focus-Main $p
    $beforeAll = (Sql 'select sum(usage_count) from snippets;') -join ''
    $from = @(Get-LogLines).Count
    Press-Hotkey @([byte]$VK.Ctrl,[byte]$VK.Shift,[byte]$VK.Space)
    $ignored = Wait-Log 'hotkey_ignored_own_or_no_window|target_rejected reason=own_process' $from
    Check 'F7' 'hotkey from main window cannot insert into Smart Snippets' ($ignored -and -not (Get-PopupWindow $p.Id) -and -not (Log-Has 'paste_sent' $from) -and ((Sql 'select sum(usage_count) from snippets;') -join '') -eq $beforeAll)
    Skip 'F8' 'elevated target' 'requires user UAC acceptance; see compatibility matrix'
    # F9-F11 each start from a fresh Notepad and an independently fingerprinted oversized payload.
    foreach ($case in @(9,10,11)) {
        $target = Start-Target
        if (-not $target) { throw "F$case requires Notepad" }
        Set-TargetText $target ''
        $beforeUsage = Get-ProbeUsage
        Set-UnsaveableClipboard $sentinel
        Open-ProbePopup $p $target
        $from = @(Get-LogLines).Count
        Send-Chord @([byte]$VK.Enter)
        $prompt = Wait-ClipboardPrompt $p $from
        $untouched = (Get-NotepadText $target) -ceq '' -and (Test-ClipboardIntact) -and (Get-ProbeUsage) -eq $beforeUsage -and -not (Log-Has 'paste_sent' $from)
        Check "F$case.prompt" 'unsaveable clipboard prompts before any paste or clipboard loss' ($prompt -and $untouched)
        if (-not $prompt) { throw "F$case prompt missing; unsafe to proceed" }
        if ($case -eq 9) {
            Invoke-El (Find-ById (Get-PopupWindow $p.Id) 'PopupPasteCancel')
            $cancelled = Wait-Log 'popup_paste_cancelled' $from
            $events = @(Get-LogLines | Select-Object -Skip $from)
            $resultIndex = -1; $cancelIndex = -1
            for ($i=0; $i -lt $events.Count; $i++) {
                if ($events[$i] -match 'insert_result outcome=ClipboardNotPreserved') { $resultIndex=$i }
                if ($events[$i] -match 'popup_paste_cancelled') { $cancelIndex=$i }
            }
            Check 'F9' 'Cancel keeps target, all clipboard formats/hashes and usage; ordered cancellation logs' ($cancelled -and $resultIndex -ge 0 -and $cancelIndex -gt $resultIndex -and (Get-NotepadText $target) -ceq '' -and (Test-ClipboardIntact) -and (Get-ProbeUsage) -eq $beforeUsage -and -not (Log-Has 'paste_sent' $from))
        } else {
            if ($case -eq 11) { Stop-Target }
            $from = @(Get-LogLines).Count
            Invoke-El (Find-ById (Get-PopupWindow $p.Id) 'PopupPasteAnyway')
            if ($case -eq 10) {
                $done = Wait-Log 'insert_result outcome=Inserted' $from
                $restored = Wait-Until { Test-UnsaveableClipboardRestored } 10000 50
                Check 'F10' 'Paste anyway pastes once, usage +1, saved formats restored and oversized payload absent' ($done -and [bool]$restored -and (Get-NotepadText $target) -ceq 'smart-snippets-probe' -and [int](Get-ProbeUsage) -eq ([int]$beforeUsage + 1) -and (Log-Has 'clipboard_restore status=Restored' $from))
            } else {
                $done = Wait-Log 'insert_result outcome=CopiedForManualPaste' $from
                $message = Wait-Until { $f = Find-ById (Get-PopupWindow $p.Id) 'PopupFooterError'; $f -and $f.Current.Name -match "^Couldn't paste into .+\. The snippet is on your clipboard\. Paste it yourself with Ctrl\+V\.$" }
                Check 'F11' 'target closed after prompt: manual-paste message, probe on clipboard, no dispatch or usage' ($done -and [bool]$message -and (Get-Clipboard -Raw) -ceq 'smart-snippets-probe' -and (Get-ProbeUsage) -eq $beforeUsage -and (Log-Has 'manual_paste_fallback reason=Closed status=Swapped' $from) -and -not (Log-Has 'paste_sent' $from))
            }
        }
        Close-TestPopup $p
        Clear-UnsaveableClipboard
        Stop-Target
    }
    Add-Result 'INFO' 'F12' 'N/A — uncertain dispatch is unit-tested' 'InsertionServiceTests.FailedDispatch_OnlyUncertainReleasesAndWaits and SendThrows_IsUncertain; PopupViewModelInsertionTests.PasteAnyway_ThenMaybePasted_ShowsCaution'
} finally {
    Clear-UnsaveableClipboard
    Stop-Target
    Stop-App
    Restore-ClipboardText $savedClipboard
}
