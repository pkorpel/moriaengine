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
using System.Runtime.CompilerServices;
using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Persistence;
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts {

	/// <summary>#962 - Erebor's f_testreveal: 2 s of looking around, then everyone unseen within 25 tiles is tried once. Skill minus
	/// their Hiding and the distance beats a roll of their Hiding, and a Hiding of over 100.0 counts three times. The same
	/// person isn't tried again for 7 s. You show yourself, and it can't be done again for 8 to 4 s.</summary>
	[ViewableClass]
	public class DetectHiddenSkillDef : SkillDef {
		private const int Range = 25, GraceAbove = 1000, DistanceDrop = 50, WarnFactor = 50, WarnRollLeast = 50, WarnRollMost = 400;
		private const double SecondsLooking = 2, SecondsBetweenTries = 7;

		private static readonly TagKey nextDetectTK = TagKey.Acquire("nextdetect");
		private static readonly ConditionalWeakTable<Character, Dictionary<Character, double>> triedBy = new ConditionalWeakTable<Character, Dictionary<Character, double>>();

		public DetectHiddenSkillDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
		}

		protected override TriggerResult On_Select(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			DetectLoc texts = Loc<DetectLoc>.Get(self.Language);
			HidingSkillDef.Spoil(self);
			HidingSkillDef.Reveal(self);
			object next = self.GetTag(nextDetectTK);
			if (ParalyzeEffectPluginDef.IsParalyzed(self)) {
				self.SysMessage(texts.Frozen);
			} else if (self.Flag_Dead) {
				self.SysMessage(texts.Dead);
			} else if ((next != null) && ((double) next > Globals.TimeInSeconds) && !self.IsGM) {
				self.SysMessage(texts.NotYet);
			} else {
				self.SysMessage(texts.Looking);
				self.SetTag(nextDetectTK, Globals.TimeInSeconds + this.GetDelayForValue((ushort) self.GetSkill(SkillName.DetectHidden)));
				self.AddTimer(DetectTimer.Key, new DetectTimer()).DueInSeconds = SecondsLooking;
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

		internal static void Finish(Character self) {
			foreach (AbstractCharacter other in new List<AbstractCharacter>(self.GetMap().GetCharsInRange(self.X, self.Y, Range))) {
				Character target = other as Character;
				if ((target != null) && (target != self) && !target.IsGM && !target.Flag_Invulnerable && !target.Flag_Dead && (target.Flag_Hidden || target.Flag_InvisByMagic)) {
					Try(self, target);
				}
			}
			self.SysMessage(Loc<DetectLoc>.Get(self.Language).Finished);
		}

		private static void Try(Character self, Character target) {
			Dictionary<Character, double> tried = triedBy.GetOrCreateValue(target);
			double until;
			if (tried.TryGetValue(self, out until) && (until > Globals.TimeInSeconds)) {
				return;
			}
			tried[self] = Globals.TimeInSeconds + SecondsBetweenTries;
			int distance = Point2D.GetSimpleDistance(self, target);
			int detect = self.GetSkill(SkillName.DetectHidden);
			int hiding = target.GetSkill(SkillName.Hiding);
			if (hiding > (((detect / Globals.dice.Next(WarnRollLeast, WarnRollMost + 1)) + distance) * WarnFactor)) {
				target.SysMessage(string.Format(Loc<DetectLoc>.Get(target.Language).TryingToFindYou, self.Name));
			}
			int beyondMastery = Math.Max(0, (hiding - GraceAbove) * 2);
			if ((detect - hiding + GraceAbove - (distance * DistanceDrop)) > (Globals.dice.Next(hiding + 1) + beyondMastery)) {
				HidingSkillDef.Reveal(target);
				DetectLoc texts = Loc<DetectLoc>.Get(self.Language);
				self.SysMessage(string.Format(self.IsFemale ? texts.FoundFemale : texts.FoundMale, target.Name));
			}
		}
	}

	[SaveableClass, DeepCopyableClass]
	public class DetectTimer : BoundTimer {
		public static readonly TimerKey Key = TimerKey.Acquire("_detecting_");

		[LoadingInitializer, DeepCopyImplementation]
		public DetectTimer() {
		}

		protected override void OnTimeout(TagHolder cont) {
			DetectHiddenSkillDef.Finish((Character) cont);
		}
	}

	internal class DetectLoc : CompiledLocStringCollection<DetectLoc> {
		public string Frozen = "You are frozen and cannot move.";
		public string Dead = "You are dead!";
		public string NotYet = "Jeste nelze pouzit.";
		public string Looking = "Prozkoumavas sve okoli";
		public string Finished = "Hledani ukonceno.";
		public string TryingToFindYou = "{0} se te pokousi objevit.";
		public string FoundMale = "Objevil jsi {0}.";
		public string FoundFemale = "Objevila jsi {0}.";
	}
}
