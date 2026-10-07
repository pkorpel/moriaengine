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


namespace SteamEngine.CompiledScripts {

	public abstract class AbstractSpeech : TriggerGroup {
		protected AbstractSpeech()
		{
		}

		protected AbstractSpeech(string name)
			: base(name) {
		}

		public sealed override object Run(object self, TriggerKey tk, ScriptArgs sa) {
			if (TriggerKey.hear.Uid == tk.Uid) {
				AbstractCharacter ch = self as AbstractCharacter;
				if (self != null) {
					SpeechArgs speechArgs = sa as SpeechArgs;
					if (speechArgs != null) {
						return this.Handle(ch, speechArgs);
					}
				}
			}

			return null;
		}

		public sealed override object TryRun(object self, TriggerKey tk, ScriptArgs sa) {
			if (TriggerKey.hear.Uid == tk.Uid) {
				AbstractCharacter ch = self as AbstractCharacter;
				if (self != null) {
					SpeechArgs speechArgs = sa as SpeechArgs;
					if (speechArgs != null) {
						return this.TryHandle(ch, speechArgs);
					}
				}
			}

			return null;
		}

		public override string ToString() {
			return "SpeechDef " + this.Defname;
		}

		protected abstract SpeechResult Handle(AbstractCharacter listener, SpeechArgs speechArgs);

		protected abstract SpeechResult TryHandle(AbstractCharacter listener, SpeechArgs speechArgs);

		public new static AbstractSpeech GetByDefname(string defname) {
			return AbstractScript.GetByDefname(defname) as AbstractSpeech;
		}
	}
}