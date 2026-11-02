"""Execute the real PowerShell measurement function; no UIA/native calls."""
import json
from pathlib import Path
import subprocess
import tempfile
import unittest
from test_uno_candidate_ui import s


class PickerInvokeEpochTests(unittest.TestCase):
    def test_real_helper_measures_single_invoke_even_on_exception_without_rebaseline(self):
        with tempfile.TemporaryDirectory() as directory:
            script = r'''
$ErrorActionPreference='Stop'
Add-Type 'public static class Owned019 { public static uint Time=100; public static uint Epoch(){return Time;} }'
$t=$null; $e=$null
$a=[Management.Automation.Language.Parser]::ParseFile('HELPER',[ref]$t,[ref]$e)
if($e.Count){throw 'parse'}
$f=$a.FindAll({param($n) $n -is [Management.Automation.Language.FunctionDefinitionAst] -and $n.Name -ceq 'Invoke-PickerMeasured'},$true)
if($f.Count -ne 1){throw 'measurement_missing'}
Invoke-Expression $f[0].Extent.Text
$root='ROOT'; $script:pickerPhase='import-cancel'; $epoch=100; $script:calls=0
$pattern=New-Object PSObject
$pattern | Add-Member ScriptMethod Invoke { $script:calls++; [Owned019]::Time=200 }
Invoke-PickerMeasured $pattern 'open'
if($script:calls -ne 1 -or $epoch -ne 100){throw 'single_or_rebaseline'}
$pattern=New-Object PSObject
$pattern | Add-Member ScriptMethod Invoke { $script:calls++; [Owned019]::Time=300; throw 'provider' }
$failed=$false
try { Invoke-PickerMeasured $pattern 'commit' } catch { $failed=$true }
if(-not $failed -or $script:calls -ne 2 -or $epoch -ne 100){throw 'failure_or_rebaseline'}
$failed=$false
try { Invoke-PickerMeasured $pattern 'private title' } catch { $failed=$true }
if(-not $failed -or $script:calls -ne 2){throw 'token'}
'''.replace('HELPER', str(s.HELPER).replace("'", "''")).replace('ROOT', directory.replace("'", "''"))
            result = subprocess.run([str(s.system_powershell()), '-NoProfile', '-NonInteractive', '-Command', script],
                                    capture_output=True, timeout=25, creationflags=subprocess.CREATE_NO_WINDOW)
            self.assertEqual((0, b'', b''), (result.returncode, result.stdout, result.stderr))
            files = sorted(Path(directory).glob('*.json'))
            self.assertEqual(2, len(files))
            self.assertEqual({'before': 100, 'after': 200, 'completed': True},
                json.loads((Path(directory) / 'picker-invoke-import-cancel-open.json').read_text()))
            self.assertEqual({'before': 200, 'after': 300, 'completed': False},
                json.loads((Path(directory) / 'picker-invoke-import-cancel-commit.json').read_text()))

    def test_measurement_is_wired_to_open_and_commit_sites_source_supplement(self):
        helper = s.HELPER.read_text()
        self.assertIn("Invoke-PickerMeasured $invoke 'commit'", helper)
        self.assertIn("if ($key -cin @('picker-import','picker-export')) { Invoke-PickerMeasured $pattern 'open' }", helper)
