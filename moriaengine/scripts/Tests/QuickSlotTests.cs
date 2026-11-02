using System;

namespace SteamEngine.CompiledScripts {

	/// <summary>#962 - helpers for Tests/Protocol/QuickSlotTests.cs.</summary>
	public static class QuickSlotTestHelpers {
		[SteamFunction]
		public static void TestSlotsReady(Character self) {
			self.Account.PLevel = 1;
			self.SetRealSkillValue((int) SkillName.Healing, 1000);
			self.SetRealSkillValue((int) SkillName.Anatomy, 1000);
			ThingDef.GetByDefname("i_bandage").Create(self.Backpack);
			NPC friend = (NPC) ((NPCDef) ThingDef.GetByDefname("c_a_jackrabbit")).Create(self.X + 1, self.Y, self.Z, self.M);
			friend.MaxHits = 100;
			friend.Hits = 10;
			Item dagger = (Item) ThingDef.GetByDefname("i_dagger").Create(self.X, self.Y + 1, self.Z, self.M);
			Console.WriteLine("SLOTS_READY: npc=#" + friend.Uid + " item=#" + dagger.Uid);
		}
	}
}
