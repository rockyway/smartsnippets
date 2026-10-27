using SmartSnippets.Core.Domain;

namespace SmartSnippets.Core.Store;

/// <summary>Outcome of an expected-failure-capable store call; database failures throw <see cref="StoreException"/> instead.</summary>
public class StoreResult
{
    protected StoreResult(ValidationError? error)
    {
        Error = error;
    }

    public bool Success => Error is null;

    public ValidationError? Error { get; }

    /// <summary>User-readable text for <see cref="Error"/>; empty on success.</summary>
    public string Message => Error is { } error ? error.ToMessage() : "";

    public static StoreResult Ok() => new(null);

    public static StoreResult Fail(ValidationError error) => new(error);
}

public sealed class StoreResult<T> : StoreResult
{
    private StoreResult(T? value, ValidationError? error)
        : base(error)
    {
        Value = value;
    }

    /// <summary>The created or updated item; null when <see cref="StoreResult.Success"/> is false.</summary>
    public T? Value { get; }

    public static StoreResult<T> Ok(T value) => new(value, null);

    public static new StoreResult<T> Fail(ValidationError error) => new(default, error);
}
