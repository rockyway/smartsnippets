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


if __name__ == '__main__':
    unittest.main()
