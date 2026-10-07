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
using System.Globalization;
using System.IO;
using SteamEngine.Common;

namespace SteamEngine {
	static class CharData {
		private static HashSet<int> knownDispids = new HashSet<int>();

		public static void AddDispid(int uid) {
			knownDispids.Add(uid);
		}

		public static bool Exists(int num) {
			//Sanity.IfTrueThrow(num>ThingDef.MaxCharModels, "CharData.Exists("+num+") called: "+num+" is an invalid value (valid values are 0-"+ThingDef.MaxCharModels+")");
			return knownDispids.Contains(num);
		}

		public static void GenerateMissingDefs() {
			if (Globals.ReadBodyDefs) {
				StreamReader sr;
				string fPath = Path.Combine(Globals.MulPath, "Bodyconv.def");
				if (File.Exists(fPath)) {
					sr = new StreamReader(fPath);
				} else {
					Logger.WriteCritical("Unable to locate Bodyconv.def.");
					Globals.ReadBodyDefs = false;
					return;
				}

				string chardefsPath = Tools.CombineMultiplePaths(Globals.ScriptsPath, "defaults", "chardefs");
				Tools.EnsureDirectory(chardefsPath, true);

				StreamWriter scr = File.AppendText(Path.Combine(chardefsPath, "newCharDefsFromMuls.def"));
				Console.WriteLine("Checking " + LogStr.File("Bodyconv.def") + " to find character models for which we lack chardefs.");
				int numWritten = 0;
				while (true) {
					string oline = sr.ReadLine();
					string line = oline;
					if (line == null) break;
					int comment = line.IndexOf("#");
					if (comment > -1) {
						line = line.Substring(0, comment);
					}
					line = line.Trim();
					if (line.Length > 0) {
						string[] args = Utility.SplitSphereString(line, true);
						if (args.Length != 4) {
							Logger.WriteWarning("Bodyconv.def contains a line in an unexpected format. That line is '" + line + "'");
							break;
						}
						int model = ConvertTools.ParseInt32(args[0]);
						if (!Exists(model)) {
							AddDispid(model);
							AbstractCharacterDef def = ThingDef.FindCharDef(model);
							if (def == null) {
								if (numWritten == 0) {
									scr.WriteLine("\n\n\n//-------------- Written " + DateTime.Now + " --------------//\n");
								}
								numWritten++;
								scr.WriteLine("");
								scr.WriteLine("[Character 0x" + model.ToString("x", CultureInfo.InvariantCulture) + "]");
								scr.WriteLine("model=0x" + model.ToString("x", CultureInfo.InvariantCulture));
								scr.WriteLine("name=Unknown");
							}
						}
					}
				}
				scr.Close();
				if (numWritten > 0) {
					Console.WriteLine("Wrote " + numWritten + " basic chardefs (for which there were no chardefs in the scripts), to " + chardefsPath + "/newChardefs.def.");
				}
			}
		}
	}
}
