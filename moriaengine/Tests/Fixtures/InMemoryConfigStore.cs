using System;
using System.Collections.Generic;
using SteamEngine.DIExperiment;

namespace SteamEngine.Tests.Fixtures {
	internal sealed class InMemoryConfigStore : IConfigStore {
		private readonly Dictionary<Type, object> values = new();

		public bool Load() => true;

		public void Save() { }

		/// <summary>Supplies one shared settings instance, using its defaults unless the test supplied values.</summary>
		public object Read(Type type) {
			if (!values.TryGetValue(type, out var value)) {
				value = Activator.CreateInstance(type);
				values.Add(type, value);
			}
			return value;
		}

		internal void WriteDefaults<T>(T defaults) where T : class => values.Add(typeof(T), defaults);
	}
}
