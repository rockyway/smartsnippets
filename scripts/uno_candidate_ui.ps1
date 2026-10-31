param([ValidateSet('snippets','folders','settings')][string]$Scene = 'snippets',
      [Parameter(Mandatory=$true)][int]$OwnedPid,
      [Parameter(Mandatory=$true)][long]$OwnedHwnd,
      [Parameter(Mandatory=$true)][long]$InitialForeground,
      [Parameter(Mandatory=$true)][uint32]$InputEpoch,
      [Parameter(Mandatory=$true)][string]$PrivateRoot,
      [Parameter(Mandatory=$true)][string]$Artifact)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$stage = 'bootstrap'
try {
    if ([Threading.Thread]::CurrentThread.ApartmentState -ne 'STA') { throw 'sta' }
    [void][Reflection.Assembly]::Load('UIAutomationClient, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35')
    [void][Reflection.Assembly]::Load('UIAutomationTypes, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35')
    Add-Type -TypeDefinition @'
using System;
using System.Text;
using System.Runtime.InteropServices;
public static class Owned019 {
 [DllImport("user32.dll")] public static extern bool IsWindow(IntPtr h);
 [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr h);
 [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h, out uint p);
 [DllImport("user32.dll", CharSet=CharSet.Unicode)] public static extern int GetClassName(IntPtr h, StringBuilder b, int n);
 [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
 [DllImport("user32.dll")] public static extern short GetAsyncKeyState(int k);
 [DllImport("user32.dll")] public static extern uint GetClipboardSequenceNumber();
 [DllImport("user32.dll")] public static extern IntPtr OpenInputDesktop(uint f, bool i, uint a);
 [DllImport("user32.dll", CharSet=CharSet.Unicode)] public static extern bool GetUserObjectInformation(IntPtr h, int i, StringBuilder b, uint n, out uint needed);
 [DllImport("user32.dll")] public static extern bool CloseDesktop(IntPtr h);
 [StructLayout(LayoutKind.Sequential)] public struct Input { public uint size; public uint time; }
 [DllImport("user32.dll")] public static extern bool GetLastInputInfo(ref Input input);
 public static uint Epoch() { var i=new Input {size=8}; if(!GetLastInputInfo(ref i)) throw new Exception(); return i.time; }
}
'@
    $root = [IO.Path]::GetFullPath($PrivateRoot)
    if ($root -cne [Environment]::CurrentDirectory -or [IO.Path]::GetFileName($root) -cnotmatch '^ss-uno018-ui-[a-zA-Z0-9_-]{8,80}$') { throw 'root' }
    $h = [IntPtr]$OwnedHwnd
    $epoch = $InputEpoch
    if ([Owned019]::Epoch() -ne $epoch) { throw 'input' }
    $focused = $false
    $script:invokes = 0; $script:values = 0; $script:focuses = 0
    $script:scrolls = 0; $script:selections = 0
    $clipboardSequence = [Owned019]::GetClipboardSequenceNumber()
    if ($clipboardSequence -eq 0) { throw 'clipboard' }
    $ownedProcess = [Diagnostics.Process]::GetProcessById($OwnedPid)
    try { $started = $ownedProcess.StartTime.ToUniversalTime().Ticks } finally { $ownedProcess.Dispose() }
    function Guard {
        if ([Owned019]::GetClipboardSequenceNumber() -ne $clipboardSequence) { throw 'clipboard' }
        if (-not [Owned019]::IsWindow($h) -or -not [Owned019]::IsWindowVisible($h)) { throw 'window' }
        [uint32]$owner = 0
        [void][Owned019]::GetWindowThreadProcessId($h, [ref]$owner)
        $b = New-Object Text.StringBuilder 256
        [void][Owned019]::GetClassName($h, $b, 256)
        if ($owner -ne $OwnedPid -or $b.ToString() -cne 'WinUIDesktopWin32WindowClass') { throw 'identity' }
        $p = [Diagnostics.Process]::GetProcessById($OwnedPid)
        try {
            if ($p.MainModule.FileName -cne $Artifact -or $p.StartTime.ToUniversalTime().Ticks -ne $started) { throw 'artifact' }
        } finally { $p.Dispose() }
        $d = [Owned019]::OpenInputDesktop(0, $false, 1)
        if ($d -eq [IntPtr]::Zero) { throw 'desktop' }
        try {
            $name = New-Object Text.StringBuilder 256
            [uint32]$needed = 0
            if (-not [Owned019]::GetUserObjectInformation($d, 2, $name, 512, [ref]$needed) -or $name.ToString() -cne 'Default') { throw 'desktop' }
        } finally { if (-not [Owned019]::CloseDesktop($d)) { throw 'desktop_close' } }
        if ([Owned019]::Epoch() -ne $epoch -or ([Environment]::TickCount -band 0xffffffffL) - $epoch -lt 1500) { throw 'input' }
        for ($k=1; $k -lt 256; $k++) { if (([int][Owned019]::GetAsyncKeyState($k) -band 0x8000) -ne 0) { throw 'input' } }
        $fg = [Owned019]::GetForegroundWindow().ToInt64()
        if (($script:focused -and $fg -ne $OwnedHwnd) -or (-not $script:focused -and $fg -ne $InitialForeground -and $fg -ne $OwnedHwnd)) { throw 'foreground' }
    }
    function SelectOwnedLeave($items) {
        $seen = @{}
        $visible = @()
        foreach ($item in $items) {
            if ($item.Current.ProcessId -ne $OwnedPid) { throw 'uia_owner' }
            $itemName = $item.Current.Name
            if ($itemName -cnotin @('New snippet','Back to list') -or $seen.ContainsKey($itemName)) { throw 'schema' }
            $seen[$itemName] = $true
            if (-not $item.Current.IsOffscreen) { $visible += $item }
        }
        if ($visible.Count -gt 1) { throw 'schema' }
        if ($visible.Count -eq 1 -and $visible[0].Current.IsEnabled) { return $visible[0] }
    }
    function Find([string]$key) {
        Guard
        $r = [Windows.Automation.AutomationElement]::FromHandle($h)
        if ($r.Current.ProcessId -ne $OwnedPid) { throw 'uia_owner' }
        switch -CaseSensitive ($key) {
            'folder-new-name' { $name='New folder name'; $id=$null; $type=[Windows.Automation.ControlType]::Edit }
            'folder-create' { $name='Create folder'; $id=$null; $type=[Windows.Automation.ControlType]::Button }
            'folder-rename' { $name='Rename folder'; $id=$null; $type=[Windows.Automation.ControlType]::Button }
            'folder-name' { $name='Folder name'; $id='RenameBox'; $type=[Windows.Automation.ControlType]::Edit }
            'folder-save' { $name='Save folder name'; $id=$null; $type=[Windows.Automation.ControlType]::Button }
            'folder-delete' { $name='Delete folder'; $id=$null; $type=[Windows.Automation.ControlType]::Button }
            'folder-confirm' { $name='Delete folder'; $id='PrimaryButton'; $type=[Windows.Automation.ControlType]::Button }
            'new' { $name='New snippet'; $id=$null; $type=[Windows.Automation.ControlType]::Button }
            'text' { $name='Snippet text'; $id='SnippetTextBox'; $type=[Windows.Automation.ControlType]::Edit }
            'save' { $name='Save snippet'; $id=$null; $type=[Windows.Automation.ControlType]::Button }
            'cancel' { $name='Keep editing'; $id=$null; $type=[Windows.Automation.ControlType]::Button }
            'leave' { $name=$null; $id=$null; $type=[Windows.Automation.ControlType]::Button }
            default { throw 'schema' }
        }
        $nameCondition = if ($key -ceq 'leave') {
            # Responsive page: exactly one of toolbar New (wide) or editor Back (narrow) is onscreen.
            [Windows.Automation.OrCondition]::new(
                [Windows.Automation.PropertyCondition]::new([Windows.Automation.AutomationElement]::NameProperty,'New snippet'),
                [Windows.Automation.PropertyCondition]::new([Windows.Automation.AutomationElement]::NameProperty,'Back to list'))
        } else { [Windows.Automation.PropertyCondition]::new([Windows.Automation.AutomationElement]::NameProperty,$name) }
        $conditions = @($nameCondition,
            [Windows.Automation.PropertyCondition]::new([Windows.Automation.AutomationElement]::ControlTypeProperty,$type))
        if ($id) { $conditions += [Windows.Automation.PropertyCondition]::new([Windows.Automation.AutomationElement]::AutomationIdProperty,$id) }
        $condition = [Windows.Automation.AndCondition]::new([Windows.Automation.Condition[]]$conditions)
        $until = [DateTime]::UtcNow.AddSeconds(4)
        while ([DateTime]::UtcNow -lt $until) {
            Guard
            $r = [Windows.Automation.AutomationElement]::FromHandle($h)
            if ($r.Current.ProcessId -ne $OwnedPid) { throw 'uia_owner' }
            $items = $r.FindAll([Windows.Automation.TreeScope]::Descendants,$condition)
            if ($key -ceq 'leave') {
                $selected = SelectOwnedLeave $items
                if ($null -ne $selected) { Guard; return $selected }
            } else {
                if ($Scene -ceq 'snippets' -and $items.Count -gt 1) { throw 'schema' }
                $eligible = @()
                foreach ($item in $items) {
                    if ($item.Current.ProcessId -ne $OwnedPid) { throw 'uia_owner' }
                    if (-not $item.Current.IsOffscreen -and $item.Current.IsEnabled) { $eligible += $item }
                }
                if ($eligible.Count -gt 1) { throw 'schema' }
                if ($eligible.Count -eq 1) { Guard; return $eligible[0] }
            }
            Start-Sleep -Milliseconds 50
        }
        throw 'control_timeout'
    }
    function FindSettingsRadio([string]$key, [bool]$requireVisible) {
        # Only these two fixed group/radio identities. No global or fallback discovery.
        switch -CaseSensitive ($key) {
            'settings-dark' { $groupId='ThemeRadios'; $groupName='Theme'; $radioName='Dark' }
            'settings-large' { $groupId='TextSizeRadios'; $groupName='Text size'; $radioName='Large' }
            default { throw 'schema' }
        }
        $groupCondition = [Windows.Automation.AndCondition]::new([Windows.Automation.Condition[]]@(
            [Windows.Automation.PropertyCondition]::new([Windows.Automation.AutomationElement]::AutomationIdProperty,$groupId),
            [Windows.Automation.PropertyCondition]::new([Windows.Automation.AutomationElement]::NameProperty,$groupName),
            [Windows.Automation.PropertyCondition]::new([Windows.Automation.AutomationElement]::ControlTypeProperty,[Windows.Automation.ControlType]::Group)))
        $radioCondition = [Windows.Automation.AndCondition]::new([Windows.Automation.Condition[]]@(
            [Windows.Automation.PropertyCondition]::new([Windows.Automation.AutomationElement]::NameProperty,$radioName),
            [Windows.Automation.PropertyCondition]::new([Windows.Automation.AutomationElement]::ControlTypeProperty,[Windows.Automation.ControlType]::RadioButton)))
        $until = [DateTime]::UtcNow.AddSeconds(4)
        while ([DateTime]::UtcNow -lt $until) {
            Guard
            $r = [Windows.Automation.AutomationElement]::FromHandle($h)
            if ($r.Current.ProcessId -ne $OwnedPid) { throw 'uia_owner' }
            $groups = $r.FindAll([Windows.Automation.TreeScope]::Descendants,$groupCondition)
            if ($groups.Count -gt 1) { throw 'schema' }
            if ($groups.Count -eq 1) {
                if ($groups[0].Current.ProcessId -ne $OwnedPid) { throw 'uia_owner' }
                $items = $groups[0].FindAll([Windows.Automation.TreeScope]::Descendants,$radioCondition)
                if ($items.Count -gt 1) { throw 'schema' }
                if ($items.Count -eq 1) {
                    $e = $items[0]
                    if ($e.Current.ProcessId -ne $OwnedPid) { throw 'uia_owner' }
                    if ($e.Current.IsEnabled -and (-not $requireVisible -or -not $e.Current.IsOffscreen)) {
                        Guard; return $e
                    }
                }
            }
            Start-Sleep -Milliseconds 50
        }
        throw 'control_timeout'
    }
    function SelectSettingsRadio([string]$key) {
        $e = FindSettingsRadio $key $false
        $scroll = [Windows.Automation.ScrollItemPattern]$e.GetCurrentPattern([Windows.Automation.ScrollItemPattern]::Pattern)
        Guard
        # One UI scroll only, not a settings mutation. Missing pattern/refusal is BLOCKED.
        $scroll.ScrollIntoView()
        $script:scrolls++
        Guard
        $e = FindSettingsRadio $key $true
        $selection = [Windows.Automation.SelectionItemPattern]$e.GetCurrentPattern([Windows.Automation.SelectionItemPattern]::Pattern)
        Guard
        if ($e.Current.IsOffscreen -or -not $e.Current.IsEnabled -or $selection.Current.IsSelected) { throw 'selection' }
        $selection.Select() # Exactly once; no mutation retries or focus/input fallback.
        $script:selections++
        Guard
        Start-Sleep -Milliseconds 400
        $e = FindSettingsRadio $key $true
        Guard
        if (-not ([Windows.Automation.SelectionItemPattern]$e.GetCurrentPattern([Windows.Automation.SelectionItemPattern]::Pattern)).Current.IsSelected) { throw 'selection' }
    }
    function Invoke([string]$key) {
        $e = Find $key
        Guard
        ([Windows.Automation.InvokePattern]$e.GetCurrentPattern([Windows.Automation.InvokePattern]::Pattern)).Invoke()
        $script:invokes++
    }
    function Value([string]$value, [string]$key = 'text') {
        $e = Find $key
        Guard
        $e.SetFocus() # Explicitly authorized management activation; not nonactivation QA.
        $script:focused = $true
        $script:focuses++
        Guard
        if (-not $e.Current.HasKeyboardFocus) { throw 'focus' }
        ([Windows.Automation.ValuePattern]$e.GetCurrentPattern([Windows.Automation.ValuePattern]::Pattern)).SetValue($value)
        $script:values++
        Guard
        if (([Windows.Automation.ValuePattern]$e.GetCurrentPattern([Windows.Automation.ValuePattern]::Pattern)).Current.Value -cne $value) { throw 'value' }
    }
    function WaitPhase([string]$phase) {
        $path = Join-Path $root ($phase+'.receipt')
        $until = [DateTime]::UtcNow.AddSeconds(8)
        while (-not [IO.File]::Exists($path)) { Guard; if ([DateTime]::UtcNow -gt $until) { throw 'timeout' }; Start-Sleep -Milliseconds 50 }
        Guard
        $info = Get-Item -LiteralPath $path
        if (($info.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0 -or $info.Length -gt 256) { throw 'receipt' }
        $lines = [IO.File]::ReadAllLines($path)
        if ($lines.Count -ne 3 -or $lines[0] -cne [string]$OwnedPid -or $lines[1] -cne [IO.Path]::GetFileName($root) -or $lines[2] -cne $phase) { throw 'receipt' }
    }
    function Ack([string]$phase) {
        Guard
        $file = [IO.File]::Open((Join-Path $root ($phase+'.continue')), [IO.FileMode]::CreateNew)
        try { $bytes=[Text.Encoding]::ASCII.GetBytes("continue`n"); $file.Write($bytes,0,$bytes.Length) } finally { $file.Dispose() }
    }
    $stage = 'ready'; WaitPhase 'ui-ready'
    if ($Scene -ceq 'settings') {
        $stage = 'settings_theme'
        SelectSettingsRadio 'settings-dark'; Ack 'ui-ready'
        WaitPhase 'ui-settings-theme'
        $stage = 'settings_size'
        SelectSettingsRadio 'settings-large'; Ack 'ui-settings-theme'
        WaitPhase 'ui-settings-size'
        # Read-only repository reload is a separate fixture checkpoint, not a UI mutation.
        Ack 'ui-settings-size'; WaitPhase 'ui-settings-reloaded'; Guard
        if ($script:scrolls -ne 2 -or $script:selections -ne 2 -or $script:invokes -ne 0 -or $script:values -ne 0 -or $script:focuses -ne 0) { throw 'actions' }
        $file = [IO.File]::Open((Join-Path $root 'ui-settings-actions.receipt'), [IO.FileMode]::CreateNew)
        try {
            $bytes = [Text.Encoding]::ASCII.GetBytes("scroll=$script:scrolls`nselect=$script:selections`n")
            $file.Write($bytes,0,$bytes.Length)
        } finally { $file.Dispose() }
    } elseif ($Scene -ceq 'folders') {
        # Fixed scene only: no selectors, names or mutation payloads supplied by the caller.
        $stage = 'folder_create'
        Value 'ui019 synthetic folder' 'folder-new-name'
        Invoke 'folder-create'; Start-Sleep -Milliseconds 400; Ack 'ui-ready'
        WaitPhase 'ui-folder-created'
        $stage = 'folder_rename'
        Invoke 'folder-rename'
        Value 'ui019 synthetic renamed' 'folder-name'
        Invoke 'folder-save'; Start-Sleep -Milliseconds 400; Ack 'ui-folder-created'
        WaitPhase 'ui-folder-renamed'
        $stage = 'folder_delete'
        Invoke 'folder-delete'
        Invoke 'folder-confirm'; Start-Sleep -Milliseconds 400; Ack 'ui-folder-renamed'
        WaitPhase 'ui-folder-deleted'; Guard
        if ($script:invokes -ne 5 -or $script:values -ne 2 -or $script:focuses -ne 2) { throw 'actions' }
        $file = [IO.File]::Open((Join-Path $root 'ui-folder-actions.receipt'), [IO.FileMode]::CreateNew)
        try {
            $bytes = [Text.Encoding]::ASCII.GetBytes("invoke=$script:invokes`nvalue=$script:values`nfocus=$script:focuses`n")
            $file.Write($bytes,0,$bytes.Length)
        } finally { $file.Dispose() }
    } else {
        # Focus only this retained owned window before invoking callbacks.
        $e=Find 'new'; Guard; $e.SetFocus(); $script:focused=$true; Guard
        $stage = 'new'; Invoke 'new'; Start-Sleep -Milliseconds 400; Ack 'ui-ready'
        WaitPhase 'ui-new'
        $stage = 'edit_saved'; Value 'ui019 synthetic saved'
        $stage = 'save'; Invoke 'save'; Start-Sleep -Milliseconds 700; Ack 'ui-new'
        WaitPhase 'ui-saved'
        $stage = 'edit_dirty'; Value 'ui019 synthetic dirty'
        $stage = 'dirty_leave'
        $leave = Find 'leave'
        $backToList = $leave.Current.Name -ceq 'Back to list'
        Guard
        ([Windows.Automation.InvokePattern]$leave.GetCurrentPattern([Windows.Automation.InvokePattern]::Pattern)).Invoke()
        # Back changes only the narrow presentation, retaining the dirty editor. New then runs its real guard.
        if ($backToList) { Invoke 'new' }
        Start-Sleep -Milliseconds 400
        $stage = 'cancel'; Invoke 'cancel'; Start-Sleep -Milliseconds 400; Ack 'ui-saved'
        # The observer acknowledges final shutdown only after this helper exits successfully.
        $stage = 'finish'; WaitPhase 'ui-cancel'; Guard
    }
    [Console]::Out.WriteLine('ui_callbacks_observed')
} catch {
    $reason = 'operation'
    $allowed = @('sta','root','input','window','identity','artifact','desktop','desktop_close','foreground','uia_owner','schema','focus','value','timeout','receipt','control_timeout','actions','clipboard','selection')
    if ($_.Exception.Message -cin $allowed) { $reason = $_.Exception.Message }
    [Console]::Error.WriteLine("ui_helper_failed stage=$stage reason=$reason")
    exit 1
}
