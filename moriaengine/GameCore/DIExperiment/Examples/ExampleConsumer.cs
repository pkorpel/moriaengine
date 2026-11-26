using System;

namespace SteamEngine.DIExperiment {
	internal sealed record ExampleConfiguration(string StartupMessage = "Example consumer initialized.") : ICoreSettings {
		public void Validate() => ArgumentException.ThrowIfNullOrWhiteSpace(StartupMessage, nameof(StartupMessage));
	}

	internal sealed class ExampleConsumer(ExampleConfiguration settings, ILogger log) : ICoreService {
		public void Init() => log.WriteLine(settings.StartupMessage);

		public void Dispose() { }
	}
}
