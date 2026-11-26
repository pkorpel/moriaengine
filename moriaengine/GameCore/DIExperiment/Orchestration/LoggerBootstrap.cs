using System;

namespace SteamEngine.DIExperiment {
	internal static class LoggerBootstrap {
		/// <summary>
		/// Loads and validates logger settings first, then starts either the configured or emergency logger.
		/// Returns false after logging a load, validation, or initialization failure.
		/// <paramref name="existingConfiguration"/> reports whether the store found an existing configuration.
		/// </summary>
		internal static bool Initialize(IConfigStore store, LoggerService loggerService, out bool existingConfiguration) {
			ILogger logger = loggerService;
			existingConfiguration = false;
			Exception configurationError = null;
			bool emergency = true;
			// Capture configuration failures until there is an initialized logger to report them.
			try {
				existingConfiguration = store.Load();
				// Missing keys are staged with their defaults; the store is saved after all settings validate.
				var configuration = store.Read<LoggerConfiguration>();
				configuration.Validate();
				loggerService.Configure(configuration);
				emergency = false;
			} catch (Exception ex) {
				configurationError = StartupException.Unwrap(ex);
			}

			// Even a malformed INI must be reported through an initialized logger, never a static fallback.
			// If the configured logger can't start, use emergency settings to report the failure.
			try {
				loggerService.Init();
			} catch (Exception ex) {
				configurationError = ex;
				emergency = true;
				loggerService.Configure(LoggerConfiguration.Emergency);
				loggerService.Init();
			}
			if (emergency) {
				logger.WriteWarning("LoggerService initialized with emergency configuration.");
			} else {
				logger.WriteDebug("LoggerService phase-zero init completed");
			}
			// Report a captured failure now that either the configured or emergency logger is running.
			if (configurationError != null) {
				logger.WriteError(configurationError);
				return false;
			}
			return true;
		}
	}
}
