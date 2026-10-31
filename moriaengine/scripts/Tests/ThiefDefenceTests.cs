using System;

namespace SteamEngine.CompiledScripts {

	/// <summary>#866 - server-side helper for Tests/Protocol/ThiefDefenceTests.cs.</summary>
	public static class ThiefDefenceTests {
		[SteamFunction]
		public static void TestThiefDefenceReport(Character self) {
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			Player player = (Player) self;
			player.Profession = Professions.Thief;
			self.DisArm();
			self.NewEquip(ThingDef.GetByDefname("i_cutlass_copper"));
			self.SetRealAbilityPoints(AbilityDef.GetByDefname("a_thief_rebound"), 30);
			self.SetRealAbilityPoints(AbilityDef.GetByDefname("a_thief_defstance"), 10);
			self.MaxStam = 200;
			self.Stam = 200;
			NPC monster = (NPC) ((NPCDef) ThingDef.GetByDefname("npc_test_resist")).Create(self.X + 1, self.Y, self.Z, self.M);

			int plain = CountRebounds(monster, self);
			ThiefDefence.Vigilance(self);
			int vigilant = CountRebounds(monster, self);
			int weakens = ThiefDefence.WeakensOwnBlow(self, monster) ? 1 : 0;
			int fromPlayer = CountRebounds(self, self);
			self.Stam = 200;
			ThiefDefence.Vigilance(self);

			self.SetRealAbilityPoints(AbilityDef.GetByDefname("a_thief_rebound"), 0);
			ThiefDefence.SetMagicDefence(self, 50, 0, 0, 0);
			int reflected = 0;
			for (int i = 0; i < 10000; i++) {
				if (ThiefDefence.TryAvoid(monster, self, DamageType.PhysicalBlunt, 10, out double back) && (back > 0)) {
					reflected++;
				}
			}
			ThiefDefence.SetMagicDefence(self, 0, 100, 0, 0);
			int blocked = (ThiefDefence.TryAvoid(monster, self, DamageType.PhysicalBlunt, 10, out double none) && (none == 0)) ? 1 : 0;
			ThiefDefence.SetMagicDefence(self, 0, 0, 0, 0);
			monster.Delete();
			Console.WriteLine("THIEF_DEFENCE: plain=" + plain + " vigilant=" + vigilant + " weakens=" + weakens + " fromPlayer=" + fromPlayer +
				" reflected=" + reflected + " blocked=" + blocked);
		}

		/// <summary>#865 - wear to destruction, and repair at an anvil for ingots.</summary>
		[SteamFunction]
		public static void TestWearAndRepairReport(Character self) {
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			self.DisArm();
			Player player = (Player) self;
			Weapon sword = (Weapon) self.NewEquip(ThingDef.GetByDefname("i_sword_long_iron"));
			int max = sword.MaxDurability;
			int fresh = sword.Durability;
			int certain = (max * 16) + 1;
			sword.TakeWear(certain, self);
			int worn = sword.Durability;
			sword.TakeWear(1, self); //nearly always nothing
			sword.Durability = 1;
			sword.TakeWear(certain, self);
			bool destroyed = sword.IsDeleted;

			Weapon second = (Weapon) self.NewEquip(ThingDef.GetByDefname("i_sword_long_iron"));
			second.Durability = (ushort) (second.MaxDurability / 2);
			Repair.TryRepair(player, second);
			int awayFromAnvil = second.Durability;
			Item anvil = (Item) ThingDef.FindItemDef(0xfaf).Create(self.X + 1, self.Y, self.Z, self.M);
			Item ingots = (Item) ThingDef.GetByDefname("i_ingot_iron").Create(self.Backpack);
			ingots.Amount = 100;
			self.SetRealSkillValue((int) SkillName.Blacksmith, 1000);
			int repairedAt = 0;
			for (int i = 0; (i < 20) && (second.Durability < second.MaxDurability); i++) {
				Repair.TryRepair(player, second);
				repairedAt = i + 1;
			}
			bool repaired = second.Durability == second.MaxDurability;
			int maxAfter = second.MaxDurability;
			int ingotsLeft = ingots.IsDeleted ? 0 : ingots.Amount;
			anvil.Delete();
			second.Delete();
			Console.WriteLine("WEAR_REPAIR: max=" + max + " fresh=" + fresh + " worn=" + worn + " destroyed=" + (destroyed ? 1 : 0) +
				" awayFromAnvil=" + awayFromAnvil + " half=" + (max / 2) + " repaired=" + (repaired ? 1 : 0) + " tries=" + repairedAt + " maxAfter=" + maxAfter + " ingotsLeft=" + ingotsLeft);
		}

		private static int CountRebounds(Character attacker, Character defender) {
			int count = 0;
			for (int i = 0; i < 10000; i++) {
				if (ThiefDefence.RollRebound(attacker, defender)) {
					count++;
				}
			}
			return count;
		}
	}
}
