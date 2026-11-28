using System;
using System.Collections.Generic;
using SteamEngine.Common;
using SteamEngine.DIExperiment;
using SteamEngine.Tests.Fixtures;
using Xunit;
using Xunit.Abstractions;

namespace SteamEngine.Tests.DIExperiment.Orchestration {
	public sealed class SettingsLoaderTests {
		private readonly ITestOutputHelper output;

		public SettingsLoaderTests(ITestOutputHelper output) {
			this.output = output;
		}

		/// <summary>Valid settings are returned by identity; a new configuration still stops for review.</summary>
		[Theory]
		[InlineData(false)]
		[InlineData(true)]
		public void ReadsAndValidatesSettingsAndStopsOnlyForNewConfiguration(bool existingConfiguration) {
			var store = new InMemoryConfigStore();
			var value = new ValidSettings();
			store.WriteDefaults(value);
			store.Load();
			var settings = new Dictionary<Type, object>();

			var result = SettingsLoader.Load(store, [typeof(ValidSettings)], settings, new RecordingLogger(), existingConfiguration);

			Assert.Equal(existingConfiguration ? StartupResult.Running : StartupResult.DefaultsWritten, result);
			Assert.Same(value, settings[typeof(ValidSettings)]);
			Assert.Equal(1, value.ValidationCount);
		}

		/// <summary>Both engine and ordinary validation errors are logged, later types are still read, and nothing is saved.</summary>
		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public void InvalidSettingsAreReportedWithoutSaving(bool isSEException) {
			var inner = new InMemoryConfigStore();
			inner.WriteDefaults(new InvalidSettings(isSEException));
			var trace = new OrchestratorTestServices.StartupTrace();
			var store = new OrchestratorTestServices.TrackingStore(inner, trace);
			var settings = new Dictionary<Type, object>();
			var logger = new RecordingLogger();

			// A bad value must not hide errors in the settings read after it.
			var result = SettingsLoader.Load(store, [typeof(InvalidSettings), typeof(ValidSettings)], settings, logger, true);

			Assert.Equal(StartupResult.Failed, result);
			Assert.Equal(new[] { "store.read InvalidSettings", "store.read ValidSettings" }, trace.Events);
			Assert.Single(settings);
			Assert.Contains(logger.Entries, entry => entry.Kind == LogKind.Error && entry.Display.Contains("invalid settings"));
			Print(logger);
		}

		private void Print(RecordingLogger logger) {
			foreach (var entry in logger.Entries) {
				output.WriteLine($"{entry.Kind}: {entry.Display}");
			}
		}

		private sealed class ValidSettings : ICoreSettings {
			internal int ValidationCount { get; private set; }
			public void Validate() => ValidationCount++;
		}

		private sealed class InvalidSettings(bool isSEException) : ICoreSettings {
			/// <summary>Simulates both types of validation failure handled by the loader.</summary>
			public void Validate() {
				if (isSEException) throw new SEException("invalid settings");
				throw new InvalidOperationException("invalid settings");
			}
		}
	}
}
