namespace SmartSnippets.Uno.Services;

internal interface ICandidateMessageWindowNative
{
    bool RegisterClass();
    nint CreateWindow();
    bool DestroyWindow(nint handle);
    bool UnregisterClass();
}

/// <summary>Owns exactly one registration. Never adopts a pre-existing native class.</summary>
internal sealed class CandidateMessageWindowLifetime : IDisposable
{
    internal const string ClassName = "SmartSnippets.UnoCandidate019.MessageWindow";
    private readonly ICandidateMessageWindowNative _native;
    private bool _registered;
    internal nint Handle { get; private set; }
    internal CandidateMessageWindowLifetime(ICandidateMessageWindowNative native)
    {
        _native = native;
        if (!native.RegisterClass()) throw new InvalidOperationException("Candidate message class registration failed.");
        _registered = true;
        try
        {
            Handle = native.CreateWindow();
            if (Handle == 0) throw new InvalidOperationException("Candidate message window creation failed.");
        }
        catch { Dispose(); throw; }
    }
    public void Dispose()
    {
        if (Handle != 0)
        {
            if (!_native.DestroyWindow(Handle)) throw new InvalidOperationException("Candidate message window cleanup failed.");
            Handle = 0;
        }
        if (_registered)
        {
            if (!_native.UnregisterClass()) throw new InvalidOperationException("Candidate message class cleanup failed.");
            _registered = false;
        }
    }
}
