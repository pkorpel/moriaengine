using System;
using System.Collections.Generic;
using System.Globalization;
using SteamEngine.Common;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1240 - every venue, found in the world on first use (as House.ByRegion): nothing else lists them.</summary>
	public static class VenueRegistry {
		public const string MarkerDefname = "i_venue_marker";
		private const string IdPrefix = "venue/";

		private static List<CityVenue> all;
		private static Dictionary<string, CityVenue> byId;

		public static IList<CityVenue> All {
			get {
				if (all == null) {
					all = new List<CityVenue>();
					foreach (Thing thing in Thing.AllThings) {
						CityVenue venue = thing as CityVenue;
						if ((venue != null) && !venue.IsDeleted) {
							all.Add(venue);
						}
					}
				}
				all.RemoveAll(v => v.IsDeleted);
				return all;
			}
		}

		internal static void Forget(CityVenue venue) {
			if (all != null) {
				all.Remove(venue);
			}
			byId = null;
		}

		/// <summary>For tests and a world load: the next use reads the world again.</summary>
		public static void Reset() {
			all = null;
			byId = null;
		}

		/// <summary>The smallest venue holding the point, or null: a bath in a bigger hall wins.</summary>
		public static CityVenue At(IPoint4D point) {
			CityVenue best = null;
			foreach (CityVenue venue in All) {
				if (venue.Contains(point) && ((best == null) || (venue.TileCount < best.TileCount))) {
					best = venue;
				}
			}
			return best;
		}

		public static List<CityVenue> InCity(string city) {
			List<CityVenue> list = new List<CityVenue>();
			foreach (CityVenue venue in All) {
				if (string.Equals(venue.City, city, StringComparison.OrdinalIgnoreCase)) {
					list.Add(venue);
				}
			}
			return list;
		}

		/// <summary>By id, through a dictionary: residents' schedules ask this for every resident every round.</summary>
		public static CityVenue ById(string id) {
			if (id == null) {
				return null;
			}
			if (byId == null) {
				byId = new Dictionary<string, CityVenue>(StringComparer.OrdinalIgnoreCase);
				foreach (CityVenue venue in All) {
					if (venue.VenueId != null) {
						byId[venue.VenueId] = venue;
					}
				}
			}
			CityVenue found;
			if (byId.TryGetValue(id, out found) && !found.IsDeleted) {
				return found;
			}
			return null;
		}

		/// <summary>The city a point belongs to, by geometry: the smallest area with Erebor's city name that holds it (as
		/// HouseRentals.CityOf, since an area's Parent often isn't its city).</summary>
		public static string CityAt(IPoint4D point) {
			string city = null;
			int smallest = int.MaxValue;
			Point2D p = new Point2D(point.X, point.Y);
			foreach (StaticRegion region in StaticRegion.AllRegions) {
				FlaggedRegion area = region as FlaggedRegion;
				if ((area != null) && !string.IsNullOrEmpty(area.City) && (area.Mapplane == point.M) && area.ContainsPoint(p)) {
					int tiles = 0;
					foreach (ImmutableRectangle rect in area.Rectangles) {
						tiles += (rect.Width + 1) * (rect.Height + 1);
					}
					if (tiles < smallest) {
						smallest = tiles;
						city = area.City;
					}
				}
			}
			return city;
		}

		/// <summary>The areas carrying a city's name, on any plane: a city can be several (Pelargir and Linhir are Dol Amroth's).</summary>
		public static List<FlaggedRegion> AreasOf(string city) {
			List<FlaggedRegion> areas = new List<FlaggedRegion>();
			foreach (StaticRegion region in StaticRegion.AllRegions) {
				FlaggedRegion area = region as FlaggedRegion;
				if ((area != null) && string.Equals(area.City, city, StringComparison.OrdinalIgnoreCase)) {
					areas.Add(area);
				}
			}
			return areas;
		}

		public static string NextId(string city) {
			string prefix = IdPrefix + (city ?? "mimo") + "/";
			int highest = 0;
			List<string> ids = new List<string>();
			foreach (CityVenue venue in All) {
				ids.Add(venue.VenueId);
			}
			foreach (VenueLine line in VenueFile.Read(VenueFile.Path)) { //a world rebuilt before .SetupWorld ran mustn't reuse the file's ids
				ids.Add(line.Id);
			}
			foreach (string id in ids) {
				int n;
				if ((id != null) && id.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) &&
						int.TryParse(id.Substring(prefix.Length), NumberStyles.Integer, CultureInfo.InvariantCulture, out n)) {
					highest = Math.Max(highest, n);
				}
			}
			return prefix + (highest + 1).ToString(CultureInfo.InvariantCulture);
		}

		/// <summary>A new venue's marker, hidden and fixed at the middle of its first rectangle.</summary>
		public static CityVenue Create(string id, VenueType type, string title, string city, int opens, int closes, IList<ImmutableRectangle> rects, int zMin, int zMax, byte m) {
			Point4D at = MarkerSpot(rects, zMin, zMax, m);
			CityVenue venue = (CityVenue) ThingDef.GetByDefname(MarkerDefname).Create(at.X, at.Y, at.Z, at.M);
			Fill(venue, id, type, title, city, opens, closes, rects, zMin, zMax);
			return venue;
		}

		internal static void Fill(CityVenue venue, string id, VenueType type, string title, string city, int opens, int closes, IList<ImmutableRectangle> rects, int zMin, int zMax) {
			venue.SetAttribute(ItemAttributes.attr_invis, true);
			venue.SetAttribute(ItemAttributes.attr_move_never, true);
			venue.VenueType = type;
			venue.Title = title;
			venue.City = city;
			venue.Opens = opens;
			venue.Closes = closes;
			venue.SetRectangles(rects);
			venue.ZMin = zMin;
			venue.ZMax = zMax;
			venue.VenueId = id;
			if (all != null && !all.Contains(venue)) {
				all.Add(venue);
			}
			byId = null;
		}

		internal static Point4D MarkerSpot(IList<ImmutableRectangle> rects, int zMin, int zMax, byte m) {
			ImmutableRectangle first = rects[0];
			int x = first.MinX + (first.Width / 2), y = first.MinY + (first.Height / 2);
			int z = (zMin == CityVenue.AnyZMin) ? Map.GetMap(m).GetTileZ(x, y) : zMin;
			return new Point4D(x, y, z, m);
		}
	}
}
