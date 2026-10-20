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
using System.Collections.Generic;

namespace SteamEngine.CompiledScripts {

	/// <summary>
	/// V-3 (verification pass, #71): server-side setup/inspection helpers for banking and NPC
	/// vendor scenarios driven by real speech/gump packets from Tests/Protocol/EconomyTests.cs.
	/// Banking/vendor speech (BankerSpeech.cs/ShopkeeperSpeech.cs) is heard-by-a-nearby-NPC
	/// speech, not a "." command - every scenario here spawns the real banker/vendor NPC the
	/// speech trigger needs to actually fire, same as a player walking up to one in-game.
	///
	/// Every "TestSetup*" function resets gold to zero before granting whatever the scenario
	/// needs - real finding while building this: GameState.LoginCharacter resumes a "lingering"
	/// character (IsLingering: GameState==null but Flag_Disconnected never got set, which an
	/// abrupt disconnect - exactly what a test's TCP socket Dispose() does - never sets) instead
	/// of creating/selecting a new one whenever one exists on the account. Each test's
	/// CreateCharacterInPacket can therefore end up controlling an EARLIER test's leftover
	/// character and its leftover gold rather than a blank-slate one, so starting balances can't
	/// be assumed - they're reset explicitly instead.
	/// </summary>
	public static class EconomyTests {
		private static void ResetGold(Character self) {
			DeleteAllGoldIn(self.Backpack);
			DeleteAllGoldIn(self.Bank);
		}

		private static void DeleteAllGoldIn(AbstractItem container) {
			ThingDef goldDef = ThingDef.GetByDefname("i_gold");
			List<AbstractItem> items = new List<AbstractItem>(container);
			foreach (AbstractItem item in items) {
				if (item.Def == goldDef) {
					item.Delete();
				}
			}
		}

		[SteamFunction]
		public static void TestSetupBankerAndGold(Character self) {
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			ResetGold(self);
			NPCDef bankerDef = ThingDef.GetByDefname("C_H_BANKER") as NPCDef;
			bankerDef.Create(self.X + 1, self.Y, self.Z, self.M);
			GoldUtils.Grant(self, 500);
			Console.WriteLine("ECONOMY_SETUP: STARTED (banker placed, 500 gold granted to backpack)");
		}

		[SteamFunction]
		public static void TestSetupBankerAndBankGold(Character self) {
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			ResetGold(self);
			NPCDef bankerDef = ThingDef.GetByDefname("C_H_BANKER") as NPCDef;
			bankerDef.Create(self.X + 1, self.Y, self.Z, self.M);
			GoldUtils.GrantToBank(self, 500);
			Console.WriteLine("ECONOMY_SETUP: STARTED (banker placed, 500 gold granted to bank)");
		}

		[SteamFunction]
		public static void TestCheckGold(Character self) {
			Console.WriteLine("GOLD_CHECK: backpack=" + GoldUtils.CountGoldIn(self.Backpack) + " bank=" + GoldUtils.CountGoldIn(self.Bank));
		}

		[SteamFunction]
		public static void TestSetupVendorAndSellableItem(Character self) {
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			ResetGold(self);
			NPCDef vendorDef = ThingDef.GetByDefname("C_m_WEAPONSMITH") as NPCDef;
			vendorDef.Create(self.X + 1, self.Y, self.Z, self.M);
			ThingDef bandageDef = ThingDef.GetByDefname("i_bandage");
			AbstractItem bandage = (AbstractItem) bandageDef.Create(self.Backpack);
			// backpackUid included because targeting an item inside a container that was never
			// explicitly "opened" (CanSeeImplementation.CanReachFromAt -> OpenedContainers.
			// HasContainerOpenFromAt) is denied - a real client auto-opens its own backpack on
			// login, which this minimal test client doesn't do, so the protocol test needs to
			// double-click it first itself.
			Console.WriteLine("ECONOMY_SETUP: STARTED (vendor placed, sellable item #" + bandage.Uid +
				", backpack #" + self.Backpack.Uid + ")");
		}

		[SteamFunction]
		public static void TestSetupVendorOnly(Character self) {
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			ResetGold(self);
			NPCDef vendorDef = ThingDef.GetByDefname("C_m_WEAPONSMITH") as NPCDef;
			vendorDef.Create(self.X + 1, self.Y, self.Z, self.M);
			Console.WriteLine("ECONOMY_SETUP: STARTED (vendor placed)");
		}

		[SteamFunction]
		public static void TestGrantBackpackAndBankGold(Character self) {
			ResetGold(self);
			GoldUtils.Grant(self, 500);
			GoldUtils.GrantToBank(self, 500);
			Console.WriteLine("ECONOMY_SETUP: STARTED (500 backpack + 500 bank gold granted)");
		}

		/// <summary>Logs the player's own bankbox uid so the protocol test can double-click it
		/// directly (BankBox.On_DClick's real deny-without-a-banker gate, #36).</summary>
		[SteamFunction]
		public static void TestGetBankUid(Character self) {
			Console.WriteLine("BANK_UID: " + self.Bank.Uid);
		}

		/// <summary>#509 - a banker within BankerRange.MaxDistance (6), for the "double-click
		/// opens the dialog" and "say 'bank' opens the dialog" happy-path tests. Reports the
		/// banker's own uid so the protocol test can double-click it directly.</summary>
		[SteamFunction]
		public static void TestSetupNearbyBankerForDialogCheck(Character self) {
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			NPCDef bankerDef = ThingDef.GetByDefname("C_H_BANKER") as NPCDef;
			NPC banker = (NPC) bankerDef.Create(self.X + 1, self.Y, self.Z, self.M);
			Console.WriteLine("ECONOMY_SETUP: STARTED (nearby banker for dialog check, uid=" + banker.Uid + ")");
		}

		/// <summary>#509 - a banker at distance 8: inside Globals.SpeechDistance's own default
		/// (10, so ordinary banker speech like "balance" still reaches it) but outside
		/// BankerRange.MaxDistance (6) - the "too far for the dialog specifically" case for both
		/// the speech keyword and the double-click path.</summary>
		[SteamFunction]
		public static void TestSetupFarBankerForDialogCheck(Character self) {
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			// This test map's default spawn point has dense static placement nearby (same
			// cramped-antechamber terrain documented elsewhere in CLAUDE.md, e.g. V-7/V-9/V-10's
			// own movement lessons and MagerySpellTests' ground-targeted casts) - a banker placed
			// a naive 8 tiles away can end up with no LOS back to self, so Trigger_Hear never
			// fires and this scenario silently tests nothing. Move to confirmed open ground first.
			MovementSpeedTests.TestTeleportToOpenGround(self);
			NPCDef bankerDef = ThingDef.GetByDefname("C_H_BANKER") as NPCDef;
			NPC banker = (NPC) bankerDef.Create(self.X + 8, self.Y, self.Z, self.M);
			Console.WriteLine("ECONOMY_SETUP: STARTED (far banker for dialog check, uid=" + banker.Uid +
				", distance=" + Point2D.GetSimpleDistance(self, banker) +
				", los=" + banker.CanSeeLOS(self).Allow + ")");
		}

		/// <summary>#509 - queries the real OpenedContainers state (not just "did OpenTo throw")
		/// to confirm the player's bank box is actually open to them.</summary>
		[SteamFunction]
		public static void TestReportBankOpenToMe(Character self) {
			Console.WriteLine("BANK_OPEN_TEST: open=" + OpenedContainers.HasContainerOpen(self, self.Bank).Allow);
		}
	}
}
