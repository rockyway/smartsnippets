using System.Runtime.InteropServices;

namespace SmartSnippets.Uno;

/// <summary>SDK 10.0.26100.0 um/UIAutomationClient.h: IUnknown Release slot 2,
/// IUIAutomation ElementFromHandle slot 6 (UIA_HWND, IUIAutomationElement**),
/// IUIAutomationElement SetFocus slot 3 (HRESULT). All calls are synchronous on the WinUI COM apartment.</summary>
internal sealed unsafe partial class OwnedPreparationAutomation : IOwnedPreparationAutomation
{
    private nint automation, element;
    internal OwnedPreparationAutomation()
    {
        var clsid = new Guid("ff48dba4-60ef-4201-aa87-54103eef594e"); // CUIAutomation
        var iid = new Guid("30cbe57d-d9d0-452a-ab13-7ac5ac4825ee"); // IUIAutomation
        var result = CoCreateInstance(&clsid, 0, 1, &iid, out automation); // CLSCTX_INPROC_SERVER only
        if (result < 0 || automation == 0) { Dispose(); throw new InvalidOperationException("owned_preparation_com"); }
    }
    // Test seam owns fake interface pointers, never invokes CoCreateInstance.
    internal OwnedPreparationAutomation(nint fakeAutomation) => automation = fakeAutomation;
    public bool ElementFromHandle(nint editor)
    {
        if (automation == 0 || element != 0 || editor == 0) return false;
        int result;
        fixed (nint* output = &element)
            result = ((delegate* unmanaged[Stdcall]<nint, nint, nint*, int>)(*(nint**)automation)[6])(automation, editor, output);
        if (result < 0) { Release(ref element); return false; }
        return element != 0;
    }
    public bool SetFocus() => element != 0 &&
        ((delegate* unmanaged[Stdcall]<nint, int>)(*(nint**)element)[3])(element) >= 0;
    public void Dispose()
    {
        try { Release(ref element); }
        finally { Release(ref automation); }
    }
    private static void Release(ref nint pointer)
    {
        var value = pointer; pointer = 0;
        if (value != 0) _ = ((delegate* unmanaged[Stdcall]<nint, uint>)(*(nint**)value)[2])(value);
    }
    [LibraryImport("ole32.dll")] private static partial int CoCreateInstance(Guid* clsid, nint outer, uint context, Guid* iid, out nint instance);
}
