using SmartSnippets.Core.Porting;

namespace SmartSnippets.Core.Tests.Porting;

public class ImportSummaryTests
{
    [Theory]
    [InlineData(4, true)]
    [InlineData(3, false)]
    [InlineData(5, false)]
    public void IsBalanced_TrueOnlyWhenPartsSumToEntries(int entries, bool expected)
    {
        var summary = new ImportSummary(ImportFormat.TermFlow, entries, 1, 1, 1, 1, 0);
        Assert.Equal(expected, summary.IsBalanced);
    }
}
