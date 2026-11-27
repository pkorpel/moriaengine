using System;
using System.ComponentModel;
using SteamEngine.DIExperiment;

namespace SteamEngine.Tests.DIExperiment.Examples {
	[IniSection("example")]
	internal sealed record ExampleConfiguration : ICoreSettings {
		[Description("Message written when the example consumer starts")]
		public string StartupMessage { get; init; } = "Example consumer initialized.";

		public void Validate() => ArgumentException.ThrowIfNullOrWhiteSpace(StartupMessage, nameof(StartupMessage));
	}

	[IniSection("example")]
	internal sealed record ExampleCatalogConfiguration : ICoreSettings {
		[Description("Number of entries in the example catalog")]
		public int EntryCount { get; init; } = 3;

		public void Validate() => ArgumentOutOfRangeException.ThrowIfNegative(EntryCount);
	}

	internal sealed class ExampleCatalog(ExampleCatalogConfiguration settings, ILogger log) : ICoreService {
		internal int EntryCount => settings.EntryCount;

		public void Init() => log.WriteLine($"Example catalog: {EntryCount} entries.");

		public void Dispose() { }
	}

	internal sealed class ExampleConsumer(ExampleConfiguration settings, ExampleCatalog catalog, ILogger log) : ICoreService {
		public void Init() => log.WriteLine($"{settings.StartupMessage} Catalog has {catalog.EntryCount} entries.");

		public void Dispose() { }
	}

	[IniSection("example-report")]
	internal sealed record ExampleReportConfiguration : ICoreSettings {
		[Description("Whether the example report is written at startup")]
		public bool Enabled { get; init; } = true;

		public void Validate() { }
	}

	internal sealed class ExampleReport(ExampleReportConfiguration settings, ExampleConsumer consumer, ExampleCatalog catalog, ILogger log) : ICoreService {
		/// <summary>Writes the optional report after the consumer and catalog have started.</summary>
		public void Init() {
			if (settings.Enabled) {
				log.WriteLine($"{consumer.GetType().Name} is ready; reporting {catalog.EntryCount} entries.");
			}
		}

		public void Dispose() { }
	}
}
