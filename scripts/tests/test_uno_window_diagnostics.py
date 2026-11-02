"""Run the actual classifier/monitor with inert Win32 boundary stubs."""
import json
from pathlib import Path
import tempfile
from types import SimpleNamespace
import unittest
from unittest.mock import patch
from test_uno_candidate_native import native


class Fn:
    def __init__(self, call): self.call = call
    def __call__(self, *args): return self.call(*args)


def desktop(name='IME', owner=2, parent=-3, thread=7, style=0x08000080, cloak=2, hr=0):
    def class_name(h, buffer, size):
        buffer.value = name
        return len(name)
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


class WindowDiagnosticsTests(unittest.TestCase):
    def test_fixed_classes_and_all_properties_are_observed(self):
        for name, token in [*native.WINDOW_CLASSES.items(),
                ('SS_Target018_0123456789abcdef0123456789abcdef', 'target018_class'),
                ('SS_Target018_secret', 'other'), ('private title', 'other')]:
            with self.subTest(name=name):
                result = native.window_facts(desktop(name), 3, 1, 2)
                self.assertEqual(dict(class_token=token, sampled=True, visible=True, toolwindow=True,
                    noactivate=True, has_owner=True, owner_is_a=False, owner_is_b=True, is_message_only=True,
                    same_thread_as_a=True, cloaked=True, cloaked_known=True), result)
                self.assertNotIn(name, json.dumps(result))

    def test_negative_flags_and_unknown_cloak_are_not_assumed_positive(self):
        d = desktop(owner=0, parent=0, thread=0, style=0, hr=-1)
        d.user.IsWindowVisible = Fn(lambda h: False)
        result = native.window_facts(d, 3, 1, 2)
        self.assertEqual({**native.empty_window_facts(), 'sampled': True, 'class_token': 'ime_default'}, result)
        self.assertEqual(native.empty_window_facts(), native.window_facts(SimpleNamespace(), 3, 1, 2))
        a_owner = native.window_facts(desktop(owner=1), 3, 1, 2)
        self.assertTrue(a_owner['owner_is_a'])
        self.assertFalse(a_owner['owner_is_b'])

    def test_real_monitor_latches_facts_at_first_refusal_not_teardown(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            (root / 'positive-surfaces.txt').write_bytes(b'1 2 30 40')
            (root / 'positive-identities.txt').write_bytes(b'100 200 300')
            d = desktop('UserAdapterWindowClass')
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
            for key, value in [('class_token', 'private title'), ('visible', 1), ('extra', False)]:
                broken = {**record, 'windowFacts': {**record['windowFacts'], key: value}}
                (root / 'native-monitor-failure.json').write_text(json.dumps(broken), encoding='ascii')
                with self.assertRaisesRegex(ValueError, '^native_monitor_record$'):
                    native.validate_monitor_failure(root)
