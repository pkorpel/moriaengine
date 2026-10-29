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
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace SteamEngine.CompiledScripts {

	/// <summary>#813 - server-side helpers for the region-resource tests (Mining/Lumberjacking
	/// vein tests sample the archive's region types through these).</summary>
	public static class RegionResourceTests {

		/// <summary>Samples `attempts` weighted picks from one region type ("r_doly 4000"),
		/// reporting how many were nothing and how many each resource got.</summary>
		[SteamFunction]
		public static void TestSampleRegionTypeRoll(Character self, ScriptArgs sa) {
			//the raw text: a plain account's Argv is the whole line as one literal string
			string[] args = sa.Args.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
			string typeName = args[0].Trim('"');
			int attempts = Convert.ToInt32(args[1], CultureInfo.InvariantCulture);
			RegionTypeDef type = RegionTypeDef.GetByDefname(typeName);

			int nothing = 0;
			SortedDictionary<string, int> counts = new SortedDictionary<string, int>(StringComparer.OrdinalIgnoreCase);
			for (int i = 0; i < attempts; i++) {
				RegionResourceDef r = type.PickRandom();
				if ((r == null) || r.IsNothing) {
					nothing++;
				} else {
					int count;
					counts.TryGetValue(r.Defname, out count);
					counts[r.Defname] = count + 1;
				}
			}
			StringBuilder sb = new StringBuilder("REGION_TYPE_SAMPLE: " + typeName + " attempts=" + attempts + " nothing=" + nothing);
			foreach (KeyValuePair<string, int> pair in counts) {
				sb.Append(" " + pair.Key + "=" + pair.Value);
			}
			Console.WriteLine(sb.ToString());
		}
	}
}
