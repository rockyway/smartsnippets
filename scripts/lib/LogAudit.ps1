# Source-level emission-to-consumption audit; safe without an interactive desktop.
function Test-HarnessLogPattern([string[]]$Lines, [string]$Pattern, [int]$From = 0) {
    $tail=@($Lines | Select-Object -Skip ([Math]::Max(0,$From)))
    return @($tail | Where-Object { $_ -match $Pattern }).Count -gt 0
}
function Get-SafeLogSummary([string[]]$Lines, [int]$From = 0, [ValidateRange(1,20)][int]$Limit = 4) {
    $all=@($Lines); $start=[Math]::Max([Math]::Max(0,$From),$all.Count-$Limit)
    $safe=@()
    $allowed=@(Get-HarnessLogEvents)
    for ($i=$start; $i -lt $all.Count; $i++) {
        $event='unrecognized'
        $m=[regex]::Match($all[$i], '\A\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}\.\d{3}Z (?:INFO|ERROR) ([a-z][a-z0-9_]+)(?: |$)')
        if ($m.Success -and $m.Groups[1].Value -cin $allowed) { $event=$m.Groups[1].Value }
        $safe += 'line={0} event={1}' -f ($i+1),$event
    }
    if (-not $safe.Count) { return '(no new log lines)' }
    return $safe -join '; '
}
function Get-HarnessLogEvents {
    @(
        'store_loaded','settings_loaded','hotkey_registered','hotkey_changed','hotkey_startup_failed',
        'hotkey_ignored_own_or_no_window','theme_applied','tray_added','tray_add_failed','tray_readded',
        'restart_elevated_started','exit_unregistered','migration_backup','migrations_applied','migration_failed','data_load_failed',
        'navigate_failed','second_instance_redirect','snippet_saved','snippet_deleted','folder_created','folder_renamed','folder_deleted',
        'app_started','tray_icon','tray_icon_load_failed','tray_theme_changed','popup_ready_ms','popup_search_ms',
        'popup_opened','popup_closed','popup_layout','popup_sort','popup_add_opened','popup_add_saved','popup_add_cancelled',
        'popup_insert','popup_insert_ignored','popup_open_ignored','popup_paste_anyway','popup_paste_cancelled','insert_start','target_check',
        'target_rejected','foreground_gate','clipboard_swap','clipboard_restore','clipboard_restore_formats',
        'insert_result','manual_paste_fallback','usage_record_failed','paste_sent'
    )
}
function Get-HarnessLogAudit([string]$Root) {
    $emitters = @{}
    foreach ($file in (Get-ChildItem (Join-Path $Root 'src') -Recurse -Filter *.cs | Where-Object { $_.FullName -notmatch '[\\/](obj|bin)[\\/]' })) {
        $text = [IO.File]::ReadAllText($file.FullName)
        # First argument only: include both branches of conditional event-name expressions.
        foreach ($call in [regex]::Matches($text, 'AppLog\.(?:Info|Error)\(\s*([^,;\r\n]+)')) {
            foreach ($quoted in [regex]::Matches($call.Groups[1].Value, '"([a-z][a-z0-9_]+)"')) {
                $name = $quoted.Groups[1].Value
                $line = ($text.Substring(0,$call.Index) -split "`n").Count
                $emitters[$name] = $file.FullName.Substring($Root.Length).TrimStart('\','/') + ':' + $line
            }
        }
    }
    $consumed = @(Get-HarnessLogEvents)
    foreach ($file in (Get-ChildItem (Join-Path $Root 'scripts') -Recurse -Filter *.ps1)) {
        $tokens=$null; $errors=$null
        $ast = [Management.Automation.Language.Parser]::ParseFile($file.FullName,[ref]$tokens,[ref]$errors)
        if ($errors.Count) { throw "PowerShell parse failed: $($file.FullName)" }
        foreach ($cmd in $ast.FindAll({ param($n) $n -is [Management.Automation.Language.CommandAst] -and $n.GetCommandName() -in @('Log-Has','Wait-Log') },$true)) {
            if ($cmd.CommandElements.Count -gt 1 -and $cmd.CommandElements[1] -is [Management.Automation.Language.StringConstantExpressionAst]) {
                foreach ($match in [regex]::Matches($cmd.CommandElements[1].Value,'(?:^|\|)([a-z][a-z0-9_]+)')) { $consumed += $match.Groups[1].Value }
            }
        }
        # Log-line regex consumers (timings and ordered outcomes) not routed through Wait-Log.
        foreach ($node in $ast.FindAll({ param($n) $n -is [Management.Automation.Language.BinaryExpressionAst] -and $n.Operator -in @('Imatch','Inotmatch','Cmatch','Cnotmatch') },$true)) {
            if ($node.Right -is [Management.Automation.Language.StringConstantExpressionAst] -and $node.Right.Value -match '^((?:popup|insert|clipboard|manual|target|paste|tray)_[a-z_]+)') { $consumed += $Matches[1] }
        }
    }
    foreach ($name in @($consumed | Sort-Object -Unique)) {
        [pscustomobject]@{ Event=$name; Emitted=$emitters.ContainsKey($name); Source=$emitters[$name] }
    }
}
