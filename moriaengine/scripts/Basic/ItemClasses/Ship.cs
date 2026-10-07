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

using System.Collections.Generic;
using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {
	[ViewableClass]
	public partial class ShipDef : MultiItemDef {


		DMICDLoadHelper[] shipComponentsHelpers = new DMICDLoadHelper[4];
		DynamicMultiItemComponentDescription[] shipComponentsDescs;

		//N/E/S/W
		private static readonly short[] tillerModels = { 0x3e4e, 0x3e55, 0x3e4b, 0x3e50 };
		private static readonly short[] leftPlankModels = { 0x3eb2, 0x3e85, 0x3eb2, 0x3e85 };
		private static readonly short[] rightPlankModels = { 0x3eb1, 0x3e8a, 0x3eb1, 0x3e8a };
		private static readonly short[] trunkModels = { 0x3eae, 0x3e65, 0x3eb9, 0x3e93 };

		protected override void LoadScriptLine(string filename, int line, string param, string args) {
			switch (param) {
				case "tiller":
					this.shipComponentsHelpers[0] = new DMICDLoadHelper(filename, line, args);
					return;
				case "leftplank":
				case "leftdoor":
					this.shipComponentsHelpers[1] = new DMICDLoadHelper(filename, line, args);
					return;
				case "rightplank":
				case "rightdoor":
					this.shipComponentsHelpers[2] = new DMICDLoadHelper(filename, line, args);
					return;
				case "trunk":
				case "hatch":
					this.shipComponentsHelpers[3] = new DMICDLoadHelper(filename, line, args);
					return;
			}
			base.LoadScriptLine(filename, line, param, args);
		}

		public override void Unload() {
			base.Unload();
			this.shipComponentsHelpers = new DMICDLoadHelper[4];
			this.shipComponentsDescs = null;
		}

		protected override void On_Create(Thing t) {
			base.On_Create(t);
			if (this.shipComponentsDescs == null) {
				this.shipComponentsDescs = new DynamicMultiItemComponentDescription[4];
				this.ResolveDMICDLoadHelper(0, tillerModels);
				this.ResolveDMICDLoadHelper(1, leftPlankModels);
				this.ResolveDMICDLoadHelper(2, rightPlankModels);
				this.ResolveDMICDLoadHelper(3, trunkModels);
			}
			Ship ship = (Ship) t;
			this.CreateShipComponent(this.shipComponentsDescs[0], ship, ref ship.tiller);
			this.CreateShipComponent(this.shipComponentsDescs[1], ship, ref ship.leftPlank);
			this.CreateShipComponent(this.shipComponentsDescs[2], ship, ref ship.rightPlank);
			this.CreateShipComponent(this.shipComponentsDescs[3], ship, ref ship.trunk);
			ship.facing = this.Facing;
		}

		private void ResolveDMICDLoadHelper(int i, short[] models) {
			DMICDLoadHelper helper = this.shipComponentsHelpers[i];
			if (helper != null) {
				helper.args = string.Concat(models[(int) this.Facing], ",", helper.args);
				this.shipComponentsDescs[i] = helper.Resolve();
			}
		}

		private void CreateShipComponent(DynamicMultiItemComponentDescription dmicd, Ship ship, ref Item item) {
			if (dmicd != null) {
				item = dmicd.Create(ship.X, ship.Y, ship.Z, ship.M);
			}
		}
	}

	public enum ShipFacing
	{
		North = 0,
		East = 1,
		South = 2,
		West = 3
	}

	public enum ShipMovementDir {
		Forward,
		ForwardLeft,
		ForwardRight,
		Backward,
		BackwardLeft,
		BackwardRight,
		Left,
		Right,
		Port = Left,
		Starboard = Right
	}

	[ViewableClass]
	public partial class Ship : MultiItem {

		internal override void InitMultiRegion() {
			int n = this.TypeDef.rectangleHelpers.Count;
			if (n > 0) {
				ImmutableRectangle[] newRectangles = new ImmutableRectangle[n];
				for (int i = 0; i < n; i++) {
					newRectangles[i] = this.TypeDef.rectangleHelpers[i].CreateRect(this);
				}
				this.region = new ShipRegion(this, newRectangles);
				// TODO - pouzit region.Place(P()) a v pripade false poresit co delat s neuspechem!!
			}
		}


		internal void AddThing(Thing t) {
			if (this.carriedThings == null) {
				this.carriedThings = new LinkedList<Thing>();
			}
			this.carriedThings.AddFirst(t);
		}

		internal void RemoveThing(Thing t) {
			if (this.carriedThings != null) {
				this.carriedThings.Remove(t);
				if (this.carriedThings.Count == 0) {
					this.carriedThings = null;
				}
			}
		}

		public void HandleCommand(Character from, int[] keywords) {
			if ((keywords == null) || (keywords.Length == 0)) {
				return;
			}
			foreach (int keyword in keywords) {
				if (keyword >= 0x42 && keyword <= 0x6B) {
					switch (keyword) {
						//case 66://set name*
						//    this.SetName(e); return;
						//case 67://remove name
						//    this.RemoveName(e.Mobile); return;
						//case 68://name
						//    this.GiveName(e.Mobile); return;
						case 69://forward
							this.StartMoving(ShipMovementDir.Forward, true); return;
						case 70://back(ward(s))
							this.StartMoving(ShipMovementDir.Backward, true); return;
						case 71://(drift )left
							this.StartMoving(ShipMovementDir.Left, true); return;
						case 72://(drift ) right
							this.StartMoving(ShipMovementDir.Right, true); return;
						case 73://starboard
						case 101://turn right
							this.StartTurning(ShipMovementDir.Right); return;
						case 74://port
						case 102://turn left
							this.StartTurning(ShipMovementDir.Left); return;
						case 75://forward left
							this.StartMoving(ShipMovementDir.ForwardLeft, true); return;
						case 76://forward right
							this.StartMoving(ShipMovementDir.ForwardRight, true); return;
						case 77://backward left
							this.StartMoving(ShipMovementDir.BackwardLeft, true); return;
						case 78://back(ward(s)) left
							this.StartMoving(ShipMovementDir.BackwardRight, true); return;
						case 79://stop
							this.StopMoving(); return;
						case 80://slow left
							this.StartMoving(ShipMovementDir.Left, false); return;
						case 81:
							this.StartMoving(ShipMovementDir.Right, false); return;
						case 82:
							this.StartMoving(ShipMovementDir.Forward, false); return;
						case 83:
							this.StartMoving(ShipMovementDir.Backward, false); return;
						case 84:
							this.StartMoving(ShipMovementDir.ForwardLeft, false); return;
						case 85:
							this.StartMoving(ShipMovementDir.ForwardRight, false); return;
						case 86:
							this.StartMoving(ShipMovementDir.BackwardRight, false); return;
						case 87:
							this.StartMoving(ShipMovementDir.BackwardLeft, false); return;
						case 88://one left/left one
							this.OneMovement(ShipMovementDir.Left); return;
						case 89:
							this.OneMovement(ShipMovementDir.Right); return;
						case 90:
							this.OneMovement(ShipMovementDir.Forward); return;
						case 91:
							this.OneMovement(ShipMovementDir.Backward); return;
						case 92:
							this.OneMovement(ShipMovementDir.ForwardLeft); return;
						case 93:
							this.OneMovement(ShipMovementDir.ForwardRight); return;
						case 94:
							this.OneMovement(ShipMovementDir.BackwardRight); return;
						case 95:
							this.OneMovement(ShipMovementDir.BackwardLeft); return;
						//case 96:
						//    this.GiveNavPoint(); return;
						//case 97://nav
						//    this.NextNavPoint = 0;
						//    this.StartCourse(false, true); return;
						//case 98://continue
						//    this.StartCourse(false, true); return;
						//case 99://goto*
						//    this.StartCourse(e.Speech, false, true); return;
						//case 100://single*
						//    this.StartCourse(e.Speech, true, true); return;
						case 103://turn around/come about
							this.StartTurning(ShipMovementDir.Backward); return;
						case 104://unfurl sail
							this.StartMoving(ShipMovementDir.Forward, true); return;
						case 105://furl sail
							this.StopMoving(); return;
						case 106://drop anchor/lower anchor
							this.LowerAnchor(); return;
						case 107://raise anchor/lift anchor/hoist anchor
							this.RaiseAnchor(); return;
					}
					return;
				}
			}
		}

		public void StartMoving(ShipMovementDir shipDir, bool fast) {

		}

		public void OneMovement(ShipMovementDir shipDir) {

		}

		public void StopMoving() {

		}

		public void StartTurning(ShipMovementDir shipDir) {

		}

		public void LowerAnchor() {

		}

		public void RaiseAnchor() {

		}

		public Item Tiller {
			get {
				return this.tiller;
			}
		}

		public Item LeftPlank {
			get {
				return this.leftPlank;
			}
		}

		public Item RightPlank {
			get {
				return this.rightPlank;
			}
		}

		public Item Trunk {
			get {
				return this.trunk;
			}
		}

		public ShipFacing Facing {
			get {
				return this.facing;
			}
			//set {
			//    this.facing.CurrentValue = value;
			//}
		}

		public override void On_Destroy() {
			base.On_Destroy();
			if (this.tiller != null) {
				this.tiller.Delete();
			}
			if (this.leftPlank != null) {
				this.leftPlank.Delete();
			}
			if (this.rightPlank != null) {
				this.rightPlank.Delete();
			}
			if (this.trunk != null) {
				this.trunk.Delete();
			}
		}
	}

	[ViewableClass]
	public class ShipRegion : MultiRegion {
		public ShipRegion() {
			throw new SEException("The constructor without paramaters is not supported");
		}

		public ShipRegion(Ship ship, ImmutableRectangle[] rectangles)
			: base(ship, rectangles) {
		}

		public Ship Ship {
			get {
				return (Ship) this.multiItem;
			}
		}

		public override TriggerResult On_Enter(AbstractCharacter ch, bool forced) {
			var result = base.On_Enter(ch, forced);
			if (result != TriggerResult.Cancel || forced) {
				this.Ship.AddThing(ch);
				ch.AddTriggerGroup(SingletonScript<E_BeingOnShip>.Instance);
			}
			return result;
		}

		public override TriggerResult On_Exit(AbstractCharacter ch, bool forced) {
			var result = base.On_Exit(ch, forced);
			if (result != TriggerResult.Cancel || forced) {
				this.Ship.RemoveThing(ch);
				ch.RemoveTriggerGroup(SingletonScript<E_BeingOnShip>.Instance);
			}
			return result;
		}

		public class E_BeingOnShip : CompiledTriggerGroup {
			private Ship GetShipOnPoint(Map map, int x, int y) {
				Region region = map.GetRegionFor(x, y);
				ShipRegion shipRegion = region as ShipRegion;
				if (shipRegion != null) {
					return shipRegion.Ship;
				}
				return null;
			}

			public int On_ItemPickUp_Ground(Character self, Item pickedUp) {
				Ship ship = this.GetShipOnPoint(self.GetMap(), pickedUp.X, pickedUp.Y);
				if (ship != null) {
					ship.RemoveThing(pickedUp);
				} else {
					self.RemoveTriggerGroup(SingletonScript<E_BeingOnShip>.Instance);
				}
				return 0;
			}

			public int On_ItemDropOn_Ground(Character self, Item dropped, ushort x, ushort y, sbyte z) {
				Ship ship = this.GetShipOnPoint(self.GetMap(), x, y);
				if (ship != null) {
					ship.AddThing(dropped);
				} else {
					self.RemoveTriggerGroup(SingletonScript<E_BeingOnShip>.Instance);
				}
				return 0;
			}

			public int On_Say(Character self, string speech, SpeechType type, int[] keywords) {
				Ship ship = this.GetShipOnPoint(self.GetMap(), self.X, self.Y);
				if (ship != null) {
					ship.HandleCommand(self, keywords);
				} else {
					self.RemoveTriggerGroup(SingletonScript<E_BeingOnShip>.Instance);
				}
				return 0;
			}
		}
	}
}
