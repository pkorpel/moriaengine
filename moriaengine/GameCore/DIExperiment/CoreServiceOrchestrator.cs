using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SteamEngine.Common;

namespace SteamEngine.DIExperiment {
	internal enum StartupResult { Running, DefaultsWritten, Failed }

	// Starts the given service types in dependency order, worked out from their constructors. Settings of every
	// service are read and validated before any service is constructed. Called by a single host thread.
	internal sealed class CoreServiceOrchestrator : IDisposable {
		private sealed class Plan(Type type) {
			internal Type Type { get; } = type;
			internal ConstructorInfo Constructor { get; } = SingleConstructor(type);
		}

		private readonly List<Plan> plans;
		private readonly object[] externals;
		private readonly Dictionary<Type, object> settings = new();
		private readonly Dictionary<Plan, ICoreService> built = new();
		private readonly List<ICoreService> constructed = new();
		private bool started;
		private bool disposed;

		internal static IEnumerable<Type> Discover(params Assembly[] assemblies) => assemblies
			.SelectMany(a => a.GetTypes())
			.Where(t => t.IsClass && !t.IsAbstract && !t.ContainsGenericParameters && typeof(ICoreService).IsAssignableFrom(t))
			.OrderBy(t => t.FullName, StringComparer.Ordinal);

		// Externals stand in for services (stubs, test recorders) and take precedence over discovered services.
		internal CoreServiceOrchestrator(IEnumerable<Type> serviceTypes, params object[] externals) {
			ArgumentNullException.ThrowIfNull(serviceTypes);
			ArgumentNullException.ThrowIfNull(externals);
			this.externals = (object[]) externals.Clone();
			var candidates = serviceTypes.Distinct().Select(t => {
				if (!typeof(ICoreService).IsAssignableFrom(t)) throw new ArgumentException($"{t} is not an {nameof(ICoreService)}.");
				return new Plan(t);
			}).ToList();
			this.plans = candidates;
			this.plans = Order(candidates);
		}

		private static ConstructorInfo SingleConstructor(Type type) {
			var constructors = type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (constructors.Length != 1) throw new ArgumentException($"{type} must have exactly one constructor.");
			return constructors[0];
		}

		private static bool IsSettings(Type type) => typeof(ICoreSettings).IsAssignableFrom(type);

		private bool IsExternal(Type type) => externals.Any(type.IsInstanceOfType);

		private Plan FindService(Type type) {
			var matches = plans.Where(p => type.IsAssignableFrom(p.Type)).ToList();
			if (matches.Count > 1) throw new InvalidOperationException($"{type} is implemented by more than one service.");
			return matches.SingleOrDefault();
		}

		private List<Plan> Order(List<Plan> candidates) {
			var ordered = new List<Plan>();
			var done = new Dictionary<Plan, bool>(); // false while visiting
			void Visit(Plan plan) {
				if (done.TryGetValue(plan, out bool finished)) {
					if (!finished) throw new InvalidOperationException($"Dependency cycle through {plan.Type}.");
					return;
				}
				done[plan] = false;
				foreach (var parameter in plan.Constructor.GetParameters()) {
					var type = parameter.ParameterType;
					if (IsSettings(type) || IsExternal(type)) continue;
					Visit(FindService(type) ?? throw new InvalidOperationException(
						$"{plan.Type} needs {type} ({parameter.Name}), which no service implements."));
				}
				done[plan] = true;
				ordered.Add(plan);
			}
			candidates.ForEach(Visit);
			return ordered;
		}

		internal StartupResult Start(IConfigStore store) {
			ArgumentNullException.ThrowIfNull(store);
			ObjectDisposedException.ThrowIf(disposed, this);
			if (started) throw new InvalidOperationException("The orchestrator has already started.");
			started = true;

			if (!ConfigureAll(store, out var result)) {
				Dispose();
				return result;
			}

			Plan current = null;
			string phase = null;
			try {
				foreach (var plan in plans) {
					current = plan;
					phase = "constructor";
					var args = plan.Constructor.GetParameters().Select(p => Resolve(p.ParameterType)).ToArray();
					var service = (ICoreService) plan.Constructor.Invoke(args);
					constructed.Add(service);
					phase = nameof(ICoreService.Init);
					service.Init();
					built[plan] = service;
				}
				return StartupResult.Running;
			} catch (Exception ex) {
				Logger.WriteError($"{current?.Type.Name}.{phase}", Unwrap(ex));
				Dispose();
				return StartupResult.Failed;
			}
		}

		// Reads every service's settings; writes defaults for all missing ones and validates the rest.
		private bool ConfigureAll(IConfigStore store, out StartupResult result) {
			var types = plans.SelectMany(p => p.Constructor.GetParameters()).Select(p => p.ParameterType)
				.Where(IsSettings).Distinct();
			bool defaultsWritten = false, invalid = false;
			foreach (var type in types) {
				try {
					var value = Invoke(store, nameof(IConfigStore.Read), type);
					if (value == null) {
						Invoke(store, nameof(IConfigStore.WriteDefaults), type, CreateDefaults(type));
						defaultsWritten = true;
					} else {
						((ICoreSettings) value).Validate();
						settings[type] = value;
					}
				} catch (Exception ex) {
					Logger.WriteError($"{type.Name}.{nameof(ICoreSettings.Validate)}", Unwrap(ex));
					invalid = true;
				}
			}
			result = invalid ? StartupResult.Failed : defaultsWritten ? StartupResult.DefaultsWritten : StartupResult.Running;
			return result == StartupResult.Running;
		}

		private static object Invoke(IConfigStore store, string method, Type type, params object[] args) =>
			typeof(IConfigStore).GetMethod(method).MakeGenericMethod(type).Invoke(store, args);

		private static object CreateDefaults(Type type) {
			var constructor = type.GetConstructors().FirstOrDefault(c => c.GetParameters().All(p => p.HasDefaultValue))
				?? throw new InvalidOperationException($"{type} needs a public constructor whose parameters all have defaults.");
			return constructor.Invoke(constructor.GetParameters().Select(p => p.DefaultValue).ToArray());
		}

		private static Exception Unwrap(Exception ex) => ex is TargetInvocationException { InnerException: { } inner } ? inner : ex;

		private object Resolve(Type type) {
			if (settings.TryGetValue(type, out var value)) return value;
			var external = externals.FirstOrDefault(type.IsInstanceOfType);
			if (external != null) return external;
			var plan = FindService(type);
			if (plan == null || !built.TryGetValue(plan, out var service)) {
				throw new InvalidOperationException($"No running service implements {type}.");
			}
			return service;
		}

		internal T Get<T>() {
			ObjectDisposedException.ThrowIf(disposed, this);
			return (T) Resolve(typeof(T));
		}

		public void Dispose() {
			if (disposed) return;
			disposed = true;
			for (int i = constructed.Count - 1; i >= 0; i--) {
				try {
					constructed[i].Dispose();
				} catch (Exception ex) {
					Logger.WriteError($"{constructed[i].GetType().Name}.Dispose", ex);
				}
			}
			constructed.Clear();
			built.Clear();
		}
	}
}
