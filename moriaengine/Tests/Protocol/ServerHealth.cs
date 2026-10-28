using System;
using System.IO;

namespace SteamEngine.Tests.Protocol {
	/// <summary>
	/// #785 - thrown instead of letting a test keep retrying against a server pair that's dead or
	/// stopped responding. Deliberately NOT an InvalidOperationException/TimeoutException: every
	/// ConnectLoginAndCreateCharacter/WaitForServerListing retry loop in the suite catches those
	/// and tries again (up to 40 times, 15s read timeout each), which is how a broken server made
	/// every test in a class burn ~2 minutes before failing - 20+ minutes of silent waiting per class.
	/// </summary>
	public sealed class ServerUnresponsiveException : Exception {
		public ServerUnresponsiveException(string message)
			: base(message) {
		}
	}

	/// <summary>
	/// #785 - tracks whether the current fixture's server pair still answers logins. Every new
	/// test client checks in (BeforeConnect); a completed login reports back (MarkHealthy). Once
	/// GameCore has exited, or logins keep failing for UnresponsiveAfter across at least
	/// MinFailedAttempts attempts, the pair is declared dead for the rest of the fixture's life:
	/// every later client throws ServerUnresponsiveException right away, so the remaining tests
	/// of the class fail in seconds with the reason, instead of minutes each.
	/// </summary>
	public static class ServerHealth {
		public static readonly TimeSpan UnresponsiveAfter = TimeSpan.FromSeconds(30);
		public const int MinFailedAttempts = 3;

		private static readonly object sync = new object();
		private static GameServerPairFixture current;
		private static int attemptsSinceSuccess;
		private static DateTime firstAttemptSinceSuccess;
		private static string deadReason;

		/// <summary>A freshly booted fixture - forget everything about the previous one.</summary>
		internal static void Reset(GameServerPairFixture fixture) {
			lock (sync) {
				current = fixture;
				attemptsSinceSuccess = 0;
				deadReason = null;
			}
		}

		internal static void MarkHealthy() {
			lock (sync) {
				attemptsSinceSuccess = 0;
			}
		}

		/// <summary>Called by every TestLoginClient/TestGameClient before it connects.</summary>
		internal static void BeforeConnect() {
			lock (sync) {
				if (deadReason == null) {
					if ((current != null) && current.GameProcessHasExited) {
						deadReason = "GameCore exited (code " + current.GameProcessExitCode + ") - see GameOutputSnapshot() for why";
					} else {
						DateTime now = DateTime.UtcNow;
						if (attemptsSinceSuccess == 0) {
							firstAttemptSinceSuccess = now;
						}
						attemptsSinceSuccess++;
						TimeSpan failingFor = now - firstAttemptSinceSuccess;
						if ((attemptsSinceSuccess > MinFailedAttempts) && (failingFor >= UnresponsiveAfter)) {
							deadReason = "no successful login for " + (int) failingFor.TotalSeconds + "s across " +
								(attemptsSinceSuccess - 1) + " attempts - the server pair stopped responding";
						}
					}
					if (deadReason != null) {
						ProgressLog.Write("UNRESPONSIVE: " + deadReason + " - failing the remaining tests of this class fast");
					}
				}
				if (deadReason != null) {
					throw new ServerUnresponsiveException("Server pair is dead: " + deadReason + ".");
				}
			}
		}
	}

	/// <summary>
	/// #785 - one-line progress events (boot started/ready/failed, server unresponsive, shutdown)
	/// appended to the file named by STEAMENGINE_TEST_PROGRESS_FILE, which tools/protocol-test.sh
	/// sets and echoes live. Nothing is written when the variable isn't set.
	/// </summary>
	public static class ProgressLog {
		public const string EnvironmentVariable = "STEAMENGINE_TEST_PROGRESS_FILE";

		public static void Write(string message) {
			string path = Environment.GetEnvironmentVariable(EnvironmentVariable);
			if (string.IsNullOrEmpty(path)) {
				return;
			}
			try {
				File.AppendAllText(path, DateTime.Now.ToString("HH:mm:ss") + " " + message + Environment.NewLine);
			} catch (IOException) {
			}
		}
	}
}
