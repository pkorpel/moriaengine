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

namespace SteamEngine.CompiledScripts {
	/// <summary>
	/// The post-creation race/profession/nation selection flow (#162) - race picker -> profession
	/// picker -> nation picker (only for the 3 races with a real choice) -> finish. Triggered via
	/// a plain player-level ".ChooseRace" command rather than a physical stone item - the original
	/// Sphere design uses an in-world stone (i_stone_raceclass), but a new custom Item/ItemDef
	/// subclass needs its own ClassTemplate (.ct) and a run through the Generated.cs pipeline,
	/// real added risk for a cosmetic trigger mechanism. A command is functionally equivalent and
	/// far lower risk - flagged here if the user wants the physical-stone flavor added later.
	///
	/// Finish() is the single place that actually commits anything to the player - every step
	/// before it only threads the in-progress choice through OptionPickerArgs callbacks, so
	/// closing the gump partway through (e.g. the cross button) leaves the player completely
	/// untouched, same spirit as the original design's own "only commit at the very end".
	/// </summary>
	public static class RaceSelection {
		[SteamFunction("ChooseRace")]
		public static void RaceCommand(Player self) {
			if (self.Race != Race.None) {
				self.SysMessage("You have already chosen your race (" + RaceData.GetDisplayName(self.Race) + ").");
				return;
			}
			ShowRacePicker(self);
		}

		private static void ShowRacePicker(Player player) {
			Race[] races = {
				Race.Human, Race.Elf, Race.Dwarf, Race.Hobbit, Race.Orc, Race.UrukHai
			};
			List<string> labels = new List<string>();
			foreach (Race race in races) {
				labels.Add(RaceData.GetDisplayName(race));
			}
			player.Dialog(player, SingletonScript<D_OptionPicker>.Instance, new DialogArgs(
				new OptionPickerArgs("Choose your race", labels, (p, index) => ShowProfessionPicker(p, races[index]))));
		}

		private static void ShowProfessionPicker(Player player, Race race) {
			IReadOnlyList<ProfessionDef> professions = Professions.AllReal;
			List<string> labels = new List<string>();
			foreach (ProfessionDef profession in professions) {
				labels.Add(profession.Name);
			}
			player.Dialog(player, SingletonScript<D_OptionPicker>.Instance, new DialogArgs(
				new OptionPickerArgs("Choose your profession", labels, (p, index) => ShowNationPicker(p, race, professions[index]))));
		}

		private static void ShowNationPicker(Player player, Race race, ProfessionDef profession) {
			IReadOnlyList<Nation> nations = RaceData.GetAvailableNations(race);
			if (!RaceData.HasNationChoice(race)) {
				Finish(player, race, profession, nations[0]);
				return;
			}
			List<string> labels = new List<string>();
			foreach (Nation nation in nations) {
				labels.Add(NationData.GetDisplayName(nation));
			}
			player.Dialog(player, SingletonScript<D_OptionPicker>.Instance, new DialogArgs(
				new OptionPickerArgs("Choose your nation", labels, (p, index) => Finish(p, race, profession, nations[index]))));
		}

		private static void Finish(Player player, Race race, ProfessionDef profession, Nation nation) {
			player.Race = race;
			player.Profession = profession; //triggers ProfessionPlugin.On_Assign - starting skills + title (#135/#165)
			player.Nation = nation; //cascades Realm + Home (#163/#164)
			ApplyStartingStats(player, profession);
			player.P(player.Home); //one-time placement at the new home, NOT a reusable recall (#164 explicitly excludes recall)

			player.SysMessage("Welcome! You are now a " + RaceData.GetDisplayName(race) + " " +
				profession.Name + " of " + NationData.GetDisplayName(nation) + ".");
		}

		/// <summary>Automatically allocates starting STR/DEX/INT/VIT (#162) - a deliberate
		/// simplification of the original Sphere design's fully-interactive point-buy dialog
		/// (free-text entry per stat, live sum-must-equal-300 validation). Building that exact
		/// UI needs a new numeric-entry-with-cross-field-validation gump pattern that doesn't
		/// exist anywhere else in this codebase yet - out of scope for this pass. Instead, scales
		/// the profession's own startingMax values down proportionally to sum to 300 (preserving
		/// their relative emphasis - e.g. a mage's largest allocation stays INT), clamped to each
		/// stat's own [minimum, hardCap]. Not pixel-exact to 300 after clamping/rounding, and not
		/// player-adjustable - flagged as a known follow-up if manual allocation matters later.</summary>
		private static void ApplyStartingStats(Player player, ProfessionDef profession) {
			AttributeStat[] stats = { AttributeStat.Str, AttributeStat.Dex, AttributeStat.Int, AttributeStat.Vit };
			int[] startingMax = new int[4];
			int sum = 0;
			for (int i = 0; i < stats.Length; i++) {
				int[] range = profession.GetStatRange(stats[i]);
				startingMax[i] = (range.Length == 3) ? range[1] : 0;
				sum += startingMax[i];
			}
			if (sum == 0) {
				return; //no stat-range data authored for this profession - leave stats as-is
			}

			double scale = 300.0 / sum;
			for (int i = 0; i < stats.Length; i++) {
				int[] range = profession.GetStatRange(stats[i]);
				int minimum = (range.Length == 3) ? range[0] : 1;
				int hardCap = (range.Length == 3) ? range[2] : short.MaxValue;
				int value = Math.Max(minimum, Math.Min(hardCap, (int) Math.Round(startingMax[i] * scale)));
				SetStat(player, stats[i], (short) value);
			}
		}

		private static void SetStat(Player player, AttributeStat stat, short value) {
			switch (stat) {
				case AttributeStat.Str:
					player.Str = value;
					break;
				case AttributeStat.Dex:
					player.Dex = value;
					break;
				case AttributeStat.Int:
					player.Int = value;
					break;
				case AttributeStat.Vit:
					player.Vit = value;
					break;
			}
		}
	}
}
