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

namespace SteamEngine.CompiledScripts {
	/// <summary>#927 - what a potion does is its PotionKind, found by its def: see PotionKinds.</summary>
	[ViewableClass]
	public partial class Potion {
		public override void On_DClick(AbstractCharacter dclicker) {
			Character user = dclicker as Character;
			PotionKind kind = PotionKinds.Find(this);
			if ((user != null) && (kind != null)) {
				kind.Use(user, this);
			}
		}

		public Item CreateEmptyFlask(Container container) {
			ItemDef emptyFlaskDef = this.TypeDef.EmptyFlask;
			if (emptyFlaskDef != null) {
				return (Item) container.NewItem(emptyFlaskDef);
			}
			return null;
		}

		/// <summary>A bomb in the open goes off in the hands of whoever lifts it.</summary>
		public override TriggerResult On_DenyPickup(DenyPickupArgs args) {
			TriggerResult result = base.On_DenyPickup(args);
			if ((result != TriggerResult.Cancel) && this.IsOnGround && (PotionKinds.Find(this) is BombPotionKind bomb)) {
				bomb.Explode(this);
				args.Result = DenyResultMessages.Deny_NoMessage;
				return TriggerResult.Cancel;
			}
			return result;
		}

		public override TriggerResult On_DenyPutOnGround(DenyPutOnGroundArgs args) {
			PotionKinds.Find(this)?.On_PutOnGround(args.Argv[0] as Character, this);
			return base.On_DenyPutOnGround(args);
		}
	}

	[ViewableClass]
	public partial class PotionDef {
	}
}
