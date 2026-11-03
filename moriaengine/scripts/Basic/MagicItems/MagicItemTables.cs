using System;
using System.Collections.Generic;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1017 - magic_defs.scp: what each property is called, says and does, and the roll tables.</summary>
	public static class MagicItemTables {
		public const int Count = 150;

		/// <summary>mifdef_prefname / mifdef_sufname, skills 0-49 then the others.</summary>
		private static readonly Dictionary<int, string[]> names = new Dictionary<int, string[]> {
			{ 0, new[] { "Alchemists", "of Alchemist" } }, { 1, new[] { "Scholars", "of Scholar" } }, { 2, new[] { "Scholars", "of Scholar" } },
			{ 3, new[] { "Identifiers", "of Identifier" } }, { 4, new[] { "Crafts", "of Craft" } }, { 5, new[] { "Shieldfighters", "of Shieldfighter" } },
			{ 6, new[] { "Beggars", "of Beggar" } }, { 7, new[] { "Blacksmiths", "of Blacksmith" } }, { 8, new[] { "Bowyers", "of Bowyer" } },
			{ 9, new[] { "Peacemakers", "of Peacemaker" } }, { 10, new[] { "Campers", "of Camper" } }, { 11, new[] { "Carpenters", "of Carpenter" } },
			{ 12, new[] { "Cartographers", "of Cartographer" } }, { 13, new[] { "Chefs", "of Chef" } }, { 14, new[] { "Detectives", "of Detective" } },
			{ 15, new[] { "Bards", "of Bard" } }, { 16, new[] { "Scholars", "of Scholar" } }, { 17, new[] { "Healers", "of Healer" } },
			{ 18, new[] { "Fishers", "of Fisher" } }, { 19, new[] { "Scholars", "of Scholar" } }, { 20, new[] { "Shepherds", "of Shepherd" } },
			{ 21, new[] { "Rogues", "of Rogue" } }, { 22, new[] { "Bards", "of Bard" } }, { 23, new[] { "Scribes", "of Scribe" } },
			{ 24, new[] { "Locksmiths", "of Locksmith" } }, { 25, new[] { "Mages", "of Mage" } }, { 26, new[] { "Resistors", "of Resistor" } },
			{ 27, new[] { "Warriors", "of Warrior" } }, { 28, new[] { "Pickpockets", "of Pickpocket" } }, { 29, new[] { "Bards", "of Bard" } },
			{ 30, new[] { "Assassins", "of Assassin" } }, { 31, new[] { "Archers", "of Archer" } }, { 32, new[] { "Mediums", "of Medium" } },
			{ 33, new[] { "Thiefs", "of Thief" } }, { 34, new[] { "Tailors", "of Tailor" } }, { 35, new[] { "Tamers", "of Tamer" } },
			{ 36, new[] { "Tasters", "of Taster" } }, { 37, new[] { "Tinkers", "of Tinker" } }, { 38, new[] { "Rangers", "of Ranger" } },
			{ 39, new[] { "Veterinarians", "of Veterinarian" } }, { 40, new[] { "Swordsmans", "of Swordsman" } }, { 41, new[] { "Crusher", "of Crushe" } },
			{ 42, new[] { "Fencers", "of Fencer" } }, { 43, new[] { "Wrestlers", "of Wrestler" } }, { 44, new[] { "Lumberjacks", "of Lumberjack" } },
			{ 45, new[] { "Miners", "of Miner" } }, { 46, new[] { "Stoics", "of Stoic" } }, { 47, new[] { "Rogues", "of Rogue" } },
			{ 48, new[] { "Jacks", "of Jack" } }, { 49, new[] { "Necromancers", "of Necromancer" } },
			{ 51, new[] { "Knights", "of Knight" } }, { 52, new[] { "Grandswordsman", "of Grandswordsman" } }, { 53, new[] { "Grandfencers", "of Grandfencer" } },
			{ 54, new[] { "Grandarchers", "of Grandarcher" } }, { 55, new[] { "Grandwrestlers", "of Grandwrestler" } }, { 56, new[] { "Grandcrushers", "of Grandcrusher" } },
			{ 57, new[] { "Warlords", "of Warlord" } }, { 58, new[] { "Grandmages", "of Grandmage" } },
			{ 60, new[] { "Titans", "of Titan" } }, { 61, new[] { "Snakes", "of Snake" } }, { 62, new[] { "Foxes", "of Fox" } }, { 63, new[] { "Whales", "of Whale" } },
			{ 64, new[] { "", "" } }, { 70, new[] { "Regenerating", "of Regeneration" } }, { 71, new[] { "Relaxating", "of Relaxation" } },
			{ 72, new[] { "Meditating", "of Meditation" } }, { 73, new[] { "", "" } },
			{ 101, new[] { "Demonslaying", "of Demonslayer" } }, { 102, new[] { "Dragonslaying", "of Dragonslayer" } }, { 103, new[] { "Ogrekilling", "of Ogrekiller" } },
			{ 104, new[] { "Animalkilling", "of Animalkiller" } }, { 105, new[] { "Mechanicslaying", "of Mechanicslayer" } },
			{ 111, new[] { "Lifeleeching", "of Lifeleech" } }, { 112, new[] { "Manadraining", "of Manadrain" } }, { 113, new[] { "Exhausting", "of Exhaustion" } },
			{ 121, new[] { "Freezing", "of Freezing" } },
			{ 131, new[] { "Physical Reflecting", "of Physical Reflection" } }, { 132, new[] { "Ranged Reflecting", "of Ranged Reflection" } },
			{ 133, new[] { "Magic Reflecting", "of Magic Reflection" } }, { 134, new[] { "Physical Blocking", "of Physical Block" } },
			{ 135, new[] { "Ranged Blocking", "of Ranged Block" } }, { 136, new[] { "Magic Blocking", "of Magic Block" } },
		};

		/// <summary>mifdef_info, after "+value ". Erebor had none for 63, 64 and 73; here they say what they do.</summary>
		private static readonly Dictionary<int, string> infos = new Dictionary<int, string> {
			{ 51, "% ke vsem bojovym skillum" }, { 52, "% ke skillu Twohands a Tactics" }, { 53, "% ke skillu Sharps a Tactics" },
			{ 54, "% ke skillu Archery a Tactics" }, { 55, "% ke skillu Wrestling a Tactics" }, { 56, "% ke skillu Crushing a Tactics" },
			{ 57, "% ke vsem bojovym skillum a Tactics" }, { 58, "% ke skillu Magery a EI" },
			{ 60, "k sile" }, { 61, "k obratnosti" }, { 62, "k inteligenci" }, { 63, "k vitalite" }, { 64, "ke vsem statum" },
			{ 70, "zivotu za 10 vterin" }, { 71, "staminy za 10 vterin" }, { 72, "many za 10 vterin" }, { 73, "zivotu, staminy a many za 10 vterin" },
			{ 101, "% zraneni proti nemrtvym a demonum" }, { 102, "% zraneni proti drakum" }, { 103, "% zraneni proti humanoidnim monstrum" },
			{ 104, "% zraneni proti zviratum vcetne pavouku" }, { 105, "% zraneni proti ocim, gazerum a golemum" },
			{ 111, "% vysatych zivotu kazdym zasahem zbrani" }, { 112, "% vysate many kazdym zasahem zbrani" }, { 113, "% vysate staminy kazdym zasahem zbrani" },
			{ 121, "% uspesnosti paralyzovani cile uderem" },
			{ 131, "% uspesnosti odrazeni uderu zblizka zpet na utocnika" }, { 132, "% uspesnosti odrazeni uderu zdalky zpet na utocnika" },
			{ 133, "% uspesnosti odrazeni negativniho kouzla zpet na utocnika" }, { 134, "% uspesnosti blokace uderu zblizka" },
			{ 135, "% uspesnosti blokace uderu zdalky" }, { 136, "% uspesnosti blokace negativniho kouzla" },
		};

		/// <summary>Erebor's own skill keys, as the click shows them.</summary>
		private static readonly string[] skillKeys = { "Alchemy", "Anatomy", "AnimalLore", "ItemID", "ArmsLore", "Parrying", "Begging",
			"Blacksmithing", "Bowcraft", "Peacemaking", "Camping", "Carpentry", "Cartography", "Cooking", "DetectingHidden", "Enticement",
			"EI", "Healing", "Fishing", "Forensics", "Herding", "Hiding", "Provocation", "Inscription", "LockPicking", "Magery", "Resist",
			"Tactics", "Snooping", "Musicianship", "Poisoning", "Archery", "SpiritSpeak", "Stealing", "Tailoring", "Taming", "TasteID",
			"Tinkering", "Tracking", "Veterinary", "TwoHand", "Crushing", "OneHand", "Wrestling", "Lumberjacking", "Mining", "Meditation",
			"Stealth", "RemoveTrap", "Necromancy" };

		/// <summary>The skills a combination property raises.</summary>
		internal static readonly Dictionary<int, SkillName[]> SkillGroups = new Dictionary<int, SkillName[]> {
			{ 51, new[] { SkillName.Swords, SkillName.Fencing, SkillName.Archery, SkillName.Wrestling, SkillName.Macing } },
			{ 52, new[] { SkillName.Swords, SkillName.Tactics } },
			{ 53, new[] { SkillName.Fencing, SkillName.Tactics } },
			{ 54, new[] { SkillName.Archery, SkillName.Tactics } },
			{ 55, new[] { SkillName.Wrestling, SkillName.Tactics } },
			{ 56, new[] { SkillName.Macing, SkillName.Tactics } },
			{ 57, new[] { SkillName.Swords, SkillName.Fencing, SkillName.Archery, SkillName.Wrestling, SkillName.Macing, SkillName.Tactics } },
			{ 58, new[] { SkillName.Magery, SkillName.EvalInt } },
		};

		/// <summary>mifdef_sort_N_type_M_rare and its max: weight, max.</summary>
		internal static readonly Dictionary<int, int[][]> Rolls = new Dictionary<int, int[][]> {
			{ 1, new[] { new[] { 27, 20, 10 }, new[] { 51, 20, 10 }, new[] { 57, 10, 10 }, new[] { 60, 18, 10 }, new[] { 61, 22, 10 },
				new[] { 101, 17, 30 }, new[] { 102, 20, 30 }, new[] { 103, 12, 30 }, new[] { 104, 12, 30 }, new[] { 105, 12, 30 },
				new[] { 111, 17, 10 }, new[] { 112, 17, 10 }, new[] { 113, 18, 10 }, new[] { 121, 10, 40 } } },
			{ 2, new[] { new[] { 16, 21, 10 }, new[] { 25, 19, 10 }, new[] { 32, 19, 10 }, new[] { 58, 10, 10 }, new[] { 62, 15, 10 } } },
			{ 3, new[] { new[] { 20, 19, 10 }, new[] { 35, 15, 5 }, new[] { 39, 19, 5 } } },
			{ 11, new[] { new[] { 1, 14, 5 }, new[] { 5, 20, 5 }, new[] { 17, 14, 5 }, new[] { 26, 12, 10 }, new[] { 60, 12, 5 }, new[] { 61, 15, 5 },
				new[] { 62, 15, 5 }, new[] { 70, 14, 5 }, new[] { 71, 10, 5 }, new[] { 72, 13, 5 }, new[] { 131, 10, 5 }, new[] { 132, 14, 5 },
				new[] { 133, 12, 5 }, new[] { 134, 18, 10 }, new[] { 135, 22, 10 }, new[] { 136, 20, 10 } } },
			{ 21, new[] { new[] { 9, 35, 5 }, new[] { 15, 35, 5 }, new[] { 22, 35, 5 }, new[] { 25, 35, 5 }, new[] { 32, 35, 5 }, new[] { 35, 35, 5 },
				new[] { 21, 20, 5 }, new[] { 47, 20, 5 }, new[] { 18, 19, 5 }, new[] { 39, 19, 5 },
				new[] { 101, 16, 15 }, new[] { 102, 16, 15 }, new[] { 103, 16, 15 }, new[] { 104, 16, 15 }, new[] { 105, 16, 15 },
				new[] { 27, 15, 5 }, new[] { 31, 15, 5 }, new[] { 40, 15, 5 }, new[] { 41, 15, 5 }, new[] { 42, 15, 5 }, new[] { 43, 15, 5 }, new[] { 61, 15, 5 },
				new[] { 28, 14, 5 }, new[] { 33, 14, 5 }, new[] { 16, 12, 5 }, new[] { 46, 12, 5 }, new[] { 26, 12, 10 },
				new[] { 44, 11, 5 }, new[] { 45, 11, 5 }, new[] { 62, 11, 5 },
				new[] { 51, 10, 5 }, new[] { 60, 10, 5 }, new[] { 70, 10, 5 }, new[] { 71, 10, 5 }, new[] { 72, 10, 5 },
				new[] { 111, 10, 5 }, new[] { 112, 10, 5 }, new[] { 113, 10, 5 } } },
		};

		/// <summary>mif_choosemetal by tier: metal, weight.</summary>
		internal static readonly int[][] Metals = {
			new[] { 1, 500, 2, 400, 5, 200, 6, 100, 7, 20, 8, 10, 9, 1 },
			new[] { 5, 300, 6, 200, 7, 80, 8, 30, 9, 1 },
			new[] { 6, 200, 7, 80, 8, 30, 9, 1 },
		};
		internal static readonly string[] MetalNames = { "", "copper", "iron", "silver", "golden", "verite", "valorite", "obsidian", "adamantinum", "mithril" };

		internal static readonly string[][] Weapons = {
			new[] { "i_kryss_M", "i_cutlass_M", "i_scimitar_M", "i_sword_broad_M", "i_war_fork_M", "i_sword_long_M", "i_sword_viking_M", "i_katana_M", "i_spear_short_M" },
			new[] { "i_axe_M", "i_axe_battle_M", "i_pitchfork_M", "i_axe_exec_M", "i_axe_double_M", "i_axe_two_hand_M", "i_spear_M", "i_axe_war_M",
				"i_axe_battle_large_M", "i_bardiche_M", "i_halberd_M" },
			new[] { "i_mace_M", "i_mace_war_M", "i_hammer_pick_M", "i_hammer_war_M", "i_maul_M" },
			new[] { "i_staff_quarter_M", "i_shepherds_crook_M", "i_staff_black_M", "i_staff_gnarled_M" },
			new[] { "i_crossbow_M", "i_crossbow_heavy_M", "i_bow_M" },
		};

		internal static readonly string[] PlateHelms = { "i_M_PLATE_HELM", "i_M_CLOSED_HELM", "i_M_BASCINET", "i_M_NOSE_HELM" };

		internal static string Name(int n, bool prefix) {
			return names.TryGetValue(n, out string[] pair) ? pair[prefix ? 0 : 1] : "";
		}

		/// <summary>mif_skillinfo / mif_othersinfo: one line of a click.</summary>
		internal static string Info(int n, int value) {
			if (n < 50) {
				return "+" + value + "% ke skillu " + skillKeys[n];
			}
			return "+" + value + " " + (infos.TryGetValue(n, out string text) ? text : "");
		}

		/// <summary>{v1 w1 v2 w2 ...}.</summary>
		internal static int Weighted(int[] pairs) {
			int total = 0;
			for (int i = 1; i < pairs.Length; i += 2) {
				total += pairs[i];
			}
			int roll = Globals.dice.Next(total);
			for (int i = 0; i < pairs.Length; i += 2) {
				roll -= pairs[i + 1];
				if (roll < 0) {
					return pairs[i];
				}
			}
			return pairs[pairs.Length - 2];
		}
	}
}
