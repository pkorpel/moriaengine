using System;
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1022 - helpers for Tests/Protocol/MonsterLastSharedTests.cs.</summary>
	public static class MonsterLastSharedTests {
		private static int offset;

		private static NPC Monster(Character self, string ability, int x = 3, string defname = "c_orc") {
			offset++;
			NPC monster = (NPC) ThingDef.GetByDefname(defname).Create((ushort) (self.X + x), (ushort) (self.Y + (offset % 3)), self.Z, self.M);
			monster.DeleteTimer(TimerKey.Acquire("_npcAiTimer_"));
			monster.MaxHits = 1000;
			monster.Hits = 1000;
			if (ability != null) {
				monster.SetRealAbilityPoints((AbilityDef) AbstractScript.GetByDefname(ability), 1);
			}
			return monster;
		}

		private static int Around(Character self, Predicate<Character> what, int range = 12) {
			int count = 0;
			foreach (AbstractCharacter ch in self.GetMap().GetCharsInRange(self.X, self.Y, range)) {
				if (what((Character) ch)) {
					count++;
				}
			}
			return count;
		}

		[SteamFunction]
		public static void TestLastReady(Character self) {
			MovementSpeedTests.TestTeleportToOpenGround(self);
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			self.Account.PLevel = 1;
			self.MaxHits = 2000;
			self.Hits = 2000;
			Console.WriteLine("LAST_READY: done");
		}

		[SteamFunction]
		public static void TestLastBlowUp(Character self) {
			NPC bomb = Monster(self, "a_tevent_e_npc_blow_up", 2);
			self.Hits = 2000;
			MonsterSpecialAttacks.BlowUp(bomb);
			int corpses = 0;
			foreach (Thing thing in self.GetMap().GetThingsInRange(self.X, self.Y, 6)) {
				if (thing is Corpse) {
					corpses++;
				}
			}
			Console.WriteLine("LAST_BLOW: dead=" + (bomb.IsDeleted || bomb.Flag_Dead) + " hurt=" + (2000 - self.Hits > 0) + " corpses=" + corpses);
		}

		[SteamFunction]
		public static void TestLastFuse(Character self) {
			NPC bomb = Monster(self, "a_tevent_e_npc_blow_up", 1);
			bomb.P((ushort) (self.X + 1), self.Y, self.Z);
			MonsterSpecialAttacks.TryBlowUpOnHit(bomb, self);
			Console.WriteLine("LAST_FUSE: lit=" + (bomb.GetTimer(MonsterSpecialAttacks.blowUpFuseTK) != null));
		}

		[SteamFunction]
		public static void TestLastIllusion(Character self) {
			NPC monster = Monster(self, "a_tevent_e_illusionist", 3, "c_a_jackrabbit");
			int before = Around(self, ch => SummonSpellDef.IsSummonedCreature(ch));
			MonsterSpecialAttacks.MakeIllusions(monster, self, MonsterSpecialAttacks.IllusionistDef);
			int copies = Around(self, ch => SummonSpellDef.IsSummonedCreature(ch)) - before;
			Console.WriteLine("LAST_ILLUSION: copies=" + copies + " oneHit=" + (Around(self, ch => ch.MaxHits == 1) >= copies));
		}

		[SteamFunction]
		public static void TestLastShapeshift(Character self) {
			NPC monster = Monster(self, "a_tevent_e_menavec", 4);
			string name = monster.Name;
			int model = monster.Model;
			MonsterSpecialAttacks.Shapeshift(monster, MonsterSpecialAttacks.MenivecDef);
			string morphed = "changed=" + (monster.Model != model) + " name=" + monster.Name;
			monster.Hits = 50;
			monster.SetTag(MonsterSpecialAttacks.nextShapeshiftTK, Globals.TimeInSeconds);
			MonsterSpecialAttacks.TryShapeshift(monster);
			Console.WriteLine("LAST_SHIFT: " + morphed + " back=" + ((monster.Model == model) && (monster.Name == name)) + " rest=" + ((double) monster.GetTag(MonsterSpecialAttacks.nextShapeshiftTK) > Globals.TimeInSeconds + 500));
		}

		[SteamFunction]
		public static void TestLastSummonKiller(Character self) {
			NPC killer = Monster(self, "a_tevent_e_npc_summonkiller", 2);
			NPC summon = Monster(self, null, 3);
			NPC plain = Monster(self, null, 4);
			SummonSpellDef.MarkAsSummonedCreature(summon);
			Console.WriteLine("LAST_KILLER: summon=" + MonsterSpecialAttacks.DamageAfterAbsorbing(killer, summon, 100) + " plain=" + MonsterSpecialAttacks.DamageAfterAbsorbing(killer, plain, 100));
		}

		[SteamFunction]
		public static void TestLastTerror(Character self) {
			self.Hits = 2000;
			NPC terror = Monster(self, "a_tevent_e_elem_terror", 2);
			((NPCDef) terror.Def).DeathDamage = null;
			MonsterSpecialAttacks.OnMonsterDeath(terror, self);
			Console.WriteLine("LAST_TERROR: lost=" + (2000 - self.Hits));
		}
	}
}
