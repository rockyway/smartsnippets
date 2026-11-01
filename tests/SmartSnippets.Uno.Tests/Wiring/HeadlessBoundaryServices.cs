// External platform services used by the linked WindowsCandidateBoundary. The action
// audit uses its real CreateMain/CreatePopup factories; no OS registrations are made.
using SmartSnippets.Core.Settings;
using SmartSnippets.Presentation.Navigation;
using SmartSnippets.Presentation.Services;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

namespace SmartSnippets.Uno
{
    internal static class Program
    {
        internal static bool StartInBackground => true;
        internal static LaunchOptions InitialOptions => new(null, false, null);
        internal static void StopActivations() { }
    }
}
namespace SmartSnippets.Uno.Services
{
    internal sealed class UiDispatcher(DispatcherQueue queue) : IUiDispatcher { public void Post(Action action) => queue.TryEnqueue(action); }
    internal sealed class MessageWindow : IDisposable { public void Dispose() { } }
    internal sealed class TrayIcon(MessageWindow window, Action open, Action preview, Action settings, Action restart, Action exit, bool elevated, Func<object?> target) : IDisposable
    {
        internal TrayState State { get; set; }
        internal void Add() { _ = window; _ = open; _ = preview; _ = settings; _ = restart; _ = exit; _ = elevated; _ = target; }
        internal void ShowBalloon(string title, string message) { }
        public void Dispose() { }
    }
    internal sealed class ThemeService(ThemeMode mode, ColorPalette palette) : IThemeService
    {
        public ThemeMode Current { get; private set; } = mode;
        public ColorPalette CurrentPalette { get; private set; } = palette;
        public void Apply(ThemeMode value) => Current = value;
        public void ApplyPalette(ColorPalette value) => CurrentPalette = value;
        internal void Register(Window window) { }
        internal void ObserveSystemMessages(MessageWindow window) { }
    }
    internal sealed class DialogService : IDialogService
    {
        internal DialogService(Func<XamlRoot?> root, Action activate, ITextSizeService textSize, IThemeService? theme, Func<bool> visible) { }
        public Task<bool> ConfirmAsync(string title, string message, string confirmText) => throw new NotSupportedException();
        public Task<SaveChoice> AskSaveDiscardCancelAsync(string title, string message) => throw new NotSupportedException();
        public Task ShowErrorAsync(string title, string message) => throw new NotSupportedException();
        public Task ShowInfoAsync(string title, string message) => throw new NotSupportedException();
    }
    internal sealed class SnippetFileService : ISnippetFileService
    {
        internal SnippetFileService(Func<int?> owner) { }
        public Task<string?> PickImportFileAsync() => throw new NotSupportedException();
        public Task<string?> PickExportFileAsync(string suggestedBaseName) => throw new NotSupportedException();
        public Stream OpenRead(string path) => throw new NotSupportedException();
        public Task WriteAtomicallyAsync(string path, ReadOnlyMemory<byte> content) => throw new NotSupportedException();
    }
}
namespace Windows.Win32.UI.WindowsAndMessaging { public enum MESSAGEBOX_STYLE { MB_OK } }
