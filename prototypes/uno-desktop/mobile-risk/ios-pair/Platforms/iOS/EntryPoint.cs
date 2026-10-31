using Uno.UI.Hosting;

namespace SmartSnippets.IosPair;

public static class EntryPoint
{
    public static void Main(string[] args) => UnoPlatformHostBuilder.Create()
        .App(() => new App())
        .UseAppleUIKit()
        .Build()
        .Run();
}
