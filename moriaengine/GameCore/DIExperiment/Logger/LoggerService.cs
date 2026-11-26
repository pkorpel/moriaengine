using System;
using System.Globalization;
using System.IO;
using SteamEngine.Common;

namespace SteamEngine.DIExperiment {
	internal sealed class LoggerService(LoggerConfiguration settings, ILogger output = null) : ICoreService, ILogger {
		private readonly object gate = new();
		private bool initialized;
		private bool disposed;
		private TextWriter logFile;

		// Phase zero: the orchestrator constructs this with LoggerConfiguration.Emergency before any config is read,
		// then Configure() swaps in the validated [files] settings (if there are usable ones) and Init() applies them.
		// Emergency = console (or the test sink) only, no log file, so it can't fail on a missing or unwritable directory.
		// The optional output is a test recording sink standing in for the console, not a replacement for this service.
		internal LoggerConfiguration Settings { get; private set; } = settings ?? throw new ArgumentNullException(nameof(settings));

		/// <summary>Replaces emergency settings before initialization; a running logger cannot be reconfigured.</summary>
		internal void Configure(LoggerConfiguration configuration) {
			lock (gate) {
				ObjectDisposedException.ThrowIf(disposed, this);
				if (initialized) throw new InvalidOperationException("Logger has already been initialized.");
				Settings = configuration ?? throw new ArgumentNullException(nameof(configuration));
			}
		}

		internal string FilePath { get; private set; }

		/// <summary>
		/// Creates an initialized, file-less facade for legacy static code such as MultiData.Init().
		/// It forwards to the static logger and owns no resources.
		/// </summary>
		internal static LoggerService CreateLegacyFacade() {
			var facade = new LoggerService(LoggerConfiguration.Emergency);
			facade.Init();
			return facade;
		}

		/// <summary>Applies the settings once and opens a dedicated log file only when file logging is enabled.</summary>
		public void Init() {
			lock (gate) {
				ObjectDisposedException.ThrowIf(disposed, this);
				if (initialized) throw new InvalidOperationException("Logger has already been initialized.");
				if (Settings.LogToFiles) {
					// The experimental filename cannot collide with the legacy CoreLogger in the same process.
					string directory = Directory.CreateDirectory(Settings.LogPath).FullName;
					FilePath = Path.Combine(directory, string.Format(CultureInfo.InvariantCulture,
						"SteamEngine.DIExperiment.{0:yyyy-MM-dd}.log", DateTime.Today));
					logFile = File.AppendText(FilePath);
				}
				initialized = true;
			}
		}

		private void EnsureReady() {
			lock (gate) {
				ObjectDisposedException.ThrowIf(disposed, this);
				if (!initialized) throw new InvalidOperationException("Logger has not been initialized.");
			}
		}

		/// <summary>Closes the owned file without disposing the output sink or the static logger.</summary>
		public void Dispose() {
			lock (gate) {
				disposed = true;
				logFile?.Dispose();
				logFile = null;
			}
		}

		/// <summary>Creates an indentation scope on the output sink or the legacy logger.</summary>
		public IDisposable Indent() {
			EnsureReady();
			if (output != null) return output.Indent();
			return LoggerIndentation.Push();
		}

		/// <summary>Writes an entry to the owned file and forwards it to the sink or the legacy logger.</summary>
		public void Emit(LogKind kind, object message, string file = null, int line = 0) {
			EnsureReady();
			WriteToFile(kind, message, file, line);
			// A test sink replaces console delivery, not the service's file or lifecycle.
			if (output != null) {
				output.Emit(kind, message, file, line);
				return;
			}
			// Keep the legacy logger's formatting and file/line handling for each kind.
			switch (kind) {
				case LogKind.Line: Logger.StaticWriteLine(message); break;
				case LogKind.Fragment: Logger.Write(message as LogStr ?? LogStr.Raw(message)); break;
				case LogKind.FileOnly: Logger.Log(message?.ToString()); break;
				case LogKind.Debug: Logger.WriteDebug(message); break;
				case LogKind.Info: Logger.WriteInfo(true, message); break;
				case LogKind.Warning:
					if (file == null) Logger.WriteWarning(message); else Logger.WriteWarning(file, line, message);
					break;
				case LogKind.Error:
					if (file == null) Logger.WriteError(message); else Logger.WriteError(file, line, message);
					break;
				case LogKind.Critical:
					if (file == null) Logger.WriteCritical(message); else Logger.WriteCritical(file, line, message);
					break;
				case LogKind.Fatal:
					if (file == null) Logger.WriteFatal(message); else Logger.WriteFatal(file, line, message);
					break;
				default: throw new ArgumentOutOfRangeException(nameof(kind));
			}
		}

		private void WriteToFile(LogKind kind, object message, string sourceFile, int line) {
			// Serialize file writes with disposal and flush each entry so failures are visible immediately.
			lock (gate) {
				if (logFile == null) return;
				string location = sourceFile == null ? "" : $"({sourceFile}, {line}) ";
				string prefix = kind is LogKind.Line or LogKind.Fragment or LogKind.FileOnly ? "" : kind.ToString().ToUpperInvariant() + ": ";
				logFile.WriteLine(string.Concat(DateTime.Now.ToString("HH:mm:ss", CultureInfo.InvariantCulture), ": ",
					Logger.indentation, prefix, location, message));
				logFile.Flush();
			}
		}
	}
}
