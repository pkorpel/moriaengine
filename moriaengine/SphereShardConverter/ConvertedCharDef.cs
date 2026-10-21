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
using System.Text.RegularExpressions;
using SteamEngine.Common;

//
namespace SteamEngine.Converter {
	//	/**
	//	Hardcoded changes to converted scripts:
	//		1) The fishing pole is forced to be twohanded.
	//			(It's detected by checking if an item's layer is 2 and model is 0xdbf. 0xdc0 is a dupeitem of it and so will inherit its twohandedness.)
	//	
	//	*/
	//	


	public class ConvertedCharDef : ConvertedThingDef {
		public static Dictionary<string, ConvertedThingDef> charsByDefname = new Dictionary<string, ConvertedThingDef>(StringComparer.OrdinalIgnoreCase);
		public static Dictionary<int, ConvertedThingDef> charsByModel = new Dictionary<int, ConvertedThingDef>();
		//by model and by defnames


		private static LineImplTask[] firstStageImpl = {
				new LineImplTask("anim", MayBeHex_IgnorePoint), 
//TODO:
				new LineImplTask("npc", WriteAsComment),
				new LineImplTask("brain", WriteAsComment)
				


			};

		private static LineImplTask[] thirdStageImpl = {
			new LineImplTask("sound", HandleSound),
			new LineImplTask("mountid", HandleMountId),
			new LineImplTask("mountitem", HandleMountId),
			new LineImplTask("tag.mountid", HandleMountId),
			new LineImplTask("icon", WriteAsIsLine),
			new LineImplTask("can", PassThroughUnlessPlayerDef),
			new LineImplTask("aggromode", PassThroughUnlessPlayerDef),
			new LineImplTask("combatspell", PassThroughUnlessPlayerDef),
			new LineImplTask("experience", PassThroughUnlessPlayerDef),
			new LineImplTask("tamingdifficulty", PassThroughUnlessPlayerDef),
			new LineImplTask("attack", HandleAttack),
			new LineImplTask("dam", HandleAttack),
			new LineImplTask("armor", HandleArmor)
		};

		//guards against both "attack" and "dam" showing up on the same def
		private bool attackHandled;

		public ConvertedCharDef(PropsSection input, ConvertedFile convertedFile)
			: base(input, convertedFile) {
			this.byModel = charsByModel;
			this.byDefname = charsByDefname;

			this.firstStageImplementations.Add(firstStageImpl);
			this.thirdStageImplementations.Add(thirdStageImpl);
		}

		//TEVENTS/aggromode/etc are only valid on NPCDef - a PlayerDef (c_man/c_woman) can't
		//carry them, so PrettyDefname must already be resolved (it is, by ThirdStage time)
		private bool IsPlayerDefByName {
			get {
				switch (this.PrettyDefname.ToLowerInvariant()) {
					case "c_man":
					case "c_woman":
						return true;
					default:
						return false;
				}
			}
		}

		public override void ThirdStage() {
			base.ThirdStage();

			if (this.IsPlayerDefByName) {
				this.headerType = "PlayerDef";
			} else {
				this.headerType = "NPCDef";
			}
		}

		private static void WriteAsIsLine(ConvertedDef def, PropsLine line) {
			def.Set(line);
		}

		private static readonly Regex numberRE = new Regex(@"[-+]?\d+(\.\d+)?",
			RegexOptions.CultureInvariant | RegexOptions.Compiled);

		//the real archive field is tag.experience=<eval {min max}>, embedded inside the
		//ON=@Create trigger body - NOT the top-level "experience" line the base LineImplTask
		//handlers above already cover. NPCDef.ExperienceWorth (#127) already understands the
		//"min max" shape this emits, so this is a pure converter fix - #566.
		protected override void ProcessCreateTriggerLine(string name, string value, string comment, int lineNum) {
			if (name == "tag.experience") {
				MatchCollection matches = numberRE.Matches(value);
				if (matches.Count > 0) {
					int min = (int) Math.Round(double.Parse(matches[0].Value, CultureInfo.InvariantCulture));
					int max = min;
					if (matches.Count > 1) {
						max = (int) Math.Round(double.Parse(matches[1].Value, CultureInfo.InvariantCulture));
					}
					this.Set("Experience", min + " " + max, comment);
				}
				return;
			}
			//#559 - the real archive STR/DEX/INT lines live inside @Create too (e.g. c_titan:
			//"STR=1500 DEX=350 INT=60"), as either a plain number or a Sphere {min max} range -
			//NPCDef.StrRange/DexRange/IntRange (#559) understands the same "value"/"min max"
			//shape ExperienceWorth already uses. NPCDef-only (no PlayerDef in the archive ever
			//sets these - c_man/c_woman have no STR/DEX/INT line at all), same guard as attack/dam.
			if (name == "str" || name == "dex" || name == "int") {
				if (this.IsPlayerDefByName) {
					return;
				}
				string fieldName = name == "str" ? "Str" : (name == "dex" ? "Dex" : "Int");
				MatchCollection matches = numberRE.Matches(value);
				if (matches.Count > 0) {
					int min = (int) Math.Round(double.Parse(matches[0].Value, CultureInfo.InvariantCulture));
					if (matches.Count > 1) {
						int max = (int) Math.Round(double.Parse(matches[1].Value, CultureInfo.InvariantCulture));
						this.Set(fieldName, min + " " + max, comment);
					} else {
						this.Set(fieldName, min.ToString(), comment);
					}
				}
				return;
			}
			base.ProcessCreateTriggerLine(name, value, comment, lineNum);
		}

		//can/aggromode/combatspell/experience/tamingdifficulty are all NPCDef-only fields
		//(confirmed live in NPC.cs/AbstractCharacterDef.cs, #551) - comment them out rather
		//than emit them on a PlayerDef, where they'd produce an "Invalid data" load warning
		private static void PassThroughUnlessPlayerDef(ConvertedDef d, PropsLine line) {
			ConvertedCharDef def = (ConvertedCharDef) d;
			if (def.IsPlayerDefByName) {
				WriteAsComment(def, line);
			} else {
				def.Set(line);
			}
		}

		//attack/dam -> WeaponAttack, averaged the same way ConvertedItemDef.HandleArmorOrDam
		//averages weapon attack ranges - NPCDef-only, matches #44's own hand-edit precedent
		private static void HandleAttack(ConvertedDef d, PropsLine line) {
			ConvertedCharDef def = (ConvertedCharDef) d;
			if (def.IsPlayerDefByName) {
				WriteAsComment(def, line);
				return;
			}
			if (def.attackHandled) {
				return;
			}
			def.attackHandled = true;
			def.Set("WeaponAttack", AverageSphereNumberList(line.Value), line.Comment);
		}

		//ARMOR -> ArmorVsP/ArmorVsM, averaged and duplicated across both - Character-level,
		//safe for PlayerDef too (unlike attack/dam, which are NPCDef-only weapon fields)
		private static void HandleArmor(ConvertedDef d, PropsLine line) {
			ConvertedCharDef def = (ConvertedCharDef) d;
			string value = AverageSphereNumberList(line.Value);
			def.Set("armorVsP", value, line.Comment);
			def.Set("armorVsM", value, "");
		}

		private static string AverageSphereNumberList(string rawValue) {
			string[] values = Utility.SplitSphereString(rawValue, true);
			int n = values.Length;
			if (n == 0) {
				return rawValue;
			}
			double sum = 0;
			for (int i = 0; i < n; i++) {
				int number;
				if (ConvertTools.TryParseInt32(values[i], out number)) {
					sum += number;
				} else {
					return rawValue;
				}
			}
			return Math.Round(sum / n).ToString();
		}

		private static void HandleMountId(ConvertedDef def, PropsLine line) {
			int num = -1;
			if (!ConvertTools.TryParseInt32(line.Value, out num)) {
				ConvertedThingDef i;
				if (ConvertedItemDef.itemsByDefname.TryGetValue(line.Value, out i)) {
					num = i.Model;
				}
			}
			if (num != -1) {
				string retVal = "0x" + num.ToString("x");
				def.Set("MountItem", retVal, line.Comment);
				//return retVal;
			} else {
				def.Warning(line.Line, "Unresolvable MountItem model");
			}
			//return "";
		}


		private static void HandleSound(ConvertedDef def, PropsLine line) {
			string retVal = line.Value;
			int num;
			if (ConvertTools.TryParseInt32(line.Value, out num)) {
				retVal = "0x" + num.ToString("x");
				def.Set("AngerSound", retVal, line.Comment);
				def.Set("IdleSound", "0x" + (num + 1).ToString("x"), "");
				def.Set("AttackSound", "0x" + (num + 2).ToString("x"), "");
				def.Set("HurtSound", "0x" + (num + 3).ToString("x"), "");
				def.Set("DeathSound", "0x" + (num + 4).ToString("x"), "");
			} else {
				def.Set("AngerSound", retVal, line.Comment);
				def.Set("IdleSound", retVal + " + 1", "");
				def.Set("AttackSound", retVal + " + 2", "");
				def.Set("HurtSound", retVal + " + 3", "");
				def.Set("DeathSound", retVal + " + 4", "");
			}
			//return retVal;
		}
	}
}
