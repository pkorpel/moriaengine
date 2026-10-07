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

using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using SteamEngine.Common;

namespace SteamEngine.Regions {

	internal struct MutablePoint3D {
		internal ushort x;
		internal ushort y;
		internal sbyte z;

		internal void SetXYZ(int x, int y, int z) {
			this.x = (ushort) x;
			this.y = (ushort) y;
			this.z = (sbyte) z;
		}

		internal void SetXYZ(ushort x, ushort y, sbyte z) {
			this.x = x;
			this.y = y;
			this.z = z;
		}

		internal void SetXYZ(MutablePoint3D p) {
			this.x = p.x;
			this.y = p.y;
			this.z = p.z;
		}
	}


	internal struct MutablePoint4D {
		internal ushort x;
		internal ushort y;
		internal sbyte z;
		internal byte m;

		[SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
		public static bool Equals(MutablePoint4D a, MutablePoint4D b) {
			return ((a.x == b.x) && (a.y == b.y) && (a.z == b.z) && (a.m == b.m));
		}

		internal void SetParsedP(string value) {
			Match match = Point4D.positionRE.Match(value);
			if (match.Success) {
				GroupCollection gc = match.Groups;
				int x = ConvertTools.ParseUInt16(gc["x"].Value);
				int y = ConvertTools.ParseUInt16(gc["y"].Value);
				string zstr = gc["z"].Value;
				string mstr = gc["m"].Value;
				int z;
				byte m;
				if (zstr.Length > 0) {
					z = ConvertTools.ParseSByte(zstr);
					if (mstr.Length > 0) {
						m = ConvertTools.ParseByte(mstr);
					} else {
						m = 0;
					}
				} else {
					z = 0;
					m = 0;
				}

				this.SetXYZM(x, y, z, m);
			} else {
				throw new SEException("Invalid input string for Point4D parse: '" + value + "'");
			}
		}

		internal void SetXYZM(int x, int y, int z, int m) {
			this.x = (ushort) x;
			this.y = (ushort) y;
			this.z = (sbyte) z;
			this.m = (byte) m;
		}

		internal void SetXYZM(int x, int y, int z, byte m) {
			this.x = (ushort) x;
			this.y = (ushort) y;
			this.z = (sbyte) z;
			this.m = m;
		}

		internal void SetXYZM(ushort x, ushort y, sbyte z, byte m) {
			this.x = x;
			this.y = y;
			this.z = z;
			this.m = m;
		}

		internal void SetXYZM(IPoint4D p) {
			this.SetXYZM(p.X, p.Y, p.Z, p.M);
		}

		internal void SetXYZM(MutablePoint4D p) {
			this.SetXYZM(p.x, p.y, p.z, p.m);
		}

		internal void SetXYZ(int x, int y, int z) {
			this.x = (ushort) x;
			this.y = (ushort) y;
			this.z = (sbyte) z;
		}

		internal void SetXYZ(ushort x, ushort y, sbyte z) {
			this.x = x;
			this.y = y;
			this.z = z;
		}

		internal void SetXY(int x, int y) {
			this.x = (ushort) x;
			this.y = (ushort) y;
		}

		internal void SetXY(ushort x, ushort y) {
			this.x = x;
			this.y = y;
		}
	}
}