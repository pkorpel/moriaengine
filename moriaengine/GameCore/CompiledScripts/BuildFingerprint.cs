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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1358: a hash of everything a script project's build depends on, so a boot can reuse the last build's
	/// assembly instead of running dotnet build (MsBuildLauncher.Compile's inputsFingerprint).</summary>
	internal static class BuildFingerprint {
		// what MSBuild reads for these projects: sources, resources, the project file, and Directory.Build files above it
		private static readonly string[] inputExtensions = { ".cs", ".resx", ".csproj", ".props", ".targets" };
		private static readonly string[] directoryBuildFiles = { "Directory.Build.props", "Directory.Build.targets", "Directory.Packages.props" };

		/// <summary>Of, or null (build as before, no reuse) if an input can't be read.</summary>
		internal static string TryOf(string projectDir, params string[] parts) {
			try {
				return Of(projectDir, parts);
			} catch (Exception e) when (e is IOException || e is UnauthorizedAccessException) {
				Logger.WriteWarning("Couldn't fingerprint " + projectDir + ", building it: " + e.Message);
				return null;
			}
		}

		/// <param name="parts">Anything else the output depends on: build type, assembly number, an assembly's MVID.</param>
		internal static string Of(string projectDir, params string[] parts) {
			projectDir = Path.GetFullPath(projectDir);
			using (var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256)) {
				foreach (string part in parts) {
					AddString(hash, part);
				}
				// the engine the scripts compile against is the one this process runs on
				foreach (string file in EngineAssemblies()) {
					AddFile(hash, Path.GetFileName(file), file);
				}
				foreach (string file in ProjectInputs(projectDir)) {
					AddFile(hash, Path.GetRelativePath(projectDir, file).Replace('\\', '/'), file);
				}
				for (DirectoryInfo dir = new DirectoryInfo(projectDir).Parent; dir != null; dir = dir.Parent) {
					foreach (string name in directoryBuildFiles) {
						string file = Path.Combine(dir.FullName, name);
						if (File.Exists(file)) {
							AddFile(hash, file, file);
						}
					}
				}
				return Convert.ToHexString(hash.GetHashAndReset());
			}
		}

		internal static IEnumerable<string> EngineAssemblies() {
			return Directory.GetFiles(AppContext.BaseDirectory, "SteamEngine.*.dll").OrderBy(f => f, StringComparer.Ordinal);
		}

		/// <summary>The project's input files, as the SDK's default globs see them: everything below it except the
		/// top-level bin and obj folders and hidden folders, in a fixed order.</summary>
		internal static List<string> ProjectInputs(string projectDir) {
			var files = new List<string>();
			Collect(new DirectoryInfo(projectDir), true, files);
			files.Sort(StringComparer.Ordinal);
			return files;
		}

		private static void Collect(DirectoryInfo dir, bool isProjectRoot, List<string> files) {
			foreach (FileInfo file in dir.EnumerateFiles()) {
				if (inputExtensions.Contains(file.Extension, StringComparer.OrdinalIgnoreCase)) {
					files.Add(file.FullName);
				}
			}
			foreach (DirectoryInfo sub in dir.EnumerateDirectories()) {
				if (sub.Name.StartsWith(".", StringComparison.Ordinal)) {
					continue;
				}
				if (isProjectRoot && (sub.Name.Equals("bin", StringComparison.OrdinalIgnoreCase) ||
						sub.Name.Equals("obj", StringComparison.OrdinalIgnoreCase))) {
					continue;
				}
				Collect(sub, false, files);
			}
		}

		private static void AddString(IncrementalHash hash, string text) {
			byte[] bytes = Encoding.UTF8.GetBytes(text ?? "");
			hash.AppendData(BitConverter.GetBytes(bytes.Length));
			hash.AppendData(bytes);
		}

		private static void AddFile(IncrementalHash hash, string name, string path) {
			AddString(hash, name);
			byte[] content = File.ReadAllBytes(path);
			hash.AppendData(BitConverter.GetBytes(content.Length));
			hash.AppendData(content);
		}
	}
}
