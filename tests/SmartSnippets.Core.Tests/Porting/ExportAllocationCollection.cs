namespace SmartSnippets.Core.Tests.Porting;

// Allocation measurements use a process-wide counter, so no other tests may allocate concurrently.
[CollectionDefinition("Export allocations", DisableParallelization = true)]
public sealed class ExportAllocationCollection;
