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
using SteamEngine.Timers;
using SteamEngine.Persistence;

namespace SteamEngine.CompiledScripts {

	/// <summary>#962 - Erebor's f_tezba_pauza: what Mining, Lumberjacking and Fishing do first. A try too soon after the last
	/// is refused and pushes the next one back; riders get down; the dark, hunger and being hidden are dealt with.
	/// Then the strokes of Sphere's skills: Mining and Lumberjacking swing a few times at the skill's delay.</summary>
	public static class Gathering {
		private const int Reach = 3;
		private const double MoreForTheImpatient = 2;
		private static readonly TagKey strokesTK = TagKey.Acquire("_gatherStrokes_");

		/// <summary>False when it doesn't start.</summary>
		internal static bool Begin(Character self, IPoint3D target, SkillName skill) {
			GatheringLoc texts = Loc<GatheringLoc>.Get(self.Language);
			double delay = GatheringDelaySystem.GetDelaySeconds(self, skill);
			BoundTimer waiting = self.GetTimer(GatherDelayTimer.Key);
			if (waiting != null) {
				self.SysMessage(texts.NotYet);
				waiting.DueInSeconds = delay + MoreForTheImpatient;
				return false;
			}
			self.AddTimer(GatherDelayTimer.Key, new GatherDelayTimer()).DueInSeconds = delay;
			if (self.Flag_Riding) {
				self.SysMessage(texts.FromTheSaddle);
				self.Dismount();
			}
			if (!WorkLight.CheckWithMessage(self)) {
				return false;
			}
			PlayerHunger.HurtIfStarving(self);
			HidingSkillDef.Reveal(self);
			if ((Point2D.GetSimpleDistance(self, target) > Reach) && !self.IsGM) {
				self.SysMessage(texts.TooFar);
				return false;
			}
			if (!self.CanSeeLOS(target).Allow) {
				self.SysMessage(texts.CantReach);
				return false;
			}
			return true;
		}

		internal static void RollStrokes(Character self) {
			GatheringDelaySystem delays = GatheringDelaySystem.instance;
			self.SetTag(strokesTK, Globals.dice.Next(delays.leastStrokes, delays.mostStrokes + 1));
		}

		/// <summary>True while there are swings to go. The caller waits for the next one.</summary>
		internal static bool NextStroke(SkillSequenceArgs skillSeqArgs) {
			Character self = skillSeqArgs.Self;
			int strokes = (self.GetTag(strokesTK) is int left) ? left : 0;
			if (strokes <= 0) {
				return false;
			}
			self.SetTag(strokesTK, strokes - 1);
			AnimCalculator.PerformAttackAnim(self);
			skillSeqArgs.DelayStroke();
			return true;
		}
	}

	/// <summary>i_delayer.</summary>
	[SaveableClass, DeepCopyableClass]
	public class GatherDelayTimer : BoundTimer {
		public static readonly TimerKey Key = TimerKey.Acquire("_gatherDelay_");

		[LoadingInitializer, DeepCopyImplementation]
		public GatherDelayTimer() {
		}

		protected override void OnTimeout(TagHolder cont) {
		}
	}

	internal class GatheringLoc : CompiledLocStringCollection<GatheringLoc> {
		public string NotYet = "Jeste nemuzes pouzit skill.";
		public string FromTheSaddle = "Ze sedla to pujde tezko.";
		public string TooFar = "You are too far away.";
		public string CantReach = "Tam nedosahnes.";
		public string MiningFailMale = "Nevykopal jsi nic zajimaveho.";
		public string MiningFailFemale = "Nevykopala jsi nic zajimaveho.";
		public string LumberjackingFail = "Drevo se nepodarilo kvalitne odseknout.";
		public string FishingFail = "Dneska nejak neberou.";
		public string PulledOut = "You pull out a {0}!";
	}
}
