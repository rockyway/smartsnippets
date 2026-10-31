using SmartSnippets.Core.Store;
using SmartSnippets.Presentation.Services;

namespace SmartSnippets.WindowsInsertion018;

/// <summary>The worker and non-native policy controls use this same actual service composition.</summary>
internal static class InsertionWorkerFlow
{
    internal static async Task<(InsertionResult First, InsertionResult? Duplicate)> ExecuteAsync(ISnippetStore store,
        ITargetProbe probe, IPasteDispatcher dispatcher, InsertionTarget target, long id)
    {
        var service = new InsertionService(store, probe, dispatcher);
        var first = service.InsertAsync(target, id);
        // Never turn a synchronously completed refusal/failure into an automatic
        // retry. Only issue the duplicate while the original is actually in flight.
        var duplicate = first.IsCompleted ? null : await service.InsertAsync(target, id);
        return (await first, duplicate);
    }
}
