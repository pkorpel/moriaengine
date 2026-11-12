using System;
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {
	/// <summary>#1370 - Thing.CancellableTrigger (the one that doesn't catch exceptions) called itself instead of its base, so any
	/// call overflowed the stack. On a dagger and an NPC: without the trigger group it continues, with it (as EVENTS) it's cancelled.</summary>
	public static class CancellableTriggerTests {
		private static readonly TriggerKey testTK = TriggerKey.Acquire("testcancellable");

		/// <summary>#1463 - a trigger group cancelling @denyDeath keeps a rabbit alive through CauseDeath, with the killer as
		/// the argument and no @death; without it the rabbit dies.</summary>
		[SteamFunction]
		public static void TestDenyDeath(Character self) {
			NPC rabbit = (NPC) ((NPCDef) ThingDef.GetByDefname("c_a_jackrabbit")).Create(self.X + 1, self.Y, self.Z, self.M);
			E_DenyDeathProbe.denied = 0;
			E_DenyDeathProbe.died = 0;
			E_DenyDeathProbe.hostile = 0;
			E_DenyDeathProbe.killer = null;
			rabbit.AddTriggerGroup(SingletonScript<E_DenyDeathProbe>.Instance);
			rabbit.CauseDeath((Character) self); //(Hits = 0 would call CauseDeath itself)
			bool kept = !rabbit.IsDeleted && !rabbit.Flag_Dead;
			string line = "CANCELLABLE_DENYDEATH: kept=" + kept + " asked=" + (E_DenyDeathProbe.denied == 1) +
				" killer=" + (E_DenyDeathProbe.killer == self) + " noDeath=" + (E_DenyDeathProbe.died == 0) +
				" beforeAnyStep=" + (E_DenyDeathProbe.hostile == 0);
			if (!rabbit.IsDeleted) {
				rabbit.RemoveTriggerGroup(SingletonScript<E_DenyDeathProbe>.Instance);
				rabbit.CauseDeath((Character) self);
			}
			Console.WriteLine(line + " diesWithout=" + rabbit.IsDeleted + " |");
		}

		[SteamFunction]
		public static void TestCancellableTrigger(Character self) {
			TriggerGroup group = TriggerGroup.GetByDefname("e_test_cancellable");
			Item dagger = (Item) ThingDef.GetByDefname("i_dagger").Create(self.X + 1, self.Y, self.Z, self.M);
			NPC rabbit = (NPC) ThingDef.GetByDefname("c_rabbit").Create(self.X + 2, self.Y, self.Z, self.M);
			string line = "";
			foreach (Thing thing in new Thing[] { dagger, rabbit }) {
				TriggerResult before = thing.CancellableTrigger(testTK, null);
				thing.AddTriggerGroup(group);
				TriggerResult after = thing.CancellableTrigger(testTK, null);
				line += " " + ((thing is Item) ? "dagger" : "rabbit") + "=" + before + "/" + after;
				thing.Delete();
			}
			Console.WriteLine("CANCELLABLE_TRIGGER:" + line + " end");
		}

		private static readonly TriggerKey throwTK = TriggerKey.Acquire("testThrow");

		/// <summary>#1372 - an item's and an account's Trigger and CancellableTrigger let an exception from a trigger group
		/// through (they called the base's Try versions, which swallow it); TryTrigger and TryCancellableTrigger still don't.</summary>
		[SteamFunction]
		public static void TestThrowingTrigger(Character self) {
			TriggerGroup group = SingletonScript<E_TestThrows>.Instance;
			Item dagger = (Item) ThingDef.GetByDefname("i_dagger").Create(self.X + 1, self.Y, self.Z, self.M);
			dagger.AddTriggerGroup(group);
			AbstractAccount account = self.Account;
			//real args: a compiled trigger group's generated Run reads them, so null threw a NullReferenceException of its own
			account.AddTriggerGroup(group);
			try {
				Console.WriteLine("CANCELLABLE_THROW: itemTrigger=" + Throws(() => dagger.Trigger(throwTK, new ScriptArgs()))
					+ " itemCancellable=" + Throws(() => dagger.CancellableTrigger(throwTK, new ScriptArgs()))
					+ " itemTry=" + Throws(() => dagger.TryTrigger(throwTK, new ScriptArgs()))
					+ " itemTryCancellable=" + Throws(() => dagger.TryCancellableTrigger(throwTK, new ScriptArgs()))
					+ " accountTrigger=" + Throws(() => account.Trigger(throwTK, new ScriptArgs()))
					+ " accountCancellable=" + Throws(() => account.CancellableTrigger(throwTK, new ScriptArgs()))
					+ " accountTry=" + Throws(() => account.TryTrigger(throwTK, new ScriptArgs()))
					+ " accountTryCancellable=" + Throws(() => account.TryCancellableTrigger(throwTK, new ScriptArgs())) + " end");
			} finally {
				account.RemoveTriggerGroup(group);
				dagger.Delete();
			}
		}

		/// <summary>True only for E_TestThrows' own exception (maybe wrapped), so an unrelated throw can't pass.</summary>
		private static bool Throws(Action call) {
			try {
				call();
				return false;
			} catch (Exception e) {
				for (Exception x = e; x != null; x = x.InnerException) {
					if (x.Message.Contains("TESTTHROW")) {
						return true;
					}
				}
				Console.WriteLine("CANCELLABLE_THROW_OTHER: " + e);
				return false;
			}
		}
	}

	/// <summary>#1463 - @denyDeath: a probe that keeps its holder alive and counts @death.</summary>
	public class E_DenyDeathProbe : CompiledTriggerGroup {
		internal static int denied, died, hostile;
		internal static Character killer;

		public TriggerResult On_DenyDeath(Character self, Character killedBy) {
			denied++;
			killer = killedBy;
			return TriggerResult.Cancel;
		}

		public void On_Death(Character self, Character killedBy) {
			died++;
		}

		/// <summary>The death's first step raises it: a denial must come before.</summary>
		public void On_HostileAction(Character self, Character enemy) {
			hostile++;
		}
	}

	public class E_TestThrows : CompiledTriggerGroup {
		public void On_TestThrow(PluginHolder self) {
			throw new InvalidOperationException("TESTTHROW from a trigger group");
		}
	}
}
