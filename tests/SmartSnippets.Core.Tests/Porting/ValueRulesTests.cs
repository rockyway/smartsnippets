using SmartSnippets.Core.Porting;

namespace SmartSnippets.Core.Tests.Porting;

public class ValueRulesTests
{
    private static ImportStats Stats(string fields) => Assert.Single(Fixture.TermFlow("{\"text\":\"value\"," + fields + "}").Batch.Candidates).Stats!;
    private static void Date(string literal, DateTimeOffset expected) => Assert.Equal(expected, Stats("\"createdAt\":" + literal).CreatedAt);
    private static void Usage(string literal, int expected) => Assert.Equal(expected, Stats("\"usageCount\":" + literal).UsageCount);
    [Fact] public void Date_Int_Kept() => Date("1700000000000", DateTimeOffset.FromUnixTimeMilliseconds(1700000000000));
    [Fact] public void Date_1e400_Invalid() => Date("1e400", Fixture.Now);
    [Fact] public void Date_Fraction_Invalid() => Date("1.5", Fixture.Now);
    [Fact] public void Date_IntegralDouble_Kept() => Date("1.7e12", DateTimeOffset.FromUnixTimeMilliseconds(1700000000000));
    [Fact] public void Date_Negative_Invalid() => Date("-1", Fixture.Now);
    [Fact] public void Date_String_Invalid() => Date("\"yesterday\"", Fixture.Now);
    [Fact] public void Date_Future_ClampedToNow() => Date("4102444800000", Fixture.Now);
    [Fact] public void Date_LongMax_ClampedToNow() => Date("9223372036854775807", Fixture.Now);
    [Fact] public void Date_TwoPow63_ClampedToNow() => Date("9223372036854775808", Fixture.Now);
    [Fact] public void Date_TwoPow63Scientific_ClampedToNow() => Date("9.223372036854775808e18", Fixture.Now);
    [Fact] public void Date_1e300_ClampedToNow() => Date("1e300", Fixture.Now);
    [Fact] public void Date_LongMin_Invalid() => Date("-9223372036854775808", Fixture.Now);
    [Fact] public void Date_Minus1e300_Invalid() => Date("-1e300", Fixture.Now);
    [Fact] public void Usage_Negative_Zero() => Usage("-1", 0);
    [Fact] public void Usage_Fraction_Zero() => Usage("1.5", 0);
    [Fact] public void Usage_String_Zero() => Usage("\"7\"", 0);
    [Fact] public void Usage_OneBillionAndThree_KeptExactly() => Usage("1000000003", 1000000003);
    [Fact] public void Usage_IntMax_KeptExactly() => Usage("2147483647", int.MaxValue);
    [Fact] public void Usage_IntMaxPlusOne_Saturated() => Usage("2147483648", int.MaxValue);
    [Fact] public void Usage_LongMax_Saturated() => Usage("9223372036854775807", int.MaxValue);
    [Fact] public void Usage_TwoPow63_Saturated() => Usage("9223372036854775808", int.MaxValue);
    [Fact] public void Usage_TwoPow63Scientific_Saturated() => Usage("9.223372036854775808e18", int.MaxValue);
    [Fact] public void Usage_1e300_Saturated() => Usage("1e300", int.MaxValue);
    [Fact] public void Usage_1e400_Zero() => Usage("1e400", 0);
    [Fact] public void UpdatedBeforeCreated_SetToCreated() { var s = Stats("\"createdAt\":1700000000000,\"updatedAt\":1600000000000"); Assert.Equal(s.CreatedAt, s.UpdatedAt); }
    [Fact] public void Text_LoneSurrogate_RejectedInvalidText() => Assert.Equal(RejectReason.InvalidText, Assert.Single(Fixture.TermFlow("{\"text\":\"\\ud800\"}").Rejected).Reason);
    [Fact] public void Label_LoneSurrogate_RejectedWrongFieldType() => Assert.Equal(RejectReason.WrongFieldType, Assert.Single(Fixture.TermFlow("{\"text\":\"ok\",\"label\":\"\\ud800\"}").Rejected).Reason);
    [Fact] public void Null_IsAbsent()
    {
        var c = Assert.Single(Fixture.TermFlow("{\"text\":\"ok\",\"label\":null,\"folder\":null,\"tags\":null,\"createdAt\":null,\"updatedAt\":null,\"lastUsedAt\":null,\"usageCount\":null}").Batch.Candidates);
        Assert.Null(c.Label); Assert.Null(c.FolderName); Assert.Empty(c.Tags); Assert.Equal(new ImportStats(Fixture.Now, Fixture.Now, null, 0), c.Stats);
    }
}
