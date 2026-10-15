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
	/// <summary>Ability point-spend dialog (#156), opened from .explevel's new "Abilities"
	/// button - deliberately left empty for now, per explicit user instruction: abilities/
	/// talent-trees are still out of scope until a proper redesign happens (same standing
	/// decision as the Professions system's own deferral). Only shows the unspent point count
	/// and a close button so the navigation from .explevel is already wired up for whenever
	/// abilities get designed.</summary>
	public class D_LevelSpendAbilities : CompiledGumpDef {
		private const int BackButtonId = 1;
		private const int CloseButtonId = 2;

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			Player player = (Player) sendTo;

			const int width = 350;
			this.SetLocation(50, 50);
			this.AddBackground(0, 0, width, 150, 2620);
			this.AddLabel(20, 20, 37, "Abilities");
			this.AddLabel(20, 50, 3, "Unspent ability points: " + player.UnspentAbilityPoints);
			this.AddLabel(20, 75, 3, "Ability spending isn't implemented yet.");

			// Back/Close bottom row, same "paper tab" style as explevel.scp's own buttons (#202) -
			// Back pinned to the left edge, Close to the right edge (#204: they overlapped when
			// both sat near the left edge only ~100px apart - the 2445 graphic renders wider
			// than that).
			this.AddButton(20, 110, 2445, 2445, BackButtonId, GumpButtonType.Reply, 0);
			this.AddLabel(25, 112, 3, "Back");
			this.AddButton(width - 120, 110, 2445, 2445, CloseButtonId, GumpButtonType.Reply, 0);
			this.AddLabel(width - 115, 112, 3, "Close");
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			if (gr.PressedButton == BackButtonId) {
				Player player = (Player) gi.Cont;
				player.Dialog(player, GumpDef.GetByDefname("d_explevel"));
			}
			// Close (or right-click, button 0) - nothing to do.
		}
	}
}
