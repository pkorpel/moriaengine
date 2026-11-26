using System;
using System.Collections.Generic;
using SteamEngine.DIExperiment;

namespace SteamEngine.Tests.Fixtures {
	internal sealed record RecordedLog(LogKind Kind, object Message, string Indentation, string File, int Line) {
		internal string Display => Indentation + (Message is Exception exception
			? exception.ToString()
			: Message?.ToString() ?? "(null)");
	}

	internal sealed class RecordingLogger : ILogger {
		private readonly object gate = new();
		private readonly List<RecordedLog> entries = new();

		internal IReadOnlyList<RecordedLog> Entries {
			get {
				lock (gate) {
					return entries.ToArray();
				}
			}
		}

		/// <summary>Records the entry and current indentation together for later assertions and display.</summary>
		public void Emit(LogKind kind, object message, string file = null, int line = 0) {
			lock (gate) {
				entries.Add(new RecordedLog(kind, message, SteamEngine.Common.Logger.indentation, file, line));
			}
		}

		public IDisposable Indent() => LoggerIndentation.Push();
	}
}
