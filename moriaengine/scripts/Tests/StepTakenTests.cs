using System;
using SteamEngine.Persistence;

namespace SteamEngine.CompiledScripts {
	/// <summary>#1414 - @stepTaken after a real step: a trigger group and a plugin on the player each hear it once per step,
	/// with `running`; a teleport and a GM's step raise nothing. Protocol side: Tests/Protocol/MovementHoldsTests.cs.</summary>
	public static class StepTakenTests {
		private static readonly PluginKey probeKey = PluginKey.Acquire("_stepTakenProbe_");
		internal static int groupHeard;
		internal static bool groupRunning;

		private static void Check(string name, bool ok) {
			Console.WriteLine("HOLDS_CHECK: " + name + "=" + ok);
		}

		[SteamFunction]
		public static void TestStepTaken(Character self) {
			int plevel = self.Account.PLevel;
			StepTakenProbePlugin probe = (StepTakenProbePlugin) StepTakenProbePluginDef.instance.Create();
			try {
				self.Account.PLevel = 1;
				MovementSpeedTests.TestTeleportToOpenGround(self);
				VerificationTestUtils.CleanupNearbyNpcs(self, 10);
				groupHeard = 0;
				self.AddPlugin(probeKey, probe);
				self.AddTriggerGroup(SingletonScript<E_StepTakenProbe>.Instance);
				self.Stam = self.MaxStam;
				bool walked = Step(self, Direction.East, false);
				Check("stepTakenWalk", walked && (groupHeard == 1) && !groupRunning && (probe.heard == 1) && !probe.running);
				bool ran = Step(self, Direction.East, true);
				Check("stepTakenRun", ran && (groupHeard == 2) && groupRunning && (probe.heard == 2) && probe.running);

				self.P(self.X - 2, self.Y, self.Z); //a teleport isn't a step
				Check("teleportNotAStep", (groupHeard == 2) && (probe.heard == 2));

				self.Account.PLevel = (byte) Globals.PlevelOfGM; //a GM moves anywhere, as before the trigger: nothing raised
				bool gmStepped = Step(self, Direction.East, false);
				Check("gmStepNotRaised", gmStepped && (groupHeard == 2) && (probe.heard == 2));

				DeletedByItsStep(self);
				Console.WriteLine("HOLDS_STEP: group=" + groupHeard + " plugin=" + probe.heard + " walked=" + walked + " ran=" + ran + " gm=" + gmStepped);
			} finally {
				self.RemoveTriggerGroup(SingletonScript<E_StepTakenProbe>.Instance);
				self.DeletePlugin(probeKey);
				self.Account.PLevel = (byte) plevel;
			}
			Console.WriteLine("HOLDS_STEP_DONE");
		}

		/// <summary>An NPC deleted by the step itself (as a trap kills one: an item's @step on the tile) ends the step
		/// quietly, with no @stepTaken on the deleted character.</summary>
		private static void DeletedByItsStep(Character self) {
			NPC sheep = (NPC) ((NPCDef) ThingDef.GetByDefname("c_sheep_woolly")).Create(self.X, self.Y + 2, self.Z, self.M);
			NPCAiTimer.Freeze(sheep, 300);
			sheep.AddTriggerGroup(SingletonScript<E_StepTakenProbe>.Instance);
			Item killer = (Item) ((ItemDef) ThingDef.GetByDefname("i_gold")).Create(sheep.X + 1, sheep.Y, sheep.Z, sheep.M);
			killer.AddTriggerGroup(SingletonScript<E_DeleteWhoSteps>.Instance);
			int before = groupHeard;
			E_DeleteWhoSteps.killed = 0;
			string error = "none";
			try {
				sheep.Direction = Direction.East;
				sheep.WalkRunOrFly(Direction.East, false, false);
			} catch (Exception e) {
				error = e.GetType().Name;
			} finally {
				if (!killer.IsDeleted) {
					killer.Delete();
				}
				if (!sheep.IsDeleted) {
					sheep.Delete();
				}
			}
			Console.WriteLine("HOLDS_STEP: deletedByStep error=" + error + " killed=" + E_DeleteWhoSteps.killed);
			Check("deletedByItsStep", (error == "none") && (E_DeleteWhoSteps.killed == 1) && (groupHeard == before));
		}

		/// <summary>One step in dir: facing it first, or the first request only turns.</summary>
		private static bool Step(Character self, Direction dir, bool running) {
			self.Direction = dir;
			int x = self.X, y = self.Y;
			self.WalkRunOrFly(dir, running, true);
			return (self.X != x) || (self.Y != y);
		}
	}

	public class E_StepTakenProbe : CompiledTriggerGroup {
		public void On_StepTaken(Character self, bool running) {
			StepTakenTests.groupHeard++;
			StepTakenTests.groupRunning = running;
		}
	}

	/// <summary>On an item: deletes the NPC that steps onto it, as a trap's kill would.</summary>
	public class E_DeleteWhoSteps : CompiledTriggerGroup {
		internal static int killed;

		public void On_Step(Item self, Character stepping) {
			if (stepping is NPC) {
				killed++;
				stepping.Delete();
			}
		}
	}

	public class StepTakenProbePluginDef : PluginDef {
		public static readonly StepTakenProbePluginDef instance =
			(StepTakenProbePluginDef) new StepTakenProbePluginDef("p_stepTaken_probe", "C# scripts", -1).Register();

		public StepTakenProbePluginDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
		}

		protected override Plugin CreateImpl() {
			return new StepTakenProbePlugin();
		}

		public new static void Bootstrap() {
			RegisterPluginDef(typeof(StepTakenProbePluginDef), typeof(StepTakenProbePlugin));
		}
	}

	/// <summary>Counts @stepTaken and keeps its `running`. Never saved.</summary>
	[DeepCopyableClass]
	[SaveableClass]
	public class StepTakenProbePlugin : Plugin {
		internal int heard;
		internal bool running;

		[DeepCopyImplementation]
		public StepTakenProbePlugin(StepTakenProbePlugin copyFrom)
			: base(copyFrom) {
		}

		[LoadingInitializer]
		public StepTakenProbePlugin() {
		}

		public void On_StepTaken(bool running) {
			this.heard++;
			this.running = running;
		}
	}
}
