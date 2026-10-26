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
	/// Server-side helpers for the real-user-reported "dropping one gold pile onto another
	/// doesn't stack" bug, driven by real packets from Tests/Protocol/ItemStackingTests.cs.
	/// ItemDef.Create(container) does NOT auto-stack (confirmed via GoldUtils.Grant's own
	/// explicit follow-up TryStackToAnyInside call) - so two Create() calls into the same
	/// backpack reliably stay as two separate piles for this setup.
	/// </summary>
	public static class ItemStackingTests {
		[SteamFunction]
		public static void TestSetupTwoGoldPiles(Character self) {
			ItemDef goldDef = (ItemDef) ThingDef.GetByDefname("i_gold");
			Container backpack = self.Backpack;
			AbstractItem pileA = (AbstractItem) goldDef.Create(backpack);
			pileA.Amount = 50;
			AbstractItem pileB = (AbstractItem) goldDef.Create(backpack);
			pileB.Amount = 30;
			Console.WriteLine("GOLD_STACK_SETUP: pileA=" + pileA.Uid + " pileAAmount=" + pileA.Amount +
				" pileB=" + pileB.Uid + " pileBAmount=" + pileB.Amount +
				" backpack=" + backpack.Uid);
		}

		/// <summary>Confirms the tiledata-fallback fix generalizes beyond gold to the other
		/// real stackable categories (bandages, arrows, bolts, scrolls - potions excluded here,
		/// i_potion_heal itself is missing from tracked content entirely, a pre-existing,
		/// unrelated content gap, not a stacking bug).</summary>
		[SteamFunction]
		public static void TestReportStackableFlags(Character self) {
			string[] defnames = { "i_gold", "i_bandage", "i_arrow", "i_bolt_jagged", "i_xbolt", "i_scroll_blank", "i_sword_broad_iron" };
			foreach (string defname in defnames) {
				ItemDef def = (ItemDef) ThingDef.GetByDefname(defname);
				if (def == null) {
					Console.WriteLine("STACKABLE_FLAG: " + defname + "=<missing>");
					continue;
				}
				Console.WriteLine("STACKABLE_FLAG: " + defname + "=" + def.IsStackable);
			}
		}

		[SteamFunction]
		public static void TestReportGoldPile(Character self, ScriptArgs sa) {
			uint uid = Convert.ToUInt32(sa.Argv[0], CultureInfo.InvariantCulture);
			AbstractItem item = Thing.UidGetItem(uid);
			if (item == null) {
				Console.WriteLine("GOLD_STACK_RESULT: uid=" + uid + " exists=False");
				return;
			}
			Console.WriteLine("GOLD_STACK_RESULT: uid=" + uid + " exists=True amount=" + item.Amount);
		}
	}
}
