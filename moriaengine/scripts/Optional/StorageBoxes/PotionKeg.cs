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

using SteamEngine.Common;
using SteamEngine.Communication.TCP;
using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Networking;

namespace SteamEngine.CompiledScripts {
	[ViewableClass]
	public partial class PotionKeg {

		public override void On_DClick(AbstractCharacter ac) {
			((Player) ac).Target(SingletonScript<Targ_PotionKeg>.Instance, this);
			base.On_DClick(ac);
		}

		public override void On_Click(AbstractCharacter clicker, GameState clickerState, TcpConnection<GameState> clickerConn) {
			base.On_Click(clicker, clickerState, clickerConn);
			Language language = clickerState.Language;
			PacketSequences.SendNameFrom(clicker.GameState.Conn, this,
				string.Concat(this.potionsCount.ToString(), " potions"),
				0);
		}
	}

	public class Targ_PotionKeg : CompiledTargetDef {

		protected override void On_Start(Player self, object parameter) {
			self.SysMessage("Zaměř potiony, které chceš vylít do kegu");
			base.On_Start(self, parameter);
		}

		protected override TargetResult On_TargonItem(Player self, Item targetted, object parameter) {
			PotionKeg keg = (PotionKeg) parameter;
			Potion potion = targetted as Potion;

			if (!self.CanReachWithMessage(keg)) {
				self.SysMessage("CanReachWithMessage");
				return TargetResult.RestartTargetting;
			}
			if (potion != null) {
				if (keg.potionDef == null) {
					keg.potionDef = potion.TypeDef;
					keg.Color = potion.Color;
				}

				if (keg.potionDef == potion.TypeDef) {
					ThingDef.GetByDefname("i_bottle_empty").Create(self.Backpack);
					if ((keg.TypeDef.Capacity - keg.potionsCount) < targetted.Amount) {	// poresime prekroceni nosnosti kegu -> do kegu se prida jen tolik potionu, kolik skutecne lze pridat
						int potionsToTake = keg.TypeDef.Capacity - keg.potionsCount;
						targetted.Amount -= potionsToTake;
						keg.potionsCount += potionsToTake;
						Globals.LastNewItem.Amount = potionsToTake;
					} else {
						keg.potionsCount += targetted.Amount;
						potion.Delete();
						Globals.LastNewItem.Amount = targetted.Amount;
					}
				} else {
					self.SysMessage("Tim bys to celé skazil!");
				}
			} else if (targetted.Type.Defname == "t_bottle_empty") {
				if (keg.potionDef != null) {
					if (targetted.Amount < keg.potionsCount) {
						keg.potionDef.Create(self.Backpack);
						Globals.LastNewItem.Amount = targetted.Amount;
						keg.potionsCount -= targetted.Amount;
						targetted.Delete();
					} else {
						keg.potionDef.Create(self.Backpack);
						Globals.LastNewItem.Amount = keg.potionsCount;

						if (targetted.Amount == keg.potionsCount) {
							targetted.Delete();
						} else {
							targetted.Amount -= keg.potionsCount;
						}

						keg.Color = 0;
						keg.potionDef = null;
						keg.potionsCount = 0;
					}
				}
			} else {
				self.SysMessage("Můžeš nalít jenom potiony.");
			}

			return TargetResult.Done;
		}

	}

	//[Dialogs.ViewableClass]
	//public partial class PotionKegDef {}
	//
}
