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
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using SteamEngine.Persistence;

namespace SteamEngine {
	public sealed class PluginKey : AbstractKey<PluginKey> {
		private PluginKey(string name, int uid)
			: base(name, uid) {
		}

		public static PluginKey Acquire(string name) {
			return Acquire(name, (n, u) => new PluginKey(n, u));
		}
	}


	public sealed class PluginKeySaveImplementor : ISimpleSaveImplementor {
		private static Regex re = new Regex(@"^\@\@(?<value>.+)\s*$",
			RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

		public Type HandledType {
			get {
				return typeof(PluginKey);
			}
		}

		public Regex LineRecognizer {
			get {
				return re;
			}
		}

		[SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods")]
		public string Save(object objToSave) {
			return "@@" + ((PluginKey) objToSave).Name;
		}

		[SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods")]
		public object Load(Match match) {
			return PluginKey.Acquire(match.Groups["value"].Value);
		}

		public string Prefix {
			get {
				return "@@";
			}
		}
	}
}