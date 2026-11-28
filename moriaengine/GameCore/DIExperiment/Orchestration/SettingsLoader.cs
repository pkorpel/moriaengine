using System;
using System.Collections.Generic;
using SteamEngine.Common;

namespace SteamEngine.DIExperiment {
	internal static class SettingsLoader {
		/// <summary>
		/// Reads and validates service settings, reporting all errors, then saves any staged defaults once.
		/// Startup stops if any setting is invalid, the save fails, or the configuration did not exist before.
		/// </summary>
		internal static StartupResult Load(IConfigStore store, IEnumerable<Type> types, IDictionary<Type, object> settings,
			ILogger logger, bool existingConfiguration) {
			bool invalid = false;
			// Continue after an error so every invalid setting is reported in one pass.
			foreach (var type in types) {
				try {
					var value = (ICoreSettings) store.Read(type);
					value.Validate();
					settings[type] = value;
				} catch (Exception ex) when (StartupException.Unwrap(ex) is SEException see) {
					// Preserve any file and line information already attached to the SEException.
					logger.WriteError(see);
					invalid = true;
				} catch (Exception ex) {
					logger.WriteError($"{type.Name}.{nameof(ICoreSettings.Validate)}", StartupException.Unwrap(ex));
					invalid = true;
				}
			}
			if (invalid) return StartupResult.Failed;

			// Nothing is written unless every setting is valid.
			try {
				store.Save();
			} catch (Exception ex) {
				logger.WriteError(StartupException.Unwrap(ex));
				return StartupResult.Failed;
			}
			// A newly generated configuration is reviewed before the next boot.
			return existingConfiguration ? StartupResult.Running : StartupResult.DefaultsWritten;
		}
	}
}
