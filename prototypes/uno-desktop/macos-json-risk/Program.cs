using System.Text.Json;
using SmartSnippets.UnoProbe.JsonRisk;

// Standalone headless proof of a proposed vendor correction. No Uno runtime,
// network, profile, clipboard, native host or GUI is used by this executable.
if (JsonSerializer.IsReflectionEnabledByDefault)
    throw new InvalidOperationException("Reflection serialization must be disabled.");

Dictionary<string, string>[] fixtures =
[
    [],
    new() { ["X-Synthetic"] = "fixture" },
    new() { ["X-Unicode"] = "λ 日本語 😀", ["Unicode-鍵"] = "café" },
    new() { ["X-Escaped"] = "quote\" backslash\\ newline\n tab\t", ["X-Removed"] = "" },
];

foreach (var headers in fixtures)
{
    // This overload's type contract is generated at compile time, not discovered
    // by reflection. It would have to replace the call inside the vendor library.
    var json = JsonSerializer.Serialize(headers, HeaderJsonContext.Default.DictionaryStringString);
    using var document = JsonDocument.Parse(json);
    var actual = document.RootElement;
    if (actual.ValueKind != JsonValueKind.Object || actual.EnumerateObject().Count() != headers.Count)
        throw new InvalidOperationException("Synthetic JSON shape mismatch.");
    foreach (var pair in headers)
    {
        if (!actual.TryGetProperty(pair.Key, out var value) || value.GetString() != pair.Value)
            throw new InvalidOperationException("Synthetic JSON value mismatch.");
    }
}

Console.WriteLine($"json_repair_checks={fixtures.Length} reflection_enabled=false");
