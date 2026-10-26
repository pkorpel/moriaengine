/*
	This program is free software; you can redistribute it and/or modify
	it under the terms of the GNU General Public License as published by
	the Free Software Foundation; either version 2 of the License, or
	(at your option) any later version.

	This program is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
	GNU General Public License for more details.

	You should have received a copy of the GNU General Public License
	along with this program; if not, write to the Free Software
	Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
	Or visit http://www.gnu.org/copyleft/gpl.html
*/

using System;

namespace SteamEngine.CompiledScripts {

	/// <summary>Verification-harness diagnostics for the GM ".static" command (#697) - see
	/// Tests/Protocol/StaticCommandTests.cs.</summary>
	public static class StaticCommandTests {
		[SteamFunction]
		public static void TestCleanupNearbyRocks(Character self) {
			VerificationTestUtils.CleanupNearbyItems(self, (ItemDef) ThingDef.GetByDefname("i_rock_plain"), 20);
		}

		[SteamFunction]
		public static void TestReportPositionXYZ(Character self) {
			Console.WriteLine("STATIC_POSITION: x=" + self.X + " y=" + self.Y + " z=" + self.Z);
		}

		/// <summary>Reports the nearest i_rock_plain's Flag_NonMovable and whether it has an
		/// active decay timer scheduled - used both for the ".static" item itself and, as a
		/// no-regression check, for a plain ".add"-created item of the same type. i_rock_plain's
		/// own canonical Defname is an auto-generated hex id (e.g. "i_0x1363") - "i_rock_plain"
		/// only exists as its Altdefname, same quirk already documented in
		/// SpawnCommandTests.TestVerifySpawnResult for the spawn gem.</summary>
		[SteamFunction]
		public static void TestCheckNearestRockAttrs(Character self) {
			foreach (AbstractItem item in self.GetMap().GetItemsInRange(self.X, self.Y, 10)) {
				if (item.Def.Altdefname != null &&
					item.Def.Altdefname.Equals("i_rock_plain", StringComparison.OrdinalIgnoreCase)) {
					Item asItem = (Item) item;
					Console.WriteLine("STATIC_TEST: nonMovable=" + asItem.Flag_NonMovable +
						" hasDecayTimer=" + ItemDecaySystem.HasActiveTimer(asItem));
					return;
				}
			}
			Console.WriteLine("STATIC_TEST: no item found");
		}
	}
}
