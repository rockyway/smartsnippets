using SmartSnippets.Presentation.Services;

namespace SmartSnippets.Presentation.Tests.Fakes;

internal sealed class FakeTargetProbe(List<string> calls) : ITargetProbe
{
    public TargetCheck Result { get; set; } = TargetCheck.Ok;
    public bool Foreground { get; set; } = true;
    public Action? OnGate { get; set; }
    public TargetCheck Check(InsertionTarget target) { calls.Add("check"); return Result; }
    public bool IsTargetForeground(InsertionTarget target) { calls.Add("gate"); OnGate?.Invoke(); return Foreground; }
}
