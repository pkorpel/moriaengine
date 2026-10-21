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

		//#565 - captured from the "karma"/"npc" @Create lines as they're seen (possibly in
		//either order - confirmed real archive content has both orders), then consulted
		//together in ThirdStage once the whole trigger body has been read, to derive AggroMode.
		private double? pendingKarmaMidpoint;
		private string pendingBrainType;

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
				//#565 - derive AggroMode now that the whole @Create trigger body has been
				//read (karma/npc can appear in either order), since there's no literal
				//AGGROMODE= field anywhere in the real archive to pass through instead.
				this.Set("AggroMode", DeriveAggroMode(this.pendingBrainType, this.pendingKarmaMidpoint), "");
			}
		}

		//#565 - brain types that are never meant to be hostile regardless of their often
		//mildly-negative "flavor" karma (confirmed against real archive data: 26/37 real
		//brain_vendor chardefs sit at exactly Karma=-1000).
		private static readonly HashSet<string> calmBrains = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
			"brain_vendor", "brain_banker", "brain_animal_trainer"
		};
		//#565 - brain types that are always hostile regardless of karma magnitude -
		//confirmed this also catches real undead-boss chardefs (liche/vampire/satan
		//families) that set Karma=0 explicitly, which a pure-Karma reading would miss.
		private static readonly HashSet<string> hostileBrains = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
			"brain_monster", "brain_dragon", "brain_beserk", "brain_daemon"
		};

		//#565 - 3-tier priority formula, derived from real archive data (698 chardefs with
		//Karma, cross-tabbed against NPC=brain_xxx): brain-type override first (vendor/
		//banker/trainer always Calm, monster/dragon/beserk/daemon always Hostile), then
		//Karma as the fallback for everything else (brain_human, brain_animal, no/
		//unrecognized brain) - Hostile only if the configured Karma midpoint is strictly
		//below -1000 (also the default when Karma isn't configured at all).
		private static string DeriveAggroMode(string brainType, double? karmaMidpoint) {
			if ((brainType != null) && calmBrains.Contains(brainType)) {
				return "Calm";
			}
			if ((brainType != null) && hostileBrains.Contains(brainType)) {
				return "Hostile";
			}
			if (karmaMidpoint.HasValue && (karmaMidpoint.Value < -1000)) {
				return "Hostile";
			}
			return "Calm";
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
			//#565 - the real archive brain assignment lives inside @Create too (e.g.
			//c_dragon_v1: "NPC=brain_dragon"), right alongside karma/fame - captured here,
			//consulted together with pendingKarmaMidpoint in ThirdStage once the whole
			//trigger body has been read (the two lines can appear in either order).
			if (name == "npc") {
				if (!this.IsPlayerDefByName) {
					this.pendingBrainType = value.Trim().ToLowerInvariant();
				}
				return;
			}
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
			//#560 - the real archive FAME/KARMA lines live inside @Create too (e.g. c_titan:
			//"FAME={100 2000} KARMA={-5000 -5999}"), always a Sphere {min max} range in source -
			//NPCDef.FameRange/KarmaRange (#560) understands the same shape. numberRE already
			//matches a leading sign, so KARMA's negative values parse correctly. NPCDef-only,
			//same guard as str/dex/int.
			if (name == "fame" || name == "karma") {
				if (this.IsPlayerDefByName) {
					return;
				}
				string fieldName = name == "fame" ? "Fame" : "Karma";
				MatchCollection matches = numberRE.Matches(value);
				if (matches.Count > 0) {
					int min = (int) Math.Round(double.Parse(matches[0].Value, CultureInfo.InvariantCulture));
					if (matches.Count > 1) {
						int max = (int) Math.Round(double.Parse(matches[1].Value, CultureInfo.InvariantCulture));
						this.Set(fieldName, min + " " + max, comment);
						//#565 - stash the karma midpoint for the AggroMode derivation in ThirdStage
						if (name == "karma") {
							this.pendingKarmaMidpoint = (min + max) / 2.0;
						}
					} else {
						this.Set(fieldName, min.ToString(), comment);
						if (name == "karma") {
							this.pendingKarmaMidpoint = (double) min;
						}
					}
				}
				return;
			}
			//#561 - the real archive maxhits/maxstam/maxmana lines live inside @Create too
			//(e.g. c_titan: "maxhits={1500 1700} maxstam={350 400} maxmana=0"), as either a
			//plain number or a Sphere {min max} range - NPCDef.MaxHitsRange/MaxStamRange/
			//MaxManaRange (#561) understands the same shape. NPCDef-only, same guard as
			//str/dex/int/fame/karma.
			if (name == "maxhits" || name == "maxstam" || name == "maxmana") {
				if (this.IsPlayerDefByName) {
					return;
				}
				string hpFieldName = name == "maxhits" ? "MaxHits" : (name == "maxstam" ? "MaxStam" : "MaxMana");
				MatchCollection hpMatches = numberRE.Matches(value);
				if (hpMatches.Count > 0) {
					int min = (int) Math.Round(double.Parse(hpMatches[0].Value, CultureInfo.InvariantCulture));
					if (hpMatches.Count > 1) {
						int max = (int) Math.Round(double.Parse(hpMatches[1].Value, CultureInfo.InvariantCulture));
						this.Set(hpFieldName, min + " " + max, comment);
					} else {
						this.Set(hpFieldName, min.ToString(), comment);
					}
				}
				return;
			}
			//#562 - the real archive sets skill starting values inside @Create too (e.g.
			//c_titan_ranger: "PARRYING={40.0 50.0} WRESTLING={70.0 80.0} ARCHERY={90.0 95.0}
			//resist=100.0 SpiritSpeak=60.0"), as a plain percentage or a Sphere {min max}
			//percentage range. Skills are stored x10 internally in this engine (confirmed
			//elsewhere - 70.0% is raw value 700), so the parsed number(s) are scaled by 10
			//before being emitted. The archive's own field names don't all match the
			//SkillName enum member names NPCDef.LoadScriptLine matches against (#562) -
			//skillNameAliases translates the archive's name to the canonical one to emit.
			string skillFieldName;
			if (skillNameAliases.TryGetValue(name, out skillFieldName)) {
				if (this.IsPlayerDefByName) {
					return;
				}
				MatchCollection skillMatches = numberRE.Matches(value);
				if (skillMatches.Count > 0) {
					int min = (int) Math.Round(double.Parse(skillMatches[0].Value, CultureInfo.InvariantCulture) * 10);
					if (skillMatches.Count > 1) {
						int max = (int) Math.Round(double.Parse(skillMatches[1].Value, CultureInfo.InvariantCulture) * 10);
						this.Set(skillFieldName, min + " " + max, comment);
					} else {
						this.Set(skillFieldName, min.ToString(), comment);
					}
				}
				return;
			}
			//#563 - the real archive grants starting equipment via ITEMNEWBIE lines inside
			//@Create (e.g. c_titan_ranger: "ITEMNEWBIE=i_bow" then "ITEMNEWBIE=i_arrow,{50
			//75}"), one line per item - a chardef commonly has several in a row. Confirmed real
			//forms in source: bare defname, "defname,amount" (plain number), and
			//"defname,{min max}" (Sphere range) - all three map onto NPCDef.StartingEquipment's
			//"Equip = defname[, amount or min max]" syntax. NPCDef-only, same guard as the
			//other @Create fields above.
			if (name == "itemnewbie") {
				if (this.IsPlayerDefByName) {
					return;
				}
				int comma = value.IndexOf(',');
				if (comma < 0) {
					this.Set("Equip", value.Trim(), comment);
				} else {
					string itemDefname = value.Substring(0, comma).Trim();
					string amountPart = value.Substring(comma + 1);
					MatchCollection amountMatches = numberRE.Matches(amountPart);
					if (amountMatches.Count > 0) {
						int min = (int) Math.Round(double.Parse(amountMatches[0].Value, CultureInfo.InvariantCulture));
						if (amountMatches.Count > 1) {
							int max = (int) Math.Round(double.Parse(amountMatches[1].Value, CultureInfo.InvariantCulture));
							this.Set("Equip", itemDefname + ", " + min + " " + max, comment);
						} else {
							this.Set("Equip", itemDefname + ", " + min, comment);
						}
					} else {
						this.Set("Equip", itemDefname, comment);
					}
				}
				return;
			}
			//#564 - the real archive grants multiple spells via addspell= lines inside @Create
			//(e.g. c_titan_mage: "addspell=s_lightning" then "addspell=s_fireball" then
			//"addspell=s_magic_reflection"), one line per spell - maps directly onto
			//NPCDef.Spells' "AddSpell = defname" syntax, no numeric parsing needed (unlike
			//str/fame/skills). NPCDef-only, same guard as the other @Create fields above.
			if (name == "addspell") {
				if (this.IsPlayerDefByName) {
					return;
				}
				this.Set("AddSpell", value.Trim(), comment);
				return;
			}
			base.ProcessCreateTriggerLine(name, value, comment, lineNum);
		}

		//#562 - archive field name (lowercased) -> canonical SkillName enum member name to
		//emit in the .def field, for the ~29 skill names real chardefs set inside @Create.
		//NPCDef.LoadScriptLine's own fallback matches against the literal enum member name
		//(case-insensitive), so most of these map 1:1 - only the handful below actually need
		//translating (archive vocabulary vs. this engine's SkillName naming).
		private static readonly Dictionary<string, string> skillNameAliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
			{ "wrestling", "Wrestling" },
			{ "tactics", "Tactics" },
			{ "resist", "MagicResist" },
			{ "parrying", "Parry" },
			{ "spiritspeak", "SpiritSpeak" },
			{ "peacemaking", "Peacemaking" },
			{ "provocation", "Provocation" },
			{ "swordsmanship", "Swords" },
			{ "magery", "Magery" },
			{ "fencing", "Fencing" },
			{ "macefighting", "Macing" },
			{ "evalint", "EvalInt" },
			{ "taming", "AnimalTaming" },
			{ "archery", "Archery" },
			{ "poisoning", "Poisoning" },
			{ "animallore", "AnimalLore" },
			{ "forensics", "Forensics" },
			{ "anatomy", "Anatomy" },
			{ "healing", "Healing" },
			{ "detectinghidden", "DetectHidden" },
			{ "hiding", "Hiding" },
			{ "stealth", "Stealth" },
			{ "meditation", "Meditation" },
			{ "inscription", "Inscribe" },
			{ "armslore", "ArmsLore" },
			{ "cooking", "Cooking" },
			{ "lumberjacking", "Lumberjacking" },
			{ "blacksmithing", "Blacksmith" },
			{ "veterinary", "Veterinary" }
		};

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
