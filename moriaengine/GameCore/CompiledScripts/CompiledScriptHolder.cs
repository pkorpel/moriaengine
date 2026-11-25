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

namespace SteamEngine.CompiledScripts {

	/// <summary>
	/// CompiledScriptHolders generator emits types that derive from this class,
	/// based on methods decorated by <see cref="SteamFunctionAttribute"/> in the source code.
	/// </summary>
	public abstract class CompiledScriptHolder : ScriptHolder {
		private string desc;

		public static void Bootstrap() {
			ClassManager.RegisterSupplySubclassInstances<CompiledScriptHolder>(null, false, false);
		}

		//protected CompiledScriptHolder()
		//    : base() {

		//}

		protected CompiledScriptHolder(string name, string description)
			: base(name) {

			this.desc = description;
		}

		/// <summary>Description provided in any SteamDocAttribute on the SteamFunction</summary>
		public override string Description {
			get {
				return this.desc;
				//return Tools.TypeToString(this.GetType());
			}
		}
	}
}