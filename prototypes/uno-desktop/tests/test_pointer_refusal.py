"""Content-free diagnostics only; no native calls or process launches."""
from pathlib import Path
import sys
import unittest

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from owned_pointer_hover import failure_code


class PointerRefusalTests(unittest.TestCase):
    def test_known_preconditions_are_distinct(self):
        self.assertEqual("INPUT_NOT_IDLE", failure_code(ValueError("Exclusive idle input required")))
        self.assertEqual("CURSOR_OVER_PROBE", failure_code(ValueError("Original cursor already over owned probe")))
        self.assertEqual("OWNED_LEAF", failure_code(ValueError("Final owned leaf hit-test failed")))

    def test_allowlisted_control_ids_only(self):
        self.assertEqual("UIA_ROW", failure_code(ValueError("Owned UIA bounds unavailable:OwnedCompanionRow")))
        self.assertEqual("GUARD_REFUSED", failure_code(ValueError("Owned UIA bounds unavailable:other")))

    def test_unknown_error_content_never_escapes(self):
        self.assertEqual("GUARD_REFUSED", failure_code(OSError("synthetic private error canary")))


if __name__ == "__main__":
    unittest.main()
