namespace SmartSnippets.Core.Porting;

public enum ImportFormat { Native, TermFlow, Rephlo, InkSpoke }

/// <summary>Why a whole file was refused; nothing in the library changed.</summary>
public enum ImportRefusal
{
    NotJsonFile,        // path does not end in .json (case-insensitive)
    Unreadable,         // opening OR reading failed (IOException, UnauthorizedAccessException), at any byte offset
    TooLarge,           // more than SnippetFileRules.MaxImportBytes (32 MB)
    NotUtf8,            // UTF-16/UTF-32 BOM, or invalid UTF-8 anywhere
    InvalidJson,        // JsonException (syntax, depth, trailing data)
    UnknownFormat,      // root is not an object, or no known marker/key
    UnsupportedVersion, // known format, version missing or not the supported one
    MissingEntries,     // known format and version, entries member missing or not an array
    TooManyEntries,     // array entries exceed MaxImportEntries (native: snippets + explicit folders); checked before mapping
}

/// <summary>An entry the file holds but this app deliberately does not import.</summary>
public enum SkipReason { Archived, NotSendKeys, Parameterized, UnparsableParams, Secret }

/// <summary>An entry that should be importable but is malformed.</summary>
public enum RejectReason { NotAnObject, TextMissing, TextBlank, InvalidText, WrongFieldType, FailedValidation }

public sealed record SkippedEntry(int EntryIndex, SkipReason Reason);

public sealed record RejectedEntry(int EntryIndex, RejectReason Reason);

/// <summary>Stats kept from the file (TermFlow, native). Already validated and clamped by the parser.</summary>
public sealed record ImportStats(DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt, DateTimeOffset? LastUsedAt, int UsageCount);

/// <summary>
/// One importable entry. <see cref="Label"/> null or blank → auto-suggested; otherwise kept as a custom label.
/// <see cref="FolderName"/> null → Unfiled. <see cref="Stats"/> null → fresh (import time, never used).
/// <see cref="Text"/> is exactly the file's text (never trimmed, line endings untouched).
/// </summary>
public sealed record ImportCandidate(
    int EntryIndex,
    string Text,
    string? Label,
    string? FolderName,
    IReadOnlyList<string> Tags,
    ImportStats? Stats);

/// <summary>TextOnly: TermFlow, Rephlo, InkSpoke. FullRow: native (user decision A1, §4.3.5).</summary>
public enum DedupeMode { TextOnly, FullRow }

/// <summary><see cref="FolderNames"/> are folders to ensure exist even if no candidate uses them (native files only).</summary>
public sealed record ImportBatch(IReadOnlyList<ImportCandidate> Candidates, IReadOnlyList<string> FolderNames, DedupeMode Dedupe);

public sealed record ParsedImport(
    ImportFormat Format,
    int EntryCount,
    ImportBatch Batch,
    IReadOnlyList<SkippedEntry> Skipped,
    IReadOnlyList<RejectedEntry> Rejected);

/// <summary><see cref="FoundVersion"/>: the raw JSON of the version value (at most 32 chars), or null when absent.
/// <see cref="EntryCount"/>: set for TooManyEntries (native: combined snippets + raw explicit-folder slots).</summary>
public sealed record ImportFailure(ImportRefusal Refusal, ImportFormat? Format = null, string? FoundVersion = null, int? EntryCount = null);

/// <summary>Exactly one of <see cref="Parsed"/> and <see cref="Failure"/> is non-null.</summary>
public sealed record ImportParseResult(ParsedImport? Parsed, ImportFailure? Failure)
{
    public static ImportParseResult Ok(ParsedImport parsed) => new(parsed, null);
    public static ImportParseResult Fail(ImportFailure failure) => new(null, failure);
}

/// <summary>What the store wrote. <see cref="Rejected"/> counts candidates that failed SnippetValidator.</summary>
public sealed record ImportCommit(IReadOnlyList<long> SnippetIds, int FoldersCreated, int SkippedDuplicates, int Rejected);

public sealed record ImportSummary(
    ImportFormat Format,
    int Entries,
    int Imported,
    int SkippedDuplicates,
    int SkippedUnsupported,
    int Rejected,
    int FoldersCreated)
{
    public bool IsBalanced => Imported + SkippedDuplicates + SkippedUnsupported + Rejected == Entries;
}

/// <summary>Exactly one of <see cref="Summary"/> and <see cref="Failure"/> is non-null.</summary>
public sealed record ImportRunResult(ImportSummary? Summary, ImportFailure? Failure);

public enum ExportRefusal { TooLarge, TooManySnippets, TooManyEntries }

/// <summary>Exactly one of <see cref="Utf8Json"/> (compact, ≤ MaxImportBytes) and <see cref="Refusal"/> is non-null.
/// For TooLarge, <see cref="Bytes"/> is "more than MaxImportBytes" (the writer stops at the limit, so the exact size of
/// an oversized export is never computed).</summary>
public sealed record NativeExportResult(byte[]? Utf8Json, ExportRefusal? Refusal, long Bytes, int SnippetCount, int FolderCount);
