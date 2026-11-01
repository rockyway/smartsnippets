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
    public async Task TransientBusy_RetriesSameGuardedRestoreBeforeExposingManualPipe()
    {
        var backup = true; var attempts = 0; var waits = 0;
        await GuardianRecovery.RetainUntilSettledAsync(() => true, () => backup,
            () => { if (++attempts < 3) return RestoreStatus.Busy; backup = false; return RestoreStatus.Restored; },
            () => { waits++; return Task.CompletedTask; },
            _ => throw new InvalidOperationException("No manual recovery after settlement"));
        Assert.Equal(3, attempts); Assert.Equal(2, waits); Assert.False(backup);
    }

    [Theory]
    [InlineData(RestoreStatus.ClipboardChanged)] [InlineData(RestoreStatus.Partial)] [InlineData(RestoreStatus.Failed)]
    public async Task BusyThenNonBusyRefusal_ImmediatelyRetainsManualRecovery(RestoreStatus refusal)
    {
        var backup = true; var attempts = 0; var waits = 0; var clients = 0;
        await GuardianRecovery.RetainUntilSettledAsync(() => true, () => backup,
            () => ++attempts == 1 ? RestoreStatus.Busy : refusal,
            () => { waits++; return Task.CompletedTask; }, status =>
            {
                Assert.Equal(refusal, status); Assert.True(backup); clients++;
                backup = false; return Task.FromResult(RestoreStatus.Restored);
            });
        Assert.Equal(2, attempts); Assert.Equal(1, waits); Assert.Equal(1, clients);
    }

    [Fact]
    public async Task PersistentBusy_BoundedRetriesRetainBackupForExplicitRecovery()
    {
        var backup = true; var attempts = 0; var waits = 0;
        await GuardianRecovery.RetainUntilSettledAsync(() => true, () => backup,
            () => { attempts++; return RestoreStatus.Busy; },
            () => { waits++; return Task.CompletedTask; }, status =>
            {
                Assert.Equal(RestoreStatus.Busy, status); Assert.True(backup);
                Assert.Equal(31, attempts); Assert.Equal(30, waits);
                backup = false; return Task.FromResult(RestoreStatus.Restored);
            });
        Assert.False(backup);
    }

    [Fact]
    public async Task BusyRetryException_RetainsFailedRecoveryWithoutFurtherAutomaticAttempts()
    {
        var backup = true; var attempts = 0; var waits = 0;
        await GuardianRecovery.RetainUntilSettledAsync(() => true, () => backup,
            () => ++attempts == 1 ? RestoreStatus.Busy : throw new InvalidOperationException(),
            () => { waits++; return Task.CompletedTask; }, status =>
            {
                Assert.Equal(RestoreStatus.Failed, status); Assert.True(backup);
                backup = false; return Task.FromResult(RestoreStatus.Restored);
            });
        Assert.Equal(2, attempts); Assert.Equal(1, waits);
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
