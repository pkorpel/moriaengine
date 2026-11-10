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
using System.Globalization;
using System.IO;
using System.Text;

namespace SteamEngine.Common {

	/// <summary>Shells out to the "dotnet" CLI to build a project, and returns the path to its output assembly.</summary>
	/// <remarks>
	/// This used to host the MSBuild object model in-process (Microsoft.Build.Execution.BuildManager) against the
	/// old monolithic SteamEngine.sln, using a sanitized "SteamEngine_X" pseudo-target-name per project. That API
	/// is Framework-only and fragile to version-match against whatever MSBuild happens to be installed. Shelling
	/// out to "dotnet build" against the project file directly is simpler and works identically cross-platform.
	///
	/// This needs two separate "dotnet build" invocations, not one: combining "build" with "-getProperty" in a
	/// single invocation only evaluates properties, it does not actually run the Build target (confirmed - the
	/// combined form exits 0 and happily reports a TargetPath whose file was never written). So: a real build
	/// first, then a second (cheap, already-up-to-date) invocation to read back the resolved output path without
	/// needing to know or hardcode each project's AssemblyName pattern (the scripts/Generated projects derive
	/// theirs from $(ScriptsAssemblyNumber), which varies per compile).
	/// </remarks>
	public static class MsBuildLauncher {
		/// <param name="inputsFingerprint">#1358: when given, and the project's stamp says its last build had these inputs
		/// and its output is still the file it wrote, that output is returned without running dotnet at all.</param>
		/// <param name="buildProjectReferences">false = -p:BuildProjectReferences=false, the referenced projects' outputs are taken as they are.</param>
		public static string Compile(string seRootPath, BuildType build, string projectRelativePath, int? scriptAssemblyNumber = null,
				string inputsFingerprint = null, bool buildProjectReferences = true) {
			var projectPath = Path.GetFullPath(Path.Combine(seRootPath, projectRelativePath));
			string stampPath = StampPath(projectPath, build, scriptAssemblyNumber);
			LastCompileReused = false;
			if (inputsFingerprint != null && TryReuse(stampPath, inputsFingerprint, out string reused)) {
				return reused;
			}
			// a build that fails or is cut short must not leave the previous build's stamp in place
			if (!TryDeleteStamp(stampPath)) {
				inputsFingerprint = null; // a stamp we can't remove we mustn't rewrite either
			}

			var commonArgs = new StringBuilder();
			commonArgs.Append('"').Append(projectPath).Append('"');
			commonArgs.Append(" -c ").Append(build);
			commonArgs.Append(" --nologo -v quiet");
			if (scriptAssemblyNumber.HasValue) {
				commonArgs.Append(" -p:ScriptsAssemblyNumber=").Append(scriptAssemblyNumber.Value);
			}
			if (!buildProjectReferences) {
				commonArgs.Append(" -p:BuildProjectReferences=false");
			}

			RunDotnet("build " + commonArgs, projectPath);
			string compiledFilePath = RunDotnet("build " + commonArgs + " -getProperty:TargetPath", projectPath).Trim();

			if (string.IsNullOrEmpty(compiledFilePath) || !File.Exists(compiledFilePath)) {
				throw new Exception($"The compiled file '{compiledFilePath}' doesn't exist.");
			}
			if (inputsFingerprint != null) {
				try {
					WriteStamp(stampPath, inputsFingerprint, compiledFilePath);
				} catch (Exception e) when (e is IOException || e is UnauthorizedAccessException) {
					Logger.WriteWarning("Couldn't write the build stamp " + stampPath + " (the next boot builds again): " + e.Message);
					TryDeleteStamp(stampPath);
				}
			}
			return compiledFilePath;
		}

		/// <summary>True if <see cref="Compile"/> returned its last output without building (#1358).</summary>
		public static bool LastCompileReused { get; private set; }

		internal static string StampPath(string projectPath, BuildType build, int? scriptAssemblyNumber) {
			string number = scriptAssemblyNumber.HasValue ? scriptAssemblyNumber.Value.ToString(CultureInfo.InvariantCulture) : "VS";
			return Path.Combine(Path.GetDirectoryName(projectPath), "obj", build.ToString(), "moria-build-stamp." + number + ".txt");
		}

		private static bool TryDeleteStamp(string stampPath) {
			try {
				if (File.Exists(stampPath)) {
					File.Delete(stampPath);
				}
				return true;
			} catch (Exception e) when (e is IOException || e is UnauthorizedAccessException) {
				Logger.WriteWarning("Couldn't delete the build stamp " + stampPath + ": " + e.Message);
				return false;
			}
		}

		/// <summary>The stamp holds the inputs' fingerprint and the output's path, length and write time, so an output
		/// rebuilt or replaced by anything else (an IDE, a plain dotnet build) isn't taken for the stamped one.</summary>
		public static bool TryReuse(string stampPath, string inputsFingerprint, out string compiledFilePath) {
			compiledFilePath = null;
			LastCompileReused = false;
			try {
				if (!File.Exists(stampPath)) {
					return false;
				}
				string[] lines = File.ReadAllLines(stampPath);
				if (lines.Length != 4 || lines[0] != inputsFingerprint) {
					return false;
				}
				var file = new FileInfo(lines[1]);
				// the output belongs to this project (a tree copied with its obj/ would point at the original's)
				string projectDir = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(stampPath), "..", ".."));
				if (!file.FullName.StartsWith(projectDir + Path.DirectorySeparatorChar, StringComparison.Ordinal) || !file.Exists || file.Length.ToString(CultureInfo.InvariantCulture) != lines[2] ||
						file.LastWriteTimeUtc.Ticks.ToString(CultureInfo.InvariantCulture) != lines[3]) {
					return false;
				}
				compiledFilePath = file.FullName;
				LastCompileReused = true;
				return true;
			} catch (Exception e) when (e is IOException || e is UnauthorizedAccessException) {
				return false;
			}
		}

		public static void WriteStamp(string stampPath, string inputsFingerprint, string compiledFilePath) {
			var file = new FileInfo(compiledFilePath);
			Directory.CreateDirectory(Path.GetDirectoryName(stampPath));
			File.WriteAllLines(stampPath, new[] {
				inputsFingerprint,
				file.FullName,
				file.Length.ToString(CultureInfo.InvariantCulture),
				file.LastWriteTimeUtc.Ticks.ToString(CultureInfo.InvariantCulture)
			});
		}

		private static readonly Encoding sourceEncoding = new UTF8Encoding(false, true); // as StreamWriter's default: no BOM, throws on bad text

		/// <summary>Writes a generated source file only if its text differs from what's there (#1358). Rewriting the same
		/// text gave it a new write time, and MSBuild then recompiled the whole project on every boot.</summary>
		/// <returns>True if the file was written.</returns>
		public static bool WriteSourceIfChanged(string path, string text) {
			if (File.Exists(path) && File.ReadAllText(path, sourceEncoding) == text) {
				return false;
			}
			File.WriteAllText(path, text, sourceEncoding);
			return true;
		}

		private static string RunDotnet(string arguments, string projectPath) {
			var psi = new ProcessStartInfo("dotnet", arguments) {
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
				return stdout;
			}
		}
	}
}
