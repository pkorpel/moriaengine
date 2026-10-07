/*
	This program is free software); you can redistribute it and/or modify
	it under the terms of the GNU General Public License as published by
	the Free Software Foundation); either version 2 of the License, or
	(at your option) any later version.

	This program is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY); without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
	GNU General Public License for more details.

	You should have received a copy of the GNU General Public License
	along with this program); if not, write to the Free Software
	Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
	Or visit http://www.gnu.org/copyleft/gpl.html
*/

using System;
using System.Collections.Generic;

namespace SteamEngine.CompiledScripts {

	public static class ItemTypeNames {
		private static readonly Dictionary<string, string> typeNames = CreateDict();

		static Dictionary<string, string> CreateDict() {
			var dict = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);

			//fill the dictionary with names of various item types
			dict.Add("00", "Normal");
			dict.Add("t_normal", "Normal");
			dict.Add("t_container", "Kontejner");
			dict.Add("t_container_locked", "Zamèenı Kontejner");
			dict.Add("t_door", "Dveøe");
			dict.Add("t_door_locked", "Zamèené dveøe");
			dict.Add("t_key", "Klíè");
			dict.Add("t_light_lit", "Svítící svìtlo");
			dict.Add("t_light_out", "Zhasnuté svìtlo");
			dict.Add("t_food", "Jídlo");
			dict.Add("t_food_raw", "Syrové Jídlo");
			dict.Add("t_armor", "Brnìní");
			dict.Add("t_weapon_mace_smith", "Tupá Zbraò(nástroj?)");
			dict.Add("t_weapon_mace_sharp", "Tupá Zbraò");
			dict.Add("t_weapon_sword", "Ostra Zbraò (tìká)");
			dict.Add("t_weapon_fence", "Ostrá Zbraò (lehká)");
			dict.Add("t_weapon_bow", "Luk");
			dict.Add("t_wand", "Hùlka");
			dict.Add("t_telepad", "Telepad");
			dict.Add("t_switch", "Pøepínaè");
			dict.Add("t_book", "Kniha");
			dict.Add("t_rune", "Runa");
			dict.Add("t_booze", "Alkohol");
			dict.Add("t_potion", "Lektvar");
			dict.Add("t_allpotions", "Lektvar");
			dict.Add("t_speedpotions", "Lektvar");
			dict.Add("t_potion_bomba", "Bomba");
			dict.Add("t_fire", "Oheò");
			dict.Add("t_clock", "Hodiny");
			dict.Add("t_trap", "Past");
			dict.Add("t_trap_active", "Aktivní past");
			dict.Add("t_musical", "Hudební nástroj");
			dict.Add("t_spell", "Kouzlo");
			dict.Add("t_gem", "Drahokam");
			dict.Add("t_water", "Voda");
			dict.Add("t_clothing", "Obleèení");
			dict.Add("t_scroll", "	Svitek kouzla");
			dict.Add("t_carpentry", "Truhláøskı nástroj");
			dict.Add("t_spawn_char", "Character spawner");
			dict.Add("t_game_piece", "Hrací figurka");
			dict.Add("t_portculis", "Portculis?");
			dict.Add("t_figurine", "Figurina(shrink)");
			dict.Add("t_shrine", "Oltáø(ress)");
			dict.Add("t_moongate", "Moongate");
			dict.Add("t_chair", "idle");
			dict.Add("t_forge", "Vıheò");
			dict.Add("t_ore", "Ruda");
			dict.Add("t_log", "Kmen");
			dict.Add("t_tree", "Strom");
			dict.Add("t_rock", "Skála");
			dict.Add("t_carpentry_chop", "Not Used?");
			dict.Add("t_multi", "Multiitem");
			dict.Add("t_reagent", "Reagent");
			dict.Add("t_ship", "Loï");
			dict.Add("t_ship_plank", "Lodní mùstek");
			dict.Add("t_ship_side", "Bok lodi");
			dict.Add("t_ship_side_locked", "Zamèenı bok lodi");
			dict.Add("t_ship_tiller", "Lodivod");
			dict.Add("t_eq_trade_window", "Secure trade item");
			dict.Add("t_fish", "Ryba");
			dict.Add("t_sign_gump", "Cedule");
			dict.Add("t_stone_guild", "Guild stone");
			dict.Add("t_anim_active", "Animace");
			dict.Add("t_advance_gate", "Advance Gate");
			dict.Add("t_cloth", "Látka");
			dict.Add("t_hair", "Vlasy");
			dict.Add("t_beard", "Vousy");
			dict.Add("t_ingot", "Ingot");
			dict.Add("t_coin", "Mince");
			dict.Add("t_crops", "Ùroda");
			dict.Add("t_drink", "Pití");
			dict.Add("t_anvil", "Kovadlina");
			dict.Add("t_port_locked", "Zamèenı port");
			dict.Add("t_spawn_item", "Item spawner");
			dict.Add("t_telescope", "Teleskop");
			dict.Add("t_bed", "Postel");
			dict.Add("t_gold", "Peníze");
			dict.Add("t_map", "Mapa");
			dict.Add("t_eq_memory_obj", "equippable memory object");
			dict.Add("t_weapon_mace_staff", "Tupá zbraò (hul)");
			dict.Add("t_eq_horse", "Mount item (equipped horse)");
			dict.Add("t_comm_crystal", "Komunkaèní krystal");
			dict.Add("t_game_board", "Herní deska");
			dict.Add("t_trash_can", "Odpadkovı koš");
			dict.Add("t_cannon_muzzle", "Kanon - prach?");
			dict.Add("t_cannon", "Kanon");
			dict.Add("t_cannon_ball", "Dìlová koule");
			dict.Add("t_armor_leather", "Koené brnìní");
			dict.Add("t_seed", "Semena");
			dict.Add("t_junk", "Odpad");
			dict.Add("t_crystal_ball", "Køíšálová koule");
			//t_old_cashiers_check	
			dict.Add("t_message", "Zpráva");
			dict.Add("t_reagent_raw", "'Syrovı' reagent");
			dict.Add("t_eq_client_linger", "udìlá z klienta NPC");
			dict.Add("t_dream_gate", "Brána na jinı shard?");
			dict.Add("t_it_stone", "Supply stone");
			dict.Add("t_metronome", "Metronom");
			dict.Add("t_explosion", "Exploze");
			dict.Add("t_eq_npc_script", "EQuippable NPC script");
			dict.Add("t_web", "Pavouèí sí");
			dict.Add("t_grass", "Tráva");
			dict.Add("t_arock", "Kámen(házen obry)");
			dict.Add("t_tracker", "Tracking obj");
			dict.Add("t_sound", "Zvuk");
			dict.Add("t_stone_town", "Townstone");
			dict.Add("t_weapon_mace_crook", "Tupá zbraò (hák)");
			dict.Add("t_weapon_bow_run", "Run støelná zbraò (/krumpáè)");
			dict.Add("t_leather", "Kùe");
			dict.Add("t_ship_other", "Èást lodi?");
			dict.Add("t_bboard", "Èást tabule");
			dict.Add("t_spellbook", "Kouzelná kniha");
			dict.Add("t_corpse", "Mrtvola");
			dict.Add("t_track_item", "Tracking item");
			dict.Add("t_track_char", "Tracking character");
			dict.Add("t_weapon_arrow", "Šíp");
			dict.Add("t_weapon_bolt", "Šipka");
			dict.Add("t_weapon_bolt_jagged", "Zubatá šipka");
			dict.Add("t_eq_vendor_box", "Vendor box");
			dict.Add("t_eq_bank_box", "Bank box");
			dict.Add("t_deed", "Deed");
			dict.Add("t_loom", "Tkalcovskı stav");
			dict.Add("t_bee_hive", "Vèelí úl");
			dict.Add("t_archery_butte", "Terè");
			dict.Add("t_eq_murder_count", "Killcount timer");
			dict.Add("t_eq_stuck", "para v pavouèí síti");
			dict.Add("t_trap_inactive", "Neaktivní past");
			dict.Add("t_stone_room", "Region stone?");
			dict.Add("t_bandage", "Bandá(healing)");
			dict.Add("t_campfire", "Táborák(camping)");
			dict.Add("t_map_blank", "Prázdná mapa");
			dict.Add("t_spy_glass", "Dalekohled");
			dict.Add("t_sextant", "Sextant");
			dict.Add("t_scroll_blank", "Prázdnı svitek");
			dict.Add("t_fruit", "Ovoce");
			dict.Add("t_water_wash", "Èistá voda (bez ryb)");
			dict.Add("t_weapon_axe", "Ostrá zbraò (sekera?)");
			dict.Add("t_weapon_xbow", "Kuèe");
			dict.Add("t_spellicon", "Ikona kouzla");
			dict.Add("t_door_open", "Otevøené dveøe");
			dict.Add("t_meat_raw", "Syrové maso");
			dict.Add("t_garbage", "Odpad");
			dict.Add("t_keyring", "Krouek na klíèe");
			dict.Add("t_table", "Stùl");
			dict.Add("t_floor", "Podlaha");
			dict.Add("t_roof", "Støecha");
			dict.Add("t_feather", "Peøí");
			dict.Add("t_wool", "Vlna");
			dict.Add("t_fur", "Srst");
			dict.Add("t_blood", "Krev");
			dict.Add("t_foliage", "jídlo?");
			dict.Add("t_grain", "Zrní");
			dict.Add("t_scissors", "Nùky");
			dict.Add("t_thread", "Nitì");
			dict.Add("t_yarn", "Pøíze");
			dict.Add("t_spinwheel", "Kolovrat");
			dict.Add("t_bandage_blood", "Krvavá bandá");
			dict.Add("t_fish_pole", "Rybáøskı prut");
			dict.Add("t_shaft", "Násada); Døík");
			dict.Add("t_lockpick", "Šperhák");
			dict.Add("t_kindling", "Tøíska");
			dict.Add("t_train_dummy", "Panák k boji");
			dict.Add("t_train_pickpocket", "Panák k okrádáni");
			dict.Add("t_bedroll", "Pøikrıvka");
			dict.Add("t_bellows", "Mech");
			dict.Add("t_hide", "Kùe");
			dict.Add("t_cloth_bolt", "Balík látky");
			dict.Add("t_board", "Trám/prkno");
			dict.Add("t_pitcher", "Dbán");
			dict.Add("t_pitcher_empty", "Prázdnı dbán");
			dict.Add("t_dye_vat", "Nádoba na barvu");
			dict.Add("t_dye", "Barva");
			dict.Add("t_potion_empty", "Prázdnı lektvar");
			dict.Add("t_mortar", "Hmodíø");
			dict.Add("t_hair_dye", "Barva na vlasy");
			dict.Add("t_sewing_kit", "Šicí souprava");
			dict.Add("t_tinker_tools", "Nastroje(tinkering)");
			dict.Add("t_wall", "Zeï");
			dict.Add("t_window", "Okno");
			dict.Add("t_cotton", "Bavlna");
			dict.Add("t_bone", "Kost");
			dict.Add("t_eq_script", "Equippable script item");
			dict.Add("t_ship_hold", "Lodní podpalubí");
			dict.Add("t_ship_hold_lock", "Zamèene podpalubí");
			dict.Add("t_lava", "Láva");
			dict.Add("t_shield", "Štít");
			dict.Add("t_jewelry", "Šperk");
			dict.Add("t_dirt", "Špína");
			dict.Add("t_script", "Skript item");
			dict.Add("t_eq_message", "Dialog item");
			dict.Add("t_shovel", "Lopata");
			dict.Add("t_book_shrink", "Recept shrink");
			dict.Add("t_bottle_form", "Forma Láhev");
			dict.Add("t_book_bottle", "Recept Láhev");
			dict.Add("t_src_changer", "mìnitko Src(script)");
			dict.Add("t_book_inscription", "Kniha inscripce");
			dict.Add("t_bottle_empty", "Prázdná láhev");

			return dict;
		}

		public static string GetPrettyName(TriggerGroup tg) {
			string name;
			string dn = tg.PrettyDefname;
			if (typeNames.TryGetValue(dn, out name)) {
				return name;
			}
			return dn;
		}

		public static string GetPrettyName(string itemTypeDefname) {
			string name;
			if (typeNames.TryGetValue(itemTypeDefname, out name)) {
				return name;
			}
			return itemTypeDefname;
		}
	}
}