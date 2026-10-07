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
using System.Text;
using System.Text.RegularExpressions;
using SteamEngine.Common;
using SteamEngine.CompiledScripts;

namespace SteamEngine.Converter {
	public class ConvertedItemDef : ConvertedThingDef {
		public static Dictionary<string, ConvertedThingDef> itemsByDefname = new Dictionary<string, ConvertedThingDef>(StringComparer.OrdinalIgnoreCase);
		public static Dictionary<int, ConvertedThingDef> itemsByModel = new Dictionary<int, ConvertedThingDef>();

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

		private static LineImplTask[] firstStageImpl = {
				new LineImplTask("type", HandleType), 
				new LineImplTask("dupelist", WriteAsComment), 
				new LineImplTask("weight", MayBeInt_IgnorePoint), 
				new LineImplTask("resources", HandleResourcesList),
				new LineImplTask("resources2", HandleResourcesList),
				new LineImplTask("skillmake", HandleResourcesList),
				new LineImplTask("skillmake2", HandleResourcesList),
//TODO
				new LineImplTask("flip", WriteAsComment),
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

			this.firstStageImplementations.Add(firstStageImpl);
			this.secondStageImplementations.Add(secondStageImpl);
			this.thirdStageImplementations.Add(thirdStageImpl);

			this.headerType = "ItemDef";
		}

		//resources list may need some number (counts) corrections
		private static void HandleResourcesList(ConvertedDef def, PropsLine line) {
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
					corrected.Append("1 ").Append(split[0]).Append(", "); //number 1 prepended
					commentary = "some resources were fixed by converter";
				} else {
					def.Warning(line.Line, "Unknown resource string '"+singleRes+"'");
					corrected.Append(singleRes).Append(", ");
				}
			}
			corrected.Length -= 2; //remove the last ", "

			def.Set(line.Name, corrected.ToString(), commentary);
			//return line.Value;
		}

		private static void HandleType(ConvertedDef d, PropsLine line) {
			ConvertedItemDef def = (ConvertedItemDef) d;
			def.Set(line);

			string args = line.Value.ToLowerInvariant();
			def.type = args;

			switch (args) {
				case "t_container":
				case "t_container_locked":
				case "t_eq_vendor_box":
				case "t_eq_bank_box":
					def.headerType = "ContainerDef";
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
					def.headerType = "MultiItemDef";
					def.DontDump();	//for now
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
				case "T_potion_shrink":
				case "t_speedpotions":
				case "t_potion_bomba":
					def.headerType = "PotionDef";
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
				if (sum != 0) {
					sum = sum / n;
					if (def.isWearable) {
						sum = Math.Round(sum);
					}
					value = sum.ToString();
				} else {
					value = line.Value;
				}

				if (def.isWeapon) {
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
			if (def.headerType.Equals("PotionDef", StringComparison.OrdinalIgnoreCase)) {
				def.Set("EmptyFlask", line.Value, "converted from 'TDATA1'");
			} else {
				WriteAsComment(def, line);
			}
		}

		private static void HandleTData2(ConvertedDef def, PropsLine line) {
			WriteAsComment(def, line);
		}

		private static void HandleTData3(ConvertedDef def, PropsLine line) {
			WriteAsComment(def, line);
		}

		public static void SecondStageFinished() {
			HashSet<ConvertedItemDef> itemDefSet = new HashSet<ConvertedItemDef>();
			foreach (ConvertedItemDef def in itemsByDefname.Values) {
				itemDefSet.Add(def);
			}

			foreach (ConvertedItemDef def in itemDefSet) {
				ConvertedItemDef baseDef = def.modelDef as ConvertedItemDef;
				if (baseDef != null) {
					if (baseDef.isEquippable) {
						def.MakeEquippable();

						if ((baseDef.isTwoHanded) && (!def.twoHandedSet)) {
							def.Set("TwoHanded", "true", "guessed from base def by Converter");
						}
						if ((baseDef.layerSet) && (!def.layerSet)) {
							def.Set("Layer", baseDef.layer, "guessed from base def by Converter");
							def.layerSet = true;
							def.layer = baseDef.layer;
						}
					}

					if ((baseDef.type != null) && (def.type == null)) {
						HandleType(def, new PropsLine("type", baseDef.type, -1, "guessed from base def by Converter"));
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

		public override void ThirdStage() {
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
						if (prettyDefName.Contains("_axe_") || prettyDefName.EndsWith("_axe")) {
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
						if (this.isTwoHanded) {
							weaponType = WeaponType.TwoHandSpike;
						} else {
							weaponType = WeaponType.OneHandSpike;
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