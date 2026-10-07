using System;
using System.Text.RegularExpressions;
using SteamEngine.Common;
using SteamEngine.Persistence;

namespace SteamEngine.CompiledScripts {
	public sealed class Point2DSaveImplementor : ISimpleSaveImplementor {
		private static Point2DSaveImplementor instance;
		public static Point2DSaveImplementor Instance {
			get {
				return instance;
			}
		}

		public Point2DSaveImplementor() {
			instance = this;
		}

		public static Regex re = new Regex(@"^\(2D\)\s*(?<x>\d+)\s*,\s*(?<y>\d+)\s*$",
			RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

		public Type HandledType {
			get {
				return typeof(Point2D);
			}
		}


		public Regex LineRecognizer {
			get {
				return re;
			}
		}

		public string Save(object objToSave) {
			Point2D p = (Point2D) objToSave;
			return "(2D)" + p.X + "," + p.Y;
		}

		public object Load(Match match) {
			GroupCollection gc = match.Groups;
			ushort x = ConvertTools.ParseUInt16(gc["x"].Value);
			ushort y = ConvertTools.ParseUInt16(gc["y"].Value);

			return new Point2D(x, y);
		}

		public string Prefix {
			get {
				return "(2D)";
			}
		}
	}

	public sealed class Point3DSaveImplementor : ISimpleSaveImplementor {
		private static Point3DSaveImplementor instance;
		public static Point3DSaveImplementor Instance {
			get {
				return instance;
			}
		}

		public Point3DSaveImplementor() {
			instance = this;
		}

		public static Regex re = new Regex(@"^\(3D\)\s*(?<x>\d+)\s*,\s*(?<y>\d+)\s*(,\s*(?<z>-?\d+))?\s*$",
			RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

		public Type HandledType {
			get {
				return typeof(Point3D);
			}
		}


		public Regex LineRecognizer {
			get {
				return re;
			}
		}

		public string Save(object objToSave) {
			Point3D p = (Point3D) objToSave;
			if (p.Z == 0) {
				return "(3D)" + p.X + "," + p.Y;
			}
			return "(3D)" + p.X + "," + p.Y + "," + p.Z;
		}

		public object Load(Match match) {
			GroupCollection gc = match.Groups;
			ushort x = ConvertTools.ParseUInt16(gc["x"].Value);
			ushort y = ConvertTools.ParseUInt16(gc["y"].Value);

			string zstr = gc["z"].Value;
			sbyte z = 0;
			if (zstr.Length > 0) {
				z = ConvertTools.ParseSByte(zstr);
			}

			return new Point3D(x, y, z);
		}

		public string Prefix {
			get {
				return "(3D)";
			}
		}
	}

	public sealed class Point4DSaveImplementor : ISimpleSaveImplementor {
		private static Point4DSaveImplementor instance;
		public static Point4DSaveImplementor Instance {
			get {
				return instance;
			}
		}

		public Point4DSaveImplementor() {
			instance = this;
		}

		public static Regex re = new Regex(@"^\(4D\)\s*(?<x>\d+)\s*,\s*(?<y>\d+)\s*(,\s*(?<z>-?\d+))?\s*(,\s*(?<m>\d+))?\s*$",
			RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

		public Type HandledType {
			get {
				return typeof(Point4D);
			}
		}

		public Regex LineRecognizer {
			get {
				return re;
			}
		}

		public string Save(object objToSave) {
			Point4D p = (Point4D) objToSave;

			string mzPart = "";
			if (p.M == 0) {
				if (p.Z != 0) {
					mzPart = "," + p.Z;
				}
			} else {
				mzPart = "," + p.Z + "," + p.M;
			}
			return "(4D)" + p.X + "," + p.Y + mzPart; ;
		}

		public object Load(Match match) {
			GroupCollection gc = match.Groups;
			ushort x = ConvertTools.ParseUInt16(gc["x"].Value);
			ushort y = ConvertTools.ParseUInt16(gc["y"].Value);

			string zstr = gc["z"].Value;
			string mstr = gc["m"].Value;
			sbyte z = 0;
			byte m = 0;
			if (zstr.Length > 0) {
				z = ConvertTools.ParseSByte(zstr);
				if (mstr.Length > 0) {
					m = ConvertTools.ParseByte(mstr);
				}
			}
			return new Point4D(x, y, z, m);
		}

		public string Prefix {
			get {
				return "(4D)";
			}
		}
	}


	//internal static Regex positionRE = new Regex(@"\s*(?<x>\d+)\s*,\s*(?<y>\d+)\s*(,\s*(?<z>-{0,1}\d+))?\s*(,\s*(?<m>\d+))?\s*",
	//	RegexOptions.IgnoreCase|RegexOptions.CultureInvariant|RegexOptions.Compiled);
}