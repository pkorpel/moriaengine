using System;
using System.Globalization;
using System.IO;
using SteamEngine.DIExperiment;
using SteamEngine.Tests.Fixtures;
using Xunit;
using Xunit.Abstractions;

namespace SteamEngine.Tests.DIExperiment.Orchestration {
	public sealed class LoggerBootstrapTests {
		private readonly ITestOutputHelper output;

		public LoggerBootstrapTests(ITestOutputHelper output) {
			this.output = output;
		}

		/// <summary>Usable settings initialize the logger before it is used for later startup work.</summary>
		[Fact]
		public void ConfiguredLoggerStartsWithItsSettings() {
			var store = new InMemoryConfigStore();
			var configuration = new LoggerConfiguration { LogToFiles = false };
			store.WriteDefaults(configuration);
			var sink = new RecordingLogger();
			using var logger = new LoggerService(LoggerConfiguration.Emergency, sink);

			Assert.True(LoggerBootstrap.Initialize(store, logger, out bool existingConfiguration));
			Assert.True(existingConfiguration);
			Assert.Same(configuration, logger.Settings);
			((ILogger) logger).WriteLine("Configured logger is ready.");
			Assert.Contains(sink.Entries, entry => entry.Message?.ToString() == "Configured logger is ready.");
			Print(sink);
		}

		/// <summary>Malformed or invalid input is reported through the emergency logger without rewriting it.</summary>
		[Theory]
		[InlineData("[broken")]
		[InlineData("[files]\nLogPath=x\nLogToFiles=not-a-bool\n")]
		[InlineData("[files]\nLogPath=\"\"\nLogToFiles=true\n")]
		public void UnusableConfigurationIsReportedByEmergencyLogger(string content) {
			using var folder = new TempDirectory();
			string path = Path.Combine(folder.Path, "steamengine.ini");
			File.WriteAllText(path, content);
			var sink = new RecordingLogger();
			using var logger = new LoggerService(LoggerConfiguration.Emergency, sink);

			Assert.False(LoggerBootstrap.Initialize(new SharpConfigStore(path), logger, out _));
			Assert.Same(LoggerConfiguration.Emergency, logger.Settings);
			Assert.Contains("emergency configuration", sink.Entries[0].Display);
			Assert.Equal(LogKind.Error, sink.Entries[1].Kind);
			Assert.Equal(content, File.ReadAllText(path));
			Print(sink);
		}

		/// <summary>A failure opening the configured log file is reported after emergency initialization.</summary>
		[Fact]
		public void FailedLogFileOpenIsReportedByEmergencyLogger() {
			using var folder = new TempDirectory();
			var today = DateTime.Today;
			// The validation probe can succeed, but opening the dated log file must fail, even across midnight.
			for (int offset = -1; offset <= 1; offset++) {
				string name = string.Format(CultureInfo.InvariantCulture,
					"SteamEngine.DIExperiment.{0:yyyy-MM-dd}.log", today.AddDays(offset));
				Directory.CreateDirectory(Path.Combine(folder.Path, name));
			}
			var store = new InMemoryConfigStore();
			store.WriteDefaults(new LoggerConfiguration { LogPath = folder.Path });
			var sink = new RecordingLogger();
			using var logger = new LoggerService(LoggerConfiguration.Emergency, sink);

			Assert.False(LoggerBootstrap.Initialize(store, logger, out _));
			Assert.Same(LoggerConfiguration.Emergency, logger.Settings);
			Assert.Contains(sink.Entries, entry => entry.Kind == LogKind.Warning && entry.Display.Contains("emergency configuration"));
			Assert.Contains(sink.Entries, entry => entry.Kind == LogKind.Error && entry.Display.Contains(folder.Path));
			Print(sink);
		}

		private void Print(RecordingLogger logger) {
			foreach (var entry in logger.Entries) {
				output.WriteLine($"{entry.Kind}: {entry.Display}");
			}
		}
	}
}
