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
using SteamEngine.Generators.Abstractions;
using SteamEngine.Persistence;

namespace SteamEngine.CompiledScripts {
	/// <summary>#927 - what a potion does is its PotionKind, found by its def: see PotionKinds.</summary>
	[ViewableClass]
	[ThingTemplate]
	public partial class Potion : Item {
		public override void On_DClick(AbstractCharacter dclicker) {
			Character user = dclicker as Character;
			PotionKind kind = PotionKinds.Find(this);
			if ((user != null) && (kind != null) && !MysticHypnosis.RefusesUse(user, this)) {
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

		//#1558 - who is dragging this potion and where it was lifted from the ground, for the commit steps below (never saved:
		//both only last from one step of a pickup or drop to the next)
		private Character draggedBy;
		private Point4D liftedFrom;

		/// <summary>Leaving the ground: where it lay, for a bomb that goes off once lifted.</summary>
		public override void On_LeaveRegion(ItemOnGroundArgs args) {
			base.On_LeaveRegion(args);
			this.liftedFrom = args.Point;
		}

		/// <summary>Leaving a character's hand (the dragging layer): who dragged it, if it lands on the ground next.</summary>
		public override void On_LeaveChar(ItemInCharArgs args) {
			base.On_LeaveChar(args);
			this.draggedBy = (args.Layer == (int) LayerNames.Dragging) ? args.Cont as Character : null;
		}

		/// <summary>Sphere's @PICKUP_GROUND (Erebor's t_potion_bomba): a bomb lifted from the ground goes off where it lay.
		/// #1558 - after the pickup, no longer inside its deny step; at once, but out of the pickup's own steps, which
		/// can't have the item deleted under them.</summary>
		public override void On_EnterChar(ItemInCharArgs args) {
			base.On_EnterChar(args);
			this.draggedBy = null;
			Point4D from = this.liftedFrom;
			this.liftedFrom = null;
			if ((from != null) && (args.Layer == (int) LayerNames.Dragging) && (PotionKinds.Find(this) is BombPotionKind)) {
				this.AddTimer(BombLiftedTimer.Key, new BombLiftedTimer { x = from.X, y = from.Y, z = from.Z, m = from.M }).DueInSeconds = 0;
			}
		}

		public override void On_EnterItem(ItemInItemArgs args) {
			base.On_EnterItem(args);
			this.draggedBy = null;
			this.liftedFrom = null;
		}

		/// <summary>Sphere's @DROPON_GROUND: put down by who dragged it, a bomb starts counting, a dragon cure breaks.
		/// #1558 - after the drop (the regions and the reach check may still refuse it before), no longer inside its
		/// deny step, where a bomb still in the hand went off as a bomb in the pack.</summary>
		public override void On_EnterRegion(ItemOnGroundArgs args) {
			base.On_EnterRegion(args);
			Character by = this.draggedBy;
			this.draggedBy = null;
			this.liftedFrom = null;
			if (by != null) {
				PotionKinds.Find(this)?.On_PutOnGround(by, this);
			}
		}
	}

	[ViewableClass]
	[ThingDefTemplate]
	public partial class PotionDef : ItemDef {
		[DefField(typeof(ItemDef))]
		private FieldValue emptyFlask;
	}
}
