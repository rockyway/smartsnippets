"""Synthetic privacy controls only; no profile, input or clipboard access."""
import importlib.util
import json
from pathlib import Path
import unittest
from unittest.mock import patch

SPEC = importlib.util.spec_from_file_location(
    'candidate_privacy', Path(__file__).resolve().parents[1] / 'uno_candidate_privacy.py')
privacy = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(privacy)
CANARIES = ('PRIVATE-CONTENT-α漢🙂', 'PRIVATE-LABEL-019', 'PRIVATE-TAG-019',
            'PRIVATE-TITLE-019', 'PRIVATE-QUERY-019', 'C:\\PRIVATE-DOCUMENT-019')


class CandidatePrivacyTests(unittest.TestCase):
    def test_clean_receipt_is_not_native_acceptance(self):
        receipt = privacy.scan_evidence((b'candidate_started\ncandidate_shutdown\n', b''), CANARIES)
        self.assertEqual({'privacy': 'CLEAN', 'records': 2,
                          'nativeAcceptance': 'NOT_ASSERTED'}, receipt)

    def test_each_field_and_encoding_is_detected_without_echo(self):
        for canary in CANARIES:
            for encoding in ('utf-8', 'utf-16-le', 'utf-16-be'):
                with self.subTest(encoding=encoding):
                    receipt = privacy.scan_evidence((b'clean', b'prefix' + canary.encode(encoding)), CANARIES)
                    self.assertEqual('LEAK_DETECTED', receipt['privacy'])
                    self.assertNotIn(canary, json.dumps(receipt, ensure_ascii=False))
                    self.assertNotIn('offset', receipt)

    def test_negative_controls_use_the_actual_scanner(self):
        self.assertEqual({'negativeControls': 'DETECTED', 'count': 18},
                         privacy.verify_negative_controls(CANARIES))
        with patch.object(privacy, 'scan_evidence', return_value={'privacy': 'CLEAN'}):
            with self.assertRaisesRegex(ValueError, '^privacy_negative_control_failed$'):
                privacy.verify_negative_controls(CANARIES)

    def test_missing_short_null_or_excessive_canaries_refuse(self):
        for canaries in ((), ('short',), ('abcdefgh\x00',), (None,), ('abcdefgh',) * 65):
            with self.assertRaisesRegex(ValueError, '^privacy_canaries_invalid$'):
                privacy.scan_evidence((b'clean',), canaries)

    def test_absent_invalid_or_excessive_evidence_refuses(self):
        for records in ((), ('not bytes',), (None,), (b'',) * 129):
            with self.assertRaisesRegex(ValueError, '^privacy_evidence_invalid$'):
                privacy.scan_evidence(records, CANARIES)
        with patch.object(privacy, 'MAX_EVIDENCE_BYTES', 8):
            with self.assertRaisesRegex(ValueError, '^privacy_evidence_invalid$'):
                privacy.scan_evidence((b'12345', b'6789'), CANARIES)


def valid_foreground_failure():
    record = {key: False for key, kind in privacy.FOREGROUND_FAILURE_SCHEMA if kind == 'bool'}
    record.update({'foregroundRelation': 'owned_other', 'phaseIndex': 0, 'sampleOrder': 3})
    return record


class ForegroundFailureSchemaTests(unittest.TestCase):
    def test_exact_schema_is_accepted_and_returned_unchanged(self):
        record = valid_foreground_failure()
        self.assertIs(record, privacy.validate_foreground_failure(record))
        self.assertEqual(13, len(record))
        self.assertTrue(all(k == 'foregroundRelation' or type(v) in (bool, int) for k, v in record.items()))

    def test_any_key_outside_the_schema_is_rejected(self):
        for extra in ('title', 'className', 'hwnd', 'path', 'foregroundTitle', 'fgClass', 'Extra', ''):
            record = valid_foreground_failure()
            record[extra] = True
            with self.subTest(extra=extra), self.assertRaisesRegex(ValueError, 'ui_foreground_schema'):
                privacy.validate_foreground_failure(record)

    def test_missing_keys_and_wrong_shapes_are_rejected(self):
        for key, _ in privacy.FOREGROUND_FAILURE_SCHEMA:
            record = valid_foreground_failure()
            del record[key]
            with self.subTest(missing=key), self.assertRaisesRegex(ValueError, 'ui_foreground_schema'):
                privacy.validate_foreground_failure(record)
        for bad in (None, [], 'text', 3, valid_foreground_failure().items()):
            with self.subTest(bad=type(bad).__name__), self.assertRaisesRegex(ValueError, 'ui_foreground_schema'):
                privacy.validate_foreground_failure(bad)

    def test_values_are_strictly_typed_numeric_boolean_or_fixed_enum(self):
        cases = [('fgIsMain', 1), ('fgEnabled', 'true'), ('fgVisible', None), ('popupPresent', 0),
                 ('phaseIndex', True), ('phaseIndex', 4), ('phaseIndex', -2), ('phaseIndex', '0'),
                 ('sampleOrder', -1), ('sampleOrder', 1000001), ('sampleOrder', 1.5), ('sampleOrder', 'x'),
                 ('sampleOrder', False), ('foregroundRelation', 'C:\\secret'), ('foregroundRelation', 7),
                 ('foregroundRelation', 'Owned_Other'), ('foregroundRelation', '')]
        for key, value in cases:
            record = valid_foreground_failure()
            record[key] = value
            with self.subTest(key=key, value=value), self.assertRaisesRegex(ValueError, 'ui_foreground_schema'):
                privacy.validate_foreground_failure(record)
        for relation in privacy.FOREGROUND_RELATIONS:
            record = valid_foreground_failure()
            record['foregroundRelation'] = relation
            privacy.validate_foreground_failure(record)


if __name__ == '__main__':
    unittest.main()
