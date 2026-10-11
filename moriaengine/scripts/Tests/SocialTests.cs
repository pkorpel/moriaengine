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
	/// V-4 (verification pass, #72): server-side helpers for social-system scenarios needing two
	/// REAL, simultaneously-connected characters (guild invite/accept, secure trade). Real finding
	/// while building this: steamengine.ini's autoAccountCreation defaults to false, and
	/// AbstractAccount.HandleLoginAttempt only ever auto-creates the very first account on a fresh
	/// server regardless - so a second real account for a protocol test had no path to exist at
	/// all until Globals.AutoAccountCreation got a setter (mirrors Globals.PaymentSource's own
	/// settable-at-runtime pattern) and this function to flip it on mid-fixture.
	/// </summary>
	public static class SocialTests {
		[SteamFunction]
		public static void TestEnableAutoAccountCreation(Character self) {
			Globals.AutoAccountCreation = true;
			Console.WriteLine("AUTO_ACCOUNT_TEST: ENABLED");
		}

		/// <summary>Logs this character's own uid so a DIFFERENT, simultaneously-connected
		/// character's test can target it (e.g. a guild invite) without the two connections
		/// needing any other way to learn about each other.</summary>
		[SteamFunction]
		public static void TestReportMyUid(Character self) {
			Console.WriteLine("MY_UID: " + self.Uid + " name=" + self.Name);
		}

		[SteamFunction]
		public static void TestCheckGuild(Character self) {
			Guild guild = Guild.GetGuild(self);
			Console.WriteLine("GUILD_CHECK: " + self.Name + " guild=" + (guild != null ? guild.Name : "none"));
		}

		/// <summary>Creates a known, named item directly in self's backpack for a secure-trade
		/// scenario to pick up and drop on the other participant.</summary>
		[SteamFunction]
		public static void TestGrantTradeItem(Character self) {
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			ThingDef itemDef = ThingDef.GetByDefname("i_bandage");
			AbstractItem item = (AbstractItem) itemDef.Create(self.Backpack);
			Console.WriteLine("TRADE_SETUP: item=#" + item.Uid + " backpack=#" + self.Backpack.Uid);
		}

		/// <summary>Reports whether the exact item (by uid) is now in self's backpack - used after
		/// a secure trade completes to confirm that SPECIFIC item actually moved to/away from
		/// each side, not just that the trade "finished" with no error. Checked by uid rather
		/// than defname so a character's own starting-kit items (if any) can never be confused
		/// with the one item this scenario is tracking.</summary>
		[SteamFunction]
		public static void TestCheckHasItem(Character self, ScriptArgs sa) {
			int itemUid = Convert.ToInt32(sa.Argv[0], System.Globalization.CultureInfo.InvariantCulture);
			AbstractItem item = (AbstractItem) Thing.UidGetItem(itemUid);
			bool has = (item != null) && (item.Cont == self.Backpack);
			Console.WriteLine("HAS_ITEM: uid=" + itemUid + " has=" + has);
		}

		/// <summary>Reports whether self is in a party, and if so, the leader's name and member
		/// count - verified against real UO party semantics (Party.GetParty/IsLeader/Members).</summary>
		[SteamFunction]
		public static void TestCheckParty(Character self) {
			Party party = Party.GetParty(self);
			if (party == null) {
				Console.WriteLine("PARTY_CHECK: " + self.Name + " inParty=False");
			} else {
				Console.WriteLine("PARTY_CHECK: " + self.Name + " inParty=True leader=" + party.Leader.Name +
					" members=" + party.Members.Count);
			}
		}
	}
}
