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

	/// <summary>
	/// Regression coverage for #282 - Targ_Lockpick.On_TargonItem's locked-check was inverted,
	/// so lockpicking only ever "worked" on items that were already unlocked.
	/// </summary>
	public static class LockpickingTests {

		/// <summary>Grants a real i_lockpick into self's backpack, and creates a locked and an
		/// unlocked throwaway container nearby (the real p_lockedItem plugin def already exists
		/// in lockpicking.scp content, just never had any real content actually using it).</summary>
		[SteamFunction]
		public static void TestSetupLockpickScenario(Character self) {
			VerificationTestUtils.CleanupNearbyNpcs(self, 10);

			// On the ground, not in self.Backpack - a freshly-created item in a container isn't
			// "pickupable" by a connection that never explicitly opened that container first
			// (OpenedContainers.HasContainerOpenFromAt), and t_lockpick's own On_DClick checks
			// CanPickup before ever calling SelectSkill. Ground items have no such requirement.
			ItemDef lockpickDef = (ItemDef) ThingDef.GetByDefname("i_lockpick");
			Item lockpick = (Item) lockpickDef.Create(self.P());

			ItemDef containerDef = (ItemDef) ThingDef.GetByDefname("i_backpack");
			PluginDef lockPluginDef = PluginDef.GetByDefname("p_lockedItem");

			Item lockedContainer = (Item) containerDef.Create(self.P());
			ItemLockPlugin lockedPlugin = (ItemLockPlugin) lockedContainer.AddNewPlugin(ItemLockPlugin.pkItemLock, lockPluginDef);
			lockedPlugin.isLocked = true;

			Item unlockedContainer = (Item) containerDef.Create(self.P());
			ItemLockPlugin unlockedPlugin = (ItemLockPlugin) unlockedContainer.AddNewPlugin(ItemLockPlugin.pkItemLock, lockPluginDef);
			unlockedPlugin.isLocked = false;

			Console.WriteLine("LOCKPICK_SCENARIO: lockpickUid=" + lockpick.Uid +
				" lockedUid=" + lockedContainer.Uid + " unlockedUid=" + unlockedContainer.Uid);
		}
	}
}
