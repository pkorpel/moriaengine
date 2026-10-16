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
using System.Globalization;

namespace SteamEngine.CompiledScripts {
	/// <summary>Server-side helpers for #221 (house lockdown mechanic) protocol tests - reuses
	/// HousingTests.cs's own TestCleanupNearbyHouses/TestGrantHouseDeed/TestReportMyUid for the
	/// real deed-placement flow (same global [SteamFunction] dispatch table, no need to
	/// duplicate), adds only what #221 itself needs on top: friend-list setup, placing a test
	/// item/container inside the house, and reporting where an item ended up.</summary>
	public static class HouseLockdownTests {
		// #210 moved PlayerCreation.startingPosition to a deliberately narrow, boxed-in
		// antechamber (4763,1363,10,0) - HousingTests.cs's own "+5,+5 from spawn" house-
		// placement offset was tuned against the OLD spawn point and silently broke once that
		// changed (confirmed: HousingTests.DeedPlacementRejectsOverlap itself now fails the
		// same way, unrelated to #221 - filed as its own issue). Reuses MovementSpeedTests.
		// TestTeleportToOpenGround (#78's own expanding-ring open-ground search, already
		// checks for a 7-tile-radius static-free zone) instead of adding a second, narrower,
		// hardcoded-coordinate teleport helper.

		/// <summary>Real finding while building this: On_DenyPickup correctly allowed a
		/// co-owner's pickup (CanMoveLockedItems returned true) but the overall attempt still
		/// silently failed - AbstractCharacter.CanPickup ALSO checks CanReach after the deny-
		/// trigger chain passes, and every test character in this file spawns at its own
		/// default position, nowhere near the house. A single comma-joined "x,y,z" argument
		/// (not 3 separate space-separated ones) sidesteps the usual ScriptArgs dual-shape
		/// issue entirely, since these accounts route through SimpleCommandParser (no
		/// splitting at all) - there's nothing to split either way.</summary>
		[SteamFunction]
		public static void TestTeleportTo(Character self, ScriptArgs sa) {
			string[] parts = sa.Argv[0].ToString().Split(',');
			int x = Convert.ToInt32(parts[0], CultureInfo.InvariantCulture);
			int y = Convert.ToInt32(parts[1], CultureInfo.InvariantCulture);
			int z = Convert.ToInt32(parts[2], CultureInfo.InvariantCulture);
			self.P(x, y, z, self.M);
			Console.WriteLine("HOUSE_LOCKDOWN_SETUP: teleported");
		}

		[SteamFunction]
		public static void TestAddFriendToNearbyHouse(Character self, ScriptArgs sa) {
			int friendUid = Convert.ToInt32(sa.Argv[0], CultureInfo.InvariantCulture);
			Character friend = (Character) Thing.UidGetCharacter(friendUid);
			House house = FindNearbyHouse(self);
			house.AddFriend(friend);
			Console.WriteLine("HOUSE_LOCKDOWN_SETUP: added friend " + friend.Name);
		}

		/// <summary>Places a plain item directly at the house's own anchor point - guaranteed
		/// inside its MULTIREGION footprint (every HouseDef's own MULTIREGION rectangle
		/// includes the origin (0,0), confirmed against item_multis.def) - and locks it down.</summary>
		[SteamFunction]
		public static void TestSetupLockedDownItem(Character self) {
			House house = FindNearbyHouse(self);
			ItemDef plainDef = (ItemDef) ThingDef.GetByDefname("i_rock_plain");
			Item item = (Item) plainDef.Create(house.P());
			item.SetAttribute(ItemAttributes.attr_lockeddown, true);
			Console.WriteLine("HOUSE_LOCKDOWN_SETUP: uid=" + item.Uid);
		}

		/// <summary>A secure container (with one plain item already inside it) at the house's
		/// anchor point - exercises both attr_secure's pickup restriction (the container
		/// itself, via On_DenyPickup) and its access restriction (the item inside it, via
		/// On_DenyPickupItemFrom).</summary>
		[SteamFunction]
		public static void TestSetupSecureContainer(Character self) {
			House house = FindNearbyHouse(self);
			ItemDef containerDef = (ItemDef) ThingDef.GetByDefname("i_backpack");
			Item container = (Item) containerDef.Create(house.P());
			container.SetAttribute(ItemAttributes.attr_secure, true);
			ItemDef plainDef = (ItemDef) ThingDef.GetByDefname("i_rock_plain");
			Item inner = (Item) plainDef.Create(container);
			Console.WriteLine("HOUSE_LOCKDOWN_SETUP: containerUid=" + container.Uid + " innerUid=" + inner.Uid);
		}

		/// <summary>Reports the uid of whatever currently contains the given item - "none" if
		/// it's sitting loose on the ground. A denied pickup/access attempt leaves this
		/// unchanged from its original value; a successful one changes it (moves into the
		/// picker's own dragging layer/backpack).</summary>
		[SteamFunction]
		public static void TestReportItemContainer(Character self, ScriptArgs sa) {
			uint uid = Convert.ToUInt32(sa.Argv[0], CultureInfo.InvariantCulture);
			Item item = (Item) Thing.UidGetItem(uid);
			Thing cont = item.Cont;
			Console.WriteLine("HOUSE_LOCKDOWN_RESULT: uid=" + uid + " contUid=" + (cont != null ? cont.Uid.ToString(CultureInfo.InvariantCulture) : "none"));
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
