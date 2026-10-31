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

using SteamEngine.CompiledScripts.Dialogs;

using System.Globalization;
using SteamEngine.Common;
namespace SteamEngine.CompiledScripts {
	[ViewableClass]
	public partial class DestroyableDef : EquippableDef {

	}

	public class DestroyableLoc : CompiledLocStringCollection<DestroyableLoc> {
		internal string IsDestroyed = "{0} je zniceno";
		internal string MayBeDamaged = "{0} mozna utrpelo poskozeni";
	}

	[ViewableClass]
	public partial class Destroyable : Equippable {

		public ushort Durability {
			get {
				return this.durability;
			}
			set {
				if (value != this.durability) {
					this.durability = value;
					Character cont = this.Cont as Character;
					if (cont != null) {
						if (this is Weapon) {
							cont.InvalidateCombatWeaponValues();
						} else if (this is Wearable) {
							cont.InvalidateCombatArmorValues();
						}
					}
				}
			}
		}

		public ushort MaxDurability {
			get {
				return this.maxDurability;
			}
			set {
				if (value != this.maxDurability) {
					this.maxDurability = value;
					Character cont = this.Cont as Character;
					if (cont != null) {
						if (this is Weapon) {
							cont.InvalidateCombatWeaponValues();
						} else if (this is Wearable) {
							cont.InvalidateCombatArmorValues();
						}
					}
				}
			}
		}

		public override void On_Create() {
			base.On_Create();
			this.maxDurability = this.TypeDef.MaxDurability; //set the initial maximal durability from the Def
			this.durability = this.maxDurability;
		}

		/// <summary>#865 - Sphere's CItem::OnTakeDamage: one point of wear at damage in (max * 16),
		/// and at the last point the item is gone.</summary>
		public void TakeWear(int damage, Character source) {
			if ((damage <= 0) || (Globals.dice.Next(this.MaxDurability * 16) > damage)) {
				return;
			}
			Character wearer = this.TopObj() as Character;
			if (this.Durability <= 1) {
				this.Durability = 0;
				if (wearer != null) {
					wearer.SysMessage(string.Format(CultureInfo.InvariantCulture, Loc<DestroyableLoc>.Get(wearer.Language).IsDestroyed, this.Name));
				}
				this.Delete();
				return;
			}
			this.Durability = (ushort) (this.Durability - 1);
			if ((wearer != null) && (wearer != source)) {
				wearer.SysMessage(string.Format(CultureInfo.InvariantCulture, Loc<DestroyableLoc>.Get(wearer.Language).MayBeDamaged, this.Name));
			}
		}
	}
}