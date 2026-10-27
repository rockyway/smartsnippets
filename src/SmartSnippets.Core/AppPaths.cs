namespace SmartSnippets.Core;

public sealed class AppPaths
{
    public AppPaths()
        : this(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SmartSnippets"))
    {
    }

    public AppPaths(string root)
    {
        Root = root;
    }

    public string Root { get; }

    public string DatabasePath => Path.Combine(Root, "smartsnippets.db");

    public string LogDirectory => Path.Combine(Root, "logs");
}
