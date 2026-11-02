using SmartSnippets.Core.Query;
using static SmartSnippets.Core.Tests.Query.QueryTestData;

namespace SmartSnippets.Core.Tests.Query;

public sealed class PasswordSearchTests
{
    [Fact]
    public void SecretBodyNotIndexedButMetadataIs()
    {
        var ordinary = View(1, label: "SafeLabel", text: "secretOnly", folder: "SafeFolder", tags: ["SafeTag"]);
        var password = ordinary with { Snippet = ordinary.Snippet with { Id = 2, IsPassword = true } };
        var search = new SnippetSearch([ordinary, password]);
        Assert.Equal([1L], Ids(search.Filter("secretOnly")));
        Assert.Equal([1L, 2L], Ids(search.Filter("safelabel safefolder safetag")));
        Assert.Equal([1L, 2L], Ids(search.Filter("")));
    }
}
