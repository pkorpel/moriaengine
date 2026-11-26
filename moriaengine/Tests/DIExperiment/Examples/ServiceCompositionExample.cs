using SteamEngine.DIExperiment;
using SteamEngine.Tests.Fixtures;

namespace SteamEngine.Tests.DIExperiment.Examples {
	// Illustrative composition for a future integration test, deliberately not an xUnit test.
	internal static class ServiceCompositionExample {
		/// <summary>Composes the discovered example services with the caller's configuration store.</summary>
		internal static StartupResult Run(IConfigStore store) {
			var types = CoreServiceOrchestrator.Discover(typeof(LoggerService).Assembly, typeof(ExampleConsumer).Assembly);
			using var host = new CoreServiceOrchestrator(types);
			return host.Start(store);
		}

		/// <summary>Shows explicit test settings, with ordinary property defaults for unspecified values.</summary>
		internal static StartupResult WithTestConfiguration() {
			// Seed the example's settings before composing its services.
			var store = new InMemoryConfigStore();
			store.WriteDefaults(new LoggerConfiguration { LogToFiles = false });
			store.WriteDefaults(new ExampleCatalogConfiguration { EntryCount = 5 });
			store.WriteDefaults(new ExampleConfiguration { StartupMessage = "Starting with test configuration." });
			store.WriteDefaults(new ExampleReportConfiguration());
			return Run(store);
		}

		internal static StartupResult WithIni(string path) => Run(new SharpConfigStore(path));
	}
}
