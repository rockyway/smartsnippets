namespace SmartSnippets.Uno;

/// <summary>Restart handoff arguments shared by the elevation launcher and process entry point.</summary>
internal static class AdministratorRestart
{
    internal static string Arguments(int processId, bool mainHidden) =>
        $"--after-pid {processId}" + (mainHidden ? " --background" : "");

    internal static int? PreviousProcessId(string[] commandLine)
    {
        var index = Array.FindIndex(commandLine, arg => string.Equals(arg, "--after-pid", StringComparison.OrdinalIgnoreCase));
        return index >= 0 && index + 1 < commandLine.Length && int.TryParse(commandLine[index + 1], out var pid) && pid > 0
            ? pid : null;
    }
}
