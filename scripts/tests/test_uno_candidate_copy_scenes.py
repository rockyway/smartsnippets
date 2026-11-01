from contextlib import closing
import importlib.util
import os
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

    def test_parent_checks_metadata_without_reading_database_bytes(self):
        with tempfile.TemporaryDirectory() as directory:
            private = Path(directory)
            database = self.database(private, 0)
            with patch.object(Path, 'read_bytes', side_effect=AssertionError('no content reads')):
                native.verify_positive_boundary(private, 'popup-copy', 7, 8)
            alias = database.with_name('alias.db')
            os.link(database, alias)
            with self.assertRaisesRegex(ValueError, 'native_database_identity'):
                native.verify_positive_boundary(private, 'popup-copy', 7, 8)
            alias.unlink()
            wal = database.with_name(database.name + '-wal')
            wal.write_bytes(b'')
            os.link(wal, alias)
            with self.assertRaisesRegex(ValueError, 'native_database_sidecar'):
                native.verify_positive_boundary(private, 'popup-copy', 7, 8)

    def test_read_only_query_observes_committed_wal_without_writing(self):
        with tempfile.TemporaryDirectory() as directory:
            private = Path(directory)
            database = self.database(private, 0)
            with closing(sqlite3.connect(database)) as writer:
                self.assertEqual('wal', writer.execute('PRAGMA journal_mode=WAL').fetchone()[0])
                writer.execute('PRAGMA wal_autocheckpoint=0')
                writer.execute('UPDATE snippets SET usage_count=1')
                writer.commit()
                wal = database.with_name(database.name + '-wal')
                self.assertTrue(wal.exists())
                files = {p.name: (p.stat().st_size, p.stat().st_mtime_ns)
                         for p in database.parent.iterdir()}
                native.verify_positive_boundary(private, 'context-insert', 7, 8)
                self.assertEqual(files, {p.name: (p.stat().st_size, p.stat().st_mtime_ns)
                                         for p in database.parent.iterdir()})
                with self.assertRaisesRegex(ValueError, 'native_usage_or_epoch'):
                    native.verify_positive_boundary(private, 'popup-copy', 7, 8)

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


class EditorCopyLogTests(unittest.TestCase):
    ERROR = ('2026-01-01T00:00:00.000Z ERROR store_write_failed '
             'op=update_snippet error=SqliteException sqlite=19 hresult=0x80004005')

    def verify(self, private, scene, errors):
        positive = scene in native.POSITIVE_SCENES
        expected = ('native_result=' + scene + '_settled').encode() if positive else {
            'restore': b'native_result=regular_restored', 'abort': b'native_result=abort_restored',
            'owner-refusal': b'native_result=owner_refusal_restored'}[scene]
        phases = [b'native_phase=guardian_ready', b'native_phase=awaiting_owned_a' if positive else b'native_phase=published']
        if scene == 'owner-refusal': phases.append(b'native_phase=owner_refused')
        log = private / 'native-fixture-profile/logs/app.log'
        log.parent.mkdir(parents=True, exist_ok=True)
        log.write_text('\n'.join(['2026-01-01T00:00:00.000Z INFO candidate_started', *errors,
                                  '2026-01-01T00:00:00.000Z INFO candidate_shutdown_complete']) + '\n', encoding='utf-8')
        if scene in native.REGISTERED_SCENES:
            (private / native.REGISTERED_READY_FILE).write_bytes(native.REGISTERED_READY_RECEIPT)
        native.verify_result(private, [b'\n'.join([*phases, expected]) + b'\n', b''], expected, positive)

    def test_editor_copy_accepts_exactly_one_fixed_content_free_failure(self):
        with tempfile.TemporaryDirectory() as directory:
            self.verify(Path(directory), 'editor-copy', [self.ERROR])

    def test_editor_copy_rejects_missing_duplicate_other_or_raw_error(self):
        for errors in ([], [self.ERROR, self.ERROR], [self.ERROR, 'timestamp ERROR other'],
                       [self.ERROR + ' | SqliteException (HResult=0x80004005)'],
                       [self.ERROR + ' message=synthetic'], [self.ERROR.replace('sqlite=19', 'sqlite=1')],
                       [self.ERROR.replace('update_snippet', 'create_snippet')]):
            with self.subTest(errors=errors), tempfile.TemporaryDirectory() as directory:
                with self.assertRaisesRegex(ValueError, '^native_host_readiness_cleanup$'):
                    self.verify(Path(directory), 'editor-copy', errors)

    def test_fixed_failure_is_rejected_in_every_other_scene(self):
        for scene in (*native.POSITIVE_SCENES, 'restore', 'abort', 'owner-refusal'):
            if scene == 'editor-copy': continue
            with self.subTest(scene=scene), tempfile.TemporaryDirectory() as directory:
                with self.assertRaisesRegex(ValueError, '^native_host_readiness_cleanup$'):
                    self.verify(Path(directory), scene, [self.ERROR])


if __name__ == '__main__':
    unittest.main()
