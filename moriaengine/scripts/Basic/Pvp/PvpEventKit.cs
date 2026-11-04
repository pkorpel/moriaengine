using System.Collections.Generic;
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1116 - Erebor's PvP event kit (funkce.scp's f_varGate_pvp_equip, f_varGate_pvp_clearequip and their helpers),
	/// for a GM's variable gate: a function line "f_varGate_pvp_equip(iron)" (any metal with an i_&lt;metal&gt;_platemail: iron,
	/// copper, verite, valorite, obsidian, adamantinum, mithril) and "f_varGate_pvp_clearequip".
	/// Equip: what's worn goes to the pack, and all of the pack that isn't fixed goes to the bank in a red "Vybava z turnaje"
	/// backpack (f_equip2bank); then 20 empty bottles, the PvP spellbook, 40 bandages, 15 cooked fish steaks, a llama
	/// (f_pvp_give_basic_items), and the profession's armor and weapons of that metal, each named PVPEVENT
	/// (f_profession_giveArmor/giveWeapon), with the ammunition of thieves, craftsmen and rangers. Nothing reads the PVPEVENT
	/// name back, as on Erebor: it's only a marker.
	/// Clear: what's worn goes to the pack, then all of the pack that isn't fixed is deleted, nothing kept.
	/// Fixed from Erebor: worn things that can't be moved (a badge of office, a Nazgul's ring) stay on; Erebor's bounce took
	/// them off into the pack, where the kit then left them.</summary>
	public static class PvpEventKit {
		public const string Marker = "PVPEVENT";
		public const string BankBagName = "Vybava z turnaje";
		public const int BankBagColor = 0x012B;

		/// <summary>eqlayerspvp: what a PvP kit takes off. Not the mount, pack, bank, hair, beard or light.</summary>
		private static readonly LayerNames[] KitLayers = {
			LayerNames.Hand1, LayerNames.Hand2, LayerNames.Shoes, LayerNames.Pants, LayerNames.Shirt, LayerNames.Helmet,
			LayerNames.Gloves, LayerNames.Ring, LayerNames.Collar, LayerNames.HalfApron, LayerNames.Chest, LayerNames.Bracelet,
			LayerNames.Tunic, LayerNames.Earrings, LayerNames.Arms, LayerNames.Cape, LayerNames.Robe, LayerNames.Skirt,
			LayerNames.Leggins
		};

		private static readonly string[] Plate = { "i_{0}_platemail", "i_{0}_platemail_leggings", "i_{0}_platemail_gauntlets", "i_{0}_plate_helm", "i_{0}_platemail_gorget", "i_{0}_platemail_arms", "i_{0}_heater_shield" };
		private static readonly string[] Chain = { "i_{0}_chainmail_tunic", "i_{0}_chainmail_leggings", "i_{0}_chainmail_gloves", "i_{0}_chainmail_coif", "i_{0}_chainmail_gorget", "i_{0}_chainmail_sleeves", "i_{0}_metal_shield" };
		private static readonly string[] Studded = { "i_studded_sleeves_{0}", "i_studded_cap_{0}", "i_studded_gloves_{0}", "i_studded_gorget_{0}", "i_studded_leggings_{0}", "i_studded_tunic_{0}" };
		private static readonly string[] Bone = { "i_bone_chest_{0}", "i_bone_gloves_{0}", "i_bone_helmet_{0}", "i_bone_arms_{0}", "i_bone_leggings_{0}" };
		private static readonly string[] Ring = { "i_{0}_ringmail_tunic", "i_{0}_ringmail_gloves", "i_{0}_ringmail_sleeves", "i_{0}_ringmail_leggings", "i_{0}_ringmail_helm", "i_{0}_ringmail_gorget" };

		/// <summary>The varGate_pvp_equip line's metal, as the gate passes it (after the gate) or a GM types it.</summary>
		private static string Argument(ScriptArgs sa) {
			if (sa != null) {
				foreach (object arg in sa.Argv) {
					if (arg is string word) {
						return word.Trim().Trim('"').ToLowerInvariant();
					}
				}
			}
			return "";
		}

		[SteamFunction]
		public static bool f_varGate_pvp_equip(Character self, ScriptArgs sa) {
			string metal = Argument(sa);
			if (ThingDef.GetByDefname("i_" + metal + "_platemail") == null) {
				self.RedMessage("Nelze volat funkci s timto parametrem: " + metal + ".");
				return true;
			}
			Equip(self, metal);
			return false;
		}

		[SteamFunction]
		public static bool f_varGate_pvp_clearequip(Character self) {
			Clear(self);
			return false;
		}

		public static void Equip(Character ch, string metal) {
			ToBank(ch);
			Give(ch, "i_bottle_empty", 20, false);
			Give(ch, "i_spellbook_pvp", 1, false);
			Give(ch, "i_bandage", 40, false);
			Give(ch, "i_fish_cut_cooked", 15, false);
			Give(ch, "i_pet_llama", 1, false);
			foreach (string format in ArmorOf(ch)) {
				Give(ch, string.Format(format, metal), 1, true);
			}
			GiveWeapons(ch, metal);
		}

		public static void Clear(Character ch) {
			TakeOff(ch);
			foreach (Item item in Loose(ch)) {
				item.Delete();
			}
		}

		/// <summary>f_equip2bank.</summary>
		private static void ToBank(Character ch) {
			TakeOff(ch);
			List<Item> loose = Loose(ch);
			if (loose.Count == 0) {
				return;
			}
			Item bag = (Item) ThingDef.GetByDefname("i_backpack").Create(ch.Bank);
			bag.Name = BankBagName;
			bag.Color = BankBagColor;
			foreach (Item item in loose) {
				item.Cont = bag;
			}
		}

		private static void TakeOff(Character ch) {
			foreach (LayerNames layer in KitLayers) {
				if ((ch.FindLayer(layer) is Item worn) && !worn.Flag_NonMovable) {
					worn.Cont = ch.Backpack;
				}
			}
		}

		/// <summary>The pack's top level, what isn't fixed (attr_move_never).</summary>
		private static List<Item> Loose(Character ch) {
			List<Item> loose = new List<Item>();
			foreach (AbstractItem abstractItem in ch.Backpack) {
				if ((abstractItem is Item item) && !item.Flag_NonMovable) {
					loose.Add(item);
				}
			}
			return loose;
		}

		/// <summary>giveitem, or f_pvp_giveitem (named PVPEVENT) when marked.</summary>
		private static void Give(Character ch, string defname, int amount, bool marked) {
			ItemDef def = ThingDef.GetByDefname(defname) as ItemDef;
			if (def == null) {
				ch.RedMessage("Tento predmet neexistuje: " + defname);
				return;
			}
			Item item = (Item) def.Create(ch.Backpack);
			if (amount > 1) {
				item.Amount = amount;
			}
			if (marked) {
				item.Name = Marker;
			}
		}

		private static bool Is(Character ch, ProfessionDef profession) {
			return (ch is Player player) && Professions.HasProfession(player, profession);
		}

		/// <summary>f_profession_giveArmor.</summary>
		private static string[] ArmorOf(Character ch) {
			if (Is(ch, Professions.Warrior) || Is(ch, Professions.Priest) || Is(ch, Professions.Craftsman)) {
				return Plate;
			}
			if (Is(ch, Professions.Thief)) {
				return Chain;
			}
			if (Is(ch, Professions.Mage) || Is(ch, Professions.Mystic)) {
				return Studded;
			}
			if (Is(ch, Professions.Necromant)) {
				return Bone;
			}
			if (Is(ch, Professions.Shaman) || Is(ch, Professions.Ranger)) {
				return Ring;
			}
			return new string[0];
		}

		/// <summary>f_profession_giveWeapon.</summary>
		private static void GiveWeapons(Character ch, string metal) {
			string[] weapons;
			if (Is(ch, Professions.Warrior)) {
				weapons = new[] { "i_katana_{0}", "i_halberd_{0}", "i_axe_double_{0}", "i_hammer_war_{0}" };
			} else if (Is(ch, Professions.Thief)) {
				weapons = new[] { "i_kryss_{0}", "i_scimitar_{0}", "i_cutlass_{0}" };
			} else if (Is(ch, Professions.Priest)) {
				weapons = new[] { "i_hammer_pick_{0}", "i_mace_{0}" };
			} else if (Is(ch, Professions.Craftsman)) {
				weapons = new[] { "i_crossbow_heavy_{0}", "i_katana_{0}" };
			} else if (Is(ch, Professions.Ranger)) {
				weapons = new[] { "i_bow_{0}", "i_crossbow_{0}", "i_crossbow_heavy_{0}" };
			} else if (Is(ch, Professions.Mage) || Is(ch, Professions.Necromant) || Is(ch, Professions.Mystic) || Is(ch, Professions.Shaman)) {
				weapons = new[] { "i_staff_gnarled_{0}" };
			} else {
				weapons = new string[0];
			}
			foreach (string format in weapons) {
				Give(ch, string.Format(format, metal), 1, true);
			}
			if (Is(ch, Professions.Thief)) {
				Give(ch, "i_kudla", 40, false);
			} else if (Is(ch, Professions.Craftsman)) {
				Give(ch, "i_xbolt_piercing_verite", 50, false);
			} else if (Is(ch, Professions.Ranger)) {
				Give(ch, "i_xbolt_piercing_verite", 20, false);
				Give(ch, "i_arrow_piercing_verite", 100, false);
				Give(ch, "i_bolt_jagged_verite", 20, false);
			}
		}
	}
}
