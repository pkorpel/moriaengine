using System;
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1022 - helpers for Tests/Protocol/MonsterDrinkerTests.cs.</summary>
	public static class MonsterDrinkerTests {
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

		[SteamFunction]
		public static void TestDrinkReady(Character self) {
			MovementSpeedTests.TestTeleportToOpenGround(self);
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			Console.WriteLine("DRINK_READY: done");
		}

		[SteamFunction]
		public static void TestDrinkHeal(Character self) {
			HealDrinkerAbilityDef def = MonsterSpecialAttacks.HealDrinkerDef;
			int oneIn = def.HealOneIn;
			def.HealOneIn = 1;
			NPC monster = Monster(self, "a_tevent_e_healdrinker");
			monster.Hits = 100;
			MonsterSpecialAttacks.TryDrinkOnGotHit(monster);
			int afterFirst = monster.Hits;
			MonsterSpecialAttacks.TryDrinkOnGotHit(monster);
			def.HealOneIn = oneIn;
			Console.WriteLine("DRINK_HEAL: first=" + afterFirst + " second=" + monster.Hits);
		}

		[SteamFunction]
		public static void TestDrinkPotions(Character self) {
			NPC monster = Monster(self, "a_tevent_e_potiondrinker");
			monster.ResistPhysical = 100;
			monster.ResistFire = 100;
			monster.SwingStamina = 200;
			monster.MaxMana = 100;
			monster.Mana = 100;
			short intBefore = monster.Int;
			MonsterSpecialAttacks.Drink(monster, NpcPotion.Stoneskin);
			MonsterSpecialAttacks.Drink(monster, NpcPotion.Spellshield);
			MonsterSpecialAttacks.Drink(monster, NpcPotion.Speed);
			MonsterSpecialAttacks.Drink(monster, NpcPotion.Intellect);
			string on = "phys=" + monster.ResistPhysical + " fire=" + monster.ResistFire + " speed=" + monster.SwingStamina + " int=" + (monster.Int - intBefore) + " drunk=" + monster.HasTag(MonsterSpecialAttacks.drunkTK);
			foreach (NpcPotion potion in new[] { NpcPotion.Stoneskin, NpcPotion.Spellshield, NpcPotion.Speed, NpcPotion.Intellect }) {
				MonsterSpecialAttacks.PotionWoreOff(monster, potion);
			}
			Console.WriteLine("DRINK_POTIONS: " + on + " back=" + monster.ResistPhysical + "," + monster.ResistFire + "," + monster.SwingStamina + "," + (monster.Int - intBefore));
		}

		[SteamFunction]
		public static void TestDrinkBerserk(Character self) {
			NPC monster = Monster(self, "a_tevent_e_berserk_potvora");
			monster.ResistFire = 500;
			monster.SwingStamina = 100;
			short dex = monster.Dex;
			monster.Hits = 900;
			MonsterSpecialAttacks.TryBerserk(monster);
			monster.Hits = 700;
			MonsterSpecialAttacks.TryBerserk(monster);
			string first = "stage=" + MonsterSpecialAttacks.BerserkStage(monster) + " dex=" + monster.Dex + " speed=" + monster.SwingStamina + " fire=" + monster.ResistFire;
			monster.Hits = 400;
			MonsterSpecialAttacks.TryBerserk(monster);
			monster.Hits = 200;
			MonsterSpecialAttacks.TryBerserk(monster);
			string last = "stage=" + MonsterSpecialAttacks.BerserkStage(monster) + " speed=" + monster.SwingStamina + " fire=" + monster.ResistFire;
			monster.Hits = 900;
			MonsterSpecialAttacks.TryBerserk(monster);
			Console.WriteLine("DRINK_BERSERK: " + first + " | " + last + " | calm=" + MonsterSpecialAttacks.BerserkStage(monster) + " dexBack=" + (monster.Dex == dex) + " speed=" + monster.SwingStamina + " fire=" + monster.ResistFire);
		}

		[SteamFunction]
		public static void TestDrinkKnockout(Character self) {
			WeaponKnockoutAbilityDef def = MonsterSpecialAttacks.WeaponKnockoutDef;
			int oneIn = def.KnockoutOneIn;
			def.KnockoutOneIn = 1;
			Weapon sword = (Weapon) ThingDef.GetByDefname("i_katana").Create(self.Backpack);
			self.TryEquip(sword);
			bool held = self.Weapon == sword;
			NPC monster = Monster(self, "a_tevent_e_getwpn");
			MonsterSpecialAttacks.TryKnockWeaponOut(monster, self, 0);
			bool keptOnNoHarm = self.Weapon == sword;
			MonsterSpecialAttacks.TryKnockWeaponOut(monster, self, 10);
			def.KnockoutOneIn = oneIn;
			Console.WriteLine("DRINK_KNOCK: held=" + held + " kept=" + keptOnNoHarm + " lost=" + (self.Weapon == null) + " pack=" + (sword.Cont == self.Backpack));
		}

		[SteamFunction]
		public static void TestDrinkLeech(Character self) {
			NPC monster = Monster(self, "a_tevent_e_lifeleech_vampire");
			MonsterSpecialAttacks.TryLeechAboveMaxHits(monster);
			Console.WriteLine("DRINK_LEECH: above=" + (monster.Hits > monster.MaxHits));
		}

		[SteamFunction]
		public static void TestDrinkSummon(Character self) {
			BattleSummonerAbilityDef def = MonsterSpecialAttacks.BattleSummonerDef;
			int oneIn = def.SummonOneIn;
			def.SummonOneIn = 1;
			NPC monster = Monster(self, "a_tevent_e_npc_summoner");
			NPCDef monsterDef = (NPCDef) monster.Def;
			var list = monsterDef.BattleSummons;
			monsterDef.BattleSummons = new System.Collections.Generic.List<NPCDef> { (NPCDef) ThingDef.GetByDefname("c_a_jackrabbit") };
			int before = 0;
			foreach (AbstractCharacter ch in self.GetMap().GetCharsInRange(monster.X, monster.Y, 3)) {
				before++;
			}
			MonsterSpecialAttacks.TryBattleSummon(monster, self);
			int after = 0;
			bool summoned = false;
			foreach (AbstractCharacter ch in self.GetMap().GetCharsInRange(monster.X, monster.Y, 3)) {
				after++;
				summoned |= SummonSpellDef.IsSummonedCreature((Character) ch);
			}
			monsterDef.BattleSummons = list;
			def.SummonOneIn = oneIn;
			Console.WriteLine("DRINK_SUMMON: more=" + (after > before) + " summoned=" + summoned);
		}

		[SteamFunction]
		public static void TestDrinkAcid(Character self) {
			NPC monster = Monster(self, "a_tevent_e_acid_damage");
			self.Hits = self.MaxHits;
			int hits = self.Hits;
			self.Account.PLevel = 1;
			MonsterSpecialAttacks.Splash(monster, (Player) self, 20);
			Console.WriteLine("DRINK_ACID: hurt=" + (self.Hits < hits));
		}
	}
}
