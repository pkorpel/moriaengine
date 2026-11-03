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
using System.Collections.Generic;
using SteamEngine.Common;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {

	/// <summary>#843 - one group of things that must exist in the world, such as a quest's stones and NPCs. To add one: a
	/// subclass in scripts/Setup and a line in WorldSetups.All, and the .SetupWorld command takes care of it. Place asks
	/// WorldPieces for every piece by a permanent id, so it can run any number of times: see WorldPieces.</summary>
	public abstract class WorldSetup {
		public abstract string Name {
			get;
		}

		protected internal abstract void Place(WorldPieces pieces, Character gm);

		/// <summary>Place, for a run with pauses: each thing returned says how far it got, and the server is free until the next.
		/// A setup that takes long overrides this, and its Place goes through it.</summary>
		protected internal virtual IEnumerable<string> PlaceInSteps(WorldPieces pieces, Character gm) {
			this.Place(pieces, gm);
			yield break;
		}

		/// <summary>This setup alone, at once, reported to the GM.</summary>
		public void Run(Character gm) {
			WorldPieces pieces = new WorldPieces();
			this.Place(pieces, gm);
			pieces.Report(gm, this.Name);
		}
	}

	/// <summary>#951 - setups run a step at a time, telling the GM how far they got, with the server free in between. It isn't
	/// a saved timer: a save must not meet a half done run, and one that a restart ends just runs again, setups only add.</summary>
	public sealed class WorldSetupRun : SteamEngine.Timers.Timer {
		private const double PauseSeconds = 0.1;

		private static WorldSetupRun running;

		private readonly Character gm;
		private readonly IEnumerator<string> steps;
		private readonly System.Diagnostics.Stopwatch watch = System.Diagnostics.Stopwatch.StartNew();

		private WorldSetupRun(Character gm, string what, WorldSetup[] setups) {
			this.gm = gm;
			this.steps = this.Steps(what, setups).GetEnumerator();
		}

		public static void Start(Character gm, string what, params WorldSetup[] setups) {
			if (running != null) {
				gm.SysMessage("A setup is already running, wait for it to finish.");
				return;
			}
			running = new WorldSetupRun(gm, what, setups);
			running.PeriodInSeconds = PauseSeconds;
			running.DueInSeconds = PauseSeconds;
		}

		private IEnumerable<string> Steps(string what, WorldSetup[] setups) {
			WorldPieces pieces = new WorldPieces();
			foreach (WorldSetup setup in setups) {
				yield return setup.Name + "...";
				foreach (string step in setup.PlaceInSteps(pieces, this.gm)) {
					yield return setup.Name + " " + step;
				}
			}
			pieces.Report(this.gm, what);
		}

		protected override void OnTimeout() {
			bool more = false;
			try {
				more = this.steps.MoveNext();
				if (more) {
					this.Tell("Setup: " + this.steps.Current);
				}
			} catch (FatalException) {
				throw;
			} catch (Exception e) {
				Logger.WriteError(e);
				this.Tell("Setup failed, see the log. What's placed stays, run it again to add the rest.");
			}
			if (!more) {
				running = null;
				this.Delete();
			}
		}

		private void Tell(string message) {
			Console.WriteLine(message + " (" + this.watch.ElapsedMilliseconds + " ms)");
			if (!this.gm.IsDeleted) {
				this.gm.SysMessage(message);
			}
		}
	}

	/// <summary>#843 - the .SetupWorld command, the way anything gets into the world.</summary>
	public static class WorldSetups {
		//in the order they run; a new setup goes at the end
		public static readonly WorldSetup[] All = { new ExpertQuestSetup(), new MasterQuestSetup(), new GrandmasterQuestSetup(), new DungeonEntrancesSetup(), new TownSetup(), new HouseSetup(), new TravelStonesSetup(), new RentedPlacesSetup() };

		/// <summary>Every setup: adds what's missing, leaves everything else alone. Safe to run at any time.</summary>
		[SteamFunction]
		public static void SetupWorld(Character self) {
			WorldSetupRun.Start(self, "World", All);
		}
	}

	/// <summary>#843 - the pieces setups put into the world. Each is known by a permanent id, kept in a tag on the piece itself.
	/// A setup asks for a piece: if it exists anywhere it's left as it is, however far a GM moved it, and if a GM deleted it,
	/// it's created again. A piece from before there were ids, the same kind of thing near the spot, is taken as that piece.
	/// Nothing is ever removed or replaced, and anything not asked for is never touched. So changing a spot in a setup
	/// only affects pieces yet to be created. See WorldPieces.Item for the one thing a run does to a piece that exists.</summary>
	public sealed class WorldPieces {
		private const int NearSpot = 16;
		private static readonly TagKey pieceIdTK = TagKey.Acquire("_worldPieceId_");
		private static readonly TagKey spawnInfoTK = TagKey.Acquire("spawnInfo");

		private readonly Dictionary<string, Thing> existing = new Dictionary<string, Thing>(StringComparer.OrdinalIgnoreCase);
		private int added, kept;

		internal WorldPieces() {
			foreach (Thing thing in Thing.AllThings) {
				string id = thing.GetTag(pieceIdTK) as string;
				if ((id != null) && !thing.IsDeleted) {
					this.existing[id] = thing;
				}
			}
		}

		/// <summary>A thing that replaces a piece at runtime, as a respawned quest NPC, becomes that piece.</summary>
		public static void MarkAsPiece(Thing thing, string id) {
			thing.SetTag(pieceIdTK, id);
		}

		/// <summary>An item that belongs in the world: fixed in place, which also keeps it from decaying and from being
		/// picked up. Done on every run, for pieces placed before this rule too.</summary>
		public Item Item(string id, string defname, int[] at, byte m, Action<Item> onNew = null) {
			Item item = (Item) this.GetOrCreate(id, defname, at, m, onNew == null ? (Action<Thing>) null : t => onNew((Item) t));
			item.SetAttribute(ItemAttributes.attr_move_never, true);
			return item;
		}

		public NPC Npc(string id, string defname, int[] at, byte m, Action<NPC> onNew = null) {
			return (NPC) this.GetOrCreate(id, defname, at, m, onNew == null ? (Action<Thing>) null : t => onNew((NPC) t));
		}

		/// <summary>A spawn gem. What it spawned belongs to it, not to the setup.</summary>
		public void SpawnGem(string id, SpawnInfo info, int[] at, byte m) {
			if (this.existing.ContainsKey(id) || this.TryAdoptGem(id, at, m, 2)) {
				this.kept++;
				return;
			}
			ScriptHolder.GetFunction("f_spawninit").Run(new Point4D(at[0], at[1], at[2], m), info);
			this.TryAdoptGem(id, at, m, 0);
			this.added++;
		}

		private bool TryAdoptGem(string id, int[] at, byte m, int range) {
			foreach (Thing thing in Near(at, m, range)) {
				if ((thing.GetTag(spawnInfoTK) != null) && (thing.GetTag(pieceIdTK) == null)) {
					this.Claim(thing, id);
					return true;
				}
			}
			return false;
		}

		private Thing GetOrCreate(string id, string defname, int[] at, byte m, Action<Thing> onNew) {
			Thing piece;
			if (this.existing.TryGetValue(id, out piece)) {
				this.kept++;
				return piece;
			}
			ThingDef def = ThingDef.GetByDefname(defname);
			piece = FindOldPiece(def, at, m);
			if (piece != null) {
				this.kept++;
			} else {
				piece = def.Create(at[0], at[1], at[2], m);
				if (onNew != null) {
					onNew(piece);
				}
				this.added++;
			}
			this.Claim(piece, id);
			return piece;
		}

		private void Claim(Thing piece, string id) {
			MarkAsPiece(piece, id);
			this.existing[id] = piece;
		}

		private static Thing FindOldPiece(ThingDef def, int[] at, byte m) {
			Thing closest = null;
			int best = int.MaxValue;
			foreach (Thing thing in Near(at, m, NearSpot)) {
				if ((thing.Def == def) && (thing.GetTag(pieceIdTK) == null)) {
					int distance = Point2D.GetSimpleDistance(thing.X, thing.Y, at[0], at[1]);
					if (distance < best) {
						best = distance;
						closest = thing;
					}
				}
			}
			return closest;
		}

		private static List<Thing> Near(int[] at, byte m, int range) {
			ImmutableRectangle area = new ImmutableRectangle(Math.Max(at[0] - range, 0), Math.Max(at[1] - range, 0), at[0] + range, at[1] + range);
			return new List<Thing>(Map.GetMap(m).GetThingsInRectangle(area));
		}

		internal void Report(Character gm, string what) {
			string summary = "SETUP_DONE: " + what + " added=" + this.added + " kept=" + this.kept;
			Console.WriteLine(summary);
			gm.SysMessage(summary);
		}
	}

	/// <summary>#843 - finding spots for pieces on the map.</summary>
	public static class WorldSpots {
		/// <summary>The highest spot at x,y a character can stand on (land or the top of a static), or the land height.</summary>
		public static int StandZ(Map map, int x, int y) {
			int best = map.GetTileZ(x, y);
			bool found = map.CanFit(x, y, best, 16, true, false, true);
			foreach (AbstractInternalItem s in map.GetStaticsAndMultiComponentsOnCoords(x, y)) {
				int top = s.Z + s.Height;
				if ((!found || (top > best)) && map.CanFit(x, y, top, 16, true, false, true)) {
					best = top;
					found = true;
				}
			}
			return best;
		}

		/// <summary>The lowest spot at x,y a character can stand on, so a house's ground floor and not its roof.</summary>
		public static int LowestStandZ(Map map, int x, int y) {
			int land = map.GetTileZ(x, y);
			if (map.CanFit(x, y, land, 16, true, false, true)) {
				return land;
			}
			int best = int.MaxValue;
			foreach (AbstractInternalItem s in map.GetStaticsAndMultiComponentsOnCoords(x, y)) {
				int top = s.Z + s.Height;
				if ((top < best) && map.CanFit(x, y, top, 16, true, false, true)) {
					best = top;
				}
			}
			return (best == int.MaxValue) ? land : best;
		}

		internal static bool CanStand(Map map, int x, int y, out int z) {
			z = StandZ(map, x, y);
			//a static above water is a deck to stand on (Esgaroth), the water itself isn't
			bool onWater = map.IsMapTileWater(x, y) && (z <= map.GetTileZ(x, y));
			return map.IsValidPos(x, y) && !onWater && map.CanFit(x, y, z, 16, true, false, true);
		}

		/// <summary>{x, y, z} of the standable spot closest to x,y within `range` tiles, or null.</summary>
		public static int[] NearestStandable(Map map, int x, int y, int range) {
			int[] best = null;
			for (int ax = x - range; ax <= x + range; ax++) {
				for (int ay = y - range; ay <= y + range; ay++) {
					int z;
					if (map.IsValidPos(ax, ay) && CanStand(map, ax, ay, out z) && ((best == null) || (Distance(new[] { ax, ay }, x, y) < Distance(best, x, y)))) {
						best = new[] { ax, ay, z };
					}
				}
			}
			return best;
		}

		internal static int Distance(int[] spot, int x, int y) {
			return Math.Max(Math.Abs(spot[0] - x), Math.Abs(spot[1] - y));
		}

		/// <summary>The {x, y, z} tiles `steps` walking steps away from `start` by the real movement rules, or as far as
		/// there is to walk. Check how far a walk gets before trusting it: from a tile nobody can leave, this is just that tile
		/// (#841).</summary>
		public static List<int[]> WalkableRing(Map map, IPoint3D start, IMovementSettings settings, int steps, ImmutableRectangle within = null) {
			HashSet<long> seen = new HashSet<long>();
			List<int[]> ring = new List<int[]> { new[] { start.X, start.Y, start.Z } };
			seen.Add((((long) start.X) << 16) | (uint) start.Y);
			for (int step = 0; step < steps; step++) {
				List<int[]> next = new List<int[]>();
				foreach (int[] tile in ring) {
					Point3D probe = new Point3D(tile[0], tile[1], tile[2]);
					for (int dir = 0; dir < 8; dir++) {
						int x, y, z;
						if (map.CheckMovement(probe, settings, (Direction) dir, false, out x, out y, out z) && IsWithin(within, x, y) && seen.Add((((long) x) << 16) | (uint) y)) {
							next.Add(new[] { x, y, z });
						}
					}
				}
				if (next.Count == 0) {
					break;
				}
				ring = next;
			}
			return ring;
		}

		private static bool IsWithin(ImmutableRectangle rect, int x, int y) {
			return (rect == null) || ((x >= rect.MinX) && (x <= rect.MaxX) && (y >= rect.MinY) && (y <= rect.MaxY));
		}
	}
}
