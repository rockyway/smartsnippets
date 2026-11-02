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


def set_rect(r):
    r.left, r.top, r.right, r.bottom = 0, 0, 100, 100
    return 1


def monitor_info(i):
    set_rect(i.monitor)
    set_rect(i.work)
    i.flags = 1
    return 1


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
        GetWindowThreadProcessId=Fn(tid), IsWindowVisible=Fn(lambda h: True),
        GetWindowRect=Fn(lambda h, r: set_rect(r._obj)),
        GetWindowTextLengthW=Fn(lambda h: 0),
        GetMonitorInfoW=Fn(lambda h, i: monitor_info(i._obj)),
        EnumDisplayMonitors=Fn(lambda dc, r, cb, param: cb(1, None, None, 0)),
        EnumThreadWindows=Fn(lambda tid, cb, param: all(cb(h, 0) for h in (1, 2, 3)))),
        dwm=SimpleNamespace(DwmGetWindowAttribute=Fn(dwm)))


EXPECTED_CLASSES = [
    ('IME', 'ime_default'), ('MSCTFIME UI', 'msctfime_ui'),
    ('OleMainThreadWndClass', 'ole_main_thread'), ('CicMarshalWndClass', 'cicmarshal'),
    ('UserAdapterWindowClass', 'user_adapter'), ('tooltips_class32', 'tooltip'),
    ('#32770', 'dialog_32770'), ('#32768', 'menu_32768'),
]


class WindowDiagnosticsTests(unittest.TestCase):
    def test_sha8_vector_prefixes_and_partial_geometry(self):
        self.assertEqual('fd2fa13c', native.class_sha8('Edit'))
        self.assertEqual('8003b59e', native.class_sha8('Edit\U0001f600\ud800'))
        for prefix, token in native.WINDOW_PREFIXES.items():
            self.assertEqual(token, native.window_facts(desktop(prefix + 'Synthetic'), 3, 1, 2)['class_token'])
        d = desktop()
        rect_calls = []
        def rect(h, ptr):
            rect_calls.append(h)
            return 0 if not h else set_rect(ptr._obj)
        d.user.GetWindowRect = Fn(rect)
        facts = native.window_facts(d, 3, 0, 0)
        self.assertNotIn(0, rect_calls)  # NULL A is unavailable; its rectangle is never queried.
        self.assertEqual('ime_default', facts['class_token'])
        self.assertEqual(native.class_sha8('IME'), facts['class_sha8'])
        self.assertTrue(facts['toolwindow'] and facts['noactivate'] and facts['ws_disabled'])
        self.assertFalse(facts['sampled'] or facts['equals_a_rect'] or facts['on_bottom_left_work'])
        self.assertFalse(facts['zero_area'] or facts['offscreen'])

        d = desktop()
        d.user.GetWindowRect = Fn(lambda h, ptr: 0 if h == 1 else set_rect(ptr._obj))
        facts = native.window_facts(d, 3, 1, 2)
        self.assertEqual('ime_default', facts['class_token'])
        self.assertTrue(facts['noactivate'] and facts['ws_disabled'])
        self.assertFalse(facts['equals_a_rect'] or facts['on_bottom_left_work'])
        d.user.EnumDisplayMonitors = Fn(lambda *args: 0)
        facts = native.window_facts(d, 3, 1, 2)
        self.assertEqual('ime_default', facts['class_token'])
        self.assertTrue(facts['noactivate'] and facts['ws_disabled'])
        self.assertFalse(facts['offscreen'] or facts['equals_a_rect'] or facts['on_bottom_left_work'])

    def test_csharp_shaped_facts_json_passes_exact_python_validator(self):
        sample = json.loads('{"class_token":"ime_default","visible":false,"toolwindow":false,"noactivate":false,'
            '"has_owner":false,"owner_is_a":false,"owner_is_b":false,"is_message_only":false,'
            '"same_thread_as_a":false,"cloaked":false,"cloaked_known":false,"class_sha8":"29ee788e",'
            '"ws_popup":false,"ws_caption":false,"ws_child":false,"ws_disabled":false,"ws_ex_topmost":false,'
            '"ws_ex_layered":false,"ws_ex_transparent":false,"ws_ex_appwindow":false,"zero_area":false,'
            '"offscreen":false,"equals_a_rect":false,"on_bottom_left_work":false,"title_nonempty":false,"sampled":false}')
        native.validate_window_facts(sample)

    def test_all_new_style_bits_and_geometry_are_independent(self):
        for key, mask, index in [
                ('ws_popup', 0x80000000, -16), ('ws_caption', 0xc00000, -16),
                ('ws_child', 0x40000000, -16), ('ws_disabled', 0x08000000, -16),
                ('ws_ex_topmost', 8, -20), ('ws_ex_layered', 0x80000, -20),
                ('ws_ex_transparent', 0x20, -20), ('ws_ex_appwindow', 0x40000, -20)]:
            d = desktop()
            d.user.GetWindowLongPtrW = Fn(lambda h, i: mask if i == index else 0)
            facts = native.window_facts(d, 3, 1, 2)
            self.assertTrue(facts[key])
            self.assertEqual(1, sum(facts[k] for k in native.WINDOW_BOOLS if k.startswith('ws_')))
        d = desktop()
        d.user.GetWindowTextLengthW = Fn(lambda h: 9)
        def rect(h, ptr):
            set_rect(ptr._obj)
            if h == 3:
                ptr._obj.left, ptr._obj.right = 200, 300
            return 1
        d.user.GetWindowRect = Fn(rect)
        facts = native.window_facts(d, 3, 1, 2)
        self.assertTrue(facts['offscreen'] and facts['title_nonempty'])
        self.assertFalse(facts['equals_a_rect'] or facts['on_bottom_left_work'] or facts['zero_area'])

    def test_thread_inventory_new_window_diff_and_strict_validation(self):
        ready = dict(phase='ready', complete=True, windows=[
            dict(window_id=native.class_sha8(str(h)), is_a=h == 1, is_b=h == 2,
                 new_at_refusal=False, facts=native.window_facts(desktop(), h, 1, 2)) for h in (1, 2)])
        native.validate_inventory(ready)
        value = native.thread_inventory(desktop(), 1, 2, ready)
        self.assertTrue(value['complete'])
        self.assertEqual([False, False, True], [e['new_at_refusal'] for e in value['windows']])
        self.assertEqual([True, False, False], [e['is_a'] for e in value['windows']])
        for key, wrong in [('class_sha8', 'ABCDEF12'), ('class_sha8', '123'),
                           ('visible', 1), ('ws_popup', 'false'), ('secret', False)]:
            broken = json.loads(json.dumps(value))
            broken['windows'][0]['facts'][key] = wrong
            with self.assertRaises(ValueError):
                native.validate_inventory(broken)
        for key, wrong in [('window_id', 'private'), ('is_a', 1), ('new_at_refusal', 0), ('extra', False)]:
            broken = json.loads(json.dumps(value))
            broken['windows'][0][key] = wrong
            with self.assertRaises(ValueError):
                native.validate_inventory(broken)
        d = desktop()
        d.user.EnumThreadWindows = Fn(lambda tid, cb, p: all(cb(h, p) for h in range(1, 35)))
        self.assertEqual(dict(phase='refusal', complete=False, windows=[]), native.thread_inventory(d, 1, 2, ready))
        d.user.EnumThreadWindows = Fn(lambda *args: 0)
        self.assertFalse(native.thread_inventory(d, 1, 2, ready)['complete'])

    def test_fixed_classes_and_all_properties_are_observed(self):
        for name, token in [*EXPECTED_CLASSES,
                ('SS_Target018_0123456789abcdef0123456789abcdef', 'target018_class'),
                ('SS_Target018_secret', 'other'), ('private title', 'other')]:
            with self.subTest(name=name):
                result = native.window_facts(desktop(name), 3, 1, 2)
                self.assertEqual(dict(**native.extra_window_facts(desktop().user, 3, 1), class_sha8=native.class_sha8(name), class_token=token, sampled=True, visible=True, toolwindow=True,
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
        self.assertEqual({**native.empty_window_facts(), **native.extra_window_facts(d.user, 3, 1), 'class_sha8': native.class_sha8('IME'), 'sampled': True, 'class_token': 'ime_default'}, result)
        self.assertEqual(native.empty_window_facts(), native.window_facts(SimpleNamespace(), 3, 1, 2))
        self.assertFalse(native.window_facts(desktop(), 3)['sampled'])
        a_owner = native.window_facts(desktop(owner=1), 3, 1, 2)
        self.assertTrue(a_owner['owner_is_a'])
        self.assertFalse(a_owner['owner_is_b'])

    def test_python_and_csharp_fixed_class_tables_agree(self):
        self.assertGreaterEqual(len(native.WINDOW_CLASSES), 35)
        source = (native.ROOT / 'WindowDiagnosticFacts.cs').read_text(encoding='utf-8')
        csharp_classes = re.findall(r'"([^"\n]+)"\s*=>\s*"([^"\n]+)"', source)
        self.assertEqual(list(native.WINDOW_CLASSES.items()), csharp_classes)

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
            ready = dict(phase='ready', complete=True, windows=[])
            (root / 'target-thread-windows-ready.json').write_text(json.dumps(ready), encoding='ascii')
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
            inventory = native.read_inventory(root, 'refusal')
            self.assertTrue(inventory['complete'])
            self.assertEqual([True, True, True], [e['new_at_refusal'] for e in inventory['windows']])
            self.assertEqual('user_adapter', inventory['windows'][2]['facts']['class_token'])
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
