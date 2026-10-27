using SmartSnippets.Presentation.Services;

namespace SmartSnippets.Presentation.Tests.Fakes;

internal sealed class FakeClipboardService : IClipboardService
{
    public List<string> Texts { get; } = [];

    public void SetText(string text) => Texts.Add(text);
}

/// <summary>A clock fixed at <see cref="Now"/> with a fixed UTC offset as the local zone.</summary>
internal sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public DateTimeOffset Now { get; set; } = now;

    public override DateTimeOffset GetUtcNow() => Now.ToUniversalTime();

    public override TimeZoneInfo LocalTimeZone { get; } =
        TimeZoneInfo.CreateCustomTimeZone("test", now.Offset, "test", "test");
}
