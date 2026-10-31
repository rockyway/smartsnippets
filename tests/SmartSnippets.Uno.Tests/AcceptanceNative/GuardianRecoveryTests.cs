using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SmartSnippets.Presentation.Services;
using SmartSnippets.Uno.Platforms.Windows.AcceptanceNative;
using Xunit;

namespace SmartSnippets.Uno.Tests.AcceptanceNative;

public sealed class GuardianRecoveryTests
{
    [Fact]
    public async Task EofOrUnknownHandleStatus_WaitsForRetainedWorkerSignalBeforeRestore()
    {
        var checks = 0; var waits = 0; var restored = false;
        await GuardianRecovery.RetainUntilSettledAsync(
            () => ++checks == 1 ? throw new InvalidOperationException() : checks >= 4,
            () => !restored,
            () => { Assert.Equal(3, waits); restored = true; return RestoreStatus.Restored; },
            () => { waits++; return Task.CompletedTask; },
            _ => throw new InvalidOperationException("Must not expose retry after verified settlement."));
        Assert.True(restored);
    }

    [Fact]
    public async Task RestoreExceptionAndForeignOwner_RetainRamAcrossDisconnectedRecoveryClients()
    {
        var backup = true; var restores = 0; var clients = 0;
        var statuses = new List<RestoreStatus>();
        RestoreStatus Restore()
        {
            restores++;
            if (restores == 1) throw new InvalidOperationException();
            if (restores == 2) return RestoreStatus.ClipboardChanged;
            backup = false; return RestoreStatus.Restored;
        }
        await GuardianRecovery.RetainUntilSettledAsync(() => true, () => backup, Restore,
            () => throw new InvalidOperationException(), status =>
            {
                Assert.True(backup);
                statuses.Add(status);
                if (++clients == 1) throw new InvalidOperationException();
                return Task.FromResult(Restore());
            });
        Assert.Equal(3, restores);
        Assert.Equal(3, clients);
        Assert.Equal(new[] { RestoreStatus.Failed, RestoreStatus.Failed, RestoreStatus.ClipboardChanged }, statuses);
        Assert.False(backup);
    }

    [Fact]
    public async Task NoBackup_NeverTouchesWorkerOrClipboardDuringDeniedCleanup()
    {
        await GuardianRecovery.RetainUntilSettledAsync(
            () => throw new InvalidOperationException(), () => false,
            () => throw new InvalidOperationException(), () => throw new InvalidOperationException(),
            _ => throw new InvalidOperationException());
    }
}
