using SmartSnippets.Presentation.Services;
using SmartSnippets.Presentation.Text;

namespace SmartSnippets.Presentation.Tests;

public sealed class InsertionMessagesTests
{
    private static string Message(InsertionOutcome outcome, bool notice = false, string app = "Notepad") => UiText.InsertionMessage(outcome, app, notice);
    [Fact] public void TargetClosed_DesignWording() => Assert.Equal("Couldn't insert. Notepad was closed. Nothing was pasted.", Message(InsertionOutcome.TargetClosed));
    [Fact] public void TargetElevated_DesignWording() => Assert.Equal("The target app is running as administrator. Smart Snippets can't insert text into it.", Message(InsertionOutcome.TargetElevated));
    [Fact] public void ClipboardBusy_MentionsAnotherApp() => Assert.Contains("another app", Message(InsertionOutcome.ClipboardBusy));
    [Fact] public void ClipboardNotPreserved_AsksPasteAnyway() => Assert.Contains("Paste anyway?", Message(InsertionOutcome.ClipboardNotPreserved));
    [Fact] public void MaybePasted_DoesNotSayNothingWasPasted() => Assert.DoesNotContain("Nothing was pasted", Message(InsertionOutcome.MaybePasted));
    [Fact] public void CopiedForManualPaste_MentionsCtrlV() => Assert.Contains("Ctrl+V", Message(InsertionOutcome.CopiedForManualPaste));
    [Fact] public void Inserted_Empty_WithoutNotice_NoticeTextWithIt() { Assert.Equal("", Message(InsertionOutcome.Inserted)); Assert.Equal(UiText.InsertedRestoreNotice, Message(InsertionOutcome.Inserted, true)); }
    [Fact] public void EveryNonInsertedOutcome_HasNonEmptyMessage() { foreach (var value in Enum.GetValues<InsertionOutcome>().Where(x => x != InsertionOutcome.Inserted)) Assert.NotEmpty(Message(value)); }
    [Fact] public void BlankDisplayName_Fallback() => Assert.Contains("the target app", Message(InsertionOutcome.TargetClosed, app: " "));
}
