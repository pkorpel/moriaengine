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
	public static class AStar {
		private static HashSet<AStarNode> closedAndOpenList = new HashSet<AStarNode>();
		private static PriorityQueue<AStarNode, int> openList = new PriorityQueue<AStarNode, int>();

		public static Direction[] GetPathFromTo(IPoint3D start, IPoint3D target, Map map, IMovementSettings settings) {
			return GetPathFromTo(start, target, map, settings, 1000);
		}

		private static Direction[] emptyArray = new Direction[0];

		public static Direction[] GetPathFromTo(IPoint3D start, IPoint3D target, Map map, IMovementSettings settings, int maxIterations) {
#if DEBUG
			long ticksStart = HighPerformanceTimer.TickCount;
			long ticksEnd;
			double seconds;
#endif
			int targetX = target.X;
			int targetY = target.Y;
			int targetZ = target.Z;

			if ((targetX == start.X) && (targetY == start.Y) && (targetZ == start.Z)) {
				return emptyArray;
			}

			closedAndOpenList.Clear();
			openList.Clear();
			AStarNode.ReuseAll();
			AStarNode parentNode = AStarNode.GetNew(null, Direction.North, 0, start.X, start.Y, start.Z);

			closedAndOpenList.Add(parentNode);
			openList.Enqueue(parentNode, Heuristic(start, target));


			int iterations = 0, nodesUsed = 0;
			while ((iterations < maxIterations) && (openList.Count > 0)) {
				parentNode = openList.Dequeue();
				int newStepsSoFar = parentNode.stepsSoFar + 1;
				for (int d = 0; d < 8; d++) {
					int newY, newX, newZ;
					Direction dir = (Direction) d;
					if (map.CheckMovement(parentNode, settings, dir, false, out newX, out newY, out newZ)) {

						//we found the target
						if ((targetX == newX) && (targetY == newY) && (targetZ == newZ)) {
							Direction[] path = new Direction[newStepsSoFar];
							int i = newStepsSoFar - 1;
							path[i] = dir;
							i--;
							for (; i >= 0; i--) {
								path[i] = parentNode.cameFrom;
								parentNode = parentNode.parentNode;// vtipny :)
							}
#if DEBUG
							ticksEnd = HighPerformanceTimer.TickCount;
							seconds = HighPerformanceTimer.TicksToSeconds(ticksEnd - ticksStart);
							Logger.WriteDebug("Astar success. iterations:" + iterations + " nodes used:" + nodesUsed + ", took " + seconds + " seconds.");
#endif
							return path;
						}

						AStarNode newNode = AStarNode.GetNew(parentNode, dir, newStepsSoFar, newX, newY, newZ);
						if (!closedAndOpenList.Contains(newNode)) {
							int f = newStepsSoFar + Heuristic(newNode, target);
							closedAndOpenList.Add(newNode);
							openList.Enqueue(newNode, f);

							nodesUsed++;
							//Item i = (Item) Def.Create((ushort) newX, (ushort) newY, (sbyte) newZ, map.m);
							//new AStarDecayTimer(i, TimeSpan.FromSeconds(5))
							//    .Enqueue();
						}
					}
				}
				iterations++;
			}
#if DEBUG
			ticksEnd = HighPerformanceTimer.TickCount;
			seconds = HighPerformanceTimer.TicksToSeconds(ticksEnd - ticksStart);
			Logger.WriteDebug("Astar failed. iterations:" + iterations + " nodes used:" + nodesUsed + ", took " + seconds + " seconds.");
#endif
			return null;
		}

		private static int Heuristic(IPoint3D start, IPoint3D target) {
			//return Point2D.GetSimpleDistance(start, target);

			//taken RunUO heuristic. Let's see how we're compatible ;)
			int x = start.X - target.X;
			int y = start.Y - target.Y;
			int z = start.Z - target.Z;

			x *= 11;
			y *= 11;

			return (x * x) + (y * y) + (z * z);
			//works nicely ;)
		}

		private class AStarNode : IPoint3D {
			internal int x;
			internal int y;
			internal int z;
			internal int stepsSoFar;
			internal Direction cameFrom;
			internal AStarNode parentNode;

			private AStarNode nextInStack;

			//zasobnik instanci, ucelem je pouze setreni vykonu, 
			//tj. aby se nevytvarely porad novy a novy instance kdyz se muzou pouzit stary
			private static AStarNode unusedStack;


			private static AStarNode firstInUsedStack;
			private static AStarNode lastInUsedStack;

			private AStarNode() {
			}

			internal static AStarNode GetNew(AStarNode parentNode, Direction cameFrom, int stepsSoFar, int x, int y, int z) {
				AStarNode retVal = null;
				if (unusedStack != null) {
					retVal = unusedStack;
					unusedStack = retVal.nextInStack;
				} else {
					retVal = new AStarNode();
				}
				if (lastInUsedStack == null) {
					lastInUsedStack = retVal;
				}
				retVal.nextInStack = firstInUsedStack;
				firstInUsedStack = retVal;

				retVal.x = x;
				retVal.y = y;
				retVal.z = z;
				retVal.stepsSoFar = stepsSoFar;
				retVal.cameFrom = cameFrom;
				retVal.parentNode = parentNode;
				return retVal;
			}

			internal static void ReuseAll() {
				if (lastInUsedStack != null) {
					lastInUsedStack.nextInStack = unusedStack;
					unusedStack = firstInUsedStack;

					lastInUsedStack = null;
					firstInUsedStack = null;
				}
			}

			public override int GetHashCode() {
				return ((17 ^ this.x) ^ this.y) ^ this.z;
			}

			public override bool Equals(object obj) {
				AStarNode node = obj as AStarNode;
				if (node != null) {
					return ((this.x == node.x) && (this.y == node.y) && (this.z == node.z));
				}
				return false;
			}

			int IPoint2D.X {
				get {
					return (ushort) this.x;
				}
			}

			int IPoint2D.Y {
				get {
					return (ushort) this.y;
				}
			}

			int IPoint3D.Z {
				get {
					return (sbyte) this.z;
				}
			}

			#region IPoint3D Members


			IPoint3D IPoint3D.TopPoint {
				get { return this; }
			}

			#endregion

			#region IPoint2D Members


			IPoint2D IPoint2D.TopPoint {
				get { return this; }
			}

			#endregion
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