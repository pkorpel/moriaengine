using System;

namespace SteamEngine.CompiledScripts {

	/// <summary>#882 - server-side helpers for Tests/Protocol/StatSkillPortTests.cs.</summary>
	public static class StatSkillPortTests {
		[SteamFunction]
		public static void TestSkillPortProfession(Character self) {
			Player player = (Player) self;
			player.Profession = ProfessionDef.GetByDefname("profession_warrior");
			player.Level = 0;
			player.Str = 100;
			player.Dex = 80;
			player.Int = 40;
			player.Vit = 80;
			LevelingSystem.RecordStartingStats(player);
			Console.WriteLine("SKILLPORT_READY: stats=" + (player.Str + player.Dex + player.Int + player.Vit));
		}

		/// <summary>Args: raw DetectHidden value, points; one click of d_levelbonus on it (warrior: 0 to 100.0).</summary>
		[SteamFunction]
		public static void TestSkillPortRaise(Character self, ScriptArgs sa) {
			Player player = (Player) self;
			string[] args = sa.Args.Trim().Trim('"').Split(' ');
			player.SetRealSkillValue((int) SkillName.DetectHidden, int.Parse(args[0]));
			player.UnspentSkillPoints = int.Parse(args[1]);
			string refusal = LevelingSystem.RaiseSkill(player, SkillName.DetectHidden);
			Console.WriteLine("SKILLPORT_RAISE: value=" + player.GetRealSkillValue(SkillName.DetectHidden) + " points=" + player.UnspentSkillPoints +
				" refusal=" + (refusal ?? "none").Replace(' ', '_'));
		}

		/// <summary>Sets the level, spends one point on Str at this Str, or under a bless ("bless").</summary>
		[SteamFunction]
		public static void TestSkillPortStat(Character self, ScriptArgs sa) {
			Player player = (Player) self;
			string[] args = sa.Args.Trim().Trim('"').Split(' ');
			player.Level = int.Parse(args[0]);
			int startStr = player.Str;
			if (args[1] == "bless") {
				((SpellDef) SpellDef.GetByDefname("s_bless")).ApplyEffectChar(player, player, 500, EffectFlag.FromSpellBook | EffectFlag.BeneficialEffect);
			} else {
				player.Str = short.Parse(args[1]);
				player.StrPointsAllocated += player.Str - startStr;
			}
			int budget = player.AttributePointBudget;
			bool spent = LevelingSystem.TrySpendAttributePoints(player, AttributeStat.Str, 1, out string refusal);
			Console.WriteLine("SKILLPORT_STAT: spent=" + spent + " budgetBefore=" + budget + " refusal=" + (refusal ?? "none").Replace(' ', '_'));
		}

		[SteamFunction]
		public static void TestSkillPortDeath(Character self) {
			Player player = (Player) self;
			player.SetRealSkillValue((int) SkillName.Tactics, 412);
			player.SetRealSkillValue((int) SkillName.Mining, 412);
			LevelingSystem.TrimSkillsAtDeath(player);
			Console.WriteLine("SKILLPORT_DEATH: tactics=" + player.GetRealSkillValue(SkillName.Tactics) + " mining=" + player.GetRealSkillValue(SkillName.Mining));
		}
	}
}
