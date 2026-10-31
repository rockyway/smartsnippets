using System;
using System.Threading;
using System.Threading.Tasks;
using SmartSnippets.Presentation.Services;

namespace SmartSnippets.UnoProbe;

/// <summary>Injection seam only: implementations must enforce owned exact-window identity,
/// authorization, safe clipboard preservation/recovery and final dispatch validation.
/// No implementation is supplied or enabled by the default Uno flow mode.</summary>
public interface IGuardedProbeNativeAdapter : ITargetProbe, IPasteDispatcher, IClipboardService
{
    InsertionTarget? CaptureTarget();
}

/// <summary>Only OS/native-window services are substituted by portable flow tests.
/// Defaults refuse capture, focus, clipboard and input. A guarded native adapter
/// must explicitly supply these operations; this is not the shipping adapter.</summary>
public abstract class ProbeFlowBoundary : ITargetProbe, IPasteDispatcher, IClipboardService,
    IElevation, IDialogService, INotifier
{
    private readonly IGuardedProbeNativeAdapter? _native;
    protected ProbeFlowBoundary(IGuardedProbeNativeAdapter? native = null) => _native = native;
    public virtual InsertionTarget? CaptureTarget() => _native?.CaptureTarget();
    public abstract void StartWarmup();
    public abstract void StopWarmup();
    public abstract void ShowPopup();
    public abstract void HidePopup();
    public abstract void ShowMain();
    public abstract void Focus(PopupFocusTarget target);
    public virtual TargetCheck Check(InsertionTarget target) => _native?.Check(target) ?? TargetCheck.Closed;
    public virtual bool IsTargetForeground(InsertionTarget target) => _native?.IsTargetForeground(target) == true;
    public virtual Task<FocusPath?> RestoreFocusAsync(InsertionTarget target, CancellationToken ct) => _native?.RestoreFocusAsync(target, ct) ?? Task.FromResult<FocusPath?>(null);
    public virtual bool ReleaseHeldModifiers() => _native?.ReleaseHeldModifiers() == true;
    public virtual ClipboardSwap TrySwapClipboard(string text, bool allowIncomplete) => _native?.TrySwapClipboard(text, allowIncomplete) ?? new(SwapStatus.NotPreserved, null);
    public virtual SwapStatus TryPlaceText(string text) => _native?.TryPlaceText(text) ?? SwapStatus.Failed;
    public virtual DispatchState SendPaste() => _native?.SendPaste() ?? DispatchState.NotDispatched;
    public virtual bool ReleasePasteKeys() => _native?.ReleasePasteKeys() == true;
    public virtual RestoreStatus TryRestoreClipboard(IClipboardSnapshot snapshot) => _native?.TryRestoreClipboard(snapshot) ?? RestoreStatus.Failed;
    public virtual void DiscardClipboardSnapshot(IClipboardSnapshot snapshot)
    {
        if (_native is null) throw new InvalidOperationException("Native adapter unavailable.");
        _native.DiscardClipboardSnapshot(snapshot);
    }
    public virtual void SetText(string text)
    {
        if (_native is null) throw new InvalidOperationException("Clipboard adapter unavailable.");
        _native.SetText(text);
    }
    public bool IsElevated => false;
    public ElevationResult RestartAsAdministrator() => new(ElevationOutcome.Failed, "Unavailable in flow probe.");
    public virtual void Notify(string title, string message) { }
    public Task<bool> ConfirmAsync(string title, string message, string confirmText) => Task.FromResult(false);
    public Task<SaveChoice> AskSaveDiscardCancelAsync(string title, string message) => Task.FromResult(SaveChoice.Cancel);
    public Task ShowErrorAsync(string title, string message) => Task.CompletedTask;
    public Task ShowInfoAsync(string title, string message) => Task.CompletedTask;
}
