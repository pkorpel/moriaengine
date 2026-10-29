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

		public override void FirstStage() {
			base.FirstStage();
			string line;
			StringReader reader = new StringReader(this.origData.GetTrigger(0).Code.ToString());
			while ((line = reader.ReadLine()) != null) {
				this.writtenData.Add(ConvertedTemplateDef.FixRandomExpression(line));
				string trimmed = line.Trim();
				if ((trimmed.Length > 0) && !trimmed.StartsWith("//", StringComparison.Ordinal)) {
					definedNames.Add(trimmed.Split(new[] { ' ', '\t', '=' }, 2)[0]);
				}
			}
		}

		/// <summary>A bare identifier that no [DEFNAMES] section of the archive defines (only meaningful after the
		/// first stage).</summary>
		internal static bool IsUndefinedName(string value) {
			string trimmed = value.Trim();
			return identifierRE.IsMatch(trimmed) && !definedNames.Contains(trimmed);
		}
	}
}