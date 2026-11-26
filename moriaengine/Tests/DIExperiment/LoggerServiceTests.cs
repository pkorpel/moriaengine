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

		private static CoreServiceOrchestrator Discovered(params object[] externals) =>
			new(CoreServiceOrchestrator.Discover(typeof(LoggerService).Assembly), externals);

		[Fact]
		public void FirstRunWritesAllDefaultsWithoutStartingAnything() {
			var store = new InMemoryConfigStore();
			using var host = Discovered();
			Assert.Equal(StartupResult.DefaultsWritten, host.Start(store));
			Assert.Equal(new LoggerConfiguration(), store.Read<LoggerConfiguration>());
			Assert.Equal(new ExampleConfiguration(), store.Read<ExampleConfiguration>());
		}

		[Fact]
		public void DiscoveredServicesStartInDependencyOrder() {
			using var folder = new TempDirectory();
			var store = new InMemoryConfigStore();
			store.WriteDefaults(new LoggerConfiguration(folder.Path));
			store.WriteDefaults(new ExampleConfiguration());
			using var host = Discovered();
			var output = CaptureLegacyLog(() => Assert.Equal(StartupResult.Running, host.Start(store)));
			Assert.Contains("Example consumer initialized.", output);
			Assert.Empty(Directory.EnumerateFileSystemEntries(folder.Path));
			var logger = host.Get<ILogger>();
			Assert.Same(host.Get<LoggerService>(), logger);
			host.Dispose();
			Assert.Throws<ObjectDisposedException>(() => logger.WriteLine("after shutdown"));
		}

		private sealed class RecordingLog : ILogger {
			public object LastMessage;
			public void Emit(LogKind kind, object message, string file = null, int line = 0) => LastMessage = message;
		}

		[Fact]
		public void ConsumerRunsAgainstAStubbedLogger() {
			var store = new InMemoryConfigStore();
			store.WriteDefaults(new ExampleConfiguration("stubbed"));
			var log = new RecordingLog();
			using var host = new CoreServiceOrchestrator([typeof(ExampleConsumer)], log);
			Assert.Equal(StartupResult.Running, host.Start(store));
			Assert.Equal("stubbed", log.LastMessage);
		}
	}
}
