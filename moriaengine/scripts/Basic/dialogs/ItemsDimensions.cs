using System.IO;
using SteamEngine.Common;
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

namespace SteamEngine.CompiledScripts.Dialogs {
	public class GumpDimensions {
		private static readonly string datafilePath = "/Basic/dialogs/Bounds.bin";

		private static GumpArtDimension[] bounds;

		public static GumpArtDimension[] Table {
			get {
				return bounds;
			}
		}

		static GumpDimensions() {
			if (File.Exists(Globals.ScriptsPath + datafilePath)) {
				using (FileStream fs = new FileStream(Globals.ScriptsPath + datafilePath, FileMode.Open, FileAccess.Read, FileShare.Read)) {
					BinaryReader bin = new BinaryReader(fs);

					bounds = new GumpArtDimension[0x4000];

					for (int i = 0; i < 0x4000; ++i) {
						int xMin = bin.ReadInt16();
						int yMin = bin.ReadInt16();
						int xMax = bin.ReadInt16();
						int yMax = bin.ReadInt16();

						bounds[i].Set(xMin, yMin, (xMax - xMin) + 1, (yMax - yMin) + 1);
					}

					bin.Close();
				}
			} else {
				LogStr.Warning("Warning: " + datafilePath + " does not exist");

				bounds = new GumpArtDimension[0x4000];
			}
		}
	}

	public struct GumpArtDimension {
		private Point2D start, end;

		public void Set(int x, int y, int width, int height) {
			this.start = new Point2D((ushort) x, (ushort) y);
			this.end = new Point2D((ushort) (x + width), (ushort) (y + height));
		}

		public int X {
			get {
				return this.start.X;
			}
		}

		public int Y {
			get {
				return this.start.Y;
			}
		}

		public int Width {
			get {
				return this.end.X - this.start.X;
			}
		}

		public int Height {
			get {
				return this.end.Y - this.start.Y;
			}
		}
	}
}