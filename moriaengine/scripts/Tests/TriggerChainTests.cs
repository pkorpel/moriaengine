using System;
using System.Collections.Generic;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {
	/// <summary>#1558 - chain probes: trigger groups on every participant record which triggers fire, in what order, and what
	/// a cancel stops, for the double click and the step (with its regions). Written against the code before #1558 (its old
	/// chain shows in the lines it prints), then the rework changed what Tests/Protocol/TriggerChainTests.cs expects. Before:
	/// a character's double click raised the item's @denyItemDClick; a step was me:denyMove,me:step,(regions' cancellable
	/// enter/exit),me:newPosition,me:itemStep,it:step,me:stepTaken.</summary>
	public static class TriggerChainTests {
		internal static readonly List<string> log = new List<string>();
		internal static Thing me;

		internal static void Log(Thing who, string what) {
			log.Add(((who == me) ? "me" : "it") + ":" + what);
		}

		private static void Check(string name, bool ok) {
			Console.WriteLine("CHAIN_CHECK: " + name + "=" + ok);
		}

		private static string Logged() {
			string s = string.Join(",", log);
			log.Clear();
			return s;
		}

		private static void Ready(Character self) {
			self.Account.PLevel = 1;
			MovementSpeedTests.TestTeleportToOpenGround(self);
			VerificationTestUtils.CleanupNearbyNpcs(self, 10);
			self.Stam = self.MaxStam;
			me = self;
			log.Clear();
			E_ChainProbeChar.cancelDenyStep = false;
			E_ChainProbeRegion.cancelEnter = false;
		}

		/// <summary>The double click on an item and on a character, the clicker and the target probed.</summary>
		[SteamFunction]
		public static void TestChainDClick(Character self) {
			Ready(self);
			self.AddTriggerGroup(SingletonScript<E_ChainProbeChar>.Instance);
			Item dagger = (Item) ThingDef.GetByDefname("i_dagger").Create(self.X + 1, self.Y, self.Z, self.M);
			dagger.AddTriggerGroup(SingletonScript<E_ChainProbeItem>.Instance);
			NPC rabbit = (NPC) ThingDef.GetByDefname("c_rabbit").Create(self.X, self.Y + 1, self.Z, self.M);
			NPCAiTimer.Freeze(rabbit, 300);
			rabbit.AddTriggerGroup(SingletonScript<E_ChainProbeChar>.Instance);
			try {
				dagger.Trigger_DClick(self);
				string item = Logged();
				Console.WriteLine("CHAIN_DCLICK_ITEM: " + item + " |");
				rabbit.Trigger_DClick(self);
				string ch = Logged();
				Console.WriteLine("CHAIN_DCLICK_CHAR: " + ch + " |");
				Check("dclickItem", item == "me:denyItemDClick,it:denyDClick,me:itemDClick,it:DClick");
				Check("dclickChar", ch == "me:denyCharDClick,it:denyDClick,me:charDClick,it:DClick");
			} finally {
				self.RemoveTriggerGroup(SingletonScript<E_ChainProbeChar>.Instance);
				dagger.Delete();
				rabbit.Delete();
			}
			Console.WriteLine("CHAIN_DCLICK_DONE");
		}

		/// <summary>#1558 - Telekinesis's double click (skipReachChecks): the whole deny chain runs and only the reach test is
		/// skipped; a plain double click from afar is refused by that test, before the target is asked.</summary>
		[SteamFunction]
		public static void TestChainDClickFromAfar(Character self) {
			Ready(self);
			self.AddTriggerGroup(SingletonScript<E_ChainProbeChar>.Instance);
			Item dagger = (Item) ThingDef.GetByDefname("i_dagger").Create(self.X + 6, self.Y, self.Z, self.M);
			dagger.AddTriggerGroup(SingletonScript<E_ChainProbeItem>.Instance);
			try {
				dagger.Trigger_DClick(self);
				string plain = Logged();
				dagger.Trigger_DClick(self, true);
				string skipped = Logged();
				E_ChainProbeItem.denyDClick = true;
				dagger.Trigger_DClick(self, true);
				string denied = Logged();
				E_ChainProbeItem.denyDClick = false;
				Console.WriteLine("CHAIN_DCLICK_AFAR: plain=" + plain + " skipped=" + skipped + " denied=" + denied + " |");
				Check("afarRefusedByReach", plain == "me:denyItemDClick");
				Check("skipReachRunsTheChain", skipped == "me:denyItemDClick,it:denyDClick,me:itemDClick,it:DClick");
				Check("skipReachKeepsTargetDenies", denied == "me:denyItemDClick,it:denyDClick");
			} finally {
				self.RemoveTriggerGroup(SingletonScript<E_ChainProbeChar>.Instance);
				dagger.Delete();
			}
			Console.WriteLine("CHAIN_AFAR_DONE");
		}

		/// <summary>A step across a region boundary onto an item, the mover, the item and both regions probed; then a cancel
		/// by the mover's deny and by the region it enters.</summary>
		[SteamFunction]
		public static void TestChainStep(Character self) {
			Ready(self);
			Region from, to;
			int destZ;
			if (!GoToBoundary(self, out from, out to, out destZ)) {
				Check("boundaryFound", false);
				Console.WriteLine("CHAIN_STEP_DONE");
				return;
			}
			self.Direction = Direction.East;
			Item pad = (Item) ThingDef.GetByDefname("i_gold").Create(self.X + 1, self.Y, destZ, self.M);
			pad.AddTriggerGroup(SingletonScript<E_ChainProbeItem>.Instance);
			self.AddTriggerGroup(SingletonScript<E_ChainProbeChar>.Instance);
			from.AddTriggerGroup(SingletonScript<E_ChainProbeRegion>.Instance);
			to.AddTriggerGroup(SingletonScript<E_ChainProbeRegion>.Instance);
			E_ChainProbeRegion.from = from;
			//leaving "from" asks it only when it isn't a parent of "to" (the walk goes up to the shared parent); "to" is always entered
			bool leavesFrom = !IsParentOf(from, to), leavesTo = !IsParentOf(to, from);
			string denyExit = leavesFrom ? "from:denyExit," : "", exitFrom = leavesFrom ? "from:exit0," : "";
			try {
				int x = self.X;
				self.WalkRunOrFly(Direction.East, false, true);
				string step = Logged();
				bool moved = self.X == x + 1;
				Console.WriteLine("CHAIN_STEP: moved=" + moved + " leavesFrom=" + leavesFrom + " " + step + " |");
				Check("stepOrder", moved && (step == "me:denyStep," + denyExit + "to:denyEnter," + exitFrom + "to:enter0,me:newPosition,me:itemStep,it:step,me:step"));

				self.Go(x, self.Y, self.Z); //back, a teleport: forced exit and enter
				string teleport = Logged();
				Console.WriteLine("CHAIN_TELEPORT: " + teleport + " |");
				Check("teleportNotAStep", teleport == (leavesTo ? "to:exit1," : "") + (leavesFrom ? "from:enter1," : "") + "me:newPosition");

				E_ChainProbeChar.cancelDenyStep = true;
				self.WalkRunOrFly(Direction.East, false, true);
				string held = Logged();
				bool heldStill = self.X == x;
				E_ChainProbeChar.cancelDenyStep = false;
				Console.WriteLine("CHAIN_STEP_HELD: still=" + heldStill + " " + held + " |");
				Check("denyStepStopsAll", heldStill && (held == "me:denyStep"));

				E_ChainProbeRegion.cancelEnter = true;
				self.WalkRunOrFly(Direction.East, false, true);
				string refused = Logged();
				bool refusedStill = self.X == x;
				E_ChainProbeRegion.cancelEnter = false;
				Console.WriteLine("CHAIN_STEP_REGION: still=" + refusedStill + " " + refused + " |");
				Check("regionDenyStops", refusedStill && (refused == "me:denyStep," + denyExit + "to:denyEnter"));
			} finally {
				self.RemoveTriggerGroup(SingletonScript<E_ChainProbeChar>.Instance);
				from.RemoveTriggerGroup(SingletonScript<E_ChainProbeRegion>.Instance);
				to.RemoveTriggerGroup(SingletonScript<E_ChainProbeRegion>.Instance);
				pad.Delete();
			}
			Console.WriteLine("CHAIN_STEP_DONE");
		}

		private static bool IsParentOf(Region parent, Region child) {
			for (Region r = child.Parent; r != null; r = r.Parent) {
				if (r == parent) {
					return true;
				}
			}
			return false;
		}

		/// <summary>Puts self one tile west of a region's west edge, where the terrain lets it step east into the region.</summary>
		private static bool GoToBoundary(Character self, out Region from, out Region to, out int destZ) {
			Map map = self.GetMap();
			int tries = 0;
			foreach (StaticRegion region in StaticRegion.AllRegions) {
				if (region.IsWorldRegion || (region.Mapplane != self.M)) {
					continue;
				}
				foreach (ImmutableRectangle rect in region.Rectangles) {
					for (int y = rect.MinY; y <= rect.MaxY; y++) {
						int x = rect.MinX - 1;
						if (!map.IsValidPos(x, y)) {
							continue;
						}
						Region outside = map.GetRegionFor(x, y), inside = map.GetRegionFor(x + 1, y);
						if ((outside == inside) || (inside != region)) {
							continue;
						}
						if (++tries > 2000) {
							from = to = null;
							destZ = 0;
							return false;
						}
						try {
							self.Go(x, y);
						} catch (SEException) {
							continue; //an edge on the map's border
						}
						int nx = 0, ny = 0, nz = 0;
						bool canStep;
						try {
							canStep = (self.X == x) && (self.Y == y) && map.CheckMovement(self, self.MovementSettings, Direction.East, false, out nx, out ny, out nz);
						} catch (SEException) {
							continue; //the terrain check reads past the map's border
						}
						if (canStep && (nx == x + 1) && (map.GetRegionFor(nx, ny) == inside) && !AnyoneAt(map, nx, ny) && !AnyoneAt(map, x, y)) {
							from = outside;
							to = inside;
							destZ = nz;
							return true;
						}
					}
				}
			}
			from = to = null;
			destZ = 0;
			return false;
		}

		private static bool AnyoneAt(Map map, int x, int y) {
			foreach (Thing t in map.GetCharsInRange(x, y, 0)) {
				if (t != me) {
					return true;
				}
			}
			foreach (Thing t in map.GetItemsInRange(x, y, 0)) {
				return true;
			}
			return false;
		}
	}

	/// <summary>On the clicker and the clicked character, and on the mover: every trigger of the two chains it may hear,
	/// the pre-#1558 ones too (they show what the old chain did).</summary>
	public partial class E_ChainProbeChar : CompiledTriggerGroup {
		internal static bool cancelDenyStep;

		public TriggerResult On_DenyItemDClick(Character self, ScriptArgs args) { TriggerChainTests.Log(self, "denyItemDClick"); return TriggerResult.Continue; }
		public TriggerResult On_DenyCharDClick(Character self, ScriptArgs args) { TriggerChainTests.Log(self, "denyCharDClick"); return TriggerResult.Continue; }
		public TriggerResult On_DenyDClick(Character self, ScriptArgs args) { TriggerChainTests.Log(self, "denyDClick"); return TriggerResult.Continue; }
		public void On_ItemDClick(Character self, ScriptArgs args) { TriggerChainTests.Log(self, "itemDClick"); }
		public void On_CharDClick(Character self, ScriptArgs args) { TriggerChainTests.Log(self, "charDClick"); }
		public TriggerResult On_DClick(Character self, ScriptArgs args) { TriggerChainTests.Log(self, "DClick"); return TriggerResult.Continue; }

		public TriggerResult On_DenyMove(Character self, ScriptArgs args) { TriggerChainTests.Log(self, "denyMove"); return TriggerResult.Continue; }
		public TriggerResult On_DenyStep(Character self, ScriptArgs args) {
			TriggerChainTests.Log(self, "denyStep");
			return cancelDenyStep ? TriggerResult.Cancel : TriggerResult.Continue;
		}
		public TriggerResult On_Step(Character self, ScriptArgs args) { TriggerChainTests.Log(self, "step"); return TriggerResult.Continue; }
		public void On_StepTaken(Character self, ScriptArgs args) { TriggerChainTests.Log(self, "stepTaken"); }
		public void On_NewPosition(Character self, ScriptArgs args) { TriggerChainTests.Log(self, "newPosition"); }
		public void On_ItemStep(Character self, ScriptArgs args) { TriggerChainTests.Log(self, "itemStep"); }
	}

	/// <summary>On the clicked item and the item stepped onto.</summary>
	public partial class E_ChainProbeItem : CompiledTriggerGroup {
		internal static bool denyDClick;

		public TriggerResult On_DenyDClick(Item self, DenyClickArgs args) {
			TriggerChainTests.Log(self, "denyDClick");
			if (denyDClick) {
				args.Result = DenyResultMessages.Deny_ThatDoesNotBelongToYou;
				return TriggerResult.Cancel;
			}
			return TriggerResult.Continue;
		}
		public TriggerResult On_DClick(Item self, ScriptArgs args) { TriggerChainTests.Log(self, "DClick"); return TriggerResult.Continue; }
		public void On_Step(Item self, ScriptArgs args) { TriggerChainTests.Log(self, "step"); }
	}

	/// <summary>On the region left and the region entered; their names in the log are "from" and "to".</summary>
	public partial class E_ChainProbeRegion : CompiledTriggerGroup {
		internal static Region from;
		internal static bool cancelEnter;

		private static void Log(Region self, string what) {
			TriggerChainTests.log.Add(((self == from) ? "from" : "to") + ":" + what);
		}

		public TriggerResult On_DenyExit(Region self, ScriptArgs args) { Log(self, "denyExit"); return TriggerResult.Continue; }
		public TriggerResult On_DenyEnter(Region self, ScriptArgs args) {
			Log(self, "denyEnter");
			return cancelEnter ? TriggerResult.Cancel : TriggerResult.Continue;
		}
		public TriggerResult On_Exit(Region self, ScriptArgs args) { Log(self, "exit" + args.Argv[1]); return TriggerResult.Continue; }
		public TriggerResult On_Enter(Region self, ScriptArgs args) {
			Log(self, "enter" + args.Argv[1]);
			return cancelEnter ? TriggerResult.Cancel : TriggerResult.Continue;
		}
	}
}
