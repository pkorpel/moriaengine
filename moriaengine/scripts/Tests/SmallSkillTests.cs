using System;
using System.Globalization;

namespace SteamEngine.CompiledScripts {

	/// <summary>#962 - helpers for Tests/Protocol/SmallSkillTests.cs.</summary>
	public static class SmallSkillTestHelpers {
		[SteamFunction]
		public static void TestSmallPrepare(Character self) {
			self.Account.PLevel = 1;
			MovementSpeedTests.TestTeleportToOpenGround(self);
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			foreach (SkillName skill in new[] { SkillName.Begging, SkillName.SpiritSpeak, SkillName.Herding, SkillName.Camping }) {
				self.SetRealSkillValue((int) skill, 1200);
			}
			Console.WriteLine("SMALL_PREPARED: done");
		}

		[SteamFunction]
		public static void TestSmallThing(Character self, ScriptArgs sa) {
			Thing thing = ThingDef.GetByDefname(sa.Args.Trim('"')).Create(self.X, self.Y + 1, self.Z, self.M);
			if (thing is NPC npc) {
				npc.Int = 10;
				npc.Name = "Test Sheep";
				npc.Dex = 100;
			}
			Console.WriteLine("SMALL_THING: #" + thing.Uid);
		}

		[SteamFunction]
		public static void TestSmallCrook(Character self) {
			Item crook = (Item) ThingDef.GetByDefname("i_shepherds_crook_copper").Create(self.Backpack);
			self.TryEquip(crook);
			Console.WriteLine("SMALL_CROOK: #" + crook.Uid);
		}

		[SteamFunction]
		public static void TestSmallReport(Character self, ScriptArgs sa) {
			NPC npc = (NPC) Thing.UidGetCharacter(int.Parse(sa.Args.Trim('"'), CultureInfo.InvariantCulture));
			Console.WriteLine("SMALL_REPORT: goal=" + npc.PetGoalPoint + " spirits=" + (self.GetTimer(SpiritContactTimer.Key) != null));
		}

		[SteamFunction]
		public static void TestSmallFire(Character self) {
			bool found = false;
			foreach (AbstractItem item in self.GetMap().GetItemsInRange(self.X, self.Y, 2)) {
				found |= item.Def.PrettyDefname == "i_campfire";
			}
			Console.WriteLine("SMALL_FIRE: " + found);
		}
	}
}
