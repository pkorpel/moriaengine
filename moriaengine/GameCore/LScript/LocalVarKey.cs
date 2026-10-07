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


namespace SteamEngine {
	/// <summary>
	/// TriggerKeys are used when calling triggers. You should call Get(name) once to get a LocalVarKey, and then use
	/// that from then on for calling that trigger.
	/// This and FunctionKey are very similar, and serve similar purposes.
	/// </summary>
	public sealed class LocalVarKey : AbstractKey<LocalVarKey> {
		private LocalVarKey(string name, int uid)
			: base(name, uid) {
		}

		public static LocalVarKey Acquire(string name) {
			return Acquire(name, (n, u) => new LocalVarKey(n, u));
		}
	}
}