import json
import os
import subprocess
import tempfile
import unittest
from pathlib import Path
from unittest.mock import patch
from test_uno_candidate_ui import s


class PickerSchemaTests(unittest.TestCase):
    def test_fixed_schema(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            path = root / 'ui-helper-picker-schema.json'
            self.assertFalse(s.validate_helper_picker_schema(root))
            for reason, count, kind in [('picker_schema_missing', 0, 0),
                                        ('picker_schema_multiple', 2, 0),
                                        ('picker_schema_type', 1, 50001),
                                        ('picker_schema_type', 1, 50040)]:
                record = dict(reason=reason, matchCount=count, controlTypeId=kind)
                path.write_text(json.dumps(record), encoding='ascii')
                self.assertTrue(s.validate_helper_picker_schema(root))

    def test_rejects_every_consistency_and_type_rule(self):
        valid = dict(reason='picker_schema_missing', matchCount=0, controlTypeId=0)
        invalid = [[], None, 1, 'text', {},
                   {**valid, 'extra': 0},
                   {key: value for key, value in valid.items() if key != 'reason'},
                   {key: value for key, value in valid.items() if key != 'matchCount'},
                   {key: value for key, value in valid.items() if key != 'controlTypeId'}]
        triples = [('picker_schema_missing', 0, 50001),
                   ('picker_schema_missing', 1, 0),
                   ('picker_schema_missing', 2, 0),
                   ('picker_schema_multiple', 0, 0),
                   ('picker_schema_multiple', 1, 0),
                   ('picker_schema_multiple', 2, 50000),
                   ('picker_schema_type', 0, 50001),
                   ('picker_schema_type', 2, 50001),
                   ('picker_schema_type', 1, 0),
                   ('picker_schema_type', 1, 49999),
                   ('picker_schema_type', 1, 50000),
                   ('picker_schema_type', 1, 50041),
                   ('other', 0, 0), ('picker_schema_missing', -1, 0)]
        invalid += [dict(reason=r, matchCount=c, controlTypeId=k) for r, c, k in triples]
        for key in ('matchCount', 'controlTypeId'):
            for value in (False, True, 0.0, '0'):
                invalid.append({**valid, key: value})
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            path = root / 'ui-helper-picker-schema.json'
            for record in invalid:
                with self.subTest(record=record):
                    path.write_text(json.dumps(record), encoding='ascii')
                    with self.assertRaisesRegex(ValueError, '^ui_picker_schema_record$'):
                        s.validate_helper_picker_schema(root)
            for raw in (b'{', b'\xff', b'{"reason":"\xc3\xa9"}',
                        b'{"reason":"picker_schema_missing","matchCount":0,"controlTypeId":0,"matchCount":0}'):
                with self.subTest(raw=raw):
                    path.write_bytes(raw)
                    with self.assertRaisesRegex(ValueError, '^ui_picker_schema_record$'):
                        s.validate_helper_picker_schema(root)

    def test_owned_file_guards(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            path = root / 'ui-helper-picker-schema.json'
            path.write_bytes(b' ' * 4097)
            with self.assertRaisesRegex(ValueError, '^evidence_file$'):
                s.validate_helper_picker_schema(root)
            path.unlink()
            target = root / 'target'
            target.write_bytes(b'{}')
            # Hard links work without symlink privilege on Windows and exercise owned_bytes.
            os.link(target, path)
            with self.assertRaisesRegex(ValueError, '^evidence_file$'):
                s.validate_helper_picker_schema(root)
            path.unlink()
            # Model the filesystem's symlink observation without requiring developer mode.
            path.write_bytes(b'{}')
            original = Path.is_symlink
            with patch.object(Path, 'is_symlink', lambda p: p == path or original(p)):
                with self.assertRaisesRegex(ValueError, '^evidence_alias$'):
                    s.validate_helper_picker_schema(root)

    def test_foreground_validator_and_failure_allowlist_are_wired(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            (root / 'ui-helper-picker-schema.json').write_bytes(b'{}')
            with self.assertRaisesRegex(ValueError, '^ui_picker_schema_record$'):
                s.validate_helper_foreground_failure(root)
            with patch.object(s.sys, 'argv', ['worker', '--worker-private']):
                s.base.record_failure(root, ValueError('ui_picker_schema_record'))
            self.assertEqual({'reason': 'ui_picker_schema_record'},
                             json.loads((root / 'observer-worker-failure.json').read_text()))

    def test_record_must_agree_with_helper_outcome_and_stderr(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            (root / 'ui-helper-picker-schema.json').write_text(json.dumps(
                dict(reason='picker_schema_missing', matchCount=0, controlTypeId=0)), encoding='ascii')
            for code, err in [(0, b''), (1, b''),
                              (1, b'ui_helper_failed stage=picker_import_cancel reason=picker_schema_type\n'),
                              (1, b'ui_helper_failed stage=picker_import_cancel reason=picker_schema_missing\nextra')]:
                with self.subTest(code=code, err=err):
                    with self.assertRaisesRegex(ValueError, '^ui_picker_schema_record$'):
                        s.validate_helper_foreground_failure(root, code, err)
            self.assertFalse(s.validate_helper_foreground_failure(root, 1,
                b'ui_helper_failed stage=picker_import_cancel reason=picker_schema_missing\r\n'))

    @unittest.skipUnless(s.sys.platform == 'win32', 'PowerShell stub execution')
    def test_actual_poll_waits_and_classifies(self):
        helper = s.HELPER.read_text()
        block = helper.split('    function PickerElement', 1)[1].split('        do {', 1)[1].split('        $e = $items[0];', 1)[0]
        setup = r'''
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
        # A separate owned directory per execution retains every real emitted record for Python.
        for count, kind, reason in [(0, 0, 'picker_schema_missing'),
                                   (2, 50000, 'picker_schema_multiple'),
                                   (2, 0, 'picker_schema_multiple'),
                                   (1, 50004, 'picker_schema_type'), (1, 50000, '')]:
            with self.subTest(count=count, kind=kind), tempfile.TemporaryDirectory() as tmp:
                root = Path(tmp)
                script = setup + "\n$root='" + tmp.replace("'", "''") + "'\n"
                script += f"$script:matches=@(); 1..{max(1,count)} | ForEach-Object {{ if({count} -gt 0){{$script:matches+= [pscustomobject]@{{Current=[pscustomobject]@{{ControlType=[pscustomobject]@{{Id={kind}}}}}}}}} }}\n"
                script += "$script:delayed=$false\n"
                if not reason:
                    script += "$script:delayed=$true; $script:readyMatches=$script:matches; $script:matches=@()\n"
                script += "$script:poll=0; $script:pickerStepUntil=[DateTime]::UtcNow.AddSeconds(6); $caught=''\ntry { do {" + block + "\n} catch { $caught=$_.Exception.Message }\n"
                script += f"if($caught -cne '{reason}'){{throw ('reason:'+ $caught)}}\n"
                script += "if($script:poll -lt 3){throw 'did_not_wait'}\n" if reason else "if($script:poll -ne 2){throw 'did_not_wait_for_tree'}\n"
                result = subprocess.run([str(s.system_powershell()), '-NoProfile', '-NonInteractive', '-Command', script], capture_output=True, timeout=20, creationflags=subprocess.CREATE_NO_WINDOW)
                self.assertEqual((0, b'', b''), (result.returncode, result.stdout, result.stderr))
                path = root / 'ui-helper-picker-schema.json'
                if reason:
                    self.assertEqual(dict(reason=reason, matchCount=count,
                                          controlTypeId=kind if count == 1 else 0),
                                     json.loads(path.read_bytes().decode('ascii')))
                    self.assertTrue(s.validate_helper_picker_schema(root))
                else:
                    self.assertFalse(path.exists())
                    self.assertFalse(s.validate_helper_picker_schema(root))
