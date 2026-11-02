"""Content-free observer diagnostics; no desktop execution."""
import json
import tempfile
import unittest
from pathlib import Path
from unittest.mock import patch
from test_uno_candidate_ui import s


def row(elapsed=0):
    return dict(elapsed_ms=elapsed, epoch_changed=False, idle_ms=1500,
                owned_dialog_present=False, dialog_foreground=False, any_input_down=False)


class PickerInputSamplesTests(unittest.TestCase):
    def test_bounded_series_retains_refusal(self):
        rows = []
        for elapsed in range(100):
            value = row(elapsed)
            value['epoch_changed'] = elapsed == 99
            s.append_picker_input_sample(rows, value)
        self.assertEqual(64, len(rows))
        self.assertEqual(36, rows[0]['elapsed_ms'])
        self.assertTrue(rows[-1]['epoch_changed'])

    def test_strict_validation_and_failure_serialization(self):
        for bad in [[], [row()] * 65, [dict(row(), title='secret')],
                    [dict(row(), idle_ms=True)], [dict(row(), epoch_changed=1)],
                    [dict(row(), elapsed_ms=-1)], [dict(row(), idle_ms=2**32)],
                    [dict(row(), dialog_foreground=True)], [row(2), row(1)]]:
            with self.subTest(bad=bad):
                self.assertIsNone(s.base.validate_picker_input_samples(bad))
                with tempfile.TemporaryDirectory() as temp:
                    error = s.InputRefused('epoch_changed')
                    error.picker_input_samples = bad
                    s.base.record_failure(Path(temp), error)
                    record = json.loads(next(Path(temp).glob('observer-*-failure.json')).read_text())
                    self.assertNotIn('pickerInputSamples', record)
        with tempfile.TemporaryDirectory() as temp:
            error = s.InputRefused('epoch_changed')
            error.picker_input_samples = [row()]
            s.base.record_failure(Path(temp), error)
            record = json.loads(next(Path(temp).glob('observer-*-failure.json')).read_text())
            self.assertEqual([row()], record['pickerInputSamples'])

    def test_sampling_reads_pinned_dialog_and_input_without_mutating_epoch(self):
        class User:
            pass
        class Desktop:
            user = User()
        from unittest.mock import Mock
        desktop = Desktop()
        def last_input(pointer):
            pointer._obj.time = 123
            return True
        desktop.user.GetLastInputInfo = Mock(side_effect=last_input)
        desktop.user.GetAsyncKeyState = Mock(side_effect=lambda key: 0x8000 if key == 1 else 0)
        desktop.user.GetForegroundWindow = Mock(return_value=22)
        kernel = Mock()
        kernel.GetTickCount.return_value = 140
        with patch.object(s.startup.C, 'WinDLL', return_value=kernel, create=True), \
                patch.object(s, 'owned_picker', return_value=22) as owned, \
                patch.object(s, 'picker_foreground', return_value=True), \
                patch.object(s.time, 'monotonic', return_value=2):
            result = s.sample_picker_input(desktop, 11, 42, 100, 1)
        owned.assert_called_once_with(desktop, 11, 42, foreground=22)
        self.assertEqual(dict(elapsed_ms=1000, epoch_changed=True, idle_ms=17,
                              owned_dialog_present=True, dialog_foreground=True,
                              any_input_down=True), result)
