"""Run the actual classifier/monitor with inert Win32 boundary stubs."""
import json
from pathlib import Path
import re
import tempfile
from types import SimpleNamespace
import unittest
from unittest.mock import patch
from test_uno_candidate_native import native


class Fn:
    def __init__(self, call): self.call = call
    def __call__(self, *args): return self.call(*args)


def desktop(name='IME', owner=2, parent=-3, thread=7, style=0x08000080, cloak=2, hr=0, classes=None):
    class_names = classes or {}
    def class_name(h, buffer, size):
        value = class_names.get(h, name)
        buffer.value = value
        return len(value)
    def tid(h, pid):
        pid._obj.value = 100
        return 7 if h == 1 else thread
    def dwm(h, attr, output, size):
        assert attr == 14
        output._obj.value = cloak
        return hr
    return SimpleNamespace(user=SimpleNamespace(
        GetClassNameW=Fn(class_name), GetWindowLongPtrW=Fn(lambda h, i: style),
        GetWindow=Fn(lambda h, cmd: owner), GetAncestor=Fn(lambda h, flags: parent),
        FindWindowExW=Fn(lambda parent_hwnd, after, cls, title: 3 if parent and not after else 0),
        GetWindowThreadProcessId=Fn(tid), IsWindowVisible=Fn(lambda h: True)),
        dwm=SimpleNamespace(DwmGetWindowAttribute=Fn(dwm)))


EXPECTED_CLASSES = [
    ('IME', 'ime_default'), ('MSCTFIME UI', 'msctfime_ui'),
    ('OleMainThreadWndClass', 'ole_main_thread'), ('CicMarshalWndClass', 'cicmarshal'),
    ('UserAdapterWindowClass', 'user_adapter'), ('tooltips_class32', 'tooltip'),
    ('#32770', 'dialog_32770'), ('#32768', 'menu_32768'),
]


class WindowDiagnosticsTests(unittest.TestCase):
    def test_fixed_classes_and_all_properties_are_observed(self):
        for name, token in [*EXPECTED_CLASSES,
                ('SS_Target018_0123456789abcdef0123456789abcdef', 'target018_class'),
                ('SS_Target018_secret', 'other'), ('private title', 'other')]:
            with self.subTest(name=name):
                result = native.window_facts(desktop(name), 3, 1, 2)
                self.assertEqual(dict(class_token=token, sampled=True, visible=True, toolwindow=True,
                    noactivate=True, has_owner=True, owner_is_a=False, owner_is_b=True, is_message_only=True,
                    same_thread_as_a=True, cloaked=True, cloaked_known=True), result)
                self.assertNotIn(name, json.dumps(result))

    def test_style_flags_are_independent_bits(self):
        for style, toolwindow, noactivate in [(0x80, True, False), (0x08000000, False, True)]:
            with self.subTest(style=style):
                facts = native.window_facts(desktop(style=style), 3, 1, 2)
                self.assertEqual(toolwindow, facts['toolwindow'])
                self.assertEqual(noactivate, facts['noactivate'])

    def test_cloak_known_zero_is_not_unknown(self):
        facts = native.window_facts(desktop(cloak=0), 3, 1, 2)
        self.assertFalse(facts['cloaked'])
        self.assertTrue(facts['cloaked_known'])

    def test_negative_flags_and_unknown_cloak_are_not_assumed_positive(self):
        d = desktop(owner=0, parent=0, thread=0, style=0, hr=-1)
        d.user.IsWindowVisible = Fn(lambda h: False)
        result = native.window_facts(d, 3, 1, 2)
        self.assertEqual({**native.empty_window_facts(), 'sampled': True, 'class_token': 'ime_default'}, result)
        self.assertEqual(native.empty_window_facts(), native.window_facts(SimpleNamespace(), 3, 1, 2))
        self.assertFalse(native.window_facts(desktop(), 3)['sampled'])
        a_owner = native.window_facts(desktop(owner=1), 3, 1, 2)
        self.assertTrue(a_owner['owner_is_a'])
        self.assertFalse(a_owner['owner_is_b'])

    def test_python_and_csharp_fixed_class_tables_agree(self):
        self.assertEqual(dict(EXPECTED_CLASSES), native.WINDOW_CLASSES)
        source = (native.ROOT / 'WindowDiagnosticFacts.cs').read_text(encoding='utf-8')
        csharp_classes = re.findall(r'"([^"\n]+)"\s*=>\s*"([^"\n]+)"', source)
        self.assertEqual(EXPECTED_CLASSES, csharp_classes)

    def test_production_wiring_passes_window_facts_and_window_b(self):
        app = (native.ROOT / 'App.xaml.cs').read_text(encoding='utf-8')
        self.assertIn('() => fixture?.Lease.Readiness.WindowB ?? 0,', app)
        self.assertIn('window => WindowDiagnosticNative.Sample(window, fixture?.Lease.Readiness.WindowA ?? 0, fixture?.Lease.Readiness.WindowB ?? 0)', app)
        preparation = (native.ROOT / 'OwnedTargetPreparationNative.cs').read_text(encoding='utf-8')
        self.assertIn('fixture.Lease.Readiness.WindowB,\n            window => WindowDiagnosticNative.Sample(window, fixture.Lease.Readiness.WindowA, fixture.Lease.Readiness.WindowB));', preparation)

    def test_real_monitor_latches_facts_at_first_refusal_not_teardown(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            (root / 'positive-surfaces.txt').write_bytes(b'1 2 30 40')
            (root / 'positive-identities.txt').write_bytes(b'100 200 300')
            d = desktop(classes={1: 'IME', 3: 'UserAdapterWindowClass', 2: '#32770'})
            samples = iter([1, 3, 2])
            state = SimpleNamespace(window=0)
            class Process:
                returncode = None
                def poll(self):
                    state.window = next(samples, None)
                    if state.window is None: self.returncode = 2
                    return self.returncode
            d.user.GetForegroundWindow = Fn(lambda: state.window)
            d.validate_input_desktop = lambda: None
            with patch('builtins.print'), self.assertRaisesRegex(ValueError, '^native_scene_failed$'):
                native.monitor(Process(), d, 99, [], positive_private=root, scene='posted-row', sleep=lambda _: None)
            record = native.validate_monitor_failure(root)
            self.assertEqual(('same_pid_other', 2), (record['relation'], record['sampleOrder']))
            self.assertEqual(native.window_facts(d, 3, 1, 2), record['windowFacts'])
            self.assertEqual('user_adapter', record['windowFacts']['class_token'])
            self.assertNotEqual(record['windowFacts'], native.window_facts(d, 2, 1, 2))
            for key, value in [('class_token', 'private title'), ('visible', 1), ('extra', False)]:
                broken = {**record, 'windowFacts': {**record['windowFacts'], key: value}}
                (root / 'native-monitor-failure.json').write_text(json.dumps(broken), encoding='ascii')
                with self.assertRaisesRegex(ValueError, '^native_monitor_record$'):
                    native.validate_monitor_failure(root)
