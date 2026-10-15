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
		private const int CloseButtonId = 1;

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			Player player = (Player) sendTo;

			this.SetLocation(50, 50);
			this.AddBackground(0, 0, 350, 150, 2620);
			this.AddLabel(20, 20, 37, "Abilities");
			this.AddLabel(20, 50, 3, "Unspent ability points: " + player.UnspentAbilityPoints);
			this.AddLabel(20, 75, 3, "Ability spending isn't implemented yet.");
			this.AddButton(20, 110, 2151, 2152, CloseButtonId, GumpButtonType.Reply, 0);
			this.AddLabel(55, 112, 3, "Close");
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			//nothing to do - the only button just closes.
		}
	}
}
