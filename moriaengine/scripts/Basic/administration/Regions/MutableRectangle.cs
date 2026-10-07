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
using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Persistence;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {
	/// <summary>
	/// Rectangle class for dialogs - the mutable one. It will be used for operating with " 
	/// "rectangles when editing region. After setting to the region it will be transformed to normal RegionRectangle
	/// </summary>
	[ViewableClass]
	[SaveableClass]
	public class MutableRectangle : AbstractRectangle {
		public int minX, minY, maxX, maxY;

		[LoadingInitializer]
		public MutableRectangle() {
		}

		public MutableRectangle(AbstractRectangle copiedOne) {
			this.minX = copiedOne.MinX;
			this.minY = copiedOne.MinY;
			this.maxX = copiedOne.MaxX;
			this.maxY = copiedOne.MaxY;
		}

		/// <summary>
		/// Return a rectangle created from the central point with the specific range around the point (square 'around' it)
		/// </summary>
		public MutableRectangle(int x, int y, int range) {
			this.minX = x - range;
			this.minY = y - range;
			this.maxX = x + range;
			this.maxY = y + range;
		}

		/// <summary>Create a rectangle using the center point and the area around (=>square)</summary>
		public MutableRectangle(IPoint4D center, int range)
			: this(center.X, center.Y, range) {
		}

		public MutableRectangle(int startX, int startY, int endX, int endY) {
			Sanity.IfTrueThrow((startX > endX) || (startY > endY),
				"MutableRectangle (" + startX + "," + startY + "," + endX + "," + endY + "). The first two arguments are supposed to be the upper left corner coordinates while the 3rd and 4th arguments coordinates of the lower right corner.");
			this.minX = startX;
			this.minY = startY;
			this.maxX = endX;
			this.maxY = endY;
		}

		public override int MinX {
			get {
				return this.minX;
			}
		}

		public override int MinY {
			get {
				return this.minY;
			}
		}

		public override int MaxX {
			get {
				return this.maxX;
			}
		}

		public override int MaxY {
			get {
				return this.maxY;
			}
		}

		/// <summary>Alters all four rectangle's position coordinates for specified tiles in X and Y axes.</summary>
		public MutableRectangle Move(int timesX, int timesY) {
			this.minX += (ushort) timesX;
			this.maxX += (ushort) timesX;
			this.minY += (ushort) timesY;
			this.maxY += (ushort) timesY;

			return this;
		}

		/// <summary>Takes the regions rectagles and makes a list of MutableRectangles for usage (copies the immutable ones)</summary>
		public static List<MutableRectangle> TakeRectsFromRegion(Region reg) {
			List<MutableRectangle> retList = new List<MutableRectangle>();
			foreach (ImmutableRectangle regRect in reg.Rectangles) {
				retList.Add(new MutableRectangle(regRect));
			}
			return retList;
		}

		[LoadLine]
		public void LoadLine(string filename, int line, string valueName, string valueString) {
			switch (valueName.ToLowerInvariant()) {
				case "minx":
					this.minX = (ushort) ObjectSaver.OptimizedLoad_SimpleType(valueString, typeof(ushort));
					return;
				case "miny":
					this.minY = (ushort) ObjectSaver.OptimizedLoad_SimpleType(valueString, typeof(ushort));
					return;
				case "maxx":
					this.maxX = (ushort) ObjectSaver.OptimizedLoad_SimpleType(valueString, typeof(ushort));
					return;
				case "maxy":
					this.maxX = (ushort) ObjectSaver.OptimizedLoad_SimpleType(valueString, typeof(ushort));
					return;
			}
			throw new ScriptException("Invalid data '" + LogStr.Ident(valueName) + "' = '" + LogStr.Number(valueString) + "'.");
		}

		[Save]
		public void Save(SaveStream output) {
			output.WriteValue("minX", this.minX);
			output.WriteValue("minY", this.minY);
			output.WriteValue("maxX", this.maxX);
			output.WriteValue("maxY", this.maxY);
		}
	}
}