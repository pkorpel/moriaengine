using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using SharpConfig;
using SteamEngine.Common;

namespace SteamEngine.DIExperiment {
	// File and value errors are SEExceptions. SharpConfig owns parsing, object mapping, and file formatting.
	internal sealed class SharpConfigStore : IConfigStore {
		private readonly string path;
		private string fullPath;
		private Configuration configuration;
		private bool fileExists;
		private bool changed;

		internal SharpConfigStore(string path) {
			this.path = path;
		}

		/// <summary>Loads the INI, or stages an empty configuration and returns false for a first-run stop.</summary>
		public bool Load() {
			if (string.IsNullOrWhiteSpace(path)) {
				throw new SEException("No configuration file path given.");
			}
			configuration = null;
			try {
				fullPath = Path.GetFullPath(path);
				fileExists = File.Exists(fullPath);
				changed = !fileExists;
				if (fileExists) {
					configuration = Configuration.LoadFromFile(fullPath);
					return true;
				}

				configuration = new Configuration();
				return false;
			} catch (ParserException ex) {
				throw new SEException(fullPath, ex.Line, ex);
			} catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException
				or NotSupportedException) {
				throw FileError(fullPath ?? path, "cannot load the configuration file", ex);
			}
		}

		/// <summary>Maps existing values onto defaults and stages missing settings for a later save.</summary>
		public object Read(Type type) {
			ArgumentNullException.ThrowIfNull(type);
			var configuration = Loaded();
			var value = Activator.CreateInstance(type);
			var defaults = Describe(type, value);

			// SharpConfig leaves absent keys at their property defaults and throws for invalid existing values.
			try {
				if (configuration.Contains(defaults.Name)) {
					configuration[defaults.Name].SetValuesTo(value);
				}
			} catch (Exception ex) when (ex is SettingValueCastException or InvalidOperationException
				or ArgumentException or TargetInvocationException) {
				throw FileError(fullPath, $"invalid settings in [{defaults.Name}] for {type.Name}", ex);
			}
			if (defaults.All(setting => configuration.Contains(defaults.Name, setting.Name))) {
				return value;
			}

			// Add missing keys without publishing them to disk before all settings validate.
			var section = configuration[defaults.Name];
			foreach (var setting in defaults) {
				if (!section.Contains(setting.Name)) {
					section.Add(setting);
				} else if (string.IsNullOrWhiteSpace(section[setting.Name].PreComment)) {
					section[setting.Name].PreComment = setting.PreComment;
				}
			}
			changed = true;
			return value;
		}

		/// <summary>Atomically publishes staged settings; a failed write can be retried.</summary>
		public void Save() {
			var configuration = Loaded();
			if (!changed) return;

			// Write beside the destination so the final move does not cross filesystems.
			string temp = fullPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
			bool tempCreated = false;
			try {
				var options = new FileStreamOptions {
					Mode = FileMode.CreateNew,
					Access = FileAccess.Write,
					Share = FileShare.None
				};
				if (fileExists && !OperatingSystem.IsWindows()) {
					options.UnixCreateMode = File.GetUnixFileMode(fullPath);
				}
				using (var stream = new FileStream(temp, options)) {
					tempCreated = true;
					if (options.UnixCreateMode is { } mode && !OperatingSystem.IsWindows()) {
						File.SetUnixFileMode(temp, mode);
					}
					configuration.SaveToStream(stream);
				}
				File.Move(temp, fullPath, overwrite: fileExists);
				tempCreated = false;
				fileExists = true;
				changed = false;
			} catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) {
				throw FileError(fullPath, "cannot write the configuration file", ex);
			} finally {
				if (tempCreated) {
					try {
						File.Delete(temp);
					} catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) {
						Logger.WriteError(ex);
					}
				}
			}
		}

		// Same shape as LogStr.FileLine, for errors where SharpConfig gives no line.
		private static SEException FileError(string file, string message, Exception inner) =>
			new(LogStr.Concat(LogStr.Raw("("), LogStr.File(file), LogStr.Raw(") "), LogStr.Raw(message)), inner);

		private Configuration Loaded() =>
			configuration ?? throw new InvalidOperationException(nameof(Load) + " must be called first.");

		private static Section Describe(Type type, object value) {
			var section = type.GetCustomAttribute<IniSectionAttribute>()?.Name;
			if (string.IsNullOrWhiteSpace(section)) {
				throw new SEException($"{type} must declare an INI section.");
			}
			// Let SharpConfig describe the values; our only extra metadata is the required documentation.
			var defaults = Section.FromObject(section, value);
			foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance)) {
				var description = property.GetCustomAttribute<DescriptionAttribute>()?.Description;
				if (string.IsNullOrWhiteSpace(description)) {
					throw new SEException($"{type}.{property.Name} must have a description.");
				}
				defaults[property.Name].PreComment = description;
			}
			return defaults;
		}
	}
}
