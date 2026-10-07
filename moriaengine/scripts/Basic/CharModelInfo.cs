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
using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {
	[ViewableClass]
	public sealed class CharModelInfo {
		private static Dictionary<int, CharModelInfo> animsByModel = new Dictionary<int, CharModelInfo>();
		private static CharAnimType[] bodyTable;

		public readonly ushort model;
		public readonly ushort icon; //shrinked char's statue
		public readonly CharacterDef charDef;
		public readonly CharAnimType charAnimType;
		public readonly Gender gender;
		public readonly bool isMale;
		public readonly bool isFemale;
		public readonly bool isGhost;

		//data taken from runuo
		public static void Bootstrap() {
			string filename = Path.Combine(Globals.ScriptsPath, "bodyTable.cfg");
			if (File.Exists(filename)) {
				using (StreamReader ip = new StreamReader(filename)) {
					bodyTable = new CharAnimType[1000];

					string line;

					while ((line = ip.ReadLine()) != null) {
						if (line.Length == 0 || line.StartsWith("#"))
							continue;

						string[] split = line.Split('\t');

						try {
							int bodyID = int.Parse(split[0]);
							CharAnimType type = (CharAnimType) Enum.Parse(typeof(CharAnimType), split[1], true);

							if (bodyID >= 0 && bodyID < bodyTable.Length)
								bodyTable[bodyID] = type;
						} catch {
							Logger.WriteWarning("Invalid bodyTable entry:");
							Console.WriteLine(line);
						}
					}
				}
			} else {
				Logger.WriteWarning(filename + " does not exist");
				bodyTable = new CharAnimType[0];
			}
		}

		private CharModelInfo(int model) {
			this.model = (ushort) model;
			this.charDef = (CharacterDef) ThingDef.FindCharDef(model);
			if (this.charDef == null) {
				throw new SEException("There is no Chardef for model 0x" + model.ToString("x"));
			}

			this.charAnimType = CharAnimType.Empty;
			if (model < bodyTable.Length) {
				this.charAnimType = bodyTable[model];
			}

			this.gender = Gender.Undefined;
			if (IsMaleModel(model)) {
				this.gender = Gender.Male;
				this.isMale = true;
			}
			if (IsFemaleModel(model)) {
				this.gender = Gender.Female;
				this.isFemale = true;
			}
			this.isGhost = IsGhostModel(model);
		}

		public static CharModelInfo GetByModel(int model) {
			CharModelInfo info;
			if (animsByModel.TryGetValue(model, out info)) {
				return info;
			}
			info = new CharModelInfo(model);
			animsByModel[model] = info;
			return info;
		}

		public uint AnimsAvailable {
			get {
				return this.charDef.AnimsAvailable;
			}
		}

		public static bool IsMaleModel(int model) {
			switch (model) {
				case 183:
				case 185:
				case 400:
				case 402:
				case 605:
				case 607:
				case 750:
					return true;
			}
			return false;
		}

		public static bool IsFemaleModel(int model) {
			switch (model) {
				case 184:
				case 186:
				case 401:
				case 403:
				case 606:
				case 608:
				case 751:
					return true;
			}
			return false;
		}

		public static bool IsGhostModel(int model) {
			switch (model) {
				case 402:
				case 403:
				case 607:
				case 608:
				case 970:
					return true;
			}
			return false;
		}

		public static bool IsHumanModel(int model) {
			return (GetByModel(model).charAnimType & CharAnimType.Human) == CharAnimType.Human;
		}
	}
}