using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>
	/// The "full-boot tier" from the test-harness plan: spawns real AuxiliaryServer.dll and
	/// GameCore.dll child processes (same binaries build.ps1 runs), against the real repo
	/// working directory (steamengine.ini, uo-files/, saves/) - NOT the fast-tier
	/// EngineTestFixture's isolated temp dir, which is irrelevant here since these are separate
	/// OS processes with their own independent static state.
	///
	/// Mirrors build.ps1's "-Run Both" logic: AuxiliaryServer gets its own dedicated stdin pipe
	/// held open for the the whole fixture lifetime (its Main() blocks on Console.ReadLine() in
	/// a background thread and shuts the process down the instant that returns - see build.ps1
	/// for the full explanation), since without an explicit redirect it would inherit whatever
	/// this test process's own stdin happens to be.
	/// </summary>
	public sealed class GameServerPairFixture : IDisposable {
		public const int LoginPort = 2593;
		public const int GamePort = 2595;

		private readonly Process auxProcess;
		private readonly Process gameProcess;
		private FileStream portLock;
		private readonly StringBuilder gameOutput = new StringBuilder();
		private readonly object gameOutputLock = new object();

		public string RepoRoot { get; }

		/// <summary>Must stay the ONLY public constructor - xunit's ICollectionFixture DI
		/// requires exactly one with zero (or DI-resolvable) parameters, used by every other
		/// test file's parameterless `new GameServerPairFixture()` via that mechanism. Both a
		/// second public constructor overload AND a default-valued `bool` parameter on this one
		/// broke ALL of them, caught the hard way when adding this (xunit's activation tries to
		/// DI-resolve every parameter, including ones with a C# default value, and fails since
		/// `bool` isn't a fixture type). See BootPreservingSaves() below for the non-DI path.</summary>
		public GameServerPairFixture() : this(false) {
		}

		/// <summary>Skips the snapshot-wiping below entirely, so this boot loads whatever a
		/// PRIOR fixture instance (in the same test, already Dispose()d) left behind in saves/ -
		/// needed to test a real save -> restart -> reload round trip (#434), which every other
		/// fixture usage deliberately avoids (see the wipe's own reasoning just below). Only for
		/// a SECOND fixture instance, manually constructed (not via ICollectionFixture DI) in
		/// the same test, after a first default-constructed one has already produced the save
		/// being reloaded.</summary>
		public static GameServerPairFixture BootPreservingSaves() {
			return new GameServerPairFixture(true);
		}

		private GameServerPairFixture(bool preserveSaves) {
			// Tests/bin/Debug/net8.0 -> up 4 levels -> the moriaengine/moriaengine repo root,
			// regardless of whatever Environment.CurrentDirectory the fast-tier fixture (if it
			// ran in this same process) already changed.
			this.RepoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

			// A process killed mid-run (e.g. a hung/duplicate `dotnet test` invocation, or this
			// fixture's own previous collection being torn down mid-save) can leave behind a
			// save snapshot under saves/ whose only account isn't "protocoltest" - GameCore
			// auto-loads the most recent snapshot on boot, and the account-auto-creation login
			// flow (TestEnableAutoAccountCreation) only actually creates "protocoltest" when it
			// would be the very first account ever, not on any missing-account login attempt.
			// A stale leftover account from an unrelated test run silently breaks every test in
			// THIS run with LoginDeniedReason=0 (NoAccount) cascades that have nothing to do with
			// whatever's actually being tested. Delete every snapshot EXCEPT the git-tracked
			// "2000-01-01/00_00_00" bootstrap skeleton (saves/2000-01-01/00_00_00/*.sav) before
			// every fixture boot, so each test collection's GameCore process always starts from
			// that known-clean world - NOT by wiping saves/ outright, which removes the
			// bootstrap skeleton itself and makes WorldSaver.Load() fail after 3 attempts with
			// nothing at all to load (confirmed the hard way: don't repeat this).
			string savesDir = Path.Combine(this.RepoRoot, "saves");
			if (!preserveSaves && Directory.Exists(savesDir)) {
				foreach (string snapshotDateDir in Directory.GetDirectories(savesDir)) {
					if (Path.GetFileName(snapshotDateDir) == "2000-01-01") {
						foreach (string snapshotTimeDir in Directory.GetDirectories(snapshotDateDir)) {
							if (Path.GetFileName(snapshotTimeDir) != "00_00_00") {
								Directory.Delete(snapshotTimeDir, true);
							}
						}
					} else {
						Directory.Delete(snapshotDateDir, true);
					}
				}
			}

			//#785 - one server pair at a time, across processes too: every pair binds the same fixed
			//ports and shares saves/, so a second concurrent run (another dotnet test, or a run that
			//was "stopped" but is still alive) used to make every class of both runs fail with
			//"Game server never appeared"/LoginDenied noise. The OS drops the lock with the process.
			this.portLock = AcquirePortLock();

			string auxDll = Path.Combine(this.RepoRoot, "AuxiliaryServer", "bin", "Debug", "net8.0", "SteamEngine.AuxiliaryServer.dll");
			string gameDll = Path.Combine(this.RepoRoot, "GameCore", "bin", "Debug", "net8.0", "SteamEngine.GameCore.dll");

			if (!File.Exists(auxDll)) {
				this.ReleasePortLock();
				throw new FileNotFoundException("AuxiliaryServer not built - run build.ps1 first.", auxDll);
			}
			if (!File.Exists(gameDll)) {
				this.ReleasePortLock();
				throw new FileNotFoundException("GameCore not built - run build.ps1 first.", gameDll);
			}
			DateTime bootStarted = DateTime.UtcNow;
			ProgressLog.Write("BOOT: starting AuxiliaryServer + GameCore");

			// Real bug found and fixed here: if anything from this point on throws (most
			// commonly the boot-timeout a few lines down, when GameCore's own nested script
			// compile stalls under sandbox CPU contention), the constructor never returns an
			// object for xUnit to Dispose() - so without this try/catch, the already-spawned
			// auxProcess/gameProcess were silently abandoned, still running, still holding
			// the fixed ports. Observed directly: a single run leaked 9 separate orphaned
			// pairs this way over its lifetime, each one making every SUBSEQUENT collection's
			// own compile step slower (more sandbox CPU contention from the growing pile of
			// already-stuck processes), cascading one transient hiccup into a pathological
			// whole-run spiral instead of one isolated failure.
			try {
				this.auxProcess = StartHeldOpenStdin("dotnet", auxDll, this.RepoRoot);
				// Drain aux's stdout/stderr unconditionally - redirecting a stream and never
				// reading it risks the OS pipe buffer filling and blocking the child process.
				this.auxProcess.OutputDataReceived += (s, e) => { };
				this.auxProcess.ErrorDataReceived += (s, e) => { };
				this.auxProcess.BeginOutputReadLine();
				this.auxProcess.BeginErrorReadLine();
				System.Threading.Thread.Sleep(2000); // let it bind its ports/pipe before GameCore tries to connect

				this.gameProcess = StartHeldOpenStdin("dotnet", gameDll, this.RepoRoot);
				this.gameProcess.OutputDataReceived += (s, e) => {
					if (e.Data != null) {
						lock (this.gameOutputLock) {
							this.gameOutput.AppendLine(e.Data);
						}
					}
				};
				//#785 - stderr too: a crashing GameCore (e.g. exit 134, an abort) writes its fatal error
				//and stack trace there, and that was being thrown away
				this.gameProcess.ErrorDataReceived += (s, e) => {
					if (e.Data != null) {
						lock (this.gameOutputLock) {
							this.gameOutput.AppendLine("[stderr] " + e.Data);
						}
					}
				};
				this.gameProcess.BeginOutputReadLine();
				this.gameProcess.BeginErrorReadLine();

				if (!this.WaitForGameOutput("Init done.", TimeSpan.FromSeconds(60))) {
					throw new TimeoutException(this.gameProcess.HasExited ?
						"GameCore exited during boot (code " + this.gameProcess.ExitCode + "). Output:\n" + this.gameOutput :
						"GameCore did not reach 'Init done.' in 60s. Output so far:\n" + this.gameOutput);
				}
			} catch (Exception e) {
				ProgressLog.Write("BOOT FAILED after " + (int) (DateTime.UtcNow - bootStarted).TotalSeconds + "s: " + FirstLine(e.Message));
				TryKill(this.gameProcess);
				TryKill(this.auxProcess);
				this.ReleasePortLock();
				throw;
			}
			ProgressLog.Write("READY: servers up in " + (int) (DateTime.UtcNow - bootStarted).TotalSeconds + "s");
			ServerHealth.Reset(this);
		}

		public bool GameProcessHasExited {
			get { return this.gameProcess.HasExited; }
		}

		public int GameProcessExitCode {
			get { return this.gameProcess.HasExited ? this.gameProcess.ExitCode : 0; }
		}

		private static FileStream AcquirePortLock() {
			//machine-wide (temp dir), not per checkout: the ports are shared by every worktree
			string path = Path.Combine(Path.GetTempPath(), "moria-protocol-servers.lock");
			DateTime deadline = DateTime.UtcNow + TimeSpan.FromSeconds(15); //the previous collection's pair may still be shutting down
			while (true) {
				try {
					return new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
				} catch (IOException) {
					if (DateTime.UtcNow >= deadline) {
						ProgressLog.Write("BOOT FAILED: another protocol test run holds the server ports");
						throw new InvalidOperationException("Another protocol test run is using the server ports (" + path +
							" is locked). Stop it first - scripts/test-full.sh --kill-stale kills leftover runs.");
					}
					System.Threading.Thread.Sleep(500);
				}
			}
		}

		private void ReleasePortLock() {
			if (this.portLock != null) {
				this.portLock.Dispose();
				this.portLock = null;
			}
		}

		private static string FirstLine(string text) {
			int newline = text.IndexOf('\n');
			return (newline < 0) ? text : text.Substring(0, newline);
		}

		/// <summary>#929 - a CRITICAL in the server fails no test, and one went unseen for as long as a
		/// party could form. The run says so, once per distinct message.</summary>
		private void ReportCriticals() {
			HashSet<string> seen = new HashSet<string>();
			foreach (string line in this.GameOutputSnapshot().Split('\n')) {
				int at = line.IndexOf("CRITICAL:", StringComparison.Ordinal);
				if ((at >= 0) && seen.Add(line.Substring(at))) {
					ProgressLog.Write(line.Substring(at, Math.Min(line.Length - at, 180)).TrimEnd());
				}
			}
		}

		public string GameOutputSnapshot() {
			lock (this.gameOutputLock) {
				return this.gameOutput.ToString();
			}
		}

		public bool WaitForGameOutput(string marker, TimeSpan timeout) {
			DateTime deadline = DateTime.UtcNow + timeout;
			while (DateTime.UtcNow < deadline) {
				lock (this.gameOutputLock) {
					if (this.gameOutput.ToString().Contains(marker)) {
						return true;
					}
				}
				if (this.gameProcess.HasExited) {
					return false;
				}
				System.Threading.Thread.Sleep(100);
			}
			return false;
		}

		/// <summary>
		/// Same as <see cref="WaitForGameOutput"/>, but only considers text appended at or after
		/// <paramref name="sincePosition"/> (a length previously captured via
		/// <see cref="GameOutputSnapshot"/>) - needed for any marker/log line that more than one
		/// test method in the same fixture can legitimately print (e.g. a generic "GOLD_CHECK:"
		/// debug line used by several scenarios). Plain WaitForGameOutput would otherwise report
		/// success instantly off an EARLIER test's leftover occurrence, before this call's own
		/// triggering action has even run - a real bug this caught: a later test's "withdraw"
		/// read a stale GOLD_CHECK line from the test before it and silently passed on the wrong
		/// numbers.
		/// </summary>
		public bool WaitForGameOutputSince(string marker, int sincePosition, TimeSpan timeout) {
			DateTime deadline = DateTime.UtcNow + timeout;
			while (DateTime.UtcNow < deadline) {
				lock (this.gameOutputLock) {
					int searchFrom = Math.Max(0, sincePosition - marker.Length);
					if (this.gameOutput.ToString(searchFrom, this.gameOutput.Length - searchFrom).Contains(marker)) {
						return true;
					}
				}
				if (this.gameProcess.HasExited) {
					return false;
				}
				System.Threading.Thread.Sleep(100);
			}
			return false;
		}

		private static Process StartHeldOpenStdin(string fileName, string arguments, string workingDirectory) {
			var psi = new ProcessStartInfo(fileName, arguments) {
				WorkingDirectory = workingDirectory,
				RedirectStandardInput = true,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				UseShellExecute = false,
			};
			// GameCore's own script-compile step shells out to a nested "dotnet build"
			// (MsBuildLauncher.cs). A long test session accumulates many idle MSBuild/
			// VBCSCompiler node-reuse server processes; inherited by this child process, this
			// stops its own nested builds from trying to hand off to one of those (observed
			// hanging indefinitely at "Compiling scripts..." otherwise) instead of always
			// spawning a fresh node.
			psi.Environment["MSBUILDDISABLENODEREUSE"] = "1";
			//#785 - the server exits on its own if this test process dies (ParentProcessWatchdog),
			//instead of living on as an orphan that holds the ports
			psi.Environment[SteamEngine.Common.TestHarness.CommandsVariable] = "1"; //#951 - Test* commands for this server only
			psi.Environment[SteamEngine.Common.ParentProcessWatchdog.EnvironmentVariable] = Environment.ProcessId.ToString(System.Globalization.CultureInfo.InvariantCulture);
			var proc = Process.Start(psi);
			// Deliberately never write to or close StandardInput - see class comment.
			return proc;
		}

		public void Dispose() {
			TryKill(this.gameProcess);
			TryKill(this.auxProcess);
			this.ReleasePortLock();
			ReportCriticals();
			ProgressLog.Write("STOPPED: servers shut down");
		}

		private static void TryKill(Process proc) {
			try {
				if (proc != null && !proc.HasExited) {
					proc.Kill(true); //#785 - the whole tree, including GameCore's nested script-compile builds
					proc.WaitForExit(5000);
				}
			} catch (InvalidOperationException) {
			}
		}
	}

	[CollectionDefinition(Name, DisableParallelization = true)]
	public class GameServerPairCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "GameServerPair";
	}
}
