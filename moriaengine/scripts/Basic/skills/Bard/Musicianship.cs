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

using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {

	/// <summary>#962 - Sphere's skill, played by double clicking an instrument: the roll is against 0-90. The skill list
	/// has no use for it.</summary>
	[ViewableClass]
	public class MusicianshipSkillDef : SkillDef {
		private const int MostDifficulty = 90, DifficultyScale = 10;

		public MusicianshipSkillDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
		}

		protected override TriggerResult On_Select(SkillSequenceArgs skillSeqArgs) {
			return (skillSeqArgs.Tool is Musical) ? TriggerResult.Continue : TriggerResult.Cancel;
		}

		protected override TriggerResult On_Start(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			Musical instrument = (Musical) skillSeqArgs.Tool;
			if (this.CheckSuccess(self, Globals.dice.Next(MostDifficulty) * DifficultyScale)) {
				self.Sound(instrument.TypeDef.SuccessSound);
				return TriggerResult.Continue;
			}
			self.Sound(instrument.TypeDef.FailureSound);
			BardLoc texts = Loc<BardLoc>.Get(self.Language);
			self.SysMessage(texts.PlayPoorly);
			self.Message(texts.PlayFalse);
			return TriggerResult.Cancel;
		}

		protected override TriggerResult On_Stroke(SkillSequenceArgs skillSeqArgs) {
			skillSeqArgs.Success = true;
			return TriggerResult.Continue;
		}

		protected override void On_Success(SkillSequenceArgs skillSeqArgs) {
		}

		protected override void On_Fail(SkillSequenceArgs skillSeqArgs) {
		}

		protected override void On_Abort(SkillSequenceArgs skillSeqArgs) {
		}
	}
}
