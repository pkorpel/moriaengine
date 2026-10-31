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

using System.Collections.Generic;
using System.Linq;

namespace SteamEngine.CompiledScripts {
	/// <summary>
	/// The post-creation race/profession/nation selection flow (#162, reworked #173/#175/#176)
	/// - profession picker -> race picker -> nation picker (only for the 3 races with a real
	/// choice) -> stats point-buy -> finish, matching Erebor's own `D_RACEclass_*` order
	/// exactly (`newbie_portals.scp`'s stone triggers classes before races).
	///
	/// Triggered by double-clicking the i_stone_raceclass item (#174, see RaceClassStone.def) via
	/// D_RaceClassEntry - no player-invokable command exists for this anymore.
	///
	/// Finish() is the single place that actually commits anything to the player - every step
	/// before it only threads the in-progress choice through callbacks, so closing the gump
	/// partway through (e.g. the cross button) leaves the player completely untouched, same
	/// spirit as the original design's own "only commit at the very end".
	/// </summary>
	public static class RaceSelection {
		/// <summary>Real root cause of a live-reported regression: picking ANY profession/race
		/// option just reopened the profession picker again instead of advancing. `SingletonScript
		/// &lt;D_OptionPicker&gt;.Instance` does a plain `as T` scan over every registered script -
		/// since `D_RaceClassEntry : D_OptionPicker`, it's ALSO a valid match, and is in fact the
		/// one `FindSingletonInstance` finds (confirmed via a live stack trace) - so every "plain"
		/// D_OptionPicker dialog opened from here was actually being Construct()-ed by
		/// D_RaceClassEntry's own override, which ignores whatever args it's given and always
		/// rebuilds the profession picker from scratch. A genuine standalone D_OptionPicker
		/// instance DOES exist (CompiledGumpDef auto-registers one per concrete subclass, keyed by
		/// its own class name) - looked up directly by defname instead, bypassing SingletonScript's
		/// subclass-unsafe lookup entirely.</summary>
		private static D_OptionPicker PlainOptionPicker {
			get { return (D_OptionPicker) GumpDef.GetByDefname("D_OptionPicker"); }
		}

		/// <summary>Builds the profession-picker args - called both by D_RaceClassEntry (the
		/// i_stone_raceclass entry point, which needs no external args to open) and could be
		/// reused by any other future entry point.</summary>
		public static OptionPickerArgs BuildProfessionPickerArgs() {
			IReadOnlyList<ProfessionDef> professions = Professions.AllReal;
			List<string> labels = new List<string>();
			foreach (ProfessionDef profession in professions) {
				labels.Add(profession.Name);
			}
			return new OptionPickerArgs("Choose your profession", labels, (p, index) => ShowRacePicker(p, professions[index]));
		}

		private static void ShowRacePicker(Player player, ProfessionDef profession) {
			Race[] races = {
				Race.Human, Race.Elf, Race.Dwarf, Race.Hobbit, Race.Orc, Race.UrukHai
			};
			List<string> labels = new List<string>();
			foreach (Race race in races) {
				labels.Add(RaceData.GetDisplayName(race));
			}
			player.Dialog(player, PlainOptionPicker, new DialogArgs(
				new OptionPickerArgs("Choose your race", labels, (p, index) => ShowNationPicker(p, races[index], profession))));
		}

		private static void ShowNationPicker(Player player, Race race, ProfessionDef profession) {
			IReadOnlyList<Nation> nations = RaceData.GetAvailableNations(race);
			if (!RaceData.HasNationChoice(race)) {
				ShowStatsPicker(player, race, profession, nations[0]);
				return;
			}
			List<string> labels = new List<string>();
			foreach (Nation nation in nations) {
				labels.Add(NationData.GetDisplayName(nation));
			}
			player.Dialog(player, PlainOptionPicker, new DialogArgs(
				new OptionPickerArgs("Choose your nation", labels, (p, index) => ShowStatsPicker(p, race, profession, nations[index]))));
		}

		private static void ShowStatsPicker(Player player, Race race, ProfessionDef profession, Nation nation) {
			player.Dialog(player, SingletonScript<D_RaceClassStats>.Instance, new DialogArgs(
				new RaceClassStatsArgs(profession, p => Finish(p, race, profession, nation))));
		}

		/// <summary>The only place that commits anything - stats were already validated and
		/// applied by D_RaceClassStats before this runs (#176).</summary>
		private static void Finish(Player player, Race race, ProfessionDef profession, Nation nation) {
			player.Race = race;
			player.Profession = profession; //triggers ProfessionPlugin.On_Assign - starting skills + title (#135/#165/#177)
			player.Nation = nation; //cascades Realm + Home (#163/#164)
			player.P(player.Home); //one-time placement at the new home, NOT a reusable recall (#164 explicitly excludes recall)
			ClearForNewLife(player);
			LevelingSystem.StartNewLife(player, profession);
			GiveStartingEquipment(player, profession);

			player.SysMessage("Welcome! You are now a " + RaceData.GetDisplayName(race) + " " +
				profession.Name + " of " + NationData.GetDisplayName(nation) + ".");
		}

		private static readonly LayerNames[] clearedLayers = {
			LayerNames.Hand1, LayerNames.Hand2, LayerNames.Shoes, LayerNames.Pants, LayerNames.Shirt, LayerNames.Helmet, LayerNames.Gloves,
			LayerNames.Ring, LayerNames.Collar, LayerNames.HalfApron, LayerNames.Chest, LayerNames.Bracelet, LayerNames.Tunic, LayerNames.Earrings,
			LayerNames.Arms, LayerNames.Cape, LayerNames.Robe, LayerNames.Skirt, LayerNames.Leggins, LayerNames.Dragging
		};

		/// <summary>#881 - Erebor's promazat: nothing of the tutorial comes along, worn, carried,
		/// banked or in hand.</summary>
		internal static void ClearForNewLife(Player player) {
			foreach (LayerNames layer in clearedLayers) {
				AbstractItem item = player.FindLayer(layer);
				if (item != null) {
					item.Delete();
				}
			}
			player.Backpack.EmptyCont();
			player.Bank.EmptyCont();
		}

		/// <summary>Profession.Name (a short English key: "mag", "warrior", "necromancer", ...)
		/// to Erebor's own newbie-template suffix (newbie_portals.scp's startclass:
		/// `newloot(tm_&lt;tag.class&gt;newbie)`) - the two don't always match 1:1 (e.g.
		/// "warrior" -> "war", "craftsman" -> "craft", "mystic" -> "Mystik"), so this is an
		/// explicit table rather than a guessed string transform.</summary>
		private static readonly Dictionary<string, string> professionToTemplateSuffix = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase) {
			{ "mag", "mag" },
			{ "ranger", "ranger" },
			{ "necromancer", "necro" },
			{ "craftsman", "craft" },
			{ "priest", "priest" },
			{ "warrior", "war" },
			{ "thief", "thief" },
			{ "shaman", "shaman" },
			{ "mystic", "Mystik" },
		};

		/// <summary>Grants the beginner's equipment newbie_portals.scp's own `startclass`
		/// function gives on first entering the real world: 2000 gold (`givegold(2000)`) plus
		/// the profession-specific `tm_&lt;class&gt;newbie` TemplateDef (weapons/armor/potions/
		/// bandages/spellbook - see scripts/converted/newbie_portals.def for the full per-
		/// profession kit, restored to match the original Erebor source where the automated
		/// converter had silently dropped items it couldn't resolve, e.g. Craftsman's tools).
		/// Deliberately NOT ported: `giveitem(i_rune_base)` (a recall rune - recall is an
		/// explicitly separate, not-yet-designed feature per #164's own scope correction) and
		/// `giveitem(i_crystal_&lt;class&gt;)` (a decorative "class crystal" item that was never
		/// converted into this engine's content at all - flavor only, not gameplay-relevant).</summary>
		private static void GiveStartingEquipment(Player player, ProfessionDef profession) {
			GoldUtils.Grant(player, 2000);

			if (!professionToTemplateSuffix.TryGetValue(profession.Name, out string suffix)) {
				return; //no newbie kit mapped for this profession - nothing to grant
			}
			TemplateDef template = TemplateDef.GetByDefname("tm_" + suffix + "newbie");
			if (template != null) {
				template.Create(player);
			}
			//newloot dresses the character, our template only fills the pack
			foreach (Wearable piece in new List<AbstractItem>(player.Backpack).OfType<Wearable>().ToList()) {
				if (player.FindLayer((LayerNames) piece.Layer) == null) {
					player.TryEquip(piece);
				}
			}
			//startclass: weapons go to the pack, what's left on gets the class color
			foreach (LayerNames hand in new[] { LayerNames.Hand1, LayerNames.Hand2 }) {
				AbstractItem held = player.FindLayer(hand);
				if (held != null) {
					held.Cont = player.Backpack;
				}
			}
			if (professionColors.TryGetValue(profession.Name, out int color)) {
				foreach (LayerNames layer in clearedLayers) {
					AbstractItem item = player.FindLayer(layer);
					if (item != null) {
						item.Color = color;
					}
				}
			}
		}

		private static readonly Dictionary<string, int> professionColors = new Dictionary<string, int>(System.StringComparer.OrdinalIgnoreCase) {
			{ "warrior", 0x538 }, { "necromancer", 0x3c2 }, { "priest", 0x2219 }, { "mag", 0x57 }, { "craftsman", 0x712 },
			{ "thief", 0x2ff }, { "ranger", 0x233 }, { "shaman", 0x2e8 }, { "mystic", 0x835 }
		};
	}

	/// <summary>Dialog-openable entry point for the race-selection flow (#174) - unlike
	/// D_OptionPicker's other uses, this one needs to be reachable with NO external
	/// DialogArgs (i_stone_raceclass's on=@dclick trigger just does `src.Dialog(d_raceclassentry)`
	/// - a bare LScript Dialog() call can't construct an OptionPickerArgs/closure itself), so it
	/// builds its own args internally and forwards to the shared two-column rendering/response
	/// logic in D_OptionPicker.</summary>
	public class D_RaceClassEntry : D_OptionPicker {
		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			Player player = sendTo as Player;
			if ((player != null) && (player.Race != Race.None)) {
				player.SysMessage("You have already chosen your race (" + RaceData.GetDisplayName(player.Race) + ").");
				//Still has to render SOMETHING here - a CompiledGumpDef subclass can't suppress
				//the gump entirely from Construct (GumpInstance has no public setter) - an empty
				//picker (no options) just shows the background/title with nothing to click.
				DialogArgs emptyArgs = new DialogArgs(new OptionPickerArgs("Already chosen", new string[0], null));
				this.GumpInstance.InputArgs = emptyArgs;
				base.Construct(focus, sendTo, emptyArgs);
				return;
			}
			DialogArgs realArgs = new DialogArgs(RaceSelection.BuildProfessionPickerArgs());
			this.GumpInstance.InputArgs = realArgs;
			base.Construct(focus, sendTo, realArgs);
		}
	}
}
