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
    internal static readonly TimeSpan MaxLifetime = TimeSpan.FromMinutes(5);
    private readonly object _liveness = new();
    private readonly TimeProvider _clock;
    private readonly ITimer _expiryTimer;
    private readonly long _started, _frequency;
    private long _lastTimestamp;
    private readonly TimeSpan _lifetime;
    private readonly CandidateNativeOperation _permissions;
    private bool _inactive;
    internal Guid SessionId { get; }
    internal nint Window { get; }
    internal uint ProcessId { get; }
    internal ICandidateNativeOperations Operations { get; }
    internal event Action? AuthorizationChanged;

    internal CandidateNativeCapability(Guid sessionId, nint window, uint processId,
        CandidateNativeOperation permissions, TimeSpan lifetime, TimeProvider clock,
        ICandidateNativeOperations operations)
    {
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(operations);
        if (sessionId == Guid.Empty || window == 0 || processId == 0 || lifetime <= TimeSpan.Zero || lifetime > MaxLifetime)
            throw new ArgumentException("An owned session, exact target and bounded positive lifetime are required.");
        _frequency = clock.TimestampFrequency;
        if (_frequency <= 0) throw new ArgumentException("A monotonic clock with positive frequency is required.", nameof(clock));
        SessionId = sessionId; Window = window; ProcessId = processId;
        _permissions = permissions; _lifetime = lifetime; _clock = clock;
        _started = _lastTimestamp = clock.GetTimestamp(); Operations = operations;
        _expiryTimer = clock.CreateTimer(_ => ObserveExpiry(), null, lifetime, Timeout.InfiniteTimeSpan);
    }

    internal void Revoke() => Deactivate("candidate_native_revoked");
    private void ObserveExpiry()
    {
        try
        {
            if (!IsLive()) return;
            // Timer resolution can wake just before the monotonic deadline.
            lock (_liveness)
            {
                if (!_inactive)
                    _expiryTimer?.Change(_lifetime - _clock.GetElapsedTime(_started, _lastTimestamp), Timeout.InfiniteTimeSpan);
            }
        }
        catch { Deactivate("candidate_native_expired"); }
    }
    private void Deactivate(string eventName)
    {
        lock (_liveness)
        {
            if (_inactive) return;
            _inactive = true;
        }
        NotifyDeactivated(eventName);
    }
    private void NotifyDeactivated(string eventName)
    {
        try { _expiryTimer?.Dispose(); }
        catch { AppLog.Info("candidate_expiry_timer_cleanup_failed"); }
        AppLog.Info(eventName);
        // Observers must not turn refusal into an exception, or prevent other observers refreshing.
        if (AuthorizationChanged is not { } changed) return;
        foreach (var handler in changed.GetInvocationList())
        {
            try { ((Action)handler)(); }
            catch { AppLog.Info("candidate_authorization_notification_failed"); }
        }
    }
    internal bool Allows(CandidateNativeOperation operation)
    {
        try
        {
            if (!IsLive() || (_permissions & operation) != operation) return false;
            return Operations.Validate(operation, SessionId, Window, ProcessId) && IsLive();
        }
        catch
        {
            // Clock or validation faults revoke rather than letting a later check revive the lease.
            Deactivate("candidate_native_expired");
            return false;
        }
    }
    private bool IsLive()
    {
        lock (_liveness)
        {
            if (_inactive) return false;
            var timestamp = _clock.GetTimestamp();
            var elapsed = _clock.GetElapsedTime(_started, timestamp);
            if (_clock.TimestampFrequency == _frequency && timestamp >= _lastTimestamp
                && elapsed >= TimeSpan.Zero && elapsed < _lifetime)
            {
                _lastTimestamp = timestamp;
                return true;
            }
            _inactive = true;
        }
        NotifyDeactivated("candidate_native_expired");
        return false;
    }
    internal bool Owns(InsertionTarget target) => target.WindowHandle == Window && target.ProcessId == ProcessId;
}
