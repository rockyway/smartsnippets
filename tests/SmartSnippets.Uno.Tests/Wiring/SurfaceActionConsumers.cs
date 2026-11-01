using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using SmartSnippets.Presentation.Navigation;
using SmartSnippets.Presentation.Services;
using SmartSnippets.Uno.Tests.Composition;

namespace SmartSnippets.Uno
{
    public sealed partial class MainWindow
    {
        internal async Task Audit(HostFixture f)
        {
            OnSelectionChanged(Navigation, new() { SelectedItem = new NavigationViewItem { Tag = "Folders" } });
            Assert.Equal(PageKind.Folders, CurrentPage);
            OnSelectionChanged(Navigation, new() { SelectedItem = new NavigationViewItem { Tag = "Snippets" } });
            await f.Host.Snippets.NewAsync(); f.Host.Snippets.Editor!.Text = "dirty";
            f.Boundary.Dialogs.Choices.Enqueue(SaveChoice.Cancel);
            OnSelectionChanged(Navigation, new() { SelectedItem = new NavigationViewItem { Tag = "Settings" } });
            Assert.Equal(PageKind.Snippets, CurrentPage);
            f.Boundary.Dialogs.Choices.Enqueue(SaveChoice.Discard);
            OnSelectionChanged(Navigation, new() { SelectedItem = new NavigationViewItem { Tag = "Settings" } });
            Assert.Equal(PageKind.Settings, CurrentPage);
            CloseForExit();
        }
    }
}
namespace SmartSnippets.Uno.Controls
{
    public sealed partial class HoverDetailsSurface
    {
        internal bool AuditOpen => _popup.IsOpen;
        internal bool AuditHandoff => _policy.IsHandingOff;
        internal Windows.Foundation.Point AuditPointer => _pointer;
        internal void Audit(string action)
        {
            Request(_owner, "body", new(40,40), false); DispatcherQueue.Tick();
            Assert.True(_popup.IsOpen); Assert.Equal("body", Body.Text);
            OwnerExited(); Assert.True(_policy.IsHandingOff);
            if (action == "OnSurfaceEntered")
            {
                OnSurfaceEntered(this, new()); DispatcherQueue.Tick(); Assert.True(_popup.IsOpen);
            }
            else if (action == "OnSurfaceExited")
            {
                OnSurfaceEntered(this, new()); OnSurfaceExited(this, new()); DispatcherQueue.Tick(); Assert.False(_popup.IsOpen);
            }
            else throw new InvalidOperationException("Unmapped hover action");
        }
    }
    public sealed partial class SnippetRowDetails
    {
        internal void Audit(string action, HostFixture f, string? targetName)
        {
            // Use actual composition data, never a replacement details model.
            var snippet = f.Host.Store.CreateSnippet(new("body", "label", true, null, "tag")).Value!;
            ContentText = snippet.Text; Label = snippet.Label; TagsDisplay = string.Join(", ", snippet.Tags); FolderName = "folder"; TextSize = f.Host.TextSize;
            var pointer = new PointerRoutedEventArgs { Position = new(40,40) };
            var target = targetName switch { "FolderText" => FolderText, "TagsText" => TagsText, _ => LabelText };
            OnTargetEntered(target, pointer);
            var hover = Assert.Single(_hovers).Value;
            switch (action)
            {
                case "OnTargetEntered":
                    DispatcherQueue.Tick(); Assert.True(hover.AuditOpen); Assert.Equal(ReferenceEquals(target, FolderText) ? "folder" : ReferenceEquals(target, TagsText) ? "tag" : "body", hover.Body.Text); break;
                case "OnTargetExited":
                    OnTargetExited(target, pointer); DispatcherQueue.Tick(); Assert.False(hover.AuditOpen); break;
                case "OnRowPointerMoved":
                    OnRowPointerMoved(this, new() { Position = new(70,80) }); Assert.Equal(new(70,80), hover.AuditPointer); break;
                case "OnRowExited":
                    DispatcherQueue.Tick(); OnRowExited(this, pointer); DispatcherQueue.Tick(); Assert.False(hover.AuditOpen); break;
                case "OnRowEntered":
                    DispatcherQueue.Tick(); OnRowExited(this, pointer); Assert.True(hover.AuditHandoff);
                    OnRowEntered(this, pointer); DispatcherQueue.Tick(); Assert.True(hover.AuditOpen); Assert.False(hover.AuditHandoff); break;
                default: throw new InvalidOperationException("Unmapped row action");
            }
            Cancel();
        }
    }
}
