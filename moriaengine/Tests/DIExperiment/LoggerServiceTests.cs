using System;
using System.IO;
using SteamEngine.Common;
using SteamEngine.DIExperiment;
using SteamEngine.Tests.Fixtures;
using Xunit;

namespace SteamEngine.Tests.DIExperiment {
	public class LoggerServiceTests {
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

		/// <summary>The facade forwards output without owning files and rejects use outside its lifetime.</summary>
		[Fact]
		public void LoggerDelegatesWithoutOwningLogFiles() {
			using var folder = new TempDirectory();
			using var service = new LoggerService(new LoggerConfiguration { LogPath = folder.Path });
			ILogger logger = service;
			Assert.Throws<InvalidOperationException>(() => logger.WriteLine("before initialization"));
			service.Init();
			var output = CaptureLegacyLog(() => logger.WriteLine("Logger service message."));
			Assert.Contains("Logger service message.", output);
			Assert.Empty(Directory.EnumerateFileSystemEntries(folder.Path));
			service.Dispose();
			Assert.Throws<ObjectDisposedException>(() => logger.WriteLine("after shutdown"));
		}
	}
}
