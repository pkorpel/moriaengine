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
using System.Text.RegularExpressions;

namespace SteamEngine.Converter {
	public class ConvertedConstants : ConvertedDef {
		public ConvertedConstants(PropsSection input, ConvertedFile convertedFile)
			: base(input, convertedFile) {
			this.headerType = "Constants";
		}

		//#828 - every name the archive's [DEFNAMES] define, collected in the first stage
		private static readonly HashSet<string> definedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		private static readonly Regex identifierRE = new Regex(@"^[A-Za-z_]\w*$", RegexOptions.CultureInvariant);

		//#937 - where each name was last defined: as in Sphere the later one counts, the earlier one is commented out
		private static readonly Dictionary<string, (ConvertedConstants def, int index)> lastDefinitions =
			new Dictionary<string, (ConvertedConstants, int)>(StringComparer.OrdinalIgnoreCase);

		public override void FirstStage() {
			base.FirstStage();
			string line;
			StringReader reader = new StringReader(this.origData.GetTrigger(0).Code.ToString());
			while ((line = reader.ReadLine()) != null) {
				string trimmed = line.Trim();
				if ((trimmed.Length == 0) || trimmed.StartsWith("//", StringComparison.Ordinal)) {
					this.writtenData.Add(ConvertedTemplateDef.FixRandomExpression(line));
					continue;
				}
				string name = trimmed.Split(new[] { ' ', '\t', '=' }, 2)[0];
				definedNames.Add(name);
				if (HandWrittenScripts.IsConstant(name)) {
					this.writtenData.Add("//" + line + " //defined in the engine's own scripts, commented out by Converter");
					continue;
				}
				if (lastDefinitions.TryGetValue(name, out var previous)) {
					previous.def.writtenData[previous.index] = "//" + previous.def.writtenData[previous.index] + " //defined again later, commented out by Converter";
				}
				lastDefinitions[name] = (this, this.writtenData.Count);
				this.writtenData.Add(ConvertedTemplateDef.FixRandomExpression(WithValue(line, name)));
			}
		}

		//a name alone is an empty string in Sphere
		private static string WithValue(string line, string name) {
			int nameEnd = line.IndexOf(name, StringComparison.Ordinal) + name.Length;
			string rest = line.Substring(nameEnd);
			if (rest.TrimStart().Length == 0) {
				return line.TrimEnd() + " \"\"";
			}
			if (rest.TrimStart().StartsWith("//", StringComparison.Ordinal)) {
				return line.Substring(0, nameEnd) + " \"\"" + rest;
			}
			if (name.StartsWith("typename_", StringComparison.OrdinalIgnoreCase)) {
				return QuotedText(line, nameEnd, rest);
			}
			return line;
		}

		//#962 - Erebor's names for item types are plain words, some with a space or a question mark
		private static string QuotedText(string line, int nameEnd, string rest) {
			if (rest.TrimStart().StartsWith("\"", StringComparison.Ordinal)) {
				return line;
			}
			int comment = rest.IndexOf("//", StringComparison.Ordinal);
			string text = ((comment < 0) ? rest : rest.Substring(0, comment)).Trim();
			string tail = (comment < 0) ? "" : " " + rest.Substring(comment);
			return line.Substring(0, nameEnd) + "\t\"" + text + "\"" + tail;
		}

		/// <summary>A bare identifier that no [DEFNAMES] section of the archive defines (only meaningful after the
		/// first stage).</summary>
		internal static bool IsUndefinedName(string value) {
			string trimmed = value.Trim();
			return identifierRE.IsMatch(trimmed) && !definedNames.Contains(trimmed);
		}
	}
}