using System;
using System.Collections.Generic;

namespace SteamEngine.CompiledScripts {
	/// <summary>#1558 - chain probes for the swing: a trigger group on the attacker and the defender records which triggers
	/// fire, in what order, for a landed blow, a dodged one, a cancelled swing, refused damage and a swing being drawn.
	/// Written against the code before #1558, then the rework changed what Tests/Protocol/TriggerChainCombatTests.cs expects.
	/// Before: a landed blow was me:beforeSwing,it:beforeGetSwing,me:swingLanding,it:getSwingLanding,me:hit,it:getHit,
	/// me:causeDamage,it:damage,me:afterHit,it:afterGetHit,me:afterSwing,it:afterGetSwing; a dodged one ended after
	/// @getSwingLanding in @afterSwing/@afterGetSwing; refused damage raised nothing between @getHit and @afterHit; the
	/// drawn swing raised @hitTry.</summary>
	public static class TriggerChainCombatTests {
		internal static readonly List<string> log = new List<string>();
		internal static Thing me;

		internal static void Log(Thing who, string what) {
			log.Add(((who == me) ? "me" : "it") + ":" + what);
		}

		/// <summary>#1558 - what ProcessSwing raises once a landed blow dealt its damage (was DamageManager.Trigger_AfterHit):
		/// @afterSwing on the attacker, @afterGetSwing on the defender, with that FinalDamage.</summary>
		internal static void AfterLandedBlow(WeaponSwingArgs swing, int damage) {
			swing.InternalSetFinalDamage(damage);
			DamageManager.Trigger_AfterSwing(swing);
		}

		private static void Check(string name, bool ok) {
			Console.WriteLine("CHAINC_CHECK: " + name + "=" + ok);
		}

		private static string Logged() {
			string s = string.Join(",", log);
			log.Clear();
			return s;
		}

		private static NPC Ready(Character self) {
			self.Account.PLevel = 1;
			MovementSpeedTests.TestTeleportToOpenGround(self);
			VerificationTestUtils.CleanupNearbyNpcs(self, 10);
			self.Hits = self.MaxHits;
			me = self;
			log.Clear();
			E_ChainProbeFighter.cancelSwing = false;
			NPC rabbit = (NPC) ThingDef.GetByDefname("c_rabbit").Create(self.X + 1, self.Y, self.Z, self.M);
			NPCAiTimer.Freeze(rabbit, 300);
			rabbit.Hits = 30000;
			self.AddTriggerGroup(SingletonScript<E_ChainProbeFighter>.Instance);
			rabbit.AddTriggerGroup(SingletonScript<E_ChainProbeFighter>.Instance);
			return rabbit;
		}

		private static void Done(Character self, NPC rabbit) {
			self.RemoveTriggerGroup(SingletonScript<E_ChainProbeFighter>.Instance);
			E_ChainProbeFighter.cancelSwing = false;
			if (!rabbit.IsDeleted) {
				rabbit.Delete();
			}
		}

		/// <summary>The player's blow on a rabbit: a landed one, a cancelled one, and one the rabbit's invulnerability refuses.</summary>
		[SteamFunction]
		public static void TestChainSwing(Character self) {
			NPC rabbit = Ready(self);
			try {
				DamageManager.ProcessSwing(self, rabbit);
				string landed = Logged();
				Console.WriteLine("CHAINC_LANDED: " + landed + " |");
				Check("landedOrder", landed == "me:swing,it:getSwing,me:hit,it:getHit,it:denyDamage,me:causeDamage,it:damage,me:afterSwing,it:afterGetSwing");

				E_ChainProbeFighter.cancelSwing = true;
				DamageManager.ProcessSwing(self, rabbit);
				string cancelled = Logged();
				E_ChainProbeFighter.cancelSwing = false;
				Console.WriteLine("CHAINC_CANCELLED: " + cancelled + " |");
				Check("swingCancelStopsAll", cancelled == "me:swing");

				rabbit.Flag_Invulnerable = true;
				DamageManager.ProcessSwing(self, rabbit);
				string refused = Logged();
				rabbit.Flag_Invulnerable = false;
				Console.WriteLine("CHAINC_REFUSED: " + refused + " |");
				Check("refusedDamageIsADeny", refused == "me:swing,it:getSwing,me:hit,it:getHit,it:denyDamage,me:afterSwing,it:afterGetSwing");
			} finally {
				Done(self, rabbit);
			}
			Console.WriteLine("CHAINC_SWING_DONE");
		}

		/// <summary>The rabbit's blow on a player who dodges every monster blow.</summary>
		[SteamFunction]
		public static void TestChainDodged(Character self) {
			NPC rabbit = Ready(self);
			Player player = (Player) self;
			ProfessionDef original = player.Profession;
			player.Profession = Professions.Warrior;
			AbilityDef dodge = AbilityDef.GetByDefname("a_weapon_dodge");
			int perPoint = CombatSettings.instance.weaponDodgePerPoint;
			CombatSettings.instance.weaponDodgePerPoint = 1000;
			self.SetRealAbilityPoints(dodge, 1);
			try {
				DamageManager.ProcessSwing(rabbit, self);
				string dodged = Logged();
				Console.WriteLine("CHAINC_DODGED: " + dodged + " |");
				Check("dodgedOrder", dodged == "it:swing,me:getSwing,it:hit(dodged),it:afterSwing,me:afterGetSwing");
			} finally {
				CombatSettings.instance.weaponDodgePerPoint = perPoint;
				self.SetRealAbilityPoints(dodge, 0);
				player.Profession = original;
				Done(self, rabbit);
			}
			Console.WriteLine("CHAINC_DODGED_DONE");
		}

		/// <summary>The swing being drawn (WeaponSkillDef's Drawing phase in reach): the attacker's deny, which a cancel turns
		/// into a retry a second later instead of a blow.</summary>
		[SteamFunction]
		public static void TestChainDrawSwing(Character self) {
			NPC rabbit = Ready(self);
			Item light = (Item) ThingDef.GetByDefname("i_sconce_lit").Create(self.X, self.Y, self.Z, self.M); //enough light to swing by
			try {
				SkillSequenceArgs drawn = SkillSequenceArgs.Acquire(self, SkillName.Wrestling, rabbit, null, null, WeaponSkillDef.WeaponSkillPhase.Drawing, null);
				drawn.PhaseStroke();
				bool striking = (WeaponSkillDef.WeaponSkillPhase) Convert.ToInt64(drawn.Param1) == WeaponSkillDef.WeaponSkillPhase.Striking;
				self.AbortSkill();
				string allowed = Logged();

				E_ChainProbeFighter.denySwing = true;
				SkillSequenceArgs refused = SkillSequenceArgs.Acquire(self, SkillName.Wrestling, rabbit, null, null, WeaponSkillDef.WeaponSkillPhase.Drawing, null);
				refused.PhaseStroke();
				bool stillDrawing = (WeaponSkillDef.WeaponSkillPhase) Convert.ToInt64(refused.Param1) == WeaponSkillDef.WeaponSkillPhase.Drawing;
				E_ChainProbeFighter.denySwing = false;
				self.AbortSkill();
				string denied = Logged();
				Console.WriteLine("CHAINC_DRAW: allowed=" + allowed + " striking=" + striking + " denied=" + denied + " stillDrawing=" + stillDrawing + " |");
				Check("drawRaisesDenySwing", striking && (allowed == "me:denySwing"));
				Check("deniedSwingRetries", stillDrawing && (denied == "me:denySwing"));
			} finally {
				E_ChainProbeFighter.denySwing = false;
				light.Delete();
				Done(self, rabbit);
			}
			Console.WriteLine("CHAINC_DRAW_DONE");
		}
	}

	/// <summary>On the attacker and the defender: every swing trigger it may hear, the pre-#1558 ones too (they show what the
	/// old chain did).</summary>
	public partial class E_ChainProbeFighter : CompiledTriggerGroup {
		internal static bool cancelSwing, denySwing;

		private static bool IsDodged(ScriptArgs args) {
			return (args is WeaponSwingArgs swing) && (swing.Argv.Length > 7) && Convert.ToBoolean(swing.Argv[7]);
		}

		public void On_HitTry(Character self, ScriptArgs args) { TriggerChainCombatTests.Log(self, "hitTry"); }
		public TriggerResult On_DenySwing(Character self, DenyTriggerArgs args) {
			TriggerChainCombatTests.Log(self, "denySwing");
			if (denySwing) {
				args.Result = DenyResultMessages.Deny_NoMessage;
				return TriggerResult.Cancel;
			}
			return TriggerResult.Continue;
		}
		public TriggerResult On_BeforeSwing(Character self, ScriptArgs args) {
			TriggerChainCombatTests.Log(self, "beforeSwing");
			return cancelSwing ? TriggerResult.Cancel : TriggerResult.Continue;
		}
		public TriggerResult On_BeforeGetSwing(Character self, ScriptArgs args) { TriggerChainCombatTests.Log(self, "beforeGetSwing"); return TriggerResult.Continue; }
		public TriggerResult On_Swing(Character self, ScriptArgs args) {
			TriggerChainCombatTests.Log(self, "swing");
			return cancelSwing ? TriggerResult.Cancel : TriggerResult.Continue;
		}
		public TriggerResult On_GetSwing(Character self, ScriptArgs args) { TriggerChainCombatTests.Log(self, "getSwing"); return TriggerResult.Continue; }
		public void On_SwingLanding(Character self, ScriptArgs args) { TriggerChainCombatTests.Log(self, "swingLanding"); }
		public void On_GetSwingLanding(Character self, ScriptArgs args) { TriggerChainCombatTests.Log(self, "getSwingLanding"); }
		public void On_Hit(Character self, ScriptArgs args) { TriggerChainCombatTests.Log(self, IsDodged(args) ? "hit(dodged)" : "hit"); }
		public void On_GetHit(Character self, ScriptArgs args) { TriggerChainCombatTests.Log(self, "getHit"); }
		public TriggerResult On_DenyDamage(Character self, DenyTriggerArgs args) { TriggerChainCombatTests.Log(self, "denyDamage"); return TriggerResult.Continue; }
		public void On_CauseDamage(Character self, ScriptArgs args) { TriggerChainCombatTests.Log(self, "causeDamage"); }
		public void On_Damage(Character self, ScriptArgs args) { TriggerChainCombatTests.Log(self, "damage"); }
		public void On_AfterHit(Character self, ScriptArgs args) { TriggerChainCombatTests.Log(self, "afterHit"); }
		public void On_AfterGetHit(Character self, ScriptArgs args) { TriggerChainCombatTests.Log(self, "afterGetHit"); }
		public void On_AfterSwing(Character self, ScriptArgs args) { TriggerChainCombatTests.Log(self, "afterSwing"); }
		public void On_AfterGetSwing(Character self, ScriptArgs args) { TriggerChainCombatTests.Log(self, "afterGetSwing"); }
	}
}
