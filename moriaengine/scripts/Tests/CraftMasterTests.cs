using System;
using System.Globalization;

namespace SteamEngine.CompiledScripts {

	/// <summary>#962 - helpers for Tests/Protocol/CraftMasterTests.cs.</summary>
	public static class CraftMasterTestHelpers {
		[SteamFunction]
		public static void TestMasterReady(Character self) {
			self.Account.PLevel = 1;
			self.SetRealSkillValue((int) SkillName.Lumberjacking, 1000);
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			Item sword = (Item) ThingDef.GetByDefname("i_katana_verite").Create(self.Backpack);
			CraftingSkillDef.SetCrafter(sword, self);
			sword.SetTag(TagKey.Acquire("_quality_"), 100);
			Item foreign = (Item) ThingDef.GetByDefname("i_katana_verite").Create(self.Backpack);
			Item logs = (Item) ThingDef.GetByDefname("i_log_obsidian").Create(self.Backpack);
			logs.Amount = 10;
			NPC master = (NPC) ((NPCDef) ThingDef.GetByDefname("c_mistr_craft")).Create(self.X + 1, self.Y, self.Z, self.M);
			Console.WriteLine("MASTER_READY: mine=" + CraftMasters.ExperienceFor(self, sword) + " foreign=" + CraftMasters.ExperienceFor(self, foreign)
				+ " logs=" + CraftMasters.ExperienceForMaterial(self, logs) + " value=" + ((ItemDef) logs.Def).ItemValue + " factor=" + CraftMasters.ConstantOf("craftexp_factor")
				+ " speech=" + master.Def.PrettyDefname);
		}
	}
}
