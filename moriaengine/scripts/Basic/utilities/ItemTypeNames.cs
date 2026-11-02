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
			dict.Add("t_container_locked", "Zamceny Kontejner");
			dict.Add("t_door", "Dvere");
			dict.Add("t_door_locked", "Zamcene dvere");
			dict.Add("t_key", "Klic");
			dict.Add("t_light_lit", "Svitici svetlo");
			dict.Add("t_light_out", "Zhasnute svetlo");
			dict.Add("t_food", "Jidlo");
			dict.Add("t_food_raw", "Syrove Jidlo");
			dict.Add("t_armor", "Brneni");
			dict.Add("t_weapon_mace_smith", "Tupa Zbran(nastroj?)");
			dict.Add("t_weapon_mace_sharp", "Tupa Zbran");
			dict.Add("t_weapon_sword", "Ostra Zbran (tezka)");
			dict.Add("t_weapon_fence", "Ostra Zbran (lehka)");
			dict.Add("t_weapon_bow", "Luk");
			dict.Add("t_wand", "Hulka");
			dict.Add("t_telepad", "Telepad");
			dict.Add("t_switch", "Prepinac");
			dict.Add("t_book", "Kniha");
			dict.Add("t_rune", "Runa");
			dict.Add("t_booze", "Alkohol");
			dict.Add("t_potion", "Lektvar");
			dict.Add("t_allpotions", "Lektvar");
			dict.Add("t_speedpotions", "Lektvar");
			dict.Add("t_potion_bomba", "Bomba");
			dict.Add("t_fire", "Ohen");
			dict.Add("t_clock", "Hodiny");
			dict.Add("t_trap", "Past");
			dict.Add("t_trap_active", "Aktivni past");
			dict.Add("t_musical", "Hudebni nastroj");
			dict.Add("t_spell", "Kouzlo");
			dict.Add("t_gem", "Drahokam");
			dict.Add("t_water", "Voda");
			dict.Add("t_clothing", "Obleceni");
			dict.Add("t_scroll", "	Svitek kouzla");
			dict.Add("t_carpentry", "Truhlarsky nastroj");
			dict.Add("t_spawn_char", "Character spawner");
			dict.Add("t_game_piece", "Hraci figurka");
			dict.Add("t_portculis", "Portculis?");
			dict.Add("t_figurine", "Figurina(shrink)");
			dict.Add("t_shrine", "Oltar(ress)");
			dict.Add("t_moongate", "Moongate");
			dict.Add("t_chair", "Zidle");
			dict.Add("t_forge", "Vyhen");
			dict.Add("t_ore", "Ruda");
			dict.Add("t_log", "Kmen");
			dict.Add("t_tree", "Strom");
			dict.Add("t_rock", "Skala");
			dict.Add("t_carpentry_chop", "Not Used?");
			dict.Add("t_multi", "Multiitem");
			dict.Add("t_reagent", "Reagent");
			dict.Add("t_ship", "Lod");
			dict.Add("t_ship_plank", "Lodni mustek");
			dict.Add("t_ship_side", "Bok lodi");
			dict.Add("t_ship_side_locked", "Zamceny bok lodi");
			dict.Add("t_ship_tiller", "Lodivod");
			dict.Add("t_eq_trade_window", "Secure trade item");
			dict.Add("t_fish", "Ryba");
			dict.Add("t_sign_gump", "Cedule");
			dict.Add("t_stone_guild", "Guild stone");
			dict.Add("t_anim_active", "Animace");
			dict.Add("t_advance_gate", "Advance Gate");
			dict.Add("t_cloth", "Latka");
			dict.Add("t_hair", "Vlasy");
			dict.Add("t_beard", "Vousy");
			dict.Add("t_ingot", "Ingot");
			dict.Add("t_coin", "Mince");
			dict.Add("t_crops", "Uroda");
			dict.Add("t_drink", "Piti");
			dict.Add("t_anvil", "Kovadlina");
			dict.Add("t_port_locked", "Zamceny port");
			dict.Add("t_spawn_item", "Item spawner");
			dict.Add("t_telescope", "Teleskop");
			dict.Add("t_bed", "Postel");
			dict.Add("t_gold", "Penize");
			dict.Add("t_map", "Mapa");
			dict.Add("t_eq_memory_obj", "equippable memory object");
			dict.Add("t_weapon_mace_staff", "Tupa zbran (hul)");
			dict.Add("t_eq_horse", "Mount item (equipped horse)");
			dict.Add("t_comm_crystal", "Komunkacni krystal");
			dict.Add("t_game_board", "Herni deska");
			dict.Add("t_trash_can", "Odpadkovy kos");
			dict.Add("t_cannon_muzzle", "Kanon - prach?");
			dict.Add("t_cannon", "Kanon");
			dict.Add("t_cannon_ball", "Delova koule");
			dict.Add("t_armor_leather", "Kozene brneni");
			dict.Add("t_seed", "Semena");
			dict.Add("t_junk", "Odpad");
			dict.Add("t_crystal_ball", "Kristalova koule");
			//t_old_cashiers_check	
			dict.Add("t_message", "Zprava");
			dict.Add("t_reagent_raw", "'Syrovy' reagent");
			dict.Add("t_eq_client_linger", "udela z klienta NPC");
			dict.Add("t_dream_gate", "Brana na jiny shard?");
			dict.Add("t_it_stone", "Supply stone");
			dict.Add("t_metronome", "Metronom");
			dict.Add("t_explosion", "Exploze");
			dict.Add("t_eq_npc_script", "EQuippable NPC script");
			dict.Add("t_web", "Pavouci sit");
			dict.Add("t_grass", "Trava");
			dict.Add("t_arock", "Kamen(hazen obry)");
			dict.Add("t_tracker", "Tracking obj");
			dict.Add("t_sound", "Zvuk");
			dict.Add("t_stone_town", "Townstone");
			dict.Add("t_weapon_mace_crook", "Tupa zbran (hak)");
			dict.Add("t_weapon_bow_run", "Run strelna zbran (/krumpac)");
			dict.Add("t_leather", "Kuze");
			dict.Add("t_ship_other", "Cast lodi?");
			dict.Add("t_bboard", "Cast tabule");
			dict.Add("t_spellbook", "Kouzelna kniha");
			dict.Add("t_corpse", "Mrtvola");
			dict.Add("t_track_item", "Tracking item");
			dict.Add("t_track_char", "Tracking character");
			dict.Add("t_weapon_arrow", "Sip");
			dict.Add("t_weapon_bolt", "Sipka");
			dict.Add("t_weapon_bolt_jagged", "Zubata sipka");
			dict.Add("t_eq_vendor_box", "Vendor box");
			dict.Add("t_eq_bank_box", "Bank box");
			dict.Add("t_deed", "Deed");
			dict.Add("t_loom", "Tkalcovsky stav");
			dict.Add("t_bee_hive", "Vceli ul");
			dict.Add("t_archery_butte", "Terc");
			dict.Add("t_eq_murder_count", "Killcount timer");
			dict.Add("t_eq_stuck", "para v pavouci siti");
			dict.Add("t_trap_inactive", "Neaktivni past");
			dict.Add("t_stone_room", "Region stone?");
			dict.Add("t_bandage", "Bandaz(healing)");
			dict.Add("t_campfire", "Taborak(camping)");
			dict.Add("t_map_blank", "Prazdna mapa");
			dict.Add("t_spy_glass", "Dalekohled");
			dict.Add("t_sextant", "Sextant");
			dict.Add("t_scroll_blank", "Prazdny svitek");
			dict.Add("t_fruit", "Ovoce");
			dict.Add("t_water_wash", "Cista voda (bez ryb)");
			dict.Add("t_weapon_axe", "Ostra zbran (sekera?)");
			dict.Add("t_weapon_xbow", "Kuce");
			dict.Add("t_spellicon", "Ikona kouzla");
			dict.Add("t_door_open", "Otevrene dvere");
			dict.Add("t_meat_raw", "Syrove maso");
			dict.Add("t_garbage", "Odpad");
			dict.Add("t_keyring", "Krouzek na klice");
			dict.Add("t_table", "Stul");
			dict.Add("t_floor", "Podlaha");
			dict.Add("t_roof", "Strecha");
			dict.Add("t_feather", "Peri");
			dict.Add("t_wool", "Vlna");
			dict.Add("t_fur", "Srst");
			dict.Add("t_blood", "Krev");
			dict.Add("t_foliage", "jidlo?");
			dict.Add("t_grain", "Zrni");
			dict.Add("t_scissors", "Nuzky");
			dict.Add("t_thread", "Nite");
			dict.Add("t_yarn", "Prize");
			dict.Add("t_spinwheel", "Kolovrat");
			dict.Add("t_bandage_blood", "Krvava bandaz");
			dict.Add("t_fish_pole", "Rybarsky prut");
			dict.Add("t_shaft", "Nasada); Drik");
			dict.Add("t_lockpick", "Sperhak");
			dict.Add("t_kindling", "Triska");
			dict.Add("t_train_dummy", "Panak k boji");
			dict.Add("t_train_pickpocket", "Panak k okradani");
			dict.Add("t_bedroll", "Prikryvka");
			dict.Add("t_bellows", "Mech");
			dict.Add("t_hide", "Kuze");
			dict.Add("t_cloth_bolt", "Balik latky");
			dict.Add("t_board", "Tram/prkno");
			dict.Add("t_pitcher", "Dzban");
			dict.Add("t_pitcher_empty", "Prazdny dzban");
			dict.Add("t_dye_vat", "Nadoba na barvu");
			dict.Add("t_dye", "Barva");
			dict.Add("t_potion_empty", "Prazdny lektvar");
			dict.Add("t_mortar", "Hmozdir");
			dict.Add("t_hair_dye", "Barva na vlasy");
			dict.Add("t_sewing_kit", "Sici souprava");
			dict.Add("t_tinker_tools", "Nastroje(tinkering)");
			dict.Add("t_wall", "Zed");
			dict.Add("t_window", "Okno");
			dict.Add("t_cotton", "Bavlna");
			dict.Add("t_bone", "Kost");
			dict.Add("t_eq_script", "Equippable script item");
			dict.Add("t_ship_hold", "Lodni podpalubi");
			dict.Add("t_ship_hold_lock", "Zamcene podpalubi");
			dict.Add("t_lava", "Lava");
			dict.Add("t_shield", "Stit");
			dict.Add("t_jewelry", "Sperk");
			dict.Add("t_dirt", "Spina");
			dict.Add("t_script", "Skript item");
			dict.Add("t_eq_message", "Dialog item");
			dict.Add("t_shovel", "Lopata");
			dict.Add("t_book_shrink", "Recept shrink");
			dict.Add("t_bottle_form", "Forma Lahev");
			dict.Add("t_book_bottle", "Recept Lahev");
			dict.Add("t_src_changer", "menitko Src(script)");
			dict.Add("t_book_inscription", "Kniha inscripce");
			dict.Add("t_bottle_empty", "Prazdna lahev");

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