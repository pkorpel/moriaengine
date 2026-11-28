using System;

namespace SteamEngine.DIExperiment {
	// A process-lifetime service. Except for the phase-zero logger, its constructor takes validated settings
	// and its dependencies.
	// Construction must not do I/O; Init does the real work.
	internal interface ICoreService : IDisposable {
		void Init();
	}

	// A service's typed settings have a public parameterless constructor and defaults on their properties.
	// Validate runs before any service is constructed, so it must not need another service.
	// Phase zero validates logger settings first; the initialized logger reports validation of all other
	// settings before their services are constructed.
	internal interface ICoreSettings {
		void Validate();
	}

	// The caller picks the store (file or fake). Its constructor does no I/O.
	// Phase zero loads it; a failure is reported after initializing the logger with emergency settings.
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
