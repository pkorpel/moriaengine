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
using System.Diagnostics.CodeAnalysis;
using SteamEngine.Persistence;

namespace SteamEngine.Regions {
	public class DynamicRegion : Region {
		public DynamicRegion() {
			throw new SEException("The constructor without paramaters is not supported");
		}

		public DynamicRegion(ImmutableRectangle[] newRects)
		{

			int n = newRects.Length;
			this.rectangles = new RegionRectangle[n];
			for (int i = 0; i < n; i++) {
				this.rectangles[i] = new RegionRectangle(newRects[i], this);
			}
		}

		public override string Name {
			get {
				return "DynamicRegion";
			}
		}

		/// <summary>
		/// Serves to place the region to the map for the first time (after creation)
		/// </summary>
		[SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods")]
		public bool Place(Point4D p) {
			this.ThrowIfDeleted();
			if (p != null) { //already placed!
				throw new SEException("This Dynamic region has already been placed to the map. For movement try setting its P");
			}
			this.InternalSetP(p);

			Map map = Map.GetMap(p.M);
			this.Parent = map.GetRegionFor(p);

			return map.AddDynamicRegion(this, true); //add it to the map, but try if there is no other obstacle!
		}

		public override Point4D P {
			get {
				return base.P;
			}
			set {
				this.ThrowIfDeleted();
				if (value == null) {
					throw new SEException("P is null");
				}
				if (this.Step(value)) { //first move to the desired position after performing necessary checks
					//(trying to move over another dynamic region causes movement to fail!)
					base.P = value;
				}
			}
		}

		/// <summary>
		/// Tries to move the specified amount of fields in X and Y axes. First examines if it is possible
		/// to move that way to the desired location and if so, it moves every rectangle there.
		/// We expect the timesX and timesY parameteres to be small numbers
		/// </summary>
		/// <param name="xDiff">The x diff.</param>
		/// <param name="yDiff">The y diff.</param>
		/// <returns></returns>
		public bool Step(int xDiff, int yDiff) {
			//a new list of changed (moved) rectangles
			List<RegionRectangle> movedRects = new List<RegionRectangle>();
			foreach (RegionRectangle oneRect in this.rectangles) {
				movedRects.Add(oneRect.CloneMoved(xDiff, yDiff));
			}
			Point4D oldP = this.P;
			Map oldMap = Map.GetMap(oldP.M); //the dynamic region's old Map
			oldMap.RemoveDynamicRegion(this);//remove it anyways
			bool result = this.SetRectangles(movedRects, oldMap);
			//add it without checks (these were performed when setting the rectangles)
			//we will add either the new array of rectangles or the old one if there were problems
			oldMap.AddDynamicRegion(this, false);
			if (result) { //OK - alter the position also
				this.InternalSetP(new Point4D((ushort) (oldP.X + xDiff), (ushort) (oldP.Y + yDiff), oldP.Z, oldP.M));
			}
			return result;
		}

		/// <summary>
		/// Method called on position change - it recounts the region's rectangles' position and also makes 
		/// sure that no conflicts with other dynamic regions occur when moving!
		/// </summary>
		private bool Step(Point4D newP) {
			Point4D oldPos = this.P; //store the old position for case the movement fails!

			bool xyChanged = (oldPos.X != newP.X || oldPos.Y != newP.Y);
			bool mapChanged = oldPos.M != newP.M;

			Map oldMap = Map.GetMap(oldPos.M); //the dynamic region's old Map
			oldMap.RemoveDynamicRegion(this);//remove it anyways
			bool movingOK = true;//indicator if the movement success
			if (xyChanged) {
				int diffX = newP.X - oldPos.X;
				int diffY = newP.Y - oldPos.Y;
				List<RegionRectangle> movedRects = new List<RegionRectangle>();
				foreach (RegionRectangle oneRect in this.rectangles) {
					movedRects.Add(oneRect.CloneMoved(diffX, diffY));
				}
				if (mapChanged) {
					Map newMap = Map.GetMap(newP.M);
					this.Parent = newMap.GetRegionFor(newP);
					movingOK = this.SetRectangles(movedRects, newMap);
					newMap.AddDynamicRegion(this, false);//place the region to the map (no checks, they were already performed in SetRectangles)
				} else {
					this.Parent = oldMap.GetRegionFor(newP);
					movingOK = this.SetRectangles(movedRects, oldMap);
					oldMap.AddDynamicRegion(this, false);//and place (no checks as well)
				}
			} else if (mapChanged) {
				Map newMap = Map.GetMap(newP.M);
				this.Parent = newMap.GetRegionFor(newP);
				movingOK = this.SetRectangles(this.rectangles, newMap); //here set the old rectangles (but to the new map)
				newMap.AddDynamicRegion(this, false);//place, still no checks
			} else { //nothing at all :) - set to the same position
				oldMap.AddDynamicRegion(this, false);//return it			
			}
			if (!movingOK) {
				//return the parent and place the region to the old position to the map without checkings!
				this.Parent = oldMap.GetRegionFor(oldPos);
				oldMap.AddDynamicRegion(this, false);//return
			}
			return movingOK;
		}

		/// <summary>
		/// Take the list of rectangles and make an array of RegionRectangles of it.
		/// The purpose is the same as for StaticRegion but the checks are different.
		/// The map parameter allows us to specifiy the map where the region should be
		/// </summary>
		[SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter")]
		public bool SetRectangles<T>(IList<T> list, Map map) where T : ImmutableRectangle {
			RegionRectangle[] newArr = new RegionRectangle[list.Count];
			for (int i = 0; i < list.Count; i++) {
				//take the start/end point from the IRectangle and create a new RegionRectangle
				newArr[i] = new RegionRectangle(list[i].MinX, list[i].MinY, list[i].MaxX, list[i].MaxY, this);
			}
			//now the checking phase!
			this.inactivated = true;
			foreach (RegionRectangle rect in newArr) {
				if (!map.CheckDynRectIntersection(rect)) {
					//check the intercesction of the dynamic region, in case of any trouble immediatelly finish
					return false;
				}
			}
			//everything is OK, we can swith the lists
			this.rectangles = newArr;
			this.inactivated = false;
			return true;
		}

		public override void Delete() {
			Map.GetMap(this.P.M).RemoveDynamicRegion(this);
			base.Delete();
		}

		public sealed override void Save(SaveStream output) {
			throw new SEException("Dynamic regions are not supposed to be saved");
		}

		public sealed override void LoadLine(string filename, int line, string valueName, string valueString) {
			throw new SEException("Dynamic regions are not supposed to be loaded");
		}
	}
}
