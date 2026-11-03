using System.Collections.Generic;
using System.Globalization;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1035 - one of Erebor's st_pos/st_name places: a point, or an area, which means its P.
	/// #1043 - prices go by PricePoint. Two cities are set apart in a far corner of the map, so where the
	/// coach sets you down says nothing about how far the place is.</summary>
	public sealed class TravelStop {
		public readonly string Name;
		public readonly int X, Y;
		public readonly bool HasPoint;
		private readonly int priceX, priceY;
		private readonly bool hasPricePoint;

		internal TravelStop(string name) {
			this.Name = name;
		}

		internal TravelStop(string name, int x, int y) {
			this.Name = name;
			this.X = x;
			this.Y = y;
			this.HasPoint = true;
		}

		internal TravelStop(string name, int x, int y, int priceX, int priceY)
			: this(name, x, y) {
			this.priceX = priceX;
			this.priceY = priceY;
			this.hasPricePoint = true;
		}

		public bool HasOwnPricePoint {
			get { return this.hasPricePoint; }
		}

		/// <summary>Where the place really is on the map, for working out a price.</summary>
		public Point4D PricePoint {
			get {
				return this.hasPricePoint ? new Point4D(this.priceX, this.priceY, 0, TravelStops.Plane) : this.Destination;
			}
		}

		/// <summary>Where Erebor's `go` lands. Points without a height are Sphere's z 0, so the height is
		/// always looked up: a character can't stand at most of those.</summary>
		public Point4D Destination {
			get {
				if (this.HasPoint) {
					return new Point4D(this.X, this.Y, 0, TravelStops.Plane);
				}
				return StaticRegion.GetByName(this.Name).P;
			}
		}

		/// <summary>{x, y, z} of the closest place to stand to Destination, or null.</summary>
		public int[] Arrival {
			get {
				Point4D point = this.Destination;
				return WorldSpots.NearestStandable(Map.GetMap(point.M), point.X, point.Y, TravelStops.ArrivalRange);
			}
		}

		/// <summary>Erebor's tag.realm of the area there: 1 Gondor, 2 Mordor, 0 or 3 nobody's.</summary>
		public int RealmTag {
			get { return TravelStops.RealmTagAt(this.Destination); }
		}
	}

	/// <summary>#1035 - systems/travelstone.scp's st_places and its f_ functions. On Erebor the numbers were
	/// worked out when a GM used a stone and kept on it; here they're worked out when asked.</summary>
	public static class TravelStops {
		internal const byte Plane = 0;
		internal const int ArrivalRange = 6;

		public static readonly TravelStop[] All = {
			new TravelStop("Hobitin"),
			new TravelStop("Edoras"),
			new TravelStop("Minas Tirith", 5691, 3872, 2977, 2951),
			new TravelStop("Lorien", 2469, 1925),
			new TravelStop("Nurn", 3472, 3256),
			new TravelStop("Tharbad", 1588, 1940),
			new TravelStop("Barad-Dur", 3559, 2764),
			new TravelStop("Fornost", 1413, 1093),
			new TravelStop("Severni Spoust"),
			new TravelStop("Harad", 4144, 4032),
			new TravelStop("Severni doly", 533, 666),
			new TravelStop("Krajske doly", 771, 1551),
			new TravelStop("Sede pristavy"),
			new TravelStop("Mordorske doly", 4512, 3262),
			new TravelStop("Barad-durske doly", 3846, 2562),
			new TravelStop("Umbar", 2480, 4033),
			new TravelStop("Tahabar", 2779, 3807),
			new TravelStop("Oaza Hagir", 2887, 3724),
			new TravelStop("Rhun", 3837, 2081),
			new TravelStop("Pelargir", 2897, 3203),
			new TravelStop("Linhir"),
			new TravelStop("Dol Amroth", 2281, 3150),
			new TravelStop("Calembel", 2449, 2931),
			new TravelStop("Hurka", 1571, 1201),
			new TravelStop("Esgaroth", 3132, 1046),
			new TravelStop("Osamela hora a Dol", 3190, 1013),
			new TravelStop("Forondon", 1225, 612),
			new TravelStop("Bagronk", 3920, 3012),
			new TravelStop("Radovsko", 1309, 1359),
			new TravelStop("Helmuv Zleb", 5323, 3750, 2099, 2559),
		};

		internal static int RealmTagAt(IPoint4D point) {
			FlaggedRegion area = AreaAt(point);
			return (area == null) ? 0 : area.RealmTag;
		}

		private static FlaggedRegion AreaAt(IPoint4D point) {
			return RegionEventSystem.AreaOf(Map.GetMap(point.M).GetRegionFor(point));
		}

		/// <summary>#1041 - realm alone decides, the user's call. Erebor tests ismurderer here, which is
		/// Mordor's characters, created with 30000 kills, and any Gondor player with enough murders.</summary>
		public static bool IsMordorSide(Character ch) {
			return ch.Realm == Realm.Mordor;
		}

		/// <summary>f_srcwantsenemy.</summary>
		public static bool IsEnemyLand(Character ch, int realmTag) {
			if (ch.IsGM) {
				return false;
			}
			return IsMordorSide(ch) ? (realmTag == 1) : (realmTag == 2);
		}

		/// <summary>f_srcwantsshome: leaving enemy land. It looks at where the stone stands, and GMs aren't
		/// spared, they pay nothing anyway.</summary>
		public static bool NeedsEscort(Character ch, IPoint4D stone) {
			int realmTag = RealmTagAt(stone);
			return IsMordorSide(ch) ? (realmTag == 1) : (realmTag == 2);
		}

		/// <summary>#1043 - a stone in one of the set apart cities counts as standing at that city's price point,
		/// or the way back would cost what the way there used to.</summary>
		private static IPoint4D PriceOrigin(IPoint4D stone) {
			FlaggedRegion here = AreaAt(stone);
			foreach (TravelStop stop in All) {
				if (stop.HasOwnPricePoint && (here != null) && (AreaAt(stop.Destination) == here)) {
					return stop.PricePoint;
				}
			}
			return stone;
		}

		/// <summary>f_moneydist: Sphere's distance, the longer of the two axes, and Erebor's whole number math.</summary>
		public static int Price(Character ch, IPoint4D stone, TravelStop stop) {
			TravelSettings settings = TravelSettings.instance;
			int distance = Point2D.GetSimpleDistance(PriceOrigin(stone), stop.PricePoint);
			int price = (distance * 1000) / settings.priceDivisor;
			return NeedsEscort(ch, stone) ? (price * settings.escortPrice) / 10 : price;
		}

		/// <summary>The stops ch may be taken to, in Erebor's order.</summary>
		public static List<TravelStop> Offered(Character ch) {
			List<TravelStop> offered = new List<TravelStop>();
			foreach (TravelStop stop in All) {
				if (!IsEnemyLand(ch, stop.RealmTag)) {
					offered.Add(stop);
				}
			}
			return offered;
		}

		internal static string Count(int number) {
			return number.ToString(CultureInfo.InvariantCulture);
		}
	}
}
