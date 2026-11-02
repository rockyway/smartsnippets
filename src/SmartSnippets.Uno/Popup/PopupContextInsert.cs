using SmartSnippets.Presentation.ViewModels;

namespace SmartSnippets.Uno.Popup;

/// <summary>The popup menu's invocation-time checks, shared with closed-gate callback acceptance.
/// This is callback evidence, not evidence of a rendered menu or physical pointer delivery.</summary>
internal static class PopupContextInsert
{
    internal static bool CanInvoke(PopupViewModel popup, PopupItemViewModel row, Func<bool> native) =>
        popup.CanInsertFromMain && popup.CanUseRowActions(row) && native();

    internal static Task InvokeAsync(PopupViewModel popup, PopupItemViewModel row, Func<bool> native,
        Func<Func<Task>, Task> run) => CanInvoke(popup, row, native)
        ? run(() => popup.InsertRowAsync(row)) : Task.CompletedTask;
}
