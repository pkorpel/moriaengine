using System;
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
		private readonly StringBuilder gameOutput = new StringBuilder();
		private readonly object gameOutputLock = new object();

		public string RepoRoot { get; }

		public GameServerPairFixture() {
			// Tests/bin/Debug/net8.0 -> up 4 levels -> the moriaengine/moriaengine repo root,
			// regardless of whatever Environment.CurrentDirectory the fast-tier fixture (if it
			// ran in this same process) already changed.
			this.RepoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

			string auxDll = Path.Combine(this.RepoRoot, "AuxiliaryServer", "bin", "Debug", "net8.0", "SteamEngine.AuxiliaryServer.dll");
			string gameDll = Path.Combine(this.RepoRoot, "GameCore", "bin", "Debug", "net8.0", "SteamEngine.GameCore.dll");

			if (!File.Exists(auxDll)) {
				throw new FileNotFoundException("AuxiliaryServer not built - run build.ps1 first.", auxDll);
			}
			if (!File.Exists(gameDll)) {
				throw new FileNotFoundException("GameCore not built - run build.ps1 first.", gameDll);
			}

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
			this.gameProcess.ErrorDataReceived += (s, e) => { };
			this.gameProcess.BeginOutputReadLine();
			this.gameProcess.BeginErrorReadLine();

			if (!this.WaitForGameOutput("Init done.", TimeSpan.FromSeconds(60))) {
				throw new TimeoutException("GameCore did not reach 'Init done.' in time. Output so far:\n" + this.gameOutput);
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
			var proc = Process.Start(psi);
			// Deliberately never write to or close StandardInput - see class comment.
			return proc;
		}

		public void Dispose() {
			TryKill(this.gameProcess);
			TryKill(this.auxProcess);
		}

		private static void TryKill(Process proc) {
			try {
				if (proc != null && !proc.HasExited) {
					proc.Kill();
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
