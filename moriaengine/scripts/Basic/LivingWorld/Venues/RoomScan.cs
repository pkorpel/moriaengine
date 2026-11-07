using System;
using System.Collections.Generic;
using SteamEngine.Common;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1240 - a room found by RoomScan: its floor tiles, the rectangles covering exactly them, its floor heights.</summary>
	public sealed class FoundRoom {
		public readonly List<int[]> Tiles;
		public readonly List<ImmutableRectangle> Rectangles;
		public readonly int ZMin, ZMax;
		public readonly bool TooBig;
		public byte M { get; internal set; }

		internal FoundRoom(List<int[]> tiles, bool tooBig) {
			this.Tiles = tiles;
			this.TooBig = tooBig;
			this.ZMin = int.MaxValue;
			this.ZMax = int.MinValue;
			foreach (int[] tile in tiles) {
				this.ZMin = Math.Min(this.ZMin, tile[2]);
				this.ZMax = Math.Max(this.ZMax, tile[2]);
			}
			this.Rectangles = RoomScan.ToRectangles(tiles);
		}

		public int[] Middle {
			get {
				long sx = 0, sy = 0;
				foreach (int[] tile in this.Tiles) {
					sx += tile[0];
					sy += tile[1];
				}
				int mx = (int) (sx / this.Tiles.Count), my = (int) (sy / this.Tiles.Count);
				int[] best = this.Tiles[0];
				foreach (int[] tile in this.Tiles) {
					if ((Math.Abs(tile[0] - mx) + Math.Abs(tile[1] - my)) < (Math.Abs(best[0] - mx) + Math.Abs(best[1] - my))) {
						best = tile;
					}
				}
				return best;
			}
		}

		/// <summary>Tiles of the room with a neighbour outside it: the outline .venue shows.</summary>
		public List<int[]> Edge() {
			HashSet<long> inside = new HashSet<long>();
			foreach (int[] tile in this.Tiles) {
				inside.Add(RoomScan.Key(tile[0], tile[1]));
			}
			List<int[]> edge = new List<int[]>();
			foreach (int[] tile in this.Tiles) {
				if (!inside.Contains(RoomScan.Key(tile[0] + 1, tile[1])) || !inside.Contains(RoomScan.Key(tile[0] - 1, tile[1])) ||
						!inside.Contains(RoomScan.Key(tile[0], tile[1] + 1)) || !inside.Contains(RoomScan.Key(tile[0], tile[1] - 1))) {
					edge.Add(tile);
				}
			}
			return edge;
		}
	}

	/// <summary>#1240 - finds a room from a tile inside it: the floor someone can walk over (the real movement rules, so walls stop it)
	/// with something overhead (a Roof or Surface static at least a head above the floor), so a doorway with the street beyond ends it.
	/// Hobbit holes and orc halls have no roofed inside on our map: their venues are set by corners.</summary>
	public static class RoomScan {
		public const int MaxTiles = 1600, MaxSpan = 48, MinRoomTiles = 12;
		private const int HeadRoom = 12;

		internal static long Key(int x, int y) {
			return (((long) x) << 16) | (uint) y;
		}

		/// <summary>Something overhead at x,y over a floor at z: a static with Roof or Surface whose bottom is a head above.</summary>
		public static bool IsCovered(Map map, int x, int y, int z) {
			foreach (AbstractInternalItem s in map.GetStaticsAndMultiComponentsOnCoords(x, y)) {
				if ((TileData.HasFlag(s.Flags, TileFlag.Roof) || TileData.HasFlag(s.Flags, TileFlag.Surface)) && (s.Z >= z + HeadRoom)) {
					return true;
				}
			}
			return false;
		}

		/// <summary>The room around start, or null when start itself has nothing overhead.</summary>
		public static FoundRoom Scan(Map map, IPoint3D start, IMovementSettings settings) {
			return Scan(map, start, settings, null, false);
		}

		internal static FoundRoom Scan(Map map, IPoint3D start, IMovementSettings settings, HashSet<long> seen, bool outsideVenues) {
			if (!IsCovered(map, start.X, start.Y, start.Z)) {
				return null;
			}
			seen = seen ?? new HashSet<long>();
			List<int[]> tiles = new List<int[]>();
			Queue<int[]> open = new Queue<int[]>();
			int[] first = { start.X, start.Y, start.Z };
			open.Enqueue(first);
			seen.Add(Key(start.X, start.Y));
			bool tooBig = false;
			while (open.Count > 0) {
				int[] tile = open.Dequeue();
				tiles.Add(tile);
				if (tiles.Count >= MaxTiles) {
					tooBig = true;
					foreach (int[] left in open) {
						seen.Remove(Key(left[0], left[1])); //not this room's: another scan may start there
					}
					break;
				}
				Point3D probe = new Point3D(tile[0], tile[1], tile[2]);
				for (int dir = 0; dir < 8; dir += 2) { //straight steps only: a diagonal squeezes past a wall's corner
					int x, y, z;
					if (!map.CheckMovement(probe, settings, (Direction) dir, false, out x, out y, out z)) {
						continue;
					}
					if ((Math.Abs(x - start.X) > MaxSpan) || (Math.Abs(y - start.Y) > MaxSpan)) {
						tooBig |= IsCovered(map, x, y, z); //the room goes on past the limit
						continue;
					}
					if (!seen.Contains(Key(x, y)) && IsCovered(map, x, y, z) &&
							(!outsideVenues || (VenueRegistry.At(new Point4D(x, y, z, map.M)) == null))) {
						seen.Add(Key(x, y));
						open.Enqueue(new[] { x, y, z });
					}
				}
			}
			FoundRoom found = new FoundRoom(tiles, tooBig);
			found.M = map.M;
			return found;
		}

		/// <summary>Rooms of a city not yet inside a venue, biggest first: every covered standable tile of the city's areas that
		/// no room found so far holds starts a scan, which stops at a venue's tiles.</summary>
		public static List<FoundRoom> Candidates(string city, IMovementSettings settings) {
			List<FoundRoom> rooms = new List<FoundRoom>();
			Dictionary<int, HashSet<long>> seenByPlane = new Dictionary<int, HashSet<long>>();
			foreach (FlaggedRegion area in VenueRegistry.AreasOf(city)) {
				Map map = Map.GetMap(area.Mapplane);
				HashSet<long> seen;
				if (!seenByPlane.TryGetValue(area.Mapplane, out seen)) {
					seen = new HashSet<long>();
					seenByPlane[area.Mapplane] = seen;
				}
				foreach (ImmutableRectangle rect in area.Rectangles) {
					for (int x = rect.MinX; x <= rect.MaxX; x++) {
						for (int y = rect.MinY; y <= rect.MaxY; y++) {
							if (seen.Contains(Key(x, y)) || !map.IsValidPos(x, y)) {
								continue;
							}
							int z = WorldSpots.LowestStandZ(map, x, y);
							if (!IsCovered(map, x, y, z) || (VenueRegistry.At(new Point4D(x, y, z, area.Mapplane)) != null)) {
								continue;
							}
							FoundRoom room = Scan(map, new Point3D(x, y, z), settings, seen, true);
							if ((room != null) && (room.Tiles.Count >= MinRoomTiles)) {
								room.M = (byte) area.Mapplane;
								rooms.Add(room);
							}
						}
					}
				}
			}
			rooms.Sort((a, b) => b.Tiles.Count.CompareTo(a.Tiles.Count));
			return rooms;
		}

		/// <summary>Rectangles covering exactly the tiles: runs along each row, a run joined to the rectangle above when it spans the same.</summary>
		public static List<ImmutableRectangle> ToRectangles(List<int[]> tiles) {
			SortedDictionary<int, List<int>> rows = new SortedDictionary<int, List<int>>();
			foreach (int[] tile in tiles) {
				List<int> row;
				if (!rows.TryGetValue(tile[1], out row)) {
					row = new List<int>();
					rows[tile[1]] = row;
				}
				row.Add(tile[0]);
			}
			List<ImmutableRectangle> done = new List<ImmutableRectangle>();
			Dictionary<long, int[]> growing = new Dictionary<long, int[]>(); //run (minX,maxX) -> {minY, maxY}
			foreach (KeyValuePair<int, List<int>> row in rows) {
				int y = row.Key;
				List<int> xs = row.Value;
				xs.Sort();
				Dictionary<long, int[]> next = new Dictionary<long, int[]>();
				int runStart = xs[0], previous = xs[0];
				for (int i = 1; i <= xs.Count; i++) {
					if ((i < xs.Count) && (xs[i] == previous + 1)) {
						previous = xs[i];
						continue;
					}
					long run = Key(runStart, previous);
					int[] span;
					if (growing.TryGetValue(run, out span) && (span[1] == y - 1)) {
						span[1] = y;
						growing.Remove(run);
					} else {
						span = new[] { y, y };
					}
					next[run] = span;
					if (i < xs.Count) {
						runStart = xs[i];
						previous = xs[i];
					}
				}
				foreach (KeyValuePair<long, int[]> ended in growing) {
					done.Add(Rect(ended.Key, ended.Value));
				}
				growing = next;
			}
			foreach (KeyValuePair<long, int[]> left in growing) {
				done.Add(Rect(left.Key, left.Value));
			}
			return done;
		}

		private static ImmutableRectangle Rect(long run, int[] span) {
			int minX = (int) (run >> 16), maxX = (int) (run & 0xFFFF);
			return new ImmutableRectangle(minX, span[0], maxX, span[1]);
		}
	}
}
