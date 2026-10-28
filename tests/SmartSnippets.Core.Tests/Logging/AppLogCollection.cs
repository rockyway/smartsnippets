namespace SmartSnippets.Core.Tests.Logging;

// AppLog is process-wide; no other test may write while a privacy oracle reads its log.
[CollectionDefinition("AppLog", DisableParallelization = true)]
public sealed class AppLogCollection;
