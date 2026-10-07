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
using SteamEngine.Common;
using SteamEngine.Communication.TCP;
using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Networking;

namespace SteamEngine.CompiledScripts {
	[ViewableClass]
	public partial class ColoredStaffDef {
	}

	[ViewableClass]
	public partial class ColoredStaff {

		//this ability raises the maximum of mana that can be deposited in a staff
		private static AbilityDef a_mana_deposit_bonus;
		private static AbilityDef ManaDepositBonusDef {
			get {
				if (a_mana_deposit_bonus == null) {
					a_mana_deposit_bonus = AbilityDef.GetByDefname("a_mana_deposit_bonus");
				}
				return a_mana_deposit_bonus;
			}
		}
		public override void On_Click(AbstractCharacter clicker, GameState clickerState, TcpConnection<GameState> clickerConn) {
			base.On_Click(clicker, clickerState, clickerConn);
			this.ShowMana(clicker as Player);
		}

		public override void On_DClick(AbstractCharacter from) {
			Player self = from as Player;
			if ((self != null) && (this.Cont == from)) { //we have it equipped
				int selfMana = self.Mana;
				int selfMaxMana = self.MaxMana;
				if (selfMana >= selfMaxMana) {
					double staffMaxMana = this.CalculateMaxMana(self);
					if (this.mana < staffMaxMana) {
						double manaswap = (self.EffectiveLevel + 30.0) / 100.0; //swap effectivity = 90% at level 60
						double staffResultMana = this.mana + selfMana * manaswap;
						if (staffResultMana > staffMaxMana) {
							self.Mana = (short) ((staffResultMana - staffMaxMana) / manaswap);
							this.mana = (ushort) staffMaxMana;
						} else {
							self.Mana = 0;
							this.mana = (ushort) staffResultMana;
						}
						this.ManaSwapped(self);
					}
				} else {
					int manadiff = Math.Min(selfMaxMana - selfMana, this.mana);
					if (manadiff > 0) {
						self.Mana = (short) (selfMana + manadiff);
						this.mana = (ushort) (this.mana - manadiff);
						this.ManaSwapped(self);
					}
				}
				this.ShowMana(self);
			}

			base.On_DClick(from);
		}

		public override void On_Unequip(ItemInCharArgs args) {
			if (this.mana > 0) {
				Player self = args.Cont as Player;
				if (self != null) {
					self.WriteLine(Loc<ColoredStaffLoc>.Get(self.Language).manaVanished);
				}
				this.mana = 0;
			}

			base.On_Unequip(args);
		}

		private double CalculateMaxMana(Player self) {
			double staffMaxMana = this.TypeDef.MaxMana;
			double perCentBonus = self.GetAbility(ManaDepositBonusDef) * ManaDepositBonusDef.EffectPower;
			staffMaxMana += (staffMaxMana * perCentBonus);
			return staffMaxMana;
		}

		private void ManaSwapped(Player self) {
			this.SoundTo(250, self);
			this.InvalidateAosToolTips();
		}

		public void ShowMana(Player self) {
			if (self != null) {
				double staffMaxMana = this.CalculateMaxMana(self);
				Globals.SrcWriteLine(string.Concat(
					Loc<ColoredStaffLoc>.Get(self.Language).manaInStaff, ": ",
					this.mana.ToString(), "/", 
					staffMaxMana.ToString()));
			}
		}

		public int Mana {
			get {
				return this.mana;
			}
			set {
				ushort newValue = (ushort) value;
				if (this.mana != newValue) {
					this.mana = newValue;
					this.InvalidateAosToolTips();
				}
			}
		}

		public override void On_BuildAosToolTips(AosToolTips opc, Language language) {
			base.On_BuildAosToolTips(opc, language);

			opc.AddNameColonValue(Loc<ColoredStaffLoc>.Get(language).manaInStaff,
				string.Concat(this.mana.ToString(), "/", this.TypeDef.MaxMana.ToString()));
		}
	}

	public class ColoredStaffLoc : CompiledLocStringCollection<ColoredStaffLoc> {
		public string manaInStaff = "Mana v holi";
		public string manaVanished = "Mana v holi je svázána s tvou myslí - odložením hole mana vyprchala.";
	}
}