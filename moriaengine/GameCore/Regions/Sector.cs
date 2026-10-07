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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using SteamEngine.Common;

namespace SteamEngine.Regions {
	public partial class Map {
		private class Sector {
			private ThingLinkedList things;
			private LinkedList<AbstractCharacter> players;
			private ThingLinkedList disconnects;//disconnected players/mounts/whatever
			private MultiComponentLinkedList multiComponents;
			private StaticSector staticSector;
			private readonly int sx;//first index in the sector 2d array
			private readonly int sy;//second index
			private readonly byte m;//mapplane
			private RegionRectangle[] rectangles = RegionRectangle.emptyArray;
			private LinkedList<RegionRectangle> dynamicRegionRects;

			//internal static Sector voidSector;
			//static Sector() {
			//    voidSector = new Sector(ushort.MaxValue, ushort.MaxValue);
			//    voidSector.players = List<AbstractCharacter>.ReadOnly(voidSector.players.read);
			//    voidSector.things = ArrayList.ReadOnly(voidSector.things);
			//    voidSector.disconnects = ArrayList.ReadOnly(voidSector.disconnects);
			//    //the void sector will complain if someone tries to add/remove things to/from it
			//}

			internal Sector(int sx, int sy, byte m) {
				this.sx = sx;
				this.sy = sy;
				this.m = m;
				this.ClearThings();
			}

			internal ThingLinkedList Things {
				get {
					return this.things;
				}
			}

			internal LinkedList<AbstractCharacter> Players {
				get {
					return this.players;
				}
			}

			internal ThingLinkedList Disconnects {
				get {
					return this.disconnects;
				}
			}

			internal MultiComponentLinkedList MultiComponents {
				get {
					return this.multiComponents;
				}
			}

			[SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
			public int Sx {
				get {
					return this.sx;
				}
			}

			[SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
			public int Sy {
				get {
					return this.sy;
				}
			}

			[SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
			public byte M {
				get {
					return this.m;
				}
			}

			public override string ToString() {
				return string.Format(CultureInfo.InvariantCulture,
					"sector with coords {0}, {1}, mapplane {2}",
					this.sx.ToString(CultureInfo.InvariantCulture),
					this.sy.ToString(CultureInfo.InvariantCulture),
					this.m.ToString(CultureInfo.InvariantCulture));
			}

			#region dynamic stuff
			internal void ClearThings() {
				this.things = new ThingLinkedList(this);
				this.players = new LinkedList<AbstractCharacter>();
				this.disconnects = new ThingLinkedList(this);
				this.multiComponents = new MultiComponentLinkedList();
			}

			internal void Add(Thing thing) {
				Sanity.IfTrueThrow(thing == null, "You can't tell us to add a NULL thing to the sector!");
				if (thing.Flag_Disconnected) {
					this.AddDisconnected(thing);
				} else {
					if (thing.IsPlayer) {
						this.AddPlayer((AbstractCharacter) thing);
					}
					this.AddThing(thing);
				}
			}

			internal void Remove(Thing thing) {
				Sanity.IfTrueThrow(thing == null, "You can't tell us to remove a NULL thing from the sector!");
				if (thing.contOrTLL == this.disconnects) {
					this.RemoveDisconnected(thing);
				} else {
					if (thing.IsPlayer) {
						this.RemovePlayer((AbstractCharacter) thing);
					}
					this.RemoveThing(thing);
				}
			}

			internal void Disconnected(Thing thing) {
				Sanity.IfTrueThrow(thing == null, "You can't tell us a NULL thing has disconnected!");
				Sanity.IfTrueThrow(thing.contOrTLL == this.disconnects, "Disconnected(" + thing + ") was called, but that thing is already in our list of disconnected stuff!");
				if (thing.IsPlayer) {
					this.RemovePlayer((AbstractCharacter) thing);
				}
				this.RemoveThing(thing);
				this.AddDisconnected(thing);
			}

			internal void Reconnected(Thing thing) {
				Sanity.IfTrueThrow(thing == null, "You can't tell us a NULL thing has reconnected!");
				Sanity.IfTrueThrow(thing.contOrTLL != this.disconnects, "Reconnected(" + thing + ") was called, but that thing is not in our list of disconnected stuff!");
				this.RemoveDisconnected(thing);
				if (thing.IsPlayer) {
					this.AddPlayer((AbstractCharacter) thing);
				}
				this.AddThing(thing);
			}

			[SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
			internal void MadeIntoNonPlayer(AbstractCharacter cre) {
				Sanity.IfTrueThrow(cre == null, "You can't tell us a NULL character is now a non-player!");
				Sanity.IfTrueThrow(cre.IsPlayer, "MadeIntoNonPlayer(" + cre + ") was called, but that character is actually still a player!");
				this.RemovePlayer(cre);
			}

			internal void MadeIntoPlayer(AbstractCharacter cre) {
				Sanity.IfTrueThrow(cre == null, "You can't tell us a NULL character is now a player!");
				Sanity.IfTrueThrow(!cre.IsPlayer, "MadeIntoPlayer(" + cre + ") was called, but that character isn't actually a player!");
				this.AddPlayer(cre);
			}

			private void AddThing(Thing thing) {
				Sanity.IfTrueThrow(thing.contOrTLL == this.things, "Sector's AddThing(" + thing + ") was called, but that thing is ALREADY in our list of things!");
				this.things.Add(thing);
			}

			private void AddPlayer(AbstractCharacter player) {
				Sanity.IfTrueThrow(this.players.Contains(player), "Sector's AddPlayer(" + player + ") was called, but that character is ALREADY in our list of players!");
				this.players.AddLast(player);
			}

			private void AddDisconnected(Thing thing) {
				Sanity.IfTrueThrow(thing.contOrTLL == this.disconnects, "Sector's AddDisconnected(" + thing + ") was called, but that thing is ALREADY in our list of disconnected things!");
				this.disconnects.Add(thing);
			}

			private void RemoveThing(Thing thing) {
				Sanity.IfTrueThrow(thing.contOrTLL != this.things,
					"Sector's RemoveThing(" + thing + ") was called, but that thing isn't in our list of things.");

				this.things.Remove(thing);
				thing.contOrTLL = null;
			}

			private void RemovePlayer(AbstractCharacter thing) {
				if (this.players.Remove(thing)) {
					Sanity.IfTrueThrow(this.players.Contains(thing), "Removed player still exists in ArrayList - It was in there multiple times?");
				} else {
					Sanity.IfTrueSay(true, "Sector's RemovePlayer(" + thing + ") was called, but that character isn't in our list of players.");
				}
			}

			internal void AddMultiComponent(MultiItemComponent component) {
				Sanity.IfTrueThrow(component == null, "You can't tell us to add a NULL MultiItemComponent to the sector!");
				Sanity.IfTrueThrow(component.collection != null, "You can't tell us to add a MultiItemComponent which is added elsewhere already!");

				this.multiComponents.Add(component);
				component.M = this.m;
			}

			internal void RemoveMultiComponent(MultiItemComponent component) {
				Sanity.IfTrueThrow(component == null, "You can't tell us to remove a NULL MultiItemComponent to the sector!");
				Sanity.IfTrueThrow(component.collection != this.multiComponents, "You can't tell us to remove a MultiItemComponent which is added elsewhere!");

				this.multiComponents.Remove(component);
			}

			private void RemoveDisconnected(Thing thing) {
				Sanity.IfTrueThrow(thing.contOrTLL != this.disconnects,
					"Sector's RemoveThing(" + thing + ") was called, but that thing isn't in our list of disconnects.");

				this.disconnects.Remove(thing);
				thing.contOrTLL = null;
			}
			#endregion dynamic stuff

			#region Static stuff
			private void LoadStatics() {
				if (this.staticSector == null) {
					this.staticSector = StaticSector.GetStaticSector(this.sx, this.sy, this.m);
				}
			}

			internal StaticItem[] Statics {
				get {
					this.LoadStatics();
					return this.staticSector.statics;
				}
			}

			internal StaticItem GetStatic(int x, int y, int z, int staticId) {
				this.LoadStatics();
				return this.staticSector.GetStatic(x, y, z, staticId);
			}

			internal bool HasStaticId(int x, int y, int staticId) {
				this.LoadStatics();
				return this.staticSector.HasStaticId(x, y, staticId);
			}

			internal int GetTileId(int x, int y) {
				this.LoadStatics();
				int basex = x & sectorAnd;
				int basey = y & sectorAnd;

				int relX = x - basex;
				int relY = y - basey;
				return this.staticSector.GetTileId(relX, relY);
			}

			internal int GetTileZ(int x, int y) {
				this.LoadStatics();
				int basex = x & sectorAnd;
				int basey = y & sectorAnd;

				int relX = x - basex;
				int relY = y - basey;
				return this.staticSector.GetTileZ(relX, relY);
			}

			internal void GetTile(int x, int y, out int z, out int id) {
				this.LoadStatics();
				int basex = x & sectorAnd;
				int basey = y & sectorAnd;

				int relX = x - basex;
				int relY = y - basey;

				z = this.staticSector.GetTileZ(relX, relY);
				id = this.staticSector.GetTileId(relX, relY);
			}

			#endregion Static stuff

			#region Regions
			public Region GetRegionFor(int x, int y) {
				if (this.dynamicRegionRects != null) {
					foreach (RegionRectangle dynamicRect in this.dynamicRegionRects) {
						if (dynamicRect.Contains(x, y)) {
							return dynamicRect.region;
						}
					}
				}
				for (int i = this.rectangles.Length - 1; i >= 0; i--) {
					RegionRectangle rect = this.rectangles[i];
					if (rect.Contains(x, y)) {
						return rect.region;
					}
				}
				return StaticRegion.WorldRegion;
			}

			internal void SetRegionRectangles(List<RegionRectangle> list) {
				if (list == null) {
					this.rectangles = RegionRectangle.emptyArray;
				} else {
					this.rectangles = list.ToArray();
					//ACHTUNG!!! do not use the width but send immediatelly the second coordinates!!!
					//ImmutableRectangle sectorRect = new ImmutableRectangle((ushort) (sx<<Map.sectorFactor), (ushort) (sy<<Map.sectorFactor),
					//	Map.sectorWidth, Map.sectorWidth);
					int startX = (this.sx << sectorFactor);
					int startY = (this.sy << sectorFactor);
					ImmutableRectangle sectorRect = new ImmutableRectangle(startX, startY,
						(startX + sectorWidth), (startY + sectorWidth));
					SectRectComparer comparer = new SectRectComparer(sectorRect);
					Array.Sort(this.rectangles, comparer);
				}
			}

			internal MultiItemComponent GetMultiComponent(int x, int y, int z, int staticId) {
				return this.multiComponents.Find(x, y, z, staticId);
			}

			private class SectRectComparer : IComparer {
				private ImmutableRectangle sectorRectangle;
				internal SectRectComparer(ImmutableRectangle sectorRectangle) {
					this.sectorRectangle = sectorRectangle;
				}

				//Less than zero: x is less than y.
				//Greater than zero: x is greater than y.
				public int Compare(object x, object y) {
					RegionRectangle a = (RegionRectangle) x;
					RegionRectangle b = (RegionRectangle) y;
					if (a.region.HierarchyIndex < b.region.HierarchyIndex) {
						return -1;
					}
					if (a.region.HierarchyIndex > b.region.HierarchyIndex) {
						return 1;
					}
					if (Globals.FastStartUp) {
						return 0;
					}
					ImmutableRectangle intersectA = ImmutableRectangle.GetIntersection(this.sectorRectangle, a);
					ImmutableRectangle intersectB = ImmutableRectangle.GetIntersection(this.sectorRectangle, b);
					return intersectA.TilesNumber.CompareTo(intersectB.TilesNumber);
				}
			}

			internal void ClearRegionRectangles(bool dynamicsToo) {
				this.rectangles = RegionRectangle.emptyArray;
				if (dynamicsToo) {
					this.dynamicRegionRects = null;
				}
			}

			///// <summary>Used only for one region - we will remove only its rectangles (used e.g. when editing one region through the dialog)</summary>
			//internal void ClearOneRegionRectangles(Region whichRegion) {
			//    List<RegionRectangle> copyRects = new List<RegionRectangle>(this.rectangles);
			//    foreach (RegionRectangle regRect in this.rectangles) {
			//        if (regRect.region == whichRegion) {
			//            copyRects.Remove(regRect); //remove it from the temporary list
			//        }
			//    }
			//    RegionRectangle[] restRects = new RegionRectangle[copyRects.Count];
			//    for (int i = 0; i < copyRects.Count; i++) { //those who are left will be set back as a new array
			//        restRects[i] = copyRects[i];
			//    }
			//    this.rectangles = restRects;
			//}

			//Simple getter
			internal LinkedList<RegionRectangle> RegionRectangles {
				get {
					return this.dynamicRegionRects;
				}
			}
			#endregion Regions

			#region Dynamic Regions
			internal bool AddDynamicRegionRect(RegionRectangle rect, bool performControls) {
				if (this.dynamicRegionRects == null) {
					this.dynamicRegionRects = new LinkedList<RegionRectangle>();
				}
				if (performControls) {
					//check whethre other dynamic region rectangles do not intersect with the currently inserted one
					foreach (RegionRectangle existingRect in this.dynamicRegionRects) {
						if (existingRect.IntersectsWith(rect)) {
							return false; //problem here, stop trying !
						}
					}
					this.dynamicRegionRects.AddFirst(rect);
					return true;
				}
				this.dynamicRegionRects.AddFirst(rect);
				return true; //always OK
			}

			internal void RemoveDynamicRegionRect(RegionRectangle rect) {
				if (this.dynamicRegionRects == null) {
					return;
				}
				this.dynamicRegionRects.Remove(rect);
				if (this.dynamicRegionRects.Count == 0) {
					this.dynamicRegionRects = null;
				}
			}
			#endregion
		}
	}
}