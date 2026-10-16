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
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {

	/// <summary>
	/// Server-side helpers for ".info"/D_Info regression scenarios driven by real packets from
	/// Tests/Protocol/InfoDialogTests.cs.
	/// </summary>
	public static class InfoDialogTests {
		/// <summary>Kills and immediately resurrects self (same pattern as AttrFlagTests'/
		/// NotorietyTests' own self-death helpers) to produce a real Corpse at self's own
		/// position - the real-user-reported ".info on a corpse" crash needs an actual Corpse
		/// instance, not a throwaway item.
		///
		/// Real bug found writing this: the parameterless Character.Resurrect() auto-detects
		/// and loots+DELETES any corpse within 1 tile owned by the resurrecting character
		/// (Corpse.ReturnStuffToChar's own this.Delete() call) - exactly where this corpse sits.
		/// Calling the explicit Resurrect(Corpse) overload with null instead skips that
		/// auto-detection entirely, so the corpse survives for the test to actually open .info
		/// on.</summary>
		[SteamFunction]
		public static void TestSetupCorpseForInfo(Character self) {
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			Player victim = (Player) self;
			int x = victim.X;
			int y = victim.Y;

			NPCDef killerDef = (NPCDef) ThingDef.GetByDefname("c_a_jackrabbit");
			NPC killer = (NPC) killerDef.Create(victim.P());

			victim.CauseDeath(killer);

			Corpse foundCorpse = null;
			foreach (AbstractItem item in victim.GetMap().GetItemsInRange(x, y, 5)) {
				Corpse corpse = item as Corpse;
				if ((corpse != null) && (corpse.Owner == victim)) {
					foundCorpse = corpse;
					break;
				}
			}

			victim.Resurrect(null);

			if (foundCorpse != null) {
				Console.WriteLine("INFO_CORPSE_SETUP: uid=" + foundCorpse.Uid);
			} else {
				Console.WriteLine("INFO_CORPSE_SETUP: none");
			}
		}
	}
}
