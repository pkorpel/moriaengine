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

	/// <summary>#962 - Erebor's systems/skills/peacemaking.scp: one creature falls asleep, for 10 s plus a second for each
	/// 2.0 of skill, and goes for the bard when it wakes. Being hit doesn't wake it. The timer starts with the cursor, so a
	/// cursor left unanswered blocks the skill for its 60 s, as on Erebor.</summary>
	[ViewableClass]
	public class PeacemakingSkillDef : SkillDef {
		public PeacemakingSkillDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
		}

		protected override TriggerResult On_Select(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			PeacemakingLoc texts = Loc<PeacemakingLoc>.Get(self.Language);
			if (self.GetTimer(PeacemakingTimer.Key) != null) {
				self.SysMessage(texts.DontHurry);
			} else if (self.Flag_Dead) {
			} else if (Bard.FindInstrument(self) == null) {
				self.SysMessage(Loc<BardLoc>.Get(self.Language).NeedInstrument);
			} else {
				HidingSkillDef.Reveal(self);
				ScriptedSkills.Stop(self);
				if (!PlayerHunger.DistractsFrom(self) && (self is Player player)) {
					PeacemakingTimer timer = new PeacemakingTimer();
					self.AddTimer(PeacemakingTimer.Key, timer).DueInSeconds = PeacemakingTimer.SecondsForTheCursor;
					self.SysMessage(texts.WhoToCalm);
					player.Target(SingletonScript<Targ_Peacemaking>.Instance, timer);
				}
			}
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

		/// <summary>stopscriptedskills.</summary>
		internal static void Spoil(Character self) {
			PeacemakingTimer timer = self.GetTimer(PeacemakingTimer.Key) as PeacemakingTimer;
			if (timer != null) {
				timer.works = false;
				timer.DueInSeconds = 0;
			}
		}

		public static bool IsAsleep(NPC npc) {
			return npc.GetTimer(PeacefulSleepTimer.Key) != null;
		}
	}

	public class Targ_Peacemaking : CompiledTargetDef {
		protected override TargetResult On_TargonChar(Player self, Character targetted, object parameter) {
			PeacemakingTimer timer = (PeacemakingTimer) parameter;
			if (timer.IsDeleted) {
				return TargetResult.Done;
			}
			PeacemakingLoc texts = Loc<PeacemakingLoc>.Get(self.Language);
			SkillDef skill = SkillDef.GetBySkillName(SkillName.Peacemaking);
			NPC npc = targetted as NPC;
			if ((npc == null) || SummonSpellDef.IsSummonedCreature(npc) || Bard.IsBoss(npc)) {
				self.SysMessage(texts.CantCalm);
				timer.Delete();
			} else if ((self.GetSkill(SkillName.Peacemaking) <= npc.GetSkill(SkillName.Peacemaking)) || (Bard.Reach(skill, self) < Bard.ExperienceOf(npc))) {
				self.SysMessage(texts.BeyondYou);
				timer.Delete();
			} else {
				timer.target = npc;
				timer.played = true;
				timer.works = Bard.Plays(self, Bard.FindInstrument(self)) && (Bard.Power(skill, self) >= Bard.ExperienceOf(npc));
				timer.DueInSeconds = Bard.Delay(skill, self);
			}
			return TargetResult.Done;
		}

		protected override TargetResult On_TargonItem(Player self, Item targetted, object parameter) {
			return TargetResult.Done;
		}

		protected override TargetResult On_TargonGround(Player self, IPoint3D targetted, object parameter) {
			return TargetResult.Done;
		}
	}

	/// <summary>i_peacemaking_timer.</summary>
	[SaveableClass, DeepCopyableClass]
	public class PeacemakingTimer : BoundTimer {
		public static readonly TimerKey Key = TimerKey.Acquire("_peacemaking_");

		internal const double SecondsForTheCursor = 60;

		[SaveableData, CopyableData]
		public NPC target;

		[SaveableData, CopyableData]
		public bool played;

		[SaveableData, CopyableData]
		public bool works;

		[LoadingInitializer, DeepCopyImplementation]
		public PeacemakingTimer() {
		}

		protected override void OnTimeout(TagHolder cont) {
			Character self = (Character) cont;
			if (!this.played) {
				return;
			}
			PeacemakingLoc texts = Loc<PeacemakingLoc>.Get(self.Language);
			if ((this.target == null) || this.target.IsDeleted || this.target.Flag_Dead) {
				self.RedMessage(texts.NothingToCalm);
			} else if (this.works) {
				self.SysMessage(texts.Calmed, Bandaging.ClassMessageColor);
				PeacefulSleepTimer.Fall(this.target, self);
			} else {
				self.RedMessage(texts.DidntWork);
				PeacefulSleepTimer.Wake(this.target);
			}
		}
	}

	/// <summary>i_rune_peacemaking with e_peacemade: it neither fights nor casts.</summary>
	[SaveableClass, DeepCopyableClass]
	public class PeacefulSleepTimer : BoundTimer {
		public static readonly TimerKey Key = TimerKey.Acquire("_peacefulSleep_");

		private const int LeastSeconds = 10, SkillForASecond = 20, EffectSpeed = 1, EffectLength = 70;

		[SaveableData, CopyableData]
		public Character bard;

		[LoadingInitializer, DeepCopyImplementation]
		public PeacefulSleepTimer() {
		}

		internal static void Fall(NPC npc, Character bard) {
			PeacefulSleepTimer timer = npc.GetTimer(Key) as PeacefulSleepTimer;
			if (timer == null) {
				timer = new PeacefulSleepTimer();
				npc.AddTimer(Key, timer);
				SkillSequenceArgs.AbortSkill(npc);
				NPCAiTimer.GetFor(npc)?.Disengage();
				npc.Emote("is falling asleep");
				EffectFactory.StationaryEffect(npc, ThingDef.GetByDefname("i_fx_sleeping").Model, EffectSpeed, EffectLength);
			}
			timer.bard = bard;
			timer.DueInSeconds = LeastSeconds + (bard.GetSkill(SkillName.Peacemaking) / SkillForASecond);
			FightMemory.Record(npc, bard);
		}

		internal static void Wake(NPC npc) {
			BoundTimer timer = npc.GetTimer(Key);
			if (timer != null) {
				timer.Delete();
				Awake(npc);
			}
		}

		private static void Awake(NPC npc) {
			if (npc.Hits > 0) {
				npc.Emote("woke up");
			}
		}

		protected override void OnTimeout(TagHolder cont) {
			NPC npc = (NPC) cont;
			Awake(npc);
			if ((this.bard != null) && !this.bard.IsDeleted) {
				NPCAiTimer.Attack(npc, this.bard);
			}
		}
	}

	internal class PeacemakingLoc : CompiledLocStringCollection<PeacemakingLoc> {
		public string DontHurry = "Nespechej s pouzivanim skillu.";
		public string WhoToCalm = "Koho chces zkusit uklidnit?";
		public string CantCalm = "Tohle nemuzes uklidnit.";
		public string BeyondYou = "Uklidneni teto prisery presahuje tve moznosti.";
		public string Calmed = "Uklidneni se povedlo.";
		public string DidntWork = "Uklidnovani nezabralo.";
		public string NothingToCalm = "Jiz neni co uklidnovat!";
	}
}
