# Core service lifecycle (prototype)

An experiment in starting the engine's core systems as small, self-contained services instead of
static singletons wired up by hand. Production startup does not use it yet.

## Ideas

- **Services are discovered, not registered.** Every non-abstract `ICoreService` in the scanned assemblies
  takes part. Its single constructor says what it needs: its settings and the other services it depends on.
- **Settings are plain objects.** Each service's settings are a class mapped to one INI section, with the
  defaults written as property initializers. SharpConfig does the mapping; we add no converters or attributes
  beyond the section name and a description per key.
- **Configure everything before starting anything.** All settings are read and validated first, and every
  error is reported at once. Missing keys get their defaults and the INI is written once, only if everything
  succeeded. A missing INI is generated and startup stops so it can be reviewed.
- **Then start in dependency order.** Each service is constructed with its validated settings and its
  already-started dependencies, then `Init()` runs. If one fails, what has started is disposed in reverse.
- **No I/O in constructors**, so services and the config store are easy to replace in tests
  (`InMemoryConfigStore`, stub services).

## A settings class

```csharp
[IniSection("files")]
internal sealed record LoggerConfiguration : ICoreSettings {
	[Description("Path to the log files")]
	public string LogPath { get; init; } = "./logs/";

	[Description("Whether to log console output to a file")]
	public bool LogToFiles { get; init; } = true;

	// Checks the values alone, without needing any other service (simplified here).
	public void Validate() {
		if (LogToFiles && string.IsNullOrWhiteSpace(LogPath))
			throw new InvalidDataException("LogPath is required when file logging is enabled.");
	}
}
```

Property names are the INI keys. A settings class may cover only part of a section.

## A service

```csharp
internal sealed class ExampleCatalog(ExampleCatalogConfiguration settings, ILogger log) : ICoreService {
	internal int EntryCount => settings.EntryCount;

	public void Init() => log.WriteLine($"Example catalog: {EntryCount} entries.");

	public void Dispose() { }
}
```

`ILogger` is satisfied by `LoggerService`, which implements it. Another service can depend on
`ExampleCatalog` the same way.

## Starting it

```csharp
var store = new SharpConfigStore("steamengine.ini");
using var host = new CoreServiceOrchestrator(CoreServiceOrchestrator.Discover(typeof(LoggerService).Assembly));
StartupResult result = host.Start(store); // Running, DefaultsWritten or Failed
```

More examples are in `Tests/DIExperiment/Examples/`.
