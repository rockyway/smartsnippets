using Xunit;

namespace SmartSnippets.Uno;

public sealed class WindowDiagnosticFactsTests
{
    [Fact]
    public void Utf16ClassFingerprintAndSharedTableArePinned()
    {
        Assert.Equal("fd2fa13c", WindowDiagnosticFacts.ClassSha8("Edit"));
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "scripts", "run_uno_candidate_native.py")))
            directory = directory.Parent;
        Assert.NotNull(directory);
        var source = File.ReadAllText(Path.Combine(directory.FullName, "scripts", "run_uno_candidate_native.py"));
        var table = source.Split("WINDOW_CLASSES = {")[1].Split("}")[0];
        foreach (System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(table, "'([^']+)': '([^']+)'"))
            Assert.Equal(m.Groups[2].Value, WindowDiagnosticFacts.Classify(m.Groups[1].Value));
        Assert.Equal("dotnet_broadcast", WindowDiagnosticFacts.Classify(".NET-BroadcastEventWindow.synthetic"));
        Assert.Equal("microsoft_content", WindowDiagnosticFacts.Classify("Microsoft.UI.Content.synthetic"));
        Assert.Equal("soft_keyboard", WindowDiagnosticFacts.Classify("SoftKeyboardSynthetic"));
    }

    [Theory]
    [InlineData("IME", "ime_default")]
    [InlineData("MSCTFIME UI", "msctfime_ui")]
    [InlineData("OleMainThreadWndClass", "ole_main_thread")]
    [InlineData("CicMarshalWndClass", "cicmarshal")]
    [InlineData("UserAdapterWindowClass", "user_adapter")]
    [InlineData("tooltips_class32", "tooltip")]
    [InlineData("#32770", "dialog_32770")]
    [InlineData("#32768", "menu_32768")]
    [InlineData("SS_Target018_0123456789abcdef0123456789abcdef", "target018_class")]
    [InlineData("SS_Target018_private title", "other")]
    [InlineData("private title", "other")]
    public void CaptureReducesClassAndNeverEmitsRawName(string name, string token)
    {
        var facts = WindowDiagnosticFacts.Capture(3, 1, 2, _ => name, _ => true,
            _ => 0x08000080, _ => 2, _ => true, _ => 5, _ => 2);
        Assert.Equal(token, facts.ClassToken);
        Assert.Equal(WindowDiagnosticFacts.ClassSha8(name), facts.ClassSha);
        using var json = System.Text.Json.JsonDocument.Parse(facts.Json());
        Assert.Equal(facts.ClassSha, json.RootElement.GetProperty("class_sha8").GetString());
        Assert.True(json.RootElement.GetProperty("sampled").GetBoolean());
        Assert.Equal(26, json.RootElement.EnumerateObject().Count());
        Assert.True(facts.Visible && facts.Toolwindow && facts.Noactivate && facts.HasOwner &&
            facts.OwnerIsB && facts.IsMessageOnly && facts.SameThreadAsA && facts.Cloaked && facts.CloakedKnown);
        Assert.False(facts.OwnerIsA);
        Assert.Contains("class_token=" + token, facts.Fields());
        Assert.DoesNotContain(name, facts.Fields());
    }

    [Theory]
    [InlineData(0x80, true, false)]
    [InlineData(0x08000000, false, true)]
    public void ExtendedStyleFlagsAreIndependentBits(long style, bool toolwindow, bool noactivate)
    {
        var facts = WindowDiagnosticFacts.Capture(3, 1, 2, _ => "IME", _ => true,
            _ => style, _ => 0, _ => false, _ => 0, _ => 0);
        Assert.Equal(toolwindow, facts.Toolwindow);
        Assert.Equal(noactivate, facts.Noactivate);
    }

    [Fact]
    public void KnownZeroCloakIsDifferentFromUnavailableCloak()
    {
        var knownZero = WindowDiagnosticFacts.Capture(3, 1, 2, _ => "IME", _ => true,
            _ => 0, _ => 0, _ => false, _ => 0, _ => 0);
        var unavailable = WindowDiagnosticFacts.Capture(3, 1, 2, _ => "IME", _ => true,
            _ => 0, _ => 0, _ => false, _ => 0, _ => null);
        Assert.False(knownZero.Cloaked);
        Assert.True(knownZero.CloakedKnown);
        Assert.False(unavailable.Cloaked);
        Assert.False(unavailable.CloakedKnown);
    }

    [Theory]
    [InlineData(0, 0, false, false)]
    [InlineData(1, 5, true, true)]
    [InlineData(2, 6, false, false)]
    public void OwnerThreadAndCloakAreIndependent(int owner, uint thread, bool ownerA, bool sameThread)
    {
        var facts = WindowDiagnosticFacts.Capture(3, 1, 2, _ => "private", _ => false,
            _ => 0, _ => owner, _ => false, h => h == 1 ? 5u : thread, _ => null);
        Assert.Equal(ownerA, facts.OwnerIsA);
        Assert.Equal(owner == 2, facts.OwnerIsB);
        Assert.Equal(owner != 0, facts.HasOwner);
        Assert.Equal(sameThread, facts.SameThreadAsA);
        Assert.False(facts.Visible || facts.Toolwindow || facts.Noactivate || facts.IsMessageOnly || facts.Cloaked || facts.CloakedKnown);
        Assert.Contains("class_token=other", (facts with { ClassToken = "private title" }).Fields());
    }

    [Fact]
    public void StartupAndTraceUseSameSampledWindowAndDistinguishBWithoutAdmission()
    {
        var sampled = new List<nint>();
        WindowDiagnosticFacts Sample(nint h)
        {
            sampled.Add(h);
            return WindowDiagnosticFacts.Capture(h, 1, 2, _ => "IME", _ => true,
                _ => 0, _ => 0, _ => false, _ => 7, _ => 0);
        }
        var startup = new NativeStartupDiagnostics(() => 2, _ => 10, 50, () => 1, () => 2, Sample);
        var trace = new OwnedForegroundTrace(() => 2, _ => 10, 1, 10, 20, 30, 40, 50, 7,
            _ => throw new Exception("B must not be classified as other"), () => 0, () => Task.CompletedTask, 2, Sample);
        foreach (var record in new[] { startup.Sample("after_focus"), trace.Sample(1, 0) })
        {
            Assert.Contains("relation=owned_b", record);
            Assert.Contains("class_token=ime_default visible=True", record);
            Assert.DoesNotContain("a_other_window", record);
        }
        Assert.Equal(new nint[] { 2, 2 }, sampled);
    }
}
