using System;

namespace SteamEngine.DIExperiment {
	// A process-lifetime service. Its constructor takes validated settings and its dependencies.
	// Construction must not do I/O; Init does the real work.
	internal interface ICoreService : IDisposable {
		void Init();
	}

	// A service's typed settings have a public parameterless constructor and defaults on their properties.
	// Validate runs before any service is constructed, so it must not need another service.
	internal interface ICoreSettings {
		void Validate();
	}

	// Not a discovered service: the orchestrator needs it before any service, and the caller picks it (file or fake).
	// Like a service, its constructor does no I/O; the orchestrator calls Load first and logs its failure.
	internal interface IConfigStore {
		// False means the configuration is missing; the host reads its defaults but starts no services.
		bool Load();

		// Stage missing keys, retaining existing values. The type needs a public parameterless constructor.
		// Invalid input throws.
		object Read(Type type);

		T Read<T>() where T : class, new() => (T) Read(typeof(T));

		// Persist the staged configuration after every settings type has been validated.
		void Save();
	}
}
