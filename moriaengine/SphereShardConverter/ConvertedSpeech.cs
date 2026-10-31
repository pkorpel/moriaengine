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

	/// <summary>#825 - an archive [SPEECH x] block becomes a [SpeechStub x] placeholder (the engine's SpeechStub):
	/// its ON= patterns are kept as they are, and the Sphere script under each one is kept as comments, for
	/// porting later. Sphere script can't run as LScript, so nothing of the body is live.</summary>
	public class ConvertedSpeech : ConvertedDef {
		private static readonly HashSet<string> names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		public ConvertedSpeech(PropsSection input, ConvertedFile convertedFile)
			: base(input, convertedFile) {
			this.headerType = "SpeechStub";
			names.Add(input.HeaderName);
		}

		internal static bool IsDefined(string name) {
			return names.Contains(name.Trim());
		}

		public override void Dump(TextWriter writer) {
			writer.WriteLine();
			string header = string.Concat("[", this.headerType, " ", this.headerName, "]");
			if (this.origData.HeaderComment.Length > 0) {
				header = header + " //" + this.origData.HeaderComment;
			}
			writer.WriteLine(header);

			int n = this.origData.TriggerCount;
			for (int i = 0; i < n; i++) {
				TriggerSection trigger = this.origData.GetTrigger(i);
				//the first "trigger" is what comes before the first ON= (the section starts as script)
				if (string.Equals(trigger.TriggerKey, "on", StringComparison.OrdinalIgnoreCase)) {
					string on = "ON=" + trigger.TriggerName;
					if (!string.IsNullOrEmpty(trigger.TriggerComment)) {
						on = on + " //" + trigger.TriggerComment;
					}
					writer.WriteLine(on);
				}
				foreach (string line in CodeLines(trigger)) {
					if (line.Length == 0 || line.StartsWith("//", StringComparison.Ordinal)) {
						writer.WriteLine(line);
					} else {
						writer.WriteLine("//" + line);
					}
				}
			}
		}

		//the trigger's code lines, without the blank ones it ends with
		private static List<string> CodeLines(TriggerSection trigger) {
			List<string> lines = new List<string>();
			using (StringReader reader = new StringReader(trigger.Code.ToString())) {
				string line;
				while ((line = reader.ReadLine()) != null) {
					lines.Add(line.Trim());
				}
			}
			while (lines.Count > 0 && lines[lines.Count - 1].Length == 0) {
				lines.RemoveAt(lines.Count - 1);
			}
			return lines;
		}
	}
}
