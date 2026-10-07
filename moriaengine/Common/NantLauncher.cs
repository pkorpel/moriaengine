///*
//	This program is free software; you can redistribute it and/or modify
//    it under the terms of the GNU General Public License as published by
//    the Free Software Foundation; either version 2 of the License, or
//    (at your option) any later version.

//    This program is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//    GNU General Public License for more details.

//    You should have received a copy of the GNU General Public License
//    along with this program; if not, write to the Free Software
//    Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
//	Or visit http://www.gnu.org/copyleft/gpl.html
//*/

//using System;
//using System.IO;
//using System.Reflection;
//using System.Text;
//using System.Text.RegularExpressions;
//using NAnt.Core;


//namespace SteamEngine.Common {


//	/// <summary>Use this class to run tasks from the ./distrib/nant/default.build and/or other NAnt build files.</summary>
//	public class NantLauncher {

//		Project nantProject;
//		string target = "build";
//		Exception exception;
//		IBuildLogger logger;
//		string[] sourceFileNames;
//		string sourceFileNamesFile;

//		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Member")]
//		public const string defaultPathInProject = "./distrib/nant/default.build";

//		public NantLauncher()
//			: this(defaultPathInProject) {
//		}

//		public NantLauncher(string buildFilename) {
//			this.nantProject = new NAnt.Core.Project(buildFilename, Level.Info, 0);
//		}

//		public void SetOutputFilePrefix(string prefix) {
//			this.nantProject.Properties["outputFilePrefix"] = prefix;
//		}

//		public void SetDebugMode(bool debug) {
//			this.nantProject.Properties["debug"] = debug.ToString();
//		}

//		public void SetOptimizeMode(bool optimize) {
//			this.nantProject.Properties["optimize"] = optimize.ToString();
//		}

//		public void SetDefineSymbols(string symbols) {
//			this.nantProject.Properties["defineSymbols"] = symbols;
//		}

//		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "logger"),
//		System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
//		public void SetLogger(IBuildLogger logger) {
//			this.logger = logger;
//		}

//		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "sourceFileNamesFile"),
//		System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "sourceFileNames")]
//		public void SetSourceFileNames(string[] sourceFileNames, string sourceFileNamesFile) {
//			this.sourceFileNames = sourceFileNames;
//			this.sourceFileNamesFile = sourceFileNamesFile;
//		}

//		public void SetProperty(string name, string value) {
//			this.nantProject.Properties[name] = value;
//		}

//		public void SetPropertiesAndSymbols(BuildType build) {
//			StringBuilder symbols;
//			switch (build) {
//				case BuildType.Debug:
//					this.SetDebugMode(true);
//					this.SetOptimizeMode(false);
//					symbols = new StringBuilder("TRACE,DEBUG");
//					break;
//				case BuildType.Release:
//					this.SetDebugMode(false);
//					this.SetOptimizeMode(false);
//					symbols = new StringBuilder("TRACE,SANE");
//					this.SetProperty("cmdLineParams", "/debug+");
//					break;
//				case BuildType.Optimized:
//					this.SetDebugMode(false);
//					this.SetOptimizeMode(true);
//					symbols = new StringBuilder("OPTIMIZED");
//					break;
//				default:
//					throw new ArgumentOutOfRangeException("build");
//			}

//			AddNonbuildSymbolsAsSelf(symbols);
//			SetDefineSymbols(symbols.ToString());
//		}

//		public void SetPropertiesAndSymbolsAsSelf() {
//#if DEBUG
//			this.SetPropertiesAndSymbols(BuildType.Debug);
//#elif SANE
//			this.SetPropertiesAndSymbols(BuildType.Release);

//#elif OPTIMIZED
//			this.SetPropertiesAndSymbols(BuildType.Optimized);
//#else
//#error DEBUG, SANE or OPTIMIZED must be defined
//#endif
//		}

//		private static void AddNonbuildSymbolsAsSelf(StringBuilder symbols) {
//			//not all of these are used. Maybe. Anyway, thay can't harm.
//#if MSWIN
//			symbols.Append(",MSWIN");
//#endif
//#if TESTRUNUO
//			symbols.Append(",TESTRUNUO");
//#endif
//#if MSVS
//			symbols.Append(",MSVS");
//#endif
//#if USEFASTDLL
//			symbols.Append(",USEFASTDLL");
//#endif
//#if MONO
//			symbols.Append(",MONO");
//#endif
//		}

//		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "target"),
//		System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
//		public void SetTarget(string target) {
//			this.target = target;
//		}

//		private void OnBuildFinished(object sender, BuildEventArgs e) {
//			this.exception = e.Exception;
//		}

//		public void Execute() {
//			FileInfo fileListFile = null;
//			if (this.sourceFileNames != null) {
//				fileListFile = new FileInfo(this.sourceFileNamesFile);
//				if (fileListFile.Exists) {
//					fileListFile.Delete();
//				}
//				using (TextWriter writer = new StreamWriter(fileListFile.Create(), System.Text.Encoding.UTF8)) {
//					foreach (string sourcefile in this.sourceFileNames) {
//						writer.WriteLine(sourcefile);
//					}
//					this.nantProject.Properties["sourcesListPath"] = fileListFile.FullName;
//					writer.Flush();
//				}
//			}

//			BuildListenerCollection lListeners = new BuildListenerCollection();
//			IBuildLogger lBuildLogger = this.logger;
//			lBuildLogger.Threshold = Level.Info;
//			lListeners.Add(lBuildLogger);
//			this.nantProject.AttachBuildListeners(lListeners);

//			this.nantProject.BuildFinished += new BuildEventHandler(OnBuildFinished);

//			this.nantProject.BuildTargets.Add(this.target);

//			this.nantProject.Run();
//		}

//		public bool WasSuccess() {
//			return this.exception == null;
//		}


//		public Assembly GetCompiledAssembly(string seRootPath, string filenameproperty) {
//			return Assembly.LoadFile(GetCompiledAssemblyName(seRootPath, filenameproperty));
//		}

//		public string GetCompiledAssemblyName(string seRootPath, string filenameproperty) {
//			return System.IO.Path.GetFullPath(System.IO.Path.Combine(
//				seRootPath, this.nantProject.Properties[filenameproperty]));
//		}

//		public Exception Exception {
//			get {
//				return this.exception;
//			}
//		}


//		private static Regex compileErrorRE = new Regex(@"^\[csc\] (?<filename>.+)\((?<linenumber>\d+),(?<colnumber>\d+)\):(?<errtext>.*)$",
//			//private static Regex compileErrorRE = new Regex(@"^\[csc\](?<filename>.+)$",                   
//			RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

//		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods")]
//		public static object GetDecoratedLogMessage(string msg) {
//			msg = msg.Trim();

//			if (msg.Length == 0) {
//				return null;
//			}

//			if (msg.StartsWith("Buildfile:") || msg.StartsWith("Target framework:") || msg.StartsWith("Target(s) specified:")) {
//				return null;
//			}

//			Match m = compileErrorRE.Match(msg);
//			if (m.Success) {
//				LogStr logstr;

//				if (msg.IndexOf("warning", StringComparison.OrdinalIgnoreCase) > -1) {
//					logstr = LogStr.Warning("WARNING: ");
//				} else {
//					logstr = LogStr.Error("ERROR: ");
//				}

//				logstr += LogStr.FileLine(m.Groups["filename"].Value, int.Parse(m.Groups["linenumber"].Value, System.Globalization.CultureInfo.InvariantCulture))
//					+ ": " + m.Groups["errtext"].Value;

//				return logstr;
//			}

//			if (msg.StartsWith("[csc] ")) {
//				return msg.Substring(6);
//			}

//			return null;//msg - should we want anything else?
//		}

//		//		public void Execute() {
//		//		
//		//		Assembly.LoadFrom(exePath);
//		//		}
//	}
//}