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
using System.Text;
using System.Text.RegularExpressions;
using SteamEngine.Common;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {

	/// <summary>#825 - a placeholder for one of the Erebor archive's [SPEECH] blocks that isn't ported yet. The
	/// converter writes one [SpeechStub name] per archive block (scripts/converted), keeping its real ON= patterns
	/// and the original Sphere script as comments under them, so every TSPEECH=/SPEECH= reference resolves and the
	/// speech shows on the def. It does nothing for players: when someone says a phrase matching one of its
	/// patterns, GMs near the listening NPC get a hint naming the speech, so live testing shows what is unported.
	/// A real speech of the same name (a C# AbstractSpeech like BankerSpeech, or an LScript [Speech] section)
	/// replaces it - the stub section is then skipped.</summary>
	public sealed class SpeechStub : AbstractSpeech {
		private static readonly Dictionary<string, Regex> regexes = new Dictionary<string, Regex>(StringComparer.OrdinalIgnoreCase);

		private string[] patterns = new string[0];
		private Regex[] matchers = new Regex[0];

		private SpeechStub(string defname)
			: base(defname) {
		}

		public static void Bootstrap() {
			ScriptLoader.RegisterScriptType("SpeechStub", LoadFromScripts, false);
		}

		private static IUnloadable LoadFromScripts(PropsSection input) {
			string name = input.HeaderName;
			AbstractScript existing = AbstractScript.GetByDefname(name);
			SpeechStub stub = existing as SpeechStub;
			if (existing is AbstractSpeech && stub == null) {
				return SkippedStub.instance; //ported - the real speech wins (and replaces the stub if it loads later)
			}
			if (existing != null && stub == null) {
				throw new SEException(input.Filename, input.HeaderLine, "A script called " + LogStr.Ident(name) + " already exists, the speech placeholder can't use its name.");
			}
			if (stub == null) {
				stub = new SpeechStub(name);
				stub.Register();
			} else {
				stub.UnUnload(); //a resync re-reading the same section
			}

			int n = input.TriggerCount;
			stub.patterns = new string[n];
			stub.matchers = new Regex[n];
			for (int i = 0; i < n; i++) {
				string pattern = input.GetTrigger(i).TriggerName;
				stub.patterns[i] = pattern;
				stub.matchers[i] = GetRegex(pattern);
			}
			return stub;
		}

		public override bool IsPlaceholder {
			get { return true; }
		}

		/// <summary>The archive's ON= patterns, in order.</summary>
		public IList<string> Patterns {
			get { return Array.AsReadOnly(this.patterns); }
		}

		/// <summary>The first of this speech's patterns the utterance matches, or null.</summary>
		public string FindMatchingPattern(string speech) {
			if (speech == null) {
				return null;
			}
			speech = speech.Trim();
			for (int i = 0; i < this.matchers.Length; i++) {
				if (this.matchers[i].IsMatch(speech)) {
					return this.patterns[i];
				}
			}
			return null;
		}

		protected override SpeechResult Handle(AbstractCharacter listener, SpeechArgs speechArgs) {
			return this.TryHandle(listener, speechArgs);
		}

		protected override SpeechResult TryHandle(AbstractCharacter listener, SpeechArgs speechArgs) {
			string pattern = this.FindMatchingPattern(speechArgs.Speech);
			if (pattern != null) {
				string hint = "[unported speech " + this.Defname + " on " + listener.Name + ": " + pattern + "]";
				foreach (AbstractCharacter ch in listener.GetMap().GetPlayersInRange(listener.X, listener.Y, Globals.SpeechDistance)) {
					Character player = ch as Character;
					if (player != null && player.IsGM) {
						player.SysMessage(hint);
					}
				}
			}
			return SpeechResult.IgnoredOrActedUpon; //never handles it - pet commands, vendors etc. still hear it
		}

		/// <summary>Sphere's Str_Match: the pattern matches the WHOLE utterance, case-insensitively, '*' being any
		/// run of characters and '?' any one character.</summary>
		internal static Regex GetRegex(string pattern) {
			Regex regex;
			if (!regexes.TryGetValue(pattern, out regex)) {
				StringBuilder sb = new StringBuilder("^");
				foreach (char c in pattern.Trim()) {
					if (c == '*') {
						sb.Append(".*");
					} else if (c == '?') {
						sb.Append('.');
					} else {
						sb.Append(Regex.Escape(c.ToString()));
					}
				}
				sb.Append('$');
				regex = new Regex(sb.ToString(), RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Singleline);
				regexes[pattern] = regex;
			}
			return regex;
		}

		public override string ToString() {
			return "SpeechStub " + this.Defname;
		}

		private sealed class SkippedStub : IUnloadable {
			internal static readonly SkippedStub instance = new SkippedStub();
			public void Unload() { }
			public bool IsUnloaded { get { return false; } }
		}
	}
}
