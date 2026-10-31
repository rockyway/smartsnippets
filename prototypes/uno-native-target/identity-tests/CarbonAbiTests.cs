using System.Reflection;
using SmartSnippets.TargetIdentity018;
using Xunit;

namespace SmartSnippets.TargetIdentity018.Tests;

public sealed class CarbonAbiTests
{
    // Reflection is confined to this managed test assembly, never the AOT probe.
    // Xcode MacTypes.h defines ByteCount and ItemCount as unsigned long (LP64).
    [Theory]
    [InlineData("InstallEventHandler", 2)]
    [InlineData("SetEventParameter", 3)]
    [InlineData("ReceiveNextEvent", 0)]
    [InlineData("GetEventParameter", 4)]
    public void CarbonCountsArePointerSized(string method, int parameter)
    {
        var declaration = typeof(NativeCallbackLease).GetMethod(method, BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(declaration);
        Assert.Equal(typeof(nuint), declaration.GetParameters()[parameter].ParameterType);
    }

    [Fact]
    public void ActualByteCountCannotOverwriteFourByteStorage()
    {
        var declaration = typeof(NativeCallbackLease).GetMethod("GetEventParameter", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(declaration);
        var count = declaration.GetParameters()[5];
        Assert.True(count.IsOut);
        Assert.Equal(typeof(nuint).MakeByRefType(), count.ParameterType);
    }
}
