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

	/// <summary>
	/// V-6 (verification pass, #74): server-side helpers for housing scenarios driven by real
	/// deed double-click + target-cursor placement packets from Tests/Protocol/HousingTests.cs.
	/// Friend/co-owner/ban list management has no packet-level UI yet (see House.cs's own doc
	/// comment) - those are exercised by calling the real House methods directly, same as any
	/// other "Test*" setup helper in this session, not reimplementing or faking the logic.
	/// </summary>
	public static class HousingTests {
		/// <summary>Real finding while building this: every test method in a fixture/collection
		/// shares one long-lived server process, and every test character spawns at the same
		/// default point - so a leftover house from an earlier test in the same collection sits
		/// exactly where a later test's own placement attempt would go, silently rejecting it as
		/// a (spurious) overlap. Called once at the start of each test, same idiom as
		/// VerificationTestUtils.CleanupNearbyNpcs - NOT folded into TestGrantHouseDeed, which a
		/// single test calls more than once (that would delete its own just-placed house before
		/// the overlap check it's trying to set up).</summary>
		[SteamFunction]
		public static void TestCleanupNearbyHouses(Character self) {
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			House leftover = FindNearbyHouse(self);
			if (leftover != null) {
				leftover.Delete();
			}
			Console.WriteLine("HOUSE_CLEANUP: DONE");
		}

		[SteamFunction]
		public static void TestGrantHouseDeed(Character self) {
			ThingDef deedDef = ThingDef.GetByDefname("i_deed_house_stone_plaster_small");
			AbstractItem deed = (AbstractItem) deedDef.Create(self.Backpack);
			Console.WriteLine("HOUSE_SETUP: deed=#" + deed.Uid + " backpack=#" + self.Backpack.Uid);
		}

		/// <summary>Counts real House instances within 15 tiles of self's own current position -
		/// used both to confirm a placement succeeded (count goes from 0 to 1) and that a
		/// second, overlapping placement attempt was genuinely rejected (count stays at 1, not 2).</summary>
		[SteamFunction]
		public static void TestCheckHouseNearby(Character self) {
			int count = 0;
			Character owner = null;
			foreach (AbstractItem item in self.GetMap().GetItemsInRange(self.X, self.Y, 15)) {
				House h = item as House;
				if (h != null) {
					count++;
					owner = h.Owner;
				}
			}
			Console.WriteLine("HOUSE_CHECK: count=" + count + " owner=" + (owner != null ? owner.Name : "none"));
		}

		[SteamFunction]
		public static void TestAddCoOwnerToNearbyHouse(Character self, ScriptArgs sa) {
			int coOwnerUid = Convert.ToInt32(sa.Argv[0], System.Globalization.CultureInfo.InvariantCulture);
			Character coOwner = (Character) Thing.UidGetCharacter(coOwnerUid);
			House house = FindNearbyHouse(self);
			if (house == null) {
				Console.WriteLine("HOUSE_ACCESS_SETUP: no house nearby");
				return;
			}
			house.AddCoOwner(coOwner);
			Console.WriteLine("HOUSE_ACCESS_SETUP: added co-owner " + coOwner.Name);
		}

		[SteamFunction]
		public static void TestCheckHouseAccess(Character self, ScriptArgs sa) {
			int targetUid = Convert.ToInt32(sa.Argv[0], System.Globalization.CultureInfo.InvariantCulture);
			Character target = (Character) Thing.UidGetCharacter(targetUid);
			House house = FindNearbyHouse(self);
			if (house == null) {
				Console.WriteLine("HOUSE_ACCESS_CHECK: no house nearby");
				return;
			}
			Console.WriteLine("HOUSE_ACCESS_CHECK: " + target.Name + " hasAccess=" + house.HasAccess(target));
		}

		private static House FindNearbyHouse(Character self) {
			foreach (AbstractItem item in self.GetMap().GetItemsInRange(self.X, self.Y, 15)) {
				House h = item as House;
				if (h != null) {
					return h;
				}
			}
			return null;
		}
	}
}
