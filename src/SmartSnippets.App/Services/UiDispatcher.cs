using Microsoft.UI.Dispatching;
using SmartSnippets.Presentation.Services;

namespace SmartSnippets.App.Services;

internal sealed class UiDispatcher : IUiDispatcher
{
    private readonly DispatcherQueue _queue;

    public UiDispatcher(DispatcherQueue queue) => _queue = queue;

    public void Post(Action action) => _queue.TryEnqueue(() => action());
}
