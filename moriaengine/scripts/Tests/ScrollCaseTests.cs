using System;
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts {

	/// <summary>#962 - helpers for Tests/Protocol/ScrollCaseTests.cs.</summary>
	public static class ScrollCaseTestHelpers {
		private static Item scrollCase;
		private static NPC dummy;

		private static Item Give(Character self, string defname, int amount) {
			Item item = (Item) ThingDef.GetByDefname(defname).Create(self.Backpack);
			item.Amount = amount;
			return item;
		}

		private static int Count(Character self, string defname) {
			int count = 0;
			foreach (Item item in self.Backpack) {
				if (item.Def == ThingDef.GetByDefname(defname)) {
					count += item.Amount;
				}
			}
			return count;
		}

		[SteamFunction]
		public static void TestScrollReady(Character self) {
			MovementSpeedTests.TestTeleportToOpenGround(self);
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			Player player = (Player) self;
			player.Profession = Professions.Mage;
			self.Account.PLevel = 1;
			self.SetRealSkillValue((int) SkillName.Magery, 1000);
			self.Int = 300;
			self.MaxMana = 300;
			self.Mana = 300;
			dummy = (NPC) ThingDef.GetByDefname("c_a_jackrabbit").Create(self.X + 1, self.Y, self.Z, self.M);
			dummy.DeleteTimer(TimerKey.Acquire("_npcAiTimer_"));
			Item light = (Item) ThingDef.GetByDefname("i_sconce_lit").Create(self.X, self.Y + 1, self.Z, self.M);
			Item plain = Give(self, "i_scroll_night_sight", 1);
			Item shield = Give(self, "i_customscroll_manashield", 2);
			Item bless = Give(self, "i_customscroll_mass_bless", 1);
			Item clumsy = Give(self, "i_scroll_clumsy", 5);
			scrollCase = Give(self, "i_scroll_case", 1);
			Console.WriteLine("SCROLL_READY: self=#" + self.Uid + " dummy=#" + dummy.Uid + " pack=#" + self.Backpack.Uid + " plain=#" + plain.Uid
				+ " shield=#" + shield.Uid + " bless=#" + bless.Uid + " clumsy=#" + clumsy.Uid + " case=#" + scrollCase.Uid + " light=#" + light.Uid);
		}

		[SteamFunction]
		public static void TestScrollReport(Character self) {
			Console.WriteLine("SCROLL_REPORT: mana=" + self.Mana
				+ " shielded=" + (self.GetPlugin(DamageManager.manaShieldKey) != null)
				+ " plain=" + Count(self, "i_scroll_night_sight")
				+ " shieldscrolls=" + Count(self, "i_customscroll_manashield")
				+ " clumsy=" + Count(self, "i_scroll_clumsy")
				+ " incase=" + t_svitkovac.CountIn(scrollCase, 1) + " shieldincase=" + t_svitkovac.CountIn(scrollCase, 104));
		}

		[SteamFunction]
		public static void TestScrollProfession(Character self, ScriptArgs sa) {
			((Player) self).Profession = ProfessionDef.GetByDefname(sa.Argv[0].ToString());
			Console.WriteLine("SCROLL_PROFESSION: done");
		}
	}
}
