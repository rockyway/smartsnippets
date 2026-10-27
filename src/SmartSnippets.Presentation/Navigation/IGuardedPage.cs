namespace SmartSnippets.Presentation.Navigation;

/// <summary>Asked before the shell navigates away from or hides a page; false keeps the user where they are.</summary>
public interface IGuardedPage
{
    Task<bool> CanLeaveAsync();
}
