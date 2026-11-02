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
using SteamEngine.Persistence;
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts {

	/// <summary>#962 - Erebor's f_skillhide: 3 to 1.5 s, then Hiding against rand(1025). Anything else started meanwhile spoils it.
	/// Once hidden a step rolls Stealth (Sphere's CheckRevealOnMove), the dark helping, and running always gives you away.</summary>
	[ViewableClass]
	public class HidingSkillDef : SkillDef {
		private const int RollOf = 1025, StepRollOf = 105, DarknessFactor = 10;

		public HidingSkillDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
		}

		protected override TriggerResult On_Select(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			ScriptedSkills.Stop(self);
			Reveal(self);
			self.AddTimer(HidingTimer.Key, new HidingTimer()).DueInSeconds = this.GetDelayForValue((ushort) self.GetSkill(SkillName.Hiding));
			return TriggerResult.Cancel;
		}

		protected override TriggerResult On_Start(SkillSequenceArgs skillSeqArgs) {
			return TriggerResult.Cancel;
		}

		protected override TriggerResult On_Stroke(SkillSequenceArgs skillSeqArgs) {
			return TriggerResult.Cancel;
		}

		protected override void On_Success(SkillSequenceArgs skillSeqArgs) {
		}

		protected override void On_Fail(SkillSequenceArgs skillSeqArgs) {
		}

		protected override void On_Abort(SkillSequenceArgs skillSeqArgs) {
		}

		/// <summary>stopscriptedskills: a hiding under way fails now.</summary>
		internal static void Spoil(Character self) {
			HidingTimer timer = self.GetTimer(HidingTimer.Key) as HidingTimer;
			if (timer != null) {
				timer.Delete();
				self.RedMessage(Loc<HidingLoc>.Get(self.Language).Failed);
			}
		}

		internal static void Finish(Character self) {
			if (self.GetSkill(SkillName.Hiding) > Globals.dice.Next(RollOf)) {
				Hide(self);
				self.SysMessage(Loc<HidingLoc>.Get(self.Language).Hidden, Bandaging.ClassMessageColor);
			} else {
				self.RedMessage(Loc<HidingLoc>.Get(self.Language).Failed);
			}
		}

		[SteamFunction]
		public static void Hide(Character self) {
			self.Flag_InvisByMagic = false;
			self.Flag_Hidden = true;
		}

		/// <summary>Erebor's reveal, whatever made you unseen.</summary>
		public static void Reveal(Character self) {
			if (self.Flag_Hidden || self.Flag_InvisByMagic) {
				self.Flag_Hidden = false;
				self.Flag_InvisByMagic = false;
				HidingLoc texts = Loc<HidingLoc>.Get(self.Language);
				self.SysMessage(self.IsFemale ? texts.FoundFemale : texts.FoundMale);
			}
		}

		public static void UnHide(Character self) {
			Reveal(self);
		}

		/// <summary>Sphere's CheckRevealOnMove. Stealth counts the sector's darkness, as i_stealthbonus adds it.</summary>
		internal static void OnStep(Character self, bool running) {
			if (self.Flag_Hidden) {
				int stealth = self.GetSkill(SkillName.Stealth) + (LightAndWeather.GetLightAt(self) * DarknessFactor);
				if (running || !SkillUtils.CheckSuccess(stealth, Globals.dice.Next(StepRollOf) * 10)) {
					Reveal(self);
				}
			} else if (self.Flag_InvisByMagic) {
				Reveal(self);
			}
		}
	}

	/// <summary>Stealth has no use of its own on Erebor: its button hides you, and it's rolled as you walk hidden.</summary>
	[ViewableClass]
	public class StealthSkillDef : HidingSkillDef {
		public StealthSkillDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
		}

		protected override TriggerResult On_Select(SkillSequenceArgs skillSeqArgs) {
			skillSeqArgs.Self.SelectSkill(SkillName.Hiding);
			return TriggerResult.Cancel;
		}
	}

	[SaveableClass, DeepCopyableClass]
	public class HidingTimer : BoundTimer {
		public static readonly TimerKey Key = TimerKey.Acquire("_hiding_");

		[LoadingInitializer, DeepCopyImplementation]
		public HidingTimer() {
		}

		protected override void OnTimeout(TagHolder cont) {
			HidingSkillDef.Finish((Character) cont);
		}
	}

	internal class HidingLoc : CompiledLocStringCollection<HidingLoc> {
		public string Hidden = "Skryti se povedlo.";
		public string Failed = "Nepovedlo se ti schovat.";
		public string FoundMale = "Byl jsi objeven.";
		public string FoundFemale = "Byla jsi objevena.";
	}
}
