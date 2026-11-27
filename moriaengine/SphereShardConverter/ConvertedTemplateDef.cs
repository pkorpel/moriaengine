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
using System.IO;
using System.Text.RegularExpressions;
using SteamEngine.Common;

namespace SteamEngine.Converter {
	public class ConvertedTemplateDef : ConvertedDef {

		bool hasNumericDefname;

		public ConvertedTemplateDef(PropsSection input, ConvertedFile convertedFile)
			: base(input, convertedFile) {

			this.headerType = "TemplateDef";
		}

		public override void FirstStage() {
			int defnum;
			if (ConvertTools.TryParseInt32(this.headerName, out defnum)) {
				this.headerName = "td_0x" + defnum.ToString("x");
				this.hasNumericDefname = true;
			}
		}

		public override void SecondStage() {
			StringWriter writer = new StringWriter();
			StringReader reader = new StringReader(this.origData.GetTrigger(0).Code.ToString());
			string line;
			int linenum = this.origData.HeaderLine;
			while ((line = reader.ReadLine()) != null) {
				linenum++;
				Match m = LocStringCollection.valueRE.Match(line);
				if (m.Success) {
					GroupCollection gc = m.Groups;
					string name = gc["name"].Value;
					string value = gc["value"].Value;
					string comment = gc["comment"].Value;

					switch (name.ToLowerInvariant()) {
						case "defname":
							if (this.hasNumericDefname) {
								this.Info(linenum, "Ignoring the numeric defname of TemplateDef '" + value + "'.");
								this.headerName = value;
								//origData.headerComment = origData.headerComment+" // "+comment;
							} else {
								this.Set(name, value, comment);
							}
							break;
						case "category":
						case "subsection":
						case "description":
							this.Set(name, value, comment);
							break;
						default:
							writer.WriteLine(line);
							break;

					}
					//writer.WriteLine();
				} else {
					writer.WriteLine(line);
				}
			}

			reader = new StringReader(writer.GetStringBuilder().ToString());
			writer = new StringWriter();
			while ((line = reader.ReadLine()) != null) {
				Match m = LocStringCollection.valueRE.Match(line);
				if (m.Success) {
					GroupCollection gc = m.Groups;
					string name = gc["name"].Value;
					string value = gc["value"].Value;
					string comment = gc["comment"].Value;

					switch (name.ToLowerInvariant()) {
						case "item":
						case "itemnewbie":
						case "buy":
						case "sell":

							//#937 - a comma is missing from the archive's "i_sewing_kit{3 5}"
							this.Set(name, FixTwoEntryLists(ItemIdsAsDefnames(FixRandomExpression(Regex.Replace(value, @"(?<=\w)\s*{", ",{")))), comment);
							break;
					}
				}
			}
		}

		internal static string FixRandomExpression(string input) {
			while (true) {
				string temp = Regex.Replace(input, @"{(?<prefix>.*?)(?<first>[a-zA-Z0-9_-]+)\s+(?<second>[a-zA-Z0-9_-]+)(?<postfix>.*?)}",
					"{${prefix}${first}, ${second}${postfix}}");
				if (temp.Equals(input)) {
					break;
				}
				input = temp;
			}
			return FixShortRandomLists(input);
		}

		private static readonly Regex weightedListRE = new Regex(@"(?<!,\s*){(?<list>[^{}]*)}", RegexOptions.CultureInvariant); //not an amount: "i_gold,{...}"
		private static readonly Regex itemIdRE = new Regex(@"^0[0-9a-fA-F]+$", RegexOptions.CultureInvariant);

		/// <summary>#1587 - in an item list's weighted pick, Sphere reads a bare number in a value's place as the item def
		/// with that id ("013cf" in RANDOM_LEATHER_ARMOR); LScript reads a number. Written as the def's name, i_0x13cf.</summary>
		private static string ItemIdsAsDefnames(string input) {
			if (input.IndexOf('"') >= 0) {
				return input;
			}
			return weightedListRE.Replace(input, m => {
				string[] parts = m.Groups["list"].Value.Split(',');
				if (parts.Length < 4) {
					return m.Value; //a range, or one entry
				}
				for (int i = 0; i < parts.Length; i += 2) {
					string part = parts[i].Trim();
					if (itemIdRE.IsMatch(part)) {
						int id = int.Parse(part, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
						parts[i] = parts[i].Replace(part, "i_0x" + id.ToString("x", CultureInfo.InvariantCulture));
					}
				}
				return "{" + string.Join(",", parts) + "}";
			});
		}

		private static readonly Regex randBracesRE = new Regex(@"\bRAND\s*{(?<max>[^{}]+)}",
			RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
		private static readonly Regex oneEntryRE = new Regex(@"{\s*(?<entry>[a-zA-Z0-9_]+)\s*}", RegexOptions.CultureInvariant);
		private static readonly Regex twoEntriesRE = new Regex(@"{\s*(?<first>[a-zA-Z_][a-zA-Z0-9_]*)\s*,\s*(?<second>[a-zA-Z0-9_]+)\s*}",
			RegexOptions.CultureInvariant);
		private static readonly Regex numberRE = new Regex(@"^[0-9][0-9a-fA-F]*$", RegexOptions.CultureInvariant);
		private static readonly Regex trailingSemicolonRE = new Regex(@"}\s*;\s*$", RegexOptions.CultureInvariant); //on the code before any comment

		/// <summary>#1587 - brace forms Sphere reads one way whatever the names are, and LScript can't read:
		/// <list type="bullet">
		/// <item>"{x}" is x.</item>
		/// <item>A ';' after the list ends the value.</item>
		/// <item>"RAND{x}" (Erebor's ratmen): Sphere's RAND needs parentheses, so it was an undefined name, 0. Written as
		/// what RAND(x) means, 0 to x - 1: fixing Erebor's bug, see the design decisions log.</item>
		/// </list>
		/// Quoted text and comments are left alone. Two-entry lists need to know the defs: <see cref="FixTwoEntryLists"/>.</summary>
		private static string FixShortRandomLists(string input) {
			int commentAt = input.IndexOf("//", StringComparison.Ordinal);
			string code = (commentAt < 0) ? input : input.Substring(0, commentAt);
			if (code.IndexOf('"') >= 0) {
				return input;
			}
			code = trailingSemicolonRE.Replace(code, "}");
			code = randBracesRE.Replace(code, m => "{0, (" + m.Groups["max"].Value.Trim() + " - 1)}");
			code = oneEntryRE.Replace(code, m => m.Groups["entry"].Value);
			return (commentAt < 0) ? code : code + input.Substring(commentAt);
		}

		/// <summary>#1587 - a two-entry brace list is always a range in Sphere: of numbers, or of the ids of the item defs
		/// named. LScript can roll a range only of numbers. Run once every item def is known (the second stage), on item
		/// lists and constants:
		/// <list type="bullet">
		/// <item>"{i_rock_plain i_rock_plain_x}" is every item def with an archive header number between the two
		/// (0x1363-0x136d, the rocks): written as a weighted list of them, each 1.</item>
		/// <item>"{i_sash 1}" ranged from i_sash's resource id (a negative number) to 1, nothing useful: Erebor meant a
		/// weighted list of one entry, so it's written as the item. Fixing Erebor's bug, see the design decisions log.</item>
		/// </list>
		/// Any other name (a constant, which can be a number) leaves the list as it is.</summary>
		internal static string FixTwoEntryLists(string input) {
			int commentAt = input.IndexOf("//", StringComparison.Ordinal);
			string code = (commentAt < 0) ? input : input.Substring(0, commentAt);
			if (code.IndexOf('"') >= 0) {
				return input;
			}
			code = twoEntriesRE.Replace(code, m => {
				string first = m.Groups["first"].Value, second = m.Groups["second"].Value;
				ConvertedItemDef.itemsByDefname.TryGetValue(first, out ConvertedThingDef firstDef);
				if (numberRE.IsMatch(second) && ((firstDef != null) || HandWrittenScripts.Items.Contains(first))) {
					return (firstDef != null) ? firstDef.EngineDefname : first; //the engine's own scripts can define it (i_spellbook)
				}
				if ((firstDef == null) || !ConvertedItemDef.itemsByDefname.TryGetValue(second, out ConvertedThingDef secondDef)
						|| (firstDef.ArchiveHeaderNumber < 0) || (secondDef.ArchiveHeaderNumber < 0)) {
					return m.Value;
				}
				int from = Math.Min(firstDef.ArchiveHeaderNumber, secondDef.ArchiveHeaderNumber);
				int to = Math.Max(firstDef.ArchiveHeaderNumber, secondDef.ArchiveHeaderNumber);
				List<string> entries = new List<string>();
				for (int id = from; id <= to; id++) {
					if (ConvertedItemDef.itemsByHeaderNumber.TryGetValue(id, out ConvertedThingDef def)) {
						entries.Add(def.EngineDefname + ", 1");
					}
				}
				return "{" + string.Join(", ", entries) + "}";
			});
			return (commentAt < 0) ? code : code + input.Substring(commentAt);
		}
	}
}