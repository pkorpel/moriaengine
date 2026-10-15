using System;

namespace SteamEngine.CompiledScripts {
	/// <summary>Verification-harness diagnostics for the GM ".spawn" command (#191) - see
	/// Tests/Protocol/SpawnCommandTests.cs.</summary>
	public static class SpawnCommandTests {
		/// <summary>Prints whether a spawn gem (ItemDef altdefname "i_worldgem_bit") and at least
		/// one other character are present near self. The gem's canonical Defname is an
		/// auto-generated hex id (e.g. "i_0x1ea7") - "i_worldgem_bit" only exists as its
		/// Altdefname, which is why this checks Altdefname rather than Defname.</summary>
		[SteamFunction]
		public static void TestVerifySpawnResult(Character self) {
			bool foundGem = false;
			foreach (AbstractItem item in self.GetMap().GetItemsInRange(self.X, self.Y, 20)) {
				if (item.Def.Altdefname != null &&
					item.Def.Altdefname.Equals("i_worldgem_bit", StringComparison.OrdinalIgnoreCase)) {
					foundGem = true;
					break;
				}
			}

			bool foundNpc = false;
			foreach (AbstractCharacter ch in self.GetMap().GetCharsInRange(self.X, self.Y, 20)) {
				if (ch != self) {
					foundNpc = true;
					break;
				}
			}

			Console.WriteLine("SPAWN_TEST: gem=" + foundGem + " npc=" + foundNpc);
		}
	}
}
