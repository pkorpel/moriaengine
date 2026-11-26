using System;
using System.Collections.Generic;
using SteamEngine.Common;
using SteamEngine.DIExperiment;
using SteamEngine.Tests.Fixtures;

namespace SteamEngine.Tests.DIExperiment.Orchestration {
	// These closed test types are selected explicitly; their open definitions are not assembly-discovered.
	internal static class OrchestratorTestServices {
		internal enum Failure { None, Validation, Constructor, Init, Dispose }

		// The trace records what the host actually did, including cleanup after exceptions.
		internal sealed class StartupTrace {
			internal List<string> Events { get; } = new();
			internal Failure Failure { get; init; }
		}

		internal sealed class TrackingStore(InMemoryConfigStore inner, StartupTrace trace) : IConfigStore {
			internal Action<Type> BeforeRead { get; set; }

			/// <summary>Records when the store is loaded relative to the service settings.</summary>
			public void Load() {
				trace.Events.Add("store.load");
				inner.Load();
			}

			/// <summary>Records each requested type and lets a test probe logger readiness during the read.</summary>
			public T Read<T>() where T : class {
				trace.Events.Add($"store.read {typeof(T).Name}");
				BeforeRead?.Invoke(typeof(T));
				return inner.Read<T>();
			}

			/// <summary>Records default writes while retaining the ordinary in-memory store behavior.</summary>
			public void WriteDefaults<T>(T defaults) where T : class {
				trace.Events.Add($"store.write {typeof(T).Name}");
				inner.WriteDefaults(defaults);
			}
		}

		internal sealed class TrackedSettings(StartupTrace trace) : ICoreSettings {
			/// <summary>Records validation and can fail before any service is constructed.</summary>
			public void Validate() {
				trace.Events.Add("settings.validate");
				if (trace.Failure == Failure.Validation) throw new SEException("invalid settings");
			}
		}

		internal sealed class TrackedDependency<T> : ICoreService {
			private readonly StartupTrace trace;
			internal TrackedSettings Settings { get; }
			internal bool Initialized { get; private set; }

			/// <summary>Records construction and retains the settings for identity assertions.</summary>
			public TrackedDependency(TrackedSettings settings, StartupTrace trace) {
				this.trace = trace;
				Settings = settings;
				trace.Events.Add("dependency.construct");
			}

			/// <summary>Marks the dependency ready so a consumer can verify startup order.</summary>
			public void Init() {
				trace.Events.Add("dependency.init");
				Initialized = true;
			}

			public void Dispose() => trace.Events.Add("dependency.dispose");
		}

		internal sealed class TrackedDependent<T> : ICoreService {
			private readonly StartupTrace trace;
			internal TrackedSettings Settings { get; }
			internal ILogger Logger { get; }

			/// <summary>Requires an initialized dependency and can simulate a constructor failure.</summary>
			public TrackedDependent(TrackedDependency<T> dependency, TrackedSettings settings, StartupTrace trace, ILogger logger) {
				// Checking readiness here catches ordering errors before the consumer can initialize.
				if (!dependency.Initialized) throw new InvalidOperationException("Dependency not initialized.");
				this.trace = trace;
				Settings = settings;
				Logger = logger;
				trace.Events.Add("dependent.construct");
				if (trace.Failure == Failure.Constructor) throw new InvalidOperationException("constructor failure");
			}

			/// <summary>Can fail after construction, when the instance must still be disposed.</summary>
			public void Init() {
				trace.Events.Add("dependent.init");
				if (trace.Failure == Failure.Init) throw new InvalidOperationException("init failure");
				Logger.WriteLine("Dependent initialized.");
			}

			/// <summary>Can fail while the host still needs to dispose its dependencies.</summary>
			public void Dispose() {
				trace.Events.Add("dependent.dispose");
				if (trace.Failure == Failure.Dispose) throw new InvalidOperationException("dispose failure");
			}
		}

		internal sealed class Downstream<T> : ICoreService {
			public Downstream(TrackedDependent<T> dependency, StartupTrace trace) => trace.Events.Add("downstream.construct");
			public void Init() { }
			public void Dispose() { }
		}

		internal interface IFirstRole { }
		internal interface ISecondRole { }

		internal sealed class MultiRoleService<T> : ICoreService, IFirstRole, ISecondRole {
			private readonly StartupTrace trace;
			/// <summary>Records construction so both interface resolutions can be checked for duplicate activation.</summary>
			public MultiRoleService(StartupTrace trace) {
				this.trace = trace;
				trace.Events.Add("roles.construct");
			}
			public void Init() => trace.Events.Add("roles.init");
			public void Dispose() => trace.Events.Add("roles.dispose");
		}

		internal sealed class UsesBothRoles<T> : ICoreService {
			internal IFirstRole First { get; }
			internal ISecondRole Second { get; }

			/// <summary>Retains both interfaces so the test can check they refer to one instance.</summary>
			public UsesBothRoles(IFirstRole first, ISecondRole second) {
				First = first;
				Second = second;
			}

			public void Init() { }
			public void Dispose() { }
		}
	}
}
