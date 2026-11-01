using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

namespace SmartSnippets.Uno;

public sealed unsafe class OwnedPreparationAutomationTests
{
    // Fake COM objects have private counters/result slots after their vtable pointer.
    [Theory]
    [InlineData(0, 0)] [InlineData(-1, 0)] [InlineData(0, -1)]
    public void SdkSlotsUseExactHwndAndReleaseEvenFailureOutputs(int elementResult, int focusResult)
    {
        nint* elementTable = stackalloc nint[4];
        elementTable[2] = (nint)(delegate* unmanaged[Stdcall]<nint, uint>)&Release;
        elementTable[3] = (nint)(delegate* unmanaged[Stdcall]<nint, int>)&Focus;
        nint* element = stackalloc nint[5];
        element[0] = (nint)elementTable; element[1] = 0; element[2] = focusResult; element[3] = 0;
        nint* table = stackalloc nint[7];
        table[2] = (nint)(delegate* unmanaged[Stdcall]<nint, uint>)&Release;
        table[6] = (nint)(delegate* unmanaged[Stdcall]<nint, nint, nint*, int>)&FromHandle;
        nint* client = stackalloc nint[5];
        client[0] = (nint)table; client[1] = 0; client[2] = elementResult; client[3] = 0; client[4] = (nint)element;
        using (var bridge = new OwnedPreparationAutomation((nint)client))
        {
            Assert.Equal(elementResult >= 0, bridge.ElementFromHandle(123));
            Assert.Equal(123, client[3]);
            if (elementResult >= 0) Assert.Equal(focusResult >= 0, bridge.SetFocus());
        }
        Assert.Equal(1, client[1]); Assert.Equal(1, element[1]);
        Assert.Equal(elementResult >= 0 ? 1 : 0, element[3]);
    }
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static uint Release(nint self) { ((nint*)self)[1]++; return 0; }
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static int Focus(nint self) { ((nint*)self)[3]++; return (int)((nint*)self)[2]; }
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static int FromHandle(nint self, nint hwnd, nint* output)
    {
        ((nint*)self)[3] = hwnd; *output = ((nint*)self)[4]; return (int)((nint*)self)[2];
    }
}
