using System.Runtime.InteropServices;

namespace SmartSnippets.TargetIdentity018;

internal sealed partial class MacWindowIdentity : IExactWindowIdentity
{
    private const string AX = "/System/Library/Frameworks/ApplicationServices.framework/ApplicationServices";
    private const string CF = "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";
    private const string ObjC = "/usr/lib/libobjc.A.dylib";
    private static long nextIdentity;
    private readonly nint tokenA = (nint)Interlocked.Increment(ref nextIdentity);
    private readonly nint tokenB = (nint)Interlocked.Increment(ref nextIdentity);
    private readonly uint expectedPid;
    private readonly IOwnedFixtureLease lease;
    private readonly Dictionary<nint, nint> windows = [];
    // AppKit contains Objective-C classes. Unloading it can invalidate runtime class
    // registrations, so this one process-lifetime load is intentionally never freed.
    private static readonly Lazy<nint> AppKit = new(() => NativeLibrary.Load("/System/Library/Frameworks/AppKit.framework/AppKit"));
    private nint app;
    private bool disposed;

    internal MacWindowIdentity(IOwnedFixtureLease lease)
    {
        var expectedPid = lease.ExpectedPid;
        if (!OperatingSystem.IsMacOS() || !lease.IsAliveAndPinned() || expectedPid == 0 || expectedPid > int.MaxValue ||
            expectedPid == (uint)Environment.ProcessId) throw new InvalidOperationException("Owned Mac fixture required.");
        this.expectedPid = expectedPid;
        this.lease = lease;
        try
        {
            if (AXIsProcessTrusted() == 0) throw new InvalidOperationException("Accessibility unavailable.");
            _ = AppKit.Value;
            app = AXUIElementCreateApplication((int)expectedPid);
            if (app == 0 || AXUIElementSetMessagingTimeout(app, 1) != 0 || ElementPid(app) != expectedPid)
                throw new InvalidOperationException("Owned AX application unavailable.");
            using var live = Copy(app, "AXWindows");
            if (!IsWindowArray(live.Value) || CFArrayGetCount(live.Value) != 2)
                throw new InvalidOperationException("Two owned windows required.");
            for (nint index = 0; index < 2; index++)
            {
                var window = CFArrayGetValueAtIndex(live.Value, index);
                if (ElementPid(window) != expectedPid || AXUIElementSetMessagingTimeout(window, 1) != 0)
                    throw new InvalidOperationException("AX window owner refused.");
                using var identifier = Copy(window, "AXIdentifier");
                using var a = String("SSFixtureWindowA");
                using var b = String("SSFixtureWindowB");
                if (a.Value == 0 || b.Value == 0 || !lease.IsAliveAndPinned())
                    throw new InvalidOperationException("Owned AX identity unavailable.");
                var key = identifier.Value == 0 || CFGetTypeID(identifier.Value) != CFStringGetTypeID() ? 0 :
                    CFEqual(identifier.Value, a.Value) != 0 ? tokenA : CFEqual(identifier.Value, b.Value) != 0 ? tokenB : 0;
                if (key == 0 || windows.ContainsKey(key)) throw new InvalidOperationException("Fixture identifier refused.");
                windows.Add(key, CFRetain(window));
            }
            if (CFEqual(windows[tokenA], windows[tokenB]) != 0) throw new InvalidOperationException("Distinct windows required.");
        }
        catch { Dispose(); throw; }
    }

    public nint Capture(string fixtureWindow) => disposed ? 0 : fixtureWindow switch { "a" => tokenA, "b" => tokenB, _ => 0 };

    public bool Exists(nint identity)
    {
        if (disposed || !lease.IsAliveAndPinned() || AXIsProcessTrusted() == 0 || !windows.TryGetValue(identity, out var retained)) return false;
        using var live = Copy(app, "AXWindows");
        if (!IsWindowArray(live.Value)) return false;
        for (nint i = 0; i < CFArrayGetCount(live.Value); i++)
        {
            var window = CFArrayGetValueAtIndex(live.Value, i);
            if (ElementPid(window) == expectedPid && CFEqual(window, retained) != 0) return true;
        }
        return false;
    }

    public uint ProcessId(nint identity) => !disposed && windows.TryGetValue(identity, out var window) ? ElementPid(window) : 0;

    public bool IsForeground(nint identity)
    {
        if (!Exists(identity) || FrontmostPid() != expectedPid) return false;
        using var focused = Copy(app, "AXFocusedWindow");
        return focused.Value != 0 && ElementPid(focused.Value) == expectedPid &&
            CFEqual(focused.Value, windows[identity]) != 0 && FrontmostPid() == expectedPid;
    }

    private uint FrontmostPid()
    {
        var pool = Send(Send(GetClass("NSAutoreleasePool"), Selector("alloc")), Selector("init"));
        if (pool == 0) return 0;
        try
        {
            var workspace = Send(GetClass("NSWorkspace"), Selector("sharedWorkspace"));
            var front = workspace == 0 ? 0 : Send(workspace, Selector("frontmostApplication"));
            var pid = front == 0 ? 0 : SendInt(front, Selector("processIdentifier"));
            return pid > 0 ? (uint)pid : 0;
        }
        finally { SendVoid(pool, Selector("drain")); }
    }

    private static uint ElementPid(nint element) => element != 0 && CFGetTypeID(element) == AXUIElementGetTypeID() &&
        AXUIElementGetPid(element, out var pid) == 0 && pid > 0 ? (uint)pid : 0;
    private static bool IsWindowArray(nint value) => value != 0 && CFGetTypeID(value) == CFArrayGetTypeID() &&
        CFArrayGetCount(value) is >= 0 and <= 2;
    private static Ref String(string text) => new(CFStringCreateWithCString(0, text, 0x08000100));
    private static Ref Copy(nint element, string attribute)
    {
        using var key = String(attribute);
        if (element == 0 || key.Value == 0) return new(0);
        // On AX failure the out parameter is not a successfully copied reference.
        return AXUIElementCopyAttributeValue(element, key.Value, out var value) == 0 ? new(value) : new(0);
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        foreach (var window in windows.Values) CFRelease(window);
        windows.Clear();
        if (app != 0) { CFRelease(app); app = 0; }
    }

    private sealed class Ref(nint value) : IDisposable
    {
        internal nint Value { get; private set; } = value;
        public void Dispose() { if (Value != 0) { CFRelease(Value); Value = 0; } }
    }

    [LibraryImport(AX)] private static partial byte AXIsProcessTrusted();
    [LibraryImport(AX)] private static partial nint AXUIElementCreateApplication(int pid);
    [LibraryImport(AX)] private static partial nuint AXUIElementGetTypeID();
    [LibraryImport(AX)] private static partial int AXUIElementGetPid(nint element, out int pid);
    [LibraryImport(AX)] private static partial int AXUIElementSetMessagingTimeout(nint element, float timeout);
    [LibraryImport(AX)] private static partial int AXUIElementCopyAttributeValue(nint element, nint key, out nint value);
    [LibraryImport(CF)] private static partial void CFRelease(nint value);
    [LibraryImport(CF)] private static partial nint CFRetain(nint value);
    [LibraryImport(CF)] private static partial byte CFEqual(nint a, nint b);
    [LibraryImport(CF)] private static partial nuint CFGetTypeID(nint value);
    [LibraryImport(CF)] private static partial nuint CFArrayGetTypeID();
    [LibraryImport(CF)] private static partial nint CFArrayGetCount(nint array);
    [LibraryImport(CF)] private static partial nint CFArrayGetValueAtIndex(nint array, nint index);
    [LibraryImport(CF)] private static partial nuint CFStringGetTypeID();
    [LibraryImport(CF, StringMarshalling = StringMarshalling.Utf8)] private static partial nint CFStringCreateWithCString(nint allocator, string value, uint encoding);
    [LibraryImport(ObjC, EntryPoint = "objc_getClass", StringMarshalling = StringMarshalling.Utf8)] private static partial nint GetClass(string name);
    [LibraryImport(ObjC, EntryPoint = "sel_registerName", StringMarshalling = StringMarshalling.Utf8)] private static partial nint Selector(string name);
    [LibraryImport(ObjC, EntryPoint = "objc_msgSend")] private static partial nint Send(nint receiver, nint selector);
    [LibraryImport(ObjC, EntryPoint = "objc_msgSend")] private static partial int SendInt(nint receiver, nint selector);
    [LibraryImport(ObjC, EntryPoint = "objc_msgSend")] private static partial void SendVoid(nint receiver, nint selector);
}
