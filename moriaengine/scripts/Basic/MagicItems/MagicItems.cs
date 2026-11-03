using System;
using System.Collections.Generic;
using System.Text;
using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {

	[ViewableClass]
	public partial class MagicBagDef {
	}

	/// <summary>#1017 - mit_main: a double click unpacks the bag into one unidentified magic item.</summary>
	[ViewableClass]
	public partial class MagicBag {
		public override void On_Create() {
			base.On_Create();
			MagicItemGmTools.CountBag(this); //#1070 - padnute_*
		}

		public override void On_DClick(AbstractCharacter from) {
			if (from is Character ch) {
				MagicItems.Unpack(this, ch);
			}
		}
	}

	/// <summary>#1017 - Erebor's magic items, magicITEMS.scp and magic_defs.scp. A property N with a value lives in the item's
	/// tag imi_value_N; an unidentified item has them but does nothing until a Vetesnik reveals it. Worn, an identified one
	/// adds its skills and stats, regeneration and the tags combat reads (slayers, leech, paralysis, reflex and block).</summary>
	public static class MagicItems {
		internal const string UnidentifiedName = "Magicky predmet";
		private static readonly TagKey[] valueTKs = new TagKey[MagicItemTables.Count];
		private static readonly TagKey unidentifiedTK = TagKey.Acquire("unidentified");
		private static readonly TagKey finishedTK = TagKey.Acquire("_magicItem_");
		internal static readonly TagKey MagicCountTK = TagKey.Acquire("_magic_");
		private static readonly Dictionary<int, TagKey> wearerTKs = new Dictionary<int, TagKey> {
			{ 101, TagKey.Acquire("_daemonbonus_") }, { 102, TagKey.Acquire("_dragonbonus_") }, { 103, TagKey.Acquire("_humanbonus_") },
			{ 104, TagKey.Acquire("_spiderbonus_") }, { 105, TagKey.Acquire("_eyebonus_") },
			{ 111, TagKey.Acquire("_mi_hitsleech_") }, { 112, TagKey.Acquire("_mi_manaleech_") }, { 113, TagKey.Acquire("_mi_stamleech_") },
			{ 121, TagKey.Acquire("_paralising_") },
			{ 131, TagKey.Acquire("_mi_meleereflex_") }, { 132, TagKey.Acquire("_mi_rangedreflex_") }, { 133, TagKey.Acquire("_mi_spellreflex_") },
			{ 134, TagKey.Acquire("_mi_meleeblock_") }, { 135, TagKey.Acquire("_mi_rangedblock_") }, { 136, TagKey.Acquire("_mi_spellblock_") },
		};

		static MagicItems() {
			for (int n = 0; n < MagicItemTables.Count; n++) {
				valueTKs[n] = TagKey.Acquire("imi_value_" + n);
			}
		}

		public static int Value(Item item, int n) {
			return ((n >= 0) && (n < MagicItemTables.Count) && (item.GetTag(valueTKs[n]) is int value)) ? value : 0;
		}

		public static void SetValue(Item item, int n, int value) {
			if (value != 0) {
				item.SetTag(valueTKs[n], value);
			} else {
				item.RemoveTag(valueTKs[n]);
			}
		}

		/// <summary>The properties in Erebor's order: skills ascending, then the others ascending.</summary>
		public static List<int> Properties(Item item) {
			List<int> found = new List<int>();
			for (int n = 0; n < MagicItemTables.Count; n++) {
				if (Value(item, n) != 0) {
					found.Add(n);
				}
			}
			return found;
		}

		public static bool IsUnidentified(Item item) {
			return item.GetTag(unidentifiedTK) != null;
		}

		/// <summary>tmi_info: an identified magic item.</summary>
		public static bool IsMagic(Item item) {
			return item.GetTag(finishedTK) != null;
		}

		/// <summary>The wearer's sum of a combat property (slayers, leech, paralysis, reflex, block).</summary>
		public static int WearerValue(Character ch, int n) {
			return (wearerTKs.TryGetValue(n, out TagKey key) && (ch.GetTag(key) is int value)) ? value : 0;
		}

		/// <summary>ismagic: something magic is worn.</summary>
		public static bool IsUnderMagic(Character ch) {
			return (ch.GetTag(MagicCountTK) is int count) && (count > 0);
		}

		#region generation
		/// <summary>f_mif_create: a new item of the bag's tier, rolled once, then 5 in 6 for each further roll.</summary>
		internal static Item Unpack(MagicBag bag, Character opener) {
			MagicBagDef def = (MagicBagDef) bag.Def;
			ItemDef chosen = ChooseItem(def.Tier);
			Thing top = bag.TopObj();
			Item item;
			if (top is Character owner) {
				item = (Item) chosen.Create(owner.Backpack);
			} else {
				item = (Item) chosen.Create(top.X, top.Y, top.Z, top.M);
			}
			for (int i = 0; i < Math.Max(def.Rolls, 1); i++) {
				if ((i == 0) || (Globals.dice.Next(0, 6) != 0)) {
					RandType(item);
				}
			}
			item.SetTag(unidentifiedTK, 1);
			item.Name = UnidentifiedName;
			Logger.WriteInfo(true, opener.Name + " rozbaluje magicky vacek (" + bag.Uid + ") a vznika magicky predmet " + chosen.PrettyDefname + " s uid: " + item.Uid);
			bag.Delete();
			return item;
		}

		/// <summary>mif_chooseitem: weapon 40%, armor 40%, other 20%. A name the archive lacks is rolled again.</summary>
		internal static ItemDef ChooseItem(int tier) {
			string metal = MagicItemTables.MetalNames[MagicItemTables.Weighted(MagicItemTables.Metals[Math.Max(0, Math.Min(tier, 2))])];
			for (int attempt = 0; attempt < 20; attempt++) {
				int group = MagicItemTables.Weighted(new[] { 0, 2, 1, 2, 2, 1 });
				string name = (group == 0) ? Weapon() : (group == 1) ? Armor() : Other();
				if (ThingDef.GetByDefname(System.Text.RegularExpressions.Regex.Replace(name, "(?<=_)M(?=_|$)", metal)) is ItemDef def) {
					return def;
				}
			}
			return (ItemDef) ThingDef.GetByDefname("i_ring_gold");
		}

		private static string Pick(params string[] names) {
			return names[Globals.dice.Next(names.Length)];
		}

		private static string Weapon() {
			string[] kind = MagicItemTables.Weapons[Globals.dice.Next(MagicItemTables.Weapons.Length)];
			return kind[Globals.dice.Next(kind.Length)];
		}

		private static string Armor() {
			switch (MagicItemTables.Weighted(new[] { 0, 23, 1, 23, 2, 18, 3, 18, 4, 18 })) {
				case 0:
					switch (Globals.dice.Next(7)) {
						case 5:
							return (Globals.dice.Next(100) < 75) ? "i_M_PLATEMAIL" : "i_M_FEMALE_PLATEMAIL";
						case 6:
							return Pick(MagicItemTables.PlateHelms);
						default:
							return Pick("i_M_HEATER_SHIELD", "i_M_PLATEMAIL_ARMS", "i_M_PLATEMAIL_leggings", "i_M_PLATEMAIL_GORGET", "i_M_PLATEMAIL_GAUNTLETS");
					}
				case 1:
					return Pick("i_M_METAL_SHIELD", "i_M_CHAINMAIL_COIF", "i_M_CHAINMAIL_GORGET", "i_M_CHAINMAIL_SLEEVES", "i_M_CHAINMAIL_GLOVES",
						"i_M_CHAINMAIL_LEGGINGS", "i_M_CHAINMAIL_TUNIC");
				case 2:
					return Pick("i_M_kite_wood_shield", "i_M_RINGMAIL_HELM", "i_M_RINGMAIL_GORGET", "i_M_RINGMAIL_SLEEVES", "i_M_RINGMAIL_GLOVES",
						"i_M_RINGMAIL_LEGGINGS", "i_M_RINGMAIL_TUNIC");
				case 3:
					if (Globals.dice.Next(6) == 5) {
						return Pick("i_studded_tunic_M", "i_studded_tunic_M", "i_armor_female_bustier_studded_M", "i_armor_female_studded_M");
					}
					return Pick("i_studded_cap_M", "i_studded_sleeves_M", "i_studded_gloves_M", "i_studded_gorget_M", "i_studded_leggings_M");
				default:
					return Pick("i_bone_arms_M", "i_bone_chest_M", "i_bone_gloves_M", "i_bone_helmet_M", "i_bone_leggings_M");
			}
		}

		/// <summary>mif_chooseother: no metal in any of these.</summary>
		private static string Other() {
			string metal = (Globals.dice.Next(100) < 60) ? "gold" : "silver";
			switch (Globals.dice.Next(8)) {
				case 0:
					return Pick("i_robe", "i_robe", "i_dress_plain", "i_dress_full");
				case 1:
					return "i_earrings_" + metal;
				case 2:
					return "i_bracelet_" + metal;
				case 3:
					return "i_ring_" + metal;
				case 4:
					return "i_necklace_" + metal;
				case 5:
					return Pick("i_hat_wizards", "i_hat_feather", "i_hat_straw", "i_hat_floppy", "i_skull_cap");
				case 6:
					return "i_cape";
				default:
					return "i_sash";
			}
		}

		/// <summary>The roll table for an item: weapons 1 (a mage's staff 3 in 4 times 2, a crook 3), armor and shields 11,
		/// the rest 21.</summary>
		internal static int Sort(Item item) {
			string type = (item.Type == null) ? "" : item.Type.Defname.ToLowerInvariant();
			if (item is Weapon) {
				if ((item is ColoredStaff) && (type != "t_weapon_mace_crook") && (Globals.dice.Next(4) != 0)) {
					return 2;
				}
				return (type == "t_weapon_mace_crook") ? 3 : 1;
			}
			if ((type == "t_armor") || (type == "t_armor_leather") || (type == "t_shield")) {
				return 11;
			}
			return 21;
		}

		/// <summary>mif_randtype: one property by weight, a value from 1 to its max. Rolled twice, the second overwrites.</summary>
		internal static void RandType(Item item) {
			int[][] table = MagicItemTables.Rolls[Sort(item)];
			int total = 0;
			foreach (int[] row in table) {
				total += row[1];
			}
			int roll = Globals.dice.Next(total);
			foreach (int[] row in table) {
				roll -= row[1];
				if (roll < 0) {
					SetValue(item, row[0], Globals.dice.Next(1, row[2] + 1));
					return;
				}
			}
		}
		#endregion

		#region identification
		/// <summary>mif_finishitem: identified, named, working when worn.</summary>
		public static void Finish(Item item) {
			item.RemoveTag(unidentifiedTK);
			item.SetTag(finishedTK, 1);
			item.Name = BuildName(item);
		}

		/// <summary>mif_setname: the graphic's plain name, each property a prefix or a suffix at even odds.</summary>
		internal static string BuildName(Item item) {
			ThingDef plain = ThingDef.GetByModel(item.Model);
			string name = (plain != null) ? plain.Name : item.Def.Name;
			name = string.IsNullOrEmpty(name) ? " " : char.ToUpperInvariant(name[0]) + name.Substring(1);
			foreach (int n in Properties(item)) {
				if (Globals.dice.Next(2) == 0) {
					name = name + " " + MagicItemTables.Name(n, false) + " and";
				} else {
					name = "and " + MagicItemTables.Name(n, true) + " " + name;
				}
			}
			if (name.StartsWith("and ", StringComparison.OrdinalIgnoreCase)) {
				name = name.Substring(4);
			}
			if (name.EndsWith(" and", StringComparison.Ordinal)) {
				name = name.Substring(0, name.Length - 4);
			}
			return name;
		}

		/// <summary>tmi_info's @UserClick, for its holder or a GM.</summary>
		public static List<string> InfoLines(Item item) {
			List<string> lines = new List<string>();
			foreach (int n in Properties(item)) {
				lines.Add(MagicItemTables.Info(n, Value(item, n)));
			}
			return lines;
		}

		internal static void OnClick(Item item, AbstractCharacter clicker) {
			if (IsMagic(item) && (clicker is Character ch) && ((item.TopObj() == ch) || ch.IsGM)) {
				foreach (string line in InfoLines(item)) {
					item.ClickMessage(ch, line);
				}
			}
		}
		#endregion

		#region wearing
		/// <summary>e_character's @ItemEquip: an unidentified item can't be worn.</summary>
		internal static bool RefusesEquip(Item item, Character ch) {
			if (IsUnidentified(item)) {
				ch.RedMessage("Toto musis nejdrive identifikovat.");
				return true;
			}
			return false;
		}

		internal static void OnEquip(Character ch, Item item) {
			Apply(ch, item, 1);
		}

		internal static void OnUnequip(Character ch, Item item) {
			Apply(ch, item, -1);
		}

		/// <summary>tmi_skills, tmi_51-64, tmi_70-73 and the wearer's tags. Skills go on as a modifier, so nothing drifts;
		/// stats on the base values as on Erebor.</summary>
		private static void Apply(Character ch, Item item, int sign) {
			if (!IsMagic(item)) {
				return;
			}
			AddTag(ch, MagicCountTK, sign);
			foreach (int n in Properties(item)) {
				ApplyProperty(ch, n, Value(item, n) * sign);
			}
		}

		/// <summary>#925 - what a magic mount gives its rider, on as a worn magic item's.</summary>
		internal static void ApplyValues(Character ch, IEnumerable<KeyValuePair<int, int>> values, int sign) {
			AddTag(ch, MagicCountTK, sign);
			foreach (KeyValuePair<int, int> value in values) {
				ApplyProperty(ch, value.Key, value.Value * sign);
			}
		}

		private static void ApplyProperty(Character ch, int n, int v) {

			if (n < 50) {
				ch.ModifySkillValue(n, v * 10);
			} else if (MagicItemTables.SkillGroups.TryGetValue(n, out SkillName[] skills)) {
				foreach (SkillName skill in skills) {
					ch.ModifySkillValue(skill, v * 10);
				}
			} else {
				switch (n) {
					case 60:
						ch.Str = (short) (ch.Str + v);
						break;
					case 61:
						ch.Dex = (short) (ch.Dex + v);
						break;
					case 62:
						ch.Int = (short) (ch.Int + v);
						break;
					case 63:
						if (ch is Player whale) { //vitality is a player's
							whale.Vit = (short) (whale.Vit + v);
						}
						break;
					case 64:
						ch.Str = (short) (ch.Str + v);
						ch.Dex = (short) (ch.Dex + v);
						ch.Int = (short) (ch.Int + v);
						break;
					case 70:
						ch.HitsRegenSpeed += v / 10.0;
						break;
					case 71:
						ch.StamRegenSpeed += v / 10.0;
						break;
					case 72:
						ch.ManaRegenSpeed += v / 10.0;
						break;
					case 73:
						ch.HitsRegenSpeed += v / 10.0;
						ch.StamRegenSpeed += v / 10.0;
						ch.ManaRegenSpeed += v / 10.0;
						break;
					default:
						if (wearerTKs.TryGetValue(n, out TagKey key)) {
							AddTag(ch, key, v);
						}
						break;
				}
			}
		}

		private static void AddTag(Character ch, TagKey key, int delta) {
			int now = ((ch.GetTag(key) is int value) ? value : 0) + delta;
			if (now != 0) {
				ch.SetTag(key, now);
			} else {
				ch.RemoveTag(key);
			}
		}
		#endregion

		/// <summary>A GM's helper and the tests': an identified item of a def with the given properties.</summary>
		[SteamFunction]
		public static Item MagicItem(Character self, ScriptArgs sa) {
			string[] args = (sa == null) ? new string[0] : sa.Args.Trim('"').Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
			if ((args.Length < 3) || !(ThingDef.GetByDefname(args[0]) is ItemDef def)) {
				self.SysMessage("MagicItem defname property value [property value ...]");
				return null;
			}
			Item item = (Item) def.Create(self.Backpack);
			for (int i = 1; i + 1 < args.Length; i += 2) {
				SetValue(item, int.Parse(args[i], System.Globalization.CultureInfo.InvariantCulture), int.Parse(args[i + 1], System.Globalization.CultureInfo.InvariantCulture));
			}
			Finish(item);
			return item;
		}
	}
}
