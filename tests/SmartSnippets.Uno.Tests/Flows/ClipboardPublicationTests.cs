using SmartSnippets.Uno.Platforms.Windows;

namespace SmartSnippets.Uno.Tests.Flows;

public sealed class ClipboardPublicationTests
{
    [Fact]
    public void OwnCloseAndLazyTextSynthesis_AreSealedBeforePaste_NotMistakenForExternalWrite()
    {
        var clipboard = new Clipboard();
        var premature = clipboard.Sequence;
        var committed = clipboard.Seal();
        Assert.Equal(43u, committed); // write close (42), lazy-render close (43)
        Assert.False(ClipboardPublication.CanRestore(premature, clipboard.Sequence, true));
        Assert.True(ClipboardPublication.CanRestore(committed, clipboard.Sequence, true));
        Assert.Equal(new[] { "close", "open", "recognize", "materialize", "recognize", "close",
            "open", "recognize", "recognize", "sequence", "close" }, clipboard.Trace);
        Assert.False(clipboard.Locked);
    }

    [Theory]
    [InlineData("owner")]
    [InlineData("transaction")]
    [InlineData("payload")]
    [InlineData("foreign-format")]
    public void ForeignWriteDuringClose_IsNotAdoptedAsOurBaseline(string changed)
    {
        var clipboard = new Clipboard();
        clipboard.AfterClose = () =>
        {
            clipboard.Sequence++;
            switch (changed)
            {
                case "owner": clipboard.SameOwner = false; break;
                case "transaction": clipboard.SameTransaction = false; break;
                case "payload": clipboard.SamePayload = false; break;
                case "foreign-format": clipboard.KnownFormats = false; break;
            }
        };
        Assert.Null(clipboard.Seal());
        Assert.DoesNotContain("sequence", clipboard.Trace);
        Assert.False(clipboard.Locked);
    }

    [Fact]
    public void ForeignWriterBetweenMaterializationAndFinalLock_IsAlsoRejected()
    {
        var clipboard = new Clipboard(); var closes = 0;
        clipboard.AfterClose = () => { if (++closes == 2) clipboard.SameOwner = false; };
        Assert.Null(clipboard.Seal()); Assert.False(clipboard.Locked);
    }

    [Fact]
    public void AfterSealing_AnySequenceChangeEvenSameOwner_IsRefused_NoRebaseAfterPaste()
    {
        var clipboard = new Clipboard(); var committed = clipboard.Seal();
        clipboard.Sequence++;
        Assert.False(ClipboardPublication.CanRestore(committed, clipboard.Sequence, true));
        Assert.False(ClipboardPublication.CanRestore(committed, committed!.Value, false));
        Assert.False(ClipboardPublication.CanRestore(null, clipboard.Sequence, true));
    }

    [Fact]
    public void ContendedOpen_DoesNotCaptureAnUnprotectedSequence()
    {
        var clipboard = new Clipboard { OpenAvailable = false };
        Assert.Null(clipboard.Seal()); Assert.Equal(new[] { "close", "open" }, clipboard.Trace);
    }

    [Fact]
    public void MaterializationFailure_ClosesAndRefusesPublication()
    {
        var clipboard = new Clipboard { MaterializeAvailable = false };
        Assert.Null(clipboard.Seal()); Assert.False(clipboard.Locked);
        Assert.DoesNotContain("sequence", clipboard.Trace);
    }

    [Fact]
    public void SequenceFailure_StillClosesTheReadLock()
    {
        var clipboard = new Clipboard { SequenceThrows = true };
        Assert.Throws<InvalidOperationException>(() => clipboard.Seal());
        Assert.False(clipboard.Locked);
    }

    [Fact]
    public void RealDispatcher_UsesSealedTransactionAndStrictRestoreGuard_NotPrecloseBaseline()
    {
        var root = SmartSnippets.Uno.Tests.Build.BuildGraphTests.RepoRoot;
        var source = File.ReadAllText(Path.Combine(root, "src/SmartSnippets.Uno/Platforms/Windows/DailyNative/ClipboardPasteDispatcher.cs"));
        Assert.Contains("ClipboardPublication.CloseAndCapture(", source);
        Assert.Contains("ClipboardPublication.CanRestore(snapshot.Sequence, currentSequence, sameOwner)", source);
        Assert.Contains("PInvoke.GetClipboardOwner() != clipboardOwner", source);
        Assert.Contains("MatchesData(entry.Format, entry.Data)", source);
        Assert.Contains("allowed.Contains(format)", source);
        Assert.Contains("publication.Transaction.ToByteArray()", source);
        Assert.DoesNotContain("CaptureBeforeClose(", source);
    }

    private sealed class Clipboard
    {
        internal uint Sequence = 41;
        internal bool Locked = true;
        internal bool SameOwner = true, SameTransaction = true, SamePayload = true, KnownFormats = true;
        internal bool OpenAvailable = true, MaterializeAvailable = true, SequenceThrows;
        internal List<string> Trace = [];
        internal Action? AfterClose;
        private bool _dirty = true;
        internal uint? Seal() => ClipboardPublication.CloseAndCapture(Close, Open, Recognize,
            Materialize, ReadSequence, () => Close());
        private bool Close()
        {
            Assert.True(Locked); Trace.Add("close");
            if (_dirty) { Sequence++; _dirty = false; }
            Locked = false; AfterClose?.Invoke(); return true;
        }
        private bool Open()
        {
            Assert.False(Locked); Trace.Add("open");
            if (!OpenAvailable) return false;
            Locked = true; return true;
        }
        private bool Recognize()
        {
            Assert.True(Locked); Trace.Add("recognize");
            return SameOwner && SameTransaction && SamePayload && KnownFormats;
        }
        private bool Materialize()
        {
            Assert.True(Locked); Trace.Add("materialize"); _dirty = true;
            return MaterializeAvailable;
        }
        private uint ReadSequence()
        {
            Assert.True(Locked); Trace.Add("sequence");
            if (SequenceThrows) throw new InvalidOperationException("synthetic sequence failure");
            return Sequence;
        }
    }
}
