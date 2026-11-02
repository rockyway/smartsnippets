using SmartSnippets.Uno.Services;

namespace SmartSnippets.Uno.Tests.Flows;

public sealed class DailyStartupTests
{
    [Fact]
    public void EnableReadDisable_UsesQuotedCurrentPathAndAppValueOnly()
    {
        var values = new Dictionary<string, object?> { ["UnrelatedApp"] = "unrelated sentinel" };
        const string exe = @"C:\synthetic folder\SmartSnippets.exe";
        var service = new StartupRegistration(() => exe, name => values.GetValueOrDefault(name),
            (name, command) => values[name] = command, name => values.Remove(name));
        Assert.False(service.IsEnabled());
        Assert.True(service.Enable().Success);
        Assert.Equal("\"" + exe + "\" --background", values[StartupRegistration.ValueName]);
        Assert.True(service.IsEnabled());
        values[StartupRegistration.ValueName] = "prefix " + exe; // Merely containing the path is not our registration.
        Assert.False(service.IsEnabled());
        Assert.True(service.Disable().Success);
        Assert.False(service.IsEnabled());
        Assert.Equal("unrelated sentinel", Assert.Single(values).Value);
        Assert.True(service.Disable().Success);
    }

    [Fact]
    public void MissingPathAndAccessFailures_ReportFailureWithoutClaimingEnabled()
    {
        var missing = new StartupRegistration(() => null, _ => null,
            (_, _) => throw new InvalidOperationException("Must not write"), _ => { });
        Assert.False(missing.Enable().Success);
        var refused = new StartupRegistration(() => "synthetic.exe",
            _ => throw new UnauthorizedAccessException(), (_, _) => throw new IOException(),
            _ => throw new UnauthorizedAccessException());
        Assert.False(refused.IsEnabled());
        Assert.False(refused.Enable().Success);
        Assert.False(refused.Disable().Success);
    }
}
