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
	/// the C# ImprovedDialog framework. One text-entry + spend button per STR/DEX/INT/VIT,
	/// validated by LevelingSystem.TrySpendAttributePoints against both the overall
	/// AttributePointBudget and the profession's own per-stat hard cap.</summary>
	public class D_LevelSpendAttributes : CompiledGumpDef {
		private const int StrButtonId = 1;
		private const int DexButtonId = 2;
		private const int IntButtonId = 3;
		private const int VitButtonId = 4;
		private const int StrEntryId = 10;
		private const int DexEntryId = 11;
		private const int IntEntryId = 12;
		private const int VitEntryId = 13;

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			Player player = (Player) sendTo;

			this.SetLocation(50, 50);
			this.AddBackground(0, 0, 400, 300, 2620);
			this.AddLabel(20, 20, 37, "Attributes - Level " + player.Level);
			this.AddLabel(20, 45, 3, "Unspent budget: " + player.AttributePointBudget);

			ProfessionDef profession = player.Profession;
			string[] names = { "STR", "DEX", "INT", "VIT" };
			AttributeStat[] attrs = { AttributeStat.Str, AttributeStat.Dex, AttributeStat.Int, AttributeStat.Vit };
			int[] current = { player.Str, player.Dex, player.Int, player.Vit };
			int[] entryIds = { StrEntryId, DexEntryId, IntEntryId, VitEntryId };
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
				this.AddTextEntry(220, y - 2, 60, 20, 0, entryIds[i], "0");
				this.AddButton(290, y - 2, 2151, 2152, buttonIds[i], GumpButtonType.Reply, 0);
				y += 32;
			}

			this.AddButton(20, y + 10, 2151, 2152, 99, GumpButtonType.Reply, 0);
			this.AddLabel(55, y + 12, 3, "Close");
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			Player player = (Player) gi.Cont;
			if (gr.PressedButton == 99) {
				return;
			}

			AttributeStat stat;
			int entryId;
			switch (gr.PressedButton) {
				case StrButtonId: stat = AttributeStat.Str; entryId = StrEntryId; break;
				case DexButtonId: stat = AttributeStat.Dex; entryId = DexEntryId; break;
				case IntButtonId: stat = AttributeStat.Int; entryId = IntEntryId; break;
				case VitButtonId: stat = AttributeStat.Vit; entryId = VitEntryId; break;
				default: return;
			}

			if (!int.TryParse(gr.GetTextResponse(entryId), out int pointsToSpend) || (pointsToSpend <= 0)) {
				player.RedMessage("Enter a positive whole number of points to spend.");
			} else if (!LevelingSystem.TrySpendAttributePoints(player, stat, pointsToSpend)) {
				player.RedMessage("Could not spend that many points - check your remaining budget and this stat's cap.");
			}
			player.Dialog(player, this, args);
		}
	}
}
