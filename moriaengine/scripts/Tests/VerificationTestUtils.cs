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

using System.Collections.Generic;

namespace SteamEngine.CompiledScripts {

	/// <summary>
	/// Shared helper for the full verification pass's (#67) server-side test scenarios. Real
	/// finding while chasing a CI flake in V-1 (#69): several scenarios there (and V-2's pet
	/// commands) spawn NPCs with AggroMode.Hostile (c_titan/c_liche/c_dragon_lord/
	/// c_skeleton_archer_nb - see V-1's own PR for why that's now seeded at all), and a
	/// GameServerPairFixture/xunit collection runs several test methods back-to-back against the
	/// SAME long-lived server process. A hostile NPC an earlier test spawned never gets cleaned
	/// up and keeps wandering/aggroing autonomously - CI genuinely caught a leftover titan
	/// killing an unrelated later test's brand-new character outright (started at 100 hits,
	/// ended at 0) well before that test's own, unrelated scenario even got a chance to run.
	/// </summary>
	public static class VerificationTestUtils {
		/// <summary>Deletes every non-player Character within `range` tiles of `self` - called at
		/// the start of each scenario's setup so it always starts from a clean slate regardless
		/// of what an earlier test in the same fixture spawned and left behind.</summary>
		public static void CleanupNearbyNpcs(Character self, int range) {
			List<NPC> toDelete = new List<NPC>();
			foreach (AbstractCharacter ch in self.GetMap().GetCharsInRange(self.X, self.Y, range)) {
				NPC npc = ch as NPC;
				if (npc != null) {
					toDelete.Add(npc);
				}
			}
			foreach (NPC npc in toDelete) {
				if (!npc.IsDeleted) {
					npc.Delete();
				}
			}
		}

		/// <summary>Deletes every Corpse within `range` tiles of `self` - a real player/NPC death
		/// (#128/#127) leaves one of these behind, unlike plain NPC deletion. Needed once a
		/// scenario that spawns many real deaths (Phase 3's two-account death tests) shares a
		/// fixture with a later scenario connecting fresh near the same spawn point - an
		/// accumulating pile of nearby corpses otherwise means a new connection's normal
		/// nearby-object sync burst (DrawObject/health/ObjectInfo/CorpseClothing/ItemsInContainer
		/// per corpse) can grow large enough to push a later, unrelated packet past a test's
		/// timeout.</summary>
		public static void CleanupNearbyCorpses(Character self, int range) {
			List<Corpse> toDelete = new List<Corpse>();
			foreach (Thing thing in self.GetMap().GetThingsInRange(self.X, self.Y, range)) {
				Corpse corpse = thing as Corpse;
				if (corpse != null) {
					toDelete.Add(corpse);
				}
			}
			foreach (Corpse corpse in toDelete) {
				if (!corpse.IsDeleted) {
					corpse.Delete();
				}
			}
		}

		/// <summary>Deletes every real dynamic Item of `itemDef`'s type within `range` tiles of
		/// `self` - same leftover-content problem as CleanupNearbyNpcs/CleanupNearbyCorpses
		/// above, just for ITEMS. Real finding (#692): LineOfSightBlockingTests/
		/// VerticalLosBlockingTests place real i_wall_stone Items to test blocking, and - unlike
		/// NPCs/Corpses - nothing ever cleaned those up. Three such tests sharing one
		/// long-lived GameServerPairFixture meant a LATER test's own fresh "open ground" pick
		/// could land close enough to an EARLIER test's leftover wall to report LOS already
		/// blocked before that later test ever placed its own wall - a false failure with
		/// nothing wrong in the actual LOS code being tested.</summary>
		public static void CleanupNearbyItems(Character self, ItemDef itemDef, int range) {
			List<Item> toDelete = new List<Item>();
			foreach (AbstractItem item in self.GetMap().GetItemsInRange(self.X, self.Y, range)) {
				if (item.Def == itemDef) {
					Item asItem = item as Item;
					if (asItem != null) {
						toDelete.Add(asItem);
					}
				}
			}
			foreach (Item item in toDelete) {
				if (!item.IsDeleted) {
					item.Delete();
				}
			}
		}
	}
}
