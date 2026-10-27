using SmartSnippets.Core.Domain;

namespace SmartSnippets.Core.Tests.Domain;

public sealed class FolderNameRulesTests
{
    [Fact]
    public void CaseInsensitiveUnicodeDuplicate()
    {
        Assert.Equal(FolderNameRules.Key("Ärger"), FolderNameRules.Key("ärger"));
        Assert.Equal(FolderNameRules.Key("Work"), FolderNameRules.Key("  wORk "));
    }

    [Fact]
    public void Key_DistinguishesDifferentNames()
    {
        Assert.NotEqual(FolderNameRules.Key("Work"), FolderNameRules.Key("Home"));
    }

    [Fact]
    public void Eszett_IsNotFoldedToSS()
    {
        // Accepted limitation (plan 003 R1): invariant upper-casing does not fold ß to SS.
        Assert.NotEqual(FolderNameRules.Key("ß"), FolderNameRules.Key("SS"));
    }

    [Fact]
    public void Normalize_Trims()
    {
        Assert.Equal("My Folder", FolderNameRules.Normalize("  My Folder\t"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\r\n")]
    public void Blank_IsRequired(string name)
    {
        Assert.Equal(ValidationError.FolderNameRequired, FolderNameRules.Validate(name));
    }

    [Fact]
    public void NonBlank_IsValid()
    {
        Assert.Null(FolderNameRules.Validate(" Work "));
    }
}
