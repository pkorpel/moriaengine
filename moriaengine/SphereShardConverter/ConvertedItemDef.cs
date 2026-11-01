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
using System.Text;
using System.Text.RegularExpressions;
using SteamEngine.Common;
using SteamEngine.CompiledScripts;

namespace SteamEngine.Converter {
	public class ConvertedItemDef : ConvertedThingDef {
		public static Dictionary<string, ConvertedThingDef> itemsByDefname = new Dictionary<string, ConvertedThingDef>(StringComparer.OrdinalIgnoreCase);
		public static Dictionary<int, ConvertedThingDef> itemsByModel = new Dictionary<int, ConvertedThingDef>();
		private static readonly Dictionary<string, ConvertedThingDef> itemsByHeader = new Dictionary<string, ConvertedThingDef>(StringComparer.OrdinalIgnoreCase);

		string type;

		string layer = "-1";
		bool layerSet;
		bool isEquippable;

		bool isWeapon;
		bool isWearable;
		bool armorOrDamHandled;
		bool isTwoHanded;
		bool twoHandedSet;
		bool wearableTypeSet;

		//#539 - readable (not just output-text) copies of values the WeaponType/MindPower
		//cross-referencing pass in ThirdStageFinished needs to read back after the fact.
		WeaponType? computedWeaponType;
		double? attackVsPValue;

		string tData1, tData2;

		//#700 - readable copy of TDATA3, needed by ConvertedCharDef's own "tag.mount"
		//handling to resolve a T_EQ_HORSE mount item (e.g. i_mt_horse_brown_dk) to the real
		//mount creature chardef it actually represents (TDATA3=c_horse_brown_dk) - the item's
		//own .Model is the WRONG thing to grab here, see HandleTData3's doc comment.
		public string TData3;

		private static LineImplTask[] firstStageImpl = {
				new LineImplTask("type", HandleType), 
				new LineImplTask("dupelist", HandleDupeList), 
				new LineImplTask("weight", WeightInStones), 
				new LineImplTask("resources", HandleResourcesList),
				new LineImplTask("resources2", HandleResourcesList),
				new LineImplTask("skillmake", HandleResourcesList),
				new LineImplTask("skillmake2", HandleResourcesList),
//TODO
				new LineImplTask("flip", HandleFlip),
				new LineImplTask("reqstr", WriteAsComment),
				new LineImplTask("dye", WriteAsComment),
				new LineImplTask("skill", WriteAsComment),
				new LineImplTask("speed", WriteAsComment)
			};

		private static LineImplTask[] secondStageImpl = {
				new LineImplTask("layer", HandleLayer), 
				new LineImplTask("twohanded", HandleTwohanded),
				new LineImplTask("twohands", HandleTwohanded),
				new LineImplTask("tdata1", HandleTData1),
				new LineImplTask("tdata2", HandleTData2),
				new LineImplTask("tdata3", HandleTData3)
			};

		private static LineImplTask[] thirdStageImpl = {
				new LineImplTask("armor", HandleArmorOrDam),
				new LineImplTask("dam", HandleArmorOrDam)
			};


		public ConvertedItemDef(PropsSection input, ConvertedFile convertedFile)
			: base(input, convertedFile) {
			this.byModel = itemsByModel;
			this.byDefname = itemsByDefname;
			this.byHeader = itemsByHeader;
			this.handWritten = HandWrittenScripts.Items;

			this.firstStageImplementations.Add(firstStageImpl);
			this.secondStageImplementations.Add(secondStageImpl);
			this.thirdStageImplementations.Add(thirdStageImpl);

			this.headerType = "ItemDef";
		}

		//#938 - the models of the flip group, numbers in hex as everywhere in the archive
		private static void HandleDupeList(ConvertedDef def, PropsLine line) {
			List<string> models = new List<string>();
			foreach (string entry in line.Value.Split(new[] { ',', ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries)) {
				bool isHex = Regex.IsMatch(entry, "^(0x)?[0-9a-fA-F]+$", RegexOptions.CultureInvariant);
				int model = isHex ? ConvertTools.ParseInt32(entry) : 0;
				models.Add(isHex ? ("0x" + model.ToString("x", CultureInfo.InvariantCulture)) : entry);
			}
			def.Set("DupeList", string.Join(",", models), line.Comment);
		}

		private static void HandleFlip(ConvertedDef def, PropsLine line) {
			def.Set("Flippable", (line.Value.Trim() != "0") ? "true" : "false", line.Comment);
		}

		//resources list may need some number (counts) corrections
		internal static void HandleResourcesList(ConvertedDef def, PropsLine line) {
			string args = line.Value.ToLowerInvariant();
			StringBuilder corrected = new StringBuilder(args.Length);
			string commentary = "";

			string[] singleResources = args.Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries); //split to single resources
			foreach(string s in singleResources) {
				string singleRes = s.Replace(".", ""); //sphere scripts weirdly have skill numbers with a dot sometimes. Hope this won't break something else than skills
				string[] split = singleRes.Split(Tools.whitespaceChars, StringSplitOptions.RemoveEmptyEntries);
				if (split.Length == 2) {
					object ignored;
					if (ConvertTools.TryParseAnyNumber(split[1], out ignored)) {
						corrected.Append(split[1]).Append(" ").Append(split[0]).Append(", "); //switch the number and the value in the resources definition
						commentary = "some resources were fixed by converter";
					} else {
						corrected.Append(split[0]).Append(" ").Append(split[1]).Append(", ");
					}
				} else if (split.Length == 1) {
					object ignored;
					if (ConvertTools.TryParseAnyNumber(split[0], out ignored)) { //a number alone is an amount of nothing
						commentary = "some resources were fixed by converter";
						continue;
					}
					corrected.Append("1 ").Append(split[0]).Append(", "); //number 1 prepended
					commentary = "some resources were fixed by converter";
				} else if ((split.Length == 3) && ConvertTools.TryParseAnyNumber(split[1], out object amount)) { //#962 - a comma short: MAGERY 80.0 t_sewing_kit
					corrected.Append(split[1]).Append(" ").Append(split[0]).Append(", 1 ").Append(split[2]).Append(", ");
					commentary = "some resources were fixed by converter";
				} else {
					def.Warning(line.Line, "Unknown resource string '"+singleRes+"'");
					corrected.Append(singleRes).Append(", ");
				}
			}
			if (corrected.Length == 0) {
				return;
			}
			corrected.Length -= 2; //remove the last ", "

			def.Set(line.Name, corrected.ToString(), commentary);
			//return line.Value;
		}

		private void SetBadge(string layer) {
			this.headerType = "PowerBadgeDef";
			this.isEquippable = true;
			this.Replace("TYPE", "t_normal", "");
			this.Set("Layer", layer, "");
		}

		private static void HandleType(ConvertedDef d, PropsLine line) {
			ConvertedItemDef def = (ConvertedItemDef) d;
			def.Set(line);

			string args = line.Value.ToLowerInvariant();
			def.type = args;

			switch (args) {
				case "t_eq_script":
					//#892 - what marks an office: PowerBadge here, worn on Sphere's layers for a ring and a bracelet
					if (string.Equals(def.headerName, "i_generals_bracelet", StringComparison.OrdinalIgnoreCase)) {
						def.SetBadge("14");
					} else if (string.Equals(def.headerName, "i_ring_power", StringComparison.OrdinalIgnoreCase) ||
							string.Equals(def.headerName, "i_leading_ring_power", StringComparison.OrdinalIgnoreCase)) {
						def.SetBadge("8");
					}
					break;
				case "t_shrine_power":
					def.headerType = "NazgulShrineDef"; //#892
					def.Replace("TYPE", "t_normal", "");
					break;
				case "t_lordnazgulboard":
					def.headerType = "NazgulBoardDef";
					def.Replace("TYPE", "t_normal", "");
					break;
				case "t_normal":
					if (string.Equals(def.headerName, "i_lordnazguls_gate", StringComparison.OrdinalIgnoreCase) ||
							string.Equals(def.headerName, "i_lordnazguls_gate_out", StringComparison.OrdinalIgnoreCase)) {
						def.headerType = "NazgulGateDef";
					}
					break;
				case "t_generalmenu":
				case "t_lordnazgulmenu":
					def.headerType = "RealmStoneDef"; //#892
					def.Replace("TYPE", "t_normal", "");
					break;
				case "t_vargate":
					def.headerType = "VarGateDef"; //#920
					def.Replace("TYPE", "t_normal", "");
					break;
				case "t_townmenu":
					def.headerType = "TownStoneDef"; //#892 - the typedef's script is TownStone here
					def.Replace("TYPE", "t_normal", "");
					break;
				case "t_container":
				case "t_container_locked":
				case "t_eq_vendor_box":
				case "t_eq_bank_box":
					def.headerType = "ContainerDef";
					if (string.Equals(def.headerName, "i_tax_chest", StringComparison.OrdinalIgnoreCase)) {
						def.headerType = "TaxChestDef";
					}
					def.isEquippable = true;
					break;
				case "t_corpse":
					def.headerType = "CorpseDef";
					def.isEquippable = true;
					break;
				case "t_light_lit":
				case "t_light_out":
				case "t_wand":
				case "t_carpentry_chop":
				case "t_hair":
				case "t_beard":
				case "t_spellbook":
					def.headerType = "EquippableDef";
					def.isEquippable = true;
					break;
				case "t_clothing":
					def.Set("WearableType", "WearableType.Clothing", "guessed by Converter");
					def.wearableTypeSet = true;
					goto case "t_armor_leather";
				case "t_armor_leather":
				case "t_armor":
				case "t_shield":
					def.isWearable = true;
					def.isEquippable = true;
					break;
				case "t_multi":
					if (string.Equals(def.headerName, "i_guildstone", StringComparison.OrdinalIgnoreCase)) {
						//#884 - a plain stone with Erebor's t_guild events, GuildStone here
						def.headerType = "GuildStoneDef";
						def.Replace("TYPE", "t_normal", "");
						break;
					}
					def.headerType = "MultiItemDef";
					break;
				case "t_musical":
					def.headerType = "MusicalDef";
					break;
				case "t_eq_horse":
					//isHorseMountItem=true;
					Logger.WriteInfo(ConverterMain.AdditionalConverterMessages, "Ignoring mountitem def " + LogStr.Ident(def.headerName) + " (steamengine does not need those defined).");
					def.DontDump();	//TODO: make just some constant out of it?
					break;

				case "t_weapon_bolt":
				case "t_weapon_bolt_jagged":
				case "t_weapon_arrow":
					bool isColored = def.IsColoredMetal();
					if (isColored) {
						def.headerType = "ColoredProjectileDef";
						def.Set("MaterialType", "MaterialType.Wood", "guessed by Converter");
					} else {
						def.headerType = "ProjectileDef";
					}
					if (args.Equals("t_weapon_arrow")) {
						def.Set("ProjectileType", "ProjectileType.Arrow", "guessed by Converter");
					} else {
						def.Set("ProjectileType", "ProjectileType.Bolt", "guessed by Converter");
					}

					break;
				case "t_potion":
				case "t_allpotions":
				case "t_drink":
				case "t_potion_shrink":
				case "t_speedpotions":
				case "t_potion_bomba":
					def.headerType = "PotionDef";
					break;
				case "t_fish_pole":
					//the archive's fishing pole falls through to a plain, non-equippable
					//ItemDef without this - T_FISH_POLE has no other handling anywhere in
					//this switch. tooltype=FishingPole (EquippableDef.AssociatedSkill,
					//#392) is what makes dclicking it actually start the Fishing skill.
					def.headerType = "EquippableDef";
					def.isEquippable = true;
					def.Set("tooltype", "FishingPole", "guessed by Converter");
					//real-user-reported: equipping a fishing pole didn't displace whatever
					//weapon was already in hand. The raw archive source has no TWOHANDS= line
					//at all for i_fishing_pole (confirmed - not a dropped/guessed value, just
					//genuinely absent), but a real UO fishing pole IS held with both hands -
					//same "archive has no recoverable data, use real-world knowledge" judgment
					//call as #157/#390's WeaponType/layer corrections. T_FISH_POLE is used by
					//exactly one archive entry (confirmed), so this is safe to apply
					//unconditionally for the whole TYPE, not just a defname-keyed override.
					def.Set("TwoHanded", "true", "guessed by Converter");
					def.isTwoHanded = true;
					def.twoHandedSet = true;
					break;
				default:
					if (args.StartsWith("t_weapon")) {
						def.isWeapon = true;
						def.isEquippable = true;
					}
					break;
			}
			//TODO: Implement args=="t_sign_gump" || "t_board", which have gumps too in sphere scripts,
			//but aren't containers.
			//TODO: Detect if item with 'resmake' isn't craftable.
			//return line.Value;
		}

		private static void HandleTwohanded(ConvertedDef d, PropsLine line) {
			ConvertedItemDef def = (ConvertedItemDef) d;
			def.twoHandedSet = true;
			string largs = line.Value.ToLowerInvariant();
			switch (largs) {
				case "0":
				case "n":
				case "false":
					def.Set("TwoHanded", "false", line.Comment);
					break; //return "false";
				default:
					def.Set("TwoHanded", "true", line.Comment);
					def.isTwoHanded = true;
					break; //return "true";
			}

		}

		private static void HandleArmorOrDam(ConvertedDef d, PropsLine line) {
			ConvertedItemDef def = (ConvertedItemDef) d;
			if (!def.armorOrDamHandled) {
				def.armorOrDamHandled = true;
				string value = null;
				string[] strings = Utility.SplitSphereString(line.Value, true);
				int n = strings.Length;
				double sum = 0;
				for (int i = 0; i < n; i++) {
					string str = strings[i];
					int number;
					if (ConvertTools.TryParseInt32(str, out number)) {
						sum += number;
					} else {
						sum = int.MinValue;
						break;
					}
				}
				bool sumIsValid = sum != 0;
				if (sumIsValid) {
					sum = sum / n;
					if (def.isWearable) {
						sum = Math.Round(sum);
					}
					value = sum.ToString();
				} else {
					value = line.Value;
				}

				if (def.isWeapon) {
					if (sumIsValid) {
						//#539 - readable copy for ThirdStageFinished's MindPower cross-
						//referencing pass (Set() below only ever stores output text).
						def.attackVsPValue = sum;
					}
					def.Set("attackVsP", value, line.Comment);
					def.Set("attackVsM", value, line.Comment);
					def.Set("piercing", "100", "default value set by Converter");
					def.Set("speed", "100", "default value set by Converter");
					def.Set("range", "1", "default value set by Converter");
					def.Set("strikeStartRange", "5", "default value set by Converter");
					def.Set("strikeStopRange", "10", "default value set by Converter");
				} else if (def.isWearable) {
					def.Set("armorVsP", value, line.Comment);
					def.Set("mindDefenseVsP", value, "");
					def.Set("armorVsM", value, "");
					def.Set("mindDefenseVsM", value, "");
				}
				//return value;
			}
			//return "";
		}

		private static void HandleTData1(ConvertedDef def, PropsLine line) {
			((ConvertedItemDef) def).tData1 = line.Value;
			if (def.headerType.Equals("PotionDef", StringComparison.OrdinalIgnoreCase)) {
				def.Set("EmptyFlask", line.Value, "converted from 'TDATA1'");
			} else {
				WriteAsComment(def, line);
			}
		}

		private static void HandleTData2(ConvertedDef def, PropsLine line) {
			((ConvertedItemDef) def).tData2 = line.Value;
			WriteAsComment(def, line);
		}

		//#700 - for a real T_EQ_HORSE mount item, TDATA3 names the actual mount creature
		//chardef this item represents (e.g. i_mt_horse_brown_dk's TDATA3=c_horse_brown_dk) -
		//needed by ConvertedCharDef's "tag.mount" handling. No engine field exists for this
		//directly (TDATA3 has no general meaning outside mount items), so it's still written
		//as a comment for visibility, but also kept in-memory on the item itself (same
		//text-plus-queryable-value shape as #539's computedWeaponType/attackVsPValue).
		private static void HandleTData3(ConvertedDef def, PropsLine line) {
			ConvertedItemDef itemDef = def as ConvertedItemDef;
			if (itemDef != null) {
				itemDef.TData3 = line.Value;
			}
			WriteAsComment(def, line);
		}

		//#618/#628 - a handful of base items that real archive entries still reference via
		//ID=/model= for their own Layer/class/type inheritance were themselves removed from
		//the archive as superseded #618-pattern duplicates (the real moriaengine-authored
		//winner lives elsewhere, e.g. i_spellbook -> scripts/Basic/skills/Magery/
		//spellbooks.def). The converter has zero knowledge of moriaengine's own hand-authored
		//content, so once the archive-side stub is gone, this inheritance silently stops
		//working for every OTHER still-real entry that depended on it (i_spellbook_pvp,
		//i_atlas_map, i_infobook, i_book_rune all lost their EquippableDef/Layer/type this
		//way). This table is the fix: known base-item attributes to fall back to when the
		//normal archive lookup fails, keyed by the literal ID=/model= name - NOT a duplicate
		//archive entry, just enough data for the SAME inheritance mechanism to keep working.
		private static readonly Dictionary<string, (bool isEquippable, string layer, string type)> knownRemovedBases =
			new Dictionary<string, (bool isEquippable, string layer, string type)>(StringComparer.OrdinalIgnoreCase) {
				{ "i_spellbook", (true, "1", "t_spellbook") },
			};

		public static void SecondStageFinished() {
			HashSet<ConvertedItemDef> itemDefSet = new HashSet<ConvertedItemDef>();
			foreach (ConvertedItemDef def in itemsByDefname.Values) {
				itemDefSet.Add(def);
			}

			foreach (ConvertedItemDef def in itemDefSet) {
				ConvertedItemDef baseDef = def.modelDef as ConvertedItemDef;
				bool isEquippableBase, layerSetBase, isTwoHandedBase;
				string layerBase, typeBase;
				if (baseDef != null) {
					isEquippableBase = baseDef.isEquippable;
					layerSetBase = baseDef.layerSet;
					isTwoHandedBase = baseDef.isTwoHanded;
					layerBase = baseDef.layer;
					typeBase = baseDef.type;
				} else if ((def.idLine != null) && knownRemovedBases.TryGetValue(def.idLine.Value, out var known)) {
					isEquippableBase = known.isEquippable;
					layerSetBase = true;
					isTwoHandedBase = false;
					layerBase = known.layer;
					typeBase = known.type;
				} else {
					continue;
				}

				if (isEquippableBase) {
					def.MakeEquippable();

					if (isTwoHandedBase && (!def.twoHandedSet)) {
						def.Set("TwoHanded", "true", "guessed from base def by Converter");
						def.twoHandedSet = true;
						def.isTwoHanded = true;
					}
					if (layerSetBase && (!def.layerSet)) {
						def.Set("Layer", layerBase, "guessed from base def by Converter");
						def.layerSet = true;
						def.layer = layerBase;
					}
				}

				if ((typeBase != null) && (def.type == null)) {
					HandleType(def, new PropsLine("type", typeBase, -1, "guessed from base def by Converter"));
				}
			}
		}

		//#539 - the material tiers a blunt weapon's own defname gets suffixed with. Copper is
		//always the baseline (ratio 1.0, MindPower stays flat 700) - every other tier scales
		//up from it using that same archetype's own real AttackVsP progression. Order matters
		//only in that "copper" must be tried last, since it's a substring-safe suffix of
		//nothing else in this list - but there's no actual ambiguity here, this is just the
		//known real tier list.
		private static readonly string[] materialTierSuffixes = {
			"_copper", "_iron", "_verite", "_valorite", "_obsidian", "_adamantinum", "_mithril"
		};

		public static void ThirdStageFinished() {
			ApplyArmorSettings();
			ApplyCashiersSettings();
			foreach (ConvertedItemDef def in itemsByDefname.Values) {
				if (def.isWeapon) {
					def.ApplyWeaponSettings();
				} else if (def.headerType.EndsWith("ProjectileDef", StringComparison.OrdinalIgnoreCase)) {
					def.ApplyAmmoSettings();
				}
				if (def.computedWeaponType != WeaponType.OneHandBlunt && def.computedWeaponType != WeaponType.TwoHandBlunt) {
					continue;
				}
				if (SphereWorldVars.TryGetInt("nastaveni_staffSMbonus_" + def.PrettyDefname, out int staffBonus)) {
					def.Set("MindPowerVsP", staffBonus.ToString(CultureInfo.InvariantCulture), "nastaveni_staffSMbonus");
					def.Set("MindPowerVsM", staffBonus.ToString(CultureInfo.InvariantCulture), "");
					continue;
				}
				if (def.attackVsPValue == null) {
					continue;
				}
				string prettyDefName = def.PrettyDefname;
				string baseName = null;
				foreach (string suffix in materialTierSuffixes) {
					if (prettyDefName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)) {
						baseName = prettyDefName.Substring(0, prettyDefName.Length - suffix.Length);
						break;
					}
				}
				if (baseName == null) {
					continue; //not part of the material-tier system, nothing to scale against
				}
				if (!itemsByDefname.TryGetValue(baseName + "_copper", out ConvertedThingDef copperBaseThing)) {
					continue;
				}
				ConvertedItemDef copperBase = copperBaseThing as ConvertedItemDef;
				if ((copperBase == null) || (copperBase.attackVsPValue == null) || (copperBase.attackVsPValue == 0)) {
					continue;
				}
				double mindPower = Math.Round(700.0 * def.attackVsPValue.Value / copperBase.attackVsPValue.Value, 2);
				string mindPowerText = mindPower.ToString("0.0##", CultureInfo.InvariantCulture);
				string comment = "#539 - scaled: 700 * " + def.attackVsPValue.Value.ToString("0.0##", CultureInfo.InvariantCulture)
					+ "/" + copperBase.attackVsPValue.Value.ToString("0.0##", CultureInfo.InvariantCulture)
					+ " (bare-hands baseline x this weapon's own real AttackVsP progression)";
				def.Set("MindPowerVsP", mindPowerText, comment);
				def.Set("MindPowerVsM", mindPowerText, "#539 - same scaling, AttackVsM==AttackVsP for every real weapon def");
			}
		}

		//#860 - f_nastaveni_setdefault overwrites DAM with nastaveni_weapdam_<defname>; speed and
		//piercing are read by the display id (combat.scp), so all materials share them
		private void ApplyWeaponSettings() {
			string defname = this.PrettyDefname;
			string dispId = (this.modelDef != null) ? this.modelDef.PrettyDefname : defname;
			if (SphereWorldVars.TryGetInt("nastaveni_weapdam_" + defname, out int attack)) {
				this.attackVsPValue = attack;
				this.Replace("attackVsP", attack.ToString(CultureInfo.InvariantCulture), "nastaveni_weapdam");
				this.Replace("attackVsM", attack.ToString(CultureInfo.InvariantCulture), "nastaveni_weapdam");
			}
			if (SphereWorldVars.TryGetInt("nastaveni_weapspeed_" + dispId, out int speed)) {
				this.Replace("speed", speed.ToString(CultureInfo.InvariantCulture), "nastaveni_weapspeed");
			}
			foreach (string suffix in materialTierSuffixes) {
				if (defname.EndsWith(suffix, StringComparison.OrdinalIgnoreCase) &&
						SphereWorldVars.TryGetInt("nastaveni_durability" + suffix, out int metalDurability) &&
						SphereWorldVars.TryGetInt("nastaveni_durability_" + dispId, out int weaponDurability)) {
					//weaponcreate
					this.Set("maxDurability", ((metalDurability * weaponDurability) / 1000).ToString(CultureInfo.InvariantCulture), "nastaveni_durability");
				}
			}
			if (SphereWorldVars.TryGetInt("nastaveni_cm_can_repair_" + defname, out int canRepair) && (canRepair != 0)) {
				this.Set("canRepair", "true", "nastaveni_cm_can_repair");
			}
			if (SphereWorldVars.TryGetInt("nastaveni_weapbleed_" + dispId, out int bleed)) {
				this.Replace("BleedingEfficiency", (bleed / 1000.0).ToString("0.###", CultureInfo.InvariantCulture), "nastaveni_weapbleed");
			}
			if (SphereWorldVars.TryGetInt("nastaveni_weappiercing_" + dispId, out int piercing)) {
				this.Replace("piercing", piercing.ToString(CultureInfo.InvariantCulture), "nastaveni_weappiercing");
			}
		}

		//#860 - ammo's TDATA1 and TDATA2 index nastaveni_arrowdamage_N (combat.scp): the first is a
		//damage multiplier in permille, the second piercing; no TDATA2 is index 0, piercing 0
		//#883 - seky.scp's money items; the limits are globals in sphereworld.scp
		private static void ApplyCashiersSettings() {
			string[,] items = {
				{ "i_cashiers_check", null },
				{ "i_cashiers_mesec", "nastaveni_max_amount_mesec" },
				{ "i_cashiers_pokladnicka", "nastaveni_max_amount_pokladnice" }
			};
			for (int i = 0; i < items.GetLength(0); i++) {
				if (itemsByDefname.TryGetValue(items[i, 0], out ConvertedThingDef def)) {
					def.headerType = "CashiersItemDef";
					def.Remove("layer"); //a bag's, and this isn't worn
					if ((items[i, 1] != null) && SphereWorldVars.TryGetInt(items[i, 1], out int maxAmount)) {
						def.Set("MaxAmount", maxAmount.ToString(CultureInfo.InvariantCulture), "");
					}
				}
			}
		}

		private void ApplyAmmoSettings() {
			if (!int.TryParse(this.tData1, NumberStyles.Integer, CultureInfo.InvariantCulture, out int damageIndex) ||
					!SphereWorldVars.TryGetInt("nastaveni_arrowdamage_" + damageIndex, out int damage)) {
				return;
			}
			if (!int.TryParse(this.tData2, NumberStyles.Integer, CultureInfo.InvariantCulture, out int piercingIndex)) {
				piercingIndex = 0;
			}
			SphereWorldVars.TryGetInt("nastaveni_arrowdamage_" + piercingIndex, out int piercing);
			this.Set("DamageMultiplier", (damage / 1000.0).ToString("0.0##", CultureInfo.InvariantCulture), "nastaveni_arrowdamage_" + damageIndex);
			this.Set("Piercing", piercing.ToString(CultureInfo.InvariantCulture), "nastaveni_arrowdamage_" + piercingIndex);
		}

		//#860 - the defnames of the f_<type>defname functions in nastaveni_ceho.scp, {0} = metal
		private static readonly Dictionary<string, string[]> armorSetPieces = new Dictionary<string, string[]> {
			{ "plate", new[] { "i_{0}_closed_helm", "i_{0}_bascinet", "i_{0}_nose_helm", "i_{0}_plate_helm", "i_{0}_platemail_gorget", "i_{0}_platemail",
				"i_{0}_female_platemail", "i_{0}_platemail_arms", "i_{0}_platemail_gauntlets", "i_{0}_platemail_leggings", "i_{0}_heater_shield" } },
			{ "chain", new[] { "i_{0}_chainmail_coif", "i_{0}_chainmail_gorget", "i_{0}_chainmail_tunic", "i_{0}_chainmail_sleeves", "i_{0}_chainmail_gloves",
				"i_{0}_chainmail_leggings", "i_{0}_metal_shield" } },
			{ "ring", new[] { "i_{0}_ringmail_helm", "i_{0}_ringmail_gorget", "i_{0}_ringmail_tunic", "i_{0}_ringmail_sleeves", "i_{0}_ringmail_gloves",
				"i_{0}_ringmail_leggings", "i_{0}_kite_wood_shield" } },
			{ "bone", new[] { "i_bone_helmet_{0}", "i_bone_chest_{0}", "i_bone_arms_{0}", "i_bone_gloves_{0}", "i_bone_leggings_{0}" } },
			{ "studded", new[] { "i_studded_cap_{0}", "i_studded_gorget_{0}", "i_studded_tunic_{0}", "i_armor_female_bustier_studded_{0}", "i_armor_female_studded_{0}",
				"i_studded_sleeves_{0}", "i_studded_gloves_{0}", "i_studded_leggings_{0}", "i_{0}_buckler_shield" } }
		};

		private static readonly string[] armorMetals = { "copper", "iron", "silver", "golden", "verite", "valorite", "obsidian", "adamantinum", "mithril" };

		//#860 - f_nastav_armorset: nastaveni_armorarmor_<metal>_<type> is the armor of every piece of
		//the set, a shield gets a sixth of it, and a plate set's shield then loses another 8
		private static void ApplyArmorSettings() {
			SphereWorldVars.TryGetInt("nastaveni_global_plateshield_armor_decrease", out int plateShieldChange);
			foreach (KeyValuePair<string, string[]> set in armorSetPieces) {
				foreach (string metal in armorMetals) {
					if (!SphereWorldVars.TryGetInt("nastaveni_armorarmor_" + metal + "_" + set.Key, out int armor)) {
						continue;
					}
					foreach (string piece in set.Value) {
						string defname = string.Format(CultureInfo.InvariantCulture, piece, metal);
						if (itemsByDefname.TryGetValue(defname, out ConvertedThingDef thing)) {
							bool isShield = defname.EndsWith("_shield", StringComparison.OrdinalIgnoreCase);
							int value = isShield ? (armor / 6) : armor;
							if (isShield && set.Key.Equals("plate")) {
								value += plateShieldChange;
							}
							//f_nastav_armorset's argv(4)==1: a share of the whole set by layer, not in the shield's -8
							if (SphereWorldVars.TryGetInt("nastaveni_armorweight_" + metal + "_" + set.Key, out int setWeight) &&
									int.TryParse(((ConvertedItemDef) thing).layer, NumberStyles.Integer, CultureInfo.InvariantCulture, out int layer) &&
									SphereWorldVars.TryGetLayerArmorPercent(layer, out int layerPercent)) {
								int pieceWeight = isShield ? (setWeight / 6) : setWeight;
								pieceWeight = ((layerPercent * pieceWeight) + 50) / 100;
								//the setting is in tenths of a stone, Sphere's native unit
								thing.Replace("WEIGHT", (pieceWeight / 10.0).ToString("R", CultureInfo.InvariantCulture), "nastaveni_armorweight");
								//armorcreate, shieldcreate
								if (SphereWorldVars.TryGetInt("nastaveni_durability_" + metal, out int metalDurability) &&
										SphereWorldVars.TryGetInt("nastaveni_durability_" + set.Key, out int setDurability)) {
									thing.Set("maxDurability", ((metalDurability * setDurability * layerPercent) / 100000).ToString(CultureInfo.InvariantCulture), "nastaveni_durability");
								}
							}
							if (SphereWorldVars.TryGetInt("nastaveni_cm_can_repair_" + defname, out int canRepair) && (canRepair != 0)) {
								thing.Set("canRepair", "true", "nastaveni_cm_can_repair");
							}
							thing.Replace("armorVsP", value.ToString(CultureInfo.InvariantCulture), "nastaveni_armorarmor");
							thing.Replace("armorVsM", value.ToString(CultureInfo.InvariantCulture), "");
							if (SphereWorldVars.TryGetInt("nastaveni_armorparts_OM_" + defname, out int mindDefense)) {
								thing.Replace("mindDefenseVsP", mindDefense.ToString(CultureInfo.InvariantCulture), "nastaveni_armorparts_OM");
								thing.Replace("mindDefenseVsM", mindDefense.ToString(CultureInfo.InvariantCulture), "");
							}
						}
					}
				}
			}
		}

		private void MakeEquippable() {
			if (this.headerType.Equals("ItemDef")) {
				this.headerType = "EquippableDef";
			}
			this.isEquippable = true;
		}

		//defname -> ToolType - items whose archive TYPE= is shared with an unrelated
		//weapon category (so WeaponType-based/generic detection can't identify them as a
		//gathering tool), confirmed case by case. i_pickaxe carries TYPE=t_weapon_bow_run
		//in the raw source (the same type real bow "run" variants use - a pre-existing
		//archive-authoring quirk, not a converter bug), which makes it compute as a
		//WeaponDef with WeaponType.Bow - tooltype=Pickaxe (EquippableDef.AssociatedSkill,
		//#392) is what actually makes dclicking it start the Mining skill, independent of
		//whatever WeaponType it happens to compute as. Add a defname here ONLY after
		//confirming its own TYPE= doesn't already map to a real tool category above.
		private static readonly Dictionary<string, string> toolTypeOverrides =
			new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
				{ "i_pickaxe", "Pickaxe" }
			};

		//defname -> real Layer, for items whose own TwoHanded= is real/confirmed (not a
		//converter guess) but whose Layer still falls back to the generic tiledata-Quality-
		//byte guess below, which disagrees with that real data. i_hatchet's own TwoHanded=
		//false is real (no guess-comment on it, confirmed via the raw archive source), which
		//is only consistent with one-handed Hand1 (layer=1) - the tiledata fallback instead
		//guesses layer=2 (Hand2, shared with shields/two-handed items), so equipping a hatchet
		//never displaced whatever one-handed weapon was already in Hand1 (#390). Originally
		//fixed as a one-off hand-patch of the OUTPUT file, which a later full regen (#640)
		//silently reverted (output-only fixes don't survive a regen, by design) - this table
		//is the durable fix, applied every regen from here on. Same ~70-entry layer/TwoHanded
		//reliability gap as #393 affects other weapons, but only i_hatchet has directly-
		//verifiable real TwoHanded data backing a confident correction (per #390's own
		//scoping) - don't widen this table opportunistically.
		private static readonly Dictionary<string, string> layerOverrides =
			new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
				{ "i_hatchet", "1" }
			};

		private static readonly Regex itemNameRE = new Regex(@"\bi_\w+", RegexOptions.CultureInvariant);

		//#937 - a recipe with something that's nowhere (i_enchant_weapon) can't be made. One under a second defname,
		//which defs here don't have (i_lemon), is written under the first.
		private void FixRecipeNames() {
			for (int i = 0; i < this.writtenData.Count; i++) {
				string line = this.writtenData[i];
				if (line.StartsWith("resources", StringComparison.OrdinalIgnoreCase)) {
					foreach (Match m in itemNameRE.Matches(line.Split(new[] { "//" }, 2, StringSplitOptions.None)[0])) {
						if (itemsByDefname.TryGetValue(m.Value, out ConvertedThingDef def)) {
							if (def.HasDefname2(m.Value)) {
								this.writtenData[i] = line.Replace(m.Value, def.PrettyDefname);
							}
						} else if (!HandWrittenScripts.IsDefined(m.Value)) {
							this.writtenData[i] = "//" + line + " //" + m.Value + " is nowhere, commented out by Converter";
							break;
						}
					}
				}
			}
		}

		//#962 - cm_applyall: the shard's live recipes override what its .scp files say, so most of those are out of date
		private void ApplyLiveRecipe() {
			string resources = SphereWorldVars.GetRecipe("nastaveni_cm_resources_" + this.PrettyDefname);
			if (resources != null) {
				this.Replace("RESOURCES", resources, "nastaveni_cm_resources");
			}
			string skillMake = SphereWorldVars.GetRecipe("nastaveni_cm_skillmake_" + this.PrettyDefname);
			if (skillMake != null) {
				this.Replace("SKILLMAKE", skillMake, "nastaveni_cm_skillmake");
			}
		}

		//#962 - t_shaft_multi and t_arrow_piercing_multi unpack into TDATA1, TDATA2 of them each
		private void WriteBundleData() {
			if ((this.tData1 != null) && this.writtenData.Exists(line => line.EndsWith("_multi", StringComparison.OrdinalIgnoreCase) && line.StartsWith("type", StringComparison.OrdinalIgnoreCase))) {
				this.Set("tag.bundleItem", "\"" + this.tData1 + "\"", "converted from 'TDATA1'");
				if (this.tData2 != null) {
					this.Set("tag.bundleCount", this.tData2, "converted from 'TDATA2'");
				}
			}
		}

		//#962 - raw food says in TDATA1 what cooking it makes
		private void WriteCookedData() {
			if ((this.tData1 != null) && this.writtenData.Exists(line => line.EndsWith("t_food_raw", StringComparison.OrdinalIgnoreCase) || line.EndsWith("t_meat_raw", StringComparison.OrdinalIgnoreCase))) {
				this.Set("tag.cooked", "\"" + this.tData1 + "\"", "converted from 'TDATA1'");
			}
		}

		public override void ThirdStage() {
			this.ApplyLiveRecipe();
			this.WriteBundleData();
			this.WriteCookedData();
			this.FixRecipeNames();

			string toolTypeOverride;
			if (toolTypeOverrides.TryGetValue(this.PrettyDefname, out toolTypeOverride)) {
				this.Set("tooltype", toolTypeOverride, "guessed by Converter");
			}

			string layerOverride;
			if (layerOverrides.TryGetValue(this.PrettyDefname, out layerOverride)) {
				this.layer = layerOverride;
				this.Set("layer", layerOverride, "#390 - corrected; this entry's own TwoHanded=false is real (no guess-comment), but the tiledata-Quality-byte fallback guess is layer=2 - one-handed Hand1 is consistent with TwoHanded=false");
				this.layerSet = true;
			}

			if (this.isEquippable && !this.layerSet) {
				int model = this.Model;
				ItemDispidInfo info = ItemDispidInfo.GetByModel(model);
				if (info != null) {
					this.layer = info.Quality.ToString();
					this.Set("layer", this.layer, "Set by Converter");
					this.layerSet = true;
				} else {
					this.Set("//layer", "unknown", "");
					this.Info(this.origData.HeaderLine, "Unknown layer for ItemDef " + this.headerName);
				}
			}

			if (this.isWeapon) {
				bool isColored = this.IsColoredMetal();
				if (isColored) {
					this.headerType = "ColoredWeaponDef";
				} else {
					this.headerType = "WeaponDef";
				}

				MaterialType materialType = MaterialType.Metal;
				WeaponType weaponType = WeaponType.BareHands;

				switch (this.type.ToLowerInvariant()) {
					case "t_weapon_sword":
						string prettyDefName = this.PrettyDefname.ToLowerInvariant();
						//#157 - a handful of real axe-type polearms are explicitly typed
						//t_weapon_sword in the raw Sphere source (confirmed directly: e.g.
						//i_bardiche/i_halberd carry "SKILL=Swordsmanship" and a source comment
						//"Using Sword type below") - the source's own game-mechanic choice
						//(which skill trains it) isn't the same as the weapon's real-world
						//category, so these need a name-based override on top of the TYPE=
						//field, same as the existing "_axe_" substring check right below.
						bool isKnownAxeSynonym = prettyDefName.Contains("bardiche")
							|| prettyDefName.Contains("halberd")
							|| prettyDefName.Contains("hatchet");
						if (isKnownAxeSynonym || prettyDefName.Contains("_axe_") || prettyDefName.EndsWith("_axe")) {
							if (this.isTwoHanded) {
								weaponType = WeaponType.TwoHandAxe;
							} else {
								weaponType = WeaponType.OneHandAxe;
							}
						} else {
							if (this.isTwoHanded) {
								weaponType = WeaponType.TwoHandSword;
							} else {
								weaponType = WeaponType.OneHandSword;
							}
						}
						break;
					case "t_weapon_fence":
						if (this.isTwoHanded) {
							weaponType = WeaponType.TwoHandSpike;
						} else {
							weaponType = WeaponType.OneHandSpike;
						}
						break;
					case "t_weapon_mace_staff":
						if (isColored) {
							this.headerType = "ColoredStaffDef";
						}
						goto case "t_weapon_mace_crook";
					case "t_weapon_mace_crook":
						materialType = MaterialType.Wood;
						goto case "t_weapon_mace_smith";
					case "t_weapon_mace_smith":
						//#157 - mace/staff/crook/smith are blunt weapons (real Sphere source
						//comment: "jednorucni/dvourucni tupe" = one/two-handed BLUNT), not
						//fencing/piercing weapons - this case used to fall all the way through
						//to "t_weapon_fence"'s Spike assignment via the goto chain above, which
						//was wrong for every mace/staff/hammer in the archive.
						if (this.isTwoHanded) {
							weaponType = WeaponType.TwoHandBlunt;
						} else {
							weaponType = WeaponType.OneHandBlunt;
						}
						break;
					case "t_weapon_bow":
						weaponType = WeaponType.Bow;
						materialType = MaterialType.Wood;
						this.Set("ProjectileType", "ProjectileType.Arrow", "guessed by Converter");
						this.Set("ProjectileAnim", "0xf42", "guessed by Converter");
						break;
					case "t_weapon_xbow":
						weaponType = WeaponType.XBow;
						materialType = MaterialType.Wood;
						this.Set("ProjectileType", "ProjectileType.Bolt", "guessed by Converter");
						this.Set("ProjectileAnim", "0x1bfe", "guessed by Converter");
						break;
					case "t_weapon_xbow_run":
						weaponType = WeaponType.XBow;
						materialType = MaterialType.Wood;
						this.Set("ProjectileType", "ProjectileType.Bolt", "guessed by Converter");
						this.Set("ProjectileAnim", "0x1bfe", "guessed by Converter");
						break;
					case "t_weapon_bow_run":
						weaponType = WeaponType.Bow;
						materialType = MaterialType.Wood;
						this.Set("ProjectileType", "ProjectileType.Arrow", "guessed by Converter");
						this.Set("ProjectileAnim", "0xf42", "guessed by Converter");
						break;
				}
				this.Set("WeaponType", "WeaponType." + weaponType, "guessed by Converter");
				this.computedWeaponType = weaponType;

				WeaponAnimType animType = WeaponAnimTypeSetting.TranslateAnimType(weaponType);
				if (animType != WeaponAnimType.Undefined) {
					this.Set("WeaponAnimType", "WeaponAnimType." + animType, "guessed by Converter");
				}

				if (isColored) {
					this.Set("MaterialType", "MaterialType." + materialType, "guessed by Converter");
				}
			} else if (this.isWearable) {
				bool isColored = this.IsColoredMetal();
				if (isColored) {
					this.headerType = "ColoredArmorDef";
					this.Set("MaterialType", "MaterialType.Metal", "guessed by Converter");
				} else {
					this.headerType = "WearableDef";
				}
				if (!this.wearableTypeSet) {
					this.Set("WearableType", "WearableType." + this.GetWearableType(this.PrettyDefname), "guessed by Converter");

				}
			}
			base.ThirdStage();
		}

		//called on the very end of 3rd stage
		protected override void ProcessCreateTriggerLine(string name, string value, string comment, int lineNum) {
			switch (name) {
				case "tag.jed_typ":
					this.headerType = "PoisonPotionDef";
					this.Set("PoisonType", "p_"+value, comment);
					break;
			}
			base.ProcessCreateTriggerLine(name, value, comment, lineNum);
		}

		private bool IsColoredMetal() {
			string material = GetMaterialFromDefname(this.PrettyDefname);
			if (material != null) {
				this.Set("Material", "Material." + Utility.Capitalize(material), "guessed by Converter");
				return true;
			}
			return false;
		}

		private static Regex materialRE = new Regex(
			@".*(?<material>((Copper)|(Iron)|(Silver)|(Gold)|(Verite)|(Valorite)|(Obsidian)|(Adamantinum)|(Mithril))).*",
			RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase);

		private static string GetMaterialFromDefname(string defname) {
			Match m = materialRE.Match(defname);
			if (m.Success) {
				return m.Groups["material"].Value;
			}
			return null;
		}

		private static Regex wearableTypeRE = new Regex(
			@".*(?<type>((bone)|(studded)|(chain)|(ring))).*",
			RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase);

		private WearableType GetWearableType(string prettyDefname) {
			Match m = wearableTypeRE.Match(prettyDefname);
			if (m.Success) {
				switch (m.Groups["type"].Value.ToLowerInvariant()) {
					case "bone":
						return WearableType.Bone;
					case "studded":
						return WearableType.Studded;
					case "chain":
						return WearableType.Chain;
					case "ring":
						return WearableType.Ring;
				}
			}
			if (this.type.Equals("t_armor_leather", StringComparison.OrdinalIgnoreCase)) {
				return WearableType.Leather;
			}

			return WearableType.Plate;
		}

		private static void HandleLayer(ConvertedDef d, PropsLine line) {
			ConvertedItemDef def = (ConvertedItemDef) d;
			def.MakeEquippable();
			def.layerSet = true;
			def.layer = TryNormalizeNumber(line.Value);
			def.Set("layer", def.layer, line.Comment);
			//return def.layer;
		}
	}
}