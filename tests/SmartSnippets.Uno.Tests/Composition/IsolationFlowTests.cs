using SmartSnippets.Core;
using SmartSnippets.Core.Domain;
using SmartSnippets.Presentation.Navigation;
using SmartSnippets.Presentation.Services;
using SmartSnippets.Uno.Services;

[assembly: CollectionBehavior(DisableTestParallelization = true)]
namespace SmartSnippets.Uno.Tests.Composition;

public sealed class IsolationFlowTests
{
    [Fact]
    public async Task DefaultComposition_UsesExplicitProfileAndSubstitutesEveryNativeBoundary()
    {
        using var f = new HostFixture();
        await f.Start();
        Assert.True(File.Exists(Path.Combine(f.Root, "smartsnippets.db")));
        Assert.True(Directory.Exists(Path.Combine(f.Root, "logs")));
        Assert.Equal(1, f.Boundary.Messages);
        Assert.Single(f.Boundary.Popups);
        Assert.NotNull(f.Host.Snippets);
        Assert.NotNull(f.Host.Folders);
        Assert.NotNull(f.Host.SettingsPage);
        Assert.False(f.Host.Gate.IsAvailable);
        Assert.False(f.Host.Gate.TrySetText("synthetic"));
        f.Host.Gate.SetText("synthetic");
        Assert.False(f.Host.Snippets.CanInsertRow);
        f.Host.Dispose(); f.Host.Dispose();
        Assert.Equal(1, f.Boundary.MessageDisposals);
        Assert.Equal(1, f.Boundary.TrayDisposals);
        Assert.Equal(1, f.Boundary.Main.Disposals);
    }
    [Fact]
    public async Task StartupFailure_ExitsThroughSubstitutionAndReleasesProfile()
    {
        using var f = new HostFixture(); f.Boundary.FailWindow = true;
        await f.Start();
        Assert.Equal(1, f.Boundary.Reports);
        Assert.Equal(1, f.Boundary.Exits);
        Assert.Equal(1, f.Boundary.MessageDisposals);
        Assert.Empty(f.Boundary.Popups);
        using var reopened = AppProfile.OpenOwnedRoot(f.Root);
    }
    [Fact]
    public async Task CorruptDatabase_ReportsWithoutConstructingNativeBoundaries()
    {
        using var f = new HostFixture(); Directory.CreateDirectory(f.Root);
        File.WriteAllText(Path.Combine(f.Root, "smartsnippets.db"), "not sqlite");
        await f.Start();
        Assert.Equal(1, f.Boundary.Reports);
        Assert.Equal(1, f.Boundary.Exits);
        Assert.Equal(0, f.Boundary.Messages);
    }
    [Fact]
    public async Task InitialAndRedirectedQa_UseSameHostAndRespectDirtyGuard()
    {
        using var f = new HostFixture(); await f.Start(true, new(PageKind.Folders, false, null));
        Assert.Equal(PageKind.Folders, f.Boundary.Main.Current);
        Assert.False(f.Boundary.Main.Visible);
        await f.Host.ApplyLaunchOptionsAsync(new(PageKind.Snippets, false, null));
        await f.Host.Snippets.NewAsync(); f.Host.Snippets.Editor!.Text = "unsaved synthetic";
        f.Boundary.Dialogs.Choices.Enqueue(SaveChoice.Cancel);
        await f.Host.ApplyLaunchOptionsAsync(new(PageKind.Settings, false, null));
        Assert.Equal(PageKind.Snippets, f.Boundary.Main.Current);
        Assert.True(f.Host.Snippets.Editor.IsDirty);
        f.Boundary.Dialogs.Choices.Enqueue(SaveChoice.Save);
        await f.Host.ApplyLaunchOptionsAsync(new(PageKind.Settings, false, null));
        Assert.Equal(PageKind.Settings, f.Boundary.Main.Current);
        Assert.Equal("unsaved synthetic", Assert.Single(f.Host.Store.Snippets).Snippet.Text);
    }
    [Fact]
    public async Task CloseHides_ExitGuardsAndDisposesOnce_StaleTrayCallbacksIgnored()
    {
        using var f = new HostFixture(); await f.Start();
        await f.Boundary.Main.CloseAsync(); Assert.False(f.Boundary.Main.Visible);
        Assert.Equal(0, f.Boundary.Main.Disposals);
        await f.Host.Snippets.NewAsync(); f.Host.Snippets.Editor!.Text = "dirty";
        f.Boundary.Dialogs.Choices.Enqueue(SaveChoice.Cancel);
        await f.Host.ExitAsync(); Assert.Equal(0, f.Boundary.Exits);
        f.Boundary.Dialogs.Choices.Enqueue(SaveChoice.Discard);
        await f.Host.ExitAsync(); await f.Host.ExitAsync();
        f.Boundary.TrayPreview!(); f.Boundary.TrayOpen!(); f.Boundary.Drain();
        Assert.Equal(1, f.Boundary.Exits); Assert.Single(f.Boundary.Popups);
        Assert.Equal(1, f.Boundary.Main.Disposals);
    }
    [Fact]
    public async Task Exit_WaitsForRealPortingContinuationBeforeReleasingProfile()
    {
        using var f = new HostFixture(); await f.Start();
        var picker = new TaskCompletionSource<string?>(); f.Boundary.Files.DelayedExportPicker = picker.Task;
        var operation = f.Host.Snippets.Porting.ExportAsync();
        Assert.True(f.Host.Snippets.Porting.IsBusy);
        var exit = f.Host.ExitAsync();
        Assert.False(exit.IsCompleted); Assert.Equal(0, f.Boundary.Exits);
        picker.SetResult(null);
        Assert.False(await operation); await exit;
        Assert.False(f.Host.Snippets.Porting.IsBusy); Assert.Equal(1, f.Boundary.Exits);
        using var reopened = AppProfile.OpenOwnedRoot(f.Root);
    }
    [Fact]
    public async Task Exit_PopupEditorCancelRetainsDraftAndRecoversThroughDeferredHost()
    {
        using var f = new HostFixture(); await f.Start(); f.Host.ShowPreview();
        f.Host.Popup.AddSnippetCommand.Execute(null); f.Host.Popup.Editor!.Text = "dirty popup synthetic";
        f.Boundary.Dialogs.Choices.Enqueue(SaveChoice.Cancel);
        await f.Host.ExitAsync();
        Assert.Equal(0, f.Boundary.Exits); Assert.True(f.Host.Popup.Editor.IsDirty);
        Assert.Equal("dirty popup synthetic", f.Host.Popup.Editor.Text);
        Assert.Equal(2, Assert.Single(f.Boundary.Popups).Shows);
        f.Boundary.Dialogs.Choices.Enqueue(SaveChoice.Save);
        await f.Host.ExitAsync();
        Assert.Equal(1, f.Boundary.Exits);
        Assert.Equal("dirty popup synthetic", Assert.Single(f.Host.Store.Snippets).Snippet.Text);
    }
    [Fact]
    public async Task ClosedPopup_RecreatesThroughActualDeferredRecoveryHost()
    {
        using var f = new HostFixture(); await f.Start();
        var first = Assert.Single(f.Boundary.Popups); first.Dispose();
        f.Host.ShowPreview("new synthetic");
        Assert.Equal(2, f.Boundary.Popups.Count);
        Assert.True(f.Host.Popup.IsPreview);
        Assert.Equal("new synthetic", f.Host.Popup.Query);
        Assert.Equal(0, first.Shows);
    }
    [Fact]
    public async Task DeniedTrayPopup_IsPostedTargetFreeAndFlushesDirtyCache()
    {
        using var f = new HostFixture(); await f.Start();
        f.Host.Store.CreateSnippet(new SnippetDraft("synthetic", "Synthetic", true, null, "tag"));
        f.Boundary.TrayPreview!();
        Assert.False(f.Host.Popup.IsPreview);
        f.Boundary.Drain();
        Assert.True(f.Host.Popup.IsPreview); Assert.Null(f.Host.Popup.Target);
        Assert.Single(f.Host.Popup.Items, i => i.Kind == SmartSnippets.Presentation.ViewModels.PopupItemKind.Snippet);
    }
}

public sealed class MessageWindowTests
{
    [Fact]
    public void RepeatedMessageWindow_CreateDisposeAndFailedCreateReleaseClass()
    {
        var native = new Native();
        var first = new MessageWindowLifetime(native); first.Dispose(); first.Dispose();
        using (var second = new MessageWindowLifetime(native)) Assert.Equal((nint)1, second.Handle);
        Assert.Equal(2, native.Registers); Assert.Equal(2, native.Unregisters);
        native.FailCreate = true;
        Assert.Throws<InvalidOperationException>(() => new MessageWindowLifetime(native));
        Assert.Equal(3, native.Unregisters); Assert.Equal(2, native.Destroys);
        native.FailCreate = false;
        using var third = new MessageWindowLifetime(native);
        Assert.Equal((nint)1, third.Handle);
    }
    [Fact]
    public void ExistingClass_IsNeverAdoptedOrUnregistered()
    {
        var native = new Native { Registered = true };
        Assert.Throws<InvalidOperationException>(() => new MessageWindowLifetime(native));
        Assert.Equal(0, native.Unregisters); Assert.Equal(0, native.Creates);
    }
    private sealed class Native : IMessageWindowNative
    {
        internal bool Registered, FailCreate;
        internal int Registers, Unregisters, Creates, Destroys;
        public bool RegisterClass() { Registers++; if (Registered) return false; Registered = true; return true; }
        public nint CreateWindow() { Creates++; return FailCreate ? 0 : 1; }
        public bool DestroyWindow(nint handle) { Destroys++; return true; }
        public bool UnregisterClass() { Unregisters++; Registered = false; return true; }
    }
}
