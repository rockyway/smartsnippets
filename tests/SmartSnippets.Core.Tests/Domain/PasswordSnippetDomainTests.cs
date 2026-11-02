using SmartSnippets.Core.Domain;

namespace SmartSnippets.Core.Tests.Domain;

public class PasswordSnippetDomainTests
{
    [Theory]
    [InlineData("")]
    [InlineData(" \t")]
    [InlineData("Manual label")]
    public void ManualLabelCannotFallBackToBodyOrTags(string label)
    {
        foreach (var custom in new[] { true, false })
        {
            var draft = new SnippetDraft("synthetic body", label, custom, null, "tag", true);
            var resolved = LabelSuggester.ResolveLabel(draft, ["tag"]);
            Assert.Equal(label, resolved.Label);
            Assert.True(resolved.IsCustom);
            Assert.NotEqual(draft, draft with { IsPassword = false });
        }
    }
}
