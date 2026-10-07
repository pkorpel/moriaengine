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
using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;

namespace SteamEngine.Common {


	/// <summary>Use this class to run tasks from the sln/csproj files.</summary>
	public static class MsBuildLauncher {
		private static string slnName = "SteamEngine.sln";

		public static string Compile(string seRootPath, BuildType build, string targetTask, int? scriptAssemblyNumber = null) {
			var slnPath = Path.Combine(seRootPath, slnName);
			var globalProperties = new Dictionary<string, string>();
			globalProperties["Configuration"] = build.ToString();
			if (scriptAssemblyNumber.HasValue) {
				globalProperties["ScriptsAssemblyNumber"] = scriptAssemblyNumber.ToString();
			}
			var buildRequest = new BuildRequestData(slnPath, globalProperties, null, new[] { targetTask }, null);
			var buildParameters = new BuildParameters(new ProjectCollection()) { Loggers = new[] { new MsBuildLogger()  } };

			var result = BuildManager.DefaultBuildManager.Build(buildParameters, buildRequest);
			if (result.OverallResult == BuildResultCode.Success) {
				var compiledFilePath = result.ResultsByTarget[targetTask].Items[0].ItemSpec;
				if (File.Exists(compiledFilePath)) {
					return compiledFilePath;
				}
				throw new Exception($"The compiled file \'{compiledFilePath}\' doesn\'t exist.");
			}

			throw new Exception("Compilation failed.", result.Exception);
		}
	}
}

