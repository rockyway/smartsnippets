using System.Text.Json;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using Uno.UI.Runtime.Skia.MacOS;

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
    var json = JsonSerializer.Serialize(headers, MacOSWebViewJsonContext.Default.DictionaryStringString);
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

using var request = new HttpRequestMessage();
if (!request.Headers.TryAddWithoutValidation("X-Synthetic", new[] { "first", "second" }))
    throw new InvalidOperationException("Synthetic header setup failed.");
var headerJson = JsonSerializer.Serialize(request.Headers, MacOSWebViewJsonContext.Default.HttpRequestHeaders);
// Golden measured independently with the original reflection serializer. The
// header collection is an array of Key/Value pairs, NOT a string dictionary.
var digest = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(headerJson))).ToLowerInvariant();
if (digest != "ecc9a586e6757150414f01bdf84b470534e083ef42673499ded50e1f0daf7acc")
    throw new InvalidOperationException("Navigation header wire contract changed.");
Console.WriteLine($"json_repair_checks={fixtures.Length + 1} reflection_enabled=false");
