using System;
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {

	/// <summary>#962 - server-side helpers for Tests/Protocol/CraftingRulesTests.cs.</summary>
	public static class CraftingRulesTests {
		[SteamFunction]
		public static void TestCraftDifficulty(Character self) {
			Console.WriteLine("CRAFT_DIFFICULTY: dagger=" + CraftingSkillDef.MakeDifficulty((ItemDef) ThingDef.GetByDefname("i_dagger_copper"))
				+ " boots=" + CraftingSkillDef.MakeDifficulty((ItemDef) ThingDef.GetByDefname("i_boots_calf")));
		}

		/// <summary>What failing at a given loss takes from 19 ingots of a real recipe.</summary>
		[SteamFunction]
		public static void TestCraftFailLoss(Character self, ScriptArgs sa) {
			int percent = int.Parse(sa.Args.Trim('"'));
			ItemDef recipe = (ItemDef) ThingDef.GetByDefname("i_dagger_copper");
			Item ingots = (Item) ((ItemDef) ThingDef.GetByDefname("i_ingot_copper")).Create(self.Backpack);
			ingots.Amount = 19;
			recipe.Resources.ConsumeSomeResources(self, ResourcesLocality.Backpack, percent);
			Console.WriteLine("CRAFT_LOSS: percent=" + percent + " left=" + (ingots.IsDeleted ? 0 : ingots.Amount));
			if (!ingots.IsDeleted) {
				ingots.Delete();
			}
		}
	}
}
