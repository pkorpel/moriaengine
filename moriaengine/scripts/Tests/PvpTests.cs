using System;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {
	/// <summary>#1116 - server side of Tests/Protocol/PvpTests.cs.</summary>
	public static class PvpTests {
		private static FlaggedRegion Area(string defname) {
			return (FlaggedRegion) StaticRegion.GetByDefname(defname);
		}

		/// <summary>The converter's tag.arena and tag.bf, as the regions loaded them.</summary>
		[SteamFunction]
		public static void TestPvpRegionTags(Character self) {
			string arenas = "";
			foreach (string name in new[] { "a_Arena_Mordor", "a_Mordorska_bojova_arena", "a_Nova_Arena_Equip_Room", "a_Arena_Lamy", "a_Murdoc" }) {
				arenas += " " + name + "=" + Area(name).Flag_Arena;
			}
			string fronts = "";
			foreach (string name in new[] { "a_Pelenorska_pole", "a_Anorien", "a_Vychodni_breh", "a_Minas_Morgul", "a_Rhovanion" }) {
				fronts += " " + name + "=" + Area(name).Battlefield;
			}
			Console.WriteLine("PVP_REGIONS:" + arenas + fronts + " |");
		}
	}
}
