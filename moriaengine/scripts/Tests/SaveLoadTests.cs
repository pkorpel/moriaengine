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

using System.Collections;

namespace SteamEngine.CompiledScripts {
	public static class SaveLoadTests {

		[SteamFunction]
		public static void _TestArrSave(TagHolder globals) {
			ArrayList list = new ArrayList();
			foreach (Thing t in Thing.AllThings) {
				list.Add(t as Character);
			}
			Thing[] arr = (Thing[]) list.ToArray(typeof(Thing));

			globals.SetTag(TagKey.Acquire("_testArrayList"), list);
			globals.SetTag(TagKey.Acquire("_testArray"), arr);

		}
	}
}