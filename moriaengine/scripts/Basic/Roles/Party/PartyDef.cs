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

using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {
	[ViewableClass]
	public class PartyDef : RoleDef {
		public static readonly PartyDef instance = (PartyDef)
			new PartyDef("r_party", "C# scripts", -1).Register();

		public static readonly RoleKey rkParty = RoleKey.Acquire("_party_");


		public PartyDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
		}

		protected override Role CreateImpl(RoleKey key) {
			return new Party(this, key);
		}

		public static Party NewParty(Character leader) {
			Party p = (Party) instance.Create(rkParty);
			RolesManagement.Assign(leader, p);
			return p;
		}
	}
}
