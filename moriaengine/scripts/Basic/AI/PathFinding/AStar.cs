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
using SteamEngine.Persistence;
using SteamEngine.Regions;
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts {
	/// <summary>
	/// A* over the 8-directional UO grid (#769). Every step costs 1, diagonals included, so the
	/// heuristic is the Chebyshev distance. The search itself (FindPath) only knows a step
	/// function, so it runs the same on the real map (Map.CheckMovement) and on synthetic grids
	/// in unit tests. All search state is per call - nothing static, so it's reentrant.
	/// </summary>
	public static class AStar {
		/// <summary>One step from (x, y, z) in dir: false if illegal, otherwise where it lands.</summary>
		public delegate bool StepFunc(int x, int y, int z, Direction dir, out int newX, out int newY, out int newZ);

		public const int DefaultMaxIterations = 1000;

		/// <summary>Search tuning. The defaults suit NPC movement.</summary>
		public sealed class Options {
			/// <summary>Node expansions before giving up.</summary>
			public int MaxIterations = DefaultMaxIterations;
			/// <summary>Arrived once within this many tiles (Chebyshev) of the target - 1 for a
			/// character target, since its own tile is occupied.</summary>
			public int StopWithin;
			/// <summary>Max Z difference that still counts as arrived. The target's Z often isn't the
			/// exact walkable Z of its tile (standing on an item, a slope), and the old exact-Z goal
			/// test then burned the whole iteration budget and failed.</summary>
			public int ZTolerance = 10;
			/// <summary>Tiles beyond the start/target bounding box the search may wander.</summary>
			public int WindowMargin = 16;
			/// <summary>On failure, return the path to the node that got closest to the target
			/// instead of null, so the mover still makes progress.</summary>
			public bool AllowPartial = true;
			/// <summary>#767, #1031 - (x, y, z) of a tile that's blocked anyway (e.g. a character
			/// stands there). Only asked within BlockedWithin tiles of the start - characters move,
			/// and callers replan.</summary>
			public Func<int, int, int, bool> Blocked;
			/// <summary>Tiles from the start (Chebyshev) where Blocked counts. A distance, not a step
			/// count, or the search would dawdle until a blocked tile was "far enough" along.</summary>
			public int BlockedWithin = 1;
		}

		public sealed class Result {
			public Direction[] Path;
			/// <summary>False when Path is a partial route toward the target (or null).</summary>
			public bool ReachedGoal;
			public int Iterations;
			/// <summary>Options.Blocked turned something down, so a character may be what's in the way.</summary>
			public bool HitBlocked;
		}

		private static readonly Direction[] emptyArray = new Direction[0];

		public static Direction[] GetPathFromTo(IPoint3D start, IPoint3D target, Map map, IMovementSettings settings) {
			return GetPathFromTo(start, target, map, settings, DefaultMaxIterations);
		}

		/// <summary>Exact-tile path (or null). Kept for the old callers; NPC movement uses
		/// FindPathOnMap with a mover.</summary>
		public static Direction[] GetPathFromTo(IPoint3D start, IPoint3D target, Map map, IMovementSettings settings, int maxIterations) {
			Options options = new Options { MaxIterations = maxIterations, AllowPartial = false };
			return FindPathOnMap(start, target, map, settings, options).Path;
		}

		/// <summary>#767 - a step next to the start onto a tile another character blocks (see CharacterCollision)
		/// is treated as impassable, so the path routes around it.</summary>
		public static Direction[] GetPathFromTo(Character mover, IPoint3D target, Map map, IMovementSettings settings) {
			Options options = new Options { AllowPartial = false };
			options.Blocked = (x, y, z) => CharacterCollision.IsTileBlockedFor(mover, map, x, y, z);
			return FindPathOnMap(mover, target, map, settings, options).Path;
		}

		public static Result FindPathOnMap(IPoint3D start, IPoint3D target, Map map, IMovementSettings settings, Options options) {
			MutablePoint probe = new MutablePoint();
			StepFunc step = delegate (int x, int y, int z, Direction dir, out int newX, out int newY, out int newZ) {
				probe.x = x;
				probe.y = y;
				probe.z = z;
				return map.CheckMovement(probe, settings, dir, false, out newX, out newY, out newZ);
			};
			return FindPath(start.X, start.Y, start.Z, target.X, target.Y, target.Z, step, options);
		}

		public static Result FindPath(int startX, int startY, int startZ, int targetX, int targetY, int targetZ, StepFunc step, Options options) {
			Result result = new Result();
			Node start = new Node(startX, startY, startZ, 0, null, Direction.North);
			if (IsGoal(start, targetX, targetY, targetZ, options)) {
				result.Path = emptyArray;
				result.ReachedGoal = true;
				return result;
			}

			int minX = Math.Min(startX, targetX) - options.WindowMargin;
			int maxX = Math.Max(startX, targetX) + options.WindowMargin;
			int minY = Math.Min(startY, targetY) - options.WindowMargin;
			int maxY = Math.Max(startY, targetY) + options.WindowMargin;

			Dictionary<long, int> bestG = new Dictionary<long, int>();
			PriorityQueue<Node, long> open = new PriorityQueue<Node, long>();
			bestG[Key(startX, startY, startZ)] = 0;
			int startH = Heuristic(startX, startY, targetX, targetY, options.StopWithin);
			open.Enqueue(start, Priority(0, startH));
			Node best = start;
			int bestH = startH;

			while ((open.Count > 0) && (result.Iterations < options.MaxIterations)) {
				Node node = open.Dequeue();
				int knownG;
				if (bestG.TryGetValue(Key(node.x, node.y, node.z), out knownG) && (knownG < node.g)) {
					continue; //stale queue entry - a cheaper route to this node was found after it was queued
				}
				result.Iterations++;

				int newG = node.g + 1;
				for (int d = 0; d < 8; d++) {
					Direction dir = (Direction) d;
					int x, y, z;
					if (!step(node.x, node.y, node.z, dir, out x, out y, out z)) {
						continue;
					}
					if ((x < minX) || (x > maxX) || (y < minY) || (y > maxY)) {
						continue;
					}
					long key = Key(x, y, z);
					int oldG;
					if (bestG.TryGetValue(key, out oldG) && (oldG <= newG)) {
						continue;
					}
					bool isTargetTile = (x == targetX) && (y == targetY);
					if (!isTargetTile && (options.Blocked != null) && (Chebyshev(x, y, startX, startY) <= options.BlockedWithin) && options.Blocked(x, y, z)) {
						result.HitBlocked = true;
						continue;
					}
					bestG[key] = newG;

					Node next = new Node(x, y, z, newG, node, dir);
					if (IsGoal(next, targetX, targetY, targetZ, options)) {
						result.Path = BuildPath(next);
						result.ReachedGoal = true;
						return result;
					}
					int h = Heuristic(x, y, targetX, targetY, options.StopWithin);
					if ((h < bestH) || ((h == bestH) && (newG < best.g))) {
						best = next;
						bestH = h;
					}
					open.Enqueue(next, Priority(newG, h));
				}
			}

			if (options.AllowPartial && (best != start)) {
				result.Path = BuildPath(best);
			}
			return result;
		}

		private static bool IsGoal(Node node, int targetX, int targetY, int targetZ, Options options) {
			return (Chebyshev(node.x, node.y, targetX, targetY) <= options.StopWithin) &&
				(Math.Abs(node.z - targetZ) <= options.ZTolerance);
		}

		private static int Chebyshev(int x1, int y1, int x2, int y2) {
			return Math.Max(Math.Abs(x1 - x2), Math.Abs(y1 - y2));
		}

		/// <summary>Admissible and consistent: every step, diagonal or not, costs 1 and changes the
		/// Chebyshev distance by at most 1. Z is left out - it only limits which steps are legal.</summary>
		private static int Heuristic(int x, int y, int targetX, int targetY, int stopWithin) {
			return Math.Max(0, Chebyshev(x, y, targetX, targetY) - stopWithin);
		}

		/// <summary>f = g + h, ties broken toward the node nearer the target (smaller h).</summary>
		private static long Priority(int g, int h) {
			return ((long) (g + h) << 16) | (long) Math.Min(h, 0xffff);
		}

		private static long Key(int x, int y, int z) {
			return ((long) (x & 0xffff) << 24) | ((long) (y & 0xffff) << 8) | (long) ((z + 128) & 0xff);
		}

		private static Direction[] BuildPath(Node end) {
			Direction[] path = new Direction[end.g];
			for (Node n = end; n.parent != null; n = n.parent) {
				path[n.g - 1] = n.cameFrom;
			}
			return path;
		}

		private sealed class Node {
			internal readonly int x, y, z, g;
			internal readonly Node parent;
			internal readonly Direction cameFrom;

			internal Node(int x, int y, int z, int g, Node parent, Direction cameFrom) {
				this.x = x;
				this.y = y;
				this.z = z;
				this.g = g;
				this.parent = parent;
				this.cameFrom = cameFrom;
			}
		}

		/// <summary>Reused probe point for Map.CheckMovement, so the map adapter doesn't allocate per step.</summary>
		private sealed class MutablePoint : IPoint3D {
			internal int x, y, z;

			public int X {
				get { return this.x; }
			}

			public int Y {
				get { return this.y; }
			}

			public int Z {
				get { return this.z; }
			}

			public IPoint3D TopPoint {
				get { return this; }
			}

			IPoint2D IPoint2D.TopPoint {
				get { return this; }
			}
		}

		#region Testing
		//for demonstrating of the path
		private static ItemDef def;
		private static ItemDef Def {
			get {
				if (def == null) {
					def = (ItemDef) ThingDef.FindItemDef(0x4f3);//0x1f14 marker rune , 0x4f3 - marble floor
				}
				return def;
			}
		}

		public static class Functions_Astar_Test {
			[SteamFunction]
			public static void Astar_Walk(Character self, ScriptArgs sa) {
				Player src = Globals.Src as Player;
				if (src != null) {
					double seconds = 0.5;
					if ((sa != null) && (sa.Argv.Length > 0)) {
						seconds = Convert.ToDouble(sa.Argv[0]);
					}

					src.Target(Targ_AStar_Walk.Instance, new object[] { self, seconds });
				}
			}

			[SteamFunction]
			public static void Astar_Draw(Character self, ScriptArgs sa) {
				Player src = Globals.Src as Player;
				if (src != null) {
					IMovementSettings settings = null;
					if ((sa != null) && (sa.Argv.Length > 0)) {
						settings = sa.Argv[0] as IMovementSettings;
					}
					if (settings == null) {
						settings = self.MovementSettings;
					}
					src.Target(Targ_AStar_Draw.Instance, new object[] { self, settings });
				}
			}
		}

		public class Targ_AStar_Draw : CompiledTargetDef {
			private static Targ_AStar_Draw instance;
			public static Targ_AStar_Draw Instance {
				get {
					return instance;
				}
			}

			public Targ_AStar_Draw() {
				instance = this;
			}

			protected override TargetResult On_TargonPoint(Player ignored, IPoint3D targetted, object parameter) {
				object[] arr = (object[]) parameter;
				Character self = (Character) arr[0];
				IMovementSettings settings = (IMovementSettings) arr[1];

				byte m = self.M;
				Map map = Map.GetMap(m);
				Direction[] path = GetPathFromTo(self, targetted, map, settings);
				if (path != null) {
					IPoint3D start = self;
					foreach (Direction dir in path) {
						int x, y, z;
						if (!map.CheckMovement(start, settings, dir, false, out x, out y, out z)) {
							Globals.SrcWriteLine("Dir " + dir + " from " + start + " unwalkable when re-checked.");
						}
						Item i = (Item) Def.Create((ushort) x, (ushort) y, (sbyte) z, m);
						i.Color = 0x21; // Tento radek je historicky prvnim C# skripterkym pocinem pana Verbatima, mocneho a krfafeho! 7.4.2007 0:29am
						start = i;
						i.AddTimer(decayTimerKey, new AStarDecayTimer()).DueInSeconds = 10;
					}
				} else {
					Globals.SrcWriteLine("Path not calculated");
				}

				return TargetResult.Done;
			}
		}

		private static TimerKey decayTimerKey = TimerKey.Acquire("_testDecayTimer_");
		[DeepCopyableClass]
		[SaveableClass]
		public class AStarDecayTimer : BoundTimer {
			[LoadingInitializer]
			[DeepCopyImplementation]
			public AStarDecayTimer() {
			}

			protected sealed override void OnTimeout(TagHolder cont) {
				Item self = cont as Item;
				if (self != null) {
					self.Delete();
				}
			}
		}

		public class Targ_AStar_Walk : CompiledTargetDef {
			private static Targ_AStar_Walk instance;
			public static Targ_AStar_Walk Instance {
				get {
					return instance;
				}
			}

			public Targ_AStar_Walk() {
				instance = this;
			}

			protected override TargetResult On_TargonPoint(Player ignored, IPoint3D targetted, object parameter) {
				object[] arr = (object[]) parameter;
				Character self = (Character) arr[0];
				double seconds = Convert.ToDouble(arr[1]);
				byte m = self.M;
				Map map = Map.GetMap(m);
				Direction[] path = GetPathFromTo(self, targetted, map, self.MovementSettings);
				if (path != null) {
					self.RemoveTimer(walkTimerKey);
					AStarWalkTimer timer = new AStarWalkTimer(path);
					timer.DueInSeconds = seconds;
					timer.PeriodInSeconds = seconds;
					self.AddTimer(walkTimerKey, timer);
				} else {
					Globals.SrcWriteLine("Path not calculated");
				}
				return TargetResult.Done;
			}
		}

		private static TimerKey walkTimerKey = TimerKey.Acquire("_testWalkTimer_");

		[DeepCopyableClass]
		[SaveableClass]
		public class AStarWalkTimer : BoundTimer {
			[SaveableData]
			[CopyableData]
			public Direction[] path;

			[SaveableData]
			[CopyableData]
			public int pathIndex;

			[LoadingInitializer]
			[DeepCopyImplementation]
			public AStarWalkTimer() {
			}

			public AStarWalkTimer(Direction[] path) {
				this.path = path;
			}

			protected sealed override void OnTimeout(TagHolder cont) {
				Character self = (Character) cont;

				if (this.pathIndex < this.path.Length) {
					Direction nextStep = this.path[this.pathIndex];
					if (nextStep != self.Direction) {
						self.Direction = nextStep;
					}
					self.WalkRunOrFly(nextStep, true, false);
					this.pathIndex++;
				} else {
					this.DueInSpan = negativeOneSecond;
				}
			}
		}
		#endregion Testing
	}
}