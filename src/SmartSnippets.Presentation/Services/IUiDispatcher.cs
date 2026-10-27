namespace SmartSnippets.Presentation.Services;

public interface IUiDispatcher
{
    void Post(Action action);
}
