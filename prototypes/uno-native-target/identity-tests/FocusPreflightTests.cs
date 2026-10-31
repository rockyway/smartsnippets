using SmartSnippets.TargetIdentity018;
using Xunit;

namespace SmartSnippets.TargetIdentity018.Tests;

public sealed class FocusPreflightTests
{
    [Fact]
    public void ConsoleSessionKeyUsesPublicHeaderMacroValue()
    {
        // Xcode CGSession.h: #define kCGSessionOnConsoleKey CFSTR("kCGSSessionOnConsoleKey").
        // Using the macro name literally makes an unlocked console falsely unavailable.
        Assert.Equal("kCGSSessionOnConsoleKey", FocusPreflight.OnConsoleDictionaryKey);
        Assert.NotEqual("kCGSessionOnConsoleKey", FocusPreflight.OnConsoleDictionaryKey);
    }
}
