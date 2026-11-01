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
	/// <summary>#962 - Sphere's Skill_Meditation, as Erebor keeps it: a roll to begin, then a mana point a stroke until full.
	/// Stepping breaks it (e_meditation), and a mana shield leaves no mind for it.</summary>
	[ViewableClass]
	public class MeditationSkillDef : SkillDef {
		private const int AtPeace = 501846, FirstStrokeSound = 0xf9, DifficultyRoll = 100;

		public MeditationSkillDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
		}

		protected override TriggerResult On_Select(SkillSequenceArgs skillSeqArgs) {
			return TriggerResult.Continue;
		}

		protected override TriggerResult On_Start(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			MeditationLoc texts = Loc<MeditationLoc>.Get(self.Language);
			if (self.GetPlugin(DamageManager.manaShieldKey) != null) {
				self.SysMessage(texts.HoldingABarrier);
				return TriggerResult.Cancel;
			}
			if (self.Mana >= self.MaxMana) {
				self.ClilocSysMessage(AtPeace);
				return TriggerResult.Cancel;
			}
			self.SysMessage(texts.Attempting);
			int difficulty = Globals.dice.Next(DifficultyRoll) * 10;
			skillSeqArgs.Success = (difficulty <= 0) || this.CheckSuccess(self, difficulty);
			skillSeqArgs.Param1 = null;
			return TriggerResult.Continue; //continue to delay, then @stroke
		}

		protected override TriggerResult On_Stroke(SkillSequenceArgs skillSeqArgs) {
			return TriggerResult.Continue;
		}

		protected override void On_Success(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			if (self.Mana >= self.MaxMana) {
				self.ClilocSysMessage(AtPeace);
				return;
			}
			if (skillSeqArgs.Param1 == null) {
				self.Sound(FirstStrokeSound);
				skillSeqArgs.Param1 = true;
			}
			self.Mana++;
			skillSeqArgs.DelayStroke();
		}

		protected override void On_Fail(SkillSequenceArgs skillSeqArgs) {
		}

		protected override void On_Abort(SkillSequenceArgs skillSeqArgs) {
		}

		/// <summary>e_meditation's @step.</summary>
		internal static void OnStep(Character self) {
			if (self.CurrentSkillName == SkillName.Meditation) {
				self.SysMessage(Loc<MeditationLoc>.Get(self.Language).LostConcentration);
				self.AbortSkill();
			}
		}

		[SteamFunction]
		public static void Meditate(Character self) {
			self.SelectSkill(SkillName.Meditation);
		}
	}

	internal class MeditationLoc : CompiledLocStringCollection<MeditationLoc> {
		public string HoldingABarrier = "Tva mysl se soustredi na udrzeni ochranne bariery...";
		public string Attempting = "You attempt a meditative trance.";
		public string LostConcentration = "Pohybem jsi ztratil soustredeni";
	}
}
