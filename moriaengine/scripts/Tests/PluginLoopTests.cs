using System;
using SteamEngine.Persistence;

namespace SteamEngine.CompiledScripts {
	/// <summary>#1387 - a plugin removing itself during a trigger: the plugins after it still hear the trigger, through
	/// all four PluginHolder trigger methods; one removing the plugin after it keeps that one from running. Protocol
	/// side: Tests/Protocol/CancellableTriggerTests.cs.</summary>
	public static class PluginLoopTests {
		private static readonly TriggerKey tkLoopTest = TriggerKey.Acquire("loopTest");
		private static readonly PluginKey firstKey = PluginKey.Acquire("_loopTestFirst_"), secondKey = PluginKey.Acquire("_loopTestSecond_");

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
			}

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

		[DeepCopyImplementation]
		public LoopTestPlugin(LoopTestPlugin copyFrom)
			: base(copyFrom) {
		}

		[LoadingInitializer]
		public LoopTestPlugin() {
		}

		public void On_LoopTest() {
			this.heard++;
			if (this.removeSelf) {
				this.Delete();
			}
			if (this.removeOther != null) {
				this.Cont.RemovePlugin(this.removeOther); //removed, not deleted: a deleted plugin wouldn't run anyway
			}
		}
	}
}
