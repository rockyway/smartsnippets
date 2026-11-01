import json
import os
import subprocess
import tempfile
import unittest
from pathlib import Path
from unittest.mock import patch
from test_uno_candidate_ui import s


class PickerSchemaTests(unittest.TestCase):
    def test_helper_failure_tokens_are_strict_and_unknown_classes_collapse(self):
        line = (b'ui_helper_failed stage=picker_import_cancel reason=operation '
                b'operationClass=com pickerStep=cancel_invoke hresult=0x80004005\r\n')
        self.assertEqual({'stage': 'picker_import_cancel', 'reason': 'operation',
                          'operationClass': 'com', 'pickerStep': 'cancel_invoke',
                          'operationHresult': 0x80004005}, s.parse_helper_failure(line))
        bootstrap = (b'ui_helper_failed stage=bootstrap reason=sta '
                     b'operationClass=other pickerStep=not_applicable\n')
        self.assertEqual({'stage': 'bootstrap', 'reason': 'sta', 'operationClass': 'other',
                          'pickerStep': 'not_applicable'}, s.parse_helper_failure(bootstrap))
        commit = (b'ui_helper_failed stage=picker_import reason=operation '
                  b'operationClass=other pickerStep=commit_ready\n')
        self.assertEqual('commit_ready', s.parse_helper_failure(commit)['pickerStep'])
        self.assertEqual(s.HELPER_FAILURE_ALLOWLISTS, s.base.HELPER_FAILURE_ALLOWLISTS)
        unknown = (b'ui_helper_failed stage=picker_import_cancel reason=operation '
                   b'operationClass=future_exception pickerStep=future_step\n')
        self.assertEqual({'stage': 'picker_import_cancel', 'reason': 'operation',
                          'operationClass': 'other', 'pickerStep': 'other'},
                         s.parse_helper_failure(unknown))
        for malformed in (
                line[:-2] + b'\nextra',
                b'ui_helper_failed stage=unknown reason=operation operationClass=other pickerStep=start\n',
                b'ui_helper_failed stage=picker_import_cancel reason=unknown operationClass=other pickerStep=start\n',
                b'ui_helper_failed stage=picker_import_cancel reason=operation operationClass=com pickerStep=start\n',
                b'ui_helper_failed stage=picker_import_cancel reason=operation operationClass=other pickerStep=start hresult=0x80004005\n',
                b'ui_helper_failed stage=picker_import_cancel reason=operation operationClass=com pickerStep=start hresult=0xZZZZZZZZ\n',
                b'ui_helper_failed stage=picker_import_cancel reason=operation operationClass=other pickerStep=start payload\n'):
            with self.subTest(malformed=malformed), self.assertRaisesRegex(ValueError, '^ui_helper_protocol$'):
                s.parse_helper_failure(malformed)
        for bad_hresult in (b'0x8000400', b'0x80004005F', b'0x8000400a'):
            malformed = line.replace(b'0x80004005', bad_hresult)
            with self.subTest(hresult=bad_hresult), self.assertRaisesRegex(ValueError, '^ui_helper_protocol$'):
                s.parse_helper_failure(malformed)

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
                if count > 1:
                    record['matches'] = [dict(isDirectChild=True, classToken='button', enabled=True,
                                              offscreen=False, controlTypeId=50000)] * count
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

    def test_multiple_match_fields_are_mandatory_exact_and_content_free(self):
        match = dict(isDirectChild=True, classToken='button', enabled=True, offscreen=False, controlTypeId=50000)
        good = dict(reason='picker_schema_multiple', matchCount=2, controlTypeId=0, matches=[match, match])
        invalid = [{key: value for key, value in good.items() if key != 'matches'},
                   {**good, 'matches': []}, {**good, 'matches': [match]}, {**good, 'matches': {}}]
        for key, value in [('isDirectChild', 1), ('classToken', 'localized label'), ('enabled', 'true'),
                           ('offscreen', 0), ('controlTypeId', True), ('controlTypeId', 0), ('name', 'private')]:
            invalid.append({**good, 'matches': [match, {**match, key: value}]})
        for key in match:
            invalid.append({**good, 'matches': [match, {k: v for k, v in match.items() if k != key}]})
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            path = root / 'ui-helper-picker-schema.json'
            for record in invalid:
                path.write_text(json.dumps(record), encoding='ascii')
                with self.subTest(record=record), self.assertRaisesRegex(ValueError, '^ui_picker_schema_record$'):
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

    def test_record_failure_revalidates_helper_metadata_and_emits_it_only_for_operation(self):
        operation = dict(stage='bootstrap', reason='operation', operationClass='com',
                         pickerStep='not_applicable')
        invalid = [operation,
                   {**operation, 'operationHresult': 0x100000000},
                   {**operation, 'operationHresult': -1},
                   {**operation, 'operationHresult': True},
                   {**operation, 'operationClass': 'other', 'operationHresult': 5}]
        with patch.object(s.base.sys, 'argv', ['worker', '--worker-private']):
            for helper_failure in invalid:
                with self.subTest(helper_failure=helper_failure), tempfile.TemporaryDirectory() as tmp:
                    root = Path(tmp)
                    s.base.record_failure(root, ValueError('ui_helper_protocol'),
                                          helper_failure=helper_failure)
                    record = json.loads((root / 'observer-worker-failure.json').read_text())
                    self.assertEqual({'reason': 'ui_helper_protocol'}, record)
            with tempfile.TemporaryDirectory() as tmp:
                root = Path(tmp)
                specific_helper = dict(stage='bootstrap', reason='sta', operationClass='other',
                                       pickerStep='not_applicable')
                s.base.record_failure(root, ValueError('ui_helper_protocol'),
                                      helper_failure=specific_helper)
                self.assertEqual({'reason': 'ui_helper_protocol'},
                                 json.loads((root / 'observer-worker-failure.json').read_text()))

    def test_record_must_agree_with_helper_outcome_and_stderr(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            (root / 'ui-helper-picker-schema.json').write_text(json.dumps(
                dict(reason='picker_schema_missing', matchCount=0, controlTypeId=0)), encoding='ascii')
            for code, err in [(0, b''), (1, b''),
                              (1, b'ui_helper_failed stage=picker_import_cancel reason=picker_schema_type operationClass=other pickerStep=wait_dialog\n'),
                              (1, b'ui_helper_failed stage=picker_import_cancel reason=picker_schema_missing operationClass=other pickerStep=wait_dialog\nextra')]:
                with self.subTest(code=code, err=err):
                    with self.assertRaisesRegex(ValueError, '^ui_picker_schema_record$'):
                        s.validate_helper_foreground_failure(root, code, err)
            self.assertFalse(s.validate_helper_foreground_failure(root, 1,
                b'ui_helper_failed stage=picker_import_cancel reason=picker_schema_missing operationClass=other pickerStep=wait_dialog\r\n'))

    @unittest.skipUnless(s.sys.platform == 'win32', 'PowerShell stub execution')
    def test_actual_operation_classifier_unwraps_wrappers_and_bounds_depth(self):
        helper = str(s.HELPER).replace("'", "''")
        script = r'''$ErrorActionPreference='Stop'
$tokens=$null; $errors=$null
$ast=[Management.Automation.Language.Parser]::ParseFile('HELPER',[ref]$tokens,[ref]$errors)
if($errors.Count){throw 'parse'}
$f=$ast.FindAll({param($n) $n -is [Management.Automation.Language.FunctionDefinitionAst] -and $n.Name -ceq 'Get-OperationClassification'},$true)
if($f.Count -ne 1){throw 'function'}
Invoke-Expression $f[0].Extent.Text
Add-Type @'
using System;
using System.Runtime.InteropServices;
namespace System.Windows.Automation {
 public class ElementNotAvailableException : Exception { public ElementNotAvailableException():base("private canary") {} }
}
public class StubOperation {
 public void ThrowElement() { throw new System.Windows.Automation.ElementNotAvailableException(); }
 public object Broken { get { throw new System.Windows.Automation.ElementNotAvailableException(); } }
 public void ThrowCom() { throw new COMException("private canary", unchecked((int)0x80004005)); }
}
'@
$stub=[StubOperation]::new()
try { $stub.ThrowElement() } catch { $elementWrapper=$_.Exception }
if($elementWrapper.GetType().FullName -cne 'System.Management.Automation.MethodInvocationException' -or $null -eq $elementWrapper.InnerException){throw 'method_not_wrapped'}
$element=Get-OperationClassification $elementWrapper
if($element.OperationClass -cne 'element_not_available' -or $element.HResultText -cne ''){throw 'element_class'}
$propertyWrapper=[System.Management.Automation.GetValueInvocationException]::new('property canary', [System.Windows.Automation.ElementNotAvailableException]::new())
if($propertyWrapper.GetType().FullName -cne 'System.Management.Automation.GetValueInvocationException' -or $null -eq $propertyWrapper.InnerException){throw 'property_not_wrapped'}
$property=Get-OperationClassification $propertyWrapper
if($property.OperationClass -cne 'element_not_available' -or $property.HResultText -cne ''){throw 'property_class'}
try { $stub.ThrowCom() } catch { $comWrapper=$_.Exception }
if($null -eq $comWrapper.InnerException){throw 'com_not_wrapped'}
$com=Get-OperationClassification $comWrapper
if($com.OperationClass -cne 'com' -or $com.HResultText -cne ' hresult=0x80004005'){throw 'com_hresult'}
$bounded=[System.Runtime.InteropServices.COMException]::new('private canary', [int]0x80004005)
for($i=0; $i -lt 4; $i++){$bounded=[System.Reflection.TargetInvocationException]::new($bounded)}
if((Get-OperationClassification $bounded).OperationClass -cne 'com'){throw 'four_links'}
$tooDeep=[System.Runtime.InteropServices.COMException]::new('private canary', [int]0x80004005)
for($i=0; $i -lt 5; $i++){$tooDeep=[System.Reflection.TargetInvocationException]::new($tooDeep)}
if((Get-OperationClassification $tooDeep).OperationClass -cne 'other'){throw 'depth_not_bounded'}
'''.replace('HELPER', helper)
        result = subprocess.run([str(s.system_powershell()), '-NoProfile', '-NonInteractive', '-Command', script],
                                capture_output=True, timeout=20, creationflags=subprocess.CREATE_NO_WINDOW)
        self.assertEqual((0, b'', b''), (result.returncode, result.stdout, result.stderr))

    @unittest.skipUnless(s.sys.platform == 'win32', 'Windows PowerShell bootstrap schema execution')
    def test_actual_sta_bootstrap_failure_keeps_fixed_stderr_line(self):
        result = subprocess.run(
            [str(s.system_powershell()), '-NoLogo', '-NoProfile', '-NonInteractive', '-MTA',
             '-File', str(s.HELPER), '-Scene', 'snippets', '-OwnedPid', '1', '-OwnedHwnd', '1',
             '-InitialForeground', '1', '-InputEpoch', '1', '-PrivateRoot', '.', '-Artifact', '.'],
            capture_output=True, timeout=20, creationflags=subprocess.CREATE_NO_WINDOW)
        self.assertEqual(1, result.returncode)
        self.assertEqual(b'', result.stdout)
        self.assertEqual([b'ui_helper_failed stage=bootstrap reason=sta operationClass=other '
                          b'pickerStep=not_applicable'], result.stderr.splitlines())

    @unittest.skipUnless(s.sys.platform == 'win32', 'PowerShell stub execution')
    def test_actual_poll_waits_and_classifies(self):
        helper = s.HELPER.read_text()
        block = helper.split('    function PickerElement', 1)[1].split('        do {', 1)[1].split('        $e = $items[0];', 1)[0]
        setup = r'''
$ErrorActionPreference='Stop'
Add-Type @'
public class StubType { public int Id; }
public class StubCurrent { public StubType ControlType = new StubType(); public string ClassName = "Button"; public bool IsEnabled = true; public bool IsOffscreen; }
public class StubElement { public StubCurrent Current = new StubCurrent(); public object Parent; }
namespace Windows.Automation {
 public enum TreeScope { Descendants }
 public class TreeWalker {
  public static TreeWalker RawViewWalker = new TreeWalker();
  public object GetParent(StubElement e) { return e.Parent; }
 }
 public class AutomationElement { public static bool Compare(object a, object b) { return object.ReferenceEquals(a,b); } }
}
'@
function Guard {}
function OwnedPicker { return 20 }
function Start-Sleep { $script:poll++; if($script:delayed -and $script:poll -eq 2){$script:matches=$script:readyMatches}; if($script:poll -gt 2){$script:pickerStepUntil=[DateTime]::UtcNow.AddSeconds(-1)} }
$scope=New-Object PSObject
$scope | Add-Member ScriptMethod FindAll { param($a,$b) return ,$script:matches }
$dialog=20; $button=$true; $condition=$null
'''
        # A separate owned directory per execution retains every real emitted record for Python.
        for count, kind, reason, variant in [(0, 0, 'picker_schema_missing', ''),
                                            (2, 50000, 'picker_schema_multiple', 'nested'),
                                            (2, 50000, 'picker_schema_multiple', 'hidden'),
                                            (1, 50004, 'picker_schema_type', ''), (1, 50000, '', '')]:
            with self.subTest(count=count, kind=kind, variant=variant), tempfile.TemporaryDirectory() as tmp:
                root = Path(tmp)
                script = setup + "\n$root='" + tmp.replace("'", "''") + "'\n"
                script += f"$script:matches=@(); 1..{max(1,count)} | ForEach-Object {{ if({count} -gt 0){{$e=[StubElement]::new(); $e.Current.ControlType.Id={kind}; $e.Parent=$scope; $script:matches+=$e}} }}\n"
                if variant == 'nested':
                    script += "$script:matches[1].Parent=$null; $script:matches[1].Current.ClassName='DUIButton'\n"
                elif variant == 'hidden':
                    script += "$script:matches[1].Current.IsOffscreen=$true\n"
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
                    expected = dict(reason=reason, matchCount=count, controlTypeId=kind if count == 1 else 0)
                    if count > 1:
                        expected['matches'] = [dict(isDirectChild=True, classToken='button', enabled=True, offscreen=False, controlTypeId=kind),
                                               dict(isDirectChild=variant == 'hidden', classToken='other' if variant == 'nested' else 'button',
                                                    enabled=True, offscreen=variant == 'hidden', controlTypeId=kind)]
                    self.assertEqual(expected, json.loads(path.read_bytes().decode('ascii')))
                    self.assertTrue(s.validate_helper_picker_schema(root))
                else:
                    self.assertFalse(path.exists())
                    self.assertFalse(s.validate_helper_picker_schema(root))
