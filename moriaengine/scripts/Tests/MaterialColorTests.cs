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

	/// <summary>#822 - server-side helpers for Tests/Protocol/MaterialColorTests.cs.</summary>
	public static class MaterialColorTests {
		/// <summary>Reports the def's hue and the hue of a freshly created item of it.</summary>
		[SteamFunction]
		public static void TestMaterialColor(Character self, ScriptArgs sa) {
			string defname = sa.Args.Trim().Trim('"');
			ItemDef def = (ItemDef) ThingDef.GetByDefname(defname);
			Item item = (Item) def.Create(self.Backpack);
			Console.WriteLine("MATERIAL_COLOR: " + defname + " def=0x" + def.Color.ToString("x", CultureInfo.InvariantCulture) +
				" item=0x" + item.Color.ToString("x", CultureInfo.InvariantCulture));
			item.Delete();
		}
	}
}
