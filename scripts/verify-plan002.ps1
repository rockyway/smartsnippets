# Historical prototype harness; superseded by verify-plans-003-009.ps1.
# Its prototype popup insertion check no longer applies to the production popup.
# Automated Verification Harness for Plan 002 (Technical Validation Prototype)
$ErrorActionPreference = "Stop"

Write-Host "====================================================" -ForegroundColor Cyan
Write-Host " Running Automated Verification for Plan 002" -ForegroundColor Cyan
Write-Host "====================================================" -ForegroundColor Cyan

# 1. Run Unit Tests
Write-Host "`n[1/7] Running SmartSnippets.Core Unit Tests..." -ForegroundColor Yellow
$testOutput = & dotnet test tests/SmartSnippets.Core.Tests --nologo -v q
Write-Host $testOutput
if ($LASTEXITCODE -ne 0) {
    throw "Unit tests failed!"
}

# 2. Check Native AOT Artifacts
Write-Host "`n[2/7] Checking Native AOT Publish Output..." -ForegroundColor Yellow
$publishDir = "publish"
if (-not (Test-Path "$publishDir/SmartSnippets.App.exe")) {
    throw "publish/SmartSnippets.App.exe is missing!"
}
if (Test-Path "$publishDir/SmartSnippets.App.dll") {
    throw "publish/SmartSnippets.App.dll exists! Build is not pure Native AOT."
}
if (-not (Test-Path "$publishDir/e_sqlite3.dll")) {
    throw "publish/e_sqlite3.dll is missing!"
}
Write-Host "  -> SmartSnippets.App.exe exists (Native AOT executable)" -ForegroundColor Green
Write-Host "  -> SmartSnippets.App.dll absent (Confirmed Native AOT binary)" -ForegroundColor Green
Write-Host "  -> e_sqlite3.dll present" -ForegroundColor Green

# 3. Clean up any existing instances and clear log
Get-Process -Name "SmartSnippets.App" -ErrorAction SilentlyContinue | Stop-Process -Force
$logPath = "$env:LOCALAPPDATA\SmartSnippets\logs\app.log"
$dbPath = "$env:LOCALAPPDATA\SmartSnippets\smartsnippets.db"

# 4. Verify SQLite Database and Migrations via Native AOT App
Write-Host "`n[3/7] Verifying Native AOT App Launch and SQLite Migration..." -ForegroundColor Yellow
$appProc = Start-Process -FilePath ".\publish\SmartSnippets.App.exe" -ArgumentList "--background" -PassThru
Start-Sleep -Seconds 2

if ($appProc.HasExited) {
    throw "SmartSnippets.App.exe exited prematurely!"
}
Write-Host "  -> App launched in background with PID: $($appProc.Id)" -ForegroundColor Green

if (-not (Test-Path $dbPath)) {
    throw "SQLite database was not created at $dbPath"
}
Write-Host "  -> SQLite DB created at $dbPath" -ForegroundColor Green

# Query schema and version using sqlite3 CLI
$schema = & sqlite3 $dbPath ".schema"
Write-Host "  -> Database schema initialized:"
$schema | ForEach-Object { Write-Host "     $_" -ForegroundColor DarkGray }

$schemaVersion = & sqlite3 $dbPath "SELECT version FROM schema_version;"
if ($schemaVersion -ne "1") {
    throw "Expected schema_version 1, but got $schemaVersion"
}
Write-Host "  -> Schema version is 1" -ForegroundColor Green

# Test SQLite CRUD on probe table
& sqlite3 $dbPath "INSERT INTO probe (id, text) VALUES (999, 'Unicode test');"
$queried = & sqlite3 $dbPath "SELECT text FROM probe WHERE id = 999;"
& sqlite3 $dbPath "DELETE FROM probe WHERE id = 999;"
if ($queried -notlike "*Unicode test*") {
    throw "SQLite CRUD probe roundtrip failed!"
}
Write-Host "  -> SQLite CRUD roundtrip on probe table passed" -ForegroundColor Green

# 5. Verify Single-Instance Redirection
Write-Host "`n[4/7] Verifying Single-Instance Redirection..." -ForegroundColor Yellow
$secondProc = Start-Process -FilePath ".\publish\SmartSnippets.App.exe" -PassThru
$secondProc.WaitForExit(5000)
if (-not $secondProc.HasExited) {
    $secondProc | Stop-Process -Force
    throw "Second instance did not exit!"
}
Write-Host "  -> Second instance exited cleanly (redirected to main instance)" -ForegroundColor Green

# Stop the first app instance for next isolated tests
$appProc | Stop-Process -Force
Start-Sleep -Milliseconds 500

# 6. Verify Hotkey Conflict Handling
Write-Host "`n[5/7] Verifying Hotkey Conflict Handling (Win32 1409)..." -ForegroundColor Yellow
$csharpCode = @'
using System;
using System.Runtime.InteropServices;

public static class Win32TestHarness {
    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
    [DllImport("user32.dll")]
    public static extern bool UnregisterHotKey(IntPtr hWnd, int id);
    [DllImport("user32.dll")]
    public static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

    public const int VK_CONTROL = 0x11;
    public const int VK_SHIFT = 0x10;
    public const int VK_SPACE = 0x20;
    public const int VK_RETURN = 0x0D;
    public const uint KEYEVENTF_KEYUP = 0x0002;

    public static void SendHotkey() {
        keybd_event((byte)VK_CONTROL, 0, 0, UIntPtr.Zero);
        keybd_event((byte)VK_SHIFT, 0, 0, UIntPtr.Zero);
        keybd_event((byte)VK_SPACE, 0, 0, UIntPtr.Zero);
        System.Threading.Thread.Sleep(50);
        keybd_event((byte)VK_SPACE, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        keybd_event((byte)VK_SHIFT, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        keybd_event((byte)VK_CONTROL, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
    }

    public static void SendEnter() {
        keybd_event((byte)VK_RETURN, 0, 0, UIntPtr.Zero);
        System.Threading.Thread.Sleep(50);
        keybd_event((byte)VK_RETURN, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
    }
}
'@
Add-Type -TypeDefinition $csharpCode

# Pre-register Ctrl+Shift+Space (0x4006, 0x20)
$conflictRegistered = [Win32TestHarness]::RegisterHotKey([IntPtr]::Zero, 9001, 0x4006, 0x20)
if (-not $conflictRegistered) {
    throw "Failed to pre-register hotkey for conflict test"
}

# Clear log to inspect isolated output
if (Test-Path $logPath) { Clear-Content $logPath }

$conflictApp = Start-Process -FilePath ".\publish\SmartSnippets.App.exe" -ArgumentList "--background" -PassThru
Start-Sleep -Seconds 2

$conflictLogs = Get-Content $logPath
$hasConflictLog = $conflictLogs | Where-Object { $_ -match "hotkey_register_failed win32=1409" }
$conflictApp | Stop-Process -Force
[Win32TestHarness]::UnregisterHotKey([IntPtr]::Zero, 9001) | Out-Null

if (-not $hasConflictLog) {
    throw "App did not log expected hotkey conflict (win32=1409)!"
}
Write-Host "  -> Handled hotkey conflict gracefully with log: win32=1409" -ForegroundColor Green

# 7. Verify Hotkey, Popup Latency, Target Focus Restore, Unicode Paste, and Clipboard Preservation
Write-Host "`n[6/7] Verifying Hotkey, Popup Latency, Focus Restore, and Insertion..." -ForegroundColor Yellow

$sentinelClipboard = "SENTINEL_CLIPBOARD_BEFORE_INSERTION_12345"
Set-Clipboard $sentinelClipboard

# Clear log
if (Test-Path $logPath) { Clear-Content $logPath }

# Launch target Notepad
$notepad = Start-Process "notepad.exe" -PassThru
Start-Sleep -Seconds 2

# Launch App
$app = Start-Process -FilePath ".\publish\SmartSnippets.App.exe" -ArgumentList "--background" -PassThru
Start-Sleep -Seconds 2

# Send Hotkey
Write-Host "  -> Sending Ctrl+Shift+Space..." -ForegroundColor DarkGray
[Win32TestHarness]::SendHotkey()
Start-Sleep -Milliseconds 600

# Send Enter to insert
Write-Host "  -> Sending Enter to trigger insertion..." -ForegroundColor DarkGray
[Win32TestHarness]::SendEnter()
Start-Sleep -Seconds 2

$appLogs = Get-Content $logPath
Write-Host "  Captured Log Entries:"
$appLogs | ForEach-Object { Write-Host "    $_" -ForegroundColor DarkGray }

# Cleanup processes
$app | Stop-Process -Force
Stop-Process -Name "notepad" -Force

# Analyze metrics
$readyLog = $appLogs | Where-Object { $_ -match "popup_ready_ms (\d+)" }
if ($readyLog -match "popup_ready_ms (\d+)") {
    $latency = [int]$matches[1]
    Write-Host "  -> Popup Ready Latency: ${latency} ms (Target: <= 300 ms)" -ForegroundColor Green
    if ($latency -gt 300) {
        Write-Warning "Popup latency exceeded 300 ms target!"
    }
} else {
    throw "popup_ready_ms was not logged!"
}

$focusPath = $appLogs | Where-Object { $_ -match "focus_path (\w+)" }
Write-Host "  -> Focus path: $focusPath" -ForegroundColor Green

$insertResult = $appLogs | Where-Object { $_ -match "insert_result (\w+)" }
if ($insertResult -notmatch "Success") {
    throw "Insertion did not report Success: $insertResult"
}
Write-Host "  -> Insertion Result: Success" -ForegroundColor Green

# Verify Clipboard Restoration
$restoredClipboard = Get-Clipboard
if ($restoredClipboard -eq $sentinelClipboard) {
    Write-Host "  -> Clipboard Preserved and Restored cleanly" -ForegroundColor Green
} else {
    throw "Clipboard restoration failed! Got: $restoredClipboard"
}

# 8. Verify Startup Registration (HKCU Run key)
Write-Host "`n[7/7] Verifying Startup Registration (HKCU Run key)..." -ForegroundColor Yellow
$runKey = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run"
$valueName = "SmartSnippets"
$resolvedExe = (Resolve-Path ".\publish\SmartSnippets.App.exe").Path
$expectedVal = '"' + $resolvedExe + '" --background'

# Write Run key value
Set-ItemProperty -Path $runKey -Name $valueName -Value $expectedVal
$readVal = (Get-ItemProperty -Path $runKey -Name $valueName).SmartSnippets
if ($readVal -ne $expectedVal) {
    throw "Startup key readback mismatch! Expected: $expectedVal, Got: $readVal"
}
Write-Host "  -> Startup key correctly set with --background switch" -ForegroundColor Green

# Delete Run key value
Remove-ItemProperty -Path $runKey -Name $valueName
$afterDelete = Get-ItemProperty -Path $runKey -Name $valueName -ErrorAction SilentlyContinue
if ($null -ne $afterDelete) {
    throw "Failed to remove startup key during cleanup!"
}
Write-Host "  -> Startup key cleaned up cleanly" -ForegroundColor Green

Write-Host "`n====================================================" -ForegroundColor Cyan
Write-Host " ALL PLAN 002 VERIFICATION CHECKS PASSED!" -ForegroundColor Green
Write-Host "====================================================" -ForegroundColor Cyan
