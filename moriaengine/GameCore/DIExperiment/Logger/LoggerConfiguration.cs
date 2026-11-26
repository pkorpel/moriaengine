using System.IO;

namespace SteamEngine.DIExperiment {
	internal sealed record LoggerConfiguration(string LogPath = "./logs/", bool LogToFiles = true) : ICoreSettings {
		public void Validate() {
			if (!LogToFiles) return;
			if (string.IsNullOrWhiteSpace(LogPath)) {
				throw new InvalidDataException("LogPath is required when file logging is enabled.");
			}
			var directory = Directory.CreateDirectory(LogPath);
			var path = Path.Combine(directory.FullName, Path.GetRandomFileName());
			using var probe = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 1, FileOptions.DeleteOnClose);
			probe.WriteByte(0);
		}
	}
}
