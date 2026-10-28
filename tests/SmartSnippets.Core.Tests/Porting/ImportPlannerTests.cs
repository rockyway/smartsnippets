using SmartSnippets.Core.Domain;
using SmartSnippets.Core.Porting;

namespace SmartSnippets.Core.Tests.Porting;

public class ImportPlannerTests
{
    private static ImportCandidate C(string text = "hello", string? label = "Greeting", string? folder = null, string[]? tags = null, ImportStats? stats = null) => new(0, text, label, folder, tags ?? [], stats);
    private static Snippet S(ImportCandidate c, long? folderId = null) => new(1, c.Text, c.Label ?? LabelSuggester.Suggest(c.Text, c.Tags), c.Label is not null, folderId, c.Tags, Fixture.Now, Fixture.Now, null, 0);
    private static ImportPlan Plan(ImportCandidate[] candidates, DedupeMode mode = DedupeMode.TextOnly, Snippet[]? existing = null, Folder[]? folders = null, string[]? names = null) => ImportPlanner.Plan(new(candidates, names ?? [], mode), existing ?? [], folders ?? []);
    private static void Pair(ImportCandidate a, ImportCandidate b, int count = 2, bool library = false)
    {
        var p = library ? Plan([b], DedupeMode.FullRow, [S(a)]) : Plan([a, b], DedupeMode.FullRow);
        Assert.Equal(library ? count - 1 : count, p.Snippets.Count); Assert.Equal(2 - count, p.SkippedDuplicates);
    }
    [Fact] public void Duplicate_OfLibrary_Skipped() { var p = Plan([C()], existing: [S(C())]); Assert.Empty(p.Snippets); Assert.Equal(1, p.SkippedDuplicates); }
    [Fact] public void Duplicate_WithinBatch_FirstWins() { var p = Plan([C(label: "First"), C(label: "Second")]); Assert.Equal("First", Assert.Single(p.Snippets).Label); Assert.Equal(1, p.SkippedDuplicates); }
    [Fact] public void Duplicate_LineEndingsOnly_Skipped() { var p = Plan([C("a\nb"), C("a\r\nb")], existing: [S(C("a\rb"))]); Assert.Empty(p.Snippets); Assert.Equal(2, p.SkippedDuplicates); }
    [Fact] public void NotDuplicate_CaseDiffers() => Assert.Equal(2, Plan([C("a"), C("A")]).Snippets.Count);
    [Fact] public void NotDuplicate_TrailingSpaceDiffers() => Assert.Equal(2, Plan([C("a"), C("a ")]).Snippets.Count);
    [Fact] public void NotDuplicate_TabVersusSpaces() => Assert.Equal(2, Plan([C("a\tb"), C("a  b")]).Snippets.Count);
    [Fact] public void FullRow_SameTextDifferentFolder_BothPlanned() => Pair(C(folder: "Work"), C(folder: "Home"));
    [Fact] public void FullRow_SameTextDifferentLabel_BothPlanned() => Pair(C(label: "One"), C(label: "Two"));
    [Fact] public void FullRow_SameTextDifferentTags_BothPlanned() => Pair(C(tags: ["x"]), C(tags: ["y"]));
    [Fact] public void FullRow_SameTextDifferentTagOrder_BothPlanned() => Pair(C(tags: ["x", "y"]), C(tags: ["y", "x"]));
    [Fact] public void FullRow_SameEverything_DifferentStats_SecondIsDuplicate() => Pair(C(), C(stats: new(Fixture.Now, Fixture.Now, Fixture.Now, 99)), 1);
    [Fact] public void FullRow_CrVsLf_SameMetadata_Duplicate() => Pair(C("a\rb"), C("a\nb"), 1);
    [Fact] public void FullRow_CrVsLf_DifferentLabel_BothPlanned() => Pair(C("a\rb", "One"), C("a\nb", "Two"));
    [Fact] public void FullRow_MatchesLibraryRow_BuiltFromStoredFolderAndTags() { var p = Plan([C(folder: "work", tags: ["one", "two"])], DedupeMode.FullRow, [S(C(tags: ["one", "two"]), 7)], [new(7, "Work", Fixture.Now, Fixture.Now)]); Assert.Empty(p.Snippets); Assert.Equal(1, p.SkippedDuplicates); }
    [Fact] public void FullRow_AutoLabelKeyUsesSuggestion() => Pair(C(label: "hello"), C(label: null), 1);
    [Fact] public void FullRow_FolderCaseDiffers_SameKey_Duplicate() => Pair(C(folder: "Work"), C(folder: "work"), 1);
    [Fact] public void TextOnly_SameTextDifferentFolder_SecondIsDuplicate() { var p = Plan([C(folder: "Work"), C(folder: "Home")]); Assert.Single(p.Snippets); Assert.Equal(1, p.SkippedDuplicates); }
    [Theory] [InlineData(false)] [InlineData(true)] public void FullRow_SeparatorInText_VersusSeparatorInLabel_NotDuplicate(bool library) => Pair(C("a\u001fb", "c"), C("a", "b\u001fc"), library: library);
    [Theory] [InlineData(false)] [InlineData(true)] public void FullRow_SeparatorInsideTag_VersusTwoTags_NotDuplicate(bool library) => Pair(C(tags: ["x\u001fy"]), C(tags: ["x", "y"]), library: library);
    [Theory] [InlineData(false)] [InlineData(true)] public void FullRow_NulAndControlCharsInComponents_NotDuplicate(bool library) => Pair(C("a\0b", "c\u0001"), C("a", "b\0c\u0001"), library: library);
    [Theory] [InlineData(false)] [InlineData(true)] public void FullRow_EmptyTagsVersusOneTag_NotDuplicate(bool library) => Pair(C(tags: []), C(tags: ["x"]), library: library);
    [Theory] [InlineData(false)] [InlineData(true)] public void FullRow_TagBoundaryShift_NotDuplicate(bool library) => Pair(C(tags: ["ab", "c"]), C(tags: ["a", "bc"]), library: library);
    [Theory] [InlineData(false)] [InlineData(true)] public void FullRow_SeparatelyAllocatedEqualTags_Duplicate(bool library) => Pair(C(tags: ["x", "y"]), C(tags: ["x", "y"]), 1, library);
    [Fact] public void DuplicateCandidate_WithUnseenFolder_CreatesNoFolder() => Assert.Empty(Plan([C(folder: "Novel")], existing: [S(C())]).NewFolderNames);
    [Fact] public void RejectedCandidate_WithUnseenFolder_CreatesNoFolder() { var p = Plan([C(" ", folder: "Novel")]); Assert.Empty(p.NewFolderNames); Assert.Equal(1, p.Rejected); }
    [Fact] public void AllDuplicates_WithNovelFolderNames_NoNewFolders() => Assert.Empty(Plan([C(folder: "Novel"), C(folder: "Other")], existing: [S(C())]).NewFolderNames);
    [Fact] public void NativeFolderNames_StillCreatedWhenNoCandidateUsesThem() => Assert.Equal(new[] { "Empty" }, Plan([], DedupeMode.FullRow, names: ["Empty"]).NewFolderNames);
    [Fact] public void Folder_MatchesExistingCaseInsensitive_UsesExistingId() { var p = Plan([C(folder: " work ")], folders: [new(7, "Work", Fixture.Now, Fixture.Now)]); Assert.Empty(p.NewFolderNames); Assert.Equal(7, p.ExistingFolderIds[Assert.Single(p.Snippets).FolderKey!]); }
    [Fact] public void Folder_NewTwiceDifferentCase_OneNewFolder_FirstSpelling() => Assert.Equal(new[] { "Work" }, Plan([C("a", folder: "Work"), C("b", folder: "work")]).NewFolderNames);
    [Fact] public void Folder_Blank_Unfiled() => Assert.Null(Assert.Single(Plan([C(folder: "")]).Snippets).FolderKey);
    [Fact] public void Folder_Whitespace_Unfiled() => Assert.Null(Assert.Single(Plan([C(folder: "  ")]).Snippets).FolderKey);
    [Fact] public void Folder_Trimmed() => Assert.Equal(new[] { "Work" }, Plan([C(folder: " Work ")]).NewFolderNames);
    [Fact] public void FolderNames_ProcessedFirst_SpellingWins() => Assert.Equal(new[] { "WORK" }, Plan([C(folder: "work")], names: ["WORK"]).NewFolderNames);
    [Fact] public void FolderNames_ExistingNotRecreated() => Assert.Empty(Plan([], folders: [new(7, "Work", Fixture.Now, Fixture.Now)], names: ["work"]).NewFolderNames);
    [Fact] public void Label_Given_KeptExactlyAsCustom() { var s = Assert.Single(Plan([C(label: " Custom ")]).Snippets); Assert.Equal(" Custom ", s.Label); Assert.True(s.IsLabelCustom); }
    [Fact] public void Label_Missing_Suggested_FromFirstTag() { var s = Assert.Single(Plan([C(label: null, tags: ["first", "second"])]).Snippets); Assert.Equal("first", s.Label); Assert.False(s.IsLabelCustom); }
    [Fact] public void Label_Missing_Suggested_FromFirstLine() => Assert.Equal("First", Assert.Single(Plan([C("\nFirst\nSecond", label: " ")]).Snippets).Label);
    [Fact] public void Tags_CommaSplit_CaseDeduped() => Assert.Equal(new[] { "Beta", "Gamma" }, Assert.Single(Plan([C(tags: ["Beta, Gamma", "beta"])]).Snippets).Tags);
    [Fact] public void Validation_Failure_CountedRejected() { var p = Plan([C(" ")]); Assert.Empty(p.Snippets); Assert.Equal(1, p.Rejected); }
    [Fact] public void Plan_DoesNotMutateInputs() { string[] tags = [" x, y ", "X"]; var c = C(tags: tags); Plan([c]); Assert.Equal(new[] { " x, y ", "X" }, tags); Assert.Equal("Greeting", c.Label); }
    [Fact] public void Counts_DuplicatesPlusPlannedPlusRejected_EqualCandidates() { var p = Plan([C(), C(), C(" "), C("new")]); Assert.Equal(4, p.Snippets.Count + p.SkippedDuplicates + p.Rejected); Assert.Equal(2, p.Snippets.Count); }
}
