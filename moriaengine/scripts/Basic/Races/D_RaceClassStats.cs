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

namespace SteamEngine.CompiledScripts {
	/// <summary>Parameters for D_RaceClassStats (#176).</summary>
	public class RaceClassStatsArgs {
		public readonly ProfessionDef Profession;
		public readonly Action<Player> OnFinished;

		public RaceClassStatsArgs(ProfessionDef profession, Action<Player> onFinished) {
			this.Profession = profession;
			this.OnFinished = onFinished;
		}
	}

	/// <summary>Real point-buy starting-stats dialog (#176) - visually matches Erebor's own
	/// `D_RACEclass_stats` (`newbie_portals.scp`): one text-entry field per STR/DEX/INT/VIT,
	/// each showing its valid (min - max) range from the chosen profession, submit validates
	/// every stat is within its own range AND the four sum to exactly 300 - replacing the
	/// previous pass's automatic proportional allocation (RaceSelection.ApplyStartingStats),
	/// which never let the player actually choose.</summary>
	public class D_RaceClassStats : CompiledGumpDef {
		private const int SubmitButtonId = 1;
		private const int StrEntryId = 10;
		private const int DexEntryId = 11;
		private const int IntEntryId = 12;
		private const int VitEntryId = 13;
		public const int RequiredSum = 300;

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			RaceClassStatsArgs stats = (RaceClassStatsArgs) args[0];

			this.SetLocation(0, 0);
			this.AddImage(140, 200, 2200);
			// Same readability fix as D_OptionPicker (#202) - default black HTML text on the
			// gump's plain backdrop above the book image was close to unreadable.
			this.AddHtml(170, 180, 280, 24, "<CENTER><BASEFONT COLOR=#FFFFFF>Choose your starting stats (sum must be " + RequiredSum + ")</BASEFONT></CENTER>", false, false);

			string[] names = { "STR", "DEX", "INT", "VIT" };
			AttributeStat[] attrs = { AttributeStat.Str, AttributeStat.Dex, AttributeStat.Int, AttributeStat.Vit };
			int[] entryIds = { StrEntryId, DexEntryId, IntEntryId, VitEntryId };
			int y = 215;
			for (int i = 0; i < names.Length; i++) {
				int[] range = stats.Profession.GetStatRange(attrs[i]);
				int minimum = (range.Length == 3) ? range[0] : 1;
				int maximum = (range.Length == 3) ? range[2] : short.MaxValue;
				this.AddHtml(170, y, 60, 20, names[i], false, false);
				this.AddHtml(210, y, 100, 20, "(" + minimum + " - " + maximum + ")", false, false);
				this.AddTextEntry(340, y - 2, 60, 20, 0, entryIds[i], minimum.ToString());
				y += 36;
			}

			this.AddButton(170, y + 10, 4017, 4017, SubmitButtonId, GumpButtonType.Reply, 0);
			this.AddHtml(210, y + 10, 100, 20, "Confirm", false, false);
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			if (gr.PressedButton != SubmitButtonId) {
				return; //cancel/close - nothing committed yet, matches Finish()'s "only commit at the end" design
			}
			RaceClassStatsArgs stats = (RaceClassStatsArgs) args[0];
			Player player = (Player) gi.Cont;

			if (!TryParse(gr, StrEntryId, out int str) || !TryParse(gr, DexEntryId, out int dex) ||
				!TryParse(gr, IntEntryId, out int intel) || !TryParse(gr, VitEntryId, out int vit)) {
				player.RedMessage("Please enter whole numbers for all stats.");
				player.Dialog(player, this, args);
				return;
			}

			if (!InRange(stats.Profession, AttributeStat.Str, str, out string error) ||
				!InRange(stats.Profession, AttributeStat.Dex, dex, out error) ||
				!InRange(stats.Profession, AttributeStat.Int, intel, out error) ||
				!InRange(stats.Profession, AttributeStat.Vit, vit, out error)) {
				player.RedMessage(error);
				player.Dialog(player, this, args);
				return;
			}

			int sum = str + dex + intel + vit;
			if (sum != RequiredSum) {
				player.RedMessage("Sum of stats must be " + RequiredSum + ", but is " + sum + ".");
				player.Dialog(player, this, args);
				return;
			}

			player.Str = (short) str;
			player.Dex = (short) dex;
			player.Int = (short) intel;
			player.Vit = (short) vit;
			stats.OnFinished(player);
		}

		private static bool TryParse(GumpResponse gr, int entryId, out int value) {
			return int.TryParse(gr.GetTextResponse(entryId), out value);
		}

		private static bool InRange(ProfessionDef profession, AttributeStat stat, int value, out string error) {
			int[] range = profession.GetStatRange(stat);
			int minimum = (range.Length == 3) ? range[0] : 1;
			int maximum = (range.Length == 3) ? range[2] : short.MaxValue;
			if ((value < minimum) || (value > maximum)) {
				error = stat + " must be between " + minimum + " and " + maximum + ".";
				return false;
			}
			error = null;
			return true;
		}
	}
}
