using SmartSnippets.Uno.Services;
using SmartSnippets.Uno.Measurement;

namespace SmartSnippets.Uno;

internal sealed partial class UnoAppHost
{
    private readonly IdleTrimSchedule _idleTrimSchedule = new();
    private void ObserveMeasurementIdle()
    {
        if ((!MeasurementOptions.Trim && !MeasurementOptions.Release) || _disposed || _exiting || _measurementBusy) return;
        // All observations and collection run on the owning UI thread. Pending previews/capture,
        // editors and management operations cancel trimming; popup readiness is never changed.
        var main = (boundary as WindowsHostBoundary)?.MainWindow;
        var idle = main is { IsHidden: true } && !_popupVisible && !_previewActive && !_pendingCaptured
            && Gate.IsIdle && !Popup.IsEditorOpen && Snippets.Editor?.IsDirty != true
            && !ManagementTasks().Any(task => !task.IsCompleted);
        if (idle && MeasurementOptions.Release) main!.ReleaseMeasurementPages();
        if (!MeasurementOptions.Trim || !_idleTrimSchedule.Poll(Environment.TickCount64, idle)) return;
        var success = IdleMemoryTrimmer.TrimNative();
        // Fixed, content-free receipt: failed native trim must not be mistaken for a measured success.
        File.WriteAllText(Path.Combine(MeasurementProfile.Root, "trim-result"), success ? "ok" : "failed");
    }

}
