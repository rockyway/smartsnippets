from contextlib import closing
import importlib.util
from pathlib import Path
import sqlite3
import tempfile
import unittest
from unittest.mock import patch

spec = importlib.util.spec_from_file_location('copy_scene_runner', Path(__file__).parents[1] / 'run_uno_candidate_native.py')
native = importlib.util.module_from_spec(spec)
spec.loader.exec_module(native)


class CopySceneTests(unittest.TestCase):
    SCENES = ('popup-copy', 'editor-copy', 'footer-copy', 'context-insert')

    def database(self, private, usage):
        database = private / 'native-fixture-profile/smartsnippets.db'
        database.parent.mkdir()
        with closing(sqlite3.connect(database)) as connection, connection:
            connection.execute('CREATE TABLE snippets (usage_count INTEGER NOT NULL)')
            connection.execute('INSERT INTO snippets VALUES (?)', (usage,))
        return database

    def test_each_scene_requires_closed_review_and_truthful_callback_proof(self):
        for scene in self.SCENES:
            with self.subTest(scene=scene):
                self.assertIn(scene, native.POSITIVE_SCENES)
                self.assertEqual(scene == 'context-insert', scene in native.INSERT_SCENES)
                self.assertEqual(scene == 'context-insert', scene in native.INPUT_CAPABILITY_SCENES)
                self.assertEqual('registered_posted_callback_not_physical', native.shortcut_proof(scene))
                self.assertEqual('vm_callback', native.action_proof(scene))
                with self.assertRaises(ValueError):
                    native.require_positive_authorization(scene, True, True)
                with patch.object(native, 'POSITIVE_SCENES_SOURCE_REVIEWED', True):
                    if scene == 'context-insert':
                        with self.assertRaises(ValueError):
                            native.require_positive_authorization(scene, True, True)
                    else:
                        native.require_positive_authorization(scene, True, True)
                    for row6, recopied in ((False, True), (True, False)):
                        with self.assertRaises(ValueError):
                            native.require_positive_authorization(scene, row6, recopied)

    def test_independent_query_requires_exact_persisted_usage_and_changed_epoch(self):
        for scene in self.SCENES:
            with self.subTest(scene=scene), tempfile.TemporaryDirectory() as directory:
                private = Path(directory)
                usage = int(scene == 'context-insert')
                database = self.database(private, usage)
                before = database.read_bytes()
                self.assertEqual({'usageVerified': True, 'clipboardEpochChanged': True},
                                 native.verify_positive_boundary(private, scene, 7, 8))
                self.assertEqual(before, database.read_bytes())
                for a, b in ((7, 7), (0, 8), (7, 0)):
                    with self.assertRaises(ValueError): native.verify_positive_boundary(private, scene, a, b)
                with closing(sqlite3.connect(database)) as connection, connection:
                    connection.execute('UPDATE snippets SET usage_count=?', (1 - usage,))
                with self.assertRaises(ValueError): native.verify_positive_boundary(private, scene, 7, 8)

    def test_preview_and_expiry_require_unchanged_epoch_and_zero_usage(self):
        for scene in ('preview', 'expired'):
            with self.subTest(scene=scene), tempfile.TemporaryDirectory() as directory:
                private = Path(directory)
                self.database(private, 0)
                self.assertFalse(native.verify_positive_boundary(private, scene, 7, 7)['clipboardEpochChanged'])
                with self.assertRaises(ValueError): native.verify_positive_boundary(private, scene, 7, 8)

    def test_missing_empty_duplicate_or_wrong_database_never_passes(self):
        for scene in self.SCENES:
            with self.subTest(scene=scene), tempfile.TemporaryDirectory() as directory:
                private = Path(directory)
                with self.assertRaises(OSError): native.verify_positive_boundary(private, scene, 7, 8)
                database = self.database(private, int(scene == 'context-insert'))
                with closing(sqlite3.connect(database)) as connection, connection:
                    connection.execute('DELETE FROM snippets')
                with self.assertRaises(ValueError): native.verify_positive_boundary(private, scene, 7, 8)
                with closing(sqlite3.connect(database)) as connection, connection:
                    connection.executemany('INSERT INTO snippets VALUES (?)', [(0,), (0,)])
                with self.assertRaises(ValueError): native.verify_positive_boundary(private, scene, 7, 8)


if __name__ == '__main__':
    unittest.main()
