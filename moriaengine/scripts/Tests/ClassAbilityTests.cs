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

		private static void Fill(Player player, RearAbility ability) {
			while (ClassAbilities.MayRaise(player, ability, false) && ClassAbilities.Raise(player, ability)) {
			}
		}

		private static void Report(string name, bool ok) {
			Console.WriteLine("CLASS_ABILITY: " + name + "=" + ok);
		}
	}
}
