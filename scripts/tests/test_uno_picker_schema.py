import json
import subprocess
import tempfile
import unittest
from pathlib import Path
from test_uno_candidate_ui import s


class PickerSchemaTests(unittest.TestCase):
    def test_fixed_schema(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            path = root / 'ui-helper-picker-schema.json'
            for reason, count, kind in [('picker_schema_missing', 0, 0),
                                        ('picker_schema_multiple', 2, 0),
                                        ('picker_schema_type', 1, 50004)]:
                record = dict(reason=reason, matchCount=count, controlTypeId=kind)
                path.write_text(json.dumps(record), encoding='ascii')
                self.assertTrue(s.validate_helper_picker_schema(root))
                record['extra'] = 'secret'
                path.write_text(json.dumps(record), encoding='ascii')
                with self.assertRaises(ValueError):
                    s.validate_helper_picker_schema(root)

    @unittest.skipUnless(s.sys.platform == 'win32', 'PowerShell stub execution')
    def test_actual_poll_waits_and_classifies(self):
        helper = s.HELPER.read_text()
        block = helper.split('    function PickerElement', 1)[1].split('        do {', 1)[1].split('        $e = $items[0];', 1)[0]
        script = r'''
$ErrorActionPreference='Stop'
Add-Type @'
namespace Windows.Automation { public enum TreeScope { Descendants } }
'@
function Guard {}
function OwnedPicker { return 20 }
function Start-Sleep { $script:poll++; if($script:delayed -and $script:poll -eq 2){$script:matches=$script:readyMatches}; if($script:poll -gt 2){$script:pickerStepUntil=[DateTime]::UtcNow.AddSeconds(-1)} }
$scope=New-Object PSObject
$scope | Add-Member ScriptMethod FindAll { param($a,$b) return ,$script:matches }
$dialog=20; $button=$true; $condition=$null
'''
        with tempfile.TemporaryDirectory() as tmp:
            script += "\n$root='" + tmp.replace("'", "''") + "'\n"
            for count, kind, reason in [(0, 0, 'picker_schema_missing'), (2, 50000, 'picker_schema_multiple'), (1, 50004, 'picker_schema_type'), (1, 50000, '')]:
                script += "\nRemove-Item -LiteralPath (Join-Path $root 'ui-helper-picker-schema.json') -ErrorAction SilentlyContinue\n"
                script += f"\n$script:matches=@(); 1..{max(1,count)} | ForEach-Object {{ if({count} -gt 0){{$script:matches+= [pscustomobject]@{{Current=[pscustomobject]@{{ControlType=[pscustomobject]@{{Id={kind}}}}}}}}} }}\n"
                script += "$script:delayed=$false\n"
                if not reason:
                    script += "$script:delayed=$true; $script:readyMatches=$script:matches; $script:matches=@()\n"
                script += "$script:poll=0; $script:pickerStepUntil=[DateTime]::UtcNow.AddSeconds(6); $caught=''\ntry { do {" + block + "\n} catch { $caught=$_.Exception.Message }\n"
                script += f"if($caught -cne '{reason}'){{throw ('reason:'+ $caught)}}\n"
                if reason:
                    script += "if($script:poll -lt 3){throw 'did_not_wait'}\n"
                else:
                    script += "if($script:poll -ne 2){throw 'did_not_wait_for_tree'}\n"
            result = subprocess.run([str(s.system_powershell()), '-NoProfile', '-NonInteractive', '-Command', script], capture_output=True, timeout=20, creationflags=subprocess.CREATE_NO_WINDOW)
            self.assertEqual((0, b'', b''), (result.returncode, result.stdout, result.stderr))
