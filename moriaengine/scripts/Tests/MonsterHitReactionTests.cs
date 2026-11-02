using System;
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1022 - helpers for Tests/Protocol/MonsterHitReactionTests.cs.</summary>
	public static class MonsterHitReactionTests {
		private static int offset;

		private static NPC Monster(Character self, string ability) {
			offset++;
			NPC monster = (NPC) ThingDef.GetByDefname("c_orc").Create(self.X + (offset % 6) - 3, self.Y + 3, self.Z, self.M);
			monster.DeleteTimer(TimerKey.Acquire("_npcAiTimer_"));
			monster.MaxHits = 1000;
			monster.Hits = 1000;
			monster.SetRealAbilityPoints((AbilityDef) AbstractScript.GetByDefname(ability), 1);
			return monster;
		}

		private static int GoldAround(Character self) {
			int gold = 0;
			foreach (Thing thing in self.GetMap().GetThingsInRange(self.X, self.Y + 3, 5)) {
				if (thing.Def == ThingDef.GetByDefname("i_gold")) {
					gold += ((Item) thing).Amount;
				}
			}
			return gold;
		}

		[SteamFunction]
		public static void TestHitReactDrops(Character self) {
			MovementSpeedTests.TestTeleportToOpenGround(self);
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			GoldDropAbilityDef def = MonsterSpecialAttacks.GoldDropDef;
			int oneIn = def.DropOneIn;
			def.DropOneIn = 1;
			NPC monster = Monster(self, "a_tevent_e_npc_golddrop");
			int before = GoldAround(self);
			MonsterSpecialAttacks.TryReactToHit(self, monster);
			def.DropOneIn = oneIn;
			Console.WriteLine("HIT_DROP: gold=" + (GoldAround(self) > before));
		}

		[SteamFunction]
		public static void TestHitReactScales(Character self) {
			ScalesAbilityDef def = MonsterSpecialAttacks.ScalesDef;
			int oneIn = def.AbsorbOneIn;
			def.AbsorbOneIn = 1;
			double left = MonsterSpecialAttacks.DamageAfterAbsorbing(self, Monster(self, "a_tevent_e_scales"), 500);
			def.AbsorbOneIn = oneIn;
			Console.WriteLine("HIT_SCALES: small=" + ((left >= 1) && (left <= 4)));
		}

		[SteamFunction]
		public static void TestHitReactShield(Character self) {
			NpcManaShieldAbilityDef def = MonsterSpecialAttacks.NpcManaShieldDef;
			int oneIn = def.RaiseOneIn;
			def.RaiseOneIn = 1;
			NPC monster = Monster(self, "a_tevent_e_npc_manashield");
			monster.MaxMana = 100;
			monster.Mana = 100;
			double first = MonsterSpecialAttacks.DamageAfterAbsorbing(self, monster, 30);
			bool raised = monster.GetTimer(MonsterSpecialAttacks.manaShieldTK) != null;
			double second = MonsterSpecialAttacks.DamageAfterAbsorbing(self, monster, 30);
			int mana = monster.Mana;
			double third = MonsterSpecialAttacks.DamageAfterAbsorbing(self, monster, 500);
			bool gone = monster.GetTimer(MonsterSpecialAttacks.manaShieldTK) == null;
			def.RaiseOneIn = oneIn;
			Console.WriteLine("HIT_SHIELD: first=" + first + " raised=" + raised + " second=" + second + " mana=" + mana + " third=" + third + " gone=" + gone);
		}

		[SteamFunction]
		public static void TestHitReactRegen(Character self) {
			NpcRegenerationAbilityDef def = MonsterSpecialAttacks.NpcRegenerationDef;
			int most = def.MostHeals;
			def.MostHeals = 1;
			NPC monster = Monster(self, "a_tevent_e_npc_regeneration");
			monster.Hits = 100;
			MonsterSpecialAttacks.TryReactToHit(self, monster);
			bool started = monster.GetTimer(MonsterSpecialAttacks.regenerationTK) != null;
			bool healed = MonsterSpecialAttacks.RegenerateOnce(monster);
			int hits = monster.Hits;
			bool second = MonsterSpecialAttacks.RegenerateOnce(monster);
			monster.DeleteTimer(MonsterSpecialAttacks.regenerationTK);
			monster.Hits = 100;
			MonsterSpecialAttacks.TryReactToHit(self, monster);
			bool restarted = monster.GetTimer(MonsterSpecialAttacks.regenerationTK) != null;
			def.MostHeals = most;
			Console.WriteLine("HIT_REGEN: started=" + started + " healed=" + healed + " hits=" + hits + " second=" + second + " restarted=" + restarted);
		}

		[SteamFunction]
		public static void TestHitReactResist(Character self) {
			ResistCountdownAbilityDef def = MonsterSpecialAttacks.ResistCountdownDef;
			double[] chance = def.CountdownChance;
			def.CountdownChance = new double[] { 1000, 1000 };
			Player player = (Player) self;
			NPC monster = Monster(self, "a_tevent_e_resist_countdown");
			monster.ResistFire = 500;
			monster.ResistSharp = 500;
			monster.ResistPoison = 960;
			monster.ResistBleed = 500;
			player.Profession = Professions.Mage;
			MonsterSpecialAttacks.TryReactToHit(self, monster);
			string mage = "fire=" + monster.ResistFire + " sharp=" + monster.ResistSharp;
			player.Profession = Professions.Thief;
			MonsterSpecialAttacks.TryReactToHit(self, monster);
			def.CountdownChance = chance;
			Console.WriteLine("HIT_RESIST: " + mage + " poison=" + monster.ResistPoison + " bleed=" + monster.ResistBleed);
		}

		[SteamFunction]
		public static void TestHitReactPhoenix(Character self) {
			NPC monster = Monster(self, "a_tevent_e_phoenix_25");
			monster.Hits = 100;
			MonsterSpecialAttacks.TryApplyPhoenixRebirth(monster);
			Console.WriteLine("HIT_PHOENIX: max=" + monster.MaxHits + " hits=" + monster.Hits);
		}

		[SteamFunction]
		public static void TestHitReactPeaceful(Character self) {
			NPC monster = Monster(self, "a_tevent_e_no_player_attack");
			bool before = MonsterSpecialAttacks.LeavesPlayersAlone(monster);
			MonsterSpecialAttacks.TryReactToHit(self, monster);
			Console.WriteLine("HIT_PEACE: before=" + before + " after=" + MonsterSpecialAttacks.LeavesPlayersAlone(monster));
		}
	}
}
