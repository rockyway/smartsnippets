using System.Text.Json;

namespace SmartSnippets.Core.Porting;

internal static class JsonFields
{
    internal enum Field { Absent, Ok, WrongType }
    internal enum Text { Absent, Ok, WrongType, InvalidUtf16 }
    internal static Text String(JsonElement obj, string name, out string? value)
    {
        value = null;
        if (!obj.TryGetProperty(name, out var element) || element.ValueKind == JsonValueKind.Null) return Text.Absent;
        if (element.ValueKind != JsonValueKind.String) return Text.WrongType;
        try { value = element.GetString(); return Text.Ok; } catch (InvalidOperationException) { return Text.InvalidUtf16; }
    }
    internal static Field StringArray(JsonElement obj, string name, out IReadOnlyList<string> values)
    {
        values = [];
        if (!obj.TryGetProperty(name, out var element) || element.ValueKind == JsonValueKind.Null) return Field.Absent;
        if (element.ValueKind != JsonValueKind.Array) return Field.WrongType;
        var list = new List<string>();
        foreach (var item in element.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.String) return Field.WrongType;
            try { list.Add(item.GetString()!); } catch (InvalidOperationException) { return Field.WrongType; }
        }
        values = list; return Field.Ok;
    }
    internal static Field Bool(JsonElement obj, string name, out bool value)
    {
        value = false;
        if (!obj.TryGetProperty(name, out var element) || element.ValueKind == JsonValueKind.Null) return Field.Absent;
        if (element.ValueKind is not (JsonValueKind.True or JsonValueKind.False)) return Field.WrongType;
        value = element.GetBoolean(); return Field.Ok;
    }
    private static bool Number(JsonElement obj, string name, out double value)
    {
        value = 0;
        if (!obj.TryGetProperty(name, out var element) || element.ValueKind != JsonValueKind.Number) return false;
        if (element.TryGetInt64(out var n)) { value = n; return true; }
        return element.TryGetDouble(out value) && double.IsFinite(value) && value == Math.Floor(value);
    }
    internal static DateTimeOffset? EpochMs(JsonElement obj, string name, DateTimeOffset now)
    {
        if (!Number(obj, name, out var value) || value < 0) return null;
        long bound = now.ToUnixTimeMilliseconds();
        return DateTimeOffset.FromUnixTimeMilliseconds(value >= bound ? bound : (long)value);
    }
    internal static int Count(JsonElement obj, string name)
    {
        if (!Number(obj, name, out var value) || value < 0) return 0;
        return value >= int.MaxValue ? int.MaxValue : (int)value;
    }
    internal static string? VersionText(JsonElement obj, string name)
    {
        if (!obj.TryGetProperty(name, out var element) || element.ValueKind == JsonValueKind.Null) return null;
        var raw = element.GetRawText(); return raw.Length > 32 ? raw[..32] : raw;
    }
}
