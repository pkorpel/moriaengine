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

	/// <summary>#962 - Erebor's systems/skills/provocation.scp, the command .provo: pick a creature, then whoever it should go
	/// for, each rolled on its own. The skill's button only says so.</summary>
	[ViewableClass]
	public class ProvocationSkillDef : SkillDef {
		public ProvocationSkillDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
		}

		protected override TriggerResult On_Select(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			self.SysMessage(Loc<ProvocationLoc>.Get(self.Language).UseTheCommand);
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

		[SteamFunction]
		public static void Provo(Character self) {
			ScriptedSkills.Stop(self);
			SkillSequenceArgs.AbortSkill(self);
			ProvocationLoc texts = Loc<ProvocationLoc>.Get(self.Language);
			if (self.GetTimer(ProvocationTimer.Key) != null) {
				self.SysMessage(texts.DontHurry);
			} else if (self.Flag_Dead) {
			} else if (Bard.FindInstrument(self) == null) {
				self.SysMessage(Loc<BardLoc>.Get(self.Language).NeedInstrument);
			} else if (!PlayerHunger.DistractsFrom(self) && (self is Player player)) {
				ProvocationTimer timer = new ProvocationTimer();
				self.AddTimer(ProvocationTimer.Key, timer).DueInSeconds = ProvocationTimer.SecondsForTheCursor;
				self.SysMessage(texts.WhoFirst);
				player.Target(SingletonScript<Targ_Provocation>.Instance, timer);
			}
		}

		/// <summary>stopscriptedskills.</summary>
		internal static void Spoil(Character self) {
			ProvocationTimer timer = self.GetTimer(ProvocationTimer.Key) as ProvocationTimer;
			if (timer != null) {
				timer.firstWorks = false;
				timer.secondWorks = false;
				timer.DueInSeconds = 0;
			}
		}
	}

	public class Targ_Provocation : CompiledTargetDef {
		protected override TargetResult On_TargonChar(Player self, Character targetted, object parameter) {
			ProvocationTimer timer = (ProvocationTimer) parameter;
			if (timer.IsDeleted) {
				return TargetResult.Done;
			}
			ProvocationLoc texts = Loc<ProvocationLoc>.Get(self.Language);
			SkillDef skill = SkillDef.GetBySkillName(SkillName.Provocation);
			if (timer.first == null) {
				if (targetted.IsPlayer || (targetted.GetSkill(SkillName.Provocation) > 0)) {
					self.SysMessage(texts.CantProvoke);
					timer.Delete();
				} else if (Bard.Reach(skill, self) < Bard.ExperienceOf(targetted)) {
					self.SysMessage(texts.BeyondYou);
					timer.Delete();
				} else {
					timer.first = targetted;
					timer.DueInSeconds = ProvocationTimer.SecondsForTheCursor;
					self.SysMessage(texts.WhoAgainst);
					return TargetResult.RestartTargetting;
				}
				return TargetResult.Done;
			}
			HidingSkillDef.Reveal(self);
			if (targetted == timer.first) {
				return TargetResult.RestartTargetting;
			}
			timer.second = targetted;
			timer.played = true;
			if (Bard.Plays(self, Bard.FindInstrument(self))) {
				timer.firstWorks = Bard.Power(skill, self) > Bard.ExperienceOf(timer.first);
				timer.secondWorks = !targetted.IsPlayer && (targetted.GetSkill(SkillName.Provocation) == 0)
					&& (Bard.Power(skill, self) > Bard.ExperienceOf(targetted));
			}
			timer.DueInSeconds = Bard.Delay(skill, self);
			return TargetResult.Done;
		}

		protected override TargetResult On_TargonItem(Player self, Item targetted, object parameter) {
			return TargetResult.RestartTargetting;
		}

		protected override TargetResult On_TargonGround(Player self, IPoint3D targetted, object parameter) {
			return TargetResult.Done;
		}
	}

	/// <summary>i_provo_timer.</summary>
	[SaveableClass, DeepCopyableClass]
	public class ProvocationTimer : BoundTimer {
		public static readonly TimerKey Key = TimerKey.Acquire("_provocation_");

		internal const double SecondsForTheCursor = 60;

		[SaveableData, CopyableData]
		public Character first;

		[SaveableData, CopyableData]
		public Character second;

		[SaveableData, CopyableData]
		public bool played;

		[SaveableData, CopyableData]
		public bool firstWorks;

		[SaveableData, CopyableData]
		public bool secondWorks;

		[LoadingInitializer, DeepCopyImplementation]
		public ProvocationTimer() {
		}

		protected override void OnTimeout(TagHolder cont) {
			Character self = (Character) cont;
			HidingSkillDef.Reveal(self);
			if (!this.played || (this.first == null) || (this.second == null) || this.first.IsDeleted || this.second.IsDeleted) {
				return;
			}
			ProvocationLoc texts = Loc<ProvocationLoc>.Get(self.Language);
			if (this.firstWorks || this.secondWorks) {
				self.SysMessage(texts.Worked, Bandaging.ClassMessageColor);
			} else {
				self.RedMessage(texts.DidntWork);
			}
			Provoke(self, this.first, this.second, this.firstWorks, texts);
			this.second.Emote("is furious");
			Provoke(self, this.second, this.first, this.secondWorks, texts);
		}

		private static void Provoke(Character self, Character who, Character whom, bool works, ProvocationLoc texts) {
			if (works && (who is NPC npc)) {
				NPCAiTimer.Attack(npc, whom);
			} else {
				self.SysMessage(string.Format(texts.DoesntReact, who.Name));
			}
		}
	}

	internal class ProvocationLoc : CompiledLocStringCollection<ProvocationLoc> {
		public string UseTheCommand = "Provokace se vyvolava prikazem .provo";
		public string DontHurry = "Nespechej s pouzivanim skillu";
		public string WhoFirst = "Koho chces vyprovokovat?";
		public string WhoAgainst = "Proti komu?";
		public string CantProvoke = "Tohle nemuzes vyprovokovat.";
		public string BeyondYou = "Provokace teto prisery presahuje tve moznosti";
		public string Worked = "Provokace se povedla.";
		public string DidntWork = "Provokace se nepovedla.";
		public string DoesntReact = "{0} nereaguje na provokaci.";
	}
}
