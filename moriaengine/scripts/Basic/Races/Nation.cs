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

namespace SteamEngine.CompiledScripts {
	/// <summary>The 12 nations (#161/#162), ported from the original Sphere source's
	/// nation_homes/nation_realm constants (newbie_portals.scp). Each nation implies exactly
	/// one Realm and one home coordinate - see NationData.</summary>
	public enum Nation {
		None = 0,
		Gondoran,
		Rohir,
		Severan,
		Mordoran,
		Haradwaith,
		Cerny,
		NurnMan,
		SedyElf,
		LesniElf,
		DivokySkret,
		Ereboran,
		Pulcik
	}

	/// <summary>Per-nation data: Realm (drives notoriety, #163) and home coordinate (#164).
	/// Coordinates are taken verbatim from the original Sphere source's home_&lt;nation&gt;
	/// constants - nations whose source entry didn't include a Z use 0.</summary>
	public static class NationData {
		private class Entry {
			public readonly Realm realm;
			public readonly Point4D home;
			public readonly string displayName;

			public Entry(Realm realm, Point4D home, string displayName) {
				this.realm = realm;
				this.home = home;
				this.displayName = displayName;
			}
		}

		private static readonly Dictionary<Nation, Entry> data = new Dictionary<Nation, Entry> {
			{ Nation.Gondoran, new Entry(Realm.Gondor, new Point4D(5678, 3871, -80), "Gondor") }, // Minas Tirith
			{ Nation.Rohir, new Entry(Realm.Gondor, new Point4D(2344, 2542, 0), "Rohan") }, // Edoras
			{ Nation.Severan, new Entry(Realm.Gondor, new Point4D(1261, 644, 0), "the North") },
			{ Nation.Mordoran, new Entry(Realm.Mordor, new Point4D(3610, 2718, -50), "Mordor") }, // Barad-dur
			{ Nation.Haradwaith, new Entry(Realm.Mordor, new Point4D(4186, 3960, 0), "Harad") },
			{ Nation.Cerny, new Entry(Realm.Mordor, new Point4D(2456, 4018, 11), "the Black Numenoreans") }, // Umbar
			{ Nation.NurnMan, new Entry(Realm.Mordor, new Point4D(3477, 3282, 13), "Nurn") },
			{ Nation.SedyElf, new Entry(Realm.Gondor, new Point4D(741, 1360, 0), "the Grey Havens") },
			{ Nation.LesniElf, new Entry(Realm.Gondor, new Point4D(3000, 1250, 45), "the Woodland Realm") }, // Mirkwood
			{ Nation.DivokySkret, new Entry(Realm.Mordor, new Point4D(3824, 2134, 0), "the wild Orcs") }, // Rhun
			{ Nation.Ereboran, new Entry(Realm.Gondor, new Point4D(3157, 965, 40), "Erebor") }, // Lonely Mountain
			{ Nation.Pulcik, new Entry(Realm.Gondor, new Point4D(1082, 1335, 0), "the Shire") }, // Hobbiton
		};

		public static Realm GetRealm(Nation nation) {
			return data[nation].realm;
		}

		public static Point4D GetHome(Nation nation) {
			return data[nation].home;
		}

		public static string GetDisplayName(Nation nation) {
			return data[nation].displayName;
		}
	}
}
