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
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using SteamEngine.Common;
using SteamEngine.Regions;
//using System.IO;

namespace SteamEngine {
	public interface IPoint2D {
		int X { get; } //Map X coordinate (Low is west, high is east)
		int Y { get; } //Map Y coordinate (Low is north, high is south)
		IPoint2D TopPoint { get; }
	}

	public interface IPoint3D : IPoint2D {
		int Z { get; } //Z coordinate (Low is lower, high is higher)
		new IPoint3D TopPoint { get; }
	}

	public interface IPoint4D : IPoint3D {
		byte M { get; } //Mapplane (0 is default, 255 can see and be seen by all planes (theoretically -tar))

		new IPoint4D TopPoint { get; }
		[SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
		Map GetMap();
	}

	public class Point2D : IPoint2D {
		private readonly int x;	//Map X coordinate (Low is west, high is east)
		private readonly int y;

		public static int GetSimpleDistance(int ax, int ay, int bx, int by) {
			return Math.Max(Math.Abs(ax - bx), Math.Abs(ay - by));
		}

		public static int GetSimpleDistance(Point2D a, Point2D b) {
			return Math.Max(Math.Abs(a.x - b.x), Math.Abs(a.y - b.y));
		}

		public static int GetSimpleDistance(IPoint2D a, IPoint2D b) {
			a = a.TopPoint;
			b = b.TopPoint;
			return Math.Max(Math.Abs(a.X - b.X), Math.Abs(a.Y - b.Y));
		}

		[SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods")]
		public static Direction GetDirFromTo(IPoint2D start, IPoint2D target) {
			// Get the 2D direction between points.
			start = start.TopPoint;
			target = target.TopPoint;

			int dx = (start.X - target.X);
			int dy = (start.Y - target.Y);

			int ax = Math.Abs(dx);
			int ay = Math.Abs(dy);

			if (ay > ax) {
				if (ax == 0) {
					return ((dy > 0) ? Direction.North : Direction.South);
				}
				int slope = ay / ax;
				if (slope > 2) {
					return ((dy > 0) ? Direction.North : Direction.South);
				}
				if (dx > 0) {	// westish
					return ((dy > 0) ? Direction.NorthWest : Direction.SouthWest);
				}
				return ((dy > 0) ? Direction.NorthEast : Direction.SouthEast);
			} else {
				if (ay == 0) {
					if (dx == 0)
						return (Direction.North);	// here ?
					return ((dx > 0) ? Direction.West : Direction.East);
				}
				int slope = ax / ay;
				if (slope > 2) {
					return ((dx > 0) ? Direction.West : Direction.East);
				}
				if (dy > 0) {
					return ((dx > 0) ? Direction.NorthWest : Direction.NorthEast);
				}
				return ((dx > 0) ? Direction.SouthWest : Direction.SouthEast);
			}
		}

		/// <summary>
		/// Returns a new Point instance with coordinates of this instance + the given parameters
		/// </summary>
		public Point2D Add(int diffX, int diffY) {
			return new Point2D((ushort) (this.x + diffX), (ushort) (this.y + diffY));
		}

		public Point2D(int x, int y) {
			this.x = x;
			this.y = y;
		}

		public Point2D(Point2D p) {
			this.x = p.x;
			this.y = p.y;
		}

		public Point2D(IPoint2D p) {
			this.x = p.X;
			this.y = p.Y;
		}

		public int X {
			get { return this.x; }
		}

		public int Y {
			get { return this.y; }
		}

		public static bool Equals(Point2D a, Point2D b) {
			return ((a.x == b.x) && (a.y == b.y));
		}

		public static bool Equals(IPoint2D a, IPoint2D b) {
			return ((a.X == b.X) && (a.Y == b.Y));
		}

		public override string ToString() {
			return "(" + this.x + "," + this.y + ")";
		}

		public static bool operator ==(Point2D first, Point2D second) {
			if (ReferenceEquals(first, null)) {
				if (ReferenceEquals(second, null)) {
					return true;
				}
				return false;
			}
			if (ReferenceEquals(second, null)) {
				return false;
			}
			return ((first.x == second.x) && (first.y == second.y));
		}

		public override bool Equals(object obj) {
			Point2D p = obj as Point2D;
			if (p != null) {
				return ((this.x == p.x) && (this.y == p.y));
			}
			IPoint2D ip = obj as IPoint2D;
			if (ip != null) {
				return ((this.x == ip.X) && (this.y == ip.Y));
			}
			return false;
		}

		public static bool operator !=(Point2D first, Point2D second) {
			return !(first == second);
		}

		public override int GetHashCode() {
			return (37 * 17 ^ this.x) ^ this.y;
		}

		IPoint2D IPoint2D.TopPoint {
			get { return this; }
		}
	}

	public class Point3D : Point2D, IPoint3D {
		private readonly int z;

		public Point3D(int x, int y, int z)
			: base(x, y) {
			this.z = z;
		}

		public Point3D(Point3D p)
			: base(p) {
			this.z = p.z;
		}

		public Point3D(IPoint3D p)
			: base(p) {
			this.z = p.Z;
		}

		/// <summary>
		/// Returns a new Point instance with coordinates of this instance + the given parameters
		/// </summary>
		public Point3D Add(int diffX, int diffY, int diffZ) {
			return new Point3D((ushort) (this.X + diffX), (ushort) (this.Y + diffY), (sbyte) (this.z + diffZ));
		}

		public static bool Equals(Point3D a, Point3D b) {
			return ((a.X == b.X) && (a.Y == b.Y) && (a.z == b.z));
		}

		public static bool Equals(IPoint3D a, IPoint3D b) {
			return ((a.X == b.X) && (a.Y == b.Y) && (a.Z == b.Z));
		}

		public int Z {
			get {
				return this.z;
			}
		}

		public override string ToString() {
			return "(" + this.X + "," + this.Y + "," + this.z + ")";
		}

		public static bool operator ==(Point3D first, Point3D second) {
			if (ReferenceEquals(first, null)) {
				if (ReferenceEquals(second, null)) {
					return true;
				}
				return false;
			}
			if (ReferenceEquals(second, null)) {
				return false;
			}
			return ((first.X == second.X) && (first.Y == second.Y) && (first.z == second.z));
		}

		public override bool Equals(object obj) {
			Point3D p = obj as Point3D;
			if (p != null) {
				return ((this.X == p.X) && (this.Y == p.Y) && (this.z == p.z));
			}
			IPoint3D ip = obj as IPoint3D;
			if (ip != null) {
				return ((this.X == ip.X) && (this.Y == ip.Y) && (this.z == ip.Z));
			}
			return false;
		}

		public static bool operator !=(Point3D first, Point3D second) {
			return !(first == second);
		}

		public override int GetHashCode() {
			return ((37 * 17 ^ this.X) ^ this.Y) ^ this.z;
		}

		IPoint3D IPoint3D.TopPoint {
			get { return this; }
		}
	}

	public sealed class Point4D : Point3D, IPoint4D {
		private readonly byte m;

		public Point4D(IPoint3D point3d, byte m)
			: base(point3d) {
			this.m = m;
		}

		public Point4D(int x, int y, int z, byte m)
			: base(x, y, z) {
			this.m = m;
		}

		public Point4D(int x, int y, int z)
			: this(x, y, z, 0) {
		}

		public Point4D(int x, int y)
			: this(x, y, 0, 0) {
		}


		internal Point4D(MutablePoint4D p)
			: this(p.x, p.y, p.z, p.m) {
		}

		public Point4D(Point4D p)
			: base(p) {
			this.m = p.m;
		}

		public Point4D(IPoint4D p)
			: base(p) {
			this.m = p.M;
		}

		public byte M {
			get {
				return this.m;
			}
		}

		public static bool Equals(Point4D a, Point4D b) {
			return ((a.X == b.X) && (a.Y == b.Y) && (a.Z == b.Z) && (a.m == b.m));
		}

		public static bool Equals(IPoint4D a, IPoint4D b) {
			return ((a.X == b.X) && (a.Y == b.Y) && (a.Z == b.Z) && (a.M == b.M));
		}

		public static Point4D Parse(string value) {
			Match match = positionRE.Match(value);
			if (match.Success) {
				GroupCollection gc = match.Groups;
				int x = ConvertTools.ParseInt32(gc["x"].Value);
				int y = ConvertTools.ParseInt32(gc["y"].Value);
				string zstr = gc["z"].Value;
				string mstr = gc["m"].Value;
				int z;
				byte m;
				if (zstr.Length > 0) {
					z = ConvertTools.ParseInt32(zstr);
					if (mstr.Length > 0) {
						m = ConvertTools.ParseByte(mstr);
					} else {
						m = 0;
					}
				} else {
					z = 0;
					m = 0;
				}
				return new Point4D(x, y, z, m);
			}
			throw new SEException("Invalid input string for Point4D parse: '" + value + "'");
		}

		internal static Regex positionRE = new Regex(@"\s*(?<x>\d+)\s*(,|\s)\s*(?<y>\d+)\s*((,|\s)\s*(?<z>-?\d+))?\s*((,|\s)\s*(?<m>\d+))?\s*",
			RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

		public override string ToString() {
			return "(" + this.X + "," + this.Y + "," + this.Z + "," + this.m + ")";
		}

		public string ToNormalString() {
			return this.X + "," + this.Y + "," + this.Z + "," + this.m;
		}

		public static bool operator ==(Point4D first, Point4D second) {
			if (ReferenceEquals(first, null)) {
				if (ReferenceEquals(second, null)) {
					return true;
				}
				return false;
			}
			if (ReferenceEquals(second, null)) {
				return false;
			}
			return ((first.X == second.X) && (first.Y == second.Y) && (first.Z == second.Z) && (first.m == second.m));
		}

		public override bool Equals(object obj) {
			Point4D p = obj as Point4D;
			if (p != null) {
				return ((this.X == p.X) && (this.Y == p.Y) && (this.Z == p.Z) && (this.m == p.m));
			}
			IPoint4D ip = obj as IPoint4D;
			if (ip != null) {
				return ((this.X == ip.X) && (this.Y == ip.Y) && (this.Z == ip.Z) && (this.m == ip.M));
			}
			return false;
		}

		public static bool operator !=(Point4D first, Point4D second) {
			return !(first == second);
		}

		public override int GetHashCode() {
			return (((37 * 17 ^ this.X) ^ this.Y) ^ this.Z) ^ this.m;
		}

		public Map GetMap() {
			return Map.GetMap(this.m);
		}

		IPoint4D IPoint4D.TopPoint {
			get { return this; }
		}
	}
}