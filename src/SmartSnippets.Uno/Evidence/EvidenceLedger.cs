using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace SmartSnippets.Uno.Evidence;

public sealed class EvidenceLedger
{
    public string SourceFingerprint { get; set; } = "";
    public List<EvidenceRow> Rows { get; set; } = [];

    public bool CanComplete => Rows.Count != 0 && Rows.All(r => r.Status == "PASS"
        && !string.IsNullOrWhiteSpace(r.ResearchDoc) && !string.IsNullOrWhiteSpace(r.SessionId)
        && r.SourceFingerprint == SourceFingerprint);
}

public sealed class EvidenceRow
{
    public string Id { get; set; } = "";
    public string Ac { get; set; } = "";
    public string Surface { get; set; } = "";
    public string Scene { get; set; } = "";
    public string Status { get; set; } = "";
    public string ResearchDoc { get; set; } = "";
    public string SessionId { get; set; } = "";
    public string SourceFingerprint { get; set; } = "";
}

[JsonSerializable(typeof(EvidenceLedger))]
public partial class EvidenceLedgerJsonContext : JsonSerializerContext;
