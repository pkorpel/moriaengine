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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using SteamEngine.Common;
using SteamEngine.CompiledScripts;
using SteamEngine.LScript;
using SteamEngine.Networking;
using SteamEngine.Persistence;
using SteamEngine.Regions;

namespace SteamEngine {
	public static class ScriptLoader {
		private static ScriptFileCollection allFiles = InitScpCollection();

		private static ConcurrentDictionary<string, RegisteredScript> scriptTypesByName =
			new ConcurrentDictionary<string, RegisteredScript>(StringComparer.OrdinalIgnoreCase);

		static ScriptFileCollection InitScpCollection() {
			ScriptFileCollection retVal = new ScriptFileCollection(Globals.ScriptsPath, ".scp");
			retVal.AddExtension(".def");
			//allFiles.AddAvoided("import");
			return retVal;
		}

		//the method that is called on server initialisation by MainClass.
		internal static void Load() {
			ICollection<ScriptFile> files = allFiles.GetAllFiles();
			long lengthSum = allFiles.LengthSum;

			using (StopWatch.StartAndDisplay("Loading " + LogStr.Number(files.Count) + " *.def and *.scp script files. (" + LogStr.Number(lengthSum) + " bytes)...")) {

				ThingDef.StartingLoading();
				ScriptedTriggerGroup.StartingLoading();
				Constant.StartingLoading();
				Map.StartingLoading();
				//ObjectSaver.StartingLoading();
				AbstractSkillDef.StartingLoading();

				long loadedBytes;

				//load either paralell or sequentially, depending on .ini setting
				Func<long, ScriptFile, long> loadAndAggregate = (alreadyloaded, sf) => {
					Logger.WriteDebug("Loading " + sf.Name);
					LoadFile(sf);
					return alreadyloaded + sf.Length;
				};
				Action<long> showLoaded = alreadyloaded => Logger.SetTitle("Loading scripts: " + ((alreadyloaded * 100) / lengthSum) + " %");

				if (Globals.ParallelStartUp) {
					loadedBytes = files.AsParallel().Aggregate(
						() => 0L,
						loadAndAggregate,
						(a, b) => {
							var alreadyloaded = a + b;
							showLoaded(alreadyloaded);
							return alreadyloaded;
						},
						a => a);
				} else {
					loadedBytes = files.Aggregate(
						0L,
						(alreadyloaded, sf) => {
							alreadyloaded = loadAndAggregate(alreadyloaded, sf);
							showLoaded(alreadyloaded);
							return alreadyloaded;
						});
				}

				Sanity.IfTrueSay(lengthSum != loadedBytes, "ScriptLoader.Load: lengthSum != loadedBytes");

				Logger.WriteDebug("Script files loaded.");
				Logger.SetTitle("");//reset title of the console

				//file = new ScriptFile(new FileInfo(Globals.scriptsPath+"\\defaults\\lscript.scp"));
				//LoadFile();

				Constant.LoadingFinished();
				ThingDef.LoadingFinished();
				ScriptedTriggerGroup.LoadingFinished();
				Map.LoadingFinished();
				//ObjectSaver.LoadingFinished();
				AbstractSkillDef.LoadingFinished();
				AbstractDef.LoadingFinished();

				AbstractDefTriggerGroupHolder.LoadingFinished();
				ScriptedGumpDef.LoadingFinished();

				if (Globals.ResolveEverythingAtStart) {
					Constant.ResolveAll();
					AbstractDef.ResolveAll();
				}
			}
		}

		public static void Resync() {
			ICollection<ScriptFile> files = allFiles.GetChangedFiles();//this makes the entities unload
			if (files.Count > 0) {
				PacketSequences.BroadCast("Server is pausing for script resync...");

				Sanity.IfTrueThrow(!RunLevelManager.IsRunning, "!RunLevelManager.IsRunning @ Resync");

				RunLevelManager.SetStartup();

				ObjectSaver.StartingLoading();

				foreach (ScriptFile f in files) {
					if (f.Exists) {
						Console.WriteLine("Resyncing file '" + LogStr.File(f.FullName) + "'.");
						try {
							LoadFile(f);
						} catch (IOException e) {
							Logger.WriteWarning(e);
						}
					} else {
						f.Unload();
					}
				}

				if (Globals.ResolveEverythingAtStart) {
					Constant.ResolveAll();
					AbstractDef.ResolveAll();
				}

				ObjectSaver.LoadingFinished();
				AbstractDef.LoadingFinished();

				AbstractDefTriggerGroupHolder.LoadingFinished();
				ScriptedGumpDef.LoadingFinished();
				//foreach (Thing t in Thing.AllThings) {
				//    t.region = null;
				//}

				RunLevelManager.UnsetStartup();

				PacketSequences.BroadCast("Script resync finished.");
			} else {
				ISrc src = Globals.Src;
				if (src != null) {
					src.WriteLine("No files to resync.");
				} else {
					Console.WriteLine("No files to resync.");
				}
			}
		}

		[SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity"), SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		internal static void LoadFile(ScriptFile file) {
			//string filepath = file.Name;
			//WorldSaver.currentfile = filepath;
			if (file.Exists) { //this may not be true on rare circumstances (basically, delete script and recompile) not gonna do any better fix

				using (StreamReader stream = file.OpenText()) {
					foreach (PropsSection section in PropsFileParser.Load(
							file.FullName, stream, StartsAsScript, false)) {

						try {
							string type = section.HeaderType.ToLowerInvariant();
							string name = section.HeaderName;
							if ((string.IsNullOrEmpty(name)) && (type == "eof")) {
								continue;
							}

							switch (type) {
								case "function":
									file.Add(LScriptMain.LoadAsFunction(section.GetTrigger(0)));
									if (section.TriggerCount > 1) {
										Logger.WriteWarning(section.Filename, section.HeaderLine, "Triggers in a function are nonsensual (and ignored).");
									}
									continue;
								case "typedef":
								case "triggergroup":
								case "events":
								case "event":
									file.Add(ScriptedTriggerGroup.Load(section));
									continue;
								case "defname":
								case "defnames":
								case "constants":
								case "constant":
									foreach (Constant constant in Constant.Load(section)) {
										file.Add(constant);
									}
									continue;

								case "loc":
								case "localisation":
								case "language":
								case "languages":
								case "servloc":
								case "scriptloc":
								case "scriptedloc":
								case "scriptedloccollection":
								case "scriptedlocstringcollection":
								case "locstringcollection":
								case "locstrings":
									file.Add(ScriptedLocStringCollection.Load(section));
									continue;
								//case "template":
								//	
								//	//file.Add(ThingDef.LoadFromScripts(input));
								//	return null;
								case "dialog":
								case "gump":
									IUnloadable gump = ScriptedGumpDef.Load(section);
									if (gump != null) {//it could have been a "subsection" of dialog, i.e. TEXT or BUTTON part
										file.Add(gump);
									}
									continue;
								//case "skill":
								//	Skills.Load(input);
								//	return null;
								default:
									RegisteredScript rs;
									if (scriptTypesByName.TryGetValue(type, out rs)) {
										file.Add(rs.deleg(section));
										continue;
									}

									break;
							}
						} catch (FatalException) {
							throw;
						} catch (Exception e) {
							Logger.WriteError(section.Filename, section.HeaderLine, e);
							continue;
						}
						Logger.WriteError(section.Filename, section.HeaderLine, "Unknown section " + LogStr.Ident(section));
					}
				}
			}
		}

		internal static bool StartsAsScript(string headerType) {
			switch (headerType.ToLowerInvariant()) {
				case "function":
				case "dialog":
				case "gump":
				case "defname":
				case "defnames":
				case "constants":
					return true;
			}
			RegisteredScript rs;
			if (scriptTypesByName.TryGetValue(headerType, out rs)) {
				return rs.startAsScript;
			}
			return false;
		}


		[SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		internal static void LoadNewFile(string filename) {
			FileInfo fi = new FileInfo(filename);
			if (!fi.Exists) {
				try {
					fi = new FileInfo(Path.Combine(Globals.ScriptsPath, filename));
				} catch { }
			}

			if (fi.Exists) {
				if (!allFiles.HasFile(fi)) {
					PacketSequences.BroadCast("Server is pausing for script file loading...");
					Globals.PauseServerTime();

					ScriptFile sf = allFiles.AddFile(fi);
					Console.WriteLine("Loading " + LogStr.File(fi.FullName));
					LoadFile(sf);

					AbstractDefTriggerGroupHolder.LoadingFinished();
					ScriptedGumpDef.LoadingFinished();

					Globals.UnPauseServerTime();
					PacketSequences.BroadCast("Script loading finished.");
				} else {
					throw new SEException("This file is already loaded.");
				}
			} else {
				throw new SEException("Such file does not exist.");
			}
		}

		public static void RegisterScriptType(string name, LoadSection deleg, bool startAsScript) {
			scriptTypesByName.AddOrUpdate(name,
				n => new RegisteredScript(deleg, startAsScript),
				(n, rs) => {
					if (rs.deleg.Method != deleg.Method) {
						throw new OverrideNotAllowedException("There is already a script section loader (" + LogStr.Ident(rs) + ") registered for handling the section name " + LogStr.Ident(name));
					}
					rs.startAsScript = rs.startAsScript || startAsScript; //if any wants true, it stays true. This is here because of AbstractDef and TemplateDef... yeah not exactly clean
					return rs;
				});
		}

		public static void RegisterScriptType(string[] names, LoadSection deleg, bool startAsScript) {
			foreach (string name in names) {
				RegisterScriptType(name, deleg, startAsScript);
			}
		}

		private class RegisteredScript {
			internal LoadSection deleg;
			internal bool startAsScript;

			internal RegisteredScript(LoadSection deleg, bool startAsScript) {
				this.deleg = deleg;
				this.startAsScript = startAsScript;
			}
		}

		//forgets stuff that come from scripts.
		internal static void ForgetScripts() {
			allFiles.Clear();

			Assembly coreAssembly = ClassManager.CoreAssembly;

			var origScripts = scriptTypesByName.ToArray();
			scriptTypesByName = new ConcurrentDictionary<string, RegisteredScript>(StringComparer.OrdinalIgnoreCase);
			foreach (KeyValuePair<string, RegisteredScript> pair in origScripts) {
				if (coreAssembly == pair.Value.deleg.Method.DeclaringType.Assembly) {
					scriptTypesByName[pair.Key] = pair.Value;
				}
			}
		}
	}
}
