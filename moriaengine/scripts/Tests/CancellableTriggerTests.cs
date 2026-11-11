using System;
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {
	/// <summary>#1370 - Thing.CancellableTrigger (the one that doesn't catch exceptions) called itself instead of its base, so any
	/// call overflowed the stack. On a dagger and an NPC: without the trigger group it continues, with it (as EVENTS) it's cancelled.</summary>
	public static class CancellableTriggerTests {
		private static readonly TriggerKey testTK = TriggerKey.Acquire("testcancellable");

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
			account.AddTriggerGroup(group);
			try {
				Console.WriteLine("CANCELLABLE_THROW: itemTrigger=" + Throws(() => dagger.Trigger(throwTK, (ScriptArgs) null))
					+ " itemCancellable=" + Throws(() => dagger.CancellableTrigger(throwTK, (ScriptArgs) null))
					+ " itemTry=" + Throws(() => dagger.TryTrigger(throwTK, (ScriptArgs) null))
					+ " itemTryCancellable=" + Throws(() => dagger.TryCancellableTrigger(throwTK, (ScriptArgs) null))
					+ " accountTrigger=" + Throws(() => account.Trigger(throwTK, (ScriptArgs) null))
					+ " accountCancellable=" + Throws(() => account.CancellableTrigger(throwTK, (ScriptArgs) null))
					+ " accountTry=" + Throws(() => account.TryTrigger(throwTK, (ScriptArgs) null))
					+ " accountTryCancellable=" + Throws(() => account.TryCancellableTrigger(throwTK, (ScriptArgs) null)) + " end");
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

	public class E_TestThrows : CompiledTriggerGroup {
		public void On_TestThrow(PluginHolder self) {
			throw new InvalidOperationException("TESTTHROW from a trigger group");
		}
	}
}
