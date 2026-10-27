namespace SmartSnippets.Core.Store;

/// <summary>A database failure. <see cref="Exception.Message"/> is safe to show and never contains user content.</summary>
public sealed class StoreException : Exception
{
    public StoreException(StoreFailure failure, string message, Exception? innerException = null)
        : base(message, innerException)
    {
        Failure = failure;
    }

    public StoreFailure Failure { get; }
}
