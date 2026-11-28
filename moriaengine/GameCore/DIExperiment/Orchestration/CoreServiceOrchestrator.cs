using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SteamEngine.DIExperiment {
	internal enum StartupResult { Running, DefaultsWritten, Failed }

	// Owns startup and shutdown on one host thread. The logger is initialized before any other settings.
	internal sealed class CoreServiceOrchestrator : IDisposable {
		private readonly ServiceGraph graph;
		private readonly object[] externals;
		private readonly LoggerService loggerService;
		private readonly ILogger logger;
		private readonly Dictionary<Type, object> settings = new();
		private readonly Dictionary<ServiceGraph.Plan, ICoreService> built = new();
		private readonly List<ICoreService> constructed = new();
		private bool started;
		private bool disposed;

		internal static IEnumerable<Type> Discover(params Assembly[] assemblies) => ServiceGraph.Discover(assemblies);

		/// <summary>
		/// Plans only the supplied types. An external logger is the phase-zero logger's output sink;
		/// other externals take precedence over discovered services for the requested type.
		/// </summary>
		internal CoreServiceOrchestrator(IEnumerable<Type> serviceTypes, params object[] externals) {
			ArgumentNullException.ThrowIfNull(serviceTypes);
			ArgumentNullException.ThrowIfNull(externals);
			this.externals = (object[]) externals.Clone();
			this.loggerService = new LoggerService(LoggerConfiguration.Emergency, this.externals.OfType<ILogger>().SingleOrDefault());
			this.logger = this.loggerService;
			this.graph = new ServiceGraph(serviceTypes, logger, this.externals);
		}

		/// <summary>
		/// Starts the logger, validates settings, then constructs and initializes services in dependency order.
		/// Failures are logged and constructed services are disposed before returning.
		/// </summary>
		internal StartupResult Start(IConfigStore store) {
			ArgumentNullException.ThrowIfNull(store);
			ObjectDisposedException.ThrowIf(disposed, this);
			if (started) throw new InvalidOperationException("The orchestrator has already started.");
			started = true;

			// Phase 0: logger settings first, then the logger itself. From here on everything logs through it.
			if (!LoggerBootstrap.Initialize(store, loggerService, out bool existingConfiguration)) {
				Dispose();
				return StartupResult.Failed;
			}
			settings[typeof(LoggerConfiguration)] = loggerService.Settings;

			// Phase 1: every other service's settings, read and validated with the real logger running, then saved.
			var result = SettingsLoader.Load(store, graph.SettingsTypes, settings, logger, existingConfiguration);
			if (result != StartupResult.Running) {
				Dispose();
				return result;
			}

			// Phase 2: construct and Init services in dependency order.
			ServiceGraph.Plan current = null;
			string phase = null;
			try {
				foreach (var plan in graph.Plans) {
					current = plan;
					phase = "constructor";
					var args = plan.Constructor.GetParameters().Select(p => Resolve(p.ParameterType)).ToArray();
					var service = (ICoreService) plan.Constructor.Invoke(args);
					// Track the instance before Init so it is disposed even if Init fails.
					constructed.Add(service);
					phase = nameof(ICoreService.Init);
					logger.WriteDebug($"Initializing {plan.Type.Name}");
					using (logger.Indent()) {
						service.Init();
					}
					logger.WriteDebug($"{plan.Type.Name} init completed");
					built[plan] = service;
				}
				return StartupResult.Running;
			} catch (Exception ex) {
				logger.WriteError($"{current?.Type.Name}.{phase}", StartupException.Unwrap(ex));
				Dispose();
				return StartupResult.Failed;
			}
		}

		private object Resolve(Type type) {
			// Explicit settings and externals retain precedence over services from the graph.
			if (settings.TryGetValue(type, out var value)) return value;
			if (type.IsInstanceOfType(logger)) return logger;
			var external = externals.FirstOrDefault(type.IsInstanceOfType);
			if (external != null) return external;
			if (ServiceGraph.TryGetEnumerableElementType(type, out var elementType)) return ResolveAll(elementType);
			var plan = graph.FindService(type);
			if (plan == null || !built.TryGetValue(plan, out var service)) {
				throw new InvalidOperationException($"No running service implements {type}.");
			}
			return service;
		}

		private Array ResolveAll(Type elementType) {
			var plans = graph.FindServices(elementType).ToArray();
			bool includeLogger = elementType.IsInstanceOfType(logger);
			var instances = Array.CreateInstance(elementType, plans.Length + (includeLogger ? 1 : 0));
			int index = 0;

			// The phase-zero logger is owned separately and therefore has no graph plan.
			if (includeLogger) instances.SetValue(logger, index++);
			// The graph orders every matching service before a consumer of this collection.
			foreach (var plan in plans) {
				if (!built.TryGetValue(plan, out var service)) {
					throw new InvalidOperationException($"No running service implements {elementType}.");
				}
				instances.SetValue(service, index++);
			}
			return instances;
		}

		/// <summary>Resolves a type using the same rules as constructor injection.</summary>
		internal T Get<T>() {
			ObjectDisposedException.ThrowIf(disposed, this);
			return (T) Resolve(typeof(T));
		}

		/// <summary>Disposes constructed services in reverse order, reporting failures before disposing the logger.</summary>
		public void Dispose() {
			if (disposed) return;
			disposed = true;
			// Keep dependencies and the logger available while their consumers are being disposed.
			for (int i = constructed.Count - 1; i >= 0; i--) {
				try {
					constructed[i].Dispose();
				} catch (Exception ex) {
					logger.WriteError($"{constructed[i].GetType().Name}.Dispose", ex);
				}
			}
			constructed.Clear();
			built.Clear();
			// The logger stays alive for all disposal diagnostics and closes last.
			loggerService.Dispose();
		}
	}

	internal static class StartupException {
		internal static Exception Unwrap(Exception ex) =>
			ex is TargetInvocationException { InnerException: { } inner } ? inner : ex;
	}
}
