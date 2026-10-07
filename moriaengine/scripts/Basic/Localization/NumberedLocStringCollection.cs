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

using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace SteamEngine.Common {

	public class NumberedLocStringCollection : LocStringCollection {
		public NumberedLocStringCollection(string defname, string assemblyName, Language language, ICollection<string> strings)
			: base(defname, assemblyName, language, GetEntriesFromList(strings)) {

			var list = strings.ToList();
			for (int i = 0, n = list.Count; i < n; i++) {
				list[i] = this.GetEntry(i.ToString());
			}
			this.Entries = list;
		}

		private static IEnumerable<KeyValuePair<string, string>> GetEntriesFromList(IEnumerable<string> strings) {
			return strings.Select((s, i) => new KeyValuePair<string, string>(i.ToString(CultureInfo.InvariantCulture), s));
		}

		public IReadOnlyCollection<string> Entries { get; }
	}
}
