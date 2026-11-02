using System;
using System.Globalization;

namespace SteamEngine.CompiledScripts {

	/// <summary>#962 - helpers for Tests/Protocol/InformationSkillTests.cs.</summary>
	public static class InformationSkillTestHelpers {
		[SteamFunction]
		public static void TestInfoPrepare(Character self, ScriptArgs sa) {
			int value = int.Parse(sa.Args.Trim('"'), CultureInfo.InvariantCulture);
			self.Account.PLevel = 1;
			foreach (SkillName skill in new[] { SkillName.Anatomy, SkillName.ArmsLore, SkillName.ItemID, SkillName.EvalInt, SkillName.Forensics, SkillName.TasteID }) {
				self.SetRealSkillValue((int) skill, value);
			}
			Console.WriteLine("INFO_PREPARED: " + value);
		}

		[SteamFunction]
		public static void TestInfoNpc(Character self) {
			NPC npc = (NPC) ((NPCDef) ThingDef.GetByDefname("c_a_jackrabbit")).Create(self.X + 1, self.Y, self.Z, self.M);
			npc.Name = "Test Rabbit";
			npc.Str = 95;
			npc.Dex = 5;
			npc.Int = 35;
			Console.WriteLine("INFO_NPC: #" + npc.Uid);
		}

		[SteamFunction]
		public static void TestInfoItem(Character self, ScriptArgs sa) {
			Item item = (Item) ThingDef.GetByDefname(sa.Args.Trim('"')).Create(self.X, self.Y + 1, self.Z, self.M);
			Console.WriteLine("INFO_ITEM: #" + item.Uid);
		}

		[SteamFunction]
		public static void TestInfoCorpse(Character self) {
			NPC npc = (NPC) ((NPCDef) ThingDef.GetByDefname("c_a_jackrabbit")).Create(self.X, self.Y + 1, self.Z, self.M);
			npc.CauseDeath(self);
			foreach (AbstractItem item in self.GetMap().GetItemsInRange(self.X, self.Y, 2)) {
				if (item is Corpse corpse) {
					Console.WriteLine("INFO_CORPSE: #" + corpse.Uid);
					return;
				}
			}
			Console.WriteLine("INFO_CORPSE: none");
		}
	}
}
