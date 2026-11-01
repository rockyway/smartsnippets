"""Supplemental cross-language text pins, not behavioral/native acceptance proof."""
from pathlib import Path
import unittest
from test_uno_candidate_native import native


class NativeMonitorProtocolPins(unittest.TestCase):
    def test_names_formats_atomic_publication_and_cross_process_order(self):
        root = Path(__file__).resolve().parents[1] / 'uno_candidate_native_fixture'
        coordinator = (root / 'Coordinator.cs').read_text(encoding='utf-8')
        worker = (root / 'PositiveHostScene.cs').read_text(encoding='utf-8')
        app = (root / 'App.xaml.cs').read_text(encoding='utf-8')
        route = (root / 'PositiveProtocol.cs').read_text(encoding='utf-8')
        python = Path(native.__file__).read_text(encoding='utf-8')
        for filename, producer in [('positive-identities.txt', coordinator),
                                   ('positive-foreground-granted', worker),
                                   ('positive-foreground-closed', app)]:
            self.assertIn('"' + filename + '"', producer)
            self.assertIn("'" + filename + "'", python)

        def ordered(text, *snippets):
            positions = []
            for snippet in snippets:
                self.assertEqual(1, text.count(snippet), snippet)
                positions.append(text.index(snippet))
            self.assertEqual(sorted(positions), positions)

        ordered(coordinator,
                'var identitiesPath = Path.Combine(Program.PrivateRoot, "positive-identities.txt");',
                'await File.WriteAllTextAsync(identitiesPath + ".tmp", $"{RetainedIdentity.Id(fixture)} {worker.Id} {RetainedIdentity.Id(guardian)}", ct.Token);',
                'File.Move(identitiesPath + ".tmp", identitiesPath);',
                'var surfacesPath = Path.Combine(Program.PrivateRoot, "positive-surfaces.txt");',
                'await File.WriteAllTextAsync(surfacesPath + ".tmp", string.Join(" ", fields), ct.Token);',
                'File.Move(surfacesPath + ".tmp", surfacesPath);',
                'await BootstrapProtocol.WriteAsync(commands, "lease_positive", ct.Token);')
        ordered(worker,
                '"positive_surfaces {ready.WindowA} {ready.WindowB} {boundary.Popup!.NativeHandle} {boundary.Main!.NativeHandle}"',
                'if (await BootstrapProtocol.ReadAsync(Program.Commands, ct) != "lease_positive")',
                'if (PositiveProtocol.NeedsCapturedPopup(scene)) await InsertAsync(scene, id, ct);',
                'var callbackTimeout = await PositiveShortcutRoute.BeginAsync(scene,',
                'granted = await fixture.Lease.GrantForegroundAsync(ct);',
                'if (!granted) throw new InvalidOperationException("native_foreground_grant_refused");',
                'await File.WriteAllTextAsync(grantPath + ".tmp", "granted\\n", ct);',
                'File.Move(grantPath + ".tmp", grantPath);')
        ordered(route, 'await grant();', 'post();')
        # The post-lease scene (including any grant) completes before host exit/closure;
        # closure must be published before disposal of A/B, not after process settlement.
        positive_app = app.split('if (isPositive)', 1)[1].split('return;', 1)[0]
        ordered(positive_app, 'await positive.RunAsync(config[0], ct);', 'await host.ExitAsync();',
                'var closedPath = Path.Combine(Program.PrivateRoot, "positive-foreground-closed");',
                'File.WriteAllText(closedPath + ".tmp", "closed\\n");',
                'File.Move(closedPath + ".tmp", closedPath);',
                'await fixture.DisposeAsync(); fixture = null;')
        for literal in ("b'granted\\n'", "b'closed\\n'", "tuple(map(int, raw.split()))"):
            self.assertIn(literal, python)
