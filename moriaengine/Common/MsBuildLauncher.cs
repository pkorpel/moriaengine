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
using System.Text;

namespace SteamEngine.Common {

	/// <summary>Shells out to the "dotnet" CLI to build a project, and returns the path to its output assembly.</summary>
	/// <remarks>
	/// This used to host the MSBuild object model in-process (Microsoft.Build.Execution.BuildManager) against the
	/// old monolithic SteamEngine.sln, using a sanitized "SteamEngine_X" pseudo-target-name per project. That API
	/// is Framework-only and fragile to version-match against whatever MSBuild happens to be installed. Shelling
	/// out to "dotnet build" against the project file directly is simpler, works identically cross-platform, and
	/// "-getProperty:TargetPath" lets us read back the resolved output path without needing to know or hardcode
	/// each project's AssemblyName pattern (e.g. the scripts/Generated projects derive theirs from
	/// $(ScriptsAssemblyNumber), which varies per compile).
	/// </remarks>
	public static class MsBuildLauncher {
		public static string Compile(string seRootPath, BuildType build, string projectRelativePath, int? scriptAssemblyNumber = null) {
			var projectPath = Path.GetFullPath(Path.Combine(seRootPath, projectRelativePath));

			var args = new StringBuilder();
			args.Append("build \"").Append(projectPath).Append('"');
			args.Append(" -c ").Append(build);
			args.Append(" --nologo -v quiet");
			if (scriptAssemblyNumber.HasValue) {
				args.Append(" -p:ScriptsAssemblyNumber=").Append(scriptAssemblyNumber.Value);
			}
			args.Append(" -getProperty:TargetPath");

			var psi = new ProcessStartInfo("dotnet", args.ToString()) {
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				UseShellExecute = false,
			};

			using (var process = Process.Start(psi)) {
				string stdout = process.StandardOutput.ReadToEnd();
				string stderr = process.StandardError.ReadToEnd();
				process.WaitForExit();

				if (process.ExitCode != 0) {
					throw new Exception($"Compilation of '{projectPath}' failed:{Environment.NewLine}{stdout}{stderr}");
				}

				string compiledFilePath = stdout.Trim();
				if (string.IsNullOrEmpty(compiledFilePath) || !File.Exists(compiledFilePath)) {
					throw new Exception($"The compiled file '{compiledFilePath}' doesn't exist.");
				}
				return compiledFilePath;
			}
		}
	}
}
