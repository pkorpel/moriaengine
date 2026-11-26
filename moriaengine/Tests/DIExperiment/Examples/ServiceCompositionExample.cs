using System;
using System.Collections.Generic;
using SteamEngine.DIExperiment;
using SteamEngine.Tests.Fixtures;

namespace SteamEngine.Tests.DIExperiment.Examples {
	// Illustrative composition for a future integration test, deliberately not an xUnit test.
	internal static class ServiceCompositionExample {
		/// <summary>Starts exactly the service types chosen by the caller with the supplied configuration store.</summary>
		internal static StartupResult Run(IConfigStore store, IEnumerable<Type> serviceTypes) {
			using var host = new CoreServiceOrchestrator(serviceTypes);
			return host.Start(store);
		}

		/// <summary>Shows test configuration with explicit service types and no assembly discovery.</summary>
		internal static StartupResult WithTestConfiguration() {
			// A fake store supplies only the settings needed by this composition.
			var store = new InMemoryConfigStore();
			store.WriteDefaults(new LoggerConfiguration { LogToFiles = false });
			store.WriteDefaults(new MultiDataConfiguration { UseMultiItems = false });
			store.WriteDefaults(new ExampleCatalogConfiguration { EntryCount = 5 });
			store.WriteDefaults(new ExampleConfiguration { StartupMessage = "Starting with test configuration." });
			store.WriteDefaults(new ExampleReportConfiguration());
			// Tests opt into the types they need instead of scanning the test assembly.
			return Run(store, [typeof(MultiDataService), typeof(ExampleCatalog), typeof(ExampleConsumer), typeof(ExampleReport)]);
		}

		/// <summary>Shows runtime-style composition that explicitly discovers services in its assemblies.</summary>
		internal static StartupResult WithIni(string path) =>
			Run(new SharpConfigStore(path),
				CoreServiceOrchestrator.Discover(typeof(LoggerService).Assembly, typeof(ExampleConsumer).Assembly));
	}
}
