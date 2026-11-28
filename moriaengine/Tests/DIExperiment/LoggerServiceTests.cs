using System;
using System.IO;
using SteamEngine.Common;
using SteamEngine.DIExperiment;
using SteamEngine.Tests.Fixtures;
using Xunit;
using Xunit.Abstractions;

namespace SteamEngine.Tests.DIExperiment {
	public class LoggerServiceTests {
		private readonly ITestOutputHelper output;

		public LoggerServiceTests(ITestOutputHelper output) {
			this.output = output;
		}

		private static string CaptureLegacyLog(Action action) {
			var original = Console.Out;
			using var capture = new StringWriter();
			try {
				// An installed legacy logger writes to its saved console and forwards the line as an event.
				Logger.OnConsoleWriteLine += capture.WriteLine;
				Console.SetOut(capture);
				action();
				return capture.ToString();
			} finally {
				Console.SetOut(original);
				Logger.OnConsoleWriteLine -= capture.WriteLine;
			}
		}

		/// <summary>The configured logger owns its file, preserves indentation, and rejects use after disposal.</summary>
		[Fact]
		public void ConfiguredLoggerWritesItsOwnLogFile() {
			using var folder = new TempDirectory();
			using var service = new LoggerService(new LoggerConfiguration { LogPath = folder.Path });
			ILogger logger = service;
			Assert.Throws<InvalidOperationException>(() => logger.WriteLine("before initialization"));
			service.Init();
			var console = CaptureLegacyLog(() => logger.WriteLine("Logger service message."));
			using (logger.Indent()) {
				logger.WriteWarning("indented warning");
			}
			Assert.Contains("Logger service message.", console);
			service.Dispose();
			string text = File.ReadAllText(Assert.Single(Directory.GetFiles(folder.Path)));
			output.WriteLine(text);
			Assert.Contains("Logger service message.", text);
			Assert.Contains("\tWARNING: indented warning", text);
			Assert.Throws<ObjectDisposedException>(() => logger.WriteLine("after shutdown"));
		}

		/// <summary>Emergency logging works without creating or opening a log file.</summary>
		[Fact]
		public void EmergencyLoggerWorksWithoutAnyLogFile() {
			var sink = new RecordingLogger();
			using var service = new LoggerService(LoggerConfiguration.Emergency, sink);
			service.Init();
			((ILogger) service).WriteError("emergency message");
			Assert.Null(service.FilePath);
			Assert.Equal("emergency message", Assert.Single(sink.Entries).Message);
		}

	}
}
