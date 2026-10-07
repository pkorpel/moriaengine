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
using System.IO;
using System.Reflection;
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {

	internal static class CompilerInvoker {
		internal static CompiledScriptFileCollection compiledScripts;//CompiledScriptFileCollection instances
																	 //all types of Steamengine namespace, regardless if from scripts or core. 

		private static int compilationNumber;

		//removes all non-core references
		//internal static void UnLoadScripts() {
		//    compiledScripts = null;
		//}

		internal static bool FindIfSourcesHaveChanged() {
			return compiledScripts.GetChangedFiles().Count > 0;
		}


		internal static bool CompileScripts(bool firstCompiling) {
			using (StopWatch.StartAndDisplay("Compiling scripts...")) {
				bool success = true;

				if (firstCompiling) {
					success = ClassManager.InitClasses(ClassManager.CoreAssembly);
				}
				//then try to compile scripts
				compilationNumber++;

				success = success && CompileScriptsUsingMsBuild();

				success = success && ClassManager.InitClasses(compiledScripts.assembly);

				success = success && GeneratedCodeUtil.WriteOutAndCompile(compilationNumber);

				if (success) {
					success = ClassManager.InitClasses(GeneratedCodeUtil.generatedAssembly);
				}

				return success;
			}
		}

		private static bool CompileScriptsUsingMsBuild() {
			try {
				var file = MsBuildLauncher.Compile(".", Build.Type, "SteamEngine_Scripts", compilationNumber);
				var fileCollection = new CompiledScriptFileCollection(Globals.ScriptsPath, ".cs") {
					assembly = Assembly.LoadFile(file)
				};
				fileCollection.GetAllFiles();
				compiledScripts = fileCollection;
				Console.WriteLine($"Done compiling C# scripts. ({Path.GetFileName(file)})");
				return true;
			} catch (Exception e) {
				Logger.WriteError(e);
				return false;
			}
		}
	}
}