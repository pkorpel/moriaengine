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

		/// <summary>Valid settings are returned by identity, while a phase-zero default still requires a restart.</summary>
		[Theory]
		[InlineData(false)]
		[InlineData(true)]
		public void ReadsAndValidatesSettingsAndPreservesLoggerDefaultsResult(bool loggerDefaultsWritten) {
			var store = new InMemoryConfigStore();
			var value = new ValidSettings();
			store.WriteDefaults(value);
			store.Load();
			var settings = new Dictionary<Type, object>();

			// A prior logger default must not be lost just because these settings are valid.
			var result = SettingsLoader.Load(store, [typeof(ValidSettings)], settings, new RecordingLogger(), loggerDefaultsWritten);

			Assert.Equal(loggerDefaultsWritten ? StartupResult.DefaultsWritten : StartupResult.Running, result);
			Assert.Same(value, settings[typeof(ValidSettings)]);
			Assert.Equal(1, value.ValidationCount);
		}

		/// <summary>All missing types receive defaults, but are not returned as validated settings yet.</summary>
		[Fact]
		public void MissingSettingsWriteAllDefaults() {
			var store = new InMemoryConfigStore();
			store.Load();
			var settings = new Dictionary<Type, object>();

			var result = SettingsLoader.Load(store, [typeof(FirstSettings), typeof(SecondSettings)],
				settings, new RecordingLogger(), false);

			Assert.Equal(StartupResult.DefaultsWritten, result);
			Assert.Equal("first", store.Read<FirstSettings>().Value);
			Assert.Equal("second", store.Read<SecondSettings>().Value);
			Assert.Empty(settings);
		}

		/// <summary>Both engine and ordinary validation errors are logged without skipping other defaults.</summary>
		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public void InvalidSettingsAreReportedAndOtherDefaultsAreStillWritten(bool isSEException) {
			var store = new InMemoryConfigStore();
			store.WriteDefaults(new InvalidSettings(isSEException));
			store.Load();
			var settings = new Dictionary<Type, object>();
			var logger = new RecordingLogger();

			// A bad value must not prevent the rest of the missing defaults being written.
			var result = SettingsLoader.Load(store, [typeof(InvalidSettings), typeof(FirstSettings)], settings, logger, false);

			Assert.Equal(StartupResult.Failed, result);
			Assert.NotNull(store.Read<FirstSettings>());
			Assert.Empty(settings);
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

		private sealed class FirstSettings : ICoreSettings {
			public FirstSettings(string value = "first") => Value = value;
			internal string Value { get; }
			public void Validate() { }
		}

		private sealed class SecondSettings : ICoreSettings {
			public SecondSettings(string value = "second") => Value = value;
			internal string Value { get; }
			public void Validate() { }
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
