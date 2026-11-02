namespace SmartSnippets.Uno.Services;

internal interface IMessageWindowNative
{
    bool RegisterClass();
    nint CreateWindow();
    bool DestroyWindow(nint handle);
    bool UnregisterClass();
}

/// <summary>Owns exactly one registration. Never adopts a pre-existing native class.</summary>
internal sealed class MessageWindowLifetime : IDisposable
{
#if SMARTSNIPPETS_MEASURE
    internal const string ClassName = "SmartSnippets.Measure.MessageWindow";
#else
    internal const string ClassName = "SmartSnippets.MessageWindow";
#endif
    private readonly IMessageWindowNative _native;
    private bool _registered;
    internal nint Handle { get; private set; }
    internal MessageWindowLifetime(IMessageWindowNative native)
    {
        _native = native;
        if (!native.RegisterClass()) throw new InvalidOperationException("App message class registration failed.");
        _registered = true;
        try
        {
            Handle = native.CreateWindow();
            if (Handle == 0) throw new InvalidOperationException("App message window creation failed.");
        }
        catch { Dispose(); throw; }
    }
    public void Dispose()
    {
        var handle = Handle;
        var registered = _registered;
        Handle = 0; _registered = false;
        var failed = false;
        try { if (handle != 0 && !_native.DestroyWindow(handle)) failed = true; }
        catch { failed = true; }
        try { if (registered && !_native.UnregisterClass()) failed = true; }
        catch { failed = true; }
        if (failed) throw new InvalidOperationException("App message cleanup failed; native cleanup is not verified.");
    }
}
