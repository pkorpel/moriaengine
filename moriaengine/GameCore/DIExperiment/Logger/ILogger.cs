using System.Diagnostics;
using SteamEngine.Common;

namespace SteamEngine.DIExperiment {
	internal enum LogKind { Line, Fragment, FileOnly, Debug, Info, Warning, Error, Critical, Fatal }

	internal interface ILogger {
		void Emit(LogKind kind, object message, string file = null, int line = 0);

		void WriteLine(object message) => Emit(LogKind.Line, message);
		void Write(LogStr message) => Emit(LogKind.Fragment, message);
		void SetTitle(string title) => Write(LogStr.Title(title));
		void Log(string message) => Emit(LogKind.FileOnly, message);
		void WriteWarning(object message, string file = null, int line = 0) => Emit(LogKind.Warning, message, file, line);
		void WriteError(object message, string file = null, int line = 0) => Emit(LogKind.Error, message, file, line);
		void WriteCritical(object message, string file = null, int line = 0) => Emit(LogKind.Critical, message, file, line);
		void WriteFatal(object message, string file = null, int line = 0) => Emit(LogKind.Fatal, message, file, line);
	}

	internal static class LoggerExtensions {
		[Conditional("DEBUG")]
		internal static void WriteDebug(this ILogger logger, object message) => logger.Emit(LogKind.Debug, message);

		[Conditional("TRACE")]
		internal static void WriteInfo(this ILogger logger, bool enabled, object message) {
			if (enabled) logger.Emit(LogKind.Info, message);
		}
	}
}
