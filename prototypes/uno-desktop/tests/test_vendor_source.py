import importlib.util
import json
from pathlib import Path
import subprocess
import tempfile
import unittest

VENDOR = Path(__file__).resolve().parents[1] / "vendor"
spec = importlib.util.spec_from_file_location("prepare_source", VENDOR / "prepare_source.py")
source = importlib.util.module_from_spec(spec)
spec.loader.exec_module(source)


class VendorSourceTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.commit = "a" * 40
        (self.root / ".smartsnippets-uno018-source").write_text(self.commit)
        self.lock = {"commit": self.commit, "files": []}
        for relative, (old, _) in source.REPLACEMENTS.items():
            path = source.PREFIX + relative
            data = ("// synthetic fixture\r\n" + old + ";\r\n").encode()
            target = self.root / path
            target.parent.mkdir(parents=True, exist_ok=True)
            target.write_bytes(data)
            sha = subprocess.run(["git", "hash-object", "--stdin"], input=data,
                                 capture_output=True, check=True).stdout.decode().strip()
            self.lock["files"].append({"path": path, "gitBlobSha1": sha})

    def test_git_blob_digest_matches_git_not_a_self_defined_oracle(self):
        for item in self.lock["files"]:
            self.assertEqual(item["gitBlobSha1"], source.git_blob_sha1((self.root / item["path"]).read_bytes()))

    def test_patch_changes_both_exact_calls_and_preserves_all_other_bytes(self):
        changes = source.patch_source(self.root)
        self.assertEqual(2, len(changes))
        source.verify_source(self.root, self.lock)
        for item in self.lock["files"]:
            self.assertIn(b"\r\n", (self.root / item["path"]).read_bytes())

    def test_changed_source_tampering_is_rejected(self):
        source.patch_source(self.root)
        target = self.root / self.lock["files"][0]["path"]
        target.write_bytes(target.read_bytes() + b"// unapproved change")
        with self.assertRaises(ValueError):
            source.verify_source(self.root, self.lock)

    def test_unpatched_source_and_wrong_marker_are_rejected(self):
        with self.assertRaises(ValueError):
            source.verify_source(self.root, self.lock)
        source.patch_source(self.root)
        (self.root / ".smartsnippets-uno018-source").write_text("wrong")
        with self.assertRaises(ValueError):
            source.verify_source(self.root, self.lock)

    def test_changed_upstream_shape_cannot_be_patched_silently(self):
        target = self.root / self.lock["files"][0]["path"]
        target.write_bytes(target.read_bytes() * 2)
        with self.assertRaises(ValueError):
            source.patch_source(self.root)

    def test_unlisted_compilable_source_is_rejected_even_during_reprepare(self):
        source.patch_source(self.root)
        (self.root / source.PREFIX / "Injected.cs").write_text("// not pinned")
        for complete in (True, False):
            with self.assertRaisesRegex(ValueError, "file set differs"):
                source.verify_layout(self.root, self.lock, complete=complete)
        with self.assertRaisesRegex(ValueError, "file set differs"):
            source.verify_source(self.root, self.lock)

    def test_optional_receipt_is_allowed_but_other_files_are_not(self):
        source.patch_source(self.root)
        (self.root / "source-receipt.json").write_text("{}")
        source.verify_source(self.root, self.lock)
        (self.root / "unexpected.txt").write_text("unlisted")
        with self.assertRaisesRegex(ValueError, "file set differs"):
            source.verify_source(self.root, self.lock)

    def test_symlink_is_rejected(self):
        source.patch_source(self.root)
        link = self.root / "linked.cs"
        try:
            link.symlink_to(self.root / self.lock["files"][0]["path"])
        except OSError:
            self.skipTest("Host does not permit creating symlinks")
        with self.assertRaisesRegex(ValueError, "must not contain symlinks"):
            source.verify_source(self.root, self.lock)

    def test_lock_pins_original_sources_and_licenses(self):
        lock = json.loads((VENDOR / "source-lock.json").read_text())
        self.assertEqual("da217df2312e9e39676880f9cb4b8d519cc297e0", lock["commit"])
        paths = [item["path"] for item in lock["files"]]
        self.assertEqual("6.7.135", lock["frameworkVersion"])
        self.assertEqual(39, len(set(paths)))
        self.assertIn("License.md", paths)
        self.assertIn("THIRD-PARTY-NOTICES.md", paths)
        self.assertEqual(37, sum(path.endswith(".cs") for path in paths))


if __name__ == "__main__":
    unittest.main()
