using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using SmartSnippets.Presentation.Navigation;

namespace SmartSnippets.Uno;

// Test-only executable: no general profile argument/environment override or default-profile fallback.
public static class Program
{
    private static AppInstance? _instance;
    internal static bool StartInBackground => false;
    internal static LaunchOptions InitialOptions => new(SettingsScene ? PageKind.Settings : FolderScene ? PageKind.Folders : PageKind.Snippets, false, null);
    internal static string PrivateRoot { get; private set; } = "";
    internal static string ProfileRoot => Path.Combine(PrivateRoot, "fixture-profile");
    internal static bool RenderedUiMode { get; private set; }
    internal static bool FolderScene { get; private set; }
    internal static bool SettingsScene { get; private set; }
    internal static bool FilePickerScene { get; private set; }

    [STAThread]
    private static void Main(string[] args)
    {
        if (args.Length != 0) throw new InvalidOperationException("fixture_arguments");
        PrivateRoot = Path.GetFullPath(Environment.CurrentDirectory);
        var name = Path.GetFileName(PrivateRoot);
        if (!System.Text.RegularExpressions.Regex.IsMatch(name, "^ss-uno018-ui-[a-zA-Z0-9_-]{8,80}$")
            || Directory.Exists(ProfileRoot) || File.Exists(ProfileRoot)
            || !File.Exists(Path.Combine(PrivateRoot, "fixture-launch-permit")))
            throw new InvalidOperationException("fixture_ownership");
        for (var directory = new DirectoryInfo(PrivateRoot); directory is not null; directory = directory.Parent)
            if ((directory.Attributes & FileAttributes.ReparsePoint) != 0)
                throw new InvalidOperationException("fixture_alias");
        var uiPermit = Path.Combine(PrivateRoot, "fixture-rendered-ui-permit");
        if (File.Exists(uiPermit))
        {
            var info = new FileInfo(uiPermit);
            if ((info.Attributes & FileAttributes.ReparsePoint) != 0 || info.Length != 22
                || File.ReadAllText(uiPermit) != "owned_rendered_ui_019\n")
                throw new InvalidOperationException("fixture_ui_permit");
            RenderedUiMode = true;
        }
        var folderPermit = Path.Combine(PrivateRoot, "fixture-folder-scene-permit");
        if (File.Exists(folderPermit))
        {
            var info = new FileInfo(folderPermit);
            if (!RenderedUiMode || (info.Attributes & FileAttributes.ReparsePoint) != 0
                || info.Length != 20 || File.ReadAllText(folderPermit) != "owned_folder_ui_019\n")
                throw new InvalidOperationException("fixture_folder_permit");
            FolderScene = true;
        }
        var settingsPermit = Path.Combine(PrivateRoot, "fixture-settings-scene-permit");
        if (File.Exists(settingsPermit))
        {
            var info = new FileInfo(settingsPermit);
            if (!RenderedUiMode || FolderScene || (info.Attributes & FileAttributes.ReparsePoint) != 0
                || info.Length != 22 || File.ReadAllText(settingsPermit) != "owned_settings_ui_019\n")
                throw new InvalidOperationException("fixture_settings_permit");
            SettingsScene = true;
        }
        var pickerPermit = Path.Combine(PrivateRoot, "fixture-file-picker-scene-permit");
        if (File.Exists(pickerPermit))
        {
            var info = new FileInfo(pickerPermit);
            if (!RenderedUiMode || FolderScene || SettingsScene || (info.Attributes & FileAttributes.ReparsePoint) != 0
                || info.Length != 25)
                throw new InvalidOperationException("fixture_picker_permit");
            FilePickerAssertDriver.RequirePickerPermit(pickerPermit);
            FilePickerScene = true;
        }
        WinRT.ComWrappersSupport.InitializeComWrappers();
        _instance = AppInstance.FindOrRegisterForKey("SmartSnippets.UnoAcceptanceFixture019." + name);
        if (!_instance.IsCurrent) throw new InvalidOperationException("fixture_instance");
        Application.Start(parameters =>
        {
            SynchronizationContext.SetSynchronizationContext(new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread()));
            _ = new App();
        });
    }
    internal static void StopActivations() { _instance?.UnregisterKey(); _instance = null; }
    internal static void Receipt(string phase) => File.WriteAllText(Path.Combine(PrivateRoot, phase + ".receipt"),
        $"{Environment.ProcessId}\n{Path.GetFileName(PrivateRoot)}\n{phase}\n");
    internal static async Task WaitAsync(string phase)
    {
        var deadline = DateTime.UtcNow.AddSeconds(FilePickerScene ? 25 : 12);
        while (!File.Exists(Path.Combine(PrivateRoot, phase + ".continue")))
        {
            if (DateTime.UtcNow >= deadline) throw new TimeoutException("fixture_observer");
            await Task.Delay(50);
        }
    }
}
