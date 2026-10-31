using SmartSnippets.Core.Logging;
using SmartSnippets.Presentation.Services;

namespace SmartSnippets.Uno.Platforms.Windows;

[Flags]
internal enum CandidateNativeOperation
{
    Capture = 1, Insert = 2, Copy = 4, Focus = 8, Input = 16, Clipboard = 32, ClipboardLoss = 64
}

/// <summary>
/// Internal harness injection only, never constructed from launch options, settings or environment.
/// Validate must recheck the session's owned HWND/PID, desktop/input safety and recovery readiness
/// before inspection or mutation. Capture may inspect only that validated exact window.
/// Recovery must retain independent owned backups until settled, including after authorization expires.
/// </summary>
internal interface ICandidateNativeOperations : ITargetProbe, IPasteDispatcher, IClipboardService
{
    bool Validate(CandidateNativeOperation operation, Guid sessionId, nint window, uint processId);
    InsertionTarget? CaptureOwnedTarget(nint window, uint processId);
    Task<bool> DrainOwnedRecoveryAsync();
}

/// <summary>One revocable, operation-scoped, monotonic bounded lease for one harness-owned target.</summary>
internal sealed class CandidateNativeCapability
{
    private readonly TimeProvider _clock;
    private readonly long _started;
    private readonly TimeSpan _lifetime;
    private readonly CandidateNativeOperation _permissions;
    private bool _revoked, _expiryReported;
    internal Guid SessionId { get; }
    internal nint Window { get; }
    internal uint ProcessId { get; }
    internal ICandidateNativeOperations Operations { get; }
    internal event Action? AuthorizationChanged;

    internal CandidateNativeCapability(Guid sessionId, nint window, uint processId,
        CandidateNativeOperation permissions, TimeSpan lifetime, TimeProvider clock,
        ICandidateNativeOperations operations)
    {
        if (sessionId == Guid.Empty || window == 0 || processId == 0 || lifetime <= TimeSpan.Zero)
            throw new ArgumentException("An owned session, exact target and finite positive lifetime are required.");
        SessionId = sessionId; Window = window; ProcessId = processId;
        _permissions = permissions; _lifetime = lifetime; _clock = clock;
        _started = clock.GetTimestamp(); Operations = operations;
    }

    internal void Revoke()
    {
        if (_revoked) return;
        _revoked = true;
        AppLog.Info("candidate_native_revoked");
        AuthorizationChanged?.Invoke();
    }
    internal bool Allows(CandidateNativeOperation operation)
    {
        if ((_permissions & operation) != operation || !IsLive()) return false;
        try { return Operations.Validate(operation, SessionId, Window, ProcessId) && IsLive(); }
        catch { return false; }
    }
    private bool IsLive()
    {
        if (_clock.GetElapsedTime(_started) >= _lifetime)
        {
            if (!_expiryReported)
            {
                _expiryReported = true;
                AppLog.Info("candidate_native_expired");
                AuthorizationChanged?.Invoke();
            }
            return false;
        }
        return !_revoked;
    }
    internal bool Owns(InsertionTarget target) => target.WindowHandle == Window && target.ProcessId == ProcessId;
}
