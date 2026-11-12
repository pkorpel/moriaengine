using System;
using SteamEngine.Persistence;

namespace SteamEngine.CompiledScripts {
	/// <summary>#1387 - a plugin removing itself during a trigger: the plugins after it still hear the trigger, through
	/// all four PluginHolder trigger methods; one removing the plugin after it keeps that one from running. Protocol
	/// side: Tests/Protocol/CancellableTriggerTests.cs.</summary>
	public static class PluginLoopTests {
		private static readonly TriggerKey tkLoopTest = TriggerKey.Acquire("loopTest");
		private static readonly PluginKey firstKey = PluginKey.Acquire("_loopTestFirst_"), secondKey = PluginKey.Acquire("_loopTestSecond_"),
			thirdKey = PluginKey.Acquire("_loopTestThird_");

		private static void Check(string name, bool ok) {
			Console.WriteLine("CANCELLABLE_CHECK: " + name + "=" + ok);
		}

		/// <summary>Two plugins on the character (an item's Trigger calls the Try version, #1372); the newest runs first,
		/// so "front" is the one added second.</summary>
		private static Character WithTwo(Character self, out LoopTestPlugin front, out LoopTestPlugin back) {
			Character holder = self;
			back = (LoopTestPlugin) LoopTestPluginDef.instance.Create();
			holder.AddPlugin(firstKey, back);
			front = (LoopTestPlugin) LoopTestPluginDef.instance.Create();
			holder.AddPlugin(secondKey, front);
			return holder;
		}

		[SteamFunction]
		public static void TestPluginLoop(Character self) {
			string[] ways = { "Trigger", "TryTrigger", "CancellableTrigger", "TryCancellableTrigger" };
			foreach (string way in ways) {
				Character holder = WithTwo(self, out LoopTestPlugin front, out LoopTestPlugin back);
				front.removeSelf = true;
				Fire(holder, way);
				Check("selfRemoved" + way, (front.heard == 1) && (back.heard == 1) && (holder.GetPlugin(secondKey) == null));
				holder.DeletePlugin(firstKey);

				holder = WithTwo(self, out front, out back);
				front.removeOther = back;
				Fire(holder, way);
				Check("otherRemoved" + way, (front.heard == 1) && (back.heard == 0) && (holder.GetPlugin(firstKey) == null));
				holder.DeletePlugin(secondKey);

				//#1468 - one removing itself and the plugin right after it: the oldest still hears it
				holder = WithTwo(self, out LoopTestPlugin middle, out back);
				front = (LoopTestPlugin) LoopTestPluginDef.instance.Create();
				holder.AddPlugin(thirdKey, front);
				front.removeSelf = true;
				front.removeOther = middle;
				Fire(holder, way);
				Check("selfAndNextRemoved" + way, (front.heard == 1) && (middle.heard == 0) && (back.heard == 1) &&
					(holder.GetPlugin(thirdKey) == null) && (holder.GetPlugin(secondKey) == null));
				holder.DeletePlugin(firstKey);

				//#1468 - a removed plugin keeps its old next: both removed and added again in the other order, the list must be
				//the two, once each (with the old next kept, newer -> older -> newer would be a cycle and a trigger would hang)
				NPC rabbit = (NPC) ((NPCDef) ThingDef.GetByDefname("c_a_jackrabbit")).Create(self.X + 1, self.Y, self.Z, self.M);
				int own = System.Linq.Enumerable.Count(rabbit.GetNonSimplePlugins());
				LoopTestPlugin older = (LoopTestPlugin) LoopTestPluginDef.instance.Create(), newer = (LoopTestPlugin) LoopTestPluginDef.instance.Create();
				rabbit.AddPlugin(firstKey, older);
				rabbit.AddPlugin(secondKey, newer);
				rabbit.RemovePlugin(newer);
				rabbit.RemovePlugin(older);
				rabbit.AddPlugin(secondKey, newer);
				rabbit.AddPlugin(firstKey, older);
				int listed = System.Linq.Enumerable.Count(System.Linq.Enumerable.Take(rabbit.GetNonSimplePlugins(), own + 10));
				if (listed == own + 2) { //only fire on a sane list: a cycle would hang the server
					Fire(rabbit, way);
				}
				Check("readdedForgetsItsOldNext" + way, (listed == own + 2) && (newer.heard == 1) && (older.heard == 1));
				rabbit.Delete();
			}

			//#1479 - deleting all of a holder's plugins while an @unAssign changes them. Moved: the first of a pair to go moves
			//the other to another holder, which keeps it, unassigned once (it used to be removed again from its new holder and
			//deleted). Re-added: the first re-adds the other under another key, nothing stays (the stale key used to leave the
			//new one pointing at a deleted plugin). Added: one adds a new plugin as it goes, that goes too (it used to stay).
			PluginKey movedKey = PluginKey.Acquire("_loopTestMoved_"), readdedKey = PluginKey.Acquire("_loopTestReadded_"),
				addedKey = PluginKey.Acquire("_loopTestAdded_");
			NPC hare = Hare(self), hare2 = Hare(self);
			LoopTestPlugin one = Pair(hare, out LoopTestPlugin two);
			bool done = false;
			Action<LoopTestPlugin, PluginHolder> moveOther = (gone, cont) => {
				if (!done) {
					done = true;
					hare2.AddPlugin(movedKey, (gone == one) ? two : one);
				}
			};
			one.atUnAssign = moveOther;
			two.atUnAssign = moveOther;
			hare.DeletePlugins();
			LoopTestPlugin moved = (one.Cont == hare2) ? one : two, left = (moved == one) ? two : one;
			Check("deletePluginsLeavesAMovedOne", done && (hare2.GetPlugin(movedKey) == moved) && (moved.unassigned == 1) &&
				(left.Cont == null) && (left.unassigned == 1) && !HasLoopTestPlugin(hare));
			moved.atUnAssign = null; //hare2's delete at the end mustn't move the next scenario's plugins
			left.atUnAssign = null;

			NPC hareB = Hare(self);
			one = Pair(hareB, out two);
			done = false;
			Action<LoopTestPlugin, PluginHolder> readdOther = (gone, cont) => {
				if (!done) {
					done = true;
					LoopTestPlugin other = (gone == one) ? two : one;
					cont.RemovePlugin(other);
					cont.AddPlugin(readdedKey, other);
				}
			};
			one.atUnAssign = readdOther;
			two.atUnAssign = readdOther;
			hareB.DeletePlugins();
			Check("deletePluginsReaddedGoes", done && (hareB.GetPlugin(readdedKey) == null) && (one.Cont == null) && (two.Cont == null) &&
				!HasLoopTestPlugin(hareB));

			NPC hareC = Hare(self);
			LoopTestPlugin adder = (LoopTestPlugin) LoopTestPluginDef.instance.Create(), added = null;
			hareC.AddPlugin(firstKey, adder);
			adder.atUnAssign = (gone, cont) => {
				if (added == null) {
					added = (LoopTestPlugin) LoopTestPluginDef.instance.Create();
					cont.AddPlugin(addedKey, added);
				}
			};
			hareC.DeletePlugins();
			Check("deletePluginsAddedGoes", (added != null) && (hareC.GetPlugin(addedKey) == null) && (added.Cont == null) && !HasLoopTestPlugin(hareC));
			Console.WriteLine("PLUGIN_DELETE_ALL: moved=" + (moved.Cont == hare2) + "," + moved.unassigned + " left=" + (left.Cont == null) + "," + left.unassigned +
				" readded=" + (hareB.GetPlugin(readdedKey) != null) + " added=" + (hareC.GetPlugin(addedKey) != null));
			foreach (NPC used in new[] { hare, hare2, hareB, hareC }) {
				used.Delete();
			}

			//#1489 - a deleted plugin says so, a live one doesn't
			LoopTestPlugin alive = (LoopTestPlugin) LoopTestPluginDef.instance.Create(), gone = (LoopTestPlugin) LoopTestPluginDef.instance.Create();
			self.AddPlugin(firstKey, alive);
			self.AddPlugin(secondKey, gone);
			gone.Delete();
			Check("deletedPluginIsDeleted", gone.IsDeleted && !alive.IsDeleted && (self.GetPlugin(secondKey) == null));
			self.DeletePlugin(firstKey);
			Check("deletePluginMarksIt", alive.IsDeleted);

			//now every effect plugin hears a death: a timed effect ends, a talent's lasting one stays
			AbilityDef talent = AbilityDef.GetByDefname("a_cunning_bonus");
			self.SetRealAbilityPoints(talent, 1);
			PluginKey talentKey = PluginKey.Acquire("cunning_bonus"), timedKey = PluginKey.Acquire("_loopTestTimed_");
			EffectDurationPlugin timed = (EffectDurationPlugin) PluginDef.GetByDefname("p_cunning_bonus").Create();
			timed.Init(self, EffectFlag.BeneficialEffect, 1, TimeSpan.FromSeconds(60), null);
			self.AddPlugin(timedKey, timed);
			bool hadBoth = (self.GetPlugin(talentKey) != null) && (self.GetPlugin(timedKey) != null);
			self.CauseDeath(null);
			Check("talentOutlivesDeath", hadBoth && (self.GetPlugin(talentKey) != null));
			Check("effectEndsAtDeath", self.GetPlugin(timedKey) == null);
			self.Resurrect();
			self.SetRealAbilityPoints(talent, 0);
			Console.WriteLine("CANCELLABLE_LOOP_DONE");
		}

		private static NPC Hare(Character self) {
			return (NPC) ((NPCDef) ThingDef.GetByDefname("c_a_jackrabbit")).Create(self.X + 1, self.Y + 1, self.Z, self.M);
		}

		private static LoopTestPlugin Pair(PluginHolder holder, out LoopTestPlugin second) {
			LoopTestPlugin first = (LoopTestPlugin) LoopTestPluginDef.instance.Create();
			second = (LoopTestPlugin) LoopTestPluginDef.instance.Create();
			holder.AddPlugin(firstKey, first);
			holder.AddPlugin(secondKey, second);
			return first;
		}

		private static bool HasLoopTestPlugin(PluginHolder holder) {
			foreach (Plugin plugin in holder.GetAllPlugins()) {
				if (plugin is LoopTestPlugin) {
					return true;
				}
			}
			return false;
		}

		private static void Fire(Character holder, string way) {
			ScriptArgs sa = new ScriptArgs();
			switch (way) {
				case "Trigger":
					holder.Trigger(tkLoopTest, sa);
					break;
				case "TryTrigger":
					holder.TryTrigger(tkLoopTest, sa);
					break;
				case "CancellableTrigger":
					holder.CancellableTrigger(tkLoopTest, sa);
					break;
				default:
					holder.TryCancellableTrigger(tkLoopTest, sa);
					break;
			}
		}
	}

	/// <summary>#1389 - trigger groups removed during a trigger don't run, the ones after a removed one still do.
	/// Protocol side: Tests/Protocol/CancellableTriggerTests.cs.</summary>
	public static class TriggerGroupLoopTests {
		private static readonly TriggerKey tkLoopTest = TriggerKey.Acquire("loopTest");

		private static void Check(string name, bool ok) {
			Console.WriteLine("CANCELLABLE_CHECK: " + name + "=" + ok);
		}

		private static void Fire(PluginHolder holder, string way) {
			ScriptArgs sa = new ScriptArgs();
			switch (way) {
				case "Trigger":
					holder.Trigger(tkLoopTest, sa);
					break;
				case "TryTrigger":
					holder.TryTrigger(tkLoopTest, sa);
					break;
				case "CancellableTrigger":
					holder.CancellableTrigger(tkLoopTest, sa);
					break;
				default:
					holder.TryCancellableTrigger(tkLoopTest, sa);
					break;
			}
		}

		/// <summary>The front group runs first (the newest); what it does when it hears the trigger is set per case.</summary>
		private static void Attach(PluginHolder holder, E_LoopTestFront.Action action) {
			E_LoopTestFront.action = action;
			E_LoopTestFront.heard = 0;
			E_LoopTestBack.heard = 0;
			holder.AddTriggerGroup(SingletonScript<E_LoopTestBack>.Instance);
			holder.AddTriggerGroup(SingletonScript<E_LoopTestFront>.Instance);
		}

		[SteamFunction]
		public static void TestTriggerGroupLoop(Character self) {
			foreach (string way in new[] { "Trigger", "TryTrigger", "CancellableTrigger", "TryCancellableTrigger" }) {
				Attach(self, E_LoopTestFront.Action.RemoveSelf);
				Fire(self, way);
				Check("tgSelfRemoved" + way, (E_LoopTestFront.heard == 1) && (E_LoopTestBack.heard == 1));
				self.RemoveTriggerGroup(SingletonScript<E_LoopTestBack>.Instance);

				Attach(self, E_LoopTestFront.Action.RemoveBoth);
				Fire(self, way);
				Check("tgBothRemoved" + way, (E_LoopTestFront.heard == 1) && (E_LoopTestBack.heard == 0));
			}
			//an item: its Trigger goes to TryTrigger (#1372), the other three reach their own loop
			foreach (string way in new[] { "TryTrigger", "CancellableTrigger", "TryCancellableTrigger" }) {
				Item item = (Item) ThingDef.GetByDefname("i_dagger").Create(self.Backpack);
				Attach(item, E_LoopTestFront.Action.DeleteHolder);
				Fire(item, way);
				Check("tgHolderDeleted" + way, item.IsDeleted && (E_LoopTestFront.heard == 1) && (E_LoopTestBack.heard == 0));
			}
			E_LoopTestFront.action = E_LoopTestFront.Action.None;

			//#1394 - a duplicate's groups: the first copied one is found and removed like the others, and added again runs once
			Item original = (Item) ThingDef.GetByDefname("i_dagger").Create(self.Backpack);
			Attach(original, E_LoopTestFront.Action.None);
			Item copy = (Item) original.Dupe();
			TriggerGroup front = SingletonScript<E_LoopTestFront>.Instance, back = SingletonScript<E_LoopTestBack>.Instance;
			bool found = copy.HasTriggerGroup(front) && copy.HasTriggerGroup(back);
			copy.RemoveTriggerGroup(front);
			E_LoopTestFront.heard = 0;
			E_LoopTestBack.heard = 0;
			Fire(copy, "TryTrigger");
			bool removed = (E_LoopTestFront.heard == 0) && (E_LoopTestBack.heard == 1);
			copy.AddTriggerGroup(front);
			E_LoopTestFront.heard = 0;
			Fire(copy, "TryTrigger");
			Check("dupeGroupsFound", found);
			Check("dupeFirstGroupRemoved", removed);
			Check("dupeReaddedRunsOnce", E_LoopTestFront.heard == 1);
			original.Delete();
			copy.Delete();
			Console.WriteLine("CANCELLABLE_TGLOOP_DONE");
		}
	}

	public class E_LoopTestFront : CompiledTriggerGroup {
		internal enum Action { None, RemoveSelf, RemoveBoth, DeleteHolder }
		internal static Action action;
		internal static int heard;

		public void On_LoopTest(PluginHolder self) {
			heard++;
			switch (action) {
				case Action.RemoveSelf:
					self.RemoveTriggerGroup(this);
					break;
				case Action.RemoveBoth:
					self.RemoveTriggerGroup(this);
					self.RemoveTriggerGroup(SingletonScript<E_LoopTestBack>.Instance);
					break;
				case Action.DeleteHolder:
					((Thing) self).Delete();
					break;
			}
		}
	}

	public class E_LoopTestBack : CompiledTriggerGroup {
		internal static int heard;

		public void On_LoopTest(PluginHolder self) {
			heard++;
		}
	}

	public class LoopTestPluginDef : PluginDef {
		public static readonly LoopTestPluginDef instance =
			(LoopTestPluginDef) new LoopTestPluginDef("p_loop_test", "C# scripts", -1).Register();

		public LoopTestPluginDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
		}

		protected override Plugin CreateImpl() {
			return new LoopTestPlugin();
		}

		public new static void Bootstrap() {
			RegisterPluginDef(typeof(LoopTestPluginDef), typeof(LoopTestPlugin));
		}
	}

	/// <summary>Counts the test trigger; may remove itself or another plugin when it hears it. Never saved.</summary>
	[DeepCopyableClass]
	[SaveableClass]
	public class LoopTestPlugin : Plugin {
		internal int heard;
		internal bool removeSelf;
		internal Plugin removeOther;
		internal Action<LoopTestPlugin, PluginHolder> atUnAssign;
		internal int unassigned;

		[DeepCopyImplementation]
		public LoopTestPlugin(LoopTestPlugin copyFrom)
			: base(copyFrom) {
		}

		[LoadingInitializer]
		public LoopTestPlugin() {
		}

		/// <summary>#1479 - may change its former holder's plugins as it goes.</summary>
		public void On_UnAssign(PluginHolder formerCont) {
			this.unassigned++;
			this.atUnAssign?.Invoke(this, formerCont);
		}

		public void On_LoopTest() {
			this.heard++;
			PluginHolder holder = this.Cont; //#1468 - read before a self-removal clears it
			if (this.removeSelf) {
				this.Delete();
			}
			if ((this.removeOther != null) && (holder != null)) {
				holder.RemovePlugin(this.removeOther); //removed, not deleted: a deleted plugin wouldn't run anyway
			}
		}
	}
}
