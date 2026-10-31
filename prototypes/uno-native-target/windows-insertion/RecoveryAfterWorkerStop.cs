namespace SmartSnippets.WindowsInsertion018;

internal static class RecoveryAfterWorkerStop
{
    internal static async Task<bool> RunAsync(Func<Task> stopWorker, Func<Task<bool>> recoverGuardian)
    {
        // Even a job/Kill/reap race must not skip the guardian handshake. The
        // guardian independently requires its worker HANDLE to be signaled.
        try { await stopWorker(); }
        catch (Exception) { }
        try { return await recoverGuardian(); }
        catch (Exception) { return false; }
    }
}
