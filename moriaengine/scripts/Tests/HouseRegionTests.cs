using System;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {

	/// <summary>#941 - server-side helper for Tests/Protocol/HouseRegionTests.cs.</summary>
	public static class HouseRegionTests {
		[SteamFunction]
		public static void TestHouseRegions(Character self) {
			int numbered = 0, houses = 0, gondor = 0, mordor = 0;
			foreach (StaticRegion region in StaticRegion.AllRegions) {
				FlaggedRegion flagged = region as FlaggedRegion;
				if ((flagged != null) && (flagged.HouseNumber != 0)) {
					numbered++;
					if (flagged.Name.StartsWith("dum ", StringComparison.Ordinal)) {
						houses++;
					}
					if (flagged.HouseRealm == Realm.Gondor) {
						gondor++;
					} else if (flagged.HouseRealm == Realm.Mordor) {
						mordor++;
					}
				}
			}
			FlaggedRegion first = (FlaggedRegion) StaticRegion.GetByDefname("a_dum_2");
			Console.WriteLine("HOUSE_REGIONS: numbered=" + numbered + " houses=" + houses + " gondor=" + gondor + " mordor=" + mordor
				+ " dum2=" + first.HouseNumber + "/" + first.HouseRealm);
		}
	}
}
