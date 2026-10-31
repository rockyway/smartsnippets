import sys
import unittest
from pathlib import Path
from unittest.mock import Mock, patch

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
import private_evidence as evidence


class PrivateEvidenceTests(unittest.TestCase):
    def test_identity_failure_never_creates(self):
        api = Mock()
        api.current_sid.side_effect = OSError("token")
        with patch.object(evidence, "WindowsSecurity", return_value=api):
            with self.assertRaises(OSError):
                evidence.create_windows_private_directory()
        api.create.assert_not_called()

    def test_create_failure_never_removes_or_retries_existing_path(self):
        api = Mock()
        api.create.side_effect = FileExistsError("collision")
        with patch.object(evidence, "WindowsSecurity", return_value=api), patch.object(Path, "rmdir") as remove:
            with self.assertRaises(FileExistsError):
                evidence.create_windows_private_directory()
        api.create.assert_called_once()
        api.verify.assert_not_called()
        remove.assert_not_called()

    def test_postcreate_failure_removes_only_empty_owned_directory(self):
        api = Mock()
        api.verify.side_effect = OSError("ACL")
        with patch.object(evidence, "WindowsSecurity", return_value=api), patch.object(Path, "rmdir") as remove:
            with self.assertRaises(OSError):
                evidence.create_windows_private_directory()
        api.create.assert_called_once()
        remove.assert_called_once_with()

    def test_nonempty_cleanup_fails_closed_without_recursive_deletion(self):
        api = Mock()
        api.verify.side_effect = OSError("ACL")
        with patch.object(evidence, "WindowsSecurity", return_value=api), patch.object(Path, "rmdir", side_effect=OSError("not empty")):
            with self.assertRaises(OSError):
                evidence.create_windows_private_directory()
        api.create.assert_called_once()

    @unittest.skipUnless(sys.platform == "win32", "Windows security API")
    def test_actual_windows_empty_directory_owner_protected_user_only_acl(self):
        root = evidence.create_windows_private_directory()
        try:
            self.assertEqual([], list(root.iterdir()))
            api = evidence.WindowsSecurity()
            api.verify(root, api.current_sid())
        finally:
            root.rmdir()


if __name__ == "__main__":
    unittest.main()
