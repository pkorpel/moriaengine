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
	/// Server-side helpers for professions protocol tests (#134-138). TestSetProfession already
	/// exists in scripts/Tests/LevelingTests.cs (Phase 4, #129) - reused here rather than
	/// duplicated.
	/// </summary>
	public static class ProfessionTests {
		/// <summary>Creates an item of the given defname on the ground at self's own feet (NOT
		/// in self.Backpack) and reports its uid, so a protocol test can pick it up and attempt
		/// to equip it (see #138's weapon/armor restriction tests). Real finding while building
		/// this: AbstractCharacter.CanPickup ultimately calls CanReach, which for an item inside
		/// a CONTAINER requires that container to be open to this connection
		/// (OpenedContainers.HasContainerOpenFromAt, GameCore/CanSeeImplementation.cs) - a
		/// freshly-granted backpack item is NOT reachable without first opening the backpack,
		/// so pickup silently fails (RejectMoveItemRequestOutPacket/0x27) with no deny message
		/// of any kind, long before the equip-restriction code this test actually wants to
		/// exercise is ever reached. A ground item has no container to open, sidestepping this
		/// entirely. Also noted while finding this: converted weapon content's WeaponType field
		/// is almost entirely a blanket Converter guess (WeaponType.OneHandSpike on nearly every
		/// weapon regardless of real type, scripts/converted/zbrane/*.def, #157) - not fixed
		/// here. Armor's WearableType is reliably section-driven by contrast, safe to test
		/// against directly.</summary>
		[SteamFunction]
		public static void TestGrantItem(Character self, ScriptArgs sa) {
			string defname = Convert.ToString(sa.Argv[0]);
			ThingDef itemDef = ThingDef.GetByDefname(defname);
			if (itemDef == null) {
				Console.WriteLine("PROFESSION_ITEM_GRANTED: ERROR unknown defname '" + defname + "'");
				return;
			}
			AbstractItem item = (AbstractItem) itemDef.Create(self.P());
			Console.WriteLine("PROFESSION_ITEM_GRANTED: uid=" + item.Uid);
		}

		/// <summary>Reports whether the given item uid is currently equipped (AbstractItem.
		/// IsEquipped) - the clean signal that a DropToWearItem attempt actually succeeded,
		/// independent of whether a deny message also arrived (a denied equip leaves the item
		/// sitting in the backpack, not equipped).</summary>
		[SteamFunction]
		public static void TestCheckItemEquipped(Character self, ScriptArgs sa) {
			int itemUid = Convert.ToInt32(sa.Argv[0], System.Globalization.CultureInfo.InvariantCulture);
			AbstractItem item = (AbstractItem) Thing.UidGetItem(itemUid);
			Console.WriteLine("PROFESSION_ITEM_EQUIPPED: uid=" + itemUid + " equipped=" + item.IsEquipped);
		}

		/// <summary>Reports self's real (not modified) value of the given skill - used to
		/// confirm ProfessionPlugin.On_Assign's starting-skill grant (#135) actually landed.</summary>
		[SteamFunction]
		public static void TestCheckSkillValue(Character self, ScriptArgs sa) {
			SkillName skill = (SkillName) Enum.Parse(typeof(SkillName), Convert.ToString(sa.Argv[0]), true);
			Console.WriteLine("PROFESSION_SKILL_VALUE: skill=" + skill + " value=" + self.GetRealSkillValue(skill));
		}

		/// <summary>Direct pure-logic check of ProfessionDef.LevelToTitleTier (#177) - same
		/// direct-call spirit as LevelStatBonus's own test pattern, no live Def/character state
		/// needed at all.</summary>
		[SteamFunction]
		public static void TestLevelToTitleTier(Character self, ScriptArgs sa) {
			int level = Convert.ToInt32(sa.Argv[0], System.Globalization.CultureInfo.InvariantCulture);
			int ladderLength = Convert.ToInt32(sa.Argv[1], System.Globalization.CultureInfo.InvariantCulture);
			Console.WriteLine("TITLE_TIER: " + ProfessionDef.LevelToTitleTier(level, ladderLength));
		}

		/// <summary>Reports self's PaperdollName (#177 - "The Good/Evil Name, Title" format) for
		/// the given Realm, after temporarily setting it.</summary>
		[SteamFunction]
		public static void TestCheckPaperdollNameForRealm(Character self, ScriptArgs sa) {
			Realm realm = (Realm) Enum.Parse(typeof(Realm), Convert.ToString(sa.Argv[0]), true);
			self.Realm = realm;
			Console.WriteLine("PAPERDOLL_NAME: " + self.PaperdollName);
		}
	}
}
