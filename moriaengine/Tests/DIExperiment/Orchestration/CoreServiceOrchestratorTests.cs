using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SharpConfig;
using SteamEngine.DIExperiment;
using SteamEngine.Tests.DIExperiment.Examples;
using SteamEngine.Tests.Fixtures;
using Xunit;
using Xunit.Abstractions;
using static SteamEngine.Tests.DIExperiment.Orchestration.GraphTestServices;
using static SteamEngine.Tests.DIExperiment.Orchestration.OrchestratorTestServices;

namespace SteamEngine.Tests.DIExperiment.Orchestration {
	public sealed class CoreServiceOrchestratorTests {
		private readonly ITestOutputHelper output;

		public CoreServiceOrchestratorTests(ITestOutputHelper output) {
			this.output = output;
		}

		/// <summary>Checks the phase boundary, shared dependencies, and reverse-order shutdown together.</summary>
		[Fact]
		public void StartsDependenciesAfterConfigurationAndDisposesInReverseOrder() {
			var trace = new StartupTrace();
			var sink = new RecordingLogger();
			var innerStore = ConfiguredStore(trace);
			var store = new TrackingStore(innerStore, trace);
			using var host = CreateHost(trace, sink);
			// A logger call during settings loading fails if phase zero has not initialized it.
			store.BeforeRead = type => {
				if (type == typeof(TrackedSettings)) {
					host.Get<ILogger>().WriteLine("Logger is ready during settings loading.");
				}
			};

			// The requested type order is deliberately backwards: the dependency must start first.
			Assert.Equal(StartupResult.Running, host.Start(store));
			var dependency = host.Get<TrackedDependency<int>>();
			var dependent = host.Get<TrackedDependent<int>>();
			Assert.Same(innerStore.Read<TrackedSettings>(), dependency.Settings);
			Assert.Same(dependency.Settings, dependent.Settings);
			Assert.Same(host.Get<ILogger>(), dependent.Logger);
			Assert.Contains(sink.Entries, e => e.Message?.ToString() == "Logger is ready during settings loading.");
			Assert.Contains(sink.Entries, e => e.Message?.ToString() == "Dependent initialized." && e.Indentation == "\t");
			Assert.Equal(new[] {
				"store.load", "store.read LoggerConfiguration", "store.read TrackedSettings", "settings.validate",
				"dependency.construct", "dependency.init", "dependent.construct", "dependent.init"
			}, trace.Events);

			// A second start is refused, and repeated disposal must not dispose anything twice.
			Assert.Throws<InvalidOperationException>(() => host.Start(store));
			host.Dispose();
			host.Dispose();
			Assert.Throws<ObjectDisposedException>(() => host.Get<ILogger>());
			Assert.Equal(new[] {
				"store.load", "store.read LoggerConfiguration", "store.read TrackedSettings", "settings.validate",
				"dependency.construct", "dependency.init", "dependent.construct", "dependent.init",
				"dependent.dispose", "dependency.dispose"
			}, trace.Events);
			Print(sink);
		}

		/// <summary>A failed constructor has no instance to dispose; a failed Init does, and neither starts dependents.</summary>
		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public void ConstructorOrInitFailureStopsStartupAndDisposesConstructedServices(bool inConstructor) {
			var trace = new StartupTrace { Failure = inConstructor ? Failure.Constructor : Failure.Init };
			var sink = new RecordingLogger();
			using var host = new CoreServiceOrchestrator(
				[typeof(Downstream<int>), typeof(TrackedDependent<int>), typeof(TrackedDependency<int>)], trace, sink);

			Assert.Equal(StartupResult.Failed, host.Start(ConfiguredStore(trace)));
			// Only successfully constructed instances enter the disposal list; downstream never starts.
			var expected = inConstructor
				? new[] { "settings.validate", "dependency.construct", "dependency.init", "dependent.construct", "dependency.dispose" }
				: new[] { "settings.validate", "dependency.construct", "dependency.init", "dependent.construct",
					"dependent.init", "dependent.dispose", "dependency.dispose" };
			Assert.Equal(expected, trace.Events);
			var error = Assert.Single(sink.Entries.Where(e => e.Kind == LogKind.Error));
			Assert.Contains(inConstructor ? "constructor failure" : "init failure", error.Display);
			Assert.DoesNotContain("TargetInvocationException", error.Display);
			Print(sink);
		}

		/// <summary>Shutdown must continue through dependencies after logging a consumer's disposal failure.</summary>
		[Fact]
		public void DisposeFailureIsLoggedAndDoesNotPreventDependencyCleanup() {
			var trace = new StartupTrace { Failure = Failure.Dispose };
			var sink = new RecordingLogger();
			using var host = CreateHost(trace, sink);
			Assert.Equal(StartupResult.Running, host.Start(ConfiguredStore(trace)));

			// The logger must remain available while the host reports the disposal failure.
			host.Dispose();

			Assert.Equal(new[] {
				"settings.validate", "dependency.construct", "dependency.init", "dependent.construct", "dependent.init",
				"dependent.dispose", "dependency.dispose"
			}, trace.Events);
			Assert.Contains(sink.Entries, e => e.Kind == LogKind.Error && e.Display.Contains("dispose failure"));
			Print(sink);
		}

		/// <summary>Invalid service settings must stop the host before any service is constructed.</summary>
		[Fact]
		public void InvalidSettingsStopServiceConstruction() {
			var trace = new StartupTrace { Failure = Failure.Validation };
			var sink = new RecordingLogger();
			using var host = CreateHost(trace, sink);

			Assert.Equal(StartupResult.Failed, host.Start(ConfiguredStore(trace)));
			Assert.Equal(new[] { "settings.validate" }, trace.Events);
			Assert.Contains(sink.Entries, e => e.Kind == LogKind.Error && e.Display.Contains("invalid settings"));
			Print(sink);
		}

		/// <summary>A phase-zero failure is reported without reading or constructing other services.</summary>
		[Fact]
		public void LoggerFailureStopsBeforeServiceSettingsAreRead() {
			var trace = new StartupTrace();
			var store = new InMemoryConfigStore();
			// Fail phase zero itself, before the tracked service settings are read.
			store.WriteDefaults(new LoggerConfiguration(LogPath: "", LogToFiles: true));
			store.WriteDefaults(new TrackedSettings(trace));
			var sink = new RecordingLogger();
			using var host = CreateHost(trace, sink);

			Assert.Equal(StartupResult.Failed, host.Start(new TrackingStore(store, trace)));
			Assert.Equal(new[] { "store.load", "store.read LoggerConfiguration" }, trace.Events);
			Assert.Contains(sink.Entries, e => e.Kind == LogKind.Error);
			Print(sink);
		}

		/// <summary>Missing logger settings still leave a working logger to report later validation errors.</summary>
		[Fact]
		public void EmergencyLoggerReportsInvalidServiceSettings() {
			var trace = new StartupTrace { Failure = Failure.Validation };
			var store = new InMemoryConfigStore();
			store.WriteDefaults(new TrackedSettings(trace));
			var sink = new RecordingLogger();
			using var host = CreateHost(trace, sink);

			// Phase zero writes logger defaults, but an invalid service setting still makes startup fail.
			Assert.Equal(StartupResult.Failed, host.Start(store));
			Assert.Equal(new LoggerConfiguration(), store.Read<LoggerConfiguration>());
			Assert.Equal(new[] { "settings.validate" }, trace.Events);
			Assert.Contains(sink.Entries, e => e.Kind == LogKind.Warning && e.Display.Contains("emergency configuration"));
			Assert.Contains(sink.Entries, e => e.Kind == LogKind.Error && e.Display.Contains("invalid settings"));
			Print(sink);
		}

		/// <summary>An explicitly supplied single dependency replaces discovered candidates and is not disposed by the host.</summary>
		[Fact]
		public void ExternalDependencyWinsOverDiscoveredServicesAndIsNotOwned() {
			var sink = new RecordingLogger();
			var external = new ExternalBackend();
			// The two service types would otherwise be an ambiguous single dependency.
			using var host = new CoreServiceOrchestrator(
				[typeof(DependsOn<IBackend>), typeof(FirstBackend<int>), typeof(SecondBackend<int>)], external, sink);

			Assert.Equal(StartupResult.Running, host.Start(LoggerOnlyStore()));
			Assert.Same(external, host.Get<DependsOn<IBackend>>().Dependency);
			Assert.Same(external, host.Get<IBackend>());
			host.Dispose();
			Assert.False(external.Disposed);
			Print(sink);
		}

		/// <summary>All selected implementations are started before a consumer receives their shared instances.</summary>
		[Fact]
		public void EnumerableReceivesEveryInitializedImplementation() {
			var sink = new RecordingLogger();
			// Put the consumer first to prove every collection element is initialized before its constructor.
			using var host = new CoreServiceOrchestrator(
				[typeof(UsesBackends<int>), typeof(FirstBackend<int>), typeof(SecondBackend<int>)], sink);

			Assert.Equal(StartupResult.Running, host.Start(LoggerOnlyStore()));
			var consumer = host.Get<UsesBackends<int>>();
			var first = host.Get<FirstBackend<int>>();
			var second = host.Get<SecondBackend<int>>();
			Assert.True(consumer.DependenciesWereInitialized);
			Assert.Equal(2, consumer.Dependencies.Length);
			Assert.Contains(first, consumer.Dependencies);
			Assert.Contains(second, consumer.Dependencies);
			Assert.Equal(1, first.InitCount);
			Assert.Equal(1, second.InitCount);

			// Resolving the collection again must reuse the same initialized services.
			var resolved = host.Get<IEnumerable<IBackend>>().ToArray();
			Assert.Equal(2, resolved.Length);
			Assert.Contains(first, resolved);
			Assert.Contains(second, resolved);
			Print(sink);
		}

		/// <summary>Different interfaces of one service resolve to one instance, started and disposed once.</summary>
		[Fact]
		public void MultipleInterfacesShareOneServiceInstance() {
			var trace = new StartupTrace();
			var sink = new RecordingLogger();
			using var host = new CoreServiceOrchestrator([typeof(UsesBothRoles<int>), typeof(MultiRoleService<int>)], trace, sink);

			Assert.Equal(StartupResult.Running, host.Start(LoggerOnlyStore()));
			// Both constructor arguments and later lookups must point to the same started object.
			var service = host.Get<MultiRoleService<int>>();
			var consumer = host.Get<UsesBothRoles<int>>();
			Assert.Same(service, host.Get<IFirstRole>());
			Assert.Same(service, host.Get<ISecondRole>());
			Assert.Same(service, Assert.Single(host.Get<IEnumerable<IFirstRole>>()));
			Assert.Same(service, Assert.Single(host.Get<IEnumerable<ISecondRole>>()));
			Assert.Same(service, consumer.First);
			Assert.Same(service, consumer.Second);
			Assert.Equal(new[] { "roles.construct", "roles.init" }, trace.Events);

			host.Dispose();
			Assert.Equal(new[] { "roles.construct", "roles.init", "roles.dispose" }, trace.Events);
			Print(sink);
		}

		/// <summary>A selected consumer gets an empty collection rather than implicitly scanning for services.</summary>
		[Fact]
		public void UnselectedServicesAreNotDiscoveredAndAnEmptyCollectionIsInjected() {
			var sink = new RecordingLogger();
			// Backend types exist in this assembly, but only the consumer is selected for this test.
			using var host = new CoreServiceOrchestrator([typeof(UsesBackends<int>)], sink);

			Assert.Equal(StartupResult.Running, host.Start(LoggerOnlyStore()));
			Assert.Empty(host.Get<UsesBackends<int>>().Dependencies);
			Assert.Empty(host.Get<IEnumerable<IBackend>>());
			Assert.Throws<InvalidOperationException>(() => host.Get<IBackend>());
			Assert.Throws<InvalidOperationException>(() => host.Get<MultiDataService>());
			Assert.Throws<InvalidOperationException>(() => host.Get<ExampleCatalog>());
			// The mandatory logger is a service even though it is not represented by a graph plan.
			Assert.Same(host.Get<ILogger>(), Assert.Single(host.Get<IEnumerable<ILogger>>()));
			Print(sink);
		}

		/// <summary>Tests can explicitly supply a collection without scanning for its element types.</summary>
		[Fact]
		public void ExplicitCollectionCanSupplyTestDependencies() {
			var sink = new RecordingLogger();
			// A test can supply exactly the doubles it needs without scanning for implementations.
			IEnumerable<IBackend> dependencies = new IBackend[] { new ExternalBackend(), new ExternalBackend() };
			using var host = new CoreServiceOrchestrator([typeof(UsesBackends<int>)], dependencies, sink);

			Assert.Equal(StartupResult.Running, host.Start(LoggerOnlyStore()));
			Assert.Same(dependencies, host.Get<IEnumerable<IBackend>>());
			Assert.Equal(dependencies, host.Get<UsesBackends<int>>().Dependencies);
			Print(sink);
		}

		/// <summary>Illustrates a real service going through first-run defaults and then normal startup.</summary>
		[Fact]
		public void MultiDataStartsAfterWritingMissingDefaults() {
			using var folder = new TempDirectory();
			string path = Path.Combine(folder.Path, "steamengine.ini");
			File.WriteAllText(path, $"[files]\nLogPath={folder.Path}\nLogToFiles=false\nMulPath={folder.Path}\nUseMultiItems=false\n");
			var firstLog = new RecordingLogger();

			// First run fills missing keys but must not initialize the service.
			using (var firstHost = new CoreServiceOrchestrator([typeof(MultiDataService)], firstLog)) {
				Assert.Equal(StartupResult.DefaultsWritten, firstHost.Start(new SharpConfigStore(path)));
			}
			var ini = Configuration.LoadFromFile(path);
			Assert.True(ini["files"].Contains("WriteMulDocsFiles"));
			Assert.True(ini["files"].Contains("DocsPath"));
			Assert.DoesNotContain(firstLog.Entries, entry => entry.Message?.ToString() == "Initializing MultiDataService");
			Print(firstLog);

			// The completed file can now start the same service; its own message is indented inside Init.
			var secondLog = new RecordingLogger();
			using var secondHost = new CoreServiceOrchestrator([typeof(MultiDataService)], secondLog);
			Assert.Equal(StartupResult.Running, secondHost.Start(new SharpConfigStore(path)));
			Assert.Same(secondHost.Get<MultiDataService>(), secondHost.Get<IMultiDataCatalog>());
			var message = Assert.Single(secondLog.Entries.Where(entry => entry.Message?.ToString() == "Ignoring multi.mul"));
			Assert.StartsWith("\t", message.Indentation);
			Print(secondLog);
		}

		private static CoreServiceOrchestrator CreateHost(StartupTrace trace, RecordingLogger sink) =>
			new([typeof(TrackedDependent<int>), typeof(TrackedDependency<int>)], trace, sink);

		private static InMemoryConfigStore LoggerOnlyStore() {
			var store = new InMemoryConfigStore();
			store.WriteDefaults(new LoggerConfiguration(LogToFiles: false));
			return store;
		}

		private static InMemoryConfigStore ConfiguredStore(StartupTrace trace) {
			var store = LoggerOnlyStore();
			store.WriteDefaults(new TrackedSettings(trace));
			return store;
		}

		private void Print(RecordingLogger sink) {
			foreach (var entry in sink.Entries) {
				output.WriteLine($"{entry.Kind}: {entry.Display}");
			}
		}
	}
}
