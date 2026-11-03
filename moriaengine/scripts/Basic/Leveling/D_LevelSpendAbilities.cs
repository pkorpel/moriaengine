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
using System.Globalization;
using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {
	/// <summary>#1017 - d_rearability, opened from .explevel: Erebor's vedlejsi ability, every one the
	/// class has, with a + while there's a point to spend and the ability can take it. Closing it goes
	/// back to .explevel, as on Erebor.</summary>
	public class D_LevelSpendAbilities : CompiledGumpDef {
		private const int ClassInfoButton = 1, FirstAbilityButton = 10;
		private const int FirstRow = 70, RowHeight = 20;

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			Player player = (Player) sendTo;
			List<RearAbility> available = ClassAbilities.Available(player);
			int spare = ClassAbilities.Spare(player);

			this.SetLocation(50, 50);
			this.AddBackground(0, 0, 310, FirstRow + (available.Count * RowHeight) + 50, 2620);
			this.AddLabel(90, 18, 37, "Vedlejsi ability");
			this.AddLabel(35, 43, 3, "Bodu k rozdeleni: " + spare);
			for (int line = 0; line < available.Count; line++) {
				RearAbility ability = available[line];
				int y = FirstRow + (line * RowHeight);
				this.AddLabel(35, y, 3, ability.Description);
				this.AddLabel(240, y, 85, ClassAbilities.Points(player, ability.Name).ToString(CultureInfo.InvariantCulture));
				if ((spare > 0) && ClassAbilities.MayRaise(player, ability, false)) {
					this.AddButton(20, y + 7, 2104, 2103, FirstAbilityButton + Array.IndexOf(ClassAbilityTable.Rear, ability), GumpButtonType.Reply, 0);
				}
			}
			int bottom = FirstRow + (available.Count * RowHeight) + 12;
			this.AddButton(35, bottom, 2445, 2445, ClassInfoButton, GumpButtonType.Reply, 0);
			this.AddLabel(45, bottom + 2, 3, "Tvoje trida");
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			Player player = (Player) gi.Cont;
			int button = gr.PressedButton;
			if (button == ClassInfoButton) {
				D_ClassInfo.Show(player);
			} else if ((button >= FirstAbilityButton) && (button < FirstAbilityButton + ClassAbilityTable.Rear.Length)) {
				ClassAbilities.Raise(player, ClassAbilityTable.Rear[button - FirstAbilityButton]);
				player.Dialog(player, SingletonScript<D_LevelSpendAbilities>.Instance);
			} else {
				player.Dialog(player, GumpDef.GetByDefname("d_explevel"));
			}
		}
	}
}
