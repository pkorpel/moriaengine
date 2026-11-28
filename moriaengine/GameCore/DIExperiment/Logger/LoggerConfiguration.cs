using System.ComponentModel;
using System.IO;

namespace SteamEngine.DIExperiment {
	[IniSection("files")]
	internal sealed record LoggerConfiguration : ICoreSettings {
		[Description("Path to the log files")]
		public string LogPath { get; init; } = "./logs/";

		[Description("Whether to log console output to a file")]
		public bool LogToFiles { get; init; } = true;

		// Phase zero can always initialize diagnostics without needing a writable log directory.
		internal static LoggerConfiguration Emergency { get; } = new() { LogPath = "", LogToFiles = false };

		/// <summary>Checks that file logging, when enabled, has a writable destination.</summary>
		public void Validate() {
			if (!LogToFiles) return;
			if (string.IsNullOrWhiteSpace(LogPath)) {
				throw new InvalidDataException("LogPath is required when file logging is enabled.");
			}
			// Probe actual file creation rather than assuming an existing directory is writable.
			var directory = Directory.CreateDirectory(LogPath);
			var path = Path.Combine(directory.FullName, Path.GetRandomFileName());
			using var probe = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 1, FileOptions.DeleteOnClose);
			probe.WriteByte(0);
		}
	}
}
