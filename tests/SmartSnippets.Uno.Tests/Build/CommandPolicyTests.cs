namespace SmartSnippets.Uno.Tests.Build;

public sealed class CommandPolicyTests
{
    [Fact]
    public void Commands_UsePinnedCwdAndRid()
    {
        var root = BuildGraphTests.RepoRoot;
        var workflow = File.ReadAllText(Path.Combine(root, ".github/workflows/ci.yml"));
        var steps = workflow.Split("      - name:");
        var test = Assert.Single(steps, s => s.Contains("dotnet test ../../tests/SmartSnippets.Uno.Tests"));
        Assert.Contains("working-directory: src/SmartSnippets.Uno", test);
        var publish = File.ReadAllText(Path.Combine(root, "scripts/publish-uno-aot.ps1"));
        var cwd = publish.IndexOf("Set-Location -LiteralPath $app", StringComparison.Ordinal);
        var restoreIndex = publish.IndexOf("dotnet restore SmartSnippets.Uno.csproj -r win-x64", StringComparison.Ordinal);
        var publishIndex = publish.IndexOf("dotnet publish SmartSnippets.Uno.csproj -c Release -r win-x64 --no-restore", StringComparison.Ordinal);
        Assert.True(cwd >= 0 && restoreIndex > cwd && publishIndex > restoreIndex);
        Assert.Contains("Set-Location -LiteralPath $previous.Path", publish);
        Assert.Contains("global-json-file: src/SmartSnippets.Uno/global.json", workflow);
    }
}
