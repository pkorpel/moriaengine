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

namespace SteamEngine.CompiledScripts {
	/// <summary>Attribute point-spend dialog (#156), opened from .explevel's new "Attributes"
	/// button - same plain parchment style as explevel.scp itself (background 2620) rather than
	/// the C# ImprovedDialog framework. One "+1" spend button per STR/DEX/INT/VIT (matching
	/// D_LevelSpendSkills' flat-+1-per-click pattern - a real-world-testing finding, #199: the
	/// original text-entry-then-click design defaulted the entry to "0", so clicking without
	/// first typing a number always hit "enter a positive whole number of points to spend"),
	/// validated by LevelingSystem.TrySpendAttributePoints against both the overall
	/// AttributePointBudget and the profession's own per-stat hard cap.</summary>
	public class D_LevelSpendAttributes : CompiledGumpDef {
		private const int StrButtonId = 1;
		private const int DexButtonId = 2;
		private const int IntButtonId = 3;
		private const int VitButtonId = 4;
		private const int BackButtonId = 98;
		private const int CloseButtonId = 99;

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			Player player = (Player) sendTo;

			const int width = 400;
			this.SetLocation(50, 50);
			this.AddBackground(0, 0, width, 300, 2620);
			this.AddLabel(20, 20, 37, "Attributes - Level " + player.Level);
			this.AddLabel(20, 45, 3, "Unspent budget: " + player.AttributePointBudget);

			ProfessionDef profession = player.Profession;
			string[] names = { "STR", "DEX", "INT", "VIT" };
			AttributeStat[] attrs = { AttributeStat.Str, AttributeStat.Dex, AttributeStat.Int, AttributeStat.Vit };
			int[] current = { player.Str, player.Dex, player.Int, player.Vit };
			int[] buttonIds = { StrButtonId, DexButtonId, IntButtonId, VitButtonId };

			int y = 75;
			for (int i = 0; i < names.Length; i++) {
				string capText = "no profession";
				if (profession != null) {
					int[] range = profession.GetStatRange(attrs[i]);
					int hardCap = (range.Length == 3) ? range[2] : short.MaxValue;
					capText = "cap " + hardCap;
				}
				this.AddLabel(20, y, 3, names[i] + ": " + current[i] + " (" + capText + ")");
				this.AddButton(290, y - 2, 2151, 2152, buttonIds[i], GumpButtonType.Reply, 0);
				y += 32;
			}

			// Back/Close bottom row, same "paper tab" style as explevel.scp's own buttons (#202) -
			// Back pinned to the left edge, Close to the right edge (#204: they overlapped when
			// both sat near the left edge only ~100px apart - the 2445 graphic renders wider
			// than that).
			this.AddButton(20, y + 10, 2445, 2445, BackButtonId, GumpButtonType.Reply, 0);
			this.AddLabel(25, y + 12, 3, "Back");
			this.AddButton(width - 120, y + 10, 2445, 2445, CloseButtonId, GumpButtonType.Reply, 0);
			this.AddLabel(width - 115, y + 12, 3, "Close");
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			Player player = (Player) gi.Cont;
			// Button 0 is the real UO client signal for "closed via right-click" (not this
			// dialog's own explicit Close button) - must also return without reopening, or a
			// right-click just silently gets trapped back into the same dialog (#199).
			if ((gr.PressedButton == CloseButtonId) || (gr.PressedButton == 0)) {
				return;
			}
			if (gr.PressedButton == BackButtonId) {
				player.Dialog(player, GumpDef.GetByDefname("d_explevel"));
				return;
			}

			AttributeStat stat;
			switch (gr.PressedButton) {
				case StrButtonId: stat = AttributeStat.Str; break;
				case DexButtonId: stat = AttributeStat.Dex; break;
				case IntButtonId: stat = AttributeStat.Int; break;
				case VitButtonId: stat = AttributeStat.Vit; break;
				default: return;
			}

			if (!LevelingSystem.TrySpendAttributePoints(player, stat, 1)) {
				player.RedMessage("Could not spend a point there - check your remaining budget and this stat's cap.");
			}
			player.Dialog(player, this, args);
		}
	}
}
