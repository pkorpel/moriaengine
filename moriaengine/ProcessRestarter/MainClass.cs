using System;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using System.Threading;

namespace SteamEngine.ProcessRestarter {
	public static class MainClass {
		static ManualResetEvent setForExit = new ManualResetEvent(false);

		static void Main(string[] args) {
			//name the console window for better recognizability
			Console.Title = "SE AuxRestarter - " + System.Reflection.Assembly.GetExecutingAssembly().Location;

			//Tools.ExitBinDirectory(); //we don't want any dependency, not even with Common
			ExitBinDirectory();

			if (args.Length != 2) {
				DisplayHelp();
				return;
			}

			string waitForPath = Path.GetFullPath(args[0]);
			if (!CheckFile(waitForPath)) {
				return;
			}

			string executePath = Path.GetFullPath(args[1]);
			if (!CheckFile(executePath)) {
				return;
			}

			Process runningProcess = FindRunningProcess(waitForPath);
			if (runningProcess == null) {
				Console.WriteLine("Bìžící proces '" + waitForPath + "' nenalezen.");
				RunProcess(executePath);
			} else {
				Console.WriteLine("Bìžící proces '" + waitForPath + "' nalezen. Èekám na ukonèení...");
				runningProcess.EnableRaisingEvents = true;
				runningProcess.Exited += new EventHandler(delegate(object ignored, EventArgs ignored2) {
					RunProcess(executePath);
				});

				setForExit.WaitOne();
			}
		}

		private static void RunProcess(string path) {
			Console.WriteLine("Pouštím 'clean.bat'.");
			Process.Start("clean.bat").WaitForExit();

			Console.WriteLine("Pouštím '" + path + "'.");
			Process.Start(path);
			setForExit.Set();
		}

		private static Process FindRunningProcess(string waitForPath) {
			string processName = Path.GetFileNameWithoutExtension(waitForPath);
			Process[] processes = Process.GetProcessesByName(processName);

			Process correctProcess = null;
			foreach (Process p in processes) {
				string pPath = Path.GetFullPath(p.MainModule.FileName);
				if (0 == string.Compare(waitForPath, pPath, StringComparison.OrdinalIgnoreCase)) {
					correctProcess = p;
					break;
				}
			}
			return correctProcess;
		}

		private static bool CheckFile(string waitForPath) {
			if (!File.Exists(waitForPath)) {
				Console.WriteLine("Soubor '" + waitForPath + "' neexistuje.");
				DisplayHelp();
				return false;
			}
			return true;
		}

		private static void DisplayHelp() {
			Console.WriteLine("Tento program oèekává 2 parametry: první je cesta k souboru procesu na který se èeká až skonèí, druhý je cesta k souboru který se má pak pustit.");
		}

		private static void ExitBinDirectory() {
			string path = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
			if (path.ToLowerInvariant().EndsWith("bin")) {
				Directory.SetCurrentDirectory(Path.GetDirectoryName(path));
			} else {
				Directory.SetCurrentDirectory(path);
			}
		}
	}
}
