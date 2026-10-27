using SmartSnippets.Core.Data;

namespace SmartSnippets.Core.Tests.Data;

public class DbTimeTests
{
    [Fact]
    public void RoundTrips_AsUtcMilliseconds()
    {
        var value = new DateTimeOffset(2026, 3, 4, 5, 6, 7, 89, TimeSpan.FromHours(7));

        var db = DbTime.ToDb(value);
        var back = DbTime.FromDb(db);

        Assert.Equal(value, back);
        Assert.Equal(TimeSpan.Zero, back.Offset);
        Assert.Equal(value.ToUnixTimeMilliseconds(), db);
    }

    [Fact]
    public void Nullable_PassesNullThrough()
    {
        Assert.Null(DbTime.FromDb((long?)null));
        Assert.Equal(DbTime.FromDb(5L), DbTime.FromDb((long?)5L));
    }
}
