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
using SteamEngine.Common;
using SteamEngine.LScript;

namespace SteamEngine.CompiledScripts {


	public class ScriptedSpeech : AbstractSpeech {
		private static readonly Dictionary<string, Regex> regexes = new Dictionary<string, Regex>(StringComparer.OrdinalIgnoreCase);
		//pattern(string) - Regex pairs

		SpeechTrigger[] triggers;

		public ScriptedSpeech(string defname)
			: base(defname) {
		}

		private class SpeechTrigger {
			internal LScriptHolder holder;
			internal Regex re;

			internal SpeechTrigger(string pattern) {
				this.re = GetRegex(pattern);
			}
		}

		internal static IUnloadable LoadFromScripts(PropsSection input) {
			string name = input.HeaderName.ToLower();
			AbstractScript s = AbstractScript.GetByDefname(name);
			ScriptedSpeech ssd;
			if (s != null) {
				ssd = s as ScriptedSpeech;
				if (ssd == null) {//is not scripted, so can not be overriden
					throw new SEException(input.Filename, input.HeaderLine, "A script called " + LogStr.Ident(name) + " already exists!");
				}
			} else {
				ssd = new ScriptedSpeech(name);
				ssd.Register();
			}

			//now do load the trigger code. 
			int triggerCount = input.TriggerCount;
			ssd.triggers = new SpeechTrigger[triggerCount];
			List<SpeechTrigger> sameCodeTriggers = new List<SpeechTrigger>();
			for (int i = 0; i < triggerCount; i++) {
				TriggerSection trigger = input.GetTrigger(i);
				SpeechTrigger st = new SpeechTrigger(trigger.TriggerName);
				sameCodeTriggers.Add(st);
				ssd.triggers[i] = st;

				if (!IsEmptyCode(trigger.Code.ToString())) {
					LScriptHolder holder = new LScriptHolder(trigger);
					foreach (SpeechTrigger same in sameCodeTriggers) {
						same.holder = holder;
					}
					sameCodeTriggers.Clear();
				}
			}

			return ssd;
		}

		public override void Unload() {
			base.Unload();
			this.triggers = null;
		}

		protected override SpeechResult Handle(AbstractCharacter listener, SpeechArgs speechArgs) {
			string message = speechArgs.Speech;
			foreach (SpeechTrigger st in this.triggers) {
				Match m = st.re.Match(message);
				if (m.Success) {
					object o = st.holder.Run(listener, speechArgs);
					return (SpeechResult) Convert.ToInt32(o);
				}
			}
			return SpeechResult.IgnoredOrActedUpon;
		}

		protected override SpeechResult TryHandle(AbstractCharacter listener, SpeechArgs speechArgs) {
			string message = speechArgs.Speech;
			foreach (SpeechTrigger st in this.triggers) {
				Match m = st.re.Match(message);
				if (m.Success) {
					object o = st.holder.TryRun(listener, speechArgs);
					try {
						return (SpeechResult) Convert.ToInt32(o);
					} catch { }
				}
			}
			return SpeechResult.IgnoredOrActedUpon;
		}

		private static Regex GetRegex(string triggername) {
			string pattern = GeneratePattern(triggername);
			Regex regex;
			if (!regexes.TryGetValue(pattern, out regex)) {
				regex = new Regex(pattern,
					RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
				regexes[pattern] = regex;
			}
			return regex;
		}

		private static string GeneratePattern(string triggername) {
			triggername = triggername.Trim();
			if (triggername.StartsWith("<<") && triggername.EndsWith(">>")) {
				return triggername.Substring(2, triggername.Length - 4);
			}
			triggername = Regex.Escape(triggername);
			return triggername.Replace(@"\*", ".*");
		}

		private static bool IsEmptyCode(string code) {
			string line;
			StringReader reader = new StringReader(code);
			while ((line = reader.ReadLine()) != null) {
				line = line.Trim();
				if ((line.Length != 0) && (!line.StartsWith("//"))) {
					return false;
				}
			}
			return true;
		}

		public new static void Bootstrap() {
			ScriptLoader.RegisterScriptType(new[] { "Speech", "SpeechDef" }, LoadFromScripts, false);
		}
	}

}