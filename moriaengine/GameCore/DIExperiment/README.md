# Test-only core service lifecycle (prototype)

Services are discovered, not registered: every non-abstract `ICoreService` in the scanned assemblies.

```csharp
using var host = new CoreServiceOrchestrator(CoreServiceOrchestrator.Discover(typeof(LoggerService).Assembly));
var result = host.Start(store); // IConfigStore supplied by the caller
```

A service is self-contained: its single constructor declares everything it needs.

- **Settings**: a parameter of an `ICoreSettings` type (e.g. `LoggerConfiguration`). Its defaults come from
  the type's all-defaults constructor; `Validate()` checks it without needing any other service.
- **Dependencies**: another service type or an interface it implements. `LoggerService` implements `ILogger`,
  so the orchestrator injects that same initialized service when `ExampleConsumer` asks for `ILogger`.
  There are no per-consumer loggers yet.

`Start` runs two passes:

1. **Configure.** Read every service's settings first, before any service exists. Missing ones get defaults
   written (all of them, so a first run produces a complete config) and the result is `DefaultsWritten`.
   Invalid ones are logged and the result is `Failed`. Either way nothing is constructed.
2. **Start.** Construct each service in dependency order with its validated settings and its already
   initialized dependencies, then call `Init()`. A failure is logged, everything started so far is disposed
   in reverse order, and the result is `Failed`.

Tests pick services by passing their own type list, and stub services by passing objects as `externals`,
which take precedence over discovered services. There is no production startup, INI store or DI container yet.
