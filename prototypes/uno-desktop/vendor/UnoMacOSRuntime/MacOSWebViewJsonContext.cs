using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Text.Json.Serialization;

namespace Uno.UI.Runtime.Skia.MacOS;

// Probe-scoped source correction: both contracts retain the serializer's original
// input types and default wire shape. No native callback or host is removed.
[JsonSerializable(typeof(Dictionary<string, string>))]
[JsonSerializable(typeof(HttpRequestHeaders))]
internal partial class MacOSWebViewJsonContext : JsonSerializerContext;
