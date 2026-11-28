using System;
using System.Collections.Generic;
using System.Linq;
using SteamEngine.DIExperiment;
using SteamEngine.Tests.DIExperiment.Examples;
using SteamEngine.Tests.Fixtures;
using Xunit;
using static SteamEngine.Tests.DIExperiment.Orchestration.GraphTestServices;

namespace SteamEngine.Tests.DIExperiment.Orchestration {
	public sealed class ServiceGraphTests {
		/// <summary>A dependency must precede its consumer even when the input order is reversed.</summary>
		[Fact]
		public void OrdersDependenciesBeforeConsumers() {
			// Supply the consumer first so the graph, not the input order, must put its dependency first.
			var graph = Create(typeof(DependsOn<FirstBackend<int>>), typeof(FirstBackend<int>));

			Assert.Equal(new[] { typeof(FirstBackend<int>), typeof(DependsOn<FirstBackend<int>>) },
				graph.Plans.Select(plan => plan.Type));
		}

		/// <summary>Every implementation in an enumerable is an edge that must precede its consumer.</summary>
		[Fact]
		public void OrdersEveryEnumerableDependencyBeforeItsConsumer() {
			var graph = Create(typeof(UsesBackends<int>), typeof(FirstBackend<int>), typeof(SecondBackend<int>));
			var order = graph.Plans.Select(plan => plan.Type).ToArray();

			// Siblings may be in either order, but both must exist and precede the consumer.
			Assert.Equal(3, order.Length);
			Assert.Contains(typeof(FirstBackend<int>), order);
			Assert.Contains(typeof(SecondBackend<int>), order);
			Assert.Contains(typeof(UsesBackends<int>), order);
			Assert.True(Array.IndexOf(order, typeof(FirstBackend<int>)) < Array.IndexOf(order, typeof(UsesBackends<int>)));
			Assert.True(Array.IndexOf(order, typeof(SecondBackend<int>)) < Array.IndexOf(order, typeof(UsesBackends<int>)));
		}

		/// <summary>A consumer can request a collection even when no service implements its element type.</summary>
		[Fact]
		public void AnEmptyEnumerableDoesNotRequireAService() {
			var graph = Create(typeof(UsesBackends<int>));

			Assert.Single(graph.Plans);
		}

		/// <summary>Shared settings are loaded once, while logger settings remain part of phase zero.</summary>
		[Fact]
		public void ListsEachServiceSettingsTypeOnceAndLeavesLoggerToPhaseZero() {
			var graph = Create(typeof(NeedsSettings<int>), typeof(NeedsSettings<string>), typeof(LoggerService));

			Assert.Equal(new[] { typeof(SharedSettings) }, graph.SettingsTypes);
			Assert.DoesNotContain(graph.Plans, plan => plan.Type == typeof(LoggerService));
		}

		/// <summary>Explicit scanning finds real services without picking up open generic test types.</summary>
		[Fact]
		public void DiscoveryFindsConcreteServicesButExcludesOpenGenericTypes() {
			var types = ServiceGraph.Discover(typeof(LoggerService).Assembly, typeof(ExampleCatalog).Assembly).ToArray();
			Assert.Contains(typeof(LoggerService), types);
			Assert.Contains(typeof(MultiDataService), types);
			Assert.Contains(typeof(ExampleCatalog), types);
			Assert.DoesNotContain(typeof(DependsOn<>), types);
		}

		/// <summary>A missing single dependency is reported while planning rather than during startup.</summary>
		[Fact]
		public void RejectsMissingDependencies() {
			var error = Assert.Throws<InvalidOperationException>(() => Create(typeof(DependsOn<string>)));
			Assert.Contains("no service implements", error.Message);
		}

		/// <summary>A singular dependency with multiple implementations requires an explicit choice.</summary>
		[Fact]
		public void RejectsAmbiguousDependencies() {
			var error = Assert.Throws<InvalidOperationException>(() =>
				Create(typeof(DependsOn<IBackend>), typeof(FirstBackend<int>), typeof(SecondBackend<int>)));
			Assert.Contains("more than one service", error.Message);
		}

		/// <summary>An explicit single dependency avoids an ambiguity among service types.</summary>
		[Fact]
		public void ExternalDependencyAvoidsAmbiguity() {
			var graph = new ServiceGraph(
				[typeof(DependsOn<IBackend>), typeof(FirstBackend<int>), typeof(SecondBackend<int>)],
				new RecordingLogger(), [new ExternalBackend()]);

			Assert.Equal(3, graph.Plans.Count);
		}

		/// <summary>The graph detects cycles between ordinary constructor dependencies.</summary>
		[Fact]
		public void RejectsCycles() {
			var error = Assert.Throws<InvalidOperationException>(() => Create(typeof(CycleA<int>), typeof(CycleB<int>)));
			Assert.Contains("Dependency cycle", error.Message);
		}

		/// <summary>A service depending on a collection containing itself is also a cycle.</summary>
		[Fact]
		public void RejectsCyclesThroughEnumerableDependencies() {
			var error = Assert.Throws<InvalidOperationException>(() => Create(typeof(EnumerableCycle<int>)));
			Assert.Contains("Dependency cycle", error.Message);
		}

		/// <summary>Planning rejects a service when it cannot choose a unique constructor.</summary>
		[Fact]
		public void RejectsServicesWithMultipleConstructors() {
			var error = Assert.Throws<ArgumentException>(() => Create(typeof(MultipleConstructors<int>)));
			Assert.Contains("exactly one constructor", error.Message);
		}

		private static ServiceGraph Create(params Type[] types) => new(types, new RecordingLogger(), []);

		private sealed class SharedSettings : ICoreSettings {
			public void Validate() { }
		}

		// Closed types are supplied to the graph; assembly discovery skips these open generic definitions.
		private sealed class NeedsSettings<T> : ICoreService {
			public NeedsSettings(SharedSettings settings, LoggerConfiguration loggerSettings) { }
			public void Init() { }
			public void Dispose() { }
		}

		private sealed class CycleA<T> : ICoreService {
			public CycleA(CycleB<T> dependency) { }
			public void Init() { }
			public void Dispose() { }
		}

		private sealed class CycleB<T> : ICoreService {
			public CycleB(CycleA<T> dependency) { }
			public void Init() { }
			public void Dispose() { }
		}

		private sealed class EnumerableCycle<T> : ICoreService, IBackend {
			public EnumerableCycle(IEnumerable<IBackend> dependencies) { }
			public bool Initialized => false;
			public void Init() { }
			public void Dispose() { }
		}

		private sealed class MultipleConstructors<T> : ICoreService {
			public MultipleConstructors() { }
			public MultipleConstructors(int value) { }
			public void Init() { }
			public void Dispose() { }
		}
	}

	// Shared with the host tests: open generic service definitions do not become discovered services.
	internal static class GraphTestServices {
		internal interface IBackend {
			bool Initialized { get; }
		}

		internal sealed class ExternalBackend : IBackend, IDisposable {
			internal bool Disposed { get; private set; }
			public bool Initialized => true;
			public void Dispose() => Disposed = true;
		}

		internal sealed class DependsOn<T> : ICoreService {
			internal T Dependency { get; }
			public DependsOn(T dependency) => Dependency = dependency;
			public void Init() { }
			public void Dispose() { }
		}

		internal sealed class UsesBackends<T> : ICoreService {
			internal IBackend[] Dependencies { get; }
			internal bool DependenciesWereInitialized { get; }

			/// <summary>Captures dependency readiness during construction, before this consumer starts.</summary>
			public UsesBackends(IEnumerable<IBackend> dependencies) {
				Dependencies = dependencies.ToArray();
				DependenciesWereInitialized = Dependencies.All(dependency => dependency.Initialized);
			}

			public void Init() { }
			public void Dispose() { }
		}

		internal sealed class FirstBackend<T> : ICoreService, IBackend {
			internal int InitCount { get; private set; }
			public bool Initialized => InitCount > 0;
			public void Init() => InitCount++;
			public void Dispose() { }
		}

		internal sealed class SecondBackend<T> : ICoreService, IBackend {
			internal int InitCount { get; private set; }
			public bool Initialized => InitCount > 0;
			public void Init() => InitCount++;
			public void Dispose() { }
		}
	}
}
