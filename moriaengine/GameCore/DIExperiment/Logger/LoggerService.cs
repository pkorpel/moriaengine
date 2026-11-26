using System;
using SteamEngine.Common;

namespace SteamEngine.DIExperiment {
	internal sealed class LoggerService(LoggerConfiguration settings) : ICoreService, ILogger {
		private readonly object gate = new();
		private bool initialized;
		private bool disposed;

		// Validated before construction; a future Init could open the log files it names.
		internal LoggerConfiguration Settings { get; } = settings ?? throw new ArgumentNullException(nameof(settings));

		public void Init() {
			lock (gate) {
				ObjectDisposedException.ThrowIf(disposed, this);
				initialized = true;
			}
		}

		private void EnsureReady() {
			lock (gate) {
				ObjectDisposedException.ThrowIf(disposed, this);
				if (!initialized) throw new InvalidOperationException("Logger has not been initialized.");
			}
		}

		public void Dispose() {
			lock (gate) {
				disposed = true;
			}
		}

		public void Emit(LogKind kind, object message, string file = null, int line = 0) {
			EnsureReady();
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
	}
}
