using SmartSnippets.Core.Data;
using SmartSnippets.Core.Store;
using SmartSnippets.Core.Tests.Data;

namespace SmartSnippets.Core.Tests.Porting;

internal sealed class PortingStoreEnv : IDisposable
{
    internal TempDatabase Db { get; } = new(TimeSpan.FromSeconds(1));
    internal SnippetStore Store { get; }
    internal List<StoreChangedEventArgs> Events { get; } = [];
    internal PortingStoreEnv() { new Migrator(Db.Factory).MigrateToLatest(); Store = NewStore(); Store.Changed += (_, e) => Events.Add(e); }
    internal SnippetStore NewStore() { var s = new SnippetStore(Db.Factory, new FixedTime()); s.Load(); return s; }
    public void Dispose() => Db.Dispose();
    private sealed class FixedTime : TimeProvider { public override DateTimeOffset GetUtcNow() => Fixture.Now; }
}
