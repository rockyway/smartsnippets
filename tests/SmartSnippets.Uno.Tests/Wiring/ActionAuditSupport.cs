using CommunityToolkit.Mvvm.Input;
using SmartSnippets.Presentation.Services;
using SmartSnippets.Uno.Platforms.Windows;

namespace SmartSnippets.Uno.Tests.Wiring;

internal static class ActionAuditSupport
{
    internal static async Task Execute(System.Windows.Input.ICommand command)
    {
        Assert.True(command.CanExecute(null));
        if (command is IAsyncRelayCommand asyncCommand) await asyncCommand.ExecuteAsync(null);
        else command.Execute(null);
    }
}

// Replaces only the explicitly owned OS boundary. The candidate gate and InsertionService stay real.
internal sealed class ActionAuditNative : ICandidateNativeOperations
{
    internal InsertionTarget Target { get; } = new(123, 456, 789, "synthetic");
    internal int Copies, Sends;
    internal DispatchState Dispatch = DispatchState.Dispatched;
    internal SwapStatus Swap = SwapStatus.Swapped;
    internal RestoreStatus Restore = RestoreStatus.Restored;
    internal bool CopyResult = true;
    public bool IsAvailable => true;
    public void SetText(string text) => TrySetText(text);
    public bool TrySetText(string text) { Copies++; return CopyResult; }
    public bool Validate(CandidateNativeOperation operation, Guid sessionId, nint window, uint processId) => window == Target.WindowHandle && processId == Target.ProcessId;
    public InsertionTarget? CaptureOwnedTarget(nint window, uint processId) => Target;
    public Task<bool> DrainOwnedRecoveryAsync() => Task.FromResult(true);
    public Task<bool> WaitForCopyAdmissionAsync(CancellationToken ct) => Task.FromResult(true);
    public bool TryFocusOwnedPopup() => true;
    public TargetCheck Check(InsertionTarget target) => TargetCheck.Ok;
    public bool IsTargetForeground(InsertionTarget target) => true;
    public Task<FocusPath?> RestoreFocusAsync(InsertionTarget target, CancellationToken ct) => Task.FromResult<FocusPath?>(FocusPath.Direct);
    public bool ReleaseHeldModifiers() => true;
    public bool ReleasePasteKeys() => true;
    public SwapStatus TryPlaceText(string text) => SwapStatus.Swapped;
    public ClipboardSwap TrySwapClipboard(string text, bool allowIncomplete) => new(Swap, Swap == SwapStatus.Swapped ? new Snapshot() : null);
    public DispatchState SendPaste() { Sends++; return Dispatch; }
    public RestoreStatus TryRestoreClipboard(IClipboardSnapshot snapshot) => Restore;
    public void DiscardClipboardSnapshot(IClipboardSnapshot snapshot) { }
    private sealed class Snapshot : IClipboardSnapshot
    {
        public int SavedCount => 1;
        public int SkippedByDesignCount => 0;
        public int UnsavedCount => 0;
        public bool CapExceeded => false;
        public long ByteCount => 8;
    }
}
