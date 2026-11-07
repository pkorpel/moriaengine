using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using SteamEngine.Common;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1240 - a venue: a building or open place of a city with a type, a Czech name, opening hours and rectangles. It isn't
	/// a region: FlaggedRegion.City and RealmTag aren't inherited and much asks "which area am I in" (the town stone compares areas),
	/// so a child region would change answers all over. The marker stands hidden at the venue's middle and keeps everything in tags.</summary>
	public partial class CityVenue {
		private static readonly TagKey typeTK = TagKey.Acquire("_venue_type_");
		private static readonly TagKey titleTK = TagKey.Acquire("_venue_title_");
		private static readonly TagKey cityTK = TagKey.Acquire("_venue_city_");
		private static readonly TagKey opensTK = TagKey.Acquire("_venue_opens_");
		private static readonly TagKey closesTK = TagKey.Acquire("_venue_closes_");
		private static readonly TagKey rectsTK = TagKey.Acquire("_venue_rects_");
		private static readonly TagKey zMinTK = TagKey.Acquire("_venue_zmin_");
		private static readonly TagKey zMaxTK = TagKey.Acquire("_venue_zmax_");
		private static readonly TagKey idTK = TagKey.Acquire("_venue_id_");

		/// <summary>An open-air venue's height band: any.</summary>
		public const int AnyZMin = -128, AnyZMax = 127;
		private const int BelowFloor = 5, AboveFloor = 15;

		private List<ImmutableRectangle> rectsCache;
		private string rectsCacheSource;

		public VenueType VenueType {
			get { return VenueType.Get(this.GetTag(typeTK) as string); }
			set { this.SetTag(typeTK, value.Key); }
		}

		public string Title {
			get { return (this.GetTag(titleTK) as string) ?? ""; }
			set { this.SetTag(titleTK, value ?? ""); }
		}

		public string City {
			get { return this.GetTag(cityTK) as string; }
			set {
				if (value == null) {
					this.RemoveTag(cityTK);
				} else {
					this.SetTag(cityTK, value);
				}
			}
		}

		public int Opens {
			get { return TagInt(this, opensTK, 0); }
			set { this.SetTag(opensTK, value); }
		}

		public int Closes {
			get { return TagInt(this, closesTK, 24); }
			set { this.SetTag(closesTK, value); }
		}

		public int ZMin {
			get { return TagInt(this, zMinTK, AnyZMin); }
			set { this.SetTag(zMinTK, value); }
		}

		public int ZMax {
			get { return TagInt(this, zMaxTK, AnyZMax); }
			set { this.SetTag(zMaxTK, value); }
		}

		/// <summary>The permanent id, venue/&lt;city&gt;/&lt;n&gt;, also the marker's world piece id, so .SetupWorld finds it.</summary>
		public string VenueId {
			get { return this.GetTag(idTK) as string; }
			set {
				this.SetTag(idTK, value);
				WorldPieces.MarkAsPiece(this, value);
			}
		}

		public bool IsOpenAir {
			get { return (this.ZMin == AnyZMin) && (this.ZMax == AnyZMax); }
		}

		public IList<ImmutableRectangle> Rectangles {
			get {
				string source = this.GetTag(rectsTK) as string;
				if ((this.rectsCache == null) || !ReferenceEquals(source, this.rectsCacheSource)) { //a GM's .tag edit counts too
					this.rectsCache = ParseRects(source);
					this.rectsCacheSource = source;
				}
				return this.rectsCache;
			}
		}

		public void SetRectangles(IEnumerable<ImmutableRectangle> rects) {
			this.SetTag(rectsTK, FormatRects(rects));
			this.rectsCache = null;
		}

		public int TileCount {
			get {
				int tiles = 0;
				foreach (ImmutableRectangle rect in this.Rectangles) {
					tiles += (rect.Width + 1) * (rect.Height + 1);
				}
				return tiles;
			}
		}

		public bool Contains(IPoint4D point) {
			return (point != null) && (point.M == this.M) && this.Contains(point.X, point.Y, point.Z);
		}

		public bool Contains(int x, int y, int z) {
			if (!this.IsOpenAir && ((z < this.ZMin - BelowFloor) || (z > this.ZMax + AboveFloor))) {
				return false;
			}
			foreach (ImmutableRectangle rect in this.Rectangles) {
				if ((x >= rect.MinX) && (x <= rect.MaxX) && (y >= rect.MinY) && (y <= rect.MaxY)) {
					return true;
				}
			}
			return false;
		}

		public bool IsOpenAt(double hour) {
			return VenueClock.IsOpen(this.Opens, this.Closes, hour);
		}

		public bool IsOpenNow {
			get { return this.IsOpenAt(VenueClock.Hour); }
		}

		/// <summary>"Hospoda U Zlateho bazanta" or just the type.</summary>
		public string Label {
			get {
				VenueType type = this.VenueType;
				string typeName = (type == null) ? "?" : type.Name;
				return (this.Title.Length == 0) ? typeName : typeName + " " + this.Title;
			}
		}

		public override void On_Destroy() {
			VenueRegistry.Forget(this);
			if (RunLevelManager.IsRunning && (this.VenueId != null)) { //#1242 - its people go with it, however it went; not as a world is cleared
				Residents.RemoveOfVenue(this.VenueId);
			}
			base.On_Destroy();
		}

		private static int TagInt(Thing thing, TagKey key, int fallback) {
			object value = thing.GetTag(key);
			return (value == null) ? fallback : Convert.ToInt32(value, CultureInfo.InvariantCulture);
		}

		/// <summary>"x1,y1,x2,y2;x1,y1,x2,y2"</summary>
		internal static string FormatRects(IEnumerable<ImmutableRectangle> rects) {
			StringBuilder text = new StringBuilder();
			foreach (ImmutableRectangle rect in rects) {
				if (text.Length > 0) {
					text.Append(';');
				}
				text.Append(rect.MinX.ToString(CultureInfo.InvariantCulture)).Append(',').Append(rect.MinY.ToString(CultureInfo.InvariantCulture)).Append(',')
					.Append(rect.MaxX.ToString(CultureInfo.InvariantCulture)).Append(',').Append(rect.MaxY.ToString(CultureInfo.InvariantCulture));
			}
			return text.ToString();
		}

		internal static List<ImmutableRectangle> ParseRects(string text) {
			List<ImmutableRectangle> rects = new List<ImmutableRectangle>();
			if (string.IsNullOrEmpty(text)) {
				return rects;
			}
			foreach (string part in text.Split(';')) {
				string[] numbers = part.Split(',');
				if (numbers.Length == 4) {
					int minX, minY, maxX, maxY;
					if (int.TryParse(numbers[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out minX) && int.TryParse(numbers[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out minY) &&
							int.TryParse(numbers[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out maxX) && int.TryParse(numbers[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out maxY)) {
						rects.Add(new ImmutableRectangle(Math.Min(minX, maxX), Math.Min(minY, maxY), Math.Max(minX, maxX), Math.Max(minY, maxY)));
					}
				}
			}
			return rects;
		}
	}
}
