namespace SmartSnippets.Core.Data;

public static class DbTime
{
    public static long ToDb(DateTimeOffset value) => value.ToUnixTimeMilliseconds();

    public static DateTimeOffset FromDb(long value) => DateTimeOffset.FromUnixTimeMilliseconds(value);

    public static DateTimeOffset? FromDb(long? value) => value is null ? null : FromDb(value.Value);
}
