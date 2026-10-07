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
using System.Text.RegularExpressions;
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {
	public delegate bool TryParseResource(string str, double number, bool asPercentage, out IResourceListEntry resource);

	/// <summary>Class for parsing resources strings from LScript</summary>
	public class ResourcesListParser : IFieldValueParser {
		//(^(\s*0x?[0-9a-f]+\s*)$)|(^(\s*\d+(\.\d+)?)$) = pattern for recognizing hex, float and decimal numbers together
		//regular expression for recogninzing the whole reslist (first resource is obligatory, others are voluntary (separated by commas), all resources contain from a number-value pair where number can be hex, float and decimal
		public static readonly Regex re = new Regex(@"^ (?<resource>\s* (?<number>(0x?[0-9a-f]+\s*)|(\d+(\.\d+)?\s*))? (?<value>[a-z_][a-z0-9_]*) ) (\s*,\s* (?<resource> (?<number>(0x?[0-9a-f]+\s*)|(\d+(\.\d+)?\s*))? (?<value>[a-z_][a-z0-9_]*) ) )* $",
				RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);

		#region IFieldValueParser Members

		public Type HandledType {
			get {
				return typeof(ResourcesList);
			}
		}

		public static ResourcesList Parse(string input) {
			ResourcesList retVal;
			Match m = re.Match(input);
			if (InternalProcessMatch(m, out retVal)) {
				return retVal;
			}
			throw new SEException("Invalid resources string: " + input);
		}

		public bool TryParse(string input, out object retVal) {
			ResourcesList rl;
			bool r = InternalTryParse(input, out rl);
			retVal = rl;
			return r;
		}

		//we expect sth. like this:
		//3 i_apples, 1 i_spruce_log, t_light, 5 a_warcry, 35.6 hiding etc....
		private static bool InternalTryParse(string input, out ResourcesList retVal) {
			Match m = re.Match(input);
			if (InternalProcessMatch(m, out retVal)) {
				return true;
			}
			return false;
		}

		internal static bool InternalProcessMatch(Match m, out ResourcesList retVal) {
			if (m.Success) {
				List<IResourceListEntry> resources = new List<IResourceListEntry>();
				int n = m.Groups["resource"].Captures.Count; //number of found resources
				CaptureCollection numbers = m.Groups["number"].Captures;
				CaptureCollection values = m.Groups["value"].Captures;
				for (int i = 0; i < n; i++) {
					string number = "";
					try {
						number = numbers[i].Value;
						if (number.Equals(""))
							number = "1";
					} catch {
						//maybe we have something like this: "2 i_something, i_something_else" which we want to be interpreted as "2 ..., 1 ..."
						number = "1";
					}
					string value = values[i].Value; //resource name (trimmed)
					double nmr = ConvertTools.ParseDouble(number);
					resources.Add(ParseResListItem(nmr, value));
				}
				retVal = new ResourcesList(resources);
				return true;
			}
			retVal = null;
			return false;
		}
		#endregion

		private static List<TryParseResource> parsers = new List<TryParseResource>();

		/// <summary>Register resource parsing methods here</summary>
		public static void RegisterResourceParser(TryParseResource parserDeleg) {
			parsers.Add(parserDeleg);
		}

		internal static IResourceListEntry ParseResListItem(double number, string definition) {
			//first check if the definition begins with '%' (this means that we want to consume in percents not in absolute values
			bool isPercent = definition.StartsWith("%");
			string realDefinition = definition;
			if (isPercent) {
				realDefinition = definition.Substring(1); //omit the starting '%' character
			}

			foreach (var parser in parsers) {
				IResourceListEntry entry;
				if (parser(realDefinition, number, isPercent, out entry)) {
					return entry;
				}
			}

			throw new SEException(LogStr.Error("Unresolved resource: " + number + " " + realDefinition));
		}
	}
}