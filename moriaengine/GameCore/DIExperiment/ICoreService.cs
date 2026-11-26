using System;

namespace SteamEngine.DIExperiment {
	// A process-lifetime service. Its constructor takes validated settings and its dependencies.
	// Construction must not do I/O; Init does the real work.
	internal interface ICoreService : IDisposable {
		void Init();
	}

	// A service's typed settings. A public constructor whose parameters all have defaults supplies the defaults.
	// Validate runs before any service is constructed, so it must not need another service.
	internal interface ICoreSettings {
		void Validate();
	}

	internal interface IConfigStore {
		// Null means absent; malformed configuration throws. Defaults must not overwrite an existing entry.
		T Read<T>() where T : class;
		void WriteDefaults<T>(T defaults) where T : class;
	}
}
