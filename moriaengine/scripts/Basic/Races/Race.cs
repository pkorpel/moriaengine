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
	/// <summary>The 6 playable races (#161/#162) - ported from the original Sphere source
	/// (newbie_portals.scp's def_race[] constants + canbeclass's race-name branches), not just
	/// the wiki's thinner 5-race overview (which conflates Orc/UrukHai).</summary>
	public enum Race {
		None = 0,
		Human,
		Elf,
		Dwarf,
		Hobbit,
		Orc,
		UrukHai
	}

	/// <summary>Per-race data: which nations a race may choose from (or is locked to), and
	/// display names. See RaceSelection.scp for the actual selection flow and NationData for
	/// what a Nation implies (Realm + home). Deliberately has NO stat-modifier data - race has
	/// zero mechanical stat effect in the confirmed design (#161 decision #2).</summary>
	public static class RaceData {
		private static readonly Dictionary<Race, Nation[]> availableNations = new Dictionary<Race, Nation[]> {
			{ Race.Human, new[] { Nation.Gondoran, Nation.Rohir, Nation.Severan, Nation.Mordoran, Nation.Haradwaith, Nation.Cerny, Nation.NurnMan } },
			{ Race.Elf, new[] { Nation.SedyElf, Nation.LesniElf } },
			{ Race.Orc, new[] { Nation.Mordoran, Nation.DivokySkret } },
			{ Race.Dwarf, new[] { Nation.Ereboran } },
			{ Race.Hobbit, new[] { Nation.Pulcik } },
			{ Race.UrukHai, new[] { Nation.Mordoran } },
		};

		private static readonly Dictionary<Race, string> displayNames = new Dictionary<Race, string> {
			{ Race.Human, "Human" },
			{ Race.Elf, "Elf" },
			{ Race.Dwarf, "Dwarf" },
			{ Race.Hobbit, "Hobbit" },
			{ Race.Orc, "Orc" },
			{ Race.UrukHai, "Uruk-hai" },
		};

		/// <summary>The nation(s) this race may settle in - a single-element array for the 3
		/// races locked to one nation (Dwarf/Hobbit/UrukHai), multiple for the 3 that get a
		/// real choice (Human/Elf/Orc).</summary>
		public static IReadOnlyList<Nation> GetAvailableNations(Race race) {
			return availableNations[race];
		}

		/// <summary>False for Dwarf/Hobbit/UrukHai - those races skip the nation-picker gump
		/// entirely and go straight to their one fixed nation.</summary>
		public static bool HasNationChoice(Race race) {
			return availableNations[race].Length > 1;
		}

		public static string GetDisplayName(Race race) {
			return displayNames[race];
		}

		public static IEnumerable<Race> AllRaces {
			get { return availableNations.Keys; }
		}
	}
}
