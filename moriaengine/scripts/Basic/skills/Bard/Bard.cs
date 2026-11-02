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

namespace SteamEngine.CompiledScripts {

	/// <summary>#962 - what Erebor's bard skills share: all three want an instrument in the pack, roll Musicianship against
	/// 0-1025, then their own skill against the target's experience. Nobody gains in them by use, they have ADVRATE 0fffffff
	/// and no skillgain call.</summary>
	public static class Bard {
		private const int MusicRollOf = 1026;

		public static Musical FindInstrument(Character self) {
			return (Musical) self.Backpack.FindByClass(typeof(Musical));
		}

		/// <summary>promilerange of the skill's effect. Every bard skill's effect starts at 0, so this is also randfakt's top.</summary>
		internal static int Reach(SkillDef skill, Character self) {
			return (int) skill.GetEffectForValue(self.GetSkill((SkillName) skill.Id));
		}

		internal static int Power(SkillDef skill, Character self) {
			return Globals.dice.Next(Reach(skill, self) + 1);
		}

		internal static double Delay(SkillDef skill, Character self) {
			return skill.GetDelayForValue((ushort) self.GetSkill((SkillName) skill.Id));
		}

		internal static int ExperienceOf(Character target) {
			return (target is NPC npc) ? npc.ExperienceReward : 0;
		}

		internal static bool IsBoss(NPC npc) {
			return ((NPCDef) npc.Def).IsBoss;
		}

		/// <summary>The roll, with its sound and its message. Nothing else happens when it says no.</summary>
		internal static bool Plays(Character self, Musical instrument) {
			if (self.GetSkill(SkillName.Musicianship) < Globals.dice.Next(MusicRollOf)) {
				self.SysMessage(Loc<BardLoc>.Get(self.Language).PlayPoorly);
				self.Sound(instrument.TypeDef.FailureSound);
				return false;
			}
			self.Sound(instrument.TypeDef.SuccessSound);
			return true;
		}
	}

	internal class BardLoc : CompiledLocStringCollection<BardLoc> {
		public string PlayPoorly = "You play poorly.";
		public string NoInstrumentAvailable = "You have no musical instrument available";
		public string NeedInstrument = "Potrebujes nejaky hudebni nastroj.";
		public string PlayFalse = "Hrajes falesne.";
	}
}
