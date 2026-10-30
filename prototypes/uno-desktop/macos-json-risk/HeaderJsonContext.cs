using System.Text.Json.Serialization;

namespace SmartSnippets.UnoProbe.JsonRisk;

[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Serialization)]
[JsonSerializable(typeof(Dictionary<string, string>))]
internal partial class HeaderJsonContext : JsonSerializerContext;
