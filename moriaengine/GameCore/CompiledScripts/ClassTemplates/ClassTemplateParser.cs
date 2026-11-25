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
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.CSharp;
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts.ClassTemplates {
	internal static class ClassTemplateParser {
		private static Regex sectionHeaderRE = new Regex(@"^\[\s*(?<templatename>.*?)\s+(?<classname>.*?)\s*:\s*(?<baseclassname>.*?)\s*\]\s*(//(?<comment>.*))?$",
			RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

		private static Regex subSectionHeaderRE = new Regex(@"^\s*(?<name>.*?)\s*:\s*(//(?<comment>.*))?$",
			RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

		private static Regex fieldRE = new Regex(
			@"^((?<access>(public)|(private)|(protected)|(internal))\s+)?(?<static>static\s+)?(?<type>[a-z_][_a-z0-9\.<>\[\]\,]*)\s+(?<name>[a-z_][_a-z0-9\.]*)\s*=\s*(?<defval>.*)\s*?$",
			RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

		static ScriptFileCollection allFiles;
		static CodeGeneratorOptions options = CreateOptions();

		private static CodeGeneratorOptions CreateOptions() {
			CodeGeneratorOptions options = new CodeGeneratorOptions();
			options.IndentString = "\t";
			return options;
		}

		public static void Init() {
			allFiles = new ScriptFileCollection(Globals.ScriptsPath, ".ct");

			Logger.WriteDebug("Processing ClassTemplates");
			int numFilesRead = 0;
			foreach (ScriptFile file in allFiles.GetAllFiles()) {
				ProcessFile(file);
				numFilesRead++;
			}
			Logger.WriteDebug("Processed " + numFilesRead + " ClassTemplates scripts.");
		}

		public static void Resync() {
			foreach (ScriptFile file in allFiles.GetChangedFiles()) {
				if (file.Exists) {
					ProcessFile(file);
				} else {
					UnloadFile(file);
				}
			}
		}

		private static void UnloadFile(ScriptFile scriptFile) {
			scriptFile.Unload();
			string dirName = Path.GetDirectoryName(scriptFile.FullName);
			string fileName = GetGeneratedFileName(scriptFile.FullName);
			string outFileName = Path.Combine(dirName, fileName);

			if (File.Exists(outFileName)) {
				File.Delete(outFileName);
			}
		}

		[SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands"), SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		private static void ProcessFile(ScriptFile scriptFile) {

			CodeCompileUnit ccu = CreateCompileUnit();

			foreach (ClassTemplateSection section in ParseFile(scriptFile)) {
				try {
					ClassTemplateBase.ProcessSection(section, ccu);
				} catch (FatalException) {
					throw;
				} catch (Exception e) {
					Logger.WriteError(scriptFile.FullName, section.line, e);
				}
			}

			string dirName = Path.GetDirectoryName(scriptFile.FullName);
			string fileName = GetGeneratedFileName(scriptFile.FullName);
			string outFileName = Path.Combine(dirName, fileName);

			using (StreamWriter outFile = new StreamWriter(outFileName)) {
				CodeDomProvider provider = new CSharpCodeProvider();
				provider.GenerateCodeFromCompileUnit(ccu, outFile, options);
			}

		}

		private static IEnumerable<ClassTemplateSection> ParseFile(ScriptFile scriptFile) {
			string fileName = scriptFile.FullName;

			StreamReader stream = scriptFile.OpenText();

			int line = 0;
			ClassTemplateSection curSection = null;
			ClassTemplateSubSection curSubSection = null; //these are also added to curSection...
			while (true) {
				string curLine = stream.ReadLine();
				line++;
				if (curLine != null) {
					curLine = curLine.Trim();
					if ((curLine.Length == 0) || (curLine.StartsWith("//"))) {
						continue;
					}
					Match m = sectionHeaderRE.Match(curLine);
					//[SomethingTemplate Myclass : mybaseclass]
					if (m.Success) {
						if (curSection != null) {
							yield return curSection;
						}
						GroupCollection gc = m.Groups;
						curSection = new ClassTemplateSection(line, gc["templatename"].Value, gc["classname"].Value, gc["baseclassname"].Value);
						curSubSection = null;
						continue;
					}
					m = subSectionHeaderRE.Match(curLine);
					//subsection:
					if (m.Success) {
						//create a new subsection
						curSubSection = new ClassTemplateSubSection(m.Groups["name"].Value);
						if (curSection == null) {
							//a trigger section without real section?
							Logger.WriteWarning(fileName, line, "No section for this subsection...?");
						} else {
							curSection.AddSubSection(curSubSection);
						}
						continue;
					}
					m = fieldRE.Match(curLine);
					if (m.Success) {
						if (curSubSection != null) {
							GroupCollection gc = m.Groups;
							curSubSection.AddField(
								new ClassTemplateInstanceField(gc["access"].Value, gc["static"].Value,
									gc["type"].Value, gc["name"].Value, gc["defval"].Value));
						} else {
							//this shouldnt be, a property without header...?
							Logger.WriteWarning(fileName, line, "No subsection for this field. Skipping line '" + curLine + "'.");
						}
						continue;
					}
					Logger.WriteError(fileName, line, "Unrecognizable data '" + curLine + "'.");
				} else {
					//end of file
					if (curSection != null) {
						yield return curSection;
					}
					break;
				}
			} //end of (while (true)) - for each line of the file
		}

		private static CodeCompileUnit CreateCompileUnit() {
			CodeCompileUnit codeCompileUnit = new CodeCompileUnit();
			CodeNamespace codeNamespace = new CodeNamespace("SteamEngine.CompiledScripts");
			codeNamespace.Imports.Add(new CodeNamespaceImport("System"));
			codeNamespace.Imports.Add(new CodeNamespaceImport("System.Collections"));
			codeNamespace.Imports.Add(new CodeNamespaceImport("System.Collections.Generic"));
			codeNamespace.Imports.Add(new CodeNamespaceImport("SteamEngine"));
			codeNamespace.Imports.Add(new CodeNamespaceImport("SteamEngine.Timers"));
			//codeNamespace.Imports.Add(new CodeNamespaceImport("SteamEngine.Packets"));
			codeNamespace.Imports.Add(new CodeNamespaceImport("SteamEngine.Persistence"));
			codeNamespace.Imports.Add(new CodeNamespaceImport("SteamEngine.Common"));
			codeCompileUnit.Namespaces.Add(codeNamespace);
			return codeCompileUnit;
		}

		private static string GetGeneratedFileName(string origFilename) {
			string filename = Path.GetFileNameWithoutExtension(origFilename);

			//trim every whitespace and '_'
			filename = filename.Trim('_', '\t', '\n', '\v', '\f', '\r', ' ', '\x00a0', '\u2000', '\u2001', '\u2002', '\u2003', '\u2004', '\u2005', '\u2006', '\u2007', '\u2008', '\u2009', '\u200a', '\u200b', '\u3000', '\ufeff');

			return filename + ".Generated.cs";
		}
	}
}
