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

	/// <summary>#810 - server-side helpers for Tests/Protocol/SecureTradeTests.cs.</summary>
	public static class SecureTradeTests {
		/// <summary>Two swords in my pack, plus my uid and my pack's uid.</summary>
		[SteamFunction]
		public static void TestTradeSetup(Character self) {
			ItemDef swordDef = (ItemDef) ThingDef.GetByDefname("i_sword_broad_iron");
			Container pack = self.Backpack;
			AbstractItem a = (AbstractItem) swordDef.Create(pack);
			AbstractItem b = (AbstractItem) swordDef.Create(pack);
			Console.WriteLine("TRADE_SETUP2: me=" + self.Uid + " pack=" + pack.Uid + " itemA=" + a.Uid + " itemB=" + b.Uid);
		}

		[SteamFunction]
		public static void TestTradeState(Character self) {
			Console.WriteLine("TRADE_STATE: " + self.Name + " " + SecureTrade.Describe(self));
		}

		/// <summary>Where an item is: its top object, whether it's in its holder's own pack, in
		/// a trade box, or still being dragged.</summary>
		[SteamFunction]
		public static void TestTradeItem(Character self, ScriptArgs sa) {
			uint uid = Convert.ToUInt32(sa.Argv[0], CultureInfo.InvariantCulture);
			AbstractItem item = Thing.UidGetItem(uid);
			if (item == null) {
				Console.WriteLine("TRADE_ITEM: uid=" + uid + " exists=False");
				return;
			}
			Thing top = item.TopObj();
			Character topChar = top as Character;
			AbstractItem cont = item.Cont as AbstractItem;
			bool inTradeBox = (cont != null) && (cont.Def.PrettyDefname == "i_secure_trade_box");
			bool inPack = (topChar != null) && (item.Cont == topChar.Backpack);
			bool dragging = (topChar != null) && topChar.HasPickedUp(item);
			Console.WriteLine("TRADE_ITEM: uid=" + uid + " exists=True holder=" + ((top == null) ? "none" : top.Name) +
				" inPack=" + inPack + " inTradeBox=" + inTradeBox + " dragging=" + dragging);
		}

		/// <summary>Out of reach (5) but well inside the update range.</summary>
		[SteamFunction]
		public static void TestTradeStepAway(Character self) {
			self.P(self.X + 8, self.Y, self.Z);
		}

		[SteamFunction]
		public static void TestTradeKillMe(Character self) {
			self.CauseDeath(null);
		}

		/// <summary>A trade box with a sword in it but no live trade - what a server restart
		/// mid-trade leaves behind.</summary>
		[SteamFunction]
		public static void TestTradeMakeOrphanBox(Character self) {
			ContainerDef boxDef = (ContainerDef) ThingDef.GetByDefname("i_secure_trade_box");
			Container box = (Container) boxDef.Create(self);
			AbstractItem sword = (AbstractItem) ((ItemDef) ThingDef.GetByDefname("i_sword_broad_iron")).Create(box);
			Console.WriteLine("TRADE_ORPHAN: box=" + box.Uid + " item=" + sword.Uid + " boxOnMe=" + (box.Cont == self));
		}
	}
}
