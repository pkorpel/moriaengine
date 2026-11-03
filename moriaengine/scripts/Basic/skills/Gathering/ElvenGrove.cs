using System.Collections.Generic;
using SteamEngine.Common;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {

	/// <summary>#427, #1070 - Erebor's elven wood, miningfix.scp: an area with tag.elven (two in the archive) holds an
	/// i_elvenprobe, which every 10 minutes moves to a random spot within 50 tiles of that point. A craftsman chopping within
	/// 15 tiles of it: elven wood there gives nothing, and ebony becomes one elven log when a roll of 100.0-135.0 is under
	/// their Lumberjacking. The probe was a GM-placed item; here the spot is rolled when first asked for and again 10 minutes
	/// on, not saved, so a restart rolls it anew.</summary>
	public static class ElvenGrove {
		internal const int Wander = 50, Reach = 15, MoveSeconds = 600, RollMin = 1000, RollMax = 1350;

		private sealed class Spot {
			internal Point2D Point;
			internal double MovesAt;
		}

		private static readonly Dictionary<FlaggedRegion, Spot> spots = new Dictionary<FlaggedRegion, Spot>();

		/// <summary>Where the area's probe is now, or null for an area with no tag.elven.</summary>
		internal static Point2D SpotOf(FlaggedRegion area) {
			if ((area == null) || !area.HasElvenGrove) {
				return null;
			}
			if (!spots.TryGetValue(area, out Spot spot) || (Globals.TimeInSeconds >= spot.MovesAt)) {
				spot = new Spot {
					Point = new Point2D(area.ElvenGroveX + Globals.dice.Next(-Wander, Wander + 1), area.ElvenGroveY + Globals.dice.Next(-Wander, Wander + 1)),
					MovesAt = Globals.TimeInSeconds + MoveSeconds
				};
				spots[area] = spot;
			}
			return spot.Point;
		}

		/// <summary>The craftsman's own block of Erebor's chopping, after vigour: near the probe.</summary>
		internal static bool IsNear(Character chopper) {
			if (!(chopper is Player player) || !Professions.HasProfession(player, Professions.Craftsman)) {
				return false;
			}
			Point2D spot = SpotOf(RegionEventSystem.AreaOf(chopper.Region));
			return (spot != null) && (Point2D.GetSimpleDistance(spot, chopper) < Reach);
		}

		/// <summary>{1000 1350} &lt; lumberjacking.</summary>
		internal static bool RollsElven(Character chopper) {
			return Globals.dice.Next(RollMin, RollMax + 1) < chopper.GetRealSkillValue(SkillName.Lumberjacking);
		}

		/// <summary>For tests: puts the area's probe on a point until it moves.</summary>
		internal static void PlaceForTest(FlaggedRegion area, Point2D point) {
			spots[area] = new Spot { Point = point, MovesAt = Globals.TimeInSeconds + MoveSeconds };
		}
	}
}
