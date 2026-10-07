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


using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {
	[ViewableClass]
	public class MeditationSkillDef : SkillDef {

		public MeditationSkillDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
		}

		protected override TriggerResult On_Select(SkillSequenceArgs skillSeqArgs) {
			//todo: paralyzed state etc.
			if (!this.CheckPrerequisities(skillSeqArgs)) {
				return TriggerResult.Cancel;
			}
			return TriggerResult.Continue;
		}

		protected override TriggerResult On_Start(SkillSequenceArgs skillSeqArgs) {
			return TriggerResult.Continue; //continue to delay, then @stroke
		}

		protected override TriggerResult On_Stroke(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;

			if (!this.CheckPrerequisities(skillSeqArgs)) {
				return TriggerResult.Cancel;
			}
			skillSeqArgs.Success = this.CheckSuccess(self, Globals.dice.Next(700));

			return TriggerResult.Continue; //continue to @success or @fail
		}

		//bonus talent 
		private static PassiveAbilityDef a_meditation_bonus;
		public static PassiveAbilityDef MeditationBonusDef {
			get {
				if (a_meditation_bonus == null) {
					a_meditation_bonus = (PassiveAbilityDef) AbilityDef.GetByDefname("a_meditation_bonus");
				}
				return a_meditation_bonus;
			}
		}

		protected override void On_Success(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;

			self.ClilocSysMessage(501851);//You enter a meditative trance.
			MeditationPlugin mpl = (MeditationPlugin) MeditationPlugin.defInstance.Create();
			double effect = this.GetEffectForChar(self);
			effect += effect * MeditationBonusDef.EffectPower * self.GetAbility(MeditationBonusDef);
			mpl.additionalManaRegenSpeed = effect;
			self.AddPlugin(MeditationPlugin.meditationPluginKey, mpl);
		}

		protected override void On_Fail(SkillSequenceArgs skillSeqArgs) {
			skillSeqArgs.Self.ClilocSysMessage(501848);//You cannot focus your concentration
		}

		protected override void On_Abort(SkillSequenceArgs skillSeqArgs) {
			skillSeqArgs.Self.ClilocSysMessage(501848);//You cannot focus your concentration
		}

		/// <summary>
		/// Check if we are alive, don't have weapons etc.... Return false if the trigger above 
		/// should be cancelled or true if we can continue
		/// </summary>
		private bool CheckPrerequisities(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			if (!self.CheckAliveWithMessage()) {
				return false;//no message needed, it's been already sent in the called method
			}
			if (self.Weapon != null) {
				self.ClilocSysMessage(502626);//Your hands must be free to cast spells or meditate.
				return false; //stop
			}
			if (self.Hits <= self.MaxHits / 10) {
				self.ClilocSysMessage(501849);//The mind is strong, but the body is weak.
				return false; //stop
			}
			if (self.Mana >= self.MaxMana) {
				self.ClilocSysMessage(501846);//You are at peace.
				return false; //stop
			}
			return true;
		}

		[SteamFunction]
		public static void Meditate(Character self) {
			self.SelectSkill(SkillName.Meditation);
		}
	}

	[ViewableClass]
	public partial class MeditationPlugin {
		public static readonly MeditationPluginDef defInstance = new MeditationPluginDef("p_meditation", "C#scripts", -1);
		internal static PluginKey meditationPluginKey = PluginKey.Acquire("_meditation_");

		public void On_Assign() {
			//add the regeneration speed to character
			((Character) this.Cont).ManaRegenSpeed += this.additionalManaRegenSpeed;
		}

		public void On_UnAssign(Character formerCont) {
			formerCont.ManaRegenSpeed -= this.additionalManaRegenSpeed;
			if (formerCont.Mana >= formerCont.MaxMana) {//meditation finished
				formerCont.ClilocSysMessage(501846);//You are at peace.
			} else {//meditation somehow aborted
				formerCont.ClilocSysMessage(501848);//You cannot focus your concentration
			}
		}

		public void On_Step(ScriptArgs args) {
			this.Delete();
		}

		public void On_SkillStart(SkillSequenceArgs skillSeqArgs) {
			this.Delete();
		}

		//TODO - other triggers such as ItemPickup, Speak, DClick, use another skill or ability etc...
	}

	[ViewableClass]
	public partial class MeditationPluginDef {
	}
}