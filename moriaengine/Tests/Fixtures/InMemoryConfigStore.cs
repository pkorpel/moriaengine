using System;
using System.Collections.Generic;
using SteamEngine.DIExperiment;

namespace SteamEngine.Tests.Fixtures {
	internal sealed class InMemoryConfigStore : IConfigStore {
		private readonly Dictionary<Type, object> values = new();

		public T Read<T>() where T : class => values.TryGetValue(typeof(T), out var value) ? (T)value : null;

		public void WriteDefaults<T>(T defaults) where T : class => values.Add(typeof(T), defaults);
	}
}
