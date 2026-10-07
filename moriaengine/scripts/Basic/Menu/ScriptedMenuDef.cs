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
using SteamEngine.Common;
using SteamEngine.LScript;
using SteamEngine.Networking;

namespace SteamEngine.CompiledScripts {
	public sealed class ScriptedMenuDef : AbstractMenuDef {
		private FieldValue message;

		private LScriptHolder[] triggers;

		private NumberedLocStringCollection[] choiceLists;

		public ScriptedMenuDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {

			this.message = this.InitTypedField("message", "Menu?", typeof(string));
		}

		public override void LoadScriptLines(PropsSection ps) {
			base.LoadScriptLines(ps);

			int n = ps.TriggerCount;
			string[] locEntries = new string[n + 1];
			locEntries[0] = (string) this.message.CurrentValue;
			this.triggers = new LScriptHolder[n];

			for (int i = 0; i < n; i++) {
				TriggerSection ts = ps.GetTrigger(i);
				this.triggers[i] = new LScriptHolder(ts);
				locEntries[i + 1] = ts.TriggerName;
			}

			int langCount = Tools.GetEnumLength<Language>();
			this.choiceLists = new NumberedLocStringCollection[langCount];
			string locdefname = "loc_" + ps.HeaderName;
			for (int i = 0; i < langCount; i++) {
				this.choiceLists[i] = new NumberedLocStringCollection(locdefname, "LScript", (Language) i, locEntries);
			}
		}

		protected override IEnumerable<string> GetAllTexts(Language language) {
			return this.choiceLists[(int) language].Entries;
		}

		public override void Unload() {
			if (this.choiceLists != null) {
				foreach (NumberedLocStringCollection loc in this.choiceLists) {
					LocManager.UnregisterLoc(loc);
				}
				this.choiceLists = null;

				foreach (LScriptHolder trg in this.triggers) {
					trg.Unload();
				}
				this.triggers = null;
			}
			base.Unload();
		}

		protected override void On_Response(GameState state, int index, object parameter) {
			this.ThrowIfUnloaded();

			LScriptHolder scp = this.triggers[index];
			if (scp != null) {
				AbstractCharacter self = state.Character;
				if (self != null) {
					scp.TryRun(self, parameter);
				}
			} else {
				Logger.WriteWarning("Unknown trigger number '" + index + "' in MenuDef " + this.PrettyDefname);
			}
		}

		protected override void On_Cancel(GameState state, object parameter) {
			//TODO?			
		}
	}
}