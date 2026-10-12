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
	/// Server-side helpers for the UseHand/UseRing protocol tests (#102, Part A1). i_torch
	/// (ShiningItem.def) is used as the equipped item because its On_DClick has a real,
	/// observable side effect (Model change to 0xa12, see ShiningItem.cs) even while already
	/// equipped - unlike the generic Equippable.On_DClick, which is a no-op in that case, so a
	/// plain weapon/wearable couldn't prove UseHand actually reached Trigger_DClick.
	/// </summary>
	public static class BasicCommandsTests {
		[SteamFunction]
		public static void TestGrantAndEquipTorch(Character self) {
			Equippable torch = self.NewEquip(ThingDef.GetByDefname("i_torch"));
			Console.WriteLine("TORCH_SETUP: uid=" + torch.Uid + " model=" + torch.Model);
		}

		[SteamFunction]
		public static void TestCheckItemModel(Character self, ScriptArgs sa) {
			int uid = Convert.ToInt32(sa.Argv[0], CultureInfo.InvariantCulture);
			AbstractItem item = (AbstractItem) Thing.UidGetItem(uid);
			bool equippedOnSelf = (item != null) && (item.Cont == self);
			Console.WriteLine("ITEM_CHECK: uid=" + uid + " model=" + (item != null ? item.Model : -1) +
				" equippedOnSelf=" + equippedOnSelf);
		}
	}
}
