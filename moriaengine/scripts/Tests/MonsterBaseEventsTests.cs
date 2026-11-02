using System;
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1022 - helpers for Tests/Protocol/MonsterBaseEventsTests.cs.</summary>
	public static class MonsterBaseEventsTests {
		private static NPC Monster(Character self, int offset, string defname = "c_orc") {
			NPC monster = (NPC) ThingDef.GetByDefname(defname).Create(self.X + offset, self.Y, self.Z, self.M);
			monster.DeleteTimer(TimerKey.Acquire("_npcAiTimer_"));
			monster.Flag_Invulnerable = false;
			return monster;
		}

		[SteamFunction]
		public static void TestBaseCorpse(Character self) {
			MovementSpeedTests.TestTeleportToOpenGround(self);
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			NPC orc = Monster(self, 1);
			orc.Name = "Grishnakh";
			string kind = orc.Def.Name;
			orc.CauseDeath(self);
			string found = "none";
			foreach (Thing thing in self.GetMap().GetThingsInRange(self.X, self.Y, 3)) {
				if (thing is Corpse corpse) {
					found = corpse.Name;
				}
			}
			Console.WriteLine("BASE_CORPSE: kind=" + kind + "| corpse=" + found + "|");
		}

		[SteamFunction]
		public static void TestBaseInvisibility(Character self) {
			NPC summon = Monster(self, 2);
			NPC plain = Monster(self, 3);
			SummonSpellDef.MarkAsSummonedCreature(summon);
			SpellDef spell = (SpellDef) SpellDef.GetByDefname("s_invisibility");
			spell.ApplyEffectChar(self, summon, 500, EffectFlag.FromSpellBook);
			spell.ApplyEffectChar(self, plain, 500, EffectFlag.FromSpellBook);
			Console.WriteLine("BASE_INVIS: summonGone=" + (summon.IsDeleted || summon.Flag_Dead) + " plainAlive=" + !plain.Flag_Dead + " plainInvis=" + plain.Flag_InvisByMagic);
		}

		[SteamFunction]
		public static void TestBaseTurn(Character self) {
			NPC monster = (NPC) ThingDef.GetByDefname("c_orc").Create(self.X + 2, self.Y + 2, self.Z, self.M);
			int before = CombatSettings.instance.monsterTurnsOnAttackerOneIn;
			CombatSettings.instance.monsterTurnsOnAttackerOneIn = 1;
			NPCAiTimer.MaybeTurnOn(monster, self);
			CombatSettings.instance.monsterTurnsOnAttackerOneIn = 0;
			NPC other = (NPC) ThingDef.GetByDefname("c_orc").Create(self.X - 2, self.Y + 2, self.Z, self.M);
			NPCAiTimer.MaybeTurnOn(other, self);
			CombatSettings.instance.monsterTurnsOnAttackerOneIn = before;
			Console.WriteLine("BASE_TURN: on=" + (NPCAiTimer.GetFor(monster)?.Target == self) + " off=" + (NPCAiTimer.GetFor(other)?.Target == self));
		}

		[SteamFunction]
		public static void TestBaseTelepad(Character self) {
			NPC monster = Monster(self, 4);
			NPC pet = Monster(self, 5);
			pet.ControlMaster = self;
			Console.WriteLine("BASE_PAD: monsterRefused=" + t_telepad.Refuses(monster) + " petRefused=" + t_telepad.Refuses(pet) + " playerRefused=" + t_telepad.Refuses(self));
		}

		[SteamFunction]
		public static void TestBaseClickTarget(Character self) {
			NPC monster = Monster(self, 6);
			monster.Flag_Hidden = true;
			Console.WriteLine("BASE_CLICK: uid=#" + monster.Uid);
		}
	}
}
