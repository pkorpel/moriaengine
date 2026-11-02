using System;
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {
	/// <summary>#1037 - what an office puts on the body stays there through death.</summary>
	public static class NazgulBadgeDeathTests {
		[SteamFunction]
		public static void TestBadgeThroughDeath(Character self) {
			self.Account.PLevel = 1;
			Check(self, "i_ring_power", "ring");
			Check(self, "i_leading_ring_power", "leadingRing");
			Check(self, "i_generals_bracelet", "bracelet");
			Console.WriteLine("BADGE_DONE");
		}

		private static void Check(Character self, string defname, string name) {
			PowerBadge badge = PowerBadge.GiveTo(self, defname);
			Report(name + "StartsOnTheBody", badge.Cont == self);

			self.CauseDeath(null);
			Report(name + "IsOnTheGhost", badge.Cont == self);
			Report(name + "NoCorpseHoldsIt", badge.TopObj() == self);
			self.Resurrect((Corpse) null);
			Report(name + "IsOnTheResurrected", badge.Cont == self);

			badge.Cont = self.Backpack;
			Report(name + "ThePackIsNotTheBody", PowerBadge.Find(self) == null);
			self.On_LogIn();
			Report(name + "LoginPutsItBackOn", (badge.Cont == self) && (PowerBadge.Find(self) == badge));

			self.Hits = self.MaxHits;
			badge.Delete();
		}

		private static void Report(string name, bool ok) {
			Console.WriteLine("BADGE: " + name + "=" + ok);
		}
	}
}
