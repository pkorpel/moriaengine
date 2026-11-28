using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SteamEngine.DIExperiment {
	internal sealed class ServiceGraph {
		internal sealed class Plan(Type type) {
			internal Type Type { get; } = type;
			internal ConstructorInfo Constructor { get; } = SingleConstructor(type);
		}

		private readonly ILogger logger;
		private readonly object[] externals;
		private readonly List<Plan> candidates;

		internal IReadOnlyList<Plan> Plans { get; }

		// Logger settings belong to phase zero; each remaining settings type is loaded only once.
		internal IEnumerable<Type> SettingsTypes => Plans.SelectMany(p => p.Constructor.GetParameters())
			.Select(p => p.ParameterType)
			.Where(type => IsSettings(type) && type != typeof(LoggerConfiguration))
			.Distinct();

		/// <summary>Finds concrete services in the explicitly supplied assemblies.</summary>
		internal static IEnumerable<Type> Discover(params Assembly[] assemblies) => assemblies
			.SelectMany(a => a.GetTypes())
			.Where(t => t.IsClass && !t.IsAbstract && !t.ContainsGenericParameters && typeof(ICoreService).IsAssignableFrom(t))
			.OrderBy(t => t.FullName, StringComparer.Ordinal);

		/// <summary>Builds and validates a dependency-ordered plan for the supplied types without scanning assemblies.</summary>
		internal ServiceGraph(IEnumerable<Type> serviceTypes, ILogger logger, object[] externals) {
			this.logger = logger;
			this.externals = externals;
			// Discovery may include LoggerService, but it is always owned and started by phase zero.
			this.candidates = serviceTypes.Distinct().Where(t => t != typeof(LoggerService)).Select(t => {
				if (!typeof(ICoreService).IsAssignableFrom(t)) throw new ArgumentException($"{t} is not an {nameof(ICoreService)}.");
				return new Plan(t);
			}).ToList();
			this.Plans = Order();
		}

		private static ConstructorInfo SingleConstructor(Type type) {
			var constructors = type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (constructors.Length != 1) throw new ArgumentException($"{type} must have exactly one constructor.");
			return constructors[0];
		}

		private static bool IsSettings(Type type) => typeof(ICoreSettings).IsAssignableFrom(type);

		private bool IsExternal(Type type) => type.IsInstanceOfType(logger) || externals.Any(type.IsInstanceOfType);

		/// <summary>Finds one implementation, rejecting an ambiguous single-service dependency.</summary>
		internal Plan FindService(Type type) {
			var matches = FindServices(type).ToList();
			if (matches.Count > 1) throw new InvalidOperationException($"{type} is implemented by more than one service.");
			return matches.SingleOrDefault();
		}

		/// <summary>Finds matching planned services in supplied order; the phase-zero logger is handled separately.</summary>
		internal IEnumerable<Plan> FindServices(Type type) => candidates.Where(p => type.IsAssignableFrom(p.Type));

		/// <summary>Identifies the element type of a supported enumerable dependency.</summary>
		internal static bool TryGetEnumerableElementType(Type type, out Type elementType) {
			if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>)) {
				elementType = type.GetGenericArguments()[0];
				return true;
			}
			elementType = null;
			return false;
		}

		private List<Plan> Order() {
			var ordered = new List<Plan>();
			// A false entry marks an active DFS branch, so revisiting it identifies a cycle.
			var done = new Dictionary<Plan, bool>();
			void Visit(Plan plan) {
				if (done.TryGetValue(plan, out bool finished)) {
					if (!finished) throw new InvalidOperationException($"Dependency cycle through {plan.Type}.");
					return;
				}
				done[plan] = false;
				foreach (var parameter in plan.Constructor.GetParameters()) {
					var type = parameter.ParameterType;
					if (IsSettings(type) || IsExternal(type)) continue;
					// Every matching service must start before an enumerable's consumer; no matches is valid.
					if (TryGetEnumerableElementType(type, out var elementType)) {
						foreach (var dependency in FindServices(elementType)) Visit(dependency);
						continue;
					}
					Visit(FindService(type) ?? throw new InvalidOperationException(
						$"{plan.Type} needs {type} ({parameter.Name}), which no service implements."));
				}
				done[plan] = true;
				// Add the service only after all of its dependencies have been visited.
				ordered.Add(plan);
			}
			candidates.ForEach(Visit);
			return ordered;
		}
	}
}
