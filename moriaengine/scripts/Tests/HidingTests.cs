using System;
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts {
	public static class HidingTests {
		private static NPC other;

		/// <summary>A plain player sure to hide, with a given Stealth.</summary>
		[SteamFunction]
		public static void TestHideReady(Character self, ScriptArgs sa) {
			self.Account.PLevel = 1;
			self.SetRealSkillValue((int) SkillName.Hiding, 1100);
			self.SetRealSkillValue((int) SkillName.Stealth, int.Parse(sa.Args.Trim('"')));
			self.SetRealSkillValue((int) SkillName.DetectHidden, 1000);
			Console.WriteLine("HIDE_READY:");
		}

		[SteamFunction]
		public static void TestHideStart(Character self) {
			self.SelectSkill(SkillName.Hiding);
			Console.WriteLine("HIDE_STARTED:");
		}

		[SteamFunction]
		public static void TestHideButton(Character self) {
			self.SelectSkill(SkillName.Stealth);
			Console.WriteLine("HIDE_STARTED:");
		}

		[SteamFunction]
		public static void TestHideState(Character self) {
			Console.WriteLine("HIDE_STATE: hidden=" + self.Flag_Hidden);
		}

		/// <summary>The rule of a step, walking or running, from hidden.</summary>
		[SteamFunction]
		public static void TestHideSteps(Character self) {
			int stayed = 0, ran = 0;
			bool pluginFollows = true;
			for (int i = 0; i < 100; i++) {
				self.Flag_Hidden = true;
				pluginFollows &= HiddenPlugin.Of(self) != null; //#1478 - on while hidden
				self.TryTrigger(TriggerKey.stepTaken, new ScriptArgs(false)); //its @stepTaken, as a real step raises it
				if (self.Flag_Hidden) {
					stayed++;
				} else {
					pluginFollows &= HiddenPlugin.Of(self) == null; //off once seen
				}
				self.Flag_Hidden = true;
				self.TryTrigger(TriggerKey.stepTaken, new ScriptArgs(true));
				if (self.Flag_Hidden) {
					ran++;
				}
			}
			self.Flag_Hidden = false;
			self.Flag_InvisByMagic = true; //the Invisibility spell's flag too, and a step ends it
			bool invisible = HiddenPlugin.Of(self) != null;
			self.TryTrigger(TriggerKey.stepTaken, new ScriptArgs(false));
			bool invisibilityEnds = !self.Flag_InvisByMagic && (HiddenPlugin.Of(self) == null);
			Console.WriteLine("HIDE_STEPS: stayed=" + stayed + " ran=" + ran + " pluginFollows=" + (pluginFollows && invisible && invisibilityEnds) + " |");
		}

		/// <summary>Hidden again, then one thing that gives you away.</summary>
		[SteamFunction]
		public static void TestHideDiscovered(Character self) {
			NPC rabbit = (NPC) ((NPCDef) ThingDef.GetByDefname("c_a_jackrabbit")).Create(self.P());
			self.Flag_Hidden = true;
			rabbit.Trigger_HostileAction(self);
			bool attacking = self.Flag_Hidden;
			self.Flag_Hidden = true;
			DamageManager.CauseDamage(rabbit, self, DamageType.Physical, 1);
			bool hit = self.Flag_Hidden;
			self.Flag_Hidden = true;
			rabbit.Delete();
			Console.WriteLine("HIDE_DISCOVERED: attacking=" + attacking + " hit=" + hit);
		}

		/// <summary>Someone hidden beside you, of a given Hiding, and looking for them.</summary>
		[SteamFunction]
		public static void TestHideOther(Character self, ScriptArgs sa) {
			other = (NPC) ((NPCDef) ThingDef.GetByDefname("c_a_jackrabbit")).Create(self.P());
			other.SetRealSkillValue((int) SkillName.Hiding, int.Parse(sa.Args.Trim('"')));
			other.DeleteTimer(TimerKey.Acquire("_npcAiTimer_")); //a step of its own would show it before the search ends
			other.Flag_Hidden = true;
			self.SelectSkill(SkillName.DetectHidden);
			Console.WriteLine("HIDE_OTHER:");
		}

		[SteamFunction]
		public static void TestHideOtherState(Character self) {
			Console.WriteLine("HIDE_OTHER_STATE: hidden=" + other.Flag_Hidden);
		}
	}
}
