using System;
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {
	/// <summary>#1017 - spending Erebor's two points a level, by class and level.</summary>
	public static class ClassAbilityTests {
		[SteamFunction]
		public static void TestClassAbilityRules(Character self) {
			Player player = (Player) self;
			player.Account.PLevel = 1;
			player.Profession = Professions.Thief;
			ClassAbilities.Reset(player);
			RearAbility dodge = ClassAbilities.Find("weapdodge"), stun = ClassAbilities.Find("stuntattack"), regen = ClassAbilities.Find("manareg");

			player.Level = 0;
			Report("noLevelNoPoints", (ClassAbilities.Spare(player) == 0) && !ClassAbilities.Raise(player, regen));

			player.Level = 60;
			Report("sparePointsAreLevelTimesTwo", ClassAbilities.Spare(player) == 120);
			Report("notForThisClass", !ClassAbilities.Raise(player, stun));
			Report("offeredOnlyWhatTheClassHas", !ClassAbilities.Available(player).Contains(stun) && ClassAbilities.Available(player).Contains(dodge));

			Fill(player, dodge);
			Report("level60ThiefDodgeStopsAt20", ClassAbilities.Points(player, "weapdodge") == 20);
			player.Level = 61;
			Fill(player, dodge);
			Report("level61ThiefDodgeGoesTo40", ClassAbilities.Points(player, "weapdodge") == 40);
			Report("spentPointsLeaveTheSpare", ClassAbilities.Spare(player) == 82);
			Report("anExistingAbilityGetsThePoints", player.GetAbility(AbilityDef.GetByDefname("a_weapon_dodge")) == 40);

			player.Profession = Professions.Mage;
			Report("aMageKeepsToItsOwnLimit", !ClassAbilities.MayRaise(player, dodge, false));

			player.Profession = Professions.Warrior;
			player.Level = 49;
			Report("tooYoungForTheFirstStep", !ClassAbilities.MayRaise(player, ClassAbilities.Find("stuntattack_prodleva"), false));
			player.Level = 50;
			Report("oldEnoughForIt", ClassAbilities.Raise(player, ClassAbilities.Find("stuntattack_prodleva")));
			Report("aTagAbilityGetsThePoint", ClassAbilities.Points(player, "stuntattack_prodleva") == 1);

			ClassAbilities.Reset(player);
			Report("resetClearsEverything", ClassAbilities.Spent(player) == 0);

			player.Level = 10;
			Console.WriteLine("CLASS_ABILITY_DONE");
		}

		[SteamFunction]
		public static void TestClassAbilityReport(Character self) {
			Console.WriteLine("CLASS_ABILITY_POINTS: manareg=" + ClassAbilities.Points(self, "manareg") + " end");
		}

		/// <summary>Sets a warrior up with rabbits to hit and a two handed sword.</summary>
		[SteamFunction]
		public static void TestWarriorAbilities(Character self) {
			Player player = (Player) self;
			MovementSpeedTests.TestTeleportToOpenGround(self);
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			player.Account.PLevel = 1;
			player.Profession = Professions.Warrior;
			player.Level = 100;
			player.Str = 250;
			player.MaxStam = 200;
			player.Stam = 200;
			ClassAbilities.Reset(player);
			foreach (SkillName skill in new[] { SkillName.Swords, SkillName.Tactics, SkillName.Anatomy, SkillName.ArmsLore }) {
				player.SetRealSkillValue((int) skill, 1000);
			}
			player.Dex = 100;

			ClassAbilities.SetPoints(player, "roundattack", 10);
			Item sword = (Item) ((ItemDef) ThingDef.GetByDefname("i_axe_battle_large")).Create(player);
			Console.WriteLine("WARRIOR_INFO: weapon type=" + ((sword.Type == null) ? "null" : sword.Type.Defname) + " layer=" + sword.Layer);
			NPC near = Dummy(self, 1), far = Dummy(self, 3);
			short nearHits = near.Hits, farHits = far.Hits;
			double weapAttack = CombatSettings.instance.weapAttack;
			CombatSettings.instance.weapAttack = weapAttack * 100; //a bare test character's blow doesn't get through a rabbit's hide
			WarriorAbilities.FinishRoundAttack(player);
			CombatSettings.instance.weapAttack = weapAttack;
			Report("roundAttackHitsWhoIsNear", near.Hits < nearHits);
			Report("roundAttackLeavesTheRest", far.Hits == farHits);
			player.Mana = 50;
			ClassAbilities.SetPoints(player, "warcry", 10);
			WarriorAbilities.Warcry(player);
			Report("warcryTakesOwnMana", player.Mana == 0);
			Report("warcryMustCoolDown", WarriorAbilities.IsWarrior(player));

			ClassAbilities.SetPoints(player, "stuntattack", 10);
			WarriorAbilities.Omraceni(player);
			Report("stunWindowsShrink", (WarriorAbilities.StunWindowSeconds(0) == 20) && (WarriorAbilities.StunWindowSeconds(30) == 11));
			Report("stunLimitGrowsWithTheMetal", (WarriorAbilities.StunLimit(Material.Copper, false, 0) == 51) && (WarriorAbilities.StunLimit(Material.Mithril, false, 30) == 901 + (901 * 20 / 100)));
			Report("plainMaceStunsNothing", WarriorAbilities.StunLimit(Material.None, false, 0) == 0);
			Report("shieldsByMetal", (WarriorAbilities.ShieldBlockPercent(Material.Mithril) == 50) && (WarriorAbilities.ShieldBlockPercent(Material.None) == 100));

			player.Stam = 200;
			WarriorAbilities.DefStance(player);
			Report("stanceOnCostsHalfStamina", WarriorAbilities.InDefStance(player) && (player.Stam == 100));
			player.Stam = 200;
			WarriorAbilities.DefStance(player);
			Report("stanceOff", !WarriorAbilities.InDefStance(player));

			ClassAbilities.SetPoints(player, "taunt", 10);
			player.Stam = 200;
			near.Delete();
			far.Hits = far.MaxHits;
			WarriorAbilities.TauntOne(player, far);
			NPCAiTimer ai = NPCAiTimer.GetFor(far);
			Report("tauntTurnsItOnTheWarrior", (ai.Target == player) && far.HasTimer(TauntTimer.Key));
			Report("tauntingHardensIt", TauntTimer.Resist(far) == 250);

			Character none = WeaponSkillTargetQueuePlugin.CurrentTarget(player);
			Report("leapNeedsAFight", none == null);
			player.Stam = player.MaxStam;
			far.Delete();
			Console.WriteLine("WARRIOR_DONE");
		}

		private static NPC Dummy(Character self, int offset) {
			NPC dummy = (NPC) ((NPCDef) ThingDef.GetByDefname("c_a_jackrabbit")).Create(self.X + offset, self.Y, self.Z, self.M);
			dummy.MaxHits = 30000;
			dummy.Hits = 30000;
			return dummy;
		}

		private static void Fill(Player player, RearAbility ability) {
			while (ClassAbilities.MayRaise(player, ability, false) && ClassAbilities.Raise(player, ability)) {
			}
		}

		private static void Report(string name, bool ok) {
			Console.WriteLine("CLASS_ABILITY: " + name + "=" + ok);
		}
	}
}
