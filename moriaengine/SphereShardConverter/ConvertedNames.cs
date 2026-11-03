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
using System.IO;
using SteamEngine.Common;

namespace SteamEngine.Converter {

	/// <summary>#1070 - spherename.scp's [NAMES x]: a count, then one name a line. Written as [NameList x] with a
	/// "Name = ..." line each, which NameLists loads.</summary>
	public class ConvertedNames : ConvertedDef {

		public ConvertedNames(PropsSection input, ConvertedFile convertedFile)
			: base(input, convertedFile) {
			this.headerType = "NameList";
		}

		public override void Dump(TextWriter writer) {
			writer.WriteLine();
			writer.WriteLine(string.Concat("[", this.headerType, " ", this.headerName, "]"));
			bool countSeen = false;
			int n = this.origData.TriggerCount;
			for (int i = 0; i < n; i++) {
				using (StringReader reader = new StringReader(this.origData.GetTrigger(i).Code.ToString())) {
					string line;
					while ((line = reader.ReadLine()) != null) {
						int comment = line.IndexOf("//", StringComparison.Ordinal);
						string name = ((comment >= 0) ? line.Substring(0, comment) : line).Trim();
						if (name.Length == 0) {
							continue;
						}
						if (!countSeen && int.TryParse(name, out _)) {
							countSeen = true;
							continue;
						}
						writer.WriteLine("Name = \"" + name.Replace("\"", "") + "\"");
					}
				}
			}
		}
	}
}
