namespace Microsoft.UI.Dispatching
{
    public sealed class DispatcherQueue
    {
        public static bool HasCurrentThread { get; set; } = true;
        public static DispatcherQueue? GetForCurrentThread() => HasCurrentThread ? new() : null;
    }
}
namespace Windows.ApplicationModel.DataTransfer
{
    public sealed class DataPackage
    {
        public string? Text { get; private set; }
        public void SetText(string text) => Text = text;
    }
    public static class Clipboard
    {
        public static List<string> Calls { get; } = [];
        public static string? Text { get; private set; }
        public static string? FailAt { get; set; }
        public static void SetContent(DataPackage package) { Record("SetContent"); Text = package.Text; }
        public static void Flush() => Record("Flush");
        private static void Record(string operation)
        {
            Calls.Add(operation);
            if (FailAt == operation) throw new System.Runtime.InteropServices.COMException("PRIVATE-PAYLOAD");
        }
    }
}
