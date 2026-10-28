/*
	This program is free software; you can redistribute it and/or modify
	it under the terms of the GNU General Public License as published by
	the Free Software Foundation; either version 2 of the License, or
	(at your option) any later version.

	This program is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
	GNU General Public License for more details.

	You should have received a copy of the GNU General Public License
	along with this program; if not, write to the Free Software
	Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
	Or visit http://www.gnu.org/copyleft/gpl.html
*/

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace SteamEngine.Common {
	/// <summary>
	/// #785 - lets a test harness tie a server process's life to its own. When the
	/// STEAMENGINE_PARENT_PID environment variable is set (only the protocol-test fixture does
	/// this), a background thread checks every 2s whether that process still lives, and kills
	/// this one the moment it doesn't. Before, a killed test run left its GameCore/AuxiliaryServer
	/// pair running, holding the fixed ports and breaking every later run. No effect without the
	/// variable, i.e. on any normal server start.
	/// </summary>
	public static class ParentProcessWatchdog {
		public const string EnvironmentVariable = "STEAMENGINE_PARENT_PID";

		public static void StartFromEnvironment() {
			string value = Environment.GetEnvironmentVariable(EnvironmentVariable);
			int parentPid;
			if (string.IsNullOrEmpty(value) || !int.TryParse(value, out parentPid)) {
				return;
			}
			Thread thread = new Thread(() => {
				while (IsAlive(parentPid)) {
					Thread.Sleep(2000);
				}
				Console.WriteLine("ParentProcessWatchdog: parent process " + parentPid + " is gone - exiting.");
				//a hard kill rather than Environment.Exit, which would run the server's shutdown
				//handlers (saving the world, etc.) - nothing a test server's orphan should do.
				//(Kill(true) can't be used on the calling process's own tree.)
				using (Process self = Process.GetCurrentProcess()) {
					self.Kill();
				}
			});
			thread.IsBackground = true;
			thread.Name = "ParentProcessWatchdog";
			thread.Start();
		}

		/// <summary>False once the process has exited - including a Linux zombie (exited, not yet
		/// reaped), which Process.GetProcessById still finds.</summary>
		public static bool IsAlive(int pid) {
			try {
				string stat = "/proc/" + pid + "/stat";
				if (File.Exists(stat)) {
					string text = File.ReadAllText(stat);
					int close = text.LastIndexOf(')');
					return !((close >= 0) && (close + 2 < text.Length) && (text[close + 2] == 'Z'));
				}
				using (Process process = Process.GetProcessById(pid)) {
					return !process.HasExited;
				}
			} catch (ArgumentException) {
				return false; //no such process
			} catch (InvalidOperationException) {
				return false;
			} catch (IOException) {
				return false; //it went away while we read /proc
			}
		}
	}
}
